using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadChecklistModel : PageModel
{
    [BindProperty]
    public IFormFile? ChecklistFile { get; set; }

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (ChecklistFile is null || ChecklistFile.Length == 0)
        {
            StatusMessage = "Select an Excel checklist file before continuing.";
            return Page();
        }

        var extension = Path.GetExtension(ChecklistFile.FileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".xls")
        {
            StatusMessage = "Upload an Excel file only: .xlsx or .xls.";
            return Page();
        }

        return RedirectToPage("/ChecklistPreview", new { fileName = ChecklistFile.FileName });
    }
}
