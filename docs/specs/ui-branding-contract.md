# AcuityOps UI And Branding Contract

Status: Active

This contract controls the visible product identity and first-demo app shell. It exists to prevent AcuityOps from drifting back into seed-company behavior, page-by-page branding hacks, or unfinished website-style screens.

## Product Identity

- `AcuityOps` is the product and app identity.
- Client company names and logos are tenant data, not product identity.
- `X Med` may exist only as ordinary removable demo tenant data created through normal onboarding or setup flows.
- `X Med`, `x-med`, or any other temporary demo name must never be hardcoded, seeded, recreated on startup, used as a fallback, used as a workspace default, used as a product identity, used as a logo fallback, or required for the app to run.
- Deleting a demo tenant must not cause the app to recreate it.

## Branding Rules

- Pre-company-login screens show AcuityOps branding only.
- Role selection is shown only after successful company workspace login.
- Logged-in pages show the authenticated tenant name and uploaded logo when present.
- If the authenticated tenant has no logo, pages use the neutral AcuityOps no-logo state.
- If the authenticated tenant has no saved name, pages use neutral wording such as `Company workspace`; they must not fall back to a demo company.
- Logo upload, replacement, removal, and cache-busted rendering must use the shared company branding path.

## Authenticated App Shell

- Authenticated pages must feel like an application, not standalone web pages.
- The shared shell must avoid prototype artifacts in the first-demo path.
- Global footer links such as `Access Login Page` must not float over authenticated demo pages.
- The Page Index may exist as a compact navigation aid, but it must not dominate the page or read like internal scaffolding.
- Primary actions must look like app controls, not plain text links.
- Report rows and register rows must have a clear clickable row treatment and visible action affordance.

## First-Demo Visual Standard

The first-demo path must be visually credible before deeper feature work continues:

- Home
- Checklist Management
- Daily Vehicle & Equipment Check
- Checklist Reports
- Checklist Report Detail
- Readiness Dashboard
- Operational Reports

These pages must share the same broad visual language: restrained EMS operations dashboard styling, compact cards, clear buttons, tenant branding after login, neutral AcuityOps branding before login, and no seed-company leakage.

## Verification

For this contract, verification must confirm:

- `/` and `/CompanyLogin/acuityops-workspace` do not expose client branding before company authentication.
- `/Access` and role login only appear after company authentication.
- Logged-in first-demo pages show the authenticated tenant branding or the neutral no-logo state.
- `X Med` appears only when the active authenticated tenant is intentionally that demo tenant.
- No first-demo authenticated page shows floating `Access Login Page` links.
- Checklist report rows and operations/readiness metric controls look like app controls.
