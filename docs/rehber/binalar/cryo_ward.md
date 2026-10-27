<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Kriyo Bakım Ünitesi

`cryo_ward` · Askeri · ⬜

> Ağır yaralıları dondurarak hayatta tutan ünite.

**İşlev.** Savunmada Revir taştığında ölecek askerlerin %50'sini kurtarır; kurtarılabilecek asker sayısı seviyeyle artar.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 16 |
| Adet | 1 |
| Halka | 2–12 |
| En yüksek seviye | 50 |
| Toplam maliyet (1 → en yüksek) | 37.550.610 Titanyum · 15.024.909 Biyokütle |
| Toplam ham süre | 85 g 1 sa 45 dk |
| Toplam güç | 76.834 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Yama Soğuk Odası | Soğutucu dolaplar, buz tutmuş borular, buğulu tek pencere. |
| 10–19 | Perçin | Perçinli Kriyo Ünitesi | Perçin kiti: dolaplar perçinli raflara alınır, camgöbeği sıcaklık şeritleri. |
| 20–29 | Basınç | Basınçlı Kriyo Dizisi | Basınçlı kriyo kapsül dizisi; konik pencerelerden donmuş silüetler, soğuk buhar. |
| 30–39 | Alaşım | Alaşım Kriyo Kulesi | Alaşım kiti: süperalaşım soğutma kulesi, ters çalışan eşanjörler, mor ışık. |
| 40–49 | Biyomekanik | Kitin Kozalar | Kapsüller kitin kozalara döner; koza zarları soğuk buharla örtülü. |
| 50 | Rezonans | Ninni Kozaları | Rezonans kiti: kozalar ninni temposunda, çok yavaş ışıldar. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süre | Güç | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 16 | 15 | 6 | 1 sn | 10 | 1 sn |
| 2 | 16 | 25 | 10 | 1 sn | 13 | 2 sn |
| 3 | 16 | 45 | 18 | 3 sn | 17 | 5 sn |
| 4 | 16 | 90 | 35 | 6 sn | 23 | 11 sn |
| 5 | 16 | 175 | 70 | 12 sn | 30 | 23 sn |
| 6 | 16 | 260 | 105 | 24 sn | 40 | 47 sn |
| 7 | 16 | 385 | 155 | 48 sn | 52 | 95 sn |
| 8 | 16 | 570 | 230 | 96 sn | 69 | 3 dk 11 sn |
| 9 | 16 | 845 | 335 | 3 dk | 91 | 6 dk 11 sn |
| 10 | 16 | 1.250 | 500 | 6 dk | 120 | 12 dk 11 sn |
| 11 | 16 | 1.790 | 715 | 10 dk | 138 | 22 dk 11 sn |
| 12 | 16 | 2.560 | 1.020 | 16 dk | 160 | 38 dk 11 sn |
| 13 | 16 | 3.660 | 1.470 | 23 dk | 184 | 1 sa 1 dk |
| 14 | 16 | 5.240 | 2.100 | 33 dk | 212 | 1 sa 34 dk |
| 15 | 16 | 7.500 | 3.000 | 48 dk | 245 | 2 sa 22 dk |
| 16 | 16 | 10.100 | 4.020 | 1 sa 2 dk | 283 | 3 sa 24 dk |
| 17 | 17 | 13.500 | 5.390 | 1 sa 19 dk | 326 | 4 sa 43 dk |
| 18 | 18 | 18.100 | 7.230 | 1 sa 42 dk | 376 | 6 sa 25 dk |
| 19 | 19 | 24.200 | 9.700 | 2 sa 10 dk | 434 | 8 sa 35 dk |
| 20 | 20 | 32.500 | 13.000 | 2 sa 50 dk | 500 | 11 sa 25 dk |
| 21 | 21 | 41.100 | 16.400 | 3 sa 35 dk | 554 | 15 sa |
| 22 | 22 | 52.000 | 20.800 | 4 sa 35 dk | 614 | 19 sa 35 dk |
| 23 | 23 | 65.700 | 26.300 | 5 sa 50 dk | 681 | 1 g 1 sa 25 dk |
| 24 | 24 | 83.000 | 33.200 | 7 sa 30 dk | 755 | 1 g 8 sa 55 dk |
| 25 | 25 | 105.000 | 42.000 | 9 sa 35 dk | 837 | 1 g 18 sa 30 dk |
| 26 | 26 | 127.000 | 50.900 | 11 sa 15 dk | 927 | 2 g 5 sa 45 dk |
| 27 | 27 | 154.000 | 61.700 | 13 sa 15 dk | 1.028 | 2 g 19 sa |
| 28 | 28 | 187.000 | 74.800 | 15 sa 35 dk | 1.139 | 3 g 10 sa 35 dk |
| 29 | 29 | 227.000 | 90.700 | 18 sa 20 dk | 1.263 | 4 g 4 sa 55 dk |
| 30 | 30 | 275.000 | 110.000 | 21 sa 35 dk | 1.400 | 5 g 2 sa 30 dk |
| 31 | 31 | 324.000 | 130.000 | 1 g 1 sa | 1.511 | 6 g 3 sa 30 dk |
| 32 | 32 | 382.000 | 153.000 | 1 g 5 sa | 1.631 | 7 g 8 sa 30 dk |
| 33 | 33 | 450.000 | 180.000 | 1 g 9 sa 45 dk | 1.760 | 8 g 18 sa 15 dk |
| 34 | 34 | 530.000 | 212.000 | 1 g 15 sa 15 dk | 1.899 | 10 g 9 sa 30 dk |
| 35 | 35 | 625.000 | 250.000 | 1 g 21 sa 30 dk | 2.049 | 12 g 7 sa |
| 36 | 36 | 718.000 | 287.000 | 2 g 3 sa 15 dk | 2.212 | 14 g 10 sa 15 dk |
| 37 | 37 | 825.000 | 330.000 | 2 g 9 sa 30 dk | 2.387 | 16 g 19 sa 45 dk |
| 38 | 38 | 947.000 | 379.000 | 2 g 16 sa 45 dk | 2.576 | 19 g 12 sa 30 dk |
| 39 | 39 | 1.090.000 | 435.000 | 3 g 45 dk | 2.780 | 22 g 13 sa 15 dk |
| 40 | 40 | 1.250.000 | 500.000 | 3 g 9 sa 30 dk | 3.000 | 25 g 22 sa 45 dk |
| 41 | 41 | 1.440.000 | 574.000 | 3 g 18 sa 15 dk | 3.187 | 29 g 17 sa |
| 42 | 42 | 1.650.000 | 660.000 | 4 g 3 sa 45 dk | 3.387 | 33 g 20 sa 45 dk |
| 43 | 43 | 1.890.000 | 758.000 | 4 g 14 sa | 3.598 | 38 g 10 sa 45 dk |
| 44 | 44 | 2.180.000 | 871.000 | 5 g 1 sa 45 dk | 3.823 | 43 g 12 sa 30 dk |
| 45 | 45 | 2.500.000 | 1.000.000 | 5 g 14 sa 30 dk | 4.062 | 49 g 3 sa |
| 46 | 46 | 2.870.000 | 1.150.000 | 6 g 1 sa 45 dk | 4.316 | 55 g 4 sa 45 dk |
| 47 | 47 | 3.300.000 | 1.320.000 | 6 g 14 sa | 4.586 | 61 g 18 sa 45 dk |
| 48 | 48 | 3.790.000 | 1.520.000 | 7 g 3 sa 30 dk | 4.872 | 68 g 22 sa 15 dk |
| 49 | 49 | 4.350.000 | 1.740.000 | 7 g 18 sa | 5.177 | 76 g 16 sa 15 dk |
| 50 | 50 | 5.000.000 | 2.000.000 | 8 g 9 sa 30 dk | 5.500 | 85 g 1 sa 45 dk |


← [03 · Binalar](../03_binalar.md)
