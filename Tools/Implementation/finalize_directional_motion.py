"""Publish the animation handoff only after current source, Unity, visual and preservation evidence agree."""
from pathlib import Path
from datetime import datetime, timezone
import csv
import hashlib
import io
import json
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/DirectionalMotion'
EVIDENCE = ROOT / 'Docs/ProductionFinish/Evidence'
VALIDATION = ROOT / 'Docs/Implementation/Validation'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def stamp(value):
    return datetime.fromisoformat(value.replace('Z', '+00:00'))

capture = read(OUT / 'gameview_receipt.json')
visual = read(OUT / 'visual_review.json')
preservation = read(OUT / 'preservation.json')
compiler = read(OUT / 'source_compile.json')
integration = read(OUT / 'integration.json')
fingerprint = capture['fingerprint']
assert visual['passed'] and preservation['passed'] and compiler['status'] == 'passed'
assert visual['fingerprint'] == fingerprint
assert len(capture['screenshots']) == len(visual['screenshots']) == 65
assert {Path(p).name for p in capture['screenshots']} == {s['file'] for s in visual['screenshots']}
for assembly in compiler['results']:
    for name, expected in assembly['sources'].items():
        assert digest(ROOT / name) == expected, name
for shot in visual['screenshots']:
    assert digest(OUT / 'Screenshots' / shot['file']) == shot['sha256'], shot['file']
for art in read(OUT / 'art_provenance.json'):
    assert digest(ROOT / art['file']) == art['sha256'], art['file']
    assert digest(Path(art['source'])) == art['sha256'], art['source']
sequence = read(OUT / 'sequence_receipt.json')
assert sequence['fingerprint'] == fingerprint
for video in sequence['videos']:
    assert digest(OUT / video['file']) == video['sha256']

tests = {}
for mode, expected in (('EditMode', 80), ('PlayMode', 28)):
    receipt = read(sorted(VALIDATION.glob(f'*_{mode}_DirectionalFinal2.json'))[-1])
    xml = ET.parse(receipt['xml']).getroot()
    assert receipt['status'] == 'passed' and receipt['exitCode'] == 0
    assert xml.attrib['result'] == 'Passed' and int(xml.attrib['passed']) == expected
    assert receipt['total'] == receipt['passed'] == expected and receipt['skipped'] == 0
    assert stamp(receipt['startedUtc']) > stamp(capture['utc'])
    if mode == 'PlayMode':
        regression = [t for t in xml.iter('test-case') if t.attrib.get('name') == 'WesternDirectionsReflectActualGeometryForBothProductionShaderFamilies']
        assert len(regression) == 1 and regression[0].attrib['result'] == 'Passed'
    tests[mode] = receipt
for name in ('ui_overflow.json', 'production_preflight.json', 'development_build.json', 'player_run.json'):
    (OUT / name).write_bytes((EVIDENCE / name).read_bytes())
ui, preflight, build, player = (read(OUT / name) for name in (
    'ui_overflow.json', 'production_preflight.json', 'development_build.json', 'player_run.json'))
assert ui['passed'] and build['passed'] and player['diagnosticCompleted'] and player['exitCode'] == 0
assert ui['fingerprint'] == build['fingerprint'] == player['fingerprint'] == fingerprint
assert stamp(build['utc']) > stamp(tests['PlayMode']['endedUtc'])
assert stamp(player['utc']) > stamp(build['utc'])
assert stamp(preflight['utc']) > stamp(ui['utc'])
assert all(i['code'] == 'ACCEPTANCE_GATE_OPEN' for i in preflight['issues'])
assert {i['objectPath'] for i in preflight['issues']} == {'target_profile', 'human_playthrough', 'audio_listening', 'audio_license'}
profile_path = Path(player['profile'])
profile = read(profile_path)
assert profile['completed'] and len(profile['measurements']) == 3
assert all(m['frames'] == 300 and m['maxDrawCalls'] > 0 for m in profile['measurements'])
for name in ('farm', 'overview', 'upgrade'):
    image = Path(str(profile_path) + '.' + name + '.png')
    assert image.stat().st_size > 100000
    (OUT / ('player_' + name + '.png')).write_bytes(image.read_bytes())
(OUT / 'player_profile.json').write_bytes(profile_path.read_bytes())

register = ROOT / 'Docs/ProductionFinish/VISUAL_ASSET_REGISTER.csv'
backup = OUT / 'visual_asset_register_before.csv'
if not backup.exists():
    backup.write_bytes(register.read_bytes())
original = list(csv.DictReader(io.StringIO(backup.read_text(encoding='utf-8-sig'))))
current = list(csv.DictReader(io.StringIO(register.read_text(encoding='utf-8-sig'))))
atlases = dict(resident_a='ResidentA', resident_b='ResidentB', resident_c='ResidentC', cyclist='Cyclist',
               van='DeliveryVan', swallow='Wildlife', butterfly='Wildlife')
affected = {'vehicle', 'sprite/truck_left', 'sprite/truck_right'}
affected.update('living_hills/' + key + '_' + str(frame) for key in atlases if key != 'van' for frame in (0, 1))
assert [r for r in current if r['id'] not in affected and not r['id'].startswith('directional_motion/')] == [r for r in original if r['id'] not in affected]
rows = [dict(r) for r in original]
for row in rows:
    key = None
    if row['id'].startswith('living_hills/') and row['id'] in affected:
        key = row['id'].split('/')[1][:-2]
    elif row['id'] == 'vehicle':
        key = 'van'
    elif row['id'] in ('sprite/truck_left', 'sprite/truck_right'):
        row['status'] = 'retained_legacy_reference'
        row['owner_note'] = 'Original art preserved; production vans render the DirectionalMotion van animation set. UI truck icon remains unchanged.'
    if key:
        old_asset = row['final_asset']
        row.update(final_asset='Assets/Weedtopia/Production/DirectionalMotion/Art/' + atlases[key] + '.png',
                   source_or_provenance='Docs/DirectionalMotion/art_provenance.json', status='integrated_agent_reviewed',
                   style_qa='Painted elevated perspective, six phases, directional Game View inspection',
                   unity_integration='Existing authored prefab uses DirectionalSpriteAnimator and animation set ' + key,
                   gameview_qa='Docs/DirectionalMotion/visual_review.json; fingerprint ' + fingerprint,
                   owner_note='Previous art retained: ' + old_asset + '; no user or whole-game acceptance inferred')
for key, atlas in atlases.items():
    rows.append(dict(id='directional_motion/' + key, category='directional_animation', scene_or_screen='BurningHills / moving actors',
                     current_asset='Assets/Weedtopia/Production/DirectionalMotion/Animations/' + key + '.asset',
                     final_asset='Assets/Weedtopia/Production/DirectionalMotion/Art/' + atlas + '.png',
                     source_or_provenance='Docs/DirectionalMotion/art_provenance.json', status='integrated_agent_reviewed',
                     style_qa='Six phases; ground: five painted views and three reflected views; air: continuous heading',
                     unity_integration='Prefab-bound distance-driven animation; original simulation and routes preserved',
                     gameview_qa='Docs/DirectionalMotion/visual_review.json; fingerprint ' + fingerprint,
                     owner_note='Agent visual review only; no user or whole-game acceptance inferred'))
with register.open('w', newline='', encoding='utf-8') as stream:
    writer = csv.DictWriter(stream, fieldnames=list(original[0]), quoting=csv.QUOTE_ALL)
    writer.writeheader()
    writer.writerows(rows)
assert [r for r in rows if r['id'] not in affected and not r['id'].startswith('directional_motion/')] == [r for r in original if r['id'] not in affected]
now = datetime.now(timezone.utc).isoformat()
(OUT / 'register_receipt.json').write_text(json.dumps(dict(utc=now, originalRows=len(original), addedRows=7,
    updatedRows=sum(a != b for a, b in zip(rows, original)), unrelatedRowsPreserved=True, fingerprint=fingerprint), indent=2), encoding='utf-8')
result = dict(utc=now, fingerprint=fingerprint, integration=integration, tests=tests, gameViewScreenshots=65,
    preservationPassed=True, agentVisualReviewPassed=True, windowsDiagnosticPassed=True, releaseAccepted=False,
    physicalTouchTested=False, humanPlaythrough=False, openReleaseGates=[i['objectPath'] for i in preflight['issues']])
(OUT / 'delivery.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
report = f'''# Burning Hills: Richtungsanimationen und Lieferwagen

Stand: {now}. Die Bewegungsrevision ist integriert und anhand der unten genannten Prüfungen kontrolliert. Die Gesamtspiel-Abnahme bleibt offen.

Die drei Spaziergängertypen, Radfahrer und Lieferwagen verwenden neue gemalte Animationen aus erhöhter Perspektive. Jeder Bodentyp besitzt fünf gemalte Blickrichtungen mit je sechs Phasen. Drei geometrisch gespiegelte Ansichten ergänzen die acht Richtungen Ost, Nordost, Nord, Nordwest, West, Südwest, Süd und Südost. Rückansichten ersetzen die bisher rückwärts verschobenen Vorderansichten. Schwalben und Schmetterlinge besitzen sechs Flügelphasen und drehen sich stufenlos in ihre Flugrichtung. Insgesamt sind 162 Sprites aus sechs neuen Atlasdateien in sieben Animation-Sets integriert.

Die Geh-, Tret- und Fahrphasen folgen der zurückgelegten Strecke. Bei Stillstand steht das Motiv in einer Ruhephase; Spielpause hält die Bewegung an. Bestehende 50 Umgebungsakteure und alle 60 Instanzen des Lieferwagen-Pools sind an die neuen Sets gebunden. Wiederverwendbare Prefabs und die gespeicherte Szene enthalten die Zuweisungen. Die explizite Editor-Aktion lautet `Weedtopia/Production/Install Directional Motion`; erneutes Ausführen verändert die geprüften Szenen- und Prefabbytes nicht.

Der Wagen ist in der Seitenansicht rund 9,5 Welteinheiten lang und damit etwa doppelt so lang wie eine erwachsene Figur hoch ist. Für Lagerfahrer und Bäcker gilt dieselbe Fahrzeuggröße. Der Bodenkontakt sitzt zwischen den Rädern, der Ladungsindikator oberhalb des Daches. Leerfahrt zur Butterfabrik und beladene Rückfahrt verwenden die tatsächliche jeweilige Fahrtrichtung. Position und Animation folgen nun in jedem Bild dem vorhandenen Bruchteil der Simulationszeit; die 10-Hz-Aktualisierung der Oberfläche begrenzt die Fahrzeugbewegung nicht mehr.

## Sichtprüfung

Die [interaktive Galerie](review.html) erlaubt die Auswahl aller acht Richtungen und sechs Phasen. Ihre 65 Originalbilder stammen aus der tatsächlichen Unity Game View: 48 Richtungsbilder, Stadtstraße, Nord-/Südansicht, zwölf Wegpunkte einer Stadtlieferung einschließlich Kreuzung, Bäcker-Hin- und Rückfahrt sowie Übersicht. Kontaktbögen und vergrößerte Ansichten wurden kontrolliert. Die Bildfolgen verwenden kontrollierte Zeitpunkte und sind keine Echtzeitaufnahme eines manuellen Durchspielens.

In der ersten Sichtprüfung fiel auf, dass das eigene Figurenmaterial `SpriteRenderer.flipX` nicht berücksichtigt. Die westlichen Motive zeigten deshalb trotz richtiger Richtungsauswahl nach rechts. Die Korrektur spiegelt die Geometrie des Artwork-Kindobjekts und funktioniert mit beiden vorhandenen Materialfamilien. Ein zusätzlicher PlayMode-Test prüft diese geometrische Ausrichtung. Der verworfene Aufnahmestand samt Befund bleibt unter `Iterations/UnmirroredShader` erhalten. Die hier verlinkten Bilder wurden nach der Korrektur vollständig neu aufgenommen.

![Lieferwagen auf der Stadtstraße](Screenshots/city_van_05.png)

## Aktuelle Nachweise

| Prüfung | Ergebnis | Nachweis |
| --- | --- | --- |
| Source-Compiler | {len(compiler['results'])} Assemblies; aktuelle Quellenprüfsummen bestätigt | [Compiler](source_compile.json) |
| Unity EditMode | {tests['EditMode']['passed']}/{tests['EditMode']['total']} bestanden, nichts übersprungen | [XML](../Implementation/Validation/{Path(tests['EditMode']['xml']).name}) |
| Unity PlayMode | {tests['PlayMode']['passed']}/{tests['PlayMode']['total']} bestanden, nichts übersprungen | [XML](../Implementation/Validation/{Path(tests['PlayMode']['xml']).name}) |
| Game View | 65 aktuelle Aufnahmen nach der Spiegelungskorrektur | [Aufnahmelauf](gameview_receipt.json), [Sichtprüfung mit Bildprüfsummen](visual_review.json) |
| UI-Nachprüfung | 1920×1080, 1366×768, 2560×1080 ohne gemeldeten Überlauf | [UI-Beleg](ui_overflow.json) |
| Windows-Development-Build | erstellt; drei D3D11-Szenarien mit je 300 Messbildern, zusätzlicher PNG-Nachweis | [Build](development_build.json), [Player](player_run.json), [Messdaten](player_profile.json) |
| Produktions-Preflight | blockiert (Exit-Code 1): vier externe Abnahmen offen; keine Asset-/Material-/Platzhalterbefunde | [Preflight](production_preflight.json) |
| Erhaltungsprüfung | 251 Gebäude-Wrapper und 290 vorhandene Kunst-/Audiodateien bytegleich; keine entfernten Szenenblöcke | [Vergleich](preservation.json) |

Die gezielten neuen Tests decken vollständige Richtungs-/Framezuweisungen, Prefabbindung, Fahrzeugmaßstab, Kamera-Verkürzung, Hin-/Rückrichtung, geometrische Spiegelung beider Shaderfamilien, Bewegung zwischen UI-Aktualisierungen und Stillstand bei Pause ab. Der erneute vollständige Testlauf umfasst außerdem die bestehende Spielmechanik, Worker-Zuweisung, Warenfluss, Bäckerabholung, Quests und Speichern/Laden.

Der Erhaltungsvergleich umfasst {preservation['baselineFiles']} ursprüngliche Dateien und {preservation['originalSceneBlocks']} ursprüngliche Szenenblöcke. Keine ursprüngliche Datei und kein ursprünglicher Szenenblock wurde entfernt. An der vorhandenen Szene wurde ausschließlich die Präsentationskomponente der Umgebungsakteure um Animator-Verweise ergänzt; Gebäude, Bauplätze, Zuweisungen und Straßen bleiben erhalten. Die sechs Akteurs-Prefabs und das Fahrzeug-Prefab wurden gezielt erweitert. Simulation, Logistikdaten, Speicherformat und Kunstquellen bleiben erhalten. Die generierten Atlasdateien wurden einschließlich Alpha bytegleich übernommen; Zuschnitt und Bodenkontaktpunkte sind Unity-Importereinstellungen, keine lokale Pixelbearbeitung.

Die Kunstdateien sind [ResidentA.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/ResidentA.png), [ResidentB.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/ResidentB.png), [ResidentC.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/ResidentC.png), [Cyclist.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/Cyclist.png), [DeliveryVan.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/DeliveryVan.png) und [Wildlife.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/Wildlife.png). [Generierungs-Prompts](art_prompts.json), [Abstandskorrektur des Fahrzeugatlas](van_spacing_prompt.txt), [Herkunft und Prüfsummen](art_provenance.json), [Spritezuschnitte](slice_evidence.json), [erhaltene Generierungszwischenstaende](source_iterations.json), [Assetregister](../ProductionFinish/VISUAL_ASSET_REGISTER.csv).

Fingerprint: `{fingerprint}`. Zielgeräteprofil, manuelles Durchspielen, physische Touchbedienung, Hörprüfung und Audio-Lizenzabnahme bleiben offen. Die Agenten-Sichtprüfung stellt keine persönliche Gestaltungsgenehmigung oder Gesamtspielfreigabe dar. Kein Benutzereditor wurde geschlossen oder parallel gestartet.
'''
(OUT / 'REPORT.md').write_text(report, encoding='utf-8')
status_path = ROOT / 'Docs/ProductionFinish/STATUS.md'
status = status_path.read_text(encoding='utf-8')
start, end = '<!-- directional-motion-current -->', '<!-- /directional-motion-current -->'
if start in status:
    status = status.split(end, 1)[1].lstrip()
status_path.write_text(f'''{start}
Aktualisierung {now}: **Richtungsanimationen für Bewohner, Radfahrer, Tierleben und maßstäblich vergrößerte Lieferwagen integriert.** Acht Bodenrichtungen, sechs Phasen und kontinuierliche Flugrichtung. EditMode 80/80, PlayMode 28/28, 65 aktuelle Game-View-Aufnahmen sowie neuer Windows-Diagnosebuild und gerenderter Playerlauf. [Aktueller Bericht](../DirectionalMotion/REPORT.md) · [Interaktive Bewegungsprüfung](../DirectionalMotion/review.html). Vorhandene Gebäude, Kunst und Spielmechanik erhalten. Gesamtspiel-/Geräteabnahmen bleiben offen; folgende Angaben beschreiben frühere Stände.
{end}

''' + status, encoding='utf-8')
print(json.dumps({k: v for k, v in result.items() if k != 'tests'}, indent=2))
