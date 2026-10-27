<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Basınç Ambarı

`pressure_vault` · Depolama · ⬜

> Basınca dayanıklı, mühürlü depo.

**İşlev.** Her kaynak için ek kapasite verir ve bir kısmını yağmadan korur.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 3, 2. örnek Reaktör 10, 3. örnek Reaktör 20 |
| Adet | 3 |
| Halka | 1–12 |
| En yüksek seviye | 50 |
| Toplam maliyet (1 → en yüksek) | 30.043.657 Titanyum |
| Toplam ham süre | 85 g 1 sa 45 dk |
| Toplam güç | 76.834 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Yama Sandık Yığını | Halatla bağlanmış kasa ve konteyner yığını; tehlike boyası, kehribar lamba. |
| 10–19 | Perçin | Perçinli Ambar | Perçin kiti: kasalar perçinli raflara dizilir, kapılara flanşlı kilitler gelir. |
| 20–29 | Basınç | Basınç Ambarı | Kalın duvarlı basınç küresi ambar, dev bir mühür kapağı ve konik gözetleme penceresi. |
| 30–39 | Alaşım | Alaşım Kasa | Alaşım kiti: süperalaşım takviye halkaları ve mor kilit ışıkları. |
| 40–49 | Biyomekanik | Kadim Mahzen | Küre bazalt yastıklara gömülür, kitin kaburgalarla mühürlenir. Turkuaz damarlar. |
| 50 | Rezonans | Rezonans Mahzeni | Rezonans kiti: mühür kapağındaki Hadalit kilit nabızla ışıldar; kapak nefes alır gibi. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Süre | Güç | Ek kapasite (her kaynak) | Yağmadan korunan | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3 | 12 | 1 sn | 10 | 2.000 | 500 | 1 sn |
| 2 | 3 | 20 | 1 sn | 13 | 2.500 | 625 | 2 sn |
| 3 | 3 | 35 | 3 sn | 17 | 3.130 | 780 | 5 sn |
| 4 | 4 | 70 | 6 sn | 23 | 3.910 | 980 | 11 sn |
| 5 | 5 | 140 | 12 sn | 30 | 4.900 | 1.220 | 23 sn |
| 6 | 6 | 205 | 24 sn | 40 | 6.130 | 1.530 | 47 sn |
| 7 | 7 | 305 | 48 sn | 52 | 7.660 | 1.920 | 95 sn |
| 8 | 8 | 455 | 96 sn | 69 | 9.590 | 2.400 | 3 dk 11 sn |
| 9 | 9 | 675 | 3 dk | 91 | 12.000 | 3.000 | 6 dk 11 sn |
| 10 | 10 | 1.000 | 6 dk | 120 | 15.000 | 3.750 | 12 dk 11 sn |
| 11 | 11 | 1.430 | 10 dk | 138 | 20.200 | 5.060 | 22 dk 11 sn |
| 12 | 12 | 2.050 | 16 dk | 160 | 27.300 | 6.830 | 38 dk 11 sn |
| 13 | 13 | 2.930 | 23 dk | 184 | 36.800 | 9.210 | 1 sa 1 dk |
| 14 | 14 | 4.190 | 33 dk | 212 | 49.700 | 12.400 | 1 sa 34 dk |
| 15 | 15 | 6.000 | 48 dk | 245 | 67.100 | 16.800 | 2 sa 22 dk |
| 16 | 16 | 8.040 | 1 sa 2 dk | 283 | 90.500 | 22.600 | 3 sa 24 dk |
| 17 | 17 | 10.800 | 1 sa 19 dk | 326 | 122.000 | 30.500 | 4 sa 43 dk |
| 18 | 18 | 14.500 | 1 sa 42 dk | 376 | 165.000 | 41.200 | 6 sa 25 dk |
| 19 | 19 | 19.400 | 2 sa 10 dk | 434 | 222.000 | 55.600 | 8 sa 35 dk |
| 20 | 20 | 26.000 | 2 sa 50 dk | 500 | 300.000 | 75.000 | 11 sa 25 dk |
| 21 | 21 | 32.900 | 3 sa 35 dk | 554 | 369.000 | 92.300 | 15 sa |
| 22 | 22 | 41.600 | 4 sa 35 dk | 614 | 455.000 | 114.000 | 19 sa 35 dk |
| 23 | 23 | 52.500 | 5 sa 50 dk | 681 | 560.000 | 140.000 | 1 g 1 sa 25 dk |
| 24 | 24 | 66.400 | 7 sa 30 dk | 755 | 689.000 | 172.000 | 1 g 8 sa 55 dk |
| 25 | 25 | 84.000 | 9 sa 35 dk | 837 | 849.000 | 212.000 | 1 g 18 sa 30 dk |
| 26 | 26 | 102.000 | 11 sa 15 dk | 927 | 1.040.000 | 261.000 | 2 g 5 sa 45 dk |
| 27 | 27 | 123.000 | 13 sa 15 dk | 1.028 | 1.290.000 | 322.000 | 2 g 19 sa |
| 28 | 28 | 150.000 | 15 sa 35 dk | 1.139 | 1.580.000 | 396.000 | 3 g 10 sa 35 dk |
| 29 | 29 | 181.000 | 18 sa 20 dk | 1.263 | 1.950.000 | 487.000 | 4 g 4 sa 55 dk |
| 30 | 30 | 220.000 | 21 sa 35 dk | 1.400 | 2.400.000 | 600.000 | 5 g 2 sa 30 dk |
| 31 | 31 | 259.000 | 1 g 1 sa | 1.511 | 2.770.000 | 692.000 | 6 g 3 sa 30 dk |
| 32 | 32 | 306.000 | 1 g 5 sa | 1.631 | 3.190.000 | 798.000 | 7 g 8 sa 30 dk |
| 33 | 33 | 360.000 | 1 g 9 sa 45 dk | 1.760 | 3.680.000 | 921.000 | 8 g 18 sa 15 dk |
| 34 | 34 | 424.000 | 1 g 15 sa 15 dk | 1.899 | 4.250.000 | 1.060.000 | 10 g 9 sa 30 dk |
| 35 | 35 | 500.000 | 1 g 21 sa 30 dk | 2.049 | 4.900.000 | 1.220.000 | 12 g 7 sa |
| 36 | 36 | 574.000 | 2 g 3 sa 15 dk | 2.212 | 5.650.000 | 1.410.000 | 14 g 10 sa 15 dk |
| 37 | 37 | 660.000 | 2 g 9 sa 30 dk | 2.387 | 6.520.000 | 1.630.000 | 16 g 19 sa 45 dk |
| 38 | 38 | 758.000 | 2 g 16 sa 45 dk | 2.576 | 7.520.000 | 1.880.000 | 19 g 12 sa 30 dk |
| 39 | 39 | 871.000 | 3 g 45 dk | 2.780 | 8.670.000 | 2.170.000 | 22 g 13 sa 15 dk |
| 40 | 40 | 1.000.000 | 3 g 9 sa 30 dk | 3.000 | 10.000.000 | 2.500.000 | 25 g 22 sa 45 dk |
| 41 | 41 | 1.150.000 | 3 g 18 sa 15 dk | 3.187 | 11.500.000 | 2.870.000 | 29 g 17 sa |
| 42 | 42 | 1.320.000 | 4 g 3 sa 45 dk | 3.387 | 13.200.000 | 3.300.000 | 33 g 20 sa 45 dk |
| 43 | 43 | 1.520.000 | 4 g 14 sa | 3.598 | 15.200.000 | 3.790.000 | 38 g 10 sa 45 dk |
| 44 | 44 | 1.740.000 | 5 g 1 sa 45 dk | 3.823 | 17.400.000 | 4.350.000 | 43 g 12 sa 30 dk |
| 45 | 45 | 2.000.000 | 5 g 14 sa 30 dk | 4.062 | 20.000.000 | 5.000.000 | 49 g 3 sa |
| 46 | 46 | 2.300.000 | 6 g 1 sa 45 dk | 4.316 | 23.000.000 | 5.740.000 | 55 g 4 sa 45 dk |
| 47 | 47 | 2.640.000 | 6 g 14 sa | 4.586 | 26.400.000 | 6.600.000 | 61 g 18 sa 45 dk |
| 48 | 48 | 3.030.000 | 7 g 3 sa 30 dk | 4.872 | 30.300.000 | 7.580.000 | 68 g 22 sa 15 dk |
| 49 | 49 | 3.480.000 | 7 g 18 sa | 5.177 | 34.800.000 | 8.710.000 | 76 g 16 sa 15 dk |
| 50 | 50 | 4.000.000 | 8 g 9 sa 30 dk | 5.500 | 40.000.000 | 10.000.000 | 85 g 1 sa 45 dk |


← [03 · Binalar](../03_binalar.md)
