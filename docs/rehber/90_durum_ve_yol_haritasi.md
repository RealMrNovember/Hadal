# 90 · Durum ve Yol Haritası

> **Özet.** Bu bölüm kılavuzdaki her sistemin bugün kodda ne durumda olduğunu, hangi kod
> alanlarına dokunacağını ve hangi sırayla hayata geçirilmesinin önerildiğini gösterir. Faz
> numaraları kök dizindeki `ROADMAP.md`'den gelir (K-10). Kılavuzun kendisi oyun kodunu
> değiştirmez; buradaki her satır ayrı bir geliştirme işidir.

---

## 1. Sistem durumu

| Sistem | Durum | Bugün kodda | Kılavuzdaki hedef | Bölüm | ROADMAP |
|---|---|---|---|---|---|
| Halkalı üs, yerleştirme, kaldırma | ✅ | 12 halka, 124 yuva, %50 iade | Aynı; halka takvimi değişir | [03](03_binalar.md) | 3 |
| Bina seviyeleri ve maliyet | 🟡 | 50 seviye, ×1,09 maliyet, ×1,12 süre; Reaktör 1→50 ~20 saat | Çapalı eğriler; F2P için ~1 yıl | [02](02_tempo_ve_denge.md) | 3 → v9 |
| Bina türleri | 🟡 | 15 tür; 5'inin işlevi yok | 34 tür, hepsi işlevli | [03](03_binalar.md) | 4–12 |
| Reaktör önkoşul zinciri | ⬜ | Yalnızca "reaktörü geçemez" kuralı | Kalkan + dönüşümlü bina + kubbe taşları | [02](02_tempo_ve_denge.md) | 3 → v9 |
| Bina görsel evrimi | ⬜ | Bina başına tek model | 6 katman + 5 seviyede ek modül | [04](04_bina_evrimi.md) | 5 |
| Kaynaklar | 🟡 | 6 kaynak, sabit tavan, tavanı aşan ödül kaybolur | +Süperalaşım, reaktörle büyüyen tavan, Kaynak Kapsülü | [02](02_tempo_ve_denge.md), [15](15_ekonomi_akisi.md) | 4 |
| Hız formülü | 🟡 | Yüzde çıkarma, %10 taban | Bölen formülü (K-13) | [02](02_tempo_ve_denge.md) | v9 |
| Araştırma | 🟡 | 2 dal, 11 düğüm | 13 dal, iki laboratuvar | [06](06_arastirma.md) | 3.6 → 8–9 |
| Kahramanlar | 🟡 | 8 kahraman, yalnızca ekonomi becerisi, Kriyo-Kapsül | 30 kahraman, savaş yetenekleri, yetenek ağaçları | [05](05_kahramanlar.md) | 8 |
| Teçhizat | ⬜ | Yok | 4 malzeme, 5 nadirlik, setler | [17](17_techizat.md) | 8–9 |
| Askerler ve savaş | ⬜ | Yok | 4 tür × 10 kademe, sunucu hakemli savaş | [07](07_birlikler_ve_savas.md) | 9 |
| Denizaltılar, basınç, seferler | ⬜ | Yalnızca dron kurtarma görevleri | Hangarlar, P1–P10, Derinlik Haritası | [08](08_denizaltilar_basinc_seferler.md) | 6–7 |
| Dünya haritası | 🟡 | 200×200, salt görüntü, spiral yerleşim | Kuşaklar, yataklar, PvE, yürüyüş, PvP | [09](09_dunya_haritasi_ve_pvp.md) | 10, 12 |
| İttifak | 🟡 | Kur/katıl/ayrıl, Lider/Üye, yardım, sohbet | R1–R5, teknoloji, bölge, kredi, hediye | [10](10_ittifak.md) | 11 |
| VIP | ⬜ | Yok | Kalıcı VIP | [11](11_vip.md) | 13 |
| The Syndicate | ⬜ | Yok (HUD düğmesi Kriyo-Kapsül'ü açıyor) | Paketler, aylık kart, bilet, fon | [12](12_syndicate.md) | 13 |
| Görevler ve öğeler | 🟡 | 20 hikâye, 8 günlük, 7 öğe | Genişletilmiş hikâye, 150 puan, hızlandırma ve kapsül öğeleri | [15](15_ekonomi_akisi.md) | 4, 13 |
| Etkinlikler ve sezonlar | ⬜ | Yok | Haftalık/iki haftalık/aylık takvim, Akıntı Savaşı | [13](13_etkinlikler_ve_sezonlar.md) | 14, 18 |
| Kronik, köken, profil | ⬜ | Yok | Derinlik Kroniği, 5 köken | [16](16_kronik_kokenler_profil.md) | 10, 18 |
| Son oyun | ⬜ | Yok | Çekirdek kuşağı, Hadal Titanı, açığa çıkış | [14](14_son_oyun.md) | 15–16 |
| Güç | 🟡 | Σ seviye × 10 + kahraman gücü | Bina/araştırma/asker/kahraman tablosu | [02](02_tempo_ve_denge.md) | v9 |
| Overseer paneli | ✅ | Kupon, bakım, canlı denge, ban | Seviye dizisi düzenleme, etkinlik takvimi | [13](13_etkinlikler_ve_sezonlar.md) | 3.5, 14 |

---

## 2. Önerilen uygulama sırası

Her paket kendi testleriyle kapanır (ROADMAP'in değişmez kuralı: "kod yazıldı" bir aşamayı kapatmaz).

### Paket A — Denge altyapısı (catalog v9)
1. `GameCatalogData`'ya seviye dizileri (`levels[]`: maliyet, süre, güç, üretim) eklenir; formül
   alanları geriye uyum için kalır.
2. `BuildingProgression` dizilerden okur; reaktör önkoşul doğrulaması eklenir.
3. Hız formülü bölene geçer (K-13); üretim saatlik tanımlanır (K-17).
4. `ResourceType.Alloy` (Süperalaşım) cüzdana ve HUD'a eklenir; tavanlar reaktörle büyür;
   Kaynak Kapsülü öğesi.
5. Güç formülü (K-14).
6. Kılavuz verisinden catalog v9 üreten bir dönüştürücü (`tools/rehber` → `shared/catalog/catalog.json`).

### Paket B — Yeni binalar ve işlevleri
Basınç Ambarı, Mühendislik Bölmesi, Kriyo Salonu, Hidrotermal Santral ve Mürettebat Yatakhanesi
(savaşa bağlı olmayanlar) önce; askeri binalar Paket D ile.

### Paket C — Bina evrimi (sunum)
Katman alanı, prefab değişimi, ek modüller, evrim anı; sanat hattında katman başına üretim.

### Paket D — Askerler, savaş, denizaltılar
Tatbikat Havuzu, Askeri Kubbe, Revir, Kriyo Bakım Ünitesi, Denizaltı Hangarı; sunucu hakemli savaş;
basınç sınıfları; Derinlik Haritası kampanyası.

### Paket E — Kahramanlar 2.0 ve teçhizat
Savaş istatistikleri, yetenekler, yetenek ağaçları, kaptan/subay; teçhizat atölyesi.

### Paket F — Dünya haritası 2.0, PvP, ittifak 2.0
Kuşaklar ve dış kuşağa yerleşim (K-08), kaynak yatakları, PvE düşmanları, Sonar Enerjisi, yürüyüş,
ralli, kalkanlar, basınç kilitleri, Rezonans Alanları, Egemen; ittifak rütbeleri, teknoloji, bölge.

### Paket G — Monetizasyon
Unity IAP, sunucuda makbuz doğrulama, Hadalit ×10 geçişi (K-12), VIP, The Syndicate, hızlandırma
öğeleri, olasılık açıklaması ve yaş/harcama sınırları.

### Paket H — Canlı operasyon ve son oyun
Etkinlik motoru ve Overseer takvimi, Derinlik Kroniği, Akıntı Savaşı sezonları, Leviathan'lar,
Çekirdek ve Hadal Titanı.

---

## 3. Kod haritası

| Alan | Dosyalar |
|---|---|
| Katalog ve doğrulama | `shared/catalog/catalog.json`, `shared/HADAL.Shared/Catalog/*` |
| Ortak kurallar | `shared/HADAL.Shared/Simulation/*` (`BuildingProgression`, `ResearchRules`, `HeroRules`, `ProductionFormulas`, `PlacementRules`, `BaseRules`, `WorldRules`, `QuestRules`) |
| Sunucu | `server/Hadal.Game/PlayerSession*.cs`, `World/WorldService.cs`, `Social/SocialHub.cs` |
| Overseer | `server/Hadal.Gateway/Admin/*`, `wwwroot/admin/app.js` |
| İstemci | `Assets/_Hadal/Scripts/UI/*` (BaseManagementView, BuildingFunctionsView, SocialView, WorldMapView, MainHudView), `BuildingStateView` |
| Sanat | `art/blender/buildings.py`, `art/gpu/*` |
| Kılavuz | `docs/rehber/`, `tools/rehber/` |

## 4. Bu kılavuz nasıl güncel tutulur?

1. Tasarım değişikliği önce `docs/rehber/veri/*.json`'a yazılır.
2. `python tools/rehber/uret.py` sayfaları ve tabloları yeniden üretir, kontrolleri çalıştırır.
3. `python -m unittest tools/rehber/test_rehber.py` formül ve v8 paritesi testlerini çalıştırır.
4. Kod bir sistemi hayata geçirdiğinde ilgili kayıtta `"durum": "kodda"` yapılır; durum etiketleri
   kendiliğinden güncellenir.
