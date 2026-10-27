"""Adds the army to a catalog JSON (D1–D3): the `troops` section from docs/rehber/veri/birlikler.json (types, tiers,
army cap, training, the infirmary, hero command capacity) and the military buildings it needs (four training yards,
the Med Bay), whose costs follow the catalog's own Military Dome so they fit its economy.

    python tools/catalog/add_troops.py <catalog.json> [--out <file>] [--march-load <scale>]

Safe to run again: buildings already in the catalog are left alone, the troops section is rewritten. On the repo's
catalog every other line stays as it is; a one-line catalog (a live version from the database) is written whole.
--march-load sets how much one troop carries on a march relative to its tier load (live v8 economy: 0.08).
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

BUILDINGS = [
    # id, troop type (None = not a yard), English name, reactor that opens it, copies
    ("armor_yard", "armored", "Armor Training Yard", 5, 1),
    ("harpoon_yard", "harpoon", "Harpoon Training Yard", 5, 1),
    ("arc_yard", "arc", "Arc Training Yard", 6, 1),
    ("driller_yard", "driller", "Driller Training Yard", 8, 1),
    ("med_bay", None, "Med Bay", 6, 2),
]
COUNT_UNLOCKS = {"med_bay": [6, 15]}

# A military building costs and takes a share of a Military Dome level.
SHARE_COST = 0.6
SHARE_TIME = 0.8
SHARE_POWER = 0.5

# Hero class → the troop type it leads best (docs/rehber 05 §9).
AFFINITY = {"Commander": "armored", "Medic": "armored", "Hunter": "harpoon", "Pilot": "harpoon", "Engineer": "arc", "Scientist": "arc"}


def troops_section(march_load):
    data = json.loads((ROOT / "docs/rehber/veri/birlikler.json").read_text(encoding="utf-8"))
    yards = {b[1]: b[0] for b in BUILDINGS if b[1]}
    types = [{
        "id": t["id"], "building": yards[t["id"]],
        "atk": t["atk_c"], "def": t["def_c"], "hp": t["hp_c"], "speed": t["hiz"], "load": t["yuk_c"],
    } for t in data["turler"]]
    tiers = [{
        "tier": int(k["id"][1:]), "buildingLevel": k["tatbikat"], "power": k["guc"],
        "food": k["gida"], "biomass": k["biyokutle"], "titanium": k["titanyum"], "alloy": k["alasim"] or 0,
        "seconds": k["sure"], "upkeepFood": k["bakim_gida"], "upkeepOxygen": k["bakim_o2"], "load": k["yuk"],
    } for k in data["kademeler"]]
    capacity = [{"level": r["seviye"], "value": r["ordu"]} for r in data["kapasite"]["askeri_kubbe"]["satirlar"]]
    batch = [{"level": r["seviye"], "value": r["parti"]} for r in data["kapasite"]["tatbikat"]["satirlar"]]
    infirmary = [{"level": r["seviye"], "value": r["kapasite"]} for r in data["kapasite"]["revir"]["satirlar"]]
    cure = data["iyilestirme"]
    return {
        "types": types, "tiers": tiers, "capacity": capacity, "batch": batch,
        "speedPerLevel": data["egitim"]["tatbikat_hiz_seviye_basi"], "capacityBuilding": "military_dome",
        "infirmary": {
            "building": "med_bay", "capacity": infirmary,
            "costPercent": cure["bedel_yuzde"], "timePercent": cure["sure_yuzde"], "speedPerLevel": cure["revir_hiz_seviye_basi"],
        },
        "command": {"base": 50, "perLevel": 6, "perStar": 30, "perDomeLevel": 3, "maxHeroes": 5},
        "affinity": [{"heroClass": k, "type": v} for k, v in AFFINITY.items()],
        "marchLoad": march_load,
    }


def scaled(amounts, factor):
    return [{"resource": a["resource"], "amount": max(1, int(round(a["amount"] * factor)))} for a in amounts]


def military_building(dome, building_id, name, reactor, copies):
    """Shaped like the catalog's Military Dome: its level table when it has one (v9), else the formula fields (v8)."""
    building = {
        "id": building_id, "displayName": name, "category": "Military", "slotType": "Military",
        "minRing": dome.get("minRing", 3), "maxRing": dome.get("maxRing", 12),
        "buildSeconds": max(1, int(round(dome.get("buildSeconds", 60) * SHARE_TIME))), "requiredReactorLevel": reactor,
        "maxCount": copies, "consumption": [], "buildCost": scaled(dome["buildCost"], SHARE_COST),
    }
    if "levels" not in dome:
        building["production"] = []
        return building
    building["levels"] = [{
        "reactor": max(reactor, level["reactor"]),
        "cost": scaled(level["cost"], SHARE_COST),
        "seconds": max(1, int(round(level["seconds"] * SHARE_TIME))),
        "power": max(1, int(round(level["power"] * SHARE_POWER))),
        "production": [],
        "consumption": [],
    } for level in dome["levels"]]
    building["countUnlocks"] = COUNT_UNLOCKS.get(building_id, [reactor] * copies)
    return building


def missing_buildings(catalog):
    dome = next(b for b in catalog["buildings"] if b["id"] == "military_dome")
    have = {b["id"] for b in catalog["buildings"]}
    return [military_building(dome, bid, name, reactor, copies) for bid, _, name, reactor, copies in BUILDINGS if bid not in have]


def apply(catalog, march_load):
    added = missing_buildings(catalog)
    at = next(i for i, b in enumerate(catalog["buildings"]) if b["id"] == "military_dome") + 1
    while at < len(catalog["buildings"]) and catalog["buildings"][at]["id"] in {b[0] for b in BUILDINGS}:
        at += 1
    catalog["buildings"][at:at] = added
    catalog["troops"] = troops_section(march_load)
    return catalog


def block(value, indent):
    """One JSON value laid out with 2-space indents, shifted right by `indent` spaces."""
    text = json.dumps(value, ensure_ascii=False, indent=2)
    return text.replace("\n", "\n" + " " * indent)


def splice(text, catalog, march_load):
    """Inserts missing buildings after the military ones and rewrites the troops section at the end; other lines stay."""
    nl = "\r\n" if "\r\n" in text else "\n"
    body = text.replace("\r\n", "\n")
    added = missing_buildings(catalog)

    # After the last military building already there (the Dome or a yard): each object ends at "\n    },".
    ids = ["military_dome"] + [s[0] for s in BUILDINGS]
    last = max(body.index(f'"id": "{b}"') for b in ids if f'"id": "{b}"' in body)
    end = body.index("\n    },", last) + len("\n    },")
    body = body[:end] + "".join("\n    " + block(b, 4) + "," for b in added) + body[end:]

    section = block(troops_section(march_load), 2)
    marker = '\n  "troops": '
    if marker in body:
        body = body[:body.index(marker)] + marker + section + "\n}\n"
    else:
        head = body[:body.rstrip().rindex("}")].rstrip()
        body = head + "," + marker + section + "\n}\n"
    return body.replace("\n", nl)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    source = Path(sys.argv[1])
    out = Path(sys.argv[sys.argv.index("--out") + 1]) if "--out" in sys.argv else source
    march_load = float(sys.argv[sys.argv.index("--march-load") + 1]) if "--march-load" in sys.argv else 1.0
    raw = source.read_bytes().decode("utf-8")
    catalog = json.loads(raw)
    if "\n    }," in raw.replace("\r\n", "\n"):
        text = splice(raw, catalog, march_load)
    else:
        text = json.dumps(apply(catalog, march_load), ensure_ascii=False)
    result = json.loads(text)
    out.write_bytes(text.encode("utf-8"))
    if out.resolve() == (ROOT / "shared/catalog/catalog.json").resolve():
        (ROOT / "Assets/_Hadal/Resources/catalog.json").write_bytes(text.encode("utf-8"))
    ids = [b["id"] for b in result["buildings"]]
    print(f"{out}: {len(result['troops']['tiers'])} tiers, buildings {[b[0] for b in BUILDINGS if b[0] in ids]}")


if __name__ == "__main__":
    main()
