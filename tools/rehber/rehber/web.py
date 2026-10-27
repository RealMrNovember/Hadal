"""Etkileşimli web veritabanı: tasarım verisini tek dosyalık HTML'e gömer.

Çıktılar:
- docs/rehber/web/hadal-veritabani.html — tam HTML belgesi (tarayıcıda doğrudan açılır).
- --artifact YOL verilirse aynı sayfanın iskeletsiz gövdesi (yayın platformu iskeleti kendisi ekler).
"""
import json

from . import bicim, sayfalar
from .hesap import KAYNAK_SIRASI
from .yollar import REHBER, SABLON

DOSYA_ADLARI = {
    "birlikler": "Birlikler", "denizaltilar": "Denizaltılar", "vip": "VIP", "syndicate": "The Syndicate",
    "hizlandiricilar": "Hızlandırıcılar", "oduller": "Ödüller", "etkinlikler": "Etkinlikler", "harita": "Dünya haritası",
    "ittifak": "İttifak", "kronik": "Derinlik Kroniği", "kokenler": "Kökenler", "techizat": "Teçhizat",
    "evrim": "Bina evrimi", "son_oyun": "Son oyun", "arastirma": "Araştırma", "kahramanlar": "Kahramanlar",
}
TEMEL = {"kaynaklar", "egriler", "ilerleme", "f2p_profilleri", "binalar", "yasakli_isimler", "bilim"}


def _insan(anahtar):
    s = anahtar.replace("_", " ").strip()
    return s[:1].upper() + s[1:]


def _tablolari_bul(ad, dugum, yol, out):
    if isinstance(dugum, dict):
        if isinstance(dugum.get("satirlar"), list) and dugum.get("sutunlar"):
            out.append({"baslik": f"{DOSYA_ADLARI.get(ad, _insan(ad))} · {_insan(yol.split('.')[-1])}",
                        "kaynak": f"{ad}.json → {yol}", "sutunlar": dugum["sutunlar"], "satirlar": dugum["satirlar"]})
            return
        for k, x in dugum.items():
            if k.endswith("_sutunlar") or k.startswith("_"):
                continue
            if isinstance(x, list) and dugum.get(k + "_sutunlar") and x and isinstance(x[0], dict):
                out.append({"baslik": dugum.get(k + "_baslik") or f"{DOSYA_ADLARI.get(ad, _insan(ad))} · {_insan(k)}",
                            "kaynak": f"{ad}.json → {yol + '.' if yol else ''}{k}", "sutunlar": dugum[k + "_sutunlar"], "satirlar": x})
            else:
                _tablolari_bul(ad, x, f"{yol}.{k}" if yol else k, out)


def _bilim_tablolari(b):
    out = []
    if not b:
        return out
    if b.get("cukurlar"):
        out.append({"baslik": "Bilim · Hadal çukurları (sunucular)", "kaynak": "bilim.json → cukurlar",
                    "sutunlar": [["ad", "Çukur"], ["en_derin_nokta", "En derin nokta"], ["derinlik_m", "Derinlik (m)"], ["okyanus", "Okyanus"]],
                    "satirlar": b["cukurlar"]})
    if b.get("turler"):
        out.append({"baslik": "Bilim · Canlılar", "kaynak": "bilim.json → turler",
                    "sutunlar": [["ad", "Tür"], ["bilimsel_ad", "Bilimsel ad"], ["min_m", "En sığ (m)"], ["max_m", "En derin (m)"],
                                 ["boy_cm", "Boy (cm)"], ["ozellik", "Özellik"]],
                    "satirlar": b["turler"]})
    if b.get("mineraller"):
        out.append({"baslik": "Bilim · Mineraller ve kaynaklar", "kaynak": "bilim.json → mineraller",
                    "sutunlar": [["ad", "Mineral"], ["derinlik_m", "Derinlik (m)"], ["oyun_baglantisi", "Oyundaki karşılığı"], ["aciklama", "Açıklama"]],
                    "satirlar": b["mineraller"]})
    return out


def _vip_hizlari(v):
    """VIP seviyelerinden inşa hızını bulmaya çalışır (hesaplayıcı seçimi için)."""
    seviyeler = v.get("seviyeler") if isinstance(v, dict) else None
    if not isinstance(seviyeler, list):
        return []
    out = []
    for s in seviyeler:
        if not isinstance(s, dict):
            continue
        hiz = None
        for k, x in s.items():
            if isinstance(x, (int, float)) and ("insa" in k or "insaat" in k):
                hiz = x
                break
        sev = s.get("seviye", s.get("vip", s.get("id")))
        if hiz is not None and sev is not None:
            out.append({"ad": f"VIP {sev}", "hiz": hiz})
    return out


def paket(model, sim_sonuc):
    v = model.v
    evrim = v.get("evrim") or {}
    katmanlar = [{"ad": k.get("ad"), "seviyeler": k.get("seviyeler", "")} for k in evrim.get("katmanlar", [])]
    binalar = []
    for b in model.bina_listesi:
        t = model.bina_tablosu(b["id"])
        ev = (evrim.get("binalar") or {}).get(b["id"]) or {}
        kilitler = []
        onkosul = {}
        if b["id"] == "core_reactor":
            for L in range(2, model.maks(b) + 1):
                ac = model.reaktor_acilislari(L)
                parca = [model.binalar[x]["ad"] + (f" ({k}.)" if len(model.binalar[x]["adet"]) > 1 else "") for x, k in ac["binalar"]]
                parca += [f"Halka {h}" for h in ac["halkalar"]]
                parca += [k["metin"] for k in b.get("kilitler", []) if k["seviye"] == L]
                if parca:
                    kilitler.append([L, ", ".join(parca)])
                onkosul[L] = ", ".join(f"{model.binalar[x]['ad']} {s}" for x, s in model.reaktor_on_kosul(L))
        else:
            kilitler = [[k["seviye"], k["metin"]] for k in b.get("kilitler", [])]
        ek = b.get("arastirma_ek")
        binalar.append({
            "id": b["id"], "ad": b["ad"], "kategori": b["kategori"], "durum": model.bina_durumu(b["id"]),
            "durumKodu": b.get("durum"), "aciklama": b["aciklama"], "islev": b["islev"], "maks": model.maks(b),
            "acilis": ", ".join(f"R{r}" for r in b["adet"] if r) or "Başlangıçta hazır",
            "adetMetni": str(len(b["adet"])) + (f" (+{len(ek['adet'])} araştırmayla)" if ek else ""),
            "toplamSure": t["toplam_sure"], "toplamBedel": {k: t["toplam_bedel"][k] for k in KAYNAK_SIRASI if k in t["toplam_bedel"]},
            "evrim": [{"ad": a, "katman": k, "seviyeler": s, "gorunum": g} for s, k, a, g in sayfalar.evrim_satirlari(model, ev)] if ev else [],
            "kilitler": kilitler, "onkosul": onkosul,
            "satirlar": [{"seviye": r["seviye"], "reaktor": r["reaktor"], "bedel": r["bedel"], "sure": r["sure"], "guc": r["guc"],
                          "uretim": r["uretim"], "hazir": r["hazir"]} for r in t["satirlar"]],
            "v8": [{"seviye": r["seviye"], "bedel": r["bedel"], "sure": r["sure"], "uretim": r["uretim"]} for r in t["v8"]["satirlar"]] if t["v8"] else None,
        })

    kahraman = None
    k = model.kahramanlar()
    if k:
        nad = {x["id"]: x for x in k["nadirlikler"]}
        sinif = {x["id"]: x for x in k["siniflar"]}
        fr = {x["id"]: x["ad"] for x in k["fraksiyonlar"]}
        uz = {x["id"]: x["ad"] for x in k.get("uzmanliklar", [])}
        liste = []
        for h in k["kahramanlar"]:
            mur = h.get("murettebat")
            mur_out = None
            if mur:
                mur_out = {"ad": sayfalar.etki_adi(model, mur["etki"], mur.get("ad")),
                           "degerler": [mur["taban"] + int((y * 10 - 1) * mur["seviye_onda"] / 10) + (y - 1) * mur["yildiz"] for y in range(1, 6)]}
            statlar = []
            for L in (1, 10, 20, 30, 40, 50):
                satir = [L]
                for y in range(1, 6):
                    if L > y * 10:
                        satir.append(None)
                    else:
                        s = model.kahraman_stat(h, L, y)
                        satir.append([s["atk"], s["def"], s["hp"]])
                statlar.append(satir)
            yet = []
            for y in h.get("yetenekler", []):
                d = y.get("degerler", [])
                yet.append({"ad": y["ad"], "tur": y.get("tur_ad", y.get("tur", "")), "yildiz": y.get("yildiz", 1),
                            "aciklama": y.get("aciklama", "").replace("{0}", " / ".join(bicim.sayi(x) for x in d) if d else "")})
            liste.append({"id": h["id"], "ad": h["ad"], "unvan": h.get("unvan", ""), "nadirlik": h["nadirlik"],
                          "nadirlikAd": nad[h["nadirlik"]]["ad"], "sinifAd": sinif[h["sinif"]]["ad"], "fraksiyonAd": fr.get(h["fraksiyon"], h["fraksiyon"]),
                          "nesil": h.get("nesil", 1), "kodda": h.get("durum") == "kodda", "biyografi": h.get("biyografi", ""),
                          "murettebat": mur_out, "yetenekler": yet, "statlar": statlar,
                          "agaclar": [uz.get(a, a) for a in h.get("agaclar", [])], "guc50": model.kahraman_stat(h, 50, 5)["guc"]})
        kahraman = {"nadirlikler": [{"id": x["id"], "ad": x["ad"]} for x in k["nadirlikler"]], "liste": liste}

    arastirma = None
    a = model.arastirma()
    if a:
        adlar = {t["id"]: t["ad"] for t in a["teknolojiler"]}
        tek = []
        for t in a["teknolojiler"]:
            tt = model.teknoloji_tablosu(t)
            tek.append({"id": t["id"], "dal": t["dal"], "ad": t["ad"], "maks": t["maks"], "kodda": t.get("durum") == "kodda",
                        "aciklama": t.get("aciklama", ""),
                        "gerekli": ", ".join(f"{adlar.get(g['id'], g['id'])} {g['seviye']}" for g in t.get("gerekli", [])),
                        "satirlar": [{"seviye": r["seviye"], "lab": r["lab"], "bedel": r["bedel"], "sure": r["sure"],
                                      "etki": sayfalar._etki_metni(t, r["etki"])} for r in tt["satirlar"]]})
        arastirma = {"dallar": [{"id": d["id"], "ad": d["ad"], "aciklama": d.get("aciklama", ""), "kuyruk": d.get("kuyruk_ad", ""),
                                 "acilis": d.get("acilis", "")} for d in a["dallar"]], "teknolojiler": tek}

    tablolar = []
    for ad in sorted(v):
        if ad.startswith("_") or ad in TEMEL:
            continue
        _tablolari_bul(ad, v[ad], "", tablolar)
    tablolar += _bilim_tablolari(v.get("bilim"))

    f2p = sim_sonuc["f2p"]["gun_50"]
    gosterge = [["Bina", f"{len(model.bina_listesi)} tür"]]
    if kahraman:
        gosterge.append(["Kahraman", str(len(kahraman["liste"]))])
    if arastirma:
        gosterge.append(["Araştırma", f"{len(arastirma['teknolojiler'])} teknoloji"])
    gosterge.append(["F2P Reaktör 50", bicim.gun(f2p)])
    bilim = v.get("bilim") or {}
    for c in bilim.get("cukurlar", []):
        if c.get("id") == "mariana" and c.get("derinlik_m"):
            gosterge.append([c.get("en_derin_nokta", "Challenger Deep"), f"{bicim.sayi(c['derinlik_m'])} m"])
    return {
        "meta": {"altbaslik": "Binalar, kahramanlar, araştırma ve tempo — HADAL Oyun Kılavuzu verisinden üretildi",
                 "gosterge": gosterge, "surum": v["binalar"].get("surum")},
        "kaynaklar": [{"id": x, "ad": model.kaynak_adi(x), "sembol": model.kaynaklar[x]["sembol"], "renk": model.kaynaklar[x]["renk"]}
                      for x in KAYNAK_SIRASI if x in model.kaynaklar],
        "kategoriler": [{"id": i, "ad": a} for i, a in model.kategoriler.items()],
        "katmanlar": katmanlar, "binalar": binalar, "kahramanlar": kahraman, "arastirma": arastirma,
        "vip": _vip_hizlari(v.get("vip") or {}),
        "sim": [{"id": pid, "ad": x["ad"], "gunler": [round(r["gun"], 3) for r in x["satirlar"]]} for pid, x in sim_sonuc.items()],
        "tablolar": tablolar,
    }


def govde(model, sim_sonuc):
    sablon = (SABLON / "veritabani.html").read_text(encoding="utf-8")
    veri = json.dumps(paket(model, sim_sonuc), ensure_ascii=False, separators=(",", ":")).replace("</", "<\\/")
    return sablon.replace("/*__VERI__*/null", veri)


def tam_belge(icerik):
    """İskeletsiz gövdeyi tam HTML belgesine sarar: <header>'dan önceki kısım <head>'e girer."""
    i = icerik.index("<header")
    return ("<!doctype html>\n<html lang=\"tr\">\n<head>\n<meta charset=\"utf-8\">\n"
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, viewport-fit=cover\">\n"
            + icerik[:i].rstrip() + "\n</head>\n<body>\n" + icerik[i:].rstrip() + "\n</body>\n</html>\n")


def uret(model, sim_sonuc, degisen, artifact_yolu=None):
    icerik = govde(model, sim_sonuc)
    hedef = REHBER / "web" / "hadal-veritabani.html"
    hedef.parent.mkdir(parents=True, exist_ok=True)
    belge = tam_belge(icerik)
    if not hedef.exists() or hedef.read_text(encoding="utf-8") != belge:
        hedef.write_text(belge, encoding="utf-8", newline="\n")
        degisen.append("web/hadal-veritabani.html")
    if artifact_yolu:
        from pathlib import Path
        p = Path(artifact_yolu)
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(icerik, encoding="utf-8", newline="\n")
