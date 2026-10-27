"""Çapalı eğriler ve yuvarlama kuralları (egriler.json'daki notla aynı)."""
import math


def deger(capalar, x):
    """Çapalar arasında log-doğrusal enterpolasyon; uçlarda ilk/son aralığın büyümesi sürer.

    Bir aralıkta sıfır ya da negatif değer varsa o aralık doğrusal okunur.
    """
    pts = sorted((float(a), float(b)) for a, b in capalar)
    if len(pts) == 1:
        return pts[0][1]
    if x <= pts[0][0]:
        a, b = pts[0], pts[1]
    elif x >= pts[-1][0]:
        a, b = pts[-2], pts[-1]
    else:
        a, b = pts[0], pts[1]
        for p, q in zip(pts, pts[1:]):
            if p[0] <= x <= q[0]:
                a, b = p, q
                break
    (xa, ya), (xb, yb) = a, b
    t = (x - xa) / (xb - xa)
    if ya <= 0 or yb <= 0:
        return ya + (yb - ya) * t
    return ya * (yb / ya) ** t


def anlamli(v, basamak=3):
    if v <= 0:
        return 0
    us = int(math.floor(math.log10(v)))
    return int(round(v, -us + basamak - 1))


def yuvarla_bedel(v):
    if v <= 0:
        return 0
    if v < 20:
        return max(1, int(round(v)))
    if v < 1000:
        return max(5, 5 * int(round(v / 5)))
    return anlamli(v)


def yuvarla_sure(t):
    if t <= 0:
        return 0
    if t < 120:
        return max(1, int(round(t)))
    if t < 7200:
        return 60 * int(round(t / 60))
    if t < 86400:
        return 300 * int(round(t / 300))
    return 900 * int(round(t / 900))


def yuvarla_uretim(v):
    if v >= 100:
        return int(round(v))
    return round(v, 1)
