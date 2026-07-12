# AcuityOps Commercial Completion Roadmap

Status: Month-end closeout draft

Created: 2026-07-12

This document controls the path from the current Azure staging foundation to the finished commercial AcuityOps SaaS platform. It is not a pilot-readiness or sales-demo plan. Fake tenant data may be used only to test functions and must never become seed data, fallback data, product identity, or required app data.

## Current Foundation

Verified at closeout:

- GitHub `main` is aligned with the local committed Block 1 work.
- Azure staging is live at `https://app-acuityops-stg-za-001.azurewebsites.net`.
- Latest `Deploy Azure Staging` workflow result checked during closeout: success.
- Public root returns `200`.
- `/css/site.css` returns `200`.
- `/CompanyLogin/acuityops-workspace` returns `200`.
- Azure staging remains the acceptance environment.
- Localhost remains a development workspace only.

Current product state:

- Staging has a working company-login and role-login foundation.
- Staging has a first functional checklist/report/readiness path using tenant-entered demo data.
- PDF download route returns a valid PDF.
- Readiness and Operations Reports reconcile for the verified demo evidence path.
- The app is not yet complete commercial SaaS.

Unresolved workspace item at closeout:

- `docs/specs/ui-branding-contract.md` exists as a useful branding/source-of-truth contract and should be committed as docs-only or merged into this roadmap in the next approved docs cleanup.

## Non-Negotiable Product Target

AcuityOps must become a polished, commercial, multi-tenant EMS operations SaaS platform with:

- Base manual operations that fully replace paper checklists and spreadsheet registers.
- Pro importing, column matching, PDF/reporting strength, and South African DOH Annual Inspection Mode.
- Premium AI importing, predictive analytics, SOP/CPG ingestion, and compliance forecasting.
- Secure Azure production architecture.
- Tenant isolation.
- Billing, subscriptions, downgrade/cancellation, export/offboarding, support, and incident response.
- A professional app UI, not a prototype website.

## Execution Rules

- Do not frame future work around quick pilot readiness.
- Do not build sales promises ahead of product truth.
- Do not use fake tenant data except to test functions.
- Do not create hidden seed data or fallback operational data.
- Do not mix Base stabilization with Pro/Premium AI work.
- Do not start broad audits unless a phase gate requires them.
- Each implementation batch must state scope, exclusions, verification, commit plan, estimated credit cost, and stop condition.
- Azure staging must be used for acceptance verification after each meaningful UI/workflow change.
- GitHub must remain the committed-source authority.

## Phase C1: Base Commercial Foundation

Objective:

Complete the Base product so a real EMS client can operate without seed data or manual database intervention.

Deliverables:

1. Setup Wizard completes all onboarding steps:
   - company identity,
   - logo,
   - operational structure,
   - vehicle function/subtype structure,
   - staff qualification/scope structure,
   - access model defaults,
   - register setup choices,
   - checklist setup choices,
   - readiness setup choices,
   - final review and completion.
2. Registers are complete and consistent:
   - vehicles,
   - staff,
   - equipment,
   - stock,
   - medication,
   - locations/storage,
   - edit/delete/move/issue actions,
   - grouping and search,
   - mobile-safe tables.
3. Checklist Builder is commercially reliable:
   - blank build by default,
   - sections/items/subitems/columns,
   - notes fields,
   - optional register links,
   - publish by area/function/subtype/callsign,
   - replacement warning,
   - no fixed-form fallback.
4. Live Daily Vehicle & Equipment Check uses only register-published checklist templates.
5. Tasks, issues, movement, and feedback work coherently.
6. Readiness Dashboard uses real tenant records only.
7. Reports are list-based and scoped by access level.
8. Audit logs capture core actions.
9. Access permissions are saved and enforced on actions, not only navigation.
10. App shell and branding are consistent across authenticated pages.

Acceptance criteria:

- A clean tenant can be configured through UI flows.
- No operational records are created by startup, fallback, or hidden seed logic.
- A client can manually create registers, build/publish checklists, complete checks, view reports, and manage issues/tasks.
- Staff, operational managers, and senior managers see only appropriate data/actions.
- Azure staging verification passes for the core workflows.

Expected credit scale:

- Multiple 10,000-credit blocks.
- Do not include Pro import or AI until Base is stable.

## Phase C2: PDF Evidence And Reporting Reliability

Objective:

Make submitted evidence exportable, defensible, and useful for management.

Deliverables:

1. Submitted checklist report detail page is complete.
2. PDF output includes:
   - tenant identity,
   - staff identity,
   - date/time,
   - vehicle/callsign,
   - checklist version,
   - all checklist rows/columns,
   - notes,
   - issues,
   - schematic markings,
   - submission metadata.
3. PDF layout is professional and mobile/print safe.
4. Checklist Reports are searchable/grouped by date, area, callsign, registration, user, status, type, and template.
5. Operations Reports metric tiles drill into concise list views.
6. Reports respect tenant and role scope.
7. Reports are verified on Azure staging.

Acceptance criteria:

- Every submitted checklist has a readable report and downloadable PDF.
- PDF output is suitable as compliance/evidence material.

Expected credit scale:

- One focused 10,000-credit block if Base data flows are stable.

## Phase C3: Pro Import, Column Matching, And Conversion

Objective:

Make Pro valuable by allowing clients to import existing operational data and checklists.

Deliverables:

1. Excel import for:
   - vehicles,
   - staff,
   - equipment,
   - stock,
   - medication,
   - operational areas,
   - storage locations.
2. Column matching UI:
   - detected columns,
   - target AcuityOps fields,
   - required-field validation,
   - preview before import,
   - correction table for invalid rows.
3. Excel checklist import:
   - identify sections,
   - identify rows/items/subitems,
   - identify columns/checks,
   - map to AcuityOps checklist builder structure.
4. Import audit trail and rollback path.
5. No AI dependency for basic Pro import.

Acceptance criteria:

- A client can upload a spreadsheet, map columns, validate, import, and then use the imported records/checklists inside the app.

Expected credit scale:

- Several 10,000-credit blocks.
- Requires dedicated design before code.

## Phase C4: South African DOH Annual Inspection Mode

Objective:

Provide a Pro/Premium compliance mode for South African EMS annual Department of Health inspection preparation.

Deliverables:

1. Research-backed requirement library:
   - legislation,
   - regulations,
   - provincial requirements where applicable,
   - inspection categories,
   - required documents,
   - equipment/vehicle/staff evidence expectations.
2. DOH mode dashboard:
   - compliance categories,
   - evidence completeness,
   - missing items,
   - expiring documents,
   - action list.
3. Evidence linking:
   - registers,
   - staff files,
   - vehicle files,
   - equipment service records,
   - medication/stock records,
   - SOP/CPG documents,
   - checklist evidence.
4. Export pack for inspection preparation.

Acceptance criteria:

- Client can switch into DOH mode and see what is compliant, missing, expiring, or requiring management action.

Expected credit scale:

- Research-heavy dedicated block before implementation.
- Do not mix with Base cleanup.

## Phase C5: Premium AI Importing And Analytics

Objective:

Use AI to reduce setup effort and produce forward-looking operational intelligence.

Deliverables:

1. AI register import assistance:
   - spreadsheet interpretation,
   - field matching suggestions,
   - validation explanations,
   - user approval before import.
2. AI checklist generation:
   - infer checklist structure from uploaded sheets/docs,
   - propose rows/columns/sections,
   - user approval before publish.
3. Predictive analytics:
   - 3-month,
   - 6-month,
   - 12-month forecasts for compliance, failures, shortages, service pressure, readiness deterioration, and critical-risk trends.
4. AI explanation layer:
   - why something is flagged,
   - what data was used,
   - what action is recommended.
5. Guardrails:
   - no automatic data mutation without user approval,
   - tenant isolation,
   - audit trail,
   - exportable evidence.

Acceptance criteria:

- AI accelerates import and planning but does not silently modify operational records.

Expected credit scale:

- Large dedicated phase after Pro import foundations exist.

## Phase C6: SOP/CPG Document Ingestion

Objective:

Allow higher-tier clients to upload SOPs/clinical practice guidelines and turn them into navigable app content.

Deliverables:

1. PDF/Word upload.
2. Document parsing.
3. Section extraction.
4. Searchable UI.
5. Role-scoped access.
6. Version history.
7. Acknowledgement/training evidence.
8. Link relevant SOP/CPG content to checklists, tasks, issues, and compliance mode.

Acceptance criteria:

- Uploaded SOP/CPG documents become structured, searchable app content without replacing the original file evidence.

Expected credit scale:

- Dedicated Pro/Premium phase.

## Phase C7: SaaS Production Architecture

Objective:

Make AcuityOps safe for multiple real clients.

Deliverables:

1. Azure production environment:
   - App Service,
   - Azure SQL,
   - Blob Storage,
   - Key Vault,
   - Application Insights,
   - budgets/alerts.
2. Tenant isolation:
   - tenant IDs,
   - company/workspace identity,
   - scoped queries,
   - scoped uploads,
   - scoped cache/session state.
3. Database migrations:
   - staging first,
   - backup before migration,
   - rollback plan.
4. Client-specific release rules:
   - staging,
   - production,
   - optional tenant-specific feature flags.
5. Observability:
   - logs,
   - metrics,
   - alerts,
   - incident response.

Acceptance criteria:

- Multiple tenants can operate without data leakage and without manual database patching.

Expected credit scale:

- Dedicated architecture and implementation blocks.

## Phase C8: Billing, Subscription, Export, And Offboarding

Objective:

Make the commercial SaaS model operationally safe.

Deliverables:

1. Billing provider decision:
   - Stripe,
   - Paystack,
   - South African fallback providers,
   - manual Enterprise billing.
2. Subscription states:
   - trial,
   - active,
   - grace period,
   - failed payment,
   - paused,
   - cancelled,
   - downgraded.
3. Tier enforcement:
   - Base,
   - Pro,
   - Premium,
   - Enterprise.
4. Downgrade rules:
   - what becomes read-only,
   - what remains exportable,
   - what is retained,
   - what is deleted only by explicit request.
5. Data export/offboarding:
   - complete export,
   - files,
   - audit logs,
   - reports,
   - deletion request path,
   - retention rules.
6. VAT/tax/invoices/refunds.

Acceptance criteria:

- A client can subscribe, downgrade, cancel, export data, and leave without losing trust or creating legal/commercial risk.

Expected credit scale:

- Dedicated production-commercial phase.

## Phase C9: Support, Documentation, Website, And Truth Control

Objective:

Make AcuityOps commercially presentable without overpromising.

Deliverables:

1. Help/docs inside app.
2. Admin support tools.
3. Support escalation model.
4. Training/onboarding material.
5. Public website.
6. Pricing page.
7. Demo request flow.
8. SEO/analytics.
9. Legal review of claims.
10. Trademark/legal consultation checklist.

Acceptance criteria:

- Public claims match product reality.
- Support model exists before real client onboarding.

Expected credit scale:

- Dedicated launch-preparation block.

## Next 10,000-Credit Block 2 Plan

Objective:

Move from the current staged foundation into Base Commercial Foundation completion. This is not a pilot plan; it is the next commercial-product construction block.

Block 2 scope:

1. Setup Wizard closeout:
   - complete remaining setup steps,
   - verify completion state,
   - ensure no seed data is created,
   - ensure setup produces only configuration choices or user-created records.
2. Register consistency pass:
   - vehicles,
   - staff,
   - equipment,
   - stock,
   - medication,
   - consistent edit/delete/move/issue actions,
   - grouped list layouts,
   - mobile-safe horizontal scroll.
3. Checklist source-of-truth hardening:
   - Build New Checklist starts blank,
   - checklist publish scope by function/subtype/area/callsign,
   - live daily checks use only published register templates,
   - no fallback fixed form.
4. Access permissions enforcement:
   - senior vs operational manager vs staff action boundaries,
   - register edit permissions,
   - checklist publish/request permissions.
5. Evidence baseline:
   - confirm report detail and PDF remain functional after Base changes.

Excluded from Block 2:

- AI import.
- Excel import.
- DOH mode.
- Billing.
- production launch.
- global schematic expansion.
- SOP/CPG ingestion.
- major UI redesign beyond needed consistency.

Verification:

- Build once per implementation batch.
- Deploy to Azure staging after meaningful workflow changes.
- Verify staging routes for affected workflows.
- Use fake tenant data only to test functions through normal UI workflows.
- No direct database data patching unless explicitly approved for test cleanup.

Commit plan:

- One source commit per coherent implementation batch.
- One docs commit only when roadmap/status docs change.

Estimated credit use:

- 10,000 credits should cover a controlled part of Block 2, not the entire Base product.
- Recommended first Block 2 implementation batch: Setup Wizard closeout plus no-seed verification.
- Recommended second Block 2 implementation batch: register consistency for vehicles/staff/equipment only.
- Defer stock/medication/checklist/access follow-up if credits run low.

Stop conditions:

- Any seed/fallback behavior reappears.
- A workflow requires direct database patching to work.
- A change threatens tenant isolation.
- A batch expands into Pro/Premium features.
- Azure staging verification fails in a way that requires architecture work.
