import bpy
import json
import sys
from pathlib import Path
from mathutils import Vector
import numpy as np

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'Optimization_v1'
PROJECT=next(p for p in ROOT.parents if (p/'ProjectSettings').exists())
DEST=PROJECT/'Assets/Weedtopia/Art/Buildings/Prototypes3D/TownHall20260928/Source'
DEST.mkdir(parents=True,exist_ok=True)
target=int(sys.argv[sys.argv.index('--')+1])
if target==50000:
    source=OUT/'Rebaked_50000_UVv2/TownHall_50000_Rebaked.blend'
else:
    source=OUT/f'Rebaked_{target}_Game2K/TownHall_{target}_Rebaked.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
scene=bpy.context.scene
obj=next(o for o in scene.objects if o.type=='MESH' and not o.hide_render)
obj.name=f'TownHall_{target}'
bpy.ops.object.select_all(action='DESELECT')
obj.hide_set(False)
obj.select_set(True)
bpy.context.view_layer.objects.active=obj
nodes=obj.data.materials[0].node_tree.nodes
bsdf=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
def input_image(socket):
    n=socket.links[0].from_node
    if n.type=='NORMAL_MAP': n=n.inputs['Color'].links[0].from_node
    return n.image
base=input_image(bsdf.inputs['Base Color'])
normal=input_image(bsdf.inputs['Normal'])
rough=input_image(bsdf.inputs['Roughness'])
metal=input_image(bsdf.inputs['Metallic'])

def save_copy(im,name,size):
    copy=im.copy()
    copy.name=name
    if copy.size[0]!=size: copy.scale(size,size)
    copy.filepath_raw=str(DEST/(name+'.png'))
    copy.file_format='PNG'
    copy.save()
    return copy

if target==50000:
    for im,name in [(base,'BaseColor'),(normal,'Normal')]:
        save_copy(im,f'TownHall_{target}_{name}_4K',4096)
base2=save_copy(base,f'TownHall_{target}_BaseColor_2K',2048)
norm2=save_copy(normal,f'TownHall_{target}_Normal_2K',2048)
metal2=metal.copy()
rough2=rough.copy()
metal2.scale(2048,2048)
rough2.scale(2048,2048)
m=np.empty(2048*2048*4,dtype=np.float32)
r=np.empty_like(m)
metal2.pixels.foreach_get(m)
rough2.pixels.foreach_get(r)
packed=np.zeros((2048*2048,4),dtype=np.float32)
packed[:,0]=m.reshape(-1,4)[:,0]
packed[:,3]=1-r.reshape(-1,4)[:,0]
im=bpy.data.images.new(f'TownHall_{target}_MetallicSmoothness',width=2048,height=2048,alpha=True)
im.colorspace_settings.name='Non-Color'
im.pixels.foreach_set(packed.ravel())
im.filepath_raw=str(DEST/(im.name+'.png'))
im.file_format='PNG'
im.save()

# Record identical model renders before changing its transform for Unity.
center=Vector(scene['comparison_center'])
extent=scene['comparison_extent']
scene.camera.location=center+Vector((.8,-1.6,.8)).normalized()*extent*3
scene.camera.rotation_euler=(center-scene.camera.location).to_track_quat('-Z','Y').to_euler()
for n in nodes:
    if n.type=='TEX_IMAGE' and n.image==base: n.image=base2
    elif n.type=='TEX_IMAGE' and n.image==normal: n.image=norm2
scene.render.engine='BLENDER_EEVEE_NEXT'
scene.render.filepath=str(OUT/f'game_{target}_2K.png')
bpy.ops.render.render(write_still=True)

# Match slot footprint without distorting proportions; same transform for all LODs.
baseline=json.loads((OUT/'baseline_report.json').read_text())
lo,hi=[Vector(v) for v in baseline['bounds']]
size=hi-lo
scale=min(20/size.x,18/size.y,22/size.z)
origin=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for v in obj.data.vertices:
    v.co=(obj.matrix_world @ v.co-origin)*scale
obj.matrix_world.identity()
# FBX front direction is verified in the Unity render; the source entrance faces -Y.
bpy.ops.export_scene.fbx(filepath=str(DEST/f'TownHall_{target}.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,mesh_smooth_type='OFF',add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
obj.data.calc_loop_triangles()
(OUT/f'game_{target}_export.json').write_text(json.dumps({'triangles':len(obj.data.loop_triangles),'requested_triangles':target,'uniform_scale':scale,'slot_limits_xyz':[20,22,18],'result_size_blender_xyz':list(size*scale),'origin':'bottom_center','texture_size':2048,'material_pack':'R=metallic, A=1-roughness; linear'},indent=2),encoding='utf-8')
print('UNITY_SOURCE_READY',target,flush=True)
