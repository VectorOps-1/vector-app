using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class LogoUploadModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly CurrentUserService _currentUser;
    private readonly AuditTrailService _auditTrail;
    private const long MaxLogoBytes = 4 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".png"];
    private static readonly string[] AllowedContentTypes = ["image/png"];

    public LogoUploadModel(
        VectorDbContext db,
        IWebHostEnvironment environment,
        CurrentUserService currentUser,
        AuditTrailService auditTrail)
    {
        _db = db;
        _environment = environment;
        _currentUser = currentUser;
        _auditTrail = auditTrail;
    }

    [BindProperty]
    public IFormFile? LogoFile { get; set; }

    public string? ExistingLogoPath { get; private set; }
    public string? StatusMessage { get; private set; }
    public bool CanRemoveLogo { get; private set; }
    public bool ActionSaved { get; private set; }
    public string ActionConfirmationMessage { get; private set; } = "Company logo saved.";
    public string MaxLogoSizeLabel { get; } = "4 MB";

    public async Task<IActionResult> OnGetAsync()
    {
        var company = await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        ApplyLogoState(company);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync()
    {
        var company = await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        DeleteCompanyLogoFiles(company.Id);
        company.LogoStoragePath = null;
        company.LogoRemoved = true;
        company.BrandingStatus = CompanyBranding.GetBrandingStatus(company);
        company.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var currentUser = await _currentUser.GetCurrentUserAsync();
        await _auditTrail.RecordAndSaveAsync(
            company.Id,
            currentUser?.Id,
            "Company logo removed",
            "Company",
            company.Id,
            $"{CompanyBranding.GetDisplayCompanyName(company)} company logo removed.");

        StatusMessage = "Logo removed. Default AcuityOps branding will be used until a new company logo is uploaded.";
        ActionSaved = true;
        ActionConfirmationMessage = "Company logo removed.";
        ApplyLogoState(company);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var company = await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        if (LogoFile is null || LogoFile.Length == 0)
        {
            StatusMessage = "Choose a transparent PNG logo before saving.";
            ApplyLogoState(company);
            return Page();
        }

        var extension = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(LogoFile.ContentType))
        {
            StatusMessage = "Only PNG logo files are supported. Use a transparent PNG for the cleanest borderless result.";
            ApplyLogoState(company);
            return Page();
        }

        if (LogoFile.Length > MaxLogoBytes)
        {
            StatusMessage = $"The logo file is too large. Upload a PNG smaller than {MaxLogoSizeLabel}.";
            ApplyLogoState(company);
            return Page();
        }

        var uploadFolder = GetCompanyLogoFolder(company.Id);
        Directory.CreateDirectory(uploadFolder);

        DeleteCompanyLogoFiles(company.Id);

        var fileName = $"company-logo{extension}";
        var filePath = Path.Combine(uploadFolder, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await LogoFile.CopyToAsync(stream);

        company.LogoStoragePath = $"/uploads/company/{company.Id}/{fileName}";
        company.LogoRemoved = false;
        company.BrandingStatus = CompanyBranding.GetBrandingStatus(company);
        company.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var currentUser = await _currentUser.GetCurrentUserAsync();
        await _auditTrail.RecordAndSaveAsync(
            company.Id,
            currentUser?.Id,
            "Company logo updated",
            "Company",
            company.Id,
            $"{CompanyBranding.GetDisplayCompanyName(company)} company logo updated.");

        StatusMessage = "Logo saved.";
        ActionSaved = true;
        ApplyLogoState(company);
        return Page();
    }

    private void ApplyLogoState(Company company)
    {
        ExistingLogoPath = GetExistingLogoPath(company);
        CanRemoveLogo = !string.IsNullOrWhiteSpace(company.LogoStoragePath);
    }

    private string? GetExistingLogoPath(Company company)
    {
        var logoPath = CompanyBranding.GetLogoPath(_environment, company);

        return logoPath.StartsWith("/acuityops-app-icon-light.png", StringComparison.OrdinalIgnoreCase)
            ? null
            : logoPath;
    }

    private string GetCompanyLogoFolder(int companyId)
    {
        return Path.Combine(_environment.WebRootPath, "uploads", "company", companyId.ToString());
    }

    private void DeleteCompanyLogoFiles(int companyId)
    {
        var uploadFolder = GetCompanyLogoFolder(companyId);
        if (!Directory.Exists(uploadFolder))
        {
            return;
        }

        foreach (var existingFile in Directory.GetFiles(uploadFolder, "company-logo.*"))
        {
            System.IO.File.Delete(existingFile);
        }
    }
}
