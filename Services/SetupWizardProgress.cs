using vector_app_local.Models;

namespace vector_app_local.Services;

public static class SetupWizardProgress
{
    public const string CompanyIdentityStepKey = "company-identity";
    public const string OperationalStructureStepKey = "operational-structure";
    public const string VehicleStructureStepKey = "vehicle-structure";
    public const string StaffStructureStepKey = "staff-structure";

    private static readonly IReadOnlyList<SetupWizardStepDefinition> StepDefinitions =
    [
        new(CompanyIdentityStepKey, 1, "Company identity", "Confirm company name, trading name, contact details, country, region, timezone, and logo before normal app use starts."),
        new(OperationalStructureStepKey, 2, "Operational structure", "Capture bases, regions, operational areas, storage spaces, and whether areas sit flat or under regions or bases."),
        new(VehicleStructureStepKey, 3, "Vehicle structure", "Capture vehicle functions, client-defined subtypes, and optional default unit schematic assignments by function or subtype."),
        new(StaffStructureStepKey, 4, "Staff structure", "Capture clinical qualification and scope options, staff ID format, practitioner-number requirements, licensing expiry, CPD tracking, and default profile fields.")
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
        var currentStep = GetFirstIncompleteStep(company) ?? StepDefinitions[^1];
        if (string.Equals(company.SetupWizardCurrentStepKey, currentStep.Key, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        company.SetupWizardCurrentStepKey = currentStep.Key;
        company.SetupWizardUpdatedAtUtc = DateTime.UtcNow;
        return true;
    }

    public static bool MarkStepComplete(Company company, string stepKey)
    {
        var cleanedStepKey = CleanStepKey(stepKey);
        var completed = ParseCompletedSteps(company.SetupWizardCompletedStepKeys).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var changed = completed.Add(cleanedStepKey);
        var nextStep = StepDefinitions.FirstOrDefault(step => !completed.Contains(step.Key)) ?? StepDefinitions[^1];

        if (!string.Equals(company.SetupWizardCurrentStepKey, nextStep.Key, StringComparison.OrdinalIgnoreCase))
        {
            company.SetupWizardCurrentStepKey = nextStep.Key;
            changed = true;
        }

        if (changed)
        {
            company.SetupWizardCompletedStepKeys = string.Join(
                ",",
                StepDefinitions
                    .Where(step => completed.Contains(step.Key))
                    .Select(step => step.Key));
            company.SetupWizardUpdatedAtUtc = DateTime.UtcNow;
        }

        return changed;
    }

    public static bool AreAllStepsComplete(Company company)
    {
        var completed = ParseCompletedSteps(company.SetupWizardCompletedStepKeys);
        return StepDefinitions.All(step => completed.Contains(step.Key));
    }

    private static SetupWizardStepDefinition? GetFirstIncompleteStep(Company company)
    {
        var completed = ParseCompletedSteps(company.SetupWizardCompletedStepKeys);
        return StepDefinitions.FirstOrDefault(step => !completed.Contains(step.Key));
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
