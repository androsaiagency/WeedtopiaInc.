"""Audit new Unity metadata/references and generated illustration provenance without altering assets."""
from pathlib import Path
import datetime, hashlib, json, re
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/SevenHills'
FOLDER = ROOT / 'Assets/Weedtopia/Production/SevenHills'
guid_pattern = re.compile(r'^guid:\s*([a-f0-9]{32})\s*$', re.M)
references_pattern = re.compile(r'guid:\s*([a-f0-9]{32})')
guid_paths = {}
for base in ['Assets', 'Packages', 'Library/PackageCache']:
    for path in (ROOT / base).rglob('*.meta'):
        match = guid_pattern.search(path.read_text(encoding='utf-8-sig'))
        if match:
            guid_paths.setdefault(match.group(1), []).append(path.relative_to(ROOT).as_posix())

owned = [p for p in FOLDER.rglob('*') if p.is_file()]
owned += [p for p in (ROOT / 'Assets/Weedtopia/Scripts').rglob('*SevenHills*') if p.is_file()]
owned += [ROOT / 'Assets/Weedtopia/Game/Scenes/SevenHills.unity',
          ROOT / 'Assets/Weedtopia/Scripts/Runtime/Unity/CityTravelRow.cs']
missing_metadata = [p.relative_to(ROOT).as_posix() for p in owned if p.suffix != '.meta' and not Path(str(p)+'.meta').exists()]
duplicates = {g: paths for g, paths in guid_paths.items() if len(paths) > 1 and any('SevenHills' in p or 'CityTravelRow' in p for p in paths)}
unresolved = []
seen_references = set()
for path in owned:
    if path.suffix not in {'.unity', '.prefab', '.asset', '.mat', '.meta'}:
        continue
    for guid in references_pattern.findall(path.read_text(encoding='utf-8-sig')):
        token = (path.as_posix(), guid)
        if guid not in guid_paths and not guid.startswith('0000000000000000') and token not in seen_references:
            seen_references.add(token)
            unresolved.append({'file': path.relative_to(ROOT).as_posix(), 'guid': guid})

art = []
for path in sorted((FOLDER / 'Art').glob('*.png')):
    with Image.open(path) as image:
        alpha = image.getchannel('A') if 'A' in image.getbands() else None
        expected = (1024, 1536) if path.stem.startswith('sh_') else (1536, 1024)
        art.append({'path': path.relative_to(ROOT).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                    'size': list(image.size), 'alphaExtrema': alpha.getextrema() if alpha else None,
                    'valid': image.size == expected and alpha is not None and alpha.getextrema()[0] == 0})
report = {'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'newFilesAudited': len(owned),
          'missingMetadata': missing_metadata, 'duplicateOwnedGuids': duplicates,
          'unresolvedReferences': unresolved, 'art': art, 'imageGenerationMode': 'built-in image_gen',
          'promptManifest': 'Docs/SevenHills/art_prompts.json'}
report['status'] = 'passed' if not missing_metadata and not duplicates and not unresolved and len(art) == 9 and all(a['valid'] for a in art) else 'failed'
(DOC / 'asset_audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps({'status': report['status'], 'newFilesAudited': len(owned), 'missingMetadata': missing_metadata,
                  'duplicateOwnedGuids': duplicates, 'unresolvedReferences': unresolved, 'validatedArt': len(art)}, ensure_ascii=False, indent=2))
if report['status'] != 'passed':
    raise SystemExit(1)
