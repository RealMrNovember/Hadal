"""HADAL's flat action glyphs: white silhouettes with a soft shadow, for the round blue world buttons and the info
card's header tabs (Last Asylum's look: one plain white symbol on a coloured disc, never a rendered object).

    python art/ui/make_glyphs.py

Writes 128 px PNGs to Assets/_Hadal/Resources/Icons/g_<name>.png (loaded by HudIcons like the other icons).
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Hadal/Resources/Icons"
N = 128
S = 4  # supersampling
W = (255, 255, 255, 255)


def canvas():
    img = Image.new("RGBA", (N * S, N * S), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def p(*xy):
    """Scales 0..128 coordinates to the supersampled canvas."""
    return [v * S for v in xy]


def finish(name, img):
    alpha = img.getchannel("A")
    shadow = Image.new("RGBA", img.size, (8, 20, 40, 0))
    shadow.putalpha(alpha.point(lambda a: int(a * 0.55)).filter(ImageFilter.GaussianBlur(3 * S)))
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    out.alpha_composite(shadow, (0, 3 * S))
    out.alpha_composite(img)
    out.resize((N, N), Image.LANCZOS).save(OUT / f"g_{name}.png")


def home():
    img, d = canvas()
    d.polygon(p(64, 18, 112, 60, 100, 60, 100, 108, 28, 108, 28, 60, 16, 60), fill=W)
    d.rectangle(p(54, 76, 74, 108), fill=(0, 0, 0, 0))
    return img


def shield():
    img, d = canvas()
    d.polygon(p(64, 14, 108, 28, 104, 70, 64, 114, 24, 70, 20, 28), fill=W)
    d.polygon(p(64, 30, 92, 39, 89, 66, 64, 95), fill=(0, 0, 0, 0))
    return img


def person(d, cx, cy, r):
    d.ellipse(p(cx - r * 0.5, cy - r * 1.25, cx + r * 0.5, cy - r * 0.25), fill=W)
    d.pieslice(p(cx - r, cy - r * 0.05, cx + r, cy + r * 1.9), 180, 360, fill=W)


def profile():
    img, d = canvas()
    person(d, 64, 62, 44)
    return img


def reinforce():
    img, d = canvas()
    person(d, 54, 62, 40)
    d.polygon(p(92, 62, 116, 70, 114, 94, 92, 114, 70, 94, 68, 70), fill=W)
    d.polygon(p(92, 72, 106, 77, 104, 92, 92, 104), fill=(0, 0, 0, 0))
    return img


def invite():
    img, d = canvas()
    person(d, 52, 64, 40)
    d.rectangle(p(90, 46, 100, 86), fill=W)
    d.rectangle(p(75, 61, 115, 71), fill=W)
    return img


def attack():
    img, d = canvas()
    for flip in (1, -1):
        cx = 64
        def r(x, y):
            return (cx + (x - cx) * flip, y)
        blade = [r(26, 18), r(36, 14), r(98, 84), r(88, 94)]
        d.polygon([v * S for xy in blade for v in xy], fill=W)
        guard = [r(76, 98), r(102, 72), r(108, 78), r(82, 104)]
        d.polygon([v * S for xy in guard for v in xy], fill=W)
        hilt = [r(94, 98), r(102, 90), r(116, 104), r(108, 112)]
        d.polygon([v * S for xy in hilt for v in xy], fill=W)
    return img


def rally():
    img, d = canvas()
    d.rectangle(p(28, 14, 38, 116), fill=W)
    d.polygon(p(38, 18, 110, 30, 92, 50, 110, 70, 38, 66), fill=W)
    return img


def scout():
    img, d = canvas()
    d.ellipse(p(14, 34, 114, 94), fill=W)
    d.ellipse(p(22, 42, 106, 86), fill=(0, 0, 0, 0))
    d.ellipse(p(44, 44, 84, 84), fill=W)
    d.ellipse(p(56, 56, 72, 72), fill=(0, 0, 0, 0))
    return img


def teleport():
    img, d = canvas()
    d.arc(p(16, 16, 112, 112), 200, 520 - 40, fill=W, width=12 * S)
    d.polygon(p(96, 12, 116, 40, 86, 42), fill=W)
    d.ellipse(p(50, 50, 78, 78), fill=W)
    return img


def share():
    img, d = canvas()
    nodes = [(36, 64), (92, 30), (92, 98)]
    for a, b in ((0, 1), (0, 2)):
        d.line(p(*nodes[a], *nodes[b]), fill=W, width=10 * S)
    for x, y in nodes:
        d.ellipse(p(x - 15, y - 15, x + 15, y + 15), fill=W)
    return img


def star():
    img, d = canvas()
    pts = []
    for i in range(10):
        a = math.pi / 2 + i * math.pi / 5
        r = 54 if i % 2 == 0 else 23
        pts += [64 + r * math.cos(a), 68 - r * math.sin(a)]
    d.polygon(p(*pts), fill=W)
    return img


def info():
    img, d = canvas()
    d.rounded_rectangle(p(53, 14, 75, 84), radius=10 * S, fill=W)
    d.ellipse(p(52, 92, 76, 116), fill=W)
    return img


def chat():
    img, d = canvas()
    d.rounded_rectangle(p(14, 22, 114, 92), radius=26 * S, fill=W)
    d.polygon(p(34, 84, 30, 112, 60, 90), fill=W)
    for x in (40, 64, 88):
        d.ellipse(p(x - 7, 50, x + 7, 64), fill=(0, 0, 0, 0))
    return img


def up():
    img, d = canvas()
    d.polygon(p(64, 12, 112, 62, 84, 62, 84, 114, 44, 114, 44, 62, 16, 62), fill=W)
    return img


def details():
    img, d = canvas()
    for y in (26, 56, 86):
        d.rounded_rectangle(p(16, y, 34, y + 18), radius=5 * S, fill=W)
        d.rounded_rectangle(p(44, y, 112, y + 18), radius=8 * S, fill=W)
    return img


def speed():
    img, d = canvas()
    d.polygon(p(74, 8, 24, 72, 58, 72, 46, 120, 104, 52, 68, 52), fill=W)
    return img


GLYPHS = {"up": up, "details": details, "speed": speed, "home": home, "shield": shield, "profile": profile, "reinforce": reinforce, "invite": invite,
          "attack": attack, "rally": rally, "scout": scout, "teleport": teleport, "share": share,
          "star": star, "info": info, "chat": chat}

if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    for name, draw in GLYPHS.items():
        finish(name, draw())
    print(f"{len(GLYPHS)} glyphs -> {OUT}")
