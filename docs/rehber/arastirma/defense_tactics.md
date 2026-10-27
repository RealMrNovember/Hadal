<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Savunma Taktikleri

`defense_tactics` · Bilim Kubbesi · 11 teknoloji · 95 seviye

> Savunmada birlik gücü, Kalkan Jeneratörü, ittifak takviyesi, korunan kaynak, keşif engelleme, erken uyarı ve tuzaklar.

**Açılış.** Bilim Kubbesi 2 (Reaktör 20)

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [Kubbe Topçusu](#dome-battery) | ⬜ | 10 | Savunmada birlik saldırısı | %10 | 20–47 | 3.355.800 Titanyum · 1.208.120 Biyokütle | 17 g 23 sa 20 dk |
| [Kalkan Sertleştirme](#shield-hardening) | ⬜ | 10 | Kalkan Jeneratörü dayanıklılığı | %40 | 20–47 | 3.355.800 Titanyum · 1.208.120 Biyokütle | 17 g 23 sa 20 dk |
| [Ambar Kilitleri](#vault-locks) | ⬜ | 10 | Yağmadan korunan kaynak | %50 | 20–38 | 1.199.500 Titanyum · 432.240 Biyokütle | 8 g 3 sa 5 dk |
| [Kubbe Savunma Doktrini](#dome-defense-doctrine) | ⬜ | 10 | Savunmada birlik savunması | %10 | 21–48 | 3.865.300 Titanyum · 1.393.030 Biyokütle | 19 g 23 sa 40 dk |
| [Hidrofon Dizisi](#hydrophone-array) | ⬜ | 5 | Saldırı uyarısı süresi | %50 | 21–41 | 952.600 Titanyum · 342.830 Biyokütle | 5 g 22 sa 45 dk |
| [Basınç Odası Rotasyonu](#chamber-rotation) | ⬜ | 10 | Savunmada birlik dayanıklılığı | %10 | 22–49 | 4.460.900 Titanyum · 1.606.230 Biyokütle | 22 g 3 sa 45 dk |
| [Takviye Limanı](#reinforcement-dock) | ⬜ | 10 | İttifak takviye kapasitesi | %30 | 23–50 | 5.144.100 Titanyum · 1.851.910 Biyokütle | 24 g 11 sa 55 dk |
| [Yankısız Kaplama](#anechoic-tiles) | ⬜ | 5 | Düşman keşif doğruluğu | −%40 | 24–44 | 1.477.600 Titanyum · 531.900 Biyokütle | 8 g 12 sa 25 dk |
| [Basınç Mayınları](#pressure-mines) | ⬜ | 10 | Savunma tuzağı hasarı | %40 | 25–43 | 2.567.100 Titanyum · 924.300 Biyokütle | 15 g 15 sa 15 dk |
| [Su Geçirmez Bölmeler](#watertight-bulkheads) | ⬜ | 5 | Savunmada yaralı oranı | %10 | 26–46 | 1.967.800 Titanyum · 709.500 Biyokütle | 10 g 14 sa 40 dk |
| [Akustik Yem](#acoustic-decoys) | ⬜ | 10 | Ralliye karşı savunma | %10 | 27–45 | 3.429.300 Titanyum · 1.234.700 Biyokütle | 19 g 17 sa 25 dk |

<a id="dome-battery"></a>
## Kubbe Topçusu

`dome_battery` · Kubbe çevresine yerleştirilen sabit atış noktaları; savunmada birlik saldırısı artar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 11.700 | 4.210 | 2 sa 10 dk | %1 | 200 |
| 2 | 23 | 23.600 | 8.510 | 4 sa 35 dk | %2 | 272 |
| 3 | 26 | 45.800 | 16.500 | 8 sa 55 dk | %3 | 371 |
| 4 | 29 | 81.700 | 29.400 | 14 sa 30 dk | %4 | 505 |
| 5 | 32 | 137.000 | 49.500 | 22 sa 55 dk | %5 | 652 |
| 6 | 35 | 225.000 | 81.000 | 1 g 12 sa | %6 | 820 |
| 7 | 38 | 341.000 | 123.000 | 2 g 3 sa | %7 | 1.030 |
| 8 | 41 | 517.000 | 186.000 | 2 g 23 sa | %8 | 1.275 |
| 9 | 44 | 783.000 | 282.000 | 3 g 23 sa 45 dk | %9 | 1.529 |
| 10 | 47 | 1.190.000 | 428.000 | 5 g 4 sa 30 dk | %10 | 1.834 |

<a id="shield-hardening"></a>
## Kalkan Sertleştirme

`shield_hardening` · Kalkan jeneratörünün bobinleri soğuk dip suyuyla soğutulur, aşırı yüke dayanır; kalkan daha geç çöker.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 11.700 | 4.210 | 2 sa 10 dk | %4 | 200 |
| 2 | 23 | 23.600 | 8.510 | 4 sa 35 dk | %8 | 272 |
| 3 | 26 | 45.800 | 16.500 | 8 sa 55 dk | %12 | 371 |
| 4 | 29 | 81.700 | 29.400 | 14 sa 30 dk | %16 | 505 |
| 5 | 32 | 137.000 | 49.500 | 22 sa 55 dk | %20 | 652 |
| 6 | 35 | 225.000 | 81.000 | 1 g 12 sa | %24 | 820 |
| 7 | 38 | 341.000 | 123.000 | 2 g 3 sa | %28 | 1.030 |
| 8 | 41 | 517.000 | 186.000 | 2 g 23 sa | %32 | 1.275 |
| 9 | 44 | 783.000 | 282.000 | 3 g 23 sa 45 dk | %36 | 1.529 |
| 10 | 47 | 1.190.000 | 428.000 | 5 g 4 sa 30 dk | %40 | 1.834 |

<a id="vault-locks"></a>
## Ambar Kilitleri

`vault_locks` · Basınç Ambarı bölmeleri ayrı ayrı mühürlenir; saldırıda yağmalanamayan kaynak artar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 11.700 | 4.210 | 2 sa 10 dk | %5 | 200 |
| 2 | 22 | 18.700 | 6.730 | 3 sa 35 dk | %10 | 246 |
| 3 | 24 | 29.900 | 10.800 | 5 sa 55 dk | %15 | 302 |
| 4 | 26 | 45.800 | 16.500 | 8 sa 55 dk | %20 | 371 |
| 5 | 28 | 67.400 | 24.200 | 12 sa 20 dk | %25 | 456 |
| 6 | 30 | 99.000 | 35.600 | 17 sa | %30 | 560 |
| 7 | 32 | 137.000 | 49.500 | 22 sa 55 dk | %35 | 652 |
| 8 | 34 | 191.000 | 68.700 | 1 g 7 sa | %40 | 760 |
| 9 | 36 | 258.000 | 93.000 | 1 g 16 sa 15 dk | %45 | 885 |
| 10 | 38 | 341.000 | 123.000 | 2 g 3 sa | %50 | 1.030 |

<a id="dome-defense-doctrine"></a>
## Kubbe Savunma Doktrini

`dome_defense_doctrine` · Her bölmenin kendi savunma ekibi ve kaçış yolu vardır; savunmada birlik savunması artar.

**Gerekli:** Kubbe Topçusu 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 14.800 | 5.330 | 2 sa 50 dk | %1 | 222 |
| 2 | 24 | 29.900 | 10.800 | 5 sa 55 dk | %2 | 302 |
| 3 | 27 | 55.600 | 20.000 | 10 sa 25 dk | %3 | 411 |
| 4 | 30 | 99.000 | 35.600 | 17 sa | %4 | 560 |
| 5 | 33 | 162.000 | 58.300 | 1 g 2 sa 45 dk | %5 | 704 |
| 6 | 36 | 258.000 | 93.000 | 1 g 16 sa 15 dk | %6 | 885 |
| 7 | 39 | 392.000 | 141.000 | 2 g 9 sa 15 dk | %7 | 1.112 |
| 8 | 42 | 594.000 | 214.000 | 3 g 6 sa 30 dk | %8 | 1.355 |
| 9 | 45 | 900.000 | 324.000 | 4 g 9 sa 45 dk | %9 | 1.625 |
| 10 | 48 | 1.360.000 | 491.000 | 5 g 15 sa | %10 | 1.949 |

<a id="hydrophone-array"></a>
## Hidrofon Dizisi

`hydrophone_array` · Deniz tabanına döşenen hidrofon dizileri pervane sesini kilometrelerce öteden duyar; saldırı uyarısı daha erken gelir.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 14.800 | 5.330 | 2 sa 50 dk | %10 | 222 |
| 2 | 26 | 45.800 | 16.500 | 8 sa 55 dk | %20 | 371 |
| 3 | 31 | 117.000 | 42.000 | 19 sa 45 dk | %30 | 604 |
| 4 | 36 | 258.000 | 93.000 | 1 g 16 sa 15 dk | %40 | 885 |
| 5 | 41 | 517.000 | 186.000 | 2 g 23 sa | %50 | 1.275 |

<a id="chamber-rotation"></a>
## Basınç Odası Rotasyonu

`chamber_rotation` · Savunmacılar dinlenme odalarıyla dönüşümlü çalışır; savunmada birlik dayanıklılığı artar.

**Gerekli:** Kubbe Topçusu 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 22 | 18.700 | 6.730 | 3 sa 35 dk | %1 | 246 |
| 2 | 25 | 37.800 | 13.600 | 7 sa 35 dk | %2 | 335 |
| 3 | 28 | 67.400 | 24.200 | 12 sa 20 dk | %3 | 456 |
| 4 | 31 | 117.000 | 42.000 | 19 sa 45 dk | %4 | 604 |
| 5 | 34 | 191.000 | 68.700 | 1 g 7 sa | %5 | 760 |
| 6 | 37 | 297.000 | 107.000 | 1 g 21 sa 15 dk | %6 | 955 |
| 7 | 40 | 450.000 | 162.000 | 2 g 16 sa 15 dk | %7 | 1.200 |
| 8 | 43 | 682.000 | 246.000 | 3 g 14 sa 45 dk | %8 | 1.439 |
| 9 | 46 | 1.030.000 | 372.000 | 4 g 18 sa 45 dk | %9 | 1.726 |
| 10 | 49 | 1.570.000 | 564.000 | 6 g 2 sa 30 dk | %10 | 2.071 |

<a id="reinforcement-dock"></a>
## Takviye Limanı

`reinforcement_dock` · Müttefik denizaltılar için ek yanaşma ağzı; üsse daha çok ittifak takviyesi yerleşir.

**Gerekli:** Kubbe Savunma Doktrini 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 23 | 23.600 | 8.510 | 4 sa 35 dk | %3 | 272 |
| 2 | 26 | 45.800 | 16.500 | 8 sa 55 dk | %6 | 371 |
| 3 | 29 | 81.700 | 29.400 | 14 sa 30 dk | %9 | 505 |
| 4 | 32 | 137.000 | 49.500 | 22 sa 55 dk | %12 | 652 |
| 5 | 35 | 225.000 | 81.000 | 1 g 12 sa | %15 | 820 |
| 6 | 38 | 341.000 | 123.000 | 2 g 3 sa | %18 | 1.030 |
| 7 | 41 | 517.000 | 186.000 | 2 g 23 sa | %21 | 1.275 |
| 8 | 44 | 783.000 | 282.000 | 3 g 23 sa 45 dk | %24 | 1.529 |
| 9 | 47 | 1.190.000 | 428.000 | 5 g 4 sa 30 dk | %27 | 1.834 |
| 10 | 50 | 1.800.000 | 648.000 | 6 g 14 sa 45 dk | %30 | 2.200 |

<a id="anechoic-tiles"></a>
## Yankısız Kaplama

`anechoic_tiles` · Denizaltı gövdelerindeki kauçuk kaplamalar sonar dalgasını yutar; düşman keşif raporları daha bulanık gelir.

**Gerekli:** Hidrofon Dizisi 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 24 | 29.900 | 10.800 | 5 sa 55 dk | −%8 | 302 |
| 2 | 29 | 81.700 | 29.400 | 14 sa 30 dk | −%16 | 505 |
| 3 | 34 | 191.000 | 68.700 | 1 g 7 sa | −%24 | 760 |
| 4 | 39 | 392.000 | 141.000 | 2 g 9 sa 15 dk | −%32 | 1.112 |
| 5 | 44 | 783.000 | 282.000 | 3 g 23 sa 45 dk | −%40 | 1.529 |

<a id="pressure-mines"></a>
## Basınç Mayınları

`pressure_mines` · Kubbe çevresindeki basınç tetikli tuzaklar; savunma tuzaklarının hasarı artar.

**Gerekli:** Kalkan Sertleştirme 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 25 | 37.800 | 13.600 | 7 sa 35 dk | %4 | 335 |
| 2 | 27 | 55.600 | 20.000 | 10 sa 25 dk | %8 | 411 |
| 3 | 29 | 81.700 | 29.400 | 14 sa 30 dk | %12 | 505 |
| 4 | 31 | 117.000 | 42.000 | 19 sa 45 dk | %16 | 604 |
| 5 | 33 | 162.000 | 58.300 | 1 g 2 sa 45 dk | %20 | 704 |
| 6 | 35 | 225.000 | 81.000 | 1 g 12 sa | %24 | 820 |
| 7 | 37 | 297.000 | 107.000 | 1 g 21 sa 15 dk | %28 | 955 |
| 8 | 39 | 392.000 | 141.000 | 2 g 9 sa 15 dk | %32 | 1.112 |
| 9 | 41 | 517.000 | 186.000 | 2 g 23 sa | %36 | 1.275 |
| 10 | 43 | 682.000 | 246.000 | 3 g 14 sa 45 dk | %40 | 1.439 |

<a id="watertight-bulkheads"></a>
## Su Geçirmez Bölmeler

`watertight_bulkheads` · Gemi mühendisliğinin en eski güvencesi: delinen bölme kapanır, gerisi ayakta kalır; savunma kayıplarının daha büyük kısmı yaralı sayılır.

**Gerekli:** Basınç Odası Rotasyonu 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 26 | 45.800 | 16.500 | 8 sa 55 dk | %2 | 371 |
| 2 | 31 | 117.000 | 42.000 | 19 sa 45 dk | %4 | 604 |
| 3 | 36 | 258.000 | 93.000 | 1 g 16 sa 15 dk | %6 | 885 |
| 4 | 41 | 517.000 | 186.000 | 2 g 23 sa | %8 | 1.275 |
| 5 | 46 | 1.030.000 | 372.000 | 4 g 18 sa 45 dk | %10 | 1.726 |

<a id="acoustic-decoys"></a>
## Akustik Yem

`acoustic_decoys` · Denizaltıların torpidoyu şaşırtmak için bıraktığı gürültü yemleri; ralli saldırılarına karşı birlik savunması artar.

**Gerekli:** Kubbe Savunma Doktrini 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 27 | 55.600 | 20.000 | 10 sa 25 dk | %1 | 411 |
| 2 | 29 | 81.700 | 29.400 | 14 sa 30 dk | %2 | 505 |
| 3 | 31 | 117.000 | 42.000 | 19 sa 45 dk | %3 | 604 |
| 4 | 33 | 162.000 | 58.300 | 1 g 2 sa 45 dk | %4 | 704 |
| 5 | 35 | 225.000 | 81.000 | 1 g 12 sa | %5 | 820 |
| 6 | 37 | 297.000 | 107.000 | 1 g 21 sa 15 dk | %6 | 955 |
| 7 | 39 | 392.000 | 141.000 | 2 g 9 sa 15 dk | %7 | 1.112 |
| 8 | 41 | 517.000 | 186.000 | 2 g 23 sa | %8 | 1.275 |
| 9 | 43 | 682.000 | 246.000 | 3 g 14 sa 45 dk | %9 | 1.439 |
| 10 | 45 | 900.000 | 324.000 | 4 g 9 sa 45 dk | %10 | 1.625 |

← [06 · Araştırma](../06_arastirma.md)
