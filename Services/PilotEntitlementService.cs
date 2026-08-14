using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public enum FeatureAccessMode
{
    Unavailable,
    Full,
    ReadOnlyExport
}

public sealed record PilotEntitlementSnapshot(
    int? EntitlementId,
    int CompanyId,
    string Tier,
    string EffectiveStatus,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    FeatureAccessMode AccessMode,
    int? DaysRemaining,
    string Notice)
{
    public bool IsFullAccess => AccessMode == FeatureAccessMode.Full;
    public bool IsReadOnlyExport => AccessMode == FeatureAccessMode.ReadOnlyExport;
}

public sealed record EntitlementOperationResult(bool Success, string Message, PilotEntitlement? Entitlement = null);

public static class PilotEntitlementAccess
{
    public static string EffectiveStatus(PilotEntitlement entitlement, DateTime utcNow)
    {
        if (string.Equals(entitlement.Status, PilotEntitlementStatuses.Revoked, StringComparison.OrdinalIgnoreCase))
        {
            return PilotEntitlementStatuses.Revoked;
        }

        if (utcNow < entitlement.StartsAtUtc)
        {
            return PilotEntitlementStatuses.Pending;
        }

        return utcNow >= entitlement.ExpiresAtUtc
            ? PilotEntitlementStatuses.Expired
            : PilotEntitlementStatuses.Active;
    }

    public static PilotEntitlementSnapshot Snapshot(PilotEntitlement? entitlement, int companyId, DateTime utcNow)
    {
        if (entitlement is null)
        {
            return new PilotEntitlementSnapshot(null, companyId, SubscriptionTiers.Base, "NotConfigured", null, null,
                FeatureAccessMode.Unavailable, null, "Premium pilot access has not been configured for this company.");
        }

        var status = EffectiveStatus(entitlement, utcNow);
        var daysRemaining = (int)Math.Ceiling((entitlement.ExpiresAtUtc - utcNow).TotalDays);
        var mode = status switch
        {
            PilotEntitlementStatuses.Active => FeatureAccessMode.Full,
            PilotEntitlementStatuses.Expired or PilotEntitlementStatuses.Revoked => FeatureAccessMode.ReadOnlyExport,
            _ => FeatureAccessMode.Unavailable
        };

        var notice = status switch
        {
            PilotEntitlementStatuses.Active when daysRemaining <= 15 => $"Premium pilot access expires in {Math.Max(0, daysRemaining)} day(s).",
            PilotEntitlementStatuses.Active when daysRemaining <= 30 => $"Premium pilot access expires in {daysRemaining} days.",
            PilotEntitlementStatuses.Active when daysRemaining <= 60 => $"Premium pilot access expires in {daysRemaining} days.",
            PilotEntitlementStatuses.Active => $"Premium pilot access is active until {entitlement.ExpiresAtUtc:yyyy-MM-dd}.",
            PilotEntitlementStatuses.Expired => "Premium pilot access has expired. Existing data remains available in read-only/export mode.",
            PilotEntitlementStatuses.Revoked => "Premium pilot access has been revoked. Existing data remains available in read-only/export mode.",
            _ => $"Premium pilot access begins on {entitlement.StartsAtUtc:yyyy-MM-dd}."
        };

        return new PilotEntitlementSnapshot(entitlement.Id, companyId, entitlement.Tier, status,
            entitlement.StartsAtUtc, entitlement.ExpiresAtUtc, mode, daysRemaining, notice);
    }

    public static async Task<PilotEntitlementSnapshot> LoadAsync(
        VectorDbContext db,
        int companyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var entitlement = await db.PilotEntitlements.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CompanyId == companyId, cancellationToken);
        return Snapshot(entitlement, companyId, utcNow);
    }
}

public sealed class PilotEntitlementService
{
    private readonly VectorDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PilotEntitlementService(VectorDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public Task<PilotEntitlementSnapshot> GetAsync(int companyId, CancellationToken cancellationToken = default)
        => PilotEntitlementAccess.LoadAsync(_db, companyId, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

    public async Task<IReadOnlyList<PilotEntitlementEvent>> GetHistoryAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        return await _db.PilotEntitlementEvents.AsNoTracking()
            .Where(evt => evt.CompanyId == companyId)
            .OrderByDescending(evt => evt.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<EntitlementOperationResult> ActivateAsync(
        AppUser actor,
        DateTime startsAtUtc,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage(actor))
        {
            return new(false, "Only a company owner or senior manager can manage the pilot entitlement.");
        }

        var companyExists = await _db.Companies.AsNoTracking()
            .AnyAsync(company => company.Id == actor.CompanyId && company.Status == "Active", cancellationToken);
        if (!companyExists)
        {
            return new(false, "The current company is not active.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var start = DateTime.SpecifyKind(startsAtUtc.Date, DateTimeKind.Utc);
        var expiry = start.AddYears(1);
        var entitlement = await _db.PilotEntitlements
            .SingleOrDefaultAsync(item => item.CompanyId == actor.CompanyId, cancellationToken);
        var eventType = PilotEntitlementEventTypes.Activated;
        string? previousStatus = null;
        DateTime? previousExpiry = null;

        if (entitlement is null)
        {
            entitlement = new PilotEntitlement
            {
                CompanyId = actor.CompanyId,
                Tier = SubscriptionTiers.Premium,
                StartsAtUtc = start,
                ExpiresAtUtc = expiry,
                Status = PilotEntitlementStatuses.Active,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = actor.Id
            };
            _db.PilotEntitlements.Add(entitlement);
        }
        else
        {
            var effectiveStatus = PilotEntitlementAccess.EffectiveStatus(entitlement, now);
            if (effectiveStatus is PilotEntitlementStatuses.Active or PilotEntitlementStatuses.Pending)
            {
                return new(false, "The company already has an active or scheduled Premium pilot entitlement.", entitlement);
            }

            eventType = PilotEntitlementEventTypes.Reactivated;
            previousStatus = effectiveStatus;
            previousExpiry = entitlement.ExpiresAtUtc;
            entitlement.StartsAtUtc = start;
            entitlement.ExpiresAtUtc = expiry;
            entitlement.Status = PilotEntitlementStatuses.Active;
            entitlement.RevokedAtUtc = null;
            entitlement.RevokedByUserId = null;
            entitlement.RevocationReason = null;
            entitlement.UpdatedAtUtc = now;
            entitlement.ConcurrencyToken = Guid.NewGuid().ToString("N");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        AddEvent(entitlement, actor, eventType, previousStatus, previousExpiry, note, now);
        AddAudit(actor, entitlement.Id, $"Premium pilot entitlement {eventType.ToLowerInvariant()}",
            $"Premium access {start:yyyy-MM-dd} to {expiry:yyyy-MM-dd}. {CleanNote(note)}", now);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, $"Premium pilot access is active until {expiry:yyyy-MM-dd}.", entitlement);
    }

    public async Task<EntitlementOperationResult> ExtendAsync(
        AppUser actor,
        DateTime newExpiryAtUtc,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage(actor)) return new(false, "Only a company owner or senior manager can manage the pilot entitlement.");
        var entitlement = await _db.PilotEntitlements.SingleOrDefaultAsync(item => item.CompanyId == actor.CompanyId, cancellationToken);
        if (entitlement is null) return new(false, "No Premium pilot entitlement exists for this company.");

        var newExpiry = DateTime.SpecifyKind(newExpiryAtUtc.Date, DateTimeKind.Utc);
        if (newExpiry <= entitlement.ExpiresAtUtc)
        {
            return new(false, "The extension date must be later than the current expiry date.", entitlement);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var previousExpiry = entitlement.ExpiresAtUtc;
        var previousStatus = PilotEntitlementAccess.EffectiveStatus(entitlement, now);
        entitlement.ExpiresAtUtc = newExpiry;
        entitlement.Status = PilotEntitlementStatuses.Active;
        entitlement.RevokedAtUtc = null;
        entitlement.RevokedByUserId = null;
        entitlement.RevocationReason = null;
        entitlement.UpdatedAtUtc = now;
        entitlement.ConcurrencyToken = Guid.NewGuid().ToString("N");
        AddEvent(entitlement, actor, PilotEntitlementEventTypes.Extended, previousStatus, previousExpiry, note, now);
        AddAudit(actor, entitlement.Id, "Premium pilot entitlement extended",
            $"Expiry changed from {previousExpiry:yyyy-MM-dd} to {newExpiry:yyyy-MM-dd}. {CleanNote(note)}", now);
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, $"Premium pilot access was extended to {newExpiry:yyyy-MM-dd}.", entitlement);
    }

    public async Task<EntitlementOperationResult> RevokeAsync(
        AppUser actor,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage(actor)) return new(false, "Only a company owner or senior manager can manage the pilot entitlement.");
        if (string.IsNullOrWhiteSpace(reason)) return new(false, "A revocation reason is required.");
        var entitlement = await _db.PilotEntitlements.SingleOrDefaultAsync(item => item.CompanyId == actor.CompanyId, cancellationToken);
        if (entitlement is null) return new(false, "No Premium pilot entitlement exists for this company.");
        if (string.Equals(entitlement.Status, PilotEntitlementStatuses.Revoked, StringComparison.OrdinalIgnoreCase))
        {
            return new(false, "The Premium pilot entitlement is already revoked.", entitlement);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var previousStatus = PilotEntitlementAccess.EffectiveStatus(entitlement, now);
        entitlement.Status = PilotEntitlementStatuses.Revoked;
        entitlement.RevokedAtUtc = now;
        entitlement.RevokedByUserId = actor.Id;
        entitlement.RevocationReason = reason.Trim();
        entitlement.UpdatedAtUtc = now;
        entitlement.ConcurrencyToken = Guid.NewGuid().ToString("N");
        AddEvent(entitlement, actor, PilotEntitlementEventTypes.Revoked, previousStatus, entitlement.ExpiresAtUtc, reason, now);
        AddAudit(actor, entitlement.Id, "Premium pilot entitlement revoked", reason.Trim(), now);
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Premium pilot access was revoked. Existing data remains available in read-only/export mode.", entitlement);
    }

    private void AddEvent(PilotEntitlement entitlement, AppUser actor, string type, string? previousStatus,
        DateTime? previousExpiry, string? details, DateTime now)
    {
        _db.PilotEntitlementEvents.Add(new PilotEntitlementEvent
        {
            CompanyId = actor.CompanyId,
            PilotEntitlementId = entitlement.Id,
            ActorUserId = actor.Id,
            EventType = type,
            PreviousStatus = previousStatus,
            NewStatus = PilotEntitlementAccess.EffectiveStatus(entitlement, now),
            PreviousExpiresAtUtc = previousExpiry,
            NewExpiresAtUtc = entitlement.ExpiresAtUtc,
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            OccurredAtUtc = now
        });
    }

    private void AddAudit(AppUser actor, int entitlementId, string action, string details, DateTime now)
        => AuditTrailService.Record(_db, actor.CompanyId, actor.Id, action, nameof(PilotEntitlement), entitlementId, details, now);

    private static bool CanManage(AppUser actor) => CurrentUserService.IsSeniorAccessRole(actor.AppRole?.Name);
    private static string CleanNote(string? note) => string.IsNullOrWhiteSpace(note) ? string.Empty : note.Trim();
}
