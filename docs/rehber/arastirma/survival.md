<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Hayatta Kalma

`survival` · Araştırma Merkezi · 12 teknoloji · 95 seviye

> Oksijen, gıda ve enerji tüketimi, basınç ve deprem olayları, Revir, iyileştirme ve yaralı oranı: üssü ve askeri ayakta tutmak.

**Açılış.** Araştırma Merkezi 4

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [Hava Geri Dönüşümü](#air-recycling) | ⬜ | 10 | Oksijen tüketimi | −%30 | 4–31 | 680.025 Titanyum · 244.725 Biyokütle | 5 g 3 sa 7 dk |
| [Kimyasal Oksijen Mumu](#oxygen-candles) | ⬜ | 5 | Oksijen bitince tampon süre | 5 sa | 5–25 | 140.770 Titanyum · 50.580 Biyokütle | 1 g 3 sa 59 dk |
| [Basınç Gövdesi](#pressure-hull) | ⬜ | 10 | Basınç olayı hasarı | −%50 | 6–42 | 3.457.780 Titanyum · 1.245.580 Biyokütle | 21 g 7 sa 8 dk |
| [Triyaj Protokolü](#triage-protocol) | ⬜ | 10 | İyileştirme hızı | %50 | 7–43 | 4.014.690 Titanyum · 1.446.725 Biyokütle | 24 g 3 sa 13 dk |
| [Revir Genişletme](#infirmary-expansion) | ⬜ | 10 | Revir kapasitesi | %40 | 8–44 | 4.649.555 Titanyum · 1.673.160 Biyokütle | 27 g 4 sa 42 dk |
| [Erzak Disiplini](#field-rations) | ⬜ | 10 | Asker gıda bakımı | −%20 | 9–45 | 5.379.630 Titanyum · 1.936.120 Biyokütle | 30 g 13 sa 4 dk |
| [Aerojel Yalıtım](#aerogel-insulation) | ⬜ | 10 | Bina enerji tüketimi | −%20 | 10–46 | 6.217.530 Titanyum · 2.237.490 Biyokütle | 34 g 1 sa 8 dk |
| [Sismik Ağ](#seismic-network) | ⬜ | 5 | Deprem ve heyelan hasarı | −%30 | 12–36 | 1.052.560 Titanyum · 378.835 Biyokütle | 7 g 4 sa 39 dk |
| [Hiperbarik Tıp](#hyperbaric-medicine) | ⬜ | 10 | İyileştirme maliyeti | −%30 | 14–41 | 3.708.230 Titanyum · 1.333.770 Biyokütle | 23 g 14 sa 25 dk |
| [Hipoksi Tatbikatı](#hypoxia-drills) | ⬜ | 5 | Oksijen açığında üretim kaybı | −%40 | 15–39 | 1.666.600 Titanyum · 602.090 Biyokütle | 10 g 20 sa 41 dk |
| [Kriyo Taşma Protokolü](#cryo-protocol) | ⬜ | 5 | Kriyo kurtarma oranı | %10 | 18–42 | 2.630.100 Titanyum · 947.950 Biyokütle | 15 g 21 sa 20 dk |
| [Tahliye Kapsülleri](#evacuation-pods) | ⬜ | 5 | Kayıplarda yaralı oranı | %10 | 20–40 | 2.196.200 Titanyum · 790.500 Biyokütle | 14 g 2 sa 20 dk |

<a id="air-recycling"></a>
## Hava Geri Dönüşümü

`air_recycling` · Denizaltılardaki gibi karbondioksiti lityum hidroksitle tutan yıkayıcılar; mürettebatın ve askerlerin oksijen tüketimi düşer.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 4 | 85 | 30 | 13 sn | −%3 | 9 |
| 2 | 7 | 370 | 135 | 101 sn | −%6 | 21 |
| 3 | 10 | 1.200 | 430 | 13 dk | −%9 | 48 |
| 4 | 13 | 3.520 | 1.270 | 48 dk | −%12 | 74 |
| 5 | 16 | 9.650 | 3.480 | 2 sa 10 dk | −%15 | 113 |
| 6 | 19 | 23.300 | 8.380 | 4 sa 35 dk | −%18 | 173 |
| 7 | 22 | 49.900 | 18.000 | 9 sa 40 dk | −%21 | 246 |
| 8 | 25 | 101.000 | 36.300 | 20 sa 10 dk | −%24 | 335 |
| 9 | 28 | 180.000 | 64.700 | 1 g 8 sa 45 dk | −%27 | 456 |
| 10 | 31 | 311.000 | 112.000 | 2 g 4 sa 45 dk | −%30 | 604 |

<a id="oxygen-candles"></a>
## Kimyasal Oksijen Mumu

`oxygen_candles` · Denizaltılarda ve uzay istasyonlarında kullanılan klorat mumları yanarak oksijen verir; oksijen tükenince mürettebatı bir süre daha ayakta tutar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 5 | 170 | 60 | 25 sn | 1 sa | 12 |
| 2 | 10 | 1.200 | 430 | 13 dk | 2 sa | 48 |
| 3 | 15 | 7.200 | 2.590 | 1 sa 41 dk | 3 sa | 98 |
| 4 | 20 | 31.200 | 11.200 | 5 sa 55 dk | 4 sa | 200 |
| 5 | 25 | 101.000 | 36.300 | 20 sa 10 dk | 5 sa | 335 |

<a id="pressure-hull"></a>
## Basınç Gövdesi

`pressure_hull` · Modül birleşim yerleri yüzlerce atmosfere göre yeniden hesaplanır; basınç olaylarında sızıntı ve hasar azalır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 6 | 250 | 90 | 50 sn | −%5 | 16 |
| 2 | 10 | 1.200 | 430 | 13 dk | −%10 | 48 |
| 3 | 14 | 5.030 | 1.810 | 1 sa 10 dk | −%15 | 85 |
| 4 | 18 | 17.400 | 6.250 | 3 sa 35 dk | −%20 | 150 |
| 5 | 22 | 49.900 | 18.000 | 9 sa 40 dk | −%25 | 246 |
| 6 | 26 | 122.000 | 44.000 | 23 sa 45 dk | −%30 | 371 |
| 7 | 30 | 264.000 | 95.000 | 1 g 21 sa 15 dk | −%35 | 560 |
| 8 | 34 | 509.000 | 183.000 | 3 g 10 sa 30 dk | −%40 | 760 |
| 9 | 38 | 909.000 | 327.000 | 5 g 15 sa 45 dk | −%45 | 1.030 |
| 10 | 42 | 1.580.000 | 570.000 | 8 g 17 sa 15 dk | −%50 | 1.355 |

<a id="triage-protocol"></a>
## Triyaj Protokolü

`triage_protocol` · Yaralılar ağırlığa göre sıralanır, Revir yatakları boş kalmaz; iyileştirme hızlanır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 7 | 370 | 135 | 101 sn | %5 | 21 |
| 2 | 11 | 1.720 | 620 | 21 dk | %10 | 55 |
| 3 | 15 | 7.200 | 2.590 | 1 sa 41 dk | %15 | 98 |
| 4 | 19 | 23.300 | 8.380 | 4 sa 35 dk | %20 | 173 |
| 5 | 23 | 63.100 | 22.700 | 12 sa 20 dk | %25 | 272 |
| 6 | 27 | 148.000 | 53.300 | 1 g 4 sa | %30 | 411 |
| 7 | 31 | 311.000 | 112.000 | 2 g 4 sa 45 dk | %35 | 604 |
| 8 | 35 | 600.000 | 216.000 | 3 g 23 sa 45 dk | %40 | 820 |
| 9 | 39 | 1.040.000 | 376.000 | 6 g 8 sa 30 dk | %45 | 1.112 |
| 10 | 43 | 1.820.000 | 655.000 | 9 g 15 sa 15 dk | %50 | 1.439 |

<a id="infirmary-expansion"></a>
## Revir Genişletme

`infirmary_expansion` · Katlanır basınçlı yatak modülleri; Revir daha çok yaralı alır, taşma geç başlar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 8 | 545 | 195 | 3 dk | %4 | 28 |
| 2 | 12 | 2.460 | 885 | 34 dk | %8 | 64 |
| 3 | 16 | 9.650 | 3.480 | 2 sa 10 dk | %12 | 113 |
| 4 | 20 | 31.200 | 11.200 | 5 sa 55 dk | %16 | 200 |
| 5 | 24 | 79.700 | 28.700 | 15 sa 45 dk | %20 | 302 |
| 6 | 28 | 180.000 | 64.700 | 1 g 8 sa 45 dk | %24 | 456 |
| 7 | 32 | 367.000 | 132.000 | 2 g 13 sa 15 dk | %28 | 652 |
| 8 | 36 | 689.000 | 248.000 | 4 g 11 sa 30 dk | %32 | 885 |
| 9 | 40 | 1.200.000 | 432.000 | 7 g 3 sa 15 dk | %36 | 1.200 |
| 10 | 44 | 2.090.000 | 752.000 | 10 g 15 sa 30 dk | %40 | 1.529 |

<a id="field-rations"></a>
## Erzak Disiplini

`field_rations` · Kalori hesabıyla hazırlanan tayın; askerlerin saatlik gıda tüketimi düşer.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 9 | 810 | 290 | 6 dk | −%2 | 36 |
| 2 | 13 | 3.520 | 1.270 | 48 dk | −%4 | 74 |
| 3 | 17 | 12.900 | 4.660 | 2 sa 45 dk | −%6 | 130 |
| 4 | 21 | 39.400 | 14.200 | 7 sa 30 dk | −%8 | 222 |
| 5 | 25 | 101.000 | 36.300 | 20 sa 10 dk | −%10 | 335 |
| 6 | 29 | 218.000 | 78.400 | 1 g 14 sa 30 dk | −%12 | 505 |
| 7 | 33 | 432.000 | 156.000 | 2 g 23 sa | −%14 | 704 |
| 8 | 37 | 792.000 | 285.000 | 5 g 45 dk | −%16 | 955 |
| 9 | 41 | 1.380.000 | 496.000 | 7 g 21 sa 15 dk | −%18 | 1.275 |
| 10 | 45 | 2.400.000 | 864.000 | 11 g 18 sa 15 dk | −%20 | 1.625 |

<a id="aerogel-insulation"></a>
## Aerojel Yalıtım

`aerogel_insulation` · Dip suyu donma noktasına yakındır; ısıyı en az ileten katılardan aerojel ile kaplanan modüller daha az enerji harcar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 10 | 1.200 | 430 | 13 dk | −%2 | 48 |
| 2 | 14 | 5.030 | 1.810 | 1 sa 10 dk | −%4 | 85 |
| 3 | 18 | 17.400 | 6.250 | 3 sa 35 dk | −%6 | 150 |
| 4 | 22 | 49.900 | 18.000 | 9 sa 40 dk | −%8 | 246 |
| 5 | 26 | 122.000 | 44.000 | 23 sa 45 dk | −%10 | 371 |
| 6 | 30 | 264.000 | 95.000 | 1 g 21 sa 15 dk | −%12 | 560 |
| 7 | 34 | 509.000 | 183.000 | 3 g 10 sa 30 dk | −%14 | 760 |
| 8 | 38 | 909.000 | 327.000 | 5 g 15 sa 45 dk | −%16 | 1.030 |
| 9 | 42 | 1.580.000 | 570.000 | 8 g 17 sa 15 dk | −%18 | 1.355 |
| 10 | 46 | 2.760.000 | 992.000 | 12 g 18 sa | −%20 | 1.726 |

<a id="seismic-network"></a>
## Sismik Ağ

`seismic_network` · Hadal çukurları dalma-batma kuşaklarıdır, büyük depremler burada doğar; deniz tabanı sismometreleri sarsıntıyı önceden haber verir.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 12 | 2.460 | 885 | 34 dk | −%6 | 64 |
| 2 | 18 | 17.400 | 6.250 | 3 sa 35 dk | −%12 | 150 |
| 3 | 24 | 79.700 | 28.700 | 15 sa 45 dk | −%18 | 302 |
| 4 | 30 | 264.000 | 95.000 | 1 g 21 sa 15 dk | −%24 | 560 |
| 5 | 36 | 689.000 | 248.000 | 4 g 11 sa 30 dk | −%30 | 885 |

<a id="hyperbaric-medicine"></a>
## Hiperbarik Tıp

`hyperbaric_medicine` · Basınçlı odada saf oksijen solutmak yara iyileşmesini hızlandırır; iyileştirme daha az kaynak ister.

**Gerekli:** Triyaj Protokolü 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 14 | 5.030 | 1.810 | 1 sa 10 dk | −%3 | 85 |
| 2 | 17 | 12.900 | 4.660 | 2 sa 45 dk | −%6 | 130 |
| 3 | 20 | 31.200 | 11.200 | 5 sa 55 dk | −%9 | 200 |
| 4 | 23 | 63.100 | 22.700 | 12 sa 20 dk | −%12 | 272 |
| 5 | 26 | 122.000 | 44.000 | 23 sa 45 dk | −%15 | 371 |
| 6 | 29 | 218.000 | 78.400 | 1 g 14 sa 30 dk | −%18 | 505 |
| 7 | 32 | 367.000 | 132.000 | 2 g 13 sa 15 dk | −%21 | 652 |
| 8 | 35 | 600.000 | 216.000 | 3 g 23 sa 45 dk | −%24 | 820 |
| 9 | 38 | 909.000 | 327.000 | 5 g 15 sa 45 dk | −%27 | 1.030 |
| 10 | 41 | 1.380.000 | 496.000 | 7 g 21 sa 15 dk | −%30 | 1.275 |

<a id="hypoxia-drills"></a>
## Hipoksi Tatbikatı

`hypoxia_drills` · Mürettebat düşük oksijende çalışmayı tatbikatla öğrenir; oksijen açığında üretim daha az düşer.

**Gerekli:** Hava Geri Dönüşümü 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 15 | 7.200 | 2.590 | 1 sa 41 dk | −%8 | 98 |
| 2 | 21 | 39.400 | 14.200 | 7 sa 30 dk | −%16 | 222 |
| 3 | 27 | 148.000 | 53.300 | 1 g 4 sa | −%24 | 411 |
| 4 | 33 | 432.000 | 156.000 | 2 g 23 sa | −%32 | 704 |
| 5 | 39 | 1.040.000 | 376.000 | 6 g 8 sa 30 dk | −%40 | 1.112 |

<a id="cryo-protocol"></a>
## Kriyo Taşma Protokolü

`cryo_protocol` · Revir taştığında ağır yaralılar Kriyo Bakım Ünitesi'nde uyutulur; kurtarılan asker oranı artar.

**Gerekli:** Revir Genişletme 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 18 | 17.400 | 6.250 | 3 sa 35 dk | %2 | 150 |
| 2 | 24 | 79.700 | 28.700 | 15 sa 45 dk | %4 | 302 |
| 3 | 30 | 264.000 | 95.000 | 1 g 21 sa 15 dk | %6 | 560 |
| 4 | 36 | 689.000 | 248.000 | 4 g 11 sa 30 dk | %8 | 885 |
| 5 | 42 | 1.580.000 | 570.000 | 8 g 17 sa 15 dk | %10 | 1.355 |

<a id="evacuation-pods"></a>
## Tahliye Kapsülleri

`evacuation_pods` · Her bölmede basınçlı kaçış kapsülü bulunur; savaş kayıplarının daha büyük kısmı ölü yerine yaralı sayılır.

**Gerekli:** Triyaj Protokolü 5

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 31.200 | 11.200 | 5 sa 55 dk | %2 | 200 |
| 2 | 25 | 101.000 | 36.300 | 20 sa 10 dk | %4 | 335 |
| 3 | 30 | 264.000 | 95.000 | 1 g 21 sa 15 dk | %6 | 560 |
| 4 | 35 | 600.000 | 216.000 | 3 g 23 sa 45 dk | %8 | 820 |
| 5 | 40 | 1.200.000 | 432.000 | 7 g 3 sa 15 dk | %10 | 1.200 |

← [06 · Araştırma](../06_arastirma.md)
