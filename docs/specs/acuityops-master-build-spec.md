# AcuityOps Master Build Spec

This file is the controlling execution spec for AcuityOps. Work must follow this file unless the user explicitly changes the spec. Do not introduce hidden phases, hidden prerequisites, or undocumented shortcuts.

## Non-Deviation Rules

1. Do not add new product features while source-of-truth, routing, permissions, seed cleanup, database integrity, or PDF evidence work is incomplete.
2. Do not use seed data, fallback data, compatibility templates, or hidden defaults in normal app operation.
3. Do not make live workflows depend on records that are not visible in the relevant register or setup page.
4. Do not treat navigation hiding as permission enforcement. Permissions must be enforced on actual page actions and post handlers.
5. Do not create duplicate pathways for the same user task.
6. Do not make a user type values where the source of truth already exists as a register, setup record, area, vehicle, staff profile, checklist template, or schematic assignment.
7. Do not make broad claims of completion. A step is complete only after source inspection, implementation, build verification, and browser verification when applicable.
8. Keep `http://localhost:5000` as the stable local app URL for verification.
9. Commit only intentional source changes in logical slices after verification.
10. If a change reveals a better product decision, report it as a suggested improvement before continuing.
11. `docs/specs/execution-tracker.md` is the mandatory execution gate for this spec. Codex must not perform product source, database, or runtime changes unless the requested work maps to an existing tracker row.
12. Codex must not invent phase numbers, step numbers, or continuation steps. If the user instruction does not match a tracker row, Codex must stop and report the mismatch before doing any implementation work.
13. A tracker row may be worked only when the user explicitly authorizes that exact phase and step. Broad requests must be translated into tracker rows before work starts.
14. Before implementation, Codex must run the tracker pre-flight check: confirm the active row, confirm allowed files, confirm forbidden files, confirm verification required, and confirm the exact stop condition.
15. After implementation or a blocked attempt, Codex must update the tracker status and report the exact next tracker instruction. The status report must include the app-readiness percentage requested by the user.
16. Documentation-only changes to the spec or tracker are allowed only when explicitly requested. They must not be mixed with product source code changes.

## Credit Control Protocol

This protocol overrides the previous row-by-row micro-implementation workflow. The goal is to reduce credit waste without reducing safety, source-of-truth discipline, tenant isolation, or verification quality.

Mandatory cost-control rules:

1. Codex must not continue row-by-row micro-implementation unless the user explicitly requests a single tracker row.
2. Before editing product source, Codex must propose the smallest safe batch of related tracker rows.
3. Each batch proposal must include:
   - `Scope`: tracker rows and files/modules expected to change.
   - `Excluded work`: related work that will not be touched.
   - `Risk level`: low, medium, or high, with the reason.
   - `Expected verification`: build, tests, smoke checks, browser checks, database checks, or provider checks.
   - `Commit plan`: one source commit plus one tracker/docs commit by default.
   - `Estimated credit cost`: low, medium, or high, with the main cost driver.
4. Full-app audits are allowed only at phase gates, release gates, or when the user explicitly requests a full audit.
5. Normal implementation work must use targeted file inspection only. Codex must not reread broad app areas unless the batch scope requires it.
6. Automated build and smoke checks must run before browser verification where practical.
7. Browser verification is required for UI, navigation, login, tenant, upload, checklist, reporting, or workflow changes. It is not required for docs-only changes or pure backend changes unless the batch risk demands it.
8. Commits must be grouped as one source commit plus one tracker/docs commit per batch unless a risk requires smaller commits.
9. Codex must stop before risky uncertainty instead of exploring broadly. Risky uncertainty includes unclear tenant ownership, unclear source of truth, destructive data changes, provider choice, legal/compliance uncertainty, billing consequences, or security implications.
10. Major unfinished capabilities must never be mixed into cleanup batches.
11. If a batch reveals a new major feature, provider decision, legal dependency, or architecture dependency, Codex must record it as future work and stop before coding it.
12. The tracker remains the execution gate. Batching changes how authorized rows are grouped; it does not permit undocumented work or silent scope expansion.

Dedicated major-capability rule:

The following capabilities must be treated as dedicated focused phases or subphases. Each requires its own design/spec, acceptance criteria, architecture/provider decision, cost-risk review, implementation batch, verification plan, and tier/commercial impact review before product code is written:

- AI register and checklist importing.
- AI-generated compliance and future-risk analytics.
- PDF evidence generation and downloads.
- Environment stabilization and cloud staging.
- Azure production storage and deployment architecture.
- Multi-tenant SaaS packaging and client-specific releases.
- Billing, invoices, VAT/tax, subscriptions, downgrade/cancellation rules, and refunds.
- SMS and email notifications.
- Compliance audit modes, starting with South African EMS audit requirements.
- SOP/CPG document ingestion into app UI.
- Global vehicle schematic library expansion.
- Website, pricing, demo, trial, and marketing truth-control.
- Client data export, deletion, retention, and offboarding.

Roadmap preservation rule:

The product vision and phase order below remain valid. The Credit Control Protocol changes execution discipline, not the product direction. If a roadmap conflict is found, Codex must document the conflict in this spec or tracker and stop; it must not silently change phase order or product scope.

## Execution Tracker Gate

The master spec defines the product, rules, and roadmap. The execution tracker at `docs/specs/execution-tracker.md` controls what may be executed in the current work sequence.

Mandatory operating rules:

1. No active tracker row means no product implementation.
2. The user instruction must match the tracker `ID` or exact phase/step wording before work starts.
3. If the instruction is broader than one tracker row, Codex must ask the user to choose or must propose the next exact tracker instruction without editing product source.
4. If a required step is missing from the tracker, Codex must update the tracker first, then wait for the user to authorize that row.
5. If implementation reveals additional required work, Codex must add it as a new tracker row or suggested tracker change. It must not execute the new work in the same step unless the user explicitly authorizes it.
6. Phase gates in the tracker must be expanded into child rows before implementation starts for that phase.
7. Tracker status values are limited to `Not started`, `In progress`, `Blocked`, and `Done`.
8. A row can be marked `Done` only after the acceptance criteria and verification stated in the master spec and tracker row are satisfied.

## Required Step Completion Protocol

After every completed step, Codex must post a status update with:

1. Step number and name.
2. Files changed.
3. What was verified.
4. Whether `dotnet build` passed.
5. Whether browser verification on `http://localhost:5000` passed.
6. Remaining risk, if any.
7. Suggested improvement or add-on before continuing.
8. Exact next recommended instruction.

## Required Step Detail Standard

No step in this spec may be treated as implementation-ready if it is only a concept, slogan, placeholder, or broad instruction. Before coding any step, Codex must confirm that the step contains or has been expanded to contain the following concrete details:

1. `Objective`: the exact user-facing or platform-facing outcome.
2. `Source of truth`: the register, setup record, tenant record, source pack, product asset, or service that owns the data.
3. `Affected files/modules`: pages, page models, services, models, migrations, JavaScript, CSS, tests, and documentation expected to change.
4. `Routes and navigation`: old routes removed or redirected, new routes added, Page Index behavior, Home/module entry point, and deep-link behavior.
5. `Data model`: tables/entities, fields, relationships, indexes, tenant keys, status fields, version fields, and deletion/retirement behavior.
6. `Permissions`: who can view, create, edit, delete, approve, reject, publish, export, or override; permissions must be enforced server-side, not only hidden in UI.
7. `Workflow`: start state, user actions, save behavior, approval behavior, failure behavior, success behavior, and post-action destination.
8. `UI states`: empty state, loading state, validation errors, confirmation modals, success toast, locked/read-only state, mobile layout, and sticky horizontal scroll where required.
9. `Audit events`: every meaningful create, edit, delete, publish, approval, rejection, export, login/access, billing, AI, notification, source-pack, or compliance action.
10. `Integration behavior`: billing, AI, Azure storage, PDF, notification, tenant isolation, source-pack, website, or provider behavior where relevant.
11. `No-fallback rule`: no seed data, demo data, hidden default, fixed form, stale route, compatibility fallback, or inferred assignment may satisfy a live workflow.
12. `Tests`: build verification, unit/integration checks where practical, browser verification, mobile verification where relevant, and negative tests proving the wrong behavior cannot happen.
13. `Acceptance criteria`: exact conditions that prove the step is complete.
14. `Rollback/safety`: backup, migration safety, data preservation, and rollback requirements for risky changes.

If any of these details are missing, Codex must update this spec before implementing the step. A step may be marked `Blocked` only when a required external source, legal review, provider decision, or user decision is genuinely unavailable.

## Product Definition

AcuityOps is a multi-client EMS operational readiness platform. It connects company setup, staff profiles, vehicles, equipment, stock, medication, unit schematics, checklists, tasks, issues, asset movement, expiry pressure, readiness scoring, audit evidence, PDF reports, AI-assisted imports, and future predictive analysis into one controlled operational system.

The core product promise is:

`AcuityOps tells ambulance leadership what is ready, what is missing, what is unsafe, who is responsible, and what must happen next.`

## Source-Of-Truth Model

1. Master Setup is the source of truth for company identity, branding, areas, access setup, and setup-level controls.
2. Vehicle Register is the source of truth for registration, callsign, function, subtype, area, status, schematic assignment, service data, and license data.
3. Staff Register is the source of truth for staff identity, clinical qualification/scope, practitioner number, annual license expiry, CPD status, CPD expiry, assigned area, and access relationship.
4. Equipment Register is the source of truth for equipment item identity, serial/asset ID, model, service date, battery requirement, operational state, and location.
5. Stock Register is the source of truth for disposable stock item, type/subtype, quantity, batch, expiry, and location.
6. Medication Register is the source of truth for medication name, quantity, batch, expiry, and location.
7. Checklist Register is the only source of truth for live checklist templates.
8. Unit Schematic Library is product-owned reference data. Schematic assignments are client/company records and must be explicit.
9. Readiness Engine active rules are the source of truth for readiness scoring.
10. Audit Log is the source of truth for who changed what, when, and from which access level.

## Spec Detail Audit Results And Mandatory Action Expansions

This audit covers the current spec folder. The folder contains one controlling file: `docs/specs/acuityops-master-build-spec.md`. Later production documentation may split this into separate decision records, but this file remains the controlling execution spec until the split is explicitly approved.

Audit findings:

1. `Phases 1-12` are directionally correct but several steps are one-line implementation goals. They require concrete data, route, permission, UI, test, and acceptance detail before coding.
2. `Phase 11A` is detailed for schematic assets and is acceptable, but each asset sprint still needs a batch target list before image generation starts.
3. `Phase 12B/12C` now has source-pack structure, but activation depends on AI/Codex source compilation plus legal/regulatory review. No audit mode implementation may bypass that.
4. `Phase 14` was under-specified. The mandatory expansion below now defines the required inputs, outputs, safety rules, and acceptance criteria before any AI forecasting work starts.
5. `Phase 15` is an index phase only. Implementation must use the detailed tier, billing, tenant, downgrade, cancellation, and production architecture phases that follow it.
6. `Phase 17` overlaps with `Phase 16I`. Phase 17 must be treated as website execution only after Phase 16I truth-control, sales, billing, legal, and demo controls are done.
7. `Phase 18` is correct as a release gate, but each audit must produce a written artifact, not just a verbal claim.

Mandatory expansion for `Phase 1: Workspace And Data Hygiene`:

- Define the source/runtime split before cleanup: source files, migrations, tests, docs, product assets, generated build output, active SQLite files, backups, logs, uploads, browser artifacts, and temporary files.
- Run `git status --short`, ignored-file scan, tracked-runtime-file scan, migration list, and database file inventory before changing anything.
- Update ignore rules only for runtime/generated files; never ignore source, migrations, docs, product schematics, or real static assets.
- Remove tracked generated files from the Git index only; do not delete local files unless the user explicitly approves.
- Replace startup mutation with explicit dev-only reset/repair commands. Normal startup must not seed, repair, recreate templates, assign schematics, create readiness rules, or restore company branding.
- Remove page-request schema/data repair from normal app flows. Page requests may read data and validate state; they may not mutate schema or repair records.
- Before database cleanup, create a dated backup. Preserve login accounts required for testing. Remove stale seed companies, orphan records, hidden checklist templates, active scopes pointing to missing templates, schematic assignments pointing to missing vehicles, demo-only stock/medication/equipment, and hidden readiness rules.
- Acceptance criteria: clean git visibility, app starts, login works, no seed data is recreated after restart, and live workflows show empty states when the relevant register/setup record is empty.

Mandatory expansion for `Phase 2: Company And Tenant Source Of Truth`:

- Create or confirm a single company/tenant settings record that owns company name, trading name, workspace slug, logo path/blob ID, logo removed state, country, timezone, and branding status.
- Remove file-based or hardcoded company-name/logo reads unless they are one-time migrations into the company settings record.
- Pre-company-login and access-gate screens must show AcuityOps branding only until company authentication completes.
- Logged-in pages must render company name/logo from the company settings record only.
- Logo upload must support upload, preview, save, replace, remove, and fallback-to-no-logo states. Removing a logo must clear the stored reference.
- Acceptance criteria: name/logo survive restart, X Med does not return unless entered in Master Setup, multiple tenants do not leak branding, and empty company name renders clean neutral copy.

Mandatory expansion for `Phase 3: Access And Permissions`:

- Define permission records by tenant, staff profile, access level, assigned area/base, manager scope, explicit permissions, delegated permissions, active state, and audit metadata.
- Access Setup must select an existing Staff Register record. It must not create a person from manual text fields.
- Granular permissions must cover register edit/delete, checklist build/edit/publish, readiness engine edit/request/approve, issue delete, task delete, stock order approval, staff access management, audit/compliance view, export, billing, and company setup.
- Every sensitive `OnPost` handler and page action must check permission server-side.
- Navigation hiding is allowed only after server-side permission checks exist.
- Acceptance criteria: staff cannot delete manager records, ops managers see only assigned scope, senior managers see company-wide records, and every denied action returns a controlled denied state.

Mandatory expansion for `Phase 4: Navigation And Route Cleanup`:

- Build a route inventory with current route, owner page, intended module, replacement route, redirect behavior, and removal decision.
- Legacy checklist routes must redirect to register-driven daily check or show a controlled removed-route message.
- Page Index must use broad-to-narrow selections: Home, module/work area, exact action. It must not show stale aliases or routes.
- Duplicate module buttons must be removed from Home when the module page already owns the subactions.
- Acceptance criteria: no user-facing route points to old daily/equipment checklist pages, no duplicate Manager Areas path remains, and every Page Index selection lands on a current page.

Mandatory expansion for `Phase 5: Checklist Source Of Truth`:

- Define checklist template records, version records, section records, row/item records, column records, subitem records, notes records, publish scopes, retired/deleted states, and audit events.
- Live daily checks may load only an active published scope that points to a non-retired checklist template visible in Checklist Register.
- Deleting or retiring a checklist must retire every active scope and prevent future live loading.
- Blank builder must create no default operational fields, equipment rows, schematic rows, or hidden fixed-form fields unless the user explicitly selects a template.
- Acceptance criteria: an empty register yields `No assigned checklist available`; a blank saved checklist renders blank; deleting a template makes it unavailable immediately; no fixed-form fields render without register-backed sections.

Mandatory expansion for `Phase 6: Checklist Builder And Publishing`:

- Builder must define section type, custom section name, row/item, subitem, column heading, input type, source selector, register link, required flag, editable flag, readiness flag, previous-shift flag, note flag, and per-row column override.
- Source selectors must be real options: fresh entry, vehicle register, staff register, equipment register, stock register, medication register, area/base/storage setup, schematic library, readiness engine, and fixed dropdown options.
- Publish flow must select target mode and exact target: all areas, specific area/base, vehicle function, vehicle subtype, specific registration/callsign, or future custom group.
- Publish screen must show existing active scopes and warn when replacement will occur.
- Ops managers submit for approval unless granted explicit publish permission. Senior managers approve, reject, send back, or publish.
- Acceptance criteria: every register item button has one destination, every publish target is explicit, replacements are warned and audit logged, and build/save/publish states are visible.

Mandatory expansion for `Phase 7: Live Daily Checks`:

- Vehicle selection must use registration dropdown sourced from Vehicle Register and callsign auto-populated from that vehicle, with allowed manual adjustment for the check record only.
- Selecting a vehicle must resolve checklist by published scope priority: specific vehicle/callsign, subtype, function, area, all areas, then none. The priority order must be explicit and tested.
- Same-as-previous-shift must appear only after a checklist loads and must copy only the previous checklist for the same vehicle from a different profile where allowed.
- Schematic section must use assigned schematic from the register/library assignment path only.
- Saving must create a submitted evidence record and clear the active draft/new form.
- Acceptance criteria: no checklist fields render without active published scope, schematic views switch image and fields together, marks save/reload/export, and staff/ops/senior all use the same live rendering rules.

Mandatory expansion for `Phase 8: PDF Evidence`:

- Define PDF service, storage location, generated file metadata, tenant isolation, download permission, report link, regeneration policy, and audit event.
- PDF must render the submitted evidence exactly as saved, not current template state if the template changed later.
- PDF must include checklist template/version, scope, submitter, timestamp, role, area, vehicle, callsign, registration, all responses, notes, issues, schematic marks, readiness/status outputs, and signature/identity evidence where implemented.
- Acceptance criteria: PDF opens, downloaded file matches the submitted checklist, old reports tolerate deleted template items, and PDF export is tested on desktop and mobile-sized reports.

Mandatory expansion for `Phase 9: Registers And Assets`:

- Each register must define grouping, search, list columns, open detail view, edit form, delete/retire behavior, move/reallocate behavior, issue creation, date coloring, audit events, and mobile sticky scroll.
- Vehicle Register grouping is Function then Subtype, collapsed by default.
- Staff Register grouping is clinical qualification/scope, not access role.
- Equipment/stock/medication grouping defaults to item name and supports location grouping.
- Asset movement destinations must include vehicles, bases, areas, storage locations, and other relevant register-backed destinations.
- Acceptance criteria: every listed item can open and edit, delete uses in-app confirmation, movement updates source-of-truth location everywhere, and ops manager scope limits records correctly.

Mandatory expansion for `Phase 10: Stock And Medication Flow`:

- `Stock Orders & Distribution` must own ordering, supplier confirmation, stock receipt, register entry, allocation, and movement into vehicles/bases/stores.
- Remove direct routes/buttons that bypass this flow unless they are read-only report links.
- Stock and medication registers must share grouping, search, open, edit, delete, move, issue, expiry, batch, quantity, and location behavior.
- Acceptance criteria: no duplicate stock order path exists, receipt updates register quantities, allocation updates location, and expiry status reflects register data.

Mandatory expansion for `Phase 12: Readiness And Reports`:

- Readiness Dashboard must use active Readiness Engine rules only.
- Operational Reports metric tiles must drill into list views with source record, submitter, area, callsign, status, action taken, and link to source.
- Checklist Reports must be list-based, searchable, grouped by date/area/callsign, and open submitted report view before PDF download.
- Variance Alerts must be list-based with date, callsign, field, previous value, captured value, reviewer, decision, and register-update outcome.
- Acceptance criteria: senior scope is company-wide, ops scope is assigned area only, no redundant cards duplicate drilldowns, and every list item links to the correct source record.

Mandatory expansion for `Phase 14: AI Predictive Analysis`:

- Define AI report inputs: submitted checks, readiness scores, issues, tasks, asset movements, stock levels, medication expiries, service dates, staff licensing/CPD, audit gaps, variance alerts, and SOP acknowledgement gaps.
- Define report horizons: 3 months, 6 months, and 12 months.
- Define outputs: risk category, probability/confidence, affected assets/areas/staff, source evidence, recommended action, owner, due date, and exportable report.
- Define safety: AI output is decision support, not legal/clinical certainty; every claim needs source evidence links.
- Acceptance criteria: AI cannot use seed/demo data, tenant isolation is enforced, reports cite source records, and managers can convert recommendations into tasks.

Mandatory expansion for `Phase 15: SaaS Packaging`:

- Treat Phase 15 as the short index only. Implementation detail lives in Phase 15A, 15B, 15C, and production tenant/billing phases.
- Before coding Phase 15 items, create decision records for tenant isolation, billing provider, subscription state, deployment strategy, storage isolation, feature gates, and support model.
- Acceptance criteria: feature gates are server-side, tenant data is isolated, billing state drives entitlements, and downgrade/cancellation/export rules are implemented before paid launch.

Mandatory expansion for `Phase 17: Website And Launch`:

- Phase 17 may execute only after Phase 16I truth-control, Phase 16G support, Phase 16H sales/demo/trial, Phase 15B tier matrix, Phase 15C billing, and legal review are done.
- Website pages must be built from the Content Claim Register and Pricing Truth Table, not improvised marketing text.
- Demo request must create a lead/pipeline item, not a production tenant.
- Acceptance criteria: public claims match implemented features, pricing matches tier matrix, demo flow works, legal pages are linked, analytics are privacy-reviewed, and screenshots show current approved demo data.

Mandatory expansion for `Phase 18: Final Release Gate`:

- Each audit must produce a written artifact stored in the release folder: source audit, route audit, permission audit, database source-of-truth audit, desktop verification, mobile verification, PDF evidence verification, AI import test, tenant separation test, Azure/staging test, and launch checklist.
- Release cannot proceed with known seed fallback, stale route, placeholder page, missing PDF, unverified permission, tenant leak, dirty worktree, uncommitted migration, failing build, or broken stable URL.
- Acceptance criteria: all release artifacts exist, build passes, browser verification passes, mobile verification passes, source tree is clean except intentional release files, release is committed, pushed, tagged, and documented.

## Execution Roadmap

### Phase 1: Workspace And Data Hygiene

1. Run full git/worktree audit and separate source files from runtime, generated, and database files.
2. Update `.gitignore` so SQLite files, backups, logs, `bin/`, `obj/`, artifacts, uploads, and runtime clutter do not appear in git status.
3. Remove tracked generated files from git index without deleting local runtime files.
4. Create a clean source checkpoint commit for hygiene only.
5. Stop all normal startup/sample seed mutation.
6. Remove schema/data repair calls from normal page requests.
7. Preserve current login accounts, but remove stale seeded company, register, checklist, schematic-assignment, and readiness artifacts from the active dev database.
8. Verify the app starts cleanly without recreating old seed data.

### Phase 2: Company And Tenant Source Of Truth

9. Make Master Setup the only source of company name, logo, workspace settings, and branding.
10. Remove all X Med or seed branding restoration paths.
11. Keep AcuityOps branding on pre-company-login screens.
12. Make uploaded logos persist and render across all logged-in pages.
13. Add remove-logo functionality.
14. Verify company name and logo changes survive restart and reload.
15. Confirm company data is scoped by tenant/company ID everywhere.

### Phase 2A: New Client Setup Wizard

The app must include a guided setup wizard for new client onboarding. This wizard is required before a new client is expected to operate inside the system. It must take the company owner or appointed senior manager from first workspace setup through the minimum usable operational configuration.

15A. Add a setup wizard entry point after company-level authentication and before normal app use when the company setup is incomplete.
15B. Make the wizard resumable. A client must be able to leave and return without losing completed setup steps.
15C. Store wizard progress against the company record, not browser state only.
15D. Show a clear setup progress indicator with completed, current, and remaining steps.
15E. Step 1 must capture company identity: company name, trading name if applicable, logo, contact email, contact phone, country, region, and default timezone.
15F. Step 2 must capture operational structure: bases, regions, operational areas, storage spaces, and whether areas are nested under regions or bases.
15G. Step 3 must capture vehicle structure: vehicle functions such as Ambulance or Response Vehicle, client-defined subtypes such as Operational Ambulance or IFT Ambulance, and optional default schematic assignment by function or subtype.
15H. Step 4 must capture staff structure: clinical qualification/scope options, staff ID format, practitioner number requirement, licensing expiry requirement, CPD tracking requirement, and default staff profile fields.
15I. Step 5 must capture access model defaults: company owner, senior manager permissions, operational manager scope behavior, staff permissions, and whether operational managers can draft checklist changes, edit registers, approve stock, or only request changes.
15J. Step 6 must capture asset register setup choices: vehicles, equipment, stock, medication, staff, storage locations, and whether the client will manually build registers now or import them later.
15K. Step 7 must capture checklist setup choices: build from blank, use a starter structure, import existing checklist later, publish daily check by function/subtype/callsign, and whether Full Audit checklists will be configured now or later.
15L. Step 8 must capture readiness engine setup choices: use default AcuityOps scoring, customize scoring later, require senior approval for scoring changes, and activate or defer readiness scoring.
15M. Step 9 must show a setup review screen before completion. The review must list missing required items, optional deferred items, and what the company can do immediately after setup.
15N. Completing the wizard must unlock the normal Home flow and direct the client to add staff, add/import registers, build/publish checklists, and start operating.
15O. Incomplete setup must not create fake seed data. It must show empty states and clear next actions.
15P. The wizard must be accessible later from Master Setup as `Setup Wizard / Setup Progress` so senior users can review or complete deferred setup items.
15Q. Setup wizard completion, skipped optional steps, and later setup edits must be audit logged.
15R. Verification must include a clean company with no seed data progressing through the wizard until the app is ready for staff creation and operational use.

### Phase 2B: Environment Stabilization And Cloud Staging Gate

This phase exists because local-only development is now blocking efficient progress. The current Windows + SQLite + OneDrive + manual app process workflow is acceptable for early prototyping, but it is not acceptable as the only verification environment for a multi-tenant SaaS product. Phase 2B must create a stable staging path where committed source can be built, deployed, migrated, logged, and verified without depending on the founder's active desktop session.

Phase 2B is not full production launch. It is a controlled staging environment for safer, faster verification while product development continues. Production security, billing, support, public launch, compliance, and Enterprise hardening remain governed by later phases.

Target staging architecture:

- `Source of truth`: GitHub repository and protected working branch. Local code is a working copy only; staging deploys from committed source.
- `CI build verification`: GitHub Actions must restore dependencies, build the ASP.NET app, run available tests, and fail before deployment if source does not compile.
- `Staging app host`: Azure App Service or Azure Container Apps. Initial decision must favour the simplest reliable host for an ASP.NET prototype, not premature Enterprise complexity.
- `Staging database`: managed Azure SQL Database or Azure PostgreSQL Flexible Server. SQLite must not be the staging source of truth.
- `Staging file storage`: Azure Blob Storage with tenant-aware container/path strategy for logos, uploads, generated reports, PDFs, and future import files.
- `Secrets`: Azure Key Vault for database connection strings, storage connection strings, application secrets, provider keys, and future AI/SMS/email/billing secrets.
- `Configuration`: environment-specific configuration for `Local`, `Staging`, and later `Production`. Local configuration must never be copied blindly into staging.
- `Observability`: Application Insights plus Log Analytics for staging errors, request failures, dependency failures, startup failures, deployment failures, and migration failures.
- `Deployment flow`: GitHub commit -> CI build -> staging deploy -> migration step -> smoke verification -> tracker evidence.
- `Database migrations`: EF migrations or approved migration scripts must run in a controlled staging step. Migrations must be backed by rollback/restore instructions before use.
- `Rollback`: staging deployment must be able to roll back to a previous commit and restore the previous staging database backup when a migration breaks the app.
- `Local responsibility`: local remains for quick implementation, targeted build checks, and controlled experiments.
- `Staging responsibility`: staging is the first shared verification truth for login, setup wizard, tenant isolation, uploads, checklist loading, reports, and app-start behavior.
- `Production responsibility`: production is not created by this phase. Production requires later security, billing, support, legal, observability, offboarding, and release gates.
- `Cost control`: every cloud resource must have an owner, purpose, tier/SKU, monthly estimate, budget alert, shutdown/deletion rule, and reason for existence. No resource may be created without a cost note.

Cost-control rules:

- Start with one staging environment only.
- Do not create production, demo, trial, or Enterprise environments in this phase.
- Do not enable paid add-ons, autoscale, premium monitoring, SMS, email, AI, billing, CDN, WAF, or SIEM unless explicitly authorized in a later phase.
- Prefer low-cost staging SKUs until the app needs a higher tier for verification.
- Add Azure budget alerts before or during first resource provisioning.
- Record every created Azure resource in a staging resource inventory.
- Delete failed experiment resources instead of leaving them running.

Acceptance criteria:

- A clean commit can be pushed to GitHub and built by CI.
- The staging app can start from committed source without relying on local desktop process state.
- The staging app uses a managed database, not the active local SQLite file.
- The staging app uses configured storage for uploads/logos and does not write tenant files into source folders.
- Secrets are stored outside source control.
- App startup, company login, role login, Home, Setup Wizard, Master Setup branding, Checklist Register empty state, and Daily Vehicle Check no-assigned-checklist state can be smoke verified on the staging URL.
- A failed build blocks deployment.
- A failed migration or broken staging deploy has a documented rollback path.
- Cloud spend is visible and bounded.

15S. Confirm GitHub source-of-truth readiness: remote, branch strategy, pushed commits, ignored runtime files, and clean working tree rules.
15T. Add GitHub Actions CI build verification for restore, build, and available tests.
15U. Create the staging architecture decision record covering Azure host, managed database, blob storage, Key Vault, Application Insights, Log Analytics, environment names, and cost-control choices.
15V. Provision the minimal Azure staging resource set after the decision record is approved: resource group, app host, managed database, blob storage, Key Vault, Application Insights/Log Analytics, and budget alert.
15W. Configure staging app settings and secrets without committing secrets to source.
15X. Define and verify staging database migration, backup, restore, and rollback process.
15Y. Deploy the current committed app to staging and run the staging smoke suite.
15Z. Record local-vs-staging responsibilities, stable staging URL, troubleshooting path, and rules for when verification must happen on staging instead of localhost.

### Phase 3: Access And Permissions

16. Initialize saved access permissions for current login users.
17. Restore granular permission controls under Access Setup.
18. Make Access Setup select existing staff from Staff Register, not manual person entry.
19. Enforce saved permissions on actual page actions and post handlers.
20. Verify staff, operational manager, senior manager, and owner behavior separately.
21. Verify operational managers only see assigned areas, staff, issues, reports, and assets.
22. Verify senior managers see company-wide records.
23. Verify staff cannot edit or delete manager-level records unless specifically authorized.

### Phase 4: Navigation And Route Cleanup

24. Remove or redirect legacy checklist routes: `/DailyChecklist`, `/DailyEquipmentChecklist`, and `/EditEquipmentChecklist`.
25. Remove duplicate Manager Areas route and keep only Area / Manager Control.
26. Remove duplicate Home module pathways.
27. Clean Page Index so it works broad-to-narrow across all pages.
28. Remove stale task action links and aliases pointing to old routes.
29. Remove user-facing Monthly naming and replace it with Full Audit everywhere.
30. Verify all navigation paths land on current pages only.

### Phase 5: Checklist Source Of Truth

31. Make Checklist Register the only source of truth for live daily checks.
32. Remove fixed-form and fallback daily checklist rendering completely.
33. Make live daily checks show `No assigned checklist available` when no active published checklist exists.
34. Make Build New Checklist start blank unless a template is explicitly selected.
35. Remove automatic starter rows, columns, sections, and equipment items.
36. Keep optional blank sections only if empty and user-editable.
37. Make checklist delete/retire remove active scopes so retired templates cannot load.
38. Add audit logs for checklist create, edit, publish, replace, retire/delete, approval, and rejection.

### Phase 6: Checklist Builder And Publishing

39. Finish builder matrix: sections, rows, columns, items, subitems, notes, and schematic fields.
40. Add section types: Vehicle, Equipment, Stock, Medication, and Custom.
41. Make custom section name editable only when Custom is selected.
42. Replace typed data-source text with real source selectors.
43. Publish scopes must support all areas, specific area, vehicle function, vehicle subtype, and specific callsign/vehicle.
44. Publish screen must show where the checklist is currently active.
45. Add replacement warning before publishing over an active target.
46. Operational managers submit checklist changes for senior approval unless explicitly delegated publish permission.
47. Senior managers can approve, reject, send back, or publish.

### Phase 7: Live Daily Checks

48. Daily check starts with registration dropdown and auto-populated callsign.
49. Selecting a vehicle loads only the active published checklist assigned to that vehicle.
50. Same-as-previous-shift appears only after a checklist loads.
51. No checklist means no checklist fields render.
52. Schematic sections pull only assigned schematic library records.
53. Schematic view tabs switch both image and matching response fields.
54. Damage marking must work with mouse, finger, and device pen.
55. Saving a check creates a fresh new check and stores the completed one as evidence.

### Phase 8: PDF Evidence

56. Implement real PDF generation for every submitted checklist.
57. PDF must include company, user, role, date/time, area, vehicle, callsign, checklist version, responses, notes, issues, schematic markings, and status.
58. Add PDF download from submitted checklist report view.
59. Verify PDF opens correctly and contains the actual submitted checklist data.
60. Ensure PDF generation works for daily checks and Full Audit checks.
61. Audit log every PDF generation and download event.

### Phase 9: Registers And Assets

62. Vehicle Register grouped by Function, then Subtype, collapsed by default.
63. Vehicle edit updates registration, callsign, function, subtype, schematic, service, license, and dependent views.
64. Equipment, stock, and medication registers use grouped collapsed list views with search, open, edit, move, issue, and delete.
65. Staff Register grouped by clinical qualification/scope, not role title.
66. Staff profile includes practitioner number, annual license expiry, CPD status, and CPD expiry.
67. Staff can edit allowed personal fields.
68. Managers can edit register-controlled staff fields within scope.
69. All asset movement destinations include vehicles, bases, stores, and operational areas.
70. Apply service, expiry, license, and CPD date coloring: over 60 days green, 60 days amber, 30 days orange, 15 days dark orange, overdue red.

### Phase 10: Stock And Medication Flow

71. Consolidate stock order, supplier confirmation, stock entry, and stock allocation behind Stock Orders & Distribution.
72. Remove duplicate stock-order pathways.
73. Stock Register grouped by item name and optionally location.
74. Medication Register grouped by medication name and optionally location.
75. Add edit/delete/open/move/issue parity across stock and medication.
76. Verify quantities, batches, expiry, location, and vehicle allocation update correctly.

### Phase 11: Schematics

77. Keep schematic images as product assets, not seed data.
78. Schematic Library grouped by Ambulance and Response Vehicle, then Make and Model.
79. Display collage in library only.
80. Use four separate images in checklist view: Left, Right, Front, Rear.
81. Remove Top view.
82. Assignment supports function, subtype, area, and specific vehicle/callsign.
83. Add unassign support.
84. Verify no schematic shows as assigned unless a real assignment exists.

### Phase 11A: Global Vehicle Schematic Library Completion

The vehicle schematic library is a required product asset library. It currently contains only the initial real schematic entries. It must become a broad global EMS vehicle schematic library covering common ambulance and response-vehicle makes/models used in South Africa, Africa, the UK, Europe, Australia, New Zealand, North America, and other target markets. This work will consume meaningful image-generation and review credits, so it must be scheduled as controlled asset-production sprints after the core schematic system is stable.

Timing rule:

- Do not spend major schematic-generation credits until Phase 11 is complete: assignment, unassignment, library grouping, four-view checklist display, damage marking, mobile marking, and PDF/report export must already work.
- The first production asset sprint must happen before serious demo sales to private EMS operators, because schematics are a visible product differentiator.
- The complete global library can be built in market-priority batches after the South African and first export-market libraries are complete.
- New client onboarding must still work if a vehicle model is not yet in the library. The client must be able to use a generic schematic, request a schematic, or operate without a schematic until the asset is produced.

Required asset format:

- Every schematic model must have exactly four separate checklist images: `Left`, `Right`, `Front`, and `Rear`.
- No `Top` view is allowed in the live checklist workflow.
- Every schematic model must also have one library collage image used only for browsing the schematic library.
- The collage must never be used as the live checklist marking image.
- Each view must be clean black/grey line art on transparent or clean white background, large enough for mobile marking, without cropped fragments, watermarks, third-party logos, photographic backgrounds, badges, or decorative branding.
- Emergency variants must show operationally relevant features such as roof emergency light bars, canopy/box body, high roof, rear doors, side door, side windows, roller doors, bullbar, canopy, or patient compartment where relevant.
- The schematic must prioritize practical damage marking over artistic detail.

Required metadata per schematic:

- Function category: `Ambulance`, `Response Vehicle`, or a support category added only after this spec is updated and approved.
- Make.
- Model.
- Body style.
- Model years or generation where relevant.
- Region aliases and naming differences, for example `Toyota Quantum`, `Toyota HiAce`, and `HiAce H300`.
- Vehicle subtype suggestions, for example `Operational Ambulance`, `IFT Ambulance`, `ICU Ambulance`, `ALS Response Vehicle`, `Supervisor Vehicle`, or `Rescue Response Vehicle`.
- Source/reference notes used to guide the schematic.
- Asset version.
- Review status.
- Approved date.
- Retired date if replaced.
- Legal/IP review status.

Priority build order:

1. South African Response Vehicle library.
2. South African Ambulance library.
3. Africa regional additions.
4. UK and Ireland EMS vehicles.
5. Australia and New Zealand EMS vehicles.
6. North American EMS vehicles.
7. Europe and other export markets.

Initial South African response-vehicle backlog:

- Toyota Hilux single cab, extra/super cab, and double cab.
- Ford Ranger single cab, super cab, and double cab.
- Isuzu D-Max single cab, extended cab, and double cab.
- Nissan Navara double cab.
- Volkswagen Amarok double cab.
- Toyota Fortuner.
- Toyota Land Cruiser 70-series.
- Toyota Land Cruiser Prado.
- Mahindra Pik Up single cab and double cab.
- Mitsubishi Triton.
- Renault Duster.
- Suzuki Jimny.
- Suzuki Grand Vitara.
- Hyundai H1/Staria where used as response/support vehicles.

Initial South African ambulance backlog:

- Toyota Quantum / Toyota HiAce high roof ambulance.
- Mercedes-Benz Sprinter ambulance.
- Ford Transit ambulance.
- Volkswagen Crafter ambulance.
- Iveco Daily ambulance.
- Toyota Land Cruiser ambulance conversion.
- Isuzu light-truck ambulance conversion where locally used.
- Nissan/UD light-truck ambulance conversion where locally used.

Credit-control and production workflow:

- Create schematic assets in batches, not one-off scattered generation.
- Each batch must have a written target list, reference links, expected body styles, and acceptance criteria before image generation starts.
- Each generated vehicle must be reviewed before being added to the app.
- A vehicle is approved only when all four live views and the collage pass visual review.
- Failed, draft, or experimental images must not be committed into product assets.
- Do not generate a full global library until the South African library and at least one international market library prove the workflow.

Quality acceptance rules:

- The vehicle silhouette must clearly resemble the named make/model/body style.
- The views must be consistent with each other.
- Wheels, roofline, doors, windows, lights, rear, and front must not have missing chunks.
- Images must not be cropped from a collage in a way that leaves fragments of other views.
- Images must be usable on mobile for finger/pen damage marking.
- The checklist marking canvas must save and export marks correctly on every view.
- The generated PDF must show the selected view marks clearly.

Legal and IP rules:

- The app must not embed copyrighted photographs, scraped images, watermarked assets, third-party badges, or client branding in product schematics.
- Reference images may guide proportions and body style, but the final product asset must be an original clean schematic suitable for commercial use.
- The legal/IP review phase must confirm the asset ownership and acceptable commercial use of generated schematic assets before launch.

84A. Freeze the schematic asset specification before large-scale generation begins.
84B. Create a master vehicle schematic backlog grouped by function, region, make, model, body style, and priority.
84C. Build South African response vehicles first because the initial target market is South Africa/Africa.
84D. Build South African ambulance models second.
84E. Build global additions by sales priority and client demand.
84F. Create a production asset workflow for references, generation, review, approval, commit, and release.
84G. Generate every model as four separate checklist views plus one library collage.
84H. Store product schematics as versioned product assets, not seed data and not client data.
84I. Store schematic assignments as client/company data only.
84J. Add no automatic assignment from the global library unless the client or setup wizard explicitly selects it.
84K. Add request workflow for clients to request a missing vehicle schematic.
84L. Add generic fallback schematic options only if clearly labelled generic and never presented as a make/model-specific asset.
84M. Verify every schematic on desktop and mobile before product release.
84N. Verify every schematic can receive, save, reload, and export damage marks.
84O. Verify schematic assets appear in the library grouped by function, make, and model.
84P. Verify the live checklist uses only the four separate view images, never the collage.
84Q. Verify schematic PDFs render cleanly and include marks.
84R. Add asset review checklist: silhouette accuracy, view consistency, no missing body parts, no fragments, no watermarks, no logos, no cropped collage artifacts, and mobile usability.
84S. Add legal/IP review status to every schematic asset before public launch.
84T. Add a release note whenever new schematic assets are added.
84U. Add a client-facing label that explains if a schematic is exact, approximate/generic, or pending custom production.
84V. Add internal asset-production notes so failed/draft images are not accidentally shipped.
84W. Do not spend credits on full global generation until Phase 11 is verified and South African priority vehicles are approved.
84X. Schedule global library generation as planned asset sprints, separate from functional cleanup and bug-fix work.
84Y. During Phase 15B tier-matrix review, decide whether priority custom schematic requests are included, add-on, Enterprise-only, or unavailable; record the decision in the tier matrix before public pricing is published.
84Z. Treat the completed schematic library as a competitive product asset and maintain it as part of the release process.

### Phase 12: Readiness And Reports

85. Readiness Engine uses active rules only.
86. Default rules are created only through explicit setup.
87. Operational managers can request scoring changes; senior managers approve or reject.
88. Readiness Dashboard uses active rules, not hardcoded assumptions.
89. Metric tiles drill into concise list views.
90. Operational Reports tiles are clickable and show source, submitter, callsign, and action status.
91. Checklist Reports are list-based, searchable, grouped by date, area, and callsign.
92. Variance Alerts are list-based, grouped by date and callsign.
93. Operational manager reports respect assigned area; senior reports are company-wide.

### Phase 12B: South African EMS Audit Compliance Mode

South African EMS operators must be able to prepare for annual Department of Health and provincial EMS audits in a dedicated compliance environment. This must not be a loose report page. It must be a selectable mode that turns AcuityOps into an audit-readiness workspace with clear requirements, evidence status, gaps, owner assignment, due dates, and exportable proof packs.

Regulatory approach:

- The product must not assume one permanent national audit checklist. South African EMS audit requirements may involve national health legislation, provincial Department of Health processes, HPCSA staff registration/CPD requirements, vehicle/road/legal documents, medicine/stock controls, infection control, clinical governance, base readiness, and local inspector expectations.
- The platform must therefore support versioned `Audit Requirement Packs` by country, province, regulator, year, service type, client type, and subscription tier.
- A lawyer, EMS regulatory advisor, and real operator feedback must validate every published audit pack before it is made available as a product default.
- The mode must clearly state that it is compliance-support and audit-preparation software, not a legal guarantee that a client will pass an audit.

Primary source areas to validate before implementation:

- National Health Act and applicable health-service regulations.
- Provincial Department of Health EMS licensing/inspection requirements.
- HPCSA practitioner registration, practitioner numbers, annual registration/licensing, CPD, scope of practice, and professional-board requirements.
- POPIA and data-retention requirements for staff records, patient-related operational records where applicable, uploaded documents, audit logs, and compliance evidence.
- Electronic Communications and Transactions Act requirements relevant to electronic records, digital evidence, exported PDFs, and audit logs.
- Roadworthiness, vehicle licensing, professional driver permits, insurance, oxygen/fire/safety rules, medical waste, medicine storage, and controlled medicine obligations where applicable.

Common operator difficulty themes to design for:

- Audit requirements are spread across staff files, vehicle files, equipment files, medication/stock files, base documents, incident records, and paper checklists.
- Managers often know something exists but cannot quickly prove it during inspection.
- Expired staff annual registration, CPD gaps, vehicle license discs, equipment service dates, oxygen/medical equipment checks, medication expiries, and document renewals are easy to miss.
- Evidence is often stored in WhatsApp, email, paper folders, spreadsheets, loose PDFs, or one person's laptop.
- Provincial interpretation and inspector expectations can differ, so the app must allow client-specific checklist additions without breaking the standard pack.
- Private EMS services need a clean way to show operational control: who is responsible, what is compliant, what is not compliant, what action was taken, and what proof exists.
- Companies need audit preparation months before renewal, not only after a failed inspection.

Required architecture:

- `AuditRequirementPack`: country, province, regulator, audit year, service category, version, source references, reviewer, effective date, retired date, and approval status.
- `AuditRequirement`: heading, plain-English requirement, regulatory reference, evidence required, severity, renewal frequency, responsible role, applicable asset/staff/service type, and verification method.
- `AuditEvidenceMap`: links each requirement to existing AcuityOps evidence sources such as staff profiles, CPD/licensing dates, vehicle register, equipment register, medication register, stock register, checklist reports, issue reports, readiness dashboards, task records, asset movements, uploaded documents, audit logs, and generated PDFs.
- `AuditGap`: missing evidence, expired evidence, unverified evidence, failed requirement, owner, due date, action status, escalation status, and resolution notes.
- `AuditPackExport`: export bundle containing requirement list, compliance status, evidence links, uploaded proof, generated PDFs, date status, responsible manager, and final review notes.
- `AuditModeSession`: selected pack, selected area/base, selected vehicle function/subtype, date range, current compliance score, open gaps, and export history.

User experience:

- Senior managers must access `EMS Audit Compliance Mode` from Home and from Operational Reports.
- Operational managers may access only the audit areas assigned to them unless senior management grants wider permissions.
- Staff may see only requested evidence tasks or personal compliance items assigned to them.
- Entering the mode must present a clean compliance dashboard: overall readiness, critical gaps, expiring items, missing evidence, unverified uploads, assigned owners, and export status.
- Each requirement must show: what it means, why it matters, what evidence is accepted, where AcuityOps already found evidence, what is missing, who owns it, and the due date.
- The system must separate `auto-collected evidence` from `manual uploaded evidence`.
- Clients must be able to add local/custom requirements without editing the official product default pack.
- Every requirement must support `Compliant`, `Partially compliant`, `Not compliant`, `Not applicable`, and `Needs review`.
- Every status must require a visible reason or evidence link unless it is auto-calculated from current records.
- Compliance mode must generate tasks for missing evidence, expired documents, and unresolved gaps.
- Compliance mode must produce an exportable audit pack and summary PDF.

Initial South African audit categories to support:

- Company/service registration, Department of Health licence/renewal, business documents, insurance, service area, emergency contact details, clinical governance, medical director or responsible clinical lead where applicable, policies, SOPs, and escalation procedures.
- Staff: staff ID, national ID, role/access level, clinical qualification/scope, practitioner number, HPCSA registration/licence status, annual registration expiry, CPD status, CPD expiry, driver licence, professional driving permit where applicable, medical fitness, certificates, training records, employment files, and disciplinary/competency restrictions where lawful and relevant.
- Vehicles: registration, callsign, function, subtype, assigned area/base, schematic assignment, roadworthy status, licence disc expiry, service dates, insurance, communication equipment, daily check evidence, equipment allocation, medication/stock allocation, cleanliness/infection-control evidence, and unresolved defects.
- Equipment: item type, make/model, serial/asset ID, assigned vehicle/base/store, service date, calibration where applicable, operational status, battery status, unresolved issues, and maintenance evidence.
- Stock and consumables: item name, subtype/size, quantity, location, minimum stock level, expiry, batch number, and shortage/overstock status.
- Medication: medication name, strength/form where applicable, batch, expiry, quantity, location, controlled/non-controlled classification, storage evidence, allocation, and stock movement history.
- Bases/stores: operational area, physical storage location, vehicle allocation, equipment storage, medication storage, oxygen/storage controls where applicable, infection-control documents, waste handling evidence, and responsible manager.
- Operations: submitted daily checks, Full Audit checks, readiness score history, open issues, closed issues, task history, asset movements, stock orders, supplier confirmations, variance alerts, incident records, and operational reports.
- Evidence control: generated PDFs, uploaded files, audit logs, timestamps, user identity, template versions, export history, and proof of corrective action.

Current app gaps that must be built before this mode can work:

- No official/versioned EMS audit requirement pack model exists.
- No province/country/regulator audit pack selection exists.
- No compliance-mode dashboard exists.
- No requirement-to-evidence mapping engine exists.
- No audit-gap workbench exists.
- No audit evidence export bundle exists.
- No regulator-friendly audit summary PDF exists.
- No legal/regulatory review workflow exists for publishing audit packs.
- No client-specific custom audit requirement layer exists.
- No audit pack versioning, retirement, or change log exists.
- Staff profile fields exist or are planned, but full evidence mapping for HPCSA registration, practitioner number, annual licensing, CPD status, driver licence, PDP, medical fitness, and training records must still be verified.
- Vehicle, equipment, stock, and medication registers need complete date/status/evidence metadata before audit scoring can be accurate.
- Access permissions must be enforced before audit evidence can be trusted.
- PDF generation must be real and verified before audit pack export can be trusted.
- Uploaded documents need document category, expiry date, owner, evidence type, verification status, and audit-use tags.
- AI import must be able to map client spreadsheets into audit-relevant evidence fields without creating unverified compliance claims.

93AA. Create the `EMS Audit Compliance Mode` navigation entry for senior managers and scoped operational managers.
93AB. Create the audit domain model: requirement packs, requirements, evidence maps, gaps, audit sessions, exports, and review history.
93AC. Create a South Africa audit pack framework, but do not publish official defaults until validated by a legal/regulatory advisor and at least one real EMS operator.
93AD. Add province and regulator fields so audit packs can differ by province and by audit year.
93AE. Add a requirement library with plain-English requirement text, regulatory/source reference, evidence accepted, severity, renewal frequency, owner role, and applicable record type.
93AF. Add an evidence mapping engine that can pull evidence from staff, vehicles, equipment, stock, medication, bases, checklists, readiness, issues, tasks, movements, uploads, and audit logs.
93AG. Add automatic gap detection for missing evidence, expired evidence, invalid status, unresolved hard blocker, and unverified upload.
93AH. Add manual evidence upload and manual evidence linking for records that are not generated by AcuityOps.
93AI. Add requirement status workflow: compliant, partially compliant, not compliant, not applicable, and needs review.
93AJ. Add task generation from audit gaps, with owner, due date, escalation, and completion evidence.
93AK. Add compliance score only as a support indicator. It must not claim legal pass/fail unless a regulator-approved audit rule explicitly supports that outcome.
93AL. Add audit dashboard filters for area/base, vehicle function, vehicle subtype, callsign, staff qualification, asset type, requirement category, gap severity, evidence status, and due date.
93AM. Add a clean audit workspace that hides normal operational clutter and shows only compliance requirements, evidence, gaps, tasks, and exports.
93AN. Add client-specific custom requirements layered above the official pack without modifying the official pack.
93AO. Add official pack versioning, retirement, source notes, reviewer, approval date, and change log.
93AP. Add audit export bundle with summary PDF, requirement list, evidence links, uploaded documents, generated PDFs, gap list, corrective actions, and export timestamp.
93AQ. Add audit export history and audit log entries for every export and evidence-status change.
93AR. Add warnings where a client marks something compliant without evidence.
93AS. Add warnings where evidence exists but is expired, outside the selected date range, assigned to the wrong area, or linked to the wrong vehicle/staff member.
93AT. Add audit-preparation timeline: 12 months, 6 months, 3 months, 60 days, 30 days, 15 days, overdue.
93AU. Add AI-assisted compliance analysis later: AI may summarize gaps and forecast future failure risk, but it must not generate official legal compliance conclusions without human review.
93AV. Verify with a simulated audit pack that the mode can identify missing staff CPD, expired licence disc, expired equipment service, medication expiry, missing document upload, unresolved issue, and missing checklist evidence.
93AW. Verify that operational managers see only their assigned audit scope and senior managers see company-wide audit status.
93AX. Verify that exported audit packs contain only the selected tenant and selected audit scope.
93AY. Add this mode to Pro/Premium tier planning, with Base support limited to basic expiry/register evidence unless commercial strategy later changes.
93AZ. No South African EMS audit pack may be labelled official, complete, or regulator-approved until the legal/regulatory review step signs it off.

### Phase 12C: South African Regulatory Source Pack And Multi-Country Compliance Architecture

The South African EMS audit mode must be source-backed. It must not be built from memory, assumptions, generic EMS knowledge, or a single unofficial checklist. Every compliance requirement exposed to a client must trace back to a stored source record, clause, page, effective date, jurisdiction, reviewer, and implementation mapping.

This phase exists to prevent guessing. It also establishes the architecture required to add future country packs without hardcoding South Africa into the product.

Verified source anchors already identified:

- `National Health Act 61 of 2003`, official South African Government PDF: health system framework, emergency treatment context, discharge reports, health record creation, confidentiality, health record access, health record protection, complaints, private health establishment insurance, quality requirements, equipment/hygiene/premises/service delivery standards, and Office of Standards Compliance inspection/corrective-action concepts.
- `Protection of Personal Information Act 4 of 2013`, official South African Government PDF: lawful processing, accountability, minimality, purpose specification, retention, openness, security safeguards, breach notification, special personal information including health data, data subject access/correction, and transborder information flows.
- `SAHPRA official source material`: SAHPRA regulates health products, medicines, scheduled substances, medical devices, licensing of manufacturers/wholesalers/distributors, inspections, compliance, safety, efficacy, quality, and relevant medicines/device registers under the Medicines and Related Substances Act and related legislation.
- `HPCSA / Health Professions Act source pack`: required but not yet attached as a verified clause pack in this repository. Implementation must collect official HPCSA/Health Professions Act sources for emergency care registration categories, protected titles, practitioner registration, annual registration, CPD, scope, and practitioner-number verification before those rules are activated.
- `National Road Traffic / provincial traffic source pack`: required but not yet attached as a verified clause pack. Implementation must collect official roadworthiness, licence disc, PrDP, vehicle registration, and insurance evidence rules before activating those requirements.
- `Provincial Department of Health EMS audit/licensing packs`: required per province. No province-specific pack may be activated from generic assumptions.
- `Municipal/local bylaw packs`: required only where the client jurisdiction or service model needs them.

Non-negotiable source rule:

- If a requirement cannot point to a verified `RegulatorySource` and `RegulatoryClause`, the app may show it only as `Client custom requirement` or `Internal best-practice requirement`. It must not display it as a Department of Health, provincial, statutory, or regulator requirement.
- AI may extract, classify, and propose requirements from source documents, but AI output is `Draft` until approved by a human reviewer and, where required, a legal/regulatory reviewer.
- Compliance mode is operational evidence and audit-preparation software. It must never state that AcuityOps guarantees audit pass, licence renewal, legal compliance, regulator approval, or professional indemnity protection.

AI/Codex source-pack compilation responsibility:

- The platform build process must not depend on the founder manually compiling regulatory packs. Codex/build-agent work must perform the first source discovery, source collection, source inventory, clause extraction, requirement drafting, and app-field mapping.
- The product architecture must include an `AI Source Pack Compiler` that can ingest public source URLs, uploaded Acts, regulations, bylaws, audit forms, PDFs, Word documents, regulator guidance, and client-specific policies.
- The AI compiler must produce draft source records, draft clauses, draft compliance requirements, draft evidence mappings, and a list of missing source areas.
- The founder/client must review strategy and commercial fit, but must not be the manual data-entry mechanism for laws, bylaws, regulator clauses, or audit requirements.
- A lawyer, regulatory advisor, or nominated expert must verify and approve the compiled source pack before official compliance rules become active.
- If the AI compiler cannot locate or extract an authoritative source, it must create a visible `Source needed` task with jurisdiction, regulator, source type, likely requirement domain, and suggested search targets.
- The system must preserve every source URL, uploaded file, extracted clause, AI summary, human correction, approval decision, and activation state so the compliance pack can be defended later.

Jurisdiction architecture:

- `Jurisdiction`: country, province/state, district/municipality, regulator region, active date range, and retired date.
- `Regulator`: name, jurisdiction, regulator type, contact URL, source URL, and regulator notes.
- `RegulatorySource`: source title, source type, issuing authority, jurisdiction, source URL, uploaded source file path, version/date, effective date, review date, retired date, verification status, reviewer, legal review status, and source hash.
- `RegulatoryClause`: source ID, clause/section/page, exact clause reference, short summary, raw extracted text, normalized requirement text, interpretation notes, evidence implication, and confidence/review status.
- `ComplianceRequirement`: plain-English requirement, regulatory clause link, requirement domain, severity, renewal frequency, responsible role, applicable tenant tier, applicable service type, applicable asset/staff/vehicle type, and verification method.
- `ComplianceEvidenceType`: accepted proof type, required metadata, expiry rules, source system, file requirements, and whether the evidence can be auto-collected.
- `ComplianceEvidenceRecord`: tenant ID, requirement ID, linked record type, linked record ID, uploaded document ID, evidence date, expiry date, verification status, verified by, timestamp, source of evidence, and audit log ID.
- `ComplianceGap`: requirement ID, gap type, affected person/asset/location, severity, owner, due date, escalation state, task link, resolution evidence, and closure notes.
- `ComplianceAuditPack`: selected jurisdiction, source pack version, selected scope, generated evidence bundle, generated PDF, export timestamp, generated by, and immutable export hash.
- `ComplianceRequirementMapping`: maps each requirement to existing AcuityOps domains: staff, vehicles, equipment, stock, medication, bases, storage, documents, checklists, readiness, reports, tasks, issues, audit logs, SOPs, notifications, billing, and tenant configuration.

South African source-pack domains to build:

- `Company and operating authority`: company registration, provider/service registration where applicable, Department of Health licence/renewal evidence, insurance, operating areas, responsible persons, governance documents, emergency contacts, and proof of service scope.
- `Staff and professional registration`: staff identity, clinical qualification/scope, practitioner number, HPCSA registration category, annual registration/licence status, CPD compliance, CPD expiry/review date, driver licence, PrDP where applicable, medical fitness, certificates, employment files, training, and competency restrictions where lawful.
- `Vehicles`: registration, callsign, function, subtype, area/base assignment, roadworthy status, licence disc expiry, service history, insurance, communications, daily check evidence, schematic damage evidence, unresolved defects, equipment allocation, stock allocation, and medication allocation.
- `Equipment`: item type, make/model, serial/asset ID, assigned vehicle/base/storage location, service/calibration, battery status, operational status, issue history, cleaning/maintenance evidence, and proof of corrective action.
- `Stock and consumables`: item name, subtype/size, quantity, location, minimum stock threshold, batch, expiry, shortage/overstock status, movement history, supplier evidence, and allocation.
- `Medication`: medication name, strength/form, schedule/controlled status where applicable, quantity, batch, expiry, storage location, vehicle/base allocation, movement history, supplier/receipt evidence, wastage/disposal evidence, and responsible person.
- `Base and storage`: base/area records, medication storage, equipment storage, stock storage, oxygen/storage controls where applicable, infection control, waste handling, cleaning records, access control, responsible manager, and inspection evidence.
- `Clinical and operational governance`: daily checks, Full Audit checks, incident/issue reports, corrective tasks, readiness history, audit logs, SOPs, complaints, escalation records, generated PDFs, uploaded proof, and export history.
- `Data protection`: POPIA processing basis, access control, audit logs, health/special-personal-information handling, retention, breach notifications, export/deletion, processor/operator agreements, cross-border processing, and tenant isolation.

South African province-pack rule:

- The South African compliance model must support separate packs for `Eastern Cape`, `Free State`, `Gauteng`, `KwaZulu-Natal`, `Limpopo`, `Mpumalanga`, `Northern Cape`, `North West`, and `Western Cape`.
- A province pack must be marked `Incomplete` until the exact provincial Department of Health EMS licence/audit source documents are uploaded, source-hashed, reviewed, and mapped.
- If a client selects a province without a verified pack, the app must show `Source pack incomplete` and allow only client custom requirements or general evidence preparation. It must not pretend to know the provincial audit.
- A company may operate across multiple provinces. The compliance workspace must support multi-pack views, conflicts, gaps by jurisdiction, and evidence that satisfies one province but not another.

Multi-country architecture:

- Every country must be implemented as a `CountryCompliancePack`, not as code branches scattered through pages.
- Country packs may inherit common evidence domains, but every local rule must have local source records.
- Future countries must support country, state/province, regional authority, local bylaw, regulator, source, clause, evidence, review, retirement, and legal sign-off in the same schema.
- AI source ingestion may be used to accelerate country-pack creation, but activation requires human review and legal/regulatory review.
- The UI must always show the active country/province pack, source version, last reviewed date, and whether the requirement is official, client custom, internal best practice, or draft.

93BA. Create `Jurisdiction`, `Regulator`, `RegulatorySource`, `RegulatoryClause`, `ComplianceRequirement`, `ComplianceEvidenceType`, `ComplianceEvidenceRecord`, `ComplianceGap`, `ComplianceAuditPack`, and `ComplianceRequirementMapping` models.
93BB. Add a source-pack ingestion workflow for Acts, regulations, bylaws, audit forms, regulator guidance, SOP requirements, and client custom requirements.
93BC. Add source metadata fields: issuing authority, jurisdiction, source type, source URL, uploaded file, source hash, effective date, review date, retired date, reviewer, legal review state, and product activation state.
93BD. Add clause-level capture so every requirement can cite source title, section, clause/page, raw extracted text, normalized plain-English meaning, and evidence implication.
93BE. Add a rule that official/statutory/provincial/regulator requirements cannot be activated unless linked to a verified source and reviewed.
93BF. Add the initial South African source registry using the National Health Act, POPIA, SAHPRA source material, HPCSA source-pack placeholder, road-traffic source-pack placeholder, provincial EMS source-pack placeholders, and municipal bylaw placeholders.
93BG. Add province-specific South African pack containers for all nine provinces.
93BH. Add `Source pack incomplete` UI state for provinces or domains where the exact source documents have not yet been verified.
93BI. Add source-backed mapping from National Health Act recordkeeping, confidentiality, access, protection, complaints, private establishment insurance, and quality/equipment/hygiene/service-delivery requirements into AcuityOps evidence domains.
93BJ. Add source-backed mapping from POPIA processing, security, breach notification, special personal information, data-subject access/correction, retention, and transborder flow requirements into tenant security, access, audit, export, and deletion architecture.
93BK. Add SAHPRA source-pack mapping for medicines, scheduled substances, medical devices, product registers, licensing, inspections, recalls, safety alerts, storage/evidence, and medicine/device compliance domains after exact clauses are attached.
93BL. Add HPCSA source-pack mapping for clinical qualification/scope, practitioner number, annual registration/licensing, CPD, professional-board registration, and protected title evidence after exact official sources are attached.
93BM. Add road-traffic source-pack mapping for vehicle licence disc, roadworthy, vehicle registration, PrDP, insurance, and operational vehicle legality after exact official sources are attached.
93BN. Add province Department of Health EMS audit-pack mapping after exact provincial sources are attached for each province.
93BO. Add municipal/local bylaw mapping only where sources are attached and relevant to the client jurisdiction.
93BP. Add AI document extraction support for source packs, but store AI output as draft clauses and draft requirements until reviewed.
93BQ. Add legal/regulatory review workflow for source packs, clause interpretation, requirement wording, and activation.
93BR. Add product-owner approval workflow after legal/regulatory review so reviewed rules are intentionally released.
93BS. Add source versioning and retirement so older audit packs remain reproducible for historical evidence but cannot be used for new compliance unless still active.
93BT. Add country-pack abstraction so South Africa is the first implementation, not a hardcoded product assumption.
93BU. Add UI indicators showing official source-backed, client custom, internal best practice, AI draft, legal-review pending, and retired requirement states.
93BV. Add tests proving no requirement can appear as official without a source, clause, review state, and active source pack.
93BW. Add tests proving a province with missing source documents shows `Source pack incomplete` rather than guessed requirements.
93BX. Add tests proving evidence maps can link requirements to staff, vehicle, equipment, stock, medication, document, checklist, issue, task, report, and audit-log records.
93BY. Add export metadata so audit packs include source title, source version, clause reference, evidence record, evidence timestamp, generated-by user, export timestamp, and tenant ID.
93BZ. Production release of South African EMS Audit Compliance Mode is blocked until source packs, legal review, evidence mapping, export PDFs, access permissions, and tenant isolation are verified.
93BZA. Add the `AI Source Pack Compiler` feature plan: source discovery, source ingestion, OCR/extraction, clause detection, requirement drafting, evidence mapping, missing-source detection, and review queue creation.
93BZB. Add Codex/build-agent workflow instructions requiring the build process to compile initial South African source packs rather than expecting the founder to manually assemble them.
93BZC. Add a `Source needed` queue for missing provincial, regulator, traffic, medicine, staff-registration, bylaw, and audit-form sources.
93BZD. Add tests proving AI-compiled requirements stay in draft state until reviewed and cannot appear as official requirements.
93BZE. Add tests proving source URL/file, raw extracted clause, AI summary, human correction, approval, activation, and retirement history are retained.

### Phase 12D: SMS And Email Notification Engine

SMS and email notifications are core platform functions. They must not be implemented as scattered one-off sends from individual pages. Notifications must use one central notification engine that supports in-app notification, email, and SMS channels with tenant-level configuration, user preferences, audit logging, retries, and provider abstraction.

Primary planned services and required platform functions:

- `Azure Communication Services SMS` for Azure-native SMS where the client country, sender type, regulatory requirements, and delivery needs are supported.
- `Azure Communication Services Email` for Azure-native transactional email, using verified sender domains and authenticated sending.
- `Azure Email Communication resource` connected to the Communication Services resource for email sending.
- `Azure Key Vault` for provider credentials, connection strings, signing keys, API keys, sender IDs, and webhook secrets.
- `Azure App Configuration` for per-environment and per-tenant notification flags, provider selection, channel enablement, and kill switches.
- `Azure Storage Queue`, `Azure Service Bus`, or equivalent queue for background notification dispatch so user-facing requests are not blocked by SMS/email delivery.
- `Azure Monitor` and `Application Insights` for delivery errors, queue failures, provider latency, and notification health.
- `Webhook receiver endpoints` for provider delivery receipts, bounces, opt-outs, complaints, and inbound SMS replies where supported.
- `Twilio Programmable Messaging` as an approved fallback SMS provider where Azure Communication Services country support or sender type is insufficient.
- `Infobip SMS` as an approved fallback SMS provider for broader international/African coverage where required.
- `Twilio SendGrid` as an approved fallback transactional email provider if Azure Communication Services Email does not meet deliverability, domain, analytics, or commercial needs.
- `AcuityOps Notification Provider Adapter` as an internal interface so the app can switch provider per tenant, country, channel, or subscription tier without rewriting product workflows.

93A. Add a central notification domain model: `NotificationEvent`, `NotificationRecipient`, `NotificationPreference`, `NotificationTemplate`, `NotificationDeliveryAttempt`, `NotificationProviderAccount`, and `NotificationSuppression`.
93B. Add a central notification service API used by all pages and services. Pages must raise notification events; they must not send SMS or email directly.
93C. Add a queued background dispatcher for email and SMS delivery.
93D. Add idempotency keys so the same task, issue, expiry alert, approval request, or readiness event cannot spam the same recipient repeatedly.
93E. Add retry rules with maximum attempts, backoff, failure reason, dead-letter status, and visible admin review.
93F. Add delivery status tracking for queued, sent, delivered, failed, bounced, suppressed, opted-out, and provider-rejected.
93G. Add tenant-level notification settings in Master Setup: enabled channels, default sender name, SMS provider, email provider, reply-to address, escalation windows, quiet hours, and emergency override behavior.
93H. Add provider setup screens for Azure Communication Services, Twilio, Infobip, and SendGrid where relevant. Provider credentials must never be stored in plain text database fields.
93I. Add notification templates for each event type. Templates must support tenant branding, role-specific wording, variables, preview, test send, and version history.
93J. Add required template variables for common events: client name, recipient name, role, area/base, callsign, registration number, asset name, issue title, task title, due date, expiry date, severity, readiness impact, action link, and sender name.
93K. Add user-level notification preferences: in-app, email, SMS, escalation-only SMS, quiet hours, and preferred contact details.
93L. Do not let user preference disable legally or operationally required critical notifications unless company policy explicitly allows it. Critical-notification behavior must be tenant-configurable and audit logged.
93M. Add contact validation for staff email and mobile number before enabling external notification delivery.
93N. Add opt-out and suppression handling for SMS and email where required by provider, region, and law.
93O. Add message content rules: no unnecessary patient data, no sensitive clinical details in SMS, short operational language, and links back to authenticated in-app records.
93P. Add notification triggers for task assigned, task feedback submitted, task overdue, issue assigned, issue escalated, issue resolved, readiness hard blocker, readiness score below threshold, vehicle not ready, equipment alert, checklist variance, checklist approval request, checklist rejected/sent back, checklist published, asset movement request, asset movement completed, stock order placed, supplier confirmation needed, stock entered, expiry/service pressure threshold, license expiry, CPD expiry, import mapping awaiting approval, AI report ready, and setup wizard incomplete.
93Q. Add notification recipient rules by event type. Examples: assigned user, assigned operational manager, senior managers, area managers, task creator, issue creator, checklist submitter, checklist approver, and company owner.
93R. Add escalation rules: notify assigned recipient first, then area manager, then senior manager after tenant-configured delay if unresolved.
93S. Add an in-app Notification Center showing all notification events, delivery status, failed deliveries, retries, and recipient history.
93T. Add notification badges/popups that are distinct from task and issue icons where the event is a system notification rather than a task/issue.
93U. Add an admin test mode that sends provider test messages to verified test contacts only.
93V. Add a production kill switch per provider and per notification category.
93W. Add billing/cost tracking hooks so SMS usage can be attributed by tenant and included in subscription or pass-through billing.
93X. Add audit logs for notification template changes, provider changes, preference changes, sends, delivery failures, suppressions, opt-outs, and escalation actions.
93Y. Verify SMS region support before activating SMS for a tenant. If the selected provider cannot reliably send in the tenant country, the setup screen must require a different provider or disable SMS with a clear explanation.
93Z. Verification must include one successful email, one successful SMS or provider-simulated SMS, one suppressed recipient, one failed delivery with retry, one escalation, one opt-out/suppression event, and one tenant-specific template preview.

### Phase 13: AI Import

94. Build AI-assisted register import for Excel, CSV, and PDF uploads.
95. AI maps messy client files into vehicle, staff, equipment, stock, medication, base, and location records.
96. AI import must show detected fields, confidence, duplicates, missing data, and suggested mapping.
97. User must approve mappings before records are created.
98. AI checklist import converts uploaded checklists into the builder section, row, and column structure.
99. Imported records must function inside registers, checklists, reports, readiness, and audit logs.
100. Audit every upload, mapping, approval, rejection, and import.

### Phase 13A: Clinical Guideline And SOP Knowledge UI

Higher-tier clients must be able to upload existing clinical practice guidelines, company SOPs, policies, procedures, and operational manuals as PDF or Word documents. The platform must transform those source documents into a controlled, searchable, easy-to-navigate app interface without losing the original source, version, citations, approval status, or clinical safety context.

This feature is not available to Base clients. Base may store uploaded documents as ordinary files if document storage is enabled, but Base must not receive AI-assisted document conversion, semantic SOP search, navigable guideline UI, source-backed Q&A, acknowledgement tracking, or predictive SOP analytics.

Tier positioning:

- `Base`: ordinary file storage only where included. No AI conversion, no SOP knowledge UI, no semantic search, no source-backed Q&A.
- `Pro`: structured upload, extraction, topic/category navigation, searchable SOP library, human review, version control, publish workflow, acknowledgement tracking, and role/scope access.
- `Premium`: AI-assisted restructuring, semantic search, source-backed Q&A, change comparison, SOP analytics, training/acknowledgement dashboards, and future risk reports linked to SOP compliance gaps.

Required providers and platform functions:

- `Azure Blob Storage` for immutable original source files, extracted artifacts, normalized document structures, images, generated PDFs, and published knowledge versions.
- `Azure AI Document Intelligence v4.0` for PDF extraction, OCR, layout extraction, tables, handwritten or scanned text where possible, and document structure extraction.
- `DOCX/Open XML parser` or equivalent server-side Word conversion pipeline for `.docx` files. Word files must not rely on a user's local Microsoft Word installation.
- `Azure AI Search` for tenant-scoped full-text, filtered, and hybrid search across published SOP and guideline content.
- `Vector search` using Azure AI Search vector indexes or OpenAI vector stores where semantic search and Q&A are enabled.
- `OpenAI or Azure OpenAI` for document classification, section labelling, summarization drafts, navigation tree generation, plain-English explanations, semantic search assistance, and source-backed Q&A.
- `Azure Service Bus`, `Azure Storage Queue`, or equivalent background queue for asynchronous upload processing, extraction, classification, indexing, and review jobs.
- `Azure Key Vault` for provider keys, storage credentials, search credentials, and model credentials.
- `Azure Monitor` and `Application Insights` for upload failures, extraction errors, review queue failures, indexing failures, latency, and tenant impact.
- `Virus/malware scanning service` or equivalent file security gate before processing uploaded files in production.
- `Tenant isolation layer` so one client's documents, extracted content, embeddings, citations, and search results cannot appear for another client.

Required data model:

- `KnowledgeDocument`: tenant, title, category, source type, owner, status, current published version, access scope, created by, created at, and retired status.
- `KnowledgeDocumentVersion`: source document version, extracted version, published version, version note, effective date, review status, reviewer, approved by, approved at, and retired at.
- `KnowledgeSourceFile`: original file path, file hash, file type, file size, uploaded by, upload date, security scan status, extraction status, and immutable storage reference.
- `KnowledgeProcessingJob`: queue status, provider used, extraction model version, AI model version, started at, completed at, failure reason, retry count, and reviewer handoff.
- `KnowledgeSection`: document version, heading, section number, parent section, body content, tables, warnings, definitions, linked images, source page range, and sort order.
- `KnowledgeChunk`: searchable text chunk, semantic embedding reference, section link, source citation, access scope, and index status.
- `KnowledgeTopic`: clinical guideline, company SOP, dispatch SOP, medication protocol, equipment procedure, infection control, incident reporting, vehicle procedure, HR/training policy, compliance procedure, or custom topic.
- `KnowledgeNavigationNode`: published app navigation tree with title, parent, child order, linked section, topic, and role/scope visibility.
- `KnowledgeCitation`: original file, page number, section heading, extracted text range, table reference, and source confidence.
- `KnowledgeReviewItem`: low-confidence extraction, unclear scan, missing heading, duplicate protocol, conflicting instruction, medication dosage warning, reviewer comment, and resolution status.
- `KnowledgePublishScope`: staff, operational manager, senior manager, clinical qualification/scope, assigned area, vehicle function/subtype, base, or company-wide scope.
- `KnowledgeAcknowledgement`: user, document version, acknowledgement status, acknowledgement date, due date, reminder status, and evidence record.
- `KnowledgeChangeLog`: old version, new version, changed section, change type, reviewer, approval note, and affected users.
- `KnowledgeAccessRule`: who can view, search, acknowledge, review, publish, retire, export, or ask source-backed questions.

Required ingestion pipeline:

1. User uploads PDF or Word source document.
2. System validates file type, size, tenant, permission, and tier entitlement.
3. System stores the original file immutably before any extraction or AI processing.
4. System runs file security scanning before extraction.
5. PDF and scanned files are processed through Azure AI Document Intelligence.
6. Word files are processed through the DOCX/Open XML conversion pipeline.
7. System creates a normalized intermediate representation containing headings, paragraphs, tables, lists, images, page references, and source citations.
8. System segments the document into sections, procedures, checklists, algorithms, medication rules, equipment procedures, warnings, appendices, and forms where detected.
9. AI classifies the content into guideline/SOP categories and suggests navigation labels.
10. AI detects clinical risk markers such as drug dosages, contraindications, scope restrictions, escalation requirements, and emergency procedures.
11. AI detects operational risk markers such as equipment checks, infection control, vehicle procedure, stock/medication handling, incident reporting, and audit evidence requirements.
12. AI creates draft summaries and plain-English navigation descriptions, but these are drafts only until reviewed.
13. System builds a citation map back to the original document page, heading, table, or text range.
14. System creates a review queue for low-confidence extraction, unclear scans, missing citations, conflicts, duplicates, or clinical-risk content.
15. Senior management or an assigned clinical/policy reviewer approves, edits, rejects, or sends the extracted content back for correction.
16. Only reviewed content can be published into the live SOP/guideline UI.
17. Published versions are indexed for search and semantic retrieval.
18. Every published answer, quick reference, search result, and AI explanation must show source citations.
19. Retired versions remain stored for audit and legal evidence, but are not shown as current guidance.

Clinical and legal safety rules:

- The system must not silently rewrite clinical instructions.
- AI output must be labelled as a draft until approved.
- Medication dosages, contraindications, clinical scope limits, and treatment steps must be displayed from source-backed content with citations.
- If extraction confidence is low, the system must block publishing that section until reviewed.
- If two source documents conflict, the system must flag the conflict and require human resolution.
- If a user asks a question and no reliable cited source exists, the answer must say that no approved source is available.
- The app must not provide patient-specific clinical advice.
- The app must not represent AI output as regulator-approved or clinically authoritative unless the client has reviewed and published it.
- All uploads, extractions, AI drafts, edits, approvals, publishes, retirements, acknowledgements, and exports must be audit logged.

Required user experience:

- Add a `Guidelines & SOPs` module for Pro and Premium clients.
- Provide a document library grouped by topic, role, scope, status, and version.
- Provide a search interface that supports keyword search, filters, and semantic search where enabled.
- Provide a browse interface with collapsible topic navigation and quick-reference cards.
- Provide source citations on every extracted section and every AI-assisted answer.
- Provide an original-source viewer or download action for users with permission.
- Provide a `What changed` view between document versions.
- Provide acknowledgement tracking where managers require staff to confirm they have read an updated guideline or SOP.
- Provide reminders and escalation for overdue acknowledgements.
- Provide a `Report issue in SOP` action so users can flag unclear, outdated, unsafe, or conflicting content.
- Provide manager dashboards for unpublished uploads, review queue, published versions, acknowledgement status, and user feedback.

Current app gaps that must be built before this feature can work:

- No knowledge document data model exists.
- No immutable source document store exists for guidelines or SOPs.
- No extraction/OCR pipeline exists.
- No Word document conversion pipeline exists.
- No background processing queue exists for document ingestion.
- No AI classification or SOP navigation generation exists.
- No citation model exists.
- No source-backed search or vector index exists.
- No source-backed Q&A exists.
- No clinical/policy review queue exists.
- No published knowledge UI exists.
- No acknowledgement tracking exists.
- No SOP issue feedback workflow exists.
- No document version diff exists.
- No tenant-scoped knowledge access rules exist.
- No legal/clinical safety review gate exists for AI-generated SOP views.

100A. Add the Pro/Premium-only `Guidelines & SOPs` module and hide or upgrade-lock it for Base tenants.
100B. Add tier gates so Base cannot use AI SOP conversion, source-backed Q&A, semantic search, or acknowledgement dashboards.
100C. Add secure upload for PDF, DOCX, and approved document formats with validation, size limits, tenant checks, permission checks, and security scan status.
100D. Store every original file immutably in tenant-scoped Blob Storage before extraction starts.
100E. Add the knowledge data model: documents, versions, source files, processing jobs, sections, chunks, topics, navigation nodes, citations, review items, publish scopes, acknowledgements, change logs, and access rules.
100F. Add asynchronous processing jobs for extraction, conversion, AI classification, citation mapping, indexing, and review queue creation.
100G. Integrate Azure AI Document Intelligence for PDF, scan, OCR, layout, table, and document structure extraction.
100H. Integrate a DOCX/Open XML conversion path for Word documents.
100I. Normalize extracted content into a canonical JSON/HTML/Markdown structure with source references preserved.
100J. Segment documents into headings, sections, procedures, algorithms, tables, checklists, appendices, and forms.
100K. Add AI classification for clinical guideline, company SOP, dispatch SOP, medication protocol, equipment procedure, infection control, incident reporting, vehicle procedure, HR/training policy, compliance procedure, and custom categories.
100L. Add AI-assisted navigation tree generation, but keep it draft-only until approved.
100M. Add citation mapping to original file, page, heading, table, and source text range.
100N. Add review queue flags for low confidence, unclear scans, conflicts, duplicate instructions, missing citations, medication dosage content, and clinical scope limits.
100O. Require senior or assigned clinical/policy approval before any converted SOP/guideline becomes visible as published guidance.
100P. Add publish scope controls by role, access level, clinical qualification/scope, assigned area, vehicle function/subtype, base, or company-wide visibility.
100Q. Build the published `Guidelines & SOPs` UI with topic navigation, search, quick reference, source view, version status, and mobile-friendly layout.
100R. Add Azure AI Search indexing for published content with tenant isolation and status filtering.
100S. Add semantic/vector search for Premium where enabled.
100T. Add source-backed Q&A that refuses unsupported answers and always shows citations.
100U. Add conflict detection and require reviewer resolution before publishing conflicting sections.
100V. Add document version comparison and `What changed` summaries.
100W. Add acknowledgement workflow with due dates, reminders, escalation, and evidence records.
100X. Add SOP feedback workflow for unclear, outdated, unsafe, or conflicting content.
100Y. Add audit logs for upload, extraction, AI draft, reviewer edit, approval, rejection, publish, retirement, search, Q&A, acknowledgement, and export.
100Z. Add verification using at least one clean DOCX SOP, one scanned PDF SOP, one clinical guideline PDF, one conflicting-document test, one low-confidence OCR test, and one tenant-isolation test.
100AA. Add manager dashboards for unpublished uploads, review queue, published versions, failed processing jobs, user feedback, and acknowledgement status.
100AB. Add notifications for required acknowledgement, overdue acknowledgement, published update, rejected upload, failed extraction, and SOP issue feedback.
100AC. Link SOP evidence into EMS Audit Compliance Mode where a requirement needs proof of policies, clinical guidelines, or company SOPs.
100AD. Link SOP analytics into AI 3-month, 6-month, and 12-month future reports for compliance gaps, repeated SOP issue reports, missing acknowledgements, outdated policies, and high-risk procedure gaps.
100AE. Add export controls for approved SOP packs, acknowledgement evidence, review history, and source citation reports.
100AF. No SOP/guideline AI feature may be enabled for production clients until tenant isolation, citation enforcement, human approval, audit logging, and legal/clinical safety review gates are verified.

### Phase 14: AI Predictive Analysis

101. Build 3-month, 6-month, and 12-month AI forecast reports.
102. Forecast expiry pressure, service pressure, licensing/CPD risk, shortages, failures, repeated defects, and compliance gaps.
103. Reports must use real app data only.
104. Each AI report must include risks, priority, affected assets/areas, and recommended actions.
105. Senior managers can generate company-wide reports.
106. Operational managers can generate scoped reports only for assigned areas.

### Phase 15: SaaS Packaging

107. Implement true multi-client tenant separation.
108. Add subscription tier field per company.
109. Enforce Base, Pro, and Premium feature gates.
110. Base includes manual registers, manual checklist builder, daily checks, PDF evidence, tasks/issues, and basic readiness.
111. Pro includes guided imports, advanced reports, area publishing, approvals, and readiness customization.
112. Premium includes AI import, AI analysis, Azure storage, integrations, and extended retention.
113. Add platform-owner admin controls for client management.

### Phase 15A: Commercial Downgrade, Cancellation, And Tier Data Rules

Subscription changes must be predictable, transparent, and technically safe. A client must never lose data silently because they downgrade, cancel, miss a payment, or move from a trial to a paid tier. A client must also not keep using Pro or Premium functions after downgrading to Base unless the contract explicitly allows a grace period.

Core commercial principle:

- Downgrading changes access to features.
- Downgrading must not secretly delete client data.
- Pro/Premium-only data must become read-only, exportable, and recoverable on upgrade unless the client explicitly requests deletion through the offboarding workflow.
- Cancellation must trigger export/offboarding options before deletion.
- Payment failure must suspend risky actions gradually, not immediately destroy operational continuity.

Required subscription states:

- `Trial`
- `Active`
- `Grace period`
- `Payment failed`
- `Past due`
- `Suspended`
- `Downgrade pending`
- `Downgraded`
- `Cancellation pending`
- `Cancelled export available`
- `Offboarding requested`
- `Deleted`
- `Legal hold`

Required tier transition model:

- `Base to Pro`: unlock guided import, advanced reporting, area publishing, approvals, readiness customization, and higher retention settings.
- `Pro to Premium`: unlock AI import, AI analysis, SOP/guideline knowledge UI, predictive reports, larger storage, integrations, extended retention, and dedicated Azure options where contracted.
- `Premium to Pro`: disable Premium-only new actions, keep existing Premium outputs read-only/exportable, preserve data for a defined retention/grace window, and offer reactivation.
- `Pro to Base`: disable Pro-only new actions, keep existing Pro outputs read-only/exportable, preserve data for a defined retention/grace window, and offer reactivation.
- `Any paid tier to cancelled`: stop normal operational use after the cancellation effective date, keep export available for a defined period, then execute retention/deletion rules.

Data behaviour when downgrading:

- Manual registers, daily checks, basic checklist builder, tasks/issues, basic PDF evidence, and basic readiness remain available in Base if the account is active.
- Guided import mappings from Pro become read-only after downgrade to Base. Imported records remain because they are now normal client records.
- Advanced checklist version history remains stored and exportable, but advanced comparison, approval workflow, and area-specific publish controls are locked unless the tier includes them.
- Area-specific published checklists must not break daily operations on downgrade. The app must either preserve the currently active published assignments as read-only or require the client to choose one Base-compatible active checklist during downgrade.
- Readiness Engine custom rules become read-only if the downgraded tier does not include customization. The product must choose a safe rule set: either keep the last published rules read-only or require conversion to default Base readiness before downgrade completes.
- Advanced reports remain available as static historical evidence, but new advanced report generation is locked.
- AI import mappings, AI-generated reports, AI forecast reports, SOP/guideline AI structures, semantic search indexes, vector stores, and AI citations remain stored/exportable according to retention settings, but new AI processing is locked.
- SOP/guideline knowledge UI becomes read-only or locked according to contract. Source documents must remain exportable.
- Premium integrations are disabled after downgrade unless needed for legal export/offboarding. Integration credentials must be retained only as long as needed and then removed.
- Extended retention ends prospectively. Existing retained data must follow the retention contract that applied when it was created unless the contract/legal policy says otherwise.
- Dedicated client deployment options must be reviewed before downgrade. A dedicated Premium tenant may need migration back to shared hosting, continued paid dedicated hosting, or cancellation/offboarding.

Data behaviour when cancelling:

- The client must be shown export options before cancellation completes.
- The client must be shown retention/deletion rules before cancellation completes.
- The client must be able to download all client-owned data through the export/offboarding pathway.
- After cancellation effective date, normal users lose operational write access unless the contract includes a wind-down period.
- Authorized company owners/senior administrators retain limited access to export, invoices, cancellation status, and offboarding until the export window closes.
- After the export window closes, the tenant moves to deletion/offboarding according to the client's confirmed choice and legal retention rules.
- If the client does nothing after cancellation, the contract must define the export availability window, suspension window, deletion window, and retained-data categories.

User experience requirements:

- Add a subscription management screen for company owners/senior administrators.
- Show current tier, renewal date, payment status, storage usage, active modules, locked modules, and upcoming downgrade/cancellation effects.
- Before downgrade, show a plain-English impact review: what remains active, what becomes read-only, what is locked, what must be selected, what is exportable, and what can be recovered by upgrading again.
- Before downgrade, require resolution of conflicts such as more active checklists/scopes than the target tier allows.
- Before cancellation, show export/offboarding actions prominently.
- After downgrade, locked buttons must explain why they are locked and what tier re-enables them.
- Pro/Premium-only data must not disappear from the UI without an explanation. It must show as locked/read-only/exportable where appropriate.
- Add in-app warnings before the effective date of downgrade or cancellation.
- Add email/SMS notification hooks for subscription status changes, export window reminders, payment failure, grace period ending, and cancellation effective date.

Required admin/platform controls:

- Platform owner must be able to view tenant tier, subscription status, grace period, downgrade date, cancellation date, export window, deletion state, and legal hold state.
- Platform owner must not manually delete a tenant outside the audited offboarding workflow.
- Platform owner must be able to apply a temporary grace period or support override with reason capture and expiry date.
- All subscription overrides must be audited.

Required audit events:

- Tier upgrade.
- Tier downgrade requested.
- Downgrade impact review displayed.
- Downgrade confirmed.
- Downgrade effective.
- Feature locked because of tier.
- Read-only Pro/Premium data viewed.
- Cancellation requested.
- Cancellation confirmed.
- Export requested.
- Export downloaded.
- Export window expired.
- Offboarding requested.
- Data deletion started.
- Data deletion completed.
- Legal hold applied or removed.
- Platform support override applied, changed, or expired.

113A. Add subscription status fields to the company/tenant record.
113B. Add tier transition history with old tier, new tier, requested by, approved by, effective date, reason, and audit event.
113C. Add downgrade impact calculation that lists every affected module, feature, record type, and active workflow.
113D. Add commercial rules for Base, Pro, Premium, enterprise/dedicated deployment, trial, suspended, cancelled, and legal hold states.
113E. Add UI for company owners/senior administrators to see current tier, locked features, usage, downgrade effects, cancellation effects, export options, and renewal/payment status.
113F. Add downgrade confirmation workflow with clear read-only/export/deletion explanation before the downgrade takes effect.
113G. Add cancellation workflow that routes the client to export/offboarding before deletion or final closure.
113H. Add payment-failure and grace-period rules that preserve operational continuity for a defined period without allowing unpaid expansion of Pro/Premium features.
113I. Add locked/read-only mode for Pro/Premium-only historical data after downgrade.
113J. Add rules preserving imported records as normal client records even if the Pro import tool is later locked.
113K. Add rules for checklist scopes during downgrade so daily operations cannot silently break.
113L. Add rules for readiness engine custom rules during downgrade.
113M. Add rules for AI import, AI reports, SOP/guideline knowledge UI, vector/search artifacts, and AI extracted data during downgrade.
113N. Add rules for extended retention changes during downgrade.
113O. Add rules for dedicated Premium deployments during downgrade or cancellation.
113P. Add subscription notification templates for downgrade warning, cancellation warning, payment failure, grace period ending, export ready, export window expiring, and account closure.
113Q. Add platform-owner support override with reason, expiry date, audited actor, and automatic expiry.
113R. Add audit logs for every subscription state change and every locked-feature access attempt.
113S. Add tests proving downgrade does not delete data silently.
113T. Add tests proving downgraded clients cannot create new Pro/Premium-only data.
113U. Add tests proving locked Pro/Premium historical data remains exportable.
113V. Add tests proving cancellation routes through export/offboarding.
113W. Add tests proving payment failure and grace period rules do not corrupt daily operations.
113X. Add contract/privacy wording for downgrade, cancellation, export windows, grace periods, data retention, and reactivation.
113Y. Add commercial support documentation explaining downgrade, cancellation, data export, retained data, reactivation, and deletion.
113Z. Production launch is blocked until downgrade, cancellation, export, retention, feature lock, and reactivation rules are verified.

### Phase 15B: Exact Tier Feature Matrix And Data-State Rules

The subscription matrix must be explicit before pricing, sales material, client onboarding, feature gates, downgrade logic, cancellation logic, and production release. Every module must have a defined state per tier so the app never behaves inconsistently.

Feature state definitions:

- `Active`: the tenant may create, read, update, delete, publish, submit, export, and use the feature according to role permissions.
- `Limited`: the tenant may use the feature with caps, reduced filters, reduced automation, reduced retention, or reduced workflow depth.
- `Read-only`: the tenant may view historical data and export it, but may not create new records, run new processing, publish new versions, or change configuration.
- `Locked`: the tenant cannot access the feature except for upgrade messaging and export/offboarding where required.
- `Exportable`: all tenant-owned data for that feature must be included in client export packages.
- `Deleted`: data may be deleted only through the offboarding/deletion workflow or according to an approved retention policy. Downgrade alone must not delete data.

Commercial tier matrix:

| Feature / Data Area | Base | Pro | Premium | Enterprise / Dedicated | Downgrade / Cancellation Rule |
| --- | --- | --- | --- | --- | --- |
| Tenant/company profile | Active | Active | Active | Active | Exportable. Deleted only through offboarding. |
| Setup wizard | Active | Active | Active | Active | Setup history remains exportable. |
| Company branding/logo | Active | Active | Active | Active, with client-specific UI options | Exportable. Removed only through offboarding or explicit logo removal. |
| Staff register | Active | Active | Active | Active | Staff records remain active while subscription is active. Exportable on cancellation. |
| Vehicle register | Active | Active | Active | Active | Vehicle records remain active while subscription is active. Exportable on cancellation. |
| Equipment register | Active | Active | Active | Active | Equipment records remain active while subscription is active. Exportable on cancellation. |
| Stock register | Active | Active | Active | Active | Stock records remain active while subscription is active. Exportable on cancellation. |
| Medication register | Active | Active | Active | Active | Medication records remain active while subscription is active. Exportable on cancellation. |
| Bases, areas, storage locations | Active | Active | Active | Active | Operational structure remains active while subscription is active. Exportable on cancellation. |
| Manual register add/edit/delete | Active | Active | Active | Active | Always role-permission controlled. |
| Manual checklist builder | Active | Active | Active | Active | Base must include manual checklist building. |
| Checklist register | Active basic | Active advanced | Active advanced | Active advanced/custom | All templates exportable. Deletion retires templates and scopes. |
| Checklist publishing | Limited: one active daily checklist per function/subtype where Base rules allow | Active: area, function, subtype, callsign, approval workflow | Active: advanced scopes, automation, AI assistance where enabled | Active with client-specific rules | On downgrade, active scopes must be preserved read-only or converted through guided downgrade resolution. |
| Area-specific checklist publishing | Locked or limited by commercial choice | Active | Active | Active | Existing area-specific scopes become read-only/exportable on downgrade if target tier does not support editing them. |
| Checklist approval workflow | Locked | Active | Active | Active/custom | Existing approvals remain exportable and auditable. |
| Daily Vehicle & Equipment Check | Active | Active | Active | Active | Must always draw from active Checklist Register scopes only. |
| Full Audit checklist | Active manual | Active | Active | Active/custom | Historical audits remain exportable. |
| Checklist PDF evidence | Active basic | Active | Active | Active/custom templates | PDFs exportable. Deleted only through offboarding/retention. |
| Checklist reports | Limited filters | Active advanced filters | Active advanced filters and analytics links | Active custom reporting | Historical reports remain read-only/exportable after downgrade. |
| Variance alerts | Limited/manual review | Active | Active with analytics links | Active/custom | Existing variance records remain exportable. |
| Tasks and issues | Active | Active | Active | Active | Records exportable. Permissions still apply. |
| Task feedback | Active | Active | Active | Active | Records exportable. |
| Asset movement | Active | Active | Active | Active | Movement logs exportable and audit logged. |
| Stock Orders & Distribution | Active basic | Active | Active | Active/custom supplier integration | Existing stock orders exportable. Integrations locked after downgrade. |
| Supplier confirmations | Limited/manual | Active | Active | Active/integrated | Existing confirmations exportable. |
| Expiry/service pressure | Active basic | Active | Active | Active/custom | Historical pressure data exportable. Advanced forecasting locked by tier. |
| Date coloring and expiry alerts | Active | Active | Active | Active | Notification channel availability depends on notification tier. |
| Readiness Dashboard | Active basic | Active scoped dashboards | Active analytics-linked dashboards | Active custom dashboards | Historical readiness data exportable. |
| Readiness Engine defaults | Active default rules | Active default and limited custom rules | Active custom rules and approval workflows | Active custom/contracted engine | Custom rules become read-only/exportable if downgraded below supported tier. |
| Readiness Engine customization | Locked or limited defaults only | Active | Active | Active/custom | New customization locked after downgrade; last published rules preserved read-only or converted through downgrade workflow. |
| Operational reports | Limited filters | Active advanced filters | Active advanced filters plus AI/report links | Active custom reports | Existing reports read-only/exportable after downgrade. |
| EMS Audit Compliance Mode | Locked or limited basic evidence only | Active | Active with AI support | Active/custom regulator packs | Existing packs exportable. New pack generation locked if tier no longer supports it. |
| Basic audit logs | Active | Active | Active | Active | Audit logs exportable subject to legal retention. |
| Advanced audit/evidence controls | Limited | Active | Active | Active/custom retention | Advanced controls read-only/exportable after downgrade. |
| Personal documents | Active | Active | Active | Active | Documents exportable and deletable according to permissions/retention. |
| Staff licensing/CPD tracking | Active basic | Active | Active with analytics | Active/custom | Records remain active while subscription is active. |
| Guided register import | Locked | Active | Active | Active/custom onboarding | Mappings become read-only after downgrade; imported records remain normal records. |
| Guided checklist import | Locked | Active | Active | Active/custom onboarding | Imported checklist templates remain in register, but advanced import tool locks after downgrade. |
| AI register import | Locked | Locked or optional add-on | Active | Active/custom | AI mappings, decisions, and imported results remain read-only/exportable after downgrade. |
| AI checklist generation | Locked | Locked or optional add-on | Active | Active/custom | Generated templates remain in register if approved/published; generation tool locks after downgrade. |
| AI predictive reports | Locked | Locked or optional add-on | Active | Active/custom | Generated reports become read-only/exportable after downgrade. No new generation. |
| 3/6/12 month forecasts | Locked | Locked or optional add-on | Active | Active/custom | Forecast outputs read-only/exportable after downgrade. |
| Guidelines & SOPs source storage | Locked except ordinary document storage if included | Active structured library | Active AI-assisted knowledge UI | Active/custom | Source files exportable. UI becomes read-only/locked depending on downgrade tier. |
| SOP/guideline AI conversion | Locked | Locked or optional add-on | Active | Active/custom | Extracted structures and citations read-only/exportable after downgrade. No new conversion. |
| SOP semantic search/Q&A | Locked | Locked or optional add-on | Active | Active/custom | Search/Q&A locked after downgrade. Source documents remain exportable. |
| SOP acknowledgements | Locked | Active where SOP module included | Active | Active/custom | Existing acknowledgements exportable. New acknowledgement campaigns locked if unsupported. |
| SMS/email notifications | Locked or limited in-app only | Active according to provider setup | Active with escalation and advanced templates | Active/custom provider | Existing notification logs exportable. Provider sends lock after downgrade unless needed for account/export notices. |
| Notification templates | Locked or basic | Active | Active advanced | Active/custom | Templates read-only/exportable after downgrade. |
| Integrations/API/webhooks | Locked | Limited or add-on | Active | Active/custom | Integration credentials disabled/removed according to downgrade/offboarding rules. Existing synced records remain exportable. |
| Azure Blob advanced storage | Local/basic planned for early product; production still uses secure storage | Active | Active extended storage | Active dedicated storage | Storage access follows tier and retention contract. Export always available. |
| Extended retention | Limited | Standard | Extended | Contract-defined | Downgrade changes future retention only unless contract says otherwise. Existing retention obligations respected. |
| Dedicated Azure deployment | Locked | Locked | Optional add-on | Active | Downgrade requires migration, continued dedicated hosting fee, or offboarding. |
| Client-specific UI/publishing | Locked or limited branding only | Active through configuration | Active through configuration/release rings | Active dedicated/client-specific release | Client-specific features lock/read-only after downgrade unless included in new tier. |
| Data export | Active | Active | Active | Active | Always available to authorized company owner/senior administrator. |
| Data deletion/offboarding | Active | Active | Active | Active | Always available subject to legal retention and contract terms. |
| Platform support override | Platform-only | Platform-only | Platform-only | Platform-only | Must be audited, time-limited, and reason-captured. |

Deletion rules by tier:

- No tier downgrade may delete data automatically.
- Deletion can happen only through explicit record deletion by an authorized user, explicit tenant offboarding/deletion, legal retention expiry, or approved platform-owner maintenance with audit trail.
- Pro/Premium-only artifacts must remain exportable even when the feature becomes locked.
- AI/vector/search artifacts must be deleted during offboarding if no legal/contractual retention rule requires retention.
- Locked features must not hide exportable historical data without an explanation.

Base tier boundaries:

- Base must solve the core operational pain: registers, manual checklist building, daily checks, tasks/issues, basic readiness, basic reports, audit logs, and PDF evidence.
- Base must not include AI import, AI predictive reports, SOP/guideline AI conversion, semantic Q&A, advanced integrations, dedicated deployment, or advanced compliance analytics.
- Base may include limited exports because data portability is a core trust obligation, not an upsell.

Pro tier boundaries:

- Pro is the operational management tier. It adds guided import, better reporting, area-specific publishing, approval workflows, readiness customization, compliance preparation depth, and stronger management controls.
- Pro may include optional paid add-ons, but the default Pro tier must remain clearly different from Premium AI automation.

Premium tier boundaries:

- Premium is the automation and intelligence tier. It adds AI import, AI forecast reports, SOP/guideline AI knowledge UI, semantic search/Q&A, extended retention, integrations, larger storage, advanced analytics, and optional dedicated Azure support.
- Premium AI outputs must remain source-backed, reviewed where required, and exportable.

Enterprise / Dedicated boundaries:

- Enterprise is not just a fourth generic tier. It is a contract-driven deployment/support model for larger clients needing dedicated Azure hosting, custom retention, custom integrations, custom release controls, data residency terms, dedicated support, custom security review, or special compliance requirements.
- Enterprise clients must still use the same source-of-truth, audit, export, offboarding, and safety rules unless the contract explicitly adds stricter rules.

113AA. Add the subscription feature matrix to product configuration so every feature has a tier state: active, limited, read-only, locked, exportable, or deletion-controlled.
113AB. Add feature gates for every module in the matrix, not only navigation buttons.
113AC. Add a tier-state explanation component so locked/read-only features explain why they are locked and what data remains exportable.
113AD. Add downgrade-impact rules for every row in the matrix.
113AE. Add cancellation/export/offboarding rules for every row in the matrix.
113AF. Add tests proving Base can build and publish manual checklists without Pro/Premium automation.
113AG. Add tests proving Base cannot run guided import, AI import, AI forecasting, SOP AI conversion, semantic Q&A, advanced integrations, or dedicated deployment controls.
113AH. Add tests proving Pro can use guided import, area publishing, approvals, readiness customization, and advanced reports.
113AI. Add tests proving Premium can use AI import, AI forecasting, SOP/guideline AI knowledge UI, semantic search/Q&A, extended retention, and integrations.
113AJ. Add tests proving Enterprise/dedicated clients inherit product safety rules while allowing contract-specific deployment and retention settings.
113AK. Add tests proving Pro/Premium historical data becomes read-only/exportable after downgrade.
113AL. Add tests proving downgrade does not remove imported records from normal registers.
113AM. Add tests proving AI/vector/search artifacts are exportable and then deleted during offboarding when retention allows.
113AN. Add UI verification for locked/read-only/exportable states on desktop and mobile.
113AO. Add platform-owner verification that feature overrides are audited, temporary where applicable, and cannot bypass tenant isolation.
113AP. Add documentation for sales/support explaining exact tier boundaries and downgrade behavior.
113AQ. Add contract wording matching the matrix so sales promises and product behavior cannot diverge.
113AR. Production launch is blocked until the tier matrix is implemented, tested, documented, and legally reviewed.

### Phase 15C: Production Billing, Payment Provider, Tax, And Invoice Architecture

AcuityOps must have a production-grade billing architecture before paid subscriptions, public launch, client onboarding, downgrade/cancellation automation, or tier enforcement. Billing must not be implemented as manual notes, ad hoc bank transfers, or disconnected payment links once real clients are active.

Billing must support:

- Monthly and annual subscriptions.
- Base, Pro, Premium, and Enterprise/dedicated pricing.
- Trials and trial conversion.
- Grace periods.
- Failed payment handling.
- Subscription upgrades and downgrades.
- Cancellation and offboarding.
- Invoices and invoice history.
- VAT/tax treatment.
- Refunds and credit notes.
- Manual enterprise invoices where required.
- Payment-provider webhooks.
- Audit logs for all commercial state changes.

Provider architecture:

- Build a `BillingProvider` abstraction before integrating any specific provider.
- `Stripe Billing` is the preferred global provider where the company entity, country, currency, card network, compliance, and payout support make it available and commercially practical.
- `Paystack Subscriptions` is a candidate for African markets where its supported payment methods, currencies, country availability, subscription limits, and retry behavior match the client's billing needs.
- A South African payment provider fallback must be selected before South African paid launch if Stripe or Paystack does not fully support the required entity/currency/recurring-payment setup. Candidate providers must be reviewed against subscription support, card debit support, EFT/debit-order support, invoices, refunds, webhooks, VAT invoice needs, settlement timing, payout fees, chargebacks, and API quality.
- Enterprise clients may use manual invoice/bank transfer billing when card subscription billing is commercially inappropriate. Manual billing must still update subscription state through the same billing state machine.
- The app must never trust the frontend as proof of payment. Subscription state must be updated only from verified provider webhooks, verified manual admin actions, or reconciled invoice/payment records.

Billing provider decision record template:

Before any payment provider is implemented, AcuityOps must create a written billing provider decision record. This record must compare Stripe, Paystack, at least one South African fallback provider, and manual Enterprise invoice billing against the same criteria. The decision record must be stored with the product architecture documents and must be reviewed before public paid launch.

Decision record fields:

- `Decision ID`: unique billing decision identifier.
- `Decision status`: proposed, approved, rejected, deferred, superseded.
- `Decision date`: date of review.
- `Decision owner`: person accountable for the choice.
- `Reviewers`: technical, accounting, legal, tax, and commercial reviewers.
- `Launch market`: South Africa, Africa region, UK, Australia, United States, or global.
- `Target client type`: small private EMS, medium EMS, NGO, government-adjacent, Enterprise.
- `Billing model`: self-service card subscription, manual invoice, bank transfer, debit order, hybrid.
- `Currencies required`: ZAR, USD, GBP, AUD, EUR, or other.
- `Plans covered`: Base, Pro, Premium, Enterprise, add-ons, annual contracts.
- `Provider candidates`: Stripe, Paystack, South African fallback provider name, manual Enterprise billing.
- `Provider availability`: supported company country, supported client countries, supported currencies, payout country, payout currency.
- `Recurring billing support`: monthly, annual, trials, upgrades, downgrades, proration, cancellation, grace period handling.
- `Invoice support`: invoice numbers, invoice PDFs, tax fields, invoice history, receipts, credit notes, manual invoice support.
- `VAT/tax support`: South African VAT, tax-inclusive pricing, tax-exclusive pricing, tax rate configuration, international VAT/GST/sales tax expansion.
- `Failed payment support`: retries, dunning emails, failed payment webhooks, payment recovery links, customer portal support.
- `Refund and dispute support`: full refunds, partial refunds, credit notes, chargeback/dispute handling, audit trail.
- `Payment methods`: card, EFT, debit order, bank transfer, mobile money, or other local methods.
- `Webhook capability`: signed webhooks, event types, replay protection, retry policy, idempotency support.
- `Security/compliance`: PCI handling, hosted checkout/customer portal, secret storage, POPIA/GDPR implications, data retention, provider security documentation.
- `Operational fit`: dashboard usability, support responsiveness, sandbox quality, API/SDK quality, documentation quality, local market reliability.
- `Commercial fit`: fees, chargeback fees, settlement timing, payout timing, reserve risk, foreign exchange cost, minimum fees.
- `AcuityOps integration impact`: implementation complexity, migration complexity, test burden, support burden, accounting export burden.
- `State-machine mapping`: how provider events map to `trialing`, `active`, `payment_pending`, `past_due`, `grace_period`, `unpaid`, `suspended`, `cancel_at_period_end`, and `cancelled_export_available`.
- `Entitlement impact`: how payment state updates Base, Pro, Premium, Enterprise, add-ons, locked states, read-only states, and export-only states.
- `Manual override rules`: when platform support may apply a grace period, billing hold, support override, legal hold, refund, credit, or manual payment reconciliation.
- `Exit plan`: how AcuityOps can switch provider later without losing invoices, subscription state, payment history, tax records, or audit trail.
- `Known blockers`: unresolved legal, accounting, tax, provider, market, currency, webhook, invoice, or refund issues.
- `Go/no-go decision`: selected provider path and reason.
- `Required proof before launch`: sandbox subscription tests, failed payment tests, invoice PDF tests, refund tests, webhook tests, downgrade/cancellation tests, export/offboarding tests, and accountant/legal sign-off.

Decision record scoring:

- Score each provider candidate from 0 to 5 for availability, recurring billing, invoice/tax fit, failed payment handling, refunds/disputes, webhook quality, security/compliance, commercial cost, implementation effort, accounting fit, and market fit.
- A provider with a zero in legal availability, payout availability, webhook reliability, invoice/tax compliance, or security must not be selected for production.
- Manual Enterprise billing may be selected alongside a payment provider, but it must not bypass the same subscription state machine, invoice model, entitlement checks, or audit logging.
- A provider decision must be revisited before launching into a new country or changing legal entity, currency, tax approach, or subscription packaging.

Required billing domain model:

- `BillingCustomer`: tenant, legal billing name, trading name, VAT/tax number, billing address, country, currency, billing contact, accounts contact, provider customer ID, and status.
- `BillingPlan`: Base, Pro, Premium, Enterprise, add-on, monthly/annual, currency, list price, included limits, and active/retired status.
- `BillingSubscription`: tenant, plan, provider subscription ID, current status, trial dates, period start/end, renewal date, cancellation date, downgrade date, grace period, and latest invoice.
- `BillingEntitlement`: feature/tier entitlement derived from paid subscription status, support override, trial, grace period, or enterprise contract.
- `BillingInvoice`: invoice number, provider invoice ID, tenant, period, line items, subtotal, tax, total, currency, status, due date, paid date, PDF link, and accounting export status.
- `BillingPayment`: provider payment ID, invoice, amount, currency, payment method, status, paid date, failed date, failure reason, retry count, and receipt link.
- `BillingRefund`: payment, amount, reason, status, provider refund ID, credit note ID, requested by, approved by, and audit log.
- `BillingCreditNote`: invoice, amount, reason, tax impact, status, and accounting export.
- `BillingWebhookEvent`: provider, event type, event ID, raw payload hash, received date, processed date, processing status, idempotency key, and failure reason.
- `BillingTaxProfile`: VAT/tax registration status, VAT/tax number, tax region, tax rate, tax exemption status, reverse-charge flag if applicable, and accounting review status.
- `BillingSupportOverride`: temporary grace, manual entitlement, billing hold, legal hold, discount, coupon, or support extension with reason and expiry.
- `BillingAuditEvent`: every billing-related action, provider event, state transition, invoice event, refund, credit note, override, and manual correction.

Billing state machine:

- `trialing`: access follows trial tier and trial expiry date.
- `active`: access follows paid tier.
- `payment_pending`: payment initiated but not confirmed.
- `past_due`: invoice failed or unpaid; grace rules apply.
- `grace_period`: operational continuity allowed for a defined period while payment is corrected.
- `unpaid`: payment recovery failed; new Pro/Premium actions are blocked and cancellation/suspension rules begin.
- `suspended`: normal app access blocked except billing, export, offboarding, and support-approved access.
- `downgrade_pending`: downgrade scheduled for renewal or chosen effective date.
- `cancel_at_period_end`: cancellation scheduled but access continues until paid period ends.
- `cancelled_export_available`: normal app access ended; export/offboarding access remains available for defined window.
- `offboarding_requested`: export/deletion workflow has started.
- `deleted`: tenant data removed according to retention/offboarding rules.
- `legal_hold`: deletion/export actions restricted by documented legal hold.

Invoice requirements:

- Every paid subscription period must create an invoice record.
- Every invoice must show tenant legal name, billing address, VAT/tax number where applicable, invoice number, date, due date, period, tier, add-ons, currency, subtotal, tax, total, amount paid, balance due, and payment status.
- Invoice PDFs must be downloadable by authorized company-owner/senior users.
- Invoice records must remain visible after cancellation during the export/offboarding window.
- Enterprise/manual invoices must use the same invoice model and subscription state machine.
- Invoice numbering must be legally/accounting reviewed before public launch.
- Invoices must support South African VAT fields where applicable and international tax fields later.
- Accounting export must be planned for Xero, QuickBooks, Sage, CSV, or accountant-friendly export, but not built before legal/accounting review defines the required format.

Failed payment and grace-period rules:

- Failed payment must not immediately delete data or corrupt operations.
- Failed payment must trigger an in-app billing warning, email notification, and optional SMS only where notification preferences and provider rules allow.
- Grace period length must be configurable by plan and contract.
- During grace period, existing operational workflows may continue, but tier expansion, AI processing, new imports, new integrations, and large storage expansion may be blocked.
- After grace period, tenant moves to `unpaid` or `suspended` according to the contract.
- Suspended tenants must still have access to billing, invoice payment, export, cancellation, and support contact.
- Restoring payment must restore entitlements based on the active plan and must not resurrect retired seed/default data.

Upgrade and downgrade billing rules:

- Upgrades may take effect immediately or at next billing period, depending on product settings and provider support.
- Downgrades should default to next renewal date unless the client explicitly chooses immediate downgrade.
- Proration must be defined before launch. If used, provider proration must be reflected in invoice line items and audit logs.
- Downgrade must trigger the Phase 15A downgrade-impact workflow before the subscription state changes.
- Feature entitlements must be recalculated from billing state, tier matrix, grace period, support override, and legal hold.

Refund and credit rules:

- Refunds must be request-based or platform-owner initiated; they must not happen silently.
- Refund requests must capture reason, invoice, payment, amount, approving actor, and client communication.
- Partial refunds must create credit notes where legally/accounting required.
- Refunds must not automatically delete operational data.
- Refunds must not change subscription tier unless the refund workflow explicitly includes cancellation, downgrade, or credit.
- Chargebacks/disputes must create billing alerts and may trigger support/legal hold according to policy.

VAT/tax rules:

- South African VAT must be handled through accountant/legal review before paid launch.
- The system must store VAT/tax registration status and VAT/tax number for Vector Ops Group Pty Ltd and each client where applicable.
- The billing engine must support tax-inclusive and tax-exclusive pricing, but the chosen pricing display must be fixed before launch.
- Tax rates must be configuration-driven, not hardcoded in views.
- Tax changes must be effective-dated and audit logged.
- International expansion must support country-specific tax handling later, including VAT/GST/sales tax where applicable.
- The app must not claim tax compliance until reviewed by a qualified accountant/tax advisor.

Billing UI requirements:

- Add `Billing & Subscription` page for company owners/senior administrators.
- Show current plan, subscription status, renewal date, payment method status, invoices, receipts, failed payment warnings, grace period, cancellation state, and export/offboarding actions.
- Show locked feature reason where billing state blocks access.
- Show payment recovery action when a payment fails.
- Show downgrade/cancellation impact before plan changes are confirmed.
- Show tax/VAT details on invoices and billing profile.
- Hide payment-provider technical identifiers from normal users unless needed for support.

Security and audit requirements:

- Payment provider secrets must be stored in Key Vault.
- Webhooks must verify provider signatures.
- Webhook processing must be idempotent.
- Raw webhook payloads must not expose full card data or unnecessary personal data.
- Card details must not be stored in AcuityOps; use provider-hosted payment methods/customer portal where possible.
- Platform support overrides must be audited and time-limited.
- Billing state changes must be linked to subscription entitlements and feature gates.
- Billing logs must be retained according to legal/accounting requirements.

113AS. Create the `BillingProvider` abstraction and do not hardcode business logic directly to Stripe, Paystack, or any single payment provider.
113AT. Select the initial payment provider stack for South Africa/Africa launch using the billing provider decision record template after provider, accountant, and legal review.
113AU. Add billing domain models: customer, plan, subscription, entitlement, invoice, payment, refund, credit note, webhook event, tax profile, support override, and audit event.
113AV. Add subscription state machine covering trialing, active, payment pending, past due, grace period, unpaid, suspended, downgrade pending, cancel at period end, cancelled export available, offboarding requested, deleted, and legal hold.
113AW. Add provider webhook handling with signature verification, idempotency, raw payload hash, replay protection, and failure retry.
113AX. Add invoice creation, invoice PDF storage, invoice download, invoice status, and invoice history.
113AY. Add billing profile fields for legal name, trading name, billing address, country, currency, VAT/tax number, billing contact, and accounts contact.
113AZ. Add VAT/tax configuration with effective dates, tax-inclusive/tax-exclusive pricing support, audit logging, and accounting review gate.
113BA. Add payment failure handling with in-app warning, email notification, optional SMS notification, retry/grace rules, and recovery action.
113BB. Add grace-period rules that preserve daily operations for a defined period but block unpaid expansion of Pro/Premium features.
113BC. Add suspension rules that keep billing, export, cancellation, and support access available.
113BD. Add upgrade workflow with entitlement recalculation and invoice/proration handling.
113BE. Add downgrade workflow linked to Phase 15A impact review and Phase 15B tier matrix.
113BF. Add cancellation workflow linked to Phase 16D export/offboarding.
113BG. Add refund and credit-note workflow with approval, reason, invoice/payment link, audit log, and accounting export state.
113BH. Add chargeback/dispute status handling and legal/support hold workflow.
113BI. Add `Billing & Subscription` page for company owners/senior administrators.
113BJ. Add platform-owner billing admin view with subscription state, invoice status, provider event history, support overrides, and audit history.
113BK. Add provider customer portal link only where the selected provider supports it and it does not bypass AcuityOps downgrade/export/offboarding rules.
113BL. Add manual enterprise invoice mode that still updates the same subscription state machine.
113BM. Add accounting export planning for CSV and later Xero, QuickBooks, or Sage after accountant review.
113BN. Add tests for successful monthly subscription activation.
113BO. Add tests for annual subscription activation.
113BP. Add tests for failed payment, retry/grace period, unpaid status, and suspension.
113BQ. Add tests for upgrade, downgrade, cancellation, refund, credit note, and reactivation.
113BR. Add tests proving provider webhook events cannot be replayed or processed twice.
113BS. Add tests proving feature entitlements update from billing state and do not rely on frontend-only logic.
113BT. Add tests proving billing failure does not delete, corrupt, or resurrect operational data.
113BU. Add invoice PDF verification and tax/VAT field verification.
113BV. Add contract terms for billing periods, renewals, failed payments, grace periods, suspension, refunds, chargebacks, cancellation, export windows, and taxes.
113BW. Add support documentation explaining billing, invoices, payment failure, cancellation, refunds, tax invoices, and enterprise manual billing.
113BX. Add release-blocking verification that billing states, feature gates, downgrade rules, cancellation, export/offboarding, invoices, tax configuration, and audit logs work together.
113BY. Add monitoring and alerts for failed webhook processing, failed invoice creation, repeated payment failures, provider downtime, and billing-state mismatch.
113BZ. Production paid subscriptions are blocked until billing provider integration, invoices, tax review, failed payment handling, refunds, cancellation, downgrade, and entitlement enforcement are verified.

### Phase 16: Azure And Production

114. Move production storage away from SQLite.
115. Add Azure database, blob/file storage, backups, monitoring, and deployment pipeline.
116. Add per-tenant backup and restore strategy.
117. Add production authentication and security hardening.
118. Add environment separation: development, demo, staging, and production.
119. Verify updates can be deployed without damaging client data.

### Phase 16A: Release Isolation And Client-Specific Publishing

The published client-facing platform must be separated from the environment used to build, test, and experiment. A change made during development must never affect live clients until it passes the release process. A client-specific change must be configurable or deployable to that client only, without changing other clients.

Required providers and platform functions:

- `GitHub` source control for protected branches, pull requests, release tags, and deployment history.
- `GitHub Actions` or `Azure DevOps Pipelines` for CI/CD build, test, package, migration, and deploy automation.
- `GitHub Environments` for deployment protection rules, required reviewers, environment secrets, environment variables, and branch/tag deployment restrictions.
- `Azure App Service` or equivalent managed hosting for the AcuityOps web app.
- `Azure App Service Deployment Slots` for staging, production, preview, warm-up, swap, and rollback.
- `Azure SQL Database` or equivalent production database, with tenant-safe schema and migration strategy.
- `Azure Blob Storage` for uploaded logos, documents, schematics, checklist PDFs, import files, and generated reports.
- `Azure Key Vault` for secrets, connection strings, signing keys, API keys, and external service credentials.
- `Azure App Configuration` and Feature Management for feature flags, client-specific feature activation, release rings, kill switches, and targeted rollout.
- `Azure Monitor` and `Application Insights` for logs, errors, performance, release health, and client-impact monitoring.
- `Azure Front Door`, `Azure DNS`, or equivalent edge/custom-domain layer for production domains, client subdomains, TLS, and routing.
- `Entity Framework Core migrations` or equivalent database migration tooling with backup, dry-run, and rollback rules.
- `AcuityOps Tenant Configuration` stored in the database for company branding, logo, colors, feature permissions, release channel, enabled modules, and client-specific UI settings.

119A. Define environments as `local-development`, `shared-development`, `demo`, `staging`, `production`, and optional `client-dedicated-production`.
119B. Local development must use local/dev data only and must not connect to production client data.
119C. Demo must use demo data only and must never be treated as a live client.
119D. Staging must mirror production configuration closely enough to test real releases without exposing live client operations.
119E. Production must be the only environment used by paying clients.
119F. A client-specific UI or workflow change must first be represented as tenant configuration, branding configuration, permissions, checklist templates, feature flags, or release-channel configuration before code branching is considered.
119G. Tenant configuration must support per-client branding, terminology, logo, color accents, enabled modules, allowed workflows, default checklist behavior, reporting options, and AI/import availability.
119H. Feature flags must support global rollout, tier-based rollout, client-specific rollout, role-specific rollout, and emergency disable.
119I. A change for one client must be tested against that client in staging or a client preview slot before production activation.
119J. If a client-specific change requires code, it must be deployed through a dedicated release channel or dedicated client production instance, not silently into shared production.
119K. A release must be traceable from code commit to build artifact, deployment environment, tenant activation, and audit log.
119L. Production releases must use a staging slot, pre-swap validation, health checks, and rollback plan before production swap.
119M. Client-specific production activation must include a written release note naming the client, affected modules, feature flags, data migrations, rollback plan, and verification evidence.
119N. Database migrations must be compatible with existing tenants. Destructive migrations require backup, migration report, and explicit release approval.
119O. Tenant-specific assets such as logos, schematics, PDF templates, and report templates must be stored as client-owned configuration/assets, not hardcoded in source.
119P. The platform must support shared multi-tenant hosting for normal clients and dedicated deployment for larger Premium clients that require stronger isolation.
119Q. Dedicated client deployments must still use the same product source code and release discipline unless an explicit enterprise contract requires a fork.
119R. Build verification must prove that a change can be activated for one test client without changing another test client.
119S. Build verification must prove that a production-like release can be staged, verified, swapped, and rolled back without data loss.
119T. No direct manual edits to production files, production database records, production storage assets, or production configuration are allowed outside a controlled admin interface or audited deployment process.

### Phase 16B: Mandatory Security And Client Data Protection Sweeps

Client data safety is a non-skippable architecture requirement. No real client may be onboarded to production until this phase passes. This phase protects company data, staff personal information, medical qualification records, licensing and CPD records, asset registers, medication records, stock levels, checklists, issue reports, task history, uploaded files, generated PDFs, AI import files, AI analysis outputs, notification contact details, and audit logs.

Required providers and platform functions:

- `Microsoft Entra ID` or the selected production identity provider for authentication, administrative identity, MFA, conditional access, and platform-owner access control.
- `Azure Managed Identity` for the AcuityOps web app, background jobs, deployment slots, and internal services to access Azure resources without hardcoded credentials.
- `Azure Key Vault` for database credentials where needed, API keys, AI provider keys, notification provider keys, signing keys, certificate material, and sensitive configuration.
- `Azure SQL Database` for production tenant data, with tenant-scoped schema rules, encryption, backups, auditing, and security monitoring.
- `Azure Blob Storage` for uploaded documents, logos, schematics, import files, generated PDFs, exports, and report artifacts.
- `Azure Private Link` and private endpoints for production access to Azure SQL, Blob Storage, Key Vault, and App Configuration where the chosen hosting tier supports it.
- `Azure Front Door` with `Web Application Firewall` for public ingress, TLS, managed WAF rules, rate limits, IP restrictions where needed, and edge logging.
- `Microsoft Defender for Cloud` for cloud security posture management, workload protection, App Service protection, SQL protection, Storage protection, Key Vault protection, API protection, and DevOps security visibility.
- `Azure Monitor`, `Application Insights`, and `Log Analytics` for application errors, performance, security events, audit events, failed authentication attempts, notification delivery failures, AI provider failures, and operational alerts.
- `Azure SQL Auditing` for database access events, query activity, admin activity, suspicious changes, and retained audit evidence.
- `Azure Storage blob versioning`, soft delete, lifecycle management, and private containers for accidental deletion recovery and controlled retention.
- `Azure Policy` for production guardrails such as required diagnostics, no public storage containers, mandatory encryption, approved regions, resource tagging, and private endpoint requirements.
- `GitHub Advanced Security`, `GitHub secret scanning`, `CodeQL`, `Dependabot`, or equivalent DevSecOps tooling for source code, dependency, and secret scanning.
- `OWASP ASVS` as the application security verification baseline for authentication, authorization, session handling, input validation, file uploads, API security, logging, error handling, and configuration.
- `Microsoft Sentinel` or equivalent SIEM is required for enterprise or Premium clients that need centralized security monitoring, SOC integration, or regulated security reporting.

Service linking model:

- The AcuityOps web app must use managed identity to read secrets from Key Vault, read feature flags from App Configuration, write telemetry to Application Insights, access Blob Storage, and connect to Azure SQL where supported.
- Key Vault must not be bypassed by hardcoded secrets in source code, appsettings files, database records, scripts, or deployment logs.
- Azure SQL records must carry `CompanyId` or the approved tenant identifier on every tenant-owned business table. All application queries must enforce tenant scope before data is returned.
- Blob Storage files must be tenant-scoped by container, path prefix, or metadata policy. Public blob containers are not allowed for client data.
- Front Door and WAF must be the public entry point for production traffic when production is deployed on Azure. Direct public access to backend services must be blocked or restricted where practical.
- Logs, audits, and security alerts must flow into Log Analytics. Critical alerts must create a visible operational/security review item for the platform owner.
- AI import and AI analysis jobs must use tenant-scoped input files, tenant-scoped output records, and tenant-scoped prompt logs. Client data must not be used for model training unless the client contract explicitly allows it.

119U. Add this security phase as a formal launch gate before any real client data is loaded into production.
119V. Classify protected data types: tenant identity data, staff PII, medical qualification records, licensing records, CPD records, operational registers, medication records, uploaded documents, generated PDFs, checklist evidence, task and issue records, audit logs, AI import files, AI outputs, and notification contact details.
119W. Implement tenant isolation so every tenant-owned business record is scoped by company/tenant identifier and cannot be read, written, exported, reported, or deleted across tenant boundaries.
119X. Add automated tenant-isolation tests proving that one company cannot see another company's users, assets, documents, checklists, reports, AI files, notifications, or audit logs.
119Y. Implement least-privilege production authentication. Platform owner, company owner, senior manager, operational manager, and staff access must be separate and auditable.
119Z. Require MFA for platform-owner accounts and production administrative accounts. Require MFA for company-owner and senior-management accounts when the production identity provider supports it.
119AA. Use managed identity and Key Vault for production secrets. No production secret may be committed to Git, stored in plain text, copied into a database row, or embedded in a client asset.
119AB. Configure Azure SQL encryption, backups, point-in-time restore, auditing, Defender for SQL, and least-privilege database access before production onboarding.
119AC. Configure Blob Storage private access, tenant scoping, soft delete, versioning, lifecycle policy, malware/security scanning where available, and no public container access.
119AD. Configure WAF managed rules, TLS-only access, rate limiting, secure headers, and request logging before exposing the production app publicly.
119AE. Configure Private Link/private endpoints for SQL, Storage, Key Vault, and App Configuration where the selected production tier supports it.
119AF. Configure Azure Monitor, Application Insights, Log Analytics, and alert rules for app crashes, failed logins, permission failures, database errors, storage failures, notification failures, AI job failures, and suspicious activity.
119AG. Configure Defender for Cloud and treat critical security recommendations as release blockers.
119AH. Add DevSecOps scanning to pull requests and release branches: secret scanning, dependency scanning, static code scanning, and vulnerable package detection.
119AI. Apply OWASP ASVS checks for authentication, authorization, session handling, CSRF, XSS, SQL injection, file upload validation, access control, logging, error handling, and secure configuration.
119AJ. Add explicit file-upload security rules: allowed file types, maximum size, virus/security scan where available, tenant-scoped storage path, no executable uploads, no public links, and short-lived authenticated download access only.
119AK. Add AI privacy controls: tenant-scoped prompts, no cross-tenant examples, no use of client data for training unless contractually approved, redacted logs where practical, retention limits for uploaded import files, and audit logging for AI jobs.
119AL. Add notification privacy controls: SMS and email must avoid sensitive clinical or personal detail, links must require authentication, opt-out/suppression rules must be respected, and delivery attempts must be audit logged.
119AM. Add platform-owner admin safeguards: audited admin access, least-privilege support tools, break-glass account procedure, support-session reason capture, and no silent tenant data browsing.
119AN. Add backup and restore drills for Azure SQL and Blob Storage. The product must prove restore capability before production launch.
119AO. Define RPO and RTO targets for Base, Pro, Premium, and enterprise/dedicated deployments.
119AP. Add data retention, export, and deletion controls that can support POPIA, GDPR, and similar privacy obligations. Do not claim certification until certification is completed.
119AQ. Add security incident response procedure: detection, triage, containment, credential rotation, tenant isolation, client notification, evidence preservation, and post-incident review.
119AR. Add a production security dashboard or platform-owner checklist showing unresolved critical alerts, last backup status, last restore test, WAF status, Defender status, and scan status.
119AS. Add release-blocking security verification before every production release that changes authentication, permissions, tenant filtering, file uploads, AI processing, notification delivery, reporting, or database schema.
119AT. Add tenant-specific security verification before any client-specific production activation.
119AU. Verify that a client-specific UI or feature change cannot expose another client's data, files, reports, branding, checklists, schematics, notifications, or audit logs.
119AV. Verify that deleted/retired client records are not resurrected by seed data, startup routines, repair routines, imports, AI jobs, or fallback rendering code.
119AW. Verify that all sensitive actions create audit records: login, failed login, permission change, asset movement, checklist publish, checklist retire/delete, report export, file upload, file delete, AI import, AI report generation, notification send, and admin support access.
119AX. Run a pre-production penetration/security review before public launch.
119AY. Production onboarding is blocked until security verification passes and the platform owner signs the launch security checklist.
119AZ. Security controls must be rechecked after each major architecture change, Azure deployment change, identity-provider change, AI-provider change, notification-provider change, or tenant-isolation change.

### Phase 16C: Mandatory Legal, Compliance, Evidence, And IP Review

This phase is a non-skippable legal review gate. It must be completed with qualified legal professionals before public launch, paid subscriptions, production onboarding, AI processing of real client files, or any client contract. AcuityOps must not rely on informal assumptions about privacy law, medical operations, digital evidence, SaaS liability, trademarks, patents, or international expansion.

Required professional review:

- A South African technology/commercial lawyer for SaaS terms, limitation of liability, service levels, subscription contracts, client onboarding terms, acceptable use, support obligations, payment terms, cancellation terms, and dispute resolution.
- A privacy/data-protection lawyer for POPIA, Information Regulator obligations, data processing roles, operator agreements, cross-border transfers, breach notification, direct marketing, data retention, data subject rights, and AI file-processing rules.
- An intellectual-property lawyer or patent attorney for AcuityOps naming, trademark registration, logo ownership, code ownership, invention/patent strategy, trade secret strategy, copyright notices, contractor IP assignment, and international IP filing strategy.
- A litigation/evidence lawyer for audit logs, digital records, PDF evidence, checklist submissions, issue reports, user identity records, tamper-evidence, record retention, and whether AcuityOps evidence is likely to be useful in disputes, insurance matters, regulatory reviews, labour matters, or civil claims.
- A healthcare/EMS regulatory advisor where required for South Africa and later target markets, especially where operational reports, readiness scoring, medication records, staff qualification records, or AI analysis may be interpreted as clinical or compliance advice.
- A tax/accounting advisor for subscription billing, VAT, international sales, invoicing, refunds, marketplace/payment-provider duties, and multi-currency expansion.

Legal reference areas to review:

- `POPIA` because AcuityOps processes personal information, staff records, contact details, uploaded documents, licensing/CPD information, operational records, and potentially sensitive information.
- `Electronic Communications and Transactions Act` because AcuityOps relies on electronic records, electronic communications, digital audit trails, PDFs, and electronic transaction flows.
- `Trade Marks Act` and CIPC trademark processes because the product name, logo, and brand must be protected before serious marketing.
- `Patents Act`, design protection, copyright, and trade secret strategy because some AcuityOps workflow ideas, UI structures, scoring logic, AI import mapping, and readiness analytics may be protectable or may need deliberate trade-secret treatment instead of patenting.
- Client contract law, consumer protection where applicable, limitation of liability, service availability, support terms, data loss terms, backup commitments, and disclaimer wording.
- Employment and labour-law implications where staff profiles, task feedback, incident reports, competence records, and document records may be used by clients in HR processes.
- Insurance and medico-legal evidence expectations where readiness reports, checklist PDFs, audit logs, and incident records may be used after operational failures or disputes.

119BA. Book a formal legal review before launch. This review must be recorded as a release dependency, not a future reminder.
119BB. Prepare a legal briefing pack containing the product description, tier model, data model, user roles, access levels, AI import flow, AI analysis flow, notification flow, audit-log flow, PDF evidence flow, storage architecture, client onboarding flow, and planned target countries.
119BC. Ask the privacy lawyer to classify AcuityOps as responsible party, operator, or both in each data flow, and document the exact POPIA role for platform owner, client company, staff user, and external providers.
119BD. Create a POPIA compliance matrix covering lawful basis, purpose specification, data minimisation, consent/contract/legal basis, data subject requests, retention, security safeguards, breach notification, operator agreements, and cross-border transfers.
119BE. Draft and review the client Data Processing Agreement or operator agreement before any production client data is uploaded.
119BF. Draft and review privacy policy, cookie policy if needed, data retention policy, breach response policy, AI data-use policy, support-access policy, and deletion/export policy.
119BG. Ask the lawyer whether SMS and email notification wording must avoid specific operational, medical, personal, or disciplinary content. Update notification templates accordingly.
119BH. Ask the litigation/evidence lawyer to define what must be captured for digital evidence: user identity, timestamp, time zone, IP/device/session where lawful, checklist version, submitted values, changed values, approval history, PDF hash, file hash, audit-log chain, and retention period.
119BI. Add evidence requirements to the product architecture: immutable audit events, report/PDF generation record, file hash, checklist template version, user role at time of action, company/tenant ID, and export history.
119BJ. Define retention rules with legal input for checklist evidence, audit logs, issue reports, task records, staff qualification records, licenses, CPD records, uploaded documents, AI import files, AI analysis outputs, and notification delivery records.
119BK. Draft and review SaaS Terms of Service covering subscription tiers, uptime commitments, support boundaries, implementation/onboarding, payment, cancellation, client responsibility for data accuracy, acceptable use, data ownership, and termination export.
119BL. Draft and review limitation-of-liability language. This must clarify that AcuityOps supports operational readiness management but does not replace client clinical governance, legal compliance, vehicle inspections, equipment servicing, or professional judgement.
119BM. Draft and review AI disclaimers. AI import, AI mapping, future analysis, predictive reporting, and compliance analysis must be positioned as decision-support, not as guaranteed legal, clinical, financial, or regulatory advice.
119BN. Ask an IP lawyer to run name and logo clearance for `AcuityOps`, `Vector Ops Group Pty Ltd`, associated marks, domains, app names, and future international markets.
119BO. File or plan trademark registration before serious public marketing. The plan must include South Africa first and later target markets such as Africa, UK, Australia, USA, and other expansion regions.
119BP. Ask a patent attorney whether any part of the readiness engine, AI register import, checklist-to-register reconciliation, variance approval workflow, or predictive compliance analytics should be patented, kept as a trade secret, or left unpatented.
119BQ. Ensure all generated schematics, logos, visual assets, text, code, contractor work, AI-created assets, and third-party assets have clear ownership or license rights before commercial use.
119BR. Add contractor and contributor IP assignment templates before any outside developer, designer, lawyer, consultant, or data-entry contractor contributes product assets.
119BS. Ask the lawyer to review international expansion risks: POPIA/GDPR-style privacy requirements, data residency promises, EMS/health data sensitivity, direct marketing rules, SMS/email consent rules, local consumer/subscription law, and local IP filing.
119BT. Create a legal launch checklist. Production launch is blocked until required policies, client contracts, DPA/operator agreement, IP plan, evidence-retention rules, privacy notices, liability language, and security obligations are reviewed and signed off.

### Phase 16D: Client Data Portability, Export, And Offboarding

Clients must never feel trapped in AcuityOps. A reliable, visible, and legally reviewed pathway must exist for a client to export, download, verify, and request removal of all client-owned data when they downgrade, cancel, migrate, or end the subscription.

This is a trust feature and a legal/commercial safety feature. It must be built as part of the core production architecture, not as a later support task. It must support POPIA, GDPR-style portability and erasure principles, client contracts, audit evidence retention, and practical EMS record-keeping needs.

The product promise must be precise:

- Clients can export all client-owned operational data in usable formats.
- Clients can export all original uploaded files and generated evidence files.
- Clients can export a machine-readable package for migration.
- Clients can request account closure and deletion of client data.
- AcuityOps must clearly disclose any legally required retention, backup retention window, audit-log retention, or legal-hold exception before deletion begins.
- AcuityOps must not keep active client data, AI embeddings, search indexes, uploaded files, generated PDFs, or tenant configuration after offboarding unless a documented legal, contractual, billing, tax, dispute, or security-retention reason applies.

Required providers and platform functions:

- `Azure SQL Database` export support for schema/data archive, tenant-scoped data extraction, and production-safe export jobs.
- `SqlPackage` or equivalent export tooling for transactionally consistent database exports where a technical database package is required.
- `Azure Blob Storage` export support for uploaded logos, documents, schematics, import files, generated PDFs, evidence files, SOP/source files, and report artifacts.
- `Azure Blob soft delete`, versioning, lifecycle management, and point-in-time restore controls configured so accidental deletion can be recovered during the retention window, while final deletion remains possible after the legal retention window.
- `Azure Storage SAS` or short-lived authenticated download links for secure export package download.
- `Azure Service Bus`, `Azure Storage Queue`, or equivalent background processing for long-running export, packaging, hashing, and deletion jobs.
- `Azure Key Vault` for export encryption keys and signing keys.
- `Azure Monitor` and `Application Insights` for export job failures, deletion job failures, download errors, and support escalation.
- `AcuityOps Admin Console` for platform-owner offboarding controls with strict audit logging and no silent manual deletion.
- `Client Offboarding Portal` for authorized senior/company-owner users to request export, review package status, download packages, request deletion, and receive completion certificates.

Required export package:

- A single downloadable ZIP or encrypted archive containing all selected tenant data.
- `manifest.json` listing tenant ID, company name, export date/time, requesting user, export scope, included tables, included file counts, hash values, package version, and exclusions.
- `readme.pdf` and `readme.html` explaining the folder structure, formats, legal notes, and how to open the export.
- `database/` folder containing machine-readable JSON and CSV exports for every tenant-owned table.
- `spreadsheets/` folder containing XLSX exports for non-technical users where practical.
- `pdf-evidence/` folder containing generated checklist PDFs, audit packs, reports, and evidence exports.
- `uploads/` folder containing original uploaded files, staff documents, company documents, SOPs, guidelines, import files, logos, schematics, and other client files.
- `registers/` folder containing vehicle, staff, equipment, stock, medication, bases, operational areas, storage locations, supplier records, and assignment records.
- `checklists/` folder containing checklist templates, published scopes, submitted checklist evidence, checklist version history, variance alerts, and PDF evidence.
- `readiness/` folder containing readiness engine rules, dashboard history, metric drilldown exports, scoring requests, approvals, and audit notes.
- `operations/` folder containing tasks, issues, task feedback, issue resolutions, operational reports, asset movements, stock orders, supplier confirmations, service/expiry pressure data, and notifications.
- `knowledge/` folder containing SOP/guideline source documents, extracted structures, citations, published versions, acknowledgements, and search index references where the higher-tier module is enabled.
- `ai/` folder containing AI import mappings, approved import decisions, AI analysis reports, AI forecast reports, model/version metadata, and source citations. It must not include provider secrets or internal prompt secrets.
- `audit/` folder containing tenant audit logs, export history, deletion request history, permission changes, and admin access events, subject to legal retention rules.
- Optional `database.bacpac` for enterprise clients or technical migrations, where supported and transactionally safe.

Required deletion/offboarding workflow:

1. Authorized company owner or senior administrator requests data export.
2. System verifies tenant, role, permissions, MFA where enabled, and billing/offboarding status.
3. System shows export scope and explains what will be included.
4. System starts background export job.
5. System locks export package contents to a consistent timestamp.
6. System packages database data, Blob files, generated PDFs, AI artifacts, vector/search metadata, and audit metadata.
7. System generates hashes for package integrity.
8. System creates secure time-limited download links.
9. System records download events.
10. Authorized user confirms they have downloaded and verified the export.
11. User requests deletion/offboarding.
12. System shows legal retention exceptions, backup retention window, billing retention, tax retention, dispute retention, and audit-log retention before deletion.
13. User confirms deletion request with clear in-app confirmation and secondary verification.
14. System disables normal user access for that tenant.
15. System queues deletion of active tenant data from application tables.
16. System queues deletion of tenant files from Blob Storage.
17. System queues deletion of search indexes, vector stores, embeddings, AI extracted artifacts, cached imports, temporary files, and generated previews.
18. System removes tenant-specific feature flags, branding, deployment configuration, notification templates, provider settings, and integration credentials.
19. System marks legally retained records as retained-only, inaccessible through normal app UI, and available only through audited platform-owner legal/security process.
20. System produces an offboarding certificate showing export completion, deletion request, deletion completion, retained exception categories, backup expiry window, and responsible platform actor.

Data that must be exportable:

- Company profile, branding, subscription configuration, tier, feature gates, setup wizard progress, and tenant settings.
- Staff records, access records, qualification/scope, practitioner numbers, licensing dates, CPD status, documents, permissions, manager assignments, and profile history.
- Vehicle records, function/subtype, callsign history, schematic assignments, register history, movement history, service/licence data, and checklist evidence.
- Equipment, stock, medication, supplier, storage, base, and operational area records.
- Checklist templates, builder structures, row/column definitions, subitems, published scopes, template versions, submitted evidence, generated PDFs, and variance alerts.
- Readiness engine rules, scoring requests, approvals/rejections, dashboard history, metric drilldowns, and active rule versions.
- Issues, tasks, task feedback, notifications, audit logs, reports, operational reports, stock order flow, supplier confirmations, and asset movements.
- Uploaded files, generated files, SOP/guideline documents, AI imports, AI reports, AI forecasts, compliance packs, and audit exports.

Data removal must include:

- Application database tenant records.
- Blob files and versions when retention allows.
- Search indexes and vector embeddings.
- AI provider file stores/vector stores where used.
- Temporary extraction files.
- Cached previews.
- Tenant-specific secrets and integration credentials.
- Notification templates and queued messages.
- Demo or staging tenant copies where those copies contain real client data.
- Client-specific deployment configuration and feature flag targeting.

Retention and legal exception rules:

- AcuityOps must not promise immediate irreversible deletion where backups, legal hold, tax records, billing records, fraud/security logs, or dispute evidence must be retained.
- Any retained data must be minimal, documented, access-controlled, and excluded from normal app use.
- The client must see what is retained, why, for how long, and who can access it.
- Backup deletion must follow a documented retention window and must not restore offboarded tenant data into active production.
- Audit logs may need retention for platform security and dispute evidence, but must be tenant-scoped, access-controlled, and legally reviewed.
- Offboarding terms must be included in the SaaS contract and privacy/data-processing documents.

119BU. Add `Client Export And Offboarding` as a production architecture requirement before paid launch.
119BV. Add tenant-scoped export APIs and background jobs that can export all client-owned data without mixing tenants.
119BW. Add export package generation with JSON, CSV, XLSX where practical, PDFs, original uploads, generated evidence files, manifest, hashes, and readme files.
119BX. Add optional BACPAC/database export for enterprise or technical migration scenarios where supported and safe.
119BY. Add a client-facing export portal for company owners/senior administrators.
119BZ. Add role and MFA verification before export or deletion requests.
119CA. Add export status tracking: requested, processing, ready, downloaded, expired, failed, cancelled, and verified.
119CB. Add secure time-limited download links and download audit logs.
119CC. Add deletion/offboarding request workflow with clear legal-retention disclosures before confirmation.
119CD. Add active tenant access suspension after confirmed offboarding request.
119CE. Add active tenant data deletion from application tables after export verification and final confirmation.
119CF. Add Blob Storage deletion for tenant files, versions, generated PDFs, uploads, imports, SOPs, schematics, and evidence files where retention allows.
119CG. Add deletion of AI/vector/search artifacts, including embeddings, vector stores, search indexes, extraction caches, AI file stores, and temporary processing files.
119CH. Add deletion of tenant-specific secrets, integration credentials, notification templates, feature flags, branding, and deployment targeting.
119CI. Add retained-data classification: legal hold, billing/tax, security, dispute, audit-log retention, backup-retention window, or regulator retention.
119CJ. Add offboarding certificate showing export completion, deletion completion, retained exceptions, backup expiry window, and responsible platform actor.
119CK. Add admin-only offboarding console with audited actions, no silent manual deletion, and no ability to skip export verification unless legally required.
119CL. Add contract/privacy wording for export, deletion, retention exceptions, backup windows, and offboarding support timelines.
119CM. Add automated tests proving tenant A export cannot include tenant B data.
119CN. Add automated tests proving deletion removes active tenant data, files, search/vector artifacts, and feature targeting.
119CO. Add restore tests proving an old backup cannot silently resurrect offboarded tenant data into active production.
119CP. Add monitoring and alerts for failed export jobs, failed deletion jobs, expired download packages, and retained-data review deadlines.
119CQ. Add cost controls for large export packages, storage, long-running jobs, and enterprise offboarding support.
119CR. Add support documentation explaining how clients export data, read package contents, request deletion, and understand retention exceptions.
119CS. Verify offboarding with a realistic tenant containing staff, vehicles, equipment, stock, medication, checklists, PDFs, uploads, reports, AI imports, SOPs, vector/search artifacts, notifications, and audit logs.
119CT. Production launch is blocked until export, deletion, retained-data handling, tenant isolation, and offboarding certificates are verified.

### Phase 16E: Tenant Isolation And Multi-Client Provisioning Decision Record

AcuityOps must have a written tenant isolation and multi-client provisioning decision record before production client onboarding, Azure deployment, paid subscriptions, AI import, SMS/email activation, or client-specific release work. This decision record must define how a client becomes a tenant, how tenant identity is resolved from URLs and logins, how data is isolated, how storage is isolated, how backups are taken and restored, and how client-specific releases are activated without affecting other clients.

This decision record exists because AcuityOps is a multi-client EMS SaaS platform. A client must never see, edit, export, search, restore, receive notifications for, or inherit another client's data, branding, checklists, schematics, reports, billing state, AI files, or release configuration.

Tenant definition:

- A product tenant is one subscribed client organization/company using AcuityOps.
- A tenant is not the same thing as a staff user, access level, operational area, base, or Microsoft Entra tenant.
- A single tenant may have many staff users, senior managers, operational managers, areas, bases, vehicles, documents, registers, checklists, billing records, and notification settings.
- A large Enterprise client may require a dedicated deployment, but it is still represented by the same AcuityOps tenant model.
- Every tenant must have a stable `TenantId` or `CompanyId` that is generated by the platform and is never typed manually by the client as free text.

Decision record fields:

- `Decision ID`: unique architecture decision identifier.
- `Decision status`: proposed, approved, rejected, deferred, superseded.
- `Decision date`: date of review.
- `Decision owner`: person accountable for the decision.
- `Reviewers`: platform owner, architect/developer, security reviewer, legal/privacy reviewer, and commercial reviewer.
- `Launch stage`: local, demo, staging, first paid South African client, African rollout, global rollout, Enterprise dedicated deployment.
- `Tenant definition`: exactly what counts as a tenant in AcuityOps.
- `Tenant identifier`: approved ID format, immutability rules, display name rules, workspace slug rules, and migration rules.
- `Tenant resolution strategy`: how the app resolves tenant from workspace link, custom domain, subdomain, login flow, invite link, API key, webhook, background job, and support admin action.
- `Domain strategy`: shared domain, tenant subdomain, custom domain, dedicated Enterprise domain, TLS certificate approach, DNS provider, and fallback route.
- `Database strategy`: shared database with tenant columns, separate schema per tenant, database per tenant, deployment stamp, or dedicated Enterprise database.
- `Storage strategy`: shared storage account with tenant prefixes/containers, storage account per tenant, dedicated Enterprise storage, private access, lifecycle rules, soft delete, and versioning.
- `Search/vector strategy`: shared index with tenant filters, index per tenant, vector store per tenant, AI file store per tenant, and deletion rules.
- `Cache/session strategy`: tenant-safe cache keys, session tenant binding, cross-tab/login behavior, and cache invalidation.
- `Background job strategy`: tenant-scoped queue messages, import jobs, export jobs, PDF jobs, notification jobs, AI jobs, and retry handling.
- `Notification strategy`: tenant-scoped email/SMS templates, sender settings, provider settings, quiet hours, and cost attribution.
- `Billing strategy`: tenant subscription state, invoice tenant, billing customer ID, plan entitlements, grace period, cancellation state, and export/offboarding access.
- `Release strategy`: global release, feature flag, tenant-specific feature flag, release ring, preview slot, dedicated client deployment, rollback path.
- `Client-specific customization strategy`: branding, logo, terminology, modules, checklists, schematic assignments, permissions, templates, report wording, and UI variations.
- `Backup strategy`: point-in-time restore, tenant export, tenant restore, whole database restore, per-tenant restore limitations, backup retention, and restore test frequency.
- `Disaster recovery strategy`: region, RPO, RTO, deployment stamp failover, storage redundancy, database failover, and client communication plan.
- `Provisioning workflow`: who can create tenants, required setup wizard steps, default configuration, billing state, invite creation, company login link, and initial owner account.
- `Deprovisioning/offboarding workflow`: export, suspension, deletion, retained data, backup expiry, billing/tax retention, and offboarding certificate.
- `Support access model`: platform-owner support access, break-glass access, reason capture, time limits, tenant-scoped views, and audit logs.
- `Migration strategy`: moving a tenant from shared to dedicated, dedicated to shared, one region to another, or one provider setup to another.
- `Known blockers`: unresolved cost, performance, legal, data residency, migration, backup, restore, release, or client-specific publishing risks.
- `Go/no-go decision`: selected tenant/deployment strategy and reason.
- `Required proof before launch`: tenant isolation tests, domain tests, database tests, storage tests, backup/restore tests, release flag tests, client-specific release tests, and support access tests.

Tenant isolation model candidates:

- `Shared app, shared database, tenant ID on every tenant-owned row`: lowest cost and simplest early SaaS model, but requires strict query filters, automated isolation tests, and no fallback seed/default data.
- `Shared app, database per tenant`: stronger data isolation and easier tenant restore, but more operational complexity and higher cost.
- `Deployment stamp`: multiple shared deployments, each hosting a group of tenants, useful for regional scaling, large customers, or noisy-neighbor separation.
- `Dedicated Enterprise deployment`: strongest isolation for large Premium/Enterprise clients, highest cost, must still follow the same source code, release discipline, billing state machine, export/offboarding workflow, and security controls.
- `Hybrid`: Base/Pro clients on shared tenancy, Premium/Enterprise clients optionally on deployment stamp or dedicated deployment.

Tenant identifier rules:

- `TenantId` is the internal source of truth for tenant scoping.
- Workspace slugs, company names, custom domains, display names, billing names, and logos are not security boundaries.
- Every tenant-owned table must include `TenantId` or the approved tenant key unless the table is explicitly global product configuration.
- Global product configuration must be read-only or platform-owned and must never contain client data.
- Audit logs must include tenant, user, access level, action, target entity, timestamp, and source.
- Tenant changes must never be handled by string matching company names.

Domain and routing rules:

- Public marketing website and production app must be separated.
- Company login links must resolve tenant safely without exposing another tenant.
- Tenant subdomains or workspace links must map to exactly one active tenant.
- Custom domains must be verified before activation.
- TLS certificates must be managed through the approved platform provider.
- Domain changes must be audit logged and must not change `TenantId`.
- If tenant resolution fails, the app must show AcuityOps branding and a safe generic error, not another client's branding.

Database strategy rules:

- Production must not use SQLite.
- Shared database tenancy requires automated checks that every tenant-owned query is scoped.
- Dedicated database tenancy requires automated provisioning, migrations, backups, monitoring, and cost allocation.
- Migrations must be backward compatible for existing tenants.
- A migration must not create seed company data, seed checklists, seed vehicles, seed staff, seed assets, seed logos, or seed schematic assignments in production.
- Restore workflows must not resurrect offboarded tenants or stale seed data.

Storage isolation rules:

- Client-owned files must be stored under tenant-scoped containers, prefixes, metadata, or dedicated storage accounts.
- Client-owned files include logos, uploaded documents, imported registers, checklists, PDFs, schematic assignments, AI import files, SOPs/guidelines, exports, and evidence files.
- Product-owned schematic library assets may be global, but tenant assignments to schematics are tenant-owned records.
- Public blob containers are not allowed for client-owned files.
- Blob soft delete, versioning, lifecycle rules, and deletion/offboarding behavior must be documented.

Backup and restore rules:

- Backups must protect the platform without violating tenant deletion/offboarding promises.
- Whole-database restore must include a process for preventing one restored tenant from overwriting active state for other tenants.
- Tenant-specific restore must be supported or the limitation must be explicit before paid launch.
- Backup retention must align with legal, billing, tax, security, and client contract rules.
- Restore tests must include tenant isolation verification after restore.

Client-specific release rules:

- Client-specific UI changes should first be implemented as tenant configuration, feature flags, templates, permissions, branding, or release-ring settings.
- Code-level client-specific changes require a dedicated release channel or dedicated deployment.
- A release for one tenant must not activate for another tenant unless explicitly targeted.
- Every release activation must be traceable to commit, build, environment, tenant, feature flag, actor, time, and verification evidence.
- Rollback must be defined before activation.

Decision record scoring:

- Score each model from 0 to 5 for security isolation, cost, implementation complexity, operating complexity, backup/restore quality, migration complexity, release safety, client-specific customization, compliance readiness, regional expansion, and Enterprise fit.
- Any model with a zero for tenant isolation, backup/restoration feasibility, production security, or offboarding compatibility must not be used for production.
- Shared tenancy may be selected for early Base/Pro launch only if automated tenant-isolation tests are mandatory release gates.
- Dedicated tenancy may be sold only if provisioning, monitoring, backups, release, support access, billing, and offboarding are automated enough to operate safely.
- The decision record must be revisited before launching a new country, adding dedicated Enterprise deployments, changing database topology, changing domain strategy, changing storage isolation, or activating AI features for real client data.

119CU. Create the tenant isolation and multi-client provisioning decision record before production Azure implementation starts.
119CV. Define the AcuityOps tenant model and confirm that tenant is the client organization, not a user, access level, base, area, or Microsoft Entra tenant.
119CW. Define immutable `TenantId` or approved tenant key rules and prohibit company-name string matching as a security boundary.
119CX. Define tenant resolution from workspace link, subdomain, custom domain, login flow, invite link, webhook, background job, and support admin access.
119CY. Choose initial domain strategy for first launch: shared app URL, tenant workspace link, tenant subdomain, custom domain, or hybrid.
119CZ. Choose database tenancy strategy for Base/Pro launch and document when a tenant moves to deployment stamp or dedicated database.
119DA. Choose Blob Storage isolation strategy for client-owned files and separate product-owned global schematic assets from tenant-owned schematic assignments.
119DB. Choose search/vector/AI file isolation strategy before AI import or AI analysis uses real client data.
119DC. Add tenant-safe cache, session, and background job scoping rules.
119DD. Add tenant-scoped notification, billing, and subscription-state rules.
119DE. Add tenant provisioning workflow: create tenant, assign billing state, generate workspace link, start setup wizard, create owner account, configure branding, invite staff, and verify readiness.
119DF. Add tenant deprovisioning workflow linked to Phase 16D export/offboarding and Phase 15C billing state.
119DG. Add tenant support-access workflow with break-glass controls, reason capture, least privilege, and audit logging.
119DH. Add tenant migration workflow for shared-to-dedicated, dedicated-to-shared, deployment stamp moves, region moves, and client-specific release moves.
119DI. Add backup and restore architecture that documents point-in-time restore, tenant-specific export/restore, whole-database restore risk, backup retention, and offboarding impact.
119DJ. Add restore tests proving a restored backup cannot leak, resurrect, overwrite, or cross-link another tenant's data.
119DK. Add domain and TLS tests for workspace links, subdomains, custom domains, invalid tenant links, and safe pre-login AcuityOps branding.
119DL. Add database tests proving every tenant-owned table and query uses the approved tenant key.
119DM. Add storage tests proving tenant-owned files cannot be accessed through another tenant.
119DN. Add AI/search tests proving tenant A files, embeddings, indexes, prompts, outputs, and reports cannot appear in tenant B.
119DO. Add notification tests proving tenant-specific sender settings, templates, recipients, and delivery logs cannot cross tenants.
119DP. Add billing tests proving subscription state, invoices, refunds, grace periods, and export/offboarding access are tenant-scoped.
119DQ. Add client-specific release tests proving one tenant can receive a feature, UI variation, schematic assignment, checklist behavior, or report template without changing another tenant.
119DR. Add rollout and rollback rules for client-specific releases.
119DS. Add platform-owner tenant registry showing tenant ID, workspace slug, domain, deployment, database, storage scope, billing state, release ring, setup status, and support state.
119DT. Add tenant cost-attribution fields for storage, AI usage, SMS usage, exports, dedicated deployments, and enterprise support.
119DU. Add tenant provisioning audit events for creation, activation, branding changes, domain changes, owner changes, billing state, feature flags, release activation, support access, export, and deletion.
119DV. Verify shared multi-tenant hosting with at least two test tenants containing similar names, similar callsigns, similar staff names, and overlapping asset identifiers.
119DW. Verify dedicated tenant deployment path if Enterprise/dedicated hosting is offered.
119DX. Verify that seed data, fallback rendering, startup routines, repair routines, imports, and demo data cannot create or restore tenant records in production.
119DY. Add decision-record review as a production launch blocker and as a blocker before adding any real client data.
119DZ. Production multi-client launch is blocked until tenant decision record, provisioning workflow, isolation tests, release targeting, backup/restore, support access, and offboarding behavior are verified.

### Phase 16F: Production Observability And Incident Response Decision Record

AcuityOps must have a written production observability and incident-response decision record before public launch, paid subscriptions, real client onboarding, AI imports, SMS/email activation, or Enterprise sales. The platform must be able to prove whether the app is healthy, which tenant is affected, which workflow failed, what evidence exists, who was alerted, what action was taken, and what changed after the incident.

Observability and incident response must not be treated as optional dashboards added after launch. They are core platform safety controls because AcuityOps handles EMS readiness records, staff qualification data, medication and stock data, uploaded documents, generated PDFs, audit evidence, AI import files, notifications, billing state, and multi-client tenant data.

Decision record fields:

- `Decision ID`: unique observability and incident-response decision identifier.
- `Decision status`: proposed, approved, rejected, deferred, superseded.
- `Decision date`: date of review.
- `Decision owner`: person accountable for production visibility and incident response.
- `Reviewers`: technical, security, support, legal/privacy, commercial, and client-success reviewers.
- `Launch stage`: local, demo, staging, first paid client, African rollout, global rollout, Enterprise dedicated deployment.
- `Monitoring provider stack`: Azure Monitor, Application Insights, Log Analytics, Azure Service Health, Defender for Cloud, Microsoft Sentinel, Grafana, third-party status page, third-party incident tool, or other.
- `Telemetry scope`: app logs, metrics, traces, audit logs, browser errors, API failures, background jobs, database, storage, AI jobs, notification delivery, billing webhooks, PDF generation, imports, exports, and tenant-specific release activation.
- `Tenant-impact strategy`: how every relevant telemetry event is tagged with tenant, environment, release, module, request, job, and actor without exposing sensitive personal or clinical content.
- `Availability strategy`: uptime checks, synthetic checks, health endpoints, dependency checks, readiness checks, staging-slot checks, and client-specific dedicated deployment checks.
- `Uptime target`: planned uptime/SLO per Base, Pro, Premium, and Enterprise. No public SLA may be promised until legal/commercial review signs it off.
- `SLI/SLO model`: availability, latency, error rate, failed job rate, failed notification rate, failed import rate, failed PDF generation rate, failed billing webhook rate, and tenant-impact rate.
- `Alert strategy`: alert conditions, severity, deduplication, escalation, quiet hours, action groups, support routing, incident owner, and alert fatigue controls.
- `Client-impact detection`: how the platform identifies affected tenant, affected user roles, affected module, affected records, affected region, affected deployment, and affected time window.
- `Incident severity model`: severity levels, business impact, security impact, data impact, EMS operational impact, billing impact, notification impact, and AI/import impact.
- `Incident record model`: incident ID, severity, tenant impact, start time, detection time, acknowledgement time, mitigation time, resolution time, root cause, actions, evidence, communications, and post-incident review.
- `Audit evidence model`: immutable audit events, relevant logs, correlation IDs, request IDs, job IDs, provider event IDs, user role, tenant ID, release version, file hashes, PDF hashes, and exported evidence.
- `Support escalation model`: platform owner, technical owner, security owner, legal/privacy escalation, client contact, provider support, and external incident-response provider.
- `Client communication model`: when clients are notified, who writes the message, who approves it, what facts are included, what is withheld, and how updates are tracked.
- `Security incident model`: separate handling for suspected data exposure, credential compromise, cross-tenant leak, malicious upload, AI privacy issue, payment webhook compromise, or admin/support account compromise.
- `Post-incident review model`: timeline, root cause, contributing factors, client impact, evidence preserved, corrective actions, owner, due dates, release changes, and client communication.
- `Retention model`: telemetry retention, audit retention, incident retention, security evidence retention, billing-event retention, and legal hold behavior.
- `Cost model`: ingestion cost, log retention cost, query cost, alert cost, dashboard cost, SIEM cost, and Enterprise monitoring cost.
- `Known blockers`: unresolved provider, privacy, logging, retention, legal, cost, security, client-impact, or support-readiness issues.
- `Go/no-go decision`: selected observability/incident-response stack and reason.
- `Required proof before launch`: alert test, uptime test, failed workflow test, tenant-impact test, incident runbook test, support escalation test, post-incident review test, and evidence export test.

Required observability providers and platform functions:

- `Azure Monitor` for unified logs, metrics, traces, events, dashboards, and alerts.
- `Application Insights` with OpenTelemetry instrumentation for server-side app telemetry, failures, performance, dependencies, availability checks, and live diagnostics.
- `Log Analytics` for KQL-based investigation, retained logs, incident timelines, and operational queries.
- `Azure Monitor Alerts` and `Action Groups` for alert routing to email, SMS, Teams, webhook, or incident-management tools where configured.
- `Azure Service Health` and `Azure Resource Health` for Azure outage, maintenance, resource-health, and dependency-impact monitoring.
- `Azure Workbooks`, Grafana, or approved dashboards for platform owner and support visibility.
- `Microsoft Defender for Cloud` for security posture and resource protection alerts.
- `Microsoft Sentinel` or equivalent SIEM for Premium/Enterprise or when security monitoring requirements justify it.
- `AcuityOps Incident Register` as the internal source of truth for production incidents, client-impact records, post-incident reviews, and support escalation history.
- `AcuityOps Status Page` or approved external status-page provider before public paid launch if client communication needs public or client-specific status updates.
- `AcuityOps Support Inbox` or integrated support tool for incident tickets, client support requests, support SLA tracking, and escalation.

Telemetry and evidence rules:

- Every production request must carry environment, release version, tenant where known, role where known, correlation ID, request ID, and safe module/action context.
- Every background job must carry job ID, tenant, module, trigger source, queue attempt, retry count, start time, completion time, and failure reason.
- Every provider webhook must carry provider, event ID, tenant mapping, idempotency key, processing result, and failure reason.
- Every AI job must carry tenant, source file, job ID, model/provider, token/cost where available, status, error class, human review state, and output record link without logging unnecessary sensitive prompt content.
- Every generated PDF/report/export must carry tenant, requested by, created by, source record IDs, template version, file hash, storage path, and download/access audit.
- Logs must not contain full passwords, payment card data, national IDs, sensitive medical details, full uploaded document contents, or unnecessary personal data.
- Errors shown to users must be safe and actionable. Detailed stack traces must never be exposed in production.
- Audit evidence must be separate from debug logs and must be retained according to legal/security rules.

Incident severity model:

- `SEV1 Critical`: cross-tenant data exposure, production outage, active security compromise, medication/clinical data exposure, billing/payment compromise, or failure affecting live EMS operations across multiple clients.
- `SEV2 High`: one tenant unable to complete critical operational workflows, failed login/access across a client, broken live check submission, broken PDF evidence generation, failed data export, or serious AI/import corruption risk.
- `SEV3 Medium`: degraded workflow, delayed notifications, failed non-critical background job, limited reporting issue, one module impaired with workaround available.
- `SEV4 Low`: cosmetic issue, isolated non-critical UI defect, documentation issue, low-impact support request, or planned maintenance communication.

Incident workflow:

1. Detect from alert, support request, client report, failed synthetic check, provider event, security alert, or internal review.
2. Create or link an AcuityOps incident record.
3. Assign severity and incident owner.
4. Identify tenant impact and affected workflows.
5. Preserve audit evidence and correlation IDs.
6. Acknowledge incident and start MTTA/MTTR tracking.
7. Escalate to technical, security, legal/privacy, provider support, or client-success roles as required.
8. Mitigate without deleting evidence or corrupting tenant data.
9. Communicate to affected clients where required.
10. Resolve and verify recovery.
11. Complete post-incident review.
12. Create corrective actions with owners and dates.
13. Add tests, alerts, runbook updates, documentation, or code fixes to prevent recurrence.

Client-impact detection rules:

- A production incident must be able to answer which tenants were affected, which tenants were not affected, what modules were affected, what time window was affected, what records/jobs/actions were affected, and whether data integrity was compromised.
- If tenant impact cannot be determined from telemetry, the incident is automatically treated as higher risk until proven otherwise.
- Every incident touching tenant data must include a tenant-impact analysis before closure.
- Every client-facing incident communication must be reviewed against legal/privacy guidance before stating root cause, data exposure, downtime, liability, or regulatory impact.

Support escalation rules:

- Base, Pro, Premium, and Enterprise must have defined support paths and response targets before public launch.
- Enterprise/dedicated clients may require named escalation contacts, out-of-hours process, and dedicated incident reporting.
- Platform-owner support access must be tenant-scoped, audited, reason-captured, and time-limited.
- Support staff must not browse client data casually while investigating incidents.
- Provider escalation paths must be documented for Azure, payment provider, email/SMS provider, AI provider, DNS/domain provider, and status-page/support tooling provider.

Post-incident review rules:

- Every SEV1 and SEV2 incident requires a written post-incident review.
- Post-incident review must include timeline, detection source, impact, root cause, contributing factors, what worked, what failed, evidence preserved, corrective actions, owners, due dates, and client communication record.
- Corrective actions must be tracked until complete.
- Repeat incidents must trigger architecture review, not only bug fixes.
- Lessons learned must update runbooks, tests, alerts, dashboards, or onboarding documentation.

Decision record scoring:

- Score each monitoring/incident provider stack from 0 to 5 for telemetry coverage, tenant-impact visibility, alert quality, dashboard usefulness, incident workflow fit, support escalation, security evidence, retention control, cost, implementation effort, and Enterprise readiness.
- A stack with a zero for tenant-impact visibility, critical alerting, security evidence, or production error detection must not be used for production.
- A public launch is blocked if AcuityOps cannot detect downtime, failed live checks, failed database access, failed storage access, failed PDF generation, failed AI import, failed notification delivery, failed billing webhooks, and cross-tenant data-risk signals.

119EA. Create the production observability and incident-response decision record before production launch, paid subscriptions, AI import, or real client onboarding.
119EB. Select the initial monitoring provider stack and document Azure Monitor, Application Insights, Log Analytics, Service Health, Defender for Cloud, Sentinel, status page, and support tooling decisions.
119EC. Add OpenTelemetry/Application Insights instrumentation for web requests, dependencies, exceptions, performance, availability, and background jobs.
119ED. Add tenant-safe correlation IDs, request IDs, job IDs, tenant tags, environment tags, release tags, and module/action tags.
119EE. Add logs and metrics for login, company login, role login, checklist submission, checklist publish, PDF generation, file upload, AI import, notification delivery, billing webhook, export, and deletion/offboarding.
119EF. Add explicit safeguards to prevent sensitive values from entering production logs.
119EG. Add Azure Monitor alerts for app outage, high error rate, database failure, storage failure, failed background jobs, failed notification delivery, failed PDF generation, failed AI import, failed billing webhook processing, and abnormal permission failures.
119EH. Add Azure Service Health and Resource Health alerts for Azure-side incidents and planned maintenance affecting AcuityOps resources.
119EI. Add uptime and synthetic availability checks for public app, company login, role login, daily check load, report/PDF endpoint, and billing/subscription endpoint.
119EJ. Add tenant-impact detection so incidents can identify affected tenant, module, workflow, time window, and release version.
119EK. Add AcuityOps Incident Register for production incidents, severity, owner, tenant impact, evidence, communications, corrective actions, and closure.
119EL. Add severity levels SEV1 to SEV4 with exact escalation and response expectations.
119EM. Add support escalation paths for platform owner, technical owner, security owner, legal/privacy reviewer, client contact, Azure support, payment provider, SMS/email provider, AI provider, and DNS/domain provider.
119EN. Add client communication rules and templates for outage, degraded service, security incident, maintenance, billing incident, AI/import incident, and resolved incident.
119EO. Add platform-owner dashboard showing uptime, open incidents, active alerts, failed jobs, tenant-impact summary, failed notifications, failed billing events, failed AI jobs, and last backup/restore status.
119EP. Add tenant-facing status visibility where appropriate, without exposing other tenants or internal technical details.
119EQ. Add audit evidence links from incidents to logs, request IDs, job IDs, provider event IDs, release IDs, file/PDF hashes, user roles, and tenant IDs.
119ER. Add incident retention, audit retention, log retention, and legal-hold rules.
119ES. Add runbooks for outage, database failure, storage failure, cross-tenant exposure suspicion, failed PDF generation, failed notifications, failed billing webhooks, failed AI import, failed deployment, and provider outage.
119ET. Add post-incident review workflow for every SEV1 and SEV2 incident.
119EU. Add corrective-action tracking after post-incident review and link corrective actions to tests, alerts, code fixes, runbooks, or documentation.
119EV. Add monitoring cost controls for log ingestion, retention, dashboards, SIEM, synthetic checks, SMS alerts, and Enterprise monitoring.
119EW. Add tests proving alerts fire for simulated app failure, database failure, storage failure, failed job, failed PDF, failed notification, failed billing webhook, and failed AI import.
119EX. Add tests proving incident records identify affected tenants without exposing another tenant's data.
119EY. Add tests proving production error pages do not expose stack traces, connection strings, secrets, tenant data, or sensitive personal data.
119EZ. Production launch is blocked until observability, alerting, tenant-impact detection, incident response, support escalation, audit evidence, and post-incident review workflow are verified.

### Phase 16G: Production Support And Client-Success Operating Model

AcuityOps must have a written support and client-success operating model before public launch, paid subscriptions, or real client onboarding. Support must not depend on informal chat messages, memory, or the founder manually remembering every client configuration. The product must define how clients are onboarded, trained, supported, escalated, retained, and guided from setup to daily operational value.

This phase exists because AcuityOps is not only software. It will become part of a client's operational routine. If a client cannot onboard cleanly, understand the system, get help, train staff, resolve urgent blockers, and give product feedback, the product will feel risky even when the code works.

Decision record fields:

- `Decision ID`: unique support/client-success decision identifier.
- `Decision status`: proposed, approved, rejected, deferred, superseded.
- `Decision date`: date of review.
- `Decision owner`: person accountable for client support and success.
- `Reviewers`: platform owner, support lead, product lead, legal/commercial reviewer, security/privacy reviewer.
- `Launch stage`: demo, first paid client, South Africa rollout, Africa rollout, global rollout, Enterprise dedicated deployment.
- `Support model`: founder-led, email-only, helpdesk, in-app support, managed onboarding, partner-supported, or Enterprise support.
- `Support tooling`: email inbox, helpdesk/ticketing system, knowledge base, in-app help, screen-recording workflow, status page, support analytics, client-success CRM.
- `Support channels`: in-app request, email, phone, scheduled call, emergency channel, training call, implementation project channel.
- `Support hours`: business hours, after-hours emergency, weekend support, public holiday support, Enterprise dedicated support.
- `Tier support rules`: Base, Pro, Premium, Enterprise support entitlements and limits.
- `SLA/SLO wording`: response target, resolution target where offered, uptime target, support scope, exclusions, and legal review status.
- `Onboarding model`: self-service setup wizard, guided setup, implementation call, data import assistance, training session, go-live checklist.
- `Training model`: role-specific guides for company owner, senior manager, operational manager, and staff.
- `Documentation model`: searchable help center, in-app term guide, video library, PDF quick-start packs, release notes, and admin guides.
- `Escalation model`: support triage, technical escalation, billing escalation, security escalation, legal/privacy escalation, provider escalation, and client executive escalation.
- `Feedback model`: feature requests, bug reports, product improvement requests, churn risk, satisfaction surveys, client advisory feedback, and roadmap review.
- `Client-health model`: onboarding progress, weekly active usage, checklist adoption, report usage, unresolved issues, failed imports, support load, renewal/churn risk.
- `Support evidence model`: ticket history, screenshots, logs, tenant impact, support actions, admin access reason, client communication, and closure notes.
- `Known blockers`: unresolved staffing, support tooling, legal SLA wording, after-hours support, training material, helpdesk integration, or client-success capacity risks.
- `Go/no-go decision`: selected support operating model and reason.
- `Required proof before launch`: support ticket test, onboarding test, training test, documentation review, escalation drill, feedback loop test, and client-success dashboard test.

Required support and client-success functions:

- `Support Inbox`: central place for client requests, internal issues, training requests, billing support, product feedback, and urgent operational blockers.
- `Helpdesk/Ticketing`: ticket ID, tenant, reporter, role, severity, module, status, owner, due target, linked incident, linked logs, and closure reason.
- `Knowledge Base`: searchable articles for setup, daily checks, checklist management, registers, readiness dashboard, reporting, imports, billing, exports, and troubleshooting.
- `In-App Help`: role-specific help text, term dictionary, page explanations, workflow explanations, and links to support.
- `Client Onboarding Tracker`: setup wizard progress, data import status, staff invitation status, checklist publish status, first live check, first report, first readiness dashboard review, first export.
- `Training Library`: short role-specific training flows for staff, operational managers, senior managers, company owner, and platform owner.
- `Release Notes`: client-facing notes for new features, fixed issues, breaking changes, known issues, and tier-specific changes.
- `Client Health Dashboard`: adoption, active users, checks submitted, unresolved issues, import errors, support tickets, usage by module, renewal risk, and expansion opportunity.
- `Feedback Register`: product feedback, feature requests, bug reports, client priority, revenue impact, tier impact, and product decision state.

Support severity model:

- `Support P1`: client cannot access the platform, submit live operational checks, access critical reports, or there is suspected data/security impact.
- `Support P2`: critical workflow degraded for one tenant or key manager role; workaround exists but operational value is impaired.
- `Support P3`: non-critical workflow issue, UI defect, report formatting issue, import mapping issue, training question, or configuration support.
- `Support P4`: product suggestion, documentation question, cosmetic issue, future enhancement, or low-priority request.

Support and SLA rules:

- Base support should be clear and sustainable: self-service documentation, normal business-hour support, and limited implementation assistance.
- Pro support may include guided onboarding, priority support, import assistance, and stronger reporting/configuration support.
- Premium support may include advanced implementation help, AI import assistance, compliance-mode support, scheduled success reviews, and stronger escalation.
- Enterprise support may include dedicated onboarding, custom success plan, named contacts, dedicated environment support, custom release coordination, and contractual support terms.
- No uptime SLA, support SLA, after-hours promise, or emergency operational guarantee may be sold until legal/commercial review approves exact wording.
- Support targets must be measured from ticket creation or verified alert detection, not from informal messages.
- Support must not promise clinical, legal, tax, or regulatory advice unless qualified professionals are engaged and contracts define the boundary.

Onboarding rules:

- Every new tenant must have a setup path from company creation to first operational use.
- Onboarding must include company profile, logo, areas/bases, staff, vehicles, equipment, stock, medication, checklists, schematics, readiness settings, access permissions, and first reports.
- Onboarding must have a go-live checklist that confirms the tenant has at least one active checklist, at least one active user per required role, required registers, and working company login/access flow.
- Support must be able to see setup status without manually inspecting database records.
- Onboarding support must be tier-aligned and commercially sustainable.

Training and documentation rules:

- Training must be role-specific: staff need daily workflows; operational managers need area oversight; senior managers need setup, reports, publishing, permissions, and audit control; company owners need billing, export, and subscription controls.
- Documentation must match current UI wording. Old terms such as monthly checklist, placeholder pages, or legacy routes must not appear in help material.
- Every major workflow must have a short "what this does" explanation and a practical "how to use it" guide.
- Training assets must be versioned with release notes so support is not teaching outdated workflows.
- Documentation must include boundaries: AcuityOps supports operational readiness management but does not replace clinical governance, servicing obligations, legal compliance, or professional judgement.

Escalation rules:

- Support escalation must distinguish product issue, configuration issue, client training issue, billing issue, security/privacy issue, provider outage, and legal/compliance issue.
- Security/privacy concerns must escalate through the incident-response path in Phase 16F.
- Billing support must escalate through the billing state machine in Phase 15C.
- Data export/offboarding support must escalate through Phase 16D.
- Client-specific release issues must escalate through Phase 16A and Phase 16E.
- Provider issues must include escalation paths for Azure, payment provider, SMS/email provider, AI provider, DNS/domain provider, and support/helpdesk provider.

Feedback-loop rules:

- Client feedback must be captured as structured records, not lost in chats.
- Feedback must be tagged by tenant, tier, module, role, revenue impact, safety impact, implementation effort, and recurrence.
- Bug reports must link to support tickets, logs, release version, tenant impact, and fix verification.
- Feature requests must be triaged into reject, backlog, planned, in progress, shipped, or needs commercial validation.
- Repeated support tickets must trigger product design review or documentation improvements.
- Client-success reviews must feed into product roadmap, pricing, onboarding materials, and training content.

119FA. Create the production support and client-success operating model before public launch or paid client onboarding.
119FB. Select support tooling for helpdesk/ticketing, knowledge base, client-success tracking, release notes, and support analytics.
119FC. Add Support Inbox or helpdesk integration with tenant, reporter, module, severity, status, owner, due target, linked incident, and closure reason.
119FD. Add support severity model P1 to P4 and define response targets by tier.
119FE. Add legal/commercial review gate before publishing SLA, uptime, after-hours support, emergency support, or resolution-time promises.
119FF. Add client onboarding tracker tied to setup wizard progress, register completion, checklist publishing, access setup, first check, first report, and go-live readiness.
119FG. Add go-live checklist for each tenant before they are considered operational.
119FH. Add role-specific training paths for staff, operational managers, senior managers, company owners, and platform owners.
119FI. Add searchable knowledge base with current UI wording and no legacy/placeholder terminology.
119FJ. Add in-app help links from key workflows to relevant documentation and support.
119FK. Add client-facing release notes and known-issues register.
119FL. Add client health dashboard for adoption, active users, check submissions, unresolved issues, support load, import errors, report usage, renewal risk, and expansion opportunity.
119FM. Add feedback register for feature requests, bugs, product suggestions, training gaps, support pain points, and client-success opportunities.
119FN. Add escalation rules for product, training, configuration, billing, security/privacy, legal/compliance, provider outage, AI/import, and client-specific release issues.
119FO. Add support access controls so support actions are tenant-scoped, reason-captured, time-limited, and audit logged.
119FP. Add support documentation explaining what support can and cannot do, including clinical/legal/tax/regulatory boundaries.
119FQ. Add support templates for onboarding, incident follow-up, billing issue, failed import, failed PDF, access issue, checklist publishing issue, and feature request response.
119FR. Add support analytics for response time, resolution time, open ticket age, recurring issue count, client support volume, and high-risk tenants.
119FS. Add training and documentation versioning tied to release notes.
119FT. Add process that converts repeated support problems into product backlog items, documentation updates, or training changes.
119FU. Add client-success review workflow for Pro, Premium, and Enterprise clients.
119FV. Add support tests proving a support ticket can be created, assigned, escalated, linked to logs/incidents, closed, and audited.
119FW. Add onboarding tests proving a new tenant can move from setup wizard to first live operational check and first report.
119FX. Add documentation verification before launch to ensure help content matches current app terms, pages, routes, and tier behavior.
119FY. Add support capacity review before signing clients with SLA/after-hours/Enterprise support commitments.
119FZ. Production launch is blocked until support tooling, onboarding support, documentation, escalation, training, feedback loop, and client-success operating model are verified.

### Phase 16H: Production Sales, Demo, And Trial-Conversion Operating Model

AcuityOps must have a written sales, demo, and trial-conversion operating model before public marketing, paid subscriptions, demo requests, sales calls, or real client onboarding. Sales must not promise features, timelines, support levels, AI capabilities, compliance outcomes, integrations, schematics, uptime, or legal/regulatory value that the product has not implemented, verified, documented, priced, and reviewed.

This phase exists because the sales path is part of the product. A client must move from lead capture to demo to trial to onboarding to paid subscription without creating unsafe data, false expectations, unsupported configuration, untracked promises, or hidden manual work that cannot scale.

Decision record fields:

- `Decision ID`: unique sales/demo/trial decision identifier.
- `Decision status`: proposed, approved, rejected, deferred, superseded.
- `Decision date`: date of review.
- `Decision owner`: person accountable for commercial funnel quality.
- `Reviewers`: platform owner, product lead, support/client-success owner, legal/commercial reviewer, billing owner.
- `Launch stage`: pre-launch validation, demo-only, first paid client, South Africa rollout, Africa rollout, global rollout, Enterprise sales.
- `Target market`: private EMS, NGO, event medical, rescue, ambulance, response vehicle, mixed EMS, Enterprise, government-adjacent.
- `Lead source`: website, referral, cold outreach, LinkedIn, partner, industry event, inbound email, demo request, existing relationship.
- `Sales tooling`: CRM, website form, demo booking, email automation, analytics, proposal tool, contract workflow, billing provider, support handoff.
- `Demo model`: shared demo tenant, guided live demo, client-specific demo tenant, sandbox trial tenant, recorded demo, or proof-of-concept tenant.
- `Trial model`: no trial, time-limited trial, demo-data trial, client-data trial, paid pilot, Enterprise proof of concept.
- `Trial length`: default duration and allowed extensions.
- `Trial limits`: user count, vehicle count, asset count, storage, AI imports, SMS/email, PDF exports, reports, support level, data retention, and export rights.
- `Conversion model`: self-service upgrade, assisted conversion, invoice conversion, manual Enterprise contract, paid pilot conversion.
- `Onboarding handoff`: exact point where sales hands the client to support/client success and setup wizard.
- `Sales promise register`: promises, custom requirements, special pricing, support commitments, delivery dates, integrations, compliance claims, and client-specific UI commitments.
- `Commercial approval`: who can approve discounts, custom features, custom support promises, custom contracts, or Enterprise commitments.
- `Legal review state`: whether pricing, proposal wording, trial terms, demo disclaimers, privacy wording, AI wording, compliance claims, and contract terms are approved.
- `Known blockers`: unresolved feature, legal, support capacity, billing, onboarding, AI, security, import, compliance, or product-risk items.
- `Go/no-go decision`: selected sales/demo/trial path and reason.
- `Required proof before launch`: lead form test, CRM test, demo tenant test, trial tenant test, conversion test, onboarding handoff test, billing test, sales-promise audit, and support handoff test.

Required sales and demo platform functions:

- `Lead Capture`: website and manual lead entry with name, company, country, contact, fleet size, role, email, phone, source, interest, and consent/communication status.
- `CRM/Pipeline`: lead stage, qualification, demo scheduled, demo completed, trial active, trial expired, proposal sent, contract review, won, lost, delayed, and churn risk.
- `Demo Tenant Registry`: every demo tenant must be marked as demo, seeded with non-client demo data only, separated from production clients, and excluded from production analytics unless explicitly included as demo analytics.
- `Trial Tenant Registry`: trial tenants must be marked as trial, linked to billing/trial state, and controlled by trial limits.
- `Sales Promise Register`: all promises made to a prospect must be captured against lead/client, owner, date, status, product area, tier, delivery requirement, and approval state.
- `Trial Conversion Workflow`: converts trial tenant to paid tenant, or exports/deletes trial data if the client does not convert.
- `Onboarding Handoff`: converts sales context into setup wizard tasks, support tasks, import tasks, training tasks, and commercial account records.
- `Commercial Notes`: pricing, discounts, special terms, billing frequency, VAT/tax assumptions, implementation fee, and contract path.

Lead capture rules:

- Lead capture must not automatically create a production tenant.
- A lead may become a demo tenant, trial tenant, or paid tenant only through an explicit workflow.
- Lead forms must not collect unnecessary sensitive operational, staff, patient, clinical, or medication data.
- Marketing consent and communication preferences must be captured where required.
- Country, company size, fleet size, and operating model must be captured to guide pricing, tier fit, and support load.
- Demo requests must enter the pipeline with owner, next action, and response target.

Demo tenant rules:

- Demo tenants must be clearly labelled as demo in the database, UI, logs, billing, support, reports, and analytics.
- Demo tenants must not contain real client data unless converted into a formal trial or pilot with approved terms.
- Demo tenants must be resettable without affecting real tenants.
- Demo seed data must never be used as fallback data in production clients.
- Demo tenants must not create false evidence, invoices, compliance documents, or real audit records for a paying client.
- Demo tenant routes must not confuse the user with production access links.

Trial rules:

- Trials must have explicit start date, expiry date, tier, limits, support level, data retention, and conversion path.
- Trial clients must understand what happens to their data if they do not convert.
- Trial limits must be enforced by entitlement logic, not only by hidden UI.
- If a trial uses real client data, export/offboarding, privacy, support, and deletion rules apply.
- Trial expiry must not corrupt data or leave orphaned tenants.
- Trial extension must be approved and audit logged.
- Trial-to-paid conversion must preserve tenant ID, data, users, configuration, and audit history unless the client chooses a clean start.

Sales promise rules:

- Every commercial promise must map to an implemented feature, verified roadmap item, approved custom work item, or rejected/unapproved state.
- Sales material must match the tier matrix exactly.
- Sales material must not claim AI import, future analytics, compliance mode, Azure dedicated hosting, SMS, email, SOP transformation, PDF evidence, uptime, support SLA, legal compliance, or audit-readiness unless that item is implemented or explicitly labelled as planned.
- Any client-specific request must enter the product backlog or Enterprise delivery workflow before being promised.
- Discounts, support commitments, custom delivery dates, custom integrations, and client-specific UI work require approval.
- No salesperson, founder, or support actor may promise clinical, legal, tax, or regulatory advice.

Conversion rules:

- Conversion from lead to paid client requires billing profile, subscription/tier, company owner, setup wizard start, support handoff, and tenant provisioning.
- Conversion from trial to paid must set billing state, plan entitlements, support tier, data retention, export/offboarding rights, and client-success owner.
- Lost leads and expired trials must be marked with reason codes.
- Expired trials must enter export/delete/offboarding workflow according to trial terms.
- Demo tenants must not be converted to paid tenants unless the tenant is clean or explicitly converted with data review.

Onboarding handoff rules:

- Sales handoff must include client name, legal billing name, country, billing contact, primary owner, operating model, fleet estimate, staff estimate, asset/register import needs, checklist needs, compliance needs, AI/import needs, support expectations, and promised features.
- Support/client-success must confirm handoff before client is marked ready for onboarding.
- Setup wizard tasks must be generated from handoff data where possible.
- Handoff must flag any unapproved custom promise before onboarding begins.

Feedback and product-loop rules:

- Lost deals must capture reason: price, missing feature, trust concern, compliance gap, support concern, AI/import need, integration need, competitor, timing, or budget.
- Trial failures must capture reason: onboarding friction, checklist builder confusion, import difficulty, reporting gap, support gap, pricing concern, reliability concern, missing workflow.
- Sales objections must feed into product roadmap, pricing, website copy, demo script, support material, and training.
- Frequently requested features must be tagged by revenue impact and tier strategy.
- Sales feedback must not override product safety gates.

119GA. Create the production sales, demo, and trial-conversion operating model before public marketing or paid client onboarding.
119GB. Select sales tooling for lead capture, CRM/pipeline, demo scheduling, proposal/contract tracking, trial tracking, and analytics.
119GC. Add lead capture model with company, country, contact, role, fleet size, source, interest, consent, pipeline owner, and next action.
119GD. Add pipeline stages from lead to demo, trial, proposal, won, lost, expired, and onboarding handoff.
119GE. Add demo tenant registry and ensure demo tenants are isolated from real production tenants.
119GF. Add trial tenant registry linked to billing/trial state, tier, limits, expiry date, support level, and data retention.
119GG. Add trial limit enforcement for users, vehicles, assets, storage, AI imports, SMS/email, exports, reports, and support level.
119GH. Add trial expiry workflow with warning, extension approval, conversion, export, or deletion/offboarding.
119GI. Add trial-to-paid conversion workflow preserving tenant data and configuration unless clean-start conversion is selected.
119GJ. Add Sales Promise Register with owner, client, date, promise, product area, tier, approval state, delivery requirement, and risk.
119GK. Add rule that sales material and proposals must match the approved tier matrix and verified feature state.
119GL. Add approval workflow for discounts, custom support commitments, client-specific UI, custom delivery dates, integrations, and Enterprise terms.
119GM. Add legal/commercial review before public use of pricing, proposal wording, trial terms, demo disclaimers, compliance claims, AI claims, uptime wording, and support promises.
119GN. Add demo script and demo dataset that shows real product value without using stale seed data or untrue workflows.
119GO. Add onboarding handoff record from sales to support/client success and setup wizard.
119GP. Add handoff fields for billing, owner, areas, staff estimate, vehicle estimate, import needs, checklist needs, compliance needs, AI needs, support expectations, and promised features.
119GQ. Add blocker warning when a prospect has unapproved custom promises before onboarding.
119GR. Add lost-deal and expired-trial reason tracking.
119GS. Add sales-feedback loop into roadmap, pricing, website copy, demo script, onboarding, documentation, and support.
119GT. Add demo/trial analytics for demo completions, trial activations, first checklist built, first check submitted, first report viewed, conversion rate, and churn risk.
119GU. Add tests proving lead capture does not create production tenant data.
119GV. Add tests proving demo tenants cannot leak into production clients, analytics, billing, checklist fallback, or seed routines.
119GW. Add tests proving trial limits are enforced server-side.
119GX. Add tests proving conversion preserves tenant data and applies correct billing/subscription entitlements.
119GY. Add verification that sales copy, pricing, tier matrix, demo script, and product behavior are aligned.
119GZ. Public sales launch is blocked until lead capture, demo tenants, trial limits, conversion, onboarding handoff, sales promises, and feedback loops are verified.

### Phase 16I: Production Website And Marketing-Content Truth Control

AcuityOps must have a controlled public website and marketing-content approval system before any public launch, paid advertising, SEO campaign, pricing page, demo request flow, lead magnet, sales deck, public roadmap, comparison page, or founder outreach campaign is used. Public claims must match the implemented product, approved roadmap, tier matrix, legal wording, privacy posture, support model, billing rules, and actual demo/trial behavior.

This phase exists because the website is not only a brochure. It is the first operational promise made to a client. If the website promises automation, AI import, compliance readiness, PDF evidence, readiness scoring, Azure hosting, SMS/email alerts, support levels, audit reports, or integrations that are not implemented and verified, sales will create technical debt, legal exposure, refund pressure, support burden, and trust damage.

Decision record fields:

- `Decision ID`: unique website/content decision identifier.
- `Decision status`: proposed, approved, rejected, deferred, superseded.
- `Decision date`: date of review.
- `Decision owner`: person accountable for public claims and marketing accuracy.
- `Reviewers`: platform owner, product lead, commercial owner, legal reviewer, privacy/security reviewer, support/client-success owner.
- `Launch stage`: private preview, demo-only, first paid clients, South Africa launch, Africa launch, global launch, Enterprise launch.
- `Website stack`: selected website platform, hosting provider, CMS/content source, analytics provider, form provider, CRM handoff, SEO tooling, and deployment method.
- `Public pages`: Home, Features, Pricing, EMS Operations, Readiness Engine, Checklist Builder, AI Import, Compliance, Security, Privacy, Terms, Support, Demo Request, Contact, FAQ, and Changelog.
- `Claim category`: feature claim, AI claim, compliance claim, security claim, uptime claim, support claim, pricing claim, integration claim, comparison claim, roadmap claim, legal/regulatory claim.
- `Claim status`: implemented and verified, implemented but unverified, planned, research only, not available, tier-limited, Enterprise-only, deprecated.
- `Evidence link`: build/test/audit/spec/document proving the claim is true.
- `Tier mapping`: Base, Pro, Premium, Enterprise, locked, read-only, export-only, add-on, unavailable.
- `Legal review state`: not reviewed, reviewed with changes, approved, rejected, requires external lawyer.
- `SEO state`: target market, keyword cluster, page intent, target country, metadata, schema, content owner, and update cadence.
- `Analytics state`: page tracking, conversion event tracking, consent state, privacy review, and dashboard owner.
- `Demo flow state`: form submitted, CRM created, demo tenant selected, sales owner assigned, meeting booked, handoff created, and follow-up scheduled.
- `Known blockers`: unsupported claim, missing feature, unclear tier, legal risk, privacy risk, support risk, billing mismatch, demo mismatch, outdated screenshot, or stale route.
- `Go/no-go decision`: selected website/content release path and reason.

Required website and marketing-control functions:

- `Content Claim Register`: every public claim must be recorded with page, wording, category, tier, evidence, owner, legal review state, status, and expiry/review date.
- `Pricing Truth Table`: public pricing, limits, add-ons, annual/monthly terms, VAT/tax wording, refunds, failed payment behavior, trial limits, and downgrade/cancellation rules must match billing architecture and tier matrix.
- `Demo Request Flow`: public demo forms must create a lead, capture consent, route to CRM/pipeline, assign owner, create next action, and never create a production tenant automatically.
- `Website Analytics`: public website must track visits, demo requests, pricing-page views, conversion events, campaign source, country/region, and funnel drop-off without collecting unnecessary sensitive data.
- `SEO Content Plan`: pages must be planned by target search intent, country/market, keyword cluster, content owner, review date, and claim-control status.
- `Public Screenshot Register`: screenshots, videos, and demo images must show current UI, approved demo data, and no client-sensitive data.
- `Legal Review Queue`: privacy wording, terms, pricing claims, compliance claims, AI wording, uptime claims, support commitments, refund wording, and regional claims must be reviewed before publication.
- `Content Release Gate`: content cannot be published unless every claim is mapped to a verified product state, approved roadmap label, tier limitation, or explicit planned/unavailable wording.

Public claim rules:

- Public pages must not claim a feature is available unless it is implemented, tested, and available in the tier shown.
- Planned features must be clearly labelled as planned, future, or roadmap and must not be placed in pricing tables as if available.
- AI import, AI checklist generation, AI reporting, predictive analytics, SOP transformation, and future compliance analysis must be described only according to the implemented or approved roadmap state.
- Compliance pages must not promise legal compliance, audit success, Department of Health approval, clinical compliance, or regulatory certification.
- Security pages must not promise certifications, encryption posture, uptime, hosting isolation, backups, recovery times, or incident response levels that have not been implemented and verified.
- Pricing pages must not hide limits that affect product use, including users, vehicles, assets, storage, AI imports, SMS/email, exports, reports, support, retention, and tenant model.
- Public comparison pages must avoid unsupported claims about competitors and must compare only verified product capabilities.
- Public content must not use client names, screenshots, operational data, staff data, logos, or case studies without written permission.

Pricing page rules:

- Pricing must map exactly to the Base, Pro, Premium, and Enterprise feature matrix.
- Each tier must show what is included, locked, read-only, exportable, add-on, or Enterprise-only.
- Trial limits and trial expiry behavior must be visible before sign-up.
- Downgrade and cancellation behavior must be linked from pricing or subscription pages.
- VAT/tax wording must be reviewed before launch in South Africa and before new-country expansion.
- Refund, failed payment, grace period, suspension, reactivation, and export rights must match billing architecture.
- Enterprise pricing must route to assisted sales and must not imply instant self-service activation if that path is not implemented.

Demo request rules:

- Demo request forms must capture only necessary sales data and consent.
- Demo request submission must create a lead, CRM/pipeline item, and support/sales notification.
- Demo request must not create a real company workspace unless an approved trial or paid onboarding workflow is started.
- Demo booking must clearly distinguish a shared demo, demo tenant, trial tenant, paid pilot, and production tenant.
- The demo confirmation must state what happens next and must not imply product activation before onboarding.

SEO and analytics rules:

- SEO work must not outrank truth-control. Search traffic is useless if the page creates false expectations.
- Every SEO page must have a claim owner and review date.
- SEO metadata must avoid unsupported compliance, AI, security, or legal claims.
- Analytics must respect privacy, consent, regional requirements, and data minimization.
- Analytics must identify funnel performance without exposing client operational data.
- Campaign tracking must link leads to source, content, demo request, trial activation, and conversion.

Legal review rules:

- Public claims about compliance, audits, clinical operations, AI, security, privacy, uptime, support, refund rights, and data ownership require legal/commercial review before publication.
- Any South African POPIA wording, Department of Health audit wording, EMS compliance wording, and healthcare operational-risk wording must be reviewed before use in public pages.
- Trademark, brand naming, logo usage, competitor comparison, case study, testimonial, and client logo usage must be reviewed before publication.
- Terms, privacy policy, cookie/analytics wording, trial terms, pricing terms, refund terms, and support commitments must be reviewed before public launch.

119HA. Create the production website and marketing-content truth-control phase before public launch, advertising, SEO, or paid-client acquisition.
119HB. Select website stack, hosting provider, CMS/content source, analytics provider, form provider, CRM handoff, SEO tooling, and deployment method.
119HC. Add Content Claim Register for every public claim with page, wording, category, tier, owner, evidence, status, review date, and legal review state.
119HD. Add Pricing Truth Table matching Base, Pro, Premium, and Enterprise tier matrix, billing rules, downgrade/cancellation rules, trial limits, add-ons, VAT/tax wording, refunds, and export rights.
119HE. Add public website information architecture: Home, Features, Pricing, EMS Operations, Readiness Engine, Checklist Builder, AI Import, Compliance, Security, Privacy, Terms, Support, Demo Request, Contact, FAQ, and Changelog.
119HF. Add public claim categories for feature, AI, compliance, security, uptime, support, pricing, integration, comparison, roadmap, and legal/regulatory claims.
119HG. Add rule that every public claim must be implemented and verified, tier-limited, approved as roadmap, or clearly marked unavailable/planned.
119HH. Add demo request form flow that creates lead, consent record, CRM/pipeline item, owner assignment, next action, and sales/support notification.
119HI. Add rule that demo request forms must not automatically create production tenants.
119HJ. Add demo booking and confirmation wording that distinguishes shared demo, demo tenant, trial tenant, paid pilot, and production tenant.
119HK. Add public pricing page with visible tier limits, locked features, read-only states, add-ons, trial limits, downgrade/cancellation rules, and Enterprise assisted-sales path.
119HL. Add website analytics events for page view, pricing view, demo request, demo booked, trial requested, trial activated, conversion, lost lead, and campaign source.
119HM. Add privacy-reviewed analytics and consent approach before installing website tracking or remarketing tools.
119HN. Add SEO content plan by target country/market, search intent, keyword cluster, page owner, claim-control state, and review cadence.
119HO. Add SEO rule that metadata and landing pages cannot contain unsupported compliance, AI, security, legal, or uptime claims.
119HP. Add Public Screenshot Register so all screenshots, videos, and demos use current UI, approved demo data, and no client-sensitive data.
119HQ. Add legal review queue for privacy, terms, pricing, compliance claims, AI wording, uptime wording, support commitments, refund wording, regional claims, testimonials, case studies, and competitor comparisons.
119HR. Add pre-publication review gate blocking unreviewed legal/regulatory, security, AI, pricing, support, or compliance claims.
119HS. Add website release checklist that validates links, forms, CRM handoff, demo request notifications, pricing accuracy, tier accuracy, screenshots, analytics, SEO metadata, and legal footer links.
119HT. Add content versioning and changelog for public pages that affect pricing, claims, support, privacy, security, compliance, trials, or tier behavior.
119HU. Add process for removing or correcting public claims when features are delayed, removed, tier-restricted, or found inaccurate.
119HV. Add tests proving demo request creates a lead and pipeline item without creating a production tenant.
119HW. Add tests proving pricing page feature claims match the tier matrix.
119HX. Add tests proving analytics events fire without capturing sensitive operational, staff, medication, patient, or client-confidential data.
119HY. Add verification that website copy, pricing, demo flow, sales script, tier matrix, support promises, billing state, and app behavior are aligned.
119HZ. Public website launch is blocked until claim control, pricing truth, demo flow, SEO, analytics, screenshot safety, and legal review are verified.

### Phase 17: Website And Launch

120. Build public AcuityOps website.
121. Include Home, Features, Pricing, Private EMS, AI Import, Readiness Engine, Compliance Reporting, and Contact/Demo.
122. Add lead capture and demo request flow.
123. Create demo company separate from production clients.
124. Create sales-ready tier descriptions and pricing.
125. Verify website, app, demo, and onboarding links work as a complete funnel.

### Phase 18: Final Release Gate

126. Run full source audit.
127. Run full route audit.
128. Run full permission audit.
129. Run full database source-of-truth audit.
130. Run desktop browser verification.
131. Run mobile browser verification.
132. Run PDF generation verification.
133. Run AI import test with messy sample files.
134. Run multi-client separation test.
135. Run Azure/staging deployment test.
136. Commit final verified release.
137. Push to GitHub.
138. Tag the release.
139. Prepare production launch checklist.

## Current Step Status

Status values:

- `Not started`
- `In progress`
- `Blocked`
- `Verified`
- `Committed`

This section must be updated after every completed step.

| Step | Status | Notes |
| --- | --- | --- |
| 1-139 plus 15A-15R, 84A-84Z, 93A-93Z, 93AA-93AZ, 93BA-93BZE, 100A-100AF, 113A-113Z, 113AA-113AR, 113AS-113BZ, 119A-119T, 119U-119AZ, 119BA-119BT, 119BU-119CT, 119CU-119DZ, 119EA-119EZ, 119FA-119FZ, 119GA-119GZ, and 119HA-119HZ | Not started | Use this table as the progress ledger. Expand individual rows as each step begins. |

## Change Control

If the user requests work outside this sequence, Codex must first state:

1. Which spec step the request belongs to.
2. Whether it is in order.
3. Whether it creates risk for incomplete earlier steps.
4. Whether the spec should be updated before implementation.

No implementation should proceed outside this spec unless the user explicitly approves the deviation.
