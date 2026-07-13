using System.Text.Json;
using System.Text.Json.Serialization;

namespace vector_app_local.Models;

public sealed class ChecklistEvidenceSnapshot
{
    public const int CurrentVersion = 1;

    public int ContractVersion { get; set; } = CurrentVersion;
    public bool IsLegacyRecord { get; set; }
    public string EvidenceStatus { get; set; } = "Immutable submission snapshot";
    public DateTime CapturedAtUtc { get; set; }
    public EvidenceTenantSnapshot Tenant { get; set; } = new();
    public EvidenceSubmissionMetadata Submission { get; set; } = new();
    public EvidenceSubmitterSnapshot Submitter { get; set; } = new();
    public EvidenceVehicleSnapshot Vehicle { get; set; } = new();
    public EvidenceTemplateSnapshot Template { get; set; } = new();
    public List<EvidenceSectionSnapshot> Sections { get; set; } = [];
    public List<EvidenceEquipmentSnapshot> Equipment { get; set; } = [];
    public List<EvidenceIssueReference> IssueReferences { get; set; } = [];
    public EvidenceSchematicSnapshot Schematic { get; set; } = new();
    public EvidenceNotesSnapshot Notes { get; set; } = new();
}

public sealed class EvidenceTenantSnapshot
{
    public int CompanyId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class EvidenceSubmissionMetadata
{
    public int ReportId { get; set; }
    public string WorkflowStatus { get; set; } = string.Empty;
    public string ReadinessStatus { get; set; } = string.Empty;
    public int CriticalIssueCount { get; set; }
    public int WarningIssueCount { get; set; }
    public DateTime InspectionDateUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? LastSavedAtUtc { get; set; }
    public string? ShiftName { get; set; }
    public DateTime? ShiftStartedAtUtc { get; set; }
    public DateTime? ShiftEndsAtUtc { get; set; }
    public bool VehicleSameAsPreviousShiftUsed { get; set; }
    public bool EquipmentSameAsPreviousShiftUsed { get; set; }
    public int? VehicleSameAsPreviousSourceReportId { get; set; }
    public int? EquipmentSameAsPreviousSourceReportId { get; set; }
}

public sealed class EvidenceSubmitterSnapshot
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? StaffIdentifier { get; set; }
    public string? QualificationFunction { get; set; }
    public string? PractitionerNumber { get; set; }
    public DateTime? AnnualLicenseExpiryDate { get; set; }
    public string? CpdComplianceStatus { get; set; }
    public DateTime? CpdComplianceExpiryDate { get; set; }
    public int? AssignedOperationalAreaId { get; set; }
    public string? AssignedOperationalAreaName { get; set; }
}

public sealed class EvidenceVehicleSnapshot
{
    public int VehicleId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Callsign { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string? VehicleFunction { get; set; }
    public string? VehicleSubtype { get; set; }
    public string? QualificationLevel { get; set; }
    public int? OperationalAreaId { get; set; }
    public string? OperationalAreaName { get; set; }
    public DateTime? NextServiceDate { get; set; }
}

public sealed class EvidenceTemplateSnapshot
{
    public int? TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ChecklistType { get; set; } = string.Empty;
    public string TargetVehicleType { get; set; } = string.Empty;
    public string? PublishScopeSummary { get; set; }
}

public sealed class EvidenceSectionSnapshot
{
    public int SectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<EvidenceItemSnapshot> Items { get; set; } = [];
}

public sealed class EvidenceItemSnapshot
{
    public int ItemId { get; set; }
    public int? ParentItemId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string ItemKind { get; set; } = string.Empty;
    public string ResponseType { get; set; } = string.Empty;
    public string? SchematicKey { get; set; }
    public List<EvidenceFieldSnapshot> Fields { get; set; } = [];
}

public sealed class EvidenceFieldSnapshot
{
    public string ResponseKey { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string ResponseType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsReadinessCritical { get; set; }
    public string? Value { get; set; }
}

public sealed class EvidenceEquipmentSnapshot
{
    public int? ChecklistItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EquipmentType { get; set; }
    public string? Model { get; set; }
    public string? SerialOrAssetId { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public string PresentStatus { get; set; } = string.Empty;
    public string? DamageStatus { get; set; }
    public string? BatteryStatus { get; set; }
    public bool IsOperational { get; set; }
    public string? IssueNotes { get; set; }
    public string ReadinessImpact { get; set; } = string.Empty;
    public bool SameAsPreviousShiftUsed { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

public sealed class EvidenceIssueReference
{
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? Status { get; set; }
}

public sealed class EvidenceSchematicSnapshot
{
    public string? Key { get; set; }
    public string? DisplayName { get; set; }
    public string? MarkData { get; set; }
    public string? MarkSummary { get; set; }
}

public sealed class EvidenceNotesSnapshot
{
    public string? ChecklistResponseSummary { get; set; }
    public string? OperationalNotes { get; set; }
    public string? DamageNotes { get; set; }
    public string? SchematicNotes { get; set; }
    public string? GeneralNotes { get; set; }
}

public static class ChecklistEvidenceSnapshotSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static string Serialize(ChecklistEvidenceSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, Options);
    }

    public static bool TryDeserialize(string? json, out ChecklistEvidenceSnapshot? snapshot)
    {
        snapshot = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            snapshot = JsonSerializer.Deserialize<ChecklistEvidenceSnapshot>(json, Options);
            return snapshot is not null && snapshot.ContractVersion == ChecklistEvidenceSnapshot.CurrentVersion;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
