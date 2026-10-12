import json
from pathlib import Path
from datetime import datetime,timezone
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parent
OUT=ROOT/'UnityReview'
OPT=ROOT/'Optimization_v1'
OUT.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',26)
def board(path,items,width=700):
    canvas=Image.new('RGB',(width*len(items),width+60),'#20262b')
    draw=ImageDraw.Draw(canvas)
    for i,(file,title) in enumerate(items):
        draw.text((i*width+15,16),title,font=font,fill='white')
        im=Image.open(file).convert('RGB')
        im.thumbnail((width,width),Image.Resampling.LANCZOS)
        canvas.paste(im,(i*width+(width-im.width)//2,60+(width-im.height)//2))
    canvas.save(path,quality=96)
board(OUT/'texture_comparison.jpg',[(OPT/'Rebaked_50000_UVv2/rebaked_front_3q.png','50.000 Dreiecke | 4K'),(OPT/'game_50000_2K.png','50.000 Dreiecke | 2K')])
board(OUT/'lod_comparison.jpg',[(OPT/'game_50000_2K.png','Nah | 50.000'),(OPT/'game_20000_2K.png','Mittel | 20.000'),(OPT/'game_12000_2K.png','Fern | 12.000')],550)
receipt=json.loads((OUT/'unity_review.json').read_text())
tex=receipt['textures']
def size(part):return sum(t['runtimeBytes'] for t in tex if part(t['path']))/1048576
main2=size(lambda p:'50000' in p and ('_2K' in p or 'MetallicSmoothness' in p))
main4=size(lambda p:'50000' in p and ('_4K' in p or 'MetallicSmoothness' in p))
all2=size(lambda p:'_4K' not in p and '5000_' not in p)
def encoded_mib(t):
    w,h=t['width'],t['height']; total=0
    while True:
        total+=max(1,(w+3)//4)*max(1,(h+3)//4)*16
        if w==1 and h==1:break
        w,h=max(1,w//2),max(1,h//2)
    return total/1048576
encoded2=sum(encoded_mib(t) for t in tex if '50000' in t['path'] and ('_2K' in t['path'] or 'MetallicSmoothness' in t['path']))
encoded4=sum(encoded_mib(t) for t in tex if '50000' in t['path'] and ('_4K' in t['path'] or 'MetallicSmoothness' in t['path']))
report={'utc':datetime.now(timezone.utc).isoformat(),'textures_mib':{'LOD0_4K':main4,'LOD0_2K':main2,'all_3_LODs_2K':all2},'memory_measurement':'Unity Profiler.GetRuntimeMemorySizeLong in Editor; not a dedicated GPU-residency measurement','encoded_texture_data_mib_including_mips':{'LOD0_2K':encoded2,'LOD0_4K':encoded4},'caveat':'100 instances share the same three meshes/materials/textures. Not 100 distinct building asset sets. Editor synchronous render/readback timing is not FPS. The existing pipeline shadow distance is 50m; the overview camera is more than 400m away, so this benchmark does not measure city-wide realtime shadows.','user_approved_50000_4K':True,'new_lods_user_approval':'pending'}
(OUT/'summary.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
lines=['# Rathaus: Unity-Prüfstand mit Detailstufen','',f"Stand: {report['utc']}",'','Der Nutzer hat den 50.000-Dreiecke-Ausgangspunkt visuell freigegeben. Die neuen 2K-Texturen und 20.000/12.000-Stufen sind weitere Prüfkandidaten.','', '## Ergebnisse','',f"- Unity: {receipt['unityVersion']}; GPU: {receipt['gpu']}; CPU: {receipt['cpu']}.",f'- Laufzeitspeicher laut Unity für LOD0-Texturen: {main4:.2f} MiB (4K/4K/2K) bzw. {main2:.2f} MiB (2K/2K/2K).',f'- Alle drei getrennten LOD-Textursätze zusammen: {all2:.2f} MiB.','- Das Modell ist gleichmäßig auf das reservierte 20 × 18 × 22-m-Feld eingepasst, nicht verzerrt.','- Ein Material je Detailstufe; Standalone-Texturen BC7, Normalen BC5; Mipmaps; kein Read/Write.','- Ausgemusterter 6.400-Dreiecke-Versuch: Krone und Turm stark beschädigt, nicht im Prefab verwendet.','','## Render-Vergleich','', '1280 × 720, bestehende URP, synchroner Renderaufruf plus 1-Pixel-GPU-Readback; je fünf Warmläufe und 20 Messungen. **Kein FPS-Test des fertigen Spiels.**','','| Instanzen | LOD | Dreiecke | Sichtbare Instanzen | Median ms | P95 ms |','|---:|---:|---:|---:|---:|---:|']
for s in receipt['samples']:
    lines.append(f"| {s['instances']} | {s['lod']} | {s['triangles']} | {s['visibleInstances']} | {s['medianMs']:.2f} | {s['p95Ms']:.2f} |")
lines+=['','Alle Instanzen teilen dieselben Assets. Speicher für 100 verschiedene Gebäudetexturen ist dadurch nicht gemessen. Das einmalige Rathaus darf ein höheres Nahbudget besitzen als häufige Wohngebäude.','','## Ansehen','', '- `texture_comparison.jpg`: 4K und 2K in Blender.','- `lod_comparison.jpg`: drei Detailstufen bei gleicher Nahansicht; die niedrigeren Stufen sind für kleinere Bildschirmdarstellung bestimmt.','- `single_4K.png`, `single_2K.png`: tatsächliche Unity-Bilder.','- `city_auto_LOD.png`: Unity-Szene mit 100 Instanzen und automatischer LOD-Auswahl.','- Unity-Szene: `Assets/Weedtopia/Art/Buildings/Prototypes3D/TownHall20260928/TownHall_100_Review.unity`.','- Prefab: `Assets/Weedtopia/Art/Buildings/Prototypes3D/TownHall20260928/WT_TownHall_3D_Review_LOD.prefab`.','','Produktion, Katalog und Spielszene wurden nicht ersetzt. Nächste Entscheidung: neue Darstellung und LOD-Übergänge sichten; anschließend gegebenenfalls gezielte Integration und Player-/Zielgerätemessung.']
lines+=['','## Messgrenzen und zusätzliche Nachweise','',f'- Die oben genannten Speicherzahlen stammen aus dem Editor-Profiler, nicht aus einer isolierten GPU-Residency-Messung. Die reine BC5/BC7-Texturmenge mit Mipmaps beträgt rechnerisch {encoded4:.2f} MiB für LOD0 mit 4K-Farb-/Normalenkarte und {encoded2:.2f} MiB für LOD0 mit 2K-Karten. GPU-Treiber-Overhead und ein Player-Profil sind damit nicht gemessen.','- Die bestehende PC_Renderpipeline hat 50 m Schattendistanz; die Übersichtskamera steht über 400 m entfernt. Der Stadtvergleich misst daher keine flächendeckenden Echtzeitschatten.','- Keine Figuren, Partikel, Wirtschaftssimulation oder 100 unterschiedlichen Material-/Textursätze im Prüfstand.','- Native Mesh-Bounds im JSON sind lokale FBX-Koordinaten; importierte Objekttransformationen skalieren und drehen diese in die Welt.','- Methode: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.RenderPipeline.StandardRequest.html']
(OUT/'README.de.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
progress=json.loads((OPT/'PROGRESS.json').read_text())
progress['status']='unity_lod_review_ready_pending_new_visual_review'
progress['steps']['user_review']='50k_4K_approved_by_user'
progress['steps']['unity_import']=receipt['status']
progress['steps']['lod_candidates']='50000_20000_12000; 6400_rejected'
progress['steps']['target_hardware_performance']='editor_render_microbenchmark_complete; player_target_device_pending'
progress['unity_review']='../UnityReview/README.de.md'
(OPT/'PROGRESS.json').write_text(json.dumps(progress,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
