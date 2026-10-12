"""Inspect fourth-city Unity references and image pixels; modify only the audit report."""
from __future__ import annotations

import datetime
import hashlib
import json
import pathlib
import re

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
FOLDER = ROOT / "Assets/Weedtopia/Production/LittleMontana"
DOC = ROOT / "Docs/LittleMontana"
GUID = re.compile(r"^guid:\s*([a-f0-9]{32})\s*$", re.M)
REFERENCE = re.compile(r"guid:\s*([a-f0-9]{32})")


def main() -> int:
    guid_paths: dict[str, list[str]] = {}
    for base in ("Assets", "Packages", "Library/PackageCache"):
        for path in (ROOT / base).rglob("*.meta"):
            match = GUID.search(path.read_text(encoding="utf-8-sig"))
            if match:
                guid_paths.setdefault(match.group(1), []).append(path.relative_to(ROOT).as_posix())
    owned = [path for path in FOLDER.rglob("*") if path.is_file()]
    owned += [path for path in (ROOT / "Assets/Weedtopia/Scripts").rglob("*LittleMontana*") if path.is_file()]
    owned += [ROOT / "Assets/Weedtopia/Game/Scenes/LittleMontana.unity",
              ROOT / "Assets/Weedtopia/Scripts/Runtime/Unity/CityPlaceProgressVisual.cs"]
    missing_files = [path.relative_to(ROOT).as_posix() for path in owned if not path.is_file()]
    missing_meta = [path.relative_to(ROOT).as_posix() for path in owned
                    if path.is_file() and path.suffix != ".meta" and not pathlib.Path(str(path) + ".meta").is_file()]
    owned_names = {path.relative_to(ROOT).as_posix() for path in owned}
    duplicates = {guid: names for guid, names in guid_paths.items()
                  if len(names) > 1 and any(name[:-5] in owned_names or name in owned_names for name in names)}
    unresolved = []
    for path in owned:
        if not path.is_file() or path.suffix not in {".unity", ".prefab", ".asset", ".mat", ".meta"}:
            continue
        for guid in set(REFERENCE.findall(path.read_text(encoding="utf-8-sig"))):
            if guid not in guid_paths and not guid.startswith("0000000000000000"):
                unresolved.append({"file": path.relative_to(ROOT).as_posix(), "guid": guid})
    art = []
    for path in sorted((FOLDER / "Art").glob("*.png")):
        with Image.open(path) as pixels:
            alpha = pixels.getchannel("A") if "A" in pixels.getbands() else None
            art.append({"path": path.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                        "size": list(pixels.size), "alphaExtrema": list(alpha.getextrema()) if alpha else None,
                        "validTransparency": alpha is not None and alpha.getextrema()[0] == 0 and alpha.getextrema()[1] > 0})
    residents = [entry for entry in art if pathlib.Path(entry["path"]).stem.startswith("lm_")]
    expected_sizes = {"town_sign": [1254, 1254], "timber_depot": [1536, 1024], "mountain_ridge": [1536, 1024]}
    expected_sizes.update({name: [1024, 1536] for name in ("lm_mara", "lm_jon", "lm_ellie", "lm_noah", "lm_ruth", "lm_tessa", "lm_ben")})
    for entry in art:
        entry["validDimensions"] = entry["size"] == expected_sizes.get(pathlib.Path(entry["path"]).stem)
    missing_resident_art = sorted({"lm_mara", "lm_jon", "lm_ellie", "lm_noah", "lm_ruth", "lm_tessa", "lm_ben"}
                                  - {pathlib.Path(entry["path"]).stem for entry in residents})
    report = {"utc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "newFilesAudited": len(owned),
              "missingFiles": missing_files, "missingMetadata": missing_meta, "duplicateOwnedGuids": duplicates,
              "unresolvedReferences": unresolved, "art": art, "missingResidentArt": missing_resident_art,
              "residentIllustrations": len(residents), "humanVisualApproval": False,
              "coverage": "Serialized references, metadata identity, current PNG hashes and alpha channels; visual quality and user approval remain separate."}
    report["status"] = "passed" if not (missing_files or missing_meta or duplicates or unresolved or missing_resident_art) and len(residents) == 7 and len(art) == 10 and all(entry["validTransparency"] and entry["validDimensions"] for entry in art) else "failed"
    DOC.mkdir(parents=True, exist_ok=True)
    (DOC / "asset_audit.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: report[key] for key in ("status", "newFilesAudited", "missingFiles", "missingMetadata", "duplicateOwnedGuids", "unresolvedReferences", "residentIllustrations", "missingResidentArt")}, indent=2))
    return 0 if report["status"] == "passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
