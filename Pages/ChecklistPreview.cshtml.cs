using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class ChecklistPreviewModel : PageModel
{
    public string FileName { get; private set; } = "Uploaded checklist";

    public IActionResult OnGet(string? fileName)
    {
        return RedirectToPage("/EditChecklist");
    }
}
