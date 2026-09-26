import bpy
import bmesh
import json
import math
import os
import sys
from mathutils import Vector


def args_after_separator():
    if "--" not in sys.argv:
        raise RuntimeError("Expected arguments after --: input.fbx output_dir")
    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) != 2:
        raise RuntimeError("Expected exactly two arguments: input.fbx output_dir")
    return os.path.abspath(args[0]), os.path.abspath(args[1])


def mesh_world_bounds(objects):
    points = []
    for obj in objects:
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return lo, hi


def connected_components(mesh):
    adjacency = [[] for _ in mesh.vertices]
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacency[a].append(b)
        adjacency[b].append(a)
    visited = set()
    sizes = []
    for start in range(len(mesh.vertices)):
        if start in visited:
            continue
        stack = [start]
        visited.add(start)
        count = 0
        while stack:
            current = stack.pop()
            count += 1
            for neighbor in adjacency[current]:
                if neighbor not in visited:
                    visited.add(neighbor)
                    stack.append(neighbor)
        sizes.append(count)
    sizes.sort(reverse=True)
    return sizes


def edge_topology_counts(mesh):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    result = {
        "boundary_edges": sum(1 for edge in bm.edges if edge.is_boundary),
        "non_manifold_edges": sum(1 for edge in bm.edges if not edge.is_manifold),
        "loose_edges": sum(1 for edge in bm.edges if not edge.link_faces),
    }
    bm.free()
    return result


def ensure_world():
    world = bpy.data.worlds.new("WT_InspectionWorld") if not bpy.data.worlds else bpy.data.worlds[0]
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.018, 0.022, 0.028, 1.0)
    bg.inputs[1].default_value = 0.32


def add_area_light(name, location, energy, size, color):
    light_data = bpy.data.lights.new(name, type="AREA")
    light_data.energy = energy
    light_data.shape = "DISK"
    light_data.size = size
    light_data.color = color
    light = bpy.data.objects.new(name, light_data)
    bpy.context.collection.objects.link(light)
    light.location = location
    return light


def point_at(obj, target):
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_scene(center, size):
    ensure_world()
    max_dim = max(size)
    key = add_area_light(
        "Key",
        center + Vector((-max_dim * 1.6, -max_dim * 1.8, max_dim * 2.2)),
        2200,
        max_dim * 1.1,
        (1.0, 0.82, 0.64),
    )
    fill = add_area_light(
        "Fill",
        center + Vector((max_dim * 1.8, -max_dim * 0.6, max_dim * 1.2)),
        1300,
        max_dim * 1.35,
        (0.58, 0.72, 1.0),
    )
    rim = add_area_light(
        "Rim",
        center + Vector((0.0, max_dim * 1.9, max_dim * 2.0)),
        1800,
        max_dim,
        (0.75, 0.9, 1.0),
    )
    for light in (key, fill, rim):
        point_at(light, center)

    camera_data = bpy.data.cameras.new("InspectionCamera")
    camera = bpy.data.objects.new("InspectionCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = max_dim * 1.32
    camera_data.lens = 52

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    return camera


def render_view(camera, center, distance, direction, filepath):
    direction = Vector(direction).normalized()
    camera.location = center + direction * distance
    point_at(camera, center)
    bpy.context.scene.render.filepath = filepath
    bpy.ops.render.render(write_still=True)


def main():
    input_fbx, output_dir = args_after_separator()
    os.makedirs(output_dir, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=input_fbx, use_anim=False)

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError("No mesh objects were imported")

    reports = []
    for obj in meshes:
        mesh = obj.data
        topology = edge_topology_counts(mesh)
        components = connected_components(mesh)
        reports.append(
            {
                "object": obj.name,
                "vertices": len(mesh.vertices),
                "edges": len(mesh.edges),
                "polygons": len(mesh.polygons),
                "triangles": sum(max(1, len(poly.vertices) - 2) for poly in mesh.polygons),
                "uv_layers": [layer.name for layer in mesh.uv_layers],
                "material_slots": [slot.material.name if slot.material else None for slot in obj.material_slots],
                "connected_component_count": len(components),
                "largest_component_vertex_counts": components[:25],
                **topology,
            }
        )

    images = []
    for image in bpy.data.images:
        images.append(
            {
                "name": image.name,
                "filepath": bpy.path.abspath(image.filepath),
                "size": list(image.size),
                "colorspace": image.colorspace_settings.name,
            }
        )

    materials = []
    for material in bpy.data.materials:
        nodes = []
        if material.use_nodes and material.node_tree:
            for node in material.node_tree.nodes:
                nodes.append(
                    {
                        "name": node.name,
                        "type": node.bl_idname,
                        "image": node.image.name if hasattr(node, "image") and node.image else None,
                    }
                )
        materials.append({"name": material.name, "nodes": nodes})

    lo, hi = mesh_world_bounds(meshes)
    center = (lo + hi) * 0.5
    size = hi - lo
    report = {
        "source_fbx": input_fbx,
        "mesh_object_count": len(meshes),
        "bounds_min": list(lo),
        "bounds_max": list(hi),
        "bounds_size": list(size),
        "objects": reports,
        "materials": materials,
        "images": images,
    }
    with open(os.path.join(output_dir, "inspection_report.json"), "w", encoding="utf-8") as handle:
        json.dump(report, handle, indent=2, ensure_ascii=False)

    camera = setup_scene(center, size)
    distance = max(size) * 2.4
    views = {
        "baseline_front.png": (0.0, -1.0, 0.42),
        "baseline_front_left.png": (-0.85, -1.0, 0.5),
        "baseline_front_right.png": (0.85, -1.0, 0.5),
        "baseline_rear.png": (0.0, 1.0, 0.42),
    }
    for name, direction in views.items():
        render_view(camera, center, distance, direction, os.path.join(output_dir, name))

    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output_dir, "WT_town_hall_MeshyBase_polish_v1.blend"))


if __name__ == "__main__":
    main()
