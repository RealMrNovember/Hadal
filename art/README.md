# HADAL art pipeline (no GPU needed)

Every building and seabed prop is Python code for headless Blender. Changing a design means changing code and running the generator again; the output is the same on every run.

| File | Contents |
|---|---|
| `blender/hadal_kit.py` | Palette (`HAD_*` materials), primitives, `pad()`, `anim()`, assembly, FBX export, Cycles CPU preview |
| `blender/buildings.py` | One function per catalog building (`shared/catalog/catalog.json`) |
| `blender/environment.py` | Seabed tile, rocks, kelp, coral, thermal vent |
| `blender/generate.py` | Builds everything, writes FBX and preview images, checks the triangle budget |

## Running it (DL380, CT 160)

```bash
cd /opt/hadal
su hadal -c "/opt/blender/blender -b --factory-startup --python art/blender/generate.py -- --project /opt/hadal"
# fast run without previews:     ... -- --project /opt/hadal --no-preview
# selected assets only:          ... -- --project /opt/hadal --only core_reactor,rock_large
```

Outputs:

- `Assets/_Hadal/Art/Buildings/<id>.fbx`
- `Assets/_Hadal/Art/Environment/<id>.fbx`
- `artifacts/art-preview/<id>.png`, `lineup.png` and `report.json` (triangle counts and over-budget flags)

A full run with previews takes about 3 minutes on 12 CPU cores.

Then run `server/unity/unity.sh exec Hadal.EditorTools.HadalProjectSetup.CreateBootstrapScene`. It sets up the URP pipeline and palette materials, reimports the art, links the prefabs and builds the world scene.

## Conventions

- **Scale:** 1 Blender unit equals 1 Unity metre. Each building sits on a pad of at most 2.3 m radius, because the inner rings are only 5 m apart (`catalog.json` → `grid.rings`).
- **Materials:** `HAD_<Name>`. `HadalArtImporter` remaps them on import to the URP materials in `Assets/_Hadal/Art/Materials`, built by `HadalArtPalette`. The palette is duplicated in Python and C#, so a change must be made in both files.
- **Animation:** an animated part has a name of the form `ANIM_<kind>_<value>__<part>`, and the importer adds an `ArtAnimator` to it.

  | Kind | Value | Effect |
  |---|---|---|
  | `spin_<axis>` | degrees per second | rotates the part |
  | `pulse` | Hz | pulses its emission |
  | `sway` | Hz | sways the part around its base |

- **Budget:** at most 6,000 triangles per building (`TRIANGLE_BUDGET`).

## Characters

The crew and heroes are rigged and animated, so the procedural kit cannot produce them. Options:

- CC0 packs (Quaternius, Kenney)
- an AI image-to-3D model on a cloud GPU (TRELLIS / Hunyuan3D), cleaned up afterwards in Blender
