"""The notification small icon: the logo's H and the lure light as a white silhouette on transparent (docs/36).

    python art/logo/make_notify_icon.py

Android draws a notification's small icon from its alpha channel only (white on transparent, no colour), at 24 dp.
Writes Assets/_Hadal/Art/Branding/Notify~/drawable-<density>/hadal_notify.png for mdpi..xxxhdpi; the folder ends in
"~" so Unity does not import it, and HadalNotificationIcons copies it into the APK's res/ at build time. Same H
geometry as make_icon.py, drawn larger (the icon has no background to fill).
"""
from pathlib import Path

from PIL import Image, ImageDraw

from make_icon import h_mark

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Hadal/Art/Branding/Notify~"
DENSITIES = {"mdpi": 24, "hdpi": 36, "xhdpi": 48, "xxhdpi": 72, "xxxhdpi": 96}
WORK = 1024


def silhouette(size):
    mask, (lx, ly), _ = h_mark(WORK, 0.66)
    draw = ImageDraw.Draw(mask)
    r = WORK * 0.05
    draw.ellipse([lx - r, ly - r - WORK * 0.02, lx + r, ly + r - WORK * 0.02], fill=255)
    # Android keeps 2 dp of padding inside the 24 dp square: crop to the art and fit it into 20/24 of the side.
    box = mask.getbbox()
    art = mask.crop(box)
    side = max(art.size)
    square = Image.new("L", (side, side), 0)
    square.paste(art, ((side - art.size[0]) // 2, (side - art.size[1]) // 2))
    inner = round(size * 20 / 24)
    alpha = square.resize((inner, inner), Image.LANCZOS)
    out = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    white = Image.new("RGBA", (inner, inner), (255, 255, 255, 255))
    white.putalpha(alpha)
    out.paste(white, ((size - inner) // 2, (size - inner) // 2), white)
    return out


def main():
    for density, size in DENSITIES.items():
        folder = OUT / ("drawable-" + density)
        folder.mkdir(parents=True, exist_ok=True)
        silhouette(size).save(folder / "hadal_notify.png", optimize=True)
    print("notification icons written to", OUT)


if __name__ == "__main__":
    main()
