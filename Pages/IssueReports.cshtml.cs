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

    public List<PooledIssueItem> PendingIssues { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        PendingIssues = await _db.IssueReports
            .Include(issue => issue.ReportedByUser)
            .Include(issue => issue.AssignedToUser)
            .Where(issue => issue.CompanyId == currentUser.CompanyId && issue.Status == "Open")
            .OrderByDescending(issue => issue.CreatedAtUtc)
            .Select(issue => new PooledIssueItem
            {
                Id = issue.Id,
                Module = issue.Module,
                IssueType = issue.IssueType,
                Severity = issue.Severity,
                RelatedItem = issue.RelatedItem,
                ReportedByName = issue.ReportedByUser == null ? "Reporter" : issue.ReportedByUser.FullName,
                AssignedToName = issue.AssignedToUser == null ? "Manager" : issue.AssignedToUser.FullName,
                CreatedAtUtc = issue.CreatedAtUtc
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
        public string ReportedByName { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
