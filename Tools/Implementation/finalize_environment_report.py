"""Link verified evidence for the natural environment; never infer user or device approval."""
from pathlib import Path
from datetime import datetime,timezone
import hashlib,json
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Docs/EnvironmentRevision'
def read(path):return json.loads((ROOT/path).read_text(encoding='utf-8-sig'))
def write(path,data):(ROOT/path).write_text(json.dumps(data,indent=2,ensure_ascii=False),encoding='utf-8')
def latest(mode):
 p=sorted((ROOT/'Docs/Implementation/Validation').glob('*_'+mode+'_*.json'))[-1]
 r=read(p);assert r['status']=='passed' and r['startedUtc']>integration['utc'],r
 assert Path(r['xml']).is_file()
 return r,p
integration=read('Docs/EnvironmentRevision/integration.json')
capture=read('Docs/EnvironmentRevision/gameview_receipt.json');qa=read('Docs/EnvironmentRevision/visual_review.json')
assert capture['fingerprint']==qa['fingerprint'] and qa['agentVisualReviewPassed']
assert len(capture['screenshots'])==qa['reviewedScreenshotCount']==79
for shot in qa['screenshots']:assert hashlib.sha256((ROOT/shot['path']).read_bytes()).hexdigest()==shot['sha256']
preserve=read('Docs/EnvironmentRevision/preservation.json')
assert preserve['allOriginalArtAndBuildingWrappersUnchanged'] and not preserve['protectedSceneChanges'] and not preserve['missingOriginalSceneObjects']
source=read('Docs/Implementation/Validation/SourceCompile/result.json');assert source['status']=='passed'
for assembly in source['results']:
 for path,digest in assembly['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
for art in read('Docs/EnvironmentRevision/art_provenance.json')['sources']:assert hashlib.sha256((ROOT/art['path']).read_bytes()).hexdigest()==art['sha256']
edit,ep=latest('EditMode');play,pp=latest('PlayMode')
preflight=read('Docs/ProductionFinish/Evidence/production_preflight.json')
assert preflight['utc']>integration['utc'] and all(x['code']=='ACCEPTANCE_GATE_OPEN' for x in preflight['issues'])
build=read('Docs/ProductionFinish/Evidence/development_build.json');player=read('Docs/ProductionFinish/Evidence/player_run.json');ui=read('Docs/ProductionFinish/Evidence/ui_overflow.json')
assert build['passed'] and player['diagnosticCompleted'] and player['agentRenderedFrameReviewPassed'] and ui['passed']
assert build['fingerprint']==player['fingerprint']==ui['fingerprint']==capture['fingerprint']
for frame in player['reviewedFrames']:assert hashlib.sha256(Path(frame['path']).read_bytes()).hexdigest()==frame['sha256']
measure=read(player['profile']);assert measure['completed'] and len(measure['measurements'])==3
register=read('Docs/EnvironmentRevision/register_receipt.json');assert register['gameviewFingerprint']==capture['fingerprint'] and register['unrelatedRowsPreserved']
gates=', '.join(i['objectPath'] for i in preflight['issues']);now=datetime.now(timezone.utc).isoformat()
rows='\n'.join(f"| {m['scenario']} | {m['medianMs']:.2f} / {m['p95Ms']:.2f} / {m['maxMs']:.2f} ms | {m['maxDrawCalls']} | {m['textureRuntimeBytes']/1048576:.1f} MiB |" for m in measure['measurements'])
report=f'''# Burning Hills: natürliche Umgebung

Stand: {now}. Die Umgebungsüberarbeitung ist integriert und erneut geprüft. Die endgültige Gesamtspielabnahme bleibt offen.

## Sichtbares Ergebnis

Die bisher dünn verteilten kleinen Bäume und harten Bodenflächen wurden durch eine zusammenhängende Gartenlandschaft ersetzt: Eichen, Birken, Ahorn und Weiden in lockeren Gruppen, blühende Sträucher und Vorgärten, schattige Wiesenbereiche, moosiges Natursteinpflaster und weich auslaufende Erdwege. Vier kleine Gartenhöfe enthalten Brunnen oder Pergolen, Sitzplätze und Pflanzungen. Ein bewachsener Teich, Schilf und Felsen ergänzen die geschwungenen Flussufer. Die Verbindung zur entfernten Farm führt über eine Steinbrücke. Blätter und Wasser bewegen sich dezent über Shader.

Die Umsetzung orientiert sich an der üppigen, gemalten Gartengestaltung der Nutzerreferenz. Die akzeptierte Gebäudearchitektur und Bezirksanordnung bleiben erhalten. Die ersten Game-View-Iterationen wurden nachgebessert: weniger greller Rasen, ruhigere Wasserfarben ohne sichtbare Kachelbrüche, unregelmäßigere Uferbäume, klare Brückenränder und Gartenmöbel außerhalb der Gehwege. Lampen stehen mit Abstand zu den Bewohnern.

Eine zusätzliche Nahkontrolle zeigte, dass ein Baumwipfel am südlichen Stadtrand einen Coffee Shop teilweise verdeckte. Die Waldplatzierung berücksichtigt jetzt auch die projizierte Kronenhöhe vor Gebäudezugängen. Die finale Aufnahme `05_City_expansion.png` zeigt alle drei vorderen Shops frei. Der vorherige Befund bleibt in `garden_review4_findings.json` dokumentiert; Tests und Aufnahmen wurden nach der Korrektur vollständig erneuert.

Aktuell sind {integration['plants']} Pflanzeninstanzen und {integration['gardenProps']} weitere Gartenobjekte erfasst; vorhandene Lampen sind darin nicht mitgezählt. 16 Sprite-Prefabtypen sind vorbereitet, davon 15 in der Szene genutzt. Das kleine gemalte Fußbrückenmotiv bleibt als Reserve erhalten; die befahrbare Flussquerung verwendet ein eigenes Naturstein-Brückenprefab.

[Vorher/Nachher-Galerie und alle Spielansichten](comparison.html). [Stadt](Screenshots/04_City_start.png), [Siedlung](Screenshots/04_Settlement_start.png), [Teich](Screenshots/environment_232_152.png), [Flussquerung](Screenshots/13_farm_delivery.png).

## Integration und Erhaltung

Die Landschaft ist im Editor authoriert und unter `BurningHills_Districts/NaturalLandscape` in der gespeicherten Szene sichtbar. Wiederholte Pflanzen und Gartenobjekte sind Prefabinstanzen. Die statische Umgebung wird beim Spielen nicht aus Code erzeugt. Dekoration besitzt keine Collider und kann die Gebäudeauswahl nicht abfangen. Wind und Wasser benötigen keine Update-Komponente pro Pflanze.

Die Originalumgebung bleibt in ihrer bisherigen Hierarchie inaktiv erhalten. Das explizite Menü `Weedtopia/Production/Author Natural Landscape` erkennt den vorhandenen Installationsstempel und verändert bei erneutem Aufruf keine Szenenbytes. `RebuildOwned` dient ausschließlich der bewussten Iteration in einer eigenen Batch-Instanz; es ist kein automatischer Import- oder Start-Hook und darf nach manuellen Landschaftsänderungen nicht blind erneut ausgeführt werden.

Der SHA-256-Vergleich umfasst {preserve['baselineFiles']} vorhandene Dateien: keine fehlt, alle bisherigen Kunstquellen, Gebäude-Wrapper und übrigen geschützten Assets sind unverändert. Innerhalb der Szene sind alle {preserve['protectedGameplayComponentsAndPrefabInstances']} ursprünglichen Gameplay-Komponenten und Prefabinstanzen unverändert. Vorhandene Szenenobjekte wurden nicht gelöscht. 56 Bauplätze, 35 Routen, Routenpunkte, Fahrzeiten, Inventar, Worker, Bewohner und UI-Verknüpfungen bleiben erhalten. Verändert wurden die aktiven Umgebungshierarchien sowie Straßen-Meshes und -Materialien.

[Ausgangssicherung und Vergleich](preservation.json). Die beiden geänderten bestehenden Dateien sind `BurningHills.unity` und `LayoutVisualEvidence.cs`; die übrige Implementierung und Kunst wurden ergänzt. Projektoptionen, Benutzerspielstand und offene Editorinstanzen bleiben geschützt. Vor jedem Unity-Start prüft der Runner Prozesse und Lockfile; alle verwendeten Unity-Prozesse wurden selbst gestartet und beendet.

## Aktuelle Nachweise

| Prüfung | Ergebnis | Beleg |
| --- | --- | --- |
| Unity EditMode | {edit['passed']}/{edit['total']} bestanden | [XML](../Implementation/Validation/{Path(edit['xml']).name}), [Lauf](../Implementation/Validation/{ep.name}) |
| Unity PlayMode | {play['passed']}/{play['total']} bestanden | [XML](../Implementation/Validation/{Path(play['xml']).name}), [Lauf](../Implementation/Validation/{pp.name}) |
| Source-Compiler | {len(source['results'])} Assemblies kompiliert; Quellenprüfsummen aktuell | [Compilerbeleg](../Implementation/Validation/SourceCompile/result.json) |
| Game View | 79 tatsächliche PlayMode-Aufnahmen, davon 51 Gebäudezustände; alle als Kontaktbögen geprüft, zentrale Umweltansichten zusätzlich in voller Auflösung | [Aufnahmebeleg](gameview_receipt.json), [Agentenprüfung](visual_review.json) |
| UI | 1920×1080, 1366×768 mit 115% UI-Skalierung, 2560×1080 ohne gemeldeten Überlauf | [UI-Prüfung](../ProductionFinish/Evidence/ui_overflow.json) |
| Animierte Natur | zwei aufeinanderfolgende Aufnahmen derselben Kameraposition; Wasser und Laub bewegen sich | [Bildvergleich](motion_evidence.json) |
| Windows-Build | Development-Build erfolgreich, eigene Ausführung mit aktivierter Grafik | [Build](../ProductionFinish/Evidence/development_build.json), [Player](../ProductionFinish/Evidence/player_run.json) |
| Produktions-Preflight | keine Asset-, Material- oder sichtbaren Entwicklungsplatzhalter-Befunde; externe Abnahme-Gates offen | [Preflight](../ProductionFinish/Evidence/production_preflight.json) |

Die zwei ergänzten EditMode-Tests prüfen die gespeicherten Prefabinstanzen, Spritezuordnung, Shader, fehlende Dekorations-Collider sowie das unveränderte Szenenresultat bei erneutem Installer-Aufruf. Die bestehenden PlayMode-Tests prüfen weiterhin Spiellogik, Logistik, Navigation und Auswahl in den vier Bezirken.

Der Windows-Diagnoselauf nutzt {measure['device']} mit {measure['graphics']} bei 1920×1080 und 300 Messframes pro Szenario. Die drei resultierenden PNGs wurden angesehen. Er verwendet explizite URP-Renderanfragen im Batchmodus und misst keine normale Fensterpräsentation oder Eingabelatenz. Die Zahlen sind keine Zielgeräte-FPS-Freigabe; Texture-Runtime-Speicher ist keine VRAM-Messung.

| Szenario | Median / P95 / Maximum | Max. Draw Calls | Texture-Runtime-Speicher |
| --- | --- | --- | --- |
{rows}

Alle Nachweise beziehen sich auf Fingerprint `{capture['fingerprint']}`. Die früheren Layout- und Umgebungs-Iterationsaufnahmen bleiben historische Vergleichsbelege. Nur die hier verlinkte finale Aufnahmeprüfung beschreibt diesen Stand.

## Quellen und noch offene Gesamtfreigabe

Drei neue Originalatlanten wurden mit dem integrierten ImageGen erzeugt und ohne Pixeländerung nach Unity übernommen: `Woodland.png`, `Gardens.png` und `Ground.png` unter `Assets/Weedtopia/Production/Environment/Art`. Sprite-Rechtecke wurden anhand vorhandener Alpha-Grenzen vermessen; Farbwirkung und Bewegung erfolgen über Materialien. Die Nutzerreferenz wurde als Gestaltungsrichtung verwendet, nicht als kopierte Spielgrafik.

[Vollständige Prompts](art_prompts.json), [Dateiherkunft und Prüfsummen](art_provenance.json), [aktualisiertes Assetregister](../ProductionFinish/VISUAL_ASSET_REGISTER.csv), [Registervergleich](register_receipt.json).

Diese Sichtprüfung ist eine Agentenprüfung, keine vorweggenommene Zustimmung des Benutzers zur neuen Umgebung oder zum Gesamtspiel. Die vier Gebäudeprototypen bleiben ausdrücklich freigegeben. Noch offen sind `{gates}`. Das Gesamtspiel wird deshalb weiterhin nicht als vollständig abgenommen oder releasefertig bezeichnet.
'''
(OUT/'REPORT.md').write_text(report,encoding='utf-8')
status=f'''# Burning Hills — Production Finish

Stand {now}. **Natürliche Umgebung integriert und erneut geprüft; Gesamtspiel nicht final abgenommen.**

Die akzeptierten 251 Gebäude-Wrapper und ihre Kunstquellen bleiben unverändert. Die Szene nutzt jetzt gemalte Baumgruppen, bepflanzte Höfe, Natursteinwege, weiche Wiesen- und Uferkanten, einen Teich und eine Steinbrücke. Wasser und Laub sind dezent animiert. Die 56 Standorte, 35 Routen und bestehende Spielmechanik sind erhalten.

Aktuelle Evidenz: Unity EditMode {edit['passed']}/{edit['total']}, PlayMode {play['passed']}/{play['total']}, 79 Game-View-Aufnahmen, UI in drei Auflösungen sowie Windows-Diagnosebuild mit drei gerenderten Szenarien. Der Bestandsvergleich bestätigt unveränderte ursprüngliche Kunst und {preserve['protectedGameplayComponentsAndPrefabInstances']} ursprüngliche Gameplay-Komponenten/Prefabinstanzen.

[Aktueller Prüfbericht](../EnvironmentRevision/REPORT.md) · [Vorher/Nachher](../EnvironmentRevision/comparison.html) · [Assetregister](VISUAL_ASSET_REGISTER.csv) · [Quellen](../EnvironmentRevision/art_provenance.json).

Gesamtfreigabe offen: `{gates}`. Die vorhandene Gebäude-Prototypfreigabe gilt weiterhin; Game-View-Aufnahmen und automatische Tests ersetzen keine Benutzer- oder Zielgeräteabnahme. Der [Layoutbericht vom 23. September](../LayoutRevision/REPORT.md) beschreibt die vorherige Umgebung und bleibt historische Evidenz.
'''
(ROOT/'Docs/ProductionFinish/STATUS.md').write_text(status,encoding='utf-8')
(ROOT/'Docs/ProductionFinish/VALIDATION_REPORT.md').write_text(f'# Aktueller Prüfbericht\n\nStand {now}. EditMode {edit["passed"]}/{edit["total"]}; PlayMode {play["passed"]}/{play["total"]}.\n\n[Der vollständige aktuelle Bericht](../EnvironmentRevision/REPORT.md) dokumentiert die neue Umgebung, 79 Game-View-Aufnahmen und den Windows-Diagnoselauf. Agenten-, Benutzer- und Zielgeräteabnahme bleiben getrennt. Frühere Berichte beziehen sich auf die jeweilige historische Szene.\n',encoding='utf-8')
write('Docs/EnvironmentRevision/delivery.json',{'utc':now,'environmentImplemented':True,'fingerprint':capture['fingerprint'],'editMode':edit['passed'],'playMode':play['passed'],'gameViewCaptures':79,'preservationPassed':True,'windowsDiagnosticPassed':True,'humanAcceptance':False,'releaseAccepted':False,'openGates':[i['objectPath'] for i in preflight['issues']]})
print('Environment report written after evidence verification:',capture['fingerprint'])
