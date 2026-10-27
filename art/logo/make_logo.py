"""The HADAL wordmark, drawn from geometry (no font licence involved).

    python art/logo/make_logo.py            # writes Assets/_Hadal/Resources/Branding/hadal_logo.png (+ a preview)

Angular letters like a pressure hull: a pale steel top fading to abyssal teal, a dark rim, a cyan glow, sonar rings
behind, and the A's crossbars as downward chevrons (depth markers). Deterministic: the same file every run.
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Hadal/Resources/Branding/hadal_logo.png"
PREVIEW = ROOT / "art/logo/preview.png"

W, H = 2400, 1000
S = 2  # supersampling
CAP = 330  # letter height
STROKE = 84
GAP = 46
CUT = 34  # chamfer on outer corners


def chamfer_rect(x0, y0, x1, y1, c=CUT, corners="all"):
    """A rectangle with cut corners (polygon points)."""
    tl = c if corners in ("all", "top", "left") else 0
    tr = c if corners in ("all", "top", "right") else 0
    br = c if corners in ("all", "bottom", "right") else 0
    bl = c if corners in ("all", "bottom", "left") else 0
    return [(x0 + tl, y0), (x1 - tr, y0), (x1, y0 + tr), (x1, y1 - br), (x1 - br, y1), (x0 + bl, y1), (x0, y1 - bl), (x0, y0 + tl)]


def letter_h(x, y, w):
    return [chamfer_rect(x, y, x + STROKE, y + CAP), chamfer_rect(x + w - STROKE, y, x + w, y + CAP),
            [(x + STROKE - 2, y + CAP * 0.44), (x + w - STROKE + 2, y + CAP * 0.44), (x + w - STROKE + 2, y + CAP * 0.44 + STROKE * 0.8),
             (x + STROKE - 2, y + CAP * 0.44 + STROKE * 0.8)]]


def letter_a(x, y, w):
    """An A whose bar is a downward chevron."""
    apex = x + w / 2
    outer = [(apex - STROKE * 0.55, y), (apex + STROKE * 0.55, y), (x + w, y + CAP), (x + w - STROKE * 1.15, y + CAP),
             (apex, y + STROKE * 1.55), (x + STROKE * 1.15, y + CAP), (x, y + CAP)]
    by = y + CAP * 0.6
    chevron = [(apex - w * 0.3, by), (apex, by + STROKE * 0.75), (apex + w * 0.3, by), (apex + w * 0.36, by + STROKE * 0.62),
               (apex, by + STROKE * 1.5), (apex - w * 0.36, by + STROKE * 0.62)]
    return [outer, chevron]


def letter_d(x, y, w):
    c = CAP * 0.34
    outer = [(x, y), (x + w - c, y), (x + w, y + c), (x + w, y + CAP - c), (x + w - c, y + CAP), (x, y + CAP)]
    i = STROKE
    ci = c * 0.55
    inner = [(x + i, y + i), (x + w - i - ci, y + i), (x + w - i, y + i + ci), (x + w - i, y + CAP - i - ci), (x + w - i - ci, y + CAP - i), (x + i, y + CAP - i)]
    return [("cut", outer, inner)]


def letter_l(x, y, w):
    return [chamfer_rect(x, y, x + STROKE, y + CAP, corners="top"), chamfer_rect(x, y + CAP - STROKE, x + w, y + CAP, corners="right")]


LETTERS = [("H", letter_h, 250), ("A", letter_a, 300), ("D", letter_d, 250), ("A", letter_a, 300), ("L", letter_l, 220)]


def word_mask(scale):
    total = sum(w for _, _, w in LETTERS) + GAP * (len(LETTERS) - 1)
    x = (W - total) / 2
    y = H * 0.36
    mask = Image.new("L", (W * scale, H * scale), 0)
    draw = ImageDraw.Draw(mask)
    for _, fn, w in LETTERS:
        for shape in fn(x, y, w):
            if isinstance(shape, tuple) and shape[0] == "cut":
                draw.polygon([(px * scale, py * scale) for px, py in shape[1]], fill=255)
                draw.polygon([(px * scale, py * scale) for px, py in shape[2]], fill=0)
            else:
                draw.polygon([(px * scale, py * scale) for px, py in shape], fill=255)
        x += w + GAP
    return mask, (W - total) / 2, y, total


def vertical_gradient(size, stops):
    w, h = size
    grad = Image.new("RGB", (1, h))
    for j in range(h):
        t = j / max(1, h - 1)
        for k in range(len(stops) - 1):
            t0, c0 = stops[k]
            t1, c1 = stops[k + 1]
            if t0 <= t <= t1:
                u = (t - t0) / max(1e-6, t1 - t0)
                grad.putpixel((0, j), tuple(int(c0[n] + (c1[n] - c0[n]) * u) for n in range(3)))
                break
    return grad.resize((w, h))


def main():
    mask, x0, y0, total = word_mask(S)
    size = mask.size
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))

    # Sonar rings behind the word, fading outwards.
    rings = Image.new("L", size, 0)
    rd = ImageDraw.Draw(rings)
    cx, cy = W * S / 2, (y0 + CAP * 0.5) * S
    for r, a in [(250, 70), (350, 40), (450, 18)]:
        rd.ellipse([cx - r * S * 1.75, cy - r * S * 0.5, cx + r * S * 1.75, cy + r * S * 0.5], outline=a, width=4 * S)
    rings = rings.filter(ImageFilter.GaussianBlur(2 * S))
    ring_color = Image.new("RGBA", size, (60, 210, 240, 0))
    ring_color.putalpha(rings)
    canvas = Image.alpha_composite(canvas, ring_color)

    # Outer glow.
    glow = mask.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(28 * S))
    glow_layer = Image.new("RGBA", size, (40, 200, 255, 0))
    glow_layer.putalpha(glow.point(lambda v: int(v * 0.85)))
    canvas = Image.alpha_composite(canvas, glow_layer)

    # Dark rim: the mask grown, filled near-black blue.
    rim = mask.filter(ImageFilter.MaxFilter(2 * 9 + 1))
    rim_layer = Image.new("RGBA", size, (4, 18, 28, 0))
    rim_layer.putalpha(rim)
    canvas = Image.alpha_composite(canvas, rim_layer)

    # Face: pale steel at the top fading to deep teal, a bright bevel along the top edges.
    top, bottom = int(y0 * S), int((y0 + CAP) * S)
    face = Image.new("RGB", size, (0, 0, 0))
    band = vertical_gradient((size[0], bottom - top), [(0, (238, 248, 252)), (0.42, (150, 215, 232)), (0.5, (52, 150, 178)), (1, (14, 70, 96))])
    face.paste(band, (0, top))
    face_layer = face.convert("RGBA")
    face_layer.putalpha(mask)
    canvas = Image.alpha_composite(canvas, face_layer)

    # Bevel highlight: the part of the mask whose pixel above is empty.
    shifted = ImageChops.offset(mask, 0, 6 * S)
    edge = ImageChops.subtract(mask, shifted).filter(ImageFilter.GaussianBlur(2 * S))
    hi = Image.new("RGBA", size, (255, 255, 255, 0))
    hi.putalpha(edge.point(lambda v: int(v * 0.9)))
    canvas = Image.alpha_composite(canvas, hi)

    # A thin rule under the word with a glowing lure dot in the middle (the anglerfish of the deep).
    rule = Image.new("RGBA", size, (0, 0, 0, 0))
    dr = ImageDraw.Draw(rule)
    ry = (y0 + CAP + 70) * S
    for side in (-1, 1):
        x_in = cx + side * 60 * S
        x_out = cx + side * total * 0.5 * S
        dr.line([(x_in, ry), (x_out, ry)], fill=(120, 220, 240, 200), width=4 * S)
        dr.polygon([(x_out, ry - 8 * S), (x_out + side * 22 * S, ry), (x_out, ry + 8 * S)], fill=(120, 220, 240, 200))
    lure = Image.new("L", size, 0)
    ImageDraw.Draw(lure).ellipse([cx - 22 * S, ry - 22 * S, cx + 22 * S, ry + 22 * S], fill=255)
    lure_glow = Image.new("RGBA", size, (255, 214, 120, 0))
    lure_glow.putalpha(lure.filter(ImageFilter.GaussianBlur(26 * S)))
    rule = Image.alpha_composite(rule, lure_glow)
    core = Image.new("RGBA", size, (255, 240, 200, 0))
    core.putalpha(lure.filter(ImageFilter.GaussianBlur(3 * S)))
    rule = Image.alpha_composite(rule, core)
    canvas = Image.alpha_composite(canvas, rule)

    logo = canvas.resize((W, H), Image.LANCZOS)
    bbox = logo.getbbox()
    logo = logo.crop((max(0, bbox[0] - 20), max(0, bbox[1] - 20), min(W, bbox[2] + 20), min(H, bbox[3] + 20)))
    # Unity rescales non-power-of-two textures by default: centre the logo on a 2048x512 transparent sheet instead.
    sheet = Image.new("RGBA", (2048, 512), (0, 0, 0, 0))
    k = min(2000 / logo.width, 500 / logo.height)
    fitted = logo.resize((int(logo.width * k), int(logo.height * k)), Image.LANCZOS)
    sheet.paste(fitted, ((2048 - fitted.width) // 2, (512 - fitted.height) // 2), fitted)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT)

    preview = Image.new("RGBA", logo.size, (2, 10, 18, 255))
    preview = Image.alpha_composite(preview, logo)
    preview.convert("RGB").save(PREVIEW)
    print(OUT, logo.size)


if __name__ == "__main__":
    main()
