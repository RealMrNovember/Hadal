"""Tasarım verisini (docs/rehber/veri/*.json) ve catalog v8'i yükler."""
import json

from .yollar import KATALOG, VERI

ZORUNLU = ["kaynaklar", "egriler", "ilerleme", "f2p_profilleri", "binalar"]


class VeriHatasi(Exception):
    pass


def _kompakt(d, girinti=0):
    """Okunur JSON: kısa diziler ve küçük nesneler tek satırda, gerisi 2 boşluk girintili."""
    tek = json.dumps(d, ensure_ascii=False)
    if len(tek) + girinti <= 110 or (isinstance(d, list) and all(not isinstance(x, (dict, list)) for x in d)):
        return tek
    ic = " " * (girinti + 2)
    if isinstance(d, dict):
        parca = [f'{ic}{json.dumps(k, ensure_ascii=False)}: {_kompakt(x, girinti + 2)}' for k, x in d.items()]
        return "{\n" + ",\n".join(parca) + "\n" + " " * girinti + "}"
    if isinstance(d, list):
        parca = [ic + _kompakt(x, girinti + 2) for x in d]
        return "[\n" + ",\n".join(parca) + "\n" + " " * girinti + "]"
    return tek


def yaz(ad, d):
    """Veri dosyasını kompakt biçimde yazar (kalibrasyon araçları için)."""
    (VERI / f"{ad}.json").write_text(_kompakt(d) + "\n", encoding="utf-8")


def yukle():
    v = {}
    for yol in sorted(VERI.glob("*.json")):
        try:
            v[yol.stem] = json.loads(yol.read_text(encoding="utf-8"))
        except json.JSONDecodeError as h:
            raise VeriHatasi(f"{yol.name}: geçersiz JSON ({h})") from h
    eksik = [ad for ad in ZORUNLU if ad not in v]
    if eksik:
        raise VeriHatasi("Eksik veri dosyası: " + ", ".join(eksik))
    v["_katalog"] = json.loads(KATALOG.read_text(encoding="utf-8"))
    return v
