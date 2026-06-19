using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class SupplierDetailsModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/StockOrders");
    }
}
