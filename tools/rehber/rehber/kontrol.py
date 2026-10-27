"""Kılavuz tutarlılık kontrolleri. Her kontrol (hatalar, uyarılar) listesine ekler."""
import re

from . import v8
from .yollar import REHBER

SNAKE = re.compile(r"^[a-z][a-z0-9_]*$")
LINK = re.compile(r"\]\((?!https?://|#|mailto:)([^)#\s]+)(?:#[^)]*)?\)")


def calistir(model, sim_sonuc):
    h, u = [], []
    _monoton(model, h)
    _tavan(model, h)
    _on_kosul(model, h)
    _slot(model, h)
    _enerji(model, h)
    _arastirma(model, h, u)
    _kahraman(model, h, u)
    _simulasyon(model, sim_sonuc, h, u)
    _oduller(model, sim_sonuc, h, u)
    _kimlik(model, h)
    _parite(model, h)
    _bilim(model, h, u)
    _metin(model, h, u)
    return h, u


def _monoton(m, h):
    for b in m.bina_listesi:
        s = [r for r in m.bina_tablosu(b["id"])["satirlar"] if not r["hazir"]]
        for a, c in zip(s, s[1:]):
            for k, x in a["bedel"].items():
                if c["bedel"].get(k, x) < x:
                    h.append(f"{b['id']}: {k} maliyeti Sv {a['seviye']}→{c['seviye']} düşüyor")
            if c["sure"] < a["sure"]:
                h.append(f"{b['id']}: süre Sv {a['seviye']}→{c['seviye']} düşüyor")
            if c["guc"] < a["guc"]:
                h.append(f"{b['id']}: güç Sv {a['seviye']}→{c['seviye']} düşüyor")
            for k, x in a["uretim"].items():
                if c["uretim"].get(k, x) < x:
                    h.append(f"{b['id']}: {k} üretimi Sv {a['seviye']}→{c['seviye']} düşüyor")
    r = [x for x in m.bina_tablosu("core_reactor")["satirlar"] if not x["hazir"]]
    for a, c in zip(r, r[1:]):
        if not (c["sure"] > a["sure"] and c["bedel"]["Titanium"] > a["bedel"]["Titanium"]):
            h.append(f"core_reactor: Sv {a['seviye']}→{c['seviye']} kesin artmıyor")


def _tavan(m, h):
    def kontrol_et(ad, R, bedel):
        for k, x in bedel.items():
            t = m.tavan(k, R)
            if t is not None and x > t:
                h.append(f"{ad}: {k} {x:,} > Reaktör {R} tavanı {t:,}")

    for b in m.bina_listesi:
        for r in m.bina_tablosu(b["id"])["satirlar"]:
            if not r["hazir"]:
                kontrol_et(f"{b['id']} Sv {r['seviye']}", max(1, r["reaktor"]), r["bedel"])
    for hl in m.ilerleme["halkalar"]["listesi"]:
        if hl["halka"] > m.ilerleme["halkalar"]["baslangic_acik"]:
            kontrol_et(f"halka {hl['halka']}", hl["reaktor"], m.halka_bedeli(hl))
    if m.arastirma():
        for t in m.arastirma()["teknolojiler"]:
            for r in m.teknoloji_tablosu(t)["satirlar"]:
                kontrol_et(f"araştırma {t['id']} Sv {r['seviye']}", r["lab"], r["bedel"])


def _on_kosul(m, h):
    for L in range(2, 51):
        for bid, sv in m.reaktor_on_kosul(L):
            b = m.binalar.get(bid)
            if not b:
                h.append(f"Reaktör {L}: önkoşul binası '{bid}' yok")
                continue
            if b["adet"][0] > L - 1:
                h.append(f"Reaktör {L}: {bid} henüz açılmamış (Reaktör {b['adet'][0]} gerekir)")
            if sv > m.maks(b):
                h.append(f"Reaktör {L}: {bid} {sv} > en yüksek seviye {m.maks(b)}")
            elif m.gerekli_reaktor(b, sv) > L - 1:
                h.append(f"Reaktör {L}: {bid} {sv} için Reaktör {m.gerekli_reaktor(b, sv)} gerekir (döngü)")
    for b in m.bina_listesi:
        for g in b.get("gerekli", []):
            hedef = m.binalar.get(g["bina"])
            if not hedef:
                h.append(f"{b['id']}: gerekli bina '{g['bina']}' yok")
            elif g["seviye"] > b["adet"][0]:
                h.append(f"{b['id']}: {g['bina']} {g['seviye']} ister ama Reaktör {b['adet'][0]}'de açılır")


def _slot(m, h):
    for R in range(1, 51):
        if m.ornek_sayisi(R) > m.acik_slot(R):
            h.append(f"Reaktör {R}: {m.ornek_sayisi(R)} bina örneği > {m.acik_slot(R)} açık yuva")


def _enerji(m, h):
    for R in range(1, 51):
        e = m.enerji_dengesi(R)
        if e["net"] < 0:
            h.append(f"Reaktör {R}: referans üs enerji açığı {e['net']:.0f}/sa")


def _arastirma(m, h, u):
    a = m.arastirma()
    if not a:
        u.append("arastirma.json yok")
        return
    dallar = {d["id"] for d in a["dallar"]}
    ids = {}
    for t in a["teknolojiler"]:
        if t["id"] in ids:
            h.append(f"araştırma: '{t['id']}' iki kez tanımlı")
        ids[t["id"]] = t
        if t["dal"] not in dallar:
            h.append(f"araştırma {t['id']}: dal '{t['dal']}' yok")
        son = t.get("min_lab", 1) + t.get("adim", 2) * (t["maks"] - 1)
        if son > 50:
            u.append(f"araştırma {t['id']}: son seviye Lab {son} ister (50'ye kırpılır)")
    for t in a["teknolojiler"]:
        for g in t.get("gerekli", []):
            if g["id"] not in ids:
                h.append(f"araştırma {t['id']}: gerekli '{g['id']}' yok")
            elif g["seviye"] > ids[g["id"]]["maks"]:
                h.append(f"araştırma {t['id']}: {g['id']} {g['seviye']} > maks")
    durum = {}

    def ziyaret(tid, yol):
        if durum.get(tid) == 2:
            return
        if durum.get(tid) == 1:
            h.append("araştırma döngüsü: " + " → ".join(yol + [tid]))
            return
        durum[tid] = 1
        for g in ids[tid].get("gerekli", []):
            if g["id"] in ids:
                ziyaret(g["id"], yol + [tid])
        durum[tid] = 2

    for tid in ids:
        ziyaret(tid, [])


def _kahraman(m, h, u):
    k = m.kahramanlar()
    if not k:
        u.append("kahramanlar.json yok")
        return
    nad = {n["id"] for n in k["nadirlikler"]}
    sinif = {s["id"] for s in k["siniflar"]}
    fr = {f["id"] for f in k["fraksiyonlar"]}
    gorulen = set()
    for x in k["kahramanlar"]:
        if x["id"] in gorulen:
            h.append(f"kahraman '{x['id']}' iki kez tanımlı")
        gorulen.add(x["id"])
        for alan, kume in (("nadirlik", nad), ("sinif", sinif), ("fraksiyon", fr)):
            if x.get(alan) not in kume:
                h.append(f"kahraman {x['id']}: {alan} '{x.get(alan)}' tanımsız")
        for y in x.get("yetenekler", []):
            if not 1 <= y.get("yildiz", 1) <= 5:
                h.append(f"kahraman {x['id']}: '{y['ad']}' yıldızı 1–5 dışında")


def _simulasyon(m, s, h, u):
    hedef = m.v["f2p_profilleri"]["hedefler"]
    for pid, x in s.items():
        t = hedef.get(pid)
        if t and not t["tolerans"][0] <= x["gun_50"] <= t["tolerans"][1]:
            h.append(f"simülasyon {pid}: Reaktör 50 {x['gun_50']:.1f}. gün, hedef {t['tolerans'][0]}–{t['tolerans'][1]}")
    f = {r["seviye"]: r["gun"] for r in s["f2p"]["satirlar"]}
    for L, g in m.v["f2p_profilleri"]["kilometre_taslari"].items():
        if L.startswith("_"):
            continue
        L = int(L)
        tol = 1.0 if L < 20 else 0.15 * g
        if abs(f[L] - g) > tol:
            h.append(f"simülasyon f2p: Reaktör {L} {f[L]:.1f}. gün, hedef {g} ± {tol:.1f}")


def _oduller(m, s, h, u):
    o = m.v.get("oduller")
    if not o or "f2p_gelir_ozeti" not in o:
        u.append("oduller.json'da f2p_gelir_ozeti yok; gelir kaynakları profille karşılaştırılmadı")
        return
    from . import egri
    g = m.v["f2p_profilleri"]["gelir"]
    oz = o["f2p_gelir_ozeti"]
    for R, ti in oz.get("titanyum_saat", []):
        beklenen = egri.deger(g["titanyum_saat"], R)
        if abs(ti - beklenen) > 0.2 * beklenen:
            h.append(f"ödüller: Reaktör {R} Titanyum geliri {ti:,.0f}/sa, profil {beklenen:,.0f}/sa (±%20)")
    for gun, al in oz.get("alasim_gun", []):
        beklenen = egri.deger(g["alasim_gun"], gun)
        if abs(al - beklenen) > 0.2 * beklenen:
            h.append(f"ödüller: {gun}. gün Süperalaşım {al:.0f}/gün, profil {beklenen:.0f}/gün (±%20)")


def _kimlik(m, h):
    kat = m.katalog
    for b in kat["buildings"]:
        if b["id"] not in m.binalar:
            h.append(f"v8 binası '{b['id']}' kılavuzda yok")
    if m.kahramanlar():
        ids = {x["id"] for x in m.kahramanlar()["kahramanlar"]}
        for x in kat["heroes"]["list"]:
            if x["id"] not in ids:
                h.append(f"v8 kahramanı '{x['id']}' kılavuzda yok")
    if m.arastirma():
        ids = {x["id"] for x in m.arastirma()["teknolojiler"]}
        for x in kat["research"]["nodes"]:
            if x["id"] not in ids:
                h.append(f"v8 araştırması '{x['id']}' kılavuzda yok")
    tum = [b["id"] for b in m.bina_listesi]
    if m.kahramanlar():
        tum += [x["id"] for x in m.kahramanlar()["kahramanlar"]]
    if m.arastirma():
        tum += [x["id"] for x in m.arastirma()["teknolojiler"]] + [d["id"] for d in m.arastirma()["dallar"]]
    for i in tum:
        if not SNAKE.match(i):
            h.append(f"kimlik '{i}' İngilizce snake_case değil")


def _parite(m, h):
    kat = m.katalog
    r = v8.bina(kat, "core_reactor")
    if v8.bina_bedel(kat, r, 50) != {"Titanium": 6822} or v8.bina_sure(kat, r, 50) != 7741:
        h.append("v8 paritesi: reaktör Sv 50 6.822 Ti / 7.741 sn değil")
    hub = v8.bina_bedel(kat, v8.bina(kat, "alliance_hub"), 50)["Titanium"]
    if abs(hub - 20465) > 2:
        h.append(f"v8 paritesi: İttifak Merkezi Sv 50 {hub} (beklenen ≈20.465)")


def _bilim(m, h, u):
    b = m.v.get("bilim")
    if not b:
        u.append("bilim.json yok")
        return
    kaynak_ids = {k["id"] for k in b.get("kaynaklar", [])}
    for k in b.get("kaynaklar", []):
        if not str(k.get("url", "")).startswith("http"):
            h.append(f"bilim kaynağı {k.get('id')}: URL yok")

    def gez(d, yol):
        if isinstance(d, dict):
            if "kaynak" in d:
                if not d["kaynak"]:
                    h.append(f"bilim {yol}: kaynak listesi boş")
                for kid in d["kaynak"]:
                    if kid not in kaynak_ids:
                        h.append(f"bilim {yol}: kaynak '{kid}' tanımsız")
            for a, x in d.items():
                if a != "kaynaklar":
                    gez(x, f"{yol}.{a}")
        elif isinstance(d, list):
            for i, x in enumerate(d):
                gez(x, f"{yol}[{x.get('id', i) if isinstance(x, dict) else i}]")

    gez(b, "bilim")
    sinir = None
    for k in b.get("kurallar", []):
        if k.get("id") == "balik_derinlik_siniri":
            sinir = max(k["deger_m"]) if isinstance(k["deger_m"], list) else k["deger_m"]
    for t in b.get("turler", []):
        if t.get("min_m") is not None and t.get("max_m") is not None and t["min_m"] > t["max_m"]:
            h.append(f"bilim tür {t['id']}: min_m > max_m")
        if sinir and t.get("grup") == "balik" and (t.get("max_m") or 0) > sinir:
            u.append(f"bilim tür {t['id']}: balık {t['max_m']} m'de, sınır {sinir} m")


def _metin(m, h, u):
    yasak = [x.lower() for x in (m.v.get("yasakli_isimler") or {}).get("isimler", [])]
    for yol in sorted(REHBER.rglob("*.md")):
        metin = yol.read_text(encoding="utf-8")
        ad = yol.relative_to(REHBER).as_posix()
        if re.search(r"\bKlavuz", metin):
            h.append(f"{ad}: 'Klavuz' yazımı (doğrusu 'Kılavuz')")
        kucuk = metin.lower()
        for y in yasak:
            if re.search(r"(?<![\wçğıöşü])" + re.escape(y) + r"(?![\wçğıöşü])", kucuk):
                h.append(f"{ad}: yasaklı isim '{y}'")
        for hedef in LINK.findall(metin):
            if not (yol.parent / hedef).resolve().exists():
                h.append(f"{ad}: kırık link '{hedef}'")
    for yol in sorted((REHBER / "veri").glob("*.json")):
        if yol.stem == "yasakli_isimler":
            continue
        kucuk = yol.read_text(encoding="utf-8").lower()
        for y in yasak:
            if re.search(r"(?<![\wçğıöşü])" + re.escape(y) + r"(?![\wçğıöşü])", kucuk):
                h.append(f"veri/{yol.name}: yasaklı isim '{y}'")
