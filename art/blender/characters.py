"""Crew, mechs and creatures from CC0 packs (art/THIRD_PARTY.md), adapted for HADAL.

    blender -b --factory-startup --python art/blender/characters.py -- \
        --project /opt/hadal --source /opt/hadal-thirdparty/space-kit/x [--only crew_engineer] [--no-preview]

Per character: import the rigged GLB, recolour the palette atlas for its role, decimate to the mobile
budget (skin weights survive the Decimate modifier), rename actions to plain clip names
("CharacterArmature|Walk_CharacterArmature" -> "Walk"), keep only the clips the game uses, export
FBX with animations to Assets/_Hadal/Art/Characters/<id>.fbx (+ <id>_atlas.png) and render a preview.
"""
import argparse
import json
import math
import os
import re
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import hadal_kit as kit  # noqa: E402

# id -> (source file, triangle budget, clip set, role tint, height in metres)
CHARACTERS = {
    "crew_engineer": ("Astronaut.glb", 3500, "crew", "engineer", 1.1),
    "crew_medic": ("Astronaut-0D54W8yfrA.glb", 3500, "crew", "medic", 1.1),
    "crew_scientist": ("Astronaut-OgeSH89Nmx.glb", 3500, "crew", "scientist", 1.1),
    "mech_worker": ("Mech.glb", 3500, "mech", None, 1.6),
    "mech_guard": ("Mech-4UvIHxnoSR.glb", 4500, "mech", None, 2.0),
    "creature_crawler": ("Enemy Small.glb", 1600, "creature", None, 0.9),
    "creature_siren": ("Enemy Flying.glb", 2500, "creature", None, 1.4),
    "creature_brute": ("Enemy Large.glb", 4000, "creature", None, 2.4),
}

CLIPS = {
    "crew": ["Idle", "Walk", "Run", "Wave", "Yes", "No", "Punch", "HitReact", "Death", "Duck", "Idle_Gun", "Walk_Gun", "Run_Gun_Shoot"],
    "mech": ["Idle", "Walk", "Run", "Pickup", "Hello", "Shoot_Small", "Shoot_Big", "HitRecieve_1", "Death", "Dance"],
    "creature": ["Idle", "Flying_Idle", "Fast_Flying", "Walk", "Run", "Punch", "Headbutt", "HitReact", "Death"],
}

# Suit tints per role, applied to the atlas cells that hold the suit's main colour.
ROLE_TINT = {
    "engineer": (0.86, 0.50, 0.10),   # hazard orange
    "medic": (0.85, 0.90, 0.92),      # clinical white
    "scientist": (0.20, 0.62, 0.55),  # bio teal
}


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--project", required=True)
    p.add_argument("--source", required=True)
    p.add_argument("--only", default="")
    p.add_argument("--no-preview", action="store_true")
    p.add_argument("--dump-atlas", action="store_true", help="write the source atlas and its colour cells, then stop")
    return p.parse_args(argv)


# Props the source rigs carry that the base crew should not (weapons come with combat roles).
DROPPED_MESHES = {"Pistol"}


def import_glb(path):
    """Imports a rig; keeps only meshes skinned to it (the packs also ship stray helper meshes)."""
    kit.reset()
    bpy.ops.import_scene.gltf(filepath=path)
    armature = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    meshes = []
    for o in [o for o in bpy.data.objects if o.type == "MESH"]:
        if o.parent != armature or o.name in DROPPED_MESHES:
            bpy.data.objects.remove(o, do_unlink=True)
        else:
            meshes.append(o)
    return armature, meshes


def atlas_image():
    return next(i for i in bpy.data.images if i.size[0] > 0)


def colour_cells(image, cells=32):
    """The atlas is a 32x32 grid of flat colour cells; returns {(cx, cy): (r, g, b)} in linear float."""
    w, h = image.size
    px = list(image.pixels)
    out = {}
    for cy in range(cells):
        for cx in range(cells):
            x = int((cx + 0.5) * w / cells)
            y = int((cy + 0.5) * h / cells)
            i = (y * w + x) * 4
            out[(cx, cy)] = tuple(round(v, 3) for v in px[i:i + 3])
    return out


def used_uv_cells(meshes, image, cells=32):
    """Atlas cells actually referenced by mesh UVs, weighted by face count."""
    counts = {}
    for o in meshes:
        uv = o.data.uv_layers.active
        if uv is None:
            continue
        for poly in o.data.polygons:
            u = sum(uv.data[li].uv.x for li in poly.loop_indices) / poly.loop_total
            v = sum(uv.data[li].uv.y for li in poly.loop_indices) / poly.loop_total
            key = (min(cells - 1, int(u % 1.0 * cells)), min(cells - 1, int(v % 1.0 * cells)))
            counts[key] = counts.get(key, 0) + 1
    return counts


def recolour(image, meshes, role):
    """Tints the most-used bright cell (the suit body) to the role colour, keeping its shading."""
    if role is None:
        return
    cells = colour_cells(image)
    counts = used_uv_cells(meshes, image)

    candidates = sorted((n, cell) for cell, n in counts.items() if sum(cells[cell]) / 3 > 0.15)
    if not candidates:
        return
    suit = candidates[-1][1]
    src = Vector(cells[suit])
    dst = Vector(ROLE_TINT[role])
    brightness = max(0.05, sum(src) / 3)

    w, h = image.size
    px = list(image.pixels)
    cw, ch = w / 32, h / 32
    for y in range(int(suit[1] * ch), int((suit[1] + 1) * ch)):
        for x in range(int(suit[0] * cw), int((suit[0] + 1) * cw)):
            i = (y * w + x) * 4
            k = (px[i] + px[i + 1] + px[i + 2]) / 3 / brightness
            px[i], px[i + 1], px[i + 2] = (min(1.0, dst.x * k), min(1.0, dst.y * k), min(1.0, dst.z * k))
    image.pixels = px
    image.update()


def world_bounds(meshes):
    """Bounds of the posed, skinned meshes (bound_box is unreliable on armature-deformed meshes)."""
    dg = bpy.context.evaluated_depsgraph_get()
    lo, hi = Vector((1e9, 1e9, 1e9)), Vector((-1e9, -1e9, -1e9))
    for o in meshes:
        e = o.evaluated_get(dg)
        mesh = e.to_mesh()
        for v in mesh.vertices:
            p = e.matrix_world @ v.co
            lo = Vector(map(min, lo, p))
            hi = Vector(map(max, hi, p))
        e.to_mesh_clear()
    return lo, hi


def normalise_height(armature, meshes, height):
    """Scales the rig object so the rest pose is `height` metres tall with its feet at z=0."""
    lo, hi = world_bounds(meshes)
    armature.scale = armature.scale * (height / max(1e-4, hi.z - lo.z))
    bpy.context.view_layer.update()
    lo, _ = world_bounds(meshes)
    armature.location.z -= lo.z
    bpy.context.view_layer.update()


def decimate(meshes, budget):
    total = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    if total <= budget:
        return total
    ratio = budget / total
    for o in meshes:
        mod = o.modifiers.new("Decimate", "DECIMATE")
        mod.ratio = ratio
        mod.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = o
        # Armature modifier must stay last so the decimated mesh is what gets skinned.
        bpy.ops.object.modifier_move_to_index(modifier="Decimate", index=0)
        bpy.ops.object.modifier_apply(modifier="Decimate")
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)


def clean_actions(armature, keep):
    """Plain clip names; unused clips dropped so FBX "all actions" export writes exactly the kept takes."""
    kept = []
    for action in list(bpy.data.actions):
        m = re.match(r"^[^|]*\|(.+?)_[A-Za-z]*Armature$", action.name)
        name = m.group(1) if m else action.name
        if name not in keep:
            bpy.data.actions.remove(action)
            continue
        action.name = name
        action.use_fake_user = True
        kept.append(action)

    ad = armature.animation_data or armature.animation_data_create()
    for track in list(ad.nla_tracks):
        ad.nla_tracks.remove(track)
    ad.action = next((a for a in kept if a.name == "Idle"), kept[0] if kept else None)
    return sorted(a.name for a in kept)


def export(armature, meshes, path):
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for o in meshes:
        o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        mesh_smooth_type="FACE", add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X",
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.5,
        path_mode="STRIP", embed_textures=False)


def verify_takes(path):
    """Re-imports the FBX in a scratch scene and returns its animation take names (clip part only)."""
    before = set(bpy.data.actions.keys())
    objects_before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=path)
    new_actions = [a for a in bpy.data.actions if a.name not in before]
    names = sorted(a.name.split("|")[-1] for a in new_actions)
    for a in new_actions:
        bpy.data.actions.remove(a)
    for name in set(bpy.data.objects.keys()) - objects_before:
        bpy.data.objects.remove(bpy.data.objects[name], do_unlink=True)
    return names


def preview(armature, meshes, path, clip, height):
    action = bpy.data.actions.get(clip)
    if action:
        armature.animation_data.action = action
        bpy.context.scene.frame_set(int(sum(action.frame_range) / 2))
    kit.render_preview(path, focus=(0, 0, height * 0.5), distance=height * 3.2, samples=24)


def main():
    args = parse_args()
    only = {s for s in args.only.split(",") if s}
    out_dir = os.path.join(args.project, "Assets/_Hadal/Art/Characters")
    out_preview = os.path.join(args.project, "artifacts/art-preview")
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(out_preview, exist_ok=True)

    report = {}
    for char_id, (source, budget, clip_set, role, height) in CHARACTERS.items():
        if only and char_id not in only:
            continue
        armature, meshes = import_glb(os.path.join(args.source, source))
        image = atlas_image()

        if args.dump_atlas:
            image.filepath_raw = os.path.join(out_preview, f"atlas_{char_id}.png")
            image.file_format = "PNG"
            image.save()
            cells = colour_cells(image)
            counts = used_uv_cells(meshes, image)
            print(f"[chars] {char_id} cells used: " + ", ".join(f"{c}:{n}:{cells[c]}" for c, n in sorted(counts.items(), key=lambda kv: -kv[1])[:8]), flush=True)
            continue

        recolour(image, meshes, role)
        image.filepath_raw = os.path.join(out_dir, f"{char_id}_atlas.png")
        image.file_format = "PNG"
        image.save()
        image.name = f"{char_id}_atlas.png"
        for m in bpy.data.materials:
            m.name = f"{char_id}_mat"

        normalise_height(armature, meshes, height)
        tris = decimate(meshes, budget)
        clips = clean_actions(armature, CLIPS[clip_set])
        armature.name = char_id
        fbx = os.path.join(out_dir, char_id + ".fbx")
        export(armature, meshes, fbx)
        exported = verify_takes(fbx)
        if sorted(exported) != clips:
            raise RuntimeError(f"{char_id}: exported takes {exported} != clips {clips}")
        if not args.no_preview:
            preview(armature, meshes, os.path.join(out_preview, char_id + ".png"), "Walk" if "Walk" in clips else clips[0], height)

        report[char_id] = {"source": source, "triangles": tris, "height_m": height, "clips": clips}
        print(f"[chars] {char_id}: {tris} tris, clips {clips}", flush=True)

    if report:
        with open(os.path.join(out_preview, "characters.json"), "w") as f:
            json.dump(report, f, indent=2)


if __name__ == "__main__":
    main()
