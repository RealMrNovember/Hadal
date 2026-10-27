"""Tempo simülasyonu: reaktörün kritik yolunu (reaktör + önkoşul binaları) profil profil oynatır.

Model (02_tempo_ve_denge.md'de anlatılır):
- Her reaktör seviyesi L için önce önkoşul binaları L'nin istediği seviyeye çıkarılır, sonra reaktör inşa edilir.
- Bir önkoşul binası reaktör seviyesini geçemez; bu yüzden her önkoşulun SON adımı ancak reaktör L−1
  olduktan sonra yapılabilir. Daha alttaki "yetişme" adımları, reaktör inşa edilirken boştaki
  inşaatçıların biriktirdiği arka plan kapasitesinden karşılanır.
- Süre = ham / (1 + hız%) − ittifak yardımı − ücretsiz bitirme; hızlandırma stoğu reaktör inşaatına harcanır.
- Kaynak: gelirin 'kritik_pay' kadarı kritik yola ayrılır; yetmezse beklenir. Süperalaşım, R30'dan
  itibaren biriken günlük gelirle ödenir.
- Oyuncu çevrimdışıyken biten uzun inşaatlarda boşta bekleme eklenir.
"""
from . import egri

GUN = 86400.0


def basamak(capalar, x):
    """Basamak fonksiyonu: x'e eşit ya da küçük son çapanın değeri."""
    deger = capalar[0][1]
    for a, b in sorted(capalar):
        if x >= a:
            deger = b
    return deger


class Simulasyon:
    def __init__(self, model, profil, gelir):
        self.m = model
        self.p = profil
        self.g = gelir
        self.reaktor = model.binalar["core_reactor"]
        kur = model.ilerleme["kurallar"]
        self.ff_taban = kur.get("ucretsiz_bitirme_sn", 300)
        self.yardim = kur["yardim"]

    # --- profil okumaları -------------------------------------------------
    def insaatci(self, R):
        return int(basamak(self.p["insaatci"], R))

    def hiz(self, L):
        h = self.p["hiz_yuzde"]
        return min(h["tavan"], h["carpan"] * L) + h.get("ek", 0)

    def yardim_sayisi(self, L):
        y = self.yardim
        hub = max(0, L - 1)
        return self.p["yardim_orani"] * min(y["maks"], y["taban"] + hub // y["her_hub_seviyesi"])

    def ucretsiz_bitirme(self, L):
        # Mühendislik Bölmesi (Reaktör 4'te açılır) her 5 seviyede +1 dk, en fazla +10 dk.
        ek = 60 * min(10, max(0, L - 1) // 5) if L >= 4 else 0
        return self.ff_taban + ek

    def etkin(self, ham, L):
        d = ham / (1 + self.hiz(L) / 100.0)
        d -= min(d, self.yardim_sayisi(L) * max(0.01 * d, self.yardim["min_sn"]))
        return max(0.0, d - self.ucretsiz_bitirme(L))

    def bekleme(self, d):
        b = self.p["bekleme"]
        return min(b["oran"] * d, b["tavan_sa"] * 3600.0) if d > 3600 else 0.0

    def gelir_sn(self, R):
        ti = egri.deger(self.g["titanyum_saat"], R) / 3600.0 * self.p["gelir_carpani"] * self.p["kritik_pay"]
        return {"Titanium": ti, "Biomass": ti * self.g["biyokutle_orani"]}

    def alasim_gun(self, gun):
        return egri.deger(self.g["alasim_gun"], max(0.0, gun)) * self.p["alasim_carpani"] * self.p["alasim_kritik_pay"]

    def hizlandirma_sn_gun(self, L):
        return egri.deger(self.p["hizlandirma_saat_gun"], L) * 3600.0

    # --- çalıştır ------------------------------------------------------------
    def calistir(self):
        m = self.m
        t = 0.0
        R = 1
        seviye = {}
        havuz = 0.0
        cuzdan = {k: float(v) for k, v in self.g.get("baslangic_stok", {}).items()}
        alasim = 0.0
        alasim_t0 = None
        stok = 0.0
        satirlar = []
        toplam = {"bekleme_kaynak": 0.0, "bekleme_alasim": 0.0, "bosta": 0.0, "hizlandirma": 0.0}

        def ilerlet(dt):
            nonlocal t, alasim, stok
            if dt <= 0:
                return
            oran = self.gelir_sn(R)
            for k, x in oran.items():
                cuzdan[k] = cuzdan.get(k, 0.0) + x * dt
            if alasim_t0 is not None:
                adim = 0.0
                while adim < dt:
                    parca = min(GUN / 4, dt - adim)
                    gun = (t + adim - alasim_t0) / GUN
                    alasim += self.alasim_gun(gun) * parca / GUN
                    adim += parca
            stok += self.hizlandirma_sn_gun(R) * dt / GUN
            t += dt

        for L in range(2, 51):
            baslangic = t
            nb = self.insaatci(R)
            adimlar = []
            for bid, hedef in m.reaktor_on_kosul(L):
                b = m.binalar[bid]
                for s in range(seviye.get(bid, 0) + 1, hedef + 1):
                    son = m.gerekli_reaktor(b, s) >= R
                    adimlar.append({"bina": bid, "seviye": s, "sure": self.etkin(m.sure(b, s), L),
                                    "bedel": m.bedel(b, s), "son": son})
                seviye[bid] = max(seviye.get(bid, 0), hedef)

            yetisme = sum(a["sure"] for a in adimlar if not a["son"])
            havuzdan = min(havuz, yetisme)
            havuz -= havuzdan
            kalan = yetisme - havuzdan
            sonlar = sorted((a["sure"] for a in adimlar if a["son"]), reverse=True)
            yuk = [0.0] * max(1, nb)
            for d in sonlar:
                yuk[yuk.index(min(yuk))] += d
            onkosul = max(max(yuk), (sum(yuk) + kalan) / max(1, nb))
            onkosul += self.bekleme(max(sonlar) if sonlar else 0.0)
            ilerlet(onkosul)

            # Kaynak kapısı
            r_bedel = m.bedel(self.reaktor, L)
            gerek = dict(r_bedel)
            for a in adimlar:
                for k, x in a["bedel"].items():
                    gerek[k] = gerek.get(k, 0) + x
            pay = egri.deger(self.g.get("gorev_odulu_payi", [[1, 0], [2, 0]]), L)
            for k in ("Titanium", "Biomass"):
                cuzdan[k] = cuzdan.get(k, 0.0) + max(0.0, pay) * r_bedel.get(k, 0)
            bek_kaynak = 0.0
            oran = self.gelir_sn(R)
            for k in ("Titanium", "Biomass"):
                eksik = gerek.get(k, 0) - cuzdan.get(k, 0.0)
                if eksik > 0 and oran.get(k):
                    bek_kaynak = max(bek_kaynak, eksik / oran[k])
            ilerlet(bek_kaynak)
            for k in ("Titanium", "Biomass"):
                cuzdan[k] = cuzdan.get(k, 0.0) - gerek.get(k, 0)

            bek_alasim = 0.0
            gerek_alasim = gerek.get("Alloy", 0)
            if gerek_alasim:
                while alasim < gerek_alasim and bek_alasim < 400 * GUN:
                    ilerlet(GUN / 4)
                    bek_alasim += GUN / 4
                alasim -= gerek_alasim

            # Reaktör inşaatı
            d = self.etkin(m.sure(self.reaktor, L), L)
            kullan = min(stok, d)
            d -= kullan
            stok -= kullan
            ilerlet(d)
            havuz += d * max(0, nb - 1)
            bos = self.bekleme(d)
            ilerlet(bos)
            R = L
            if R >= 30 and alasim_t0 is None:
                alasim_t0 = t

            toplam["bekleme_kaynak"] += bek_kaynak
            toplam["bekleme_alasim"] += bek_alasim
            toplam["bosta"] += bos
            toplam["hizlandirma"] += kullan
            satirlar.append({
                "seviye": L, "gun": t / GUN, "seviye_suresi": (t - baslangic) / GUN,
                "onkosul_sn": onkosul, "reaktor_sn": d, "hizlandirma_sn": kullan,
                "bekleme_kaynak_sn": bek_kaynak, "bekleme_alasim_sn": bek_alasim, "bosta_sn": bos,
                "insaatci": nb, "hiz": self.hiz(L),
            })
        return {"profil": self.p["id"], "ad": self.p["ad"], "satirlar": satirlar,
                "gun_50": satirlar[-1]["gun"], "toplam": {k: v / GUN for k, v in toplam.items()},
                "kalan_havuz_gun": havuz / GUN}


def hepsi(model):
    f = model.v["f2p_profilleri"]
    return {p["id"]: Simulasyon(model, p, f["gelir"]).calistir() for p in f["profiller"]}
