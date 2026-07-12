using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditChecklistModel : PageModel
{
    private const string ScopeAllAreas = ChecklistPublishingService.ScopeAllAreas;
    private const string ScopeOperationalArea = ChecklistPublishingService.ScopeOperationalArea;
    private const string ScopeVehicleCategory = ChecklistPublishingService.ScopeVehicleCategory;
    private const string ScopeVehicleFunction = ChecklistPublishingService.ScopeVehicleFunction;
    private const string ScopeVehicleSubtype = ChecklistPublishingService.ScopeVehicleSubtype;
    private const string ScopeVehicle = ChecklistPublishingService.ScopeVehicle;

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ChecklistPublishingService _checklistPublishing;
    private readonly IUserActionAuthorizationService _authorization;

    public EditChecklistModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ChecklistPublishingService checklistPublishing,
        IUserActionAuthorizationService authorization)
    {
        _db = db;
        _currentUser = currentUser;
        _checklistPublishing = checklistPublishing;
        _authorization = authorization;
    }

    [BindProperty] public int TemplateId { get; set; }
    [BindProperty] public string ScopeType { get; set; } = ScopeVehicleCategory;
    [BindProperty] public int? OperationalAreaId { get; set; }
    [BindProperty] public int? VehicleId { get; set; }
    [BindProperty] public string? PublishNote { get; set; }
    [BindProperty] public bool TaskAccess { get; set; }
    [BindProperty] public int? TaskId { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public string? ViewMode { get; private set; }

    public bool CanPublishChecklist { get; private set; }

    public bool CanDeleteChecklist { get; private set; }

    public List<ChecklistTemplateSummary> SavedTemplates { get; private set; } = new();

    public ChecklistTemplate? PublishTemplate { get; private set; }

    public List<PublishUseRow> CurrentUses { get; private set; } = new();

    public List<PublishOptionRow> OperationalAreaOptions { get; private set; } = new();

    public List<PublishVehicleOptionRow> VehicleOptions { get; private set; } = new();

    public List<PublishConflictWarning> ConflictWarnings { get; private set; } = new();

    public string TemplateTargetScopeType => PublishTemplate is null ? ScopeVehicleSubtype : ResolveTemplateTargetScopeType(PublishTemplate.TargetVehicleType);

    public string TemplateTargetScopeLabel => TemplateTargetScopeType switch
    {
        ScopeAllAreas => "All eligible vehicles",
        ScopeVehicleFunction => $"Vehicle function: {PublishTemplate?.TargetVehicleType}",
        _ => $"Vehicle subtype: {PublishTemplate?.TargetVehicleType}"
    };

    public async Task OnGetAsync(int? publishTemplateId, string? scopeType, int? operationalAreaId, int? vehicleId, bool taskAccess = false, int? taskId = null)
    {
        ViewMode = Request.Query["view"].ToString();
        TemplateId = publishTemplateId ?? 0;
        ScopeType = NormalizeScopeType(scopeType);
        OperationalAreaId = operationalAreaId;
        VehicleId = vehicleId;
        TaskAccess = taskAccess;
        TaskId = taskId;

        var currentUser = await _currentUser.GetCurrentUserAsync();
        var company = currentUser?.Company ?? await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return;
        }

        CanDeleteChecklist = currentUser is not null &&
            await _authorization.HasPermissionAsync(currentUser, UserActionPermissions.RegistersDelete);
        CanPublishChecklist = currentUser is not null &&
            (await _authorization.HasPermissionAsync(currentUser, UserActionPermissions.ChecklistsPublish) ||
                await HasChecklistPublishTaskAccessAsync(currentUser, TaskAccess, TaskId));

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

        if (publishTemplateId.HasValue && currentUser is not null)
        {
            ViewMode = "register";
            await LoadPublishDataAsync(currentUser, publishTemplateId.Value);
            if (PublishTemplate is not null && ScopeType == ScopeVehicleCategory)
            {
                ScopeType = ResolveTemplateTargetScopeType(PublishTemplate.TargetVehicleType);
            }
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int templateId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (!await _authorization.HasPermissionAsync(currentUser, UserActionPermissions.RegistersDelete))
        {
            TempData["StatusMessage"] = "Only authorized users can delete checklist templates.";
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

    public async Task<IActionResult> OnPostPublishAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (!await UserCanPublishChecklistAsync(currentUser))
        {
            TempData["StatusMessage"] = "Only senior management, or an operational manager with a delegated publish task, can publish a checklist for live operational use.";
            return RedirectToPage(new { view = "register", publishTemplateId = TemplateId, taskAccess = TaskAccess, taskId = TaskId });
        }

        ScopeType = NormalizeScopeType(ScopeType);

        var template = await _db.ChecklistTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == TemplateId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (template is null)
        {
            TempData["StatusMessage"] = "Checklist template not found.";
            return RedirectToPage(new { view = "register" });
        }

        if (ScopeType == ScopeVehicleCategory)
        {
            ScopeType = ResolveTemplateTargetScopeType(template.TargetVehicleType);
        }

        var publishResult = await _checklistPublishing.PublishAsync(
            currentUser,
            new ChecklistPublishRequest(
                template.Id,
                ScopeType,
                OperationalAreaId,
                VehicleId,
                template.TargetVehicleType,
                PublishNote,
                TaskAccess,
                TaskId));

        TempData["StatusMessage"] = publishResult.Message;
        if (!publishResult.Success)
        {
            return RedirectToPage(new { view = "register", publishTemplateId = TemplateId, taskAccess = TaskAccess, taskId = TaskId });
        }

        return RedirectToPage(new { view = "register" });
    }

    private async Task<bool> UserCanPublishChecklistAsync(AppUser currentUser)
    {
        return await _authorization.HasPermissionAsync(currentUser, UserActionPermissions.ChecklistsPublish) ||
            await HasChecklistPublishTaskAccessAsync(currentUser, TaskAccess, TaskId);
    }

    private async Task<bool> HasChecklistPublishTaskAccessAsync(AppUser currentUser, bool taskAccess, int? taskId)
    {
        var queryTaskAccess = Request.Query["taskAccess"].ToString();
        var queryTaskIdValue = Request.Query["taskId"].ToString();
        var hasTaskAccess = taskAccess || string.Equals(queryTaskAccess, "true", StringComparison.OrdinalIgnoreCase);
        var resolvedTaskId = taskId;

        if (resolvedTaskId is null && int.TryParse(queryTaskIdValue, out var parsedTaskId))
        {
            resolvedTaskId = parsedTaskId;
        }

        if (!hasTaskAccess || resolvedTaskId is null)
        {
            return false;
        }

        return await _db.TaskItems
            .AsNoTracking()
            .Include(task => task.AssignedByUser)
                .ThenInclude(user => user!.AppRole)
            .AnyAsync(task =>
                task.Id == resolvedTaskId.Value &&
                task.CompanyId == currentUser.CompanyId &&
                task.AssignedToUserId == currentUser.Id &&
                task.Status == "Open" &&
                task.AssignedByUser != null &&
                task.AssignedByUser.AppRole != null &&
                (task.AssignedByUser.AppRole.Name == "Senior Management" || task.AssignedByUser.AppRole.Name == "Company Owner") &&
                (EF.Functions.Like(task.ActionType, "%Checklist%") ||
                    (task.InstructionMessage != null && EF.Functions.Like(task.InstructionMessage, "%publish%"))));
    }

    private async Task LoadPublishDataAsync(AppUser currentUser, int templateId)
    {
        PublishTemplate = await _db.ChecklistTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == templateId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        OperationalAreaOptions = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == currentUser.CompanyId && area.Status != "Deleted")
            .OrderBy(area => area.Name)
            .Select(area => new PublishOptionRow(area.Id, area.Name, area.AreaType))
            .ToListAsync();

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted")
            .OrderBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        VehicleOptions = vehicles
            .Select(vehicle => new PublishVehicleOptionRow(
                vehicle.Id,
                vehicle.Callsign,
                vehicle.RegistrationNumber,
                VehicleTaxonomyService.DisplayClassification(vehicle),
                vehicle.VehicleFunction,
                vehicle.VehicleSubtype,
                vehicle.CurrentOperationalArea != null ? vehicle.CurrentOperationalArea.Name : "Unallocated"))
            .ToList();

        if (PublishTemplate is null)
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
                scope.CompanyId == currentUser.CompanyId &&
                scope.IsActive &&
                scope.RetiredAtUtc == null &&
                scope.ChecklistTemplate != null &&
                scope.ChecklistTemplate.CompanyId == currentUser.CompanyId &&
                scope.ChecklistTemplate.ChecklistType == PublishTemplate.ChecklistType &&
                scope.ChecklistTemplate.Status != "Deleted")
            .ToListAsync();

        CurrentUses = activeScopes
            .Where(scope => scope.ChecklistTemplateId == PublishTemplate.Id)
            .OrderBy(scope => ScopeRank(scope.ScopeType))
            .ThenBy(scope => ScopeTargetLabel(scope))
            .Select(scope => new PublishUseRow(
                ScopeDisplayLabel(scope.ScopeType, scope.ChecklistTemplate?.TargetVehicleType ?? PublishTemplate.TargetVehicleType),
                ScopeTargetLabel(scope),
                scope.PublishedAtUtc,
                scope.PublishedByUser?.FullName ?? "Senior manager",
                scope.PublishNote))
            .ToList();

        ConflictWarnings = BuildConflictWarnings(activeScopes.Where(scope => scope.ChecklistTemplateId != PublishTemplate.Id), PublishTemplate);
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

            if (scope.ScopeType is ScopeVehicleFunction or ScopeVehicleSubtype or ScopeVehicleCategory)
            {
                var scopeType = scope.ScopeType == ScopeVehicleCategory
                    ? ResolveTemplateTargetScopeType(template.TargetVehicleType)
                    : scope.ScopeType;
                warnings.Add(new PublishConflictWarning($"{scopeType}:{template.TargetVehicleType}", message));
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
            ScopeOperationalArea => string.Equals(scope.OperationalArea?.Name, vehicle.AreaName, StringComparison.OrdinalIgnoreCase) &&
                VehicleMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType),
            ScopeVehicleFunction => VehicleFunctionMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType),
            ScopeVehicleSubtype => VehicleSubtypeMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType),
            ScopeVehicleCategory => VehicleMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType),
            ScopeAllAreas => VehicleMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType),
            _ => false
        };
    }

    private static string ScopeTargetLabel(ChecklistPublishScope scope)
    {
        return scope.ScopeType switch
        {
            ScopeOperationalArea => scope.OperationalArea is null
                ? "Selected area / base"
                : $"{scope.OperationalArea.AreaType}: {scope.OperationalArea.Name}",
            ScopeVehicle => scope.Vehicle is null
                ? "Selected callsign"
                : $"{scope.Vehicle.Callsign} / {scope.Vehicle.RegistrationNumber}{FormatVehicleArea(scope.Vehicle)}",
            ScopeVehicleFunction => scope.ChecklistTemplate?.TargetVehicleType ?? "Selected function",
            ScopeVehicleSubtype => scope.ChecklistTemplate?.TargetVehicleType ?? "Selected subtype",
            ScopeVehicleCategory => scope.ChecklistTemplate?.TargetVehicleType ?? "Vehicle target",
            _ => "All operational areas"
        };
    }

    private static string ScopeDisplayLabel(string scopeType, string targetVehicleType)
    {
        return scopeType switch
        {
            ScopeVehicle => "Specific callsign / registration",
            ScopeOperationalArea => "Specific area / base",
            ScopeVehicleFunction => "Vehicle function",
            ScopeVehicleSubtype => "Vehicle subtype",
            ScopeVehicleCategory => ResolveTemplateTargetScopeType(targetVehicleType) == ScopeVehicleFunction
                ? "Vehicle function"
                : "Vehicle subtype",
            ScopeAllAreas => "All areas / eligible vehicles",
            _ => "Live use"
        };
    }

    private static string FormatVehicleArea(Vehicle vehicle)
    {
        return vehicle.CurrentOperationalArea is null
            ? string.Empty
            : $" - {vehicle.CurrentOperationalArea.Name}";
    }

    private static int ScopeRank(string scopeType) => scopeType switch
    {
        ScopeVehicle => 5,
        ScopeOperationalArea => 4,
        ScopeVehicleSubtype => 3,
        ScopeVehicleFunction => 2,
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
            ScopeVehicleFunction => ScopeVehicleFunction,
            ScopeVehicleSubtype => ScopeVehicleSubtype,
            ScopeVehicleCategory => ScopeVehicleCategory,
            ScopeVehicle => ScopeVehicle,
            _ => ScopeVehicleCategory
        };
    }

    private static string ResolveTemplateTargetScopeType(string targetVehicleType)
    {
        if (string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase))
        {
            return ScopeAllAreas;
        }

        return string.Equals(targetVehicleType, VehicleTaxonomyService.AmbulanceFunction, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(targetVehicleType, VehicleTaxonomyService.ResponseVehicleFunction, StringComparison.OrdinalIgnoreCase)
            ? ScopeVehicleFunction
            : ScopeVehicleSubtype;
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

    private static bool VehicleFunctionMatchesTemplateTarget(PublishVehicleOptionRow vehicle, string targetVehicleType)
    {
        var function = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        return string.Equals(function, targetVehicleType, StringComparison.OrdinalIgnoreCase);
    }

    private static bool VehicleSubtypeMatchesTemplateTarget(PublishVehicleOptionRow vehicle, string targetVehicleType)
    {
        var subtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);
        return string.Equals(subtype, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicle.VehicleType, targetVehicleType, StringComparison.OrdinalIgnoreCase);
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
