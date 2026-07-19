using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed class CompliancePackGovernanceService
{
    private readonly VectorDbContext _db;
    private readonly IProductComplianceAuthorization _authorization;

    public CompliancePackGovernanceService(
        VectorDbContext db,
        IProductComplianceAuthorization authorization)
    {
        _db = db;
        _authorization = authorization;
    }

    public async Task TransitionSourceVersionAsync(
        int sourceVersionId,
        string targetState,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var actor = await AuthorizeAsync("compliance.source-version.transition", cancellationToken);
        var version = await _db.RegulatorySourceVersions
            .Include(item => item.Clauses)
            .Include(item => item.Reviews)
            .SingleOrDefaultAsync(item => item.Id == sourceVersionId, cancellationToken)
            ?? throw new InvalidOperationException("Regulatory source version was not found.");

        ValidateTransition(version.LifecycleState, targetState);
        ValidateSourceReadiness(version, targetState);

        var previous = version.LifecycleState;
        version.LifecycleState = targetState;
        version.UpdatedAtUtc = DateTime.UtcNow;
        version.ConcurrencyToken = Guid.NewGuid().ToString("N");
        if (targetState == ComplianceLifecycleStates.Superseded)
        {
            version.SupersededAtUtc = DateTime.UtcNow;
        }

        AddEvent(
            ComplianceGovernanceEntityTypes.SourceVersion,
            version.Id,
            actor,
            previous,
            targetState,
            reason,
            version.ContentHashSha256);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TransitionPackVersionAsync(
        int packVersionId,
        string targetState,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var actor = await AuthorizeAsync("compliance.pack-version.transition", cancellationToken);
        var version = await _db.ComplianceRequirementPackVersions
            .Include(item => item.Requirements)
                .ThenInclude(requirement => requirement.SourceClauses)
            .Include(item => item.Sources)
                .ThenInclude(link => link.RegulatorySourceVersion)
            .Include(item => item.Reviews)
            .SingleOrDefaultAsync(item => item.Id == packVersionId, cancellationToken)
            ?? throw new InvalidOperationException("Compliance pack version was not found.");

        ValidateTransition(version.LifecycleState, targetState);
        ValidatePackReadiness(version, targetState);

        await using var transaction = targetState == ComplianceLifecycleStates.Active
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (targetState == ComplianceLifecycleStates.Active)
        {
            var currentVersions = await _db.ComplianceRequirementPackVersions
                .Where(item => item.ComplianceRequirementPackId == version.ComplianceRequirementPackId
                    && item.Id != version.Id
                    && item.LifecycleState == ComplianceLifecycleStates.Active)
                .ToListAsync(cancellationToken);

            foreach (var current in currentVersions)
            {
                current.LifecycleState = ComplianceLifecycleStates.Superseded;
                current.ActiveSlot = null;
                current.SupersededAtUtc = DateTime.UtcNow;
                current.UpdatedAtUtc = DateTime.UtcNow;
                current.ConcurrencyToken = Guid.NewGuid().ToString("N");
                AddEvent(
                    ComplianceGovernanceEntityTypes.PackVersion,
                    current.Id,
                    actor,
                    ComplianceLifecycleStates.Active,
                    ComplianceLifecycleStates.Superseded,
                    $"Replaced by pack version {version.VersionLabel}.",
                    current.ContentHashSha256);
            }

            if (currentVersions.Count > 0)
            {
                // Persist the retiring slot first inside the same transaction. Both SQLite and
                // SQL Server can otherwise evaluate the unique active-slot index before the
                // replacement row update is ordered.
                await _db.SaveChangesAsync(cancellationToken);
            }

            version.ActiveSlot = 1;
            version.ActivatedAtUtc = DateTime.UtcNow;
        }
        else if (targetState is ComplianceLifecycleStates.Superseded or ComplianceLifecycleStates.Withdrawn)
        {
            version.ActiveSlot = null;
            version.SupersededAtUtc = DateTime.UtcNow;
        }

        var previous = version.LifecycleState;
        version.LifecycleState = targetState;
        version.UpdatedAtUtc = DateTime.UtcNow;
        version.ConcurrencyToken = Guid.NewGuid().ToString("N");

        AddEvent(
            ComplianceGovernanceEntityTypes.PackVersion,
            version.Id,
            actor,
            previous,
            targetState,
            reason,
            version.ContentHashSha256);

        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private async Task<string> AuthorizeAsync(string operation, CancellationToken cancellationToken)
    {
        var result = await _authorization.AuthorizeAsync(operation, cancellationToken);
        if (!result.IsAuthorized || string.IsNullOrWhiteSpace(result.ActorIdentifier))
        {
            throw new UnauthorizedAccessException(
                result.DenialReason ?? "Product compliance governance authorization was denied.");
        }

        return result.ActorIdentifier.Trim();
    }

    private static void ValidateTransition(string currentState, string targetState)
    {
        if (!ComplianceLifecycleStates.All.Contains(targetState))
        {
            throw new InvalidOperationException($"Unknown compliance lifecycle state '{targetState}'.");
        }

        if (!ComplianceLifecycleStates.CanTransition(currentState, targetState))
        {
            throw new InvalidOperationException(
                $"Compliance lifecycle transition from '{currentState}' to '{targetState}' is not allowed.");
        }
    }

    private static void ValidateSourceReadiness(RegulatorySourceVersion version, string targetState)
    {
        if (targetState == ComplianceLifecycleStates.SourceVerified)
        {
            if (string.IsNullOrWhiteSpace(version.OfficialUrl)
                || string.IsNullOrWhiteSpace(version.StoredArtifactReference)
                || version.ContentHashSha256.Length != 64
                || version.Clauses.Count == 0
                || version.Clauses.Any(clause => !clause.IsVerified)
                || !string.IsNullOrWhiteSpace(version.UncertaintyNote)
                || !string.IsNullOrWhiteSpace(version.ConflictNote))
            {
                throw new InvalidOperationException(
                    "A source cannot be verified without an official location, retained artifact, SHA-256 hash, verified clauses, and resolved uncertainty/conflicts.");
            }
        }

        if (targetState == ComplianceLifecycleStates.Approved)
        {
            RequireApprovedReview(version.Reviews, ComplianceReviewTypes.SourceVerification);
            RequireApprovedReview(version.Reviews, ComplianceReviewTypes.Legal);
        }
    }

    private static void ValidatePackReadiness(ComplianceRequirementPackVersion version, string targetState)
    {
        if (targetState is ComplianceLifecycleStates.SourceVerified
            or ComplianceLifecycleStates.LegalReviewPending
            or ComplianceLifecycleStates.Approved
            or ComplianceLifecycleStates.Active)
        {
            if (version.SourceCompletenessState != ComplianceSourceCompletenessStates.Complete
                || version.Sources.Count == 0
                || version.Requirements.Count == 0
                || version.Requirements.Any(requirement => requirement.SourceClauses.Count == 0)
                || version.Requirements.Any(requirement => !string.IsNullOrWhiteSpace(requirement.ConflictNote)
                    || !string.IsNullOrWhiteSpace(requirement.UncertaintyNote))
                || !string.IsNullOrWhiteSpace(version.ConflictNote))
            {
                throw new InvalidOperationException(
                    "An incomplete, conflicted, uncertain, or unproven pack cannot advance to authoritative use.");
            }

            if (version.Sources.Any(link => link.RegulatorySourceVersion is null
                || link.RegulatorySourceVersion.LifecycleState is not (ComplianceLifecycleStates.Approved or ComplianceLifecycleStates.Active)))
            {
                throw new InvalidOperationException(
                    "Every source used by an authoritative pack must be approved or active.");
            }
        }

        if (targetState is ComplianceLifecycleStates.Approved or ComplianceLifecycleStates.Active)
        {
            RequireApprovedReview(version.Reviews, ComplianceReviewTypes.Legal);
            RequireApprovedReview(version.Reviews, ComplianceReviewTypes.Operational);
        }
    }

    private static void RequireApprovedReview(
        IEnumerable<ComplianceRuleReview> reviews,
        string reviewType)
    {
        if (!reviews.Any(review => review.ReviewType == reviewType
            && review.Decision == ComplianceReviewDecisions.Approved))
        {
            throw new InvalidOperationException($"An approved {reviewType} review is required.");
        }
    }

    private void AddEvent(
        string entityType,
        int entityId,
        string actor,
        string fromState,
        string toState,
        string reason,
        string? payloadHash)
    {
        _db.ComplianceGovernanceEvents.Add(new ComplianceGovernanceEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            EventType = "LifecycleTransition",
            ActorIdentifier = actor,
            FromState = fromState,
            ToState = toState,
            Reason = reason.Trim(),
            PayloadHashSha256 = payloadHash,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
