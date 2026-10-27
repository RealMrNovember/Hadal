<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Radar Merkezi

`radar_center` · Askeri · 🟡

> Aktif ve pasif sonar dizisi; çevreyi dinler, sonar dronlarını yönetir.

**İşlev.** Yaklaşan saldırıları erken haber verir, sisi açan sonar dronlarını gönderir, Tür Kataloğu taramaları yapar.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 4 |
| Adet | 1 |
| Halka | 3–12 |
| En yüksek seviye | 50 |
| Tüketim (saatlik) | Sv 1: 30 EN → Sv 50: 5.400 EN |
| Toplam maliyet (1 → en yüksek) | 30.043.657 Titanyum |
| Toplam ham süre | 85 g 1 sa 45 dk |
| Toplam güç | 115.250 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Seyyar Dinleme Direği | Kulübe ve bantlanmış direk, eğri ikinci el çanak. Kehribar pencere, düzensiz dönüş. |
| 10–19 | Perçin | Perçinli Sonar İstasyonu | Kulübe perçinli, direk kafes iskeleye döner; çanak dengeli ve düzenli döner. |
| 20–29 | Basınç | Basınçlı Sonar Kulesi | Kulübe basınç küresine dönüşür, direk uzar, ikinci küçük çanak eklenir. Camgöbeği gösterge ışıkları. |
| 30–39 | Alaşım | Alaşım Dinleme Çanağı | Süperalaşım kule üstünde büyük çanak ve faz dizili sonar panelleri; iki çanak ters yönde döner, mor ışık. |
| 40–49 | Biyomekanik | Kadim Kulak | Çanak kitin bir kulak kepçesi gibi kıvrılır; dinleyen zar hafifçe titreşir. Turkuaz damarlar. |
| 50 | Rezonans | Yankı Tacı | Çanaklar taç biçiminde dizilir; her sonar atışı görünür bir ışık halkası olarak yayılır. |

## Seviye kilitleri

| Sv | Açılan |
| ---: | --- |
| 1 | 1 sonar dronu; saldırı uyarısı |
| 10 | Saldıranın asker sayısı görünür |
| 15 | 2. sonar dronu |
| 20 | Saldıran kahramanlar görünür |
| 30 | 3. sonar dronu; asker türü dağılımı görünür |
| 40 | Asker kademeleri ve takviyeler görünür |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Süre | Güç | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 4 | 12 | 1 sn | 15 | 1 sn |
| 2 | 4 | 20 | 1 sn | 20 | 2 sn |
| 3 | 4 | 35 | 3 sn | 26 | 5 sn |
| 4 | 4 | 70 | 6 sn | 34 | 11 sn |
| 5 | 5 | 140 | 12 sn | 45 | 23 sn |
| 6 | 6 | 205 | 24 sn | 60 | 47 sn |
| 7 | 7 | 305 | 48 sn | 79 | 95 sn |
| 8 | 8 | 455 | 96 sn | 104 | 3 dk 11 sn |
| 9 | 9 | 675 | 3 dk | 137 | 6 dk 11 sn |
| 10 | 10 | 1.000 | 6 dk | 180 | 12 dk 11 sn |
| 11 | 11 | 1.430 | 10 dk | 208 | 22 dk 11 sn |
| 12 | 12 | 2.050 | 16 dk | 239 | 38 dk 11 sn |
| 13 | 13 | 2.930 | 23 dk | 276 | 1 sa 1 dk |
| 14 | 14 | 4.190 | 33 dk | 319 | 1 sa 34 dk |
| 15 | 15 | 6.000 | 48 dk | 367 | 2 sa 22 dk |
| 16 | 16 | 8.040 | 1 sa 2 dk | 424 | 3 sa 24 dk |
| 17 | 17 | 10.800 | 1 sa 19 dk | 489 | 4 sa 43 dk |
| 18 | 18 | 14.500 | 1 sa 42 dk | 564 | 6 sa 25 dk |
| 19 | 19 | 19.400 | 2 sa 10 dk | 650 | 8 sa 35 dk |
| 20 | 20 | 26.000 | 2 sa 50 dk | 750 | 11 sa 25 dk |
| 21 | 21 | 32.900 | 3 sa 35 dk | 831 | 15 sa |
| 22 | 22 | 41.600 | 4 sa 35 dk | 921 | 19 sa 35 dk |
| 23 | 23 | 52.500 | 5 sa 50 dk | 1.021 | 1 g 1 sa 25 dk |
| 24 | 24 | 66.400 | 7 sa 30 dk | 1.132 | 1 g 8 sa 55 dk |
| 25 | 25 | 84.000 | 9 sa 35 dk | 1.255 | 1 g 18 sa 30 dk |
| 26 | 26 | 102.000 | 11 sa 15 dk | 1.391 | 2 g 5 sa 45 dk |
| 27 | 27 | 123.000 | 13 sa 15 dk | 1.542 | 2 g 19 sa |
| 28 | 28 | 150.000 | 15 sa 35 dk | 1.709 | 3 g 10 sa 35 dk |
| 29 | 29 | 181.000 | 18 sa 20 dk | 1.895 | 4 g 4 sa 55 dk |
| 30 | 30 | 220.000 | 21 sa 35 dk | 2.100 | 5 g 2 sa 30 dk |
| 31 | 31 | 259.000 | 1 g 1 sa | 2.266 | 6 g 3 sa 30 dk |
| 32 | 32 | 306.000 | 1 g 5 sa | 2.446 | 7 g 8 sa 30 dk |
| 33 | 33 | 360.000 | 1 g 9 sa 45 dk | 2.639 | 8 g 18 sa 15 dk |
| 34 | 34 | 424.000 | 1 g 15 sa 15 dk | 2.849 | 10 g 9 sa 30 dk |
| 35 | 35 | 500.000 | 1 g 21 sa 30 dk | 3.074 | 12 g 7 sa |
| 36 | 36 | 574.000 | 2 g 3 sa 15 dk | 3.318 | 14 g 10 sa 15 dk |
| 37 | 37 | 660.000 | 2 g 9 sa 30 dk | 3.580 | 16 g 19 sa 45 dk |
| 38 | 38 | 758.000 | 2 g 16 sa 45 dk | 3.864 | 19 g 12 sa 30 dk |
| 39 | 39 | 871.000 | 3 g 45 dk | 4.170 | 22 g 13 sa 15 dk |
| 40 | 40 | 1.000.000 | 3 g 9 sa 30 dk | 4.500 | 25 g 22 sa 45 dk |
| 41 | 41 | 1.150.000 | 3 g 18 sa 15 dk | 4.781 | 29 g 17 sa |
| 42 | 42 | 1.320.000 | 4 g 3 sa 45 dk | 5.080 | 33 g 20 sa 45 dk |
| 43 | 43 | 1.520.000 | 4 g 14 sa | 5.397 | 38 g 10 sa 45 dk |
| 44 | 44 | 1.740.000 | 5 g 1 sa 45 dk | 5.735 | 43 g 12 sa 30 dk |
| 45 | 45 | 2.000.000 | 5 g 14 sa 30 dk | 6.093 | 49 g 3 sa |
| 46 | 46 | 2.300.000 | 6 g 1 sa 45 dk | 6.474 | 55 g 4 sa 45 dk |
| 47 | 47 | 2.640.000 | 6 g 14 sa | 6.878 | 61 g 18 sa 45 dk |
| 48 | 48 | 3.030.000 | 7 g 3 sa 30 dk | 7.308 | 68 g 22 sa 15 dk |
| 49 | 49 | 3.480.000 | 7 g 18 sa | 7.765 | 76 g 16 sa 15 dk |
| 50 | 50 | 4.000.000 | 8 g 9 sa 30 dk | 8.250 | 85 g 1 sa 45 dk |

## Bugünkü oyunla karşılaştırma (catalog v8)

Kodda bugün: en fazla 1 adet, Reaktör 4 ile açılır, 50 seviye; 1 → 50 toplam ham süre 1 g 6 sa. Hedef tasarımda aynı yol 85 g 1 sa 45 dk sürer.

| Sv | v8 Titanyum | v8 süre | Hedef Titanyum | Hedef süre |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 160 | 45 sn | 12 | 1 sn |
| 5 | 226 | 71 sn | 140 | 12 sn |
| 10 | 348 | 2 dk 5 sn | 1.000 | 6 dk |
| 15 | 535 | 3 dk 40 sn | 6.000 | 48 dk |
| 20 | 823 | 6 dk 28 sn | 26.000 | 2 sa 50 dk |
| 25 | 1.266 | 11 dk 23 sn | 84.000 | 9 sa 35 dk |
| 30 | 1.948 | 20 dk 4 sn | 220.000 | 21 sa 35 dk |
| 35 | 2.997 | 35 dk 21 sn | 500.000 | 1 g 21 sa 30 dk |
| 40 | 4.611 | 1 sa 2 dk | 1.000.000 | 3 g 9 sa 30 dk |
| 45 | 7.094 | 1 sa 49 dk | 2.000.000 | 5 g 14 sa 30 dk |
| 50 | 10.915 | 3 sa 13 dk | 4.000.000 | 8 g 9 sa 30 dk |


← [03 · Binalar](../03_binalar.md)
