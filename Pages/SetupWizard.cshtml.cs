using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class SetupWizardModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly VectorDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public SetupWizardModel(CurrentUserService currentUser, VectorDbContext db, IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _db = db;
        _environment = environment;
    }

    public string ClientName { get; private set; } = CompanyBranding.DefaultCompanyName;
    public string CompanyLogoPath { get; private set; } = CompanyBranding.DefaultLogoPath;
    public string SetupStatus { get; private set; } = CompanyBranding.BrandingStatusIncomplete;
    public string SignedInName { get; private set; } = string.Empty;
    public string SignedInRole { get; private set; } = string.Empty;
    public bool CanManageSetup { get; private set; }
    public SetupWizardStepDefinition CurrentStep { get; private set; } = SetupWizardProgress.Steps[0];
    public IReadOnlySet<string> CompletedStepKeys { get; private set; } = new HashSet<string>();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var company = currentUser.Company ?? await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        if (CompanySetupState.IsSetupComplete(company))
        {
            return RedirectToPage("/Home");
        }

        if (SetupWizardProgress.EnsureCurrentStep(company))
        {
            await _db.SaveChangesAsync();
        }

        ApplyPageState(company, currentUser);
        return Page();
    }

    private void ApplyPageState(Company company, AppUser currentUser)
    {
        ClientName = CompanyBranding.GetDisplayCompanyName(company);
        CompanyLogoPath = CompanyBranding.GetLogoPath(_environment, company);
        SetupStatus = CompanySetupState.DisplayStatus(company);
        SignedInName = currentUser.FullName;
        SignedInRole = currentUser.AppRole?.Name ?? string.Empty;
        CanManageSetup = CurrentUserService.IsSeniorAccessRole(SignedInRole);
        CurrentStep = SetupWizardProgress.GetCurrentStep(company);
        CompletedStepKeys = SetupWizardProgress.GetCompletedStepKeys(company);
        ViewData["ClientName"] = ClientName;
    }
}
