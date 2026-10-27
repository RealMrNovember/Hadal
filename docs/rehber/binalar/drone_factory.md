<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Dron Fabrikası

`drone_factory` · Üretim · 🟡

> Kurtarma dronları enkazdan, ilmenit kumundan ve demir-mangan kabuklarından titanyum toplar.

**İşlev.** Titanyum üretir. Kurtarma görevleri: 15 dk, 1 sa, 4 sa; seviye başına +%10 ödül, uzun görevlerde Hadalit şansı.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 2, 2. örnek Reaktör 4, 3. örnek Reaktör 7; ek örnekler Reaktör 14, 22 |
| Adet | 3 (+2 araştırmayla) |
| Halka | 2–12 |
| En yüksek seviye | 50 |
| Üretim (saatlik) | Sv 1: 150 Ti → Sv 50: 27.000 Ti |
| Tüketim (saatlik) | Sv 1: 35 EN → Sv 50: 6.300 EN |
| Toplam maliyet (1 → en yüksek) | 22.528.654 Titanyum · 7.510.277 Biyokütle |
| Toplam ham süre | 76 g 13 sa 44 dk |
| Toplam güç | 46.102 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Hurda Dron Atölyesi | Kutu hangar, kapısı söküp takılmış bir konteyner; derme vinç kolu, tek dron kehribar yanar. |
| 10–19 | Perçin | Perçinli Dron Fabrikası | Hangar perçinli panellerle düzenlenir, çatıya ikinci vinç gelir; tehlike şeritli kapı. İki dron devriye atar. |
| 20–29 | Basınç | Basınçlı Dron Hangarı | Hangarın arkasına basınç küresi montaj odası, cephede konik pencere sırası; dronlar üçe çıkar. |
| 30–39 | Alaşım | Alaşım Montaj Hattı | Süperalaşım montaj hattı iki kata çıkar, tepede nodül ayıklama haznesi; dronlar sıralı yörüngede döner. |
| 40–49 | Biyomekanik | Kaynaşık Dron Kovanı | Hangar kabuğu kitin bir kovan gibi kemerlenir; dronlar kovanın gözeneklerinden çıkar. Turkuaz ışık. |
| 50 | Rezonans | Sürü Korosu | Kovanın çevresinde dron sürüsü Çekirdek nabzıyla aynı anda ışıldar ve yön değiştirir; hepsi tek bir ses gibi uğuldar. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süre | Güç | Üretim/sa | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | 2 | 9 | — | 1 sn | 6 | 150 Ti | 1 sn |
| 2 | 2 | 15 | — | 1 sn | 8 | 183 Ti | 2 sn |
| 3 | 3 | 25 | 9 | 3 sn | 10 | 223 Ti | 5 sn |
| 4 | 4 | 50 | 18 | 5 sn | 14 | 273 Ti | 10 sn |
| 5 | 5 | 105 | 35 | 11 sn | 18 | 333 Ti | 21 sn |
| 6 | 6 | 155 | 50 | 22 sn | 24 | 406 Ti | 43 sn |
| 7 | 7 | 230 | 75 | 43 sn | 31 | 495 Ti | 86 sn |
| 8 | 8 | 340 | 115 | 86 sn | 41 | 604 Ti | 2 dk 52 sn |
| 9 | 9 | 505 | 170 | 3 dk | 55 | 738 Ti | 5 dk 52 sn |
| 10 | 10 | 750 | 250 | 5 dk | 72 | 900 Ti | 10 dk 52 sn |
| 11 | 11 | 1.070 | 360 | 9 dk | 83 | 1.020 Ti | 19 dk 52 sn |
| 12 | 12 | 1.540 | 510 | 14 dk | 96 | 1.156 Ti | 33 dk 52 sn |
| 13 | 13 | 2.200 | 735 | 21 dk | 110 | 1.311 Ti | 54 dk 52 sn |
| 14 | 14 | 3.140 | 1.050 | 30 dk | 127 | 1.485 Ti | 1 sa 24 dk |
| 15 | 15 | 4.500 | 1.500 | 43 dk | 147 | 1.684 Ti | 2 sa 7 dk |
| 16 | 16 | 6.030 | 2.010 | 56 dk | 170 | 1.908 Ti | 3 sa 3 dk |
| 17 | 17 | 8.090 | 2.700 | 1 sa 11 dk | 196 | 2.163 Ti | 4 sa 14 dk |
| 18 | 18 | 10.800 | 3.620 | 1 sa 32 dk | 226 | 2.452 Ti | 5 sa 46 dk |
| 19 | 19 | 14.500 | 4.850 | 1 sa 58 dk | 260 | 2.779 Ti | 7 sa 44 dk |
| 20 | 20 | 19.500 | 6.500 | 2 sa 30 dk | 300 | 3.150 Ti | 10 sa 14 dk |
| 21 | 21 | 24.700 | 8.220 | 3 sa 15 dk | 333 | 3.421 Ti | 13 sa 29 dk |
| 22 | 22 | 31.200 | 10.400 | 4 sa 10 dk | 369 | 3.716 Ti | 17 sa 39 dk |
| 23 | 23 | 39.400 | 13.100 | 5 sa 15 dk | 409 | 4.037 Ti | 22 sa 54 dk |
| 24 | 24 | 49.800 | 16.600 | 6 sa 45 dk | 453 | 4.384 Ti | 1 g 5 sa 39 dk |
| 25 | 25 | 63.000 | 21.000 | 8 sa 40 dk | 502 | 4.762 Ti | 1 g 14 sa 19 dk |
| 26 | 26 | 76.400 | 25.500 | 10 sa 10 dk | 556 | 5.173 Ti | 2 g 29 dk |
| 27 | 27 | 92.600 | 30.900 | 11 sa 55 dk | 617 | 5.619 Ti | 2 g 12 sa 24 dk |
| 28 | 28 | 112.000 | 37.400 | 14 sa 5 dk | 684 | 6.103 Ti | 3 g 2 sa 29 dk |
| 29 | 29 | 136.000 | 45.400 | 16 sa 30 dk | 758 | 6.629 Ti | 3 g 18 sa 59 dk |
| 30 | 30 | 165.000 | 55.000 | 19 sa 25 dk | 840 | 7.200 Ti | 4 g 14 sa 24 dk |
| 31 | 31 | 194.000 | 64.800 | 22 sa 35 dk | 907 | 7.764 Ti | 5 g 12 sa 59 dk |
| 32 | 32 | 229.000 | 76.400 | 1 g 2 sa 15 dk | 978 | 8.372 Ti | 6 g 15 sa 14 dk |
| 33 | 33 | 270.000 | 90.000 | 1 g 6 sa 30 dk | 1.056 | 9.027 Ti | 7 g 21 sa 44 dk |
| 34 | 34 | 318.000 | 106.000 | 1 g 11 sa 15 dk | 1.139 | 9.734 Ti | 9 g 8 sa 59 dk |
| 35 | 35 | 375.000 | 125.000 | 1 g 17 sa | 1.230 | 10.496 Ti | 11 g 1 sa 59 dk |
| 36 | 36 | 431.000 | 144.000 | 1 g 22 sa | 1.327 | 11.317 Ti | 12 g 23 sa 59 dk |
| 37 | 37 | 495.000 | 165.000 | 2 g 3 sa 45 dk | 1.432 | 12.203 Ti | 15 g 3 sa 44 dk |
| 38 | 38 | 568.000 | 189.000 | 2 g 10 sa 15 dk | 1.546 | 13.159 Ti | 17 g 13 sa 59 dk |
| 39 | 39 | 653.000 | 218.000 | 2 g 17 sa 15 dk | 1.668 | 14.189 Ti | 20 g 7 sa 14 dk |
| 40 | 40 | 750.000 | 250.000 | 3 g 1 sa 30 dk | 1.800 | 15.300 Ti | 23 g 8 sa 44 dk |
| 41 | 41 | 862.000 | 287.000 | 3 g 9 sa 15 dk | 1.912 | 16.194 Ti | 26 g 17 sa 59 dk |
| 42 | 42 | 990.000 | 330.000 | 3 g 17 sa 45 dk | 2.032 | 17.141 Ti | 30 g 11 sa 44 dk |
| 43 | 43 | 1.140.000 | 379.000 | 4 g 3 sa | 2.159 | 18.142 Ti | 34 g 14 sa 44 dk |
| 44 | 44 | 1.310.000 | 435.000 | 4 g 13 sa 30 dk | 2.294 | 19.203 Ti | 39 g 4 sa 14 dk |
| 45 | 45 | 1.500.000 | 500.000 | 5 g 1 sa | 2.437 | 20.325 Ti | 44 g 5 sa 14 dk |
| 46 | 46 | 1.720.000 | 574.000 | 5 g 11 sa 15 dk | 2.590 | 21.513 Ti | 49 g 16 sa 29 dk |
| 47 | 47 | 1.980.000 | 660.000 | 5 g 22 sa 15 dk | 2.751 | 22.770 Ti | 55 g 14 sa 44 dk |
| 48 | 48 | 2.270.000 | 758.000 | 6 g 10 sa 15 dk | 2.923 | 24.101 Ti | 62 g 59 dk |
| 49 | 49 | 2.610.000 | 871.000 | 6 g 23 sa 15 dk | 3.106 | 25.509 Ti | 69 g 14 dk |
| 50 | 50 | 3.000.000 | 1.000.000 | 7 g 13 sa 30 dk | 3.300 | 27.000 Ti | 76 g 13 sa 44 dk |

## Bugünkü oyunla karşılaştırma (catalog v8)

Kodda bugün: en fazla 3 adet, Reaktör 2 ile açılır, 50 seviye; 1 → 50 toplam ham süre 20 sa. Hedef tasarımda aynı yol 76 g 13 sa 44 dk sürer.

| Sv | v8 Titanyum | v8 Biyokütle | v8 süre | Hedef Titanyum | Hedef süre |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 120 | 20 | 30 sn | 9 | 1 sn |
| 5 | 169 | 28 | 47 sn | 105 | 11 sn |
| 10 | 261 | 43 | 83 sn | 750 | 5 dk |
| 15 | 401 | 67 | 2 dk 27 sn | 4.500 | 43 dk |
| 20 | 617 | 103 | 4 dk 18 sn | 19.500 | 2 sa 30 dk |
| 25 | 949 | 158 | 7 dk 35 sn | 63.000 | 8 sa 40 dk |
| 30 | 1.461 | 243 | 13 dk 22 sn | 165.000 | 19 sa 25 dk |
| 35 | 2.247 | 375 | 23 dk 34 sn | 375.000 | 1 g 17 sa |
| 40 | 3.458 | 576 | 41 dk 32 sn | 750.000 | 3 g 1 sa 30 dk |
| 45 | 5.320 | 887 | 1 sa 13 dk | 1.500.000 | 5 g 1 sa |
| 50 | 8.186 | 1.364 | 2 sa 9 dk | 3.000.000 | 7 g 13 sa 30 dk |


← [03 · Binalar](../03_binalar.md)
