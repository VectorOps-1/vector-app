using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class RoleLoginModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly IWebHostEnvironment _environment;
    private readonly IdentityAuthenticationService _authentication;
    private readonly SignInManager<ApplicationIdentityUser> _signInManager;

    public RoleLoginModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        IWebHostEnvironment environment,
        IdentityAuthenticationService authentication,
        SignInManager<ApplicationIdentityUser> signInManager)
    {
        _db = db;
        _currentUser = currentUser;
        _environment = environment;
        _authentication = authentication;
        _signInManager = signInManager;
    }

    public string AccessView { get; private set; } = "operational-management";
    public string RoleTitle { get; private set; } = "Operational Management Login";
    public string ClientName { get; private set; } = "Company Workspace";
    public string LogoPath { get; private set; } = "/acuityops-app-icon-light.png";
    public string? LoginError { get; private set; }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public async Task<IActionResult> OnGetAsync(string? access)
    {
        SetAccessView(access);

        var company = await LoadSelectedCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        ApplyCompanyBranding(company);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? access)
    {
        SetAccessView(access);

        var company = await LoadSelectedCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        ApplyCompanyBranding(company);

        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError(nameof(Email), "Enter the user email for this access level.");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ModelState.AddModelError(nameof(Password), "Enter your password.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _authentication.AuthenticateAsync(
            company.Id,
            Email!.Trim(),
            Password!,
            AccessView,
            HttpContext.RequestAborted);

        if (result.Status != IdentityAuthenticationStatus.Succeeded ||
            result.Profile is null ||
            result.Identity is null)
        {
            LoginError = result.Status == IdentityAuthenticationStatus.LockedOut
                ? "This login is temporarily locked after repeated failed attempts. Try again later or contact a senior manager."
                : "The email, password, or selected access level is incorrect.";
            return Page();
        }

        var user = result.Profile;

        var now = DateTime.UtcNow;
        user.LastLoginAtUtc = now;
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = user.CompanyId,
            AppUserId = user.Id,
            Action = "User signed in",
            EntityType = "AppUser",
            EntityId = user.Id,
            Details = $"{user.FullName} signed in to {RoleTitle}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(result.Identity, isPersistent: false);
        _currentUser.SignIn(user, AccessView);

        return result.Identity.MustChangePassword
            ? RedirectToPage("/ChangePassword")
            : RedirectToPage("/Home");
    }

    private void SetAccessView(string? access)
    {
        AccessView = CurrentUserService.NormalizeAccessView(access);
        RoleTitle = AccessView switch
        {
            CurrentUserService.StaffAccess => "Staff Login",
            CurrentUserService.SeniorManagementAccess => "Senior Management Login",
            _ => "Operational Management Login"
        };
    }

    private void ApplyCompanyBranding(Company company)
    {
        ClientName = CompanyBranding.GetDisplayCompanyName(company);
        LogoPath = CompanyBranding.GetLogoPath(_environment, company);
        ViewData["ClientName"] = ClientName;
    }

    private async Task<Company?> LoadSelectedCompanyAsync()
    {
        var companyId = HttpContext.Session.GetInt32(CurrentUserService.CompanyIdSessionKey);
        if (!companyId.HasValue)
        {
            return null;
        }

        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId.Value && item.Status == "Active");

        if (company is null)
        {
            HttpContext.Session.Remove(CurrentUserService.CompanyIdSessionKey);
            return null;
        }

        return company;
    }
}
