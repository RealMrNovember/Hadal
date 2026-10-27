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

    # 5. Harita yatakları, toplama ve Derinlik Haritası seferleri (09 §4, 08 §5, 15 §3.2–3.4).
    harita_yataklari(v, katalog["world"])
    katalog["gather"] = toplama(v)
    katalog["expedition"] = sefer(v)

    katalog["version"] = max(SURUM, int(katalog.get("version", 0)))
    return katalog, sorted(atlanan)


KISALTMA = {"Ti": "Titanium", "SA": "Alloy", "GD": "Food", "EN": "Energy", "BK": "Biomass", "HD": "Hadalite"}
VERIM_SUTUN = {"enkaz_alani": "enkaz", "kobalt_kabugu": "kabuk", "nodul_sahasi": "nodul", "masif_sulfur_bacasi": "baca",
               "metan_hidrat": "hidrat", "balina_lesi": "les", "hadalit_damari": "damar"}
YOGUNLUK_ADI = {"Enkaz": "enkaz_alani", "Kabuk": "kobalt_kabugu", "Nodül": "nodul_sahasi", "Baca": "masif_sulfur_bacasi",
                "Hidrat": "metan_hidrat", "Leş": "balina_lesi", "Hadalit Damarı": "hadalit_damari"}


def sayi(metin):
    return float(metin.replace(".", "").replace(",", "."))


def aralik(metin):
    a, _, b = metin.replace("–", "-").partition("-")
    return int(a), int(b or a)


def verim_hucresi(metin):
    """'11.200 Ti + 0,5 SA' → [{resource, perHour}]; '—' → []."""
    if not metin or metin.strip() == "—":
        return []
    out = []
    for parca in metin.split("+"):
        miktar, kisa = parca.split()
        out.append({"resource": KISALTMA[kisa], "perHour": sayi(miktar)})
    return out


def harita_yataklari(v, dunya):
    h = v["harita"]
    dunya["belts"] = [{"id": k["id"], "inner": k["d_ic"], "outer": k["d_dis"],
                       "nodeMin": aralik(k["dugum"])[0], "nodeMax": aralik(k["dugum"])[1],
                       "bases": k["us"].startswith("Evet")} for k in h["kusaklar"]]
    dunya["nodeLevels"] = [{"level": s["seviye"], "capacity": s["kapasite"], "baseRate": s["taban"],
                            "bonus": sayi(s["bonus"].lstrip("×"))} for s in h["dugum_seviyeleri"]]
    turler = []
    for t in h["dugum_turleri"]:
        lo, hi = aralik(t["seviye"])
        verim = [{"level": r["seviye"], "rates": verim_hucresi(r[VERIM_SUTUN[t["id"]]])} for r in h["verim"]]
        turler.append({"id": t["id"], "minLevel": lo, "maxLevel": hi,
                       "capacityFactor": 1.5 if t["id"] == "balina_lesi" else 1.0,
                       "lifeHours": 72 if t["id"] == "balina_lesi" else 0,
                       "yields": [y for y in verim if y["rates"]]})
    dunya["nodeTypes"] = turler
    yogunluk = []
    for satir, kusak in zip(h["yogunluk"], h["kusaklar"]):
        agirlik = []
        for parca in satir["dagilim"].split("·"):
            parca = parca.strip()
            if parca.startswith("Hadalit Damarı"):
                agirlik.append({"type": "hadalit_damari", "percent": 50})
                agirlik.append({"type": "masif_sulfur_bacasi", "percent": 50})
                break
            ad, _, yuzde = parca.rpartition("%")
            ad = ad.replace("(sığ)", "").strip()
            agirlik.append({"type": YOGUNLUK_ADI[ad], "percent": int(yuzde)})
        yogunluk.append({"belt": kusak["id"], "per100": satir["yogunluk"], "weights": agirlik})
    dunya["density"] = yogunluk
    dunya["loadWeights"] = [{"resource": {"Titanyum": "Titanium", "Biyokütle": "Biomass", "Gıda": "Food", "Enerji": "Energy",
                                          "Oksijen": "Oxygen", "Süperalaşım": "Alloy", "Hadalit": "Hadalite"}[y["kaynak"]],
                             "weight": y["agirlik"]} for y in h["yuk_agirliklari"]]
    dunya["respawnMinutes"] = 30
    dunya["hadaliteDailyCap"] = h["hadalit_damari"]["gunluk_sinir"]
    dunya["maxVeins"] = h["hadalit_damari"]["en_fazla"]
    dunya["marchSecondsPerTile"] = 60


def toplama(v):
    """Yürüyüş kuyrukları ve taşıma yükü. Askerler gelene kadar yük reaktörden okunur: tipik yatağın
    etkin hızı × taşıma saati (R10'da 2,5 sa, R20'den sonra 12 sa; 09 §4.2)."""
    h = v["harita"]
    kuyruk = [r["reaktor"] for r in v["denizaltilar"]["hangarlar"]]
    yuk = []
    for R in range(0, 51):
        seviye = next((s for s in h["dugum_seviyeleri"] if aralik(s["tipik"])[0] <= max(1, R) <= aralik(s["tipik"])[1]),
                      h["dugum_seviyeleri"][-1])
        saat = 2.5 if R <= 10 else 12.0 if R >= 20 else 2.5 + (R - 10) * 0.95
        yuk.append(int(egri.anlamli(seviye["etkin"] * saat)))
    return {"queueReactors": kuyruk, "loadByReactor": yuk, "alloyReactor": 30}


def sefer(v):
    k = v["denizaltilar"]["kampanya"]
    o = v["oduller"]["birim"]
    oduller = {r["bolum"]: r for r in k["oduller"]}
    bolumler = []
    for b in k["bolumler"]:
        r = oduller[b["no"]]
        nadirlik, _, adet = r["boss_parca"].partition("×")
        bolumler.append({"chapter": b["no"], "id": b["id"], "reactor": aralik(b["reaktor"])[0], "oxygen": b["dalis_o2"],
                         "powerFirst": b["onerilen_ilk"], "powerLast": b["onerilen_son"], "powerBoss": b["onerilen_boss"],
                         "bossHadalite": r["boss_hd"] or 0, "bossAlloy": r["boss_sa"] or 0,
                         "bossCardRarity": {"Nadir": "rare", "Destansı": "epic", "Efsanevi": "legendary"}[nadirlik.strip()],
                         "bossCards": int(adet), "diveAlloy": max(0, b["no"] - 6) if r["dalis_sa"] else 0})
    return {"stages": 10, "dailyRewarded": k["odullu_dalis"], "refundPercent": 80,
            "gsPerDayEarly": o["sefer_gs_gun_erken"], "gsPerDayLate": o["sefer_gs_gun_gec"], "lateFromReactor": 20,
            "chapters": bolumler}


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
