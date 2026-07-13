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
    private readonly IImportSourceInspector _sourceInspector;
    private readonly ImportBatchService _importBatches;

    public SetupUploadService(
        VectorDbContext db,
        CurrentUserService currentUser,
        IFileStorageService fileStorage,
        IImportSourceInspector sourceInspector,
        ImportBatchService importBatches)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _sourceInspector = sourceInspector;
        _importBatches = importBatches;
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

        var access = await _importBatches.CanPrepareAsync(currentUser);
        if (!access.Allowed)
        {
            return SetupUploadResult.Failed(access.Message ?? "Guided importing is not available for this account.");
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

        StoredFileResult storedFile;
        ImportSourceProfile sourceProfile;
        try
        {
            await _fileStorage.ValidateAsync(file, FileStorageValidationOptions.SetupImport);
            sourceProfile = await _sourceInspector.InspectAsync(file);
            storedFile = await _fileStorage.SaveAsync(
                file,
                currentUser.CompanyId,
                $"{linkedEntityType}-{category}",
                FileStorageValidationOptions.SetupImport);
        }
        catch (FileStorageValidationException ex)
        {
            return SetupUploadResult.Failed(ex.Message);
        }
        catch (ImportSourceValidationException ex)
        {
            return SetupUploadResult.Failed(ex.Message);
        }

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;
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
            var targetType = ResolveTargetType(category);
            var importBatch = await _importBatches.CreateUploadedBatchAsync(currentUser, assetFile, targetType, sourceProfile, now);
            await _db.SaveChangesAsync();

            assetFile.LinkedEntityId = importBatch.Id;
            _db.AuditLogs.Add(new AuditLog
            {
                CompanyId = currentUser.CompanyId,
                AppUserId = currentUser.Id,
                Action = "Import source uploaded",
                EntityType = nameof(ImportBatch),
                EntityId = importBatch.Id,
                Details = $"{storedFile.OriginalFileName} stored for {targetType} import review. No operational records were changed.",
                CreatedAtUtc = now
            });
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return SetupUploadResult.Saved(assetFile.Id, importBatch.Id, storedFile.OriginalFileName);
        }
        catch
        {
            await _fileStorage.DeleteAsync(storedFile.StoragePath);
            return SetupUploadResult.Failed("The source file passed validation but the import batch could not be created. No operational records were changed.");
        }
    }

    private static string ResolveTargetType(string category)
    {
        return category.Trim() switch
        {
            "Vehicle Register" => ImportTargetTypes.Vehicle,
            "Staff Register" => ImportTargetTypes.Staff,
            "Equipment Register" => ImportTargetTypes.Equipment,
            "Stock Register" => ImportTargetTypes.Stock,
            "Medication Register" => ImportTargetTypes.Medication,
            "Operational Area Register" => ImportTargetTypes.OperationalArea,
            "Storage Location Register" => ImportTargetTypes.StorageLocation,
            "Checklist" => ImportTargetTypes.Checklist,
            _ => throw new InvalidOperationException("The selected import category is not supported.")
        };
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

public sealed record SetupUploadResult(bool IsSaved, bool IsNotSignedIn, int? FileId, int? ImportBatchId, string? FileName, string? ErrorMessage)
{
    public static SetupUploadResult Saved(int fileId, int importBatchId, string fileName) => new(true, false, fileId, importBatchId, fileName, null);
    public static SetupUploadResult Failed(string errorMessage) => new(false, false, null, null, null, errorMessage);
    public static SetupUploadResult NotSignedIn() => new(false, true, null, null, null, null);
}
