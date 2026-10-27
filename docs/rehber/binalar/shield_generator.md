<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Kalkan Jeneratörü

`shield_generator` · Askeri · 🟡

> Üssün çevresinde basınç ve saldırı kalkanı oluşturan alan jeneratörü (duvar).

**İşlev.** Üs dayanıklılığı ve savunma bonusları; doğa olaylarına direnç. Reaktör 7'den itibaren her reaktör seviyesinin önkoşuludur.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 5 |
| Adet | 1 |
| Halka | 3–12 |
| En yüksek seviye | 50 |
| Tüketim (saatlik) | Sv 1: 60 EN → Sv 50: 10.800 EN |
| Toplam maliyet (1 → en yüksek) | 60.073.255 Titanyum · 15.024.840 Enerji · 2.570 Süperalaşım |
| Toplam ham süre | 191 g 10 sa 36 dk |
| Toplam güç | 115.250 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Hurda Kalkan Bobini | Altıgen kaide üstünde derme bobin, açıkta kablolar; küre sönük, hale sarsak döner. |
| 10–19 | Perçin | Perçinli Kalkan Jeneratörü | Pilon perçinli, üç yayıcı cıvatalı; küre düzenli nabız atar. |
| 20–29 | Basınç | Basınç Kalkanı Pilonu | Pilon basınç gövdesine geçer ve uzar; yayıcıların uçlarında basınç küreleri. |
| 30–39 | Alaşım | Alaşım Alan Kulesi | Süperalaşım alan kulesi, iki hale, tepede sonar halkası. Camgöbeği-mor. |
| 40–49 | Biyomekanik | Yaşayan Zar Pilonu | Pilonu yarı saydam, canlı bir zar sarar; zar kalkan ışığını süzer. Turkuaz. |
| 50 | Rezonans | Rezonans Kalkanı | Zar üssün kalkanına ince bir ışık ağı yansıtır ve Çekirdek'le aynı ritimde nefes alır. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Enerji | Süperalaşım | Süre | Güç | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 5 | 25 | — | — | 1 sn | 15 | 1 sn |
| 2 | 5 | 40 | — | — | 2 sn | 20 | 3 sn |
| 3 | 5 | 70 | — | — | 7 sn | 26 | 10 sn |
| 4 | 5 | 140 | — | — | 14 sn | 34 | 24 sn |
| 5 | 5 | 280 | 70 | — | 27 sn | 45 | 51 sn |
| 6 | 6 | 415 | 105 | — | 54 sn | 60 | 105 sn |
| 7 | 7 | 615 | 155 | — | 108 sn | 79 | 3 dk 33 sn |
| 8 | 8 | 910 | 230 | — | 4 dk | 104 | 7 dk 33 sn |
| 9 | 9 | 1.350 | 335 | — | 7 dk | 137 | 14 dk 33 sn |
| 10 | 10 | 2.000 | 500 | — | 14 dk | 180 | 28 dk 33 sn |
| 11 | 11 | 2.860 | 715 | — | 22 dk | 208 | 50 dk 33 sn |
| 12 | 12 | 4.100 | 1.020 | — | 36 dk | 239 | 1 sa 26 dk |
| 13 | 13 | 5.860 | 1.470 | — | 52 dk | 276 | 2 sa 18 dk |
| 14 | 14 | 8.390 | 2.100 | — | 1 sa 15 dk | 319 | 3 sa 33 dk |
| 15 | 15 | 12.000 | 3.000 | — | 1 sa 48 dk | 367 | 5 sa 21 dk |
| 16 | 16 | 16.100 | 4.020 | — | 2 sa 20 dk | 424 | 7 sa 41 dk |
| 17 | 17 | 21.600 | 5.390 | — | 3 sa | 489 | 10 sa 41 dk |
| 18 | 18 | 28.900 | 7.230 | — | 3 sa 50 dk | 564 | 14 sa 31 dk |
| 19 | 19 | 38.800 | 9.700 | — | 4 sa 55 dk | 650 | 19 sa 26 dk |
| 20 | 20 | 52.000 | 13.000 | — | 6 sa 20 dk | 750 | 1 g 1 sa 46 dk |
| 21 | 21 | 65.700 | 16.400 | — | 8 sa 5 dk | 831 | 1 g 9 sa 51 dk |
| 22 | 22 | 83.100 | 20.800 | — | 10 sa 20 dk | 921 | 1 g 20 sa 11 dk |
| 23 | 23 | 105.000 | 26.300 | — | 13 sa 10 dk | 1.021 | 2 g 9 sa 21 dk |
| 24 | 24 | 133.000 | 33.200 | — | 16 sa 55 dk | 1.132 | 3 g 2 sa 16 dk |
| 25 | 25 | 168.000 | 42.000 | — | 21 sa 35 dk | 1.255 | 3 g 23 sa 51 dk |
| 26 | 26 | 204.000 | 50.900 | — | 1 g 1 sa 30 dk | 1.391 | 5 g 1 sa 21 dk |
| 27 | 27 | 247.000 | 61.700 | — | 1 g 6 sa | 1.542 | 6 g 7 sa 21 dk |
| 28 | 28 | 299.000 | 74.800 | — | 1 g 11 sa 15 dk | 1.709 | 7 g 18 sa 36 dk |
| 29 | 29 | 363.000 | 90.700 | — | 1 g 17 sa 15 dk | 1.895 | 9 g 11 sa 51 dk |
| 30 | 30 | 440.000 | 110.000 | — | 2 g 30 dk | 2.100 | 11 g 12 sa 21 dk |
| 31 | 31 | 519.000 | 130.000 | 30 | 2 g 8 sa 30 dk | 2.266 | 13 g 20 sa 51 dk |
| 32 | 32 | 611.000 | 153.000 | 35 | 2 g 17 sa 30 dk | 2.446 | 16 g 14 sa 21 dk |
| 33 | 33 | 720.000 | 180.000 | 40 | 3 g 4 sa | 2.639 | 19 g 18 sa 21 dk |
| 34 | 34 | 849.000 | 212.000 | 45 | 3 g 16 sa 15 dk | 2.849 | 23 g 10 sa 36 dk |
| 35 | 35 | 1.000.000 | 250.000 | 50 | 4 g 6 sa 30 dk | 3.074 | 27 g 17 sa 6 dk |
| 36 | 36 | 1.150.000 | 287.000 | 60 | 4 g 19 sa 15 dk | 3.318 | 32 g 12 sa 21 dk |
| 37 | 37 | 1.320.000 | 330.000 | 65 | 5 g 9 sa 30 dk | 3.580 | 37 g 21 sa 51 dk |
| 38 | 38 | 1.520.000 | 379.000 | 75 | 6 g 1 sa 30 dk | 3.864 | 43 g 23 sa 21 dk |
| 39 | 39 | 1.740.000 | 435.000 | 85 | 6 g 19 sa 30 dk | 4.170 | 50 g 18 sa 51 dk |
| 40 | 40 | 2.000.000 | 500.000 | 100 | 7 g 15 sa 30 dk | 4.500 | 58 g 10 sa 21 dk |
| 41 | 41 | 2.300.000 | 574.000 | 110 | 8 g 10 sa 45 dk | 4.781 | 66 g 21 sa 6 dk |
| 42 | 42 | 2.640.000 | 660.000 | 125 | 9 g 8 sa 15 dk | 5.080 | 76 g 5 sa 21 dk |
| 43 | 43 | 3.030.000 | 758.000 | 140 | 10 g 7 sa 45 dk | 5.397 | 86 g 13 sa 6 dk |
| 44 | 44 | 3.480.000 | 871.000 | 160 | 11 g 9 sa 45 dk | 5.735 | 97 g 22 sa 51 dk |
| 45 | 45 | 4.000.000 | 1.000.000 | 180 | 12 g 14 sa 30 dk | 6.093 | 110 g 13 sa 21 dk |
| 46 | 46 | 4.590.000 | 1.150.000 | 200 | 13 g 16 sa | 6.474 | 124 g 5 sa 21 dk |
| 47 | 47 | 5.280.000 | 1.320.000 | 225 | 14 g 19 sa 45 dk | 6.878 | 139 g 1 sa 6 dk |
| 48 | 48 | 6.060.000 | 1.520.000 | 250 | 16 g 1 sa 45 dk | 7.308 | 155 g 2 sa 51 dk |
| 49 | 49 | 6.960.000 | 1.740.000 | 280 | 17 g 10 sa 15 dk | 7.765 | 172 g 13 sa 6 dk |
| 50 | 50 | 8.000.000 | 2.000.000 | 315 | 18 g 21 sa 30 dk | 8.250 | 191 g 10 sa 36 dk |

## Bugünkü oyunla karşılaştırma (catalog v8)

Kodda bugün: en fazla 1 adet, Reaktör 5 ile açılır, 50 seviye; 1 → 50 toplam ham süre 1 g 15 sa 59 dk. Hedef tasarımda aynı yol 191 g 10 sa 36 dk sürer.

| Sv | v8 Titanyum | v8 Enerji | v8 süre | Hedef Titanyum | Hedef süre |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 220 | 120 | 60 sn | 25 | 1 sn |
| 5 | 311 | 169 | 94 sn | 280 | 27 sn |
| 10 | 478 | 261 | 2 dk 46 sn | 2.000 | 14 dk |
| 15 | 735 | 401 | 4 dk 53 sn | 12.000 | 1 sa 48 dk |
| 20 | 1.131 | 617 | 8 dk 37 sn | 52.000 | 6 sa 20 dk |
| 25 | 1.740 | 949 | 15 dk 11 sn | 168.000 | 21 sa 35 dk |
| 30 | 2.678 | 1.461 | 26 dk 45 sn | 440.000 | 2 g 30 dk |
| 35 | 4.120 | 2.247 | 47 dk 9 sn | 1.000.000 | 4 g 6 sa 30 dk |
| 40 | 6.340 | 3.458 | 1 sa 23 dk | 2.000.000 | 7 g 15 sa 30 dk |
| 45 | 9.754 | 5.320 | 2 sa 26 dk | 4.000.000 | 12 g 14 sa 30 dk |
| 50 | 15.008 | 8.186 | 4 sa 18 dk | 8.000.000 | 18 g 21 sa 30 dk |


← [03 · Binalar](../03_binalar.md)
