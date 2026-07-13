# ADR: Deterministic Import Parser Libraries

Status: Accepted for B5.1 foundation

Date: 2026-07-13

Authority: `block-5-pro-import-execution-blueprint.md`

## Decision

Use `ExcelDataReader` 3.9.0 as the bounded B5.1 source-inspection reader for
`.xlsx`, `.xls`, and `.csv` files.

- Package: `ExcelDataReader`
- Version: `3.9.0`
- Licence: MIT
- B5.1 role: format parsing, worksheet enumeration, dimensions, bounded row
  and non-empty-cell counts, cached cell values, and safe failure reporting.
- It does not execute formulas or resolve external links.
- Legacy `.xls` decoding uses the .NET code-page provider required by the
  library. A file that cannot be read safely must be resaved as `.xlsx`.
- CSV parsing uses the library's quoted-field reader with a controlled set of
  delimiters and UTF-8 fallback.

For B5.3 checklist structure conversion, use the Microsoft Open XML SDK for
`.xlsx`-specific structural provenance that the tabular reader does not expose,
including merged ranges and formula-cell markers. The SDK package must be
version-pinned and its MIT licence reverified before it is added in B5.3. It is
not added or used by B5.1.

## Safety Limits

- 20 MB file size.
- 50 worksheets.
- 250 columns per worksheet.
- 50,000 total rows.
- 1,000,000 non-empty cells.
- `.xlsm` and other macro-enabled formats are rejected by extension.
- Source signatures are validated before parsing.
- A successful upload creates source evidence and an import ledger batch only.
- Parser output is metadata in B5.1; it cannot create or update operational
  records or publish a checklist.

## Licence Compatibility

MIT permits commercial use, modification, distribution, and private use with
the required copyright/licence notice. This is compatible with the commercial
AcuityOps SaaS plan. Third-party notices must include the package licence before
production distribution.

## Rejected Options

- Excel automation or Office Interop: server-hostile and requires Office.
- Ad hoc ZIP/XML parsing: higher correctness and security risk.
- AI/LLM interpretation: outside deterministic Block 5 and not auditable enough
  for the foundation contract.
- `EnsureCreated` or parser-created schema: violates the migration and database
  source-of-truth rules.
