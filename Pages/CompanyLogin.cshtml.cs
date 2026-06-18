using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class CompanyLoginModel : PageModel
{
    private const string AccessGateLogoPath = "/acuityops-app-icon-light.png";

    private readonly VectorDbContext _db;
    private readonly AuditTrailService _auditTrail;

    public CompanyLoginModel(
        VectorDbContext db,
        AuditTrailService auditTrail)
    {
        _db = db;
        _auditTrail = auditTrail;
    }

    [BindProperty]
    public string? WorkspaceSlug { get; set; }

    [BindProperty]
    public string? CompanyAccessCode { get; set; }

    public string? LoginError { get; private set; }
    public bool ShowLoginForm { get; private set; }
    public string ClientName { get; private set; } = "Company Workspace";
    public string LogoPath { get; private set; } = AccessGateLogoPath;
    public string PageSubtitle { get; private set; } = "Use the company workspace link issued during onboarding";
    public string SecurityNote { get; private set; } = "This company-level access gate is separate from individual staff, operational management, and senior management logins.";

    public async Task OnGetAsync(string? workspaceSlug)
    {
        WorkspaceSlug = workspaceSlug;
        ViewData["ClientName"] = "Company Workspace";

        var company = await LoadCompanyFromWorkspaceAsync(workspaceSlug);
        if (company is null)
        {
            ShowLoginForm = false;
            LoginError = "A valid company workspace link is required.";
            SecurityNote = "The company login is hidden unless opened from the workspace link created during onboarding.";
            return;
        }

        ShowLoginForm = true;
        PageSubtitle = GetAccessCodeSubtitle(company);
        CompanyAccessCode = company.WorkspaceAccessCode;
        ApplyCompanyBranding(company);
    }

    public async Task<IActionResult> OnPostAsync(string? workspaceSlug)
    {
        WorkspaceSlug = WorkspaceSlug ?? workspaceSlug;
        ViewData["ClientName"] = "Company Workspace";

        var company = await LoadCompanyFromWorkspaceAsync(WorkspaceSlug);
        if (company is null)
        {
            ShowLoginForm = false;
            LoginError = "A valid company workspace link is required.";
            SecurityNote = "The company login is hidden unless opened from the workspace link created during onboarding.";
            return Page();
        }

        ShowLoginForm = true;
        PageSubtitle = GetAccessCodeSubtitle(company);
        ApplyCompanyBranding(company);

        if (!CompanyWorkspaceAccess.AccessCodeMatches(company, CompanyAccessCode))
        {
            LoginError = "The company access code is incorrect.";
            return Page();
        }

        HttpContext.Session.Remove(CurrentUserService.UserIdSessionKey);
        HttpContext.Session.Remove(CurrentUserService.FullNameSessionKey);
        HttpContext.Session.Remove(CurrentUserService.RoleNameSessionKey);
        HttpContext.Session.Remove(CurrentUserService.AccessViewSessionKey);
        HttpContext.Session.SetInt32(CurrentUserService.CompanyIdSessionKey, company.Id);
        HttpContext.Session.SetString("Vector.CompanyName", CompanyBranding.GetDisplayCompanyName(company));

        await _auditTrail.RecordAndSaveAsync(
            company.Id,
            null,
            "Company workspace accessed",
            "Company",
            company.Id,
            $"{CompanyBranding.GetDisplayCompanyName(company)} company-level login accepted.");

        return RedirectToPage("/Access");
    }

    private void ApplyCompanyBranding(vector_app_local.Models.Company company)
    {
        ClientName = CompanyBranding.GetDisplayCompanyName(company);
        LogoPath = AccessGateLogoPath;
        ViewData["ClientName"] = ClientName;
    }

    private static string GetAccessCodeSubtitle(vector_app_local.Models.Company company)
    {
        var savedCompanyName = CompanyBranding.GetSavedCompanyName(company);
        return savedCompanyName is null
            ? "Enter the company access code for this workspace"
            : $"Enter the company access code for {savedCompanyName}";
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
}
