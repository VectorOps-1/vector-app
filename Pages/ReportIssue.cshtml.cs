using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class ReportIssueModel : PageModel
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp",
        ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"
    };

    [BindProperty(SupportsGet = true)] public string Module { get; set; } = "General";
    [BindProperty] public string? IssueType { get; set; }
    [BindProperty] public string? RelatedItem { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string? Severity { get; set; }
    [BindProperty] public string? OperationalStatus { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public List<IFormFile> EvidenceFiles { get; set; } = new();

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public void OnGet(string? module)
    {
        Module = NormaliseModule(module ?? Module);
    }

    public IActionResult OnPost()
    {
        Module = NormaliseModule(Module);

        if (string.IsNullOrWhiteSpace(IssueType))
        {
            StatusMessage = "Select an issue type before submitting.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            StatusMessage = "Enter an issue description before submitting.";
            return Page();
        }

        var unsupportedFile = EvidenceFiles.FirstOrDefault(file => !AllowedExtensions.Contains(Path.GetExtension(file.FileName)));
        if (unsupportedFile is not null)
        {
            StatusMessage = $"Unsupported file type: {unsupportedFile.FileName}.";
            return Page();
        }

        ActionSaved = true;
        StatusMessage = $"{Module} issue ready to save. This will later create an issue record, evidence file links, manager visibility, task option, and audit proof.";
        return Page();
    }

    private static string NormaliseModule(string module)
    {
        return module.Trim().ToLowerInvariant() switch
        {
            "vehicle" or "vehicles" => "Vehicle",
            "equipment" => "Equipment",
            "stock" => "Stock",
            "medication" => "Medication",
            "staff" => "Staff",
            "checklist" => "Checklist",
            _ => "General"
        };
    }
}
