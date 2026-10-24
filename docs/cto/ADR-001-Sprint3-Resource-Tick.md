# ADR-001 — Phase 1 Sprint 3: Resource Producer & Simulation Tick

**Durum:** Kabul edildi (değişikliklerle)
**Tarih:** 2026-09-25
**Karar veren:** CTO
**Kapsam:** `Production tick (Shared formulas) · StateDelta for resources` (server) + `Resource HUD from replicated deltas · offline reconnect snapshot` (client) — [HadalDevelopmentRoadmap.md:67](../HadalDevelopmentRoadmap.md)
**Girdi:** Ekip liderinin 5 maddelik Sprint 3 taslağı

---

## 0. Yönetici Özeti

Taslağın **iskeleti doğru**: stub-in-client deseni, absolute resource replikasyonu ve enum bulgusu. Ancak taslak, Sprint 3'ün **gerçek riskini** ıskalıyor: sorun resource üretmek değil, **client tarafında iki ayrı kaynak-otoritesi bulunması** (`ResourceManager._wallet` vs `ClientStateView.Data.resources`) ve **stale delta koruması olmaması**. Bu ikisi çözülmeden üretilen her tick, HUD'da doğrulanamayan bir sayı gösterir.

5 maddenin kararları: **1 = Kabul (değişiklikle)**, **2 = Kabul (guard şartıyla)**, **3 = Ret (ertelendi)**, **4 = Kabul (ayrıştırılarak) / offline accrual client'ta Ret**, **5 = Kabul (çözüm yöntemi değiştirildi — map yerine tek enum)**.

**Ek olarak Sprint 3'e P0 olarak giren, taslakta olmayan 3 madde:** ResourceManager'ın otorite kaybı, `LastAppliedServerTick` monotonic guard, reconnect'te reconciliation kuyruğunun flush edilmemesi.

---

## 1. Teknik Kısıtlar ve Gerçeklik Kontrolü

| # | Kısıt | Kanıt |
|---|-------|-------|
| K1 | Repoda **ASP.NET Core server projesi yok**. "Server" = client içi VContainer entry point. | `ProjectLifetimeScope.cs:65,68` — `IPlacementAckSimulator → LocalPlacementAckSimulator` |
| K2 | `HADAL.Shared` **UnityEngine'e referans veremez** (`noEngineReferences: true`). Formüller zorunlu olarak saf C#. | `shared/HADAL.Shared/HADAL.Shared.asmdef:15` |
| K3 | Gameplay state **diske yazılmıyor** — `ClientStateView` RAM-only. | `ClientStateView.cs:7` · Roadmap exit criteria "No gameplay data in persistentDataPath" |
| K4 | Wire üzerinde JSON yasak; tüm gameplay mesajları protobuf-net. | `HADAL_MASTER_DEVELOPMENT_GUIDE.md:62`, Forbidden #2 |
| K5 | **Client'ta offline reward hesabı yasak.** | `HADAL_MASTER_DEVELOPMENT_GUIDE.md:100`, Forbidden #8 · Roadmap Phase 2: "StateSnapshot — NOT client offline calc" |
| K6 | HADAL.Shared dışında duplicate state type yasak. | `HADAL_MASTER_DEVELOPMENT_GUIDE.md:95`, Forbidden #3 |
| K7 | `resource:` entity prefix ve `ResourceStateDto` pipeline'ı **zaten bağlı** — Sprint 3 sıfırdan başlamıyor. | `EntityIdPrefixes.cs:6` · `ClientStateViewMutator.cs:62,109-135` |
| K8 | Tasarım canonical kaynakları: Oxygen / Energy / Biomass / Titanium / Hadalite. | `06_Resources.md:10-32` · `19_Economy.md:12-16` |

---

## 2. Veri Akışı (komponent kutusu değil — verinin kendisi)

```
                          ┌─────────────── OTORİTE SINIRI (bugün: proses içi, yarın: WebSocket) ──────────────┐
                          │                                                                                   │
 ClientStateView.Data     │   LocalProductionTickSimulator (STUB — silinecek)                                  │
 .buildings[]             │   ┌────────────────────────────────────────────────────────────┐                  │
 (replike, otoriteden)    │   │ t += dt                                                    │                  │
        │                 │   │ if t < SimulationConstants.ProductionTickSeconds: return   │                  │
        │ oku (read-only)─┼──►│ serverTick++                                               │                  │
        │                 │   │                                                            │                  │
        │                 │   │ ProductionFormulas.Accrue(       ◄── HADAL.Shared (KALICI)  │                  │
 BuildingProductionTable  │   │     buildings: BuildingProductionEntry[],                   │                  │
 (SO'dan doldurulan       ├──►│     wallet:    IReadOnlyDictionary<ResourceType,long>,      │                  │
  adapter — silinecek)    │   │     caps:      ResourceCapTable,                            │                  │
                          │   │     ticks:     1)                                           │                  │
                          │   │   → yeni MUTLAK cüzdan                                      │                  │
                          │   └───────────────────────┬────────────────────────────────────┘                  │
                          │                           │                                                        │
                          │                           ▼                                                        │
                          │              ResourceStateDto { Amounts: {ResourceType → long} }   ← MUTLAK        │
                          │                           │  protobuf-net serialize                                 │
                          │                           ▼                                                        │
                          │              EntityChangeDto {                                                     │
                          │                  EntityId = "resource:wallet",                                     │
                          │                  Kind     = Update,                                                │
                          │                  Payload  = <bytes> }                                              │
                          │                           │                                                        │
                          │              StateDelta { ServerTick = N, BaselineTick = N-1, Changes = [..] }     │
                          └───────────────────────────┼────────────────────────────────────────────────────────┘
                                                      │  (bugün: doğrudan EnqueueInbound — LocalPlacementAckSimulator.cs:117 deseni)
                                                      │  (yarın: StateDeltaEnvelope → WebSocket → NetworkStateReceiver.cs:195-220)
                                                      ▼
                                       CommandReconciliationSystem._pending (FIFO)
                                                      │  CommandReconciliationSystem.cs:64-83
                                                      ▼
                                  ┌───────────────────────────────────────┐
                                  │ YENİ GUARD:                           │
                                  │ if delta.ServerTick <= View           │
                                  │      .LastAppliedServerTick → DROP    │  ← stale/duplicate paket burada ölür
                                  └───────────────────┬───────────────────┘
                                                      ▼
                                   StateSyncService.ApplyDelta  (StateSyncService.cs:56-65)
                                                      │
                                                      ▼
                          ClientStateViewMutator.ApplyDelta → ApplyResourceChange → ApplyResources
                                                      │        (ClientStateViewMutator.cs:109-135)
                                                      │   data.resources = dto.Amounts.Select(...)   ← TAM DEĞİŞTİRME
                                                      ▼
                                          ClientStateView.Data.resources[]   ◄── TEK KAYNAK-OTORİTESİ
                                                      │
                          ┌───────────────────────────┴──────────────────────────┐
                          ▼                                                      ▼
            LocalEventBus.Publish(ResourceStateChangedEvent)          IResourceService.Get(type)
                          │   (YENİ — StateSyncService içinden)         (ResourceManager → artık projeksiyon)
                          ▼
                 ResourceHudView (UI katmanı)
                 - mutlak değerler: otoriteden
                 - "+120/dk" etiketi: buildings[] × SO.ProductionPerTick (SADECE GÖRSEL)
```

### Reconnect akışı (ayrı, kısa)

```
socket down → NetworkStateReceiver.HandleDisconnected → _session.Reset()   (NetworkStateReceiver.cs:103-106)
                                                        + YENİ: _reconciliation.FlushPending()
socket up   → handshake → StateSnapshot (otoriteden, offline accrual DAHİL EDİLMİŞ HALDE)
            → ClientStateViewMutator.ApplySnapshot → ApplyResources → data.resources TAM DEĞİŞTİRİLİR
                                                     (ClientStateViewMutator.cs:16-31)
            → View.LastAppliedServerTick = snapshot.ServerTick
            → HUD yeniden çizilir
```

**Dikkat:** Client hiçbir noktada "offline'da ne kazandım" hesabı yapmaz. Snapshot gelir, yazılır, biter.

---

## 3. Kararlar

### Karar 1 — `LocalProductionTickSimulator` stub deseni → **KABUL (3 değişiklikle)**

Desen doğru ve Sprint 2'de kendini kanıtladı: stub client'ta yaşar, formül Shared'da yaşar, server geldiğinde stub silinir. Bu, tek geliştirici için doğru "monolith first" hamlesi — gerçek bir server prosesi ayağa kaldırmak Sprint 3'ün öğrenmek istediği şeyi (üretim → delta → HUD döngüsü) öğretmez, sadece operasyon yükü ekler.

**Değişiklik 1.1 — Stub, `BuildingDefinitionSO` okumayacak.**
`BuildingDefinitionSO.ProductionPerTick` (`BuildingDefinitionSO.cs:22,39`) bir `ScriptableObject` üzerinde `List<Hadal.Data.Models.ResourceAmount>` tutuyor. Gerçek server ne `ScriptableObject` ne `UnityEngine` görebilir (K2). Eğer stub'ı SO'ya bağlarsak, server geldiğinde formülün **girdi tarafını** baştan yazarız ve "formül kalır" vaadi çöker.

Zorunlu ara katman:

```
shared/HADAL.Shared/DTOs/BuildingProductionEntry.cs   (KALICI)
    DefinitionId : string
    Level        : int
    Production   : Dictionary<ResourceType, long>   // per tick
    Consumption  : Dictionary<ResourceType, long>   // per tick

Assets/_Hadal/Scripts/Core/Network/StubProductionTableAdapter.cs   (SİLİNECEK)
    BuildingDatabaseSO → BuildingProductionEntry[]
```

Server geldiğinde `StubProductionTableAdapter` silinir, yerine PostgreSQL okuması gelir; `ProductionFormulas` satırı değişmez. Migration sınırı buradan geçer.

**Değişiklik 1.2 — Tick sayacı `Time.unscaledTime` değil, monoton `ulong` olacak.**
`LocalPlacementAckSimulator.cs:84` bugün `ServerTick = 1` sabiti yolluyor ve `BaselineTick = 0`. Yani şu an sistemde **tick sıralaması diye bir şey yok**. Sprint 3 absolute replikasyona dayanacaksa ve stale-drop guard'ı ServerTick'e bakacaksa, tek bir monoton sayaç şart:

```
Assets/_Hadal/Scripts/Core/Network/StubServerClock.cs   (SİLİNECEK)
    ulong CurrentTick { get; }
    ulong Advance();
```

Hem `LocalPlacementAckSimulator` hem `LocalProductionTickSimulator` bunu paylaşır. Aksi halde placement delta'sı (tick=1) ile production delta'sı (tick=N) yarışır ve guard placement ack'lerini düşürür. **Bu, Sprint 3'ün en kolay gözden kaçan kırılma noktasıdır.**

**Değişiklik 1.3 — Stub `ITickable` içinde exception atarsa davranış tanımsız.**
`ProjectLifetimeScope.Configure` (`ProjectLifetimeScope.cs:37-102`) içinde `RegisterEntryPointExceptionHandler` **kaydı yok**. Sprint 3'te eklenecek: handler, exception'ı loglayıp yutmalı. "Everything fails, all the time" — bir üretim tick'i hatası, placement ack'lerini veya ağ dispatch'ini öldürmemeli.

---

### Karar 2 — Mutlak (absolute) resource replikasyonu → **KABUL, ancak monotonic guard şartıyla**

Gerekçe taslakta doğru verilmiş ve kodla destekleniyor: `ClientStateViewMutator.ApplyResources` (`ClientStateViewMutator.cs:123-135`) `data.resources` dizisini **tamamen değiştiriyor** — semantik zaten mutlak. Incremental'a geçmek mevcut mutator'ı yeniden yazmak demektir ve hiçbir şey kazandırmaz.

Mutlak replikasyonun asıl kazancı boyut değil, **hata modelidir**: kaybolan bir paketin maliyeti bir tick'lik gecikmedir, kalıcı sapma değil. Incremental'da tek kayıp paket = oyuncunun cüzdanı sonsuza kadar yanlış.

**Ancak taslağın "idempotent" iddiası bugün YANLIŞ.** Mutlak yazma idempotent değildir — *son yazan kazanır* semantiğidir. Sıra bozulursa (geç gelen eski paket) HUD geri sayar. Kodda bu koruma yok: `ClientStateViewMutator.ApplyDelta` (`ClientStateViewMutator.cs:33-75`) `delta.ServerTick`'e hiç bakmıyor.

**Zorunlu ek (P0):**

```
ClientStateView:
    ulong LastAppliedServerTick { get; private set; }
    bool TryAdvanceTick(ulong serverTick)   // serverTick <= Last → false
    ResetSession() → LastAppliedServerTick = 0
```
`StateSyncService.ApplyDelta` bu kontrolü yapmadan mutator'a girmeyecek. Snapshot ise koşulsuz kazanır ve sayacı kendi tick'ine set eder.

**Entity granülaritesi:** Tek entity — `resource:wallet`. Kaynak başına ayrı entity'ye BÖLME. `ResourceStateDto.Amounts` zaten bir map (`ResourceStateDto.cs:10`); bölmek delta boyutunu anlamlı düşürmez (6 kaynak × ~10 byte), ama kısmi-güncelleme semantiği doğurur ve mutator'ın "tümünü değiştir" mantığını bozar.

**Emit politikası:** Her tick delta gönderme. *Değişiklik varsa gönder; değişiklik yoksa en fazla 30 saniyede bir heartbeat delta gönder.* Bu bir formül değil, emitter policy'sidir — stub'ta yaşar, server'a birebir taşınır.

---

### Karar 3 — Ayrı `ProductionStateDto` + `production:` prefix → **RET (Phase 3'e ertelendi)**

Taslaktaki tek gereksiz genişleme bu. Reddetme gerekçeleri:

1. **API bir kontrattır.** Yeni bir top-level DTO + yeni entity prefix + `ClientStateViewMutator`'da yeni dal + `SaveGameData`'da yeni dizi = yayınlandığı anda uzun vadeli bakım borcu. Sprint 3'ün doğrulaması gereken şey (roadmap satırı: *"Resource HUD from replicated deltas"*) bunu gerektirmiyor.
2. **Veri zaten client'ta.** Rate göstermek için gereken iki girdi de mevcut: replike `data.buildings[]` (otoriteden gelir) ve `BuildingDefinitionSO.ProductionPerTick` (statik config, Addressables). HUD `Σ(buildings × rate)` hesaplayabilir.
3. **Bu, Forbidden #8 ihlali DEĞİLDİR** — çünkü tek bir kaynak birimi yaratmıyor, sadece bir etiket çiziyor. Otoriter sayı her zaman `data.resources`'tan gelir. Sınır nettir ve kod incelemesinde denetlenebilir: *hesaplanan rate asla `data.resources`'a yazılmaz.*
4. **Ayrışma riski bugün sıfır.** Buff/debuff, oksijen kesintisi, basınç cezası sistemleri yok. Bunlar Phase 3'te (Core Survival) geldiğinde client'ın hesabı server'dan ayrışır — **o zaman** rate replikasyonu gerekli olur.

**Phase 3'te doğru çözüm (şimdi değil):** yeni DTO değil, `ResourceStateDto`'ya alan ekleme:
```csharp
[ProtoMember(2)] public Dictionary<ResourceType, long> RatePerMinute { get; set; } = new();
```
protobuf-net'te yeni alan geriye uyumludur; yeni top-level entity type ise yeni bir kod yolu, yeni bir hata yüzeyi ve yeni bir test matrisidir. **Tek DTO, tek entity, tek kod yolu.**

**Sprint 3'te HUD rate etiketi ŞART koşulursa:** yukarıdaki `[ProtoMember(2)]` alanını şimdi ekleyin, ayrı DTO/prefix yine yok. Kararın özü DTO sayısında, replikasyonun kendisinde değil.

---

### Karar 4 — 5 saniye tick + offline accrual → **AYRIŞTIRILARAK KABUL; client-side offline accrual RET**

**4a — Tick interval: iki ayrı kavram karıştırılmış.** Taslak "5 saniye simülasyon tick'i" diyor. Bunlar iki farklı karardır:

| Kavram | Değer | Nerede yaşar |
|--------|-------|--------------|
| **Simulation tick** — otoritenin hesap yaptığı periyot | 1 sn | `HADAL.Shared/Simulation/SimulationConstants.cs` |
| **Replication/emit tick** — client'a delta çıkış periyodu | 5 sn (veya değişimde) | Emitter policy (stub → server) |

Ayırmanın nedeni: replication periyodunu ileride bant genişliği için değiştirdiğinizde **ekonomi dengesi kaymamalı**. Birleşik tutarsanız, "delta sıklığını 10 sn yapalım" kararı sessizce üretim hızını yarıya indirir. Bu, canlıya çıktıktan sonra düzeltilemez bir tasarım hatasıdır.

**4b — Tick sabiti `GameConfigSO`'da DEĞİL, `HADAL.Shared`'da olacak.**
`GameConfigSO._baseTickIntervalSeconds` (`GameConfigSO.cs:15`, default `1f`) bir client asset'idir. Ekonomi sabitini client asset'inden okumak, gerçek server geldiğinde doğrudan bir cheat vektörüdür (asset bundle patch = üretim hızı çarpanı). Ayrıca `TickManager` `_tickScale`'i pil durumuna göre değiştiriyor (`TickManager.cs:46-59`) — **düşük pilde oyuncunun ekonomisi yavaşlamamalı.** Üretim simülasyonu `TickManager` üzerinden ASLA sürülmeyecek; stub kendi `ITickable`'ında `Time.unscaledDeltaTime` ile ilerler.

**4c — Offline accrual: client'ta hesaplanmayacak. Nokta.**
Forbidden #8 (`HADAL_MASTER_DEVELOPMENT_GUIDE.md:100`) ve Roadmap Phase 2 ("StateSnapshot — NOT client offline calc") bu konuda pazarlığa açık değil.

Sprint 3'ün client tarafı sadece şunu yapar: reconnect → `StateSnapshot` al → `ApplySnapshot` → `data.resources` tam değiştir → HUD yenile. Bu yol **zaten kodda mevcut** (`ClientStateViewMutator.cs:16-31`, `NetworkStateReceiver.cs:222-246`).

Stub, otorite rolünü oynadığı için wall-clock farkını hesaplayabilir — ama bu kod **`Hadal.Core`'da değil, `HADAL.Shared/Simulation/OfflineAccrualFormula.cs`'de** yaşayacak ve **yalnızca stub'tan** çağrılacak. Hiçbir prod client kod yolu bu formülü çağırmayacak. Test edilebilir sınır: `OfflineAccrualFormula`'ya yapılan referansların tamamı silinecek dosya listesinde (§5) olmalı.

**Cap zorunlu, iki katmanlı:**
- `ResourceCapTable` (Shared) — kaynak başına `MaxCapacity`. Bugün bu değer `ResourceDefinitionSO.MaxCapacity`'de (`ResourceDefinitionSO.cs:25`) yani client asset'inde; server tarafına taşınacak, stub adapter'dan doldurulacak.
- `OfflineAccrualMaxHours` (Shared, öneri: 8 saat) — 3 ay dönen oyuncunun hazineyi patlatmaması için.

---

### Karar 5 — İkili `ResourceType` enum'ı → **BULGU DOĞRULANDI (P0 blocker). Çözüm yöntemi DEĞİŞTİRİLDİ: map değil, tek enum.**

**Bulgu teyidi (satır satır):**

| Enum | Değerler | Dosya |
|------|----------|-------|
| `HADAL.Shared.Enums.ResourceType` | Unknown=0, Oxygen=1, Food=2, Biomass=3, Research=4, Alloy=5 | `shared/HADAL.Shared/Enums/ResourceType.cs:8-13` |
| `Hadal.Data.Enums.ResourceType` | Oxygen=0, Energy=1, Food=2, Biomass=3, Titanium=4, Hadalite=5 | `Assets/_Hadal/Scripts/Data/Enums/GameEnums.cs:3-11` |

**Bozulma mekanizması** — `ResourceSaveEntry.resourceType` tipsiz bir `int` (`SaveGameData.cs:39`) ve iki farklı yazar var:

| Yazar/Okuyan | Hangi enum | Satır |
|---|---|---|
| `ResourceManager.CaptureSave` → `(int)pair.Key` | `Hadal.Data` | `ResourceManager.cs:98` |
| `ResourceManager.RestoreSave` → `(ResourceType)entry.resourceType` | `Hadal.Data` | `ResourceManager.cs:113` |
| `ClientStateViewMutator.ApplyResources` → `(int)pair.Key` | `HADAL.Shared` | `ClientStateViewMutator.cs:131` |

Sonuç tablosu:

| Shared değeri | int | `Hadal.Data` okuması | Sonuç |
|---|---|---|---|
| Oxygen | 1 | **Energy** | ❌ sessiz bozulma |
| Food | 2 | Food | ✅ tesadüfen doğru |
| Biomass | 3 | Biomass | ✅ tesadüfen doğru |
| Research | 4 | **Titanium** | ❌ sessiz bozulma |
| Alloy | 5 | **Hadalite** | ❌ premium kaynak bozulması |

**Bu, hata sınıflarının en kötüsüdür: kısmen doğru çalışıyor.** Food ve Biomass doğru geldiği için sistem "çalışıyor" görünür; Oxygen (üs ölüm koşulu, `06_Resources.md:10-14`) ve Hadalite (premium para birimi, `19_Economy.md:20-32`) sessizce bozulur. Hadalite bozulması = para birimi bozulması = geri döndürülemez müşteri destek olayı.

Ayrıca bu doğrudan **Forbidden #3** ihlalidir (`HADAL_MASTER_DEVELOPMENT_GUIDE.md:95`) ve Roadmap'in "Zero duplicate types outside HADAL.Shared" exit criteria'sının (✅ işaretli) **yanlış işaretlenmiş** olduğunu gösterir.

**Taslağın önerisi (`ResourceTypeMap` çevirici) → RET.** Gerekçe:
- Çevirici, iki enum'ı kalıcı hale getirir. Her yeni kaynakta iki dosya düzenlenir; biri unutulduğunda hata *aynı sessiz sınıfta* geri döner.
- Çevirici, `int` alanın anlamını dosya sınırına göre değişken bırakır. Bir `int`'in anlamı onu kimin okuduğuna bağlıysa, bu tip değil kurallara-dayalı-umuttur.
- Çevirici, migration'ı ertelemez — sadece pahalılaştırır. Bugün 9 dosya `ResourceType`'a dokunuyor (`ResourceManager`, `HadalProjectSetup`, `GameServiceContracts`, `GameEventChannelsSO`, `BuildingDefinitionSO`, `GameModels`, `GameEnums`, `DepthZoneDefinitionSO`, `ResourceDefinitionSO`). Phase 4'te bu sayı katlanır.

**KARAR — Tek enum:**

1. `Hadal.Data.Enums.ResourceType` **silinir** (`GameEnums.cs:3-11`). Tüm client kodu `HADAL.Shared.Enums.ResourceType` kullanır.
2. `HADAL.Shared.Enums.ResourceType` genişletilir; **mevcut wire değerleri korunur**:
   ```
   Unknown = 0, Oxygen = 1, Food = 2, Biomass = 3,
   Research = 4,   // [Obsolete-for-design] wire slot rezerve — YENİDEN KULLANMA
   Alloy = 5,      // [Obsolete-for-design] wire slot rezerve — YENİDEN KULLANMA
   Energy = 6, Titanium = 7, Hadalite = 8
   ```
   4 ve 5 asla silinmez ve asla yeniden atanmaz — protobuf enum slotları kalıcıdır.
3. `Hadal.Data.asmdef` referanslarına `HADAL.Shared` + `ProtobufNet` eklenir (bugün yalnız `Hadal.Core` var — `Hadal.Data.asmdef:4-6`).
4. `ResourceSaveEntry.resourceType`'ın canonical anlamı = `HADAL.Shared.Enums.ResourceType` değeri. **Taslağın bu maddesi kabul** — ama tek enum kaldığında zaten tartışma kalmaz.
5. `SchemaVersion.Current` 1 → 2, **`MinSupported` de 2** (`SchemaVersion.cs:5-6`). Canlı client yok; bu kırılmayı yapmanın maliyeti bugün **sıfır**, yayından sonra imkânsız.
6. `SaveGameData.CurrentVersion` 2 → 3 (`SaveGameData.cs:9`). Disk migration'ı gerekmez (K3 — gameplay diske yazılmıyor), ama RAM cache'in eski/yeni ayrımı için sürüm damgası tutulur.

**Maliyet:** ~9 dosyada mekanik değişiklik, derleyici destekli (enum silinince her kullanım derleme hatası verir — sessiz kalma ihtimali yok). Yarım gün.

**Fallback (yalnızca 5. madde Sprint 3'ü kilitlerse):** Tek yönlü `ResourceTypeMap` geçici olarak kabul edilir — **ancak** (a) tek dosyada, (b) ters yön yok, (c) `Sprint 3 exit criteria`'ya "enum birleştirme tamamlandı" maddesi zorunlu olarak eklenir, (d) Sprint 4'ten sonraya sarkamaz.

---

## 4. "Bu bileşen çökerse ne olur?"

### 4.1 — Stub tick çökerse (`LocalProductionTickSimulator.Tick` exception atar)

| Konu | Durum |
|---|---|
| **Blast radius** | Bugün **tanımsız ve muhtemelen geniş**: `ProjectLifetimeScope.cs:37-102` içinde `RegisterEntryPointExceptionHandler` yok. Aynı player-loop noktasında `CommandReconciliationSystem`, `NetworkStateReceiver`, `NativeWebSocketClient` de entry point olarak kayıtlı (`ProjectLifetimeScope.cs:58,66-68`). |
| **Belirti** | Kaynaklar donar; bina yerleştirme de sessizce ölebilir; oyuncu için "oyun bozuldu" ama hata mesajı yok. |
| **Azaltma (P0)** | `builder.RegisterEntryPointExceptionHandler(ex => Debug.LogException(ex))` ekle. Ayrıca stub'ın `Tick()` gövdesi kendi içinde try/catch'lenip, N ardışık hata sonrası kendini devre dışı bırakıp bir `SimulationStalledEvent` yayınlamalı (circuit breaker). Üretim durur ama **HUD "senkronizasyon yok" gösterir** — sessiz donma yerine görünür arıza. |
| **Gerçek server sonrası** | Blast radius doğal olarak küçülür: server tick'i çökerse client sadece delta almaz; `NetworkStateReceiver.EvaluateConnectionHealth` (`NetworkStateReceiver.cs:122-141`) zaten `NetworkReconnectingEvent` yayınlıyor. |

### 4.2 — Delta kaybolursa

| Konu | Durum |
|---|---|
| **Mutlak replikasyonla** | Maksimum sapma = 1 replication periyodu (5 sn). Bir sonraki delta durumu tam olarak düzeltir. **Kendiliğinden iyileşir** — bu, Karar 2'nin tek ve yeterli gerekçesidir. |
| **Incremental olsaydı** | Kalıcı sapma. Düzeltmek için ya sequence-ack-retransmit (TCP'yi yeniden icat etmek) ya periyodik tam senkron (zaten mutlak replikasyon) gerekirdi. |
| **Kalan açık** | Peş peşe çok sayıda kayıpta client eski değeri gösterir ve **bunu bilmez**. Azaltma: `ClientStateView.LastAppliedServerTick` + duvar saati; 3 replication periyodu (15 sn) delta gelmezse HUD'u "stale" olarak işaretle (soluk/gri). Ucuz, dürüst, ve gerçek server'da aynen çalışır. |

### 4.3 — Reconnect'te çift sayım

**Mutlak replikasyon + snapshot-tümünü-değiştir ile çift sayım yapısal olarak imkânsızdır** — toplama yapan hiçbir kod yolu yok. Ama iki gerçek delik var:

**Delik A — reconnect'te reconciliation kuyruğu flush edilmiyor.** `NetworkStateReceiver.HandleDisconnected` (`NetworkStateReceiver.cs:103-106`) ve `EvaluateConnectionHealth` (`:138`) `_session.Reset()` çağırıyor, ama `CommandReconciliationSystem._pending` (`CommandReconciliationSystem.cs:19`) dokunulmadan kalıyor. Kuyruk FIFO ve snapshot ile delta'yı ayırmıyor (`CommandReconciliationSystem.cs:64-83`). Senaryo: bağlantı kopar → kuyrukta eski delta'lar kalır → yeniden bağlanılır → snapshot gelir → snapshot uygulanır → **ardından eski delta'lar uygulanır ve cüzdanı geriye götürür.** Çift sayım değil ama aynı derecede kötü: hayalet gerileme.
> **Azaltma (P0):** `ICommandReconciliationSystem.FlushPending()` ekle, `HandleDisconnected` içinden çağır. **+** `LastAppliedServerTick` guard'ı (Karar 2) bunu ikinci savunma hattı olarak kalıcı kapatır.

**Delik B — `ResourceManager._wallet` ikinci bir otorite.** Aşağıda §4.4.

### 4.4 — Taslakta olmayan P0: `ResourceManager` ikinci kaynak-otoritesi

Bu, Sprint 3'ün **en büyük mimari riski** ve taslakta hiç geçmiyor.

| Kanıt | Sorun |
|---|---|
| `ResourceManager.cs:20` — `private readonly ResourceWallet _wallet = new();` | `ClientStateView.Data.resources`'tan **bağımsız** ikinci bir kaynak deposu. |
| `ResourceManager.cs:58-67` — `SeedStartingResources()` | Client **kendi kendine kaynak yaratıyor**. Server-authoritative modelde yasak. |
| `ResourceManager.cs:74-84` — `Add(type, amount)` | Client tarafında yazma yolu; cap'i `ResourceDefinitionSO`'dan (client asset) okuyor. |
| `ResourceManager.cs:49-56` — `Tick()` | Timer'ı sıfırlayan, başka hiçbir şey yapmayan **ölü kod**. |
| `ResourceManager.cs:113` — `RestoreSave` | Yanlış enum ile okuma (bkz. Karar 5). |

HUD hangi sayıyı gösterirse göstersin, sistemde **iki cevap** var ve hangisinin kazandığı çağrı sırasına bağlı. Bu, `IResourceService` üzerinden (`GameServiceContracts.cs:11-18`) tüm oyuna dağılır.

**Karar (P0, Sprint 3 kapsamında):** `ResourceManager` **yazma otoritesini kaybeder ve saf projeksiyona indirgenir**.
- `_wallet` kaldırılır veya her `ApplyResources` sonrası `ClientStateView.Data.resources`'tan yeniden doldurulur (tek yön: view → wallet).
- `SeedStartingResources()` silinir — başlangıç kaynakları otoriteden ilk `StateSnapshot` ile gelir.
- `Add()` ve `TrySpend()` `IResourceService`'ten çıkarılır veya `[Obsolete]` işaretlenip yalnızca *"yeterli mi?"* sorusuna cevap veren `CanAfford` bırakılır. Harcama tek yoldan olur: command → server → delta.
- `ISaveParticipant.CaptureSave` implementasyonu kaldırılır — client artık kaynak yazmıyor.

**Bu yapılmazsa Sprint 3'ün tüm çıktısı doğrulanamaz:** HUD'daki sayı ile otoritenin sayısı arasındaki farkı ölçebilecek tek bir doğru nokta olmaz.

### 4.5 — Taslakta olmayan bulgu: resource delta'sı her seferinde grid'i yeniden senkronize ediyor

`StateSyncService.ApplyDelta` (`StateSyncService.cs:56-65`) **her** delta'dan sonra koşulsuz `SyncGridFromView()` çağırıyor; bu da `_gridManager.SyncOccupancyFromBuildings(...)` + `_localBus.Publish(new BuildingStateChangedEvent())` yapıyor (`StateSyncService.cs:67-71`).

Sprint 3'ten sonra 5 saniyede bir resource-only delta gelecek → **5 saniyede bir tüm grid occupancy yeniden kurulacak ve tüm bina görsel dinleyicileri tetiklenecek.** Mobilde bu GC baskısı ve potansiyel görsel pop demektir.

**Azaltma:** `ApplyDelta`, `delta.Changes` içinde `EntityIdPrefixes.Building` ile başlayan bir değişiklik var mı diye baksın; yoksa grid senkronunu ve event'i atlasın. ~5 satır.

### 4.6 — Sprint 3'ün doğrudan parçası olmayan, ama kayda geçirilen 2 mimari bulgu

**Bulgu I — `NetworkSerializationLayer` duplicate (Forbidden #3 ihlali).**
İki ayrı sınıf aynı isimle: `shared/HADAL.Shared/Serialization/NetworkSerializationLayer.cs:7` ve `Assets/_Hadal/Scripts/Core/Network/NetworkSerializationLayer.cs:9`. Client kodu (örn. `CommandReconciliationSystem.cs:21`, `using HADAL.Shared.Serialization` **olmadan**) client kopyasını kullanıyor. Shared kopyasında `TryDeserialize` **yok** — yani server yazıldığında hata toleranslı deserialize yolu server tarafında mevcut olmayacak. Sprint 3'ü bloke etmez; gerçek server yazılmadan önce mutlaka birleştirilmeli.

**Bulgu II — asmdef çevrimsel referans şüphesi.**
`Hadal.Core.asmdef:4-10` → `Hadal.Data` referansı; `Hadal.Data.asmdef:4-6` → `Hadal.Core` referansı. Unity çevrimsel assembly referanslarını reddeder. Karar 5'in uygulanması sırasında (Hadal.Data'ya `HADAL.Shared` eklenirken) Editor'de doğrulanmalı — bu iki asmdef'in bugünkü hâliyle derlendiği teyit edilmeli. Eğer çevrim gerçekse, `ResourceType`'ı Shared'a taşımak bu düğümü de çözer (Data'nın Core'a ihtiyacı azalır).

---

## 5. Migration Sınırı — Gerçek Server Geldiğinde

### 5.1 — SİLİNECEK (stub yüzeyi — tek PR'da çıkmalı)

| Dosya | Durum |
|---|---|
| `Assets/_Hadal/Scripts/Core/Network/LocalPlacementAckSimulator.cs` | mevcut |
| `Assets/_Hadal/Scripts/Core/Network/IPlacementAckSimulator.cs` | mevcut |
| `Assets/_Hadal/Scripts/Core/Network/LocalProductionTickSimulator.cs` | **YENİ** |
| `Assets/_Hadal/Scripts/Core/Network/IProductionTickSimulator.cs` | **YENİ** |
| `Assets/_Hadal/Scripts/Core/Network/StubProductionTableAdapter.cs` | **YENİ** (SO → Shared tablo) |
| `Assets/_Hadal/Scripts/Core/Network/StubServerClock.cs` | **YENİ** (monoton tick) |
| `ProjectLifetimeScope.cs:65,68` + Sprint 3'ün eklediği stub kayıtları | satır silme |
| `CommandDispatcher.cs:106-110` — `TrySimulatePlacementAck` | satır silme |

> **Kural:** Bu listedeki her dosya `Hadal.Core.Network` içinde kalmalı ve **başka hiçbir namespace bunlara referans vermemeli** — `ProjectLifetimeScope` (DI kaydı) ve `CommandDispatcher` dışında. Bu kural CI'da bir grep ile denetlenebilir; Sprint 3'te bu grep'i ekleyin.

### 5.2 — KALACAK ve server'a AYNEN taşınacak (`HADAL.Shared`)

| Dosya | Durum |
|---|---|
| `shared/HADAL.Shared/Simulation/ProductionFormulas.cs` | **YENİ** — saf, deterministik, `UnityEngine` yok |
| `shared/HADAL.Shared/Simulation/OfflineAccrualFormula.cs` | **YENİ** — cap'li, yalnızca otoriteden çağrılır |
| `shared/HADAL.Shared/Simulation/SimulationConstants.cs` | **YENİ** — `ProductionTickSeconds`, `OfflineAccrualMaxHours` |
| `shared/HADAL.Shared/DTOs/BuildingProductionEntry.cs` | **YENİ** — SO'dan bağımsız üretim tablosu satırı |
| `shared/HADAL.Shared/DTOs/ResourceCapTable.cs` | **YENİ** — cap'ler client asset'inden çıkar |
| `shared/HADAL.Shared/DTOs/ResourceStateDto.cs` | mevcut, değişmez |
| `shared/HADAL.Shared/DTOs/StateDelta.cs` · `StateSnapshot.cs` · `EntityChangeDto.cs` | mevcut, değişmez |
| `shared/HADAL.Shared/Enums/ResourceType.cs` | mevcut, **genişletilir** (Karar 5) |

### 5.3 — KALACAK (client, saf görüntü katmanı — server'dan etkilenmez)

`ClientStateView.cs` · `ClientStateViewMutator.cs` · `EntityIdPrefixes.cs` · `StateSyncService.cs` · `CommandReconciliationSystem.cs` · `NetworkStateReceiver.cs` · `RollbackAnimator`

**YENİ (client, kalıcı):**
- `Assets/_Hadal/Scripts/Core/Events/ResourceStateChangedEvent.cs`
- `Assets/_Hadal/Scripts/UI/ResourceHudView.cs` — yalnızca `ILocalEventBus` dinler, `INetworkEventBus` **değil** (Forbidden #6)

### 5.4 — DEĞİŞTİRİLECEK (otorite kaybeder, silinmez)

- `Assets/_Hadal/Scripts/Managers/ResourceManager.cs` → saf projeksiyon (§4.4)
- `Assets/_Hadal/Scripts/Data/Enums/GameEnums.cs` → `ResourceType` silinir (Karar 5)
- `Assets/_Hadal/Scripts/Data/Hadal.Data.asmdef` → `HADAL.Shared` + `ProtobufNet` referansı

### 5.5 — Migration günü testi

> Stub dosyaları silinip yerine gerçek WebSocket akışı bağlandığında, **`HADAL.Shared/Simulation/` altındaki hiçbir dosyanın tek satırı değişmemeli.** Değişiyorsa, Sprint 3'te soyutlama sınırını yanlış çizmişiz demektir. Bunu Sprint 3'ün kabul kriteri yapın.

---

## 6. Karmaşıklık ve Operasyon Maliyeti

| İş | Tahmin | Risk |
|---|---|---|
| **P0-a** Enum birleştirme (Karar 5) — 9 dosya, derleyici destekli | 0.5 gün | Düşük (mekanik, sessiz hata imkânsız) |
| **P0-b** `ResourceManager` → projeksiyon (§4.4) | 0.5 gün | **Orta** — `IResourceService` tüketicileri kırılır; kapsamı ölçün |
| **P0-c** `LastAppliedServerTick` guard + `FlushPending()` (§4.3) | 0.25 gün | Düşük |
| **P1** `HADAL.Shared/Simulation/*` — formüller + sabitler + tablolar | 1 gün | Düşük (saf fonksiyon, birim testi kolay) |
| **P1** `StubServerClock` + `LocalProductionTickSimulator` + adapter | 1 gün | Orta — placement stub'ı ile tick paylaşımı (Değişiklik 1.2) |
| **P1** `ResourceStateChangedEvent` + `StateSyncService` yayını + grid-sync guard (§4.5) | 0.5 gün | Düşük |
| **P1** `ResourceHudView` + rate etiketi (görsel türetme, Karar 3) | 1 gün | Düşük |
| **P2** Exception handler + circuit breaker + stale HUD göstergesi (§4.1, §4.2) | 0.5 gün | Düşük |
| **P2** Birim testleri: formül determinizmi, cap, stale-delta drop, reconnect snapshot | 1 gün | — |
| **Toplam** | **~6.5 gün** | |

**Operasyon maliyeti: bugün sıfır.** Yeni proses, yeni bağımlılık, yeni deploy hedefi yok — stub client içinde yaşıyor. Bu, kararın en güçlü yanıdır ve **erken over-engineering'den kaçınmanın somut karşılığıdır.** Gerçek maliyet M5'te (Roadmap "Resource tick + StateDelta") gelir: tick scheduler, PostgreSQL yazma frekansı, delta fan-out. Sprint 3'ün tek işi, o gün geldiğinde **yalnızca stub dosyalarını silmek** olmasını garanti etmektir.

---

## 7. Sprint 3 Kabul Kriterleri

- [ ] Kod tabanında **tek bir** `ResourceType` enum'ı var; `Hadal.Data.Enums.ResourceType` silinmiş.
- [ ] `SchemaVersion.Current == 2`, `MinSupported == 2`.
- [ ] `ResourceManager` hiçbir kod yolunda kaynak **yaratmıyor** veya **yazmıyor**; `SeedStartingResources` yok.
- [ ] `ClientStateView.LastAppliedServerTick` mevcut; `ServerTick <= Last` olan delta düşürülüyor ve loglanıyor.
- [ ] Disconnect'te `CommandReconciliationSystem._pending` flush ediliyor.
- [ ] `HADAL.Shared/Simulation/*` içinde `UnityEngine`, `ScriptableObject` veya `Time.*` referansı **yok**.
- [ ] Resource-only delta geldiğinde `SyncOccupancyFromBuildings` çağrılmıyor.
- [ ] HUD'daki mutlak değerler **yalnızca** `ClientStateView.Data.resources`'tan besleniyor.
- [ ] Rate etiketi hiçbir koşulda `data.resources`'a yazmıyor (kod incelemesi maddesi).
- [ ] `RegisterEntryPointExceptionHandler` kayıtlı.
- [ ] Reconnect testi: 5 dk offline → snapshot → HUD tek adımda doğru değere atlıyor, client hiçbir accrual hesabı yapmıyor.
- [ ] Paket kaybı testi: 3 ardışık delta düşürüldüğünde HUD "stale" işaretleniyor, 4. delta geldiğinde tam düzeliyor.

---

## 8. Kayda Geçen Açık Borçlar (Sprint 3 kapsamı dışı)

| # | Borç | Ne zaman |
|---|---|---|
| D1 | `NetworkSerializationLayer` duplicate — Shared vs Core (§4.6-I) | Gerçek server yazılmadan ÖNCE |
| D2 | `Hadal.Core` ↔ `Hadal.Data` asmdef çevrim şüphesi (§4.6-II) | Karar 5 uygulanırken doğrula |
| D3 | Rate replikasyonu (`ResourceStateDto.RatePerMinute`) | Phase 3 — buff/survival sistemleri gelince |
| D4 | Roadmap "Zero duplicate types outside HADAL.Shared" exit criteria'sı hatalı ✅ işaretli | Karar 5 kapanınca güncelle |
| D5 | Consumption (tüketim) formülü — `BuildingDefinitionSO.ConsumptionPerTick` (`BuildingDefinitionSO.cs:23,40`) Sprint 3'te kullanılmıyor | Phase 3 (Core Survival) |

---

**Werner Vogels ilkesi, tek cümlede:** Bu ADR'nin tamamı tek bir soruya cevap veriyor — *"gerçek server geldiği gün kaç dosyayı yeniden yazacağız?"* Cevap **sıfır** olmalı; sadece silmeliyiz. Sprint 3'ün başarısı, ürettiği kaynak sayısıyla değil, o silme işleminin temizliğiyle ölçülecek.
