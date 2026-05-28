namespace vector_app_local.Services;

public static class CompanyBranding
{
    public static string GetLogoPath(IWebHostEnvironment environment)
    {
        var uploadFolder = Path.Combine(environment.WebRootPath, "uploads", "company");
        if (!Directory.Exists(uploadFolder))
        {
            return "/acuityops-splash.svg";
        }

        var logoFile = Directory.GetFiles(uploadFolder, "company-logo.*").FirstOrDefault();
        if (logoFile is null)
        {
            return "/acuityops-splash.svg";
        }

        return $"/uploads/company/{Path.GetFileName(logoFile)}?v={File.GetLastWriteTimeUtc(logoFile).Ticks}";
    }

    public static string GetCompanyName(IWebHostEnvironment environment)
    {
        var profileFolder = Path.Combine(environment.WebRootPath, "uploads", "company");
        var namePath = Path.Combine(profileFolder, "company-name.txt");

        if (!File.Exists(namePath))
        {
            return "Client Business Name";
        }

        var companyName = File.ReadAllText(namePath).Trim();
        return string.IsNullOrWhiteSpace(companyName) ? "Client Business Name" : companyName;
    }

    public static void SaveCompanyName(IWebHostEnvironment environment, string companyName)
    {
        var profileFolder = Path.Combine(environment.WebRootPath, "uploads", "company");
        Directory.CreateDirectory(profileFolder);

        var namePath = Path.Combine(profileFolder, "company-name.txt");
        File.WriteAllText(namePath, companyName.Trim());
    }
}
