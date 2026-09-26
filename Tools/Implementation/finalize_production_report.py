"""Summarize only fresh file-backed evidence; never infer user acceptance."""
import collections,hashlib,json
from pathlib import Path
from datetime import datetime,timezone
R=Path(__file__).resolve().parents[2];O=R/'Docs/ProductionFinish';V=R/'Docs/Implementation/Validation'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def rel(p):return p.relative_to(O).as_posix() if p.is_relative_to(O) else '../Implementation/Validation/'+p.name
def latest(pattern):return sorted(V.glob(pattern))[-1]
editp=latest('*_EditMode_ProductionFinal.json');playp=latest('*_PlayMode_ProductionFinal.json');capturep=latest('*_ProductionCapture_FinalReview.json')
edit,play,capture=map(read,[editp,playp,capturep]);assert edit['status']=='passed' and play['status']=='passed'
source=read(V/'SourceCompile/result.json');assert source['status']=='passed'
for assembly in source['results']:
    assert all(hashlib.sha256((R/p).read_bytes()).hexdigest()==sha for p,sha in assembly['sources'].items())
ui=read(O/'Evidence/ui_overflow.json');pre=read(O/'Evidence/production_preflight.json');pres=read(O/'Evidence/preservation.json');build=read(O/'Evidence/development_build.json');run=read(O/'Evidence/player_run.json');profilep=Path(run['profile']);profile=read(profilep)
counts=collections.Counter(x['code'] for x in pre['issues']);assert pre['productionReady'] is False and pre['userApproved'] is False and counts['BUILDING_SPRITE_MISSING']==251
assert pres['passed'] and ui['passed'];assert all(x['maxDrawCalls']>0 for x in profile['measurements'])
assert ui['fingerprint']==build['fingerprint'], 'Final capture/build must identify the same source and asset state'
now=datetime.now(timezone.utc).isoformat()
table='\n'.join(f"| {x['scenario']} | {x['frames']} | {x['medianMs']:.2f} ms | {x['p95Ms']:.2f} ms | {x['maxDrawCalls']} | {x['maxGcBytes']:,} |" for x in profile['measurements'])
report=f'''# Production Finish — Validierungsbericht

Stand: {now}. **Release nicht freigegeben.** Aktive Entwicklungsgebäude und die 251 fehlenden Sprite-Integrationen bleiben ausdrücklich offen.

## Aktuelle Prüfungen

| Gate | Nachweis und Ergebnis |
| --- | --- |
| Source/Compiler | {len(source['results'])} Assemblies kompiliert; aktuelle C#-Hashes gegen Ergebnis geprüft. [Rohdaten](../Implementation/Validation/SourceCompile/result.json). CS0649 für durch Unity serialisierte Felder sind protokolliert; keine Compilerfehler. |
| Unity-Import | Eigene Unity 6000.3.9f1-Läufe für Installation, Korrekturen, Tests und Build. 70 importierte Sprites, Fonts, Audio, 8 Figuren und authorierte Routen durch Assettests geprüft. |
| EditMode | **{edit['passed']}/{edit['total']} bestanden**, {edit['failed']} Fehler, {edit['skipped']} übersprungen. [XML](../Implementation/Validation/{Path(edit['xml']).name}), [Lauf](../Implementation/Validation/{editp.name}). |
| PlayMode | **{play['passed']}/{play['total']} bestanden**, {play['failed']} Fehler, {play['skipped']} übersprungen. [XML](../Implementation/Validation/{Path(play['xml']).name}), [Lauf](../Implementation/Validation/{playp.name}). |
| Game View | 21 echte Unity-Game-View-PNGs; automatisierter Session-/UI-Hauptpfad und separate Review-Szene. [Capture-Lauf](../Implementation/Validation/{capturep.name}), [Bilder](comparison.html). Keine menschliche Bedienabnahme. |
| UI-Geometrie | 1920×1080, 1366×768, 2560×1080 einschließlich UI-Skalierung 1,15; im erfassten Bildsatz kein gemessener Textüberlauf, keine erfasste Aktion außerhalb Safe Area, keine ungelösten UI-/Speaker-Schlüssel. [Aktueller Beleg](Evidence/ui_overflow.json). Diese Messung ersetzt weder jede mögliche Datenkombination noch physische Touchprüfung. |
| Preflight | **Fehlgeschlagen – vorhandene Blocker erkannt**, {len(pre['issues'])} genaue Einträge. 251 fehlende Gebäudesprites, {counts['FORBIDDEN_DEPENDENCY']} Entwicklungsabhängigkeiten, aktive Entwicklungszuordnung, fehlende Gebäudeentscheidung und offene externe Gates. [Vollständige Objekt-/Assetpfade](Evidence/production_preflight.json). |
| Windows-Build | Lokaler StandaloneWindows64 Development-Build erfolgreich, {build['bytes']:,} Bytes. [Buildbeleg](Evidence/development_build.json). Kein freigegebener Releasebuild. |
| Player-Profil | Drei Szenarien mit jeweils 300 gerenderten Offscreen-Frames und PNG-Rücklesung. [Messung]({rel(profilep)}), [Prozessbeleg](Evidence/player_run.json). Keine Zielgeräte-Endabnahme. |
| Erhaltung | {pres['baselineFiles']} Baseline-Dateien geprüft. 251 Wrapper bytegleich, keine geschützte Änderung, keine fehlende Quelle. Vier ersetzte Assets samt Meta mit identischen Bytes archiviert. Domain/Application/Persistence und sämtliche Nicht-Text-Gameplaydefinitionen unverändert. [Vergleich](Evidence/preservation.json). |
| Audio | Originale fünf WAVs technisch vermessen und in Unity gebunden. Musik exakt 60s/2.646.000 Samples. [PCM/Hash-Daten](audio_provenance.json). Abhören, Mix und kommerzielle Lizenzvoraussetzungen offen. |
| Nutzerfreigabe / Mensch / Gerät | Keine explizite Gebäude-Freigabe eingegangen; kein menschlicher Gesamtdurchlauf, kein physischer Touch-/Audio-Gerätetest. |

## Getestete Verhaltensbereiche

Die vorhandenen Domain-, Save-, Offline-, Zonen-, Tierband-, Worker- und Progressionstests wurden erhalten. PlayMode deckt Bootstrap, Startressourcen, echte Upgrades, manuelle/automatische Farm, Stadtverkauf, Rathaus/Spin, Haus-/Parkwahl, Save-Szenenreload-Continue, Industrie bis Freeplay, UI-Raycast-Sperren/Pause, Workerzuweisung/Verbesserung und Premiumbestätigung ab. Neu: alle acht NPC-Klickflächen öffnen das passende Portrait; alle zwölf Rollenfilter und höchste Seltenheit mit vollständigen Attributen bleiben lesbar. Assettests prüfen Importabdeckung, Audio, authorierte Routen, erneuten Installer-Aufruf sowie Ablehnung fehlender Gebäude/Freigaben und veralteter Abnahmebelege.

Unity erzeugte beim ersten Diagnosebuild außerdem Shader-Prefilter-/Global-Settings-Metadaten in zwei URP-Assets. Diese zwei Dateien sind im Erhaltungsbericht ausdrücklich als verändert aufgeführt; die Assets/GUIDs bleiben erhalten. Automatisch veränderte Projekt-Batching- und Unity-Connect-Schalter wurden aus der gesicherten Baseline zurückgestellt. Der Runner sichert diese Einstellungen und die URP-Dateien jetzt vor jedem Lauf und stellt sie danach wieder her.

Der erste neue NPC-Test fand eine falsche Auswahl durch einen benachbarten großen Gebäudecollider (11/12). Das Picking wurde korrigiert; die aktuelle gesamte Suite oben besteht. Historische fehlgeschlagene Testläufe bleiben als Diagnose erhalten und werden nicht als bestanden gezählt.

## Performance — begrenzter, nachvollziehbarer Befund

Windows x64 Development, Direct3D11, NVIDIA GeForce RTX 3070, 1920×1080, VSync aus, 90 Aufwärmframes plus 300 Messframes je Zustand. Verborgene Fenster übersprangen zunächst Rendering: die beiden Null-Draw-Call-Läufe um 07:35/07:36 sind **ungültig für Grafikperformance**. Der aktuelle Runner lehnt solche Ergebnisse ab. Explizite URP-StandardRequests rendern jetzt die tatsächliche Szene samt UI in ein RenderTexture; nur im Diagnosemodus verwendet das Canvas ScreenSpaceCamera mit eigener Vordergrundsortierung. Die reguläre Game View verwendet weiterhin ihr authoriertes Overlay.

| Zustand | Frames | Median | p95 | Max. Draw Calls | Max. GC-Bytes im Frame |
| --- | ---: | ---: | ---: | ---: | ---: |
{table}

Textur-Runtimegröße inklusive Diagnose-Renderzielen: {profile['measurements'][-1]['textureRuntimeBytes']/1024**2:.1f} MiB. Das ist keine Messung der belegten GPU-VRAM. GC-Spitzen schließen Simulationsaktionen, dynamische vorhandene Gebäudevisualwechsel und Diagnosearbeit ein. Die Zahlen sind kein Nachweis für die späteren 251 Sprites, WebGL, Mobilgeräte oder einen langfristigen Idle-Test. Der formale Zielprofil-Gate bleibt offen.

## Visuelle Korrekturen und verbleibende Befunde

Korrigiert: falsch abgegrenzte Atlasicons, ungelöster Sprechertext, falsches Questportrait, abgeschnittene Worker-Rollenbezeichnung und Close-Beschriftung, verdeckte Gesprächsschließfläche, verzerrte Slidergriffe, Figur/Leuchten-Überlappung, interne Tierkennungen im Detailpanel, harte Wasser-Mip-Nähte und übergroße Auswahlgrafik. Figuren, Lieferwagen, Pflanzen, Lichtprops, Terrain, Förderbänder, UI-Rahmen, Währungsicons und Portraits sind authoriert integriert. Neue Konversationen bleiben nach dem Tutorial sinnvoll.

**Verbleibt sichtbar:** die aktiven Entwicklungsmodelle für Gebäude/Ruinen/Plots. Die vier vorhandenen Review-Prototypen bleiben außerhalb des Buildpfads, bis der Nutzer sie ausdrücklich freigibt. Ein kompletter visueller Spielabschluss wird nicht behauptet. Nach Freigabe müssen sämtliche 251 zugehörigen Quellen einzeln aufbereitet, integriert, über alle Bandgrenzen mit Save/Load geprüft und erneut visuell sowie performant abgenommen werden.

Der Bildsatz enthält Intro, manuelle Farm, Stau, Stadt/Rathaus, Siedlung, Industrie, Freeplay, Worker/Spin/Zuweisung, Journal, Einstellungen, Premiumbestätigung, Gespräch, Startansicht sowie Nah-/Fernvergleich der vier Gebäudeprototypen. Höhere produktive Gebäude-Upgrades können vor deren Freigabe/Integration nicht visuell akzeptiert werden.

## Reproduktion und offene Abnahme

`Tools/Implementation/Run-UnityValidation.ps1` bietet EditMode, PlayMode, ProductionCapture, Preflight und ProductionBuild. Es startet nur ohne Unity-Prozess und Lockfile. `run_production_player.ps1` nutzt einen eigenen Diagnose-Spielstand und beendet nur den eigenen Player. Ergebnisdateien müssen aktuell sein; Exitcode allein zählt nicht als Testnachweis. Aktueller Asset/Source-Fingerprint: `{ui['fingerprint']}`.

Für die Endabnahme fehlen: explizite Entscheidung der vier Gebäudeansichten; 251 Einzelintegrationen/QA samt endgültigem Providerwechsel; menschlicher Hauptpfad inklusive Save/Offline/Upgrades; Audio-Abhören und Klärung der kommerziellen Modell-Lizenzvoraussetzungen; vollständige Zielplattform-, GPU-Speicher- und Touchprüfung. Keines dieser Gates wurde durch Screenshots oder Testzahlen ersetzt.

Die visuellen Einzelbefunde des Assistenten stehen in [Game-View-Sichtung](Evidence/gameview_review.json). Sie sind keine Nutzerfreigabe. Nach dem mit Exitcode 1 beendeten letzten Preflight blieb `Temp/UnityLockfile` zurück; der eigene Unity-Prozess ist beendet. Die Datei wurde nicht gelöscht und keine weitere Unity-Instanz gestartet. Vor einem neuen Batchlauf muss der reale Editor-/Lockzustand erneut geprüft werden.
'''
(O/'VALIDATION_REPORT.md').write_text(report,encoding='utf-8')
status=f'''# Burning Hills — Production Finish

Stand {now}. **Weiterentwickelt, in Unity integriert und erneut geprüft; kein fertiges Release.**

| Bereich | Implementiert | Unity importiert | Automatisch geprüft | Game View | Nutzerfreigabe | Zielgerät |
| --- | --- | --- | --- | --- | --- | --- |
| Nicht-Gebäude-Welt, Props, Figuren, Logistik | Ja | Ja | Asset- und PlayMode-Tests | Bildmatrix geprüft | Gesamtstil offen | Windows-Diagnose; Endabnahme offen |
| UI, Icons, Portraits, Dialoge | Ja | Ja | Rollen, Raycasts, Layout | 3 Auflösungen geprüft | Gesamtstil offen | Physischer Touch offen |
| Originalaudio | Ja | Ja | PCM/Bindungen geprüft | Nicht anwendbar | Abhören offen | Mix/Geräte offen |
| Gameplay-Erhaltung | Unveränderte Kernlogik | Ja | {edit['passed']}/{edit['total']} EditMode, {play['passed']}/{play['total']} PlayMode | Automatischer Hauptpfad | Menschlicher Durchlauf offen | Kein endgültiger Plattformnachweis |
| Vier Gebäudeprototypen | Vorhanden erhalten | Review-Szene | Technisch vorhanden | Vergleich und Nah/Fern geprüft | **Ausdrücklich offen** | Offen |
| 251 produktive Gebäudezustände | **Noch nicht integriert** | Wrapper unverändert | Exakte 251er-Lücke erkannt | Aktive Entwicklungsgebäude sichtbar | Gesperrt bis Entscheidung | Offen |

Neue Assets: sechs Originalgrafik-Atlanten/Materialtexturen, 70 Sprites, acht Weltfiguren mit Portraits, zehn Umwelt-Prop-Prefabs, 48 Lieferfahrzeug-Poolplätze, authorierte Flächen/Wege/Wasser, Lato-Schriften, eigene UI-Skins und Icons, Originalmusik plus vier Rückmeldesounds. SampleScene bleibt erhalten und ist aus der Produkt-Buildliste entfernt. Vollständig ersetzte alte Figuren/Fahrzeuge/Sounds liegen GUID-erhaltend im Archiv.

Die [Vergleichsseite](comparison.html) zeigt Baseline und aktuelle Game View sowie die [konkrete Gebäudeentscheidung](BUILDING_VISUAL_APPROVAL.md). [Prüfbericht](VALIDATION_REPORT.md), [Art-System](ART_SYSTEM.md), [Quellen/Lizenzen](PROVENANCE_AND_LICENSES.md) und das 525-zeilige Assetregister dokumentieren Stand und Grenzen. Im Register bedeutet `prototype` bewusst noch nicht endgültig abgenommen; das ist keine Umdeklarierung zu „production_ready“.

Kleinster nächster Schritt: ausdrückliche Freigabe oder konkrete Korrektur der vier Gebäudeansichten. Anschließend folgt die individuelle 251er-Aufbereitung und Integration mit erhaltenen Ankern, Offsets und IDs. Unabhängig davon bleiben menschliche Bedienung, Audio-Abhören/Lizenz und Zielgeräteabnahme Pflicht. Solange Entwicklungsgebäude oder ein erforderliches Gate offen sind, bleibt `productionReady=false`.
'''
(O/'STATUS.md').write_text(status,encoding='utf-8')
(O/'VISUAL_GAPS.md').write_text('''# Aktuelle Visual Gaps

1. **Release-Blocker:** 251 Gebäudezustände sind weiterhin accepted source / not integrated. Die vier bestehenden Prototypen warten auf die ausdrückliche Entscheidung; aktive Entwicklungsgebäude dürfen erst danach vollständig abgelöst werden.
2. **Abnahme offen:** Die neuen Figuren, Weltprops, Logistik, UI, Icons und Portraits sind importiert und in echter Game View geprüft. Endgültige menschliche Gesamtstil- und Bedienabnahme ist nicht ersetzt.
3. **Audio-Blocker:** Neue Originalclips und Loop sind technisch validiert und gebunden. Abhören, Geräte-Mix und kommerzielle Lizenzvoraussetzungen sind offen.
4. **Plattform-Blocker:** Windows-Entwicklungsbuild und begrenzte Offscreen-Renderprofile liegen vor. 251er-Texturspeicher, GPU-VRAM, Langzeit-Idle, physisches Touch und endgültige Zielplattformen müssen nach vollständiger Integration abgenommen werden.

Baseline: `Baseline/`. Aktuell: `Screenshots/`. Direkter Vergleich: `comparison.html`. Exakte Asset-/UI-Einträge: `VISUAL_ASSET_REGISTER.csv`. Technische Evidenz: `VALIDATION_REPORT.md` und `Evidence/`. Kein sichtbarer Restplatzhalter ist aus der Fertigdefinition ausgenommen.
''',encoding='utf-8')
screens=[{'path':p.relative_to(O).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted((O/'Screenshots').glob('*.png'))]
(O/'Evidence/evidence_manifest.json').write_text(json.dumps({'utc':now,'editMode':editp.name,'playMode':playp.name,'capture':capturep.name,'fingerprint':ui['fingerprint'],'screenshots':screens,'humanAcceptance':False,'buildingApproval':False,'productionReady':False},indent=2),encoding='utf-8')
print(json.dumps({'editMode':edit['passed'],'playMode':play['passed'],'screenshots':len(screens),'preflightIssues':len(pre['issues']),'productionReady':False}))
