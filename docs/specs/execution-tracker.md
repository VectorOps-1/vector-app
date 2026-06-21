# AcuityOps Spec Execution Tracker

This tracker is the execution gate for `docs/specs/acuityops-master-build-spec.md`.

The master spec remains the product and architecture authority. This tracker controls sequence, authorization, status, verification, and anti-deviation behavior. Product source code, database files, runtime files, generated files, and app behavior must not be changed unless the user authorizes one exact tracker row.

## Mandatory Anti-Deviation Rules

1. Do not work from memory. Read the relevant tracker row and master spec section before acting.
2. Do not invent step numbers. If the user asks for `Step 10` and the current tracker does not contain that step in the active phase, stop and report the mismatch.
3. Do not combine tracker rows. One user instruction may complete one tracker row unless the user explicitly authorizes a named batch of tracker rows.
4. Do not edit product source code during spec, tracker, audit, or planning rows.
5. Do not touch database files, runtime files, generated files, app startup, or app process state unless the active tracker row explicitly permits it.
6. Do not treat prior untracked work as complete. Prior work must be reconciled against the tracker before any row is marked `Done`.
7. Do not mark a row `Done` without the required verification evidence.
8. Do not continue to the next row automatically. Stop, report status, and provide the exact next instruction for the user to give.
9. If new work is discovered, add it as a proposed tracker row or report it as a required tracker change. Do not implement it silently.
10. If a row conflicts with the master spec, stop and request a spec/tracker correction before implementation.

## Allowed Status Values

- `Not started`
- `In progress`
- `Blocked`
- `Done`

## Required Pre-Flight Before Any Product Work

Before any product source, database, runtime, or generated-file work, Codex must state:

1. Active tracker row ID and exact title.
2. Matching master spec section.
3. Files allowed to change.
4. Files forbidden to change.
5. Whether database access is read-only, write-enabled, or forbidden.
6. Whether app startup/browser verification is allowed.
7. Required verification commands or checks.
8. Stop condition.

If any answer is unclear, the row is `Blocked`.

## Required Post-Step Report

After each authorized row, Codex must report:

1. Tracker row ID and title.
2. Final row status.
3. Files changed.
4. Verification completed.
5. Verification not run and reason.
6. Remaining risk.
7. App-readiness percentage where `100%` means a high-level production SaaS app running with real clients.
8. Exact next recommended instruction.

## Current Active Row

No implementation row is active.

The next implementation instruction must name one row from the tracker below.

## Phase 1: Workspace And Data Hygiene

| ID | Status | Master Spec Step | Authorized Scope | Verification Required | Notes |
| --- | --- | --- | --- | --- | --- |
| P1-01 | Done | Run full git/worktree audit and separate source files from runtime, generated, and database files. | Read-only audit only. | Git/worktree report. | Reconciled by git status, ignored-file scan, tracked-runtime scan, and source/runtime split review before Phase 1 checkpoint. |
| P1-02 | Done | Update `.gitignore` so SQLite files, backups, logs, `bin/`, `obj/`, artifacts, uploads, and runtime clutter do not appear in git status. | Hygiene docs/config only. | Git status before/after. | Hygiene committed in `c70ab96`; ignored runtime/database clutter verified after cleanup. |
| P1-03 | Done | Remove tracked generated files from git index without deleting local runtime files. | Git index cleanup only. | Git status before/after. | Tracked generated/runtime/database scan returned no tracked `bin`, `obj`, `artifacts`, logs, uploads, or SQLite files. |
| P1-04 | Done | Create a clean source checkpoint commit for hygiene only. | Commit hygiene slice only. | Commit hash and clean/expected status. | Hygiene checkpoint exists at `c70ab96`; spec guardrails committed separately at `c350d35`. |
| P1-05 | Done | Stop all normal startup/sample seed mutation. | Source changes limited to startup/seed paths. | Build and source scan. | Normal startup mutation removed; development repair remains explicit via `--dev-db-repair`; source checkpoint committed at `dbc4b30`; build passed with 0 warnings and 0 errors. |
| P1-06 | Done | Remove schema/data repair calls from normal page requests. | Source changes limited to request-time repair paths. | Build and route/source scan. | Legacy route/request repair cleanup committed at `dbc4b30`; route/source scan found no normal page-request repair flow; build passed with 0 warnings and 0 errors. |
| P1-07 | Done | Preserve current login accounts, but remove stale seeded company, register, checklist, schematic-assignment, and readiness artifacts from the active dev database. | Database cleanup only after backup. | Backup evidence, cleanup report, login verification. | Active dev database was backed up to `vector-dev.backup-before-p1-07-seed-artifact-cleanup-20260621-060810.db`; login records preserved; stale seeded company/register/checklist/schematic-assignment/readiness artifacts removed. |
| P1-08 | Done | Verify the app starts cleanly without recreating old seed data. | Verification only unless failures require a new tracker row. | App start, browser verification, seed/fallback scan. | Controlled `http://localhost:5000` verification completed with `P1-08 failures: none`; live empty states and no seed recreation were verified. |

## Phase 1 Closure Evidence

- Hygiene/spec commits: `c70ab96 chore: clean repository hygiene`, `f0ec80c docs: add AcuityOps master build spec`, `c350d35 Add spec execution tracker guardrails`.
- Register-driven checklist and legacy-route cleanup commits: `0729f80 fix: redirect legacy app routes`, `9ccbda2 fix: enforce register-driven checklist starts`, `dbc4b30 chore: checkpoint phase 1 cleanup`.
- Build verification: `dotnet build vector-app-local.csproj` completed with 0 warnings and 0 errors before the Phase 1 cleanup checkpoint commit.
- Database cleanup evidence: active dev database backup `vector-dev.backup-before-p1-07-seed-artifact-cleanup-20260621-060810.db` was created before stale seed artifact cleanup.
- Runtime verification evidence: P1-08 controlled verification on `http://localhost:5000` reported `P1-08 failures: none`.
- Current Phase 1 close condition: source tree was clean after `dbc4b30` before this docs-only tracker reconciliation.

## Phase Gates Requiring Child-Step Expansion Before Implementation

The rows below are locked phase gates. Before implementation starts inside any of these phases, Codex must expand the phase into child tracker rows from the master spec and wait for user authorization.

| ID | Status | Master Spec Section | Gate Requirement |
| --- | --- | --- | --- |
| P2-GATE | Done | Phase 2: Company And Tenant Source Of Truth | Expanded into child rows `P2-09` through `P2-15`; implementation remains locked until one child row is explicitly authorized. |
| P2A-GATE | Not started | Phase 2A: New Client Setup Wizard | Expand steps 15A-15R into tracker rows before implementation. |
| P3-GATE | Not started | Phase 3: Access And Permissions | Expand steps 16-23 into tracker rows before implementation. |
| P4-GATE | Not started | Phase 4: Navigation And Route Cleanup | Expand steps 24-30 into tracker rows before implementation. |
| P5-GATE | Not started | Phase 5: Checklist Source Of Truth | Expand steps 31-38 into tracker rows before implementation. |
| P6-GATE | Not started | Phase 6: Checklist Builder And Publishing | Expand steps 39-47 into tracker rows before implementation. |
| P7-GATE | Not started | Phase 7: Live Daily Checks | Expand steps 48-55 into tracker rows before implementation. |
| P8-GATE | Not started | Phase 8: PDF Evidence | Expand steps 56-61 into tracker rows before implementation. |
| P9-GATE | Not started | Phase 9: Registers And Assets | Expand steps 62-70 into tracker rows before implementation. |
| P10-GATE | Not started | Phase 10: Stock And Medication Flow | Expand steps 71 and onward from the master spec before implementation. |
| P11-GATE | Not started | Phase 11: Schematics | Expand schematic assignment and live schematic behavior into tracker rows before implementation. |
| P11A-GATE | Not started | Phase 11A: Global Vehicle Schematic Library Completion | Expand each schematic asset batch into tracker rows before generation or app integration. |
| P12-GATE | Not started | Phase 12: Readiness And Reports | Expand readiness/reporting rows before implementation. |
| P12B-GATE | Not started | Phase 12B: South African EMS Audit Compliance Mode | Expand legal/source-pack/review rows before implementation. |
| P12C-GATE | Not started | Phase 12C: South African Regulatory Source Pack And Multi-Country Compliance Architecture | Expand source compilation and jurisdiction model rows before implementation. |
| P12D-GATE | Not started | Phase 12D: SMS And Email Notification Engine | Expand provider, template, consent, audit, and retry rows before implementation. |
| P13-GATE | Not started | Phase 13: AI Import | Expand AI import, mapping, validation, and review rows before implementation. |
| P13A-GATE | Not started | Phase 13A: Clinical Guideline And SOP Knowledge UI | Expand document ingestion, source citation, UI, permission, and tier rows before implementation. |
| P14-GATE | Not started | Phase 14: AI Predictive Analysis | Expand prediction inputs, outputs, safety, reports, and validation rows before implementation. |
| P15-GATE | Not started | Phase 15: SaaS Packaging | Expand tenant packaging rows before implementation. |
| P15A-GATE | Not started | Phase 15A: Commercial Downgrade, Cancellation, And Tier Data Rules | Expand downgrade/cancellation behavior rows before implementation. |
| P15B-GATE | Not started | Phase 15B: Exact Tier Feature Matrix And Data-State Rules | Expand tier lock/read-only/export/delete behavior rows before implementation. |
| P15C-GATE | Not started | Phase 15C: Production Billing, Payment Provider, Tax, And Invoice Architecture | Expand billing provider and subscription state rows before implementation. |
| P16-GATE | Not started | Phase 16: Azure And Production | Expand Azure production architecture rows before implementation. |
| P16A-GATE | Not started | Phase 16A: Release Isolation And Client-Specific Publishing | Expand environment, tenant release, and deployment rows before implementation. |
| P16B-GATE | Not started | Phase 16B: Mandatory Security And Client Data Protection Sweeps | Expand security, encryption, backup, and access review rows before implementation. |
| P16C-GATE | Not started | Phase 16C: Mandatory Legal, Compliance, Evidence, And IP Review | Expand legal review and evidence retention rows before implementation. |
| P16D-GATE | Not started | Phase 16D: Client Data Portability, Export, And Offboarding | Expand export/delete/offboarding rows before implementation. |
| P16E-GATE | Not started | Phase 16E: Tenant Isolation And Multi-Client Provisioning Decision Record | Expand tenant ID, database, storage, domain, and backup rows before implementation. |
| P16F-GATE | Not started | Phase 16F: Production Observability And Incident Response Decision Record | Expand logs, metrics, alerts, uptime, support, and incident review rows before implementation. |
| P16G-GATE | Not started | Phase 16G: Production Support And Client-Success Operating Model | Expand onboarding, helpdesk, SLA, training, documentation, and feedback rows before implementation. |
| P16H-GATE | Not started | Phase 16H: Production Sales, Demo, And Trial-Conversion Operating Model | Expand sales/demo/trial conversion controls before implementation. |
| P16I-GATE | Not started | Phase 16I: Production Website And Marketing-Content Truth Control | Expand website claims, pricing, demo request, SEO, analytics, and legal review rows before implementation. |
| P17-GATE | Not started | Phase 17: Website And Launch | Expand website execution and launch rows after Phase 16I truth-control is done. |
| P18-GATE | Not started | Phase 18: Final Release Gate | Expand final audit, release, tagging, and production verification rows before implementation. |

## Phase 2: Company And Tenant Source Of Truth

These rows come from `docs/specs/acuityops-master-build-spec.md`, Phase 2 steps 9-15 and the mandatory Phase 2 expansion block. Phase 2 must make the company/tenant settings record the only source of truth for company name, workspace identity, logo, and logged-in branding. No row below may reintroduce seed branding, file-based branding, company-name string matching as a security boundary, or tenant leakage.

| ID | Status | Master Spec Step | Authorized Scope | Verification Required | Notes |
| --- | --- | --- | --- | --- | --- |
| P2-09 | Done | Make Master Setup the only source of company name, logo, workspace settings, and branding. | Completed in commit `3373d72` with product source and migration changes only. Active dev schema was backed up and migration-applied separately for verification; no company data cleanup was included. | Build passed before commit. Schema application verified `CompanyLogin` and `Access` no longer throw missing `BrandingStatus` errors. Authenticated senior verification confirmed `Home`, `CompanyProfile`, `CompanyName`, and `LogoUpload` return 200; `X Med` did not render on verified pages. | Company settings now own company name, trading name, workspace slug, logo reference, logo removed state, country, timezone, and branding status. Logo remove UI is conditional on a stored logo being present, so it was not submitted or forced during verification. |
| P2-10 | Done | Remove all X Med or seed branding restoration paths. | Completed in commit `1f59ce6` with product source-controlled launcher changes only. No database writes, seed repair commands, runtime files, generated files, or app startup were included. | Source scan found no app-owned `X Med`, `x-med`, `Random Med`, or `Client Business Name` references in `Data`, `Models`, `Pages`, `Services`, `Program.cs`, `wwwroot` excluding vendor libraries, or launcher scripts. `dotnet build vector-app-local.csproj` passed with 0 warnings and 0 errors. | Removed stale `x-med` workspace links from local launcher output so normal development guidance no longer directs testing to seed-branded workspace URLs. |
| P2-11 | Done | Keep AcuityOps branding on pre-company-login screens. | Completed in commit `e002f01` with product source/UI changes only. No database data, runtime files, generated files, or tenant records were touched. | Pre-auth HTTP verification confirmed `/`, `/Access`, `/CompanyLogin`, and `/CompanyLogin/x-med-workspace` render AcuityOps branding with no detected tenant logo/name leakage. Pre-auth `/RoleLogin?access=senior-management` redirects to `/CompanyLogin`. | Access and role login no longer select the first development company when no authenticated company session exists, preventing tenant branding before company authentication. |
| P2-12 | Done | Make uploaded logos persist and render across all logged-in pages. | Completed in commit `e5d0628` with product source/storage-path handling changes only. No database, backup, runtime, generated, or log files were committed. | `dotnet build vector-app-local.csproj` passed with 0 warnings and 0 errors. Controlled logo write verification backed up the active dev database, logged in through the existing senior test flow, uploaded a temporary PNG, verified Home and App Guide rendered `/uploads/company/1/company-logo.png?v=...`, replaced the logo, verified the cache-busted `?v=` value changed, then restored the database logo state and removed temporary upload files. | Uploaded logos now use the canonical company-scoped storage path, logged-in Home/App Guide use the shared company branding context, stale `Vector.CompanyName` session writes were removed, and logged-in pages render company name/logo from the settings record path only. |
| P2-13 | Done | Add remove-logo functionality. | Completed in commit `41a08d5` with product source/UI and stored logo reference handling changes only. No database, backup, runtime, generated, or log files were committed. | `dotnet build` passed with 0 warnings and 0 errors. Controlled remove-logo write verification backed up the active dev database, logged in through the existing senior test flow, uploaded a temporary transparent PNG, verified cache-busted logo rendering, verified the Remove Logo form is wired to the in-app confirmation modal, submitted the remove action, verified `LogoStoragePath = null` and `LogoRemoved = 1`, verified logged-in Home fell back to default AcuityOps branding, then restored the previous database and logo file state. | Removing a logo clears the stored reference and does not restore X Med or previous seed/default tenant branding. |
| P2-14 | Done | Verify company name and logo changes survive restart and reload. | Controlled verification completed with no product source, database schema, runtime, generated, staging, or commit changes before this tracker reconciliation. Active dev database was backed up to `vector-dev.backup-before-p2-14-company-identity-20260621224758.db`, then restored after verification. | Verified through the real company access and senior login flow: temporary company identity was saved through Company Identity, temporary PNG logo was uploaded through Logo Upload, logged-in Home rendered the temporary name and cache-busted logo after save and reload, the app was restarted, senior login was repeated, and logged-in Home still rendered the temporary name and logo. Final restore confirmed blank company name, no stored logo reference, `BrandingStatus = Incomplete`, absent logo folder, stopped app, and clean Git status. | Empty company identity remains restored for the current dev database. Company name/logo persistence across reload and restart is verified for the coded Master Setup source-of-truth path. |
| P2-15 | Done | Confirm company data is scoped by tenant/company ID everywhere. | Gate expanded into child rows `P2-15A` through `P2-15M`; no product source, database, runtime, generated files, or app startup changes were made during gate expansion. | Child rows below must be completed before Phase 2 can close. | Company names and workspace slugs must not be used as tenant isolation boundaries. Production multi-client architecture remains governed by `P16E-GATE`; these rows verify and fix the current app's tenant/company scoping. |
| P2-15A | Done | Confirm every tenant-owned data model has an approved tenant/company key. | Read-only model and migration audit completed. Most top-level tenant-owned models have direct `CompanyId`: users, permissions, audit logs, tasks, issues, areas, asset movements/files, vehicles, schematic assignments, equipment, stock, medication, checklist templates, readiness records, dropdowns, and catalogue items. No source, database, runtime, generated, staging, commit, or app-start changes were made during the audit. | Gaps reported: checklist child models are parent-scoped through `ChecklistTemplateId` / `ChecklistSectionId` / `ChecklistItemId` and must be query-verified in `P2-15B`; root/baseline EF migration and model snapshot are missing; `TaskEvent` and `UploadedFile` exist in the model/context without normal migrations; several checklist schema tables still appear in `Data/DevelopmentDatabase.cs` repair code rather than canonical migrations. | `P2-15H` is now unblocked for schema/key remediation planning. `P2-15B` remains required before parent-scoped child records can be accepted as tenant-safe in use. Global product configuration may be company-independent only when it contains no client data. Product-owned schematic library assets may be global; schematic assignments are tenant-owned. |
| P2-15B | Not started | Confirm every page, handler, and service query scopes tenant-owned data by current company/tenant. | Read-only source audit only across `Pages`, `Services`, `Data`, and shared helpers. Check `OnGet`, `OnPost`, list views, search, register, report, checklist, readiness, task, issue, staff, stock, medication, equipment, vehicle, upload, and export paths. | Report exact unscoped or weakly scoped queries with file/line references. Do not edit source or database. | Navigation hiding is not tenant isolation. Every returned tenant-owned record must be filtered server-side before rendering. |
| P2-15C | Not started | Confirm workspace login, role login, session, and tenant resolution are tenant-safe. | Read-only source audit only for workspace slug resolution, company login, role login, session keys, current-user context, redirects, invalid workspace behavior, and pre-auth branding. | Report any path that resolves tenant by company name, display text, stale session state, first-company fallback, or unsafe workspace slug handling. | Workspace slugs and company names are not security boundaries; they may only resolve to one active tenant record and must not leak another tenant's branding or data. |
| P2-15D | Not started | Confirm upload and file paths are tenant-scoped. | Read-only source audit only for logos, staff files, register imports, checklist imports, PDF outputs, report artifacts, exports, schematic assignments, SOP/guideline uploads, and any `wwwroot/uploads` or storage path usage. | Report all tenant-owned file paths that are missing company/tenant scoping, stale references, public assumptions, or unsafe shared paths. | Product-owned static assets are allowed outside tenant folders; client-owned files are not. |
| P2-15E | Not started | Confirm cache, session, temporary state, and background/task state cannot cross tenants. | Read-only source audit only for session keys, TempData, local storage assumptions, in-memory/static state, background-style services, notification queues if present, and report/export generation state. | Report exact state keys or static/shared collections that could mix tenants or survive tenant switching incorrectly. | Current development login flow may write normal session/audit state during verification only; this audit row must not start the app. |
| P2-15F | Not started | Confirm product-owned global library data is separated from tenant-owned assignments. | Read-only source audit only for vehicle schematic library, readiness defaults, checklist template libraries, source packs, and other product assets. | Report any global product asset that stores client-specific assignment, vehicle, area, register, checklist, or branding data. | Schematic images can be product-owned; assignment to function/subtype/area/callsign/vehicle is client data and must be tenant-scoped. |
| P2-15G | Not started | Confirm seed, fallback, demo, repair, and startup routines cannot create or restore tenant records. | Read-only source audit only for startup, dev repair/reset commands, route fallbacks, compatibility shims, default checklist/rendering fallbacks, and demo/sample helpers. | Report exact code paths that could create company, branding, register, checklist, schematic-assignment, readiness, or asset records outside an explicit dev-only command. | Normal startup and normal page requests must not mutate product data or resurrect seed/client records. |
| P2-15H | Not started | Fix tenant-key/schema gaps found by `P2-15A`. | Source and migration changes only, limited to explicitly reported model/schema gaps. Do not change active database data unless a later schema-application row authorizes it. | Build verification and migration review. | This row is blocked until `P2-15A` lists exact gaps. |
| P2-15I | Not started | Fix tenant query/action scoping gaps found by `P2-15B`, `P2-15C`, and `P2-15E`. | Product source changes only, limited to explicitly reported query, handler, service, session, and state scoping gaps. Do not touch database data. | Build verification plus targeted source scan proving every fixed path scopes by current company/tenant. | This row is blocked until audit rows identify exact files/lines. |
| P2-15J | Not started | Fix tenant file-storage and global-library assignment gaps found by `P2-15D` and `P2-15F`. | Product source and migration changes only where required for tenant-owned file metadata or assignment records. Do not touch runtime upload files or active database data unless a later verification row explicitly permits controlled writes. | Build verification plus file-path/source scan. | This row must not move or delete real upload files without a separate backup/restore instruction. |
| P2-15K | Not started | Add automated tenant-isolation tests for current app workflows. | Test/source changes only. Create at least two test tenants with similar company names, callsigns, staff names, asset identifiers, checklists, reports, and uploads in test setup only. | Tests must prove tenant A cannot see tenant B users, assets, documents, checklists, reports, branding, schematic assignments, readiness records, issues, tasks, or audit logs. | Test/demo data must stay in test fixtures and must not seed runtime product data. |
| P2-15L | Not started | Run controlled two-tenant browser/HTTP verification. | Verification only after `P2-15H` through `P2-15K` pass. Controlled writes are allowed only to a backed-up dev database and must be restored afterward. | Verify company login, role login, branding, Home, registers, checklists, reports, uploads, schematic assignments, tasks, issues, and audit visibility for two tenants with overlapping identifiers. Report failures only. | App startup is allowed only when this row is explicitly authorized. |
| P2-15M | Not started | Reconcile tracker evidence and close Phase 2. | Docs-only tracker update after all P2-15 child rows pass. Do not edit product source, database, runtime, generated files, or start the app. | Tracker evidence must name completed child rows, commits, build/test results, controlled verification evidence, and remaining risks. | Phase 2 cannot close if tenant leakage, unscoped query paths, seed restoration, or unsafe file paths remain. |

## Tracker Update Rules

1. Add child rows before entering a phase gate.
2. Keep existing IDs stable after they are used.
3. If a row is split, leave the original row as a gate and add child rows below it.
4. If a row is obsolete, mark it `Blocked` with the reason. Do not delete it unless the user explicitly authorizes tracker cleanup.
5. Every source-changing row must name a verification method before work starts.
