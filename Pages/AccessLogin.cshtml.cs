using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AccessLoginModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly SignInManager<ApplicationIdentityUser> _signInManager;

    public AccessLoginModel(
        CurrentUserService currentUser,
        SignInManager<ApplicationIdentityUser> signInManager)
    {
        _currentUser = currentUser;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await _signInManager.SignOutAsync();
        _currentUser.SignOutCurrentUserOnly();

        if (!HttpContext.Session.GetInt32(CurrentUserService.CompanyIdSessionKey).HasValue)
        {
            return RedirectToPage("/CompanyLogin");
        }

        return RedirectToPage("/Access");
    }
}
