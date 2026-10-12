"""Full glyph coverage, font identity, layout metrics and rebuild verification."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys
import unicodedata

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / ".utmp/FontTooling"))
from fontTools.ttLib import TTFont

FONT_FOLDER = ROOT / "Assets/Weedtopia/Production/Fonts/WeedtopiaGrove"
DOCS = ROOT / "Docs/Typography"
TEXT_FOLDER = ROOT / "Assets/Weedtopia/Game/Localization"
LANGUAGE_ALPHABETS = {
    "de": "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÄäÖöÜüßẞ",
    "en": "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
    "es": "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁáÉéÍíÓóÚúÜüÑñ¿¡",
    "fr": "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÀàÂâÆæÇçÉéÈèÊêËëÎîÏïÔôŒœÙùÛûÜüŸÿ",
    "ru": "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя",
}


def printable(c):
    return not unicodedata.category(c).startswith("C") and not c.isspace()


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate():
    catalogs = {p.stem: json.loads(p.read_text(encoding="utf8"))["entries"] for p in TEXT_FOLDER.glob("*.json")}
    needed_by_language = {}
    for language, entries in catalogs.items():
        text = "".join(e["value"] for e in entries) + LANGUAGE_ALPHABETS.get(language, "")
        text += unicodedata.normalize("NFD", text)
        needed_by_language[language] = {ord(c) for c in text if printable(c)}
    needed = set.union(*needed_by_language.values(), {ord(c) for c in "0123456789✓✔₽€£$→←×÷…•–—«»‘’“”"})
    results = []
    for style in ("Regular", "Bold"):
        path = FONT_FOLDER / f"WeedtopiaGrove-{style}.ttf"
        font = TTFont(path, checkChecksums=2)
        source = TTFont(FONT_FOLDER.parent / f"Lato-{style}.ttf")
        cmap = font.getBestCmap()
        missing = sorted(needed - cmap.keys())
        assert not missing, [(hex(c), chr(c)) for c in missing]
        assert all(font["glyf"][cmap[c]].numberOfContours != 0 for c in needed), "Empty printable glyph"
        for name_id in (1, 4, 6, 16):
            assert "Lato" not in font["name"].getDebugName(name_id), "Reserved original family name"
        assert font["name"].getDebugName(1) == "Weedtopia Grove"
        assert font["name"].getDebugName(2) == style
        assert font["OS/2"].usWeightClass == (400 if style == "Regular" else 700)
        assert font["OS/2"].fsType == 0
        assert "SIL OPEN FONT LICENSE Version 1.1" in font["name"].getDebugName(13)
        assert "Copyright (c) 2010-2014" in font["name"].getDebugName(0)
        assert font["hhea"].ascent == source["hhea"].ascent
        assert font["hhea"].descent == source["hhea"].descent
        assert font["hhea"].lineGap == source["hhea"].lineGap
        for name in source.getGlyphOrder():
            assert font["hmtx"][name][0] == source["hmtx"][name][0], "Changed advance: " + name
        for table in ("GPOS", "GSUB"):
            assert font.getTableData(table) == source.getTableData(table), "Shaping table changed"
        assert font.getTableData("glyf") != source.getTableData("glyf"), "Unmodified source font"
        for char in "agаij":
            name = cmap[ord(char)]
            assert font["glyf"][name].compile(font["glyf"]) != source["glyf"][name].compile(source["glyf"]), "Missing custom outline: " + char
        assert not any(tag in font for tag in ("prep", "fpgm", "cvt "))
        for name in font.getGlyphOrder():
            glyph = font["glyf"][name]
            assert not hasattr(glyph, "program") or not glyph.program.getBytecode(), "Stale glyph hinting"
        blocks = {"LatinExtendedA": (0x100, 0x17F), "RussianAlphabet": (0x410, 0x44F), "CyrillicBasicSupplement": (0x400, 0x45F)}
        results.append({"style": style, "sha256": sha(path), "unicodeCharacters": len(cmap), "glyphs": len(font.getGlyphOrder()),
                        "testedCharacters": len(needed), "missingCharacters": [],
                        "languages": {language: {"catalogEntries": len(catalogs[language]), "testedCharacters": len(chars), "missing": len(chars - cmap.keys())} for language, chars in needed_by_language.items()},
                        "blocks": {label: {"covered": sum(c in cmap for c in range(start, end + 1)), "total": end - start + 1} for label, (start, end) in blocks.items()},
                        "sourceAdvanceAndLineMetricsPreserved": True, "originalKerningAndShapingPreserved": True,
                        "customOutlinesVerified": True, "offlineFontData": True})
    before = {p: sha(p) for p in FONT_FOLDER.glob("*.ttf")}
    subprocess.run([sys.executable, "-X", "utf8", str(Path(__file__).with_name("build_fonts.py"))], cwd=ROOT, check=True, capture_output=True)
    assert all(sha(p) == value for p, value in before.items()), "Non-reproducible font build"
    result = {"status": "passed", "family": "Weedtopia Grove", "rebuildByteIdentical": True,
              "allFiveLanguagesCovered": True, "worldwideAllWritingSystemsClaimed": False, "fonts": results}
    (DOCS / "font_validation.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    validate()
