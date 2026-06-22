namespace vector_app_local.Services;

public class LocalFileStorageService : IFileStorageService
{
    public const string Provider = "local";

    private readonly IWebHostEnvironment _environment;
    private readonly IFileSecurityScanner _fileSecurityScanner;

    public LocalFileStorageService(IWebHostEnvironment environment, IFileSecurityScanner fileSecurityScanner)
    {
        _environment = environment;
        _fileSecurityScanner = fileSecurityScanner;
    }

    public string ProviderName => Provider;

    public async Task<StoredFileResult> SaveAsync(
        IFormFile file,
        int companyId,
        string category,
        FileStorageValidationOptions? validationOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            throw new InvalidOperationException("A valid company id is required before saving tenant-owned files.");
        }

        await ValidateAsync(file, validationOptions, cancellationToken);

        var originalFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var now = DateTime.UtcNow;
        var tenantSegment = $"company-{companyId}";
        var safeCategory = SanitizePathSegment(category, "general");
        var storagePath = string.Join('/',
            Provider,
            tenantSegment,
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

    public async Task ValidateAsync(
        IFormFile file,
        FileStorageValidationOptions? validationOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            throw new FileStorageValidationException("Cannot save an empty file.");
        }

        var validation = validationOptions ?? FileStorageValidationOptions.Default;
        var originalFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

        if (!validation.AllowsExtension(extension))
        {
            throw new FileStorageValidationException($"The selected file type is not allowed for {validation.Purpose} uploads.");
        }

        if (file.Length > validation.MaxSizeBytes)
        {
            throw new FileStorageValidationException($"The selected file is too large for {validation.Purpose} uploads. Maximum size is {FormatSize(validation.MaxSizeBytes)}.");
        }

        await ValidateContentSignatureAsync(file, extension, cancellationToken);

        var scanResult = await _fileSecurityScanner.ScanAsync(file, validation, cancellationToken);
        if (!scanResult.IsClean)
        {
            throw new FileStorageValidationException(scanResult.Message ?? "The selected file did not pass the upload security scan.");
        }
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

    private static async Task ValidateContentSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        if (string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase))
        {
            await ValidateWebpSignatureAsync(file, cancellationToken);
            return;
        }

        var expectedSignatures = GetExpectedSignatures(extension);
        if (expectedSignatures.Count == 0)
        {
            return;
        }

        var bufferLength = expectedSignatures.Max(signature => signature.Length);
        var buffer = new byte[bufferLength];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

        var matchesSignature = expectedSignatures.Any(signature =>
            bytesRead >= signature.Length &&
            buffer.AsSpan(0, signature.Length).SequenceEqual(signature));

        if (!matchesSignature)
        {
            throw new FileStorageValidationException("The selected file content does not match its extension.");
        }
    }

    private static IReadOnlyList<byte[]> GetExpectedSignatures(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
            ".jpg" or ".jpeg" => [[0xFF, 0xD8, 0xFF]],
            ".pdf" => [[0x25, 0x50, 0x44, 0x46]],
            ".gif" => [[0x47, 0x49, 0x46, 0x38, 0x37, 0x61], [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]],
            ".bmp" => [[0x42, 0x4D]],
            ".tif" or ".tiff" => [[0x49, 0x49, 0x2A, 0x00], [0x4D, 0x4D, 0x00, 0x2A]],
            ".doc" or ".xls" => [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
            ".docx" or ".xlsx" => [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]],
            _ => []
        };
    }

    private static async Task ValidateWebpSignatureAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var buffer = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

        var hasWebpSignature = bytesRead >= buffer.Length &&
            buffer.AsSpan(0, 4).SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }) &&
            buffer.AsSpan(8, 4).SequenceEqual(new byte[] { 0x57, 0x45, 0x42, 0x50 });

        if (!hasWebpSignature)
        {
            throw new FileStorageValidationException("The selected file content does not match its extension.");
        }
    }

    private static string FormatSize(long bytes)
    {
        return bytes >= 1024 * 1024
            ? $"{bytes / 1024d / 1024d:0.#} MB"
            : $"{bytes / 1024d:0.#} KB";
    }
}
