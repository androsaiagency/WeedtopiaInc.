"""Transfer the original high-poly appearance onto a new low-poly UV layout."""
import bpy
import json
import time
import sys
from pathlib import Path
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'Optimization_v1'
TARGET = int(sys.argv[sys.argv.index('--')+1])
REVISION = sys.argv[sys.argv.index('--')+2] if len(sys.argv) > sys.argv.index('--')+2 else 'v1'
TEX_SIZE = 2048 if REVISION == 'Game2K' else 4096
BAKE = OUT / (f'Rebaked_{TARGET}' if REVISION == 'v1' else f'Rebaked_{TARGET}_{REVISION}')
BAKE.mkdir(exist_ok=True)
started = time.time()
bpy.ops.wm.open_mainfile(filepath=str(OUT / f'TownHall_{TARGET}_Comparison.blend'))
scene = bpy.context.scene
low = next(o for o in scene.objects if o.type=='MESH')
low.name = f'TownHall_{TARGET}_Rebaked'
low.data.name = low.name
# Simplification changed the tangent basis. Remove imported custom normals before baking a new basis.
bpy.context.view_layer.objects.active=low
bpy.ops.object.select_all(action='DESELECT')
low.select_set(True)
if low.data.has_custom_normals:
    bpy.ops.mesh.customdata_custom_splitnormals_clear()
for p in low.data.polygons:
    p.use_smooth=True
print('UV_UNWRAP_START',flush=True)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=1.1519173, island_margin=.008 if REVISION == 'v1' else .0005, area_weight=.2, correct_aspect=True, scale_to_bounds=False)
bpy.ops.object.mode_set(mode='OBJECT')
print('UV_UNWRAP_DONE',flush=True)
with bpy.data.libraries.load(str(OUT/'Original_Comparison.blend'),link=False) as (src,dst):
    dst.objects=[n for n in src.objects if n=='Mesh_0']
high=dst.objects[0]
scene.collection.objects.link(high)
high.name='Original_Highpoly_BakeSource'
high.hide_render=False
high.hide_set(False)

mat=bpy.data.materials.new(f'TownHall_{TARGET}_BakedPBR')
mat.use_nodes=True
low.data.materials.clear()
low.data.materials.append(mat)
nodes=mat.node_tree.nodes
bsdf=nodes.get('Principled BSDF')
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=8
scene.render.bake.use_selected_to_active=True
scene.render.bake.use_clear=True
scene.render.bake.margin=16
scene.render.bake.cage_extrusion=.012
scene.render.bake.max_ray_distance=.06
scene.render.bake.use_pass_direct=False
scene.render.bake.use_pass_indirect=False
scene.render.bake.use_pass_color=True
bpy.ops.object.select_all(action='DESELECT')
high.select_set(True)
low.select_set(True)
bpy.context.view_layer.objects.active=low
images={}
records=[]
def bake(name, kind, size, color=False):
    t=time.time()
    im=bpy.data.images.new(f'TownHall_{TARGET}_{name}',width=size,height=size,alpha=False)
    im.colorspace_settings.name='sRGB' if color else 'Non-Color'
    node=nodes.new('ShaderNodeTexImage')
    node.image=im
    nodes.active=node
    node.select=True
    print('BAKE_START',name,size,flush=True)
    bpy.ops.object.bake(type=kind)
    im.filepath_raw=str(BAKE/f'{name}.png')
    im.file_format='PNG'
    im.save()
    im.pack()
    images[name]=node
    records.append({'name':name,'size':size,'seconds':time.time()-t})
    print('BAKE_DONE',name,time.time()-t,flush=True)

bake('BaseColor','DIFFUSE',TEX_SIZE,True)
bake('Normal','NORMAL',TEX_SIZE)
bake('Roughness','ROUGHNESS',2048)
# Emission bake transfers the original metallic channel without lighting.
highmat=high.data.materials[0]
hn=highmat.node_tree.nodes
hl=highmat.node_tree.links
hp=next(n for n in hn if n.type=='BSDF_PRINCIPLED')
hout=next(n for n in hn if n.type=='OUTPUT_MATERIAL' and n.is_active_output)
em=hn.new('ShaderNodeEmission')
if hp.inputs['Metallic'].is_linked:
    hl.new(hp.inputs['Metallic'].links[0].from_socket, em.inputs['Color'])
else:
    v=hp.inputs['Metallic'].default_value
    em.inputs['Color'].default_value=(v,v,v,1)
old_source=hout.inputs['Surface'].links[0].from_socket
hl.new(em.outputs[0],hout.inputs['Surface'])
bake('Metallic','EMIT',2048)
hl.new(old_source,hout.inputs['Surface'])
hn.remove(em)
mat.node_tree.links.new(images['BaseColor'].outputs['Color'],bsdf.inputs['Base Color'])
mat.node_tree.links.new(images['Roughness'].outputs['Color'],bsdf.inputs['Roughness'])
mat.node_tree.links.new(images['Metallic'].outputs['Color'],bsdf.inputs['Metallic'])
normal=nodes.new('ShaderNodeNormalMap')
mat.node_tree.links.new(images['Normal'].outputs['Color'],normal.inputs['Color'])
mat.node_tree.links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
high.hide_render=True
high.hide_set(True)
high.select_set(False)
scene.render.engine='BLENDER_EEVEE_NEXT'
bpy.ops.wm.save_as_mainfile(filepath=str(BAKE/f'TownHall_{TARGET}_Rebaked.blend'))
bpy.ops.object.select_all(action='DESELECT')
low.select_set(True)
bpy.context.view_layer.objects.active=low
bpy.ops.export_scene.gltf(filepath=str(BAKE/f'TownHall_{TARGET}_Rebaked.glb'),export_format='GLB',use_selection=True,export_image_format='AUTO',export_yup=True,export_cameras=False,export_lights=False)
from mathutils import Vector
center=Vector(scene['comparison_center'])
extent=scene['comparison_extent']
for name,xyz in {'front_3q':(.8,-1.6,.8),'rear_3q':(-.8,1.6,.8),'front':(0,-2,.45)}.items():
    scene.camera.location=center+Vector(xyz).normalized()*extent*3
    scene.camera.rotation_euler=(center-scene.camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(BAKE/f'rebaked_{name}.png')
    bpy.ops.render.render(write_still=True)
(BAKE/'bake_report.json').write_text(json.dumps({'created_utc':datetime.now(timezone.utc).isoformat(),'target':TARGET,'bakes':records,'seconds':time.time()-started,'cage_extrusion':.012,'max_ray_distance':.06,'status':'pending_visual_review','unity_accepted':False},indent=2),encoding='utf-8')
print('REBAKE_COMPLETE', time.time()-started, flush=True)
