"""Tall full-body hero card art and painted bag item icons, with SDXL base 1.0 on an 8 GB GPU (same setup as
portraits.py), then background removal (rembg, BiRefNet) so Unity can lay the cut-out over its own rarity backdrop
and move it (breathing, sway) separately from the background.

    # 1. SDXL, every entry of a roster in one process (art/gpu/hero_art.json or art/gpu/items.json)
    docker run --rm --gpus all -v ~/hadal-gpu:/work hadal-gpu \
        python /work/repo/art/gpu/cards.py --roster /work/repo/art/gpu/hero_art.json --out /work/out/cards/heroes
    # 2. cut-outs, one image per container (onnxruntime's arena grows across images, see Dockerfile.cutout)
    for id in ...; do docker run --rm -v ~/hadal-gpu:/work hadal-gpu-cutout \
        python /work/repo/art/gpu/cards.py --roster ... --out ... --cutout --only $id; done

Writes <id>.png (raw) and <id>_rgba.png (cut-out, cropped to the figure and padded back to the roster's aspect,
scaled to the roster's "final" size). Deterministic: each entry has a fixed seed.
"""
import argparse
import json
import os
import time


def generate(roster, entries, out, steps):
    import torch
    from diffusers import EulerAncestralDiscreteScheduler, StableDiffusionXLPipeline

    pipe = StableDiffusionXLPipeline.from_pretrained("stabilityai/stable-diffusion-xl-base-1.0", torch_dtype=torch.float16,
                                                     variant="fp16", use_safetensors=True)
    pipe.scheduler = EulerAncestralDiscreteScheduler.from_config(pipe.scheduler.config)
    pipe.enable_model_cpu_offload()
    pipe.enable_vae_tiling()
    width, height = roster["size"]
    for entry in entries:
        started = time.time()
        # CLIP reads 77 tokens: an entry can bring a shorter style of its own so the background words are not cut off.
        image = pipe(prompt=f"{entry['prompt']}, {entry.get('style', roster['style'])}", negative_prompt=roster["negative"],
                     num_inference_steps=steps, guidance_scale=6.5, width=width, height=height,
                     generator=torch.Generator("cpu").manual_seed(entry["seed"])).images[0]
        image.save(os.path.join(out, f"{entry['id']}.png"))
        print(f"[cards] {entry['id']}: {time.time() - started:.1f}s", flush=True)


def cutout(roster, entries, out, model):
    import numpy as np
    from PIL import Image
    from rembg import new_session, remove

    session = new_session(model)
    fw, fh = roster["final"]
    pad = roster.get("pad", 0.04)
    for entry in entries:
        rgba = remove(Image.open(os.path.join(out, f"{entry['id']}.png")).convert("RGB"), session=session)
        alpha = np.asarray(rgba.getchannel("A")) > 24
        if entry.get("largest"):
            # SDXL sometimes adds small copies around the object: keep only the biggest connected piece.
            from scipy import ndimage
            labels, count = ndimage.label(alpha)
            if count > 1:
                keep = labels == (np.argmax(np.bincount(labels.ravel())[1:]) + 1)
                data = np.asarray(rgba).copy()
                data[..., 3] = np.where(keep, data[..., 3], 0)
                rgba, alpha = Image.fromarray(data), keep
        ys, xs = np.nonzero(alpha)
        crop = rgba.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
        # Fit into the final aspect with a little room; figures stand on the bottom edge, icons sit centred.
        scale = min(fw * (1 - 2 * pad) / crop.width, fh * (1 - 2 * pad) / crop.height)
        crop = crop.resize((max(1, round(crop.width * scale)), max(1, round(crop.height * scale))), Image.LANCZOS)
        canvas = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
        y = fh - crop.height if roster.get("anchor") == "bottom" else (fh - crop.height) // 2
        canvas.paste(crop, ((fw - crop.width) // 2, y), crop)
        canvas.save(os.path.join(out, f"{entry['id']}_rgba.png"))
        print(f"[cards] cut-out {entry['id']}", flush=True)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--roster", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--only", default="")
    p.add_argument("--steps", type=int, default=30)
    p.add_argument("--cutout", action="store_true", help="background removal of the already generated images")
    p.add_argument("--rembg", default="birefnet-general")
    args = p.parse_args()

    roster = json.load(open(args.roster))
    only = {s for s in args.only.split(",") if s}
    entries = [e for e in roster["entries"] if not only or e["id"] in only]
    os.makedirs(args.out, exist_ok=True)
    if args.cutout:
        cutout(roster, entries, args.out, args.rembg)
    else:
        generate(roster, entries, args.out, args.steps)


if __name__ == "__main__":
    main()
