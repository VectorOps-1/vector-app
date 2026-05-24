using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class HomeModel : PageModel
{
    public string AccessView { get; private set; } = "operational-management";

    public void OnGet(string? access)
    {
        ViewData["ClientName"] = "Client Business Name";

        AccessView = access switch
        {
            "staff" => "staff",
            "senior-management" => "senior-management",
            _ => "operational-management"
        };
    }
}
