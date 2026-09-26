"""Read-only inventory; the only writes are new implementation evidence files."""
import csv, hashlib, json, pathlib, datetime

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/Implementation/Validation'
OUT.mkdir(parents=True, exist_ok=True)

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

manifest = json.loads((ROOT / 'Docs/ArtProduction/BUILDING_ASSET_MANIFEST.json').read_text(encoding='utf-8-sig'))
matrix = list(csv.DictReader((ROOT / 'Docs/ArtProduction/BUILDING_TIER_MATRIX.csv').open(encoding='utf-8-sig', newline='')))
index = (ROOT / 'Docs/ArtProduction/BUILDING_ASSET_INDEX.md').read_text(encoding='utf-8-sig')
errors = []
for slot, row in zip(manifest['slots'], matrix):
    for key, value in row.items():
        if key in slot and str(slot[key]) != value and not (isinstance(slot[key], (float, int)) and float(value) == slot[key]):
            errors.append(f"CSV mismatch: {slot['building_id']}/{slot['tier_id']}:{key}")
    for key in ('reference_image', 'unity_prefab_target', 'asset_spec_file'):
        if not (ROOT / slot[key]).exists(): errors.append('Missing: ' + slot[key])
    if slot['tier_id'] not in index: errors.append('Index missing tier')
files = {}
for folder in ('Assets', 'Packages', 'ProjectSettings', 'Docs/ArtProduction'):
    for path in (ROOT / folder).rglob('*'):
        if path.is_file(): files[path.relative_to(ROOT).as_posix()] = digest(path)
report = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
              unity='6000.3.9f1 (7a9955a4f2fa)', slots=len(manifest['slots']), csvRows=len(matrix),
              csFiles=len(list((ROOT / 'Assets').rglob('*.cs'))),
              lock=(ROOT / 'Temp/UnityLockfile').exists(),
              gitRoots=[str(p) for p in (ROOT, *ROOT.parents) if (p / '.git').exists()],
              errors=errors, hashes=files)
target = OUT / 'm0_baseline.json'
if target.exists(): raise RuntimeError('Baseline already exists; refusing to overwrite')
target.write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k != 'hashes'}))
print(f'Protected file hashes: {len(files)}')
