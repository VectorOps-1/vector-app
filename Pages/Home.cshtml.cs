using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class HomeModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly VectorDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public HomeModel(CurrentUserService currentUser, VectorDbContext db, IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _db = db;
        _environment = environment;
    }

    public string AccessView { get; private set; } = "operational-management";
    public string? SignedInName { get; private set; }
    public string? SignedInRole { get; private set; }
    public int? SignedInUserId { get; private set; }
    public string ClientName { get; private set; } = "Company Workspace";
    public string CompanyLogoPath { get; private set; } = CompanyBranding.DefaultLogoPath;
    public bool PermissionDenied { get; private set; }
    public bool PermissionSetupRequired { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? access, bool permissionDenied = false, bool permissionSetupRequired = false)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.NormalizeAccessView(access) });
        }

        var branding = await _currentUser.GetCurrentCompanyBrandingAsync(_environment);
        ClientName = branding.ClientName;
        CompanyLogoPath = branding.LogoPath;
        ViewData["ClientName"] = ClientName;

        AccessView = CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView);
        SignedInName = currentUser.FullName;
        SignedInRole = currentUser.AppRole?.Name;
        SignedInUserId = currentUser.Id;
        PermissionSetupRequired = permissionSetupRequired;
        PermissionDenied = permissionDenied;

        if (!CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            var hasSavedPermissionRows = await _db.AppUserAccessPermissions
                .AsNoTracking()
                .AnyAsync(permission =>
                    permission.CompanyId == currentUser.CompanyId &&
                    permission.AppUserId == currentUser.Id);

            PermissionSetupRequired = PermissionSetupRequired || !hasSavedPermissionRows;
            if (PermissionSetupRequired)
            {
                PermissionDenied = false;
            }
        }

        return Page();
    }
}
