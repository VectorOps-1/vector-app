using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class CompanyLoginModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public CompanyLoginModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public void OnGet()
    {
        ViewData["ClientName"] = CompanyBranding.GetCompanyName(_environment);
    }
}
