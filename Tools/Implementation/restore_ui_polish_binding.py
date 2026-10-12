"""Restore the original added inventory-wheel prefab instance, preserving its serialized IDs."""
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Docs/UiPolishRevision/Baseline'

def blocks(text):
    starts=list(re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\s*$',text,re.M))
    return {m[2]:text[m.start():starts[i+1].start() if i+1<len(starts) else len(text)] for i,m in enumerate(starts)}

def ref(text,field):
    return re.search(r'\b'+field+r': \{fileID: (-?\d+)\}',text)[1]

for name in ('BurningHills','BigRockCity'):
    relative=Path('Assets/Weedtopia/Game/Scenes')/(name+'.unity')
    old=(BASE/relative).read_text(encoding='utf-8-sig')
    file=ROOT/relative;current=file.read_text(encoding='utf-8-sig')
    identity=re.search(r'  - id: wheelInventory\n    target: \{fileID: (\d+)\}',old)[1]
    if not re.search(r'  - id: wheelInventory\n    target: \{fileID: 0\}',current):
        print(name+': original binding already exists');continue
    source=blocks(old);present=blocks(current)
    prefab=ref(source[identity],'m_PrefabInstance')
    restore={prefab}|{i for i,text in source.items() if re.search(r'\bm_PrefabInstance: \{fileID: '+prefab+r'\}',text)}
    collision=restore&present.keys()
    if collision:raise RuntimeError('Unexpected partially restored prefab: '+str(collision))
    parent_transform=ref(source[prefab],'m_TransformParent')
    parent_prefab=ref(source[parent_transform],'m_PrefabInstance')
    entries=re.search(r'    m_AddedGameObjects:(.*?)(?=    m_AddedComponents:)',source[parent_prefab],re.S)[1]
    candidates=re.split(r'(?=^    - targetCorrespondingSourceObject:)',entries,flags=re.M)
    entry=next(e for e in candidates if any('addedObject: {fileID: '+i+'}' in e for i in restore))
    parent=present[parent_prefab]
    if '    m_AddedGameObjects: []\n' in parent:repaired=parent.replace('    m_AddedGameObjects: []\n','    m_AddedGameObjects:\n'+entry)
    else:repaired=parent.replace('    m_AddedGameObjects:\n','    m_AddedGameObjects:\n'+entry)
    current=current.replace(parent,repaired)
    current=re.sub(r'(  - id: wheelInventory\n    target: \{fileID: )0(\})',r'\g<1>'+identity+r'\2',current)
    insertion=''.join(source[i] for i in sorted(restore,key=int))
    marker=re.search(r'^--- !u!1660057539 ',current,re.M)
    current=current[:marker.start()]+insertion+current[marker.start():] if marker else current+insertion
    file.write_text(current,encoding='utf-8',newline='\n')
    print(name+': restored '+str(len(restore))+' original prefab records and binding '+identity)
