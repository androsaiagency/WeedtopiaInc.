import hashlib, json, sys
from pathlib import Path

TASK = Path(__file__).resolve().parent
PROJECT = next(p for p in TASK.parents if (p / 'ProjectSettings').exists())
OUT = TASK / 'CityIntegration'
BASELINE = OUT / 'preservation_before.json'
ALLOWED = {
    'Assets/Weedtopia/Art/Buildings/Prefabs/town_hall/WT_town_hall_T_FIXED.prefab',
    'Assets/Weedtopia/Scripts/Runtime/Unity/BuildingView.cs',
    'Assets/Weedtopia/Scripts/Runtime/Unity/IsometricCameraRig.cs',
    'Assets/Weedtopia/Scripts/Editor/GameSetup/InteractionVisualEvidence.cs',
    'Assets/Weedtopia/Scripts/Editor/GameSetup/ProductionVisualPreflight.cs',
    'Assets/Weedtopia/Scripts/Editor/GameSetup/ProductionBuildingIntegration.cs',
    'Assets/Weedtopia/Scripts/Tests/PlayMode/BuildingInteractionTests.cs',
}
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
if sys.argv[-1] == 'before':
    if BASELINE.exists(): raise SystemExit('Existing baseline preserved')
    files = [p for folder in ['Assets','ProjectSettings','Packages'] for p in (PROJECT/folder).rglob('*') if p.is_file()]
    BASELINE.write_text(json.dumps({p.relative_to(PROJECT).as_posix(): digest(p) for p in files}, indent=2), encoding='utf-8')
    print('Baseline files:', len(files))
else:
    data = json.loads(BASELINE.read_text())
    changed = [k for k,v in data.items() if not (PROJECT/k).is_file() or digest(PROJECT/k) != v]
    unexpected = [k for k in changed if k not in ALLOWED]
    result = dict(files_checked=len(data), intentional_changes=changed, unexpected_changes=unexpected, passed=not unexpected)
    (OUT/'preservation_after.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))
