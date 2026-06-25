using vector_app_local.Models;

namespace vector_app_local.Services;

public class ChecklistSetupService
{
    public const string DailyChoiceBuildBlank = "build-blank";
    public const string DailyChoiceStarterStructure = "starter-structure";
    public const string DailyChoiceImportLater = "import-later";
    public const string DailyChoiceDefer = "defer";

    public const string FullAuditChoiceConfigureNow = "configure-now";
    public const string FullAuditChoiceConfigureLater = "configure-later";
    public const string FullAuditChoiceDefer = "defer";

    public const string PublishScopeFunction = "function";
    public const string PublishScopeSubtype = "subtype";
    public const string PublishScopeCallsign = "callsign";

    private static readonly IReadOnlyList<ChecklistSetupChoiceOption> DailyChecklistChoices =
    [
        new(DailyChoiceBuildBlank, "Build from blank", "Open the manual checklist builder with no hidden template or seed rows."),
        new(DailyChoiceStarterStructure, "Use starter structure", "Record that the client wants an explicit starter structure. No starter template is created until a user chooses it."),
        new(DailyChoiceImportLater, "Import existing checklist later", "Keep live daily checks empty until a real checklist is imported, reviewed, saved, and published."),
        new(DailyChoiceDefer, "Defer for now", "Return to daily checklist setup later.")
    ];

    private static readonly IReadOnlyList<ChecklistSetupChoiceOption> FullAuditChoices =
    [
        new(FullAuditChoiceConfigureNow, "Configure now", "Open the checklist builder for a Full Audit checklist."),
        new(FullAuditChoiceConfigureLater, "Configure later", "Record that Full Audit will be configured after core daily readiness is working."),
        new(FullAuditChoiceDefer, "Defer for now", "Skip Full Audit setup for this setup pass.")
    ];

    public IReadOnlyList<ChecklistSetupChoiceOption> GetDailyChecklistChoices() => DailyChecklistChoices;

    public IReadOnlyList<ChecklistSetupChoiceOption> GetFullAuditChoices() => FullAuditChoices;

    public ChecklistSetupSnapshot GetSnapshot(Company? company)
    {
        var scopeKeys = ParsePublishScopeKeys(company?.DailyChecklistPublishScopeKeys);
        return new ChecklistSetupSnapshot(
            company?.ChecklistSetupConfigured == true,
            NormalizeDailyChoice(company?.DailyChecklistSetupChoice),
            scopeKeys.Contains(PublishScopeFunction),
            scopeKeys.Contains(PublishScopeSubtype),
            scopeKeys.Contains(PublishScopeCallsign),
            NormalizeFullAuditChoice(company?.FullAuditChecklistSetupChoice),
            NormalizeNotes(company?.ChecklistSetupNotes));
    }

    public IReadOnlyList<ChecklistSetupRouteRow> BuildRows(ChecklistSetupSnapshot snapshot)
    {
        return
        [
            BuildDailyChecklistRow(snapshot.DailyChecklistChoice),
            BuildPublishScopeRow(snapshot),
            BuildFullAuditRow(snapshot.FullAuditChoice)
        ];
    }

    public static string NormalizeDailyChoice(string? choice)
    {
        return choice?.Trim().ToLowerInvariant() switch
        {
            DailyChoiceBuildBlank => DailyChoiceBuildBlank,
            DailyChoiceStarterStructure => DailyChoiceStarterStructure,
            DailyChoiceImportLater => DailyChoiceImportLater,
            DailyChoiceDefer => DailyChoiceDefer,
            _ => DailyChoiceDefer
        };
    }

    public static string NormalizeFullAuditChoice(string? choice)
    {
        return choice?.Trim().ToLowerInvariant() switch
        {
            FullAuditChoiceConfigureNow => FullAuditChoiceConfigureNow,
            FullAuditChoiceConfigureLater => FullAuditChoiceConfigureLater,
            FullAuditChoiceDefer => FullAuditChoiceDefer,
            _ => FullAuditChoiceConfigureLater
        };
    }

    public static string SerializePublishScopeKeys(bool publishByFunction, bool publishBySubtype, bool publishByCallsign)
    {
        var keys = new List<string>();
        if (publishByFunction)
        {
            keys.Add(PublishScopeFunction);
        }

        if (publishBySubtype)
        {
            keys.Add(PublishScopeSubtype);
        }

        if (publishByCallsign)
        {
            keys.Add(PublishScopeCallsign);
        }

        return string.Join(",", keys);
    }

    public static string DescribeDailyChoice(string? choice)
    {
        var normalized = NormalizeDailyChoice(choice);
        return DailyChecklistChoices.First(option => option.Value == normalized).Label;
    }

    public static string DescribeFullAuditChoice(string? choice)
    {
        var normalized = NormalizeFullAuditChoice(choice);
        return FullAuditChoices.First(option => option.Value == normalized).Label;
    }

    public static string DescribePublishScopes(bool publishByFunction, bool publishBySubtype, bool publishByCallsign)
    {
        var labels = new List<string>();
        if (publishByFunction)
        {
            labels.Add("function");
        }

        if (publishBySubtype)
        {
            labels.Add("subtype");
        }

        if (publishByCallsign)
        {
            labels.Add("callsign");
        }

        return labels.Count == 0 ? "No publish targets selected" : string.Join(", ", labels);
    }

    public static string? NormalizeNotes(string? notes)
    {
        return string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    private static HashSet<string> ParsePublishScopeKeys(string? scopeKeys)
    {
        if (string.IsNullOrWhiteSpace(scopeKeys))
        {
            return new HashSet<string>([PublishScopeFunction, PublishScopeSubtype], StringComparer.OrdinalIgnoreCase);
        }

        return scopeKeys
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(key => key is PublishScopeFunction or PublishScopeSubtype or PublishScopeCallsign)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static ChecklistSetupRouteRow BuildDailyChecklistRow(string choice)
    {
        var normalized = NormalizeDailyChoice(choice);
        return normalized switch
        {
            DailyChoiceBuildBlank => new ChecklistSetupRouteRow(
                "Daily Vehicle & Equipment Check",
                DescribeDailyChoice(normalized),
                "Build the daily checklist manually from a blank matrix. This does not create rows until the user saves a real checklist.",
                "Open blank builder",
                "/EditVehicleChecklist?checklist=daily-vehicle&mode=build"),
            DailyChoiceStarterStructure => new ChecklistSetupRouteRow(
                "Daily Vehicle & Equipment Check",
                DescribeDailyChoice(normalized),
                "Starter intent is saved. A starter must still be explicitly selected by a user; no hidden starter template is created by setup.",
                "Open builder with starter intent",
                "/EditVehicleChecklist?checklist=daily-vehicle&mode=build&starter=vehicle-equipment"),
            DailyChoiceImportLater => new ChecklistSetupRouteRow(
                "Daily Vehicle & Equipment Check",
                DescribeDailyChoice(normalized),
                "The client will upload an existing checklist later. Live checks remain unavailable until an imported checklist is saved and published from the register.",
                "Upload existing checklist",
                "/UploadChecklist"),
            _ => new ChecklistSetupRouteRow(
                "Daily Vehicle & Equipment Check",
                DescribeDailyChoice(normalized),
                "Daily checklist setup is deferred. Live checks remain unavailable until a checklist is created and published from the register.",
                "No immediate action",
                null)
        };
    }

    private static ChecklistSetupRouteRow BuildPublishScopeRow(ChecklistSetupSnapshot snapshot)
    {
        return new ChecklistSetupRouteRow(
            "Daily checklist publish targets",
            DescribePublishScopes(snapshot.PublishByFunction, snapshot.PublishBySubtype, snapshot.PublishByCallsign),
            "These are the intended publish targets. Actual live use still requires a saved checklist template and an active Checklist Register publish scope.",
            "Open Checklist Register",
            "/EditChecklist?view=register");
    }

    private static ChecklistSetupRouteRow BuildFullAuditRow(string choice)
    {
        var normalized = NormalizeFullAuditChoice(choice);
        return normalized switch
        {
            FullAuditChoiceConfigureNow => new ChecklistSetupRouteRow(
                "Full Audit",
                DescribeFullAuditChoice(normalized),
                "Open the checklist builder for Full Audit. No audit template is created until a user saves it.",
                "Open Full Audit builder",
                "/EditVehicleChecklist?checklist=full-audit&mode=build"),
            FullAuditChoiceConfigureLater => new ChecklistSetupRouteRow(
                "Full Audit",
                DescribeFullAuditChoice(normalized),
                "Full Audit setup is recorded for later configuration after daily readiness is working.",
                "Open Checklist Register",
                "/EditChecklist?view=register"),
            _ => new ChecklistSetupRouteRow(
                "Full Audit",
                DescribeFullAuditChoice(normalized),
                "Full Audit setup is deferred and can be resumed from setup progress later.",
                "No immediate action",
                null)
        };
    }
}

public sealed record ChecklistSetupChoiceOption(string Value, string Label, string Description);

public sealed record ChecklistSetupSnapshot(
    bool IsConfigured,
    string DailyChecklistChoice,
    bool PublishByFunction,
    bool PublishBySubtype,
    bool PublishByCallsign,
    string FullAuditChoice,
    string? Notes);

public sealed record ChecklistSetupRouteRow(
    string Name,
    string ChoiceLabel,
    string Description,
    string ActionLabel,
    string? ActionUrl);
