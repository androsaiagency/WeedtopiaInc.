"""Compare original assets and serialized scene references with the pre-interaction baseline."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, re, zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/InteractionRevision'
before = json.loads((OUT / 'baseline.json').read_text(encoding='utf-8'))['files']
allowed = {
    'Assets/Weedtopia/Game/Scenes/BurningHills.unity',
    'Assets/Weedtopia/Scripts/Runtime/Unity/GameUi.cs',
    'Assets/Weedtopia/Scripts/Runtime/Unity/IsometricCameraRig.cs',
    'Assets/Weedtopia/Scripts/Runtime/Unity/BuildingView.cs',
    'Assets/Weedtopia/Scripts/Tests/PlayMode/GameFlowTests.cs',
}
missing = [name for name in before if not (ROOT / name).is_file()]
changed = [name for name, digest in before.items() if (ROOT / name).is_file() and hashlib.sha256((ROOT / name).read_bytes()).hexdigest() != digest]
unexpected = [name for name in changed if name not in allowed]

def documents(text):
    return {m.group(2): (int(m.group(1)), m.group(3).strip()) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}

with zipfile.ZipFile(OUT / 'editable_originals.zip') as archive:
    old = documents(archive.read('Assets/Weedtopia/Game/Scenes/BurningHills.unity').decode('utf-8-sig'))
new = documents((ROOT / 'Assets/Weedtopia/Game/Scenes/BurningHills.unity').read_text(encoding='utf-8-sig'))
allowed_scripts = set()
for name in ['GameUi', 'ProductionPresentation']:
    meta = (ROOT / f'Assets/Weedtopia/Scripts/Runtime/Unity/{name}.cs.meta').read_text(encoding='utf-8')
    allowed_scripts.add(re.search(r'^guid: (\w+)', meta, re.M).group(1))
protected_changes, ui_changes = [], []
for key, (kind, text) in old.items():
    if new.get(key) == (kind, text) or kind not in (114, 1001):
        continue
    if kind == 114 and any(guid in text for guid in allowed_scripts):
        ui_changes.append(key)
    else:
        protected_changes.append(key)
missing_objects = [key for key in old if key not in new]
result = dict(
    utc=datetime.now(timezone.utc).isoformat(), baselineFiles=len(before),
    changedExistingFiles=changed, unexpectedChanges=unexpected, missingFiles=missing,
    originalSceneDocuments=len(old), missingOriginalSceneObjects=missing_objects,
    protectedSceneChanges=protected_changes, intentionalUiBindingComponents=ui_changes,
    originalArtCatalogWrappersAndDomainUnchanged=not missing and not unexpected,
    passed=not (missing or unexpected or protected_changes or missing_objects),
)
(OUT / 'preservation.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
print(json.dumps(result, indent=2))
assert result['passed']
