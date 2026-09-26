from pathlib import Path
from html import escape
import json
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];folder=ROOT/'Docs/LayoutRevision'
receipt=json.loads((folder/'gameview_receipt.json').read_text());images=[Path(x) for x in receipt['screenshots']]
cards=[]
for p in images:
    label=p.stem.replace('_',' ');group='Gebäude' if p.name.startswith('building_') else 'Spielansichten'
    cards.append(f'<figure data-group="{group}"><a href="Screenshots/{p.name}" target="_blank"><img loading="lazy" src="Screenshots/{p.name}" alt="{escape(label)}"></a><figcaption>{escape(label)}</figcaption></figure>')
html='''<!doctype html><html lang="de"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Burning Hills · aktuelle Game View</title><style>body{margin:0;background:#102c2b;color:#f5e6c4;font:16px system-ui}header,main{max-width:1440px;margin:auto;padding:32px}h1{font-size:38px;margin:0 0 10px}p{line-height:1.6;max-width:1000px}a{color:#efbf62}button{background:#244b45;color:inherit;border:1px solid #be9143;border-radius:8px;padding:12px 22px;margin:5px;cursor:pointer}.gallery,.compare{display:grid;grid-template-columns:1fr 1fr;gap:18px}figure{margin:0;background:#193b36;border-radius:12px;overflow:hidden}img{display:block;width:100%}figcaption{padding:12px;font-size:14px}figure[hidden]{display:none}@media(max-width:900px){.gallery,.compare{grid-template-columns:1fr}}</style><header><h1>Burning Hills</h1><p>Aktuelle Aufnahmen aus der tatsächlichen Unity Game View. Farm rechts außerhalb, Stadt in der Mitte, Siedlung nach oben und Industrie nach links. Die vier Gebäudeprototypen und die lokale pixelbewahrende Freistellung wurden ausdrücklich genehmigt. 251 Sprites sind integriert. Diese Galerie ist keine endgültige Freigabe des Gesamtspiels.</p><p><a href="REPORT.md">Prüfbericht</a> · <a href="gameview_receipt.json">Aufnahmebeleg</a> · <a href="visual_review.json">Sichtprüfung</a></p></header><main><h2>Skizze und verbundene Spielwelt</h2><div class="compare"><figure><img src="user_layout.png"><figcaption>Benutzerskizze</figcaption></figure><figure><a href="Screenshots/03_connected_world.png"><img src="Screenshots/03_connected_world.png"></a><figcaption>Aktuelle Gesamtübersicht</figcaption></figure></div><h2>Erste Integration und überarbeitete Ansicht</h2><div class="compare"><figure><img src="BeforeRefinement/04_City_start.png"><figcaption>Erste Integration: zu kleiner Gebäudemaßstab</figcaption></figure><figure><img src="Screenshots/04_City_start.png"><figcaption>Aktuell: größerer Maßstab und gestaltete Grundstücke</figcaption></figure></div><h2>Alle dokumentierten Ansichten</h2><nav><button data-filter="">Alle</button><button data-filter="Spielansichten">Spielansichten</button><button data-filter="Gebäude">Gebäudezustände</button></nav><div class="gallery">'''+''.join(cards)+'''</div></main><script>document.querySelectorAll('[data-filter]').forEach(b=>b.onclick=()=>document.querySelectorAll('[data-group]').forEach(f=>f.hidden=!!b.dataset.filter&&f.dataset.group!==b.dataset.filter));</script></html>'''
(folder/'comparison.html').write_text(html,encoding='utf-8')
# Review sheets are faithful crops of actual rendered screenshots, not substitute renders.
samples=[p for p in images if p.name.startswith('building_')]
for first in range(0,len(samples),6):
    sheet=Image.new('RGB',(1800,1000),(20,45,41));draw=ImageDraw.Draw(sheet)
    for i,p in enumerate(samples[first:first+6]):
        im=Image.open(p);crop=im.crop((440,115,1500,860));crop.thumbnail((590,445))
        x=i%3*600+(600-crop.width)//2;y=i//3*500+5;sheet.paste(crop,(x,y));draw.text((i%3*600+8,i//3*500+470),p.stem.replace('building_',''),fill=(245,227,188))
    sheet.save(folder/f'gameview_buildings_{first//6+1:02d}.png')
print(f'{len(images)} actual screenshots; gallery and {((len(samples)+5)//6)} building review sheets written.')
views=[p for p in images if not p.name.startswith('building_')]
for first in range(0,len(views),4):
    sheet=Image.new('RGB',(1920,1160),(20,45,41));draw=ImageDraw.Draw(sheet)
    for i,p in enumerate(views[first:first+4]):
        im=Image.open(p);im.thumbnail((960,540));x=i%2*960;y=i//2*580;sheet.paste(im,(x,y));draw.text((x+8,y+548),p.stem,fill=(245,227,188))
    sheet.save(folder/f'gameview_ui_{first//4+1:02d}.png')
