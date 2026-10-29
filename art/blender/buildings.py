"""Building designs, one function per catalog id (shared/catalog/catalog.json).

Each returns (static_parts, animated_parts). Footprints stay inside the 2.3 m pad; the core
reactor is drawn larger and scaled down by generate.py.
"""
import math

from mathutils import Matrix

from challenger import challenger
from hadal_kit import (anim, bake, box, cyl, dome, join, pad, pipe, ring_of, rotate_z, sphere, torus)


def core_reactor():
    s = [cyl("base", 3.2, 0.3, (0, 0, 0), "HullDark", verts=12, bevel=0.06),
         cyl("trim", 3.25, 0.07, (0, 0, 0.22), "Hazard", verts=12, bevel=0.0),
         cyl("housing", 1.9, 1.6, (0, 0, 0.3), "HullMid", verts=16, bevel=0.06),
         cyl("collar", 2.05, 0.25, (0, 0, 1.9), "Steel", verts=16, bevel=0.03),
         dome("glass", 1.8, (0, 0, 2.15), "Glass", squash=1.1)]
    s += ring_of(6, 2.35, lambda i, x, y, a: rotate_z(box(f"fin{i}", (1.1, 0.22, 1.5), (2.35, 0, 0.3), "HullDark"), a))
    s += ring_of(6, 2.0, lambda i, x, y, a: box(f"strip{i}", (0.12, 0.12, 1.2), (x, y, 0.5), "GlowCyan", 0.0), 30)
    s += ring_of(4, 0.9, lambda i, x, y, a: pipe(f"conduit{i}", (x, y, 2.15), (x * 0.25, y * 0.25, 3.2), 0.08, "Steel"), 45)
    core = anim(sphere("core", 0.75, (0, 0, 3.0), "GlowCyan"), "pulse", speed=0.8)
    ring = anim(torus("orbit", 1.15, 0.06, (0, 0, 3.0), "GlowCyan"), "spin", "z", 30)
    return s, [core, ring]


def oxygen_plant():
    s = pad(accent="GlowCyan")
    for i, (x, y) in enumerate([(-0.9, -0.6), (0.9, -0.6), (0, 0.95)]):
        s += [cyl(f"tank{i}", 0.75, 2.2, (x, y, 0.29), "HullMid", verts=20, bevel=0.05),
              dome(f"tank{i}_cap", 0.75, (x, y, 2.49), "Steel", seg=20, rings=8, squash=0.45),
              cyl(f"tank{i}_band", 0.78, 0.12, (x, y, 1.2), "Hazard", verts=20, bevel=0.0),
              box(f"tank{i}_gauge", (0.18, 0.05, 0.6), (x, y - 0.76, 0.9), "GlowCyan", 0.0)]
    s += [cyl("fan_housing", 0.85, 0.5, (0, 0, 2.9), "HullDark", verts=24, bevel=0.04),
          cyl("fan_grill", 0.7, 0.04, (0, 0, 3.4), "Steel", verts=24, bevel=0.0),
          pipe("feed_a", (-0.9, -0.6, 2.5), (0, 0, 2.95), 0.1),
          pipe("feed_b", (0.9, -0.6, 2.5), (0, 0, 2.95), 0.1),
          pipe("feed_c", (0, 0.95, 2.5), (0, 0, 2.95), 0.1)]
    blades = [rotate_z(box(f"blade{i}", (1.2, 0.18, 0.04), (0, 0, 3.46), "Steel", 0.0), math.pi * i / 3) for i in range(3)]
    return s, [anim(join(blades), "spin", "z", 240)]


def food_lab():
    s = pad(accent="GlowGreen")
    s += [box("floor", (3.8, 2.6, 0.2), (0, 0, 0.29), "HullMid", 0.04)]
    # Greenhouse vault: a half cylinder lying along X.
    vault = cyl("vault", 1.3, 3.6, (0, 0, 0), "Glass", verts=24, bevel=0.0, smooth=True)
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(vault.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.x < -1e-4], context="VERTS")
    bm.to_mesh(vault.data)
    bm.free()
    # Lay the half cylinder along X with its flat side down.
    s.append(bake(vault, (0, math.pi / 2, 0), (-1.8, 0, 0.49)))
    s += [box(f"rib{i}", (0.1, 2.75, 0.1), (-1.6 + i * 0.8, 0, 1.6), "Steel", 0.0) for i in range(5)]
    for row in range(3):
        y = -0.7 + row * 0.7
        s += [box(f"tray{row}", (3.2, 0.42, 0.3), (0, y, 0.49), "HullDark", 0.02),
              box(f"plants{row}", (3.0, 0.32, 0.35), (0, y, 0.79), "Plant", 0.06)]
    light = anim(box("growlight", (3.2, 0.12, 0.06), (0, 0, 1.75), "GlowGreen", 0.0), "pulse", speed=0.3)
    return s, [light]


def bio_lab():
    s = pad(accent="GlowGreen")
    s += [cyl("core", 1.1, 1.0, (0, 0, 0.29), "HullMid", verts=16, bevel=0.05),
          dome("roof", 1.15, (0, 0, 1.29), "HullDark", seg=16, rings=8, squash=0.6)]
    vats = []
    for i, a in enumerate((0, 2 * math.pi / 3, 4 * math.pi / 3)):
        x, y = 1.75 * math.cos(a), 1.75 * math.sin(a)
        s += [cyl(f"vat{i}_base", 0.55, 0.3, (x, y, 0.29), "HullDark", verts=16, bevel=0.03),
              cyl(f"vat{i}_glass", 0.45, 1.5, (x, y, 0.59), "Glass", verts=16, bevel=0.0, smooth=True),
              cyl(f"vat{i}_cap", 0.55, 0.2, (x, y, 2.09), "Steel", verts=16, bevel=0.03),
              pipe(f"vat{i}_pipe", (x * 0.6, y * 0.6, 1.0), (x, y, 2.15), 0.08)]
        vats.append(anim(cyl(f"vat{i}_fluid", 0.38, 1.3, (x, y, 0.62), "GlowGreen", verts=16, bevel=0.0), "pulse", speed=0.5 + 0.2 * i))
    return s, vats


def research_center():
    s = pad(accent="GlowCyan")
    s += [cyl("tower", 0.95, 2.6, (0, 0, 0.29), "HullMid", verts=12, bevel=0.06, r_top=0.75),
          cyl("deck", 1.35, 0.25, (0, 0, 2.89), "HullDark", verts=12, bevel=0.04),
          dome("observatory", 1.1, (0, 0, 3.14), "Glass", seg=24, rings=10),
          cyl("mast", 0.07, 1.4, (0, 0, 4.2), "Steel", verts=8, bevel=0.0),
          sphere("beacon", 0.14, (0, 0, 5.65), "GlowRed")]
    s += ring_of(4, 1.25, lambda i, x, y, a: box(f"buttress{i}", (0.3, 0.3, 2.2), (x, y, 0.29), "HullDark"), 45)
    s += ring_of(12, 0.98, lambda i, x, y, a: box(f"window{i}", (0.12, 0.12, 0.5), (x, y, 1.6), "GlowAmber", 0.0))
    ring = anim(torus("scanner", 1.55, 0.05, (0, 0, 3.35), "GlowCyan", seg=40), "spin", "z", 45)
    return s, [ring]


def drone_factory():
    """Challenger Hangar (id kept for saves): a docking gantry over a moonpool with a Challenger
    standing in its cradle, a back hangar shell, a control hut and a service crane."""
    s = pad(accent="GlowAmber")
    dock = (0.22, -0.15, 0.29)
    s += [cyl("moonpool_rim", 0.78, 0.05, (dock[0], dock[1], 0.27), "HullDark", verts=16, bevel=0.0),
          cyl("moonpool", 0.66, 0.02, (dock[0], dock[1], 0.29), "GlowCyan", verts=16, bevel=0.0),
          box("tower_l", (0.24, 0.24, 3.0), (-0.66, 0.52, 0.29), "HullMid", 0.03),
          box("tower_r", (0.24, 0.24, 3.0), (1.10, 0.52, 0.29), "HullMid", 0.03),
          box("beam", (2.05, 0.26, 0.20), (0.22, 0.52, 3.29), "Hazard", 0.02),
          box("winch", (0.30, 0.34, 0.22), (0.22, 0.20, 3.07), "Steel", 0.02),
          pipe("cable", (0.22, 0.04, 3.07), (0.22, 0.04, 2.92), 0.015, "Rubber"),
          box("hangar_back", (3.3, 0.85, 1.7), (0.0, 1.20, 0.29), "HullMid", 0.06),
          box("hangar_roof", (3.5, 1.05, 0.16), (0.0, 1.18, 1.99), "HullDark", 0.03),
          box("hangar_glow", (2.6, 0.06, 0.08), (0.0, 0.76, 1.70), "GlowAmber", 0.0),
          box("hut", (0.95, 0.95, 0.95), (-1.42, -0.62, 0.29), "HullMid", 0.05),
          box("hut_roof", (1.05, 1.05, 0.10), (-1.42, -0.62, 1.24), "HullDark", 0.02),
          box("hut_window", (0.70, 0.06, 0.26), (-1.42, -1.10, 0.80), "GlowAmber", 0.0),
          box("ballast_spare_a", (0.30, 0.22, 0.22), (1.55, 0.55, 0.29), "HullDark", 0.02),
          box("ballast_spare_b", (0.30, 0.22, 0.22), (1.55, 0.20, 0.29), "HullDark", 0.02),
          cyl("crane_post", 0.13, 1.4, (1.55, -0.95, 0.29), "Steel", verts=10, bevel=0.0)]
    arm = anim(box("crane_arm", (1.3, 0.14, 0.14), (1.05, -0.95, 1.62), "Hazard", 0.02), "spin", "z", 20)

    # The docked Challenger, a little smaller and lighter than the travelling vehicle.
    sub, sub_anim = challenger(0.6, with_pilot=True)
    place = Matrix.Translation(dock) @ Matrix.Scale(0.95, 4)
    for o in [*sub, *sub_anim]:
        o.data.transform(place)
    for o in sub_anim:
        if not o.name.startswith("ANIM_"):
            o.name = "docked_" + o.name   # the parked pilot is plain decor here
    s += [o for o in sub_anim if not o.name.startswith("ANIM_")]
    return s, [arm, *[o for o in sub_anim if o.name.startswith("ANIM_")]]


def submarine_dock():
    s = pad(accent="GlowCyan")
    s += [box("bay_left", (0.5, 3.6, 0.8), (-1.3, 0, 0.29), "HullMid", 0.05),
          box("bay_right", (0.5, 3.6, 0.8), (1.3, 0, 0.29), "HullMid", 0.05),
          box("moonpool", (2.1, 3.4, 0.05), (0, 0, 0.27), "GlowCyan", 0.0)]
    for i, y in enumerate((-1.3, 0, 1.3)):
        s.append(box(f"arch{i}", (3.1, 0.25, 0.25), (0, y, 2.3), "HullDark", 0.03))
        s.append(box(f"arch{i}_l", (0.25, 0.25, 1.3), (-1.3, y, 1.09), "HullDark", 0.03))
        s.append(box(f"arch{i}_r", (0.25, 0.25, 1.3), (1.3, y, 1.09), "HullDark", 0.03))
    # Docked submarine.
    hull = bake(cyl("sub_hull", 0.55, 2.6, (0, 0, 0), "Hazard", verts=20, bevel=0.0, smooth=True),
                (math.pi / 2, 0, 0), (0, 1.3, 0.85))
    s += [hull,
          sphere("sub_nose", 0.55, (0, -1.3, 0.85), "Hazard"),
          box("sub_tower", (0.4, 0.8, 0.5), (0, 0.2, 1.35), "HullDark", 0.05),
          sphere("sub_window", 0.2, (0, -1.7, 0.95), "GlowAmber", seg=12, rings=6)]
    prop = anim(box("sub_prop", (0.7, 0.08, 0.08), (0, 1.35, 0.85), "Steel", 0.0), "spin", "y", 300)
    return s, [prop]


def military_dome():
    s = pad(accent="GlowRed")
    s += [cyl("wall", 2.0, 0.7, (0, 0, 0.29), "HullDark", verts=12, bevel=0.06),
          dome("armour", 2.0, (0, 0, 0.99), "HullMid", seg=12, rings=6, squash=0.55)]
    s += ring_of(12, 2.02, lambda i, x, y, a: box(f"slit{i}", (0.1, 0.1, 0.25), (x, y, 0.5), "GlowRed", 0.0))
    turret = anim(box("turret", (1.0, 0.9, 0.5), (0, 0, 2.05), "Steel", 0.06), "spin", "z", 15)
    return s, [turret]


def shield_generator():
    s = pad(accent="GlowCyan")
    s += [cyl("base", 1.2, 0.6, (0, 0, 0.29), "HullDark", verts=6, bevel=0.06),
          cyl("pylon", 0.45, 2.4, (0, 0, 0.89), "HullMid", verts=6, bevel=0.04, r_top=0.25)]
    s += ring_of(3, 0.9, lambda i, x, y, a: pipe(f"emitter{i}", (x, y, 0.9), (x * 0.3, y * 0.3, 3.1), 0.09))
    orb = anim(sphere("orb", 0.4, (0, 0, 3.55), "GlowCyan"), "pulse", speed=1.2)
    halo = anim(torus("halo", 1.0, 0.04, (0, 0, 3.55), "GlowCyan", seg=40), "spin", "x", 25)
    return s, [orb, halo]


def radar_center():
    s = pad(accent="GlowAmber")
    s += [box("hut", (1.8, 1.5, 1.0), (0.4, 0.3, 0.29), "HullMid", 0.06),
          cyl("mast", 0.2, 2.2, (-0.8, -0.6, 0.29), "Steel", verts=10, bevel=0.0),
          box("hut_window", (1.2, 0.05, 0.25), (0.4, -0.46, 0.85), "GlowAmber", 0.0)]
    dish = bake(dome("dish", 1.0, (0, 0, 0), "Steel", seg=20, rings=6, squash=0.35),
                (math.radians(110), 0, 0), (-0.8, -0.6, 2.7))
    return s, [anim(dish, "spin", "z", 60)]


def mega_dome():
    s = [cyl("ring", 2.55, 0.35, (0, 0, 0), "HullDark", verts=24, bevel=0.06),
         cyl("ring_trim", 2.6, 0.06, (0, 0, 0.28), "Hazard", verts=24, bevel=0.0),
         dome("glass", 2.45, (0, 0, 0.35), "Glass", seg=32, rings=14, squash=0.9)]
    for i in range(6):
        rib = torus(f"rib{i}", 2.47, 0.05, (0, 0, 0), "Steel", seg=32, minor_seg=6)
        s.append(bake(rib, (math.pi / 2, 0, math.pi * i / 6), (0, 0, 0.35)))
    s += [box("interior_a", (1.2, 1.0, 0.9), (-0.6, 0.4, 0.35), "HullMid", 0.05),
          box("interior_b", (0.9, 1.3, 1.3), (0.8, -0.3, 0.35), "HullMid", 0.05),
          box("interior_lights", (1.0, 0.1, 0.1), (-0.6, -0.11, 1.0), "GlowAmber", 0.0)]
    beacon = anim(sphere("apex", 0.18, (0, 0, 2.6), "GlowCyan"), "pulse", speed=0.6)
    return s, [beacon]


def hydroponic_farm():
    s = pad(accent="GlowGreen")
    s += [box("deck", (3.6, 3.0, 0.18), (0, 0, 0.29), "HullMid", 0.04)]
    columns = []
    for i, (x, y) in enumerate([(-1.05, -0.7), (0, -0.7), (1.05, -0.7)]):
        s += [cyl(f"col{i}_base", 0.48, 0.25, (x, y, 0.47), "HullDark", verts=16, bevel=0.03),
              cyl(f"col{i}_glass", 0.42, 2.2, (x, y, 0.72), "Glass", verts=16, bevel=0.0, smooth=True),
              cyl(f"col{i}_cap", 0.48, 0.18, (x, y, 2.92), "Steel", verts=16, bevel=0.03)]
        # Stacked grow trays inside each column.
        s += [cyl(f"col{i}_tray{t}", 0.36, 0.12, (x, y, 0.95 + t * 0.6), "Plant", verts=12, bevel=0.0) for t in range(3)]
        columns.append(anim(cyl(f"col{i}_light", 0.08, 2.0, (x, y, 0.8), "GlowGreen", verts=8, bevel=0.0), "pulse", speed=0.4 + 0.15 * i))
    s += [cyl("reservoir", 0.75, 1.1, (0, 0.95, 0.47), "HullMid", verts=20, bevel=0.05),
          dome("reservoir_cap", 0.75, (0, 0.95, 1.57), "Steel", seg=20, rings=8, squash=0.35),
          cyl("reservoir_band", 0.78, 0.1, (0, 0.95, 1.0), "Hazard", verts=20, bevel=0.0)]
    s += [pipe(f"feed{i}", (0, 0.95, 1.4), (x, -0.7, 2.95), 0.07) for i, x in enumerate((-1.05, 0, 1.05))]
    return s, columns


def agriculture_dome():
    s = [cyl("ring", 2.55, 0.35, (0, 0, 0), "HullDark", verts=24, bevel=0.06),
         cyl("ring_trim", 2.6, 0.06, (0, 0, 0.28), "GlowGreen", verts=24, bevel=0.0),
         dome("glass", 2.45, (0, 0, 0.35), "Glass", seg=32, rings=14, squash=0.8)]
    for i in range(4):
        rib = torus(f"rib{i}", 2.47, 0.05, (0, 0, 0), "Steel", seg=32, minor_seg=6)
        s.append(bake(rib, (math.pi / 2, 0, math.pi * i / 4), (0, 0, 0.35)))
    # Terraced fields rising to the middle.
    for t, (r, h) in enumerate(((2.1, 0.2), (1.55, 0.45), (1.0, 0.7))):
        s += [cyl(f"terrace{t}", r, h, (0, 0, 0.35), "HullMid", verts=24, bevel=0.03),
              cyl(f"crop{t}", r - 0.12, 0.12, (0, 0, 0.35 + h), "Plant", verts=24, bevel=0.0)]
    s += [cyl("silo", 0.3, 1.4, (0, 0, 1.17), "Steel", verts=12, bevel=0.03)]
    light = anim(torus("growlight", 1.6, 0.05, (0, 0, 1.9), "GlowGreen", seg=32), "pulse", speed=0.35)
    return s, [light]


def alliance_hub():
    s = pad(accent="GlowAmber")
    s += [cyl("plaza", 2.3, 0.15, (0, 0, 0.29), "HullMid", verts=24, bevel=0.03),
          cyl("spire", 0.5, 3.8, (0, 0, 0.44), "HullDark", verts=8, bevel=0.04, r_top=0.15),
          sphere("crown", 0.3, (0, 0, 4.4), "GlowAmber")]
    s += ring_of(4, 1.6, lambda i, x, y, a: cyl(f"pillar{i}", 0.15, 1.6, (x, y, 0.44), "Steel", verts=8, bevel=0.0), 45)
    s += ring_of(4, 1.6, lambda i, x, y, a: box(f"banner{i}", (0.05, 0.45, 0.8), (x, y, 1.1), "Hazard", 0.0), 45)
    emblem = anim(torus("emblem", 0.8, 0.07, (0, 0, 3.0), "GlowAmber", seg=32), "spin", "z", 20)
    return s, [emblem]


def decoration_spire():
    s = pad(radius=1.6, accent="GlowCyan")
    s += [cyl("spire", 0.35, 3.2, (0, 0, 0.29), "HullMid", verts=6, bevel=0.03, r_top=0.05)]
    s += ring_of(3, 0.6, lambda i, x, y, a: pipe(f"strut{i}", (x, y, 0.3), (0, 0, 2.2), 0.06))
    lights = [anim(torus("halo_low", 0.55, 0.03, (0, 0, 1.2), "GlowCyan", seg=24), "pulse", speed=0.7),
              anim(torus("halo_high", 0.35, 0.03, (0, 0, 2.3), "GlowCyan", seg=24), "pulse", speed=0.9)]
    return s, lights


def armor_yard():
    """Armored divers: a squat hangar with hard suits on racks and a test chamber."""
    s = pad(accent="GlowRed")
    s += [box("hall", (2.6, 1.8, 1.1), (0, 0.35, 0.29), "HullMid", 0.08),
          box("roof", (2.8, 2.0, 0.22), (0, 0.35, 1.39), "HullDark", 0.05),
          box("roof_stripe", (2.82, 0.2, 0.05), (0, -0.2, 1.62), "Hazard", 0.0),
          box("door", (1.2, 0.06, 0.8), (0, -0.56, 0.4), "GlowRed", 0.0),
          cyl("chamber", 0.55, 1.2, (-1.3, -1.1, 0.29), "Steel", verts=16, bevel=0.04),
          dome("chamber_cap", 0.55, (-1.3, -1.1, 1.49), "Glass", seg=16, rings=6, squash=0.6)]
    for i, x in enumerate((0.55, 1.05, 1.55)):
        s += [cyl(f"suit{i}", 0.2, 0.55, (x, -1.25, 0.29), "Hazard", verts=10, bevel=0.03),
              sphere(f"helmet{i}", 0.17, (x, -1.25, 0.98), "Steel", seg=12, rings=6),
              sphere(f"visor{i}", 0.08, (x, -1.4, 0.98), "GlowAmber", seg=8, rings=4)]
    beacon = anim(sphere("beacon", 0.14, (0.9, 0.35, 1.75), "GlowRed", seg=10, rings=5), "pulse", speed=1.0)
    return s, [beacon]


def harpoon_yard():
    """Harpooners: a firing line with a launcher and target plates down the range."""
    s = pad(accent="GlowAmber")
    s += [box("bench", (0.9, 2.6, 0.45), (-1.2, 0, 0.29), "HullMid", 0.05),
          box("rail", (2.8, 0.12, 0.08), (0.2, 0, 0.62), "Steel", 0.0)]
    for i, y in enumerate((-0.8, 0.0, 0.8)):
        s += [cyl(f"post{i}", 0.06, 1.2, (1.45, y, 0.29), "Steel", verts=8, bevel=0.0),
              torus(f"target{i}", 0.3, 0.05, (1.45, y, 1.3), "Hazard", seg=20, minor_seg=6),
              sphere(f"bull{i}", 0.1, (1.45, y, 1.3), "GlowRed", seg=8, rings=4)]
    gun = bake(cyl("launcher", 0.12, 1.4, (0, 0, 0), "Steel", verts=12, bevel=0.0), (0, 1.5708, 0), (-1.0, 0, 1.0))
    tip = sphere("tip", 0.09, (-0.25, 0, 1.0), "GlowAmber", seg=8, rings=4)
    turret = anim(box("mount", (0.5, 0.5, 0.35), (-1.2, 0, 0.74), "HullDark", 0.04), "spin", "z", 10)
    return s + [gun, tip], [turret]


def arc_yard():
    """Arc troopers: insulated coil towers with a live arc between them."""
    s = pad(accent="GlowViolet")
    s += [box("hut", (1.4, 1.2, 0.9), (-0.9, 0.9, 0.29), "HullMid", 0.06),
          box("hut_glow", (0.9, 0.05, 0.2), (-0.9, 0.28, 0.8), "GlowViolet", 0.0)]
    for i, (x, y) in enumerate([(0.9, -0.9), (0.9, 0.9), (-0.9, -0.9)]):
        s += [cyl(f"coil{i}", 0.3, 1.6, (x, y, 0.29), "HullDark", verts=12, bevel=0.03, r_top=0.18)]
        s += [torus(f"wind{i}_{k}", 0.27 - k * 0.03, 0.04, (x, y, 0.6 + k * 0.4), "Hazard", seg=16, minor_seg=6) for k in range(3)]
    orbs = [anim(sphere(f"orb{i}", 0.2, (x, y, 2.05), "GlowViolet", seg=12, rings=6), "pulse", speed=2.0 + i * 0.4)
            for i, (x, y) in enumerate([(0.9, -0.9), (0.9, 0.9), (-0.9, -0.9)])]
    spark = anim(pipe("arc", (0.9, -0.9, 2.05), (0.9, 0.9, 2.05), 0.03, "GlowViolet", verts=6), "pulse", speed=3.0)
    return s, orbs + [spark]


def driller_yard():
    """Drillers: a derrick with a spinning drill head over a test bore."""
    s = pad(accent="GlowGreen")
    s += [cyl("bore", 0.7, 0.12, (0, 0, 0.29), "GlowGreen", verts=16, bevel=0.0),
          box("winch", (0.9, 0.7, 0.6), (-1.3, 0.9, 0.29), "HullMid", 0.05),
          box("crates", (0.8, 0.8, 0.5), (1.3, 1.0, 0.29), "Hazard", 0.04)]
    for i, (x, y) in enumerate([(-0.8, -0.8), (0.8, -0.8), (0.8, 0.8), (-0.8, 0.8)]):
        s.append(pipe(f"leg{i}", (x, y, 0.29), (0, 0, 2.9), 0.07, "Steel", verts=8))
    s += [box("crown", (0.5, 0.5, 0.25), (0, 0, 2.95), "HullDark", 0.03)]
    drill = anim(cyl("drill", 0.22, 1.6, (0, 0, 0.6), "Steel", verts=8, bevel=0.0, r_top=0.05), "spin", "z", 180)
    return s, [drill]


def med_bay():
    """Med Bay: a pressurised ward with a glowing cross, beds under glass and a cryo cylinder."""
    s = pad(accent="GlowGreen")
    s += [box("ward", (2.4, 1.6, 0.9), (0, 0.4, 0.29), "HullMid", 0.08),
          box("roof", (2.5, 1.7, 0.18), (0, 0.4, 1.19), "HullDark", 0.04),
          dome("skylight", 0.6, (0.6, 0.4, 1.37), "Glass", seg=16, rings=6, squash=0.5),
          box("cross_v", (0.18, 0.06, 0.62), (-0.6, -0.42, 0.75), "GlowGreen", 0.0),
          box("cross_h", (0.62, 0.06, 0.18), (-0.6, -0.42, 0.75), "GlowGreen", 0.0),
          box("door", (0.5, 0.05, 0.6), (0.55, -0.41, 0.6), "Glass", 0.0),
          cyl("cryo", 0.35, 1.2, (-1.4, -1.0, 0.29), "Steel", verts=14, bevel=0.03),
          cyl("cryo_glow", 0.36, 0.08, (-1.4, -1.0, 0.95), "GlowCyan", verts=14, bevel=0.0)]
    for i, x in enumerate((0.2, 0.9)):
        s += [box(f"bed{i}", (0.5, 0.9, 0.2), (x, -1.2, 0.29), "Steel", 0.03),
              box(f"sheet{i}", (0.46, 0.6, 0.06), (x, -1.3, 0.49), "Hazard", 0.0)]
    beacon = anim(sphere("beacon", 0.13, (-0.6, 0.4, 1.4), "GlowGreen", seg=10, rings=5), "pulse", speed=0.8)
    return s, [beacon]


BUILDINGS = {
    "core_reactor": core_reactor,
    "oxygen_plant": oxygen_plant,
    "food_lab": food_lab,
    "bio_lab": bio_lab,
    "research_center": research_center,
    "drone_factory": drone_factory,
    "submarine_dock": submarine_dock,
    "military_dome": military_dome,
    "armor_yard": armor_yard,
    "harpoon_yard": harpoon_yard,
    "arc_yard": arc_yard,
    "driller_yard": driller_yard,
    "med_bay": med_bay,
    "shield_generator": shield_generator,
    "radar_center": radar_center,
    "mega_dome": mega_dome,
    "hydroponic_farm": hydroponic_farm,
    "agriculture_dome": agriculture_dome,
    "alliance_hub": alliance_hub,
    "decoration_spire": decoration_spire,
}
