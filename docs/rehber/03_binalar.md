# 03 · Binalar

> **Özet.** Hadal üssü, Çekirdek Reaktör'ün çevresindeki 12 halkaya kurulur. Kılavuz
> <!--v:hesap.sayilar.bina_turu-->34<!--/v--> bina türü tanımlar: bugün oyunda olan
> <!--v:hesap.sayilar.bina_turu_kodda-->15<!--/v--> türün hepsi korunur, Last Asylum ve Rise of
> Kingdoms'taki rollerin Hadal karşılığı olarak <!--v:hesap.sayilar.bina_turu_yeni-->19<!--/v--> yeni
> tür eklenir. Reaktör 50'de üste <!--v:hesap.sayilar.bina_ornegi-->67<!--/v--> bina örneği
> <!--v:hesap.sayilar.yuva-->124<!--/v--> yuvaya yerleşir. Her binanın seviye seviye maliyeti,
> süresi, gücü ve üretimi kendi sayfasındadır.

Durum: 🟡. Yerleştirme, yükseltme, ücretsiz bitirme, kaldırma, halka açma ve bina işlevleri
(Takviye, Aşırı Yük, kurtarma görevleri, araştırma) kodda çalışıyor. Yeni binalar, seviye dizileri
ve önkoşul zinciri tasarım.

---

## 1. Üssün yapısı

Üs, deniz tabanına oturtulmuş basınçlı modüllerden oluşur. Merkezde **Çekirdek Reaktör** vardır;
çevresindeki her halka bir sonraki yaşam alanıdır.

- **Halka 0:** yalnızca reaktör.
- **Halka 1–3:** üçer yuva; sırasıyla Üretim, Araştırma ve Askeri yuva tipi (kodda var).
- **Halka 4 ve ötesi:** serbest yuvalar; her halkada yuva sayısı artar (6, 8, 10 … 16).
- Halka 0–4 başlangıçta açıktır. 5–12 sırayla, reaktör seviyesi ve maliyetle açılır; 7. halka ve
  ötesi Mega Kubbe ister. Halka açmak inşaatçı kullanmaz, anında olur (kodda var).

<!-- URET:halka-acilislari -->
| Halka | Yuva | Tip | Reaktör | Gerekli | Hedef açılış bedeli | v8 |
| ---: | ---: | --- | ---: | --- | --- | --- |
| 0 | 1 | Serbest | — | — | Başlangıçta açık | — |
| 1 | 3 | Üretim | — | — | Başlangıçta açık | — |
| 2 | 3 | Araştırma | — | — | Başlangıçta açık | — |
| 3 | 3 | Askeri | — | — | Başlangıçta açık | — |
| 4 | 6 | Serbest | — | — | Başlangıçta açık | — |
| 5 | 8 | Serbest | 3 | — | 90 Titanyum | R3 · 400 Titanyum · 100 Biyokütle |
| 6 | 10 | Serbest | 5 | — | 350 Titanyum · 120 Biyokütle | R4 · 800 Titanyum · 200 Biyokütle |
| 7 | 12 | Serbest | 8 | Mega Kubbe | 1.140 Titanyum · 400 Biyokütle | R6 · 1.500 Titanyum · 400 Biyokütle |
| 8 | 14 | Serbest | 12 | Mega Kubbe | 5.100 Titanyum · 1.790 Biyokütle | R7 · 2.500 Titanyum · 700 Biyokütle |
| 9 | 16 | Serbest | 16 | Mega Kubbe | 20.100 Titanyum · 7.050 Biyokütle | R8 · 4.000 Titanyum · 1.100 Biyokütle |
| 10 | 16 | Serbest | 20 | Mega Kubbe | 65.000 Titanyum · 22.800 Biyokütle | R9 · 6.000 Titanyum · 1.600 Biyokütle |
| 11 | 16 | Serbest | 25 | Mega Kubbe | 210.000 Titanyum · 73.500 Biyokütle | R10 · 8.000 Titanyum · 2.200 Biyokütle |
| 12 | 16 | Serbest | 30 | Mega Kubbe | 550.000 Titanyum · 192.000 Biyokütle | R10 · 9.500 Titanyum · 2.800 Biyokütle |

Toplam yuva: **124**.
<!-- /URET:halka-acilislari -->

Hedef takvimde halkalar daha yavaş açılır, çünkü bina sayısı arttı ve tempo uzadı. Açılış bedeli
o reaktör seviyesinin maliyetinin yarısıdır. Yuva kontrolü her reaktör seviyesinde açık yuvanın
bina örneğine yettiğini doğrular:

<!-- URET:slot-tablosu -->
| Reaktör | Açık yuva | Bina örneği | Araştırmayla birlikte |
| ---: | ---: | ---: | ---: |
| 1 | 16 | 4 | 4 |
| 3 | 24 | 13 | 13 |
| 5 | 34 | 25 | 25 |
| 8 | 46 | 36 | 36 |
| 10 | 46 | 41 | 41 |
| 12 | 60 | 46 | 47 |
| 16 | 76 | 55 | 57 |
| 20 | 92 | 60 | 63 |
| 25 | 108 | 60 | 64 |
| 30 | 124 | 62 | 66 |
| 40 | 124 | 63 | 67 |
| 50 | 124 | 63 | 67 |
<!-- /URET:slot-tablosu -->

---

## 2. Bina kuralları

| Kural | Açıklama | Durum |
|---|---|---|
| Reaktör tavanı | Reaktör dışındaki hiçbir bina reaktörün seviyesini geçemez. | ✅ |
| Açılış | Her binanın her örneği belirli bir reaktör seviyesinde açılır. | ✅ (sayılar 🟡) |
| Adet sınırı | İnşaat halindekiler dahil, en fazla örnek sayısı. Bazı binalara araştırma ek örnek verir. | ✅ |
| İnşaatçı | 1 inşaatçı; 2. araştırmadan, 3. aylık karttan (K-07). | 🟡 |
| Ücretsiz bitirme | Son 5 dakika ücretsiz; Mühendislik Bölmesi 15 dakikaya kadar uzatır. | ✅ (uzatma ⬜) |
| Yükseltme sırasında üretim | Bina yükselirken eski seviyesinde üretmeye devam eder. | ✅ |
| Kaldırma | Bitmiş bina kaldırılırsa harcanan maliyetin %50'si döner; ilk inşaat iptalinde %100. Reaktör kaldırılamaz, yükselen bina kaldırılamaz. | ✅ |
| Önkoşul zinciri | Reaktörün her seviyesi belirli binaları belirli seviyede ister. | ⬜ |
| Seviye 50 kapısı | Bazı binalar 49 → 50 için Kadim Şema ister. | ⬜ |

Maliyet, süre ve güç formülleri: [02 · Tempo ve Denge](02_tempo_ve_denge.md). Binaların görünüşünün
seviyeyle nasıl değiştiği: [04 · Bina Evrimi](04_bina_evrimi.md).

---

## 3. Tüm binalar

<!-- URET:bina-ozet -->
| Bina | Kategori | Durum | Açılış (Reaktör) | Adet | Maks Sv | İşlev |
| --- | --- | --- | ---: | ---: | ---: | --- |
| [Çekirdek Reaktör](./binalar/core_reactor.md) | Merkez | 🟡 | Başlangıç | 1 | 50 | Enerji üretir |
| [Oksijen Tesisi](./binalar/oxygen_plant.md) | Üretim | 🟡 | 1 | 6 | 50 | Oksijen üretir |
| [Gıda Laboratuvarı](./binalar/food_lab.md) | Üretim | 🟡 | 1 | 4 | 50 | Gıda üretir |
| [Biyo Laboratuvar](./binalar/bio_lab.md) | Üretim | 🟡 | 2 | 3 | 50 | Biyokütle üretir |
| [Hidroponik Çiftlik](./binalar/hydroponic_farm.md) | Üretim | 🟡 | 3 | 3 | 50 | Gıda üretir, biyokütle ve enerji tüketir |
| [Dron Fabrikası](./binalar/drone_factory.md) | Üretim | 🟡 | 2 | 5 | 50 | Titanyum üretir |
| [Araştırma Merkezi](./binalar/research_center.md) | Araştırma | 🟡 | 3 | 1 | 50 | Birinci laboratuvar: merkez tezgâhı ve esnek İkinci Tezgâh |
| [Denizaltı İskelesi](./binalar/submarine_dock.md) | Askeri | 🟡 | 4 | 1 | 50 | Denizaltı inşası ve onarımı |
| [Radar Merkezi](./binalar/radar_center.md) | Askeri | 🟡 | 4 | 1 | 50 | Yaklaşan saldırıları erken haber verir, sisi açan sonar dronlarını gönderir, Tür Kataloğu taramaları yapar |
| [Askeri Kubbe](./binalar/military_dome.md) | Askeri | 🟡 | 5 | 1 | 50 | Asker eğitimi kuyruğu ve ordu kapasitesi |
| [Kalkan Jeneratörü](./binalar/shield_generator.md) | Askeri | 🟡 | 5 | 1 | 50 | Üs dayanıklılığı ve savunma bonusları; doğa olaylarına direnç |
| [Mega Kubbe](./binalar/mega_dome.md) | Kubbe | 🟡 | 6 | 1 | 5 | Mürettebat kapasitesi ve oksijen; 7. halka ve ötesini açar |
| [Tarım Kubbesi](./binalar/agriculture_dome.md) | Kubbe | 🟡 | 7 | 1 | 5 | Bol gıda ve biyokütle; günlük hasat ödülü |
| [İttifak Merkezi](./binalar/alliance_hub.md) | İttifak | 🟡 | 6 | 1 | 50 | İttifak yardımı: yardım sınırı 10 + ⌊seviye / 5⌋ (en fazla 20) |
| [Süs Kulesi](./binalar/decoration_spire.md) | Süs | 🟡 | 2 | 6 | 50 | Görsel; küçük güç katkısı |
| [Kriyo Salonu](./binalar/cryo_hall.md) | Destek | ⬜ | 2 | 1 | 50 | Kriyo-Kapsül çekimleri; ücretsiz çekim aralığı ve kahraman kadrosu kapasitesi seviyeyle iyileşir |
| [Basınç Ambarı](./binalar/pressure_vault.md) | Depolama | ⬜ | 3 | 3 | 50 | Her kaynak için ek kapasite verir ve bir kısmını yağmadan korur |
| [Mühendislik Bölmesi](./binalar/engineering_bay.md) | Destek | ⬜ | 4 | 1 | 50 | Ücretsiz bitirme süresini 5 dakikadan 15 dakikaya kadar uzatır |
| [Denizaltı Hangarı](./binalar/submarine_hangar.md) | Askeri | ⬜ | 4 | 4 | 50 | Her hangar bir yürüyüş kuyruğudur (en fazla 4) |
| [Tatbikat Havuzu](./binalar/training_grounds.md) | Askeri | ⬜ | 5 | 1 | 50 | Asker kademelerini açar ve eğitim partisini büyütür |
| [Mürettebat Yatakhanesi](./binalar/crew_quarters.md) | Destek | ⬜ | 5 | 4 | 50 | Mürettebat kapasitesi; uzman personel binalara atanır ve kendi becerilerini getirir |
| [Revir](./binalar/med_bay.md) | Askeri | ⬜ | 6 | 2 | 50 | Yaralı asker kapasitesi ve iyileştirme hızı |
| [Kriyo Bakım Ünitesi](./binalar/cryo_ward.md) | Askeri | ⬜ | 16 | 1 | 50 | Savunmada Revir taştığında ölecek askerlerin %50'sini kurtarır; kurtarılabilecek asker sayısı seviyeyle artar |
| [Hidrotermal Santral](./binalar/hydrothermal_plant.md) | Üretim | ⬜ | 6 | 3 | 50 | Enerji üretir |
| [Takas İstasyonu](./binalar/trade_station.md) | Destek | ⬜ | 10 | 1 | 50 | İttifak içi kaynak gönderme (vergi %30'dan %10'a iner) |
| [Bilim Kubbesi](./binalar/science_dome.md) | Kubbe | ⬜ | 12 | 1 | 5 | İkinci laboratuvar: kubbe tezgâhı |
| [Savaş Kubbesi](./binalar/war_dome.md) | Kubbe | ⬜ | 16 | 1 | 5 | Basınç Arenası ve ralli (toplu saldırı) kapasitesi. 4. ve 5. seviyeler Basınç Kodeksi ister |
| [Teçhizat Atölyesi](./binalar/gear_workshop.md) | Destek | ⬜ | 12 | 1 | 50 | Teçhizat üretimi ve malzeme birleştirme; yüksek seviyeler üst nadirlikleri açar |
| [Dalgıç Anıtı](./binalar/armor_monument.md) | Anıt | ⬜ | 14 | 1 | 50 | Zırhlı Dalgıçların Ark Askerlerine karşı avantajını artırır |
| [Zıpkın Anıtı](./binalar/harpoon_monument.md) | Anıt | ⬜ | 14 | 1 | 50 | Zıpkıncıların Zırhlı Dalgıçlara karşı avantajını artırır |
| [Ark Anıtı](./binalar/arc_monument.md) | Anıt | ⬜ | 14 | 1 | 50 | Ark Askerlerinin Zıpkıncılara karşı avantajını artırır |
| [Kalıntı Arşivi](./binalar/relic_archive.md) | Destek | ⬜ | 15 | 1 | 50 | Kalıntı koleksiyonu ve Tür Kataloğu; tamamlanan setler kalıcı bonus verir |
| [Alaşım Dökümhanesi](./binalar/alloy_foundry.md) | Üretim | ⬜ | 30 | 2 | 50 | Süperalaşım üretir |
| [Kronik Feneri](./binalar/chronicle_beacon.md) | Destek | ⬜ | 8 | 1 | 50 | Derinlik Kroniği'ni gösterir; kronik hedeflerine katkıyı ve ödülleri buradan alırsın |
<!-- /URET:bina-ozet -->

Her satırdaki bağlantı binanın sayfasına gider. Sayfada şunlar bulunur:
- işlev ve açılış bilgisi, adet, halka aralığı;
- görünüm evrimi (6 katman);
- seviye kilitleri (o seviyede açılan özellikler);
- **50 satırlık seviye tablosu**: gereken reaktör, kaynak başına maliyet, ham süre, eklenen güç,
  üretim ve kümülatif süre;
- kodda olan binalar için bugünkü oyunla (catalog v8) karşılaştırma.

---

## 4. Kategoriler

### Merkez
**Çekirdek Reaktör** üssün tavanıdır: hiçbir bina onu geçemez; halkaları, yeni binaları ve inşaat
seçeneklerini o açar. Aşırı Yük (Overdrive) 1 saat %50 fazla enerji verir, 8 saatte bir, ücretsiz
(kodda var). Reaktör seviyeleriyle açılanların tam listesi reaktör sayfasındadır.

### Üretim
Oksijen Tesisi, Gıda Laboratuvarı, Biyo Laboratuvar, Hidroponik Çiftlik, Dron Fabrikası, Hidrotermal
Santral ve Alaşım Dökümhanesi. Her biri gerçek bir derin deniz yöntemine dayanır: elektroliz,
kemosentetik bakteri kültürü, bakteri matları, LED hidroponik, enkaz ve kabuk toplayan dronlar,
baca ısısından elektrik, nodül metallerinden süperalaşım ([18 · Bilim Temeli](18_bilim_temeli.md)).
Üretim binalarında **Takviye** (30 dakika %30, 150 Enerji; kodda var) ve mürettebat ataması
vardır. Dron Fabrikası ayrıca **kurtarma görevleri** gönderir (kodda var).

### Depolama
**Basınç Ambarı** tavanın üstüne ek kapasite verir ve stoğun bir kısmını yağmadan korur. Last
Asylum'daki üretim binası + koruyan depo ikilisinin Hadal karşılığıdır; ama her kaynak için ayrı
depo yerine tek tip, üç örnekli bir ambar vardır: üs kalabalıklaşmaz.

### Araştırma
Araştırma **iki laboratuvar, üç tezgâhla** yürür. **Araştırma Merkezi** birinci laboratuvardır ve
reaktör önkoşul zincirindedir; merkez tezgâhını ve araştırmayla açılan esnek İkinci Tezgâh'ı taşır.
**Bilim Kubbesi** ikinci laboratuvardır; kubbe tezgâhıyla askeri ve ileri dalları açar. Ayrıntı:
[06 · Araştırma](06_arastirma.md).

### Askeri
Askeri Kubbe (kışla), Tatbikat Havuzu (kademe ve parti), Revir (yaralılar), Kriyo Bakım Ünitesi
(taşma kurtarma), Kalkan Jeneratörü (duvar), Radar Merkezi (keşif, uyarı, sonar dronları),
Denizaltı İskelesi (gövde basınç sınıfı) ve dört Denizaltı Hangarı (yürüyüş kuyrukları). Ayrıntı:
[07 · Birlikler ve Savaş](07_birlikler_ve_savas.md), [08 · Denizaltılar, Basınç, Seferler](08_denizaltilar_basinc_seferler.md).

### Kubbeler
Dört büyük kubbe, her biri 5 seviye (K-16): **Mega Kubbe** (yaşam alanı, 7. halka ve ötesi),
**Tarım Kubbesi** (bol gıda), **Bilim Kubbesi** (2. laboratuvar), **Savaş Kubbesi** (arena ve ralli).
Kubbe seviyeleri reaktör kapılarına bağlıdır (20 / 30 / 40 / 45) ve kritik yolun kilometre taşlarıdır.

### Destek
Kriyo Salonu (kahraman uyandırma), Mühendislik Bölmesi (ücretsiz bitirme), Mürettebat Yatakhanesi
(mürettebat ve uzman personel), Takas İstasyonu (ittifak içi kaynak gönderme, gezgin kaçakçı),
Teçhizat Atölyesi, Kalıntı Arşivi (koleksiyon ve Tür Kataloğu) ve Kronik Feneri (Derinlik Kroniği).

### Anıtlar
Dalgıç, Zıpkın ve Ark Anıtı sayaç üçgenindeki avantajı büyütür; Last Asylum'daki sınıf heykellerinin
karşılığıdır, ama kahraman sınıfı yerine **birlik türüne** bağlıdır.

### İttifak ve süs
İttifak Merkezi yardım sınırını ve takviye kapasitesini büyütür ([10 · İttifak](10_ittifak.md)).
Süs Kulesi görseldir ve küçük güç verir.

---

## 5. Last Asylum ve Rise of Kingdoms rolleriyle eşleşme

| Rol | Last Asylum / Rise of Kingdoms'taki karşılık (genel) | Hadal |
|---|---|---|
| Merkez bina | Her şeyin seviye tavanı | Çekirdek Reaktör |
| Kaynak üretimi | Çiftlik, kereste, taş, altın | Oksijen, Gıda, Biyo, Hidroponik, Dron, Hidrotermal, Alaşım |
| Yağmadan koruma | Kaynak başına depo | Basınç Ambarı (tek tip, 3 örnek) |
| İki laboratuvar | Birinci ve ikinci araştırma kuyruğu | Araştırma Merkezi (merkez tezgâhı + İkinci Tezgâh) + Bilim Kubbesi (kubbe tezgâhı) |
| Takımlar 1–4 | Yürüyüş kuyrukları | Denizaltı Hangarı ×4 |
| Kışla ve eğitim alanı | Asker kapasitesi, kademe | Askeri Kubbe + Tatbikat Havuzu |
| Revir | Ağır yaralılar | Revir + Kriyo Bakım Ünitesi |
| Duvar | Şehir dayanıklılığı | Kalkan Jeneratörü |
| Gözetleme kulesi, keşif kampı | Uyarı ve sis | Radar Merkezi |
| İnşaatçı kulübesi | Boş zaman, ek inşaatçı | Mühendislik Bölmesi (ek inşaatçı araştırmada) |
| Meyhane | Kahraman alımı | Kriyo Salonu |
| Konut | Hayatta kalan yetenekleri | Mürettebat Yatakhanesi |
| Göçebe tüccar, ahır, ticaret | Takas, kervan | Takas İstasyonu |
| Nadide salonu | Koleksiyon | Kalıntı Arşivi |
| Sınıf heykelleri | Sayaç üçgeni | Dalgıç / Zıpkın / Ark Anıtı |
| Arena | Kahraman gösterisi | Savaş Kubbesi |
| Anıt (kronik) | Krallık zaman çizelgesi | Kronik Feneri |
| İttifak salonu | Yardım, takviye | İttifak Merkezi |

Hadal'ın farkı: üs dairesel ve halkalı; binalar seviye seviye **görünür biçimde evrilir**; üretim
yöntemleri gerçek derin deniz biliminden gelir.

---

## 6. Bina işlevleri paneli

Her binanın panelinde (kodda var, `docs/24_Building_Functions.md`): başlık, seviye ve durum →
işlev kartı → Yükselt → Kaldır. Henüz yapılmamış işlevler "Yakında" kartı olarak görünür; boş yer
bırakılmaz. Hedef tasarımda panel ayrıca şunları gösterir:
- bir sonraki seviyenin **evrim önizlemesi** (katman değişiyorsa),
- reaktör önkoşul zincirinde ise "Reaktör N için gerekli" etiketi,
- seviye kilidi açacaksa açılacak özellik.

---

## 7. Uygulama notları

| Değişiklik | Kod |
|---|---|
| 19 yeni bina türü | `shared/catalog/catalog.json` `buildings[]`; Unity `Hadal > Setup` ScriptableObject üretimi; `art/blender/buildings.py` |
| Yuva tipi | Halka 4+ serbest yuva; `PlacementRules.MatchesSlotType` değişmeden çalışır |
| Halka takvimi | `catalog.json` `ringUnlocks` (reaktör seviyeleri ve maliyetler) |
| Önkoşul zinciri | `BuildingProgression.ValidateUpgrade` + yeni red nedeni; istemcide panel etiketi |
| Askeri Kubbe ×1 | `military_dome.maxCount = 1` (K-15) |
| Kubbe kapıları | `maxLevel: 5` + seviye başına gereken reaktör |
| Bina sayfaları | Bu kılavuzun `binalar/` sayfaları `tools/rehber/uret.py` ile `veri/binalar.json`'dan üretilir |

## 8. Açık sorular

- Halka 1–3'teki tipli yuvalar (Üretim / Araştırma / Askeri) yeni binalarla dolunca oyuncu yer
  değiştirme isteyecek mi? Taşıma (ücretsiz yer değiştirme) eklenmeli mi?
- Süs Kulesi'nin 6 örneği yerine kozmetik dekor sistemi mi gelmeli?
