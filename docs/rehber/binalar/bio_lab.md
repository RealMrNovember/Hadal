<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Biyo Laboratuvar

`bio_lab` · Üretim · 🟡

> Bakteri matlarını ve tüp kurdu kolonilerini işlenebilir biyokütleye çevirir.

**İşlev.** Biyokütle üretir. Takviye ve mürettebat ataması uygulanır.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 2, 2. örnek Reaktör 4, 3. örnek Reaktör 8 |
| Adet | 3 |
| Halka | 1–12 |
| En yüksek seviye | 50 |
| Üretim (saatlik) | Sv 1: 90 BK → Sv 50: 16.200 BK |
| Tüketim (saatlik) | Sv 1: 30 EN → Sv 50: 5.400 EN |
| Toplam maliyet (1 → en yüksek) | 18.019.304 Titanyum |
| Toplam ham süre | 63 g 19 sa 24 dk |
| Toplam güç | 46.102 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Yama Biyo Tankları | Merkez gövde ve boyları farklı üç fıçı, biri yamalı bir varil. Yeşil sıvı bulanık, kehribar çalışma lambası. |
| 10–19 | Perçin | Perçinli Biyo Laboratuvar | Fıçılar aynı boya getirilir; perçinli kapaklar, flanşlı borular, merkez gövde bir kat yükselir. Berrak yeşil sıvı, düzenli nabız. |
| 20–29 | Basınç | Basınçlı Biyo Küreler | Fıçılar basınç kürelerine dönüşür, konik pencerelerden bakteri matları görünür; ortada halka takviyeli kule. |
| 30–39 | Alaşım | Alaşım Fermantasyon Hattı | Küreler süperalaşım kulenin çevresinde sarmal dizilir; eşanjörlü besleme hattı. Yeşil-mor iç ışık. |
| 40–49 | Biyomekanik | Kaynaşık Doku Fıçıları | Küreler kitin dokuyla birbirine kaynar, doku kürelerin dışına taşar. Turkuaz damarlar, yavaş nefes ritmi. |
| 50 | Rezonans | Uyanmış Doku Bahçesi | Doku bahçesi tepede açılır; ışıklı lifler Çekirdek nabzıyla dalgalanır. Yeşil-beyaz parıltı. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Süre | Güç | Üretim/sa | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | 2 | 7 | 1 sn | 6 | 90 BK | 1 sn |
| 2 | 2 | 12 | 1 sn | 8 | 110 BK | 2 sn |
| 3 | 3 | 20 | 2 sn | 10 | 134 BK | 4 sn |
| 4 | 4 | 40 | 4 sn | 14 | 164 BK | 8 sn |
| 5 | 5 | 85 | 9 sn | 18 | 200 BK | 17 sn |
| 6 | 6 | 125 | 18 sn | 24 | 244 BK | 35 sn |
| 7 | 7 | 185 | 36 sn | 31 | 297 BK | 71 sn |
| 8 | 8 | 275 | 72 sn | 41 | 363 BK | 2 dk 23 sn |
| 9 | 9 | 405 | 2 dk | 55 | 443 BK | 4 dk 23 sn |
| 10 | 10 | 600 | 4 dk | 72 | 540 BK | 8 dk 23 sn |
| 11 | 11 | 860 | 7 dk | 83 | 612 BK | 15 dk 23 sn |
| 12 | 12 | 1.230 | 12 dk | 96 | 694 BK | 27 dk 23 sn |
| 13 | 13 | 1.760 | 17 dk | 110 | 786 BK | 44 dk 23 sn |
| 14 | 14 | 2.520 | 25 dk | 127 | 891 BK | 1 sa 9 dk |
| 15 | 15 | 3.600 | 36 dk | 147 | 1.010 BK | 1 sa 45 dk |
| 16 | 16 | 4.830 | 46 dk | 170 | 1.145 BK | 2 sa 31 dk |
| 17 | 17 | 6.470 | 59 dk | 196 | 1.298 BK | 3 sa 30 dk |
| 18 | 18 | 8.680 | 1 sa 16 dk | 226 | 1.471 BK | 4 sa 46 dk |
| 19 | 19 | 11.600 | 1 sa 38 dk | 260 | 1.667 BK | 6 sa 24 dk |
| 20 | 20 | 15.600 | 2 sa 5 dk | 300 | 1.890 BK | 8 sa 29 dk |
| 21 | 21 | 19.700 | 2 sa 40 dk | 333 | 2.053 BK | 11 sa 9 dk |
| 22 | 22 | 24.900 | 3 sa 25 dk | 369 | 2.230 BK | 14 sa 34 dk |
| 23 | 23 | 31.500 | 4 sa 25 dk | 409 | 2.422 BK | 18 sa 59 dk |
| 24 | 24 | 39.900 | 5 sa 40 dk | 453 | 2.631 BK | 1 g 39 dk |
| 25 | 25 | 50.400 | 7 sa 10 dk | 502 | 2.857 BK | 1 g 7 sa 49 dk |
| 26 | 26 | 61.100 | 8 sa 30 dk | 556 | 3.104 BK | 1 g 16 sa 19 dk |
| 27 | 27 | 74.100 | 10 sa | 617 | 3.371 BK | 2 g 2 sa 19 dk |
| 28 | 28 | 89.800 | 11 sa 45 dk | 684 | 3.662 BK | 2 g 14 sa 4 dk |
| 29 | 29 | 109.000 | 13 sa 45 dk | 758 | 3.977 BK | 3 g 3 sa 49 dk |
| 30 | 30 | 132.000 | 16 sa 10 dk | 840 | 4.320 BK | 3 g 19 sa 59 dk |
| 31 | 31 | 156.000 | 18 sa 50 dk | 907 | 4.658 BK | 4 g 14 sa 49 dk |
| 32 | 32 | 183.000 | 21 sa 50 dk | 978 | 5.023 BK | 5 g 12 sa 39 dk |
| 33 | 33 | 216.000 | 1 g 1 sa 15 dk | 1.056 | 5.416 BK | 6 g 13 sa 54 dk |
| 34 | 34 | 255.000 | 1 g 5 sa 30 dk | 1.139 | 5.840 BK | 7 g 19 sa 24 dk |
| 35 | 35 | 300.000 | 1 g 10 sa 15 dk | 1.230 | 6.297 BK | 9 g 5 sa 39 dk |
| 36 | 36 | 345.000 | 1 g 14 sa 30 dk | 1.327 | 6.790 BK | 10 g 20 sa 9 dk |
| 37 | 37 | 396.000 | 1 g 19 sa 15 dk | 1.432 | 7.322 BK | 12 g 15 sa 24 dk |
| 38 | 38 | 455.000 | 2 g 30 dk | 1.546 | 7.895 BK | 14 g 15 sa 54 dk |
| 39 | 39 | 522.000 | 2 g 6 sa 30 dk | 1.668 | 8.513 BK | 16 g 22 sa 24 dk |
| 40 | 40 | 600.000 | 2 g 13 sa 15 dk | 1.800 | 9.180 BK | 19 g 11 sa 39 dk |
| 41 | 41 | 689.000 | 2 g 19 sa 30 dk | 1.912 | 9.717 BK | 22 g 7 sa 9 dk |
| 42 | 42 | 792.000 | 3 g 2 sa 45 dk | 2.032 | 10.284 BK | 25 g 9 sa 54 dk |
| 43 | 43 | 909.000 | 3 g 10 sa 30 dk | 2.159 | 10.885 BK | 28 g 20 sa 24 dk |
| 44 | 44 | 1.040.000 | 3 g 19 sa 15 dk | 2.294 | 11.522 BK | 32 g 15 sa 39 dk |
| 45 | 45 | 1.200.000 | 4 g 4 sa 45 dk | 2.437 | 12.195 BK | 36 g 20 sa 24 dk |
| 46 | 46 | 1.380.000 | 4 g 13 sa 15 dk | 2.590 | 12.908 BK | 41 g 9 sa 39 dk |
| 47 | 47 | 1.580.000 | 4 g 22 sa 30 dk | 2.751 | 13.662 BK | 46 g 8 sa 9 dk |
| 48 | 48 | 1.820.000 | 5 g 8 sa 30 dk | 2.923 | 14.460 BK | 51 g 16 sa 39 dk |
| 49 | 49 | 2.090.000 | 5 g 19 sa 30 dk | 3.106 | 15.306 BK | 57 g 12 sa 9 dk |
| 50 | 50 | 2.400.000 | 6 g 7 sa 15 dk | 3.300 | 16.200 BK | 63 g 19 sa 24 dk |

## Bugünkü oyunla karşılaştırma (catalog v8)

Kodda bugün: en fazla 3 adet, Reaktör 2 ile açılır, 50 seviye; 1 → 50 toplam ham süre 10 sa. Hedef tasarımda aynı yol 63 g 19 sa 24 dk sürer.

| Sv | v8 Titanyum | v8 süre | Hedef Titanyum | Hedef süre |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 70 | 15 sn | 7 | 1 sn |
| 5 | 99 | 24 sn | 85 | 9 sn |
| 10 | 152 | 42 sn | 600 | 4 dk |
| 15 | 234 | 73 sn | 3.600 | 36 dk |
| 20 | 360 | 2 dk 9 sn | 15.600 | 2 sa 5 dk |
| 25 | 554 | 3 dk 48 sn | 50.400 | 7 sa 10 dk |
| 30 | 852 | 6 dk 41 sn | 132.000 | 16 sa 10 dk |
| 35 | 1.311 | 11 dk 47 sn | 300.000 | 1 g 10 sa 15 dk |
| 40 | 2.017 | 20 dk 46 sn | 600.000 | 2 g 13 sa 15 dk |
| 45 | 3.104 | 36 dk 36 sn | 1.200.000 | 4 g 4 sa 45 dk |
| 50 | 4.775 | 1 sa 4 dk | 2.400.000 | 6 g 7 sa 15 dk |


← [03 · Binalar](../03_binalar.md)
