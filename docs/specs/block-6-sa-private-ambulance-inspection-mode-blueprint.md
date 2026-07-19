# Block 6 South African Private Ambulance Service DOH Annual Inspection Mode

Status: Authoritative design blueprint; B6.1 foundation implemented and verified; no requirement pack active

Designed: 2026-07-19

Progress authority: `docs/specs/commercial-launch-progress-tracker.md`

Source authority: `docs/specs/block-6-authoritative-source-register.md`

Roadmap authority:

- `docs/specs/acuityops-master-build-spec.md`, Phase 12B/12C
- `docs/specs/commercial-completion-roadmap.md`, Phase C4

## Objective

Build a deterministic, source-backed mode that compares a private ambulance
service's current AcuityOps records and uploaded evidence side by side with the
applicable South African licensing, inspection, statutory, professional and
clearly classified standards requirements.

The mode must answer, in plain English:

1. What applies to this service, base, vehicle, practitioner or controlled item?
2. Which official source creates or supports the requirement?
3. What evidence does the client already have?
4. What is missing, expired, inconsistent or unable to be verified?
5. What action should be taken first, by whom, and by when?
6. What evidence proves closure?

The primary output is a prioritized corrective-action list. A percentage may be
shown only as secondary orientation and may never hide a licence blocker,
patient-safety failure, expired professional registration, missing essential
equipment, prohibited/unauthorized medicine, or other critical non-compliance.

## Non-Negotiable Product Rules

- This is an inspection-preparation and compliance-evidence system, not a
  regulator and not a substitute for legal advice or an official inspection.
- Only signed, versioned and approved requirement packs may evaluate tenants.
- National, provincial, OHSC, HPCSA, road-traffic, medicines, OHS, waste,
  guidance and client-policy requirements remain separately classified.
- The app must show uncertainty, conflict and unavailable source material; it
  must never guess or silently combine rules.
- Product-owned regulatory rules must be separated from tenant-owned records,
  evidence, actions and results.
- Existing registers are evidence inputs, not automatic proof of compliance.
- Completed compliance sessions are immutable snapshots tied to the exact rule
  pack and evidence versions used at the time.
- A newer pack may trigger a new evaluation but may never rewrite an old result.
- AI may assist later with extraction or explanation, but may never activate a
  requirement, determine legal applicability or silently change status.

## 1. Authoritative Source Register

The controlled source baseline is in
`docs/specs/block-6-authoritative-source-register.md`.

The first source pack must contain:

- the final 2017 national EMS Regulations and exact Annexure A/B extraction;
- a separately classified 2022 national EMS standards overlay;
- applicable National Health Act clauses;
- applicable HPCSA Emergency Care registration, scope, APC and CPD sources;
- applicable Medicines Act, schedules, SAHPRA and permit/process sources;
- applicable National Road Traffic Act/Regulations and PrDP sources;
- applicable health-care-waste, OHS and hazardous-biological-agent sources;
- POPIA, confidentiality and verified record-management sources;
- the selected province's current application, renewal, inspection and licence
  instruments;
- clearly classified NDoH/OHSC guidance where it is not binding law;
- source gaps and conflicts that block authoritative activation.

Every source must retain title, authority, URL/document ID, Gazette/form number,
clause/page/form anchor, dates, jurisdiction, source class, service scope,
verification date, content hash, amendment/supersession state, uncertainty,
reviewers and consuming pack versions.

## 2. Applicability Matrix

Applicability is evaluated explicitly. It is not inferred from a company name or
from a generic `Ambulance` vehicle label.

| Dimension | Required values/behavior |
| --- | --- |
| Country | South Africa for Block 6; architecture supports future countries |
| Province | One or more operating/licensing provinces; each selected explicitly |
| District/municipality | Optional only where an official condition is district-specific |
| Operator type | Private commercial, non-profit/NGO, volunteer, government or other verified class |
| Service category | Ground ambulance, inter-facility transfer, event medical, aeromedical, rescue, response/scene management, training/education provider where sourced |
| Licence category/level | Exact authority-defined class; never derived from marketing labels |
| Clinical capability | BLS, ILS, ALS and exact HPCSA registration/scope categories as defined by current sources |
| Operational object | Company, service licence, province, district, base, area, vehicle, equipment item, medicine, stock item, storage location, practitioner, driver, document, checklist or incident |
| Rule period | Effective date, review date, renewal cycle and supersession state |
| Rule source class | Legislation, regulation, licence condition, official inspection tool, standard, directive, policy, guidance or client policy |

### Pack Composition

A session composes, without merging away provenance:

1. one national private-ground-ambulance licensing baseline;
2. one national standards-readiness overlay if selected;
3. every applicable provincial overlay;
4. applicable professional, medicines, road, OHS, waste and privacy modules;
5. optional client internal-policy controls.

Where two sources conflict, the mode displays both, marks
`ConflictLegalReview`, and blocks an authoritative conclusion for that item.

## 3. Compliance Taxonomy

Every requirement belongs to one primary domain and may have secondary tags.

1. Organisation, licence and legal entity.
2. Service category, district and operating conditions.
3. Bases, stations and operational areas.
4. Vehicles, licence/token, roadworthiness and markings.
5. Drivers, PrDP and vehicle-operation authority.
6. Clinical staff, HPCSA registration, qualification and scope.
7. Continuing professional development and competency.
8. Essential equipment, service, calibration and allocation.
9. Medicines, schedules, formulary, permits and custody.
10. Stock, consumables, oxygen and shortage controls.
11. Storage, environmental monitoring and access control.
12. Infection prevention and control.
13. Health-care waste and environmental controls.
14. Occupational health and safety.
15. Clinical governance, CPGs, SOPs and quality improvement.
16. Patient-care records, confidentiality and retention.
17. Complaints, adverse events and incident review.
18. Communications, dispatch, response records and data submission.
19. Insurance, indemnity and contractual evidence where sourced.
20. Readiness checks, audit evidence and corrective-action closure.

Requirement classification is independent of domain:

- `BindingLaw`
- `BindingRegulation`
- `LicenceCondition`
- `OfficialInspectionItem`
- `RegulatoryStandard`
- `OfficialDirective`
- `OfficialGuidance`
- `InternalPolicy`

The UI must display this classification beside every requirement.

## 4. Side-By-Side Comparison Contract

Each evaluated requirement must return the following stable contract:

| Field | Meaning |
| --- | --- |
| Requirement ID/version | Stable rule identity and immutable version |
| Plain-English requirement | Practical wording that does not replace the source text |
| Authoritative source | Authority, title, exact clause/page/form and link |
| Source classification | Law, regulation, condition, inspection item, standard, guidance or policy |
| Jurisdiction/service scope | Country, province, district, service type, licence level and affected objects |
| Required evidence | Defined evidence types and verification conditions |
| Existing evidence | Tenant record/file/checklist/report/audit item with timestamp and owner |
| Evidence confidence | Verified, system-derived, uploaded-unverified, self-attested or unavailable |
| Status | Deterministic result from the allowed status set |
| Gap | Exact missing, expired, inconsistent, conflicting or unverifiable condition |
| Priority and consequence | Deterministic priority plus sourced consequence or clearly labelled operational risk |
| Corrective action | Plain-English action; distinguishes statutory deadline from management target |
| Owner | Assigned role/person with tenant and area scope |
| Due date | Source-defined deadline or explicitly labelled management target |
| Closure evidence | Evidence type and verification required to close the gap |
| AcuityOps link | Deep link to the relevant profile, asset, register, document, report or task |
| Last checked | Evaluation timestamp, evaluator version and pack version |
| Review state | Automated result, manager-reviewed, independently verified or legally blocked |

No field may imply evidence that does not exist. `NotApplicable` requires a
recorded applicability rationale and reviewer.

## 5. Deterministic Status And Priority Model

### Statuses

- `Compliant`: every required condition and evidence check passes.
- `PartiallyCompliant`: some evidence passes but a non-blocking part is missing.
- `Missing`: required record/evidence is absent.
- `Expired`: a required dated item has expired.
- `Invalid`: evidence exists but fails the defined verification rule.
- `NotApplicable`: explicit applicability test and rationale pass.
- `UnableToVerify`: source, evidence or verification data is insufficient.
- `ConflictLegalReview`: applicable sources conflict or legal interpretation is
  required.

### Priority

Priority is evaluated separately from status:

| Priority | Meaning | Mandatory behavior |
| --- | --- | --- |
| P0 Licence/Safety Blocker | A source-backed condition can prevent lawful operation or creates immediate critical safety risk | Always first; mode cannot show `Inspection ready`; acknowledgement cannot close it |
| P1 Critical | Serious regulatory, clinical or operational safety failure requiring urgent correction | Above all percentage-based work; senior review required |
| P2 Major | Material inspection/evidence gap with significant consequence | Assigned owner and due date required |
| P3 Routine | Corrective maintenance, documentation or control gap | Planned action required |
| P4 Advisory | Guidance or improvement not presented as a legal failure | May be deferred with rationale |

### Blocker Rules

Only a source-approved rule may designate a legal/licensing blocker. Examples
that require exact source support include:

- missing, suspended or expired service licence;
- unlicensed base or vehicle token where required;
- unregistered or out-of-scope practitioner;
- missing source-defined essential equipment;
- expired, prohibited, unauthorized or improperly controlled medicine;
- critical roadworthiness or patient-safety failure.

The overview reports blocker counts, critical counts, evidence coverage and
domain status. If a percentage is shown, it must be labelled `Evidence coverage`
or `Requirements passing`, never as regulator approval. P0/P1 items remain
visible regardless of percentage.

## 6. Existing AcuityOps Capability Mapping

| Compliance domain | Existing source-of-truth capability | Current use in mode |
| --- | --- | --- |
| Tenant/company | Company identity, contact, country/region, workspace, setup and subscription fields | Jurisdiction and operator profile input; not sufficient for licence proof |
| Operational structure | Operational areas, hierarchy and storage locations | Base/area/storage applicability and evidence links |
| Staff | Qualification/scope label, practitioner number, annual licence expiry, CPD status/expiry, area, status and files | Practitioner evidence candidate; requires official-status and scope verification extensions |
| Vehicles | Registration, callsign, function, subtype, VIN/chassis, licence expiry, service dates, status, area/location and schematic | Vehicle inventory and evidence candidate; requires EMS licence/token and road compliance extensions |
| Equipment | Type/model/serial, service/battery dates, status, location and movement | Item identity, allocation and service evidence candidate; requires calibration/essential-set controls |
| Stock | Item/category/batch, quantities, minimum level, expiry, location and readiness criticality | Consumable availability and expiry evidence candidate |
| Medication | Medicine/code/type/schedule, batch, quantity, expiry, location and status | Inventory candidate; requires legal authority, formulary, custody and transaction controls |
| Checklists | Versioned published templates, immutable submissions, issues, notes, schematic marks and PDF reports | Inspection/readiness evidence links |
| Operations | Tasks, issues, feedback, movements and audit logs | Corrective-action assignment, traceability and closure events |
| Expiry | Expiry pressure and date-status handling | Evidence alerts; compliance due dates need explicit source-defined semantics |
| Imports | Deterministic Pro register/checklist import with mapping, validation and governed removal | Client data ingestion only; imports never create compliance conclusions |

## 7. Missing Product Capabilities

Block 6 requires the following gaps to be implemented or explicitly deferred:

### Organisation And Licensing

- service licence category, number, authority, issue/expiry dates, district,
  conditions, status, renewal application and retained certificate;
- licensed bases/stations and inspection/renewal history;
- licence suspension, defect notice and corrective-action records;
- regulator correspondence and evidence of fee/application submission.

### Staff And Drivers

- official HPCSA registration-status/APC evidence and verification timestamp;
- exact registration category and scope/CPG version;
- CPD component/points evidence instead of a generic status only;
- competency/skills records where a source requires them;
- driver identity, licence code, PrDP category/expiry and medical-fitness evidence.

### Vehicles And Equipment

- per-vehicle EMS licence/token, issue/expiry/display evidence;
- roadworthy, operator-card, insurance and emergency-light/siren evidence where
  applicable;
- source-defined required equipment sets by service/licence class;
- calibration, certification, maintenance provider and service evidence;
- clean/decontamination and vehicle/equipment availability evidence.

### Medicines, Stock And Storage

- approved formulary by practitioner/service scope;
- procurement/possession/permit/licence authority and responsible custodian;
- receipt, issue, administration, transfer, wastage, discrepancy and destruction
  transaction records;
- temperature/environment monitoring and excursion handling;
- controlled access, key custody and stock-count reconciliation;
- oxygen cylinder identity, test/service and quantity controls.

### Base, IPC, Waste And OHS

- structured base-facility inspections: rest/ablution/clean/dirty areas, wash bay,
  utilities, oxygen, fire, security and sanitation where sourced;
- IPC plan, risk assessment, training, PPE, decontamination and exposure records;
- health-care-waste plan, responsible officer/committee, contractor, manifests,
  storage, monthly records and source-defined retention;
- OHS risk, incident, training, surveillance and corrective-action evidence.

### Governance And Records

- generalized tenant evidence/document register with category, owner, scope,
  validity dates, verifier, source tags, version and immutable content hash;
- versioned SOP/CPG register and acknowledgement evidence;
- complaint, adverse-event, clinical-review and quality-improvement workflows;
- PCR completeness, response/data-submission and retention evidence;
- compliance profile/session/gap/action/snapshot/export models and services.

## 8. Canonical Data Model And Migration Strategy

The following canonical names supersede the conceptual `Audit*` names in Phase
12B. They preserve the Phase 12C generalized multi-country intent.

### Product-Owned Global Models

- `Jurisdiction`
- `Regulator`
- `RegulatorySource`
- `RegulatorySourceVersion`
- `RegulatoryClause`
- `ComplianceRequirementPack`
- `ComplianceRequirementPackVersion`
- `ComplianceRequirement`
- `ComplianceApplicabilityRule`
- `ComplianceEvidenceDefinition`
- `ComplianceRuleReview`

Product-owned tables have no tenant assignment data. Only authorized product
compliance administrators can create or publish pack versions.

### Tenant-Owned Models

- `ComplianceProfile`: selected jurisdictions, service/licence categories and
  operating scope.
- `ComplianceSession`: tenant, pack versions, selected scope, dates and state.
- `ComplianceEvidenceRecord`: generalized uploaded/system evidence metadata.
- `ComplianceEvidenceLink`: requirement/session to tenant evidence relation.
- `ComplianceEvaluation`: immutable deterministic result and evaluator version.
- `ComplianceGap`: requirement-specific gap, consequence and status.
- `ComplianceAction`: owner, due date, action, status and closure requirements.
- `ComplianceAttestation`: manager/reviewer decision and rationale.
- `ComplianceSnapshot`: immutable session, rule, evidence and result manifest.
- `ComplianceExport`: generated pack metadata, hash and access record.

Every tenant-owned row must carry `CompanyId`. All foreign-key and uniqueness
constraints must include or validate tenant ownership. Product rules may be
referenced globally, while evidence and assignments remain tenant-isolated.

### Additive Migration Sequence

1. Global source, version, clause and requirement-pack tables.
2. Tenant compliance profile and generalized evidence metadata.
3. Sessions, evaluations, gaps and actions.
4. Snapshot, attestation and export records.
5. Domain-specific licensing/vehicle/staff/medicine extensions only after exact
   source requirements are approved.

No migration may backfill compliance, infer missing evidence, rewrite history or
create active rules. Legacy data remains available through explicit adapters and
is `UnableToVerify` where required data is absent.

## 9. Architecture Decision Record

### Decision

Use a versioned deterministic rules-and-evidence architecture with product-owned
requirement packs and tenant-owned compliance state.

### Reasons

- Legal sources change independently of client data.
- Provinces and service categories have different overlays.
- Historical inspection preparation must remain reproducible.
- A rule result must be explainable from source, inputs and evaluator version.
- Future countries require new jurisdiction packs, not new tenant schemas.

### Rejected Alternatives

- Hardcoded `if` statements in pages: unversioned and not auditable.
- One South Africa checklist: hides provincial/service applicability.
- AI-generated compliance status: nondeterministic and unsafe.
- Copying requirement rows into every tenant: creates drift and silent updates.
- One weighted readiness score: can hide blockers and misrepresent regulator
  approval.

### Rule-Pack Lifecycle

`Draft -> Acquired -> ClauseExtracted -> SourceVerified -> LegalReviewPending -> Approved -> Active -> Superseded`

`Withdrawn` and `Blocked` are terminal/non-active states. Publishing a new pack
version does not modify old sessions. Tenants are notified of new versions and
explicitly start reevaluation.

## 10. UX And Navigation Contract

### Entry And Routes

- Home/Operational Reports: `DOH Annual Inspection Mode` for eligible tiers.
- `/Compliance`
- `/Compliance/SouthAfrica/AnnualInspection`
- `/Compliance/Sessions/{id}`
- `/Compliance/Requirements/{id}`
- `/Compliance/Actions`
- `/Compliance/Exports`

### Entry Gate

Before a session starts, a senior manager confirms:

- legal entity and service;
- province(s), district(s) if applicable and licensed base(s);
- private-service and service-category applicability;
- licence class/clinical capability;
- vehicles/areas included;
- pack version and source limitations;
- acknowledgement that the mode is preparation support, not regulator approval.

If the province pack is incomplete, the app permits an internal evidence review
only and displays `Authoritative provincial inspection conclusion unavailable`.

### Main Workspace

The first screen is an operational workspace, not a marketing page:

1. Current scope and pack/version bar.
2. P0 blockers and P1 critical issues.
3. Prioritized action list with owner, due date and status.
4. Domain coverage and evidence freshness.
5. Requirements needing legal/source review.
6. Recheck and final-review controls.

### Requirement Detail

Two columns display:

- `Required`: plain English, source classification, exact source/clause,
  applicability and required evidence.
- `Your current evidence`: linked records/files, validation, status, gap and
  direct action.

The detail also supports evidence upload/linking, task assignment, owner/due
date, notes, review/attestation, closure evidence and evaluation history.

### Action Workflow

`Open -> Assigned -> InProgress -> EvidenceSubmitted -> ReviewRequired -> Closed`

Only a passing reevaluation or authorized reviewer attestation can close a
source-defined compliance gap. A task being marked complete does not by itself
make the requirement compliant.

## 11. Evidence, Audit And Immutability Contract

Evidence may be:

- system-derived register data;
- immutable submitted checklist/report evidence;
- tenant-uploaded document/photo/certificate;
- regulator-issued licence/token/correspondence;
- externally verified status with timestamp;
- manager attestation, clearly labelled and never equivalent to independent
  evidence.

Every evidence record needs tenant, category, object scope, issuer, issue/expiry
dates, version, content hash, storage reference, uploader, upload time, verifier,
verification time, sensitivity, retention class and access history.

Audit events include profile changes, session start, pack selection, evidence
link/unlink, evaluation, status override attempt, reviewer decision, action
assignment/update, recheck, finalization, export and access/download.

Finalization writes an immutable manifest of:

- tenant/scope;
- requirement pack and source versions;
- evaluator version;
- requirement inputs and results;
- evidence identities, hashes and versions;
- open gaps/actions;
- attestations and unresolved legal/source limitations.

## 12. Versioned Evidence Pack And PDF Contract

The downloadable inspection-preparation pack must be generated from the
immutable compliance snapshot, not live mutable tables.

Minimum PDF/pack content:

1. Tenant identity and confirmed inspection scope.
2. Pack version, generation date and legal limitation statement.
3. Executive blocker/critical-action summary.
4. Domain-by-domain status and evidence coverage.
5. Full side-by-side requirement matrix.
6. Prioritized corrective-action plan.
7. Evidence index with evidence IDs, issuers, dates, versions and hashes.
8. Unresolved, unable-to-verify and legal-conflict register.
9. Source register and exact citations.
10. Manager/reviewer attestations.
11. Snapshot/export hash and audit identifier.

Sensitive evidence files are not embedded by default. The export uses a
role-authorized evidence index, optional redacted attachments and an export
manifest. Report detail and PDF must agree on every status, source, evidence and
action.

## 13. Roles And Permissions

| Role | Allowed behavior |
| --- | --- |
| Company owner / authorized senior manager | Configure compliance profile, start sessions, assign scope, review all tenant evidence, assign actions, approve attestations, finalize and export |
| Operational manager | View only assigned area/base/session scope; link evidence, manage assigned actions, request recheck and review staff/assets in scope; cannot publish global rule packs or finalize company-wide status unless explicitly granted |
| Staff | View and complete personally assigned evidence/actions; upload own permitted documents; cannot see company-wide compliance or alter evaluation rules |
| Product compliance administrator | Manage product-owned source/pack drafts and versions; cannot access tenant evidence without separately authorized support access |
| External reviewer (future) | Time-bound, read-only or attestation-limited access to an explicitly shared snapshot; no general tenant access |

Server-side authorization is mandatory for every handler, object ID and export.
Navigation visibility is not enforcement.

## 14. Tier And Commercial Contract

| Capability | Base | Pro | Premium | Enterprise |
| --- | --- | --- | --- | --- |
| Existing register expiry/readiness alerts | Included | Included | Included | Included |
| SA DOH Annual Inspection Mode | Locked | Included | Included | Included |
| Deterministic source-backed comparison | No | Included | Included | Included |
| Action plan and evidence linking | No | Included | Included | Included |
| Versioned inspection-preparation export | No | Included | Included | Included |
| AI source/evidence suggestions | No | No | Human-reviewed assistance later | Configurable |
| Predictive compliance forecasting | No | No | Included after Block 7 validation | Configurable |
| Custom country/jurisdiction packs | No | No | Standard supported packs | Contracted custom packs |
| External reviewer/custom workflow | No | No | Limited where offered | Configurable |

Premium AI may explain, summarize and forecast only after deterministic results
exist. It cannot activate legal rules or change compliant/non-compliant status.

## 15. Automated Test Contract

### Source And Pack Tests

- only `Active` approved pack versions evaluate tenants;
- source/classification/clause metadata is complete;
- superseded/withdrawn sources do not enter new sessions;
- conflicting sources produce `ConflictLegalReview`;
- incomplete province packs cannot claim inspection readiness.

### Applicability And Evaluation Tests

- country/province/service/licence/object rules include and exclude correctly;
- `NotApplicable` requires rationale;
- missing/expired/invalid/unverifiable statuses are deterministic;
- P0/P1 items cannot be hidden by a score;
- statutory deadlines and management targets remain distinguishable;
- new rule versions do not alter old snapshots.

### Tenant, Role And Evidence Tests

- overlapping object identifiers cannot cross tenants;
- ops managers see only assigned scope;
- staff see only assigned/self evidence;
- product compliance admins cannot access tenant evidence by default;
- evidence links reject cross-tenant objects;
- export authorization and audit pass;
- later edits to source records do not change finalized snapshots.

### Report/PDF Tests

- report detail and PDF parity;
- exact source citation and pack version;
- blocker/action ordering;
- evidence index/hash correctness;
- redaction and sensitive attachment rules;
- desktop, mobile and print layout.

## 16. Controlled Staging Acceptance Plan

Use one temporary test tenant created through real client-facing flows. No seed
or direct SQL product-data creation.

1. Back up staging before additive migrations or controlled writes.
2. Apply migrations through the existing GitHub Linux deployment path.
3. Create a compliance profile for one supported province/service class.
4. Populate the minimum evidence through existing UI and new evidence UI.
5. Prove compliant, missing, expired, unable-to-verify and not-applicable cases.
6. Prove one P0/P1 item dominates the overview/action list.
7. Link a corrective task, submit closure evidence and recheck.
8. Finalize and compare snapshot, report detail and PDF.
9. Test senior, scoped ops and staff boundaries.
10. Test cross-tenant identifiers and export isolation.
11. Prove pack supersession creates a new reevaluation without changing the old
    session.
12. Remove temporary tenant data through supported workflows or restore backup.

## 17. Mandatory Legal And Regulatory Review Gate

Before any pack is labelled authoritative or `Active`, qualified South African
legal/regulatory review must answer and record:

- current force, amendments and interaction of each national/provincial source;
- whether a source is law, regulation, licence condition, inspection item,
  standard or guidance;
- exact private-ground-ambulance and service-category applicability;
- province/district/licence-level applicability;
- correct interpretation of sanctions, blockers and deadlines;
- medicines permit/scope/custody/destruction rules;
- HPCSA category, scope, APC and CPD rules;
- vehicle, driver and PrDP rules;
- record retention, confidentiality and POPIA wording;
- health-care-waste, OHS and IPC applicability;
- liability disclaimer and acceptable public/product claims;
- process for source updates, legal disputes and regulator correction requests.

An experienced private-ambulance operational reviewer must separately verify
that evidence requests and corrective actions are practical. Legal and
operational review decisions are versioned and auditable.

## 18. Safe Implementation Sequence

### B6.0 Source Acquisition And Legal-Pack Gate

Scope:

- acquire and hash final official sources;
- extract exact clauses/forms/annexures;
- obtain missing provincial instruments directly from authorities;
- complete legal and operational review for the first supported pack.

No product code. No active rules until gate passes.

Estimated Codex credits: 800-1,200, excluding external legal/SME fees.

### B6.1 Source Registry And Pack Governance Foundation

Status: Accepted and locked on 2026-07-19 by commit `0a5df3b`.

Scope:

- additive product-owned source, version, clause, pack, requirement,
  applicability, evidence-definition and review models;
- pack lifecycle and source-admin service boundaries;
- no tenant comparison UI and no active legal rules.

Reasoning: High. Migration risk: additive, medium. Estimated credits:
1,400-2,200.

Accepted evidence:

- product-owned jurisdiction, regulator, source/version, clause, requirement
  pack/version, requirement, applicability, evidence-definition, review,
  provenance and governance-event models contain no tenant ownership;
- the governed lifecycle is default-deny and no tenant permission or mutation
  route can administer product regulatory content;
- the read-only registry returns only independently approved active national
  and provincial packs and reports incomplete provinces separately;
- all nine South African provinces, optional nested district/municipality
  levels, multi-province composition and future country roots are represented
  without inserting jurisdiction or requirement rows;
- one additive migration passed disposable SQLite apply/rollback verification
  and SQL Server script generation without being applied to an active database;
- provider, lifecycle, provenance, active-version, immutability, conflict,
  default-deny and tenant-isolation regression tests passed.

### B6.2 Tenant Compliance Profile And Evidence Contract

Scope:

- tenant compliance profile;
- generalized evidence record/link metadata;
- scope confirmation and role enforcement;
- adapters to existing tenant records without claiming compliance.

Reasoning: High. Migration risk: additive, medium. Estimated credits:
1,500-2,300.

### B6.3 Deterministic Evaluation And Domain Adapters

Scope:

- applicability engine;
- status and priority engine;
- adapters for company, staff, vehicles, equipment, stock, medication, bases,
  storage, checklists, issues/tasks and dates;
- explicit unsupported-evidence results.

Reasoning: High. Estimated credits: 1,900-2,900.

### B6.4 Inspection Workspace And Corrective Actions

Scope:

- entry gate, overview, blockers, prioritized list, domains, side-by-side detail,
  evidence linking, action ownership, due dates, recheck and role scope.

Reasoning: Medium, with High review for authorization. Estimated credits:
1,600-2,300.

### B6.5 Immutable Snapshot And Evidence Pack

Scope:

- finalization, immutable manifest, report detail and professional versioned PDF
  using the Block 4 evidence pattern.

Reasoning: High. Estimated credits: 1,200-1,900.

### B6.6 First Pack Activation

Scope:

- import only legally approved requirements;
- activate national baseline plus one legally approved province overlay;
- source/version notices and reevaluation behavior.

Western Cape is the current best-documented candidate, but may be selected only
after legal confirmation of its current relationship to national rules.

Reasoning: High; XHigh only for unresolved source conflicts. Estimated credits:
1,100-1,800.

### B6.7 Integrated Tests, Staging And Legal Closure

Scope:

- automated matrix, controlled staging acceptance, report/PDF parity, tenant and
  role isolation, source/legal sign-off and tracker evidence.

Reasoning: High. Estimated credits: 700-1,200.

Estimated Block 6 Codex total: 10,200-15,800 credits. This is a planning range,
not measured billing. External legal, regulatory-source acquisition and private
EMS subject-matter review costs are excluded.

## Stop Conditions

Stop before implementation or activation if:

- an official source cannot be obtained or verified;
- national/provincial sources conflict and legal review has not resolved the
  operational presentation;
- service/licence/province applicability is uncertain;
- a rule would infer medicine, clinical, vehicle or licensing authority;
- existing tenant evidence cannot be mapped without misrepresentation;
- a migration would be destructive or rewrite historical evidence;
- tenant/role isolation fails;
- report detail and PDF differ;
- implementation would reopen Blocks 1-5 without a reproducible regression;
- work expands into AI, billing, production infrastructure, website or unrelated
  roadmap blocks.

## Block 6 Acceptance Criteria

Block 6 may close only when:

1. The active source pack has exact, dated, retained primary sources and legal
   approval.
2. Applicability is explicit by province, service/licence class and object.
3. The tenant sees every requirement side by side with real evidence, gap,
   priority, action, owner, due date and source/version.
4. P0/P1 items cannot be masked by percentages.
5. All domains in the taxonomy are either evaluated or visibly marked
   unsupported/source-incomplete.
6. Requirement packs, evidence, evaluations and completed sessions are
   versioned and auditable.
7. The corrective-action workflow and recheck behavior pass.
8. Report detail and downloadable evidence pack agree.
9. Staff, ops, senior and cross-tenant scope tests pass.
10. Azure staging acceptance and legal/operational review pass.

## Current Controlled Boundary

B6.1 is complete. No regulatory requirement, jurisdiction, pack, tenant
compliance record or conclusion was inserted or activated. B6.0 remains the
next gate because the research baseline is not a legally approved source pack.
B6.2 may not begin until its dependency and stop conditions are reviewed through
the single progress tracker.

Recommended next instruction:

> Use High reasoning. Propose the smallest safe B6.0 Source Acquisition and
> Legal-Pack Gate batch from the Block 6 blueprint and authoritative source
> register. Do not edit product source. Define only the exact primary-source
> acquisition, retained-artifact hashing, clause extraction, province-specific
> completeness review, legal/operational review queue and approval evidence
> required before any pack can activate. Do not activate requirements, create
> tenant compliance data, apply migrations, deploy or enter B6.2.

## Reasoning Guidance

- Use `High` for source architecture, migrations, deterministic evaluation,
  tenant isolation, access control and evidence integrity.
- Use `Medium` for bounded UI implementation and routine verification after
  contracts are accepted.
- Use `XHigh` only for unresolved legal/source conflicts, irreversible security
  or architecture decisions, and final compliance-pack activation review.

The requested Sol/Extra High planning pass is appropriate for this blueprint
because it crosses legal-source provenance, rule versioning, evidence integrity
and multi-jurisdiction architecture. It would be wasteful for routine later UI
work.
