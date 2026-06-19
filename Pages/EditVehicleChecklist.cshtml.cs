using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditVehicleChecklistModel : PageModel
{
    private const string DailyVehicleChecklistName = "Daily Vehicle & Equipment Check";
    private const string FullAuditChecklistName = "Full Audit";
    private const string LegacyDailyVehicleChecklistName = "Daily Vehicle Readiness";
    private const string SectionNoteItemKind = "SectionNote";
    private const string SchematicBlockItemKind = "SchematicBlock";
    private const string SchematicViewItemKind = "SchematicView";
    private static readonly string[] UnitSchematicViews = ["Left", "Right", "Front", "Rear"];
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ChecklistPublishingService _checklistPublishing;

    public EditVehicleChecklistModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ChecklistPublishingService checklistPublishing)
    {
        _db = db;
        _currentUser = currentUser;
        _checklistPublishing = checklistPublishing;
    }

    [BindProperty] public string? ChecklistName { get; set; }
    [BindProperty] public string ChecklistStatus { get; set; } = "Draft";
    [BindProperty] public int? SelectedTemplateId { get; set; }
    [BindProperty] public bool CreateAsNewTemplate { get; set; }
    [BindProperty] public bool UseCustomChecklistName { get; set; }
    [BindProperty] public string? CustomChecklistName { get; set; }
    [BindProperty] public string TargetVehicleType { get; set; } = string.Empty;
    [BindProperty] public string? TargetVehicleFunction { get; set; }
    [BindProperty] public string? TargetVehicleSubtype { get; set; }
    [BindProperty] public bool UseCustomTargetVehicleType { get; set; }
    [BindProperty] public string? CustomTargetVehicleType { get; set; }
    [BindProperty] public string? AppliesTo { get; set; }
    [BindProperty] public int? PublishOperationalAreaId { get; set; }
    [BindProperty] public int? PublishVehicleId { get; set; }
    [BindProperty] public string? PublishVehicleType { get; set; }
    [BindProperty] public string? PublishVehicleFunction { get; set; }
    [BindProperty] public string? PublishVehicleSubtype { get; set; }
    [BindProperty] public int? SeniorApproverUserId { get; set; }
    [BindProperty] public string? PublishNote { get; set; }
    [BindProperty] public string? ActionType { get; set; }
    [BindProperty] public bool AllowSameAsPreviousVehicleInspection { get; set; } = true;
    [BindProperty] public bool AllowSameAsPreviousEquipmentCheck { get; set; } = true;
    [BindProperty] public List<EquipmentRowEditorInput> EquipmentRows { get; set; } = new();
    [BindProperty] public List<EquipmentColumnEditorInput> EquipmentColumns { get; set; } = new();
    [BindProperty] public List<ChecklistSectionEditorInput> VehicleChecklistSections { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }
    public bool IsSeniorChecklistPublisher { get; private set; }
    public bool HasDelegatedPublishAccess { get; private set; }
    public bool CanApproveAndPublish => IsSeniorChecklistPublisher || HasDelegatedPublishAccess;
    public string ChecklistAuthorityNote { get; private set; } = "Senior management publishes live checklist versions. Operational managers can draft assigned changes.";
    public bool IsScratchBuildMode => SelectedTemplateId is null;
    public string LayoutBuilderSummary => IsScratchBuildMode
        ? "New checklist starts blank. Add sections, rows, columns, items, subitems, notes, and optional register links yourself."
        : IsVehicleChecklistName(ChecklistName)
            ? $"{ChecklistName} is built from the saved register template selected above."
            : "Loaded checklist template is ready for review and editing.";

    public List<ChecklistTemplateOption> AvailableTemplates { get; private set; } = new();
    public List<SelectListItem> PublishAreaOptions { get; private set; } = new();
    public List<SelectListItem> PublishVehicleOptions { get; private set; } = new();
    public List<SelectListItem> PublishVehicleTypeOptions { get; private set; } = new();
    public List<SelectListItem> PublishVehicleFunctionOptions { get; private set; } = new();
    public List<SelectListItem> PublishVehicleSubtypeOptions { get; private set; } = new();
    public List<SelectListItem> SeniorApproverOptions { get; private set; } = new();
    public List<ChecklistLiveUseRow> CurrentLiveUses { get; private set; } = new();
    public List<PublishConflictWarning> ConflictWarnings { get; private set; } = new();
    public List<SelectListItem> TargetVehicleFunctionOptions { get; private set; } = new();
    public List<VehicleTargetSubtypeOption> TargetVehicleSubtypeOptions { get; private set; } = new();
    public IReadOnlyList<VehicleSchematicDefinition> PublishedUnitSchematics => VehicleSchematicLibrary.Published;
    public string DefaultUnitSchematicKey => ResolveDefaultSchematicKey(TargetVehicleType);
    public List<string> ChecklistNameOptions { get; private set; } = new();
    public IReadOnlyList<string> TargetVehicleTypeOptions { get; private set; } = new[] { "All Vehicles" };

    public async Task OnGetAsync(string? checklist, int? templateId, string? targetVehicleType, string? mode)
    {
        var isBuildMode = string.Equals(mode, "build", StringComparison.OrdinalIgnoreCase);
        var currentUser = await LoadCurrentAuthorityAsync(loadPublishedSettings: true);
        SelectedTemplateId = templateId;
        CreateAsNewTemplate = templateId is null;
        ChecklistName = isBuildMode && templateId is null
            ? string.Empty
            : ResolveChecklistName(checklist, templateId is null ? string.Empty : ChecklistName);
        TargetVehicleType = NormalizeTargetVehicleType(targetVehicleType ?? (templateId is null ? string.Empty : TargetVehicleType));
        if (currentUser is not null)
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
            await LoadPublishControlOptionsAsync(currentUser.CompanyId);
            SyncTargetVehicleSelectionFromTargetType();
            if (!isBuildMode || templateId is not null)
            {
                await ApplySelectedTemplateAsync(currentUser.CompanyId);
                SyncTargetVehicleSelectionFromTargetType();
                await LoadChecklistSectionEditorFromTemplateAsync(currentUser.CompanyId);
            }
        }

        LoadVehicleChecklistLayout();
        if (currentUser is not null)
        {
            await LoadEquipmentEditorFromTemplateAsync(currentUser.CompanyId);
        }
        EnsureEquipmentEditorDefaults();
        EnsureSectionMatrixEditors();
        SyncManualEntryControls();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CreateAsNewTemplate = SelectedTemplateId is null;
        var currentUser = await LoadCurrentAuthorityAsync(loadPublishedSettings: false);
        if (!ApplyManualEntryValues())
        {
            LoadVehicleChecklistLayout();
            EnsureEquipmentEditorDefaults();
            EnsureSectionMatrixEditors();
            if (currentUser is not null)
            {
                await LoadTemplateOptionsAsync(currentUser.CompanyId);
                await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
                await LoadPublishControlOptionsAsync(currentUser.CompanyId);
            }

            SyncManualEntryControls();
            return Page();
        }

        LoadVehicleChecklistLayout();
        EnsureEquipmentEditorDefaults();
        EnsureSectionMatrixEditors();

        if (string.IsNullOrWhiteSpace(ChecklistName))
        {
            if (currentUser is not null)
            {
                await LoadTemplateOptionsAsync(currentUser.CompanyId);
                await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
                await LoadPublishControlOptionsAsync(currentUser.CompanyId);
            }

            StatusMessage = "Select a checklist before saving or publishing.";
            SyncManualEntryControls();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(TargetVehicleType))
        {
            if (currentUser is not null)
            {
                await LoadTemplateOptionsAsync(currentUser.CompanyId);
                await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
                await LoadPublishControlOptionsAsync(currentUser.CompanyId);
            }

            StatusMessage = "Select a target vehicle function or subtype before saving or publishing.";
            SyncManualEntryControls();
            return Page();
        }

        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
        await LoadPublishControlOptionsAsync(currentUser.CompanyId);

        TargetVehicleType = NormalizeTargetVehicleType(TargetVehicleType);
        NormalizeEquipmentEditorInputs();
        EnsureSectionMatrixEditors();

        if (ActionType == "approve-publish" && !CanApproveAndPublish)
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
            StatusMessage = "Only senior management can approve and publish a checklist for live operational use. Draft changes can still be saved for review.";
            SyncManualEntryControls();
            return Page();
        }

        var publishScopeType = ResolvePublishScopeType();
        if ((ActionType == "approve-publish" || ActionType == "submit-approval") &&
            publishScopeType == "OperationalArea" &&
            PublishOperationalAreaId is null)
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
            StatusMessage = "Select the base or operational area this checklist should apply to.";
            SyncManualEntryControls();
            return Page();
        }

        if ((ActionType == "approve-publish" || ActionType == "submit-approval") &&
            publishScopeType == "Vehicle" &&
            PublishVehicleId is null)
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
            StatusMessage = "Select the callsign or registration this checklist should apply to.";
            SyncManualEntryControls();
            return Page();
        }

        if ((ActionType == "approve-publish" || ActionType == "submit-approval") &&
            publishScopeType == "VehicleFunction" &&
            string.IsNullOrWhiteSpace(PublishVehicleFunction))
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
            await LoadPublishControlOptionsAsync(currentUser.CompanyId);
            StatusMessage = "Select the exact vehicle function this checklist should apply to.";
            SyncManualEntryControls();
            return Page();
        }

        if ((ActionType == "approve-publish" || ActionType == "submit-approval") &&
            publishScopeType == "VehicleSubtype" &&
            string.IsNullOrWhiteSpace(PublishVehicleSubtype))
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
            await LoadPublishControlOptionsAsync(currentUser.CompanyId);
            StatusMessage = "Select the exact vehicle subtype this checklist should apply to.";
            SyncManualEntryControls();
            return Page();
        }

        if (ActionType == "submit-approval" && SeniorApproverUserId is null)
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
            StatusMessage = "Select the senior manager who should review and approve this checklist.";
            SyncManualEntryControls();
            return Page();
        }

        if (ActionType == "submit-approval" &&
            !SeniorApproverOptions.Any(option => option.Value == SeniorApproverUserId?.ToString()))
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
            StatusMessage = "Select an active senior manager in this company before submitting for approval.";
            SyncManualEntryControls();
            return Page();
        }

        if (ActionType == "submit-approval")
        {
            ChecklistStatus = "Under Review";
        }

        ApplyPublishTargetToTemplateTarget();
        var savedTemplate = await SaveTemplateAsync(currentUser, ActionType == "approve-publish");

        if (ActionType == "submit-approval")
        {
            savedTemplate.Status = "Under Review";
            savedTemplate.IsPublished = false;
            savedTemplate.PublishScopeSummary = ResolvePublishScopeSummary();
            savedTemplate.PublishNotes = NormalizeOptional(PublishNote);
            savedTemplate.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await CreateChecklistApprovalTaskAsync(currentUser, savedTemplate);
            TempData["StatusMessage"] = $"{savedTemplate.Name} for {savedTemplate.TargetVehicleType} saved and submitted to senior management for approval.";
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        if (ActionType == "approve-publish" && currentUser is not null)
        {
            var publishResult = await _checklistPublishing.PublishAsync(
                currentUser,
                new ChecklistPublishRequest(
                    savedTemplate.Id,
                    ResolvePublishScopeType(),
                    PublishOperationalAreaId,
                    PublishVehicleId,
                    TargetVehicleType,
                    PublishNote));

            if (!publishResult.Success)
            {
                await LoadTemplateOptionsAsync(currentUser.CompanyId);
                await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
                await LoadPublishControlOptionsAsync(currentUser.CompanyId);
                StatusMessage = publishResult.Message;
                SyncManualEntryControls();
                return Page();
            }

            var company = await _db.Companies.FirstOrDefaultAsync(item => item.Id == currentUser.CompanyId);
            if (company is not null)
            {
                company.AllowSameAsPreviousVehicleInspection = AllowSameAsPreviousVehicleInspection;
                company.AllowSameAsPreviousEquipmentCheck = AllowSameAsPreviousEquipmentCheck;
                company.UpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        StatusMessage = ActionType == "approve-publish"
            ? $"{savedTemplate.Name} for {savedTemplate.TargetVehicleType} approved and published. Same as previous shift: vehicle inspection {(AllowSameAsPreviousVehicleInspection ? "enabled" : "disabled")}; equipment checks {(AllowSameAsPreviousEquipmentCheck ? "enabled" : "disabled")}."
            : $"{savedTemplate.Name} for {savedTemplate.TargetVehicleType} draft saved as an available vehicle checklist template.";

        return RedirectToPage("/EditChecklist");
    }

    private async Task LoadTemplateOptionsAsync(int companyId)
    {
        await LoadChecklistNameOptionsAsync(companyId);

        AvailableTemplates = await _db.ChecklistTemplates
            .AsNoTracking()
            .Where(template =>
                template.CompanyId == companyId &&
                template.ChecklistType == "Vehicle" &&
                template.Status != "Deleted")
            .OrderBy(template => template.TargetVehicleType)
            .ThenBy(template => template.Name)
            .ThenByDescending(template => template.IsPublished)
            .Select(template => new ChecklistTemplateOption(
                template.Id,
                template.Name,
                template.TargetVehicleType,
                template.Status,
                template.IsPublished,
                template.Version))
            .ToListAsync();
    }

    private Task LoadChecklistNameOptionsAsync(int companyId)
    {
        ChecklistNameOptions = new List<string>
        {
            DailyVehicleChecklistName,
            FullAuditChecklistName
        };

        return Task.CompletedTask;
    }

    private async Task LoadTargetVehicleOptionsAsync(int companyId)
    {
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.CompanyId == companyId && vehicle.Status != "Deleted")
            .Select(vehicle => new
            {
                vehicle.VehicleFunction,
                vehicle.VehicleSubtype,
                vehicle.VehicleType
            })
            .ToListAsync();

        var functions = new[]
            {
                VehicleTaxonomyService.AmbulanceFunction,
                VehicleTaxonomyService.ResponseVehicleFunction
            }
            .Concat(vehicles.Select(vehicle =>
                NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType) ?? string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value == VehicleTaxonomyService.AmbulanceFunction ? 0 : value == VehicleTaxonomyService.ResponseVehicleFunction ? 1 : 50)
            .ThenBy(value => value)
            .ToList();

        TargetVehicleFunctionOptions = new List<SelectListItem>
        {
            new("N/A / all functions", string.Empty, string.IsNullOrWhiteSpace(TargetVehicleFunction))
        };
        TargetVehicleFunctionOptions.AddRange(functions.Select(function => new SelectListItem
        {
            Value = function,
            Text = function,
            Selected = string.Equals(TargetVehicleFunction, function, StringComparison.OrdinalIgnoreCase)
        }));

        var subtypes = vehicles
            .Select(vehicle =>
            {
                var function = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
                var subtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);
                return new VehicleTargetSubtypeOption(function, subtype ?? string.Empty);
            })
            .Where(option => !string.IsNullOrWhiteSpace(option.Subtype))
            .GroupBy(option => $"{option.Function ?? string.Empty}||{option.Subtype}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(option => option.Function == VehicleTaxonomyService.AmbulanceFunction ? 0 : option.Function == VehicleTaxonomyService.ResponseVehicleFunction ? 1 : 50)
            .ThenBy(option => option.Function)
            .ThenBy(option => option.Subtype)
            .ToList();

        var target = NormalizeOptional(TargetVehicleType);
        if (!string.IsNullOrWhiteSpace(target) &&
            !string.Equals(target, "All Vehicles", StringComparison.OrdinalIgnoreCase) &&
            !functions.Contains(target, StringComparer.OrdinalIgnoreCase) &&
            !subtypes.Any(option => string.Equals(option.Subtype, target, StringComparison.OrdinalIgnoreCase)))
        {
            subtypes.Add(new VehicleTargetSubtypeOption(
                VehicleTaxonomyService.InferFunction(target),
                target));
        }

        TargetVehicleSubtypeOptions = subtypes;
        TargetVehicleTypeOptions = new[] { "All Vehicles" }
            .Concat(functions)
            .Concat(subtypes.Select(option => option.Subtype))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        SyncTargetVehicleSelectionFromTargetType();
    }

    private async Task LoadPublishControlOptionsAsync(int companyId)
    {
        AppliesTo = NormalizePublishScope(AppliesTo);
        PublishVehicleType = NormalizeOptional(PublishVehicleType);
        PublishVehicleFunction = NormalizeOptional(PublishVehicleFunction);
        PublishVehicleSubtype = NormalizeOptional(PublishVehicleSubtype);

        PublishAreaOptions = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == companyId && area.Status == "Active")
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.Name)
            .Select(area => new SelectListItem
            {
                Value = area.Id.ToString(),
                Text = $"{area.AreaType}: {area.Name}",
                Selected = PublishOperationalAreaId == area.Id
            })
            .ToListAsync();

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == companyId && vehicle.Status != "Deleted")
            .OrderBy(vehicle => vehicle.VehicleFunction)
            .ThenBy(vehicle => vehicle.VehicleSubtype)
            .ThenBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        PublishVehicleOptions = vehicles
            .Select(vehicle => new SelectListItem
            {
                Value = vehicle.Id.ToString(),
                Text = $"{vehicle.Callsign} / {vehicle.RegistrationNumber} - {NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType) ?? "Unassigned function"} / {NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType) ?? vehicle.VehicleType}{(vehicle.CurrentOperationalArea is null ? string.Empty : $" - {vehicle.CurrentOperationalArea.Name}")}",
                Selected = PublishVehicleId == vehicle.Id
            })
            .ToList();

        var functionValues = vehicles
            .Select(vehicle => NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType))
            .Where(function => !string.IsNullOrWhiteSpace(function))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(function => function == VehicleTaxonomyService.AmbulanceFunction ? 0 : function == VehicleTaxonomyService.ResponseVehicleFunction ? 1 : 50)
            .ThenBy(function => function)
            .ToList();

        var subtypeValues = vehicles
            .Select(vehicle => new
            {
                Function = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType),
                Subtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType) ?? vehicle.VehicleType
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Subtype))
            .GroupBy(item => $"{item.Function ?? string.Empty}||{item.Subtype}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Function == VehicleTaxonomyService.AmbulanceFunction ? 0 : item.Function == VehicleTaxonomyService.ResponseVehicleFunction ? 1 : 50)
            .ThenBy(item => item.Function)
            .ThenBy(item => item.Subtype)
            .ToList();

        if (PublishVehicleFunction is null &&
            PublishVehicleSubtype is null &&
            PublishVehicleType is not null)
        {
            if (functionValues.Any(function => string.Equals(function, PublishVehicleType, StringComparison.OrdinalIgnoreCase)))
            {
                PublishVehicleFunction = PublishVehicleType;
            }
            else
            {
                PublishVehicleSubtype = PublishVehicleType;
            }
        }

        PublishVehicleFunctionOptions = functionValues
            .Select(function => new SelectListItem
            {
                Value = function!,
                Text = function!,
                Selected = string.Equals(PublishVehicleFunction, function, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        PublishVehicleSubtypeOptions = subtypeValues
            .Select(item => new SelectListItem
            {
                Value = item.Subtype!,
                Text = string.IsNullOrWhiteSpace(item.Function)
                    ? item.Subtype!
                    : $"{item.Function} / {item.Subtype}",
                Selected = string.Equals(PublishVehicleSubtype, item.Subtype, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(PublishVehicleFunction) &&
            !PublishVehicleFunctionOptions.Any(option => string.Equals(option.Value, PublishVehicleFunction, StringComparison.OrdinalIgnoreCase)))
        {
            PublishVehicleFunctionOptions.Add(new SelectListItem
            {
                Value = PublishVehicleFunction,
                Text = $"Current function: {PublishVehicleFunction}",
                Selected = true
            });
        }

        if (!string.IsNullOrWhiteSpace(PublishVehicleSubtype) &&
            !PublishVehicleSubtypeOptions.Any(option => string.Equals(option.Value, PublishVehicleSubtype, StringComparison.OrdinalIgnoreCase)))
        {
            PublishVehicleSubtypeOptions.Add(new SelectListItem
            {
                Value = PublishVehicleSubtype,
                Text = $"Current subtype: {PublishVehicleSubtype}",
                Selected = true
            });
        }

        PublishVehicleTypeOptions = PublishVehicleFunctionOptions
            .Select(option => new SelectListItem
            {
                Value = option.Value,
                Text = $"Function: {option.Text}",
                Selected = option.Selected
            })
            .Concat(PublishVehicleSubtypeOptions.Select(option => new SelectListItem
            {
                Value = option.Value,
                Text = $"Subtype: {option.Text}",
                Selected = option.Selected
            }))
            .ToList();

        SeniorApproverOptions = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Where(user =>
                user.CompanyId == companyId &&
                user.Status == "Active" &&
                user.AppRole != null &&
                (user.AppRole.Name == "Senior Management" || user.AppRole.Name == "Company Owner"))
            .OrderByDescending(user => user.AppRole!.Name == "Company Owner")
            .ThenBy(user => user.FullName)
            .Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = $"{user.FullName} ({user.AppRole!.Name})",
                Selected = SeniorApproverUserId == user.Id
            })
            .ToListAsync();

        await LoadCurrentLiveUsesAsync(companyId);
        await LoadReplacementWarningsAsync(companyId);
    }

    private async Task LoadCurrentLiveUsesAsync(int companyId)
    {
        if (SelectedTemplateId is null)
        {
            CurrentLiveUses = new List<ChecklistLiveUseRow>();
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
                scope.ChecklistTemplateId == SelectedTemplateId.Value &&
                scope.IsActive &&
                scope.RetiredAtUtc == null &&
                scope.ChecklistTemplate != null &&
                scope.ChecklistTemplate.CompanyId == companyId &&
                scope.ChecklistTemplate.Status != "Deleted")
            .ToListAsync();

        CurrentLiveUses = activeScopes
            .OrderByDescending(scope => ScopeRank(scope.ScopeType))
            .ThenBy(scope => LiveUseTargetLabel(scope))
            .Select(scope => new ChecklistLiveUseRow(
                LiveUseScopeLabel(scope.ScopeType, scope.ChecklistTemplate!.TargetVehicleType),
                LiveUseTargetLabel(scope),
                scope.PublishedAtUtc,
                scope.PublishedByUser != null ? scope.PublishedByUser.FullName : "Senior manager",
                scope.PublishNote))
            .ToList();
    }

    private async Task LoadReplacementWarningsAsync(int companyId)
    {
        var activeScopes = await _db.ChecklistPublishScopes
            .AsNoTracking()
            .Include(scope => scope.ChecklistTemplate)
            .Include(scope => scope.OperationalArea)
            .Include(scope => scope.Vehicle)
                .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
            .Where(scope =>
                scope.IsActive &&
                scope.RetiredAtUtc == null &&
                scope.ChecklistTemplate != null &&
                scope.ChecklistTemplate.CompanyId == companyId &&
                scope.ChecklistTemplate.ChecklistType == "Vehicle" &&
                scope.ChecklistTemplate.Status != "Deleted" &&
                (SelectedTemplateId == null || scope.ChecklistTemplateId != SelectedTemplateId.Value))
            .ToListAsync();

        var warnings = new List<PublishConflictWarning>();
        foreach (var scope in activeScopes)
        {
            var message = ReplacementWarningMessage(scope);
            foreach (var key in ReplacementWarningKeys(scope))
            {
                warnings.Add(new PublishConflictWarning(key, message));
            }
        }

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == companyId && vehicle.Status != "Deleted")
            .OrderBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        foreach (var vehicle in vehicles)
        {
            var effectiveScope = activeScopes
                .Where(scope => PublishScopeAppliesToVehicle(scope, vehicle))
                .OrderByDescending(scope => ScopeRank(scope.ScopeType))
                .FirstOrDefault();

            if (effectiveScope?.ChecklistTemplate is null)
            {
                continue;
            }

            warnings.Add(new PublishConflictWarning(
                $"Vehicle:{vehicle.Id}",
                $"Warning: '{effectiveScope.ChecklistTemplate.Name}' is currently the live daily check for {vehicle.Callsign} / {vehicle.RegistrationNumber}. Publishing to this callsign will replace or override the live daily check for that unit."));
        }

        ConflictWarnings = warnings
            .GroupBy(warning => new { warning.ScopeKey, warning.Message })
            .Select(group => group.First())
            .OrderBy(warning => warning.ScopeKey)
            .ToList();
    }

    private async Task ApplySelectedTemplateAsync(int companyId)
    {
        if (SelectedTemplateId is null)
        {
            return;
        }

        var template = await _db.ChecklistTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.CompanyId == companyId &&
                item.Id == SelectedTemplateId &&
                item.Status != "Deleted");

        if (template is null)
        {
            SelectedTemplateId = null;
            CreateAsNewTemplate = true;
            return;
        }

        SelectedTemplateId = template.Id;
        ChecklistName = ResolveChecklistName(template.Name, template.Name);
        TargetVehicleType = template.TargetVehicleType;
        ChecklistStatus = template.Status;
    }

    private async Task<ChecklistTemplate> SaveTemplateAsync(AppUser currentUser, bool publish)
    {
        var now = DateTime.UtcNow;
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(item => item.Id == currentUser.CompanyId);
        var template = SelectedTemplateId is null
            ? null
            : await _db.ChecklistTemplates
                .Include(item => item.Sections)
                .ThenInclude(section => section.Items)
                .ThenInclude(item => item.ColumnDefinitions)
                .Include(item => item.PublishScopes)
                .ThenInclude(scope => scope.OperationalArea)
                .Include(item => item.PublishScopes)
                .ThenInclude(scope => scope.Vehicle)
                .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
                .AsSplitQuery()
                .FirstOrDefaultAsync(item => item.CompanyId == currentUser.CompanyId && item.Id == SelectedTemplateId);
        var isNewTemplate = template is null;

        if (template is null)
        {
            template = new ChecklistTemplate
            {
                CompanyId = currentUser.CompanyId,
                ClientName = CompanyBranding.GetDisplayCompanyName(company),
                CreatedAtUtc = now
            };
            _db.ChecklistTemplates.Add(template);
        }

        template.Name = ChecklistName?.Trim() ?? string.Empty;
        template.ChecklistType = "Vehicle";
        template.TargetVehicleType = TargetVehicleType;
        template.Version = string.IsNullOrWhiteSpace(template.Version) ? "1.0" : template.Version;
        template.SourceType = SelectedTemplateId is null ? "Built" : "Edited";
        template.CreatedByUserId ??= currentUser.Id;
        template.Status = publish
            ? "Draft"
            : string.Equals(ChecklistStatus, "Published", StringComparison.OrdinalIgnoreCase) ? "Under Review" : ChecklistStatus;
        template.IsPublished = publish ? false : template.IsPublished && template.Status == "Published";
        template.PublishNotes = NormalizeOptional(PublishNote);
        template.UpdatedAtUtc = now;

        _db.ChecklistColumnDefinitions.RemoveRange(template.Sections.SelectMany(section => section.Items).SelectMany(item => item.ColumnDefinitions));
        var existingItems = template.Sections.SelectMany(section => section.Items).ToList();
        _db.ChecklistItems.RemoveRange(existingItems.Where(item => item.ParentChecklistItemId is not null));
        _db.ChecklistItems.RemoveRange(existingItems.Where(item => item.ParentChecklistItemId is null));
        _db.ChecklistSections.RemoveRange(template.Sections);

        foreach (var section in VehicleChecklistSections.Select((section, index) => new { Section = section, Index = index }))
        {
            var templateSection = new ChecklistSection
            {
                ChecklistTemplate = template,
                Name = section.Section.Title,
                DisplayOrder = (section.Index + 1) * 10
            };

            if (section.Section.Kind == ChecklistSectionKind.EquipmentTable)
            {
                var matrixRows = section.Section.MatrixRows.Count > 0
                    ? section.Section.MatrixRows
                    : EquipmentRows;
                var matrixColumns = section.Section.MatrixColumns.Count > 0
                    ? section.Section.MatrixColumns
                    : EquipmentColumns;

                foreach (var rowInput in matrixRows.Select((row, index) => new { Row = row, Index = index }))
                {
                    var row = new ChecklistItem
                    {
                        Prompt = rowInput.Row.Name,
                        ResponseType = "EquipmentRow",
                        ItemKind = "EquipmentRow",
                        EquipmentType = NormalizeOptional(rowInput.Row.EquipmentType) ?? rowInput.Row.Name,
                        Model = NormalizeOptional(rowInput.Row.Model),
                        FieldKey = FieldKey(rowInput.Row.Name),
                        RequiresCommentOnFail = true,
                        IsRequired = rowInput.Row.IsRequired,
                        IsReadinessCritical = rowInput.Row.IsReadinessCritical,
                        AllowsSameAsPrevious = rowInput.Row.AllowsSameAsPrevious,
                        DisplayOrder = rowInput.Index + 1
                    };

                    foreach (var column in matrixColumns.Select((column, index) => new { Column = column, Index = index }))
                    {
                        row.ColumnDefinitions.Add(BuildColumnDefinition(
                            column.Column,
                            column.Index + 1,
                            FindColumnOverride(rowInput.Row.ColumnOverrides, column.Index)));
                    }

                    templateSection.Items.Add(row);

                    foreach (var subItemInput in rowInput.Row.SubItems.Select((subItem, index) => new { SubItem = subItem, Index = index }))
                    {
                        var subItemName = NormalizeOptional(subItemInput.SubItem.Name) ?? "Equipment subitem";
                        var subItem = new ChecklistItem
                        {
                            ChecklistSection = templateSection,
                            ParentChecklistItem = row,
                            Prompt = subItemName,
                            ResponseType = "EquipmentRow",
                            ItemKind = "EquipmentSubItem",
                            EquipmentType = NormalizeOptional(subItemInput.SubItem.EquipmentType) ?? row.EquipmentType,
                            Model = NormalizeOptional(subItemInput.SubItem.Model),
                            FieldKey = FieldKey($"{row.Prompt}-{subItemName}"),
                            RequiresCommentOnFail = true,
                            IsRequired = subItemInput.SubItem.IsRequired,
                            IsReadinessCritical = subItemInput.SubItem.IsReadinessCritical,
                            AllowsSameAsPrevious = subItemInput.SubItem.AllowsSameAsPrevious,
                            DisplayOrder = subItemInput.Index + 1
                        };

                        foreach (var column in matrixColumns.Select((column, index) => new { Column = column, Index = index }))
                        {
                            subItem.ColumnDefinitions.Add(BuildColumnDefinition(
                                column.Column,
                                column.Index + 1,
                                FindColumnOverride(subItemInput.SubItem.ColumnOverrides, column.Index)));
                        }

                        templateSection.Items.Add(subItem);
                    }
                }
            }
            else
            {
                foreach (var field in section.Section.Fields.Select((field, index) => new { Field = field, Index = index }))
                {
                    if (IsSchematicField(field.Field))
                    {
                        AddSchematicBlockItems(templateSection, field.Field, field.Index + 1);
                        continue;
                    }

                    var item = new ChecklistItem
                    {
                        Prompt = field.Field.Label,
                        ResponseType = field.Field.Type,
                        ItemKind = "Field",
                        FieldKey = FieldKey(field.Field.Label),
                        RequiresCommentOnFail = section.Section.Kind is ChecklistSectionKind.Schematic,
                        IsRequired = field.Field.IsRequired,
                        IsReadinessCritical = field.Field.IsRequired && !field.Field.IsSystemLinked,
                        AllowsSameAsPrevious = !field.Field.IsSystemLinked,
                        DefaultLocation = field.Field.Source,
                        DisplayOrder = field.Index + 1
                    };

                    item.ColumnDefinitions.Add(BuildColumnDefinition(field.Field, 1));
                    templateSection.Items.Add(item);
                }
            }

            AddSectionNoteItems(templateSection, section.Section);
            _db.ChecklistSections.Add(templateSection);
        }

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = isNewTemplate ? "Checklist template created" : "Checklist template edited",
            EntityType = "ChecklistTemplate",
            EntityId = template.Id,
            Details = $"{currentUser.FullName} {(isNewTemplate ? "created" : "edited")} '{template.Name}' for {template.TargetVehicleType}. Status: {template.Status}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        return template;
    }

    private static void AddSectionNoteItems(ChecklistSection templateSection, ChecklistSectionEditorInput section)
    {
        foreach (var noteInput in section.Notes.Select((note, index) => new { Note = note, Index = index }))
        {
            var noteLabel = NormalizeOptional(noteInput.Note.Label) ?? $"{section.Title} notes";
            templateSection.Items.Add(new ChecklistItem
            {
                Prompt = noteLabel,
                ResponseType = "Text",
                ItemKind = SectionNoteItemKind,
                FieldKey = FieldKey($"{section.Title}-{noteLabel}"),
                RequiresCommentOnFail = false,
                IsRequired = false,
                IsReadinessCritical = false,
                AllowsSameAsPrevious = false,
                DefaultLocation = "Section notes",
                DisplayOrder = 900 + noteInput.Index
            });
        }
    }

    private static void AddSchematicBlockItems(
        ChecklistSection templateSection,
        ChecklistFieldEditorInput field,
        int displayOrder)
    {
        var fieldLabel = NormalizeOptional(field.Label) ?? "Unit Schematic";
        var schematicKey = NormalizeOptional(field.UnitSchematicKey) ?? string.Empty;
        const string source = "Unit Schematic library";

        var parentItem = new ChecklistItem
        {
            Prompt = fieldLabel,
            ResponseType = "Schematic Markup",
            ItemKind = SchematicBlockItemKind,
            FieldKey = FieldKey(fieldLabel),
            Model = schematicKey,
            RequiresCommentOnFail = true,
            IsRequired = field.IsRequired,
            IsReadinessCritical = true,
            AllowsSameAsPrevious = false,
            DefaultLocation = source,
            DisplayOrder = displayOrder
        };

        var parentField = new ChecklistFieldEditorInput(
            fieldLabel,
            "Schematic Markup",
            field.IsRequired,
            true,
            true,
            source)
        {
            UnitSchematicKey = schematicKey
        };

        parentItem.ColumnDefinitions.Add(BuildColumnDefinition(parentField, 1));
        templateSection.Items.Add(parentItem);

        foreach (var schematicView in UnitSchematicViews.Select((view, index) => new { View = view, Index = index }))
        {
            var viewItem = new ChecklistItem
            {
                ChecklistSection = templateSection,
                ParentChecklistItem = parentItem,
                Prompt = schematicView.View,
                ResponseType = "Schematic View",
                ItemKind = SchematicViewItemKind,
                FieldKey = FieldKey($"{fieldLabel}-{schematicView.View}"),
                Model = schematicKey,
                RequiresCommentOnFail = true,
                IsRequired = field.IsRequired,
                IsReadinessCritical = true,
                AllowsSameAsPrevious = false,
                DefaultLocation = source,
                DisplayOrder = schematicView.Index + 1
            };

            foreach (var column in BuildSchematicViewColumns())
            {
                viewItem.ColumnDefinitions.Add(column);
            }

            templateSection.Items.Add(viewItem);
        }
    }

    private async Task<vector_app_local.Models.AppUser?> LoadCurrentAuthorityAsync(bool loadPublishedSettings)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        IsSeniorChecklistPublisher = CurrentUserService.IsSeniorAccessRole(currentUser?.AppRole?.Name);
        HasDelegatedPublishAccess = currentUser is not null && await HasChecklistPublishTaskAccessAsync(currentUser);

        if (loadPublishedSettings && currentUser is not null)
        {
            var settings = await _db.Companies
                .AsNoTracking()
                .Where(company => company.Id == currentUser.CompanyId)
                .Select(company => new
                {
                    company.AllowSameAsPreviousVehicleInspection,
                    company.AllowSameAsPreviousEquipmentCheck
                })
                .FirstOrDefaultAsync();

            if (settings is not null)
            {
                AllowSameAsPreviousVehicleInspection = settings.AllowSameAsPreviousVehicleInspection;
                AllowSameAsPreviousEquipmentCheck = settings.AllowSameAsPreviousEquipmentCheck;
            }
        }

        return currentUser;
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

    private async Task CreateChecklistApprovalTaskAsync(AppUser currentUser, ChecklistTemplate template)
    {
        if (SeniorApproverUserId is null)
        {
            return;
        }

        var seniorApprover = await _db.AppUsers
            .Include(user => user.AppRole)
            .FirstOrDefaultAsync(user =>
                user.Id == SeniorApproverUserId.Value &&
                user.CompanyId == currentUser.CompanyId &&
                user.Status == "Active" &&
                user.AppRole != null &&
                (user.AppRole.Name == "Senior Management" || user.AppRole.Name == "Company Owner"));

        if (seniorApprover is null)
        {
            StatusMessage = "Select an active senior manager for approval.";
            return;
        }

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            CompanyId = currentUser.CompanyId,
            AssignedToUserId = seniorApprover.Id,
            AssignedByUserId = currentUser.Id,
            ActionType = "Checklist approval request",
            RelatedItemReference = $"ChecklistTemplate:{template.Id}",
            InstructionMessage = $"{currentUser.FullName} submitted '{template.Name}' for {template.TargetVehicleType}. Requested live-use scope: {ResolvePublishScopeSummary()}. Notes: {NormalizeOptional(PublishNote) ?? "No note entered."}",
            Status = "Open",
            CreatedAtUtc = now
        };

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        _db.TaskEvents.Add(new TaskEvent
        {
            TaskItemId = task.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Sent",
            Notes = "Checklist approval request submitted from Checklist Builder.",
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Checklist approval requested",
            EntityType = "ChecklistTemplate",
            EntityId = template.Id,
            Details = $"{currentUser.FullName} submitted checklist template #{template.Id} to {seniorApprover.FullName} for approval.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
    }

    private static string ResolveChecklistName(string? checklist, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(checklist))
        {
            return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
        }

        var normalized = checklist.Trim().ToLowerInvariant();
        return normalized switch
        {
            "daily-vehicle" or "daily vehicle" or "daily vehicle checklist" or "daily vehicle inspection" or "daily vehicle readiness" or "daily vehicle check" or "daily vehicle & equipment check" => DailyVehicleChecklistName,
            "full-audit" or "full audit" or "audit" => FullAuditChecklistName,
            _ => checklist
        };
    }

    private static bool IsVehicleChecklistName(string? checklistName)
    {
        return string.Equals(checklistName, DailyVehicleChecklistName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(checklistName, LegacyDailyVehicleChecklistName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(checklistName, FullAuditChecklistName, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTargetVehicleType(string? targetVehicleType)
    {
        return string.IsNullOrWhiteSpace(targetVehicleType) ? string.Empty : targetVehicleType.Trim();
    }

    private bool ApplyManualEntryValues()
    {
        if (UseCustomChecklistName)
        {
            var manualChecklistName = NormalizeOptional(CustomChecklistName);
            if (manualChecklistName is null)
            {
                StatusMessage = "Enter a manual checklist name, or switch back to the saved checklist name list.";
                return false;
            }

            ChecklistName = manualChecklistName;
        }
        else
        {
            ChecklistName = ResolveChecklistName(ChecklistName, ChecklistName);
        }

        if (UseCustomTargetVehicleType)
        {
            var manualVehicleType = NormalizeOptional(CustomTargetVehicleType);
            if (manualVehicleType is null)
            {
                StatusMessage = "Enter a manual vehicle type, or switch back to the saved vehicle type list.";
                return false;
            }

            TargetVehicleType = manualVehicleType;
            TargetVehicleSubtype = manualVehicleType;
            TargetVehicleFunction = NormalizeOptional(TargetVehicleFunction) ?? VehicleTaxonomyService.InferFunction(manualVehicleType);
        }
        else
        {
            TargetVehicleFunction = NormalizeOptional(TargetVehicleFunction);
            TargetVehicleSubtype = NormalizeOptional(TargetVehicleSubtype);
            TargetVehicleType = NormalizeTargetVehicleType(TargetVehicleSubtype ?? TargetVehicleFunction ?? TargetVehicleType);
        }

        return true;
    }

    private void SyncManualEntryControls()
    {
        ChecklistName = ResolveChecklistName(ChecklistName, ChecklistName);
        TargetVehicleType = NormalizeTargetVehicleType(TargetVehicleType);
        SyncTargetVehicleSelectionFromTargetType();

        if (string.IsNullOrWhiteSpace(ChecklistName))
        {
            UseCustomChecklistName = false;
            CustomChecklistName = null;
        }
        else if (ChecklistNameOptions.Any(option => string.Equals(option, ChecklistName, StringComparison.OrdinalIgnoreCase)))
        {
            CustomChecklistName ??= ChecklistName;
        }
        else
        {
            UseCustomChecklistName = true;
            CustomChecklistName = ChecklistName;
        }

        if (string.IsNullOrWhiteSpace(TargetVehicleType))
        {
            UseCustomTargetVehicleType = false;
            CustomTargetVehicleType = null;
        }
        else if (TargetVehicleTypeOptions.Any(option => string.Equals(option, TargetVehicleType, StringComparison.OrdinalIgnoreCase)))
        {
            CustomTargetVehicleType ??= TargetVehicleType;
        }
        else
        {
            UseCustomTargetVehicleType = true;
            CustomTargetVehicleType = TargetVehicleType;
        }
    }

    private void SyncTargetVehicleSelectionFromTargetType()
    {
        var target = NormalizeOptional(TargetVehicleType);
        if (target is null || string.Equals(target, "All Vehicles", StringComparison.OrdinalIgnoreCase))
        {
            TargetVehicleFunction = null;
            TargetVehicleSubtype = null;
            return;
        }

        var subtypeOption = TargetVehicleSubtypeOptions.FirstOrDefault(option =>
            string.Equals(option.Subtype, target, StringComparison.OrdinalIgnoreCase));
        if (subtypeOption is not null)
        {
            TargetVehicleFunction = subtypeOption.Function;
            TargetVehicleSubtype = subtypeOption.Subtype;
            return;
        }

        var functionOption = TargetVehicleFunctionOptions.FirstOrDefault(option =>
            string.Equals(option.Value, target, StringComparison.OrdinalIgnoreCase));
        if (functionOption is not null)
        {
            TargetVehicleFunction = functionOption.Value;
            TargetVehicleSubtype = null;
            return;
        }

        TargetVehicleFunction = VehicleTaxonomyService.InferFunction(target);
        TargetVehicleSubtype = target;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string ResolvePublishScopeSummary()
    {
        return ResolvePublishScopeType() switch
        {
            "OperationalArea" => $"Base / operational area: {PublishAreaOptions.FirstOrDefault(area => area.Value == PublishOperationalAreaId?.ToString())?.Text ?? "Specific operational area"}",
            "VehicleFunction" => $"Vehicle function: {NormalizeOptional(PublishVehicleFunction) ?? "No function selected"}",
            "VehicleSubtype" => $"Vehicle subtype: {NormalizeOptional(PublishVehicleSubtype) ?? "No subtype selected"}",
            "Vehicle" => $"Callsign / registration: {PublishVehicleOptions.FirstOrDefault(vehicle => vehicle.Value == PublishVehicleId?.ToString())?.Text ?? "Specific vehicle registration"}",
            _ => "All areas and all eligible vehicles"
        };
    }

    private static int ScopeRank(string scopeType) => scopeType switch
    {
        "Vehicle" => 5,
        "OperationalArea" => 4,
        "VehicleSubtype" => 3,
        "VehicleFunction" => 2,
        "VehicleCategory" => 2,
        "AllAreas" => 1,
        _ => 0
    };

    private static string LiveUseScopeLabel(string scopeType, string targetVehicleType)
    {
        return scopeType switch
        {
            "Vehicle" => "Specific callsign / registration",
            "OperationalArea" => "Specific area / base",
            "VehicleFunction" => "Vehicle function",
            "VehicleSubtype" => "Vehicle subtype",
            "VehicleCategory" => ResolveLegacyScopeLabel(targetVehicleType),
            "AllAreas" => "All areas / eligible vehicles",
            _ => "Live use"
        };
    }

    private static string ResolveLegacyScopeLabel(string targetVehicleType)
    {
        return string.Equals(targetVehicleType, VehicleTaxonomyService.AmbulanceFunction, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(targetVehicleType, VehicleTaxonomyService.ResponseVehicleFunction, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase)
            ? "Vehicle function"
            : "Vehicle subtype";
    }

    private static string LiveUseTargetLabel(ChecklistPublishScope scope)
    {
        return scope.ScopeType switch
        {
            "Vehicle" => scope.Vehicle is null
                ? "Selected callsign / registration"
                : $"{scope.Vehicle.Callsign} / {scope.Vehicle.RegistrationNumber}{FormatVehicleArea(scope.Vehicle)}",
            "OperationalArea" => scope.OperationalArea is null
                ? "Selected area / base"
                : $"{scope.OperationalArea.AreaType}: {scope.OperationalArea.Name}",
            "VehicleFunction" => scope.ChecklistTemplate?.TargetVehicleType ?? "Selected function",
            "VehicleSubtype" => scope.ChecklistTemplate?.TargetVehicleType ?? "Selected subtype",
            "VehicleCategory" => scope.ChecklistTemplate?.TargetVehicleType ?? "Selected vehicle target",
            "AllAreas" => "All areas / all eligible vehicles",
            _ => scope.ChecklistTemplate?.PublishScopeSummary ?? "Live use target"
        };
    }

    private static string FormatVehicleArea(Vehicle vehicle)
    {
        return vehicle.CurrentOperationalArea is null
            ? string.Empty
            : $" - {vehicle.CurrentOperationalArea.Name}";
    }

    private static IEnumerable<string> ReplacementWarningKeys(ChecklistPublishScope scope)
    {
        switch (scope.ScopeType)
        {
            case "AllAreas":
                yield return "AllAreas";
                break;
            case "OperationalArea" when scope.OperationalAreaId is not null:
                yield return $"OperationalArea:{scope.OperationalAreaId}";
                break;
            case "Vehicle" when scope.VehicleId is not null:
                yield return $"Vehicle:{scope.VehicleId}";
                break;
            case "VehicleFunction":
                yield return $"VehicleFunction:{scope.ChecklistTemplate?.TargetVehicleType}";
                break;
            case "VehicleSubtype":
                yield return $"VehicleSubtype:{scope.ChecklistTemplate?.TargetVehicleType}";
                break;
            case "VehicleCategory":
                var target = scope.ChecklistTemplate?.TargetVehicleType ?? string.Empty;
                yield return $"{(ResolveLegacyScopeLabel(target) == "Vehicle function" ? "VehicleFunction" : "VehicleSubtype")}:{target}";
                break;
        }
    }

    private static string ReplacementWarningMessage(ChecklistPublishScope scope)
    {
        var checklistName = scope.ChecklistTemplate?.Name ?? "Another checklist";
        return $"Warning: '{checklistName}' is already active for {LiveUseTargetLabel(scope)}. Publishing to this same target will replace the live daily check for that target.";
    }

    private static bool PublishScopeAppliesToVehicle(ChecklistPublishScope scope, Vehicle vehicle)
    {
        if (scope.ChecklistTemplate is null ||
            !VehicleMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType))
        {
            return false;
        }

        return scope.ScopeType switch
        {
            "Vehicle" => scope.VehicleId == vehicle.Id,
            "OperationalArea" => scope.OperationalAreaId == vehicle.CurrentOperationalAreaId,
            "VehicleFunction" => VehicleFunctionMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType),
            "VehicleSubtype" => VehicleSubtypeMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType),
            "VehicleCategory" => VehicleMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType),
            "AllAreas" => VehicleMatchesTemplateTarget(vehicle, scope.ChecklistTemplate.TargetVehicleType),
            _ => false
        };
    }

    private static bool VehicleMatchesTemplateTarget(Vehicle vehicle, string targetVehicleType)
    {
        if (string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var function = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        var subtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);

        return string.Equals(function, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(subtype, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicle.VehicleType, targetVehicleType, StringComparison.OrdinalIgnoreCase);
    }

    private static bool VehicleFunctionMatchesTemplateTarget(Vehicle vehicle, string targetVehicleType)
    {
        var function = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        return string.Equals(function, targetVehicleType, StringComparison.OrdinalIgnoreCase);
    }

    private static bool VehicleSubtypeMatchesTemplateTarget(Vehicle vehicle, string targetVehicleType)
    {
        var subtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);
        return string.Equals(subtype, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicle.VehicleType, targetVehicleType, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyPublishTargetToTemplateTarget()
    {
        var publishScopeType = ResolvePublishScopeType();
        if (publishScopeType == "VehicleFunction")
        {
            var publishVehicleFunction = NormalizeOptional(PublishVehicleFunction);
            if (publishVehicleFunction is not null)
            {
                TargetVehicleType = publishVehicleFunction;
            }
            return;
        }

        if (publishScopeType == "VehicleSubtype")
        {
            var publishVehicleSubtype = NormalizeOptional(PublishVehicleSubtype);
            if (publishVehicleSubtype is not null)
            {
                TargetVehicleType = publishVehicleSubtype;
            }
            return;
        }

        if (publishScopeType == "Vehicle" && PublishVehicleId is not null)
        {
            var selectedVehicle = PublishVehicleOptions.FirstOrDefault(vehicle => vehicle.Value == PublishVehicleId.Value.ToString());
            if (selectedVehicle is not null)
            {
                var labelParts = selectedVehicle.Text.Split(" - ", StringSplitOptions.None);
                if (labelParts.Length > 1)
                {
                    var vehicleTargetParts = labelParts[1].Split(" / ", StringSplitOptions.None);
                    TargetVehicleType = NormalizeOptional(vehicleTargetParts.LastOrDefault()) ?? TargetVehicleType;
                }
            }
        }
    }

    private string ResolvePublishScopeType()
    {
        return NormalizePublishScope(AppliesTo) switch
        {
            "Specific base" => "OperationalArea",
            "Vehicle function" => "VehicleFunction",
            "Vehicle subtype" => "VehicleSubtype",
            "Specific vehicle registration" => "Vehicle",
            _ => "AllAreas"
        };
    }

    private static string NormalizePublishScope(string? value)
    {
        return value switch
        {
            "Specific base" or "Specific operational area" => "Specific base",
            "Vehicle type" or "Specific vehicle category" or "Vehicle function / subtype" => "Vehicle subtype",
            "Vehicle function" => "Vehicle function",
            "Vehicle subtype" => "Vehicle subtype",
            "Specific vehicle registration" or "Specific callsign / registration" => "Specific vehicle registration",
            _ => "All operational areas"
        };
    }

    private static ChecklistColumnDefinition BuildColumnDefinition(ChecklistFieldEditorInput field, int sortOrder)
    {
        return new ChecklistColumnDefinition
        {
            Heading = field.Label,
            FieldKey = FieldKey(field.Label),
            ResponseType = field.Type,
            RegisterSource = field.Source,
            IsRequired = field.IsRequired,
            IsEditable = field.IsEditable,
            AllowsNotApplicable = true,
            PullsFromRegister = field.IsSystemLinked,
            AffectsReadiness = field.IsRequired && !field.IsSystemLinked,
            SameAsPreviousEligible = !field.IsSystemLinked || field.Label is "S/N / ID" or "Next Service",
            RequiresNoteWhenNotNormal = field.Type.Contains("Dropdown", StringComparison.OrdinalIgnoreCase) ||
                field.Type.Contains("Yes", StringComparison.OrdinalIgnoreCase),
            ReadinessImpact = field.IsRequired && !field.IsSystemLinked ? "Warning" : "None",
            DropdownOptionsJson = DefaultDropdownOptionsJson(field.Label, field.Type),
            SortOrder = sortOrder
        };
    }

    private static List<ChecklistColumnDefinition> BuildSchematicViewColumns()
    {
        return new List<ChecklistColumnDefinition>
        {
            new()
            {
                Heading = "Damage present",
                FieldKey = "damage-present",
                ResponseType = "Yes/No",
                RegisterSource = "Fresh entry",
                IsRequired = true,
                IsEditable = true,
                AllowsNotApplicable = true,
                PullsFromRegister = false,
                AffectsReadiness = true,
                SameAsPreviousEligible = false,
                RequiresNoteWhenNotNormal = true,
                ReadinessImpact = "Warning",
                DropdownOptionsJson = JsonSerializer.Serialize(new[] { "N/A", "Yes", "No" }),
                SortOrder = 1
            },
            new()
            {
                Heading = "Mark damage position",
                FieldKey = "mark-damage-position",
                ResponseType = "Schematic Markup",
                RegisterSource = "Unit Schematic library",
                IsRequired = false,
                IsEditable = true,
                AllowsNotApplicable = true,
                PullsFromRegister = true,
                AffectsReadiness = true,
                SameAsPreviousEligible = false,
                RequiresNoteWhenNotNormal = true,
                ReadinessImpact = "Warning",
                SortOrder = 2
            },
            new()
            {
                Heading = "Severity",
                FieldKey = "severity",
                ResponseType = "Dropdown",
                RegisterSource = "Fresh entry",
                IsRequired = false,
                IsEditable = true,
                AllowsNotApplicable = true,
                PullsFromRegister = false,
                AffectsReadiness = true,
                SameAsPreviousEligible = false,
                RequiresNoteWhenNotNormal = true,
                ReadinessImpact = "Warning",
                DropdownOptionsJson = JsonSerializer.Serialize(new[] { "N/A", "Minor", "Moderate", "Severe", "Out of service" }),
                SortOrder = 3
            },
            new()
            {
                Heading = "View notes",
                FieldKey = "view-notes",
                ResponseType = "Text",
                RegisterSource = "Fresh entry",
                IsRequired = false,
                IsEditable = true,
                AllowsNotApplicable = true,
                PullsFromRegister = false,
                AffectsReadiness = false,
                SameAsPreviousEligible = false,
                RequiresNoteWhenNotNormal = false,
                ReadinessImpact = "None",
                SortOrder = 4
            },
            new()
            {
                Heading = "Photo",
                FieldKey = "photo",
                ResponseType = "Photo Upload",
                RegisterSource = "Fresh entry",
                IsRequired = false,
                IsEditable = true,
                AllowsNotApplicable = true,
                PullsFromRegister = false,
                AffectsReadiness = false,
                SameAsPreviousEligible = false,
                RequiresNoteWhenNotNormal = false,
                ReadinessImpact = "None",
                SortOrder = 5
            },
            new()
            {
                Heading = "Readiness impact",
                FieldKey = "readiness-impact",
                ResponseType = "Yes/No",
                RegisterSource = "Fresh entry",
                IsRequired = false,
                IsEditable = true,
                AllowsNotApplicable = true,
                PullsFromRegister = false,
                AffectsReadiness = true,
                SameAsPreviousEligible = false,
                RequiresNoteWhenNotNormal = true,
                ReadinessImpact = "Warning",
                DropdownOptionsJson = JsonSerializer.Serialize(new[] { "N/A", "Yes", "No" }),
                SortOrder = 6
            }
        };
    }

    private static ChecklistColumnDefinition BuildColumnDefinition(
        EquipmentColumnEditorInput field,
        int sortOrder,
        EquipmentColumnOverrideInput? rowOverride = null)
    {
        var definition = new ChecklistColumnDefinition
        {
            Heading = field.Label,
            FieldKey = FieldKey(field.Label),
            ResponseType = field.Type,
            RegisterSource = NormalizeOptional(field.Source),
            IsRequired = field.IsRequired,
            IsEditable = field.IsEditable,
            AllowsNotApplicable = true,
            PullsFromRegister = field.IsSystemLinked,
            AffectsReadiness = field.AffectsReadiness,
            SameAsPreviousEligible = field.SameAsPreviousEligible,
            RequiresNoteWhenNotNormal = field.RequiresNoteWhenNotNormal,
            ReadinessImpact = field.AffectsReadiness ? "Warning" : "None",
            DropdownOptionsJson = DefaultDropdownOptionsJson(field.Label, field.Type),
            SortOrder = sortOrder
        };

        ApplyColumnOverride(definition, rowOverride);
        return definition;
    }

    private static void ApplyColumnOverride(ChecklistColumnDefinition definition, EquipmentColumnOverrideInput? rowOverride)
    {
        var mode = NormalizeColumnOverrideMode(rowOverride?.Mode);
        switch (mode)
        {
            case EquipmentColumnOverrideModes.NotApplicable:
                definition.ResponseType = "N/A";
                definition.RegisterSource = "Not applicable for this row";
                definition.IsRequired = false;
                definition.IsEditable = false;
                definition.PullsFromRegister = false;
                definition.AffectsReadiness = false;
                definition.SameAsPreviousEligible = false;
                definition.RequiresNoteWhenNotNormal = false;
                definition.ReadinessImpact = "None";
                definition.DropdownOptionsJson = JsonSerializer.Serialize(new[] { "N/A" });
                break;

            case EquipmentColumnOverrideModes.Optional:
                definition.IsRequired = false;
                definition.AffectsReadiness = false;
                definition.RequiresNoteWhenNotNormal = false;
                definition.ReadinessImpact = "None";
                break;

            case EquipmentColumnOverrideModes.Required:
                definition.IsRequired = true;
                break;
        }
    }

    private async Task LoadEquipmentEditorFromTemplateAsync(int companyId)
    {
        if (SelectedTemplateId is null)
        {
            return;
        }

        var equipmentSection = await _db.ChecklistSections
            .AsNoTracking()
            .Include(section => section.Items)
            .ThenInclude(item => item.ColumnDefinitions)
            .Where(section =>
                section.ChecklistTemplate != null &&
                section.ChecklistTemplate.CompanyId == companyId &&
                section.ChecklistTemplateId == SelectedTemplateId.Value)
            .OrderBy(section => section.DisplayOrder)
            .FirstOrDefaultAsync(section =>
                section.Name == "Carried Equipment" ||
                section.Items.Any(item => item.ItemKind == "EquipmentRow"));

        if (equipmentSection is null)
        {
            return;
        }

        var subItemsByParentId = equipmentSection.Items
            .Where(item => item.ParentChecklistItemId is not null)
            .GroupBy(item => item.ParentChecklistItemId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.DisplayOrder).ToList());

        var rows = equipmentSection.Items
            .Where(item =>
                (item.ItemKind == "EquipmentRow" || item.ResponseType == "EquipmentRow") &&
                item.ParentChecklistItemId is null)
            .OrderBy(item => item.DisplayOrder)
            .ToList();

        if (rows.Count > 0)
        {
            var templateColumns = rows
                .SelectMany(item => item.ColumnDefinitions)
                .GroupBy(column => column.FieldKey)
                .Select(group => group
                    .OrderBy(column => IsNotApplicableColumn(column))
                    .ThenBy(column => column.SortOrder)
                    .First())
                .OrderBy(column => column.SortOrder)
                .ToList();

            if (templateColumns.Count > 0)
            {
                EquipmentColumns = templateColumns
                    .Select(column => new EquipmentColumnEditorInput
                    {
                        Label = column.Heading,
                        Type = IsNotApplicableColumn(column) ? "Text" : column.ResponseType,
                        Source = IsNotApplicableColumn(column) ? "Fresh entry" : column.RegisterSource,
                        IsRequired = column.IsRequired,
                        IsEditable = column.IsEditable,
                        IsSystemLinked = column.PullsFromRegister,
                        AffectsReadiness = column.AffectsReadiness,
                        SameAsPreviousEligible = column.SameAsPreviousEligible,
                        RequiresNoteWhenNotNormal = column.RequiresNoteWhenNotNormal
                    })
                    .ToList();
            }

            EquipmentRows = rows
                .Select(item => new EquipmentRowEditorInput
                {
                    Name = item.Prompt,
                    EquipmentType = item.EquipmentType,
                    Model = item.Model,
                    IsRequired = item.IsRequired,
                    IsReadinessCritical = item.IsReadinessCritical,
                    AllowsSameAsPrevious = item.AllowsSameAsPrevious,
                    ColumnOverrides = BuildColumnOverrides(item.ColumnDefinitions, EquipmentColumns),
                    SubItems = subItemsByParentId.TryGetValue(item.Id, out var childRows)
                        ? childRows
                            .Select(subItem => new EquipmentSubItemEditorInput
                            {
                                Name = subItem.Prompt,
                                EquipmentType = subItem.EquipmentType,
                                Model = subItem.Model,
                                IsRequired = subItem.IsRequired,
                                IsReadinessCritical = subItem.IsReadinessCritical,
                                AllowsSameAsPrevious = subItem.AllowsSameAsPrevious,
                                ColumnOverrides = BuildColumnOverrides(subItem.ColumnDefinitions, EquipmentColumns)
                            })
                            .ToList()
                        : new List<EquipmentSubItemEditorInput>()
                })
                .ToList();
        }
    }

    private async Task LoadChecklistSectionEditorFromTemplateAsync(int companyId)
    {
        if (SelectedTemplateId is null)
        {
            return;
        }

        var sections = await _db.ChecklistSections
            .AsNoTracking()
            .Include(section => section.Items)
            .ThenInclude(item => item.ColumnDefinitions)
            .Where(section =>
                section.ChecklistTemplate != null &&
                section.ChecklistTemplate.CompanyId == companyId &&
                section.ChecklistTemplateId == SelectedTemplateId.Value)
            .OrderBy(section => section.DisplayOrder)
            .AsSplitQuery()
            .ToListAsync();

        if (sections.Count == 0)
        {
            return;
        }

        VehicleChecklistSections = sections
            .Select(section =>
            {
                var isEquipmentSection = section.Items.Any(item =>
                    item.ItemKind == "EquipmentRow" ||
                    item.ResponseType == "EquipmentRow");
                var matrixColumns = isEquipmentSection
                    ? BuildSectionMatrixColumns(section)
                    : new List<EquipmentColumnEditorInput>();

                return new ChecklistSectionEditorInput
                {
                    Title = section.Name,
                    HelperText = DefaultSectionHelperText(isEquipmentSection
                        ? ChecklistSectionKind.EquipmentTable
                        : ChecklistSectionKind.Fields),
                    Kind = isEquipmentSection ? ChecklistSectionKind.EquipmentTable : ChecklistSectionKind.Fields,
                    Fields = isEquipmentSection
                        ? new List<ChecklistFieldEditorInput>()
                        : section.Items
                            .Where(item => item.ParentChecklistItemId is null)
                            .Where(item => item.ItemKind != "EquipmentRow" && item.ResponseType != "EquipmentRow")
                            .Where(item => item.ItemKind != SectionNoteItemKind)
                            .OrderBy(item => item.DisplayOrder)
                            .Select(BuildFieldEditorInput)
                            .ToList(),
                    MatrixColumns = matrixColumns,
                    MatrixRows = isEquipmentSection
                        ? BuildSectionMatrixRows(section, matrixColumns)
                        : new List<EquipmentRowEditorInput>(),
                    Notes = BuildSectionNotes(section)
                };
            })
            .ToList();
    }

    private static List<ChecklistSectionNoteEditorInput> BuildSectionNotes(ChecklistSection section)
    {
        return section.Items
            .Where(item => item.ParentChecklistItemId is null)
            .Where(item => item.ItemKind == SectionNoteItemKind)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new ChecklistSectionNoteEditorInput
            {
                Label = item.Prompt
            })
            .ToList();
    }

    private static List<EquipmentColumnEditorInput> BuildSectionMatrixColumns(ChecklistSection section)
    {
        var rows = section.Items
            .Where(item =>
                (item.ItemKind == "EquipmentRow" || item.ResponseType == "EquipmentRow") &&
                item.ParentChecklistItemId is null)
            .OrderBy(item => item.DisplayOrder)
            .ToList();

        return rows
            .SelectMany(item => item.ColumnDefinitions)
            .GroupBy(column => column.FieldKey)
            .Select(group => group
                .OrderBy(column => IsNotApplicableColumn(column))
                .ThenBy(column => column.SortOrder)
                .First())
            .OrderBy(column => column.SortOrder)
            .Select(column => new EquipmentColumnEditorInput
            {
                Label = column.Heading,
                Type = IsNotApplicableColumn(column) ? "Text" : column.ResponseType,
                Source = IsNotApplicableColumn(column) ? "Fresh entry" : column.RegisterSource,
                IsRequired = column.IsRequired,
                IsEditable = column.IsEditable,
                IsSystemLinked = column.PullsFromRegister,
                AffectsReadiness = column.AffectsReadiness,
                SameAsPreviousEligible = column.SameAsPreviousEligible,
                RequiresNoteWhenNotNormal = column.RequiresNoteWhenNotNormal
            })
            .ToList();
    }

    private static List<EquipmentRowEditorInput> BuildSectionMatrixRows(
        ChecklistSection section,
        IReadOnlyList<EquipmentColumnEditorInput> matrixColumns)
    {
        var subItemsByParentId = section.Items
            .Where(item => item.ParentChecklistItemId is not null)
            .GroupBy(item => item.ParentChecklistItemId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.DisplayOrder).ToList());

        return section.Items
            .Where(item =>
                (item.ItemKind == "EquipmentRow" || item.ResponseType == "EquipmentRow") &&
                item.ParentChecklistItemId is null)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new EquipmentRowEditorInput
            {
                Name = item.Prompt,
                EquipmentType = item.EquipmentType,
                Model = item.Model,
                IsRequired = item.IsRequired,
                IsReadinessCritical = item.IsReadinessCritical,
                AllowsSameAsPrevious = item.AllowsSameAsPrevious,
                ColumnOverrides = BuildColumnOverrides(item.ColumnDefinitions, matrixColumns),
                SubItems = subItemsByParentId.TryGetValue(item.Id, out var childRows)
                    ? childRows
                        .Select(subItem => new EquipmentSubItemEditorInput
                        {
                            Name = subItem.Prompt,
                            EquipmentType = subItem.EquipmentType,
                            Model = subItem.Model,
                            IsRequired = subItem.IsRequired,
                            IsReadinessCritical = subItem.IsReadinessCritical,
                            AllowsSameAsPrevious = subItem.AllowsSameAsPrevious,
                            ColumnOverrides = BuildColumnOverrides(subItem.ColumnDefinitions, matrixColumns)
                        })
                        .ToList()
                    : new List<EquipmentSubItemEditorInput>()
            })
            .ToList();
    }

    private static ChecklistFieldEditorInput BuildFieldEditorInput(ChecklistItem item)
    {
        var primaryColumn = item.ColumnDefinitions
            .OrderBy(column => column.SortOrder)
            .FirstOrDefault();

        return new ChecklistFieldEditorInput
        {
            Label = item.Prompt,
            Type = primaryColumn?.ResponseType ?? item.ResponseType,
            Source = primaryColumn?.RegisterSource ?? item.DefaultLocation ?? "Fresh entry",
            IsRequired = primaryColumn?.IsRequired ?? item.IsRequired,
            IsEditable = primaryColumn?.IsEditable ?? true,
            IsSystemLinked = primaryColumn?.PullsFromRegister ?? false,
            UnitSchematicKey = item.ItemKind == SchematicBlockItemKind
                ? NormalizeOptional(item.Model)
                : null
        };
    }

    private void EnsureEquipmentEditorDefaults()
    {
        if (IsScratchBuildMode)
        {
            EquipmentRows = EquipmentRows
                .Select(row =>
                {
                    row.ColumnOverrides = EnsureColumnOverrides(row.ColumnOverrides, EquipmentColumns.Count);
                    row.SubItems = row.SubItems
                        .Select(subItem =>
                        {
                            subItem.ColumnOverrides = EnsureColumnOverrides(subItem.ColumnOverrides, EquipmentColumns.Count);
                            return subItem;
                        })
                        .ToList();
                    return row;
                })
                .ToList();
            return;
        }

        EquipmentRows = EquipmentRows
            .Select(row =>
            {
                row.ColumnOverrides = EnsureColumnOverrides(row.ColumnOverrides, EquipmentColumns.Count);
                row.SubItems = row.SubItems
                    .Select(subItem =>
                    {
                        subItem.ColumnOverrides = EnsureColumnOverrides(subItem.ColumnOverrides, EquipmentColumns.Count);
                        return subItem;
                    })
                    .ToList();
                return row;
            })
            .ToList();
    }

    private void NormalizeEquipmentEditorInputs()
    {
        EquipmentRows = EquipmentRows
            .Select(row =>
            {
                row.Name = NormalizeOptional(row.Name) ?? "Equipment item";
                row.EquipmentType = NormalizeOptional(row.EquipmentType);
                row.Model = NormalizeOptional(row.Model);
                row.ColumnOverrides = EnsureColumnOverrides(row.ColumnOverrides, EquipmentColumns.Count);
                row.SubItems = row.SubItems
                    .Select(subItem =>
                    {
                        subItem.Name = NormalizeOptional(subItem.Name) ?? "Equipment subitem";
                        subItem.EquipmentType = NormalizeOptional(subItem.EquipmentType) ?? row.EquipmentType ?? row.Name;
                        subItem.Model = NormalizeOptional(subItem.Model);
                        subItem.ColumnOverrides = EnsureColumnOverrides(subItem.ColumnOverrides, EquipmentColumns.Count);
                        return subItem;
                    })
                    .Where(subItem => !string.IsNullOrWhiteSpace(subItem.Name))
                    .ToList();
                return row;
            })
            .ToList();

        EquipmentColumns = EquipmentColumns
            .Select(column =>
            {
                column.Label = NormalizeOptional(column.Label) ?? "Column";
                column.Type = NormalizeOptional(column.Type) ?? "Text";
                column.Source = NormalizeOptional(column.Source);
                return column;
            })
            .ToList();

        EnsureEquipmentEditorDefaults();
    }

    private void EnsureSectionMatrixEditors()
    {
        VehicleChecklistSections = VehicleChecklistSections
            .Select(section =>
            {
                if (section.Kind != ChecklistSectionKind.EquipmentTable)
                {
                    section.MatrixRows = new List<EquipmentRowEditorInput>();
                    section.MatrixColumns = new List<EquipmentColumnEditorInput>();
                    return section;
                }

                section.MatrixColumns = section.MatrixColumns
                    .Select(column =>
                    {
                        column.Label = NormalizeOptional(column.Label) ?? "Column";
                        column.Type = NormalizeOptional(column.Type) ?? "Text";
                        column.Source = NormalizeOptional(column.Source) ?? "Fresh entry";
                        return column;
                    })
                    .ToList();

                section.MatrixRows = section.MatrixRows
                    .Select(row =>
                    {
                        row.Name = NormalizeOptional(row.Name) ?? "Checklist item";
                        row.EquipmentType = NormalizeOptional(row.EquipmentType);
                        row.Model = NormalizeOptional(row.Model);
                        row.ColumnOverrides = EnsureColumnOverrides(row.ColumnOverrides, section.MatrixColumns.Count);
                        row.SubItems = row.SubItems
                            .Select(subItem =>
                            {
                                subItem.Name = NormalizeOptional(subItem.Name) ?? "Checklist subitem";
                                subItem.EquipmentType = NormalizeOptional(subItem.EquipmentType) ?? row.EquipmentType ?? row.Name;
                                subItem.Model = NormalizeOptional(subItem.Model);
                                subItem.ColumnOverrides = EnsureColumnOverrides(subItem.ColumnOverrides, section.MatrixColumns.Count);
                                return subItem;
                            })
                            .Where(subItem => !string.IsNullOrWhiteSpace(subItem.Name))
                            .ToList();
                        return row;
                    })
                    .ToList();

                return section;
            })
            .ToList();
    }

    public string GetColumnOverrideMode(IReadOnlyList<EquipmentColumnOverrideInput>? overrides, int columnIndex)
    {
        return FindColumnOverride(overrides, columnIndex)?.Mode ?? EquipmentColumnOverrideModes.Default;
    }

    private static EquipmentColumnOverrideInput? FindColumnOverride(
        IReadOnlyList<EquipmentColumnOverrideInput>? overrides,
        int columnIndex)
    {
        return overrides?.FirstOrDefault(item => item.ColumnIndex == columnIndex);
    }

    private static List<EquipmentColumnOverrideInput> EnsureColumnOverrides(
        IEnumerable<EquipmentColumnOverrideInput>? overrides,
        int columnCount)
    {
        var byIndex = (overrides ?? Enumerable.Empty<EquipmentColumnOverrideInput>())
            .GroupBy(item => item.ColumnIndex)
            .ToDictionary(group => group.Key, group => NormalizeColumnOverrideMode(group.First().Mode));

        return Enumerable.Range(0, columnCount)
            .Select(index => new EquipmentColumnOverrideInput
            {
                ColumnIndex = index,
                Mode = byIndex.TryGetValue(index, out var mode) ? mode : EquipmentColumnOverrideModes.Default
            })
            .ToList();
    }

    private static List<EquipmentColumnOverrideInput> BuildColumnOverrides(
        IEnumerable<ChecklistColumnDefinition> itemColumns,
        IReadOnlyList<EquipmentColumnEditorInput> globalColumns)
    {
        var itemColumnsByKey = itemColumns
            .GroupBy(column => column.FieldKey)
            .ToDictionary(group => group.Key, group => group.OrderBy(column => column.SortOrder).First());

        return globalColumns
            .Select((globalColumn, index) =>
            {
                var fieldKey = FieldKey(globalColumn.Label);
                itemColumnsByKey.TryGetValue(fieldKey, out var itemColumn);

                return new EquipmentColumnOverrideInput
                {
                    ColumnIndex = index,
                    Mode = ResolveColumnOverrideMode(itemColumn, globalColumn)
                };
            })
            .ToList();
    }

    private static string ResolveColumnOverrideMode(
        ChecklistColumnDefinition? itemColumn,
        EquipmentColumnEditorInput globalColumn)
    {
        if (itemColumn is null)
        {
            return EquipmentColumnOverrideModes.NotApplicable;
        }

        if (IsNotApplicableColumn(itemColumn))
        {
            return EquipmentColumnOverrideModes.NotApplicable;
        }

        if (!itemColumn.IsRequired && globalColumn.IsRequired)
        {
            return EquipmentColumnOverrideModes.Optional;
        }

        if (itemColumn.IsRequired && !globalColumn.IsRequired)
        {
            return EquipmentColumnOverrideModes.Required;
        }

        return EquipmentColumnOverrideModes.Default;
    }

    private static bool IsNotApplicableColumn(ChecklistColumnDefinition column)
    {
        return string.Equals(column.ResponseType, "N/A", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(column.RegisterSource, "Not applicable for this row", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeColumnOverrideMode(string? mode)
    {
        return mode switch
        {
            EquipmentColumnOverrideModes.Required => EquipmentColumnOverrideModes.Required,
            EquipmentColumnOverrideModes.Optional => EquipmentColumnOverrideModes.Optional,
            EquipmentColumnOverrideModes.NotApplicable => EquipmentColumnOverrideModes.NotApplicable,
            _ => EquipmentColumnOverrideModes.Default
        };
    }

    private static string? DefaultDropdownOptionsJson(string label, string type)
    {
        if (label.Contains("Unit Schematic", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Schematic", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(VehicleSchematicLibrary.Published.Select(schematic => schematic.DisplayName).ToArray());
        }

        if (label.Contains("Battery", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new[] { "N/A", "Full", "Acceptable", "Charging", "Low", "Not applicable" });
        }

        if (label.Contains("Operational", StringComparison.OrdinalIgnoreCase) || type.Contains("Yes", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new[] { "N/A", "Yes", "No" });
        }

        if (type.Contains("Dropdown", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new[] { "N/A", "Pass", "Issue", "Fail", "Operational with notes", "Out of service" });
        }

        return null;
    }

    private static bool IsSchematicField(ChecklistFieldEditorInput field)
    {
        return field.Type.Contains("Schematic", StringComparison.OrdinalIgnoreCase) ||
            field.Label.Contains("Unit Schematic", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveDefaultSchematicKey(string? targetVehicleType)
    {
        var target = NormalizeOptional(targetVehicleType) ?? string.Empty;

        if (target.Contains("Pickup", StringComparison.OrdinalIgnoreCase) ||
            target.Contains("RV", StringComparison.OrdinalIgnoreCase) ||
            target.Contains("Response", StringComparison.OrdinalIgnoreCase) ||
            target.Contains("Sedan", StringComparison.OrdinalIgnoreCase) ||
            target.Contains("Rescue", StringComparison.OrdinalIgnoreCase))
        {
            return VehicleSchematicLibrary.Find("pickup-rv")?.Key ?? VehicleSchematicLibrary.Published.FirstOrDefault()?.Key ?? string.Empty;
        }

        return VehicleSchematicLibrary.Published
            .FirstOrDefault(schematic => string.Equals(schematic.Category, "Ambulance", StringComparison.OrdinalIgnoreCase))
            ?.Key ?? string.Empty;
    }

    private static string FieldKey(string value)
    {
        return value.ToLowerInvariant()
            .Replace("/", string.Empty)
            .Replace("?", string.Empty)
            .Replace(" / ", "-")
            .Replace(" ", "-");
    }

    private void LoadVehicleChecklistLayout()
    {
        if (VehicleChecklistSections.Count > 0)
        {
            NormalizeChecklistSectionEditors();
            return;
        }

        if (IsScratchBuildMode)
        {
            VehicleChecklistSections = BlankStarterSections();
            return;
        }

        VehicleChecklistSections = BlankStarterSections();
    }

    private static List<ChecklistSectionEditorInput> BlankStarterSections()
    {
        return new List<ChecklistSectionEditorInput>
        {
            new(
                "Vehicle",
                "Starter blank section. Rename, open, add fields, or remove this section.",
                ChecklistSectionKind.Fields,
                new List<ChecklistFieldEditorInput>()),
            new(
                "Equipment",
                "Starter blank matrix section. Rename, open, add rows and columns, or remove this section.",
                ChecklistSectionKind.EquipmentTable,
                new List<ChecklistFieldEditorInput>())
        };
    }

    private void NormalizeChecklistSectionEditors()
    {
        VehicleChecklistSections = VehicleChecklistSections
            .Select(section =>
            {
                section.Title = NormalizeOptional(section.Title) ?? DefaultSectionTitle(section.Kind);
                section.HelperText = NormalizeOptional(section.HelperText) ?? DefaultSectionHelperText(section.Kind);
                section.Fields = section.Kind == ChecklistSectionKind.EquipmentTable
                    ? new List<ChecklistFieldEditorInput>()
                    : section.Fields
                        .Select(field =>
                        {
                            field.Label = NormalizeOptional(field.Label) ?? "Checklist field";
                            field.Type = NormalizeOptional(field.Type) ?? "Text";
                            field.Source = NormalizeOptional(field.Source) ?? "Fresh entry";
                            if (IsSchematicField(field))
                            {
                                field.Label = NormalizeOptional(field.Label) ?? "Unit Schematic";
                                field.Type = "Schematic Markup";
                                field.Source = "Unit Schematic library";
                                field.IsSystemLinked = true;
                                field.IsEditable = true;
                                field.UnitSchematicKey = NormalizeOptional(field.UnitSchematicKey);
                            }
                            return field;
                        })
                        .ToList();
                section.Notes = section.Notes
                    .Select(note =>
                    {
                        note.Label = NormalizeOptional(note.Label) ?? $"{section.Title} notes";
                        note.IsRequired = false;
                        note.IsReadinessCritical = false;
                        return note;
                    })
                    .ToList();
                if (section.Kind != ChecklistSectionKind.EquipmentTable)
                {
                    section.MatrixRows = new List<EquipmentRowEditorInput>();
                    section.MatrixColumns = new List<EquipmentColumnEditorInput>();
                }
                return section;
            })
            .ToList();
    }

    private static string DefaultSectionTitle(ChecklistSectionKind kind) => kind switch
    {
        ChecklistSectionKind.EquipmentTable => "Equipment",
        ChecklistSectionKind.Action => "Same as previous shift",
        ChecklistSectionKind.Schematic => "Unit Schematic",
        _ => "Vehicle"
    };

    private static string DefaultSectionHelperText(ChecklistSectionKind kind) => kind switch
    {
        ChecklistSectionKind.EquipmentTable => "One row per equipment item configured for the selected vehicle type; subitems sit underneath their parent item and use the same columns.",
        ChecklistSectionKind.Action => "Senior management controls whether crews may use previous-shift values.",
        ChecklistSectionKind.Schematic => "Choose from saved unit schematics and capture damage markings.",
        _ => "Vehicle details are linked to registration; operational, schematic, damage, and notes fields are captured fresh unless same-as-previous is used."
    };
}

public enum ChecklistSectionKind
{
    Fields,
    Action,
    Schematic,
    EquipmentTable
}

public class ChecklistSectionEditorInput
{
    public ChecklistSectionEditorInput()
    {
    }

    public ChecklistSectionEditorInput(
        string title,
        string helperText,
        ChecklistSectionKind kind,
        List<ChecklistFieldEditorInput> fields)
    {
        Title = title;
        HelperText = helperText;
        Kind = kind;
        Fields = fields;
    }

    public string Title { get; set; } = string.Empty;
    public string? HelperText { get; set; }
    public ChecklistSectionKind Kind { get; set; } = ChecklistSectionKind.Fields;
    public List<ChecklistFieldEditorInput> Fields { get; set; } = new();
    public List<EquipmentRowEditorInput> MatrixRows { get; set; } = new();
    public List<EquipmentColumnEditorInput> MatrixColumns { get; set; } = new();
    public List<ChecklistSectionNoteEditorInput> Notes { get; set; } = new();
}

public class ChecklistSectionNoteEditorInput
{
    public string Label { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsReadinessCritical { get; set; }
}

public class ChecklistFieldEditorInput
{
    public ChecklistFieldEditorInput()
    {
    }

    public ChecklistFieldEditorInput(
        string label,
        string type,
        bool isRequired,
        bool isEditable,
        bool isSystemLinked,
        string source)
    {
        Label = label;
        Type = type;
        IsRequired = isRequired;
        IsEditable = isEditable;
        IsSystemLinked = isSystemLinked;
        Source = source;
    }

    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "Text";
    public bool IsRequired { get; set; }
    public bool IsEditable { get; set; } = true;
    public bool IsSystemLinked { get; set; }
    public string? Source { get; set; }
    public string? UnitSchematicKey { get; set; }
}

public record VehicleTargetSubtypeOption(string? Function, string Subtype)
{
    public string Label => string.IsNullOrWhiteSpace(Function)
        ? Subtype
        : $"{Function} / {Subtype}";
}

public record ChecklistTemplateOption(
    int Id,
    string Name,
    string TargetVehicleType,
    string Status,
    bool IsPublished,
    string Version)
{
    public string DisplayName => $"{TargetVehicleType} - {Name} v{Version} ({Status})";
}

public record ChecklistLiveUseRow(string Scope, string Target, DateTime PublishedAtUtc, string PublishedBy, string? PublishNote);

public class EquipmentRowEditorInput
{
    public string Name { get; set; } = string.Empty;
    public string? EquipmentType { get; set; }
    public string? Model { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadinessCritical { get; set; }
    public bool AllowsSameAsPrevious { get; set; }
    public List<EquipmentColumnOverrideInput> ColumnOverrides { get; set; } = new();
    public List<EquipmentSubItemEditorInput> SubItems { get; set; } = new();
}

public class EquipmentSubItemEditorInput
{
    public string Name { get; set; } = string.Empty;
    public string? EquipmentType { get; set; }
    public string? Model { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsReadinessCritical { get; set; } = true;
    public bool AllowsSameAsPrevious { get; set; } = true;
    public List<EquipmentColumnOverrideInput> ColumnOverrides { get; set; } = new();
}

public class EquipmentColumnOverrideInput
{
    public int ColumnIndex { get; set; }
    public string Mode { get; set; } = EquipmentColumnOverrideModes.Default;
}

public static class EquipmentColumnOverrideModes
{
    public const string Default = "Default";
    public const string Required = "Required";
    public const string Optional = "Optional";
    public const string NotApplicable = "NotApplicable";
}

public class EquipmentColumnEditorInput
{
    public EquipmentColumnEditorInput()
    {
    }

    public EquipmentColumnEditorInput(
        string label,
        string type,
        string? source,
        bool isRequired,
        bool isEditable,
        bool isSystemLinked,
        bool affectsReadiness,
        bool sameAsPreviousEligible,
        bool requiresNoteWhenNotNormal)
    {
        Label = label;
        Type = type;
        Source = source;
        IsRequired = isRequired;
        IsEditable = isEditable;
        IsSystemLinked = isSystemLinked;
        AffectsReadiness = affectsReadiness;
        SameAsPreviousEligible = sameAsPreviousEligible;
        RequiresNoteWhenNotNormal = requiresNoteWhenNotNormal;
    }

    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "Text";
    public string? Source { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEditable { get; set; }
    public bool IsSystemLinked { get; set; }
    public bool AffectsReadiness { get; set; }
    public bool SameAsPreviousEligible { get; set; }
    public bool RequiresNoteWhenNotNormal { get; set; }
}
