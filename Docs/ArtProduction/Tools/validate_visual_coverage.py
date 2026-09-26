"""Verify current hash-bound evidence for the 62 mandatory visual samples.

Reads recorded visual review observations; never grants acceptance or edits production data.
"""
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ART = ROOT / "Docs/ArtProduction"
REVIEW_FILES = (
    "independent_visual_review.json",
    "final_independent_visual_review.json",
    "town_hall_visual_review.json",
)
REQUIRED_TIERS = {"T00_RUIN", "T01_L001", "T08_L200", "T16_L1000", "T24_L5000", "T_FIXED"}


def main():
    manifest = json.loads((ART / "BUILDING_ASSET_MANIFEST.json").read_text(encoding="utf-8-sig"))
    required = {
        slot["reference_image"]: f'{slot["building_id"]}/{slot["tier_id"]}'
        for slot in manifest["slots"] if slot["tier_id"] in REQUIRED_TIERS
    }
    families = {slot["building_id"] for slot in manifest["slots"]}
    required.update({
        f"Docs/ArtProduction/Buildings/{family}/family_showcase.png": f"{family}/showcase"
        for family in families
    })
    records = {path: [] for path in required}
    sources = {}
    for filename in REVIEW_FILES:
        review_path = ART / "Validation" / filename
        data = review_path.read_bytes()
        sources[filename] = hashlib.sha256(data).hexdigest()
        review = json.loads(data.decode("utf-8-sig"))
        for entry in review.get("entries", []):
            path = entry.get("path") or entry.get("image")
            if path in records and entry.get("status", "").startswith("passed"):
                records[path].append({
                    "report": filename,
                    "key": entry.get("key") or entry.get("slot"),
                    "sha256": entry.get("sha256") or entry.get("image_sha256"),
                    "status": entry["status"],
                })
    covered, missing = [], []
    for relative, key in sorted(required.items()):
        path = ROOT / relative
        digest = hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
        matched = [record for record in records[relative] if digest and record["sha256"] == digest]
        item = {"key": key, "path": relative, "current_sha256": digest}
        if matched:
            item["review_evidence"] = matched
            covered.append(item)
        else:
            item["reason"] = "missing_image" if digest is None else "no_current_passed_review_hash"
            missing.append(item)
    result = {
        "schemaVersion": 1,
        "checked_at_utc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "passed": len(required) == 62 and not missing,
        "expected_samples": 62,
        "required_samples": len(required),
        "covered_samples": len(covered),
        "covered": covered,
        "missing": missing,
        "review_report_sha256": sources,
        "limitation": "Checks recorded visual review observations and current file hashes; does not inspect pixels or assert mesh quality.",
    }
    report = ART / "Validation/visual_coverage_report.json"
    report.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: result[key] for key in ("passed", "required_samples", "covered_samples", "missing")}))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
