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

    [BindProperty]
    public string TradingName { get; set; } = string.Empty;

    [BindProperty]
    public string ContactEmail { get; set; } = string.Empty;

    [BindProperty]
    public string ContactPhone { get; set; } = string.Empty;

    [BindProperty]
    public string Country { get; set; } = string.Empty;

    [BindProperty]
    public string Region { get; set; } = string.Empty;

    [BindProperty]
    public string Timezone { get; set; } = string.Empty;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public string WorkspaceAccessCode { get; private set; } = string.Empty;
    public string BrandingStatus { get; private set; } = CompanyBranding.BrandingStatusIncomplete;
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
        ApplyCompanySettings(company);
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
        var submittedTradingName = NormalizeOptionalSetting(TradingName);
        var submittedContactEmail = NormalizeOptionalSetting(ContactEmail);
        var submittedContactPhone = NormalizeOptionalSetting(ContactPhone);
        var submittedCountry = NormalizeOptionalSetting(Country);
        var submittedRegion = NormalizeOptionalSetting(Region);
        var submittedTimezone = NormalizeOptionalSetting(Timezone);

        company.Name = submittedName;
        company.TradingName = submittedTradingName;
        company.ContactEmail = submittedContactEmail;
        company.ContactPhone = submittedContactPhone;
        company.Country = submittedCountry;
        company.Region = submittedRegion;
        company.Timezone = submittedTimezone;
        company.BrandingStatus = CompanyBranding.GetBrandingStatus(company);
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
                "Company identity updated",
                "Company",
                currentUser.CompanyId,
                $"{auditDetail} Tenant settings updated from Master Setup.");
        }

        StatusMessage = string.IsNullOrWhiteSpace(submittedName)
            ? "Company identity saved. A neutral company workspace label will show until a company name is saved."
            : "Company identity saved.";
        ActionSaved = true;
        CompanyName = submittedName;
        ContactEmail = submittedContactEmail ?? string.Empty;
        ContactPhone = submittedContactPhone ?? string.Empty;
        Region = submittedRegion ?? string.Empty;
        ApplyCompanySettings(company);
        return Page();
    }

    private void ApplyCompanySettings(vector_app_local.Models.Company company)
    {
        TradingName = company.TradingName ?? string.Empty;
        ContactEmail = company.ContactEmail ?? string.Empty;
        ContactPhone = company.ContactPhone ?? string.Empty;
        Country = company.Country ?? string.Empty;
        Region = company.Region ?? string.Empty;
        Timezone = company.Timezone ?? string.Empty;
        WorkspaceSlug = company.WorkspaceSlug ?? "Not generated yet";
        WorkspaceAccessCode = string.IsNullOrWhiteSpace(company.WorkspaceAccessCode) ? "Not generated yet" : "Generated";
        BrandingStatus = string.IsNullOrWhiteSpace(company.BrandingStatus)
            ? CompanyBranding.GetBrandingStatus(company)
            : company.BrandingStatus;
    }

    private static string? NormalizeOptionalSetting(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }
}
