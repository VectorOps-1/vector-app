using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed record ActiveCompliancePack(
    int PackVersionId,
    string PackCode,
    string PackName,
    string VersionLabel,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo);

public sealed record JurisdictionPackStatus(
    int JurisdictionId,
    string JurisdictionCode,
    string JurisdictionName,
    string JurisdictionLevel,
    bool IsComplete,
    string StatusLabel,
    IReadOnlyList<ActiveCompliancePack> ActivePacks);

public sealed record CompliancePackComposition(
    string CountryCode,
    bool IsAuthoritative,
    IReadOnlyList<JurisdictionPackStatus> Jurisdictions);

public sealed class ComplianceSourceRegistryReader
{
    public const string IncompleteStatusLabel = "Source pack incomplete";

    private readonly VectorDbContext _db;

    public ComplianceSourceRegistryReader(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<CompliancePackComposition> GetActivePackCompositionAsync(
        string countryCode,
        IEnumerable<string> provinceCodes,
        CancellationToken cancellationToken = default)
    {
        var normalizedCountry = NormalizeCode(countryCode);
        var normalizedProvinceCodes = provinceCodes
            .Select(NormalizeCode)
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var country = await _db.Jurisdictions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == normalizedCountry
                && item.Level == ComplianceJurisdictionLevels.Country, cancellationToken)
            ?? throw new InvalidOperationException("Country jurisdiction was not found.");

        var provinces = await _db.Jurisdictions
            .AsNoTracking()
            .Where(item => normalizedProvinceCodes.Contains(item.Code)
                && item.Level == ComplianceJurisdictionLevels.Province)
            .ToListAsync(cancellationToken);

        var invalidProvinceCodes = normalizedProvinceCodes
            .Except(provinces.Select(item => item.Code), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (invalidProvinceCodes.Length > 0
            || provinces.Any(item => item.ParentJurisdictionId != country.Id))
        {
            throw new InvalidOperationException(
                "Every selected province must be a direct child of the selected country. Provincial packs are never substituted across jurisdictions.");
        }

        var jurisdictions = new List<Jurisdiction> { country };
        jurisdictions.AddRange(provinces.OrderBy(item => item.Name));
        var jurisdictionIds = jurisdictions.Select(item => item.Id).ToArray();

        var activeVersions = await _db.ComplianceRequirementPackVersions
            .AsNoTracking()
            .Include(item => item.ComplianceRequirementPack)
            .Include(item => item.Requirements)
                .ThenInclude(requirement => requirement.SourceClauses)
            .Include(item => item.Sources)
                .ThenInclude(link => link.RegulatorySourceVersion)
            .Include(item => item.Reviews)
            .Where(item => item.LifecycleState == ComplianceLifecycleStates.Active
                && item.ActiveSlot == 1
                && item.SourceCompletenessState == ComplianceSourceCompletenessStates.Complete
                && item.ComplianceRequirementPack != null
                && jurisdictionIds.Contains(item.ComplianceRequirementPack.PrimaryJurisdictionId))
            .ToListAsync(cancellationToken);

        var statuses = jurisdictions.Select(jurisdiction =>
        {
            var packs = activeVersions
                .Where(item => item.ComplianceRequirementPack!.PrimaryJurisdictionId == jurisdiction.Id)
                .Where(IsIndependentlyAuthoritative)
                .OrderBy(item => item.ComplianceRequirementPack!.PackCode)
                .Select(item => new ActiveCompliancePack(
                    item.Id,
                    item.ComplianceRequirementPack!.PackCode,
                    item.ComplianceRequirementPack.Name,
                    item.VersionLabel,
                    item.EffectiveFrom,
                    item.EffectiveTo))
                .ToArray();
            var isComplete = packs.Length > 0;
            return new JurisdictionPackStatus(
                jurisdiction.Id,
                jurisdiction.Code,
                jurisdiction.Name,
                jurisdiction.Level,
                isComplete,
                isComplete ? "Approved active source pack" : IncompleteStatusLabel,
                packs);
        }).ToArray();

        return new CompliancePackComposition(
            country.Code,
            statuses.All(status => status.IsComplete),
            statuses);
    }

    private static string NormalizeCode(string value) =>
        value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static bool IsIndependentlyAuthoritative(ComplianceRequirementPackVersion version)
    {
        return version.Sources.Count > 0
            && version.Sources.All(link => link.RegulatorySourceVersion?.LifecycleState
                is ComplianceLifecycleStates.Approved or ComplianceLifecycleStates.Active)
            && version.Requirements.Count > 0
            && version.Requirements.All(requirement => requirement.SourceClauses.Count > 0
                && string.IsNullOrWhiteSpace(requirement.UncertaintyNote)
                && string.IsNullOrWhiteSpace(requirement.ConflictNote))
            && version.Reviews.Any(review => review.ReviewType == ComplianceReviewTypes.Legal
                && review.Decision == ComplianceReviewDecisions.Approved)
            && version.Reviews.Any(review => review.ReviewType == ComplianceReviewTypes.Operational
                && review.Decision == ComplianceReviewDecisions.Approved)
            && string.IsNullOrWhiteSpace(version.ConflictNote);
    }
}
