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
    private const string ScopeVehicleFunction = "VehicleFunction";
    private const string ScopeVehicleSubtype = "VehicleSubtype";
    private const string ScopeVehicle = "Vehicle";

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ChecklistPublishingService _checklistPublishing;

    public PublishChecklistModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ChecklistPublishingService checklistPublishing)
    {
        _db = db;
        _currentUser = currentUser;
        _checklistPublishing = checklistPublishing;
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
    public string TemplateTargetScopeType => Template is null ? ScopeVehicleSubtype : ResolveTemplateTargetScopeType(Template.TargetVehicleType);
    public string TemplateTargetScopeLabel => TemplateTargetScopeType switch
    {
        ScopeAllAreas => "All eligible vehicles",
        ScopeVehicleFunction => $"Vehicle function: {Template?.TargetVehicleType}",
        _ => $"Vehicle subtype: {Template?.TargetVehicleType}"
    };

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

        if (ScopeType == ScopeVehicleCategory)
        {
            ScopeType = ResolveTemplateTargetScopeType(Template.TargetVehicleType);
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

        if (ScopeType == ScopeVehicleCategory)
        {
            ScopeType = ResolveTemplateTargetScopeType(template.TargetVehicleType);
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

        if (!publishResult.Success)
        {
            StatusMessage = publishResult.Message;
            await LoadPageDataAsync(currentUser);
            return Page();
        }

        StatusMessage = publishResult.Message;
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
                scope.RetiredAtUtc == null &&
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
                ScopeDisplayLabel(scope.ScopeType, scope.ChecklistTemplate?.TargetVehicleType ?? Template.TargetVehicleType),
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
            string.Equals(targetVehicleType, VehicleTaxonomyService.ResponseVehicleFunction, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase)
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

public record PublishUseRow(string ScopeType, string Target, DateTime PublishedAtUtc, string PublishedBy, string? PublishNote);
public record PublishOptionRow(int Id, string Name, string Type);
public record PublishVehicleOptionRow(int Id, string Callsign, string Registration, string VehicleType, string? VehicleFunction, string? VehicleSubtype, string AreaName);
public record PublishConflictWarning(string ScopeKey, string Message);
