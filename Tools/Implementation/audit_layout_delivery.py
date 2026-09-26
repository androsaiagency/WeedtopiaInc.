"""Compare the current delivery with the read-only snapshot taken before the layout revision."""
from pathlib import Path
import hashlib,json,re,zipfile
from datetime import datetime,timezone
import yaml
ROOT=Path(__file__).resolve().parents[2]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
baseline=json.loads((ROOT/'Docs/LayoutRevision/baseline.json').read_text())['files']
missing=[p for p in baseline if not (ROOT/p).is_file()]
changed=[p for p,h in baseline.items() if (ROOT/p).is_file() and sha(ROOT/p)!=h]
protected=[p for p in baseline if p.startswith('Docs/ArtProduction/') and p!='Docs/ArtProduction/PROGRESS.json' or Path(p).suffix.lower() in ['.png','.wav','.ttf','.fbx'] or p.endswith('.meta')]
protected_changes=[p for p in protected if p in changed or p in missing]
def anchors(text):
    names={};transforms={}
    for match in re.finditer(r'^--- !u!(\d+) &(\d+)(?:[^\n]*)\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S):
        kind,num,body=match.groups()
        if kind not in ('1','4'):continue
        data=yaml.load(body,Loader=yaml.CSafeLoader)
        if kind=='1':names[int(num)]=data['GameObject']['m_Name']
        elif kind=='4':transforms[int(num)]=data['Transform']
    wanted={'VisualRoot','InteractionAnchor','WorkerAnchor','DeliveryInputAnchor','DeliveryOutputAnchor','VfxAnchor','ColliderRoot'}
    result={names[t['m_GameObject']['fileID']]:{k:t[k] for k in ['m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father']} for t in transforms.values() if names.get(t.get('m_GameObject',{}).get('fileID')) in wanted}
    assert len(result)==7, 'All seven wrapper anchors must be covered'
    return result
manifest=json.loads((ROOT/'Docs/ArtProduction/BUILDING_ASSET_MANIFEST.json').read_text(encoding='utf-8-sig'))
anchor_changes=[];converted=0
with zipfile.ZipFile(ROOT/'Docs/LayoutRevision/editable_originals.zip') as archive:
    for entry in manifest['slots']:
        path=entry['unity_prefab_target'];old=archive.read(path).decode('utf-8-sig');new=(ROOT/path).read_text(encoding='utf-8-sig')
        if anchors(old)!=anchors(new):anchor_changes.append(path)
        converted+=('ProductionSpriteVisual' in new and 'resourcePath: WeedtopiaBuildings/'+entry['building_id']+'/'+entry['tier_id'] in new)
catalogs=[p for p in baseline if Path(p).name=='BuildingVisualCatalog.asset']
assert catalogs, 'Catalog coverage must not be empty'
preserved_prefabs=['Assets/Weedtopia/Game/Prefabs/BuildingSite.prefab','Assets/Weedtopia/Production/Prefabs/DeliveryTruck.prefab']
prefab_checks={p:sha(ROOT/p)==baseline[p] for p in preserved_prefabs}
assert all(prefab_checks.values()), 'Original shared prefabs must remain byte-identical'
result={'utc':datetime.now(timezone.utc).isoformat(),'baselineFiles':len(baseline),'missing':missing,'protectedChanged':protected_changes,
        'allSourcesAndOriginalArtPreserved':not protected_changes,'anchorChanges':anchor_changes,'wrapperAnchorsPreserved':not anchor_changes,
        'integratedWrappers':converted,'catalogsByteIdentical':all(sha(ROOT/p)==baseline[p] for p in catalogs),'catalogs':catalogs,'preservedSharedPrefabs':prefab_checks,'changedExistingFiles':changed}
(ROOT/'Docs/LayoutRevision/preservation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='changedExistingFiles'},indent=2));print('Changed existing files:',len(changed))
