using vector_app_local.Models;

namespace vector_app_local.Services;

public static class SetupWizardProgress
{
    public const string CompanyIdentityStepKey = "company-identity";
    public const string OperationalStructureStepKey = "operational-structure";
    public const string VehicleStructureStepKey = "vehicle-structure";
    public const string StaffStructureStepKey = "staff-structure";
    public const string AccessModelStepKey = "access-model";
    public const string AssetRegisterStepKey = "asset-registers";
    public const string ChecklistSetupStepKey = "checklist-setup";
    public const string ReadinessEngineSetupStepKey = "readiness-engine-setup";
    public const string ReviewStepKey = "setup-review";

    private static readonly IReadOnlyList<SetupWizardStepDefinition> StepDefinitions =
    [
        new(CompanyIdentityStepKey, 1, "Company identity", "Confirm company name, trading name, contact details, country, region, timezone, and logo before normal app use starts."),
        new(OperationalStructureStepKey, 2, "Operational structure", "Capture bases, regions, operational areas, storage spaces, and whether areas sit flat or under regions or bases."),
        new(VehicleStructureStepKey, 3, "Vehicle structure", "Capture vehicle functions, client-defined subtypes, and optional default unit schematic assignments by function or subtype."),
        new(StaffStructureStepKey, 4, "Staff structure", "Capture clinical qualification and scope options, staff ID format, practitioner-number requirements, licensing expiry, CPD tracking, and default profile fields."),
        new(AccessModelStepKey, 5, "Access model", "Capture default permissions for company owner, senior management, operational management, and staff before real users begin operating."),
        new(AssetRegisterStepKey, 6, "Asset registers", "Choose whether vehicles, equipment, stock, medication, staff, and storage locations will be built manually now or imported later."),
        new(ChecklistSetupStepKey, 7, "Checklist setup", "Choose whether daily checks start from blank, use an explicit starter structure, import existing checklists later, publish by function, subtype, or callsign, and configure Full Audit now or later."),
        new(ReadinessEngineSetupStepKey, 8, "Readiness engine setup", "Choose whether readiness scoring will use the default AcuityOps scoring model, be customized later, require senior approval for scoring changes, and activate or stay deferred."),
        new(ReviewStepKey, 9, "Setup review", "Review missing required setup items, optional deferred work, and what the company can do immediately after setup.")
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
