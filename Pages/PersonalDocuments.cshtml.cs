using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class PersonalDocumentsModel : PageModel
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff",
        ".doc", ".docx", ".rtf", ".txt",
        ".xls", ".xlsx", ".csv"
    };

    public string SignedInStaffName { get; private set; } = "Current signed-in staff profile";

    public string SignedInStaffIdentifier { get; private set; } = "Auto-linked when authentication is connected";

    public string SignedInClinicalScope { get; private set; } = "Clinical qualification / scope not set";

    public string SignedInPractitionerNumber { get; private set; } = "Practitioner number not set";

    public DateTime? SignedInAnnualLicenseExpiryDate { get; private set; }

    public string SignedInCpdComplianceStatus { get; private set; } = "N/A";

    public DateTime? SignedInCpdComplianceExpiryDate { get; private set; }

    public List<PersonalDocumentGroup> DocumentGroups { get; private set; } = new();
    public List<SelectListItem> DocumentTypeOptions { get; private set; } = new();

    [BindProperty]
    public string? StaffName { get; set; }

    [BindProperty]
    public string? StaffIdentifier { get; set; }

    [BindProperty]
    public string? DocumentType { get; set; }

    [BindProperty]
    public string? DocumentTypeOther { get; set; }

    [BindProperty]
    public string? CertificateName { get; set; }

    [BindProperty]
    public DateTime? ExpiryDate { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    [BindProperty]
    public List<IFormFile> PersonalFiles { get; set; } = new();

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly CustomDropdownOptionService _customDropdownOptions;

    public PersonalDocumentsModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        IFileStorageService fileStorage,
        CustomDropdownOptionService customDropdownOptions)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _customDropdownOptions = customDropdownOptions;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        LoadSignedInStaffProfile(currentUser);
        await LoadExistingDocumentsAsync(currentUser);
        await LoadDocumentTypeOptionsAsync(currentUser.CompanyId);

        if (TempData["SuccessMessage"] is string message)
        {
            StatusMessage = message;
            ActionSaved = true;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        LoadSignedInStaffProfile(currentUser);
        await LoadExistingDocumentsAsync(currentUser);
        await LoadDocumentTypeOptionsAsync(currentUser.CompanyId);

        StaffName = SignedInStaffName;
        StaffIdentifier = SignedInStaffIdentifier;

        if (string.IsNullOrWhiteSpace(DocumentType))
        {
            StatusMessage = "Select a document type before submitting.";
            return Page();
        }

        var resolvedDocumentType = await _customDropdownOptions.ResolveSelectionAsync(
            currentUser.CompanyId,
            currentUser.Id,
            CustomDropdownOptionService.PersonalDocumentTypeKey,
            DocumentType,
            DocumentTypeOther,
            "Personal Documents");

        if (resolvedDocumentType is null)
        {
            StatusMessage = "Name the Other document type before submitting.";
            return Page();
        }

        if ((resolvedDocumentType is "Certificate" or "Accreditation" or "Professional Registration" or "Training Record") && string.IsNullOrWhiteSpace(CertificateName))
        {
            StatusMessage = "Enter the certificate, accreditation, registration, or training name before submitting.";
            return Page();
        }

        if (PersonalFiles.Count == 0)
        {
            StatusMessage = "Select one or more personal documents before submitting.";
            return Page();
        }

        var unsupportedFile = PersonalFiles.FirstOrDefault(file => !AllowedExtensions.Contains(Path.GetExtension(file.FileName)));
        if (unsupportedFile is not null)
        {
            StatusMessage = $"Unsupported file type: {unsupportedFile.FileName}.";
            return Page();
        }

        var documentName = string.IsNullOrWhiteSpace(CertificateName) ? resolvedDocumentType : CertificateName;
        var category = resolvedDocumentType;
        var notes = BuildNotes(CertificateName, ExpiryDate, Notes);
        var now = DateTime.UtcNow;
        var savedCount = 0;

        foreach (var file in PersonalFiles.Where(file => file.Length > 0))
        {
            var storedFile = await _fileStorage.SaveAsync(file, $"staff-{currentUser.Id}");
            _db.AssetFiles.Add(new AssetFile
            {
                CompanyId = currentUser.CompanyId,
                UploadedByUserId = currentUser.Id,
                LinkedEntityType = "Staff",
                LinkedEntityId = currentUser.Id,
                Category = category,
                OriginalFileName = storedFile.OriginalFileName,
                ContentType = storedFile.ContentType,
                StorageProvider = storedFile.ProviderName,
                StoragePath = storedFile.StoragePath,
                SizeBytes = storedFile.SizeBytes,
                Notes = notes,
                UploadedAtUtc = now
            });

            savedCount++;
        }

        if (savedCount == 0)
        {
            StatusMessage = "The selected files were empty and could not be saved.";
            return Page();
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Personal documents uploaded",
            EntityType = "AppUser",
            EntityId = currentUser.Id,
            Details = $"{savedCount} personal document(s) uploaded into {category} for {documentName}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = savedCount == 1
            ? "1 personal document saved against your profile."
            : $"{savedCount} personal documents saved against your profile.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetDownloadAsync(int fileId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        var storedFile = await _db.AssetFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(file =>
                file.Id == fileId &&
                file.CompanyId == currentUser.CompanyId &&
                file.LinkedEntityType == "Staff" &&
                file.LinkedEntityId == currentUser.Id);

        if (storedFile is null)
        {
            return RedirectToPage();
        }

        var stream = await _fileStorage.OpenReadAsync(storedFile.StoragePath);
        return File(stream, storedFile.ContentType, storedFile.OriginalFileName);
    }

    private void LoadSignedInStaffProfile(AppUser currentUser)
    {
        SignedInStaffName = currentUser.FullName;
        SignedInStaffIdentifier = string.IsNullOrWhiteSpace(currentUser.StaffIdentifier)
            ? "Staff ID not set"
            : currentUser.StaffIdentifier;
        SignedInClinicalScope = string.IsNullOrWhiteSpace(currentUser.QualificationFunction)
            ? "Clinical qualification / scope not set"
            : currentUser.QualificationFunction;
        SignedInPractitionerNumber = string.IsNullOrWhiteSpace(currentUser.PractitionerNumber)
            ? "Practitioner number not set"
            : currentUser.PractitionerNumber;
        SignedInAnnualLicenseExpiryDate = currentUser.AnnualLicenseExpiryDate;
        SignedInCpdComplianceStatus = string.IsNullOrWhiteSpace(currentUser.CpdComplianceStatus)
            ? "N/A"
            : currentUser.CpdComplianceStatus;
        SignedInCpdComplianceExpiryDate = currentUser.CpdComplianceExpiryDate;

        StaffName = SignedInStaffName;
        StaffIdentifier = SignedInStaffIdentifier;

    }

    private async Task LoadExistingDocumentsAsync(AppUser currentUser)
    {
        var documents = await _db.AssetFiles
            .AsNoTracking()
            .Where(file =>
                file.CompanyId == currentUser.CompanyId &&
                file.LinkedEntityType == "Staff" &&
                file.LinkedEntityId == currentUser.Id)
            .OrderBy(file => file.Category)
            .ThenByDescending(file => file.UploadedAtUtc)
            .Select(file => new PersonalDocumentRecord
            {
                Id = file.Id,
                Type = file.Category,
                FileName = file.OriginalFileName,
                ContentType = file.ContentType,
                SizeBytes = file.SizeBytes,
                Notes = file.Notes,
                UploadedAtUtc = file.UploadedAtUtc
            })
            .ToListAsync();

        DocumentGroups = documents
            .GroupBy(document => document.Type)
            .Select(group => new PersonalDocumentGroup
            {
                Type = group.Key,
                Documents = group.ToList()
            })
            .ToList();
    }

    private async Task LoadDocumentTypeOptionsAsync(int companyId)
    {
        DocumentTypeOptions = await _customDropdownOptions.BuildOptionsAsync(
            companyId,
            CustomDropdownOptionService.PersonalDocumentTypeKey,
            CustomDropdownOptionService.PersonalDocumentTypeDefaults,
            DocumentType);
    }

    private static string? BuildNotes(string? certificateName, DateTime? expiryDate, string? notes)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(certificateName))
        {
            parts.Add($"Name: {certificateName.Trim()}");
        }

        if (expiryDate.HasValue)
        {
            parts.Add($"Expiry: {expiryDate.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            parts.Add(notes.Trim());
        }

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }
}

public class PersonalDocumentGroup
{
    public string Type { get; set; } = string.Empty;
    public List<PersonalDocumentRecord> Documents { get; set; } = new();
}

public class PersonalDocumentRecord
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Notes { get; set; }
    public DateTime UploadedAtUtc { get; set; }

    public string SizeLabel => SizeBytes < 1024
        ? $"{SizeBytes} B"
        : SizeBytes < 1024 * 1024
            ? $"{SizeBytes / 1024d:0.0} KB"
            : $"{SizeBytes / 1024d / 1024d:0.0} MB";
}
