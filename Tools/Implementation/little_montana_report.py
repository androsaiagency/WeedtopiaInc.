"""Aggregate current Little Montana evidence, retaining unrun acceptance gates explicitly."""
from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import pathlib
import struct
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/LittleMontana"
VALIDATION = ROOT / "Docs/Implementation/Validation"


def read(path: pathlib.Path) -> dict:
    if not path.is_file():
        return {"status": "unverified", "reason": "missing_evidence", "path": relative(path)}
    value = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(value, dict):
        return {"status": "unverified", "reason": "invalid_receipt", "path": relative(path)}
    return {**value, "path": relative(path)}


def relative(path: pathlib.Path) -> str:
    return path.relative_to(ROOT).as_posix()


def digest(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def canonical_digest(path: pathlib.Path) -> str:
    content = json.loads(path.read_text(encoding="utf-8-sig"))
    return hashlib.sha256(json.dumps(content, sort_keys=True, ensure_ascii=False, separators=(",", ":")).encode("utf-8")).hexdigest()


def instant(value: str | None) -> float | None:
    if not value:
        return None
    try:
        return datetime.datetime.fromisoformat(value.replace("Z", "+00:00")).timestamp()
    except (ValueError, TypeError):
        return None


def source_inputs() -> list[pathlib.Path]:
    return sorted((ROOT / "Assets/Weedtopia/Scripts").rglob("*.cs"))


def assembly_inputs(name: str) -> tuple[list[pathlib.Path], list[str]]:
    definitions = {json.loads(path.read_text(encoding="utf-8-sig"))["name"]: path
                   for path in (ROOT / "Assets/Weedtopia/Scripts").rglob("*.asmdef")}
    requested = [name]
    visited = set()
    paths = set()
    while requested:
        current = requested.pop()
        if current in visited or current not in definitions:
            continue
        visited.add(current)
        asmdef = definitions[current]
        paths.add(asmdef)
        config = json.loads(asmdef.read_text(encoding="utf-8-sig"))
        requested.extend(config.get("references", []))
        children = [path.parent for path in asmdef.parent.rglob("*.asmdef") if path != asmdef]
        paths.update(path for path in asmdef.parent.rglob("*.cs") if not any(folder in path.parents for folder in children))
    return sorted(paths), sorted(visited)


def authored_inputs() -> list[pathlib.Path]:
    paths = [ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json",
             ROOT / "Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab",
             ROOT / "ProjectSettings/EditorBuildSettings.asset"]
    paths += list((ROOT / "Assets/Weedtopia/Game/Scenes").glob("*.unity"))
    paths += list((ROOT / "Assets/Weedtopia/Game/Localization").glob("*.json"))
    paths += [path for path in (ROOT / "Assets/Weedtopia/Production/LittleMontana").rglob("*") if path.is_file()]
    return sorted(set(path for path in paths if path.is_file()))


def newer_inputs(gate: dict, paths: list[pathlib.Path], start: float | None) -> None:
    if start is None:
        gate["status"] = "unverified"
        gate["freshnessReason"] = "missing_start_time"
        return
    protected = json.loads((DOC / "Preservation/baseline.json").read_text(encoding="utf-8"))
    later = [path for path in paths if path.stat().st_mtime > start]
    identical = [relative(path) for path in later if protected.get(relative(path)) == digest(path)]
    gate["timestampOnlyTouches"] = identical
    newer = [relative(path) for path in later if relative(path) not in identical]
    gate["newerInputs"] = newer
    if newer and gate.get("status") != "failed":
        gate["status"] = "unverified"
        gate["freshnessReason"] = "evidence_predates_current_inputs"


def source_compile() -> dict:
    gate = read(VALIDATION / "SourceCompile/result.json")
    hashes = {name: expected for assembly in gate.get("results", [])
              for name, expected in assembly.get("sources", {}).items()}
    inputs = source_inputs()
    gate["currentSources"] = len(inputs)
    gate["compiledAssemblies"] = len(gate.get("results", []))
    gate["staleOrMissingSources"] = [relative(path) for path in inputs if hashes.get(relative(path)) != digest(path)]
    if not hashes or gate["staleOrMissingSources"]:
        gate["status"] = "unverified" if gate.get("status") != "failed" else "failed"
    return gate


def native(mode: str, label: str, include_assets: bool, covered_source: str | list[str] | None = None, covered_text_update: dict | None = None) -> dict:
    candidates = sorted(VALIDATION.glob("*_" + mode + "_" + label + ".json"), key=lambda p: p.stat().st_mtime)
    if not candidates:
        return {"status": "unverified", "reason": "native_run_not_found", "label": label}
    gate = read(candidates[-1])
    xml_path = pathlib.Path(gate.get("xml", ""))
    if not xml_path.is_file():
        gate["status"] = "unverified"
        gate["reason"] = "missing_native_xml"
        return gate
    tests = ET.parse(xml_path).getroot()
    gate["testCases"] = {test.attrib.get("fullname", test.attrib.get("name", "")): test.attrib.get("result")
                         for test in tests.iter("test-case")}
    gate["status"] = "passed" if gate.get("exitCode") == 0 and tests.attrib.get("result") == "Passed" and int(tests.attrib.get("total", 0)) > 0 else "failed"
    assembly = "Weedtopia.Game.EditModeTests" if label.startswith("LittleMontanaDomain") else "Weedtopia.Game.AssetTests" if label.startswith("LittleMontanaAsset") else "Weedtopia.Game.PlayModeTests"
    inputs, scope = assembly_inputs(assembly)
    gate["inputScope"] = scope
    if label.startswith("LittleMontanaAsset"):
        utilities = [path for path in inputs if "/Scripts/Editor/" in path.as_posix() and path.name.endswith("Evidence.cs")]
        gate["standaloneCaptureUtilitiesExcluded"] = [relative(path) for path in utilities]
        inputs = [path for path in inputs if path not in utilities]
    inputs += authored_inputs() if include_assets else [ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json"]
    if covered_source:
        covered_sources = [covered_source] if isinstance(covered_source, str) else covered_source
        inputs = [path for path in inputs if relative(path) not in covered_sources]
        gate["sourceCoveredBySupplementalClassRun"] = covered_source
    if covered_text_update:
        inputs = [path for path in inputs if relative(path) != "Assets/Weedtopia/Game/Content/BurningHills.json"]
        gate["additiveTextUpdateProof"] = covered_text_update
    newer_inputs(gate, inputs, instant(gate.get("startedUtc")))
    gate["coverageNote"] = "Passing automated cases prove their named assertions; user visual approval and physical play remain separate."
    return gate


def domain_results() -> dict:
    corrections = native("EditMode", "LittleMontanaDomainCorrections", False)
    prefix = "Weedtopia.Tests.LittleMontanaCampaignTests."
    updated = corrections.get("testCases", {})
    valid_corrections = corrections.get("status") == "passed" and updated and all(name.startswith(prefix) for name in updated)
    covered = "Assets/Weedtopia/Scripts/Tests/EditMode/LittleMontanaCampaignTests.cs" if valid_corrections else None
    text_update = read(DOC / "domain_text_update.json")
    text_valid = (text_update.get("status") == "passed" and set(text_update.get("addedTextKeys", [])) == {"campaign.shared_cities", "campaign.travel_action"} and
                  text_update.get("currentCanonicalDigest") == canonical_digest(ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json") and
                  text_update.get("currentWithoutAddedTextDigest") == text_update.get("preRunCanonicalDigest"))
    baseline = native("EditMode", "LittleMontanaDomain", False, covered, text_update if text_valid else None)
    compile_snapshot = read(DOC / "Validation/domain_source_compile_snapshot.json")
    baseline["preRunSourceCompileSnapshot"] = compile_snapshot.get("path")
    baseline["preRunCompileUtc"] = compile_snapshot.get("utc")
    baseline["preRunSourceHashes"] = {name: expected for result in compile_snapshot.get("results", [])
                                     if result.get("assembly") in baseline.get("inputScope", [])
                                     for name, expected in result.get("sources", {}).items()}
    if not valid_corrections:
        return baseline
    final = {name: result for name, result in baseline.get("testCases", {}).items() if not name.startswith(prefix)}
    final.update(updated)
    baseline["fullRegressionRun"] = {key: value for key, value in baseline.items() if key != "testCases"}
    baseline["supplementalClassRun"] = corrections
    baseline["testCases"] = final
    baseline["total"] = len(final)
    baseline["passed"] = sum(result == "Passed" for result in final.values())
    baseline["failed"] = sum(result == "Failed" for result in final.values())
    baseline["skipped"] = sum(result == "Skipped" for result in final.values())
    if not baseline.get("newerInputs") and final and baseline["failed"] == 0 and baseline["passed"] + baseline["skipped"] == len(final):
        baseline["status"] = "passed"
    baseline["coverageNote"] = "Full domain regression plus a complete current rerun of the strengthened LittleMontanaCampaignTests class; only that superseded test source is excluded from baseline freshness."
    return baseline


def play_results() -> dict:
    corrections = native("PlayMode", "LittleMontanaPlayCorrections", True)
    prefixes = ("Weedtopia.Tests.PlayMode.CityCampaignUiTests.", "Weedtopia.Tests.PlayMode.CitySceneFailureUiTests.")
    updated = corrections.get("testCases", {})
    valid = corrections.get("status") == "passed" and updated and all(name.startswith(prefixes) for name in updated)
    covered = ["Assets/Weedtopia/Scripts/Tests/PlayMode/CityCampaignUiTests.cs",
               "Assets/Weedtopia/Scripts/Tests/PlayMode/CitySceneFailureUiTests.cs"] if valid else None
    baseline = native("PlayMode", "LittleMontanaPlay", True, covered)
    snapshot = read(DOC / "Validation/play_source_compile_snapshot.json")
    baseline["preRunSourceCompileSnapshot"] = snapshot.get("path")
    baseline["preRunSourceHashes"] = {name: expected for result in snapshot.get("results", [])
                                     if result.get("assembly") in baseline.get("inputScope", [])
                                     for name, expected in result.get("sources", {}).items()}
    grid_proof = memory_grid_proof()
    if grid_proof.get("status") == "passed" and baseline.get("path") == grid_proof.get("playRun"):
        changed = {actor["path"] for actor in grid_proof.get("actors", [])}
        pine_proof = grid_proof.get("separatePineClearanceProof", {})
        if pine_proof.get("status") == "passed":
            changed.add(pine_proof["valley"]["path"])
            baseline["serializedPineClearanceProof"] = pine_proof
        uncovered = [name for name in baseline.get("newerInputs", []) if name not in changed]
        baseline["serializedMemoryPositionProof"] = grid_proof
        baseline["newerInputsCoveredByPositionProof"] = [name for name in baseline.get("newerInputs", []) if name in changed]
        baseline["newerInputs"] = uncovered
        if not uncovered and baseline.get("exitCode") == 0 and baseline.get("testCases") and all(result == "Passed" for result in baseline["testCases"].values()):
            baseline["status"] = "passed"
            baseline.pop("freshnessReason", None)
    if not valid:
        if (baseline.get("failed", 0) and corrections.get("status") == "unverified"
                and corrections.get("failed") == 0 and updated and all(name.startswith(prefixes) for name in updated)):
            baseline["status"] = "unverified"
            baseline["staleSupplementalClassRun"] = corrections
            baseline["freshnessReason"] = "Passing corrective fixtures exist but predate current authored inputs; original failures remain historical."
        return baseline
    final = {name: result for name, result in baseline.get("testCases", {}).items() if not name.startswith(prefixes)}
    final.update(updated)
    baseline["originalIntegrationRun"] = {key: value for key, value in baseline.items() if key != "testCases"}
    baseline["supplementalClassRun"] = corrections
    baseline["testCases"] = final
    baseline["total"] = len(final)
    baseline["passed"] = sum(result == "Passed" for result in final.values())
    baseline["failed"] = sum(result == "Failed" for result in final.values())
    baseline["skipped"] = sum(result == "Skipped" for result in final.values())
    if not baseline.get("newerInputs") and final and baseline["failed"] == 0 and baseline["passed"] + baseline["skipped"] == len(final):
        baseline["status"] = "passed"
    baseline["coverageNote"] = "47-case original integration run plus complete current reruns of the two repaired fixture classes; exact cases superseded by fullname, no double-counting."
    return baseline


def historical_asset_results() -> dict:
    case = "Weedtopia.Tests.Assets.ProductionPresentationTests.AuthoredWorldUsesAllCharactersRoutesAndOriginalAudioWithoutChangingContent"
    source = "Assets/Weedtopia/Scripts/Tests/EditorAssets/ProductionPresentationTests.cs"
    before_path = DOC / "Validation/ProductionPresentationTests.before_fourth_scene.cs.txt"
    snapshot = read(DOC / "Validation/assets_source_compile_snapshot.json")
    old_hashes = {name: value for assembly in snapshot.get("results", []) for name, value in assembly.get("sources", {}).items()}
    old = "Assert.That(EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path),Is.EquivalentTo(new[]{GameScaffold.BootstrapScene,GameScaffold.GameScene,BigRockCityAuthoring.ScenePath,SevenHillsAuthoring.ScenePath}));"
    new = "Assert.That(EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path),Is.EquivalentTo(new[]{GameScaffold.BootstrapScene}.Concat(defs.cities.Select(city=>city.scenePath))));"
    before = before_path.read_text(encoding="utf-8-sig") if before_path.is_file() else ""
    current = (ROOT / source).read_text(encoding="utf-8-sig")
    exact_change = before.count(old) == 1 and before.replace(old, new) == current
    proof = {"status": "passed" if exact_change and old_hashes.get(source) == digest(before_path) else "unverified",
             "source": source, "before": relative(before_path), "beforeSha256": digest(before_path) if before_path.is_file() else None,
             "afterSha256": digest(ROOT / source), "originalCompileHash": old_hashes.get(source),
             "scope": "Exactly one enabled-scene assertion changed from three hardcoded cities to Bootstrap plus all catalog cities; all other code is identical after newline normalization."}
    correction = native("EditMode", "LittleMontanaAssetCorrections", True)
    updated = correction.get("testCases", {})
    valid = correction.get("status") == "passed" and updated == {case: "Passed"} and proof["status"] == "passed"
    baseline = native("EditMode", "LittleMontanaAssets", True, source if valid else None)
    baseline["scopeError"] = "The final asset launch omitted the intended three-city-class filter and ran the full 147-case asset assembly. This wider run is retained as actual evidence; its single obsolete scene-count assertion is corrected separately."
    baseline["preRunSourceCompileSnapshot"] = snapshot.get("path")
    baseline["sceneAssertionCorrectionProof"] = proof
    if not valid:
        return baseline
    full = {key: value for key, value in baseline.items() if key != "testCases"}
    cases = {**baseline.get("testCases", {}), **updated}
    baseline["fullAssemblyRun"] = full
    baseline["correctiveCaseRun"] = correction
    baseline["testCases"] = cases
    baseline["total"] = len(cases)
    baseline["passed"] = sum(result == "Passed" for result in cases.values())
    baseline["failed"] = sum(result == "Failed" for result in cases.values())
    baseline["skipped"] = sum(result == "Skipped" for result in cases.values())
    baseline["status"] = "passed" if not baseline.get("newerInputs") and cases and all(result == "Passed" for result in cases.values()) else "unverified"
    baseline["gateType"] = "aggregate_native_cases"
    baseline["exitCode"] = None
    baseline["coverageNote"] = "147 unique assertions: the unchanged 146 passing cases from the full native assembly plus the exact fresh corrected scene-count case. The 12 city/guard cases passed in the full run; the isolated manual-link guard took 1.775 seconds. No duplicate cases counted."
    return baseline


def pine_clearance_proof() -> dict:
    proof = read(DOC / "pine_clearance_asset_diff.json")
    snapshot = read(DOC / "Validation/PineClearanceBefore/hashes.json")
    if proof.get("status") != "passed":
        return proof
    from little_montana_pine_clearance_proof import check_valley, VALLEY

    issues = []
    before = DOC / "Validation/PineClearanceBefore/ValleyPlaces.prefab"
    valley = proof.get("valley", {})
    if (not before.is_file() or digest(before) != snapshot.get("valleySha256")
            or valley.get("beforeSha256") != snapshot.get("valleySha256")
            or valley.get("afterSha256") != digest(ROOT / VALLEY)):
        issues.append("Separate pine before/after bytes are not exactly bound")
    try:
        current = check_valley()
        if current != valley:
            issues.append("Separate pine raw-document or route-clearance proof is stale")
    except (AssertionError, OSError, ValueError, KeyError) as error:
        issues.append(str(error))
    for group in ("authored", "playMode", "domain"):
        for name, expected in snapshot.get(group, {}).items():
            if group == "authored" and name == VALLEY:
                continue
            path = ROOT / name
            if not path.is_file() or digest(path) != expected:
                issues.append(name)
        current_paths = authored_inputs() if group == "authored" else assembly_inputs("Weedtopia.Game.PlayModeTests" if group == "playMode" else "Weedtopia.Game.EditModeTests")[0]
        issues.extend(set(relative(path) for path in current_paths) - set(snapshot.get(group, {})))
    for name, expected in snapshot.get("actors", {}).items():
        before_actor = DOC / "Validation/PineClearanceBefore" / pathlib.Path(name).name
        if not before_actor.is_file() or digest(before_actor) != expected or digest(ROOT / name) != expected:
            issues.append("Final actor changed during pine repair: " + name)
    proof["currentBindingIssues"] = sorted(set(issues))
    if issues or len(snapshot.get("actors", {})) != 7 or len(snapshot.get("authored", {})) != 618:
        proof["status"] = "unverified"
    return proof


def memory_grid_proof() -> dict:
    proof = read(DOC / "memory_grid_asset_diff.json")
    snapshot = read(DOC / "Validation/MemoryGridBefore/hashes.json")
    actor_paths = set(snapshot.get("actors", {}))
    issues = []
    pine_proof = pine_clearance_proof()
    pine_path = pine_proof.get("valley", {}).get("path")
    pine_valid = (pine_proof.get("status") == "passed" and
                  pine_proof.get("valley", {}).get("beforeSha256") == snapshot.get("authoredHashes", {}).get(pine_path))
    if pine_valid:
        proof["historicalProofUtc"] = proof.get("utc")
        proof["historicalPreservedAuthoredFiles"] = proof.pop("preservedAuthoredFiles", None)
        proof["scope"] = "Historical seven-actor memory position proof, bound to current actor bytes. The subsequent separately proved ValleyPlaces pine position repair is explicitly included in current provenance; every other authored file and every Domain/Play runtime/test input remains exact."
        proof["currentPreservedAuthoredFilesOutsideActorsAndPines"] = len(snapshot.get("authoredHashes", {})) - len(actor_paths) - 1
        proof["separatePineClearanceProof"] = pine_proof
    play_receipt = ROOT / str(snapshot.get("playRun", "missing_play_receipt"))
    if not play_receipt.is_file() or digest(play_receipt) != snapshot.get("playRunSha256") or proof.get("playRunSha256") != snapshot.get("playRunSha256"):
        issues.append("Original Play49 receipt is not exactly bound")
    if (proof.get("status") != "passed" or proof.get("checkedMemories") != 35
            or proof.get("changedMemoryTargets") != 30 or len(actor_paths) != 7
            or {entry.get("path") for entry in proof.get("actors", [])} != actor_paths):
        issues.append("Incomplete strict position-only proof")
    for actor in proof.get("actors", []):
        path = ROOT / actor["path"]
        before = DOC / "Validation/MemoryGridBefore" / path.name
        if (not path.is_file() or digest(path) != actor.get("afterSha256") or not before.is_file()
                or digest(before) != actor.get("beforeSha256")
                or actor.get("beforeSha256") != snapshot.get("actors", {}).get(actor["path"])):
            issues.append(actor["path"])
    placement = proof.get("placementCorrection", {})
    placement_snapshot = read(DOC / "Validation/MemoryPlacementBefore/hashes.json")
    if placement.get("status") != "passed" or placement.get("checkedMemories") != 35 or placement.get("changedMemoryTargets") != 7:
        issues.append("Incomplete second placement correction proof")
    for actor in placement.get("actors", []):
        path = ROOT / actor["path"]
        before = DOC / "Validation/MemoryPlacementBefore" / path.name
        if (not path.is_file() or digest(path) != actor.get("afterSha256") or not before.is_file()
                or digest(before) != actor.get("beforeSha256")
                or actor.get("beforeSha256") != placement_snapshot.get("actors", {}).get(actor["path"])):
            issues.append("Placement correction: " + actor["path"])
    for group in ("playInputs", "domainInputs", "authoredHashes"):
        for name, expected in snapshot.get(group, {}).items():
            if group == "authoredHashes" and name in actor_paths:
                continue
            if group == "authoredHashes" and pine_valid and name == pine_path:
                continue
            path = ROOT / name
            if not path.is_file() or digest(path) != expected:
                issues.append(name)
        current_paths = authored_inputs() if group == "authoredHashes" else assembly_inputs("Weedtopia.Game.PlayModeTests" if group == "playInputs" else "Weedtopia.Game.EditModeTests")[0]
        issues.extend(set(relative(path) for path in current_paths) - set(snapshot.get(group, {})))
    proof["currentBindingIssues"] = sorted(set(issues))
    if issues:
        proof["status"] = "unverified"
    return proof


def asset_results() -> dict:
    current = native("EditMode", "LittleMontanaAssetsGrid", True)
    if current.get("reason") == "native_run_not_found":
        return historical_asset_results()
    prefixes = ("Weedtopia.Tests.Assets.BigRockCityAssetTests.", "Weedtopia.Tests.Assets.SevenHillsAssetTests.",
                "Weedtopia.Tests.Assets.LittleMontanaAssetTests.")
    cases = current.get("testCases", {})
    if len(cases) != 12 or not all(name.startswith(prefixes) for name in cases):
        current["status"] = "failed"
        current["scopeReason"] = "Exactly the three requested city asset classes and 12 unique cases are required."
    current["coverageNote"] = "Fresh explicit three-city-class filter: 12 native cases cover four-city references, residents, wrappers, manual transforms/source links, memory and exact two-pine position guards, and 1820-file byte idempotence."
    current["explicitLaunch"] = read(DOC / "Validation/assets_grid_launch.json")
    current["preRunSourceCompileSnapshot"] = relative(DOC / "Validation/assets_grid_source_compile_snapshot.json")
    current["historicalFullAssemblyEvidence"] = read(DOC / "Validation/validation_summary_before_memory_grid.json").get("gates", {}).get("editorAssets", {})
    current["serializedMemoryPositionProof"] = memory_grid_proof()
    current["serializedPineClearanceProof"] = pine_clearance_proof()
    return current


def authoring() -> dict:
    gate = read(DOC / "authoring_receipt.json")
    if gate.get("runtimeStaticObjects") is not False:
        gate["status"] = "unverified"
        gate["reason"] = "static_authoring_contract_not_confirmed"
    newer_inputs(gate, [path for path in source_inputs() if "LittleMontanaAuthoring" in path.name]
                 + [ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json"], instant(gate.get("utc")))
    return gate


def gameview() -> dict:
    gate = read(DOC / "gameview_receipt.json")
    runs = sorted((DOC / "Validation").glob("*_Capture_*.json"), key=lambda path: path.stat().st_mtime)
    runner = read(runs[-1]) if runs else {"status": "unverified", "reason": "capture_runner_missing"}
    gate["runner"] = runner
    shots = gate.get("screenshots", [])
    gate["missingScreenshots"] = [str(name) for name in shots if not pathlib.Path(name).is_file()]
    gate["screenshotsCount"] = len(shots)
    dimensions = gate.get("captureDimensions", [])
    records = {entry.get("path"): entry for entry in dimensions if isinstance(entry, dict)}
    mismatches = []
    verified_resolutions = set()
    for name in shots:
        path = pathlib.Path(name)
        entry = records.get(name, {})
        header = path.read_bytes()[:24] if path.is_file() else b""
        size = struct.unpack(">II", header[16:24]) if len(header) == 24 and header[:8] == b"\x89PNG\r\n\x1a\n" else None
        requested = (entry.get("requestedWidth"), entry.get("requestedHeight"))
        rendered = (entry.get("renderedWidth"), entry.get("renderedHeight"))
        recorded = (entry.get("pngWidth"), entry.get("pngHeight"))
        if size is None or requested != rendered or requested != recorded or requested != size:
            mismatches.append({"path": name, "requested": requested, "rendered": rendered,
                               "recordedPng": recorded, "actualPng": size})
        else:
            verified_resolutions.add(size)
    gate["dimensionMismatches"] = mismatches
    gate["verifiedResolutions"] = sorted(verified_resolutions)
    expected_resolutions = {(1920, 1080), (1366, 768), (2560, 1080), (1080, 1920)}
    if mismatches or len(records) != len(shots) or len(dimensions) != len(shots) or not expected_resolutions.issubset(verified_resolutions):
        gate["status"] = "failed"
        gate["dimensionReason"] = "Each actual PNG must match its requested/rendered dimensions; all four authored resolutions require coverage."
    if gate.get("mode") != "actual_playmode_gameview" or not shots or gate["missingScreenshots"] or runner.get("status") != "passed":
        gate["status"] = "unverified" if gate.get("status") != "failed" else "failed"
    elif "status" not in gate:
        gate["status"] = "passed"
    newer_inputs(gate, source_inputs() + authored_inputs(), instant(runner.get("startedUtc")))
    gate["userAcceptance"] = False
    return gate


def verify_visual_review(review: dict, capture: dict) -> None:
    reviewed = review.get("reviewedScreenshots", {})
    if not isinstance(reviewed, dict):
        reviewed = {}
    checked = review.get("checkedImages", [])
    if not reviewed and isinstance(checked, list):
        reviewed = {entry["path"]: entry["sha256"] for entry in checked
                    if isinstance(entry, dict) and entry.get("status") == "passed"
                    and isinstance(entry.get("path"), str) and isinstance(entry.get("sha256"), str)}
    review["reviewedScreenshots"] = reviewed
    review["staleReviewedScreenshots"] = []
    for name, expected in reviewed.items():
        path = pathlib.Path(name)
        if not path.is_absolute():
            path = ROOT / path
        if not path.is_file() or digest(path) != expected:
            review["staleReviewedScreenshots"].append(name)
    actual = {str(pathlib.Path(name).resolve()) for name in capture.get("screenshots", [])}
    covered = {str((pathlib.Path(name) if pathlib.Path(name).is_absolute() else ROOT / name).resolve()) for name in reviewed}
    review["unreviewedScreenshots"] = sorted(actual - covered)
    if (not reviewed or review["staleReviewedScreenshots"] or review["unreviewedScreenshots"] or
        not capture.get("fingerprint") or review.get("fingerprint") != capture.get("fingerprint")):
        review["status"] = "unverified" if review.get("status") != "failed" else "failed"
    review["userAcceptance"] = False


def memory_probe_history() -> dict:
    runs = sorted((DOC / "Validation").glob("*_MemoryProbe_*.json"), key=lambda path: path.stat().st_mtime)
    runner = read(runs[-1]) if runs else {"status": "unverified"}
    capture = read(DOC / "memory_probe_receipt.json")
    layout = read(DOC / "ui_overflow_probe.json")
    review = read(DOC / "resident_probe_visual_review.json")
    verify_visual_review(review, capture)
    return {"scope": "Seven actual 1920x1080 memory views used to verify placement before the final 77-image capture; this diagnostic does not substitute for that complete review.",
            "runner": runner, "capture": capture, "uiLayout": layout, "actualPixelReview": review,
            "previousCoverageFailure": read(DOC / "Validation/ProbeCoverageFailure/20261007_212547_MemoryProbe_PlacementProbe7.json"),
            "coverageCorrection": "The first probe rendered seven images normally but its layout receipt failed the shared default three-resolution requirement. The isolated capture writer now explicitly requires the probe's 1920x1080 resolution and all four resolutions for the full capture; shared UI evidence code remains unchanged."}


def build_report() -> dict:
    content_path = ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json"
    content = read(content_path)
    city = next((city for city in content.get("cities", []) if city.get("id") == "little_montana"), {})
    counts = {key: len([entry for entry in content.get(key, []) if entry.get("cityId") == "little_montana"])
              for key in ("instances", "vipStories", "vipCollaborations")}
    stories = [entry for entry in content.get("vipStories", []) if entry.get("cityId") == "little_montana"]
    counts["personalChapters"] = sum(len(story.get("chapters", [])) for story in stories)
    counts["recurringVariants"] = sum(len(story.get("recurring", [])) for story in stories)
    counts["quests"] = len([entry for entry in content.get("quests", []) if str(entry.get("focus", "")).startswith("lm_")])
    instance_ids = {entry["id"] for entry in content.get("instances", []) if entry.get("cityId") == "little_montana"}
    counts["routes"] = len([entry for entry in content.get("routes", []) if entry.get("from") in instance_ids])
    gates = {"sourceCompile": source_compile(), "domainEditMode": domain_results(),
             "editorAssets": asset_results(),
             "playMode": play_results(),
             "authoring": authoring(), "preservation": read(DOC / "Preservation/result.json"),
             "contentValidation": read(DOC / "content_validation.json"), "idempotence": read(DOC / "idempotence_receipt.json"),
             "gameView": gameview(), "uiLayout": read(DOC / "ui_overflow.json"),
             "assetAudit": read(DOC / "asset_audit.json"), "visualReview": read(DOC / "visual_review.json")}
    content_gate = gates["contentValidation"]
    current_digest = canonical_digest(content_path)
    content_gate["status"] = "passed" if (
        content_gate.get("contentDigest") == current_digest and content_gate.get("priorContentPreserved") is True and
        content_gate.get("priorLocaleEntriesPreserved") is True and content_gate.get("instances") == counts["instances"] == 56 and
        content_gate.get("personalChapters") == counts["personalChapters"] == 28 and content_gate.get("recurringVariants") == counts["recurringVariants"] == 21 and
        set(content_gate.get("languages", [])) == {"de", "en", "es", "fr", "ru"}
    ) else "unverified"
    content_gate["coverageNote"] = "Authoring receipt and current content digest match; native content rules and prior-entry preservation are separate required gates."
    if gates["uiLayout"].get("passed") is True and gates["uiLayout"].get("issues") == []:
        gates["uiLayout"]["status"] = "passed"
    elif gates["uiLayout"].get("passed") is False or gates["uiLayout"].get("issues"):
        gates["uiLayout"]["status"] = "failed"
    if not gates["gameView"].get("fingerprint") or gates["uiLayout"].get("fingerprint") != gates["gameView"].get("fingerprint"):
        gates["uiLayout"]["status"] = "unverified" if gates["uiLayout"].get("status") != "failed" else "failed"
    verify_visual_review(gates["visualReview"], gates["gameView"])
    newer_inputs(gates["assetAudit"], authored_inputs(), instant(gates["assetAudit"].get("utc")))
    newer_inputs(gates["idempotence"], authored_inputs() + [path for path in source_inputs()
                 if "LittleMontanaAuthoring" in path.name or "LittleMontanaAssetTests" in path.name],
                 instant(gates["idempotence"].get("utc")))
    foundation_case = "Weedtopia.Tests.LittleMontanaCampaignTests.FirstTwelveFoundationQuestsFinishThroughPublicCommandsWithoutPremium"
    foundation_result = gates["domainEditMode"].get("testCases", {}).get(foundation_case)
    foundation = read(DOC / "foundation_simulation.json")
    foundation_start = instant(gates["domainEditMode"].get("supplementalClassRun", gates["domainEditMode"]).get("startedUtc"))
    foundation_utc = instant(foundation.get("utc"))
    reconciled = all(isinstance(foundation.get(key), int) for key in ("initialGrinders", "awardedGrinders", "finalGrinders")) and foundation["finalGrinders"] == foundation["initialGrinders"] + foundation["awardedGrinders"]
    valid_foundation = (foundation_result == "Passed" and gates["domainEditMode"]["status"] == "passed" and reconciled and
                        foundation.get("noPremiumSpend") is True and foundation.get("noRecovery") is True and
                        foundation.get("initialMoney") == 3500 and foundation.get("initialCards") == 3 and
                        foundation.get("quests", 0) >= 12 and str(foundation.get("contentHash", "")).lower() == digest(content_path) and
                        foundation_start is not None and foundation_utc is not None and foundation_utc >= foundation_start)
    gates["foundationSimulation"] = {**foundation, "status": "passed" if valid_foundation else "unverified",
        "testCase": foundation_case, "nativeResult": foundation_result,
        "coverage": "First twelve authored main quests from the 3500-money/3-card charter through public commands, exact grinder award accounting and no recovery; later population growth and a human full playthrough remain separate."}
    gates["preservation"]["baseline"] = relative(DOC / "Preservation/baseline.json")
    technical = ("sourceCompile", "domainEditMode", "editorAssets", "playMode", "authoring", "preservation", "contentValidation", "idempotence", "foundationSimulation", "assetAudit", "gameView", "uiLayout", "visualReview")
    return {"utc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "city": "little_montana", "schemaVersion": 1,
            "contentVersion": content.get("version"), "contentSha256": digest(content_path), "content": counts,
            "unlock": {"residents": city.get("unlockResidents"), "sources": city.get("unlockSources", []),
                       "rule": "Sum current residents of the first three cities; Little Montana does not count; charter acceptance persists."},
            "implementationStatus": "verified" if all(gates[key].get("status") == "passed" for key in technical) else "incomplete",
            "requiredTechnicalGates": list(technical), "gates": gates,
            "diagnosticHistory": {"memoryPlacementProbe": memory_probe_history()},
            "unverifiedGates": [name for name, gate in gates.items() if gate.get("status") != "passed"],
            "releaseAcceptance": {"userVisualApproval": False, "humanFullPlaythrough": "unverified", "physicalTouch": "unverified",
                                  "deviceAudio": "unverified", "playerBuild": "unverified"},
            "releaseAccepted": False, "published": False,
            "notes": ["No original save is used by diagnostics.", "Compilation, native tests, authored scenes, rendered views and human approval remain distinct evidence."]}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--allow-incomplete", action="store_true")
    args = parser.parse_args()
    report = build_report()
    DOC.mkdir(parents=True, exist_ok=True)
    (DOC / "validation_summary.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"implementationStatus": report["implementationStatus"], "content": report["content"],
                      "gates": {name: gate.get("status") for name, gate in report["gates"].items()}, "releaseAccepted": False}, indent=2))
    return 0 if args.allow_incomplete or report["implementationStatus"] == "verified" else 1


if __name__ == "__main__":
    raise SystemExit(main())
