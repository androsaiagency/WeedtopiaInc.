"""Verify additive town life without changing any original scene binding or production artwork."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, re, zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/LivingHills'
SCENE = 'Assets/Weedtopia/Game/Scenes/BurningHills.unity'
baseline = json.loads((OUT / 'baseline.json').read_text(encoding='utf-8-sig'))['files']
allowed = {SCENE, 'Assets/Weedtopia/Scripts/Editor/GameSetup/InteractionVisualEvidence.cs',
    'Assets/Weedtopia/Scripts/Editor/GameSetup/NaturalEnvironmentAuthoring.cs',
    'Assets/Weedtopia/Scripts/Tests/PlayMode/GameFlowTests.cs'}
missing = [f for f in baseline if not (ROOT / f).is_file()]
changed = [f for f, digest in baseline.items() if (ROOT / f).is_file() and hashlib.sha256((ROOT / f).read_bytes()).hexdigest() != digest]
unexpected = [f for f in changed if f not in allowed]

def documents(text):
    return {m[2]: (int(m[1]), m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}

with zipfile.ZipFile(OUT / 'editable_originals.zip') as archive:
    old = documents(archive.read(SCENE).decode('utf-8-sig'))
new = documents((ROOT / SCENE).read_text(encoding='utf-8-sig'))
world_go = next(k for k, v in old.items() if v[0] == 1 and '  m_Name: BurningHills_Districts\n' in v[1])
parent = next(k for k, v in old.items() if v[0] == 4 and f'  m_GameObject: {{fileID: {world_go}}}' in v[1])
array = r'^  m_Children:\n((?:  - \{fileID: \d+\}\n)*)'
old_children, new_children = (re.search(array, d[parent][1], re.M)[1] for d in (old, new))
parent_ok = new_children.startswith(old_children) and len(new_children.splitlines()) == len(old_children.splitlines()) + 1
parent_ok &= re.sub(array, '', old[parent][1], flags=re.M) == re.sub(array, '', new[parent][1], flags=re.M)
protected_changes = [k for k, v in old.items() if new.get(k) != v and k != parent]
wrappers = [f for f in baseline if f.startswith('Assets/Weedtopia/Art/Buildings/Prefabs/') and f.endswith('.prefab')]
result = dict(utc=datetime.now(timezone.utc).isoformat(), baselineFiles=len(baseline),
    changedExistingFiles=changed, unexpectedChanges=unexpected, missingFiles=missing,
    originalSceneDocuments=len(old), addedSceneDocuments=len(new) - len(old),
    missingOriginalSceneObjects=[k for k in old if k not in new], protectedSceneChanges=protected_changes,
    onlyOneNewChildOnExistingWorld=parent_ok,
    unchangedCenteredBuildingPrefabs=len(wrappers) if not set(wrappers).intersection(changed + missing) else 0,
    domainSavesContentAndLogisticsUnchanged=not any('/Runtime/Domain/' in f or '/Runtime/Application/' in f or '/Runtime/Persistence/' in f or f.endswith('BurningHills.json') or f.endswith('BurningHillsLayout.json') for f in changed),
    passed=not (missing or unexpected or protected_changes) and parent_ok and len(wrappers) == 251)
(OUT / 'preservation.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
print(json.dumps(result, indent=2));assert result['passed']
