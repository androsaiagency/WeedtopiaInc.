"""Preserve the exact working state before the additive Saint Mary Ville integration."""
import argparse, hashlib, json, re, shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
FOLDER=ROOT/'Docs/SaintMaryVille/Preservation'
CONTENT='Assets/Weedtopia/Game/Content/BurningHills.json'
LOCALES=[f'Assets/Weedtopia/Game/Localization/{lang}.json' for lang in ('de','en','es','fr','ru')]
SAVE='Assets/Weedtopia/Scripts/Runtime/Persistence/LocalSaveStore.cs'
CAMPAIGN='Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab'
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def capture():
    FOLDER.mkdir(parents=True,exist_ok=True)
    manifest=FOLDER/'baseline.json'
    if manifest.exists(): return {'status':'retained','baseline':str(manifest)}
    files={p.relative_to(ROOT).as_posix():digest(p) for parent in ('Assets','ProjectSettings') for p in (ROOT/parent).rglob('*') if p.is_file() and 'SaintMaryVille' not in p.as_posix()}
    important=[CONTENT,*LOCALES,SAVE,CAMPAIGN,'ProjectSettings/EditorBuildSettings.asset','Assets/Weedtopia/Scripts/Tests/EditMode/GoldenFlowerCampaignTests.cs']+[p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/Weedtopia/Game/Scenes').glob('*.unity')]
    for name in important:
        backup=FOLDER/'Before'/name;backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/name,backup)
    manifest.write_text(json.dumps(files,indent=2),encoding='utf-8')
    return {'status':'captured','files':len(files)}
def additive(before,after,content=False):
    changes=[]
    for key,old in before.items():
        if content and key=='version':continue
        new=after.get(key)
        if isinstance(old,list) and old and isinstance(old[0],dict):
            identity='id' if 'id' in old[0] else 'key'; current={x[identity]:x for x in new}
            for item in old:
                expected=dict(item)
                if content and key=='buildings' and item[identity]=='residential_house':
                    expected['residentTierBonuses']=[0]*24+[135]
                if current.get(item[identity])!=expected:changes.append(f'{key}:{item[identity]}')
        elif old!=new:changes.append(key)
    return changes
def documents(p):return {m.group(1):m.group(0) for m in re.finditer(r'^--- !u!\d+ &(-?\d+).*?(?=^--- !u!|\Z)',p.read_text(encoding='utf-8'),re.M|re.S)}
def verify():
    baseline=read(FOLDER/'baseline.json');changed=[n for n,h in baseline.items() if (ROOT/n).exists() and digest(ROOT/n)!=h];removed=[n for n in baseline if not (ROOT/n).exists()]
    scenes=[n for n in baseline if n.startswith('Assets/Weedtopia/Game/Scenes/') and n.endswith('.unity')]
    allowed={CONTENT,*LOCALES,SAVE,CAMPAIGN,'ProjectSettings/EditorBuildSettings.asset','Assets/Weedtopia/Scripts/Tests/EditMode/GoldenFlowerCampaignTests.cs','Assets/Weedtopia/Scripts/Tests/EditorAssets/GoldenFlowerAssetTests.cs',*scenes}
    content=additive(read(FOLDER/'Before'/CONTENT),read(ROOT/CONTENT),True)
    locales={n:additive(read(FOLDER/'Before'/n),read(ROOT/n)) for n in LOCALES}
    original=(FOLDER/'Before'/SAVE).read_text(encoding='utf-8-sig')
    migration=(ROOT/SAVE).read_text(encoding='utf-8-sig')==original.replace('definitions.version > 10','definitions.version > 11')
    ui_guids=[re.search(r'guid: (\w+)',(ROOT/f'Assets/Weedtopia/Scripts/Runtime/Unity/{n}.cs.meta').read_text()).group(1) for n in ('CityCampaignUi','CityTravelRow')]
    scene_reports={}
    for n in [*scenes,CAMPAIGN]:
        old,new=documents(FOLDER/'Before'/n),documents(ROOT/n);unexpected=[]
        for i,doc in old.items():
            now=new.get(i,'')
            if doc==now or any(g in doc for g in ui_guids):continue
            if 'Transform:' in doc:
                pattern=r'\n  m_Children:.*?(?=\n  m_Father:)'; a=re.search(pattern,doc,re.S);b=re.search(pattern,now,re.S)
                if a and b:
                    ai=re.findall(r'fileID: (-?\d+)',a.group());bi=re.findall(r'fileID: (-?\d+)',b.group())
                    if bi[:len(ai)]==ai and all(x in new.keys()-old.keys() for x in bi[len(ai):]) and re.sub(pattern,'\n  m_Children: OMITTED',doc,flags=re.S)==re.sub(pattern,'\n  m_Children: OMITTED',now,flags=re.S):continue
            if 'PrefabInstance:' in doc:
                pattern=r'\n    m_Modifications:.*?(?=\n    m_RemovedComponents:)'
                if re.sub(pattern,'\n    m_Modifications: OMITTED',doc,flags=re.S)==re.sub(pattern,'\n    m_Modifications: OMITTED',now,flags=re.S):continue
            if 'SceneRoots:' in doc:continue
            unexpected.append(i)
        scene_reports[n]={'byteIdentical':digest(FOLDER/'Before'/n)==digest(ROOT/n),'originalObjects':len(old),'removedObjects':list(old.keys()-new.keys()),'addedObjects':len(new.keys()-old.keys()),'unexpectedChangedObjects':unexpected}
    report={'protectedFiles':len(baseline),'changedFiles':changed,'removedFiles':removed,'unexpectedChanges':[n for n in changed if n not in allowed],'previousContentChanges':content,'previousLocaleChanges':locales,'migrationOnly':migration,'housingChange':'Final tier bonus 20 -> 135; every lower tier unchanged','scenes':scene_reports}
    report['status']='failed' if removed or report['unexpectedChanges'] or content or any(locales.values()) or not migration or any(s['removedObjects'] or s['unexpectedChangedObjects'] for s in scene_reports.values()) else 'passed'
    (FOLDER/'result.json').write_text(json.dumps(report,indent=2),encoding='utf-8');return report
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['capture','verify']);r=capture() if p.parse_args().mode=='capture' else verify();print(json.dumps(r,indent=2));raise SystemExit(r['status']=='failed')