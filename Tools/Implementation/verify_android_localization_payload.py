"""Prove current translations/fonts are embedded verbatim in a test APK.

Unity may split serialized data into levelN/sharedassetsN.assets.splitN files.
Read each logical asset in numeric split order and scan a bounded window; never
extract or load the entire multi-gigabyte APK into memory.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import zipfile

ROOT = Path(__file__).resolve().parents[2]


def verify(apk):
    sources = sorted((ROOT / "Assets/Weedtopia/Game/Localization").glob("*.json"))
    sources += sorted((ROOT / "Assets/Weedtopia/Production/Fonts/WeedtopiaGrove").glob("*.ttf"))
    assert len(sources) == 7, "Expected five translations and two Weedtopia fonts"
    pending = {p.relative_to(ROOT).as_posix(): p.read_bytes() for p in sources}
    hashes = {name: hashlib.sha256(data).hexdigest() for name, data in pending.items()}
    max_overlap = max(len(data) for data in pending.values()) - 1
    found, groups = {}, {}
    with zipfile.ZipFile(apk) as archive:
        for entry in archive.infolist():
            if not entry.filename.startswith("assets/bin/Data/"):
                continue
            match = re.match(r"(.+)\.split(\d+)$", entry.filename)
            name, index = (match.group(1), int(match.group(2))) if match else (entry.filename, 0)
            if not re.search(r"(?:\.assets|\.resource|\.resS|/level\d+)$", name):
                continue
            groups.setdefault(name, []).append((index, entry))
        for logical_name, entries in sorted(groups.items()):
            tail = b""
            offset = 0
            for _, entry in sorted(entries, key=lambda pair: pair[0]):
                with archive.open(entry) as stream:
                    while chunk := stream.read(4 * 1024 * 1024):
                        data = tail + chunk
                        for name, needle in list(pending.items()):
                            position = data.find(needle[:160])
                            while position >= 0:
                                if data[position:position + len(needle)] == needle:
                                    found[name] = {"logicalAsset": logical_name,
                                                   "offset": offset - len(tail) + position,
                                                   "sourceBytes": len(needle), "sha256": hashes[name],
                                                   "embeddedBytesIdentical": True}
                                    del pending[name]
                                    break
                                position = data.find(needle[:160], position + 1)
                        offset += len(chunk)
                        tail = data[-max_overlap:]
                        if not pending:
                            break
                if not pending:
                    break
            if not pending:
                break
    result = {"passed": not pending, "apk": str(apk), "method": "Exact source byte comparison across logical Unity serialized assets",
              "currentPayloadsFound": found, "missingPayloads": list(pending), "deviceRenderingVerified": False}
    (apk.parent / "localization-payload-verification.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return result["passed"]


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("apk", type=Path)
    args = parser.parse_args()
    raise SystemExit(0 if verify(args.apk.resolve(strict=True)) else 1)
