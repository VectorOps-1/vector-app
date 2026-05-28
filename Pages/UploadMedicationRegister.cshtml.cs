using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadMedicationRegisterModel : PageModel
{
    [BindProperty]
    public IFormFile? MedicationRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("/MedicationRegister");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/MedicationRegister");
    }
}
