using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class CompanyNameModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AuditTrailService _auditTrail;

    public CompanyNameModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        AuditTrailService auditTrail)
    {
        _db = db;
        _currentUser = currentUser;
        _auditTrail = auditTrail;
    }

    [BindProperty]
    public string CompanyName { get; set; } = string.Empty;

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var company = await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        CompanyName = CompanyBranding.GetSavedCompanyName(company) ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var company = await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        var submittedName = CompanyName?.Trim() ?? string.Empty;
        company.Name = submittedName;
        company.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is not null)
        {
            var auditDetail = string.IsNullOrWhiteSpace(submittedName)
                ? "Company display name cleared in Master Setup."
                : $"Company display name changed to {submittedName}.";

            await _auditTrail.RecordAndSaveAsync(
                currentUser.CompanyId,
                currentUser.Id,
                "Company display name updated",
                "Company",
                currentUser.CompanyId,
                auditDetail);
        }

        StatusMessage = string.IsNullOrWhiteSpace(submittedName)
            ? "Company name cleared. AcuityOps will show a neutral placeholder until a name is saved."
            : "Company name saved.";
        ActionSaved = true;
        CompanyName = submittedName;
        return Page();
    }
}
