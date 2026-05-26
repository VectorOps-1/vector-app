using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class ChecklistPreviewModel : PageModel
{
    public string FileName { get; private set; } = "Uploaded checklist";

    public void OnGet(string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName;
        }
    }
}
