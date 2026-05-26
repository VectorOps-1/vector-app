using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadEquipmentRegisterModel : PageModel
{
    [BindProperty]
    public IFormFile? EquipmentRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (EquipmentRegisterFile is null || EquipmentRegisterFile.Length == 0)
        {
            StatusMessage = "Select an Excel equipment register before continuing.";
            return Page();
        }

        var extension = Path.GetExtension(EquipmentRegisterFile.FileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".xls")
        {
            StatusMessage = "Upload an Excel file only: .xlsx or .xls.";
            return Page();
        }

        return RedirectToPage("/EquipmentRegisterPreview", new { fileName = EquipmentRegisterFile.FileName });
    }
}
