using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class HomeModel : PageModel
{
    public void OnGet()
    {
        ViewData["ClientName"] = "Client Business Name";
    }
}
