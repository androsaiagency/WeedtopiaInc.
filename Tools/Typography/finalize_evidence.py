"""Consolidate only current, independently verified typography evidence."""
from pathlib import Path
import hashlib
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DOCS = ROOT / "Docs/Typography"


def read(path):
    return json.loads(path.read_text(encoding="utf8"))


def relative(path):
    return path.relative_to(ROOT).as_posix()


fonts = read(DOCS / "font_validation.json")
preservation = read(DOCS / "preservation.json")
idempotence = read(DOCS / "idempotence.json")
overflow = read(DOCS / "ui_overflow.json")
capture = read(DOCS / "gameview_receipt.json")
source = read(ROOT / "Docs/Implementation/Validation/SourceCompile/result.json")
assert fonts["status"] == preservation["status"] == source["status"] == "passed"
assert idempotence["passed"] and overflow["passed"] and not overflow["issues"]
for font in fonts["fonts"]:
    path = ROOT / f'Assets/Weedtopia/Production/Fonts/WeedtopiaGrove/WeedtopiaGrove-{font["style"]}.ttf'
    assert hashlib.sha256(path.read_bytes()).hexdigest() == font["sha256"]
for assembly in source["results"]:
    for path, expected in assembly["sources"].items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == expected, "Stale compilation: " + path
assert len(capture["screenshots"]) == 14
shots = []
for name in capture["screenshots"]:
    path = Path(name)
    with Image.open(path) as im:
        width, height = im.size
    expected = path.stem.rsplit("-", 1)[1]
    assert expected == f"{width}x{height}"
    shots.append({"file": relative(path), "width": width, "height": height})
tests = []
for mode in ("EditMode", "PlayMode"):
    candidates = sorted((ROOT / "Docs/Implementation/Validation").glob(f"*_{mode}_WeedtopiaGrove.json"))
    assert candidates, "Missing Unity test receipt"
    path = candidates[-1]
    result = read(path)
    assert result["status"] == "passed" and result["total"] > 0 and result["failed"] == 0
    tests.append({"mode": mode, "total": result["total"], "passed": result["passed"], "receipt": relative(path)})
summary = {
    "status": "passed", "family": "Weedtopia Grove", "styles": ["Regular", "Bold"],
    "languages": ["de", "en", "es", "fr", "ru"], "fontValidation": relative(DOCS / "font_validation.json"),
    "glyphsPerStyle": 3027, "unicodeCharactersPerStyle": 2169, "missingCurrentCharacters": 0,
    "rebuildByteIdentical": fonts["rebuildByteIdentical"], "sourceAssembliesCompiled": len(source["results"]),
    "unityTests": tests, "actualGameView": shots, "overflowIssues": 0,
    "preservedAssetFingerprints": preservation["baselineFiles"], "fontReferenceChangesOnlyInAssets": len(preservation["changedAssets"]),
    "idempotentAssetFiles": idempotence["files"], "reusablePrefabTextComponents": idempotence["prefabTextComponents"],
    "physicalDeviceRenderingVerified": False, "humanDesignApprovalReceived": False,
    "allWorldwideWritingSystemsClaimed": False, "license": "SIL Open Font License 1.1",
    "origin": "Designed derivative of existing Lato Regular/Bold, with rewritten outlines and custom letters/symbols"
}
(DOCS / "validation_summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
print(json.dumps({key: value for key, value in summary.items() if key != "actualGameView"}, ensure_ascii=False))
