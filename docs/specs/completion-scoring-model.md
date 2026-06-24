# AcuityOps Completion Scoring Model

This file defines the only approved way to report AcuityOps completion percentage.
It exists to prevent informal estimates such as `20%` or `13%` from being used as
project truth.

## Authority

The score is calculated from:

1. `docs/specs/acuityops-master-build-spec.md`
2. `docs/specs/execution-tracker.md`

No chat memory, informal judgement, uncommitted source work, visual impression, or
partially completed implementation may change the score.

## Meaning Of 100 Percent

`100%` means AcuityOps is ready to run as a production SaaS platform with real
clients.

That includes:

- stable source tree and deployment process
- clean tenant isolation
- company setup wizard
- access control and permission enforcement
- register-driven operational workflows
- register-driven daily checks
- checklist builder, publishing, and evidence
- working PDF downloads
- readiness engine and reports
- AI register/checklist import
- AI future analysis reports
- SOP/clinical guideline ingestion
- billing and subscription state handling
- tier enforcement and downgrade/cancellation behavior
- Azure or approved production hosting architecture
- production storage, backups, observability, and incident response
- security sweeps and legal/compliance review
- data export/offboarding
- public website, marketing truth-control, sales/demo/trial model
- final production release gate

## Reporting Rule

Completion percentage may be reported only from reconciled tracker rows.

Every completion/status report after a tracker row must include both:

1. Phase progress for the current phase.
2. Total SaaS readiness progress.

Use this exact display shape:

```text
Current tracker row: <row id> - <status>
Phase progress: <phase name> - <done rows>/<total rows> rows Done (<phase row percent>%), phase score <phase score>/<phase weight>
Total SaaS readiness: <total score>% / 100%
Calculation: <completed group scores> + <current phase score> + <remaining groups score>
```

The report must also state:

- tracker file used
- tracker date if known
- exact rows counted
- exact rows excluded
- whether the tracker is stale or unreconciled

If the tracker is stale, contradictory, or missing required rows, the percentage is
`Blocked`, not estimated.

No arbitrary percentage jumps are allowed. A score may change only when one of
these occurs:

- a tracker row status changes to `Done` through tracker reconciliation
- a tracker row is added or removed through an approved gate expansion
- the scoring weights in this file are changed through a docs-only scoring-model
  update

All row-count and score changes must be visible in the next status report.

## Status Values

Only tracker rows with status `Done` count as completed.

The following statuses count as zero:

- `Not started`
- `In progress`
- `Blocked`
- missing row
- unexpanded gate
- implemented but not reconciled in the tracker
- verified in chat but not recorded in the tracker

No row receives partial credit. If a step is too large for this rule, it must be
split into child tracker rows before work continues.

## Gate Rows

Gate expansion rows are governance controls. They do not directly increase product
completion unless the scoring table below explicitly assigns weight to the
expanded child rows.

A phase with an unexpanded gate scores `0` for that phase.

## Formula

For each scoring group:

`group score = group weight x (Done required rows / total required rows)`

Total completion:

`total score = sum(group scores)`

The maximum total is `100`.

If a group has a completed closure row that explicitly states the phase may close,
the group may score its full weight.

If a group has child tracker rows but no closure row yet, use the child-row ratio.

If a group has not been expanded into tracker rows, the group score is `0`.

Round only the final reported group score and total score to one decimal place.
Do not round row counts. Do not estimate rows that are not present in the
execution tracker.

## Scoring Weights

| Group | Master spec coverage | Weight |
| --- | --- | ---: |
| Workspace and data hygiene | Phase 1 | 4 |
| Company and tenant source of truth | Phase 2 | 6 |
| New client setup wizard | Phase 2A | 5 |
| Access and permissions | Phase 3 | 6 |
| Navigation and route cleanup | Phase 4 | 4 |
| Checklist source of truth | Phase 5 | 7 |
| Checklist builder and publishing | Phase 6 | 6 |
| Live daily checks | Phase 7 | 6 |
| PDF evidence | Phase 8 | 5 |
| Registers and assets | Phase 9 | 6 |
| Stock and medication flow | Phase 10 | 4 |
| Schematic assignment and live schematic behavior | Phase 11 | 3 |
| Global vehicle schematic library | Phase 11A | 2 |
| Readiness and operational reports | Phase 12 | 5 |
| Compliance source packs, compliance mode, SMS/email | Phases 12B, 12C, 12D | 4 |
| AI import and SOP/clinical guideline UI | Phases 13, 13A | 7 |
| AI predictive analysis | Phase 14 | 4 |
| SaaS packaging, tiers, downgrade/cancellation, billing | Phases 15, 15A, 15B, 15C | 6 |
| Production architecture, security, legal, support, sales, marketing truth-control | Phases 16, 16A-16I | 7 |
| Public website and launch funnel | Phase 17 | 1 |
| Final release gate | Phase 18 | 2 |
| **Total** |  | **100** |

## Release Blockers

The app cannot be called production-ready, regardless of score, if any of these
are true:

- build fails
- app cannot start from clean source
- normal startup creates seed/sample tenant data
- live daily checks use a fallback or fixed form outside Checklist Register
- tenant isolation fails
- permissions are only hidden in navigation and not enforced server-side
- PDF evidence download does not work
- client data export/offboarding is absent
- billing/subscription state is not production-safe
- uploaded files are not production-safe
- security/legal/compliance review is incomplete
- public website claims are not truth-controlled
- final release gate is incomplete

## Current Tracker Baseline

Date: 2026-06-24

This baseline uses `docs/specs/execution-tracker.md` only.

| Group | Tracker evidence | Row count | Group weight | Score |
| --- | --- | ---: | ---: | ---: |
| Phase 1 | P1-01 through P1-08 are `Done` | 8/8 | 4.0 | 4.0 |
| Phase 2 | P2-GATE through P2-15M are `Done` | 21/21 | 6.0 | 6.0 |
| Phase 2A | P2A-15A through P2A-15J are `Done`; P2A-15K through P2A-15R are `Not started` | 10/18 | 5.0 | 2.8 |
| Remaining groups | Not expanded, not started, or not tracker-done | 0/0 counted | 85.0 | 0.0 |
| **Calculated completion** | Phase 1 4.0 + Phase 2 6.0 + Phase 2A 2.8 + remaining groups 0.0 |  |  | **12.8% / 100%** |

Current Phase 2A row progress:

```text
P2A-15A through P2A-15J: Done = 10 rows
P2A-15K through P2A-15R: Not started = 8 rows
Phase 2A row progress = 10/18 = 55.6%
Phase 2A score = 5.0 x (10/18) = 2.8
Total SaaS readiness = 4.0 + 6.0 + 2.8 = 12.8% / 100%
```

Future Phase 2A rows must change the score by this exact formula. For example,
if P2A-15K is reconciled as `Done`, Phase 2A becomes `11/18`, not an arbitrary
manual percentage.

## Required Update Process

After each tracker reconciliation:

1. Read the execution tracker.
2. Confirm the active row status.
3. Count `Done`, non-`Done`, and total rows for the active phase.
4. Recalculate the phase score using the group weight.
5. Recalculate total SaaS readiness using all completed group scores.
6. Report phase progress and total SaaS readiness using the required display
   shape above.
7. State whether any release blockers remain.
8. State the exact next tracker instruction.

If this file conflicts with the master spec or execution tracker, stop and ask for
spec/tracker correction before implementation continues.
