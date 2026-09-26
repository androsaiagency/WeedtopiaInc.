"""Verify that the navigation migration retains the authored world and gameplay references."""
import json
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
REPORT=ROOT/'Docs/WorldNavigationRevision'
SCENE=Path('Assets/Weedtopia/Game/Scenes/BurningHills.unity')

def documents(path):
    return {m[2]:(m[1],m[0]) for m in re.finditer(
        r'^--- !u!(\d+) &(\d+).*?\n.*?(?=^--- !u!|\Z)',path.read_text(),re.M|re.S)}

original=documents(REPORT/'Originals'/SCENE)
current=documents(ROOT/SCENE)
scripts={re.search(r'guid: (\w+)',p.read_text())[1]:p.name[:-5]
         for p in (ROOT/'Assets/Weedtopia/Scripts/Runtime').rglob('*.cs.meta')}
counts={}
changed=[]
for identifier,(kind,body) in original.items():
    if kind!='114':continue
    script=re.search(r'm_Script: \{fileID: \d+, guid: (\w+)',body)
    if not script or script[1] not in scripts:continue
    name=scripts[script[1]]
    counts[name]=counts.get(name,0)+1
    if identifier not in current or current[identifier][1]!=body:changed.append({'id':identifier,'script':name})
result=dict(originalSceneDocumentsRetained=set(original)<=set(current),
            runtimeComponentCounts=counts,changedRuntimeComponents=changed)
result['passed']=result['originalSceneDocumentsRetained'] and all(x['script']=='GameUi.cs' for x in changed)
(REPORT/'preservation.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
raise SystemExit(0 if result['passed'] else 1)
