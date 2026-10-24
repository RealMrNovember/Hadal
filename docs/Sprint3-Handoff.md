# Phase 1 Sprint 3 — Handoff

**Tarih:** 2026-09-25
**Durum:** Kod tamamlandı, **doğrulanmadı** (bu oturumda Unity derlemesi, test koşumu, git işlemi YAPILMADI)
**Ekip:** cto-vogels · fullstack-dhh · qa-bach · critic-munger

Detay raporlar: [cto/ADR-001](./cto/ADR-001-Sprint3-Resource-Tick.md) · [fullstack/Sprint3-Minimum-Scope.md](./fullstack/Sprint3-Minimum-Scope.md) · [qa/Sprint3-Risk-And-Test-Plan.md](./qa/Sprint3-Risk-And-Test-Plan.md) · [critic/Sprint3-PreMortem.md](./critic/Sprint3-PreMortem.md)

---

## 1. Veri akışı

```
BuildingDefinitionSO ──┐
ResourceDefinitionSO ──┤  (Unity config, sadece stub okur)
                       ▼
              ProductionCatalog          [HADAL.Shared — engine-free]
                       │
   ClientStateView.buildings ──► ProductionSimulator.Advance()
                                          │  (saf integer matematik)
                                          ▼
                       LocalProductionTickSimulator   [Hadal.Core — SİLİNECEK]
                                          │  StateDelta
                                          ▼
                       ICommandReconciliationSystem.EnqueueInbound()
                                          ▼
                       StateSyncService.ApplyDelta()
                                          ▼
                       ClientStateView  ──► ResourceStateChangedEvent (LocalEventBus)
                                          ├──► ResourceHudView   (okur)
                                          └──► ResourceManager   (ayna, optimistic TrySpend)
```

Sınır kuralı: **gerçek server geldiği gün `HADAL.Shared/Simulation/` altında tek satır değişmemeli.**
Değişiyorsa sınır yanlış çizilmiş demektir.

---

## 2. Alınan kararlar

| # | Karar | Kaynak |
|---|-------|--------|
| 1 | Üretim matematiği `HADAL.Shared/Simulation/` içinde saf statik ve **integer-only** (float server/client sapması yaratır) | CTO |
| 2 | Bakiye replikasyonu **mutlak toplam**, incremental değil — düşen paket 1 tick gecikme, kalıcı sapma değil | CTO + lider |
| 3 | Mutlak yazım "son yazan kazanır"dır → `ResourceStateDto.ServerTick` + `ClientStateView.TryAcceptResourceTick()` stale-frame guard | CTO (P0) |
| 4 | Ekonomi **asla** `TickManager`'dan sürülmez (düşük pilde `_tickScale=0.5` üretimi yarıya indirirdi). Simülatör VContainer `ITickable`'dır, `IGameTickable` değil | QA + CTO |
| 5 | Simulation tick (1 sn, **balance sabiti**) ile replication interval (5 sn, **network sabiti**) ayrıldı | CTO |
| 6 | `Hadal.Data.Enums.ResourceType` **SİLİNDİ**, tek enum HADAL.Shared'da | CTO + Munger |
| 7 | Stub, gerçek gateway socket'i bağlıyken **susar**; ayrıca release build'de başlamayı reddeder | Munger + QA |
| 8 | Build cost'lar stub ekonomide düşülür — aksi hâlde her üretim frame'i binayı iade ederdi | DHH |
| 9 | `ResourceManager` otorite olmaktan çıktı; `ClientStateView`'ın aynası + optimistic `TrySpend` | CTO + Munger |

---

## 3. Ekip tavsiyesine RAĞMEN alınan karar (açık anlaşmazlık)

**`ProductionStateDto` + `production:` entity prefix tutuldu.**
CTO ve full-stack lead ikisi de ertelenmesini önerdi (YAGNI; rate client'ta replike `buildings[]` × statik SO'dan türetilebilir).

Tutulma gerekçesi: Roadmap satırı "Resource HUD from replicated deltas" diyor ve rate göstergesi HUD'un yarısı. Rate'i server'ın göndermesi, client'ı ekonomi aritmetiğinin tamamen dışında tutar — bu, Forbidden #8 karşısında client-side türetmeden **daha** savunulabilir. Maliyet ~40 satır.

**Bu karar geri alınabilir.** Geri alınacaksa silinecekler: `ProductionStateDto.cs`, `EntityIdPrefixes.Production/ProductionEntityId`, `ClientStateView.ApplyProduction/ProductionPerTick`, `ClientStateViewMutator.ApplyProductionChange`, `ResourceHudView` rate satırları, `StateSnapshot.Production`.

---

## 4. Bilinerek kabul edilen sapma

Stub, offline accrual'ı `DateTime.UtcNow`'dan türetiyor. Bu, cihaz saatini ileri alma exploit'ine açıktır ve Forbidden Pattern #8'in sınırındadır. Munger ve CTO ikisi de itiraz etti.

Üç katmanlı sınırlama:
1. 12 saat cap (`SimulationTick.MaxOfflineAccrualTicks`) — cap aşılırsa fazlası **bankalanmaz, yakılır**
2. Gerçek gateway bağlıyken simülatör hiç çalışmaz
3. `#if !UNITY_EDITOR && !DEVELOPMENT_BUILD` → release build'de `LogError` + başlamayı reddeder

Yani exploit yalnızca geliştirici makinesinde mümkün. **Gerçek server geldiğinde bu dosya silinir.**

---

## 5. Sprint 3 dışında bırakılan, DOĞRULANMIŞ bulgular

Bunlar bu sprint'te düzeltilmedi; ayrı iş kalemi olarak açılmalı:

| Bulgu | Kanıt | Önem |
|-------|-------|------|
| `NetworkStateReceiver` önce `TryDecodeHandshakeAck` deniyor; `HandshakeAckEnvelope` ile `StateDeltaEnvelope` wire-type uyumlu → gerçek gateway'de **delta'lar handshake sanılıp yutulabilir** | `NetworkStateReceiver.cs:148` | **Yüksek** — gerçek server günü patlar |
| Reconnect'te `CommandReconciliationSystem._pending` flush edilmiyor; snapshot sonrası eski delta uygulanabilir | `NetworkStateReceiver.cs:103` vs `CommandReconciliationSystem.cs:19` | Orta (kaynak tarafı artık tick guard'ıyla korunuyor, binalar değil) |
| `NetworkSerializationLayer` **duplicate** (HADAL.Shared + `Hadal.Core.Network`) = Forbidden #3 | iki dosya | Orta |
| `StateSyncService` her delta'da koşulsuz grid rebuild yapıyor | `StateSyncService.cs` `SyncGridFromView` | Düşük (perf) |
| Repoda **hiç test projesi yok** (`*.Tests.asmdef`, EditMode/PlayMode → 0 sonuç). 0-R exit kriteri "100ms/200ms latency testi" bugün doğrulanamaz | — | Orta |
| `shared/` Unity ağacının dışında ve `.meta` dosyası yok; temiz clone'da HADAL.Shared derlenmeyebilir | `Assets/_Hadal/Plugins/Shared/` boş | **Doğrulanmalı** |
| `ProjectLifetimeScope`'ta `Register<IFoo,Foo>()` + `RegisterEntryPoint<Foo>()` ikilisi VContainer'da **iki ayrı instance** üretebilir (`CircularGridManager`, `LocalPlacementAckSimulator` bu desende) | `ProjectLifetimeScope.cs` | **Doğrulanmalı** — Sprint 3 bu deseni bilinçli kullanmadı |

---

## 6. İnsanın yapması gerekenler

1. **Unity'de derle.** Hiçbir şey derlenmedi. Özellikle enum konsolidasyonu 7 dosyaya dokundu.
2. **asmdef değişikliklerini doğrula:** `Hadal.Data` → `Hadal.Core` referansı silindi (döngüydü ve ölüydü); `HADAL.Shared` referansı `Hadal.Data`/`Hadal.Editor`/`Hadal.UI`'ya, `VContainer` `Hadal.Core`/`Hadal.UI`'ya eklendi.
3. **`Hadal → Setup → Create Full Project Data`** ile SO asset'lerini üret (repoda hiç `.asset` yok) ve `BuildingDefinitionSO.ProductionPerTick` / `ConsumptionPerTick` / `BuildCosts` alanlarını doldur — **boşsa hiçbir şey üretilmez.**
4. **`ResourceHudView`** sahneye eklenmeli ve `_rows` listesi Inspector'dan bağlanmalı (kod prefab oluşturmuyor).
5. **QA planındaki senaryoları koştur** (`docs/qa/Sprint3-Risk-And-Test-Plan.md`).
6. **Commit edilmedi.** Tüm değişiklikler working tree'de.
7. **Karar ver:** Bölüm 3'teki `ProductionStateDto` anlaşmazlığı ve bölüm 5'teki bulguların önceliklendirmesi.

---

## 7. Açık varsayımlar

| Varsayım | Değer | Nerede değişir |
|----------|-------|----------------|
| Simulation tick | 1 sn | `SimulationTick.IntervalSeconds` — **balance sabiti, değiştirmek tüm rate'leri ölçekler** |
| Replication interval | 5 sn | `SimulationTick.ReplicationIntervalSeconds` — network sabiti, serbest |
| Offline cap | 12 saat | `SimulationTick.MaxOfflineAccrualTicks` |
| Seviye başına üretim bonusu | +%25 additive | `ProductionFormulas.LevelBonusPercentPerLevel` |
| Başlangıç kaynakları | `ResourceDefinitionSO.StartingAmount` | SO asset'leri (henüz yok) |
| Kapasite | `ResourceDefinitionSO.MaxCapacity`; ≤0 = sınırsız | SO asset'leri |

`ResourceType.Alloy(5)` wire uyumluluğu için **rezerve** bırakıldı (`Titanium(7)` ile aynı anlama geliyor). Tasarım dokümanları Titanium diyor; Alloy hiç kullanılmamalı. Canlı client yokken `SchemaVersion`'ı 2'ye çıkarıp Alloy'u tamamen kaldırmak CTO'nun önerisiydi — bu sprint'te yapılmadı.
