"""Image -> 3D mesh (.glb) on an 8 GB GPU (doku / doku2: RTX 5060), for HADAL asset blockouts.

Models (see Dockerfile.3d for the two images):
  hunyuan-mini        tencent/Hunyuan3D-2mini, 0.6B shape DiT, 50 steps     (image hadal-3d-hunyuan)
  hunyuan-mini-turbo  same, step-distilled + FlashVDM decoder, 5 steps     (image hadal-3d-hunyuan)
  trellis             microsoft/TRELLIS-image-large, stage-wise GPU offload (image hadal-3d-trellis)

    docker run --rm --gpus all -v ~/hadal-gpu:/work hadal-3d-hunyuan \
        python /work/repo/art/gpu/image_to_3d.py --model hunyuan-mini-turbo --texture \
            --image /work/out/3d/input/diver_robot_rgba.png --out /work/out/3d/hunyuan-mini-turbo

    docker run --rm --gpus all -v ~/hadal-gpu:/work hadal-3d-trellis \
        python /work/repo/art/gpu/image_to_3d.py --model trellis \
            --image /work/out/3d/input/diver_robot_rgba.png --out /work/out/3d/trellis

Per run it writes into --out:
  <name>_shape.glb       raw shape (Hunyuan: untextured; TRELLIS: mesh only)
  <name>_textured.glb    with --texture: Hunyuan3D-Paint / TRELLIS gaussian bake (time-boxed, --texture-timeout)
  <name>_lowpoly.glb     shape decimated to --lowpoly faces, flat palette colours projected from the input image
                         (the "mesh-only + our own colouring" path for the stylized low-poly look)
  *_preview.png          3-view orthographic previews (matplotlib, no OpenGL needed)
  <name>_stats.json      wall time per stage, peak VRAM (torch allocator), triangle counts
Model weights are cached in /work/models (HF_HOME, TORCH_HOME).
"""
import argparse
import gc
import json
import os
import signal
import time

import numpy as np
import torch
import trimesh
from PIL import Image

os.environ.setdefault("TORCH_HOME", os.path.join(os.environ.get("HF_HOME", "/work/models"), "torch"))
# Fragmentation matters at the 8 GB edge (TRELLIS' FlexiCubes decode); read at the first CUDA allocation.
os.environ.setdefault("PYTORCH_CUDA_ALLOC_CONF", "expandable_segments:True")
# nvdiffrast (TRELLIS bake) JIT-compiles its plugin on first use: cache it on the volume, compile lean.
os.environ.setdefault("TORCH_EXTENSIONS_DIR", "/work/models/torch_extensions")
os.environ["MAX_JOBS"] = os.environ.get("JIT_MAX_JOBS", "2")


def peak_gb():
    return round(torch.cuda.max_memory_allocated() / 2**30, 2)


def as_trimesh(obj):
    """Scene/Trimesh -> single Trimesh (keeps visuals where possible)."""
    if isinstance(obj, trimesh.Scene):
        return obj.dump(concatenate=True)
    return obj


# ------------------------------------------------------------------------------------------ preview

def face_colors(mesh):
    """Per-face RGB in 0..1: from face/vertex colours or by sampling the texture at face UV centroids."""
    vis = mesh.visual
    if isinstance(vis, trimesh.visual.TextureVisuals) and vis.uv is not None:
        mat = vis.material
        img = getattr(mat, "baseColorTexture", None) or getattr(mat, "image", None)
        if img is not None:
            tex = np.asarray(img.convert("RGB"), dtype=np.float32) / 255.0
            uv = vis.uv[mesh.faces].mean(axis=1) % 1.0
            px = (uv[:, 0] * (tex.shape[1] - 1)).astype(int)
            py = ((1.0 - uv[:, 1]) * (tex.shape[0] - 1)).astype(int)
            return tex[py, px]
    if isinstance(vis, trimesh.visual.ColorVisuals) and vis.kind in ("face", "vertex"):
        return vis.face_colors[:, :3].astype(np.float32) / 255.0
    return np.full((len(mesh.faces), 3), 0.72, dtype=np.float32)


def decimate(mesh, target_faces):
    """Quadric edge collapse (pymeshlab); geometry only."""
    import pymeshlab

    ms = pymeshlab.MeshSet()
    ms.add_mesh(pymeshlab.Mesh(mesh.vertices, mesh.faces))
    ms.meshing_decimation_quadric_edge_collapse(targetfacenum=target_faces, preservenormal=True,
                                                preservetopology=True, qualitythr=0.5)
    m = ms.current_mesh()
    return trimesh.Trimesh(m.vertex_matrix(), m.face_matrix(), process=True)


def render_preview(mesh, path, title="", max_faces=80000):
    """Front / three-quarter / side orthographic views, flat Lambert shading, painter's algorithm."""
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.collections import PolyCollection

    mesh = as_trimesh(mesh)
    untextured = isinstance(mesh.visual, trimesh.visual.ColorVisuals) and mesh.visual.kind is None
    if untextured and len(mesh.faces) > max_faces:  # decimate for the preview only
        mesh = decimate(mesh, max_faces)
    cols = face_colors(mesh)
    faces = mesh.faces
    v = mesh.vertices - mesh.bounds.mean(axis=0)
    v = v / np.abs(v).max()
    light = np.array([0.4, 0.6, 1.0])
    light /= np.linalg.norm(light)

    fig, axes = plt.subplots(1, 3, figsize=(12, 4.4), facecolor="#d9dde3")
    for ax, azim in zip(axes, (0, 45, 90)):
        a = np.radians(azim)
        rot = np.array([[np.cos(a), 0, np.sin(a)], [0, 1, 0], [-np.sin(a), 0, np.cos(a)]])
        r = v @ rot.T
        tri = r[faces]
        n = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
        n /= np.linalg.norm(n, axis=1, keepdims=True) + 1e-12
        shade = 0.35 + 0.65 * np.abs(n @ light)
        order = np.argsort(tri[:, :, 2].mean(axis=1))  # far -> near (camera on +Z)
        pc = PolyCollection(tri[order][:, :, :2], facecolors=np.clip(cols[order] * shade[order, None], 0, 1),
                            edgecolors="none", antialiased=False)
        ax.add_collection(pc)
        ax.set_xlim(-1.05, 1.05)
        ax.set_ylim(-1.05, 1.05)
        ax.set_aspect("equal")
        ax.axis("off")
        ax.set_title(f"azim {azim}", fontsize=9)
    fig.suptitle(title, fontsize=10)
    fig.tight_layout()
    fig.savefig(path, dpi=110)
    plt.close(fig)


# ------------------------------------------------------------------------------- low-poly + palette

def lowpoly_palette(mesh, image_rgba, target_faces=3000, colors=6):
    """Decimate (quadric edge collapse) and give each face one of `colors` flat colours, sampled by
    orthographic front projection of the (front-view) input image and quantised with k-means.
    Stand-in for 'decimate + recolour in Blender' to judge whether shape-only output is enough."""
    from scipy.cluster.vq import kmeans2

    low = decimate(as_trimesh(mesh), target_faces)

    rgba = np.asarray(image_rgba.convert("RGBA"), dtype=np.float32) / 255.0
    ys, xs = np.nonzero(rgba[:, :, 3] > 0.5)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    c = low.triangles_center
    lo, hi = low.bounds
    u = (c[:, 0] - lo[0]) / (hi[0] - lo[0])
    w = (c[:, 1] - lo[1]) / (hi[1] - lo[1])
    px = np.clip((x0 + u * (x1 - x0)).astype(int), 0, rgba.shape[1] - 1)
    py = np.clip((y1 - w * (y1 - y0)).astype(int), 0, rgba.shape[0] - 1)
    samp = rgba[py, px, :3]
    # Background pixels (outside the silhouette) fall back to the mean object colour.
    bg = rgba[py, px, 3] < 0.5
    samp[bg] = rgba[ys, xs, :3].mean(axis=0)
    palette, label = kmeans2(samp.astype(np.float64), colors, seed=0, minit="++")
    fc = (np.clip(palette[label], 0, 1) * 255).astype(np.uint8)
    low.visual = trimesh.visual.ColorVisuals(low, face_colors=np.c_[fc, np.full(len(fc), 255, np.uint8)])
    return low


# ------------------------------------------------------------------------------------------ models

def run_hunyuan(args, image, stats):
    from hy3dgen.shapegen import FaceReducer

    if args.shape_glb:  # texture-only run: the paint models alone need most of a 15 GB host's RAM
        mesh = as_trimesh(trimesh.load(args.shape_glb))
        stats["shape_tris"] = int(len(mesh.faces))
    else:
        mesh = hunyuan_shape(args, image, stats)

    def texture():
        from hy3dgen.texgen import Hunyuan3DPaintPipeline
        t = time.time()
        paint = Hunyuan3DPaintPipeline.from_pretrained("tencent/Hunyuan3D-2", subfolder=args.paint_subfolder)
        if args.paint_offload:
            paint.enable_model_cpu_offload()
        paint.config.render_size = args.paint_res
        paint.config.texture_size = args.paint_res
        paint.render.set_default_render_resolution(args.paint_res)
        paint.render.set_default_texture_resolution(args.paint_res)
        stats["paint_load_s"] = round(time.time() - t, 1)
        torch.cuda.reset_peak_memory_stats()
        t = time.time()
        try:
            return paint(FaceReducer()(mesh.copy(), max_facenum=args.paint_faces), image=image)
        finally:
            stats["texture_s"] = round(time.time() - t, 1)
            stats["texture_peak_vram_gb"] = peak_gb()

    return mesh, (texture if args.texture else None)


def hunyuan_shape(args, image, stats):
    from hy3dgen.shapegen import DegenerateFaceRemover, FloaterRemover, Hunyuan3DDiTFlowMatchingPipeline

    turbo = args.model == "hunyuan-mini-turbo"
    t = time.time()
    pipe = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(
        "tencent/Hunyuan3D-2mini",
        subfolder="hunyuan3d-dit-v2-mini-turbo" if turbo else "hunyuan3d-dit-v2-mini",
        use_safetensors=True, variant="fp16", device="cuda")
    if turbo:
        pipe.enable_flashvdm(topk_mode="merge")
    stats["load_s"] = round(time.time() - t, 1)

    t = time.time()
    torch.cuda.reset_peak_memory_stats()
    mesh = pipe(image=image, num_inference_steps=args.steps or (5 if turbo else 50),
                octree_resolution=args.octree, num_chunks=args.chunks,
                generator=torch.manual_seed(args.seed), output_type="trimesh")[0]
    mesh = FloaterRemover()(mesh)
    mesh = DegenerateFaceRemover()(mesh)
    stats["shape_s"] = round(time.time() - t, 1)
    stats["shape_peak_vram_gb"] = peak_gb()
    stats["shape_tris"] = int(len(mesh.faces))
    del pipe
    torch.cuda.empty_cache()
    return mesh


def force_xformers_cutlass():
    """xformers 0.0.31 dispatches to its FlashAttention-3 (Hopper) kernel on sm_120 and dies with 'invalid
    argument'; pin the portable CUTLASS kernels instead (TRELLIS calls xops.memory_efficient_attention)."""
    import xformers.ops as xops
    from xformers.ops import fmha

    original = xops.memory_efficient_attention

    def cutlass_attention(*a, **kw):
        kw.setdefault("op", (fmha.cutlass.FwOp, fmha.cutlass.BwOp))
        return original(*a, **kw)

    xops.memory_efficient_attention = cutlass_attention


def run_trellis(args, image, stats):
    force_xformers_cutlass()
    from trellis.pipelines import TrellisImageTo3DPipeline
    from trellis.pipelines.base import Pipeline
    from trellis.utils import postprocessing_utils

    t = time.time()
    pipe = TrellisImageTo3DPipeline.from_pretrained("microsoft/TRELLIS-image-large")
    # 8 GB card: weights stay on the CPU; each stage moves only its own model(s) to the GPU.
    Pipeline.device = property(lambda self: torch.device("cuda"))
    for m in pipe.models.values():
        m.cpu().eval()
    stats["load_s"] = round(time.time() - t, 1)

    def on_gpu(names, fn, *a, **kw):
        for n in names:
            pipe.models[n].cuda()
        try:
            return fn(*a, **kw)
        finally:
            for n in names:
                pipe.models[n].cpu()
            torch.cuda.empty_cache()

    t = time.time()
    torch.cuda.reset_peak_memory_stats()
    stage_peaks = {}
    with torch.no_grad():
        img = pipe.preprocess_image(image)
        cond = on_gpu(["image_cond_model"], pipe.get_cond, [img])
        stage_peaks["dinov2"] = peak_gb()
        ss = {"steps": args.steps} if args.steps else {}
        # The mesh decoder upsamples the sparse structure, so its memory follows the surface
        # complexity; dense shapes (crystal clusters, stacked crates) can exceed 8 GB even at modest
        # voxel counts. The structure is sampled, so on an OOM we resample with the next seed and a
        # 15% lower voxel cap, keeping the leanest candidate per round.
        max_voxels = args.max_voxels
        seed = args.seed
        out = {}
        for attempt in range(args.voxel_retries + 1):
            coords = None
            for _ in range(args.voxel_retries + 1):
                torch.manual_seed(seed)
                seed += 1
                candidate = on_gpu(["sparse_structure_flow_model", "sparse_structure_decoder"],
                                   pipe.sample_sparse_structure, cond, 1, ss)
                if coords is None or candidate.shape[0] < coords.shape[0]:
                    coords = candidate
                if coords.shape[0] <= max_voxels:
                    break
                print(f"[image_to_3d] {coords.shape[0]} voxels > {max_voxels}, resampling", flush=True)
            stage_peaks["sparse_structure"] = peak_gb()
            slat = on_gpu(["slat_flow_model"], pipe.sample_slat, cond, coords, ss)
            stage_peaks["slat_flow"] = peak_gb()
            torch.cuda.empty_cache()
            try:
                out["mesh"] = on_gpu(["slat_decoder_mesh"], pipe.models["slat_decoder_mesh"], slat)
                break
            except torch.OutOfMemoryError:
                if attempt == args.voxel_retries:
                    raise
                max_voxels = int(coords.shape[0] * 0.85)
                print(f"[image_to_3d] mesh decode OOM at {coords.shape[0]} voxels, retrying with <= {max_voxels}", flush=True)
                del slat
                gc.collect()
                torch.cuda.empty_cache()
        stats["structure_attempts"] = attempt + 1
        stats["seed_used"] = seed - 1
        del cond, coords
        stage_peaks["decode_mesh"] = peak_gb()
        out["gaussian"] = on_gpu(["slat_decoder_gs"], pipe.models["slat_decoder_gs"], slat)
        stage_peaks["decode_gs"] = peak_gb()
    stats["shape_s"] = round(time.time() - t, 1)
    # The texture bake only needs the decoded outputs; drop the ~4 GB of CPU-side weights first so the bake
    # (plus nvdiffrast's one-time JIT compile) stays under the container RAM cap.
    pipe.models.clear()
    del pipe
    gc.collect()
    stats["stage_peak_vram_gb"] = stage_peaks
    stats["voxels"] = int(slat.coords.shape[0])

    raw = out["mesh"][0]
    mesh = trimesh.Trimesh(raw.vertices.cpu().numpy(), raw.faces.cpu().numpy(), process=False)
    mesh.vertices = mesh.vertices @ np.array([[1, 0, 0], [0, 0, 1], [0, -1, 0]]).T  # Z-up -> glTF Y-up
    stats["shape_tris"] = int(len(mesh.faces))

    def texture():
        t = time.time()
        torch.cuda.reset_peak_memory_stats()
        try:
            return postprocessing_utils.to_glb(out["gaussian"][0], raw, simplify=args.trellis_simplify,
                                               texture_size=args.paint_res, verbose=False)
        finally:
            stats["texture_s"] = round(time.time() - t, 1)
            stats["texture_peak_vram_gb"] = peak_gb()

    return mesh, (texture if args.texture else None)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--model", required=True, choices=["hunyuan-mini", "hunyuan-mini-turbo", "trellis"])
    p.add_argument("--image", required=True, help="RGBA cut-out (or RGB; rembg is applied)")
    p.add_argument("--out", required=True)
    p.add_argument("--name", default="")
    p.add_argument("--seed", type=int, default=1234)
    p.add_argument("--steps", type=int, default=0, help="sampler steps (0 = model default)")
    p.add_argument("--octree", type=int, default=256, help="Hunyuan marching-cubes grid (256 / 380 / 512)")
    p.add_argument("--chunks", type=int, default=8000, help="Hunyuan VAE decode chunk size (lower = less VRAM)")
    p.add_argument("--texture", action="store_true",
                   help="Hunyuan: run Hunyuan3D-Paint after the shape; TRELLIS: bake the gaussians into a texture")
    p.add_argument("--texture-timeout", type=int, default=600, help="seconds before the texture stage is abandoned")
    p.add_argument("--paint-subfolder", default="hunyuan3d-paint-v2-0",
                   help="hunyuan3d-paint-v2-0 or hunyuan3d-paint-v2-0-turbo")
    p.add_argument("--shape-glb", default="", help="Hunyuan: skip shape generation, texture this mesh")
    p.add_argument("--paint-offload", action="store_true", help="Hunyuan3D-Paint: enable_model_cpu_offload()")
    p.add_argument("--paint-res", type=int, default=1024, help="texture/render resolution")
    p.add_argument("--paint-faces", type=int, default=40000, help="faces kept before painting")
    p.add_argument("--trellis-simplify", type=float, default=0.95)
    p.add_argument("--max-voxels", type=int, default=24000,
                   help="TRELLIS: resample the sparse structure above this many voxels (mesh decode VRAM)")
    p.add_argument("--voxel-retries", type=int, default=3)
    p.add_argument("--lowpoly", type=int, default=3000, help="face budget for the flat-palette low-poly variant")
    p.add_argument("--palette", type=int, default=6)
    args = p.parse_args()

    os.makedirs(args.out, exist_ok=True)
    name = args.name or os.path.splitext(os.path.basename(args.image))[0].replace("_rgba", "")
    image = Image.open(args.image)
    if image.mode != "RGBA":
        from rembg import remove
        image = remove(image.convert("RGB"))
    stats = {"model": args.model, "args": vars(args), "gpu": torch.cuda.get_device_name(0)}

    base = os.path.join(args.out, name)

    def save_stats():
        with open(f"{base}_stats.json", "w") as f:
            json.dump(stats, f, indent=2)

    started = time.time()
    run = run_trellis if args.model == "trellis" else run_hunyuan
    shape, texture = run(args, image, stats)
    stats["shape_total_s"] = round(time.time() - started, 1)

    # Save the shape results first, so a texture stage that OOMs / times out never loses them.
    if not args.shape_glb:
        shape.export(f"{base}_shape.glb")
        render_preview(shape, f"{base}_shape_preview.png", f"{args.model} shape, {len(shape.faces)} tris")
        low = lowpoly_palette(shape, image, args.lowpoly, args.palette)
        low.export(f"{base}_lowpoly.glb")
        stats["lowpoly_tris"] = int(len(low.faces))
        render_preview(low, f"{base}_lowpoly_preview.png", f"{args.model} low-poly + {args.palette}-colour "
                       f"palette, {len(low.faces)} tris")
        save_stats()
    else:
        base += "_paint"

    if texture is not None:
        def on_alarm(signum, frame):
            raise TimeoutError(f"texture stage exceeded {args.texture_timeout}s")
        signal.signal(signal.SIGALRM, on_alarm)
        signal.alarm(args.texture_timeout)
        try:
            textured = texture()
            signal.alarm(0)
            textured.export(f"{base}_textured.glb")
            tm = as_trimesh(textured)
            stats["textured_tris"] = int(len(tm.faces))
            render_preview(tm, f"{base}_textured_preview.png", f"{args.model} textured, {len(tm.faces)} tris")
        except (TimeoutError, torch.OutOfMemoryError, RuntimeError) as e:
            signal.alarm(0)
            stats["texture_error"] = f"{type(e).__name__}: {str(e).splitlines()[0][:300]}"
            stats["texture_peak_vram_gb"] = peak_gb()
    stats["total_s"] = round(time.time() - started, 1)
    stats["peak_vram_gb_overall"] = peak_gb()
    save_stats()
    print(f"[image_to_3d] {json.dumps({k: v for k, v in stats.items() if k != 'args'})}", flush=True)

if __name__ == "__main__":
    main()
