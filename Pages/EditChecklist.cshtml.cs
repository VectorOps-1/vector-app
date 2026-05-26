using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class EditChecklistModel : PageModel
{
    [BindProperty] public string? ChecklistName { get; set; }
    [BindProperty] public string ChecklistStatus { get; set; } = "Draft";
    [BindProperty] public string? DropdownField { get; set; }
    [BindProperty] public string? DropdownOptions { get; set; }
    [BindProperty] public string? AppliesTo { get; set; }
    [BindProperty] public string? PublishNote { get; set; }
    [BindProperty] public string? ActionType { get; set; }

    public string? StatusMessage { get; private set; }

    public List<ChecklistFieldExample> ExampleFields { get; private set; } = new();

    public void OnGet()
    {
        LoadExampleFields();
        DropdownOptions = "Full\n3/4\n1/2\n1/4\nEmpty";
    }

    public IActionResult OnPost()
    {
        LoadExampleFields();

        if (string.IsNullOrWhiteSpace(ChecklistName))
        {
            StatusMessage = "Select a checklist before saving or publishing.";
            return Page();
        }

        StatusMessage = ActionType == "approve-publish"
            ? $"{ChecklistName} ready to approve and publish. This will later create a new published version and audit record."
            : $"{ChecklistName} draft changes ready to save. This will later save editable fields, dropdown options, and version history.";

        return Page();
    }

    private void LoadExampleFields()
    {
        ExampleFields = new List<ChecklistFieldExample>
        {
            new("Vehicle / callsign", "Text"),
            new("Current kilometres", "Number"),
            new("Fuel level", "Dropdown"),
            new("Vehicle condition", "Dropdown"),
            new("Vehicle schematic", "Schematic Markup"),
            new("Inspection notes", "Text")
        };
    }
}

public record ChecklistFieldExample(string Label, string Type);
