"""Extract retained B6.0 candidate source text with stable page anchors.

This research helper writes only to the external regulatory source vault. It
does not create product requirements, modify the application, or approve a
source pack.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path

from pypdf import PdfReader


def extract_pages(source: Path, output: Path, first: int, last: int) -> dict[str, object]:
    reader = PdfReader(str(source))
    if first < 1 or last > len(reader.pages) or first > last:
        raise ValueError(f"Invalid page range {first}-{last} for {source} ({len(reader.pages)} pages)")

    sections: list[str] = []
    for page_number in range(first, last + 1):
        text = reader.pages[page_number - 1].extract_text() or ""
        sections.append(f"\n\n===== RETAINED PDF PAGE {page_number} =====\n\n{text.strip()}\n")

    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("".join(sections), encoding="utf-8")
    return {
        "source": str(source),
        "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
        "page_range": f"{first}-{last}",
        "output": str(output),
        "output_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
        "characters": output.stat().st_size,
        "review_state": "Unreviewed",
        "authority_state": "NotActive",
    }


def normalized_anchors(path: Path) -> dict[str, str]:
    text = path.read_text(encoding="utf-8")
    normalized = re.sub(r"\s+", " ", text).casefold()
    anchors = {}
    for heading in (
        "requirements for licensing of ambulance services",
        "issue and renewal of licence",
        "inspection of ambulance service",
        "display of licence certificate and licence token",
        "information management",
        "management of ambulance service",
    ):
        anchors[heading] = "present" if heading in normalized else "not-found"
    return anchors


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("vault", type=Path)
    args = parser.parse_args()
    vault = args.vault.resolve()

    national = extract_pages(
        vault / "national/ZA-EMS-2017/verifier/government-gazette-41287-open-gazettes.pdf",
        vault / "national/ZA-EMS-2017/extracted/regulations-pages-32-83.txt",
        32,
        83,
    )
    western_cape_official = extract_pages(
        vault / "WC/ZA-WC-REGS-2012/original/gazette-7010-ambulance-regulations-2012.pdf",
        vault / "WC/ZA-WC-REGS-2012/extracted/official-english-pages-3-24.txt",
        3,
        24,
    )
    western_cape_verifier = extract_pages(
        vault / "WC/ZA-WC-REGS-2012/verifier/2012-180-lawlibrary-verifier.pdf",
        vault / "WC/ZA-WC-REGS-2012/extracted/verifier-pages-1-14.txt",
        1,
        14,
    )

    comparison = {
        "official_anchors": normalized_anchors(Path(western_cape_official["output"])),
        "verifier_anchors": normalized_anchors(Path(western_cape_verifier["output"])),
        "result": "Manual clause comparison still required",
        "legal_approval": "NotPerformed",
        "operational_approval": "NotPerformed",
    }
    comparison_path = vault / "WC/ZA-WC-REGS-2012/extracted/anchor-comparison.json"
    comparison_path.write_text(json.dumps(comparison, indent=2), encoding="utf-8")

    index = {
        "batch": "B6.0",
        "extracts": [national, western_cape_official, western_cape_verifier],
        "comparison": str(comparison_path),
        "legal_approval": "NotPerformed",
        "operational_approval": "NotPerformed",
        "requirement_activation": "Prohibited",
    }
    index_path = vault / "candidate-extraction-index.json"
    index_path.write_text(json.dumps(index, indent=2), encoding="utf-8")
    print(json.dumps(index, indent=2))


if __name__ == "__main__":
    main()
