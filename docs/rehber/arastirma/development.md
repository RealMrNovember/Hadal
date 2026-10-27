<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Gelişim

`development` · Araştırma Merkezi · 13 teknoloji · 97 seviye

> İnşa ve araştırma hızı, ikinci inşaatçı ve ikinci tezgâh, Takviye, Aşırı Yük ve kurtarma görevleri: üssün iş temposu.

**Açılış.** Araştırma Merkezi 1 (Reaktör 3)

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [İnşaat Ustalığı](#construction-mastery) | 🟡 | 10 | İnşa hızı | %30 | 1–19 | 41.727 Titanyum · 15.003 Biyokütle | 8 sa 37 dk |
| [Araştırma Yöntemleri](#research-methods) | 🟡 | 10 | Araştırma hızı | %50 | 1–19 | 41.727 Titanyum · 15.003 Biyokütle | 8 sa 37 dk |
| [İkinci İnşaatçı](#second-builder) | 🟡 | 1 | İnşaatçı | 1 inşaatçı | 5–5 | 140 Titanyum · 50 Biyokütle | 21 sn |
| [İkinci Tezgâh](#second-workbench) | 🟡 | 1 | Araştırma Merkezi kuyruğu | 1 kuyruk | 8–8 | 455 Titanyum · 165 Biyokütle | 3 dk |
| [Takviye Ayarı](#boost-tuning) | ⬜ | 5 | Takviye süresi | %50 | 6–26 | 144.575 Titanyum · 51.990 Biyokütle | 1 g 4 sa 5 dk |
| [Kurtarma Planlaması](#salvage-planning) | ⬜ | 10 | Kurtarma görevi kazancı | %40 | 6–42 | 2.885.495 Titanyum · 1.039.055 Biyokütle | 17 g 17 sa 53 dk |
| [Hiperbarik Kaynak](#hyperbaric-welding) | ⬜ | 5 | Halka açma maliyeti | −%20 | 8–32 | 438.645 Titanyum · 157.735 Biyokütle | 3 g 4 sa 41 dk |
| [Aşırı Yük Soğutması](#overdrive-cooling) | ⬜ | 5 | Aşırı Yük soğuma süresi | −%30 | 9–29 | 271.665 Titanyum · 97.935 Biyokütle | 2 g 2 sa 18 dk |
| [Malzeme Verimliliği](#material-efficiency) | ⬜ | 10 | İnşa maliyeti | −%10 | 10–46 | 5.185.290 Titanyum · 1.865.980 Biyokütle | 28 g 8 sa 53 dk |
| [Numune Ekonomisi](#sample-economy) | ⬜ | 10 | Araştırma maliyeti | −%10 | 12–48 | 6.902.490 Titanyum · 2.484.795 Biyokütle | 35 g 3 sa 51 dk |
| [Vardiya Düzeni](#crew-rotation) | ⬜ | 10 | Atanan mürettebat verimi | %20 | 16–43 | 4.166.040 Titanyum · 1.499.280 Biyokütle | 25 g 6 sa 43 dk |
| [Modüler Montaj](#modular-assembly) | ⬜ | 10 | İnşa hızı | %10 | 20–47 | 7.455.500 Titanyum · 2.684.260 Biyokütle | 39 g 22 sa 10 dk |
| [Yağ Dengeli Elektronik](#oil-compensated-electronics) | ⬜ | 10 | Araştırma hızı | %10 | 21–48 | 8.597.300 Titanyum · 3.094.300 Biyokütle | 44 g 9 sa 55 dk |

<a id="construction-mastery"></a>
## İnşaat Ustalığı

`construction_mastery` · Basınçlı modüllerin montaj sırasını standartlaştıran ilk mühendislik dersi; her seviye inşa hızını artırır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 12 | — | 1 sn | %3 | 4 |
| 2 | 3 | 35 | 13 | 5 sn | %6 | 7 |
| 3 | 5 | 140 | 50 | 21 sn | %9 | 12 |
| 4 | 7 | 305 | 110 | 84 sn | %12 | 21 |
| 5 | 9 | 675 | 245 | 5 dk | %15 | 36 |
| 6 | 11 | 1.430 | 515 | 17 dk | %18 | 55 |
| 7 | 13 | 2.930 | 1.050 | 40 dk | %21 | 74 |
| 8 | 15 | 6.000 | 2.160 | 1 sa 24 dk | %24 | 98 |
| 9 | 17 | 10.800 | 3.880 | 2 sa 20 dk | %27 | 130 |
| 10 | 19 | 19.400 | 6.980 | 3 sa 50 dk | %30 | 173 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 1 | 80 Titanyum · 20 Biyokütle | 60 sn |
| 2 | 3 | 112 Titanyum · 28 Biyokütle | 90 sn |
| 3 | 5 | 157 Titanyum · 39 Biyokütle | 2 dk 15 sn |
| 4 | 7 | 220 Titanyum · 55 Biyokütle | 3 dk 22 sn |
| 5 | 9 | 307 Titanyum · 77 Biyokütle | 5 dk 4 sn |
| 6 | 11 | 430 Titanyum · 108 Biyokütle | 7 dk 36 sn |
| 7 | 13 | 602 Titanyum · 151 Biyokütle | 11 dk 23 sn |
| 8 | 15 | 843 Titanyum · 211 Biyokütle | 17 dk 5 sn |
| 9 | 17 | 1.181 Titanyum · 295 Biyokütle | 25 dk 38 sn |
| 10 | 19 | 1.653 Titanyum · 413 Biyokütle | 38 dk 27 sn |

<a id="research-methods"></a>
## Araştırma Yöntemleri

`research_methods` · Numune toplama, ölçüm ve raporlamayı tek protokolde birleştirir; her seviye araştırma hızını artırır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 12 | — | 1 sn | %5 | 4 |
| 2 | 3 | 35 | 13 | 5 sn | %10 | 7 |
| 3 | 5 | 140 | 50 | 21 sn | %15 | 12 |
| 4 | 7 | 305 | 110 | 84 sn | %20 | 21 |
| 5 | 9 | 675 | 245 | 5 dk | %25 | 36 |
| 6 | 11 | 1.430 | 515 | 17 dk | %30 | 55 |
| 7 | 13 | 2.930 | 1.050 | 40 dk | %35 | 74 |
| 8 | 15 | 6.000 | 2.160 | 1 sa 24 dk | %40 | 98 |
| 9 | 17 | 10.800 | 3.880 | 2 sa 20 dk | %45 | 130 |
| 10 | 19 | 19.400 | 6.980 | 3 sa 50 dk | %50 | 173 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 1 | 60 Titanyum · 30 Biyokütle | 60 sn |
| 2 | 3 | 84 Titanyum · 42 Biyokütle | 90 sn |
| 3 | 5 | 118 Titanyum · 59 Biyokütle | 2 dk 15 sn |
| 4 | 7 | 165 Titanyum · 82 Biyokütle | 3 dk 22 sn |
| 5 | 9 | 230 Titanyum · 115 Biyokütle | 5 dk 4 sn |
| 6 | 11 | 323 Titanyum · 161 Biyokütle | 7 dk 36 sn |
| 7 | 13 | 452 Titanyum · 226 Biyokütle | 11 dk 23 sn |
| 8 | 15 | 632 Titanyum · 316 Biyokütle | 17 dk 5 sn |
| 9 | 17 | 885 Titanyum · 443 Biyokütle | 25 dk 38 sn |
| 10 | 19 | 1.240 Titanyum · 620 Biyokütle | 38 dk 27 sn |

<a id="second-builder"></a>
## İkinci İnşaatçı

`second_builder` · İkinci bir montaj ekibi eğitilir: aynı anda iki inşaat yürür.

**Gerekli:** İnşaat Ustalığı 5

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 5 | 140 | 50 | 21 sn | 1 inşaatçı | 12 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 5 | 3.000 Titanyum · 800 Biyokütle | 30 dk |

<a id="second-workbench"></a>
## İkinci Tezgâh

`second_workbench` · Araştırma Merkezi'ne ikinci bir deney tezgâhı kurulur: merkezde aynı anda iki araştırma yürür.

**Gerekli:** Araştırma Yöntemleri 5

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 8 | 455 | 165 | 3 dk | 1 kuyruk | 28 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 8 | 5.000 Titanyum · 1.500 Biyokütle | 1 sa |

<a id="boost-tuning"></a>
## Takviye Ayarı

`boost_tuning` · Pompaları ve elektroliz hücrelerini zorlamadan çalıştıran ayar; Takviye daha uzun sürer.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 6 | 205 | 75 | 42 sn | %10 | 16 |
| 2 | 11 | 1.430 | 515 | 17 dk | %20 | 55 |
| 3 | 16 | 8.040 | 2.900 | 1 sa 48 dk | %30 | 113 |
| 4 | 21 | 32.900 | 11.800 | 6 sa 15 dk | %40 | 222 |
| 5 | 26 | 102.000 | 36.700 | 19 sa 45 dk | %50 | 371 |

<a id="salvage-planning"></a>
## Kurtarma Planlaması

`salvage_planning` · Dron Fabrikası'nın kurtarma görevlerini batimetri haritasıyla planlar; dronlar daha dolu döner.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 6 | 205 | 75 | 42 sn | %4 | 16 |
| 2 | 10 | 1.000 | 360 | 10 dk | %8 | 48 |
| 3 | 14 | 4.190 | 1.510 | 58 dk | %12 | 85 |
| 4 | 18 | 14.500 | 5.210 | 3 sa | %16 | 150 |
| 5 | 22 | 41.600 | 15.000 | 8 sa | %20 | 246 |
| 6 | 26 | 102.000 | 36.700 | 19 sa 45 dk | %24 | 371 |
| 7 | 30 | 220.000 | 79.200 | 1 g 13 sa 45 dk | %28 | 560 |
| 8 | 34 | 424.000 | 153.000 | 2 g 20 sa 45 dk | %32 | 760 |
| 9 | 38 | 758.000 | 273.000 | 4 g 17 sa 15 dk | %36 | 1.030 |
| 10 | 42 | 1.320.000 | 475.000 | 7 g 6 sa 15 dk | %40 | 1.355 |

<a id="hyperbaric-welding"></a>
## Hiperbarik Kaynak

`hyperbaric_welding` · Açık deniz boru hatlarında kullanılan, kuru basınç odasında yapılan kaynak tekniği; yeni halkaları daha ucuza açar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 8 | 455 | 165 | 3 dk | −%4 | 28 |
| 2 | 14 | 4.190 | 1.510 | 58 dk | −%8 | 85 |
| 3 | 20 | 26.000 | 9.360 | 4 sa 55 dk | −%12 | 200 |
| 4 | 26 | 102.000 | 36.700 | 19 sa 45 dk | −%16 | 371 |
| 5 | 32 | 306.000 | 110.000 | 2 g 3 sa | −%20 | 652 |

<a id="overdrive-cooling"></a>
## Aşırı Yük Soğutması

`overdrive_cooling` · Reaktör devrelerini çevredeki buz gibi dip suyuyla soğutan eşanjör; Aşırı Yük'ün soğuma süresini kısaltır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 9 | 675 | 245 | 5 dk | −%6 | 36 |
| 2 | 14 | 4.190 | 1.510 | 58 dk | −%12 | 85 |
| 3 | 19 | 19.400 | 6.980 | 3 sa 50 dk | −%18 | 173 |
| 4 | 24 | 66.400 | 23.900 | 13 sa 10 dk | −%24 | 302 |
| 5 | 29 | 181.000 | 65.300 | 1 g 8 sa 15 dk | −%30 | 505 |

<a id="material-efficiency"></a>
## Malzeme Verimliliği

`material_efficiency` · Kesim artıklarını eritip yeniden döken atölye düzeni; inşa maliyetini düşürür.

**Gerekli:** İnşaat Ustalığı 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 10 | 1.000 | 360 | 10 dk | −%1 | 48 |
| 2 | 14 | 4.190 | 1.510 | 58 dk | −%2 | 85 |
| 3 | 18 | 14.500 | 5.210 | 3 sa | −%3 | 150 |
| 4 | 22 | 41.600 | 15.000 | 8 sa | −%4 | 246 |
| 5 | 26 | 102.000 | 36.700 | 19 sa 45 dk | −%5 | 371 |
| 6 | 30 | 220.000 | 79.200 | 1 g 13 sa 45 dk | −%6 | 560 |
| 7 | 34 | 424.000 | 153.000 | 2 g 20 sa 45 dk | −%7 | 760 |
| 8 | 38 | 758.000 | 273.000 | 4 g 17 sa 15 dk | −%8 | 1.030 |
| 9 | 42 | 1.320.000 | 475.000 | 7 g 6 sa 15 dk | −%9 | 1.355 |
| 10 | 46 | 2.300.000 | 827.000 | 10 g 15 sa | −%10 | 1.726 |

<a id="sample-economy"></a>
## Numune Ekonomisi

`sample_economy` · Bir numuneden birden çok deney çıkaran laboratuvar düzeni; araştırma maliyetini düşürür.

**Gerekli:** Araştırma Yöntemleri 5

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 12 | 2.050 | 735 | 28 dk | −%1 | 64 |
| 2 | 16 | 8.040 | 2.900 | 1 sa 48 dk | −%2 | 113 |
| 3 | 20 | 26.000 | 9.360 | 4 sa 55 dk | −%3 | 200 |
| 4 | 24 | 66.400 | 23.900 | 13 sa 10 dk | −%4 | 302 |
| 5 | 28 | 150.000 | 53.900 | 1 g 3 sa 15 dk | −%5 | 456 |
| 6 | 32 | 306.000 | 110.000 | 2 g 3 sa | −%6 | 652 |
| 7 | 36 | 574.000 | 207.000 | 3 g 17 sa 45 dk | −%7 | 885 |
| 8 | 40 | 1.000.000 | 360.000 | 5 g 22 sa 45 dk | −%8 | 1.200 |
| 9 | 44 | 1.740.000 | 627.000 | 8 g 20 sa 45 dk | −%9 | 1.529 |
| 10 | 48 | 3.030.000 | 1.090.000 | 12 g 12 sa | −%10 | 1.949 |

<a id="crew-rotation"></a>
## Vardiya Düzeni

`crew_rotation` · Üç vardiyalı çalışma ve dinlenme planı; üretim binalarına atanan mürettebatın katkısını artırır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 16 | 8.040 | 2.900 | 1 sa 48 dk | %2 | 113 |
| 2 | 19 | 19.400 | 6.980 | 3 sa 50 dk | %4 | 173 |
| 3 | 22 | 41.600 | 15.000 | 8 sa | %6 | 246 |
| 4 | 25 | 84.000 | 30.200 | 16 sa 50 dk | %8 | 335 |
| 5 | 28 | 150.000 | 53.900 | 1 g 3 sa 15 dk | %10 | 456 |
| 6 | 31 | 259.000 | 93.300 | 1 g 20 sa | %12 | 604 |
| 7 | 34 | 424.000 | 153.000 | 2 g 20 sa 45 dk | %14 | 760 |
| 8 | 37 | 660.000 | 238.000 | 4 g 4 sa 45 dk | %16 | 955 |
| 9 | 40 | 1.000.000 | 360.000 | 5 g 22 sa 45 dk | %18 | 1.200 |
| 10 | 43 | 1.520.000 | 546.000 | 8 g 45 dk | %20 | 1.439 |

<a id="modular-assembly"></a>
## Modüler Montaj

`modular_assembly` · Modüller kuru havuzda hazırlanıp yerine tek parça indirilir; inşa hızı İnşaat Ustalığı'nın üstüne eklenir.

**Gerekli:** İnşaat Ustalığı 10

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 26.000 | 9.360 | 4 sa 55 dk | %1 | 200 |
| 2 | 23 | 52.500 | 18.900 | 10 sa 15 dk | %2 | 272 |
| 3 | 26 | 102.000 | 36.700 | 19 sa 45 dk | %3 | 371 |
| 4 | 29 | 181.000 | 65.300 | 1 g 8 sa 15 dk | %4 | 505 |
| 5 | 32 | 306.000 | 110.000 | 2 g 3 sa | %5 | 652 |
| 6 | 35 | 500.000 | 180.000 | 3 g 7 sa 45 dk | %6 | 820 |
| 7 | 38 | 758.000 | 273.000 | 4 g 17 sa 15 dk | %7 | 1.030 |
| 8 | 41 | 1.150.000 | 414.000 | 6 g 13 sa 45 dk | %8 | 1.275 |
| 9 | 44 | 1.740.000 | 627.000 | 8 g 20 sa 45 dk | %9 | 1.529 |
| 10 | 47 | 2.640.000 | 950.000 | 11 g 12 sa 30 dk | %10 | 1.834 |

<a id="oil-compensated-electronics"></a>
## Yağ Dengeli Elektronik

`oil_compensated_electronics` · Derin deniz araçlarında elektronik kartlar yağ dolu, basınç dengeli kutulara konur; laboratuvar cihazları daha az arıza verir.

**Gerekli:** Araştırma Yöntemleri 10

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 32.900 | 11.800 | 6 sa 15 dk | %1 | 222 |
| 2 | 24 | 66.400 | 23.900 | 13 sa 10 dk | %2 | 302 |
| 3 | 27 | 123.000 | 44.400 | 23 sa 15 dk | %3 | 411 |
| 4 | 30 | 220.000 | 79.200 | 1 g 13 sa 45 dk | %4 | 560 |
| 5 | 33 | 360.000 | 130.000 | 2 g 11 sa 15 dk | %5 | 704 |
| 6 | 36 | 574.000 | 207.000 | 3 g 17 sa 45 dk | %6 | 885 |
| 7 | 39 | 871.000 | 313.000 | 5 g 7 sa | %7 | 1.112 |
| 8 | 42 | 1.320.000 | 475.000 | 7 g 6 sa 15 dk | %8 | 1.355 |
| 9 | 45 | 2.000.000 | 720.000 | 9 g 19 sa 15 dk | %9 | 1.625 |
| 10 | 48 | 3.030.000 | 1.090.000 | 12 g 12 sa | %10 | 1.949 |

← [06 · Araştırma](../06_arastirma.md)
