using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadChecklistModel : PageModel
{
    [BindProperty]
    public IFormFile? ChecklistFile { get; set; }

    public string? StatusMessage { get; private set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("/EditChecklist");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/EditChecklist");
    }
}
