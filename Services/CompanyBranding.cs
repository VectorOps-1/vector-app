using vector_app_local.Models;

namespace vector_app_local.Services;

public static class CompanyBranding
{
    private const string DefaultLogoPath = "/acuityops-app-icon-light.png";
    private const string CompanyLogoFileName = "company-logo.png";
    public const string DefaultCompanyName = "Not configured yet";

    public static string GetLogoPath(IWebHostEnvironment environment)
    {
        return GetLogoPath(environment, null);
    }

    public static string GetLogoPath(IWebHostEnvironment environment, Company? company)
    {
        if (company is null)
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
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return true;
        }

        var normalized = companyName.Trim();
        return string.Equals(normalized, "X Med", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "Random Med", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "Client Business Name", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCompanyScopedLogoPath(Company company, string? logoStoragePath)
    {
        if (string.IsNullOrWhiteSpace(logoStoragePath))
        {
            return false;
        }

        var cleanPath = logoStoragePath.Split('?', 2)[0];
        var expectedPath = $"/uploads/company/{company.Id}/{CompanyLogoFileName}";

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
