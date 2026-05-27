using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditVehicleChecklistModel : PageModel
{
    private const string DailyVehicleChecklistName = "Daily Vehicle Inspection";
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
        ? $"{ChecklistName} uses the shared vehicle inspection layout builder."
        : "Select a daily or monthly vehicle checklist to edit the vehicle inspection layout.";

    public List<ChecklistSectionEditor> VehicleChecklistSections { get; private set; } = new();

    public async Task OnGetAsync(string? checklist)
    {
        await LoadCurrentAuthorityAsync(loadPublishedSettings: true);
        ChecklistName = ResolveChecklistName(checklist, ChecklistName);
        LoadVehicleChecklistLayout();
        DropdownOptions = "Full\n3/4\n1/2\n1/4\nEmpty";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await LoadCurrentAuthorityAsync(loadPublishedSettings: false);
        LoadVehicleChecklistLayout();

        if (string.IsNullOrWhiteSpace(ChecklistName))
        {
            StatusMessage = "Select a checklist before saving or publishing.";
            return Page();
        }

        if (ActionType == "approve-publish" && !IsSeniorChecklistPublisher)
        {
            StatusMessage = "Only senior management can approve and publish a checklist for live operational use. Draft changes can still be saved for review.";
            return Page();
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
            ? $"{ChecklistName} approved for publishing. Same as previous shift: vehicle inspection {(AllowSameAsPreviousVehicleInspection ? "enabled" : "disabled")}; equipment checks {(AllowSameAsPreviousEquipmentCheck ? "enabled" : "disabled")}."
            : $"{ChecklistName} draft saved. Layout, section order, field rules, dropdown options, reuse rules, and schematic source rules are ready for review.";

        return RedirectToPage("/EditChecklist");
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
            "daily-vehicle" or "daily vehicle" or "daily vehicle checklist" or "daily vehicle inspection" => DailyVehicleChecklistName,
            "monthly-vehicle" or "monthly vehicle" or "monthly vehicle checklist" or "monthly vehicle inspection" => MonthlyVehicleChecklistName,
            _ => checklist
        };
    }

    private static bool IsVehicleChecklistName(string? checklistName)
    {
        return string.Equals(checklistName, DailyVehicleChecklistName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(checklistName, MonthlyVehicleChecklistName, StringComparison.OrdinalIgnoreCase);
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
    Schematic
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
