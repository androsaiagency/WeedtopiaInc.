"""Capture pre-migration assets and prove that only the font references changed."""
from pathlib import Path
import argparse
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
BASELINE = ROOT / ".utmp/WeedtopiaFontBaseline"
OLD = ("7ab083661c9483c4fbf3606d8f1af552", "aa9aa54790670f34b9fe097036900ab9")


def files():
    weedtopia = ROOT / "Assets/Weedtopia"
    return sorted(p for p in weedtopia.rglob("*") if p.is_file() and
                  (p.suffix in (".unity", ".prefab", ".asset", ".json", ".meta") or
                   p.name in ("Lato-Regular.ttf", "Lato-Bold.ttf", "OFL.txt")) and
                  "WeedtopiaGrove" not in str(p))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def snapshot():
    if (BASELINE / "manifest.json").exists():
        raise RuntimeError("Baseline already exists; never replace the pre-migration evidence")
    BASELINE.mkdir(parents=True, exist_ok=True)
    manifest = {}
    for p in files():
        rel = p.relative_to(ROOT)
        manifest[rel.as_posix()] = sha(p)
        if p.suffix in (".unity", ".prefab", ".asset"):
            dest = BASELINE / rel
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(p.read_bytes())
    (BASELINE / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf8")
    print(f"Preserved {len(manifest)} asset fingerprints before font migration")


def verify():
    manifest = json.loads((BASELINE / "manifest.json").read_text(encoding="utf8"))
    new = tuple((ROOT / f"Assets/Weedtopia/Production/Fonts/WeedtopiaGrove/WeedtopiaGrove-{style}.ttf.meta").read_text().split("guid: ")[1].splitlines()[0] for style in ("Regular", "Bold"))
    changed, errors = [], []
    for rel, before in manifest.items():
        p = ROOT / rel
        if not p.exists():
            errors.append("Removed: " + rel)
            continue
        if sha(p) == before:
            continue
        old_file = BASELINE / rel
        if not old_file.exists():
            errors.append("Unexpected change: " + rel)
            continue
        expected = old_file.read_bytes()
        for old_guid, new_guid in zip(OLD, new):
            expected = expected.replace(old_guid.encode(), new_guid.encode())
        if expected != p.read_bytes():
            errors.append("Changed beyond font references: " + rel)
        changed.append(rel)
    result = {"status": "passed" if not errors else "failed", "baselineFiles": len(manifest),
              "changedAssets": changed, "allowedChanges": "Only Lato to Weedtopia Grove font/material GUID substitutions", "errors": errors}
    (ROOT / "Docs/Typography/preservation.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf8")
    print(json.dumps(result))
    assert not errors, errors


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=("snapshot", "verify"))
    args = parser.parse_args()
    (snapshot if args.mode == "snapshot" else verify)()
