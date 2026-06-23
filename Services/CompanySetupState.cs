using vector_app_local.Models;

namespace vector_app_local.Services;

public static class CompanySetupState
{
    public static bool IsSetupComplete(Company company)
    {
        return IsConfiguredStatus(company.BrandingStatus)
            && SetupWizardProgress.AreAllStepsComplete(company);
    }

    public static bool RequiresSetupWizard(Company company)
    {
        return !IsSetupComplete(company);
    }

    public static bool RequiresSetupWizard(string? setupStatus)
    {
        return !IsConfiguredStatus(setupStatus);
    }

    public static string DisplayStatus(Company company)
    {
        return IsSetupComplete(company) ? "Configured" : "Incomplete";
    }

    private static bool IsConfiguredStatus(string? setupStatus)
    {
        return string.Equals(
            setupStatus?.Trim(),
            CompanyBranding.BrandingStatusConfigured,
            StringComparison.OrdinalIgnoreCase);
    }
}
