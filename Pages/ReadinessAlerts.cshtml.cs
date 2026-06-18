using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ReadinessAlertsModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ReadinessAlertService _readinessAlertService;

    public ReadinessAlertsModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ReadinessAlertService readinessAlertService)
    {
        _db = db;
        _currentUser = currentUser;
        _readinessAlertService = readinessAlertService;
    }

    public List<ReadinessAlertRow> Alerts { get; private set; } = [];
    public string? SuccessMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CanUseReadinessAlerts(currentUser))
        {
            return RedirectToPage("/Home");
        }

        SuccessMessage = TempData["SuccessMessage"] as string;
        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);

        Alerts = await _db.ReadinessAlerts
            .AsNoTracking()
            .Include(alert => alert.TriggeredByUser)
            .Include(alert => alert.AssignedToUser)
            .Where(alert =>
                alert.CompanyId == currentUser.CompanyId &&
                (alert.Status == ReadinessAlertStatuses.Open || alert.Status == ReadinessAlertStatuses.Acknowledged) &&
                (isSenior || alert.AssignedToUserId == currentUser.Id))
            .OrderBy(alert => alert.Status == ReadinessAlertStatuses.Open ? 0 : 1)
            .ThenByDescending(alert => alert.CreatedAtUtc)
            .Select(alert => new ReadinessAlertRow
            {
                Id = alert.Id,
                Status = alert.Status,
                VehicleId = alert.VehicleId,
                ReportId = alert.DailyVehicleReadinessReportId,
                VehicleLabel = alert.VehicleLabel,
                AssetType = alert.AssetType,
                ItemName = alert.ItemName,
                FieldKey = alert.FieldKey ?? "Rule",
                TriggerValue = alert.TriggerValue,
                SourceValue = alert.SourceValue,
                Severity = alert.Severity,
                ImpactPercent = alert.ImpactPercent,
                IsHardBlocker = alert.IsHardBlocker,
                AlertSummary = alert.AlertSummary,
                TriggeredByName = alert.TriggeredByUser == null ? "Crew member" : alert.TriggeredByUser.FullName,
                AssignedToName = alert.AssignedToUser == null ? "Senior management" : alert.AssignedToUser.FullName,
                CreatedAtUtc = alert.CreatedAtUtc
            })
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync(int alertId, string? reviewNote)
    {
        var currentUser = await LoadCurrentReviewerAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var updated = await _readinessAlertService.AcknowledgeAlertAsync(alertId, currentUser, reviewNote);
        TempData["SuccessMessage"] = updated
            ? "Readiness alert acknowledged."
            : "That readiness alert is no longer open or available to you.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveAsync(int alertId, string? reviewNote)
    {
        var currentUser = await LoadCurrentReviewerAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var updated = await _readinessAlertService.ResolveAlertAsync(alertId, currentUser, reviewNote);
        TempData["SuccessMessage"] = updated
            ? "Readiness alert resolved."
            : "That readiness alert is no longer open or available to you.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int alertId, string? reviewNote)
    {
        var currentUser = await LoadCurrentReviewerAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var updated = await _readinessAlertService.DeleteAlertAsync(alertId, currentUser, reviewNote);
        TempData["SuccessMessage"] = updated
            ? "Readiness alert deleted."
            : "That readiness alert is no longer open or available to you.";

        return RedirectToPage();
    }

    private async Task<AppUser?> LoadCurrentReviewerAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        return currentUser is not null && CanUseReadinessAlerts(currentUser) ? currentUser : null;
    }

    private static bool CanUseReadinessAlerts(AppUser user)
    {
        return string.Equals(user.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase) ||
            CurrentUserService.IsSeniorAccessRole(user.AppRole?.Name);
    }

    public sealed class ReadinessAlertRow
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public int ReportId { get; set; }
        public string VehicleLabel { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public string TriggerValue { get; set; } = string.Empty;
        public string? SourceValue { get; set; }
        public string Severity { get; set; } = string.Empty;
        public int ImpactPercent { get; set; }
        public bool IsHardBlocker { get; set; }
        public string AlertSummary { get; set; } = string.Empty;
        public string TriggeredByName { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string SourceUrl => $"/ReadinessMetricDetail?metric=equipment-alerts&vehicleId={VehicleId}&reportId={ReportId}";
    }
}
