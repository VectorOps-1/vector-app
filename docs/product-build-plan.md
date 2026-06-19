# AcuityOps Product Build Plan

## Product Target

AcuityOps should become an EMS operational readiness system, not a generic register app. The core value is proving, in real time, whether a service is operationally ready across vehicles, onboard equipment, stock, medication, staff accountability, tasking, reported issues, and audit evidence.

The product must feel like a practical command layer for private ambulance services: fast to onboard, hard to misuse, clear on accountability, and strong enough to replace fragmented spreadsheets, paper checks, WhatsApp messages, and disconnected registers.

## Product Description

AcuityOps is an operational readiness platform for ambulance and field-response services. It combines daily vehicle checks, carried-equipment checks, stock control, medication tracking, staff records, tasks, issue reporting, asset movement, audit logs, PDF evidence, and readiness scoring in one practical workflow.

The app is designed for services that need to know what is ready right now, why something is not ready, who is responsible, and what action must happen next.

The product should be described simply as:

`AcuityOps is the affordable EMS readiness layer that turns registers, checklists, tasks, issues, stock, medication, and audit evidence into one live operational picture.`

The key business position is:

`AcuityOps gives ambulance services live operational readiness without buying five separate systems or paying enterprise software prices.`

## Build Sequence

1. Stabilise the access flow and company workspace link.
2. Make vehicle plus equipment readiness the strongest workflow in the product.
3. Add the Readiness Engine as the scoring source for vehicles, equipment, stock, medication, issues, and checklist completion.
4. Make Master Setup the source of truth for registers, checklist templates, schematics, areas, managers, permissions, and readiness scoring rules.
5. Turn every check, issue, movement, order, upload, and task into reportable evidence.
6. Add guided register and checklist import so clients can move from spreadsheets into live AcuityOps records.
7. Add exports and intelligence once the data model is dependable.

## Product Non-Negotiables

These principles should guide every future feature:

- Users should select from setup-driven records wherever possible, not type critical data manually.
- Staff should only see the operational actions relevant to them unless management sends them a task.
- Operational managers should see their assigned areas and the work they are responsible for.
- Senior managers and company owners should be able to see the whole operation.
- Every save, publish, delete, override, approval, rejection, movement, issue, and task should create evidence.
- Daily readiness must combine the vehicle and carried equipment in one operational report.
- The dashboard must explain why the score changed, not just show a number.
- Base must be useful enough to retain customers, while Pro and Premium create obvious upgrade pressure.
- The system must be designed for local storage in Base/Pro and Azure-backed scale in Premium without changing the user workflow.

## Readiness Engine

The Readiness Engine is the core product differentiator. AcuityOps should not simply store checklists; it should calculate operational readiness from controlled, auditable scoring rules.

The engine should live under:

`Master Setup -> Readiness Engine`

Senior management can build, edit, save drafts, publish, archive, restore, and version the active scoring model. Operational managers can request scoring alterations, but senior management must approve and apply, or reject and clear, every request.

### Rule Table

The Readiness Engine should behave like a controlled spreadsheet:

- add rule row
- remove rule row
- duplicate rule row
- edit cells inline
- activate or deactivate rules
- filter by asset type, section, area, vehicle type, severity, and active state
- save draft scoring versions
- publish one active scoring version
- keep audit history of all changes

Each row should include:

- `Asset type`: Vehicle, Equipment, Stock, Medication, Issue Report, Checklist Completion
- `Section`: vehicle details, operational checks, schematic/damage, carried equipment, stock, medication, issue, task, or completion
- `Item / field`: tyre, windscreen, monitor defibrillator, oxygen, ET tube, morphine, stock quantity, checklist section, etc.
- `Trigger / answer`: missing, low, expired, not operational, S/N mismatch, wrong location, unresolved issue, omitted item, service overdue, etc.
- `Applies to`: all vehicles, vehicle type, specific checklist template, specific operational area, or specific asset group
- `Severity`: No impact, Minor, Moderate, Major, Critical, Hard blocker
- `Default score impact %`: visible reference value
- `Manual score impact %`: optional senior-management override
- `Manager alert`: yes/no
- `Active`: yes/no

Severity should use a dropdown. The default recommended ranges are:

- No impact: 0%
- Minor: -1% to -5%
- Moderate: -6% to -15%
- Major: -16% to -35%
- Critical: -36% to -60%
- Hard blocker: vehicle or asset is not ready, regardless of percentage

A hard blocker is not just a large deduction. It overrides the readiness result for that vehicle or asset and marks it not ready.

### Auto-Population

The engine must be able to add rules manually and auto-populate candidate rules from all operational assets:

- Vehicles: unavailable status, missing daily check, service overdue, licence/registration issue, operational failure, schematic damage, warning lights, tyres, sirens, radio connectivity
- Equipment: missing item, not operational, low/flat/charging battery, service overdue, damage, wrong vehicle, wrong location, S/N mismatch, S/N not in register
- Stock: below minimum quantity, missing required stock, expired disposable item, wrong location, batch discrepancy
- Medication: below minimum quantity, expired medication, wrong schedule handling, missing required medication, wrong location, batch discrepancy
- Issue reports: open critical issue, unresolved assigned issue, recurring issue against the same asset
- Checklist completion: missing check, incomplete section, omitted required field, unapproved same-as-previous-shift use

Auto-populated rules should appear as inactive or suggested until senior management activates them. Managers can remove irrelevant rows, keep default AcuityOps rows, or add local custom rules.

### Scoring Behaviour

Each vehicle starts at 100%. The active rules apply deductions or blockers based on captured checklist data, register status, open issues, stock/medication availability, and audit-relevant events.

The readiness dashboard should calculate:

- vehicle-level score
- vehicle hard-blocker status
- area/base score for operational managers
- all-area company score for senior managers
- metric lists explaining which variables affected the score
- manager accountability for unresolved blockers

The dashboard should never guess readiness from a loose status field once the engine exists. It should ask the engine for the score, blockers, warnings, and source variables.

### Manager Request Workflow

Operational managers should have a `Request scoring change` workflow:

- select the rule or affected asset/checklist field
- suggest a new severity, percentage, alert setting, or active/inactive state
- add reasoning
- submit to senior management

Senior management can:

- approve and apply
- reject
- add a note

Both outcomes clear the request, notify the requester, and write to the audit log.

## Reporting System

Reporting must cover:

- readiness history by vehicle, area, staff member, shift, and status
- issue reports by module, severity, assignee, resolution, and action taken
- tasks by sender, recipient, completion, deletion, and expiry
- asset movement by vehicle, equipment, stock, medication, source, destination, and user
- stock orders, supplier confirmations, batch entries, and allocation
- equipment service pressure and medication expiry pressure
- setup uploads, register imports, checklist imports, and approval history
- audit trail records for accountability and compliance

## Import System

Register and checklist uploads should move in stages:

- Base: upload files, store them, preview them, then build records manually from the preview.
- Pro: guided import wizard with column matching, duplicate detection, validation, and senior approval before publishing.
- Premium: AI-assisted parsing, automatic template/register generation, confidence scoring, conflict explanation, and migration support.

## Main Selling Points

AcuityOps should sell around operational confidence:

- act as the readiness operating system for ambulance services, not just a checklist app
- know which vehicles are actually ready now
- know which equipment is on which vehicle, whether it is operational, and when service is due
- use a configurable readiness scoring engine so a minor windscreen chip does not carry the same weight as a missing defibrillator
- stop crews typing critical vehicle details manually by using setup-driven dropdowns
- preserve a 12-hour shift draft when a crew loses internet or gets dispatched mid-check
- work offline-first for field checks and sync when internet returns
- let managers safely permit "Same as previous shift" separately for vehicle inspection and equipment
- keep vehicle, equipment, staff, stock, and medication registers searchable and reportable
- make every issue report, task, stock movement, equipment service update, medication batch, and asset reallocation accountable
- let senior managers see the whole company while operational managers see only their assigned bases or regions
- import existing spreadsheets and checklists so onboarding does not depend on rebuilding everything manually
- turn field activity into useful reports, not just stored records
- generate PDF checklist evidence packs for audits, disputes, inspections, insurance, and internal discipline
- show readiness risk reports by vehicle, area, item, staff member, supplier, issue type, expiry, and repeated failure pattern
- support supplier ordering from low stock through approval, supplier email/order, supplier confirmation, batch/expiry capture, and stock allocation
- create manager accountability by tracking unresolved alerts, ignored blockers, late checks, deleted rows, overridden scores, and rejected variance alerts
- make onboarding a paid service by helping clients clean up registers, checklists, vehicle loadouts, and scoring rules

## Strategic Product Positioning

The product should be positioned as:

`AcuityOps tells ambulance leadership what is ready, what is missing, what is unsafe, who is responsible, and what must happen next.`

The market has separate tools for inventory, dispatch, narcotic tracking, fleet management, and checklists. AcuityOps should win by combining daily readiness, asset truth, checklist intelligence, issue escalation, stock and medication control, manager accountability, and evidence reporting in one operational layer.

Current competitive reality:

- General checklist platforms can collect inspection answers, but they do not naturally become EMS readiness command systems.
- Fleet platforms can manage vehicles, but they do not usually prove whether the carried equipment, medication, stock, and unresolved issues make a unit ready for clinical response.
- Asset and inventory platforms can track items, but they do not usually connect daily crew checks, manager approvals, issue escalation, stock ordering, medication expiry, and readiness scoring.
- EMS-specific enterprise platforms are closer, but they are often quote-based, expensive, slower to implement, and aimed at larger organisations.
- Affordable platforms exist, and comprehensive EMS platforms exist, but there is no obvious low-cost all-in-one product that clearly does the full AcuityOps concept end to end.

That gap is the opportunity: AcuityOps should be affordable enough for medium and moderate-sized private ambulance services, while still feeling operationally complete.

The strongest commercial promise is fast onboarding:

`Send us your existing Excel registers and checklists. We configure your operation and give you a live readiness system.`

This is especially important for medium-sized private ambulance services that do not have time for long enterprise implementation projects.

## Why AcuityOps Is Special

AcuityOps is special because it is not trying to be another checklist app, stock register, fleet list, or task tool. It connects those parts into one operational readiness system.

AcuityOps must still clearly include the vital baseline functions buyers expect:

- vehicle register
- equipment register
- stock register
- medication register
- staff register and staff files
- daily vehicle and equipment checks
- checklist builder and checklist register
- issue reporting
- tasking and feedback
- asset movement and reallocation
- supplier stock ordering
- stock and medication batch/expiry tracking
- document storage
- audit logs
- reports and downloadable evidence

The difference is that these baseline functions are not isolated modules. They feed one readiness picture.

Most competing products solve one slice of the problem:

- fleet tools know what vehicles exist, but not whether the carried equipment and medication make the unit clinically ready today
- checklist tools collect answers, but often do not connect those answers to registers, stock, medication, issue escalation, or manager accountability
- inventory tools count items, but usually do not show whether missing stock or equipment makes a specific vehicle unavailable for the shift
- task tools assign work, but do not become part of a live operational readiness score
- reporting tools show history, but often do not drive real-time decisions during the shift

AcuityOps should be better because it links the operational chain:

1. The company sets up vehicles, areas, staff, equipment, stock, medication, schematics, and checklists.
2. Crews complete daily readiness checks against real register data.
3. The Readiness Engine gives each finding a proper operational weight.
4. Managers see the exact score, blocker, alert, issue, and responsible person.
5. Every task, movement, issue, override, approval, rejection, and report becomes evidence.
6. Senior management sees the whole company; operational managers see only assigned areas.
7. Reports and future analytics are built from structured operational truth, not disconnected notes.

This creates a product that can answer questions other systems usually cannot answer cleanly:

- Which vehicles can safely respond right now?
- Why is this vehicle not ready?
- Is the problem vehicle, equipment, stock, medication, staffing, or unresolved management action?
- Who was assigned to fix it?
- Was the issue resolved, ignored, deleted, or escalated?
- Which base or manager repeatedly has readiness problems?
- Which equipment or stock causes recurring operational weakness?
- What evidence can be produced for audit, inspection, insurance, dispute, or internal review?

The unique selling point is the Readiness Engine. A minor cosmetic issue, a low battery, an expired medication, a missing defibrillator, and an unresolved critical issue should not all affect readiness equally. AcuityOps gives management a configurable scoring model so operational reality is reflected accurately.

The product should therefore be sold as:

`The operational readiness layer for ambulance services.`

Not:

`A digital checklist app.`

The long-term advantage is that once AcuityOps holds the registers, checklist templates, readiness rules, issues, tasks, movements, stock orders, medication batches, documents, and audit history, it becomes difficult to replace. The client is no longer buying a form tool; they are running their operation through AcuityOps.

## Commercial Strategy

The early customer target is medium and moderate-sized private ambulance services, starting in Africa and expanding globally. These services often have real operational complexity but cannot afford slow enterprise implementation projects or expensive fragmented systems.

AcuityOps should win through:

- lower total cost than enterprise EMS platforms
- faster onboarding from existing spreadsheets
- practical operational value from day one
- readiness visibility that owners and senior managers can understand immediately
- upgrade pressure created by reporting, imports, scoring, analytics, and storage scale
- optional paid setup services for companies that want their existing records cleaned up and configured

### Draft Price Ladder

These are working planning numbers, not final public pricing:

- Base: USD 149 to USD 249 per month, or roughly USD 1,500 to USD 2,500 per year, for smaller services that need registers, manual checklist building, daily readiness checks, tasks, issue reporting, basic documents, audit log, PDF evidence, and app-managed storage.
- Pro: USD 399 to USD 699 per month, or roughly USD 4,500 to USD 7,500 per year, for multi-base services that need manager area scoping, readiness dashboard, Readiness Engine, advanced checklist publishing controls, stock ordering, supplier confirmation, stronger reports, exports, and guided import.
- Premium: USD 899 to USD 1,499 per month, or roughly USD 10,000 to USD 18,000 per year, for larger services that need Azure storage, advanced analytics, AI-assisted import, benchmarking, predictive readiness, executive reporting, integrations, and higher retention.

Setup and migration should be charged separately:

- Basic setup: USD 300 to USD 750
- Pro migration from registers/checklists: USD 1,000 to USD 3,000
- Premium onboarding and data cleanup: USD 5,000+ depending on size

The goal is not to be the cheapest checklist tool. The goal is to be the most useful affordable operational readiness layer at a price that private services can justify quickly.

Pricing should be framed by company size and active operational footprint, not pure per-user pricing. Per-user pricing creates friction in ambulance services because shift staff, casual staff, managers, and owners may all need occasional access. A company/vehicle/base tier model is easier to understand and easier to sell.

## Profit Drivers

The strongest revenue drivers should be:

- paid onboarding and spreadsheet cleanup
- Pro subscription as the main product
- Premium storage and analytics for larger clients
- optional AI-assisted import as a premium migration tool
- QR/barcode scanning as a Premium operational-control feature
- supplier workflow and stock intelligence as Pro/Premium value
- executive and compliance reporting for senior management and owners
- future benchmarking once enough anonymised operational data exists

The product becomes more defensible as more workflows connect to the same operational truth: registers, checklists, readiness rules, issues, tasks, stock, medication, documents, and audit evidence.

## Tier System

### Base: Replaces Paper

Base must be genuinely useful to a smaller EMS provider without needing Azure storage or AI. It should give them a secure company workspace, reduce typing mistakes, and create a dependable operational record.

Included:

- company workspace link and company-level access code
- individual staff, operational manager, senior manager, and company owner access
- vehicle register with registration number, callsign, type, schematic type, next service date, and base/location
- equipment register with name, model/type, S/N / asset ID, next service date, battery requirement, status, and location
- staff register and staff document storage
- stock register with item, quantity, batch number, location, and last movement stamp
- medication register with name, schedule, quantity, batch number, expiry date, location, and last allocation stamp
- daily vehicle readiness check with linked onboard equipment check
- equipment checklist rows for multiple items per vehicle
- manual checklist builder for daily readiness, vehicle sections, equipment sections, rows, columns, items, and subitems
- checklist register with saved templates by vehicle type and function
- publish one active checklist per vehicle type/function
- editable auto-filled vehicle and equipment fields from the register
- local 12-hour draft save for interrupted checks
- issue reporting to selected managers
- assigned issue inbox with resolve/delete behavior
- basic task sending and task feedback
- asset movement/reallocation for vehicles, equipment, stock, and medication
- operational areas/bases setup
- audit log
- basic operational reports
- manual upload and storage of existing registers/checklists for reference
- app-managed storage with practical quotas, without Azure Blob Storage

Upgrade pressure from Base:

- manual checklist and register setup are useful but slower
- limited reporting depth
- no advanced import mapping
- no automated analytics or prediction

### Pro: Controls Operations

Pro is the main operational product. It should suit services with multiple bases, multiple managers, active stock movement, and a real need for daily readiness visibility.

Included:

- everything in Base
- operational manager area/region assignment
- senior company-wide dashboard
- operational manager dashboard scoped to assigned areas
- dynamic shift readiness score with red-to-green status wheel
- Readiness Engine with configurable scoring rules, severity dropdowns, percentage impacts, hard blockers, and source-variable lists
- readiness logic using completed checks, unavailable vehicles, open issues, equipment operational status, battery status, service dates, stock/medication availability, critical warnings, and hard blockers
- senior approval workflow for operational manager scoring-change requests
- separate senior controls for "Same as previous shift" on vehicle inspection and equipment checks
- advanced checklist publishing controls, including area-specific publishing and stronger version review
- checklist templates by vehicle type, such as ambulance, ICU ambulance, response vehicle, and rescue vehicle, with deeper management controls
- vehicle schematic library by category, make/model, and vehicle type
- manager ability to do vehicle checks from their own profiles
- stock order workflow from app to selected supplier
- senior approval for operational managers placing orders
- supplier confirmation capture with item, quantity, batch number, and expiry date
- senior or authorised manager entry into stock register
- stock allocation by item, batch, quantity, and location
- equipment service date update workflow from register or equipment service page
- readiness risk reports by vehicle, base, asset, issue, stock, medication, expiry, and recurring failure
- guided upload workflow for existing registers and checklists
- column mapping, duplicate detection, validation, and senior approval before importing data
- searchable reports by date range, base/region, vehicle, staff, issue, task, asset, stock, medication, expiry, and status
- PDF and Excel report exports
- stronger audit and accountability views
- larger app-managed storage allowance than Base

Upgrade pressure from Pro:

- data is organised and reportable, but not yet predictive
- imports still need human approval and mapping
- no advanced AI migration support
- no high-scale Azure storage tier by default

### Premium: Intelligence And Scale

Premium is for larger services, multi-region operations, franchises, national clients, and companies that want analytics, AI-assisted setup, and scalable cloud storage.

Included:

- everything in Pro
- Azure SQL production database option
- Azure Blob Storage for client files, staff documents, uploaded registers, photos, and report evidence
- higher storage limits and retention controls
- AI-assisted import of existing checklists and registers
- automatic recognition of spreadsheet structure, field types, duplicate rows, and likely mappings
- automatic generation of draft checklist templates from uploaded files
- confidence scores and senior approval before publishing imported records
- recurring issue detection by vehicle, item, base, staff member, or supplier
- predictive stock usage and reorder suggestions
- medication and stock expiry forecasting
- service-date forecasting for vehicles and equipment
- readiness trend analysis by base, region, vehicle class, and shift
- anonymised benchmarking against similar services once enough customers exist
- QR/barcode scanning for vehicle, equipment, stock, medication, and movement confirmation
- scheduled executive reports
- compliance packs for audits and inspections
- advanced notification rules
- API/integration options for suppliers, HR systems, finance systems, and external dashboards
- multi-company or group-level reporting for larger clients

Premium should feel like operational intelligence, not just storage and forms.

## Implementation Roadmap

### Phase 1: Usable Operational Core

Goal: a test company can use the app end to end for daily readiness and basic management.

Required:

- company workspace flow
- role login
- registers for vehicles, equipment, stock, medication, and staff
- daily vehicle plus carried-equipment readiness check
- 12-hour draft save
- same-as-previous-shift controls
- issue reporting and assigned issue workflow
- tasks and feedback
- audit log
- checklist PDF evidence
- readiness dashboard with area scoping

### Phase 2: Readiness Engine

Goal: the dashboard score becomes explainable and configurable.

Required:

- readiness rule database
- default AcuityOps scoring rules
- spreadsheet-style Readiness Engine page under Master Setup
- auto-populated suggested rules from vehicles, equipment, stock, medication, issues, and checklist completion
- senior edit, draft, publish, archive, and restore controls
- operational manager scoring-change request workflow
- active published rules connected to dashboard scoring
- source-variable drilldowns for every score-impacting item

### Phase 3: Operational Control

Goal: managers can manage the daily operational burden from inside AcuityOps.

Required:

- stock order workflow
- supplier email/order request
- supplier confirmation capture
- stock and medication batch/expiry allocation
- equipment service update workflow
- movement/reallocation across all assets
- readiness risk reporting
- Excel/PDF exports
- stronger manager accountability views

### Phase 4: Import And Migration

Goal: clients can move from existing spreadsheets and paper checklists into live records.

Required:

- upload storage and preview
- register import wizard
- checklist import wizard
- column mapping
- duplicate detection
- validation warnings
- senior approval before publishing imported data
- import audit history

### Phase 5: Premium Intelligence

Goal: AcuityOps becomes a scalable intelligence product.

Required:

- Azure SQL and Azure Blob Storage support
- AI-assisted checklist/register parsing
- predictive stock and medication expiry forecasting
- equipment and vehicle service forecasting
- recurring issue detection
- QR/barcode scanning
- scheduled executive reports
- compliance packs
- anonymised benchmarking
- API/integration layer

## Feature Ownership By Tier

- Onboarding link and company access: Base
- Personal role login and access separation: Base
- Vehicle, equipment, stock, medication, and staff registers: Base
- Staff files and personal documents: Base
- Daily vehicle plus equipment readiness: Base
- Draft save for interrupted checks: Base
- Manual checklist builder and checklist register: Base
- Same as previous shift: Base when allowed, with Pro adding stronger manager controls and reporting
- Vehicle schematics: Base for selected schematic type, Pro for full schematic library management
- Operational areas/bases: Base
- Manager area scoping: Pro
- Readiness dashboard: Pro
- Asset movement/reallocation: Base
- Tasking and task feedback: Base
- Issue reporting and assigned issue workflow: Base
- Manager issue pool and senior oversight: Pro
- Readiness Engine default scoring: Pro
- Operational manager scoring-change requests: Pro
- Stock ordering and supplier confirmation: Pro
- Stock and medication batch/expiry reporting: Pro
- Service date update workflow: Pro
- Audit log: Base
- Operational reports: Base for simple views, Pro for filters and exports, Premium for analytics
- Checklist/register uploads: Base stores and previews, Pro imports through guided mapping, Premium uses AI assistance
- QR/barcode scanning: Premium
- Benchmarking and predictive readiness intelligence: Premium
- Azure SQL and Azure Blob Storage: Premium

## Current Cleanup Lock

Before new features are added, AcuityOps needs a controlled UI and workflow cleanup pass. The objective is to make the app feel simple, consistent, and reliable across Staff, Operational Management, Senior Management, and Company Owner access.

Work through this list in order and do not skip ahead:

1. Senior Home navigation and Page Index cleanup across all access levels.
2. Checklist Management workflow cleanup.
3. Daily Vehicle & Equipment Check workflow cleanup.
4. Vehicle Register and Equipment Register cleanup.
5. Stock Register and Medication Register cleanup.
6. Readiness Dashboard and Readiness Engine cleanup.
7. Audit logging and permission/access cleanup.

Cleanup rule: every shared function must behave consistently across all relevant access levels, while still respecting each role's permissions.

## Readiness Engine Build Priority

The Readiness Engine slice has been started. Further engine expansion should wait until the current cleanup lock is completed, because the dashboard and scoring pages need to sit inside a clean, predictable navigation flow.

1. Add `Readiness Engine` under Master Setup.
2. Create readiness rule tables and default AcuityOps scoring seed data.
3. Auto-populate suggested rule rows from vehicles, equipment, stock, medication, issue reports, and checklist completion.
4. Add senior-management edit/publish controls.
5. Add operational-manager scoring-change requests.
6. Connect the readiness dashboard score to the active published rules.
7. Keep the existing metric pages as source-variable drilldowns for score explanations.

## First Readiness Engine Build Slice

Do not try to build the full intelligence layer in one pass. The first slice should be:

- `ReadinessEngineRules` table
- `ReadinessEngineVersions` table
- `ReadinessScoringChangeRequests` table
- default seed rules for common vehicle, equipment, stock, medication, issue, and checklist-completion triggers
- Master Setup button: `Readiness Engine`
- senior view: spreadsheet-style rule editor
- ops manager view: read-only rules plus `Request scoring change`
- publish button for senior management
- dashboard scoring service that applies active rules to the current 12-hour shift

The first version can calculate from existing captured fields and expand later as checklist templates become more structured.
