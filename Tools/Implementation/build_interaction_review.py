"""Read-only screenshot gallery and contact sheets for visual QA; source captures remain unchanged."""
from pathlib import Path
import json, html, hashlib
from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/InteractionRevision'
receipt = json.loads((OUT / 'gameview_receipt.json').read_text(encoding='utf-8'))
shots = [Path(p) for p in receipt['screenshots']]
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 18)
sheets = []
for start in range(0, len(shots), 6):
    canvas = Image.new('RGB', (1280, 1164), '#142b2b')
    draw = ImageDraw.Draw(canvas)
    for n, path in enumerate(shots[start:start+6]):
        x, y = (n % 2) * 640, (n // 2) * 388
        with Image.open(path) as source:
            thumbnail = ImageOps.contain(source.convert('RGB'), (632, 350))
        canvas.paste(thumbnail, (x + (640-thumbnail.width)//2, y))
        draw.text((x+8, y+354), path.name, font=font, fill='#f6dfad')
    dest = OUT / f'contact_{start//6+1:02d}.jpg'
    canvas.save(dest, quality=92)
    sheets.append(str(dest))
captions = {
    '1920x1080_01_free_world.png': 'Freie Stadtansicht',
    '1920x1080_02_building.png': 'Gebäudemenü und Kamerafokus',
    '1920x1080_03_matching_workers.png': 'Passende Workerauswahl rechts',
    '1920x1080_04_assigned.png': 'Zugewiesener Worker und Ausbauwerte',
    '1920x1080_06_fleet.png': 'Fuhrpark mit einzelnen Fahrerplätzen',
    '1920_fleet_in_transit.png': 'Geschützter Fahrer unterwegs',
    'coffee_level_5000.png': 'Höchste Gebäudestufe vollständig im Bild',
}
cards = ''.join(f'<figure><a href="Screenshots/{html.escape(p.name)}"><img loading="lazy" src="Screenshots/{html.escape(p.name)}" alt="{html.escape(captions.get(p.name,p.stem))}"></a><figcaption>{html.escape(captions.get(p.name,p.stem))}<small>{html.escape(p.name)}</small></figcaption></figure>' for p in shots)
page = f'''<!doctype html><html lang="de"><meta charset="utf-8"><title>Burning Hills – neue Bedienung</title>
<style>body{{margin:0;background:#102829;color:#f3e4c3;font:17px system-ui}}header,main{{max-width:1500px;margin:auto;padding:28px}}h1{{font-size:36px;color:#eabe6c}}p{{line-height:1.55;max-width:1000px}}.grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(480px,1fr));gap:22px}}figure{{margin:0;background:#1c3839;border:1px solid #947b48;border-radius:12px;overflow:hidden}}img{{width:100%;display:block}}figcaption{{padding:14px}}small{{display:block;color:#b9c9ba;margin-top:6px}}a{{color:#eabe6c}}</style>
<header><h1>Freie Ansicht. Gebäude im Mittelpunkt.</h1><p>Die tatsächliche Unity Game View zeigt die neue Maus- und Touchbedienung: Details links, ausgewähltes Gebäude rechts, passende Workerlisten und getrennte Lagerleitung / Fahrerplätze. Bilder anklicken für Originalauflösung.</p><p>{len(shots)} Aufnahmen · 1920×1080 · 1366×768 mit 115 % UI-Skalierung · 2560×1080. Diagnose-Spielstand; keine persönliche oder physische Geräteabnahme. <a href="REPORT.md">Prüfbericht</a>.</p></header><main><div class="grid">{cards}</div></main></html>'''
(OUT / 'review.html').write_text(page, encoding='utf-8')
(OUT / 'screenshot_index.json').write_text(json.dumps(dict(fingerprint=receipt['fingerprint'],screenshots=[dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in shots],contactSheets=sheets), indent=2), encoding='utf-8')
print(json.dumps(dict(screenshots=len(shots), contactSheets=sheets), indent=2))
