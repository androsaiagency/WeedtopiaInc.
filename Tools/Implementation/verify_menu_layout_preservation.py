"""Compare the menu migration with its immediate pre-change scene, including dirty user work."""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Docs/MenuLayoutRevision'
SCENE = ROOT / 'Assets/Weedtopia/Game/Scenes/BurningHills.unity'


def blocks(path):
    return {m[1]: m[0] for m in re.finditer(
        r'^--- !u!\d+ &(\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)', path.read_text(), re.M | re.S)}


def reference(block, field):
    match = re.search(r'\b' + field + r': \{fileID: (\d+)', block)
    return match[1] if match else None


before, after = blocks(REPORT / 'Originals/BurningHills.unity'), blocks(SCENE)
objects = {key: value for key, value in before.items() if value.splitlines()[1] == 'GameObject:'}
ui_object = next(key for key, value in objects.items() if '\n  m_Name: BurningHillsUI\n' in value)
transforms = {key: value for key, value in before.items()
              if value.splitlines()[1] in ('Transform:', 'RectTransform:')}
ui_transforms = {key for key, value in transforms.items() if reference(value, 'm_GameObject') == ui_object}
while True:
    children = {key for key, value in transforms.items() if reference(value, 'm_Father') in ui_transforms}
    if children.issubset(ui_transforms):
        break
    ui_transforms |= children
ui_objects = {reference(transforms[key], 'm_GameObject') for key in ui_transforms}
removed = sorted(before.keys() - after.keys())
assert not removed, f'Original scene objects removed: {removed}'
changed = sorted(key for key in before.keys() & after.keys() if before[key] != after[key])
presentation_guid = re.search(r'guid: (\w+)', (ROOT / 'Assets/Weedtopia/Scripts/Runtime/Unity/ProductionPresentation.cs.meta').read_text())[1]
for key in changed:
    block = before[key]
    if key in ui_objects or reference(block, 'm_GameObject') in ui_objects:
        continue
    if block.splitlines()[1] == 'PrefabInstance:' and reference(block, 'm_TransformParent') in ui_transforms:
        continue
    if presentation_guid in block:
        def without_currencies(value):
            return re.sub(r'  currencyValues:\n(?:  - .*\n)+', '', value)
        assert without_currencies(block) == without_currencies(after[key]), 'Unexpected presentation changes'
        continue
    raise AssertionError(f'Non-UI scene object changed: {key}')
result = {
    'passed': True, 'originalSceneObjects': len(before), 'removedOriginalObjects': 0,
    'changedObjects': len(changed), 'allChangesRestrictedToUiAndCurrencyBindings': True,
    'unchangedOriginalObjects': len(before) - len(changed), 'addedObjects': len(after.keys() - before.keys()),
    'sceneSha256': hashlib.sha256(SCENE.read_bytes()).hexdigest(),
}
(REPORT / 'preservation.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
