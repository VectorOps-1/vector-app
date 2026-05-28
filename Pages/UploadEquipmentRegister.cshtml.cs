using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadEquipmentRegisterModel : PageModel
{
    [BindProperty]
    public IFormFile? EquipmentRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("/EquipmentRegister");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/EquipmentRegister");
    }
}
