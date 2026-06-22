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
    private readonly CustomDropdownOptionService _customDropdownOptions;

    public UploadStaffFilesModel(
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

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff",
        ".doc", ".docx", ".rtf", ".txt",
        ".xls", ".xlsx", ".csv"
    };

    [BindProperty(SupportsGet = true)] public int? StaffUserId { get; set; }
    [BindProperty] public string Category { get; set; } = "Personal Documents";
    [BindProperty] public string? CategoryOther { get; set; }
    [BindProperty] public string? Notes { get; set; }

    [BindProperty]
    public List<IFormFile> StaffFiles { get; set; } = new();

    public string? StatusMessage { get; private set; }
    public List<SelectListItem> StaffOptions { get; private set; } = new();
    public List<SelectListItem> CategoryOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadStaffOptionsAsync(currentUser);
        await LoadCategoryOptionsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadStaffOptionsAsync(currentUser);
        await LoadCategoryOptionsAsync(currentUser.CompanyId);

        if (!StaffUserId.HasValue || !await StaffExistsAsync(currentUser, StaffUserId.Value))
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

        foreach (var file in StaffFiles.Where(file => file.Length > 0))
        {
            try
            {
                await _fileStorage.ValidateAsync(file, FileStorageValidationOptions.StaffDocument);
            }
            catch (FileStorageValidationException ex)
            {
                StatusMessage = ex.Message;
                return Page();
            }
        }

        var now = DateTime.UtcNow;
        var category = await _customDropdownOptions.ResolveSelectionAsync(
            currentUser.CompanyId,
            currentUser.Id,
            CustomDropdownOptionService.StaffFileCategoryKey,
            Category,
            CategoryOther,
            "Personal Documents");
        if (category is null)
        {
            StatusMessage = "Name the Other folder / category before saving staff files.";
            return Page();
        }

        var savedCount = 0;

        foreach (var file in StaffFiles.Where(file => file.Length > 0))
        {
            var storedFile = await _fileStorage.SaveAsync(
                file,
                currentUser.CompanyId,
                $"staff-{StaffUserId.Value}",
                FileStorageValidationOptions.StaffDocument);
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

    private async Task LoadStaffOptionsAsync(AppUser currentUser)
    {
        var query = await BuildScopedStaffQueryAsync(currentUser);

        StaffOptions = await query
            .OrderBy(user => user.FullName)
            .Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = user.FullName
                    + (string.IsNullOrWhiteSpace(user.StaffIdentifier) ? string.Empty : $" / {user.StaffIdentifier}")
                    + " - "
                    + (user.AppRole == null ? "Unassigned" : user.AppRole.Name)
            })
            .ToListAsync();
    }

    private async Task LoadCategoryOptionsAsync(int companyId)
    {
        CategoryOptions = await _customDropdownOptions.BuildOptionsAsync(
            companyId,
            CustomDropdownOptionService.StaffFileCategoryKey,
            CustomDropdownOptionService.StaffFileCategoryDefaults,
            Category);
    }

    private async Task<bool> StaffExistsAsync(AppUser currentUser, int staffUserId)
    {
        var query = await BuildScopedStaffQueryAsync(currentUser);
        return await query.AnyAsync(user => user.Id == staffUserId);
    }

    private async Task<IQueryable<AppUser>> BuildScopedStaffQueryAsync(AppUser currentUser)
    {
        var query = _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Include(user => user.AssignedOperationalArea)
            .Where(user => user.CompanyId == currentUser.CompanyId && user.Status != "Deleted");

        if (CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return query;
        }

        var assignedAreaIds = await LoadAssignedAreaIdsAsync(currentUser);

        return assignedAreaIds.Count == 0
            ? query.Where(user => user.Id == currentUser.Id)
            : query.Where(user =>
                user.Id == currentUser.Id ||
                (user.AssignedOperationalAreaId.HasValue && assignedAreaIds.Contains(user.AssignedOperationalAreaId.Value)));
    }

    private async Task<List<int>> LoadAssignedAreaIdsAsync(AppUser currentUser)
    {
        var assignedAreaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync();

        if (currentUser.AssignedOperationalAreaId.HasValue &&
            !assignedAreaIds.Contains(currentUser.AssignedOperationalAreaId.Value))
        {
            assignedAreaIds.Add(currentUser.AssignedOperationalAreaId.Value);
        }

        return assignedAreaIds;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
