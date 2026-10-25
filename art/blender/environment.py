"""Seabed props: terrain tile, rocks, kelp and coral. Seeded so every run is identical."""
import math
import random

import bmesh
from mathutils import Matrix, Vector, noise

from hadal_kit import _finish, _from_bmesh, anim, box, cyl, join, pipe, sphere


def seabed_tile(size=20.0, cuts=40, amplitude=0.35, seed=7):
    """Square sand tile with soft dunes; edges stay at z=0 so tiles line up."""
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=cuts, y_segments=cuts, size=size / 2)
    offset = Vector((seed * 13.1, seed * 7.7, 0))
    for v in bm.verts:
        edge = min(1.0, (size / 2 - max(abs(v.co.x), abs(v.co.y))) / 2.0)
        v.co.z = amplitude * edge * noise.noise(v.co * 0.18 + offset)
    return _finish(_from_bmesh("seabed_tile", bm), "Sand", 0.0, smooth=True)


def rock(name, radius=1.0, seed=1, squash=0.6):
    rng = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=3, radius=radius)
    offset = Vector((rng.uniform(0, 100), rng.uniform(0, 100), rng.uniform(0, 100)))
    for v in bm.verts:
        d = 1.0 + 0.35 * noise.noise(v.co * 1.4 + offset) + 0.12 * noise.noise(v.co * 4.0 + offset)
        v.co = Vector((v.co.x * d, v.co.y * d * rng.uniform(0.85, 1.0), v.co.z * d * squash))
    # Sink the base slightly into the sand.
    min_z = min(v.co.z for v in bm.verts)
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector((0, 0, -min_z - radius * 0.15)))
    return _finish(_from_bmesh(name, bm), "Rock", 0.0, smooth=False)


def kelp(name, height=4.0, seed=1, segments=10):
    """A twisting ribbon; Unity sways it (ANIM_sway)."""
    rng = random.Random(seed)
    bm = bmesh.new()
    prev = None
    bend = Vector((rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), 0))
    for i in range(segments + 1):
        t = i / segments
        width = 0.28 * math.sin(math.pi * min(1.0, t * 1.15 + 0.08))
        twist = t * math.pi * rng.uniform(0.8, 1.6)
        centre = Vector((0, 0, t * height)) + bend * (t * t)
        side = Vector((math.cos(twist), math.sin(twist), 0)) * width
        pair = (bm.verts.new(centre - side), bm.verts.new(centre + side))
        if prev:
            bm.faces.new((prev[0], prev[1], pair[1], pair[0]))
        prev = pair
    obj = _finish(_from_bmesh(name, bm), "Kelp", 0.0, smooth=True)
    return anim(obj, "sway", speed=round(rng.uniform(0.4, 0.8), 2))


def coral(name, seed=1):
    """Branching tube coral with glowing tips."""
    rng = random.Random(seed)
    parts = [sphere(f"{name}_base", 0.35, (0, 0, 0.05), "Coral", seg=12, rings=6)]

    def branch(p0, direction, length, radius, depth):
        p1 = p0 + direction * length
        parts.append(pipe(f"{name}_b{len(parts)}", p0, p1, radius, "Coral", verts=8))
        if depth == 0:
            parts.append(sphere(f"{name}_tip{len(parts)}", radius * 1.4, p1, "GlowAmber", seg=8, rings=4))
            return
        for _ in range(2):
            d = (direction + Vector((rng.uniform(-0.7, 0.7), rng.uniform(-0.7, 0.7), rng.uniform(0.1, 0.5)))).normalized()
            branch(p1, d, length * 0.7, radius * 0.7, depth - 1)

    for k in range(3):
        a = 2 * math.pi * k / 3 + rng.uniform(-0.4, 0.4)
        branch(Vector((0, 0, 0.1)), Vector((math.cos(a) * 0.4, math.sin(a) * 0.4, 1)).normalized(), 0.7, 0.09, 2)
    return join(parts)


def vent(name):
    """Hydrothermal vent chimney: a dark rock stack with a glowing mouth."""
    stack = cyl(name, 0.6, 1.6, (0, 0, 0), "Rock", verts=10, bevel=0.0, r_top=0.3)
    mouth = cyl(f"{name}_mouth", 0.25, 0.08, (0, 0, 1.6), "GlowRed", verts=10, bevel=0.0)
    return join([stack, mouth])


def crystal_cluster(name, seed=8):
    """Hadalite: faceted cyan / violet spikes fanning out of a dark rock."""
    rng = random.Random(seed)
    parts = [rock(f"{name}_rock", 0.6, seed=seed, squash=0.45)]
    for i in range(9):
        a = rng.uniform(0, 2 * math.pi)
        tilt = rng.uniform(0.15, 0.6)
        length = rng.uniform(0.7, 1.5) * (1.2 if i < 3 else 1.0)
        base = Vector((math.cos(a) * rng.uniform(0, 0.35), math.sin(a) * rng.uniform(0, 0.35), 0.15))
        tip = base + Vector((math.cos(a) * tilt, math.sin(a) * tilt, 1.0)).normalized() * length
        spike = cyl(f"{name}_c{i}", rng.uniform(0.09, 0.17), (tip - base).length, (0, 0, 0),
                    "GlowCyan" if i % 3 else "GlowViolet", verts=6, bevel=0.0, r_top=0.0)
        rot = (tip - base).to_track_quat("Z", "Y").to_matrix().to_4x4()
        spike.data.transform(Matrix.Translation(base) @ rot)
        parts.append(spike)
    return join(parts)


def cargo_stack(name):
    """Three shipping containers on a pallet: two below, one rotated on top."""
    parts = [box(f"{name}_pallet", (2.6, 1.3, 0.12), (0, 0, 0), "HullDark", 0.02)]
    for i, (x, colour) in enumerate(((-0.62, "PaintTeal"), (0.62, "Hazard"))):
        parts.append(box(f"{name}_box{i}", (1.15, 1.15, 0.85), (x, 0, 0.12), colour, 0.03))
        parts += [box(f"{name}_rib{i}_{k}", (0.04, 1.18, 0.8), (x - 0.45 + k * 0.3, 0, 0.14), "HullDark", 0.0) for k in range(4)]
    top = box(f"{name}_top", (1.5, 0.95, 0.75), (0, 0, 0.97), "PaintTeal", 0.03)
    parts.append(top)
    parts += [box(f"{name}_strap{k}", (0.05, 1.0, 0.8), (-0.4 + k * 0.8, 0, 0.96), "Rubber", 0.0) for k in range(2)]
    return join(parts)


# id -> (builder, preview focus, preview distance)
PROPS = {
    "seabed_tile": (lambda: ([seabed_tile()], []), (0, 0, 0), 22.0),
    "rock_large": (lambda: ([rock("rock_large", 1.6, seed=3)], []), (0, 0, 0.5), 7.0),
    "rock_medium": (lambda: ([rock("rock_medium", 0.9, seed=11, squash=0.8)], []), (0, 0, 0.4), 4.5),
    "rock_small": (lambda: ([rock("rock_small", 0.45, seed=23, squash=0.7)], []), (0, 0, 0.2), 2.5),
    "kelp_cluster": (lambda: ([cyl("kelp_root", 0.4, 0.15, (0, 0, 0), "Rock", verts=8, bevel=0.0)],
                              [_placed(kelp(f"kelp{i}", 3.0 + i * 0.6, seed=i), i) for i in range(4)]),
                     (0, 0, 2.0), 9.0),
    "coral_branch": (lambda: ([coral("coral_branch", seed=5)], []), (0, 0, 0.8), 4.5),
    "thermal_vent": (lambda: ([vent("thermal_vent")], []), (0, 0, 0.8), 5.0),
    "crystal_cluster": (lambda: ([crystal_cluster("crystal_cluster")], []), (0, 0, 0.8), 4.5),
    "cargo_stack": (lambda: ([cargo_stack("cargo_stack")], []), (0, 0, 0.8), 5.5),
}


def _placed(obj, i):
    a = i * 2.4
    obj.data.transform(Matrix.Translation((0.25 * math.cos(a), 0.25 * math.sin(a), 0.1)))
    return obj
