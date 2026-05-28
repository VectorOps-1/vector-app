using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class CompanyLoginModel : PageModel
{
    private readonly VectorDbContext _db;

    public CompanyLoginModel(VectorDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public string? CompanyId { get; set; }

    public string? LoginError { get; private set; }

    public void OnGet()
    {
        ViewData["ClientName"] = "Company Workspace";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["ClientName"] = "Company Workspace";

        if (!int.TryParse(CompanyId?.Trim(), out var companyId))
        {
            LoginError = "Enter a valid company ID.";
            return Page();
        }

        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId && item.Status == "Active");

        if (company is null)
        {
            LoginError = "No active company subscription was found for that ID.";
            return Page();
        }

        HttpContext.Session.Remove(CurrentUserService.UserIdSessionKey);
        HttpContext.Session.Remove(CurrentUserService.FullNameSessionKey);
        HttpContext.Session.Remove(CurrentUserService.RoleNameSessionKey);
        HttpContext.Session.Remove(CurrentUserService.AccessViewSessionKey);
        HttpContext.Session.SetInt32(CurrentUserService.CompanyIdSessionKey, company.Id);
        HttpContext.Session.SetString("Vector.CompanyName", company.Name);

        return RedirectToPage("/Access");
    }
}
