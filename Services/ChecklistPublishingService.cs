using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class ChecklistPublishingService
{
    public const string ScopeAllAreas = "AllAreas";
    public const string ScopeOperationalArea = "OperationalArea";
    public const string ScopeVehicleCategory = "VehicleCategory";
    public const string ScopeVehicleFunction = "VehicleFunction";
    public const string ScopeVehicleSubtype = "VehicleSubtype";
    public const string ScopeVehicle = "Vehicle";

    private readonly VectorDbContext _db;

    public ChecklistPublishingService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<ChecklistPublishResult> PublishAsync(AppUser actor, ChecklistPublishRequest request)
    {
        var scopeType = NormalizeScopeType(request.ScopeType);
        var template = await _db.ChecklistTemplates
            .Include(item => item.PublishScopes)
            .FirstOrDefaultAsync(item =>
                item.Id == request.TemplateId &&
                item.CompanyId == actor.CompanyId &&
                item.Status != "Deleted");

        if (template is null)
        {
            return ChecklistPublishResult.Fail("Checklist template not found.");
        }

        if (scopeType == ScopeVehicleCategory)
        {
            scopeType = ResolveTemplateTargetScopeType(template.TargetVehicleType);
        }

        var selectedArea = scopeType == ScopeOperationalArea && request.OperationalAreaId is not null
            ? await _db.OperationalAreas.FirstOrDefaultAsync(area =>
                area.Id == request.OperationalAreaId &&
                area.CompanyId == actor.CompanyId &&
                area.Status != "Deleted")
            : null;

        var selectedVehicle = scopeType == ScopeVehicle && request.VehicleId is not null
            ? await _db.Vehicles
                .Include(vehicle => vehicle.CurrentOperationalArea)
                .FirstOrDefaultAsync(vehicle =>
                    vehicle.Id == request.VehicleId &&
                    vehicle.CompanyId == actor.CompanyId &&
                    vehicle.Status != "Deleted")
            : null;

        if (scopeType == ScopeOperationalArea && selectedArea is null)
        {
            return ChecklistPublishResult.Fail("Select the operational area or base before publishing.");
        }

        if (scopeType == ScopeVehicle && selectedVehicle is null)
        {
            return ChecklistPublishResult.Fail("Select the callsign or registration before publishing.");
        }

        if (scopeType == ScopeVehicleFunction || scopeType == ScopeVehicleSubtype)
        {
            var targetVehicleType = NormalizeOptional(request.TargetVehicleTypeOverride) ??
                NormalizeOptional(template.TargetVehicleType);
            if (targetVehicleType is null || string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase))
            {
                return ChecklistPublishResult.Fail("Select the exact vehicle function or subtype before publishing.");
            }

            template.TargetVehicleType = targetVehicleType;
        }

        if (scopeType == ScopeVehicle && selectedVehicle is not null)
        {
            template.TargetVehicleType = NormalizeOptional(selectedVehicle.VehicleSubtype) ??
                NormalizeOptional(selectedVehicle.VehicleFunction) ??
                NormalizeOptional(selectedVehicle.VehicleType) ??
                template.TargetVehicleType;
        }

        var now = DateTime.UtcNow;
        var activeScopes = await _db.ChecklistPublishScopes
            .Include(scope => scope.ChecklistTemplate)
            .Include(scope => scope.OperationalArea)
            .Include(scope => scope.Vehicle)
                .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
            .Where(scope =>
                scope.CompanyId == actor.CompanyId &&
                scope.IsActive &&
                scope.RetiredAtUtc == null &&
                scope.ChecklistTemplate != null &&
                scope.ChecklistTemplate.CompanyId == actor.CompanyId &&
                scope.ChecklistTemplate.ChecklistType == template.ChecklistType &&
                scope.ChecklistTemplate.Status != "Deleted")
            .ToListAsync();

        var scopeSummary = ScopeSummary(scopeType, template.TargetVehicleType, selectedArea, selectedVehicle);
        var replacedScopes = new List<ChecklistPublishScopeAuditEntry>();
        var refreshedScopes = new List<ChecklistPublishScopeAuditEntry>();
        var retiredTemplateIds = new HashSet<int>();

        foreach (var activeScope in activeScopes.Where(scope => DirectScopeConflict(scope, template, scopeType, selectedArea?.Id, selectedVehicle?.Id)))
        {
            var auditEntry = new ChecklistPublishScopeAuditEntry(
                activeScope.Id,
                activeScope.ChecklistTemplateId,
                activeScope.ChecklistTemplate?.Name ?? $"Checklist #{activeScope.ChecklistTemplateId}",
                activeScope.ScopeType,
                ScopeTargetLabel(activeScope));

            if (activeScope.ChecklistTemplateId == template.Id)
            {
                refreshedScopes.Add(auditEntry);
            }
            else
            {
                replacedScopes.Add(auditEntry);
                retiredTemplateIds.Add(activeScope.ChecklistTemplateId);
            }

            activeScope.IsActive = false;
            activeScope.RetiredAtUtc = now;
        }

        template.Status = "Published";
        template.IsPublished = true;
        template.PublishedAtUtc = now;
        template.PublishedByUserId = actor.Id;
        template.PublishScopeSummary = scopeSummary;
        template.PublishNotes = NormalizeOptional(request.PublishNote);
        template.UpdatedAtUtc = now;

        var newScope = new ChecklistPublishScope
        {
            CompanyId = actor.CompanyId,
            ChecklistTemplate = template,
            ScopeType = scopeType,
            OperationalAreaId = scopeType == ScopeOperationalArea ? selectedArea?.Id : null,
            VehicleId = scopeType == ScopeVehicle ? selectedVehicle?.Id : null,
            PublishedByUserId = actor.Id,
            PublishNote = NormalizeOptional(request.PublishNote),
            IsActive = true,
            PublishedAtUtc = now
        };
        _db.ChecklistPublishScopes.Add(newScope);

        foreach (var retiredTemplateId in retiredTemplateIds)
        {
            var hasRemainingScope = activeScopes.Any(scope =>
                scope.ChecklistTemplateId == retiredTemplateId &&
                scope.IsActive &&
                scope.RetiredAtUtc == null);

            if (hasRemainingScope)
            {
                continue;
            }

            var retiredTemplate = await _db.ChecklistTemplates
                .FirstOrDefaultAsync(item =>
                    item.Id == retiredTemplateId &&
                    item.CompanyId == actor.CompanyId);
            if (retiredTemplate is null)
            {
                continue;
            }

            retiredTemplate.IsPublished = false;
            retiredTemplate.Status = "Archived";
            retiredTemplate.UpdatedAtUtc = now;
            AuditTrailService.Record(
                _db,
                actor.CompanyId,
                actor.Id,
                "Checklist template retired",
                "ChecklistTemplate",
                retiredTemplate.Id,
                $"'{retiredTemplate.Name}' was retired because all matching live scopes were replaced by '{template.Name}'.",
                now);
        }

        foreach (var refreshedScope in refreshedScopes)
        {
            AuditTrailService.Record(
                _db,
                actor.CompanyId,
                actor.Id,
                "Checklist publish scope refreshed",
                "ChecklistPublishScope",
                refreshedScope.ScopeId,
                $"Republishing '{template.Name}' refreshed its prior live scope on {refreshedScope.TargetLabel}.",
                now);
        }

        foreach (var replacedScope in replacedScopes)
        {
            AuditTrailService.Record(
                _db,
                actor.CompanyId,
                actor.Id,
                "Checklist scope replaced",
                "ChecklistPublishScope",
                replacedScope.ScopeId,
                $"Publishing '{template.Name}' replaced '{replacedScope.TemplateName}' on {replacedScope.TargetLabel}. Previous template id: {replacedScope.TemplateId}.",
                now);
        }

        AuditTrailService.Record(
            _db,
            actor.CompanyId,
            actor.Id,
            "Checklist template published",
            "ChecklistTemplate",
            template.Id,
            $"{actor.FullName} published '{template.Name}' for {scopeSummary}.",
            now);

        await CompleteChecklistApprovalTaskAsync(actor, template, request, now);
        await _db.SaveChangesAsync();

        return ChecklistPublishResult.Ok(
            replacedScopes.Count,
            refreshedScopes.Count,
            scopeSummary,
            replacedScopes.Count == 0
                ? "Checklist published for live use."
                : "Checklist published for live use. Existing checklist coverage for the selected target was replaced.");
    }

    private async Task CompleteChecklistApprovalTaskAsync(
        AppUser actor,
        ChecklistTemplate template,
        ChecklistPublishRequest request,
        DateTime now)
    {
        if (!request.TaskAccess || request.TaskId is null)
        {
            return;
        }

        var task = await _db.TaskItems
            .FirstOrDefaultAsync(item =>
                item.Id == request.TaskId.Value &&
                item.CompanyId == actor.CompanyId &&
                item.AssignedToUserId == actor.Id &&
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
            CompanyId = actor.CompanyId,
            TaskItemId = task.Id,
            PerformedByUserId = actor.Id,
            EventType = "Approved",
            Notes = "Checklist approval request approved and published for live use.",
            CreatedAtUtc = now
        });
    }

    private static bool DirectScopeConflict(
        ChecklistPublishScope scope,
        ChecklistTemplate selectedTemplate,
        string selectedScopeType,
        int? operationalAreaId,
        int? vehicleId)
    {
        if (scope.ChecklistTemplate is null)
        {
            return false;
        }

        if (selectedScopeType == ScopeVehicle)
        {
            return scope.ScopeType == ScopeVehicle && scope.VehicleId == vehicleId;
        }

        if (!TargetsMatch(selectedTemplate.TargetVehicleType, scope.ChecklistTemplate.TargetVehicleType))
        {
            return false;
        }

        return selectedScopeType switch
        {
            ScopeAllAreas => scope.ScopeType == ScopeAllAreas,
            ScopeVehicleFunction => scope.ScopeType is ScopeVehicleFunction or ScopeVehicleCategory,
            ScopeVehicleSubtype => scope.ScopeType is ScopeVehicleSubtype or ScopeVehicleCategory,
            ScopeOperationalArea => scope.ScopeType == ScopeOperationalArea && scope.OperationalAreaId == operationalAreaId,
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

    private static string ScopeSummary(string scopeType, string templateTargetVehicleType, OperationalArea? area, Vehicle? vehicle)
    {
        return scopeType switch
        {
            ScopeOperationalArea => $"Area/base: {area?.Name ?? "selected area"}",
            ScopeVehicle => vehicle is null ? "Specific callsign" : $"Callsign: {vehicle.Callsign} / {vehicle.RegistrationNumber}",
            ScopeVehicleFunction => $"Vehicle function: {templateTargetVehicleType}",
            ScopeVehicleSubtype => $"Vehicle subtype: {templateTargetVehicleType}",
            ScopeVehicleCategory => $"Vehicle target: {templateTargetVehicleType}",
            _ => "All operational areas"
        };
    }

    private static string FormatVehicleArea(Vehicle vehicle)
    {
        return vehicle.CurrentOperationalArea is null
            ? string.Empty
            : $" - {vehicle.CurrentOperationalArea.Name}";
    }

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

    private static bool TargetsMatch(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public record ChecklistPublishRequest(
    int TemplateId,
    string ScopeType,
    int? OperationalAreaId,
    int? VehicleId,
    string? TargetVehicleTypeOverride,
    string? PublishNote,
    bool TaskAccess = false,
    int? TaskId = null);

public record ChecklistPublishResult(
    bool Success,
    int ReplacedScopeCount,
    int RefreshedScopeCount,
    string ScopeSummary,
    string Message)
{
    public static ChecklistPublishResult Ok(
        int replacedScopeCount,
        int refreshedScopeCount,
        string scopeSummary,
        string message) =>
        new(true, replacedScopeCount, refreshedScopeCount, scopeSummary, message);

    public static ChecklistPublishResult Fail(string message) =>
        new(false, 0, 0, string.Empty, message);
}

internal record ChecklistPublishScopeAuditEntry(
    int ScopeId,
    int TemplateId,
    string TemplateName,
    string ScopeType,
    string TargetLabel);
