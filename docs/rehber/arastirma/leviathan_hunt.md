<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/arastirma.json -->
# Leviathan Avı

`leviathan_hunt` · Bilim Kubbesi · 9 teknoloji · 56 seviye

> Leviathan'lara karşı ittifak rallileri ve haftalık av: hasar, zırh delme, ralli, ganimet, Sonar Enerjisi, Av Feneri ve Av Halkası.

**Açılış.** Bilim Kubbesi 3 (Reaktör 30)

| Teknoloji | Durum | Maks | Etki | Toplam etki | Lab Sv | Toplam maliyet | Toplam süre |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [Zıpkın Halatları](#harpoon-tethers) | ⬜ | 10 | Leviathan'a hasar | %20 | 30–48 | 4.666.000 Titanyum · 1.681.500 Biyokütle | 24 g 2 sa |
| [Leviathan İz Sürme](#hunt-tracking) | ⬜ | 5 | Avda Sonar Enerjisi maliyeti | −%25 | 30–46 | 2.008.000 Titanyum · 722.800 Biyokütle | 10 g 19 sa 35 dk |
| [Av Rallisi](#hunt-rally) | ⬜ | 10 | Leviathan rallisi kapasitesi | %30 | 31–49 | 5.375.000 Titanyum · 1.933.100 Biyokütle | 26 g 15 sa 15 dk |
| [Av Ganimeti](#hunt-loot) | ⬜ | 10 | Leviathan ganimeti | %40 | 31–49 | 5.375.000 Titanyum · 1.933.100 Biyokütle | 26 g 15 sa 15 dk |
| [Biyolüminesans Fener](#bioluminescent-beacon) | ⬜ | 5 | Av Feneri dayanıklılığı | %25 | 32–44 | 1.780.000 Titanyum · 641.000 Biyokütle | 10 g 5 sa 55 dk |
| [Demir Plaka Delici](#iron-plate-piercing) | ⬜ | 5 | Demir Leviathan zırhını delme | %15 | 33–45 | 2.050.000 Titanyum · 737.500 Biyokütle | 11 g 9 sa 55 dk |
| [Av Sağlıkçıları](#hunt-medics) | ⬜ | 5 | PvE yaralılarının dinlenme süresi | −%30 | 34–46 | 2.359.000 Titanyum · 849.100 Biyokütle | 12 g 15 sa 45 dk |
| [Akıntıyla Toplanma](#current-muster) | ⬜ | 5 | Av rallisi yürüyüş hızı | %20 | 35–47 | 2.718.000 Titanyum · 977.000 Biyokütle | 14 g 15 dk |
| [Üçüncü Av Hattı](#third-hunt-line) | ⬜ | 1 | Av Halkası'na yerleşen yürüyüş | 1 yürüyüş | 38–38 | 303.000 Titanyum · 109.000 Biyokütle | 1 g 21 sa 15 dk |

<a id="harpoon-tethers"></a>
## Zıpkın Halatları

`harpoon_tethers` · Çelikten hafif, yüzen sentetik halatlarla bağlanan zıpkınlar dev gövdeyi bırakmaz; Leviathan'lara verilen hasar artar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 30 | 88.000 | 31.700 | 15 sa 5 dk | %2 | 560 |
| 2 | 32 | 122.000 | 44.000 | 20 sa 25 dk | %4 | 652 |
| 3 | 34 | 170.000 | 61.100 | 1 g 3 sa 30 dk | %6 | 760 |
| 4 | 36 | 230.000 | 82.700 | 1 g 11 sa 45 dk | %8 | 885 |
| 5 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | %10 | 1.030 |
| 6 | 40 | 400.000 | 144.000 | 2 g 9 sa | %12 | 1.200 |
| 7 | 42 | 528.000 | 190.000 | 2 g 21 sa 45 dk | %14 | 1.355 |
| 8 | 44 | 696.000 | 251.000 | 3 g 13 sa 15 dk | %16 | 1.529 |
| 9 | 46 | 919.000 | 331.000 | 4 g 6 sa | %18 | 1.726 |
| 10 | 48 | 1.210.000 | 437.000 | 5 g | %20 | 1.949 |

<a id="hunt-tracking"></a>
## Leviathan İz Sürme

`hunt_tracking` · Leviathan'ların pasif sonar izi kataloglanır; ava çıkmak daha az Sonar Enerjisi harcar.

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 30 | 88.000 | 31.700 | 15 sa 5 dk | −%5 | 560 |
| 2 | 34 | 170.000 | 61.100 | 1 g 3 sa 30 dk | −%10 | 760 |
| 3 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | −%15 | 1.030 |
| 4 | 42 | 528.000 | 190.000 | 2 g 21 sa 45 dk | −%20 | 1.355 |
| 5 | 46 | 919.000 | 331.000 | 4 g 6 sa | −%25 | 1.726 |

<a id="hunt-rally"></a>
## Av Rallisi

`hunt_rally` · Av rallisi için ittifak denizaltılarının dizilişi standartlaşır; rallide daha çok asker toplanır.

**Gerekli:** Zıpkın Halatları 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 31 | 104.000 | 37.300 | 17 sa 35 dk | %3 | 604 |
| 2 | 33 | 144.000 | 51.800 | 23 sa 40 dk | %6 | 704 |
| 3 | 35 | 200.000 | 72.000 | 1 g 8 sa | %9 | 820 |
| 4 | 37 | 264.000 | 95.000 | 1 g 16 sa 15 dk | %12 | 955 |
| 5 | 39 | 348.000 | 125.000 | 2 g 2 sa 45 dk | %15 | 1.112 |
| 6 | 41 | 459.000 | 165.000 | 2 g 15 sa | %18 | 1.275 |
| 7 | 43 | 606.000 | 218.000 | 3 g 5 sa | %21 | 1.439 |
| 8 | 45 | 800.000 | 288.000 | 3 g 22 sa | %24 | 1.625 |
| 9 | 47 | 1.060.000 | 380.000 | 4 g 14 sa 45 dk | %27 | 1.834 |
| 10 | 49 | 1.390.000 | 501.000 | 5 g 10 sa 15 dk | %30 | 2.071 |

<a id="hunt-loot"></a>
## Av Ganimeti

`hunt_loot` · Leviathan kalıntılarından Hadalit, kalıntı ve teçhizat malzemesi ayıklanır; av ganimeti artar.

**Gerekli:** Leviathan İz Sürme 1

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 31 | 104.000 | 37.300 | 17 sa 35 dk | %4 | 604 |
| 2 | 33 | 144.000 | 51.800 | 23 sa 40 dk | %8 | 704 |
| 3 | 35 | 200.000 | 72.000 | 1 g 8 sa | %12 | 820 |
| 4 | 37 | 264.000 | 95.000 | 1 g 16 sa 15 dk | %16 | 955 |
| 5 | 39 | 348.000 | 125.000 | 2 g 2 sa 45 dk | %20 | 1.112 |
| 6 | 41 | 459.000 | 165.000 | 2 g 15 sa | %24 | 1.275 |
| 7 | 43 | 606.000 | 218.000 | 3 g 5 sa | %28 | 1.439 |
| 8 | 45 | 800.000 | 288.000 | 3 g 22 sa | %32 | 1.625 |
| 9 | 47 | 1.060.000 | 380.000 | 4 g 14 sa 45 dk | %36 | 1.834 |
| 10 | 49 | 1.390.000 | 501.000 | 5 g 10 sa 15 dk | %40 | 2.071 |

<a id="bioluminescent-beacon"></a>
## Biyolüminesans Fener

`bioluminescent_beacon` · Fener balığının ışıyan oltasından esinlenen fener, sürüleri hatlara çeker; Leviathan Avı'nda Av Feneri daha geç düşer.

**Gerekli:** Leviathan İz Sürme 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 32 | 122.000 | 44.000 | 20 sa 25 dk | %5 | 652 |
| 2 | 35 | 200.000 | 72.000 | 1 g 8 sa | %10 | 820 |
| 3 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | %15 | 1.030 |
| 4 | 41 | 459.000 | 165.000 | 2 g 15 sa | %20 | 1.275 |
| 5 | 44 | 696.000 | 251.000 | 3 g 13 sa 15 dk | %25 | 1.529 |

<a id="iron-plate-piercing"></a>
## Demir Plaka Delici

`iron_plate_piercing` · Delicilerin elmas uçları Demir Leviathan'ın metal plakaları için yeniden bilenir; zırh delme artar.

**Gerekli:** Zıpkın Halatları 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 33 | 144.000 | 51.800 | 23 sa 40 dk | %3 | 704 |
| 2 | 36 | 230.000 | 82.700 | 1 g 11 sa 45 dk | %6 | 885 |
| 3 | 39 | 348.000 | 125.000 | 2 g 2 sa 45 dk | %9 | 1.112 |
| 4 | 42 | 528.000 | 190.000 | 2 g 21 sa 45 dk | %12 | 1.355 |
| 5 | 45 | 800.000 | 288.000 | 3 g 22 sa | %15 | 1.625 |

<a id="hunt-medics"></a>
## Av Sağlıkçıları

`hunt_medics` · Av rallilerine eşlik eden sağlık denizaltısı; PvE'de yaralanan askerler Askeri Kubbe'de daha kısa dinlenir.

**Gerekli:** Av Rallisi 2

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 34 | 170.000 | 61.100 | 1 g 3 sa 30 dk | −%6 | 760 |
| 2 | 37 | 264.000 | 95.000 | 1 g 16 sa 15 dk | −%12 | 955 |
| 3 | 40 | 400.000 | 144.000 | 2 g 9 sa | −%18 | 1.200 |
| 4 | 43 | 606.000 | 218.000 | 3 g 5 sa | −%24 | 1.439 |
| 5 | 46 | 919.000 | 331.000 | 4 g 6 sa | −%30 | 1.726 |

<a id="current-muster"></a>
## Akıntıyla Toplanma

`current_muster` · Ralli noktası akıntı yönüne göre seçilir; av rallileri hedefe daha hızlı varır.

**Gerekli:** Av Rallisi 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 35 | 200.000 | 72.000 | 1 g 8 sa | %4 | 820 |
| 2 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | %8 | 1.030 |
| 3 | 41 | 459.000 | 165.000 | 2 g 15 sa | %12 | 1.275 |
| 4 | 44 | 696.000 | 251.000 | 3 g 13 sa 15 dk | %16 | 1.529 |
| 5 | 47 | 1.060.000 | 380.000 | 4 g 14 sa 45 dk | %20 | 1.834 |

<a id="third-hunt-line"></a>
## Üçüncü Av Hattı

`third_hunt_line` · Av Halkası'nın hatlarına üye başına bir yürüyüş daha yerleştirilebilir.

**Gerekli:** Av Ganimeti 3

| Sv | Lab | Titanyum | Biyokütle | Süre | Etki (toplam) | Güç |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 38 | 303.000 | 109.000 | 1 g 21 sa 15 dk | 1 yürüyüş | 1.030 |

← [06 · Araştırma](../06_arastirma.md)
