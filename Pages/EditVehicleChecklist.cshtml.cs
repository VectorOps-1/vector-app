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
    private const string LegacyMonthlyVehicleEquipmentChecklistName = "Monthly Vehicle & Equipment Check";
    private const string LegacyMonthlyVehicleChecklistName = "Monthly Vehicle Checklist";
    private const string SectionNoteItemKind = "SectionNote";
    private const string SchematicBlockItemKind = "SchematicBlock";
    private const string SchematicViewItemKind = "SchematicView";
    private static readonly string[] UnitSchematicViews = ["Left", "Right", "Front", "Rear"];
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public EditVehicleChecklistModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty] public string? ChecklistName { get; set; } = DailyVehicleChecklistName;
    [BindProperty] public string ChecklistStatus { get; set; } = "Draft";
    [BindProperty] public int? SelectedTemplateId { get; set; }
    [BindProperty] public bool CreateAsNewTemplate { get; set; }
    [BindProperty] public bool UseCustomChecklistName { get; set; }
    [BindProperty] public string? CustomChecklistName { get; set; }
    [BindProperty] public string TargetVehicleType { get; set; } = "All Vehicles";
    [BindProperty] public string? TargetVehicleFunction { get; set; }
    [BindProperty] public string? TargetVehicleSubtype { get; set; }
    [BindProperty] public bool UseCustomTargetVehicleType { get; set; }
    [BindProperty] public string? CustomTargetVehicleType { get; set; }
    [BindProperty] public string? AppliesTo { get; set; }
    [BindProperty] public int? PublishOperationalAreaId { get; set; }
    [BindProperty] public int? PublishVehicleId { get; set; }
    [BindProperty] public string? PublishVehicleType { get; set; }
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
    public bool IsScratchBuildMode => CreateAsNewTemplate && SelectedTemplateId is null;
    public string LayoutBuilderSummary => IsVehicleChecklistName(ChecklistName)
        ? IsScratchBuildMode
            ? $"{ChecklistName} starts blank. Add sections, rows, columns, items, subitems, notes, and optional register links yourself."
            : $"{ChecklistName} is built from staff-facing blocks: Vehicle fields linked to registration, and matrix sections linked to vehicle type where needed."
        : "Select a daily check or full audit checklist to edit the layout.";

    public List<ChecklistTemplateOption> AvailableTemplates { get; private set; } = new();
    public List<SelectListItem> PublishAreaOptions { get; private set; } = new();
    public List<SelectListItem> PublishVehicleOptions { get; private set; } = new();
    public List<SelectListItem> PublishVehicleTypeOptions { get; private set; } = new();
    public List<SelectListItem> SeniorApproverOptions { get; private set; } = new();
    public List<SelectListItem> TargetVehicleFunctionOptions { get; private set; } = new();
    public List<VehicleTargetSubtypeOption> TargetVehicleSubtypeOptions { get; private set; } = new();
    public IReadOnlyList<VehicleSchematicDefinition> PublishedUnitSchematics => VehicleSchematicLibrary.Published;
    public string DefaultUnitSchematicKey => ResolveDefaultSchematicKey(TargetVehicleType);
    public IReadOnlyList<string> ChecklistNameOptions { get; } = new[]
    {
        DailyVehicleChecklistName,
        FullAuditChecklistName,
        "Custom Uploaded Vehicle Checklist"
    };
    public IReadOnlyList<string> TargetVehicleTypeOptions { get; private set; } = new[] { "All Vehicles" };
    public IReadOnlyList<string> EquipmentTableExampleRows { get; } = new[]
    {
        "LP15",
        "Syringe driver 1",
        "Syringe driver 2",
        "Ventilator Oxylog",
        "LUCAS"
    };

    public async Task OnGetAsync(string? checklist, int? templateId, string? targetVehicleType, string? mode)
    {
        var isBuildMode = string.Equals(mode, "build", StringComparison.OrdinalIgnoreCase);
        var currentUser = await LoadCurrentAuthorityAsync(loadPublishedSettings: true);
        ChecklistName = ResolveChecklistName(checklist, ChecklistName);
        TargetVehicleType = NormalizeTargetVehicleType(targetVehicleType ?? TargetVehicleType);
        SelectedTemplateId = templateId;
        CreateAsNewTemplate = isBuildMode && templateId is null;
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
            publishScopeType == "VehicleCategory" &&
            string.IsNullOrWhiteSpace(PublishVehicleType))
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await LoadTargetVehicleOptionsAsync(currentUser.CompanyId);
            await LoadPublishControlOptionsAsync(currentUser.CompanyId);
            StatusMessage = "Select the exact vehicle function or subtype this checklist should apply to.";
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
            ? $"{savedTemplate.Name} for {savedTemplate.TargetVehicleType} approved for publishing. Same as previous shift: vehicle inspection {(AllowSameAsPreviousVehicleInspection ? "enabled" : "disabled")}; equipment checks {(AllowSameAsPreviousEquipmentCheck ? "enabled" : "disabled")}."
            : $"{savedTemplate.Name} for {savedTemplate.TargetVehicleType} draft saved as an available vehicle checklist template.";

        return RedirectToPage("/EditChecklist");
    }

    private async Task LoadTemplateOptionsAsync(int companyId)
    {
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

        var publishVehicleTypeOptions = new List<SelectListItem>();
        publishVehicleTypeOptions.AddRange(vehicles
            .Select(vehicle => NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType))
            .Where(function => !string.IsNullOrWhiteSpace(function))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(function => function == VehicleTaxonomyService.AmbulanceFunction ? 0 : function == VehicleTaxonomyService.ResponseVehicleFunction ? 1 : 50)
            .ThenBy(function => function)
            .Select(function => new SelectListItem
            {
                Value = function!,
                Text = $"Function: {function}",
                Selected = string.Equals(PublishVehicleType, function, StringComparison.OrdinalIgnoreCase)
            }));

        publishVehicleTypeOptions.AddRange(vehicles
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
            .Select(item => new SelectListItem
            {
                Value = item.Subtype,
                Text = string.IsNullOrWhiteSpace(item.Function)
                    ? $"Subtype: {item.Subtype}"
                    : $"Subtype: {item.Function} / {item.Subtype}",
                Selected = string.Equals(PublishVehicleType, item.Subtype, StringComparison.OrdinalIgnoreCase)
            }));

        if (!string.IsNullOrWhiteSpace(PublishVehicleType) &&
            !publishVehicleTypeOptions.Any(option => string.Equals(option.Value, PublishVehicleType, StringComparison.OrdinalIgnoreCase)))
        {
            publishVehicleTypeOptions.Add(new SelectListItem
            {
                Value = PublishVehicleType,
                Text = $"Current target: {PublishVehicleType}",
                Selected = true
            });
        }

        PublishVehicleTypeOptions = publishVehicleTypeOptions;

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
                .AsSplitQuery()
                .FirstOrDefaultAsync(item => item.CompanyId == currentUser.CompanyId && item.Id == SelectedTemplateId);

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

        template.Name = ChecklistName?.Trim() ?? DailyVehicleChecklistName;
        template.ChecklistType = "Vehicle";
        template.TargetVehicleType = TargetVehicleType;
        template.Version = string.IsNullOrWhiteSpace(template.Version) ? "1.0" : template.Version;
        template.SourceType = SelectedTemplateId is null ? "Built" : "Edited";
        template.CreatedByUserId ??= currentUser.Id;
        template.Status = publish
            ? "Published"
            : string.Equals(ChecklistStatus, "Published", StringComparison.OrdinalIgnoreCase) ? "Under Review" : ChecklistStatus;
        template.IsPublished = publish;
        template.PublishedAtUtc = publish ? now : template.PublishedAtUtc;
        template.PublishedByUserId = publish ? currentUser.Id : template.PublishedByUserId;
        template.PublishScopeSummary = publish ? ResolvePublishScopeSummary() : template.PublishScopeSummary;
        template.PublishNotes = NormalizeOptional(PublishNote);
        template.UpdatedAtUtc = now;

        if (publish)
        {
            var otherPublishedTemplates = await _db.ChecklistTemplates
                .Where(item =>
                    item.CompanyId == currentUser.CompanyId &&
                    item.ChecklistType == "Vehicle" &&
                    item.TargetVehicleType == TargetVehicleType &&
                    item.Id != template.Id &&
                    item.IsPublished)
                .ToListAsync();

            foreach (var otherTemplate in otherPublishedTemplates)
            {
                otherTemplate.IsPublished = false;
                otherTemplate.Status = "Archived";
                otherTemplate.UpdatedAtUtc = now;
            }

            foreach (var activeScope in template.PublishScopes.Where(scope => scope.IsActive))
            {
                activeScope.IsActive = false;
                activeScope.RetiredAtUtc = now;
            }
        }

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

        if (publish)
        {
            _db.ChecklistPublishScopes.Add(new ChecklistPublishScope
            {
                ChecklistTemplate = template,
                ScopeType = ResolvePublishScopeType(),
                OperationalAreaId = ResolvePublishScopeType() == "OperationalArea" ? PublishOperationalAreaId : null,
                VehicleId = ResolvePublishScopeType() == "Vehicle" ? PublishVehicleId : null,
                PublishedByUserId = currentUser.Id,
                PublishNote = NormalizeOptional(PublishNote),
                IsActive = true,
                PublishedAtUtc = now
            });
        }

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = publish ? "Checklist template published" : "Checklist template saved",
            EntityType = "ChecklistTemplate",
            EntityId = template.Id,
            Details = $"{currentUser.FullName} {(publish ? "published" : "saved")} {template.Name} for {template.TargetVehicleType}.",
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
        var schematicKey = NormalizeOptional(field.UnitSchematicKey) ?? ResolveDefaultSchematicKey(null);
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
            return string.IsNullOrWhiteSpace(fallback) ? DailyVehicleChecklistName : fallback;
        }

        var normalized = checklist.Trim().ToLowerInvariant();
        return normalized switch
        {
            "daily-vehicle" or "daily vehicle" or "daily vehicle checklist" or "daily vehicle inspection" or "daily vehicle readiness" or "daily vehicle check" or "daily vehicle & equipment check" => DailyVehicleChecklistName,
            "full-audit" or "full audit" or "audit" or "monthly-vehicle" or "monthly vehicle" or "monthly vehicle checklist" or "monthly vehicle inspection" or "monthly vehicle check" or "monthly vehicle & equipment check" => FullAuditChecklistName,
            _ => checklist
        };
    }

    private static bool IsVehicleChecklistName(string? checklistName)
    {
        return string.Equals(checklistName, DailyVehicleChecklistName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(checklistName, LegacyDailyVehicleChecklistName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(checklistName, FullAuditChecklistName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(checklistName, LegacyMonthlyVehicleEquipmentChecklistName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(checklistName, LegacyMonthlyVehicleChecklistName, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTargetVehicleType(string? targetVehicleType)
    {
        return string.IsNullOrWhiteSpace(targetVehicleType) ? "All Vehicles" : targetVehicleType.Trim();
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

        if (ChecklistNameOptions.Any(option => string.Equals(option, ChecklistName, StringComparison.OrdinalIgnoreCase)))
        {
            CustomChecklistName ??= ChecklistName;
        }
        else
        {
            UseCustomChecklistName = true;
            CustomChecklistName = ChecklistName;
        }

        if (TargetVehicleTypeOptions.Any(option => string.Equals(option, TargetVehicleType, StringComparison.OrdinalIgnoreCase)))
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
            "VehicleCategory" => $"Vehicle function / subtype: {NormalizeOptional(PublishVehicleType) ?? "No function or subtype selected"}",
            "Vehicle" => $"Callsign / registration: {PublishVehicleOptions.FirstOrDefault(vehicle => vehicle.Value == PublishVehicleId?.ToString())?.Text ?? "Specific vehicle registration"}",
            _ => "All areas and all eligible vehicles"
        };
    }

    private void ApplyPublishTargetToTemplateTarget()
    {
        var publishScopeType = ResolvePublishScopeType();
        if (publishScopeType == "VehicleCategory")
        {
            var publishVehicleType = NormalizeOptional(PublishVehicleType);
            if (publishVehicleType is not null)
            {
                TargetVehicleType = publishVehicleType;
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
            "Vehicle function / subtype" => "VehicleCategory",
            "Specific vehicle registration" => "Vehicle",
            _ => "AllAreas"
        };
    }

    private static string NormalizePublishScope(string? value)
    {
        return value switch
        {
            "Specific base" or "Specific operational area" => "Specific base",
            "Vehicle type" or "Specific vehicle category" or "Vehicle function / subtype" => "Vehicle function / subtype",
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

        if (EquipmentRows.Count == 0)
        {
            EquipmentRows = EquipmentRowsForTarget(TargetVehicleType)
                .Select(row => new EquipmentRowEditorInput
                {
                    Name = row,
                    EquipmentType = row,
                    IsRequired = true,
                    IsReadinessCritical = true,
                    AllowsSameAsPrevious = true
                })
                .ToList();
        }

        if (EquipmentColumns.Count == 0)
        {
            EquipmentColumns = new List<EquipmentColumnEditorInput>
            {
                new("Name/item", "Configured row", "Vehicle equipment setup", true, false, true, false, true, false),
                new("S/N / ID", "Dropdown", "Equipment register", true, true, true, false, true, false),
                new("Next Service", "Date", "Equipment register", false, true, true, false, true, false),
                new("Battery", "Dropdown", "Fresh entry", true, true, false, true, true, true),
                new("Operational?", "Yes/No", "Fresh entry", true, true, false, true, true, true),
                new("Issues / errors", "Text", "Required if not operational", false, true, false, false, true, false)
            };
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

                if (!IsScratchBuildMode && SelectedTemplateId is null && section.Title.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
                {
                    if (section.MatrixColumns.Count == 0)
                    {
                        section.MatrixColumns = DefaultEquipmentColumns();
                    }

                    if (section.MatrixRows.Count == 0)
                    {
                        section.MatrixRows = EquipmentRowsForTarget(TargetVehicleType)
                            .Select(row => new EquipmentRowEditorInput
                            {
                                Name = row,
                                EquipmentType = row,
                                IsRequired = true,
                                IsReadinessCritical = true,
                                AllowsSameAsPrevious = true
                            })
                            .ToList();
                    }
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

    private static List<EquipmentColumnEditorInput> DefaultEquipmentColumns() => new()
    {
        new("Name/item", "Configured row", "Vehicle equipment setup", true, false, true, false, true, false),
        new("S/N / ID", "Dropdown", "Equipment register", true, true, true, false, true, false),
        new("Next Service", "Date", "Equipment register", false, true, true, false, true, false),
        new("Battery", "Dropdown", "Fresh entry", true, true, false, true, true, true),
        new("Operational?", "Yes/No", "Fresh entry", true, true, false, true, true, true),
        new("Issues / errors", "Text", "Required if not operational", false, true, false, false, true, false)
    };

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

    private static IReadOnlyList<string> EquipmentRowsForTarget(string targetVehicleType)
    {
        if (targetVehicleType.Contains("ICU", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Monitor defibrillator", "Transport ventilator", "Syringe driver", "Infusion pump", "Suction unit", "Mechanical CPR device", "Video laryngoscope", "Portable ultrasound", "Oxygen regulator", "Portable radio", "Rugged tablet" };
        }

        if (targetVehicleType.Contains("Pickup", StringComparison.OrdinalIgnoreCase) ||
            targetVehicleType.Contains("RV", StringComparison.OrdinalIgnoreCase) ||
            targetVehicleType.Contains("Response", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Monitor defibrillator" };
        }

        return new[] { "Monitor defibrillator", "Suction unit", "Syringe driver", "Infusion pump", "Oxygen regulator", "Portable radio", "Rugged tablet" };
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
            VehicleChecklistSections = new List<ChecklistSectionEditorInput>();
            return;
        }

        VehicleChecklistSections = new List<ChecklistSectionEditorInput>
        {
            new(
                "Vehicle",
                "Blank vehicle section. Add the fields or rows this checklist should capture.",
                ChecklistSectionKind.Fields,
                new List<ChecklistFieldEditorInput>()),
            new(
                "Equipment",
                "Blank matrix section. Add item rows, subitems, and the columns crews must complete.",
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
                                field.UnitSchematicKey = NormalizeOptional(field.UnitSchematicKey) ?? ResolveDefaultSchematicKey(TargetVehicleType);
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
