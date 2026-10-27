"""HADAL Oyun Kılavuzu üreticisi.

    python tools/rehber/uret.py            # kontrol + tüm üretilen sayfalar ve işaretler
    python tools/rehber/uret.py --kontrol  # yalnızca kontroller (hiçbir şey yazılmaz)
    python tools/rehber/uret.py --sim      # tempo simülasyonu özeti
    python tools/rehber/uret.py --html     # web veritabanını da üret

Tek veri kaynağı docs/rehber/veri/*.json; çıktı deterministiktir, ikinci çalıştırma hiçbir dosyayı değiştirmez.
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from rehber import bicim, hesap, kontrol, md, sayfalar, sim, veri  # noqa: E402
from rehber.yollar import REHBER  # noqa: E402

URETILEN_KLASORLER = ["binalar", "kahramanlar", "arastirma", "uretilen"]


def hesap_agaci(model, sim_sonuc):
    """Satır içi değerlerin ('<!--v:hesap.…-->') okuduğu ağaç."""
    bina = {}
    for b in model.bina_listesi:
        t = model.bina_tablosu(b["id"])
        d = {r["seviye"]: r for r in t["satirlar"]}
        d.update(toplam_bedel=t["toplam_bedel"], toplam_sure=t["toplam_sure"], toplam_guc=t["toplam_guc"])
        bina[b["id"]] = d
    agac = {
        "bina": bina,
        "sim": {pid: {"gun_50": x["gun_50"], "seviye": {r["seviye"]: r["gun"] for r in x["satirlar"]},
                      "toplam": x["toplam"], "bos_insaatci_gun": x["kalan_havuz_gun"]} for pid, x in sim_sonuc.items()},
        "tavan": {k: {R: model.tavan(k, R) for R in range(1, 50)} for k in model.kaynaklar},
        "slot": {R: model.acik_slot(R) for R in range(1, 51)},
        "enerji": {R: model.enerji_dengesi(R)["net"] for R in range(1, 51)},
        "sayilar": {
            "bina_turu": len(model.bina_listesi),
            "bina_turu_kodda": sum(1 for b in model.bina_listesi if b.get("durum") == "kodda"),
            "bina_turu_yeni": sum(1 for b in model.bina_listesi if b.get("durum") != "kodda"),
            "bina_ornegi": model.ornek_sayisi(50),
            "yuva": model.acik_slot(50),
            "tum_binalar_ham_gun": round(sum(
                (len(b["adet"]) + (len(b["arastirma_ek"]["adet"]) if b.get("arastirma_ek") else 0))
                * model.bina_tablosu(b["id"])["toplam_sure"] for b in model.bina_listesi) / 86400),
            "reaktor_ham_gun": round(model.bina_tablosu("core_reactor")["toplam_sure"] / 86400),
            "v8_reaktor_ham_saat": round(sum(x["sure"] for x in model.bina_tablosu("core_reactor")["v8"]["satirlar"]) / 3600, 1),
        },
    }
    a = model.arastirma()
    if a:
        agac["sayilar"]["teknoloji"] = len(a["teknolojiler"])
        agac["sayilar"]["arastirma_seviyesi"] = sum(t["maks"] for t in a["teknolojiler"])
        agac["sayilar"]["arastirma_dali"] = len(a["dallar"])
        agac["arastirma"] = {t["id"]: {r["seviye"]: r for r in model.teknoloji_tablosu(t)["satirlar"]} for t in a["teknolojiler"]}
    k = model.kahramanlar()
    if k:
        agac["sayilar"]["kahraman"] = len(k["kahramanlar"])
        agac["kahraman"] = {h["id"]: {"guc_50": model.kahraman_stat(h, 50, 5)["guc"],
                                      "stat_50": model.kahraman_stat(h, 50, 5)} for h in k["kahramanlar"]}
    return agac


def blok_ureticisi(model, sim_sonuc, deger_kok):
    sabit = {
        "bina-ozet": lambda: sayfalar.bina_ozet(model),
        "reaktor-capalari": lambda: sayfalar.reaktor_capalari(model, sim_sonuc),
        "reaktor-on-kosul": lambda: sayfalar.reaktor_on_kosul_tablosu(model),
        "halka-acilislari": lambda: sayfalar.halka_tablosu(model),
        "slot-tablosu": lambda: sayfalar.slot_tablosu(model),
        "tavanlar": lambda: sayfalar.tavan_tablosu(model),
        "enerji-dengesi": lambda: sayfalar.enerji_tablosu(model),
        "guc-tablosu": lambda: sayfalar.guc_tablosu(model),
        "egriler": lambda: sayfalar.egri_tablosu(model),
        "kaynak-tablosu": lambda: sayfalar.kaynak_tablosu(model),
        "v8-karsilastirma": lambda: sayfalar.v8_karsilastirma(model),
        "sim-ozet": lambda: sayfalar.sim_ozet(model, sim_sonuc),
        "profil-ozet": lambda: sayfalar.profil_ozet(model),
        "gelir-tablosu": lambda: sayfalar.gelir_tablosu(model),
        "insaatci-butcesi": lambda: sayfalar.insaatci_butcesi(model),
        "kilometre-taslari": lambda: sayfalar.kilometre_tablosu(model, sim_sonuc),
        "arastirma-ozet": lambda: sayfalar.arastirma_ozet(model),
        "kahraman-ozet": lambda: sayfalar.kahraman_ozet(model),
        "kahraman-egitim": lambda: sayfalar.kahraman_egitim_tablosu(model),
        "evrim-ozet": lambda: sayfalar.evrim_ozet(model),
        "sozluk-binalar": lambda: sayfalar.sozluk_binalar(model),
        "sozluk-kahramanlar": lambda: sayfalar.sozluk_kahramanlar(model),
        "sozluk-arastirma": lambda: sayfalar.sozluk_arastirma(model),
    }

    def uret(ad):
        if ad in sabit:
            return sabit[ad]()
        if ad.startswith("tablo:"):
            return md.genel_tablo(deger_kok["veri"], ad[len("tablo:"):])
        raise md.IsaretHatasi(f"bilinmeyen blok '{ad}'")

    return uret


def _yaz(yol, metin, degisen):
    yol.parent.mkdir(parents=True, exist_ok=True)
    eski = yol.read_text(encoding="utf-8") if yol.exists() else None
    if eski != metin:
        yol.write_text(metin, encoding="utf-8", newline="\n")
        degisen.append(yol.relative_to(REHBER).as_posix())


def sayfalari_uret(model, sim_sonuc, degisen, turler=None):
    """turler: None = hepsi; yoksa {'bina', 'arastirma', 'kahraman', 'sim'} alt kümesi."""
    hepsi = turler is None
    turler = turler or set()
    hedef = {}
    if hepsi or "bina" in turler:
        for b in model.bina_listesi:
            hedef[REHBER / "binalar" / f"{b['id']}.md"] = sayfalar.bina_sayfasi(model, b["id"], sim_sonuc)
    a = model.arastirma()
    if a and (hepsi or "arastirma" in turler):
        for d in a["dallar"]:
            hedef[REHBER / "arastirma" / f"{d['id']}.md"] = sayfalar.arastirma_dal_sayfasi(model, d)
    k = model.kahramanlar()
    if k and (hepsi or "kahraman" in turler):
        for h in k["kahramanlar"]:
            hedef[REHBER / "kahramanlar" / f"{h['id']}.md"] = sayfalar.kahraman_sayfasi(model, h)
    if hepsi or "sim" in turler:
        hedef[REHBER / "uretilen" / "simulasyon.md"] = sayfalar.sim_sayfasi(model, sim_sonuc)
    for yol, metin in hedef.items():
        _yaz(yol, metin.rstrip() + "\n", degisen)
    if not hepsi:
        return
    for klasor in URETILEN_KLASORLER:
        for yol in (REHBER / klasor).glob("*.md"):
            if yol not in hedef and yol.read_text(encoding="utf-8").startswith("<!-- Bu sayfa tools/rehber/uret.py"):
                yol.unlink()
                degisen.append(yol.relative_to(REHBER).as_posix() + " (silindi)")


def isaretleri_isle(model, sim_sonuc, degisen, hatalar, yalniz=None):
    agac = {"veri": {k: x for k, x in model.v.items() if not k.startswith("_")}, "hesap": hesap_agaci(model, sim_sonuc)}
    uret = blok_ureticisi(model, sim_sonuc, agac)
    for yol in sorted(REHBER.rglob("*.md")):
        if yol.parent.parent == REHBER and yol.parent.name in URETILEN_KLASORLER + ["veri"]:
            continue
        if yalniz is not None and yol.relative_to(REHBER).as_posix() not in yalniz:
            continue
        metin = yol.read_text(encoding="utf-8")
        if "URET:" not in metin and "<!--v:" not in metin:
            continue
        yeni, h = md.isle(metin, uret, agac)
        hatalar += [f"{yol.relative_to(REHBER).as_posix()}: {x}" for x in h]
        _yaz(yol, yeni, degisen)


def ozet_yazdir(sim_sonuc):
    for pid, x in sim_sonuc.items():
        f = {r["seviye"]: r["gun"] for r in x["satirlar"]}
        print(f"  {x['ad']:<22} Reaktör 50: {x['gun_50']:6.1f}. gün   "
              + "  ".join(f"R{L}:{f[L]:.1f}" for L in (10, 20, 30, 40, 45)))


def main(argv=None):
    ap = argparse.ArgumentParser(description="HADAL Oyun Kılavuzu üreticisi")
    ap.add_argument("--kontrol", action="store_true", help="yalnızca kontroller")
    ap.add_argument("--sim", action="store_true", help="simülasyon özetini yazdır")
    ap.add_argument("--html", action="store_true", help="web veritabanını da üret")
    ap.add_argument("--artifact", metavar="YOL", help="--html ile: yayın için iskeletsiz gövdeyi de bu yola yaz")
    ap.add_argument("--yalniz", nargs="+", metavar="BOLUM.md",
                    help="yalnızca bu bölüm dosyalarının işaretlerini işle (docs/rehber'e göre yol); paralel yazarlar için")
    ap.add_argument("--sayfa", nargs="*", default=[], choices=["bina", "arastirma", "kahraman", "sim"],
                    help="--yalniz ile birlikte: bu türdeki üretilen sayfaları da yaz")
    arg = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass

    model = hesap.Model(veri.yukle())
    sim_sonuc = sim.hepsi(model)
    if arg.sim:
        ozet_yazdir(sim_sonuc)
        return 0

    degisen, isaret_hatalari = [], []
    yalniz = set(arg.yalniz) if arg.yalniz else None
    if not arg.kontrol:
        sayfalari_uret(model, sim_sonuc, degisen, set(arg.sayfa) if yalniz is not None else None)
        isaretleri_isle(model, sim_sonuc, degisen, isaret_hatalari, yalniz)
        if arg.html and yalniz is None:
            from rehber import web
            web.uret(model, sim_sonuc, degisen, arg.artifact)

    hatalar, uyarilar = kontrol.calistir(model, sim_sonuc)
    hatalar = isaret_hatalari + hatalar
    if yalniz is not None:
        # Paralel yazımda başkalarının henüz yazılmamış bölümlerine giden kırık linkler gürültüdür.
        hatalar = [x for x in hatalar if "kırık link" not in x or x.split(":", 1)[0] in yalniz]
    for d in degisen:
        print(f"  yazıldı: {d}")
    print(f"Simülasyon:")
    ozet_yazdir(sim_sonuc)
    for x in uyarilar:
        print(f"  uyarı: {x}")
    for x in hatalar:
        print(f"  HATA: {x}")
    print(f"{len(hatalar)} hata, {len(uyarilar)} uyarı, {len(degisen)} dosya değişti.")
    return 1 if hatalar else 0


if __name__ == "__main__":
    sys.exit(main())
