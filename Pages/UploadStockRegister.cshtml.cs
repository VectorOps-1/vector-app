using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class UploadStockRegisterModel : PageModel
{
    [BindProperty]
    public IFormFile? StockRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("/StockRegister");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/StockRegister");
    }
}
