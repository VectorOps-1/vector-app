using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class OnboardingModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly IWebHostEnvironment _environment;
    private readonly AuditTrailService _auditTrail;

    public OnboardingModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        IWebHostEnvironment environment,
        AuditTrailService auditTrail)
    {
        _db = db;
        _currentUser = currentUser;
        _environment = environment;
        _auditTrail = auditTrail;
    }

    public string? WorkspaceLoginUrl { get; private set; }
    public string? CompanyAccessCode { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["ClientName"] = CompanyBranding.DefaultCompanyName;

        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (!CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        var company = await _db.Companies.FirstOrDefaultAsync(item =>
            item.Id == currentUser.CompanyId &&
            item.Status == "Active");

        if (company is null)
        {
            StatusMessage = "No active company workspace was found for this user.";
            return Page();
        }

        var previousSlug = company.WorkspaceSlug;
        var previousAccessCode = company.WorkspaceAccessCode;
        CompanyWorkspaceAccess.EnsureWorkspaceAccess(company);

        if (!string.Equals(previousSlug, company.WorkspaceSlug, StringComparison.Ordinal) ||
            !string.Equals(previousAccessCode, company.WorkspaceAccessCode, StringComparison.Ordinal))
        {
            _auditTrail.Record(
                currentUser,
                "Company workspace access generated",
                "Company",
                company.Id,
                $"Workspace login link and company access code generated for {CompanyBranding.GetDisplayCompanyName(company)}.");
        }

        await _db.SaveChangesAsync();

        ViewData["ClientName"] = CompanyBranding.GetDisplayCompanyName(company);
        CompanyAccessCode = company.WorkspaceAccessCode;
        var relativeLoginUrl = Url.Page(
            "/CompanyLogin",
            values: new { workspaceSlug = company.WorkspaceSlug }) ?? $"/CompanyLogin/{company.WorkspaceSlug}";

        WorkspaceLoginUrl = $"{Request.Scheme}://{Request.Host}{relativeLoginUrl}";

        return Page();
    }
}
