using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class Jurisdiction
{
    public int Id { get; set; }

    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Level { get; set; } = ComplianceJurisdictionLevels.Country;

    public int? ParentJurisdictionId { get; set; }
    public Jurisdiction? ParentJurisdiction { get; set; }
    public bool IsSelectable { get; set; } = true;

    [MaxLength(40)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Jurisdiction> ChildJurisdictions { get; set; } = new List<Jurisdiction>();
    public ICollection<Regulator> Regulators { get; set; } = new List<Regulator>();
    public ICollection<RegulatorySource> RegulatorySources { get; set; } = new List<RegulatorySource>();
    public ICollection<ComplianceRequirementPack> RequirementPacks { get; set; } = new List<ComplianceRequirementPack>();
    public ICollection<ComplianceApplicabilityRule> ApplicabilityRules { get; set; } = new List<ComplianceApplicabilityRule>();
}

public class Regulator
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string AuthorityType { get; set; } = string.Empty;

    public int JurisdictionId { get; set; }
    public Jurisdiction? Jurisdiction { get; set; }

    [MaxLength(1000)]
    public string? OfficialWebsiteUrl { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<RegulatorySource> RegulatorySources { get; set; } = new List<RegulatorySource>();
}

public class RegulatorySource
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string SourceCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string OfficialTitle { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Classification { get; set; } = string.Empty;

    public int RegulatorId { get; set; }
    public Regulator? Regulator { get; set; }
    public int JurisdictionId { get; set; }
    public Jurisdiction? Jurisdiction { get; set; }

    [MaxLength(200)]
    public string? DocumentIdentifier { get; set; }

    [MaxLength(120)]
    public string? GazetteNumber { get; set; }

    [MaxLength(120)]
    public string? NoticeNumber { get; set; }

    [MaxLength(1000)]
    public string OfficialUrl { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<RegulatorySourceVersion> Versions { get; set; } = new List<RegulatorySourceVersion>();
}

public class RegulatorySourceVersion
{
    public int Id { get; set; }
    public int RegulatorySourceId { get; set; }
    public RegulatorySource? RegulatorySource { get; set; }

    [MaxLength(100)]
    public string VersionLabel { get; set; } = string.Empty;

    public DateTime? PublicationDate { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? SupersededAtUtc { get; set; }
    public DateTime AcquiredAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string OfficialUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string StoredArtifactReference { get; set; } = string.Empty;

    [MaxLength(64)]
    public string ContentHashSha256 { get; set; } = string.Empty;

    [MaxLength(40)]
    public string LifecycleState { get; set; } = ComplianceLifecycleStates.Draft;

    [MaxLength(2000)]
    public string? UncertaintyNote { get; set; }

    [MaxLength(2000)]
    public string? ConflictNote { get; set; }

    [MaxLength(64)]
    [ConcurrencyCheck]
    public string ConcurrencyToken { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RegulatoryClause> Clauses { get; set; } = new List<RegulatoryClause>();
    public ICollection<ComplianceRequirementPackSource> PackSources { get; set; } = new List<ComplianceRequirementPackSource>();
    public ICollection<ComplianceRuleReview> Reviews { get; set; } = new List<ComplianceRuleReview>();
}

public class RegulatoryClause
{
    public int Id { get; set; }
    public int RegulatorySourceVersionId { get; set; }
    public RegulatorySourceVersion? RegulatorySourceVersion { get; set; }

    [MaxLength(160)]
    public string ClauseCode { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? PageReference { get; set; }

    [MaxLength(500)]
    public string? Heading { get; set; }

    public string ExactText { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }

    [MaxLength(240)]
    public string? VerifiedBy { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ComplianceRequirementSourceClause> RequirementClauses { get; set; } = new List<ComplianceRequirementSourceClause>();
}

public class ComplianceRequirementPack
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string PackCode { get; set; } = string.Empty;

    [MaxLength(240)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int PrimaryJurisdictionId { get; set; }
    public Jurisdiction? PrimaryJurisdiction { get; set; }

    [MaxLength(80)]
    public string PackType { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ComplianceRequirementPackVersion> Versions { get; set; } = new List<ComplianceRequirementPackVersion>();
}

public class ComplianceRequirementPackVersion
{
    public int Id { get; set; }
    public int ComplianceRequirementPackId { get; set; }
    public ComplianceRequirementPack? ComplianceRequirementPack { get; set; }

    [MaxLength(100)]
    public string VersionLabel { get; set; } = string.Empty;

    [MaxLength(40)]
    public string LifecycleState { get; set; } = ComplianceLifecycleStates.Draft;

    [MaxLength(40)]
    public string SourceCompletenessState { get; set; } = ComplianceSourceCompletenessStates.Incomplete;

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? SupersededAtUtc { get; set; }
    public int? ActiveSlot { get; set; }

    [MaxLength(64)]
    public string? ContentHashSha256 { get; set; }

    [MaxLength(2000)]
    public string? LimitationsNote { get; set; }

    [MaxLength(2000)]
    public string? ConflictNote { get; set; }

    [MaxLength(64)]
    [ConcurrencyCheck]
    public string ConcurrencyToken { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ComplianceRequirement> Requirements { get; set; } = new List<ComplianceRequirement>();
    public ICollection<ComplianceRequirementPackSource> Sources { get; set; } = new List<ComplianceRequirementPackSource>();
    public ICollection<ComplianceRuleReview> Reviews { get; set; } = new List<ComplianceRuleReview>();
}

public class ComplianceRequirement
{
    public int Id { get; set; }
    public int ComplianceRequirementPackVersionId { get; set; }
    public ComplianceRequirementPackVersion? ComplianceRequirementPackVersion { get; set; }

    [MaxLength(120)]
    public string RequirementCode { get; set; } = string.Empty;

    [MaxLength(240)]
    public string Title { get; set; } = string.Empty;

    public string PlainEnglishRequirement { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Domain { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Classification { get; set; } = string.Empty;

    [MaxLength(8)]
    public string Priority { get; set; } = CompliancePriorities.P3;

    public bool IsPotentialBlocker { get; set; }

    [MaxLength(2000)]
    public string? ConsequenceText { get; set; }

    [MaxLength(2000)]
    public string? CorrectiveActionText { get; set; }

    [MaxLength(2000)]
    public string? UncertaintyNote { get; set; }

    [MaxLength(2000)]
    public string? ConflictNote { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ComplianceApplicabilityRule> ApplicabilityRules { get; set; } = new List<ComplianceApplicabilityRule>();
    public ICollection<ComplianceEvidenceDefinition> EvidenceDefinitions { get; set; } = new List<ComplianceEvidenceDefinition>();
    public ICollection<ComplianceRequirementSourceClause> SourceClauses { get; set; } = new List<ComplianceRequirementSourceClause>();
    public ICollection<ComplianceRuleReview> Reviews { get; set; } = new List<ComplianceRuleReview>();
}

public class ComplianceApplicabilityRule
{
    public int Id { get; set; }
    public int ComplianceRequirementId { get; set; }
    public ComplianceRequirement? ComplianceRequirement { get; set; }
    public int GroupNumber { get; set; } = 1;
    public int SortOrder { get; set; }
    public bool IsExclusion { get; set; }
    public int? JurisdictionId { get; set; }
    public Jurisdiction? Jurisdiction { get; set; }

    [MaxLength(100)]
    public string? OperatorType { get; set; }

    [MaxLength(100)]
    public string? ServiceCategory { get; set; }

    [MaxLength(100)]
    public string? LicenceCategory { get; set; }

    [MaxLength(100)]
    public string? ClinicalCapability { get; set; }

    [MaxLength(100)]
    public string? ObjectType { get; set; }
}

public class ComplianceEvidenceDefinition
{
    public int Id { get; set; }
    public int ComplianceRequirementId { get; set; }
    public ComplianceRequirement? ComplianceRequirement { get; set; }

    [MaxLength(120)]
    public string EvidenceCode { get; set; } = string.Empty;

    [MaxLength(120)]
    public string EvidenceType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ObjectType { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string VerificationMethod { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;
    public int MinimumCount { get; set; } = 1;
    public int? MaximumAgeDays { get; set; }
    public bool RequiresIndependentVerification { get; set; }
    public int SortOrder { get; set; }
}

public class ComplianceRuleReview
{
    public int Id { get; set; }
    public int? RegulatorySourceVersionId { get; set; }
    public RegulatorySourceVersion? RegulatorySourceVersion { get; set; }
    public int? ComplianceRequirementPackVersionId { get; set; }
    public ComplianceRequirementPackVersion? ComplianceRequirementPackVersion { get; set; }
    public int? ComplianceRequirementId { get; set; }
    public ComplianceRequirement? ComplianceRequirement { get; set; }

    [MaxLength(80)]
    public string ReviewType { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Decision { get; set; } = string.Empty;

    [MaxLength(240)]
    public string ReviewerName { get; set; } = string.Empty;

    [MaxLength(240)]
    public string? ReviewerOrganization { get; set; }

    [MaxLength(500)]
    public string? ReviewerCredential { get; set; }

    [MaxLength(2000)]
    public string? DecisionNote { get; set; }

    [MaxLength(500)]
    public string? EvidenceReference { get; set; }

    [MaxLength(64)]
    public string? SignatureHashSha256 { get; set; }

    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ComplianceRequirementPackSource
{
    public int Id { get; set; }
    public int ComplianceRequirementPackVersionId { get; set; }
    public ComplianceRequirementPackVersion? ComplianceRequirementPackVersion { get; set; }
    public int RegulatorySourceVersionId { get; set; }
    public RegulatorySourceVersion? RegulatorySourceVersion { get; set; }

    [MaxLength(120)]
    public string Purpose { get; set; } = string.Empty;

    public bool IsMandatory { get; set; } = true;
}

public class ComplianceRequirementSourceClause
{
    public int Id { get; set; }
    public int ComplianceRequirementId { get; set; }
    public ComplianceRequirement? ComplianceRequirement { get; set; }
    public int RegulatoryClauseId { get; set; }
    public RegulatoryClause? RegulatoryClause { get; set; }

    [MaxLength(40)]
    public string RelationshipType { get; set; } = "Creates";

    [MaxLength(1000)]
    public string? Note { get; set; }
}

public class ComplianceGovernanceEvent
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    [MaxLength(80)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ActorIdentifier { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? FromState { get; set; }

    [MaxLength(40)]
    public string? ToState { get; set; }

    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? PayloadHashSha256 { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
