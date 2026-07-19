namespace vector_app_local.Models;

public static class ComplianceLifecycleStates
{
    public const string Draft = "Draft";
    public const string Acquired = "Acquired";
    public const string ClauseExtracted = "ClauseExtracted";
    public const string SourceVerified = "SourceVerified";
    public const string LegalReviewPending = "LegalReviewPending";
    public const string Approved = "Approved";
    public const string Active = "Active";
    public const string Superseded = "Superseded";
    public const string Blocked = "Blocked";
    public const string Withdrawn = "Withdrawn";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft,
        Acquired,
        ClauseExtracted,
        SourceVerified,
        LegalReviewPending,
        Approved,
        Active,
        Superseded,
        Blocked,
        Withdrawn
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedTransitions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [Draft] = Set(Acquired, Blocked, Withdrawn),
            [Acquired] = Set(ClauseExtracted, Blocked, Withdrawn),
            [ClauseExtracted] = Set(SourceVerified, Blocked, Withdrawn),
            [SourceVerified] = Set(LegalReviewPending, Blocked, Withdrawn),
            [LegalReviewPending] = Set(Approved, Blocked, Withdrawn),
            [Approved] = Set(Active, Blocked, Withdrawn),
            [Active] = Set(Superseded, Withdrawn),
            [Superseded] = Set(),
            [Blocked] = Set(),
            [Withdrawn] = Set()
        };

    public static bool CanTransition(string current, string target)
    {
        return AllowedTransitions.TryGetValue(current, out var targets) && targets.Contains(target);
    }

    public static bool IsImmutable(string state) => state is Active or Superseded or Withdrawn;

    private static IReadOnlySet<string> Set(params string[] values) =>
        values.ToHashSet(StringComparer.Ordinal);
}

public static class ComplianceJurisdictionLevels
{
    public const string Country = "Country";
    public const string Province = "Province";
    public const string District = "District";
    public const string Municipality = "Municipality";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Country,
        Province,
        District,
        Municipality
    };
}

public static class ComplianceSourceClassifications
{
    public const string BindingLaw = "BindingLaw";
    public const string BindingRegulation = "BindingRegulation";
    public const string LicenceCondition = "LicenceCondition";
    public const string OfficialInspectionItem = "OfficialInspectionItem";
    public const string RegulatoryStandard = "RegulatoryStandard";
    public const string OfficialDirective = "OfficialDirective";
    public const string OfficialGuidance = "OfficialGuidance";
    public const string InternalPolicy = "InternalPolicy";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        BindingLaw,
        BindingRegulation,
        LicenceCondition,
        OfficialInspectionItem,
        RegulatoryStandard,
        OfficialDirective,
        OfficialGuidance,
        InternalPolicy
    };
}

public static class ComplianceSourceCompletenessStates
{
    public const string Incomplete = "Incomplete";
    public const string Complete = "Complete";
    public const string Conflicted = "Conflicted";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Incomplete,
        Complete,
        Conflicted
    };
}

public static class ComplianceReviewTypes
{
    public const string SourceVerification = "SourceVerification";
    public const string Legal = "Legal";
    public const string Operational = "Operational";
    public const string Approval = "Approval";
}

public static class ComplianceReviewDecisions
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string ChangesRequired = "ChangesRequired";
}

public static class CompliancePriorities
{
    public const string P0 = "P0";
    public const string P1 = "P1";
    public const string P2 = "P2";
    public const string P3 = "P3";
    public const string P4 = "P4";
}

public static class ComplianceGovernanceEntityTypes
{
    public const string SourceVersion = "RegulatorySourceVersion";
    public const string PackVersion = "ComplianceRequirementPackVersion";
}
