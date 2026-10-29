# 35 — Hayatta Kalma Döngüsü

**Document:** Game Design · HADAL
**Language:** TR (canonical design voice)
**Status:** İlk sürüm uygulandı (2026-10-30, katalog tasarımı v13). Sayılar `shared/catalog/catalog.json` → `survival` bölümündedir; kurallar `shared/HADAL.Shared/Simulation/SurvivalRules.cs` ve `ProductionSimulator`'dadır.

---

## Neden

`06_Resources` "Oksijen biterse üs ölür" der; temanın ekonomiye bağlandığı yer burasıdır. Araştırma §3.1 ve §15 iki sınır çizer: tek bir kaynak bütün ilerlemeyi kilitlememeli ve kayıp oyuncuyu oyundan koparmamalı. Bu yüzden döngü **toparlanabilir**dir: üs hiçbir zaman ölmez, yalnızca verimi düşer. Hiçbir bina yıkılmaz, hiçbir asker ölmez; sebep ortadan kalktığı an her şey normale döner.

## 1. Bağımlılık zinciri: enerji

- Enerji deposu **boşken** ve enerji talebi üretimden **fazlayken**, enerji kullanan her bina (Oksijen Tesisi, Gıda/Biyo Laboratuvarı, Dron Fabrikası, kubbeler, Araştırma Merkezi …) `arz / talep` oranında çalışır.
- Oran yukarı yuvarlanır (binde bir hassasiyet); böylece boş depo 0'da kalır, her tikte dolup boşalarak titremez.
- Oran hiçbir zaman `energyFloorPercent`'in altına inmez: **üretim asla tamamen durmaz** (açılış değeri %50; matematik %25'e kadar ayarlanabilir).
- Depoda enerji varken açık yalnızca stoğu eritir (eski davranış); stok bittiği **tikte** yavaşlama başlar.
- Reaktörün kendisi enerji kullanmaz, yavaşlamaz. Çare: Çekirdek Reaktör'ü yükseltmek ya da tüketiciyi azaltmak.

Zincir: enerji yetmezse Oksijen Tesisleri yavaşlar → oksijen düşebilir → §2.

## 2. Oksijen

- Oksijeni askerler (`troops.tiers[].upkeepOxygen`, mevcut kural) ve üs mürettebatı soluk alır. Mürettebat: saatte `crewOxygenPerReactorLevel` × reaktör seviyesi (açılış: 10; reaktör 20'de saatte 200, tek bir Sv.10 tesisin 1.440'ının yanında küçük).
- **Uyarı (Low):** oksijen azalıyorsa (net < 0) ve deposunun `warnPercent`'inin (%20) altındaysa. Ceza yok; HUD uyarır.
- **Kritik (Critical):** oksijen 0'da ve hâlâ düşüyorsa. Cezalar:
  - oksijen ve enerji dışındaki bütün üretim `outputPenaltyPercent` (−%20) düşer,
  - yeni başlayan inşaat, araştırma ve asker eğitimi `timePenaltyPercent` (+%20) uzun sürer (başlarken hesaplanır; süren zamanlayıcılar değişmez).
- Ceza oksijen ve enerji üretimine **dokunmaz**; böylece ceza kendini besleyen bir sarmala dönüşmez ve oksijen üretmek oyuncuyu her zaman kurtarır.
- Oksijen yükseldiği (net ≥ 0) anda kritik durum biter; toparlanma anında.
- Bina kaybı yok, asker ölümü yok.

## 3. Alarmlar (HUD)

- Oksijen ve Enerji hapı: uyarıda kehribar, kritikte kırmızı nabız.
- Başlığın altında tek satırlık alarm şeridi: önce acil durum, sonra boş oksijen, boş enerji, sonra uyarılar.
- Şeride ya da hapa dokununca kart açılır: ne olduğu, **"Ne yapmalıyım?"** ipucu ve **Git** düğmesi (en düşük seviyeli Oksijen Tesisi ya da Çekirdek Reaktör; tesis yoksa "boş yuvaya kur" ipucu).
- Sunucu durumu üretim özetinde (`ProductionStateDto.Survival`, `SurvivalStateDto`) gönderir: enerji ve oksijen durumu, enerji yüzdesi, cezalar, acil durum ve onarım süreleri, onarım bedeli. Cüzdan değişmeden durum değişirse (depo 0'da sabit) en geç 5 saniyede yine gönderilir.

## 4. Acil durumlar

| | Sızıntı (`leak`) | Arıza (`breakdown`) |
|---|---|---|
| Etki | oksijen üretiminin `leakPercent`'i (%20) kaybolur, en az saatte `leakMinPerHour` (60) | bir üretici (reaktör hariç) hiç üretmez, tüketmez |
| Nerede | bir Oksijen Tesisi (yoksa reaktör) | rastgele bir üretici |

- **Zamanlama:** sunucu, oyuncu başına ve saat başına **bir** tohumlu zar atar (FNV-1a: oyuncu kimliği + saat); şans `chancePercent` (%10). Tür ve bina aynı tohumdan seçilir; yani tekrar üretilebilir.
- **Sınırlar:**
  - yalnızca oyuncu **çevrimiçiyken** başlar; çevrimdışı hiçbir acil durum doğmaz,
  - ilk oturum rehberi sürerken ve reaktör `minReactorLevel`'in (5) altındayken yok,
  - iki başlangıç arası en az `cooldownHours` (6 saat),
  - aynı anda en fazla bir tane (üst üste binmez).
- **Onarım (`RepairEmergency` komutu):** bedel, reaktör seviyesindeki bir saatlik Titanyum gelirinin `repairCostIncomePercent`'i (%10; en az `repairCostMin` = 20). Ödenince `repairSeconds` (10 dk) süren bir onarım başlar. İttifak arkadaşları yardım edebilir (`HelpKinds.Repair`, hedef = acil durum kimliği; her yardım her işte olduğu gibi `helpPercent`/`helpMinSeconds` kadar kısaltır).
- Aynı `CommandId` ile tekrar gönderilen onarım komut defterinden ilk cevabı alır: ikinci kez ödenmez.
- **Kendiliğinden düzelme:** dokunulmayan acil durum `autoResolveHours` (4 saat) sonra biter; hiçbir şey sonsuza kadar kilitlenmez.
- **Görünürlük:** etkilenen binanın üstünde kırmızı yanıp sönen uyarı ışığı ve işaret, sızıntıda yükselen kabarcıklar; başlarken ve biterken bildirim (toast, telefonda titreşim).

## 5. Denge güvenliği

- **Katalog anahtarları:** `survival.enabled` bütün döngüyü (enerji zinciri, oksijen cezaları, acil durumlar) açar/kapar; `survival.emergencies.enabled` yalnızca acil durumları. `survival` bölümü olmayan katalog (tasarım v12 ve öncesi) döngüyü kapalı çalıştırır, yani eski bir sürüme geri dönmek de döngüyü kapatır. Kapatıldığında süren acil durum hemen biter, oranlar o anda normale döner (Overseer → Denge, yeniden başlatma gerekmez).
- **Çevrimdışı telafi:** aynı kurallar uygulanır. Bir depo yolda boşalırsa adım tam o tikte bölünür; ceza ne erken ne geç başlar. Çevrimdışı acil durum başlamaz; süren bir acil durum en çok `autoResolveHours` sürer; çevrimdışı birikim zaten 12 saatle sınırlı.
- **Muhafazakâr açılış değerleri:** enerji tabanı %50, ceza −%20 / +%20, mürettebat saatte reaktör seviyesi × 10, acil durum şansı saatte %10, 6 saat bekleme, 4 saatte kendiliğinden biter.

## 6. Yayın notu

- Katalog tasarım sürümü **13**. İstemciyle birlikte yayınlanırken `/opt/hadal/server/.env` içindeki `HADAL_PUBLISH_CATALOG_UP_TO` (şu an 12) **13**'e yükseltilmeli; yoksa sunucu yeni katalog dosyasını yayınlamaz ve döngü kapalı kalır.
- Eski istemciler `SurvivalStateDto`'yu görmez (yeni Protobuf alanı 6), `RepairEmergency` komutunu gönderemez; acil durumlar onlarda 4 saatte kendiliğinden biter. Etkiler sunucuda olduğundan herkes için aynıdır.

## Testler

- Sunucu: `server/Hadal.Game.Tests/SurvivalTests.cs` — enerji oranı ve taban, depo boşalınca tam o tikte bölünme (çevrimiçi ve çevrimdışı aynı sonuç), oksijen eşikleri, cezalar (üretim ve süre), anında toparlanma ve kayıpsızlık, mürettebat tüketimi, arıza ve sızıntı, onarım (bedel, komut defteriyle tek sefer, yetersiz kaynak), ittifak yardımı, bekleme süresi, rehber ve çevrimdışı istisnası, kendiliğinden düzelme, katalog anahtarları, kalıcılık ve wire.
- Unity EditMode: `SurvivalClientTests` — durum üretim özetiyle çoğaltılır, paketli katalogda anahtar var, alarm metinleri Türkçe ve İngilizce okunur; 18 dilin hepsinde bütün yeni anahtarlar var (`Every_language_has_every_english_string`).
