"""Capture and verify the working assets before the explicit Golden Flower installation."""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path
from unify_resident_capacity import HELPERS, INSPECTOR, BEFORE as CAPACITY_BEFORE, projected

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / "Docs/GoldenFlower/Preservation"
CONTENT = "Assets/Weedtopia/Game/Content/BurningHills.json"
LOCALES = [f"Assets/Weedtopia/Game/Localization/{lang}.json" for lang in ("de", "en", "es", "fr", "ru")]
SAVE = "Assets/Weedtopia/Scripts/Runtime/Persistence/LocalSaveStore.cs"
CAMPAIGN = "Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab"
PLAYMODE_FIXTURES = {
    "Assets/Weedtopia/Scripts/Tests/PlayMode/CityCampaignUiTests.cs": (
        ("plot.family=\"residential_house\";plot.level=5000;", "plot.family=\"residential_house\";plot.level=1750;", 2),
        ("plot.family = \"residential_house\"; plot.level = 5000;", "plot.family = \"residential_house\"; plot.level = 1750;", 2),
        ("slot=0,level=20,rank=1", "slot=0,level=30,rank=1", 1),
        ("slot=0,level=60,rank=1", "slot=0,level=80,rank=1", 1),
        ("slot = 0, level = 60, rank = 1", "slot = 0, level = 80, rank = 1", 1),
        ("slot = 0, level = 60, rank = 3", "slot = 0, level = 46, rank = 5", 1)),
    "Assets/Weedtopia/Scripts/Tests/PlayMode/CitySceneFailureUiTests.cs": (
        ("plot.family = \"residential_house\"; plot.level = 5000;", "plot.family = \"residential_house\"; plot.level = 1750;", 1),
        ("slot = 0, level = 60, rank = 1", "slot = 0, level = 80, rank = 1", 1)),
}

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def capture():
    FOLDER.mkdir(parents=True, exist_ok=True)
    manifest = FOLDER / "baseline.json"
    if manifest.exists():
        return {"status": "retained", "baseline": str(manifest)}
    files = {}
    for parent in (ROOT / "Assets", ROOT / "ProjectSettings"):
        for path in parent.rglob("*"):
            if path.is_file() and "/GoldenFlower/" not in path.as_posix():
                files[path.relative_to(ROOT).as_posix()] = digest(path)
    for name in [CONTENT, *LOCALES, SAVE, CAMPAIGN, "ProjectSettings/EditorBuildSettings.asset"] + [
        p.relative_to(ROOT).as_posix() for p in (ROOT / "Assets/Weedtopia/Game/Scenes").glob("*.unity")
    ]:
        path = ROOT / name
        backup = FOLDER / "Before" / name
        if path.exists() and not backup.exists():
            backup.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, backup)
        # Content/migration authors may have preserved these files before this capture.
        if backup.exists():
            files[name] = digest(backup)
    manifest.write_text(json.dumps(files, indent=2), encoding="utf-8")
    return {"status": "captured", "files": len(files)}

def entries(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def additive(before, after):
    changes = []
    for key, old in before.items():
        if key == "version":
            continue
        new = after.get(key)
        if isinstance(old, list) and old and isinstance(old[0], dict):
            identity = "id" if "id" in old[0] else "key"
            current = {value[identity]: value for value in new}
            for value in old:
                updated = current.get(value[identity])
                if key == "buildings" and value[identity] == "residential_house" and updated:
                    updated = dict(updated)
                    bonuses = updated.pop("residentTierBonuses", None)
                    if bonuses != [0] * 24 + [20]:
                        changes.append("buildings:residential_house:unexpected_density_bonus")
                if updated != value:
                    changes.append(f"{key}:{value[identity]}")
        elif old != new:
            changes.append(key)
    return changes

def documents(path):
    return {match.group(1): match.group(0) for match in re.finditer(
        r"^--- !u!\d+ &(-?\d+).*?(?=^--- !u!|\Z)", path.read_text(encoding="utf-8"), re.M | re.S)}

def verify():
    baseline = entries(FOLDER / "baseline.json")
    changed = [name for name, hash_value in baseline.items() if (ROOT / name).exists() and digest(ROOT / name) != hash_value]
    removed = [name for name in baseline if not (ROOT / name).exists()]
    scene_paths = [name for name in baseline if name.startswith("Assets/Weedtopia/Game/Scenes/") and name.endswith(".unity")]
    allowed = {CONTENT, *LOCALES, SAVE, CAMPAIGN, "ProjectSettings/EditorBuildSettings.asset", *scene_paths,
        "Assets/Weedtopia/Scripts/Runtime/Domain/Definitions.cs", "Assets/Weedtopia/Scripts/Runtime/Domain/Simulation.cs",
        "Assets/Weedtopia/Scripts/Tests/EditMode/PurpleGardenCampaignTests.cs",
        "Assets/Weedtopia/Scripts/Tests/EditMode/GreenLeafVillageCampaignTests.cs", *HELPERS, INSPECTOR}
    allowed.add("Assets/Weedtopia/Scripts/Tests/EditMode/CityCampaignTests.cs")
    allowed.update(PLAYMODE_FIXTURES)
    previous_content = entries(FOLDER / "Before" / CONTENT)
    current_content = entries(ROOT / CONTENT)
    content_changes = additive(previous_content, current_content)
    locale_changes = {name: additive(entries(FOLDER / "Before" / name), entries(ROOT / name)) for name in LOCALES}
    script = (FOLDER / "Before" / SAVE).read_text(encoding="utf-8")
    migration_only = (ROOT / SAVE).read_text(encoding="utf-8") == script.replace("definitions.version > 9", "definitions.version > 10")
    ui_guids = [re.search(r"guid: (\w+)", (ROOT / f"Assets/Weedtopia/Scripts/Runtime/Unity/{name}.cs.meta").read_text()).group(1)
                for name in ("CityCampaignUi", "CityTravelRow")]
    scene_reports = {}
    for name in [*scene_paths, CAMPAIGN]:
        old, new = documents(FOLDER / "Before" / name), documents(ROOT / name)
        unexpected = []
        for identity in old:
            if old[identity] == new.get(identity):
                continue
            if any(guid in old[identity] for guid in ui_guids):
                continue
            if "RectTransform:" in old[identity] or "Transform:" in old[identity]:
                pattern = r"\n  m_Children:.*?(?=\n  m_Father:)"
                old_children = re.search(pattern, old[identity], flags=re.S)
                new_children = re.search(pattern, new.get(identity, ""), flags=re.S)
                if old_children and new_children:
                    old_ids = re.findall(r"fileID: (-?\d+)", old_children.group(0))
                    new_ids = re.findall(r"fileID: (-?\d+)", new_children.group(0))
                    # Serialized travel children may be appended; every original child and all
                    # other transform data must remain identical. Added children must be new objects.
                    if new_ids[:len(old_ids)] == old_ids and all(child in new.keys() - old.keys() for child in new_ids[len(old_ids):]) and \
                       re.sub(pattern, "\n  m_Children: OMITTED", old[identity], flags=re.S) == re.sub(pattern, "\n  m_Children: OMITTED", new.get(identity, ""), flags=re.S):
                        continue
            if "PrefabInstance:" in old[identity]:
                # Existing instance identities, transforms and removed-object lists are protected;
                # only serialized campaign travel row overrides may be added.
                pattern = r"\n    m_Modifications:.*?(?=\n    m_RemovedComponents:)"
                if re.sub(pattern, "\n    m_Modifications: OMITTED", old[identity], flags=re.S) == re.sub(pattern, "\n    m_Modifications: OMITTED", new.get(identity, ""), flags=re.S):
                    continue
            if "SceneRoots:" in old[identity]:
                continue
            unexpected.append(identity)
        scene_reports[name] = {"originalObjects": len(old), "removedObjects": list(old.keys() - new.keys()),
                               "addedObjects": len(new.keys() - old.keys()), "unexpectedChangedObjects": unexpected}
    capacity_changes = {}
    for name in [*HELPERS, INSPECTOR]:
        before = CAPACITY_BEFORE / name
        if before.exists():
            capacity_changes[name] = (ROOT / name).read_text(encoding="utf-8-sig") == projected(before.read_text(encoding="utf-8-sig"), name == INSPECTOR)
    playmode_fixtures = {}
    for name, replacements in PLAYMODE_FIXTURES.items():
        before = FOLDER / "BeforePlayModeCapacity" / name
        if before.exists():
            expected = before.read_text(encoding="utf-8-sig")
            valid = digest(before) == baseline[name]
            for old, new, count in replacements:
                valid = valid and expected.count(old) == count
                expected = expected.replace(old, new)
            playmode_fixtures[name] = valid and (ROOT / name).read_text(encoding="utf-8-sig") == expected
        else:
            playmode_fixtures[name] = (ROOT / name).exists() and digest(ROOT / name) == baseline[name]
    report = {"protectedFiles": len(baseline), "changedFiles": changed, "removedFiles": removed,
              "unexpectedChanges": [name for name in changed if name not in allowed],
              "housingFormulaUnificationOnly": capacity_changes,
              "playModeExactPopulationFixtureCorrectionsOnly": playmode_fixtures,
              "previousContentChanges": content_changes, "previousLocaleChanges": locale_changes,
              "migrationOnly": migration_only, "scenes": scene_reports}
    report["status"] = "passed" if not (removed or report["unexpectedChanges"] or content_changes or
        any(locale_changes.values()) or not migration_only or not all(capacity_changes.values()) or not all(playmode_fixtures.values()) or any(s["removedObjects"] or s["unexpectedChangedObjects"] for s in scene_reports.values())) else "failed"
    (FOLDER / "result.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    return report

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("capture", "verify"))
    result = capture() if parser.parse_args().mode == "capture" else verify()
    print(json.dumps(result, indent=2))
    raise SystemExit(1 if result["status"] == "failed" else 0)
