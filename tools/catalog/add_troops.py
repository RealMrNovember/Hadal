"""Adds troops (D1) to a catalog JSON: the `troops` section from docs/rehber/veri/birlikler.json and the four
training buildings, whose level tables follow the catalog's own Military Dome (so they fit its economy).

    python tools/catalog/add_troops.py <catalog.json> [--out <file>]

Run it on a catalog without troops (it refuses a second run). Every other line stays as it is.
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

YARDS = [
    # id, troop type, English name, reactor that opens it
    ("armor_yard", "armored", "Armor Training Yard", 5),
    ("harpoon_yard", "harpoon", "Harpoon Training Yard", 5),
    ("arc_yard", "arc", "Arc Training Yard", 6),
    ("driller_yard", "driller", "Driller Training Yard", 8),
]

# A training yard costs and takes a share of a Military Dome level (one per type, so four of them).
YARD_COST = 0.6
YARD_TIME = 0.8
YARD_POWER = 0.5


def troops_section():
    data = json.loads((ROOT / "docs/rehber/veri/birlikler.json").read_text(encoding="utf-8"))
    types = [{
        "id": t["id"], "building": next(y[0] for y in YARDS if y[1] == t["id"]),
        "atk": t["atk_c"], "def": t["def_c"], "hp": t["hp_c"], "speed": t["hiz"], "load": t["yuk_c"],
    } for t in data["turler"]]
    tiers = [{
        "tier": int(k["id"][1:]), "buildingLevel": k["tatbikat"], "power": k["guc"],
        "food": k["gida"], "biomass": k["biyokutle"], "titanium": k["titanyum"], "alloy": k["alasim"] or 0,
        "seconds": k["sure"], "upkeepFood": k["bakim_gida"], "upkeepOxygen": k["bakim_o2"], "load": k["yuk"],
    } for k in data["kademeler"]]
    capacity = [{"level": r["seviye"], "value": r["ordu"]} for r in data["kapasite"]["askeri_kubbe"]["satirlar"]]
    batch = [{"level": r["seviye"], "value": r["parti"]} for r in data["kapasite"]["tatbikat"]["satirlar"]]
    return {
        "types": types, "tiers": tiers, "capacity": capacity, "batch": batch,
        "speedPerLevel": data["egitim"]["tatbikat_hiz_seviye_basi"], "capacityBuilding": "military_dome",
    }


def scaled(amounts, factor):
    out = []
    for a in amounts:
        out.append({"resource": a["resource"], "amount": max(1, int(round(a["amount"] * factor)))})
    return out


def yard_building(dome, yard_id, name, reactor):
    levels = []
    for level in dome["levels"]:
        levels.append({
            "reactor": max(reactor, level["reactor"]),
            "cost": scaled(level["cost"], YARD_COST),
            "seconds": max(1, int(round(level["seconds"] * YARD_TIME))),
            "power": max(1, int(round(level["power"] * YARD_POWER))),
            "production": [],
            "consumption": [],
        })
    return {
        "id": yard_id, "displayName": name, "category": "Military", "slotType": "Military",
        "minRing": dome.get("minRing", 3), "maxRing": dome.get("maxRing", 12),
        "buildSeconds": dome.get("buildSeconds", 60), "requiredReactorLevel": reactor, "maxCount": 1,
        "consumption": [], "buildCost": scaled(dome["buildCost"], YARD_COST), "levels": levels, "countUnlocks": [reactor],
    }


def apply(catalog):
    dome = next(b for b in catalog["buildings"] if b["id"] == "military_dome")
    ids = {y[0] for y in YARDS}
    catalog["buildings"] = [b for b in catalog["buildings"] if b["id"] not in ids]
    at = catalog["buildings"].index(dome) + 1
    for offset, (yard_id, _, name, reactor) in enumerate(YARDS):
        catalog["buildings"].insert(at + offset, yard_building(dome, yard_id, name, reactor))
    catalog["troops"] = troops_section()
    return catalog


def block(value, indent):
    """One JSON value laid out with 2-space indents, shifted right by `indent` spaces."""
    text = json.dumps(value, ensure_ascii=False, indent=2)
    return text.replace("\n", "\n" + " " * indent)


def splice(text, catalog):
    """Inserts the yards after the Military Dome and the troops section at the end, leaving every other line as it is."""
    nl = "\r\n" if "\r\n" in text else "\n"
    body = text.replace("\r\n", "\n")
    original = json.loads(body)
    if any(b["id"] in {y[0] for y in YARDS} for b in original["buildings"]) or "troops" in original:
        raise SystemExit("already has troops; restore the file first (git checkout) to re-run")
    apply(catalog)
    yards = [b for b in catalog["buildings"] if b["id"] in {y[0] for y in YARDS}]

    # The Military Dome object ends at the first "    }," after its id.
    start = body.index('"id": "military_dome"')
    end = body.index("\n    },", start) + len("\n    },")
    insert = "".join("\n    " + block(y, 4) + "," for y in yards)
    body = body[:end] + insert + body[end:]

    last = body.rstrip().rindex("}")
    head = body[:last].rstrip()
    body = head + ',\n  "troops": ' + block(catalog["troops"], 2) + "\n}\n"
    return body.replace("\n", nl)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    source = Path(sys.argv[1])
    out = Path(sys.argv[sys.argv.index("--out") + 1]) if "--out" in sys.argv else source
    raw = source.read_bytes().decode("utf-8")
    catalog = json.loads(raw)
    text = splice(raw, catalog)
    json.loads(text)
    out.write_bytes(text.encode("utf-8"))
    if out.resolve() == (ROOT / "shared/catalog/catalog.json").resolve():
        (ROOT / "Assets/_Hadal/Resources/catalog.json").write_bytes(text.encode("utf-8"))
    print(f"{out}: {len(catalog['troops']['tiers'])} tiers, {len(YARDS)} training yards")


if __name__ == "__main__":
    main()
