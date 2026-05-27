using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadMedicationRegisterModel : PageModel
{
    [BindProperty]
    public IFormFile? MedicationRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (MedicationRegisterFile is null || MedicationRegisterFile.Length == 0)
        {
            StatusMessage = "Select an Excel medication register before continuing.";
            return Page();
        }

        var extension = Path.GetExtension(MedicationRegisterFile.FileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".xls")
        {
            StatusMessage = "Upload an Excel file only: .xlsx or .xls.";
            return Page();
        }

        return RedirectToPage("/MedicationRegisterPreview", new { fileName = MedicationRegisterFile.FileName });
    }
}
