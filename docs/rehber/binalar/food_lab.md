<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Gıda Laboratuvarı

`food_lab` · Üretim · 🟡

> Baca suyundan beslenen kemosentetik bakteri kültürlerini yenilebilir proteine çevirir.

**İşlev.** Gıda üretir. Takviye ve mürettebat ataması uygulanır.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 1, 2. örnek Reaktör 2, 3. örnek Reaktör 5, 4. örnek Reaktör 9 |
| Adet | 4 |
| Halka | 1–12 |
| En yüksek seviye | 50 |
| Üretim (saatlik) | Sv 1: 200 GD → Sv 50: 36.000 GD |
| Tüketim (saatlik) | Sv 1: 30 EN → Sv 50: 5.400 EN |
| Toplam maliyet (1 → en yüksek) | 15.024.909 Titanyum · 4.506.325 Biyokütle |
| Toplam ham süre | 63 g 19 sa 24 dk |
| Toplam güç | 46.102 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Geçici Kültür Odası | Farklı tonlarda kurtarılmış akrilik levhalardan yarım silindir sera; tepsilerde cılız kültürler. Tek kehribar lamba, zayıf yeşil ışık. |
| 10–19 | Perçin | Perçinli Gıda Laboratuvarı | Sera perçinli kaburgalarla düzenlenir, raflar ikiye çıkar; tepsiler borularla baca suyuna bağlanır. Sabit yeşil büyüme ışığı. |
| 20–29 | Basınç | Basınçlı Kültür Hücresi | Kalın akrilik tonoz, iki ucunda basınç küresi kilit odası; üç katlı kültür rafları. Yeşil ışık camı içeriden boyar. |
| 30–39 | Alaşım | Alaşım Biyoreaktör | Tonozun üstüne süperalaşım biyoreaktör kuleleri yükselir; eşanjörlü baca suyu hattı. Protein tanklarında yeşil-mor ışık. |
| 40–49 | Biyomekanik | Yaşayan Mayalık | Tonozu kitin kaburgalar taşır; kültürler duvarlara yayılmış canlı bir mayalık gibi. Turkuaz damarlar yavaşça nefes alır. |
| 50 | Rezonans | Bereket Nabzı | Seranın tepesini ışık saçan bir zar örter; her nabızda kültürler içeriden parlar, yeşil-beyaz bir dalga tonoz boyunca akar. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süre | Güç | Üretim/sa | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | 1 | 6 | — | 1 sn | 6 | 200 GD | 1 sn |
| 2 | 2 | 10 | — | 1 sn | 8 | 244 GD | 2 sn |
| 3 | 3 | 18 | — | 2 sn | 10 | 298 GD | 4 sn |
| 4 | 4 | 35 | — | 4 sn | 14 | 363 GD | 8 sn |
| 5 | 5 | 70 | 20 | 9 sn | 18 | 443 GD | 17 sn |
| 6 | 6 | 105 | 30 | 18 sn | 24 | 541 GD | 35 sn |
| 7 | 7 | 155 | 45 | 36 sn | 31 | 660 GD | 71 sn |
| 8 | 8 | 230 | 70 | 72 sn | 41 | 806 GD | 2 dk 23 sn |
| 9 | 9 | 335 | 100 | 2 dk | 55 | 983 GD | 4 dk 23 sn |
| 10 | 10 | 500 | 150 | 4 dk | 72 | 1.200 GD | 8 dk 23 sn |
| 11 | 11 | 715 | 215 | 7 dk | 83 | 1.360 GD | 15 dk 23 sn |
| 12 | 12 | 1.020 | 305 | 12 dk | 96 | 1.542 GD | 27 dk 23 sn |
| 13 | 13 | 1.470 | 440 | 17 dk | 110 | 1.747 GD | 44 dk 23 sn |
| 14 | 14 | 2.100 | 630 | 25 dk | 127 | 1.981 GD | 1 sa 9 dk |
| 15 | 15 | 3.000 | 900 | 36 dk | 147 | 2.245 GD | 1 sa 45 dk |
| 16 | 16 | 4.020 | 1.210 | 46 dk | 170 | 2.545 GD | 2 sa 31 dk |
| 17 | 17 | 5.390 | 1.620 | 59 dk | 196 | 2.884 GD | 3 sa 30 dk |
| 18 | 18 | 7.230 | 2.170 | 1 sa 16 dk | 226 | 3.269 GD | 4 sa 46 dk |
| 19 | 19 | 9.700 | 2.910 | 1 sa 38 dk | 260 | 3.705 GD | 6 sa 24 dk |
| 20 | 20 | 13.000 | 3.900 | 2 sa 5 dk | 300 | 4.200 GD | 8 sa 29 dk |
| 21 | 21 | 16.400 | 4.930 | 2 sa 40 dk | 333 | 4.562 GD | 11 sa 9 dk |
| 22 | 22 | 20.800 | 6.230 | 3 sa 25 dk | 369 | 4.955 GD | 14 sa 34 dk |
| 23 | 23 | 26.300 | 7.880 | 4 sa 25 dk | 409 | 5.382 GD | 18 sa 59 dk |
| 24 | 24 | 33.200 | 9.970 | 5 sa 40 dk | 453 | 5.846 GD | 1 g 39 dk |
| 25 | 25 | 42.000 | 12.600 | 7 sa 10 dk | 502 | 6.350 GD | 1 g 7 sa 49 dk |
| 26 | 26 | 50.900 | 15.300 | 8 sa 30 dk | 556 | 6.897 GD | 1 g 16 sa 19 dk |
| 27 | 27 | 61.700 | 18.500 | 10 sa | 617 | 7.491 GD | 2 g 2 sa 19 dk |
| 28 | 28 | 74.800 | 22.500 | 11 sa 45 dk | 684 | 8.137 GD | 2 g 14 sa 4 dk |
| 29 | 29 | 90.700 | 27.200 | 13 sa 45 dk | 758 | 8.838 GD | 3 g 3 sa 49 dk |
| 30 | 30 | 110.000 | 33.000 | 16 sa 10 dk | 840 | 9.600 GD | 3 g 19 sa 59 dk |
| 31 | 31 | 130.000 | 38.900 | 18 sa 50 dk | 907 | 10.352 GD | 4 g 14 sa 49 dk |
| 32 | 32 | 153.000 | 45.800 | 21 sa 50 dk | 978 | 11.162 GD | 5 g 12 sa 39 dk |
| 33 | 33 | 180.000 | 54.000 | 1 g 1 sa 15 dk | 1.056 | 12.036 GD | 6 g 13 sa 54 dk |
| 34 | 34 | 212.000 | 63.600 | 1 g 5 sa 30 dk | 1.139 | 12.978 GD | 7 g 19 sa 24 dk |
| 35 | 35 | 250.000 | 75.000 | 1 g 10 sa 15 dk | 1.230 | 13.994 GD | 9 g 5 sa 39 dk |
| 36 | 36 | 287.000 | 86.200 | 1 g 14 sa 30 dk | 1.327 | 15.090 GD | 10 g 20 sa 9 dk |
| 37 | 37 | 330.000 | 99.000 | 1 g 19 sa 15 dk | 1.432 | 16.271 GD | 12 g 15 sa 24 dk |
| 38 | 38 | 379.000 | 114.000 | 2 g 30 dk | 1.546 | 17.545 GD | 14 g 15 sa 54 dk |
| 39 | 39 | 435.000 | 131.000 | 2 g 6 sa 30 dk | 1.668 | 18.919 GD | 16 g 22 sa 24 dk |
| 40 | 40 | 500.000 | 150.000 | 2 g 13 sa 15 dk | 1.800 | 20.400 GD | 19 g 11 sa 39 dk |
| 41 | 41 | 574.000 | 172.000 | 2 g 19 sa 30 dk | 1.912 | 21.592 GD | 22 g 7 sa 9 dk |
| 42 | 42 | 660.000 | 198.000 | 3 g 2 sa 45 dk | 2.032 | 22.854 GD | 25 g 9 sa 54 dk |
| 43 | 43 | 758.000 | 227.000 | 3 g 10 sa 30 dk | 2.159 | 24.190 GD | 28 g 20 sa 24 dk |
| 44 | 44 | 871.000 | 261.000 | 3 g 19 sa 15 dk | 2.294 | 25.603 GD | 32 g 15 sa 39 dk |
| 45 | 45 | 1.000.000 | 300.000 | 4 g 4 sa 45 dk | 2.437 | 27.100 GD | 36 g 20 sa 24 dk |
| 46 | 46 | 1.150.000 | 345.000 | 4 g 13 sa 15 dk | 2.590 | 28.684 GD | 41 g 9 sa 39 dk |
| 47 | 47 | 1.320.000 | 396.000 | 4 g 22 sa 30 dk | 2.751 | 30.360 GD | 46 g 8 sa 9 dk |
| 48 | 48 | 1.520.000 | 455.000 | 5 g 8 sa 30 dk | 2.923 | 32.134 GD | 51 g 16 sa 39 dk |
| 49 | 49 | 1.740.000 | 522.000 | 5 g 19 sa 30 dk | 3.106 | 34.012 GD | 57 g 12 sa 9 dk |
| 50 | 50 | 2.000.000 | 600.000 | 6 g 7 sa 15 dk | 3.300 | 36.000 GD | 63 g 19 sa 24 dk |

## Bugünkü oyunla karşılaştırma (catalog v8)

Kodda bugün: en fazla 4 adet, Reaktör 1 ile açılır, 50 seviye; 1 → 50 toplam ham süre 6 sa 40 dk. Hedef tasarımda aynı yol 63 g 19 sa 24 dk sürer.

| Sv | v8 Titanyum | v8 süre | Hedef Titanyum | Hedef süre |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 50 | 10 sn | 6 | 1 sn |
| 5 | 71 | 16 sn | 70 | 9 sn |
| 10 | 109 | 28 sn | 500 | 4 dk |
| 15 | 167 | 49 sn | 3.000 | 36 dk |
| 20 | 257 | 86 sn | 13.000 | 2 sa 5 dk |
| 25 | 396 | 2 dk 32 sn | 42.000 | 7 sa 10 dk |
| 30 | 609 | 4 dk 27 sn | 110.000 | 16 sa 10 dk |
| 35 | 936 | 7 dk 51 sn | 250.000 | 1 g 10 sa 15 dk |
| 40 | 1.441 | 13 dk 51 sn | 500.000 | 2 g 13 sa 15 dk |
| 45 | 2.217 | 24 dk 24 sn | 1.000.000 | 4 g 4 sa 45 dk |
| 50 | 3.411 | 43 dk | 2.000.000 | 6 g 7 sa 15 dk |


← [03 · Binalar](../03_binalar.md)
