using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class UploadStaffFilesModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public UploadStaffFilesModel(VectorDbContext db, CurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff",
        ".doc", ".docx", ".rtf", ".txt",
        ".xls", ".xlsx", ".csv"
    };

    [BindProperty(SupportsGet = true)] public int? StaffUserId { get; set; }
    [BindProperty] public string Category { get; set; } = "Personal Documents";
    [BindProperty] public string? Notes { get; set; }

    [BindProperty]
    public List<IFormFile> StaffFiles { get; set; } = new();

    public string? StatusMessage { get; private set; }
    public List<SelectListItem> StaffOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadStaffOptionsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadStaffOptionsAsync(currentUser.CompanyId);

        if (!StaffUserId.HasValue || !await StaffExistsAsync(currentUser.CompanyId, StaffUserId.Value))
        {
            StatusMessage = "Select the staff member these files belong to.";
            return Page();
        }

        if (StaffFiles.Count == 0)
        {
            StatusMessage = "Select one or more staff files before continuing.";
            return Page();
        }

        var unsupportedFile = StaffFiles.FirstOrDefault(file => !AllowedExtensions.Contains(Path.GetExtension(file.FileName)));
        if (unsupportedFile is not null)
        {
            StatusMessage = $"Unsupported file type: {unsupportedFile.FileName}.";
            return Page();
        }

        var now = DateTime.UtcNow;
        var category = NormalizeCategory(Category);
        var savedCount = 0;

        foreach (var file in StaffFiles.Where(file => file.Length > 0))
        {
            var storedFile = await _fileStorage.SaveAsync(file, $"staff-{StaffUserId.Value}");
            _db.AssetFiles.Add(new AssetFile
            {
                CompanyId = currentUser.CompanyId,
                UploadedByUserId = currentUser.Id,
                LinkedEntityType = "Staff",
                LinkedEntityId = StaffUserId.Value,
                Category = category,
                OriginalFileName = storedFile.OriginalFileName,
                ContentType = storedFile.ContentType,
                StorageProvider = storedFile.ProviderName,
                StoragePath = storedFile.StoragePath,
                SizeBytes = storedFile.SizeBytes,
                Notes = NormalizeOptional(Notes),
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
            Action = "Staff files uploaded",
            EntityType = "AppUser",
            EntityId = StaffUserId.Value,
            Details = $"{savedCount} staff file(s) uploaded into {category}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        return RedirectToPage("/StaffFiles", new { staffUserId = StaffUserId.Value, uploaded = savedCount });
    }

    private async Task LoadStaffOptionsAsync(int companyId)
    {
        StaffOptions = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Where(user => user.CompanyId == companyId && user.Status != "Deleted")
            .OrderBy(user => user.FullName)
            .Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = user.FullName + " - " + (user.AppRole == null ? "Unassigned" : user.AppRole.Name)
            })
            .ToListAsync();
    }

    private async Task<bool> StaffExistsAsync(int companyId, int staffUserId)
    {
        return await _db.AppUsers.AnyAsync(user =>
            user.CompanyId == companyId &&
            user.Id == staffUserId &&
            user.Status != "Deleted");
    }

    private static string NormalizeCategory(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Personal Documents" : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
