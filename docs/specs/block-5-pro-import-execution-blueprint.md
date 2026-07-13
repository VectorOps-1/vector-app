# Block 5: Pro Import, Column Matching, And Conversion

Status: Design complete; implementation not started

Authority:

- Progress and execution status: `commercial-launch-progress-tracker.md`
- Product requirements: `acuityops-recovery-roadmap.md` Phase R4
- Commercial requirements: `commercial-completion-roadmap.md` Phase C3

## Objective

Deliver a deterministic Pro workflow that converts client-owned Excel and CSV
files into AcuityOps register records and checklist templates through explicit
column matching, validation, correction, preview, deduplication, approval,
audit, and rollback controls.

Block 5 does not use AI. Premium AI may later suggest mappings against this
accepted contract, but it may not bypass validation, preview, user approval,
tenant isolation, audit, or rollback.

## Non-Negotiable Outcomes

1. A Pro-authorized user can import vehicles, staff, equipment, stock,
   medication, operational areas, and storage locations.
2. A Pro-authorized user can convert a checklist spreadsheet into the existing
   AcuityOps section, item, subitem, and column model.
3. The system never creates operational records merely because a file was
   uploaded.
4. Every source column is explicitly mapped or ignored before commit.
5. Every required field, reference, duplicate, conversion, and row error is
   visible before commit.
6. Imported register records use the same models, pages, permissions, reports,
   and tenant boundaries as manually created records.
7. Imported checklist templates enter Checklist Register as drafts and cannot
   become live without the existing explicit publish workflow.
8. Every upload, mapping confirmation, validation result, correction, commit,
   rollback, and checklist publication is attributable and audit logged.
9. Base users see an accurate Pro upgrade state. Premium AI remains a separate
   later capability.

## Current Product Facts

The current app already:

- stores tenant-scoped register and checklist source files through
  `SetupUploadService` and `AssetFile`;
- accepts `.xlsx`, `.xls`, and `.csv` at the upload UI;
- has placeholder preview routes that do not parse or import the file;
- stores register data in the existing tenant-owned vehicle, staff, equipment,
  stock, medication, operational-area, and storage-location models;
- stores checklist structure in `ChecklistTemplate`, `ChecklistSection`,
  `ChecklistItem`, and `ChecklistColumnDefinition`;
- has a register-driven checklist publish flow that must remain the only path
  from draft template to live checklist.

Block 5 extends these paths. It must not create a second register, checklist
store, publish mechanism, or tenant identity.

## Deterministic Import Boundary

The importer recognizes structure through explicit, testable rules:

- file format and workbook metadata;
- selected worksheet and selected header row;
- normalized header names and a versioned alias dictionary;
- explicit user-selected target fields;
- explicit value conversion rules;
- exact or rule-based duplicate keys;
- explicit checklist layout mode and hierarchy controls.

The importer does not infer operational meaning from prose. It does not use an
LLM, embeddings, OCR, semantic similarity, or hidden fallback template.

Deterministic suggestions are allowed only when they are explainable. For
example, `Reg No`, `Registration`, and `Vehicle Registration` may all be listed
as versioned aliases for `vehicle.registration_number`. The UI must label the
result as a suggestion until the user confirms it.

## File Support And Safety Limits

Required support:

- `.xlsx`: primary Excel format;
- `.csv`: UTF-8 primary, with controlled encoding and delimiter selection;
- `.xls`: compatibility format through an explicit binary Excel reader. If the
  selected library cannot parse a file safely, the UI must instruct the user to
  save it as `.xlsx`; it must not pretend the upload was imported.

The implementation decision should use maintained .NET libraries with these
roles:

- workbook structure and merged-cell inspection for `.xlsx`;
- legacy workbook reading for `.xls` only;
- standards-based CSV parsing with quoted fields, embedded delimiters, and
  configurable delimiter/encoding.

The first implementation batch must record the selected libraries and licence
compatibility before adding packages.

Mandatory limits, configurable per environment:

- maximum file size: 20 MB;
- maximum worksheets inspected: 50;
- maximum columns per selected sheet: 250;
- maximum rows per import batch: 50,000;
- maximum non-empty cells processed: 1,000,000;
- no macro-enabled workbook support;
- no external-link resolution;
- no formula execution. Cached displayed values may be read and must be marked
  as originating from a formula cell in preview.

Validation must check extension, content signature, size, and parser result.
The original file remains tenant-scoped evidence. Production malware scanning
is a production-storage requirement and cannot be silently claimed during
Block 5.

## Import Lifecycle

Every import follows one state machine:

1. `Uploaded`: source file stored; no operational data changed.
2. `SourceSelected`: target entity, worksheet, header row, and layout selected.
3. `Mapping`: source columns are mapped or ignored.
4. `Validated`: conversions, references, required fields, and duplicate checks
   have run against the current tenant state.
5. `CorrectionRequired`: one or more selected rows contain blocking errors.
6. `ReadyToCommit`: all selected rows are valid and every warning/duplicate has
   an explicit decision.
7. `Committed`: one controlled transaction created or updated records.
8. `PartiallyRolledBack`: only safe changes were reversed; blocked changes are
   listed.
9. `RolledBack`: every eligible change was reversed.
10. `Failed`: parsing or commit failed without leaving an untracked partial
    write.
11. `Cancelled`: user cancelled before commit; source and audit history remain
    subject to retention rules.

No state transition may be inferred from visiting a page. POST handlers and
services enforce transitions server-side.

## Additive Data Contract

Block 5 requires an additive, tenant-scoped import ledger. Exact names may
follow repository conventions, but the responsibilities are fixed.

### Import Batch

Stores:

- company/tenant ID;
- source `AssetFile` ID;
- target type: vehicle, staff, equipment, stock, medication, operational area,
  storage location, or checklist;
- file hash, original name, selected worksheet, header row, layout mode, and
  parser contract version;
- status and row/count summaries;
- creator, validator, committer, rollback actor, and UTC timestamps;
- optimistic concurrency token;
- failure code and safe diagnostic summary.

### Import Column Mapping

Stores:

- import batch ID;
- source column index and original heading;
- normalized source heading;
- target field key or explicit ignore state;
- conversion rule and optional fixed/default value;
- suggestion reason and whether the user confirmed it;
- display order.

### Import Row Result

Stores:

- import batch ID and source row number;
- tenant-scoped normalized row payload required to resume correction;
- validation status;
- field errors and warnings;
- duplicate candidates and explicit row decision;
- include/exclude decision;
- corrected values separately from original source values.

Sensitive values must not be copied into general application logs. Retention
and export of source and staging rows follow tenant data-lifecycle rules.

### Import Entity Change

Stores:

- import batch and source row;
- target entity type and ID;
- action: created, updated, skipped, or rejected;
- field-level before and after values needed for an authorized rollback;
- entity state/concurrency marker at commit;
- rollback eligibility, result, actor, and timestamp.

This ledger is the traceability path. Imported domain entities do not require a
new parallel data store.

## Canonical Field Registry

The mapping UI must be generated from a versioned server-side field registry,
not hand-written separately on every page. Each target field definition must
contain:

- stable field key;
- user-facing label and help text;
- target model property or controlled resolver;
- data type and accepted formats;
- required/optional rule;
- maximum length and normalizer;
- allowed values or tenant-owned option source;
- deterministic aliases;
- duplicate-key role;
- create/update permission;
- example value;
- whether blank input clears, preserves, or is invalid during update.

Required field groups are based on the current domain models.

### Vehicles

Required: registration number.

Available mappings: callsign, function, subtype, qualification level, VIN,
chassis number, licence number, licence-disc expiry, last service, next service,
status, operational area, location detail, and notes.

Import does not silently assign product schematics. Schematic assignment remains
the explicit library/register workflow.

### Staff

Required: full name plus the tenant-approved identity field required by the
current staff model, normally email or staff identifier.

Available mappings: email, staff ID, national ID, cell number, clinical
qualification/scope, practitioner number, annual licence expiry, CPD status,
CPD expiry, assigned operational area, and status.

Importing a staff profile must not grant login or manager access. If the current
staff/profile model cannot represent a non-login profile safely, implementation
must stop at the Block 5 schema gate and resolve that gap explicitly rather than
granting access implicitly.

### Equipment

Required: equipment name.

Available mappings: equipment type, model, serial/asset ID, next service date,
battery required, status, operational area, location detail, and notes.

### Stock

Required: item name and quantity.

Available mappings: item type, category, batch, quantity, minimum quantity,
unit, expiry, readiness-critical flag, location, operational area, status, and
notes.

### Medication

Required: medication name.

Available mappings: medication code, medication type, schedule, batch, storage
location, operational area, status, quantity, expiry, and notes.

### Operational Areas

Required: name and area type.

Available mappings: parent area, address, status, and notes. Parent references
must resolve within the same tenant and must not create cycles.

### Storage Locations

Required: name and operational area.

Available mappings: storage type, status, and notes.

## Source Recognition And Header Selection

For each worksheet, the parser produces a read-only source profile:

- sheet name, dimensions, merged ranges, hidden state, and non-empty count;
- candidate header rows;
- detected column headings;
- representative sample rows;
- formula-cell and conversion warnings.

Candidate header scoring may use only deterministic factors: non-empty cells,
text ratio, unique headings, known aliases, and populated rows beneath the
candidate. The user always sees and can override the selected worksheet and
header row.

Duplicate or blank headings receive stable source labels such as `Column C` and
must be mapped or ignored explicitly.

## Column Matching UI

The guided UI uses a fixed sequence:

1. Choose import target.
2. Upload or select source file.
3. Choose worksheet/table and header row.
4. Review detected columns and sample values.
5. Map each source column to an AcuityOps field or Ignore.
6. Configure data conversions where required.
7. Validate rows and references.
8. Correct errors and decide duplicates.
9. Review final create/update/skip counts and field-level changes.
10. Confirm import in an in-app confirmation modal.
11. Show committed results, audit reference, and available rollback action.

Each mapping row shows source heading, sample values, target-field dropdown,
required state, conversion, result, and error. `N/A` appears first where it is a
valid selection; Ignore is a distinct action and must not be confused with a
data value.

The system may save a tenant-owned reusable mapping profile only after an
explicit `Save mapping` action. Reuse requires matching target type and source
heading signature, and the user must reconfirm it before commit.

## Conversion Rules

Supported deterministic conversions include:

- trim/collapse whitespace;
- preserve original text while applying case-insensitive matching;
- date parsing from Excel serial dates and user-selected date formats;
- integer and decimal parsing with selected locale;
- boolean mapping using a user-visible value table;
- status and category mapping to tenant/configured options;
- quantity/unit normalization without silently changing quantities;
- operational-area and storage-location lookup by exact normalized name;
- vehicle function/subtype and staff qualification lookup against tenant setup;
- split or combine fields only through an explicit user-configured rule.

Failed conversions are errors, not blank values. Default values must be visible
in the mapping and final preview.

## Reference Resolution

References are tenant-scoped and explicit. A referenced operational area,
storage location, function, subtype, or qualification resolves by stable ID when
the source supplies one, otherwise by exact normalized configured name.

For an unresolved reference the user must choose one of:

- map to an existing tenant value;
- leave blank where the domain field is optional;
- exclude the row;
- stop and import the required reference data first.

The importer never creates reference data silently from spelling variants.

## Duplicate Detection And Update Rules

Duplicate detection is deterministic and tenant-scoped.

- Vehicle: normalized registration number; callsign collision is a separate
  blocking warning.
- Staff: exact normalized email when present, then staff ID, practitioner
  number, or national ID as individually identified candidates. Conflicting
  identifiers block automatic matching.
- Equipment: serial/asset ID when present. Name/model alone may warn but may not
  auto-merge.
- Stock: normalized item name, batch, and resolved location/area.
- Medication: medication code and batch when available; otherwise normalized
  name, batch, and resolved location/area.
- Operational area: normalized name, area type, and parent.
- Storage location: normalized name and resolved operational area.

For every candidate the user chooses `Skip`, `Update existing`, or, only where
domain uniqueness permits it, `Create separate`. Update preview shows every
changed field. Blank source values preserve existing data by default; clearing
an existing field requires an explicit clear rule.

Duplicate decisions are revalidated immediately before commit to prevent a
stale preview from creating conflicts.

## Validation And Correction

Validation runs in layers:

1. File and parser validation.
2. Worksheet/header/mapping validation.
3. Cell type, required value, length, and allowed-value validation.
4. Tenant reference validation.
5. Duplicate and uniqueness validation.
6. Cross-field business validation.
7. Permission, tier, and company setup validation.
8. Pre-commit concurrency revalidation.

The correction table is a compact row list, not repeated cards. It supports
error-only filtering, source-row search, field correction, exclude/include, and
duplicate decision. Original and corrected values remain distinguishable.

The default commit policy is all selected rows valid. A user may explicitly
exclude invalid rows and import the remaining valid rows; the final confirmation
must state exactly how many source rows are excluded. A database exception
rolls back the entire commit transaction.

## Register Commit Contract

One import batch targets one entity type. Commit occurs through target-specific
domain writers that apply the same validation and tenant rules as manual entry.
Page models may not insert imported entities directly.

The commit service must:

- re-resolve the current company and authorized actor;
- recheck Pro entitlement and action permission;
- revalidate the batch and duplicate decisions;
- use one database transaction;
- create the `ImportEntityChange` ledger with the domain writes;
- add concise audit events without sensitive row payloads;
- return deterministic counts and row outcomes;
- be idempotent against repeated POST or workflow retry.

Imported records immediately appear in existing registers because they are
normal domain records. No special imported-record view replaces the register.

## Checklist Spreadsheet Conversion

Checklist conversion produces an editable draft using the existing checklist
builder models. It never creates a live checklist directly.

Supported deterministic layout modes:

1. `Explicit columns`: source contains fields such as Section, Item, Subitem,
   Parent, Input Type, Required, Register Source, and Readiness.
2. `Matrix`: the user selects the item column; subsequent selected source
   columns become checklist columns.
3. `One sheet per section`: each chosen worksheet becomes a section using the
   selected header and item columns.
4. `Sectioned sheet`: section boundaries come from an explicit section column,
   user-marked rows, or qualifying merged heading rows confirmed in preview.

Hierarchy may be derived only from:

- explicit parent/level columns;
- preserved spreadsheet indentation confirmed by the user;
- user corrections in the structure preview.

The importer must not infer parent/subitem relationships from wording alone.

The structure preview is hierarchical and shows:

- sections in order;
- items and subitems;
- columns and response types;
- required/editable/N/A/readiness rules;
- register-source links;
- notes fields;
- unmapped or invalid source rows.

The user can rename, reorder, change hierarchy, map response types, select
dropdown values, and disable readiness impact before saving.

On save:

- create one `ChecklistTemplate` with `SourceType = Imported` and Draft status;
- create its sections, items, subitems, and column definitions in one
  transaction;
- link the source file and import batch;
- show it in Checklist Register;
- require the existing Edit/Crew View/Publish workflow;
- require explicit scope selection and replacement warning at publish time.

No default sections, fixed daily-check fields, fallback template, schematic
assignment, equipment row, or publication scope may be injected.

## Audit And Rollback

Audit actions include:

- file uploaded;
- target/sheet/header selected;
- mapping confirmed or saved;
- validation completed;
- row corrected/excluded;
- duplicate decision confirmed;
- import committed/failed/cancelled;
- rollback requested/completed/partially blocked;
- imported checklist draft created;
- later checklist publication remains in the existing checklist audit flow.

Rollback rules:

- created records may be deleted only when still tenant-owned, unchanged since
  import, and free of downstream operational references;
- updated records may be restored only when their current state still matches
  the imported after-state;
- changed or referenced records are blocked and listed for manual resolution;
- a published or used checklist is retired/versioned through checklist rules,
  never silently deleted;
- rollback is permission-controlled, confirmed in-app, transactional for the
  eligible change set, and audit logged.

Rollback is not a substitute for database backup and does not rewrite
historical operational evidence.

## Permissions And Tier Enforcement

- Base: source-file storage may remain available where included, but guided
  import controls are locked with accurate Pro messaging.
- Pro: deterministic register and checklist import is available to company
  owner/senior users with explicit import permission.
- Operational manager: no commit permission by default. A later explicit saved
  permission may allow import preparation or request submission, but may not be
  inferred from role title.
- Staff: no import access.
- Premium/Enterprise: include the Pro deterministic workflow; later AI may
  suggest mappings but the same approval and commit contract applies.

All GET and POST handlers enforce tenant, role, saved permission, and entitlement
server-side. Navigation visibility alone is not enforcement.

## UI Contract

The import experience uses the shared authenticated app shell and compact
operational UI:

- one progress stepper;
- compact mapping and correction tables;
- sticky horizontal controls on mobile-width tables;
- counts and filters above the table;
- in-app confirmations and temporary action messages;
- no browser-native confirmation dialog;
- no page of repeated cards;
- no import result mixed into the normal register until commit succeeds.

Long jobs must show a resumable batch state rather than holding one HTTP request
open. Background processing architecture may be added later, but Block 5 parsing
must remain bounded by the configured limits and must not require a production
queue claim that does not exist.

## Implementation Slices

Block 5 is decomposed into five coherent batches. Overall commercial progress
remains 35% until all five pass and Block 5 closes.

### B5.1 Import Foundation And Contract

Reasoning: High

Scope:

- record parser-library decision and licences;
- add additive import ledger models/migration;
- add canonical field registry;
- add tenant/tier/permission-aware import lifecycle service;
- replace upload-only handoff with a real import-batch entry route;
- preserve current source-file storage.

Acceptance:

- build and tenant tests pass;
- upload creates only source evidence and an import batch;
- no register/checklist records are created;
- unsupported or unsafe files fail clearly;
- Base and unauthorized roles cannot enter or commit guided import.

Stop conditions:

- staff import would grant login implicitly;
- source files cannot be resolved tenant-safely;
- migration requires destructive change;
- parser licensing is incompatible.

### B5.2 Deterministic Register Mapping And Commit

Reasoning: High for commit/deduplication; Medium for bounded UI

Scope:

- worksheet/header detection and override;
- mapping, conversion, validation, correction, preview;
- duplicate decisions;
- domain writers for areas, storage, vehicles, staff, equipment, stock, and
  medication;
- import result and existing-register visibility.

Implementation order inside the batch:

1. operational areas and storage locations;
2. vehicles and equipment;
3. stock and medication;
4. staff after the non-login-profile safety gate is satisfied.

Acceptance:

- each target passes create, skip, update, invalid-row, duplicate, unresolved
  reference, and cross-tenant tests;
- commit is transactional and idempotent;
- imported records behave like manually created records.

### B5.3 Deterministic Checklist Conversion

Reasoning: High for source-of-truth; Medium for builder UI

Scope:

- supported layout modes;
- section/item/subitem/column mapping;
- structure correction preview;
- draft checklist creation using existing models;
- source/import linkage and Checklist Register visibility.

Acceptance:

- representative explicit, matrix, multi-sheet, and sectioned workbooks produce
  the confirmed draft structure;
- no hidden/default content is injected;
- no checklist is live before explicit publish;
- the imported draft can be edited and crew-previewed through existing pages.

### B5.4 Audit, Rollback, Mapping Reuse, And Pro UX

Reasoning: High

Scope:

- complete audit coverage;
- safe rollback eligibility and conflict handling;
- reusable tenant mapping profiles;
- import history/status;
- Base lock states and Pro permission enforcement;
- mobile/desktop correction-table verification.

Acceptance:

- rollback reverses only eligible unchanged records;
- downstream/historical evidence is never rewritten;
- mapping reuse requires confirmation;
- downgrade leaves imported domain records usable while import tools lock.

### B5.5 Integrated Staging Acceptance And Closure

Reasoning: High

Scope:

- representative clean and messy register files;
- representative checklist files for each supported layout;
- role, tenant, duplicate, validation, correction, commit, rollback, register,
  checklist-register, edit, crew-view, and publish checks;
- tracker evidence and Block 5 closure.

Acceptance:

- the Block 5 acceptance gate in the Commercial Launch Progress Tracker passes;
- no seed/fallback data is created;
- no cross-tenant source, staging row, mapping, record, or checklist is visible;
- explicit publication is the only route to a live imported checklist;
- the accepted deterministic contract is ready for Block 6 compliance data and
  later Block 7 AI suggestions.

## Verification Strategy

Automated tests before browser verification:

- parser tests for dates, numbers, booleans, quoted CSV, blank/duplicate headers,
  merged cells, formula cells, limits, and malformed files;
- mapping registry tests for every required field;
- validation and deterministic duplicate-key tests;
- transaction/idempotency/concurrency tests;
- rollback eligibility/conflict tests;
- tenant isolation and permission tests;
- checklist structure conversion tests;
- tests proving upload alone creates no operational data;
- tests proving import cannot publish a checklist.

Targeted staging verification uses test tenant data only and real UI workflows.
It requires a staging backup before controlled writes and supported cleanup or
rollback afterward. It must not use direct SQL, startup seed data, or fallback
templates.

## Migration And Data Risk

Risk: High but bounded.

The import ledger is additive. No existing register, checklist, report, or
evidence row is rewritten. Migrations must work on SQLite development/test and
Azure SQL staging. Apply only after build and migration-script review, with a
staging backup and rollback procedure.

Block 5 must stop before schema application if the migration is destructive,
the staff profile/access distinction is unsafe, or tenant-owned staging payloads
cannot be isolated.

## Commit And Deployment Plan

Each accepted batch permits at most:

- one source/migration commit;
- one tracker/docs evidence commit;
- one GitHub-controlled Azure staging deployment;
- one targeted staging verification pass.

Additional commits or deployments require a recorded failure or a declared
data-integrity risk. Manual local ZIP deployment is not permitted.

## Explicit Exclusions

Block 5 does not include:

- AI mapping, OCR, PDF import, Word import, semantic inference, embeddings, or
  predictive analytics;
- South African DOH Annual Inspection Mode;
- SOP/CPG ingestion;
- PDF evidence redesign;
- billing/provider implementation;
- production Azure architecture;
- website, trial, or sales work;
- global schematic-library expansion;
- startup seed/fallback data;
- automatic checklist publication;
- automatic access-account creation from staff imports.

## Credit-Control Rule

Block 5 is expected to require multiple credit purchase blocks. Work must follow
B5.1 through B5.5 in order, but each batch should be implemented as one coherent
verification-first slice rather than row-by-row micro-work. Accepted Blocks 1-4
must not be reopened without a reproducible regression under the tracker finality
rule.

The next implementation action after this design is B5.1 only.
