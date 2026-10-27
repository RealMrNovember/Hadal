<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Revir

`med_bay` · Askeri · ⬜

> Yaralı askerlerin tedavi edildiği tıbbi modül.

**İşlev.** Yaralı asker kapasitesi ve iyileştirme hızı. Kapasite dolarsa fazla yaralılar ölür. Reaktör önkoşul zincirindedir.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 6, 2. örnek Reaktör 15 |
| Adet | 2 |
| Halka | 2–12 |
| En yüksek seviye | 50 |
| Toplam maliyet (1 → en yüksek) | 30.043.657 Titanyum · 15.024.909 Biyokütle · 1.297 Süperalaşım |
| Toplam ham süre | 51 g 46 dk |
| Toplam güç | 76.834 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Seyyar Revir | Tek konteyner revir ve iki sedye; kapıda elle boyanmış camgöbeği şifa halkası, kehribar lamba. |
| 10–19 | Perçin | Perçinli Revir Bölmesi | Perçin kiti: perçinli koridor, ışıklı şifa halkası. |
| 20–29 | Basınç | Basınçlı Tedavi Bölmesi | İki tedavi küresi (basınç odası), konik pencereler; sıcak beyaz iç ışık. |
| 30–39 | Alaşım | Alaşım Şifa Kulesi | Alaşım kiti: süperalaşım kule katı, iyileştirme tankları, mor-camgöbeği. |
| 40–49 | Biyomekanik | Yaşayan Şifahane | Tedavi kürelerini yaşayan doku sarar. Turkuaz şifa damarları. |
| 50 | Rezonans | Rezonans Şifahanesi | Rezonans kiti: şifa damarları nabızla ışıldar; iyileşen asker arttıkça ışık yoğunlaşır. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süperalaşım | Süre | Güç | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 6 | 12 | 6 | — | 1 sn | 10 | 1 sn |
| 2 | 6 | 20 | 10 | — | 1 sn | 13 | 2 sn |
| 3 | 6 | 35 | 18 | — | 2 sn | 17 | 4 sn |
| 4 | 6 | 70 | 35 | — | 4 sn | 23 | 8 sn |
| 5 | 6 | 140 | 70 | — | 7 sn | 30 | 15 sn |
| 6 | 6 | 205 | 105 | — | 14 sn | 40 | 29 sn |
| 7 | 7 | 305 | 155 | — | 29 sn | 52 | 58 sn |
| 8 | 8 | 455 | 230 | — | 58 sn | 69 | 116 sn |
| 9 | 9 | 675 | 335 | — | 108 sn | 91 | 3 dk 44 sn |
| 10 | 10 | 1.000 | 500 | — | 4 dk | 120 | 7 dk 44 sn |
| 11 | 11 | 1.430 | 715 | — | 6 dk | 138 | 13 dk 44 sn |
| 12 | 12 | 2.050 | 1.020 | — | 10 dk | 160 | 23 dk 44 sn |
| 13 | 13 | 2.930 | 1.470 | — | 14 dk | 184 | 37 dk 44 sn |
| 14 | 14 | 4.190 | 2.100 | — | 20 dk | 212 | 57 dk 44 sn |
| 15 | 15 | 6.000 | 3.000 | — | 29 dk | 245 | 1 sa 26 dk |
| 16 | 16 | 8.040 | 4.020 | — | 37 dk | 283 | 2 sa 3 dk |
| 17 | 17 | 10.800 | 5.390 | — | 48 dk | 326 | 2 sa 51 dk |
| 18 | 18 | 14.500 | 7.230 | — | 1 sa 1 dk | 376 | 3 sa 52 dk |
| 19 | 19 | 19.400 | 9.700 | — | 1 sa 18 dk | 434 | 5 sa 10 dk |
| 20 | 20 | 26.000 | 13.000 | — | 1 sa 41 dk | 500 | 6 sa 51 dk |
| 21 | 21 | 32.900 | 16.400 | — | 2 sa 10 dk | 554 | 9 sa 1 dk |
| 22 | 22 | 41.600 | 20.800 | — | 2 sa 45 dk | 614 | 11 sa 46 dk |
| 23 | 23 | 52.500 | 26.300 | — | 3 sa 30 dk | 681 | 15 sa 16 dk |
| 24 | 24 | 66.400 | 33.200 | — | 4 sa 30 dk | 755 | 19 sa 46 dk |
| 25 | 25 | 84.000 | 42.000 | — | 5 sa 45 dk | 837 | 1 g 1 sa 31 dk |
| 26 | 26 | 102.000 | 50.900 | — | 6 sa 45 dk | 927 | 1 g 8 sa 16 dk |
| 27 | 27 | 123.000 | 61.700 | — | 8 sa | 1.028 | 1 g 16 sa 16 dk |
| 28 | 28 | 150.000 | 74.800 | — | 9 sa 20 dk | 1.139 | 2 g 1 sa 36 dk |
| 29 | 29 | 181.000 | 90.700 | — | 11 sa | 1.263 | 2 g 12 sa 36 dk |
| 30 | 30 | 220.000 | 110.000 | — | 13 sa | 1.400 | 3 g 1 sa 36 dk |
| 31 | 31 | 259.000 | 130.000 | 15 | 15 sa 5 dk | 1.511 | 3 g 16 sa 41 dk |
| 32 | 32 | 306.000 | 153.000 | 17 | 17 sa 30 dk | 1.631 | 4 g 10 sa 11 dk |
| 33 | 33 | 360.000 | 180.000 | 20 | 20 sa 15 dk | 1.760 | 5 g 6 sa 26 dk |
| 34 | 34 | 424.000 | 212.000 | 20 | 23 sa 35 dk | 1.899 | 6 g 6 sa 1 dk |
| 35 | 35 | 500.000 | 250.000 | 25 | 1 g 3 sa 15 dk | 2.049 | 7 g 9 sa 16 dk |
| 36 | 36 | 574.000 | 287.000 | 30 | 1 g 6 sa 45 dk | 2.212 | 8 g 16 sa 1 dk |
| 37 | 37 | 660.000 | 330.000 | 35 | 1 g 10 sa 30 dk | 2.387 | 10 g 2 sa 31 dk |
| 38 | 38 | 758.000 | 379.000 | 40 | 1 g 14 sa 45 dk | 2.576 | 11 g 17 sa 16 dk |
| 39 | 39 | 871.000 | 435.000 | 45 | 1 g 19 sa 30 dk | 2.780 | 13 g 12 sa 46 dk |
| 40 | 40 | 1.000.000 | 500.000 | 50 | 2 g 1 sa | 3.000 | 15 g 13 sa 46 dk |
| 41 | 41 | 1.150.000 | 574.000 | 55 | 2 g 6 sa | 3.187 | 17 g 19 sa 46 dk |
| 42 | 42 | 1.320.000 | 660.000 | 65 | 2 g 11 sa 45 dk | 3.387 | 20 g 7 sa 31 dk |
| 43 | 43 | 1.520.000 | 758.000 | 70 | 2 g 18 sa | 3.598 | 23 g 1 sa 31 dk |
| 44 | 44 | 1.740.000 | 871.000 | 80 | 3 g 1 sa | 3.823 | 26 g 2 sa 31 dk |
| 45 | 45 | 2.000.000 | 1.000.000 | 90 | 3 g 8 sa 45 dk | 4.062 | 29 g 11 sa 16 dk |
| 46 | 46 | 2.300.000 | 1.150.000 | 100 | 3 g 15 sa 30 dk | 4.316 | 33 g 2 sa 46 dk |
| 47 | 47 | 2.640.000 | 1.320.000 | 115 | 3 g 22 sa 45 dk | 4.586 | 37 g 1 sa 31 dk |
| 48 | 48 | 3.030.000 | 1.520.000 | 125 | 4 g 6 sa 45 dk | 4.872 | 41 g 8 sa 16 dk |
| 49 | 49 | 3.480.000 | 1.740.000 | 140 | 4 g 15 sa 30 dk | 5.177 | 45 g 23 sa 46 dk |
| 50 | 50 | 4.000.000 | 2.000.000 | 160 | 5 g 1 sa | 5.500 | 51 g 46 dk |


← [03 · Binalar](../03_binalar.md)
