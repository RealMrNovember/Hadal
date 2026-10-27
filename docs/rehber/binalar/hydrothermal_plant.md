<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Hidrotermal Santral

`hydrothermal_plant` · Üretim · ⬜

> Yakındaki hidrotermal bacanın ısısını elektriğe çeviren santral.

**İşlev.** Enerji üretir.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 6, 2. örnek Reaktör 12, 3. örnek Reaktör 20 |
| Adet | 3 |
| Halka | 4–12 |
| En yüksek seviye | 50 |
| Üretim (saatlik) | Sv 1: 500 EN → Sv 50: 90.000 EN |
| Toplam maliyet (1 → en yüksek) | 22.528.654 Titanyum |
| Toplam ham süre | 76 g 13 sa 44 dk |
| Toplam güç | 46.102 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Yama Isı Hattı | Bacaya uzatılmış yamalı bir ısı borusu ve küçük jeneratör kulübesi; borunun ağzında titreyen sıcak su. |
| 10–19 | Perçin | Perçinli Baca Santrali | Perçin kiti: boru hatları flanşlı, türbin kulübesi perçinli. |
| 20–29 | Basınç | Basınç Türbin Evi | İki küre türbin odası, bacanın dibinde halka takviyeli ısı toplayıcı. |
| 30–39 | Alaşım | Alaşım Isı Kulesi | Alaşım kiti: süperalaşım ısı kulesi, kanatlı eşanjörler, ısıdan kızaran kenarlar. |
| 40–49 | Biyomekanik | Kaynaşık Baca | Yapı bacanın mineral sütunuyla kaynaşır; kitin kaburgalar, sülfür kristalleri. Turkuaz. |
| 50 | Rezonans | Rezonans Bacası | Rezonans kiti: bacadan yükselen sıcak su nabızla ışıklı halkalar çizer. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Süre | Güç | Üretim/sa | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | 6 | 9 | 1 sn | 6 | 500 EN | 1 sn |
| 2 | 6 | 15 | 1 sn | 8 | 610 EN | 2 sn |
| 3 | 6 | 25 | 3 sn | 10 | 745 EN | 5 sn |
| 4 | 6 | 50 | 5 sn | 14 | 909 EN | 10 sn |
| 5 | 6 | 105 | 11 sn | 18 | 1.109 EN | 21 sn |
| 6 | 6 | 155 | 22 sn | 24 | 1.353 EN | 43 sn |
| 7 | 7 | 230 | 43 sn | 31 | 1.651 EN | 86 sn |
| 8 | 8 | 340 | 86 sn | 41 | 2.015 EN | 2 dk 52 sn |
| 9 | 9 | 505 | 3 dk | 55 | 2.458 EN | 5 dk 52 sn |
| 10 | 10 | 750 | 5 dk | 72 | 3.000 EN | 10 dk 52 sn |
| 11 | 11 | 1.070 | 9 dk | 83 | 3.400 EN | 19 dk 52 sn |
| 12 | 12 | 1.540 | 14 dk | 96 | 3.854 EN | 33 dk 52 sn |
| 13 | 13 | 2.200 | 21 dk | 110 | 4.369 EN | 54 dk 52 sn |
| 14 | 14 | 3.140 | 30 dk | 127 | 4.952 EN | 1 sa 24 dk |
| 15 | 15 | 4.500 | 43 dk | 147 | 5.612 EN | 2 sa 7 dk |
| 16 | 16 | 6.030 | 56 dk | 170 | 6.362 EN | 3 sa 3 dk |
| 17 | 17 | 8.090 | 1 sa 11 dk | 196 | 7.211 EN | 4 sa 14 dk |
| 18 | 18 | 10.800 | 1 sa 32 dk | 226 | 8.173 EN | 5 sa 46 dk |
| 19 | 19 | 14.500 | 1 sa 58 dk | 260 | 9.264 EN | 7 sa 44 dk |
| 20 | 20 | 19.500 | 2 sa 30 dk | 300 | 10.500 EN | 10 sa 14 dk |
| 21 | 21 | 24.700 | 3 sa 15 dk | 333 | 11.405 EN | 13 sa 29 dk |
| 22 | 22 | 31.200 | 4 sa 10 dk | 369 | 12.388 EN | 17 sa 39 dk |
| 23 | 23 | 39.400 | 5 sa 15 dk | 409 | 13.455 EN | 22 sa 54 dk |
| 24 | 24 | 49.800 | 6 sa 45 dk | 453 | 14.615 EN | 1 g 5 sa 39 dk |
| 25 | 25 | 63.000 | 8 sa 40 dk | 502 | 15.875 EN | 1 g 14 sa 19 dk |
| 26 | 26 | 76.400 | 10 sa 10 dk | 556 | 17.243 EN | 2 g 29 dk |
| 27 | 27 | 92.600 | 11 sa 55 dk | 617 | 18.729 EN | 2 g 12 sa 24 dk |
| 28 | 28 | 112.000 | 14 sa 5 dk | 684 | 20.343 EN | 3 g 2 sa 29 dk |
| 29 | 29 | 136.000 | 16 sa 30 dk | 758 | 22.096 EN | 3 g 18 sa 59 dk |
| 30 | 30 | 165.000 | 19 sa 25 dk | 840 | 24.000 EN | 4 g 14 sa 24 dk |
| 31 | 31 | 194.000 | 22 sa 35 dk | 907 | 25.879 EN | 5 g 12 sa 59 dk |
| 32 | 32 | 229.000 | 1 g 2 sa 15 dk | 978 | 27.905 EN | 6 g 15 sa 14 dk |
| 33 | 33 | 270.000 | 1 g 6 sa 30 dk | 1.056 | 30.090 EN | 7 g 21 sa 44 dk |
| 34 | 34 | 318.000 | 1 g 11 sa 15 dk | 1.139 | 32.446 EN | 9 g 8 sa 59 dk |
| 35 | 35 | 375.000 | 1 g 17 sa | 1.230 | 34.986 EN | 11 g 1 sa 59 dk |
| 36 | 36 | 431.000 | 1 g 22 sa | 1.327 | 37.725 EN | 12 g 23 sa 59 dk |
| 37 | 37 | 495.000 | 2 g 3 sa 45 dk | 1.432 | 40.678 EN | 15 g 3 sa 44 dk |
| 38 | 38 | 568.000 | 2 g 10 sa 15 dk | 1.546 | 43.863 EN | 17 g 13 sa 59 dk |
| 39 | 39 | 653.000 | 2 g 17 sa 15 dk | 1.668 | 47.297 EN | 20 g 7 sa 14 dk |
| 40 | 40 | 750.000 | 3 g 1 sa 30 dk | 1.800 | 51.000 EN | 23 g 8 sa 44 dk |
| 41 | 41 | 862.000 | 3 g 9 sa 15 dk | 1.912 | 53.981 EN | 26 g 17 sa 59 dk |
| 42 | 42 | 990.000 | 3 g 17 sa 45 dk | 2.032 | 57.135 EN | 30 g 11 sa 44 dk |
| 43 | 43 | 1.140.000 | 4 g 3 sa | 2.159 | 60.474 EN | 34 g 14 sa 44 dk |
| 44 | 44 | 1.310.000 | 4 g 13 sa 30 dk | 2.294 | 64.009 EN | 39 g 4 sa 14 dk |
| 45 | 45 | 1.500.000 | 5 g 1 sa | 2.437 | 67.750 EN | 44 g 5 sa 14 dk |
| 46 | 46 | 1.720.000 | 5 g 11 sa 15 dk | 2.590 | 71.709 EN | 49 g 16 sa 29 dk |
| 47 | 47 | 1.980.000 | 5 g 22 sa 15 dk | 2.751 | 75.900 EN | 55 g 14 sa 44 dk |
| 48 | 48 | 2.270.000 | 6 g 10 sa 15 dk | 2.923 | 80.336 EN | 62 g 59 dk |
| 49 | 49 | 2.610.000 | 6 g 23 sa 15 dk | 3.106 | 85.031 EN | 69 g 14 dk |
| 50 | 50 | 3.000.000 | 7 g 13 sa 30 dk | 3.300 | 90.000 EN | 76 g 13 sa 44 dk |


← [03 · Binalar](../03_binalar.md)
