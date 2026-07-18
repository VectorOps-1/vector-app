using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public static class ImportBatchStatuses
{
    public const string Uploaded = "Uploaded";
    public const string SourceSelected = "SourceSelected";
    public const string Mapping = "Mapping";
    public const string Validated = "Validated";
    public const string CorrectionRequired = "CorrectionRequired";
    public const string ReadyToCommit = "ReadyToCommit";
    public const string Committed = "Committed";
    public const string PartiallyRolledBack = "PartiallyRolledBack";
    public const string RolledBack = "RolledBack";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

public static class ImportTargetTypes
{
    public const string Vehicle = "Vehicle";
    public const string Staff = "Staff";
    public const string Equipment = "Equipment";
    public const string Stock = "Stock";
    public const string Medication = "Medication";
    public const string OperationalArea = "OperationalArea";
    public const string StorageLocation = "StorageLocation";
    public const string Checklist = "Checklist";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Vehicle,
        Staff,
        Equipment,
        Stock,
        Medication,
        OperationalArea,
        StorageLocation,
        Checklist
    };
}

public class ImportBatch
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public int SourceAssetFileId { get; set; }
    public AssetFile? SourceAssetFile { get; set; }

    [MaxLength(80)]
    public string TargetType { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? ProposedRecordName { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = ImportBatchStatuses.Uploaded;

    [MaxLength(128)]
    public string FileHash { get; set; } = string.Empty;

    [MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? SelectedWorksheet { get; set; }

    public int? HeaderRowNumber { get; set; }

    [MaxLength(80)]
    public string? LayoutMode { get; set; }

    public int ParserContractVersion { get; set; } = 1;
    public string SourceProfileJson { get; set; } = "{}";
    public int WorksheetCount { get; set; }
    public int SourceRowCount { get; set; }
    public int IncludedRowCount { get; set; }
    public int ValidRowCount { get; set; }
    public int InvalidRowCount { get; set; }
    public int WarningRowCount { get; set; }

    public int CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public int? ValidatedByUserId { get; set; }
    public AppUser? ValidatedByUser { get; set; }
    public int? CommittedByUserId { get; set; }
    public AppUser? CommittedByUser { get; set; }
    public int? RolledBackByUserId { get; set; }
    public AppUser? RolledBackByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ValidatedAtUtc { get; set; }
    public DateTime? CommittedAtUtc { get; set; }
    public DateTime? RolledBackAtUtc { get; set; }

    [MaxLength(80)]
    public string? FailureCode { get; set; }

    [MaxLength(1200)]
    public string? FailureSummary { get; set; }

    [MaxLength(36)]
    [ConcurrencyCheck]
    public string ConcurrencyToken { get; set; } = Guid.NewGuid().ToString("D");

    public ICollection<ImportColumnMapping> ColumnMappings { get; set; } = new List<ImportColumnMapping>();
    public ICollection<ImportRowResult> RowResults { get; set; } = new List<ImportRowResult>();
    public ICollection<ImportEntityChange> EntityChanges { get; set; } = new List<ImportEntityChange>();
}

public class ImportColumnMapping
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public int ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public int SourceColumnIndex { get; set; }

    [MaxLength(260)]
    public string SourceHeading { get; set; } = string.Empty;

    [MaxLength(260)]
    public string NormalizedSourceHeading { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? TargetFieldKey { get; set; }

    [MaxLength(120)]
    public string? ConversionRule { get; set; }

    [MaxLength(1000)]
    public string? FixedValue { get; set; }

    [MaxLength(600)]
    public string? SuggestionReason { get; set; }

    public bool IsIgnored { get; set; }
    public bool IsUserConfirmed { get; set; }
    public int DisplayOrder { get; set; }
}

public class ImportRowResult
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public int ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public int SourceRowNumber { get; set; }
    public string OriginalPayloadJson { get; set; } = "{}";
    public string? CorrectedPayloadJson { get; set; }

    [MaxLength(40)]
    public string ValidationStatus { get; set; } = "Pending";

    public string? FieldErrorsJson { get; set; }
    public string? WarningsJson { get; set; }
    public string? DuplicateCandidatesJson { get; set; }

    [MaxLength(40)]
    public string? RowDecision { get; set; }

    public bool IsIncluded { get; set; } = true;
}

public class ImportEntityChange
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public int ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public int? ImportRowResultId { get; set; }
    public ImportRowResult? ImportRowResult { get; set; }

    [MaxLength(80)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [MaxLength(40)]
    public string Action { get; set; } = string.Empty;

    public string? BeforeValuesJson { get; set; }
    public string? AfterValuesJson { get; set; }

    [MaxLength(160)]
    public string? EntityStateToken { get; set; }

    public bool IsRollbackEligible { get; set; }

    [MaxLength(40)]
    public string? RollbackStatus { get; set; }

    [MaxLength(1200)]
    public string? RollbackReason { get; set; }

    public int? RolledBackByUserId { get; set; }
    public AppUser? RolledBackByUser { get; set; }
    public DateTime? RolledBackAtUtc { get; set; }
}

public class ImportMappingProfile
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string TargetType { get; set; } = string.Empty;

    [MaxLength(128)]
    public string HeadingSignature { get; set; } = string.Empty;

    public string MappingJson { get; set; } = "[]";
    public int CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
