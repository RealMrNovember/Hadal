"""The Challenger: HADAL's salvage and crew-transport submersible.

An homage to the real DEEPSEA CHALLENGER (the 2012 solo submersible that dove the Challenger Deep): a
lime-green vertical "torpedo" of syntactic foam standing on end, the pilot sphere at the bottom, a long
vertical LED light bar in front, ducted thrusters on the flanks, a manipulator arm and a stereo camera
boom low at the front. Built procedurally (own work, no third-party mesh, no logos or markings) and
scaled down to game size: 2.6 m tall instead of 7.3 m, so a crew member fits the sphere and the vehicle
reads next to buildings whose pads are 2.3 m in radius.

Orientation: origin on the floor under the hull, up +Z, the viewport looks along -Y (Unity +Z after the
FBX axis conversion), so a Unity LookRotation(direction) points the pilot where it travels.

    challenger(detail)   -> (static parts, animated parts) for kit.assemble / buildings.py
"""
import math

import bmesh
from mathutils import Matrix, Vector

import hadal_kit as kit

HEIGHT = 2.6
BODY_RX = 0.30          # half-width across (X)
BODY_RY = 0.24          # half-depth front to back (Y)
BODY_Y = 0.10           # the foam body sits behind the sphere
SPHERE_R = 0.33
SPHERE_C = (0.0, -0.14, 0.42)
# Unity-side seat for a passenger (CrewMember rides here); mirrored in ChallengerVehicle.SeatLocal.
SEAT = (0.0, -0.14, 0.18)

# (z, scale) along the body: narrow foot that meets the sphere, straight flanks, rounded crown.
BODY_PROFILE = [(0.26, 0.0), (0.27, 0.42), (0.36, 0.66), (0.50, 0.88), (0.68, 1.0), (1.60, 1.0),
                (2.12, 0.95), (2.32, 0.86), (2.45, 0.72), (2.54, 0.52), (2.585, 0.30), (2.60, 0.0)]


def _superellipse(n_seg, rx, ry, power=2.6):
    """Points of a rounded-rectangle-like ellipse (the foam body is boxier than an ellipse)."""
    pts = []
    for i in range(n_seg):
        a = 2 * math.pi * i / n_seg
        c, s = math.cos(a), math.sin(a)
        x = rx * math.copysign(abs(c) ** (2 / power), c)
        y = ry * math.copysign(abs(s) ** (2 / power), s)
        pts.append((x, y))
    return pts


def loft(name, profile, rx, ry, centre=(0.0, 0.0), seg=24, material="PaintLime", power=2.6):
    """Closed vertical loft through (z, scale) rings; a scale of 0 makes a pole."""
    bm = bmesh.new()
    base = _superellipse(seg, rx, ry, power)
    rings = []
    for z, s in profile:
        if s <= 1e-6:
            rings.append([bm.verts.new((centre[0], centre[1], z))])
        else:
            rings.append([bm.verts.new((centre[0] + x * s, centre[1] + y * s, z)) for x, y in base])
    for lower, upper in zip(rings, rings[1:]):
        for i in range(seg):
            j = (i + 1) % seg
            if len(lower) == 1:
                bm.faces.new((lower[0], upper[j], upper[i]))
            elif len(upper) == 1:
                bm.faces.new((lower[i], lower[j], upper[0]))
            else:
                bm.faces.new((lower[i], lower[j], upper[j], upper[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return kit._finish(kit._from_bmesh(name, bm), material, 0.0, smooth=True)


def band(name, z, h, scale=1.025, seg=24, material="HullDark"):
    """A thin seam ring around the body at height z."""
    return loft(name, [(z, scale), (z + h, scale)], BODY_RX, BODY_RY, (0, BODY_Y), seg, material)


def pilot_sphere(seg, rings):
    """Steel sphere with a forward-and-down viewport; returns (shell, glass)."""
    view = Vector((0.0, -1.0, -0.35)).normalized()
    cos_cut = math.cos(math.radians(56))

    def build(keep_view):
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=SPHERE_R)
        doomed = [f for f in bm.faces if (f.calc_center_median().normalized().dot(view) > cos_cut) != keep_view]
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
        bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(SPHERE_C))
        return bm

    shell = kit._finish(kit._from_bmesh("pilot_sphere", build(False)), "Steel", 0.0, smooth=True)
    glass = kit._finish(kit._from_bmesh("viewport", build(True)), "Glass", 0.0, smooth=True)
    # Viewport rim: a steel collar where the glass meets the shell.
    rim = kit.torus("viewport_rim", SPHERE_R * math.sin(math.radians(56)), 0.022, (0, 0, 0), "Steel", seg=seg, minor_seg=6)
    centre = Vector(SPHERE_C) + view * (SPHERE_R * math.cos(math.radians(56)))
    rim.data.transform(Matrix.Translation(centre) @ view.to_track_quat("Z", "Y").to_matrix().to_4x4())
    return [shell, glass, rim]


def thruster(name, p0, p1, r, seg):
    """Ducted thruster: white shroud with a dark throat, from p0 to p1."""
    shroud = kit.pipe(name, p0, p1, r, "PaintWhite", verts=seg)
    a, b = Vector(p0), Vector(p1)
    d = (b - a).normalized() * 0.012
    throat = kit.pipe(name + "_throat", a - d, b + d, r * 0.78, "HullDark", verts=seg)
    return [shroud, throat]


def strut(name, p0, p1, w=0.035, material="HullDark"):
    return kit.pipe(name, p0, p1, w, material, verts=6)


def pilot_figure():
    """Seated crew silhouette inside the sphere (hidden in Unity while a real crew member rides)."""
    sx, sy, sz = SEAT
    parts = [
        kit.box("pilot_torso", (0.17, 0.13, 0.20), (sx, sy + 0.03, sz + 0.06), "PaintTeal", 0.03),
        kit.box("pilot_legs", (0.15, 0.17, 0.07), (sx, sy - 0.07, sz + 0.02), "PaintTeal", 0.02),
        kit.sphere("pilot_head", 0.072, (sx, sy + 0.01, sz + 0.36), "PaintWhite", seg=12, rings=6),
        kit.box("pilot_visor", (0.10, 0.03, 0.04), (sx, sy - 0.055, sz + 0.34), "GlowCyan", 0.0),
    ]
    figure = kit.join(parts)
    figure.name = "pilot"
    return figure


def challenger(detail=1.0, with_pilot=True):
    """Static parts and animated/named children of one Challenger (detail 1 = vehicle, ~0.6 = docked prop)."""
    seg = max(12, int(round(28 * detail / 2)) * 2)
    small = max(8, int(round(14 * detail)))
    s = []

    # Foam body, seams and a white crown cap band.
    s.append(loft("hull", BODY_PROFILE, BODY_RX, BODY_RY, (0, BODY_Y), seg, "PaintLime"))
    s.append(band("seam_low", 1.05, 0.025, seg=seg))
    s.append(band("seam_high", 1.92, 0.025, seg=seg))
    s.append(loft("crown", [(2.20, 0.915), (2.28, 0.885)], BODY_RX * 1.02, BODY_RY * 1.02, (0, BODY_Y), seg, "PaintWhite"))

    # Pilot sphere, viewport, hatch boss on top of the sphere where it enters the body.
    s += pilot_sphere(seg, max(8, seg // 2))
    s.append(kit.cyl("hatch", 0.11, 0.05, (SPHERE_C[0], SPHERE_C[1] + 0.06, SPHERE_C[2] + SPHERE_R - 0.03), "Steel", verts=small, bevel=0.0))

    # Skid frame and ballast: four legs with pads, two dark ballast blocks on the flanks.
    for i, (x, y) in enumerate(((-0.26, -0.32), (0.26, -0.32), (-0.24, 0.26), (0.24, 0.26))):
        s.append(strut(f"leg{i}", (x, y, 0.04), (x * 0.8, y * 0.6 + BODY_Y * 0.4, 0.36), 0.025, "Steel"))
        s.append(kit.cyl(f"foot{i}", 0.06, 0.04, (x, y, 0.0), "HullDark", verts=8, bevel=0.0))
    s.append(strut("skid_front", (-0.26, -0.32, 0.06), (0.26, -0.32, 0.06), 0.02, "Steel"))
    s.append(strut("skid_back", (-0.24, 0.26, 0.06), (0.24, 0.26, 0.06), 0.02, "Steel"))
    for i, x in enumerate((-1, 1)):
        s.append(kit.box(f"ballast{i}", (0.10, 0.22, 0.24), (x * (BODY_RX + 0.03), BODY_Y + 0.02, 0.40), "HullDark", 0.02))

    # Vertical LED light bar on struts in front of the body (the glow is an animated child).
    bar_y = BODY_Y - BODY_RY - 0.17
    s.append(kit.box("lightbar_housing", (0.11, 0.07, 1.36), (0, bar_y + 0.02, 0.92), "PaintWhite", 0.015))
    for i, z in enumerate((1.02, 1.6, 2.18)):
        s.append(strut(f"bar_strut{i}", (0, bar_y + 0.04, z), (0, BODY_Y - BODY_RY + 0.04, z), 0.02, "Steel"))
    lightbar = kit.anim(kit.box("lightbar", (0.07, 0.02, 1.26), (0, bar_y - 0.022, 0.97), "GlowWhite", 0.0), "pulse", speed=0.5)

    # Thrusters: two big lateral pairs, two vertical ones low at the back.
    for i, x in enumerate((-1, 1)):
        xo = x * (BODY_RX + 0.13)
        s += thruster(f"thr_mid{i}", (xo, BODY_Y - 0.10, 1.38), (xo, BODY_Y + 0.10, 1.38), 0.085, small)
        s.append(strut(f"thr_mid_arm{i}", (x * (BODY_RX - 0.02), BODY_Y, 1.38), (xo, BODY_Y, 1.38), 0.03, "HullDark"))
        xo2 = x * (BODY_RX + 0.10)
        s += thruster(f"thr_top{i}", (xo2, BODY_Y - 0.08, 2.05), (xo2, BODY_Y + 0.08, 2.05), 0.065, small)
        s.append(strut(f"thr_top_arm{i}", (x * (BODY_RX - 0.04), BODY_Y, 2.05), (xo2, BODY_Y, 2.05), 0.025, "HullDark"))
        xv = x * 0.16
        s += thruster(f"thr_vert{i}", (xv, BODY_Y + BODY_RY + 0.08, 0.62), (xv, BODY_Y + BODY_RY + 0.08, 0.78), 0.07, small)

    # Manipulator arm (right of the viewport) and stereo camera boom (left), both low at the front.
    joints = [(0.20, -0.36, 0.26), (0.30, -0.56, 0.34), (0.24, -0.70, 0.16)]
    s.append(strut("arm_upper", joints[0], joints[1], 0.032, "Steel"))
    s.append(strut("arm_fore", joints[1], joints[2], 0.026, "Steel"))
    for i, p in enumerate(joints):
        s.append(kit.sphere(f"arm_joint{i}", 0.045 if i < 2 else 0.035, p, "HullDark", seg=small, rings=max(4, small // 2)))
    wx, wy, wz = joints[2]
    s.append(strut("claw_a", (wx, wy, wz), (wx - 0.05, wy - 0.06, wz - 0.08), 0.014, "Hazard"))
    s.append(strut("claw_b", (wx, wy, wz), (wx + 0.05, wy - 0.06, wz - 0.08), 0.014, "Hazard"))

    s.append(strut("cam_boom", (-0.24, -0.12, 0.70), (-0.38, -0.50, 0.86), 0.024, "Steel"))
    s.append(kit.box("cam_pod", (0.20, 0.10, 0.09), (-0.38, -0.52, 0.82), "HullDark", 0.015))
    for i, dx in enumerate((-0.05, 0.05)):
        s.append(kit.pipe(f"cam_lens{i}", (-0.38 + dx, -0.56, 0.865), (-0.38 + dx, -0.60, 0.865), 0.026, "Glass", verts=small))

    # Crown: antenna mast; its recovery strobe is animated.
    s.append(kit.cyl("mast", 0.015, 0.16, (0, BODY_Y + 0.05, HEIGHT - 0.02), "Steel", verts=6, bevel=0.0))
    strobe = kit.anim(kit.sphere("strobe", 0.035, (0, BODY_Y + 0.05, HEIGHT + 0.16), "GlowWhite", seg=10, rings=5), "pulse", speed=1.0)

    # Cabin: warm interior light behind the pilot (glows through the viewport).
    cabin_light = kit.anim(kit.sphere("cabin_light", 0.06, (0, SPHERE_C[1] + 0.20, SPHERE_C[2] + 0.12), "GlowWarm", seg=10, rings=5), "pulse", speed=0.25)

    animated = [lightbar, strobe, cabin_light]
    if with_pilot:
        animated.append(pilot_figure())
    return s, animated
