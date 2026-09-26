"""Package actual Game View frames as review sequences; never modifies production artwork."""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw
import imageio.v2 as imageio
import numpy as np
ROOT=Path(__file__).resolve().parents[2];DOC=ROOT/'Docs/DirectionalMotion';SHOTS=DOC/'Screenshots'
receipt=json.loads((DOC/'gameview_receipt.json').read_text(encoding='utf-8-sig'))
for start in range(0,8,2):
    sheet=Image.new('RGB',(2880,1300),'#172019');draw=ImageDraw.Draw(sheet)
    for row,direction in enumerate((start,start+1)):
        for phase in range(6):
            im=Image.open(SHOTS/f'direction_{direction}_frame_{phase}.png').crop((240,340,1850,620))
            im.thumbnail((960,190));x=phase%3*960;y=row*650+phase//3*300
            sheet.paste(im,(x,y+35));draw.text((x+12,y+12),f'Direction {direction} / frame {phase}',fill='white')
    sheet.save(DOC/f'directions_{start//2}.jpg',quality=93)
files=[Path(s) for s in receipt['screenshots'] if not Path(s).name.startswith('direction_')]
for start in range(0,len(files),9):
    sheet=Image.new('RGB',(2700,1620),'#172019');draw=ImageDraw.Draw(sheet)
    for i,file in enumerate(files[start:start+9]):
        im=Image.open(file);im.thumbnail((900,500));x=i%3*900;y=i//3*540
        sheet.paste(im,(x,y+30));draw.text((x+10,y+10),file.name,fill='white')
    sheet.save(DOC/f'scene_contact_{start//9}.jpg',quality=91)
videos=[]
for name,frames in [('directions', [SHOTS/f'direction_{d}_frame_{f}.png' for d in range(8) for f in range(6)]),
                    ('city_route',[SHOTS/f'city_van_{f:02}.png' for f in range(12)])]:
    dest=DOC/(name+'.mp4')
    with imageio.get_writer(dest,fps=6,codec='libx264',quality=8,macro_block_size=1) as writer:
        for frame in frames:writer.append_data(np.asarray(Image.open(frame).convert('RGB')))
    videos.append(dict(file=dest.name,frames=len(frames),fps=6,sha256=hashlib.sha256(dest.read_bytes()).hexdigest(),source='Actual Game View frames at controlled diagnostic time samples; not a realtime or human playthrough recording'))
(DOC/'sequence_receipt.json').write_text(json.dumps(dict(fingerprint=receipt['fingerprint'],videos=videos),indent=2),encoding='utf-8')
html='''<!doctype html><html lang="de"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Burning Hills – Bewegungsprüfung</title>
<style>body{margin:28px;background:#18271e;color:#f0ebd8;font:17px system-ui;line-height:1.5}main{max-width:1500px;margin:auto}button,select,input{font:inherit;margin:5px;padding:7px}img,video{width:100%;height:auto;background:#253629}figure{margin:20px 0}a{color:#f0ce83}details{margin:20px 0}h2{font-size:22px}</style>
<main><h1>Burning Hills – Richtung, Animation und Fahrzeuggröße</h1>
<p>Echte Unity Game View. Für den Richtungsvergleich sind bestehende Instanzen vorübergehend nebeneinander aufgestellt. Diese Prüfaufstellung wird nicht in der Spielszene gespeichert. Die Sequenzen verwenden kontrollierte Zeitpunkte und ersetzen keine Echtzeitaufnahme eines manuellen Durchspielens.</p>
<p><a href="REPORT.md">Prüfbericht und Testnachweise</a></p>
<h2>Acht Richtungen, sechs Phasen</h2><label>Richtung <select id="direction"><option>Rechts / Ost</option><option>Oben rechts / Nordost</option><option>Oben / Nord</option><option>Oben links / Nordwest</option><option>Links / West</option><option>Unten links / Südwest</option><option>Unten / Süd</option><option>Unten rechts / Südost</option></select></label>
<button id="play">Phasen abspielen</button><label>Phase <input id="phase" type="range" min="0" max="5" value="0"><output id="label">1 / 6</output></label><img id="frame" src="Screenshots/direction_0_frame_0.png" alt="Aktuelle Richtung und Animationsphase">
<p>Die Bewegung nach links ist auch im eigenen Figurenmaterial geometrisch gespiegelt. Die Tiermotive drehen sich stufenlos mit ihrer Flugrichtung. Die Fahrzeuggröße ist für Lagerfahrer und Bäcker vereinheitlicht.</p>
<h2>Lieferung über die Stadtstraße</h2><video controls preload="metadata" poster="Screenshots/city_van_05.png" src="city_route.mp4"></video><p>Bildfolge entlang einer vorhandenen Lieferroute mit Richtungswechsel an der Kreuzung.</p>
<h2>Bäcker: Hinfahrt und beladene Rückfahrt</h2><img src="Screenshots/baker_outbound.png" alt="Bäcker zur Butterfabrik"><img src="Screenshots/baker_return.png" alt="Bäcker mit Butter auf der Rückfahrt">
<details><summary>Gesamte Richtungsfolge</summary><video controls preload="none" src="directions.mp4"></video></details>
<details><summary>Stadtansichten</summary><img loading="lazy" src="Screenshots/city_street.png"><img loading="lazy" src="Screenshots/north_south_walk.png"><img loading="lazy" src="Screenshots/final_overview.png"></details></main>
<script>const direction=document.getElementById('direction'),phase=document.getElementById('phase'),frame=document.getElementById('frame'),label=document.getElementById('label'),play=document.getElementById('play');let timer=null;function show(){frame.src=`Screenshots/direction_${direction.selectedIndex}_frame_${phase.value}.png`;label.value=`${Number(phase.value)+1} / 6`;}direction.onchange=show;phase.oninput=show;play.onclick=()=>{if(timer){clearInterval(timer);timer=null;play.textContent='Phasen abspielen';}else{timer=setInterval(()=>{phase.value=(Number(phase.value)+1)%6;show()},166.667);play.textContent='Anhalten';}};</script></html>'''
(DOC/'review.html').write_text(html,encoding='utf-8');print('Review sequences packaged:',len(receipt['screenshots']),'source frames')
