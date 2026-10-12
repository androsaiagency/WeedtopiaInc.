"""Verify this UI revision against its snapshot, including stripped prefab GameObject ownership."""
import hashlib, json, re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
REPORT=ROOT/'Docs/UiRefinementRevision'
BASE=REPORT/'Baseline'

def records(file):
    text=file.read_text(encoding='utf-8-sig')
    headers=list(re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\s*$',text,re.M))
    return {m[2]:(int(m[1]),text[m.end():headers[i+1].start() if i+1<len(headers) else len(text)]) for i,m in enumerate(headers)}

def ref(text,field):
    m=re.search(r'\b'+re.escape(field)+r': \{fileID: (-?\d+)\}',text)
    return m[1] if m else None

def ui_ids(data):
    canvas={ref(text,'m_GameObject') for cls,text in data.values() if cls==223}
    transforms={i:(ref(text,'m_GameObject'),ref(text,'m_Father')) for i,(cls,text) in data.items() if cls in (4,224)}
    descendants={i for i,(obj,_) in transforms.items() if obj in canvas}
    prefabs=set()
    for _ in range(50):
        before=len(descendants),len(prefabs)
        descendants.update(i for i,(_,father) in transforms.items() if father in descendants)
        prefabs.update(i for i,(cls,text) in data.items() if cls==1001 and ref(text,'m_TransformParent') in descendants)
        descendants.update(i for i,(cls,text) in data.items() if cls in (4,224) and ref(text,'m_PrefabInstance') in prefabs)
        if before==(len(descendants),len(prefabs)):break
    objects={transforms[i][0] for i in descendants if i in transforms}
    # A stripped prefab GameObject can be serialized without its inherited Transform.
    # Added UI components point to that GameObject rather than to the prefab instance itself.
    objects.update(i for i,(cls,text) in data.items() if cls==1 and ref(text,'m_PrefabInstance') in prefabs)
    return {i for i,(cls,text) in data.items() if i in descendants or i in objects or i in prefabs or ref(text,'m_GameObject') in objects or ref(text,'m_PrefabInstance') in prefabs}

content=Path('Assets/Weedtopia/Game/Content/BurningHills.json')
result={'contentByteIdentical':(BASE/content).read_bytes()==(ROOT/content).read_bytes(),'scenes':[],'protectedPrefabCount':0,'issues':[]}
for name in ('BurningHills','BigRockCity'):
    relative=Path('Assets/Weedtopia/Game/Scenes')/(name+'.unity')
    before,after=records(BASE/relative),records(ROOT/relative)
    excluded=ui_ids(before)|ui_ids(after)
    changed=[i for i,item in before.items() if i not in excluded and after.get(i)!=item]
    added=[i for i in after if i not in before and i not in excluded]
    result['scenes'].append({'scene':name,'nonUiRecordsCompared':len(before)-len(ui_ids(before)),'changedOutsideUi':changed,'addedOutsideUi':added})
    if changed or added:result['issues'].append(name+' has a change outside the Canvas hierarchy')
manifest=json.loads((REPORT/'baseline.json').read_text())
result['protectedArtworkCount']=0
result['protectedContentCount']=0
for relative,digest in manifest.items():
    # The revision adds player portraits but preserves all pre-existing source art and content.
    if relative.lower().endswith(('.png','.jpg','.jpeg','.psd','.tga')) or relative.startswith('Assets/Weedtopia/Game/Content/'):
        kind='protectedContentCount' if relative.startswith('Assets/Weedtopia/Game/Content/') else 'protectedArtworkCount'
        result[kind]+=1
        current=ROOT/relative
        if not current.exists() or hashlib.sha256(current.read_bytes()).hexdigest()!=digest:
            result['issues'].append('Protected source changed: '+relative)
    # World characters, deliveries and cargo use renderers, not Unity UI Images.
    if not relative.endswith('.prefab'):continue
    baseline=BASE/relative
    if 'CanvasRenderer:' in baseline.read_text(encoding='utf-8-sig'):continue
    result['protectedPrefabCount']+=1
    current=ROOT/relative
    if not current.exists() or hashlib.sha256(current.read_bytes()).hexdigest()!=digest:result['issues'].append('Protected prefab changed: '+relative)
result['passed']=result['contentByteIdentical'] and not result['issues']
(REPORT/'preservation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
if not result['passed']:raise SystemExit(1)
