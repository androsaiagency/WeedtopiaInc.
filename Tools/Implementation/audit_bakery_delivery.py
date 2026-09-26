"""Verify the scoped bakery change against the original files and serialized references."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, re, zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/BakeryDelivery'
SCENE = 'Assets/Weedtopia/Game/Scenes/BurningHills.unity'
CONTENT = 'Assets/Weedtopia/Game/Content/BurningHills.json'
LAYOUT = 'Assets/Weedtopia/Production/Layout/BurningHillsLayout.json'
SCRIPTS = 'Assets/Weedtopia/Scripts/'
allowed = {SCENE, CONTENT, LAYOUT} | {SCRIPTS + p for p in (
    'Editor/GameSetup/InteractionVisualEvidence.cs', 'Editor/GameSetup/NaturalEnvironmentAuthoring.cs',
    'Runtime/Domain/Definitions.cs', 'Runtime/Domain/GameState.cs', 'Runtime/Domain/Simulation.cs',
    'Runtime/Persistence/LocalSaveStore.cs', 'Runtime/Unity/BuildingInspectionMetrics.cs',
    'Runtime/Unity/BuildingInteractionUi.cs', 'Runtime/Unity/CharacterActor.cs', 'Runtime/Unity/DeliveryView.cs',
    'Tests/EditMode/PersistenceTests.cs', 'Tests/EditorAssets/DistrictLayoutTests.cs', 'Tests/PlayMode/GameFlowTests.cs')}
baseline = json.loads((OUT / 'baseline.json').read_text(encoding='utf-8-sig'))['files']
missing = [f for f in baseline if not (ROOT / f).is_file()]
changed = [f for f, digest in baseline.items() if (ROOT / f).is_file() and hashlib.sha256((ROOT / f).read_bytes()).hexdigest() != digest]
unexpected = [f for f in changed if f not in allowed]

def documents(text):
    return {m[2]: (int(m[1]), m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}

def omit(text, *fields):
    for field in fields:
        text = re.sub(r'^  ' + re.escape(field) + r':[^\n]*\n(?:  (?:  |-|\{|\})[^\n]*\n)*', '', text, flags=re.M)
    return text

with zipfile.ZipFile(OUT / 'editable_originals.zip') as archive:
    old = documents(archive.read(SCENE).decode('utf-8-sig'))
    old_content, old_layout = (json.loads(archive.read(p)) for p in (CONTENT, LAYOUT))
new = documents((ROOT / SCENE).read_text(encoding='utf-8-sig'))
route_docs = {k: v for k, v in old.items() if v[0] == 114 and re.search(r'^  routeId: butter_bakery\d+$', v[1], re.M)}
road_objects = {re.search(r'  m_GameObject: \{fileID: (\d+)\}', v[1])[1] for v in route_docs.values()}
root_key = next(k for k, v in old.items() if v[0] == 114 and '  vehicles:\n' in v[1])
pool_go = next(k for k, v in old.items() if v[0] == 1 and '  m_Name: DeliveryVisualPool\n' in v[1])
pool_key = next(k for k, v in old.items() if v[0] == 4 and f'  m_GameObject: {{fileID: {pool_go}}}' in v[1])
protected_changes, intentional_changes = [], []
for k, (kind, text) in old.items():
    if new.get(k) == (kind, text):
        continue
    if k not in new:
        protected_changes.append(k); continue
    fields = ()
    if k in route_docs: fields = ('points', 'conveyor', 'cargoMarkers')
    elif k == root_key: fields = ('vehicles',)
    elif k == pool_key: fields = ('m_Children',)
    elif kind in (23, 33) and any(f'  m_GameObject: {{fileID: {g}}}' in text for g in road_objects):
        fields = ('m_Materials', 'm_SortingOrder') if kind == 23 else ('m_Mesh',)
    if fields and omit(text, *fields) == omit(new[k][1], *fields): intentional_changes.append(k)
    else: protected_changes.append(k)

def references(text, field):
    m = re.search(r'^  ' + field + r':\n((?:  - \{fileID: \d+\}\n)*)', text, re.M)
    return re.findall(r'fileID: (\d+)', m[1]) if m else []

old_vehicles, new_vehicles = (references(d[root_key][1], 'vehicles') for d in (old, new))
old_children, new_children = (references(d[pool_key][1], 'm_Children') for d in (old, new))
bindings_preserved = len(old_vehicles) == 48 and len(new_vehicles) == 60 and new_vehicles[:48] == old_vehicles and new_children[:len(old_children)] == old_children
content, layout = (json.loads((ROOT / p).read_text(encoding='utf-8-sig')) for p in (CONTENT, LAYOUT))
content_ok = {k: v for k, v in old_content.items() if k not in ('routes', 'texts')} == {k: v for k, v in content.items() if k not in ('routes', 'texts')}
layout_ok = {k: v for k, v in old_layout.items() if k != 'routes'} == {k: v for k, v in layout.items() if k != 'routes'}
pickup_ids = {f'butter_bakery{i}' for i in range(1, 13)}
for previous, current in ((old_content['routes'], content['routes']), (old_layout['routes'], layout['routes'])):
    assert [r['id'] for r in previous] == [r['id'] for r in current]
    for a, b in zip(previous, current):
        if a['id'] not in pickup_ids: assert a == b, a['id']
        elif 'vehicle' in a:
            assert b['vehicle'] and b['pickupByRecipient']
            assert {k: v for k, v in a.items() if k not in ('vehicle', 'travelSeconds')} == {k: v for k, v in b.items() if k not in ('vehicle', 'travelSeconds', 'pickupByRecipient')}
        else: assert a['points'][1:] == b['points'][1:]
old_texts, new_texts = ({t['key']: t['de'] for t in d['texts']} for d in (old_content, content))
text_changes = [k for k, v in old_texts.items() if new_texts.get(k) != v]
texts_ok = set(text_changes) == {'quest.industry_sell.body', 'dialog.baker'} and set(new_texts) - set(old_texts) == {'bottleneck.pickup'}
wrappers = [f for f in baseline if f.startswith('Assets/Weedtopia/Art/Buildings/Prefabs/') and f.endswith('.prefab')]
wrappers_ok = len(wrappers) == 251 and not set(wrappers).intersection(changed + missing)
result = dict(utc=datetime.now(timezone.utc).isoformat(), baselineFiles=len(baseline),
    changedExistingFiles=changed, unexpectedChanges=unexpected, missingFiles=missing,
    originalSceneDocuments=len(old), newSceneDocuments=len(new) - len(old),
    missingOriginalSceneObjects=[k for k in old if k not in new], protectedSceneChanges=protected_changes,
    intentionalRoadAndPoolDocuments=intentional_changes, existingVehicleReferencesPreserved=bindings_preserved,
    centeredBuildingPrefabsUnchanged=len(wrappers) if wrappers_ok else 0,
    otherContentAndBalancePreserved=content_ok and texts_ok, sitesAndDistrictLayoutPreserved=layout_ok,
    passed=not (missing or unexpected or protected_changes) and bindings_preserved and content_ok and texts_ok and layout_ok and wrappers_ok)
(OUT / 'preservation.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
print(json.dumps(result, indent=2))
assert result['passed']
