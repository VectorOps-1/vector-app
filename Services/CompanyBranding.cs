using vector_app_local.Models;

namespace vector_app_local.Services;

public static class CompanyBranding
{
    public const string DefaultLogoPath = "/acuityops-app-icon-light.png";
    public const string CompanyLogoFileName = "company-logo.png";
    public const string DefaultCompanyName = "Company workspace";
    public const string BrandingStatusConfigured = "Configured";
    public const string BrandingStatusIncomplete = "Incomplete";

    public static string GetLogoPath(IWebHostEnvironment environment)
    {
        return GetLogoPath(environment, null);
    }

    public static string GetLogoPath(IWebHostEnvironment environment, Company? company)
    {
        if (company is null || company.LogoRemoved)
        {
            return DefaultLogoPath;
        }

        var storedLogoPath = company.LogoStoragePath;
        if (IsCompanyScopedLogoPath(company, storedLogoPath))
        {
            return WithVersion(environment, storedLogoPath!) ?? DefaultLogoPath;
        }

        return DefaultLogoPath;
    }

    public static string GetStoredLogoPath(int companyId)
    {
        // Company logos are public branding assets, not confidential tenant documents.
        // Protected client uploads use IFileStorageService tenant-scoped storage paths.
        return $"/uploads/company/{companyId}/{CompanyLogoFileName}";
    }

    public static string GetLogoUploadFolder(IWebHostEnvironment environment, int companyId)
    {
        return Path.Combine(environment.WebRootPath, "uploads", "company", companyId.ToString());
    }

    public static bool IsDefaultLogoPath(string? logoPath)
    {
        return string.IsNullOrWhiteSpace(logoPath)
            || logoPath.StartsWith(DefaultLogoPath, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetDisplayCompanyName(Company? company)
    {
        var companyName = GetSavedCompanyName(company);
        return companyName ?? DefaultCompanyName;
    }

    public static string? GetSavedCompanyName(Company? company)
    {
        var companyName = company?.Name?.Trim();
        return IsPlaceholderCompanyName(companyName) ? null : companyName;
    }

    public static bool IsPlaceholderCompanyName(string? companyName)
    {
        return string.IsNullOrWhiteSpace(companyName);
    }

    public static string GetBrandingStatus(Company company)
    {
        return !string.IsNullOrWhiteSpace(GetSavedCompanyName(company))
            ? BrandingStatusConfigured
            : BrandingStatusIncomplete;
    }

    private static bool IsCompanyScopedLogoPath(Company company, string? logoStoragePath)
    {
        if (string.IsNullOrWhiteSpace(logoStoragePath))
        {
            return false;
        }

        var cleanPath = logoStoragePath.Split('?', 2)[0];
        var expectedPath = GetStoredLogoPath(company.Id);

        return string.Equals(cleanPath, expectedPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string? WithVersion(IWebHostEnvironment environment, string logoStoragePath)
    {
        if (!logoStoragePath.StartsWith("/", StringComparison.Ordinal))
        {
            return null;
        }

        var cleanPath = logoStoragePath.Split('?', 2)[0].TrimStart('/');
        var filePath = Path.Combine(environment.WebRootPath, cleanPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(filePath))
        {
            return null;
        }

        return $"{logoStoragePath.Split('?', 2)[0]}?v={File.GetLastWriteTimeUtc(filePath).Ticks}";
    }
}
