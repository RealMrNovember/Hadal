"""Lists what a folder of .glb/.fbx files contains: meshes, triangles, armatures, animations.

    blender -b --factory-startup --python art/blender/inspect_pack.py -- <folder> [report.json]
"""
import json
import os
import sys

import bpy


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if path.lower().endswith(".glb") or path.lower().endswith(".gltf"):
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        bpy.ops.import_scene.fbx(filepath=path)


def describe(path):
    load(path)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    armatures = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    return {
        "meshes": len(meshes),
        "triangles": tris,
        "armature_bones": [len(a.data.bones) for a in armatures],
        "animations": sorted(a.name for a in bpy.data.actions),
        "materials": sorted(m.name for m in bpy.data.materials),
        "images": sorted(i.name for i in bpy.data.images),
    }


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    folder = argv[0]
    out = argv[1] if len(argv) > 1 else None
    report = {}
    for root, _, files in os.walk(folder):
        for name in sorted(files):
            if name.lower().endswith((".glb", ".gltf", ".fbx")):
                path = os.path.join(root, name)
                try:
                    report[os.path.relpath(path, folder)] = describe(path)
                except Exception as exc:  # report and keep going
                    report[os.path.relpath(path, folder)] = {"error": str(exc)}
    text = json.dumps(report, indent=1)
    if out:
        with open(out, "w") as f:
            f.write(text)
    rigged = {k: v for k, v in report.items() if v.get("armature_bones")}
    print(f"[inspect] {len(report)} files, {len(rigged)} rigged", flush=True)
    for k, v in rigged.items():
        print(f"[inspect] {k}: {v['triangles']} tris, bones {v['armature_bones']}, anims {len(v['animations'])}: {', '.join(v['animations'][:30])}", flush=True)


if __name__ == "__main__":
    main()
