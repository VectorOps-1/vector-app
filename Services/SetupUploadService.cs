using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class SetupUploadService
{
    public const string RegisterUploadEntityType = "RegisterUpload";
    public const string ChecklistUploadEntityType = "ChecklistUpload";

    private static readonly HashSet<string> AllowedSpreadsheetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xlsx", ".xls", ".csv"
    };

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public SetupUploadService(VectorDbContext db, CurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<SetupUploadResult> SaveRegisterUploadAsync(IFormFile? file, string registerCategory)
    {
        return await SaveSetupUploadAsync(file, RegisterUploadEntityType, registerCategory);
    }

    public async Task<SetupUploadResult> SaveChecklistUploadAsync(IFormFile? file)
    {
        return await SaveSetupUploadAsync(file, ChecklistUploadEntityType, "Checklist");
    }

    public async Task<List<SetupUploadSummary>> GetRecentUploadsAsync(string linkedEntityType, int take = 12)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return new List<SetupUploadSummary>();
        }

        return await _db.AssetFiles
            .AsNoTracking()
            .Where(file => file.CompanyId == currentUser.CompanyId && file.LinkedEntityType == linkedEntityType)
            .OrderByDescending(file => file.UploadedAtUtc)
            .Take(take)
            .Select(file => new SetupUploadSummary(
                file.Id,
                file.Category,
                file.OriginalFileName,
                file.SizeBytes,
                file.UploadedAtUtc))
            .ToListAsync();
    }

    public async Task<string?> GetUploadedFileNameAsync(int sourceFileId, string linkedEntityType)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return null;
        }

        return await _db.AssetFiles
            .AsNoTracking()
            .Where(file =>
                file.CompanyId == currentUser.CompanyId &&
                file.Id == sourceFileId &&
                file.LinkedEntityType == linkedEntityType)
            .Select(file => file.OriginalFileName)
            .FirstOrDefaultAsync();
    }

    private async Task<SetupUploadResult> SaveSetupUploadAsync(IFormFile? file, string linkedEntityType, string category)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return SetupUploadResult.NotSignedIn();
        }

        if (file is null || file.Length <= 0)
        {
            return SetupUploadResult.Failed("Select an Excel or CSV file before continuing.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedSpreadsheetExtensions.Contains(extension))
        {
            return SetupUploadResult.Failed("Accepted file types are .xlsx, .xls, and .csv.");
        }

        var now = DateTime.UtcNow;
        var storedFile = await _fileStorage.SaveAsync(file, $"{linkedEntityType}-{category}");
        var assetFile = new AssetFile
        {
            CompanyId = currentUser.CompanyId,
            UploadedByUserId = currentUser.Id,
            LinkedEntityType = linkedEntityType,
            LinkedEntityId = 0,
            Category = category,
            OriginalFileName = storedFile.OriginalFileName,
            ContentType = storedFile.ContentType,
            StorageProvider = storedFile.ProviderName,
            StoragePath = storedFile.StoragePath,
            SizeBytes = storedFile.SizeBytes,
            UploadedAtUtc = now
        };

        _db.AssetFiles.Add(assetFile);
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = $"{category} source file uploaded",
            EntityType = linkedEntityType,
            EntityId = 0,
            Details = $"{storedFile.OriginalFileName} uploaded for setup review.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        return SetupUploadResult.Saved(assetFile.Id, storedFile.OriginalFileName);
    }
}

public sealed record SetupUploadSummary(
    int Id,
    string Category,
    string OriginalFileName,
    long SizeBytes,
    DateTime UploadedAtUtc)
{
    public string SizeLabel => SizeBytes switch
    {
        >= 1024 * 1024 => $"{SizeBytes / 1024d / 1024d:0.0} MB",
        >= 1024 => $"{SizeBytes / 1024d:0.0} KB",
        _ => $"{SizeBytes} B"
    };
}

public sealed record SetupUploadResult(bool IsSaved, bool IsNotSignedIn, int? FileId, string? FileName, string? ErrorMessage)
{
    public static SetupUploadResult Saved(int fileId, string fileName) => new(true, false, fileId, fileName, null);
    public static SetupUploadResult Failed(string errorMessage) => new(false, false, null, null, errorMessage);
    public static SetupUploadResult NotSignedIn() => new(false, true, null, null, null);
}
