"""Puts speed-up items into the chests and the daily chest rewards (where players get them).

    python tools/catalog/add_speedups.py <catalog.json> [--out <file>]

Bronze, silver and gold chests gain weighted speed-up draws; the 20- and 60-point daily chests add a small one.
The repo catalog is edited line by line (its layout stays); a one-line live catalog is rewritten whole.
Refuses a second run on the same file.
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

CHEST_DRAWS = {
    "chest_bronze": [(20, "speed_any_5", 2), (12, "speed_train_15", 1), (8, "speed_march_p25", 1)],
    "chest_silver": [(18, "speed_any_60", 1), (14, "speed_build_60", 1), (10, "speed_heal_60", 1), (10, "speed_march_p50", 1)],
    "chest_gold": [(20, "speed_any_480", 1), (15, "speed_research_60", 2), (10, "speed_march_p50", 2)],
}
DAILY_EXTRA = {20: ("speed_any_5", 3), 60: ("speed_march_p25", 1)}


def draw(weight, item, amount):
    return {"weight": weight, "reward": {"type": "item", "id": item, "amount": amount}}


def apply(catalog):
    for item in catalog["items"]:
        for weight, item_id, amount in CHEST_DRAWS.get(item["id"], []):
            item["table"].append(draw(weight, item_id, amount))
    for chest in catalog["quests"]["dailyChests"]:
        if chest["points"] in DAILY_EXTRA:
            item_id, amount = DAILY_EXTRA[chest["points"]]
            chest["rewards"].append({"type": "item", "id": item_id, "amount": amount})
    return catalog


def splice(text):
    nl = "\r\n" if "\r\n" in text else "\n"
    body = text.replace("\r\n", "\n")
    items = body.index('\n  "items": [')
    for chest, draws in CHEST_DRAWS.items():
        start = body.index(f'"id": "{chest}"', items)
        table = body.index('"table": [', start)
        end = body.index("\n      ]", table)
        lines = "".join(f',\n        {json.dumps(draw(w, i, a), ensure_ascii=False)}' for w, i, a in draws)
        body = body[:end] + lines + body[end:]
    daily = body.index('"dailyChests": [')
    for points, (item_id, amount) in DAILY_EXTRA.items():
        line_start = body.index(f'{{"points": {points}, "rewards": [', daily)
        close = body.index("]}", line_start)
        body = body[:close] + f', {{"type": "item", "id": "{item_id}", "amount": {amount}}}' + body[close:]
    return body.replace("\n", nl)


def main():
    source = Path(sys.argv[1])
    out = Path(sys.argv[sys.argv.index("--out") + 1]) if "--out" in sys.argv else source
    raw = source.read_bytes().decode("utf-8")
    if "speed_any_5" in raw:
        raise SystemExit("speed-ups are already in this catalog")
    text = splice(raw) if "\n    }," in raw.replace("\r\n", "\n") else json.dumps(apply(json.loads(raw)), ensure_ascii=False)
    json.loads(text)
    out.write_bytes(text.encode("utf-8"))
    if out.resolve() == (ROOT / "shared/catalog/catalog.json").resolve():
        (ROOT / "Assets/_Hadal/Resources/catalog.json").write_bytes(text.encode("utf-8"))
    print(out, "speed-ups added")


if __name__ == "__main__":
    main()
