# AcuityOps Staging Runbook

Status: Active Block 1 staging evidence

Created: 2026-07-02

This runbook records the current staging truth for demo-readiness work. It does not replace the recovery roadmap. It exists to prevent repeated rediscovery of links, credentials, route expectations, and deployment commands.

## Authority

- Active roadmap: `docs/specs/acuityops-recovery-roadmap.md`
- Staging architecture decision record: `docs/specs/p2b-staging-architecture-decision-record.md`
- Staging is the current shared verification environment for demo-readiness checks.
- Localhost remains a development workspace only.

## Stable Staging URLs

- Staging app: `https://app-acuityops-stg-za-001.azurewebsites.net`
- Demo workspace login: `https://app-acuityops-stg-za-001.azurewebsites.net/CompanyLogin/acuityops-workspace`
- Public root: `https://app-acuityops-stg-za-001.azurewebsites.net/`

Root `/` must not show Staff, Operational Management, or Senior Management role-login choices before company workspace authentication. Role selection belongs behind successful company workspace login at `/Access`.

## Staging Test Identity

Workspace:

- Workspace slug: `acuityops-workspace`
- Company access code: `ACUITY-STAGING-2026`
- Company display name currently used for staging: `Demo EMS Service`

Role login accounts:

- Staff: `staff@test.local`
- Operational Management: `ops@test.local`
- Senior Management: `senior@test.local`
- Company Owner: `owner@test.local`

These accounts are staging verification accounts only. They must not be treated as product seed data or copied into production.

Passwords are not stored in Git or this runbook. Temporary credentials are issued through the controlled staging identity-reset path, are verified during reset, and require replacement at first login. After login, the Company Owner account is the recovery authority for resetting other staging accounts through **Area / Manager Control > Access Setup**. If every privileged staging credential is unavailable, use the controlled identity-reset utility after confirming Azure SQL point-in-time recovery; do not restore obsolete documented passwords.

## Azure Resource Inventory

- Subscription: Vector Ops Group Azure subscription
- Region: South Africa North
- Resource group: `rg-acuityops-stg-za-001`
- App Service: `app-acuityops-stg-za-001`
- Purpose tag: `phase2b-staging`
- Environment tag: `staging`

Any new Azure resource must be approved in a separate batch. This runbook does not authorize resource creation.

## Deployment Pattern

Current staging deployment is a pre-built Linux App Service zip deployment.

Build/publish pattern:

```powershell
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$out = "artifacts\staging-entry-flow-$stamp"
dotnet publish .\vector-app-local.csproj -c Release -r linux-x64 --self-contained false -o $out
```

Package pattern:

```powershell
$zip = "$out.zip"
Push-Location $out
tar.exe -a -c -f "..\$(Split-Path $zip -Leaf)" *
Pop-Location
```

Do not use PowerShell `Compress-Archive` for Azure staging packages. On 2026-07-12, Windows
`Compress-Archive` packages repeatedly reached Kudu OneDeploy and failed with HTTP 400 during the
generated deployment command. The same published output deployed successfully when packaged with
`tar.exe -a -c -f`, matching the working GitHub/Linux zip behavior more closely.

Deploy pattern:

```powershell
az webapp deploy --resource-group rg-acuityops-stg-za-001 --name app-acuityops-stg-za-001 --src-path $zip --type zip --clean true --restart true
```

Deployment packages under `artifacts/` are runtime artifacts and must not be committed.

## Minimum Demo-Readiness Smoke Routes

Unauthenticated routes:

- `/` returns 200 and explains that the user must open their company workspace link.
- `/` must not expose direct role-login buttons.
- `/CompanyLogin` returns 200 with the valid-workspace-link requirement.
- `/CompanyLogin/acuityops-workspace` returns 200 and shows company login.

Authenticated route flow:

1. Open `/CompanyLogin/acuityops-workspace`.
2. Submit the company access code.
3. Verify `/Access` displays Staff, Operational Management, and Senior Management options.
4. Sign in as the required role.
5. Verify role reaches `/Home`.

Approved normal verification writes:

- Company workspace login audit/session write.
- Role login audit/session write.

Not approved during demo smoke verification:

- Creating registers.
- Creating vehicles.
- Creating staff.
- Creating equipment, stock, or medication.
- Creating checklists.
- Creating readiness rules.
- Creating schematic assignments.
- Editing company product data.

## First-Demo Empty-State Expectations

For a clean staging tenant, first-demo routes must be clear and non-seeded:

- Daily Vehicle & Equipment Check must not show fixed-form seed checklists.
- Checklist Register must not show hidden seed templates.
- Operational Reports must explain that no operational activity has been collected yet.
- Empty states must guide the user toward setup or register/checklist creation without creating records automatically.

## Local Vs Staging Responsibilities

Local PC:

- Source editing.
- Targeted builds.
- Fast pre-commit checks.
- No longer the main demo truth.

Azure staging:

- Stable browser URL.
- Demo-readiness route checks.
- Login and setup-gating checks.
- Checklist/register/report workflow checks once features are ready.

GitHub:

- Committed-source authority.
- CI gate.
- Pull request and rollback trail.

## Rollback And Delete Notes

Deployment rollback:

- Re-deploy the previous known-good package or commit build to the same App Service.
- If a database migration caused the failure, restore the staging database from the approved backup/restore path before re-testing.

Resource delete rule:

- Deleting staging resources requires a separate approved Azure cleanup batch.
- Never delete production resources from a staging batch.

## Cost-Control Rules

- Do not create additional staging environments without explicit approval.
- Do not increase SKU/tier without explicit approval.
- Do not run repeated full smoke suites after a pass unless a later change invalidates the result.
- Prefer targeted route checks for narrow source changes.

## Current Known Demo Entry

Use this for first-demo testing:

`https://app-acuityops-stg-za-001.azurewebsites.net/CompanyLogin/acuityops-workspace`

Use `docs/specs/first-demo-script.md` for the current first-demo click path, talking points, known limits, and features that must not be represented as complete.
