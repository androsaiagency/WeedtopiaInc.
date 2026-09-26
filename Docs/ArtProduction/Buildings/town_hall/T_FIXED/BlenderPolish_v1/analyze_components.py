import bpy
import json
import os
import sys
from collections import deque
from mathutils import Vector


def command_args():
    if "--" not in sys.argv:
        raise RuntimeError("Expected input FBX and output directory after --")
    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) != 2:
        raise RuntimeError("Expected input FBX and output directory")
    return os.path.abspath(args[0]), os.path.abspath(args[1])


def polygon_components(mesh):
    vertex_faces = [[] for _ in mesh.vertices]
    for poly in mesh.polygons:
        for vertex_index in poly.vertices:
            vertex_faces[vertex_index].append(poly.index)

    visited = set()
    components = []
    for start in range(len(mesh.polygons)):
        if start in visited:
            continue
        queue = deque([start])
        visited.add(start)
        faces = []
        while queue:
            face_index = queue.popleft()
            faces.append(face_index)
            for vertex_index in mesh.polygons[face_index].vertices:
                for neighbor in vertex_faces[vertex_index]:
                    if neighbor not in visited:
                        visited.add(neighbor)
                        queue.append(neighbor)
        components.append(faces)
    components.sort(key=len, reverse=True)
    return components


def make_emission_material(name, color):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (*color, 1.0)
    emission.inputs["Strength"].default_value = 0.8
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def point_at(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def render_views(obj, components, output_dir, center, size):
    palette = [
        (0.89, 0.22, 0.19), (0.20, 0.58, 0.86), (0.22, 0.72, 0.36),
        (0.95, 0.66, 0.14), (0.58, 0.31, 0.78), (0.10, 0.74, 0.72),
        (0.92, 0.35, 0.62), (0.62, 0.70, 0.16), (0.94, 0.45, 0.15),
        (0.22, 0.38, 0.73), (0.47, 0.74, 0.56), (0.76, 0.29, 0.25),
        (0.37, 0.27, 0.65), (0.76, 0.67, 0.22), (0.15, 0.63, 0.46),
        (0.65, 0.31, 0.55), (0.33, 0.64, 0.75), (0.87, 0.52, 0.55),
    ]
    materials = [make_emission_material(f"ComponentColor_{i:02d}", color) for i, color in enumerate(palette)]
    obj.data.materials.clear()
    for material in materials:
        obj.data.materials.append(material)
    for rank, face_indices in enumerate(components):
        for face_index in face_indices:
            obj.data.polygons[face_index].material_index = rank % len(materials)

    camera_data = bpy.data.cameras.new("ComponentCamera")
    camera = bpy.data.objects.new("ComponentCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = max(size) * 1.32
    bpy.context.scene.camera = camera

    world = bpy.data.worlds.new("ComponentWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.012, 0.015, 0.02, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.1
    bpy.context.scene.world = world

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.look = "AgX - Medium High Contrast"

    distance = max(size) * 2.4
    views = {
        "components_front.png": (0.0, -1.0, 0.42),
        "components_front_left.png": (-0.85, -1.0, 0.5),
        "components_front_right.png": (0.85, -1.0, 0.5),
        "components_rear.png": (0.0, 1.0, 0.42),
    }
    for filename, direction in views.items():
        camera.location = center + Vector(direction).normalized() * distance
        point_at(camera, center)
        scene.render.filepath = os.path.join(output_dir, filename)
        bpy.ops.render.render(write_still=True)


def main():
    input_fbx, output_dir = command_args()
    os.makedirs(output_dir, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=input_fbx, use_anim=False)
    objects = [item for item in bpy.context.scene.objects if item.type == "MESH"]
    if len(objects) != 1:
        raise RuntimeError(f"Expected one mesh object, got {len(objects)}")
    obj = objects[0]
    mesh = obj.data
    components = polygon_components(mesh)
    uv_layer = mesh.uv_layers.active.data if mesh.uv_layers.active else None

    records = []
    for rank, face_indices in enumerate(components):
        vertex_indices = sorted({vi for fi in face_indices for vi in mesh.polygons[fi].vertices})
        coords = [obj.matrix_world @ mesh.vertices[index].co for index in vertex_indices]
        lo = Vector((min(c.x for c in coords), min(c.y for c in coords), min(c.z for c in coords)))
        hi = Vector((max(c.x for c in coords), max(c.y for c in coords), max(c.z for c in coords)))
        uv_values = []
        area = 0.0
        for face_index in face_indices:
            polygon = mesh.polygons[face_index]
            area += polygon.area
            if uv_layer:
                uv_values.extend(uv_layer[loop_index].uv.copy() for loop_index in polygon.loop_indices)
        uv_bounds = None
        if uv_values:
            uv_bounds = {
                "min": [min(uv.x for uv in uv_values), min(uv.y for uv in uv_values)],
                "max": [max(uv.x for uv in uv_values), max(uv.y for uv in uv_values)],
            }
        records.append(
            {
                "rank": rank,
                "face_count": len(face_indices),
                "vertex_count": len(vertex_indices),
                "surface_area": area,
                "bounds_min": list(lo),
                "bounds_max": list(hi),
                "center": list((lo + hi) * 0.5),
                "size": list(hi - lo),
                "uv_bounds": uv_bounds,
            }
        )

    all_points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    bounds_min = Vector((min(p.x for p in all_points), min(p.y for p in all_points), min(p.z for p in all_points)))
    bounds_max = Vector((max(p.x for p in all_points), max(p.y for p in all_points), max(p.z for p in all_points)))
    center = (bounds_min + bounds_max) * 0.5
    size = bounds_max - bounds_min
    report = {
        "source_fbx": input_fbx,
        "component_count": len(records),
        "bounds_min": list(bounds_min),
        "bounds_max": list(bounds_max),
        "components": records,
    }
    with open(os.path.join(output_dir, "component_report.json"), "w", encoding="utf-8") as handle:
        json.dump(report, handle, indent=2)

    render_views(obj, components, output_dir, center, size)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output_dir, "WT_town_hall_component_debug.blend"))


if __name__ == "__main__":
    main()
