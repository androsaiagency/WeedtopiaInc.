import json
import struct
from pathlib import Path
from datetime import datetime, timezone
from PIL import Image, ImageDraw, ImageFont, ImageChops, ImageStat

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'Optimization_v1'
BAKE=OUT/'Rebaked_50000_UVv2'
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',28)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',21)
for view in ['front_3q','rear_3q','front']:
    sheet=Image.new('RGB',(1800,970),'#20262b')
    draw=ImageDraw.Draw(sheet)
    paths=[OUT/f'original_{view}.png',BAKE/f'rebaked_{view}.png']
    titles=['Original | 3.335.710 Dreiecke','Kandidat | 50.000 Dreiecke + neue Texturen']
    for i,(p,title) in enumerate(zip(paths,titles)):
        draw.text((i*900+22,19),title,font=font,fill='white')
        im=Image.open(p).convert('RGB').resize((900,900),Image.Resampling.LANCZOS)
        sheet.paste(im,(i*900,65))
    sheet.save(OUT/f'REVIEW_{view}.jpg',quality=96)

for name,box in [('entrance',(355,525,700,1000)),('crown',(350,230,720,540))]:
    w,h=box[2]-box[0],box[3]-box[1]
    sheet=Image.new('RGB',(w*3,h+58),'#20262b')
    draw=ImageDraw.Draw(sheet)
    for i,(p,title) in enumerate([(OUT/'original_front_3q.png','Original'),(OUT/'reduced_50000_front_3q.png','50.000 | alte Texturen'),(BAKE/'rebaked_front_3q.png','50.000 | neue Texturen')]):
        draw.text((i*w+10,14),title,font=small,fill='white')
        sheet.paste(Image.open(p).convert('RGB').crop(box),(i*w,58))
    sheet.save(OUT/f'REVIEW_detail_{name}.png')

base=Image.open(BAKE/'rebaked_front_3q.png').convert('RGB')
roundtrip=Image.open(BAKE/'roundtrip_front_3q.png').convert('RGB')
diff=ImageStat.Stat(ImageChops.difference(base,roundtrip))
with (BAKE/'TownHall_50000_Rebaked.glb').open('rb') as f:
    magic,version,size=struct.unpack('<4sII',f.read(12))
    count,kind=struct.unpack('<II',f.read(8))
    doc=json.loads(f.read(count))
primitives=[p for m in doc['meshes'] for p in m['primitives']]
vertices=sum(doc['accessors'][p['attributes']['POSITION']]['count'] for p in primitives)
triangles=sum(doc['accessors'][p['indices']]['count']//3 for p in primitives)
assert triangles==50000
assert len(doc['materials'])==1
receipt=json.loads((BAKE/'roundtrip_report.json').read_text())
receipt.update({'exported_vertices':vertices,'glb_bytes':size,'roundtrip_render_mean_absolute_rgb_error_0_to_255':diff.mean,'visual_review':{'front':'crest and panel grid restored; small foliage and surface differences remain','rear':'facade lines preserved; small pipe and plant detail loss','raw_decimation':'rejected_all_three_targets_due_to_texture_distortion','rebake_v1':'rejected_poor_UV_packing_and_blur','rebake_UVv2':'candidate_for_user_review_not_final_approval'},'texture_sizes':{'basecolor':[4096,4096],'normal':[4096,4096],'metallic_roughness':[2048,2048]}})
(BAKE/'roundtrip_report.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')

html='''<!doctype html><html lang="de"><meta charset="utf-8"><title>Rathaus – 98,5 % weniger Dreiecke</title><style>
body{margin:0;background:#1b2227;color:#edf4ef;font:16px system-ui}main{max-width:1150px;margin:30px auto;padding:0 22px}h1{font-size:30px}p{line-height:1.6;color:#c9d6ce}select{padding:12px;background:#34473e;color:white;border:1px solid #6a8b78;border-radius:8px}.viewer{position:relative;width:min(85vw,880px);height:min(85vw,880px);margin:auto;overflow:hidden}.viewer img{position:absolute;width:100%;height:100%;object-fit:contain}.viewer #top{clip-path:inset(0 50% 0 0)}input{width:100%;margin:16px 0}.legend{display:flex;justify-content:space-between;margin:18px 0}a{color:#abe2bd}.note{padding:15px;background:#293c32;border-radius:10px}</style><main>
<h1>Rathaus: Original und 50.000-Dreiecke-Kandidat</h1><p>98,50 % weniger Dreiecke. Die ursprüngliche Oberfläche wurde auf eine neue Texturaufteilung übertragen. Bewege den Regler: Links erscheint das Original, rechts die optimierte Variante. Kamera und Beleuchtung sind identisch.</p>
<select id="view" onchange="refresh()"><option value="front_3q">Vorne schräg</option><option value="rear_3q">Hinten schräg</option><option value="front">Frontal</option></select>
<div class="legend"><strong>Original · 3.335.710 Dreiecke</strong><strong>50.000 Dreiecke · neue Texturen</strong></div>
<div class="viewer"><img id="bottom" src="Rebaked_50000_UVv2/rebaked_front_3q.png"><img id="top" src="original_front_3q.png"></div><input type="range" min="0" max="100" value="50" aria-label="Originalanteil" oninput="document.getElementById('top').style.clipPath='inset(0 '+(100-this.value)+'% 0 0)'">
<p class="note">Der GLB-Export wurde erneut in Blender geladen: 50.000 Dreiecke, ein Material, keine offenen oder nicht-manifold Kanten. Kleine Form- und Pflanzendetails weichen ab. Unity, LODs, Texturspeicher und Leistung mit vielen Gebäuden sind noch zu prüfen. Der Kandidat ist noch nicht für das Spiel freigegeben.</p>
<p><a href="REVIEW_front_3q.jpg">Vorderseite nebeneinander</a> · <a href="REVIEW_rear_3q.jpg">Rückseite</a> · <a href="REVIEW_detail_entrance.png">Wappen und Eingang im Detail</a> · <a href="REVIEW_detail_crown.png">Krone im Detail</a></p>
<p><a href="Rebaked_50000_UVv2/TownHall_50000_Rebaked.glb">Aktueller GLB-Kandidat</a> · <a href="Rebaked_50000_UVv2/TownHall_50000_Review.blend">Blender-Prüfszene</a> · <a href="Vergleich.html">Frühere Reduktionsversuche (verworfen)</a></p>
</main><script>function refresh(){let v=document.getElementById('view').value;document.getElementById('top').src='original_'+v+'.png';document.getElementById('bottom').src='Rebaked_50000_UVv2/rebaked_'+v+'.png'}</script></html>'''
(OUT/'REVIEW.html').write_text(html,encoding='utf-8')

progress=json.loads((OUT/'PROGRESS.json').read_text())
progress['status']='visual_comparison_ready_pending_user_review'
progress['updated_utc']=datetime.now(timezone.utc).isoformat()
progress['steps'].update({'reduce_50000':'complete_visual_rejection_due_to_uv_distortion','reduce_25000':'complete_visual_rejection_due_to_uv_distortion','rebake_50000':'v1_rejected_UVv2_ready_for_user_review','comparison_board':'complete','glb_roundtrip':'passed_50000_triangles_one_material_no_boundary_or_nonmanifold_edges'})
progress['current_candidate']='Rebaked_50000_UVv2/TownHall_50000_Rebaked.glb'
progress['review']='REVIEW.html'
(OUT/'PROGRESS.json').write_text(json.dumps(progress,indent=2),encoding='utf-8')
print(json.dumps(receipt,indent=2))
