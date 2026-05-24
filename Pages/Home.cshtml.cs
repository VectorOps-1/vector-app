using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class HomeModel : PageModel
{
    public string AccessView { get; private set; } = "manager";

    public void OnGet(string? access)
    {
        ViewData["ClientName"] = "Client Business Name";
        AccessView = access == "operational-staff" ? "operational-staff" : "manager";
    }
}
