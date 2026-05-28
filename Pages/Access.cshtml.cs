using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AccessModel : PageModel
{
    private readonly VectorDbContext _db;

    public AccessModel(VectorDbContext db)
    {
        _db = db;
    }

    public string Workspace { get; private set; } = "Selected Workspace";

    public async Task<IActionResult> OnGetAsync()
    {
        var companyId = HttpContext.Session.GetInt32(CurrentUserService.CompanyIdSessionKey);
        if (!companyId.HasValue)
        {
            return RedirectToPage("/CompanyLogin");
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

        Workspace = company.Name;
        ViewData["ClientName"] = company.Name;
        HttpContext.Session.SetString("Vector.CompanyName", company.Name);

        return Page();
    }
}
