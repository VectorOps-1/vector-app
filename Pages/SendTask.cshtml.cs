using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class SendTaskModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public SendTaskModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
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
        var assignedBy = await _currentUser.GetCurrentUserAsync();
        if (assignedBy is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CurrentUserService.CanSendTasks(assignedBy.AppRole?.Name))
        {
            ModelState.AddModelError(string.Empty, "The signed-in user is not authorised to send tasks.");
        }

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

        var recipientExists = await _db.AppUsers.AnyAsync(user =>
            user.Id == AssignedToUserId &&
            user.CompanyId == assignedBy.CompanyId &&
            user.Status == "Active");
        if (!recipientExists)
        {
            ModelState.AddModelError(nameof(AssignedToUserId), "Select an active recipient in the signed-in user's company.");
            return Page();
        }

        var status = string.Equals(submitAction, "draft", StringComparison.OrdinalIgnoreCase) ? "Draft" : "Open";
        var now = DateTime.UtcNow;

        var task = new TaskItem
        {
            CompanyId = assignedBy.CompanyId,
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
            CompanyId = assignedBy.CompanyId,
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
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            Recipients = new List<SelectListItem>();
            return;
        }

        Recipients = await _db.AppUsers
            .Include(u => u.AppRole)
            .Where(u => u.CompanyId == currentUser.CompanyId && u.Status == "Active")
            .OrderBy(u => u.FullName)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.AppRole == null ? u.FullName : $"{u.FullName} ({u.AppRole.Name})"
            })
            .ToListAsync();
    }
}
