using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Pages;

public class TaskInboxModel : PageModel
{
    private const int PrototypeCurrentUserId = 1;
    private readonly VectorDbContext _db;

    public TaskInboxModel(VectorDbContext db)
    {
        _db = db;
    }

    public List<TaskInboxItem> OpenTasks { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        await LoadTasksAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int taskId)
    {
        var task = await _db.TaskItems
            .Include(t => t.Company)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.AssignedToUserId == PrototypeCurrentUserId);

        if (task is null)
        {
            TempData["SuccessMessage"] = "Task was not found.";
            return RedirectToPage();
        }

        var now = DateTime.UtcNow;
        task.Status = "Deleted";
        task.DeletedAtUtc = now;

        _db.TaskEvents.Add(new TaskEvent
        {
            TaskItemId = task.Id,
            PerformedByUserId = PrototypeCurrentUserId,
            EventType = "Deleted",
            Notes = "Task removed from inbox by assigned user.",
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = task.CompanyId,
            AppUserId = PrototypeCurrentUserId,
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
        OpenTasks = await _db.TaskItems
            .Include(t => t.AssignedBy)
            .Include(t => t.AssignedToUser)
            .Where(t => t.AssignedToUserId == PrototypeCurrentUserId && t.Status == "Open")
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TaskInboxItem
            {
                Id = t.Id,
                ActionType = t.ActionType,
                AssignedByName = t.AssignedBy == null ? "Manager" : t.AssignedBy.FullName,
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
