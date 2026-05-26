using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;

namespace vector_app_local.Pages;

public class TaskActionModel : PageModel
{
    public TaskActionDetails? TaskDetails { get; set; }

    public async Task OnGetAsync(int? taskId)
    {
        if (!taskId.HasValue)
        {
            return;
        }

        var db = HttpContext.RequestServices.GetRequiredService<VectorDbContext>();

        TaskDetails = await db.TaskItems
            .Include(t => t.AssignedByUser)
            .Include(t => t.AssignedToUser)
            .Where(t => t.Id == taskId.Value && t.Status == "Open")
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
