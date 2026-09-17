# AcuityOps Controlled Hibernation And Restart Runbook

Status: Recovery package verified; controlled shutdown authorized

Recovery ID: `20260917-154643`

This runbook is the operational authority for pausing and restoring the
AcuityOps Azure staging environment. Hibernation preserves the product and its
data; it does not reset roadmap progress or abandon the product.

## Verified Recovery Package

Source:

- repository: `https://github.com/VectorOps-1/vector-app`
- branch: `main`
- recovery tag: `acuityops-hibernation-20260917-154643`
- source checkpoint before hibernation: commit recorded by the tag

Database:

- BACPAC: `database-archives/hibernation/20260917-154643/sqldb-acuityops-stg-20260917-154643.bacpac`
- BACPAC size: `80,228` bytes
- BACPAC content MD5: `xqkMuvAt80+vceSodFiH4A==`
- disposable restore: completed successfully, compared with the active database, and deleted
- active/restored migration count: `32`
- active/restored migration signature SHA-256: `b36859194c479cab55b53426c18e8b03777e60ef2364a06a85f3495c0f1d11c9`

Storage:

- private container: `hibernation-archive`
- snapshot: `hibernation/20260917-154643/storage/acuityops-storage-20260917-154643.zip`
- snapshot SHA-256: `6c6b676201b7dbf337d86e400c5dd51a3a027bfa96f72c4474f14e1955b28be2`
- file manifest: `hibernation/20260917-154643/storage/storage-sha256-manifest.json`
- file manifest SHA-256: `bacf6d84dd18cc3931fadbc529f7ec17132f097c854d5df2fb40b6f8c3f5eeb9`
- source container totals at snapshot time: 16 blobs and 499,910 bytes in
  `database-archives`; zero blobs in `generated-reports`, `import-files`, and
  `tenant-uploads`

Configuration:

- no-secret manifest: `hibernation/20260917-154643/config/azure-github-recovery-manifest.no-secrets.json`
- manifest SHA-256: `bbd09b46586e74046c71ff4a36ffc69860d68459e9755dfd36dd5e356aed0bb7`
- secrets remain in `kv-acuityops-stg-001`; the manifest records secret names,
  never values

## Retained During Hibernation

- GitHub repository, commit history, recovery tag, specifications, migrations,
  and disabled workflow definitions
- Azure resource group `rg-acuityops-stg-za-001`
- storage account `stacuityopsstg001`, including all original containers and
  the private hibernation archive
- Key Vault `kv-acuityops-stg-001`
- SQL logical server `sql-acuityops-stg-za-001` without a billable user database
- Azure OpenAI account `oai-acuityops-stg-za-001` without a model deployment
- deployment identity `id-acuityops-gh-stg-za-001` and its GitHub OIDC
  federation
- Azure budget and billing alerts

## Removed After Recovery Gates Pass

- App Service `app-acuityops-stg-za-001`
- App Service plan `asp-acuityops-stg-za-001`
- active SQL database `sqldb-acuityops-stg`
- Azure OpenAI deployment `ai-structured-low-cost`
- Document Intelligence account `di-acuityops-stg-za-001`
- Application Insights `appi-acuityops-stg-za-001`
- Log Analytics workspace `log-acuityops-stg-za-001`

These resources contain no sole copy of retained product or tenant evidence.
Their recreation settings and role assignments are in the no-secret manifest.

## Restart Sequence

1. Check out the hibernation recovery tag and verify the tag commit against
   GitHub.
2. Download the no-secret manifest, storage hash manifest, and BACPAC metadata.
   Recalculate SHA-256 hashes before restoration.
3. Confirm that Key Vault still contains the recorded SQL and temporary-login
   secret names. Rotate credentials if policy or elapsed time requires it.
4. Recreate one Linux B1 App Service plan and one .NET 8 Linux App Service in
   South Africa North. Use the former names when available; otherwise update
   GitHub deployment settings and the workspace URL deliberately.
5. Recreate Log Analytics and Application Insights only when work is resuming,
   with the previous 30-day retention and `0.1 GB/day` ingestion cap.
6. Create one empty Basic Azure SQL database on the retained logical server.
   Import the verified BACPAC and compare the restored migration signature with
   the value in this runbook before deployment.
7. Restore App Service settings from the no-secret manifest. Obtain secret
   values from Key Vault or reissue them; never copy placeholder values from
   the manifest.
8. Enable a system-assigned identity on the recreated web app and restore only
   the recorded least-privilege roles for OpenAI, Document Intelligence (when
   enabled), and the Premium AI queue.
9. If Premium AI is required, recreate the approved Global Standard model
   deployment and Document Intelligence account, then restore the endpoints.
   Otherwise leave Premium AI disabled.
10. Re-enable the GitHub `CI` workflow. Re-enable `Deploy Azure Staging` only
    after the App Service exists and the retained OIDC identity has the required
    App Service-scoped role.
11. Deploy exclusively through the GitHub Linux staging workflow.
12. Verify static assets, workspace login, all role logins, Home gating,
    tenant isolation, database-backed routes, imports, report detail, PDF
    evidence, and one backup/restore check before accepting resumed work.

## Rollback And Safety

- Never overwrite the retained BACPAC or hibernation archive.
- Restore into a new database name first. Promote it only after schema and
  tenant checks pass.
- Do not recreate seed or fallback tenants, users, registers, checklists, or
  schematics.
- Do not enable Premium AI until privacy, cost, human-review, and deterministic
  import boundaries pass.
- The former `azurewebsites.net` hostname is expected only if the same App
  Service name remains available. A future custom domain should be treated as
  the stable public address.

## Expected Hibernated Cost

The retained set has no App Service plan, user SQL database, AI model
deployment, Document Intelligence processing, Application Insights ingestion,
or Log Analytics ingestion. Expected steady-state Azure cost is approximately
`USD 0-2/month`, with a conservative ceiling below `USD 5/month` for retained
Blob, Key Vault operations, and incidental storage transactions. The first
invoice after hibernation can be higher because it includes charges accrued
before deletion and retained-log or billing-reporting lag.
