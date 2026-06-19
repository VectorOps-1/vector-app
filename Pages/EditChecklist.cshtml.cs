using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditChecklistModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public EditChecklistModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    public string? ViewMode { get; private set; }

    public bool CanPublishChecklist { get; private set; }

    public bool CanDeleteChecklist { get; private set; }

    public List<ChecklistTemplateSummary> SavedTemplates { get; private set; } = new();

    public async Task OnGetAsync()
    {
        ViewMode = Request.Query["view"].ToString();

        var currentUser = await _currentUser.GetCurrentUserAsync();
        var company = currentUser?.Company ?? await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return;
        }

        CanDeleteChecklist = CurrentUserService.IsSeniorAccessRole(currentUser?.AppRole?.Name);
        CanPublishChecklist = CanDeleteChecklist ||
            (currentUser is not null && await HasChecklistPublishTaskAccessAsync(currentUser));

        SavedTemplates = await _db.ChecklistTemplates
            .AsNoTracking()
            .Where(template => template.CompanyId == company.Id)
            .Where(template => template.Status != "Deleted")
            .OrderBy(template => template.TargetVehicleType)
            .ThenBy(template => template.ChecklistType)
            .ThenBy(template => template.Name)
            .ThenByDescending(template => template.IsPublished)
            .ThenByDescending(template => template.UpdatedAtUtc ?? template.CreatedAtUtc)
            .Select(template => new ChecklistTemplateSummary(
                template.Id,
                template.Name,
                template.ChecklistType,
                template.TargetVehicleType,
                template.Version,
                template.Status,
                template.IsPublished,
                template.SourceType,
                template.PublishScopeSummary,
                template.UpdatedAtUtc ?? template.CreatedAtUtc))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int templateId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (!CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            TempData["StatusMessage"] = "Only senior management can delete checklist templates.";
            return RedirectToPage(new { view = "register" });
        }

        var template = await _db.ChecklistTemplates
            .Include(item => item.PublishScopes)
            .FirstOrDefaultAsync(item => item.Id == templateId && item.CompanyId == currentUser.CompanyId && item.Status != "Deleted");

        if (template is null)
        {
            TempData["StatusMessage"] = "Checklist template not found or already deleted.";
            return RedirectToPage(new { view = "register" });
        }

        var now = DateTime.UtcNow;
        template.Status = "Deleted";
        template.IsPublished = false;
        template.PublishedAtUtc = null;
        template.PublishedByUserId = null;
        template.PublishScopeSummary = null;
        template.PublishNotes = null;
        template.UpdatedAtUtc = now;

        var retiredScopeCount = 0;
        foreach (var scope in template.PublishScopes)
        {
            scope.IsActive = false;
            scope.RetiredAtUtc ??= now;
            retiredScopeCount++;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Checklist template retired/deleted",
            EntityType = "ChecklistTemplate",
            EntityId = template.Id,
            Details = $"Retired/deleted checklist template '{template.Name}' for {template.TargetVehicleType}. Retired publish scopes: {retiredScopeCount}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Checklist template deleted and removed from live use.";
        return RedirectToPage(new { view = "register" });
    }

    private async Task<bool> HasChecklistPublishTaskAccessAsync(AppUser currentUser)
    {
        var taskAccess = Request.Query["taskAccess"].ToString();
        var taskIdValue = Request.Query["taskId"].ToString();
        if (!string.Equals(taskAccess, "true", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(taskIdValue, out var taskId))
        {
            return false;
        }

        return await _db.TaskItems
            .AsNoTracking()
            .Include(task => task.AssignedByUser)
                .ThenInclude(user => user!.AppRole)
            .AnyAsync(task =>
                task.Id == taskId &&
                task.CompanyId == currentUser.CompanyId &&
                task.AssignedToUserId == currentUser.Id &&
                task.Status == "Open" &&
                task.AssignedByUser != null &&
                task.AssignedByUser.AppRole != null &&
                (task.AssignedByUser.AppRole.Name == "Senior Management" || task.AssignedByUser.AppRole.Name == "Company Owner") &&
                (EF.Functions.Like(task.ActionType, "%Checklist%") ||
                    (task.InstructionMessage != null && EF.Functions.Like(task.InstructionMessage, "%publish%"))));
    }
}

public record ChecklistTemplateSummary(
    int Id,
    string Name,
    string ChecklistType,
    string TargetVehicleType,
    string Version,
    string Status,
    bool IsPublished,
    string SourceType,
    string? PublishScopeSummary,
    DateTime LastChangedAtUtc)
{
    public string ChecklistRoute => IsFullAuditName(Name)
        ? "full-audit"
        : "daily-vehicle";

    public string FunctionLabel
    {
        get
        {
            if (IsFullAuditName(Name))
            {
                return "Full Audit";
            }

            if (Name.Contains("Daily", StringComparison.OrdinalIgnoreCase) ||
                Name.Contains("Readiness", StringComparison.OrdinalIgnoreCase))
            {
                return "Daily Check";
            }

            return "Custom";
        }
    }

    public string RegisterName => $"{TargetVehicleType} - {FunctionLabel}";

    public string DisplayName => IsFullAuditName(Name) ? "Full Audit" : Name;

    public string StatusLabel => IsPublished ? "Published" : Status;

    public string ScopeLabel => string.IsNullOrWhiteSpace(PublishScopeSummary)
        ? "Not published"
        : PublishScopeSummary;

    private static bool IsFullAuditName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
            name.Contains("Full Audit", StringComparison.OrdinalIgnoreCase);
    }
}
