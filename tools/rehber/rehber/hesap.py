"""Hedef tasarımın seviye seviye hesapları: binalar, reaktör önkoşulları, tavanlar, araştırma, kahramanlar."""
from . import egri, v8

KAYNAK_SIRASI = ["Titanium", "Biomass", "Food", "Oxygen", "Energy", "Alloy", "Hadalite"]


class Model:
    def __init__(self, v):
        self.v = v
        self.katalog = v["_katalog"]
        self.egriler = {ad: e["capalar"] for ad, e in v["egriler"]["egriler"].items()}
        self.kaynaklar = {k["id"]: k for k in v["kaynaklar"]["kaynaklar"]}
        self.bina_listesi = v["binalar"]["binalar"]
        self.binalar = {b["id"]: b for b in self.bina_listesi}
        self.kategoriler = {k["id"]: k["ad"] for k in v["binalar"].get("kategoriler", [])}
        self.ilerleme = v["ilerleme"]
        self._bina_tablosu = {}

    # --- eğri ---------------------------------------------------------------
    def e(self, ad, x):
        return egri.deger(self.egriler[ad], x)

    def kaynak_adi(self, kid):
        k = self.kaynaklar.get(kid)
        return k["ad"] if k else kid

    # --- binalar ------------------------------------------------------------
    def maks(self, b):
        return b.get("maks", 50)

    def esdeger(self, b, seviye):
        m = b.get("seviye_esleme")
        return m[seviye - 1] if m else seviye

    def gerekli_reaktor(self, b, seviye):
        """Bu seviyeye yükseltmek için gereken reaktör seviyesi."""
        if b["id"] == "core_reactor":
            return seviye - 1
        m = b.get("seviye_esleme")
        ilk = b["adet"][0]
        return max(m[seviye - 1], ilk) if m else max(seviye, ilk)

    def bedel(self, b, seviye):
        if seviye <= b.get("hazir_seviye", 0):
            return {}
        es = self.esdeger(b, seviye)
        out = {}
        for s in b.get("bedel", []):
            if es < s.get("baslangic", 1):
                continue
            out[s["kaynak"]] = egri.yuvarla_bedel(s["olcek"] * self.e(s.get("egri", "reaktor_bedel"), es))
        return out

    def sure(self, b, seviye):
        if seviye <= b.get("hazir_seviye", 0):
            return 0
        return egri.yuvarla_sure(b["sure_olcek"] * self.e("reaktor_sure", self.esdeger(b, seviye)))

    def guc(self, b, seviye):
        return int(round(b.get("guc_agirlik", 1.0) * self.e("guc", self.esdeger(b, seviye))))

    def akis(self, b, seviye, alan):
        es = self.esdeger(b, seviye)
        return {s["kaynak"]: egri.yuvarla_uretim(s["saatlik"] * self.e(s.get("egri", "uretim"), es))
                for s in b.get(alan, [])}

    def etkiler(self, b, seviye):
        es = self.esdeger(b, seviye)
        return [{"ad": e["ad"], "deger": egri.yuvarla_bedel(e["olcek"] * self.e(e["egri"], es)), "bicim": e.get("bicim")}
                for e in b.get("etkiler", [])]

    def bina_tablosu(self, bid):
        if bid in self._bina_tablosu:
            return self._bina_tablosu[bid]
        b = self.binalar[bid]
        satirlar, top_bedel, top_sure, top_guc = [], {}, 0, 0
        for L in range(1, self.maks(b) + 1):
            bedel = self.bedel(b, L)
            sure = self.sure(b, L)
            guc = self.guc(b, L)
            for k, x in bedel.items():
                top_bedel[k] = top_bedel.get(k, 0) + x
            top_sure += sure
            top_guc += guc
            satirlar.append({
                "seviye": L, "esdeger": self.esdeger(b, L), "reaktor": self.gerekli_reaktor(b, L),
                "bedel": bedel, "sure": sure, "guc": guc,
                "uretim": self.akis(b, L, "uretim"), "tuketim": self.akis(b, L, "tuketim"),
                "etkiler": self.etkiler(b, L),
                "kumulatif_bedel": dict(top_bedel), "kumulatif_sure": top_sure, "kumulatif_guc": top_guc,
                "hazir": L <= b.get("hazir_seviye", 0),
            })
        out = {"satirlar": satirlar, "toplam_bedel": top_bedel, "toplam_sure": top_sure, "toplam_guc": top_guc,
               "v8": self.bina_v8(bid)}
        self._bina_tablosu[bid] = out
        return out

    def bina_v8(self, bid):
        kb = v8.bina(self.katalog, bid)
        if not kb:
            return None
        satirlar = []
        for L in range(1, v8.maks_seviye(self.katalog, kb) + 1):
            satirlar.append({
                "seviye": L,
                "bedel": v8.bina_bedel(self.katalog, kb, L),
                "sure": v8.bina_sure(self.katalog, kb, L),
                "uretim": v8.bina_uretim_saat(kb, L, "production"),
                "tuketim": v8.bina_uretim_saat(kb, L, "consumption"),
            })
        return {"satirlar": satirlar, "adet": kb["maxCount"], "reaktor": kb["requiredReactorLevel"],
                "maks": v8.maks_seviye(self.katalog, kb)}

    def bina_durumu(self, bid):
        """✅ kodda ve aynı · 🟡 kodda ama farklı/işlevsiz · ⬜ yok."""
        b = self.binalar[bid]
        if b.get("durum") != "kodda" or not v8.bina(self.katalog, bid):
            return "⬜"
        t = self.bina_tablosu(bid)
        v = t["v8"]["satirlar"]
        ayni = len(v) == len(t["satirlar"]) and all(
            a["bedel"] == b2["bedel"] and a["sure"] == b2["sure"] for a, b2 in zip(t["satirlar"], v))
        return "✅" if ayni else "🟡"

    # --- reaktör ------------------------------------------------------------
    def reaktor_on_kosul(self, L):
        """Reaktörü L seviyesine çıkarmak için gereken [(bina, seviye)]."""
        rk = self.ilerleme["reaktor_on_kosul"]
        out = [tuple(x) for x in rk.get("sabit", {}).get(str(L), [])]
        kal = rk.get("kalici")
        if kal and L >= kal["baslangic"]:
            out.append((kal["bina"], L - 1))
        don = rk.get("donusumlu")
        if don and L >= don["baslangic"]:
            liste = don["binalar"]
            out.append((liste[(L - don["baslangic"]) % len(liste)], L - 1))
        out += [tuple(x) for x in rk.get("kubbe", {}).get(str(L), [])]
        return out

    def reaktor_acilislari(self, L):
        """Reaktör L seviyesine ulaşınca açılan bina örnekleri ve halkalar."""
        binalar = []
        for b in self.bina_listesi:
            if b["id"] == "core_reactor":
                continue
            for i, r in enumerate(b["adet"]):
                if r == L or (L == 1 and r == 0):
                    binalar.append((b["id"], i + 1))
        halkalar = [h["halka"] for h in self.ilerleme["halkalar"]["listesi"] if h["reaktor"] == L and h["halka"] > 4]
        return {"binalar": binalar, "halkalar": halkalar}

    def halka_bedeli(self, h):
        reaktor = self.binalar["core_reactor"]
        olcek = self.ilerleme["halkalar"]["bedel_olcek"]
        tam = self.bedel(reaktor, max(2, h["reaktor"]))
        return {k: egri.yuvarla_bedel(olcek * x) for k, x in tam.items() if k in ("Titanium", "Biomass")}

    def acik_slot(self, R):
        return sum(h["sektor"] for h in self.ilerleme["halkalar"]["listesi"] if h["reaktor"] <= R)

    def ornek_sayisi(self, R, arastirma=True):
        n = 0
        for b in self.bina_listesi:
            n += sum(1 for r in b["adet"] if r <= R)
            if arastirma and b.get("arastirma_ek"):
                n += sum(1 for r in b["arastirma_ek"]["adet"] if r <= R)
        return n

    def tavan(self, kaynak, R):
        """Hedef cüzdan tavanı: max(v8 tavanı, çarpan × reaktör(R+1) Titanyum maliyeti)."""
        k = self.kaynaklar[kaynak]
        carpan = (k.get("hedef") or {}).get("tavan_carpani", 0)
        if not carpan:
            return None
        v8t = (k.get("v8") or {}).get("tavan", 0)
        ref = self.bedel(self.binalar["core_reactor"], min(50, R + 1)).get("Titanium", 0)
        return max(v8t, egri.yuvarla_bedel(carpan * ref))

    def bina_seviyesi_reaktorde(self, b, R):
        """Referans üs: R reaktöründe bu bina en fazla hangi seviyededir."""
        if R < b["adet"][0]:
            return 0
        m = b.get("seviye_esleme")
        if m:
            return max((i + 1 for i, g in enumerate(m) if g <= R), default=0)
        return min(R, self.maks(b))

    def enerji_dengesi(self, R):
        """Referans üs: açık her örnek R seviyesinde; Enerji üretimi − tüketimi (saatlik)."""
        uret = tuket = 0.0
        for b in self.bina_listesi:
            sev = self.bina_seviyesi_reaktorde(b, R)
            if not sev:
                continue
            adet = sum(1 for r in b["adet"] if r <= R)
            uret += adet * self.akis(b, sev, "uretim").get("Energy", 0)
            tuket += adet * self.akis(b, sev, "tuketim").get("Energy", 0)
        return {"uretim": uret, "tuketim": tuket, "net": uret - tuket}

    def referans_guc(self, R):
        """Referans üs gücü: açık her bina örneğinin R'deki kümülatif gücü."""
        top = 0
        for b in self.bina_listesi:
            sev = self.bina_seviyesi_reaktorde(b, R)
            if not sev:
                continue
            adet = sum(1 for r in b["adet"] if r <= R)
            top += adet * self.bina_tablosu(b["id"])["satirlar"][sev - 1]["kumulatif_guc"]
        return top

    # --- araştırma -----------------------------------------------------------
    def arastirma(self):
        return self.v.get("arastirma")

    def teknoloji_tablosu(self, t):
        a = self.arastirma()
        varsayilan = a.get("varsayilan_bedel", [{"kaynak": "Titanium", "olcek": 0.25},
                                                {"kaynak": "Biomass", "olcek": 0.09, "baslangic": 2}])
        sure_olcek = a.get("sure_olcek", 0.35)
        guc_agirlik = a.get("guc_agirlik", 0.4)
        satirlar, top_bedel, top_sure = [], {}, 0
        for l in range(1, t["maks"] + 1):
            r = min(50, t.get("min_lab", 1) + t.get("adim", 2) * (l - 1))
            bedel = {}
            for s in t.get("bedel", varsayilan):
                if r < s.get("baslangic", 1):
                    continue
                bedel[s["kaynak"]] = egri.yuvarla_bedel(
                    s["olcek"] * t.get("bedel_olcek", 1.0) * self.e(s.get("egri", "reaktor_bedel"), r))
            sure = egri.yuvarla_sure(sure_olcek * t.get("sure_olcek", 1.0) * self.e("reaktor_sure", r))
            for k, x in bedel.items():
                top_bedel[k] = top_bedel.get(k, 0) + x
            top_sure += sure
            etki = t.get("seviye_basi")
            satirlar.append({"seviye": l, "lab": r, "bedel": bedel, "sure": sure,
                             "guc": int(round(guc_agirlik * self.e("guc", r))),
                             "etki": None if etki is None else round(etki * l, 2),
                             "kumulatif_bedel": dict(top_bedel), "kumulatif_sure": top_sure})
        v8n = v8.arastirma_dugumu(self.katalog, t["id"])
        v8s = None
        if v8n:
            v8s = [dict(seviye=l, **v8.arastirma_seviye(self.katalog, v8n, l)) for l in range(1, v8n["maxLevel"] + 1)]
        return {"satirlar": satirlar, "toplam_bedel": top_bedel, "toplam_sure": top_sure, "v8": v8s}

    # --- kahramanlar --------------------------------------------------------
    def kahramanlar(self):
        return self.v.get("kahramanlar")

    def kahraman_stat(self, h, seviye, yildiz):
        k = self.kahramanlar()
        nad = {n["id"]: n for n in k["nadirlikler"]}[h["nadirlik"]]
        sinif = {s["id"]: s for s in k["siniflar"]}[h["sinif"]]
        f = k.get("stat_formulu", {"seviye": 0.08, "yildiz": 0.15})
        carpan = (1 + f["seviye"] * (seviye - 1)) * (1 + f["yildiz"] * (yildiz - 1))
        out = {s: int(round(nad["taban"][s] * sinif["carpan"][s] * carpan)) for s in ("atk", "def", "hp")}
        out["guc"] = int(round((out["atk"] + out["def"] + out["hp"] / 10) * 10))
        return out

    def kahraman_egitim(self, h, seviye):
        """Hedef eğitim maliyeti (seviye → seviye+1): Gıda + Biyokütle, reaktör eğrisine bağlı × nadirlik çarpanı."""
        k = self.kahramanlar()
        nad = {n["id"]: n for n in k["nadirlikler"]}[h["nadirlik"]]
        e = k.get("egitim", {"Food": 0.03, "Biomass": 0.01})
        return {kay: egri.yuvarla_bedel(o * nad.get("egitim_carpani", 1.0) * self.e("reaktor_bedel", seviye))
                for kay, o in e.items()}
