# Block 7 Premium AI And Knowledge Intelligence Execution Blueprint

Status: Proposed for approval; no implementation accepted

Updated: 2026-07-29

## Authority And Boundary

This blueprint decomposes core commercial block `B7` from:

- `docs/specs/commercial-launch-progress-tracker.md`
- `docs/specs/acuityops-recovery-roadmap.md`
- `docs/specs/commercial-completion-roadmap.md`
- `docs/specs/acuityops-master-build-spec.md`
- `docs/specs/block-5-pro-import-execution-blueprint.md`
- `docs/specs/adr-block-7-ai-provider-and-data-residency.md`

The Commercial Launch Progress Tracker remains the sole progress authority.
This document controls only Block 7 implementation scope.

Block 7 contains three parts:

1. `B7.1` Premium AI Import Intelligence.
2. `B7.2` SOP/CPG Knowledge System.
3. `B7.3` Operational Forecasting And Integrated Closure.

Add-on Track A1, historical Block 6, Department of Health inspection packs,
regulatory requirements, regulatory conclusions, and regulatory compliance
forecasting are excluded. Core operational forecasting may identify operational
control, readiness, expiry, service, stock, medication, staffing, checklist,
task, issue, and acknowledgement risks. It must not state that a tenant is
legally compliant or non-compliant.

## Credit-Control Execution Rules

1. Execute one complete part at a time. Do not split a passing part into
   approval-heavy micro-rows.
2. Inspect only direct dependencies and changed workflows.
3. Reuse accepted Block 5 parsing, mapping, preview, correction,
   deduplication, transactional commit, removal, audit, and checklist-draft
   conversion behavior.
4. Do not rebuild deterministic behavior as an AI feature.
5. Each part receives no more than:
   - one implementation proposal;
   - one source/migration commit;
   - one tracker/docs evidence commit;
   - one GitHub staging deployment;
   - one targeted staging acceptance pass.
6. Use automated provider fakes and disposable databases before paid provider
   calls or browser verification.
7. Use `Medium` reasoning for bounded UI, provider adapters, routine services,
   tests, builds, and staging checks.
8. Use `High` reasoning for migrations, tenant isolation, evidence integrity,
   prompt-injection controls, clinical/policy publishing, and cross-module
   source-of-truth decisions.
9. Use `XHigh` only for an irreversible provider, security, or data-residency
   decision that cannot be resolved by the approved ADR.
10. No full-app audit, PDF redesign, A1 work, billing work, production release,
    website work, or schematic expansion may be included.

## Shared Safety Contract

All three parts must satisfy these rules:

- AI output is a proposal, draft, explanation, or prioritization. It is never
  an automatic operational record mutation.
- A human with the correct tenant permission must explicitly approve a
  proposed import, publication, or action.
- Provider requests and responses are tenant-bound and correlation-bound.
- Provider credentials never enter source control, tenant data, prompts,
  reports, logs, or exported evidence.
- Prompt templates and output schemas are versioned. Every invocation records
  the provider, deployment alias, prompt version, schema version, and input
  fingerprint.
- Structured output must validate against an application-owned schema before
  it reaches domain services.
- Untrusted uploaded text is treated as data, never as executable
  instructions.
- Low confidence, invalid structure, conflicts, unsupported citations,
  missing evidence, and provider failures default to review or refusal.
- Provider failure leaves deterministic Block 5 and ordinary document access
  available where the tenant tier permits it.
- AI may not create credentials, grant permissions, publish a checklist,
  publish clinical guidance, create a task, modify a register, or alter
  historical evidence without an explicit supported user action.
- Every AI-assisted decision remains reconstructable from immutable source
  references, version metadata, reviewer decisions, and audit events.

## Shared AI Governance Data Contract

The following additive tenant-owned models are introduced in `B7.1` and reused
by all Block 7 parts.

### `AiProcessingJob`

- `Id`
- `CompanyId`
- `RequestedByAppUserId`
- `FeatureKey`
- `SourceType`
- `SourceReferenceId`
- `InputFingerprintSha256`
- `ProviderKey`
- `DeploymentAlias`
- `ModelVersion`
- `PromptVersion`
- `OutputSchemaVersion`
- `Status`
- `RequestedAtUtc`
- `StartedAtUtc`
- `CompletedAtUtc`
- `AttemptCount`
- `SafeFailureCode`
- `SafeFailureMessage`
- `CorrelationId`
- `ConcurrencyToken`

### `AiJobAttempt`

- `Id`
- `CompanyId`
- `AiProcessingJobId`
- `AttemptNumber`
- `ProviderRequestId`
- `StartedAtUtc`
- `CompletedAtUtc`
- `Status`
- `InputUnits`
- `OutputUnits`
- `DocumentPages`
- `EmbeddingUnits`
- `EstimatedCostMinorUnits`
- `BillingCurrency`
- `RateCardVersion`
- `FailureCategory`

Raw secret-bearing provider payloads are not persisted. Retained output is the
validated application DTO or a safely redacted diagnostic.

### `AiSuggestionSet`

- `Id`
- `CompanyId`
- `AiProcessingJobId`
- `TargetDomain`
- `TargetReferenceId`
- `Status`
- `OverallConfidence`
- `WarningCount`
- `CreatedAtUtc`
- `SubmittedForReviewAtUtc`
- `DecidedByAppUserId`
- `DecidedAtUtc`

### `AiSuggestion`

- `Id`
- `CompanyId`
- `AiSuggestionSetId`
- `SuggestionKind`
- `SourceLocator`
- `CanonicalTargetKey`
- `ProposedValueJson`
- `Confidence`
- `Explanation`
- `WarningCodesJson`
- `SortOrder`

### `AiHumanDecision`

- `Id`
- `CompanyId`
- `AiSuggestionId`
- `Decision`
- `CorrectedValueJson`
- `DecisionNote`
- `DecidedByAppUserId`
- `DecidedAtUtc`

Decision values are `Accepted`, `Corrected`, `Rejected`, and `Deferred`.
Original suggestions are immutable after review begins.

### `CompanyAiUsagePolicy`

- `Id`
- `CompanyId`
- `EnabledFeaturesJson`
- `MonthlySoftLimitMinorUnits`
- `MonthlyHardLimitMinorUnits`
- `PerJobLimitMinorUnits`
- `MaxConcurrentJobs`
- `AllowHighCapabilityEscalation`
- `ChangedByAppUserId`
- `ChangedAtUtc`

No policy record means AI processing is denied. This model is not a billing
entitlement replacement; Block 10 remains the commercial entitlement authority.

### `AiUsageLedger`

- `Id`
- `CompanyId`
- `AiProcessingJobId`
- `FeatureKey`
- `ProviderKey`
- `DeploymentAlias`
- `OccurredAtUtc`
- `InputUnits`
- `OutputUnits`
- `DocumentPages`
- `EmbeddingUnits`
- `SearchUnits`
- `EstimatedCostMinorUnits`
- `BillingCurrency`
- `RateCardVersion`

Ledger rows are append-only.

## Shared Provider Interfaces

Provider-specific SDK types must not cross these interfaces:

```csharp
public interface IAiStructuredOutputProvider
{
    Task<StructuredAiResult<T>> ExecuteAsync<T>(
        StructuredAiRequest request,
        JsonSchemaContract<T> schema,
        CancellationToken cancellationToken);
}

public interface IAiPromptRegistry
{
    PromptDefinition GetRequired(string featureKey, string version);
}

public interface IAiRedactionService
{
    RedactedAiInput Redact(AiInputEnvelope input);
}

public interface IAiUsageMeter
{
    Task<AiBudgetDecision> AuthorizeAsync(
        Guid companyId,
        AiCostEstimate estimate,
        CancellationToken cancellationToken);

    Task RecordAsync(AiUsageRecord usage, CancellationToken cancellationToken);
}

public interface IAiJobQueue
{
    Task EnqueueAsync(AiJobEnvelope job, CancellationToken cancellationToken);
}

public interface IDocumentExtractionProvider
{
    Task<ExtractedDocument> ExtractAsync(
        DocumentExtractionRequest request,
        CancellationToken cancellationToken);
}

public interface IFileSecurityScanner
{
    Task<FileSecurityResult> ScanAsync(
        SecureFileReference source,
        CancellationToken cancellationToken);
}

public interface IKnowledgeSearchProvider
{
    Task<KnowledgeSearchResult> SearchAsync(
        TenantKnowledgeQuery query,
        CancellationToken cancellationToken);
}

public interface IEmbeddingProvider
{
    Task<IReadOnlyList<EmbeddingVector>> CreateAsync(
        TenantEmbeddingRequest request,
        CancellationToken cancellationToken);
}
```

Forecast-specific interfaces are defined in `B7.3`.

## Azure Provider And Resource Boundary

The binding decision is in
`docs/specs/adr-block-7-ai-provider-and-data-residency.md`.

Block 7 targets:

- Azure-hosted structured-output model deployments behind
  `IAiStructuredOutputProvider`;
- Azure AI Document Intelligence for supported PDF/OCR/layout extraction;
- server-side Open XML parsing for DOCX;
- tenant-scoped Azure Blob Storage for immutable originals and normalized
  artifacts;
- Azure Storage Queue for asynchronous staging jobs unless a measured
  reliability requirement justifies Service Bus;
- Azure AI Search only from `B7.2`;
- Key Vault and managed identity for credentials;
- Application Insights and Azure Monitor for job failures, latency, usage, and
  tenant-impact alerts;
- a production-grade malware scanner before production document processing.

No Azure resource is provisioned by this blueprint. Each part must complete a
live SKU, regional availability, privacy, and projected-cost preflight before
resource creation.

# Part 1: B7.1 Premium AI Import Intelligence

## Objective

Use AI to interpret ambiguous register and checklist source files, then hand
human-approved corrections into the accepted Block 5 deterministic contract.

## Scope

1. Add the shared AI governance contract and provider abstractions.
2. Add Premium feature and saved-action permission gates.
3. Accept the Block 5 source fingerprint, worksheet/column metadata, bounded
   sample values, and parser warnings as AI input.
4. Suggest:
   - target register domain;
   - source-column to canonical-field mappings;
   - normalization rules;
   - duplicate-key candidates;
   - checklist layout type;
   - section, row, column, item, and subitem structure;
   - unresolved ambiguity and confidence.
5. Present every suggestion beside deterministic Block 5 validation.
6. Require explicit accept, correct, reject, or defer decisions.
7. Feed only approved mappings or draft structures into Block 5.
8. Support PDF checklist extraction through the document extraction boundary,
   then convert the normalized result into a Block 5 checklist draft.
9. Record job, cost, provider/model/prompt/schema versions, reviewer decisions,
   final Block 5 batch/template references, and audit events.
10. Preserve deterministic guided import when AI is disabled, unavailable,
    over budget, or rejected.

## Block 5 Non-Negotiable Contract

AI must not:

- write to register tables;
- create a checklist template directly;
- bypass canonical field definitions;
- bypass row conversion, validation, correction, duplicate decisions, preview,
  transactional commit, governed removal, audit, or explicit checklist
  publication;
- create staff login identities or credentials;
- infer missing values and present them as source facts;
- silently reuse a prior mapping.

The only accepted path is:

`source -> deterministic inspection -> AI suggestions -> human decisions ->
Block 5 mapping/draft -> Block 5 validation/preview -> explicit commit or
publication`.

## Additional Models

### `AiImportProposal`

- `Id`
- `CompanyId`
- `ImportBatchId`
- `AiSuggestionSetId`
- `ProposedDomain`
- `ProposedChecklistLayout`
- `Status`
- `CreatedAtUtc`

### `AiImportColumnProposal`

- `Id`
- `CompanyId`
- `AiImportProposalId`
- `SourceColumnKey`
- `CanonicalFieldKey`
- `ProposedTransformationKey`
- `Confidence`
- `EvidenceJson`
- `WarningCodesJson`

### `AiChecklistStructureProposal`

- `Id`
- `CompanyId`
- `ImportBatchId`
- `AiSuggestionSetId`
- `ProposedName`
- `StructureJson`
- `SourceCitationJson`
- `Status`

`StructureJson` must validate against a versioned checklist-draft schema. It is
not executable markup and cannot contain scripts, rules, or provider-authored
database identifiers.

## Human Review Gates

- No AI job runs until the user explicitly selects AI assistance.
- No source value is sent unless required for the approved feature and passed
  through redaction/minimization.
- A reviewer sees source location, suggestion, confidence, warnings, and the
  deterministic alternative.
- Required or safety-sensitive fields cannot be accepted below the configured
  confidence threshold without a reviewer correction and note.
- Checklist names remain user-editable and are saved only through the normal
  draft workflow.
- Final register commit and checklist publication remain separate explicit
  actions.

## Migration

One additive migration:

`AddPremiumAiImportGovernance`

It creates the shared AI governance tables and Part 1 proposal tables. It must:

- add no provider credentials;
- add no AI policy, entitlement, import, tenant, checklist, or register rows;
- use restrictive tenant foreign keys;
- index every tenant query with `CompanyId`;
- enforce append-only usage/audit behavior in application services;
- apply and roll back on disposable SQLite;
- generate a reviewable SQL Server script without provider-specific unsafe SQL.

## Tests

Automated tests must prove:

- tenant A cannot submit, view, review, meter, or apply tenant B suggestions;
- staff and unauthorized operational managers cannot invoke or approve AI;
- Base and Pro cannot invoke Premium AI;
- provider output failing JSON schema is rejected;
- prompt-injection text in cells, PDFs, filenames, and worksheet names is
  treated as source data;
- redaction removes configured personal/sensitive fields;
- soft, hard, per-job, and concurrency limits work;
- retry behavior is bounded and idempotent;
- provider failure leaves Block 5 deterministic import usable;
- accepted mapping enters Block 5 validation and does not write directly;
- AI checklist output creates only an editable draft;
- no staff login identity is created;
- duplicate decisions remain explicit;
- every provider call and human decision is audited;
- migration apply/rollback is safe on disposable SQLite;
- generated SQL Server migration is additive;
- zero automatic tenant/product records are created.

## Staging Acceptance

Use synthetic, non-personal files through real Premium UI workflows:

1. messy vehicle or equipment spreadsheet;
2. ambiguous stock or medication columns;
3. staff file proving no login creation;
4. checklist spreadsheet with irregular headings;
5. checklist PDF;
6. malicious prompt-injection text;
7. low-confidence and provider-failure cases;
8. duplicate source and existing-record cases.

The reviewer must be able to reject AI and finish with deterministic Block 5.
Temporary imports are removed through supported workflows.

## Acceptance Criteria

`B7.1` is accepted only when AI reduces mapping effort while every resulting
record or checklist draft still passes Block 5 and every mutation remains
human-controlled, tenant-scoped, cost-bounded, auditable, and removable.

## Hard Stops

Stop if:

- provider structured outputs cannot be schema-enforced;
- the selected deployment processes data outside the approved geography;
- AI needs direct domain-table write access;
- Block 5 must be weakened or duplicated;
- staff import could grant access;
- an active database migration is destructive;
- projected Azure staging cost exceeds the approved staging budget;
- a provider key must enter source or app settings outside Key Vault;
- tenant isolation fails.

# Part 2: B7.2 SOP/CPG Knowledge System

## Objective

Convert tenant-owned SOP, CPG, policy, procedure, and operational manual files
into a controlled, cited, reviewable, searchable knowledge interface.

## Tier Boundary

- Base: ordinary document storage only where entitled.
- Pro: deterministic extraction, navigation, keyword search, review, version,
  publish, acknowledgement, and role/scope controls.
- Premium: AI-assisted restructuring, semantic/hybrid search, cited Q&A,
  change comparison, and operational knowledge analytics.

Block 7 implements the Premium intelligence layer and the shared controlled
knowledge foundation needed for it. It does not implement A1 evidence mapping
or regulatory conclusions.

## Models

### `KnowledgeDocument`

- `Id`, `CompanyId`, `Title`, `Category`, `SourceType`, `OwnerAppUserId`
- `Status`, `CurrentPublishedVersionId`, `CreatedAtUtc`, `RetiredAtUtc`

### `KnowledgeDocumentVersion`

- `Id`, `CompanyId`, `KnowledgeDocumentId`, `VersionLabel`
- `EffectiveDate`, `Status`, `VersionNote`
- `ReviewedByAppUserId`, `ReviewedAtUtc`
- `ApprovedByAppUserId`, `ApprovedAtUtc`, `PublishedAtUtc`, `RetiredAtUtc`
- `ContentFingerprintSha256`, `ConcurrencyToken`

### `KnowledgeSourceFile`

- `Id`, `CompanyId`, `KnowledgeDocumentVersionId`
- `ImmutableBlobReference`, `OriginalFileName`, `MediaType`, `SizeBytes`
- `Sha256`, `UploadedByAppUserId`, `UploadedAtUtc`
- `SecurityScanStatus`, `ExtractionStatus`

### `KnowledgeProcessingJob`

- `Id`, `CompanyId`, `KnowledgeDocumentVersionId`, `AiProcessingJobId`
- `JobType`, `Status`, `AttemptCount`
- `ProviderKey`, `ProviderModelVersion`
- `StartedAtUtc`, `CompletedAtUtc`, `SafeFailureCode`

### `KnowledgeSection`

- `Id`, `CompanyId`, `KnowledgeDocumentVersionId`, `ParentSectionId`
- `Heading`, `SectionNumber`, `BodyCanonicalJson`
- `SourcePageFrom`, `SourcePageTo`, `SortOrder`
- `ReviewStatus`

### `KnowledgeChunk`

- `Id`, `CompanyId`, `KnowledgeSectionId`, `ChunkText`
- `ChunkFingerprintSha256`, `EmbeddingReference`
- `IndexStatus`, `AccessScopeFingerprint`

### `KnowledgeTopic`

- `Id`, `CompanyId`, `Name`, `TopicType`, `IsActive`

### `KnowledgeNavigationNode`

- `Id`, `CompanyId`, `KnowledgeDocumentVersionId`, `ParentNodeId`
- `KnowledgeSectionId`, `KnowledgeTopicId`, `Title`, `SortOrder`
- `VisibilityScopeJson`

### `KnowledgeCitation`

- `Id`, `CompanyId`, `KnowledgeDocumentVersionId`, `KnowledgeSectionId`
- `KnowledgeSourceFileId`, `PageNumber`, `Heading`
- `SourceTextLocator`, `TableReference`, `Confidence`

### `KnowledgeReviewItem`

- `Id`, `CompanyId`, `KnowledgeDocumentVersionId`, `KnowledgeSectionId`
- `ReviewType`, `Severity`, `Description`, `Status`
- `AssignedReviewerAppUserId`, `ResolutionNote`, `ResolvedAtUtc`

### `KnowledgePublishScope`

- `Id`, `CompanyId`, `KnowledgeDocumentVersionId`
- `ScopeType`, `ScopeReferenceId`, `RoleKey`, `ClinicalScopeKey`

### `KnowledgeAcknowledgement`

- `Id`, `CompanyId`, `KnowledgeDocumentVersionId`, `AppUserId`
- `Status`, `DueAtUtc`, `AcknowledgedAtUtc`, `EvidenceFingerprint`

### `KnowledgeChangeLog`

- `Id`, `CompanyId`, `OldVersionId`, `NewVersionId`
- `KnowledgeSectionId`, `ChangeType`, `Summary`
- `ReviewedByAppUserId`, `CreatedAtUtc`

### `KnowledgeAccessRule`

- `Id`, `CompanyId`, `KnowledgeDocumentId`
- `ActionKey`, `SubjectType`, `SubjectReferenceId`, `Effect`

### `KnowledgeQuestion`

- `Id`, `CompanyId`, `AskedByAppUserId`, `QuestionText`
- `ScopeFingerprint`, `AskedAtUtc`

### `KnowledgeAnswer`

- `Id`, `CompanyId`, `KnowledgeQuestionId`, `AiProcessingJobId`
- `AnswerText`, `Status`, `AnsweredAtUtc`, `RefusalReason`

### `KnowledgeAnswerCitation`

- `Id`, `CompanyId`, `KnowledgeAnswerId`, `KnowledgeCitationId`
- `QuotedExcerpt`, `SortOrder`

## Ingestion Pipeline

1. Validate tier, permission, type, extension, media signature, size, and tenant.
2. Store the original immutably in a tenant-scoped blob path.
3. Calculate and store SHA-256.
4. Run malware/security scanning.
5. Reject or quarantine unsafe files.
6. Extract PDF/OCR/layout through Document Intelligence.
7. Parse DOCX through server-side Open XML.
8. Normalize headings, paragraphs, lists, tables, images, pages, and source
   locators into application-owned canonical JSON.
9. Create deterministic sections and citations.
10. Use AI only to propose classification, navigation, summaries, conflict
    flags, change descriptions, and risk-review items.
11. Validate output schemas.
12. Route low-confidence, clinical, dosage, contraindication, scope,
    conflicting, or uncited content to review.
13. Require assigned clinical/policy review before publication.
14. Index only approved published content.
15. Apply tenant and publish-scope filters in every search query.
16. Generate Q&A only from authorized published retrieval results.
17. Refuse answers without sufficient approved citations.
18. Keep retired versions immutable and available to authorized audit/export
    workflows.

## Search And Citation Contract

- Every Azure AI Search document carries `CompanyId`, document version,
  publication status, and access-scope filter fields.
- Query code supplies server-owned tenant and access filters; the model cannot
  author or relax them.
- Hybrid search combines keyword and vector retrieval only against published,
  permitted content.
- Every answer must cite stored `KnowledgeCitation` records.
- The UI links citations to the original source page/section.
- An answer with no reliable source must explicitly refuse.
- Prompt text, retrieved content, and user questions remain untrusted data.
- Search and Q&A are logged without storing provider secrets or unnecessary
  sensitive text.

## Human Review Gates

- Extraction remains draft until reviewed.
- AI summaries and navigation labels remain draft until accepted.
- Medication dosage, contraindication, treatment, clinical scope, or emergency
  procedure content requires designated clinical review.
- Policy, HR, and operational content requires designated policy/management
  review.
- Conflicting sources cannot be silently merged.
- Publishing requires permission, completed mandatory review items, complete
  citations, and an immutable source file.
- AI cannot publish or retire knowledge.
- Acknowledgements bind to an exact published version.

## Migration

One additive migration:

`AddTenantKnowledgeSystem`

It creates the knowledge tables and references the shared AI job contract. It
must insert no documents, topics, access rules, tenant records, embeddings,
prompts, or sample content.

Azure AI Search indexes, Blob containers, Queue configuration, and provider
deployments are infrastructure and are not represented as database migrations.
Their configuration is versioned in deployment documentation and validated
before use.

## Tests

Tests must cover:

- clean DOCX;
- scanned PDF;
- structured clinical guideline PDF;
- tables, lists, images, page citations, and malformed files;
- file signature mismatch, oversize file, and malware result;
- low-confidence OCR;
- clinical dosage/scope review routing;
- conflicting documents;
- missing citation refusal;
- tenant isolation in database, blob paths, queues, search indexes, embeddings,
  retrieval, answers, acknowledgements, and exports;
- role, area, clinical-scope, and publish-scope filtering;
- unpublished and retired content exclusion;
- prompt injection in documents and questions;
- version comparison and immutable prior versions;
- acknowledgement evidence;
- provider failure/retry/idempotency;
- budget enforcement;
- migration apply/rollback and SQL Server script review;
- zero automatic content publication.

## Staging Acceptance

Use synthetic non-patient documents only:

1. one clean DOCX SOP;
2. one scanned PDF SOP;
3. one clinical guideline PDF;
4. one conflicting document pair;
5. one low-confidence OCR file;
6. one malicious prompt-injection file;
7. two tenants with overlapping document names;
8. staff, operational manager, senior manager, and clinical reviewer scopes.

Acceptance must prove upload, scan, extraction, review, correction, publication,
keyword and semantic search, cited Q&A/refusal, version change, acknowledgement,
retirement, export, tenant isolation, and temporary-data cleanup.

## Acceptance Criteria

`B7.2` is accepted only when an authorized tenant can turn a supported source
document into published, cited, searchable knowledge without losing the
immutable source, reviewer control, version history, access scope, or tenant
boundary.

## Hard Stops

Stop if:

- production file scanning is unavailable;
- original files cannot be immutable and tenant-scoped;
- the search provider cannot enforce server-owned tenant filters;
- answers can be generated without stored citations;
- clinical-risk content can publish without assigned review;
- one tenant can infer another tenant's document, index, embedding, or answer;
- a selected provider processes data outside the approved geography;
- the staging budget would be exceeded;
- implementation requires A1 rules, conclusions, or evidence adapters.

# Part 3: B7.3 Operational Forecasting And Integrated Closure

## Objective

Produce explainable 3-month, 6-month, and 12-month operational risk forecasts
from real tenant data while separating deterministic facts and calculations
from AI-authored explanations.

## Forecasting Boundary

Core forecasting may cover:

- vehicle/equipment service and expiry pressure;
- stock shortage and consumption pressure;
- medication expiry and shortage pressure;
- practitioner licence and CPD expiry pressure;
- repeated checklist failures and readiness deterioration;
- repeated equipment/vehicle defects;
- unresolved issue and task patterns;
- knowledge publication and acknowledgement gaps;
- area, subtype, callsign, base, and company operational patterns.

Core forecasting may not:

- decide legal or regulatory compliance;
- use A1 requirement packs or conclusions;
- state that a licence, service, base, or company will pass or fail a Department
  of Health inspection;
- provide patient-specific clinical advice;
- silently create tasks, orders, issues, or register changes.

## Models

### `OperationalForecastRun`

- `Id`, `CompanyId`, `RequestedByAppUserId`
- `ScopeType`, `ScopeReferenceId`
- `HorizonMonths`, `AsOfUtc`, `Status`
- `CalculationVersion`, `NarrativePromptVersion`
- `InputCoverageScore`, `DataQualityStatus`
- `RequestedAtUtc`, `CompletedAtUtc`

### `OperationalForecastInputSnapshot`

- `Id`, `CompanyId`, `OperationalForecastRunId`
- `DomainKey`, `SourceCutoffUtc`
- `RecordCount`, `CanonicalSnapshotJson`
- `SnapshotSha256`

Snapshot data is immutable and contains only the minimized fields required for
the forecast.

### `OperationalForecastFinding`

- `Id`, `CompanyId`, `OperationalForecastRunId`
- `FindingType`, `AffectedDomain`, `AffectedReferenceId`
- `AreaId`, `Callsign`, `HorizonDate`
- `ProbabilityBand`, `Severity`, `Priority`
- `DeterministicFactsJson`, `CalculationTraceJson`
- `Confidence`, `DataQualityWarning`

### `OperationalForecastRecommendation`

- `Id`, `CompanyId`, `OperationalForecastFindingId`
- `RecommendationText`, `RationaleText`
- `AiProcessingJobId`, `Status`, `SortOrder`

### `OperationalForecastReview`

- `Id`, `CompanyId`, `OperationalForecastRunId`
- `ReviewerAppUserId`, `Decision`, `ReviewNote`, `ReviewedAtUtc`

### `OperationalForecastExport`

- `Id`, `CompanyId`, `OperationalForecastRunId`
- `Format`, `GeneratedAtUtc`, `GeneratedByAppUserId`
- `ImmutableFileReference`, `Sha256`

## Forecast Provider Interfaces

```csharp
public interface IForecastFeatureSnapshotBuilder
{
    Task<ForecastFeatureSnapshot> BuildAsync(
        ForecastScope scope,
        DateTime asOfUtc,
        CancellationToken cancellationToken);
}

public interface IForecastCalculationEngine
{
    ForecastCalculationResult Calculate(
        ForecastFeatureSnapshot snapshot,
        ForecastHorizon horizon);
}

public interface IForecastNarrativeProvider
{
    Task<ForecastNarrativeResult> ExplainAsync(
        ForecastCalculationResult calculation,
        CancellationToken cancellationToken);
}

public interface IForecastExportService
{
    Task<ImmutableForecastExport> GenerateAsync(
        Guid forecastRunId,
        ForecastExportFormat format,
        CancellationToken cancellationToken);
}
```

`IForecastCalculationEngine` is deterministic and versioned. AI may explain,
group, and prioritize its findings, but it cannot replace the calculation
trace or invent source facts.

## Calculation And Review Flow

1. Resolve tenant, role, and permitted scope.
2. Capture an immutable as-of snapshot.
3. Record source cutoffs, counts, missing domains, and data quality.
4. Calculate deterministic expiry/service/consumption/repeat-event features.
5. Produce versioned 3/6/12-month findings.
6. Send only minimized finding facts to the narrative provider.
7. Schema-validate explanations and recommendations.
8. Display facts, calculation trace, confidence, coverage, warnings, and
   recommendations separately.
9. Require senior review for company-wide outputs.
10. Restrict operational managers to assigned areas.
11. Require explicit user action to create a task, stock order, issue, or
    follow-up from a recommendation.
12. Generate tenant-scoped export evidence from the accepted snapshot and
    findings.

## Migration

One additive migration:

`AddOperationalForecasting`

It creates the forecast run, input snapshot, finding, recommendation, review,
and export tables. It inserts no forecasts, policies, data snapshots, tenant
records, tasks, or recommendations.

## Tests

Tests must prove:

- 3, 6, and 12-month horizons are calculated from the same as-of contract;
- exact-boundary expiry/service dates are classified consistently;
- no data and insufficient data produce explicit non-authoritative states;
- deterministic calculations are repeatable;
- AI explanation cannot change a calculation fact;
- provider failure still leaves deterministic findings readable;
- role and assigned-area scopes are enforced server-side;
- tenant isolation covers snapshots, findings, narratives, exports, and usage;
- later register changes do not alter prior snapshots;
- no recommendation creates an action automatically;
- A1 tables/services are not referenced;
- regulatory terms or conclusions are rejected from core output;
- costs and concurrency are bounded;
- migration apply/rollback and SQL Server script are safe.

## Integrated Block 7 Acceptance

One controlled Azure staging acceptance pass must prove:

1. Premium AI import proposes mappings and a checklist draft but uses Block 5
   for correction, commit, and publication.
2. Deterministic import remains usable with AI disabled.
3. SOP/CPG ingestion preserves the source and citations, requires review, and
   produces tenant-scoped search and cited answers/refusals.
4. Forecasts produce 3/6/12-month operational findings with traceable facts,
   data sufficiency, review, and export.
5. Staff and unauthorized managers cannot invoke, approve, publish, or view
   out-of-scope AI content.
6. Two tenants with overlapping names and identifiers remain isolated across
   jobs, files, queues, indexes, embeddings, suggestions, knowledge, forecasts,
   exports, and usage.
7. Cost limits stop excess processing.
8. No seed/fallback data, silent mutation, historical evidence rewrite,
   product-owned schematic change, A1 activation, or regulatory conclusion
   occurs.
9. Temporary staging data is removed through supported workflows.
10. The tracker records all three accepted parts and the core score changes
    only after the complete B7 acceptance gate passes.

## Deployment Sequence

For each part:

1. Confirm the approved blueprint/ADR and current staging budget.
2. Inspect only direct dependencies.
3. Implement source and one additive migration.
4. Run Release build and targeted automated tests.
5. Apply migration to an isolated disposable SQLite database.
6. Generate and inspect the SQL Server migration script.
7. Commit the verified source/migration slice.
8. Push through the existing GitHub Linux staging path.
9. Back up staging.
10. Apply the additive migration to staging.
11. Provision or configure only the resources approved for that part.
12. Run the targeted staging acceptance matrix.
13. Remove temporary data through supported workflows.
14. Record evidence in the Commercial Launch Progress Tracker.
15. Commit the tracker/docs evidence and lock the part.

Part order is mandatory: `B7.1 -> B7.2 -> B7.3`.

## Block 7 Credit Envelope

Planning range:

- `B7.1`: 8,000-12,000 credits.
- `B7.2`: 9,000-15,000 credits.
- `B7.3`: 7,000-13,000 credits.
- Total: 24,000-40,000 credits.

These are planning ranges, not a metered promise. Cost control comes from
bounded parts, accepted dependency reuse, automated fakes, limited provider
calls, no repeated audits, and one staging pass per part.

## Block 7 Hard Stops

Stop the active part if:

- destructive migration or historical rewrite is required;
- tenant, role, area, search-index, blob, queue, embedding, or export isolation
  fails;
- selected data processing geography conflicts with the ADR;
- safe rollback or temporary-data cleanup is unavailable;
- AI can mutate operational data without human approval;
- provider output cannot be schema-validated;
- source citations cannot be enforced;
- clinical/policy review cannot be enforced;
- a provider or always-on Azure SKU would exceed the approved budget;
- Azure/GitHub access requires user interaction;
- work would enter A1/B6, Block 8+, billing, production, website, or unrelated
  modules.
