"""Record verified receipts without turning technical checks into visual/player approval."""
import json
from datetime import datetime, timezone
from pathlib import Path

TASK=Path(__file__).resolve().parent
PROJECT=next(p for p in TASK.parents if (p/'ProjectSettings').exists())
OUT=TASK/'CityIntegration'
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,data): p.write_text(json.dumps(data,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
def latest(pattern):
    path=sorted((PROJECT/'Docs/Implementation/Validation').glob(pattern))[-1]
    return path,read(path)
edit_path,edit=latest('*_EditMode_TownHall3DFinal.json')
play_path,play=latest('*_PlayMode_TownHall3DInteractions.json')
preservation=read(OUT/'preservation_after.json')
captures=read(OUT/'gameview_receipt.json')
assert edit['status']=='passed' and play['status']=='passed' and preservation['passed']
utc=datetime.now(timezone.utc).isoformat()
summary={
    'utc':utc,'status':'integrated_technical_checks_passed_city_visual_review_pending',
    'approved_input':'User approved 2K maps and 50000/20000/12000 LOD review',
    'editmode':{'passed':edit['passed'],'total':edit['total'],'receipt':edit_path.relative_to(PROJECT).as_posix()},
    'playmode':{'passed':play['passed'],'total':play['total'],'receipt':play_path.relative_to(PROJECT).as_posix()},
    'preservation':preservation,'gameview_count':len(captures['screenshots']),
    'production_scene_changed':False,'catalog_references_changed':False,
    'active_building_slots':{'sprites':250,'approved_3d_lod':1},
    'external_credits_used':0,'city_visual_user_approval':False,'player_target_performance':'not measured',
    'notes':['Actual city lighting is darker than the isolated review.','100-instance synchronous render benchmark is not gameplay FPS.','Old source model files remain intact; retired FBX dependency removed from active wrapper.']}
write(OUT/'summary.json',summary)
progress_path=TASK/'Optimization_v1/PROGRESS.json'
progress=read(progress_path)
progress.update(status=summary['status'],updated_utc=utc,city_integration='../CityIntegration/README.de.md',resume='Review CityIntegration/Screenshots in the actual city. Do not regenerate approved source or repeat completed baking. Next independent gate: player performance on a defined target device; visual city composition awaits user review.')
progress['steps'].update(user_review='2K_and_LODs_50000_20000_12000_explicitly_approved',rebake_50000='UVv2_approved',city_integration='complete',city_gameview='captured_agent_reviewed_user_review_pending',interaction_tests=f"passed_{play['passed']}_of_{play['total']}",asset_tests=f"passed_{edit['passed']}_of_{edit['total']}")
write(progress_path,progress)
path=PROJECT/'Docs/ArtProduction/PROGRESS.json';global_progress=read(path)
global_progress['updated_at_utc']=utc
global_progress['status']='250_sprite_slots_and_approved_town_hall_3d_integrated_release_gates_open'
global_progress['active_delivery_strategy']='isometric_2d_2_5d_city_with_user_approved_town_hall_3d_LOD_exception'
global_progress['active_production_visuals']={'sprite_slots':250,'mesh_lod_slots':1}
entry=global_progress['town_hall_highpoly_experiment_2026_09_28']
entry.update(scope='explicitly_approved_single_town_hall_3d_integration; other_250_slots_unchanged',status=summary['status'],production_assets_changed=True,city_integration=(OUT/'README.de.md').relative_to(PROJECT).as_posix())
write(path,global_progress)
path=TASK/'UnityReview/summary.json';review=read(path);review['new_lods_user_approval']='approved';review['city_integration']='../CityIntegration/README.de.md';write(path,review)
path=TASK/'UnityReview/README.de.md';text=path.read_text(encoding='utf-8')
notice='> Fortschreibung 2026-09-28: 2K-Texturen und alle drei LODs wurden vom Nutzer freigegeben. Das Rathaus ist inzwischen gezielt in die Stadt integriert. Aktueller Stand: [Stadtintegration](../CityIntegration/README.de.md). Die Angaben unten dokumentieren den vorherigen isolierten Prüfstand.\n\n'
if not text.startswith('> Fortschreibung'):path.write_text(notice+text,encoding='utf-8')
lines=[
'# Freigegebenes 3D-Rathaus in Burning Hills','',f'Stand: {utc}','',
'Das Rathaus verwendet im bestehenden produktiven Gebäude-Slot jetzt das freigegebene 3D-Modell. Alle anderen 250 Gebäude-Slots behalten ihre bisherige Darstellung. Der Katalog-GUID, die Stadt-Szene, Standort, Spielanker und Gebäude-ID sind unverändert.','',
'## Ansehen','',
'Unity: `Assets/Weedtopia/Game/Scenes/BurningHills.unity` öffnen, Play starten, Stadtviertel über das Rathaus-Symbol anwählen und das Rathaus anklicken. Es wird wie die übrigen Gebäude über den bestehenden Katalog dargestellt.','',
'- [Stadtumgebung](Screenshots/01_city_context.png)','- [Rathaus nah](Screenshots/02_town_hall_close.png)','- [Auswahl mit Informationsfenster](Screenshots/03_selected.png)','- [Statistiken](Screenshots/04_town_hall_statistics.png)','- [Rocko-Dialog](Screenshots/05_rocko_dialog.png)','',
'## Technischer Stand','',
'- LOD0: 50.000, LOD1: 20.000, LOD2: 12.000 Dreiecke. Je ein Material und drei 2K-Karten. Einfacher Box-Collider für Sockel und Turm, keine teure Mesh-Kollision.',
'- Produktiver Wrapper: `Assets/Weedtopia/Art/Buildings/Prefabs/town_hall/WT_town_hall_T_FIXED.prefab`.',
'- Wiederverwendbarer Modell-Prefab: `Assets/Weedtopia/Art/Buildings/Models/town_hall/T_FIXED/Approved20260928/WT_TownHall_LOD.prefab`.',
'- Der Modell-Prefab nutzt dieselben freigegebenen FBX- und Texturdateien wie der Prüfstand. Dadurch bleiben deren GUIDs erhalten; es gibt keine duplizierten Binärdateien.',
'- Die Detailkamera berücksichtigt jetzt sowohl Sprites als auch stabile Grenzen der 3D-Detailstufen. Grenzen werden aus der aktuellen visuellen Instanz ermittelt.',
'- Ursprünglicher Wrapper unter `Backup/`; Sprite im produktiven Wrapper deaktiviert erhalten. Der alte Meshy-FBX-Verweis wurde aus dem produktiven Wrapper entfernt, damit seine Texturen nicht mitgeladen werden. Originaldateien bleiben bestehen.',
'- Explizite Installation: `Weedtopia > Art Pipeline > 3D Town Hall > Install Approved Model In City`. Erneute Sprite-Reparatur respektiert diesen Modell-Slot.','',
'## Nachweise','',
f"- EditMode: **{edit['passed']}/{edit['total']} bestanden** – vollständige LODs/2K-Karten, ausgeschlossene alte Abhängigkeiten, wiederholbare Installation, Szenenreferenzen und Produktionsprüfung.",
f"- PlayMode: **{play['passed']}/{play['total']} bestanden** – bestehende Gebäudeinteraktionen sowie virtuelle Maus-/Touchauswahl des 3D-Rathauses, Kameraausschnitt, Rückkehr, Statistik-/Dialogfenster und stabile Grenzen über alle LODs.",
f"- {preservation['files_checked']} Bestandsdateien per Hash geprüft; nur die sieben geplanten Bestandsdateien geändert. Szene, Katalog, weitere Wrapper, Spielinhalt und Einstellungen unverändert.",
f"- {len(captures['screenshots'])} echte Game-View-Aufnahmen bei laufendem Spiel, separater Diagnose-Spielstand. Keine Nutzerspielstände geändert.",
'- Maschinenlesbare Belege: `integration.json`, `summary.json`, `gameview_receipt.json`, `preservation_after.json`.','',
'## Verbleibende Bewertung','',
'Die Freigabe der 2K-Texturen und LODs ist dokumentiert. Die neue Stadtkomposition und Beleuchtung sind als eigener sichtbarer Zwischenstand vorbereitet; ihre Nutzerfreigabe wird nicht aus den Tests abgeleitet. In der vorhandenen Stadtbeleuchtung erscheint das 3D-Modell dunkler als die gemalten Nachbargebäude.','',
'Player-Bildrate, Zielgerät, reale Touchbedienung und Gesamtspiel-Release sind damit nicht abgenommen. Die drei getrennten Textursätze haben rechnerisch zusammen rund 48 MiB BC5/BC7-Daten inklusive Mipmaps; ein größerer Bestand unterschiedlicher 3D-Gebäude braucht weiterhin ein Gesamtbudget. Meshy wurde nicht bedient, keine externen Credits verbraucht.','']
(OUT/'README.de.md').write_text('\n'.join(lines),encoding='utf-8')
print(json.dumps(summary,indent=2,ensure_ascii=False))
