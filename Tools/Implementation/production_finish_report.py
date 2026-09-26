"""Evidence and handoff data; never grants approval or mutates game assets."""
import csv,hashlib,json,re,zipfile
from pathlib import Path
from datetime import datetime,timezone
R=Path(__file__).resolve().parents[2];O=R/'Docs/ProductionFinish'
def read(p): return json.loads((R/p).read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def dump(path,data): (O/path).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
baseline=read('Docs/ProductionFinish/baseline.json')['files']
moved=[];missing=[];changed=[];same=0
for path,old in baseline.items():
    p=R/path
    if p.exists():
        if sha(p)==old: same+=1
        else: changed.append(path)
    else:
        archived=R/'Assets/Weedtopia/Game/Archive/ReplacedPresentation'/p.name
        if archived.exists() and sha(archived)==old:moved.append({'from':path,'to':archived.relative_to(R).as_posix(),'sha256':old})
        else: missing.append(path)
manifest=read('Docs/ArtProduction/BUILDING_ASSET_MANIFEST.json')
protected=[p for p in baseline if p.startswith('Docs/ArtProduction/') or '/Buildings/Prefabs/' in p or 'BuildingVisualCatalog.asset' in p]
wrappers=[s['unity_prefab_target'] for s in manifest['slots']]
protected=list(set(protected+wrappers+[p+'.meta' for p in wrappers]))
violations=[p for p in protected if p in baseline and (not (R/p).exists() or sha(R/p)!=baseline[p])]
with zipfile.ZipFile(O/'editable_originals.zip') as z:
    before=json.loads(z.read('Assets/Weedtopia/Game/Content/BurningHills.json').decode('utf-8-sig'))
after=read('Assets/Weedtopia/Game/Content/BurningHills.json');before.pop('texts');gameplay=dict(after);gameplay.pop('texts')
core=[p for p in changed if '/Runtime/Domain/' in p or '/Runtime/Application/' in p or '/Runtime/Persistence/' in p]
dump('Evidence/preservation.json',{'utc':datetime.now(timezone.utc).isoformat(),'baselineFiles':len(baseline),'unchanged':same,'intentionallyMoved':moved,'changedExisting':changed,'missing':missing,'protectedViolations':violations,'wrappersChecked':len(wrappers),'wrappersByteIdentical':all(sha(R/p)==baseline[p] for p in wrappers),'gameplayDefinitionsUnchanged':before==gameplay,'domainApplicationPersistenceChanged':core,'passed':not(missing or violations or core) and before==gameplay})

# Keep every audit row and add the actual imported sprite slots and serialized UI bindings.
with (O/'VISUAL_ASSET_REGISTER.csv').open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
rows=[x for x in rows if not x['id'].startswith(('sprite/','binding/','archive/','audio/upgrade','audio/delivery','font/'))]
fields=list(rows[0]);art='Assets/Weedtopia/Production/';scene='Assets/Weedtopia/Game/Scenes/BurningHills.unity'
for row in rows:
    if row['category']=='building':
        row.update(unity_integration='wrapper retained; sprite not integrated',gameview_qa='blocked by explicit building approval',style_qa='source accepted; no productive sprite QA')
        continue
    row.update(final_asset=scene,status='prototype',style_qa='original authored art; final user acceptance open',unity_integration='imported; serialized scene or prefab references',gameview_qa='see current Game View matrix; not human playthrough',source_or_provenance='Docs/ProductionFinish/PROVENANCE_AND_LICENSES.md',owner_note='New presentation integrated. Global release remains blocked by building art and independent acceptance gates.')
    if row['category']=='tutorial_site': row['owner_note']='16 instance IDs, coordinates and gameplay definitions preserved; building appearance still blocked.'
    if row['category']=='character':
        name=row['id'].split('/')[0].replace('settlement_gardener','gardener').replace('industry_baker','baker')
        row['final_asset']=art+('Prefabs/Character_'+name+'.prefab' if row['id'].endswith('world_character') else 'Art/Characters.png#portrait_'+name)
    if row['category']=='icon':row['final_asset']=art+'PresentationArt.asset#'+{'cancel':'back','rarity':'star'}.get(row['id'].split('/')[1],row['id'].split('/')[1])
    if row['category']=='audio':
        name={'accept':'confirm','blocked':'unavailable','music':'afterglow_loop_60s','ambience':'afterglow_loop_60s'}[row['id'].split('/')[1]]
        row.update(final_asset='Assets/Audio/Approved/BurningHills/'+name+'.wav',style_qa='not auditioned',gameview_qa='not applicable; audio listening open',owner_note='PCM validated and imported. Listening and commercial license eligibility/registration remain open. Ambience shares the music bed.')
def add(id,category,screen,current,final,source,note,style='original generated art; final acceptance pending'):
    rows.append(dict(zip(fields,[id,category,screen,current,final,source,'prototype',style,'imported and referenced','see Game View matrix; no user approval inferred',note])))
for s in read('Assets/Weedtopia/Production/art_slices.json')['entries']:
    add('sprite/'+s['name'],'sprite_slot','World / reachable UI','',art+'Art/'+s['file']+'#'+s['name'],'generation_prompts.json; art_provenance.json','Unity sub-sprite: '+str(s['width'])+'x'+str(s['height'])+'; pivot '+str(s['pivotX'])+','+str(s['pivotY'])+'; source pixels unchanged')
sceneText=(R/scene).read_text(encoding='utf-8-sig')
for id,fileid in re.findall(r'- id: (.+)\n\s+target: \{fileID: (\d+)\}',sceneText):
    add('binding/'+id,'ui_binding',scene+'#'+fileid,scene,scene+'#'+fileid,'ProductionUiAuthoring.cs; generated Skin/Icons; Lato OFL','Reachable serialized UI target; hidden templates remain included in preflight. Inactive debug/hud legacy bindings retained for compatibility.' if id in ('development','hud') else 'Exact serialized binding; required target must remain present.')
for name in ('upgrade','delivery'):
    add('audio/'+name,'audio','GameRoot','',f'Assets/Audio/Approved/BurningHills/{name}.wav','audio_provenance.json','State-triggered original cue; hearing/license gates open','PCM validated; not auditioned')
for name in ('Lato-Regular.ttf','Lato-Bold.ttf'):
    add('font/'+name,'font','All UI','LegacyRuntime.ttf',art+'Fonts/'+name,'https://github.com/google/fonts/tree/main/ofl/lato','SIL OFL 1.1; original unmodified font; license bundled','German text inspected in Game View')
for item in moved:
    if item['from'].endswith('.meta'):continue
    add('archive/'+Path(item['from']).name,'archived_presentation','Not used by game',item['from'],item['to'],'Preserved original SHA256 '+item['sha256'],'GUID and original bytes preserved; replaced from active game dependencies','archived')
    rows[-1]['status']='archived';rows[-1]['unity_integration']='archived; no active game reference';rows[-1]['gameview_qa']='not active'
assert len({x['id'] for x in rows})==len(rows)
dump('register_rows.json',{'fields':fields,'rows':rows,'source':'current manifest, scene YAML bindings, imported sprite metadata and preservation hashes'})
print(json.dumps({'registerRows':len(rows),'bindings':sum(x['category']=='ui_binding' for x in rows),'preservationMissing':missing,'protectedViolations':violations,'changedExisting':len(changed),'moved':len(moved)}))
