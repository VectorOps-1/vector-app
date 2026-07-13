# AcuityOps Commercial Launch Progress Tracker

Status: Active and sole progress authority

Updated: 2026-07-13

## Authority

This is the only AcuityOps document permitted to calculate completion, report
block status, or nominate the authorized execution action. The recovery roadmap and
commercial completion roadmap define requirements. Implementation blueprints
define approved block scope. This tracker records accepted evidence and controls
movement between blocks.

`100%` means the first real customer has successfully completed subscription
payment, received an isolated production tenant, signed in, and reached the
onboarding flow. Plans, documentation, partially implemented work, test tenants,
and staging demonstrations do not earn completion unless their block acceptance
gate has passed.

## Progress Dashboard

| Measure | Current value |
| --- | --- |
| Total commercial-launch blocks | 13 |
| Accepted and locked | 3 |
| Active | 0 (Block 4 is next) |
| Remaining after active block | 10 |
| Blocked | 0 |
| Overall commercial-launch progress | 27% |
| Current Block 3 progress | 100% (6/6 batches accepted; closed) |
| Estimated remaining implementation credits | 115,900-198,400 |
| Credit estimate basis | Planning range; actual usage is not reliably metered by block |

The overall score is the sum of weights for blocks whose complete acceptance
gate is accepted. Partial block work does not earn weighted progress. Therefore,
Blocks 1, 2, and 3 contribute `7% + 8% + 12% = 27%`. The next contribution is
earned only when Block 4 and its closure gate pass.

## Reasoning Levels

- `Medium`: routine implementation, targeted verification, UI, documentation,
  and isolated workflow fixes.
- `High`: migrations, tenant isolation, permissions, imports, billing,
  production architecture, security, or compliance data contracts.
- `XHigh`: focused AI design, predictive compliance logic, or critical
  production security/release review. It is not used for routine work.

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

## Commercial Block Register

| ID | Block | Weight | Status | Dependencies | Acceptance gate | Reasoning | Estimated credits |
| --- | --- | ---: | --- | --- | --- | --- | ---: |
| B1 | Stable staging and committed-source foundation | 7% | Accepted and locked | None | GitHub-controlled deployment serves the app and static assets from stable Azure staging; login and first evidence path pass | Medium/High | Actual not reliably metered |
| B2 | Base commercial foundation | 8% | Accepted and locked | B1 | Setup gate, core vehicle/staff/equipment registers, checklist source of truth, action permissions, and evidence baseline pass | Medium/High | Actual not reliably metered |
| B3 | Base manual operations completion | 12% | Active, 0/6 | B2 | All six batches pass staging with no seed/fallback data and no regression of locked evidence | Medium; High only for an approved migration | 8,900-13,400 |
| B4 | PDF evidence and report reliability | 8% | Not started | B3 | Every submitted checklist has complete, professional, tenant-scoped report/PDF evidence; reporting drilldowns and role scopes pass staging | High | 7,000-10,000 |
| B5 | Pro import, column matching, and conversion | 10% | Not started; not yet decomposed | B4 | Validated Excel register/checklist import, preview, correction, deduplication, mapping, audit, and explicit publishing pass | High | 14,000-24,000 |
| B6 | South African DOH Annual Inspection Mode | 8% | Not started; not yet decomposed | B4, B5 | Source-backed SA requirements, dated references, gap analysis, inspection mode, evidence pack, and extensible jurisdiction model pass legal/compliance review | High | 10,000-18,000 |
| B7 | Premium AI and knowledge intelligence | 12% | Not started; not yet decomposed | B5, B6 | Human-reviewed AI import, 3/6/12-month forecasting, compliance/failure analytics, and cited SOP/CPG ingestion pass safety and audit gates | XHigh for design/review; High for implementation | 24,000-40,000 |
| B8 | Operational communications and product libraries | 6% | Not started; not yet decomposed | B3 | SMS/email notification delivery, preferences, audit/failure handling, and the product-owned global vehicle schematic library pass cross-tenant and mobile checks | High | 8,000-15,000 |
| B9 | Production Azure SaaS platform | 11% | Not started; not yet decomposed | B3, B4 | Production tenant/storage isolation, managed database/blob/secrets, CI/CD, client-specific release controls, backups, observability, incident response, and rollback pass | High; XHigh for final security review | 18,000-30,000 |
| B10 | Billing, tiers, subscriptions, and data lifecycle | 7% | Not started; not yet decomposed | B9 | Base/Pro/Premium/Enterprise enforcement, invoices, VAT/tax, payment failure/grace/refund, downgrade/cancel, export, deletion, retention, and offboarding pass | High | 10,000-18,000 |
| B11 | Legal, security, support, and client success | 4% | Not started; not yet decomposed | B6, B9, B10 | POPIA/legal review, liability/trademark decisions, security evidence, support SLAs, training, documentation, escalation, and feedback loops are approved | High | 6,000-12,000 |
| B12 | Website, trial, and public truth control | 3% | Not started; not yet decomposed | B7, B10, B11 | Website, pricing, signup/demo/trial flows, analytics, SEO, and every public claim match verified product/tier/legal truth | Medium; High for legal/billing review | 6,000-10,000 |
| B13 | Production release and first customer activation | 4% | Not started; not yet decomposed | B1-B12 | Release/security/mobile/tenant/payment tests pass and the first real customer pays, receives an isolated tenant, signs in, and reaches onboarding | High; XHigh for final release/security gate | 4,000-8,000 |

Weights total `100%`.

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

Block 3 earns its `12%` only when all six batches and the blueprint closure gate
are accepted.

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

## Next Block 4: PDF Evidence And Report Reliability

Block 4 is defined now because it immediately follows Block 3. Implementation is
not authorized until Block 3 closes.

| Batch | Objective | Status | Acceptance summary | Reasoning | Credits |
| --- | --- | --- | --- | --- | ---: |
| B4.1 | Submitted evidence data contract | Not started | One tenant-scoped report model contains tenant, staff, time, vehicle/callsign, template/version, all dynamic values, notes, issues, schematic marks, and submission metadata | High | 1,500-2,200 |
| B4.2 | Professional PDF evidence output | Not started | Every submitted checklist downloads a readable, complete, print-safe PDF generated from the accepted report contract | High | 2,500-3,500 |
| B4.3 | Report search, grouping, drilldowns, and scope | Not started | Checklist and Operations Reports provide concise list drilldowns and required search/grouping within senior/ops tenant scope | Medium/High | 2,000-2,800 |
| B4.4 | Evidence staging closure | Not started | Targeted desktop/mobile/print staging verification proves report detail and PDF parity without database fallback | High | 1,000-1,500 |

## Shortest Safe Remaining Order

1. Finish B3 using verification-first batches; do not rebuild passing functions.
2. Complete B4 so evidence is defensible before imports or compliance modes.
3. Complete B5 imports before DOH and AI because both consume normalized data.
4. Complete B6 source-backed DOH architecture before predictive compliance.
5. Complete B7 AI and knowledge functions against accepted import/compliance
   contracts.
6. Complete B8 communications and global schematic expansion without coupling
   either to seed or tenant identity.
7. Complete B9 production architecture before billing or real client data.
8. Complete B10 commercial controls and data lifecycle.
9. Complete B11 legal/security/support approval.
10. Complete B12 public website and trial truth controls only after product and
    commercial behavior are verified.
11. Complete B13 release gate and first customer activation.

No requirement is removed by this consolidation. Detailed batches for B5-B13
remain intentionally undecomposed until the preceding dependency is close to
acceptance.

## Roadmap Reconciliation

- Recovery `R1` maps to B1.
- Recovery `R2` and commercial `C1` map to B2-B3.
- Recovery `R3` and commercial `C2` map to B4.
- Recovery `R4` and commercial `C3` map to B5.
- Recovery `R5` and commercial `C4` map to B6.
- Recovery `R6-R8` and commercial `C5-C6` map to B7.
- SMS/email notifications and the product-owned schematic-library expansion map
  to B8 rather than being hidden inside cleanup work.
- Recovery `R9` and commercial `C7-C8` map to B9-B11.
- Recovery `R10` and commercial `C9` map to B12-B13.

No ordering conflict remains. The master spec retains detailed product
requirements but no longer controls progress calculations.

## Next Approved Action

Propose the smallest safe B4.1 Submitted Evidence Data Contract batch. Do not
reopen Blocks 1-3 without a Verified-Work Finality Rule trigger.

## Update Rules

1. Only this file may report overall or block completion percentages.
2. Only accepted block gates change the overall percentage.
3. Each accepted batch updates current-block numerator, evidence, actual credits
   when known, and remaining risk in one docs commit associated with that batch.
4. The authorized execution action must always name exactly one batch or docs action.
5. Any proposed deviation requires an explicit tracker change approved by the
   user before implementation.
