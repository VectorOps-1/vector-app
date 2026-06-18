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
    public int? SignedInUserId { get; private set; }
    public string ClientName { get; private set; } = "Company Workspace";
    public string CompanyLogoPath { get; private set; } = "/acuityops-app-icon-light.png";

    public async Task<IActionResult> OnGetAsync(string? access)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.NormalizeAccessView(access) });
        }

        ClientName = CompanyBranding.GetDisplayCompanyName(currentUser.Company);
        CompanyLogoPath = CompanyBranding.GetLogoPath(_environment, currentUser.Company);
        ViewData["ClientName"] = ClientName;

        AccessView = CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView);
        SignedInName = currentUser.FullName;
        SignedInRole = currentUser.AppRole?.Name;
        SignedInUserId = currentUser.Id;

        return Page();
    }
}
