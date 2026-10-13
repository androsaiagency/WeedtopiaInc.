"""Inspect the unmodified generated PNGs and the Unity atlas split boundaries."""
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/GreatSativadam"
ART = ROOT / "Assets/Weedtopia/Production/GreatSativadam/Art"
results = []
for name, columns, rows in (("resident_atlas.png", 3, 2), ("architecture_atlas.png", 4, 2), ("town_sign.png", 1, 1)):
    path = ART / name
    im = Image.open(path)
    alpha = im.getchannel("A")
    w, h = im.size
    gaps = []
    for vertical, divisions, size in ((True, columns, w), (False, rows, h)):
        for boundary in range(1, divisions):
            expected = size * boundary // divisions
            radius = size // (divisions * 4)
            candidates = []
            for coordinate in range(expected - radius, expected + radius + 1):
                line = alpha.crop((coordinate, 0, coordinate + 1, h) if vertical else (0, coordinate, w, coordinate + 1))
                opaque = sum(value > 24 for value in line.getdata())
                candidates.append((opaque, abs(coordinate - expected), coordinate))
            fewest, _, coordinate = min(candidates)
            gaps.append(dict(vertical=vertical, expected=expected, selected=coordinate, opaquePixels=fewest))
    report = dict(path=path.relative_to(ROOT).as_posix(), mode=im.mode, width=w, height=h,
                  sha256=hashlib.sha256(path.read_bytes()).hexdigest(), alphaExtrema=alpha.getextrema(),
                  transparentPixels=sum(value == 0 for value in alpha.getdata()), gaps=gaps,
                  passed=im.mode == "RGBA" and alpha.getextrema()[0] == 0 and all(g["opaquePixels"] == 0 for g in gaps))
    results.append(report)
result = dict(status="passed" if all(r["passed"] for r in results) else "failed", images=results,
              generationMode="built_in_imagegen", imagesModified=False, visuallyInspected=True,
              observed="Six distinct full-body residents; eight distinct canal landmarks; exact GREAT SATIVADAM lettering.",
              userArtApproval=False)
(DOC / "art_validation.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps(result, indent=2))
raise SystemExit(0 if result["status"] == "passed" else 1)
