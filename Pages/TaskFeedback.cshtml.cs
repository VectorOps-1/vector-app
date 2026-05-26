using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class TaskFeedbackModel : PageModel
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp",
        ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"
    };

    [BindProperty] public string? Outcome { get; set; }
    [BindProperty] public string? RelatedItem { get; set; }
    [BindProperty] public string? FeedbackMessage { get; set; }
    [BindProperty] public List<IFormFile> EvidenceFiles { get; set; } = new();
    public string? StatusMessage { get; private set; }
    public bool FeedbackSaved { get; private set; }

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Outcome))
        {
            StatusMessage = "Select a task outcome before submitting feedback.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(FeedbackMessage))
        {
            StatusMessage = "Enter feedback before submitting.";
            return Page();
        }

        var unsupportedFile = EvidenceFiles.FirstOrDefault(file => !AllowedExtensions.Contains(Path.GetExtension(file.FileName)));
        if (unsupportedFile is not null)
        {
            StatusMessage = $"Unsupported file type: {unsupportedFile.FileName}.";
            return Page();
        }

        FeedbackSaved = true;
        StatusMessage = "Task feedback submitted. This will later attach to the assigned task, notify the manager, and create an audit record.";
        return Page();
    }
}
