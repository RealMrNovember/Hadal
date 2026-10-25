"""Front-view concept image for image-to-3D tests, with SDXL base 1.0 on an 8 GB GPU (same setup as
portraits.py), followed by background removal (rembg, BiRefNet) so the 3D models get a clean RGBA cut-out.

    docker run --rm --gpus all -v ~/hadal-gpu:/work hadal-gpu \
        sh -c "pip install -q rembg==2.0.66 onnxruntime==1.21.1 && \
               python /work/repo/art/gpu/concept.py --out /work/out/3d/input"

Writes <name>.png (raw SDXL, 1024²) and <name>_rgba.png (background removed). Deterministic via --seed.
"""
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
    args = p.parse_args()
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


def generate(args, raw):
    pipe = StableDiffusionXLPipeline.from_pretrained(MODEL, torch_dtype=torch.float16, variant="fp16", use_safetensors=True)
    pipe.scheduler = EulerAncestralDiscreteScheduler.from_config(pipe.scheduler.config)
    pipe.enable_model_cpu_offload()
    pipe.enable_vae_tiling()
    image = pipe(prompt=args.prompt, negative_prompt=NEGATIVE, num_inference_steps=args.steps, guidance_scale=6.5,
                 width=1024, height=1024, generator=torch.Generator("cpu").manual_seed(args.seed)).images[0]
    image.save(raw)
    del pipe
    torch.cuda.empty_cache()


if __name__ == "__main__":
    main()
