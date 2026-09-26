"""Publish only file-backed current delivery evidence; never grant human or device acceptance."""
from pathlib import Path
import json,hashlib
from datetime import datetime,timezone
ROOT=Path(__file__).resolve().parents[2]
def read(path):return json.loads((ROOT/path).read_text(encoding='utf-8-sig'))
def dump(path,value):(ROOT/path).write_text(json.dumps(value,indent=2,ensure_ascii=False),encoding='utf-8')
def newest(mode):
    files=sorted((ROOT/'Docs/Implementation/Validation').glob('*_'+mode+'_*.json'))
    item=read(files[-1].relative_to(ROOT));assert item['status']=='passed',item
    return item,files[-1].relative_to(ROOT).as_posix()
edit,editpath=newest('EditMode');play,playpath=newest('PlayMode')
preserve=read('Docs/LayoutRevision/preservation.json');assert not preserve['missing'] and not preserve['protectedChanged'] and not preserve['anchorChanges']
cutouts=read('Docs/LayoutRevision/Cutouts/manifest.json');assert len(cutouts['slots'])==251
assert all(s['technicalPassed'] and s['individualVisualReviewed'] for s in cutouts['slots'])
capture=read('Docs/LayoutRevision/gameview_receipt.json');qa=read('Docs/LayoutRevision/visual_review.json')
assert qa['fingerprint']==capture['fingerprint']
preflight=read('Docs/ProductionFinish/Evidence/production_preflight.json')
assert all(x['code']=='ACCEPTANCE_GATE_OPEN' for x in preflight['issues']),preflight
now=datetime.now(timezone.utc).isoformat();n=len(capture['screenshots'])
profile=read('Docs/ProductionFinish/Evidence/player_run.json')
assert profile['diagnosticCompleted'] and profile['fingerprint']==capture['fingerprint']
assert profile.get('agentRenderedFrameReviewPassed') and len(profile.get('reviewedFrames',[]))==3
pixels=read('Docs/LayoutRevision/Cutouts/pixel_verification.json');assert pixels['passed'] and pixels['count']==251
source=read('Docs/Implementation/Validation/SourceCompile/result.json');assert source['status']=='passed'
for assembly in source['results']:
    for path,digest in assembly['sources'].items():
        assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
for shot in qa['screenshots']:
    assert hashlib.sha256((ROOT/shot['path']).read_bytes()).hexdigest()==shot['sha256'],shot['path']
measurements=json.loads(Path(profile['profile']).read_text(encoding='utf-8-sig'))
assert measurements['completed'] and len(measurements['measurements'])==3
timings='; '.join(f"{x['scenario']}: Median {x['medianMs']:.2f} ms, P95 {x['p95Ms']:.2f} ms, {x['loadedBuildingSprites']} geladene Gebäudesprites" for x in measurements['measurements'])
performance_rows='\n'.join(f"| {x['scenario']} | {x['medianMs']:.2f} / {x['p95Ms']:.2f} / {x['maxMs']:.2f} ms | {x['maxDrawCalls']} / {x['maxBatches']} | {x['maxGcBytes']:,} B / {x['gcCollections']} | {x['textureRuntimeBytes']/1048576:.1f} MiB | {x['loadedBuildingSprites']} |" for x in measurements['measurements'])
assert qa['reviewedScreenshotCount']==n and qa['agentVisualReviewPassed']
gates=', '.join(x['objectPath'] for x in preflight['issues'])
report=f'''# Burning Hills — genehmigte Bezirks- und Gebäudeintegration

Stand: {now}. **Implementiert und technisch geprüft; Gesamtspiel nicht final abgenommen.**

## Ergebnis

Die Szene folgt der Benutzerskizze: Industrie links, Siedlung oberhalb der Stadt, Farm deutlich rechts außerhalb des bebauten Bereichs. Eine Landstraße mit Flussquerung verbindet Farm-Lager und Stadt-Lager. Die Fahrt umfasst 196,083 Welteinheiten und 15 Simulationssekunden pro Lieferung gemäß bestehendem Routenmodell. Das Stadt-Lager beliefert sämtliche Coffee Shops und die Butterfabrik mit Fahrern. Förderbänder verbinden Anbau, Verarbeitung und Farm-Lager sowie Butterfabrik und Bäckereien.

| Bezirk | Erweiterung | Belegte bzw. freischaltbare Standorte |
| --- | --- | --- |
| Farm | nach rechts | 8 Anbauplätze, Verarbeitung, Farm-Lager |
| Stadt | nach unten | 12 Coffee Shops, Rathaus, Stadt-Lager |
| Siedlung | nach oben | 18 Plätze für Wohnhäuser oder Parks |
| Industrie | nach links | 12 Bäckereien, Butterfabrik, Supermarkt |

56 Standorte und 35 Routen sind als Szenenobjekte und Daten angelegt. Bezirksnavigation: Schieberegler/Pfeile, Mausrad entlang der Bezirksrichtung, Ziehen mit rechter oder mittlerer Maustaste; Strg+Mausrad zoomt. Die Übersicht zeigt alle Bezirke. Die vorhandene Touch-Bedienung bleibt erhalten; physischer Touch wurde nicht abgenommen.

Die ausdrücklich freigegebenen vier Prototypen sind unverändert als Bilder übernommen. Für die restlichen 247 Slots wurde die ausdrücklich gewählte lokale Freistellung verwendet. Originale RGB-Pixel bleiben identisch; ausschließlich Alpha und bei sieben randberührenden Motiven transparenter Außenabstand wurden ergänzt. Alle 251 Wrapper verwenden ihren eigenen passenden Sprite, darunter jede der 25 Ausbaustufen der zehn Familien und das feste Rathaus. Ruinen gehören zum vorgesehenen Wiederaufbau, sie sind keine Entwicklungsvisuals. Der aktive Spielpfad nutzt den Produktionskatalog und keinen Development-Fallback.

Katalog, GUIDs und alle sieben bestehenden Anker je Wrapper bleiben erhalten. Die ursprünglichen 16 Standort-IDs bleiben erhalten. Die Migration ergänzt nur die 40 neuen Plätze; vorhandene Gebäude, Worker, Geld und laufende Fracht bleiben bestehen. Die vorherige Umgebung liegt inaktiv unter `Archive_PreviousPresentationLayout`. Straßen, Grundstücke, Bäume und Props sind im Editor authoriert und in der Hierarchie sichtbar.

## Aktuelle Nachweise

| Prüfung | Ergebnis | Evidenz |
| --- | --- | --- |
| Unity EditMode | {edit['passed']}/{edit['total']}, 0 fehlgeschlagen | [{edit['run']}](../Implementation/Validation/{Path(editpath).name}) |
| Unity PlayMode | {play['passed']}/{play['total']}, 0 fehlgeschlagen | [{play['run']}](../Implementation/Validation/{Path(playpath).name}) |
| Source-Compiler | {len(source['results'])} Assemblies erfolgreich; alle Quellenprüfsummen weiterhin identisch | [SourceCompile/result.json](../Implementation/Validation/SourceCompile/result.json) |
| Bestanderhaltung | {preserve['baselineFiles']} Baseline-Dateien; keine fehlt; geschützte Quellen unverändert; 251 Ankervergleiche gleich | [preservation.json](preservation.json) |
| Sprite-Aufbereitung | 251 Einzelzuordnungen, 31 ursprüngliche Kontaktbögen und drei Korrekturbögen angesehen; vier Prototypen bytegleich und 247 Quellbilder RGB-identisch | [Cutouts/manifest.json](Cutouts/manifest.json), [Pixelvergleich](Cutouts/pixel_verification.json) |
| Tatsächliche Game View | {n} aktuelle Screenshots einschließlich 51 Gebäudezuständen und drei Auflösungen | [gameview_receipt.json](gameview_receipt.json), [visual_review.json](visual_review.json) |
| UI | aktuelle Prüfung in 1920×1080, 1366×768 mit 115% UI-Skalierung und 2560×1080 | [ui_overflow.json](../ProductionFinish/Evidence/ui_overflow.json) |
| Windows-Diagnosebuild | erfolgreich gebaut und ausgeführt; drei gerenderte Szenarien à 300 Messframes | [development_build.json](../ProductionFinish/Evidence/development_build.json), [player_run.json](../ProductionFinish/Evidence/player_run.json) |
| Produktions-Preflight | keine Asset-/Platzhalterfehler; unabhängige Abnahme-Gates offen | [production_preflight.json](../ProductionFinish/Evidence/production_preflight.json) |

Der erste EditMode-Lauf meldete einen Fehler im erneuten Presentation-Installer-Aufruf: Ein inaktiver Installationsstempel wurde nicht erkannt. Der Installer prüft diesen nun einschließlich inaktiver Objekte vor jeglichem Import. Die dabei unbeabsichtigt geänderten Dateien BuildingSite.prefab und DeliveryTruck.prefab wurden aus der SHA-verifizierten Ausgangssicherung wiederhergestellt. Im ersten PlayMode-Lauf war ein Stadtbewohner vom Seitenpanel verdeckt; die Platzierung am inneren Gehweg korrigiert das. Weitere Diagnosen belegten native Physik-Ray-Misses trotz rechnerisch getroffener Box. Die Auswahl prüft jetzt die vorhandenen authorierten Boxen direkt unter Berücksichtigung von Rotation und Skalierung. Der neue Test klickt außerdem Gebäude aller vier Erweiterungsbereiche vor und nach dem größten Spritewechsel. Die vollständigen Wiederholungsläufe oben prüfen die Korrekturen; die fehlgeschlagenen Protokolle bleiben erhalten.

Die Game-View-Iteration korrigierte zu kleine frühe Gebäudestufen, harte Grundstückskanten, Dreiecksverengungen an Straßenbiegungen, transparente Bodenflächen vor nördlichen Gebäuden und den zu unauffälligen Navigationsgriff. Die aktuelle Galerie zeigt die korrigierte Version. Die Aufnahmen laufen nach Kameraruhe; Kunst-Nahansichten verwenden explizite Diagnosestände, die den Spielstand des Benutzers nicht verändern.

Eine zusätzliche Prüfung neutralweißer Flächen verglich 67 Kandidaten mit den Originalmotiven. 28 bestätigte Hintergrundöffnungen in 23 Bildern wurden ausschließlich im Alpha-Kanal korrigiert, etwa zwischen Rohrbögen und unter offenen Solardächern. 39 tatsächlich gemalte Wandflächen oder Lichtreflexe blieben erhalten. Alle 23 Korrekturen wurden auf drei Kontaktbögen geprüft; repräsentative Ausbaustufen dieser Familien sind außerdem in der neu aufgenommenen Game View enthalten. [Einzelentscheidungen und Vorher/Nachher-Prüfsummen](Cutouts/WhiteGapReview/decisions.json).

Der Zwischenlauf `20260923_013503_EditMode_FinalDistrictAssets` wurde als ausschließlich eigener Testprozess kontrolliert beendet: NUnit verglich rund 630 MB als einzelne Byte-Collection-Elemente. Der Test berechnet jetzt für jede vollständig gelesene Datei SHA-256 vor und nach dem Installer-Aufruf. Der unterbrochene Lauf gilt ausdrücklich nicht als bestanden; nur die oben verlinkte vollständige Wiederholung zählt.

Der lokale Windows-Diagnoselauf verwendet {measurements['device']} mit {measurements['graphics']} bei 1920×1080 und expliziten URP-Renderanfragen: {timings}. Die Werte gelten für diesen Development-Build auf diesem Rechner. Sie sind keine Leistungszusage für andere Geräte; Texture-Runtime-Speicher ist keine VRAM-Messung. Die drei PNGs neben dem Profil belegen tatsächlich gerenderten Inhalt.

| Szenario, je 300 Messframes | Median / P95 / Maximum | Max. Draw Calls / Batches | Max. GC-Allokation pro Frame / GC-Sammlungen | Texture-Runtime-Speicher | Geladene Gebäudesprites |
| --- | --- | --- | --- | --- | --- |
{performance_rows}

Der Prüflauf lief ohne sichtbare Fensterpräsentation im Batchmodus mit aktivierter Direct3D11-Grafik und expliziten URP-Renderanfragen. Die Framezeiten sind deshalb keine Messung der Bildausgabe oder Eingabelatenz eines normal bedienten Spiels und keine Zielgeräte-FPS-Freigabe. Insbesondere die Draw-Call-Zahl der Gesamtübersicht und die Spitze beim Tierwechsel müssen gegen das noch festzulegende Zielgerät bewertet werden. Der erste verborgene Fensterlauf pausierte bei `runInBackground=0`; ausschließlich dieser eigene Diagnoseprozess wurde beendet und nicht als bestanden gewertet. [Unterbrechungsbeleg](player_interruption.json). Die Spieler-Projekteinstellung wurde dafür nicht geändert. [Unity-Dokumentation zum Player-Batchmodus](https://docs.unity3d.com/6000.0/Documentation/Manual/PlayerCommandLineArguments.html).

## Offen für endgültige Abnahme

Die Gebäude-Prototypfreigabe und die Erlaubnis zur lokalen Freistellung liegen vor; dazu ist keine erneute Entscheidung nötig. Offen sind die unabhängigen Gates: `{gates}`. Automatische Hauptpfad-, Screenshot- und Leistungstests ersetzen weder den menschlichen Spieldurchlauf noch Audio-Abhören, Lizenzfreigabe oder eine vereinbarte Zielgeräteabnahme. `productionReady` bleibt deshalb false. Sämtliche hier genannten Game-View-Prüfungen sind Agentenprüfungen, keine neue Nutzerfreigabe.

Die verbleibende Zielplattformentscheidung und die Prüfung des Spielgefühls betreffen das endgültige Release, nicht die bereits genehmigte Serienintegration.

[Konkrete Schritte für die vier offenen Abnahmen](REMAINING_ACCEPTANCE.md). [Vergleichsgalerie und alle aktuellen Spielansichten](comparison.html).
'''
(ROOT/'Docs/LayoutRevision/REPORT.md').write_text(report,encoding='utf-8')
status=f'''# Burning Hills — Production Finish

Stand {now}. **Gebäude- und Bezirksumbau integriert; kein abschließend abgenommenes Release.**

Die vier vorhandenen Gebäudeprototypen sind vom Benutzer ausdrücklich freigegeben. Die darauf beruhende Umstellung aller 251 Wrapper ist abgeschlossen. Die lokale pixelbewahrende Freistellung der übrigen 247 Bilder wurde separat ausdrücklich gewählt. Alle Originalquellen und manuell gepflegten Wrapper-Anker sind erhalten. Die aktuelle Szene folgt der Skizze mit vier gerichteten Erweiterungen und einer entfernten Farm.

| Bereich | Aktueller Stand |
| --- | --- |
| Layout/Logistik | 56 Standorte, 35 Routen, Farm rechts, Stadt-Lager beliefert Shops und Butterfabrik |
| Gebäude | 251 produktive Sprites; keine aktive Development-Bindung |
| Speicherung | Migration von 16 auf 56 Standorte, bestehender Fortschritt und laufende Fracht erhalten |
| EditMode / PlayMode | {edit['passed']}/{edit['total']} und {play['passed']}/{play['total']} bestanden; aktuelle XML-Dateien verlinkt |
| Game View | {n} aktuelle Aufnahmen; 51 repräsentative Gebäudezustände; drei Auflösungen |
| Gesamtfreigabe | Offen: {gates} |

[Vollständiger Bericht mit Bedienung und Evidenz](../LayoutRevision/REPORT.md). [Assetregister](VISUAL_ASSET_REGISTER.csv). [Aktuelle Screenshots](../LayoutRevision/gameview_receipt.json). [Quellen und Lizenzen](PROVENANCE_AND_LICENSES.md).

Die bisherigen Screenshots unter `Docs/ProductionFinish/Screenshots` und Testzahlen vom 22. September gehören zum Stand vor diesem Layoutumbau. Maßgeblich sind die oben verlinkten neuen Nachweise. Die vier Gebäudeansichten brauchen keine erneute Freigabe; die Gesamtspielabnahme bleibt separat.
'''
(ROOT/'Docs/ProductionFinish/STATUS.md').write_text(status,encoding='utf-8')
(ROOT/'Docs/ProductionFinish/VALIDATION_REPORT.md').write_text('# Aktueller Prüfbericht\n\n'+f'Stand {now}. EditMode {edit["passed"]}/{edit["total"]}; PlayMode {play["passed"]}/{play["total"]}.\n\n'+ '[Der vollständige aktuelle Bericht](../LayoutRevision/REPORT.md) trennt Test-, Game-View-, Nutzer- und Zielgeräte-Gates. Frühere Nachweise sind historisch; nicht auf diesen Szenenstand übertragen.\n',encoding='utf-8')
(ROOT/'Docs/ProductionFinish/VISUAL_GAPS.md').write_text('# Aktuelle Abnahmelücken\n\n'+f'Stand {now}. Die 251er-Gebäudelücke ist geschlossen. Die Prototypfreigabe liegt vor. Aktuell meldet der Preflight ausschließlich diese unabhängigen Gates: {gates}.\n\n'+ '[Prüfbericht](../LayoutRevision/REPORT.md), [visuelle Einzelprüfung](../LayoutRevision/visual_review.json). Es wurden keine fehlenden Gebäude oder sichtbaren Platzhalter als akzeptiert umetikettiert.\n',encoding='utf-8')
progress=read('Docs/ArtProduction/PROGRESS.json')
progress.update(updated_at_utc=now,status='approved_251_sprites_integrated_district_layout_tested_release_gates_open',blockers=[x['objectPath'] for x in preflight['issues']],production_wrappers_converted=251)
progress['two_d_prototype'].update(user_approval=True,production_wrapper_changed=True,catalog_changed=False,approval_evidence='Docs/LayoutRevision/building_approval.json')
progress['manual_gates'].update(unity_editmode=f'passed_{edit["passed"]}_of_{edit["total"]}_see_{editpath}',unity_playmode=f'passed_{play["passed"]}_of_{play["total"]}_see_{playpath}',town_hall_unity_visual_qa='prototype_explicitly_approved_and_production_Game_View_agent_reviewed',visual_agent_review='all_251_cutouts_and_51_production_building_samples_reviewed_see_Docs/LayoutRevision/visual_review.json')
dump('Docs/ArtProduction/PROGRESS.json',progress)
for name in ['Docs/Implementation/IMPLEMENTATION_STATUS.md','Docs/Implementation/VALIDATION_REPORT.md','Docs/ProductionFinish/BUILDING_VISUAL_APPROVAL.md']:
    p=ROOT/name;old=p.read_text(encoding='utf-8-sig');marker='<!-- LAYOUT_REVISION_CURRENT -->'
    if marker not in old:p.write_text(marker+'\n# Aktueller Stand nach Benutzerfreigabe\n\nDie vier Gebäudeprototypen und die lokale Freistellung sind ausdrücklich freigegeben; 251 produktive Sprites und die Skizzen-Anordnung wurden integriert. Maßgeblich ist [Docs/LayoutRevision/REPORT.md](../LayoutRevision/REPORT.md).\n\nDie folgenden Abschnitte dokumentieren den historischen Stand vor dieser Integration. Ihre damaligen offenen Gebäude-Gates und Testzahlen gelten nicht als aktueller Status.\n\n---\n\n'+old,encoding='utf-8')
dump('Docs/LayoutRevision/delivery.json',{'utc':now,'implemented':True,'buildings':251,'sites':56,'routes':35,'editmode':editpath,'playmode':playpath,'gameviewFingerprint':capture['fingerprint'],'releaseAccepted':False,'openGates':[x['objectPath'] for x in preflight['issues']]})
print('Current reports written; release acceptance remains false.')
