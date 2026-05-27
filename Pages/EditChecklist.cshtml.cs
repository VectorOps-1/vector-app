using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class EditChecklistModel : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
    }
}
