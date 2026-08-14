# AcuityOps Single-Client Premium Pilot Roadmap

Status: Active and sole roadmap, progress, and execution authority

Updated: 2026-08-14

## Authority

This file is the only AcuityOps document permitted to calculate completion,
report execution status, order mandatory work, or nominate the next authorized
action. Requirement specifications, architecture decisions, runbooks, add-on
contracts, and implementation blueprints remain binding within their approved
scope, but they do not calculate progress or change execution order.

The prior commercial-launch sequence is replaced by this pilot-first sequence.
Its accepted evidence has been migrated below and is not discarded. Git history
retains the previous authority as historical context.

`100%` means one real pilot client has:

1. received an ordinary, isolated tenant through the supported provisioning and
   Setup Wizard paths;
2. received a dated twelve-month Premium pilot entitlement without hardcoded
   identity, seed data, or billing dependency;
3. entered or imported its own operational data;
4. operated every approved pilot workflow on desktop and mobile;
5. passed the complete pilot acceptance, security, restore, export, evidence,
   support, and data-isolation gates; and
6. reached normal daily use on the stable pilot environment.

Payment, automated subscriptions, a public website, public trials, enterprise
scaling, and Add-on Track A1 are not part of this 100% definition. They remain
post-pilot requirements and may not delay or expand the pilot critical path.

## Pilot Product Boundary

The pilot is a real product environment for one client, not a demo tenant and
not a client-specific fork. The following boundaries are mandatory:

- all tenant-owned records retain `CompanyId` isolation and every existing
  cross-tenant test remains binding;
- the pilot client is created and managed as an ordinary removable tenant;
- no company name, workspace, credential, checklist, register, entitlement, or
  fallback is hardcoded or seeded;
- product-owned schematic and future global libraries remain separate from
  tenant assignments;
- the data model and supported provisioning path continue to allow additional
  tenants later without schema redesign;
- only one client is operationally provisioned during this roadmap;
- Premium pilot access is an explicit entitlement with start, expiry, status,
  audit history, and expiry behavior, not a permanent bypass;
- Add-on Track A1 is separately licensed and remains inactive unless explicitly
  resumed after its source and legal gates pass.

## Progress Dashboard

| Measure | Current value |
| --- | --- |
| Mandatory pilot blocks | 8 |
| Accepted evidence points | 60 / 100 |
| Overall Premium pilot readiness | 60% |
| Active block | P4 Premium Import, Knowledge, And Forecasting |
| Locked completed product evidence | Historical B1-B5 evidence migrated below |
| Premium intelligence status | B7.1 source, privacy controls, Azure resources, and migration implemented; live structured-mapping acceptance has a confirmed defect |
| Add-on Track A1 | Deferred and outside pilot progress; B6.0 externally blocked, B6.1 accepted |
| Current Azure month-to-date spend | USD 32.83 last confirmed on 2026-08-14; Cost Management was throttled during the post-cleanup query |
| Pilot Azure monthly ceiling | USD 75 total; approval required before a forecast above USD 60 |

The previous accepted points are preserved exactly:

- historical B1 contributes `7` accepted points to P2;
- historical B2 and B3 contribute `20` accepted points to P3;
- historical B5 contributes `10` accepted points to P4;
- historical B4 contributes `8` accepted points to P5.

Those `45` historical points plus the `8` accepted P1 points and the `7`
accepted P2 completion points are measured against the complete 100-point pilot
target, so readiness is `60%`. Implemented but unaccepted B7.1 work remains
recorded as evidence and earns no additional points until its live acceptance
gate passes.

## Verified-Work Finality Rule

1. Work with recorded build, staging, commit, or acceptance evidence is final
   and must be marked `Accepted and locked`.
2. Locked work may be reopened only for a reproducible regression, a materially
   changed dependency, a discovered security/data-isolation concern, or an
   explicit user instruction.
3. Reopening requires the exact trigger, affected evidence, expected credit
   cost, and smallest required verification before work begins.
4. Repeated full-app, full-route, source-of-truth, seed, branding, navigation,
   alignment, reconciliation, and staging audits are prohibited.
5. Each implementation batch receives one targeted build and one relevant
   verification pass. Repetition requires a recorded failure or an invalidating
   later change.
6. Browser verification covers only changed routes and one directly dependent
   workflow.
7. Documentation-only work requires no build, browser test, database access, or
   Azure deployment.
8. After acceptance passes, close and lock the batch. Do not create extra
   alignment, reconciliation, closeout, or final-verification batches.
9. One coherent batch produces at most one source commit, one tracker/docs
   commit, and one deployment with targeted staging verification unless a
   declared risk requires separation.
10. A proposed batch that only repeats current accepted evidence must be
    rejected.

## Pilot Block Register

| ID | Block | Points | Accepted | Status | Primary acceptance gate |
| --- | --- | ---: | ---: | --- | --- |
| P1 | Azure Cost Stabilization | 8 | 8 | Accepted and locked | One necessary pilot resource set remains; obsolete SQL copies and environments are safely retired; fixed and variable cost controls pass |
| P2 | Pilot Infrastructure And Tenant Lifecycle | 14 | 14 | Accepted and locked | Stable GitHub-controlled pilot environment, ordinary tenant provisioning, dated Premium entitlement, backup/restore, observability, and rollback pass |
| P3 | Core Operational Product Completion | 20 | 20 | Accepted and locked | Setup, registers, movement, tasks/issues, checklist source of truth, daily work, permissions, and manual operations remain accepted |
| P4 | Premium Import, Knowledge, And Forecasting | 18 | 10 | In progress | Deterministic import remains locked; AI import, SOP/CPG knowledge, and 3/6/12-month operational forecasting pass human-review and tenant gates |
| P5 | Notifications And Evidence | 10 | 8 | In progress | Immutable report/PDF evidence remains locked; required email/SMS events, preferences, delivery audit, failure handling, and evidence links pass |
| P6 | Security, Data Lifecycle, And Support | 12 | 0 | Not started | Security review, restore drill, export, deletion, retention, offboarding, incident process, support path, and pilot legal documents pass |
| P7 | Full UI And Mobile Quality | 8 | 0 | Not started | Every pilot route uses the shared app shell and passes desktop/mobile workflow, accessibility, empty/loading/error, and visual consistency checks |
| P8 | Pilot Onboarding And Acceptance | 10 | 0 | Not started | A real pilot tenant completes onboarding, enters/imports its own data, operates every approved workflow, and signs off the acceptance matrix |

Total pilot weight is exactly `100` points. Points move only when a named child
gate with recorded evidence is accepted. Existing locked evidence is never
retested merely to earn points.

## Mandatory Execution Order

1. `P1` Azure Cost Stabilization.
2. Close the remaining `P2` pilot infrastructure and entitlement gates.
3. Complete the remaining `P4` Premium intelligence gates.
4. Complete the remaining `P5` notification gates while preserving locked
   evidence behavior.
5. Complete `P6` security, data lifecycle, restore, and support controls.
6. Complete `P7` as one bounded full pilot-route UI quality program.
7. Complete `P8` with the real pilot client.

`P3` is already locked. It is not reopened unless the Verified-Work Finality
Rule is triggered.

## Block Acceptance Contracts

### P1 Azure Cost Stabilization

Scope:

- identify the current resource-level cost and retain one necessary pilot
  resource set;
- verify and retire obsolete App Service environments only after backup and
  dependency checks;
- replace billable SQL database-copy backups with approved built-in restore or
  Blob/BACPAC retention;
- retain one active pilot database and no persistent verification database;
- cap Application Insights ingestion and retention;
- configure actual and forecast alerts at 50%, 75%, 90%, and 100%; and
- record fixed baseline and variable AI/document costs independently.

Cost gates:

- persistent fixed pilot infrastructure target: `<= USD 45/month`;
- AI and document-processing allowance: `<= USD 20/month` unless explicitly
  approved;
- total monthly budget: `USD 75`;
- no new provisioning when forecast reaches `USD 60` without approval;
- no copied billable database may remain beyond 24 hours after its controlled
  verification purpose ends.

Acceptance requires a resource inventory, cost-by-resource evidence, confirmed
backup/restore path, deletion plan approval, post-cleanup forecast, and no loss
of app availability or recoverability.

P1 accepted-point breakdown:

| Gate | Points | Status |
| --- | ---: | --- |
| P1-A Resource and dependency inventory | 1 | Accepted and locked |
| P1-B Protected database archive and disposable restore proof | 2 | Accepted and locked |
| P1-C Inactive live SQL database-copy retirement | 1 | Accepted and locked |
| P1-D Legacy Qatar environment and deployment-identity retirement | 2 | Accepted and locked |
| P1-E GitHub deployment and staging availability after cleanup | 1 | Accepted and locked |
| P1-F Budget thresholds, forecast alerts, and monitoring ingestion cap | 1 | Accepted and locked |

P1 Phase B and Phase C evidence, 2026-08-14:

- Nine BACPACs and their SHA-256 manifest are retained under protected Blob
  prefix `p1-phase-b/20260814-145021/`; a disposable restore of the active
  database archive succeeded before the eight inactive live database copies
  were deleted.
- The no-secret legacy configuration, RBAC, OIDC and workflow recovery manifest
  is retained under `p1-phase-c/20260814-162641/`. The post-cleanup evidence file
  `cleanup-result.json` has SHA-256
  `230acd2574a22038a9e237fab133dd5f1e45135dee3b7a0ff43e1289a0957766`.
- GitHub workflow run `31805070116` completed successfully from `main` using
  staging-owned identity `id-acuityops-gh-stg-za-001`, branch-specific OIDC and
  `Website Contributor` permission at the staging App Service scope only.
- The legacy Qatar web app, Windows B1 plan, Application Insights component,
  Smart Detection action group, two obsolete managed identities, Qatar Log
  Analytics workspace and both empty Qatar resource groups were deleted only
  after replacement deployment and dependency verification passed.
- Only `rg-acuityops-stg-za-001` remains. The retained SQL server contains one
  active Basic database, `sqldb-acuityops-stg`, plus the system `master`
  database. The staging Linux app is running and the root, workspace login,
  CSS and rendered AcuityOps logo returned HTTP 200 after cleanup; the user
  confirmed authenticated staging login.
- Fixed monthly infrastructure is projected at approximately `USD 24.89`.
  Normal one-client usage is projected at `USD 25-45/month`, with the existing
  `USD 75` ceiling retained. The confirmed fixed monthly reduction is
  approximately `USD 117.41`: `USD 57.18` from retiring eight live SQL copies
  and `USD 60.23` from retiring the Qatar Windows B1 environment.
- P1-F closed on 2026-08-14. Budget `budget-acuityops-stg-monthly` remains at
  `USD 75` and now sends both actual and forecast notifications at 50%, 75%,
  90%, and 100% to `admin@vectoropsgroup.com`.
- Log Analytics workspace `log-acuityops-stg-za-001` now has a `0.1 GB/day`
  ingestion cap, retains 30 days of logs, remains on `PerGB2018`, and was
  healthy after the change. The preceding 30-day billable-ingestion query
  returned zero usage, so the cap preserves substantial diagnostic headroom
  while bounding a runaway logger to approximately 3 GB/month.
- Application Insights remains provisioned in Log Analytics ingestion mode and
  linked to the retained workspace. App Service availability remained Normal;
  GitHub staging workflow run `31805070116` remained successful; and the public
  root, workspace login, CSS, and AcuityOps logo all returned HTTP 200.
- No product source, tenant data, database data, or Azure resource topology was
  changed for P1-F. P1 is accepted and locked at `8/8`.

### P2 Pilot Infrastructure And Tenant Lifecycle

Acceptance requires:

- one stable pilot URL deployed from committed GitHub source through CI;
- one ordinary pilot tenant with no seed/fallback identity;
- tenant-owned storage paths, database queries, sessions, uploads, AI jobs,
  reports, audit logs, and assignments scoped by company;
- a twelve-month Premium pilot entitlement with start, expiry, status, audit,
  reminder, expiry, and manual extension/revocation behavior;
- expiry never deletes tenant data and changes unavailable Premium functions to
  an explicit read-only/export state;
- Setup Wizard completion, owner/senior/ops/staff access, and first-operation
  guidance;
- managed identity, Key Vault, backups, restore, telemetry, deployment, and
  rollback verified; and
- a documented path for adding another tenant later without client-specific
  code or schema changes.

P2 closed on 2026-08-14 with `14/14` points accepted. Completion evidence:

- source commits `14afbe4` and `717b64a` implement the tenant-owned Premium
  entitlement lifecycle and preserve Import History in read-only mode;
- additive migration `20260814150000_AddPilotEntitlementLifecycle` was verified
  in disposable provider tests before controlled staging application;
- protected pre-migration backup
  `pre-p2-entitlement-20260814-174622.bacpac` was retained in the approved
  database archive and verified before the staging write;
- Release build and the targeted tenant-isolation suite passed with zero build
  errors or warnings;
- GitHub deployment runs `31811506015` and `31821360229` completed through the
  approved Linux staging workflow, including static-asset checks;
- staging verified activation, exact twelve-month expiry, reminders, manual
  revocation/reactivation, persisted audit history, restart persistence, and an
  active final entitlement ending 2027-08-14;
- revoked access preserved existing imports and tenant data, exposed explicit
  read-only/export behavior, and removed Import History mutation actions;
- owner/senior administration, staff/operational-manager default denial,
  company scoping, and no automatic entitlement creation were verified by the
  targeted authorization and isolation tests; and
- no Azure resource was added and no new fixed monthly infrastructure cost was
  introduced. The previously pending compliance-source foundation migration
  was applied as inert product-owned schema only; it created no regulatory,
  tenant, or add-on data and did not reopen Add-on Track A1.

### P3 Core Operational Product Completion

This gate is accepted and locked from historical B2-B3 evidence. It covers:

- company setup and operational structure;
- staff, vehicle, equipment, stock, and medication registers;
- movement, allocation, service, expiry, tasks, issues, and feedback;
- role and area boundaries;
- blank checklist authoring, scoped publishing, daily checks, and checklist
  source-of-truth behavior; and
- no seed, fixed-form, or fallback product data.

### P4 Premium Import, Knowledge, And Forecasting

Acceptance requires:

- locked deterministic Excel/CSV mapping, validation, correction, duplicate,
  transactional commit, removal, and checklist conversion behavior;
- AI suggestions that remain advisory, tenant-scoped, budgeted, privacy-safe,
  human-reviewed, and unable to bypass deterministic mutation controls;
- AI-assisted register imports support operational staff, asset, stock, and
  medication source structures without granting login access;
- SOP/CPG PDF and Word uploads become immutable, versioned, cited, searchable
  tenant knowledge with human review and no invented clinical guidance;
- 3/6/12-month forecasts use tenant operational evidence to identify likely
  shortages, failures, expiry, service pressure, recurring issues, and data
  quality limitations in clear language;
- forecasting never claims legal or regulatory compliance and never invokes
  Add-on Track A1; and
- all AI use has cost ledgers, per-company limits, provider-failure fallback,
  retention controls, and prompt/response privacy boundaries.

### P5 Notifications And Evidence

Acceptance requires:

- locked immutable report detail and PDF parity remain unchanged;
- event definitions for assignment, issue, task, checklist failure, expiry,
  service, import completion/failure, access activation, and pilot entitlement
  expiry;
- email/SMS preferences, recipient scope, templates, provider abstraction,
  retries, opt-out where permitted, delivery status, failure handling, and audit;
- messages contain no unnecessary sensitive or patient-identifiable data; and
- notification links resolve to authorized evidence or work items only.

### P6 Security, Data Lifecycle, And Support

Acceptance requires:

- targeted application security, dependency, secrets, access, tenant,
  upload/file, audit, and operational threat review;
- successful backup restore and release rollback drills;
- complete tenant export with manifest, records, evidence, files, audit data,
  and integrity hashes;
- governed deletion, retention, legal hold, cancellation, and offboarding paths;
- pilot privacy, processing, acceptable-use, support, incident, backup, data
  ownership, and feedback terms reviewed with qualified counsel;
- support contacts, severity levels, response targets, escalation, incident
  evidence, and change communication; and
- no pilot data is used to train product AI models.

### P7 Full UI And Mobile Quality

Acceptance requires one consolidated pilot-route sweep covering:

- shared AcuityOps shell and authenticated tenant branding;
- setup, Home, all registers, movement, tasks/issues, checklist management,
  daily work, reports/PDF, imports, knowledge, forecasts, notifications,
  profile/access, and export/offboarding routes;
- desktop and representative phone widths;
- app-style actions, list density, grouping/collapse, sticky horizontal controls,
  responsive text, forms, confirmations, success/error/loading/empty states,
  keyboard access, contrast, and no dead ends; and
- no page-specific duplicate navigation, placeholder control, missing asset, or
  prototype artifact.

### P8 Pilot Onboarding And Acceptance

Acceptance requires:

- the pilot client is provisioned through supported paths and receives the
  dated Premium entitlement;
- the client completes Setup Wizard and adds/imports its own staff, operational
  structure, assets, stock, medication, checklists, and documents;
- staff, operational managers, senior managers, and owner complete the signed
  role-based acceptance matrix;
- one complete daily operational cycle, issue/task cycle, movement cycle,
  checklist/report/PDF cycle, import cycle, knowledge query, forecast, and
  notification cycle passes;
- restore, export, support, incident, entitlement-expiry, and offboarding
  rehearsals pass without deleting pilot data;
- all severity-one and severity-two defects are closed; and
- the client reaches stable normal use with a documented support and change
  process.

## Explicit Deferrals

The following remain valid future requirements but are excluded from pilot
progress and execution:

- automated billing, invoices, VAT/tax, payment providers, failed-payment
  automation, refunds, downgrade automation, and paid renewal;
- public website, SEO, public pricing, lead capture, demos, public trials, and
  sales conversion;
- enterprise client-specific releases, multiple production regions, dedicated
  databases per large tenant, and large-client Azure scaling;
- complete global schematic-library expansion beyond the schematics necessary
  for the pilot's real fleet; and
- Add-on Track A1, including every B6.2+ implementation step.

Deferral does not delete these requirements. It prevents them from consuming
pilot budget or delaying the first operational client.

## Cost-Control Execution Rules

1. Work uses the fewest coherent batches; row-by-row micro-work is prohibited.
2. Before source work, state scope, exclusions, risk, verification, commit plan,
   and cost impact.
3. Inspect only direct dependencies. Full-app audits occur only at P7, P8, a
   security gate, or by explicit instruction.
4. Reuse locked evidence. Reverification requires a reproducible regression.
5. Use one build, one source commit, one deployment, and one evidence update per
   coherent passing batch unless a declared risk requires separation.
6. Prefer existing resources, libraries, provider abstractions, and accepted
   contracts over new services or parallel implementations.
7. Fake tenant data may be used only for controlled functional verification and
   must be ordinary removable tenant data.
8. No Azure resource is created, upgraded, or retained without a monthly cost,
   owner, purpose, deletion condition, and approval.
9. A budget is an alert, not a cap. Forecast and resource-level cost are checked
   before provisioning and after deployment.
10. Major unfinished capability work is never mixed into cleanup or UI batches.

## Verification Rules

1. Automated Release build and targeted tests precede browser verification.
2. Browser verification covers changed routes and one directly dependent flow.
3. Tenant or permission changes require two-tenant and role-boundary tests.
4. Migration changes require disposable SQLite apply/rollback and SQL Server
   script review before active-environment application.
5. Controlled active writes require backup, supported UI or governed command,
   acceptance evidence, and safe cleanup.
6. Pilot evidence and reports are never backfilled, inferred, or rewritten.
7. GitHub CI is the committed-source gate; Azure deployment uses the accepted
   workflow only.
8. Documentation-only changes require no build, database, browser, or Azure use.

## Reasoning Levels

- `Medium`: bounded implementation, UI, documentation, routine tests, builds,
  deployment, and targeted verification.
- `High`: migrations, tenant isolation, authentication, permissions, evidence
  integrity, AI privacy, data lifecycle, cost-bearing resource deletion, and
  cross-module source-of-truth changes.
- `XHigh`: only irreversible production security, tenancy, provider,
  data-residency, or legal architecture decisions not resolved by an approved
  decision record.

## Hard Stops

Stop before proceeding when:

- a destructive or unverified migration is required;
- tenant data, historical evidence, product-owned assets, or login access could
  be lost, rewritten, leaked, or silently recreated;
- safe backup, restore, cleanup, or rollback is unavailable;
- Azure forecast reaches USD 60 or a new recurring resource is required without
  approval;
- authentication, external legal review, provider ownership, or client action
  requires the user;
- a requirement conflicts with this pilot boundary; or
- work would enter an explicit deferral or Add-on Track A1.

## Historical Add-On Evidence

Add-on Track A1 remains separately governed by
`docs/specs/sa-doh-compliance-pack-add-on-contract.md`. Historical B6.0 and B6.1
evidence is retained later in this file but contributes no pilot points and
creates no pilot dependency.

## Accepted Evidence

### Block 1: Stable Staging And Committed-Source Foundation

Status: Accepted and locked

Accepted evidence includes:

- Azure staging deployment workflow: `014d8eb`.
- Branch-based Azure OIDC staging deployment: `154fecb`.
- Azure CLI resource-group deployment correction: `abea065`.
- Stable staging runbook authority: `b1d59a0`.
- Authenticated app-shell polish: `c7af22f`.
- Checklist/readiness consistency fixes: `d9d69ef`, `f1061dc`, and
  `637c401`.
- Formal Block 1 closeout: `eaab674`.
- Staging evidence records successful public root, CSS, workspace login,
  submitted report detail, PDF media response, readiness visibility, and
  Operations Reports reconciliation.

This block may not be rechecked without a Verified-Work Finality Rule trigger.

### Block 2: Base Commercial Foundation

Status: Accepted and locked

Accepted evidence includes:

- Block 2 execution blueprint: `88cdbef`.
- Setup completion decoupled from branding: `ae8da60`.
- Reliable staging package path: `45e5692`.
- Server-side core action enforcement: `a6a9d59`.
- Access-enforcement evidence: `51b9f93`.
- Formal Block 2 closure: `0b98720`.
- Azure staging evidence records all five Block 2 batches complete, including
  post-permission report detail, PDF, Readiness Dashboard, and Operations
  Reports reconciliation.

This block may not be rechecked without a Verified-Work Finality Rule trigger.

## Closed Block 3: Base Manual Operations Completion

Block progress: `100% (6/6 accepted; closed)`

Authority: `docs/specs/block-3-base-manual-operations-execution-blueprint.md`

| Batch | Objective | Dependencies | Status | Acceptance summary | Reasoning | Credits | Verification-first opportunity |
| --- | --- | --- | --- | --- | --- | ---: | --- |
| B3.1 | Stock Register and Distribution Flow | B2 | Accepted and locked | Unified stock register/order/supplier/receipt/allocation flow works with edit, move, issue, delete, grouping, scope, and audit | Medium | 1,800-2,600 | Accepted from targeted staging verification and commit `b94e1cf` |
| B3.2 | Medication Register Consistency | B3.1 only where stock patterns are shared | Accepted and locked | Medication grouping/search/detail/edit/move/issue/delete, expiry, role scope, and audit pass | Medium | 1,200-1,800 | Accepted from targeted staging verification and commit `b94e1cf` |
| B3.3 | Cross-Asset Movement Completion | B3.1-B3.2 source paths | Accepted and locked | Relevant assets can move among vehicles, bases, storage, and areas with confirmation and audit | Medium; High if schema gap is proven | 1,400-2,100 | Accepted from targeted staging and handler verification |
| B3.4 | Tasks, Issues, And Feedback Coherence | B2 permissions | Accepted and locked | Role scope, task-specific feedback, completion/removal, delete authority, notifications, and audit behave coherently | Medium | 1,500-2,300 | Accepted from targeted staging and handler verification with locked B2 enforcement evidence |
| B3.5 | Manual Checklist Authoring Completion | B2 checklist source of truth | Accepted and locked | Blank builder supports sections/items/subitems/columns/notes/register links and correct scoped publish/live display | Medium; High if migration is required | 2,400-3,600 | Accepted from targeted staging authoring and live-check verification |
| B3.6 | Base Manual Operations Closure Regression | B3.1-B3.5 | Accepted and locked | One targeted staging pass proves all Block 3 workflows and locked B2 boundaries | Medium | 600-1,000 | Accepted from 2026-07-13 Slice 1-3 evidence; no regression trigger found |

Historical Block 3 acceptance is retained as P3 evidence. The historical block
percentage is superseded by the pilot point model above.

### Block 3 Slice 1 Evidence

- Staging verification on 2026-07-13 used existing Demo EMS Service tenant data
  only; no records were created, modified, or deleted.
- `/Stock`, `/StockRegister?view=register`, and `/StockOrders` rendered the
  unified stock workflow, grouped register, existing stock item actions, and
  a correct zero-order empty state.
- `/Medication` and `/MedicationRegister?view=register` rendered the grouped
  medication register and existing medication item actions.
- Senior management and the assigned operational-manager session both exposed
  Edit, Move, Issue, and Delete for the Central Operations stock and medication
  records. The staff session was redirected away from both protected registers
  to operational-management login, proving it cannot access those manager
  actions.
- `b94e1cf` supplies the accepted server-side area authorization for stock and
  medication edit/delete plus guarded stock-order lifecycle transitions.

### Block 3 Slice 2 Evidence

- Staging verification on 2026-07-13 used the existing Demo EMS Service tenant
  records only; no movement, task, issue, or feedback record was created or
  modified.
- `/MoveAsset` rendered one consistent movement flow for vehicle, equipment,
  stock, and medication. Each existing selector included the active base,
  operational area, region, storage space, and vehicle where applicable.
- `MoveAsset` validates company-owned assets and destinations, permission to
  move, task assignment permission, and records both movement and task
  completion audit events. It rejects invalid or cross-company selections.
- Senior management received company-wide issue scope. Operational management
  received the scoped issue page labelled "Assigned to you". Staff was kept out
  of manager-only stock and medication registers during Slice 1.
- `/TaskInbox`, `/IssueReports`, and `/TaskFeedback` rendered coherent empty
  states. Task feedback exposes separate specific-task and general-feedback
  paths; the handler closes only the signed-in user's open task and writes task
  events and audit logs. Existing B2 access-enforcement evidence remains locked
  and is reused rather than retested.

### Block 3 Slice 3 And Closure Evidence

- Staging verification on 2026-07-13 used the existing Demo EMS Service tenant
  only; no checklist, scope, live check, asset, or report record was created,
  edited, published, or deleted.
- `/EditVehicleChecklist?checklist=daily-vehicle&mode=build` started with no
  sections. It exposed optional manual naming, function/subtype targeting, blank
  section creation for Vehicle, Equipment, Stock, Medication, or Custom, and
  no hidden template or checklist scope.
- The existing template editor exposed section controls, editable field rows,
  X-axis column settings, register links, per-row overrides, subitems, notes,
  and scoped publish controls. The register displayed the active exact subtype
  scope and its publication evidence.
- `/DailyVehicleChecklist?frequency=daily&registration=DEM-101&callsign=A01`
  loaded only the published Operational Ambulance checklist. Its vehicle,
  schematic, and Monitor Defibrillator sections matched the register-published
  template; no fixed-form fallback rendered.
- The closure check reuses locked B2 checklist/report/access evidence and the
  Slice 1 and Slice 2 staging evidence above. No reproducible regression or
  source/schema gap was found. Block 3 is accepted and locked.

### Block 4: PDF Evidence And Report Reliability

Status: Accepted and locked

| Batch | Objective | Status | Acceptance summary | Reasoning | Credits |
| --- | --- | --- | --- | --- | ---: |
| B4.1 | Submitted evidence data contract | Accepted and locked | One tenant-scoped report model contains tenant, staff, time, vehicle/callsign, template/version, all dynamic values, notes, issues, schematic marks, and submission metadata | High | 1,500-2,200 |
| B4.2 | Professional PDF evidence output | Accepted and locked | Every submitted checklist downloads a readable, complete, print-safe PDF generated from the accepted report contract | High | 2,500-3,500 |
| B4.3 | Report search, grouping, drilldowns, and scope | Accepted and locked | Checklist and Operations Reports provide concise list drilldowns and required search/grouping within senior/ops tenant scope | Medium/High | 2,000-2,800 |
| B4.4 | Evidence staging closure | Accepted and locked | Targeted desktop/mobile/print staging verification proves report detail and PDF parity without database fallback | High | 1,000-1,500 |

### Block 4 Acceptance Evidence

- `b132bc6` adds the additive, versioned immutable checklist evidence snapshot,
  submission capture, snapshot reader, and clearly separated legacy adapter. No
  historical report was backfilled, rewritten, or inferred.
- `ac0573f` generates professional report PDFs from the immutable evidence
  contract. Build and tenant tests passed, the PDF was rendered for print
  inspection, and Azure staging served the authenticated download route.
- `d6671f6` makes Checklist Reports, report detail, PDF, and Operations Reports
  resolve submitted identity, scope, template version, status, and counts from
  the same immutable snapshot. Tenant tests prove later register changes cannot
  alter submitted evidence.
- `6064642` confines the Readiness Dashboard completed-check metric to the
  current 12-hour shift and excludes retired evidence consistently.
- GitHub Linux staging workflow runs `29240493100`, `29242792250`,
  `29244105194`, and `29245705089` deployed the accepted Block 4 commits with
  build, publish, static-asset, OIDC, deployment, and deployed-asset checks.
- Controlled staging verification created report `4` through the real daily
  check UI for `DEM-101 / A01`. Checklist Reports, immutable detail, PDF,
  Readiness Dashboard, and Operations Reports agreed on the captured identity,
  Operational status, and zero issues. An operational manager without assigned
  area scope could not open the report directly.
- The same controlled report was retired through the supported in-app
  confirmation workflow. Checklist Reports and Operations Reports removed it,
  and the deployed current-shift dashboard returned to zero completed checks.
- Mobile-width report detail verification passed at `390px` without horizontal
  page overflow. The temporary evidence was not created by seed, fallback, or
  direct SQL, and no historical report was altered.

### Block 5: Pro Import, Column Matching, And Conversion

Status: Accepted and locked; 5/5 batches accepted

| Batch | Objective | Status | Acceptance summary | Reasoning |
| --- | --- | --- | --- | --- |
| B5.1 | Import foundation and deterministic contract | Accepted and locked | Additive tenant-owned import ledger, bounded parser, canonical field registry, Pro/permission gates, source-evidence upload handoff, and no-domain-write boundary pass | High |
| B5.2 | Deterministic register mapping and commit | Accepted and locked | Tenant-scoped mapping, conversion, correction, duplicate decisions, transactional commit, and non-login staff profile safety pass | High/Medium |
| B5.3 | Deterministic checklist conversion | Accepted and locked | Explicit-column, matrix, sectioned-sheet, and one-sheet-per-section sources create editable drafts without hidden publication | High/Medium |
| B5.4 | Audit, removal, mapping reuse, and Pro UX | Accepted and locked | Tenant-bound mapping reuse requires reconfirmation; import history and governed removal preserve conflicts and remove only unchanged unused records | High/Medium |
| B5.5 | Integrated staging acceptance and closure | Accepted and locked | Automated tenant tests and real staging UI acceptance pass; temporary product records removed and original live checklist restored | High |

### B5.1 Acceptance Evidence

- Source commit `1d41db9` adds the versioned import ledger, canonical field
  registry, deterministic source inspector, Pro feature and action-permission
  gates, tenant-scoped import-batch route, and upload-to-review handoff.
- Parser decision `decisions/adr-b5-import-parser-libraries.md` records
  ExcelDataReader `3.9.0`, MIT compatibility, supported formats, bounded parser
  limits, and the explicit exclusion of AI and automatic domain writes.
- Release builds for the application and tenant-isolation test executable
  passed with zero warnings and zero errors on 2026-07-13.
- All ten tenant, evidence, PDF, and import tests passed. Import-specific tests
  prove quoted CSV parsing, the canonical field contract, Pro/permission
  boundaries, tenant isolation, and that upload preparation creates no vehicle,
  staff, equipment, stock, medication, or checklist records.
- `20260713190000_AddImportFoundation` was applied to an isolated disposable
  SQLite baseline. Four import tables, fourteen indexes, three uniqueness
  constraints, fourteen restrictive foreign keys, and migration history were
  verified. Rollback removed only B5.1 objects and preserved the preceding
  immutable-evidence schema. The temporary database and inspector were deleted.
- No active development or Azure staging database was migrated, and no staging
  deployment or operational data write occurred during B5.1 acceptance.

### B5.2-B5.5 Acceptance Evidence

- Source commits `579cde1`, `2a554db`, `75840c3`, `00b361a`, and `1b88f1a`
  implement deterministic register import, checklist conversion, governed
  history/removal, cross-request removal safety, and terminal import-history
  rendering independent of temporary source files.
- Staff-profile and login-identity separation commits `dedab8a` and `0342350`
  preserve tenant-owned staff profiles while requiring explicit Access Setup
  activation for real login identities. Importing staff creates no credentials
  or login access.
- The Release build passed with zero warnings and zero errors. All sixteen
  targeted test groups passed, covering tenant isolation, immutable evidence and
  PDF consistency, deterministic imports, staff identity separation, all four
  checklist layouts, mapping reuse, transaction boundaries, and governed
  removal.
- GitHub Azure staging workflow run `29682027504` deployed the accepted source
  successfully. Staging terminal import history remained readable after the
  original local upload source was unavailable.
- Real staging UI acceptance imported `stock-messy.csv`, confirmed deterministic
  alias mapping, excluded one malformed row and one duplicate source row,
  transactionally committed the single valid row, and verified exactly one
  normal Stock Register record. A second import reused the saved tenant mapping,
  required mapping reconfirmation, committed successfully, and was removed
  through Import History. Both temporary stock records were removed.
- Real staging UI acceptance imported one staff profile, verified it appeared in
  the Staff Register with no configured login identity or credentials, and
  removed it through Import History.
- Real staging UI acceptance converted explicit-column CSV, checklist-matrix CSV,
  sectioned-sheet CSV, and one-worksheet-per-section XLSX sources into named,
  editable drafts. Crew preview rendered the imported vehicle and equipment
  fields. None became live while in Draft status.
- Explicit publication of the acceptance checklist to `A01 / DEM-101` was the
  only action that made it load in Daily Vehicle & Equipment Check. Deleting the
  temporary template removed its live scope and restored the prior published
  subtype checklist. All temporary checklist templates and imported register
  records were removed through supported UI workflows. Import-history evidence
  remains as the tenant audit record; already-deleted templates are reported as
  protected removal conflicts rather than rewritten.
- No seed/fallback data, direct SQL product-data creation, historical-evidence
  rewrite, product-owned schematic change, or Block 1-4 reopening occurred.

### Block 7: Premium AI And Knowledge Intelligence

Status: In progress; 0/3 parts accepted; 0/12 points earned

Proposed authorities:

- `docs/specs/block-7-premium-ai-knowledge-intelligence-execution-blueprint.md`
- `docs/specs/adr-block-7-ai-provider-and-data-residency.md`

| Part | Objective | Status | Core boundary |
| --- | --- | --- | --- |
| B7.1 | Premium AI Import Intelligence | Source, privacy controls, Azure resources, managed identity, migration, and feature configuration implemented; live structured-mapping acceptance blocked by a confirmed validation defect | AI suggestions must enter the accepted Block 5 deterministic review, validation, commit and publication contract |
| B7.2 | SOP/CPG Knowledge System | Not started | Immutable tenant sources, reviewed extraction, tenant-scoped search, and cited Q&A; no A1 conclusions |
| B7.3 | Operational Forecasting And Integrated Closure | Not started | 3/6/12-month operational findings and explanations only; no regulatory compliance forecasting |

B7.1 source evidence:

- Commit `93a719c` implemented Premium AI import intelligence behind Premium
  entitlement, saved permission, tenant AI policy, strict structured output,
  bounded cost controls, human review and the accepted Block 5 mutation
  boundary.
- Commit `f307578` approved GlobalStandard for the bounded B7.1 operational
  import use case and hardened the privacy boundary: explicit patient-data
  exclusion confirmation, server-side patient-identifier screening, complete
  omission of staff row values and identifying source metadata from AI prompts,
  and generic failure recording without prompt or response bodies.
- Release build passed with `0` warnings and `0` errors.
- The complete tenant/security executable passed, including Premium AI tenant
  scope, cost limits, manager-role boundary, Block 5 handoff, no staff login
  creation, privacy confirmation, staff-value prompt omission, patient-field
  detection and raw-provider-output exclusion from failure records.
- Azure OpenAI resource `oai-acuityops-stg-za-001` and GlobalStandard deployment
  `ai-structured-low-cost` were provisioned for the approved bounded use case.
- Azure Document Intelligence resource `di-acuityops-stg-za-001`, queue
  `premium-ai-import`, managed-identity role assignments, tenant AI policy, and
  feature configuration were established for staging.
- Staging backup `sqldb-acuityops-stg-before-b7-1-20260803-124057` was taken
  before additive migration `20260718160000_AddPremiumAiImportGovernance` was
  applied.
- Controlled synthetic stock-import acceptance reached the AI suggestion path
  but failed strict structured-output validation. The page also exposed
  `ChecklistLayout` and `AiDecision` as required form fields. No stock record
  was written, and the defect remains the direct B7.1 acceptance blocker.
- Commits `b460fed` and `29d561f` contain the related guarded B7.1 staging and
  identity work. B7.2 did not begin.

Provisioning does not accept B7.1. B7.1 remains open until the confirmed
structured-output and form-validation defect is fixed and the controlled
staging acceptance matrix passes. Under this pilot roadmap, the accepted
deterministic Block 5 evidence already contributes `10` points to P4. Further
P4 points are awarded only at named AI, knowledge, and forecasting acceptance
gates.

## Add-On Track A1: South African DOH Compliance Pack

Add-on progress: `12.5% (1/8 stages accepted; externally blocked)`

Core-launch impact: `None`

Authorities:

- `docs/specs/block-6-sa-private-ambulance-inspection-mode-blueprint.md`
- `docs/specs/block-6-authoritative-source-register.md`
- `docs/specs/sa-doh-compliance-pack-add-on-contract.md`

| Stage | Objective | Status | Evidence |
| --- | --- | --- | --- |
| B6.0 | Source acquisition and legal-pack gate | Blocked: external review | Public-source acquisition and missing-source remediation are documented in commit `db57bb7`; no pack has completed authority-response, qualified legal-review and private-EMS operational-review gates, and no requirement is approved or active |
| B6.1 | Source registry and province-aware pack governance foundation | Accepted and locked | Commit `0a5df3b`; Release build passed with zero warnings/errors; full isolation suite and disposable SQLite/SQL Server migration checks passed |
| B6.2 | Tenant compliance profile and evidence contract | Not started | Blocked from execution until explicitly authorized after B6.0 dependency review |
| B6.3 | Deterministic evaluation and domain adapters | Not started | Depends on approved source and tenant evidence contracts |
| B6.4 | Inspection workspace and corrective actions | Not started | Depends on B6.3 |
| B6.5 | Immutable snapshot and evidence pack | Not started | Depends on B6.3-B6.4 |
| B6.6 | First pack activation | Not started | Requires completed legal and operational approval; no pack is active |
| B6.7 | Integrated tests, staging and legal closure | Not started | Add-On Track A1 closure gate |

### B6.1 Accepted Evidence

- Product-owned jurisdiction, source, clause, pack, requirement, applicability,
  evidence-definition, review, relationship and governance tables carry no
  tenant assignment.
- South Africa, all nine provinces, optional nested district/municipality
  levels, multi-province selection and future-country roots are supported by
  structure only; the migration inserts no regulatory or tenant records.
- National baseline and provincial overlays remain separately sourced,
  versioned and reported. Missing or incomplete provinces return
  `Source pack incomplete` and cannot produce an authoritative composition.
- Governance writes are default-deny and require product-level authorization;
  no tenant permission key, tenant mutation route or automatic rule creation
  was added.
- Lifecycle, clause provenance, conflict handling, one active version,
  post-activation immutability, SQLite apply/rollback, SQL Server script
  compatibility and zero-record creation were verified in disposable tests.
- No active development or Azure database was migrated, no rule was activated,
  no deployment occurred and product-owned schematics were untouched.

### B6.0 Evidence And External Block

- Commit `db57bb7` records the source-acquisition report, national and
  province-specific completeness matrix, candidate clause extracts,
  independent-verification record, unavailable-source register, missing-source
  remediation report, formal authority-request pack, and the prepared legal and
  private-EMS operational review dossiers.
- The retained external evidence vault contains 71 artifacts. Its inventory
  SHA-256 is
  `7c82a072279710b56cf59f49e3bc0091090d39b747270eaa0ceea5c272fda997`;
  the supplemental-manifest SHA-256 is
  `3557670751a9ed5fe65655cf7c00cec3a707d1f2ee24104ed63e6f673d93bfa6`.
  Independent verification found zero hash mismatches and no duplicate
  supplemental-manifest rows.
- National sources and all nine provincial jurisdictions were assessed.
  Unavailable or incomplete official records remain explicitly identified and
  have not been substituted with another province's requirements.
- `requirementsActivated = 0` and `tenantComplianceRecordsCreated = 0`.
  Candidate extracts are not authoritative requirements.
- B6.0 remains blocked pending responses from the responsible national,
  provincial and professional authorities, qualified legal review, and
  province-specific private-EMS operational review. B6.2 may not begin while
  this gate remains blocked.

## Migrated Historical Roadmap Reconciliation

- Historical B1 maps to P2 and contributes `7` accepted points.
- Historical B2-B3 map to P3 and contribute `20` accepted points.
- Historical B4 maps to P5 and contributes `8` accepted points.
- Historical B5 maps to P4 and contributes `10` accepted points.
- Historical B7 maps to the unfinished portion of P4. Its implemented but
  unaccepted B7.1 evidence is retained without receiving points prematurely.
- Historical B8 notification work maps to P5. Global schematic-library
  expansion is deferred except for models required by the pilot's actual fleet.
- Historical B9 infrastructure and security requirements map to P2 and P6.
- Historical B10 export, retention, and offboarding requirements map to P6.
  Automated billing and subscription commerce are deferred.
- Historical B11 support, incident, legal, and operating requirements map to
  P6.
- Historical B12 public website, pricing, trial, and sales work is deferred.
- Historical B13 customer activation is replaced by P8's real pilot onboarding
  and acceptance gate.
- Historical B6.0-B6.7 remain Add-on Track A1 and contribute no pilot points.

No requirement or accepted evidence is deleted by this mapping. Deferred work
is retained in the requirement roadmaps and Git history, but cannot alter pilot
progress or execution order.

## Next Authorized Action

Proceed to P4 Premium Import, Knowledge, And Forecasting. First close the
confirmed live B7.1 structured-mapping acceptance defect while preserving the
accepted Block 5 deterministic import mutation boundary, tenant isolation,
human review, privacy controls, and cost caps. Then complete the SOP/CPG
knowledge and 3/6/12-month operational forecasting acceptance gates. Do not
enter Add-on Track A1, claim regulatory compliance forecasting, or reopen
P1-P3 without a reproducible regression.

## Update Rules

1. Only this file may report overall pilot or add-on completion percentages,
   block status, execution order, or the next authorized action.
2. Only accepted named pilot gates change the overall pilot percentage. Add-on
   acceptance changes only the add-on progress and availability state.
3. Each accepted coherent batch updates its block numerator, evidence, actual
   cost or credits when known, and remaining risk in one docs commit.
4. The next authorized action must name exactly one pilot block and the smallest
   coherent batch within it.
5. Any execution-order, scope, weight, or 100%-definition change requires an
   explicit approved edit to this file before implementation.
6. Requirement roadmaps, blueprints, ADRs, contracts, and runbooks may define
   implementation detail but may not publish a competing percentage, block
   status, execution order, or next action.
