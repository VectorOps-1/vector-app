# ADR: Block 7 AI Provider And Data Residency

Status: Proposed for approval

Date: 2026-07-29

## Decision

Block 7 uses an Azure-first provider architecture. Initial staging and
production implementation must use Azure-hosted AI, extraction, storage,
search, queue, secret, and observability services behind application-owned
interfaces. The application must not call the public OpenAI API directly in the
initial Block 7 implementation.

The preferred processing region is `South Africa North`.

Use a regional model deployment only when the required structured-output model
and capacity are available in the approved region. Do not silently switch to a
Global or Data Zone deployment. Those deployment types can change where
processing occurs.

`West Europe` is a conditional fallback, not an automatic fallback. It requires
all of:

1. written founder approval;
2. documented POPIA/privacy and cross-border data-flow review;
3. confirmation of the exact provider/model processing location;
4. tenant-facing disclosure where required;
5. a cost and latency comparison;
6. tracker evidence before use.

## Context

Block 7 processes operational registers, checklists, staff-related source files,
SOPs, CPGs, policies, and forecast inputs. It therefore requires stronger
tenant isolation, source provenance, review, and cost controls than a generic
chat feature.

The application already uses Azure staging and has an accepted deterministic
Block 5 import contract. The selected architecture must extend that contract,
not replace it.

## Provider Bindings

| Capability | Initial provider decision | Activation part |
| --- | --- | --- |
| Structured AI output | Azure-hosted model deployment supporting strict JSON schema | B7.1 |
| PDF/OCR/layout extraction | Azure AI Document Intelligence | B7.1 for checklist PDF; B7.2 for knowledge |
| DOCX extraction | Server-side Open XML parser | B7.2 |
| Immutable original and normalized artifacts | Tenant-scoped Azure Blob Storage | B7.2 |
| Background jobs | Azure Storage Queue through `IAiJobQueue` | B7.1 |
| Keyword/hybrid/vector search | Azure AI Search | B7.2 only |
| Embeddings | Azure-hosted embedding deployment through `IEmbeddingProvider` | B7.2 only |
| Secrets | Azure Key Vault accessed by managed identity | B7.1 |
| Runtime identity | App Service managed identity with least-privilege RBAC | B7.1 |
| Metrics/logs/alerts | Application Insights and Azure Monitor | B7.1 |
| File security | Production-grade malware scanner through `IFileSecurityScanner` | Required before B7.2 production use |

No specific model marketing name is hardcoded into product behavior. The app
uses capability aliases:

- `ai-structured-low-cost`
- `ai-structured-high-capability`
- `ai-embedding`

The deployed provider, model family, model version, region, prompt version, and
schema version are recorded for every job. Live model availability and pricing
must be checked during the provisioning preflight because Azure availability
changes by region and deployment type.

## Model Selection Policy

1. Use the lowest-cost approved model that passes the feature's schema,
   accuracy, safety, and latency acceptance tests.
2. Use `ai-structured-low-cost` for column classification, mapping proposals,
   section labels, and bounded summaries.
3. Escalate to `ai-structured-high-capability` only after:
   - the lower-cost result fails a recorded quality rule;
   - the tenant policy allows escalation;
   - the estimated job cost remains below the limit.
4. Never retry by silently changing model, region, or prompt version.
5. Provider temperature and nondeterminism are minimized for extraction,
   mapping, and evidence-oriented work.
6. Structured outputs must match an application-owned JSON schema.
7. Provider output is never executed as code, SQL, HTML, a query filter, or a
   rule expression.

## Data Residency And Privacy Rules

1. South Africa North is preferred for storage and processing.
2. Regional services must be colocated where required for private connectivity
   and managed-identity access.
3. Global/Data Zone model deployments are disabled unless separately approved.
4. Tenant files, extracted text, prompts, retrieved chunks, embeddings,
   suggestions, answers, and forecasts remain tenant-bound.
5. Send only minimized fields required for the approved feature.
6. Patient-specific records and patient advice are outside Block 7.
7. Staff personal data is redacted or omitted unless the feature explicitly
   requires it and has an approved lawful purpose.
8. Provider prompts and outputs must not be used to train shared models without
   explicit contractual and product approval.
9. Abuse-monitoring, content-retention, and support-access behavior must be
   documented for the selected Azure deployment before production activation.
10. Original files and retained artifacts follow the tenant's later Block 10
    retention/export/deletion contract.

## Tenant Isolation

- Database rows use `CompanyId` and restrictive foreign keys.
- Blob keys include an opaque tenant identifier, not company display name.
- Queue messages include signed server-owned tenant/job identifiers and no
  provider secrets.
- Search documents include `CompanyId`, publication state, and access-scope
  filters.
- Search filters are constructed server-side; model/user text cannot override
  them.
- Embedding references are tenant-scoped and cannot be queried without the
  same server-owned filter.
- Provider correlation IDs are not authorization.
- Every service resolves the current tenant independently of request-supplied
  company IDs.
- Two-tenant overlapping-identifier tests are mandatory in every part.

## Authentication And Secret Handling

- Use App Service managed identity wherever supported.
- Grant least-privilege RBAC at the individual resource scope.
- Store unavoidable credentials in Key Vault.
- Do not store secrets in source, GitHub workflow text, database rows, prompts,
  logs, reports, exports, or client-side code.
- Rotate credentials without changing application code.
- Provider health diagnostics must redact keys, tokens, prompt bodies, source
  files, and tenant-sensitive output.

## Cost-Control Decision

The existing staging monthly budget is `USD 75`. Block 7 staging resources and
usage must remain within that total unless the user approves a revised budget.

Mandatory controls:

1. Run a live Azure price/SKU/region preflight before each resource is created.
2. Prefer consumption, pay-as-you-go, basic, free, or existing shared staging
   resources where they satisfy isolation and reliability.
3. Do not use provisioned throughput or reserved AI capacity in staging.
4. Do not provision Azure AI Search during B7.1.
5. Use Azure Storage Queue before Service Bus unless measured requirements
   justify the additional service.
6. Enforce tenant monthly soft/hard limits, per-job limits, concurrency limits,
   file/page/row/token limits, bounded retries, and timeout limits.
7. Estimate cost before queueing and reject jobs above the hard limit.
8. Record actual usage and estimated cost in the append-only ledger.
9. Cache only tenant-safe extraction or embedding results keyed by immutable
   source fingerprint, provider/model, and schema version.
10. Never repeat a paid call merely for browser verification when a recorded,
    schema-valid result can be reused.
11. Use provider fakes for automated tests.
12. Stop provisioning if projected monthly staging spend exceeds `USD 75`.

## Human Review Decision

AI is advisory:

- Import suggestions require mapping review and Block 5 confirmation.
- Checklist generation creates only a draft and requires explicit publication.
- Knowledge extraction and AI restructuring require assigned review.
- Clinical or medication content requires designated clinical review.
- Q&A must cite approved published sources or refuse.
- Forecast recommendations require management review.
- Tasks, issues, orders, register edits, and publication require explicit
  application actions.

## Azure Staging Resource Plan

This ADR does not provision resources. The later provisioning sequence is:

### B7.1

1. Confirm South Africa North model and Document Intelligence availability.
2. Reuse existing staging Key Vault, App Service managed identity, storage
   account, Application Insights, and budget where suitable.
3. Create only the required regional structured-output deployment, extraction
   resource, and Storage Queue.
4. Add resource-scoped RBAC.
5. Configure Key Vault references or managed identity.
6. Add budget/usage alerts before paid acceptance calls.

### B7.2

1. Confirm Azure AI Search SKU, South Africa North feature support, and monthly
   cost.
2. Create a staging search service only after explicit cost approval.
3. Create tenant-filtered indexes and embedding deployment.
4. Configure tenant-scoped Blob containers/paths.
5. Configure and verify the selected malware scanner.
6. Refuse production-like ingestion if file scanning is not active.

### B7.3

Reuse B7.1 model, queue, monitoring, and storage resources. Do not create a
separate forecasting AI platform unless measured capacity requires it.

## Deployment And Rollback

1. Provider adapters remain disabled by default.
2. Additive migrations precede feature activation.
3. Feature activation requires Premium entitlement, saved action permission,
   tenant AI usage policy, provider health, and budget authorization.
4. Deploy through the existing GitHub Linux Azure staging workflow.
5. If a provider fails, disable the provider/feature flag without rolling back
   domain data.
6. Existing deterministic imports and ordinary document access remain
   available.
7. Rollback never deletes immutable source, human decisions, usage ledger, or
   accepted operational records.
8. A database rollback is allowed only before data is written to the new
   tables and after a verified backup.

## Alternatives Rejected

### Direct public OpenAI API for initial implementation

Rejected because it introduces a second secret, billing, support, processing,
and observability path while the platform is already Azure-based.

### AI replacing Block 5

Rejected because it would remove deterministic validation, correction,
transaction, audit, removal, and explicit publication controls.

### One shared unfiltered vector index

Rejected because tenant filtering and scope filtering are mandatory security
boundaries.

### Global deployment as an automatic availability fallback

Rejected because processing geography may differ and cannot be changed without
an explicit privacy/data-residency decision.

### Provisioned throughput for staging

Rejected because the current workload does not justify fixed cost.

### AI-only forecasting

Rejected because operational findings must remain traceable, repeatable, and
readable when the provider is unavailable.

## Verification Gate

Before implementation begins, confirm:

- the selected regional services and models are available;
- structured outputs are supported;
- exact processing geography is documented;
- projected staging cost fits the approved budget;
- managed identity/RBAC is supported;
- tenant filters can be server-enforced;
- provider retention/abuse-monitoring behavior is documented;
- file scanning is selected before B7.2 production-like use;
- the user approves any West Europe fallback.

## Official References

- Azure Direct Models data, privacy, and processing geography:
  https://learn.microsoft.com/en-us/azure/foundry/responsible-ai/openai/data-privacy
- Azure structured outputs:
  https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/structured-outputs
- Azure AI Document Intelligence managed identities:
  https://learn.microsoft.com/azure/ai-services/document-intelligence/authentication/managed-identities?view=doc-intel-4.0.0
- Azure AI Search vector and hybrid search:
  https://learn.microsoft.com/en-us/azure/search/vector-search-overview
- Azure AI Search regional availability:
  https://learn.microsoft.com/en-us/azure/search/search-region-support
- Microsoft Foundry model regional availability:
  https://learn.microsoft.com/en-us/azure/foundry/foundry-models/concepts/models-sold-directly-by-azure-region-availability
- Microsoft Foundry regional support:
  https://learn.microsoft.com/en-us/azure/foundry/reference/region-support
