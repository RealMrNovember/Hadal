# Phase 2 — Dış Erişim, Kimlik ve Dağıtım: Ara Sonuçlar

**Tarih:** 2026-10-25
**Durum:** 🟡 Kod ve dağıtım tamam; tünel/DNS operatör adımı ve telefon testi bekliyor.

## Özet

| Süit | Sonuç |
|------|-------|
| Sunucu (xUnit) | **44/44** — kimlik, token rotasyonu, süresi dolan/sahte jeton, manifest, uçtan uca giriş → el sıkışma |
| Unity EditMode | **71/71** |
| Canlı kabul (`unity.sh acceptance`) | **2/2** — misafir giriş → JWT el sıkışma → yerleştirme → üretim → yeniden bağlanma; gateway yeniden başlatma sonrası yenileme anahtarıyla aynı hesaba dönüş |
| Gecikme (`unity.sh latency`) | **100 ms ✔ · 200 ms ✔** |
| Dağıtım (`deploy.sh`) | 33 sn'de testler → build → yayın → sağlık kontrolü ✔ (geri dönüş dalı henüz canlıda tetiklenmedi) |

## Kimlik akışı

```
İlk açılış:   POST /api/v1/auth/guest  → { accountId, accessToken (JWT, 60 dk), refreshToken }
Sonraki:      POST /api/v1/auth/refresh { refreshToken } → yeni çift (eski anahtar artık geçersiz)
Oyun:         WebSocket → ClientAuthCommand { AccessToken } → gateway JWT'yi doğrular
              → oyuncu kimliği = hesap ID'si (players.identity_key)
Red:          HandshakeAck.Reason = TokenMissing | TokenInvalid | TokenExpired | SchemaIncompatible
```

- Yenileme anahtarları veritabanında yalnızca SHA-256 özeti olarak durur, tek kullanımlıktır (`DELETE … RETURNING`).
- Misafir hesap oluşturma IP başına saatte 20 ile sınırlı (Cloudflare arkasında `CF-Connecting-IP`).
- JWT anahtarı yalnızca CT'deki `.env`'de; 32 karakterden kısaysa sunucu başlamaz.

## Bulunan ve düzeltilen hatalar

| Hata | Düzeltme |
|------|----------|
| ASP.NET Core JSON, Unity uyumlu sözleşmelerin alanlarını (field) yazmıyordu → boş yanıtlar | `IncludeFields = true` |
| Unity 6 `http://` isteklerini engelliyor → testlerde giriş hiç başlamıyordu | `insecureHttpOption = 1`: yalnızca geliştirme build'lerinde HTTP; yayın build'leri yalnızca HTTPS |
| `GameConfig.asset` eski adresi (`hadal.cicibyte.com`) saklıyordu; alan varsayılanı mevcut asset'e uygulanmaz | Setup adresi açıkça yazar |
| `System.Version`: "1.0" < "1.0.0" | Sürümler üç parçaya tamamlanır |

## Operatörün yapması gereken (tek seferlik)

```bash
ssh cicibyte@dl380 'bash -s' < server/deploy/setup-tunnel.sh
```

Alan adı/sertifika işlemi olduğu için otomatik çalıştırılmadı. Script hesap sertifikasını yalnızca tünel oluşturulurken CT'ye koyar ve sonra siler; `hadal.cicibyte.com` (teaser) değişmez.

## Açık kalanlar

- Telefonda internet üzerinden oyun (Android APK: `unity.sh android`, tünel sonrası).
- Hesap bağlama (Google/Apple/e-posta); şimdilik yenileme anahtarı kaybolursa misafir hesap da kaybolur.
- PlayerPrefs düz metin; hesap bağlamayla birlikte Keystore/Keychain'e taşınmalı.
- GitHub Actions CI ve `staging` ortamı.
- Redis hâlâ kullanılmıyor.
