using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

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
        if (!HttpContext.Session.GetInt32(CurrentUserService.CompanyIdSessionKey).HasValue)
        {
            HttpContext.Session.Remove(CurrentUserService.UserIdSessionKey);
            HttpContext.Session.Remove(CurrentUserService.FullNameSessionKey);
            HttpContext.Session.Remove(CurrentUserService.RoleNameSessionKey);
            HttpContext.Session.Remove(CurrentUserService.AccessViewSessionKey);
            return RedirectToPage("/CompanyLogin");
        }

        Workspace = AcuityOpsWorkspaceName;
        LogoPath = AcuityOpsLogoPath;
        ViewData["ClientName"] = Workspace;

        return Page();
    }
}
