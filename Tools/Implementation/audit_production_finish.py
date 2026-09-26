"""Read current assets and preserve a fresh, immutable production-finish baseline."""
import csv, hashlib, json, re, shutil, zipfile
from pathlib import Path
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/ProductionFinish'
OUT.mkdir(parents=True, exist_ok=True)
baseline = OUT / 'baseline.json'
if baseline.exists():
    raise SystemExit('Existing baseline preserved; do not overwrite it.')
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
paths = [p for folder in ('Assets','Packages','ProjectSettings','Docs/ArtProduction','Tools') for p in (ROOT/folder).rglob('*') if p.is_file()]
manifest = json.loads((ROOT/'Docs/ArtProduction/BUILDING_ASSET_MANIFEST.json').read_text(encoding='utf-8-sig'))
content = json.loads((ROOT/'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8-sig'))
slots = manifest['slots']
assert len(slots)==251 and len({s['reference_image'] for s in slots})==251
assert all((ROOT/s['reference_image']).is_file() and (ROOT/s['unity_prefab_target']).is_file() for s in slots)
data={'utc':datetime.now(timezone.utc).isoformat(),'files':{p.relative_to(ROOT).as_posix():sha(p) for p in paths},'buildingSlots':251,'instances':len(content['instances']),'userVisualApproval':False}
baseline.write_text(json.dumps(data,indent=2),encoding='utf-8')
with zipfile.ZipFile(OUT/'editable_originals.zip','x',zipfile.ZIP_DEFLATED) as archive:
    for p in paths:
        rel=p.relative_to(ROOT).as_posix()
        if rel.startswith(('Assets/Weedtopia/Scripts/','Assets/Weedtopia/Game/','ProjectSettings/')) and p.suffix not in ('.png','.wav'):
            archive.write(p,rel)
(OUT/'Baseline').mkdir(exist_ok=True)
for p in (ROOT/'Docs/Implementation/Validation/Screenshots').glob('*.png'):
    shutil.copy2(p,OUT/'Baseline'/p.name)
rows=[]
def row(id,category,scene,current,final,source,status='prototype',note=''):
    rows.append(dict(id=id,category=category,scene_or_screen=scene,current_asset=current,final_asset=final,source_or_provenance=source,status=status,style_qa='pending',unity_integration='pending',gameview_qa='pending',owner_note=note))
for s in slots:
    row(s['building_id']+'/'+s['tier_id'],'building','BurningHills / level bands',s['unity_prefab_target'],'',s['reference_image'],'approved_source_not_integrated','Explicit four-prototype approval required; wrapper protected')
for i in content['instances']:
    row('site/'+i['id'],'tutorial_site','BurningHills', 'Assets/Weedtopia/Game/Prefabs/BuildingSite.prefab','',i['id'],note='Preserve gameplay ID, position and collider')
for item in [('terrain','world'),('roads','logistics'),('conveyors','logistics'),('loading_docks','logistics'),('vehicle','logistics'),('cargo','logistics'),('parcel_edges','world'),('vegetation','world'),('trees','world'),('rebuild_props','world'),('lanterns','world'),('world_boundary','world'),('water','world'),('selection','feedback'),('upgrade','feedback'),('delivery','feedback'),('ambient_light','feedback')]:
    row(item[0],item[1],'BurningHills','Assets/Weedtopia/Game/Scenes/BurningHills.unity','','Current scene',note='Replace or author visible presentation')
for name in ('rocko','mila','jaro','nia','city_host','settlement_gardener','industry_baker','driver'):
    for kind in ('world_character','portrait'):
        row(name+'/'+kind,'character','World / Intro / Quest / Workers','Assets/Weedtopia/Game/Prefabs/Resident.prefab' if kind=='world_character' else '','','Original character production','prototype' if kind=='world_character' else 'missing')
for name in ('start','intro','quest','hud','building_details','workers','worker_card','worker_spin','townhall','journal','premium','offline','settings','navigation','input','dropdown','slider','button','list_row','overlay'):
    row('ui/'+name,'ui',name,'Assets/Weedtopia/Game/Scenes/BurningHills.unity','','Current uGUI / LegacyRuntime.ttf')
for name in ('money','grinder','cards','raw','refined','butter','pastry','residents','quest','farm','city','settlement','industry','save','settings','upgrade','harvest','work','talk','close','confirm','cancel','lock','rarity'):
    row('icon/'+name,'icon','HUD / Panels','','','Original icon system','missing')
for r in content['roles']:
    row('worker/'+r['id'],'worker_card','Workers / Spin','','','Role definition: '+r['id'],'missing')
for name in ('accept','blocked','music','ambience'):
    row('audio/'+name,'audio','GameRoot','Assets/Weedtopia/Game/Audio/development_'+name+'.wav' if name in ('accept','blocked') else '','','Original audio needed','prototype' if name in ('accept','blocked') else 'missing')
with (OUT/'VISUAL_ASSET_REGISTER.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
(OUT/'VISUAL_GAPS.md').write_text('''# Visual gap audit\n\nThe fresh baseline preserves source hashes, editable originals and all 14 prior Game View images. Historical test totals are not new acceptance.\n\n1. BLOCKING: 251 building slots have accepted sources but no approved productive 2D integration. Four existing prototypes await explicit user approval.\n2. HIGH: BurningHills world uses primitive district grounds, route bars, capsule residents and block delivery vehicles. Replace the non-building presentation additively with authored production assets.\n3. HIGH: UI uses LegacyRuntime.ttf, untextured rectangles and Unicode icons. Replace with a licensed font, authored skin, portraits and original icon sprites across all reachable panels.\n4. HIGH: Rocko and seven supporting characters need distinctive art, portraits and motion.\n5. HIGH: Existing development audio is not final. Keep release blocked unless verified replacement audio exists.\n6. REQUIRED: Production preflight must reject the current unapproved building path, missing assets and false approval; current gameplay tests must still pass after changes.\n\nBaseline image paths: `Baseline/01_intro_1920x1080.png`, `Baseline/03_farm_backlog.png`, `Baseline/07_industry_freeplay.png`.\n''',encoding='utf-8')
(OUT/'STATUS.md').write_text('# Production Finish status\n\nFresh audit completed. Visual production in progress. Explicit building approval remains pending; release is blocked. No production wrappers or catalog references changed. New tests and Game View evidence still required.\n',encoding='utf-8')
print(json.dumps({'baselineFiles':len(paths),'registerRows':len(rows),'slots':len(slots),'instances':len(content['instances'])}))
