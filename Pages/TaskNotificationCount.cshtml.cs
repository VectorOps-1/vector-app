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
            return new JsonResult(new { count = 0, url = "/TaskInbox" });
        }

        var openTasks = await _db.TaskItems
            .AsNoTracking()
            .Where(t => t.AssignedToUserId == currentUserId.Value && t.Status == "Open")
            .OrderByDescending(t => t.ActionType == "Checklist approval request")
            .ThenByDescending(t => t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id,
                t.ActionType,
                t.RelatedItemReference
            })
            .ToListAsync();

        var task = openTasks.FirstOrDefault();
        var url = task is null
            ? "/TaskInbox"
            : TaskActionCatalog.BuildTaskUrl(task.ActionType, task.Id, task.RelatedItemReference);
        var label = task?.ActionType == "Checklist approval request"
            ? "Open checklist approval request"
            : "Open assigned tasks";

        return new JsonResult(new { count = openTasks.Count, url, label });
    }
}
