using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadVehicleRegisterModel : PageModel
{
    [BindProperty]
    public IFormFile? VehicleRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (VehicleRegisterFile is null || VehicleRegisterFile.Length == 0)
        {
            StatusMessage = "Select an Excel vehicle register before continuing.";
            return Page();
        }

        var extension = Path.GetExtension(VehicleRegisterFile.FileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".xls")
        {
            StatusMessage = "Upload an Excel file only: .xlsx or .xls.";
            return Page();
        }

        return RedirectToPage("/VehicleRegisterPreview", new { fileName = VehicleRegisterFile.FileName });
    }
}
