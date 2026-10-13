"""Reconcile actual content, source, Unity, preservation, and visual evidence."""
import argparse
import datetime
import hashlib
import json
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/GreatSativadam"
CONTENT = "Assets/Weedtopia/Game/Content/BurningHills.json"
LANGUAGES = ("de", "en", "es", "fr", "ru")
CATALOGS = [CONTENT, *[f"Assets/Weedtopia/Game/Localization/{lang}.json" for lang in LANGUAGES]]


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stamp(value):
    return datetime.datetime.fromisoformat(value.replace("Z", "+00:00")).timestamp()


def main(args):
    checks, errors = [], []

    def check(name, ok):
        checks.append(dict(check=name, passed=bool(ok)))
        if not ok:
            errors.append(name)

    content = read(ROOT / CONTENT)
    city = next(c for c in content["cities"] if c["id"] == "great_sativadam")
    sources = [c["id"] for c in content["cities"] if c["id"] != city["id"]]
    people = [p for p in content["vipStories"] if p["cityId"] == city["id"]]
    projects = [p for p in content["vipCollaborations"] if p["cityId"] == city["id"]]
    sites = [b for b in content["instances"] if b["cityId"] == city["id"]]
    site_ids = {b["id"] for b in sites}
    routes = [r for r in content["routes"] if r["from"] in site_ids]
    check("Version14 and thirteen authored cities", content["version"] == 14 and len(content["cities"]) == 13)
    check("20 million combined residents from twelve preceding cities", city["unlockResidents"] == 20000000 and city["unlockSources"] == sources and not city["sourceCity"])
    check("One-time17500 money and9 team cards", (city["bridgeMoney"], city["bridgeCards"]) == (17500, 9))
    check("Six permanent residents,18 personal chapters,18 recurring tasks", len(people) == 6 and sum(len(p["chapters"]) for p in people) == 18 and sum(len(p["recurring"]) for p in people) == 18)
    check("Three chapter-linked civic projects", len(projects) == 3)
    check("56 sites and35 city-local goods routes", len(sites) == 56 and len(routes) == 35 and all(r["to"] in site_ids for r in routes))
    check("18 main quests", len([q for q in content["quests"] if q.get("cityId") == city["id"]]) == 18)
    joost = next(p for p in people if p["id"] == "gs_joost")
    check("Two durable Joost decisions", {c["id"] for c in joost["chapters"][2]["choices"]} == {"repair_wharf", "learning_dock"})
    old = read(DOC / "Preservation/Before" / CONTENT)
    old_house = next(b for b in old["buildings"] if b["id"] == "residential_house")
    house = next(b for b in content["buildings"] if b["id"] == "residential_house")
    check("Only last housing tier bonus1000 to2000", old_house["residentTierBonuses"] == [0] * 24 + [1000] and house["residentTierBonuses"] == [0] * 24 + [2000])
    maximum = round((house["baseResidents"] * 25 + 2000) * (1 + content["workerAttributeStep"] * 100 * 10)) * 17 * 12
    check("Legal predecessor capacity22108500 exceeds unlock threshold", maximum == 22108500)
    preservation = read(DOC / "Preservation/preservation_receipt.json")
    strict = read(DOC / "Preservation/prefab_result.json")
    check("Actual dirty checkout preserved", preservation["status"] == "passed")
    check("All twelve previous city scenes andBootstrap byte-identical", strict["status"] == "passed" and strict["previousCityScenes"] == 12 and strict["sharedCampaign"]["travelRowsAfter"] == 13)
    compile_receipt = read(ROOT / "Docs/Implementation/Validation/SourceCompile/result.json")
    check("All source assemblies compile", compile_receipt["status"] == "passed" and all(r["exitCode"] == 0 for r in compile_receipt["results"]))
    check("Compiled source hashes match final files", all(digest(ROOT / name) == value for assembly in compile_receipt["results"] for name, value in assembly["sources"].items()))
    author = read(DOC / "authoring_receipt.json")
    check("Explicit Unity authoring passed", author["status"] == "passed" and author["contentVersion"] == 14 and not author["runtimeStaticObjects"])
    authoring_repeat = read(DOC / "authoring_idempotence_receipt.json")
    check("Repeated explicit Unity installation byte-idempotent", authoring_repeat["status"] == "passed" and authoring_repeat["byteIdentical"] and not authoring_repeat["changed"] and not authoring_repeat["added"] and not authoring_repeat["removed"])
    activities = read(DOC / "joost_activity_repair_receipt.json")
    def vector_matches(actual, expected):
        return all(abs(actual[axis] - expected[axis]) < .001 for axis in ("x", "y", "z"))
    check("Four Joost activities use authored ground offsets and independent prop size", activities["status"] == "passed" and len(activities["activities"]) == 4 and all(vector_matches(a["after"]["groundOffset"], a["expectedGroundOffset"]) and vector_matches(a["after"]["worldScale"], a["expectedWorldScale"]) for a in activities["activities"]))
    waterfront = read(DOC / "houseboat_waterfront_repair_receipt.json")
    check("Houseboat berth occupies existing water and normal camera reaches it", waterfront["status"] == "passed" and waterfront["waterAnchorVerified"] and waterfront["normalCameraReachable"] and len(waterfront["cameraFrames"]) == 4 and waterfront["afterPlan"] == {"x": 176.0, "y": 236.0})
    idempotence = read(DOC / "content_idempotence_receipt.json")
    idempotent_files = {name.replace("\\", "/"): value for name, value in idempotence["files"].items()}
    check("Content authoring byte-idempotent", idempotence["byteIdenticalOnSecondRun"] and not idempotence["changedOnSecondRun"] and set(idempotent_files) == set(CATALOGS) and all(digest(ROOT / name) == value for name, value in idempotent_files.items()))
    content_receipt = read(DOC / "content_receipt.json")
    check("230 authored text keys have five-language placeholder parity", content_receipt["localizedKeys"] == 230 and set(content_receipt["languages"]) == set(LANGUAGES) and content_receipt["placeholderParity"])
    polish = read(DOC / "localization_polish_receipt.json")
    check("Two new Joost locale values explicitly polished without changing other entries", polish["status"] == "passed" and polish["unchangedOtherEntries"] and polish["unchangedOtherRawBytes"] and polish["gameDataUnchanged"] and len(polish["changedKeys"]) == 2)
    check("Generated art and transparent atlas splits validated", read(DOC / "art_validation.json")["status"] == "passed")
    check("New asset references validated", read(DOC / "asset_reference_receipt.json")["status"] == "passed")
    latest_catalog = max((ROOT / name).stat().st_mtime for name in CATALOGS)
    latest_gameplay_catalog = (ROOT / CONTENT).stat().st_mtime
    latest_localization_catalog = max((ROOT / name).stat().st_mtime for name in CATALOGS[1:])
    tests, cases, first_failures = [], {}, set()
    receipts = sorted((read(ROOT / relative) for relative in args.tests), key=lambda r: r["startedUtc"])
    for receipt in receipts:
        xml = Path(receipt["xml"])
        tree = ET.parse(xml).getroot()
        test_cases = tree.findall(".//test-case")
        passed = sum(c.attrib["result"] == "Passed" for c in test_cases)
        failed = sum(c.attrib["result"] == "Failed" for c in test_cases)
        check("NUnit actual results consistent " + receipt["run"], len(test_cases) == receipt["total"] == int(tree.attrib["total"]) and passed == receipt["passed"] and failed == receipt["failed"] and passed + failed == len(test_cases) > 0)
        check("NUnit process andresult agree " + receipt["run"], (receipt["status"], receipt["exitCode"]) == (("passed", 0) if failed == 0 else ("failed", 2)) and tree.attrib["result"] in ({"Passed"} if failed == 0 else {"Failed", "Failed(Child)"}))
        check("NUnit XML belongs to this run " + receipt["run"], stamp(receipt["startedUtc"]) <= xml.stat().st_mtime <= stamp(receipt["endedUtc"]) + 2)
        check("NUnit cases unique withinrun " + receipt["run"], len({c.attrib["fullname"] for c in test_cases}) == len(test_cases))
        # Domain runs must use the final gameplay catalog. A later prose-only edit
        # requires fresh localization cases and final rendered language captures;
        # it does not require repeating unrelated hour-long economic simulations.
        check("NUnit used final gameplay catalog " + receipt["run"], stamp(receipt["startedUtc"]) >= latest_gameplay_catalog)
        for case in test_cases:
            name = case.attrib["fullname"]
            cases[name] = dict(result=case.attrib["result"], run=receipt["run"], startedUtc=receipt["startedUtc"], fixture=case.attrib.get("classname", ""))
            if case.attrib["result"] == "Failed":
                first_failures.add(name)
        tests.append(dict(run=receipt["run"], status=receipt["status"], total=receipt["total"], passed=passed, failed=failed, xml=str(xml), sha256=digest(xml)))
    check("Every executed NUnit case has a passing latest result", len(cases) > 0 and all(c["result"] == "Passed" for c in cases.values()))
    localization_cases = [c for c in cases.values() if c["fixture"] in {"Weedtopia.Tests.EditMode.LocalizationTests", "Weedtopia.Tests.Assets.GreatSativadamAssetTests"}]
    check("Localization service rerun and final authored scene checks", {c["fixture"] for c in localization_cases} == {"Weedtopia.Tests.EditMode.LocalizationTests", "Weedtopia.Tests.Assets.GreatSativadamAssetTests"} and all(c["result"] == "Passed" and stamp(c["startedUtc"]) >= latest_localization_catalog for c in localization_cases))
    fixtures = {c["fixture"] for c in cases.values()}
    required = {"Weedtopia.Tests.GreatSativadamCampaignTests", "Weedtopia.Tests.Assets.GreatSativadamAssetTests", "Weedtopia.Tests.PlayMode.GreatSativadamPlayModeTests", "Weedtopia.Tests.SkunkfurtCampaignTests", "Weedtopia.Tests.PersistenceTests", "Weedtopia.Tests.CityCampaignTests"}
    check("New city,previous city,persistence andcampaign regression executed", required <= fixtures)
    geometry_case = "Weedtopia.Tests.Assets.GreatSativadamAssetTests.JoostBranchActivitiesKeepGroundOffsetsAndSpriteBoundsIndependentOfArchitectureScale"
    check("Authored Joost branch ground geometry regression executed", geometry_case in cases and cases[geometry_case]["result"] == "Passed")
    asset_source_time = (ROOT / "Assets/Weedtopia/Scripts/Tests/EditorAssets/GreatSativadamAssetTests.cs").stat().st_mtime
    final_city_asset_cases = [case for case in cases.values() if case["fixture"] == "Weedtopia.Tests.Assets.GreatSativadamAssetTests"]
    check("Seven final city asset cases rerun with actual waterfront geometry", len(final_city_asset_cases) == 7 and all(case["result"] == "Passed" and stamp(case["startedUtc"]) >= asset_source_time for case in final_city_asset_cases))
    timeout_patch = read(DOC / "offline_nunit_timeout_preservation.json")
    check("Offline test timeout repair retains every functional assertion", timeout_patch["status"] == "passed" and timeout_patch["byteReversiblePatch"] and timeout_patch["functionalAssertionsUnchanged"] and timeout_patch["afterSha256"] == digest(ROOT / "Assets/Weedtopia/Scripts/Tests/EditMode/OfflineWorkerTests.cs"))
    long_offline_cases = {
        "Weedtopia.Tests.OfflineWorkerTests.CancelledCalculationKeepsSessionBusyUntilRollbackAndNeverSaves",
        "Weedtopia.Tests.OfflineWorkerTests.FailedCommitRollsBackEveryFieldAndReferenceAndRetryGrantsVipOnce",
        "Weedtopia.Tests.OfflineWorkerTests.WorkerPreservesUtcRollbackZeroElapsedAndEightHourCap(86400)",
    }
    offline_source_time = (ROOT / "Assets/Weedtopia/Scripts/Tests/EditMode/OfflineWorkerTests.cs").stat().st_mtime
    check("All three long offline cases rerun after both timeout repairs", long_offline_cases <= cases.keys() and all(cases[name]["result"] == "Passed" and stamp(cases[name]["startedUtc"]) >= offline_source_time for name in long_offline_cases))
    capture = max((r for p in (DOC / "Validation").glob("*Capture*.json") if (r := read(p)).get("mode") == "Capture"), key=lambda r: r["startedUtc"])
    game = read(DOC / "gameview_receipt.json")
    overflow = read(DOC / "ui_overflow.json")
    check("Owned Unity rendered final catalog", capture["status"] == "passed" and capture["exitCode"] == 0 and stamp(capture["startedUtc"]) >= latest_catalog)
    waterfront_times = [stamp(waterfront["utc"])] + [
        (ROOT / name).stat().st_mtime for name in (
            "Assets/Weedtopia/Production/GreatSativadam/Prefabs/TideCommons.prefab",
            "Assets/Weedtopia/Scripts/Editor/GameSetup/GreatSativadamAuthoring.cs",
            "Assets/Weedtopia/Scripts/Editor/GameSetup/GreatSativadamEvidence.cs",
        )
    ]
    check("GameView captured after final waterfront repair and evidence sources", stamp(capture["startedUtc"]) >= max(waterfront_times))
    player = read(DOC / "player_state_preservation.json")
    check("Actual player saves and original language preserved during capture", player["run"] == capture["run"] and player["status"] == "passed" and player["saveFilesUnchanged"] and player["languageRestored"] and not player["errors"])
    check("Four-resolution UI overflow passed", overflow["passed"] and not overflow["issues"])
    for flag in ("savedStoryRoundtrip", "housingInspectionMatchesSimulation", "returnedToPreviousCity", "allThirteenCitiesVisited", "thirteenCityOfflineRoundtrip", "joostChoiceVariantsVerified", "architectureGroundAnchorsVerified"):
        check("GameView " + flag, game.get(flag) is True)
    check("All thirteen destinations rendered", set(game["visitedCities"]) == set(sources + [city["id"]]))
    check("Both Joost outcomes rendered", set(game["verifiedChoices"]) == {"repair_wharf", "learning_dock"})
    screenshots = []
    for entry in game["dimensions"]:
        path = ROOT / entry["path"]
        data = path.read_bytes()
        check("Fresh exact PNG " + path.name, data[:8] == b"\x89PNG\r\n\x1a\n" and struct.unpack(">II", data[16:24]) == (entry["width"], entry["height"]) and path.stat().st_mtime >= stamp(capture["startedUtc"]))
        screenshots.append(dict(path=entry["path"], sha256=digest(path)))
    check("89 distinct GameView captures", len(screenshots) == len({s["path"] for s in screenshots}) == 89)
    sizes = {(1920, 1080), (1366, 768), (2560, 1080), (1080, 1920)}
    expected = {(scenario, lang, w, h) for scenario in ("initial_quest", "travel_final_row", "joost_choice") for lang in LANGUAGES for w, h in sizes}
    actual = {(r["scenario"], r["language"], r["width"], r["height"]) for r in game["matrix"]}
    check("Full60-case language andresolution matrix", actual == expected and len(game["matrix"]) == 60)
    review = read(DOC / "visual_review.json")
    screenshot_paths = {s["path"] for s in screenshots}
    reviewed_paths = {item["path"] for item in review["inspected"]}
    check("Final GameView samples visually inspected", review["gameViewReviewed"] and review["captureRun"] == capture["run"] and not review["blockingGameViewIssuesObserved"] and len(reviewed_paths) == len(review["inspected"]) >= 12 and all(item["path"] in screenshot_paths and digest(ROOT / item["path"]) == item["sha256"] for item in review["inspected"]))
    check("All twelve original city arrival captures visually inspected", all(f"Docs/GreatSativadam/Screenshots/11_thirteen_city_{source}.png" in reviewed_paths for source in sources))
    result = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), status="passed_local_integration" if not errors else "failed", technicalIntegrationComplete=not errors,
                  city=city["id"], unlockResidents=20000000, maximumPreviousCityPopulation=maximum, tests=tests, uniquePassingNUnitCases=sum(c["result"] == "Passed" for c in cases.values()), totalTestExecutions=sum(t["total"] for t in tests),
                  initialFailures=tests[0]["failed"] if tests else 0, historicallyFailedCases=len(first_failures), unresolvedTestFailures=[name for name, c in cases.items() if c["result"] != "Passed"],
                  finalCasesPerFixture={f: sum(c["fixture"] == f for c in cases.values()) for f in sorted(fixtures)}, sourceCompiledAssemblies=len(compile_receipt["results"]),
                  gameView=dict(screenshots=len(screenshots), matrix=len(game["matrix"]), visuallyInspectedSamples=len(review["inspected"]), visitedCities=game["visitedCities"], captureRun=capture["run"]),
                  checks=checks, errors=errors, diagnosticGrowthFixtures=True, fullHumanPlaythrough=False, physicalDeviceAcceptance=False, releaseAccepted=False, userArtApproval=False, nativeSpeakerReviewCompleted=False)
    (DOC / "validation_summary.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in result.items() if k != "checks"}, indent=2))
    return not errors


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--tests", nargs="+", required=True)
    raise SystemExit(0 if main(parser.parse_args()) else 1)
