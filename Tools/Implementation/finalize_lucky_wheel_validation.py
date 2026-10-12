"""Assemble evidence from completed runs and check current source against the compiler receipt."""
import hashlib
import json
from pathlib import Path
from shutil import copyfile

root=Path('Docs/LuckyWheel')
validation=Path('Docs/Implementation/Validation')
run_names=[
    '20261001_064154_EditMode_LuckyWheelSaveCompatibility',
    '20261001_064626_PlayMode_LuckyWheelRegression',
    '20261001_065826_PlayMode_LuckyWheelUiFinal',
    '20261001_070148_EditMode_LuckyWheelAssetsFinal',
]
runs=[json.loads((validation/(name+'.json')).read_text(encoding='utf-8-sig')) for name in run_names]
assert all(r['status']=='passed' and r['failed']==0 and r['passed']==r['total'] for r in runs)
source=json.loads((root/'source_compile_receipt.json').read_text(encoding='utf-8-sig'))
assert source['status']=='passed' and all(r['exitCode']==0 for r in source['results'])
stale=[]
checked=set()
for assembly in source['results']:
    for path, expected in assembly['sources'].items():
        checked.add(path)
        if hashlib.sha256(Path(path).read_bytes()).hexdigest()!=expected:
            stale.append(path)
assert not stale,stale
copyfile('Docs/ProductionFinish/Evidence/ui_overflow.json',root/'ui_overflow.json')
layout=json.loads((root/'ui_overflow.json').read_text(encoding='utf-8-sig'))
capture=json.loads((root/'gameview_receipt.json').read_text(encoding='utf-8-sig'))
assert layout['passed'] and not layout['issues'] and layout['fingerprint']==capture['fingerprint']
assert len(capture['screenshots'])==12 and all(Path(p).is_file() for p in capture['screenshots'])
preservation=json.loads((root/'preservation_receipt.json').read_text(encoding='utf-8-sig'))
assert preservation['allExistingSceneIdsPreserved'] and preservation['originalContentPreserved']
repeat=json.loads((root/'idempotence_receipt.json').read_text(encoding='utf-8-sig'))
assert repeat['status']=='passed'
receipt=dict(status='passed',userAcceptance=False,sourceAssemblies=len(source['results']),currentSourceFilesChecked=len(checked),
             staleSources=stale,tests=[dict(run=r['run'],passed=r['passed'],total=r['total']) for r in runs],
             screenshots=12,resolutions=layout['resolutions'],overflowIssues=0,fingerprint=capture['fingerprint'],
             preservedInstallerFiles=repeat['preservedFiles'],preservation=preservation)
(root/'completion_receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
report='''# Abschlussprüfung – tägliches Glücksrad

Der implementierte Stand ist automatisch geprüft. Persönliche Spiel- und Grafikabnahme bleibt offen.

| Prüfung | Ergebnis |
|---|---|
| Source-Compiler | 12/12 Assemblies, alle kompilierten Quelldateien entsprechen dem aktuellen Stand |
| Vollständige Spielregel-Tests | 169/169 bestanden, einschließlich 16 Glücksrad-Fällen |
| Vollständige PlayMode-Tests | 56/56 bestanden |
| Betroffene UI nach visuellen Korrekturen | 12/12 bestanden (Glücksrad und Belohnungs-/Inventar-Präsentation) |
| Asset-Referenzen und wiederholter Installer | 2/2 bestanden; 26 bestehende Dateien bytegleich |
| Echte Game View | 12 Aufnahmen, 1920×1080, 1366×768 mit UI-Skalierung 1,15, 2560×1080 und 1080×1920 |
| Layout-Prüfung | 0 Textüberläufe, 0 Aktionen außerhalb des sicheren Bildschirmbereichs |
| Bewahrung | Alle 9.167 vorherigen Szenenobjekt-IDs erhalten, 10 zusätzliche Szenenobjekte; vorhandene Inhaltswerte und Inventar-Prefab unverändert |

Der erste vollständige EditMode-Lauf fand die Besonderheit von Unitys Serialisierung optionaler Objekte sowie einen zu frühen Zeitstempel beim Initialisieren des Rads. Beide Fehler wurden korrigiert; der gesamte EditMode-Lauf wurde danach erfolgreich wiederholt. Anschließende Änderungen betrafen ausschließlich UI-Geometrie, Icon-Clipping, Seltenheitsfarbe und Editor-Beschriftungen. Die betroffenen PlayMode-Tests und die Asset-Prüfung wurden erneut ausgeführt.

Der Gratis-Dreh und die Erneuerung verwenden 00:00 UTC. Das Rad enthält täglich genau zehn verschiedene Item-Gewinne mit gleichen Chancen. Neun optionale zusätzliche Drehs kosten zusammen 9.850 Grinder. Save-Tests prüfen tatsächliches Laden und Speichern, Einzelvergabe, Kostenfolge, Überläufe, Tageswechsel, Uhr-Rückstellung und Rücknahme bei einem Speicherfehler. Pointer-Tests prüfen die obersten Raycast-Treffer auf Radfeldern, Dreh-/Abbruch-/Inventar-Buttons und beiden Einstiegspunkten.

Die Aufnahmen entstanden in einer separaten Diagnose-Speicherdatei unter `Docs/LuckyWheel/capture_save.json`. Nur für das Bild des geleerten Rads wurde dort Testguthaben gesetzt. Benutzer-Spielstände und Produktionskunst wurden dafür nicht geändert. Der gemeinsame Belohnungskarten-Prefab erhielt einen zusätzlichen Icon-Clipbereich; seine GUID, alle bisherigen Objekte und die Original-Sprites bleiben erhalten.

Siehe `completion_receipt.json` für genaue Run-IDs und den Fingerprint, `gameview_receipt.json` für die Bilddateien, `ui_overflow.json` für die Layoutprüfung und `Originals/` für die Ausgangsdateien. Die Original-Testlogs und XML-Ergebnisse liegen unter `Docs/Implementation/Validation`.
'''
(root/'VALIDATION.md').write_text(report,encoding='utf-8')
print(json.dumps(receipt,indent=2))
