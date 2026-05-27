using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditChecklistModel : PageModel
{
    private readonly CurrentUserService _currentUser;

    public EditChecklistModel(CurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [BindProperty] public string? ChecklistName { get; set; } = "Daily Vehicle Inspection";
    [BindProperty] public string ChecklistStatus { get; set; } = "Draft";
    [BindProperty] public string? DropdownField { get; set; }
    [BindProperty] public string? DropdownOptions { get; set; }
    [BindProperty] public string? AppliesTo { get; set; }
    [BindProperty] public string? PublishNote { get; set; }
    [BindProperty] public string? ActionType { get; set; }
    [BindProperty] public bool AllowSameAsPreviousShift { get; set; } = true;

    public string? StatusMessage { get; private set; }
    public bool IsSeniorChecklistPublisher { get; private set; }
    public string ChecklistAuthorityNote { get; private set; } = "Senior management publishes live checklist versions. Operational managers can draft assigned changes.";

    public List<ChecklistSectionEditor> VehicleChecklistSections { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await LoadCurrentAuthorityAsync();
        LoadVehicleChecklistLayout();
        DropdownOptions = "Full\n3/4\n1/2\n1/4\nEmpty";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadCurrentAuthorityAsync();
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

        StatusMessage = ActionType == "approve-publish"
            ? $"{ChecklistName} ready to approve and publish. Same as previous shift is {(AllowSameAsPreviousShift ? "enabled" : "disabled")} for this checklist. This will later create a new published version and audit record."
            : $"{ChecklistName} draft layout changes ready to save. This will later save section order, field rules, dropdown options, reuse rules, schematic source rules, and version history.";

        return Page();
    }

    private async Task LoadCurrentAuthorityAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        IsSeniorChecklistPublisher = CurrentUserService.IsSeniorAccessRole(currentUser?.AppRole?.Name);
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
                "Optional reuse control shown below vehicle details.",
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
