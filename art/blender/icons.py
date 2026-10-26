"""HADAL HUD icons: small 3D props rendered to transparent PNGs for the uGUI header, side buttons and
bottom bar (docs/26_HUD.md).

    /opt/blender/blender -b --factory-startup --python art/blender/icons.py -- --project /opt/hadal [--only a,b]

Writes Assets/_Hadal/Resources/Icons/<id>.png (256x256, transparent). Icons use their own saturated
"toy" materials (not the building palette) so they read at 60 px on a phone, with a dark outline.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Vector

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import hadal_kit as kit  # noqa: E402

SIZE = 256

# name: (base RGB, metallic, roughness, emission RGB or None, emission strength)
ICON_MATS = {
    "Cyan":    ((0.02, 0.45, 0.70), 0.0, 0.25, (0.05, 0.60, 0.90), 0.5),
    "Bubble":  ((0.08, 0.50, 0.85), 0.0, 0.08, (0.05, 0.40, 0.80), 0.25),
    "Amber":   ((1.00, 0.45, 0.02), 0.0, 0.30, (1.00, 0.40, 0.02), 0.6),
    "Gold":    ((0.95, 0.65, 0.15), 1.0, 0.25, None, 0.0),
    "Steel":   ((0.62, 0.66, 0.70), 1.0, 0.22, None, 0.0),
    "Dark":    ((0.08, 0.10, 0.12), 0.6, 0.40, None, 0.0),
    "Green":   ((0.10, 0.55, 0.12), 0.0, 0.45, (0.05, 0.30, 0.05), 0.2),
    "Leaf":    ((0.25, 0.80, 0.30), 0.0, 0.50, None, 0.0),
    "Red":     ((0.85, 0.12, 0.10), 0.0, 0.40, None, 0.0),
    "Ribbon":  ((0.95, 0.80, 0.20), 0.3, 0.35, None, 0.0),
    "Violet":  ((0.35, 0.08, 0.85), 0.0, 0.15, (0.40, 0.10, 0.90), 0.5),
    "Blue":    ((0.10, 0.35, 0.85), 0.2, 0.35, None, 0.0),
    "White":   ((0.80, 0.83, 0.86), 0.0, 0.40, None, 0.0),
    "Paper":   ((0.85, 0.72, 0.45), 0.0, 0.70, None, 0.0),
    "Wood":    ((0.45, 0.25, 0.10), 0.0, 0.65, None, 0.0),
    "Glass":   ((0.70, 0.95, 1.00), 0.0, 0.03, None, 0.0),
    "Teal":    ((0.05, 0.50, 0.45), 0.3, 0.35, None, 0.0),
    "Sack":    ((0.45, 0.28, 0.12), 0.0, 0.85, None, 0.0),
    "Card1":   ((0.85, 0.30, 0.15), 0.0, 0.45, None, 0.0),
    "Card2":   ((0.20, 0.45, 0.90), 0.0, 0.45, None, 0.0),
    "Card3":   ((0.95, 0.70, 0.15), 0.0, 0.45, None, 0.0),
}

_mats = {}


def m(name):
    if name in _mats:
        return _mats[name]
    base, metallic, rough, emit, strength = ICON_MATS[name]
    mat = bpy.data.materials.new("ICON_" + name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*base, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = rough
    if emit:
        bsdf.inputs["Emission Color"].default_value = (*emit, 1.0)
        bsdf.inputs["Emission Strength"].default_value = strength
    if name == "Glass":
        bsdf.inputs["Transmission Weight"].default_value = 1.0
        bsdf.inputs["IOR"].default_value = 1.2
    _mats[name] = mat
    return mat


def paint(obj, name):
    obj.data.materials.clear()
    obj.data.materials.append(m(name))
    return obj


# Every helper builds with the kit (geometry) and then repaints with an icon material.
def cyl(name, r, h, loc=(0, 0, 0), mat="Steel", verts=32, r_top=None, bevel=0.02, smooth=True):
    return paint(kit.cyl(name, r, h, loc, "HullMid", verts, bevel, r_top, smooth), mat)


def box(name, size, loc=(0, 0, 0), mat="Steel", bevel=0.03):
    return paint(kit.box(name, size, loc, "HullMid", bevel), mat)


def ball(name, r, loc=(0, 0, 0), mat="Bubble"):
    return paint(kit.sphere(name, r, loc, "GlowCyan", 32, 16), mat)


def ring(name, major, minor, loc=(0, 0, 0), mat="Gold"):
    return paint(kit.torus(name, major, minor, loc, "Steel", 48, 12), mat)


def prism(name, points, depth, loc=(0, 0, 0), mat="Amber", bevel=0.02):
    """Extrudes a 2D outline (x, z pairs) along Y, centred on loc."""
    bm = bmesh.new()
    verts = [bm.verts.new((x, -depth / 2, z)) for x, z in points]
    face = bm.faces.new(verts)
    ext = bmesh.ops.extrude_face_region(bm, geom=[face])
    moved = [e for e in ext["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=moved, vec=Vector((0, depth, 0)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(loc))
    obj = kit._from_bmesh(name, bm)
    return paint(kit._finish(obj, "HullMid", bevel), mat)


def crystal(name, r, h, loc=(0, 0, 0), mat="Violet", tilt=(0, 0, 0)):
    body = cyl(name + "_b", r, h * 0.7, loc, mat, verts=6, bevel=0.0, smooth=False)
    tip = cyl(name + "_t", r, h * 0.3, (loc[0], loc[1], loc[2] + h * 0.7), mat, verts=6, r_top=0.0, bevel=0.0, smooth=False)
    obj = kit.join([body, tip])
    obj.rotation_euler = Euler(tilt)
    return obj


def rot(obj, x=0.0, y=0.0, z=0.0):
    obj.rotation_euler = Euler((math.radians(x), math.radians(y), math.radians(z)))
    return obj


# ---------------------------------------------------------------------------------------------
# Icons
# ---------------------------------------------------------------------------------------------
def res_oxygen():
    ball("b1", 0.55, (0, 0, 0.55))
    ball("b2", 0.32, (0.62, -0.1, 1.15))
    ball("b3", 0.2, (-0.45, -0.1, 1.25))


def res_energy():
    bolt = [(-0.15, 1.6), (0.55, 1.6), (0.15, 0.95), (0.6, 0.95), (-0.35, -0.2), (0.0, 0.7), (-0.45, 0.7)]
    prism("bolt", bolt, 0.3, mat="Amber")


def res_food():
    cyl("can", 0.55, 0.95, (0, 0, 0), "Steel")
    cyl("label", 0.565, 0.5, (0, 0, 0.22), "Red", bevel=0.0)
    cyl("lid", 0.5, 0.06, (0, 0, 0.95), "Steel", bevel=0.01)
    ball("bean", 0.18, (0.45, -0.55, 0.15), "Amber")


def res_biomass():
    ball("blob", 0.55, (0, 0, 0.5), "Green")
    for i, ang in enumerate((0, 120, 240)):
        leaf = box("leaf%d" % i, (0.18, 0.06, 0.85), (0, 0, 0.6), "Leaf", 0.03)
        rot(leaf, 35, 0, ang)


def res_titanium():
    for i, (x, z) in enumerate(((-0.42, 0), (0.42, 0), (0, 0.42))):
        prism("ingot%d" % i, [(-0.38, 0), (0.38, 0), (0.28, 0.4), (-0.28, 0.4)], 1.1, (x, 0, z), "Steel", 0.03)


def res_hadalite():
    crystal("c1", 0.32, 1.6, (0, 0, 0), "Violet")
    crystal("c2", 0.22, 1.0, (0.45, 0.1, 0), "Violet", (0, 0.4, 0))
    crystal("c3", 0.2, 0.9, (-0.45, 0.1, 0), "Violet", (0, -0.45, 0))


def power():
    prism("shield", [(-0.7, 1.4), (0.7, 1.4), (0.7, 0.6), (0, -0.2), (-0.7, 0.6)], 0.25, mat="Gold")
    prism("inner", [(-0.5, 1.25), (0.5, 1.25), (0.5, 0.65), (0, 0.05), (-0.5, 0.65)], 0.3, (0, -0.02, 0), "Blue")
    ball("gem", 0.2, (0, -0.25, 0.8), "Cyan")


def nav_heroes():
    for i, (mat, ang, x) in enumerate((("Card1", 18, -0.35), ("Card2", 0, 0), ("Card3", -18, 0.35))):
        card = box("card%d" % i, (0.85, 0.06, 1.25), (0, 0, 0), mat, 0.04)
        rot(card, 0, ang, 0)
        card.location = (x, -i * 0.08, 0.1)
    ball("emblem", 0.18, (0.35, -0.35, 0.7), "Gold")


def nav_world():
    ball("globe", 0.75, (0, 0, 0.8), "Teal")
    ring("lat", 0.82, 0.04, (0, 0, 0.8), "Gold")
    rot(ring("lon", 0.82, 0.04, (0, 0, 0), "Gold"), 90, 0, 30).location = (0, 0, 0.8)
    cyl("stand", 0.4, 0.12, (0, 0, -0.1), "Wood")


def side_alliance():
    prism("shield", [(-0.7, 1.4), (0.7, 1.4), (0.7, 0.6), (0, -0.25), (-0.7, 0.6)], 0.25, mat="Blue")
    prism("star", [(0, 1.15), (0.12, 0.85), (0.42, 0.85), (0.18, 0.65), (0.27, 0.35), (0, 0.52), (-0.27, 0.35), (-0.18, 0.65), (-0.42, 0.85), (-0.12, 0.85)],
          0.32, (0, -0.02, 0), "Gold", 0.0)


def side_mail():
    box("env", (1.5, 0.18, 1.0), (0, 0, 0), "White", 0.04)
    prism("flap", [(-0.74, 1.0), (0.74, 1.0), (0, 0.4)], 0.22, (0, -0.03, 0), "Paper", 0.0)
    ball("seal", 0.14, (0, -0.15, 0.45), "Red")


def side_bag():
    sack = ball("sack", 0.7, (0, 0, 0.62), "Sack")
    sack.scale = (1.0, 0.9, 0.85)
    cyl("neck", 0.25, 0.3, (0, 0, 1.15), "Sack", r_top=0.38)
    ring("tie", 0.27, 0.06, (0, 0, 1.22), "Red")


def left_research():
    ball("bulb", 0.6, (0, 0, 0.6), "Glass")
    cyl("neck", 0.18, 0.6, (0, 0, 1.05), "Glass")
    cyl("cork", 0.2, 0.15, (0, 0, 1.62), "Wood")
    ball("liquid", 0.5, (0, 0, 0.5), "Green")


def left_expand():
    plate = cyl("plate", 0.85, 0.2, (0, 0, 0), "Dark", verts=6, bevel=0.03, smooth=False)
    rot(plate, 90, 0, 0)
    plate.location = (0, 0.1, 0.85)
    box("plus_v", (0.25, 0.25, 1.0), (0, -0.1, 0.35), "Cyan", 0.04)
    box("plus_h", (1.0, 0.25, 0.25), (0, -0.1, 0.72), "Cyan", 0.04)


def left_help():
    rot(ring("r1", 0.45, 0.12, (0, 0, 0), "Gold"), 90, 0, 0).location = (-0.3, 0, 0.7)
    rot(ring("r2", 0.45, 0.12, (0, 0, 0), "Cyan"), 90, 0, 0).location = (0.3, 0, 0.7)


def left_search():
    ring("lens_rim", 0.5, 0.1, (0, 0, 0), "Gold")
    rot(_lens := cyl("lens", 0.45, 0.04, (0, 0, -0.02), "Glass", bevel=0.0), 0, 0, 0)
    for part in ("lens_rim", "lens"):
        o = bpy.data.objects[part]
        o.rotation_euler = Euler((math.radians(90), 0, 0))
        o.location = (-0.15, 0, 0.9)
    handle = cyl("handle", 0.1, 0.8, (0, 0, 0), "Wood")
    rot(handle, 0, 140, 0)
    handle.location = (0.25, 0, 0.55)


def left_home():
    cyl("base", 0.8, 0.3, (0, 0, 0), "Dark", verts=8, smooth=False)
    paint(kit.dome("dome", 0.65, (0, 0, 0.3), "Glass", 32, 16), "Glass")
    ball("core", 0.22, (0, 0, 0.55), "Amber")


def event_pod():
    cyl("pod", 0.45, 1.3, (0, 0, 0.1), "Steel")
    ball("top", 0.45, (0, 0, 1.4), "Steel")
    ball("bottom", 0.45, (0, 0, 0.1), "Steel")
    box("window", (0.5, 0.2, 0.9), (0, -0.38, 0.35), "Glass", 0.08)
    ring("band", 0.47, 0.05, (0, 0, 1.0), "Cyan")


def event_gift():
    box("box", (1.2, 1.2, 0.9), (0, 0, 0), "Red", 0.04)
    box("lid", (1.32, 1.32, 0.25), (0, 0, 0.9), "Red", 0.04)
    box("rib1", (0.2, 1.36, 1.17), (0, 0, 0), "Ribbon", 0.02)
    box("rib2", (1.36, 0.2, 1.17), (0, 0, 0), "Ribbon", 0.02)
    rot(ring("bow1", 0.25, 0.07, (0, 0, 0), "Ribbon"), 90, 0, 35).location = (-0.2, 0, 1.3)
    rot(ring("bow2", 0.25, 0.07, (0, 0, 0), "Ribbon"), 90, 0, -35).location = (0.2, 0, 1.3)


def event_war():
    for i, ang in enumerate((40, -40)):
        blade = box("blade%d" % i, (0.38, 0.12, 1.3), (0, 0, 0.0), "Steel", 0.04)
        guard = box("guard%d" % i, (0.7, 0.18, 0.16), (0, 0, -0.1), "Gold", 0.03)
        grip = cyl("grip%d" % i, 0.1, 0.45, (0, 0, -0.55), "Wood")
        sword = kit.join([blade, guard, grip])
        rot(sword, 0, ang, 0)
        sword.location = (0, -i * 0.1, 0.4)


def shop():
    box("chest", (1.4, 0.9, 0.7), (0, 0, 0), "Wood", 0.04)
    box("band", (1.44, 0.94, 0.12), (0, 0, 0.45), "Gold", 0.02)
    crystal("g1", 0.2, 0.8, (-0.25, 0, 0.6), "Violet", (0, -0.3, 0))
    crystal("g2", 0.24, 1.0, (0.1, 0, 0.6), "Violet")
    crystal("g3", 0.18, 0.7, (0.42, 0, 0.6), "Violet", (0, 0.35, 0))


def quest():
    paper = box("paper", (1.1, 0.05, 1.2), (0, 0, 0.1), "Paper", 0.02)
    rot(paper, -10, 0, 0)
    for i, z in enumerate((0.05, 1.3)):
        r = cyl("roll%d" % i, 0.13, 1.4, (0, 0, -0.7), "Wood")
        rot(r, 0, 90, 0)
        r.location = (0, 0, z)
    rot(box("quill", (0.08, 0.04, 1.1), (0, 0, 0), "White", 0.02), 0, -30, 0).location = (0.45, -0.15, 0.4)


def queue_build():
    head = box("head", (0.9, 0.35, 0.35), (0, 0, 1.1), "Steel", 0.03)
    handle = cyl("handle", 0.09, 1.2, (0, 0, 0), "Wood")
    hammer = kit.join([handle, head])
    rot(hammer, 0, 35, 0)


def queue_drone():
    box("body", (0.8, 0.8, 0.3), (0, 0, 0.5), "Dark", 0.08)
    ball("eye", 0.15, (0, -0.4, 0.65), "Cyan")
    for i, (x, y) in enumerate(((-0.6, -0.6), (0.6, -0.6), (-0.6, 0.6), (0.6, 0.6))):
        ring("rotor%d" % i, 0.25, 0.04, (x, y, 0.75), "Cyan")


def chat():
    prism("bubble", [(-0.8, 1.3), (0.8, 1.3), (0.8, 0.45), (-0.1, 0.45), (-0.45, 0.1), (-0.4, 0.45), (-0.8, 0.45)], 0.25, mat="White")
    for i, x in enumerate((-0.4, 0, 0.4)):
        ball("dot%d" % i, 0.1, (x, -0.15, 0.88), "Blue")


def avatar():
    ball("head", 0.45, (0, 0, 1.15), "Paper")
    cyl("body", 0.65, 0.7, (0, 0, 0.1), "Teal", r_top=0.4)
    cyl("helmet", 0.5, 0.25, (0, 0, 1.2), "Steel", r_top=0.42)
    box("visor", (0.6, 0.08, 0.18), (0, -0.4, 1.05), "Cyan", 0.03)


ICONS = {f.__name__: f for f in (
    res_oxygen, res_energy, res_food, res_biomass, res_titanium, res_hadalite, power,
    nav_heroes, nav_world, side_alliance, side_mail, side_bag,
    left_research, left_expand, left_help, left_search, left_home,
    event_pod, event_gift, event_war, shop, quest, queue_build, queue_drone, chat, avatar,
)}


# ---------------------------------------------------------------------------------------------
# Rendering
# ---------------------------------------------------------------------------------------------
def frame_and_render(path):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    scene.render.resolution_x = scene.render.resolution_y = SIZE
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"

    # Outline like the genre's sticker-style icons.
    scene.render.use_freestyle = True
    scene.render.line_thickness_mode = "ABSOLUTE"
    scene.render.line_thickness = 2.4
    settings = scene.view_layers[0].freestyle_settings
    for old in list(settings.linesets):
        settings.linesets.remove(old)
    lineset = settings.linesets.new("Outline")
    lineset.select_by_visibility = True
    lineset.select_by_edge_types = True
    lineset.select_silhouette = True
    lineset.select_border = True
    lineset.select_crease = False
    lineset.linestyle = bpy.data.linestyles.new("OutlineStyle")
    lineset.linestyle.color = (0.02, 0.03, 0.05)
    lineset.linestyle.thickness = 2.4

    world = bpy.data.worlds.new("IconWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.35, 0.42, 0.5, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6
    scene.world = world

    # Fit an orthographic camera to everything, seen from the front, slightly above.
    objs = [o for o in scene.objects if o.type == "MESH"]
    bpy.context.view_layer.update()
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        for corner in o.bound_box:
            p = o.matrix_world @ Vector(corner)
            lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
            hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
    center = (lo + hi) / 2
    extent = max(hi.x - lo.x, hi.z - lo.z, hi.y - lo.y)

    cam_data = bpy.data.cameras.new("IconCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = extent * 1.22
    cam = bpy.data.objects.new("IconCam", cam_data)
    scene.collection.objects.link(cam)
    direction = Vector((0.35, -1.0, 0.45)).normalized()
    cam.location = center + direction * (extent * 4)
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam

    def light(name, energy, loc, color):
        lamp = bpy.data.objects.new(name, bpy.data.lights.new(name, "AREA"))
        lamp.data.energy = energy * extent * extent
        lamp.data.size = extent * 2
        lamp.data.color = color
        lamp.location = center + Vector(loc) * extent
        lamp.rotation_euler = (center - lamp.location).to_track_quat("-Z", "Y").to_euler()
        scene.collection.objects.link(lamp)

    light("Key", 140, (-1.2, -2.0, 2.2), (1.0, 0.96, 0.9))
    light("Fill", 45, (2.0, -1.5, 0.5), (0.6, 0.85, 1.0))
    light("Rim", 90, (0.5, 2.0, 1.5), (0.7, 0.9, 1.0))

    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project = argv[argv.index("--project") + 1] if "--project" in argv else os.getcwd()
    only = set(argv[argv.index("--only") + 1].split(",")) if "--only" in argv else None
    out = os.path.join(project, "Assets", "_Hadal", "Resources", "Icons")
    os.makedirs(out, exist_ok=True)

    for name, build in ICONS.items():
        if only and name not in only:
            continue
        kit.reset()
        _mats.clear()
        build()
        frame_and_render(os.path.join(out, name + ".png"))
        print("[icons] %s" % name)


main()
