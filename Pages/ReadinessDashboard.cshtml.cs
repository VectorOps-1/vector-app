using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ReadinessDashboardModel : PageModel
{
    private const int ShiftHours = 12;

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ReadinessEngineScoringService _readinessScoring;

    public ReadinessDashboardModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ReadinessEngineScoringService readinessScoring)
    {
        _db = db;
        _currentUser = currentUser;
        _readinessScoring = readinessScoring;
    }

    [BindProperty(SupportsGet = true)] public int? OperationalAreaId { get; set; }

    public bool IsSeniorOverview { get; private set; }
    public bool HasAreaScope { get; private set; } = true;
    public string ScopeLabel { get; private set; } = "All bases / regions";
    public string? StatusMessage { get; private set; }
    public DateTime ShiftStartUtc { get; private set; }
    public DateTime ShiftEndUtc { get; private set; }
    public DashboardSummary Summary { get; private set; } = new();
    public List<SelectListItem> AreaOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CanViewReadinessDashboard(currentUser))
        {
            return RedirectToPage("/Home");
        }

        IsSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        ShiftEndUtc = DateTime.UtcNow;
        ShiftStartUtc = ShiftEndUtc.AddHours(-ShiftHours);

        var allowedAreas = await LoadAreaScopeAsync(currentUser);
        if (!HasAreaScope)
        {
            return Page();
        }

        await LoadDashboardAsync(currentUser.CompanyId, allowedAreas);
        return Page();
    }

    private async Task<List<AreaScopeItem>> LoadAreaScopeAsync(AppUser currentUser)
    {
        var companyAreas = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == currentUser.CompanyId && area.Status == "Active")
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.Name)
            .Select(area => new AreaScopeItem(area.Id, area.Name, area.AreaType))
            .ToListAsync();

        if (IsSeniorOverview)
        {
            AreaOptions.Add(new SelectListItem { Value = string.Empty, Text = "All bases / regions" });
            AreaOptions.AddRange(companyAreas.Select(area => new SelectListItem
            {
                Value = area.Id.ToString(),
                Text = $"{area.Name} ({area.AreaType})"
            }));

            if (OperationalAreaId.HasValue && companyAreas.All(area => area.Id != OperationalAreaId.Value))
            {
                OperationalAreaId = null;
            }

            ScopeLabel = OperationalAreaId.HasValue
                ? companyAreas.First(area => area.Id == OperationalAreaId.Value).Name
                : "All bases / regions";

            return companyAreas;
        }

        var assignedAreas = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Include(assignment => assignment.OperationalArea)
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active" &&
                assignment.OperationalArea != null &&
                assignment.OperationalArea.Status == "Active")
            .OrderBy(assignment => assignment.OperationalArea!.AreaType)
            .ThenBy(assignment => assignment.OperationalArea!.Name)
            .Select(assignment => new AreaScopeItem(
                assignment.OperationalAreaId,
                assignment.OperationalArea!.Name,
                assignment.OperationalArea.AreaType))
            .ToListAsync();

        if (assignedAreas.Count == 0)
        {
            HasAreaScope = false;
            StatusMessage = "No operational areas are assigned to this manager yet. Senior management can assign areas in Master Setup.";
            return assignedAreas;
        }

        AreaOptions = assignedAreas.Select(area => new SelectListItem
        {
            Value = area.Id.ToString(),
            Text = $"{area.Name} ({area.AreaType})"
        }).ToList();

        if (!OperationalAreaId.HasValue || assignedAreas.All(area => area.Id != OperationalAreaId.Value))
        {
            OperationalAreaId = assignedAreas[0].Id;
        }

        ScopeLabel = assignedAreas.First(area => area.Id == OperationalAreaId.Value).Name;
        return assignedAreas;
    }

    private async Task LoadDashboardAsync(int companyId, List<AreaScopeItem> allowedAreas)
    {
        var vehicleQuery = _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == companyId && vehicle.Status != "Deleted");

        if (OperationalAreaId.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(vehicle => vehicle.CurrentOperationalAreaId == OperationalAreaId.Value);
        }
        else if (!IsSeniorOverview)
        {
            var assignedAreaIds = allowedAreas.Select(area => area.Id).ToList();
            vehicleQuery = vehicleQuery.Where(vehicle =>
                vehicle.CurrentOperationalAreaId.HasValue &&
                assignedAreaIds.Contains(vehicle.CurrentOperationalAreaId.Value));
        }

        var vehicles = await vehicleQuery
            .OrderBy(vehicle => vehicle.CurrentOperationalArea == null ? "Unallocated" : vehicle.CurrentOperationalArea.Name)
            .ThenBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        var vehicleIds = vehicles.Select(vehicle => vehicle.Id).ToList();
        var recentReports = vehicleIds.Count == 0
            ? new List<DailyVehicleReadinessReport>()
            : await _db.DailyVehicleReadinessReports
                .AsNoTracking()
                .Include(report => report.PerformedByUser)
                .Include(report => report.EquipmentChecks)
                .Where(report =>
                    report.CompanyId == companyId &&
                    report.WorkflowStatus != "Deleted" &&
                    vehicleIds.Contains(report.VehicleId) &&
                    (report.SubmittedAtUtc ?? report.InspectionDateUtc) >= ShiftStartUtc)
                .ToListAsync();

        var latestReports = recentReports
            .GroupBy(report => report.VehicleId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(report => report.SubmittedAtUtc ?? report.InspectionDateUtc)
                    .ThenByDescending(report => report.Id)
                    .First());

        var openIssues = await _db.IssueReports
            .AsNoTracking()
            .Include(issue => issue.AssignedToUser)
            .Where(issue => issue.CompanyId == companyId && issue.Status == "Open")
            .OrderByDescending(issue => issue.CreatedAtUtc)
            .ToListAsync();

        if (OperationalAreaId.HasValue)
        {
            var selectedAreaName = allowedAreas.FirstOrDefault(area => area.Id == OperationalAreaId.Value)?.Name ?? string.Empty;
            var scopedVehicleLabels = vehicles.SelectMany(vehicle => new[] { vehicle.RegistrationNumber, vehicle.Callsign }).ToList();
            openIssues = openIssues
                .Where(issue => MatchesText(issue.Location, selectedAreaName) || scopedVehicleLabels.Any(label => MatchesIssue(issue, label)))
                .ToList();
        }

        var vehicleRows = vehicles
            .Select(vehicle => BuildVehicleRow(vehicle, latestReports.GetValueOrDefault(vehicle.Id), openIssues))
            .ToList();

        var engineSummary = await _readinessScoring.ScoreDashboardAsync(companyId, vehicles, latestReports, openIssues);
        Summary = new DashboardSummary
        {
            TotalVehicles = engineSummary.TotalVehicles,
            CheckedVehicles = engineSummary.CheckedVehicles,
            ReadyVehicles = engineSummary.ReadyVehicles,
            UnavailableVehicles = engineSummary.UnavailableVehicles,
            MissingChecks = engineSummary.MissingChecks,
            OpenIssues = engineSummary.OpenIssues,
            EquipmentWarnings = engineSummary.EquipmentWarnings,
            ScorePercent = engineSummary.ScorePercent,
            ScoreClass = engineSummary.ScoreClass
        };
    }

    private static VehicleReadinessRow BuildVehicleRow(
        Vehicle vehicle,
        DailyVehicleReadinessReport? report,
        List<IssueReport> openIssues)
    {
        var openIssueCount = openIssues.Count(issue =>
            MatchesIssue(issue, vehicle.RegistrationNumber) ||
            MatchesIssue(issue, vehicle.Callsign));

        var checkedThisShift = IsReadinessCheckComplete(report);
        var vehicleUnavailable = IsUnavailable(vehicle.Status);
        var reportUnavailable = report is not null &&
            (IsUnavailable(report.ReadinessStatus) || report.CriticalIssueCount > 0);
        var equipmentIssue = report is not null && HasEquipmentIssue(report);
        var warningCount = report?.WarningIssueCount ?? 0;

        var critical = !checkedThisShift || vehicleUnavailable || reportUnavailable;
        var warning = !critical && (warningCount > 0 || equipmentIssue || openIssueCount > 0 || !IsOperational(report?.ReadinessStatus));
        var readinessLabel = critical ? "Not ready" : warning ? "Attention" : "Ready";

        var issueParts = new List<string>();
        if (!checkedThisShift)
        {
            issueParts.Add("check missing");
        }
        if (vehicleUnavailable)
        {
            issueParts.Add("vehicle unavailable");
        }
        if (reportUnavailable)
        {
            issueParts.Add("critical check issue");
        }
        if (equipmentIssue)
        {
            issueParts.Add("equipment attention");
        }
        if (openIssueCount > 0)
        {
            issueParts.Add($"{openIssueCount} open issue{(openIssueCount == 1 ? string.Empty : "s")}");
        }

        return new VehicleReadinessRow
        {
            VehicleId = vehicle.Id,
            RegistrationNumber = vehicle.RegistrationNumber,
            Callsign = vehicle.Callsign,
            VehicleType = vehicle.VehicleType,
            AreaName = vehicle.CurrentOperationalArea?.Name ?? "Unallocated",
            VehicleStatus = vehicle.Status,
            CheckedThisShift = checkedThisShift,
            LastCheckAtUtc = report?.SubmittedAtUtc ?? report?.InspectionDateUtc,
            LastCheckedByName = report?.PerformedByUser?.FullName,
            ReadinessStatus = readinessLabel,
            StatusClass = critical ? "critical" : warning ? "warning" : "ready",
            EquipmentIssue = equipmentIssue,
            OpenIssueCount = openIssueCount,
            IssueSummary = issueParts.Count == 0 ? "No blocking data captured" : string.Join(", ", issueParts)
        };
    }

    private static DashboardSummary BuildSummary(List<VehicleReadinessRow> rows, int openIssueCount)
    {
        var totalVehicles = rows.Count;
        var readyVehicles = rows.Count(row => row.StatusClass == "ready");
        var checkedVehicles = rows.Count(row => row.CheckedThisShift);
        var unavailableVehicles = rows.Count(row => row.StatusClass == "critical");
        var equipmentWarnings = rows.Count(row => row.EquipmentIssue);
        var missingChecks = rows.Count(row => !row.CheckedThisShift);
        var score = totalVehicles == 0 ? 0 : (int)Math.Round((double)readyVehicles / totalVehicles * 100);

        return new DashboardSummary
        {
            TotalVehicles = totalVehicles,
            CheckedVehicles = checkedVehicles,
            ReadyVehicles = readyVehicles,
            UnavailableVehicles = unavailableVehicles,
            MissingChecks = missingChecks,
            OpenIssues = openIssueCount,
            EquipmentWarnings = equipmentWarnings,
            ScorePercent = score,
            ScoreClass = score >= 90 ? "score-green" : score >= 75 ? "score-teal" : score >= 50 ? "score-amber" : "score-red"
        };
    }

    private static bool HasEquipmentIssue(DailyVehicleReadinessReport report)
    {
        if (!report.EquipmentChecks.Any())
        {
            return true;
        }

        return report.EquipmentChecks.Any(check =>
            !check.IsOperational ||
            IsProblemStatus(check.PresentStatus, "Present") ||
            IsProblemStatus(check.BatteryStatus, "Full", "Acceptable", "Charging", "Not applicable", "N/A") ||
            IsProblemStatus(check.DamageStatus, "No damage", "None", "Good", "Not applicable", "N/A") ||
            IsProblemStatus(check.ReadinessImpact, "None"));
    }

    private static bool IsReadinessCheckComplete(DailyVehicleReadinessReport? report)
    {
        return report is not null &&
            !string.Equals(report.WorkflowStatus, "Draft", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(report.LastSavedSection, "Equipment", StringComparison.OrdinalIgnoreCase) &&
            report.EquipmentChecks.Any();
    }

    private static bool IsProblemStatus(string? value, params string[] acceptedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !acceptedValues.Any(accepted => string.Equals(value, accepted, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsOperational(string? value)
    {
        return string.Equals(value, "Operational", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnavailable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("out of service", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("inactive", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesIssue(IssueReport issue, string? needle)
    {
        return MatchesText(issue.RelatedItem, needle) ||
            MatchesText(issue.Location, needle) ||
            MatchesText(issue.Description, needle);
    }

    private static bool MatchesText(string? haystack, string? needle)
    {
        return !string.IsNullOrWhiteSpace(haystack) &&
            !string.IsNullOrWhiteSpace(needle) &&
            haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanViewReadinessDashboard(AppUser user)
    {
        return string.Equals(user.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase) ||
            CurrentUserService.IsSeniorAccessRole(user.AppRole?.Name);
    }

    private sealed record AreaScopeItem(int Id, string Name, string AreaType);

    public sealed class DashboardSummary
    {
        public int TotalVehicles { get; set; }
        public int CheckedVehicles { get; set; }
        public int ReadyVehicles { get; set; }
        public int UnavailableVehicles { get; set; }
        public int MissingChecks { get; set; }
        public int OpenIssues { get; set; }
        public int EquipmentWarnings { get; set; }
        public int ScorePercent { get; set; }
        public string ScoreClass { get; set; } = "score-red";
    }

    public sealed class VehicleReadinessRow
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string VehicleStatus { get; set; } = string.Empty;
        public bool CheckedThisShift { get; set; }
        public DateTime? LastCheckAtUtc { get; set; }
        public string? LastCheckedByName { get; set; }
        public string ReadinessStatus { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public bool EquipmentIssue { get; set; }
        public int OpenIssueCount { get; set; }
        public string IssueSummary { get; set; } = string.Empty;
    }
}
