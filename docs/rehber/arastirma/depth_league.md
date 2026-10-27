<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Derinlik Ligi

`depth_league` · Bilim Kubbesi · 8 teknoloji · 40 seviye

> Haftalık ittifak ligi: her günün temasında (inşa, araştırma, kahraman, toplama, eğitim, av) daha çok puan ve kademe ödülü.

**Açılış.** Bilim Kubbesi 2 (Reaktör 20)

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [Lig: İnşa Günü](#league-build) | ⬜ | 5 | İnşa günü lig puanı | %25 | 20–40 | 640.500 Titanyum · 230.580 Biyokütle | 4 g 2 sa 53 dk |
| [Lig: Araştırma Günü](#league-research) | ⬜ | 5 | Araştırma günü lig puanı | %25 | 21–41 | 740.800 Titanyum · 267.040 Biyokütle | 4 g 15 sa 10 dk |
| [Lig: Kahraman Günü](#league-hero) | ⬜ | 5 | Kahraman günü lig puanı | %25 | 22–42 | 857.700 Titanyum · 308.440 Biyokütle | 5 g 5 sa 5 dk |
| [Lig: Toplama Günü](#league-gather) | ⬜ | 5 | Toplama günü lig puanı | %25 | 23–43 | 992.800 Titanyum · 357.420 Biyokütle | 5 g 20 sa 55 dk |
| [Lig: Eğitim Günü](#league-training) | ⬜ | 5 | Eğitim günü lig puanı | %25 | 24–44 | 1.149.800 Titanyum · 413.770 Biyokütle | 6 g 14 sa 50 dk |
| [Lig: Av Günü](#league-hunt) | ⬜ | 5 | Av günü lig puanı | %25 | 25–45 | 1.331.400 Titanyum · 479.300 Biyokütle | 7 g 11 sa 25 dk |
| [Kademe Ödülleri](#league-tier-rewards) | ⬜ | 5 | Günlük kademe ödülü (Titanyum, hızlandırma) | %25 | 26–46 | 1.533.300 Titanyum · 551.900 Biyokütle | 8 g 6 sa 15 dk |
| [Lig Koordinasyonu](#league-coordination) | ⬜ | 5 | Tüm günlerde lig puanı | %10 | 30–46 | 1.757.000 Titanyum · 631.700 Biyokütle | 9 g 11 sa |

<a id="league-build"></a>
## Lig: İnşa Günü

`league_build` · Derinlik Ligi'nin Pazartesi (inşa) gününde yapılan her iş daha çok puan getirir.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 20 | 9.100 | 3.280 | 1 sa 43 dk | %5 | 200 |
| 2 | 25 | 29.400 | 10.600 | 5 sa 55 dk | %10 | 335 |
| 3 | 30 | 77.000 | 27.700 | 13 sa 15 dk | %15 | 560 |
| 4 | 35 | 175.000 | 63.000 | 1 g 4 sa | %20 | 820 |
| 5 | 40 | 350.000 | 126.000 | 2 g 2 sa | %25 | 1.200 |

<a id="league-research"></a>
## Lig: Araştırma Günü

`league_research` · Derinlik Ligi'nin Salı (araştırma) gününde yapılan her iş daha çok puan getirir.

**Gerekli:** Lig: İnşa Günü 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 21 | 11.500 | 4.140 | 2 sa 10 dk | %5 | 222 |
| 2 | 26 | 35.600 | 12.800 | 6 sa 55 dk | %10 | 371 |
| 3 | 31 | 90.700 | 32.700 | 15 sa 20 dk | %15 | 604 |
| 4 | 36 | 201.000 | 72.400 | 1 g 7 sa 30 dk | %20 | 885 |
| 5 | 41 | 402.000 | 145.000 | 2 g 7 sa 15 dk | %25 | 1.275 |

<a id="league-hero"></a>
## Lig: Kahraman Günü

`league_hero` · Derinlik Ligi'nin Çarşamba (kahraman) gününde yapılan her iş daha çok puan getirir.

**Gerekli:** Lig: İnşa Günü 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 22 | 14.500 | 5.240 | 2 sa 50 dk | %5 | 246 |
| 2 | 27 | 43.200 | 15.600 | 8 sa 10 dk | %10 | 411 |
| 3 | 32 | 107.000 | 38.500 | 17 sa 50 dk | %15 | 652 |
| 4 | 37 | 231.000 | 83.100 | 1 g 11 sa 15 dk | %20 | 955 |
| 5 | 42 | 462.000 | 166.000 | 2 g 13 sa | %25 | 1.355 |

<a id="league-gather"></a>
## Lig: Toplama Günü

`league_gather` · Derinlik Ligi'nin Perşembe (toplama) gününde yapılan her iş daha çok puan getirir.

**Gerekli:** Lig: İnşa Günü 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 23 | 18.400 | 6.620 | 3 sa 35 dk | %5 | 272 |
| 2 | 28 | 52.400 | 18.900 | 9 sa 35 dk | %10 | 456 |
| 3 | 33 | 126.000 | 45.400 | 20 sa 45 dk | %15 | 704 |
| 4 | 38 | 265.000 | 95.500 | 1 g 15 sa 30 dk | %20 | 1.030 |
| 5 | 43 | 531.000 | 191.000 | 2 g 19 sa 30 dk | %25 | 1.439 |

<a id="league-training"></a>
## Lig: Eğitim Günü

`league_training` · Derinlik Ligi'nin Cuma (eğitim) gününde yapılan her iş daha çok puan getirir.

**Gerekli:** Lig: İnşa Günü 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 24 | 23.300 | 8.370 | 4 sa 35 dk | %5 | 302 |
| 2 | 29 | 63.500 | 22.900 | 11 sa 15 dk | %10 | 505 |
| 3 | 34 | 149.000 | 53.500 | 1 g | %15 | 760 |
| 4 | 39 | 305.000 | 110.000 | 1 g 20 sa 30 dk | %20 | 1.112 |
| 5 | 44 | 609.000 | 219.000 | 3 g 2 sa 30 dk | %25 | 1.529 |

<a id="league-hunt"></a>
## Lig: Av Günü

`league_hunt` · Derinlik Ligi'nin Cumartesi (av) gününde yapılan her iş daha çok puan getirir.

**Gerekli:** Lig: İnşa Günü 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 25 | 29.400 | 10.600 | 5 sa 55 dk | %5 | 335 |
| 2 | 30 | 77.000 | 27.700 | 13 sa 15 dk | %10 | 560 |
| 3 | 35 | 175.000 | 63.000 | 1 g 4 sa | %15 | 820 |
| 4 | 40 | 350.000 | 126.000 | 2 g 2 sa | %20 | 1.200 |
| 5 | 45 | 700.000 | 252.000 | 3 g 10 sa 15 dk | %25 | 1.625 |

<a id="league-tier-rewards"></a>
## Kademe Ödülleri

`league_tier_rewards` · Lig kademesine ulaşınca açılan sandığın Titanyum ve hızlandırma içeriği artar; Hadalit payı değişmez.

**Gerekli:** Lig: İnşa Günü 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 26 | 35.600 | 12.800 | 6 sa 55 dk | %5 | 371 |
| 2 | 31 | 90.700 | 32.700 | 15 sa 20 dk | %10 | 604 |
| 3 | 36 | 201.000 | 72.400 | 1 g 7 sa 30 dk | %15 | 885 |
| 4 | 41 | 402.000 | 145.000 | 2 g 7 sa 15 dk | %20 | 1.275 |
| 5 | 46 | 804.000 | 289.000 | 3 g 17 sa 15 dk | %25 | 1.726 |

<a id="league-coordination"></a>
## Lig Koordinasyonu

`league_coordination` · İttifakın haftalık lig planı ortak takvimde tutulur; her gün biraz daha çok puan kazanılır.

**Gerekli:** Lig: Av Günü 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 30 | 77.000 | 27.700 | 13 sa 15 dk | %2 | 560 |
| 2 | 34 | 149.000 | 53.500 | 1 g | %4 | 760 |
| 3 | 38 | 265.000 | 95.500 | 1 g 15 sa 30 dk | %6 | 1.030 |
| 4 | 42 | 462.000 | 166.000 | 2 g 13 sa | %8 | 1.355 |
| 5 | 46 | 804.000 | 289.000 | 3 g 17 sa 15 dk | %10 | 1.726 |

← [06 · Araştırma](../06_arastirma.md)
