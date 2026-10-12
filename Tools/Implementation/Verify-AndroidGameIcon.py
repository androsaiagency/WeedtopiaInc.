"""Verify the selected game icon inside an APK without rewriting image pixels."""
import argparse
import io
import json
import math
import pathlib
import re
import zipfile
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument("--apk", required=True, type=pathlib.Path)
parser.add_argument("--source", required=True, type=pathlib.Path)
parser.add_argument("--foreground", required=True, type=pathlib.Path)
args = parser.parse_args()
original = Image.open(args.source).convert("RGBA")
foreground_source = Image.open(args.foreground).convert("RGBA")

def sample_difference(actual, expected):
    total = count = 0
    for row in range(24):
        for col in range(24):
            ax = min(actual.width - 1, int((col + 0.5) / 24 * actual.width))
            ay = min(actual.height - 1, int((row + 0.5) / 24 * actual.height))
            # Align both samples to the packaged pixel center, not two independently rounded positions.
            u, v = (ax + 0.5) / actual.width, (ay + 0.5) / actual.height
            a = actual.getpixel((ax, ay))
            b = expected.getpixel((min(expected.width - 1, int(u * expected.width)),
                                   min(expected.height - 1, int(v * expected.height))))
            if a[3] < 64 and b[3] < 64:
                continue
            for channel in range(4):
                total += abs(a[channel] * (a[3] / 255 if channel < 3 else 1) - b[channel] * (b[3] / 255 if channel < 3 else 1))
                count += 1
    return round(total / max(count, 1), 3)

def alpha_radius(image):
    alpha = image.getchannel("A")
    pixels = alpha.load()
    return max((math.hypot((x + 0.5) / image.width - 0.5,
                           (y + 0.5) / image.height - 0.5)
                for y in range(image.height) for x in range(image.width)
                if pixels[x, y] >= 16), default=1.0)

checks = []
with zipfile.ZipFile(args.apk) as archive:
    paths = archive.namelist()
    adaptive_xml = [p for p in paths if re.match(r"res/mipmap-anydpi-v26/app_icon(?:_round)?\.xml$", p)]
    for path in paths:
        if not re.match(r"res/mipmap-[^/]+/(app_icon(?:_round)?|ic_launcher_(foreground|background))\.png$", path):
            continue
        with Image.open(io.BytesIO(archive.read(path))) as native_image:
            image = native_image.convert("RGBA")
        check = {"resource": path, "size": list(image.size)}
        if path.endswith("/ic_launcher_foreground.png"):
            radius = alpha_radius(image)
            difference = sample_difference(image, foreground_source)
            check.update(maxRadius=radius, imageDifference=difference,
                         passed=radius <= 1 / 3 and difference < 28)
        elif path.endswith("/ic_launcher_background.png"):
            extrema = image.getextrema()
            check.update(opaque=extrema[3] == (255, 255),
                         solid=all(low == high for low, high in extrema),
                         passed=extrema[3] == (255, 255) and all(low == high for low, high in extrema))
        else:
            difference = sample_difference(image, original)
            check.update(imageDifference=difference, passed=difference < 28)
        checks.append(check)
    group_counts = {name: sum(p.endswith("/" + name + ".png") for p in paths)
                    for name in ("app_icon", "app_icon_round", "ic_launcher_foreground", "ic_launcher_background")}
    preview = next((p for p in paths if p.endswith("/app_icon.png") and "xxxhdpi" in p), None)
    if preview:
        # Copy the exact packaged PNG bytes for inspection; no resizing or composition.
        (args.apk.parent / "packaged-game-icon.png").write_bytes(archive.read(preview))
passed = all(c["passed"] for c in checks) and len(adaptive_xml) == 2 and all(n == 6 for n in group_counts.values())
report = {"passed": passed, "apk": str(args.apk.resolve()), "selectedConcept": 2,
          "source": str(args.source), "foreground": str(args.foreground),
          "adaptiveXml": adaptive_xml, "densityCounts": group_counts,
          "resources": checks, "deviceTestPerformed": False}
output = args.apk.parent / "apk-icon-verification.json"
output.write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps({"passed": passed, "adaptiveXmlCount": len(adaptive_xml),
                  "densityCounts": group_counts, "resourceCount": len(checks),
                  "failed": [c for c in checks if not c["passed"]]}, indent=2))
if not passed:
    raise SystemExit("Packaged icon verification failed; inspect " + str(output))
