"""Verify serialized scene preservation and the actual saved garden/road mesh topology."""
import hashlib
import json
import math
from pathlib import Path
import re
import struct

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Docs/FarmGardenRevision'


def mesh_report(name):
    source = (ROOT / f'Assets/Weedtopia/Production/FarmGarden/Meshes/{name}.asset').read_text()
    count = int(re.search(r'm_VertexCount: (\d+)', source)[1])
    stride = int(re.search(r'm_DataSize: (\d+)', source)[1]) // count
    data = bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)', source)[1])
    vertices = [struct.unpack_from('<fff', data, index * stride) for index in range(count)]
    raw = bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)', source)[1])
    assert re.search(r'm_IndexFormat: 0', source), 'Expected 16-bit mesh indices'
    indices = struct.unpack('<' + 'H' * (len(raw) // 2), raw)
    areas = []
    for index in range(0, len(indices), 3):
        a, b, c = (vertices[k] for k in indices[index:index + 3])
        areas.append((b[2]-a[2])*(c[0]-a[0]) - (b[0]-a[0])*(c[2]-a[2]))
    return dict(mesh=name, vertices=count, triangles=len(areas),
                invertedTriangles=sum(area < -1e-6 for area in areas),
                degenerateTriangles=sum(abs(area) <= 1e-6 for area in areas),
                finiteVertices=all(math.isfinite(value) for vertex in vertices for value in vertex))


def documents(source):
    return {match[2]: (match[1], match[0]) for match in re.finditer(
        r'^--- !u!(\d+) &(\d+).*?\n.*?(?=^--- !u!|\Z)', source, re.M | re.S)}


def main():
    baseline = json.loads((REPORT / 'baseline.json').read_text())
    missing = [path for path in baseline if not (ROOT / path).exists()]
    changed = [path for path, digest in baseline.items() if (ROOT / path).exists()
               and hashlib.sha256((ROOT / path).read_bytes()).hexdigest() != digest]
    allowed = {'Assets/Weedtopia/Game/Scenes/BurningHills.unity',
               'Assets/Weedtopia/Scripts/Editor/GameSetup/InteractionVisualEvidence.cs'}
    original = documents((REPORT / 'Originals/BurningHills.unity').read_text())
    current = documents((ROOT / 'Assets/Weedtopia/Game/Scenes/BurningHills.unity').read_text())
    script_names = {}
    for path in (ROOT / 'Assets/Weedtopia/Scripts/Runtime').rglob('*.cs.meta'):
        script_names[re.search(r'guid: (\w+)', path.read_text())[1]] = path.name[:-5]
    component_changes = []
    component_counts = {}
    for identifier, (kind, body) in original.items():
        if kind != '114':
            continue
        match = re.search(r'm_Script: \{fileID: \d+, guid: (\w+)', body)
        if not match or match[1] not in script_names:
            continue
        name = script_names[match[1]]
        component_counts[name] = component_counts.get(name, 0) + 1
        if identifier not in current or current[identifier][1] != body:
            component_changes.append(dict(id=identifier, script=name))
    meshes = [mesh_report(name) for name in ('FarmRiverFlagstones', 'FarmRiverSoftVerge',
              'ContinuousBridgeDeck', 'WarehouseToPergola', 'OrchardGardenLoop')]
    result = dict(changedExisting=changed, missingExisting=missing,
                  unexpectedChangedExisting=sorted(set(changed)-allowed),
                  originalSceneDocumentsRetained=set(original) <= set(current),
                  runtimeComponentCounts=component_counts, changedRuntimeComponents=component_changes,
                  meshes=meshes)
    result['passed'] = (not missing and not result['unexpectedChangedExisting']
                        and result['originalSceneDocumentsRetained']
                        and all(item['script'] == 'LivingTownPresentation.cs' for item in component_changes)
                        and all(not item['invertedTriangles'] and not item['degenerateTriangles']
                                and item['finiteVertices'] for item in meshes))
    (REPORT / 'verification.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))
    raise SystemExit(0 if result['passed'] else 1)


if __name__ == '__main__':
    main()
