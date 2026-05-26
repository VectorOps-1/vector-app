using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;

namespace vector_app_local.Pages;

public class TaskNotificationCountModel : PageModel
{
    private readonly VectorDbContext _db;

    public TaskNotificationCountModel(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnGetAsync(int userId = 1)
    {
        var count = await _db.TaskItems.CountAsync(t => t.AssignedToUserId == userId && t.Status == "Open");
        return new JsonResult(new { count });
    }
}
