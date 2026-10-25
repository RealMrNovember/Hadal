"""HADAL procedural asset kit for headless Blender 4.2.

Every asset is code: re-running the generator rebuilds the whole art set identically.

Conventions shared with Unity (Assets/_Hadal/Scripts/Editor/HadalArtImporter.cs):
  * Materials are named HAD_<Name>; Unity remaps them to URP materials from the same palette.
  * Child objects named ANIM_spin_<axis>_<deg/s> rotate, ANIM_pulse_<hz> pulse their emission,
    ANIM_sway_<hz> sway around their base (kelp); anything after "__" is just the part name.
  * 1 Blender unit = 1 Unity metre. Grid ring spacing is 5 m, so a building footprint stays
    within ~2.3 m of its origin; the origin sits on the floor at the building centre.
"""
import math

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

# --------------------------------------------------------------------------------------------
# Palette — mirrored in Unity by HadalArtPalette. (base RGB, metallic, roughness, emission RGB,
# emission strength, alpha). Linear colours.
# --------------------------------------------------------------------------------------------
PALETTE = {
    "HullDark":   ((0.035, 0.050, 0.060), 0.85, 0.42, None, 0.0, 1.0),
    "HullMid":    ((0.090, 0.120, 0.135), 0.80, 0.38, None, 0.0, 1.0),
    "Steel":      ((0.300, 0.320, 0.330), 1.00, 0.28, None, 0.0, 1.0),
    "Hazard":     ((0.820, 0.520, 0.030), 0.20, 0.50, None, 0.0, 1.0),
    "Rubber":     ((0.015, 0.015, 0.018), 0.00, 0.80, None, 0.0, 1.0),
    "Glass":      ((0.050, 0.180, 0.200), 0.00, 0.05, None, 0.0, 0.35),
    "GlowCyan":   ((0.020, 0.300, 0.350), 0.00, 0.30, (0.10, 0.90, 1.00), 6.0, 1.0),
    "GlowAmber":  ((0.400, 0.200, 0.020), 0.00, 0.30, (1.00, 0.55, 0.12), 5.0, 1.0),
    "GlowGreen":  ((0.050, 0.300, 0.080), 0.00, 0.30, (0.25, 1.00, 0.35), 5.0, 1.0),
    "GlowRed":    ((0.300, 0.020, 0.020), 0.00, 0.30, (1.00, 0.10, 0.08), 6.0, 1.0),
    "Plant":      ((0.040, 0.220, 0.060), 0.00, 0.70, (0.02, 0.10, 0.03), 0.6, 1.0),
    "Sand":       ((0.180, 0.200, 0.190), 0.00, 0.95, None, 0.0, 1.0),
    "Rock":       ((0.060, 0.070, 0.075), 0.00, 0.90, None, 0.0, 1.0),
    "Kelp":       ((0.030, 0.120, 0.060), 0.00, 0.60, (0.01, 0.05, 0.02), 0.4, 1.0),
    "Coral":      ((0.450, 0.120, 0.180), 0.00, 0.70, (0.30, 0.05, 0.12), 0.8, 1.0),
    "GlowViolet": ((0.200, 0.050, 0.400), 0.00, 0.20, (0.55, 0.20, 1.00), 5.0, 1.0),
    "PaintTeal":  ((0.040, 0.300, 0.300), 0.30, 0.55, None, 0.0, 1.0),
}

_materials = {}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _materials.clear()


def mat(name):
    if name in _materials:
        return _materials[name]
    base, metallic, rough, emit, strength, alpha = PALETTE[name]
    m = bpy.data.materials.new("HAD_" + name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*base, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = rough
    if emit:
        bsdf.inputs["Emission Color"].default_value = (*emit, 1.0)
        bsdf.inputs["Emission Strength"].default_value = strength
    if alpha < 1.0:
        bsdf.inputs["Alpha"].default_value = alpha
        bsdf.inputs["Transmission Weight"].default_value = 0.6
        m.blend_method = "BLEND"
    _materials[name] = m
    return m


# --------------------------------------------------------------------------------------------
# Primitives (all return the new object; z of `loc` is the BOTTOM of the shape unless noted)
# --------------------------------------------------------------------------------------------
def _finish(obj, material, bevel=0.0, smooth=False):
    obj.data.materials.append(mat(material))
    if bevel > 0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = "ANGLE"
        mod.angle_limit = math.radians(40)
    if smooth:
        for poly in obj.data.polygons:
            poly.use_smooth = True
    return obj


def _from_bmesh(name, bm):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def cyl(name, r, h, loc=(0, 0, 0), material="HullMid", verts=24, bevel=0.03, r_top=None, smooth=False):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=verts,
                          radius1=r, radius2=r if r_top is None else r_top, depth=h)
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector((loc[0], loc[1], loc[2] + h / 2)))
    return _finish(_from_bmesh(name, bm), material, bevel, smooth)


def box(name, size, loc=(0, 0, 0), material="HullMid", bevel=0.04):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector((loc[0], loc[1], loc[2] + size[2] / 2)))
    return _finish(_from_bmesh(name, bm), material, bevel)


def sphere(name, r, loc=(0, 0, 0), material="GlowCyan", seg=24, rings=12, center=True):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=r)
    z = loc[2] if center else loc[2] + r
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector((loc[0], loc[1], z)))
    return _finish(_from_bmesh(name, bm), material, 0.0, smooth=True)


def dome(name, r, loc=(0, 0, 0), material="Glass", seg=32, rings=16, squash=1.0):
    """Upper hemisphere sitting on loc (open bottom)."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=r)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -1e-4], context="VERTS")
    bmesh.ops.scale(bm, vec=Vector((1, 1, squash)), verts=bm.verts)
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(loc))
    return _finish(_from_bmesh(name, bm), material, 0.0, smooth=True)


def torus(name, major, minor, loc=(0, 0, 0), material="Steel", seg=32, minor_seg=8):
    bm = bmesh.new()
    for i in range(seg):
        a = 2 * math.pi * i / seg
        for j in range(minor_seg):
            b = 2 * math.pi * j / minor_seg
            x = (major + minor * math.cos(b)) * math.cos(a)
            y = (major + minor * math.cos(b)) * math.sin(a)
            z = minor * math.sin(b)
            bm.verts.new((x + loc[0], y + loc[1], z + loc[2]))
    bm.verts.ensure_lookup_table()
    for i in range(seg):
        for j in range(minor_seg):
            a = i * minor_seg + j
            b = ((i + 1) % seg) * minor_seg + j
            c = ((i + 1) % seg) * minor_seg + (j + 1) % minor_seg
            d = i * minor_seg + (j + 1) % minor_seg
            bm.faces.new((bm.verts[a], bm.verts[b], bm.verts[c], bm.verts[d]))
    return _finish(_from_bmesh(name, bm), material, 0.0, smooth=True)


def pipe(name, p0, p1, r=0.12, material="Steel", verts=12):
    p0, p1 = Vector(p0), Vector(p1)
    direction = p1 - p0
    obj = cyl(name, r, direction.length, (0, 0, 0), material, verts, bevel=0.0, smooth=True)
    rot = direction.to_track_quat("Z", "Y").to_matrix().to_4x4()
    obj.data.transform(Matrix.Translation(p0) @ rot)
    return obj


def ring_of(count, radius, fn, start_deg=0.0):
    """Calls fn(i, x, y, angle_rad) around a circle; returns the created objects."""
    out = []
    for i in range(count):
        a = math.radians(start_deg) + 2 * math.pi * i / count
        out.append(fn(i, radius * math.cos(a), radius * math.sin(a), a))
    return out


def bake(obj, rotation=(0, 0, 0), location=(0, 0, 0)):
    """Rotates (XYZ euler, around the world origin) then moves the mesh data itself."""
    obj.data.transform(Matrix.Translation(Vector(location)) @ Euler(rotation, "XYZ").to_matrix().to_4x4())
    return obj


def join(parts):
    """Merges parts into the first one (which keeps its name) and returns it."""
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    return parts[0]


def rotate_z(obj, angle_rad, pivot=(0, 0, 0)):
    pivot = Vector(pivot)
    obj.data.transform(Matrix.Translation(pivot) @ Matrix.Rotation(angle_rad, 4, "Z") @ Matrix.Translation(-pivot))
    return obj


# --------------------------------------------------------------------------------------------
# Shared building parts
# --------------------------------------------------------------------------------------------
def pad(radius=2.3, accent="GlowCyan"):
    """Octagonal foundation every building stands on, with a light strip and hazard trim."""
    parts = [
        cyl("pad", radius, 0.25, (0, 0, 0), "HullDark", verts=8, bevel=0.05),
        cyl("pad_trim", radius * 1.02, 0.06, (0, 0, 0.19), "Hazard", verts=8, bevel=0.0),
        cyl("pad_top", radius * 0.94, 0.04, (0, 0, 0.25), "HullMid", verts=8, bevel=0.0),
    ]
    parts += ring_of(8, radius * 0.97, lambda i, x, y, a: box(f"pad_light{i}", (0.18, 0.18, 0.05), (x, y, 0.25), accent, 0.0), 22.5)
    return parts


def anim(obj, kind, axis="z", speed=1.0):
    """Name a part so Unity animates it: spin around an axis or pulse its emission."""
    # The original part name after "__" keeps names unique; Unity parses only the prefix.
    tag = f"spin_{axis}_{speed:g}" if kind == "spin" else f"{kind}_{speed:g}"
    obj.name = f"ANIM_{tag}__{obj.name}"
    return obj


# --------------------------------------------------------------------------------------------
# Assembly, export, preview
# --------------------------------------------------------------------------------------------
def _apply_modifiers(obj):
    bpy.context.view_layer.objects.active = obj
    for m in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)


def assemble(name, static_parts, animated_parts=()):
    """Joins static parts into one mesh named `name`; animated parts become its children."""
    bpy.ops.object.select_all(action="DESELECT")
    for o in static_parts:
        _apply_modifiers(o)
        o.select_set(True)
    bpy.context.view_layer.objects.active = static_parts[0]
    bpy.ops.object.join()
    root = bpy.context.view_layer.objects.active
    root.name = name
    root.data.name = name

    for o in animated_parts:
        _apply_modifiers(o)
        # Animated parts pivot on their own bounds centre so they spin in place in Unity.
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
        if o.name.startswith("ANIM_sway"):
            # Sway pivots on the root of the stalk, not its middle.
            base_z = min((o.matrix_world @ v.co).z for v in o.data.vertices)
            bpy.context.scene.cursor.location = (o.location.x, o.location.y, base_z)
            bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        o.parent = root
    return root


def triangle_count(root):
    total = 0
    for o in [root, *root.children]:
        total += sum(len(p.vertices) - 2 for p in o.data.polygons)
    return total


def export_fbx(root, path):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for c in root.children:
        c.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="FACE", use_mesh_modifiers=True, add_leaf_bones=False,
        bake_anim=False, path_mode="STRIP")


def render_preview(path, focus=(0, 0, 1.2), distance=9.0, size=(640, 480), samples=24, floor=True):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"

    world = bpy.data.worlds.new("Abyss")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.004, 0.018, 0.030, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
    scene.world = world

    focus = Vector(focus)
    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.lens = 40
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = focus + Vector((distance * 0.62, -distance * 0.62, distance * 0.55))
    cam.rotation_euler = (focus - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam

    key = bpy.data.objects.new("Key", bpy.data.lights.new("Key", "AREA"))
    key.data.energy = 900
    key.data.size = 6
    key.data.color = (0.55, 0.80, 1.0)
    key.location = focus + Vector((-4, -5, 8))
    key.rotation_euler = (focus - key.location).to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(key)

    if floor:
        cyl("preview_floor", 30, 0.01, (0, 0, -0.02), "Sand", verts=48, bevel=0.0)

    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
