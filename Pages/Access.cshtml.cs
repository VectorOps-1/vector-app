using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AccessModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public AccessModel(VectorDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public string Workspace { get; private set; } = "Selected Workspace";
    public bool ShowSplash { get; private set; }
    public string LogoPath { get; private set; } = "/acuityops-app-icon-light.png";

    public async Task<IActionResult> OnGetAsync()
    {
        var companyId = HttpContext.Session.GetInt32(CurrentUserService.CompanyIdSessionKey);
        if (!companyId.HasValue)
        {
            if (!_environment.IsDevelopment())
            {
                return RedirectToPage("/CompanyLogin");
            }

            var developmentCompany = await _db.Companies
                .AsNoTracking()
                .Where(item => item.Status == "Active")
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync();

            if (developmentCompany is null)
            {
                return RedirectToPage("/CompanyLogin");
            }

            HttpContext.Session.SetInt32(CurrentUserService.CompanyIdSessionKey, developmentCompany.Id);
            companyId = developmentCompany.Id;
        }

        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId.Value && item.Status == "Active");

        if (company is null)
        {
            HttpContext.Session.Remove(CurrentUserService.CompanyIdSessionKey);
            HttpContext.Session.Remove("Vector.CompanyName");
            return RedirectToPage("/CompanyLogin");
        }

        Workspace = CompanyBranding.GetDisplayCompanyName(company);
        LogoPath = CompanyBranding.GetLogoPath(_environment, company);
        ViewData["ClientName"] = Workspace;
        HttpContext.Session.SetString("Vector.CompanyName", Workspace);

        return Page();
    }
}
