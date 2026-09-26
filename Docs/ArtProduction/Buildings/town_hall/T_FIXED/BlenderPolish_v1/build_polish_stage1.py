import bpy
import math
import os
import sys
from mathutils import Vector


def command_args():
    if "--" not in sys.argv:
        raise RuntimeError("Expected input FBX, leaf decal and output directory after --")
    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) != 3:
        raise RuntimeError("Expected input FBX, leaf decal and output directory")
    return tuple(os.path.abspath(value) for value in args)


def material(name, base_color, roughness=0.7, metallic=0.0):
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    principled = result.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (*base_color, 1.0)
    principled.inputs["Roughness"].default_value = roughness
    principled.inputs["Metallic"].default_value = metallic
    return result


def add_box(name, location, dimensions, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if mat:
        obj.data.materials.append(mat)
    if bevel > 0.0:
        modifier = obj.modifiers.new("Soft civic edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    return obj


def add_cylinder(name, location, radius, depth, mat, vertices=64):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Soft civic edges", "BEVEL")
    bevel.width = 0.0025
    bevel.segments = 2
    return obj


def arch_outline(width, bottom_z, shoulder_z, top_z, steps=10):
    points = [(-width * 0.5, bottom_z), (-width * 0.5, shoulder_z)]
    radius = width * 0.5
    center_z = shoulder_z
    for index in range(steps + 1):
        angle = math.pi - math.pi * index / steps
        points.append((math.cos(angle) * radius, center_z + math.sin(angle) * (top_z - shoulder_z)))
    points.append((width * 0.5, bottom_z))
    return points


def add_extruded_arch(name, center_x, front_y, depth, width, bottom_z, shoulder_z, top_z, mat):
    outline = arch_outline(width, bottom_z, shoulder_z, top_z)
    vertices = []
    for y in (front_y, front_y + depth):
        vertices.extend((center_x + x, y, z) for x, z in outline)
    count = len(outline)
    faces = []
    faces.append(tuple(range(count)))
    faces.append(tuple(range(count, count * 2))[::-1])
    for index in range(count):
        nxt = (index + 1) % count
        faces.append((index, nxt, count + nxt, count + index))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Rounded plaque edge", "BEVEL")
    bevel.width = 0.003
    bevel.segments = 3
    return obj


def add_leaf_decal(filepath, center_x, y, center_z, width, height):
    image = bpy.data.images.load(filepath, check_existing=True)
    mat = bpy.data.materials.new("WT Clean Leaf Decal")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = image
    principled.inputs["Roughness"].default_value = 0.78
    links.new(texture.outputs["Color"], principled.inputs["Base Color"])
    links.new(texture.outputs["Alpha"], principled.inputs["Alpha"])
    links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    if hasattr(mat, "surface_render_method"):
        mat.surface_render_method = "DITHERED"

    half_w = width * 0.5
    half_h = height * 0.5
    vertices = [
        (center_x - half_w, y, center_z - half_h),
        (center_x + half_w, y, center_z - half_h),
        (center_x + half_w, y, center_z + half_h),
        (center_x - half_w, y, center_z + half_h),
    ]
    mesh = bpy.data.meshes.new("WT Clean Leaf Decal Mesh")
    mesh.from_pydata(vertices, [], [(0, 1, 2, 3)])
    uv = mesh.uv_layers.new(name="UVMap")
    for loop, value in zip(uv.data, ((0, 0), (1, 0), (1, 1), (0, 1))):
        loop.uv = value
    obj = bpy.data.objects.new("WT Clean Leaf Decal", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def add_window(name, center, width, height, orientation, glass, frame):
    x, y, z = center
    thickness = 0.009
    bar = 0.009
    if orientation == "front":
        add_box(name + " Glass", (x, y, z), (width, thickness, height), glass, 0.0015)
        add_box(name + " Frame L", (x - width * 0.5, y - 0.006, z), (bar, thickness * 1.4, height + bar), frame, 0.001)
        add_box(name + " Frame R", (x + width * 0.5, y - 0.006, z), (bar, thickness * 1.4, height + bar), frame, 0.001)
        add_box(name + " Frame T", (x, y - 0.006, z + height * 0.5), (width + bar, thickness * 1.4, bar), frame, 0.001)
        add_box(name + " Frame B", (x, y - 0.006, z - height * 0.5), (width + bar, thickness * 1.4, bar), frame, 0.001)
        add_box(name + " Mullion", (x, y - 0.007, z), (bar * 0.75, thickness * 1.5, height), frame, 0.001)
    elif orientation == "rear":
        add_box(name + " Glass", (x, y, z), (width, thickness, height), glass, 0.0015)
        add_box(name + " Frame L", (x - width * 0.5, y + 0.006, z), (bar, thickness * 1.4, height + bar), frame, 0.001)
        add_box(name + " Frame R", (x + width * 0.5, y + 0.006, z), (bar, thickness * 1.4, height + bar), frame, 0.001)
        add_box(name + " Frame T", (x, y + 0.006, z + height * 0.5), (width + bar, thickness * 1.4, bar), frame, 0.001)
        add_box(name + " Frame B", (x, y + 0.006, z - height * 0.5), (width + bar, thickness * 1.4, bar), frame, 0.001)
        add_box(name + " Mullion", (x, y + 0.007, z), (bar * 0.75, thickness * 1.5, height), frame, 0.001)
    else:
        add_box(name + " Glass", (x, y, z), (thickness, width, height), glass, 0.0015)
        add_box(name + " Frame A", (x, y - width * 0.5, z), (thickness * 1.4, bar, height + bar), frame, 0.001)
        add_box(name + " Frame B", (x, y + width * 0.5, z), (thickness * 1.4, bar, height + bar), frame, 0.001)
        add_box(name + " Frame T", (x, y, z + height * 0.5), (thickness * 1.4, width + bar, bar), frame, 0.001)
        add_box(name + " Frame Btm", (x, y, z - height * 0.5), (thickness * 1.4, width + bar, bar), frame, 0.001)


def add_area_light(name, location, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    return obj


def point_at(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return lo, hi


def render_views(output_dir):
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    lo, hi = bounds(mesh_objects)
    center = (lo + hi) * 0.5
    size = hi - lo
    max_dim = max(size)

    world = bpy.data.worlds.new("WT Polish World")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.012, 0.016, 0.022, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.12
    bpy.context.scene.world = world

    lights = [
        add_area_light("Key", center + Vector((-1.4, -1.7, 1.9)) * max_dim, 700, max_dim * 1.2, (1.0, 0.82, 0.64)),
        add_area_light("Fill", center + Vector((1.5, -0.5, 1.0)) * max_dim, 350, max_dim * 1.4, (0.58, 0.72, 1.0)),
        add_area_light("Rim", center + Vector((0.0, 1.7, 1.6)) * max_dim, 500, max_dim, (0.72, 0.88, 1.0)),
    ]
    for light in lights:
        point_at(light, center)

    camera_data = bpy.data.cameras.new("WT Polish Camera")
    camera = bpy.data.objects.new("WT Polish Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = max_dim * 1.30
    bpy.context.scene.camera = camera

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = -0.75
    distance = max_dim * 2.4
    views = {
        "stage1_front.png": (0.0, -1.0, 0.42),
        "stage1_front_left.png": (-0.85, -1.0, 0.5),
        "stage1_front_right.png": (0.85, -1.0, 0.5),
        "stage1_rear.png": (0.0, 1.0, 0.42),
    }
    for filename, direction in views.items():
        camera.location = center + Vector(direction).normalized() * distance
        point_at(camera, center)
        scene.render.filepath = os.path.join(output_dir, filename)
        bpy.ops.render.render(write_still=True)


def main():
    input_fbx, leaf_decal, output_dir = command_args()
    os.makedirs(output_dir, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=input_fbx, use_anim=False)
    source = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(source) != 1:
        raise RuntimeError(f"Expected one source mesh; got {len(source)}")
    source[0].name = "WT_town_hall_Meshy_v2_preserved"

    collection = bpy.data.collections.new("WT_Polish_Additions_Stage1")
    bpy.context.scene.collection.children.link(collection)
    previous_collection = bpy.context.collection

    cream = material("WT Warm Mineral Plaster", (0.66, 0.61, 0.50), 0.82)
    cream_light = material("WT Crest Face", (0.82, 0.77, 0.65), 0.78)
    sage = material("WT Sage Enamel", (0.075, 0.22, 0.17), 0.68)
    glass = material("WT Opaque Sage Glass", (0.035, 0.115, 0.105), 0.34)
    copper = material("WT Aged Copper", (0.36, 0.13, 0.055), 0.46, 0.65)

    # Hidden inner cylinder closes the tower gaps without changing its silhouette.
    add_cylinder("WT Tower Inner Backing", (0.0, 0.012, 0.075), 0.099, 0.335, glass, 64)

    # Clean replaceable crest layered in front of the distorted source plaque.
    add_extruded_arch("WT Crest Border", 0.0, -0.118, 0.010, 0.190, -0.166, -0.018, 0.052, sage)
    add_extruded_arch("WT Crest Face", 0.0, -0.124, 0.007, 0.174, -0.155, -0.020, 0.042, cream_light)
    add_leaf_decal(leaf_decal, 0.0, -0.132, -0.062, 0.115, 0.112)

    # Entrance replacement and readable three-step approach.
    add_box("WT Entrance Doors", (0.0, -0.219, -0.296), (0.155, 0.010, 0.175), glass, 0.002)
    add_box("WT Entrance Frame Top", (0.0, -0.226, -0.205), (0.169, 0.012, 0.010), copper, 0.0015)
    add_box("WT Entrance Frame Left", (-0.080, -0.226, -0.294), (0.010, 0.012, 0.182), copper, 0.0015)
    add_box("WT Entrance Frame Right", (0.080, -0.226, -0.294), (0.010, 0.012, 0.182), copper, 0.0015)
    add_box("WT Entrance Door Split", (0.0, -0.227, -0.294), (0.007, 0.012, 0.174), copper, 0.001)
    add_box("WT Step Top", (0.0, -0.235, -0.386), (0.220, 0.075, 0.022), cream, 0.004)
    add_box("WT Step Middle", (0.0, -0.270, -0.406), (0.250, 0.105, 0.022), cream, 0.004)
    add_box("WT Step Bottom", (0.0, -0.307, -0.426), (0.282, 0.130, 0.022), cream, 0.004)

    # Calm opaque window inserts cover projection shards while retaining original surrounds and plants.
    add_window("WT Front Window L", (-0.318, -0.222, -0.265), 0.174, 0.168, "front", glass, sage)
    add_window("WT Front Window R", (0.318, -0.222, -0.265), 0.174, 0.168, "front", glass, sage)
    add_window("WT Rear Window L", (-0.315, 0.238, -0.247), 0.182, 0.230, "rear", glass, sage)
    add_window("WT Rear Window R", (0.315, 0.238, -0.247), 0.182, 0.230, "rear", glass, sage)
    add_window("WT Side Window L", (-0.492, -0.015, -0.247), 0.175, 0.210, "side", glass, sage)
    add_window("WT Side Window R", (0.492, -0.015, -0.247), 0.175, 0.210, "side", glass, sage)

    # Flat rear civic panel covers the worst atlas streaks while preserving the tower above it.
    add_box("WT Rear Central Repair Panel", (0.0, 0.236, -0.205), (0.205, 0.012, 0.270), cream, 0.004)

    # Move additions into their own collection for review and easy toggling.
    for obj in list(bpy.context.scene.objects):
        if obj.type == "MESH" and obj is not source[0] and obj.name not in {"WT Polish Camera"}:
            for owner in list(obj.users_collection):
                owner.objects.unlink(obj)
            collection.objects.link(obj)

    render_views(output_dir)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output_dir, "WT_town_hall_polish_stage1.blend"))


if __name__ == "__main__":
    main()
