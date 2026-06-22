namespace vector_app_local.Services;

public interface IFileSecurityScanner
{
    Task<FileSecurityScanResult> ScanAsync(
        IFormFile file,
        FileStorageValidationOptions validationOptions,
        CancellationToken cancellationToken = default);
}

public sealed record FileSecurityScanResult(bool IsClean, string? Message)
{
    public static FileSecurityScanResult Clean() => new(true, null);
    public static FileSecurityScanResult Blocked(string message) => new(false, message);
}

public sealed class NoOpFileSecurityScanner : IFileSecurityScanner
{
    public Task<FileSecurityScanResult> ScanAsync(
        IFormFile file,
        FileStorageValidationOptions validationOptions,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FileSecurityScanResult.Clean());
    }
}
