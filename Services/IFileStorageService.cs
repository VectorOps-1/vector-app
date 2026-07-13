namespace vector_app_local.Services;

public interface IFileStorageService
{
    string ProviderName { get; }

    Task<StoredFileResult> SaveAsync(
        IFormFile file,
        int companyId,
        string category,
        FileStorageValidationOptions? validationOptions = null,
        CancellationToken cancellationToken = default);

    Task ValidateAsync(
        IFormFile file,
        FileStorageValidationOptions? validationOptions = null,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}

public sealed record FileStorageValidationOptions(
    string Purpose,
    long MaxSizeBytes,
    IReadOnlyCollection<string> AllowedExtensions)
{
    public static readonly FileStorageValidationOptions SetupImport = new(
        "setup import",
        20 * 1024 * 1024,
        [".xlsx", ".xls", ".csv"]);

    public static readonly FileStorageValidationOptions StaffDocument = new(
        "staff document",
        10 * 1024 * 1024,
        [".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff", ".doc", ".docx", ".rtf", ".txt", ".xls", ".xlsx", ".csv"]);

    public static readonly FileStorageValidationOptions TaskEvidence = new(
        "task evidence",
        10 * 1024 * 1024,
        [".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"]);

    public static readonly FileStorageValidationOptions Default = StaffDocument;

    public bool AllowsExtension(string extension)
    {
        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record StoredFileResult(
    string ProviderName,
    string StoragePath,
    string OriginalFileName,
    string ContentType,
    long SizeBytes);

public sealed class FileStorageValidationException : InvalidOperationException
{
    public FileStorageValidationException(string message)
        : base(message)
    {
    }
}
