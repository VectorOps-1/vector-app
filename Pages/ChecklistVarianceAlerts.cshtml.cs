using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ChecklistVarianceAlertsModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ChecklistVarianceService _varianceService;

    public ChecklistVarianceAlertsModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ChecklistVarianceService varianceService)
    {
        _db = db;
        _currentUser = currentUser;
        _varianceService = varianceService;
    }

    public List<VarianceAlertItem> Alerts { get; private set; } = new();
    public string? SuccessMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        SuccessMessage = TempData["SuccessMessage"] as string;
        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);

        Alerts = await _db.ChecklistVarianceAlerts
            .AsNoTracking()
            .Include(alert => alert.DetectedForUser)
            .Include(alert => alert.DailyVehicleReadinessReport)
                .ThenInclude(report => report!.Vehicle)
                    .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
            .Where(alert =>
                alert.CompanyId == currentUser.CompanyId &&
                alert.Status == "Open" &&
                (alert.AssignedToUserId == currentUser.Id || isSenior))
            .Select(alert => new VarianceAlertItem
            {
                Id = alert.Id,
                ReportId = alert.DailyVehicleReadinessReportId,
                AssetLabel = alert.AssetLabel ?? "Equipment item",
                FieldKey = alert.FieldKey,
                SourceDateUtc = alert.DailyVehicleReadinessReport == null
                    ? alert.CreatedAtUtc
                    : alert.DailyVehicleReadinessReport.SubmittedAtUtc ??
                        alert.DailyVehicleReadinessReport.LastSavedAtUtc ??
                        alert.DailyVehicleReadinessReport.CreatedAtUtc,
                Callsign = alert.DailyVehicleReadinessReport == null
                    ? "No callsign"
                    : alert.DailyVehicleReadinessReport.CallsignAtCheck,
                RegistrationNumber = alert.DailyVehicleReadinessReport == null
                    ? "No registration"
                    : alert.DailyVehicleReadinessReport.VehicleRegistrationNumber,
                AreaName = alert.DailyVehicleReadinessReport == null ||
                    alert.DailyVehicleReadinessReport.Vehicle == null ||
                    alert.DailyVehicleReadinessReport.Vehicle.CurrentOperationalArea == null
                        ? "Unallocated"
                        : alert.DailyVehicleReadinessReport.Vehicle.CurrentOperationalArea.Name,
                DetectedForName = alert.DetectedForUser == null ? "Crew member" : alert.DetectedForUser.FullName,
                PreviousValue = alert.PreviousValue,
                NewValue = alert.NewValue,
                RegisterValue = alert.RegisterValue,
                RequiresRegisterUpdate = alert.RequiresRegisterUpdate,
                CreatedAtUtc = alert.CreatedAtUtc
            })
            .ToListAsync();

        Alerts = Alerts
            .OrderByDescending(alert => alert.SourceDateUtc.Date)
            .ThenBy(alert => alert.Callsign)
            .ThenBy(alert => alert.AssetLabel)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostReviewAsync(int alertId, string decision, string? reviewNote)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var approved = string.Equals(decision, "approve", StringComparison.OrdinalIgnoreCase);
        var reviewed = await _varianceService.ReviewVarianceAlertAsync(alertId, currentUser, approved, reviewNote);
        TempData["SuccessMessage"] = reviewed
            ? approved ? "Checklist variance approved and logged." : "Checklist variance rejected and logged."
            : "That checklist variance alert is no longer open or available to you.";

        return RedirectToPage();
    }

    public class VarianceAlertItem
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public string AssetLabel { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public DateTime SourceDateUtc { get; set; }
        public string DetectedForName { get; set; } = string.Empty;
        public string? PreviousValue { get; set; }
        public string? NewValue { get; set; }
        public string? RegisterValue { get; set; }
        public bool RequiresRegisterUpdate { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
