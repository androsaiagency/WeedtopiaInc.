"""Verify additive scene/prefab changes against the starting snapshot."""
import json
import re
from pathlib import Path

root=Path('Docs/InventoryWheelDock')
paths=['Assets/Weedtopia/Game/Scenes/BurningHills.unity','Assets/Weedtopia/Production/Prefabs/WorldNavigationDock.prefab']
reports=[]
for path in paths:
    p=Path(path)
    before=(root/'Originals'/p).read_text(encoding='utf-8-sig')
    after=p.read_text(encoding='utf-8-sig')
    pattern=r'^--- !u!\d+ &(-?\d+)(?: stripped)?\n'
    old=set(re.findall(pattern,before,re.M));new=set(re.findall(pattern,after,re.M))
    report=dict(path=path,originalObjects=len(old),currentObjects=len(new),removedIds=sorted(old-new),
                metaPreserved=(root/'Originals'/Path(path+'.meta')).read_bytes()==Path(path+'.meta').read_bytes())
    assert not report['removedIds'] and report['metaPreserved'],report
    reports.append(report)
(root/'preservation_receipt.json').write_text(json.dumps(dict(status='passed',assets=reports),indent=2)+'\n',encoding='utf-8')
print(json.dumps(reports,indent=2))
