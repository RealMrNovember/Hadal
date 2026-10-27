"""Üretilen bloklar ve sayfalar: bina, araştırma, kahraman sayfaları, özet tablolar, simülasyon raporu."""
import re

from . import bicim, egri, md
from .hesap import KAYNAK_SIRASI

UST_NOT = ("<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. "
           "Kaynak: docs/rehber/veri/{kaynak}.json -->")

ETKI_ADLARI = {
    "buildTime": "İnşa süresi", "researchTime": "Araştırma süresi", "builderSlots": "İnşaatçı",
    "researchSlots": "Araştırma kuyruğu", "capacity:All": "Tüm depo kapasitesi",
    "limit:oxygen_plant": "Oksijen Tesisi sınırı", "limit:drone_factory": "Dron Fabrikası sınırı",
    "production:All": "Tüm üretim",
}


def etki_adi(model, etki, yedek=None):
    if yedek:
        return yedek
    if etki in ETKI_ADLARI:
        return ETKI_ADLARI[etki]
    if etki and etki.startswith("production:"):
        return model.kaynak_adi(etki.split(":", 1)[1]) + " üretimi"
    if etki and etki.startswith("capacity:"):
        return model.kaynak_adi(etki.split(":", 1)[1]) + " kapasitesi"
    return etki or "—"


def bina_linki(model, bid, ust="."):
    b = model.binalar.get(bid)
    ad = b["ad"] if b else bid
    return f"[{ad}]({ust}/binalar/{bid}.md)"


def kaynak_sutunlari(satirlar, alan="bedel"):
    var = set()
    for s in satirlar:
        var.update(s[alan].keys())
    return [k for k in KAYNAK_SIRASI if k in var]


def akis_metni(model, akis):
    if not akis:
        return "—"
    return " · ".join(f"{bicim.sayi(x)} {model.kaynaklar[k]['sembol'] if k in model.kaynaklar else k}"
                      for k, x in akis.items())


def bedel_metni(model, bedel):
    if not bedel:
        return "—"
    return " · ".join(f"{bicim.sayi(bedel[k])} {model.kaynak_adi(k)}" for k in KAYNAK_SIRASI if k in bedel)


# --- bina sayfası ----------------------------------------------------------------
def bina_sayfasi(model, bid, sim_sonuc=None):
    b = model.binalar[bid]
    t = model.bina_tablosu(bid)
    s = t["satirlar"]
    durum = model.bina_durumu(bid)
    kat = model.kategoriler.get(b["kategori"], b["kategori"])
    out = [UST_NOT.format(kaynak="binalar"), f"# {b['ad']}", "",
           f"`{bid}` · {kat} · {durum}", "", f"> {b['aciklama']}", "", f"**İşlev.** {b['islev']}", ""]

    adet = len(b["adet"])
    ek = b.get("arastirma_ek")
    adet_metni = str(adet) + (f" (+{len(ek['adet'])} araştırmayla)" if ek else "")
    acilis = ", ".join(f"{i + 1}. örnek Reaktör {r}" for i, r in enumerate(b["adet"]) if r) or "Başlangıçta hazır"
    if ek:
        acilis += "; ek örnekler Reaktör " + ", ".join(str(r) for r in ek["adet"])
    ilk, son = s[0], s[-1]
    ozet = [
        ["Açılış", acilis],
        ["Adet", adet_metni],
        ["Halka", f"{b['halka'][0]}–{b['halka'][1]}" if b["halka"][0] != b["halka"][1] else str(b["halka"][0])],
        ["En yüksek seviye", str(model.maks(b))],
    ]
    if b.get("uretim"):
        ozet.append(["Üretim (saatlik)", f"Sv 1: {akis_metni(model, ilk['uretim'])} → Sv {son['seviye']}: {akis_metni(model, son['uretim'])}"])
    if b.get("tuketim"):
        ozet.append(["Tüketim (saatlik)", f"Sv 1: {akis_metni(model, ilk['tuketim'])} → Sv {son['seviye']}: {akis_metni(model, son['tuketim'])}"])
    ozet += [
        ["Toplam maliyet (1 → en yüksek)", bedel_metni(model, t["toplam_bedel"])],
        ["Toplam ham süre", bicim.sure(t["toplam_sure"])],
        ["Toplam güç", bicim.sayi(t["toplam_guc"])],
    ]
    if b.get("gerekli"):
        ozet.append(["İnşa için gerekli", ", ".join(f"{model.binalar[g['bina']]['ad']} {g['seviye']}" for g in b["gerekli"])])
    out += [md.tablo(["Özellik", "Değer"], ozet), ""]

    evrim = (model.v.get("evrim") or {}).get("binalar", {}).get(bid)
    if evrim:
        out += ["## Görünüm evrimi", "", evrim_tablosu(model, bid, evrim), ""]

    kilit_satirlari = []
    if bid == "core_reactor":
        for L in range(2, model.maks(b) + 1):
            ac = model.reaktor_acilislari(L)
            parca = [f"{model.binalar[x]['ad']}" + (f" ({n}.)" if len(model.binalar[x]["adet"]) > 1 else "")
                     for x, n in ac["binalar"]]
            parca += [f"Halka {h}" for h in ac["halkalar"]]
            parca += [k["metin"] for k in b.get("kilitler", []) if k["seviye"] == L]
            if parca:
                kilit_satirlari.append([L, ", ".join(parca)])
    else:
        kilit_satirlari = [[k["seviye"], k["metin"]] for k in b.get("kilitler", [])]
    if kilit_satirlari:
        baslik = "## Reaktör seviyeleriyle açılanlar" if bid == "core_reactor" else "## Seviye kilitleri"
        out += [baslik, "", md.tablo(["Sv", "Açılan"], kilit_satirlari, ["r", "l"]), ""]

    out += ["## Seviye tablosu", "",
            "Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve "
            "hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). "
            "Güç, o seviyenin eklediği güçtür.", ""]
    kaynaklar = kaynak_sutunlari(s)
    basliklar = ["Sv", "Reaktör"]
    if bid == "core_reactor":
        basliklar.append("Önkoşul")
    basliklar += [model.kaynak_adi(k) for k in kaynaklar] + ["Süre", "Güç"]
    uretim_var = bool(b.get("uretim"))
    if uretim_var:
        basliklar.append("Üretim/sa")
    etki_adlari = [e["ad"] for e in b.get("etkiler", [])]
    basliklar += etki_adlari + ["Kümülatif süre"]
    satirlar = []
    for r in s:
        if r["hazir"]:
            satir = [r["seviye"], "—"] + (["Başlangıç"] if bid == "core_reactor" else [])
            satir += ["—"] * len(kaynaklar) + ["—", r["guc"]]
        else:
            satir = [r["seviye"], r["reaktor"]]
            if bid == "core_reactor":
                satir.append(", ".join(f"{model.binalar[x]['ad']} {sv}" for x, sv in model.reaktor_on_kosul(r["seviye"])) or "—")
            satir += [r["bedel"].get(k, "—") for k in kaynaklar] + [bicim.sure(r["sure"]), r["guc"]]
        if uretim_var:
            satir.append(akis_metni(model, r["uretim"]))
        satir += [e["deger"] for e in r["etkiler"]]
        satir.append(bicim.sure(r["kumulatif_sure"]))
        satirlar.append(satir)
    hiz = ["r", "r"] + (["l"] if bid == "core_reactor" else []) + ["r"] * len(kaynaklar) + ["r", "r"]
    hiz += (["l"] if uretim_var else []) + ["r"] * len(etki_adlari) + ["r"]
    out += [md.tablo(basliklar, satirlar, hiz), ""]

    if t["v8"]:
        out += v8_bolumu(model, bid, t)
    out += ["", "← [03 · Binalar](../03_binalar.md)", ""]
    return "\n".join(out)


def v8_bolumu(model, bid, t):
    v = t["v8"]
    vs = v["satirlar"]
    kaynaklar = kaynak_sutunlari(vs)
    secili = sorted({1, 5, 10, 15, 20, 25, 30, 35, 40, 45, v["maks"]} & set(range(1, v["maks"] + 1)))
    basliklar = ["Sv"] + [f"v8 {model.kaynak_adi(k)}" for k in kaynaklar] + ["v8 süre", "Hedef Titanyum", "Hedef süre"]
    satirlar = []
    for L in secili:
        a = vs[L - 1]
        h = t["satirlar"][L - 1] if L <= len(t["satirlar"]) else None
        satir = [L] + [a["bedel"].get(k, "—") for k in kaynaklar] + [bicim.sure(a["sure"])]
        satir += [h["bedel"].get("Titanium", "—") if h else "—", bicim.sure(h["sure"]) if h else "—"]
        satirlar.append(satir)
    toplam_v8 = sum(x["sure"] for x in vs)
    return ["## Bugünkü oyunla karşılaştırma (catalog v8)", "",
            f"Kodda bugün: en fazla {v['adet']} adet, Reaktör {v['reaktor']} ile açılır, {v['maks']} seviye; "
            f"1 → {v['maks']} toplam ham süre {bicim.sure(toplam_v8)}. Hedef tasarımda aynı yol "
            f"{bicim.sure(t['toplam_sure'])} sürer.", "",
            md.tablo(basliklar, satirlar, ["r"] * len(basliklar)), ""]


def evrim_satirlari(model, evrim):
    """Bir binanın evrim satırları: (seviyeler, katman adı, bina adı, görünüm).

    Kubbeler gibi farklı seviye düzeni olan binalar kendi 'katmanlar' ve 'seviyeler' listelerini taşır.
    """
    katmanlar = (model.v.get("evrim") or {}).get("katmanlar", [])
    kat = {k["id"]: k for k in katmanlar}
    sira = evrim.get("katmanlar") or [k["id"] for k in katmanlar]
    seviyeler = evrim.get("seviyeler") or [kat.get(k, {}).get("seviyeler", "") for k in sira]
    adlar, gorunum = evrim.get("adlar", []), evrim.get("gorunum", [])
    return [(seviyeler[i], kat.get(kid, {}).get("ad", kid), adlar[i], gorunum[i] if i < len(gorunum) else "")
            for i, kid in enumerate(sira) if i < len(adlar)]


def evrim_tablosu(model, bid, evrim):
    return md.tablo(["Seviye", "Katman", "Ad", "Görünüm"], [list(s) for s in evrim_satirlari(model, evrim)])


# --- özet blokları ----------------------------------------------------------------
def ilk_cumle(metin):
    """Büyük harfle başlayan bir sonraki cümleden önce keser; "1. kuyruk" gibi sıra sayılarında bölmez."""
    parca = re.split(r"(?<=[a-zçğıöşü)])\.\s+(?=[A-ZÇĞİÖŞÜ])", metin, maxsplit=1)
    return parca[0].rstrip(".")


def bina_ozet(model):
    satirlar = []
    for b in model.bina_listesi:
        ilk = b["adet"][0]
        adet = len(b["adet"]) + (len(b["arastirma_ek"]["adet"]) if b.get("arastirma_ek") else 0)
        satirlar.append([bina_linki(model, b["id"]), model.kategoriler.get(b["kategori"], b["kategori"]),
                         model.bina_durumu(b["id"]), ilk if ilk else "Başlangıç", adet, model.maks(b), ilk_cumle(b["islev"])])
    return md.tablo(["Bina", "Kategori", "Durum", "Açılış (Reaktör)", "Adet", "Maks Sv", "İşlev"], satirlar,
                    ["l", "l", "l", "r", "r", "r", "l"])


def reaktor_capalari(model, sim_sonuc):
    r = model.bina_tablosu("core_reactor")["satirlar"]
    f2p = {x["seviye"]: x["gun"] for x in sim_sonuc["f2p"]["satirlar"]} if sim_sonuc else {}
    satirlar = []
    for L in (2, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50):
        x = r[L - 1]
        satirlar.append([L, bicim.sure(x["sure"]), x["bedel"].get("Titanium", 0), x["bedel"].get("Biomass", "—"),
                         x["bedel"].get("Alloy", "—"), x["kumulatif_guc"], bicim.gun(f2p.get(L))])
    return md.tablo(["Sv", "Ham süre", "Titanyum", "Biyokütle", "Süperalaşım", "Reaktör gücü (kümülatif)", "F2P'nin ulaştığı an"],
                    satirlar, ["r"] * 7)


def reaktor_on_kosul_tablosu(model):
    satirlar = []
    for L in range(2, 51):
        k = model.reaktor_on_kosul(L)
        if k:
            satirlar.append([L, ", ".join(f"{bina_linki(model, x)} {sv}" for x, sv in k)])
    return md.tablo(["Reaktör Sv", "Gerekenler"], satirlar, ["r", "l"])


def halka_tablosu(model):
    v8 = {h["halka"]: h for h in model.ilerleme["halkalar"].get("v8", [])}
    satirlar = []
    for h in model.ilerleme["halkalar"]["listesi"]:
        if h["halka"] <= model.ilerleme["halkalar"]["baslangic_acik"]:
            bedel, reak = "Başlangıçta açık", "—"
        else:
            bedel, reak = bedel_metni(model, model.halka_bedeli(h)), h["reaktor"]
        eski = v8.get(h["halka"])
        satirlar.append([h["halka"], h["sektor"], h.get("tip", "Serbest"), reak,
                         model.binalar[h["gerekli"]]["ad"] if h.get("gerekli") else "—", bedel,
                         f"R{eski['reaktor']} · {bedel_metni(model, eski['bedel'])}" if eski else "—"])
    toplam = sum(h["sektor"] for h in model.ilerleme["halkalar"]["listesi"])
    return md.tablo(["Halka", "Yuva", "Tip", "Reaktör", "Gerekli", "Hedef açılış bedeli", "v8"], satirlar,
                    ["r", "r", "l", "r", "l", "l", "l"]) + f"\n\nToplam yuva: **{toplam}**."


def slot_tablosu(model):
    satirlar = [[R, model.acik_slot(R), model.ornek_sayisi(R, arastirma=False), model.ornek_sayisi(R)]
                for R in (1, 3, 5, 8, 10, 12, 16, 20, 25, 30, 40, 50)]
    return md.tablo(["Reaktör", "Açık yuva", "Bina örneği", "Araştırmayla birlikte"], satirlar, ["r"] * 4)


def tavan_tablosu(model):
    kaynaklar = [k for k in KAYNAK_SIRASI if k in model.kaynaklar and (model.kaynaklar[k].get("hedef") or {}).get("tavan_carpani")]
    satirlar = []
    for R in (1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 49):
        satirlar.append([R] + [model.tavan(k, R) for k in kaynaklar])
    return md.tablo(["Reaktör"] + [model.kaynak_adi(k) for k in kaynaklar], satirlar, ["r"] * (len(kaynaklar) + 1))


def enerji_tablosu(model):
    satirlar = []
    for R in (1, 5, 8, 10, 15, 20, 25, 30, 35, 40, 45, 50):
        e = model.enerji_dengesi(R)
        satirlar.append([R, round(e["uretim"]), round(e["tuketim"]), round(e["net"])])
    return md.tablo(["Reaktör", "Üretim/sa", "Tüketim/sa", "Net/sa"], satirlar, ["r"] * 4)


def guc_tablosu(model):
    satirlar = [[L, round(model.e("guc", L))] for L in (1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50)]
    agirlik = {}
    for b in model.bina_listesi:
        agirlik.setdefault(b.get("guc_agirlik", 1.0), []).append(b["ad"])
    a_satir = [[w, ", ".join(adlar)] for w, adlar in sorted(agirlik.items(), reverse=True)]
    ref = [[R, model.referans_guc(R)] for R in (5, 10, 20, 30, 40, 50)]
    return "\n\n".join([
        md.tablo(["Sv", "güç(Sv)"], satirlar, ["r", "r"]),
        md.tablo(["Ağırlık", "Binalar"], a_satir, ["r", "l"]),
        "Referans üs (açık her bina o reaktör seviyesinde) bina gücü:",
        md.tablo(["Reaktör", "Bina gücü"], ref, ["r", "r"]),
    ])


def egri_tablosu(model):
    satirlar = []
    for ad, e in model.v["egriler"]["egriler"].items():
        capa = " · ".join(f"{a}: {bicim.sayi(b)}" for a, b in e["capalar"])
        satirlar.append([f"`{ad}`", e.get("ad", ""), e.get("birim", ""), capa])
    return md.tablo(["Eğri", "Ad", "Birim", "Çapalar (Sv: değer)"], satirlar)


def kaynak_tablosu(model):
    satirlar = []
    for k in model.v["kaynaklar"]["kaynaklar"]:
        v8 = k.get("v8") or {}
        satirlar.append([f"{k['ad']} ({k['sembol']})", f"`{k['id']}`", "✅" if k.get("durum") == "kodda" else "⬜",
                         k["kaynagi"], k["harcama"],
                         f"{bicim.sayi(v8.get('baslangic'))} / {bicim.sayi(v8.get('tavan'))}" if v8 else "—"])
    return md.tablo(["Kaynak", "Kimlik", "Durum", "Nereden gelir", "Nereye gider", "v8 başlangıç / tavan"], satirlar)


def v8_karsilastirma(model):
    satirlar = []
    for b in model.bina_listesi:
        t = model.bina_tablosu(b["id"])
        if not t["v8"]:
            continue
        v = t["v8"]
        son = v["maks"]
        h = t["satirlar"][min(son, len(t["satirlar"])) - 1]
        satirlar.append([b["ad"], f"{bicim.sayi(v['satirlar'][-1]['bedel'].get('Titanium', 0))} / {bicim.sure(v['satirlar'][-1]['sure'])}",
                         f"{bicim.sayi(h['bedel'].get('Titanium', 0))} / {bicim.sure(h['sure'])}",
                         bicim.sure(sum(x['sure'] for x in v['satirlar'])), bicim.sure(t["toplam_sure"])])
    return md.tablo(["Bina", "v8 son seviye Ti / süre", "Hedef son seviye Ti / süre", "v8 toplam süre", "Hedef toplam süre"], satirlar)


# --- simülasyon -------------------------------------------------------------------
def profil_ozet(model):
    satirlar = []
    for p in model.v["f2p_profilleri"]["profiller"]:
        h = p["hiz_yuzde"]
        hiz = f"min(%{bicim.sayi(h['tavan'])}, %{bicim.sayi(h['carpan'])} × Sv)" + (f" + %{bicim.sayi(h['ek'])}" if h.get("ek") else "")
        satirlar.append([
            p["ad"],
            " → ".join(f"R{a}: {b}" for a, b in p["insaatci"]),
            hiz,
            bicim.yuzde(round(p["yardim_orani"] * 100)),
            " → ".join(f"R{a}: {bicim.sayi(b)} sa" for a, b in p["hizlandirma_saat_gun"]),
            f"×{bicim.sayi(p['gelir_carpani'])}", f"×{bicim.sayi(p['alasim_carpani'])}",
            bicim.yuzde(round(p["kritik_pay"] * 100)),
            f"%{bicim.sayi(round(p['bekleme']['oran'] * 100))}, en çok {bicim.sayi(p['bekleme']['tavan_sa'])} sa",
        ])
    return md.tablo(["Profil", "İnşaatçı", "Hız bonusu", "Yardım kullanımı", "Hızlandırma (günlük)", "Gelir",
                     "Süperalaşım", "Kritik yola ayrılan", "Boşta bekleme"], satirlar)


def gelir_tablosu(model):
    g = model.v["f2p_profilleri"]["gelir"]
    satirlar = []
    for R in (1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50):
        ti = egri.deger(g["titanyum_saat"], R)
        pay = egri.deger(g.get("gorev_odulu_payi", [[1, 0], [2, 0]]), R) if R >= 2 else 0
        satirlar.append([R, round(ti), round(ti * g["biyokutle_orani"]), round(ti * 24),
                         bicim.yuzde(round(max(0, pay) * 100)) if pay > 0 else "—"])
    al = [[f"{bicim.sayi(gun)}. gün", round(egri.deger(g["alasim_gun"], gun))] for gun in (0, 30, 90, 180, 270, 337)]
    return "\n\n".join([
        md.tablo(["Reaktör", "Titanyum/sa", "Biyokütle/sa", "Titanyum/gün", "Hikâye görevlerinin karşıladığı reaktör maliyeti"],
                 satirlar, ["r"] * 5),
        "Süperalaşım geliri (Reaktör 30'a ulaşıldıktan sonraki gün sayısına göre, F2P):",
        md.tablo(["R30'dan sonra", "Süperalaşım/gün"], al, ["r", "r"]),
    ])


def insaatci_butcesi(model):
    kategori = {}
    for b in model.bina_listesi:
        adet = len(b["adet"]) + (len(b["arastirma_ek"]["adet"]) if b.get("arastirma_ek") else 0)
        gun = adet * model.bina_tablosu(b["id"])["toplam_sure"] / 86400
        k = model.kategoriler.get(b["kategori"], b["kategori"])
        kategori[k] = kategori.get(k, 0) + gun
    toplam = sum(kategori.values())
    satirlar = [[k, round(g), bicim.yuzde(round(g / toplam * 100))] for k, g in sorted(kategori.items(), key=lambda x: -x[1])]
    satirlar.append(["**Toplam**", f"**{bicim.sayi(round(toplam))}**", "%100"])
    return md.tablo(["Kategori", "Ham inşaatçı-günü (her örnek 1 → maks)", "Pay"], satirlar, ["l", "r", "r"])


def sim_ozet(model, sim_sonuc):
    hedef = model.v["f2p_profilleri"]["hedefler"]
    satirlar = []
    for pid, x in sim_sonuc.items():
        f = {r["seviye"]: r["gun"] for r in x["satirlar"]}
        h = hedef.get(pid, {})
        satirlar.append([x["ad"], f"**{bicim.sayi(round(x['gun_50']))}. gün**",
                         f"{h.get('gun', '—')} ({'–'.join(str(t) for t in h.get('tolerans', []))})"]
                        + [bicim.gun(f[L]) for L in (10, 20, 30, 40, 45)]
                        + [bicim.gun(x["toplam"]["bekleme_alasim"]) if x["toplam"]["bekleme_alasim"] else "—"])
    return md.tablo(["Profil", "Reaktör 50", "Hedef (aralık)", "R10", "R20", "R30", "R40", "R45", "Süperalaşım beklemesi"],
                    satirlar, ["l", "r", "r", "r", "r", "r", "r", "r", "r"])


def kilometre_tablosu(model, sim_sonuc):
    hedef = {int(k): v for k, v in model.v["f2p_profilleri"]["kilometre_taslari"].items() if not k.startswith("_")}
    f = {r["seviye"]: r["gun"] for r in sim_sonuc["f2p"]["satirlar"]}
    satirlar = [[L, bicim.gun(g), bicim.gun(f[L]), f"{bicim.sayi(round((f[L] - g) / g * 100))}%"] for L, g in sorted(hedef.items())]
    return md.tablo(["Reaktör", "Hedef", "Simülasyon", "Fark"], satirlar, ["r"] * 4)


def sim_sayfasi(model, sim_sonuc):
    out = [UST_NOT.format(kaynak="f2p_profilleri"), "# Tempo simülasyonu", "",
           "Üç oyuncu profilinin Çekirdek Reaktör 50'ye yolculuğu. Model ve varsayımlar: "
           "[02 · Tempo ve Denge](../02_tempo_ve_denge.md).", "", "## Özet", "", sim_ozet(model, sim_sonuc), ""]
    for pid, x in sim_sonuc.items():
        p = next(p for p in model.v["f2p_profilleri"]["profiller"] if p["id"] == pid)
        out += [f"## {x['ad']}", "", p["aciklama"], ""]
        satirlar = []
        for r in x["satirlar"]:
            satirlar.append([r["seviye"], bicim.gun(r["gun"]), bicim.sure(r["seviye_suresi"] * 86400), r["insaatci"],
                             bicim.yuzde(r["hiz"]), bicim.sure(r["onkosul_sn"]), bicim.sure(r["reaktor_sn"]),
                             bicim.sure(r["hizlandirma_sn"]), bicim.sure(r["bekleme_kaynak_sn"]),
                             bicim.sure(r["bekleme_alasim_sn"]), bicim.sure(r["bosta_sn"])])
        out += [md.tablo(["Sv", "Ulaşılan an", "Seviye süresi", "İnşaatçı", "Hız", "Önkoşul", "Reaktör (etkin)",
                          "Hızlandırma", "Kaynak bekleme", "Süperalaşım bekleme", "Boşta"], satirlar, ["r"] * 11), ""]
        tp = x["toplam"]
        out += [f"Toplam: hızlandırma {bicim.gun(tp['hizlandirma'])}, kaynak beklemesi {bicim.gun(tp['bekleme_kaynak'])}, "
                f"Süperalaşım beklemesi {bicim.gun(tp['bekleme_alasim'])}, boşta {bicim.gun(tp['bosta'])}. "
                f"Kritik yol dışında kalan inşaatçı zamanı: {bicim.gun(x['kalan_havuz_gun'])}.", ""]
    return "\n".join(out)


# --- araştırma ----------------------------------------------------------------
def arastirma_dal_sayfasi(model, dal):
    a = model.arastirma()
    teknolojiler = [t for t in a["teknolojiler"] if t["dal"] == dal["id"]]
    out = [UST_NOT.format(kaynak="arastirma"), f"# {dal['ad']}", "",
           f"`{dal['id']}` · {dal.get('kuyruk_ad', '')} · {len(teknolojiler)} teknoloji · "
           f"{sum(t['maks'] for t in teknolojiler)} seviye", "", f"> {dal.get('aciklama', '')}", ""]
    if dal.get("acilis"):
        out += [f"**Açılış.** {dal['acilis']}", ""]
    ozet = []
    for t in teknolojiler:
        tt = model.teknoloji_tablosu(t)
        son = tt["satirlar"][-1]
        etki = t.get("seviye_basi")
        ozet.append([f"[{t['ad']}](#{t['id'].replace('_', '-')})", "⬜" if t.get("durum") != "kodda" else ("🟡" if tt["v8"] else "⬜"),
                     t["maks"], etki_adi(model, t.get("etki"), t.get("etki_ad")),
                     _etki_metni(t, son["etki"]), f"{t.get('min_lab', 1)}–{son['lab']}",
                     bedel_metni(model, tt["toplam_bedel"]), bicim.sure(tt["toplam_sure"])])
    out += [md.tablo(["Teknoloji", "Durum", "Maks", "Etki", "Toplam etki", "Lab Sv", "Toplam maliyet", "Toplam süre"], ozet), ""]
    for t in teknolojiler:
        tt = model.teknoloji_tablosu(t)
        out += [f"<a id=\"{t['id'].replace('_', '-')}\"></a>", f"## {t['ad']}", "", f"`{t['id']}` · {t.get('aciklama', '')}", ""]
        if t.get("gerekli"):
            out += ["**Gerekli:** " + ", ".join(f"{_tek_ad(a, g['id'])} {g['seviye']}" for g in t["gerekli"]), ""]
        kaynaklar = kaynak_sutunlari(tt["satirlar"])
        satirlar = [[r["seviye"], r["lab"]] + [r["bedel"].get(k, "—") for k in kaynaklar]
                    + [bicim.sure(r["sure"]), _etki_metni(t, r["etki"]), r["guc"]] for r in tt["satirlar"]]
        out += [md.tablo(["Sv", "Lab"] + [model.kaynak_adi(k) for k in kaynaklar] + ["Süre", "Etki (toplam)", "Güç"],
                         satirlar, ["r"] * (len(kaynaklar) + 5)), ""]
        if tt["v8"]:
            v8s = tt["v8"]
            satir_v8 = [[x["seviye"], x["merkez"], bedel_metni(model, x["bedel"]), bicim.sure(x["sure"])] for x in v8s]
            out += ["Bugünkü oyunda (catalog v8):", "", md.tablo(["Sv", "Merkez Sv", "Maliyet", "Süre"], satir_v8, ["r", "r", "l", "r"]), ""]
    out += ["← [06 · Araştırma](../06_arastirma.md)", ""]
    return "\n".join(out)


def _tek_ad(a, tid):
    for t in a["teknolojiler"]:
        if t["id"] == tid:
            return t["ad"]
    return tid


def _etki_metni(t, deger):
    if deger is None:
        return t.get("etki_metni", "—")
    birim = t.get("birim", "%")
    if birim == "%":
        return bicim.yuzde(deger) if deger >= 0 else bicim.yuzde(deger)
    return f"{bicim.sayi(deger)} {birim}".strip()


def arastirma_ozet(model):
    a = model.arastirma()
    if not a:
        return "_Araştırma verisi henüz yok._"
    satirlar = []
    for d in a["dallar"]:
        ts = [t for t in a["teknolojiler"] if t["dal"] == d["id"]]
        satirlar.append([f"[{d['ad']}](arastirma/{d['id']}.md)", d.get("kuyruk_ad", ""), len(ts), sum(t["maks"] for t in ts),
                         d.get("acilis", ""), d.get("aciklama", "")])
    top_t = sum(r[2] for r in satirlar)
    top_s = sum(r[3] for r in satirlar)
    return md.tablo(["Dal", "Kuyruk", "Teknoloji", "Seviye", "Açılış", "Odak"], satirlar,
                    ["l", "l", "r", "r", "l", "l"]) + f"\n\nToplam: **{top_t} teknoloji, {top_s} seviye.**"


# --- kahramanlar ----------------------------------------------------------------
def kahraman_ozet(model):
    k = model.kahramanlar()
    if not k:
        return "_Kahraman verisi henüz yok._"
    nad = {n["id"]: n for n in k["nadirlikler"]}
    sinif = {s["id"]: s["ad"] for s in k["siniflar"]}
    fr = {f["id"]: f["ad"] for f in k["fraksiyonlar"]}
    sira = {n["id"]: i for i, n in enumerate(k["nadirlikler"])}
    satirlar = []
    for h in sorted(k["kahramanlar"], key=lambda h: (-sira[h["nadirlik"]], h["ad"])):
        st = model.kahraman_stat(h, 50, 5)
        satirlar.append([f"[{h['ad']}](kahramanlar/{h['id']}.md)", nad[h["nadirlik"]]["ad"], sinif.get(h["sinif"], h["sinif"]),
                         fr.get(h["fraksiyon"], h["fraksiyon"]), h.get("nesil", 1), "✅" if h.get("durum") == "kodda" else "⬜",
                         st["guc"]])
    return md.tablo(["Kahraman", "Nadirlik", "Sınıf", "Fraksiyon", "Nesil", "Kodda", "Güç (Sv 50, 5★)"], satirlar,
                    ["l", "l", "l", "l", "r", "l", "r"])


def kahraman_sayfasi(model, h):
    k = model.kahramanlar()
    nad = {n["id"]: n for n in k["nadirlikler"]}[h["nadirlik"]]
    sinif = {s["id"]: s for s in k["siniflar"]}[h["sinif"]]
    fr = {f["id"]: f for f in k["fraksiyonlar"]}.get(h["fraksiyon"], {"ad": h["fraksiyon"]})
    out = [UST_NOT.format(kaynak="kahramanlar"), f"# {h['ad']}", "",
           f"`{h['id']}` · {nad['ad']} · {sinif['ad']} · {fr['ad']} · {h.get('nesil', 1)}. nesil · "
           + ("✅ kodda" if h.get("durum") == "kodda" else "⬜ tasarım"), ""]
    if h.get("unvan"):
        out += [f"*{h['unvan']}*", ""]
    if h.get("biyografi"):
        out += [f"> {h['biyografi']}", ""]
    birlik = {b["id"]: b["ad"] for b in k.get("birlik_turleri", [])}.get(sinif.get("birlik"), sinif.get("birlik", "—"))
    out += [md.tablo(["Özellik", "Değer"], [
        ["Sınıf", f"{sinif['ad']} — yatkın olduğu birlik: {birlik}"],
        ["Fraksiyon", fr["ad"]],
        ["Nasıl bulunur", h.get("kazanim", "Kriyo-Kapsül, kahraman kartları")],
        ["Yetenek ağaçları", ", ".join({u["id"]: u["ad"] for u in k.get("uzmanliklar", [])}.get(a, a) for a in h.get("agaclar", [])) or "—"],
    ]), ""]

    mur = h.get("murettebat")
    if mur:
        out += ["## Mürettebat becerisi", "",
                f"Komuta ekibindeyken çalışan ekonomi becerisi: **{etki_adi(model, mur['etki'], mur.get('ad'))}**.", ""]
        satirlar = []
        for y in range(1, 6):
            L = y * 10
            deger = mur["taban"] + int((L - 1) * mur["seviye_onda"] / 10) + (y - 1) * mur["yildiz"]
            satirlar.append([f"{y}★", L, bicim.yuzde(deger)])
        out += [md.tablo(["Yıldız", "En yüksek Sv", "Etki"], satirlar, ["l", "r", "r"]),
                "", f"Sv 1, 1★: {bicim.yuzde(mur['taban'])}.", ""]

    yet = h.get("yetenekler", [])
    if yet:
        out += ["## Yetenekler", ""]
        satirlar = []
        for y in yet:
            degerler = y.get("degerler", [])
            satirlar.append([y["ad"], y.get("tur_ad", y.get("tur", "")), f"{y.get('yildiz', 1)}★",
                             y.get("aciklama", "").replace("{0}", " / ".join(bicim.sayi(d) for d in degerler) if degerler else "")])
        out += [md.tablo(["Yetenek", "Tür", "Açılış", "Etki (yetenek seviyesi 1 / 2 / 3 / 4 / 5)"], satirlar), ""]

    out += ["## İstatistikler", "",
            "ATK / DEF / HP; seviye sınırı yıldız × 10. Güç = (ATK + DEF + HP / 10) × 10.", ""]
    satirlar = []
    for L in (1, 10, 20, 30, 40, 50):
        satir = [L]
        for y in range(1, 6):
            if L > y * 10:
                satir.append("—")
            else:
                s = model.kahraman_stat(h, L, y)
                satir.append(f"{bicim.sayi(s['atk'])} / {bicim.sayi(s['def'])} / {bicim.sayi(s['hp'])}")
        satirlar.append(satir)
    out += [md.tablo(["Sv", "1★", "2★", "3★", "4★", "5★"], satirlar, ["r"] * 6), ""]
    s50 = model.kahraman_stat(h, 50, 5)
    out += [f"Sv 50, 5★ güç: **{bicim.sayi(s50['guc'])}**.", ""]
    if h.get("notlar"):
        out += ["## Tasarım notları", "", h["notlar"], ""]
    out += ["← [05 · Kahramanlar](../05_kahramanlar.md)", ""]
    return "\n".join(out)


def kahraman_egitim_tablosu(model):
    k = model.kahramanlar()
    if not k:
        return "_Kahraman verisi henüz yok._"
    satirlar = []
    for L in (1, 5, 10, 20, 30, 40, 49):
        satir = [f"{L} → {L + 1}"]
        for n in k["nadirlikler"]:
            h = {"nadirlik": n["id"]}
            satir.append(bedel_metni(model, model.kahraman_egitim(h, L)))
        satirlar.append(satir)
    return md.tablo(["Seviye"] + [n["ad"] for n in k["nadirlikler"]], satirlar)


def evrim_ozet(model):
    e = model.v.get("evrim")
    if not e:
        return "_Evrim verisi henüz yok._"
    satirlar = [[k.get("seviyeler", ""), k["ad"], k.get("tema", ""), k.get("malzeme", "")] for k in e.get("katmanlar", [])]
    return md.tablo(["Seviye", "Katman", "Tema", "Malzeme ve ışık"], satirlar)


def sozluk_binalar(model):
    return md.tablo(["Türkçe ad", "Kimlik", "Kategori"],
                    [[bina_linki(model, b["id"]), f"`{b['id']}`", model.kategoriler.get(b["kategori"], b["kategori"])]
                     for b in model.bina_listesi])


def sozluk_kahramanlar(model):
    k = model.kahramanlar()
    if not k:
        return "_Kahraman verisi henüz yok._"
    return md.tablo(["Türkçe ad", "Kimlik"], [[f"[{h['ad']}](kahramanlar/{h['id']}.md)", f"`{h['id']}`"] for h in k["kahramanlar"]])


def sozluk_arastirma(model):
    a = model.arastirma()
    if not a:
        return "_Araştırma verisi henüz yok._"
    return md.tablo(["Türkçe ad", "Kimlik", "Dal"],
                    [[t["ad"], f"`{t['id']}`", {d["id"]: d["ad"] for d in a["dallar"]}.get(t["dal"], t["dal"])]
                     for t in a["teknolojiler"]])
