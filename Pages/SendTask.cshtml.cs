using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Pages;

public class SendTaskModel : PageModel
{
    private readonly VectorDbContext _db;

    public SendTaskModel(VectorDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public int AssignedToUserId { get; set; }

    [BindProperty]
    public string ActionType { get; set; } = "Complete Checklist";

    [BindProperty]
    public DateTime? ExpiresAtLocal { get; set; }

    [BindProperty]
    public string? InstructionMessage { get; set; }

    public List<SelectListItem> Recipients { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        await LoadRecipientsAsync();
    }

    public async Task<IActionResult> OnPostAsync(string submitAction)
    {
        await LoadRecipientsAsync();

        if (AssignedToUserId <= 0)
        {
            ModelState.AddModelError(nameof(AssignedToUserId), "Select a recipient.");
        }

        if (string.IsNullOrWhiteSpace(ActionType))
        {
            ModelState.AddModelError(nameof(ActionType), "Select a task action.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var company = await _db.Companies.OrderBy(c => c.Id).FirstAsync();
        var assignedBy = await _db.AppUsers
            .Include(u => u.AppRole)
            .Where(u => u.CompanyId == company.Id &&
                (u.AppRole!.Name == "Operational Management" || u.AppRole.Name == "Senior Management" || u.AppRole.Name == "Company Owner"))
            .OrderBy(u => u.AppRole!.Name == "Operational Management" ? 0 : 1)
            .ThenBy(u => u.Id)
            .FirstAsync();

        var status = string.Equals(submitAction, "draft", StringComparison.OrdinalIgnoreCase) ? "Draft" : "Open";
        var now = DateTime.UtcNow;

        var task = new TaskItem
        {
            CompanyId = company.Id,
            AssignedToUserId = AssignedToUserId,
            AssignedByUserId = assignedBy.Id,
            ActionType = ActionType,
            InstructionMessage = InstructionMessage,
            Status = status,
            CreatedAtUtc = now,
            ExpiresAtUtc = ExpiresAtLocal.HasValue ? DateTime.SpecifyKind(ExpiresAtLocal.Value, DateTimeKind.Local).ToUniversalTime() : null
        };

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        _db.TaskEvents.Add(new TaskEvent
        {
            TaskItemId = task.Id,
            PerformedByUserId = assignedBy.Id,
            EventType = status == "Draft" ? "DraftCreated" : "Sent",
            Notes = InstructionMessage,
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = company.Id,
            AppUserId = assignedBy.Id,
            Action = status == "Draft" ? "Task draft saved" : "Task sent",
            EntityType = "TaskItem",
            EntityId = task.Id,
            Details = $"{ActionType} assigned to user #{AssignedToUserId}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = status == "Draft"
            ? "Draft saved to the task database."
            : "Task sent and saved to the task database.";

        return RedirectToPage();
    }

    private async Task LoadRecipientsAsync()
    {
        Recipients = await _db.AppUsers
            .Include(u => u.AppRole)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.AppRole == null ? u.FullName : $"{u.FullName} ({u.AppRole.Name})"
            })
            .ToListAsync();
    }
}
