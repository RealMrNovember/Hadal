<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Çekirdek Reaktör

`core_reactor` · Merkez · 🟡

> Üssün kalbi: kompakt bir fisyon reaktörü. Her şeyin seviye tavanı budur.

**İşlev.** Enerji üretir. Hiçbir bina reaktörün seviyesini geçemez. Halkaları, yeni binaları ve inşaat seçeneklerini açar. Aşırı Yük (Overdrive): 1 saat %50 fazla enerji, 8 saatte bir, ücretsiz.

| Özellik | Değer |
| --- | --- |
| Açılış | Başlangıçta hazır |
| Adet | 1 |
| Halka | 0 |
| En yüksek seviye | 50 |
| Üretim (saatlik) | Sv 1: 700 EN → Sv 50: 126.000 EN |
| Toplam maliyet (1 → en yüksek) | 150.249.010 Titanyum · 52.572.945 Biyokütle · 8.595 Süperalaşım |
| Toplam ham süre | 425 g 9 sa 19 dk |
| Toplam güç | 384.164 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Yama Reaktörü | Basık silindir gövde enkaz levhalarıyla yamalı, cam kubbe bantlı. Kehribar çalışma lambaları; çekirdek düzensiz titrer, yörünge halkası sarsak döner. |
| 10–19 | Perçin | Perçinli Reaktör | Gövde perçinli titanyum panellerle kaplanır ve yükselir; kanatçıklar cıvatalı flanşla bağlanır. Camgöbeği şeritler sabit yanar, halka akıcı döner. |
| 20–29 | Basınç | Basınç Çekirdeği | Gövde halka takviyeli bir basınç küresine dönüşür; çevresinde konik akrilik pencereli dört yardımcı küre. Soğuk camgöbeği çekirdek, pencerelerden sıcak ışık. |
| 30–39 | Alaşım | Alaşım Çekirdek Kulesi | Süperalaşım kaplı iki katlı kule; tepede sonar çanağı, yanlarda baca ısısını alan kanatlı eşanjörler. Mavimsi-bronz parıltı, mor vurgu ışıkları. |
| 40–49 | Biyomekanik | Kadim Kalp | Kitin kaburgalar gövdeyi sarar, kaide yastık lav bazaltına oturur; Hadalit damarlar çekirdeğe akar. Turkuaz damarlar beş saniyelik nefes ritmiyle yanar. |
| 50 | Rezonans | Uyanmış Çekirdek | Kaburgalar açılıp yarı saydam bir taç kurar; çekirdek beyaz-camgöbeği, çevresinde mor hale. Her atış kablolar boyunca halkalara yayılır; üssün bütün binaları bu nabzı izler. |

## Reaktör seviyeleriyle açılanlar

| Sv | Açılan |
| ---: | --- |
| 2 | Gıda Laboratuvarı (2.), Biyo Laboratuvar (1.), Dron Fabrikası (1.), Süs Kulesi (1.), Kriyo Salonu |
| 3 | Oksijen Tesisi (3.), Hidroponik Çiftlik (1.), Araştırma Merkezi, Basınç Ambarı (1.), Halka 5 |
| 4 | Biyo Laboratuvar (2.), Dron Fabrikası (2.), Denizaltı İskelesi, Radar Merkezi, Süs Kulesi (2.), Mühendislik Bölmesi, Denizaltı Hangarı (1.) |
| 5 | Gıda Laboratuvarı (3.), Askeri Kubbe, Kalkan Jeneratörü, Tatbikat Havuzu, Mürettebat Yatakhanesi (1.), Halka 6 |
| 6 | Oksijen Tesisi (4.), Mega Kubbe, İttifak Merkezi, Revir (1.), Hidrotermal Santral (1.) |
| 7 | Hidroponik Çiftlik (2.), Dron Fabrikası (3.), Tarım Kubbesi |
| 8 | Biyo Laboratuvar (3.), Süs Kulesi (3.), Kronik Feneri, Halka 7 |
| 9 | Gıda Laboratuvarı (4.), Mürettebat Yatakhanesi (2.) |
| 10 | Basınç Ambarı (2.), Denizaltı Hangarı (2.), Takas İstasyonu |
| 12 | Hidroponik Çiftlik (3.), Süs Kulesi (4.), Hidrotermal Santral (2.), Bilim Kubbesi, Teçhizat Atölyesi, Halka 8 |
| 14 | Mürettebat Yatakhanesi (3.), Dalgıç Anıtı, Zıpkın Anıtı, Ark Anıtı |
| 15 | Revir (2.), Kalıntı Arşivi |
| 16 | Süs Kulesi (5.), Kriyo Bakım Ünitesi, Savaş Kubbesi, Halka 9 |
| 18 | Denizaltı Hangarı (3.) |
| 20 | Süs Kulesi (6.), Basınç Ambarı (3.), Mürettebat Yatakhanesi (4.), Hidrotermal Santral (3.), Halka 10 |
| 25 | Halka 11 |
| 26 | Denizaltı Hangarı (4.) |
| 30 | Alaşım Dökümhanesi (1.), Halka 12 |
| 40 | Alaşım Dökümhanesi (2.) |
| 50 | 49 → 50 için 1 Kadim Şema gerekir. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Önkoşul | Titanyum | Biyokütle | Süperalaşım | Süre | Güç | Üretim/sa | Kümülatif süre |
| ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | — | Başlangıç | — | — | — | — | 50 | 700 EN | — |
| 2 | 1 | — | 100 | — | — | 5 sn | 66 | 854 EN | 5 sn |
| 3 | 2 | Oksijen Tesisi 2 | 180 | — | — | 15 sn | 87 | 1.042 EN | 20 sn |
| 4 | 3 | Dron Fabrikası 3 | 350 | 120 | — | 30 sn | 114 | 1.272 EN | 50 sn |
| 5 | 4 | Araştırma Merkezi 4 | 700 | 245 | — | 60 sn | 151 | 1.552 EN | 110 sn |
| 6 | 5 | Gıda Laboratuvarı 5 | 1.040 | 365 | — | 2 dk | 199 | 1.894 EN | 3 dk 50 sn |
| 7 | 6 | Kalkan Jeneratörü 6, Araştırma Merkezi 6 | 1.540 | 540 | — | 4 dk | 262 | 2.311 EN | 7 dk 50 sn |
| 8 | 7 | Kalkan Jeneratörü 7, Askeri Kubbe 7 | 2.280 | 795 | — | 8 dk | 345 | 2.821 EN | 15 dk 50 sn |
| 9 | 8 | Kalkan Jeneratörü 8, Denizaltı İskelesi 8 | 3.370 | 1.180 | — | 15 dk | 455 | 3.442 EN | 30 dk 50 sn |
| 10 | 9 | Kalkan Jeneratörü 9, Tatbikat Havuzu 9 | 5.000 | 1.750 | — | 30 dk | 600 | 4.200 EN | 1 sa |
| 11 | 10 | Kalkan Jeneratörü 10, Revir 10 | 7.150 | 2.500 | — | 49 dk | 692 | 4.761 EN | 1 sa 49 dk |
| 12 | 11 | Kalkan Jeneratörü 11, İttifak Merkezi 11 | 10.200 | 3.580 | — | 1 sa 20 dk | 798 | 5.396 EN | 3 sa 9 dk |
| 13 | 12 | Kalkan Jeneratörü 12, Araştırma Merkezi 12 | 14.700 | 5.130 | — | 1 sa 55 dk | 921 | 6.116 EN | 5 sa 4 dk |
| 14 | 13 | Kalkan Jeneratörü 13, Askeri Kubbe 13 | 21.000 | 7.340 | — | 2 sa 45 dk | 1.062 | 6.932 EN | 7 sa 49 dk |
| 15 | 14 | Kalkan Jeneratörü 14, Denizaltı İskelesi 14 | 30.000 | 10.500 | — | 4 sa | 1.225 | 7.857 EN | 11 sa 49 dk |
| 16 | 15 | Kalkan Jeneratörü 15, Tatbikat Havuzu 15 | 40.200 | 14.100 | — | 5 sa 10 dk | 1.413 | 8.906 EN | 16 sa 59 dk |
| 17 | 16 | Kalkan Jeneratörü 16, Revir 16 | 53.900 | 18.900 | — | 6 sa 35 dk | 1.629 | 10.095 EN | 23 sa 34 dk |
| 18 | 17 | Kalkan Jeneratörü 17, İttifak Merkezi 17 | 72.300 | 25.300 | — | 8 sa 30 dk | 1.879 | 11.442 EN | 1 g 8 sa 4 dk |
| 19 | 18 | Kalkan Jeneratörü 18, Araştırma Merkezi 18 | 97.000 | 33.900 | — | 10 sa 55 dk | 2.168 | 12.969 EN | 1 g 18 sa 59 dk |
| 20 | 19 | Kalkan Jeneratörü 19, Askeri Kubbe 19 | 130.000 | 45.500 | — | 14 sa | 2.500 | 14.700 EN | 2 g 8 sa 59 dk |
| 21 | 20 | Kalkan Jeneratörü 20, Denizaltı İskelesi 20, Mega Kubbe 2 | 164.000 | 57.500 | — | 17 sa 55 dk | 2.771 | 15.967 EN | 3 g 2 sa 54 dk |
| 22 | 21 | Kalkan Jeneratörü 21, Tatbikat Havuzu 21, Tarım Kubbesi 2 | 208.000 | 72.700 | — | 22 sa 55 dk | 3.072 | 17.343 EN | 4 g 1 sa 49 dk |
| 23 | 22 | Kalkan Jeneratörü 22, Revir 22 | 263.000 | 92.000 | — | 1 g 5 sa 15 dk | 3.405 | 18.838 EN | 5 g 7 sa 4 dk |
| 24 | 23 | Kalkan Jeneratörü 23, İttifak Merkezi 23 | 332.000 | 116.000 | — | 1 g 13 sa 30 dk | 3.774 | 20.461 EN | 6 g 20 sa 34 dk |
| 25 | 24 | Kalkan Jeneratörü 24, Araştırma Merkezi 24 | 420.000 | 147.000 | — | 2 g | 4.183 | 22.224 EN | 8 g 20 sa 34 dk |
| 26 | 25 | Kalkan Jeneratörü 25, Askeri Kubbe 25 | 509.000 | 178.000 | — | 2 g 8 sa 30 dk | 4.637 | 24.140 EN | 11 g 5 sa 4 dk |
| 27 | 26 | Kalkan Jeneratörü 26, Denizaltı İskelesi 26 | 617.000 | 216.000 | — | 2 g 18 sa 30 dk | 5.140 | 26.220 EN | 13 g 23 sa 34 dk |
| 28 | 27 | Kalkan Jeneratörü 27, Tatbikat Havuzu 27 | 748.000 | 262.000 | — | 3 g 6 sa | 5.697 | 28.480 EN | 17 g 5 sa 34 dk |
| 29 | 28 | Kalkan Jeneratörü 28, Revir 28 | 907.000 | 318.000 | — | 3 g 19 sa 45 dk | 6.315 | 30.934 EN | 21 g 1 sa 19 dk |
| 30 | 29 | Kalkan Jeneratörü 29, İttifak Merkezi 29 | 1.100.000 | 385.000 | — | 4 g 12 sa | 7.000 | 33.600 EN | 25 g 13 sa 19 dk |
| 31 | 30 | Kalkan Jeneratörü 30, Araştırma Merkezi 30, Mega Kubbe 3 | 1.300.000 | 454.000 | 100 | 5 g 5 sa 30 dk | 7.554 | 36.231 EN | 30 g 18 sa 49 dk |
| 32 | 31 | Kalkan Jeneratörü 31, Askeri Kubbe 31, Bilim Kubbesi 3 | 1.530.000 | 535.000 | 115 | 6 g 1 sa 30 dk | 8.153 | 39.067 EN | 36 g 20 sa 19 dk |
| 33 | 32 | Kalkan Jeneratörü 32, Denizaltı İskelesi 32 | 1.800.000 | 630.000 | 130 | 7 g 1 sa | 8.798 | 42.126 EN | 43 g 21 sa 19 dk |
| 34 | 33 | Kalkan Jeneratörü 33, Tatbikat Havuzu 33 | 2.120.000 | 743.000 | 150 | 8 g 4 sa 15 dk | 9.495 | 45.424 EN | 52 g 1 sa 34 dk |
| 35 | 34 | Kalkan Jeneratörü 34, Revir 34 | 2.500.000 | 875.000 | 170 | 9 g 12 sa | 10.247 | 48.980 EN | 61 g 13 sa 34 dk |
| 36 | 35 | Kalkan Jeneratörü 35, İttifak Merkezi 35 | 2.870.000 | 1.010.000 | 195 | 10 g 16 sa 15 dk | 11.058 | 52.815 EN | 72 g 5 sa 49 dk |
| 37 | 36 | Kalkan Jeneratörü 36, Araştırma Merkezi 36 | 3.300.000 | 1.150.000 | 220 | 11 g 23 sa 45 dk | 11.934 | 56.950 EN | 84 g 5 sa 34 dk |
| 38 | 37 | Kalkan Jeneratörü 37, Askeri Kubbe 37 | 3.790.000 | 1.330.000 | 255 | 13 g 11 sa 15 dk | 12.879 | 61.408 EN | 97 g 16 sa 49 dk |
| 39 | 38 | Kalkan Jeneratörü 38, Denizaltı İskelesi 38 | 4.350.000 | 1.520.000 | 290 | 15 g 3 sa 15 dk | 13.899 | 66.216 EN | 112 g 20 sa 4 dk |
| 40 | 39 | Kalkan Jeneratörü 39, Tatbikat Havuzu 39 | 5.000.000 | 1.750.000 | 330 | 17 g | 15.000 | 71.400 EN | 129 g 20 sa 4 dk |
| 41 | 40 | Kalkan Jeneratörü 40, Revir 40, Mega Kubbe 4 | 5.740.000 | 2.010.000 | 370 | 18 g 18 sa 45 dk | 15.937 | 75.573 EN | 148 g 14 sa 49 dk |
| 42 | 41 | Kalkan Jeneratörü 41, İttifak Merkezi 41, Savaş Kubbesi 4 | 6.600.000 | 2.310.000 | 420 | 20 g 18 sa 15 dk | 16.933 | 79.989 EN | 169 g 9 sa 4 dk |
| 43 | 42 | Kalkan Jeneratörü 42, Araştırma Merkezi 42 | 7.580.000 | 2.650.000 | 470 | 22 g 22 sa 30 dk | 17.991 | 84.664 EN | 192 g 7 sa 34 dk |
| 44 | 43 | Kalkan Jeneratörü 43, Askeri Kubbe 43 | 8.710.000 | 3.050.000 | 530 | 25 g 8 sa 15 dk | 19.116 | 89.612 EN | 217 g 15 sa 49 dk |
| 45 | 44 | Kalkan Jeneratörü 44, Denizaltı İskelesi 44 | 10.000.000 | 3.500.000 | 600 | 28 g | 20.310 | 94.849 EN | 245 g 15 sa 49 dk |
| 46 | 45 | Kalkan Jeneratörü 45, Tatbikat Havuzu 45, Mega Kubbe 5 | 11.500.000 | 4.020.000 | 670 | 30 g 8 sa 45 dk | 21.579 | 100.393 EN | 276 g 34 dk |
| 47 | 46 | Kalkan Jeneratörü 46, Revir 46 | 13.200.000 | 4.620.000 | 750 | 32 g 22 sa 15 dk | 22.928 | 106.260 EN | 308 g 22 sa 49 dk |
| 48 | 47 | Kalkan Jeneratörü 47, İttifak Merkezi 47 | 15.200.000 | 5.310.000 | 840 | 35 g 17 sa | 24.360 | 112.470 EN | 344 g 15 sa 49 dk |
| 49 | 48 | Kalkan Jeneratörü 48, Araştırma Merkezi 48 | 17.400.000 | 6.090.000 | 940 | 38 g 17 sa 30 dk | 25.883 | 119.043 EN | 383 g 9 sa 19 dk |
| 50 | 49 | Kalkan Jeneratörü 49, Askeri Kubbe 49 | 20.000.000 | 7.000.000 | 1.050 | 42 g | 27.500 | 126.000 EN | 425 g 9 sa 19 dk |

## Bugünkü oyunla karşılaştırma (catalog v8)

Kodda bugün: en fazla 1 adet, Reaktör 0 ile açılır, 50 seviye; 1 → 50 toplam ham süre 20 sa. Hedef tasarımda aynı yol 425 g 9 sa 19 dk sürer.

| Sv | v8 Titanyum | v8 süre | Hedef Titanyum | Hedef süre |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 100 | 30 sn | — | — |
| 5 | 141 | 47 sn | 700 | 60 sn |
| 10 | 217 | 83 sn | 5.000 | 30 dk |
| 15 | 334 | 2 dk 27 sn | 30.000 | 4 sa |
| 20 | 514 | 4 dk 18 sn | 130.000 | 14 sa |
| 25 | 791 | 7 dk 35 sn | 420.000 | 2 g |
| 30 | 1.217 | 13 dk 22 sn | 1.100.000 | 4 g 12 sa |
| 35 | 1.873 | 23 dk 34 sn | 2.500.000 | 9 g 12 sa |
| 40 | 2.882 | 41 dk 32 sn | 5.000.000 | 17 g |
| 45 | 4.434 | 1 sa 13 dk | 10.000.000 | 28 g |
| 50 | 6.822 | 2 sa 9 dk | 20.000.000 | 42 g |


← [03 · Binalar](../03_binalar.md)
