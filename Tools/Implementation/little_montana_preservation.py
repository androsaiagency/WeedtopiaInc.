"""Verify the captured working state before adding Little Montana; never reset assets."""
from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/LittleMontana/Preservation"
SCENES = ("BurningHills", "BigRockCity", "SevenHills")


def digest(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path: pathlib.Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def documents(path: pathlib.Path) -> dict[str, str]:
    source = path.read_text(encoding="utf-8-sig")
    return {m.group(1): m.group(0) for m in re.finditer(
        r"^--- !u!\d+ &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)", source, re.M | re.S)}


def additive_entries(before: dict, after: dict) -> list[str]:
    errors = []
    for key, original in before.items():
        if key == "version":
            continue
        current = after.get(key)
        if isinstance(original, list) and original and isinstance(original[0], dict):
            identity = "key" if key in ("texts", "entries") else "id"
            by_id = {entry.get(identity): entry for entry in current or []}
            for entry in original:
                if by_id.get(entry.get(identity)) != entry:
                    errors.append(key + ":" + str(entry.get(identity)))
        elif current != original:
            errors.append(key)
    return errors


def verify() -> dict:
    baseline = read(DOC / "baseline.json")
    allowed = set(read(DOC / "reviewed_changes.json")["files"])
    changed = [name for name, expected in baseline.items()
               if not (ROOT / name).is_file() or digest(ROOT / name) != expected]
    removed = [name for name in baseline if not (ROOT / name).is_file()]
    original_content = read(DOC / "BurningHills.json")
    current_content = read(ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json")
    prior_content = additive_entries(original_content, current_content)
    locales = {}
    for language in ("de", "en", "es", "fr", "ru"):
        old = read(DOC / "Localization" / (language + ".json"))
        new = read(ROOT / "Assets/Weedtopia/Game/Localization" / (language + ".json"))
        locales[language] = additive_entries(old, new)
    scene_results = {}
    for name in SCENES:
        old = documents(DOC / (name + ".unity"))
        new = documents(ROOT / "Assets/Weedtopia/Game/Scenes" / (name + ".unity"))
        scene_results[name] = {
            "originalObjects": len(old), "removedObjects": sorted(old.keys() - new.keys()),
            "changedObjects": [identity for identity in old if old[identity] != new.get(identity)],
            "addedObjects": len(new.keys() - old.keys()),
            "originalObjectIdentitiesRetained": not (old.keys() - new.keys()),
        }
    # Review only the known additive UI extension. Full scene bytes and exact changed document IDs remain evidence.
    permitted_scene_guids = set(read(DOC / "reviewed_changes.json").get("sceneScriptGuids", []))
    for name, result in scene_results.items():
        old = documents(DOC / (name + ".unity"))
        new = documents(ROOT / "Assets/Weedtopia/Game/Scenes" / (name + ".unity"))
        result["unexpectedChangedObjects"] = []
        for identity in result["changedObjects"]:
            doc = old[identity]
            if any(guid in doc for guid in permitted_scene_guids):
                continue
            # Existing prefab instances may gain only authored CityCampaign overrides.
            if "PrefabInstance:" in doc:
                a = re.sub(r"\n    m_Modifications:.*?(?=\n    m_RemovedComponents:)", "\n    m_Modifications: OMITTED", doc, flags=re.S)
                b = re.sub(r"\n    m_Modifications:.*?(?=\n    m_RemovedComponents:)", "\n    m_Modifications: OMITTED", new.get(identity, ""), flags=re.S)
                if a == b:
                    continue
            result["unexpectedChangedObjects"].append(identity)
    report = {
        "utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "protectedFiles": len(baseline), "changedFiles": changed,
        "reviewedIntegrationFiles": sorted(allowed),
        "unexpectedChanges": [name for name in changed if name not in allowed],
        "unchangedProtectedFiles": len(baseline) - len(changed), "removedFiles": removed,
        "priorContentChanges": prior_content, "priorLocalizationChanges": locales,
        "scenes": scene_results,
        "captureNote": "Current working state captured before content writes; preexisting local changes retained.",
    }
    report["status"] = "passed" if not (removed or report["unexpectedChanges"] or prior_content
        or any(locales.values()) or any(entry["removedObjects"] or entry["unexpectedChangedObjects"]
                                      for entry in scene_results.values())) else "failed"
    (DOC / "result.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    return report


def main() -> int:
    argparse.ArgumentParser(description=__doc__).parse_args()
    report = verify()
    print(json.dumps({key: report[key] for key in ("status", "protectedFiles", "unchangedProtectedFiles",
        "unexpectedChanges", "removedFiles", "priorContentChanges", "priorLocalizationChanges", "scenes")}, indent=2))
    return 0 if report["status"] == "passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
