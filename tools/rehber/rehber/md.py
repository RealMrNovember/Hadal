"""Markdown yardımcıları ve işaret motoru.

Blok:   <!-- URET:ad -->  …üretilen içerik…  <!-- /URET:ad -->
Değer:  <!--v:yol|bicim-->değer<!--/v-->   (bicim isteğe bağlı: sayi, sure, yuzde, gun, kisa, para, ham)

Yol 'veri.<dosya>.<anahtar>…' ham veriyi, 'hesap.<…>' hesaplanmış değerleri okur. Listelerde eleman
numarayla (0'dan) ya da 'id' alanıyla seçilir. İşaretlerin dışındaki metne asla dokunulmaz.
"""
import re

from . import bicim

BLOK = re.compile(r"(<!-- URET:(?P<ad>[^\s>]+) -->)(?P<ic>.*?)(<!-- /URET:(?P=ad) -->)", re.S)
DEGER = re.compile(r"(<!--v:(?P<yol>[^|>]+?)(?:\|(?P<bicim>\w+))?-->)(?P<ic>.*?)(<!--/v-->)", re.S)


class IsaretHatasi(Exception):
    pass


def tablo(basliklar, satirlar, hizala=None):
    """Markdown tablosu; hizala: her sütun için 'l' ya da 'r'."""
    if not satirlar:
        return "_Kayıt yok._"
    hizala = hizala or ["l"] * len(basliklar)
    ayrac = ["---:" if h == "r" else "---" for h in hizala]
    out = ["| " + " | ".join(basliklar) + " |", "| " + " | ".join(ayrac) + " |"]
    for s in satirlar:
        out.append("| " + " | ".join(_hucre(x) for x in s) + " |")
    return "\n".join(out)


def _hucre(x):
    if isinstance(x, str):
        return x.replace("|", "\\|").replace("\n", " ")
    return bicim.bicimle(x)


def yol_coz(kok, yol):
    dugum = kok
    for parca in yol.split("."):
        if isinstance(dugum, dict):
            if parca in dugum:
                dugum = dugum[parca]
            elif parca.isdigit() and int(parca) in dugum:
                dugum = dugum[int(parca)]
            else:
                raise IsaretHatasi(f"'{yol}': '{parca}' bulunamadı")
        elif isinstance(dugum, list):
            if parca.lstrip("-").isdigit():
                dugum = dugum[int(parca)]
            else:
                bulunan = [x for x in dugum if isinstance(x, dict) and x.get("id") == parca]
                if not bulunan:
                    raise IsaretHatasi(f"'{yol}': listede id='{parca}' yok")
                dugum = bulunan[0]
        else:
            raise IsaretHatasi(f"'{yol}': '{parca}' bir değerin içine inemiyor")
    return dugum


def genel_tablo(kok, yol):
    """URET:tablo:<yol> — bir nesne listesini tabloya döker.

    Sütunlar şu sırayla aranır: düğüm {'sutunlar', 'satirlar'} biçimindeyse 'sutunlar'; değilse üst
    düğümdeki '<anahtar>_sutunlar'; o da yoksa ilk satırın anahtarları. Sütun tanımı
    [alan, başlık] ya da [alan, başlık, biçim].
    """
    dugum = yol_coz(kok, yol)
    sutunlar = None
    if isinstance(dugum, dict) and "satirlar" in dugum:
        sutunlar, satirlar = dugum.get("sutunlar"), dugum["satirlar"]
    else:
        satirlar = dugum
        ust, _, anahtar = yol.rpartition(".")
        try:
            ust_dugum = yol_coz(kok, ust) if ust else kok
            if isinstance(ust_dugum, dict):
                sutunlar = ust_dugum.get(anahtar + "_sutunlar")
        except IsaretHatasi:
            pass
    if not isinstance(satirlar, list):
        raise IsaretHatasi(f"'{yol}' bir liste değil")
    if not satirlar:
        return "_Kayıt yok._"
    if not sutunlar:
        sutunlar = [[k, k] for k in satirlar[0].keys() if not k.startswith("_")]
    basliklar = [s[1] for s in sutunlar]
    hizala = []
    tablo_satirlari = []
    for satir in satirlar:
        hucreler = []
        for s in sutunlar:
            deger = satir.get(s[0]) if isinstance(satir, dict) else None
            tur = s[2] if len(s) > 2 else None
            hucreler.append(bicim.bicimle(deger, tur) if deger is not None else "—")
        tablo_satirlari.append(hucreler)
    for i, s in enumerate(sutunlar):
        sayisal = all(isinstance(r.get(s[0]), (int, float)) and not isinstance(r.get(s[0]), bool)
                      for r in satirlar if isinstance(r, dict) and r.get(s[0]) is not None)
        hizala.append("r" if sayisal else "l")
    return tablo(basliklar, tablo_satirlari, hizala)


def isle(metin, blok_uret, deger_kok):
    """Bir Markdown metnindeki tüm işaretleri yeniden üretir; (yeni_metin, hatalar) döner."""
    hatalar = []

    def blok(m):
        ad = m.group("ad")
        try:
            icerik = blok_uret(ad)
        except Exception as h:  # hatayı rapora yaz, eski içeriği koru
            hatalar.append(f"URET:{ad}: {h}")
            return m.group(0)
        return f"{m.group(1)}\n{icerik.strip()}\n{m.group(4)}"

    def deger(m):
        try:
            d = yol_coz(deger_kok, m.group("yol").strip())
            return f"{m.group(1)}{bicim.bicimle(d, m.group('bicim'))}{m.group(5)}"
        except Exception as h:
            hatalar.append(f"v:{m.group('yol')}: {h}")
            return m.group(0)

    yeni = BLOK.sub(blok, metin)
    yeni = DEGER.sub(deger, yeni)
    return yeni, hatalar
