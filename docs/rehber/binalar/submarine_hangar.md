<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Denizaltı Hangarı

`submarine_hangar` · Askeri · ⬜

> Bir denizaltının ve mürettebatının sefer hazırlığı yaptığı hangar.

**İşlev.** Her hangar bir yürüyüş kuyruğudur (en fazla 4). Seviye, denizaltının asker kapasitesini artırır.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 4, 2. örnek Reaktör 10, 3. örnek Reaktör 18, 4. örnek Reaktör 26 |
| Adet | 4 |
| Halka | 2–12 |
| En yüksek seviye | 50 |
| Toplam maliyet (1 → en yüksek) | 30.043.657 Titanyum · 7.510.285 Biyokütle |
| Toplam ham süre | 85 g 1 sa 45 dk |
| Toplam güç | 76.834 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Yama Bağlama Rıhtımı | Bağlama kazıklarına iple tutturulmuş açık rıhtım; küçük denizaltı, sallanan iş lambası. |
| 10–19 | Perçin | Perçinli Hangar | Perçin kiti: rıhtımı perçinli bir iskele çevreler, yükleme vinci eklenir. |
| 20–29 | Basınç | Basınç Hangarı | Kapalı basınç hangarı: kemerli tavan, konik pencere sırası, denizaltı içeride. |
| 30–39 | Alaşım | Alaşım Sefer Hangarı | Alaşım kiti: süperalaşım kapılar, sonar dizisi, hareketli yükleme köprüsü. |
| 40–49 | Biyomekanik | Kitin Sefer Yuvası | Hangar kitin bir yuvaya dönüşür, kemerleri kaburga olur. Turkuaz rehber ışıklar. |
| 50 | Rezonans | Işıyan Sefer Kapısı | Rezonans kiti: denizaltı çıkarken önündeki ışık yolu Çekirdek nabzıyla yanar. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süre | Güç | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 4 | 12 | 3 | 1 sn | 10 | 1 sn |
| 2 | 4 | 20 | 5 | 1 sn | 13 | 2 sn |
| 3 | 4 | 35 | 9 | 3 sn | 17 | 5 sn |
| 4 | 4 | 70 | 18 | 6 sn | 23 | 11 sn |
| 5 | 5 | 140 | 35 | 12 sn | 30 | 23 sn |
| 6 | 6 | 205 | 50 | 24 sn | 40 | 47 sn |
| 7 | 7 | 305 | 75 | 48 sn | 52 | 95 sn |
| 8 | 8 | 455 | 115 | 96 sn | 69 | 3 dk 11 sn |
| 9 | 9 | 675 | 170 | 3 dk | 91 | 6 dk 11 sn |
| 10 | 10 | 1.000 | 250 | 6 dk | 120 | 12 dk 11 sn |
| 11 | 11 | 1.430 | 360 | 10 dk | 138 | 22 dk 11 sn |
| 12 | 12 | 2.050 | 510 | 16 dk | 160 | 38 dk 11 sn |
| 13 | 13 | 2.930 | 735 | 23 dk | 184 | 1 sa 1 dk |
| 14 | 14 | 4.190 | 1.050 | 33 dk | 212 | 1 sa 34 dk |
| 15 | 15 | 6.000 | 1.500 | 48 dk | 245 | 2 sa 22 dk |
| 16 | 16 | 8.040 | 2.010 | 1 sa 2 dk | 283 | 3 sa 24 dk |
| 17 | 17 | 10.800 | 2.700 | 1 sa 19 dk | 326 | 4 sa 43 dk |
| 18 | 18 | 14.500 | 3.620 | 1 sa 42 dk | 376 | 6 sa 25 dk |
| 19 | 19 | 19.400 | 4.850 | 2 sa 10 dk | 434 | 8 sa 35 dk |
| 20 | 20 | 26.000 | 6.500 | 2 sa 50 dk | 500 | 11 sa 25 dk |
| 21 | 21 | 32.900 | 8.220 | 3 sa 35 dk | 554 | 15 sa |
| 22 | 22 | 41.600 | 10.400 | 4 sa 35 dk | 614 | 19 sa 35 dk |
| 23 | 23 | 52.500 | 13.100 | 5 sa 50 dk | 681 | 1 g 1 sa 25 dk |
| 24 | 24 | 66.400 | 16.600 | 7 sa 30 dk | 755 | 1 g 8 sa 55 dk |
| 25 | 25 | 84.000 | 21.000 | 9 sa 35 dk | 837 | 1 g 18 sa 30 dk |
| 26 | 26 | 102.000 | 25.500 | 11 sa 15 dk | 927 | 2 g 5 sa 45 dk |
| 27 | 27 | 123.000 | 30.900 | 13 sa 15 dk | 1.028 | 2 g 19 sa |
| 28 | 28 | 150.000 | 37.400 | 15 sa 35 dk | 1.139 | 3 g 10 sa 35 dk |
| 29 | 29 | 181.000 | 45.400 | 18 sa 20 dk | 1.263 | 4 g 4 sa 55 dk |
| 30 | 30 | 220.000 | 55.000 | 21 sa 35 dk | 1.400 | 5 g 2 sa 30 dk |
| 31 | 31 | 259.000 | 64.800 | 1 g 1 sa | 1.511 | 6 g 3 sa 30 dk |
| 32 | 32 | 306.000 | 76.400 | 1 g 5 sa | 1.631 | 7 g 8 sa 30 dk |
| 33 | 33 | 360.000 | 90.000 | 1 g 9 sa 45 dk | 1.760 | 8 g 18 sa 15 dk |
| 34 | 34 | 424.000 | 106.000 | 1 g 15 sa 15 dk | 1.899 | 10 g 9 sa 30 dk |
| 35 | 35 | 500.000 | 125.000 | 1 g 21 sa 30 dk | 2.049 | 12 g 7 sa |
| 36 | 36 | 574.000 | 144.000 | 2 g 3 sa 15 dk | 2.212 | 14 g 10 sa 15 dk |
| 37 | 37 | 660.000 | 165.000 | 2 g 9 sa 30 dk | 2.387 | 16 g 19 sa 45 dk |
| 38 | 38 | 758.000 | 189.000 | 2 g 16 sa 45 dk | 2.576 | 19 g 12 sa 30 dk |
| 39 | 39 | 871.000 | 218.000 | 3 g 45 dk | 2.780 | 22 g 13 sa 15 dk |
| 40 | 40 | 1.000.000 | 250.000 | 3 g 9 sa 30 dk | 3.000 | 25 g 22 sa 45 dk |
| 41 | 41 | 1.150.000 | 287.000 | 3 g 18 sa 15 dk | 3.187 | 29 g 17 sa |
| 42 | 42 | 1.320.000 | 330.000 | 4 g 3 sa 45 dk | 3.387 | 33 g 20 sa 45 dk |
| 43 | 43 | 1.520.000 | 379.000 | 4 g 14 sa | 3.598 | 38 g 10 sa 45 dk |
| 44 | 44 | 1.740.000 | 435.000 | 5 g 1 sa 45 dk | 3.823 | 43 g 12 sa 30 dk |
| 45 | 45 | 2.000.000 | 500.000 | 5 g 14 sa 30 dk | 4.062 | 49 g 3 sa |
| 46 | 46 | 2.300.000 | 574.000 | 6 g 1 sa 45 dk | 4.316 | 55 g 4 sa 45 dk |
| 47 | 47 | 2.640.000 | 660.000 | 6 g 14 sa | 4.586 | 61 g 18 sa 45 dk |
| 48 | 48 | 3.030.000 | 758.000 | 7 g 3 sa 30 dk | 4.872 | 68 g 22 sa 15 dk |
| 49 | 49 | 3.480.000 | 871.000 | 7 g 18 sa | 5.177 | 76 g 16 sa 15 dk |
| 50 | 50 | 4.000.000 | 1.000.000 | 8 g 9 sa 30 dk | 5.500 | 85 g 1 sa 45 dk |


← [03 · Binalar](../03_binalar.md)
