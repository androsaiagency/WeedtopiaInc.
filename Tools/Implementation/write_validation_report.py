"""Summarize current, successful raw receipts without promoting manual gates."""
import datetime
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/Implementation/Validation'
def read(name):
    return json.loads((OUT / name).read_text(encoding='utf-8-sig'))

def latest(mode):
    runs = [read(p.name) for p in OUT.glob('*_' + mode + '_*.json')]
    return max(runs, key=lambda x: x['startedUtc'])

edit, play, capture = (latest(mode) for mode in ['EditMode', 'PlayMode', 'Capture'])
assert edit['status'] == play['status'] == 'passed'
assert capture['exitCode'] == 0
manifest = read('evidence_manifest.json')
assert manifest['status'] == 'passed'
progress = read('progression_simulation.json')
preserve = read('preservation_final.json')
assets = read('game_asset_validation.json')
idem = read('game_idempotence.json')
visual = read('visual_capture.json')
texture = read('sprite_prototype_validation.json')
compiled = read('SourceCompile/result.json')
assert visual['screenshots'] == 14 and not visual['manualAcceptance']
start = datetime.datetime.fromisoformat(capture['startedUtc'].replace('Z', '+00:00')).timestamp()
screens = []
for path in sorted((OUT / 'Screenshots').glob('*.png')):
    assert path.stat().st_mtime >= start, f'Stale screenshot: {path.name}'
    screens.append(dict(path=path.relative_to(ROOT).as_posix(), sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
assert len(screens) == 14
(OUT / 'screenshot_manifest.json').write_text(json.dumps(dict(utc=visual['utc'], run=capture['run'],
    mode='actual_Unity_GameView_ScreenCapture_automated_public_commands', manualAcceptance=False,
    screenshots=screens), indent=2), encoding='utf-8')

when = datetime.datetime.now(datetime.timezone.utc).isoformat()
report = f'''# Burning Hills – lokaler Validierungsbericht

Stand: {when}. Unity 6000.3.9f1 (7a9955a4f2fa), Windows, URP 17.3.0, Input System 1.18.0, uGUI 2.0.0, Test Framework 1.6.0. Inhaltsversion 1, Save-Schema 2. Kein Git-Repository festgestellt; keine Git-, Commit- oder Deploymentbehauptung.

## Aktuelle Gates

| Gate | Ergebnis und Zeitpunkt UTC | Roh-Evidenz | Grenze |
| --- | --- | --- | --- |
| Quellkompilierung | {len(compiled['results'])} Assemblies erfolgreich, {compiled['utc']} | `Validation/SourceCompile/result.json` und Assemblylogs | Roslyn gegen installierte Unity-Referenzen; vier CS0649-Warnungen für serialisierte Felder, keine Compilerfehler |
| Unity-Import / Domain Reload | Erfolgreich in aktuellen Test-/Captureläufen | `{edit['run']}.log`, `{play['run']}.log` unter Validation | Gilt für dieses Unity-Projekt/diese Editorversion |
| EditMode | {edit['passed']}/{edit['total']}, 0 fehlgeschlagen, 0 übersprungen; {edit['endedUtc']} | `Validation/{edit['run']}.xml` und `.json` | 32 Gameplay-/Save-/Progressiontests, 14 bestehende Arttests, 4 Assettests |
| PlayMode | {play['passed']}/{play['total']}, 0 fehlgeschlagen, 0 übersprungen; {play['endedUtc']} | `Validation/{play['run']}.xml` und `.json` | Echte Szenen/UI-Befehle, teilweise deterministische Command-Automation; kein menschlicher Hauptpfad |
| Szenen / Assets | {assets['sites']} Instanzen, {assets['events']} EventSystem, {assets['missingScripts']} Missing Scripts, {assets['errors']} Fehler; {assets['utc']} | `Validation/game_asset_validation.json` | Bootstrap/BurningHills in Build Settings; Pflichtreferenzen, IDs, Katalog und gesperrtes Produktionsgate geprüft |
| Explizite Installation / Idempotenz | Initialer Installationslauf plus aktuelle Reparatur, {idem['gameFiles']} Gameplaydateien und {idem['productionFiles']} Dateien im Produktions-Prefabordner bytegleich; {idem['utc']} | `Validation/game_idempotence.json`, `Validation/20260921_221937_Install_M4.json`, aktueller Assettest | Zweiter Lauf überschreibt keine authorierten Daten; unklare bestehende Szene wird geschützt |
| Reale Game View | 14 PNGs, {visual['utc']} | `Validation/{capture['run']}.log`, `Validation/screenshot_manifest.json`, `Validation/Screenshots/` | Automatisierter PlayMode mit ScreenCapture, Bilder agentseitig geprüft; kein manueller Durchlauf |
| 2D-Prototypen | Vier isolierte Prefabs, Alpha, Bodenpivot, Anker, Mipmaps, ein Collider je Prefab und Sortingkomponente geprüft | `Validation/sprite_prototype_validation.json` | Ausdrückliche Nutzerfreigabe ausstehend; 251 Produktions-Wrapper unverändert |
| Originalbestand | {preserve['baselineFiles']} Ausgangsdateien, {len(preserve['missing'])} fehlend, vier begründete Änderungen, 251 Produktions-Wrapper bytegleich | `Validation/m0_baseline.json`, `Validation/preservation_final.json` | Hashvergleich des vorhandenen Bestands, kein Ersatz für Git-Historie |

`Validation/evidence_manifest.json` bindet aktuelle C#-/asmdef-/Contentdateien per SHA256, bestätigt passende Quellkompilierung und dass die Quellen vor den letzten Teststarts vorlagen. Frühere fehlerhafte Läufe bleiben als Diagnosehistorie vorhanden und werden nicht als aktuelles Ergebnis verwendet.

## Was ausgeführt wurde

EditMode prüft skalierbare Zahlen, alle 5.001 Level je Katalogfamilie, monotone Kurven, 251 Katalogbänder, Massenbilanz bei vollen Puffern, verzögerte/blockierte Fracht, Fahrerbindung, manuelle/automatische Arbeit, Workerrollen/Seltenheiten/Level/Rang, deterministischen Spin, Geld-/Grindertransaktionen, doppelte Belohnungen, Tagesgrenzen/Rückstellung/Nachholen, VIP-Erreichbarkeit, Bauplatzreservierung, Save-/Backup-/Migration-/Offlineverhalten, Referenzfehler und Fortschritt bis Freeplay. Bestehende Arttests wurden nicht umgeschrieben.

PlayMode lädt Bootstrap/BurningHills, startet über die tatsächlichen Namens-/Start-/Skip-Schaltflächen und prüft Farm, Stadtverkauf, Rathaus, Wohnhaus-/Park-Roundtrip nach Szenenneuladen, Industrieverkäufe und Freeplay. Zusätzliche echte UI-Buttonpfade prüfen Worker zuweisen/freistellen/verbessern und Premiumabbruch/-bestätigung. UI-Raycasts verhindern World-Clicks; Pause hält die Simulation an. Die größte angebotene UI-Skalierung lässt Aktionen oberhalb der Navigation und alle Navigationsknöpfe innerhalb des Bildes. Kein unerwarteter Logfehler im geprüften PlayMode-Pfad.

Der öffentliche Command-Fortschrittssimulator erreichte {progress['quests']} Quests in {progress['seconds']} simulierten Sekunden, Freeplay={progress['freeplay']}, {progress['grinders']} Grinder verbleibend und {progress['money']} Geld. Kein Premiumkauf und keine direkte Injektion von Geld, Questfortschritt oder Freischaltungen. Ein gesonderter Pfad mit früh gebautem zweitem Anbauplatz und zweiter Bäckerei erreicht ebenfalls Freeplay. Die 72 Sekunden sind kein menschlicher Tutorialzeitwert.

## Sichtprüfung und Prototypen

Aufnahmen: Intro/Rocko-Text; manuelle Farm; automatisierte Farm mit Engpass; Stadt/Rathaus; Worker nach Spin; Wohnhaus/Park; Industrie/Freeplay; 1366×768 und 2560×1080; größte UI-Skalierung 1,15 bei 1366×768; isolierte Gebäude bei Orthogröße 26 und 38 sowie Nahansichten bei 14.

Erkannte und reparierte Darstellungsfehler: zu kleine Gebäudenamen, mehrdeutige summierte Pufferanzeige, abgeschnittener Aufbauhilfe-Text und Überlappung mit Navigation auf Breitbild. Die letzte Variante verwendet getrennte Puffer, kompakteres Detailpanel, horizontale Navigation mit flexiblen Breiten und am unteren Rand verankerte Aufbauhilfe. Panels sind deckend. Deutsche Texte wurden nach einem Windows-Encodingfehler korrigiert und durch einen Unicode-Codepoint-Test abgesichert. Kurze Engpassmeldungen passen in den geprueften Statusbereich. Die letzten Aufnahmen zeigen diese Korrekturen.

Die drei neuen Coffee-Shop-Cutouts liegen in `Assets/Weedtopia/Game/VisualReview/Sprites/`; das vorhandene Rathausbild bleibt seine unveränderte Quelle. Alle vier Texturen haben 1254×1254 Pixel. Gemessen wurden je {texture['prototypes'][0]['textureBytes']:,} Bytes importierter Texturspeicher inklusive Mips, zusammen etwa 32 MiB. Das ist eine Editor-Speichermessung der vier Prototypen, kein GPU-/FPS-/Mobilbudget für alle 251 Slots. Vor Massenintegration sind bedarfsgesteuertes Laden, plattformspezifische Kompression und Zielgeräteprofiling nötig. Prompts/Quellen: `Validation/sprite_cutout_prompts.json`.

## Offene Abnahmen

- **M9: ausdrückliche visuelle Nutzerfreigabe** für Rathaus und frühe/mittlere/finale Coffee-Shop-Stufe. Bis dahin keine Massenumstellung. `productionVisualsApproved` bleibt false. Produktive 251-Sprite-Abdeckung kann deshalb noch nicht bestanden sein.
- Vollständiger menschlich bedienter Hauptpfad in Game View einschließlich Kamera-/Mausgefühl, lesbarer Dialoge, Save/Load/Recovery und Tageswechsel. Automatische Tests und Bilder ersetzen diese Prüfung nicht.
- Physisches Touchgerät, Android/iOS/WebGL-Builds, Browser, Zielgeräte-Audio und tatsächliches Anhören der Entwicklungsclips wurden nicht geprüft.
- Profiling unter Zielbedingungen, Overdraw, Framezeit/GC, Langzeitsimulation und finales Balancing sind offen. Keine FPS-Zahl behauptet.
- Finale NPC-/Weltgestaltung, animiertes Onboarding, fertiges VFX-/Audio-Polish folgen der gestalterischen Abnahme. Die lokale Spielwelt verwendet erkennbare Entwicklungs-Prefabs.
- Online-/globale Rangliste ist ausdrücklich deaktiviert. Kein Backend, keine Authentifizierung, keine Cloud-/Produktivtests oder Deployments.

## Wiederholung und nächste Bedienung

Projekt mit Unity 6000.3.9f1 öffnen, `Assets/Weedtopia/Game/Scenes/Bootstrap.unity` laden und Play drücken. Namen eingeben, Intro ansehen/überspringen und die angezeigte Hauptquest bedienen. Einzelne Gebäude im World View auswählen. Rathaus enthält Spin/Tagesabholung; das Journal enthält VIP-Rewards. Produktionsart separat über `Assets/Weedtopia/Game/VisualReview/Building2DReview.unity` begutachten.

Explizites Authoring: `Weedtopia > Setup > Create or Repair Game Scaffold`; Prüfung: `Weedtopia > Setup > Validate Game Assets`. Vorher eigene schmutzige Szenen speichern. `Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode` beziehungsweise `-Mode PlayMode` starten nur ohne laufenden Unity-Prozess und ohne Projektlock. Ein bereits geöffneter Benutzereditor wird niemals beendet. Das alte eigene verwaiste Lock wurde einmal nach bestätigtem Prozessende/exklusiver Öffnung archiviert; dieser Vorgang ist keine automatische Lock-Löschfunktion.

Die erste Produktversion wird wegen dieser offenen Gates ausdrücklich noch nicht als vollständig abgenommen oder veröffentlicht bezeichnet.
'''
(ROOT / 'Docs/Implementation/VALIDATION_REPORT.md').write_text(report, encoding='utf-8')

status = f'''# Burning Hills – Implementierungsstand

Stand: {when}. Masterauftrag vollständig gelesen; M0 vor Änderungen neu gegen Projekt, Prozesse, Lock, Quellbestände, Szenen, Pakete und bestehende Artberichte geprüft. Ergebnis und Ausführung sind in `VALIDATION_REPORT.md` mit Rohbelegen getrennt dokumentiert.

| Meilenstein | Stand |
| --- | --- |
| M0 Audit / Erhaltung | Aktuell durchgeführt. 3.171 vorhandene Dateien gehasht; 251 Produktions-Wrapper unverändert, nichts entfernt. |
| M1 Domain | Implementiert und EditMode-geprüft: skalierbare Zahlen, Stufen, Puffer/Umwandlung, Fahrer, Worker, Ausbau/Premium. |
| M2 Progression | 16 datengetriebene Hauptquests, Zonen, VIP, Tag, Recovery; Referenz- und optionaler Ausbaupfad erreichen Freeplay ohne Premium. |
| M3 Persistenz | Save-Schema 2, Atomik, Backup, Recovery, Migration, acht Stunden Offline, Uhrenrückstellung, Save/Szenenreload/Continue geprüft. |
| M4 Unity-Integration | Bootstrap und BurningHills authoriert; 16 Instanzen, Kamera/Input, uGUI, EventSystem, Fahrzeuge; Pflichtreferenzen/Idempotenz geprüft. |
| M5 Farm | Einstieg, manuelle Ernte, Manager, Veredelung, Lager/Fahrer und Stadtfreischaltung integriert; PlayMode und Game View ausgeführt. |
| M6 Stadt / Rathaus | Coffee-Verkauf, Statistik, Worker/Spin, lokale Bestenliste; Online-deaktiviert-Zustand integriert und geprüft. |
| M7 Siedlung | Dauerhafte Platzwahl, Häuser/Parks, Einwohner/Karten/VIP, Hausmeister/Parkmanager, Tage und Roundtrip geprüft. |
| M8 Industrie / Freeplay | Butter, Supermarkt, Bäckerei, Verkäufe, tägliche Grinder und Abschluss integriert und geprüft. |
| M9 Visuelle Produktion | Vier isolierte 2D-Prototypen und echte Game-View-Nachweise vorbereitet. **Nutzerfreigabe offen. 251er-Massenintegration nicht ausgeführt.** |
| M10 Politur / Abnahme | Automatische Prüfungen, UI-Korrekturen, Audiohooks und Game-View-Aufnahmen ausgeführt. Manueller Gesamtdurchlauf, finales Art-/Audio-/VFX-Polish, Touch, Zielgeräte und Performance offen. |

Aktuell: {edit['passed']}/{edit['total']} EditMode, {play['passed']}/{play['total']} PlayMode, {len(compiled['results'])} Quellassemblies, null Missing Scripts/Pflichtreferenzfehler. Rohdateien: `Validation/{edit['run']}.xml`, `Validation/{play['run']}.xml`, `Validation/evidence_manifest.json`. Screenshots: `Validation/Screenshots/`.

Beim Audit bestanden 13 vorhandene C#-Dateien und nur SampleScene als Startszene. Jetzt existieren separate Domain-, Application-, Persistence-, Content-, Unity-, Editor- und Testassemblies; authorierte Gameplayassets liegen unter `Assets/Weedtopia/Game`. Keine neue Paketinstallation, kein Commit/Push/Deployment und kein beendeter Benutzereditor.

Vier bestehende Dateien sind begründet geändert: Sprite-Prototyp-Utility (Importerreihenfolge/Erhalt vorhandener GUIDs), Metadaten des vorhandenen Rathaus-Sprites (korrekter Spriteimport), EditorBuildSettings (zwei Spielszenen ergänzt) und ArtProduction/PROGRESS.json (aktuelle Evidenz statt überholtem Editorblocker). Vorhandene Bilder, Modelle, manuelle Wrapperzuweisungen, Offsets und Projekteinstellungen außerhalb der Buildliste sind hashgleich.

Die produktive Artfreigabe fehlt. Der aktuelle Gameplaypfad ist eine funktionierende lokale Version mit Entwicklungsdarstellung und kein vollständig freigegebenes Endprodukt. Der frühere offene-Editor-Blocker ist durch frische sichere eigene Läufe aufgelöst. Historische fehlgeschlagene Versuche werden im Ergebnis nicht mitgezählt.
'''
(ROOT / 'Docs/Implementation/IMPLEMENTATION_STATUS.md').write_text(status, encoding='utf-8')
print(json.dumps(dict(editMode=edit['passed'],playMode=play['passed'],screenshots=len(screens),
                     userVisualApproval=False,report='Docs/Implementation/VALIDATION_REPORT.md')))
