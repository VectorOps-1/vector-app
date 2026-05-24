using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class HomeModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public HomeModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string AccessView { get; private set; } = "operational-management";

    public void OnGet(string? access)
    {
        ViewData["ClientName"] = CompanyBranding.GetCompanyName(_environment);

        AccessView = access switch
        {
            "staff" => "staff",
            "senior-management" => "senior-management",
            _ => "operational-management"
        };
    }
}
