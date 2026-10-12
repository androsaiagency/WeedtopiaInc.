"""Check the additive prefab change against the immediate pre-task baseline."""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Docs/ConstructionFences'


def blocks(path):
    return {m[1]: m[0] for m in re.finditer(
        r'^--- !u!\d+ &(\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)', path.read_text(), re.M | re.S)}


scene = ROOT / 'Assets/Weedtopia/Game/Scenes/BurningHills.unity'
prefab = ROOT / 'Assets/Weedtopia/Game/Prefabs/BuildingSite.prefab'
assert scene.read_bytes() == (REPORT / 'Originals/BurningHills.unity').read_bytes()
original, current = blocks(REPORT / 'Originals/BuildingSite.prefab'), blocks(prefab)
assert not original.keys() - current.keys()
changed = [key for key in original if original[key] != current[key]]
assert len(changed) == 2, changed
for key in changed:
    before, after = original[key], current[key]
    if '  constructionFence:' in after:
        assert before == re.sub(r'^  constructionFence:.*\n', '', after, flags=re.M)
    else:
        assert before.splitlines()[1] == 'Transform:'
        old_children = re.search(r'  m_Children:\n((?:  - .*\n)+)', before)[1]
        new_children = re.search(r'  m_Children:\n((?:  - .*\n)+)', after)[1]
        assert new_children.startswith(old_children)
        assert len(new_children.splitlines()) == len(old_children.splitlines()) + 1
        assert before == after.replace(new_children, old_children)
hashes = json.loads((REPORT / 'Originals/building_hashes.json').read_text())
for file, expected in hashes.items():
    assert hashlib.sha256((ROOT / file).read_bytes()).hexdigest() == expected, file
result = {'passed': True, 'sceneByteIdentical': True, 'protectedBuildingWrappersByteIdentical': len(hashes),
          'originalSitePrefabObjects': len(original), 'removedSitePrefabObjects': 0,
          'changesRestrictedToChildAndReference': True, 'addedPrefabBlocks': len(current) - len(original),
          'sceneSha256': hashlib.sha256(scene.read_bytes()).hexdigest(),
          'sitePrefabSha256': hashlib.sha256(prefab.read_bytes()).hexdigest(),
          'artSha256': {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
                        for p in (ROOT / 'Assets/Weedtopia/Production/Construction/Art').glob('*.png')}}
(REPORT / 'preservation.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
