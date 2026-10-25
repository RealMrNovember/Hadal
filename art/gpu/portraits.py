"""Hero portraits on an 8 GB GPU (doku2: RTX 5060) with SDXL base 1.0 (CreativeML Open RAIL++-M,
commercial use allowed).

    docker run --rm --gpus all -v ~/hadal-gpu:/work hadal-gpu \
        python /work/repo/art/gpu/portraits.py --roster /work/repo/art/gpu/heroes.json --out /work/out/portraits

Deterministic: each hero has a fixed seed, so the same roster always gives the same images.
Model weights are cached in /work/models (HF_HOME).
"""
import argparse
import json
import os
import time

import torch
from diffusers import EulerAncestralDiscreteScheduler, StableDiffusionXLPipeline

MODEL = "stabilityai/stable-diffusion-xl-base-1.0"


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--roster", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--only", default="")
    p.add_argument("--steps", type=int, default=30)
    p.add_argument("--size", type=int, default=1024)
    args = p.parse_args()

    roster = json.load(open(args.roster))
    only = {s for s in args.only.split(",") if s}
    os.makedirs(args.out, exist_ok=True)

    pipe = StableDiffusionXLPipeline.from_pretrained(MODEL, torch_dtype=torch.float16, variant="fp16", use_safetensors=True)
    pipe.scheduler = EulerAncestralDiscreteScheduler.from_config(pipe.scheduler.config)
    # 8 GB card: keep only the active sub-model on the GPU and decode the VAE in tiles.
    pipe.enable_model_cpu_offload()
    pipe.enable_vae_tiling()

    for hero in roster["heroes"]:
        if only and hero["id"] not in only:
            continue
        started = time.time()
        prompt = f"{hero['prompt']}, {roster['style']}"
        image = pipe(
            prompt=prompt,
            negative_prompt=roster["negative"],
            num_inference_steps=args.steps,
            guidance_scale=6.5,
            width=args.size,
            height=args.size,
            generator=torch.Generator("cpu").manual_seed(hero["seed"]),
        ).images[0]
        image.save(os.path.join(args.out, f"{hero['id']}.png"))
        print(f"[portraits] {hero['id']} ({hero['rarity']} {hero['class']}): {time.time() - started:.1f}s", flush=True)


if __name__ == "__main__":
    main()
