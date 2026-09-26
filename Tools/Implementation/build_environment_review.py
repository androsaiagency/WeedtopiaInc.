"""Assemble review evidence from real Game View captures, without changing captured images."""
from pathlib import Path
from PIL import Image,ImageDraw
from html import escape
import json
ROOT=Path(__file__).resolve().parents[2];folder=ROOT/'Docs/EnvironmentRevision'
receipt=json.loads((folder/'gameview_receipt.json').read_text());images=[Path(p) for p in receipt['screenshots']]
for group,prefix,columns,rows in [('building_','buildings',3,2),('','views',2,2)]:
 samples=[p for p in images if p.name.startswith(group)] if group else [p for p in images if not p.name.startswith('building_')]
 for first in range(0,len(samples),columns*rows):
  sheet=Image.new('RGB',(columns*640,rows*390),(21,43,35));draw=ImageDraw.Draw(sheet)
  for i,p in enumerate(samples[first:first+columns*rows]):
   im=Image.open(p)
   if group:im=im.crop((410,105,1510,890))
   im.thumbnail((635,355));x=(i%columns)*640;y=(i//columns)*390
   sheet.paste(im,(x+(640-im.width)//2,y));draw.text((x+5,y+362),p.stem.replace('building_',''),fill=(248,233,196))
  sheet.save(folder/f'review_{prefix}_{first//(columns*rows)+1:02}.png')
cards=''.join(f'<figure data-kind="{"buildings" if p.name.startswith("building_") else "world"}"><a href="Screenshots/{p.name}" target="_blank"><img loading="lazy" src="Screenshots/{p.name}" alt="{escape(p.stem)}"></a><figcaption>{escape(p.stem.replace("_"," "))}</figcaption></figure>' for p in images)
comparisons=''.join(f'<h2>{label}</h2><div class="pair"><figure><img src="Before/{p}"><figcaption>Vorher</figcaption></figure><figure><img src="Screenshots/{p}"><figcaption>Natürliche Umgebung</figcaption></figure></div>' for label,p in [('Gesamtübersicht','03_connected_world.png'),('Stadt','04_City_start.png'),('Farm','04_Farm_start.png'),('Siedlung','04_Settlement_start.png'),('Industrie','04_Industry_start.png')])
html='''<!doctype html><html lang="de"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Burning Hills – natürliche Umgebung</title><style>body{margin:0;background:#112c25;color:#f5e8c8;font:16px system-ui}main{max-width:1560px;margin:auto;padding:30px}h1{font-size:40px}p{max-width:1000px;line-height:1.6}a{color:#edc373}.pair,.gallery{display:grid;grid-template-columns:1fr 1fr;gap:18px}figure{margin:0;background:#234536;border-radius:10px;overflow:hidden}img{display:block;width:100%}figcaption{padding:12px}button{background:#2f583e;color:inherit;border:1px solid #b39c65;border-radius:8px;padding:12px;margin:5px;cursor:pointer}[hidden]{display:none}@media(max-width:900px){.pair,.gallery{grid-template-columns:1fr}}</style><main><h1>Ein grüneres Burning Hills</h1><p>Echte Unity-Game-View-Aufnahmen: üppige Baumgruppen, bepflanzte Höfe, weiche Erdwege, Naturstein und geschwungene Ufer. Die akzeptierten Gebäude und die Anordnung der Bezirke bleiben erhalten. Die Bilder dokumentieren eine Agentenprüfung, keine abschließende Nutzerfreigabe.</p><p><a href="REPORT.md">Prüfbericht</a> · <a href="gameview_receipt.json">Aufnahmebeleg</a> · <a href="preservation.json">Bestanderhaltung</a></p>'''+comparisons+'''<h2>Aktuelle Ansichten</h2><nav><button data-filter="">Alle</button><button data-filter="world">Umgebung und UI</button><button data-filter="buildings">Gebäudezustände</button></nav><div class="gallery">'''+cards+'''</div></main><script>document.querySelectorAll('[data-filter]').forEach(b=>b.onclick=()=>document.querySelectorAll('[data-kind]').forEach(f=>f.hidden=!!b.dataset.filter&&f.dataset.kind!==b.dataset.filter));</script></html>'''
(folder/'comparison.html').write_text(html,encoding='utf-8')
print('Review gallery and contact sheets:',len(images),'actual captures.')
