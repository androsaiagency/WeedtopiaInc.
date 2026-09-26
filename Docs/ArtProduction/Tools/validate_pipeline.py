"""Local-only, resumable acceptance of the Weedtopia building art pipeline.

No image generation, network access, Unity invocation or source-model modification.
Requires Python 3.11+; decoding actual images additionally requires Pillow.
"""
from __future__ import annotations

import argparse
from contextvars import ContextVar
import csv
from functools import wraps
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import sys
import tempfile
from datetime import datetime, timezone

ART = "Docs/ArtProduction"
MANIFEST = f"{ART}/BUILDING_ASSET_MANIFEST.json"
MATRIX = f"{ART}/BUILDING_TIER_MATRIX.csv"
PROGRESS = f"{ART}/PROGRESS.json"
QA = f"{ART}/Validation/visual_acceptance.json"
REPORT = f"{ART}/Validation/pipeline_validation.json"
FAMILIES = (
    "cultivation_plot", "farm_processing", "farm_warehouse", "city_warehouse",
    "coffee_shop", "residential_house", "park", "butter_factory", "supermarket", "bakery",
)
ALL_FAMILIES = (*FAMILIES, "town_hall")
LEVELS = (1, 5, 10, 25, 50, 100, 150, 200, 300, 400, 500, 600, 700,
          800, 900, 1000, 1250, 1500, 1750, 2000, 2500, 3000, 4000, 5000)
TIERS = ("T00_RUIN", *(f"T{i:02d}_L{level:03d}" for i, level in enumerate(LEVELS, 1)))
PATH_FIELDS = {
    "reference_image": "mesh_input_3q.png",
    "meshy_prompt_file": "meshy_prompt_en.txt",
    "texture_prompt_file": "texture_prompt_en.txt",
    "image_prompt_file": "image_prompt_en.txt",
    "asset_spec_file": "asset_spec.de.md",
    "meshy_settings_file": "meshy_settings.json",
    "unity_incoming_folder": None,
    "unity_prefab_target": None,
}
MATRIX_FIELDS = (
    "building_id", "building_name_de", "tier_id", "min_level", "max_level", "era",
    "visual_name_de", "visible_delta_from_previous", "footprint_m_x", "footprint_m_z",
    "height_m", "target_faces", "driver_slot_delta", "reference_image",
    "meshy_prompt_file", "texture_prompt_file", "unity_incoming_folder", "unity_prefab_target", "status",
)
REQUIRED_TERMS = (
    "stylized", "game-ready", "smart-topology", "clean edges", "minimal geometry",
    "single complete asset", "centered", "grounded", "front facing +z", "no floating parts",
)
EXCLUSIONS = {
    "people": r"\b(?:people|persons|humans)\b", "scene": r"\bscenes?\b",
    "landscape": r"\blandscapes?\b", "text": r"\btext\b", "logos": r"\blogos?\b",
    "vehicles": r"\bvehicles?\b", "loose props": r"\bloose props\b",
    "diorama": r"\bdiorama(?:[- ](?:base|ground|floor))?\b",
    "cropped geometry": r"\b(?:cropped (?:geometry|parts|objects)|cut.off geometry)\b",
}
SETTINGS = {
    "primary_workflow": "image_to_3d", "fallback_workflow": "text_to_3d",
    "model_type": "smart-topology", "topology": "triangles", "texture_resolution": 2048,
    "pbr": True, "rigging": False, "animation": False,
    "recommended_export": "FBX with complete texture folder", "unity_scale": "1 unit = 1 meter",
    "unity_up_axis": "+Y", "unity_forward_axis": "+Z", "pivot_intent": "ground center",
}

_VALIDATION_MEMO = ContextVar('weedtopia_validation_memo', default=None)


def _validation_scope(function):
    """A fresh in-memory cache for one validation only; never persists acceptance."""
    @wraps(function)
    def run(*args, **kwargs):
        token = _VALIDATION_MEMO.set({'sha256': {}, 'png': {}})
        try:
            return function(*args, **kwargs)
        finally:
            _VALIDATION_MEMO.reset(token)
    return run


def _file_version(path):
    path = Path(path).resolve()
    stat = path.stat()
    return (str(path), stat.st_dev, stat.st_ino, stat.st_mtime_ns, stat.st_ctime_ns, stat.st_size)


def now():
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def sha256(path):
    memo = _VALIDATION_MEMO.get()
    version = _file_version(path)
    if memo is not None and version in memo['sha256']:
        return memo['sha256'][version]
    with Path(path).open("rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    if _file_version(path) != version:
        raise OSError(f'File changed while hashing: {path}')
    if memo is not None:
        memo['sha256'][version] = digest
    return digest


def read_json(path, default=None):
    path = Path(path)
    if not path.exists() and default is not None:
        return default
    with path.open(encoding="utf-8-sig") as stream:
        return json.load(stream)


def atomic_json(path, value):
    """Write in the same directory, fsync, then atomic replace. Never truncate in place."""
    atomic_text(path, json.dumps(value, ensure_ascii=False, indent=2) + "\n")


def atomic_text(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile("w", encoding="utf-8", newline="\n",
                                         dir=path.parent, prefix=f".{path.name}.",
                                         suffix=".tmp", delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(value)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        if temporary and temporary.exists():
            temporary.unlink()


def safe_path(root, value):
    """Reject traversal, Windows/URI forms, symlinks escaping the project, aliases."""
    if not isinstance(value, str) or not value or "\\" in value or ":" in value:
        raise ValueError("path must be a nonempty project-relative POSIX path")
    parts = value.split("/")
    if value.startswith("/") or any(p in ("", ".", "..") for p in parts):
        raise ValueError("path must be normalized, relative and contain no traversal")
    if any(p.endswith((" ", ".")) or any(c in p for c in '*?<>|\x00') for p in parts):
        raise ValueError("path contains an invalid or ambiguous Windows component")
    if any(re.fullmatch(r"(?:con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\..*)?", p, re.I) for p in parts):
        raise ValueError("path contains a Windows reserved name")
    root = Path(root).resolve()
    path = (root / PurePosixPath(value)).resolve()
    try:
        path.relative_to(root)
    except ValueError as exc:
        raise ValueError("resolved path escapes project root") from exc
    return path


def slot_key(slot):
    return f"{slot.get('building_id', '')}/{slot.get('tier_id', '')}"


def expected_keys():
    return {f"{family}/{tier}" for family in FAMILIES for tier in TIERS} | {"town_hall/T_FIXED"}


def production_order():
    result = [f"{family}/T01_L001" for family in FAMILIES] + ["town_hall/T_FIXED"]
    for family in FAMILIES:
        result.extend(f"{family}/{tier}" for tier in TIERS if tier != "T01_L001")
    return result


def decode_png(path, showcase=False):
    """Verify the full compressed stream and decode all pixels, not only the header."""
    from PIL import Image
    memo = _VALIDATION_MEMO.get()
    version = _file_version(path)
    cache_key = (version, showcase)
    if memo is not None and cache_key in memo['png']:
        return dict(memo['png'][cache_key])
    with Image.open(path) as image:
        if image.format != "PNG":
            raise ValueError("not a PNG file")
        image.verify()
    with Image.open(path) as image:
        image.load()
        width, height = image.size
        if getattr(image, "n_frames", 1) != 1:
            raise ValueError("animated PNG is not a single asset reference")
        if showcase:
            if width <= height or width < 1024:
                raise ValueError(f"showcase must be landscape and at least 1024 px wide; got {width}x{height}")
        elif width != height or width < 1024:
            raise ValueError(f"mesh input must be square, minimum 1024x1024; got {width}x{height}")
        result = {"width": width, "height": height, "sha256": sha256(path), "decoder": "Pillow full decode"}
    if _file_version(path) != version:
        raise OSError(f'File changed while decoding: {path}')
    if memo is not None:
        memo['png'][cache_key] = dict(result)
    return result


def load_manifest(root):
    manifest = read_json(root / MANIFEST)
    if not isinstance(manifest, dict) or manifest.get("schemaVersion") != 1:
        raise ValueError("manifest must be an object with schemaVersion 1")
    if not isinstance(manifest.get("slots"), list):
        raise ValueError("manifest.slots must be an array")
    return manifest


@_validation_scope
def validate(root, data_only=False):
    root = Path(root).resolve()
    report = {"schemaVersion": 1, "checked_at_utc": now(), "mode": "data_only" if data_only else "full",
              "passed": False, "errors": [], "pending": [], "warnings": [], "slots": {}, "showcases": {}}

    def issue(code, message, key=None, pending=False):
        report["pending" if pending else "errors"].append({"code": code, "slot": key, "message": message})

    try:
        manifest = load_manifest(root)
    except (OSError, ValueError, TypeError) as exc:
        issue("manifest", str(exc))
        return report
    slots = manifest["slots"]
    if len(slots) != 251:
        issue("slot_count", f"Expected exactly 251 slots, found {len(slots)}")
    keys = [slot_key(s) for s in slots if isinstance(s, dict)]
    if len(keys) != len(slots):
        issue("slot_type", "Every slot must be a JSON object")
    if len(keys) != len(set(keys)):
        issue("duplicate_slot", "Duplicate building_id/tier_id combinations")
    if set(keys) != expected_keys():
        issue("slot_set", f"Missing: {sorted(expected_keys() - set(keys))}; unexpected: {sorted(set(keys) - expected_keys())}")
    try:
        with (root / MATRIX).open(encoding="utf-8-sig", newline="") as stream:
            reader = csv.DictReader(stream)
            rows = list(reader)
            if not reader.fieldnames or not set(MATRIX_FIELDS).issubset(reader.fieldnames):
                issue("matrix_columns", "Matrix is missing required columns")
        if len(rows) != 251 or len({slot_key(row) for row in rows}) != len(rows):
            issue("matrix_count", "Matrix must contain exactly 251 unique rows")
        matrix_by_key = {slot_key(row): row for row in rows}
        if set(matrix_by_key) != set(keys):
            issue("matrix_slot_set", "CSV and manifest slot sets differ")
        for slot in slots:
            if not isinstance(slot, dict):
                continue
            key = slot_key(slot)
            row = matrix_by_key.get(key)
            if row:
                for field, value in row.items():
                    if field is None or field not in slot:
                        issue("matrix_field", f"CSV field {field!r} is absent from JSON", key)
                    elif (str(slot[field]).lower() if isinstance(slot[field], bool) else str(slot[field])) != value:
                        # Numeric CSV formatting is normalized below; all other text must match exactly.
                        if isinstance(slot[field], (int, float)) and not isinstance(slot[field], bool):
                            try:
                                if float(value) == slot[field]:
                                    continue
                            except (ValueError, TypeError):
                                pass
                        if str(slot[field]).lower() == value and isinstance(slot[field], bool):
                            continue
                        issue("matrix_mismatch", f"CSV and JSON differ in {field}", key)
    except (OSError, csv.Error) as exc:
        issue("matrix", str(exc))

    for name in ("ART_DIRECTION.md", "MESHY_WORKFLOW.md"):
        file = root / ART / name
        if not file.is_file() or not file.read_text(encoding="utf-8-sig").strip():
            issue("production_doc", f"Missing or empty {name}")
    try:
        qa = read_json(root / QA, {"schemaVersion": 1, "entries": {}})
        if qa.get("schemaVersion") != 1 or not isinstance(qa.get("entries"), dict):
            raise ValueError("QA ledger schemaVersion/entries invalid")
    except (OSError, ValueError, TypeError) as exc:
        issue("qa_ledger", str(exc))
        qa = {"entries": {}}
    seen_paths = {}
    hashes = {}
    family_dimensions = {}
    for slot in slots:
        if not isinstance(slot, dict):
            continue
        key = slot_key(slot)
        initial_error_count = len(report["errors"])
        state = {"image_present": False, "image_valid": False, "companions_complete": True,
                 "visual_accepted": False, "complete": False, "failed": False}
        report["slots"][key] = state
        family, tier = slot.get("building_id"), slot.get("tier_id")
        missing_fields = set((*MATRIX_FIELDS, *PATH_FIELDS)) - slot.keys()
        if missing_fields:
            issue("required_fields", f"Missing fields {sorted(missing_fields)}", key)
        if family in FAMILIES and tier in TIERS:
            index = TIERS.index(tier)
            low = 0 if index == 0 else LEVELS[index - 1]
            high = 0 if index == 0 else (LEVELS[index] - 1 if index < 24 else 5000)
            if slot.get("min_level") != low or slot.get("max_level") != high:
                issue("level_band", f"Expected [{low}, {high}] for {tier}", key)
            expected_driver = 1 if family in ("farm_warehouse", "city_warehouse") and index >= 1 else 0
        else:
            expected_driver = 0
            if family == "town_hall" and (slot.get("min_level"), slot.get("max_level")) != (0, 5000):
                issue("fixed_band", "T_FIXED must map levels 0 through 5000", key)
        if slot.get("driver_slot_delta") != expected_driver:
            issue("driver_delta", f"Expected driver_slot_delta {expected_driver}", key)
        for field in ("footprint_m_x", "footprint_m_z", "height_m"):
            value = slot.get(field)
            if isinstance(value, bool) or not isinstance(value, (int, float)) or not (0 < value < 1000):
                issue("dimensions", f"{field} must be a positive plausible number in meters", key)
        dimensions = (slot.get("footprint_m_x"), slot.get("footprint_m_z"))
        if family in family_dimensions and family_dimensions[family] != dimensions:
            issue("reserved_footprint", "Reserved maximum footprint must remain constant within the family", key)
        family_dimensions[family] = dimensions
        if not isinstance(slot.get("target_faces"), int) or isinstance(slot.get("target_faces"), bool) or not 6000 <= slot["target_faces"] <= 15000:
            issue("face_budget", "target_faces must be an integer between 6000 and 15000", key)
        for field in ("building_name_de", "era", "visual_name_de", "visible_delta_from_previous", "status"):
            if not isinstance(slot.get(field), str) or not slot[field].strip():
                issue("required_text", f"{field} must contain text", key)
        paths = {}
        for field, filename in PATH_FIELDS.items():
            value = slot.get(field)
            try:
                path = safe_path(root, value)
                paths[field] = path
                expected_prefix = f"{ART}/Buildings/{family}/{tier}/"
                if filename and value != expected_prefix + filename:
                    issue("slot_path", f"{field} must be {expected_prefix + filename}", key)
                if field == "unity_incoming_folder" and value != f"Assets/Weedtopia/Art/Buildings/Incoming/{family}/{tier}":
                    issue("incoming_path", "Incoming folder does not match slot identity", key)
                if field == "unity_prefab_target" and not (value.startswith(f"Assets/Weedtopia/Art/Buildings/Prefabs/{family}/") and value.endswith(".prefab")):
                    issue("prefab_path", "Prefab path must be in the assigned family folder with .prefab extension", key)
                folded = value.casefold()
                if folded in seen_paths:
                    issue("duplicate_path", f"Path also used by {seen_paths[folded]}", key)
                seen_paths[folded] = key + ":" + field
                if field == "unity_incoming_folder":
                    if not path.is_dir():
                        issue("missing_incoming", f"Missing incoming directory {value}", key)
                elif field == "unity_prefab_target":
                    if not path.is_file():
                        issue("missing_prefab", f"Serialized wrapper target not present: {value}", key, pending=True)
                elif field == "reference_image":
                    state["image_present"] = path.is_file()
                    if not path.is_file():
                        issue("missing_image", value, key, pending=True)
                elif not path.is_file() or path.stat().st_size == 0:
                    state["companions_complete"] = False
                    issue("missing_companion", f"Missing or empty {value}", key)
            except (ValueError, OSError) as exc:
                state["companions_complete"] = False
                issue("unsafe_path", f"{field}: {exc}", key)
        for field in ("meshy_prompt_file", "texture_prompt_file", "image_prompt_file", "asset_spec_file"):
            file = paths.get(field)
            if file and file.is_file():
                try:
                    raw_text = file.read_text(encoding="utf-8-sig")
                    content = raw_text.rstrip("\r\n")
                    if not content.strip():
                        issue("empty_prompt", f"{field} is empty", key)
                    if field in ("meshy_prompt_file", "texture_prompt_file") and len(content) > 800:
                        issue("prompt_length", f"{field}: {len(content)} characters (max 800)", key)
                    if field == "meshy_prompt_file":
                        lower = content.lower()
                        for term in REQUIRED_TERMS:
                            if term not in lower:
                                issue("prompt_term", f"Main prompt lacks {term!r}", key)
                        # Require an explicitly negative clause, not incidental affirmative mentions.
                        negative_clauses = re.findall(r"(?:\bno\b|\bexclude\b|\bwithout\b)([^.;\n]*)(?:[.;\n]|$)", lower)
                        negative = " ".join(negative_clauses)
                        for name, pattern in EXCLUSIONS.items():
                            if not re.search(pattern, negative):
                                issue("prompt_exclusion", f"Main prompt does not explicitly exclude {name}", key)
                except (OSError, UnicodeError) as exc:
                    issue("companion_encoding", f"{field}: {exc}", key)
        settings_file = paths.get("meshy_settings_file")
        if settings_file and settings_file.is_file():
            try:
                settings = read_json(settings_file)
                if not isinstance(settings, dict):
                    raise ValueError("settings must be an object")
                for field, expected in SETTINGS.items():
                    if settings.get(field) != expected or isinstance(expected, bool) and type(settings.get(field)) is not bool:
                        issue("settings", f"{field} must be {expected!r}", key)
                if settings.get("target_faces") != slot.get("target_faces"):
                    issue("settings_budget", "Settings face budget differs from manifest", key)
            except (OSError, ValueError, TypeError) as exc:
                issue("settings", str(exc), key)
        image = paths.get("reference_image")
        if image and image.is_file():
            try:
                info = decode_png(image)
                state.update(info)
                state["image_valid"] = True
                if info["sha256"] in hashes:
                    other = hashes[info["sha256"]]
                    issue("duplicate_image", f"Identical image also assigned to {other}", key)
                    report["slots"][other]["failed"] = True
                    report["slots"][other]["complete"] = False
                hashes[info["sha256"]] = key
                state["visual_accepted"] = qa_valid(root, qa["entries"].get(key), image, paths.get("image_prompt_file"))
                if not state["visual_accepted"]:
                    issue("visual_qa_pending", "No current image/prompt-hash-bound visual acceptance and provenance", key, pending=True)
                if qa["entries"].get(key, {}).get("status") == "accepted" and not state["visual_accepted"]:
                    issue("accepted_file_changed", "Previously accepted image, prompt or reference provenance no longer matches", key)
            except (OSError, ValueError, ImportError, SyntaxError) as exc:
                issue("invalid_image", f"Cannot fully decode/verify PNG: {exc}", key)
        if qa["entries"].get(key, {}).get("status") == "rejected":
            state["failed"] = True
            issue("rejected_image", qa["entries"][key].get("notes", "Visually rejected"), key)
        state["failed"] |= len(report["errors"]) > initial_error_count and state["image_present"]
        state["complete"] = state["image_valid"] and state["companions_complete"] and state["visual_accepted"] and not state["failed"]

    for family in ALL_FAMILIES:
        family_spec = root / ART / "Buildings" / family / "family_spec.de.md"
        if not family_spec.is_file() or not family_spec.read_text(encoding="utf-8-sig").strip():
            issue("family_spec", "Missing or empty family specification", family)
        showcase = root / ART / "Buildings" / family / "family_showcase.png"
        state = {"present": showcase.is_file(), "valid": False, "visual_accepted": False, "complete": False}
        report["showcases"][family] = state
        if not showcase.is_file():
            issue("missing_showcase", str(showcase.relative_to(root)).replace("\\", "/"), family, pending=True)
        else:
            try:
                info = decode_png(showcase, showcase=True)
                state.update(info)
                state["valid"] = True
                state["visual_accepted"] = qa_valid(root, qa["entries"].get(f"showcase:{family}"), showcase)
                state["complete"] = state["visual_accepted"]
                if info["sha256"] in hashes:
                    other = hashes[info["sha256"]]
                    issue("duplicate_showcase", f"Duplicate of {other}", family)
                    state["complete"] = False
                    if other.startswith("showcase:"):
                        report["showcases"][other.split(":", 1)[1]]["complete"] = False
                hashes[info["sha256"]] = f"showcase:{family}"
                if not state["visual_accepted"]:
                    issue("showcase_qa_pending", "Showcase requires hash-bound visual acceptance", family, pending=True)
            except (OSError, ValueError, ImportError, SyntaxError) as exc:
                issue("invalid_showcase", str(exc), family)
    report["counts"] = {
        "expected_slots": 251, "manifest_slots": len(slots),
        "generated_slots": sum(s["image_present"] for s in report["slots"].values()),
        "validated_slots": sum(s["complete"] for s in report["slots"].values()),
        "failed_slots": sum(s["failed"] for s in report["slots"].values()),
        "expected_showcases": 11,
        "generated_showcases": sum(s["present"] for s in report["showcases"].values()),
        "validated_showcases": sum(s["complete"] for s in report["showcases"].values()),
        "metadata_errors": len(report["errors"]), "pending_items": len(report["pending"]),
    }
    report["companion_file_counts"] = {}
    for field, filename in PATH_FIELDS.items():
        if filename and field != "reference_image":
            count = 0
            for slot in slots:
                if not isinstance(slot, dict):
                    continue
                try:
                    path = safe_path(root, slot.get(field))
                    count += path.is_file() and path.stat().st_size > 0
                except (ValueError, OSError):
                    pass
            report["companion_file_counts"][field] = count
    report["passed"] = not report["errors"] and (data_only or not report["pending"])
    report["scope_note"] = "Data-only success never accepts missing images, visual QA, serialized Unity objects or Unity tests."
    return report


def qa_valid(root, entry, image, prompt=None):
    if not isinstance(entry, dict) or entry.get("status") != "accepted" or not entry.get("notes", "").strip():
        return False
    if entry.get("image_sha256") != sha256(image):
        return False
    if prompt and (not prompt.is_file() or entry.get("prompt_sha256") != sha256(prompt)):
        return False
    if not entry.get("reviewed_at_utc") or not entry.get("source_image") or not entry.get("source_image_sha256"):
        return False
    # Source may be a temporary tool output that later disappears; recorded matching hash is durable.
    if entry["source_image_sha256"] != entry["image_sha256"]:
        return False
    # Also verify the chain when resuming an old ledger; obsolete layout metadata
    # must never make a missing immediate predecessor look accepted.
    try:
        family, tier, filename = Path(image).resolve().relative_to(Path(root).resolve() / ART / "Buildings").parts
    except ValueError:
        family, tier, filename = None, None, None
    if filename == "mesh_input_3q.png" and family in FAMILIES and tier in TIERS and tier != "T01_L001":
        required = {f"{ART}/Buildings/{family}/T01_L001/mesh_input_3q.png"}
        index = TIERS.index(tier)
        if index >= 2:
            required.add(f"{ART}/Buildings/{family}/{TIERS[index - 1]}/mesh_input_3q.png")
        if not required.issubset({reference.get("path") for reference in entry.get("references", [])}):
            return False
    for reference in entry.get("references", []):
        reference_path = Path(reference.get("path", ""))
        if not reference_path.is_absolute():
            try:
                reference_path = safe_path(root, reference.get("path", ""))
            except ValueError:
                return False
        if not reference_path.is_file() or sha256(reference_path) != reference.get("sha256"):
            return False
    return True


def sync_progress(root, report=None):
    root = Path(root).resolve()
    report = report or validate(root)
    if sync_source_statuses(root, report):
        report = validate(root, data_only=report["mode"] == "data_only")
    old = read_json(root / PROGRESS, {})
    complete = [key for key in production_order() if report.get("slots", {}).get(key, {}).get("complete")]
    pending = [key for key in production_order() if key not in complete]
    remaining_showcases = [family for family in ALL_FAMILIES if not report.get("showcases", {}).get(family, {}).get("complete")]
    completed_families = []
    for family in ALL_FAMILIES:
        required = [key for key in production_order() if key.startswith(family + "/")]
        if all(key in complete for key in required) and family not in remaining_showcases:
            completed_families.append(family)
    blockers = old.get("blockers", [])
    art_complete = not pending and not remaining_showcases and not report["errors"]
    result = {"schemaVersion": 1, "updated_at_utc": now(),
              "status": "blocked" if blockers else ("art_complete" if art_complete else "pending"),
              "expected_slots": 251, "generated_slots": report.get("counts", {}).get("generated_slots", 0),
              "validated_slots": len(complete), "failed_slots": report.get("counts", {}).get("failed_slots", 0),
              "missing_slots": [key for key in production_order() if not report.get("slots", {}).get(key, {}).get("image_present")],
              "pending_slots": pending, "last_completed_slot": complete[-1] if complete else None,
              "next_slot": pending[0] if pending else None,
              "expected_showcases": 11, "generated_showcases": report.get("counts", {}).get("generated_showcases", 0),
              "validated_showcases": 11 - len(remaining_showcases), "pending_showcases": remaining_showcases,
              "next_showcase": remaining_showcases[0] if not pending and remaining_showcases else None,
              "completed_families": completed_families,
              "art_acceptance_complete": art_complete, "full_local_validation_passed": report["passed"] and report["mode"] == "full",
              "blockers": blockers, "manual_gates": old.get("manual_gates", {
                  "unity_source_compile": "not_run", "unity_import": "not_run", "unity_editmode": "not_run",
                  "unity_scaffold_idempotence": "not_run", "unity_model_intake": "requires_future_user_FBX",
              }),
              "production_order": "10 family T01 anchors, T_FIXED, then each family T00 and T02..T24, then 11 showcases",
              "acceptance_rule": "Decoded unique image + five nonempty validated companions + current hash-bound visual QA and generation provenance. Missing images are pending, not failed."}
    atomic_json(root / PROGRESS, result)
    return result


def sync_source_statuses(root, report):
    """Project computed art state into JSON and CSV without repairing unrelated data.

    Each file is atomically replaced. A process interruption between the two replacements
    can leave a detectable CSV/JSON status mismatch; rerunning sync repairs that projection.
    """
    manifest = load_manifest(root)
    statuses = {}
    changed = False
    for slot in manifest["slots"]:
        state = report.get("slots", {}).get(slot_key(slot), {})
        status = ("accepted" if state.get("complete") else "failed" if state.get("failed")
                  else "image_pending_review" if state.get("image_present") else "pending_image")
        statuses[slot_key(slot)] = status
        if slot.get("status") != status:
            slot["status"] = status
            changed = True
    with (root / MATRIX).open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.DictReader(stream)
        rows = list(reader)
        fieldnames = reader.fieldnames
    if not fieldnames or "status" not in fieldnames:
        raise ValueError("Cannot synchronize status: CSV lacks status column")
    csv_changed = False
    for row in rows:
        key = slot_key(row)
        if key in statuses and row["status"] != statuses[key]:
            row["status"] = statuses[key]
            csv_changed = True
    if changed:
        atomic_json(root / MANIFEST, manifest)
    if csv_changed:
        output = io.StringIO(newline="")
        writer = csv.DictWriter(output, fieldnames=fieldnames, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)
        atomic_text(root / MATRIX, output.getvalue())
    return changed or csv_changed


def record_qa(root, args):
    manifest = load_manifest(root)
    slots = {slot_key(s): s for s in manifest["slots"]}
    if not args.notes.strip():
        raise ValueError("Concrete nonempty visual QA notes are required")
    if args.slot:
        if args.slot not in slots:
            raise ValueError("Unknown manifest slot")
        slot = slots[args.slot]
        key = args.slot
        image = safe_path(root, slot["reference_image"])
        prompt = safe_path(root, slot["image_prompt_file"])
    else:
        if args.showcase not in ALL_FAMILIES:
            raise ValueError("Unknown showcase family")
        key = "showcase:" + args.showcase
        image = root / ART / "Buildings" / args.showcase / "family_showcase.png"
        prompt = None
    decode_png(image, showcase=bool(args.showcase))
    ledger = read_json(root / QA, {"schemaVersion": 1, "entries": {}})
    existing = ledger["entries"].get(key)
    if args.command == "accept":
        if existing and existing.get("status") == "accepted":
            if qa_valid(root, existing, image, prompt):
                return sync_progress(root)  # A rerun preserves the accepted record unchanged.
            raise ValueError("Accepted data changed; reject with QA notes before intentionally re-accepting")
        if not args.source_image or not args.prompt_file:
            raise ValueError("Acceptance requires --source-image and --prompt-file (actual generation prompt)")
        source = Path(args.source_image).resolve()
        actual_prompt = Path(args.prompt_file).resolve()
        if not source.is_file() or sha256(source) != sha256(image):
            raise ValueError("Source tool output must exist and match the final image SHA-256")
        if not actual_prompt.is_file() or not actual_prompt.read_text(encoding="utf-8-sig").strip():
            raise ValueError("Actual generation prompt must exist and contain text")
        if prompt and sha256(actual_prompt) != sha256(prompt):
            raise ValueError("image_prompt_en.txt must exactly match the actual generation prompt file")
        report = validate(root, data_only=True)
        # Explicit fresh visual acceptance supersedes the previous rejection;
        # every current file/schema/decode error must still block acceptance.
        blockers = [error for error in report["errors"] if error["slot"] in (key, None)
                    and not (error["slot"] == key and error["code"] == "rejected_image")]
        if blockers:
            raise ValueError("Cannot accept invalid slot data: " + json.dumps(blockers, ensure_ascii=False))
        references = []
        for value in args.reference:
            reference = Path(value).resolve()
            if not reference.is_file():
                raise ValueError(f"Missing generation reference {reference}")
            try:
                recorded = reference.relative_to(root).as_posix()
            except ValueError:
                recorded = str(reference)
            references.append({"path": recorded, "sha256": sha256(reference)})
        if args.slot:
            family, tier = key.split("/")
            required = []
            if family in FAMILIES and tier != "T01_L001":
                required.append(f"{ART}/Buildings/{family}/T01_L001/mesh_input_3q.png")
                index = TIERS.index(tier)
                if index >= 2:
                    required.append(f"{ART}/Buildings/{family}/{TIERS[index - 1]}/mesh_input_3q.png")
            for required_reference in required:
                if not any(r["path"] == required_reference for r in references):
                    raise ValueError(f"Generation must record required accepted reference {required_reference}")
                reference_key = "/".join(PurePosixPath(required_reference).parts[-3:-1])
                if not report["slots"].get(reference_key, {}).get("complete"):
                    raise ValueError(f"Required preceding/anchor reference is not accepted: {reference_key}")
        # Single-tier inputs always include the three selected style references.
        # The image tool permits five inputs: showcases instead use three exact
        # accepted tiers plus Reclaimed + Afterglow; retro machinery is already
        # carried by the accepted tier images.
        styles = sorted((root / ART / "References" / "ChosenStyle").glob("*.png"))
        if args.showcase:
            styles = [root / ART / "References/ChosenStyle" / name for name in
                      ('Reclaimed_Green_Haven.png', 'Burning_Hills_Afterglow.png')]
            showcase_tiers = ['T_FIXED'] if args.showcase == 'town_hall' else ['T01_L001', 'T12_L600', 'T24_L5000']
            for reference_tier in showcase_tiers:
                reference_key = args.showcase + '/' + reference_tier
                expected_reference = slots[reference_key]['reference_image']
                if not report['slots'].get(reference_key, {}).get('complete'):
                    raise ValueError('Showcase tier must be accepted: ' + reference_key)
                if not any(r['path'] == expected_reference for r in references):
                    raise ValueError('Showcase must record its actual tier reference: ' + reference_key)
        for style in styles:
            required_style = style.relative_to(root).as_posix()
            if not any(r["path"] == required_style for r in references):
                raise ValueError(f"Generation must record selected style reference {required_style}")
        entry = {"status": "accepted", "reviewed_at_utc": now(), "notes": args.notes.strip(),
                 "image_sha256": sha256(image), "source_image": str(source), "source_image_sha256": sha256(source),
                 "actual_prompt_file": str(actual_prompt), "prompt_sha256": sha256(actual_prompt), "references": references}
        if existing:
            entry["previous_record"] = existing
    else:
        entry = {"status": "rejected", "reviewed_at_utc": now(), "notes": args.notes.strip(), "image_sha256": sha256(image)}
        if existing:
            entry["previous_record"] = existing
    ledger["entries"][key] = entry
    atomic_json(root / QA, ledger)
    return sync_progress(root)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[3])
    commands = parser.add_subparsers(dest="command", required=True)
    check = commands.add_parser("validate", help="Write a machine-readable report and return 0 only if this mode passes")
    check.add_argument("--data-only", action="store_true")
    check.add_argument("--report", default=REPORT)
    commands.add_parser("sync-progress", help="Recompute and atomically save resumable progress from actual files and QA")
    for command in ("accept", "reject"):
        sub = commands.add_parser(command)
        identity = sub.add_mutually_exclusive_group(required=True)
        identity.add_argument("--slot")
        identity.add_argument("--showcase")
        sub.add_argument("--notes", required=True)
        sub.add_argument("--source-image")
        sub.add_argument("--prompt-file")
        sub.add_argument("--reference", action="append", default=[])
    blocker = commands.add_parser("set-blocker")
    blocker.add_argument("--reason", required=True)
    blocker = commands.add_parser("block")
    blocker.add_argument("--code", required=True)
    blocker.add_argument("--message", required=True)
    blocker.add_argument("--slot")
    commands.add_parser("clear-blockers")
    args = parser.parse_args(argv)
    args.root = args.root.resolve()
    try:
        if args.command == "validate":
            report = validate(args.root, args.data_only)
            atomic_json(safe_path(args.root, args.report), report)
            print(json.dumps({"passed": report["passed"], "mode": report["mode"], "counts": report.get("counts"),
                              "first_errors": report["errors"][:12], "report": args.report}, ensure_ascii=False, indent=2))
            return 0 if report["passed"] else 1
        if args.command in ("accept", "reject"):
            result = record_qa(args.root, args)
        else:
            if args.command in ("set-blocker", "block", "clear-blockers"):
                progress = read_json(args.root / PROGRESS, {})
                if args.command == "block":
                    if not args.code.strip() or not args.message.strip():
                        raise ValueError("Blocker code and message must not be empty")
                    if args.slot and args.slot not in expected_keys():
                        raise ValueError("Unknown blocker slot")
                    blockers = progress.setdefault("blockers", [])
                    entry = {"recorded_at_utc": now(), "code": args.code.strip(),
                             "reason": args.message.strip(), "slot": args.slot}
                    blockers[:] = [old for old in blockers if (old.get("code"), old.get("slot")) != (entry["code"], entry["slot"])]
                    blockers.append(entry)
                elif args.command == "set-blocker":
                    if not args.reason.strip():
                        raise ValueError("Blocker reason must not be empty")
                    progress.setdefault("blockers", []).append({"recorded_at_utc": now(), "reason": args.reason.strip()})
                else:
                    progress["blockers"] = []
                atomic_json(args.root / PROGRESS, progress)
            result = sync_progress(args.root)
        print(json.dumps({k: result[k] for k in ("status", "expected_slots", "generated_slots", "validated_slots", "failed_slots", "last_completed_slot", "next_slot", "next_showcase")}, ensure_ascii=False, indent=2))
        return 0
    except (OSError, ValueError, TypeError, KeyError, ImportError) as exc:
        print(f"Validation command failed: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
