using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class PublishChecklistModel : PageModel
{
    private const string ScopeAllAreas = "AllAreas";
    private const string ScopeOperationalArea = "OperationalArea";
    private const string ScopeVehicleCategory = "VehicleCategory";
    private const string ScopeVehicle = "Vehicle";

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public PublishChecklistModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty] public int TemplateId { get; set; }
    [BindProperty] public string ScopeType { get; set; } = ScopeVehicleCategory;
    [BindProperty] public int? OperationalAreaId { get; set; }
    [BindProperty] public int? VehicleId { get; set; }
    [BindProperty] public string? PublishNote { get; set; }
    [BindProperty] public bool TaskAccess { get; set; }
    [BindProperty] public int? TaskId { get; set; }

    [TempData] public string? StatusMessage { get; set; }

    public ChecklistTemplate? Template { get; private set; }
    public bool IsSeniorManager { get; private set; }
    public bool CanPublishChecklist { get; private set; }
    public List<PublishUseRow> CurrentUses { get; private set; } = new();
    public List<PublishOptionRow> OperationalAreaOptions { get; private set; } = new();
    public List<PublishVehicleOptionRow> VehicleOptions { get; private set; } = new();
    public List<PublishConflictWarning> ConflictWarnings { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int templateId, string? scopeType, int? operationalAreaId, int? vehicleId, bool taskAccess = false, int? taskId = null)
    {
        TemplateId = templateId;
        ScopeType = NormalizeScopeType(scopeType);
        OperationalAreaId = operationalAreaId;
        VehicleId = vehicleId;
        TaskAccess = taskAccess;
        TaskId = taskId;

        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        await LoadPageDataAsync(currentUser);
        if (Template is null)
        {
            StatusMessage = "Checklist template not found.";
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostPublishAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (!await UserCanPublishChecklistAsync(currentUser))
        {
            StatusMessage = "Only senior management, or an operational manager with a delegated publish task, can publish a checklist for live operational use.";
            await LoadPageDataAsync(currentUser);
            return Page();
        }

        ScopeType = NormalizeScopeType(ScopeType);

        var template = await _db.ChecklistTemplates
            .Include(item => item.PublishScopes)
            .FirstOrDefaultAsync(item =>
                item.Id == TemplateId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (template is null)
        {
            StatusMessage = "Checklist template not found.";
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        var selectedVehicle = ScopeType == ScopeVehicle && VehicleId is not null
            ? await _db.Vehicles
                .AsNoTracking()
                .Include(vehicle => vehicle.CurrentOperationalArea)
                .FirstOrDefaultAsync(vehicle => vehicle.Id == VehicleId && vehicle.CompanyId == currentUser.CompanyId)
            : null;

        var selectedArea = ScopeType == ScopeOperationalArea && OperationalAreaId is not null
            ? await _db.OperationalAreas
                .AsNoTracking()
                .FirstOrDefaultAsync(area => area.Id == OperationalAreaId && area.CompanyId == currentUser.CompanyId)
            : null;

        if (ScopeType == ScopeOperationalArea && selectedArea is null)
        {
            StatusMessage = "Select the operational area or base before publishing.";
            await LoadPageDataAsync(currentUser);
            return Page();
        }

        if (ScopeType == ScopeVehicle && selectedVehicle is null)
        {
            StatusMessage = "Select the callsign or registration before publishing.";
            await LoadPageDataAsync(currentUser);
            return Page();
        }

        var now = DateTime.UtcNow;
        var activeScopes = await _db.ChecklistPublishScopes
            .Include(scope => scope.ChecklistTemplate)
            .Where(scope =>
                scope.IsActive &&
                scope.ChecklistTemplate != null &&
                scope.ChecklistTemplate.CompanyId == currentUser.CompanyId &&
                scope.ChecklistTemplate.ChecklistType == template.ChecklistType &&
                scope.ChecklistTemplate.Status != "Deleted")
            .ToListAsync();

        var retiredTemplateIds = new HashSet<int>();
        foreach (var activeScope in activeScopes.Where(scope => ExactScopeConflict(scope, template, ScopeType, OperationalAreaId, VehicleId)))
        {
            activeScope.IsActive = false;
            activeScope.RetiredAtUtc = now;
            retiredTemplateIds.Add(activeScope.ChecklistTemplateId);
        }

        template.Status = "Published";
        template.IsPublished = true;
        template.PublishedAtUtc = now;
        template.PublishedByUserId = currentUser.Id;
        template.PublishScopeSummary = ScopeSummary(ScopeType, template.TargetVehicleType, selectedArea, selectedVehicle);
        template.PublishNotes = NormalizeOptional(PublishNote);
        template.UpdatedAtUtc = now;

        _db.ChecklistPublishScopes.Add(new ChecklistPublishScope
        {
            ChecklistTemplate = template,
            ScopeType = ScopeType,
            OperationalAreaId = ScopeType == ScopeOperationalArea ? selectedArea?.Id : null,
            VehicleId = ScopeType == ScopeVehicle ? selectedVehicle?.Id : null,
            PublishedByUserId = currentUser.Id,
            PublishNote = NormalizeOptional(PublishNote),
            IsActive = true,
            PublishedAtUtc = now
        });

        await _db.SaveChangesAsync();

        foreach (var retiredTemplateId in retiredTemplateIds.Where(id => id != template.Id))
        {
            var hasRemainingScope = await _db.ChecklistPublishScopes
                .AnyAsync(scope => scope.ChecklistTemplateId == retiredTemplateId && scope.IsActive);

            if (!hasRemainingScope)
            {
                var retiredTemplate = await _db.ChecklistTemplates.FirstOrDefaultAsync(item => item.Id == retiredTemplateId);
                if (retiredTemplate is not null)
                {
                    retiredTemplate.IsPublished = false;
                    retiredTemplate.Status = "Archived";
                    retiredTemplate.UpdatedAtUtc = now;
                }
            }
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Checklist published",
            EntityType = "ChecklistTemplate",
            EntityId = template.Id,
            Details = $"{currentUser.FullName} published '{template.Name}' for {ScopeSummary(ScopeType, template.TargetVehicleType, selectedArea, selectedVehicle)}.",
            CreatedAtUtc = now
        });

        await CompleteChecklistApprovalTaskAsync(currentUser, template, now);

        await _db.SaveChangesAsync();

        StatusMessage = "Checklist published for live use. Existing daily check coverage for the selected target has been replaced where applicable.";
        return RedirectToPage("/EditChecklist", new { view = "register" });
    }

    private async Task LoadPageDataAsync(AppUser currentUser)
    {
        IsSeniorManager = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        CanPublishChecklist = IsSeniorManager || await HasChecklistPublishTaskAccessAsync(currentUser);

        Template = await _db.ChecklistTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == TemplateId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        OperationalAreaOptions = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == currentUser.CompanyId && area.Status != "Deleted")
            .OrderBy(area => area.Name)
            .Select(area => new PublishOptionRow(area.Id, area.Name, area.AreaType))
            .ToListAsync();

        VehicleOptions = await _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted")
            .OrderBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .Select(vehicle => new PublishVehicleOptionRow(
                vehicle.Id,
                vehicle.Callsign,
                vehicle.RegistrationNumber,
                vehicle.VehicleType,
                vehicle.VehicleFunction,
                vehicle.VehicleSubtype,
                vehicle.CurrentOperationalArea != null ? vehicle.CurrentOperationalArea.Name : "Unallocated"))
            .ToListAsync();

        if (Template is null)
        {
            return;
        }

        var activeScopes = await _db.ChecklistPublishScopes
            .AsNoTracking()
            .Include(scope => scope.ChecklistTemplate)
            .Include(scope => scope.OperationalArea)
            .Include(scope => scope.Vehicle)
            .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
            .Include(scope => scope.PublishedByUser)
            .Where(scope =>
                scope.IsActive &&
                scope.ChecklistTemplate != null &&
                scope.ChecklistTemplate.CompanyId == currentUser.CompanyId &&
                scope.ChecklistTemplate.ChecklistType == Template.ChecklistType &&
                scope.ChecklistTemplate.Status != "Deleted")
            .ToListAsync();

        CurrentUses = activeScopes
            .Where(scope => scope.ChecklistTemplateId == Template.Id)
            .OrderBy(scope => ScopeRank(scope.ScopeType))
            .ThenBy(scope => ScopeTargetLabel(scope))
            .Select(scope => new PublishUseRow(
                scope.ScopeType,
                ScopeTargetLabel(scope),
                scope.PublishedAtUtc,
                scope.PublishedByUser?.FullName ?? "Senior manager",
                scope.PublishNote))
            .ToList();

        ConflictWarnings = BuildConflictWarnings(activeScopes.Where(scope => scope.ChecklistTemplateId != Template.Id), Template);
    }

    private async Task<bool> UserCanPublishChecklistAsync(AppUser currentUser)
    {
        return CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name) ||
            await HasChecklistPublishTaskAccessAsync(currentUser);
    }

    private async Task<bool> HasChecklistPublishTaskAccessAsync(AppUser currentUser)
    {
        var queryTaskAccess = Request.Query["taskAccess"].ToString();
        var queryTaskIdValue = Request.Query["taskId"].ToString();
        var hasTaskAccess = TaskAccess || string.Equals(queryTaskAccess, "true", StringComparison.OrdinalIgnoreCase);
        var taskId = TaskId;

        if (taskId is null && int.TryParse(queryTaskIdValue, out var parsedTaskId))
        {
            taskId = parsedTaskId;
        }

        if (!hasTaskAccess || taskId is null)
        {
            return false;
        }

        return await _db.TaskItems
            .AsNoTracking()
            .Include(task => task.AssignedByUser)
                .ThenInclude(user => user!.AppRole)
            .AnyAsync(task =>
                task.Id == taskId.Value &&
                task.CompanyId == currentUser.CompanyId &&
                task.AssignedToUserId == currentUser.Id &&
                task.Status == "Open" &&
                task.AssignedByUser != null &&
                task.AssignedByUser.AppRole != null &&
                (task.AssignedByUser.AppRole.Name == "Senior Management" || task.AssignedByUser.AppRole.Name == "Company Owner") &&
                (EF.Functions.Like(task.ActionType, "%Checklist%") ||
                    (task.InstructionMessage != null && EF.Functions.Like(task.InstructionMessage, "%publish%"))));
    }

    private async Task CompleteChecklistApprovalTaskAsync(AppUser currentUser, ChecklistTemplate template, DateTime now)
    {
        if (!TaskAccess || TaskId is null)
        {
            return;
        }

        var task = await _db.TaskItems
            .FirstOrDefaultAsync(item =>
                item.Id == TaskId.Value &&
                item.CompanyId == currentUser.CompanyId &&
                item.AssignedToUserId == currentUser.Id &&
                item.Status == "Open" &&
                item.ActionType == "Checklist approval request" &&
                item.RelatedItemReference == $"ChecklistTemplate:{template.Id}");

        if (task is null)
        {
            return;
        }

        task.Status = "Completed";
        task.CompletedAtUtc = now;

        _db.TaskEvents.Add(new TaskEvent
        {
            TaskItemId = task.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Approved",
            Notes = "Checklist approval request approved and published for live use.",
            CreatedAtUtc = now
        });
    }

    private List<PublishConflictWarning> BuildConflictWarnings(IEnumerable<ChecklistPublishScope> activeScopes, ChecklistTemplate template)
    {
        var warnings = new List<PublishConflictWarning>();

        foreach (var scope in activeScopes)
        {
            if (scope.ChecklistTemplate is null)
            {
                continue;
            }

            if (!VehicleTypeMatchesTemplateTarget(template.TargetVehicleType, scope.ChecklistTemplate.TargetVehicleType))
            {
                continue;
            }

            var message = $"Warning: {scope.ChecklistTemplate.Name} is already used for {ScopeTargetLabel(scope)}. Publishing this checklist to the same target will replace the daily check for that target.";

            if (scope.ScopeType == ScopeAllAreas)
            {
                warnings.Add(new PublishConflictWarning(ScopeAllAreas, message));
            }

            if (scope.ScopeType == ScopeVehicleCategory)
            {
                warnings.Add(new PublishConflictWarning($"{ScopeVehicleCategory}:{template.TargetVehicleType}", message));
            }

            if (scope.ScopeType == ScopeOperationalArea && scope.OperationalAreaId is not null)
            {
                warnings.Add(new PublishConflictWarning($"{ScopeOperationalArea}:{scope.OperationalAreaId}", message));
            }

            if (scope.ScopeType == ScopeVehicle && scope.VehicleId is not null)
            {
                warnings.Add(new PublishConflictWarning($"{ScopeVehicle}:{scope.VehicleId}", message));
            }
        }

        foreach (var vehicle in VehicleOptions)
        {
            var effectiveScope = activeScopes
                .Where(scope => ScopeAppliesToVehicle(scope, vehicle))
                .OrderByDescending(scope => ScopeRank(scope.ScopeType))
                .FirstOrDefault();

            if (effectiveScope?.ChecklistTemplate is not null)
            {
                warnings.Add(new PublishConflictWarning(
                    $"{ScopeVehicle}:{vehicle.Id}",
                    $"Warning: {effectiveScope.ChecklistTemplate.Name} is currently the live daily check for {vehicle.Callsign} / {vehicle.Registration}. Publishing this checklist to that callsign will replace it for that unit."));
            }
        }

        return warnings
            .GroupBy(warning => new { warning.ScopeKey, warning.Message })
            .Select(group => group.First())
            .ToList();
    }

    private static bool ScopeAppliesToVehicle(ChecklistPublishScope scope, PublishVehicleOptionRow vehicle)
    {
        if (scope.ChecklistTemplate is null ||
            !VehicleMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType))
        {
            return false;
        }

        return scope.ScopeType switch
        {
            ScopeVehicle => scope.VehicleId == vehicle.Id,
            ScopeOperationalArea => string.Equals(scope.OperationalArea?.Name, vehicle.AreaName, StringComparison.OrdinalIgnoreCase),
            ScopeVehicleCategory => true,
            ScopeAllAreas => true,
            _ => false
        };
    }

    private static bool ExactScopeConflict(
        ChecklistPublishScope scope,
        ChecklistTemplate template,
        string selectedScopeType,
        int? operationalAreaId,
        int? vehicleId)
    {
        if (scope.ChecklistTemplate is null ||
            !VehicleTypeMatchesTemplateTarget(template.TargetVehicleType, scope.ChecklistTemplate.TargetVehicleType))
        {
            return false;
        }

        return selectedScopeType switch
        {
            ScopeAllAreas => scope.ScopeType == ScopeAllAreas,
            ScopeVehicleCategory => scope.ScopeType == ScopeVehicleCategory,
            ScopeOperationalArea => scope.ScopeType == ScopeOperationalArea && scope.OperationalAreaId == operationalAreaId,
            ScopeVehicle => scope.ScopeType == ScopeVehicle && scope.VehicleId == vehicleId,
            _ => false
        };
    }

    private static string ScopeTargetLabel(ChecklistPublishScope scope)
    {
        return scope.ScopeType switch
        {
            ScopeOperationalArea => scope.OperationalArea?.Name ?? "Selected area/base",
            ScopeVehicle => scope.Vehicle is null
                ? "Selected callsign"
                : $"{scope.Vehicle.Callsign} / {scope.Vehicle.RegistrationNumber}",
            ScopeVehicleCategory => scope.ChecklistTemplate?.TargetVehicleType ?? "Vehicle type",
            _ => "All operational areas"
        };
    }

    private static string ScopeSummary(string scopeType, string templateTargetVehicleType, OperationalArea? area, Vehicle? vehicle)
    {
        return scopeType switch
        {
            ScopeOperationalArea => $"Area/base: {area?.Name ?? "selected area"}",
            ScopeVehicle => vehicle is null ? "Specific callsign" : $"Callsign: {vehicle.Callsign} / {vehicle.RegistrationNumber}",
            ScopeVehicleCategory => $"Vehicle type: {templateTargetVehicleType}",
            _ => "All operational areas"
        };
    }

    private static int ScopeRank(string scopeType) => scopeType switch
    {
        ScopeVehicle => 4,
        ScopeOperationalArea => 3,
        ScopeVehicleCategory => 2,
        ScopeAllAreas => 1,
        _ => 0
    };

    private static string NormalizeScopeType(string? scopeType)
    {
        return scopeType switch
        {
            ScopeAllAreas => ScopeAllAreas,
            ScopeOperationalArea => ScopeOperationalArea,
            ScopeVehicle => ScopeVehicle,
            _ => ScopeVehicleCategory
        };
    }

    private static bool VehicleMatchesTemplateTarget(PublishVehicleOptionRow vehicle, string targetVehicleType)
    {
        if (string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var function = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        var subtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);

        return string.Equals(function, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(subtype, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            VehicleTypeMatchesTemplateTarget(vehicle.VehicleType, targetVehicleType) ||
            (!string.IsNullOrWhiteSpace(subtype) && VehicleTypeMatchesTemplateTarget(subtype, targetVehicleType));
    }

    private static bool VehicleTypeMatchesTemplateTarget(string vehicleType, string targetVehicleType)
    {
        if (string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicleType, targetVehicleType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsPickupRvType(vehicleType))
        {
            return IsPickupRvType(targetVehicleType);
        }

        if (vehicleType.Contains("Ops Ambulance", StringComparison.OrdinalIgnoreCase) ||
            vehicleType.Contains("Operational Ambulance", StringComparison.OrdinalIgnoreCase))
        {
            return targetVehicleType.Contains("Ops Ambulance", StringComparison.OrdinalIgnoreCase) ||
                targetVehicleType.Contains("Operational Ambulance", StringComparison.OrdinalIgnoreCase) ||
                targetVehicleType.Contains("Ambulance", StringComparison.OrdinalIgnoreCase);
        }

        if (vehicleType.Contains("ICU", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(targetVehicleType, "ICU Ambulance", StringComparison.OrdinalIgnoreCase);
        }

        if (vehicleType.Contains("Ambulance", StringComparison.OrdinalIgnoreCase))
        {
            return targetVehicleType.Contains("Ambulance", StringComparison.OrdinalIgnoreCase);
        }

        if (vehicleType.Contains("Response", StringComparison.OrdinalIgnoreCase))
        {
            return targetVehicleType.Contains("Response", StringComparison.OrdinalIgnoreCase);
        }

        if (vehicleType.Contains("Rescue", StringComparison.OrdinalIgnoreCase))
        {
            return targetVehicleType.Contains("Rescue", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsPickupRvType(string? vehicleType)
    {
        return !string.IsNullOrWhiteSpace(vehicleType) &&
            (vehicleType.Contains("Pickup", StringComparison.OrdinalIgnoreCase) ||
             vehicleType.Contains("RV", StringComparison.OrdinalIgnoreCase) ||
             vehicleType.Contains("Response", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public record PublishUseRow(string ScopeType, string Target, DateTime PublishedAtUtc, string PublishedBy, string? PublishNote);
public record PublishOptionRow(int Id, string Name, string Type);
public record PublishVehicleOptionRow(int Id, string Callsign, string Registration, string VehicleType, string? VehicleFunction, string? VehicleSubtype, string AreaName);
public record PublishConflictWarning(string ScopeKey, string Message);
