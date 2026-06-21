using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AppGuideModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly IWebHostEnvironment _environment;

    public AppGuideModel(CurrentUserService currentUser, IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _environment = environment;
    }

    public string ClientName { get; private set; } = "Company Workspace";
    public string CompanyLogoPath { get; private set; } = CompanyBranding.DefaultLogoPath;
    public string AccessView { get; private set; } = CurrentUserService.OperationalManagementAccess;
    public bool IsStaff => AccessView == CurrentUserService.StaffAccess;
    public bool IsOperationalManagement => AccessView == CurrentUserService.OperationalManagementAccess;
    public bool IsSeniorManagement => AccessView == CurrentUserService.SeniorManagementAccess;
    public bool CanViewManagementGuide => IsOperationalManagement || IsSeniorManagement;
    public string GuideScope => IsStaff
        ? "Staff guide"
        : IsSeniorManagement
            ? "Senior management guide"
            : "Operational management guide";

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView) });
        }

        var branding = await _currentUser.GetCurrentCompanyBrandingAsync(_environment);
        ClientName = branding.ClientName;
        CompanyLogoPath = branding.LogoPath;
        AccessView = CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView);
        ViewData["ClientName"] = ClientName;

        return Page();
    }
}
