# Rakip Analizi — Last Asylum: Plague

**Tarih:** 2026-10-25
**Amaç:** HADAL'ın hedeflediği kalite çıtasını ve tür formülünü anlamak; tasarım ve yol haritasına etkisini belirlemek.

---

## 1. Oyunun kimliği

| | |
|---|---|
| Geliştirici | WISENESS GAME ONLINE INTERNATIONAL LTD |
| Çıkış | 11 Şubat 2026 (global) |
| Tür | Midcore **survival 4X strateji** (Whiteout Survival / Kingshot ailesi) |
| Mağaza | App Store Strateji #1 (Mart 2026), 4.8★ (~42K oy), 13+, ~703 MB, 18 dil (Türkçe dahil) |
| Gelir | Haftalık IAP rekoru **3,8 M $** (10–16 Ağu 2026); Midcore 4X'te 6. sıra — Puzzle & Survival ve Evony'yi geçti |
| Büyüme | Temmuz ortası "Era of Revival" genişlemesi + ağır reklam (AI üretimli hikâye kancası), çıkıştan önce haftalık 918K yükleme |
| Fiyatlama | Ücretsiz + IAP: 0,99 $ – 99,99 $ arası ~35 ürün, ganimet kutusu (gacha) var |

## 2. Oyun döngüsü

**İç halka — "Sığınak" (hastane yönetimi):** Oyuncu bir veba doktoru; kasabanın son sığınağını işletir.
- Hastalar gelir → koğuşlarda tedavi edilir → **ilaç** gerekir → ilaç **şifalı ottan** üretilir.
- Personel tutulur (resepsiyonist, işçi, taşıyıcı, hemşire) ve işler **otomatikleşir**. Oyuncu küçük insanların çalıştığını görür.
- Ana bina yükseldikçe yeni koğuşlar, depo ve personel kapasitesi açılır. VIP hastalar ve bölüm hedefleri ilerlemeyi yönetir.
- Kaynaklar: odun, buğday/yiyecek, şifalı ot, ilaç. Rehberlere göre sığınakların en sık çöküş nedeni **ilaç kıtlığı**. Talep (yatak, hayatta kalan) üretimi aşarsa sistem çöker.

**Savunma — "Fare Sürüsü" (tower defense):** Dalgalar hâlinde gelen mutant fareler ve sonunda Fare Kralı. Kuleler yerleştirilir ve yükseltilir; kahramanlar savaşa girer.

**Dış halka — 4X harita:** Hayatta kalanlar kaynak toplamaya gönderilir. Kahraman + asker birlikleriyle veba bölgeleri ve kasabalar geri alınır. İttifak toplanma saldırıları (rally), bölge kontrolü ve ittifak etkinlikleri vardır (ör. "Blight Hunt": 20+ üye, 15 dalga).

**Kahramanlar:** Savaşa en fazla 5 kahraman girer; nadirlik seviyeleri ve aktif yetenekler var. Enerji dolunca yetenek elle ya da otomatik kullanılır (yarı-idle savaş). Kazanım: etkinlik, görev ve gacha. Kahramanlar birliğin yeteneğini ve rolünü belirler; sayı ve gücü askerler sağlar.

**Ritim:** İlk görevler bir dakikanın altında, sürekli küçük ödüller. Sistemler zamanla katman katman açılır: karantina bölgesi, antitoksin atölyesi, birlik dağıtımı. İnceleme özellikle **oyuncuyu zamanla sıkıştırmamasını** övüyor: birkaç gün oynamasanız bile saldırı bildirimi yağmuru yok.

## 3. Neden başarılı?

1. **Güçlü, tek cümlelik fantezi:** "Vebada son hastaneyi işleten doktor." Reklamda anında anlaşılıyor.
2. **Yaşayan üs simülasyonu:** Görünür personel, hasta akışı ve kuyruklar; ilk dakikadan "işletme" keyfi. Klasik 4X'in soğuk bina listesinin yerini alıyor.
3. **Talep odaklı ekonomi:** Hastalar ve hayatta kalanlar sürekli tüketiyor; üretim zinciri (ot → ilaç) gerilim yaratıyor.
4. **Tür hibriti:** Simülasyon + tower defense + kahraman gacha + ittifak 4X. Her biri farklı oyuncu motivasyonuna hitap ediyor ve gelir modeli tanıdık.
5. **Canlı operasyon ve UA:** Çıkıştan 5 ay sonra büyük genişleme, sürekli etkinlik, agresif reklam testi.
6. **Atmosfer:** Kasvetli, soluk renk paleti, Karanlık Çağ mimarisi, uyumlu müzik.

**Zayıf yönler (inceleme 3,5/5):** Kahraman ve karakter tasarımları oyunun tonuyla uyumsuz. Temel döngü türü yeniden icat etmiyor.

## 4. HADAL ile karşılaştırma

HADAL'ın tasarımı ile Last Asylum'un yapısı **neredeyse birebir örtüşüyor**. Bu iyi bir haber: kanıtlanmış, yüksek gelirli bir formülün üzerindeyiz.

| Last Asylum | HADAL karşılığı | Durum |
|---|---|---|
| Sığınak / hastane | Okyanus dibindeki üs, dairesel halkalar (`docs/05`) | Tasarım var; **yaşayan simülasyon yok** |
| Veba = sürekli tehdit | Basınç ve oksijen (`docs/07`) | Tasarım var |
| Hasta → ilaç talebi | Mürettebat → oksijen ve yiyecek talebi | **Eksik:** talep tarafı tasarlanmadı (Phase 4) |
| Personel otomasyonu | — | **Eksik** |
| Fare Sürüsü TD | Abyss Crawler / Siren Swarm (`docs/09`) | **Eksik:** üs savunması TD modu olarak tasarlanmadı |
| Kahraman gacha (5'li takım) | Cryo-Pod Salvage, sınıf ve fraksiyon (`docs/10`, `docs/15`) | Tasarım var |
| Birlik ve bölge fethi | Seferler + denizaltılar + dünya haritası (`docs/08`, `docs/11`) | Tasarım var; asker katmanı yok |
| İttifak rally / etkinlik | Alliance, Leviathan raidleri (`docs/12`) | Tasarım var |
| Premium döngü | Hadalite (`docs/19`) | Tasarım var |
| 18 dil, 700 MB, yoğun içerik | — | Kapsam ve bütçe planı gerekli |

## 5. HADAL için öneriler

1. **Çekirdek fantezi ilk 60 saniyede okunmalı.** "Okyanusun dibinde insanlığın son kubbesini yönetiyorsun ve oksijen azalıyor." Basınç ve oksijen, Last Asylum'daki veba ve ilacın karşılığı; ekonominin merkezine konmalı.
2. **Yaşayan üs:** Mürettebat görünür olmalı; oksijen, yiyecek ve onarım işleri taşınmalı. İlk sürümde basit animasyonlu "küçük insanlar" bile algıyı kökten değiştirir. Phase 3 ile Phase 5 birleşmeli.
3. **Talep odaklı ekonomi (Phase 4 öne alınmalı):** Mürettebat sayısı, oksijen ve yiyecek tüketimi; kapasite ve depo baskısı. Sunucu formülleri Shared'da hazır, eksik olan tüketim tarafı.
4. **Üs savunması = tower defense modu:** Abyss Crawler ve Siren Swarm dalgaları, kubbe kuleleri, sonunda bir Leviathan. Sunucu hakemli ama görsel olarak hızlı.
5. **Kahraman + asker ayrımı:** Kahraman rol ve yetenek verir, mürettebat ya da asker sayı verir.
6. **Rahat tempo:** Oyuncuyu zamanla cezalandırmamak, inceleme ve yorumlarda en çok övülen özellik. Offline sınırı (12 saat) ve kalkan kuralları buna göre ayarlanmalı.
7. **Kalite çıtası görsel:** Last Asylum'un gücü atmosferinden geliyor. HADAL'ın "derinlik ve karanlık + sonar estetiği" (`docs/14`) güçlü bir ayrışma noktası. Karakter tasarımında rakibin zayıf kaldığı tutarlılık bizim fırsatımız.

## 6. Yol haritasına etkisi (öneri — karar kullanıcıda)

- Teknik temel (Phase 0–2) bu formülü taşıyacak şekilde hazır: sunucu otoritesi, ekonomi tick'i, kimlik ve dağıtım.
- **Bir sonraki büyük iş, "oyun gibi görünen ve hissettiren" dikey dilim olmalı.** Hedef: mobilde açılınca bir kubbe, çalışan mürettebat, oksijen gerilimi, ilk bina ve ilk görev.
  - Phase 3 (üs inşası), Phase 4 (hayatta kalma döngüsü) ve Phase 5'in (sunum) çekirdek kısımları tek bir **"Dikey Dilim"** aşamasında birleştirilir.
  - Tower defense ve 4X haritası bunun ardından gelir.
- Art yönü ve UI kalitesi bu aşamanın darboğazı. 3D model ve animasyon üretimi için bir kaynak kararı gerekiyor: asset store, dış kaynak ya da AI destekli üretim.

## Kaynaklar

- [App Store — Last Asylum: Plague](https://apps.apple.com/us/app/6756989323)
- [Pocket Gamer — inceleme (3,5/5)](https://www.pocketgamer.com/last-asylum-plague/review)
- [BlueStacks — başlangıç rehberi](https://www.bluestacks.com/blog/game-guides/last-asylum-plague/lap-beginners-guide-en.html)
- [BlueStacks — ipuçları](https://www.bluestacks.com/blog/game-guides/last-asylum-plague/lap-tips-tricks-en.html)
- [Gamigion / AppMagic — gelir raporu](https://www.gamigion.com/?p=35318)
- [LDShop — Blight Hunt rehberi](https://www.ldshop.gg/blog/last-asylum-plague/hunt-battle-strategy-guide.html)
- [Lootbar — asker rehberi](https://lootbar.gg/blog/en/last-asylum-plague-troop-guide.html)
- [TopUpLive — başlangıç rehberi](https://www.topuplive.com/news/last-asylum-plague-beginner-guide.html)
- [AppPricingLab — mağaza verisi](https://apppricinglab.com/app/apple/6756989323)
