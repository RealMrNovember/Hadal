# Sprint 3 — Test Sonuçları

**Tarih:** 2026-10-24
**Ortam:** DL380 · CT 160 · headless Unity 6000.0.32f1 (`server/unity/unity.sh`)
**Plan:** [Sprint3-Risk-And-Test-Plan.md](./Sprint3-Risk-And-Test-Plan.md)

## Özet

| Süit | Sonuç | Komut |
|------|-------|-------|
| EditMode | **56/56 geçti** | `unity.sh test EditMode` |
| PlayMode (smoke) | **1/1 geçti** | `unity.sh test PlayMode` |

Sahne ve SO asset'leri batchmode ile üretilir:
`unity.sh exec Hadal.EditorTools.HadalProjectSetup.CreateBootstrapScene`

Bu çalışma, Sprint 3'ten bağımsız olarak projenin **hiç açılamadığını** ortaya çıkardı. Aşağıdaki "Bulunan ve düzeltilen hatalar" tablosu bunları listeler.

## Senaryo kapsamı

| # | Senaryo | Nasıl doğrulandı | Sonuç |
|---|---------|------------------|-------|
| S1 | Enum haritası (Oxygen → Energy) | `ResourceTypeContractTests`, `WalletReplicationTests.OxygenStaysOxygen_EndToEnd` | ✅ Tek enum; değerler sabitlendi; bilinmeyen değer `Unknown`'a düşer |
| S2 | Sıfır bina, sıfır üretim | `WalletReplicationTests.EmptyWallet_*` + PlayMode (6 sn bakiye sabit) | ✅ |
| S3 | Cap dolu | `ProductionFormulaTests.Cap_StopsAtCapacity` | ✅ Cap sunucu formülünde uygulanıyor |
| S4 | Negatif tüketim | `ProductionFormulaTests.NegativeNet_ClampsAtZero`, `ResourceWalletTests.Add_NeverGoesNegative` | ✅ (cüzdan `Add` düzeltildi) |
| S5 | 200 ms içinde iki yerleştirme | `CommandReconciliationTests.TwoPlacementsInFlight_BothResolve` | ✅ (tek bekleyen komut → sözlük, düzeltildi) |
| S6 | Reconnect: snapshot + eski delta | `WalletReplicationTests.SnapshotWins_ThenOlderDeltaIsRejected`, `OlderWalletFrame_IsRejected` | ✅ Kaynak tarafı. ⚠️ Canlı ağ kesintisi denenmedi; `_pending` kuyruğu reconnect'te hâlâ boşaltılmıyor (bina tarafı için açık) |
| S7 | Uzun offline | `ProductionFormulaTests.OfflineGap_IsClampedToTwelveHours`, `ThirtyDaysOfAccrual_DoesNotOverflow` | ✅ 12 sa cap. Kabul edilmiş sapma: stub `DateTime.UtcNow` kullanır (handoff §4) |
| S8 | Gerçek wire üzerinden delta | `ServerMessageRoutingTests` (9 test) | ✅ `ServerMessage` zarfı (schema v2) |
| S9 | Kısmi delta ile kaynak silinmesi | `WalletReplicationTests.WalletFrame_ReplacesWholeWallet`, `ProducerAlwaysSendsFullWallet` | ✅ Tasarım gereği: cüzdan tek varlık, her frame tam cüzdan. Sözleşme testle sabitlendi |
| S10 | Düşük pil tick ölçekleme | Kod incelemesi | ✅ Simülatör VContainer `ITickable`; `TickManager`'a kayıtlı değil. Cihazda (B yolu) denenmedi |
| S11 | Cihaz saati değişimi | Kod + `NegativeElapsedTime_GrantsNothing` | ✅ Geri alma → 0; ileri alma → en fazla 12 sa (kabul edilmiş sapma) |
| S12 | Stub + gateway aynı anda | Kod incelemesi | ✅ Ekonomi stub'ı soket bağlıyken susar. ⚠️ `LocalPlacementAckSimulator` hâlâ koşulsuz kayıtlı |
| S13 | Temiz clone derlemesi | Sunucudaki temiz kopyada derleme | ✅ Bkz. aşağıdaki düzeltmeler |

## Bulunan ve düzeltilen hatalar

| Hata | Etki | Düzeltme |
|------|------|----------|
| `ProjectLifetimeScope`: `Register<…>` + `RegisterEntryPoint<…>` çifti | `CircularGridManager` için Awake'te **VContainerException** — oyun açılmıyordu. Diğerlerinde iki ayrı instance (paket bir `CommandReconciliationSystem`'a girip diğerinde işleniyordu) | Her entry point tek kayıt |
| `*DatabaseSO`, event kanalları vb. 17 sınıf başka sınıfın dosyasında | Unity bu asset'leri `m_Script: 0` ile kaydediyor → yüklenince boş; simülatör "BuildingDatabase missing" | Her sınıf kendi dosyasında |
| `HadalProjectSetup` veritabanı listelerini doldurmuyordu | Hiç bina/başlangıç kaynağı yok → hiçbir şey üretilmez | Listeler + yer tutucu ekonomi değerleri |
| `EnsureFolder` tek seviye | Temiz klonda `Settings/ScriptableObjects` oluşmuyor | Özyinelemeli |
| Sahne HUD'ı yoktu, `autoInjectGameObjects` boştu | HUD hiç enjekte edilmiyor | Setup HUD'ı kurar ve listeye ekler |
| `System.Runtime.CompilerServices.Unsafe` eksik | protobuf-net → HADAL.Shared → tüm Hadal assembly'leri Unity'de **yüklenmiyordu** | NuGet 6.0.0 netstandard2.0 DLL eklendi |
| `ResourceWallet.Add` negatife iniyordu | Negatif bakiye / HUD | 0'a clamp |
| `ResourceWallet.CanAfford` satır satır kontrol | Aynı kaynak iki satırda → bakiye −20 | Kaynak başına toplam |
| `CommandReconciliationSystem` tek bekleyen komut | İkinci yerleştirme ilkinin ack'ini yutuyordu, hayalet asılı kalıyordu | Komut id'sine göre sözlük |

## Açık kalanlar (Sprint 3'ü bloklamaz)

- **Yer tutucu ekonomi değerleri:** başlangıç, kapasite, üretim/tüketim ve maliyetler tasarım dokümanlarında yok; `HadalProjectSetup` içinde `PLACEHOLDER BALANCE` olarak işaretli.
- **Gerçek ağ ile reconnect (S6 canlı)** ve **cihazda düşük pil (S10-B)** denenmedi.
- Reconnect'te `CommandReconciliationSystem._pending` boşaltılmıyor (bina delta'ları için risk).
- `LocalPlacementAckSimulator` gateway bağlıyken de kayıtlı; ekonomi stub'ı gibi bir koşula alınmalı.
- `BaselineTick` uyuşmazlığında snapshot isteme yok (R4).
- Gecikme harness'ı (100/200 ms, Phase 0-R exit kriteri) yazılmadı.
- **Repo `.meta` dosyalarını takip etmiyor.** Bu yüzden üretilen `.asset`/`.unity` dosyaları commit edilemez (script GUID'leri her makinede farklı olur). Sahne ve asset'ler `HadalProjectSetup` ile yeniden üretilir; kalıcı çözüm `.meta` takibine geçmek.
