using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class AccessModel : PageModel
{
    private const string AcuityOpsWorkspaceName = "AcuityOps";
    private const string AcuityOpsLogoPath = "/acuityops-app-icon-light.png";

    public string Workspace { get; private set; } = AcuityOpsWorkspaceName;
    public bool ShowSplash { get; private set; }
    public string LogoPath { get; private set; } = AcuityOpsLogoPath;

    public IActionResult OnGet()
    {
        Workspace = AcuityOpsWorkspaceName;
        LogoPath = AcuityOpsLogoPath;
        ViewData["ClientName"] = Workspace;

        return Page();
    }
}
