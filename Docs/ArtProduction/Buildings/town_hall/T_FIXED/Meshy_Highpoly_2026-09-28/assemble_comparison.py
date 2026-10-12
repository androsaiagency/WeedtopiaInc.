"""Assemble unchanged Blender renders into labeled QA contact sheets and a local review page."""
import json
import struct
import hashlib
from pathlib import Path
from datetime import datetime, timezone
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'Optimization_v1'
labels = [('original', 'Original | 3.335.710 Dreiecke'), ('reduced_100000', '100.000 Dreiecke'), ('reduced_50000', '50.000 Dreiecke'), ('reduced_25000', '25.000 Dreiecke')]
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 25)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 19)
for view in ['front_3q', 'rear_3q', 'front']:
    canvas = Image.new('RGB', (1440, 1570), '#20262b')
    draw = ImageDraw.Draw(canvas)
    for i, (label, title) in enumerate(labels):
        x, y = (i % 2) * 720, (i // 2) * 785
        draw.text((x+25, y+15), title, font=font, fill='white')
        im = Image.open(OUT / f'{label}_{view}.png').convert('RGB')
        im.thumbnail((720, 720), Image.Resampling.LANCZOS)
        canvas.paste(im, (x, y+55))
    canvas.save(OUT / f'comparison_{view}.jpg', quality=95)

# Identical unscaled pixel crops: crest, canopy, steps and foliage.
crops = [('Wappen und Eingang', (350, 530, 700, 1000)), ('Krone und Rohre', (375, 230, 725, 700))]
for name, box in crops:
    w, h = box[2]-box[0], box[3]-box[1]
    canvas = Image.new('RGB', (w*4, h+65), '#20262b')
    draw = ImageDraw.Draw(canvas)
    for i,(label,title) in enumerate(labels):
        draw.text((i*w+10, 12), title.split('|')[0].strip(), font=small, fill='white')
        canvas.paste(Image.open(OUT/f'{label}_front_3q.png').convert('RGB').crop(box), (i*w, 60))
    canvas.save(OUT / ('detail_entrance.png' if name.startswith('Wappen') else 'detail_crown.png'))

source = ROOT / 'Meshy_AI_Green_Industrial_Buil_0928195308_image-to-3d-texture.glb'
with source.open('rb') as f:
    source_hash = hashlib.file_digest(f, 'sha256').hexdigest()
baseline = json.loads((OUT/'baseline_report.json').read_text())
reports=[]
for target in [100000,50000,25000]:
    glb=OUT/f'TownHall_{target}_Candidate.glb'
    with glb.open('rb') as f:
        magic, version, length = struct.unpack('<4sII',f.read(12))
        size, kind=struct.unpack('<II',f.read(8))
        doc=json.loads(f.read(size))
    triangles=vertices=0
    for mesh in doc['meshes']:
        for p in mesh['primitives']:
            assert p.get('mode',4)==4
            triangles+=doc['accessors'][p['indices']]['count']//3
            vertices+=doc['accessors'][p['attributes']['POSITION']]['count']
            assert 'TEXCOORD_0' in p['attributes']
    assert length==glb.stat().st_size and magic==b'glTF'
    report=json.loads((OUT/f'reduced_{target}_report.json').read_text())
    assert triangles==report['after']['triangles']
    reports.append({'target':target,'triangles':triangles,'exported_vertices':vertices,'glb_bytes':length,'materials':len(doc.get('materials',[])),'textures':len(doc.get('images',[])),'reduction_percent':100*(1-triangles/baseline['triangles'])})
receipt={'created_utc':datetime.now(timezone.utc).isoformat(),'source_sha256':source_hash,'source_unchanged':source_hash==baseline['sha256'],'variants':reports,'render_settings':'identical cameras, original PBR textures, lighting, resolution, AgX transform','unity_import':'not_run','target_device_performance':'not_run','user_visual_acceptance':'pending','production_assets_changed':False}
assert receipt['source_unchanged']
(OUT/'comparison_validation.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')

html='''<!doctype html><html lang="de"><meta charset="utf-8"><title>Rathaus – Original und vereinfachte Modelle</title><style>
body{margin:0;background:#171d21;color:#edf2f0;font:16px system-ui}main{max-width:1200px;margin:30px auto;padding:0 24px}h1{font-size:28px}p{line-height:1.6;color:#c7d2cc}button,select{padding:12px;background:#34473e;color:white;border:1px solid #6a8b78;border-radius:8px;margin:4px}button.active{background:#4d735e}.viewer{position:relative;width:min(86vw,850px);height:min(86vw,850px);margin:auto;overflow:hidden}.viewer img{position:absolute;width:100%;height:100%;object-fit:contain}.viewer #top{clip-path:inset(0 50% 0 0)}input{width:100%;margin:20px 0}.legend{display:flex;justify-content:space-between}a{color:#a9e4bf}.note{padding:15px;background:#26342e;border-radius:10px}table{width:100%;border-collapse:collapse}td,th{text-align:left;padding:10px;border-bottom:1px solid #3a4940}</style><main>
<h1>Rathaus: Wie viel Geometrie brauchen wir wirklich?</h1><p>Links das Original, rechts die gewählte Vereinfachung. Den Regler bewegen, um die unveränderten Blender-Bilder direkt zu vergleichen. Identische Kamera, Licht und Originaltexturen; noch kein neues Texture-Baking.</p>
<div id="choices"><button onclick="setTarget(100000,this)" class="active">100.000</button><button onclick="setTarget(50000,this)">50.000</button><button onclick="setTarget(25000,this)">25.000 Dreiecke</button><select id="view" onchange="refresh()"><option value="front_3q">Vorne schräg</option><option value="rear_3q">Hinten schräg</option><option value="front">Frontal</option></select></div>
<div class="legend"><strong>Original · 3.335.710 Dreiecke</strong><strong id="label">100.000 Dreiecke</strong></div>
<div class="viewer"><img id="bottom" src="reduced_100000_front_3q.png"><img id="top" src="original_front_3q.png"></div><input type="range" min="0" max="100" value="50" aria-label="Originalanteil" oninput="document.getElementById('top').style.clipPath='inset(0 '+(100-this.value)+'% 0 0)'">
<p class="note">Vergleichskandidaten, keine Spiel-Freigabe. Unity-Import, Texturspeicher, LOD-Wechsel und Leistung mit vielen Gebäuden wurden noch nicht getestet. Das Original und das bestehende Spiel bleiben erhalten.</p>
<h2>Dateien und Messwerte</h2><table><tr><th>Variante</th><th>Reduktion</th><th>GLB</th><th>Download</th></tr>'''
for r in reports:
    html+=f'<tr><td>{r["triangles"]:,} Dreiecke</td><td>{r["reduction_percent"]:.2f} %</td><td>{r["glb_bytes"]/1e6:.1f} MB</td><td><a href="TownHall_{r["target"]}_Candidate.glb">Modell</a> · <a href="TownHall_{r["target"]}_Comparison.blend">Blender</a></td></tr>'
html+='''</table><p><a href="comparison_front_3q.jpg">Vergleichstafel Vorderseite</a> · <a href="comparison_rear_3q.jpg">Rückseite</a> · <a href="detail_entrance.png">Wappen / Eingang</a> · <a href="detail_crown.png">Krone</a></p></main><script>let target=100000;function setTarget(n,b){target=n;document.querySelectorAll('button').forEach(x=>x.classList.remove('active'));b.classList.add('active');refresh()}function refresh(){let v=document.getElementById('view').value;document.getElementById('top').src='original_'+v+'.png';document.getElementById('bottom').src='reduced_'+target+'_'+v+'.png';document.getElementById('label').textContent=target.toLocaleString('de-DE')+' Dreiecke'}</script></html>'''
(OUT/'Vergleich.html').write_text(html,encoding='utf-8')
print(json.dumps(receipt,indent=2))
