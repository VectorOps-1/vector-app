namespace vector_app_local.Services;

public interface IFileStorageService
{
    string ProviderName { get; }

    Task<StoredFileResult> SaveAsync(IFormFile file, string category, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}

public sealed record StoredFileResult(
    string ProviderName,
    string StoragePath,
    string OriginalFileName,
    string ContentType,
    long SizeBytes);
