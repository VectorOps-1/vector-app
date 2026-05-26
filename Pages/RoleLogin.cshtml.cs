using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class RoleLoginModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public RoleLoginModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string AccessView { get; private set; } = "operational-management";
    public string RoleTitle { get; private set; } = "Operational Management Login";

    public void OnGet(string? access)
    {
        ViewData["ClientName"] = CompanyBranding.GetCompanyName(_environment);

        AccessView = access switch
        {
            "staff" => "staff",
            "senior-management" => "senior-management",
            _ => "operational-management"
        };

        RoleTitle = AccessView switch
        {
            "staff" => "Staff Login",
            "senior-management" => "Senior Management Login",
            _ => "Operational Management Login"
        };
    }
}
