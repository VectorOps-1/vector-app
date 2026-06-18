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
            var assignedAreaNames = await LoadAssignedAreaNamesAsync(currentUser);
            ScopeLabel = assignedAreaNames.Count == 0
                ? "Assigned to you"
                : $"Assigned to you or areas: {string.Join(", ", assignedAreaNames)}";

            issues = issues
                .Where(issue =>
                    issue.Status == "Open" &&
                    (issue.AssignedToUserId == currentUser.Id || IssueMatchesAreaScope(issue, assignedAreaNames)))
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

    private async Task<List<string>> LoadAssignedAreaNamesAsync(AppUser currentUser)
    {
        return await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Include(assignment => assignment.OperationalArea)
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active" &&
                assignment.OperationalArea != null &&
                assignment.OperationalArea.Status == "Active")
            .Select(assignment => assignment.OperationalArea!.Name)
            .ToListAsync();
    }

    private static bool IssueMatchesAreaScope(IssueReport issue, IReadOnlyList<string> assignedAreaNames)
    {
        return assignedAreaNames.Any(areaName =>
            ContainsAny(areaName, issue.Location, issue.Description, issue.RelatedItem));
    }

    private static bool ContainsAny(string search, params string?[] values)
    {
        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

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
