using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class TaskInboxModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public TaskInboxModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public List<TaskInboxItem> OpenTasks { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync(string? confirmation)
    {
        SuccessMessage = confirmation switch
        {
            "feedback-submitted" => "Feedback submitted. The task is now marked complete and recorded.",
            "task-not-found" => "That task is no longer open for the signed-in user.",
            _ => TempData["SuccessMessage"] as string
        };

        await LoadTasksAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int taskId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        var task = await _db.TaskItems
            .Include(t => t.Company)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.AssignedToUserId == currentUser.Id && t.Status == "Open");

        if (task is null)
        {
            return RedirectToPage(new { confirmation = "task-not-found" });
        }

        var now = DateTime.UtcNow;
        task.Status = "Deleted";
        task.DeletedAtUtc = now;

        _db.TaskEvents.Add(new TaskEvent
        {
            TaskItemId = task.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Deleted",
            Notes = "Task removed from inbox by assigned user.",
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = task.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Task deleted",
            EntityType = "TaskItem",
            EntityId = task.Id,
            Details = $"Assigned user deleted task #{task.Id} from inbox.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Task removed from inbox and logged.";
        return RedirectToPage();
    }

    private async Task LoadTasksAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            OpenTasks = new List<TaskInboxItem>();
            return;
        }

        OpenTasks = await _db.TaskItems
            .Include(t => t.AssignedByUser)
            .Include(t => t.AssignedToUser)
            .Where(t => t.AssignedToUserId == currentUser.Id && t.Status == "Open")
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TaskInboxItem
            {
                Id = t.Id,
                ActionType = t.ActionType,
                AssignedByName = t.AssignedByUser == null ? "Manager" : t.AssignedByUser.FullName,
                AssignedToName = t.AssignedToUser == null ? "Current user" : t.AssignedToUser.FullName,
                InstructionMessage = t.InstructionMessage,
                CreatedAtUtc = t.CreatedAtUtc,
                ExpiresAtUtc = t.ExpiresAtUtc
            })
            .ToListAsync();
    }

    public class TaskInboxItem
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string AssignedByName { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public string? InstructionMessage { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
    }
}
