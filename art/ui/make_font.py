"""HADAL's UI font: Fredoka (SIL OFL 1.1) cut to a static SemiBold, with the Turkish letters it lacks built in.

    python art/ui/make_font.py <Fredoka[wdth,wght].ttf> [out.ttf]

Fredoka has no ğ Ğ ş Ş İ; they are added as composites of the font's own g/G/s/S/I and its breve, cedilla and dot
accent, placed the way its ç and accented capitals place theirs. Writes Assets/_Hadal/Resources/Fonts/Fredoka-SemiBold.ttf.
"""
import sys
from pathlib import Path

from fontTools.pens.boundsPen import BoundsPen
from fontTools.ttLib import TTFont
from fontTools.ttLib.tables._g_l_y_f import Glyph, GlyphComponent
from fontTools.varLib import instancer

ROOT = Path(__file__).resolve().parents[2]


def bounds(font, name):
    pen = BoundsPen(font.getGlyphSet())
    font.getGlyphSet()[name].draw(pen)
    return pen.bounds


def composite(font, name, codepoint, base, mark, dy):
    """name = base + mark centred over it, shifted dy up (negative: down)."""
    glyf, hmtx = font["glyf"], font["hmtx"]
    bx0, _, bx1, _ = bounds(font, base)
    mx0, _, mx1, _ = bounds(font, mark)
    dx = round((bx0 + bx1) / 2 - (mx0 + mx1) / 2)
    glyph = Glyph()
    glyph.numberOfContours = -1
    glyph.components = []
    for comp_name, x, y in ((base, 0, 0), (mark, dx, round(dy))):
        c = GlyphComponent()
        c.glyphName, c.x, c.y, c.flags = comp_name, x, y, 0x4 if comp_name == base else 0
        glyph.components.append(c)
    glyf[name] = glyph
    hmtx[name] = hmtx[base]
    font.setGlyphOrder(font.getGlyphOrder() + [name] if name not in font.getGlyphOrder() else font.getGlyphOrder())
    for table in font["cmap"].tables:
        if table.isUnicode():
            table.cmap[codepoint] = name


def main():
    src = sys.argv[1]
    out = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT / "Assets/_Hadal/Resources/Fonts/Fredoka-SemiBold.ttf"
    font = instancer.instantiateVariableFont(TTFont(src), {"wght": 600, "wdth": 100})

    os2 = font["OS/2"]
    x_height, cap_height = os2.sxHeight, os2.sCapHeight
    # The spacing accents sit at lowercase height; capitals take them raised by the cap/x-height gap.
    lift = cap_height - x_height
    _, cy0, _, _ = bounds(font, "cedilla")
    _, cc0, _, _ = bounds(font, "ccedilla")
    cedilla_dy = cc0 - cy0  # where ç puts its cedilla

    composite(font, "gbreve", 0x11F, "g", "breve", 0)
    composite(font, "Gbreve", 0x11E, "G", "breve", lift)
    composite(font, "scedilla", 0x15F, "s", "cedilla", cedilla_dy)
    composite(font, "Scedilla", 0x15E, "S", "cedilla", cedilla_dy)
    composite(font, "Idotaccent", 0x130, "I", "dotaccent", lift)

    font["maxp"].numGlyphs = len(font.getGlyphOrder())
    out.parent.mkdir(parents=True, exist_ok=True)
    font.save(out)
    print("font ->", out)


if __name__ == "__main__":
    main()
