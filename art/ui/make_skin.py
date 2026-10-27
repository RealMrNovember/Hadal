"""HADAL's UI skin: every frame, button, tab, slot and bar the interface is built from, drawn in code.

    python art/ui/make_skin.py [--mockup <out-dir>]

Writes 9-sliceable sprites to Assets/_Hadal/Resources/UiSkin/ (each with its border in skin.json) and, with
--mockup, renders three screens from them for review. The look: light, readable panels under deep-navy headers
trimmed in brass (a diving bell's porthole rim), chunky glossy buttons with a darker lip, rarity-framed slots.
"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Hadal/Resources/UiSkin"
S = 4  # supersampling

# Palette
NAVY_TOP, NAVY_BOT = (52, 76, 112), (30, 44, 70)
BRASS_HI, BRASS, BRASS_LO = (255, 222, 140), (214, 166, 74), (128, 88, 32)
BODY_TOP, BODY_BOT = (238, 243, 248), (218, 227, 237)
INSET = (205, 216, 229)
INK = (24, 36, 56)
BUTTONS = {
    "blue": ((104, 172, 246), (46, 110, 210), (22, 62, 140)),
    "green": ((124, 218, 112), (52, 160, 70), (24, 98, 40)),
    "gold": ((255, 214, 96), (236, 150, 34), (150, 82, 10)),
    "red": ((246, 110, 96), (196, 52, 46), (118, 24, 24)),
    "grey": ((180, 192, 206), (128, 142, 160), (70, 82, 98)),
    "teal": ((92, 214, 222), (28, 146, 168), (10, 82, 98)),
    "navy": ((76, 104, 146), (42, 60, 96), (16, 24, 42)),
    "light": ((252, 253, 255), (220, 229, 240), (128, 146, 170)),
}
RARITY = {"common": ((150, 214, 120), (66, 150, 70)), "rare": ((110, 176, 246), (40, 98, 200)),
          "epic": ((196, 130, 246), (118, 56, 196)), "legendary": ((255, 196, 90), (214, 120, 30))}


def rounded_mask(w, h, r, s=S):
    m = Image.new("L", (w * s, h * s), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, w * s - 1, h * s - 1], radius=r * s, fill=255)
    return m


def vgrad(w, h, top, bottom, s=S):
    g = Image.new("RGB", (1, h * s))
    for y in range(h * s):
        t = y / max(1, h * s - 1)
        g.putpixel((0, y), tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
    return g.resize((w * s, h * s))


def finish(img, w, h):
    return img.resize((w, h), Image.LANCZOS)


def shape(w, h, r, fill_top, fill_bot, border=None, border_w=0, inner_hi=None, lip=None, lip_h=0, gloss=0.0, s=S):
    """A rounded slab: optional dark lip under it, a border, a gradient face, an inner top highlight and a gloss band."""
    canvas = Image.new("RGBA", (w * s, (h + lip_h) * s), (0, 0, 0, 0))
    if lip:
        lm = rounded_mask(w, h, r)
        layer = Image.new("RGBA", lm.size, lip + (255,))
        layer.putalpha(lm)
        canvas.alpha_composite(layer, (0, lip_h * s))
    outer = rounded_mask(w, h, r)
    if border:
        b = Image.new("RGBA", outer.size, border + (255,))
        b.putalpha(outer)
        canvas.alpha_composite(b, (0, 0))
    inset = border_w * s
    iw, ih = w * s - 2 * inset, h * s - 2 * inset
    face_mask = Image.new("L", (iw, ih), 0)
    ImageDraw.Draw(face_mask).rounded_rectangle([0, 0, iw - 1, ih - 1], radius=max(1, r * s - inset), fill=255)
    face = vgrad(iw // s + 1, ih // s + 1, fill_top, fill_bot).resize((iw, ih)).convert("RGBA")
    if gloss > 0:
        band = Image.new("L", (iw, ih), 0)
        ImageDraw.Draw(band).rounded_rectangle([iw * 0.04, ih * 0.06, iw * 0.96, ih * 0.48], radius=max(1, r * s - inset), fill=int(255 * gloss))
        band = band.filter(ImageFilter.GaussianBlur(2 * s))
        face = Image.composite(Image.new("RGBA", (iw, ih), (255, 255, 255, 255)), face, band)
    if inner_hi:
        hi = ImageChops.subtract(face_mask, ImageChops.offset(face_mask, 0, 2 * s))
        hl = Image.new("RGBA", (iw, ih), inner_hi + (255,))
        hl.putalpha(hi.point(lambda v: int(v * 0.9)))
        face.alpha_composite(hl)
    face.putalpha(face_mask)
    canvas.alpha_composite(face, (inset, inset))
    return canvas


def rivets(img, w, h, r, color=BRASS, s=S):
    d = ImageDraw.Draw(img)
    for cx, cy in [(r * 0.9, h * 0.5), (w - r * 0.9, h * 0.5)]:
        rad = 3.2 * s
        d.ellipse([cx * s - rad, cy * s - rad, cx * s + rad, cy * s + rad], fill=BRASS_LO + (255,))
        d.ellipse([cx * s - rad * 0.7, cy * s - rad * 0.8, cx * s + rad * 0.6, cy * s + rad * 0.5], fill=color + (255,))


SPRITES = {}


def save(name, img, w, h, border):
    OUT.mkdir(parents=True, exist_ok=True)
    finish(img, w, h).save(OUT / f"{name}.png")
    SPRITES[name] = {"border": border}


def build():
    # Panel body: light, a thin cool border and a soft shadow.
    w, h = 128, 128
    body = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    sh = rounded_mask(w - 8, h - 8, 26).filter(ImageFilter.GaussianBlur(6 * S))
    shl = Image.new("RGBA", sh.size, (0, 10, 30, 110))
    shl.putalpha(sh.point(lambda v: int(v * 0.45)))
    body.alpha_composite(shl, (4 * S, 7 * S))
    body.alpha_composite(shape(w - 8, h - 10, 24, BODY_TOP, BODY_BOT, border=(150, 168, 190), border_w=2, inner_hi=(255, 255, 255)), (4 * S, 2 * S))
    save("panel", body, w, h, [36, 36, 36, 36])

    # Header: navy, brass rule under it, rivets at both ends (the bell's rim).
    w, h = 160, 72
    hdr = shape(w, h - 6, 24, NAVY_TOP, NAVY_BOT, border=(18, 26, 44), border_w=2, inner_hi=(120, 150, 196))
    full = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
    full.alpha_composite(hdr, (0, 0))
    d = ImageDraw.Draw(full)
    d.rectangle([10 * S, (h - 8) * S, (w - 10) * S, (h - 4) * S], fill=BRASS + (255,))
    d.rectangle([10 * S, (h - 8) * S, (w - 10) * S, (h - 7) * S], fill=BRASS_HI + (255,))
    rivets(full, w, h - 8, 18)
    save("header", full, w, h, [40, 24, 40, 24])

    # Inset section inside panels.
    save("inset", shape(96, 96, 18, INSET, (196, 208, 222), border=(178, 192, 210), border_w=1), 96, 96, [24, 24, 24, 24])

    # Buttons: glossy face, dark border, a lip underneath.
    for name, (top, bot, edge) in BUTTONS.items():
        img = shape(128, 76, 22, top, bot, border=edge, border_w=3, inner_hi=(255, 255, 255), lip=edge, lip_h=7, gloss=0.16)
        save("btn_" + name, img, 128, 83, [30, 26, 30, 32])

    # Round icon button (the side bar, close): a porthole with a brass rim.
    for name, (top, bot) in {"round_blue": BUTTONS["blue"][:2], "round_red": BUTTONS["red"][:2], "round_navy": (NAVY_TOP, NAVY_BOT)}.items():
        n = 128
        img = Image.new("RGBA", (n * S, n * S), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        d.ellipse([2 * S, 4 * S, (n - 2) * S, n * S - 1], fill=(0, 0, 0, 90))
        d.ellipse([2 * S, 0, (n - 2) * S, (n - 4) * S], fill=BRASS_LO + (255,))
        d.ellipse([6 * S, 3 * S, (n - 6) * S, (n - 8) * S], fill=BRASS + (255,))
        face = vgrad(n, n, top, bot).convert("RGBA")
        m = Image.new("L", (n * S, n * S), 0)
        ImageDraw.Draw(m).ellipse([12 * S, 9 * S, (n - 12) * S, (n - 14) * S], fill=255)
        img.paste(face, (0, 0), m)
        gl = Image.new("L", (n * S, n * S), 0)
        ImageDraw.Draw(gl).ellipse([24 * S, 14 * S, (n - 24) * S, (n * 0.48) * S], fill=80)
        img.alpha_composite(Image.new("RGBA", img.size, (255, 255, 255, 255)) if False else Image.composite(Image.new("RGBA", img.size, (255, 255, 255, 255)), Image.new("RGBA", img.size, (0, 0, 0, 0)), gl.filter(ImageFilter.GaussianBlur(3 * S))))
        save(name, img, n, n, [0, 0, 0, 0])

    # Tabs: chosen = light with a brass underline, others = navy.
    save("tab_on", shape(120, 64, 18, (250, 252, 255), (226, 234, 244), border=(150, 168, 190), border_w=2, inner_hi=(255, 255, 255)), 120, 64, [24, 20, 24, 20])
    save("tab_off", shape(120, 64, 18, (70, 96, 136), (44, 64, 98), border=(22, 32, 52), border_w=2, inner_hi=(130, 160, 204)), 120, 64, [24, 20, 24, 20])

    # Item slots by rarity: coloured gradient, white inner rim, rounded.
    for name, (top, bot) in RARITY.items():
        img = shape(128, 128, 20, top, bot, border=tuple(int(c * 0.55) for c in bot), border_w=3, inner_hi=(255, 255, 255), gloss=0.12)
        save("slot_" + name, img, 128, 128, [28, 28, 28, 28])

    # Bars: a dark track and fills.
    save("bar_track", shape(96, 32, 14, (20, 30, 46), (40, 54, 76), border=(10, 16, 26), border_w=2), 96, 32, [16, 14, 16, 14])
    for name, (top, bot) in {"bar_green": ((150, 236, 120), (54, 168, 64)), "bar_gold": ((255, 222, 110), (232, 150, 40)),
                             "bar_blue": ((120, 196, 255), (40, 118, 220))}.items():
        save(name, shape(96, 32, 14, top, bot, gloss=0.25), 96, 32, [16, 14, 16, 14])

    # HUD pill: a dark translucent capsule with a light rim (resource counters).
    pill = shape(160, 56, 28, (14, 24, 40), (8, 16, 28), border=(110, 140, 180), border_w=2)
    pill.putalpha(pill.getchannel("A").point(lambda v: int(v * 0.86)))
    save("pill", pill, 160, 56, [28, 26, 28, 26])

    (OUT / "skin.json").write_text(json.dumps(SPRITES, indent=2), encoding="utf-8")
    print(f"{len(SPRITES)} sprites -> {OUT}")


# --- Mockup -----------------------------------------------------------------------------------

def nine(name, w, h):
    """Draws a skin sprite 9-sliced to w x h."""
    src = Image.open(OUT / f"{name}.png").convert("RGBA")
    l, b, r, t = SPRITES.get(name, {"border": [0, 0, 0, 0]})["border"] if SPRITES else json.loads((OUT / "skin.json").read_text())[name]["border"]
    sw, sh = src.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, sw - r, l, w - r), (sw - r, sw, w - r, w)]
    ys = [(0, t, 0, t), (t, sh - b, t, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            out.alpha_composite(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out


def font(size, heavy=True):
    return ImageFont.truetype("C:/Windows/Fonts/seguibl.ttf" if heavy else "C:/Windows/Fonts/segoeuib.ttf", size)


def text(img, xy, s, size, fill=(255, 255, 255), stroke=True, anchor="mm", heavy=True):
    d = ImageDraw.Draw(img)
    d.text(xy, s, font=font(size, heavy), fill=fill, anchor=anchor, stroke_width=3 if stroke else 0, stroke_fill=(16, 26, 44))


def icon(name, size):
    p = ROOT / f"Assets/_Hadal/Resources/Icons/{name}.png"
    return Image.open(p).convert("RGBA").resize((size, size), Image.LANCZOS)


def portrait(name, w, h):
    p = ROOT / f"Assets/_Hadal/Art/Portraits/{name}.png"
    im = Image.open(p).convert("RGBA")
    k = max(w / im.width, h / im.height)
    im = im.resize((int(im.width * k), int(im.height * k)), Image.LANCZOS)
    return im.crop(((im.width - w) // 2, 0, (im.width - w) // 2 + w, h))


def window(canvas, x, y, w, h, title):
    canvas.alpha_composite(nine("panel", w, h), (x, y))
    canvas.alpha_composite(nine("header", w, 120), (x, y))
    text(canvas, (x + w // 2, y + 54), title, 48)
    canvas.alpha_composite(Image.open(OUT / "round_red.png").convert("RGBA").resize((96, 96)), (x + w - 112, y + 6))
    text(canvas, (x + w - 64, y + 50), "X", 44)


def button(canvas, x, y, w, h, color, label, size=44):
    canvas.alpha_composite(nine("btn_" + color, w, h), (x, y))
    text(canvas, (x + w // 2, y + (h - 8) // 2), label, size)


def mock_upgrade(bg):
    c = bg.copy()
    c.alpha_composite(Image.new("RGBA", c.size, (0, 6, 16, 150)))
    x, y, w, h = 50, 300, 980, 1300
    window(c, x, y, w, h, "Askeri Kubbe")
    text(c, (x + w // 2, y + 165), "Seviye 16  →  17", 40, fill=INK, stroke=False)
    c.alpha_composite(nine("inset", w - 80, 260), (x + 40, y + 210))
    rows = [("Ordu kapasitesi", "7.200  →  7.650"), ("Güç", "4.120  →  4.480"), ("Komuta bonusu", "+48  →  +51")]
    for i, (a, b) in enumerate(rows):
        text(c, (x + 80, y + 260 + i * 70), a, 34, fill=(70, 86, 110), stroke=False, anchor="lm", heavy=False)
        text(c, (x + w - 80, y + 260 + i * 70), b, 36, fill=(30, 140, 60), stroke=False, anchor="rm")
    c.alpha_composite(nine("header", w - 80, 74), (x + 40, y + 500))
    text(c, (x + w // 2, y + 534), "Gerekenler", 34)
    for i, (ic, have, need, ok) in enumerate([("res_titanium", "24.9K", "18.2K", True), ("res_food", "5.000", "7.400", False), ("res_alloy", "3", "2", True)]):
        yy = y + 600 + i * 120
        c.alpha_composite(nine("inset", w - 80, 104), (x + 40, yy))
        c.alpha_composite(icon(ic, 76), (x + 64, yy + 14))
        text(c, (x + 170, yy + 52), f"{have} / {need}", 40, fill=(30, 140, 60) if ok else (210, 50, 40), stroke=False, anchor="lm")
        if ok:
            ImageDraw.Draw(c).line([(x + w - 110, yy + 52), (x + w - 96, yy + 68), (x + w - 70, yy + 34)], fill=(30, 160, 70), width=9, joint="curve")
        else:
            text(c, (x + w - 90, yy + 52), "+", 52, fill=(230, 140, 30), stroke=False)
    c.alpha_composite(nine("bar_track", w - 80, 40), (x + 40, y + 980))
    c.alpha_composite(nine("bar_gold", int((w - 80) * 0.62), 40), (x + 40, y + 980))
    text(c, (x + w // 2, y + 1000), "1 sa 12 dk", 28)
    button(c, x + 60, y + 1080, 400, 150, "gold", "Hemen 120")
    button(c, x + w - 460, y + 1080, 400, 150, "green", "Yükselt")
    return c


def mock_bag(bg):
    c = bg.copy()
    c.alpha_composite(Image.new("RGBA", c.size, (0, 6, 16, 150)))
    x, y, w, h = 30, 260, 1020, 1240
    window(c, x, y, w, h, "Çanta")
    tabs = ["Özel", "Kaynak", "Hızlandırma", "Kahraman"]
    tw = (w - 80) // 4
    for i, t in enumerate(tabs):
        on = i == 1
        c.alpha_composite(nine("tab_on" if on else "tab_off", tw - 8, 88), (x + 40 + i * tw, y + 140))
        text(c, (x + 40 + i * tw + (tw - 8) // 2, y + 182), t, 30, fill=INK if on else (255, 255, 255), stroke=not on)
    items = [("legendary", "res_titanium", "1M", 3), ("epic", "res_titanium", "250K", 12), ("rare", "res_food", "50K", 36), ("rare", "res_biomass", "50K", 8),
             ("common", "res_oxygen", "10K", 54), ("epic", "res_energy", "100K", 2), ("rare", "res_titanium", "20K", 19), ("common", "res_food", "5K", 140)]
    for i, (rar, ic, amt, n) in enumerate(items):
        col, row = i % 4, i // 4
        sx, sy = x + 50 + col * 235, y + 270 + row * 245
        c.alpha_composite(nine("slot_" + rar, 215, 215), (sx, sy))
        c.alpha_composite(icon(ic, 120), (sx + 48, sy + 52))
        text(c, (sx + 107, sy + 34), amt, 34)
        text(c, (sx + 195, sy + 188), str(n), 34, anchor="rm")
    # The use panel under the chosen row.
    py = y + 770
    c.alpha_composite(nine("inset", w - 80, 380), (x + 40, py))
    text(c, (x + w // 2, py + 60), "250K Titanyum Paketi", 42, fill=INK, stroke=False)
    text(c, (x + w // 2, py + 115), "Kullanıldığında 250.000 Titanyum verir.", 30, fill=(80, 96, 120), stroke=False, heavy=False)
    button(c, x + 70, py + 160, 90, 90, "blue", "-", 48)
    c.alpha_composite(nine("bar_track", 520, 34), (x + 180, py + 186))
    c.alpha_composite(nine("bar_blue", 220, 34), (x + 180, py + 186))
    button(c, x + 720, py + 160, 90, 90, "blue", "+", 48)
    text(c, (x + 880, py + 200), "4", 44, fill=INK, stroke=False)
    button(c, x + w // 2 - 200, py + 270, 400, 120, "blue", "Kullan")
    return c


def mock_march(bg):
    c = bg.copy()
    c.alpha_composite(Image.new("RGBA", c.size, (0, 6, 16, 120)))
    x, y, w, h = 40, 640, 1000, 1180
    window(c, x, y, w, h, "Sefer Düzenle")
    c.alpha_composite(nine("inset", w - 80, 150), (x + 40, y + 150))
    for i, (ic, a, b) in enumerate([("res_titanium", "Toplanacak", "2.800 / 2.800"), ("queue_build", "Süre", "12:26:40")]):
        c.alpha_composite(icon(ic, 52), (x + 70, y + 168 + i * 66))
        text(c, (x + 140, y + 194 + i * 66), a, 32, fill=(60, 76, 100), stroke=False, anchor="lm", heavy=False)
        text(c, (x + w - 80, y + 194 + i * 66), b, 34, fill=INK, stroke=False, anchor="rm")
    heroes = [("aelis", "legendary"), ("rourke", "epic"), ("solenne", "rare"), ("kade", "rare"), ("mei", "common")]
    for i, (p, rar) in enumerate(heroes):
        sx = x + 50 + i * 182
        c.alpha_composite(nine("slot_" + rar, 166, 200), (sx, y + 330))
        c.alpha_composite(portrait(p, 146, 180), (sx + 10, y + 340))
        c.alpha_composite(nine("bar_green", 166, 48), (sx, y + 540))
        text(c, (sx + 83, y + 562), "104", 30)
    c.alpha_composite(nine("bar_track", w - 80, 50), (x + 40, y + 630))
    c.alpha_composite(nine("bar_green", int((w - 80) * 0.77), 50), (x + 40, y + 630))
    text(c, (x + w // 2, y + 655), "Asker 400 / 520", 32)
    text(c, (x + w // 2, y + 760), "Yol 5:00", 40, fill=INK, stroke=False)
    button(c, x + w // 2 - 260, y + 840, 520, 160, "blue", "Yola Çık", 56)
    return c


def mockup(out_dir):
    out_dir = Path(out_dir)
    bg = Image.open(out_dir / "t1-full.png").convert("RGBA").resize((1080, 2400))
    shots = [mock_upgrade(bg), mock_bag(bg), mock_march(bg)]
    sheet = Image.new("RGB", (1080 * 3 + 80, 2400), (12, 16, 22))
    for i, s in enumerate(shots):
        sheet.paste(s.convert("RGB"), (i * 1120, 0))
    sheet.resize((sheet.width // 2, sheet.height // 2), Image.LANCZOS).save(out_dir / "skin_mockup.png")
    for i, s in enumerate(shots):
        s.convert("RGB").resize((540, 1200), Image.LANCZOS).save(out_dir / f"skin_mock_{i}.png")
    print("mockup ->", out_dir / "skin_mockup.png")


if __name__ == "__main__":
    build()
    if "--mockup" in sys.argv:
        mockup(sys.argv[sys.argv.index("--mockup") + 1])
