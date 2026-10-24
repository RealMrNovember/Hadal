# Sprint 3 — Risk Kaydı ve Test Planı

**Kapsam:** Phase 1 / Sprint 3 — "Resource producer + simulation tick"
**QA sahibi:** James Bach modu (Rapid Software Testing / Context-Driven)
**Tarih:** 2026-09-25
**Oturum kısıtı:** Bash, Unity derlemesi, test koşumu ve ağ erişimi KAPALI. Bu plandaki her doğrulama adımı **repo sahibi tarafından Unity Editor'de elle** yürütülecek şekilde yazıldı.

> **İletişim notu:** Aşağıdakiler "bug raporu" değil **risk bildirimi**dir. Her madde için bilgi ve bağlam verilir; düzeltme kararı repo sahibinindir. Sadece "Bu sprint'i bloklar" başlığı altındakiler için QA'nın net bir tavsiyesi vardır.

---

## 0. Yönetici özeti

Sprint 3 kodu **henüz yazılmadı** (repoda `LocalProductionTickSimulator` yok, `shared/HADAL.Shared/` altında `Formulas/` dizini yok — bkz. §1.7). Bu yüzden bu belge **mevcut altyapıda Sprint 3'ün üzerine ineceği zemin**deki riskleri kaydeder ve kodu yazarken/yazdıktan sonra koşulacak doğrulamaları tanımlar.

Zemin üç noktada kırılgan:

1. **İki farklı `ResourceType` enum'u** aynı `int` alanına yazıyor ve bu alan sessizce yanlış kaynağa map ediliyor (§1.1).
2. **Inbound frame ayrıştırma sırası** yanlış — gerçek gateway'den gelen `StateDelta` frame'leri `HandshakeAckEnvelope` sanılıp yutulabilir (§1.2). Sprint 2'de bu gizli kaldı çünkü stub delta'lar wire'ı bypass ediyor.
3. **Tick/delta sıra kontrolü yok** — `ServerTick`/`BaselineTick` hiçbir yerde doğrulanmıyor (§1.3).

Repoda **hiçbir test projesi, test asmdef'i, EditMode/PlayMode dizini yok** (§1.8). Bu, Roadmap'in hâlâ işaretsiz olan Phase 0-R exit kriteri ile doğrudan bağlantılı: *"Prediction + rollback tested at 100ms and 200ms simulated latency"* (`docs/HadalDevelopmentRoadmap.md:37`).

---

## 1. Risk kaydı

Ölçek: Olasılık / Etki / Tespit zorluğu ∈ {Düşük, Orta, Yüksek}. "Tespit zorluğu" = **yüksek ise sessizce geçer**, düşük ise ilk denemede gözünüze çarpar.

| # | Risk | Olasılık | Etki | Tespit zorluğu | Azaltım |
|---|------|----------|------|----------------|---------|
| R1 | **Çifte `ResourceType` enum'u → sessiz veri bozulması.** `ResourceSaveEntry.resourceType` (int) alanına iki farklı enum yazıyor | **Yüksek** (Sprint 3 bu yolu ilk kez sıcak hale getiriyor) | **Yüksek** (yanlış kaynak, yanlış cap, yanlış HUD) | **Yüksek** (exception yok, log yok, sadece rakam yanlış) | Tek bir `ResourceType` (Shared) kanonik yapılmalı **veya** `ClientStateViewMutator` ile `ResourceManager` arasına açık bir `ResourceTypeMap` konmalı + EditMode round-trip testi |
| R2 | **Wire frame ayrıştırma sırası: `StateDelta` → `HandshakeAck` olarak yutuluyor** | **Yüksek** (gerçek gateway devreye girince) | **Yüksek** (tüm resource delta'ları kaybolur + sahte handshake + tekrarlayan `FlushBuffer`) | **Yüksek** (client "bağlı" görünür, sadece kaynak akmaz) | Envelope'lara ayırt edici bir `FrameKind` alanı eklemek; veya handshake decode'unu `SessionToken`/`Accepted` dolu olma şartına bağlamak; veya sırayı Delta→Snapshot→Handshake yapmak |
| R3 | **Tick sırası / baseline doğrulaması yok.** `StateDelta.ServerTick` ve `BaselineTick` hiçbir yerde okunup kıyaslanmıyor | Orta | **Yüksek** (reconnect'te kaynak geri sarar) | Orta | `ClientStateView`'a `LastAppliedServerTick` eklemek; `ServerTick <= LastApplied` olan delta'yı atmak; `BaselineTick != LastApplied` ise snapshot istemek |
| R4 | **Reconnect'te inbound kuyruk temizlenmiyor.** `HandleDisconnected` sadece `_session.Reset()` yapıyor; `CommandReconciliationSystem._pending` duruyor | Orta | Orta | Orta | Disconnect'te kuyruğu boşaltmak; reconnect sonrası ilk paketin snapshot olmasını şart koşmak |
| R5 | **`ApplyResources` tüm resource listesini komple değiştiriyor.** Partial delta gelirse dokunulmayan kaynaklar listeden düşer → HUD'da 0 görünür | **Yüksek** (partial delta ekonomi tick'inin doğal formatı) | **Yüksek** | Orta (gözle görülür ama "server öyle gönderdi" diye yanlış teşhis edilir) | Delta yolunda **merge** (upsert), snapshot yolunda **replace** semantiği ayrılmalı |
| R6 | **`ResourceManager` cüzdanı ile `ClientStateView` iki ayrı doğruluk kaynağı.** Replike edilen delta cüzdana hiç akmıyor | **Yüksek** | **Yüksek** (HUD hangisini okursa o) | Düşük (ilk denemede görülür) | HUD'u **yalnız** `ClientStateView`'dan beslemek; `ResourceManager.Wallet`'ı Sprint 3'te read-only projeksiyona indirgemek |
| R7 | **Client-side seeding = otorite sızıntısı.** `SeedStartingResources()` snapshot gelmeden cüzdanı dolduruyor | **Yüksek** (mevcut davranış) | Orta | Düşük | Seed'i kaldırıp "snapshot bekleniyor" durumu göstermek — Forbidden Pattern #1 ve #8'e komşu |
| R8 | **Düşük pil tick ölçeklemesi ekonomiyi yavaşlatır.** `TickManager` `deltaTime * 0.5` uyguluyor; `IGameTickable` üzerinden üreten bir simülatör yarı hızda üretir | **Yüksek** (pil <%20 + deşarj) | **Yüksek** (ekonomi cihaz durumuna bağlı = adaletsiz) | **Yüksek** (sadece düşük pilde, sadece uzun sürede fark edilir) | Simülatörü `TickManager`'a **kaydetmemek**; `Time.unscaledTime` veya server tick sayacı kullanmak |
| R9 | **`Time.deltaTime` arka planda ilerlemez ve frame-clamp'lidir.** Uygulama arka plandayken hiçbir tick birikmez | **Yüksek** | Orta (offline üretim sıfırlanır) | Orta | Offline birikimi **yalnız sunucu snapshot'ıyla** kapatmak (Forbidden Pattern #8 zaten client offline hesabı yasaklıyor) |
| R10 | **Negatif bakiye mümkün.** `ResourceManager.Add(type, -N)` cap'e clamp'liyor ama 0'a clamp'lemiyor | Orta (consumption eklenince yükselir) | Orta | Orta | `Add` içinde `Math.Max(0, ...)`; `ResourceWallet.Add` de `Set` gibi clamp'lemeli |
| R11 | **Çifte sayım: stub simülatör + gerçek gateway aynı anda.** `LocalPlacementAckSimulator` kalıbı wire'ı bypass edip doğrudan `EnqueueInbound` yapıyor; üretim simülatörü aynı kalıbı izlerse gateway URL dolu olduğunda iki kaynak birden üretir | Orta | **Yüksek** | Orta | Simülatörü açık bir define/flag arkasına almak; `GatewayUrl` doluyken simülatörü hiç register etmemek |
| R12 | **`_pending` kuyruğu sınırsız.** `EnqueueInbound` cap'siz; her `Tick`'te tamamı drain ediliyor | Düşük | Orta (uzun arka plan sonrası frame spike) | Orta | Kuyruk cap'i + tick başına drain bütçesi |
| R13 | **`NetworkSerializationLayer` iki yerde tanımlı** — `Hadal.Core.Network` ve `HADAL.Shared.Serialization`. Client sınıfı `SchemaVersion` taşımıyor | Orta | Orta | Düşük | Duplicate'i silip Shared'ınkini kullanmak (Forbidden Pattern #3 komşuluğu) |
| R14 | **`Hadal.UI` asmdef'i `HADAL.Shared`'ı referans etmiyor.** Resource HUD Shared enum'unu göremez → R1'i kalıcılaştıran bir "int cast" kestirmesine zorlar | **Yüksek** | Orta | Düşük (derleme hatası) | HUD yazılmadan **önce** `Hadal.UI.asmdef`'e `HADAL.Shared` eklemek |
| R15 | **`shared/` klasörü Unity ağacının dışında ve `.meta` dosyası yok.** Temiz bir clone'da `HADAL.Shared` derlenmeyebilir | Orta | **Yüksek** (build kırılır) | Düşük (derleme hatası) | Link/junction'ı repo'ya belgelemek veya Shared'ı `Packages/com.hadal.shared` olarak taşımak |
| R16 | **Test altyapısı yok.** Hiç asmdef, hiç EditMode/PlayMode dizini | **Kesin** | **Yüksek** (0-R exit kriteri hâlâ açık) | Düşük | §5'teki minimum EditMode seti |

### 1.1 R1 — Somut veri bozulması senaryosu

İki enum yan yana:

| int | `Hadal.Data.Enums.ResourceType`<br>(`Data/Enums/GameEnums.cs:3-11`) | `HADAL.Shared.Enums.ResourceType`<br>(`shared/HADAL.Shared/Enums/ResourceType.cs:6-14`) |
|-----|---|---|
| 0 | `Oxygen` | `Unknown` |
| 1 | `Energy` | `Oxygen` |
| 2 | `Food` | `Food` |
| 3 | `Biomass` | `Biomass` |
| 4 | `Titanium` | `Research` |
| 5 | `Hadalite` | `Alloy` |

Yazan ve okuyan taraflar:

- **Sunucu → view yazımı:** `ClientStateViewMutator.ApplyResources` → `resourceType = (int)pair.Key` burada `pair.Key` **Shared** enum'u (`Core/StateSync/ClientStateViewMutator.cs:123-134`, `using HADAL.Shared.Enums;` satır 5).
- **Cüzdan → view yazımı:** `ResourceManager.CaptureSave` → `resourceType = (int)pair.Key` burada `pair.Key` **Data** enum'u (`Managers/ResourceManager.cs:96-102`, `using Hadal.Data.Enums;` satır 8).
- **View → cüzdan okuması:** `ResourceManager.RestoreSave` → `_wallet.Set((ResourceType)entry.resourceType, ...)` burada cast **Data** enum'una (`Managers/ResourceManager.cs:113`).

**Senaryo (Sprint 3'te kaçınılmaz):**

1. Sunucu (veya stub simülatör) `ResourceStateDto.Amounts = { Shared.Oxygen(1): 500, Shared.Research(4): 20 }` gönderir.
2. `ClientStateViewMutator.ApplyResources` bunu `data.resources = [{1,500},{4,20}]` yazar.
3. Herhangi bir kod yolu `ResourceManager.RestoreSave` çağırır (bugün `GameBootstrapEntryPoint.cs:66`; Sprint 3'te HUD/cüzdan senkronu için çağrılması çok muhtemel).
4. Cüzdan `Data.Energy = 500` ve `Data.Titanium = 20` olur.
5. `ResourceManager.Add` cap'i `_database.Get(type)?.MaxCapacity` ile arar (`ResourceManager.cs:77`) — `ResourceDefinitionSO._type` **Data** enum'u (`Data/Config/ResourceDefinitionSO.cs:10,19`). Yani **Oxygen'in cap'i yerine Energy'nin cap'i** uygulanır.
6. HUD "Oxygen 500" yerine "Energy 500" gösterir. Exception yok, log yok.

Ters yön de bozuk: `CaptureSave` `Data.Oxygen(0)` yazar; Shared tarafı bunu `Unknown` okur — yani oksijen tamamen kaybolur.

**Kritik ayrıntı:** Bu bir *offset* hatası değil. Kümeler farklı (`Energy`, `Titanium`, `Hadalite` Shared'da yok; `Research`, `Alloy`, `Unknown` Data'da yok). Tek satırlık `+1/-1` düzeltmesi **imkânsız**. Açık bir mapping tablosu veya tek enum şart.

### 1.2 R2 — Frame yutulması (detay)

`NetworkStateReceiver.HandleMessageReceived` sırası (`Core/Network/NetworkStateReceiver.cs:143-167`): **önce** `TryDecodeHandshakeAck`, sonra delta, sonra snapshot.

Protobuf alan şekilleri:

| tag | `HandshakeAckEnvelope` (`shared/.../HandshakeAckEnvelope.cs:9-12`) | `StateDeltaEnvelope` (`shared/.../StateDeltaEnvelope.cs:8-11`) | wire type uyumu |
|-----|---|---|---|
| 1 | `uint SchemaVersion` | `uint SchemaVersion` | varint = varint ✔ |
| 2 | `bool Accepted` | `ulong ServerTick` | varint = varint ✔ |
| 3 | `CommandResultCode Result` | `ulong BaselineTick` | varint = varint ✔ |
| 4 | `string SessionToken` | `byte[] Payload` | length-delimited = length-delimited ✔ |

Her dört alan da wire-type uyumlu → `TryDeserialize<HandshakeAckEnvelope>` **exception atmaz, true döner**. Ardından:

- `SchemaVersion == 1 >= MinSupported` ✔ (satır 188)
- `Accepted = (ServerTick != 0)` → gerçek tick'te **true**
- `Result = (CommandResultCode)BaselineTick` → `Rejected = 2` (`shared/.../CommandResultCode.cs:10`). Yani `BaselineTick == 2` değilse **reddedilmez**.

Sonuç: `HandleHandshakeAck` çağrılır, `_session.CompleteHandshake()` + `_commandDispatcher.FlushBuffer()` koşar ve `return` ile **delta çöpe gider** (satır 149-151).

**Neden Sprint 2'de görünmedi:** `LocalPlacementAckSimulator` delta'yı wire'a değil doğrudan `_reconciliation.EnqueueInbound`'a veriyor (`Core/Network/LocalPlacementAckSimulator.cs:117-124`). Yani stub yol bu kodu hiç geçmiyor. Sprint 3'ün stub simülatörü de aynı kalıbı izlerse **bu hata yine gizli kalır ve gerçek gateway gününe kadar ertelenir.** Senaryo S8 bunu zorlar.

### 1.3 R3/R4 — Tick + delta birleşiminde çift sayım ve snapshot çakışması

`CommandReconciliationSystem.ProcessQueue` (`Core/Network/CommandReconciliationSystem.cs:64-83`) `packet.ServerTick` ve `packet.BaselineTick` alanlarını **hiç okumuyor**; `StateDelta.BaselineTick` de hiçbir yerde kıyaslanmıyor. Kuyruktaki her paket geldiği sırayla uygulanıyor.

- **Bugün çift sayım riski düşük**, çünkü `ApplyResources` mutlak yazıyor (idempotent). Aynı delta iki kez gelse toplam değişmez.
- **Ama sıra bozulursa kayıp/geri sarma var:** snapshot(tick 500) uygulanır, sonra kuyrukta bekleyen disconnect öncesi delta(tick 480) uygulanır → kaynaklar 480'e geri döner. Reconnect'te `HandleDisconnected` kuyruğu temizlemiyor (satır 103-106).
- **Çift sayım riski Sprint 3 ile doğar:** HUD veya cüzdan delta'ları `+=` ile biriktirirse, ya da stub simülatör + gerçek gateway aynı anda üretirse (R11). **Tavsiye: üretim delta'sı asla "kazanç miktarı" değil, her zaman "yeni toplam" taşımalı.** Bu tek karar R3'ün çift sayım kolunu tamamen kapatır.

### 1.4 R5 — `ApplyResourceChange` mutlak yazım doğru mu?

`ClientStateViewMutator.ApplyResourceChange` → `ApplyResources` (satır 109-135) `data.resources` dizisini **komple yeniden inşa ediyor** (`.Select(...).ToArray()`).

**Değerlendirme:** Mutlak değer taşımak **doğru tercih** (idempotent, drift yok, çift sayım yok). Bozuk olan mutlak değer değil, **kapsam**: delta yalnız değişen kaynakları taşıyorsa, taşımadıkları listeden **düşüyor**. Bu sessiz drift değil, sessiz **silinme**.

Ayrıca `change.Kind == Delete` geldiğinde tüm resource listesi boşaltılıyor (satır 111-114) — tek bir kaynağın silinmesi ile tüm cüzdanın silinmesi ayırt edilemiyor (`EntityId` "resource:" önekinden sonraki kısım hiç okunmuyor, kıyasla building yolu satır 84'te okuyor).

**Tavsiye:** Snapshot yolu replace kalsın; delta yolu **upsert** olsun (var olan girdiyi güncelle, olmayanı ekle, hiçbirini silme).

### 1.5 R8 — Düşük pil modu ekonomiyi bozar mı?

`TickManager.Update` (`Managers/TickManager.cs:34-44`):

```
ApplyPowerProfile();
var delta = Time.deltaTime * _tickScale;
for (...) _tickables[i].Tick(delta);
```

`ApplyPowerProfile` (satır 46-59): pil deşarjda ve `batteryLevel < 0.2` ise `_tickScale = LowPowerTickMultiplier` — default **0.5** (`Data/Config/MobilePerformanceConfigSO.cs:20`).

`ResourceManager.Tick` (satır 49-56) bu ölçekli delta'yı `_tickTimer`'a biriktirip `Config.BaseTickIntervalSeconds` (default **1.0**, `Data/Config/GameConfigSO.cs:15`) ile kıyaslıyor.

**Cevap: EVET, bozulur.** `LocalProductionTickSimulator` `IGameTickable` olarak `TickManager`'a kayıt olursa, pil %20'nin altındayken üretim tick'i her 1 saniye yerine her **2 saniyede** bir ateşlenir → oyuncu **yarı hızda** kaynak üretir. Bu, cihaz pil durumuna bağlı bir ekonomi farkı demektir ve MMO için kabul edilemez.

İkinci kat: `Time.deltaTime` frame-clamp'lidir ve uygulama arka plandayken hiç ilerlemez (R9). Yani `IGameTickable` tabanlı herhangi bir birikim duvar saatiyle **hiçbir zaman** örtüşmez.

**Tavsiye:** Üretim simülatörü `TickManager`'a kayıt olmasın. `LocalPlacementAckSimulator`'daki kalıbı izlesin — `Time.unscaledTime` (`LocalPlacementAckSimulator.cs:53,62`) veya kendi `ITickable` (VContainer) girişi. En doğrusu: simülatör bir **server tick sayacı** tutsun ve `ServerTick` alanını gerçekten doldursun (bugün sabit `1` yazılıyor, `LocalPlacementAckSimulator.cs:84`), böylece R3'ün testi de mümkün olsun.

### 1.6 R10 — Negatif consumption

- `ResourceWallet.Set` 0'a clamp'liyor: `Math.Max(0, amount)` (`Data/Models/GameModels.cs:55`).
- `ResourceWallet.Add` clamp'lemiyor: `_balances[type] = Get(type) + amount` (satır 50).
- `ResourceManager.Add` yalnız **üst** sınırı uyguluyor: `var next = Math.Min(cap, old + amount)` (satır 79).

`Add(Food, -100)` çağrısı bakiye 50 iken → `next = -50` → cüzdan **-50**. Consumption Sprint 3'e girerse bu yol açılır. HUD negatif sayı gösterir.

### 1.7 Sprint 3 için henüz var olmayan parçalar

Doğrulandı (glob/grep ile):

- `shared/HADAL.Shared/` altında **`Formulas/` dizini yok** — tüm dosyalar: `Commands/`, `DTOs/`, `Enums/`, `Serialization/`. Sprint 3 gereksinimi olan "saf statik üretim formülleri" henüz yazılmamış.
- `LocalProductionTickSimulator` repoda **yok**.
- `Assets/_Hadal/Scripts/UI/` altında yalnız `UIManager.cs` ve `UIViewBase.cs` var — **Resource HUD yok**.
- `BuildingStateDto`'da üretim/rate alanı yok; üretim hızı ya `BuildingDefinitionSO`'dan (client-side config) ya Shared'dan gelecek. **Client-side config'den gelirse Forbidden Pattern #8'e yaklaşır** — formül Shared'da, **girdi** de Shared/server'dan olmalı.

### 1.8 Test altyapısı — durum tespiti

**Repoda hiçbir test projesi, test asmdef'i veya test dizini yoktur.** Arandı ve bulunamadı:

- `**/*Tests*` → 0 sonuç
- `**/*.Tests.asmdef` → 0 sonuç
- `**/{EditMode,PlayMode,Tests}/**` → 0 sonuç
- Mevcut asmdef'lerin tamamı: `Hadal.Data`, `Hadal.UI`, `Hadal.Editor`, `Hadal.Gameplay`, `Hadal.Managers`, `Hadal.Core` (+ `shared/HADAL.Shared`, `Packages/` altında `ProtobufNet` ve `colyseus.nativewebsocket`). Hiçbiri test asmdef'i değil, hiçbiri `UnityEngine.TestRunner`/`nunit.framework.dll` referans etmiyor.

Bunun doğrudan sonucu: `docs/HadalDevelopmentRoadmap.md:37` satırındaki Phase 0-R exit kriteri *"Prediction + rollback tested at 100ms and 200ms simulated latency"* — **bugün doğrulanabilir değil**, çünkü test edecek bir harness yok. Roadmap bunu "(Phase 1 test harness)" diye erteliyor; Sprint 3 o harness'ın kurulacağı en makul yer.

---

## 2. SFDPOT ısı haritası — Sprint 3

| Boyut | Sıcaklık | Sprint 3'e özgü odak |
|-------|----------|----------------------|
| **S**tructure (yapı) | 🔴 **Sıcak** | İki `ResourceType` (R1); iki `NetworkSerializationLayer` (R13); `Hadal.UI` → `HADAL.Shared` referansı yok (R14); `shared/` Unity ağacı dışında (R15); Shared'da `Formulas/` yok |
| **F**unction (işlev) | 🔴 **Sıcak** | Üretim formülünün doğruluğu; delta→view→HUD zinciri; cüzdan ile view'ın iki ayrı doğruluk kaynağı olması (R6); seeding otoritesi (R7) |
| **D**ata (veri) | 🔴 **Sıcak** | Enum map'i (R1); partial delta'da silinme (R5); negatif bakiye (R10); `long` cap ve overflow; `Dictionary<ResourceType,long>` protobuf map serileştirmesi |
| **P**latform | 🟡 Ilık | Düşük pil `_tickScale` (R8); arka planda `deltaTime` durması (R9); `Application.lowMemory` → `ClearAll` sırasında HUD; IL2CPP altında protobuf-net reflection'ı (Editor'de çalışan cihazda çalışmayabilir) |
| **O**perations (kullanım) | 🟡 Ilık | Reconnect akışı (R3/R4); uygulama arka plan/ön plan geçişleri; oyuncunun tick anında bina yerleştirmesi; uzun offline |
| **T**ime (zaman) | 🔴 **Sıcak** | Tick sırası ve baseline (R3); duvar saati vs frame saati (R9); NTP kayması / cihaz saati oynaması; uzun offline birikimi; tick ile placement ack'inin yarışması |

**Isı okuması:** Sprint 3'ün riski **Structure/Data/Time üçgeninde** yoğunlaşıyor. Presentation (HUD görselliği) en soğuk bölge — oraya test eforu harcamayın.

---

## 3. Elle yürütülecek doğrulama senaryoları

Ortak ön koşul: Unity 6 ile proje açık, `ProjectLifetimeScope` bulunan ana sahne yüklü, Console açık ve **Error Pause kapalı**, Collapse **kapalı** (tekrarlayan logları saymak için).

---

### S1 — Enum haritası çakışması (R1) — *önce bunu koş*

**Neden:** En yüksek etki × en yüksek tespit zorluğu. Kod yazmadan, sadece Editor'de görünür.

1. Play mode'a gir.
2. `LocalProductionTickSimulator` (veya geçici bir test kancası) ile `ResourceStateDto.Amounts` içinde **yalnız** `HADAL.Shared.Enums.ResourceType.Oxygen` (=1) için 500 taşıyan bir `resource:` delta'sı üret.
3. Delta uygulandıktan sonra `ClientStateView.Data.resources` içeriğine Inspector/debug log ile bak.
4. Sonra `ResourceManager.RestoreSave(view.Data)` yolunu tetikle (veya `ResourceManager.Wallet.Snapshot()` içeriğini logla).

- **Beklenen (doğru davranış):** Cüzdanda `Oxygen = 500`.
- **Gerçek (tahmin edilen):** `data.resources = [{resourceType:1, amount:500}]`, cüzdanda **`Energy = 500`**, `Oxygen = 0`.
- **Geçme ölçütü:** Cüzdandaki anahtar `Oxygen` olmalı. Değilse R1 doğrulanmıştır.

**Ek kontrol:** Aynı testi `Shared.Research` (=4) ile tekrarla → cüzdanda `Titanium` çıkarsa kesin teyit.

---

### S2 — Sıfır bina, sıfır üretim (sınır: boş durum)

1. Yeni Play session (hiç bina yerleştirmeden).
2. En az 10 üretim tick'i geçecek kadar bekle (`BaseTickIntervalSeconds` default 1s → ~10s).

- **Beklenen:** Resource HUD değerleri **hiç değişmez**. Hiçbir `resource:` delta'sı üretilmez (veya üretilirse tüm değerler aynı kalır). Console'da NullReference / "Sequence contains no elements" yok.
- **Gözlenecek:** `ApplyResources` boş `Amounts` ile çağrılırsa `data.resources` **boş diziye** düşer ve HUD tüm kaynakları kaybeder → bu R5'in en basit tezahürü.

---

### S3 — Cap dolu (sınır: üst sınır)

1. `ResourceDatabase` içinde bir kaynağın `MaxCapacity` değerini küçük bir sayıya çek (örn. 100).
2. Başlangıç miktarını 95 yap, üretim hızını tick başına 10 yap.
3. 5 tick bekle.

- **Beklenen:** Değer 100'de durur, **100'ü aşmaz**, `ResourceChangedEvent` cap'e vardıktan sonra artık atılmaz (`ResourceManager.cs:80-81` `next == old` erken çıkışı).
- **Riskli nokta:** Cap sunucu tarafında mı client tarafında mı uygulanıyor? Client'ta `_database.Get(type)?.MaxCapacity` ile uygulanıyorsa (`ResourceManager.cs:77`) ve sunucu 105 gönderirse, cüzdan 100'de kalırken `ClientStateView` 105 tutar → **iki kaynak iki farklı sayı** (R6). HUD hangisini okuyorsa onu not edin.
- **Karar verilmesi gereken:** Cap **sunucu otoritesi** olmalı. Client cap'i yalnız görsel amaçlı kalmalı.

---

### S4 — Negatif consumption (sınır: alt sınır)

1. Üretim formülüne negatif net değer verdirecek bir consumption senaryosu kur (veya geçici olarak `ResourceManager.Add(ResourceType.Food, -1000)` çağır).
2. HUD'a ve `Wallet.Snapshot()`'a bak.

- **Beklenen:** Bakiye **0'ın altına inmez**; HUD "0" gösterir.
- **Gerçek (tahmin edilen):** `ResourceManager.Add` yalnız cap'e clamp'liyor (`ResourceManager.cs:79`), `ResourceWallet.Add` hiç clamp'lemiyor (`GameModels.cs:50`) → **negatif bakiye ve negatif HUD**. R10 doğrulanır.
- **Not:** `Set` 0'a clamp'liyor (`GameModels.cs:55`) — yani sunucudan gelen `Set` yolu güvenli, `Add` yolu değil. Tutarsızlık kendi başına bir bulgu.

---

### S5 — Tick sırasında bina yerleştirme (yarış koşulu)

1. Üretim tick'i açık haldeyken, tick'in ateşleneceği ana denk gelecek şekilde (1 sn aralıkta, sürekli tıklayarak) bir bina yerleştir.
2. `LocalPlacementAckSimulator` 200 ms sonra ack + building delta atıyor (`LocalPlacementAckSimulator.cs:19,53`).

- **Beklenen:** Hem building hem resource delta'sı uygulanır; ghost doğru şekilde çözülür; `BuildingPlacementResolvedEvent` bir kez yayınlanır.
- **Riskli nokta:** `CommandReconciliationSystem` yalnız **tek bir** pending command tutuyor (`_lastCommandId`, satır 25-27, `RegisterPendingCommand` satır 51-56 üzerine yazıyor). Üretim delta'sı `command:` değişikliği taşımadığı için doğrudan çakışma yok — ama iki placement hızlıca art arda yapılırsa ilkinin ack'i **sessizce yok sayılır** (satır 98-99'daki `CommandId` eşitlik kontrolü). Bunu da aynı oturumda dene: **iki binayı 200 ms içinde** yerleştir.
- **Geçme ölçütü:** İki bina da render olmalı, her ikisi için de bir resolution event'i görülmeli.

---

### S6 — Reconnect: snapshot ile bekleyen delta çakışması (R3/R4)

1. Play mode'da, gerçek gateway'e bağlıyken (veya `GatewayUrl`'ü geçerli bir yerel endpoint'e çevirip) kaynaklar akarken **ağı kes** (Wi-Fi kapat / endpoint'i durdur).
2. Console'da `NetworkReconnectingEvent` ("Reconnecting...") logunu bekle (`NetworkStateReceiver.cs:139`).
3. 30 saniye bekle, sonra ağı geri aç.

- **Beklenen:** Bağlantı gelir, handshake tamamlanır, **snapshot** alınır, HUD sunucudaki güncel değere **atlar** ve orada kalır.
- **Gerçek (tahmin edilen riskler):**
  - `HandleDisconnected` yalnız `_session.Reset()` yapıyor (satır 103-106); `_pending` kuyruğu temizlenmiyor → kopma anında kuyrukta kalmış eski delta'lar **snapshot'tan sonra** uygulanabilir → HUD **geri sarar**.
  - `ServerTick`/`BaselineTick` hiç kıyaslanmadığı için (`CommandReconciliationSystem.cs:64-83`) bu geri sarma **hiçbir uyarı üretmez**.
- **Ölçüm:** Kopma öncesi ve reconnect sonrası HUD değerlerini not edin. Reconnect sonrası değer, kopma öncesinden **küçükse** R3 doğrulanmıştır.

---

### S7 — Uzun offline (sınır: uzun süre)

1. Kaynaklar akarken uygulamayı **arka plana al** (Editor'de: `Application.isFocused` ile test etmek zor → tercihen cihaz build'i; Editor'de yaklaşık karşılığı: Play mode'u duraklat).
2. 10 dakika bekle. Geri dön.

- **Beklenen (doğru mimari):** Client **hiçbir şey hesaplamaz**. Sunucu snapshot'ı gelir ve HUD doğru toplama atlar. Bu, `docs/HADAL_MASTER_DEVELOPMENT_GUIDE.md:100` — Forbidden Pattern **#8 "Offline reward calculation on client"** gereğidir.
- **İzlenecek kırmızı bayrak:** Geri dönüşte HUD'un **sunucu paketi gelmeden** artmış olması. Bu, simülatörün duvar saati farkını client'ta kapattığı anlamına gelir ve **doğrudan Forbidden Pattern #8 ihlalidir.**
- **İkinci kırmızı bayrak:** Geri dönüşte tek bir frame'de yüzlerce tick'in birden işlenip frame spike yaratması (R12).

---

### S8 — Gerçek wire üzerinden resource delta (R2) — *bu senaryoyu atlamayın*

**Neden kritik:** Sprint 3'ün stub'ı wire'ı bypass ederse bu hata **hiç görünmez** ve gerçek gateway gününe ertelenir.

1. `LocalProductionTickSimulator`'ı, delta'yı doğrudan `EnqueueInbound`'a vermek yerine **serileştirip `IWebSocketClient.OnMessageReceived` olayını taklit edecek** şekilde bir kez çalıştır (geçici bir debug butonu yeterli). `StateDeltaEnvelope { SchemaVersion=1, ServerTick=42, BaselineTick=41, Payload=<StateDelta bytes> }` gönder.
2. Console'a bak.

- **Beklenen:** `CommandReconciliationSystem` delta'yı alır, HUD güncellenir.
- **Gerçek (tahmin edilen):** Console'da **`[NetworkStateReceiver] Gateway handshake complete — command buffer flushed.`** (satır 179) görülür ve **HUD hiç güncellenmez**. Çünkü `TryDecodeHandshakeAck` önce denenir ve dört alanın da wire-type'ı uyumlu olduğu için başarılı olur (detay §1.2).
- **Ek varyant:** `BaselineTick = 2` ile tekrar gönder → bu sefer `Result = Rejected` okunur ve Console'da **`Gateway handshake rejected.`** (satır 173) çıkar, delta yine kaybolur. İki farklı yanlış log, aynı kök neden.
- **Geçme ölçütü:** Hiçbir handshake logu çıkmamalı, HUD güncellenmeli.

---

### S9 — Partial delta ile kaynak silinmesi (R5)

1. Önce tam bir snapshot uygula: `{Oxygen:500, Food:300, Biomass:100}`.
2. Sonra **yalnız** `{Food:310}` içeren bir `resource:` delta'sı gönder.

- **Beklenen:** HUD `Oxygen:500, Food:310, Biomass:100`.
- **Gerçek (tahmin edilen):** `ApplyResources` diziyi baştan kuruyor (`ClientStateViewMutator.cs:128-134`) → HUD **yalnız `Food:310`**, diğerleri kaybolur veya 0 görünür.
- **Ek varyant:** `Kind = Delete` olan bir `resource:` değişikliği gönder → **tüm** kaynaklar silinir (satır 111-114), tek kaynak değil.

---

### S10 — Düşük pil tick ölçeklemesi (R8)

**Editor'de doğrudan test edilemez** (`SystemInfo.batteryStatus` Editor'de `Unknown`). İki yol:

**A — Config ile taklit (Editor):**
1. `MobilePerformanceConfig` üzerinde `_lowPowerTickMultiplier` değerini geçici olarak `0.25` yap.
2. `TickManager.ApplyPowerProfile` içindeki koşulu geçici olarak `true` döndürecek şekilde **debug amaçlı** zorla (kodu commit etmeyin).
3. 60 saniye boyunca üretilen toplam kaynağı ölç ve `_tickScale = 1f` ile aynı ölçümü karşılaştır.

**B — Cihazda (kesin):** Telefonu %20'nin altına düşür, şarjdan çıkar, 5 dakikalık üretimi normal pil durumuyla karşılaştır.

- **Beklenen:** İki ölçüm **eşit** olmalı — üretim pil durumundan bağımsız.
- **Gerçek (tahmin edilen):** `0.25` çarpanla üretim **dörtte bire** düşer. R8 doğrulanır.
- **Doğrulama sorusu:** `LocalProductionTickSimulator`, `TickManager.Register` çağırıyor mu? Çağırıyorsa test etmeden bile risk kesindir.

---

### S11 — Cihaz saati değişimi / NTP kayması (sınır: zaman)

1. Play mode'da kaynaklar akarken **işletim sistemi saatini 2 saat ileri** al.
2. HUD'a bak. Sonra saati **2 saat geri** al (orijinalden geriye).

- **Beklenen:** **Hiçbir şey değişmez.** Üretim `DateTime.UtcNow`'a değil monotonic saate veya server tick'ine bağlı olmalı.
- **İzlenecek:** `TimeManager._sessionStartUtc` `DateTime.UtcNow.Ticks` ile dolduruluyor (`Managers/TimeManager.cs:22`) ama şu an hiçbir yerde okunmuyor. Sprint 3 bunu üretim hesabına sokarsa **saat değiştirilerek kaynak üretilebilir** → cheat vektörü + Forbidden Pattern #8.
- **Doğru referanslar:** `Time.realtimeSinceStartupAsDouble` (`TimeManager.cs:18`) ve `Time.unscaledTime` (`LocalPlacementAckSimulator.cs:53`) saat değişiminden etkilenmez; `DateTime.UtcNow` etkilenir. Simülatörün hangisini kullandığını koda bakarak teyit edin.

---

### S12 — Çift kaynak: stub + gateway aynı anda (R11)

1. `GameConfigSO.GatewayUrl` dolu (default `wss://hadal.cicibyte.com/gateway`, `GameConfigSO.cs:26`) iken Play mode'a gir.
2. `LocalProductionTickSimulator`'ın register edilip edilmediğini Console log'u veya breakpoint ile teyit et.

- **Beklenen:** Gerçek gateway bağlıyken stub simülatör **hiç çalışmamalı**.
- **Riskli nokta:** `LocalPlacementAckSimulator` `ProjectLifetimeScope.cs:65,68`'te **koşulsuz** register ediliyor ve `Start()` içinde koşulsuz `CompleteHandshake()` çağırıyor (`LocalPlacementAckSimulator.cs:39-46`). Üretim simülatörü aynı kalıbı izlerse gerçek gateway ile **çift üretim** olur.
- **Geçme ölçütü:** Simülatör bir define (`HADAL_LOCAL_SIM`) veya config flag'i arkasında olmalı.

---

### S13 — Temiz clone derleme kontrolü (R15) — *bir kez*

1. Repoyu **temiz bir dizine** clone'la, Unity ile aç.

- **Beklenen:** `HADAL.Shared` assembly'si derlenir.
- **Riskli nokta:** `shared/` klasörü `Assets/` veya `Packages/` altında değil ve **hiç `.meta` dosyası içermiyor**; `Assets/_Hadal/Plugins/Shared/` ise yalnız bir `.gitkeep` barındırıyor. Bu, `HADAL.Shared`'ın yerel bir junction/symlink ile bağlandığına ve o bağın **repoda olmadığına** işaret ediyor. Böyleyse temiz clone'da `Hadal.Core` ve `Hadal.Managers` derlenmez (ikisi de `HADAL.Shared` referans ediyor).
- **Aksiyon:** Bağlama yöntemi `README` veya bir editor script ile belgelenmeli; en temizi Shared'ı `Packages/com.hadal.shared` local package'ı yapmak.

---

## 4. Sınır koşulları kontrol listesi

| Sınır | Test eden senaryo | Beklenen |
|-------|-------------------|----------|
| 0 bina / hiç üretici yok | S2 | HUD sabit; boş `Amounts` cüzdanı **silmez** |
| Tek bina, tek tick | S5 | Tam olarak bir artış, tekrar yok |
| Cap dolu | S3 | Cap'te durur, cap sunucu otoritesinde |
| Cap = `long.MaxValue` (default, `ResourceDefinitionSO.cs:16`) | — | `old + amount` **overflow** etmemeli. `checked` veya `long.MaxValue - old` kontrolü gerekli |
| Negatif consumption | S4 | 0'a clamp; HUD negatif göstermez |
| Sıfır/negatif `BaseTickIntervalSeconds` | — | Config `0` ise `Tick` her frame ateşlenir (`ResourceManager.cs:52`) → sonsuz üretim. Config'e `[Min(0.05f)]` doğrulaması önerilir |
| Uzun offline (10 dk / 24 sa / 30 gün) | S7 | Client hesaplamaz; snapshot kapatır. 30 gün × saniyelik üretim `long`'u taşırmamalı |
| Saat ileri/geri alma, NTP kayması | S11 | Etkisiz |
| Tick anında bina yerleştirme | S5 | İkisi de uygulanır, ack kaybolmaz |
| Arka plandan dönüş | S7 | Tek frame'de tick patlaması yok |
| Reconnect'te snapshot + eski delta | S6 | Snapshot kazanır, geri sarma yok |
| Aynı delta'nın iki kez gelmesi | S8 varyantı | İdempotent (mutlak değer sayesinde) — `+=` semantiğine geçilirse **bozulur** |
| `ServerTick` geriye giden delta | S6 | Atılmalı (bugün atılmıyor) |
| Düşük pil (%20 altı, deşarj) | S10 | Üretim hızı değişmez |
| `Amounts` içinde bilinmeyen enum değeri (ör. 99) | — | `(ResourceType)99` cast'i **exception atmaz**, sessizce geçer (`ResourceManager.cs:113`). Bilinmeyen değer loglanıp atılmalı |
| `Payload` boş / bozuk byte dizisi | — | `TryDeserializePayload` `false` döner ve **sessizce atlanır** (`ClientStateViewMutator.cs:185-192`) — en az bir warning log'u olmalı |

---

## 5. Otomatik test önerisi (EditMode) — yazılmaya değer olanlar

**Önce altyapı:** `Assets/_Hadal/Tests/EditMode/Hadal.Tests.EditMode.asmdef`
Referanslar: `Hadal.Core`, `Hadal.Data`, `HADAL.Shared`, `ProtobufNet`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `precompiledReferences: ["nunit.framework.dll"]`; `includePlatforms: ["Editor"]`.
*(Not: `Hadal.Managers` çoğunlukla MonoBehaviour olduğu için EditMode'da zor; testleri saf sınıflara — `ClientStateViewMutator`, `ResourceWallet`, Shared formülleri — odaklayın.)*

Öncelik sırasıyla:

| # | Dosya | Ne kontrol eder | Hangi riski kapatır |
|---|-------|-----------------|---------------------|
| 1 | `ResourceTypeMappingTests.cs` | `HADAL.Shared.Enums.ResourceType`'ın her üyesi için `Hadal.Data.Enums.ResourceType`'a **açık** bir map var mı; round-trip kayıpsız mı; iki enum'un üye sayısı/isimleri beklenen sözleşmeye uyuyor mu (yeni üye eklenince **test kırılsın**) | **R1** — en kritik test |
| 2 | `EnvelopeDiscriminationTests.cs` | `StateDeltaEnvelope`/`StateSnapshotEnvelope` serileştirilip `TryDeserialize<HandshakeAckEnvelope>` denendiğinde **kabul edilmemeli**; ve `NetworkStateReceiver`'ın ayrıştırma sırası her üç frame tipini doğru sınıflandırmalı | **R2** |
| 3 | `ClientStateViewMutatorResourceTests.cs` | (a) Aynı `resource:` delta'sının iki kez uygulanması toplamı değiştirmez (idempotency); (b) partial delta dokunulmayan kaynakları **silmez** (upsert); (c) `Kind=Delete` yalnız hedef kaynağı siler; (d) bozuk `Payload` state'i bozmaz | **R5**, R3'ün çift sayım kolu |
| 4 | `ProductionFormulaTests.cs` | Shared üretim formülü: 0 bina → 0; cap clamp; negatif net → 0'a clamp; 30 günlük `elapsedSeconds` ile `long` overflow yok; aynı girdi → aynı çıktı (determinizm); formülün `UnityEngine`'e hiç bağımlılığı olmaması (assembly referans assert'i) | R7, R10, overflow |
| 5 | `StateDeltaOrderingTests.cs` | `ServerTick` geriye giden delta uygulanmaz; `BaselineTick` beklenenle uyuşmayan delta snapshot talep eder; snapshot sonrası eski delta state'i geri sarmaz | **R3**, R4 |
| 6 | `ResourceWalletTests.cs` | `Add` negatife inmez; `Set` 0'a clamp eder; `TrySpend` yetersiz bakiyede **hiçbir** kaynağı düşürmez (atomiklik — bugün `TrySpend` `CanAfford` sonrası yazıyor, `GameModels.cs:37-46`) | **R10** |
| 7 | `LatencyHarnessTests.cs` (PlayMode olabilir) | Ack/delta teslimatını 100 ms ve 200 ms geciktiren bir sahte transport ile prediction + rollback'in doğru sonuçlanması | **Roadmap 0-R exit kriteri** (`HadalDevelopmentRoadmap.md:37`) |

**Yazmaya değmeyenler (bilinçli olarak dışarıda):** HUD görsel testleri, `ScriptableObject` getter testleri, VContainer registration smoke testleri. Bunlar zaman yer, risk kapatmaz.

---

## 6. "Bu sprint'i bloklar" vs "bilinen kısıt, kabul et"

### 🔴 Bloklar — Sprint 3 kodu yazılmadan **önce** karara bağlanmalı

| Risk | Neden bloklar |
|------|---------------|
| **R1 — Çifte `ResourceType`** | Sprint 3'ün *tüm* çıktısı bu alandan geçiyor. Şimdi düzeltilmezse her HUD, her cap, her formül yanlış anahtarla yazılır ve sonradan düzeltmek tüm Sprint 3'ü geri almak demektir. Ayrıca kümeler farklı olduğu için "sonra map ederiz" ucuz değil. **Karar gereken:** Shared enum kanonik mi, yoksa açık `ResourceTypeMap` mi? |
| **R14 — `Hadal.UI` → `HADAL.Shared` referansı yok** | HUD yazılmadan önce çözülmeli; aksi halde geliştirici "int cast" kestirmesine zorlanır ve R1 kalıcılaşır. Tek satırlık asmdef düzenlemesi. |
| **R6 — İki doğruluk kaynağı (cüzdan vs view)** | HUD'un neyi okuyacağı Sprint 3'ün temel tasarım kararı. Yanlış seçim, sprint sonunda "bazen doğru bazen yanlış rakam" demektir. **Tavsiye:** HUD yalnız `ClientStateView`'dan beslensin. |
| **R8 — Tick ölçekleme kararı** | Simülatörün `TickManager`'a kayıt olup olmayacağı ilk satırda verilecek bir karar. Sonradan değiştirmek tüm zamanlama testlerini geçersiz kılar. **Tavsiye:** Kaydolmasın. |
| **R11 — Stub/gateway çift üretim** | Simülatörün flag'lenmesi kod yazılırken 5 dakika, sonradan 2 gün. |

### 🟡 Bu sprint'te düzeltilmeli ama kodu bloklamaz

- **R2 — Frame yutulması.** Sprint 3'ün stub'ı wire'ı bypass ederse iş yürür; ama S8 senaryosu **mutlaka koşulmalı** ve bulgu sprint içinde kapatılmalı. Gerçek gateway'e geçildiği gün bu bir P0'a dönüşür.
- **R5 — Partial delta silmesi.** Upsert'e çevirmek küçük bir değişiklik; ilk partial delta gönderildiği anda görünür hale gelir.
- **R3/R4 — Tick sırası ve kuyruk temizliği.** `LastAppliedServerTick` alanı + disconnect'te kuyruk temizliği; ikisi de küçük, ikisi de S6 ile doğrulanabilir.
- **R10 — Negatif bakiye.** Consumption Sprint 3'e giriyorsa bloklayıcıya yükselir; girmiyorsa bu sprintte düzelt yeter.
- **R16 — Test altyapısı.** En az §5'teki 1, 3 ve 4 numaralı testler bu sprintte yazılmalı; aksi halde Phase 0-R exit kriteri bir sprint daha ertelenir.

### 🟢 Bilinen kısıt — kabul et, kaydet, sonraya bırak

- **R7 — Client-side seeding.** Gerçek `StateSnapshot` sunucusu (M4/M5) gelene kadar stub'ın bir yerden başlangıç değeri üretmesi gerekiyor. **Kabul şartı:** Seeding kodu tek bir yerde ve açıkça `// TODO(M5): server snapshot replaces this` ile işaretli olsun; HUD'da "dev seed" göstergesi bulunsun. Forbidden Pattern #8'e girmemesi için **süreye bağlı hiçbir hesap yapmamalı** — sadece sabit başlangıç.
- **R9 — Arka planda tick durması.** Zaten istenen davranış (client offline hesaplamamalı). Snapshot yolu M5'te gelecek.
- **R12 — Sınırsız kuyruk.** Gerçek trafik hacmi bilinene kadar erken optimizasyon. Kaydet, Phase 2'de ölç.
- **R13 — Çifte `NetworkSerializationLayer`.** Davranışları neredeyse aynı; temizlik işi, risk düşük. Ayrı bir refactor commit'ine bırakılabilir.
- **R15 — `shared/` bağlantısı.** Tek geliştiricili repoda çalışıyor. CI kurulduğu gün bloklayıcıya yükselir; şimdilik S13 ile teyit + README notu yeterli.
- **`_pending` tek-komut penceresi** (`CommandReconciliationSystem.cs:25-27`). Sprint 3 resource odaklı; çok komutlu ack takibi Phase 4'ün (Buildings) işi. S5'in ikinci varyantıyla davranışı **belgeleyin**, düzeltmeyin.

---

## 7. Test oturumu önerisi (Session-Based Test Management)

Sprint 3 kodu yazıldıktan sonra, ~90 dakikalık üç charter:

| Charter | Süre | Kapsam | Çıktı |
|---------|------|--------|-------|
| **C1 — "Kaynak doğru kaynağa mı yazılıyor?"** | 60 dk | S1, S9, S2 · SFDPOT: Data | Enum map bulgusu net/kapalı |
| **C2 — "Zaman ve yeniden bağlanma ekonomiyi nasıl bozar?"** | 90 dk | S6, S7, S10, S11 · SFDPOT: Time + Platform | Tick sırası ve pil bulguları |
| **C3 — "Wire gerçekten çalışıyor mu?"** | 60 dk | S8, S12, S13 · SFDPOT: Structure | Frame ayrıştırma bulgusu |

Her charter sonunda: **ne test edildi / ne bulundu / ne test edilemedi (ve neden)** üç satır. Bulguları "bug" değil "risk" olarak kaydedin.

---

## 8. Kaynak dosya referansları

| Konu | Dosya:satır |
|------|-------------|
| Shared enum | `shared/HADAL.Shared/Enums/ResourceType.cs:6-14` |
| Data enum | `Assets/_Hadal/Scripts/Data/Enums/GameEnums.cs:3-11` |
| Enum yazımı (Shared → int) | `Assets/_Hadal/Scripts/Core/StateSync/ClientStateViewMutator.cs:123-135` |
| Enum yazımı (Data → int) | `Assets/_Hadal/Scripts/Managers/ResourceManager.cs:88-104` |
| Enum okuması (int → Data) | `Assets/_Hadal/Scripts/Managers/ResourceManager.cs:106-114` |
| Mutlak yazım / tam değiştirme | `Assets/_Hadal/Scripts/Core/StateSync/ClientStateViewMutator.cs:109-135` |
| Frame ayrıştırma sırası | `Assets/_Hadal/Scripts/Core/Network/NetworkStateReceiver.cs:143-167` |
| Handshake envelope şekli | `shared/HADAL.Shared/DTOs/HandshakeAckEnvelope.cs:9-12` |
| Delta envelope şekli | `shared/HADAL.Shared/DTOs/StateDeltaEnvelope.cs:8-11` |
| Tick sırası kontrolünün yokluğu | `Assets/_Hadal/Scripts/Core/Network/CommandReconciliationSystem.cs:64-83` |
| Disconnect işleme | `Assets/_Hadal/Scripts/Core/Network/NetworkStateReceiver.cs:103-106` |
| Tick ölçekleme | `Assets/_Hadal/Scripts/Managers/TickManager.cs:34-59` |
| Düşük pil çarpanı (0.5) | `Assets/_Hadal/Scripts/Data/Config/MobilePerformanceConfigSO.cs:20` |
| Tick aralığı (1.0s) | `Assets/_Hadal/Scripts/Data/Config/GameConfigSO.cs:15,44` |
| Cüzdan clamp tutarsızlığı | `Assets/_Hadal/Scripts/Data/Models/GameModels.cs:48-56` |
| Cap yalnız üst sınır | `Assets/_Hadal/Scripts/Managers/ResourceManager.cs:74-84` |
| Client-side seeding | `Assets/_Hadal/Scripts/Managers/ResourceManager.cs:41-47,58-68` |
| Stub simülatör kalıbı (wire bypass) | `Assets/_Hadal/Scripts/Core/Network/LocalPlacementAckSimulator.cs:39-46,117-124` |
| Koşulsuz simülatör kaydı | `Assets/_Hadal/Scripts/Managers/Bootstrap/ProjectLifetimeScope.cs:65,68` |
| Çifte serileştirme katmanı | `Assets/_Hadal/Scripts/Core/Network/NetworkSerializationLayer.cs:9` vs `shared/HADAL.Shared/Serialization/NetworkSerializationLayer.cs:7` |
| `Hadal.UI` referans eksiği | `Assets/_Hadal/Scripts/UI/Hadal.UI.asmdef:4-8` |
| Forbidden Patterns #1/#8 | `docs/HADAL_MASTER_DEVELOPMENT_GUIDE.md:93,100` |
| Açık 0-R exit kriteri | `docs/HadalDevelopmentRoadmap.md:37` |
| Sprint 3 tanımı | `docs/HadalDevelopmentRoadmap.md:67` |

---

*Bu belge bir kalite kapısı değil, bir risk haritasıdır. Kalite test edilerek üretilmez — test yalnızca kaliteyi görünür kılar.*
