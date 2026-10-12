"""Isolated, resumable mesh reduction experiment. Never modifies the input GLB."""
import bpy
import hashlib
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / 'Meshy_AI_Green_Industrial_Buil_0928195308_image-to-3d-texture.glb'
OUT = ROOT / 'Optimization_v1'
OUT.mkdir(exist_ok=True)
MODE = sys.argv[sys.argv.index('--') + 1]

def write(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2), encoding='utf-8')

def digest(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()

def meshes():
    return [o for o in bpy.context.scene.objects if o.type == 'MESH']

def bounds(objects):
    pts = [o.matrix_world @ Vector(c) for o in objects for c in o.bound_box]
    lo = Vector(tuple(min(p[i] for p in pts) for i in range(3)))
    hi = Vector(tuple(max(p[i] for p in pts) for i in range(3)))
    return lo, hi

def stats(objects):
    result = []
    for o in objects:
        o.data.calc_loop_triangles()
        result.append({'object': o.name, 'vertices': len(o.data.vertices), 'triangles': len(o.data.loop_triangles), 'uv_layers': [u.name for u in o.data.uv_layers], 'materials': [s.material.name if s.material else None for s in o.material_slots]})
    return {'objects': result, 'triangles': sum(o['triangles'] for o in result), 'vertices': sum(o['vertices'] for o in result)}

def aim(o, target):
    o.rotation_euler = (target - o.location).to_track_quat('-Z', 'Y').to_euler()

def setup(objects):
    scene = bpy.context.scene
    lo, hi = bounds(objects)
    center = (lo + hi) / 2
    extent = max(hi - lo)
    scene['comparison_center'] = list(center)
    scene['comparison_extent'] = extent
    world = bpy.data.worlds.new('NeutralComparisonWorld')
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs[0].default_value = (0.09, 0.105, 0.12, 1)
    world.node_tree.nodes['Background'].inputs[1].default_value = 0.5
    for name, xyz, power, color in [('Key',(-1.6,-2,2.5),160,(1,.93,.84)),('Fill',(1.8,-.4,1.5),100,(.8,.9,1)),('Rim',(0,1.8,2),130,(1,1,1))]:
        data = bpy.data.lights.new(name, 'AREA')
        data.energy = power * extent * extent
        data.shape = 'DISK'
        data.size = extent * 1.5
        data.color = color
        obj = bpy.data.objects.new(name, data)
        scene.collection.objects.link(obj)
        obj.location = center + Vector(xyz) * extent
        aim(obj, center)
    data = bpy.data.cameras.new('ComparisonCamera')
    cam = bpy.data.objects.new('ComparisonCamera', data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    data.type = 'ORTHO'
    data.ortho_scale = extent * 1.30
    data.clip_start = .01
    data.clip_end = extent * 100
    scene.render.engine = 'BLENDER_EEVEE_NEXT'
    scene.render.resolution_x = 1100
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'AgX - Medium High Contrast'
    scene.render.threads_mode = 'FIXED'
    scene.render.threads = 8

def render(label):
    scene = bpy.context.scene
    center = Vector(scene['comparison_center'])
    extent = scene['comparison_extent']
    views = {'front_3q': (.8,-1.6,.8), 'rear_3q': (-.8,1.6,.8), 'front': (0,-2,.45)}
    for name, xyz in views.items():
        scene.camera.location = center + Vector(xyz).normalized() * extent * 3
        aim(scene.camera, center)
        scene.render.filepath = str(OUT / f'{label}_{name}.png')
        print('RENDER', label, name, flush=True)
        bpy.ops.render.render(write_still=True)

started = time.time()
if MODE == 'baseline':
    bpy.ops.wm.read_factory_settings(use_empty=True)
    print('IMPORT_SOURCE', flush=True)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE), merge_vertices=True)
    objects = meshes()
    report = stats(objects)
    report.update({'source': str(SOURCE), 'sha256': digest(SOURCE), 'source_bytes': SOURCE.stat().st_size, 'blender_version': bpy.app.version_string, 'images': [{'name': im.name, 'size': list(im.size), 'channels': im.channels} for im in bpy.data.images], 'created_utc': datetime.now(timezone.utc).isoformat(), 'gltf_import_merge_vertices': True})
    setup(objects)
    report['bounds'] = [list(v) for v in bounds(objects)]
    write('baseline_report.json', report)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'Original_Comparison.blend'))
    render('original')
else:
    target = int(MODE)
    label = f'reduced_{target}'
    bpy.ops.wm.open_mainfile(filepath=str(OUT / 'Original_Comparison.blend'))
    objects = meshes()
    before = stats(objects)
    print('REDUCE_START', target, flush=True)
    for o in objects:
        bpy.context.view_layer.objects.active = o
        o.select_set(True)
        mod = o.modifiers.new(f'Comparison_{target}', 'DECIMATE')
        mod.decimate_type = 'COLLAPSE'
        mod.ratio = target / before['triangles']
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
        o.select_set(False)
    after = stats(objects)
    write(f'{label}_report.json', {'target_triangles':target, 'before': before, 'after':after, 'ratio_requested':target / before['triangles'], 'reduction_seconds':time.time()-started, 'texture_strategy':'original UVs and original PBR maps; no rebake; visual comparison candidate only', 'unity_accepted':False, 'user_accepted':False})
    print('REDUCE_DONE', after, flush=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / f'TownHall_{target}_Comparison.blend'))
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT / f'TownHall_{target}_Candidate.glb'), export_format='GLB', use_selection=True, export_image_format='AUTO', export_yup=True, export_cameras=False, export_lights=False)
    render(label)
write(f'{MODE}_complete.json', {'completed_utc':datetime.now(timezone.utc).isoformat(), 'seconds':time.time()-started, 'source_preserved_sha256':digest(SOURCE)})
print('COMPLETE', MODE, time.time()-started, flush=True)
