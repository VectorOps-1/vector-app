# AcuityOps First Demo Script

Status: Active Block 1 staging demo guide

Created: 2026-07-04

This script is for the current Azure staging demo only. It must not be used to promise unfinished Base, Pro, Premium, AI, import, billing, or compliance capabilities.

## Demo Link

Open:

`https://app-acuityops-stg-za-001.azurewebsites.net/CompanyLogin/acuityops-workspace`

Do not start the demo from localhost. Do not start from the root page unless showing that AcuityOps requires a company workspace link before role login.

## Login Details

Company access code:

`ACUITY-STAGING-2026`

Senior Management login:

- Email: `senior@test.local`
- Password: `prototype`

Other staging accounts exist for verification, but the first demo should use Senior Management unless the demo specifically needs staff or operational-manager comparison.

## Opening Narrative

Use this framing:

> AcuityOps is being built as an EMS operations platform for private ambulance and response services. The staging environment currently shows the Base operating model: company access, role login, setup direction, checklist management as the source of truth, a deliberately entered demo vehicle/checklist flow, submitted checklist evidence, readiness visibility, and operational reporting without hidden seed/fallback data.

Avoid saying that AI import, DOH audit mode, or predictive analytics are already finished.

## Click Path

1. Open the staging workspace link.
2. Enter the company access code.
3. Select `Senior Management`.
4. Sign in with the senior login details.
5. Land on Home.

Expected Home signals:

- Signed in as Senior Management.
- `Checklist Management` is visible.
- `Operational Reports` is visible.
- `Daily Vehicle & Equipment Check` is visible.
- `Master Setup` is visible.

## What To Show

### 1. Company Workspace Gate

Show:

- The workspace link is required.
- The company access code comes before individual role login.
- Staff and manager roles are not exposed before company authentication.

Message:

> A real client enters through their own company workspace, then selects their role.

### 2. Senior Home

Show:

- Senior Management view.
- First operation setup panel.
- Operational work actions.
- Management oversight modules.
- System setup actions.

Message:

> Senior management controls setup, registers, checklists, reports, access, and operational oversight.

### 3. Checklist Management

Click:

- `Checklist Management`

Expected:

- Checklist Register opens.
- Staging may show the deliberately created demo checklist if the demo dataset is present.
- A live checklist should not appear unless it exists in Checklist Management and is published to a target.

Message:

> The register is the source of truth. A live checklist should not exist unless it is saved and published here.

### 4. Daily Vehicle & Equipment Check

Click:

- Return Home.
- Open `Daily Vehicle & Equipment Check`.

Expected:

- Staging may show the deliberately created demo vehicle `DEM-101 / A01` if the demo dataset is present.
- Selecting the demo vehicle should load only the register-published demo checklist.
- It must not show fixed-form seed checklists.
- It must not show hidden default templates.

Message:

> Daily checks only become available when real vehicles and assigned checklists exist.

### 5. Operational Reports

Click:

- Return Home.
- Open `Operational Reports`.

Expected:

- Staging may show deliberately submitted demo checks.
- Current verified staging status: Operations Reports shows submitted checklist activity for the demo date range.

Message:

> Reports are driven by real activity, not fake seed records.

## Current Clean-Tenant Empty States

These were expected in the initial clean tenant. Current staging now contains deliberate demo records created through approved app/UI workflows:

- Demo vehicle: `DEM-101 / A01`.
- Demo submitted checklist report: `ChecklistReportDetail?id=3`.
- Checklist Reports shows dated report groups and the submitted report row.
- The submitted report detail opens.
- PDF download is available and verified to return `application/pdf`.
- Checklist Report Detail renders the checklist template/version correctly.
- Readiness Dashboard reconciles with submitted demo evidence and shows `1 Daily checks complete` for the verified demo flow.

These records are demo tenant data, not seed data. They must remain removable through normal tenant/data management flows and must never be recreated by startup, fallback logic, or product code.

## What Not To Demo Yet

Do not demo these as working product capabilities yet:

- Excel register import.
- Excel checklist import and column matching.
- AI-assisted import.
- AI predictive reports.
- South African DOH Annual Inspection Mode.
- SOP/CPG ingestion.
- Billing and subscription state changes.
- Production tenant onboarding.
- Production support/admin workflows.

These remain roadmap capabilities and must be implemented in dedicated approved phases.

## Known First-Demo Limits

- The demo tenant now contains deliberate demo records entered through approved workflows. It is no longer a pure empty-state tenant.
- The demo currently proves the access path, senior navigation, checklist source-of-truth behavior, one daily-check evidence path, report detail, PDF download availability, readiness visibility, and operational report counts.
- PDF download returns a valid PDF for the verified demo report, but PDF layout/content polish is not yet a completed product phase.
- A richer sales demo still needs a controlled demo-data script and a polished report/PDF pass.

## Next Demo Blockers To Fix

Recommended next blockers, in order:

1. Push the latest verified staging source commits and redeploy through the GitHub workflow so staging matches committed source.
2. Polish PDF output layout/content in a dedicated report/PDF batch; do not expand Block 1 for PDF redesign.
3. Create a controlled demo-data script if a richer sales demo is needed.

## Demo Safety Rules

- Do not create product records during a live demo unless the purpose is to show setup.
- Do not imply clean empty states are failures.
- Do not show unfinished Pro/Premium pages as if complete.
- Do not use local development as demo truth.
- Do not manually patch database records during a demo.
