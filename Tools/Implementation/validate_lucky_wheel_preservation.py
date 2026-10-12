"""Compare the pre-feature snapshot, content and authored Unity identities."""
import json
import re
from pathlib import Path

root=Path('Docs/LuckyWheel')
old=root/'Originals'
scene=Path('Assets/Weedtopia/Game/Scenes/BurningHills.unity')
before=(old/scene).read_text(encoding='utf-8-sig')
after=scene.read_text(encoding='utf-8-sig')
pattern=r'^--- !u!\d+ &(-?\d+)(?: stripped)?\n'
old_ids=set(re.findall(pattern,before,re.M))
new_ids=set(re.findall(pattern,after,re.M))
original=json.loads((old/'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8-sig'))
current=json.loads(Path('Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8-sig'))
old_keys={t['key'] for t in original['texts']}
compare=current.copy()
compare.pop('wheelRewards')
compare['texts']=[t for t in current['texts'] if t['key'] in old_keys]
inventory=Path('Assets/Weedtopia/Production/Rewards/RewardCollection.prefab')
result=dict(originalContentPreserved=compare==original,allExistingSceneIdsPreserved=old_ids<=new_ids,
            originalSceneObjects=len(old_ids),currentSceneObjects=len(new_ids),newObjects=len(new_ids-old_ids),
            removedIds=sorted(old_ids-new_ids),inventoryPrefabPreserved=(old/inventory).read_bytes()==inventory.read_bytes())
assert result['originalContentPreserved'] and result['allExistingSceneIdsPreserved'] and result['inventoryPrefabPreserved'],result
(root/'preservation_receipt.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
