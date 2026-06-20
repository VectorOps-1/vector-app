# AcuityOps Spec Execution Tracker

This tracker is the execution gate for `docs/specs/acuityops-master-build-spec.md`.

The master spec remains the product and architecture authority. This tracker controls sequence, authorization, status, verification, and anti-deviation behavior. Product source code, database files, runtime files, generated files, and app behavior must not be changed unless the user authorizes one exact tracker row.

## Mandatory Anti-Deviation Rules

1. Do not work from memory. Read the relevant tracker row and master spec section before acting.
2. Do not invent step numbers. If the user asks for `Step 10` and the current tracker does not contain that step in the active phase, stop and report the mismatch.
3. Do not combine tracker rows. One user instruction may complete one tracker row unless the user explicitly authorizes a named batch of tracker rows.
4. Do not edit product source code during spec, tracker, audit, or planning rows.
5. Do not touch database files, runtime files, generated files, app startup, or app process state unless the active tracker row explicitly permits it.
6. Do not treat prior untracked work as complete. Prior work must be reconciled against the tracker before any row is marked `Done`.
7. Do not mark a row `Done` without the required verification evidence.
8. Do not continue to the next row automatically. Stop, report status, and provide the exact next instruction for the user to give.
9. If new work is discovered, add it as a proposed tracker row or report it as a required tracker change. Do not implement it silently.
10. If a row conflicts with the master spec, stop and request a spec/tracker correction before implementation.

## Allowed Status Values

- `Not started`
- `In progress`
- `Blocked`
- `Done`

## Required Pre-Flight Before Any Product Work

Before any product source, database, runtime, or generated-file work, Codex must state:

1. Active tracker row ID and exact title.
2. Matching master spec section.
3. Files allowed to change.
4. Files forbidden to change.
5. Whether database access is read-only, write-enabled, or forbidden.
6. Whether app startup/browser verification is allowed.
7. Required verification commands or checks.
8. Stop condition.

If any answer is unclear, the row is `Blocked`.

## Required Post-Step Report

After each authorized row, Codex must report:

1. Tracker row ID and title.
2. Final row status.
3. Files changed.
4. Verification completed.
5. Verification not run and reason.
6. Remaining risk.
7. App-readiness percentage where `100%` means a high-level production SaaS app running with real clients.
8. Exact next recommended instruction.

## Current Active Row

No implementation row is active.

The next implementation instruction must name one row from the tracker below.

## Phase 1: Workspace And Data Hygiene

| ID | Status | Master Spec Step | Authorized Scope | Verification Required | Notes |
| --- | --- | --- | --- | --- | --- |
| P1-01 | Not started | Run full git/worktree audit and separate source files from runtime, generated, and database files. | Read-only audit only. | Git/worktree report. | Prior work must be reconciled before marking done. |
| P1-02 | Not started | Update `.gitignore` so SQLite files, backups, logs, `bin/`, `obj/`, artifacts, uploads, and runtime clutter do not appear in git status. | Hygiene docs/config only. | Git status before/after. | Do not delete database/runtime files. |
| P1-03 | Not started | Remove tracked generated files from git index without deleting local runtime files. | Git index cleanup only. | Git status before/after. | No local file deletion unless separately authorized. |
| P1-04 | Not started | Create a clean source checkpoint commit for hygiene only. | Commit hygiene slice only. | Commit hash and clean/expected status. | No feature code in hygiene commit. |
| P1-05 | Not started | Stop all normal startup/sample seed mutation. | Source changes limited to startup/seed paths. | Build and source scan. | No database cleanup in this row. |
| P1-06 | Not started | Remove schema/data repair calls from normal page requests. | Source changes limited to request-time repair paths. | Build and route/source scan. | No database cleanup in this row. |
| P1-07 | Not started | Preserve current login accounts, but remove stale seeded company, register, checklist, schematic-assignment, and readiness artifacts from the active dev database. | Database cleanup only after backup. | Backup evidence, cleanup report, login verification. | This row explicitly permits database write only when authorized. |
| P1-08 | Not started | Verify the app starts cleanly without recreating old seed data. | Verification only unless failures require a new tracker row. | App start, browser verification, seed/fallback scan. | Do not fix discovered defects inside this row. |

## Phase Gates Requiring Child-Step Expansion Before Implementation

The rows below are locked phase gates. Before implementation starts inside any of these phases, Codex must expand the phase into child tracker rows from the master spec and wait for user authorization.

| ID | Status | Master Spec Section | Gate Requirement |
| --- | --- | --- | --- |
| P2-GATE | Not started | Phase 2: Company And Tenant Source Of Truth | Expand steps 9-15 into tracker rows before implementation. |
| P2A-GATE | Not started | Phase 2A: New Client Setup Wizard | Expand steps 15A-15R into tracker rows before implementation. |
| P3-GATE | Not started | Phase 3: Access And Permissions | Expand steps 16-23 into tracker rows before implementation. |
| P4-GATE | Not started | Phase 4: Navigation And Route Cleanup | Expand steps 24-30 into tracker rows before implementation. |
| P5-GATE | Not started | Phase 5: Checklist Source Of Truth | Expand steps 31-38 into tracker rows before implementation. |
| P6-GATE | Not started | Phase 6: Checklist Builder And Publishing | Expand steps 39-47 into tracker rows before implementation. |
| P7-GATE | Not started | Phase 7: Live Daily Checks | Expand steps 48-55 into tracker rows before implementation. |
| P8-GATE | Not started | Phase 8: PDF Evidence | Expand steps 56-61 into tracker rows before implementation. |
| P9-GATE | Not started | Phase 9: Registers And Assets | Expand steps 62-70 into tracker rows before implementation. |
| P10-GATE | Not started | Phase 10: Stock And Medication Flow | Expand steps 71 and onward from the master spec before implementation. |
| P11-GATE | Not started | Phase 11: Schematics | Expand schematic assignment and live schematic behavior into tracker rows before implementation. |
| P11A-GATE | Not started | Phase 11A: Global Vehicle Schematic Library Completion | Expand each schematic asset batch into tracker rows before generation or app integration. |
| P12-GATE | Not started | Phase 12: Readiness And Reports | Expand readiness/reporting rows before implementation. |
| P12B-GATE | Not started | Phase 12B: South African EMS Audit Compliance Mode | Expand legal/source-pack/review rows before implementation. |
| P12C-GATE | Not started | Phase 12C: South African Regulatory Source Pack And Multi-Country Compliance Architecture | Expand source compilation and jurisdiction model rows before implementation. |
| P12D-GATE | Not started | Phase 12D: SMS And Email Notification Engine | Expand provider, template, consent, audit, and retry rows before implementation. |
| P13-GATE | Not started | Phase 13: AI Import | Expand AI import, mapping, validation, and review rows before implementation. |
| P13A-GATE | Not started | Phase 13A: Clinical Guideline And SOP Knowledge UI | Expand document ingestion, source citation, UI, permission, and tier rows before implementation. |
| P14-GATE | Not started | Phase 14: AI Predictive Analysis | Expand prediction inputs, outputs, safety, reports, and validation rows before implementation. |
| P15-GATE | Not started | Phase 15: SaaS Packaging | Expand tenant packaging rows before implementation. |
| P15A-GATE | Not started | Phase 15A: Commercial Downgrade, Cancellation, And Tier Data Rules | Expand downgrade/cancellation behavior rows before implementation. |
| P15B-GATE | Not started | Phase 15B: Exact Tier Feature Matrix And Data-State Rules | Expand tier lock/read-only/export/delete behavior rows before implementation. |
| P15C-GATE | Not started | Phase 15C: Production Billing, Payment Provider, Tax, And Invoice Architecture | Expand billing provider and subscription state rows before implementation. |
| P16-GATE | Not started | Phase 16: Azure And Production | Expand Azure production architecture rows before implementation. |
| P16A-GATE | Not started | Phase 16A: Release Isolation And Client-Specific Publishing | Expand environment, tenant release, and deployment rows before implementation. |
| P16B-GATE | Not started | Phase 16B: Mandatory Security And Client Data Protection Sweeps | Expand security, encryption, backup, and access review rows before implementation. |
| P16C-GATE | Not started | Phase 16C: Mandatory Legal, Compliance, Evidence, And IP Review | Expand legal review and evidence retention rows before implementation. |
| P16D-GATE | Not started | Phase 16D: Client Data Portability, Export, And Offboarding | Expand export/delete/offboarding rows before implementation. |
| P16E-GATE | Not started | Phase 16E: Tenant Isolation And Multi-Client Provisioning Decision Record | Expand tenant ID, database, storage, domain, and backup rows before implementation. |
| P16F-GATE | Not started | Phase 16F: Production Observability And Incident Response Decision Record | Expand logs, metrics, alerts, uptime, support, and incident review rows before implementation. |
| P16G-GATE | Not started | Phase 16G: Production Support And Client-Success Operating Model | Expand onboarding, helpdesk, SLA, training, documentation, and feedback rows before implementation. |
| P16H-GATE | Not started | Phase 16H: Production Sales, Demo, And Trial-Conversion Operating Model | Expand sales/demo/trial conversion controls before implementation. |
| P16I-GATE | Not started | Phase 16I: Production Website And Marketing-Content Truth Control | Expand website claims, pricing, demo request, SEO, analytics, and legal review rows before implementation. |
| P17-GATE | Not started | Phase 17: Website And Launch | Expand website execution and launch rows after Phase 16I truth-control is done. |
| P18-GATE | Not started | Phase 18: Final Release Gate | Expand final audit, release, tagging, and production verification rows before implementation. |

## Tracker Update Rules

1. Add child rows before entering a phase gate.
2. Keep existing IDs stable after they are used.
3. If a row is split, leave the original row as a gate and add child rows below it.
4. If a row is obsolete, mark it `Blocked` with the reason. Do not delete it unless the user explicitly authorizes tracker cleanup.
5. Every source-changing row must name a verification method before work starts.
