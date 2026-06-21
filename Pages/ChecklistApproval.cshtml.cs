using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ChecklistApprovalModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ChecklistApprovalModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty]
    public int TaskId { get; set; }

    [BindProperty]
    public string? ReturnNote { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public TaskItem? ApprovalTask { get; private set; }
    public ChecklistTemplate? Template { get; private set; }

    public string SubmittedBy => ApprovalTask?.AssignedByUser?.FullName ?? "Operational manager";
    public string ApproverName => ApprovalTask?.AssignedToUser?.FullName ?? "Senior manager";
    public string CrewViewUrl => Template is null ? "#" : $"/ChecklistTemplateView?templateId={Template.Id}&taskAccess=true&taskId={TaskId}";
    public string PublishUrl => Template is null ? "#" : $"/EditChecklist?view=register&publishTemplateId={Template.Id}&taskAccess=true&taskId={TaskId}";

    public async Task<IActionResult> OnGetAsync(int taskId)
    {
        TaskId = taskId;
        return await LoadReviewAsync(markOpened: true);
    }

    public async Task<IActionResult> OnPostReturnAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (string.IsNullOrWhiteSpace(ReturnNote))
        {
            StatusMessage = "Enter a reason before sending this checklist back for modification.";
            return await LoadReviewAsync(markOpened: false);
        }

        var task = await LoadApprovalTaskAsync(currentUser, TaskId);
        if (task is null)
        {
            StatusMessage = "Checklist approval task not found or already closed.";
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        if (!TaskActionCatalog.TryParseChecklistTemplateId(task.RelatedItemReference, out var templateId))
        {
            StatusMessage = "Checklist approval task is missing its checklist reference.";
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        var template = await _db.ChecklistTemplates
            .FirstOrDefaultAsync(item =>
                item.Id == templateId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (template is null)
        {
            StatusMessage = "Checklist template not found.";
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        var now = DateTime.UtcNow;
        template.Status = "Changes Requested";
        template.IsPublished = false;
        template.UpdatedAtUtc = now;

        task.Status = "Completed";
        task.CompletedAtUtc = now;

        _db.TaskEvents.Add(new TaskEvent
        {
            TaskItemId = task.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Returned",
            Notes = ReturnNote.Trim(),
            CreatedAtUtc = now
        });

        _db.TaskItems.Add(new TaskItem
        {
            CompanyId = currentUser.CompanyId,
            AssignedToUserId = task.AssignedByUserId,
            AssignedByUserId = currentUser.Id,
            ActionType = "Checklist modification request",
            RelatedItemReference = $"ChecklistTemplate:{template.Id}",
            InstructionMessage = $"{currentUser.FullName} sent '{template.Name}' back for modification. Notes: {ReturnNote.Trim()}",
            Status = "Open",
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Checklist approval returned",
            EntityType = "ChecklistTemplate",
            EntityId = template.Id,
            Details = $"{currentUser.FullName} returned checklist template #{template.Id} to {task.AssignedByUser?.FullName ?? "the submitter"} for modification.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        StatusMessage = "Checklist sent back for modification. The register now shows Changes Requested.";
        return RedirectToPage("/EditChecklist", new { view = "register" });
    }

    private async Task<IActionResult> LoadReviewAsync(bool markOpened)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var task = await LoadApprovalTaskAsync(currentUser, TaskId);
        if (task is null)
        {
            StatusMessage = "Checklist approval task not found or already closed.";
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        if (!TaskActionCatalog.TryParseChecklistTemplateId(task.RelatedItemReference, out var templateId))
        {
            StatusMessage = "Checklist approval task is missing its checklist reference.";
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        var template = await _db.ChecklistTemplates
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .FirstOrDefaultAsync(item =>
                item.Id == templateId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (template is null)
        {
            StatusMessage = "Checklist template not found.";
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        if (markOpened && task.OpenedAtUtc is null)
        {
            task.OpenedAtUtc = DateTime.UtcNow;
            _db.TaskEvents.Add(new TaskEvent
            {
                TaskItemId = task.Id,
                PerformedByUserId = currentUser.Id,
                EventType = "Opened",
                Notes = "Senior manager opened the checklist approval request.",
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        ApprovalTask = task;
        Template = template;
        return Page();
    }

    private async Task<TaskItem?> LoadApprovalTaskAsync(AppUser currentUser, int taskId)
    {
        if (!CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return null;
        }

        return await _db.TaskItems
            .Include(item => item.AssignedByUser)
            .Include(item => item.AssignedToUser)
            .FirstOrDefaultAsync(item =>
                item.Id == taskId &&
                item.CompanyId == currentUser.CompanyId &&
                item.AssignedToUserId == currentUser.Id &&
                item.Status == "Open" &&
                item.ActionType == "Checklist approval request");
    }
}
