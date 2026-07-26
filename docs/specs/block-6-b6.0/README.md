# B6.0 Source Acquisition And Legal-Pack Gate

This directory contains the reproducible research and review artifacts for the
South African private-ambulance compliance source gate. It does not contain
active product rules and must not be used as evidence that a company is
compliant or inspection-ready.

The retained official files and their hashes are stored outside Git in:

`C:\Users\odend\OneDrive\Documents\Vector EMS\regulatory-source-vault\B6.0-2026-07-20`

Rules for this batch:

1. A retained file is evidence of acquisition, not legal approval.
2. Portal pages prove that an official process exists; they do not replace the
   missing application form, inspection tool, fee schedule or directive.
3. National and provincial instruments remain separate.
4. No province may borrow another province's requirements.
5. A pack remains blocked until all required primary material is acquired,
   independently extracted, legally reviewed and operationally reviewed.
6. No artifact in this directory may activate a product requirement.

Run `acquire-official-sources.ps1` to recreate the initial retained source
manifest. The script downloads only the fixed official URLs listed in the
script and records SHA-256 hashes, byte sizes, acquisition time and failures.
The later archive and alternative-channel remediation is recorded separately
in `supplemental-remediation-manifest.csv` in the external vault so that the
initial acquisition result is not rewritten.

Run `extract-candidate-packs.py <vault-path>` to reproduce the page-anchored
national and Western Cape extracts. The helper requires `pypdf` and writes only
to the external vault.

The gate outcome is recorded in `gate-decision.md`. This batch stops before
legal approval, operational approval, requirement activation, database work,
deployment, and B6.2.

Additional remediation artifacts:

- `missing-source-remediation-report.md`: final public-source search outcome
  and the exact boundary at which an authority response is required;
- `provincial-process-candidate-extraction.md`: non-authoritative provincial
  process evidence that may guide review but may not become inspection rules;
- `provincial-source-request-pack.md`: copy-ready national and provincial
  record requests;
- `unavailable-source-register.md`: controlling record-by-record list of
  sources that now require an authority response or professional sign-off;
- `ocr-pdf-windows.ps1`: deterministic page-anchored OCR for image-only PDFs.

The external vault inventory currently covers 71 retained artifacts, excluding
the inventory and gate summary themselves. The inventory SHA-256 is recorded in
`gate-summary.json`. A hash proves file identity only; it does not prove legal
force, currentness, completeness or applicability.

The inventory includes locator evidence where an official page was verified
but the versioned source binary could not be retained. Locator evidence is
explicitly labelled and may not support clause-level requirements.
