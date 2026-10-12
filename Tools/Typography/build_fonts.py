"""Reproducible OFL font build. No OS font installation or network access.

Install requirements into .utmp/FontTooling, then run python -X utf8 this_file.
Keep the source Lato files unchanged: composites, accents and OpenType tables
retain their existing glyph references, including the redesigned base letters.
"""
from __future__ import annotations

import hashlib
import json
import math
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / ".utmp/FontTooling"))
from fontTools.pens.basePen import BasePen
from fontTools.pens.cu2quPen import Cu2QuPen
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont
from fontTools.ttLib.tables.ttProgram import Program

SOURCE = ROOT / "Assets/Weedtopia/Production/Fonts"
OUTPUT = SOURCE / "WeedtopiaGrove"
DOCS = ROOT / "Docs/Typography"
DESIGN = json.loads(Path(__file__).with_name("design.json").read_text(encoding="utf8"))


def lerp(a, b, t):
    return tuple(x + (y - x) * t for x, y in zip(a, b))


def split(points, t):
    levels = [list(points)]
    while len(levels[-1]) > 1:
        levels.append([lerp(a, b, t) for a, b in zip(levels[-1], levels[-1][1:])])
    return tuple(level[0] for level in levels), tuple(level[-1] for level in reversed(levels))


def trim(points, start, end):
    left, _ = split(points, end)
    _, middle = split(left, start / end) if start else ((), left)
    return middle


class Contours(BasePen):
    """Resolve implied quadratic points without flattening curved outlines."""
    def __init__(self, glyph_set):
        super().__init__(glyph_set)
        self.contours = []
        self.current = []
        self.point = self.start = None

    def _moveTo(self, point):
        self.start = self.point = point
        self.current = []

    def _lineTo(self, point):
        if point != self.point:
            self.current.append((self.point, point))
        self.point = point

    def _qCurveToOne(self, control, point):
        self.current.append((self.point, control, point))
        self.point = point

    def _curveToOne(self, control1, control2, point):
        self.current.append((self.point, control1, control2, point))
        self.point = point

    def _closePath(self):
        self._lineTo(self.start)
        if self.current:
            self.contours.append(self.current)
        self.current = []

    def _endPath(self):
        self._closePath()


def soften(glyph, glyph_set, radius):
    contours = Contours(glyph_set)
    glyph.draw(contours, glyph_set)
    pen = TTGlyphPen(None)
    rounded = 0
    for segments in contours.contours:
        cuts = [(0., 0.) for _ in segments]
        for i, segment in enumerate(segments):
            previous = segments[i - 1]
            incoming = tuple(b - a for a, b in zip(previous[-2], previous[-1]))
            outgoing = tuple(b - a for a, b in zip(segment[0], segment[1]))
            before = math.hypot(*incoming)
            after = math.hypot(*outgoing)
            if before < 1 or after < 1:
                continue
            cosine = sum(a * b for a, b in zip(incoming, outgoing)) / (before * after)
            # Preserve smooth joins and near-cusps. Soften genuine terminals/corners.
            if cosine > .985 or cosine < -.985:
                continue
            cuts[i] = (min(.23, radius / (after * (len(segment) - 1))),
                       min(.23, radius / (before * (len(previous) - 1))))
            rounded += 1
        trimmed = [trim(segment, cuts[i][0], 1 - cuts[(i + 1) % len(segments)][1])
                   for i, segment in enumerate(segments)]
        pen.moveTo(trimmed[0][0])
        for i, points in enumerate(trimmed):
            if len(points) == 2:
                pen.lineTo(points[-1])
            elif len(points) == 3:
                pen.qCurveTo(*points[1:])
            else:
                raise ValueError("Unexpected cubic in the TrueType source")
            following = trimmed[(i + 1) % len(segments)][0]
            if points[-1] != following:
                pen.qCurveTo(segments[i][-1], following)
        pen.closePath()
    return pen.glyph(), rounded


def bowl_letter(width, stem, descender=False):
    """Original single-storey a/g with one continuous outline and open counter."""
    tt = TTGlyphPen(None)
    p = Cu2QuPen(tt, max_err=1.0, reverse_direction=False)
    left, right = 65, width - 115
    inner_right = right - stem
    center = (left + inner_right) / 2
    top = 1029
    p.moveTo((center, top))
    p.curveTo((center + 120, top), (inner_right - 40, 990), (inner_right, 916))
    p.lineTo((inner_right + 12, 982))
    p.qCurveTo((inner_right + 18, 1013), (inner_right + 48, 1013))
    p.lineTo((right - 34, 1013))
    p.qCurveTo((right, 1013), (right, 979))
    if descender:
        p.lineTo((right, -30))
        p.curveTo((right, -262), (right - 155, -365), (center, -365))
        p.curveTo((center - 104, -365), (left + 80, -333), (left + 22, -281))
        p.qCurveTo((left + 8, -267), (left + 20, -247))
        p.lineTo((left + 68, -162))
        p.qCurveTo((left + 79, -145), (left + 101, -160))
        p.curveTo((center - 62, -211), (center - 28, -220), (center + 7, -220))
        p.curveTo((inner_right - 50, -220), (inner_right, -156), (inner_right, -34))
        p.lineTo((inner_right, 135))
    else:
        p.lineTo((right, 34))
        p.qCurveTo((right, 0), (right - 34, 0))
        p.lineTo((inner_right + 48, 0))
        p.qCurveTo((inner_right + 18, 0), (inner_right + 12, 34))
        p.lineTo((inner_right, 135))
    p.curveTo((inner_right - 66, 38), (center + 63, -14), (center - 12, -14))
    p.curveTo((left + 119, -14), (left, 185), (left, 507))
    p.curveTo((left, 811), (left + 117, top), (center, top))
    p.closePath()
    # Opposite winding to the exterior; broad aperture also at small UI sizes.
    bottom, inner_top = 139, 1029 - 152
    inner_left = left + stem
    p.moveTo((center, bottom))
    p.curveTo((inner_right - 28, bottom), (inner_right, 268), (inner_right, 507))
    p.curveTo((inner_right, 747), (inner_right - 35, inner_top), (center, inner_top))
    p.curveTo((inner_left + 42, inner_top), (inner_left, 747), (inner_left, 507))
    p.curveTo((inner_left, 268), (inner_left + 39, bottom), (center, bottom))
    p.closePath()
    return tt.glyph()


def seed_dots(glyph, glyph_set):
    contours = Contours(glyph_set)
    glyph.draw(contours, glyph_set)
    pen = TTGlyphPen(None)
    for segments in contours.contours:
        points = [point for segment in segments for point in segment]
        xmin, xmax = min(x for x, y in points), max(x for x, y in points)
        ymin, ymax = min(y for x, y in points), max(y for x, y in points)
        if ymin > 1100 and ymax - ymin < 400:  # Only the dot, never the stem.
            cx, cy = (xmin + xmax) / 2, (ymin + ymax) / 2
            rx, ry = (xmax - xmin) / 2, (ymax - ymin) / 2
            pen.moveTo((cx - rx, cy - .20 * ry))
            pen.qCurveTo((cx - rx, cy + ry), (cx + .28 * rx, cy + ry))
            pen.qCurveTo((cx + rx, cy + ry), (cx + rx, cy + .20 * ry))
            pen.qCurveTo((cx + rx, cy - ry), (cx - .28 * rx, cy - ry))
            pen.qCurveTo((cx - rx, cy - ry), (cx - rx, cy - .20 * ry))
            pen.closePath()
        else:
            pen.moveTo(segments[0][0])
            for segment in segments:
                if len(segment) == 2:
                    pen.lineTo(segment[-1])
                else:
                    pen.qCurveTo(*segment[1:])
            pen.closePath()
    return pen.glyph()


def checkmark(stem):
    p = TTGlyphPen(None)
    p.moveTo((124, 626))
    p.qCurveTo((88, 664), (128, 700))
    p.lineTo((173, 741))
    p.qCurveTo((213, 775), (247, 738))
    p.lineTo((474, 481))
    p.lineTo((1058, 1202))
    p.qCurveTo((1086, 1238), (1123, 1208))
    p.lineTo((1180, 1162))
    p.qCurveTo((1217, 1131), (1187, 1094))
    p.lineTo((548, 302))
    p.qCurveTo((482, 222), (416, 297))
    p.closePath()
    glyph = p.glyph()
    if stem > 200:
        # Weight-specific expansion around the diagonal skeleton.
        for i, (x, y) in enumerate(glyph.coordinates):
            glyph.coordinates[i] = (round(x), round(735 + (y - 735) * 1.035))
    return glyph


def square_marker():
    p = TTGlyphPen(None)
    # Three nested contours: rounded frame, open interior, filled centre.
    for left, bottom, right, top, radius, hole in (
        (100, 30, 1400, 1330, 76, False),
        (268, 198, 1232, 1162, 24, True),
        (516, 446, 984, 914, 38, False),
    ):
        if hole:
            p.moveTo((left + radius, bottom)); p.lineTo((right - radius, bottom))
            p.qCurveTo((right, bottom), (right, bottom + radius)); p.lineTo((right, top - radius))
            p.qCurveTo((right, top), (right - radius, top)); p.lineTo((left + radius, top))
            p.qCurveTo((left, top), (left, top - radius)); p.lineTo((left, bottom + radius))
            p.qCurveTo((left, bottom), (left + radius, bottom))
        else:
            p.moveTo((left, bottom + radius)); p.lineTo((left, top - radius))
            p.qCurveTo((left, top), (left + radius, top)); p.lineTo((right - radius, top))
            p.qCurveTo((right, top), (right, top - radius)); p.lineTo((right, bottom + radius))
            p.qCurveTo((right, bottom), (right - radius, bottom)); p.lineTo((left + radius, bottom))
            p.qCurveTo((left, bottom), (left, bottom + radius))
        p.closePath()
    return p.glyph()


def diamond_marker():
    p = TTGlyphPen(None)
    p.moveTo((710, 1305)); p.qCurveTo((750, 1345), (790, 1305))
    p.lineTo((1325, 770)); p.qCurveTo((1365, 730), (1325, 690))
    p.lineTo((790, 155)); p.qCurveTo((750, 115), (710, 155))
    p.lineTo((175, 690)); p.qCurveTo((135, 730), (175, 770))
    p.closePath()
    return p.glyph()


def add_glyph(font, name, glyph, width, codepoints):
    font.setGlyphOrder(font.getGlyphOrder() + [name])
    font["glyf"][name] = glyph
    glyph.recalcBounds(font["glyf"])
    font["hmtx"][name] = (width, glyph.xMin)
    for table in font["cmap"].tables:
        if table.isUnicode() and table.format in (4, 12):
            for codepoint in codepoints:
                table.cmap[codepoint] = name


def rename(font, style, license_text):
    family = DESIGN["family"]
    names = {
        0: 'Copyright (c) 2010-2014 by tyPoland Lukasz Dziedzic. Copyright (c) 2026 Weedtopia font contributors.',
        1: family, 2: style, 3: f'WeedtopiaGrove:{style}:{DESIGN["version"]}',
        4: f'{family} {style}', 5: f'Version {DESIGN["version"]}',
        6: f'WeedtopiaGrove-{style}', 8: 'Weedtopia font project',
        9: 'Weedtopia font contributors; original Lato by Lukasz Dziedzic',
        10: DESIGN["description"], 13: license_text,
        14: 'https://openfontlicense.org', 16: family, 17: style,
    }
    font["name"].names = []
    for name_id, value in names.items():
        font["name"].setName(value, name_id, 3, 1, 0x409)
        font["name"].setName(value, name_id, 1, 0, 0)


def build(style, options):
    path = SOURCE / options["source"]
    font = TTFont(path, recalcTimestamp=False)
    cmap = font.getBestCmap()
    original_metrics = {tag: getattr(font["hhea"], tag) for tag in ("ascent", "descent", "lineGap")}
    outlines = {}
    corner_count = 0
    # Draw from the original set; avoid changing referenced outlines mid-pass.
    glyph_set = font.getGlyphSet()
    for name in font.getGlyphOrder():
        glyph = font["glyf"][name]
        if glyph.isComposite():
            glyph.program = Program()
        elif glyph.numberOfContours > 0:
            outlines[name], count = soften(glyph, glyph_set, options["cornerRadius"])
            corner_count += count
    for name, glyph in outlines.items():
        font["glyf"][name] = glyph
    for char, descender in (("a", False), ("g", True), ("а", False)):
        name = cmap[ord(char)]
        width = font["hmtx"][name][0]
        font["glyf"][name] = bowl_letter(width, options["stem"], descender)
    glyph_set = font.getGlyphSet()
    for char in "ij":
        name = cmap[ord(char)]
        font["glyf"][name] = seed_dots(font["glyf"][name], glyph_set)
    # The old font lacks the tick used in completed objectives and the rouble.
    add_glyph(font, "weedtopia.checkmark", checkmark(options["stem"]), 1300, (0x2713, 0x2714))
    add_glyph(font, "weedtopia.squareMarker", square_marker(), 1500, (0x25A3,))
    add_glyph(font, "weedtopia.diamondMarker", diamond_marker(), 1500, (0x25C6,))
    thickness = 104 if style == "Regular" else 142
    # Decompose the currency: a TrueType glyph cannot mix contours/components.
    from fontTools.pens.recordingPen import DecomposingRecordingPen
    recording = DecomposingRecordingPen(font.getGlyphSet())
    font.getGlyphSet()[cmap[ord("Р")]].draw(recording)
    ruble = TTGlyphPen(None)
    recording.replay(ruble)
    for bottom in (490, 274):
        ruble.moveTo((82, bottom)); ruble.lineTo((82, bottom + thickness))
        ruble.lineTo((902, bottom + thickness)); ruble.lineTo((902, bottom)); ruble.closePath()
    add_glyph(font, "weedtopia.ruble", ruble.glyph(), font["hmtx"][cmap[ord("Р")]][0], (0x20BD,))
    for name in font.getGlyphOrder():
        glyph = font["glyf"][name]
        glyph.program = Program()
        glyph.recalcBounds(font["glyf"])
        width, lsb = font["hmtx"][name]
        font["hmtx"][name] = (width, getattr(glyph, "xMin", lsb))
    for tag in ("fpgm", "prep", "cvt ", "DSIG"):
        if tag in font:
            del font[tag]
    font["gasp"].gaspRange = {0xFFFF: 10}  # Smooth outlines; no stale grid fitting.
    font["OS/2"].fsType = 0
    font["OS/2"].usWeightClass = 400 if style == "Regular" else 700
    font["head"].fontRevision = float(DESIGN["version"])
    stamp = int(datetime.fromisoformat(DESIGN["buildDate"]).replace(tzinfo=timezone.utc).timestamp()) + 2082844800
    font["head"].created = font["head"].modified = stamp
    license_text = (SOURCE / "OFL.txt").read_text(encoding="utf8")
    license_text = license_text.replace('\n\nThis Font', '\nCopyright (c) 2026 Weedtopia font contributors.\n\nThis Font', 1)
    rename(font, style, license_text)
    filename = OUTPUT / f'WeedtopiaGrove-{style}.ttf'
    font.save(filename)
    (OUTPUT / "OFL.txt").write_text(license_text, encoding="utf8")
    return {
        "style": style, "file": filename.relative_to(ROOT).as_posix(),
        "sourceSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "sha256": hashlib.sha256(filename.read_bytes()).hexdigest(),
        "glyphs": len(font.getGlyphOrder()), "unicodeCharacters": len(font.getBestCmap()),
        "roundedCorners": corner_count, "lineMetrics": original_metrics,
        "originalAdvanceWidthsRetained": True,
        "customGlyphs": ["a", "g", "а", "i", "j", "✓", "✔", "▣", "◆", "₽"],
        "legacyHintingRemoved": True, "openTypeShapingAndKerningRetained": True,
    }


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    DOCS.mkdir(parents=True, exist_ok=True)
    receipt = {"family": DESIGN["family"], "version": DESIGN["version"], "styles": [build(style, options) for style, options in DESIGN["styles"].items()]}
    (DOCS / "font_build.json").write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    print(json.dumps(receipt, ensure_ascii=False))
