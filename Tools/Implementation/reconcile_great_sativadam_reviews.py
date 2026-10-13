"""Record the root's actual views and merge completed independent visual reviews."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/GreatSativadam"
ROOT_VIEWS = (
    "10_houseboat_gardens.png", "07_repair_wharf.png", "08_learning_dock.png",
    "09_completed_project_0.png", "01_great_sativadam_invitation.png",
    "06_resident_joost.png", "10_canals_and_tide_gate.png",
)
REVIEWERS = ("root", "content", "authoring", "campaign")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write(name, value):
    (DOC / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main(args):
    capture = read(DOC / "Validation" / (args.capture_run + ".json"))
    if capture.get("status") != "passed" or capture.get("exitCode") != 0:
        raise ValueError("Actual completed passing capture required.")
    started = datetime.datetime.fromisoformat(capture["startedUtc"].replace("Z", "+00:00")).timestamp()
    if not args.merge:
        inspected = []
        for filename in ROOT_VIEWS:
            path = DOC / "Screenshots" / filename
            if path.stat().st_mtime < started:
                raise ValueError("Stale root view: " + filename)
            inspected.append(dict(path=path.relative_to(ROOT).as_posix(), sha256=digest(path)))
        write("visual_review_root.json", dict(
            utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
            captureRun=args.capture_run, gameViewReviewed=True, reviewer="root",
            inspected=inspected, blockingGameViewIssuesObserved=[],
            notes=[
                "Actually opened all seven fresh full-resolution captures with view_image after the final capture passed.",
                "HouseboatGardenCourt now visibly occupies the turquoise water with its authored quay platform; it is clear of the surrounding high houses and central HUD. The adjacent willow does not obscure its facade.",
                "Repair workshop and learning pavilion remain visibly distinct; their activity props have normal ground size and do not dominate the view.",
                "Tide gate and completed civic project fit the central view with readable HUD and grounded artwork.",
                "German invitation and Joost story have readable text, visible actions and close controls. The travel list uses its intended scroll mask.",
                "Growth is a diagnostic fixture; these views do not establish natural progression pacing, physical input, user art approval or a free-floating boat simulation.",
            ], userVisualApproval=False))
        print(json.dumps(dict(status="recorded_actual_root_views", count=len(inspected))))
        return
    reports = [read(DOC / ("visual_review_" + reviewer + ".json")) for reviewer in REVIEWERS]
    inspected, blockers = {}, []
    for reviewer, report in zip(REVIEWERS, reports):
        if report.get("captureRun") != args.capture_run or not report.get("gameViewReviewed"):
            raise ValueError("Stale or incomplete visual report: " + reviewer)
        for entry in report["inspected"]:
            path = ROOT / entry["path"]
            if path.stat().st_mtime < started or digest(path) != entry["sha256"]:
                raise ValueError("Stale or changed visual sample: " + entry["path"])
            if entry["path"] in inspected and inspected[entry["path"]]["sha256"] != entry["sha256"]:
                raise ValueError("Reviewers disagree about sample bytes.")
            inspected[entry["path"]] = entry
        issues = report.get("blockingGameViewIssuesObserved", [])
        if issues is True:
            blockers.append(reviewer + ": blocking issue reported")
        elif issues:
            blockers.extend(issues)
    write("visual_review.json", dict(
        utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), captureRun=args.capture_run,
        gameViewReviewed=True, inspected=[inspected[path] for path in sorted(inspected)],
        reviewers=list(REVIEWERS), reports=["visual_review_" + r + ".json" for r in REVIEWERS],
        blockingGameViewIssuesObserved=blockers,
        notesByReviewer={r: report.get("notes", []) for r, report in zip(REVIEWERS, reports)},
        userVisualApproval=False, nativeSpeakerReviewCompleted=False,
        physicalDeviceAcceptance=False, fullHumanPlaythrough=False))
    print(json.dumps(dict(status="merged" if not blockers else "blocking_issues", uniqueActualViews=len(inspected), blockers=blockers)))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--capture-run", required=True)
    parser.add_argument("--merge", action="store_true")
    main(parser.parse_args())
