using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class HomeModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly IWebHostEnvironment _environment;

    public HomeModel(CurrentUserService currentUser, IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _environment = environment;
    }

    public string AccessView { get; private set; } = "operational-management";
    public string? SignedInName { get; private set; }
    public string? SignedInRole { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? access)
    {
        ViewData["ClientName"] = CompanyBranding.GetCompanyName(_environment);

        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.NormalizeAccessView(access) });
        }

        AccessView = CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView);
        SignedInName = currentUser.FullName;
        SignedInRole = currentUser.AppRole?.Name;

        return Page();
    }
}
