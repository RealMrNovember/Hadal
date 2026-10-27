# 07 · Birlikler ve Savaş

> **Özet.** Hadal'da savaşın sayısını askerler, yönünü kahramanlar verir. Dört asker türü vardır:
> üçü bir karşı üçgeni kurar (**Zırhlı Dalgıç > Ark Askeri > Zıpkıncı > Zırhlı Dalgıç**), dördüncüsü
> **Delici** üçgenin dışında kalan kuşatma ve taşıma birliğidir. Her tür on kademeye (T1–T10) ayrılır;
> kademeleri Tatbikat Havuzu ve araştırma açar, T9 ve T10 Süperalaşım ister. Savaş sunucuda,
> rastgelelik olmadan, en çok <!--v:veri.birlikler.savas.tur_sayisi-->10<!--/v--> turda çözülür.
> Kaybedilen asker ya **yaralı** ya **ölü** olur; PvE'de kimse ölmez, sezon sonunda ölülerin
> <!--v:veri.birlikler.kurtarma_kapsulu.oran|yuzde-->%50<!--/v-->'si Kurtarma Kapsülü olarak geri gelir (K-21).
> Bu bölüm türleri, kademeleri, askeri binaların kapasitelerini, iyileştirmeyi, kayıp kurallarını,
> savaş formüllerini, üs savunmasını ve keşfi tanımlar.

Durum: ⬜. Kodda asker, savaş ve sefer yoktur; yalnızca Unity tarafında ağa bağlı olmayan eski
taslaklar durur (bkz. [9. Bugün kodda](#9-bugün-kodda)).

---

## 1. Amaç

Üs ekonomisi bir yıl sürecek bir yolculuk kurar ([02 · Tempo ve Denge](02_tempo_ve_denge.md)); askerler
o ekonominin **harcandığı yeri** ve haritanın **anlamını** verir. Bu bölümün hedefleri:

1. **Okunur karar.** Oyuncu bir bakışta "ne eğitmeliyim?" sorusunu cevaplayabilmeli. Dört tür, tek bir
   üçgen, tek bir kademe çizelgesi; sürpriz çarpan yok.
2. **Kayıp acıtır ama yıkmaz.** Yaralılar iyileşir, PvE'de ölüm yoktur, savunmada Kriyo Bakım Ünitesi
   ve sezon sonunda Kurtarma Kapsülü vardır. Ücretsiz oyuncu büyük savaşlardan dışlanmaz (K-21).
3. **Deterministik ve denetlenebilir savaş.** Aynı girdi her zaman aynı sonucu verir; istemci savaş
   raporunu yeniden oynatabilir, sunucu hakemdir.
4. **Gerçek mühendislikten güç alan fantezi.** Zırhlı Dalgıç tek atmosferli basınç kıyafetinden,
   Zıpkıncı zıpkın tüfeklerinden, Ark Askeri sualtı kaynak ve şok silahlarından, Delici batık kurtarma
   ve sondaj ekipmanından doğar. Düşmanlar ise hikâyenin kendi yaratıklarıdır; gerçek canlılar asla
   hedef olmaz (K-25).

---

## 2. Kurallar

### 2.1 Dört tür

<!-- URET:tablo:birlikler.turler -->
| Tür | Kimlik | Rol | Saf | Üstün | Zayıf | ATK × | DEF × | HP × | Hız | Yük × |
| --- | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| Zırhlı Dalgıç | `armored` | Ön saf, dayanıklı | Ön | Ark Askeri | Zıpkıncı | 0,85 | 1,25 | 1,2 | 80 | 1 |
| Zıpkıncı | `harpoon` | Arka saf, delici atış | Arka | Zırhlı Dalgıç | Ark Askeri | 1,25 | 0,85 | 0,85 | 120 | 0,8 |
| Ark Askeri | `arc` | Arka saf, yayılan hasar | Arka | Zıpkıncı | Zırhlı Dalgıç | 1,1 | 0,95 | 0,95 | 100 | 0,7 |
| Delici | `driller` | Kuşatma ve taşıma | Ön | Yapılar | Sahada herkes | 0,9 | 0,9 | 1,1 | 70 | 2 |
<!-- /URET:tablo:birlikler.turler -->

Tür çarpanları tabana uygulanır: her asker taban **ATK <!--v:veri.birlikler.savas.taban.atk-->30<!--/v--> ·
DEF <!--v:veri.birlikler.savas.taban.def-->30<!--/v--> · HP <!--v:veri.birlikler.savas.taban.hp-->100<!--/v-->**
değerinden başlar, tür çarpanıyla şekillenir ve kademe gücüyle büyür (formül: [3.1](#31-tür-istatistikleri)).
**Hız** bir yürüyüşün seyir hızıdır (100 = taban; denizaltı ve araştırma bunu çarpar, bkz.
[08](08_denizaltilar_basinc_seferler.md)). **Yük** asker başına taşınabilen kaynak miktarıdır.

<!-- URET:tablo:birlikler.turler_esin -->
| Tür | Dayandığı mühendislik | Savaştaki özelliği |
| --- | --- | --- |
| Zırhlı Dalgıç | İçini 1 atmosferde tutan sert basınç kıyafetleri: dalgıç, derinliğin ezici basıncını eklemli bir kabuğun içinde hissetmez. | Faraday kabuğu: iletken dış kabuk ark akımını gövdeden geçirip suya verir; Ark Askerine karşı üstün. |
| Zıpkıncı | Lastikli ve pnömatik zıpkın tüfekleri, gaz kartuşlu fırlatıcılar, patlayıcı uçlar; geç kademede bobin ve ray hızlandırıcılar. | Delici atış: hasarının %85'ini ön safa verir; ağır kabukları deler, Zırhlı Dalgıça karşı üstün. En hızlı tür. |
| Ark Askeri | Yalıtımlı temas mızrakları, şok çubukları, ıslak kaynak ve kesme arkları; tuzlu suda akım dağılır, bu yüzden silah hedefe dokunarak ya da çok kısa mesafede ateşlenir. | Zincir ark: hasarı ön ve arka safa yarı yarıya yayılır; hafif korunan zıpkıncıları sersemletir, Zıpkıncıya karşı üstün. |
| Delici | Hidrolik kesiciler, elmas tel testereler, aşındırıcılı su jeti, termik mızrak ve tünel açma burguları: batık kurtarma ve deniz tabanı sondajının araçları. | Askerlere karşı hasarı ×0,6; yapılara (Kalkan, kale, yuva) karşı ×3. Taşıma kapasitesi diğer türlerin iki katından fazla. Üçgenin dışındadır. |
<!-- /URET:tablo:birlikler.turler_esin -->

Tasarım notu: dört türün eğitim maliyeti **aynıdır**. Tür seçimi bir maliyet kararı değil, bir taktik
karardır; oyuncu "en ucuz" türü değil, rakibine karşı doğru türü eğitir.

### 2.2 Karşı üçgeni ve anıtlar

Hasar çarpanı, saldıran türün savunan türe karşı durumuna göre seçilir:

- **Avantaj:** ×<!--v:veri.birlikler.karsi.avantaj-->1,2<!--/v--> · **Dezavantaj:** ×<!--v:veri.birlikler.karsi.dezavantaj-->0,85<!--/v--> · **Nötr:** ×1
- **Delici** askerlere karşı ×<!--v:veri.birlikler.karsi.saha_cezasi-->0,6<!--/v--> ile vurur; yapılara karşı
  gücü ×<!--v:veri.birlikler.karsi.yapi_carpani-->3<!--/v--> sayılır (diğer türler ×<!--v:veri.birlikler.karsi.diger_yapi_carpani-->1<!--/v-->;
  bkz. [4.5](#45-kuşatma-ve-yapılar)). Delici üçgenin dışındadır: ona vurulan hasar her türden ×1'dir.

<!-- URET:tablo:birlikler.karsi_tablosu -->
| Saldıran ↓ / Savunan → | Zırhlı Dalgıç | Zıpkıncı | Ark Askeri | Delici | Yapı |
| --- | ---: | ---: | ---: | ---: | ---: |
| Zırhlı Dalgıç | 1 | 0,85 | 1,2 | 1 | 1 |
| Zıpkıncı | 1,2 | 1 | 0,85 | 1 | 1 |
| Ark Askeri | 0,85 | 1,2 | 1 | 1 | 1 |
| Delici | 0,6 | 0,6 | 0,6 | 0,6 | 3 |
<!-- /URET:tablo:birlikler.karsi_tablosu -->

Üç anıt binası avantaj çarpanını büyütür: her 10 anıt seviyesi için
+<!--v:veri.birlikler.karsi.anit_on_seviye-->0,02<!--/v-->, en çok +<!--v:veri.birlikler.karsi.anit_en_fazla-->0,1<!--/v-->.
Anıtlar dezavantajı değiştirmez; yalnızca kendi türünün üstün olduğu eşleşmeyi keskinleştirir.

<!-- URET:tablo:birlikler.anitlar -->
| Anıt | Kimlik | Etki | Sv 10 | Sv 25 | Sv 50 |
| --- | --- | --- | ---: | ---: | ---: |
| Dalgıç Anıtı | `armor_monument` | Zırhlı → Ark avantajı | 1,22 | 1,25 | 1,3 |
| Zıpkın Anıtı | `harpoon_monument` | Zıpkıncı → Zırhlı avantajı | 1,22 | 1,25 | 1,3 |
| Ark Anıtı | `arc_monument` | Ark → Zıpkıncı avantajı | 1,22 | 1,25 | 1,3 |
<!-- /URET:tablo:birlikler.anitlar -->

Neden bu üçgen? Zırhlı Dalgıç'ın iletken dış kabuğu ark akımını gövdeden geçirip suya verir (bir
Faraday kafesi gibi); bu yüzden Ark Askeri onu delemez. Zıpkının sivri ucu ise ağır kabuğa odaklanır ve
eklem yerlerinden girer. Ark Askerinin akımı da hafif korunan, ince giysili zıpkıncıları sersemletir.
Oyuncu kuralı bir cümlede hatırlar: **"kabuk akımı yutar, zıpkın kabuğu deler, akım zıpkıncıyı vurur."**

### 2.3 Kademeler

Her türün on kademesi vardır. Kademe, **Tatbikat Havuzu** seviyesiyle açılır; T4 ve üstü ayrıca
**Askeri dalda** bir doktrin araştırması ister ([06 · Araştırma](06_arastirma.md)). T9 ve T10 eğitimi
Süperalaşım ister. Maliyetler **asker başınadır** ve dört tür için aynıdır.

<!-- URET:tablo:birlikler.kademeler -->
| Kademe | Tatbikat Sv | En erken R | Araştırma | Güç/asker | Gıda | Biyokütle | Titanyum | Süperalaşım | Süre (sn) | Bakım Gıda/sa | Bakım O₂/sa |
| --- | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| T1 | 1 | 5 | — | 3 | 6 | 2 | 4 | — | 2 | 0,3 | 0,2 |
| T2 | 5 | 5 | — | 4 | 9 | 3 | 6 | — | 3 | 0,35 | 0,2 |
| T3 | 10 | 10 | — | 6 | 14 | 5 | 9 | — | 4 | 0,4 | 0,2 |
| T4 | 15 | 15 | Kademe IV Doktrini | 9 | 30 | 9 | 18 | — | 6 | 0,5 | 0,2 |
| T5 | 20 | 20 | Kademe V Doktrini | 13 | 60 | 18 | 35 | — | 8 | 0,6 | 0,2 |
| T6 | 25 | 25 | Kademe VI Doktrini | 18 | 110 | 30 | 65 | — | 11 | 0,7 | 0,2 |
| T7 | 30 | 30 | Kademe VII Doktrini | 25 | 190 | 50 | 110 | — | 15 | 0,8 | 0,2 |
| T8 | 35 | 35 | Kademe VIII Doktrini | 34 | 300 | 80 | 170 | — | 20 | 0,9 | 0,2 |
| T9 | 40 | 40 | Kademe IX Doktrini | 45 | 450 | 120 | 250 | 0,02 | 26 | 1 | 0,2 |
| T10 | 45 | 45 | Kademe X Doktrini | 60 | 650 | 170 | 360 | 0,04 | 34 | 1,1 | 0,2 |
<!-- /URET:tablo:birlikler.kademeler -->

- **Derinlik Kroniği** kademelere ayrıca sunucu çapında bir kapı koyar (T4–T5 aşama 2 … T10 aşama 10);
  kapılar F2P temposundan önce açılır, yalnızca hızlı ilerleyen oyuncuyu bekletir
  ([16 · Kronik, Köken, Profil](16_kronik_kokenler_profil.md#23-kronik-ve-tempo)).
- **Köken birlikleri:** her köken bir türün T8–T10'unu kendi birliğiyle değiştirir; bedel, süre ve güç
  aynıdır, istatistik farkı ±%2–5'tir ([16](16_kronik_kokenler_profil.md#34-özel-birlikler)). Bu bölümün
  tabloları standart birlikleri gösterir.
- **Güç/asker** kanondaki değerdir: T1 <!--v:veri.birlikler.kademeler.t1.guc-->3<!--/v--> → T10 <!--v:veri.birlikler.kademeler.t10.guc-->60<!--/v-->.
- **Süre** bir askerin ham eğitim süresidir. Parti süresi = parti boyutu × süre; hız bonusları K-13'e göre
  bölen olarak uygulanır: `süre = ham / (1 + Σhız% / 100)`.
- **En erken R**, Tatbikat Havuzu'nun o seviyeye çıkabileceği ilk reaktördür (bina reaktörü geçemez;
  Tatbikat Havuzu Reaktör 5'te açılır). F2P için: T5 <!--v:hesap.sim.f2p.seviye.20|gun-->4,5. gün<!--/v-->,
  T7 <!--v:hesap.sim.f2p.seviye.30|gun-->28,6. gün<!--/v-->, T9 <!--v:hesap.sim.f2p.seviye.40|gun-->128,4. gün<!--/v-->,
  T10 <!--v:hesap.sim.f2p.seviye.45|gun-->224,9. gün<!--/v--> (araştırma süresi hariç).

### 2.4 Eğitim, terfi, terhis

| Kural | Değer |
|---|---|
| Eğitim yeri | Askeri Kubbe (kuyruk sayısı) + Tatbikat Havuzu (parti boyutu, kademe) |
| Bir kuyruk | Bir türden, bir kademeden, en çok bir parti |
| Kuyruk sayısı | Askeri Kubbe <!--v:veri.birlikler.egitim.kuyruk_esikleri.0-->1<!--/v-->: 1 · Sv <!--v:veri.birlikler.egitim.kuyruk_esikleri.1-->10<!--/v-->: 2 · Sv <!--v:veri.birlikler.egitim.kuyruk_esikleri.2-->25<!--/v-->: 3 · Sv <!--v:veri.birlikler.egitim.kuyruk_esikleri.3-->40<!--/v-->: 4 |
| Bedel | Parti başlarken peşin alınır; iptalde %100 iade |
| Kapasite | Sağlıklı + seferdeki + eğitimdeki + Revir'deki askerler ordu kapasitesini aşamaz |
| Eğitim hızı | Tatbikat Havuzu seviye başına +%<!--v:veri.birlikler.egitim.tatbikat_hiz_seviye_basi-->0,5<!--/v-->; araştırma, kahraman, VIP, ittifak bonuslarıyla toplanır |
| Terfi | Bir kademeden üst kademeye yükseltme. Bedel ve süre iki kademenin farkıdır; hedef kademe açık olmalı; kapasite değişmez |
| Terhis | Asker serbest bırakılır, iade %<!--v:veri.birlikler.egitim.terhis_iadesi-->0<!--/v-->; güç düşer. Yanlışlıkla dokunmaya karşı iki adımlı onay |
| Ücretsiz bitirme | İnşaattaki gibi son <!--v:veri.ilerleme.kurallar.ucretsiz_bitirme_sn|sure-->5 dk<!--/v--> (+ Mühendislik Bölmesi bonusu) |
| İttifak yardımı | Eğitim kuyruğuna yardım edilmez; iyileştirmeye edilir ([10](10_ittifak.md)) |

**Terfi** geç oyunun ana işidir: ordu kapasitesi sınırlı olduğu için güç artışı yeni askerden değil,
var olan askerin kademesinden gelir. T8 → T9 terfisi T9 eğitiminden ucuzdur çünkü T8'in bedeli zaten
ödenmiştir.

### 2.5 Bakım

Askerler saatte Gıda ve Oksijen tüketir (tablodaki *Bakım* sütunları). Bakım **yumuşaktır**: asker
kaçmaz, ölmez.

- **Açlık:** Gıda stoğu 0'a iner ve gelir bakımı karşılamazsa ordu **Aç** durumuna geçer: ATK ve DEF
  −%<!--v:veri.birlikler.bakim.aclik_cezasi-->10<!--/v-->, eğitim ve iyileştirme durur. Gıda gelince
  ceza hemen kalkar.
- **Nefessizlik:** Oksijen 0'da üs geneli kriz kuralları işler ([15 · Ekonomi Akışı](15_ekonomi_akisi.md));
  askerler ayrıca −%<!--v:veri.birlikler.bakim.nefessizlik_cezasi-->10<!--/v--> alır ve yeni sefere çıkamaz.
- **Seferde** asker üstekinin <!--v:veri.birlikler.bakim.seferde_o2_carpani-->2<!--/v--> katı Oksijen solur;
  bu, denizaltının dalış maliyetine eklenir ([08](08_denizaltilar_basinc_seferler.md)).
- Yaralılar ve Kriyo'dakiler bakım istemez.

Tam kapasite ordunun bakımı, referans üssün Gıda üretiminin yaklaşık altıda biridir (Reaktör 50'de
%<!--v:veri.birlikler.denge.8.bakim_payi-->16<!--/v-->; bkz. [5. İlerleme](#5-ilerleme)).

### 2.6 Yaralı, ölü, Kriyo ve Kurtarma Kapsülü

Savaşta kaybedilen her asker önce bir **kayıp** sayılır; kayıp, durumun kuralına göre yaralıya ve
ölüye bölünür:

<!-- URET:tablo:birlikler.kayiplar -->
| Durum | Yaralı % | Ölü % | Revir taşarsa | Not |
| --- | ---: | ---: | --- | --- |
| Derinlik Haritası, harita canavarları, Sürü Baskını, Leviathan avı (PvE) | 100 | 0 | Ölmez; Askeri Kubbe'de dinlenir, 8 saatte ücretsiz iyileşir | İyileştirme bedeli yarıya iner |
| Açık suda karşılaşma (iki yürüyüş) | 100 | 0 | Revir doluysa taşan yaralı ölür | İki taraf için aynı kural |
| Üsse saldırı (saldıran) | 70 | 30 | Revir doluysa taşan yaralı ölür | Kriyo saldırana çalışmaz |
| Üs savunması (savunan) | 100 | 0 | Taşanların %50'si Kriyo'ya (kapasiteye kadar), kalanı ölür | Takviye gönderenin askeri kendi Revir'ine gider |
| İttifak yapısına saldırı / yapı savunması | 70 | 30 | Revir doluysa taşan yaralı ölür | Ayrıntı: 09 ve 10 |
<!-- /URET:tablo:birlikler.kayiplar -->

- **Revir** yaralıları tutar ve iyileştirir. İki Revir vardır (Reaktör <!--v:veri.binalar.binalar.med_bay.adet.0-->6<!--/v-->
  ve <!--v:veri.binalar.binalar.med_bay.adet.1-->15<!--/v-->); kapasiteleri toplanır.
- **Kriyo Bakım Ünitesi** yalnızca **üs savunmasında** çalışır: Revir taşarsa taşan askerlerin yarısını
  dondurarak kurtarır (kapasitesine kadar), diğer yarısı ölür. Kriyo'daki asker güce sayılmaz, bakım
  istemez, Revir'de yer açıldıkça normal iyileştirme bedelinin
  %<!--v:veri.birlikler.iyilestirme.kriyo_yuzde-->50<!--/v-->'siyle uyandırılır.
- **PvE'de ölüm yoktur.** Kayıplar yaralıdır ve iyileştirme bedeli %<!--v:veri.birlikler.iyilestirme.pve_yuzde-->50<!--/v-->
  indirimlidir. Revir doluysa taşan askerler ölmez; Askeri Kubbe'de dinlenir ve
  <!--v:veri.birlikler.iyilestirme.dinlenme_saat-->8<!--/v--> saatte ücretsiz iyileşir.
- **Kurtarma Kapsülü (K-21).** <!--v:veri.birlikler.kurtarma_kapsulu.aciklama-->Sezon boyunca ölen askerlerin %50'si, kademe kademe aşağı yuvarlanarak sezon sonunda Kurtarma Kapsülü olarak verilir.<!--/v--> Kapsül
  açılınca askerler Askeri Kubbe'ye döner; kapasite doluysa kapsül çantada süresiz bekler. Kapsül
  devredilemez ve satılamaz. Sezon takvimi: [13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md).

### 2.7 İyileştirme

- **Bedel** = eğitim bedelinin %<!--v:veri.birlikler.iyilestirme.bedel_yuzde-->30<!--/v-->'u (Gıda,
  Biyokütle, Titanyum). Süperalaşım istenmez: T9–T10 kaybı ağır ama onarılabilir kalır.
- **Süre** = eğitim süresinin %<!--v:veri.birlikler.iyilestirme.sure_yuzde-->25<!--/v-->'i; Revir seviye
  başına +%<!--v:veri.birlikler.iyilestirme.revir_hiz_seviye_basi-->1<!--/v--> iyileştirme hızı verir (iki
  Revir'in yüksek olanı geçerlidir).
- İyileştirme tek kuyruktur ve parti boyutu sınırı yoktur; ittifak yardımı alır.
- Yaralı asker güce sayılmaz; iyileşince güç geri gelir. Böylece kayıp görünür olur ama kalıcı olmaz.

---

## 3. Sayılar

### 3.1 Tür istatistikleri

```
ATK = 30 × tür ATK çarpanı × (kademe gücü / 3)
DEF = 30 × tür DEF çarpanı × (kademe gücü / 3)
HP  = 100 × tür HP çarpanı × (kademe gücü / 3)
```

Kademe gücü T1 <!--v:veri.birlikler.kademeler.t1.guc-->3<!--/v-->'ten T10 <!--v:veri.birlikler.kademeler.t10.guc-->60<!--/v-->'a
çıktığı için T10 bir asker, T1'in ATK ve HP'sinin yirmi katını taşır. Hız türe bağlıdır ve kademeyle
değişmez; yük kademeyle büyür.

**Zırhlı Dalgıç** (`armored`)

<!-- URET:tablo:birlikler.istatistik.armored -->
| Kademe | Ad | ATK | DEF | HP | Hız | Yük | Güç |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| T1 | Bakır Miğfer | 26 | 38 | 120 | 80 | 10 | 3 |
| T2 | Perçinli Korse | 34 | 50 | 160 | 80 | 12 | 4 |
| T3 | Mafsallı Kabuk | 51 | 75 | 240 | 80 | 14 | 6 |
| T4 | Tek Atmosfer Zırhı | 77 | 113 | 360 | 80 | 17 | 9 |
| T5 | Titanyum Kabuk | 110 | 163 | 520 | 80 | 20 | 13 |
| T6 | Seramik Gövde | 153 | 225 | 720 | 80 | 24 | 18 |
| T7 | Sintaktik Zırh | 213 | 313 | 1.000 | 80 | 28 | 25 |
| T8 | Basınç Siperi | 289 | 425 | 1.360 | 80 | 33 | 34 |
| T9 | Derinlik Kalesi | 383 | 563 | 1.800 | 80 | 39 | 45 |
| T10 | Hadal Kolosu | 510 | 750 | 2.400 | 80 | 46 | 60 |
<!-- /URET:tablo:birlikler.istatistik.armored -->

**Zıpkıncı** (`harpoon`)

<!-- URET:tablo:birlikler.istatistik.harpoon -->
| Kademe | Ad | ATK | DEF | HP | Hız | Yük | Güç |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| T1 | Sapan Zıpkıncı | 38 | 26 | 85 | 120 | 8 | 3 |
| T2 | Lastikli Tüfekçi | 50 | 34 | 113 | 120 | 10 | 4 |
| T3 | Pnömatik Zıpkıncı | 75 | 51 | 170 | 120 | 11 | 6 |
| T4 | Kartuşlu Avcı | 113 | 77 | 255 | 120 | 14 | 9 |
| T5 | Patlayıcı Uçlu Nişancı | 163 | 110 | 368 | 120 | 16 | 13 |
| T6 | Çift Namlu Avcısı | 225 | 153 | 510 | 120 | 19 | 18 |
| T7 | Bobin Zıpkıncısı | 313 | 213 | 708 | 120 | 22 | 25 |
| T8 | Ray Zıpkıncısı | 425 | 289 | 963 | 120 | 26 | 34 |
| T9 | Güdümlü Nişancı | 563 | 383 | 1.275 | 120 | 31 | 45 |
| T10 | Uçurum Nişancısı | 750 | 510 | 1.700 | 120 | 37 | 60 |
<!-- /URET:tablo:birlikler.istatistik.harpoon -->

**Ark Askeri** (`arc`)

<!-- URET:tablo:birlikler.istatistik.arc -->
| Kademe | Ad | ATK | DEF | HP | Hız | Yük | Güç |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| T1 | Şok Çubukçusu | 33 | 29 | 95 | 100 | 7 | 3 |
| T2 | Batarya Mızrakçısı | 44 | 38 | 127 | 100 | 8 | 4 |
| T3 | Kıvılcım Eri | 66 | 57 | 190 | 100 | 10 | 6 |
| T4 | Kondansatör Eri | 99 | 86 | 285 | 100 | 12 | 9 |
| T5 | Plazma Kesici | 143 | 123 | 412 | 100 | 14 | 13 |
| T6 | Ark Kaynakçısı | 198 | 171 | 570 | 100 | 17 | 18 |
| T7 | Fırtına Eri | 275 | 238 | 792 | 100 | 20 | 25 |
| T8 | Yıldırım Mızrakçısı | 374 | 323 | 1.077 | 100 | 23 | 34 |
| T9 | Volta Şövalyesi | 495 | 428 | 1.425 | 100 | 27 | 45 |
| T10 | Derin Yıldırım | 660 | 570 | 1.900 | 100 | 32 | 60 |
<!-- /URET:tablo:birlikler.istatistik.arc -->

**Delici** (`driller`)

<!-- URET:tablo:birlikler.istatistik.driller -->
| Kademe | Ad | ATK | DEF | HP | Hız | Yük | Güç |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| T1 | El Matkapçısı | 27 | 27 | 110 | 70 | 20 | 3 |
| T2 | Hidrolik Kesici | 36 | 36 | 147 | 70 | 24 | 4 |
| T3 | Elmas Testere Ekibi | 54 | 54 | 220 | 70 | 28 | 6 |
| T4 | Su Jeti Ekibi | 81 | 81 | 330 | 70 | 34 | 9 |
| T5 | Burgu Taşıyıcı | 117 | 117 | 477 | 70 | 40 | 13 |
| T6 | Termik Mızrak Ekibi | 162 | 162 | 660 | 70 | 48 | 18 |
| T7 | Kaya Delici | 225 | 225 | 917 | 70 | 56 | 25 |
| T8 | Yürüyen Sondaj | 306 | 306 | 1.247 | 70 | 66 | 34 |
| T9 | Tünel Kurdu | 405 | 405 | 1.650 | 70 | 78 | 45 |
| T10 | Uçurum Burgusu | 540 | 540 | 2.200 | 70 | 92 | 60 |
<!-- /URET:tablo:birlikler.istatistik.driller -->

### 3.2 Askeri binaların kapasiteleri

Tablolar anahtar seviyeleri gösterir; aradaki seviyeler aynı çapalardan log-doğrusal okunur
(`veri/birlikler.json` → `kapasite.*.capalar`). Binaların maliyet ve süreleri kendi sayfalarındadır.

**[Askeri Kubbe](binalar/military_dome.md)** — ordu kapasitesi ve eğitim kuyruğu (K-15: en fazla 1 adet).

<!-- URET:tablo:birlikler.kapasite.askeri_kubbe -->
| Askeri Kubbe Sv | En erken R | Ordu kapasitesi | Eğitim kuyruğu |
| ---: | ---: | ---: | ---: |
| 1 | 5 | 300 | 1 |
| 5 | 5 | 800 | 1 |
| 10 | 10 | 2.500 | 2 |
| 15 | 15 | 6.000 | 2 |
| 20 | 20 | 12.000 | 2 |
| 25 | 25 | 19.000 | 3 |
| 30 | 30 | 27.000 | 3 |
| 35 | 35 | 37.000 | 3 |
| 40 | 40 | 48.000 | 4 |
| 45 | 45 | 60.000 | 4 |
| 50 | 50 | 75.000 | 4 |
<!-- /URET:tablo:birlikler.kapasite.askeri_kubbe -->

**[Tatbikat Havuzu](binalar/training_grounds.md)** — parti boyutu, en yüksek kademe ve eğitim hızı.

<!-- URET:tablo:birlikler.kapasite.tatbikat -->
| Tatbikat Sv | En erken R | Parti boyutu | En yüksek kademe | Eğitim hızı +% |
| ---: | ---: | ---: | --- | ---: |
| 1 | 5 | 40 | T1 | 0,5 |
| 5 | 5 | 100 | T2 | 2,5 |
| 10 | 10 | 250 | T3 | 5 |
| 15 | 15 | 500 | T4 | 7,5 |
| 20 | 20 | 900 | T5 | 10 |
| 25 | 25 | 1.400 | T6 | 12,5 |
| 30 | 30 | 2.000 | T7 | 15 |
| 35 | 35 | 2.700 | T8 | 17,5 |
| 40 | 40 | 3.500 | T9 | 20 |
| 45 | 45 | 4.400 | T10 | 22,5 |
| 50 | 50 | 5.500 | T10 | 25 |
<!-- /URET:tablo:birlikler.kapasite.tatbikat -->

**[Revir](binalar/med_bay.md)** — yaralı kapasitesi (iki örnek) ve iyileştirme hızı.

<!-- URET:tablo:birlikler.kapasite.revir -->
| Revir Sv | En erken R | Yaralı kapasitesi | İki Revir toplamı | İyileştirme hızı +% |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 6 | 100 | — | 1 |
| 5 | 6 | 300 | — | 5 |
| 10 | 10 | 700 | — | 10 |
| 15 | 15 | 1.600 | 3.200 | 15 |
| 20 | 20 | 3.000 | 6.000 | 20 |
| 25 | 25 | 5.000 | 10.000 | 25 |
| 30 | 30 | 7.200 | 14.400 | 30 |
| 35 | 35 | 10.000 | 20.000 | 35 |
| 40 | 40 | 13.000 | 26.000 | 40 |
| 45 | 45 | 16.500 | 33.000 | 45 |
| 50 | 50 | 20.000 | 40.000 | 50 |
<!-- /URET:tablo:birlikler.kapasite.revir -->

**[Kriyo Bakım Ünitesi](binalar/cryo_ward.md)** — savunmada Revir taşınca kurtarılabilecek asker sayısı.
Bina Reaktör <!--v:veri.binalar.binalar.cryo_ward.adet.0-->16<!--/v-->'da açılır; o anda o seviyeye kadar bütün seviyeler
birlikte erişilebilir olur.

<!-- URET:tablo:birlikler.kapasite.kriyo -->
| Kriyo Bakım Sv | En erken R | Kurtarma kapasitesi |
| ---: | ---: | ---: |
| 1 | 16 | 500 |
| 5 | 16 | 870 |
| 10 | 16 | 1.750 |
| 16 | 16 | 4.000 |
| 20 | 20 | 5.500 |
| 25 | 25 | 8.000 |
| 30 | 30 | 11.000 |
| 35 | 35 | 14.500 |
| 40 | 40 | 18.500 |
| 45 | 45 | 23.000 |
| 50 | 50 | 28.000 |
<!-- /URET:tablo:birlikler.kapasite.kriyo -->

**[Denizaltı Hangarı](binalar/submarine_hangar.md)** — bir denizaltının taşıyabileceği asker sayısı.
Dört hangar (Reaktör <!--v:veri.binalar.binalar.submarine_hangar.adet-->4, 10, 18, 26<!--/v-->) dört yürüyüş kuyruğudur; ayrıntı
[08 · Denizaltılar, Basınç, Seferler](08_denizaltilar_basinc_seferler.md).

<!-- URET:tablo:denizaltilar.hangar_kapasite -->
| Hangar Sv | Denizaltı asker kapasitesi | Dört hangar toplamı |
| ---: | ---: | ---: |
| 1 | 100 | 400 |
| 5 | 300 | 1.200 |
| 10 | 900 | 3.600 |
| 15 | 2.000 | 8.000 |
| 20 | 3.800 | 15.200 |
| 25 | 6.000 | 24.000 |
| 30 | 8.500 | 34.000 |
| 35 | 11.500 | 46.000 |
| 40 | 15.000 | 60.000 |
| 45 | 19.000 | 76.000 |
| 50 | 24.000 | 96.000 |
<!-- /URET:tablo:denizaltilar.hangar_kapasite -->

Oranlar bilinçli seçildi:

- Revir toplamı ordunun yarısından biraz fazlasını tutar (Sv 50'de <!--v:veri.birlikler.kapasite.revir.satirlar.10.iki_revir-->40.000<!--/v-->
  yaralı / <!--v:veri.birlikler.kapasite.askeri_kubbe.satirlar.10.ordu-->75.000<!--/v--> asker): küçük
  savaşlar tamamen iyileşir, büyük savaşta seçim yapmak gerekir.
- Dört hangar toplamı ordu kapasitesinin üstündedir (Sv 50'de <!--v:veri.denizaltilar.hangar_kapasite.satirlar.10.dort_hangar-->96.000<!--/v-->):
  dört kuyruğu olan oyuncu bütün ordusunu sahaya çıkarabilir, üç kuyruğu olan çıkaramaz.

### 3.3 Parti ve depo tavanı

Bir parti, kademesinin açıldığı reaktör seviyesindeki depo tavanına sığmalıdır; aksi hâlde oyuncu tek
bir partiyi bile ödeyemez. Tavanlar: [02 · Tempo ve Denge](02_tempo_ve_denge.md#7-depolama-tavan-ve-yağma-koruması).

| Kademe | R | Parti | Parti Gıda | Gıda tavanı (R) | Parti Titanyum | Titanyum tavanı (R) | Parti SA | Parti ham süre |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| T1 | 5 | <!--v:veri.birlikler.kademeler.t1.parti-->40<!--/v--> | <!--v:veri.birlikler.kademeler.t1.parti_gida-->240<!--/v--> | <!--v:hesap.tavan.Food.5-->5.000<!--/v--> | <!--v:veri.birlikler.kademeler.t1.parti_titanyum-->160<!--/v--> | <!--v:hesap.tavan.Titanium.5-->25.000<!--/v--> | — | <!--v:veri.birlikler.kademeler.t1.parti_sure|sure-->80 sn<!--/v--> |
| T2 | 5 | <!--v:veri.birlikler.kademeler.t2.parti-->100<!--/v--> | <!--v:veri.birlikler.kademeler.t2.parti_gida-->900<!--/v--> | <!--v:hesap.tavan.Food.5-->5.000<!--/v--> | <!--v:veri.birlikler.kademeler.t2.parti_titanyum-->600<!--/v--> | <!--v:hesap.tavan.Titanium.5-->25.000<!--/v--> | — | <!--v:veri.birlikler.kademeler.t2.parti_sure|sure-->5 dk<!--/v--> |
| T3 | 10 | <!--v:veri.birlikler.kademeler.t3.parti-->250<!--/v--> | <!--v:veri.birlikler.kademeler.t3.parti_gida-->3.500<!--/v--> | <!--v:hesap.tavan.Food.10-->5.000<!--/v--> | <!--v:veri.birlikler.kademeler.t3.parti_titanyum-->2.250<!--/v--> | <!--v:hesap.tavan.Titanium.10-->25.000<!--/v--> | — | <!--v:veri.birlikler.kademeler.t3.parti_sure|sure-->16 dk 40 sn<!--/v--> |
| T4 | 15 | <!--v:veri.birlikler.kademeler.t4.parti-->500<!--/v--> | <!--v:veri.birlikler.kademeler.t4.parti_gida-->15.000<!--/v--> | <!--v:hesap.tavan.Food.15-->24.100<!--/v--> | <!--v:veri.birlikler.kademeler.t4.parti_titanyum-->9.000<!--/v--> | <!--v:hesap.tavan.Titanium.15-->60.300<!--/v--> | — | <!--v:veri.birlikler.kademeler.t4.parti_sure|sure-->50 dk<!--/v--> |
| T5 | 20 | <!--v:veri.birlikler.kademeler.t5.parti-->900<!--/v--> | <!--v:veri.birlikler.kademeler.t5.parti_gida-->54.000<!--/v--> | <!--v:hesap.tavan.Food.20-->98.400<!--/v--> | <!--v:veri.birlikler.kademeler.t5.parti_titanyum-->31.500<!--/v--> | <!--v:hesap.tavan.Titanium.20-->246.000<!--/v--> | — | <!--v:veri.birlikler.kademeler.t5.parti_sure|sure-->2 sa<!--/v--> |
| T6 | 25 | <!--v:veri.birlikler.kademeler.t6.parti-->1.400<!--/v--> | <!--v:veri.birlikler.kademeler.t6.parti_gida-->154.000<!--/v--> | <!--v:hesap.tavan.Food.25-->305.000<!--/v--> | <!--v:veri.birlikler.kademeler.t6.parti_titanyum-->91.000<!--/v--> | <!--v:hesap.tavan.Titanium.25-->764.000<!--/v--> | — | <!--v:veri.birlikler.kademeler.t6.parti_sure|sure-->4 sa 16 dk<!--/v--> |
| T7 | 30 | <!--v:veri.birlikler.kademeler.t7.parti-->2.000<!--/v--> | <!--v:veri.birlikler.kademeler.t7.parti_gida-->380.000<!--/v--> | <!--v:hesap.tavan.Food.30-->780.000<!--/v--> | <!--v:veri.birlikler.kademeler.t7.parti_titanyum-->220.000<!--/v--> | <!--v:hesap.tavan.Titanium.30-->1.950.000<!--/v--> | — | <!--v:veri.birlikler.kademeler.t7.parti_sure|sure-->8 sa 20 dk<!--/v--> |
| T8 | 35 | <!--v:veri.birlikler.kademeler.t8.parti-->2.700<!--/v--> | <!--v:veri.birlikler.kademeler.t8.parti_gida-->810.000<!--/v--> | <!--v:hesap.tavan.Food.35-->1.720.000<!--/v--> | <!--v:veri.birlikler.kademeler.t8.parti_titanyum-->459.000<!--/v--> | <!--v:hesap.tavan.Titanium.35-->4.300.000<!--/v--> | — | <!--v:veri.birlikler.kademeler.t8.parti_sure|sure-->15 sa<!--/v--> |
| T9 | 40 | <!--v:veri.birlikler.kademeler.t9.parti-->3.500<!--/v--> | <!--v:veri.birlikler.kademeler.t9.parti_gida-->1.575.000<!--/v--> | <!--v:hesap.tavan.Food.40-->3.440.000<!--/v--> | <!--v:veri.birlikler.kademeler.t9.parti_titanyum-->875.000<!--/v--> | <!--v:hesap.tavan.Titanium.40-->8.610.000<!--/v--> | <!--v:veri.birlikler.kademeler.t9.parti_alasim-->70<!--/v--> | <!--v:veri.birlikler.kademeler.t9.parti_sure|sure-->1 g 1 sa 16 dk<!--/v--> |
| T10 | 45 | <!--v:veri.birlikler.kademeler.t10.parti-->4.400<!--/v--> | <!--v:veri.birlikler.kademeler.t10.parti_gida-->2.860.000<!--/v--> | <!--v:hesap.tavan.Food.45-->6.900.000<!--/v--> | <!--v:veri.birlikler.kademeler.t10.parti_titanyum-->1.584.000<!--/v--> | <!--v:hesap.tavan.Titanium.45-->17.200.000<!--/v--> | <!--v:veri.birlikler.kademeler.t10.parti_alasim-->176<!--/v--> | <!--v:veri.birlikler.kademeler.t10.parti_sure|sure-->1 g 17 sa 33 dk<!--/v--> |

Kural: bir partinin Gıda bedeli, açıldığı reaktördeki Gıda tavanının **%75'ini geçmez**. En sıkı nokta
T3'tür: Reaktör 10'da tavan hâlâ catalog v8 tabanındadır (<!--v:hesap.tavan.Food.10-->5.000<!--/v--> Gıda),
bu yüzden T3 ucuz tutulmuştur. Sonraki kademelerde oran tavanın yaklaşık yarısına iner; oyuncu iki parti
arasında depoyu baştan doldurmak zorunda kalmaz. Süperalaşım tavansızdır; T10 partisinin bedeli
<!--v:veri.birlikler.kademeler.t10.parti_alasim-->176<!--/v--> SA, F2P'nin geç oyundaki yaklaşık bir günlük
Süperalaşım gelirine denk gelir.

### 3.4 İyileştirme bedeli

<!-- URET:tablo:birlikler.iyilestirme.tablo -->
| Kademe | Gıda | Biyokütle | Titanyum | Süre (sn) | Kriyo uyandırma Gıda |
| --- | ---: | ---: | ---: | ---: | ---: |
| T1 | 1,8 | 0,6 | 1,2 | 0,5 | 0,9 |
| T2 | 2,7 | 0,9 | 1,8 | 0,75 | 1,35 |
| T3 | 4,2 | 1,5 | 2,7 | 1 | 2,1 |
| T4 | 9 | 2,7 | 5,4 | 1,5 | 4,5 |
| T5 | 18 | 5,4 | 10,5 | 2 | 9 |
| T6 | 33 | 9 | 19,5 | 2,75 | 16,5 |
| T7 | 57 | 15 | 33 | 3,75 | 28,5 |
| T8 | 90 | 24 | 51 | 5 | 45 |
| T9 | 135 | 36 | 75 | 6,5 | 67,5 |
| T10 | 195 | 51 | 108 | 8,5 | 97,5 |
<!-- /URET:tablo:birlikler.iyilestirme.tablo -->

### 3.5 Güç

```
asker gücü = Σ (sağlıklı + seferdeki askerler) × kademe gücü
```

Yaralılar, Kriyo'dakiler, eğitimdekiler ve ölüler güce sayılmaz. Toplam güç = bina + araştırma +
asker + kahraman (K-14, [02](02_tempo_ve_denge.md#9-güç-k-14)).

<!-- URET:tablo:birlikler.guc_ornekleri -->
| Ordu | Asker gücü |
| --- | ---: |
| 1.000 T1 karma | 3.000 |
| 10.000 T5 karma | 130.000 |
| 27.000 T7 (R30 kapasitesi) | 675.000 |
| 75.000 T10 (R50 kapasitesi) | 4.500.000 |
<!-- /URET:tablo:birlikler.guc_ornekleri -->

Ordu kapasitesi, tam ve en üst kademe bir ordunun gücü referans üssün bina gücüne yakın kalacak
şekilde seçildi (bkz. [5. İlerleme](#5-ilerleme)): savaşçı oyuncunun gücünün kabaca yarısı askerden,
yarısı binadan gelir.

**Kademe farkı gücün gösterdiğinden büyüktür.** ATK ve HP kademeyle birlikte büyüdüğü ve savunma
oranı (aşağıda) iki tarafın kademesini karşılaştırdığı için, bir üst kademenin askeri aynı güçteki alt
kademeye karşı belirgin üstündür (örnek: [4.6](#46-örnek-savaşlar)). Bu bilinçlidir: terfi, geç oyunun
anlamlı hedefi olur. Eşleştirme ve önerilen güç değerleri bunu hesaba katar.

---

## 4. Savaş çözümü

### 4.1 Akış

Savaş sunucuda, sabit bir sırayla ve rastgelelik olmadan çözülür:

1. **Hazırlık.** Her taraf tek bir komuta altında savaşır:
   - **Yürüyüş:** denizaltının Kaptanı ve Subayları.
   - **Ralli:** bütün katılımcıların askerleri liderin denizaltısında birleşir ve **liderin** Kaptanı
     ve Subayları komuta eder ([09](09_dunya_haritasi_ve_pvp.md#82-ralli)).
   - **Üs savunması:** garnizon ve gelen takviyeler birlikte, **savunma filosunun** (oyuncunun üste
     bekleyen filolarından seçtiği; seçmezse en güçlüsü) Kaptanı ve Subayları komutasında savaşır;
     takviye gönderenlerin kahramanları savaşa katılmaz.

   Askerler tür ve kademeye göre **yığınlara** ayrılır. Yığının son istatistikleri hesaplanır:
   `ATK' = ATK × (1 + Σ ATK bonusu)`; DEF ve HP için aynı. Bonuslar (kahraman, araştırma, teçhizat,
   ittifak, etkinlik, Kalkan, harita kristalleri) kategoriler içinde toplanır. Takviye askerleri kendi
   sahibinin araştırma bonuslarını taşır.
2. **Saf düzeni.** Zırhlı Dalgıç ve Delici **ön safta**, Zıpkıncı ve Ark Askeri **arka saftadır**.
3. **Turlar.** En çok <!--v:veri.birlikler.savas.tur_sayisi-->10<!--/v--> tur (boss savaşında
   <!--v:veri.birlikler.savas.boss_tur_sayisi-->15<!--/v-->). Her turda:
   1. Öfkesi dolan kahramanların aktif yetenekleri tur başında tetiklenir.
   2. İki taraf **aynı anda** vurur: hasarlar turun başındaki asker sayılarıyla hesaplanır.
   3. Kayıplar düşülür (kesirler bir sonraki tura taşınır).
   4. Bir tarafın kalan askeri başlangıcın %<!--v:veri.birlikler.savas.bozgun_yuzde-->30<!--/v-->'unun
      altındaysa o taraf **bozulur** ve çekilir.
4. **Sonuç.** Bozulan taraf kaybeder. İki taraf da ayaktaysa kalan güç oranı (kalan güç / başlangıç
   gücü) yüksek olan kazanır; eşitlikte **savunan** kazanır.
5. **Kayıp dağıtımı.** Her yığının kesirli kaybı en yakın tam sayıya yuvarlanır; kayıplar
   [2.6](#26-yaralı-ölü-kriyo-ve-kurtarma-kapsülü)'daki kurala göre yaralıya ve ölüye bölünür.

<!-- URET:tablo:birlikler.sabitler -->
| Sabit | Değer | Açıklama |
| --- | --- | --- |
| Tur sayısı | 10 (boss savaşında 15) | Savaş en çok bu kadar tur sürer. |
| Tur katsayısı C | 0,40 | Eşit iki ordu bir turda gücünün yaklaşık %6'sını kaybeder. |
| Bozgun eşiği | %30 | Bir tarafın kalan askeri başlangıcın %30'unun altına inerse o tur sonunda çekilir ve kaybeder. |
| Ön saf koruması | %10 | Ön safta başlangıcın %10'undan az asker kalınca ön saf düşmüş sayılır; bütün hasar arka safa gider. |
| Karşı avantajı | ×1,20 | Zırhlı > Ark > Zıpkıncı > Zırhlı. Anıtlar avantajı en çok +0,10 artırır. |
| Karşı dezavantajı | ×0,85 | Anıtlar dezavantajı değiştirmez. |
| Delici saha cezası | ×0,6 | Delici askerlere karşı. |
| Kuşatma çarpanı | Delici ×3, diğerleri ×1 | Kuşatma gücünde: hayatta kalan askerlerin gücü yapı hasarına bu çarpanla yansır (geçit, üs, yuva; 09). |
| Beraberlik | Savunan kazanır | 10 tur sonunda kimse bozulmadıysa kalan güç oranı yüksek olan kazanır; eşitlikte savunan. |
<!-- /URET:tablo:birlikler.sabitler -->

### 4.2 Hasar formülü

Saldıran yığın *a*, savunan yığın *d*'ye bir turda şu hasarı verir:

```
hasar(a → d) = n_a × ATK_a × ATK_a / (ATK_a + DEF_d) × K(a, d) × S(a, d) × C × pay(a → d)
kayıp(d)     = Σ_a hasar(a → d) / HP_d
```

| Terim | Anlamı |
|---|---|
| `n_a` | Saldıran yığındaki asker sayısı (turun başındaki) |
| `ATK_a / (ATK_a + DEF_d)` | Savunma oranı: eşit ATK ve DEF'te 0,5; DEF büyüdükçe hasar azalır ama hiç sıfırlanmaz |
| `K` | Karşı çarpanı (×1,2 / ×1 / ×0,85; anıtlar avantaja ekler) |
| `S` | Özel çarpan: Delici askerlere ×0,6; Basınç Hayaleti ilk tur hedef alınamaz (S = 0) |
| `C` | Tur katsayısı <!--v:veri.birlikler.savas.tur_katsayisi-->0,4<!--/v-->: eşit iki ordu turda ~%6 kaybeder |
| `pay` | Hasarın bu yığına düşen payı (saf kuralı × yığının saftaki asker payı) |

Sezgi: eşit iki karma ordu 10 turda yaklaşık yarısını kaybeder ve kimse bozulmaz, savunan kazanır.
%20 sayı üstünlüğü 10 turun sonunda oranla, %50 üstünlük 8. turda bozgunla kazanır. Formül
Lanchester'ın kare yasasına benzer: sayı üstünlüğü kendini besler, bu yüzden ralli ve takviye anlamlıdır.

### 4.3 Saflar ve hedefleme

Ön saf, arka safı korur. Her saldıran türün hasarı iki saf arasında sabit oranla bölünür; saf
içinde yığınlara asker sayısıyla orantılı dağılır.

<!-- URET:tablo:birlikler.hedefleme -->
| Saldıran | Ön safa % | Arka safa % | Not |
| --- | ---: | ---: | --- |
| Zırhlı Dalgıç | 70 | 30 | Varsayılan dağılım |
| Zıpkıncı | 85 | 15 | Delici atış: ağır hedefe odaklanır |
| Ark Askeri | 50 | 50 | Zincir ark: arka safa ulaşır |
| Delici | 100 | 0 | Ön saf ve yapılar |
| Taret (Sürü Baskını) | 70 | 30 | Basınç Topu önce kuşatma düşmanını vurur |
<!-- /URET:tablo:birlikler.hedefleme -->

- Ön safta başlangıcın %<!--v:veri.birlikler.savas.on_sira_yuzde-->10<!--/v-->'undan az asker kalınca
  ön saf **düşmüş** sayılır; o andan itibaren bütün hasar arka safa gider.
- Bir tarafın hiç arka safı yoksa bütün hasar ön safa, hiç ön safı yoksa arka safa gider.
- Saf düzeni ilk sürümde türden gelir; oyuncu dizilim seçmez. Kaptan yetenekleri ve teçhizat payları
  değiştirebilir (ör. "arka safa %20 daha fazla").

### 4.4 Öfke, yetenekler ve kahraman katkısı

Kahraman kuralları [05 · Kahramanlar](05_kahramanlar.md#32-filo-komutası-savaş)'tedir; bu bölüm onların
savaş formülüne nasıl girdiğini tanımlar.

<!-- URET:tablo:birlikler.kahraman_katkisi -->
| Kaynak | Kaptan | Subay | Askerlere etkisi |
| --- | --- | --- | --- |
| Komuta bonusu | Tam | Subay çarpanıyla | Kahramanın ATK, DEF ve HP'si askerlere yüzde olarak eklenir (05) |
| Aktif yetenek (öfke) | Çalışır | Çalışır | Her kahramanın kendi öfke çubuğu; dolunca tur başında tetiklenir |
| Pasif yetenek | Çalışır | Çalışır | Savaş boyunca sürekli |
| Sefer / basınç yeteneği | Çalışır | Çalışır | Dalış, basınç ve PvE karşılaşmalarında; ordu savaşında etkisiz |
| Komuta ve Uyanış yeteneği | Çalışır | Çalışmaz | Filoya, ralliye ya da garnizona yayılır |
| Yetenek ağacı ve teçhizat | Çalışır | Çalışmaz | 05, 17 |
| Sınıf yatkınlığı | Sayılır | Sayılmaz | Filonun yeterli payı Kaptan'ın türündense o türe bonus (05) |
<!-- /URET:tablo:birlikler.kahraman_katkisi -->

**Komuta bonusu.** Kaptan'ın ATK'sının her <!--v:veri.kahramanlar.komuta.atk_bolen-->100<!--/v--> puanı,
DEF'inin her <!--v:veri.kahramanlar.komuta.def_bolen-->100<!--/v--> puanı ve HP'sinin her
<!--v:veri.kahramanlar.komuta.hp_bolen-->1.000<!--/v--> puanı filonun bütün askerlerine +%1 ATK, DEF
ve HP olarak eklenir; Subaylar ×<!--v:veri.kahramanlar.komuta.subay_carpani-->0,4<!--/v--> ile katılır.
Bu yüzdeler 4.1'deki bonus toplamına girer.

**Sınıf yatkınlığı.** Filodaki askerlerin en az %<!--v:veri.kahramanlar.sinerji.yatkinlik_esik-->50<!--/v-->'si
Kaptan'ın yatkın olduğu türdense o türün ATK, DEF ve HP'si
+%<!--v:veri.kahramanlar.sinerji.yatkinlik_bonus-->5<!--/v--> artar. Komutan ve Hekim → Zırhlı Dalgıç;
Avcı ve Pilot → Zıpkıncı; Mühendis ve Bilim insanı → Ark Askeri. Delici'nin yatkın sınıfı yoktur.

**Öfke.** Kaptan ve iki Subay'ın her birinin kendi öfke çubuğu vardır; savaş
<!--v:veri.kahramanlar.ofke.baslangic-->200<!--/v--> ile başlar. Filo saldırdığı her tur
+<!--v:veri.kahramanlar.ofke.tur_basi-->150<!--/v-->, hasar aldığı her tur
+<!--v:veri.kahramanlar.ofke.hasar_alinca-->50<!--/v--> kazanır; çubuk
<!--v:veri.kahramanlar.ofke.tavan-->1.000<!--/v-->'de dolar ve aktif yetenek bir sonraki turun başında
kendiliğinden tetiklenir. Varsayılan ritimde aktif yetenek, kayıp verilen 10 turluk bir savaşta iki
kez (5. ve 10. turun başında), 15 turluk boss savaşında üç kez gelir; hiç kayıp vermeyen taraf daha
yavaş dolar. Pasifler, yetenek ağacı ve teçhizat bu ritmi bir tur kadar öne çeker
([05](05_kahramanlar.md)).

**Normal saldırı ve katsayı.** 05'teki yetenek değerleri **katsayı** ile yazılır:
<!--v:veri.kahramanlar.ofke.katsayi_birimi-->100<!--/v--> katsayı = bir normal saldırı.

```
normal saldırı = filonun o turdaki toplam hasarı (4.2'deki formül, bütün yığınlar, saf ve karşı kurallarıyla)
yetenek hasarı = katsayı / 100 × normal saldırı
iyileştirme    = katsayı / 100 × normal saldırı kadar HP; kurtarılan asker = HP / yığının HP'si (hafif yaralı döner)
```

- Normal saldırı askerlerden gelir ve Komuta bonusunu zaten içerir; yeteneği kullanan kahramanın ATK'sı
  ayrıca çarpılmaz. Subay'ın aktif yeteneği de aynı filo saldırısını kullanır.
- Hedefi yetenek metni seçer ("ön saf", "arka saf", "en kalabalık yığın"); yazmıyorsa 4.3'teki
  dağılım geçerlidir.
- Kalkan ve zayıflatma yetenekleri yüzde olarak yazılır ve 4.1'deki bonus toplamına o tur için eklenir.

Kahraman savaşta ölmez ve yaralanmaz; yalnızca askerler kaybedilir.

### 4.5 Kuşatma ve yapılar

Yapıların (oyuncu üssü, geçit, Rezonans Alanı, Leviathan yuvası, ittifak yapıları) **dayanıklılığı** ve
yenilginin sonucu (üssün su alması, sürüklenme, geçidin el değiştirmesi) [09 · Dünya Haritası ve
PvP](09_dunya_haritasi_ve_pvp.md)'dedir. Bu bölüm, askerlerin o hasara katkısını tanımlar:

1. **Garnizon savaşı.** Yapının içindeki askerler (üste ayrıca taretler) ile normal savaş.
2. **Kuşatma gücü.** Saldıran kazanırsa yapıya verilen hasar hayatta kalan askerlerin gücünden
   hesaplanır; Delici'nin gücü bu hesapta ×<!--v:veri.birlikler.karsi.yapi_carpani-->3<!--/v--> sayılır.

```
kuşatma gücü = Σ hayatta kalan asker gücü × kuşatma çarpanı   (Delici ×3, diğerleri ×1)
```

09'daki "kalan asker gücü" ile yazılmış her yapı formülü (ör. geçit dayanıklılığı) kuşatma gücünü
kullanır. Üs yenilgisindeki anlık dayanıklılık kaybı ise `kuşatma gücü / kalan asker gücü` oranıyla
çarpılır: Delici'siz bir ordu 09'daki kaybı aynen verir, yalnızca Delici'den oluşan bir ordu üç katını.

Yağma miktarı hayatta kalan askerlerin toplam yüküyle sınırlıdır (Delici'nin yükü en yüksektir);
yağma oranı, korumalı stok ve güç farkı kuralı 09'dadır.

### 4.6 Örnek savaşlar

Aşağıdaki sonuçlar formülün referans uygulamasıyla hesaplandı (kahraman ve bonus yok; "karma" = üç
türden eşit sayıda). Kayıplar yaralı + ölü toplamıdır.

<!-- URET:tablo:birlikler.ornek_savaslar -->
| Senaryo | Saldıran | Savunan | Güç oranı | Tur | Kazanan | Saldıran kayıp % | Savunan kayıp % |
| --- | --- | --- | ---: | ---: | --- | ---: | ---: |
| Ayna savaş | 10.000 T5 karma | 10.000 T5 karma | 1 | 10 | Savunan | 49 | 49 |
| %20 sayı üstünlüğü | 12.000 T5 karma | 10.000 T5 karma | 1,2 | 10 | Saldıran | 38 | 64 |
| %50 sayı üstünlüğü | 15.000 T5 karma | 10.000 T5 karma | 1,5 | 8 | Saldıran | 24 | 70 |
| Karşı: Zırhlı → Ark | 10.000 T5 Zırhlı Dalgıç | 10.000 T5 Ark Askeri | 1 | 10 | Saldıran | 33 | 50 |
| Karşı: Zıpkıncı → Zırhlı | 10.000 T5 Zıpkıncı | 10.000 T5 Zırhlı Dalgıç | 1 | 10 | Saldıran | 36 | 61 |
| Karşı: Ark → Zıpkıncı | 10.000 T5 Ark Askeri | 10.000 T5 Zıpkıncı | 1 | 9 | Saldıran | 44 | 72 |
| Ters karşı: Ark → Zırhlı | 10.000 T5 Ark Askeri | 10.000 T5 Zırhlı Dalgıç | 1 | 10 | Savunan | 50 | 33 |
| Kademe: T6 → T5, eşit sayı | 10.000 T6 karma | 10.000 T5 karma | 1,38 | 8 | Saldıran | 21 | 76 |
| Kademe: T7 → T5, ~eşit güç | 5.200 T7 karma | 10.000 T5 karma | 1 | 10 | Saldıran | 30 | 74 |
| Delici sahada | 10.000 T5 Delici | 10.000 T5 karma | 1 | 10 | Savunan | 60 | 16 |
<!-- /URET:tablo:birlikler.ornek_savaslar -->

Okuma notları:

- Üçgenin üç eşleşmesi benzer güçte avantaj verir; Ark → Zıpkıncı en hızlı biter çünkü iki tür de
  hafif korunur, Zırhlı → Ark en yavaş biter çünkü Zırhlı Dalgıç az vurur ama çok dayanır.
- Kademe farkı sayıdan güçlüdür: aynı güçteki T7 ordusu T5 ordusunu bozguna uğratır. Bu, terfinin değerini gösterir.
- Delici sahada yenilir; değeri kuşatmada ve taşımada ortaya çıkar.

### 4.7 Üs savunması ve Sürü Baskını

**Sürü Baskını**, üssün dalgalar hâlinde saldıran yaratıklara karşı savunulduğu kule savunması
benzeri bir PvE etkinliğidir. Etkinliğin takvimi, dalga sayısı ve ödülleri
[13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md)'tedir; bu bölüm **savunma matematiğini**
tanımlar. Aynı kurallar PvP'de üs savunmasına da uygulanır.

**Savunanlar.** Üsteki bütün sağlıklı askerler (seferde olmayanlar) garnizondur. Garnizonu
**savunma filosu** yönetir: oyuncunun üste bekleyen filolarından seçtiği filonun Kaptanı ve Subayları
(seçmezse üsteki en güçlü filo; hiç yoksa garnizon kahramansız savaşır). Kalkan Jeneratörü üç şey verir:

<!-- URET:tablo:birlikler.kalkan -->
| Kalkan Jeneratörü Sv | Kalkan dayanıklılığı | Taret yuvası | Taret gücü (eşdeğer asker) | Taret kademesi | Garnizon DEF +% |
| ---: | ---: | ---: | ---: | --- | ---: |
| 1 | 135 | 1 | 10 | T1 | 0,4 |
| 5 | 430 | 1 | 30 | T2 | 2 |
| 10 | 1.950 | 1 | 100 | T3 | 4 |
| 15 | 7.020 | 2 | 240 | T4 | 6 |
| 20 | 20.500 | 2 | 480 | T5 | 8 |
| 25 | 45.600 | 2 | 760 | T6 | 10 |
| 30 | 89.900 | 3 | 1.100 | T7 | 12 |
| 35 | 169.000 | 3 | 1.500 | T8 | 14 |
| 40 | 292.000 | 3 | 1.900 | T9 | 16 |
| 45 | 486.000 | 3 | 2.400 | T10 | 18 |
| 50 | 608.000 | 3 | 3.000 | T10 | 20 |
<!-- /URET:tablo:birlikler.kalkan -->

- **Kalkan dayanıklılığı** üssün dayanıklılığının üst sınırıdır; PvP'deki su alma ve sürüklenme bu
  sınırın yüzdesi olarak işler ([09](09_dunya_haritasi_ve_pvp.md#103-su-baskını-ve-sürüklenme)).
  Sürü Baskını aynı değerle **dolu başlayan ayrı bir sayaç** kullanır: bir dalganın sonunda hayatta
  kalan her düşman **sızar** ve sayaçtan kendi gücü kadar düşer (Kum Kazıcıları ×3). Sayaç 0'a inince
  etkinlik turu biter; o ana kadar geçilen dalgaların ödülü alınır. Kaynak kaybı yoktur, üs su almaz.
- **Taretler** savaşta hasar almayan sanal yığınlardır: her taret, tablodaki sayıda ve kademede bir
  asker yığını gibi vurur. Taret türünü oyuncu yuva başına seçer ve dalgalar arasında değiştirebilir
  (kule savunmasının karar anı):

<!-- URET:tablo:birlikler.taretler -->
| Taret | Savaşta sayılır | Hedef | En iyi |
| --- | --- | --- | --- |
| Zıpkın Bataryası | Zıpkıncı gibi | %85 ön saf | Uçurum Sürüngenleri ve Kadim Muhafızlara karşı |
| Ark Bobini | Ark Askeri gibi | %50 / %50 | Basınç Hayaletlerine karşı |
| Basınç Topu | Zırhlı Dalgıç gibi | Önce kuşatma düşmanı | Siren Sürüsü ve Kum Kazıcılarına karşı |
<!-- /URET:tablo:birlikler.taretler -->

- **Garnizon DEF bonusu** bütün savunan askerlere eklenir.

**Dalga savaşı.** Her dalga en çok 10 turluk normal bir savaştır, şu farklarla: savunanlar bozulmaz
(üs terk edilmez), düşmanlar da bozulmaz (sürü geri çekilmez). Kayıplar PvE kuralıyla yaralıdır.
Dalgalar arasında iyileştirme yapılmaz; aşınma kule savunmasının gerilimini kurar. Radar Merkezi bir
sonraki dalganın tür dağılımını gösterir (Radar 30 ve üstü; altında yalnızca toplam sayı).

**Düşmanlar.** Savaşta her düşman bir asker türü gibi sayılır; böylece üçgen PvE'de de işler:

<!-- URET:tablo:birlikler.dusmanlar -->
| Düşman | Kimlik | Savaşta sayılır | Rol | En iyi karşı | Özellik |
| --- | --- | --- | --- | --- | --- |
| Uçurum Sürüngenleri | abyss_crawler | Zırhlı (ön saf) | Kitin kabuklu, hızlı, kalabalık sürü | Zıpkıncı | Hız 130; HP ×0,8 |
| Siren Sürüsü | siren_swarm | Ark (arka saf) | Elektrik boşaltan yüzücü sürü | Zırhlı Dalgıç | ATK ×1,2; HP ×0,7; zincir ark |
| Kum Kazıcıları | sand_borer | Kuşatma (ön saf) | Tortulu delip yapıya tırmanan kazıcılar | Taret: Basınç Topu | Yapı hasarı ×3; sızma ×3 |
| Basınç Hayaletleri | pressure_wraith | Zıpkıncı (arka saf) | Yalnızca sonarda görünen sessiz avcılar | Ark Askeri | İlk tur hedef alınamaz |
| Kadim Muhafızlar | ancient_guardian | Zırhlı (ön saf) | Kadim uygarlığın biyomekanik askerleri | Zıpkıncı | DEF ×1,3; Ark Askerine dirençli; her 3 turda bir kalkan |
| Demir Leviathan | iron_leviathan | Zırhlı (boss) | Tek gövde, dev HP havuzu | Zıpkıncı | Her 4 turda yutma: ön safın %3'ü |
| Siren Kraliçesi | siren_queen | Ark (boss) | Siren Sürüsü'nü yöneten dev | Zırhlı Dalgıç | Her 3 turda sürü çağırır |
| Titan'ın Yavrusu | titan_spawn | Tarafsız (boss) | Hadal Titanı'nın uyanmış yavrusu | Yok (karşı çarpanı ×1) | Öfke kazancını yarıya indirir |
| Hadal Titanı | hadal_titan | Tarafsız (son boss) | Son oyun; ittifak savaşı | — | Ayrıntı: 14 · Son Oyun |
<!-- /URET:tablo:birlikler.dusmanlar -->

Düşman istatistikleri asker formülüyle aynı tabandan ve bir **eşdeğer kademeden** türetilir; dalga ve
aşama tabloları bu kademeyi ve sayıyı verir.

### 4.8 Keşif ve erken uyarı

**Erken uyarı** Radar Merkezi'nin seviye kilitlerinden gelir ([Radar Merkezi](binalar/radar_center.md)):

<!-- URET:tablo:birlikler.kesif_gelen -->
| Radar Merkezi Sv | Yaklaşan saldırı hakkında görülen |
| ---: | --- |
| 1 | Saldırı uyarısı: saldıranın adı, varış süresi |
| 10 | Saldıranın toplam asker sayısı |
| 20 | Saldıran Kaptan ve Subaylar |
| 30 | Asker türü dağılımı |
| 40 | Asker kademeleri ve takviyeler |
<!-- /URET:tablo:birlikler.kesif_gelen -->

**Keşif** için bir sonar dronu hedef üsse gönderilir (dron sayısı, menzil ve ping bedeli:
[08](08_denizaltilar_basinc_seferler.md#6-sis-ve-sonar-dronları)). Rapor, keşfedenin Radar seviyesine
göre katman katman açılır:

<!-- URET:tablo:birlikler.kesif_giden -->
| Kendi Radar Sv | Keşif raporunda görülen |
| ---: | --- |
| 1 | Yağmalanabilir kaynak (±%20), Kalkan Jeneratörü seviyesi ve dayanıklılığı |
| 5 | Garnizon toplamı (±%20) |
| 10 | Garnizon kesin sayısı |
| 20 | Savunma filosunun Kaptanı ve Subayları |
| 25 | İttifak takviyelerinin toplamı |
| 30 | Tür dağılımı, taret türleri |
| 40 | Kademeler, takviyelerin ayrıntısı |
| 45 | Hedefin askeri araştırma bonuslarının özeti |
<!-- /URET:tablo:birlikler.kesif_giden -->

- Hedefin Radar'ı keşfedenden <!--v:veri.birlikler.kesif.karistirma_farki-->10<!--/v--> ya da daha fazla
  seviye yüksekse rapor bir katman eksik gelir (**sonar karıştırma**).
- Hedef "sonarla tarandınız" bildirimi alır; keşfedenin adı Radar 20 ve üstünde görünür.
- Keşif savaş değildir: dron düşürülmez, kayıp olmaz. Kalkanlı ve korumalı üsler keşfedilemez; geçit
  ve alan garnizonları keşfedilebilir (09).
- Rapordaki "tür dağılımı" ve "kademeler", saldıranın karşı üçgenine göre ordu seçmesini sağlar; bu
  yüzden Radar 30 ve 40, savaşçı oyuncu için Askeri Kubbe kadar önemlidir.

---

## 5. İlerleme

Ordu kapasitesi, kademe ve maliyetler reaktör seviyesine birlikte bağlanır. Aşağıdaki tablo "her
bina o reaktör seviyesinde" olan referans üste, ordunun en yüksek kademeyle tam doldurulmasının
maliyetini ve bakımını gösterir (hesaplanmış; `veri/birlikler.json` → `denge`).

<!-- URET:tablo:birlikler.denge -->
| R | Ordu kapasitesi | En yüksek kademe | Tam ordu Gıda | Günlük Gıda üretimi | Kaç günlük üretim | Bakım Gıda/sa | Bakım / üretim % | Ordu gücü | Bina gücü (referans) |
| ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 10 | 2.500 | T3 | 35.000 | 313.968 | 0,1 | 1.000 | 8 | 15.000 | 17.312 |
| 15 | 6.000 | T4 | 180.000 | 601.560 | 0,3 | 3.000 | 12 | 54.000 | 63.304 |
| 20 | 12.000 | T5 | 720.000 | 1.552.320 | 0,5 | 7.200 | 11 | 156.000 | 176.721 |
| 25 | 19.000 | T6 | 2.090.000 | 2.037.360 | 1 | 13.300 | 16 | 342.000 | 343.977 |
| 30 | 27.000 | T7 | 5.130.000 | 3.548.160 | 1,4 | 21.600 | 15 | 675.000 | 682.445 |
| 35 | 37.000 | T8 | 11.100.000 | 4.539.504 | 2,4 | 33.300 | 18 | 1.258.000 | 1.130.246 |
| 40 | 48.000 | T9 | 21.600.000 | 7.539.840 | 2,9 | 48.000 | 15 | 2.160.000 | 1.892.081 |
| 45 | 60.000 | T10 | 39.000.000 | 10.016.136 | 3,9 | 66.000 | 16 | 3.600.000 | 2.921.308 |
| 50 | 75.000 | T10 | 48.750.000 | 12.023.976 | 4,1 | 82.500 | 16 | 4.500.000 | 4.182.944 |
<!-- /URET:tablo:birlikler.denge -->

Okuma:

- **Erken oyunda asker ucuzdur.** Reaktör 20'de tam ordu bir günlük Gıda üretiminin yarısına mal olur;
  oyuncu savaşmayı öğrenirken kayıptan korkmaz.
- **Geç oyunda asker bir plandır.** Reaktör 50'de tam T10 ordusu
  <!--v:veri.birlikler.denge.8.gun-->4,1<!--/v--> günlük Gıda üretimine, ayrıca Titanyum ve Süperalaşıma
  mal olur. Büyük bir savaşın ölüleri haftalık bir iş yükü yaratır; yaralılar ise
  %<!--v:veri.birlikler.iyilestirme.bedel_yuzde-->30<!--/v--> bedelle döner.
- **Bakım oransal kalır.** Ordu büyüdükçe bakım üretimle birlikte büyür; Reaktör 10'da üretimin
  %<!--v:veri.birlikler.denge.0.bakim_payi-->8<!--/v-->'i, Reaktör 50'de %<!--v:veri.birlikler.denge.8.bakim_payi-->16<!--/v-->'sı.
  Ordu ekonomiyi boğmaz.
- **Güç dengesi.** Tam ordunun gücü her aşamada referans bina gücüne yakındır.

F2P zaman çizelgesi (simülasyondan):

| Aşama | Reaktör | F2P günü | Ordu kapasitesi | En yüksek kademe |
|---|---:|---:|---:|---|
| İlk askerler | 5 | <!--v:hesap.sim.f2p.seviye.5|gun-->4 dk 48 sn<!--/v--> | <!--v:veri.birlikler.kapasite.askeri_kubbe.satirlar.1.ordu-->800<!--/v--> | T2 |
| İlk ralliler | 20 | <!--v:hesap.sim.f2p.seviye.20|gun-->4,5. gün<!--/v--> | <!--v:veri.birlikler.kapasite.askeri_kubbe.satirlar.4.ordu-->12.000<!--/v--> | T5 |
| Süperalaşım dönemi | 30 | <!--v:hesap.sim.f2p.seviye.30|gun-->28,6. gün<!--/v--> | <!--v:veri.birlikler.kapasite.askeri_kubbe.satirlar.6.ordu-->27.000<!--/v--> | T7 |
| T9 | 40 | <!--v:hesap.sim.f2p.seviye.40|gun-->128,4. gün<!--/v--> | <!--v:veri.birlikler.kapasite.askeri_kubbe.satirlar.8.ordu-->48.000<!--/v--> | T9 |
| T10 | 45 | <!--v:hesap.sim.f2p.seviye.45|gun-->224,9. gün<!--/v--> | <!--v:veri.birlikler.kapasite.askeri_kubbe.satirlar.9.ordu-->60.000<!--/v--> | T10 |
| Son | 50 | <!--v:hesap.sim.f2p.seviye.50|gun-->368,1. gün<!--/v--> | <!--v:veri.birlikler.kapasite.askeri_kubbe.satirlar.10.ordu-->75.000<!--/v--> | T10 |

---

## 6. F2P ve ödeyen oyuncu

| Konu | Ücretsiz oyuncu | Ödeyen oyuncu | Neden adil |
|---|---|---|---|
| Kademeye erişim | Aynı kapılar (Tatbikat Havuzu + araştırma) | Aynı kapılar; daha erken ulaşır | Kademe satılmaz |
| Ordu kapasitesi | Askeri Kubbe seviyesi | Aynı; satın alınamaz | Güç tavanı herkes için aynı |
| Eğitim hızı | Araştırma, kahraman, ittifak | + VIP hız bonusu ve hızlandırma | Hız avantajı süreyi kısaltır, tavanı yükseltmez |
| Kayıp | Revir + Kriyo + Kurtarma Kapsülü | Aynı | Kayıp telafisi parayla genişlemez |
| İyileştirme | %30 bedel, ittifak yardımı | + hızlandırma | Yaralı herkes için aynı oranla döner |
| PvE | Ölüm yok | Ölüm yok | Kampanya parayla kilitlenmez |

İlke: para **zamanı** satın alır, **tavanı** satın almaz. Kademe, kapasite ve karşı çarpanları satılık
değildir. Hızlandırma öğeleri ve VIP bonusları: [11 · VIP](11_vip.md), [12 · The Syndicate](12_syndicate.md).

---

## 7. Arayüz notları (Quiet Luxury)

- **Askeri Kubbe paneli:** dört tür sekmesi; her sekmede kademe şeridi (kilitli kademeler soluk, açılış
  koşulu tek satır). Parti kaydırıcısı; altında bedel, süre ve "tavanın %'si" ince bir çubukla.
  Bağıran renk yok; kaynak yetmiyorsa eksik miktar sakin bir tonla yazılır.
- **Karşı üçgeni** her zaman aynı küçük ikonla gösterilir (kabuk · zıpkın · ark). Bir düşmana dokununca
  "En iyi karşı: Zıpkıncı" satırı çıkar.
- **Savaş raporu:** sonar estetiğinde, tur tur oynatılabilir. Üstte kazanan, altında iki tarafın kayıpları
  "yaralı / ölü" diye ayrı; ölü sayısının yanında "sezon sonunda %50'si Kurtarma Kapsülü olarak döner"
  notu. Abartılı zafer ekranı yok; ince bir çizgi ve kısa bir ses.
- **Revir:** yaralı sayısı, kapasite çubuğu, "taşarsa ne olur" satırı (duruma göre: dinlenme / Kriyo /
  ölüm). Kapasite %80'i geçince sakin bir uyarı.
- **Kurtarma Kapsülü** postada sade bir kartla gelir: "Bu sezon kaybettiğin 1.240 askerin 620'si geri döndü."
- **Erken uyarı:** ekranın üstünde ince bir sonar halkası ve varış sayacı; titreşim tek, ses kısa. Oyuncu
  uyurken "saldırı yağmuru" bildirimi gönderilmez: bir saldırı, bir bildirim.
- Sayılar Türkçe biçimde, kısa: 75.000 değil gerekirse 75 B; tablolar binlik ayraçlı.

---

## 8. Uç durumlar

| Durum | Kural |
|---|---|
| Eğitim sürerken kapasite dolar (ör. Kapsül açıldı) | Süren eğitim biter; yeni eğitim başlamaz |
| Revir dolu, PvE savaşı | Taşan askerler dinlenmeye geçer, ölmez |
| Revir dolu, savunma, Kriyo dolu | Taşanların %50'si ölür, kalan %50'nin Kriyo'ya sığmayanı da ölür |
| Yaralı askeri terfi ettirmek | Olmaz; önce iyileşmeli |
| Seferdeki askeri terhis etmek | Olmaz; önce dönmeli |
| İki taraf aynı turda bozulur | Savunan kazanır |
| Hiç asker olmayan üsse saldırı | Taretler bir tur vurur; ardından üs yenilmiş sayılır ve kuşatma gücü uygulanır |
| Yalnız Delici ile saldırı | Saf kuralı işler: Delici ön saftadır; garnizon savaşında zayıftır |
| Bütün filolar seferde | Garnizon kahramansız savaşır (Komuta bonusu ve yetenek yok) |
| Gıda 0, Oksijen 0 | İki ceza toplanır (−%20); asker yine kaçmaz |
| Sezon biterken asker Revir'de | Yaralı ölü sayılmaz; Kapsül yalnızca ölüleri iade eder |
| Kapsül, açıldığı sezondan sonraki kademe değişikliği | Kapsül kademesiyle açılır; terfi ayrıca yapılır |
| Kesirli kayıplar | Savaş sonunda yığın başına en yakın tam sayıya yuvarlanır; toplam başlangıcı geçemez |
| Saldırı sırasında hedef üs yer değiştirir | Yürüyüş hedefsiz kalır ve döner (09) |
| Ralliye katılanın kahramanı | Savaşmaz; komuta liderindir. Katılımcının askerleri kendi araştırma bonuslarını taşır |
| Takviye gönderenin askerleri savunmada ölür | Ölüler gönderenin sezon kaydına yazılır; Kurtarma Kapsülü gönderene gelir |
| Bakım sırasında çevrimdışı | Bakım üretimle birlikte saatlik işlenir (12 saat birikim sınırı) |

---

## 9. Bugün kodda

| Öğe | Durum | Not |
|---|---|---|
| Asker türleri, kademeler, eğitim | ⬜ | Yok |
| Askeri Kubbe | 🟡 | Kodda bina var (v8: en fazla 2 adet, işlevsiz); K-15 ile 1 adet olacak |
| Tatbikat Havuzu, Revir, Kriyo Bakım Ünitesi, Denizaltı Hangarı | ⬜ | Binalar kodda yok |
| Radar Merkezi, Kalkan Jeneratörü | 🟡 | Binalar var, işlevleri yok |
| Anıtlar | ⬜ | Yok |
| Savaş çözümü | ⬜ | Sunucuda yok. Unity'de `CombatManager` (Assets/_Hadal/Scripts/Managers) yalnızca yerel bir HP düşürme taslağıdır; ağa bağlı değildir |
| Güç | 🟡 | Bugün `Σ bina seviyesi × 10 + kahraman gücü`; asker terimi yok |

---

## 10. Uygulama notları

- **Paylaşılan kurallar** (`shared/HADAL.Shared/Simulation`): yeni `TroopRules.cs` (istatistik, maliyet,
  kapasite, terfi, bakım), `CombatRules.cs` (savaş çözümü), `CasualtyRules.cs` (yaralı/ölü/Kriyo/Kapsül).
  Sunucu ve istemci aynı kodu çalıştırır; istemci yalnızca rapor oynatır ve önizleme gösterir.
- **Tam sayı matematiği.** Savaş binde bir hassasiyetli tam sayılarla (`long`, milli-birim) hesaplanır;
  kesirler yığın başına taşınır, böylece .NET sunucu ile Unity/Mono istemci arasında kayan nokta farkı
  oluşmaz (02'deki "hesaplanmış tam sayı dizileri" ilkesiyle aynı).
- **Deterministik sıra.** Yığınlar (taraf, denizaltı, tür sırası `armored, harpoon, arc, driller`,
  kademe azalan) sırasıyla işlenir; hasarlar turun başındaki sayılarla hesaplanır, kayıplar tur sonunda
  birlikte düşülür.
- **Savaş raporu** girdinin anlık görüntüsünü (yığınlar, bonuslar, kural sürümü) saklar; istemci aynı
  `CombatRules` ile turları yeniden üretir. Rapor `hadal.players.extras` yanında ayrı bir koleksiyonda
  tutulur (sezon sonu Kapsül hesabı ölüleri buradan toplar).
- **Katalog** (`shared/catalog/catalog.json`, v9 ve sonrası): `troops` bölümü (türler, kademeler,
  maliyet, süre, bakım), `combat` (sabitler, karşı tablosu), `buildingFunctions` içinde Askeri Kubbe,
  Tatbikat Havuzu, Revir, Kriyo, Kalkan değerleri. Değerler bu kılavuzun `veri/birlikler.json`'undan
  üretilir.
- **Sunucu** (`server/Hadal.Game`): `PlayerSession.Troops.cs` (eğitim, terfi, iyileştirme komutları:
  `TrainTroops`, `PromoteTroops`, `HealTroops`, `DismissTroops`), kuyruklar `GameTickService` adımlarında
  biter; ekonomiyi bölen her olay gibi ([24_Building_Functions](../24_Building_Functions.md) teknik notu).
- **Unity**: eski `CombatManager` taslağı kaldırılır ya da `CombatRules` raporunu oynatan bir görünüme
  dönüştürülür.
- **Test**: `Hadal.Game.Tests` içinde örnek savaşlar tablosundaki senaryolar birebir sınanır (kazanan,
  tur, kayıp yüzdeleri ±1).

---

## 11. Esin kaynakları

- **4X strateji türü:** üç türlü karşı üçgeni ve dışında bir kuşatma birimi; kademeleri araştırma ve
  binayla açmak; ağır yaralıların hastaneye gitmesi, taşanların ölmesi; iyileştirmenin kaynak ve zaman
  istemesi; şehir saldırısında farklı kayıp kuralları; sezon sonunda ölülerin bir kısmının iadesi.
- **Hayatta kalma temalı 4X:** bir savaşa birden çok kahraman, kahramanın rol, askerin sayı vermesi;
  yürüyüş kuyrukları; gözetleme kulesinin saldırı ayrıntılarını seviyeyle açması; dalgalı kule savunması.
- **Derin deniz mühendisliği:** tek atmosferli basınç kıyafetleri, zıpkın tüfekleri, sualtı ark kaynağı,
  aşındırıcılı su jeti ve termik mızrakla kesim ([18 · Bilim Temeli](18_bilim_temeli.md)).
- **Lanchester denklemleri:** sayı üstünlüğünün kare yasası.

İsimler, metinler ve sayılar özgündür; başka oyunlardan alınmamıştır.

---

## 12. Açık sorular

1. **Kademe araştırmaları.** T4–T10 için "Kademe IV–X Doktrini" adıyla Askeri dalda yedi teknoloji
   önerildi; kimlikler ve Araştırma Merkezi seviyeleri 06 yazarıyla eşlenmeli (öneri: Tatbikat
   seviyesiyle aynı lab seviyesi).
2. **Öfke ritmi.** Karar verildi: 05 öfkeyi güncelledi (başlangıç 200, tur başı 150, kayıpta 50);
   kayıplı 10 turluk savaşta iki, boss savaşında üç tetik (4.4).
3. **Kalkan Jeneratörü değerleri** (dayanıklılık, taret, DEF bonusu) bu bölümde tanımlandı; 09 bu
   dayanıklılığı üssün PvP dayanıklılığının üst sınırı olarak kullanır. `binalar.json`'a `etkiler`
   olarak taşınması ana yazarın kararı.
4. **Sürü Baskını dalga tabloları** 13'te: dalga gücünün garnizon gücüne mi, reaktör seviyesine mi
   bağlanacağı. Öneri: reaktör seviyesine (oyuncu ordusunu küçültüp kolay dalga almasın).
5. **Kademe farkı.** Savunma oranı kademe üstünlüğünü güçten büyük yapar (aynı güçte T7, T5'i bozar).
   İstenirse `ATK / (ATK + 0,5 × DEF)` ile yumuşatılabilir; karar canlı test verisine bırakılmalı.
6. **Denge tablosu** el ile hesaplandı; `uret.py`'ye "parti ≤ %75 tavan" ve "bakım ≤ %20 üretim"
   kontrollerinin eklenmesi önerilir.
7. **Kuşatma gücü** 09'un geçit ve su baskını formüllerinde kullanılıyor; Delici çarpanı (×3)
   değişirse iki bölüm birlikte güncellenmeli. Ralli komutası ve kapasitesi 09'daki gibidir.
8. **Delici'nin yatkın sınıfı yok** (05: Fetih ağacı onu taşır). Unutulanlar'ın köken birliği Hurda
   Delicisi (16) tek özel Delici'dir; Delici'nin yalnızca kuşatmada değer üretmesi bilinçli bir seçimdir.
