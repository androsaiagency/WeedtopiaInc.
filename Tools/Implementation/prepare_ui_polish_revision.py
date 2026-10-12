"""Preserve the current authored UI before the requested second visual revision."""
import hashlib, json, shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Docs/UiPolishRevision'
REPORT.mkdir(parents=True, exist_ok=True)
paths = ['Assets/Weedtopia/Game/Scenes/BurningHills.unity',
         'Assets/Weedtopia/Game/Scenes/BigRockCity.unity',
         'Assets/Weedtopia/Game/Content/BurningHills.json',
         'Assets/Weedtopia/Production/PresentationArt.asset']
paths += [str(p.relative_to(ROOT)).replace('\\', '/') for p in
          (ROOT / 'Assets/Weedtopia/Production/Prefabs').rglob('*') if p.is_file()]
for directory in ('Rewards', 'LuckyWheel', 'VipStories', 'BigRockCity'):
    paths += [str(p.relative_to(ROOT)).replace('\\', '/') for p in
              (ROOT / 'Assets/Weedtopia/Production' / directory).rglob('*.prefab*') if p.is_file()]
manifest = {}
for relative in paths:
    source, target = ROOT / relative, REPORT / 'Baseline' / relative
    if not target.exists():
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
    manifest[relative] = hashlib.sha256(target.read_bytes()).hexdigest()
(REPORT / 'baseline.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print(f'Preserved {len(manifest)} files for the UI polish revision')
