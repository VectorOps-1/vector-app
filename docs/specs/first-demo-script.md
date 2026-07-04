# AcuityOps First Demo Script

Status: Active Block 1 staging demo guide

Created: 2026-07-04

This script is for the current Azure staging demo only. It must not be used to promise unfinished Base, Pro, Premium, AI, PDF, import, billing, or compliance capabilities.

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

> AcuityOps is being built as an EMS operations platform for private ambulance and response services. The staging environment currently shows the clean Base starting point: company access, role login, setup direction, checklist management, operational reports, and empty-state behavior without seed data or fake operational records.

Avoid saying that AI import, PDF evidence packs, DOH audit mode, or predictive analytics are already finished.

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
- Clean staging should show no saved checklist templates unless demo data has deliberately been created later.

Message:

> The register is the source of truth. A live checklist should not exist unless it is saved and published here.

### 4. Daily Vehicle & Equipment Check

Click:

- Return Home.
- Open `Daily Vehicle & Equipment Check`.

Expected:

- Clean staging shows no vehicles available for daily checks.
- It must not show fixed-form seed checklists.
- It must not show hidden default templates.

Message:

> Daily checks only become available when real vehicles and assigned checklists exist.

### 5. Operational Reports

Click:

- Return Home.
- Open `Operational Reports`.

Expected:

- Clean staging shows no operational activity collected yet.

Message:

> Reports are driven by real activity, not fake seed records.

## Current Clean-Tenant Empty States

These are expected in the current demo tenant:

- No vehicles available for daily checks.
- No saved checklist templates.
- No operational activity collected.
- No register-driven daily checks until real vehicle and checklist records exist.

These empty states are a strength for this stage because they prove the app is not silently falling back to seed data.

## What Not To Demo Yet

Do not demo these as working product capabilities yet:

- Real submitted checklist PDFs.
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

- The demo tenant is clean and does not contain operational demo records.
- The demo currently proves the access path, senior navigation, empty-state behavior, and source-of-truth discipline.
- A richer sales demo will require deliberate demo data or a guided setup script in a later approved batch.
- PDF evidence and import workflows are not yet demo-ready.

## Next Demo Blockers To Fix

Recommended next blockers, in order:

1. Create a deliberate, minimal demo dataset plan without seed/fallback behavior.
2. Add one realistic vehicle, one staff profile, and one manually built checklist only through approved app flows or controlled staging data setup.
3. Verify a complete daily check can be performed from a register-published checklist.
4. Verify a submitted checklist report view exists.
5. Implement or defer PDF download depending on Block 1 credit budget.

## Demo Safety Rules

- Do not create product records during a live demo unless the purpose is to show setup.
- Do not imply clean empty states are failures.
- Do not show unfinished Pro/Premium pages as if complete.
- Do not use local development as demo truth.
- Do not manually patch database records during a demo.

