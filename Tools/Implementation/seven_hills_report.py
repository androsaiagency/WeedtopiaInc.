"""Aggregate Seven Hills evidence without running Unity or promoting unrun gates.

Run from any directory. The default output is Docs/SevenHills/validation_summary.json.
Exit 0 means every implementation gate passed; release acceptance stays separate.
Missing, unfinished, malformed or stale evidence remains unverified. --print-only
prints the report without writing it, and --allow-incomplete suppresses exit 1.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/SevenHills"
VALIDATION = ROOT / "Docs/Implementation/Validation"
CONTENT = ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json"
CITY = "seven_hills"
CAPTURE_FIXTURE = "Assets/Weedtopia/Scripts/Editor/GameSetup/SevenHillsEvidence.cs"
CAPTION_KEY = "sh.joint.panorama_ring.memory"
CAPTION_FILES = {"Assets/Weedtopia/Game/Content/BurningHills.json": ("texts", "de")}
CAPTION_FILES.update({"Assets/Weedtopia/Game/Localization/" + language + ".json": ("entries", "value")
                      for language in ("de", "en", "es", "fr", "ru")})


def relative(path: Path) -> str:
    try:
        return path.resolve().relative_to(ROOT.resolve()).as_posix()
    except ValueError:
        return str(path)


def resolve(value: str | Path) -> Path:
    path = Path(value)
    return path if path.is_absolute() else ROOT / path


def utc(timestamp: float) -> str:
    return datetime.fromtimestamp(timestamp, timezone.utc).isoformat()


def instant(value: object) -> float | None:
    if not isinstance(value, str):
        return None
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
        return parsed.timestamp() if parsed.tzinfo else None
    except ValueError:
        return None


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def receipt(path: Path) -> dict:
    evidence = {"report": relative(path), "status": "unverified"}
    if not path.is_file():
        return dict(evidence, reason="missing_report")
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        if not isinstance(data, dict):
            raise ValueError("The report must be a JSON object")
    except (OSError, ValueError) as error:
        return dict(evidence, reason="unreadable_report", error=str(error))
    evidence.update(data)
    evidence.update(report=relative(path), reportSha256=digest(path), modifiedUtc=utc(path.stat().st_mtime))
    evidence["reportedStatus"] = data.get("status")
    evidence["status"] = data.get("status") if data.get("status") in ("passed", "failed") else "unverified"
    return evidence


def invalidate(gate: dict, reason: str, *, failed: bool = False) -> None:
    gate.setdefault("reasons", []).append(reason)
    gate["status"] = "failed" if failed or gate.get("status") == "failed" else "unverified"


def newer_inputs(gate: dict, paths: list[Path], timestamp: float | None, *, allow_caption: bool = False) -> None:
    if timestamp is None:
        invalidate(gate, "missing_evidence_timestamp")
        return
    changed = sorted({relative(p) for p in paths if p.is_file() and p.stat().st_mtime > timestamp})
    if changed and allow_caption:
        proof = caption_fix()
        if proof.get("verifiedCanonicalChange"):
            masked = sorted(set(changed) & set(proof["verifiedFiles"]))
            if masked:
                gate["verifiedCaptionOnlyInputsAfterEvidence"] = masked
                gate["captionFreshnessException"] = {"key": CAPTION_KEY, "proof": proof["report"],
                    "proofSha256": proof["reportSha256"],
                    "reason": "Only the verified existing Seven Hills caption value changed; all other JSON fields and model data are canonically identical to preserved pre-fix bytes.",
                    "requiredIndependentGate": "nativeCaptionFreshProof"}
                changed = sorted(set(changed) - set(masked))
    if changed:
        gate["inputsModifiedAfterEvidence"] = changed
        invalidate(gate, "stale_inputs")


def canonical(value: object) -> str:
    return hashlib.sha256(json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()


def strict_json(path: Path) -> object:
    def unique_pairs(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("Duplicate JSON property: " + key)
            result[key] = value
        return result
    return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=unique_pairs)


def caption_fix() -> dict:
    """Independently prove the single permitted text change in exactly six input files."""
    proof = receipt(DOC / "caption_fix.json")
    proof["verifiedCanonicalChange"] = False
    records = proof.get("files", [])
    if (proof.get("reportedStatus") != "passed" or proof.get("key") != CAPTION_KEY
            or not CAPTION_KEY.startswith("sh.") or proof.get("changedFiles") != 6
            or not isinstance(records, list) or len(records) != 6
            or {r.get("path") for r in records if isinstance(r, dict)} != set(CAPTION_FILES)):
        invalidate(proof, "caption_fix_not_exact_owned_key_and_six_files")
        return proof
    errors, verified = [], []
    for record in records:
        name = record["path"]
        collection, field = CAPTION_FILES[name]
        current, backup = resolve(name), resolve(record.get("beforeBackupPath", ""))
        expected_backup = DOC / "CaptionFix" / name
        try:
            if record.get("field") != field or backup.resolve() != expected_backup.resolve():
                raise ValueError("Unexpected caption field or backup path")
            if digest(backup) != record.get("beforeSha256") or digest(current) != record.get("afterSha256"):
                raise ValueError("Preserved before bytes or current after bytes do not match recorded SHA-256")
            before, after = strict_json(backup), strict_json(current)
            old_matches = [entry for entry in before.get(collection, []) if entry.get("key") == CAPTION_KEY]
            new_matches = [entry for entry in after.get(collection, []) if entry.get("key") == CAPTION_KEY]
            if len(old_matches) != 1 or len(new_matches) != 1:
                raise ValueError("Exactly one existing owned caption entry must exist before and after")
            old_value, new_value = old_matches[0].get(field), new_matches[0].get(field)
            if (not isinstance(old_value, str) or not isinstance(new_value, str) or not new_value.strip()
                    or old_value != record.get("beforeValue") or new_value != record.get("afterValue")
                    or len(old_value) != record.get("beforeLength") or len(new_value) != record.get("afterLength")
                    or len(new_value) >= len(old_value)):
                raise ValueError("Caption shortening values and lengths are not established")
            old_matches[0][field] = ""
            new_matches[0][field] = ""
            before_hash, after_hash = canonical(before), canonical(after)
            if (before != after or before_hash != after_hash
                    or before_hash != record.get("maskedBeforeCanonicalSha256")
                    or after_hash != record.get("maskedAfterCanonicalSha256")
                    or record.get("onlyCaptionChanged") is not True):
                raise ValueError("A field other than the one permitted caption value changed")
            verified.append(name)
        except (OSError, ValueError, TypeError, AttributeError) as error:
            errors.append({"path": name, "error": str(error)})
    proof["verificationErrors"] = errors
    proof["verifiedFiles"] = sorted(verified)
    proof["verifiedCanonicalChange"] = not errors and len(verified) == 6 and proof.get("allMaskedCanonicalEqual") is True
    proof["runtimeAndModelsUnchanged"] = proof["verifiedCanonicalChange"]
    if not proof["verifiedCanonicalChange"]:
        invalidate(proof, "caption_fix_independent_canonical_verification_failed")
    proof["scopeNote"] = "Exempts only this verified caption value from native/model/authoring freshness; source compile, current content validation, rendered Game View, UI overflow and visual review remain independent required proofs."
    return proof


def input_paths(include_assets: bool = False) -> list[Path]:
    scripts = ROOT / "Assets/Weedtopia/Scripts"
    paths = list(scripts.rglob("*.cs")) + list(scripts.rglob("*.asmdef"))
    paths += list((ROOT / "Assets/Weedtopia/Game/Content").glob("*.json"))
    paths += list((ROOT / "Assets/Weedtopia/Game/Localization").glob("*.json"))
    if include_assets:
        paths += [p for p in (ROOT / "Assets/Weedtopia/Production/SevenHills").rglob("*") if p.is_file()]
        paths += [ROOT / "Assets/Weedtopia/Game/Scenes/SevenHills.unity",
                  ROOT / "Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab"]
    return paths


def domain_input_paths() -> list[Path]:
    return assembly_input_paths("Weedtopia.Game.EditModeTests")[0]


def assembly_input_paths(assembly: str, include_assets: bool = False) -> tuple[list[Path], dict]:
    """Follow actual local asmdef references and exclude independently owned nested assemblies."""
    definitions = {}
    for path in (ROOT / "Assets/Weedtopia/Scripts").rglob("*.asmdef"):
        config = json.loads(path.read_text(encoding="utf-8-sig"))
        definitions[config["name"]] = (path, config)
    closure, external, pending = set(), set(), [assembly]
    while pending:
        name = pending.pop()
        if name in closure:
            continue
        if name not in definitions:
            external.add(name)
            continue
        closure.add(name)
        pending.extend(definitions[name][1].get("references", []))
    paths = []
    for name in sorted(closure):
        path = definitions[name][0]
        nested = [other.parent for other, _ in definitions.values() if other != path and path.parent in other.parents]
        paths.append(path)
        paths.extend(p for p in path.parent.rglob("*.cs") if not any(folder in p.parents for folder in nested))
    paths += list((ROOT / "Assets/Weedtopia/Game/Content").glob("*.json"))
    paths += list((ROOT / "Assets/Weedtopia/Game/Localization").glob("*.json"))
    if include_assets:
        paths += [p for p in (ROOT / "Assets/Weedtopia/Production/SevenHills").rglob("*") if p.is_file()]
        paths += [ROOT / "Assets/Weedtopia/Game/Scenes/SevenHills.unity",
                  ROOT / "Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab"]
    paths = sorted(set(paths))
    exclusions = []
    if assembly in ("Weedtopia.Game.AssetTests", "Weedtopia.Game.PlayModeTests"):
        fixture = ROOT / CAPTURE_FIXTURE
        native_references = [relative(p) for p in paths if p.suffix == ".cs" and "/Scripts/Tests/" in p.as_posix()
                             and "SevenHillsEvidence" in p.read_text(encoding="utf-8-sig")]
        was_dependency = fixture in paths
        if not native_references:
            paths = [p for p in paths if p != fixture]
        elif fixture.is_file() and fixture not in paths:
            paths.append(fixture)
        exclusions.append({"path": CAPTURE_FIXTURE, "excluded": not native_references,
                           "wasAssemblyDependency": was_dependency, "nativeTestReferences": native_references,
                           "reason": ("Native test references detected; the capture-fixture freshness exemption is disabled."
                                      if native_references else
                                      "Owned capture fixture begins through -executeMethod SevenHillsEvidence.Begin; its InitializeOnLoad hook only resumes an already requested capture. Native test classes do not invoke it."),
                           "independentProofGates": ["sourceCompile", "gameView"],
                           "scope": "Only this capture fixture is exempt; all other editor and runtime dependencies retain freshness checks."})
    scope = {"assembly": assembly, "localAssemblyClosure": sorted(closure),
             "asmdefPaths": [relative(definitions[name][0]) for name in sorted(closure)],
             "externalAssemblyReferences": sorted(external), "trackedInputCount": len(paths),
             "nativeInputExclusions": exclusions}
    return paths, scope


def latest_runner(folder: Path, pattern: str) -> dict:
    # An in-progress newest log must not be hidden by an older completed JSON.
    candidates = {p.with_suffix("") for suffix in ("json", "log") for p in folder.glob(pattern + "." + suffix)}
    if not candidates:
        return {"status": "unverified", "reason": "no_matching_run", "search": relative(folder / (pattern + ".json"))}
    selected = max(candidates, key=lambda p: p.name)
    gate = receipt(selected.with_suffix(".json"))
    gate["matchingRuns"] = len(candidates)
    if not selected.with_suffix(".json").exists():
        gate.update(reason="newest_run_has_no_completed_receipt", log=relative(selected.with_suffix(".log")))
    return gate


def source_compile() -> dict:
    gate = receipt(VALIDATION / "SourceCompile/result.json")
    assemblies = gate.get("results", [])
    if not assemblies:
        invalidate(gate, "missing_assembly_results")
        return gate
    recorded: dict[str, str] = {}
    for assembly in assemblies:
        if assembly.get("exitCode") != 0:
            invalidate(gate, "compiler_assembly_failed", failed=True)
        recorded.update(assembly.get("sources", {}))
    gate["assemblies"] = len(assemblies)
    gate["sourceCount"] = len(recorded)
    gate["staleSources"] = sorted(name for name, expected in recorded.items()
                                   if not resolve(name).is_file() or digest(resolve(name)) != expected)
    gate["unrecordedSources"] = sorted(relative(p) for p in (ROOT / "Assets/Weedtopia/Scripts").rglob("*.cs")
                                       if relative(p) not in recorded)
    if not recorded:
        invalidate(gate, "missing_source_hashes")
    if gate["staleSources"] or gate["unrecordedSources"]:
        invalidate(gate, "source_compile_does_not_match_current_sources")
    return gate


def native_tests(mode: str, label: str, sources: list[Path], run: Path | None = None) -> dict:
    gate = receipt(run) if run else latest_runner(VALIDATION, "*_" + mode + "_" + label)
    if gate.get("reason") in ("no_matching_run", "newest_run_has_no_completed_receipt", "unreadable_report"):
        return gate
    started = instant(gate.get("startedUtc"))
    newer_inputs(gate, sources, started, allow_caption=True)
    xml = resolve(gate.get("xml", str(resolve(gate["report"]).with_suffix(".xml"))))
    gate["xml"] = relative(xml)
    if not xml.is_file():
        invalidate(gate, "missing_native_test_xml")
        return gate
    if started is None or xml.stat().st_mtime < started:
        invalidate(gate, "xml_is_not_fresh_for_run")
    try:
        document = ET.parse(xml).getroot()
        cases = list(document.iter("test-case"))
        counts = {key: int(document.get(key, "0")) for key in ("total", "passed", "failed", "skipped")}
    except (OSError, ValueError, ET.ParseError) as error:
        invalidate(gate, "unreadable_native_test_xml")
        gate["xmlError"] = str(error)
        return gate
    gate["xmlCounts"] = counts
    gate["xmlSha256"] = digest(xml)
    gate["testCases"] = len(cases)
    gate["unresolvedTests"] = [{"name": case.get("fullname", case.get("name")), "result": case.get("result")}
                               for case in cases if case.get("result") != "Passed"]
    disagreements = {key: {"runner": gate.get(key), "xml": value} for key, value in counts.items()
                     if gate.get(key) != value}
    if disagreements:
        gate["countDisagreements"] = disagreements
        invalidate(gate, "runner_xml_count_disagreement")
    if gate.get("exitCode") != 0 or document.get("result") != "Passed" or counts["failed"]:
        invalidate(gate, "native_test_run_failed", failed=True)
    elif gate["unresolvedTests"]:
        invalidate(gate, "native_test_cases_not_passed")
    if not cases or counts["total"] <= 0:
        invalidate(gate, "no_native_tests_executed")
    gate["coverageNote"] = "Latest complete labeled native run; tests are not inferred from compiler or process completion."
    return gate


def suite_results(mode: str, label: str, correction_label: str, sources: list[Path]) -> dict:
    """Retain the full-suite coverage and replace only actually repeated test cases."""
    base = native_tests(mode, label, sources)
    if not base.get("xmlSha256"):
        return base
    base_started = instant(base.get("startedUtc"))
    correction_paths = sorted(p for p in VALIDATION.glob("*_" + mode + "_" + correction_label + ".json")
                              if instant(receipt(p).get("startedUtc")) is not None
                              and (base_started is None or instant(receipt(p).get("startedUtc")) >= base_started))
    runs = [base] + [native_tests(mode, correction_label, sources, p) for p in correction_paths]
    final: dict[str, dict] = {}
    original: set[str] = set()
    unverifiable: list[str] = []
    for index, run in enumerate(runs):
        # A failed native suite can be corrected; malformed/stale XML cannot.
        reasons = set(run.get("reasons", [])) - {"native_test_run_failed", "stale_inputs"}
        if reasons or not run.get("xmlSha256"):
            unverifiable.append(run["report"])
            continue
        for case in ET.parse(resolve(run["xml"])).getroot().iter("test-case"):
            name = case.get("fullname", case.get("name"))
            if not name:
                unverifiable.append(run["report"])
                continue
            if index == 0:
                original.add(name)
            final[name] = {"result": case.get("result"), "run": run["report"],
                           "startedUtc": run.get("startedUtc")}
    gate = {"status": "passed", "method": "Full labeled suite plus targeted correction runs; latest result per test fullname",
            "baseRun": base["report"], "originalCounts": base.get("xmlCounts"), "runs": runs,
            "total": len(final), "passed": sum(v["result"] == "Passed" for v in final.values()),
            "failed": sum(v["result"] == "Failed" for v in final.values()),
            "skipped": sum(v["result"] == "Skipped" for v in final.values()),
            "unresolvedTests": [{"name": name, **value} for name, value in final.items() if value["result"] != "Passed"],
            "requiredBaseTests": len(original), "missingBaseTests": sorted(original - final.keys()),
            "finalResults": final, "unverifiableRuns": sorted(set(unverifiable))}
    if gate["unresolvedTests"]:
        invalidate(gate, "native_test_failures_not_superseded", failed=True)
    if not original or len(original) != base.get("xmlCounts", {}).get("total") or gate["missingBaseTests"]:
        invalidate(gate, "full_suite_coverage_not_preserved")
    if unverifiable:
        invalidate(gate, "one_or_more_native_runs_not_verifiable")
    caption = caption_fix()
    caption_paths = set(caption.get("verifiedFiles", [])) if caption.get("verifiedCanonicalChange") else set()
    changed = [p for p in sources if p.is_file() and (base_started is None or p.stat().st_mtime > base_started)
               and relative(p) not in caption_paths]
    masked_captions = sorted(relative(p) for p in sources if p.is_file() and base_started is not None
                            and p.stat().st_mtime > base_started and relative(p) in caption_paths)
    if masked_captions:
        gate["verifiedCaptionOnlyInputsAfterEvidence"] = masked_captions
        gate["captionFreshnessException"] = {"key": CAPTION_KEY, "proof": caption["report"],
                                              "proofSha256": caption["reportSha256"],
                                              "requiredIndependentGate": "nativeCaptionFreshProof"}
    stale, covered = [], []
    for path in changed:
        test_names = [name for name in original if ("." + path.stem + ".") in ("." + name)]
        if "/Scripts/Tests/" in path.as_posix() and path.suffix == ".cs" and test_names and all(
            instant(final[name].get("startedUtc")) is not None
            and instant(final[name].get("startedUtc")) >= path.stat().st_mtime for name in test_names):
            covered.append(relative(path))
        else:
            stale.append(relative(path))
    gate["changedTestSourcesCoveredByCorrections"] = sorted(covered)
    gate["staleSources"] = sorted(stale)
    if stale:
        invalidate(gate, "sources_changed_after_full_suite_without_complete_retest_coverage")
    newest = latest_runner(VALIDATION, "*_" + mode + "_" + correction_label)
    if newest.get("reason") == "newest_run_has_no_completed_receipt":
        gate["pendingCorrection"] = newest
        invalidate(gate, "latest_correction_run_not_completed")
    return gate


def domain_results(sources: list[Path]) -> dict:
    return suite_results("EditMode", "SevenHillsDomain", "SevenHillsDomainCorrections", sources)


def play_results(sources: list[Path]) -> dict:
    return suite_results("PlayMode", "SevenHillsPlay", "SevenHillsPlayCorrections", sources)


def content_counts() -> tuple[dict, dict, list[Path]]:
    content = receipt(CONTENT)
    if "cities" not in content:
        return {}, {"city": CITY, "status": "unverified", "reason": "content_unreadable"}, []
    city = next((c for c in content["cities"] if c.get("id") == CITY), {})
    sites = [s for s in content.get("instances", []) if s.get("cityId", "burning_hills") == CITY]
    ids = {s["id"] for s in sites}
    residents = [r for r in content.get("vipStories", []) if r.get("cityId") == CITY]
    counts = {"sites": len(sites), "routes": sum(r.get("from") in ids for r in content.get("routes", [])),
              "mainQuests": sum(q.get("cityId") == CITY for q in content.get("quests", [])),
              "permanentResidents": len(residents), "personalChapters": sum(len(r.get("chapters", [])) for r in residents),
              "recurringVariants": sum(len(r.get("recurring", [])) for r in residents),
              "sharedProjects": sum(c.get("cityId") == CITY for c in content.get("vipCollaborations", [])),
              "contentVersion": content.get("version"), "source": relative(CONTENT), "sourceSha256": content["reportSha256"]}
    unlock = {"sourceCities": city.get("unlockSources", []), "residentsCombined": city.get("unlockResidents"),
              "oneTimeMoney": city.get("bridgeMoney"), "oneTimeCards": city.get("bridgeCards"), "source": relative(CONTENT)}
    return counts, unlock, [CONTENT] + list((ROOT / "Assets/Weedtopia/Game/Localization").glob("*.json"))


def timestamp(gate: dict) -> float | None:
    return instant(gate.get("utc")) or instant(gate.get("modifiedUtc"))


def authoring(sources: list[Path]) -> dict:
    gate = latest_runner(DOC / "Validation", "*_Install_*")
    authored = receipt(DOC / "authoring_receipt.json")
    gate["authoringReceipt"] = authored
    if authored["status"] != "passed":
        invalidate(gate, "authoring_receipt_not_passed", failed=authored["status"] == "failed")
    if authored.get("runtimeStaticObjects") is not False:
        invalidate(gate, "authored_static_objects_not_confirmed")
    started = instant(gate.get("startedUtc"))
    if started is None or timestamp(authored) is None or timestamp(authored) < started:
        invalidate(gate, "authoring_receipt_is_not_fresh_for_run")
    authored_inputs = [p for p in sources if "SevenHillsAuthoring" in p.name or "/Game/Content/" in p.as_posix()]
    newer_inputs(gate, authored_inputs, timestamp(authored), allow_caption=True)
    return gate


def capture(sources: list[Path]) -> tuple[dict, dict]:
    gate = latest_runner(DOC / "Validation", "*_Capture_FinalGameViews")
    view = receipt(DOC / "gameview_receipt.json")
    gate["captureReceipt"] = view
    layout = receipt(DOC / "ui_overflow.json")
    layout["status"] = "passed" if layout.get("passed") is True and layout.get("issues") == [] else layout["status"]
    if layout.get("passed") is False or layout.get("issues"):
        invalidate(layout, "ui_layout_reported_issues", failed=True)
    started = instant(gate.get("startedUtc"))
    if view.get("mode") != "actual_playmode_gameview":
        invalidate(gate, "actual_playmode_gameview_not_confirmed")
    if started is None or timestamp(view) is None or timestamp(view) < started:
        invalidate(gate, "capture_receipt_is_not_fresh_for_run")
    shots = view.get("screenshots", [])
    gate["screenshots"] = len(shots)
    gate["missingScreenshots"] = [str(p) for p in shots if not resolve(p).is_file()]
    gate["staleScreenshots"] = [str(p) for p in shots if resolve(p).is_file()
                                and (started is None or resolve(p).stat().st_mtime < started)]
    if len(shots) < 25:
        invalidate(gate, "fewer_than_25_required_gameview_captures")
    if gate["missingScreenshots"] or gate["staleScreenshots"]:
        invalidate(gate, "missing_or_stale_gameview_captures")
    if not view.get("fingerprint") or view.get("fingerprint") != layout.get("fingerprint"):
        invalidate(layout, "layout_capture_fingerprint_mismatch")
    if started is None or timestamp(layout) is None or timestamp(layout) < started:
        invalidate(layout, "layout_report_is_not_fresh_for_capture")
    newer_inputs(gate, sources, started)
    newer_inputs(layout, sources, started)
    return gate, layout


def visual_review(gameview: dict) -> dict:
    gate = receipt(DOC / "visual_review.json")
    view = gameview.get("captureReceipt", {})
    if not view.get("fingerprint") or gate.get("fingerprint") != view.get("fingerprint"):
        invalidate(gate, "review_capture_fingerprint_mismatch")
    hashes = gate.get("reviewedScreenshots", {})
    if not isinstance(hashes, dict) or not hashes:
        invalidate(gate, "missing_reviewed_screenshot_hashes")
        return gate
    gate["reviewedScreenshotCount"] = len(hashes)
    gate["staleReviewedScreenshots"] = [name for name, expected in hashes.items()
                                         if not resolve(name).is_file() or digest(resolve(name)) != expected]
    gate["unreviewedScreenshots"] = sorted(set(view.get("screenshots", [])) - set(hashes))
    if gate["staleReviewedScreenshots"] or gate["unreviewedScreenshots"]:
        invalidate(gate, "review_does_not_cover_current_captures")
    gate["userAcceptance"] = False
    gate["acceptanceNote"] = "Assistant visual review does not constitute direct user approval."
    return gate


def build_report() -> dict:
    sources = input_paths()
    assets = input_paths(include_assets=True)
    domain_sources, domain_scope = assembly_input_paths("Weedtopia.Game.EditModeTests")
    asset_sources, asset_scope = assembly_input_paths("Weedtopia.Game.AssetTests", include_assets=True)
    play_sources, play_scope = assembly_input_paths("Weedtopia.Game.PlayModeTests", include_assets=True)
    capture_sources, capture_scope = assembly_input_paths("Weedtopia.Editor.GameSetup", include_assets=True)
    domain = domain_results(domain_sources)
    domain["inputScope"] = domain_scope
    counts, unlock, content_paths = content_counts()
    content = receipt(DOC / "content_validation.json")
    if content.get("valid") is True:
        content["status"] = "passed" if content.get("reportedStatus") != "failed" else "failed"
    newer_inputs(content, content_paths, timestamp(content))
    foundation = receipt(DOC / "foundation_simulation.json")
    foundation_test = next((case for name, case in domain.get("finalResults", {}).items()
                            if name.endswith(".FoundationReachesLocalSupplyUsingPublicCommandsWithoutRecoveryOrPremium")), {})
    foundation["nativeTestResult"] = foundation_test
    foundation["coverageNote"] = "Public-command foundation through 12 quests and local supply; later population chapters and complete human playthrough are separate."
    currency = [foundation.get(k) for k in ("initialGrinders", "awardedGrinders", "finalGrinders")]
    reconciled = all(isinstance(value, int) for value in currency) and currency[2] == currency[0] + currency[1]
    if (foundation_test.get("result") == "Passed" and foundation.get("completedQuests") == 12
            and foundation.get("quest") == "sh_morning_courtyard" and foundation.get("freeplay") is False
            and foundation.get("noPremiumSpend") is True and foundation.get("noRecoveryUsed") is True
            and foundation.get("spentGrinders") == 0 and foundation.get("recoveryCheckpoint") == 0 and reconciled):
        foundation["status"] = "passed" if foundation.get("reportedStatus") != "failed" else "failed"
    else:
        invalidate(foundation, "foundation_supply_and_no_spend_proof_not_demonstrated")
    foundation_time = timestamp(foundation)
    foundation_start = instant(foundation_test.get("startedUtc"))
    if foundation_time is None or foundation_start is None or foundation_time < foundation_start:
        invalidate(foundation, "foundation_receipt_is_not_fresh_for_its_passing_native_test")
    newer_inputs(foundation, [p for p in domain_sources if "/Scripts/Tests/" not in p.as_posix()], timestamp(foundation), allow_caption=True)
    audit = receipt(DOC / "asset_audit.json")
    audit["staleArtSources"] = [a.get("path") for a in audit.get("art", [])
                                if not a.get("path") or not resolve(a["path"]).is_file()
                                or digest(resolve(a["path"])) != a.get("sha256")]
    if audit["staleArtSources"]:
        invalidate(audit, "asset_audit_art_hash_mismatch")
    newer_inputs(audit, [p for p in assets if "SevenHills" in str(p)], timestamp(audit))
    preservation = receipt(DOC / "Preservation/result.json")
    baseline = receipt(DOC / "Preservation/baseline.json")
    preservation["baseline"] = relative(DOC / "Preservation/baseline.json")
    if baseline.get("reason"):
        invalidate(preservation, "missing_preservation_baseline")
    else:
        protected = [resolve(name) for name in baseline if name.startswith(("Assets/", "ProjectSettings/"))]
        newer_inputs(preservation, protected, timestamp(preservation))
    gameview, layout = capture(capture_sources)
    gameview["inputScope"] = capture_scope
    editor_assets = native_tests("EditMode", "SevenHillsAssets", asset_sources)
    editor_assets["inputScope"] = asset_scope
    play_mode = play_results(play_sources)
    play_mode["inputScope"] = play_scope
    gates = {"sourceCompile": source_compile(),
             "domain": domain,
             "editorAssets": editor_assets,
             "playMode": play_mode,
             "authoring": authoring(sources), "foundationSimulation": foundation,
             "contentValidation": content, "preservation": preservation,
             "assetAudit": audit, "gameView": gameview, "uiLayout": layout,
             "visualReview": visual_review(gameview)}
    if (DOC / "caption_fix.json").is_file():
        caption = caption_fix()
        independent = ("sourceCompile", "contentValidation", "gameView", "uiLayout", "visualReview")
        caption["independentGateStatuses"] = {name: gates[name]["status"] for name in independent}
        caption["currentContentSha256"] = digest(CONTENT) if CONTENT.is_file() else None
        if content.get("contentSha256") != caption["currentContentSha256"]:
            invalidate(caption, "caption_current_content_validation_hash_mismatch")
        fixed = timestamp(caption)
        captured = instant(gameview.get("startedUtc"))
        if fixed is None or captured is None or captured < fixed:
            invalidate(caption, "caption_requires_a_complete_capture_started_after_its_fix")
        if any(gates[name]["status"] != "passed" for name in independent):
            invalidate(caption, "caption_independent_presentation_proofs_not_complete")
        gates["nativeCaptionFreshProof"] = caption
    authored = gates["authoring"].get("authoringReceipt", {})
    counts.update(hills=authored.get("hills"), buildingVariants=authored.get("buildingVariants"),
                  authoredCountsSource=authored.get("report"))
    required = list(gates)
    verified = all(gates[name].get("status") == "passed" for name in required)
    return {"utc": datetime.now(timezone.utc).isoformat(), "schemaVersion": 1, "city": CITY,
            "implementationStatus": "verified" if verified else "incomplete", "content": counts, "unlock": unlock,
            "requiredImplementationGates": required, "gates": gates,
            "unverifiedGates": [name for name in required if gates[name].get("status") != "passed"],
            "releaseAcceptance": {"userVisualApproval": False, "humanPlaythrough": "unverified",
                                  "physicalTouch": "unverified", "deviceAudio": "unverified", "playerBuild": "unverified"},
            "releaseAccepted": False, "published": False, "diagnosticSaveOnly": True,
            "notes": ["Source compilation, native tests, authoring, rendered captures and visual review are separate gates.",
                      "Capture growth fixtures do not prove complete human progression or physical-device usability.",
                      "The report generator starts no Unity process and modifies no assets, saves or project settings."]}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--print-only", action="store_true", help="Print full JSON without writing the summary file")
    parser.add_argument("--allow-incomplete", action="store_true", help="Exit 0 after aggregation even when implementation gates remain open")
    args = parser.parse_args()
    report = build_report()
    if args.print_only:
        # Windows redirected consoles can use cp1252; JSON escapes preserve all languages.
        print(json.dumps(report, ensure_ascii=True, indent=2))
    else:
        DOC.mkdir(parents=True, exist_ok=True)
        output = DOC / "validation_summary.json"
        temporary = output.with_suffix(".json.tmp")
        temporary.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        temporary.replace(output)
        print(json.dumps({"report": relative(output), "implementationStatus": report["implementationStatus"],
                          "gates": {name: gate["status"] for name, gate in report["gates"].items()},
                          "content": report["content"], "releaseAccepted": False}, ensure_ascii=True, indent=2))
    return 0 if args.allow_incomplete or report["implementationStatus"] == "verified" else 1


if __name__ == "__main__":
    raise SystemExit(main())
