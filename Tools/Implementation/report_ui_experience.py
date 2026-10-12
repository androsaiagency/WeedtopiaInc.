"""Create the local UI review gallery and join completed Unity validation receipts."""
import html
import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Docs/UiExperienceRevision'
VALIDATION = ROOT / 'Docs/Implementation/Validation'
capture = json.loads((REPORT / 'capture_receipt.json').read_text(encoding='utf-8-sig'))
preservation = json.loads((REPORT / 'preservation.json').read_text(encoding='utf-8-sig'))
idempotence = json.loads((REPORT / 'idempotence.json').read_text(encoding='utf-8-sig'))
runs = {}
for name, pattern in {
    'EditMode': '*_EditMode_UiExperienceFinalRegression.json',
    'PlayMode': '*_PlayMode_UiExperienceFinalRegression.json',
    'Finale UI-Bedienung': '*_PlayMode_UiExperienceFinalLayout.json',
    'Assets': '*_EditMode_UiExperienceAssetsFinal.json',
}.items():
    paths = sorted(VALIDATION.glob(pattern))
    if not paths:
        raise SystemExit('Missing completed validation: ' + name)
    runs[name] = json.loads(paths[-1].read_text(encoding='utf-8-sig'))
    if runs[name]['status'] != 'passed':
        raise SystemExit('Validation failed: ' + name)
if not capture['complete'] or not preservation['passed'] or not idempotence['passed']:
    raise SystemExit('Capture, preservation or idempotence is incomplete')
asset_history = [json.loads(p.read_text(encoding='utf-8-sig')) for p in sorted(VALIDATION.glob('*_EditMode_UiExperienceAssetsFinal.json'))]
asset_full = next((r for r in reversed(asset_history) if r.get('total') == 46), None)

receipt = {
    'utc': datetime.now(timezone.utc).isoformat(),
    'implementedInBothCities': True,
    'tests': runs,
    'assetFullRegression': asset_full,
    'resolvedAssetFailure': {'cause': 'Eight scroll viewport Images had no sprite in the two scenes', 'repair': 'Existing Weedtopia panel sprite authored on every affected viewport', 'verifiedBy': runs['Assets']['run'], 'status': 'passed'},
    'capture': capture,
    'preservation': preservation,
    'idempotence': idempotence,
    'physicalPhoneVerified': False,
    'androidApkBuiltForThisRevision': False,
    'userVisualAcceptance': False,
}
(REPORT / 'validation_summary.json').write_text(json.dumps(receipt, indent=2, ensure_ascii=False), encoding='utf-8')
names = {
    'start': 'Startmenü', 'rocko_name': 'Rocko fragt nach dem Namen',
    'mila_explaining': 'Mila erklärt', 'mila_pleased': 'Mila freut sich über den Fortschritt',
    'active_quests': 'Aktive Aufgaben', 'coffeeshop_checklist': 'Konkreter Coffeeshop-Auftrag',
    'worker_picker': 'Worker zuweisen', 'settings': 'Einstellungen: Spiel',
    'profile': 'Einstellungen: Profil', 'new_game': 'Neustart bestätigen',
}
cards = []
for path in capture['screenshots']:
    file = Path(path).name
    resolution, view = Path(file).stem.split('_', 1)
    title = names[view]
    cards.append(f'<figure data-size="{resolution}"><a href="Screenshots/{file}" target="_blank"><img src="Screenshots/{file}" loading="lazy" alt="{html.escape(title)}"></a><figcaption>{html.escape(title)} · {resolution}</figcaption></figure>')
buttons = '<button data-filter="all" aria-pressed="true">Alle</button>' + ''.join(f'<button data-filter="{r}" aria-pressed="false">{r}</button>' for r in capture['resolutions'])
gallery = '''<!doctype html><html lang="de"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Weedtopia UI-Prüfung</title>
<style>body{margin:0;background:#082c2a;color:#fff2d3;font:17px/1.5 system-ui}header,main{max-width:1500px;margin:auto;padding:24px}h1{color:#ffca61}nav{display:flex;flex-wrap:wrap;gap:12px}button{padding:12px 20px;border:2px solid #d3a852;border-radius:18px;background:#164b46;color:inherit;cursor:pointer;font:inherit}button[aria-pressed=true]{background:#e9b556;color:#102e29}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:24px}figure{margin:0;padding:12px;border:1px solid #9c8045;border-radius:16px;background:#0c3836}img{display:block;width:100%;max-height:600px;object-fit:contain;background:#031916}figcaption{padding:12px 0}figure[hidden]{display:none}</style>
<header><h1>Weedtopia: Start, Dialoge und Aufgaben</h1><p>Echte Unity Game Views mit isoliertem Testspielstand. Bild anklicken, um es in voller Auflösung zu sehen. Die Zahlen im Coffeeshop-Beispiel stammen aus einem kontrollierten Testfall. 2340×1080 verwendet zusätzlich simulierte Android-Displaydichte 2,75; die übrigen Formate Dichte 1.</p><p>50 Aufnahmen, fünf tatsächliche Renderauflösungen, keine gemeldeten Textüberläufe oder Aktionen außerhalb der Safe Area. Bedienung auf einem physischen Handy und deine visuelle Abnahme stehen noch aus.</p><nav>BUTTONS</nav></header><main class="grid">CARDS</main>
<script>document.querySelectorAll('[data-filter]').forEach(b=>b.onclick=()=>{document.querySelectorAll('[data-filter]').forEach(x=>x.setAttribute('aria-pressed',x===b));document.querySelectorAll('figure').forEach(f=>f.hidden=b.dataset.filter!=='all'&&f.dataset.size!==b.dataset.filter)})</script></html>'''.replace('BUTTONS', buttons).replace('CARDS', '\n'.join(cards))
(REPORT / 'ReviewGallery.html').write_text(gallery, encoding='utf-8')

test_rows = '\n'.join(f'| {name} | {r["passed"]} / {r["total"]} | [XML](../Implementation/Validation/{Path(r["xml"]).name}) |' for name, r in runs.items())
readme = f'''# Weedtopia – Startmenü, Dialogführung, Profil und Questführung

In Burning Hills und Big Rock City installiert. Datum: 03.10.2026. Unity: 6000.3.9f1.

## Sichtbares Ergebnis

- Das Startmenü zeigt WEEDTOPIA, das vorhandene Weedtopia-Artwork und einen großen Play-Button. Play lädt den vorhandenen Spielstand; ohne Spielstand beginnt der neue Namensdialog mit Rocko.
- Rocko fragt nach einem Namen mit 2–24 Zeichen, bevor das Tutorial fortgesetzt werden kann. Bestehende Spielstände behalten ihren Namen und bekommen bei Bedarf den Standardavatar.
- Einstellungen enthält die Tabs Spiel und Profil. Im Profil werden Name und einer von acht Avataren gemeinsam gespeichert, ohne Stadtfortschritt zurückzusetzen.
- Neues Spiel steht im Tab Spiel. Der Bestätigungsdialog kann abgebrochen werden. Vor dem Neustart wird der bisherige Stand als eigene `*.before_restart_*.json`-Datei gesichert, die spätere Autosaves nicht überschreiben.
- NPC-Gespräche verwenden eine gemeinsame untere Dialogbox. Das Porträt sitzt links darüber. Acht vorhandene Begrüßungsporträts sowie je acht erklärende und erfreute Varianten sind eingebunden. 32 Tutorial-Quests haben eigene Gesprächs- und Erfolgstexte in natürlicher NPC-Sprache; Handlungsanweisungen stehen in der Questliste.
- Aktive Tutorial-, Stadtprojekt-, angenommene Story- und VIP-Aufgaben sind oben links als Buttons erreichbar. Die Details nennen jeden notwendigen Schritt mit aktuellem Fortschritt, erledigte Punkte, den nächsten Schritt und die Belohnungsabholung. Ein Zielpunkt öffnet das passende Gebäude beziehungsweise den passenden Auftrag.
- Das Worker-Fenster hat einen eigenen großen Schließen-Button unterhalb der globalen Einstellungen. Während der Auswahl wird der darunterliegende Inspektor ausgeblendet. Globale Menüs schließen den Inspektor und die Worker-Auswahl und bringen danach die Navigation wieder zurück.

## Der unklare Coffeeshop-Schritt

Der bestehende Stadtfest-Plan verlangt in diesem Abschnitt **Coffeeshop 2 (`coffee_02`)** auf **Stufe 5**, eine eingesetzte **Leitung** für den automatischen Betrieb sowie **360 verkaufte Blüten seit Abschnittsbeginn**. Andere besetzte Coffeeshops ersetzen die konkrete Gebäudevoraussetzung nicht. Die Liste liest dieselben Bedingungen und Zähler wie die bestehende Belohnungsprüfung.

Das Aufnahmebeispiel simuliert vier Coffeeshops, davon drei auf Stufe 5 und besetzt. Coffeeshop 2 ist Stufe 1 und unbesetzt. Die neue Liste zeigt `1 / 5`, `0 / 1` und `100 / 360`. Dieses Beispiel greift nicht auf deinen persönlichen Spielstand zu.

## Prüfnachweise

| Prüfung | Bestanden | Nachweis |
|---|---:|---|
{test_rows}

- [Gesamtreceipt](validation_summary.json): aktuelle abgeschlossene Läufe mit Log- und XML-Pfaden.
- [Aufnahmegalerie](ReviewGallery.html): 50 echte Game Views in 1920×1080, 1280×720, 844×390, 390×844 und 2340×1080. Das letzte Format verwendet simulierte Android-Displaydichte 2,75, um die Mindestgrößen auf einem hochauflösenden Handy zu prüfen.
- [Layout-Receipt](capture_receipt.json): tatsächliche Renderauflösungen geprüft; keine gemeldeten Textüberläufe oder Aktionen außerhalb der Safe Area.
- [Erhaltung](preservation.json): 1.480 Datensätze außerhalb der UI pro Szene unverändert verglichen. Lediglich die erlaubte Referenz des Intro-Porträts wurde auf das gemeinsame Dialog-Prefab umgebunden. Gameplay-Daten und Questvoraussetzungen unverändert; nur Textinhalte geändert.
- [Wiederholbarkeit](idempotence.json): erneuter expliziter UI-Installer lässt Szenen, gemeinsame Prefabs und Atlas-Metadaten bytegleich.

PlayMode prüft unter anderem neuen Start/Namenseingabe, Fortsetzen, Profilwechsel samt Save-Roundtrip, Abbrechen/Bestätigen des Neustarts, passende Mila-Pose samt Erfolgstext, Raycast-Erreichbarkeit der Schließen-Buttons, Worker-Auswahl/Einstellungen und Abbrechen der Premium-Bestätigung. EditMode prüft alte Spielstände, fehlgeschlagenes Profil-Speichern mit Rollback, dauerhaftes Neustart-Archiv und den Mehr-Coffeeshop-Fall.

Die vollständige Asset-Suite erreichte zuerst 45 von 46 bestandene Tests. Die einzige fehlgeschlagene Prüfung meldete acht Scroll-Hintergründe ohne Sprite in den beiden Szenen. Diese wurden im expliziten Installer mit dem vorhandenen Weedtopia-Panel-Sprite verbunden. Anschließend bestanden alle oben aufgeführten gezielten Asset- und Produktionsprüfungen, einschließlich derselben zuvor fehlgeschlagenen Preflight-Prüfung und der Installer-Wiederholung. Der Gesamtlauf wurde nach dieser begrenzten Sprite-Korrektur nicht erneut ausgeführt; der originale Lauf und die konkrete Korrektur sind im Gesamtreceipt enthalten. Die unabhängigen bisherigen Release-Abnahmegates bleiben offen.

## Architektur und Erhaltung

Statische Oberflächen sind in den Szenen und den wiederverwendbaren Prefabs `DialogueStage.prefab` und `QuestRow.prefab` authored. Laufzeitobjekte sind auf die dynamischen, gepoolten Listenzeilen beschränkt. `QuestGuidance` bildet vorhandene Application-Dienste auf lesbare Anweisungen ab und hält keinen zweiten Questzustand. `PlayerProfile` teilt Validierungsregeln zwischen Tutorial, Einstellungen und Persistence. Der explizite Installer befindet sich unter `Weedtopia > Presentation > Install UI Experience`; die Installation ist bereits erfolgt.

Vorherige UI-Objekte liegen deaktiviert unter `ArchivedUiBeforeExperience`. Ein Task-Start-Snapshot der betroffenen Dateien liegt in `Baseline`; der erste Porträtatlas wurde vor der Bildüberarbeitung in `ArtSource` gesichert. Vorhandene Weltobjekte, Gebäude, Kataloge, Worker-/Liefer-/Questlogik und Autosave bleiben erhalten.

Der Porträtatlas wurde mit dem eingebauten ImageGen-Werkzeug erstellt und anschließend für saubere Zellabstände überarbeitet. Der Import verwendet tatsächliche Quelldimensionen und eine kleine Schutzkante; GUID und Sprite-Referenzen bleiben stabil. [Bildprovenienz und Prompt](PortraitPrompt.md).

`UiDisplayMetrics` rechnet Canvas-Einheiten mit der gemeldeten mobilen Displaydichte um. Neue Dialog-/Aufgabentexte verwenden mindestens 18 Display-Einheiten, Schließen/Navigation mindestens 48 und Play mindestens 64 Einheiten Höhe. Damit werden hochauflösende Telefone beim Layout berücksichtigt. Bei nicht gemeldeter DPI wird Dichte 1 verwendet; die tatsächliche Geräteprüfung bleibt offen. Grundlagen: [Unity Screen.dpi](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/screen/dpi), [Android-Displaydichte](https://developer.android.com/training/multiscreen/screendensities) und [Android-Trefferflächen](https://developer.android.com/guide/topics/ui/accessibility/views/apps-views).

## Offene Abnahmegrenzen

Kein APK für diese Revision gebaut und kein physisches Android-Gerät bedient. Android-Bildschirmtastatur, Aussparungen eines konkreten Geräts, echte Touch-Bedienung und Geräteperformance sind noch nicht geprüft. Die Game Views und Raycast-Tests ersetzen diese Prüfungen nicht. Die visuelle Freigabe des Designs und der neuen Mimik erfolgt durch dich.
'''
(REPORT / 'README.md').write_text(readme, encoding='utf-8')
print(json.dumps({'tests': {name: r['passed'] for name, r in runs.items()}, 'screenshots': len(capture['screenshots']), 'report': str(REPORT / 'README.md')}, ensure_ascii=False))
