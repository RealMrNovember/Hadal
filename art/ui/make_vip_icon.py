"""The VIP crown icon (Resources/Icons/vip.png): a gold crown with sea-glass gems, drawn like the other HUD icons.

    python art/ui/make_vip_icon.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
N, S = 256, 4


def main():
    size = N * S
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Shadow.
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    crown = [(30, 190), (30, 80), (80, 130), (128, 50), (176, 130), (226, 80), (226, 190)]
    sd.polygon([((x + 6) * S, (y + 10) * S) for x, y in crown], fill=(0, 0, 0, 140))
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(8 * S)))
    # Dark rim, gold body with a vertical shine.
    d.polygon([(x * S, y * S) for x, y in crown], fill=(110, 62, 10, 255))
    inner = [(40, 182), (40, 102), (82, 142), (128, 70), (174, 142), (216, 102), (216, 182)]
    body = Image.new("RGBA", img.size, (0, 0, 0, 0))
    bd = ImageDraw.Draw(body)
    for y in range(size):
        t = y / size
        c = (int(255 - 40 * t), int(214 - 90 * t), int(90 - 60 * t), 255)
        bd.line([(0, y), (size, y)], fill=c)
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).polygon([(x * S, y * S) for x, y in inner], fill=255)
    img.paste(body, (0, 0), mask)
    # Band and gems.
    d.rounded_rectangle([34 * S, 168 * S, 222 * S, 204 * S], radius=8 * S, fill=(150, 88, 18, 255))
    d.rounded_rectangle([40 * S, 172 * S, 216 * S, 196 * S], radius=6 * S, fill=(240, 180, 60, 255))
    for x, color in [(80, (60, 220, 210)), (128, (230, 60, 80)), (176, (60, 220, 210))]:
        d.ellipse([(x - 12) * S, 172 * S, (x + 12) * S, 196 * S], fill=(30, 30, 40, 255))
        d.ellipse([(x - 9) * S, 175 * S, (x + 9) * S, 193 * S], fill=color + (255,))
        d.ellipse([(x - 5) * S, 177 * S, (x + 1) * S, 183 * S], fill=(255, 255, 255, 200))
    for x, y in [(30, 80), (128, 50), (226, 80)]:
        d.ellipse([(x - 14) * S, (y - 14) * S, (x + 14) * S, (y + 14) * S], fill=(110, 62, 10, 255))
        d.ellipse([(x - 10) * S, (y - 10) * S, (x + 10) * S, (y + 10) * S], fill=(255, 226, 120, 255))
    out = ROOT / "Assets/_Hadal/Resources/Icons/vip.png"
    img.resize((N, N), Image.LANCZOS).save(out)
    print("icon ->", out)


if __name__ == "__main__":
    main()
