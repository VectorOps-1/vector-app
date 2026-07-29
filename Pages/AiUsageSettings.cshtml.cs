using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AiUsageSettingsModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly IFeatureAccessService _features;

    public AiUsageSettingsModel(VectorDbContext db, CurrentUserService currentUser, IFeatureAccessService features)
    {
        _db = db;
        _currentUser = currentUser;
        _features = features;
    }

    [BindProperty] public bool Enabled { get; set; }
    [BindProperty] public decimal MonthlySoftLimitUsd { get; set; } = 10;
    [BindProperty] public decimal MonthlyHardLimitUsd { get; set; } = 20;
    [BindProperty] public decimal PerJobLimitUsd { get; set; } = 1;
    [BindProperty] public int MaxConcurrentJobs { get; set; } = 1;
    [BindProperty] public bool AllowHighCapabilityModel { get; set; }
    public bool PremiumAvailable { get; private set; }
    public decimal CurrentMonthUsageUsd { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? saved, CancellationToken cancellationToken)
    {
        var user = await RequireSeniorAsync(cancellationToken);
        if (user is null) return RedirectToPage("/Access");
        await LoadAsync(user, cancellationToken);
        if (saved == "true") StatusMessage = "Premium AI usage policy saved.";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var user = await RequireSeniorAsync(cancellationToken);
        if (user is null) return RedirectToPage("/Access");
        PremiumAvailable = await _features.CanUseFeatureAsync(VectorFeatures.AiImportIntelligence, cancellationToken);
        if (!PremiumAvailable)
        {
            ModelState.AddModelError(string.Empty, "Premium AI Import Intelligence requires the Premium plan.");
            await LoadUsageAsync(user.CompanyId, cancellationToken);
            return Page();
        }
        if (MonthlySoftLimitUsd < 0 || MonthlyHardLimitUsd <= 0 || MonthlySoftLimitUsd > MonthlyHardLimitUsd)
            ModelState.AddModelError(string.Empty, "Set a positive hard limit and a soft limit that does not exceed it.");
        if (PerJobLimitUsd <= 0 || PerJobLimitUsd > MonthlyHardLimitUsd)
            ModelState.AddModelError(string.Empty, "The per-job limit must be positive and no higher than the monthly hard limit.");
        if (MaxConcurrentJobs is < 1 or > 5)
            ModelState.AddModelError(string.Empty, "Concurrent jobs must be between 1 and 5.");
        if (!ModelState.IsValid)
        {
            await LoadUsageAsync(user.CompanyId, cancellationToken);
            return Page();
        }

        var policy = await _db.CompanyAiUsagePolicies.SingleOrDefaultAsync(item => item.CompanyId == user.CompanyId, cancellationToken);
        if (policy is null)
        {
            policy = new CompanyAiUsagePolicy { CompanyId = user.CompanyId };
            _db.CompanyAiUsagePolicies.Add(policy);
        }
        policy.EnabledFeaturesJson = JsonSerializer.Serialize(Enabled
            ? new[] { PremiumAiImportService.FeatureKey }
            : Array.Empty<string>());
        policy.MonthlySoftLimitUsd = MonthlySoftLimitUsd;
        policy.MonthlyHardLimitUsd = MonthlyHardLimitUsd;
        policy.PerJobLimitUsd = PerJobLimitUsd;
        policy.MaxConcurrentJobs = MaxConcurrentJobs;
        policy.AllowHighCapabilityModel = AllowHighCapabilityModel;
        policy.ChangedByUserId = user.Id;
        policy.ChangedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = user.CompanyId,
            AppUserId = user.Id,
            Action = "Premium AI usage policy updated",
            EntityType = nameof(CompanyAiUsagePolicy),
            Details = $"AI import enabled: {Enabled}; monthly hard limit USD {MonthlyHardLimitUsd:0.00}; per-job limit USD {PerJobLimitUsd:0.00}; concurrency {MaxConcurrentJobs}.",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return RedirectToPage(new { saved = "true" });
    }

    private async Task<AppUser?> RequireSeniorAsync(CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetCurrentUserAsync();
        return user is not null && CurrentUserService.IsSeniorAccessRole(user.AppRole?.Name) ? user : null;
    }

    private async Task LoadAsync(AppUser user, CancellationToken cancellationToken)
    {
        PremiumAvailable = await _features.CanUseFeatureAsync(VectorFeatures.AiImportIntelligence, cancellationToken);
        var policy = await _db.CompanyAiUsagePolicies.AsNoTracking().SingleOrDefaultAsync(item => item.CompanyId == user.CompanyId, cancellationToken);
        if (policy is not null)
        {
            Enabled = policy.EnabledFeaturesJson.Contains(PremiumAiImportService.FeatureKey, StringComparison.Ordinal);
            MonthlySoftLimitUsd = policy.MonthlySoftLimitUsd;
            MonthlyHardLimitUsd = policy.MonthlyHardLimitUsd;
            PerJobLimitUsd = policy.PerJobLimitUsd;
            MaxConcurrentJobs = policy.MaxConcurrentJobs;
            AllowHighCapabilityModel = policy.AllowHighCapabilityModel;
        }
        await LoadUsageAsync(user.CompanyId, cancellationToken);
    }

    private async Task LoadUsageAsync(int companyId, CancellationToken cancellationToken)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var costs = await _db.AiUsageLedgers.AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.RecordedAtUtc >= start)
            .Select(item => item.EstimatedCostUsd)
            .ToListAsync(cancellationToken);
        CurrentMonthUsageUsd = costs.Sum();
    }
}
