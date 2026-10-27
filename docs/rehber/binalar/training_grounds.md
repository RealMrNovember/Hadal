<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Tatbikat Havuzu

`training_grounds` · Askeri · ⬜

> Basınçlı tatbikat havuzu: askerler gerçek derinlik koşullarında eğitilir.

**İşlev.** Asker kademelerini açar ve eğitim partisini büyütür. Reaktör önkoşul zincirindedir.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 5 |
| Adet | 1 |
| Halka | 3–12 |
| En yüksek seviye | 50 |
| Toplam maliyet (1 → en yüksek) | 37.550.610 Titanyum · 15.024.909 Gıda · 1.297 Süperalaşım |
| Toplam ham süre | 51 g 46 dk |
| Toplam güç | 115.250 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Geçici Tatbikat Çukuru | Kazılmış sığ çukur, derme korkuluklar ve hedef panoları; kehribar projektör. |
| 10–19 | Perçin | Perçinli Tatbikat Havuzu | Perçin kiti: havuz kenarına perçinli platform, izleme iskelesi. |
| 20–29 | Basınç | Basınç Odaları | Havuz yanında sıralı basınç odaları, konik pencerelerden eğitim mankenleri görünür. |
| 30–39 | Alaşım | Alaşım Talim Havzası | Alaşım kiti: süperalaşım platformlar, hedef dronları, kırmızı-mor ışık. |
| 40–49 | Biyomekanik | Kitin Talim Havuzu | Havuzu kitin kaburgalar çevreler, zemin bazalt yastık. Turkuaz-kırmızı. |
| 50 | Rezonans | Nabız Havuzu | Rezonans kiti: havuz suyu nabızla ışıldar; her atışta bir tatbikat halkası yayılır. |

## Seviye kilitleri

| Sv | Açılan |
| ---: | --- |
| 1 | T1 askerler |
| 5 | T2 askerler |
| 10 | T3 askerler |
| 15 | T4 askerler (+ araştırma) |
| 20 | T5 askerler (+ araştırma) |
| 25 | T6 askerler (+ araştırma) |
| 30 | T7 askerler (+ araştırma) |
| 35 | T8 askerler (+ araştırma) |
| 40 | T9 askerler (+ araştırma, Süperalaşım) |
| 45 | T10 askerler (+ araştırma, Süperalaşım) |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Gıda | Süperalaşım | Süre | Güç | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 5 | 15 | 6 | — | 1 sn | 15 | 1 sn |
| 2 | 5 | 25 | 10 | — | 1 sn | 20 | 2 sn |
| 3 | 5 | 45 | 18 | — | 2 sn | 26 | 4 sn |
| 4 | 5 | 90 | 35 | — | 4 sn | 34 | 8 sn |
| 5 | 5 | 175 | 70 | — | 7 sn | 45 | 15 sn |
| 6 | 6 | 260 | 105 | — | 14 sn | 60 | 29 sn |
| 7 | 7 | 385 | 155 | — | 29 sn | 79 | 58 sn |
| 8 | 8 | 570 | 230 | — | 58 sn | 104 | 116 sn |
| 9 | 9 | 845 | 335 | — | 108 sn | 137 | 3 dk 44 sn |
| 10 | 10 | 1.250 | 500 | — | 4 dk | 180 | 7 dk 44 sn |
| 11 | 11 | 1.790 | 715 | — | 6 dk | 208 | 13 dk 44 sn |
| 12 | 12 | 2.560 | 1.020 | — | 10 dk | 239 | 23 dk 44 sn |
| 13 | 13 | 3.660 | 1.470 | — | 14 dk | 276 | 37 dk 44 sn |
| 14 | 14 | 5.240 | 2.100 | — | 20 dk | 319 | 57 dk 44 sn |
| 15 | 15 | 7.500 | 3.000 | — | 29 dk | 367 | 1 sa 26 dk |
| 16 | 16 | 10.100 | 4.020 | — | 37 dk | 424 | 2 sa 3 dk |
| 17 | 17 | 13.500 | 5.390 | — | 48 dk | 489 | 2 sa 51 dk |
| 18 | 18 | 18.100 | 7.230 | — | 1 sa 1 dk | 564 | 3 sa 52 dk |
| 19 | 19 | 24.200 | 9.700 | — | 1 sa 18 dk | 650 | 5 sa 10 dk |
| 20 | 20 | 32.500 | 13.000 | — | 1 sa 41 dk | 750 | 6 sa 51 dk |
| 21 | 21 | 41.100 | 16.400 | — | 2 sa 10 dk | 831 | 9 sa 1 dk |
| 22 | 22 | 52.000 | 20.800 | — | 2 sa 45 dk | 921 | 11 sa 46 dk |
| 23 | 23 | 65.700 | 26.300 | — | 3 sa 30 dk | 1.021 | 15 sa 16 dk |
| 24 | 24 | 83.000 | 33.200 | — | 4 sa 30 dk | 1.132 | 19 sa 46 dk |
| 25 | 25 | 105.000 | 42.000 | — | 5 sa 45 dk | 1.255 | 1 g 1 sa 31 dk |
| 26 | 26 | 127.000 | 50.900 | — | 6 sa 45 dk | 1.391 | 1 g 8 sa 16 dk |
| 27 | 27 | 154.000 | 61.700 | — | 8 sa | 1.542 | 1 g 16 sa 16 dk |
| 28 | 28 | 187.000 | 74.800 | — | 9 sa 20 dk | 1.709 | 2 g 1 sa 36 dk |
| 29 | 29 | 227.000 | 90.700 | — | 11 sa | 1.895 | 2 g 12 sa 36 dk |
| 30 | 30 | 275.000 | 110.000 | — | 13 sa | 2.100 | 3 g 1 sa 36 dk |
| 31 | 31 | 324.000 | 130.000 | 15 | 15 sa 5 dk | 2.266 | 3 g 16 sa 41 dk |
| 32 | 32 | 382.000 | 153.000 | 17 | 17 sa 30 dk | 2.446 | 4 g 10 sa 11 dk |
| 33 | 33 | 450.000 | 180.000 | 20 | 20 sa 15 dk | 2.639 | 5 g 6 sa 26 dk |
| 34 | 34 | 530.000 | 212.000 | 20 | 23 sa 35 dk | 2.849 | 6 g 6 sa 1 dk |
| 35 | 35 | 625.000 | 250.000 | 25 | 1 g 3 sa 15 dk | 3.074 | 7 g 9 sa 16 dk |
| 36 | 36 | 718.000 | 287.000 | 30 | 1 g 6 sa 45 dk | 3.318 | 8 g 16 sa 1 dk |
| 37 | 37 | 825.000 | 330.000 | 35 | 1 g 10 sa 30 dk | 3.580 | 10 g 2 sa 31 dk |
| 38 | 38 | 947.000 | 379.000 | 40 | 1 g 14 sa 45 dk | 3.864 | 11 g 17 sa 16 dk |
| 39 | 39 | 1.090.000 | 435.000 | 45 | 1 g 19 sa 30 dk | 4.170 | 13 g 12 sa 46 dk |
| 40 | 40 | 1.250.000 | 500.000 | 50 | 2 g 1 sa | 4.500 | 15 g 13 sa 46 dk |
| 41 | 41 | 1.440.000 | 574.000 | 55 | 2 g 6 sa | 4.781 | 17 g 19 sa 46 dk |
| 42 | 42 | 1.650.000 | 660.000 | 65 | 2 g 11 sa 45 dk | 5.080 | 20 g 7 sa 31 dk |
| 43 | 43 | 1.890.000 | 758.000 | 70 | 2 g 18 sa | 5.397 | 23 g 1 sa 31 dk |
| 44 | 44 | 2.180.000 | 871.000 | 80 | 3 g 1 sa | 5.735 | 26 g 2 sa 31 dk |
| 45 | 45 | 2.500.000 | 1.000.000 | 90 | 3 g 8 sa 45 dk | 6.093 | 29 g 11 sa 16 dk |
| 46 | 46 | 2.870.000 | 1.150.000 | 100 | 3 g 15 sa 30 dk | 6.474 | 33 g 2 sa 46 dk |
| 47 | 47 | 3.300.000 | 1.320.000 | 115 | 3 g 22 sa 45 dk | 6.878 | 37 g 1 sa 31 dk |
| 48 | 48 | 3.790.000 | 1.520.000 | 125 | 4 g 6 sa 45 dk | 7.308 | 41 g 8 sa 16 dk |
| 49 | 49 | 4.350.000 | 1.740.000 | 140 | 4 g 15 sa 30 dk | 7.765 | 45 g 23 sa 46 dk |
| 50 | 50 | 5.000.000 | 2.000.000 | 160 | 5 g 1 sa | 8.250 | 51 g 46 dk |


← [03 · Binalar](../03_binalar.md)
