# Block 2 Base Commercial Foundation Execution Blueprint

Status: Closed

Progress authority: `docs/specs/commercial-launch-progress-tracker.md`

Block 2 acceptance is locked in the progress authority. This blueprint remains
the detailed evidence and scope record only.

Closed: 2026-07-12

Created: 2026-07-12

Authority:

- `docs/specs/commercial-completion-roadmap.md`
- `docs/specs/acuityops-recovery-roadmap.md`
- `docs/specs/staging-runbook.md`
- `docs/specs/ui-branding-contract.md`

This blueprint converts Base Commercial Foundation into implementation-ready batches for the next 10,000-credit block. It is not a pilot plan, demo plan, Pro import plan, AI plan, billing plan, or DOH inspection plan.

## Block 2 Objective

Move AcuityOps from a stable Azure staging foundation into a stronger Base commercial product foundation:

- setup can be completed by a real client without seed data;
- registers are consistently editable and usable;
- checklist build/publish/live-check behavior remains source-of-truth driven;
- access permissions start controlling real actions;
- report detail and PDF baseline remain functional after Base changes.

Block 2 will not complete the entire Base product. It must create the cleanest next step toward the complete commercial SaaS product without drifting into Pro/Premium features.

## Hard Do-Not-Touch List

Do not include these in Block 2 implementation unless a separate explicit approval is given:

- AI importing;
- AI checklist generation;
- AI predictive analytics;
- Excel import;
- Excel checklist column matching;
- South African DOH Annual Inspection Mode;
- billing, invoices, VAT/tax, payments, subscription state changes;
- production Azure provisioning;
- production tenant migration;
- global vehicle schematic library expansion;
- SOP/CPG ingestion;
- public website/marketing pages;
- pricing page;
- sales/demo copy beyond existing staging docs;
- direct database patching for product behavior;
- seed/fallback operational data creation;
- X Med/x-med as product identity or fallback;
- broad full-app audits.

Fake tenant data may be used only for functional testing through normal UI workflows.

## Batch 2.1: Setup Wizard Closeout And No-Seed Gate

Status: Completed

Objective:

Make Setup Wizard completion reliable enough that a clean tenant can complete onboarding configuration without creating hidden operational seed data.

Scope:

- Review the current Setup Wizard step pages and completion state only.
- Complete or repair remaining Base setup steps required by the roadmap:
  - company identity completion state;
  - operational structure completion state;
  - vehicle function/subtype setup completion state;
  - staff qualification/scope setup completion state;
  - access model setup completion state;
  - register setup choices completion state;
  - checklist setup choices completion state;
  - readiness setup choices completion state;
  - final review and complete setup action.
- Ensure `/Home` gating uses the completed setup state.
- Ensure staff and operational managers cannot complete company setup unless explicitly permitted.
- Ensure setup choices do not create vehicles, staff, equipment, stock, medication, checklists, readiness rules, schematic assignments, or demo records.
- Add/adjust audit logging for setup completion only if the existing audit pattern already supports it.

Excluded work:

- Register CRUD implementation.
- Checklist builder changes.
- Readiness engine redesign.
- Access permission matrix redesign.
- UI redesign beyond clear setup status/action labels.
- Any direct DB data cleanup.

Likely files/modules:

- `Pages/SetupWizard*.cshtml`
- `Pages/SetupWizard*.cshtml.cs`
- `Pages/OperationalStructureSetup*.cshtml`
- `Pages/VehicleStructureSetup*.cshtml`
- `Pages/StaffStructureSetup*.cshtml`
- `Pages/AccessModelSetup*.cshtml`
- `Pages/AssetRegisterSetup*.cshtml`
- `Pages/ChecklistSetup*.cshtml`
- `Pages/ReadinessEngineSetup*.cshtml`
- `Pages/Home.cshtml.cs`
- `Services/CurrentUserService.cs`
- `Data/VectorDbContext.cs`
- `Models/Company.cs`
- migrations only if completion fields are missing.

Database/migration risk:

- Medium.
- Migration is allowed only if current company/setup fields cannot represent completion state safely.
- No data mutation beyond normal controlled UI verification.

Verification:

- Build.
- If source changed, deploy to Azure staging.
- Use a controlled test tenant/state only through UI where possible.
- Verify:
  - incomplete setup routes to setup;
  - completed setup reaches Home;
  - setup completion survives reload;
  - setup completion survives logout/login;
  - no operational records are created by setup completion;
  - staff/ops roles see a blocked/incomplete state if setup is incomplete.

Staging checks:

- `/CompanyLogin/acuityops-workspace`
- `/Access`
- `/RoleLogin?access=senior-management`
- `/SetupWizard`
- `/Home`

Commit plan:

- One source commit.
- One docs/status commit only if this blueprint or roadmap status is updated.

Expected credit cost:

- 2,000-3,000 credits.

Stop conditions:

- Setup completion requires direct database patching to work.
- Seed/fallback records are created.
- Required setup state needs a larger data model redesign.
- Login/session scoping becomes uncertain.

Acceptance criteria:

- A clean tenant can complete setup configuration and reach Home without hidden product data.
- Incomplete tenants are gated clearly.
- Setup Wizard no longer feels like an unfinished blocker to Base commercial foundation.

Completion evidence:

- Phase 2A setup wizard implementation and closure evidence already proved company-owned, resumable setup progress, role-appropriate setup access, final review, setup completion, Home unlock, first-operation guidance, setup audit logging, and clean-company no-seed behavior.
- Commit `ae8da60` (`Decouple setup completion from branding status`) made the completed setup state independent from tenant branding state, preventing logo/name configuration from incorrectly controlling Home access.
- Azure staging verification confirmed incomplete company setup remains gated, completed setup reaches Home, staff and operational managers do not receive actionable company setup controls, and senior management retains the setup completion path.
- Setup completion did not create vehicles, staff, equipment, stock, medication, checklist templates, publish scopes, readiness rules, schematic assignments, or demo records.
- No direct database patch was required for product behavior.

## Batch 2.2: Vehicle, Staff, And Equipment Register Consistency

Status: Completed

Objective:

Make the three highest-impact Base registers commercially usable before touching stock/medication/checklist complexity.

Scope:

- Vehicle Register:
  - grouped by function then subtype;
  - collapsed groups by default;
  - list rows, not bulky cards;
  - Open/Edit available for each vehicle;
  - function/subtype edit persists and reflects in list;
  - assigned area, callsign, registration, status visible;
  - move/issue links remain intact.
- Staff Register:
  - grouped by clinical qualification/scope, not role title;
  - list rows, not bulky cards;
  - Open/Edit available according to access;
  - profile includes practitioner number, annual license expiry, CPD status/expiry;
  - staff can edit allowed personal fields;
  - managers can edit register-controlled fields within scope.
- Equipment Register:
  - grouped by equipment type/name;
  - collapsed groups by default;
  - list rows, not bulky cards;
  - Open/Edit/Move/Issue/Delete parity where allowed;
  - service date/location/status visible;
  - vehicle destinations available in movement.

Excluded work:

- Stock register.
- Medication register.
- Excel import.
- AI import.
- Full permission matrix enforcement beyond these register actions.
- Full date-coloring sweep unless needed for visible service/license/CPD fields in these pages.

Likely files/modules:

- `Pages/VehicleRegister.cshtml`
- `Pages/VehicleRegister.cshtml.cs`
- `Pages/EditVehicle.cshtml`
- `Pages/EditVehicle.cshtml.cs`
- `Pages/StaffRegister.cshtml`
- `Pages/StaffRegister.cshtml.cs`
- `Pages/StaffRecordsSearch.cshtml`
- `Pages/StaffRecordsSearch.cshtml.cs`
- `Pages/EditStaffProfile.cshtml`
- `Pages/EditStaffProfile.cshtml.cs`
- `Pages/EquipmentRegister.cshtml`
- `Pages/EquipmentRegister.cshtml.cs`
- `Pages/MoveAsset.cshtml`
- `Pages/MoveAsset.cshtml.cs`
- `Models/Vehicle.cs`
- `Models/AppUser.cs`
- `Models/EquipmentAsset.cs`
- `Services/VehicleTaxonomyService.cs`
- `Services/CurrentUserService.cs`
- migrations only if missing fields are not already present.

Database/migration risk:

- Medium.
- Staff profile fields may already exist; verify before adding migrations.
- Avoid schema churn if existing fields can be reused.

Verification:

- Build.
- Deploy to Azure staging if source changed.
- Verify with controlled tenant data only:
  - vehicle list grouping;
  - vehicle edit function/subtype;
  - staff grouping by qualification/scope;
  - staff profile edit boundaries;
  - equipment grouping and item edit/open/move/issue links;
  - no role sees unauthorized edit/delete actions.

Staging checks:

- `/VehicleRegister`
- `/EditVehicle?vehicleId=<test-id>`
- `/StaffRegister`
- `/StaffRecordsSearch?staffUserId=<test-id>`
- `/EquipmentRegister`
- `/MoveAsset?asset=equipment&assetId=<test-id>`

Commit plan:

- One source commit if changes stay within these three registers.
- Split into separate commits only if a migration is required.

Expected credit cost:

- 3,000-4,000 credits.

Stop conditions:

- Existing data model cannot support staff clinical qualification and role separation.
- Register actions require a broader access-control framework first.
- Movement destinations are not tenant-scoped.
- A change risks deleting or mutating tenant records outside UI verification.

Acceptance criteria:

- Vehicles, staff, and equipment registers feel like consistent app registers.
- Each rendered item can be opened and edited where access allows.
- Bulky card layouts are removed from these core registers.

Completion evidence:

- Time-boxed source and Azure staging verification passed separately for Vehicle Register, Staff Register, and Equipment Register.
- Vehicle Register is grouped by function and then subtype, uses collapsed groups, exposes compact rows and record actions, and persists function/subtype changes through the vehicle source-of-truth edit path.
- Staff Register is grouped by clinical qualification/scope rather than access role and exposes practitioner number, annual licence expiry, CPD status/expiry, profile open, and access-controlled edit behavior.
- Equipment Register is grouped by equipment type/name with collapsed groups and exposes Edit, Move, Issue, and Delete actions according to access.
- Equipment movement includes vehicle destinations through the existing tenant-scoped movement flow.
- Batch 2.4 subsequently added server-side enforcement for the register actions verified in this batch.
- No register schema migration or direct tenant-data mutation was required to satisfy this batch.

## Batch 2.3: Checklist Source-Of-Truth Guard And Publish Target Review

Status: Completed

Objective:

Keep checklist behavior commercially safe before doing deeper checklist builder work.

Scope:

- Confirm Build New Checklist starts blank unless a template is explicitly selected.
- Confirm deleted/retired templates cannot be used by live daily checks.
- Confirm live daily checks show "No assigned checklist available" when no active published checklist exists for the selected vehicle target.
- Review publish scope UI and logic for:
  - area;
  - vehicle function;
  - vehicle subtype;
  - specific registration/callsign.
- Add/repair replacement warning only if the publish path already has a clear target model.
- Confirm audit log entries for checklist publish/replace/delete where existing audit patterns support it.

Excluded work:

- Full checklist builder redesign.
- Excel checklist import.
- AI checklist generation.
- New checklist column-mapping engine.
- Full Audit implementation.
- PDF redesign.

Likely files/modules:

- `Pages/EditChecklist.cshtml`
- `Pages/EditChecklist.cshtml.cs`
- `Pages/EditVehicleChecklist.cshtml`
- `Pages/EditVehicleChecklist.cshtml.cs`
- `Pages/DailyVehicleChecklist.cshtml`
- `Pages/DailyVehicleChecklist.cshtml.cs`
- `Services/ChecklistDisplayService.cs`
- `Models/ChecklistTemplate.cs`
- `Models/ChecklistPublishScope.cs`
- `Data/VectorDbContext.cs`
- migrations only if publish target cannot represent function/subtype/callsign safely.

Database/migration risk:

- Medium-high.
- Publish scope model must not be hacked if missing target fields are found.
- If schema is insufficient, stop and propose a separate migration batch.

Verification:

- Build.
- Deploy to Azure staging if source changed.
- Functional checks:
  - clean target with no published checklist shows no assigned checklist;
  - published checklist loads only for intended target;
  - retired/deleted checklist does not load;
  - replacement warning appears before replacing active target;
  - report/PDF route still works for an existing submitted report.

Staging checks:

- `/EditChecklist?view=register`
- `/EditVehicleChecklist?checklist=daily-vehicle&mode=build`
- `/DailyVehicleChecklist`
- `/ChecklistReports`
- `/ChecklistReportDetail?id=<test-id>`

Commit plan:

- One source commit if no migration.
- Separate schema commit if publish target migration is required.

Expected credit cost:

- 2,000-3,000 credits.

Stop conditions:

- Live daily check still depends on old fixed-form behavior.
- Publish targets require a broader taxonomy redesign.
- A source fix would mutate existing checklist records without explicit approval.

Acceptance criteria:

- Checklist register remains the only source of truth for live daily checks.
- Publish targets are understandable and safe.
- No fallback checklist appears.

Completion evidence:

- Source and Azure staging verification confirmed Build New Checklist starts blank unless a user explicitly selects a template.
- Live daily checks resolve only active, published Checklist Register templates and scopes; no fixed-form or fallback daily checklist is rendered.
- A vehicle without an active matching scope renders `No assigned checklist available`.
- Publishing supports operational area, vehicle function, vehicle subtype, and specific vehicle/callsign targets.
- Existing active target coverage is shown and replacement warnings are generated before publishing over an active target.
- Publish, replacement, retirement, and deletion paths use the existing audit pattern, and deleting a template retires its active scopes so it cannot remain live.
- Controlled staging data proved that one UI-built and register-published checklist loaded for its intended demo vehicle and produced the submitted evidence later used by Batch 2.5.
- No seed checklist, fallback template, or direct database behavior patch was introduced.

## Batch 2.4: Core Access Action Enforcement

Status: Completed

Source commit: `a6a9d59` (`Enforce core access actions`)

Objective:

Begin enforcing Base role boundaries on real actions, starting with the actions touched in Batches 2.1-2.3.

Scope:

- Confirm current saved permission model.
- Enforce action boundaries for:
  - setup completion;
  - vehicle edit;
  - staff profile/register-controlled edit;
  - equipment edit/move/issue/delete;
  - checklist publish vs submit-for-approval.
- Staff must not receive manager edit/delete powers unless explicitly task-authorized.
- Operational managers must remain scoped to their assigned area(s).
- Senior management sees company-wide records.

Excluded work:

- Full enterprise permission builder.
- Billing/tier-based permissions.
- Pro/Premium feature gates.
- New notification workflows.

Likely files/modules:

- `Pages/AccessSetup*.cshtml`
- `Pages/AreaManagerControl*.cshtml`
- `Services/CurrentUserService.cs`
- `Services/PermissionService.cs` if present;
- register page models touched in Batches 2.1-2.3;
- checklist page models touched in Batch 2.3.

Database/migration risk:

- Medium.
- If saved permissions are not structured enough, stop and propose a permission model batch.

Verification:

- Build.
- Deploy to Azure staging if source changed.
- Verify staff, ops, senior flows:
  - staff denied manager actions;
  - ops manager scoped to assigned areas;
  - senior sees company-wide;
  - hidden buttons are matched by server-side handler checks.

Staging checks:

- staff login;
- operational management login;
- senior management login;
- relevant register/checklist/setup routes.

Commit plan:

- One source commit if no schema change.
- Separate migration commit if permission schema must change.

Expected credit cost:

- 1,500-2,500 credits.

Stop conditions:

- UI hides actions but handlers remain open and require broader security work.
- Permission storage is placeholder-only.
- Fix requires touching billing/tier gates.

Acceptance criteria:

- Core Base actions are enforced server-side for the touched workflows.
- Access behavior is not just navigation hiding.

Completion evidence:

- The existing saved permission model was sufficient; no permission schema or migration change was required.
- Added one shared server-side action authorization service and applied it only to setup completion, checklist publish/delete, vehicle edit/delete, staff register-controlled edit, and equipment edit/move/delete.
- Staff manager actions are denied unless the approved assigned movement-task exception applies.
- Operational manager actions require both the relevant saved permission and assigned-area scope.
- Senior management and company-owner access remains company-wide through the existing permission model.
- Operational managers retain checklist draft/submission behavior; checklist approval and publishing require the saved publish permission or an existing delegated publish task.
- Equipment issue reporting remains available as an operational safety action and was not converted into a manager-only permission.
- Local build verification passed with 0 warnings and 0 errors.
- Azure staging verification passed for staff, operational manager, and senior manager login paths and the scoped setup, checklist, vehicle, staff, equipment, and movement actions.
- No database data, schema, tenant records, seed data, product-owned schematics, runtime files, generated files, logs, or Azure resource configuration were changed by the source commit.

Remaining boundary:

- Full enterprise permission design, tier-based permissions, Pro/Premium feature gates, and new notification workflows remain excluded from Batch 2.4.

## Batch 2.5: Evidence Baseline Regression Check

Status: Completed

Objective:

Ensure Base changes did not break report detail, PDF route, readiness, or operational reports.

Scope:

- No new feature work.
- Verify existing report detail and PDF after Batches 2.1-2.4.
- Verify Readiness Dashboard and Operations Reports still reconcile for controlled functional test data.
- Update docs only if known limits changed.

Excluded work:

- PDF redesign.
- Report UI redesign.
- New analytics.
- DOH mode.

Likely files/modules:

- No source files expected unless a blocker is found.
- `docs/specs/first-demo-script.md` or this blueprint only if status changes.

Database/migration risk:

- Low.

Verification:

- Azure staging only:
  - `/ChecklistReports`;
  - `/ChecklistReportDetail?id=<test-id>`;
  - `/ChecklistReports?id=<test-id>&handler=Pdf`;
  - `/ReadinessDashboard`;
  - `/OperationsReports`.

Commit plan:

- No source commit expected.
- Docs-only commit only if status docs are updated.

Expected credit cost:

- 500-800 credits.

Stop conditions:

- Any report/PDF/readiness break requires a separate approved fix batch.

Acceptance criteria:

- Base workflow changes did not break evidence visibility.

Completion evidence:

- Post-Batch-2.4 Azure staging verification completed on 2026-07-12 against existing submitted report `3`; no test records or database changes were created.
- Checklist Reports displayed the existing `DEM-101 / A01` submitted checklist as Operational with Critical `0`, Warning `0`, and the correct date, area, vehicle, and submitter.
- Checklist Report Detail opened successfully and rendered the saved template/version, submitter, vehicle, checklist responses, and readiness evidence.
- The authenticated PDF endpoint `/ChecklistReports?id=3&handler=Pdf` completed a PDF media download.
- Readiness Dashboard reconciled to `1 / 1` ready vehicle, `1` daily check complete, `0` missing checks, and `0` not-ready vehicles.
- Operations Reports reconciled to `1` saved/submitted check and linked the same `DEM-101 / A01` Operational report with no action required.
- No report, PDF, readiness, or operational evidence failure remained after Batch 2.4 access enforcement.

## Block 2 Closure

Block 2 is closed with all five batches completed and verified:

1. Batch 2.1 - Setup Wizard Closeout And No-Seed Gate: Completed.
2. Batch 2.2 - Vehicle, Staff, And Equipment Register Consistency: Completed.
3. Batch 2.3 - Checklist Source-Of-Truth Guard And Publish Target Review: Completed.
4. Batch 2.4 - Core Access Action Enforcement: Completed in source commit `a6a9d59` and documented in commit `51b9f93`.
5. Batch 2.5 - Evidence Baseline Regression Check: Completed after Batch 2.4 with no failures.

Closure determination:

- Every Block 2 acceptance criterion has evidence.
- No Block 2 stop condition remains active.
- No unresolved Block 2 product blocker remains.
- Work excluded by the Block 2 hard do-not-touch list remains future roadmap work and is not implied complete by this closure.

## Historical 10,000-Credit Allocation

If the next block is limited to 10,000 credits, use this priority order:

1. Batch 2.1 Setup Wizard Closeout: 2,000-3,000 credits.
2. Batch 2.2 Vehicle/Staff/Equipment Register Consistency: 3,000-4,000 credits.
3. Batch 2.3 Checklist Source-Of-Truth Guard: 2,000-3,000 credits.
4. Batch 2.5 Evidence Baseline Regression Check: 500-800 credits.

Only start Batch 2.4 Access Action Enforcement if the first three batches finish under budget. If credits run low, defer Batch 2.4 rather than rushing access/security work.

## Closed-Block Guardrail

Do not restart or add batches to Block 2. Any further work must be selected from the active recovery/commercial roadmap and proposed as a new, separately approved block or phase.
