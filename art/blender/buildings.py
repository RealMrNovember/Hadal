"""Building designs, one function per catalog id (shared/catalog/catalog.json).

Each returns (static_parts, animated_parts). Footprints stay inside the 2.3 m pad; the core
reactor is drawn larger and scaled down by generate.py.
"""
import math

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
    s = pad(accent="GlowAmber")
    s += [box("hangar", (3.4, 2.6, 1.8), (0, 0.2, 0.29), "HullMid", 0.08),
          box("roof", (3.6, 2.8, 0.2), (0, 0.2, 2.09), "HullDark", 0.04),
          box("door", (2.0, 0.08, 1.3), (0, -1.12, 0.29), "HullDark", 0.02),
          box("door_frame", (2.3, 0.12, 0.12), (0, -1.14, 1.6), "Hazard", 0.0),
          box("vent_a", (0.5, 0.5, 0.5), (-1.1, 0.7, 2.29), "Steel", 0.04),
          box("vent_b", (0.5, 0.5, 0.5), (1.1, 0.7, 2.29), "Steel", 0.04),
          box("door_light", (2.0, 0.1, 0.06), (0, -1.17, 1.65), "GlowAmber", 0.0),
          cyl("crane_post", 0.15, 1.2, (1.3, -0.6, 2.29), "Steel", verts=10, bevel=0.0)]
    arm = anim(box("crane_arm", (1.6, 0.16, 0.16), (0.7, -0.6, 3.4), "Hazard", 0.02), "spin", "z", 20)
    drone = anim(sphere("drone", 0.22, (-0.6, -1.6, 2.4), "GlowAmber", seg=12, rings=6), "pulse", speed=1.5)
    return s, [arm, drone]


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


BUILDINGS = {
    "core_reactor": core_reactor,
    "oxygen_plant": oxygen_plant,
    "food_lab": food_lab,
    "bio_lab": bio_lab,
    "research_center": research_center,
    "drone_factory": drone_factory,
    "submarine_dock": submarine_dock,
    "military_dome": military_dome,
    "shield_generator": shield_generator,
    "radar_center": radar_center,
    "mega_dome": mega_dome,
    "alliance_hub": alliance_hub,
    "decoration_spire": decoration_spire,
}
