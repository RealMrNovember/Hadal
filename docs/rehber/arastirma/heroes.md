<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Kahraman

`heroes` · Bilim Kubbesi · 10 teknoloji · 85 seviye

> Kahraman deneyimi, istatistikleri, öfke, mürettebat becerileri, Subay deneyimi, fraksiyon sinerjisi ve teçhizat.

**Açılış.** Bilim Kubbesi 1 (Reaktör 12)

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [Komuta Simülatörü](#command-simulator) | ⬜ | 10 | Kahraman deneyimi | %40 | 12–48 | 3.457.040 Titanyum · 1.242.400 Biyokütle | 17 g 14 sa 3 dk |
| [Eğitim Bütçesi](#hero-training-budget) | ⬜ | 10 | Kahraman eğitim maliyeti | −%30 | 12–39 | 1.134.550 Titanyum · 408.570 Biyokütle | 7 g 12 sa 25 dk |
| [Nöral Taktik Arayüzü](#neural-tactics) | ⬜ | 10 | Kahraman saldırısı | %15 | 13–49 | 3.979.960 Titanyum · 1.433.985 Biyokütle | 19 g 12 sa 9 dk |
| [Kişisel Basınç Zırhı](#personal-pressure-armor) | ⬜ | 10 | Kahraman savunması | %15 | 14–50 | 4.592.030 Titanyum · 1.653.135 Biyokütle | 21 g 12 sa 48 dk |
| [Biyomedikal İzleme](#biomedical-monitoring) | ⬜ | 10 | Kahraman canı | %15 | 15–42 | 1.793.530 Titanyum · 646.200 Biyokütle | 11 g 3 sa 26 dk |
| [Mürettebat Koçluğu](#crew-coaching) | ⬜ | 10 | Mürettebat becerisi etkisi | %20 | 16–43 | 2.081.320 Titanyum · 749.520 Biyokütle | 12 g 15 sa 3 dk |
| [Subay Deneyimi](#officer-mentoring) | ⬜ | 5 | Subayların deneyim payı | 15 puan | 18–42 | 1.097.430 Titanyum · 395.200 Biyokütle | 6 g 14 sa 59 dk |
| [Adrenalin Protokolü](#adrenaline-protocol) | ⬜ | 5 | Öfke birikimi | %10 | 20–44 | 1.466.900 Titanyum · 526.980 Biyokütle | 8 g 8 sa 50 dk |
| [Teçhizat Uyumu](#gear-affinity) | ⬜ | 10 | Teçhizat istatistikleri | %15 | 22–49 | 4.957.600 Titanyum · 1.785.580 Biyokütle | 24 g 14 sa 30 dk |
| [Fraksiyon Sinerjisi](#faction-synergy) | ⬜ | 5 | Fraksiyon sinerjisi bonusu | %5 | 24–44 | 1.641.900 Titanyum · 591.100 Biyokütle | 9 g 10 sa 55 dk |

<a id="command-simulator"></a>
## Komuta Simülatörü

`command_simulator` · Sefer ve savaş kayıtlarından kurulan simülasyonlarla kahramanlar daha hızlı deneyim kazanır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 12 | 1.020 | 370 | 14 dk | %4 | 64 |
| 2 | 16 | 4.020 | 1.450 | 54 dk | %8 | 113 |
| 3 | 20 | 13.000 | 4.680 | 2 sa 25 dk | %12 | 200 |
| 4 | 24 | 33.200 | 12.000 | 6 sa 35 dk | %16 | 302 |
| 5 | 28 | 74.800 | 26.900 | 13 sa 40 dk | %20 | 456 |
| 6 | 32 | 153.000 | 55.000 | 1 g 1 sa 30 dk | %24 | 652 |
| 7 | 36 | 287.000 | 103.000 | 1 g 20 sa 45 dk | %28 | 885 |
| 8 | 40 | 500.000 | 180.000 | 2 g 23 sa 30 dk | %32 | 1.200 |
| 9 | 44 | 871.000 | 313.000 | 4 g 10 sa 30 dk | %36 | 1.529 |
| 10 | 48 | 1.520.000 | 546.000 | 6 g 6 sa | %40 | 1.949 |

<a id="hero-training-budget"></a>
## Eğitim Bütçesi

`hero_training_budget` · Eğitim programı ortak müfredatla yeniden düzenlenir; kahraman seviye atlatmak daha az kaynak ister.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 12 | 1.020 | 370 | 14 dk | −%3 | 64 |
| 2 | 15 | 3.000 | 1.080 | 42 dk | −%6 | 98 |
| 3 | 18 | 7.230 | 2.600 | 1 sa 29 dk | −%9 | 150 |
| 4 | 21 | 16.400 | 5.920 | 3 sa 10 dk | −%12 | 222 |
| 5 | 24 | 33.200 | 12.000 | 6 sa 35 dk | −%15 | 302 |
| 6 | 27 | 61.700 | 22.200 | 11 sa 35 dk | −%18 | 411 |
| 7 | 30 | 110.000 | 39.600 | 18 sa 55 dk | −%21 | 560 |
| 8 | 33 | 180.000 | 64.800 | 1 g 5 sa 30 dk | −%24 | 704 |
| 9 | 36 | 287.000 | 103.000 | 1 g 20 sa 45 dk | −%27 | 885 |
| 10 | 39 | 435.000 | 157.000 | 2 g 15 sa 30 dk | −%30 | 1.112 |

<a id="neural-tactics"></a>
## Nöral Taktik Arayüzü

`neural_tactics` · Kask içi taktik ekran, sonar verisini komutanın görüş alanına bindirir; kahraman saldırısı artar.

**Gerekli:** Komuta Simülatörü 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 13 | 1.470 | 525 | 20 dk | %1,5 | 74 |
| 2 | 17 | 5.390 | 1.940 | 1 sa 9 dk | %3 | 130 |
| 3 | 21 | 16.400 | 5.920 | 3 sa 10 dk | %4,5 | 222 |
| 4 | 25 | 42.000 | 15.100 | 8 sa 25 dk | %6 | 335 |
| 5 | 29 | 90.700 | 32.700 | 16 sa 5 dk | %7,5 | 505 |
| 6 | 33 | 180.000 | 64.800 | 1 g 5 sa 30 dk | %9 | 704 |
| 7 | 37 | 330.000 | 119.000 | 2 g 2 sa 15 dk | %10,5 | 955 |
| 8 | 41 | 574.000 | 207.000 | 3 g 7 sa | %12 | 1.275 |
| 9 | 45 | 1.000.000 | 360.000 | 4 g 21 sa 30 dk | %13,5 | 1.625 |
| 10 | 49 | 1.740.000 | 627.000 | 6 g 18 sa 45 dk | %15 | 2.071 |

<a id="personal-pressure-armor"></a>
## Kişisel Basınç Zırhı

`personal_pressure_armor` · Komutanlara özel ölçülen hafif basınç zırhı; kahraman savunması artar.

**Gerekli:** Komuta Simülatörü 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 14 | 2.100 | 755 | 29 dk | %1,5 | 85 |
| 2 | 18 | 7.230 | 2.600 | 1 sa 29 dk | %3 | 150 |
| 3 | 22 | 20.800 | 7.480 | 4 sa | %4,5 | 246 |
| 4 | 26 | 50.900 | 18.300 | 9 sa 55 dk | %6 | 371 |
| 5 | 30 | 110.000 | 39.600 | 18 sa 55 dk | %7,5 | 560 |
| 6 | 34 | 212.000 | 76.400 | 1 g 10 sa 15 dk | %9 | 760 |
| 7 | 38 | 379.000 | 136.000 | 2 g 8 sa 30 dk | %10,5 | 1.030 |
| 8 | 42 | 660.000 | 238.000 | 3 g 15 sa 15 dk | %12 | 1.355 |
| 9 | 46 | 1.150.000 | 414.000 | 5 g 7 sa 30 dk | %13,5 | 1.726 |
| 10 | 50 | 2.000.000 | 720.000 | 7 g 8 sa 30 dk | %15 | 2.200 |

<a id="biomedical-monitoring"></a>
## Biyomedikal İzleme

`biomedical_monitoring` · Nabız, kan oksijeni ve vücut ısısını izleyen giysi algılayıcıları; kahraman canı artar.

**Gerekli:** Komuta Simülatörü 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 15 | 3.000 | 1.080 | 42 dk | %1,5 | 98 |
| 2 | 18 | 7.230 | 2.600 | 1 sa 29 dk | %3 | 150 |
| 3 | 21 | 16.400 | 5.920 | 3 sa 10 dk | %4,5 | 222 |
| 4 | 24 | 33.200 | 12.000 | 6 sa 35 dk | %6 | 302 |
| 5 | 27 | 61.700 | 22.200 | 11 sa 35 dk | %7,5 | 411 |
| 6 | 30 | 110.000 | 39.600 | 18 sa 55 dk | %9 | 560 |
| 7 | 33 | 180.000 | 64.800 | 1 g 5 sa 30 dk | %10,5 | 704 |
| 8 | 36 | 287.000 | 103.000 | 1 g 20 sa 45 dk | %12 | 885 |
| 9 | 39 | 435.000 | 157.000 | 2 g 15 sa 30 dk | %13,5 | 1.112 |
| 10 | 42 | 660.000 | 238.000 | 3 g 15 sa 15 dk | %15 | 1.355 |

<a id="crew-coaching"></a>
## Mürettebat Koçluğu

`crew_coaching` · Komuta ekibindeki kahramanlar ekonomi becerilerini mürettebata öğretir; mürettebat becerilerinin etkisi artar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 16 | 4.020 | 1.450 | 54 dk | %2 | 113 |
| 2 | 19 | 9.700 | 3.490 | 1 sa 54 dk | %4 | 173 |
| 3 | 22 | 20.800 | 7.480 | 4 sa | %6 | 246 |
| 4 | 25 | 42.000 | 15.100 | 8 sa 25 dk | %8 | 335 |
| 5 | 28 | 74.800 | 26.900 | 13 sa 40 dk | %10 | 456 |
| 6 | 31 | 130.000 | 46.700 | 21 sa 55 dk | %12 | 604 |
| 7 | 34 | 212.000 | 76.400 | 1 g 10 sa 15 dk | %14 | 760 |
| 8 | 37 | 330.000 | 119.000 | 2 g 2 sa 15 dk | %16 | 955 |
| 9 | 40 | 500.000 | 180.000 | 2 g 23 sa 30 dk | %18 | 1.200 |
| 10 | 43 | 758.000 | 273.000 | 4 g 15 dk | %20 | 1.439 |

<a id="officer-mentoring"></a>
## Subay Deneyimi

`officer_mentoring` · Kaptan savaş sonrası raporu Subaylarıyla birlikte yazar; Subaylar savaş deneyiminden daha büyük pay alır.

**Gerekli:** Komuta Simülatörü 4

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 18 | 7.230 | 2.600 | 1 sa 29 dk | 3 puan | 150 |
| 2 | 24 | 33.200 | 12.000 | 6 sa 35 dk | 6 puan | 302 |
| 3 | 30 | 110.000 | 39.600 | 18 sa 55 dk | 9 puan | 560 |
| 4 | 36 | 287.000 | 103.000 | 1 g 20 sa 45 dk | 12 puan | 885 |
| 5 | 42 | 660.000 | 238.000 | 3 g 15 sa 15 dk | 15 puan | 1.355 |

<a id="adrenaline-protocol"></a>
## Adrenalin Protokolü

`adrenaline_protocol` · Savaş anında stres tepkisini yöneten nefes ve odak teknikleri; kahramanların öfkesi daha hızlı dolar.

**Gerekli:** Nöral Taktik Arayüzü 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 13.000 | 4.680 | 2 sa 25 dk | %2 | 200 |
| 2 | 26 | 50.900 | 18.300 | 9 sa 55 dk | %4 | 371 |
| 3 | 32 | 153.000 | 55.000 | 1 g 1 sa 30 dk | %6 | 652 |
| 4 | 38 | 379.000 | 136.000 | 2 g 8 sa 30 dk | %8 | 1.030 |
| 5 | 44 | 871.000 | 313.000 | 4 g 10 sa 30 dk | %10 | 1.529 |

<a id="gear-affinity"></a>
## Teçhizat Uyumu

`gear_affinity` · Kitin, bazalt camı ve sintaktik köpük parçaları kahramanın vücut ölçüsüne göre ayarlanır; teçhizat istatistikleri artar.

**Gerekli:** Kişisel Basınç Zırhı 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 22 | 20.800 | 7.480 | 4 sa | %1,5 | 246 |
| 2 | 25 | 42.000 | 15.100 | 8 sa 25 dk | %3 | 335 |
| 3 | 28 | 74.800 | 26.900 | 13 sa 40 dk | %4,5 | 456 |
| 4 | 31 | 130.000 | 46.700 | 21 sa 55 dk | %6 | 604 |
| 5 | 34 | 212.000 | 76.400 | 1 g 10 sa 15 dk | %7,5 | 760 |
| 6 | 37 | 330.000 | 119.000 | 2 g 2 sa 15 dk | %9 | 955 |
| 7 | 40 | 500.000 | 180.000 | 2 g 23 sa 30 dk | %10,5 | 1.200 |
| 8 | 43 | 758.000 | 273.000 | 4 g 15 dk | %12 | 1.439 |
| 9 | 46 | 1.150.000 | 414.000 | 5 g 7 sa 30 dk | %13,5 | 1.726 |
| 10 | 49 | 1.740.000 | 627.000 | 6 g 18 sa 45 dk | %15 | 2.071 |

<a id="faction-synergy"></a>
## Fraksiyon Sinerjisi

`faction_synergy` · Aynı fraksiyondan kahramanlar ortak gelenek ve dille daha iyi anlaşır; fraksiyon sinerjisinin bonusu artar.

**Gerekli:** Subay Deneyimi 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 24 | 33.200 | 12.000 | 6 sa 35 dk | %1 | 302 |
| 2 | 29 | 90.700 | 32.700 | 16 sa 5 dk | %2 | 505 |
| 3 | 34 | 212.000 | 76.400 | 1 g 10 sa 15 dk | %3 | 760 |
| 4 | 39 | 435.000 | 157.000 | 2 g 15 sa 30 dk | %4 | 1.112 |
| 5 | 44 | 871.000 | 313.000 | 4 g 10 sa 30 dk | %5 | 1.529 |

← [06 · Araştırma](../06_arastirma.md)
