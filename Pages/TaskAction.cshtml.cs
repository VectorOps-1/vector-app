using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class TaskActionModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public TaskActionModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public TaskActionDetails? TaskDetails { get; set; }

    public async Task<IActionResult> OnGetAsync(int? taskId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        if (!taskId.HasValue)
        {
            return Page();
        }

        TaskDetails = await _db.TaskItems
            .Include(t => t.AssignedByUser)
            .Include(t => t.AssignedToUser)
            .Where(t => t.Id == taskId.Value && t.AssignedToUserId == currentUser.Id && t.Status == "Open")
            .Select(t => new TaskActionDetails
            {
                Id = t.Id,
                ActionType = t.ActionType,
                InstructionMessage = t.InstructionMessage,
                AssignedByName = t.AssignedByUser == null ? "Manager" : t.AssignedByUser.FullName,
                AssignedToName = t.AssignedToUser == null ? "Current user" : t.AssignedToUser.FullName,
                ExpiresAtUtc = t.ExpiresAtUtc
            })
            .FirstOrDefaultAsync();

        if (TaskDetails is null)
        {
            return RedirectToPage("/TaskInbox", new { confirmation = "task-not-found" });
        }

        return Page();
    }

    public class TaskActionDetails
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? InstructionMessage { get; set; }
        public string AssignedByName { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public DateTime? ExpiresAtUtc { get; set; }
    }
}
