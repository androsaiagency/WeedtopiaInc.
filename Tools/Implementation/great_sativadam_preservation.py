"""Capture and verify Great Sativadam against the actual working checkout."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / "Docs/GreatSativadam/Preservation"
CONTENT = "Assets/Weedtopia/Game/Content/BurningHills.json"
LOCALES = [f"Assets/Weedtopia/Game/Localization/{lang}.json" for lang in ("de", "en", "es", "fr", "ru")]
SAVE = "Assets/Weedtopia/Scripts/Runtime/Persistence/LocalSaveStore.cs"
DEFINITIONS = "Assets/Weedtopia/Scripts/Runtime/Domain/Definitions.cs"
STORY_SAVE = "Assets/Weedtopia/Scripts/Runtime/Persistence/VipStorySaveValidator.cs"
CAMPAIGN = "Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab"
BUILD = "ProjectSettings/EditorBuildSettings.asset"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def capture():
    FOLDER.mkdir(parents=True, exist_ok=True)
    manifest = FOLDER / "baseline.json"
    if manifest.exists():
        return {"status": "retained", "files": len(read(manifest))}
    files = {
        path.relative_to(ROOT).as_posix(): digest(path)
        for parent in ("Assets", "ProjectSettings")
        for path in (ROOT / parent).rglob("*")
        if path.is_file() and "GreatSativadam" not in path.as_posix()
    }
    important = [CONTENT, *LOCALES, SAVE, DEFINITIONS, CAMPAIGN, BUILD]
    important += [path.relative_to(ROOT).as_posix() for path in (ROOT / "Assets/Weedtopia/Game/Scenes").glob("*.unity")]
    important += [path.relative_to(ROOT).as_posix() for path in (ROOT / "Assets/Weedtopia/Scripts/Tests").rglob("*.cs")]
    for name in important:
        target = FOLDER / "Before" / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / name, target)
    manifest.write_text(json.dumps(files, indent=2), encoding="utf-8")
    return {"status": "captured", "files": len(files), "scenes": len(list((ROOT / "Assets/Weedtopia/Game/Scenes").glob("*.unity")))}


def additive(before, after, content=False):
    errors = []
    for key, old in before.items():
        if key == "version":
            continue
        new = after.get(key)
        if isinstance(old, list) and old and isinstance(old[0], dict):
            identity = "id" if "id" in old[0] else "key"
            current = {item[identity]: item for item in new or []}
            for item in old:
                expected = dict(item)
                if content and key == "buildings" and item[identity] == "residential_house":
                    expected["residentTierBonuses"] = list(item["residentTierBonuses"])
                    expected["residentTierBonuses"][24] = current[item[identity]]["residentTierBonuses"][24]
                if current.get(item[identity]) != expected:
                    errors.append(f"{key}:{item[identity]}")
        elif old != new:
            errors.append(key)
    return errors


def read_build(path):
    return re.findall(r"  - enabled:.*?(?=\n  - enabled:|\n  m_configObjects:|\Z)", path.read_text(encoding="utf-8-sig"), re.S)


def verify():
    baseline = read(FOLDER / "baseline.json")
    changed = [name for name, value in baseline.items() if (ROOT / name).exists() and digest(ROOT / name) != value]
    removed = [name for name in baseline if not (ROOT / name).exists()]
    allowed = {CONTENT, *LOCALES, SAVE, DEFINITIONS, CAMPAIGN, BUILD}
    runtime_authorization_path = FOLDER / "authorized_runtime_changes.json"
    runtime_authorizations = read(runtime_authorization_path) if runtime_authorization_path.exists() else {}
    runtime_errors = []
    for name, authorization in runtime_authorizations.items():
        if name != STORY_SAVE:
            raise ValueError("Runtime authorization is limited to the population milestone validator: " + name)
        original = FOLDER / "Before" / name
        if not original.exists() or digest(original) != baseline.get(name) or authorization.get("beforeSha256") != baseline.get(name) or authorization.get("afterSha256") != digest(ROOT / name) or not authorization.get("reason"):
            runtime_errors.append(name)
        else:
            allowed.add(name)
    fixtures = [name for name in changed if name.startswith("Assets/Weedtopia/Scripts/Tests/")]
    approvals_path = FOLDER / "authorized_fixture_changes.json"
    authorized_fixtures = read(approvals_path) if approvals_path.exists() else {}
    fixture_errors = []
    for name, approval in authorized_fixtures.items():
        if not name.startswith("Assets/Weedtopia/Scripts/Tests/") or not name.endswith(".cs"):
            raise ValueError("Fixture authorization must identify an existing test source: " + name)
        if approval.get("beforeSha256") != baseline.get(name) or approval.get("afterSha256") != digest(ROOT / name) or not approval.get("reason"):
            fixture_errors.append(name)
        else:
            allowed.add(name)
    before_content = read(FOLDER / "Before" / CONTENT)
    after_content = read(ROOT / CONTENT)
    content_errors = additive(before_content, after_content, True)
    locale_errors = {name: additive(read(FOLDER / "Before" / name), read(ROOT / name)) for name in LOCALES}
    before_house = next(item for item in before_content["buildings"] if item["id"] == "residential_house")
    after_house = next(item for item in after_content["buildings"] if item["id"] == "residential_house")
    housing = before_house["residentTierBonuses"][:24] == after_house["residentTierBonuses"][:24] and after_house["residentTierBonuses"][24] >= before_house["residentTierBonuses"][24]
    before_save = (FOLDER / "Before" / SAVE).read_text(encoding="utf-8-sig")
    migration = (ROOT / SAVE).read_text(encoding="utf-8-sig") == before_save.replace("definitions.version > 13", "definitions.version > 14")
    before_definitions = (FOLDER / "Before" / DEFINITIONS).read_text(encoding="utf-8-sig")
    after_definitions = (ROOT / DEFINITIONS).read_text(encoding="utf-8-sig")
    # A larger final density bonus needs only a version-aware validation bound.
    definitions_change = before_definitions == after_definitions or re.sub(r"bonus > \d+", "bonus > 1000", after_definitions) == before_definitions
    previous_build = read_build(FOLDER / "Before" / BUILD)
    current_build = read_build(ROOT / BUILD)
    build_additive = all(item in current_build for item in previous_build) and len(current_build) == len(previous_build) + 1 and any("GreatSativadam.unity" in item for item in current_build)
    previous_scenes = {name: digest(ROOT / name) == value for name, value in baseline.items() if name.startswith("Assets/Weedtopia/Game/Scenes/") and name.endswith(".unity")}
    result = {
        "status": "passed", "protectedFiles": len(baseline), "changed": changed, "removed": removed,
        "unexpectedChanges": sorted(set(changed) - allowed), "contentChangesToExisting": content_errors,
        "localeChangesToExisting": locale_errors, "onlyFinalHousingTierChanged": housing,
        "housingBonusBefore": before_house["residentTierBonuses"][24], "housingBonusAfter": after_house["residentTierBonuses"][24],
        "migrationGuardOnly": migration, "definitionsValidationBoundOnly": definitions_change,
        "buildSettingsAdditive": build_additive, "previousCityScenesIdentical": previous_scenes,
        "regressionFixtureChanges": fixtures, "authorizedRegressionFixtureChanges": authorized_fixtures,
        "regressionFixtureAuthorizationErrors": fixture_errors,
        "authorizedPopulationMilestoneValidationChanges": runtime_authorizations,
        "populationMilestoneValidationAuthorizationErrors": runtime_errors,
        "userVisualApproval": False, "physicalTouch": False,
    }
    if removed or result["unexpectedChanges"] or content_errors or fixture_errors or runtime_errors or any(locale_errors.values()) or not all((housing, migration, definitions_change, build_additive, all(previous_scenes.values()))):
        result["status"] = "failed"
    (FOLDER / "preservation_receipt.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=("capture", "verify"))
    args = parser.parse_args()
    result = capture() if args.mode == "capture" else verify()
    print(json.dumps(result, indent=2))
    raise SystemExit(0 if result["status"] in ("captured", "retained", "passed") else 1)
