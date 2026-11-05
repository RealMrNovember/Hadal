# HADAL — Proje Yol Haritası

**Güncelleme:** 2026-10-30
**Yayındaki sürüm:** istemci **0.9.10** (Android, `hadal.cicibyte.com/download/hadal.apk`); sunucu `main` @ `eb35db5` (Doku; 2026-11-01'den beri DL380 CT 160 yerine), katalog v13 (canlı v60). 0.9.10 (ilk derleme Doku'nun yeni anahtarıyla imzalandığı için geri çekildi; DL380'den kurtarılan eski anahtarla yeniden imzalandı): Yem Kapanı (docs/29 §4.1; salı ve cumartesi 48 saat, iki kapan, ittifaka özel Yutucu, hasar kademeleri), Hendek Fethi ve Yem Kapanı yönetim paneli formları. 0.9.9: 13 binaya daha I–III. katman görünümü (toplam 42 katman modeli; Zırh Atölyesi ile 8 GB karta sığmayan 13 katman alt katmanın modelini kullanır). 0.9.8: Hendek Fethi (docs/29 §4.6), binalar ücretsiz taşınır ya da seviyesini koruyarak yer değiştirir, üs halkaları 7 m aralıklı (binalar artık üst üste binmiyor), Çekirdek Reaktör seviyesine göre görünüm değiştirir (docs/rehber/04_bina_evrimi.md; I, IV, V, VI. katman modelli). 0.9.7: Seferberlik, İttifak Düellosu, etkinlik ödülleri postayla (docs/29 E6). 0.9.6: haftalık etkinlik takvimi ve takvim sekmesi, ittifak görev izinleri, oylamalar, zaman dilimi oylaması, lider devri (docs/29 E2). 0.9.5: bütün arayüz Last Asylum pencere düzeninde (`docs/41`), krallıklar, sekmeli posta, telefon bildirimleri.
**Sürüm kuralı:** Oyun 1.0'a kadar **0.x** ile ilerler. Android sürüm kodu `1.000.000 + major·10000 + minor·100 + patch`.

**Kaynaklar:**
- Tasarım: `docs/01–32`
- Tür araştırması: [`docs/research/Mobil_Strateji_Oyunlari_Arastirma.md`](docs/research/Mobil_Strateji_Oyunlari_Arastirma.md)
- Mimari: `docs/17`, `docs/18`, `docs/21`
- Altyapı: `docs/HADAL_SERVER_INFRASTRUCTURE.md`
- Ayrıntılı sprint geçmişi: [`docs/HadalDevelopmentRoadmap.md`](docs/HadalDevelopmentRoadmap.md)

Durum işaretleri: ✅ tamam · 🟡 devam ediyor · ⬜ başlanmadı

---

## Değişmez kurallar

1. **Sunucu otoritelidir.** Her eylem `Command → sunucu doğrular → StateDelta → istemci görünümü` akışından geçer. Ekonomiyi, savaşı, zamanı ve rastgeleliği istemci değil sunucu belirler. Bir komut tekrar gönderilirse ikinci kez ödül ya da kayıp doğmaz (idempotent).
2. **Tek kaynak `HADAL.Shared`.** Komutlar, DTO'lar, kurallar ve formüller yalnızca burada tanımlanır. Wire formatı Protobuf'tur.
3. **Bir iş ancak testlerle kapanır.** Sunucu xUnit, Unity EditMode/PlayMode ve telefonda uçtan uca deneme. "Kod yazıldı" bir aşamayı kapatmaz.
4. **Özgün tema.** Referans oyunların görünüşü ya da içeriği kopyalanmaz; sistemlerin işlevi uyarlanır (araştırma §1.2). Lisansı temiz olmayan varlık oyuna girmez.
5. **Dağıtım düzeni:**
   - Sunucu dağıtımı yalnızca commit'lenmiş HEAD'den yapılır: temiz worktree, önce DB yedeği.
   - APK yalnızca temiz kopyadan (`/opt/hadal-build`) derlenir.
   - Yayın sırası: önce APK Kale2'ye yüklenir, sonra boyutu doğrulanır, en son duyurulan sürüm (`HADAL_LATEST_CLIENT_VERSION`) değiştirilir.

---

## Öncelik sırası (buradan devam ediyoruz)

Sıralama araştırmanın ana önerisine göre yapıldı (§16): önce **kaynak → gelişim → takım → savaş → ödül** döngüsü adil, anlaşılır ve sağlam olacak; sonra içerik ve ölçek gelecek.

| Sıra | İş | Neden şimdi | Durum |
|---|---|---|---|
| **1** | Sağlamlık ve kalite kapıları | Canlıda oyuncu var. Ekonomi ve savaş hatası güveni bozar (§14). | 🟡 |
| **2** | PvP adaleti ve savaş motoru sürümlemesi | Tamamlandı; kahraman metası ve sezon savaşı sonraki aşamalarda (docs/33). | ✅ |
| **3** | İttifak bölgesi ve ittifak binaları | A, B aşamaları ve Şehir Listesi canlıda (docs/32). | ✅ |
| **4** | Yeni deniz yaratıkları ve etkinlik boss'ları | Tamamlandı; boss hasar tablosu ve katkı ödülü sonra. | ✅ |
| **5** | İlk oturum akışı ve öğretici | 0.9.3 ile yayında; ödüller ilk huni verisiyle ayarlanacak (docs/34). | ✅ |
| **6** | Hayatta kalma döngüsü (oksijen, enerji, acil durum) | Yayında: 0.9.4 ve katalog v13 (`7a5b926`, docs/35). | ✅ |
| **7** | Etkinlik takvimi ve ittifak etkinlikleri | Günlük geri dönüş ve ortak hedef (§6.2, docs/29 E2–E6). | 🟡 |
| **8** | Asenkron arena ve düello | Kısa oturumlu, adil PvP (§5.1, §13.1). | ⬜ |
| **9** | Gerçek ödeme ve mağaza | Play Billing ve makbuz doğrulama (§7). | ⬜ |
| **10** | Seferler, basınç ve sonar; denizaltı filosu | Derin keşif fantezisi (docs/07, 08). | ⬜ |
| **11** | Kapalı test ve soft launch hazırlığı | Performans, analitik, mağaza uyumu, emeği geçenler ekranı. | ⬜ |
| **12** | Lansman, sezonlar, Leviathan'lar, THE CORE | Uzun vadeli hedef ve son oyun. | ⬜ |

Ayrıntılar aşağıda, aynı sırayla.

---

## 1. Sağlamlık ve kalite kapıları 🟡

**Hedef:** Araştırma §14'teki yayın kapıları. Canlı oyuncunun ilerlemesi hiçbir hata yüzünden kaybolmaz ya da çiftlenmez.

- ✅ Ekonomi denetimi (`9d27726`):
  - komut defteri: aynı CommandId bir kez çalışır, tekrarı ilk cevabı alır; oyuncuyla birlikte kaydedilir
  - `EconomySafetyTests`: negatif bakiye, iki kez ödül, eşzamanlı harcama (iki bağlantı, sekiz iş parçacığı), taşma ve depo sınırı
  - bulunan hatalar: postada taşan kaynak toplamı göndereni ödüyordu; long sınırını aşan hediye bakiyeyi sıfırlıyordu
- ✅ Zamanlayıcılar (`9d27726`, `TimerSafetyTests`): inşaat, araştırma, eğitim, iyileştirme, sefer, kalkan ve etkinlik uygulama kapalıyken sunucu saatiyle biter; erken talep ve sunucu saatinin ileri ya da geri sıçraması hiçbir şeyi iki kez ödemez. İstemci saati hiçbir şeye karışmaz.
- ✅ Ağ (`9d27726`, `NetworkResilienceTests`): gerçek ağ geçidi üzerinden kopan bağlantı, yeniden bağlanınca tekrar gönderilen komut, zaman aşımı sonrası çift zarf, eski ve yeni soketten aynı komut. Oyuncu hiçbir şeyi iki kez ödemez.
- ⬜ Mobil: düşük bellek, eski cihaz, arka plan/ön plan geçişi. Kritik çökme ve UI taşması yok.
- ⬜ Hata ve çökme izleme, temel analitik: oturum, ilerleme hunisi, ekonomi akışı.
- 🟡 Operasyon (el kitabı: `docs/HADAL_SERVER_INFRASTRUCTURE.md` §14):
  - ✅ yedekten geri yükleme provası (`a803c24`, 29.10.2026 denendi: `server/tests/run-restore-drill.sh`)
  - ✅ denge geri alma provası (aynı prova; yeniden başlatmada `HADAL_PUBLISH_CATALOG_UP_TO` sabitlenmeli)
  - ⬜ bakım mesajı akışı
  - ✅ dağılan ittifak belgelerinin temizlenmesi (`1607453`)
- ✅ Postgres mağaza testleri CT'de gerçekten çalışıyor (`a803c24`): her koşu `hadal-postgres` içinde geçici bir veritabanı açar ve siler (`server/tests/run-ct-tests.sh`). Dağıtım testleri artık canlı veritabanına dokunmuyor.

**Çıkış kriteri:** §14 tablosundaki her alan için otomatik test var ve yeşil. Yedekten geri yükleme bir kez denendi.

---

## 2. PvP adaleti ve savaş motoru sürümlemesi ✅

**Hedef:** Araştırma §5.4 ve §9. Kaybeden toparlanabilir, güçlü oyuncu zayıfı ezemez, her savaş tekrar üretilebilir.

- ✅ Sunucu otoriteli PvP: üs ve takım saldırısı, ralli, keşif, kalkanlar, saldırı alarmı (`6907e25`, `a6bbc57`, `5453444`)
- ✅ Savaşların iki ittifakın savaş kaydına yazılması (`651a1c6`)
- ✅ Takviye: koruyucu listesi savaş penceresinde (`afa1349`, `8c48786`)
- ✅ Adalet kuralları (`b252963`, docs/33):
  - güç aralığı: saldıran hedefin 4 katından güçlüyse üs saldırısı, keşif ve ralli yok
  - saldırı yorgunluğu: aynı üsse 2 saat içindeki galibiyetlerde yağma %100/%50/%25, 4. saldırı reddedilir
  - toparlanma kalkanı: 6 saatte 3 savunma kaybı 2 saatlik kalkan verir (24 saatte en fazla bir kez)
  - yaralılar sahibinin Med Bay yeriyle sınırlı, fazlası ölür; saldıranın düşenlerinin %25'i kalıcı kayıp
  - başlangıç tamponu: en dış üs kuşağına merkez, şamandıra ya da bina kurulamaz
  - saldırı, keşif ve ralli gidişte hızlandırılamaz
- ✅ Savaş motoru bütünlüğü: `CombatRules` sürüm 2, savaş kimliğinden tohum, tur başına 0,97–1,03 rastgelelik; rapor tohumu ve kural sürümünü saklar. Komut tekrarı ve tek seferlik sonuç zaten vardı.
- ✅ Ralli kuralları: takım başına en az 100 asker, raporda üye başına katkı yüzdesi ve yağma payı
- ✅ Sonuç ekranı: iki ordu türüne göre, ölü ve yaralı, yağma, yorgunluk yüzdesi, katkı, turlar, savaş kimliği, tohum, sürüm (`445d380`)
- ✅ Korunan depo payı (`4f47658`): saldırı yalnızca korunan miktarın üstündekinin %20'sini alır; korunan miktar = depo sınırı × (%10 + Kalkan Jeneratörü seviyesi başına %4), en az reaktör × 5.000; Hadalit hiç alınamaz. Savaş penceresinin kalkan sekmesinde görünür.
- ⬜ Karşı sistemler (meta): kahramanlarda kontrol, destek, alan hasarı
- ⬜ Sezon savaşı; arena ve düello Sıra 8'de
- ✅ Yorgunluk, kaybedilen savunmalar ve toparlanma kalkanı `hadal.world_fairness` tablosunda kalıcı (`4f47658`)

**Çıkış kriteri:** Aynı giriş aynı sürümde aynı sonucu verir. İstismar testleri (çoklu saldırı, yarış koşulu, çevrimdışı) yeşil.

---

## 3. İttifak bölgesi ve ittifak binaları ✅

**Hedef:** `docs/32`. Bayrakla doğan ve ittifak büyüdükçe genişleyen alan; liderlerin kurduğu, üyelerin koruyup geliştirdiği binalar.

- ✅ **A aşaması** (`8e58f0a`, `afa1349`, `8c48786`):
  - İttifak merkezi: R4 ve R5 diker, en az 3 üye gerekir. Yarıçap 5 + seviye; seviye 1–5 bağışla yükselir.
  - Şamandıralar: her biri 3 kare alan açar. Hak = 2 × merkez seviyesi + üye/5, en fazla 30.
  - Alan, toplanma noktasını, ittifak teleportunu ve +%25 toplama bonusunu belirler.
  - Şehirler ayrı katmanda: alana 4 kare yakınlıkta saldırılabilir, en fazla 6 şehir.
  - Haritada alan ittifak renginde boyanıyor. Bayrak menüleri var. Etiketler artık ekranı kaplamıyor.
  - Çoklu dünya için temel: `world_id`.
- ✅ Şehir Listesi (`445d380`): İşgal edilmiş / Alınabilir sekmeleri, seviye filtresi, dokunulabilir koordinat, km, sahip, "komşu, saldırılabilir" işareti, sahip olunan/izin verilen sayısı; Şehir Bonusu (şehir başına her yerde +%5 toplama hızı)
- ✅ **B aşaması** (`3b5ba03`, `52c1c8c`): ittifak binaları
  - Röle (+%3 savunma/seviye, 5 üye), Sondaj (+%5 toplama, 10 üye ve 1 M güç), Top (+%3 saldırı, 15 üye ve 3 M güç)
  - R4/R5 alana yerleştirir; üyeler takım göndererek inşa eder, takımlar muhafız olarak kalır
  - bağışla Sv.5'e kadar yükselir
  - binalar ve şamandıralar saldırıya ve ralliye açık (merkez değil); dayanıklılığı biten bina hasarlı kalır ve saatte %10 onarılır, şamandıra düşer
  - savaşlar iki ittifakın kaydına yazılır
- ⬜ Toplanma noktası uyarısının alanın merkezine göre güncellenmesi (sunucu tarafı hazır).

**Çıkış kriteri:** Bir ittifak sıfırdan alan kurar, büyütür, bir binayı birlikte inşa edip savunur. Rakip ittifak saldırabilir; sonuç iki tarafın kaydına düşer.

---

## 4. Yeni deniz yaratıkları ve etkinlik boss'ları ✅

**Hedef:** Araştırma §6. Canavarlar ekonomiyi besler, etkinlik boss'ları ittifakın ortak hedefi olur.

- ✅ Kendi hattımızda (SDXL → BiRefNet → TRELLIS, doku2 GPU) yalnızca metin tarifinden üretilen yaratıklar (`4537025`); indirilen örnek modellerin hiçbiri kullanılmadı (`art/THIRD_PARTY.md`, `art/gpu/creatures.json`). Mobil bütçe: 1,5–15 bin üçgen, 512–1024 doku.
  - Canavarlar: abis fener balığı (Sv.10–28), fener ağızlı dev (Sv.20–35, başlangıç kuşaklarında çıkmaz), zırhlı dev yengeç (Sv.14–35)
  - Ortam: ışıklı melek balığı sürüleri ve denizanaları kamerayı izler, uzaklaşınca gizlenir
  - Etkinlik boss'ları (yalnızca etkinlikte): "Uçurum Ağzı" ve "Derinlik Kraken'i"
- ✅ Animasyon: `Hadal/Creature` gölgelendiricisi (kuyruk ve gövde dalgası, dokunaç, çan nabzı, kıskaç, parlama, vuruş flaşı) ve `CreatureMotion` (yüzme yolu, yengeç adımı, süzülme, yem salınımı); takım varınca canavar atılır ve parlar, yenilen canavar batar
- ✅ Sunucu kuralları (`1de1768`): kuşaklara göre ağırlıklı tür seçimi, türe göre güç ve ödül çarpanları
- ✅ Etkinlik boss'u: etkinlik tanımında boss listesi; ortak can havuzu, her vuruş kendi öldürdüğü kadar düşer; hasar `boss_damage` olarak puanlanır ve ittifak yarışmasında ittifakların toplam hasarı sıralanır; son vuruşa gümüş (1/10 altın) sandık; admin panelinde "Uçurum Ağzı Uyanıyor" hazır şablonu
- ⬜ Boss için oyun içi hasar tablosu, ölümde tüm katkı verenlere ödül, takvimle gezen boss'lar
- ⬜ Denge: boss gövde çarpanları ve ödüller ilk tahmin; düşük seviyeli oyuncu yüksek seviyeli boss'a 0 hasar verebilir

---

## 5. İlk oturum akışı ve öğretici ✅

**Hedef:** Araştırma §16'nın ilk sorusu: "Oyuncu ilk 10 dakikada ne yaptığını anlıyor mu?" Ayrıntı: `docs/34_First_Session_Guide.md`.

- ✅ 15 adımlık sunucu otoriteli rehber zinciri (görev sistemine bağlı): reaktör, ilk üretim, ilk kahraman, ilk canavar, ilk ödül, ilk toplama seferi; ödüller doğrudan cüzdana, hiçbir adım zamanlayıcıda takılmaz. Gerçek bakiyeyle uçtan uca test: 12. adım (ilk toplama) 20 dakikanın altında.
- ✅ Rehber arayüzü: Komutan Rourke baloncuğu, dokunulacak düğmede nabız halkası, haritada hedef oku; her zaman kapatılabilir, sunucunun adımından devam eder.
- ✅ Kilit görünürlüğü: Araştırma ve PvP düğmelerinde "Sv.X" rozeti, görev panelinde "Yakında açılacaklar".
- ✅ İlk gün hedefi: ittifaka katılma, ittifak sohbetine bir mesaj, bir yardım alma ya da verme (ödüllü).
- ✅ Huni ölçümü: `hadal.onboarding_events` + Overseer "İlk Oturum Hunisi" (adım başına ulaşan, ortanca süre, ilk 10 dakikada takılan oranı).
- ✅ Yayın: sunucu `35d78a0`, istemci 0.9.3, katalog 12 (canlıda v59)
- ✅ İsim kuralı: küfür, görünmez karakter, `< >` ve emoji dışında her isim serbest; gizlenmiş küfürleri yakalayan filtre (`e919bd5`)
- ⬜ İlk huni verisiyle adım ödüllerinin ayarı

**Çıkış kriteri:** Yeni hesap yardımsız ilk kilometre taşına ulaşır. Huniden ilk 10 dakikada terk oranı ölçülür.

---

## 6. Hayatta kalma döngüsü ✅

**Hedef:** "Oksijen biterse üs ölür" gerilimi (`docs/06`, `docs/19`). Temayı ekonomiye bağlar. Ayrıntı: `docs/35_Survival_Loop.md`.

- ✅ Bağımlılık zinciri (`7a5b926`): enerji deposu boşken ve talep üretimden fazlayken enerji kullanan binalar arz/talep oranında çalışır, hiçbir zaman %50'nin altına inmez (durmaz). Depo yolda boşalırsa adım tam o tikte bölünür; çevrimdışı sonuç çevrimiçiyle aynı.
- ✅ Oksijen (`7a5b926`): askerler ve mürettebat (saatte reaktör seviyesi × 10) solur. %20'nin altında ve düşerken uyarı; 0'da ve düşerken oksijen/enerji dışı üretim −%20, yeni inşaat, araştırma ve eğitim +%20. HUD: nabız atan Oksijen/Enerji hapı, alarm şeridi, "Ne yapmalıyım?" kartı ve Git düğmesi.
- ✅ Acil durumlar (`7a5b926`): sızıntı (oksijen üretiminin %20'si) ve arıza (bir üretici durur). Yalnızca çevrimiçiyken, oyuncu ve saat başına tohumlu zar (%10), rehber sürerken ve reaktör 5 altında yok, en fazla bir tane ve 6 saat arayla, 4 saatte kendiliğinden biter. `RepairEmergency`: bir saatlik Titanyum gelirinin %10'u ve 10 dk; ittifak yardımı alır; aynı CommandId bir kez çalışır. Binada uyarı ışığı, sızıntıda kabarcık; başlayınca ve bitince bildirim.
- ✅ Cezanın toparlanabilir olması: bina kaybı ve asker ölümü yok, sebep gidince anında normale döner. Katalog anahtarları `survival.enabled` ve `survival.emergencies.enabled` canlıda kapatır (katalog tasarımı v13; bölümsüz eski katalog döngüyü kapalı çalıştırır).
- ✅ Testler: sunucu 367/367 (24 yeni `SurvivalTests`), Unity EditMode 169/169 (`SurvivalClientTests`, 18 dil)
- ⬜ Yayın: istemci ile birlikte; `/opt/hadal/server/.env` içinde `HADAL_PUBLISH_CATALOG_UP_TO` 12 → 13

**Çıkış kriteri:** Kaynak yönetimi hatası oyuncuyu gerçekten zorlar ama oyundan koparmaz.

---

## 7. Etkinlik takvimi ve ittifak etkinlikleri 🟡

**Hedef:** Araştırma §6.2 ve `docs/29` E2–E6.

- ✅ Etkinlik motoru ve admin sayfası: bireysel ve ittifak yarışmaları, giriş ödülleri, aşamalar, sıralama (`docs/29` E1)
- ✅ Haftalık döngü takvimi ve hazır etkinlik şablonları: haftalık programlar her krallık için etkinliği başlangıçtan önce kendisi oluşturur (sabit kimlik, kayıt defteri; yeniden başlatmada çift oluşmaz). Hazır programlar: her gün Derinlik Yarışı (4 saatlik 6 tema, her gün kayar), pazartesi başlayan İttifak Haftası ve 7 günlük Dalış Günlüğü. Admin'de takvim (7 günlük ızgara, aç/kapat, gün/saat/süre, etkinlikten program, sıradakini şimdi oluştur); oyunda etkinlik merkezinde Takvim sekmesi (`docs/29` §2.5). Hafta sonu bonus penceresi bonus şablonuyla gelecek.
- ✅ Görev izinleri, oylama ve zaman dilimi seçimi (E2): lider her şeyi yapar, R4 görevine göre — savaş zaman oylaması açar, lojistik merkez/şamandıra/ittifak binası kurar, diplomasi anket açar ve duyuruyu yazar (keşif için harita işareti henüz yok). Anketler (2–6 seçenek, 1–72 saat, tek/çoklu, gizli/açık; açıkken oy değiştirilebilir, sonuç canlı) ve etkinlik zaman oylaması (2–5 saat, 24 saat oylama, kazanan sunucu saatiyle sabitlenir ve sonraki etkinlik adımı için saklanır); kapanınca özet ittifak sohbetine düşer. Lider 14 gün girmezse liderlik en güçlü aktif R4'e geçer, sohbet ve postayla duyurulur. Oyunda ittifak merkezinde Oylamalar sayfası, adminde oylama listesi (kapat/sil, denetim kaydıyla). Bağışlar zaten vardı (`docs/29` §2.3, §5.1).
- 🟡 Hendek Fethi, Derinlik Kuşatması, İttifak Düellosu, Seferberlik (E3–E6): İttifak Düellosu ve Seferberlik hazır (E6), Hendek Fethi 0.9.8'de yayında (yönetim paneli formu da hazır), Yem Kapanı 0.9.10'da yayında (toplanma şartı, çevrimdışı katılım ve zorluk kilidi sonra), Derinlik Kuşatması hazır (yayın bekliyor; ipucu şarjı sonra).
  - **Seferberlik**: 15+ üyeli her ittifaka 8 kartlık ortak görev havuzu (toplama, eğitim, araştırma, inşaat gücü, yaratık, iyileştirme, hızlandırma, kahraman seviyesi, yardım, Derinlik Haritası); kartlar yeşil/mavi/mor/altın (×1/×2/×3/×5 hedef, 100/200/340/560 puan). Üye bir seferde bir görev alır, oynayarak bitirir; puan kendisine ve ittifakına yazılır. Bırak, günde 3 yenile (10 dk ara), günde 8 görev. Kişisel puan sandıkları ve ittifak sıralaması. Varsayılan program perşembe 00:00 UTC, 3 gün.
  - **İttifak Düellosu**: her pazartesi 00:00 UTC, 6 gün; krallık içinde güce göre 1'e 1 eşleşme, tek kalan krallığın günlük ortalamasıyla oynayan Derinlik Yankısı'yla eşleşir. Altı temalı gün (toplama, üs büyütme, araştırma, kahramanlar, topyekûn seferberlik, av), gün kazanmak 1/2/2/2/2/4, 7'ye ilk ulaşan kazanır. Günlük kişisel sandıklar, hafta sonucu postası (kazanan/kaybeden/berabere ödülü), son düellolar. İttifak Haftası yanında ayrı puanla sürer.
  - Oyunda etkinlik merkezinde kendi sekmeleri ve ittifak merkezinde Düello / Seferberlik kutucukları; adminde form ayarları, canlı havuzlar ve eşleşmeler, yeniden eşleştirme (denetim kaydıyla). Yeni varsayılan programlar canlı takvime bir kez eklenir, mevcut programlara dokunulmaz (`docs/29` §2.5, §4.3, §4.5).
- ✅ Savaş raporlarının ve etkinlik ödüllerinin postayla gelmesi: savaş raporları sekmeli postada (0.9.5); etkinlik bitince sıralama ödülleri, alınmamış aşama ödülleri, giriş günleri ve düello sandıkları Etkinlikler sekmesine postayla gelir (kahraman kartları dahil). Her posta bir kez gider: yeniden başlatma, yarıda kalan gönderim ya da silinip temizlenen posta iki kez ödetmez (`docs/29` §2.2).

**Çıkış kriteri:** Her gün yapılacak süreli bir etkinlik ve haftada bir ittifak hedefi var.

---

## 8. Asenkron arena ve düello ⬜

- ⬜ Kayıtlı savunma takımı, benzer güç aralığında eşleşme, günlük deneme sınırı
- ⬜ Sıralama ve sezon ödülleri. Ödüller güç farkını büyütmez.

---

## 9. Gerçek ödeme ve mağaza ⬜

- ✅ Hadalit mağazası ve paketler; VIP puanı satın alımdan geliyor. Ödeme şimdilik admin panelinden elle işleniyor.
- ⬜ Google Play Billing, sunucuda makbuz doğrulama, iade ve hile koruması
- ⬜ Pay-to-win sınırları: konfor ve kozmetik ağırlıklı teklifler (§15)

---

## 10. Seferler, basınç, sonar ve denizaltı filosu ⬜

- ✅ Sonar enerjisi ve canavar saldırıları; toplama seferleri; tek denizaltılı takım görünümü
- ✅ Yürüyüş hızı araştırması (`3037142`): Araştırma Merkezi'nde İtki ağacı. İtici Verimliliği +%4 (10 sv., Merkez 1), Hidrodinamik Gövde +%5 (5 sv., Merkez 8), toplam +%65: 20 dk'lık yol 12:07. Hızlı Dönüş toplama ekiplerine eve dönüşte +%25. VIP ve ittifak araştırmasıyla toplanır; toplama, canavar/boss, PvP ve ralli yolculuklarında sunucu uygular. Sunucu 374/374, Unity EditMode 178/178. Katalog tasarımı v13 ile yayınlanacak
- ⬜ Derinlik katmanları ve basınç dayanımı (`docs/07`)
- ⬜ Sefer örnekleri ve sonar bölgeleri (`docs/08`)
- ⬜ Denizaltı filosu, gövde istatistikleri ve üretim

---

## 11. Kapalı test ve soft launch hazırlığı ⬜

- ⬜ Performans: varlık bütçesi, LOD, gerçek cihaz testi. Yük testi.
- ⬜ Analitik panoları, tutundurma ve ekonomi metrikleri
- ⬜ Play Store uyumu: gizlilik, hesap silme, içerik derecelendirmesi
- ⬜ **Emeği geçenler ekranı:** CC-BY varlıklar ve OFL yazı tipi (Fredoka)
- ⬜ iOS derlemesi

---

## 12. Lansman ve sonrası ⬜

- ⬜ Sezonlar ve kısmi sıfırlama, çoklu dünya ve göç (`world_id` temeli hazır)
- ⬜ Leviathan raid'leri (`docs/15`)
- ⬜ THE CORE son oyunu (`docs/20`)

---

## ✅ Tamamlananlar

### Temel ve sunucu çekirdeği
- ✅ `HADAL.Shared` protokolü (Protobuf, şema v3)
- ✅ DI, StateSync hattı (snapshot, delta, reconciliation)
- ✅ Otoriter oyun sunucusu (`server/Hadal.Game`), gateway, Postgres kalıcılığı, write-behind kuyruklar
- ✅ Kimlik:
  - misafir hesap, JWT ve dönen refresh token
  - aktarım kodu, Google/Apple bağlama altyapısı
  - profil ve isim değiştirme kuralları: ilki ücretsiz; sonrası İsim Kartı ya da 500 Hadalit; 24 saat bekleme; benzersiz isim
- ✅ Dış erişim:
  - `hadal-api.cicibyte.com` (Cloudflare Tunnel)
  - tek komutla dağıtım (`server/deploy/deploy.sh`, hata olursa otomatik geri alma)
  - headless Unity derlemesi (CT 160)
- ✅ Uygulama içi güncelleme (OTA), indirme sayfası (Kale2), 18 dil desteği (İtalyanca dahil)

### Üs ve ekonomi
- ✅ Dairesel üs: inşa, yükseltme, kaldırma, sunucu saatiyle inşaat, ücretsiz bitirme
- ✅ Halka açma ve bina kopya sınırları (v9). Fazla binalar kaldırılıp bedelleri iade edildi.
- ✅ Araştırma, Boost, Overdrive, kurtarma seferleri, kesirli üretim
- ✅ Katalog v10, canlı katalog yayınlama
- ✅ Depo sınırını aşan bakiyeyi üretimin silmemesi (`9559e1d`)
- ✅ Hızlandırma eşyaları, kapsüller
- ✅ Last Asylum tarzı çanta: kullanım kartı, sınırsız toplu kullanım, çizilmiş simgeler (`d92b180`)

### Kahramanlar ve ordu
- ✅ Kahramanlar: portreler, 3D modeller, kartlar, yıldızlar, seviye
- ✅ Last Asylum tarzı kahraman listesi ve tam boy çizimli kahraman kartı (`d92b180`)
- ✅ Dört asker türü ve on kademe, talim alanları, Askeri Kubbe kapasitesi
- ✅ Med Bay: yaralılar ve iyileştirme. Beş kahramanlı takımlar ve komuta kapasitesi.
- ✅ Savaş çözümü: raundlar, sıralar, karşıtlıklar, bozgun. Canavarlar ve savaş raporları.

### Dünya haritası
- ✅ Paylaşılan dünya, kaynak düğümleri, toplama, Derinlik Haritası
- ✅ Kilometre cinsinden mesafe (12 sn/km), herkesin canlı gördüğü kesikli rotalar, tek denizaltılı takım
- ✅ Akıcı hareket, gidiş yönüne bakan denizaltı ve kabarcık izi
- ✅ Toplama rozeti (yeşil/mavi/kırmızı) ve canlı sefer ayrıntı ekranı (`cc17603`, `c46a41d`)
- ✅ Teleportlar: rastgele, hedefli, ittifak. Bilet ya da Hadalit ile.

### Sosyal ve ittifak
- ✅ Last Asylum tarzı sohbet: kanallar, avatarlar, konum paylaşımı
- ✅ İttifak kurma, katılma, rütbeler R1–R5, görevler, yönetim
- ✅ İttifak yardımı: inşaat, iyileştirme ve araştırma; yüzen yardım baloncukları (`bf1aac2`)
- ✅ İttifak ekranı (`docs/31`):
  - ana sayfa ve duyuru (`9f5d0be`)
  - üyeler sayfası: lider kartı, unvanlar, açılır rütbeler, son görülme (`c0fd524`)
  - ittifak ve savaş kayıtları (`1033952`)
  - ittifak parası ve haftalık mağaza (`3ff139f`)
  - ittifak hediyeleri (`73d1529`)
  - ittifak araştırma ağacı ve bağışlar (`bd030a9`)
  - sıralama: güç, yok etme, bağış (`e86a91e`)
  - toplanma noktası kartı ve anında ittifak teleportu (`f3cd82f`)
- ✅ Posta kutusu:
  - sistem ve admin postaları, ekleri bir kez alınır (`befcbad`)
  - oyuncudan oyuncuya posta; kaynak yalnızca ittifak arkadaşına, kaynak başına günlük limitle (`8b4a58d`)

### Etkinlikler, VIP ve monetizasyon temeli
- ✅ Etkinlik motoru ve etkinlik merkezi
- ✅ VIP 1–15 (Last Asylum tablosu): giriş serisi, satın alma ve VIP puanı eşyaları; günlük sandık; yürüyüş hızı bonusu
- ✅ Hadalit mağaza ekranı (ödeme bekliyor)

### The Overseer (admin paneli)
- ✅ Şifre ve TOTP girişi, roller, denetim kaydı
- ✅ Oyuncu arama ve yönetimi, ban
- ✅ Kaynak, eşya, kahraman, asker, kalkan ve VIP gönderme
- ✅ Kuponlar, bakım modu ve duyuru, denge kataloğu yayınlama ve geri alma
- ✅ Etkinlik sayfası, mağaza paketi kredilendirme
- ✅ Posta gönderme: tek oyuncuya ya da herkese

### Sunum
- ✅ UI tasarım sistemi (çizilmiş kaplama, Fredoka yazı tipi), HUD, logo, açılış ekranı
- ✅ URP derin deniz görünümü, prosedürel ve GPU ile üretilmiş sanat, yaşayan mürettebat ve ortam
- ✅ Özgün müzik ve ses efektleri: üs, harita, savaş, zafer ve yenilgi (`45badf4`)
- ✅ Tanıtım sitesi (EN/TR/IT)

---

## Notlar

- **Sabit kalmış iki seçim:**
  - İttifak araştırmasında "asker kapasitesi" yerine "takım yükü" kullanılıyor.
  - Ralli yok etmelerinin hepsi ralliyi başlatan oyuncuya yazılıyor.
  Gerekirse Sıra 2 ve 3'te ele alınır.
- **İlk tahmin olan denge değerleri:** ittifak parası, mağaza fiyatları, hediye eşikleri ve araştırma maliyetleri. Kapalı testte (Sıra 11) ayarlanacak.
