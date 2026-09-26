"""Generate the delivery report from fresh Unity receipts; never infer user or device acceptance."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, html, json, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/InteractionRevision'
VALIDATION = ROOT / 'Docs/Implementation/Validation'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def latest(mode):
    files = sorted(VALIDATION.glob(f'20260925_*_{mode}_*.json'))
    assert files, mode
    receipt = read(files[-1])
    assert receipt['status'] == 'passed', receipt
    xml = ET.parse(receipt['xml']).getroot()
    assert xml.attrib['result'] == 'Passed' and int(xml.attrib['failed']) == 0
    return receipt, files[-1]

edit, edit_path = latest('EditMode')
play, play_path = latest('PlayMode')
capture = read(OUT / 'gameview_receipt.json')
overflow = read(ROOT / 'Docs/ProductionFinish/Evidence/ui_overflow.json')
build = read(ROOT / 'Docs/ProductionFinish/Evidence/development_build.json')
player = read(ROOT / 'Docs/ProductionFinish/Evidence/player_run.json')
preservation = read(OUT / 'preservation.json')
source = read(VALIDATION / 'SourceCompile/result.json')
review = read(OUT / 'visual_review.json')
assert overflow['passed'] and preservation['passed'] and build['passed'] and player['diagnosticCompleted']
assert source['status'] == 'passed' and review['passed']
fingerprint = capture['fingerprint']
assert all(r['fingerprint'] == fingerprint for r in [overflow, build, player, review])
for assembly in source['results']:
    for path, digest in assembly['sources'].items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, path
for shot in review['screenshots']:
    assert hashlib.sha256((OUT / 'Screenshots' / shot['file']).read_bytes()).hexdigest() == shot['sha256']
preflight = read(ROOT / 'Docs/ProductionFinish/Evidence/production_preflight.json')
assert all(issue['code'] == 'ACCEPTANCE_GATE_OPEN' for issue in preflight['issues'])
gates = [issue['objectPath'] for issue in preflight['issues']]
assert 'ui_overflow' not in gates
utc = datetime.now(timezone.utc).isoformat()
delivery = dict(utc=utc, fingerprint=fingerprint, implemented=True, releaseReady=False,
                editMode=edit, playMode=play, gameViewScreenshots=len(capture['screenshots']),
                overflowPassed=True, preservationPassed=True, developmentBuildPassed=True,
                virtualMouseAndTouchTested=True, physicalTouchTested=False, openReleaseGates=gates)
(OUT / 'delivery.json').write_text(json.dumps(delivery, indent=2), encoding='utf-8')

def rel(path):
    return '../Implementation/Validation/' + Path(path).name

report = f'''# Burning Hills: freie Spielansicht und Gebäudemenüs

Stand: {utc}. Die neue Bedienung ist in der vorhandenen Szene integriert. Eine endgültige Gesamtspiel- und Geräteabnahme bleibt offen.

## Bedienung

- Im Spiel bleibt nur der kleine Menüknopf sichtbar. Questkarte, Ressourcenleiste, feste Gebäudeanzeige, Navigationsleiste und Positionsregler sind nicht mehr dauerhaft eingeblendet.
- Linke Maustaste halten und ziehen oder mit einem Finger wischen: den jeweiligen Stadtteil bewegen. Die vier Richtungen und ihre Grenzen bleiben erhalten. Mausrad und Zwei-Finger-Geste zoomen.
- Kurzer Klick oder Touch-Tipp: Gebäudemenü links, das bestehende Gebäude vergrößert rechts. Eine Bewegung über die Gestenschwelle wird nicht als Klick verarbeitet. Ein auf UI begonnener Zug bleibt der UI zugeordnet.
- Das Gebäude wird anhand seiner aktuellen Spriteinstanz eingerahmt. Schließen kehrt zur vorherigen Kameraposition und Zoomstufe zurück. Eine neue Weltgeste bzw. ein gezielter Kamerasprung beendet den bisherigen Gebäudekontext.
- Das Menü zeigt Name, Level, Workerportrait, Rolle, Seltenheit, Level/Rang und tatsächliche Attribute. Grün **+** öffnet die passende Auswahl rechts; Rot **X** entfernt die Besetzung. Weiterbildung und Rangaufstieg zeigen ihre Kosten.
- Gebäudespezifische Kennzahlen zeigen aktuelle Werte und tatsächliche Verbesserungen beim nächsten Level mit grünen Pfeilen. Werte, die sich nicht ändern, erhalten keinen Verbesserungspfeil. Bau, Ernte, Verarbeitung, Gespräche, Wohnhaus/Park-Wahl und Premiumbestätigung verwenden die bestehenden Spielbefehle.
- Über **Menü** bleiben Stadtteile, Übersicht, Ressourcen, aktuelle Quest und Belohnung, Aufträge, Rathaus, Team, Speichern und Einstellungen erreichbar. Hinweise verschwinden nach vier Sekunden.

## Lager und Fahrer

Der obere Workerplatz ist die Leitung. **Fuhrpark** öffnet rechts die einzeln besetzbaren Fahrerplätze. Ihre Anzahl folgt weiterhin den vorhandenen Ausbaustufen bis zu 24 Plätzen. Ein freier Platz bietet nur Farm-Fahrer bzw. Stadt-Lieferfahrer des passenden Lagertyps an. Die Auswahl zeigt den bisherigen Einsatzort; Versetzen und kompatibles Tauschen verwenden die vorhandene atomare Zuweisungslogik. Bereits zugewiesene Kandidaten und Fahrer unterwegs sind nicht neu auswählbar. Eine laufende Lieferung verhindert Entfernen und Wechsel. Die vorhandenen Versorgungsrouten und Warenbestände wurden nicht geändert.

## Technische Integration und Erhaltung

Die festen Panels sind gespeicherte Hierarchieobjekte in `BurningHillsUI/SafeArea`. Die frühere feste Oberfläche liegt unter `BurningHillsUI/ArchivedFixedInterface` inaktiv vor. Wiederholte Worker- und Fuhrparkkarten verwenden `Assets/Weedtopia/Production/Prefabs/InteractionRow.prefab`; nur diese datenabhängigen Einträge werden zur Laufzeit aus einem Prefab-Pool angezeigt. Sämtliche Panels, Schriften und Porträts verwenden die vorhandene Kunst. Plus, Kreuz und Verbesserungspfeile sind skalierbare Vektorgrafiken.

`BuildingInteractionUi` steuert den Kontext, `WorldPointerGesture` trennt Tippen und Ziehen, `BuildingInspectionMetrics` projiziert die vorhandenen Simulationsformeln ohne den Spielstand zu ändern. `BuildingInteractionAuthoring.Apply` ist ein expliziter, wiederholbar sicherer Installer; es gibt keinen automatischen Szenenneuaufbau beim Import oder Spielstart.

Der Bestandsvergleich umfasst {preservation['baselineFiles']} ursprüngliche Dateien. Keine fehlt. Alle bisherigen Kunstquellen, 251 Gebäude-Wrapper, Katalogzuordnungen, Landschafts-Prefabinstanzen, Domain- und Anwendungssysteme bleiben unverändert. Von bestehenden Dateien wurden nur die Spielszene, drei Unity-Präsentationsskripte und die an die neue Bedienung angepassten Spielfluss-Tests geändert. Alle {preservation['originalSceneDocuments']} ursprünglichen serialisierten Szenenblöcke bleiben vorhanden; an geschützten Gameplay-Komponenten und Prefabinstanzen gibt es keine Änderungen. Zwei UI-Komponenten erhielten die neuen Referenzen.

Vor jedem Unity-Start wurden Prozess und Lock geprüft. Kein Benutzereditor wurde beendet oder parallel gestartet. Ein ausschließlich vom eigenen fehlgeschlagenen Compilerlauf hinterlassener Lock wurde erst nach Prozessprüfung, Zeitabgleich und exklusivem Dateizugriff archiviert; [Nachweis](stale_owned_lock.json). Testspielstände liegen in eigenen Diagnose-/Testpfaden.

## Aktuelle Nachweise

| Prüfung | Ergebnis | Beleg |
| --- | --- | --- |
| EditMode | {edit['passed']}/{edit['total']} bestanden | [XML]({rel(edit['xml'])}), [Lauf]({rel(edit_path)}) |
| PlayMode | {play['passed']}/{play['total']} bestanden | [XML]({rel(play['xml'])}), [Lauf]({rel(play_path)}) |
| Source-Compiler | {len(source['results'])} Assemblies, aktuelle Quellenprüfsummen | [Receipt](../Implementation/Validation/SourceCompile/result.json) |
| Tatsächliche Game View | {len(capture['screenshots'])} Aufnahmen des Play Mode | [Receipt](gameview_receipt.json), [Bildprüfung](visual_review.json), [Galerie](review.html) |
| UI-Überläufe | Keine Befunde bei 1920×1080, 1366×768 / 115 %, 2560×1080 | [Receipt](../ProductionFinish/Evidence/ui_overflow.json) |
| Erhaltung | Keine fehlenden Assets oder unerwarteten Änderungen | [SHA-256-Vergleich](preservation.json) |
| Windows-Testbuild | Development-Build erfolgreich; separater D3D11-Diagnoselauf mit gerenderten Szenarien | [Build](../ProductionFinish/Evidence/development_build.json), [Player](../ProductionFinish/Evidence/player_run.json) |
| Produktions-Preflight | Nur unabhängige Abnahme-Gates offen | [Receipt](../ProductionFinish/Evidence/production_preflight.json) |

Die neun ergänzten PlayMode-Tests prüfen die freie Ansicht, Maus-Wischen in allen vier Bezirken, Maus-/Touch-Tipp und Kamerarückkehr, Touch-Wischen und Pinch ohne Fehlklick, UI-Gestenerfassung, alle zwölf kompatiblen Rollen, Fahrerplätze und Schutz laufender Lieferungen, Upgrade-Projektionen sowie die vollständige Rahmung nach einem sofortigen Wechsel zur höchsten Gebäudestufe. Die 14 bestehenden Spielfluss-Tests wurden erneut ausgeführt; veraltete Erwartungen an den Positionsregler wurden auf die neue Oberfläche angepasst.

Frühe fehlerhafte Läufe sind in den datierten Logs erhalten. Sie zählen nicht als bestanden. Die Bildprüfung führte zu Korrekturen an UI-Mark-Renderern, Kostenbeschriftung, Bauplatztext und der Kamerarahmung neuer Gebäudestufen.

Fingerprint: `{fingerprint}`.

## Offene Abnahme

Die Maus-/Touch-Ereignisse wurden über virtuelle Geräte im Unity Input System geprüft. Dies ersetzt keine Bedienung auf einem physischen Touchgerät. Die aktuelle Game-View-Bildprüfung ist Agentenprüfung, keine persönliche visuelle Freigabe des Benutzers.

Offene Gesamtspiel-Gates: `{', '.join(gates)}`. Zielgeräteprofil, persönlicher Durchspieltest, Audiohörprobe und Audio-Lizenzabnahme sind weiterhin separat erforderlich. Der Development-Testbuild ist keine freigegebene Veröffentlichung. Die bereits erteilte Gebäudeart-Freigabe bleibt gültig.
'''
(OUT / 'REPORT.md').write_text(report, encoding='utf-8')
status = f'''# Burning Hills — aktueller Stand

Stand {utc}. **Freie Spielansicht, Wischsteuerung und kontextuelle Gebäudemenüs integriert und geprüft; Gesamtspiel nicht final abgenommen.**

Die feste Oberfläche ist eingeklappt. Ein Gebäudetipp öffnet links Details, Worker und Ausbauwerte und vergrößert rechts das bestehende Gebäude. Passende Worker werden rechts ausgewählt; Lager trennen Leitung und einzelne Fahrerplätze. Die akzeptierten 251 Gebäude und die natürliche Umgebung bleiben erhalten.

Aktuelle Evidenz: Unity EditMode {edit['passed']}/{edit['total']}, PlayMode {play['passed']}/{play['total']}, {len(capture['screenshots'])} tatsächliche Game-View-Aufnahmen in drei Auflösungen ohne gemeldeten UI-Überlauf, Windows-Testbuild und separater D3D11-Diagnoselauf. Physische Touchbedienung ist noch nicht abgenommen.

[Aktueller Prüfbericht](../InteractionRevision/REPORT.md) · [Spielansichten](../InteractionRevision/review.html) · [Erhaltungsprüfung](../InteractionRevision/preservation.json).

Gesamtfreigabe offen: `{', '.join(gates)}`. Vorherige [Umgebungsprüfung](../EnvironmentRevision/REPORT.md) und [Layoutprüfung](../LayoutRevision/REPORT.md) bleiben historische Evidenz ihres jeweiligen Stands.
'''
(ROOT / 'Docs/ProductionFinish/STATUS.md').write_text(status, encoding='utf-8')
(ROOT / 'Docs/ProductionFinish/VALIDATION_REPORT.md').write_text('# Aktueller Prüfbericht\n\n' + status.split('\n\n', 1)[1], encoding='utf-8')
print(json.dumps({k: v for k, v in delivery.items() if k not in ['editMode', 'playMode']}, indent=2))
