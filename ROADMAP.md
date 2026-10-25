# HADAL — Proje Yol Haritası

**Güncelleme:** 2026-10-24
**Kapsam:** İlk satır koddan global lansmana kadar tüm aşamalar.
**Kaynaklar:** Tasarım `docs/01–22`, mimari `docs/17`, `docs/18`, `docs/21`, altyapı `docs/HADAL_SERVER_INFRASTRUCTURE.md`.
Ayrıntılı sprint geçmişi: [docs/HadalDevelopmentRoadmap.md](docs/HadalDevelopmentRoadmap.md).

Durum işaretleri: ✅ tamam · 🟡 devam ediyor · ⬜ başlanmadı

---

## Değişmez kurallar (her aşama için geçerli)

1. **Sunucu otoritelidir.** Her oyun eylemi `Command → sunucu doğrular → StateDelta → istemci görünümü` akışından geçer. İstemci ekonomi, RNG veya offline kazanç hesaplamaz.
2. **Tek kaynak: HADAL.Shared.** Komutlar, DTO'lar, enum'lar ve formüller yalnızca orada tanımlanır. Wire formatı Protobuf'tur.
3. **Her aşama, çalışan testlerle kapanır.** Testler EditMode, PlayMode ve sunucu entegrasyon testleridir. "Kod yazıldı" bir aşamayı kapatmaz.
4. **İzole altyapı.** Tüm backend DL380 üzerindeki CT 160 (`hadal`) içindedir. Diğer projelere dokunulmaz.

---

## Genel bakış

| # | Aşama | Kilometre taşı | Durum |
|---|-------|----------------|-------|
| 0 | Temel, mimari ve ilk dikey dilim | Shared protokol, grid inşa, kaynak tick (stub), test altyapısı | ✅ |
| 1 | Otoriter oyun sunucusu çekirdeği | İstemci gerçek sunucuya karşı oynar; stub'lar kalkar (M3–M5) | ✅ |
| 2 | **Dış erişim, kimlik ve dağıtım** | `hadal-api.cicibyte.com`, TLS, Auth API, patch, dağıtım (M1–M2) | 🟡 |
| 3 | Üs inşasının tamamlanması | Kaldırma, yükseltme, halka açma, mega kubbe, tam katalog | ⬜ |
| 4 | Hayatta kalma döngüsü | Oksijen/enerji bağımlılıkları, acil durumlar, alarmlar | ⬜ |
| 5 | Sunum katmanı | Ana menü, UI tasarım sistemi, sualtı görünümü, ses | ⬜ |
| 6 | Denizaltılar | Filo, gövde/basınç istatistikleri, üretim | ⬜ |
| 7 | Basınç, keşif seferleri ve sonar | Derinlik katmanları, sefer örnekleri, sonar bölgeleri | ⬜ |
| 8 | Kahramanlar | Sınıflar, fraksiyonlar, seviye, yetenekler | ⬜ |
| 9 | Düşmanlar ve savaş | Sunucu hakemli savaş, düşman türleri | ⬜ |
| 10 | Dünya haritası | Paylaşılan dünya, derinlik halkaları, shard'lar | ⬜ |
| 11 | Alliance | İttifak, ortak yapılar, Trade Station, alliance şehri | ⬜ |
| 12 | PvP ve kuşatmalar | Saldırı/savunma, anti-cheat denetim kaydı (M6) | ⬜ |
| 13 | Monetizasyon | Hadalite, Cryo-Pod ve Sonar Ping gacha, mağaza, makbuz doğrulama | ⬜ |
| 14 | LiveOps — The Overseer | Admin API, GM paneli, audit, 2FA, gacha hot-swap (M7–M9) | ⬜ |
| 15 | Leviathan'lar | Raid örnekleri, ittifak boss içeriği | ⬜ |
| 16 | THE CORE | Son oyun, organik reveal, dünya olayı | ⬜ |
| 17 | Soft launch hazırlığı | Performans, yük testi, analitik, yerelleştirme, mağaza uyumu | ⬜ |
| 18 | Lansman ve canlı operasyon | Global yayın, etkinlik takvimi, sezonlar | ⬜ |

**Paralel izler:**
- **Phase 14 (Overseer):** Phase 2'deki kimlik sistemi bitince başlayabilir.
- **Phase 5 (sunum):** Phase 3'ten itibaren oynanış işleriyle paralel yürür.

**Kritik yol:** 1 → 2 → 3 → 4 → 7 → 13 → 14 → 17.

---

## Phase 0 — Temel, mimari ve ilk dikey dilim ✅

**Hedef:** Üretime uygun MMO iskeleti ve uçtan uca çalışan ilk oynanış dilimi.

- ✅ `HADAL.Shared` (komutlar, DTO'lar, enum'lar, Protobuf serileştirme, üretim formülleri)
- ✅ VContainer ile DI; ServiceLocator ve statik yönetici yok
- ✅ StateSync hattı: snapshot, delta, reconciliation, rollback animasyonu
- ✅ `ServerMessage` zarfı, şema v2
- ✅ Dairesel grid, hayalet önizleme, sunucu onaylı yerleştirme (stub ile)
- ✅ Kaynak üretim tick'i, mutlak bakiye replikasyonu, kaynak HUD'ı (stub ile)
- ✅ DL380 / CT 160: Docker, Gateway iskeleti, Postgres, Redis
- ✅ Headless Unity 6000.0.32f1 (`server/unity/unity.sh`), batchmode sahne üretimi
- ✅ Test altyapısı: 56 EditMode testi ve PlayMode ekonomi smoke testi

**Çıkış kriteri:** Proje temiz derlenir, yüklenir, açılır ve bütün testler geçer. ✔

---

## Phase 1 — Otoriter oyun sunucusu çekirdeği ✅

**Hedef:** Unity istemcisi CT 160'taki gerçek sunucuya bağlanıp oynar. Yerleştirme doğrulaması, kaynak üretimi ve kalıcılık sunucuya taşınır. İstemci tarafındaki stub sunucular kaldırılır.

### 1.1 Repo hijyeni
- [x] Unity `.meta` dosyalarını git'e al. Proje ayarları, URP varsayılanları, üretilen sahne ve asset'ler de artık repoda.
- [x] `.gitattributes`: `*.sh` dosyaları LF, Unity YAML dosyaları için merge kuralları.

### 1.2 Tek oyun kataloğu
- [x] Bina ve kaynak tanımlarını (maliyet, üretim, tüketim, kapasite, başlangıç miktarı, halka kuralları) tek bir veri dosyasına taşı: `shared/catalog/catalog.json`.
- [x] Shared'da katalog modeli ve yükleyici (`GameCatalog`, `PlacementRules`). Sunucu bunu doğrudan okur; Unity setup aracı SO'ları aynı dosyadan üretir. Böylece iki taraf asla ayrışmaz.

### 1.3 Sunucu oyun çekirdeği (`server/Hadal.Game`)
- [x] Oyuncu oturumu: handshake'teki `DeviceId` ile oyuncu kaydı. Gerçek kimlik doğrulama Phase 2'de gelir.
- [x] Bağlantı sonrası tam `StateSnapshot`: binalar, cüzdan ve üretim oranları.
- [x] `PlaceBuildingCommand` doğrulaması: halka/slot tipi, doluluk ve maliyet. Sonuç ack ve delta olarak döner.
- [x] Sunucu tick döngüsü: `ProductionSimulator` ile 1 sn simülasyon, 5 sn replikasyon.
- [x] Offline kazanç sunucu saatine göre, 12 saat sınırıyla. Forbidden Pattern #8 böylece kapanır.

### 1.4 Kalıcılık
- [x] PostgreSQL şeması (`players`, `buildings`, `wallets`) ve açılışta çalışan migration'lar.
- [x] Sunucu yeniden başlasa da oyuncu durumu korunur.

### 1.5 İstemci geçişi
- [x] Stub simülatörler yalnızca `HADAL_LOCAL_SIM=1` / `GameConfigSO.UseLocalSimulation` ile çalışır. Varsayılan artık gerçek sunucudur.
- [x] Otomatik yeniden bağlanma (backoff); reconnect'te bekleyen paket kuyruğu boşaltılır, ardından snapshot beklenir.
- [x] Geliştirme için gateway adresi ve cihaz kimliği yapılandırılabilir (`HADAL_GATEWAY_URL`, `HADAL_DEVICE_ID`).

### 1.6 Doğrulama
- [x] Sunucu testleri (33): katalog, yerleştirme kuralları, oturum, tick, offline, Postgres, uçtan uca Gateway.
- [x] Entegrasyon testi: gerçek Unity istemci kodu ↔ gerçek gateway (`unity.sh acceptance`). Akış: el sıkışma → snapshot → yerleştirme → ack → üretim delta'sı → yeniden bağlanma.
- [x] Gecikme harness'ı, 100 ms ve 200 ms (`unity.sh latency`).

**Çıkış kriteri:** Headless Unity, PlayMode'da gerçek sunucuya bağlanır; bina yerleştirir, üretimi görür, bağlantıyı koparıp geri gelir ve aynı durumu bulur. Sunucu yeniden başlatıldığında da durum korunur. Stub'lar kapalıdır. ✔ (2026-10-24 — ayrıntılar: [docs/qa/Phase1-Results.md](docs/qa/Phase1-Results.md))

---

## Phase 2 — Dış erişim, kimlik ve dağıtım 🟡

**Hedef:** Oyun dışarıdan güvenli şekilde erişilebilir, sürümlenir ve otomatik dağıtılır. (M1, M2)

> `hadal.cicibyte.com` yayındaki teaser sitesi olduğu için oyun sunucusu **`hadal-api.cicibyte.com`** üzerinde.

- [x] Auth API: misafir hesap → JWT (60 dk) + tek kullanımlık, dönen yenileme anahtarı (hash'li saklanır), IP başına hız sınırı.
- [x] Gateway el sıkışması JWT doğrular (şema 3); oyuncu kimliği hesap ID'si, `DeviceId` yalnızca bilgi amaçlı.
- [x] Sürüm manifesti (`/manifest/version.json`) ve istemcide `PatchService`: zorunlu güncelleme ve bakım modu.
- [x] İstemci: `AuthService` (giriş, yenileme, tekil istek), JWT'li bağlantı akışı, reddedilen jetonda yenileme.
- [x] Tek komutla dağıtım: `server/deploy/deploy.sh` (testler → build → yayın → sağlık kontrolü → otomatik geri dönüş).
- [x] Cloudflare Tunnel `hadal` → `hadal-api.cicibyte.com` yayında (2026-10-25, `server/deploy/setup-tunnel.sh`). İnternetten HTTPS giriş + WSS el sıkışma + yerleştirme uçtan uca doğrulandı.
- [ ] Android geliştirme APK'sı (`unity.sh android`) ve telefonda internet üzerinden oyun testi.
- [ ] Hesap bağlama (Google Play / Apple / e-posta) — geliştirici hesapları gerekiyor; o zamana kadar misafir hesap cihazda saklanan anahtara bağlı.
- [ ] Addressables uzak katalog / CDN — uzak içerik olduğunda (Phase 5).
- [ ] CI: GitHub Actions (push, sırlar ve self-hosted runner kararı gerekiyor); `staging` ortamı.

**Çıkış kriteri:** Telefondaki bir build, internet üzerinden giriş yapıp oynar. Yeni sunucu sürümü tek komutla dağıtılır.

---

## Phase 3 — Üs inşasının tamamlanması ⬜

**Hedef:** `docs/05` tasarımındaki üs deneyiminin tamamı.

- `RemoveBuildingCommand` (iade kuralı) ve `UpgradeBuildingCommand` (seviye, maliyet, süre).
- İnşa ve yükseltme süreleri sunucu saatiyle; hızlandırma kancası (monetizasyon Phase 13'te).
- Halka açma ve mega kubbe genişlemesi (halka 4–12).
- Tam bina kataloğu ve gerçek denge değerleri (yer tutucuların yerine).
- Bina prefab'ları (yer tutucu art), seçim/inceleme UI'ı, mobil dokunma girdisi.

**Çıkış kriteri:** Oyuncu tüm bina türlerini kurar, yükseltir, kaldırır ve halka açar; hepsi sunucu doğrulamalıdır.

---

## Phase 4 — Hayatta kalma döngüsü ⬜

**Hedef:** "Oksijen biterse üs ölür" gerilimi (`docs/06`, `docs/19`).

- Bağımlılık zinciri: enerji yetersizse üreticiler yavaşlar ya da durur (sunucu formülü).
- Oksijen tüketimi (mürettebat), kritik eşik ve alarmlar (LocalEventBus).
- Acil durum olayları: arıza, sızıntı. Onarım komutları.
- Depolama ve kapasite yükseltmeleri.

**Çıkış kriteri:** Ekonomide bir denge döngüsü vardır. Kaynak yönetimi hatası oyuncuyu gerçekten zorlar.

---

## Phase 5 — Sunum katmanı ⬜ *(Phase 3'ten itibaren paralel)*

**Hedef:** `docs/13` ve `docs/14` görsel/işitsel kimliği.

- Oyun durum makinesi: patch → login → handshake → loading → ana menü → üs.
- "Quiet Luxury" UI tasarım sistemi, HUD, menüler, bildirimler.
- URP sualtı görünümü: derinlik karanlığı, sis, kostik ışık, sonar estetiği.
- Ses: açılış, üs ambiyansı, alarm ve UI sesleri.
- Mobil performans profili (pil, ısı, FPS hedefleri).

**Çıkış kriteri:** Dikey dilim "oyun gibi" görünür ve hissettirir; hedef cihazda 30/60 FPS.

---

## Phase 6 — Denizaltılar ⬜

- Denizaltı tersanesi (`submarine_dock`), üretim komutu, filo durumu sunucuda.
- Gövde dayanımı, basınç kalkanı, kapasite istatistikleri.
- Bakım ve onarım.

**Çıkış kriteri:** Oyuncu denizaltı üretir ve donatır; istatistikler sunucudan gelir.

---

## Phase 7 — Basınç, keşif seferleri ve sonar ⬜

**Hedef:** Oyunun en özgün sistemi (`docs/07`, `docs/08`).

- Derinlik katmanları (Safe Zone → The Core) ve basınç sonuçları (gövde hasarı, kayıp).
- Sunucu örnekli seferler: kadro (kahraman + denizaltı), süre, karşılaşmalar, ödül olarak StateDelta.
- Sonar-only bölgeler, sonar ping ve karşılaşma tetikleri (NetworkEvent → Translator).
- Sefer UI'ı ve ikonik sahne.

**Çıkış kriteri:** Oyuncu farklı derinliklere sefer gönderir; risk ve ödül dengesi sunucuda belirlenir.

---

## Phase 8 — Kahramanlar ⬜

- Sınıflar, fraksiyonlar ve nadirlikler (`docs/10`); seviye ve deneyim.
- Yetenek ve ultimate verileri (Shared), sunucuda hesaplanan savaş istatistikleri.
- Kahraman yönetimi UI'ı. Kazanım şimdilik seferlerden; gacha Phase 13'te.

---

## Phase 9 — Düşmanlar ve savaş ⬜

- Düşman türleri (`docs/09`): Abyss Crawler, Siren Swarm, Iron Leviathan, The Forgotten, Titan.
- Sunucu hakemli savaş çözümü (deterministik, Shared formüller), Protobuf savaş delta'ları.
- İstemcide savaş görselleştirme (animasyon LocalEventBus ile).

---

## Phase 10 — Dünya haritası ⬜

- Paylaşılan dünya durumu, merkez ve derinlik halkaları (`docs/11`).
- Bölge sahipliği, risk/ödül dağılımı.
- Shard modeli ve oyuncu yerleşimi; Redis pub/sub replikasyonu.

---

## Phase 11 — Alliance ⬜

- İttifak kurma, üyelik, roller ve sohbet.
- Ortak yapılar ve alliance şehri (`AllianceCityView`).
- Trade Station: kaynak ve blueprint takası, sunucu doğrulamalı.

---

## Phase 12 — PvP ve kuşatmalar ⬜ *(M6)*

- Üs saldırı ve savunma, kuşatma zamanlaması, kalkan ve koruma kuralları.
- Anti-cheat: komut denetim kaydı, hız ve anomali tespiti.
- PvP shard'ları ve eşleştirme.

---

## Phase 13 — Monetizasyon ⬜

**Hedef:** `docs/15` "Quiet Luxury" monetizasyonu; tamamen sunucu otoriteli.

- Hadalite para birimi ve harcama noktaları.
- Cryo-Pod Salvage (kahraman gacha) ve Deep Sea Sonar Ping (ekipman/blueprint gacha). RNG sunucuda, pity kuralları, olasılık şeffaflığı.
- Mağaza: Google Play / App Store faturalandırma, sunucu tarafı makbuz doğrulama, iade işleme.
- Yasal uyum: yaş, bölge, gacha olasılık açıklaması.

**Çıkış kriteri:** Gerçek ödeme test ortamında satın alma → doğrulama → envanter akışı uçtan uca çalışır.

---

## Phase 14 — LiveOps: The Overseer ⬜ *(M7–M9; Phase 2'den sonra paralel)*

- Admin API (ASP.NET Core, JWT + 2FA, IP izin listesi), `AdminActionAuditLog`.
- Next.js GM paneli: oyuncu CRM, posta ve ödül dağıtımı, ekonomi ve gacha kontrol odası, telemetri.
- Gacha hot-swap: staging'de yayın, simülasyon, iki kişi onayı.

**Çıkış kriteri:** Soft launch destek işleri (tazminat, ban, etkinlik ödülü) panelden ve denetim kaydıyla yapılır.

---

## Phase 15 — Leviathan'lar ⬜

- Raid örnekleri, ittifak koordinasyonu, boss mekanikleri ve ödül tabloları.

---

## Phase 16 — THE CORE ⬜

- Son oyun bölgesi, organik hikâye reveal'ı (`docs/20`), dünya çapında olay koordinasyonu.

---

## Phase 17 — Soft launch hazırlığı ⬜

- Yük testi (eş zamanlı oyuncu hedefi), sunucu yatay ölçekleme planı (Gateway / Game Server ayrımı).
- Analitik hattı, çökme raporlama, istemci performans bütçeleri.
- Yerelleştirme (TR/EN), erişilebilirlik, mağaza listeleri ve gizlilik politikası.
- Güvenlik denetimi ve yedekleme/geri dönüş tatbikatı.
- **iOS build hattı** (karar 2026-10-25: o zamana kadar test Android'de): DL380 Xcode projesini üretir; derleme ve imza macOS + Xcode'da yapılır — bir Mac (ör. Mac mini) ya da bulutta Mac (GitHub Actions macOS / Codemagic / Unity Build Automation). Apple Developer Program üyeliği, sertifikalar ve TestFlight gerekir. Linux'ta IPA üretilemez; DL380'de macOS sanallaştırmak Apple lisansına aykırı.
- **Google Play yayın build'i:** kendi imzalama anahtarı (sırlarda saklanır), AAB çıktısı, Play Console hesabı. Android Studio gerekmez; Unity'nin Android imajı SDK/NDK/JDK/Gradle'ı içerir.

**Çıkış kriteri:** Seçili bölgelerde soft launch; KPI takibi (retention D1/D7, ARPDAU).

---

## Phase 18 — Lansman ve canlı operasyon ⬜

- Global yayın, sezon ve etkinlik takvimi (`docs/16`), içerik güncelleme hattı.
- Canlı ekonomi ayarı, topluluk ve destek süreçleri.
