using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    public string ActionType { get; set; } = "Daily Vehicle & Equipment Check";

    [BindProperty]
    public DateTime? ExpiresAtLocal { get; set; }

    [BindProperty]
    public string? InstructionMessage { get; set; }

    public List<TaskRecipientOption> Recipients { get; set; } = new();
    public List<TaskActionOption> TaskActions { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        var assignedBy = await LoadFormOptionsAsync();
        if (assignedBy is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string submitAction)
    {
        var assignedBy = await LoadFormOptionsAsync();
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

        var recipient = await _db.AppUsers
            .Include(user => user.AppRole)
            .FirstOrDefaultAsync(user =>
            user.Id == AssignedToUserId &&
            user.CompanyId == assignedBy.CompanyId &&
            user.Status == "Active");
        if (recipient is null)
        {
            ModelState.AddModelError(nameof(AssignedToUserId), "Select an active recipient in the signed-in user's company.");
            return Page();
        }

        if (!TaskActionCatalog.IsAllowedForSenderAndRecipient(ActionType, _currentUser.CurrentAccessView, recipient.AppRole?.Name))
        {
            ModelState.AddModelError(nameof(ActionType), "Select an action allowed for the sender and the selected recipient's access level.");
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

    private async Task<AppUser?> LoadFormOptionsAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            Recipients = new List<TaskRecipientOption>();
            TaskActions = new List<TaskActionOption>();
            return null;
        }

        TaskActions = TaskActionCatalog
            .GetActionsForSender(_currentUser.CurrentAccessView)
            .ToList();

        var usersQuery = _db.AppUsers
            .Include(u => u.AppRole)
            .Where(u => u.CompanyId == currentUser.CompanyId && u.Status == "Active")
            .AsQueryable();

        if (!CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            var assignedAreaIds = await LoadAssignedAreaIdsAsync(currentUser);
            usersQuery = assignedAreaIds.Count == 0
                ? usersQuery.Where(user => user.Id == currentUser.Id)
                : usersQuery.Where(user =>
                    user.Id == currentUser.Id ||
                    (user.AssignedOperationalAreaId.HasValue && assignedAreaIds.Contains(user.AssignedOperationalAreaId.Value)));
        }

        var users = await usersQuery
            .OrderBy(u => u.FullName)
            .ToListAsync();

        Recipients = users
            .Select(user => new TaskRecipientOption
            {
                Id = user.Id,
                DisplayName = user.AppRole == null ? user.FullName : $"{user.FullName} ({user.AppRole.Name})",
                AccessView = TaskActionCatalog.AccessViewForRoleName(user.AppRole?.Name)
            })
            .ToList();

        return currentUser;
    }

    private async Task<List<int>> LoadAssignedAreaIdsAsync(AppUser currentUser)
    {
        var assignedAreaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync();

        if (currentUser.AssignedOperationalAreaId.HasValue &&
            !assignedAreaIds.Contains(currentUser.AssignedOperationalAreaId.Value))
        {
            assignedAreaIds.Add(currentUser.AssignedOperationalAreaId.Value);
        }

        return assignedAreaIds;
    }

    public class TaskRecipientOption
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string AccessView { get; set; } = CurrentUserService.StaffAccess;
    }
}
