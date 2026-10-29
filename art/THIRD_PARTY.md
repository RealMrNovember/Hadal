# Third-party art sources

Everything the game ships under `Assets/_Hadal/Art/` is listed here: what it comes from, under which license, and how it is rebuilt. Source packs are not committed. Keep them on the build server under `/opt/hadal-thirdparty/` (CT 160).

| Used in | Source | License | Retrieved | Rebuilt by |
|---|---|---|---|---|
| `Art/Characters/crew_*`, `mech_*`, `creature_*` | **Ultimate Space Kit** by Quaternius. Mirror: [OpenGameArt](https://opengameart.org/content/ultimate-space-kit-by-quaternius) (`ultimate_space_kit-glb.zip`). Origin: quaternius.com | CC0 1.0 (public domain) | 2026-10-25 | `art/blender/characters.py` (recolour, scale, decimate, clip clean-up) |
| `Art/Portraits/*` | Generated with **Stable Diffusion XL base 1.0** (Stability AI) on doku2, `art/gpu/portraits.py` | CreativeML Open RAIL++-M: commercial use allowed, with the license's use restrictions | 2026-10-25 | `art/gpu/portraits.py` with `art/gpu/heroes.json` (fixed seeds) |
| `Art/Props/*` (wildlife, submarines, ruins) | Generated on doku2: SDXL concept → BiRefNet cut-out → **TRELLIS-image-large** (Microsoft) | SDXL: CreativeML Open RAIL++-M; TRELLIS: MIT. Usable for shipped assets | 2026-10-25 | `art/gpu/batch_3d.sh set1` → `art/blender/props.py` (roster `art/gpu/assets.json`) |
| `Art/Creatures/*` (map monsters, event bosses, schools of fish and jellies) | Generated on doku2 from our own text prompts: SDXL concept → BiRefNet cut-out → **TRELLIS-image-large**. Sample models the user collected (Sketchfab anglerfish, crab, angelfish, cephalopod; a CC0 goosefish) were only looked at for silhouettes and motion ideas; none of their meshes, textures or renders went into the pipeline or the game | SDXL: CreativeML Open RAIL++-M; TRELLIS: MIT. Usable for shipped assets | 2026-10-29 | `ROSTER=creatures.json art/gpu/batch_3d.sh creatures` → `art/blender/props.py --roster art/gpu/creatures.json --out-dir Assets/_Hadal/Art/Creatures --bake-yaw` → `unity.sh exec Hadal.EditorTools.HadalCreatureSetup.Build` |
| (3D pipeline, `art/gpu/image_to_3d.py --model trellis`) | **TRELLIS-image-large** (Microsoft) on doku2 | MIT (code and weights): usable for shipped assets | 2026-10-25 | `art/gpu/concept.py` → `image_to_3d.py` |
| (blockout only, `--model hunyuan-mini*`) | **Hunyuan3D-2mini** (Tencent) | Tencent Hunyuan 3D 2.0 Community License: **excludes the EU, UK and South Korea** (§5c bans using or displaying Output there). **Never ship Hunyuan output in a global build** without a license from Tencent | 2026-10-25 | — |
| `Resources/Fonts/HadalSansWorld-Regular.ttf` (Arabic and Thai UI text) | **Noto Sans**, **Noto Sans Arabic**, **Noto Sans Thai** from [notofonts](https://github.com/notofonts/notofonts.github.io), merged with fontTools (layout tables dropped; UGUI does not use them) | SIL Open Font License 1.1 (`art/licenses/Noto-OFL.txt`); renamed as the OFL requires for modified fonts | 2026-10-25 | fontTools `merge` of the three Regular TTFs |
| `Art/Buildings/*`, `Art/Environment/*` | Our own procedural art, `art/blender/generate.py` | Ours | — | `art/blender/generate.py` |

CC0 requires no attribution. Quaternius is still credited in the game's credits screen as a courtesy.

## Fetching the packs again

```bash
mkdir -p /opt/hadal-thirdparty/space-kit && cd /opt/hadal-thirdparty/space-kit
curl -fsSL -o pack.zip https://opengameart.org/sites/default/files/ultimate_space_kit-glb.zip
python3 -c "import zipfile; z = zipfile.ZipFile('pack.zip'); assert not [n for n in z.namelist() if n.startswith('/') or '..' in n]; z.extractall('x')"
```
