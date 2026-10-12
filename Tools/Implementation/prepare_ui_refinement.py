"""Keep a byte baseline before the third requested UI revision."""
import hashlib, json, shutil
from pathlib import Path

root = Path(__file__).resolve().parents[2]
report = root / 'Docs/UiRefinementRevision'
paths = list((root / 'Assets/Weedtopia/Production').rglob('*'))
paths += list((root / 'Assets/Weedtopia/Game/Scenes').glob('*.unity'))
paths += list((root / 'Assets/Weedtopia/Game/Content').glob('*.json'))
manifest = {}
for source in paths:
    if not source.is_file():
        continue
    relative = source.relative_to(root).as_posix()
    target = report / 'Baseline' / relative
    if not target.exists():
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
    manifest[relative] = hashlib.sha256(target.read_bytes()).hexdigest()
report.mkdir(parents=True, exist_ok=True)
(report / 'baseline.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print(f'Preserved {len(manifest)} files')
