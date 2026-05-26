using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class TaskNotificationCountModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public TaskNotificationCountModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUserId = _currentUser.CurrentUserId;
        if (!currentUserId.HasValue)
        {
            return new JsonResult(new { count = 0 });
        }

        var count = await _db.TaskItems.CountAsync(t => t.AssignedToUserId == currentUserId.Value && t.Status == "Open");
        return new JsonResult(new { count });
    }
}
