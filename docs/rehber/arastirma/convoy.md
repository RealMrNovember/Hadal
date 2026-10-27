<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Konvoy

`convoy` · Bilim Kubbesi · 9 teknoloji · 56 seviye

> İttifak içi takas konvoyları: yük, hız, vergi, eskort, sigorta, ek hat, kaçakçı rafları; Leviathan Konvoyu maçı.

**Açılış.** Bilim Kubbesi 2 (Reaktör 20)

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [Konvoy Yük Kapasitesi](#convoy-capacity) | ⬜ | 10 | Konvoy yükü | %50 | 20–47 | 2.608.600 Titanyum · 939.600 Biyokütle | 13 g 23 sa 18 dk |
| [Derin Akıntı Rotaları](#deep-currents) | ⬜ | 10 | Konvoy hızı | %40 | 21–48 | 3.009.000 Titanyum · 1.083.610 Biyokütle | 15 g 13 sa 10 dk |
| [Takas Vergisi Muafiyeti](#trade-exemption) | ⬜ | 5 | Takas vergisi | −5 puan | 22–42 | 857.700 Titanyum · 308.440 Biyokütle | 5 g 5 sa 5 dk |
| [Eskort Protokolü](#escort-protocol) | ⬜ | 10 | Saldırıda konvoy kaybı | −%40 | 23–50 | 3.999.500 Titanyum · 1.440.320 Biyokütle | 19 g 1 sa 5 dk |
| [Ek Konvoy Hattı](#extra-convoy-line) | ⬜ | 3 | Günlük konvoy | 3 konvoy | 24–40 | 480.300 Titanyum · 172.870 Biyokütle | 3 g 25 dk |
| [Kaçakçı Ağı](#smuggler-network) | ⬜ | 3 | Kaçakçı rafı | 3 raf | 25–41 | 557.400 Titanyum · 201.000 Biyokütle | 3 g 9 sa 55 dk |
| [Saçılma Katmanı Gizlemesi](#scattering-layer) | ⬜ | 5 | Konvoyun yakalanma olasılığı | −%25 | 26–42 | 988.600 Titanyum · 355.500 Biyokütle | 6 g 40 dk |
| [Tersane Rotasyonu](#shipyard-rotation) | ⬜ | 5 | Leviathan Konvoyu'nda yeniden çıkış süresi | −%40 | 24–44 | 1.149.800 Titanyum · 413.770 Biyokütle | 6 g 14 sa 50 dk |
| [Konvoy Sigortası](#convoy-insurance) | ⬜ | 5 | Kayıp yükün iadesi | %25 | 28–44 | 1.319.400 Titanyum · 474.800 Biyokütle | 7 g 15 sa 25 dk |

<a id="convoy-capacity"></a>
## Konvoy Yük Kapasitesi

`convoy_capacity` · Takas konvoylarına çekilen ek yük römorkları; bir konvoy daha çok kaynak taşır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 9.100 | 3.280 | 1 sa 43 dk | %5 | 200 |
| 2 | 23 | 18.400 | 6.620 | 3 sa 35 dk | %10 | 272 |
| 3 | 26 | 35.600 | 12.800 | 6 sa 55 dk | %15 | 371 |
| 4 | 29 | 63.500 | 22.900 | 11 sa 15 dk | %20 | 505 |
| 5 | 32 | 107.000 | 38.500 | 17 sa 50 dk | %25 | 652 |
| 6 | 35 | 175.000 | 63.000 | 1 g 4 sa | %30 | 820 |
| 7 | 38 | 265.000 | 95.500 | 1 g 15 sa 30 dk | %35 | 1.030 |
| 8 | 41 | 402.000 | 145.000 | 2 g 7 sa 15 dk | %40 | 1.275 |
| 9 | 44 | 609.000 | 219.000 | 3 g 2 sa 30 dk | %45 | 1.529 |
| 10 | 47 | 924.000 | 333.000 | 4 g 45 dk | %50 | 1.834 |

<a id="deep-currents"></a>
## Derin Akıntı Rotaları

`deep_currents` · Derin okyanus akıntıları yavaş ama kesintisizdir; rotaları bu akıntılara göre çizilen konvoylar hızlanır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 11.500 | 4.140 | 2 sa 10 dk | %4 | 222 |
| 2 | 24 | 23.300 | 8.370 | 4 sa 35 dk | %8 | 302 |
| 3 | 27 | 43.200 | 15.600 | 8 sa 10 dk | %12 | 411 |
| 4 | 30 | 77.000 | 27.700 | 13 sa 15 dk | %16 | 560 |
| 5 | 33 | 126.000 | 45.400 | 20 sa 45 dk | %20 | 704 |
| 6 | 36 | 201.000 | 72.400 | 1 g 7 sa 30 dk | %24 | 885 |
| 7 | 39 | 305.000 | 110.000 | 1 g 20 sa 30 dk | %28 | 1.112 |
| 8 | 42 | 462.000 | 166.000 | 2 g 13 sa | %32 | 1.355 |
| 9 | 45 | 700.000 | 252.000 | 3 g 10 sa 15 dk | %36 | 1.625 |
| 10 | 48 | 1.060.000 | 382.000 | 4 g 9 sa | %40 | 1.949 |

<a id="trade-exemption"></a>
## Takas Vergisi Muafiyeti

`trade_exemption` · Takas İstasyonu'nda ittifak içi gönderimlerin vergisi her seviyede bir puan daha düşer.

**Gerekli:** Konvoy Yük Kapasitesi 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 22 | 14.500 | 5.240 | 2 sa 50 dk | −1 puan | 246 |
| 2 | 27 | 43.200 | 15.600 | 8 sa 10 dk | −2 puan | 411 |
| 3 | 32 | 107.000 | 38.500 | 17 sa 50 dk | −3 puan | 652 |
| 4 | 37 | 231.000 | 83.100 | 1 g 11 sa 15 dk | −4 puan | 955 |
| 5 | 42 | 462.000 | 166.000 | 2 g 13 sa | −5 puan | 1.355 |

<a id="escort-protocol"></a>
## Eskort Protokolü

`escort_protocol` · Konvoylara silahlı dron eskortu katılır; yolda saldırıya uğrayan konvoy daha az kaybeder.

**Gerekli:** Konvoy Yük Kapasitesi 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 23 | 18.400 | 6.620 | 3 sa 35 dk | −%4 | 272 |
| 2 | 26 | 35.600 | 12.800 | 6 sa 55 dk | −%8 | 371 |
| 3 | 29 | 63.500 | 22.900 | 11 sa 15 dk | −%12 | 505 |
| 4 | 32 | 107.000 | 38.500 | 17 sa 50 dk | −%16 | 652 |
| 5 | 35 | 175.000 | 63.000 | 1 g 4 sa | −%20 | 820 |
| 6 | 38 | 265.000 | 95.500 | 1 g 15 sa 30 dk | −%24 | 1.030 |
| 7 | 41 | 402.000 | 145.000 | 2 g 7 sa 15 dk | −%28 | 1.275 |
| 8 | 44 | 609.000 | 219.000 | 3 g 2 sa 30 dk | −%32 | 1.529 |
| 9 | 47 | 924.000 | 333.000 | 4 g 45 dk | −%36 | 1.834 |
| 10 | 50 | 1.400.000 | 504.000 | 5 g 3 sa 30 dk | −%40 | 2.200 |

<a id="extra-convoy-line"></a>
## Ek Konvoy Hattı

`extra_convoy_line` · Takas İstasyonu'na yeni yanaşma hattı; her seviye günde bir konvoy daha gönderdirir.

**Gerekli:** Konvoy Yük Kapasitesi 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 24 | 23.300 | 8.370 | 4 sa 35 dk | 1 konvoy | 302 |
| 2 | 32 | 107.000 | 38.500 | 17 sa 50 dk | 2 konvoy | 652 |
| 3 | 40 | 350.000 | 126.000 | 2 g 2 sa | 3 konvoy | 1.200 |

<a id="smuggler-network"></a>
## Kaçakçı Ağı

`smuggler_network` · Gezgin kaçakçılarla güven ilişkisi kurulur; her ziyarette bir raf daha açılır.

**Gerekli:** Takas Vergisi Muafiyeti 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 25 | 29.400 | 10.600 | 5 sa 55 dk | 1 raf | 335 |
| 2 | 33 | 126.000 | 45.400 | 20 sa 45 dk | 2 raf | 704 |
| 3 | 41 | 402.000 | 145.000 | 2 g 7 sa 15 dk | 3 raf | 1.275 |

<a id="scattering-layer"></a>
## Saçılma Katmanı Gizlemesi

`scattering_layer` · Her gece yukarı göç eden canlıların oluşturduğu derin saçılma katmanı sonarı yanıltır, ilk ölçümlerde deniz tabanı sanılmıştı; konvoylar bu katmanda gizlenir.

**Gerekli:** Eskort Protokolü 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 26 | 35.600 | 12.800 | 6 sa 55 dk | −%5 | 371 |
| 2 | 30 | 77.000 | 27.700 | 13 sa 15 dk | −%10 | 560 |
| 3 | 34 | 149.000 | 53.500 | 1 g | −%15 | 760 |
| 4 | 38 | 265.000 | 95.500 | 1 g 15 sa 30 dk | −%20 | 1.030 |
| 5 | 42 | 462.000 | 166.000 | 2 g 13 sa | −%25 | 1.355 |

<a id="shipyard-rotation"></a>
## Tersane Rotasyonu

`shipyard_rotation` · Tersane Kapısı'nda yedek mürettebat hazır bekler; Leviathan Konvoyu maçında yenilen yürüyüş daha erken yeniden çıkar.

**Gerekli:** Konvoy Yük Kapasitesi 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 24 | 23.300 | 8.370 | 4 sa 35 dk | −%8 | 302 |
| 2 | 29 | 63.500 | 22.900 | 11 sa 15 dk | −%16 | 505 |
| 3 | 34 | 149.000 | 53.500 | 1 g | −%24 | 760 |
| 4 | 39 | 305.000 | 110.000 | 1 g 20 sa 30 dk | −%32 | 1.112 |
| 5 | 44 | 609.000 | 219.000 | 3 g 2 sa 30 dk | −%40 | 1.529 |

<a id="convoy-insurance"></a>
## Konvoy Sigortası

`convoy_insurance` · İttifak kasasından işleyen sigorta; saldırıda kaybedilen yükün bir kısmı geri döner.

**Gerekli:** Eskort Protokolü 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 28 | 52.400 | 18.900 | 9 sa 35 dk | %5 | 456 |
| 2 | 32 | 107.000 | 38.500 | 17 sa 50 dk | %10 | 652 |
| 3 | 36 | 201.000 | 72.400 | 1 g 7 sa 30 dk | %15 | 885 |
| 4 | 40 | 350.000 | 126.000 | 2 g 2 sa | %20 | 1.200 |
| 5 | 44 | 609.000 | 219.000 | 3 g 2 sa 30 dk | %25 | 1.529 |

← [06 · Araştırma](../06_arastirma.md)
