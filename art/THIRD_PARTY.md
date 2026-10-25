# Third-party art sources

Everything the game ships under `Assets/_Hadal/Art/` is listed here: what it comes from, under which license, and how it is rebuilt. Source packs are not committed. Keep them on the build server under `/opt/hadal-thirdparty/` (CT 160).

| Used in | Source | License | Retrieved | Rebuilt by |
|---|---|---|---|---|
| `Art/Characters/crew_*`, `mech_*`, `creature_*` | **Ultimate Space Kit** by Quaternius. Mirror: [OpenGameArt](https://opengameart.org/content/ultimate-space-kit-by-quaternius) (`ultimate_space_kit-glb.zip`). Origin: quaternius.com | CC0 1.0 (public domain) | 2026-10-25 | `art/blender/characters.py` (recolour, scale, decimate, clip clean-up) |
| `Art/Portraits/*` | Generated with **Stable Diffusion XL base 1.0** (Stability AI) on doku2, `art/gpu/portraits.py` | CreativeML Open RAIL++-M: commercial use allowed, with the license's use restrictions | 2026-10-25 | `art/gpu/portraits.py` with `art/gpu/heroes.json` (fixed seeds) |
| `Art/Buildings/*`, `Art/Environment/*` | Our own procedural art, `art/blender/generate.py` | Ours | — | `art/blender/generate.py` |

CC0 requires no attribution. Quaternius is still credited in the game's credits screen as a courtesy.

## Fetching the packs again

```bash
mkdir -p /opt/hadal-thirdparty/space-kit && cd /opt/hadal-thirdparty/space-kit
curl -fsSL -o pack.zip https://opengameart.org/sites/default/files/ultimate_space_kit-glb.zip
python3 -c "import zipfile; z = zipfile.ZipFile('pack.zip'); assert not [n for n in z.namelist() if n.startswith('/') or '..' in n]; z.extractall('x')"
```
