using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
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

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        IsSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);

        var issueQuery = _db.IssueReports
            .Include(issue => issue.ReportedByUser)
            .Include(issue => issue.AssignedToUser)
            .Where(issue => issue.CompanyId == currentUser.CompanyId);

        if (!IsSeniorOverview)
        {
            issueQuery = issueQuery.Where(issue => issue.Status == "Open");
        }

        IssueReports = await issueQuery
            .OrderByDescending(issue => issue.CreatedAtUtc)
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
                ResolvedAtUtc = issue.ResolvedAtUtc
            })
            .ToListAsync();

        return Page();
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
    }
}
