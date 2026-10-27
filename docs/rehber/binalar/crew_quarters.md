<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Mürettebat Yatakhanesi

`crew_quarters` · Destek · ⬜

> Üssün insanlarının yaşadığı modül.

**İşlev.** Mürettebat kapasitesi; uzman personel binalara atanır ve kendi becerilerini getirir.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 5, 2. örnek Reaktör 9, 3. örnek Reaktör 14, 4. örnek Reaktör 20 |
| Adet | 4 |
| Halka | 1–12 |
| En yüksek seviye | 50 |
| Toplam maliyet (1 → en yüksek) | 15.024.909 Titanyum · 7.510.285 Biyokütle |
| Toplam ham süre | 51 g 46 dk |
| Toplam güç | 46.102 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Yama Ranza Bölmesi | Üst üste konmuş kargo konteynerleri, merdivenler ve ranzalar; pencerelerde kehribar ışık. |
| 10–19 | Perçin | Perçinli Yatakhane | Perçin kiti: konteynerler perçinli bloklara dönüşür, iskele koridorları eklenir. |
| 20–29 | Basınç | Basınç Yaşam Bloğu | Basınç küresi yaşam blokları, konik pencerelerden sıcak iç ışık. |
| 30–39 | Alaşım | Alaşım Konut Kulesi | Alaşım kiti: süperalaşım kule katları, bağlantı köprüleri, mor sokak ışıkları. |
| 40–49 | Biyomekanik | Kadim Konaklar | Bloklar kitin kaburgalarla birbirine bağlanır, bahçeli teraslar açılır. Turkuaz. |
| 50 | Rezonans | Işıyan Mahalle | Rezonans kiti: pencereler Çekirdek nabzıyla yumuşakça parlar; mahalle ışıktan bir ağ olur. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süre | Güç | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 5 | 6 | 3 | 1 sn | 6 | 1 sn |
| 2 | 5 | 10 | 5 | 1 sn | 8 | 2 sn |
| 3 | 5 | 18 | 9 | 2 sn | 10 | 4 sn |
| 4 | 5 | 35 | 18 | 4 sn | 14 | 8 sn |
| 5 | 5 | 70 | 35 | 7 sn | 18 | 15 sn |
| 6 | 6 | 105 | 50 | 14 sn | 24 | 29 sn |
| 7 | 7 | 155 | 75 | 29 sn | 31 | 58 sn |
| 8 | 8 | 230 | 115 | 58 sn | 41 | 116 sn |
| 9 | 9 | 335 | 170 | 108 sn | 55 | 3 dk 44 sn |
| 10 | 10 | 500 | 250 | 4 dk | 72 | 7 dk 44 sn |
| 11 | 11 | 715 | 360 | 6 dk | 83 | 13 dk 44 sn |
| 12 | 12 | 1.020 | 510 | 10 dk | 96 | 23 dk 44 sn |
| 13 | 13 | 1.470 | 735 | 14 dk | 110 | 37 dk 44 sn |
| 14 | 14 | 2.100 | 1.050 | 20 dk | 127 | 57 dk 44 sn |
| 15 | 15 | 3.000 | 1.500 | 29 dk | 147 | 1 sa 26 dk |
| 16 | 16 | 4.020 | 2.010 | 37 dk | 170 | 2 sa 3 dk |
| 17 | 17 | 5.390 | 2.700 | 48 dk | 196 | 2 sa 51 dk |
| 18 | 18 | 7.230 | 3.620 | 1 sa 1 dk | 226 | 3 sa 52 dk |
| 19 | 19 | 9.700 | 4.850 | 1 sa 18 dk | 260 | 5 sa 10 dk |
| 20 | 20 | 13.000 | 6.500 | 1 sa 41 dk | 300 | 6 sa 51 dk |
| 21 | 21 | 16.400 | 8.220 | 2 sa 10 dk | 333 | 9 sa 1 dk |
| 22 | 22 | 20.800 | 10.400 | 2 sa 45 dk | 369 | 11 sa 46 dk |
| 23 | 23 | 26.300 | 13.100 | 3 sa 30 dk | 409 | 15 sa 16 dk |
| 24 | 24 | 33.200 | 16.600 | 4 sa 30 dk | 453 | 19 sa 46 dk |
| 25 | 25 | 42.000 | 21.000 | 5 sa 45 dk | 502 | 1 g 1 sa 31 dk |
| 26 | 26 | 50.900 | 25.500 | 6 sa 45 dk | 556 | 1 g 8 sa 16 dk |
| 27 | 27 | 61.700 | 30.900 | 8 sa | 617 | 1 g 16 sa 16 dk |
| 28 | 28 | 74.800 | 37.400 | 9 sa 20 dk | 684 | 2 g 1 sa 36 dk |
| 29 | 29 | 90.700 | 45.400 | 11 sa | 758 | 2 g 12 sa 36 dk |
| 30 | 30 | 110.000 | 55.000 | 13 sa | 840 | 3 g 1 sa 36 dk |
| 31 | 31 | 130.000 | 64.800 | 15 sa 5 dk | 907 | 3 g 16 sa 41 dk |
| 32 | 32 | 153.000 | 76.400 | 17 sa 30 dk | 978 | 4 g 10 sa 11 dk |
| 33 | 33 | 180.000 | 90.000 | 20 sa 15 dk | 1.056 | 5 g 6 sa 26 dk |
| 34 | 34 | 212.000 | 106.000 | 23 sa 35 dk | 1.139 | 6 g 6 sa 1 dk |
| 35 | 35 | 250.000 | 125.000 | 1 g 3 sa 15 dk | 1.230 | 7 g 9 sa 16 dk |
| 36 | 36 | 287.000 | 144.000 | 1 g 6 sa 45 dk | 1.327 | 8 g 16 sa 1 dk |
| 37 | 37 | 330.000 | 165.000 | 1 g 10 sa 30 dk | 1.432 | 10 g 2 sa 31 dk |
| 38 | 38 | 379.000 | 189.000 | 1 g 14 sa 45 dk | 1.546 | 11 g 17 sa 16 dk |
| 39 | 39 | 435.000 | 218.000 | 1 g 19 sa 30 dk | 1.668 | 13 g 12 sa 46 dk |
| 40 | 40 | 500.000 | 250.000 | 2 g 1 sa | 1.800 | 15 g 13 sa 46 dk |
| 41 | 41 | 574.000 | 287.000 | 2 g 6 sa | 1.912 | 17 g 19 sa 46 dk |
| 42 | 42 | 660.000 | 330.000 | 2 g 11 sa 45 dk | 2.032 | 20 g 7 sa 31 dk |
| 43 | 43 | 758.000 | 379.000 | 2 g 18 sa | 2.159 | 23 g 1 sa 31 dk |
| 44 | 44 | 871.000 | 435.000 | 3 g 1 sa | 2.294 | 26 g 2 sa 31 dk |
| 45 | 45 | 1.000.000 | 500.000 | 3 g 8 sa 45 dk | 2.437 | 29 g 11 sa 16 dk |
| 46 | 46 | 1.150.000 | 574.000 | 3 g 15 sa 30 dk | 2.590 | 33 g 2 sa 46 dk |
| 47 | 47 | 1.320.000 | 660.000 | 3 g 22 sa 45 dk | 2.751 | 37 g 1 sa 31 dk |
| 48 | 48 | 1.520.000 | 758.000 | 4 g 6 sa 45 dk | 2.923 | 41 g 8 sa 16 dk |
| 49 | 49 | 1.740.000 | 871.000 | 4 g 15 sa 30 dk | 3.106 | 45 g 23 sa 46 dk |
| 50 | 50 | 2.000.000 | 1.000.000 | 5 g 1 sa | 3.300 | 51 g 46 dk |


← [03 · Binalar](../03_binalar.md)
