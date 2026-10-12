"""Compare the task-start snapshot with installed scenes, excluding their Canvas hierarchies."""
import json, re, hashlib
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
REPORT=ROOT/'Docs/UiExperienceRevision'
BASE=REPORT/'Baseline'

def scene_records(file):
    text=file.read_text(encoding='utf-8-sig')
    headers=list(re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\s*$',text,re.M))
    return {m[2]:(int(m[1]),text[m.end():headers[i+1].start() if i+1<len(headers) else len(text)]) for i,m in enumerate(headers)}

def reference(text,field):
    match=re.search(r'\b'+re.escape(field)+r': \{fileID: (-?\d+)\}',text)
    return match[1] if match else None

def ui_records(records):
    canvas_objects={reference(text,'m_GameObject') for cls,text in records.values() if cls==223}
    transforms={identity:(reference(text,'m_GameObject'),reference(text,'m_Father')) for identity,(cls,text) in records.items() if cls in (4,224)}
    ui_transforms={identity for identity,(obj,_) in transforms.items() if obj in canvas_objects}
    ui_prefabs=set()
    for _ in range(30):
        before=(len(ui_transforms),len(ui_prefabs))
        ui_transforms.update(identity for identity,(_,father) in transforms.items() if father in ui_transforms)
        ui_prefabs.update(identity for identity,(cls,text) in records.items() if cls==1001 and reference(text,'m_TransformParent') in ui_transforms)
        ui_transforms.update(identity for identity,(cls,text) in records.items() if cls in (4,224) and reference(text,'m_PrefabInstance') in ui_prefabs)
        if before==(len(ui_transforms),len(ui_prefabs)):break
    ui_objects={transforms[t][0] for t in ui_transforms if t in transforms}
    return {identity for identity,(cls,text) in records.items() if identity in ui_transforms or identity in ui_objects or identity in ui_prefabs or reference(text,'m_GameObject') in ui_objects or reference(text,'m_PrefabInstance') in ui_prefabs}

before=json.loads((BASE/'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8-sig'))
after=json.loads((ROOT/'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8-sig'))
before.pop('texts');after.pop('texts')
result={'gameplayContentUnchanged':before==after,'scenes':[],'issues':[]}
for scene in ('BurningHills','BigRockCity'):
    relative=Path('Assets/Weedtopia/Game/Scenes')/(scene+'.unity')
    old=scene_records(BASE/relative);new=scene_records(ROOT/relative)
    excluded=ui_records(old)|ui_records(new)
    changed=[]
    for identity,(cls,text) in old.items():
        if identity in excluded:continue
        newer=new.get(identity)
        # ProductionPresentation retains its world references and points the intro portrait at the new shared UI prefab.
        def normalize(value):return re.sub(r'^  introPortrait:.*\n','',value,flags=re.M).strip()
        if newer is None or newer[0]!=cls or normalize(text)!=normalize(newer[1]):changed.append({'fileId':identity,'class':cls})
    added=[identity for identity in new if identity not in old and identity not in excluded]
    result['scenes'].append({'scene':scene,'nonUiRecordsCompared':len(old)-len(ui_records(old)),'changedOutsideUi':changed,'addedOutsideUi':added})
    if changed or added:result['issues'].append(scene+' has records outside the UI to review')
result['passed']=result['gameplayContentUnchanged'] and not result['issues']
(REPORT/'preservation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
if not result['passed']:raise SystemExit(1)
