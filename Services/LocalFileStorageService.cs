namespace vector_app_local.Services;

public class LocalFileStorageService : IFileStorageService
{
    public const string Provider = "local";

    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string ProviderName => Provider;

    public async Task<StoredFileResult> SaveAsync(IFormFile file, string category, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Cannot save an empty file.");
        }

        var now = DateTime.UtcNow;
        var safeCategory = SanitizePathSegment(category, "general");
        var originalFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName);
        var storagePath = string.Join('/',
            Provider,
            safeCategory,
            now.ToString("yyyy"),
            now.ToString("MM"),
            $"{Guid.NewGuid():N}{extension}");

        var fullPath = ResolveStoragePath(storagePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        return new StoredFileResult(
            ProviderName,
            storagePath,
            originalFileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            file.Length);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveStoragePath(storagePath);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveStoragePath(storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string StorageRoot => Path.Combine(_environment.ContentRootPath, "App_Data", "uploads");

    private string ResolveStoragePath(string storagePath)
    {
        var normalizedPath = storagePath.Replace('\\', '/');
        if (normalizedPath.StartsWith($"{Provider}/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath[(Provider.Length + 1)..];
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var fullRoot = Path.GetFullPath(StorageRoot);
        var fullPath = Path.GetFullPath(Path.Combine(new[] { fullRoot }.Concat(segments).ToArray()));

        if (!fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage path resolves outside the configured storage root.");
        }

        return fullPath;
    }

    private static string SanitizePathSegment(string? value, string fallback)
    {
        var rawValue = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var safeChars = rawValue
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            .ToArray();

        return safeChars.Length == 0 ? fallback : new string(safeChars).ToLowerInvariant();
    }
}
