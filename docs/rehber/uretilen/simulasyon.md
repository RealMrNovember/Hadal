<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/f2p_profilleri.json -->
# Tempo simülasyonu

Üç oyuncu profilinin Çekirdek Reaktör 50'ye yolculuğu. Model ve varsayımlar: [02 · Tempo ve Denge](../02_tempo_ve_denge.md).

## Özet

| Profil | Reaktör 50 | Hedef (aralık) | R10 | R20 | R30 | R40 | R45 | Süperalaşım beklemesi |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Ücretsiz oyuncu | **368. gün** | 365 (340–390) | 6 sa 51 dk | 4,5. gün | 28,6. gün | 128,4. gün | 224,9. gün | 21,2. gün |
| Aylık kart + VIP 8 | **267. gün** | 265 (235–295) | 3 sa 59 dk | 2,7. gün | 19,9. gün | 84,5. gün | 156,6. gün | — |
| Balina | **147. gün** | 165 (140–190) | 1 sa 19 dk | 1,1. gün | 10. gün | 45,9. gün | 85,8. gün | — |

## Ücretsiz oyuncu

Hiç ödeme yapmayan, günde birkaç kez giren düzenli oyuncu. VIP'i yalnızca günlük giriş ve etkinliklerle yükselir (12 ayda ~VIP 7).

| Sv | Ulaşılan an | Seviye süresi | İnşaatçı | Hız | Önkoşul | Reaktör (etkin) | Hızlandırma | Kaynak bekleme | Süperalaşım bekleme | Boşta |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 2 | — | — | 1 | %3 | — | — | — | — | — | — |
| 3 | — | — | 1 | %4,5 | — | — | — | — | — | — |
| 4 | — | — | 1 | %6 | — | — | — | — | — | — |
| 5 | 4 dk 48 sn | 4 dk 48 sn | 1 | %7,5 | — | — | — | 4 dk 48 sn | — | — |
| 6 | 26 dk 14 sn | 21 dk 26 sn | 2 | %9 | — | — | — | 21 dk 26 sn | — | — |
| 7 | 1 sa 42 dk | 1 sa 16 dk | 2 | %10,5 | — | — | — | 1 sa 16 dk | — | — |
| 8 | 3 sa 1 dk | 1 sa 19 dk | 2 | %12 | — | — | — | 1 sa 19 dk | — | — |
| 9 | 4 sa 42 dk | 1 sa 41 dk | 2 | %13,5 | — | — | — | 1 sa 41 dk | — | — |
| 10 | 6 sa 51 dk | 2 sa 8 dk | 2 | %15 | — | — | 11 dk 17 sn | 2 sa 8 dk | — | — |
| 11 | 9 sa 36 dk | 2 sa 45 dk | 2 | %16,5 | — | 20 dk 8 sn | 5 dk 19 sn | 2 sa 25 dk | — | — |
| 12 | 13 sa 25 dk | 3 sa 48 dk | 2 | %18 | 2 dk 3 sn | 44 dk 15 sn | 6 dk 57 sn | 3 sa 1 dk | — | — |
| 13 | 18 sa 32 dk | 5 sa 6 dk | 2 | %19,5 | 13 dk 32 sn | 1 sa 10 dk | 9 dk 15 sn | 3 sa 21 dk | — | 21 dk 7 sn |
| 14 | 1. gün | 6 sa 20 dk | 2 | %21 | 26 dk 23 sn | 1 sa 43 dk | 12 dk 33 sn | 3 sa 39 dk | — | 31 dk 7 sn |
| 15 | 1,4. gün | 8 sa 14 dk | 2 | %22,5 | 44 dk 37 sn | 2 sa 33 dk | 16 dk 47 sn | 4 sa 10 dk | — | 46 dk |
| 16 | 1,8. gün | 9 sa 45 dk | 2 | %24 | 1 sa 29 dk | 3 sa 14 dk | 21 dk 46 sn | 4 sa 3 dk | — | 58 dk 16 sn |
| 17 | 2,2. gün | 10 sa 54 dk | 2 | %25,5 | 1 sa 59 dk | 3 sa 54 dk | 39 dk 43 sn | 3 sa 50 dk | — | 1 sa 10 dk |
| 18 | 2,8. gün | 14 sa 19 dk | 2 | %27 | 2 sa 34 dk | 4 sa 45 dk | 1 sa 6 dk | 5 sa 33 dk | — | 1 sa 25 dk |
| 19 | 3,6. gün | 17 sa 47 dk | 2 | %28,5 | 3 sa 18 dk | 6 sa 8 dk | 1 sa 20 dk | 6 sa 30 dk | — | 1 sa 50 dk |
| 20 | 4,5. gün | 21 sa 37 dk | 2 | %30 | 4 sa 13 dk | 7 sa 54 dk | 1 sa 36 dk | 7 sa 6 dk | — | 2 sa 22 dk |
| 21 | 6,1. gün | 1 g 14 sa 29 dk | 2 | %31,5 | 12 sa 5 dk | 8 sa 50 dk | 3 sa 6 dk | 14 sa 53 dk | — | 2 sa 39 dk |
| 22 | 7,8. gün | 1 g 18 sa 16 dk | 2 | %33 | 6 sa 49 dk | 11 sa 58 dk | 3 sa 11 dk | 19 sa 53 dk | — | 3 sa 35 dk |
| 23 | 9,4. gün | 1 g 13 sa 43 dk | 2 | %34,5 | 8 sa 40 dk | 16 sa 30 dk | 2 sa 39 dk | 7 sa 35 dk | — | 4 sa 57 dk |
| 24 | 11,3. gün | 1 g 20 sa 9 dk | 2 | %36 | 10 sa 58 dk | 21 sa 2 dk | 3 sa 17 dk | 7 sa 8 dk | — | 5 sa |
| 25 | 13,4. gün | 2 g 3 sa 42 dk | 2 | %37,5 | 14 sa | 1 g 3 sa 2 dk | 3 sa 48 dk | 5 sa 39 dk | — | 5 sa |
| 26 | 15,7. gün | 2 g 7 sa 55 dk | 2 | %39 | 17 sa 32 dk | 1 g 7 sa 17 dk | 4 sa 18 dk | 2 sa 4 dk | — | 5 sa |
| 27 | 18,4. gün | 2 g 15 sa 20 dk | 2 | %40,5 | 20 sa 32 dk | 1 g 12 sa 39 dk | 4 sa 49 dk | 1 sa 8 dk | — | 5 sa |
| 28 | 21,4. gün | 2 g 23 sa 49 dk | 2 | %42 | 23 sa 25 dk | 1 g 18 sa 41 dk | 5 sa 28 dk | 42 dk 34 sn | — | 5 sa |
| 29 | 24,8. gün | 3 g 9 sa 22 dk | 2 | %43,5 | 1 g 2 sa 27 dk | 2 g 1 sa 55 dk | 6 sa 10 dk | — | — | 5 sa |
| 30 | 28,6. gün | 3 g 21 sa 10 dk | 2 | %45 | 1 g 5 sa 52 dk | 2 g 10 sa 18 dk | 7 sa 3 dk | — | — | 5 sa |
| 31 | 35,5. gün | 6 g 21 sa 5 dk | 2 | %46,5 | 2 g 21 sa 6 dk | 2 g 12 sa 58 dk | 13 sa 32 dk | — | 1 g 6 sa | 5 sa |
| 32 | 42,4. gün | 6 g 20 sa 48 dk | 2 | %48 | 1 g 14 sa 6 dk | 3 g 47 dk | 12 sa 44 dk | 6 sa 54 dk | 1 g 18 sa | 5 sa |
| 33 | 50,1. gün | 7 g 17 sa 50 dk | 2 | %49,5 | 1 g 19 sa 1 dk | 3 g 11 sa 49 dk | 14 sa 34 dk | — | 2 g 6 sa | 5 sa |
| 34 | 58,9. gün | 8 g 19 sa 24 dk | 2 | %50 | 2 g 59 dk | 4 g 1 sa 25 dk | 16 sa 29 dk | — | 2 g 12 sa | 5 sa |
| 35 | 68,7. gün | 9 g 19 sa 16 dk | 2 | %50 | 2 g 8 sa 7 dk | 4 g 18 sa 8 dk | 18 sa 12 dk | — | 2 g 12 sa | 5 sa |
| 36 | 79,2. gün | 10 g 10 sa 29 dk | 2 | %50 | 2 g 15 sa 50 dk | 5 g 7 sa 39 dk | 19 sa 44 dk | — | 2 g 6 sa | 5 sa |
| 37 | 90,6. gün | 11 g 10 sa 14 dk | 2 | %50 | 2 g 23 sa 11 dk | 6 g 3 dk | 21 sa 29 dk | — | 2 g 6 sa | 5 sa |
| 38 | 102,7. gün | 12 g 1 sa 50 dk | 2 | %50 | 3 g 7 sa 23 dk | 6 g 19 sa 27 dk | 22 sa 32 dk | — | 1 g 18 sa | 5 sa |
| 39 | 115,3. gün | 12 g 15 sa 13 dk | 2 | %50 | 3 g 16 sa 36 dk | 7 g 17 sa 36 dk | 23 sa 25 dk | — | 1 g | 5 sa |
| 40 | 128,4. gün | 13 g 2 sa 39 dk | 2 | %50 | 4 g 2 sa 58 dk | 8 g 18 sa 40 dk | 1 g 7 dk | — | — | 5 sa |
| 41 | 147,7. gün | 19 g 5 sa 51 dk | 2 | %50 | 9 g 21 sa 36 dk | 9 g 3 sa 14 dk | 1 g 13 sa 46 dk | — | — | 5 sa |
| 42 | 167. gün | 19 g 7 sa 22 dk | 2 | %50 | 5 g 29 dk | 10 g 7 sa 53 dk | 1 g 12 sa 13 dk | — | 3 g 18 sa | 5 sa |
| 43 | 184,5. gün | 17 g 11 sa 33 dk | 2 | %50 | 5 g 12 sa 45 dk | 11 g 17 sa 47 dk | 1 g 8 sa 8 dk | — | — | 5 sa |
| 44 | 203,7. gün | 19 g 5 sa 58 dk | 2 | %50 | 6 g 2 sa 9 dk | 12 g 22 sa 48 dk | 1 g 12 sa 4 dk | — | — | 5 sa |
| 45 | 224,9. gün | 21 g 5 sa 32 dk | 2 | %50 | 6 g 17 sa | 14 g 7 sa 32 dk | 1 g 15 sa 44 dk | — | — | 5 sa |
| 46 | 255,8. gün | 30 g 20 sa 19 dk | 2 | %50 | 16 g 40 dk | 14 g 14 sa 39 dk | 2 g 13 sa 6 dk | — | — | 5 sa |
| 47 | 280,6. gün | 24 g 20 sa 13 dk | 2 | %50 | 7 g 22 sa 11 dk | 16 g 17 sa 2 dk | 1 g 21 sa 29 dk | — | — | 5 sa |
| 48 | 307,5. gün | 26 g 20 sa 8 dk | 2 | %50 | 8 g 13 sa 53 dk | 18 g 1 sa 15 dk | 2 g 2 sa 59 dk | — | — | 5 sa |
| 49 | 336,6. gün | 29 g 1 sa 59 dk | 2 | %50 | 9 g 6 sa 50 dk | 19 g 14 sa 9 dk | 2 g 7 sa 5 dk | — | — | 5 sa |
| 50 | 368,1. gün | 31 g 12 sa 8 dk | 2 | %50 | 10 g 1 sa 13 dk | 21 g 5 sa 55 dk | 2 g 11 sa 41 dk | — | — | 5 sa |

Toplam: hızlandırma 28,8. gün, kaynak beklemesi 4,9. gün, Süperalaşım beklemesi 21,2. gün, boşta 6,5. gün. Kritik yol dışında kalan inşaatçı zamanı: 105. gün.

## Aylık kart + VIP 8

Sendika Sözleşmesi'ni (aylık kart) sürekli alan, ayda ~20–30 $ harcayan oyuncu. 3. inşaatçı açık, VIP 8 hız bonusları.

| Sv | Ulaşılan an | Seviye süresi | İnşaatçı | Hız | Önkoşul | Reaktör (etkin) | Hızlandırma | Kaynak bekleme | Süperalaşım bekleme | Boşta |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 2 | — | — | 2 | %18 | — | — | — | — | — | — |
| 3 | — | — | 2 | %19,5 | — | — | — | — | — | — |
| 4 | — | — | 2 | %21 | — | — | — | — | — | — |
| 5 | 2 dk 48 sn | 2 dk 48 sn | 2 | %22,5 | — | — | — | 2 dk 48 sn | — | — |
| 6 | 15 dk 18 sn | 12 dk 30 sn | 3 | %24 | — | — | — | 12 dk 30 sn | — | — |
| 7 | 59 dk 43 sn | 44 dk 25 sn | 3 | %25,5 | — | — | — | 44 dk 25 sn | — | — |
| 8 | 1 sa 45 dk | 46 dk 7 sn | 3 | %27 | — | — | — | 46 dk 7 sn | — | — |
| 9 | 2 sa 44 dk | 59 dk 1 sn | 3 | %28,5 | — | — | — | 59 dk 1 sn | — | — |
| 10 | 3 sa 59 dk | 1 sa 14 dk | 3 | %30 | — | — | 7 dk 11 sn | 1 sa 14 dk | — | — |
| 11 | 5 sa 24 dk | 1 sa 25 dk | 3 | %31,5 | — | — | 19 dk 28 sn | 1 sa 25 dk | — | — |
| 12 | 7 sa 36 dk | 2 sa 11 dk | 3 | %33 | — | 14 dk 12 sn | 28 dk 9 sn | 1 sa 57 dk | — | — |
| 13 | 10 sa 38 dk | 3 sa 1 dk | 3 | %34,5 | 8 dk 58 sn | 46 dk 17 sn | 21 dk 25 sn | 2 sa 6 dk | — | — |
| 14 | 14 sa 37 dk | 3 sa 59 dk | 3 | %36 | 20 dk 26 sn | 1 sa 12 dk | 29 dk 10 sn | 2 sa 8 dk | — | 18 dk 1 sn |
| 15 | 19 sa 36 dk | 4 sa 59 dk | 3 | %37,5 | 36 dk 45 sn | 1 sa 49 dk | 39 dk 21 sn | 2 sa 5 dk | — | 27 dk 20 sn |
| 16 | 1,1. gün | 5 sa 48 dk | 3 | %39 | 58 dk | 2 sa 17 dk | 51 dk 7 sn | 1 sa 58 dk | — | 34 dk 27 sn |
| 17 | 1,3. gün | 6 sa 29 dk | 3 | %40,5 | 1 sa 39 dk | 2 sa 45 dk | 1 sa 14 dk | 1 sa 22 dk | — | 41 dk 29 sn |
| 18 | 1,7. gün | 8 sa 19 dk | 3 | %42 | 2 sa 9 dk | 3 sa 12 dk | 1 sa 56 dk | 2 sa 8 dk | — | 48 dk 10 sn |
| 19 | 2,1. gün | 10 sa 39 dk | 3 | %43,5 | 2 sa 46 dk | 4 sa 14 dk | 2 sa 20 dk | 2 sa 35 dk | — | 1 sa 3 dk |
| 20 | 2,7. gün | 13 sa | 3 | %45 | 3 sa 34 dk | 5 sa 33 dk | 2 sa 50 dk | 2 sa 28 dk | — | 1 sa 23 dk |
| 21 | 3,5. gün | 21 sa 15 dk | 3 | %46,5 | 10 sa 15 dk | 5 sa 4 dk | 5 sa 27 dk | 4 sa 39 dk | — | 1 sa 16 dk |
| 22 | 4,6. gün | 1 g 1 sa 41 dk | 3 | %48 | 5 sa 46 dk | 7 sa 49 dk | 5 sa 33 dk | 10 sa 7 dk | — | 1 sa 57 dk |
| 23 | 5,6. gün | 1 g 13 dk | 3 | %49,5 | 7 sa 21 dk | 12 sa 17 dk | 4 sa 39 dk | 1 sa 29 dk | — | 3 sa 4 dk |
| 24 | 6,8. gün | 1 g 4 sa 33 dk | 3 | %51 | 9 sa 20 dk | 15 sa 22 dk | 6 sa 10 dk | — | — | 3 sa 50 dk |
| 25 | 8,3. gün | 1 g 11 sa 30 dk | 3 | %52,5 | 11 sa 55 dk | 19 sa 34 dk | 7 sa 47 dk | — | — | 4 sa |
| 26 | 10. gün | 1 g 16 sa 53 dk | 3 | %54 | 14 sa 56 dk | 21 sa 56 dk | 9 sa 37 dk | — | — | 4 sa |
| 27 | 12. gün | 1 g 23 sa 28 dk | 3 | %55,5 | 17 sa 31 dk | 1 g 1 sa 57 dk | 10 sa 51 dk | — | — | 4 sa |
| 28 | 14,3. gün | 2 g 6 sa 35 dk | 3 | %57 | 20 sa 21 dk | 1 g 6 sa 13 dk | 12 sa 34 dk | — | — | 4 sa |
| 29 | 16,9. gün | 2 g 14 sa 39 dk | 3 | %58,5 | 23 sa 4 dk | 1 g 11 sa 34 dk | 14 sa 19 dk | — | — | 4 sa |
| 30 | 19,9. gün | 2 g 23 sa 55 dk | 3 | %60 | 1 g 2 sa 8 dk | 1 g 17 sa 47 dk | 16 sa 25 dk | — | — | 4 sa |
| 31 | 24,2. gün | 4 g 8 sa 40 dk | 3 | %61,5 | 2 g 13 sa 3 dk | 1 g 15 sa 37 dk | 1 g 2 sa 42 dk | — | — | 4 sa |
| 32 | 28,2. gün | 3 g 22 sa 26 dk | 3 | %63 | 1 g 9 sa 29 dk | 2 g 8 sa 56 dk | 19 sa 16 dk | — | — | 4 sa |
| 33 | 32,5. gün | 4 g 8 sa 56 dk | 3 | %64,5 | 1 g 13 sa 54 dk | 2 g 15 sa 2 dk | 1 g 42 dk | — | — | 4 sa |
| 34 | 37,6. gün | 5 g 1 sa 18 dk | 3 | %65 | 1 g 19 sa 14 dk | 3 g 2 sa 3 dk | 1 g 3 sa 34 dk | — | — | 4 sa |
| 35 | 43,4. gün | 5 g 19 sa 47 dk | 3 | %65 | 2 g 1 sa 35 dk | 3 g 14 sa 11 dk | 1 g 7 sa 54 dk | — | — | 4 sa |
| 36 | 49,9. gün | 6 g 11 sa 6 dk | 3 | %65 | 2 g 8 sa 25 dk | 3 g 22 sa 41 dk | 1 g 12 sa 39 dk | — | — | 4 sa |
| 37 | 57,1. gün | 7 g 6 sa 3 dk | 3 | %65 | 2 g 14 sa 57 dk | 4 g 11 sa 5 dk | 1 g 16 sa 24 dk | — | — | 4 sa |
| 38 | 65,2. gün | 8 g 2 sa 40 dk | 3 | %65 | 2 g 22 sa 16 dk | 5 g 23 dk | 1 g 21 sa 20 dk | — | — | 4 sa |
| 39 | 74,3. gün | 9 g 2 sa 2 dk | 3 | %65 | 3 g 6 sa 29 dk | 5 g 15 sa 32 dk | 2 g 2 sa 43 dk | — | — | 4 sa |
| 40 | 84,5. gün | 10 g 4 sa 9 dk | 3 | %65 | 3 g 15 sa 43 dk | 6 g 8 sa 25 dk | 2 g 8 sa 49 dk | — | — | 4 sa |
| 41 | 99,2. gün | 14 g 15 sa 51 dk | 3 | %65 | 8 g 18 sa 59 dk | 5 g 16 sa 51 dk | 3 g 19 sa 51 dk | — | — | 4 sa |
| 42 | 111,7. gün | 12 g 13 sa 41 dk | 3 | %65 | 4 g 10 sa 45 dk | 7 g 22 sa 55 dk | 2 g 13 sa 54 dk | — | — | 4 sa |
| 43 | 125,2. gün | 13 g 10 sa 53 dk | 3 | %65 | 4 g 21 sa 40 dk | 8 g 9 sa 13 dk | 3 g 6 sa 9 dk | — | — | 4 sa |
| 44 | 140,1. gün | 14 g 22 sa 36 dk | 3 | %65 | 5 g 9 sa 36 dk | 9 g 8 sa 59 dk | 3 g 11 sa 42 dk | — | — | 4 sa |
| 45 | 156,6. gün | 16 g 10 sa 56 dk | 3 | %65 | 5 g 22 sa 48 dk | 10 g 8 sa 7 dk | 3 g 20 sa 57 dk | — | — | 4 sa |
| 46 | 180. gün | 23 g 10 sa 55 dk | 3 | %65 | 14 g 5 sa 23 dk | 9 g 1 sa 31 dk | 6 g 4 sa 22 dk | — | — | 4 sa |
| 47 | 199,7. gün | 19 g 15 sa 50 dk | 3 | %65 | 7 g 33 dk | 12 g 11 sa 17 dk | 4 g 1 sa 31 dk | — | — | 4 sa |
| 48 | 220,3. gün | 20 g 15 sa 24 dk | 3 | %65 | 7 g 14 sa 30 dk | 12 g 20 sa 53 dk | 5 g 1 sa 26 dk | — | — | 4 sa |
| 49 | 242,9. gün | 22 g 12 sa 43 dk | 3 | %65 | 8 g 5 sa 34 dk | 14 g 3 sa 9 dk | 5 g 7 sa 37 dk | — | — | 4 sa |
| 50 | 267,2. gün | 24 g 8 sa 51 dk | 3 | %65 | 8 g 21 sa 54 dk | 15 g 6 sa 56 dk | 5 g 19 sa 15 dk | — | — | 4 sa |

Toplam: hızlandırma 62,8. gün, kaynak beklemesi 1,7. gün, Süperalaşım beklemesi —, boşta 5. gün. Kritik yol dışında kalan inşaatçı zamanı: 210,6. gün.

## Balina

Paketleri ve hızlandırmayı düzenli alan yüksek harcamalı oyuncu.

| Sv | Ulaşılan an | Seviye süresi | İnşaatçı | Hız | Önkoşul | Reaktör (etkin) | Hızlandırma | Kaynak bekleme | Süperalaşım bekleme | Boşta |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 2 | — | — | 3 | %33 | — | — | — | — | — | — |
| 3 | — | — | 3 | %34,5 | — | — | — | — | — | — |
| 4 | — | — | 3 | %36 | — | — | — | — | — | — |
| 5 | 56 sn | 56 sn | 3 | %37,5 | — | — | — | 56 sn | — | — |
| 6 | 5 dk 6 sn | 4 dk 10 sn | 3 | %39 | — | — | — | 4 dk 10 sn | — | — |
| 7 | 19 dk 54 sn | 14 dk 48 sn | 3 | %40,5 | — | — | — | 14 dk 48 sn | — | — |
| 8 | 35 dk 17 sn | 15 dk 22 sn | 3 | %42 | — | — | — | 15 dk 22 sn | — | — |
| 9 | 54 dk 57 sn | 19 dk 40 sn | 3 | %43,5 | — | — | — | 19 dk 40 sn | — | — |
| 10 | 1 sa 19 dk | 24 dk 58 sn | 3 | %45 | — | — | 3 dk 41 sn | 24 dk 58 sn | — | — |
| 11 | 1 sa 48 dk | 28 dk 20 sn | 3 | %46,5 | — | — | 14 dk 27 sn | 28 dk 20 sn | — | — |
| 12 | 2 sa 27 dk | 39 dk 7 sn | 3 | %48 | — | — | 35 dk 3 sn | 39 dk 7 sn | — | — |
| 13 | 3 sa 43 dk | 1 sa 16 dk | 3 | %49,5 | 5 dk 5 sn | 27 dk 15 sn | 30 dk 40 sn | 44 dk 9 sn | — | — |
| 14 | 5 sa 18 dk | 1 sa 35 dk | 3 | %51 | 15 dk 26 sn | 55 dk 25 sn | 33 dk 45 sn | 24 dk 13 sn | — | — |
| 15 | 7 sa 29 dk | 2 sa 10 dk | 3 | %52,5 | 30 dk 11 sn | 1 sa 25 dk | 46 dk 29 sn | 2 dk 19 sn | — | 12 dk 45 sn |
| 16 | 9 sa 55 dk | 2 sa 26 dk | 3 | %54 | 49 dk 8 sn | 1 sa 24 dk | 1 sa 22 dk | — | — | 12 dk 43 sn |
| 17 | 12 sa 43 dk | 2 sa 47 dk | 3 | %55,5 | 1 sa 19 dk | 1 sa 16 dk | 2 sa 16 dk | — | — | 11 dk 31 sn |
| 18 | 16 sa 1 dk | 3 sa 18 dk | 3 | %57 | 1 sa 45 dk | 1 sa 20 dk | 3 sa 13 dk | — | — | 12 dk 8 sn |
| 19 | 20 sa 38 dk | 4 sa 36 dk | 3 | %58,5 | 2 sa 15 dk | 2 sa 2 dk | 3 sa 48 dk | — | — | 18 dk 23 sn |
| 20 | 1,1. gün | 5 sa 27 dk | 3 | %60 | 2 sa 55 dk | 2 sa 12 dk | 5 sa 16 dk | — | — | 19 dk 53 sn |
| 21 | 1,4. gün | 8 sa 24 dk | 3 | %61,5 | 8 sa 24 dk | — | 9 sa 23 dk | — | — | — |
| 22 | 1,9. gün | 11 sa 14 dk | 3 | %63 | 4 sa 43 dk | 5 sa 39 dk | 6 sa 16 dk | — | — | 50 dk 55 sn |
| 23 | 2,3. gün | 9 sa 1 dk | 3 | %64,5 | 6 sa 2 dk | 2 sa 35 dk | 12 sa 32 dk | — | — | 23 dk 21 sn |
| 24 | 3. gün | 17 sa 35 dk | 3 | %66 | 7 sa 40 dk | 8 sa 37 dk | 10 sa 39 dk | — | — | 1 sa 17 dk |
| 25 | 3,7. gün | 15 sa 17 dk | 3 | %67,5 | 9 sa 48 dk | 4 sa 45 dk | 19 sa 43 dk | — | — | 42 dk 52 sn |
| 26 | 4,7. gün | 1 g 20 dk | 3 | %69 | 12 sa 17 dk | 10 sa 28 dk | 17 sa 46 dk | — | — | 1 sa 34 dk |
| 27 | 5,6. gün | 21 sa 54 dk | 3 | %70,5 | 14 sa 25 dk | 6 sa 30 dk | 1 g 2 sa 28 dk | — | — | 58 dk 33 sn |
| 28 | 7. gün | 1 g 8 sa 53 dk | 3 | %72 | 16 sa 39 dk | 14 sa 14 dk | 1 g 8 dk | — | — | 2 sa |
| 29 | 8,2. gün | 1 g 5 sa 57 dk | 3 | %73,5 | 19 sa 6 dk | 9 sa 26 dk | 1 g 11 sa 20 dk | — | — | 1 sa 24 dk |
| 30 | 10. gün | 1 g 19 sa 26 dk | 3 | %75 | 21 sa 52 dk | 19 sa 33 dk | 1 g 8 sa 43 dk | — | — | 2 sa |
| 31 | 12,2. gün | 2 g 5 sa 12 dk | 3 | %76,5 | 2 g 5 sa 12 dk | — | 2 g 11 sa 32 dk | — | — | — |
| 32 | 14,5. gün | 2 g 7 sa 14 dk | 3 | %78 | 1 g 4 sa 28 dk | 1 g 45 dk | 1 g 19 sa 42 dk | — | — | 2 sa |
| 33 | 16,8. gün | 2 g 6 sa 8 dk | 3 | %79,5 | 1 g 8 sa 28 dk | 19 sa 40 dk | 2 g 11 sa 13 dk | — | — | 2 sa |
| 34 | 19,8. gün | 2 g 23 sa 43 dk | 3 | %80 | 1 g 13 sa 17 dk | 1 g 8 sa 26 dk | 2 g 10 sa 57 dk | — | — | 2 sa |
| 35 | 22,8. gün | 3 g 1 sa 46 dk | 3 | %80 | 1 g 19 sa | 1 g 4 sa 46 dk | 3 g 5 sa 26 dk | — | — | 2 sa |
| 36 | 26,6. gün | 3 g 17 sa 11 dk | 3 | %80 | 2 g 1 sa 3 dk | 1 g 14 sa 7 dk | 3 g 7 sa 50 dk | — | — | 2 sa |
| 37 | 30,5. gün | 3 g 22 sa 21 dk | 3 | %80 | 2 g 6 sa 56 dk | 1 g 13 sa 24 dk | 3 g 23 sa 4 dk | — | — | 2 sa |
| 38 | 35,1. gün | 4 g 15 sa 26 dk | 3 | %80 | 2 g 13 sa 30 dk | 1 g 23 sa 55 dk | 4 g 4 sa 55 dk | — | — | 2 sa |
| 39 | 40,1. gün | 4 g 23 sa 22 dk | 3 | %80 | 2 g 20 sa 53 dk | 2 g 28 dk | 4 g 22 sa 48 dk | — | — | 2 sa |
| 40 | 45,9. gün | 5 g 19 sa 27 dk | 3 | %80 | 3 g 5 sa 11 dk | 2 g 12 sa 15 dk | 5 g 7 sa 40 dk | — | — | 2 sa |
| 41 | 53,7. gün | 7 g 19 sa 39 dk | 3 | %80 | 7 g 19 sa 39 dk | — | 8 g 13 sa 7 dk | — | — | — |
| 42 | 61,4. gün | 7 g 15 sa 58 dk | 3 | %80 | 3 g 22 sa 8 dk | 3 g 15 sa 49 dk | 5 g 18 sa 55 dk | — | — | 2 sa |
| 43 | 68,2. gün | 6 g 18 sa 44 dk | 3 | %80 | 4 g 7 sa 56 dk | 2 g 8 sa 47 dk | 8 g 1 sa 46 dk | — | — | 2 sa |
| 44 | 77,4. gün | 9 g 4 sa 4 dk | 3 | %80 | 4 g 18 sa 38 dk | 4 g 7 sa 26 dk | 7 g 5 sa 26 dk | — | — | 2 sa |
| 45 | 85,8. gün | 8 g 10 sa 28 dk | 3 | %80 | 5 g 6 sa 29 dk | 3 g 1 sa 59 dk | 9 g 15 sa 55 dk | — | — | 2 sa |
| 46 | 98,5. gün | 12 g 16 sa 10 dk | 3 | %80 | 12 g 16 sa 10 dk | — | 13 g 15 sa 42 dk | — | — | — |
| 47 | 111,2. gün | 12 g 16 sa 55 dk | 3 | %80 | 6 g 5 sa 22 dk | 6 g 9 sa 33 dk | 8 g 9 sa 49 dk | — | — | 2 sa |
| 48 | 120,8. gün | 9 g 15 sa 51 dk | 3 | %80 | 6 g 17 sa 51 dk | 2 g 20 sa | 13 g 5 sa 24 dk | — | — | 2 sa |
| 49 | 135,4. gün | 14 g 14 sa 2 dk | 3 | %80 | 7 g 7 sa 21 dk | 7 g 4 sa 41 dk | 10 g 5 sa 21 dk | — | — | 2 sa |
| 50 | 147,1. gün | 11 g 16 sa 40 dk | 3 | %80 | 7 g 21 sa 58 dk | 3 g 16 sa 42 dk | 15 g 4 sa 39 dk | — | — | 2 sa |

Toplam: hızlandırma 143,1. gün, kaynak beklemesi 3 sa 38 dk, Süperalaşım beklemesi —, boşta 1,9. gün. Kritik yol dışında kalan inşaatçı zamanı: 18,9. gün.
