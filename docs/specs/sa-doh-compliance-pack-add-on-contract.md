# AcuityOps South African DOH Compliance Pack Add-On Contract

Status: Proposed controlling commercial and module-boundary contract

Updated: 2026-07-29

Progress authority: `docs/specs/commercial-launch-progress-tracker.md`

Regulatory design authority:
`docs/specs/block-6-sa-private-ambulance-inspection-mode-blueprint.md`

Source authority: `docs/specs/block-6-authoritative-source-register.md`

## Product Decision

The South African Department of Health inspection-preparation capability is a
separately licensed annual AcuityOps add-on. It is not included automatically
in Base, Pro, Premium or Enterprise and is not a dependency of the core
AcuityOps commercial launch.

Commercial product name:

`AcuityOps South African DOH Compliance Pack`

Historical Block 6 becomes Add-on Track A1. Existing identifiers `B6.0` through
`B6.7`, commits, source evidence and acceptance records remain unchanged for
traceability. Moving the work does not approve a requirement pack, remove an
external review gate or authorize B6.2.

The add-on is inspection-preparation and compliance-evidence software. It is
not a regulator, legal advice, a certification or a guarantee that an
inspection will be passed.

## Commercial Packaging

- The integrated add-on may be purchased by an active AcuityOps tenant on any
  core tier.
- A 12-month entitlement is required. Core tier entitlement alone never
  activates the add-on.
- The initial annual SKU includes the approved South African national baseline
  and one approved province.
- Each additional approved province is a separately recorded province
  entitlement aligned to the same annual term.
- A province may not be sold or activated as authoritative while its source
  pack is incomplete, blocked, withdrawn, unapproved or awaiting required legal
  or private-EMS operational review.
- Premium AI assistance requires both an active Premium entitlement and an
  active add-on entitlement. AI may explain or forecast deterministic results;
  it may not activate rules or determine legal applicability.
- Enterprise custom jurisdiction packs require a separate contract, source
  acquisition plan, legal review, operational review and activation gate.
- Assisted evidence review, implementation support and pre-inspection
  consulting are separate services and must not be represented as regulator
  approval.

Exact prices, VAT treatment, renewal terms, refund rules and province-addition
fees remain subject to the production billing decision, accountant review and
legal review.

## Module Boundary

### Core AcuityOps

Core AcuityOps owns normal tenant operations:

- tenant identity, users, roles and permissions;
- operational structures and storage locations;
- staff, vehicle, equipment, stock and medication registers;
- checklists, readiness, tasks, issues and evidence reports;
- tenant documents, audit history, export and offboarding;
- billing and core subscription state.

The add-on may read eligible core evidence only through tenant-scoped,
permission-checked evidence-provider interfaces. The add-on must not silently
alter core records or treat record existence as proof of compliance.

### Product-Owned Add-On Data

The platform owns and governs:

- jurisdictions and regulators;
- retained source versions and clauses;
- national baseline and provincial overlay packs;
- requirement, applicability and evidence definitions;
- source relationships, legal/operational reviews and governance events;
- deterministic evaluator versions.

Product-owned regulatory data has no tenant ownership or tenant mutation route.

### Tenant-Owned Add-On Data

The tenant owns:

- add-on entitlement and selected province assignments;
- compliance profile and applicability answers;
- inspection-preparation sessions and scope;
- linked evidence, attestations and reviewer decisions;
- deterministic results and corrective actions;
- immutable session snapshots and generated evidence packs;
- add-on audit events and exports.

Tenant data must remain isolated, exportable and subject to the tenant
retention/offboarding contract. Product packs must never be copied into tenant
tables as mutable rules.

## Entitlement Model

The implementation must provide a server-enforced entitlement equivalent to:

- `TenantId`;
- product code `SA_DOH_COMPLIANCE`;
- purchase/billing reference;
- `TermStartsAtUtc` and `TermEndsAtUtc`;
- status: `Pending`, `Provisioning`, `Active`, `Grace`, `Expired`,
  `Suspended` or `Cancelled`;
- purchased province assignments;
- approved national and provincial pack availability;
- renewal, cancellation and support-override history;
- audited actor, reason and timestamps for every status change.

Province assignments must be separate entitlement records or child rows. An
active national entitlement does not imply access to every province.

All route, action, evaluation and export authorization must be enforced
server-side. Navigation visibility alone is not an entitlement control.

Pack approval and commercial entitlement are independent gates:

1. A client cannot use a pack without an active entitlement.
2. An entitlement cannot make an unapproved pack authoritative.
3. A pack becoming active does not grant any tenant access automatically.

## Twelve-Month Lifecycle

1. `Pending`: purchase or contract exists but access is unavailable.
2. `Provisioning`: billing and selected provinces are being validated against
   approved pack availability.
3. `Active`: new sessions, reevaluations, actions and exports are permitted
   according to role permissions.
4. `Grace`: completed evidence remains accessible; new processing follows the
   explicit billing grace policy and cannot exceed contracted scope.
5. `Expired` or `Suspended`: no new session, reevaluation, pack comparison or
   configuration mutation is allowed.
6. `Cancelled`: operational access ends at the contracted effective date and
   export/offboarding rules apply.
7. Renewal creates a new auditable entitlement term. It does not rewrite the
   previous term or historical compliance sessions.

If a source pack is superseded during an active term, clients are notified.
Historical sessions remain tied to their original pack. Any evaluation against
the new pack creates a new session or reevaluation snapshot.

## Expired-Access Contract

After expiry, suspension or cancellation:

- core AcuityOps remains available while the core subscription is active;
- no new add-on evaluation, session or requirement comparison may run;
- configuration and unfinished add-on sessions become read-only;
- completed immutable sessions remain readable;
- existing evidence-pack exports remain downloadable for the contractual
  retention/export window;
- tenant-owned add-on data remains included in full tenant export;
- corrective tasks already created in the core task system remain normal core
  records, with their add-on origin retained;
- downgrade, expiry or cancellation never deletes evidence silently;
- deletion occurs only through audited offboarding and retention rules;
- reactivation uses currently approved packs and never changes old results.

The billing and website truth tables must state the export window, retained
categories, reactivation behavior and any limits before purchase.

## Province Contract

- South Africa is the country root.
- The national baseline remains separate from all provincial overlays.
- All nine provinces remain separately sourced, versioned, reviewed and
  activated.
- A tenant may hold one or multiple province entitlements.
- Province selection must reflect the tenant's real operating/licensing scope.
- One province's rules may never substitute for another province's missing
  sources.
- District or municipal overlays are added only when verified requirements
  require them.
- The UI must show `Source pack incomplete` where authoritative coverage is
  unavailable.
- An incomplete province may support a clearly labelled internal evidence
  review only if legally approved for that limited use; it may not generate an
  authoritative inspection-preparation conclusion.

## Integrated And Standalone Path

### Integrated Release First

The first commercial release runs as an entitlement-gated module in the
existing AcuityOps application and tenant. It reuses core authentication,
billing, tenant isolation, audit, evidence storage, export and operational
registers through defined interfaces.

### Standalone Release Later

A future standalone `AcuityOps Compliance` product may provide a reduced tenant
shell and manual evidence-entry/import experience for organizations that do not
buy the core operations product. It must:

- reuse the same compliance bounded context and product-owned pack registry;
- reuse AcuityOps tenant identity, authentication, billing and audit contracts;
- use adapters for evidence sources rather than a forked evaluator;
- preserve the same legal, province, entitlement and export rules;
- remain separately deployable only after production tenancy and release
  architecture support it.

No standalone fork or duplicate regulatory database is permitted.

## Legal And Activation Gate

No pack may be sold as authoritative, activated or used for an authoritative
tenant conclusion until:

- retained official sources are complete enough for the defined jurisdiction;
- every requirement has clause-level provenance and effective dates;
- conflicts, repeals, amendments and unavailable sources are recorded;
- qualified South African legal review is approved and evidenced;
- province-specific private-EMS operational review is approved and evidenced;
- limitations and public wording are legally reviewed;
- deterministic evaluation, tenant isolation, permissions, immutable evidence
  and export tests pass;
- the product claim register and pricing truth table match the approved state.

B6.0 remains blocked by these external gates. B6.2 may not begin under this
contract until explicitly authorized after the B6.0 gate is resolved.

## Core Launch Separation

- Add-on Track A1 does not contribute to or block core commercial-launch
  completion.
- Core Premium AI import, general operational forecasting and SOP/CPG knowledge
  work may proceed without A1.
- Add-on-specific compliance forecasting, regulatory explanations and pack
  evidence suggestions depend on A1 deterministic outputs and an active
  entitlement.
- Core production, billing, legal, support, website and first-customer gates
  must not claim A1 availability.
- A1 has its own implementation progress and a separate commercial-availability
  state.

## Acceptance Conditions For Commercial Availability

The add-on may be sold only when:

1. B6.0 through B6.7 are accepted under Add-on Track A1.
2. At least one province and the national baseline are approved and active.
3. Entitlement, renewal, expiry, suspension, export and offboarding tests pass.
4. Tenant and role isolation pass.
5. Report and evidence-pack outputs match immutable session evidence.
6. Legal and operational review evidence is current.
7. Pricing, website claims, contracts and support guidance match product truth.
8. No marketing material promises compliance, certification or audit success.
