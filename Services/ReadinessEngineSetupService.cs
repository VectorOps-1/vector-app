using vector_app_local.Models;

namespace vector_app_local.Services;

public class ReadinessEngineSetupService
{
    public const string ChoiceUseDefault = "use-default";
    public const string ChoiceCustomizeLater = "customize-later";
    public const string ChoiceDefer = "defer";

    private static readonly IReadOnlyList<ReadinessEngineSetupChoiceOption> ScoringChoices =
    [
        new(ChoiceUseDefault, "Use default AcuityOps scoring", "Record that the company intends to use the default AcuityOps scoring model when readiness scoring is activated by a senior user."),
        new(ChoiceCustomizeLater, "Customize scoring later", "Record that a senior user will configure scoring rules before readiness scoring is activated."),
        new(ChoiceDefer, "Defer readiness scoring", "Keep readiness scoring inactive until a senior user completes readiness engine setup later.")
    ];

    public IReadOnlyList<ReadinessEngineSetupChoiceOption> GetScoringChoices() => ScoringChoices;

    public ReadinessEngineSetupSnapshot GetSnapshot(Company? company)
    {
        return new ReadinessEngineSetupSnapshot(
            company?.ReadinessEngineSetupConfigured == true,
            NormalizeScoringChoice(company?.ReadinessScoringSetupChoice),
            company?.ReadinessScoringActivated == true,
            company?.RequireSeniorApprovalForScoringChanges != false,
            NormalizeNotes(company?.ReadinessEngineSetupNotes));
    }

    public IReadOnlyList<ReadinessEngineSetupRouteRow> BuildRows(ReadinessEngineSetupSnapshot snapshot)
    {
        return
        [
            BuildScoringChoiceRow(snapshot.ScoringChoice, snapshot.ReadinessScoringActivated),
            BuildApprovalRow(snapshot.RequireSeniorApprovalForScoringChanges),
            BuildActivationRow(snapshot.ReadinessScoringActivated)
        ];
    }

    public static string NormalizeScoringChoice(string? choice)
    {
        return choice?.Trim().ToLowerInvariant() switch
        {
            ChoiceUseDefault => ChoiceUseDefault,
            ChoiceCustomizeLater => ChoiceCustomizeLater,
            ChoiceDefer => ChoiceDefer,
            _ => ChoiceDefer
        };
    }

    public static string DescribeScoringChoice(string? choice)
    {
        var normalized = NormalizeScoringChoice(choice);
        return ScoringChoices.First(option => option.Value == normalized).Label;
    }

    public static string DescribeActivation(bool isActivated)
    {
        return isActivated ? "Activate readiness scoring after setup" : "Keep readiness scoring deferred";
    }

    public static string DescribeApproval(bool requiresApproval)
    {
        return requiresApproval ? "Senior approval required" : "Senior approval not required";
    }

    public static string? NormalizeNotes(string? notes)
    {
        return string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    private static ReadinessEngineSetupRouteRow BuildScoringChoiceRow(string scoringChoice, bool isActivated)
    {
        var normalized = NormalizeScoringChoice(scoringChoice);
        return normalized switch
        {
            ChoiceUseDefault => new ReadinessEngineSetupRouteRow(
                "Scoring model",
                DescribeScoringChoice(normalized),
                isActivated
                    ? "Default scoring intent is saved. A senior user must still review and publish the readiness engine before live scoring depends on it."
                    : "Default scoring intent is saved, but readiness scoring remains deferred until a senior user activates and publishes the engine.",
                "Open Readiness Engine",
                "/ReadinessEngine"),
            ChoiceCustomizeLater => new ReadinessEngineSetupRouteRow(
                "Scoring model",
                DescribeScoringChoice(normalized),
                "Customization intent is saved. A senior user must configure and publish scoring rules before readiness scoring is treated as live.",
                "Open Readiness Engine",
                "/ReadinessEngine"),
            _ => new ReadinessEngineSetupRouteRow(
                "Scoring model",
                DescribeScoringChoice(normalized),
                "Readiness scoring is deferred. The dashboard can remain available, but scoring setup must be completed before it is treated as operational scoring.",
                "No immediate action",
                null)
        };
    }

    private static ReadinessEngineSetupRouteRow BuildApprovalRow(bool requiresApproval)
    {
        return new ReadinessEngineSetupRouteRow(
            "Scoring change approval",
            DescribeApproval(requiresApproval),
            requiresApproval
                ? "Operational manager scoring changes must be submitted for senior approval before they affect the engine."
                : "Senior approval is not required by default, but page permissions can still restrict who may edit or publish scoring.",
            "Review Readiness Engine",
            "/ReadinessEngine");
    }

    private static ReadinessEngineSetupRouteRow BuildActivationRow(bool isActivated)
    {
        return new ReadinessEngineSetupRouteRow(
            "Readiness scoring activation",
            DescribeActivation(isActivated),
            isActivated
                ? "Setup records intent to activate readiness scoring. Live impact still depends on explicitly published active rules."
                : "Readiness scoring is not activated by this setup step. No rules are created and no scoring assumptions are applied.",
            isActivated ? "Open Readiness Dashboard" : "Return later",
            isActivated ? "/ReadinessDashboard" : null);
    }
}

public sealed record ReadinessEngineSetupChoiceOption(string Value, string Label, string Description);

public sealed record ReadinessEngineSetupSnapshot(
    bool IsConfigured,
    string ScoringChoice,
    bool ReadinessScoringActivated,
    bool RequireSeniorApprovalForScoringChanges,
    string? Notes);

public sealed record ReadinessEngineSetupRouteRow(
    string Name,
    string ChoiceLabel,
    string Description,
    string ActionLabel,
    string? ActionUrl);
