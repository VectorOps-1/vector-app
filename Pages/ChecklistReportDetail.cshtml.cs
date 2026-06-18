using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ChecklistReportDetailModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ChecklistReportDetailModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public DailyVehicleReadinessReport? Report { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CurrentUserService.CanSendTasks(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        var report = await LoadFullReportAsync(currentUser.CompanyId, Id);
        if (report is null)
        {
            return NotFound();
        }

        if (!await CanAccessReportAsync(currentUser, report))
        {
            return NotFound();
        }

        Report = report;
        RecordedAtUtc = report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc;
        return Page();
    }

    public string Text(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "N/A" : value;
    }

    public string FormatDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd") : "N/A";
    }

    public string FormatDateTime(DateTime? value)
    {
        return value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "N/A";
    }

    public string YesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    public string ReadinessStatusClass(string? readinessStatus)
    {
        if (string.Equals(readinessStatus, "Operational", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(readinessStatus, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            return "status-ok";
        }

        if (string.Equals(readinessStatus, "Not ready", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(readinessStatus, "Critical", StringComparison.OrdinalIgnoreCase))
        {
            return "status-danger";
        }

        return "status-warning";
    }

    public string EquipmentStatusClass(DailyVehicleEquipmentCheck check)
    {
        return check.IsOperational &&
            string.Equals(check.ReadinessImpact, "None", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(check.IssueNotes)
                ? "status-ok"
                : "status-danger";
    }

    private async Task<DailyVehicleReadinessReport?> LoadFullReportAsync(int companyId, int reportId)
    {
        return await _db.DailyVehicleReadinessReports
            .AsNoTracking()
            .AsSplitQuery()
            .Include(report => report.Company)
            .Include(report => report.Vehicle)
                .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
            .Include(report => report.PerformedByUser)
                .ThenInclude(user => user!.AppRole)
            .Include(report => report.PerformedByUser)
                .ThenInclude(user => user!.AssignedOperationalArea)
            .Include(report => report.ChecklistTemplate)
            .Include(report => report.EquipmentChecks)
            .FirstOrDefaultAsync(report =>
                report.CompanyId == companyId &&
                report.Id == reportId &&
                report.WorkflowStatus != "Deleted");
    }

    private async Task<bool> CanAccessReportAsync(AppUser currentUser, DailyVehicleReadinessReport report)
    {
        if (CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return true;
        }

        if (!string.Equals(currentUser.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var vehicleAreaId = report.Vehicle?.CurrentOperationalAreaId;
        if (!vehicleAreaId.HasValue)
        {
            return false;
        }

        return await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .AnyAsync(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.OperationalAreaId == vehicleAreaId.Value &&
                assignment.Status == "Active");
    }
}
