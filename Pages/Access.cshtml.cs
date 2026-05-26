using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AccessModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public AccessModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string Workspace { get; private set; } = "Selected Workspace";

    public void OnGet(string? workspace)
    {
        ViewData["ClientName"] = CompanyBranding.GetCompanyName(_environment);
        Workspace = string.IsNullOrWhiteSpace(workspace) ? "Selected Workspace" : workspace;
    }
}
