<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Saldırı Taktikleri

`attack_tactics` · Bilim Kubbesi · 11 teknoloji · 100 seviye

> Saldırıda ve rallide birlik gücü, ralli kapasitesi, kalkan delme, yağma yükü, sayaç avantajı ve gizli yaklaşma.

**Açılış.** Bilim Kubbesi 2 (Reaktör 20)

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [Hücum Doktrini](#assault-doctrine) | ⬜ | 10 | Saldırıda birlik saldırısı | %10 | 20–47 | 3.355.800 Titanyum · 1.208.120 Biyokütle | 17 g 23 sa 20 dk |
| [Yağma Bölmeleri](#plunder-bays) | ⬜ | 10 | Yağma yükü | %30 | 20–47 | 3.355.800 Titanyum · 1.208.120 Biyokütle | 17 g 23 sa 20 dk |
| [Hızlı Dönüş](#swift-return) | ⬜ | 5 | Dönüş yürüyüş hızı | %30 | 20–40 | 823.500 Titanyum · 296.410 Biyokütle | 5 g 7 sa |
| [Basınçlı Hücum Kapsülleri](#assault-capsules) | ⬜ | 10 | Saldırıda birlik dayanıklılığı | %10 | 21–48 | 3.865.300 Titanyum · 1.393.030 Biyokütle | 19 g 23 sa 40 dk |
| [Sessiz Pervane](#quiet-propellers) | ⬜ | 10 | Saldırı yürüyüş hızı | %20 | 21–48 | 3.865.300 Titanyum · 1.393.030 Biyokütle | 19 g 23 sa 40 dk |
| [Kalkanlı Yaklaşma](#shielded-approach) | ⬜ | 10 | Saldırıda birlik savunması | %10 | 22–49 | 4.460.900 Titanyum · 1.606.230 Biyokütle | 22 g 3 sa 45 dk |
| [Kalkan Delme](#shield-breach) | ⬜ | 10 | Kalkan Jeneratörü'ne hasar | %30 | 23–50 | 5.144.100 Titanyum · 1.851.910 Biyokütle | 24 g 11 sa 55 dk |
| [Toplu Hücum](#massed-assault) | ⬜ | 10 | Rallide birlik saldırısı | %10 | 24–42 | 2.213.100 Titanyum · 797.300 Biyokütle | 13 g 20 sa 5 dk |
| [Ralli Kapasitesi](#rally-capacity) | ⬜ | 10 | Ralli kapasitesi | %20 | 25–43 | 2.567.100 Titanyum · 924.300 Biyokütle | 15 g 15 sa 15 dk |
| [Sayaç Doktrini](#counter-doctrine) | ⬜ | 10 | Sayaç avantajı | %10 | 26–44 | 2.966.200 Titanyum · 1.068.500 Biyokütle | 17 g 13 sa 55 dk |
| [Karşı Aydınlatma](#counter-illumination) | ⬜ | 5 | Yürüyüşün radarda görünme süresi | −%40 | 28–48 | 2.612.400 Titanyum · 942.500 Biyokütle | 12 g 23 sa 50 dk |

<a id="assault-doctrine"></a>
## Hücum Doktrini

`assault_doctrine` · Kubbeye yaklaşma, bölme açma ve geri çekilme adımlarını tek talimatta toplayan doktrin; saldırıda birlik saldırısı artar.

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

<a id="plunder-bays"></a>
## Yağma Bölmeleri

`plunder_bays` · Saldırı denizaltılarına sökülüp takılan ek yük bölmeleri; yağmalanan kaynak artar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 11.700 | 4.210 | 2 sa 10 dk | %3 | 200 |
| 2 | 23 | 23.600 | 8.510 | 4 sa 35 dk | %6 | 272 |
| 3 | 26 | 45.800 | 16.500 | 8 sa 55 dk | %9 | 371 |
| 4 | 29 | 81.700 | 29.400 | 14 sa 30 dk | %12 | 505 |
| 5 | 32 | 137.000 | 49.500 | 22 sa 55 dk | %15 | 652 |
| 6 | 35 | 225.000 | 81.000 | 1 g 12 sa | %18 | 820 |
| 7 | 38 | 341.000 | 123.000 | 2 g 3 sa | %21 | 1.030 |
| 8 | 41 | 517.000 | 186.000 | 2 g 23 sa | %24 | 1.275 |
| 9 | 44 | 783.000 | 282.000 | 3 g 23 sa 45 dk | %27 | 1.529 |
| 10 | 47 | 1.190.000 | 428.000 | 5 g 4 sa 30 dk | %30 | 1.834 |

<a id="swift-return"></a>
## Hızlı Dönüş

`swift_return` · Geri dönüşte balast boşaltılarak daha hafif seyredilir; yürüyüşler üsse daha hızlı döner.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 11.700 | 4.210 | 2 sa 10 dk | %6 | 200 |
| 2 | 25 | 37.800 | 13.600 | 7 sa 35 dk | %12 | 335 |
| 3 | 30 | 99.000 | 35.600 | 17 sa | %18 | 560 |
| 4 | 35 | 225.000 | 81.000 | 1 g 12 sa | %24 | 820 |
| 5 | 40 | 450.000 | 162.000 | 2 g 16 sa 15 dk | %30 | 1.200 |

<a id="assault-capsules"></a>
## Basınçlı Hücum Kapsülleri

`assault_capsules` · Askerleri hedef kubbenin dibine bırakan basınçlı kapsüller; saldırıda birlik dayanıklılığı artar.

**Gerekli:** Hücum Doktrini 2

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

<a id="quiet-propellers"></a>
## Sessiz Pervane

`quiet_propellers` · Eğik kanatlı pervaneler kavitasyon kabarcıklarını ve gürültüyü azaltır; saldırı yürüyüşü hızlanır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 14.800 | 5.330 | 2 sa 50 dk | %2 | 222 |
| 2 | 24 | 29.900 | 10.800 | 5 sa 55 dk | %4 | 302 |
| 3 | 27 | 55.600 | 20.000 | 10 sa 25 dk | %6 | 411 |
| 4 | 30 | 99.000 | 35.600 | 17 sa | %8 | 560 |
| 5 | 33 | 162.000 | 58.300 | 1 g 2 sa 45 dk | %10 | 704 |
| 6 | 36 | 258.000 | 93.000 | 1 g 16 sa 15 dk | %12 | 885 |
| 7 | 39 | 392.000 | 141.000 | 2 g 9 sa 15 dk | %14 | 1.112 |
| 8 | 42 | 594.000 | 214.000 | 3 g 6 sa 30 dk | %16 | 1.355 |
| 9 | 45 | 900.000 | 324.000 | 4 g 9 sa 45 dk | %18 | 1.625 |
| 10 | 48 | 1.360.000 | 491.000 | 5 g 15 sa | %20 | 1.949 |

<a id="shielded-approach"></a>
## Kalkanlı Yaklaşma

`shielded_approach` · Önde giden zırhlı kalkan dronları; saldırıda birlik savunması artar.

**Gerekli:** Hücum Doktrini 2

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

<a id="shield-breach"></a>
## Kalkan Delme

`shield_breach` · Kalkan jeneratörünün frekansını bulan Delici ekipleri; hedef kalkana verilen hasar artar.

**Gerekli:** Hücum Doktrini 3

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

<a id="massed-assault"></a>
## Toplu Hücum

`massed_assault` · İttifak denizaltılarının aynı anda vurması için akustik zamanlama; rallide birlik saldırısı artar.

**Gerekli:** Hücum Doktrini 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 24 | 29.900 | 10.800 | 5 sa 55 dk | %1 | 302 |
| 2 | 26 | 45.800 | 16.500 | 8 sa 55 dk | %2 | 371 |
| 3 | 28 | 67.400 | 24.200 | 12 sa 20 dk | %3 | 456 |
| 4 | 30 | 99.000 | 35.600 | 17 sa | %4 | 560 |
| 5 | 32 | 137.000 | 49.500 | 22 sa 55 dk | %5 | 652 |
| 6 | 34 | 191.000 | 68.700 | 1 g 7 sa | %6 | 760 |
| 7 | 36 | 258.000 | 93.000 | 1 g 16 sa 15 dk | %7 | 885 |
| 8 | 38 | 341.000 | 123.000 | 2 g 3 sa | %8 | 1.030 |
| 9 | 40 | 450.000 | 162.000 | 2 g 16 sa 15 dk | %9 | 1.200 |
| 10 | 42 | 594.000 | 214.000 | 3 g 6 sa 30 dk | %10 | 1.355 |

<a id="rally-capacity"></a>
## Ralli Kapasitesi

`rally_capacity` · Ralli komuta denizaltısına ek iletişim ve hava hattı; rallide daha çok asker toplanır.

**Gerekli:** Toplu Hücum 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 25 | 37.800 | 13.600 | 7 sa 35 dk | %2 | 335 |
| 2 | 27 | 55.600 | 20.000 | 10 sa 25 dk | %4 | 411 |
| 3 | 29 | 81.700 | 29.400 | 14 sa 30 dk | %6 | 505 |
| 4 | 31 | 117.000 | 42.000 | 19 sa 45 dk | %8 | 604 |
| 5 | 33 | 162.000 | 58.300 | 1 g 2 sa 45 dk | %10 | 704 |
| 6 | 35 | 225.000 | 81.000 | 1 g 12 sa | %12 | 820 |
| 7 | 37 | 297.000 | 107.000 | 1 g 21 sa 15 dk | %14 | 955 |
| 8 | 39 | 392.000 | 141.000 | 2 g 9 sa 15 dk | %16 | 1.112 |
| 9 | 41 | 517.000 | 186.000 | 2 g 23 sa | %18 | 1.275 |
| 10 | 43 | 682.000 | 246.000 | 3 g 14 sa 45 dk | %20 | 1.439 |

<a id="counter-doctrine"></a>
## Sayaç Doktrini

`counter_doctrine` · Zırhlı Dalgıç, Zıpkıncı ve Ark Askeri arasındaki üstünlük üçgenini savaşta bilinçli kullanmak; sayaç avantajı artar.

**Gerekli:** Hücum Doktrini 4

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 26 | 45.800 | 16.500 | 8 sa 55 dk | %1 | 371 |
| 2 | 28 | 67.400 | 24.200 | 12 sa 20 dk | %2 | 456 |
| 3 | 30 | 99.000 | 35.600 | 17 sa | %3 | 560 |
| 4 | 32 | 137.000 | 49.500 | 22 sa 55 dk | %4 | 652 |
| 5 | 34 | 191.000 | 68.700 | 1 g 7 sa | %5 | 760 |
| 6 | 36 | 258.000 | 93.000 | 1 g 16 sa 15 dk | %6 | 885 |
| 7 | 38 | 341.000 | 123.000 | 2 g 3 sa | %7 | 1.030 |
| 8 | 40 | 450.000 | 162.000 | 2 g 16 sa 15 dk | %8 | 1.200 |
| 9 | 42 | 594.000 | 214.000 | 3 g 6 sa 30 dk | %9 | 1.355 |
| 10 | 44 | 783.000 | 282.000 | 3 g 23 sa 45 dk | %10 | 1.529 |

<a id="counter-illumination"></a>
## Karşı Aydınlatma

`counter_illumination` · Orta su canlıları karınlarındaki ışıkla yukarıdan süzülen ışığı taklit edip gölgelerini siler; yürüyüş düşman radarında daha geç görünür.

**Gerekli:** Sessiz Pervane 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 28 | 67.400 | 24.200 | 12 sa 20 dk | −%8 | 456 |
| 2 | 33 | 162.000 | 58.300 | 1 g 2 sa 45 dk | −%16 | 704 |
| 3 | 38 | 341.000 | 123.000 | 2 g 3 sa | −%24 | 1.030 |
| 4 | 43 | 682.000 | 246.000 | 3 g 14 sa 45 dk | −%32 | 1.439 |
| 5 | 48 | 1.360.000 | 491.000 | 5 g 15 sa | −%40 | 1.949 |

← [06 · Araştırma](../06_arastirma.md)
