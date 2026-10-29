"""HADAL's chat emoji font: Noto Emoji (monochrome, SIL OFL 1.1) re-mapped to the Private Use Area.

    python art/ui/make_emoji_font.py <NotoEmoji[wght].ttf> <emoji-test.txt>

The UI draws text with legacy UnityEngine.UI.Text, which looks glyphs up one UTF-16 unit at a time: emoji outside
the basic plane (surrogate pairs) and multi-codepoint sequences (ZWJ families, flags, keycaps, skin tones) never
reach a font. So every emoji glyph gets its own Private Use Area codepoint (U+E001 up) and the game swaps an emoji
sequence for that one character plus U+E000 fillers (an empty, zero-width glyph) for the rest of its UTF-16 units.
The string keeps its length, so the input field's caret and selection indices stay right (Hadal.UI.Emoji).

Sequences come from Unicode's emoji-test.txt (https://unicode.org/Public/emoji/latest/emoji-test.txt); each one is
shaped with HarfBuzz on the font, and a sequence the font draws as a single glyph is mapped to it (U+FE0F is
dropped before matching). Writes:
  Assets/_Hadal/Resources/Fonts/HadalEmoji-Regular.ttf   the font, renamed as the OFL asks of modified fonts
  Assets/_Hadal/Resources/Fonts/HadalEmoji-map.txt       "E001 1F600" lines: PUA codepoint, then the sequence

Needs fontTools and uharfbuzz.
"""
import sys
from pathlib import Path

import uharfbuzz as hb
from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_Hadal/Resources/Fonts"
FILLER = 0xE000
FIRST = 0xE001
LAST = 0xF8FF
# Invisible joiners and selectors: never a glyph of their own in chat.
INVISIBLE = {0x200D, 0xFE0E, 0xFE0F, 0x20E3} | set(range(0xE0020, 0xE0080))
WEIGHT = 500
# The emoji are 1.27 em wide in Noto Emoji; a larger em draws them about the size of a capital letter pair.
UNITS_PER_EM = 2400


def sequences(emoji_test):
    """Every sequence of emoji-test.txt (any qualification), FE0F removed, in file order."""
    seen = []
    for line in Path(emoji_test).read_text(encoding="utf-8").splitlines():
        if not line or line.startswith("#") or ";" not in line:
            continue
        cps = [int(x, 16) for x in line.split(";")[0].split()]
        cps = [c for c in cps if c != 0xFE0F]
        if cps and cps not in seen:
            seen.append(cps)
    return seen


def main():
    src, emoji_test = sys.argv[1], sys.argv[2]
    font = instancer.instantiateVariableFont(TTFont(src), {"wght": WEIGHT})
    tmp = OUT / "_emoji_instance.ttf"
    font.save(tmp)

    blob = hb.Blob.from_file_path(str(tmp))
    face = hb.Face(blob)
    hbfont = hb.Font(face)
    order = font.getGlyphOrder()
    cmap = font.getBestCmap()

    def shape(cps):
        buf = hb.Buffer()
        buf.add_codepoints(cps)
        buf.guess_segment_properties()
        hb.shape(hbfont, buf, {})
        return [order[i.codepoint] for i in buf.glyph_infos]

    candidates = sequences(emoji_test)
    # Single characters the font has that emoji-test.txt lists only inside sequences (regional indicators).
    candidates += [[c] for c in sorted(cmap) if c >= 0x2000 and [c] not in candidates]

    glyph_pua = {}
    mapping = []
    for cps in candidates:
        if len(cps) == 1 and (cps[0] < 0x2000 or cps[0] in INVISIBLE):
            continue  # digits, '#', '*', ©, ® stay text; joiners are fillers
        glyphs = shape(cps)
        if len(glyphs) != 1 or glyphs[0] == ".notdef":
            continue
        g = glyphs[0]
        if g not in glyph_pua:
            glyph_pua[g] = FIRST + len(glyph_pua)
            if glyph_pua[g] > LAST:
                raise SystemExit("more glyphs than Private Use Area codepoints")
        mapping.append((glyph_pua[g], cps))

    # The filler: the font's own U+FE0F glyph, empty and with no advance.
    filler = cmap[0xFE0F]
    assert font["hmtx"][filler][0] == 0 and font["glyf"][filler].numberOfContours == 0, "U+FE0F is not an empty glyph"

    pua = {FILLER: filler}
    pua.update({cp: g for g, cp in glyph_pua.items()})
    for table in font["cmap"].tables:
        if table.isUnicode():
            table.cmap = dict(pua) if table.format != 4 else {k: v for k, v in pua.items() if k <= 0xFFFF}

    options = subset.Options()
    options.layout_features = []
    options.name_IDs = ["*"]
    options.notdef_outline = False
    options.drop_tables += ["GSUB", "GPOS", "GDEF", "vhea", "vmtx", "STAT"]
    subsetter = subset.Subsetter(options)
    subsetter.populate(unicodes=list(pua))
    subsetter.subset(font)

    # Scale by enlarging the em: every metric stays in proportion.
    font["head"].unitsPerEm = UNITS_PER_EM

    names = {1: "Hadal Emoji", 2: "Regular", 3: "Hadal Emoji Regular", 4: "Hadal Emoji Regular", 6: "HadalEmoji-Regular",
             16: "Hadal Emoji", 17: "Regular"}
    name = font["name"]
    for record in list(name.names):
        if record.nameID in names:
            name.removeNames(nameID=record.nameID)
    for nid, value in names.items():
        name.setName(value, nid, 3, 1, 0x409)
        name.setName(value, nid, 1, 0, 0)
    name.setName("Hadal Emoji is a modified version of Noto Emoji (SIL Open Font License 1.1): glyphs re-mapped to the "
                 "Private Use Area, weight 500, layout tables removed.", 10, 3, 1, 0x409)

    out = OUT / "HadalEmoji-Regular.ttf"
    font.save(out)
    tmp.unlink()
    with open(OUT / "HadalEmoji-map.txt", "w", encoding="ascii", newline="\n") as f:
        f.write("# PUA codepoint, then the emoji sequence (U+FE0F removed). Generated by art/ui/make_emoji_font.py.\n")
        for cp, cps in mapping:
            f.write("%04X %s\n" % (cp, " ".join("%04X" % c for c in cps)))
    print(f"{out}: {len(glyph_pua)} glyphs, {len(mapping)} sequences, {out.stat().st_size} bytes")


if __name__ == "__main__":
    main()
