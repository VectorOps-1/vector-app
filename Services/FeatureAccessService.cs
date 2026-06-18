using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public interface IFeatureAccessService
{
    Task<string> GetCurrentSubscriptionTierAsync(CancellationToken cancellationToken = default);
    Task<bool> CanUseFeatureAsync(string featureKey, CancellationToken cancellationToken = default);
    bool CanUseFeature(string? subscriptionTier, string featureKey);
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

        [VectorFeatures.ReadinessAnalytics] = SubscriptionTiers.Premium,
        [VectorFeatures.AiChecklistImport] = SubscriptionTiers.Premium,
        [VectorFeatures.AdvancedExports] = SubscriptionTiers.Premium,
        [VectorFeatures.AzureBlobStorage] = SubscriptionTiers.Premium,
        [VectorFeatures.MultiSiteReporting] = SubscriptionTiers.Premium,
        [VectorFeatures.EscalationRules] = SubscriptionTiers.Premium
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly VectorDbContext _db;

    public FeatureAccessService(IHttpContextAccessor httpContextAccessor, VectorDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public async Task<string> GetCurrentSubscriptionTierAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _httpContextAccessor.HttpContext?.Session.GetInt32(CurrentUserService.CompanyIdSessionKey);
        if (!companyId.HasValue)
        {
            return SubscriptionTiers.Base;
        }

        var tier = await _db.Companies
            .AsNoTracking()
            .Where(company => company.Id == companyId.Value)
            .Select(company => company.SubscriptionTier)
            .FirstOrDefaultAsync(cancellationToken);

        return SubscriptionTiers.Normalize(tier);
    }

    public async Task<bool> CanUseFeatureAsync(string featureKey, CancellationToken cancellationToken = default)
    {
        var tier = await GetCurrentSubscriptionTierAsync(cancellationToken);
        return CanUseFeature(tier, featureKey);
    }

    public bool CanUseFeature(string? subscriptionTier, string featureKey)
    {
        if (!FeatureMinimumTiers.TryGetValue(featureKey, out var minimumTier))
        {
            return false;
        }

        return SubscriptionTiers.IsAtLeast(subscriptionTier, minimumTier);
    }
}
