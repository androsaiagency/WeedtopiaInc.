import json
from datetime import datetime,timezone
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parent
PROJECT=next(p for p in ROOT.parents if (p/'ProjectSettings').exists())
OUT=ROOT/'CityLighting'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,data):p.write_text(json.dumps(data,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
receipt=read(OUT/'material_receipt.json')
capture=read(OUT/'Final/gameview_receipt.json')
preservation=read(OUT/'preservation.json')
assert preservation['passed'] and receipt['materials']==3 and len(capture['screenshots'])==6
# Verify the persisted asset reproduces the chosen preview, rather than merely checking a process exit.
images=[Image.open(p).convert('RGB') for p in [OUT/'Compare/Screenshots/fill_00_close.png',OUT/'Compare/Screenshots/fill_65_close.png',OUT/'Final/Screenshots/02_town_hall_close.png']]
pixels=[im.load() for im in images]
errors_before=[];errors_selected=[]
for y in range(205,690):
    for x in range(730,1210):
        before,selected,actual=[p[x,y] for p in pixels]
        if max(abs(a-b) for a,b in zip(before,selected))>20:
            errors_before.append(sum(abs(a-b) for a,b in zip(actual,before))/3)
            errors_selected.append(sum(abs(a-b) for a,b in zip(actual,selected))/3)
visual_check={'changed_pixels_checked':len(errors_before),'mean_error_vs_dark':sum(errors_before)/len(errors_before),'mean_error_vs_selected':sum(errors_selected)/len(errors_selected)}
visual_check['passed']=visual_check['mean_error_vs_selected']<visual_check['mean_error_vs_dark']*.15
assert visual_check['passed'],visual_check
write(OUT/'render_comparison_check.json',visual_check)
utc=datetime.now(timezone.utc).isoformat()
summary={'utc':utc,'status':'brighter_city_materials_applied_gameview_verified_pending_user_visual_review',
         'fill':receipt['fill'],'lods':[50000,20000,12000],
         'comparison_fill_strengths':[0,.4,.65,.9],'selected_fill':.65,
         'new_texture_files':0,'new_scene_lights':0,'scene_changed':False,
         'material_changes':'Three dedicated city materials share the original approved 2K maps; base-colour texture also supplies local fill through URP Lit emission. BakedEmissive flag required by URP 17 import; renderers do not contribute baked GI and no light bake was performed.',
         'verification':'Fresh Unity import, authoring validation of all 2K bindings, six final PlayMode Game View captures, preservation hashes. No new gameplay tests needed for the material-only visual change.',
         'preservation':preservation,'render_comparison_check':visual_check,'visual_user_approval':False,'external_credits_used':0}
write(OUT/'summary.json',summary)
for path in [PROJECT/'Docs/ArtProduction/PROGRESS.json',ROOT/'Optimization_v1/PROGRESS.json']:
    data=read(path)
    target=data['town_hall_highpoly_experiment_2026_09_28'] if 'town_hall_highpoly_experiment_2026_09_28' in data else data
    target['city_lighting']={'status':summary['status'],'report':(OUT/'README.de.md').relative_to(PROJECT).as_posix(),'fill':.65,'date':utc}
    if path.name=='PROGRESS.json' and 'updated_at_utc' in data:data['updated_at_utc']=utc
    else:data['updated_utc']=utc
    write(path,data)
path=ROOT/'CityIntegration/summary.json';data=read(path)
data['current_lighting']='../CityLighting/README.de.md'
data['notes']=[n for n in data['notes'] if not n.startswith('Actual city lighting')]
data['notes'].append('2026-09-29: darker city rendering corrected with local texture-coloured fill; new visual review pending.')
write(path,data)
notice='> Aktualisierung 2026-09-29: Die zu dunkle Rathausdarstellung wurde aufgehellt. [Aktuelle Aufnahmen und Materialänderung](../CityLighting/README.de.md). Die folgenden Aufnahmen dokumentieren die erste Integration.\n\n'
path=ROOT/'CityIntegration/README.de.md';text=path.read_text(encoding='utf-8')
if not text.startswith('> Aktualisierung 2026-09-29:'):path.write_text(notice+text,encoding='utf-8')
(OUT/'README.de.md').write_text(f'''# Rathaus: Helligkeit an die gemalte Stadt angepasst

Stand: {utc}

Die mittlere Grundaufhellung (0,65) ist im produktiven Rathaus-Prefab für alle drei LODs gespeichert. Die räumliche Beleuchtung, Normalen und Materialdetails bleiben erhalten; die Farbtextur liefert zusätzlich eine gleichmäßige Grundhelligkeit. Die neue Darstellung ist lokal umgesetzt und im tatsächlichen Spielmodus aufgenommen. Die persönliche Sichtfreigabe steht aus.

## Ansehen

- [Vorher – Nahansicht](Compare/Screenshots/fill_00_close.png)
- [Jetzt – Nahansicht](Final/Screenshots/02_town_hall_close.png)
- [Jetzt – zwischen den Nachbargebäuden](Final/Screenshots/01_city_context.png)
- [Jetzt – ausgewählt](Final/Screenshots/03_selected.png)
- [LOD1](Final/Screenshots/04_lod1.png), [LOD2](Final/Screenshots/05_lod2.png)

## Änderung

Drei eigene Materialien unter `Assets/Weedtopia/Art/Buildings/Models/town_hall/T_FIXED/Approved20260928/Materials` verwenden die vorhandenen 2K-Karten. Die URP-Lit-Emission nutzt dieselbe Farbtextur als Grundaufhellung. URP 17 benötigt dafür das Material-Flag `BakedEmissive`; die Renderer sind jedoch nicht als `ContributeGI` markiert und es wurde kein Licht-Bake ausgeführt. Es wurden keine zusätzlichen Lichtobjekte, neuen Texturbilder oder neuen Shader eingeführt. Die ursprünglichen Prüfstandmaterialien bleiben unverändert.

Vier Stärken wurden im Spiel verglichen: 0,00 / 0,40 / 0,65 / 0,90. 0,65 hellt die Cremefassade und das Grün deutlich auf; 0,90 reduziert den Kontrast stärker als nötig. Die Einstellung lässt sich explizit über `Weedtopia > Art Pipeline > 3D Town Hall > Apply City Brightness` wiederherstellen.

## Verifikation

- Unity importiert und speichert den Modell-Prefab erfolgreich; alle neun ursprünglichen 2K-Texturbindungen geprüft.
- Acht Vergleichsaufnahmen und sechs abschließende Game-View-Aufnahmen nach erneutem Laden des gespeicherten Prefabs.
- {preservation['checked']} Bestands-Assets und Einstellungen per Hash geprüft. Einzige Änderung an diesen Bestandsdateien: die drei Materialzuweisungen im Rathaus-Modell-Prefab. Drei Stadtmaterialien kommen hinzu.
- Stadt-Szene, Wrapper, Katalog, Geometrie, Texturbilder, Colliders und andere Gebäude unverändert. Die Gameplay-Prüfungen vom vorherigen Integrationsschritt wurden für diese reine Materialänderung nicht erneut ausgeführt.
- Maschinenlesbare Belege: `material_receipt.json`, `Final/gameview_receipt.json`, `preservation.json`, `summary.json`.

Keine Meshy-Nutzung und keine externen Credits.
''',encoding='utf-8')
print(json.dumps(summary,indent=2,ensure_ascii=False))
