using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class CompanyLoginModel : PageModel
{
    private const string AccessGateLogoPath = "/acuityops-app-icon-light.png";

    private readonly VectorDbContext _db;
    private readonly AuditTrailService _auditTrail;
    private readonly SignInManager<ApplicationIdentityUser> _signInManager;

    public CompanyLoginModel(
        VectorDbContext db,
        AuditTrailService auditTrail,
        SignInManager<ApplicationIdentityUser> signInManager)
    {
        _db = db;
        _auditTrail = auditTrail;
        _signInManager = signInManager;
    }

    [BindProperty]
    public string? WorkspaceSlug { get; set; }

    [BindProperty]
    public string? CompanyAccessCode { get; set; }

    public string? LoginError { get; private set; }
    public bool ShowLoginForm { get; private set; }
    public string WorkspaceContext { get; private set; } = "AcuityOps company access gate";
    public string LogoPath { get; private set; } = AccessGateLogoPath;
    public string PageSubtitle { get; private set; } = "Use the company workspace link issued during onboarding";
    public string SecurityNote { get; private set; } = "This company-level access gate is separate from individual staff, operational management, and senior management logins.";

    public async Task<IActionResult> OnGetAsync(string? workspaceSlug)
    {
        await ClearCompanyAndUserSessionAsync();
        var requestedSlug = CompanyWorkspaceAccess.NormalizeWorkspaceSlug(workspaceSlug);
        if (string.IsNullOrWhiteSpace(requestedSlug))
            requestedSlug = CompanyWorkspaceAccess.NormalizeWorkspaceSlug(Request.Cookies[CompanyWorkspaceAccess.LastWorkspaceCookieName]);
        WorkspaceSlug = requestedSlug;
        ViewData["ClientName"] = "Company Workspace";

        var company = await LoadCompanyFromWorkspaceAsync(requestedSlug);
        if (company is null)
        {
            if (!string.IsNullOrWhiteSpace(Request.Cookies[CompanyWorkspaceAccess.LastWorkspaceCookieName]))
                Response.Cookies.Delete(CompanyWorkspaceAccess.LastWorkspaceCookieName);
            ShowLoginForm = false;
            LoginError = "A valid company workspace link is required.";
            SecurityNote = "Enter or paste the workspace link issued to your company. AcuityOps does not use a default company workspace.";
            return Page();
        }

        var canonicalSlug = company.WorkspaceSlug!;
        RememberWorkspace(canonicalSlug);
        if (!string.Equals(RouteData.Values["workspaceSlug"]?.ToString(), canonicalSlug, StringComparison.OrdinalIgnoreCase))
            return RedirectToPage("/CompanyLogin", new { workspaceSlug = canonicalSlug });

        ShowLoginForm = true;
        PageSubtitle = "Enter the company access code issued during onboarding";
        CompanyAccessCode = string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? workspaceSlug)
    {
        WorkspaceSlug = CompanyWorkspaceAccess.NormalizeWorkspaceSlug(WorkspaceSlug ?? workspaceSlug);
        ViewData["ClientName"] = "Company Workspace";

        var company = await LoadCompanyFromWorkspaceAsync(WorkspaceSlug);
        if (company is null)
        {
            await ClearCompanyAndUserSessionAsync();
            ShowLoginForm = false;
            LoginError = "A valid company workspace link is required.";
            SecurityNote = "The company login is hidden unless opened from the workspace link created during onboarding.";
            return Page();
        }

        ShowLoginForm = true;
        PageSubtitle = "Enter the company access code issued during onboarding";
        RememberWorkspace(company.WorkspaceSlug!);

        if (!CompanyWorkspaceAccess.AccessCodeMatches(company, CompanyAccessCode))
        {
            LoginError = "The company access code is incorrect.";
            return Page();
        }

        await ClearCompanyAndUserSessionAsync();
        HttpContext.Session.SetInt32(CurrentUserService.CompanyIdSessionKey, company.Id);

        await _auditTrail.RecordAndSaveAsync(
            company.Id,
            null,
            "Company workspace accessed",
            "Company",
            company.Id,
            $"{CompanyBranding.GetDisplayCompanyName(company)} company-level login accepted.");

        return RedirectToPage("/Access");
    }

    private async Task<vector_app_local.Models.Company?> LoadCompanyFromWorkspaceAsync(string? workspaceSlug)
    {
        if (string.IsNullOrWhiteSpace(workspaceSlug))
        {
            return null;
        }

        var normalizedSlug = workspaceSlug.Trim();
        return await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.WorkspaceSlug == normalizedSlug &&
                item.Status == "Active");
    }

    private void RememberWorkspace(string workspaceSlug)
    {
        Response.Cookies.Append(CompanyWorkspaceAccess.LastWorkspaceCookieName, workspaceSlug, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(365)
        });
    }

    private async Task ClearCompanyAndUserSessionAsync()
    {
        await _signInManager.SignOutAsync();
        HttpContext.Session.Remove(CurrentUserService.UserIdSessionKey);
        HttpContext.Session.Remove(CurrentUserService.CompanyIdSessionKey);
        HttpContext.Session.Remove(CurrentUserService.FullNameSessionKey);
        HttpContext.Session.Remove(CurrentUserService.RoleNameSessionKey);
        HttpContext.Session.Remove(CurrentUserService.AccessViewSessionKey);
    }
}
