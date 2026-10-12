"""Prove the exact additive localized labels since the full domain run."""
import datetime
import hashlib
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/LittleMontana"
CONTENT = ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json"


def canonical(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, ensure_ascii=False,
                                    separators=(",", ":")).encode("utf-8")).hexdigest()


def without(content, keys):
    return {**content, "texts": [entry for entry in content["texts"] if entry["key"] not in keys]}


def main():
    content = json.loads(CONTENT.read_text(encoding="utf-8-sig"))
    first = json.loads((DOC / "shared_city_text_update.json").read_text(encoding="utf-8-sig"))
    keys = ["campaign.shared_cities", "campaign.travel_action"]
    present = all(sum(entry["key"] == key for entry in content["texts"]) == 1 for key in keys)
    prior_matches = canonical(without(content, {keys[1]})) == first["currentCanonicalDigest"]
    original_matches = canonical(without(content, set(keys))) == first["preRunCanonicalDigest"]
    report = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                  status="passed" if present and prior_matches and original_matches else "failed",
                  addedTextKeys=keys, preRunCanonicalDigest=first["preRunCanonicalDigest"],
                  currentCanonicalDigest=canonical(content),
                  currentWithoutAddedTextDigest=canonical(without(content, set(keys))),
                  previousCanonicalDigest=first["currentCanonicalDigest"],
                  currentWithoutTravelActionDigest=canonical(without(content, {keys[1]})),
                  contentSha256=hashlib.sha256(CONTENT.read_bytes()).hexdigest(),
                  originalProof="Docs/LittleMontana/shared_city_text_update.json",
                  coverage="Removing exactly the two additive campaign labels reproduces the whole catalog digest before the full domain run. Removing only the travel action reproduces the previous current digest. No mechanics, quests, resident stories, goals, rewards or existing text changed.")
    (DOC / "domain_text_update.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0 if report["status"] == "passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
