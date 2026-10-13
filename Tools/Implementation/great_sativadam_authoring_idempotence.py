"""Compare productive assets before and after an explicit installer repeat."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/GreatSativadam"
BASELINE = DOC / "authoring_idempotence_before.json"
REPORT = DOC / "authoring_idempotence_receipt.json"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def files():
    paths = [p for p in (ROOT / "Assets/Weedtopia/Production/GreatSativadam").rglob("*") if p.is_file()]
    paths += [ROOT / name for name in (
        "Assets/Weedtopia/Production/GreatSativadam.meta",
        "Assets/Weedtopia/Game/Scenes/GreatSativadam.unity",
        "Assets/Weedtopia/Game/Scenes/GreatSativadam.unity.meta",
        "Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab",
        "Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab.meta",
        "ProjectSettings/EditorBuildSettings.asset")]
    return {p.relative_to(ROOT).as_posix(): digest(p) for p in paths}


parser = argparse.ArgumentParser()
parser.add_argument("mode", choices=("capture", "verify"))
parser.add_argument("--baseline", default=BASELINE.name, help="A new named baseline after an explicit owned-asset repair; retain earlier evidence.")
args = parser.parse_args()
if Path(args.baseline).name != args.baseline or not args.baseline.endswith(".json"):
    raise SystemExit("Baseline must be a JSON filename within Docs/GreatSativadam.")
BASELINE = DOC / args.baseline
current = files()
if args.mode == "capture":
    if BASELINE.exists():
        raise SystemExit("Retain the existing first-repeat baseline; do not overwrite evidence.")
    BASELINE.write_text(json.dumps(current, indent=2), encoding="utf-8")
    print(json.dumps(dict(status="captured", files=len(current))))
else:
    before = json.loads(BASELINE.read_text(encoding="utf-8"))
    changed = [name for name, value in before.items() if name in current and current[name] != value]
    added, removed = sorted(current.keys() - before.keys()), sorted(before.keys() - current.keys())
    receipt = dict(status="passed" if not (changed or added or removed) else "failed", baseline=BASELINE.relative_to(ROOT).as_posix(), checkedFiles=len(before),
                   byteIdentical=not (changed or added or removed), changed=changed, added=added, removed=removed)
    REPORT.write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps(receipt, indent=2))
    raise SystemExit(0 if receipt["status"] == "passed" else 1)
