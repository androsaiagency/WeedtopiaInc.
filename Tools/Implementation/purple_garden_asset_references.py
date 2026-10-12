"""Read-only validation of Purple Garden assets against imported Unity GUID registries."""
from __future__ import annotations
import datetime
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
GUID = re.compile(r'\bguid:\s*([0-9a-f]{32})\b')


def main():
    registry = {}
    for directory in ('Assets', 'Packages', 'Library/PackageCache'):
        for meta in (ROOT / directory).rglob('*.meta'):
            match = re.search(r'^guid:\s*([0-9a-f]{32})\s*$', meta.read_text(encoding='utf-8-sig'), re.M)
            if match:
                registry.setdefault(match.group(1), []).append(meta)
    folder = ROOT / 'Assets/Weedtopia/Production/PurpleGarden'
    assets = [path for path in folder.rglob('*') if path.is_file() and path.suffix != '.meta']
    missing = [str(path.relative_to(ROOT)) for path in assets if not pathlib.Path(str(path) + '.meta').is_file()]
    duplicates = {guid: [str(path.relative_to(ROOT)) for path in paths] for guid, paths in registry.items()
                  if len(paths) > 1 and any(path.is_relative_to(folder) for path in paths)}
    references = set()
    scene = ROOT / 'Assets/Weedtopia/Game/Scenes/PurpleGarden.unity'
    for path in assets + [scene]:
        if path.suffix.lower() not in ('.png', '.jpg', '.jpeg', '.wav', '.mp3', '.ogg'):
            references.update(GUID.findall(path.read_text(encoding='utf-8-sig')))
    unresolved = sorted(guid for guid in references if guid not in registry and not guid.startswith('0000000000000000'))
    result = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        status='failed' if missing or duplicates or unresolved else 'passed', newAssets=len(assets),
        checkedUniqueReferences=len(references), unresolvedReferences=unresolved,
        duplicateGuids=duplicates, missingMetas=missing,
        scope='PurpleGarden production files and scene; references resolved through Assets, Packages and the imported PackageCache')
    (ROOT / 'Docs/PurpleGarden/asset_reference_validation.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, indent=2))
    return 0 if result['status'] == 'passed' else 1


if __name__ == '__main__':
    raise SystemExit(main())
