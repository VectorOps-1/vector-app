using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class IssueReportsModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public IssueReportsModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public bool IsSeniorOverview { get; private set; }
    public List<PooledIssueItem> IssueReports { get; private set; } = new();
    public string? SuccessMessage { get; private set; }
    public string ScopeLabel { get; private set; } = "Company-wide";

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        IsSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        SuccessMessage = TempData["SuccessMessage"] as string;

        var issueQuery = _db.IssueReports
            .Include(issue => issue.ReportedByUser)
            .Include(issue => issue.AssignedToUser)
            .Where(issue => issue.CompanyId == currentUser.CompanyId && issue.Status != "Deleted");

        var issues = await issueQuery
            .OrderByDescending(issue => issue.CreatedAtUtc)
            .ToListAsync();

        if (!IsSeniorOverview)
        {
            var issueScope = await LoadAssignedIssueScopeAsync(currentUser);
            ScopeLabel = issueScope.AreaNames.Count == 0
                ? "Assigned to you"
                : $"Assigned to you or areas: {string.Join(", ", issueScope.AreaNames)}";

            issues = issues
                .Where(issue =>
                    issue.Status == "Open" &&
                    (issue.AssignedToUserId == currentUser.Id || IssueMatchesAreaScope(issue, issueScope)))
                .ToList();
        }

        IssueReports = issues
            .Select(issue => new PooledIssueItem
            {
                Id = issue.Id,
                Module = issue.Module,
                IssueType = issue.IssueType,
                Severity = issue.Severity,
                RelatedItem = issue.RelatedItem,
                Status = issue.Status,
                ResolutionOutcome = issue.ResolutionOutcome,
                ReportedByName = issue.ReportedByUser == null ? "Reporter" : issue.ReportedByUser.FullName,
                AssignedToName = issue.AssignedToUser == null ? "Manager" : issue.AssignedToUser.FullName,
                CreatedAtUtc = issue.CreatedAtUtc,
                ResolvedAtUtc = issue.ResolvedAtUtc,
                CanDelete = IsSeniorOverview || issue.AssignedToUserId == currentUser.Id
            })
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int issueId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CurrentUserService.CanSendTasks(currentUser.AppRole?.Name))
        {
            return NotFound();
        }

        var issue = await _db.IssueReports
            .FirstOrDefaultAsync(report =>
                report.Id == issueId &&
                report.CompanyId == currentUser.CompanyId &&
                report.Status != "Deleted");

        if (issue is null)
        {
            return RedirectToPage();
        }

        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        if (!isSenior && issue.AssignedToUserId != currentUser.Id)
        {
            return NotFound();
        }

        DeleteIssue(issue, currentUser);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Issue removed and logged.";
        return RedirectToPage();
    }

    private void DeleteIssue(IssueReport issue, AppUser currentUser)
    {
        var now = DateTime.UtcNow;
        issue.Status = "Deleted";

        _db.IssueReportEvents.Add(new IssueReportEvent
        {
            IssueReportId = issue.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Deleted",
            Notes = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name)
                ? "Issue removed from management issue reports by senior management."
                : "Issue removed from management issue reports by assigned manager.",
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = issue.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Issue deleted",
            EntityType = "IssueReport",
            EntityId = issue.Id,
            Details = $"Issue #{issue.Id} deleted from management issue reports.",
            CreatedAtUtc = now
        });
    }

    private async Task<IssueAreaScope> LoadAssignedIssueScopeAsync(AppUser currentUser)
    {
        var areaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync();

        if (currentUser.AssignedOperationalAreaId.HasValue &&
            !areaIds.Contains(currentUser.AssignedOperationalAreaId.Value))
        {
            areaIds.Add(currentUser.AssignedOperationalAreaId.Value);
        }

        var areaNames = areaIds.Count == 0
            ? []
            : await _db.OperationalAreas
                .AsNoTracking()
                .Where(area =>
                    area.CompanyId == currentUser.CompanyId &&
                    areaIds.Contains(area.Id) &&
                    area.Status == "Active")
                .Select(area => area.Name)
                .ToListAsync();

        var vehicleLabels = areaIds.Count == 0
            ? []
            : await _db.Vehicles
                .AsNoTracking()
                .Where(vehicle =>
                    vehicle.CompanyId == currentUser.CompanyId &&
                    vehicle.CurrentOperationalAreaId.HasValue &&
                    areaIds.Contains(vehicle.CurrentOperationalAreaId.Value) &&
                    vehicle.Status != "Deleted")
                .SelectMany(vehicle => new[] { vehicle.RegistrationNumber, vehicle.Callsign })
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct()
                .ToListAsync();

        return new IssueAreaScope(areaNames, vehicleLabels);
    }

    private static bool IssueMatchesAreaScope(IssueReport issue, IssueAreaScope scope)
    {
        return scope.AreaNames.Any(areaName => MatchesText(issue.Location, areaName)) ||
            scope.VehicleLabels.Any(vehicleLabel => MatchesText(issue.Location, vehicleLabel) || MatchesText(issue.RelatedItem, vehicleLabel));
    }

    private static bool MatchesText(string? value, string? search)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            !string.IsNullOrWhiteSpace(search) &&
            value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record IssueAreaScope(IReadOnlyList<string> AreaNames, IReadOnlyList<string> VehicleLabels);

    public class PooledIssueItem
    {
        public int Id { get; set; }
        public string Module { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string? Severity { get; set; }
        public string? RelatedItem { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ResolutionOutcome { get; set; }
        public string ReportedByName { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }
        public bool CanDelete { get; set; }
    }
}
