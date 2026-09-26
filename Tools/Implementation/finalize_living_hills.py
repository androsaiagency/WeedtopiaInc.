"""Finalize only source-bound evidence from the actual Unity runs and reviewed Game View frames."""
from pathlib import Path
from datetime import datetime, timezone
import csv, hashlib, html, io, json, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/LivingHills'
EVIDENCE = ROOT / 'Docs/ProductionFinish/Evidence'
VALIDATION = ROOT / 'Docs/Implementation/Validation'

def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def stamp(s): return datetime.fromisoformat(s.replace('Z', '+00:00'))

capture, visual, preservation, compiler, integration = (read(OUT / n) for n in (
    'gameview_receipt.json', 'visual_review.json', 'preservation.json', 'source_compile.json', 'integration.json'))
assert visual['passed'] and preservation['passed'] and compiler['status'] == 'passed'
fingerprint = capture['fingerprint']; assert visual['fingerprint'] == fingerprint
for assembly in compiler['results']:
    for name, digest in assembly['sources'].items(): assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == digest, name
for shot in visual['screenshots']: assert hashlib.sha256((OUT / 'Screenshots' / shot['file']).read_bytes()).hexdigest() == shot['sha256']
for art in read(OUT / 'art_provenance.json'):
    assert hashlib.sha256((ROOT / art['file']).read_bytes()).hexdigest() == art['sha256']
tests = {}
for mode in ('EditMode', 'PlayMode'):
    r = read(sorted(VALIDATION.glob(f'*_{mode}_LivingHillsFinal*.json'))[-1]); xml = ET.parse(r['xml']).getroot()
    assert r['status'] == 'passed' and xml.attrib['result'] == 'Passed' and stamp(r['startedUtc']) > stamp(integration['utc'])
    tests[mode] = r
for name in ('ui_overflow.json', 'production_preflight.json', 'development_build.json', 'player_run.json'):
    (OUT / name).write_bytes((EVIDENCE / name).read_bytes())
ui, preflight, build, player = (read(OUT / name) for name in ('ui_overflow.json', 'production_preflight.json', 'development_build.json', 'player_run.json'))
assert ui['passed'] and build['passed'] and player['diagnosticCompleted']
assert ui['fingerprint'] == build['fingerprint'] == player['fingerprint'] == fingerprint
assert stamp(preflight['utc']) > stamp(integration['utc']) and all(i['code'] == 'ACCEPTANCE_GATE_OPEN' for i in preflight['issues'])
profile = read(Path(player['profile'])); assert profile['completed'] and all(m['maxDrawCalls'] > 0 for m in profile['measurements'])
(OUT / 'player_profile.json').write_bytes(Path(player['profile']).read_bytes())
register = ROOT / 'Docs/ProductionFinish/VISUAL_ASSET_REGISTER.csv'
backup = OUT / 'visual_asset_register_before.csv'
if not backup.exists(): backup.write_bytes(register.read_bytes())
original = list(csv.DictReader(io.StringIO(backup.read_text(encoding='utf-8-sig'))))
current_rows = list(csv.DictReader(io.StringIO(register.read_text(encoding='utf-8-sig'))))
assert [r for r in current_rows if not r['id'].startswith('living_hills/')] == original, 'Unrelated register changes require review'
rows = list(original)
manifest = read(ROOT / 'Assets/Weedtopia/Production/LivingHills/art_manifest.json')
for entry in manifest['sprites']:
    key = entry['id']; prefab_id = key[:-2] + '_0' if key.endswith('_1') else key
    rows.append(dict(id='living_hills/' + key, category='environment_animation' if key.endswith(('_0', '_1')) else 'environment_landmark',
        scene_or_screen='BurningHills/LivingBurningHills', current_asset=f'Assets/Weedtopia/Production/LivingHills/Prefabs/Living_{prefab_id}.prefab',
        final_asset='Assets/Weedtopia/Production/LivingHills/Art/' + entry['file'], source_or_provenance='Docs/LivingHills/art_provenance.json',
        status='integrated_agent_reviewed', style_qa='Warm copper hills, reclaimed kiln gardens and painted town life; generated alpha preserved',
        unity_integration='Authored prefab instances and shared presentation clock; no simulation changes',
        gameview_qa='Docs/LivingHills/visual_review.json; fingerprint ' + fingerprint,
        owner_note='User visual and whole-game acceptance not inferred from agent review'))
with register.open('w', newline='', encoding='utf-8') as stream:
    writer = csv.DictWriter(stream, fieldnames=list(original[0]), quoting=csv.QUOTE_ALL); writer.writeheader(); writer.writerows(rows)
assert list(csv.DictReader(io.StringIO(register.read_text(encoding='utf-8'))))[:len(original)] == original
(OUT / 'register_receipt.json').write_text(json.dumps(dict(utc=datetime.now(timezone.utc).isoformat(), originalRows=len(original), addedRows=18, unrelatedRowsPreserved=True, gameviewFingerprint=fingerprint), indent=2), encoding='utf-8')
now = datetime.now(timezone.utc).isoformat()
result = dict(utc=now, fingerprint=fingerprint, integration=integration, tests=tests, gameViewScreenshots=len(capture['screenshots']),
    preservationPassed=True, agentVisualReviewPassed=True, windowsDiagnosticPassed=True, releaseAccepted=False,
    physicalTouchTested=False, openReleaseGates=[i['objectPath'] for i in preflight['issues']])
(OUT / 'delivery.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
report = f'''# Burning Hills: Kupferhügel und Stadtleben

Stand: {now}. Integriert und geprüft; die Gesamtspiel-Abnahme bleibt offen.

Die Gestaltung greift den Stadtnamen mit warm leuchtenden, kupferroten Felsstufen, begrünten Hügeln und zwei erhaltenen Ziegelöfen auf. Sie erzählen visuell von einem früheren Industrieort, den sich die Natur zurückholt. Die bestehenden Gebäude und Gärten bleiben der Mittelpunkt. Vierzehn Hügelmotive fassen die Außenbereiche ein; zwei Ortsschilder, sieben kleine Cafégruppen und vier Wimpelgruppen geben den Quartieren zusätzliche Treffpunkte. Fußwege und Lichtungen verbinden die alten Ofenplätze mit den bestehenden Spazierwegen.

Die Stadt hat 21 Spaziergänger und fünf Radfahrer auf gespeicherten Wegen. Sie bewegen sich mit unterschiedlichen Geschwindigkeiten und versetzten Startzeiten und machen an den Endpunkten kurze Pausen. Bewohner werden erst in passenden freigeschalteten, bebauten Bereichen sichtbar. Zwölf Schwalben und zwölf Schmetterlinge bewegen sich über Fluss, Teich und Gärten. Bestehende Laub- und Wasserbewegung bleibt erhalten.

Die Figuren nutzen jeweils zwei gemalte Bewegungsposen. Eine gemeinsame Präsentationssteuerung animiert die vorhandenen Instanzen, berücksichtigt den Pausenzustand und erzeugt keine Objekte während des Spielens. Sie schreibt keine Inventar-, Worker-, Liefer-, Quest- oder Speicherwerte. Dekoration hat keine Collider und fängt keine Gebäudeklicks ab. Alle festen Objekte, Wege, Materialien und Figuren sind unter `BurningHills_Districts/LivingBurningHills` in der Szene gespeichert; wiederholte Motive sind Prefabinstanzen. Die explizite Editor-Aktion heißt `Weedtopia/Production/Author Living Burning Hills` und verändert bei erneutem Aufruf keine Szenenbytes.

## Sichtprüfung und Nachbesserungen

Die neuen Atlasdateien wurden mit dem integrierten ImageGen erzeugt und einschließlich ihres Alphakanals bytegleich übernommen. Spritebereiche werden im Unity-Importer festgelegt; es gab keine lokale Pixelbearbeitung. Die erste Figurenfassung ist unter `Assets/Weedtopia/Production/LivingHills/SourceIterations` erhalten.

In der ersten Game-View-Prüfung wurden die isolierten Ofenplätze mit Wegen verbunden, Cafégrößen angepasst und Geh-/Tretposen überarbeitet. Eine weitere Nahkontrolle zeigte, dass der Sonnenschirm vor Coffee-Shop 1 Mila teilweise verdeckte. Die Sitzgruppe wurde daraufhin versetzt. Die früheren Bilder und Befunde bleiben unter `Iterations` erhalten; maßgeblich sind die unten verlinkten letzten Aufnahmen.

Die [Galerie](review.html) enthält {len(capture['screenshots'])} tatsächliche Game-View-Aufnahmen: Übersicht, Start- und Endbereiche aller vier Bezirke, Rathausgarten, Ortsschild, Caféstraße, Hügel, Ofengarten, Teich, Farmspazierweg, Gebäudemenüs und große Gebäudestufen. Die Sichtprüfung umfasst alle Kontaktbögen sowie vergrößerte Nahansichten. Die Café- und Teichpaare zeigen unterschiedliche Bewegungszeitpunkte; dies ist keine Aufzeichnung eines manuellen Durchspielens.

## Aktuelle technische Nachweise

| Prüfung | Ergebnis | Nachweis |
| --- | --- | --- |
| Unity EditMode | {tests['EditMode']['passed']}/{tests['EditMode']['total']} bestanden | [XML](../Implementation/Validation/{Path(tests['EditMode']['xml']).name}) |
| Unity PlayMode | {tests['PlayMode']['passed']}/{tests['PlayMode']['total']} bestanden | [XML](../Implementation/Validation/{Path(tests['PlayMode']['xml']).name}) |
| Source-Compiler | {len(compiler['results'])} Assemblies; aktuelle Quellenprüfsummen bestätigt | [Compiler](source_compile.json) |
| Game View | {len(capture['screenshots'])} frische Aufnahmen | [Aufnahmelauf](gameview_receipt.json), [Sichtprüfung](visual_review.json) |
| UI | 1920×1080, 1366×768/115 %, 2560×1080; keine gemeldeten Überläufe | [UI-Beleg](ui_overflow.json) |
| Windows-Development-Build | erstellt und in drei D3D11-Diagnoseszenarien gerendert | [Build](development_build.json), [Player](player_run.json), [Messdaten](player_profile.json) |
| Produktions-Preflight | keine neuen Asset-/Material-/Platzhalterbefunde; externe Abnahmen offen | [Preflight](production_preflight.json) |

Die neuen Tests prüfen gespeicherte Prefabs und Wegpunkte, kollisionsfreie Dekoration, Abstand der Spazierwege zu Bauplätzen, unveränderte Szene beim erneuten Installieren, sichtbare Bewegung, Pausieren, unveränderte Spielwerte und gleichbleibende Objektzahl. Die vollständigen bisherigen Tests einschließlich der Bäcker-Abholfahrten wurden erneut ausgeführt.

Der [Erhaltungsvergleich](preservation.json) umfasst {preservation['baselineFiles']} ursprüngliche Dateien und {preservation['originalSceneDocuments']} ursprüngliche Szenenblöcke. Kein ursprüngliches Asset und kein ursprünglicher Szenenblock wurde entfernt. Bestehende Szenenkomponenten, Prefabzuweisungen, Gebäudezentrierung, Kunstquellen, 56 Bauplätze, Logistik, Bäckereistraßen und Spielinhalte bleiben unverändert. Im vorhandenen Welt-Transform wurde lediglich eine neue Kindhierarchie ergänzt. Zusätzlich wurden die Aufnahmefunktion und Tests sowie zwei explizite Editor-Hilfsfunktionen für neue Gartenwege ergänzt.

![Stadtleben](Screenshots/03_cafe_life_a.png)

![Kupferrote Hügel](Screenshots/05_copper_hills.png)

Die finalen neuen Kunstdateien sind [Landmarks.png](../../Assets/Weedtopia/Production/LivingHills/Art/Landmarks.png) und [TownLife_v2.png](../../Assets/Weedtopia/Production/LivingHills/Art/TownLife_v2.png). [Prompts](art_prompts.json), [Bewegungs-Nachbearbeitung](walking_refinement_prompt.txt), [Herkunft und Prüfsummen](art_provenance.json), [Assetregister](../ProductionFinish/VISUAL_ASSET_REGISTER.csv).

Fingerprint: `{fingerprint}`. Die Agentenprüfung ersetzt keine persönliche Gestaltungsgenehmigung. Physische Touchbedienung sowie Zielgeräteprofil, manuelles Durchspielen, Hörprüfung und Audio-Lizenzabnahme bleiben offen. Kein Benutzereditor wurde geschlossen oder parallel gestartet.
'''
(OUT / 'REPORT.md').write_text(report, encoding='utf-8')
cards = ''.join(f'<figure><a href="Screenshots/{html.escape(s["file"])}"><img loading="lazy" src="Screenshots/{html.escape(s["file"])}"></a><figcaption>{html.escape(s["file"])}<p>{html.escape(s["finding"])}</p></figcaption></figure>' for s in visual['screenshots'])
(OUT / 'review.html').write_text('<!doctype html><html lang="de"><meta charset="utf-8"><title>Burning Hills – Landschaft und Stadtleben</title><style>body{margin:30px;background:#17241c;color:#eee9cf;font:16px system-ui}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(420px,1fr));gap:20px}figure{margin:0;background:#25382b;padding:12px;border-radius:10px}img{width:100%;height:auto}p{font-size:14px;line-height:1.5}a{color:#e7c47c}</style><h1>Burning Hills – Landschaft und Stadtleben</h1><p>Echte Unity Game View. Agentenprüfung, keine manuelle Gesamtspiel-Abnahme. <a href="REPORT.md">Prüfbericht</a></p><main>' + cards + '</main></html>', encoding='utf-8')
status_path = ROOT / 'Docs/ProductionFinish/STATUS.md'; status = status_path.read_text(encoding='utf-8')
start, end = '<!-- living-hills-current -->', '<!-- /living-hills-current -->'
if start in status: status = status.split(end, 1)[1].lstrip()
notice = f'''{start}
Aktualisierung {now}: **Burning Hills um kupferrote Hügel, alte Ofengärten, Caféplätze, Spaziergänger, Radfahrer und Tierleben ergänzt.** EditMode {tests['EditMode']['passed']}/{tests['EditMode']['total']}, PlayMode {tests['PlayMode']['passed']}/{tests['PlayMode']['total']}, 23 aktuelle Game-View-Aufnahmen und neuer Windows-Diagnosebuild mit Grafik-Prüflauf. [Aktueller Bericht](../LivingHills/REPORT.md) · [Galerie](../LivingHills/review.html). Bestehende Kunst, Gebäude, Logistik und Bedienung erhalten. Gesamtspiel-/Geräteabnahmen bleiben offen; folgende Angaben beschreiben frühere Stände.
{end}

'''
status_path.write_text(notice + status, encoding='utf-8')
print(json.dumps({k: v for k, v in result.items() if k != 'tests'}, indent=2))
