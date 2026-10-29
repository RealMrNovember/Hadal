"""Turns TRELLIS textured GLBs (art/gpu/batch_3d.sh) into mobile-ready Unity props.

    blender -b --factory-startup --python art/blender/props.py -- \
        --project /opt/hadal --source /opt/hadal-thirdparty/trellis/set1 [--only id,id] [--no-preview]

Per asset (art/gpu/assets.json): import <source>/<id>/<id>_textured.glb, keep one mesh, scale to the
roster height (metres), put the origin at the bottom centre (decor, crawlers) or the volume centre
(swimmers), decimate to the kind's triangle budget, downscale the baked texture, export
Assets/_Hadal/Art/Props/<id>.fbx + <id>_albedo.png and render a preview.

Map creatures (art/gpu/creatures.json) go to their own folder and are turned so the head points +Z in Unity
(the shader and CreatureMotion assume it); a roster entry may set its own "budget" (triangles) and "texture" (px):

    blender -b --factory-startup --python art/blender/props.py -- --project /opt/hadal         --source /opt/hadal-thirdparty/trellis/creatures --roster art/gpu/creatures.json         --out-dir Assets/_Hadal/Art/Creatures --bake-yaw
"""
import argparse
import json
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import hadal_kit as kit  # noqa: E402

BUDGET = {"creature": 4000, "submarine": 4500, "decor": 3000}
BIG_BUDGET = 6500  # creature_iron_leviathan and other assets taller than 3.5 m
TEXTURE_SIZE = 512
SWIMMERS = {"swim", "pulse_swim", "patrol"}


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--project", required=True)
    p.add_argument("--source", required=True)
    p.add_argument("--roster", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "gpu", "assets.json"))
    p.add_argument("--only", default="")
    p.add_argument("--no-preview", action="store_true")
    p.add_argument("--out-dir", default="Assets/_Hadal/Art/Props", help="project-relative output folder")
    p.add_argument("--bake-yaw", action="store_true", help="turn the mesh by the roster yaw so the head points +Z in Unity")
    return p.parse_args(argv)


def import_mesh(path, name):
    kit.reset()
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    for o in [o for o in bpy.data.objects if o.type != "MESH"]:
        bpy.data.objects.remove(o, do_unlink=True)

    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.parent = None
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.name = obj.data.name = name
    return obj


def bounds(obj):
    pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


def normalise(obj, height, swimmer):
    lo, hi = bounds(obj)
    scale = height / max(1e-4, hi.z - lo.z)
    centre = (lo + hi) / 2
    pivot = Vector((centre.x, centre.y, centre.z if swimmer else lo.z))
    for v in obj.data.vertices:
        v.co = (v.co - pivot) * scale


def bake_yaw(obj, yaw):
    """Unity yaw (clockwise from above, degrees) -> Blender turn about Z (counter-clockwise): head ends up on Blender -Y,
    which is Unity +Z (Blender -> Unity is (x, y, z) -> (-x, z, -y))."""
    from mathutils import Matrix
    import math
    turn = Matrix.Rotation(math.radians(-yaw), 4, "Z")
    for v in obj.data.vertices:
        v.co = turn @ v.co


def decimate(obj, budget):
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    if tris > budget:
        mod = obj.modifiers.new("Decimate", "DECIMATE")
        mod.ratio = budget / tris
        mod.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier="Decimate")
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def bake_texture(obj, asset_id, out_dir, size=TEXTURE_SIZE):
    """Saves the baked base colour as a small PNG next to the FBX and points the material at it."""
    material = obj.data.materials[0] if obj.data.materials else None
    image = None
    if material and material.use_nodes:
        image = next((n.image for n in material.node_tree.nodes if n.type == "TEX_IMAGE" and n.image), None)
    if image is None:
        raise RuntimeError(f"{asset_id}: no baked texture in the GLB")

    if max(image.size) > size:
        image.scale(size, size)
    path = os.path.join(out_dir, f"{asset_id}_albedo.png")
    image.filepath_raw = path
    image.file_format = "PNG"
    image.save()
    image.name = os.path.basename(path)
    material.name = f"{asset_id}_mat"
    for m in obj.data.materials[1:]:
        obj.data.materials.remove(m)


def export(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False, path_mode="STRIP")


def render_top(path, height):
    """Straight-down view of the scene left by render_preview: image right = Blender +X, image up =
    +Y. Used to read which way a creature's head points and set its "yaw" in assets.json
    (Blender -> Unity is (x, y, z) -> (-x, z, -y): head left = yaw -90, right = +90, down = 0, up = 180)."""
    scene = bpy.context.scene
    cam = scene.camera
    cam.location = Vector((0, 0, height * 6 + 4))
    cam.rotation_euler = (0, 0, 0)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def main():
    args = parse_args()
    roster = json.load(open(args.roster))
    only = {s for s in args.only.split(",") if s}
    out_dir = os.path.join(args.project, args.out_dir)
    out_preview = os.path.join(args.project, "artifacts/art-preview")
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(out_preview, exist_ok=True)

    report = {}
    for asset in roster["assets"]:
        asset_id = asset["id"]
        if only and asset_id not in only:
            continue
        source = os.path.join(args.source, asset_id, f"{asset_id}_textured.glb")
        if not os.path.exists(source):
            print(f"[props] {asset_id}: missing {source}, skipped", flush=True)
            continue

        swimmer = asset["motion"] in SWIMMERS
        obj = import_mesh(source, asset_id)
        normalise(obj, asset["height"], swimmer)
        if args.bake_yaw:
            bake_yaw(obj, asset.get("yaw", 0))
        budget = asset.get("budget") or (BIG_BUDGET if asset["height"] > 3.5 else BUDGET[asset["kind"]])
        tris = decimate(obj, budget)
        texture = asset.get("texture", TEXTURE_SIZE)
        bake_texture(obj, asset_id, out_dir, texture)
        export(obj, os.path.join(out_dir, asset_id + ".fbx"))

        if not args.no_preview:
            h = asset["height"]
            kit.render_preview(os.path.join(out_preview, f"prop_{asset_id}.png"),
                               focus=(0, 0, 0 if swimmer else h * 0.5), distance=h * 2.4 + 1.0, samples=16,
                               floor=not swimmer)
            if swimmer or asset["motion"] == "crawl":
                render_top(os.path.join(out_preview, f"prop_{asset_id}_top.png"), h)
        lo, hi = bounds(obj)
        report[asset_id] = {"triangles": tris, "texture": texture, "height_m": asset["height"], "motion": asset["motion"], "kind": asset["kind"],
                            "size_m": [round(hi.x - lo.x, 3), round(hi.y - lo.y, 3), round(hi.z - lo.z, 3)],
                            "min_m": [round(lo.x, 3), round(lo.y, 3), round(lo.z, 3)]}
        print(f"[props] {asset_id}: {tris} tris", flush=True)

    report_name = "props.json" if args.out_dir.rstrip("/").endswith("Props") else os.path.basename(args.out_dir.rstrip("/")).lower() + ".json"
    with open(os.path.join(out_preview, report_name), "w") as f:
        json.dump(report, f, indent=2)


if __name__ == "__main__":
    main()
