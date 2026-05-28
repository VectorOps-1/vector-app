using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StaffFilesModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public StaffFilesModel(VectorDbContext db, CurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    [BindProperty(SupportsGet = true)] public int? StaffUserId { get; set; }
    [BindProperty(SupportsGet = true)] public int? Uploaded { get; set; }

    public List<StaffFileMember> StaffMembers { get; private set; } = new();
    public StaffFileMember? SelectedStaff { get; private set; }
    public List<StaffFileDocument> SelectedStaffFiles { get; private set; } = new();
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        StaffMembers = await _db.AppUsers
            .Include(user => user.AppRole)
            .Where(user => user.CompanyId == currentUser.CompanyId)
            .OrderBy(user => user.FullName)
            .Select(user => new StaffFileMember
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.AppRole == null ? "Unassigned" : user.AppRole.Name,
                Status = user.Status
            })
            .ToListAsync();

        SelectedStaff = StaffUserId.HasValue
            ? StaffMembers.FirstOrDefault(staff => staff.Id == StaffUserId.Value)
            : null;

        if (SelectedStaff is not null)
        {
            SelectedStaffFiles = await _db.AssetFiles
                .AsNoTracking()
                .Include(file => file.UploadedByUser)
                .Where(file =>
                    file.CompanyId == currentUser.CompanyId &&
                    file.LinkedEntityType == "Staff" &&
                    file.LinkedEntityId == SelectedStaff.Id)
                .OrderBy(file => file.Category)
                .ThenByDescending(file => file.UploadedAtUtc)
                .Select(file => new StaffFileDocument
                {
                    Id = file.Id,
                    Category = file.Category,
                    OriginalFileName = file.OriginalFileName,
                    ContentType = file.ContentType,
                    SizeBytes = file.SizeBytes,
                    Notes = file.Notes,
                    UploadedAtUtc = file.UploadedAtUtc,
                    UploadedByName = file.UploadedByUser == null ? "Unknown user" : file.UploadedByUser.FullName
                })
                .ToListAsync();
        }

        if (Uploaded.GetValueOrDefault() > 0)
        {
            StatusMessage = Uploaded == 1 ? "1 staff file saved." : $"{Uploaded} staff files saved.";
        }

        return Page();
    }

    public async Task<IActionResult> OnGetDownloadAsync(int fileId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var storedFile = await _db.AssetFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(file =>
                file.Id == fileId &&
                file.CompanyId == currentUser.CompanyId &&
                file.LinkedEntityType == "Staff");

        if (storedFile is null)
        {
            return RedirectToPage("/StaffFiles");
        }

        var stream = await _fileStorage.OpenReadAsync(storedFile.StoragePath);
        return File(stream, storedFile.ContentType, storedFile.OriginalFileName);
    }

    public class StaffFileMember
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class StaffFileDocument
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string? Notes { get; set; }
        public DateTime UploadedAtUtc { get; set; }
        public string UploadedByName { get; set; } = string.Empty;

        public string SizeLabel => SizeBytes < 1024
            ? $"{SizeBytes} B"
            : SizeBytes < 1024 * 1024
                ? $"{SizeBytes / 1024d:0.0} KB"
                : $"{SizeBytes / 1024d / 1024d:0.0} MB";
    }
}
