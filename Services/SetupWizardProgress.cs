using vector_app_local.Models;

namespace vector_app_local.Services;

public static class SetupWizardProgress
{
    public const string CompanyIdentityStepKey = "company-identity";

    private static readonly IReadOnlyList<SetupWizardStepDefinition> StepDefinitions =
    [
        new(CompanyIdentityStepKey, 1, "Company identity", "Confirm company identity and logo before normal app use starts.")
    ];

    public static IReadOnlyList<SetupWizardStepDefinition> Steps => StepDefinitions;

    public static SetupWizardStepDefinition GetCurrentStep(Company company)
    {
        var stepKey = CleanStepKey(company.SetupWizardCurrentStepKey);
        return StepDefinitions.FirstOrDefault(step => step.Key.Equals(stepKey, StringComparison.OrdinalIgnoreCase))
            ?? StepDefinitions[0];
    }

    public static IReadOnlySet<string> GetCompletedStepKeys(Company company)
    {
        return ParseCompletedSteps(company.SetupWizardCompletedStepKeys);
    }

    public static bool EnsureCurrentStep(Company company)
    {
        var currentStep = GetCurrentStep(company);
        if (string.Equals(company.SetupWizardCurrentStepKey, currentStep.Key, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        company.SetupWizardCurrentStepKey = currentStep.Key;
        company.SetupWizardUpdatedAtUtc = DateTime.UtcNow;
        return true;
    }

    private static string CleanStepKey(string? stepKey)
    {
        if (string.IsNullOrWhiteSpace(stepKey))
        {
            return CompanyIdentityStepKey;
        }

        var cleaned = stepKey.Trim();
        return StepDefinitions.Any(step => step.Key.Equals(cleaned, StringComparison.OrdinalIgnoreCase))
            ? cleaned
            : CompanyIdentityStepKey;
    }

    private static IReadOnlySet<string> ParseCompletedSteps(string? completedStepKeys)
    {
        if (string.IsNullOrWhiteSpace(completedStepKeys))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return completedStepKeys
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(key => StepDefinitions.Any(step => step.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record SetupWizardStepDefinition(string Key, int Number, string Title, string Description);
