using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly VectorDbContext _db;

    public IndexModel(IWebHostEnvironment environment, VectorDbContext db)
    {
        _environment = environment;
        _db = db;
    }

    public bool ShowSplash { get; private set; } = true;
    public string ClientName { get; private set; } = "Company Workspace";
    public string LogoPath { get; private set; } = "/acuityops-app-icon-light.png";

    public async Task<IActionResult> OnGetAsync()
    {
        ShowSplash = ShouldShowSplash();
        var company = await LoadSelectedCompanyAsync();

        if (company is null && !_environment.IsDevelopment())
        {
            return RedirectToPage("/CompanyLogin");
        }

        ClientName = CompanyBranding.GetDisplayCompanyName(company);
        LogoPath = CompanyBranding.GetLogoPath(_environment, company);
        ViewData["ClientName"] = ClientName;

        return Page();
    }

    private bool ShouldShowSplash()
    {
        if (HttpContext.Session.GetInt32(CurrentUserService.UserIdSessionKey).HasValue)
        {
            HttpContext.Session.SetString(CurrentUserService.SplashShownSessionKey, "true");
            return false;
        }

        if (HttpContext.Session.GetString(CurrentUserService.SplashShownSessionKey) == "true")
        {
            return false;
        }

        HttpContext.Session.SetString(CurrentUserService.SplashShownSessionKey, "true");
        return true;
    }

    private async Task<Company?> LoadSelectedCompanyAsync()
    {
        var companyId = HttpContext.Session.GetInt32(CurrentUserService.CompanyIdSessionKey);
        if (companyId.HasValue)
        {
            var selectedCompany = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(company => company.Id == companyId.Value && company.Status == "Active");

            if (selectedCompany is not null)
            {
                HttpContext.Session.SetString("Vector.CompanyName", CompanyBranding.GetDisplayCompanyName(selectedCompany));
                return selectedCompany;
            }

            HttpContext.Session.Remove(CurrentUserService.CompanyIdSessionKey);
            HttpContext.Session.Remove("Vector.CompanyName");
        }

        if (!_environment.IsDevelopment())
        {
            return null;
        }

        var developmentCompany = await _db.Companies
            .AsNoTracking()
            .Where(company => company.Status == "Active")
            .OrderBy(company => company.Id)
            .FirstOrDefaultAsync();

        if (developmentCompany is not null)
        {
            HttpContext.Session.SetInt32(CurrentUserService.CompanyIdSessionKey, developmentCompany.Id);
            HttpContext.Session.SetString("Vector.CompanyName", CompanyBranding.GetDisplayCompanyName(developmentCompany));
        }

        return developmentCompany;
    }
}
