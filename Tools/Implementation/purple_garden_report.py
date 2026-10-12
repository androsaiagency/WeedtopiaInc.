"""Aggregate verified Purple Garden receipts. This does not run Unity or grant release approval."""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import pathlib
import struct
import sys
import xml.etree.ElementTree as ET
import zlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/PurpleGarden"
CITY = "purple_garden"
SOURCES = ["burning_hills", "big_rock_city", "seven_hills", "little_montana", "cassa_vadela", "mark_wood_city", "green_leaf_village"]
LANGUAGES = ["de", "en", "es", "fr", "ru"]
RESOLUTIONS = ["1920x1080", "1366x768", "2560x1080", "1080x1920"]
EXPECTED_SHOTS = 51
SCENES = ["BurningHills", "BigRockCity", "SevenHills", "LittleMontana", "CassaVadela", "MarkWoodCity", "GreenLeafVillage", "PurpleGarden"]
CONTENT_INPUTS = ["Assets/Weedtopia/Game/Content/BurningHills.json"] + [f"Assets/Weedtopia/Game/Localization/{language}.json" for language in LANGUAGES]
ART_IDS = ["pg_iris", "pg_malik", "pg_viola", "pg_theo", "pg_jun", "pg_ada", "pg_ravi", "pg_selma", "architecture_greenhouse", "architecture_seed_terrace", "architecture_market_pergola", "architecture_cooperative_gate", "town_sign"]


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()


def relative(path):
    return path.resolve().relative_to(ROOT).as_posix()


def local_path(value):
    candidate = pathlib.Path(str(value).replace("\\", "/"))
    path = candidate if candidate.is_absolute() else ROOT / candidate
    path = path.resolve()
    path.relative_to(ROOT)
    return path


def utc(value):
    stamp = dt.datetime.fromisoformat(str(value).replace("Z", "+00:00"))
    if stamp.tzinfo is None:
        raise ValueError("UTC timestamp requires a timezone")
    return stamp.astimezone(dt.timezone.utc)


def file_time(path):
    return dt.datetime.fromtimestamp(path.stat().st_mtime, dt.timezone.utc)


def canonical_sha(value):
    return hashlib.sha256(json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()


def png(path):
    """Validate complete PNG framing and CRCs, then read the actual IHDR dimensions."""
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("Invalid PNG signature")
    offset, dimensions, ended, has_pixels = 8, None, False, False
    while offset + 12 <= len(data):
        length = struct.unpack(">I", data[offset:offset + 4])[0]
        tag = data[offset + 4:offset + 8]
        end = offset + 8 + length
        if end + 4 > len(data):
            raise ValueError("Truncated PNG chunk")
        body = data[offset + 8:end]
        expected_crc = struct.unpack(">I", data[end:end + 4])[0]
        if zlib.crc32(tag + body) & 0xffffffff != expected_crc:
            raise ValueError("PNG chunk CRC mismatch")
        if offset == 8:
            if tag != b"IHDR" or length != 13:
                raise ValueError("Missing first PNG IHDR")
            width, height = struct.unpack(">II", body[:8])
            dimensions = (width, height, body[9])
            if not width or not height:
                raise ValueError("Empty PNG dimensions")
        has_pixels |= tag == b"IDAT" and length > 0
        offset = end + 4
        if tag == b"IEND":
            ended = length == 0 and offset == len(data)
            break
    if dimensions is None or not ended or not has_pixels:
        raise ValueError("Incomplete PNG image")
    return dimensions


class Evidence:
    def __init__(self):
        self.gates = {}

    def gate(self, name, operation):
        try:
            result = operation()
            result["status"] = "passed"
        except FileNotFoundError as error:
            result = {"status": "pending", "issues": [str(error)]}
        except Exception as error:
            result = {"status": "failed", "issues": [str(error)]}
        self.gates[name] = result
        return result


def require(condition, message):
    if not condition:
        raise ValueError(message)


def latest(folder, tokens, mode=None):
    candidates = []
    for path in folder.glob("*.json"):
        if not all(token.lower() in path.name.lower() for token in tokens):
            continue
        data = read(path)
        if mode is not None and data.get("mode") != mode:
            continue
        stamp = utc(data["startedUtc"]) if data.get("startedUtc") else file_time(path)
        candidates.append((stamp, path, data))
    if not candidates:
        raise FileNotFoundError("No " + "/".join(tokens) + " " + str(mode or "") + " run receipt in " + relative(folder))
    _, path, data = max(candidates, key=lambda entry: (entry[0], entry[1].name))
    require(data.get("status") == "passed" and data.get("exitCode") == 0, "Latest run did not pass: " + relative(path))
    started, ended = utc(data["startedUtc"]), utc(data["endedUtc"])
    require(ended >= started, "Invalid run interval: " + relative(path))
    require(file_time(path) >= started, "Run receipt predates its own run: " + relative(path))
    return path, data, started, ended


def latest_modified(paths):
    files = [path for path in paths if path.is_file()]
    return max((file_time(path) for path in files), default=dt.datetime.min.replace(tzinfo=dt.timezone.utc))


def city_asset_inputs():
    production = ROOT / "Assets/Weedtopia/Production/PurpleGarden"
    files = list(production.rglob("*")) if production.exists() else []
    files += [ROOT / "Assets/Weedtopia/Game/Scenes" / (name + suffix) for name in SCENES for suffix in (".unity", ".unity.meta")]
    files += [ROOT / "Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab"]
    return [path for path in files if path.is_file()]


def source_checks():
    source_path = ROOT / "Docs/Implementation/Validation/SourceCompile/result.json"
    compiled = read(source_path)
    require(compiled.get("status") == "passed" and compiled.get("results"), "Source compiler did not pass")
    hashes, duplicates = {}, []
    for assembly in compiled["results"]:
        require(assembly.get("exitCode") == 0, "Source assembly failed: " + str(assembly.get("assembly")))
        for name, expected in assembly.get("sources", {}).items():
            name = name.replace("\\", "/")
            if name in hashes:
                duplicates.append(name)
            hashes[name] = expected.lower()
    actual = {relative(path) for path in (ROOT / "Assets/Weedtopia/Scripts").rglob("*.cs")}
    changed = [name for name, expected in hashes.items() if not local_path(name).is_file() or sha(local_path(name)) != expected]
    missing, removed = sorted(actual - hashes.keys()), sorted(hashes.keys() - actual)
    fingerprint = dict(utc=dt.datetime.now(dt.timezone.utc).isoformat(),
        status="failed" if changed or missing or removed or duplicates else "passed", assemblies=len(compiled["results"]),
        compiledSources=len(hashes), changedSourcesSinceCompile=changed, addedSourcesSinceCompile=missing,
        removedSourcesSinceCompile=removed, duplicateCompiledSources=duplicates, compileReceipt=relative(source_path),
        changedContentAndLocalesSinceValidation=[])
    return compiled, fingerprint, hashes


def content_checks(fingerprint):
    data = read(ROOT / CONTENT_INPUTS[0])
    catalogs = {lang: read(ROOT / f"Assets/Weedtopia/Game/Localization/{lang}.json") for lang in LANGUAGES}
    receipt_path = DOC / "content_validation.json"
    authored = read(receipt_path)
    require(authored.get("city") == CITY and authored.get("version") >= 9, "Wrong authored city/version")
    require(authored.get("contentDigest") == canonical_sha(data), "Content changed after author validation")
    require(all(authored.get(key) is True for key in ("priorContentPreserved", "priorLocaleEntriesPreserved", "placeholderParity", "freshProgressGoals")), "Content author checks did not pass")
    expected_counts = {"instances": 56, "routes": 35, "mainQuests": 18, "permanentResidents": 8, "personalChapters": 32, "recurringVariants": 24, "communityProjects": 3, "localizedKeys": 315}
    require(all(authored.get(key) == value for key, value in expected_counts.items()), "Authored content counts do not match")
    require(authored.get("languages") == LANGUAGES, "Incomplete authored languages")
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(ROOT / "Tools/Implementation"))
    import author_purple_garden
    author_purple_garden.validate(data, catalogs)
    path = DOC / "idempotence_validation.json"
    repeat = read(path)
    require(repeat.get("authorRuns", 0) >= 2 and repeat.get("secondRunChanged") is False and repeat.get("allSixFilesByteIdentical") is True, "Content idempotence did not pass")
    files = {entry["path"].replace("\\", "/"): entry for entry in repeat.get("files", [])}
    require(set(files) == set(CONTENT_INPUTS), "Idempotence receipt does not cover content and all five locales")
    changed = []
    for name, entry in files.items():
        expected = str(entry.get("sha256AfterSecondPass", "")).lower()
        if not entry.get("unchanged") or not expected or expected != str(entry.get("sha256BeforeSecondPass", "")).lower() or sha(local_path(name)) != expected:
            changed.append(name)
    fingerprint["changedContentAndLocalesSinceValidation"] = changed
    require(not changed, "Content/locales changed after idempotence validation: " + ", ".join(changed))
    return dict(receipt=relative(receipt_path), idempotenceReceipt=relative(path), counts=expected_counts, languages=LANGUAGES,
        contentSha256=sha(ROOT / CONTENT_INPUTS[0]), currentContentAndLocaleHashes={name: sha(local_path(name)) for name in CONTENT_INPUTS})


def test_checks(label, mode, include_assets):
    path, run, started, ended = latest(ROOT / "Docs/Implementation/Validation", ["PurpleGarden", label], mode)
    require(run.get("failed") == 0 and run.get("total", 0) > 0 and run.get("passed", 0) > 0, "Invalid success counts: " + relative(path))
    xml_path = local_path(run["xml"])
    require(xml_path.is_file() and file_time(xml_path) >= started, "Missing or stale NUnit XML: " + relative(xml_path))
    xml = ET.parse(xml_path).getroot()
    cases = xml.findall(".//test-case")
    passed = [case.get("fullname") or case.get("name") for case in cases if case.get("result") == "Passed"]
    failed = [case for case in cases if case.get("result") == "Failed"]
    skipped = [case for case in cases if case.get("result") in ("Skipped", "Inconclusive")]
    require(xml.get("result") == "Passed" and len(cases) == run["total"] and len(passed) == run["passed"] and not failed and len(skipped) == run.get("skipped", 0), "NUnit XML and JSON disagree: " + relative(path))
    inputs = list((ROOT / "Assets/Weedtopia/Scripts").rglob("*.cs")) + [local_path(name) for name in CONTENT_INPUTS]
    require(latest_modified(inputs) <= ended, "Source/content was modified after latest test ended: " + relative(path))
    if include_assets:
        require(latest_modified(city_asset_inputs()) <= ended, "Authored city assets were modified after latest test ended: " + relative(path))
    return dict(receipt=relative(path), xml=relative(xml_path), startedUtc=run["startedUtc"], endedUtc=run["endedUtc"],
        total=run["total"], passed=run["passed"], failed=run["failed"], skipped=run.get("skipped", 0), passingCaseNames=passed,
        inputBasis="Latest task-labelled native run; current sources/content hash-verified by final compiler and author receipts. No source/content modifications after run; asset fixture saves during the run are allowed.")


def install_checks():
    path, run, started, ended = latest(DOC / "Validation", ["Install"], "Install")
    receipt_path = DOC / "authoring_receipt.json"
    data = read(receipt_path)
    require(data.get("status") == "passed" and data.get("runtimeStaticObjects") is False and data.get("changedExistingCityScenes") == [], "Explicit authoring receipt did not pass")
    expected = {"buildings": 56, "routes": 35, "mainQuests": 18, "buildingVariants": 251, "landmarks": 3, "residents": 8, "chapters": 32, "recurring": 24, "sharedProjects": 3, "architecturePrefabs": 4}
    require(all(data.get(key) == value for key, value in expected.items()), "Authoring counts do not match")
    require(utc(data["utc"]) >= started, "Authoring receipt predates latest install")
    return dict(receipt=relative(receipt_path), finalInstallRun=relative(path), **expected,
        subsequentIdempotenceAuthoringReceiptAllowed=True)


def art_checks():
    path = DOC / "art_validation.json"
    data = read(path)
    require(data.get("status") == "passed" and len(data.get("images", [])) == 13, "Thirteen-image art validation did not pass")
    require({entry.get("id") for entry in data["images"]} == set(ART_IDS), "Missing or unexpected Purple Garden art IDs")
    for entry in data["images"]:
        image = local_path(entry["path"])
        width, height, colour = png(image)
        require(sha(image) == entry["sha256"].lower() and [width, height] == entry["size"], "Art changed after validation: " + relative(image))
        require(entry.get("mode") == "RGBA" and colour == 6 and entry.get("fullyTransparentPixels", 0) > 0 and entry.get("nearlyOpaquePixels", 0) > 0, "Art lacks validated transparent RGBA pixels: " + relative(image))
    return dict(receipt=relative(path), promptSet=relative(DOC / "art_generation.json"), transparentRasterAssets=13, currentHashesMatch=True, userApproved=False)


def reference_checks():
    path = DOC / "asset_reference_validation.json"
    data = read(path)
    require(data.get("status") == "passed" and data.get("newAssets", 0) > 0 and data.get("checkedUniqueReferences", 0) > 0, "Asset-reference validation did not pass")
    require(not data.get("unresolvedReferences") and not data.get("duplicateGuids") and not data.get("missingMetas"), "Asset-reference errors remain")
    require(utc(data["utc"]) >= latest_modified(city_asset_inputs()), "Asset-reference receipt predates final authored city assets")
    return dict(receipt=relative(path), **data)


def preservation_checks():
    path = DOC / "Preservation/result.json"
    data = read(path)
    baseline = read(DOC / "Preservation/baseline.json")
    require(data.get("status") == "passed" and data.get("protectedFiles") == len(baseline), "Preservation baseline/receipt did not pass")
    for key in ("unexpectedChanges", "removedFiles", "priorContentChanges"):
        require(not data.get(key), "Preservation issues remain: " + key)
    require(not any(data.get("priorLocalizationChanges", {}).values()), "Prior locale changes remain")
    changed = [name for name, expected in baseline.items() if not local_path(name).is_file() or sha(local_path(name)) != expected.lower()]
    require(set(changed) == set(data.get("changedFiles", [])), "Preservation receipt is stale against current protected file hashes")
    require(len(baseline) - len(changed) == data.get("unchangedProtectedFiles"), "Preservation unchanged count does not match current files")
    source_scene_names = SCENES[:-1]
    require(set(data.get("scenes", {})) == set(source_scene_names), "Preservation receipt does not cover all seven earlier scenes")
    for name, scene in data["scenes"].items():
        require(not scene.get("removedObjects") and not scene.get("unexpectedChangedObjects") and scene.get("originalObjectIdentitiesRetained") is True, "Original scene objects were lost or changed unexpectedly: " + name)
        before = DOC / f"Preservation/Before/Assets/Weedtopia/Game/Scenes/{name}.unity"
        current = ROOT / f"Assets/Weedtopia/Game/Scenes/{name}.unity"
        require(before.is_file() and sha(before) == sha(current), "Earlier scene is not byteidentical: " + name)
    require(utc(data["utc"]) >= latest_modified([local_path(name) for name in changed]), "Preservation receipt predates a changed protected input")
    return dict(receipt=relative(path), baseline=relative(DOC / "Preservation/baseline.json"), protectedFiles=len(baseline),
        unchangedProtectedFiles=data["unchangedProtectedFiles"], reviewedExistingFileChanges=len(changed), unexpectedChanges=[], removedProtectedFiles=[],
        sevenOriginalScenesByteIdentical={name: True for name in source_scene_names})


def capture_checks():
    path, run, started, ended = latest(DOC / "Validation", ["Capture"], "Capture")
    receipt_path, layout_path = DOC / "gameview_receipt.json", DOC / "ui_overflow.json"
    data, layout = read(receipt_path), read(layout_path)
    require(utc(data["utc"]) >= started and file_time(receipt_path) >= started and file_time(layout_path) >= started, "Capture/UI receipt predates latest capture run")
    require(data.get("mode") == "actual_playmode_gameview" and data.get("diagnosticGrowthFixtures") is True and data.get("fullEconomyPlayed") is False and data.get("userAcceptance") is False, "Capture scope/acceptance flags are invalid")
    require(data.get("expectedScreenshots") == EXPECTED_SHOTS and run.get("screenshots") == EXPECTED_SHOTS and len(data.get("screenshots", [])) == EXPECTED_SHOTS, "Expected exactly 51 actual Game View screenshots")
    require(len(data.get("plannedCaptures", [])) == EXPECTED_SHOTS and len(set(data["plannedCaptures"])) == EXPECTED_SHOTS, "Capture plan is incomplete or duplicated")
    require(layout.get("passed") is True and not layout.get("issues") and layout.get("fingerprint") == data.get("fingerprint") and data.get("fingerprint"), "UI layout receipt did not pass for captured fingerprint")
    require(set(layout.get("resolutions", [])) == set(RESOLUTIONS), "UI layout lacks the four requested resolutions")
    dimensions = {relative(local_path(entry["path"])): entry for entry in data.get("captureDimensions", [])}
    require(len(dimensions) == EXPECTED_SHOTS, "Dimension receipt does not cover all 51 shots")
    seen_sizes, paths, hashes = set(), [], {}
    for value in data["screenshots"]:
        image = local_path(value)
        image.relative_to((DOC / "Screenshots").resolve())
        name = relative(image)
        require(image.stem in data["plannedCaptures"] and file_time(image) >= started, "Missing, unplanned or stale screenshot: " + name)
        width, height, _ = png(image)
        entry = dimensions[name]
        require(width == entry.get("requestedWidth") == entry.get("renderedWidth") == entry.get("pngWidth") and height == entry.get("requestedHeight") == entry.get("renderedHeight") == entry.get("pngHeight"), "Actual PNG/render dimensions do not match request: " + name)
        seen_sizes.add(f"{width}x{height}")
        paths.append(name)
        hashes[name] = sha(image)
    require(len(set(paths)) == EXPECTED_SHOTS and seen_sizes == set(RESOLUTIONS), "Captured path/resolution coverage is incomplete")
    require(data.get("realSceneTravel") == [CITY] + SOURCES + [CITY] and data.get("preservedPreviousCities") == SOURCES, "Real city roundtrip/preservation coverage is incomplete")
    offline = data.get("saveOffline", {})
    require(offline.get("loadedCities") == 8 and offline.get("expectedCities") == 8 and offline.get("offlineSeconds") == 64, "Eight-city offline boundary was not validated")
    require(all(offline.get(key) is True for key in ("passed", "previousQuestsPreserved", "previousBuildingsPreserved", "assignedWorkersPreserved", "allCitiesProduced", "choiceMemoryPreserved", "nextOfflineReplayWasZero")), "Eight-city save/offline preservation check failed")
    production = offline.get("production", [])
    require({entry.get("city") for entry in production} == set(SOURCES + [CITY]) and len(production) == 8 and all(entry.get("increased") is True for entry in production), "Offline production does not cover every city")
    require(latest_modified(city_asset_inputs() + list((ROOT / "Assets/Weedtopia/Scripts").rglob("*.cs")) + [local_path(name) for name in CONTENT_INPUTS]) <= ended, "Captured source/content/city assets changed after the latest capture ended")
    return dict(receipt=relative(receipt_path), captureRun=relative(path), layout=relative(layout_path), screenshots=EXPECTED_SHOTS,
        screenshotPaths=paths, screenshotSha256=hashes, mode=data["mode"], fingerprint=data["fingerprint"], resolutions=RESOLUTIONS,
        uiOverflowIssues=0, actualSceneTravel=data["realSceneTravel"], saveOffline=offline, diagnosticGrowthFixtures=True,
        fullEconomyPlayed=False, userAcceptance=False,
        fingerprintBasis="Game View and UI receipts share the native fingerprint; all current source/content hashes verified separately, capture PNGs fresh against this run. No exact player-release fingerprint acceptance is inferred.")


def generate(write=True):
    evidence = Evidence()
    compiled, fingerprint, hashes = None, {"status": "pending"}, {}
    try:
        compiled, fingerprint, hashes = source_checks()
        require(fingerprint["status"] == "passed", "C# files changed, added or removed after compilation")
        evidence.gates["source"] = dict(fingerprint)
    except FileNotFoundError as error:
        evidence.gates["source"] = {"status": "pending", "issues": [str(error)]}
    except Exception as error:
        fingerprint["status"] = "failed"
        evidence.gates["source"] = {"status": "failed", "issues": [str(error)], **fingerprint}
        evidence.gates["source"]["status"] = "failed"
    content = evidence.gate("content", lambda: content_checks(fingerprint))
    if content["status"] != "passed":
        fingerprint["status"] = "failed" if content["status"] == "failed" else "pending"
    install = evidence.gate("unityImportAndAuthoring", install_checks)
    domain = evidence.gate("domain", lambda: test_checks("RelevantDomain", "EditMode", False))
    assets = evidence.gate("assets", lambda: test_checks("Assets", "EditMode", True))
    play = evidence.gate("playModeRegression", lambda: test_checks("TravelAndStories", "PlayMode", True))
    capture = evidence.gate("gameView", capture_checks)
    preserve = evidence.gate("preservation", preservation_checks)
    references = evidence.gate("newAssetReferences", reference_checks)
    art = evidence.gate("art", art_checks)
    unique_cases = set()
    for result in (domain, assets, play):
        if result["status"] == "passed":
            unique_cases.update(result["passingCaseNames"])
    technical_complete = all(result["status"] == "passed" for result in evidence.gates.values())
    pending = [name for name, result in evidence.gates.items() if result["status"] != "passed"]
    summary = dict(utc=dt.datetime.now(dt.timezone.utc).isoformat(),
        status="passed_local_integration" if technical_complete else "incomplete_local_integration", city=CITY,
        introducedContentVersion=9, unlockResidents=500000, unlockSourceCities=SOURCES,
        oneTimeMandateAid={"money": 12500, "teamCards": 7}, content=content,
        source=fingerprint, unityImportAndAuthoring=install, tests={"domain": domain, "assets": assets, "playModeRegression": play},
        uniquePassingNUnitCases=len(unique_cases), newPurpleGardenDomainCases=sum("PurpleGardenCampaignTests" in name for name in domain.get("passingCaseNames", [])),
        testCounting="Passing test-case full names are deduplicated across the latest relevant labelled runs; unrun, stale and failed runs contribute no passing cases.",
        gameView=capture, preservation=preserve, newAssetReferences=references, art=art, gateResults=evidence.gates,
        technicalIntegrationComplete=technical_complete, pendingTechnicalGates=pending,
        openGates=["User visual approval", "Physical-device touch and audio", "Android/WebGL player build and device/browser test", "Native-speaker localization review", "Human full-campaign economy playthrough"],
        humanEconomyPlaythroughCompleted=False, deviceTouchVerified=False, deviceAudioVerified=False, releaseAccepted=False)
    if write:
        DOC.mkdir(parents=True, exist_ok=True)
        source_path = ROOT / "Docs/Implementation/Validation/SourceCompile/result.json"
        if source_path.is_file():
            (DOC / "source_compile.json").write_bytes(source_path.read_bytes())
        (DOC / "source_fingerprint_validation.json").write_text(json.dumps(fingerprint, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        (DOC / "validation_summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return summary


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check-only", action="store_true", help="Read and validate receipts without writing reports")
    args = parser.parse_args()
    summary = generate(not args.check_only)
    print(json.dumps({key: summary[key] for key in ("status", "technicalIntegrationComplete", "uniquePassingNUnitCases", "newPurpleGardenDomainCases", "pendingTechnicalGates", "openGates", "releaseAccepted")}, ensure_ascii=False, indent=2))
    for name in summary["pendingTechnicalGates"]:
        print(name + ": " + "; ".join(summary["gateResults"][name].get("issues", [])))
    return 0 if summary["technicalIntegrationComplete"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
