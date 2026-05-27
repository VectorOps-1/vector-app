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

    [BindProperty] public string? ChecklistName { get; set; }
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

    public List<ChecklistFieldExample> ExampleFields { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await LoadCurrentAuthorityAsync();
        LoadExampleFields();
        DropdownOptions = "Full\n3/4\n1/2\n1/4\nEmpty";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadCurrentAuthorityAsync();
        LoadExampleFields();

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
            : $"{ChecklistName} draft changes ready to save. This will later save editable fields, dropdown options, reuse rules, schematic source rules, and version history.";

        return Page();
    }

    private async Task LoadCurrentAuthorityAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        IsSeniorChecklistPublisher = CurrentUserService.IsSeniorAccessRole(currentUser?.AppRole?.Name);
    }

    private void LoadExampleFields()
    {
        ExampleFields = new List<ChecklistFieldExample>
        {
            new("Registration-driven vehicle lookup", "Dropdown"),
            new("Vehicle / callsign", "Text"),
            new("Current kilometres", "Number"),
            new("Fuel level", "Dropdown"),
            new("Vehicle condition", "Dropdown"),
            new("Vehicle schematic from master setup", "Schematic Markup"),
            new("Same as previous shift", "Permissioned Action"),
            new("Inspection notes", "Text")
        };
    }
}

public record ChecklistFieldExample(string Label, string Type);
