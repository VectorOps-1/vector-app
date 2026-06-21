using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class IssueNotificationCountModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public IssueNotificationCountModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return new JsonResult(new { count = 0 });
        }

        var count = await _db.IssueReports.CountAsync(issue =>
            issue.CompanyId == currentUser.CompanyId &&
            issue.AssignedToUserId == currentUser.Id &&
            issue.Status == "Open");

        return new JsonResult(new { count });
    }
}
