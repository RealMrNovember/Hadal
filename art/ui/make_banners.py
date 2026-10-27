"""Event banners: six deep-sea scenes the Overseer picks from (docs/29).

    python art/ui/make_banners.py

Writes Assets/_Hadal/Resources/Events/banner_{id}.png (1024x512). The event page shows the middle band and darkens
the lower part for the title, so the motif sits up and to the right.
"""
import math
import random
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Hadal/Resources/Events"
W, H = 1024, 512
S = 2  # supersampling


def gradient(top, bottom):
    img = Image.new("RGB", (W * S, H * S))
    d = ImageDraw.Draw(img)
    for y in range(H * S):
        t = y / (H * S - 1)
        d.line([(0, y), (W * S, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
    return img.convert("RGBA")


def glow(img, xy, radius, color, strength=1.0):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x, y = xy[0] * S, xy[1] * S
    r = radius * S
    d.ellipse([x - r, y - r, x + r, y + r], fill=color + (int(255 * strength),))
    layer = layer.filter(ImageFilter.GaussianBlur(r * 0.6))
    img.alpha_composite(layer)


def rays(img, origin_x, color, count=7, seed=1):
    rng = random.Random(seed)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        x = (origin_x + rng.uniform(-260, 260)) * S
        spread = rng.uniform(40, 110) * S
        tilt = rng.uniform(-160, 160) * S
        d.polygon([(x - 18 * S, 0), (x + 18 * S, 0), (x + tilt + spread, H * S), (x + tilt - spread, H * S)], fill=color + (rng.randint(18, 40),))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(14 * S)))


def particles(img, color, count=140, seed=2, size=(1, 4)):
    rng = random.Random(seed)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        x, y = rng.uniform(0, W) * S, rng.uniform(0, H) * S
        r = rng.uniform(*size) * S
        d.ellipse([x - r, y - r, x + r, y + r], fill=color + (rng.randint(60, 200),))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(0.8 * S)))


def bubbles(img, seed=3, count=26, x_range=(560, 1000)):
    rng = random.Random(seed)
    d = ImageDraw.Draw(img)
    for _ in range(count):
        x, y = rng.uniform(*x_range) * S, rng.uniform(20, H - 40) * S
        r = rng.uniform(3, 12) * S
        d.ellipse([x - r, y - r, x + r, y + r], outline=(200, 240, 255, 150), width=max(1, int(1.5 * S)))
        d.ellipse([x - r * 0.45, y - r * 0.6, x - r * 0.05, y - r * 0.2], fill=(255, 255, 255, 140))


def seabed(img, color, seed=4, height=110):
    rng = random.Random(seed)
    pts = [(0, H * S)]
    x = 0
    while x <= W:
        pts.append((x * S, (H - height + rng.uniform(-28, 28) + 30 * math.sin(x / 140)) * S))
        x += 32
    pts.append((W * S, H * S))
    ImageDraw.Draw(img).polygon(pts, fill=color + (255,))


def vignette(img, strength=0.55):
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).ellipse([-W * S * 0.25, -H * S * 0.4, W * S * 1.25, H * S * 1.4], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(120 * S))
    dark = Image.new("RGBA", img.size, (0, 0, 0, int(255 * strength)))
    dark.putalpha(ImageChops.invert(mask).point(lambda v: int(v * strength)))
    img.alpha_composite(dark)


def fish(img, x, y, scale, color, flip=False):
    d = ImageDraw.Draw(img)
    s = scale * S
    x, y = x * S, y * S
    k = -1 if flip else 1
    d.ellipse([x - 40 * s, y - 14 * s, x + 40 * s, y + 14 * s], fill=color)
    d.polygon([(x - k * 36 * s, y), (x - k * 64 * s, y - 18 * s), (x - k * 64 * s, y + 18 * s)], fill=color)


def save(img, name):
    OUT.mkdir(parents=True, exist_ok=True)
    img.resize((W, H), Image.LANCZOS).convert("RGB").save(OUT / f"banner_{name}.png", optimize=True)
    print("banner", name)


def abyss():
    img = gradient((18, 58, 104), (2, 8, 22))
    rays(img, 700, (120, 190, 255), seed=11)
    particles(img, (160, 210, 255), seed=12)
    for i, (x, y, sc) in enumerate([(820, 150, 1.4), (690, 220, 0.9), (930, 260, 0.7)]):
        fish(img, x, y, sc, (6, 18, 34, 235), flip=i % 2 == 1)
    # An anglerfish lure in the dark.
    glow(img, (900, 330), 70, (120, 230, 255), 0.55)
    glow(img, (900, 330), 12, (220, 255, 255), 1.0)
    seabed(img, (3, 10, 22))
    vignette(img)
    return img


def vent():
    img = gradient((70, 22, 10), (10, 4, 4))
    for x, h, w in [(720, 300, 70), (840, 380, 90), (960, 260, 60)]:
        glow(img, (x, H - h), 90, (255, 120, 30), 0.6)
        # Smoke plume rising.
        for k in range(6):
            glow(img, (x + 14 * math.sin(k), H - h - 50 * k), 40 + 12 * k, (90, 60, 50), 0.35)
        ImageDraw.Draw(img).polygon([((x - w) * S, H * S), ((x - w * 0.35) * S, (H - h) * S), ((x + w * 0.35) * S, (H - h) * S), ((x + w) * S, H * S)],
                                    fill=(24, 10, 8, 255))
        glow(img, (x, H - h + 6), 22, (255, 200, 90), 0.9)
    particles(img, (255, 170, 90), seed=21, count=90)
    seabed(img, (18, 7, 5), seed=22, height=80)
    vignette(img)
    return img


def bloom():
    img = gradient((8, 48, 64), (4, 6, 24))
    rng = random.Random(31)
    for _ in range(9):
        x, y, r = rng.uniform(560, 1000), rng.uniform(60, 330), rng.uniform(26, 60)
        color = rng.choice([(70, 255, 220), (190, 120, 255), (90, 200, 255)])
        glow(img, (x, y), r * 1.6, color, 0.45)
        d = ImageDraw.Draw(img)
        d.pieslice([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], 180, 360, fill=color + (170,))
        for t in range(5):
            tx = x - r * 0.7 + t * r * 0.35
            d.line([(tx * S, y * S), ((tx + rng.uniform(-10, 10)) * S, (y + r * 1.8) * S)], fill=color + (130,), width=int(2 * S))
    particles(img, (120, 255, 230), seed=32, count=220, size=(1, 3))
    seabed(img, (3, 12, 18), seed=33)
    vignette(img)
    return img


def leviathan():
    img = gradient((80, 10, 20), (8, 2, 6))
    rays(img, 620, (255, 120, 120), seed=41, count=5)
    d = ImageDraw.Draw(img)
    # A vast body curving through the frame, an eye watching.
    body = [(1100, 40), (900, 70), (720, 160), (620, 300), (640, 470), (820, 560), (1100, 560)]
    d.polygon([(x * S, y * S) for x, y in body], fill=(14, 3, 6, 255))
    for i in range(7):
        y = 140 + i * 50
        d.arc([(640 + i * 6) * S, (y - 30) * S, (1040) * S, (y + 30) * S], 190, 250, fill=(40, 8, 14, 255), width=int(5 * S))
    glow(img, (800, 230), 60, (255, 170, 40), 0.6)
    d.ellipse([768 * S, 214 * S, 832 * S, 246 * S], fill=(255, 196, 60, 255))
    d.ellipse([794 * S, 214 * S, 806 * S, 246 * S], fill=(20, 4, 4, 255))
    particles(img, (255, 140, 140), seed=42, count=80)
    vignette(img, 0.6)
    return img


def trench():
    img = gradient((20, 92, 96), (2, 14, 18))
    glow(img, (760, -40), 240, (170, 255, 230), 0.5)
    d = ImageDraw.Draw(img)
    rng = random.Random(51)
    left = [(0, 0)] + [(380 + rng.uniform(-30, 30) - y * 0.35, y) for y in range(0, H + 1, 40)] + [(0, H)]
    right = [(W, 0)] + [(1000 + rng.uniform(-30, 30) - (H - y) * 0.25, y) for y in range(0, H + 1, 40)] + [(W, H)]
    d.polygon([(x * S, y * S) for x, y in left], fill=(4, 26, 28, 255))
    d.polygon([(x * S, y * S) for x, y in right], fill=(4, 22, 26, 255))
    particles(img, (170, 255, 230), seed=52, count=120)
    bubbles(img, seed=53, x_range=(640, 900))
    vignette(img)
    return img


def wreck():
    img = gradient((92, 70, 26), (12, 10, 6))
    rays(img, 760, (255, 220, 140), seed=61)
    d = ImageDraw.Draw(img)
    # A tilted hull resting on the bed, a mast leaning out of it.
    hull = [(560, 420), (980, 330), (1010, 400), (620, 500)]
    d.polygon([(x * S, y * S) for x, y in hull], fill=(26, 18, 8, 255))
    d.line([(800 * S, 380 * S), (860 * S, 120 * S)], fill=(26, 18, 8, 255), width=int(12 * S))
    d.line([(820 * S, 230 * S), (930 * S, 250 * S)], fill=(26, 18, 8, 255), width=int(7 * S))
    for i in range(4):
        x = 660 + i * 80
        d.ellipse([x * S, (430 - i * 20) * S, (x + 18) * S, (448 - i * 20) * S], fill=(255, 210, 120, 200))
    particles(img, (255, 225, 160), seed=62, count=110)
    bubbles(img, seed=63, x_range=(700, 980))
    seabed(img, (20, 14, 6), seed=64, height=70)
    vignette(img)
    return img


if __name__ == "__main__":
    for name, scene in [("abyss", abyss), ("vent", vent), ("bloom", bloom), ("leviathan", leviathan), ("trench", trench), ("wreck", wreck)]:
        save(scene(), name)
