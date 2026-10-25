"""Builds every HADAL asset headlessly.

    blender -b --factory-startup --python art/blender/generate.py -- \
        --project /opt/hadal [--only core_reactor,rock_large] [--no-preview]

Writes FBX files to Assets/_Hadal/Art/{Buildings,Environment}/, preview PNGs and a lineup
render to artifacts/art-preview/, and a triangle budget report to artifacts/art-preview/report.json.
"""
import argparse
import json
import math
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import hadal_kit as kit  # noqa: E402
from mathutils import Matrix  # noqa: E402
from buildings import BUILDINGS  # noqa: E402
from environment import PROPS, seabed_tile  # noqa: E402

# Mobile budget per building (all LODs share one mesh for now).
TRIANGLE_BUDGET = 6000


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--project", required=True)
    p.add_argument("--only", default="")
    p.add_argument("--no-preview", action="store_true")
    p.add_argument("--samples", type=int, default=24)
    return p.parse_args(argv)


# The reactor sits alone on ring 0 but ring 1 is only 5 m out; its design is drawn larger.
SCALE = {"core_reactor": 0.72}


def make(asset_id, builder):
    """Builds and assembles one asset into the current scene."""
    statics, animated = builder()
    s = SCALE.get(asset_id)
    if s:
        for o in [*statics, *animated]:
            o.data.transform(Matrix.Scale(s, 4))
    return kit.assemble(asset_id, statics, animated)


def build(asset_id, builder):
    kit.reset()
    return make(asset_id, builder)


def main():
    args = parse_args()
    only = {s for s in args.only.split(",") if s}
    out_buildings = os.path.join(args.project, "Assets/_Hadal/Art/Buildings")
    out_env = os.path.join(args.project, "Assets/_Hadal/Art/Environment")
    out_preview = os.path.join(args.project, "artifacts/art-preview")
    for d in (out_buildings, out_env, out_preview):
        os.makedirs(d, exist_ok=True)

    jobs = [(k, (lambda f=f: f()), out_buildings, (0, 0, 1.4), 10.0, True) for k, f in BUILDINGS.items()]
    jobs += [(k, b, out_env, focus, dist, False) for k, (b, focus, dist) in PROPS.items()]

    report = {}
    for asset_id, builder, out_dir, focus, dist, is_building in jobs:
        if only and asset_id not in only:
            continue
        started = time.time()
        root = build(asset_id, builder)
        tris = kit.triangle_count(root)
        kit.export_fbx(root, os.path.join(out_dir, asset_id + ".fbx"))
        if not args.no_preview:
            kit.render_preview(os.path.join(out_preview, asset_id + ".png"), focus, dist, samples=args.samples)
        report[asset_id] = {"triangles": tris, "animated": [c.name for c in root.children],
                            "over_budget": is_building and tris > TRIANGLE_BUDGET}
        print(f"[hadal-art] {asset_id}: {tris} tris, {len(root.children)} animated, {time.time() - started:.1f}s", flush=True)

    if not only and not args.no_preview:
        render_lineup(os.path.join(out_preview, "lineup.png"), args.samples)

    with open(os.path.join(out_preview, "report.json"), "w") as f:
        json.dump(report, f, indent=2)
    over = [k for k, v in report.items() if v["over_budget"]]
    print(f"[hadal-art] done: {len(report)} assets, over budget: {over or 'none'}", flush=True)


def render_lineup(path, samples):
    """All buildings on the seabed in one shot, reactor in the middle."""
    kit.reset()
    kit.assemble("lineup_floor", [seabed_tile(size=40.0, cuts=60)])
    ids = [k for k in BUILDINGS if k != "core_reactor"]
    make("core_reactor", BUILDINGS["core_reactor"])
    for i, asset_id in enumerate(ids):
        root = make(asset_id, BUILDINGS[asset_id])
        a = 2 * math.pi * i / len(ids)
        r = 7.5 if i % 2 == 0 else 12.0
        root.location = (r * math.cos(a), r * math.sin(a), 0)
    for prop_id, xy in (("rock_large", (15, 6)), ("kelp_cluster", (-14, 8)), ("coral_branch", (9, -14)),
                        ("thermal_vent", (-10, -15)), ("rock_medium", (-16, -3))):
        root = make(prop_id, PROPS[prop_id][0])
        root.location = (*xy, 0)
    kit.render_preview(path, focus=(0, 0, 0), distance=34.0, size=(1600, 900), samples=samples, floor=False)


if __name__ == "__main__":
    main()
