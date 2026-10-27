# 17 · Teçhizat

> Teçhizat, bir Kaptan'ı filosuna göre biçer. Dört malzeme okyanusun kendi dokusundan gelir:
> amfipod kabuğunun kitini, yastık lavın camı, kara bacaların sülfürü ve derin denizaltıları
> yüzdüren sintaktik köpük. Atölyede birleşir, setlere dönüşür ve yalnızca Kaptan'da çalışır.

Bağlayıcı kararlar: [00 · Kanon](00_kanon.md) (K-01: teçhizat malzemeleri kaynak değil eşyadır,
K-14 güç, K-23 bilim verisi, K-25 canlılar avlanmaz). Veri: `veri/techizat.json`. Kahraman tarafı:
[05 · Kahramanlar](05_kahramanlar.md). Bina: [Teçhizat Atölyesi](binalar/gear_workshop.md).

---

## 1. Amaç

1. **Kaptan'ı role göre ayarlamak.** Aynı Efsanevi kahraman bir gün Zırhlı duvarı, ertesi gün
   toplama filosunu yönetebilir; ağaç dizilimi ve teçhizat takımı bu değişimi taşır.
2. **Harita ve etkinliklere ikinci bir ödül ekseni.** Yataklar, canavarlar ve keşifler kaynağın
   yanında malzeme de verir; oyuncu "bugün nereye gideyim" sorusuna bir neden daha bulur.
3. **Uzun vadeli, adil bir hedef.** Teçhizatın tavanı yüksektir ama rastgele çekilişle değil
   oynayarak ve üreterek dolar. Satın alınan her şeyin içeriği önceden yazılıdır.

## 2. Temel kurallar

- Her kahramanın <!--v:veri.techizat.kurallar.yuva_sayisi-->6<!--/v--> teçhizat yuvası vardır: kask,
  gövde zırhı, eldiven, palet, silah, modül.
- Teçhizatı her kahraman taşıyabilir, ama **yalnızca Kaptan'ken çalışır**. Subaylar ve Komuta
  Ekibi teçhizattan yararlanmaz; arayüz Subay'ın üzerindeki teçhizatı soluk gösterir ve "Kaptan
  olunca çalışır" yazar.
- Değerler yüzdedir ve Kaptan'ın **filosundaki askerlere** uygulanır (kahramanın kendi ATK'sına
  değil). Komuta bonusu ([05](05_kahramanlar.md) bölüm 3.2), ağaç ve teçhizat toplanır.
- Takma ve çıkarma ücretsiz ve anlıktır. Filo üs dışındayken (yürüyüş, toplama, garnizon, ralli)
  o filonun kahramanlarının teçhizatı kilitlidir.
- Teçhizatın seviyesi yoktur; gücünü nadirlik, set ve Usta İşi belirler. Bu, kahraman
  değiştirmenin bedelini sıfırda tutar: aynı takım bir kahramandan ötekine aynen geçer.
- Teçhizat kahraman nadirliğine bağlı değildir: Sıradan bir kahraman Efsanevi teçhizat giyebilir.

## 3. Malzemeler

<!-- URET:tablo:techizat.malzemeler -->
| Malzeme | Doğada | Oyunda nereden | Teçhizattaki rolü | Tür Kataloğu bağı |
| --- | --- | --- | --- | --- |
| Kitin | Kabukluların dış iskeleti: kitin-protein lif ağına çökelmiş kalsiyum karbonat. Hayvan her deri değiştirmede iskeleti yeniden kurar. | Hadal amfipodlarının deri değiştirirken bıraktığı kabuklar ve leş alanlarında biriken kabuk tabakası; canlılara dokunulmadan toplanır. | Esnek zırh levhası, conta ve eldiven | Dev Hadal Amfipodu kaydı bilgi kartını açar |
| Bazalt camı | Su altında püsküren bazalt lavın dış yüzü bir saniyeden kısa sürede cama dönüşür; yastık lavların camsı kabuğu. | Deniz dağı yamaçlarındaki yastık lavlardan ve fay çatlaklarından kesilen cam kabuk. | Basınca dayanıklı vizör, yük taşıyan kaplama, silah gövdesi | — |
| Masif sülfür | Kara bacalarda kabuktan çözülen metallerin soğuk suyla karşılaşınca çökelmesiyle oluşan bakır, çinko ve demir sülfürleri. | Sönmüş baca yığınlarından alınan cevher; korunan türlerin yaşadığı aktif bacalar kazı dışıdır. | Metal aksam, silah ucu, iletken modül | Pullu Ayaklı Salyangoz kaydı bilgi kartını açar |
| Sintaktik köpük | İçi boş cam mikrokürelerin epoksi reçineye gömülmesiyle yapılan, sudan hafif ama basınca dayanıklı köpük; derin denizaltıların kaldırma kuvvetini sağlar. | Teçhizat Atölyesi'nin Köpük Hattı'nda dökülür; eski denizaltı enkazlarının yüzdürme bloklarından da sökülür. | Yüzerlik, palet ve gövde dolgusu, modül yuvası | — |
<!-- /URET:tablo:techizat.malzemeler -->

### Bilim notu

Malzemelerin doğadaki karşılıkları [18 · Bilim Temeli](18_bilim_temeli.md) verisine dayanır.

- **Kitin.** <!--v:veri.bilim.mineraller.kitin.aciklama-->Kabukluların dış iskeleti, kitin-protein lif ağının içine çökelmiş kalsiyum karbonattan (kristal ya da amorf) yapılmış doğal bir kompozittir; mineral iskeleti sertleştirir. Hayvan her deri değiştirmede bu iskeleti yeniden kurar.<!--/v-->
  Oyunda kitin, hadal amfipodlarının deri değiştirirken bıraktığı kabuklardan ve balina leşi
  alanlarında biriken kabuk tabakasından toplanır; hiçbir canlı bunun için avlanmaz (K-25).
  Malzemenin bilgi kartı <!--v:veri.bilim.turler.hadal_amphipod.ad-->Dev Hadal Amfipodu<!--/v--> kaydına
  bağlanır.
- **Bazalt camı.** <!--v:veri.bilim.mineraller.bazalt_cami.aciklama-->Okyanus ortası sırtlarda oluşan yeni kabuğun çoğu yastık biçimli bazalt lav akıntılarıdır. Sıcak bazalt suyla temas edince dış yüzü esnek bir cam kabuğa dönüşür; kabuk yırtılınca yeni lav 'diş macunu gibi' çıkıp bir sonraki yastığı yapar. Derin deniz tabanında lav yüzeyi bir saniyeden kısa sürede cama dönüşür.<!--/v-->
- **Masif sülfür.** <!--v:veri.bilim.mineraller.masif_sulfur.aciklama-->Magma ile ısınan deniz suyunun kabuktan çözdüğü metallerin, baca ağzında soğuk ve oksijenli suyla karşılaşınca çökelmesiyle oluşur (kara bacalar). Bakır, çinko, kurşun, altın ve gümüş kaynağıdır; başlıca cevher mineralleri kalkopirit (bakır) ve sfalerittir (çinko). 1979'dan beri ~430 yatak bulundu, ~300'ü hâlâ aktif. Aktif yayılma sırtları boyunca toplam ~600 milyon ton sülfür olduğu tahmin ediliyor; çoğu yatak küçüktür.<!--/v-->
  Oyunda yalnızca sönmüş baca yığınları kazılır; korunan türlerin yaşadığı aktif bacalar kazı
  dışıdır. Bilgi kartı <!--v:veri.bilim.turler.scaly_foot_snail.ad-->Pullu Ayaklı Salyangoz<!--/v--> kaydına
  bağlanır: kabuğunu demir sülfürle zırhlayan bu salyangoz, eldiven ve kask tasarımının sessiz
  ilham kaynağıdır.
- **Sintaktik köpük.** <!--v:veri.bilim.mineraller.sintaktik_kopuk.aciklama-->İçi boş cam mikrokürelerin epoksi reçineye gömülmesiyle yapılan, sudan hafif ama basınca dayanıklı köpük; derin denizaltıların kaldırma kuvvetini sağlar. Derinlik arttıkça daha yoğun ve daha dayanıklı (dolayısıyla daha az kaldıran) köpük gerekir. Deepsea Challenger'ın hacminin ~%70'i taşıyıcı omurga olarak da çalışan özel bir sintaktik köpüktü; Shinkai 6500 ve Alvin de kaldırma için bu malzemeyi kullanır.<!--/v-->
  Doğada bulunmaz, üretilir: bu yüzden Atölye'nin Köpük Hattı onun güvenceli kaynağıdır.

### Nereden gelir?

<!-- URET:tablo:techizat.malzeme_kaynaklari -->
| Kaynak | Malzeme | Kural | Bölüm |
| --- | --- | --- | --- |
| Balina Leşi yatağı | Kitin | Leşçil evrede her tam toplamada %15 şans, 1–2 adet | 09 (öneri) |
| Uçurum Sürüngenleri | Kitin | Zafer ödülünde | 09 ✓ |
| Kobalt Kabuğu yatağı | Bazalt camı | Her tam toplamada %15 şans, 1–2 adet | 09 (öneri) |
| Siren Sürüsü | Bazalt camı | Zafer ödülünde | 09 ✓ |
| Masif Sülfür Bacası yatağı | Masif sülfür | Her tam toplamada %15 şans | 09 ✓ |
| Demir Leviathan Yuvası | Masif sülfür | Ralli katılımcısına 2–6 adet | 09 (öneri) |
| Enkaz Alanı yatağı | Sintaktik köpük | Her tam toplamada %10 şans, 1 adet | 09 (öneri) |
| Kadim Muhafız Devriyesi | Sintaktik köpük | Zafer ödülünde | 09 ✓ |
| Köpük Hattı (Teçhizat Atölyesi) | Sintaktik köpük | Atölye seviyesine göre (Köpük Hattı tablosu) | 17 |
| Fay Çatlağı II keşfi | Dördü (rastgele) | Dron keşfi ödülü | 09 ✓ |
| Hikâye bölümü 6 (Kadim İz) | Dördü | Atölye açılışında başlangıç paketi | 15 ✓ |
| Muhafız Mahzeni ve Sürü Baskını etkinlikleri | Dördü | Kademe ödülleri; Destansı ve Efsanevi çizim | 13 |
| Batık enkaz (sefer olayı) | Bölgenin malzemesi | Kurtarılan her enkazdan 1 adet; hedefli dron kurtarmasında garanti | 08 ✓ |
| Sefer dükkânı | Bölümün malzemesi | Teçhizat malzemesi kutusu, haftada 5 | 08 ✓ |
| Derinlik İzni dükkânı | Seçmeli | Teçhizat malzemesi kutusu, haftada 3 | 11 ✓ |
| İttifak Dükkânı | Seçmeli | Haftalık sınırla; Destansı çizim haftada 1 | 10 |
| Kalıntı Arşivi | Dördü | Koleksiyon seti tamamlanınca | 16 |
| The Syndicate | Seçmeli | İçeriği yazılı Atölye Sandıkları; rastgele çekiliş yok | 12 |
<!-- /URET:tablo:techizat.malzeme_kaynaklari -->

Kaynağın seviyesi düşen malzemenin nadirliğini belirler; harita merkeze doğru derinleştikçe
yataklar ve düşmanlar yükselir ([09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md)).

<!-- URET:tablo:techizat.dusme_nadirligi -->
| Kaynağın seviyesi | Düşen malzemenin nadirliği |
| --- | --- |
| Yatak Sv 1–2 · düşman Sv 1–10 | Sıradan |
| Yatak Sv 3–4 · düşman Sv 11–20 | Gelişmiş (%70) · Sıradan (%30) |
| Yatak Sv 5 · düşman Sv 21–30 | Nadir (%60) · Gelişmiş (%40) |
| Yatak Sv 6–7 · düşman Sv 31–40 | Destansı (%40) · Nadir (%60) |
| Yatak Sv 8 · düşman Sv 41–50 · Leviathan 5–6 | Efsanevi (%15) · Destansı (%50) · Nadir (%35) |
<!-- /URET:tablo:techizat.dusme_nadirligi -->

**Birleştirme.** Aynı malzemenin <!--v:veri.techizat.kurallar.birlestirme_oran-->4<!--/v--> tanesi bir üst
nadirlikte 1 tane olur. Birleştirme anlıktır, Titanyum ister ve "Hepsini birleştir" tek dokunuştur;
oyuncu hangi nadirlikte duracağını seçebilir.

<!-- URET:tablo:techizat.birlestirme -->
| Birleştirme | Oran | Titanyum (birleştirme başına) | Atölye Sv |
| --- | --- | ---: | ---: |
| Sıradan → Gelişmiş | 4 → 1 | 1.000 | 1 |
| Gelişmiş → Nadir | 4 → 1 | 5.000 | 15 |
| Nadir → Destansı | 4 → 1 | 25.000 | 20 |
| Destansı → Efsanevi | 4 → 1 | 100.000 | 25 |
<!-- /URET:tablo:techizat.birlestirme -->

**Ayrıştırma.** Bir malzeme bir alt nadirlikte <!--v:veri.techizat.kurallar.ayristirma_oran-->3<!--/v-->
tane olur (Atölye <!--v:veri.techizat.atolye_kilitleri.2.seviye-->15<!--/v-->, bedelsiz). Kayıplıdır; amacı eksik kalan alt nadirlik malzemesini
tamamlamaktır, birleştirme ile ayrıştırma arasında kazanç döngüsü kurulamaz.

### İkinci kullanım: denizaltı modülleri

Aynı dört malzeme denizaltıların modüllerini de yükseltir
([08 · Denizaltılar, Basınç, Seferler](08_denizaltilar_basinc_seferler.md)). Modüller kaptana değil
denizaltıya bağlıdır; malzeme bütçesi (bölüm 10) iki kullanımı birlikte taşır.

<!-- URET:tablo:techizat.ikinci_kullanim -->
| Nerede | Malzeme | Etki |
| --- | --- | --- |
| Basınç Küresi (denizaltı modülü) | Titanyum + bazalt camı | Açıkta gövde kaybını azaltır (08) |
| Yüzdürme Bloğu (denizaltı modülü) | Sintaktik köpük | Seyir hızı; 5. seviyede bölmeli yapı (08) |
| Sonar Dizisi (denizaltı modülü) | Kitin + masif sülfür | Sonar aşamasında önceden görüş, tür taraması (08) |
<!-- /URET:tablo:techizat.ikinci_kullanim -->

Bu yüzden oyuncu için doğru soru "bu malzeme Kaptan'a mı, denizaltıya mı?"dır. Erken oyunda
modüller, geç oyunda Efsanevi setler daha çok getiri sağlar; arayüz her malzemenin iki kullanımını
aynı bilgi kartında gösterir.

## 4. Nadirlikler

<!-- URET:tablo:techizat.nadirlikler -->
| Nadirlik | Renk | Atölye Sv | En erken Reaktör | Parça gücü | Setler |
| --- | --- | ---: | ---: | ---: | --- |
| Sıradan | Kırık beyaz | 1 | 12 | 200 | Setsiz standart parçalar |
| Gelişmiş | Deniz yeşili | 15 | 15 | 500 | Setsiz standart parçalar |
| Nadir | Derin mavi | 20 | 20 | 1.200 | Kurtarma Ekibi |
| Destansı | Biyolüminesan mor | 25 | 25 | 3.000 | Sekiz birlik ve rol seti |
| Efsanevi | Hadalit altını | 35 | 35 | 7.500 | Dört geç oyun seti |
<!-- /URET:tablo:techizat.nadirlikler -->

Atölye [Teçhizat Atölyesi](binalar/gear_workshop.md) Reaktör
<!--v:veri.binalar.binalar.gear_workshop.adet-->12<!--/v-->'de açılır ve hiçbir bina gibi reaktörü geçemez.
Her nadirliğin açıldığı Atölye seviyesi, o reaktör seviyesinin Titanyum tavanına sığacak biçimde
seçildi: hiçbir parça, onu açabilen oyuncunun cüzdanına sığmayan bir bedel istemez.
Beşinci nadirlik **Gelişmiş**, kahraman nadirliklerinin arasına yalnızca teçhizatta girer.

## 5. Yuvalar ve değerler

Her yuvanın bir ana ve bir yan özelliği vardır. Yan özellik ana değerin
<!--v:veri.techizat.kurallar.yan_oran-->0,5<!--/v--> katıdır. Tablo ana özelliğin nadirliğe göre değeridir.

<!-- URET:tablo:techizat.yuvalar -->
| Yuva | Ana özellik | Yan özellik (ana × 0,5) | Ana + yan malzeme | Sıradan | Gelişmiş | Nadir | Destansı | Efsanevi |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| Kask | Birlik DEF | Birlik HP | Bazalt camı + Kitin | %1 | %2 | %3 | %5 | %7 |
| Gövde zırhı | Birlik HP | Birlik DEF | Kitin + Sintaktik köpük | %1 | %2 | %3 | %5 | %7 |
| Eldiven | Birlik ATK | Öfke kazanımı | Kitin + Masif sülfür | %1 | %2 | %3 | %5 | %7 |
| Palet | Yürüyüş hızı | Birlik DEF | Sintaktik köpük + Bazalt camı | %2 | %4 | %6 | %9 | %12 |
| Silah | Birlik ATK | Normal saldırı hasarı | Masif sülfür + Bazalt camı | %1,5 | %3 | %4,5 | %7 | %10 |
| Modül | Aktif yetenek hasarı | Birlik ATK | Sintaktik köpük + Masif sülfür | %2 | %4 | %6 | %9 | %12 |
<!-- /URET:tablo:techizat.yuvalar -->

- Silah, ATK'nın en büyük kaynağıdır; eldiven ATK'yı öfkeyle bölüşür.
- Palet yürüyüş hızı verir: harita kaptanları için silahtan değerli olabilir.
- Modül aktif yetenek hasarı verir; Ark ve Beceri dizilimli Kaptanların yuvasıdır.
- Her parçanın adı set adı ile yuvadan oluşur (ör. "Abis Levhası Kaskı").

## 6. Setler

Destansı ve Efsanevi parçaların hepsi bir sete aittir; Nadir'de tek bir başlangıç seti vardır.
Sıradan ve Gelişmiş parçalar setsizdir.

<!-- URET:tablo:techizat.setler -->
| Set | Nadirlik | Birlik / rol | Atölye Sv | 2 parça | 4 parça | 6 parça |
| --- | --- | --- | ---: | --- | --- | --- |
| Kurtarma Ekibi | Nadir | Tüm birlikler | 20 | HP +%2 | DEF +%2 | ATK +%2 |
| Abis Levhası | Destansı | Zırhlı Dalgıç | 25 | Zırhlı DEF +%3 | Zırhlı HP +%4 | Zırhlı Dalgıçların aldığı hasar −%4 |
| Akıntı Kancası | Destansı | Zıpkıncı | 25 | Zıpkıncı ATK +%3 | Yürüyüş hızı +%5 | Zıpkıncı normal saldırı hasarı +%5 |
| Fırtına Bobini | Destansı | Ark Askeri | 25 | Ark ATK +%3 | Ark HP +%4 | Aktif yetenek hasarı +%6 |
| Kuyu Matkabı | Destansı | Delici | 25 | Delici ATK +%4 | Yapılara verilen hasar +%6 | Delici kayıplarının %10'u ölü yerine yaralı |
| Kubbe Nöbetçisi | Destansı | Garnizon (tüm birlikler) | 25 | Üs savunmasında DEF +%4 | Üs savunmasında HP +%5 | Üs savunmasında alınan hasar −%5 |
| Nodül Hasatçısı | Destansı | Toplama | 25 | Toplama hızı +%5 | Denizaltı yük kapasitesi +%8 | Toplamada teçhizat malzemesi şansı +%10 |
| Sürü Avcısı | Destansı | Canavar avı | 25 | Canavarlara verilen hasar +%5 | Canavar savaşında kayıplar −%5 | Demir Leviathan ve Hadal Titanı'na hasar +%8 |
| Hadal Kâşifi | Destansı | Sefer | 25 | Seferde basınç hasarı −%5 | Sefer ödülleri +%8 | Bir basınç sınıfı derindeki bölgelerde gövde hasarı −%10 |
| Basınç Kalesi | Efsanevi | Zırhlı Dalgıç | 35 | Zırhlı DEF +%5 | Zırhlı HP +%6 | Her 4 turda bir, Zırhlılar 1 tur %20 daha az hasar alır |
| Karanlık Akıntı | Efsanevi | Zıpkıncı | 35 | Zıpkıncı ATK +%5 | Yürüyüş hızı +%8 | İlk 3 turda Zıpkıncı hasarı +%10 |
| Kadim Rezonans | Efsanevi | Ark Askeri | 35 | Ark ATK +%5 | Öfke kazanımı +%6 | Aktif yetenek hasarı +%10 |
| Muhafız Kabuğu | Efsanevi | Tüm birlikler | 40 | HP +%3 | DEF +%3 | ATK +%3; Kadim Muhafızlar'a hasar +%10 |
<!-- /URET:tablo:techizat.setler -->

**Set kuralları.**

- Set bonusu yalnızca **aynı kahramanın** üzerindeki parçaları sayar.
- Eşikler 2, 4 ve 6'dır; 6 parçalı bir set üç bonusun hepsini verir.
- Setler karıştırılabilir: 4 + 2 iki setin toplam üç bonusunu (A'nın 2 ve 4'ü, B'nin 2'si), 2 + 2 + 2
  üç setin 2 parça bonusunu verir. Bu, tek bir Kaptan'ı iki role birden hazırlamanın yoludur
  (ör. Abis Levhası ×4 + Kubbe Nöbetçisi ×2).
- Birlik setlerinin bonusu yalnızca o birliğe işler; "Tüm birlikler" setleri herkese. Rol setleri
  (Garnizon, Toplama, Canavar avı, Sefer) yalnızca o bağlamda çalışır; arayüz bağlam dışındaki set
  bonusunu soluk gösterir.
- Muhafız Kabuğu ayrıca kadim parça ister (bölüm 7, çizim tablosu); Kadim Muhafızlar'la savaşın
  kalıcı ödülüdür.

## 7. Üretim

<!-- URET:tablo:techizat.uretim -->
| Parça | Atölye Sv | Ana malzeme | Yan malzeme | Titanyum | Süperalaşım | Çizim | Ham süre |
| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| Sıradan | 1 | 4 | 2 | 12.000 | 0 | — | 10 dk |
| Gelişmiş | 15 | 4 | 2 | 45.000 | 0 | — | 1 sa |
| Nadir | 20 | 5 | 3 | 180.000 | 0 | — | 4 sa |
| Destansı | 25 | 6 | 3 | 600.000 | 0 | Destansı Çizim ×1 | 12 sa |
| Efsanevi | 35 | 8 | 4 | 3.000.000 | 150 | Efsanevi Çizim ×1 | 1 g |
<!-- /URET:tablo:techizat.uretim -->

- **Ana ve yan malzeme** yuvaya göre değişir (bölüm 5'teki "Ana + yan malzeme" sütunu). Örnek:
  Destansı kask, Destansı nadirlikte <!--v:veri.techizat.uretim.3.ana_adet-->6<!--/v--> bazalt camı +
  <!--v:veri.techizat.uretim.3.yan_adet-->3<!--/v--> kitin ister.
- **Süre** hamdır; Atölye'nin her seviyesi +%<!--v:veri.techizat.kurallar.atolye_hiz_seviye_basi-->1<!--/v-->
  üretim hızı verir ve Deniz Karaca'nın mürettebat becerisi bunun üstüne eklenir (K-13 bölen
  formülü). Üretim hızlandırıcıyla kısaltılabilir ([12](12_syndicate.md) hızlandırıcılar).
- **Kuyruk:** bir üretim kuyruğu; Atölye <!--v:veri.techizat.atolye_kilitleri.5.seviye-->30<!--/v-->'da
  ikincisi açılır.
- **Çizim** yalnızca Destansı ve Efsanevi parçalarda gerekir:

<!-- URET:tablo:techizat.cizimler -->
| Eşya | Nereden | Not |
| --- | --- | --- |
| Destansı Çizim | Kadim Muhafız Devriyesi Sv 25+ (şans), Muhafız Mahzeni etkinliği, İttifak Dükkânı (haftada 1), Kalıntı Arşivi setleri | Sökmede tam iade |
| Efsanevi Çizim | Demir Leviathan Yuvası Sv 3+ (ralli ödülü şansı), Kadim Galeri keşfi, Kronik aşama 8 ve 10, Akıntı Savaşı dükkânı | Sökmede tam iade; Usta İşi de 1 tane ister |
| Kadim parça | Kadim Muhafız Devriyesi, Kadim Galeri (09) | Yalnızca Muhafız Kabuğu: parça başına 2 |
<!-- /URET:tablo:techizat.cizimler -->

### Tam set bedeli

<!-- URET:tablo:techizat.set_maliyeti -->
| Tam set | Parça | Malzeme (o nadirlikte) | Sıradan eşdeğeri | Titanyum | Süperalaşım | Çizim |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Destansı | 6 | 54 | 3.456 | 3.600.000 | 0 | 6 |
| Efsanevi | 6 | 72 | 18.432 | 18.000.000 | 900 | 6 |
<!-- /URET:tablo:techizat.set_maliyeti -->

Bir Efsanevi set, bir Destansı setin malzemece beş katından fazlasıdır. Süperalaşım payı küçük
tutuldu, çünkü Süperalaşım Reaktör 31–50'nin kapı kaynağıdır ([02](02_tempo_ve_denge.md)):
teçhizat reaktörle yarışmamalı, yanında yürümeli.

### Köpük Hattı

Sintaktik köpük doğada bulunmadığı için Atölye onu kendisi döker. Köpük Hattı kendiliğinden
çalışır, toplanmayı bekler ve belirli bir sayıdan sonra durur; oyuncuyu sık girişe zorlamaz.

<!-- URET:tablo:techizat.kopuk_hatti -->
| Atölye Sv | Üretim | Toplanmadan bekleyen en çok |
| --- | --- | ---: |
| 8–19 | 4 saatte 1 Sıradan köpük | 12 |
| 20–29 | 4 saatte 1 Gelişmiş köpük | 12 |
| 30–39 | 4 saatte 1 Nadir köpük | 12 |
| 40–49 | 4 saatte 1 Destansı köpük | 12 |
| 50 | 3 saatte 1 Destansı köpük | 16 |
<!-- /URET:tablo:techizat.kopuk_hatti -->

### Atölye seviyeleri

<!-- URET:tablo:techizat.atolye_kilitleri -->
| Atölye Sv | Açılan |
| ---: | --- |
| 1 | Sıradan parça · Sıradan → Gelişmiş birleştirme · sökme ve pişmanlık penceresi · toplu birleştirme |
| 8 | Köpük Hattı |
| 15 | Gelişmiş parça · Gelişmiş → Nadir birleştirme · ayrıştırma |
| 20 | Nadir parça ve Kurtarma Ekibi seti · Nadir → Destansı birleştirme · Köpük Hattı Gelişmiş üretir |
| 25 | Destansı parça ve sekiz Destansı set · Destansı → Efsanevi birleştirme |
| 30 | İkinci üretim kuyruğu · Köpük Hattı Nadir üretir |
| 35 | Efsanevi parça: Basınç Kalesi, Karanlık Akıntı, Kadim Rezonans |
| 40 | Muhafız Kabuğu seti · Köpük Hattı Destansı üretir |
| 45 | Usta İşi |
| 50 | Usta İşi Titanyum bedeli yarıya iner · Köpük Hattı 3 saatte bir |
<!-- /URET:tablo:techizat.atolye_kilitleri -->

Atölye'nin seviye maliyeti ve süresi bina sayfasındadır: [Teçhizat Atölyesi](binalar/gear_workshop.md).

## 8. Sökme, iade ve Usta İşi

<!-- URET:tablo:techizat.sokum -->
| İşlem | Malzeme | Titanyum / Süperalaşım | Çizim | Not |
| --- | --- | --- | --- | --- |
| Normal sökme | %50 (aşağı yuvarlanır, aynı nadirlik) | İade yok | %100 | Usta İşi parçasında Usta İşi malzemesinin de yarısı döner |
| Pişmanlık penceresi | %100 | %100 Titanyum ve Süperalaşım | %100 | Üretimden sonraki 24 saat içinde ve parça hiç savaşa girmemişse |
| Ayrıştırma (malzeme) | 1 → 3 alt nadirlik | Bedelsiz | — | Atölye 15; eksik alt nadirlik malzemesini tamamlamak için |
<!-- /URET:tablo:techizat.sokum -->

- **Pişmanlık penceresi:** Üretimden sonraki <!--v:veri.techizat.kurallar.pismanlik_saat-->24<!--/v-->
  saat içinde, parça hiç savaşa girmediyse sökme her şeyi tam iade eder. Yanlış yuvaya yanlış set
  basan oyuncu cezalandırılmaz.
- **Çizim** sökmede her zaman geri döner; oyuncu bir seti bozup başka bir sete geçerken en kıt
  eşyayı kaybetmez.
- Malzeme iadesi %<!--v:veri.techizat.kurallar.sokum_malzeme-->50<!--/v-->'dir ve aşağı yuvarlanır;
  Titanyum ve Süperalaşım dönmez (pişmanlık penceresi hariç).

**Usta İşi.** Atölye <!--v:veri.techizat.usta_isi.atolye-->45<!--/v-->'te açılır.
<!--v:veri.techizat.usta_isi.hedef-->Yalnızca Efsanevi parça; parça başına bir kez<!--/v-->. Parçanın ana
özelliği ×<!--v:veri.techizat.usta_isi.carpan-->1,2<!--/v--> olur. Bedel:
<!--v:veri.techizat.usta_isi.ana_malzeme-->2<!--/v--> Efsanevi ana malzeme,
<!--v:veri.techizat.usta_isi.cizim-->Efsanevi Çizim ×1<!--/v--> ve
<!--v:veri.techizat.usta_isi.titanyum-->1.000.000<!--/v--> Titanyum (Atölye 50'de yarısı). Usta İşi son
oyunun Titanyum ve malzeme yutağıdır; tam Efsanevi takımı olan oyuncuya hâlâ bir hedef bırakır.

## 9. Teçhizat gücü

Kuşanılmış her parça kahraman gücüne nadirliğinin parça gücü kadar eklenir (bölüm 4 tablosu);
Usta İşi parça ×<!--v:veri.techizat.usta_isi.carpan-->1,2<!--/v--> sayılır. Güç yalnızca kuşanılmış
parçalardan gelir: çantada bekleyen takım güç vermez (K-14). Tam Efsanevi bir takım, Sv 50, 5★
bir Efsanevi kahramanın gücüne yakın bir güç ekler; bu bilinçli bir eşitliktir: teçhizat bir
kahraman kadar emek ister.

## 10. İlerleme: ücretsiz oyuncu ve ödeme yapan oyuncu

Malzeme akışı hedefi (Sıradan eşdeğeri / gün; 1 Efsanevi = 256 Sıradan):

<!-- URET:tablo:techizat.malzeme_butcesi -->
| Sunucu günü | F2P | Aylık kart + VIP 8 | Balina |
| ---: | ---: | ---: | ---: |
| 30 | 50 | 80 | 200 |
| 90 | 120 | 190 | 450 |
| 180 | 220 | 350 | 800 |
| 365 | 300 | 480 | 1.100 |
<!-- /URET:tablo:techizat.malzeme_butcesi -->

Bu bütçe 09 (yataklar, canavarlar), 13 (etkinlikler) ve 15 (ödüller) tarafından paylaşılır; üçünün
toplamı bu tabloya oturmalıdır. Hedef tempo:

<!-- URET:tablo:techizat.tempo -->
| Profil | İlk tam Destansı set | İlk tam Efsanevi set | Yıl sonunda |
| --- | --- | --- | --- |
| Ücretsiz oyuncu | ~75. gün | ~190. gün | 2 Kaptan tam Efsanevi, 2 Kaptan Destansı |
| Aylık kart + VIP 8 | ~50. gün | ~130. gün | 3–4 Kaptan tam Efsanevi |
| Balina | ~25. gün | ~50. gün | 4 Kaptan Efsanevi + Usta İşi |
<!-- /URET:tablo:techizat.tempo -->

- **Ücretsiz oyuncu** ilk yılında iki Kaptan'ı tam Efsanevi takımla, diğer ikisini Destansı
  setlerle donatır. Wira Santoso'nun malzeme şansı ve Toplama ağacının "Eşsiz Tarama" anahtarı
  F2P'nin kendi malzemesini üretme araçlarıdır.
- **Aylık kart** sahibi daha çok Sonar Enerjisi, daha çok etkinlik kademesi ve daha çok
  hızlandırıcı demektir; aynı yolu daha kısa yürür.
- **Balina** Atölye Sandıkları (içeriği yazılı) ve hızlandırıcıyla erken tamamlar, ama Efsanevi
  parça Atölye <!--v:veri.techizat.nadirlikler.efsanevi.atolye-->35<!--/v--> ister ve Atölye reaktörü
  geçemez; parayla atlanamayan bir zaman kapısı vardır.
- **Rastgele teçhizat çekilişi yoktur.** Eski tasarımdaki sonar çekilişli teçhizat fikri
  (`docs/15_Monetization.md`) bu kılavuzda kullanılmaz: teçhizatın tamamı üretilir.

## 11. Arayüz notları (Sessiz lüks)

- **Kahramanın Teçhizat sekmesi:** altı yuva 3D modelin (HeroStage) çevresinde yarım daire
  dizilir; set ilerlemesi altında ince noktalar (●●●●○○). Subay'ın sekmesinde "Kaptan olunca
  çalışır" satırı.
- **Atölye ekranı:** tarif listesi yuva, set ve birliğe göre süzülür. Eksik malzeme kırmızı değil
  soluk yazılır ve dokununca **nereden geldiğini** gösterir (yatak türü, düşman, etkinlik);
  The Syndicate en sonda, küçük bir satırdır.
- **Birleştirme** tek dokunuş; animasyon kısa bir kristalleşme sesi ve ışıktır, konfeti yoktur.
- **Karşılaştırma:** yeni parça seçilince eski ve yeni değerler yan yana, set eşiğinin değişip
  değişmediği tek satırda.
- **Takım kaydı:** her kahramanın iki dizilim kaydı ([05](05_kahramanlar.md) bölüm 7.3) teçhizat
  takımını da saklar; dizilim değişince takım da değişir.

## 12. Uç durumlar

- **Aynı parça iki kahramanda:** olamaz; başka kahramana takmak eskisinden çıkarır.
- **Kaptan değişince:** teçhizat kahramanda kalır; yeni Kaptan kendi takımını getirir. Filo
  üsteyken "takımı yeni Kaptan'a taşı" tek dokunuştur.
- **Filo dışarıdayken sökme:** o filonun kahramanlarının üzerindeki parçalar sökülemez.
- **Set bonusunun bağlamı:** Garnizon seti yalnızca üs ve ittifak yapısı savunmasında, Toplama
  seti yalnızca toplama yürüyüşünde çalışır; bir toplama filosu saldırıya uğrarsa Toplama setinin
  savaş katkısı yoktur.
- **Pişmanlık penceresi ve savaş:** parça bir kez bile savaşa girmişse (garnizondaki savunma
  dahil) pencere kapanır.
- **Atölye kuyruğu ve reaktör:** Atölye yükseltilirken üretim sürer; yükseltme kuyruğu durdurmaz.
- **Köpük Hattı dolarsa** üretim durur; hiçbir köpük kaybolmaz.
- **Muhafız Kabuğu ve kadim parça:** kadim parçası olmayan oyuncu seti göremez değil, görür ama
  "Kadim Muhafızlar'dan gelir" notuyla.

## 13. Bugün kodda

<!-- URET:tablo:techizat.durum -->
| Konu | Durum | Not |
| --- | --- | --- |
| Teçhizat Atölyesi binası | ⬜ | binalar.json'da tasarım; seviye kilitleri bu bölümde |
| Dört malzeme ve beş nadirlik | ⬜ | Eşya türü yok; catalog items yalnızca sandık ve kart |
| Altı yuva, kuşanma, yalnızca Kaptan'da etki | ⬜ | Kaptan/Subay sistemine bağlı (05) |
| Üretim, birleştirme, ayrıştırma, sökme | ⬜ | — |
| Setler 2/4/6 | ⬜ | — |
| Köpük Hattı | ⬜ | — |
| Usta İşi | ⬜ | — |
| Haritadan malzeme düşmesi | ⬜ | 09 tabloları masif sülfür, kitin, bazalt camı ve sintaktik köpüğü anıyor |
| Teçhizat gücü | ⬜ | K-14 kahraman gücüne eklenir |
<!-- /URET:tablo:techizat.durum -->

## 14. Uygulama notları

**`shared/catalog/catalog.json` (v9)**
- `gear.materials[]` (4 × 5 nadirlik), `gear.rarities[]` (Atölye seviyesi, güç),
  `gear.slots[]` (ana/yan özellik, malzeme), `gear.slotValues` (nadirlik × yuva),
  `gear.sets[]` (2/4/6 etkileri, bağlam, birlik), `gear.craft[]`, `gear.combine[]`,
  `gear.dismantle`, `gear.foamLine[]`, `gear.mastercraft`.
- `items`: dört malzemenin beş nadirliği (`material_chitin_epic` vb.), `blueprint_epic`,
  `blueprint_legendary`, `ancient_fragment`.
- `buildings[gear_workshop]`: seviye kilitleri (bu bölümdeki tablo; ana yazar binalar.json'a
  taşıyabilir).

**`shared/HADAL.Shared/Simulation/`**: yeni `GearRules.cs` (üretim doğrulama, birleştirme,
ayrıştırma, sökme/pişmanlık, set sayımı, bağlam süzgeci, Kaptan kuralı, güç). Filo istatistiği
`FleetRules` (05) içinde teçhizat yüzdelerini toplar; savaş çözücüsü ([07](07_birlikler_ve_savas.md))
yalnızca toplamı görür.

**Komutlar:** `CraftGearCommand`, `CombineMaterialCommand` (toplu), `SplitMaterialCommand`,
`DismantleGearCommand`, `EquipGearCommand` (kahraman, yuva, parça ya da boş),
`CollectFoamCommand`, `MastercraftGearCommand`.

**Sunucu** (`server/Hadal.Game/`): yeni `PlayerSession.Gear.cs`; `Persistence/PlayerExtras.cs`
→ `gear[]` (benzersiz parça kimliği, set, yuva, nadirlik, `craftedAt`, `battleUsed`, `mastercraft`,
`equippedBy`), `materials{}`, Köpük Hattı zaman damgası. Harita ödülleri (`09`) malzeme verir.

**İstemci** (`Assets/_Hadal/Scripts/UI/`): yeni `GearView.cs` (kahraman sekmesi), `WorkshopView.cs`
(tarifler, birleştirme, Köpük Hattı), çantada "Malzemeler" sekmesi. `tr.txt`: 13 set × 6 parça
adı, malzeme adları ve bilgi kartları.

## 15. Esin kaynakları

| Mekanik | Türdeki örnek (genel) | Hadal'daki biçimi |
| --- | --- | --- |
| Dört malzemeden üretilen teçhizat | Rise of Kingdoms'taki dört malzemeli komutan ekipmanına benzer | Derin denizin kendi malzemeleri; yuvaya göre ana + yan malzeme |
| 4 → 1 malzeme birleştirme | Türde yaygın malzeme yükseltme | Anlık, toplu; kayıplı ayrıştırma |
| 2/4/6 parça set bonusları | Rise of Kingdoms'taki set bonuslarına benzer | Birlik setleri, rol setleri (Garnizon, Toplama, Av, Sefer), Kadim set |
| Ekipmanın yalnızca birincil komutanda çalışması | Rise of Kingdoms'taki birincil komutan kuralına benzer | Yalnızca Kaptan'da çalışır, Subay ve ekip için soluk |
| Beş nadirlik | Türde yaygın beş kademeli ekipman | Sıradan, Gelişmiş, Nadir, Destansı, Efsanevi |

Kopyalanan ad, metin ya da sayı yoktur.

## 16. Açık sorular

1. **09 · Dünya Haritası:** Balina Leşi (kitin), Kobalt Kabuğu (bazalt camı), Enkaz Alanı
   (sintaktik köpük) ve Demir Leviathan Yuvası (masif sülfür) için önerilen malzeme şansları
   yatak tablosuna eklensin mi? Kaynak seviyesi → malzeme nadirliği tablosu 09'un yatak ve düşman
   seviyeleriyle hizalı mı?
2. **13 / 15:** Günlük malzeme bütçesi (Sıradan eşdeğeri) etkinlik ve ödül tablolarına dağıtılmalı;
   üç bölümün toplamı bölüm 10'daki tabloya oturmalı.
3. **Ana yazar (binalar.json):** Teçhizat Atölyesi'nin seviye kilitleri bu bölümde; bina sayfasında
   görünmesi için `kilitler` alanına taşınması.
4. **12 · The Syndicate:** Atölye Sandıkları'nın içeriği ve aylık sınırı; hızlandırıcıların üretime
   uygulanıp uygulanmayacağı.
5. **07 · Birlikler ve Savaş:** Delici seti (Kuyu Matkabı) ve "yapılara verilen hasar" kavramı
   kuşatma kurallarıyla uyumlu mu? Set bonusları ile anıt bonusları aynı formül katmanında mı?
6. **02 · Tempo:** Teçhizat gücünün (parça başına Efsanevi 7.500) toplam güç dengesine etkisi.
