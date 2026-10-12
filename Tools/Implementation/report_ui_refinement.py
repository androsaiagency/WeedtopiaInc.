"""Publish only fresh, passed Unity evidence for the third requested UI revision."""
import html,json
from datetime import datetime,timezone
from pathlib import Path

root=Path(__file__).resolve().parents[2]
report=root/'Docs/UiRefinementRevision'
validation=root/'Docs/Implementation/Validation'
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def latest(suffix):
    paths=sorted(validation.glob('*'+suffix+'.json'))
    if not paths:raise RuntimeError('Missing '+suffix)
    result=read(paths[-1])
    if result['status']!='passed':raise RuntimeError('Unpassed '+result['run'])
    return result
tests=[latest('_PlayMode_UiRefinementFinalRegression'),latest('_EditMode_UiRefinementDomainFinal'),latest('_EditMode_UiRefinementAssetsFinal')]
capture=read(report/'capture_receipt.json')
preservation=read(report/'preservation.json')
art=read(report/'art_verification.json')
idempotence=read(report/'idempotence.json')
def utc(value):return datetime.fromisoformat(value.replace('Z','+00:00'))
if not capture['complete'] or capture['issues']:raise RuntimeError('Capture incomplete')
if len(set(capture['screenshots']))!=len(capture['screenshots']):raise RuntimeError('Duplicate capture files')
if not {0.75,1.5}.issubset(set(capture.get('uiScales',[]))):raise RuntimeError('Saved UI-scale limits not captured')
if capture.get('closePointerChecks',0)<100 or capture.get('wheelPointerChecks',0)<80:raise RuntimeError('Pointer verification incomplete')
if utc(capture['utc'])<max(utc(t['endedUtc']) for t in tests):raise RuntimeError('Capture predates final tests')
for name,value in [('preservation',preservation),('art',art),('idempotence',idempotence)]:
    if not value['passed']:raise RuntimeError(name+' incomplete')
for name in capture['screenshots']:
    if not (root/name).is_file():raise RuntimeError('Missing '+name)
summary={'utc':datetime.now(timezone.utc).isoformat(),'passed':True,'tests':tests,'gameViewCount':len(capture['screenshots']),'resolutions':capture['resolutions'],'density':capture['displayDensities'],'uiScaleValues':capture['uiScales'],'playerAvatars':10,'male':5,'female':5,'inventoryPagination':False,'minimumVisibleItems':8,'preservedWorldPrefabs':preservation['protectedPrefabCount'],'preservedArtwork':preservation['protectedArtworkCount'],'gameplayContentUnchanged':preservation['contentByteIdentical'],'physicalPhoneTest':False,'apkBuild':False,'userVisualAcceptance':False}
summary['pointerChecks']={'closeButtons':capture['closePointerChecks'],'wheelActions':capture['wheelPointerChecks']}
(report/'validation_summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
cards=[]
for entry in read(report/'AvatarPrompts.json'):
    label=entry['id'].replace('player_male_','Männlich ').replace('player_female_','Weiblich ')
    cards.append(f'<figure class="avatar"><img src="../../{entry["asset"]}" alt="{label}"><figcaption>{label}</figcaption></figure>')
screens=[]
for name in capture['screenshots']:
    relative='Screenshots/'+Path(name).name;label=Path(name).stem
    screens.append(f'<figure class="shot" data-size="{label.split("_")[0]}"><a href="{relative}"><img loading="lazy" src="{relative}" alt="{label}"></a><figcaption>{label}</figcaption></figure>')
page='''<!doctype html><html lang="de"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Weedtopia · UI-Verfeinerung</title><style>
body{margin:0;padding:32px;font:17px/1.5 system-ui;background:#082d2f;color:#eef2d8}h1,h2{color:#d1dfa8}main{max-width:1500px;margin:auto}.avatars{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:16px}figure{margin:0;background:#174f4e;border:1px solid #9dc2a1;border-radius:16px;overflow:hidden}figcaption{padding:12px;overflow-wrap:anywhere}.avatar img{width:100%;display:block;aspect-ratio:1;object-fit:contain}.screens{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}.shot img{width:100%;display:block;max-height:650px;object-fit:contain;background:#062425}button{background:#215f59;color:#fff2cf;border:1px solid #cbd69d;border-radius:24px;padding:12px 20px;margin:4px;cursor:pointer}button.active{background:#669746}a{color:#d6eba3}@media(max-width:700px){body{padding:16px}.avatars{grid-template-columns:repeat(2,1fr)}.screens{grid-template-columns:1fr}}
</style><main><h1>Weedtopia · UI-Verfeinerung</h1><p>Größeres einheitliches Gold-X, korrigierter Zuweisen-Button, zehn eigene Profilavatare, vollständig sichtbares Glücksrad und fortlaufendes Inventar mit mindestens acht sichtbaren Einträgen.</p><p>Unity-Prüfungen: '''+', '.join(f'{t["passed"]}/{t["total"]}' for t in tests)+f'. Echte Game-View-Aufnahmen: {len(capture["screenshots"])}. Physischer Handytest und visuelle Freigabe durch den Nutzer stehen aus.</p><h2>Fünf männliche und fünf weibliche Profilavatare</h2><div class="avatars">'+''.join(cards)+'</div><h2>Aktuelle Game Views</h2><nav><button class="active" data-filter="all">Alle</button>'+''.join(f'<button data-filter="{size}">{size}</button>' for size in capture['resolutions'])+'</nav><div class="screens">'+''.join(screens)+'''</div><script>document.querySelectorAll('[data-filter]').forEach(b=>b.onclick=()=>{document.querySelectorAll('[data-filter]').forEach(x=>x.classList.toggle('active',x===b));document.querySelectorAll('.shot').forEach(s=>s.hidden=b.dataset.filter!=='all'&&s.dataset.size!==b.dataset.filter)});</script></main></html>'''
(report/'ReviewGallery.html').write_text(page,encoding='utf-8')
readme=f'''# Weedtopia: dritte UI-Verfeinerung

Die angefragten Änderungen sind in beiden Stadt-Szenen und den gemeinsamen Prefabs eingebunden.

- Zuweisen: links ausgerichtetes Symbol, mittiger Text, vollständige Breite bei freien Slots.
- Schließen: gemeinsames goldenes X mit zwei logischen Einheiten Randabstand; kein Text-X.
- Profil: zehn eigene transparente Spielerporträts, fünf männlich und fünf weiblich. Bewohnerporträts bleiben im Dialogkatalog. Historische Avatar-IDs werden beim Laden migriert, ohne Name und Spielstand zu ändern.
- Glücksrad: vollständiges Rad und Bedienfelder in einem Fenster; Anordnung nach verfügbarem Seitenverhältnis. Die bestätigungspflichtigen Grinder-Kosten bleiben erhalten.
- Inventar: alle vorhandenen Einträge in einer fortlaufenden Liste. Mindestens acht vollständige Kacheln sind in den geprüften Formaten sichtbar. Keine Seitenschalter. Details sind separat scrollbar und im Hochformat über „Zur Übersicht“ schließbar.

## Nachweise

{chr(10).join('- '+t['run']+': '+str(t['passed'])+'/'+str(t['total'])+' bestanden.' for t in tests)}
- {len(capture['screenshots'])} echte Game Views: {', '.join(capture['resolutions'])}, einschließlich Dichte 2,75.
- Erfasste UI-Skalierung: {', '.join(format(v,'.2f').rstrip('0').rstrip('.').replace('.',',') for v in capture['uiScales'])}; aktuelle Reglergrenzen 0,85/1,15 sowie gespeicherte Grenzwerte 0,75/1,5.
- Pointer-Raycast-Prüfungen: {capture['closePointerChecks']} an Schließen-Buttons und {capture['wheelPointerChecks']} an Rad-Aktionen. Der Gebäudeschließen-Button wird nach dem Schließen seiner darüberliegenden Worker-Auswahl geprüft.
- Expliziter Installer ist bei Wiederholung byte-identisch für {idempotence['files']} Dateien.
- {preservation['protectedPrefabCount']} Welt-Prefabs sowie alle verglichenen Nicht-UI-Objekte in beiden Szenen sind unverändert.
- {preservation['protectedArtworkCount']} bereits vorhandene Grafikdateien sind byte-identisch zur Baseline.
- Die Produktionsdaten sind byte-identisch zur Baseline.
- Alle zehn Avatar-PNGs behalten die originalen Generierungsbytes und Transparenz. Prompts, Quelle und Ziel: AvatarPrompts.json. Modus: integriertes image_gen.

Galerie: [ReviewGallery.html](ReviewGallery.html). Maschineller Prüfbericht: [validation_summary.json](validation_summary.json).

Der explizite, wiederholbare Installer befindet sich unter `Weedtopia > Presentation > Install UI Refinements`. Er verändert keine Szene automatisch bei Import oder Start.

Physische Touch-Bedienung auf einem echten Handy, APK-Build und visuelle Nutzerfreigabe wurden in dieser Revision nicht ausgeführt.
'''
(report/'README.md').write_text(readme,encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k!='tests'},indent=2))
