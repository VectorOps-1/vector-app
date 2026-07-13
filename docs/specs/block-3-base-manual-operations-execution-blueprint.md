# Block 3 Base Manual Operations Completion Execution Blueprint

Status: Proposed execution blueprint

Progress authority: `docs/specs/commercial-launch-progress-tracker.md`

Block status, percentage, and authorization to begin a batch come only from the
progress authority.

Created: 2026-07-13

Authority:

- `docs/specs/acuityops-recovery-roadmap.md` - Phase R2: Base MVP Manual Operations
- `docs/specs/commercial-completion-roadmap.md` - Phase C1: Base Commercial Foundation
- `docs/specs/block-2-base-commercial-execution-blueprint.md` - closed predecessor
- `docs/specs/staging-runbook.md` - Azure staging authority
- `docs/specs/ui-branding-contract.md` - authenticated app-shell and branding authority

This blueprint covers only the Base manual-operation requirements that remain after Block 2. It does not reopen Block 2 and does not imply completion of the wider commercial roadmap.

## Block 3 Objective

Complete the remaining Base manual workflows so a clean tenant can operate through normal UI flows without seed data, fallback behavior, hidden defaults, or manual database intervention.

Block 3 must make these remaining Phase R2 / Phase C1 workflows coherent:

- stock register and stock distribution;
- medication register;
- cross-asset movement;
- tasks, issues, and feedback;
- complete manual checklist authoring;
- audit evidence, navigation consistency, and targeted mobile behavior for the affected workflows.

## Closed Predecessor Boundary

Block 2 is closed. Block 3 must consume, not reopen, these verified foundations:

- Setup Wizard completion and no-seed behavior;
- vehicle, staff, and equipment register consistency;
- Checklist Register as the only live-check source of truth;
- publish targeting by area, function, subtype, and callsign;
- server-side action authorization for the Block 2 workflows;
- existing submitted report, PDF route, Readiness Dashboard, and Operations Reports baseline.

If a Block 3 change appears to require reopening a closed Block 2 acceptance criterion, stop and propose a separately authorized regression-fix batch.

## Mandatory Scope Guardrail

Block 3 cannot expand into any of the following:

- PDF redesign or evidence-pack expansion;
- Pro Excel register import;
- Pro checklist import or column matching;
- Premium AI import, generation, prediction, or analytics;
- South African DOH Annual Inspection Mode;
- billing, subscriptions, invoices, VAT/tax, refunds, downgrade, or cancellation;
- production Azure architecture or production tenant migration;
- product-owned schematic creation, editing, expansion, or library redesign;
- public website, pricing, sales, trial, SEO, or marketing work;
- SOP/CPG ingestion;
- global UI redesign;
- direct database patching for product behavior;
- seed, fallback, hidden default, or automatically recreated operational data.

Crossing this guardrail requires a new roadmap-authorized block. It must never be treated as an incidental Block 3 fix.

## Shared Execution Rules

1. The Credit Control Protocol in the active recovery roadmap applies to every batch.
2. The user must approve each implementation batch before product-source edits.
3. Inspect only files relevant to the approved batch.
4. Build once after a coherent source slice unless a build failure requires a targeted retry.
5. Use Azure staging for acceptance verification after workflow or UI changes.
6. Use fake tenant data only through normal UI flows and only for functional verification.
7. Never create test data through startup, seed routines, fallback logic, or direct SQL.
8. Back up staging data before controlled writes when verification requires record creation or mutation.
9. Restore or remove temporary verification data through an approved rollback path.
10. One source commit per coherent batch; docs/status reconciliation is a separate commit.
11. Stop before schema changes unless the approved batch explicitly permits a migration.
12. Stop before deleting tenant records, changing Azure resources, or pushing to GitHub unless explicitly authorized.

## Batch 3.1: Stock Register And Distribution Flow

Objective:

Make stock records and the complete stock-order lifecycle usable through one coherent Base workflow.

Scope:

- Verify and complete Stock Register grouping, alphabetical ordering, search, compact rows, collapsed groups, and item detail.
- Ensure allowed users can Open, Edit, Move, Report Issue, and Delete a specific stock record.
- Consolidate these actions behind `Stock Orders & Distribution`:
  - place stock order;
  - view order;
  - supplier confirmation;
  - receive stock;
  - enter received stock into the register;
  - allocate stock to an allowed destination.
- Preserve batch/lot, quantity, expiry, location, supplier, and receiving evidence where the existing model supports them.
- Apply existing permission and tenant-scope services to stock mutations.
- Add core audit records using the established audit pattern.
- Remove duplicate stock-order pathways only when their replacement route is verified.

Excluded work:

- Medication workflows.
- AI or Excel import.
- Supplier integrations, purchase payments, accounting, or invoices.
- Forecasting or automated reorder intelligence.
- New production storage architecture.

Likely files/modules:

- `Pages/Stock*.cshtml`
- `Pages/Stock*.cshtml.cs`
- `Pages/StockOrders*.cshtml`
- `Pages/StockOrders*.cshtml.cs`
- `Pages/MoveAsset.cshtml*`
- stock/order/allocation models and existing services;
- `Services/UserActionAuthorizationService.cs` only if existing stock permission checks are missing.

Database/migration risk:

- Medium-high.
- Reuse existing order, batch, location, and movement entities first.
- Stop before migration if the current model cannot link order, receipt, register entry, and allocation without data ambiguity.

Verification:

- Build.
- Deploy to Azure staging if source changes pass build.
- Through normal UI flows, verify one controlled order through supplier confirmation, receipt, register entry, and allocation.
- Verify register grouping/search and item actions.
- Verify tenant scope, role boundaries, confirmation behavior, and audit evidence.
- Verify rollback/removal of temporary functional test records.

Staging checks:

- `/Stock`
- `/StockRegister`
- `/StockOrders`
- exact existing stock-order detail/receipt/allocation routes discovered from visible navigation
- `/MoveAsset?asset=stock&assetId=<test-id>`

Commit plan:

- One source commit if no migration is required.
- If a migration is proven necessary, stop and propose a separate schema/source split before implementation.

Estimated credit cost:

- 1,800-2,600 credits.

Stop conditions:

- The existing data model cannot preserve order-to-allocation traceability.
- A duplicate route cannot be removed without losing a function.
- A stock mutation is not tenant-scoped.
- A fix requires billing, supplier integration, AI, or direct database patching.

Acceptance criteria:

- Stock Register is grouped, searchable, compact, and actionable.
- One stock order can move through confirmation, receipt, register entry, and allocation in one understandable workflow.
- Every mutation is permission-enforced, tenant-scoped, confirmed where destructive, and audit logged.
- No duplicate pathway bypasses the unified flow.

## Batch 3.2: Medication Register Consistency

Objective:

Make Medication Register a complete, consistent Base register without mixing medication into stock-order redesign.

Scope:

- Group alphabetically by medication/item name by default.
- Support alternate location grouping through the existing register UI pattern.
- Keep groups collapsed by default.
- Use compact clickable rows that reveal full item details.
- Provide Open/Edit/Move/Issue/Delete parity where access permits.
- Preserve medication name, formulation/concentration, quantity, batch/lot, expiry, assigned location, and status where present in the model.
- Apply existing tenant, area, permission, date-status, confirmation, and audit patterns.
- Ensure allowed movement destinations include vehicles, bases, operational areas, and storage spaces where relevant.

Excluded work:

- Clinical prescribing, patient administration, controlled-drug dispensing, or eMAR functionality.
- Pharmacy integrations.
- AI/Excel import.
- Expiry forecasting beyond existing Base date status.
- Stock-order workflow changes.

Likely files/modules:

- `Pages/Medication*.cshtml`
- `Pages/Medication*.cshtml.cs`
- medication register edit/detail pages;
- `Pages/MoveAsset.cshtml*` only for medication movement gaps;
- medication models and existing audit/permission services.

Database/migration risk:

- Medium.
- Reuse current medication fields. Stop before migration if clinically distinct records cannot be represented safely.

Verification:

- Build.
- Deploy to Azure staging if source changes pass build.
- Verify grouping, search, detail, edit, move, issue, delete, expiry display, access boundaries, and audit evidence using controlled UI-entered test data only.

Staging checks:

- `/Medication`
- `/MedicationRegister`
- existing medication edit/detail route
- `/MoveAsset?asset=medication&assetId=<test-id>`

Commit plan:

- One source commit if no migration is required.

Estimated credit cost:

- 1,200-1,800 credits.

Stop conditions:

- Existing medication records are not tenant-scoped.
- Required medication identity fields need a broader clinical data model.
- Work expands into prescribing, dispensing, pharmacy integration, AI, or Pro importing.

Acceptance criteria:

- Medication Register matches the established compact register pattern.
- Every medication item can be opened and managed according to saved permissions.
- Expiry and location are visible and movements use valid tenant destinations.
- No medication seed/fallback record is created.

## Batch 3.3: Cross-Asset Movement Completion

Objective:

Make asset movement consistent and auditable across vehicles, equipment, stock, and medication.

Scope:

- Verify and complete movement for vehicle, equipment, stock, and medication asset types.
- Resolve valid tenant-owned destinations from:
  - operational areas;
  - bases;
  - regions where operationally meaningful;
  - storage spaces;
  - registered vehicles where the asset can be carried.
- Preserve source, destination, moved by, date/time, reason, and optional task link.
- Keep `Send as a task` optional through an explicit toggle.
- Enforce saved movement permissions and the approved assigned-task exception server-side.
- Show in-app confirmation after successful movement.
- Update the applicable register location only after a successful movement.
- Audit every completed movement.

Excluded work:

- GPS tracking, telematics, barcode scanning, RFID, or route optimization.
- Inventory forecasting.
- New notification providers.
- Vehicle callsign reassignment logic except where an existing movement route already invokes it.

Likely files/modules:

- `Pages/MoveAsset.cshtml`
- `Pages/MoveAsset.cshtml.cs`
- movement models/services;
- destination resolution services;
- affected register page models only where location refresh is missing;
- `Services/UserActionAuthorizationService.cs` only for isolated movement gaps.

Database/migration risk:

- Medium.
- Stop before migration if destinations cannot be represented unambiguously by current destination type/id fields.

Verification:

- Build.
- Deploy to Azure staging.
- Move one controlled asset of each supported type through normal UI flows.
- Verify destination lists, source-of-truth location update, optional-task behavior, access boundaries, confirmation, audit row, and rollback cleanup.

Staging checks:

- `/MoveAsset` for each supported asset type;
- affected vehicle, equipment, stock, and medication register/detail routes;
- Audit Log filtered to the controlled movements.

Commit plan:

- One source commit if no migration is required.

Estimated credit cost:

- 1,400-2,100 credits.

Stop conditions:

- Movement destination records are not tenant-scoped.
- Location updates bypass the register source of truth.
- Movement requires a new universal asset model or destructive migration.
- Work expands into tracking hardware or external integrations.

Acceptance criteria:

- Every supported asset type can move to each operationally valid destination.
- Invalid cross-tenant or unsupported destinations are impossible to select or submit.
- Task creation remains optional.
- Successful movement updates the relevant register, produces confirmation, and creates audit evidence.

## Batch 3.4: Tasks, Issues, And Feedback Coherence

Objective:

Make operational work assignment, issue handling, and feedback behave as one coherent Base workflow across staff, operational managers, and senior managers.

Scope:

- Verify task creation, assignment, visibility, completion, deletion, and audit behavior.
- Verify issue reporting, assignment, visibility, status changes, resolution, deletion, and audit behavior.
- Keep staff deletion restricted unless an existing management-assigned task explicitly authorizes the action.
- Operational managers see only assigned tasks/issues and their permitted areas.
- Senior managers see company-wide tasks/issues.
- Task Feedback provides:
  - feedback about a specific received task;
  - general operational feedback.
- Specific-task feedback must use actual open tasks assigned to the current user.
- After accepted task feedback/completion, the task must leave the active feedback selector and active work list according to its status.
- Use in-app confirmation and temporary success messaging for destructive/status actions.

Excluded work:

- SMS/email notifications.
- New real-time messaging or chat.
- AI issue triage.
- Enterprise workflow designer.
- New escalation-provider integrations.

Likely files/modules:

- `Pages/Task*.cshtml*`
- `Pages/Issue*.cshtml*`
- `Pages/TaskFeedback.cshtml*`
- task/issue services and models;
- notification badges only where existing counts fail to reflect the corrected state;
- existing audit and permission services.

Database/migration risk:

- Medium.
- Reuse current task, issue, feedback, status, assignment, and area fields.
- Stop before migration if feedback cannot be linked to a task without ambiguous text matching.

Verification:

- Build.
- Deploy to Azure staging.
- Use one controlled task and issue through staff, operational manager, and senior manager sessions.
- Verify area scope, assignment scope, feedback linkage, completion/removal behavior, confirmation, and audit evidence.
- Restore temporary test data.

Staging checks:

- current task list/detail/send routes;
- current issue list/detail/report routes;
- `/TaskFeedback`;
- Home notification/task/issue links for all three access levels;
- Audit Log.

Commit plan:

- One source commit if no migration is required.

Estimated credit cost:

- 1,500-2,300 credits.

Stop conditions:

- Task feedback is not relationally linked and requires schema work.
- Area assignment is ambiguous or cross-tenant.
- Fix requires SMS/email, real-time messaging, or a new workflow engine.

Acceptance criteria:

- Tasks, issues, and feedback have clear role-appropriate ownership and visibility.
- Specific-task feedback is linked to a real assigned task.
- Completed/resolved work leaves active lists correctly without deleting required evidence.
- Destructive actions are permission-enforced, confirmed in-app, and audit logged.

## Batch 3.5: Manual Checklist Authoring Completion

Objective:

Complete the Base manual checklist builder without changing the verified Checklist Register source-of-truth or live-check publish model.

Scope:

- Preserve blank-by-default creation unless a user explicitly selects an existing template.
- Support manual creation, editing, reordering, and removal of:
  - sections;
  - items;
  - subitems;
  - columns;
  - vehicle fields;
  - section notes fields.
- Start new checklists with no populated rows or columns.
- Allow the two empty starter sections only when they contain no hidden template fields or items.
- Section names may use Vehicle, Equipment, Stock, Medication, or a user-entered custom name.
- Custom section name is editable only when Custom is selected.
- Notes fields remain optional and do not affect readiness or submission.
- Register links are optional and use structured source selectors rather than free-text source labels.
- Row-specific column applicability can disable a non-relevant service/expiry/check without changing every row.
- Keep sections and items collapsed by default with compact controls.
- Keep the builder matrix in one row/column table with sticky horizontal scrolling.
- Preserve existing publish approval rules and Block 2 publish targeting.
- Save/edit/delete/publish actions must use existing server-side permissions and audit patterns.

Excluded work:

- Excel checklist import.
- AI checklist generation or conversion.
- PDF redesign.
- Full Audit content design beyond preserving the existing checklist type route/name.
- New schematic assets or schematic library redesign.
- Readiness scoring redesign.

Likely files/modules:

- `Pages/EditChecklist.cshtml*`
- `Pages/EditVehicleChecklist.cshtml*`
- checklist template/section/item/subitem/column models;
- checklist builder serialization/binding services;
- existing checklist publishing and authorization services only where integration is required;
- shared JavaScript/CSS only for the builder controls and sticky matrix behavior.

Database/migration risk:

- Medium-high.
- First prove the existing checklist section/item/subitem/column entities can represent the requirements.
- Stop and propose a separate schema batch before adding or repurposing fields.

Verification:

- Build.
- Deploy to Azure staging.
- Through normal UI flows, build one checklist from blank with:
  - one vehicle section and field;
  - one equipment item with a subitem;
  - one additional stock or medication section;
  - multiple columns;
  - one row-specific non-applicable column;
  - one optional notes field.
- Save, reopen, edit, and republish to a controlled existing target.
- Verify the crew-facing checklist renders the saved structure only.
- Stop before submitting a new daily check unless submission is necessary to prove builder persistence.
- Remove controlled temporary records through the normal UI where safe.

Staging checks:

- `/EditChecklist`
- `/EditChecklist?view=register`
- `/EditVehicleChecklist?checklist=daily-vehicle&mode=build`
- edit route for the controlled saved template;
- `/DailyVehicleChecklist` for the controlled target.

Commit plan:

- One source commit if no migration is required.
- Separate migration/source plan required if the current model cannot represent row-specific applicability or subitems safely.

Estimated credit cost:

- 2,400-3,600 credits.

Stop conditions:

- Existing checklist entities cannot persist the required structure without schema changes.
- The builder attempts to recreate default/fallback sections, rows, columns, or equipment.
- A change threatens Checklist Register source-of-truth behavior.
- Work expands into import, AI, PDF, DOH, readiness redesign, or schematic creation.

Acceptance criteria:

- A Base client can build a useful checklist manually from blank without developer or database intervention.
- Saved sections, items, subitems, columns, row applicability, register links, and notes persist and reopen correctly.
- Crew view renders only the published saved structure.
- No hidden starter content, fallback fields, or seed checklist appears.
- Builder controls remain compact and usable on desktop and mobile.

## Batch 3.6: Base Manual Operations Closure Regression

Objective:

Verify the Block 3 workflows together and close remaining Phase R2 / Phase C1 manual-operation criteria without adding features.

Scope:

- No feature development unless one isolated blocker is proven and separately approved.
- Verify affected workflows on Azure staging:
  - stock register and distribution;
  - medication register;
  - cross-asset movement;
  - tasks, issues, and feedback;
  - manual checklist authoring and crew rendering;
  - audit evidence;
  - permission and tenant boundaries;
  - navigation without duplicate affected pathways;
  - targeted desktop/mobile layout for affected lists and builder matrix.
- Confirm startup and normal usage create no seed/fallback operational data.
- Confirm Block 2 checklist/report/readiness evidence remains intact after Block 3.

Excluded work:

- All work prohibited by the Mandatory Scope Guardrail.
- Broad full-app redesign or audit outside the Block 3 routes.

Likely files/modules:

- No product source expected.
- This blueprint may receive completion evidence in a separate docs-only reconciliation.

Database/migration risk:

- Low for read-only closure verification.
- Controlled verification writes require backup and rollback.

Verification:

- Targeted build/CI status check for the committed Block 3 source.
- One controlled Azure staging workflow pass across the listed routes and roles.
- No repeated verification after a pass.

Commit plan:

- No source commit expected.
- One docs-only closure commit after every acceptance criterion has evidence.

Estimated credit cost:

- 600-1,000 credits.

Stop conditions:

- Any true failure requires a separately proposed, smallest-safe fix batch.
- Any failure belongs to a prohibited future phase.
- Verification requires direct database patching.

Acceptance criteria:

- Every Block 3 batch has committed source evidence or documented no-change verification evidence.
- Every Block 3 workflow passes on Azure staging.
- No Block 3 change regresses tenant isolation, permissions, checklist source-of-truth, PDF availability, readiness, or operational reporting.
- Block 3 can be marked closed without carrying hidden Base manual-operation defects into PDF or Pro phases.

## Credit Plan

Expected implementation range:

- Batch 3.1: 1,800-2,600 credits.
- Batch 3.2: 1,200-1,800 credits.
- Batch 3.3: 1,400-2,100 credits.
- Batch 3.4: 1,500-2,300 credits.
- Batch 3.5: 2,400-3,600 credits.
- Batch 3.6: 600-1,000 credits.
- Total expected range: 8,900-13,400 credits.

Cost-control order:

1. Start each batch with a verification-first check of current source and staging behavior.
2. If the acceptance criteria already pass, record no-change evidence and do not rewrite the workflow.
3. Implement only proven gaps.
4. Stop Block 3 spending at the approved credit ceiling and preserve incomplete criteria in this same block; do not invent a new block to hide unfinished work.
5. Do not rush Batch 3.5 checklist authoring if the existing data model requires a separate migration decision.

## Block 3 Completion Gate

Block 3 may close only when:

1. Batches 3.1 through 3.6 have completion evidence.
2. Every acceptance criterion in this blueprint is satisfied.
3. No stop condition remains active.
4. Azure staging passes the targeted closure regression.
5. No operational seed/fallback data is created.
6. The closed Block 2 source-of-truth, access, evidence, and reporting baseline remains intact.

Block 3 closure does not mean AcuityOps is production-ready. It permits the roadmap to proceed to Phase R3 / Phase C2 PDF Evidence And Reporting Reliability.
