using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public static class AiProcessingStatuses
{
    public const string Queued = "Queued";
    public const string Running = "Running";
    public const string AwaitingReview = "AwaitingReview";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Rejected = "Rejected";
}

public static class AiSuggestionStatuses
{
    public const string PendingReview = "PendingReview";
    public const string Reviewed = "Reviewed";
    public const string Accepted = "Accepted";
    public const string Corrected = "Corrected";
    public const string Rejected = "Rejected";
    public const string Deferred = "Deferred";
    public const string Applied = "Applied";
}

public static class AiHumanDecisions
{
    public const string Accept = "Accept";
    public const string Correct = "Correct";
    public const string Reject = "Reject";
    public const string Defer = "Defer";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Accept, Correct, Reject, Defer
    };
}

public class AiProcessingJob
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public int RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }
    public int ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    [MaxLength(80)] public string FeatureKey { get; set; } = string.Empty;
    [MaxLength(80)] public string SourceType { get; set; } = string.Empty;
    [MaxLength(128)] public string InputHash { get; set; } = string.Empty;
    [MaxLength(80)] public string Provider { get; set; } = string.Empty;
    [MaxLength(120)] public string Deployment { get; set; } = string.Empty;
    [MaxLength(120)] public string Model { get; set; } = string.Empty;
    [MaxLength(80)] public string PromptVersion { get; set; } = string.Empty;
    [MaxLength(80)] public string SchemaVersion { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = AiProcessingStatuses.Queued;
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    [MaxLength(80)] public string? FailureCode { get; set; }
    [MaxLength(1200)] public string? FailureSummary { get; set; }
    [MaxLength(80)] public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    [MaxLength(36)] [ConcurrencyCheck] public string ConcurrencyToken { get; set; } = Guid.NewGuid().ToString("D");
    public ICollection<AiJobAttempt> Attempts { get; set; } = new List<AiJobAttempt>();
    public ICollection<AiSuggestionSet> SuggestionSets { get; set; } = new List<AiSuggestionSet>();
}

public class AiJobAttempt
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int AiProcessingJobId { get; set; }
    public AiProcessingJob? AiProcessingJob { get; set; }
    public int AttemptNumber { get; set; }
    [MaxLength(120)] public string? ProviderRequestId { get; set; }
    [MaxLength(40)] public string Status { get; set; } = AiProcessingStatuses.Queued;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    [MaxLength(80)] public string? FailureCode { get; set; }
    [MaxLength(1200)] public string? FailureSummary { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public class AiSuggestionSet
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int AiProcessingJobId { get; set; }
    public AiProcessingJob? AiProcessingJob { get; set; }
    [MaxLength(80)] public string TargetType { get; set; } = string.Empty;
    [MaxLength(40)] public string Status { get; set; } = AiSuggestionStatuses.PendingReview;
    public decimal Confidence { get; set; }
    public string WarningsJson { get; set; } = "[]";
    public int? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<AiSuggestion> Suggestions { get; set; } = new List<AiSuggestion>();
}

public class AiSuggestion
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int AiSuggestionSetId { get; set; }
    public AiSuggestionSet? AiSuggestionSet { get; set; }
    [MaxLength(80)] public string Kind { get; set; } = string.Empty;
    [MaxLength(300)] public string SourceLocator { get; set; } = string.Empty;
    [MaxLength(160)] public string? TargetKey { get; set; }
    public string ProposedValueJson { get; set; } = "{}";
    public decimal Confidence { get; set; }
    [MaxLength(1200)] public string Explanation { get; set; } = string.Empty;
    public string WarningCodesJson { get; set; } = "[]";
    public int SortOrder { get; set; }
    [MaxLength(40)] public string Status { get; set; } = AiSuggestionStatuses.PendingReview;
    public ICollection<AiHumanDecision> HumanDecisions { get; set; } = new List<AiHumanDecision>();
}

public class AiHumanDecision
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int AiSuggestionId { get; set; }
    public AiSuggestion? AiSuggestion { get; set; }
    [MaxLength(40)] public string Decision { get; set; } = string.Empty;
    public string? CorrectedValueJson { get; set; }
    [MaxLength(1200)] public string? ReviewNote { get; set; }
    public int ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }
    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CompanyAiUsagePolicy
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public string EnabledFeaturesJson { get; set; } = "[]";
    public decimal MonthlySoftLimitUsd { get; set; }
    public decimal MonthlyHardLimitUsd { get; set; }
    public decimal PerJobLimitUsd { get; set; }
    public int MaxConcurrentJobs { get; set; } = 1;
    public bool AllowHighCapabilityModel { get; set; }
    public int ChangedByUserId { get; set; }
    public AppUser? ChangedByUser { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AiUsageLedger
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int AiProcessingJobId { get; set; }
    public AiProcessingJob? AiProcessingJob { get; set; }
    [MaxLength(80)] public string FeatureKey { get; set; } = string.Empty;
    [MaxLength(80)] public string Provider { get; set; } = string.Empty;
    [MaxLength(120)] public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AiImportProposal
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public int AiSuggestionSetId { get; set; }
    public AiSuggestionSet? AiSuggestionSet { get; set; }
    [MaxLength(80)] public string? ProposedDomain { get; set; }
    [MaxLength(80)] public string? ProposedChecklistLayout { get; set; }
    [MaxLength(40)] public string Status { get; set; } = AiSuggestionStatuses.PendingReview;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<AiImportColumnProposal> ColumnProposals { get; set; } = new List<AiImportColumnProposal>();
}

public class AiImportColumnProposal
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int AiImportProposalId { get; set; }
    public AiImportProposal? AiImportProposal { get; set; }
    [MaxLength(160)] public string SourceColumnKey { get; set; } = string.Empty;
    [MaxLength(160)] public string? CanonicalFieldKey { get; set; }
    [MaxLength(120)] public string? ProposedTransformationKey { get; set; }
    public decimal Confidence { get; set; }
    public string EvidenceJson { get; set; } = "[]";
    public string WarningCodesJson { get; set; } = "[]";
}

public class AiChecklistStructureProposal
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public int AiSuggestionSetId { get; set; }
    public AiSuggestionSet? AiSuggestionSet { get; set; }
    [MaxLength(160)] public string ProposedName { get; set; } = string.Empty;
    public string StructureJson { get; set; } = "{}";
    public string SourceCitationJson { get; set; } = "[]";
    [MaxLength(40)] public string Status { get; set; } = AiSuggestionStatuses.PendingReview;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
