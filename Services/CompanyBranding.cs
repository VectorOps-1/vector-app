namespace vector_app_local.Services;

public static class CompanyBranding
{
    public static string GetLogoPath(IWebHostEnvironment environment)
    {
        var uploadFolder = Path.Combine(environment.WebRootPath, "uploads", "company");
        if (!Directory.Exists(uploadFolder))
        {
            return "/vector-splash.png";
        }

        var logoFile = Directory.GetFiles(uploadFolder, "company-logo.*").FirstOrDefault();
        if (logoFile is null)
        {
            return "/vector-splash.png";
        }

        return $"/uploads/company/{Path.GetFileName(logoFile)}?v={File.GetLastWriteTimeUtc(logoFile).Ticks}";
    }
}
