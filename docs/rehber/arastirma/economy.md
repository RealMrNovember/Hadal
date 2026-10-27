<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Ekonomi

`economy` · Araştırma Merkezi · 16 teknoloji · 134 seviye

> Altı kaynağın üretimi, depo, ek üretim hatları, haritada toplama hızı ve yükü; geç dönemde karanlık oksijen ve metan hidrat.

**Açılış.** Araştırma Merkezi 1 (Reaktör 3)

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [Oksijen Verimi](#oxygen-yield) | 🟡 | 10 | Oksijen üretimi | %40 | 1–19 | 39.551 Titanyum · 14.257 Biyokütle | 8 sa 5 dk |
| [Gıda Verimi](#food-yield) | 🟡 | 10 | Gıda üretimi | %40 | 1–19 | 39.551 Titanyum · 14.257 Biyokütle | 8 sa 5 dk |
| [Biyokütle Verimi](#biomass-yield) | 🟡 | 10 | Biyokütle üretimi | %40 | 2–20 | 53.634 Titanyum · 19.317 Biyokütle | 10 sa 48 dk |
| [Titanyum Geri Kazanımı](#titanium-recovery) | 🟡 | 10 | Titanyum üretimi | %40 | 2–20 | 53.634 Titanyum · 19.317 Biyokütle | 10 sa 48 dk |
| [Depo Mühendisliği](#storage-engineering) | 🟡 | 10 | Tüm depo kapasitesi | %50 | 3–21 | 70.740 Titanyum · 25.457 Biyokütle | 14 sa |
| [Ek Oksijen Hattı](#extra-oxygen-line) | 🟡 | 2 | Oksijen Tesisi sınırı | 2 adet | 12–20 | 26.650 Titanyum · 9.590 Biyokütle | 5 sa 7 dk |
| [Ek Dron Hattı](#extra-drone-line) | 🟡 | 2 | Dron Fabrikası sınırı | 2 adet | 14–22 | 43.480 Titanyum · 15.630 Biyokütle | 8 sa 30 dk |
| [Hidrotermal Isı Eşanjörü](#hydrothermal-exchange) | ⬜ | 10 | Enerji üretimi | %30 | 8–44 | 3.675.825 Titanyum · 1.323.395 Biyokütle | 21 g 13 sa 8 dk |
| [Hidrolik Toplama Kolları](#gathering-arms) | ⬜ | 10 | Toplama hızı | %30 | 9–45 | 4.255.620 Titanyum · 1.532.920 Biyokütle | 24 g 4 sa 48 dk |
| [Hafif Yük Kapları](#cargo-bays) | ⬜ | 10 | Toplama yükü | %40 | 11–47 | 5.690.360 Titanyum · 2.048.070 Biyokütle | 30 g 1 sa 1 dk |
| [Deniz Karı Tuzakları](#marine-snow-traps) | ⬜ | 5 | Deniz karı olayı kazancı | %40 | 15–39 | 1.322.900 Titanyum · 476.450 Biyokütle | 8 g 14 sa 20 dk |
| [Kobalt Kabuğu Kazıma](#ferromanganese-crusts) | ⬜ | 10 | Titanyum üretimi | %20 | 20–47 | 7.078.300 Titanyum · 2.548.790 Biyokütle | 37 g 22 sa 25 dk |
| [Bakteri Matı Hasadı](#microbial-mat-harvest) | ⬜ | 10 | Biyokütle üretimi | %20 | 20–47 | 7.078.300 Titanyum · 2.548.790 Biyokütle | 37 g 22 sa 25 dk |
| [Kemosentez Kültürü](#chemosynthesis-culture) | ⬜ | 10 | Gıda üretimi | %20 | 21–48 | 8.165.300 Titanyum · 2.943.300 Biyokütle | 42 g 5 sa |
| [Karanlık Oksijen](#dark-oxygen) | ⬜ | 10 | Oksijen üretimi | %20 | 21–48 | 8.165.300 Titanyum · 2.943.300 Biyokütle | 42 g 5 sa |
| [Metan Hidrat Hücreleri](#methane-hydrate-cells) | ⬜ | 5 | Enerji üretimi | %10 | 38–46 | 6.750.000 Titanyum · 2.433.000 Biyokütle | 35 g 13 sa 30 dk |

<a id="oxygen-yield"></a>
## Oksijen Verimi

`oxygen_yield` · Elektroliz hücrelerinin elektrot yüzeyi büyütülür; deniz suyundan ayrışan oksijen artar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 11 | — | 1 sn | %4 | 4 |
| 2 | 3 | 35 | 12 | 5 sn | %8 | 7 |
| 3 | 5 | 135 | 50 | 20 sn | %12 | 12 |
| 4 | 7 | 290 | 105 | 80 sn | %16 | 21 |
| 5 | 9 | 640 | 230 | 5 dk | %20 | 36 |
| 6 | 11 | 1.360 | 490 | 16 dk | %24 | 55 |
| 7 | 13 | 2.780 | 1.000 | 38 dk | %28 | 74 |
| 8 | 15 | 5.700 | 2.050 | 1 sa 20 dk | %32 | 98 |
| 9 | 17 | 10.200 | 3.690 | 2 sa 10 dk | %36 | 130 |
| 10 | 19 | 18.400 | 6.630 | 3 sa 35 dk | %40 | 173 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 1 | 100 Titanyum · 30 Biyokütle | 90 sn |
| 2 | 3 | 140 Titanyum · 42 Biyokütle | 2 dk 15 sn |
| 3 | 5 | 196 Titanyum · 59 Biyokütle | 3 dk 22 sn |
| 4 | 7 | 274 Titanyum · 82 Biyokütle | 5 dk 4 sn |
| 5 | 9 | 384 Titanyum · 115 Biyokütle | 7 dk 36 sn |
| 6 | 11 | 538 Titanyum · 161 Biyokütle | 11 dk 23 sn |
| 7 | 13 | 753 Titanyum · 226 Biyokütle | 17 dk 5 sn |
| 8 | 15 | 1.054 Titanyum · 316 Biyokütle | 25 dk 38 sn |
| 9 | 17 | 1.476 Titanyum · 443 Biyokütle | 38 dk 27 sn |
| 10 | 19 | 2.066 Titanyum · 620 Biyokütle | 57 dk 40 sn |

<a id="food-yield"></a>
## Gıda Verimi

`food_yield` · Kemosentetik bakteri kültürlerinin besi ortamı iyileştirilir; Gıda Laboratuvarı daha çok üretir.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 11 | — | 1 sn | %4 | 4 |
| 2 | 3 | 35 | 12 | 5 sn | %8 | 7 |
| 3 | 5 | 135 | 50 | 20 sn | %12 | 12 |
| 4 | 7 | 290 | 105 | 80 sn | %16 | 21 |
| 5 | 9 | 640 | 230 | 5 dk | %20 | 36 |
| 6 | 11 | 1.360 | 490 | 16 dk | %24 | 55 |
| 7 | 13 | 2.780 | 1.000 | 38 dk | %28 | 74 |
| 8 | 15 | 5.700 | 2.050 | 1 sa 20 dk | %32 | 98 |
| 9 | 17 | 10.200 | 3.690 | 2 sa 10 dk | %36 | 130 |
| 10 | 19 | 18.400 | 6.630 | 3 sa 35 dk | %40 | 173 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 1 | 100 Titanyum · 30 Biyokütle | 90 sn |
| 2 | 3 | 140 Titanyum · 42 Biyokütle | 2 dk 15 sn |
| 3 | 5 | 196 Titanyum · 59 Biyokütle | 3 dk 22 sn |
| 4 | 7 | 274 Titanyum · 82 Biyokütle | 5 dk 4 sn |
| 5 | 9 | 384 Titanyum · 115 Biyokütle | 7 dk 36 sn |
| 6 | 11 | 538 Titanyum · 161 Biyokütle | 11 dk 23 sn |
| 7 | 13 | 753 Titanyum · 226 Biyokütle | 17 dk 5 sn |
| 8 | 15 | 1.054 Titanyum · 316 Biyokütle | 25 dk 38 sn |
| 9 | 17 | 1.476 Titanyum · 443 Biyokütle | 38 dk 27 sn |
| 10 | 19 | 2.066 Titanyum · 620 Biyokütle | 57 dk 40 sn |

<a id="biomass-yield"></a>
## Biyokütle Verimi

`biomass_yield` · Bakteri matlarının ve kültür tanklarının hasat döngüsü kısaltılır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 2 | 19 | 7 | 2 sn | %4 | 5 |
| 2 | 4 | 65 | 25 | 10 sn | %8 | 9 |
| 3 | 6 | 195 | 70 | 40 sn | %12 | 16 |
| 4 | 8 | 435 | 155 | 3 dk | %16 | 28 |
| 5 | 10 | 950 | 340 | 10 dk | %20 | 48 |
| 6 | 12 | 1.950 | 700 | 27 dk | %24 | 64 |
| 7 | 14 | 3.980 | 1.430 | 55 dk | %28 | 85 |
| 8 | 16 | 7.640 | 2.750 | 1 sa 43 dk | %32 | 113 |
| 9 | 18 | 13.700 | 4.950 | 2 sa 50 dk | %36 | 150 |
| 10 | 20 | 24.700 | 8.890 | 4 sa 40 dk | %40 | 200 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 2 | 120 Titanyum · 20 Biyokütle | 90 sn |
| 2 | 4 | 168 Titanyum · 28 Biyokütle | 2 dk 15 sn |
| 3 | 6 | 235 Titanyum · 39 Biyokütle | 3 dk 22 sn |
| 4 | 8 | 329 Titanyum · 55 Biyokütle | 5 dk 4 sn |
| 5 | 10 | 461 Titanyum · 77 Biyokütle | 7 dk 36 sn |
| 6 | 12 | 645 Titanyum · 108 Biyokütle | 11 dk 23 sn |
| 7 | 14 | 904 Titanyum · 151 Biyokütle | 17 dk 5 sn |
| 8 | 16 | 1.265 Titanyum · 211 Biyokütle | 25 dk 38 sn |
| 9 | 18 | 1.771 Titanyum · 295 Biyokütle | 38 dk 27 sn |
| 10 | 20 | 2.479 Titanyum · 413 Biyokütle | 57 dk 40 sn |

<a id="titanium-recovery"></a>
## Titanyum Geri Kazanımı

`titanium_recovery` · Dronların getirdiği hurdadan ve ilmenitten titanyum ayrıştırma verimi artar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 2 | 19 | 7 | 2 sn | %4 | 5 |
| 2 | 4 | 65 | 25 | 10 sn | %8 | 9 |
| 3 | 6 | 195 | 70 | 40 sn | %12 | 16 |
| 4 | 8 | 435 | 155 | 3 dk | %16 | 28 |
| 5 | 10 | 950 | 340 | 10 dk | %20 | 48 |
| 6 | 12 | 1.950 | 700 | 27 dk | %24 | 64 |
| 7 | 14 | 3.980 | 1.430 | 55 dk | %28 | 85 |
| 8 | 16 | 7.640 | 2.750 | 1 sa 43 dk | %32 | 113 |
| 9 | 18 | 13.700 | 4.950 | 2 sa 50 dk | %36 | 150 |
| 10 | 20 | 24.700 | 8.890 | 4 sa 40 dk | %40 | 200 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 2 | 120 Titanyum · 40 Biyokütle | 90 sn |
| 2 | 4 | 168 Titanyum · 56 Biyokütle | 2 dk 15 sn |
| 3 | 6 | 235 Titanyum · 78 Biyokütle | 3 dk 22 sn |
| 4 | 8 | 329 Titanyum · 110 Biyokütle | 5 dk 4 sn |
| 5 | 10 | 461 Titanyum · 154 Biyokütle | 7 dk 36 sn |
| 6 | 12 | 645 Titanyum · 215 Biyokütle | 11 dk 23 sn |
| 7 | 14 | 904 Titanyum · 301 Biyokütle | 17 dk 5 sn |
| 8 | 16 | 1.265 Titanyum · 422 Biyokütle | 25 dk 38 sn |
| 9 | 18 | 1.771 Titanyum · 590 Biyokütle | 38 dk 27 sn |
| 10 | 20 | 2.479 Titanyum · 826 Biyokütle | 57 dk 40 sn |

<a id="storage-engineering"></a>
## Depo Mühendisliği

`storage_engineering` · Basınç tankları daha yüksek doluluk oranında güvenle çalışır; tüm kaynak tavanları yükselir.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3 | 35 | 12 | 5 sn | %5 | 7 |
| 2 | 5 | 135 | 50 | 20 sn | %10 | 12 |
| 3 | 7 | 290 | 105 | 80 sn | %15 | 21 |
| 4 | 9 | 640 | 230 | 5 dk | %20 | 36 |
| 5 | 11 | 1.360 | 490 | 16 dk | %25 | 55 |
| 6 | 13 | 2.780 | 1.000 | 38 dk | %30 | 74 |
| 7 | 15 | 5.700 | 2.050 | 1 sa 20 dk | %35 | 98 |
| 8 | 17 | 10.200 | 3.690 | 2 sa 10 dk | %40 | 130 |
| 9 | 19 | 18.400 | 6.630 | 3 sa 35 dk | %45 | 173 |
| 10 | 21 | 31.200 | 11.200 | 5 sa 55 dk | %50 | 222 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 3 | 200 Titanyum · 60 Biyokütle | 2 dk |
| 2 | 5 | 280 Titanyum · 84 Biyokütle | 3 dk |
| 3 | 7 | 392 Titanyum · 118 Biyokütle | 4 dk 30 sn |
| 4 | 9 | 549 Titanyum · 165 Biyokütle | 6 dk 45 sn |
| 5 | 11 | 768 Titanyum · 230 Biyokütle | 10 dk 8 sn |
| 6 | 13 | 1.076 Titanyum · 323 Biyokütle | 15 dk 11 sn |
| 7 | 15 | 1.506 Titanyum · 452 Biyokütle | 22 dk 47 sn |
| 8 | 17 | 2.108 Titanyum · 632 Biyokütle | 34 dk 10 sn |
| 9 | 19 | 2.952 Titanyum · 885 Biyokütle | 51 dk 15 sn |
| 10 | 21 | 4.132 Titanyum · 1.240 Biyokütle | 1 sa 16 dk |

<a id="extra-oxygen-line"></a>
## Ek Oksijen Hattı

`extra_oxygen_line` · Elektroliz için ikinci bir deniz suyu besleme hattı çekilir; her seviye bir Oksijen Tesisi daha kurdurur.

**Gerekli:** Oksijen Verimi 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 12 | 1.950 | 700 | 27 dk | 1 adet | 64 |
| 2 | 20 | 24.700 | 8.890 | 4 sa 40 dk | 2 adet | 200 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 4 | 800 Titanyum · 200 Biyokütle | 15 dk |
| 2 | 6 | 1.120 Titanyum · 280 Biyokütle | 22 dk 30 sn |

<a id="extra-drone-line"></a>
## Ek Dron Hattı

`extra_drone_line` · Dron montaj bandı çoğaltılır; her seviye bir Dron Fabrikası daha kurdurur.

**Gerekli:** Titanyum Geri Kazanımı 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 14 | 3.980 | 1.430 | 55 dk | 1 adet | 85 |
| 2 | 22 | 39.500 | 14.200 | 7 sa 35 dk | 2 adet | 246 |

Bugünkü oyunda (catalog v8):

| Sv | Merkez Sv | Maliyet | Süre |
| ---: | ---: | --- | ---: |
| 1 | 5 | 1.000 Titanyum · 300 Biyokütle | 15 dk |
| 2 | 7 | 1.400 Titanyum · 420 Biyokütle | 22 dk 30 sn |

<a id="hydrothermal-exchange"></a>
## Hidrotermal Isı Eşanjörü

`hydrothermal_exchange` · Kara bacalardan yükselen yüzlerce derecelik akışkanın ısısı eşanjörle elektriğe çevrilir.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 8 | 435 | 155 | 3 dk | %3 | 28 |
| 2 | 12 | 1.950 | 700 | 27 dk | %6 | 64 |
| 3 | 16 | 7.640 | 2.750 | 1 sa 43 dk | %9 | 113 |
| 4 | 20 | 24.700 | 8.890 | 4 sa 40 dk | %12 | 200 |
| 5 | 24 | 63.100 | 22.700 | 12 sa 30 dk | %15 | 302 |
| 6 | 28 | 142.000 | 51.200 | 1 g 2 sa | %18 | 456 |
| 7 | 32 | 290.000 | 104.000 | 2 g 30 dk | %21 | 652 |
| 8 | 36 | 546.000 | 196.000 | 3 g 13 sa 15 dk | %24 | 885 |
| 9 | 40 | 950.000 | 342.000 | 5 g 15 sa 45 dk | %27 | 1.200 |
| 10 | 44 | 1.650.000 | 595.000 | 8 g 10 sa 15 dk | %30 | 1.529 |

<a id="gathering-arms"></a>
## Hidrolik Toplama Kolları

`gathering_arms` · Haritadaki nodül sahalarında, enkazlarda ve balina leşlerinde çalışan kepçeli kollar daha hızlı toplar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 9 | 640 | 230 | 5 dk | %3 | 36 |
| 2 | 13 | 2.780 | 1.000 | 38 dk | %6 | 74 |
| 3 | 17 | 10.200 | 3.690 | 2 sa 10 dk | %9 | 130 |
| 4 | 21 | 31.200 | 11.200 | 5 sa 55 dk | %12 | 222 |
| 5 | 25 | 79.800 | 28.700 | 16 sa | %15 | 335 |
| 6 | 29 | 172.000 | 62.100 | 1 g 6 sa 30 dk | %18 | 505 |
| 7 | 33 | 342.000 | 123.000 | 2 g 8 sa 15 dk | %21 | 704 |
| 8 | 37 | 627.000 | 226.000 | 3 g 23 sa 45 dk | %24 | 955 |
| 9 | 41 | 1.090.000 | 393.000 | 6 g 6 sa | %27 | 1.275 |
| 10 | 45 | 1.900.000 | 684.000 | 9 g 7 sa 30 dk | %30 | 1.625 |

<a id="cargo-bays"></a>
## Hafif Yük Kapları

`cargo_bays` · Denizaltıların yük bölmeleri sintaktik köpük dolgulu hafif kaplarla genişletilir; her sefer daha çok taşır.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 11 | 1.360 | 490 | 16 dk | %4 | 55 |
| 2 | 15 | 5.700 | 2.050 | 1 sa 20 dk | %8 | 98 |
| 3 | 19 | 18.400 | 6.630 | 3 sa 35 dk | %12 | 173 |
| 4 | 23 | 49.900 | 18.000 | 9 sa 45 dk | %16 | 272 |
| 5 | 27 | 117.000 | 42.200 | 22 sa 5 dk | %20 | 411 |
| 6 | 31 | 246.000 | 88.700 | 1 g 17 sa 45 dk | %24 | 604 |
| 7 | 35 | 475.000 | 171.000 | 3 g 3 sa 45 dk | %28 | 820 |
| 8 | 39 | 827.000 | 298.000 | 5 g 45 dk | %32 | 1.112 |
| 9 | 43 | 1.440.000 | 518.000 | 7 g 15 sa | %36 | 1.439 |
| 10 | 47 | 2.510.000 | 903.000 | 10 g 22 sa 45 dk | %40 | 1.834 |

<a id="marine-snow-traps"></a>
## Deniz Karı Tuzakları

`marine_snow_traps` · Oşinografların kullandığı huni biçimli sediment tuzakları, derinin ana besin kaynağı olan deniz karını toplar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 15 | 5.700 | 2.050 | 1 sa 20 dk | %8 | 98 |
| 2 | 21 | 31.200 | 11.200 | 5 sa 55 dk | %16 | 222 |
| 3 | 27 | 117.000 | 42.200 | 22 sa 5 dk | %24 | 411 |
| 4 | 33 | 342.000 | 123.000 | 2 g 8 sa 15 dk | %32 | 704 |
| 5 | 39 | 827.000 | 298.000 | 5 g 45 dk | %40 | 1.112 |

<a id="ferromanganese-crusts"></a>
## Kobalt Kabuğu Kazıma

`ferromanganese_crusts` · Deniz dağlarının yamaçlarını kaplayan demir-mangan kabukları kazınır; yan ürün olarak titanyum ayrıştırılır.

**Gerekli:** Titanyum Geri Kazanımı 10

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 24.700 | 8.890 | 4 sa 40 dk | %2 | 200 |
| 2 | 23 | 49.900 | 18.000 | 9 sa 45 dk | %4 | 272 |
| 3 | 26 | 96.700 | 34.800 | 18 sa 45 dk | %6 | 371 |
| 4 | 29 | 172.000 | 62.100 | 1 g 6 sa 30 dk | %8 | 505 |
| 5 | 32 | 290.000 | 104.000 | 2 g 30 dk | %10 | 652 |
| 6 | 35 | 475.000 | 171.000 | 3 g 3 sa 45 dk | %12 | 820 |
| 7 | 38 | 720.000 | 259.000 | 4 g 11 sa 30 dk | %14 | 1.030 |
| 8 | 41 | 1.090.000 | 393.000 | 6 g 6 sa | %16 | 1.275 |
| 9 | 44 | 1.650.000 | 595.000 | 8 g 10 sa 15 dk | %18 | 1.529 |
| 10 | 47 | 2.510.000 | 903.000 | 10 g 22 sa 45 dk | %20 | 1.834 |

<a id="microbial-mat-harvest"></a>
## Bakteri Matı Hasadı

`microbial_mat_harvest` · Soğuk sızıntılarda metan ve sülfürle beslenen bakteri matları kontrollü biçimde hasat edilir.

**Gerekli:** Biyokütle Verimi 10

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 24.700 | 8.890 | 4 sa 40 dk | %2 | 200 |
| 2 | 23 | 49.900 | 18.000 | 9 sa 45 dk | %4 | 272 |
| 3 | 26 | 96.700 | 34.800 | 18 sa 45 dk | %6 | 371 |
| 4 | 29 | 172.000 | 62.100 | 1 g 6 sa 30 dk | %8 | 505 |
| 5 | 32 | 290.000 | 104.000 | 2 g 30 dk | %10 | 652 |
| 6 | 35 | 475.000 | 171.000 | 3 g 3 sa 45 dk | %12 | 820 |
| 7 | 38 | 720.000 | 259.000 | 4 g 11 sa 30 dk | %14 | 1.030 |
| 8 | 41 | 1.090.000 | 393.000 | 6 g 6 sa | %16 | 1.275 |
| 9 | 44 | 1.650.000 | 595.000 | 8 g 10 sa 15 dk | %18 | 1.529 |
| 10 | 47 | 2.510.000 | 903.000 | 10 g 22 sa 45 dk | %20 | 1.834 |

<a id="chemosynthesis-culture"></a>
## Kemosentez Kültürü

`chemosynthesis_culture` · Baca bakterileri ışık yerine hidrojen sülfürü oksitleyerek besin üretir; kültürler bu enerjiyle büyütülür.

**Gerekli:** Gıda Verimi 10

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 31.200 | 11.200 | 5 sa 55 dk | %2 | 222 |
| 2 | 24 | 63.100 | 22.700 | 12 sa 30 dk | %4 | 302 |
| 3 | 27 | 117.000 | 42.200 | 22 sa 5 dk | %6 | 411 |
| 4 | 30 | 209.000 | 75.200 | 1 g 12 sa | %8 | 560 |
| 5 | 33 | 342.000 | 123.000 | 2 g 8 sa 15 dk | %10 | 704 |
| 6 | 36 | 546.000 | 196.000 | 3 g 13 sa 15 dk | %12 | 885 |
| 7 | 39 | 827.000 | 298.000 | 5 g 45 dk | %14 | 1.112 |
| 8 | 42 | 1.250.000 | 451.000 | 6 g 21 sa 45 dk | %16 | 1.355 |
| 9 | 45 | 1.900.000 | 684.000 | 9 g 7 sa 30 dk | %18 | 1.625 |
| 10 | 48 | 2.880.000 | 1.040.000 | 11 g 21 sa | %20 | 1.949 |

<a id="dark-oxygen"></a>
## Karanlık Oksijen

`dark_oxygen` · Deneysel dal: 2024'te nodül sahasında ışıksız ortamda oksijenin arttığı ölçüldü ama bulgu henüz tekrarlanmadı; tesis, nodül yüzeyindeki gerilimin suyu ayırabileceği fikrini sınar.

**Gerekli:** Oksijen Verimi 10

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 31.200 | 11.200 | 5 sa 55 dk | %2 | 222 |
| 2 | 24 | 63.100 | 22.700 | 12 sa 30 dk | %4 | 302 |
| 3 | 27 | 117.000 | 42.200 | 22 sa 5 dk | %6 | 411 |
| 4 | 30 | 209.000 | 75.200 | 1 g 12 sa | %8 | 560 |
| 5 | 33 | 342.000 | 123.000 | 2 g 8 sa 15 dk | %10 | 704 |
| 6 | 36 | 546.000 | 196.000 | 3 g 13 sa 15 dk | %12 | 885 |
| 7 | 39 | 827.000 | 298.000 | 5 g 45 dk | %14 | 1.112 |
| 8 | 42 | 1.250.000 | 451.000 | 6 g 21 sa 45 dk | %16 | 1.355 |
| 9 | 45 | 1.900.000 | 684.000 | 9 g 7 sa 30 dk | %18 | 1.625 |
| 10 | 48 | 2.880.000 | 1.040.000 | 11 g 21 sa | %20 | 1.949 |

<a id="methane-hydrate-cells"></a>
## Metan Hidrat Hücreleri

`methane_hydrate_cells` · Yüksek basınç ve soğukta su kafesine hapsolan metan kontrollü çözülüp yakıt hücresinde yakılır.

**Gerekli:** Hidrotermal Isı Eşanjörü 8

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 38 | 720.000 | 259.000 | 4 g 11 sa 30 dk | %2 | 1.030 |
| 2 | 40 | 950.000 | 342.000 | 5 g 15 sa 45 dk | %4 | 1.200 |
| 3 | 42 | 1.250.000 | 451.000 | 6 g 21 sa 45 dk | %6 | 1.355 |
| 4 | 44 | 1.650.000 | 595.000 | 8 g 10 sa 15 dk | %8 | 1.529 |
| 5 | 46 | 2.180.000 | 786.000 | 10 g 2 sa 15 dk | %10 | 1.726 |

← [06 · Araştırma](../06_arastirma.md)
