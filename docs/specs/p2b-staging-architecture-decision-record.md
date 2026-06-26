# ADR-P2B-15U: Staging Architecture Decision Record

## Status

Approved for Phase 2B staging planning only.

This decision record does not create Azure resources, configure secrets, deploy the app, change product code, or approve production launch.

## Purpose

Phase 2B exists because local-only verification is blocking efficient progress. The staging environment must provide a stable, cloud-hosted verification path where committed source can be built, deployed, migrated, logged, smoke-tested, and rolled back without depending on the founder's active Windows desktop session.

This is a staging architecture, not a production architecture. Production security, billing, public launch, AI, SMS/email, compliance, support, and Enterprise hardening remain later phases.

## Scope

Included:

- GitHub source-of-truth and CI build gate.
- One Azure staging web app host.
- One managed staging database.
- One staging Blob Storage account/container strategy.
- Key Vault for staging secrets.
- Application Insights and Log Analytics for staging telemetry.
- Environment names and deployment flow.
- Migration, backup, restore, and rollback outline.
- Cost-control rules for staging.
- Provider/resource list for the next provisioning row.

Excluded:

- Production resources.
- Demo, trial, or Enterprise environments.
- AI, SMS, email, billing, CDN, WAF, autoscale, advanced security add-ons, or public launch infrastructure.
- Any client data import or paid-client onboarding.
- Any app source-code change.

## Environment Names

- `Local`: developer machine, SQLite, local uploads, and local-only experiments.
- `Staging`: shared cloud verification environment created in Phase 2B.
- `Production`: future real-client environment governed by later security, billing, support, and legal gates.

## Selected Staging Architecture

### Source Control And CI

- Provider: GitHub repository `VectorOps-1/vector-app`.
- Branch model for staging work: local work is committed, pushed to a `codex/...` branch, reviewed through a pull request, merged into `main`, then deployed to staging from committed source.
- CI provider: GitHub Actions.
- CI behavior: restore, build, and run available verification checks before any deployment step.
- Deployment must not run if CI fails.

### App Host

- Selected host for initial staging: Azure App Service.
- Runtime target: ASP.NET Core / .NET 8.
- Reason: simplest managed Azure host for an ASP.NET prototype, lower operational complexity than containers, direct GitHub Actions deployment support, deployment-slot path available later.
- Preferred operating system for staging: Linux App Service unless a Windows-only dependency is discovered.

### Managed Database

- Selected staging database: Azure SQL Database.
- Reason: supported by the current app stack through Entity Framework Core SQL Server provider, managed backups, point-in-time restore path, familiar migration model, and a cleaner path toward production than SQLite.
- SQLite must not be used as the staging source of truth.
- Azure PostgreSQL is deferred because the current app already carries SQL Server provider support and switching database provider now would add migration risk without immediate staging benefit.

### File Storage

- Selected storage: Azure Blob Storage.
- Required usage: uploaded logos, staff documents, register import files, generated reports, future PDFs, future AI import files, and tenant-owned evidence artifacts.
- Access model: private containers only. No public blob containers for tenant-owned files.
- Tenant path model for staging: tenant-owned files must be stored under tenant-aware prefixes or containers. Product-owned schematic/library assets must remain separate from tenant-owned assignments.

### Secrets

- Selected secrets provider: Azure Key Vault.
- Secrets that must be stored outside source control:
  - staging database connection string,
  - storage connection string or managed-identity access settings,
  - application secrets/signing keys,
  - future AI/SMS/email/billing provider secrets when those later phases are authorized.
- No secrets may be committed to Git, appsettings files, database rows, logs, or workflow files.

### Observability

- Selected telemetry stack: Application Insights plus Log Analytics.
- Required staging visibility:
  - app startup failures,
  - request failures,
  - dependency/database/storage failures,
  - deployment failures,
  - migration failures,
  - unhandled exceptions,
  - smoke-test failures.
- Logs must be tenant-safe. Sensitive personal, clinical, billing, and credential values must not be written to logs.

### Region

- Preferred staging region: South Africa North if all selected staging services and low-cost tiers are available at provisioning time.
- Fallback region: West Europe only if South Africa North blocks a required low-cost staging resource or deployment capability.
- The final region must be confirmed during `P2B-15V` before resources are created.

## Rejected Alternatives

### Continue Local-Only

Rejected because local Windows + SQLite + OneDrive + manual process management is already slowing verification and causing repeated startup/access friction.

### Azure Container Apps For Initial Staging

Deferred because containers add image build, registry, networking, and deployment complexity before the app needs container-specific behavior.

### Azure PostgreSQL For Initial Staging

Deferred because the current app is already aligned to SQL Server provider support. PostgreSQL can be reconsidered only through a later database decision record.

### SQLite In Staging

Rejected. SQLite is suitable for local prototyping only and is not the staging source of truth for a multi-tenant SaaS product.

### Source-Folder Or App-Local File Storage

Rejected. Tenant files must not be written into source folders or app-local deployment folders in staging.

### Committed Secrets

Rejected. All staging secrets must be stored outside Git and supplied through Azure configuration or Key Vault.

### Direct Deployment From A Local Desktop

Rejected. Staging must deploy from committed source through CI/CD, not from an uncommitted local machine state.

## Deployment Flow

1. Local implementation.
2. Local targeted build verification.
3. Commit intentional changes.
4. Push a `codex/...` branch.
5. Open pull request to `main`.
6. GitHub Actions CI runs restore/build/verifier checks.
7. Merge only after CI passes.
8. Staging deployment runs from committed source.
9. Staging migration step runs with backup/restore controls.
10. Staging smoke verification records evidence in the tracker.

## Migration, Backup, Restore, And Rollback Outline

Detailed commands belong to `P2B-15X`. This row defines the minimum staging rule:

- Before any staging migration, take a managed database backup or confirm point-in-time restore availability.
- Run migrations through an explicit staging migration step, not hidden app startup mutation.
- After migration, verify schema state and app startup.
- If migration breaks staging, roll back the app to the previous working commit and restore the previous staging database state.
- Keep rollback evidence in the tracker.

## Cost-Control Rules

- Create one staging environment only.
- No production, demo, trial, or Enterprise environments in Phase 2B.
- Use the lowest practical non-production SKU/tier that supports the required staging behavior.
- Every Azure resource in `P2B-15V` must have:
  - owner,
  - purpose,
  - environment tag,
  - monthly cost estimate,
  - budget alert coverage,
  - deletion/shutdown rule,
  - reason for existence.
- No paid add-ons are authorized in this decision record.
- Do not enable AI, SMS, email, billing, CDN, WAF, autoscale, premium monitoring, or SIEM in Phase 2B unless a later tracker row explicitly authorizes it.

## Required Resource List For P2B-15V

Provisioning is not authorized by this decision record. The next provisioning row must use this list as its starting point:

- Azure Resource Group for staging.
- Azure App Service Plan for staging.
- Azure App Service for the AcuityOps staging web app.
- Azure SQL Database for staging tenant data.
- Azure SQL logical server if required by Azure SQL Database.
- Azure Storage Account for staging tenant files.
- Azure Blob container or tenant-prefix strategy for staging uploads and generated artifacts.
- Azure Key Vault for staging secrets.
- Application Insights resource.
- Log Analytics workspace.
- Budget alert for staging resources.

## Ownership

- Platform owner: AcuityOps founder until delegated.
- Technical owner: Codex-assisted implementation, with tracker evidence required.
- Cost owner: AcuityOps founder until billing/account ownership changes.
- Approval gate: no Azure resource may be created until `P2B-15V` is explicitly authorized.

## Acceptance Criteria

This decision record is complete when it:

- names the selected host, database, storage, secrets, and telemetry providers,
- defines environment names,
- documents rejected alternatives and reasons,
- defines cost controls,
- documents the deployment flow,
- defines migration/backup/rollback expectations,
- lists resources required for provisioning,
- states that no Azure resource was created by this row,
- is committed separately from product source.

## Follow-Up Rows

- `P2B-15V`: provision the minimal Azure staging resource set after this decision record is approved.
- `P2B-15W`: configure staging app settings and secrets without committing secrets to source.
- `P2B-15X`: define and verify staging database migration, backup, restore, and rollback process.
- `P2B-15Y`: deploy committed source to staging and run the staging smoke suite.
- `P2B-15Z`: record local-vs-staging responsibilities and troubleshooting rules.
