using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class IndexModel : PageModel
{
    private const string AcuityOpsClientName = "AcuityOps";
    private const string AcuityOpsLogoPath = "/acuityops-app-icon-light.png";

    public bool ShowSplash { get; private set; } = true;
    public string ClientName { get; private set; } = AcuityOpsClientName;
    public string LogoPath { get; private set; } = AcuityOpsLogoPath;

    public IActionResult OnGet()
    {
        ShowSplash = ShouldShowSplash();
        ClientName = AcuityOpsClientName;
        LogoPath = AcuityOpsLogoPath;
        ViewData["ClientName"] = ClientName;

        return Page();
    }

    private bool ShouldShowSplash()
    {
        if (HttpContext.Session.GetInt32(CurrentUserService.UserIdSessionKey).HasValue)
        {
            HttpContext.Session.SetString(CurrentUserService.SplashShownSessionKey, "true");
            return false;
        }

        if (HttpContext.Session.GetString(CurrentUserService.SplashShownSessionKey) == "true")
        {
            return false;
        }

        HttpContext.Session.SetString(CurrentUserService.SplashShownSessionKey, "true");
        return true;
    }
}
