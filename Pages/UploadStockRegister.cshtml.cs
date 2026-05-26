using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadStockRegisterModel : PageModel
{
    [BindProperty]
    public IFormFile? StockRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (StockRegisterFile is null || StockRegisterFile.Length == 0)
        {
            StatusMessage = "Select an Excel stock register before continuing.";
            return Page();
        }

        var extension = Path.GetExtension(StockRegisterFile.FileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".xls")
        {
            StatusMessage = "Upload an Excel file only: .xlsx or .xls.";
            return Page();
        }

        return RedirectToPage("/StockRegisterPreview", new { fileName = StockRegisterFile.FileName });
    }
}
