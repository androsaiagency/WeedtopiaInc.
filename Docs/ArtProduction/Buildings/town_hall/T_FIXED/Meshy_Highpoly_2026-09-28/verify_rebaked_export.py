import bpy
import json
import hashlib
from pathlib import Path
from datetime import datetime, timezone
from mathutils import Vector
import bmesh

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'Optimization_v1'
BAKE=OUT/'Rebaked_50000_UVv2'
bpy.ops.wm.open_mainfile(filepath=str(BAKE/'TownHall_50000_Rebaked.blend'))
# Remove working geometry from this temporary scene before checking the exported model.
for obj in list(bpy.context.scene.objects):
    if obj.type=='MESH':
        bpy.data.objects.remove(obj,do_unlink=True)
bpy.ops.import_scene.gltf(filepath=str(BAKE/'TownHall_50000_Rebaked.glb'),merge_vertices=True)
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
stats=[]
for o in objects:
    o.data.calc_loop_triangles()
    bm=bmesh.new()
    bm.from_mesh(o.data)
    stats.append({'name':o.name,'triangles':len(o.data.loop_triangles),'vertices_after_weld':len(o.data.vertices),'boundary_edges':sum(e.is_boundary for e in bm.edges),'non_manifold_edges':sum(not e.is_manifold for e in bm.edges),'uv_layers':len(o.data.uv_layers),'materials':len(o.data.materials)})
    bm.free()
assert sum(s['triangles'] for s in stats)==50000
assert len(objects)==1
scene=bpy.context.scene
center=Vector(scene['comparison_center'])
extent=scene['comparison_extent']
scene.camera.location=center+Vector((.8,-1.6,.8)).normalized()*extent*3
scene.camera.rotation_euler=(center-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(BAKE/'roundtrip_front_3q.png')
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BAKE/'TownHall_50000_Review.blend'))
with (ROOT/'Meshy_AI_Green_Industrial_Buil_0928195308_image-to-3d-texture.glb').open('rb') as f:
    sha=hashlib.file_digest(f,'sha256').hexdigest()
expected=json.loads((OUT/'baseline_report.json').read_text())['sha256']
assert sha==expected
(BAKE/'roundtrip_report.json').write_text(json.dumps({'created_utc':datetime.now(timezone.utc).isoformat(),'objects':stats,'source_sha256_unchanged':True,'glb_roundtrip':'passed_import_and_50000_triangle_assertion','unity_import':'not_run','performance':'not_run','user_acceptance':'pending'},indent=2),encoding='utf-8')
print('ROUNDTRIP_VERIFIED',stats,flush=True)
