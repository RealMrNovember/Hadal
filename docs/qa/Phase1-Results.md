# Phase 1 — Otoriter Oyun Sunucusu Çekirdeği: Sonuçlar

**Tarih:** 2026-10-24
**Ortam:** DL380 · CT 160 · Docker (gateway + PostgreSQL 16 + Redis) · headless Unity 6000.0.32f1
**Yol haritası:** [ROADMAP.md](../../ROADMAP.md) → Phase 1

## Özet

Unity istemcisi artık varsayılan olarak **gerçek sunucuya** bağlanır. Yerleştirme doğrulaması, kaynak üretimi, offline kazanç ve kalıcılık sunucudadır. İstemci tarafındaki stub sunucu yalnızca açıkça istendiğinde (`HADAL_LOCAL_SIM=1`) çalışır.

| Süit | Sonuç | Komut |
|------|-------|-------|
| Sunucu (xUnit) | **33/33** | `cd server && dotnet test Hadal.Game.Tests` (`HADAL_TEST_PG` ile Postgres testi dahil) |
| Unity EditMode | **60/60** | `unity.sh test EditMode` |
| Unity PlayMode (offline) | **1/1** (+2 canlı test atlanır) | `unity.sh test PlayMode` |
| Canlı kabul | **2/2** | `unity.sh acceptance` |
| Gecikme | **100 ms ✔ · 200 ms ✔** | `unity.sh latency` |

## Canlı kabul akışı (`unity.sh acceptance`)

1. Editör konteyneri `hadal_isolated_net` ağına katılır, `ws://hadal-gateway:8080/gateway` adresine bağlanır.
2. **Yeni oyuncu:** el sıkışma → snapshot (Oxygen 500, Titanium 150, bina yok).
3. `PlaceBuildingCommand(oxygen_plant, R1:S0)` → sunucu doğrular, maliyeti düşer (Titanium 100), binayı oluşturur, ack gönderir.
4. Sunucu tick'i üretimi akıtır; 5 sn replikasyonla oksijen artar.
5. Soket koparılır → istemci fark eder, backoff ile yeniden bağlanır → snapshot → aynı bina, geri sarılmamış cüzdan.
6. `docker restart hadal-gateway` → aynı cihazla yeniden bağlanılır → bina ve cüzdan **PostgreSQL'den** geri gelir.

## Yapılanlar

| Alan | İçerik |
|------|--------|
| Repo | `.meta` takibi, `.gitattributes`, `ProjectSettings/`, URP varsayılanları, üretilmiş sahne ve SO asset'leri repoda |
| Shared | `shared/catalog/catalog.json` (tek katalog), `GameCatalog`, `PlacementRules` (ret nedenleriyle), `GridSlotType`, `EntityIds`, `CommandAckDto.Reason` |
| Sunucu | `Hadal.Game`: `GameWorld`, `PlayerSession` (oyuncu başına kilit), `GameTickService` (1 sn tick, 5 sn replikasyon, periyodik kayıt), offline 12 sa sınırı, saat geri gitme koruması |
| Kalıcılık | `PostgresPlayerRepository` (şema açılışta), `InMemoryPlayerRepository` (testler) |
| Gateway | Tek yazıcılı gönderim kuyruğu (sıra korunur, sınırlı), el sıkışma → `GameWorld`, yeni bağlantı eskisini kapatır, test amaçlı gecikme seçeneği |
| İstemci | Gerçek sunucu varsayılan; `NullPlacementAckSimulator`; otomatik yeniden bağlanma; reconnect'te kuyruk temizliği ve askıda kalan hayaletlerin çözülmesi; `HADAL_GATEWAY_URL` / `HADAL_DEVICE_ID` (yalnızca editör/dev) |
| Editör | Setup kaynak/bina/grid verisini katalogdan üretir; Bootstrap sahnesi build listesinde; `CatalogSyncTests` sapmayı yakalar |

## Alınan kararlar

- **Tek katalog dosyası.** Sunucu kuralları ve istemci SO'ları aynı JSON'dan gelir. Elle SO düzenlemek yerine JSON düzenlenip setup çalıştırılır.
- **Bina kimliğini sunucu üretir** (`b<guid>`); istemcinin komut kimliğine güvenilmez.
- **Kimlik geçici olarak `DeviceId`.** Gerçek kimlik doğrulama Phase 2'de (JWT).
- **Oyun mantığı şimdilik gateway süreci içinde.** Ayrı Game Server süreci yatay ölçekleme gerektiğinde (Phase 17) ayrılır; `Hadal.Game` taşıma katmanından bağımsız yazıldı.
- **Yer tutucu ekonomi değerleri** katalogda kaldı; dengeleme Phase 3/4'te.

## Açık kalanlar (Phase 2+)

- Dış erişim yok: Cloudflare Tunnel, TLS ve `hadal.cicibyte.com` Phase 2'de.
- `DeviceId` kimliği taklit edilebilir; JWT ile değişecek.
- Redis henüz kullanılmıyor (oturum/pub-sub, çoklu düğümde gerekli).
- `NetworkSerializationLayer` hâlâ hem Shared'da hem Core'da (kopya tip).
- Rollback animasyonu gecikme altında çalışıyor; görsel kalite ölçümü Phase 5'te.
