<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Mühendislik Bölmesi

`engineering_bay` · Destek · ⬜

> Mühendislerin dinlendiği ve aletlerini sakladığı atölye.

**İşlev.** Ücretsiz bitirme süresini 5 dakikadan 15 dakikaya kadar uzatır.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 4 |
| Adet | 1 |
| Halka | 1–12 |
| En yüksek seviye | 50 |
| Toplam maliyet (1 → en yüksek) | 22.528.654 Titanyum |
| Toplam ham süre | 63 g 19 sa 24 dk |
| Toplam güç | 76.834 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1–9 | Yama | Seyyar Alet Odası | Alet dolapları ve tezgâh, yamalı levha çatı; kehribar lamba, tek robot kol. |
| 10–19 | Perçin | Perçinli Atölye | Perçin kiti: düzenli raf duvarı, cıvatalı vinç rayı, camgöbeği şerit. |
| 20–29 | Basınç | Basınç Bakım Bölmesi | Atölye basınç gövdesine taşınır; iki robot kol, konik pencereli kontrol odası. |
| 30–39 | Alaşım | Alaşım Mühendislik Bölmesi | Alaşım kiti: süperalaşım kaplama, eşanjörlü kaynak istasyonu, mor ark ışığı. |
| 40–49 | Biyomekanik | Kaynaşık Ustahane | Robot kollar kitin eklemlere kavuşur; atölye canlı bir el gibi çalışır. Turkuaz. |
| 50 | Rezonans | Uyanmış Ustahane | Rezonans kiti: kollar Çekirdek ritmiyle aynı anda hareket eder; kaynak kıvılcımları beyaz-camgöbeği. |

## Seviye kilitleri

| Sv | Açılan |
| ---: | --- |
| 5 | Ücretsiz bitirme 6 dk |
| 10 | Ücretsiz bitirme 7 dk |
| 15 | Ücretsiz bitirme 8 dk |
| 20 | Ücretsiz bitirme 9 dk |
| 25 | Ücretsiz bitirme 10 dk |
| 30 | Ücretsiz bitirme 11 dk |
| 35 | Ücretsiz bitirme 12 dk |
| 40 | Ücretsiz bitirme 13 dk |
| 45 | Ücretsiz bitirme 14 dk |
| 50 | Ücretsiz bitirme 15 dk |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Süre | Güç | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 4 | 9 | 1 sn | 10 | 1 sn |
| 2 | 4 | 15 | 1 sn | 13 | 2 sn |
| 3 | 4 | 25 | 2 sn | 17 | 4 sn |
| 4 | 4 | 50 | 4 sn | 23 | 8 sn |
| 5 | 5 | 105 | 9 sn | 30 | 17 sn |
| 6 | 6 | 155 | 18 sn | 40 | 35 sn |
| 7 | 7 | 230 | 36 sn | 52 | 71 sn |
| 8 | 8 | 340 | 72 sn | 69 | 2 dk 23 sn |
| 9 | 9 | 505 | 2 dk | 91 | 4 dk 23 sn |
| 10 | 10 | 750 | 4 dk | 120 | 8 dk 23 sn |
| 11 | 11 | 1.070 | 7 dk | 138 | 15 dk 23 sn |
| 12 | 12 | 1.540 | 12 dk | 160 | 27 dk 23 sn |
| 13 | 13 | 2.200 | 17 dk | 184 | 44 dk 23 sn |
| 14 | 14 | 3.140 | 25 dk | 212 | 1 sa 9 dk |
| 15 | 15 | 4.500 | 36 dk | 245 | 1 sa 45 dk |
| 16 | 16 | 6.030 | 46 dk | 283 | 2 sa 31 dk |
| 17 | 17 | 8.090 | 59 dk | 326 | 3 sa 30 dk |
| 18 | 18 | 10.800 | 1 sa 16 dk | 376 | 4 sa 46 dk |
| 19 | 19 | 14.500 | 1 sa 38 dk | 434 | 6 sa 24 dk |
| 20 | 20 | 19.500 | 2 sa 5 dk | 500 | 8 sa 29 dk |
| 21 | 21 | 24.700 | 2 sa 40 dk | 554 | 11 sa 9 dk |
| 22 | 22 | 31.200 | 3 sa 25 dk | 614 | 14 sa 34 dk |
| 23 | 23 | 39.400 | 4 sa 25 dk | 681 | 18 sa 59 dk |
| 24 | 24 | 49.800 | 5 sa 40 dk | 755 | 1 g 39 dk |
| 25 | 25 | 63.000 | 7 sa 10 dk | 837 | 1 g 7 sa 49 dk |
| 26 | 26 | 76.400 | 8 sa 30 dk | 927 | 1 g 16 sa 19 dk |
| 27 | 27 | 92.600 | 10 sa | 1.028 | 2 g 2 sa 19 dk |
| 28 | 28 | 112.000 | 11 sa 45 dk | 1.139 | 2 g 14 sa 4 dk |
| 29 | 29 | 136.000 | 13 sa 45 dk | 1.263 | 3 g 3 sa 49 dk |
| 30 | 30 | 165.000 | 16 sa 10 dk | 1.400 | 3 g 19 sa 59 dk |
| 31 | 31 | 194.000 | 18 sa 50 dk | 1.511 | 4 g 14 sa 49 dk |
| 32 | 32 | 229.000 | 21 sa 50 dk | 1.631 | 5 g 12 sa 39 dk |
| 33 | 33 | 270.000 | 1 g 1 sa 15 dk | 1.760 | 6 g 13 sa 54 dk |
| 34 | 34 | 318.000 | 1 g 5 sa 30 dk | 1.899 | 7 g 19 sa 24 dk |
| 35 | 35 | 375.000 | 1 g 10 sa 15 dk | 2.049 | 9 g 5 sa 39 dk |
| 36 | 36 | 431.000 | 1 g 14 sa 30 dk | 2.212 | 10 g 20 sa 9 dk |
| 37 | 37 | 495.000 | 1 g 19 sa 15 dk | 2.387 | 12 g 15 sa 24 dk |
| 38 | 38 | 568.000 | 2 g 30 dk | 2.576 | 14 g 15 sa 54 dk |
| 39 | 39 | 653.000 | 2 g 6 sa 30 dk | 2.780 | 16 g 22 sa 24 dk |
| 40 | 40 | 750.000 | 2 g 13 sa 15 dk | 3.000 | 19 g 11 sa 39 dk |
| 41 | 41 | 862.000 | 2 g 19 sa 30 dk | 3.187 | 22 g 7 sa 9 dk |
| 42 | 42 | 990.000 | 3 g 2 sa 45 dk | 3.387 | 25 g 9 sa 54 dk |
| 43 | 43 | 1.140.000 | 3 g 10 sa 30 dk | 3.598 | 28 g 20 sa 24 dk |
| 44 | 44 | 1.310.000 | 3 g 19 sa 15 dk | 3.823 | 32 g 15 sa 39 dk |
| 45 | 45 | 1.500.000 | 4 g 4 sa 45 dk | 4.062 | 36 g 20 sa 24 dk |
| 46 | 46 | 1.720.000 | 4 g 13 sa 15 dk | 4.316 | 41 g 9 sa 39 dk |
| 47 | 47 | 1.980.000 | 4 g 22 sa 30 dk | 4.586 | 46 g 8 sa 9 dk |
| 48 | 48 | 2.270.000 | 5 g 8 sa 30 dk | 4.872 | 51 g 16 sa 39 dk |
| 49 | 49 | 2.610.000 | 5 g 19 sa 30 dk | 5.177 | 57 g 12 sa 9 dk |
| 50 | 50 | 3.000.000 | 6 g 7 sa 15 dk | 5.500 | 63 g 19 sa 24 dk |


← [03 · Binalar](../03_binalar.md)
