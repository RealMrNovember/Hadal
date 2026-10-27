<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Hidroponik Çiftlik

`hydroponic_farm` · Üretim · 🟡

> LED ışık altında besin çözeltisinde ekin yetiştirir; biyokütleyi gübre olarak kullanır.

**İşlev.** Gıda üretir, biyokütle ve enerji tüketir.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 3, 2. örnek Reaktör 7, 3. örnek Reaktör 12 |
| Adet | 3 |
| Halka | 1–12 |
| En yüksek seviye | 50 |
| Üretim (saatlik) | Sv 1: 360 GD → Sv 50: 64.800 GD |
| Tüketim (saatlik) | Sv 1: 30 BK · 40 EN → Sv 50: 5.400 BK · 7.200 EN |
| Toplam maliyet (1 → en yüksek) | 22.528.654 Titanyum · 7.510.285 Biyokütle |
| Toplam ham süre | 76 g 13 sa 44 dk |
| Toplam güç | 46.102 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Hurda Sera Rafları | Eski kargo kafesinden tek sıra raf; birkaç bitki bantlı LED altında. Kehribar lamba. |
| 10–19 | Perçin | Perçinli Hidroponik Çiftlik | Üç cam sütun, perçinli kapaklar, flanşlı besleme boruları; boyalı, şeritli rezervuar. Sabit yeşil ışık. |
| 20–29 | Basınç | Basınçlı Sera Sütunları | Sütunlar basınç camına geçer ve uzar, rezervuar basınç küresi olur; raflar dört kata çıkar. |
| 30–39 | Alaşım | Alaşım Dikey Tarla | Süperalaşım çerçeveli dikey tarla: beş sütun yay biçiminde, tepede eşanjör. Yeşil-mor büyüme ışığı. |
| 40–49 | Biyomekanik | Kadim Kök Ağı | Sütunların dibinden kitin kökler bazalt kaideye yayılır ve sütunları birbirine bağlar. Turkuaz damarlı kök ağı. |
| 50 | Rezonans | Işıyan Hasat Kulesi | Kökler tepede birleşip tek bir kule olur; her nabızda ışık kökten sütunlara yükselir. Yeşil-beyaz. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süre | Güç | Üretim/sa | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | 3 | 9 | 3 | 1 sn | 6 | 360 GD | 1 sn |
| 2 | 3 | 15 | 5 | 1 sn | 8 | 439 GD | 2 sn |
| 3 | 3 | 25 | 9 | 3 sn | 10 | 536 GD | 5 sn |
| 4 | 4 | 50 | 18 | 5 sn | 14 | 654 GD | 10 sn |
| 5 | 5 | 105 | 35 | 11 sn | 18 | 798 GD | 21 sn |
| 6 | 6 | 155 | 50 | 22 sn | 24 | 974 GD | 43 sn |
| 7 | 7 | 230 | 75 | 43 sn | 31 | 1.189 GD | 86 sn |
| 8 | 8 | 340 | 115 | 86 sn | 41 | 1.451 GD | 2 dk 52 sn |
| 9 | 9 | 505 | 170 | 3 dk | 55 | 1.770 GD | 5 dk 52 sn |
| 10 | 10 | 750 | 250 | 5 dk | 72 | 2.160 GD | 10 dk 52 sn |
| 11 | 11 | 1.070 | 360 | 9 dk | 83 | 2.448 GD | 19 dk 52 sn |
| 12 | 12 | 1.540 | 510 | 14 dk | 96 | 2.775 GD | 33 dk 52 sn |
| 13 | 13 | 2.200 | 735 | 21 dk | 110 | 3.145 GD | 54 dk 52 sn |
| 14 | 14 | 3.140 | 1.050 | 30 dk | 127 | 3.565 GD | 1 sa 24 dk |
| 15 | 15 | 4.500 | 1.500 | 43 dk | 147 | 4.041 GD | 2 sa 7 dk |
| 16 | 16 | 6.030 | 2.010 | 56 dk | 170 | 4.580 GD | 3 sa 3 dk |
| 17 | 17 | 8.090 | 2.700 | 1 sa 11 dk | 196 | 5.192 GD | 4 sa 14 dk |
| 18 | 18 | 10.800 | 3.620 | 1 sa 32 dk | 226 | 5.884 GD | 5 sa 46 dk |
| 19 | 19 | 14.500 | 4.850 | 1 sa 58 dk | 260 | 6.670 GD | 7 sa 44 dk |
| 20 | 20 | 19.500 | 6.500 | 2 sa 30 dk | 300 | 7.560 GD | 10 sa 14 dk |
| 21 | 21 | 24.700 | 8.220 | 3 sa 15 dk | 333 | 8.212 GD | 13 sa 29 dk |
| 22 | 22 | 31.200 | 10.400 | 4 sa 10 dk | 369 | 8.919 GD | 17 sa 39 dk |
| 23 | 23 | 39.400 | 13.100 | 5 sa 15 dk | 409 | 9.688 GD | 22 sa 54 dk |
| 24 | 24 | 49.800 | 16.600 | 6 sa 45 dk | 453 | 10.523 GD | 1 g 5 sa 39 dk |
| 25 | 25 | 63.000 | 21.000 | 8 sa 40 dk | 502 | 11.430 GD | 1 g 14 sa 19 dk |
| 26 | 26 | 76.400 | 25.500 | 10 sa 10 dk | 556 | 12.415 GD | 2 g 29 dk |
| 27 | 27 | 92.600 | 30.900 | 11 sa 55 dk | 617 | 13.485 GD | 2 g 12 sa 24 dk |
| 28 | 28 | 112.000 | 37.400 | 14 sa 5 dk | 684 | 14.647 GD | 3 g 2 sa 29 dk |
| 29 | 29 | 136.000 | 45.400 | 16 sa 30 dk | 758 | 15.909 GD | 3 g 18 sa 59 dk |
| 30 | 30 | 165.000 | 55.000 | 19 sa 25 dk | 840 | 17.280 GD | 4 g 14 sa 24 dk |
| 31 | 31 | 194.000 | 64.800 | 22 sa 35 dk | 907 | 18.633 GD | 5 g 12 sa 59 dk |
| 32 | 32 | 229.000 | 76.400 | 1 g 2 sa 15 dk | 978 | 20.092 GD | 6 g 15 sa 14 dk |
| 33 | 33 | 270.000 | 90.000 | 1 g 6 sa 30 dk | 1.056 | 21.665 GD | 7 g 21 sa 44 dk |
| 34 | 34 | 318.000 | 106.000 | 1 g 11 sa 15 dk | 1.139 | 23.361 GD | 9 g 8 sa 59 dk |
| 35 | 35 | 375.000 | 125.000 | 1 g 17 sa | 1.230 | 25.190 GD | 11 g 1 sa 59 dk |
| 36 | 36 | 431.000 | 144.000 | 1 g 22 sa | 1.327 | 27.162 GD | 12 g 23 sa 59 dk |
| 37 | 37 | 495.000 | 165.000 | 2 g 3 sa 45 dk | 1.432 | 29.288 GD | 15 g 3 sa 44 dk |
| 38 | 38 | 568.000 | 189.000 | 2 g 10 sa 15 dk | 1.546 | 31.581 GD | 17 g 13 sa 59 dk |
| 39 | 39 | 653.000 | 218.000 | 2 g 17 sa 15 dk | 1.668 | 34.054 GD | 20 g 7 sa 14 dk |
| 40 | 40 | 750.000 | 250.000 | 3 g 1 sa 30 dk | 1.800 | 36.720 GD | 23 g 8 sa 44 dk |
| 41 | 41 | 862.000 | 287.000 | 3 g 9 sa 15 dk | 1.912 | 38.866 GD | 26 g 17 sa 59 dk |
| 42 | 42 | 990.000 | 330.000 | 3 g 17 sa 45 dk | 2.032 | 41.137 GD | 30 g 11 sa 44 dk |
| 43 | 43 | 1.140.000 | 379.000 | 4 g 3 sa | 2.159 | 43.542 GD | 34 g 14 sa 44 dk |
| 44 | 44 | 1.310.000 | 435.000 | 4 g 13 sa 30 dk | 2.294 | 46.086 GD | 39 g 4 sa 14 dk |
| 45 | 45 | 1.500.000 | 500.000 | 5 g 1 sa | 2.437 | 48.780 GD | 44 g 5 sa 14 dk |
| 46 | 46 | 1.720.000 | 574.000 | 5 g 11 sa 15 dk | 2.590 | 51.630 GD | 49 g 16 sa 29 dk |
| 47 | 47 | 1.980.000 | 660.000 | 5 g 22 sa 15 dk | 2.751 | 54.648 GD | 55 g 14 sa 44 dk |
| 48 | 48 | 2.270.000 | 758.000 | 6 g 10 sa 15 dk | 2.923 | 57.842 GD | 62 g 59 dk |
| 49 | 49 | 2.610.000 | 871.000 | 6 g 23 sa 15 dk | 3.106 | 61.222 GD | 69 g 14 dk |
| 50 | 50 | 3.000.000 | 1.000.000 | 7 g 13 sa 30 dk | 3.300 | 64.800 GD | 76 g 13 sa 44 dk |

## Bugünkü oyunla karşılaştırma (catalog v8)

Kodda bugün: en fazla 3 adet, Reaktör 3 ile açılır, 50 seviye; 1 → 50 toplam ham süre 1 g 2 sa 40 dk. Hedef tasarımda aynı yol 76 g 13 sa 44 dk sürer.

| Sv | v8 Titanyum | v8 Biyokütle | v8 süre | Hedef Titanyum | Hedef süre |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 140 | 40 | 40 sn | 9 | 1 sn |
| 5 | 198 | 56 | 63 sn | 105 | 11 sn |
| 10 | 304 | 87 | 111 sn | 750 | 5 dk |
| 15 | 468 | 134 | 3 dk 15 sn | 4.500 | 43 dk |
| 20 | 720 | 206 | 5 dk 45 sn | 19.500 | 2 sa 30 dk |
| 25 | 1.108 | 316 | 10 dk 7 sn | 63.000 | 8 sa 40 dk |
| 30 | 1.704 | 487 | 17 dk 50 sn | 165.000 | 19 sa 25 dk |
| 35 | 2.622 | 749 | 31 dk 26 sn | 375.000 | 1 g 17 sa |
| 40 | 4.034 | 1.153 | 55 dk 23 sn | 750.000 | 3 g 1 sa 30 dk |
| 45 | 6.207 | 1.773 | 1 sa 37 dk | 1.500.000 | 5 g 1 sa |
| 50 | 9.551 | 2.729 | 2 sa 52 dk | 3.000.000 | 7 g 13 sa 30 dk |


← [03 · Binalar](../03_binalar.md)
