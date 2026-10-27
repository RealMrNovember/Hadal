"""Kılavuz verisinden oyun kataloğunu üretir (Paket A, catalog v9).

    python tools/rehber/katalog.py            # shared/catalog/catalog.json'u günceller (+ Assets kopyası)
    python tools/rehber/katalog.py --kontrol  # yalnızca farkı özetler, hiçbir şey yazmaz

Kodda var olan her bina için kılavuzun seviye tablosu (maliyet, süre, güç, saatlik üretim/tüketim,
gereken reaktör) ve reaktörle açılan örnek sayısı yazılır; reaktör önkoşul zinciri, reaktörle büyüyen
tavanlar, Süperalaşım ve Gelir Saati tablosu da buradan gelir. Eski formül alanları
(buildCost, buildSeconds, production…) geriye uyum için yerinde kalır; oyun tablo varsa tabloyu okur.
Çıktı deterministiktir: ikinci çalıştırma dosyayı değiştirmez.
"""
import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from rehber import egri, hesap, veri  # noqa: E402
from rehber.yollar import KATALOG  # noqa: E402

KOK = Path(__file__).resolve().parents[2]
ASSETS_KATALOG = KOK / "Assets" / "_Hadal" / "Resources" / "catalog.json"
SURUM = 9


def tutarlar(d):
    return [{"resource": k, "amount": int(v)} for k, v in sorted(d.items(), key=lambda kv: hesap.KAYNAK_SIRASI.index(kv[0]))]


def saatlik(d):
    # Saatlik üretim; kesirli değerler binde bir hassasiyetle (oyun binde birle hesaplar).
    return [{"resource": k, "perHour": float(v)} for k, v in sorted(d.items())]


def uret(v):
    model = hesap.Model(v)
    katalog = json.loads(KATALOG.read_text(encoding="utf-8"))
    kodda = {b["id"] for b in katalog["buildings"]}

    # 1. Bina seviye tabloları ve örnek açılışları.
    for kb in katalog["buildings"]:
        b = model.binalar.get(kb["id"])
        if not b:
            continue
        tablo = model.bina_tablosu(kb["id"])
        kb["levels"] = [{
            "reactor": r["reaktor"],
            "cost": tutarlar(r["bedel"]),
            "seconds": int(r["sure"]),
            "power": int(r["guc"]),
            "production": saatlik(r["uretim"]),
            "consumption": saatlik(r["tuketim"]),
        } for r in tablo["satirlar"]]
        # Eski alanlar (maxCount, requiredReactorLevel) kılavuzun "bugün" karşılaştırması için kalır;
        # oyun countUnlocks varsa onu okur.
        kb["countUnlocks"] = list(b["adet"])

    # 2. Reaktör önkoşulları: yalnızca kodda olan binalar (tasarımdaki binalar zinciri kilitlemesin).
    gereksinim = []
    atlanan = set()
    for L in range(2, model.maks(model.binalar["core_reactor"]) + 1):
        req = []
        for bid, sev in model.reaktor_on_kosul(L):
            if bid in kodda:
                req.append({"building": bid, "level": int(sev)})
            else:
                atlanan.add(bid)
        if req:
            gereksinim.append({"level": L, "requires": req})
    katalog["reactorRequirements"] = gereksinim

    # 3. Kaynaklar: reaktörle büyüyen tavan, Süperalaşım.
    mevcut = {r["type"]: r for r in katalog["resources"]}
    for kid, k in model.kaynaklar.items():
        if kid not in mevcut:
            mevcut[kid] = {"type": kid, "displayName": kid, "description": k.get("aciklama", ""),
                           "startingAmount": 0, "maxCapacity": 0, "critical": bool(k.get("kritik"))}
            katalog["resources"].append(mevcut[kid])
        tavan = [model.tavan(kid, R) for R in range(0, 51)]
        if any(t is not None for t in tavan):
            ilk = next(t for t in tavan if t is not None)
            mevcut[kid]["capacityByReactor"] = [int(t if t is not None else ilk) for t in tavan]

    # 4. Gelir Saati (görev/sandık kapsüllerinin damgası): reaktör → F2P'nin saatlik Titanyum geliri.
    gelir = v["f2p_profilleri"]["gelir"]
    katalog["incomeHour"] = [int(egri.anlamli(egri.deger(gelir["titanyum_saat"], max(1, R)))) for R in range(0, 51)]
    katalog["incomeBiomassRatio"] = gelir.get("biyokutle_orani", 0.4)

    katalog["version"] = max(SURUM, int(katalog.get("version", 0)))
    return katalog, sorted(atlanan)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--kontrol", action="store_true")
    a = p.parse_args()
    v = veri.yukle()
    katalog, atlanan = uret(v)
    metin = veri._kompakt(katalog) + "\n"
    eski = KATALOG.read_text(encoding="utf-8")
    print(f"catalog v{katalog['version']}: {len(katalog['buildings'])} bina, "
          f"{len(katalog['reactorRequirements'])} reaktör önkoşul seviyesi")
    if atlanan:
        print("Önkoşul zincirinde atlanan (kodda olmayan) binalar: " + ", ".join(atlanan))
    if a.kontrol:
        print("değişecek" if metin != eski else "aynı")
        return
    if metin != eski:
        KATALOG.write_text(metin, encoding="utf-8")
        ASSETS_KATALOG.write_text(metin, encoding="utf-8")
        print("yazıldı")
    else:
        print("değişiklik yok")


if __name__ == "__main__":
    main()
