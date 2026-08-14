using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public interface IFeatureAccessService
{
    Task<string> GetCurrentSubscriptionTierAsync(CancellationToken cancellationToken = default);
    Task<bool> CanUseFeatureAsync(string featureKey, CancellationToken cancellationToken = default);
    Task<FeatureAccessDecision> GetFeatureAccessAsync(string featureKey, CancellationToken cancellationToken = default);
}

public sealed record FeatureAccessDecision(
    string FeatureKey,
    FeatureAccessMode Mode,
    string EffectiveTier,
    string EntitlementStatus,
    string Message)
{
    public bool IsFullAccess => Mode == FeatureAccessMode.Full;
    public bool IsReadOnlyExport => Mode == FeatureAccessMode.ReadOnlyExport;
}

public class FeatureAccessService : IFeatureAccessService
{
    private static readonly Dictionary<string, string> FeatureMinimumTiers = new(StringComparer.OrdinalIgnoreCase)
    {
        [VectorFeatures.DailyVehicleReadiness] = SubscriptionTiers.Base,
        [VectorFeatures.BasicIssueReporting] = SubscriptionTiers.Base,
        [VectorFeatures.TaskFeedback] = SubscriptionTiers.Base,
        [VectorFeatures.BasicAuditTrail] = SubscriptionTiers.Base,
        [VectorFeatures.LocalFileStorage] = SubscriptionTiers.Base,

        [VectorFeatures.VehicleEquipmentLoadouts] = SubscriptionTiers.Pro,
        [VectorFeatures.CustomChecklistBuilder] = SubscriptionTiers.Base,
        [VectorFeatures.EquipmentServiceTracking] = SubscriptionTiers.Pro,
        [VectorFeatures.StaffFiles] = SubscriptionTiers.Pro,
        [VectorFeatures.MedicationRegister] = SubscriptionTiers.Pro,
        [VectorFeatures.StockRegister] = SubscriptionTiers.Pro,
        [VectorFeatures.ManagerIssuePool] = SubscriptionTiers.Pro,
        [VectorFeatures.SameAsPreviousShiftControl] = SubscriptionTiers.Pro,
        [VectorFeatures.GuidedRegisterImport] = SubscriptionTiers.Pro,
        [VectorFeatures.GuidedChecklistImport] = SubscriptionTiers.Pro,

        [VectorFeatures.ReadinessAnalytics] = SubscriptionTiers.Premium,
        [VectorFeatures.AiChecklistImport] = SubscriptionTiers.Premium,
        [VectorFeatures.AiImportIntelligence] = SubscriptionTiers.Premium,
        [VectorFeatures.AdvancedExports] = SubscriptionTiers.Premium,
        [VectorFeatures.AzureBlobStorage] = SubscriptionTiers.Premium,
        [VectorFeatures.MultiSiteReporting] = SubscriptionTiers.Premium,
        [VectorFeatures.EscalationRules] = SubscriptionTiers.Premium
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly VectorDbContext _db;
    private readonly TimeProvider _timeProvider;

    public FeatureAccessService(IHttpContextAccessor httpContextAccessor, VectorDbContext db, TimeProvider timeProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<string> GetCurrentSubscriptionTierAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _httpContextAccessor.HttpContext?.Session.GetInt32(CurrentUserService.CompanyIdSessionKey);
        if (!companyId.HasValue)
        {
            return SubscriptionTiers.Base;
        }

        var entitlement = await PilotEntitlementAccess.LoadAsync(
            _db, companyId.Value, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return entitlement.IsFullAccess
            ? SubscriptionTiers.Normalize(entitlement.Tier)
            : SubscriptionTiers.Base;
    }

    public async Task<bool> CanUseFeatureAsync(string featureKey, CancellationToken cancellationToken = default)
    {
        return (await GetFeatureAccessAsync(featureKey, cancellationToken)).IsFullAccess;
    }

    public async Task<FeatureAccessDecision> GetFeatureAccessAsync(
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        if (!FeatureMinimumTiers.TryGetValue(featureKey, out var minimumTier))
        {
            return new(featureKey, FeatureAccessMode.Unavailable, SubscriptionTiers.Base, "Unknown",
                "This feature is not registered in the product access catalogue.");
        }

        if (string.Equals(minimumTier, SubscriptionTiers.Base, StringComparison.OrdinalIgnoreCase))
        {
            return new(featureKey, FeatureAccessMode.Full, SubscriptionTiers.Base, PilotEntitlementStatuses.Active,
                "This feature is available in the Base product.");
        }

        var companyId = _httpContextAccessor.HttpContext?.Session.GetInt32(CurrentUserService.CompanyIdSessionKey);
        if (!companyId.HasValue)
        {
            return new(featureKey, FeatureAccessMode.Unavailable, SubscriptionTiers.Base, "NoCompany",
                "Open an authorised company workspace before using this feature.");
        }

        var entitlement = await PilotEntitlementAccess.LoadAsync(
            _db, companyId.Value, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        var tierQualifies = SubscriptionTiers.IsAtLeast(entitlement.Tier, minimumTier);
        if (entitlement.IsFullAccess && tierQualifies)
        {
            return new(featureKey, FeatureAccessMode.Full, entitlement.Tier, entitlement.EffectiveStatus, entitlement.Notice);
        }

        if (entitlement.IsReadOnlyExport && tierQualifies)
        {
            return new(featureKey, FeatureAccessMode.ReadOnlyExport, entitlement.Tier, entitlement.EffectiveStatus,
                entitlement.Notice);
        }

        return new(featureKey, FeatureAccessMode.Unavailable, SubscriptionTiers.Base, entitlement.EffectiveStatus,
            entitlement.Notice);
    }

}
