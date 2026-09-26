"""Bind bakery delivery evidence to final sources, Unity captures, tests and diagnostic build."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/BakeryDelivery'
VALIDATION = ROOT / 'Docs/Implementation/Validation'
EVIDENCE = ROOT / 'Docs/ProductionFinish/Evidence'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

capture, visual, preservation, compiler, overflow = (read(OUT / name) for name in (
    'gameview_receipt.json', 'visual_review.json', 'preservation.json', 'source_compile.json', 'ui_overflow.json'))
assert preservation['passed'] and visual['passed'] and overflow['passed'] and compiler['status'] == 'passed'
fingerprint = capture['fingerprint']
assert visual['fingerprint'] == overflow['fingerprint'] == fingerprint
for assembly in compiler['results']:
    for name, digest in assembly['sources'].items():
        assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == digest, name
for shot in visual['screenshots']:
    assert hashlib.sha256((OUT / 'Screenshots' / shot['file']).read_bytes()).hexdigest() == shot['sha256']
tests = {}
for mode in ('EditMode', 'PlayMode'):
    receipt = read(sorted(VALIDATION.glob(f'*_{mode}_BakeryFinal.json'))[-1])
    xml = ET.parse(receipt['xml']).getroot()
    assert receipt['status'] == 'passed' and xml.attrib['result'] == 'Passed'
    assert receipt['startedUtc'] > compiler['utc']
    tests[mode] = receipt
build, player = (read(EVIDENCE / name) for name in ('development_build.json', 'player_run.json'))
assert build['passed'] and player['diagnosticCompleted'] and build['fingerprint'] == player['fingerprint'] == fingerprint
for name in ('development_build.json', 'player_run.json'):
    (OUT / name).write_bytes((EVIDENCE / name).read_bytes())
profile = read(Path(player['profile']))
assert profile['completed'] and len(profile['measurements']) == 3 and all(m['maxDrawCalls'] > 0 for m in profile['measurements'])
(OUT / 'player_profile.json').write_bytes(Path(player['profile']).read_bytes())
result = dict(utc=datetime.now(timezone.utc).isoformat(), fingerprint=fingerprint,
    bakeryRoads=12, preservedBuildingVariants=251, authoredVehiclePool=60,
    tests=tests, gameViewScreenshots=len(capture['screenshots']), sourceAssemblies=len(compiler['results']),
    preservationPassed=True, agentVisualReviewPassed=True, diagnosticWindowsBuildPassed=True,
    diagnosticPlayerRenderedScenarios=3, releaseAccepted=False, physicalTouchTested=False,
    openReleaseGates=['target_profile', 'human_playthrough', 'audio_listening', 'audio_license'])
(OUT / 'delivery.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
report = f'''# Bäckereien: Straßen und Butterabholung

Stand: {result['utc']}. Änderung integriert und technisch geprüft; keine neue Gesamtspiel-Freigabe.

Alle zwölf Bäckereien sind über gepflasterte Straßen an den Vorplatz der Butterfabrik angeschlossen. Die bisherigen Bäckerei-Förderbänder und ihre Frachtmarkierungen sind nicht mehr sichtbar. Die übrigen Transportverbindungen, insbesondere die Farm-Förderbänder und Lagerfahrer, behalten ihre Funktion.

Der zugewiesene Bäcker fährt mit seinem Lieferwagen leer zur Butterfabrik, lädt dort Butter ein und bringt sie zur eigenen Bäckerei zurück. Die Abholung beginnt automatisch bei höchstens einem Viertel Buttervorrat, sobald Butter verfügbar ist. Ladekapazität und Fahrzeit berücksichtigen Gebäudestufe und passende Worker-Boni. Unterwegs pausiert seine Backarbeit; die Bäckerfigur erscheint erst nach der Rückkehr wieder am Arbeitsplatz. Der Worker kann während einer aktiven Fahrt nicht entfernt oder umverteilt werden. Das Gebäudemenü zeigt Fahrtstatus, Transportkapazität und Dauer je Strecke.

Butter wird bei Abfahrt reserviert und erst beim tatsächlichen Eintreffen in der Fabrik verladen. Reservierungen verhindern doppelt verplante Ware bei mehreren Bäckern. Ein voller Zielpuffer vernichtet keine Ladung: Der zurückgekehrte Bäcker kann Vorräte verarbeiten und damit Platz schaffen. Fahrtphase, Reservierung und Ladung werden gespeichert; Nachladen und Offline-Fortschritt verwenden denselben Simulationsablauf. Die additive Erweiterung behält die bestehende Speicherformat-Version bei.

Die Straßen liegen als zwölf gespeicherte Mesh-Assets in `Assets/Weedtopia/Production/BakeryDelivery/Meshes`. Sie verwenden das vorhandene natürliche Straßenmaterial. Der vorhandene Fahrzeug-Pool wurde um zwölf Instanzen desselben Lieferwagen-Prefabs auf 60 erweitert. Die explizite Editor-Aktion lautet `Weedtopia/Production/Apply Bakery Pickup Roads`; es gibt keine automatische Reparatur beim Projektstart oder Import.

## Aktuelle Prüfungen

- **EditMode {tests['EditMode']['passed']}/{tests['EditMode']['total']} bestanden**, [XML](../Implementation/Validation/{Path(tests['EditMode']['xml']).name}). Neun neue Testfälle prüfen u. a. Hin-/Rückfahrt, tatsächlichen Ladezeitpunkt, fehlenden Bäcker, knappen Fabrikbestand, konkurrierende Abholer, volle Zielpuffer, leere Rückfahrt, Speichern und Offline-Fortschritt.
- **PlayMode {tests['PlayMode']['passed']}/{tests['PlayMode']['total']} bestanden**, [XML](../Implementation/Validation/{Path(tests['PlayMode']['xml']).name}). Zwei zusätzliche Integrationstests prüfen die sichtbaren Fahrtphasen, Bäckeranwesenheit, Straßenzuordnung, gespeicherten Fahrzeug-Pool und erhaltene Farmbänder.
- **{len(compiler['results'])} Assemblies kompiliert**, [Quellprüfsummen](source_compile.json) mit dem aktuellen Dateistand abgeglichen.
- **{len(capture['screenshots'])} echte Unity-Game-View-Aufnahmen**, [Aufnahmebeleg](gameview_receipt.json) und [dokumentierte Sichtprüfung](visual_review.json). Straßenübersicht, westliche Erweiterung, komplette Abholfahrt und Gebäudemenü bei 1920×1080, 1366×768/115 % sowie 2560×1080. [UI-Überlaufprüfung](ui_overflow.json): keine gemeldeten Überläufe in diesen Ansichten.
- **Windows-Development-Build erstellt**, [Buildbeleg](development_build.json). Der separate D3D11-Diagnoseplayer hat drei bestehende Spielszenarien tatsächlich gerendert; [Laufbeleg](player_run.json), [Messdaten](player_profile.json). Das ist ein Desktop-Diagnoselauf, keine Abnahme auf einem Zielgerät und kein manuelles Durchspielen.
- **Erhaltung geprüft:** [Vergleich](preservation.json) aller {preservation['baselineFiles']} ursprünglichen Dateien. Kein ursprüngliches Asset und kein ursprünglicher Szenenblock entfernt. Alle 251 zentrierten Gebäude-Prefabs, Kunstquellen, Kataloge, vorhandenen Fahrzeugreferenzen, Bauplatzpositionen und sonstigen Inhalts-/Balancingdaten erhalten. Geändert wurden die zwölf Bäckereirouten, passende Texte, die erforderliche Logik und deren Tests.

Der erste gezielte Domain-Testlauf hatte einen Fehlschlag: Die vollständige Tutorial-Speicherprobe endete bisher nach 120 simulierten Sekunden. Mit der Hin- und Rückfahrt erreicht sie das Ende nach 126 Sekunden. Das Testbudget wurde auf die bereits im separaten Progressionstest verwendeten 1800 Sekunden angepasst; die Abschluss- und Speicherprüfungen bleiben bestehen. Dieser erste Lauf wird nicht als bestanden gezählt. Maßgeblich sind die oben verlinkten vollständigen Wiederholungsläufe.

![Rückfahrt mit Butter](Screenshots/07_return_status.png)

Fingerprint für Game View, Build und Player: `{fingerprint}`.

Kein Benutzereditor wurde beendet oder parallel gestartet. Physische Touchbedienung sowie `target_profile`, `human_playthrough`, `audio_listening` und `audio_license` bleiben offene Abnahmen. Frühere Berichte belegen jeweils ihren damaligen Stand; dieser Bericht enthält die aktuellen Nachweise für die Bäckereiänderung und die Regressionstests.
'''
(OUT / 'REPORT.md').write_text(report, encoding='utf-8')
status_path = ROOT / 'Docs/ProductionFinish/STATUS.md'
status = status_path.read_text(encoding='utf-8')
start, end = '<!-- bakery-delivery-current -->', '<!-- /bakery-delivery-current -->'
if start in status:
    status = status.split(end, 1)[1].lstrip()
notice = f'''{start}
Aktualisierung {result['utc']}: **Alle zwölf Bäckereien über Straßen angebunden; der Bäcker holt Butter selbst mit dem Lieferwagen ab.** EditMode {tests['EditMode']['passed']}/{tests['EditMode']['total']}, PlayMode {tests['PlayMode']['passed']}/{tests['PlayMode']['total']}, elf aktuelle Game-View-Aufnahmen sowie neuer Windows-Diagnosebuild und gerenderter Playerlauf. Dieser Build enthält auch die vorherige Gebäudezentrierung und Bedienungsrevision. [Aktueller Bäckerei-Prüfbericht](../BakeryDelivery/REPORT.md). Gesamtspiel- und Geräteabnahmen bleiben offen. Die folgenden Angaben und Buildhinweise sind historische Stände.
{end}

'''
status_path.write_text(notice + status, encoding='utf-8')
print(json.dumps({k: v for k, v in result.items() if k != 'tests'}, indent=2))
