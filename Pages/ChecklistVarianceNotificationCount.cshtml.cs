using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ChecklistVarianceNotificationCountModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ChecklistVarianceNotificationCountModel(VectorDbContext db, CurrentUserService currentUser)
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

        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var count = await _db.ChecklistVarianceAlerts.CountAsync(alert =>
            alert.CompanyId == currentUser.CompanyId &&
            alert.Status == "Open" &&
            (alert.AssignedToUserId == currentUser.Id || isSenior));

        return new JsonResult(new { count });
    }
}
