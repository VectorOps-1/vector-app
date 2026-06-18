using vector_app_local.Models;

namespace vector_app_local.Services;

public static class CompanyBranding
{
    private const string DefaultLogoPath = "/acuityops-app-icon-light.png";
    public const string DefaultCompanyName = "Company name not set";

    public static string GetLogoPath(IWebHostEnvironment environment)
    {
        return GetLogoPath(environment, null);
    }

    public static string GetLogoPath(IWebHostEnvironment environment, Company? company)
    {
        if (company is not null)
        {
            var uploadedCompanyLogoPath = GetUploadedCompanyLogoPath(environment, company.Id);
            if (uploadedCompanyLogoPath is not null)
            {
                return uploadedCompanyLogoPath;
            }
        }

        if (!string.IsNullOrWhiteSpace(company?.LogoStoragePath) &&
            !IsSeedXMedLogo(company.LogoStoragePath))
        {
            return WithVersion(environment, company.LogoStoragePath) ?? company.LogoStoragePath;
        }

        if (company is not null)
        {
            return DefaultLogoPath;
        }

        return GetLegacyLogoPath(environment);
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
            string.Equals(normalized, "Client Business Name", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetUploadedCompanyLogoPath(IWebHostEnvironment environment, int companyId)
    {
        var uploadFolder = Path.Combine(environment.WebRootPath, "uploads", "company", companyId.ToString());
        if (!Directory.Exists(uploadFolder))
        {
            return null;
        }

        var logoFile = Directory.GetFiles(uploadFolder, "company-logo.*")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (logoFile is null)
        {
            return null;
        }

        return $"/uploads/company/{companyId}/{Path.GetFileName(logoFile)}?v={File.GetLastWriteTimeUtc(logoFile).Ticks}";
    }

    private static bool IsSeedXMedLogo(string? logoStoragePath)
    {
        return string.Equals(logoStoragePath?.Split('?', 2)[0], "/x-med-logo.svg", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLegacyLogoPath(IWebHostEnvironment environment)
    {
        var uploadFolder = Path.Combine(environment.WebRootPath, "uploads", "company");
        if (!Directory.Exists(uploadFolder))
        {
            return DefaultLogoPath;
        }

        var logoFile = Directory.GetFiles(uploadFolder, "company-logo.*").FirstOrDefault();
        if (logoFile is null)
        {
            return DefaultLogoPath;
        }

        return $"/uploads/company/{Path.GetFileName(logoFile)}?v={File.GetLastWriteTimeUtc(logoFile).Ticks}";
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

    public static string GetCompanyName(IWebHostEnvironment environment)
    {
        return DefaultCompanyName;
    }

    public static void SaveCompanyName(IWebHostEnvironment environment, string companyName)
    {
        var profileFolder = Path.Combine(environment.WebRootPath, "uploads", "company");
        Directory.CreateDirectory(profileFolder);

        var namePath = Path.Combine(profileFolder, "company-name.txt");
        File.WriteAllText(namePath, companyName.Trim());
    }
}
