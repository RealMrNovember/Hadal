<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Alaşım Dökümhanesi

`alloy_foundry` · Üretim · ⬜

> Nodüllerden ayrıştırılan nikel ve kobaltı süperalaşıma döken yüksek ısılı dökümhane.

**İşlev.** Süperalaşım üretir.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 30, 2. örnek Reaktör 40 |
| Adet | 2 |
| Halka | 4–12 |
| En yüksek seviye | 50 |
| Üretim (saatlik) | Sv 1: 0,5 SA → Sv 50: 2,5 SA |
| Tüketim (saatlik) | Sv 1: 100 EN → Sv 50: 18.000 EN |
| Toplam maliyet (1 → en yüksek) | 45.063.493 Titanyum · 15.024.909 Biyokütle · 2.420 Süperalaşım |
| Toplam ham süre | 106 g 7 sa 49 dk |
| Toplam güç | 76.834 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Deneme Ocağı | Bazalt bloklar arasında küçük bir pota, akkor ağız, açıkta borular; ilk deneme dökümü. |
| 10–19 | Perçin | Perçinli Döküm Ocağı | Perçin kiti: perçinli davlumbaz, nodül besleme rampası. |
| 20–29 | Basınç | Basınç Potası | Küre ocak gövdesi; konik gözetleme penceresinden akkor metal görünür. |
| 30–39 | Alaşım | Alaşım Dökümhanesi | Alaşım kiti: süperalaşım kuleler, döküm kalıpları. Turuncu-mor ısı. |
| 40–49 | Biyomekanik | Kaynaşık Pota | Ocak bazalt yastıklara gömülür, kitin kollar potayı taşır. Turkuaz-akkor. |
| 50 | Rezonans | Rezonans Dökümhanesi | Rezonans kiti: döküm akışı nabızla parlar; mor-beyaz alaşım ışığı. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süperalaşım | Süre | Güç | Üretim/sa | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | 30 | 18 | 6 | — | 1 sn | 10 | 0,5 SA | 1 sn |
| 2 | 30 | 30 | 10 | — | 1 sn | 13 | 0,5 SA | 2 sn |
| 3 | 30 | 55 | 18 | — | 4 sn | 17 | 0,5 SA | 6 sn |
| 4 | 30 | 105 | 35 | — | 8 sn | 23 | 0,5 SA | 14 sn |
| 5 | 30 | 210 | 70 | — | 15 sn | 30 | 0,5 SA | 29 sn |
| 6 | 30 | 310 | 105 | — | 30 sn | 40 | 0,5 SA | 59 sn |
| 7 | 30 | 460 | 155 | — | 60 sn | 52 | 0,5 SA | 119 sn |
| 8 | 30 | 685 | 230 | — | 2 dk | 69 | 0,5 SA | 3 dk 59 sn |
| 9 | 30 | 1.010 | 335 | — | 4 dk | 91 | 0,5 SA | 7 dk 59 sn |
| 10 | 30 | 1.500 | 500 | — | 8 dk | 120 | 0,5 SA | 15 dk 59 sn |
| 11 | 30 | 2.150 | 715 | — | 12 dk | 138 | 0,5 SA | 27 dk 59 sn |
| 12 | 30 | 3.070 | 1.020 | — | 20 dk | 160 | 0,5 SA | 47 dk 59 sn |
| 13 | 30 | 4.400 | 1.470 | — | 29 dk | 184 | 0,5 SA | 1 sa 16 dk |
| 14 | 30 | 6.290 | 2.100 | — | 42 dk | 212 | 0,5 SA | 1 sa 58 dk |
| 15 | 30 | 9.000 | 3.000 | — | 1 sa | 245 | 0,5 SA | 2 sa 58 dk |
| 16 | 30 | 12.100 | 4.020 | — | 1 sa 17 dk | 283 | 0,5 SA | 4 sa 15 dk |
| 17 | 30 | 16.200 | 5.390 | — | 1 sa 39 dk | 326 | 0,5 SA | 5 sa 54 dk |
| 18 | 30 | 21.700 | 7.230 | — | 2 sa 5 dk | 376 | 0,5 SA | 7 sa 59 dk |
| 19 | 30 | 29.100 | 9.700 | — | 2 sa 45 dk | 434 | 0,5 SA | 10 sa 44 dk |
| 20 | 30 | 39.000 | 13.000 | — | 3 sa 30 dk | 500 | 0,5 SA | 14 sa 14 dk |
| 21 | 30 | 49.300 | 16.400 | — | 4 sa 30 dk | 554 | 0,5 SA | 18 sa 44 dk |
| 22 | 30 | 62.300 | 20.800 | — | 5 sa 45 dk | 614 | 0,5 SA | 1 g 29 dk |
| 23 | 30 | 78.800 | 26.300 | — | 7 sa 20 dk | 681 | 0,5 SA | 1 g 7 sa 49 dk |
| 24 | 30 | 99.700 | 33.200 | — | 9 sa 25 dk | 755 | 0,5 SA | 1 g 17 sa 14 dk |
| 25 | 30 | 126.000 | 42.000 | — | 12 sa | 837 | 0,5 SA | 2 g 5 sa 14 dk |
| 26 | 30 | 153.000 | 50.900 | — | 14 sa 5 dk | 927 | 0,5 SA | 2 g 19 sa 19 dk |
| 27 | 30 | 185.000 | 61.700 | — | 16 sa 35 dk | 1.028 | 0,5 SA | 3 g 11 sa 54 dk |
| 28 | 30 | 225.000 | 74.800 | — | 19 sa 30 dk | 1.139 | 0,5 SA | 4 g 7 sa 24 dk |
| 29 | 30 | 272.000 | 90.700 | — | 22 sa 55 dk | 1.263 | 0,5 SA | 5 g 6 sa 19 dk |
| 30 | 30 | 330.000 | 110.000 | — | 1 g 3 sa | 1.400 | 0,5 SA | 6 g 9 sa 19 dk |
| 31 | 31 | 389.000 | 130.000 | — | 1 g 7 sa 15 dk | 1.511 | 0,5 SA | 7 g 16 sa 34 dk |
| 32 | 32 | 458.000 | 153.000 | — | 1 g 12 sa 30 dk | 1.631 | 0,6 SA | 9 g 5 sa 4 dk |
| 33 | 33 | 540.000 | 180.000 | — | 1 g 18 sa 15 dk | 1.760 | 0,7 SA | 10 g 23 sa 19 dk |
| 34 | 34 | 636.000 | 212.000 | — | 2 g 1 sa | 1.899 | 0,7 SA | 13 g 19 dk |
| 35 | 35 | 750.000 | 250.000 | 50 | 2 g 9 sa | 2.049 | 0,8 SA | 15 g 9 sa 19 dk |
| 36 | 36 | 862.000 | 287.000 | 60 | 2 g 16 sa | 2.212 | 0,9 SA | 18 g 1 sa 19 dk |
| 37 | 37 | 990.000 | 330.000 | 65 | 3 g | 2.387 | 0,9 SA | 21 g 1 sa 19 dk |
| 38 | 38 | 1.140.000 | 379.000 | 75 | 3 g 8 sa 45 dk | 2.576 | 1 SA | 24 g 10 sa 4 dk |
| 39 | 39 | 1.310.000 | 435.000 | 85 | 3 g 18 sa 45 dk | 2.780 | 1,1 SA | 28 g 4 sa 49 dk |
| 40 | 40 | 1.500.000 | 500.000 | 100 | 4 g 6 sa | 3.000 | 1,2 SA | 32 g 10 sa 49 dk |
| 41 | 41 | 1.720.000 | 574.000 | 110 | 4 g 16 sa 45 dk | 3.187 | 1,3 SA | 37 g 3 sa 34 dk |
| 42 | 42 | 1.980.000 | 660.000 | 125 | 5 g 4 sa 30 dk | 3.387 | 1,4 SA | 42 g 8 sa 4 dk |
| 43 | 43 | 2.270.000 | 758.000 | 140 | 5 g 17 sa 30 dk | 3.598 | 1,5 SA | 48 g 1 sa 34 dk |
| 44 | 44 | 2.610.000 | 871.000 | 160 | 6 g 8 sa | 3.823 | 1,6 SA | 54 g 9 sa 34 dk |
| 45 | 45 | 3.000.000 | 1.000.000 | 180 | 7 g | 4.062 | 1,8 SA | 61 g 9 sa 34 dk |
| 46 | 46 | 3.450.000 | 1.150.000 | 200 | 7 g 14 sa 15 dk | 4.316 | 1,9 SA | 68 g 23 sa 49 dk |
| 47 | 47 | 3.960.000 | 1.320.000 | 225 | 8 g 5 sa 30 dk | 4.586 | 2 SA | 77 g 5 sa 19 dk |
| 48 | 48 | 4.550.000 | 1.520.000 | 250 | 8 g 22 sa 15 dk | 4.872 | 2,2 SA | 86 g 3 sa 34 dk |
| 49 | 49 | 5.220.000 | 1.740.000 | 280 | 9 g 16 sa 15 dk | 5.177 | 2,3 SA | 95 g 19 sa 49 dk |
| 50 | 50 | 6.000.000 | 2.000.000 | 315 | 10 g 12 sa | 5.500 | 2,5 SA | 106 g 7 sa 49 dk |


← [03 · Binalar](../03_binalar.md)
