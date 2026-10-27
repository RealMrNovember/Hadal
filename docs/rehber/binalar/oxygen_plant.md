<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Oksijen Tesisi

`oxygen_plant` · Üretim · 🟡

> Deniz suyunu elektrolizle hidrojen ve oksijene ayırır.

**İşlev.** Oksijen üretir. Takviye (Boost) ile 30 dakika %30 fazla üretim. Mürettebat ataması ile ek verim.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 1, 2. örnek Reaktör 1, 3. örnek Reaktör 3, 4. örnek Reaktör 6; ek örnekler Reaktör 12, 20 |
| Adet | 4 (+2 araştırmayla) |
| Halka | 1–12 |
| En yüksek seviye | 50 |
| Üretim (saatlik) | Sv 1: 240 O₂ → Sv 50: 43.200 O₂ |
| Tüketim (saatlik) | Sv 1: 30 EN → Sv 50: 5.400 EN |
| Toplam maliyet (1 → en yüksek) | 18.019.304 Titanyum · 4.506.325 Biyokütle |
| Toplam ham süre | 63 g 19 sa 24 dk |
| Toplam güç | 46.102 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Yama Elektroliz Tankı | Üç uyumsuz tank, biri eski bir konvoy yakıt tankı; açıkta hortumlar, bantlı göstergeler. Pervane tekleyerek döner, kehribar ışık. |
| 10–19 | Perçin | Perçinli Oksijen İstasyonu | Tanklar eşitlenir; perçinli bantlar, flanşlı besleme boruları, ızgaralı pervane gövdesi. Camgöbeği gösterge şeritleri, düzenli dönüş. |
| 20–29 | Basınç | Basınçlı Elektroliz Kulesi | Tanklar basınç kürelerine döner, ortada yükselen bir elektroliz sütunu; konik gözetleme pencereleri. Sütunda kabarcıklar yükselir. |
| 30–39 | Alaşım | Alaşım Ayrıştırma Kulesi | Süperalaşım elektroliz kulesi iki kat boyunda; yanda dinleme halkası ve eşanjör kanatları. Camgöbeği-mor ışık, ince kabarcık perdesi. |
| 40–49 | Biyomekanik | Kadim Solungaç | Kule solungaç yarıklı bir kitin zarla sarılır; zar nefes alır gibi kabarır, kabarcıklar yarıklardan süzülür. Turkuaz damarlar, bazalt kaide. |
| 50 | Rezonans | Soluyan Kule | Solungaçlar tepeye kadar uzanır ve Çekirdek nabzıyla açılıp kapanır; her atışta ışıklı bir kabarcık halkası yükselir. Beyaz-camgöbeği. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süre | Güç | Üretim/sa | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | 1 | 7 | — | 1 sn | 6 | 240 O₂ | 1 sn |
| 2 | 2 | 12 | — | 1 sn | 8 | 293 O₂ | 2 sn |
| 3 | 3 | 20 | — | 2 sn | 10 | 357 O₂ | 4 sn |
| 4 | 4 | 40 | — | 4 sn | 14 | 436 O₂ | 8 sn |
| 5 | 5 | 85 | 20 | 9 sn | 18 | 532 O₂ | 17 sn |
| 6 | 6 | 125 | 30 | 18 sn | 24 | 649 O₂ | 35 sn |
| 7 | 7 | 185 | 45 | 36 sn | 31 | 792 O₂ | 71 sn |
| 8 | 8 | 275 | 70 | 72 sn | 41 | 967 O₂ | 2 dk 23 sn |
| 9 | 9 | 405 | 100 | 2 dk | 55 | 1.180 O₂ | 4 dk 23 sn |
| 10 | 10 | 600 | 150 | 4 dk | 72 | 1.440 O₂ | 8 dk 23 sn |
| 11 | 11 | 860 | 215 | 7 dk | 83 | 1.632 O₂ | 15 dk 23 sn |
| 12 | 12 | 1.230 | 305 | 12 dk | 96 | 1.850 O₂ | 27 dk 23 sn |
| 13 | 13 | 1.760 | 440 | 17 dk | 110 | 2.097 O₂ | 44 dk 23 sn |
| 14 | 14 | 2.520 | 630 | 25 dk | 127 | 2.377 O₂ | 1 sa 9 dk |
| 15 | 15 | 3.600 | 900 | 36 dk | 147 | 2.694 O₂ | 1 sa 45 dk |
| 16 | 16 | 4.830 | 1.210 | 46 dk | 170 | 3.054 O₂ | 2 sa 31 dk |
| 17 | 17 | 6.470 | 1.620 | 59 dk | 196 | 3.461 O₂ | 3 sa 30 dk |
| 18 | 18 | 8.680 | 2.170 | 1 sa 16 dk | 226 | 3.923 O₂ | 4 sa 46 dk |
| 19 | 19 | 11.600 | 2.910 | 1 sa 38 dk | 260 | 4.447 O₂ | 6 sa 24 dk |
| 20 | 20 | 15.600 | 3.900 | 2 sa 5 dk | 300 | 5.040 O₂ | 8 sa 29 dk |
| 21 | 21 | 19.700 | 4.930 | 2 sa 40 dk | 333 | 5.474 O₂ | 11 sa 9 dk |
| 22 | 22 | 24.900 | 6.230 | 3 sa 25 dk | 369 | 5.946 O₂ | 14 sa 34 dk |
| 23 | 23 | 31.500 | 7.880 | 4 sa 25 dk | 409 | 6.459 O₂ | 18 sa 59 dk |
| 24 | 24 | 39.900 | 9.970 | 5 sa 40 dk | 453 | 7.015 O₂ | 1 g 39 dk |
| 25 | 25 | 50.400 | 12.600 | 7 sa 10 dk | 502 | 7.620 O₂ | 1 g 7 sa 49 dk |
| 26 | 26 | 61.100 | 15.300 | 8 sa 30 dk | 556 | 8.276 O₂ | 1 g 16 sa 19 dk |
| 27 | 27 | 74.100 | 18.500 | 10 sa | 617 | 8.990 O₂ | 2 g 2 sa 19 dk |
| 28 | 28 | 89.800 | 22.500 | 11 sa 45 dk | 684 | 9.764 O₂ | 2 g 14 sa 4 dk |
| 29 | 29 | 109.000 | 27.200 | 13 sa 45 dk | 758 | 10.606 O₂ | 3 g 3 sa 49 dk |
| 30 | 30 | 132.000 | 33.000 | 16 sa 10 dk | 840 | 11.520 O₂ | 3 g 19 sa 59 dk |
| 31 | 31 | 156.000 | 38.900 | 18 sa 50 dk | 907 | 12.422 O₂ | 4 g 14 sa 49 dk |
| 32 | 32 | 183.000 | 45.800 | 21 sa 50 dk | 978 | 13.394 O₂ | 5 g 12 sa 39 dk |
| 33 | 33 | 216.000 | 54.000 | 1 g 1 sa 15 dk | 1.056 | 14.443 O₂ | 6 g 13 sa 54 dk |
| 34 | 34 | 255.000 | 63.600 | 1 g 5 sa 30 dk | 1.139 | 15.574 O₂ | 7 g 19 sa 24 dk |
| 35 | 35 | 300.000 | 75.000 | 1 g 10 sa 15 dk | 1.230 | 16.793 O₂ | 9 g 5 sa 39 dk |
| 36 | 36 | 345.000 | 86.200 | 1 g 14 sa 30 dk | 1.327 | 18.108 O₂ | 10 g 20 sa 9 dk |
| 37 | 37 | 396.000 | 99.000 | 1 g 19 sa 15 dk | 1.432 | 19.526 O₂ | 12 g 15 sa 24 dk |
| 38 | 38 | 455.000 | 114.000 | 2 g 30 dk | 1.546 | 21.054 O₂ | 14 g 15 sa 54 dk |
| 39 | 39 | 522.000 | 131.000 | 2 g 6 sa 30 dk | 1.668 | 22.703 O₂ | 16 g 22 sa 24 dk |
| 40 | 40 | 600.000 | 150.000 | 2 g 13 sa 15 dk | 1.800 | 24.480 O₂ | 19 g 11 sa 39 dk |
| 41 | 41 | 689.000 | 172.000 | 2 g 19 sa 30 dk | 1.912 | 25.911 O₂ | 22 g 7 sa 9 dk |
| 42 | 42 | 792.000 | 198.000 | 3 g 2 sa 45 dk | 2.032 | 27.425 O₂ | 25 g 9 sa 54 dk |
| 43 | 43 | 909.000 | 227.000 | 3 g 10 sa 30 dk | 2.159 | 29.028 O₂ | 28 g 20 sa 24 dk |
| 44 | 44 | 1.040.000 | 261.000 | 3 g 19 sa 15 dk | 2.294 | 30.724 O₂ | 32 g 15 sa 39 dk |
| 45 | 45 | 1.200.000 | 300.000 | 4 g 4 sa 45 dk | 2.437 | 32.520 O₂ | 36 g 20 sa 24 dk |
| 46 | 46 | 1.380.000 | 345.000 | 4 g 13 sa 15 dk | 2.590 | 34.420 O₂ | 41 g 9 sa 39 dk |
| 47 | 47 | 1.580.000 | 396.000 | 4 g 22 sa 30 dk | 2.751 | 36.432 O₂ | 46 g 8 sa 9 dk |
| 48 | 48 | 1.820.000 | 455.000 | 5 g 8 sa 30 dk | 2.923 | 38.561 O₂ | 51 g 16 sa 39 dk |
| 49 | 49 | 2.090.000 | 522.000 | 5 g 19 sa 30 dk | 3.106 | 40.815 O₂ | 57 g 12 sa 9 dk |
| 50 | 50 | 2.400.000 | 600.000 | 6 g 7 sa 15 dk | 3.300 | 43.200 O₂ | 63 g 19 sa 24 dk |

## Bugünkü oyunla karşılaştırma (catalog v8)

Kodda bugün: en fazla 4 adet, Reaktör 1 ile açılır, 50 seviye; 1 → 50 toplam ham süre 6 sa 40 dk. Hedef tasarımda aynı yol 63 g 19 sa 24 dk sürer.

| Sv | v8 Titanyum | v8 süre | Hedef Titanyum | Hedef süre |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 60 | 10 sn | 7 | 1 sn |
| 5 | 85 | 16 sn | 85 | 9 sn |
| 10 | 130 | 28 sn | 600 | 4 dk |
| 15 | 201 | 49 sn | 3.600 | 36 dk |
| 20 | 308 | 86 sn | 15.600 | 2 sa 5 dk |
| 25 | 475 | 2 dk 32 sn | 50.400 | 7 sa 10 dk |
| 30 | 730 | 4 dk 27 sn | 132.000 | 16 sa 10 dk |
| 35 | 1.124 | 7 dk 51 sn | 300.000 | 1 g 10 sa 15 dk |
| 40 | 1.729 | 13 dk 51 sn | 600.000 | 2 g 13 sa 15 dk |
| 45 | 2.660 | 24 dk 24 sn | 1.200.000 | 4 g 4 sa 45 dk |
| 50 | 4.093 | 43 dk | 2.400.000 | 6 g 7 sa 15 dk |


← [03 · Binalar](../03_binalar.md)
