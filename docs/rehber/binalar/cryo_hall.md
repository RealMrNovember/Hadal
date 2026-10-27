<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Kriyo Salonu

`cryo_hall` · Destek · ⬜

> Eski seferlerden kurtarılan kriyo-kapsüllerin uyandırıldığı salon.

**İşlev.** Kriyo-Kapsül çekimleri; ücretsiz çekim aralığı ve kahraman kadrosu kapasitesi seviyeyle iyileşir.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 2 |
| Adet | 1 |
| Halka | 1–12 |
| En yüksek seviye | 50 |
| Toplam maliyet (1 → en yüksek) | 22.528.654 Titanyum · 7.510.285 Biyokütle |
| Toplam ham süre | 85 g 1 sa 45 dk |
| Toplam güç | 76.834 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Yama Kapsül Odası | Alçak salon; eski konvoydan sökülmüş kriyo-kapsüller duvara dayalı, camları buğulu. Kehribar ışık. |
| 10–19 | Perçin | Perçinli Kriyo Salonu | Perçin kiti: kapsül sıraları perçinli raylara alınır, camgöbeği durum şeritleri. |
| 20–29 | Basınç | Basınç Uyandırma Salonu | Salon basınç tonozuna dönüşür, kapsüller konik pencereli nişlerde; tabanda soğuk buhar süzülür. |
| 30–39 | Alaşım | Alaşım Kriyo Galerisi | Alaşım kiti: süperalaşım kaplama, tepede soğutma eşanjörleri, mor uyanma ışığı. |
| 40–49 | Biyomekanik | Kitin Uyku Galerisi | Kapsüller kitin kozalara dönüşür, tonozu kaburgalar taşır. Turkuaz nefes ritmi. |
| 50 | Rezonans | Uyanış Salonu | Rezonans kiti: kozalar Çekirdek nabzıyla içeriden aydınlanır; bir kahraman uyandığında ışık tepeye yükselir. |

## Seviye kilitleri

| Sv | Açılan |
| ---: | --- |
| 1 | Günde 1 ücretsiz çekim |
| 20 | Ücretsiz çekim 20 saatte bir |
| 35 | Ücretsiz çekim 16 saatte bir |
| 50 | Ücretsiz çekim 12 saatte bir |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süre | Güç | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 2 | 9 | 3 | 1 sn | 10 | 1 sn |
| 2 | 2 | 15 | 5 | 1 sn | 13 | 2 sn |
| 3 | 3 | 25 | 9 | 3 sn | 17 | 5 sn |
| 4 | 4 | 50 | 18 | 6 sn | 23 | 11 sn |
| 5 | 5 | 105 | 35 | 12 sn | 30 | 23 sn |
| 6 | 6 | 155 | 50 | 24 sn | 40 | 47 sn |
| 7 | 7 | 230 | 75 | 48 sn | 52 | 95 sn |
| 8 | 8 | 340 | 115 | 96 sn | 69 | 3 dk 11 sn |
| 9 | 9 | 505 | 170 | 3 dk | 91 | 6 dk 11 sn |
| 10 | 10 | 750 | 250 | 6 dk | 120 | 12 dk 11 sn |
| 11 | 11 | 1.070 | 360 | 10 dk | 138 | 22 dk 11 sn |
| 12 | 12 | 1.540 | 510 | 16 dk | 160 | 38 dk 11 sn |
| 13 | 13 | 2.200 | 735 | 23 dk | 184 | 1 sa 1 dk |
| 14 | 14 | 3.140 | 1.050 | 33 dk | 212 | 1 sa 34 dk |
| 15 | 15 | 4.500 | 1.500 | 48 dk | 245 | 2 sa 22 dk |
| 16 | 16 | 6.030 | 2.010 | 1 sa 2 dk | 283 | 3 sa 24 dk |
| 17 | 17 | 8.090 | 2.700 | 1 sa 19 dk | 326 | 4 sa 43 dk |
| 18 | 18 | 10.800 | 3.620 | 1 sa 42 dk | 376 | 6 sa 25 dk |
| 19 | 19 | 14.500 | 4.850 | 2 sa 10 dk | 434 | 8 sa 35 dk |
| 20 | 20 | 19.500 | 6.500 | 2 sa 50 dk | 500 | 11 sa 25 dk |
| 21 | 21 | 24.700 | 8.220 | 3 sa 35 dk | 554 | 15 sa |
| 22 | 22 | 31.200 | 10.400 | 4 sa 35 dk | 614 | 19 sa 35 dk |
| 23 | 23 | 39.400 | 13.100 | 5 sa 50 dk | 681 | 1 g 1 sa 25 dk |
| 24 | 24 | 49.800 | 16.600 | 7 sa 30 dk | 755 | 1 g 8 sa 55 dk |
| 25 | 25 | 63.000 | 21.000 | 9 sa 35 dk | 837 | 1 g 18 sa 30 dk |
| 26 | 26 | 76.400 | 25.500 | 11 sa 15 dk | 927 | 2 g 5 sa 45 dk |
| 27 | 27 | 92.600 | 30.900 | 13 sa 15 dk | 1.028 | 2 g 19 sa |
| 28 | 28 | 112.000 | 37.400 | 15 sa 35 dk | 1.139 | 3 g 10 sa 35 dk |
| 29 | 29 | 136.000 | 45.400 | 18 sa 20 dk | 1.263 | 4 g 4 sa 55 dk |
| 30 | 30 | 165.000 | 55.000 | 21 sa 35 dk | 1.400 | 5 g 2 sa 30 dk |
| 31 | 31 | 194.000 | 64.800 | 1 g 1 sa | 1.511 | 6 g 3 sa 30 dk |
| 32 | 32 | 229.000 | 76.400 | 1 g 5 sa | 1.631 | 7 g 8 sa 30 dk |
| 33 | 33 | 270.000 | 90.000 | 1 g 9 sa 45 dk | 1.760 | 8 g 18 sa 15 dk |
| 34 | 34 | 318.000 | 106.000 | 1 g 15 sa 15 dk | 1.899 | 10 g 9 sa 30 dk |
| 35 | 35 | 375.000 | 125.000 | 1 g 21 sa 30 dk | 2.049 | 12 g 7 sa |
| 36 | 36 | 431.000 | 144.000 | 2 g 3 sa 15 dk | 2.212 | 14 g 10 sa 15 dk |
| 37 | 37 | 495.000 | 165.000 | 2 g 9 sa 30 dk | 2.387 | 16 g 19 sa 45 dk |
| 38 | 38 | 568.000 | 189.000 | 2 g 16 sa 45 dk | 2.576 | 19 g 12 sa 30 dk |
| 39 | 39 | 653.000 | 218.000 | 3 g 45 dk | 2.780 | 22 g 13 sa 15 dk |
| 40 | 40 | 750.000 | 250.000 | 3 g 9 sa 30 dk | 3.000 | 25 g 22 sa 45 dk |
| 41 | 41 | 862.000 | 287.000 | 3 g 18 sa 15 dk | 3.187 | 29 g 17 sa |
| 42 | 42 | 990.000 | 330.000 | 4 g 3 sa 45 dk | 3.387 | 33 g 20 sa 45 dk |
| 43 | 43 | 1.140.000 | 379.000 | 4 g 14 sa | 3.598 | 38 g 10 sa 45 dk |
| 44 | 44 | 1.310.000 | 435.000 | 5 g 1 sa 45 dk | 3.823 | 43 g 12 sa 30 dk |
| 45 | 45 | 1.500.000 | 500.000 | 5 g 14 sa 30 dk | 4.062 | 49 g 3 sa |
| 46 | 46 | 1.720.000 | 574.000 | 6 g 1 sa 45 dk | 4.316 | 55 g 4 sa 45 dk |
| 47 | 47 | 1.980.000 | 660.000 | 6 g 14 sa | 4.586 | 61 g 18 sa 45 dk |
| 48 | 48 | 2.270.000 | 758.000 | 7 g 3 sa 30 dk | 4.872 | 68 g 22 sa 15 dk |
| 49 | 49 | 2.610.000 | 871.000 | 7 g 18 sa | 5.177 | 76 g 16 sa 15 dk |
| 50 | 50 | 3.000.000 | 1.000.000 | 8 g 9 sa 30 dk | 5.500 | 85 g 1 sa 45 dk |


← [03 · Binalar](../03_binalar.md)
