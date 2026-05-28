using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadVehicleRegisterModel : PageModel
{
    [BindProperty]
    public IFormFile? VehicleRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("/VehicleRegister");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/VehicleRegister");
    }
}
