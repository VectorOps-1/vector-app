# AcuityOps Recovery Roadmap

Status: Active controlling roadmap

Created: 2026-06-26

This roadmap supersedes the previous row-by-row execution tracker as the active execution authority. The old tracker remains historical evidence only unless the user explicitly revives a specific row or migrates that row into this roadmap.

## Controlling Rule

No Codex work may fall back to the old execution tracker flow, old phase order, or old micro-row process by default. Future work must be proposed and executed from this recovery roadmap unless the user explicitly states otherwise.

## Why This Roadmap Exists

The previous process produced useful foundation work, but it became too expensive, too slow, and too fragmented. The product now needs a tighter recovery path focused on:

1. One stable testing environment.
2. Fewer but safer implementation batches.
3. Strong cost control.
4. A sellable EMS operations product.
5. Mandatory Pro and Premium differentiators.
6. No fallback to old seed/default behavior.
7. No repeated localhost launch failures as the main test loop.

## Non-Negotiable Product Direction

AcuityOps must become a multi-tenant EMS operations SaaS platform for private ambulance, response, rescue, and medical transport services.

The product must support:

1. Manual Base operations.
2. Pro import, reporting, PDF evidence, and compliance tools.
3. Premium AI import, predictive analytics, SOP/CPG ingestion, and compliance forecasting.
4. Azure-backed staging and production deployment.
5. GitHub-controlled source, CI, and deployment flow.
6. Tenant isolation, billing, support, data export, offboarding, and commercial controls.

## Environment Authority

### Local PC

Purpose:

- Development workspace only.
- Source editing.
- Targeted local builds.
- Fast checks before commit.

Not allowed as the main acceptance environment:

- Final user acceptance testing.
- Client demo truth.
- Production-like verification.
- Long repeated troubleshooting loop.

### GitHub

Purpose:

- Committed source authority.
- CI gate.
- Pull request and review history.
- Rollback point.
- Deployment trigger.

GitHub must protect the product from local drift. A change is not considered deployable until it is committed and CI passes.

### Azure Staging

Purpose:

- Stable test environment.
- One browser URL for verification.
- Product demo and workflow validation before production.
- Replacement for fragile localhost testing where practical.

Azure staging must be the next environment priority. Localhost can be used while coding, but acceptance testing should move to staging as soon as app deployment and staging database migration are ready.

### Azure Production

Purpose:

- Real client platform.
- Tenant-isolated production data.
- Billing-connected SaaS environment.
- Support, observability, backup, legal, and compliance controlled operation.

Production must not be created by accident during recovery work.

## Credit Control Protocol

This protocol is mandatory.

1. No full-app audit unless the user explicitly asks for one, or a phase/release gate requires it.
2. No row-by-row micro-implementation unless the user explicitly requests it.
3. Before editing source, Codex must propose the smallest safe batch.
4. A batch must state:
   - scope,
   - excluded work,
   - risk level,
   - verification plan,
   - commit plan,
   - estimated credit cost,
   - stop condition.
5. Normal implementation must inspect only files relevant to the approved batch.
6. Build checks should happen once per batch unless a failure requires a targeted retry.
7. Browser verification is required only when a batch changes UI, navigation, login, tenant behavior, uploads, checklists, reports, or workflows.
8. Azure commands must be limited to the approved resource/environment scope.
9. No speculative implementation.
10. No "while here" fixes.
11. No repeated verification loops after a pass.
12. If uncertainty is risky, stop and ask.
13. Major unfinished capabilities must never be mixed into cleanup or environment-stability batches.

## Anti-Deviation Rules

1. This file is the active roadmap.
2. The old execution tracker is paused and historical.
3. If Codex proposes work from the old tracker, it must first explain why and ask for explicit approval.
4. If a requested task conflicts with this roadmap, Codex must stop and identify the conflict.
5. Every batch must preserve the product direction in this roadmap.
6. Every implementation batch must end with:
   - changed files,
   - verification completed,
   - verification skipped and why,
   - remaining risk,
   - next recommended instruction,
   - whether cost was lower/equal/higher than expected.

## Recovery Phases

### Phase R1: Stabilize The Test Environment

Goal:

Make the app reliably testable from a stable URL and stop using fragile localhost as the primary verification path.

Required outcomes:

1. Azure staging resource evidence reconciled.
2. Staging app settings and Key Vault references configured.
3. Staging database migration process defined and tested.
4. Current committed app deployed to Azure staging.
5. Smoke suite passes on staging.
6. Stable staging URL documented.
7. Local-vs-staging responsibilities documented.
8. Rollback and delete plan documented.
9. Azure cost monitor active.

Do not include:

- production deployment,
- billing,
- AI features,
- new product modules,
- client data migration.

### Phase R2: Base MVP Manual Operations

Goal:

Create a clean, demonstrable Base product that can replace paper checklists and spreadsheets for a small or medium EMS provider.

Required outcomes:

1. Setup wizard works end to end.
2. Company identity and branding work.
3. Staff register works.
4. Vehicle register works.
5. Equipment register works.
6. Stock register works.
7. Medication register works.
8. Asset movement works.
9. Manual checklist builder starts blank.
10. Checklist register is the only live-check source of truth.
11. Checklist publishing works by function, subtype, area, and callsign.
12. Staff can complete assigned daily checks.
13. Managers can view submitted checks.
14. Tasks and issues work.
15. Basic readiness dashboard works from real records only.
16. No seed, fallback, or hidden default operational data appears.
17. Navigation is clear and non-duplicated.

Do not include:

- AI import,
- advanced analytics,
- DOH inspection mode,
- billing,
- public website.

### Phase R3: PDF Evidence And Report Reliability

Goal:

Make checklist and operational evidence real, exportable, and defensible.

Required outcomes:

1. Every submitted checklist opens a submitted report view.
2. Every submitted checklist can download a PDF.
3. PDF includes company identity, staff, date/time, vehicle, callsign, checklist version, rows/columns, notes, issues, schematic marks, and submission metadata.
4. Checklist reports are list-based and searchable.
5. Operational report metric tiles drill into concise lists.
6. Reports respect senior vs operational manager scope.
7. Report outputs are tenant-scoped.
8. PDF generation is tested on staging.

### Phase R4: Pro Import, Column Matching, And Checklist Conversion

Goal:

Make Pro valuable by letting clients bring existing spreadsheets into AcuityOps quickly.

Required outcomes:

1. Excel register import for vehicles, staff, equipment, stock, and medication.
2. Column matching UI.
3. Validation before import.
4. Error review and correction.
5. Duplicate detection.
6. Import preview before commit.
7. Imported checklist spreadsheet conversion into AcuityOps row/column builder format.
8. Checklist section, item, subitem, and column mapping.
9. Imported checklists save to Checklist Register.
10. Imported checklists do not become live until explicitly published.
11. Audit logs record imports and accepted mappings.
12. Base users see clear locked upgrade messaging, not broken controls.

### Phase R5: Pro South African DOH Annual Inspection Mode

Goal:

Create a Pro/Premium compliance mode that prepares EMS companies for annual Department of Health EMS inspections.

Required outcomes:

1. Compile South African DOH EMS audit requirements from authoritative sources.
2. Store source references and effective dates.
3. Identify evidence required from:
   - staff qualifications,
   - practitioner numbers,
   - licensing expiry,
   - CPD status,
   - vehicles,
   - vehicle licences,
   - equipment service records,
   - medication expiry,
   - stock expiry,
   - SOPs,
   - incident records,
   - readiness history,
   - operational areas,
   - audit logs.
4. Add DOH Inspection Mode switch for Pro/Premium.
5. Show compliance status clearly.
6. Show missing evidence.
7. Show overdue and high-risk areas.
8. Produce an exportable inspection pack.
9. Keep jurisdiction architecture extensible for other countries.

This mode must not be guessed. Requirements must be source-backed before implementation.

### Phase R6: Premium AI Import And AI Assistance

Goal:

Make Premium reduce setup friction and administrative workload.

Required outcomes:

1. AI-assisted register import.
2. AI-assisted checklist import.
3. AI column and row mapping suggestions.
4. Human review before committing data.
5. Confidence scores and warnings.
6. Audit record of AI-assisted transformations.
7. Safe handling of bad or ambiguous input.
8. No automatic silent data creation.

### Phase R7: Premium AI Analytics And Forecasting

Goal:

Make Premium a future-risk and compliance-intelligence platform.

Required outcomes:

1. 3 month forecast reports.
2. 6 month forecast reports.
3. 12 month forecast reports.
4. Service/expiry pressure prediction.
5. Stock shortage prediction.
6. Medication expiry risk.
7. Equipment failure trend detection.
8. Compliance gap forecasting.
9. Critical failure risk flags.
10. Plain-English recommendations.
11. Senior-management review controls.
12. Exportable reports.

### Phase R8: Premium SOP/CPG Knowledge UI

Goal:

Allow clients to upload SOPs, CPGs, and internal clinical documents and turn them into a usable app interface.

Required outcomes:

1. PDF and Word document upload.
2. Document parsing.
3. Structured section extraction.
4. Searchable UI.
5. Versioning.
6. Permissions.
7. Source citation back to uploaded document.
8. Human approval before publishing.
9. Tier gating.

### Phase R9: SaaS Packaging, Billing, And Tenant Operations

Goal:

Turn the app into a multi-client SaaS business.

Required outcomes:

1. Tenant provisioning.
2. Tenant isolation.
3. Tenant storage isolation.
4. Client-specific release path.
5. Base, Pro, Premium, and Enterprise feature matrix.
6. Billing provider decision.
7. Subscription state handling.
8. VAT/tax/invoices.
9. Failed payments and grace periods.
10. Downgrade and cancellation rules.
11. Data export.
12. Data deletion/offboarding.
13. Backups and restore.
14. Observability and incident response.
15. Support and client-success model.

### Phase R10: Website, Demo, Trial, And Launch

Goal:

Create a public-facing sales system that matches what the product can actually do.

Required outcomes:

1. Public website.
2. Pricing page.
3. Demo request flow.
4. Trial tenant rules.
5. Sales promises aligned to product reality.
6. Marketing claims reviewed.
7. Analytics.
8. SEO.
9. Legal review.
10. Launch checklist.

## Tier Requirements

### Base

Base must include:

- manual setup,
- manual registers,
- manual checklist builder,
- daily checks,
- issues/tasks,
- basic readiness,
- basic reports,
- basic PDF evidence.

Base must not block checklist creation.

### Pro

Pro must include:

- Excel register import,
- Excel checklist import,
- column matching,
- advanced reporting,
- PDF evidence pack,
- area/function/subtype/callsign publishing,
- DOH Annual Inspection Mode,
- approval workflows.

### Premium

Premium must include:

- AI-assisted import,
- AI-assisted checklist conversion,
- predictive analytics,
- 3/6/12 month future reports,
- SOP/CPG ingestion,
- compliance forecasting,
- Azure-backed scale and retention.

### Enterprise

Enterprise must include:

- custom terms,
- custom deployment/release controls where commercially justified,
- manual invoicing if needed,
- enhanced support,
- higher-retention and integration options.

## Batch Approval Template

Before any product-source batch, Codex must propose:

```text
Batch name:
Roadmap phase:
Scope:
Excluded work:
Files likely to change:
Database access:
Azure access:
GitHub access:
Risk level:
Verification plan:
Commit plan:
Estimated credit cost:
Stop condition:
```

The user must approve the batch before implementation.

## Verification Rules

1. Docs-only changes require no app startup.
2. Source changes require build verification.
3. UI/workflow changes require targeted browser verification.
4. Tenant or login changes require authenticated verification.
5. Azure staging changes require Azure verification commands.
6. Database schema changes require backup, migration, verification, and rollback notes.
7. No verification should be repeated after a pass unless a later change invalidates it.

## Current Immediate Direction

The next technical work should not resume the old tracker automatically.

The recommended next batch is:

1. Reconcile Azure staging provisioning evidence into the new roadmap or a short staging evidence file.
2. Configure staging app settings and secrets.
3. Define staging database migration and rollback.
4. Deploy the current committed app to staging.
5. Make staging the main verification URL.

No Pro/Premium feature implementation should begin until the staging test path is stable.

