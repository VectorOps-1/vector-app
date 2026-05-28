using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditVehicleChecklistModel : PageModel
{
    private const string DailyVehicleChecklistName = "Daily Vehicle Readiness";
    private const string MonthlyVehicleChecklistName = "Monthly Vehicle Checklist";
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
    [BindProperty] public string TargetVehicleType { get; set; } = "Ambulance";
    [BindProperty] public string? DropdownField { get; set; }
    [BindProperty] public string? DropdownOptions { get; set; }
    [BindProperty] public string? AppliesTo { get; set; }
    [BindProperty] public string? PublishNote { get; set; }
    [BindProperty] public string? ActionType { get; set; }
    [BindProperty] public bool AllowSameAsPreviousVehicleInspection { get; set; } = true;
    [BindProperty] public bool AllowSameAsPreviousEquipmentCheck { get; set; } = true;

    [TempData]
    public string? StatusMessage { get; set; }
    public bool IsSeniorChecklistPublisher { get; private set; }
    public string ChecklistAuthorityNote { get; private set; } = "Senior management publishes live checklist versions. Operational managers can draft assigned changes.";
    public string LayoutBuilderSummary => IsVehicleChecklistName(ChecklistName)
        ? $"{ChecklistName} uses the shared readiness layout builder, including the carried-equipment row table."
        : "Select a daily or monthly vehicle checklist to edit the readiness layout.";

    public List<ChecklistSectionEditor> VehicleChecklistSections { get; private set; } = new();
    public List<ChecklistTemplateOption> AvailableTemplates { get; private set; } = new();
    public IReadOnlyList<string> TargetVehicleTypeOptions { get; } = new[]
    {
        "Ambulance",
        "Operational Ambulance",
        "IFT Ambulance",
        "ICU Ambulance",
        "Response Vehicle",
        "Response Pickup",
        "Response Sedan",
        "Rescue Vehicle",
        "All Vehicles"
    };
    public IReadOnlyList<string> EquipmentTableExampleRows { get; } = new[]
    {
        "LP15",
        "Syringe driver 1",
        "Syringe driver 2",
        "Ventilator Oxylog",
        "LUCAS"
    };

    public async Task OnGetAsync(string? checklist, int? templateId, string? targetVehicleType)
    {
        var currentUser = await LoadCurrentAuthorityAsync(loadPublishedSettings: true);
        ChecklistName = ResolveChecklistName(checklist, ChecklistName);
        TargetVehicleType = NormalizeTargetVehicleType(targetVehicleType ?? TargetVehicleType);
        SelectedTemplateId = templateId;
        if (currentUser is not null)
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            await ApplySelectedTemplateAsync(currentUser.CompanyId);
        }

        LoadVehicleChecklistLayout();
        DropdownOptions = "Full\n3/4\n1/2\n1/4\nEmpty";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await LoadCurrentAuthorityAsync(loadPublishedSettings: false);
        LoadVehicleChecklistLayout();

        if (string.IsNullOrWhiteSpace(ChecklistName))
        {
            if (currentUser is not null)
            {
                await LoadTemplateOptionsAsync(currentUser.CompanyId);
            }

            StatusMessage = "Select a checklist before saving or publishing.";
            return Page();
        }

        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        TargetVehicleType = NormalizeTargetVehicleType(TargetVehicleType);

        if (ActionType == "approve-publish" && !IsSeniorChecklistPublisher)
        {
            await LoadTemplateOptionsAsync(currentUser.CompanyId);
            StatusMessage = "Only senior management can approve and publish a checklist for live operational use. Draft changes can still be saved for review.";
            return Page();
        }

        var savedTemplate = await SaveTemplateAsync(currentUser, ActionType == "approve-publish");

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
            .Where(template => template.CompanyId == companyId && template.ChecklistType == "Vehicle")
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

    private async Task ApplySelectedTemplateAsync(int companyId)
    {
        ChecklistTemplate? template = null;

        if (SelectedTemplateId is not null)
        {
            template = await _db.ChecklistTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CompanyId == companyId && item.Id == SelectedTemplateId);
        }

        template ??= await _db.ChecklistTemplates
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == companyId &&
                item.ChecklistType == "Vehicle" &&
                item.Name == ChecklistName &&
                item.TargetVehicleType == TargetVehicleType)
            .OrderByDescending(item => item.IsPublished)
            .ThenByDescending(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (template is null)
        {
            return;
        }

        SelectedTemplateId = template.Id;
        ChecklistName = template.Name;
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
                .AsSplitQuery()
                .FirstOrDefaultAsync(item => item.CompanyId == currentUser.CompanyId && item.Id == SelectedTemplateId);

        template ??= await _db.ChecklistTemplates
            .Include(item => item.Sections)
            .ThenInclude(section => section.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.ChecklistType == "Vehicle" &&
                item.Name == ChecklistName &&
                item.TargetVehicleType == TargetVehicleType);

        if (template is null)
        {
            template = new ChecklistTemplate
            {
                CompanyId = currentUser.CompanyId,
                ClientName = company?.Name ?? "Client Business Name",
                CreatedAtUtc = now
            };
            _db.ChecklistTemplates.Add(template);
        }

        template.Name = ChecklistName?.Trim() ?? DailyVehicleChecklistName;
        template.ChecklistType = "Vehicle";
        template.TargetVehicleType = TargetVehicleType;
        template.Version = string.IsNullOrWhiteSpace(template.Version) ? "1.0" : template.Version;
        template.Status = publish
            ? "Published"
            : string.Equals(ChecklistStatus, "Published", StringComparison.OrdinalIgnoreCase) ? "Under Review" : ChecklistStatus;
        template.IsPublished = publish;
        template.PublishedAtUtc = publish ? now : template.PublishedAtUtc;
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
        }

        _db.ChecklistItems.RemoveRange(template.Sections.SelectMany(section => section.Items));
        _db.ChecklistSections.RemoveRange(template.Sections);

        foreach (var section in VehicleChecklistSections.Select((section, index) => new { Section = section, Index = index }))
        {
            var templateSection = new ChecklistSection
            {
                ChecklistTemplate = template,
                Name = section.Section.Title,
                DisplayOrder = (section.Index + 1) * 10
            };

            foreach (var field in section.Section.Fields.Select((field, index) => new { Field = field, Index = index }))
            {
                templateSection.Items.Add(new ChecklistItem
                {
                    Prompt = field.Field.Label,
                    ResponseType = field.Field.Type,
                    RequiresCommentOnFail = section.Section.Kind is ChecklistSectionKind.Schematic,
                    DisplayOrder = field.Index + 1
                });
            }

            _db.ChecklistSections.Add(templateSection);
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

    private async Task<vector_app_local.Models.AppUser?> LoadCurrentAuthorityAsync(bool loadPublishedSettings)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        IsSeniorChecklistPublisher = CurrentUserService.IsSeniorAccessRole(currentUser?.AppRole?.Name);

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

    private static string ResolveChecklistName(string? checklist, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(checklist))
        {
            return string.IsNullOrWhiteSpace(fallback) ? DailyVehicleChecklistName : fallback;
        }

        var normalized = checklist.Trim().ToLowerInvariant();
        return normalized switch
        {
            "daily-vehicle" or "daily vehicle" or "daily vehicle checklist" or "daily vehicle inspection" or "daily vehicle readiness" => DailyVehicleChecklistName,
            "monthly-vehicle" or "monthly vehicle" or "monthly vehicle checklist" or "monthly vehicle inspection" => MonthlyVehicleChecklistName,
            _ => checklist
        };
    }

    private static bool IsVehicleChecklistName(string? checklistName)
    {
        return string.Equals(checklistName, DailyVehicleChecklistName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(checklistName, MonthlyVehicleChecklistName, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTargetVehicleType(string? targetVehicleType)
    {
        return string.IsNullOrWhiteSpace(targetVehicleType) ? "Ambulance" : targetVehicleType.Trim();
    }

    private void LoadVehicleChecklistLayout()
    {
        VehicleChecklistSections = new List<ChecklistSectionEditor>
        {
            new(
                "Vehicle Details",
                "Select the vehicle and capture readiness values.",
                ChecklistSectionKind.Fields,
                new List<ChecklistFieldEditor>
                {
                    new("Registration number", "Dropdown", true, true, true, "Vehicle register"),
                    new("Vehicle / callsign", "Text", true, true, true, "Auto-filled"),
                    new("Vehicle type", "Text", true, true, true, "Auto-filled"),
                    new("Next service date", "Date", false, true, true, "Auto-filled")
                }),
            new(
                "Same as previous shift",
                "Optional reuse controls shown below vehicle details and in the equipment section.",
                ChecklistSectionKind.Action,
                new List<ChecklistFieldEditor>()),
            new(
                "Operational Checks",
                "Complete these fields fresh unless Same as previous shift is selected.",
                ChecklistSectionKind.Fields,
                new List<ChecklistFieldEditor>
                {
                    new("Current kilometres", "Number", true, true, false, "Fresh entry"),
                    new("Fuel level", "Dropdown", true, true, false, "Fresh entry"),
                    new("Vehicle condition", "Dropdown", true, true, false, "Fresh entry"),
                    new("Lights", "Dropdown", true, true, false, "Fresh entry"),
                    new("Sirens", "Dropdown", true, true, false, "Fresh entry"),
                    new("Warning lights", "Dropdown", true, true, false, "Fresh entry"),
                    new("Tyres", "Dropdown", true, true, false, "Fresh entry"),
                    new("Ops radio connectivity", "Dropdown", true, true, false, "Fresh entry")
                }),
            new(
                "Vehicle Schematic",
                "Mark damage against the schematic linked to the selected registration.",
                ChecklistSectionKind.Schematic,
                new List<ChecklistFieldEditor>
                {
                    new("Vehicle schematic", "Schematic Markup", true, false, true, "Registration schematic"),
                    new("Damage type", "Dropdown", false, true, false, "Fresh entry"),
                    new("Damage severity", "Dropdown", false, true, false, "Fresh entry"),
                    new("Damage notes", "Text", false, true, false, "Fresh entry")
                }),
            new(
                "Carried Equipment",
                "One row per equipment item configured for the selected vehicle or vehicle category.",
                ChecklistSectionKind.EquipmentTable,
                new List<ChecklistFieldEditor>
                {
                    new("Name/item", "Configured row", true, false, true, "Vehicle equipment setup"),
                    new("S/N / ID", "Dropdown", true, true, true, "Equipment register"),
                    new("Next Service", "Date", false, false, true, "Equipment register"),
                    new("Battery", "Dropdown", true, true, false, "Fresh entry")
                }),
            new(
                "Notes / Issue",
                "Record anything that requires follow-up.",
                ChecklistSectionKind.Fields,
                new List<ChecklistFieldEditor>
                {
                    new("Inspection notes", "Text", false, true, false, "Fresh entry")
                })
        };
    }
}

public enum ChecklistSectionKind
{
    Fields,
    Action,
    Schematic,
    EquipmentTable
}

public record ChecklistSectionEditor(
    string Title,
    string HelperText,
    ChecklistSectionKind Kind,
    IReadOnlyList<ChecklistFieldEditor> Fields);

public record ChecklistFieldEditor(
    string Label,
    string Type,
    bool IsRequired,
    bool IsEditable,
    bool IsSystemLinked,
    string Source);

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
