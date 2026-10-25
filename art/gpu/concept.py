"""Front-view concept image for image-to-3D tests, with SDXL base 1.0 on an 8 GB GPU (same setup as
portraits.py), followed by background removal (rembg, BiRefNet) so the 3D models get a clean RGBA cut-out.

    docker run --rm --gpus all -v ~/hadal-gpu:/work hadal-gpu \
        sh -c "pip install -q rembg==2.0.66 onnxruntime==1.21.1 && \
               python /work/repo/art/gpu/concept.py --out /work/out/3d/input"

Writes <name>.png (raw SDXL, 1024²) and <name>_rgba.png (background removed). Deterministic via --seed.
With --batch art/gpu/assets.json every asset is generated in one process (SDXL loads once); the
roster's style and negative prompt are appended to each asset prompt.
"""
import json
import argparse
import os
import time

import torch
from diffusers import EulerAncestralDiscreteScheduler, StableDiffusionXLPipeline

MODEL = "stabilityai/stable-diffusion-xl-base-1.0"
PROMPT = ("stylized deep-sea diving robot, single object, centered, full body, front view, "
          "plain light grey background, game asset concept, low-poly stylized, flat colors, clean silhouette")
NEGATIVE = "multiple objects, cropped, text, watermark, busy background, scenery, shadow on floor, blurry"


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    p.add_argument("--name", default="diver_robot")
    p.add_argument("--prompt", default=PROMPT)
    p.add_argument("--seed", type=int, default=1234)
    p.add_argument("--steps", type=int, default=30)
    p.add_argument("--rembg", default="birefnet-general",
                   help="rembg model; u2net keeps the grey gaps between limbs on a grey background")
    p.add_argument("--cutout-only", action="store_true", help="reuse <out>/<name>.png, only redo background removal")
    p.add_argument("--batch", default="", help="assets.json roster: generate every asset (or --only ids)")
    p.add_argument("--only", default="")
    p.add_argument("--no-cutout", action="store_true",
                   help="batch: SDXL only; run again with --cutout-only in a fresh process (both together exceed a 10 GB cap)")
    args = p.parse_args()
    if args.batch:
        return batch(args)
    os.makedirs(args.out, exist_ok=True)
    raw = os.path.join(args.out, f"{args.name}.png")
    started = time.time()
    if not args.cutout_only:
        generate(args, raw)
    from rembg import new_session, remove
    from PIL import Image
    cut = remove(Image.open(raw).convert("RGB"), session=new_session(args.rembg))
    cut.save(os.path.join(args.out, f"{args.name}_rgba.png"))
    print(f"[concept] {raw} (+ _rgba): {time.time() - started:.1f}s", flush=True)


def keep_largest_object(rgba, pad=0.08):
    """SDXL often draws a concept sheet (several views); image-to-3D needs one object. Keeps the
    largest connected opaque region, crops to it and re-centres it on a square transparent canvas."""
    import numpy as np
    from PIL import Image

    alpha = np.asarray(rgba.getchannel("A")) > 128
    small = alpha[::4, ::4]  # 256x256 is plenty to tell objects apart
    labels = np.zeros(small.shape, dtype=np.int32)
    sizes = {}
    current = 0
    for y, x in zip(*np.nonzero(small)):
        if labels[y, x]:
            continue
        current += 1
        stack = [(y, x)]
        labels[y, x] = current
        count = 0
        while stack:
            cy, cx = stack.pop()
            count += 1
            for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                if 0 <= ny < small.shape[0] and 0 <= nx < small.shape[1] and small[ny, nx] and not labels[ny, nx]:
                    labels[ny, nx] = current
                    stack.append((ny, nx))
        sizes[current] = count
    if len(sizes) <= 1:
        return rgba

    biggest = max(sizes, key=sizes.get)
    keep = np.kron(labels == biggest, np.ones((4, 4), dtype=bool))[:alpha.shape[0], :alpha.shape[1]]
    ys, xs = np.nonzero(keep & alpha)
    data = np.asarray(rgba).copy()
    data[..., 3] = np.where(keep, data[..., 3], 0)
    crop = Image.fromarray(data).crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    side = int(max(crop.size) * (1 + 2 * pad))
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(crop, ((side - crop.size[0]) // 2, (side - crop.size[1]) // 2))
    return canvas.resize(rgba.size, Image.LANCZOS)


def load_pipe():
    pipe = StableDiffusionXLPipeline.from_pretrained(MODEL, torch_dtype=torch.float16, variant="fp16", use_safetensors=True)
    pipe.scheduler = EulerAncestralDiscreteScheduler.from_config(pipe.scheduler.config)
    pipe.enable_model_cpu_offload()
    pipe.enable_vae_tiling()
    return pipe


def render(pipe, prompt, negative, seed, steps, path):
    image = pipe(prompt=prompt, negative_prompt=negative, num_inference_steps=steps, guidance_scale=6.5,
                 width=1024, height=1024, generator=torch.Generator("cpu").manual_seed(seed)).images[0]
    image.save(path)


def generate(args, raw):
    pipe = load_pipe()
    render(pipe, args.prompt, NEGATIVE, args.seed, args.steps, raw)
    del pipe
    torch.cuda.empty_cache()


def batch(args):
    roster = json.load(open(args.batch))
    only = {s for s in args.only.split(",") if s}
    assets = [a for a in roster["assets"] if not only or a["id"] in only]
    if not args.cutout_only:
        pipe = load_pipe()
        for asset in assets:
            started = time.time()
            raw = os.path.join(args.out, f"{asset['id']}.png")
            render(pipe, f"{asset['prompt']}, {roster['style']}", roster["negative"], asset["seed"], args.steps, raw)
            print(f"[concept] {asset['id']}: {time.time() - started:.1f}s", flush=True)
        del pipe
        torch.cuda.empty_cache()

    if args.no_cutout:
        return

    from rembg import new_session, remove
    from PIL import Image

    session = new_session(args.rembg)
    for asset in assets:
        raw = os.path.join(args.out, f"{asset['id']}.png")
        cut = keep_largest_object(remove(Image.open(raw).convert("RGB"), session=session))
        cut.save(os.path.join(args.out, f"{asset['id']}_rgba.png"))
    print(f"[concept] cut-outs done: {len(assets)}", flush=True)


if __name__ == "__main__":
    main()
