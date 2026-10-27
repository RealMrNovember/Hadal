"""Türkçe sayı, süre ve yüzde biçimleri: 1.250.000 · 2,5 · %4 · 3 g 4 sa."""


def _binlik(n):
    s = f"{abs(int(n)):,}".replace(",", ".")
    return ("−" if n < 0 else "") + s


def sayi(x):
    if x is None or x == "":
        return "—"
    if isinstance(x, bool):
        return "evet" if x else "hayır"
    if isinstance(x, int):
        return _binlik(x)
    if isinstance(x, float):
        if x.is_integer():
            return _binlik(int(x))
        tam = int(abs(x))
        kesir = f"{abs(x) - tam:.2f}"[2:].rstrip("0")
        if kesir == "":
            return _binlik(int(round(x)))
        return ("−" if x < 0 else "") + _binlik(tam) + "," + kesir
    return str(x)


def yuzde(x):
    """Türkçede yüzde işareti sayıdan önce gelir: %4 · −%3."""
    if x is None:
        return "—"
    if x < 0:
        return f"−%{sayi(abs(x))}"
    return f"%{sayi(x)}"


def sure(sn):
    if sn is None:
        return "—"
    sn = int(round(sn))
    if sn <= 0:
        return "—"
    if sn < 120:
        return f"{sn} sn"
    if sn < 3600:
        d, s = divmod(sn, 60)
        return f"{d} dk" + (f" {s} sn" if s else "")
    if sn < 86400:
        s, kalan = divmod(sn, 3600)
        d = kalan // 60
        return f"{s} sa" + (f" {d} dk" if d else "")
    g, kalan = divmod(sn, 86400)
    s, kalan = divmod(kalan, 3600)
    d = kalan // 60
    parca = [f"{g} g"]
    if s:
        parca.append(f"{s} sa")
    if d:
        parca.append(f"{d} dk")
    return " ".join(parca)


def gun(g):
    """Ondalık gün → okunur; 1 günün altı saat/dakika olarak."""
    if g is None:
        return "—"
    if g < 1:
        return sure(g * 86400)
    return f"{sayi(round(g, 1))}. gün"


def kisa(x):
    """Özet tablolar için: 1,1 M · 42 B (bin) · 950."""
    if x is None:
        return "—"
    if abs(x) >= 1_000_000:
        return f"{sayi(round(x / 1_000_000, 2))} M"
    if abs(x) >= 10_000:
        return f"{sayi(round(x / 1000, 1))} B"
    return sayi(x)


def para(x):
    return f"{sayi(x)} $"


def bicimle(deger, tur=None):
    if tur == "sure":
        return sure(deger)
    if tur == "yuzde":
        return yuzde(deger)
    if tur == "gun":
        return gun(deger)
    if tur == "kisa":
        return kisa(deger)
    if tur == "para":
        return para(deger)
    if tur == "ham":
        return str(deger)
    if tur == "tam":
        return sayi(int(round(deger)))
    if isinstance(deger, list):
        return ", ".join(bicimle(d) for d in deger)
    if isinstance(deger, bool):
        return "evet" if deger else "hayır"
    if isinstance(deger, (int, float)):
        return sayi(deger)
    if deger is None:
        return "—"
    return str(deger)
