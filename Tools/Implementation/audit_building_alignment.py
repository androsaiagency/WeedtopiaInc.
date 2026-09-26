"""Verify the parcel alignment changes only the authored sprite position and its inset."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, re, zipfile

root = Path(__file__).resolve().parents[2]
out = root / 'Docs/BuildingAlignment'
baseline = json.loads((out / 'baseline.json').read_text(encoding='utf-8'))
manifest = json.loads((root / 'Docs/ArtProduction/BUILDING_ASSET_MANIFEST.json').read_text(encoding='utf-8'))
prefabs = {s['unity_prefab_target'] for s in manifest['slots']}
sources = {
    'Assets/Weedtopia/Scripts/Runtime/Buildings/Presentation/BuildingSpriteVisual.cs',
    'Assets/Weedtopia/Scripts/Editor/GameSetup/ProductionBuildingIntegration.cs',
    'Assets/Weedtopia/Scripts/Editor/GameSetup/InteractionVisualEvidence.cs',
}
missing = [p for p in baseline if not (root / p).is_file()]
changed = [p for p, digest in baseline.items() if (root / p).is_file() and hashlib.sha256((root / p).read_bytes()).hexdigest() != digest]
unexpected = set(changed) - prefabs - sources
rows = []
with zipfile.ZipFile(out / 'original_wrappers.zip') as archive:
    for path in sorted(prefabs):
        old = archive.read(path).decode('utf-8-sig').replace('\r\n', '\n')
        new = (root / path).read_text(encoding='utf-8-sig')
        def blocks(text):
            return {m[1]: m[0] for m in re.findall(r'(^--- !u!\d+ &(-?\d+)[^\n]*\n.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}
        before, after = blocks(old), blocks(new)
        assert before.keys() == after.keys(), path
        sprite = next(v for v in before.values() if '  m_Name: ProductionSpriteVisual\n' in v)
        transform_id = re.search(r'component: \{fileID: (\d+)\}', sprite).group(1)
        component_id = next(k for k, v in before.items() if '  resourcePath: WeedtopiaBuildings/' in v)
        changed_blocks = [k for k in before if before[k] != after[k]]
        assert set(changed_blocks) == {transform_id, component_id}, (path, changed_blocks)
        assert re.sub(r'^  m_LocalPosition:.*$', '', before[transform_id], flags=re.M) == re.sub(r'^  m_LocalPosition:.*$', '', after[transform_id], flags=re.M), path
        assert before[component_id] == re.sub(r'^  footprintInset:.*\n', '', after[component_id], flags=re.M), path
        inset = float(re.search(r'footprintInset: ([\d.]+)', after[component_id]).group(1))
        def position(block):
            return tuple(map(float, re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)', block).groups()))
        a, b = position(before[transform_id]), position(after[transform_id])
        delta = tuple(y-x for x, y in zip(a, b))
        assert inset > 0 and delta[1] < 0 and abs(delta[0]-delta[2]) < 0.00001, (path, delta)
        assert abs(sum(x*x for x in delta)**.5 - inset) < .00002, (path, delta, inset)
        rows.append(dict(prefab=path, inset=inset, before=a, after=b))
result = dict(utc=datetime.now(timezone.utc).isoformat(), passed=not missing and not unexpected,
              baselineFiles=len(baseline), alignedVariants=len(rows), missingFiles=missing,
              unexpectedChanges=sorted(unexpected), onlySpritePositionAndInsetChanged=True,
              sceneArtCatalogGameplayAndAnchorsUnchanged=True, variants=rows)
(out / 'preservation.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
assert result['passed'], result
print(json.dumps({k:v for k,v in result.items() if k != 'variants'}, indent=2))
