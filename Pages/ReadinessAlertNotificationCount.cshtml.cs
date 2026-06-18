using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ReadinessAlertNotificationCountModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ReadinessAlertNotificationCountModel(VectorDbContext db, CurrentUserService currentUser)
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
        var isManager = string.Equals(currentUser.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase);
        if (!isSenior && !isManager)
        {
            return new JsonResult(new { count = 0 });
        }

        var count = await _db.ReadinessAlerts.CountAsync(alert =>
            alert.CompanyId == currentUser.CompanyId &&
            alert.Status == ReadinessAlertStatuses.Open &&
            (isSenior || alert.AssignedToUserId == currentUser.Id));

        return new JsonResult(new { count });
    }
}
