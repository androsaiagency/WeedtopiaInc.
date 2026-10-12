"""Join current Unity receipts and generate a self-contained local visual review gallery."""
import html
import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Docs/UiPolishRevision'
VALIDATION = ROOT / 'Docs/Implementation/Validation'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

capture = read(REPORT / 'capture_receipt.json')
preservation = read(REPORT / 'preservation.json')
idempotence = read(REPORT / 'idempotence.json')
art = read(REPORT / 'art_verification.json')
runs = {}
for name, pattern in {'PlayMode': '*_PlayMode_UiPolishFinalRegression.json', 'Assets': '*_EditMode_UiPolishAssetsFinal.json'}.items():
    files = sorted(VALIDATION.glob(pattern))
    if not files:
        raise SystemExit('Missing completed Unity run: ' + name)
    runs[name] = read(files[-1])
    if runs[name]['status'] != 'passed':
        raise SystemExit('Failed Unity run: ' + name)
if not capture['complete'] or len(capture['screenshots']) != 105 or not preservation['passed'] or not idempotence['passed'] or not art['passed']:
    raise SystemExit('An evidence gate is incomplete')
if datetime.fromisoformat(capture['utc'].replace('Z','+00:00')) < max(datetime.fromisoformat(r['endedUtc'].replace('Z','+00:00')) for r in runs.values()):
    raise SystemExit('Final captures must be newer than the completed regression runs')

summary = {'utc': datetime.now(timezone.utc).isoformat(), 'implementedInBothCities': True, 'tests': runs,
           'capture': capture, 'preservation': preservation, 'idempotence': idempotence, 'art':art,
           'physicalPhoneVerified': False, 'apkBuiltForThisRevision': False, 'userVisualAcceptance': False}
(REPORT / 'validation_summary.json').write_text(json.dumps(summary, indent=2, ensure_ascii=False), encoding='utf-8')
names = {'start':'Hauptmenü', 'rocko_name':'Rocko fragt nach dem Namen', 'rocko_intro':'Rockos Einführung',
         'mila_explaining':'Mila erklärt', 'mila_pleased':'Mila freut sich', 'nia_dialogue':'Nia', 'jaro_dialogue':'Jaro',
         'active_quests':'Runde Aufgabenbuttons', 'quest_preview':'Nächster Schritt', 'coffeeshop_checklist':'Detaillierter Coffeeshop-Auftrag',
         'worker_picker':'Worker zuweisen', 'settings':'Einstellungen', 'profile':'Profil', 'new_game':'Neues Spiel',
         'building_inspector':'Gebäude', 'team':'Mein Team', 'journal':'Aufträge', 'inventory':'Inventar', 'wheel':'Glücksrad',
         'stories':'Stadtgeschichten', 'cities':'Städteübersicht'}
cards = []
for screenshot in capture['screenshots']:
    file = Path(screenshot).name
    size, view = Path(file).stem.split('_', 1)
    title = html.escape(names[view])
    cards.append(f'<figure data-size="{size}"><a href="Screenshots/{file}" target="_blank"><img src="Screenshots/{file}" loading="lazy" alt="{title}"></a><figcaption>{title} · {size}</figcaption></figure>')
buttons = '<button data-filter="all" aria-pressed="true">Alle Formate</button>' + ''.join(f'<button data-filter="{size}">{size}</button>' for size in capture['resolutions'])
gallery = '''<!doctype html><html lang="de"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Weedtopia – UI-Review</title>
<style>body{margin:0;background:#0d3232;color:#f4f1d8;font:17px/1.5 system-ui}header,main{max-width:1500px;margin:auto;padding:24px}h1{color:#d3e19c}nav{display:flex;flex-wrap:wrap;gap:12px}button{padding:12px 20px;border:1px solid #b4dbb5;border-radius:24px;background:#215955;color:inherit;cursor:pointer;font:inherit}button[aria-pressed=true]{background:#c1d698;color:#103432}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:24px}figure{margin:0;padding:12px;border:1px solid #87b6a5;border-radius:16px;background:#153e3c}img{display:block;width:100%;max-height:640px;object-fit:contain}figcaption{padding:12px 0}figure[hidden]{display:none}</style>
<header><h1>Weedtopia – Hauptmenü, Aufgaben und Dialoge</h1><p>105 echte Unity Game Views mit einem isolierten Testspielstand: 21 Ansichten in fünf tatsächlichen Renderauflösungen. Bild anklicken, um die volle Auflösung zu sehen. Scrollbare Inhalte zeigen in den Bildern jeweils ihren Anfang; untere Aktionen sind über die jeweilige Scrollfläche erreichbar.</p><p>2340×1080 verwendet zusätzlich simulierte Android-Displaydichte 2,75. Bedienung auf einem physischen Handy und die visuelle Freigabe durch dich stehen aus.</p><nav>BUTTONS</nav></header><main class="grid">CARDS</main>
<script>document.querySelectorAll('[data-filter]').forEach(b=>b.onclick=()=>{document.querySelectorAll('[data-filter]').forEach(x=>x.setAttribute('aria-pressed',x===b));document.querySelectorAll('figure').forEach(f=>f.hidden=b.dataset.filter!=='all'&&f.dataset.size!==b.dataset.filter)})</script></html>'''.replace('BUTTONS',buttons).replace('CARDS','\n'.join(cards))
(REPORT / 'ReviewGallery.html').write_text(gallery, encoding='utf-8')
rows = '\n'.join(f'| {name} | {r["passed"]} / {r["total"]} | [XML](../Implementation/Validation/{Path(r["xml"]).name}) |' for name,r in runs.items())
readme = f'''# Weedtopia – zweite UI-Revision

In Burning Hills und Big Rock City installiert. Unity 6000.3.9f1, 03.10.2026.

## Ergebnis

- Eigenes bildschirmfüllendes Hauptmenü mit gemalter Weedtopia-Stadt, transparentem Weedtopia-Schriftzug und einem einzigen großen Play-Button. Hintergrund verwendet Cover, Logo erhält sein Seitenverhältnis, Play ist eine vektorbasiert gerundete Kapsel. Die leere Spielwelt ist im Startmenü nicht sichtbar.
- Play lädt den vorhandenen Spielstand oder beginnt ohne Spielstand ein neues Spiel. Name mit Rocko, Profil/Avatar und bestätigter Neustart über Einstellungen bleiben erhalten.
- Aktive Aufgaben erscheinen links oben als runde Buttons mit Symbol und Nummer/Erledigt-Markierung. Ein Klick öffnet die kompakte Vorschau mit dem nächsten konkreten Schritt. „Auftrag öffnen“ führt zur vollständigen Checkliste. Zielnavigation und Belohnung verwenden die bestehenden Questdienste.
- Petrol-/Jade-Fenster mit zurückhaltenden hellen Rändern ersetzen die verzogenen goldenen Ornamente. Inhalt erhält einen eigenen maskierten Bereich; Überschrift und Schließen haben reservierten Platz. Buttons haben innere Textabstände. Team, Inventar, Stadtgeschichten, Städteübersicht und Glücksrad verwenden responsive oder scrollbare Inhalte für schmale Ansichten.
- Weltklicks auf Standard-NPCs übergeben direkt die angeklickte Sprecher-ID. Der vorher ausgewählte oder gesperrte Arbeitsplatz entscheidet nicht mehr über den Gesprächspartner. Gesichter links oberhalb der Box schauen nach rechts.
- Normaler Dialog und Einführung werden durch einen Klick auf die gesamte Bildschirmfläche weitergeschaltet und nach dem letzten Abschnitt geschlossen. Weiter- und X-Buttons sind dort ausgeblendet. Die Namenseingabe behält ihre notwendige Bestätigung.

## Prüfungen

| Prüfung | Bestanden | Nachweis |
|---|---:|---|
{rows}

Die PlayMode-Tests umfassen echte Welt-Raycasts auf die Standard-NPCs in beiden Städten, korrekte Identität trotz gesperrter Arbeitsplätze, Klicks an Bildschirmrändern zum Weiterführen/Beenden, runde Aufgabenbuttons mit Vorschau, Play/Save/Profile/Neustart, erreichbare Schließen-Buttons und gescrollte Inventaraktionen. Die komplette Asset-Suite enthält die erneute Installer-Ausführung samt Bytevergleich und die Prüfung aller bestehenden UI-Bindungen.

- [Gesamtreceipt](validation_summary.json) verbindet die aktuellen abgeschlossenen Testläufe und ihre XML-/Logpfade.
- [Galerie](ReviewGallery.html): 105 Game Views in 1920×1080, 1280×720, 844×390, 390×844 und 2340×1080. [Layout-Receipt](capture_receipt.json) enthält die tatsächlich gerenderten Auflösungen und gemeldeten Probleme.
- [Erhaltung](preservation.json): {preservation['protectedPrefabCount']} Welt-/Actor-/Lieferprefabs bytegleich. In jeder Stadt 1.480 Datensätze außerhalb der Canvas-Hierarchie ohne Änderung. Gameplay-Content bleibt gegenüber dem Start dieser zweiten Revision bytegleich.
- [Wiederholbarkeit](idempotence.json): Szenen, gemeinsame UI- und Modul-Prefabs, Titelbilder und Importmetadaten bleiben bei erneutem explizitem Installer-Lauf bytegleich.
- [Art-Prompts und Provenienz](ArtPrompts.md): Hintergrund und Logo wurden mit dem eingebauten ImageGen-Werkzeug erzeugt und unverändert übernommen. Fünf neue UI-Surfaces sind originale deterministische Zeichnungsprimitive. Vorhandene Weltkunst wurde nicht ersetzt.

## Aufbau

`TitleScreen.prefab`, `QuestPreview.prefab`, `QuestShortcut.prefab` und `DialogueStage.prefab` sind gemeinsame authored Prefabs. Die Rahmen/Scrollflächen der vorhandenen Module liegen in deren ursprünglichen Source-Prefabs. Feste Controls werden nicht zur Laufzeit erzeugt. Nur bestehende dynamische Listen bleiben dynamisch.

`ModalFrame` schützt Header und Ecken, `TeamPanelLayout` stellt Roster und Details nebeneinander beziehungsweise untereinander, `ModulePanelLayout` setzt vorhandene Modul-Controls lesbar in scrollbaren Flächen. `CapsuleButtonGraphic` zeichnet den Titel-Play-Button mit einer an die tatsächliche Höhe angepassten Rundung. `QuestTracker` führt von der Vorschau in die Details. `DialoguePanel` besitzt die authored Bildschirm-Klickfläche und Porträtausrichtung.

Explizite, bereits ausgeführte Installation: `Weedtopia > Presentation > Install Polished UI`. Sie verweigert Play Mode/ungespeicherte Szenen, aktualisiert Source-Prefabs vor den Szenen und prüft bestehende Bindungen vor dem Speichern. Erneutes Installieren ist getestet. Alte Menüobjekte bleiben deaktiviert unter `ArchivedUiBeforeExperience`. Der Task-Snapshot liegt in `Baseline`.

## Abnahmegrenzen

Die Aufnahmen und automatischen Trefferprüfungen sind echte Editor-Prüfungen, ersetzen aber keinen Test auf einem physischen Android-Gerät. Bildschirmtastatur, konkrete Displayaussparungen, Touch-Gefühl und Geräteperformance sind noch offen. Kein APK für diese Revision gebaut. Deine visuelle Freigabe ist nicht durch Tests oder Screenshots vorweggenommen.
'''
(REPORT / 'README.md').write_text(readme, encoding='utf-8')
print(json.dumps({'tests':{name:r['passed'] for name,r in runs.items()},'screenshots':len(capture['screenshots']),'report':str(REPORT/'README.md')},ensure_ascii=False))
