"""Audit against the immediate scene baseline, including pre-existing uncommitted work."""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Docs/ConveyorRevision'
SCENE = ROOT / 'Assets/Weedtopia/Game/Scenes/BurningHills.unity'


def blocks(path):
    return {m[1]: m[0] for m in re.finditer(
        r'^--- !u!\d+ &(\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)', path.read_text(), re.M | re.S)}


def reference(block, field):
    match = re.search(r'\b' + field + r': \{fileID: (\d+)', block)
    return match[1] if match else None


def without_array(block, field):
    return re.sub(r'^  ' + field + r':[^\n]*\n(?:  - [^\n]*\n)*', '', block, flags=re.M)


before, after = blocks(REPORT / 'Originals/BurningHills.unity'), blocks(SCENE)
assert not before.keys() - after.keys(), 'An original serialized object was removed'
route_guid = re.search(r'guid: (\w+)', (ROOT / 'Assets/Weedtopia/Scripts/Runtime/Unity/PresentationRoute.cs.meta').read_text())[1]
presentation_guid = re.search(r'guid: (\w+)', (ROOT / 'Assets/Weedtopia/Scripts/Runtime/Unity/ProductionPresentation.cs.meta').read_text())[1]
routes = {k: b for k, b in before.items() if route_guid in b}
belts = {reference(b, 'belt') for b in routes.values() if '\n  conveyor: 1\n' in b
         and '\n  m_Enabled: 1\n' in before[reference(b, 'belt')]
         and '\n  m_Enabled: 0\n' in after[reference(b, 'belt')]}
active_routes = {k: b for k, b in routes.items() if reference(b, 'belt') in belts}
assert len(active_routes) == len(belts) == 9
route_objects = {reference(b, 'm_GameObject') for b in active_routes.values()}
route_transforms = {k for k, b in before.items() if b.splitlines()[1] == 'Transform:'
                    and reference(b, 'm_GameObject') in route_objects}
parents = {reference(before[k], 'm_Father') for k in route_transforms}
assert len(parents) == 1
cargo = set()
for block in active_routes.values():
    array = re.search(r'  cargoMarkers:\n((?:  - [^\n]*\n)+)', block)[1]
    cargo.update(re.findall(r'fileID: (\d+)', array))
cargo_instances = {reference(before[k], 'm_PrefabInstance') for k in cargo}
assert len(cargo_instances) == 27
archive_object = next(k for k, b in after.items() if '\n  m_Name: ArchivedConveyorCargo\n' in b)
archive_transform = next(k for k, b in after.items() if b.splitlines()[1] == 'Transform:' and reference(b, 'm_GameObject') == archive_object)
assert '\n  m_IsActive: 0\n' in after[archive_object]
changed = [k for k in before if before[k] != after[k]]
for key in changed:
    original, current = before[key], after[key]
    if key in routes:
        current = without_array(current, 'conveyorPath')
        current = re.sub(r'^  (cargoSpeed|spawnDelay):.*\n', '', current, flags=re.M)
        if key in active_routes:
            original = without_array(original, 'cargoMarkers')
            current = without_array(current, 'cargoMarkers')
    elif key in belts:
        current = current.replace('\n  m_Enabled: 0\n', '\n  m_Enabled: 1\n')
    elif key in route_transforms | parents:
        original, current = without_array(original, 'm_Children'), without_array(current, 'm_Children')
    elif key in cargo_instances:
        assert reference(current, 'm_TransformParent') == archive_transform
        original = re.sub(r'm_TransformParent: \{fileID: \d+\}', 'm_TransformParent: ARCHIVE', original)
        current = re.sub(r'm_TransformParent: \{fileID: \d+\}', 'm_TransformParent: ARCHIVE', current).replace('value: -0\n', 'value: 0\n')
    elif presentation_guid in original:
        current = re.sub(r'^  conveyors:.*\n', '', current, flags=re.M)
    elif key in {'1074672286', '941554681'}:
        # Existing lantern and hedge: only position and corresponding sprite depth may change.
        def without_placement(value):
            return re.sub(r'(propertyPath: (?:m_LocalPosition\.[xyz]|m_SortingOrder)\n      value:) [^\n]+',
                          r'\1 PLACEMENT', value)
        original, current = without_placement(original), without_placement(current)
    else:
        raise AssertionError(f'Unexpected scene change: {key}')
    assert original == current, f'Additional changes outside the conveyor migration: {key}'

result = {
    'passed': True, 'baseline': 'immediate pre-conveyor scene including prior menu and world changes',
    'originalSerializedObjects': len(before), 'removedOriginalObjects': 0,
    'unchangedOriginalObjects': len(before) - len(changed), 'changedObjects': len(changed),
    'addedObjects': len(after.keys() - before.keys()), 'originalRouteComponentsPreserved': len(routes),
    'logicalRoutePointsAndIdsUnchanged': True, 'disabledOriginalConveyorRenderers': len(belts),
    'legacyCargoArchived': len(cargo_instances), 'allOtherSceneObjectsUnchanged': True,
    'decorativePropsRepositionedForClearance': 2,
    'sceneSha256': hashlib.sha256(SCENE.read_bytes()).hexdigest(),
    'artSha256': {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT / 'Assets/Weedtopia/Production/Conveyors/Art').glob('*.png')},
}
(REPORT / 'preservation.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
