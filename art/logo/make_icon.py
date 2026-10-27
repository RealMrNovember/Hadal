"""The app icon: the logo's steel H over deep water, with the depth chevron and the lure light.

    python art/logo/make_icon.py

Writes Assets/_Hadal/Art/Branding/icon.png (1024, legacy and round), icon_fg.png and icon_bg.png (Android adaptive
layers, 1024 with the art inside the 66% safe zone). Same geometry and colours as make_logo.py.
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Hadal/Art/Branding"
N = 1024
S = 2


def gradient(size, stops, power=1.0):
    w, h = size
    col = Image.new("RGB", (1, h))
    for j in range(h):
        t = (j / max(1, h - 1)) ** power
        for k in range(len(stops) - 1):
            t0, c0 = stops[k]
            t1, c1 = stops[k + 1]
            if t0 <= t <= t1:
                u = (t - t0) / max(1e-6, t1 - t0)
                col.putpixel((0, j), tuple(int(c0[n] + (c1[n] - c0[n]) * u) for n in range(3)))
                break
    return col.resize((w, h))


def background(size):
    bg = gradient((size, size), [(0, (24, 82, 104)), (0.55, (6, 30, 44)), (1, (2, 9, 16))]).convert("RGBA")
    rings = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(rings)
    c = size / 2
    for r, a in [(0.3, 60), (0.42, 38), (0.54, 20)]:
        d.ellipse([c - r * size, c - r * size * 0.42 + size * 0.1, c + r * size, c + r * size * 0.42 + size * 0.1], outline=a, width=max(2, size // 220))
    layer = Image.new("RGBA", (size, size), (70, 210, 240, 0))
    layer.putalpha(rings.filter(ImageFilter.GaussianBlur(size / 400)))
    return Image.alpha_composite(bg, layer)


def h_mark(size, scale):
    """The H with chamfered stems and a chevron bar, centred, `scale` of the canvas wide."""
    m = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(m)
    w = size * scale
    h = w * 1.08
    x0 = (size - w) / 2
    y0 = (size - h) / 2 - size * 0.04
    st = w * 0.3
    cut = st * 0.42

    def stem(x):
        d.polygon([(x + cut, y0), (x + st - cut, y0), (x + st, y0 + cut), (x + st, y0 + h - cut), (x + st - cut, y0 + h),
                   (x + cut, y0 + h), (x, y0 + h - cut), (x, y0 + cut)], fill=255)

    stem(x0)
    stem(x0 + w - st)
    by = y0 + h * 0.42
    bh = st * 0.75
    cx = x0 + w / 2
    d.polygon([(x0 + st - 2, by), (cx, by + bh * 0.6), (x0 + w - st + 2, by), (x0 + w - st + 2, by + bh), (cx, by + bh * 1.6), (x0 + st - 2, by + bh)], fill=255)
    return m, (cx, y0 + h + size * 0.07), (y0, y0 + h)


def foreground(size, scale):
    mask, lure_at, (top, bottom) = h_mark(size, scale)
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))

    glow = Image.new("RGBA", (size, size), (40, 200, 255, 0))
    glow.putalpha(mask.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(size / 40)).point(lambda v: int(v * 0.9)))
    out = Image.alpha_composite(out, glow)

    rim = Image.new("RGBA", (size, size), (4, 18, 28, 0))
    rim.putalpha(mask.filter(ImageFilter.MaxFilter(max(3, (size // 90) | 1))))
    out = Image.alpha_composite(out, rim)

    face = Image.new("RGB", (size, size))
    face.paste(gradient((size, int(bottom - top)), [(0, (240, 249, 252)), (0.42, (150, 215, 232)), (0.5, (52, 150, 178)), (1, (14, 70, 96))]), (0, int(top)))
    face = face.convert("RGBA")
    face.putalpha(mask)
    out = Image.alpha_composite(out, face)

    edge = ImageChops.subtract(mask, ImageChops.offset(mask, 0, max(2, size // 160))).filter(ImageFilter.GaussianBlur(size / 700))
    hi = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    hi.putalpha(edge.point(lambda v: int(v * 0.9)))
    out = Image.alpha_composite(out, hi)

    lx, ly = lure_at
    r = size * 0.028
    dot = Image.new("L", (size, size), 0)
    ImageDraw.Draw(dot).ellipse([lx - r, ly - r, lx + r, ly + r], fill=255)
    halo = Image.new("RGBA", (size, size), (255, 214, 120, 0))
    halo.putalpha(dot.filter(ImageFilter.GaussianBlur(size / 30)))
    out = Image.alpha_composite(out, halo)
    core = Image.new("RGBA", (size, size), (255, 242, 205, 0))
    core.putalpha(dot.filter(ImageFilter.GaussianBlur(size / 300)))
    return Image.alpha_composite(out, core)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    big = N * S
    full = Image.alpha_composite(background(big), foreground(big, 0.52)).resize((N, N), Image.LANCZOS)
    full.save(OUT / "icon.png")
    # Adaptive layers: the launcher masks to a circle or squircle and may crop 1/6 each side.
    foreground(big, 0.4).resize((N, N), Image.LANCZOS).save(OUT / "icon_fg.png")
    background(big).resize((N, N), Image.LANCZOS).save(OUT / "icon_bg.png")
    preview = full.copy()
    m = Image.new("L", (N, N), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, N - 1, N - 1], radius=N // 5, fill=255)
    preview.putalpha(m)
    sheet = Image.new("RGBA", (N, N), (230, 233, 236, 255))
    sheet = Image.alpha_composite(sheet, preview)
    sheet.convert("RGB").resize((512, 512), Image.LANCZOS).save(ROOT / "art/logo/icon_preview.png")
    print("icons written")


if __name__ == "__main__":
    main()
