<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Filo

`fleet` · Bilim Kubbesi · 12 teknoloji · 110 seviye

> Dört Denizaltı Hangarı'nın her birine özel ekip bonusu, denizaltı kapasitesi, dayanıklılığı, hızı, onarımı ve Subay katkısı.

**Açılış.** Bilim Kubbesi 2 (Reaktör 20)

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [1. Hangar Ekibi](#hangar-1-crew) | ⬜ | 10 | 1. denizaltıdaki birliklerin saldırı ve savunması | %10 | 20–47 | 2.984.700 Titanyum · 1.073.110 Biyokütle | 15 g 23 sa 28 dk |
| [Asker Bölmeleri](#troop-bays) | ⬜ | 10 | Denizaltı asker kapasitesi | %20 | 20–47 | 2.984.700 Titanyum · 1.073.110 Biyokütle | 15 g 23 sa 28 dk |
| [Kuru Havuz Robotları](#dry-dock-robots) | ⬜ | 10 | Denizaltı onarım hızı | %50 | 20–47 | 2.984.700 Titanyum · 1.073.110 Biyokütle | 15 g 23 sa 28 dk |
| [2. Hangar Ekibi](#hangar-2-crew) | ⬜ | 10 | 2. denizaltıdaki birliklerin saldırı ve savunması | %10 | 21–48 | 3.437.100 Titanyum · 1.238.300 Biyokütle | 17 g 18 sa 5 dk |
| [Hidrodinamik Gövde](#hydrodynamic-hull) | ⬜ | 10 | Yürüyüş hızı | %20 | 21–48 | 3.437.100 Titanyum · 1.238.300 Biyokütle | 17 g 18 sa 5 dk |
| [3. Hangar Ekibi](#hangar-3-crew) | ⬜ | 10 | 3. denizaltıdaki birliklerin saldırı ve savunması | %10 | 22–49 | 3.963.100 Titanyum · 1.427.080 Biyokütle | 19 g 16 sa 30 dk |
| [Kompozit Gövde Levhaları](#hull-plating) | ⬜ | 10 | Denizaltı dayanıklılığı | %30 | 22–49 | 3.963.100 Titanyum · 1.427.080 Biyokütle | 19 g 16 sa 30 dk |
| [Seri Gövde Üretimi](#serial-hull-production) | ⬜ | 10 | Denizaltı inşa hızı | %40 | 23–50 | 4.574.300 Titanyum · 1.645.370 Biyokütle | 21 g 18 sa 30 dk |
| [Basınca Dayanıklı Batarya](#pressure-tolerant-batteries) | ⬜ | 10 | Dalış oksijen maliyeti | −%30 | 24–42 | 1.968.200 Titanyum · 708.370 Biyokütle | 12 g 6 sa 50 dk |
| [Subay Eğitimi](#officer-training) | ⬜ | 5 | Subay istatistiklerinin filoya katkısı | 10 puan | 25–41 | 973.200 Titanyum · 350.000 Biyokütle | 6 g 2 sa 30 dk |
| [4. Hangar Ekibi](#hangar-4-crew) | ⬜ | 10 | 4. denizaltıdaki birliklerin saldırı ve savunması | %10 | 26–44 | 2.637.600 Titanyum · 949.800 Biyokütle | 15 g 14 sa 50 dk |
| [Filo Düzeni](#fleet-formation) | ⬜ | 5 | Aynı hedefe giden filo bonusu | %5 | 30–46 | 2.008.000 Titanyum · 722.800 Biyokütle | 10 g 19 sa 35 dk |

<a id="hangar-1-crew"></a>
## 1. Hangar Ekibi

`hangar_1_crew` · Birinci hangarın daimi ekibi aynı denizaltıda birlikte eğitilir; o denizaltıdaki birlikler güçlenir.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 10.400 | 3.740 | 1 sa 58 dk | %1 | 200 |
| 2 | 23 | 21.000 | 7.570 | 4 sa 5 dk | %2 | 272 |
| 3 | 26 | 40.700 | 14.700 | 7 sa 55 dk | %3 | 371 |
| 4 | 29 | 72.600 | 26.100 | 12 sa 50 dk | %4 | 505 |
| 5 | 32 | 122.000 | 44.000 | 20 sa 25 dk | %5 | 652 |
| 6 | 35 | 200.000 | 72.000 | 1 g 8 sa | %6 | 820 |
| 7 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | %7 | 1.030 |
| 8 | 41 | 459.000 | 165.000 | 2 g 15 sa | %8 | 1.275 |
| 9 | 44 | 696.000 | 251.000 | 3 g 13 sa 15 dk | %9 | 1.529 |
| 10 | 47 | 1.060.000 | 380.000 | 4 g 14 sa 45 dk | %10 | 1.834 |

<a id="troop-bays"></a>
## Asker Bölmeleri

`troop_bays` · Basınç bölmeleri yeniden düzenlenir, ranzalar katlanır; her denizaltı daha çok asker taşır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 10.400 | 3.740 | 1 sa 58 dk | %2 | 200 |
| 2 | 23 | 21.000 | 7.570 | 4 sa 5 dk | %4 | 272 |
| 3 | 26 | 40.700 | 14.700 | 7 sa 55 dk | %6 | 371 |
| 4 | 29 | 72.600 | 26.100 | 12 sa 50 dk | %8 | 505 |
| 5 | 32 | 122.000 | 44.000 | 20 sa 25 dk | %10 | 652 |
| 6 | 35 | 200.000 | 72.000 | 1 g 8 sa | %12 | 820 |
| 7 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | %14 | 1.030 |
| 8 | 41 | 459.000 | 165.000 | 2 g 15 sa | %16 | 1.275 |
| 9 | 44 | 696.000 | 251.000 | 3 g 13 sa 15 dk | %18 | 1.529 |
| 10 | 47 | 1.060.000 | 380.000 | 4 g 14 sa 45 dk | %20 | 1.834 |

<a id="dry-dock-robots"></a>
## Kuru Havuz Robotları

`dry_dock_robots` · Denizaltı İskelesi'nde gövdeyi tarayıp yamayan robot kollar; onarım hızlanır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 10.400 | 3.740 | 1 sa 58 dk | %5 | 200 |
| 2 | 23 | 21.000 | 7.570 | 4 sa 5 dk | %10 | 272 |
| 3 | 26 | 40.700 | 14.700 | 7 sa 55 dk | %15 | 371 |
| 4 | 29 | 72.600 | 26.100 | 12 sa 50 dk | %20 | 505 |
| 5 | 32 | 122.000 | 44.000 | 20 sa 25 dk | %25 | 652 |
| 6 | 35 | 200.000 | 72.000 | 1 g 8 sa | %30 | 820 |
| 7 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | %35 | 1.030 |
| 8 | 41 | 459.000 | 165.000 | 2 g 15 sa | %40 | 1.275 |
| 9 | 44 | 696.000 | 251.000 | 3 g 13 sa 15 dk | %45 | 1.529 |
| 10 | 47 | 1.060.000 | 380.000 | 4 g 14 sa 45 dk | %50 | 1.834 |

<a id="hangar-2-crew"></a>
## 2. Hangar Ekibi

`hangar_2_crew` · İkinci hangarın daimi ekibi; o denizaltıdaki birlikler güçlenir.

**Gerekli:** 1. Hangar Ekibi 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 13.100 | 4.730 | 2 sa 30 dk | %1 | 222 |
| 2 | 24 | 26.600 | 9.570 | 5 sa 15 dk | %2 | 302 |
| 3 | 27 | 49.400 | 17.800 | 9 sa 20 dk | %3 | 411 |
| 4 | 30 | 88.000 | 31.700 | 15 sa 5 dk | %4 | 560 |
| 5 | 33 | 144.000 | 51.800 | 23 sa 40 dk | %5 | 704 |
| 6 | 36 | 230.000 | 82.700 | 1 g 11 sa 45 dk | %6 | 885 |
| 7 | 39 | 348.000 | 125.000 | 2 g 2 sa 45 dk | %7 | 1.112 |
| 8 | 42 | 528.000 | 190.000 | 2 g 21 sa 45 dk | %8 | 1.355 |
| 9 | 45 | 800.000 | 288.000 | 3 g 22 sa | %9 | 1.625 |
| 10 | 48 | 1.210.000 | 437.000 | 5 g | %10 | 1.949 |

<a id="hydrodynamic-hull"></a>
## Hidrodinamik Gövde

`hydrodynamic_hull` · Damla biçimli gövde suya en az direnci gösterir; denizaltılar haritada daha hızlı ilerler.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 13.100 | 4.730 | 2 sa 30 dk | %2 | 222 |
| 2 | 24 | 26.600 | 9.570 | 5 sa 15 dk | %4 | 302 |
| 3 | 27 | 49.400 | 17.800 | 9 sa 20 dk | %6 | 411 |
| 4 | 30 | 88.000 | 31.700 | 15 sa 5 dk | %8 | 560 |
| 5 | 33 | 144.000 | 51.800 | 23 sa 40 dk | %10 | 704 |
| 6 | 36 | 230.000 | 82.700 | 1 g 11 sa 45 dk | %12 | 885 |
| 7 | 39 | 348.000 | 125.000 | 2 g 2 sa 45 dk | %14 | 1.112 |
| 8 | 42 | 528.000 | 190.000 | 2 g 21 sa 45 dk | %16 | 1.355 |
| 9 | 45 | 800.000 | 288.000 | 3 g 22 sa | %18 | 1.625 |
| 10 | 48 | 1.210.000 | 437.000 | 5 g | %20 | 1.949 |

<a id="hangar-3-crew"></a>
## 3. Hangar Ekibi

`hangar_3_crew` · Üçüncü hangarın daimi ekibi; o denizaltıdaki birlikler güçlenir.

**Gerekli:** 2. Hangar Ekibi 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 22 | 16.600 | 5.980 | 3 sa 15 dk | %1 | 246 |
| 2 | 25 | 33.600 | 12.100 | 6 sa 45 dk | %2 | 335 |
| 3 | 28 | 59.900 | 21.600 | 10 sa 55 dk | %3 | 456 |
| 4 | 31 | 104.000 | 37.300 | 17 sa 35 dk | %4 | 604 |
| 5 | 34 | 170.000 | 61.100 | 1 g 3 sa 30 dk | %5 | 760 |
| 6 | 37 | 264.000 | 95.000 | 1 g 16 sa 15 dk | %6 | 955 |
| 7 | 40 | 400.000 | 144.000 | 2 g 9 sa | %7 | 1.200 |
| 8 | 43 | 606.000 | 218.000 | 3 g 5 sa | %8 | 1.439 |
| 9 | 46 | 919.000 | 331.000 | 4 g 6 sa | %9 | 1.726 |
| 10 | 49 | 1.390.000 | 501.000 | 5 g 10 sa 15 dk | %10 | 2.071 |

<a id="hull-plating"></a>
## Kompozit Gövde Levhaları

`hull_plating` · Gövdeye eklenen titanyum ve kompozit levhalar; denizaltının dayanıklılığı artar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 22 | 16.600 | 5.980 | 3 sa 15 dk | %3 | 246 |
| 2 | 25 | 33.600 | 12.100 | 6 sa 45 dk | %6 | 335 |
| 3 | 28 | 59.900 | 21.600 | 10 sa 55 dk | %9 | 456 |
| 4 | 31 | 104.000 | 37.300 | 17 sa 35 dk | %12 | 604 |
| 5 | 34 | 170.000 | 61.100 | 1 g 3 sa 30 dk | %15 | 760 |
| 6 | 37 | 264.000 | 95.000 | 1 g 16 sa 15 dk | %18 | 955 |
| 7 | 40 | 400.000 | 144.000 | 2 g 9 sa | %21 | 1.200 |
| 8 | 43 | 606.000 | 218.000 | 3 g 5 sa | %24 | 1.439 |
| 9 | 46 | 919.000 | 331.000 | 4 g 6 sa | %27 | 1.726 |
| 10 | 49 | 1.390.000 | 501.000 | 5 g 10 sa 15 dk | %30 | 2.071 |

<a id="serial-hull-production"></a>
## Seri Gövde Üretimi

`serial_hull_production` · Gövde halkaları standart kalıplarda dökülür; Denizaltı İskelesi daha hızlı inşa eder.

**Gerekli:** Kuru Havuz Robotları 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 23 | 21.000 | 7.570 | 4 sa 5 dk | %4 | 272 |
| 2 | 26 | 40.700 | 14.700 | 7 sa 55 dk | %8 | 371 |
| 3 | 29 | 72.600 | 26.100 | 12 sa 50 dk | %12 | 505 |
| 4 | 32 | 122.000 | 44.000 | 20 sa 25 dk | %16 | 652 |
| 5 | 35 | 200.000 | 72.000 | 1 g 8 sa | %20 | 820 |
| 6 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | %24 | 1.030 |
| 7 | 41 | 459.000 | 165.000 | 2 g 15 sa | %28 | 1.275 |
| 8 | 44 | 696.000 | 251.000 | 3 g 13 sa 15 dk | %32 | 1.529 |
| 9 | 47 | 1.060.000 | 380.000 | 4 g 14 sa 45 dk | %36 | 1.834 |
| 10 | 50 | 1.600.000 | 576.000 | 5 g 21 sa | %40 | 2.200 |

<a id="pressure-tolerant-batteries"></a>
## Basınca Dayanıklı Batarya

`pressure_tolerant_batteries` · Yağ dolu kutulara alınan bataryalar basınç kabı gerektirmez, hafifler; dalışlar daha az oksijen harcar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 24 | 26.600 | 9.570 | 5 sa 15 dk | −%3 | 302 |
| 2 | 26 | 40.700 | 14.700 | 7 sa 55 dk | −%6 | 371 |
| 3 | 28 | 59.900 | 21.600 | 10 sa 55 dk | −%9 | 456 |
| 4 | 30 | 88.000 | 31.700 | 15 sa 5 dk | −%12 | 560 |
| 5 | 32 | 122.000 | 44.000 | 20 sa 25 dk | −%15 | 652 |
| 6 | 34 | 170.000 | 61.100 | 1 g 3 sa 30 dk | −%18 | 760 |
| 7 | 36 | 230.000 | 82.700 | 1 g 11 sa 45 dk | −%21 | 885 |
| 8 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | −%24 | 1.030 |
| 9 | 40 | 400.000 | 144.000 | 2 g 9 sa | −%27 | 1.200 |
| 10 | 42 | 528.000 | 190.000 | 2 g 21 sa 45 dk | −%30 | 1.355 |

<a id="officer-training"></a>
## Subay Eğitimi

`officer_training` · Subaylar Kaptanın komuta zincirine sıkı bağlanır; Subay istatistiklerinin askerlere geçen payı artar.

**Gerekli:** 1. Hangar Ekibi 5

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 25 | 33.600 | 12.100 | 6 sa 45 dk | 2 puan | 335 |
| 2 | 29 | 72.600 | 26.100 | 12 sa 50 dk | 4 puan | 505 |
| 3 | 33 | 144.000 | 51.800 | 23 sa 40 dk | 6 puan | 704 |
| 4 | 37 | 264.000 | 95.000 | 1 g 16 sa 15 dk | 8 puan | 955 |
| 5 | 41 | 459.000 | 165.000 | 2 g 15 sa | 10 puan | 1.275 |

<a id="hangar-4-crew"></a>
## 4. Hangar Ekibi

`hangar_4_crew` · Dördüncü hangarın daimi ekibi; o denizaltıdaki birlikler güçlenir.

**Gerekli:** 3. Hangar Ekibi 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 26 | 40.700 | 14.700 | 7 sa 55 dk | %1 | 371 |
| 2 | 28 | 59.900 | 21.600 | 10 sa 55 dk | %2 | 456 |
| 3 | 30 | 88.000 | 31.700 | 15 sa 5 dk | %3 | 560 |
| 4 | 32 | 122.000 | 44.000 | 20 sa 25 dk | %4 | 652 |
| 5 | 34 | 170.000 | 61.100 | 1 g 3 sa 30 dk | %5 | 760 |
| 6 | 36 | 230.000 | 82.700 | 1 g 11 sa 45 dk | %6 | 885 |
| 7 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | %7 | 1.030 |
| 8 | 40 | 400.000 | 144.000 | 2 g 9 sa | %8 | 1.200 |
| 9 | 42 | 528.000 | 190.000 | 2 g 21 sa 45 dk | %9 | 1.355 |
| 10 | 44 | 696.000 | 251.000 | 3 g 13 sa 15 dk | %10 | 1.529 |

<a id="fleet-formation"></a>
## Filo Düzeni

`fleet_formation` · Aynı hedefe yönelen denizaltılar sonar ağıyla birbirini tamamlar; ortak saldırıda birlikler güçlenir.

**Gerekli:** 4. Hangar Ekibi 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 30 | 88.000 | 31.700 | 15 sa 5 dk | %1 | 560 |
| 2 | 34 | 170.000 | 61.100 | 1 g 3 sa 30 dk | %2 | 760 |
| 3 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | %3 | 1.030 |
| 4 | 42 | 528.000 | 190.000 | 2 g 21 sa 45 dk | %4 | 1.355 |
| 5 | 46 | 919.000 | 331.000 | 4 g 6 sa | %5 | 1.726 |

← [06 · Araştırma](../06_arastirma.md)
