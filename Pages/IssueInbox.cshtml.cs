using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class IssueInboxModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public IssueInboxModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public List<IssueInboxItem> OpenIssues { get; private set; } = new();
    public string? SuccessMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? confirmation)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        SuccessMessage = confirmation switch
        {
            "issue-resolved" => "Issue resolved and recorded.",
            "issue-not-found" => "That issue is no longer open or assigned to you.",
            _ => TempData["SuccessMessage"] as string
        };

        OpenIssues = await _db.IssueReports
            .Include(issue => issue.ReportedByUser)
            .Where(issue =>
                issue.CompanyId == currentUser.CompanyId &&
                issue.AssignedToUserId == currentUser.Id &&
                issue.Status == "Open")
            .OrderByDescending(issue => issue.CreatedAtUtc)
            .Select(issue => new IssueInboxItem
            {
                Id = issue.Id,
                Module = issue.Module,
                IssueType = issue.IssueType,
                Severity = issue.Severity,
                RelatedItem = issue.RelatedItem,
                Location = issue.Location,
                ReportedByName = issue.ReportedByUser == null ? "Reporter" : issue.ReportedByUser.FullName,
                NotificationMethod = issue.NotificationMethod,
                Description = issue.Description,
                CreatedAtUtc = issue.CreatedAtUtc
            })
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int issueId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var issue = await _db.IssueReports
            .FirstOrDefaultAsync(report =>
                report.Id == issueId &&
                report.CompanyId == currentUser.CompanyId &&
                report.AssignedToUserId == currentUser.Id &&
                report.Status == "Open");

        if (issue is null)
        {
            return RedirectToPage(new { confirmation = "issue-not-found" });
        }

        var now = DateTime.UtcNow;
        issue.Status = "Deleted";

        _db.IssueReportEvents.Add(new IssueReportEvent
        {
            IssueReportId = issue.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Deleted",
            Notes = "Issue removed from inbox by assigned user.",
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = issue.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Issue deleted",
            EntityType = "IssueReport",
            EntityId = issue.Id,
            Details = $"Assigned user deleted issue #{issue.Id} from inbox.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Issue removed from inbox and logged.";
        return RedirectToPage();
    }

    public class IssueInboxItem
    {
        public int Id { get; set; }
        public string Module { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string? Severity { get; set; }
        public string? RelatedItem { get; set; }
        public string? Location { get; set; }
        public string ReportedByName { get; set; } = string.Empty;
        public string NotificationMethod { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
