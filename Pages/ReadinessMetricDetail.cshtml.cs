using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ReadinessMetricDetailModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ReadinessMetricDetailModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? Metric { get; set; }
    [BindProperty(SupportsGet = true)] public int? OperationalAreaId { get; set; }
    [BindProperty(SupportsGet = true)] public int VehicleId { get; set; }
    [BindProperty(SupportsGet = true)] public int? ReportId { get; set; }
    [BindProperty(SupportsGet = true)] public int? IssueId { get; set; }

    public string PageTitle { get; private set; } = "Readiness Source";
    public string BackLabel { get; private set; } = "Readiness Metric";
    public ReadinessSourceDetail? Source { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CanViewReadinessMetricDetail(currentUser))
        {
            return RedirectToPage("/Home");
        }

        var vehicle = await _db.Vehicles
            .AsNoTracking()
            .Include(item => item.CurrentOperationalArea)
            .FirstOrDefaultAsync(item => item.Id == VehicleId && item.CompanyId == currentUser.CompanyId);

        if (vehicle is null || !await CanAccessVehicleAsync(currentUser, vehicle))
        {
            return Page();
        }

        PageTitle = $"{vehicle.Callsign} Readiness Source";
        BackLabel = Metric switch
        {
            "ready-vehicles" => "Ready Vehicles",
            "checked-this-shift" => "Checked This Shift",
            "missing-checks" => "Missing Checks",
            "not-ready" => "Not Ready",
            "equipment-alerts" => "Equipment Alerts",
            "open-issues" => "Open Issues",
            _ => "Readiness Metric"
        };

        Source = new ReadinessSourceDetail
        {
            Callsign = string.IsNullOrWhiteSpace(vehicle.Callsign) ? vehicle.RegistrationNumber : vehicle.Callsign,
            RegistrationNumber = vehicle.RegistrationNumber,
            VehicleType = vehicle.VehicleType,
            AreaName = vehicle.CurrentOperationalArea?.Name ?? "Unallocated",
            AssignedManager = await LoadAssignedManagerAsync(currentUser.CompanyId, vehicle.CurrentOperationalAreaId)
        };

        if (IssueId.HasValue)
        {
            Source.Issue = await LoadIssueAsync(currentUser.CompanyId, IssueId.Value);
            Source.TriggerType = Source.Issue is null ? "Issue source unavailable" : "Open issue";
        }
        else if (ReportId.HasValue)
        {
            Source.Report = await LoadReportAsync(currentUser.CompanyId, ReportId.Value);
            Source.TriggerType = Source.Report is null ? "Checklist source unavailable" : "Checklist result";
        }
        else
        {
            Source.TriggerType = "Missing checklist";
        }

        return Page();
    }

    private async Task<bool> CanAccessVehicleAsync(AppUser currentUser, Vehicle vehicle)
    {
        if (CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return true;
        }

        if (!vehicle.CurrentOperationalAreaId.HasValue)
        {
            return false;
        }

        return await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .AnyAsync(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.OperationalAreaId == vehicle.CurrentOperationalAreaId.Value &&
                assignment.Status == "Active");
    }

    private static bool CanViewReadinessMetricDetail(AppUser user)
    {
        return string.Equals(user.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase) ||
            CurrentUserService.IsSeniorAccessRole(user.AppRole?.Name);
    }

    private async Task<string> LoadAssignedManagerAsync(int companyId, int? areaId)
    {
        if (!areaId.HasValue)
        {
            return "No manager assigned";
        }

        var managers = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Include(assignment => assignment.ManagerUser)
            .Where(assignment =>
                assignment.CompanyId == companyId &&
                assignment.OperationalAreaId == areaId.Value &&
                assignment.Status == "Active" &&
                assignment.ManagerUser != null &&
                assignment.ManagerUser.Status == "Active")
            .OrderBy(assignment => assignment.ManagerUser!.FullName)
            .Select(assignment => assignment.ManagerUser!.FullName)
            .ToListAsync();

        return managers.Count == 0 ? "No manager assigned" : string.Join(", ", managers.Distinct());
    }

    private async Task<IssueSourceDetail?> LoadIssueAsync(int companyId, int issueId)
    {
        var issue = await _db.IssueReports
            .AsNoTracking()
            .Include(report => report.AssignedToUser)
            .FirstOrDefaultAsync(report => report.Id == issueId && report.CompanyId == companyId && report.Status != "Deleted");

        if (issue is null)
        {
            return null;
        }

        return new IssueSourceDetail
        {
            Id = issue.Id,
            Status = issue.Status,
            IssueType = issue.IssueType,
            Severity = issue.Severity ?? "Not set",
            AssignedToName = issue.AssignedToUser?.FullName ?? "Manager",
            RelatedItem = issue.RelatedItem ?? "Not set",
            Description = issue.Description,
            CreatedAtUtc = issue.CreatedAtUtc
        };
    }

    private async Task<ChecklistSourceDetail?> LoadReportAsync(int companyId, int reportId)
    {
        var report = await _db.DailyVehicleReadinessReports
            .AsNoTracking()
            .Include(item => item.PerformedByUser)
            .Include(item => item.EquipmentChecks)
            .FirstOrDefaultAsync(item => item.Id == reportId && item.CompanyId == companyId && item.WorkflowStatus != "Deleted");

        if (report is null)
        {
            return null;
        }

        var notes = string.Join(" | ", new[]
        {
            report.OperationalNotes,
            report.SchematicNotes,
            report.GeneralNotes
        }.Where(note => !string.IsNullOrWhiteSpace(note)));

        return new ChecklistSourceDetail
        {
            WorkflowStatus = report.WorkflowStatus,
            ReadinessStatus = report.ReadinessStatus,
            PerformedByName = report.PerformedByUser?.FullName ?? "Unknown",
            RecordedAtUtc = report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc,
            CriticalIssueCount = report.CriticalIssueCount,
            WarningIssueCount = report.WarningIssueCount,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
            EquipmentRows = report.EquipmentChecks
                .OrderBy(check => check.SortOrder)
                .ThenBy(check => check.EquipmentName)
                .Select(check => new EquipmentSourceRow
                {
                    EquipmentName = check.EquipmentName,
                    SerialOrAssetId = check.SerialOrAssetId ?? "N/A",
                    BatteryStatus = check.BatteryStatus ?? "N/A",
                    IsOperational = check.IsOperational,
                    IssueNotes = string.IsNullOrWhiteSpace(check.IssueNotes) ? "None" : check.IssueNotes
                })
                .ToList()
        };
    }

    public sealed class ReadinessSourceDetail
    {
        public string Callsign { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string AssignedManager { get; set; } = string.Empty;
        public string TriggerType { get; set; } = string.Empty;
        public IssueSourceDetail? Issue { get; set; }
        public ChecklistSourceDetail? Report { get; set; }
    }

    public sealed class IssueSourceDetail
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public string RelatedItem { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class ChecklistSourceDetail
    {
        public string WorkflowStatus { get; set; } = string.Empty;
        public string ReadinessStatus { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime RecordedAtUtc { get; set; }
        public int CriticalIssueCount { get; set; }
        public int WarningIssueCount { get; set; }
        public string? Notes { get; set; }
        public List<EquipmentSourceRow> EquipmentRows { get; set; } = [];
    }

    public sealed class EquipmentSourceRow
    {
        public string EquipmentName { get; set; } = string.Empty;
        public string SerialOrAssetId { get; set; } = string.Empty;
        public string BatteryStatus { get; set; } = string.Empty;
        public bool IsOperational { get; set; }
        public string IssueNotes { get; set; } = string.Empty;
    }
}
