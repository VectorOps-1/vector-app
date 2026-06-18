using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AccessLoginModel : PageModel
{
    private readonly CurrentUserService _currentUser;

    public AccessLoginModel(CurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public IActionResult OnGet()
    {
        _currentUser.SignOutCurrentUserOnly();

        if (!HttpContext.Session.GetInt32(CurrentUserService.CompanyIdSessionKey).HasValue)
        {
            return RedirectToPage("/CompanyLogin");
        }

        return RedirectToPage("/Access");
    }
}
