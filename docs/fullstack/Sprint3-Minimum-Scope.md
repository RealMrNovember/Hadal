# Phase 1 / Sprint 3 — Minimum Uygulanabilir Kapsam

**Karar veren:** Full-stack lead (DHH modu)
**Tarih:** 2026-09-25
**Girdi:** Ekip liderinin önerdiği 13 maddelik kapsam
**Karar:** 13 maddenin **5'i kesildi**, 2'si küçültüldü, 3 yeni (ama küçük) zorunluluk eklendi.

---

## 0. Sprint 3'ün tek cümlelik tanımı

> Stub server bir tick'te üretim yapar, `resource:` StateDelta yollar, HUD replike edilen **miktarı** gösterir.

Bu kadar. "Resource System"in geri kalanı Phase 2'dir.

### Phase 1 Sprint 3 ile Phase 2 arasındaki sınır

| Konu | Sprint 3 | Phase 2 |
|------|----------|---------|
| Üretim tick döngüsü | ✅ stub server, sabit interval | gerçek M5 server |
| HUD | ✅ miktar (authoritative) | rate, kapasite barı, ikon, animasyon |
| Offline | ❌ | ✅ reconnect reconciliation |
| Kapasite / taşma | ❌ (basit clamp) | ✅ overflow kuralları |
| Tüketim / starvation | ⚠️ formülde var, davranış yok | ✅ bina kapatma, alarm |
| Ekonomi grant / GM | ❌ | M7 Admin API |

**Not:** Roadmap Sprint 3 satırındaki "offline reconnect snapshot" için **yeni kod yok** — `StateSyncService.ApplySnapshot` + `ClientStateViewMutator.ApplySnapshot` Phase 0-R'de zaten resource'ları uyguluyor. Gerçek offline reconciliation Phase 2'dir.

---

## 1. YAPILACAK (5 yeni dosya + 5 dokunuş)

### Shared

| # | Dosya | Gerekçe (1 satır) |
|---|-------|-------------------|
| 1 | `shared/HADAL.Shared/Formulas/ProductionFormulas.cs` **(yeni)** | Sprint'in tek gerçek nedeni: üretim formülü server ile client stub arasında tek kaynaktan gelmeli; saf static, primitif parametreli (`long perTick, int tickCount, long current, long cap`). |
| 2 | `shared/HADAL.Shared/Simulation/SimulationTick.cs` **(yeni)** | Tek bir `public const double IntervalSeconds` — stub ile gerçek server'ın tick süresi üzerinde anlaşması için; başka sabit eklemeyin. |
| 3 | `shared/HADAL.Shared/Enums/ResourceType.cs` **(dokunuş)** | Sadece **ekleme**: `Energy = 6, Titanium = 7, Hadalite = 8`. Mevcut değerler asla yeniden numaralanmaz (protobuf wire uyumu). |

### Client

| # | Dosya | Gerekçe (1 satır) |
|---|-------|-------------------|
| 4 | `Assets/_Hadal/Scripts/Core/StateSync/ResourceTypeMap.cs` **(yeni)** | **Mevcut bug'ı kapatıyor:** `Hadal.Data.Enums.ResourceType.Oxygen = 0` ama `HADAL.Shared.Enums.ResourceType.Oxygen = 1` — `ClientStateViewMutator` bugün ham `(int)` cast yapıyor, yani Oxygen miktarı client'ta Energy'ye yazılıyor. |
| 5 | `Assets/_Hadal/Scripts/Core/Network/LocalProductionTickSimulator.cs` **(yeni)** | Sprint'in gövdesi: `LocalPlacementAckSimulator` desenini birebir taklit eden `ITickable` + `IStartable` stub server; cüzdanı `ResourceDatabaseSO.StartingAmount`'tan seed'ler, her `SimulationTick.IntervalSeconds`'ta binaların `ProductionPerTick`/`ConsumptionPerTick`'ini uygulayıp `resource:wallet` `EntityChangeDto` içeren StateDelta kuyruğa basar. |
| 6 | `Assets/_Hadal/Scripts/Core/Network/LocalPlacementAckSimulator.cs` **(dokunuş, ~10 satır)** | Accept edilen placement'ta stub server cüzdanından `BuildCosts` düşülmezse, her üretim tick'i client'ın optimistic harcamasını geri iade eder ve HUD sürekli "rollback" gibi görünür. |
| 7 | `Assets/_Hadal/Scripts/Core/Events/BuildingPlacementEvents.cs` **(dokunuş)** veya yanına `ResourceEvents.cs` | `readonly struct ResourceStateChangedEvent` — 5 satır; HUD'ın polling yapmaması için tek sinyal. |
| 8 | `Assets/_Hadal/Scripts/Managers/StateSync/StateSyncService.cs` **(dokunuş, 1 satır)** | Delta uygulandıktan sonra `ResourceStateChangedEvent` publish et (bugün sadece `BuildingStateChangedEvent` var). |
| 9 | `Assets/_Hadal/Scripts/Managers/ResourceManager.cs` **(dokunuş, ~15 satır net eksi)** | `IGameTickable` + `_tickTimer` **silinecek** (hiçbir şey yapmayan ölü kod); `ResourceStateChangedEvent`'te cüzdanı `ClientStateView.Data.resources`'tan `Set()` ile senkronla. `TrySpend` **aynen kalır** (bkz. §4). |
| 10 | `Assets/_Hadal/Scripts/UI/ResourceHudView.cs` **(yeni)** | `UIViewBase`'den türeyen tek MonoBehaviour: `TMP_Text[]` + `ResourceType[]`, event'te `IResourceService.Get()` okur. Yeni asmdef referansı yok. |
| 11 | `Assets/_Hadal/Scripts/Managers/Bootstrap/ProjectLifetimeScope.cs` **(dokunuş, 2 satır)** | `builder.Register<LocalProductionTickSimulator>(Singleton).AsSelf();` + `RegisterEntryPoint<LocalProductionTickSimulator>();` |
| 12 | `shared/HADAL.Shared.Tests/ProductionFormulasTests.cs` **(yeni, KOŞULLU)** | Saf fonksiyonların testi 30 satır ve tek ucuz test noktamız. **Koşul:** repo'da `dotnet test` çalıştıran bir CI/akış yoksa bu maddeyi de kesin — çalıştırılmayan test, test değildir. |

**Toplam: 5-6 yeni dosya, 5 dosyada ufak dokunuş. Yeni asmdef referansı: SIFIR.**

---

## 2. YAPILMAYACAK (ve neden)

### Ekip liderinin listesinden kesilenler

| Madde | Karar | Gerekçe |
|-------|-------|---------|
| **`ProductionStateDto`** | ❌ **KES** | Sprint 3 HUD'ı **rate göstermiyor**, sadece miktar gösteriyor. Rate olmayınca DTO'nun taşıyacağı veri yok. Server'ın authoritative rate'i (buff/debuff/level çarpanlarıyla) Phase 2'de anlamlı olur. → **Phase 2** |
| **`BuildingProductionRule`** | ❌ **KES** | Üretim verisi `BuildingDefinitionSO.ProductionPerTick`/`ConsumptionPerTick`'te **zaten var** ve stub server aynı SO'yu okuyor. Bunu Shared'a kopyalamak, henüz var olmayan bir server için bina kataloğunu ikinci kez tanımlamak demektir. → **Phase 4 (Buildings) / M5** |
| **`IProductionTickSimulator`** | ❌ **KES** | Tek implementasyon, **sıfır çağıran**. `IPlacementAckSimulator` var çünkü `CommandDispatcher` onu çağırıyor; production simulator kendi kendine dönen bir `ITickable`. Concrete tipi entry point olarak kaydet. Rule of Three. |
| **`EntityIdPrefixes.Production`** | ❌ **KES** | `EntityIdPrefixes.Resource = "resource:"` zaten var ve `ClientStateViewMutator` onu işliyor. İkinci prefix = ikinci kod yolu = ikinci bug. |
| **`ClientStateViewMutator` production desteği** | ❌ **KES — DOSYAYA HİÇ DOKUNMA** | `ApplyResourceChange` → `ResourceStateDto` → `data.resources` yolu **bugün çalışıyor** (satır 62-65, 109-135). Sprint 3'ün inbound tarafı bedava geliyor. Tek istisna: `(int)pair.Key` cast'i `ResourceTypeMap`'e çevrilecek — bu bir bug fix, "production desteği" değil. |
| **asmdef güncellemeleri** | ❌ **KES** | `Hadal.Core` zaten `HADAL.Shared` + `Hadal.Data` görüyor; `Hadal.UI` zaten `Hadal.Core` + `Hadal.Data` + `Hadal.Managers` görüyor. Hiçbir dosya yeni bir sınırı geçmiyor. |
| **`Hadal.UI` → `HADAL.Shared` referansı** | ❌ **KES — MİMARİ OLARAK YANLIŞ** | HUD `IResourceService.Get(Hadal.Data.Enums.ResourceType)` okur. UI katmanının wire protokolünü tanıması, Phase 0-R'nin "NetworkEventBus'ta sıfır Presentation subscriber" kararını delmeye açılan ilk kapıdır. Shared tipleri `Hadal.Core` sınırında durur. |
| **`ResourceManager`'ı authoritative olmaktan çıkarma** (büyük refactor) | ⚠️ **KÜÇÜLT** | Sınıfı parçalamak yok. Sadece: ölü `Tick()` silinir + delta geldiğinde cüzdan view'dan `Set()`'lenir. `TrySpend`/`Add` optimistic prediction olarak kalır (bkz. §4). |
| **`ResourceHudView`'ın "zengin" versiyonu** | ⚠️ **KÜÇÜLT** | İkon, renk, kapasite barı, animasyonlu sayaç, `ResourceDefinitionSO.UiColor` bağlama yok. Düz `TMP_Text`. |

### Sprint 3'e sokulmayacak diğer her şey

| Konu | Nereye |
|------|--------|
| Rate / "+12 per tick" göstergesi | Phase 2 (ve o zaman `ProductionStateDto` ile, client türetmesiyle değil) |
| Offline catch-up, `MaxCatchUpTicks`, `LastTickAt` persistence | **Phase 2** — stub server'ın persistence'ı yok, simüle etmek boşa iş |
| Kapasite taşma kuralları, `MaxCapacity` server-side enforcement | Phase 2 (Sprint 3'te formülde basit `Math.Min` clamp yeter) |
| Consumption yetmediğinde bina kapatma / starvation / alarm | **Phase 3** (Core Survival) |
| Bina seviyesine göre üretim çarpanı, upgrade | **Phase 4** |
| `ResourceChangedEventSO` (SO event channel) rework / kaldırma | Dokunma — kimse dinlemiyor, zararsız |
| `ExpeditionManager.Add(reward)` yolunu server'a taşıma | **Phase 7** |
| Yeni `RemoveBuildingCommand` + üretim durdurma | Phase 1 backlog / Phase 4 |
| Latency simülasyon test harness'ı (Phase 0-R açık kalemi) | Ayrı iş kalemi, bu sprint'e bulaştırmayın |
| Unity `EditMode`/`PlayMode` test assembly'si kurmak | Projede hiç test altyapısı yok; ilk test altyapısı ayrı bir karardır, üretim tick'ine bedava eklenmez |
| `ResourceHudView` için yeni prefab/UI kit | Mevcut `UIManager.ShowView` deseni yeterli |

---

## 3. Soruların net cevapları

**S2 — `ProductionStateDto` gerekli mi? Rate'i client türetebilir mi?**
İkisi de hayır, çünkü **Sprint 3'te rate gösterilmiyor**. Yasağın sınırını yine de yazıya döküyorum, çünkü Phase 2'de sorulacak:

> **Kural:** Client bir kaynak **bakiyesini** asla hesaplamaz — bakiye yalnızca server delta'sından gelir. Client bir **oranı ekranda göstermek için** config'ten türetebilir; bu bir tahmin değil, bir etikettir ve hiçbir zaman `data.resources`'a yazılmaz.
> "Client offline reward calculation" yasağı **zamanla çarpma** işlemini yasaklar (`rate × geçen_süre`), rate'in kendisini yazdırmayı değil.

Sprint 3'te bunu test etmeye bile gerek yok: rate yok.

**S4 — Yeni `ResourceHudView` MonoBehaviour'ı doğru mu?**
Evet, ama `UIViewBase`'den türeyerek ve **mevcut desenle**. `Hadal.UI` → `HADAL.Shared` referansı **haklı değil** — HUD `IResourceService` üzerinden client enum'ıyla çalışır, wire tiplerini hiç görmez.

---

## 4. Sprint 2'yi kırma riski olan noktalar

| # | Risk | Neden riskli | Önlem |
|---|------|--------------|-------|
| **R1** | `ResourceManager.TrySpend`'i kaldırmak | `BuildingManager.TryPlaceBuilding` satır 47 buna bağlı; `false` dönerse **placement komutu hiç gönderilmez** ve Sprint 2 akışı ölür. | **TrySpend'e dokunma.** Optimistic prediction olarak kalsın; authoritative düzeltme bir sonraki `resource:` delta'sında `Set()` ile gelir. |
| **R2** | Stub server placement'ta `BuildCosts` düşmezse | Client optimistic düşer, server delta'sı eski bakiyeyi geri yazar → HUD her tick'te "para geri geldi" gösterir; bu prediction/reconciliation'ın bozuk göründüğü ilk yerdir. | YAPILACAK #6: `LocalPlacementAckSimulator` accept ederken stub cüzdandan `BuildCosts`'u düşsün. |
| **R3** | `ResourceTypeMap` olmadan HUD açmak | Bugünkü ham `(int)` cast'i Shared `Oxygen=1`'i client `Energy=1`'e yazıyor. Sprint 2'de görünür değildi çünkü HUD yoktu; **HUD'u açmak bu bug'ı ortaya çıkarır.** | YAPILACAK #4 sprint'in ilk işi olsun. |
| **R4** | Çift seed (ResourceManager + stub server) | `ResourceManager.SeedStartingResources` sadece `!HasPersistedData` iken çalışır; ilk `resource:` delta'sı `MarkCaptured()` tetikler. İkisi aynı `ResourceDatabaseSO`'dan okuduğu ve senkron `Set()` (Add değil) kullandığı için idempotent. | Stub server seed'ini **aynı** `ResourceDatabaseSO`'dan al; `Add` değil `Set` kullan. |
| **R5** | DI kayıt sırası | `LocalPlacementAckSimulator` artık `LocalProductionTickSimulator`'a bağlı; ikisi de `Singleton` + `EntryPoint`. | `ProjectLifetimeScope`'ta production simulator'ı placement simulator'dan **önce** kaydet; `IStartable.Start` içinde karşılıklı çağrı yapma. |
| **R6** | `ClientStateViewMutator`'a "production" dalı eklemek | `ApplyDelta` Sprint 2'nin `command:` ve `building:` yollarını taşıyor; buraya dördüncü bir dal eklemek ack işleme sırasını bozabilir. | Dosyaya sadece `ResourceTypeMap` cast'i için dokun, yeni dal ekleme. |
| **R7** | `ResourceManager`'dan `IGameTickable`'ı silmek | `TickManager.Register/Unregister` çağrıları `OnPostInject`/`OnShutdown` içinde; `ISaveParticipant` ile karışmasın. | `IGameTickable` ile birlikte ilgili `Register`/`Unregister` satırlarını da sil; `ISaveParticipant`'a dokunma (`StateSyncService.RestoreAll` ona bağlı). |

---

## 5. Süre ve karmaşıklık tahmini

| İş | Süre | Karmaşıklık |
|----|------|-------------|
| `ProductionFormulas` + `SimulationTick` + ResourceType ekleme | 1 sa | Düşük |
| `ResourceTypeMap` + mutator cast düzeltmesi | 0.5 sa | Düşük (ama kritik) |
| `LocalProductionTickSimulator` (seed, tick, üretim/tüketim, delta emit) | 3 sa | **Orta** — sprint'in tek gerçek işi |
| `LocalPlacementAckSimulator` build cost düşümü | 0.5 sa | Düşük |
| `ResourceStateChangedEvent` + `StateSyncService` publish | 0.5 sa | Düşük |
| `ResourceManager` sadeleştirme + view senkronu | 1 sa | Düşük-Orta (R1/R7 dikkat) |
| `ResourceHudView` + sahne/prefab bağlama | 2.5 sa | Düşük (çoğu Editor işi) |
| `ProjectLifetimeScope` kaydı | 0.25 sa | Düşük |
| `ProductionFormulasTests` (koşullu) | 1 sa | Düşük |
| Manuel oynanış doğrulaması (yerleştir → tick → HUD artışı) | 1 sa | — |

**Toplam: ~10-11 saat ≈ 1.5 geliştirici-günü.** Sahne/prefab bağlama payıyla **2 gün** planlayın.

Ekip liderinin orijinal kapsamı (13 madde, 4 yeni Shared tipi, yeni interface, yeni prefix, mutator refactor, asmdef değişiklikleri) tahminen **4-5 gün** — ve bu farkın tamamı henüz var olmayan bir server için yazılan soyutlamalardan geliyor.

---

## 6. Sprint 3 bitti sayılma kriteri

1. Oyun açılır, HUD başlangıç miktarlarını gösterir.
2. Üretim binası yerleştirilir, ack gelir, bina render edilir (Sprint 2 regresyonsuz).
3. HUD'daki miktar `BuildCosts` kadar düşer **ve geri gelmez**.
4. ~N saniyede bir HUD miktarı `ProductionPerTick` kadar artar.
5. `ClientStateView.Data.resources` ile HUD birebir aynıdır; `persistentDataPath`'te hiçbir kaynak verisi yoktur.

Bu 5 madde geçiyorsa sprint biter. Rate, kapasite, offline ve ikon Phase 2'nin işidir.
