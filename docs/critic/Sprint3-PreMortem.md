# Sprint 3 Pre-Mortem — "Resource Producer + Simulation Tick"

**Rol:** 首席怀疑官 (Chief Skeptic) · Inversion analizi
**Tarih:** 2026-09-25
**İncelenen plan:** Phase 1 Sprint 3 — ProductionFormulas + LocalProductionTickSimulator + mutlak kaynak replikasyonu + enum genişletme + client-side offline accrual

---

## 0. HÜKÜM

> **RET — planın 5 maddesinden 2'si (client-side offline accrual ve enum genişletme) doküman kurallarını doğrudan ihlal ediyor, 1'i (stub production tick) mevcut kodda zaten kanıtlanmış bir çift-ack hatasını para basan bir sisteme kopyalıyor, ve sprint'in kendisi Roadmap'te Phase 2 ile kelimesi kelimesine aynı işi tarif ediyor. Küçültülmüş hâliyle (sadece madde 1 + madde 3) KABUL edilebilir.**

Kısası: Sprint 3'ün *tamamını* yapmayın. İçinden sadece kalıcı olan parçayı alın, gerisi zaten çöp.

---

## 1. "Bu sprint 6 ay sonra başarısız oldu" — Otopsi Raporu

Tarih: 2027-03. Soft-launch öncesi ilk gerçek gateway entegrasyonu. Ne oldu?

### ÖLÜM NEDENİ #1 — Stub, gerçek server geldiğinde kapanmadı ve kaynakları iki kere bastı

**Somut senaryo:** M5 (Resource tick + StateDelta) server'ı devreye alındı. Oyuncular 4 saat boyunca gerçek üretimin iki katını aldı, çünkü `LocalProductionTickSimulator` hâlâ `ProjectLifetimeScope` içinde `RegisterEntryPoint` ile kayıtlıydı ve `EnqueueInbound`'a kendi delta'sını atmaya devam etti. Server delta'sı ile stub delta'sı aynı `ClientStateView`'a düştü. Ekonomi 4 saatte 11 gün ileri gitti. Rollback yapıldı, oyuncular kaybetti, review bombası yendi.

**Bu hayali değil — desen zaten bozuk.** `LocalPlacementAckSimulator` bugün **koşulsuz** çalışıyor:

- `CommandDispatcher.cs:100-104` — `SendWire()` gerçek WebSocket'e gönderdikten **sonra** `TrySimulatePlacementAck(command)` çağırıyor. Yani handshake tamamlanmış ve gerçek gateway bağlıyken bile stub ack üretiliyor.
- `CommandDispatcher.cs:59-65` — buffered (handshake öncesi) yolda da çağırıyor.
- Yani **her iki yolda da** stub ateşliyor. `#if UNITY_EDITOR` yok, `GameConfigSO` flag'i yok, `IGatewaySessionState` kontrolü yok.
- `LocalPlacementAckSimulator.cs:39-46` — `Start()` içinde `_session.CompleteHandshake()` çağırıyor; yani stub, gerçek gateway'in handshake'ini gasp ediyor.
- `ProjectLifetimeScope.cs:65,68` — koşulsuz `Register` + `RegisterEntryPoint`.

Bina yerleştirmede bu "sadece görsel bir duplicate" olur. **Kaynakta bu paradır.**

**Plan bunu şu an karşılıyor mu?** **HAYIR.** Planın 2. maddesi açıkça "Sprint 2'deki deseni taklit et" diyor — yani bu kusuru da taklit edecek. Kanıt: `Assets/_Hadal/Scripts/Core/Network/CommandDispatcher.cs:100-110`, `Assets/_Hadal/Scripts/Managers/Bootstrap/ProjectLifetimeScope.cs:65-68`.

---

### ÖLÜM NEDENİ #2 — İki ayrı `ResourceType` enum'ı sessizce yanlış eşleşti; oyuncu Oxygen yerine Energy aldı

**Somut senaryo:** QA "Oxygen üretimi çalışıyor" dedi, HUD'da Energy arttı. Kimse fark etmedi çünkü ikisi de sayı. 6 ay sonra ekonomi dengeleme tablosu tutmadı, telemetri ile server tablosu %100 farklı çıktı. Sebep bulunana kadar 3 hafta geçti.

**Bu bug ZATEN KODDA VAR.** Şu an, bugün:

| Kaynak | Dosya:satır | Ne yazıyor |
|---|---|---|
| Yazıcı A (server→view) | `Core/StateSync/ClientStateViewMutator.cs:128-134` | `resourceType = (int)pair.Key` — `pair.Key` **HADAL.Shared.Enums.ResourceType** |
| Yazıcı B (local wallet→view) | `Managers/ResourceManager.cs:96-102` | `resourceType = (int)pair.Key` — `pair.Key` **Hadal.Data.Enums.ResourceType** |
| Okuyucu | `Managers/ResourceManager.cs:113` | `(ResourceType)entry.resourceType` — **Hadal.Data.Enums.ResourceType** olarak okuyor |

Aynı `int` alanına iki farklı alfabeyle yazılıyor, tek alfabeyle okunuyor. Mevcut değerler:

```
Hadal.Data.Enums.ResourceType   (GameEnums.cs:3-11)  : Oxygen=0 Energy=1 Food=2 Biomass=3 Titanium=4 Hadalite=5
HADAL.Shared.Enums.ResourceType (ResourceType.cs:8-13): Unknown=0 Oxygen=1 Food=2 Biomass=3 Research=4 Alloy=5
```

Bugünkü fiilî çeviri tablosu (server delta → ResourceManager wallet):

| Server gönderir | Client'ta olur | Doğru mu |
|---|---|---|
| Oxygen (1) | **Energy** | ✗ |
| Food (2) | Food | ✓ (tesadüf) |
| Biomass (3) | Biomass | ✓ (tesadüf) |
| Research (4) | **Titanium** | ✗ |
| Alloy (5) | **Hadalite** | ✗ |
| — | Data.Oxygen(0) → Shared.**Unknown** | ✗ |

İki tesadüfi doğru eşleşme bu hatayı **saklıyor**. Sprint 3 kaynakları ilk kez gerçekten hareket ettirecek, yani bu bomba tam Sprint 3'te patlıyor.

Ve planın 4. maddesi (`Energy=6, Titanium=7, Hadalite=8` ekle + `ResourceTypeMap` yaz) **bu üç satırı düzeltmiyor**. `ResourceTypeMap` yazmak, `ClientStateViewMutator.cs:130`'daki ham `(int)` cast'i orada bıraktığın sürece hiçbir işe yaramaz. Map yazılır, kullanılmaz, herkes "hallettik" sanır. Bu, planın en tehlikeli maddesi — çünkü *güvenlik hissi* veriyor.

**Plan bunu şu an karşılıyor mu?** **HAYIR.** Plan 3 ham cast noktasından hiçbirini isimlendirmiyor: `ClientStateViewMutator.cs:130`, `ResourceManager.cs:98`, `ResourceManager.cs:113`.

---

### ÖLÜM NEDENİ #3 — İki ayrı kaynak deposu birbirini ezdi; server state'i client tarafından silindi

**Somut senaryo:** Oyuncu 40 dakika offline kalıp döndü. Server snapshot 12.400 Oxygen gönderdi. `SaveManager.SyncNow()` çağrıldı (herhangi bir autosave/scene geçişi), `ResourceManager.CaptureSave()` çalıştı ve `data.resources`'ı **local wallet'ın içeriğiyle komple ezdi**. Server'ın gönderdiği 12.400 gitti, yerine client'ın 300'ü yazıldı. Oyuncu kaynağını kaybetti. Support ticket dağı.

**Kanıt zinciri:**

1. `ResourceManager.cs:21` — `private readonly ResourceWallet _wallet = new();` → **birinci kaynak deposu**, manager içinde, authoritative gibi davranıyor.
2. `Core/StateSync/ClientStateView.cs:10` — `SaveGameData _data` → **ikinci kaynak deposu**, `data.resources`.
3. `Managers/StateSync/StateSyncService.cs:73-79` — `CaptureAll()` her `ISaveParticipant`'a `_view.Data`'yı verip **üzerine yazdırıyor**.
4. `ResourceManager.cs:88-104` — `CaptureSave()` `data.resources`'ı komple değiştiriyor (append değil, replace).
5. `ResourceManager.cs:41-47` — `_wallet` sadece **boot'ta bir kez** `RestoreSave` ile dolduruluyor; server delta'sı geldiğinde `_wallet` **hiç güncellenmiyor**. `ApplyResourceChange` (`ClientStateViewMutator.cs:109-121`) sadece `data.resources`'a yazıyor, wallet'a değil.
6. `ResourceManager.cs:49-56` — `Tick()` boş. Yani senkronizasyon hiç yapılmıyor.
7. `ResourceManager.cs:58-68` — `SeedStartingResources()` client'ta kaynak **yaratıyor**. `!HasPersistedData` koşuluna bağlı (`ResourceManager.cs:43`), ama `HasPersistedData` `MarkCaptured()` ile de set ediliyor (`ClientStateView.cs:30`) — yani sıralamaya bağımlı, kırılgan.

Bu Forbidden Pattern **#5 (Singleton managers with authoritative state)** ve **#10 (Implicit global state)**'in ders kitabı örneği. Sprint 3'ün ilk işi bu ikiliği kapatmak olmalı, kaynak üretmek değil.

**Plan bunu şu an karşılıyor mu?** **HAYIR.** Plan `ResourceManager._wallet`'ın kaderinden hiç bahsetmiyor. `ResourceStateDto`'yu mutlak toplam olarak replike etmek (madde 3) doğru bir karar, ama iki depo varken mutlak replikasyon sadece "hangi depo son yazdıysa o kazanır" yarışı yaratır.

---

## 2. Altı Soruya Net Cevaplar

### S1 — Client içinde "stub server" yazmak Forbidden #1 ve #8'i ihlal ediyor mu? Sprint 2'de kabul edildiyse Sprint 3'te neden farklı?

**Forbidden #1 ("Client gameplay JSON save as truth"): HAYIR, ihlal etmiyor.** Bu madde spesifik olarak *diske JSON yazmayı* yasaklıyor. Kod temiz: `ClientStateView.cs:5-6` "RAM only — never written to disk", `SaveManager.cs` sadece `CaptureAll` çağırıyor, repoda tek bir `persistentDataPath` kullanımı yok. Bu kutu işaretlenmiş durumda, doğru.

**Ama ihlal ettiği şey daha kötü: Principle #1 ("Server is single source of truth") ve Forbidden #5.** Stub, client process'i içinde authoritative state üretiyor. JSON'a yazmıyor diye sorun olmaktan çıkmıyor.

**Forbidden #8 ("Offline reward calculation on client"): planın 5. maddesi bunu KELİMESİ KELİMESİNE ihlal ediyor.** "Offline accrual'ı Shared formülüyle, stub içinde hesaplamak" = client'ta offline ödül hesabı. Formülün Shared'da olması hiçbir şeyi değiştirmez; hesabı **kim çalıştırıyor** önemli. Roadmap bunu ayrıca ikinci kez yasaklıyor: `HadalDevelopmentRoadmap.md:87` — *"Offline reconciliation on reconnect | StateSnapshot — **NOT client offline calc**"*. Büyük harfle yazılmış. Bunu yapmak, dokümanı okumamış olmayı değil, okuyup umursamamayı gösterir.

**Sprint 2 vs Sprint 3 farkı — ve bu fark ciddi:**

| | Sprint 2 stub | Sprint 3 stub |
|---|---|---|
| Ne üretiyor | Deterministik ack (`IsSlotAvailable` — `LocalPlacementAckSimulator.cs:73`) | **Zaman fonksiyonu olarak değer** |
| Tetikleyici | Oyuncu komutu (talep üzerine) | Periyodik timer (**kendi kendine**) |
| Yanlışsa sonucu | Bina yanlış yerde görünür, delta gelince düzelir | **Oyuncunun cüzdanı yanlış olur** |
| Gerçek server gelince | Duplicate bina — görsel, düzeltilebilir | **Duplicate para — ekonomi kalıcı bozulur** |
| Exploit yüzeyi | Yok (server zaten reddedecek) | **Cihaz saatini ileri al = sonsuz kaynak** |

Sprint 2'de kabul edilmesi, Sprint 3'te kabul edilmesini gerektirmez. Bu **承诺一致性偏差** (commitment/consistency bias) — "deseni zaten kabul ettik" argümanı, desenin *neyi* ürettiğini görmezden geliyor. Bir ack simülatörü ile bir para basma makinesi aynı şey değildir.

**Ayrıca somut exploit:** `LocalPlacementAckSimulator.cs:53,62` `Time.unscaledTime` kullanıyor — bu bir uygulama-ömrü sayacı, offline hesabı için işe yaramaz. Offline accrual için kaçınılmaz olarak `DateTime.UtcNow` / `System.DateTime` kullanacaksınız. Mobilde bu **cihaz saati**. Uçak modu → saati 1 ay ileri al → uygulamayı aç → bir aylık üretim. Sıfır çaba, root gerekmez. Ve `#if` koruması olmadığı için bu kod **production build'e girer**.

---

### S2 — Client-side stub'ın kaynak basması nasıl bir teknik borç yaratır? Bu borç ne zaman patlar?

**Borcun anatomisi (üç katman):**

1. **Kod borcu (küçük):** Stub sınıfının kendisi. Silinir, biter. Bu sorun değil.
2. **Entegrasyon borcu (orta):** Stub ile server'ın **aynı anda çalıştığı** dönem. Yukarıda kanıtladığım gibi mevcut desen stub'ı kapatmıyor. Bu, gateway entegrasyon gününde çift-üretim demek.
3. **Veri borcu (öldürücü, geri döndürülemez):** QA'nın, dengeleme ekibinin, ve varsa erken testçilerin stub'dan gelmiş kaynakları. Bunlar server'ın DB'sinde yok. Migration yazmak zorunda kalırsınız veya hesapları wipe edersiniz.

**Ne zaman patlar — takvim:**

- **T+0 (sprint sonu):** Hiçbir şey. Herkes mutlu. HUD'da sayılar artıyor.
- **T+2 hafta:** Dengeleme ekibi stub'ın ürettiği rakamlara göre ekonomi tunelı yapar. **Artık stub'ın davranışı bir spesifikasyon oldu** — ve bu spesifikasyon hiçbir yerde yazılı değil, sadece `LocalProductionTickSimulator.cs` içinde.
- **T+1 ay:** Biri Unity Editor'de saati değiştirir, milyonlarca Hadalite alır, "bug" diye bildirir. Kapatılır ("stub, önemli değil"). Bu **uyarı atışı**, kaçırılır.
- **M5 entegrasyon günü — ASIL PATLAMA:** Server production tick'i açılır. Stub hâlâ kayıtlıdır (`ProjectLifetimeScope` deseni bunu garanti ediyor). İki `EnqueueInbound` kaynağı aynı `CommandReconciliationSystem` kuyruğuna (`CommandReconciliationSystem.cs:43-49`) yazar. Mutlak toplam replikasyonu sayesinde son yazan kazanır → HUD titrer, sayılar zıplar. Ya da stub'ın delta'sı server'ınkinin üzerine biner.
- **Soft-launch:** Aynı hata gerçek parayla. Geri dönüş yok.

**Ek bir borç kalemi, kimse konuşmuyor:** `LocalPlacementAckSimulator.cs:83-84` — `ServerTick = 1, BaselineTick = 0` **sabit kodlanmış**. Delta sıralaması yok. Bir üretim tick'i simülatörü bu deseni kopyalarsa, her tick "tick 1" der; `CommandReconciliationSystem` sıralamayı kontrol etmez, `ProcessQueue` (`CommandReconciliationSystem.cs:64-83`) geldiği sırayla uygular. Yani out-of-order delta koruması **sıfır**. Gerçek server geldiğinde baseline/tick protokolü yazmak zorundasınız ve stub bu konuda size hiçbir şey öğretmemiş olacak — hatta yanlış öğretmiş olacak. Roadmap `HadalDevelopmentRoadmap.md:211` bunu "Delta compression | MEDIUM | Baseline tick protokolü" diye risk kaydına almış. Stub bu riski **azaltmıyor, gizliyor**.

---

### S3 — `ResourceType`'ı genişletmek mi, `Hadal.Data.Enums.ResourceType`'ı silip her yerde Shared mi?

**SİLİN. Tereddütsüz. `Hadal.Data.Enums.ResourceType` gitmeli.**

Bu Forbidden Pattern **#3 — "Duplicate command or state types outside HADAL.Shared"** ve `ResourceType` bir state type'tır. Doc 21 satır 82 `ResourceType`'ı açıkça Shared enum listesinde sayıyor. Roadmap exit criteria `HadalDevelopmentRoadmap.md:35` **"[x] Zero duplicate types outside HADAL.Shared"** diye işaretlenmiş — **bu kutu yanlış işaretlenmiş.** Duplicate şu anda duruyor, `GameEnums.cs:3-11`.

**"Genişlet + ResourceTypeMap yaz" neden yanlış cevap:**

1. **İki enum'u kalıcılaştırır.** Map yazmak = duplicate'i resmîleştirmek. Forbidden #3 bir daha asla kapanmaz.
2. **Map bijektif olamaz.** Shared'da `Research` ve `Alloy` var, Data'da yok. Data'da `Oxygen=0` var, Shared'da 0 `Unknown`. Her `ResourceTypeMap.ToShared(Data.Oxygen)` çağrısı ya throw eder ya `Unknown` döner. İkisi de production'da sessiz veri kaybı.
3. **Map, hatanın yaşadığı yerde kullanılmayacak.** Kanıt: `ClientStateViewMutator.cs:130` ve `ResourceManager.cs:98,113` zaten ham `(int)` cast kullanıyor ve plan bu satırlara dokunmuyor. Map var olacak, bug da var olacak.
4. **Maliyet argümanı çürük.** "Silmek daha büyük değişiklik" deniyor. Ölçtüm: `Hadal.Data.Enums.ResourceType`'a bağlı kullanım yüzeyi **dar** — `ResourceWallet` / `ResourceAmount` (`Data/Models/GameModels.cs:13-25`), `ResourceManager`, `ResourceDatabaseSO`, `ResourceChangedEventSO`, `BuildingManager`, `ExpeditionManager`. Altı-yedi dosya. Ve **UI'da tek bir kullanım yok** — `Assets/_Hadal/Scripts/UI/` altında `Resource` geçen tek dosya yok. Yani HUD'u Shared enum ile yazmanın ekstra maliyeti **sıfır**, çünkü HUD henüz yok.

**Ama bir uyarı:** `Hadal.UI.asmdef:4-8` `HADAL.Shared`'a referans vermiyor. Shared enum'ı UI'da kullanmak için oraya eklenmesi lazım. Bir satır. Bunu şimdi yapmak, sonra 40 dosyada map çağrısı temizlemekten ucuz.

**Wire değerleri konusunda plan haklı, ama nedeni yanlış:** Mevcut Shared değerlerini (1-5) bozmamak doğru. Yeni değerleri 6,7,8 olarak eklemek de doğru. Sorun eklemek değil, **Data enum'ını hayatta bırakmak**.

---

### S4 — Phase 1 Sprint 3 ile Phase 2 aynı işi tarif ediyor. Bu sprint çöpe atılacak iş mi?

**Bu bir tasarım hatası — evet.** Yan yana koyalım:

| | Phase 1 Sprint 3 (`HadalDevelopmentRoadmap.md:67`) | Phase 2 (`HadalDevelopmentRoadmap.md:84-87`) |
|---|---|---|
| Server | "Production tick (Shared formulas) · StateDelta for resources" | "Production/consumption tick (Shared formulas)" |
| Client | "Resource HUD from replicated deltas" | "HUD from replicated deltas" |
| Offline | "offline reconnect snapshot" | "Offline reconciliation on reconnect / StateSnapshot — **NOT client offline calc**" |

Aynı satırlar. Tek fark: Phase 2 offline konusunda **açık bir yasak** içeriyor, Sprint 3 satırı içermiyor. Ve ekip lideri planını **yasağı içermeyen** satıra dayandırmış. Bu tesadüf olabilir ama sonuç aynı: dokümanın daha gevşek kopyasına göre plan yapılmış.

**Çöpe atılacak mı? Parça parça cevap — ve bu ayrım sprint'in kaderini belirliyor:**

| Parça | Kader | Neden |
|---|---|---|
| `ProductionFormulas.cs` (Shared, saf statik) | **KALICI** | Server bunu aynen `using` edecek. Doc 21:82 + Principle #3. Sıfır çöp. |
| `ResourceStateDto` mutlak replikasyon | **KALICI** | Wire kontratı. Zaten var (`shared/HADAL.Shared/DTOs/ResourceStateDto.cs`). |
| Shared enum genişletmesi | **KALICI** (Data enum silinirse) | Wire kontratı. |
| Resource HUD (delta'dan okuyan) | **KALICI** | Client view katmanı, server değişse de aynı. |
| `LocalProductionTickSimulator` | **ÇÖP** | M5 gelince silinecek. |
| Client-side offline accrual | **ÇÖP + ZARAR** | Silinecek *ve* arkasında bozuk veri bırakacak. |

Yani sprint'in **yaklaşık %60'ı kalıcı, %40'ı çöp — ve çöp olan %40 tehlikeli olan kısım.** Bu, sprint'i iptal etmek için değil, **kesip atmak** için bir sebep.

---

### S5 — Bu sprint'i hiç yapmamak, doğrudan gerçek gateway/server'a geçmek daha mı akıllıca?

**Hayır — ama sprint'i planlandığı gibi yapmak da akıllıca değil. Üçüncü bir yol var ve doğru cevap o.**

**"Doğrudan gerçek server'a geç" neden yanlış:**

- Repoda backend **yok**. Ne `.csproj`, ne `Server/`, ne `Dockerfile`. Sadece `shared/HADAL.Shared` (bir Unity paketi, `Packages/manifest.json:10`). ASP.NET Core + PostgreSQL + Redis + gateway kurmak (`HADAL_MASTER_DEVELOPMENT_GUIDE.md:146-158`) haftalarca iştir ve bu süre boyunca client tarafında **hiçbir şey ilerlemez**.
- M4 (PlaceBuilding + StateSnapshot) bile gerçek değil — Sprint 2 stub ile geçti. M5'e atlamak, M4'ü atlamak demek. Roadmap critical path `HadalDevelopmentRoadmap.md:185` M2 → M4 → M7 diyor.
- Bu, "sunk cost'u kurtarmak için daha büyük bir şey inşa etmek" tuzağının aynadaki hâli: **锤子综合症** — elimizde "gerçek server yazma" çekici var diye her şeyi çivi görmek.

**"Sprint'i planlandığı gibi yap" neden yanlış:** Yukarıdaki 3 ölüm nedeni.

**Doğru yol — Sprint 3'ü İKİYE BÖL:**

**Sprint 3a — "Kontrat ve Temizlik" (stub yok, kaynak üretimi yok):**
1. `HADAL.Shared/Simulation/ProductionFormulas.cs` — saf statik, `UnityEngine` referansı yok, `DateTime.Now` yok (zaman **parametre** olarak girer).
2. `Hadal.Data.Enums.ResourceType`'ı **sil**. Her yerde `HADAL.Shared.Enums.ResourceType`. `Hadal.UI.asmdef`'e `HADAL.Shared` ekle.
3. `ClientStateViewMutator.cs:130`, `ResourceManager.cs:98`, `ResourceManager.cs:113` — ham `(int)` cast'leri kaldır.
4. `ResourceManager._wallet`'ı **sil**; `IResourceService.Get()` doğrudan `_stateSync.View.Data.resources`'dan okusun. `ResourceManager`'ı `ISaveParticipant` olmaktan çıkar (`ResourceManager.cs:16`) — böylece `CaptureAll` artık server state'ini ezemez.
5. `SeedStartingResources()`'ı **sil** (`ResourceManager.cs:58-68`). Başlangıç kaynağı server'ın işi.
6. Resource HUD yaz — **sadece** `ClientStateView`'dan okur, hiçbir şey yazmaz.
7. `ProductionFormulas` için unit test. Bu, sprint'in gerçek teslimatıdır ve server ekibi bunu aynen kullanır.

Sonuç: HUD var, formüller var, tek bir enum var, tek bir kaynak deposu var. **Hiçbir şey çöp değil.** Ve HUD test edilebilir — `StateSnapshot`'ı elle besleyen bir editor test hook'u yeter (bu, "stub server" değil, test fixture'ıdır; Update loop'ta çalışmaz, build'e girmez).

**Sprint 3b — sadece gerçek gateway hazırsa.** Production tick server'da yazılır. Offline accrual **hiç client'ta yazılmaz** — reconnect'te `StateSnapshot` gelir, bu kadar.

**Bonus — Sprint 3a'ya girmeden önce yapılması gereken 1 saatlik iş:** `LocalPlacementAckSimulator`'ı bir bayrağın arkasına al. `IGatewaySessionState` gerçek bir sunucuya bağlıysa veya `GameConfigSO.UseStubGateway == false` ise hiç kayıt edilmesin (`ProjectLifetimeScope.cs:65-68`). Bu, Ölüm Nedeni #1'i **bugün** kapatır ve bedeli neredeyse sıfırdır. Bunu yapmadan Sprint 3'e girmek, aynı hatayı iki yerde çoğaltmak demektir.

---

### S6 — asmdef referanslarında sorun var mı?

**Evet, iki tane. Biri döngü, biri eksik referans.**

**(a) Dairesel referans — `Hadal.Core` ⇄ `Hadal.Data`**

```
Assets/_Hadal/Scripts/Core/Hadal.Core.asmdef:4-10  → references: ["Hadal.Data", "HADAL.Shared", "ProtobufNet", ...]
Assets/_Hadal/Scripts/Data/Hadal.Data.asmdef:4-6   → references: ["Hadal.Core"]
```

Unity asmdef döngülerini reddeder ("Assembly with cyclic references detected"). Ve `Hadal.Data` bu referansı **hiç kullanmıyor**: `Assets/_Hadal/Scripts/Data/` altında `Hadal.Core` geçen tek şey asmdef'in kendisi — tek bir `using Hadal.Core...` yok.

Yani `Hadal.Data.asmdef:5`'teki `"Hadal.Core"` satırı **ölü ve zararlı**. Silinmeli. Doğru yön tek taraflı: `Hadal.Core → Hadal.Data` (`Core` gerçekten `Hadal.Data.Models` kullanıyor — örn. `LocalPlacementAckSimulator.cs:8`, `ClientStateViewMutator.cs:7`, `ClientStateView.cs:1`).

**Not:** Bu bağımlılık yönü kendisi de sorgulanmalı. `Hadal.Core.StateSync` neden `Hadal.Data.Models.SaveGameData`'ya bağımlı? Çünkü `ClientStateView` bir "save" tipini runtime state olarak kullanıyor (`ClientStateView.cs:10`). Adı "SaveGameData" olan bir tipi authoritative-olmayan runtime cache olarak kullanmak, Forbidden #1'e doğru kayan bir isimlendirme. Sprint 3 kapsamında değil ama not edilmeli.

**(b) Eksik referans — `Hadal.Core` VContainer'ı kullanıyor ama referans vermiyor**

`Hadal.Core.asmdef:4-10` referans listesinde **VContainer yok**, ama dört dosya `using VContainer...` yapıyor:
- `Core/Network/LocalPlacementAckSimulator.cs:10` (`VContainer.Unity` — `IStartable, ITickable`)
- `Core/Network/CommandReconciliationSystem.cs:7`
- `Core/Network/NetworkStateReceiver.cs`
- `Core/Network/NativeWebSocketClient.cs`

Karşılaştırın: `Hadal.Managers.asmdef:10` VContainer'ı **listeliyor**. VContainer bir git paketi (`Packages/manifest.json:13`), repo içinde kaynak olarak yok.

Sonuç: `Hadal.Core` ya derlenmiyor, ya asmdef'ler senkronize değil. Her iki durumda da Sprint 3'ün ilk işi bunu doğrulamak olmalı — çünkü plan `LocalProductionTickSimulator`'ı **aynı assembly'ye, aynı `ITickable` arayüzüyle** koymayı öngörüyor.

---

## 3. ŞUNU YAPMAYIN — Kesin Liste

1. **Offline accrual'ı client'ta hesaplamayın.** Ne stub içinde, ne "geçici olarak", ne `#if UNITY_EDITOR` arkasında. Forbidden #8 + `HadalDevelopmentRoadmap.md:87` ("NOT client offline calc"). Reconnect'te `StateSnapshot` uygulanır, o kadar. `ClientStateViewMutator.ApplySnapshot` (satır 16-31) bunun için zaten hazır.

2. **`DateTime.UtcNow` / `DateTime.Now`'u client kodunda kaynak hesabına sokmayın.** Cihaz saati oyuncunun kontrolündedir. Bir kere girerse çıkarmak yıllar sürer.

3. **`ResourceTypeMap` yazmayın.** Duplicate'i çözmez, resmîleştirir. `Hadal.Data.Enums.ResourceType`'ı silin (`GameEnums.cs:3-11`).

4. **`LocalProductionTickSimulator`'ı koşulsuz register etmeyin.** `ProjectLifetimeScope.cs:65-68` desenini kopyalamayın. Minimum: `GameConfigSO` flag'i + `#if !UNITY_EDITOR` dışlaması + `IGatewaySessionState` gerçek bağlantı kontrolü. Üçü birden.

5. **`ServerTick = 1` sabitini kopyalamayın** (`LocalPlacementAckSimulator.cs:83`). Üretim tick'i monoton artan tick numarası üretmeli, yoksa out-of-order koruması test edilemez.

6. **`ResourceManager._wallet`'ı yaşatmayın.** İki kaynak deposu = kesin divergence. `_wallet`'ı (`ResourceManager.cs:21`) silin, `ClientStateView` tek kaynak olsun.

7. **`ResourceManager`'ı `ISaveParticipant` olarak bırakmayın** (`ResourceManager.cs:16`). `CaptureAll` (`StateSyncService.cs:73-79`) server'dan gelen kaynakları client wallet'ıyla eziyor.

8. **`SeedStartingResources()`'ı Sprint 3'e taşımayın** (`ResourceManager.cs:58-68`). Client kaynak yaratmamalı. Başlangıç cüzdanı `StateSnapshot`'tan gelir.

9. **`ProductionFormulas`'a `UnityEngine` referansı sokmayın.** `shared/HADAL.Shared` .NET Standard 2.1; `Time.deltaTime`, `Mathf`, `Random` yasak. Saf fonksiyon = `(girdi, geçenSüre) → çıktı`.

10. **"Sprint 2'de kabul ettik" argümanını kullanmayın.** Ack simülatörü ile para basma makinesi farklı risk sınıfları. Bir kez doğru olan bir kararı ikinci kez otomatik doğru saymak sunk cost'un kendisidir.

11. **Roadmap exit criteria kutularını düzeltmeden devam etmeyin.** `HadalDevelopmentRoadmap.md:35` "Zero duplicate types outside HADAL.Shared" **[x]** işaretli ama `Hadal.Data.Enums.ResourceType` duruyor. Yanlış yeşil ışık, gerçek kırmızı ışıktan tehlikelidir.

---

## 4. Ters Kontrol Listesi

**Bu daha basit yapılabilir mi?** Evet. Sprint 3a (7 madde, hepsi kalıcı) planlanan sprint'ten daha küçük ve daha değerli. Karmaşıklık — `ResourceTypeMap`, iki enum, stub tick, offline accrual — problemin özünü anlamamayı örtüyor: **iki kaynak deposu ve iki enum var.** Önce bunları teke indirin; ondan sonra kaynak üretmek üç saatlik iş.

**Gerçek problemi mi çözüyoruz?** Kısmen. Gerçek problem "kaynak üretimi yok" değil, "client hâlâ kendi authoritative state'ini tutuyor". Sprint 3, ikinci problemin üstüne birinciyi inşa etmeye çalışıyor.

**Görmezden gelinen ters kanıt var mı?** Evet, üç tane: (a) `ClientStateViewMutator.cs:130` ↔ `ResourceManager.cs:113` arasındaki enum uyuşmazlığı zaten canlı bir bug; (b) `CommandDispatcher.cs:103` stub'ı gerçek gönderimden sonra da çağırıyor; (c) `Hadal.Data.asmdef:5`'teki döngüsel referans. Üçü de Sprint 2 "tamamlandı" işaretlenirken gözden kaçmış.

**En kötü senaryo nedir?** Soft-launch'ta ekonominin bozuk olduğunun anlaşılması ve wipe. Bir strateji MMO'sunda oyuncu ilerlemesini sıfırlamak, oyunu kapatmakla eşdeğerdir.

---

**İmza:** 首席怀疑官 · "Hangi durumda başarısız oluruz?" sorusuna cevabınız yoksa, henüz plan yapmamışsınız demektir.
