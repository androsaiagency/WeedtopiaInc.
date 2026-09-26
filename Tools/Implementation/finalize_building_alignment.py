"""Bind the small visual-placement delivery to the actual source, asset and Unity receipts."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[2]
out = root / 'Docs/BuildingAlignment'
validation = root / 'Docs/Implementation/Validation'
def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))

capture = read(out / 'gameview_receipt.json')
preservation = read(out / 'preservation.json')
visual = read(out / 'visual_review.json')
compiler = read(out / 'source_compile.json')
assert preservation['passed'] and visual['passed'] and compiler['status'] == 'passed'
assert visual['fingerprint'] == capture['fingerprint']
for assembly in compiler['results']:
    for name, digest in assembly['sources'].items():
        assert hashlib.sha256((root / name).read_bytes()).hexdigest() == digest, name
for shot in visual['screenshots']:
    assert hashlib.sha256((out / 'Screenshots' / shot['file']).read_bytes()).hexdigest() == shot['sha256']
tests = {}
for mode in ['PlayMode', 'EditMode']:
    p = sorted(validation.glob(f'*_{mode}_ParcelCenter*.json'))[-1]
    r = read(p); xml = ET.parse(r['xml']).getroot()
    assert r['status'] == 'passed' and xml.attrib['result'] == 'Passed'
    tests[mode] = r
overflow = read(root / 'Docs/ProductionFinish/Evidence/ui_overflow.json')
assert overflow['passed'] and overflow['fingerprint'] == capture['fingerprint']
result = dict(utc=datetime.now(timezone.utc).isoformat(), fingerprint=capture['fingerprint'],
              alignedVariants=preservation['alignedVariants'], gameViewScreenshots=len(capture['screenshots']),
              tests=tests, preservationPassed=True, agentVisualReviewPassed=True,
              releaseAccepted=False, physicalTouchTested=False, playerRebuilt=False)
(out / 'delivery.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
report = f'''# Gebäude mittig auf den Bauplätzen

Stand: {result['utc']}.

Alle 251 produktiven Gebäudevarianten sind nach unten versetzt. Ihre Grundfläche liegt nun mittig auf dem Bauplatz; zuvor lag die vordere untere Spritekante auf dessen Mittelpunkt. Der Versatz beträgt 22 Prozent der sichtbaren Spritebreite und berücksichtigt damit die unterschiedliche Größe der Gebäudestufen. Transparente Bildränder werden nicht mitgemessen.

Die Korrektur ist als lokale Position von `ProductionSpriteVisual` im jeweiligen Prefab gespeichert. `footprintInset` dokumentiert den bereits angewendeten Anteil, damit ein erneuter Aufruf keinen weiteren Versatz addiert und zusätzliche manuelle Offsets erhalten bleiben. Auch die bestehende Importfunktion kennt die neue Grundausrichtung. Es gibt keine zusätzliche Laufzeitverschiebung oder Änderung der Bilddateien.

Der [Datei- und Prefabvergleich](preservation.json) prüft alle {preservation['baselineFiles']} ursprünglichen Dateien. An jedem Gebäude wurden exakt zwei serialisierte Blöcke geändert: die lokale Spriteposition und der gespeicherte Versatz im Spritebaustein. Alle anderen Prefabblöcke, die Szene, Bauplätze, Katalogzuordnungen, Quellbilder, Liefer-/Workeranker und Gameplaydateien bleiben unverändert.

Aktuelle Nachweise:

- **PlayMode {tests['PlayMode']['passed']}/{tests['PlayMode']['total']} bestanden** nach der Prefabkorrektur; [XML](../Implementation/Validation/{Path(tests['PlayMode']['xml']).name}). Anschließend wurde ausschließlich der Editor-Aufnahmeablauf um zwei Auflösungen ergänzt; Laufzeitcode und Prefabs blieben unverändert.
- **EditMode {tests['EditMode']['passed']}/{tests['EditMode']['total']} bestanden**; [XML](../Implementation/Validation/{Path(tests['EditMode']['xml']).name}).
- **12 Assemblies kompiliert**, aktuelle Quellenprüfsummen geprüft; [Compiler](source_compile.json).
- **{len(capture['screenshots'])} tatsächliche Game-View-Aufnahmen**: alle elf Familien, Stadtübersicht, Coffee-Shop-Ruine und Level 5000 sowie Lager bei 1366×768/115 % und 2560×1080. [Aufnahmelauf](gameview_receipt.json), [Bildprüfung](visual_review.json).
- Keine gemeldeten UI-Überläufe in den erfassten Ansichten der drei Auflösungen. Die umfassende Bedienungsprüfung des vorherigen Auftrags bleibt separat dokumentiert.

![Stadtansicht nach der Korrektur](Screenshots/city_overview.png)

Fingerprint: `{capture['fingerprint']}`.

Die Spielszene verwendet die geänderten Prefabs direkt. Der frühere Windows-Diagnosebuild wurde für diese reine Platzierungskorrektur nicht neu erstellt. Physische Touchbedienung und die Gesamtspiel-/Audio-/Zielgeräteabnahmen sind weiterhin offen. Kein Benutzereditor wurde beendet oder parallel gestartet.
'''
(out / 'REPORT.md').write_text(report, encoding='utf-8')
status = root / 'Docs/ProductionFinish/STATUS.md'
current = status.read_text(encoding='utf-8')
notice = f'''Aktualisierung {result['utc']}: **Alle 251 Gebäudevarianten mittig auf den Bauplätzen ausgerichtet.** EditMode {tests['EditMode']['passed']}/{tests['EditMode']['total']}, PlayMode {tests['PlayMode']['passed']}/{tests['PlayMode']['total']} und {len(capture['screenshots'])} neue Game-View-Aufnahmen. [Aktueller Platzierungsbericht](../BuildingAlignment/REPORT.md). Der weiter unten genannte Windows-Build gehört zum vorherigen Bedienungsstand und enthält diese jüngste Platzierung noch nicht.

'''
status.write_text(notice + current, encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k != 'tests'}, indent=2))
