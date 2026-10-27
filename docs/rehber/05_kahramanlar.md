# 05 · Kahramanlar

> Kahraman rol ve yetenek verir, asker sayı verir. Hadal'da bir kahramanın iki işi vardır: üste
> **Komuta Ekibi**'nde ekonomiyi büyütmek, denizde bir denizaltının **Kaptan**'ı ya da **Subay**'ı
> olarak filoyu yönetmek. Bu bölüm iki işin kurallarını, sayılarını ve
> <!--v:hesap.sayilar.kahraman-->30<!--/v--> kahramanlık kadroyu tanımlar.

Bağlayıcı kararlar: [00 · Kanon](00_kanon.md) (K-04 nadirlik ve garanti, K-05 başlangıç kahramanı,
K-11 düşman adları, K-12 Hadalit ölçeği, K-13 hız formülü, K-14 güç, K-19 köken, K-25 canlılar avlanmaz).
Veri: `veri/kahramanlar.json`. Her kahramanın ayrıntılı sayfası [kahramanlar/](kahramanlar/) altında
üretilir. Teçhizat ayrı bölümdedir: [17 · Teçhizat](17_techizat.md).

---

## 1. Amaç ve ilkeler

1. **İki rol, tek kadro.** Aynı kahraman ya üste ekonomi becerisini çalıştırır ya da denizde
   savaşır; ikisini aynı anda yapamaz. Kadro büyüdükçe bu seçim gevşer ama hiç kaybolmaz.
   Böylece Nadir ve Destansı kahramanlar Efsanevilerin gölgesinde kalmaz: biri ekipte, öteki
   filoda iş görür.
2. **Kahraman rol, asker sayı.** Kahramanın istatistikleri filoyu yüzde olarak güçlendirir;
   savaşın ağırlığını asker sayısı ve kademesi taşır ([07 · Birlikler ve Savaş](07_birlikler_ve_savas.md)).
   Tek bir Efsanevi kahraman zayıf bir orduyu kurtaramaz, ama doğru kaptan güçlü bir orduyu
   kazandırır.
3. **Her nadirliğin bir yeri var.** Sıradan kahramanlar ilk günlerin ekibidir ve bazıları (Elias
   Rourke, Tess Ruiz) aylarca ekipte kalır. Nadirler köken kahramanlarıdır. Destansılar orta oyunun
   omurgası, Efsaneviler uzun vadeli hedeftir.
4. **Derinlikten gelen kimlik.** Biyografiler çukurlara, derin deniz mühendisliğinin derslerine ve
   doğa olaylarına bağlanır ([18 · Bilim Temeli](18_bilim_temeli.md)); savaş yalnızca Hadal'ın
   düşmanlarına karşıdır (K-25). Kahraman becerileri okyanusun canlılarına hiçbir zaman zarar
   vermez; taranan türler Tür Kataloğu'nda kalır.
5. **Sessiz lüks.** Kriyo-Kapsül'ün oranları, garanti sayaçları ve öne çıkan kahramanın kuralları
   her zaman görünür. Geri sayım, açılır pencere ve "son şans" dili yoktur
   ([12 · The Syndicate](12_syndicate.md)).

## 2. Hızlı bakış

| Konu | Değer |
| --- | --- |
| Kahraman sayısı | <!--v:hesap.sayilar.kahraman-->30<!--/v--> (Sıradan 4, Nadir 6, Destansı 9, Efsanevi 11) |
| Nadirlik | Sıradan, Nadir, Destansı, Efsanevi (K-04) |
| Kriyo-Kapsül oranı | Sıradan %<!--v:veri.kahramanlar.nadirlikler.common.agirlik-->65<!--/v--> · Nadir %<!--v:veri.kahramanlar.nadirlikler.rare.agirlik-->25<!--/v--> · Destansı %<!--v:veri.kahramanlar.nadirlikler.epic.agirlik-->8<!--/v--> · Efsanevi %<!--v:veri.kahramanlar.nadirlikler.legendary.agirlik-->2<!--/v--> |
| Garanti | Destansı+ <!--v:veri.kahramanlar.kriyo.destansi_garanti-->30<!--/v-->. çekimde, Efsanevi <!--v:veri.kahramanlar.kriyo.efsanevi_garanti-->80<!--/v-->. çekimde |
| Çekim bedeli | <!--v:veri.kahramanlar.kriyo.bedel_tek-->200<!--/v--> Hadalit; onlu <!--v:veri.kahramanlar.kriyo.bedel_onlu-->1.800<!--/v--> Hadalit (K-12) |
| Komuta Ekibi | 3 yuva; Reaktör <!--v:veri.kahramanlar.ekip_yuvalari.reaktor-->1, 5, 10<!--/v--> |
| Filo | Denizaltı başına 1 Kaptan + 2 Subay; hangarlar Reaktör <!--v:veri.binalar.binalar.submarine_hangar.adet-->4, 10, 18, 26<!--/v-->'da |
| Seviye tavanı | Yıldız × <!--v:veri.kahramanlar.seviye_kurali.yildiz_basi-->10<!--/v-->, Reaktör seviyesini geçemez |
| Yetenek | 3–5 yetenek, her biri 5 seviye; Anı Kristali ile yükselir |
| Yetenek ağacı | 14 uzmanlık, kahraman başına 3 ağaç, en çok <!--v:veri.kahramanlar.agac.puan.toplam-->60<!--/v--> puan, ağaç başına <!--v:veri.kahramanlar.agac.puan.agac_toplam-->35<!--/v--> |
| Uyanış | Destansı ve Efsanevi, 5★ sonrası |
| Fraksiyon sinerjisi | Filoda 3 aynı fraksiyon: birlik ATK/DEF/HP +<!--v:veri.kahramanlar.sinerji.fraksiyon_bonus|yuzde-->%5<!--/v--> |
| Çıkış | <!--v:veri.kahramanlar.nesiller.0.sayi-->18<!--/v--> kahraman; sonra <!--v:veri.kahramanlar.nesiller.1.gun-->56<!--/v-->., <!--v:veri.kahramanlar.nesiller.2.gun-->112<!--/v-->. ve <!--v:veri.kahramanlar.nesiller.3.gun-->168<!--/v-->. günlerde 4'er kahraman |

---

## 3. İki rol: Komuta Ekibi ve filo

<!-- URET:tablo:kahramanlar.roller -->
| Rol | Yer | Çalışan yetenekler | İstatistik katkısı | Yetenek ağacı | Teçhizat | Not |
| --- | --- | --- | --- | --- | --- | --- |
| Komuta Ekibi | Üs; 3 yuva | Yalnızca mürettebat becerisi | — | — | — | Seviye ve yıldız beceri gücünü belirler |
| Kaptan | Denizaltı başına 1 | Tüm açık yetenekler (Komuta ve Uyanış dahil) | Komuta bonusu ×1 | Evet | Evet | Filonun sınıf yatkınlığı ve fraksiyon sinerjisi Kaptan'dan sayılır |
| Subay | Denizaltı başına 2 | Aktif, pasif ve sefer yetenekleri | Komuta bonusu ×0,4 | Hayır | Hayır | Kendi öfke çubuğu vardır; Komuta ve Uyanış çalışmaz |
| Savunma Kaptanı / Subay | Askeri Kubbe; 1 + 2 koltuk | Kaptan ve Subay kurallarıyla aynı | Kaptan ×1, Subay ×0,4 | Kaptan'da | Kaptan'da | Üs savunmasında garnizonu yönetir; o sırada denizaltıda olamaz (07, 08) |
<!-- /URET:tablo:kahramanlar.roller -->

### 3.1 Komuta Ekibi (ekonomi) ✅

Bugünkü oyunun kahraman sistemi budur ve aynen korunur. Üç yuva Reaktör
<!--v:veri.kahramanlar.ekip_yuvalari.reaktor-->1, 5, 10<!--/v-->'da açılır; Komutan Elias Rourke ilk yuvada
başlar. Yalnızca ekipteki kahramanın **mürettebat becerisi** çalışır. Beceri araştırma etkileriyle
aynı türdendir ve araştırmanın üstüne eklenir:

```
beceri = taban + ⌊(Sv − 1) × seviye_onda / 10⌋ + (★ − 1) × yıldız_başı      (HeroRules.EffectValue)
```

Her kahramanın beceri tablosu kendi sayfasındadır. Sekiz eski kahramanın değerleri catalog v8 ile
birebir aynıdır; yeni kahramanlar aynı ölçeği izler. Standart eğri nadirlikle büyür. Oyunun
temposunu doğrudan değiştiren etkiler (inşa ve araştırma süresinin Efsanevi sürümleri,
Süperalaşım, tüm üretim, asker eğitim bedeli, Hadalit şansı) bilinçli olarak daha düşük eğridedir:

<!-- URET:tablo:kahramanlar.murettebat_egrileri -->
| Eğri | Taban / Sv başına onda / ★ başına | 5★ Sv 50 (mutlak) | Kimler |
| --- | --- | ---: | --- |
| Sıradan · standart | 3 / 2 / 2 | %20 | Rourke, Moss, Ruiz, Kostina |
| Nadir · standart | 4 / 3 / 3 | %30 | Kael, Hale, Okafor, Quispe, Farahani, Noa |
| Destansı · standart | 5 / 4 / 4 | %40 | Vega, Sable, Sakamoto, Ilo, Gröndahl, Karaca, Santoso |
| Destansı · düşük | 3 / 2 / 2 | %20 | Raman (Süperalaşım), Ferreira (asker eğitim bedeli) |
| Efsanevi · standart | 8 / 5 / 5 | %52 | Aelis, Kavanagh, Kiri, Dela Cruz |
| Efsanevi · tempo | 6 / 4 / 3 | %37 | Meridyen (inşa), Han Seo-yeon (araştırma), Varga, Sela |
| Efsanevi · geniş etki | 5 / 3 / 2 | %27 | Boateng (tüm üretim), Ufuk (Süperalaşım) |
| Efsanevi · premium | 4 / 2 / 2 | %21 | Sura (Hadalit bulma şansı) |
<!-- /URET:tablo:kahramanlar.murettebat_egrileri -->

**Süre becerileri ve K-13.** Mürettebat becerisi süreyi eksi yüzdeyle gösterir ("İnşa süresi
−%20"); hedef formülde bu değer **hız** olarak bölene eklenir: süre = ham süre / (1 + Σhız / 100).
Örnek: %20 hız süreyi %16,7 kısaltır; iki becerinin toplamı %57 olursa süre %36 kısalır. Bölen
sıfıra inmediği için Efsanevi halefler eskileri anlamsız kılmaz, yalnızca üstüne biner. Bugünkü kod
yüzdeyi süreden çıkarıp %10'da durduruyor; geçiş v9 ile yapılır
([02 · Tempo ve Denge](02_tempo_ve_denge.md)).

**Bedel ve tüketim becerileri** (eğitim bedeli, iyileştirme bedeli, Gıda ve Oksijen tüketimi)
bölene girmez, çarpan olarak uygulanır: bedel × (1 − Σ% / 100). Bütün kaynaklardan toplanan
indirim, bedeli hiçbir zaman ilk değerinin %<!--v:veri.kahramanlar.kurallar.bedel_alt_siniri-->25<!--/v-->'inin
altına indiremez.

**Ekip rezonansı.** Ekipteki üç kahraman da aynı fraksiyondansa fraksiyonun ekip bonusu açılır
(+<!--v:veri.kahramanlar.sinerji.ekip_bonus|yuzde-->%5<!--/v-->, bkz. bölüm 10). Çıkışta her köken için
en az bir üçlü kurulabilir.

#### Mürettebat becerisi rehberi

Hangi kahraman hangi ekonomi işini görür; aynı satırdaki daha nadir kahraman, ötekinin doğal
halefidir.

| Alan | Sıradan / Nadir | Destansı | Efsanevi |
| --- | --- | --- | --- |
| İnşa | Komutan Elias Rourke | — | Meridyen |
| Araştırma | Dr. Orin Hale | — | Dr. Han Seo-yeon |
| Temel üretim | Dr. Lyra Moss (Oksijen), Tess Ruiz (Titanyum), Kael (Gıda) | Juno Vega (Enerji) | Aelis (Biyokütle), Amiral Kwame Boateng (tüm üretim) |
| Süperalaşım | — | Dr. Priya Raman | Ufuk |
| Depo ve tüketim | — | Sable (tüm depolar) | Sela (üs Oksijen tüketimi), Ilian Varga (ordu Gıda tüketimi) |
| Ordu | Kaptan Amara Okafor (eğitim süresi) | Odalys Ferreira (eğitim bedeli) | — |
| Revir | — | Dr. Emi Sakamoto (iyileştirme hızı), Ilo (Revir kapasitesi) | Dr. Isagani Dela Cruz (iyileştirme bedeli) |
| Harita | Vera Kostina (toplama hızı), Noa (Sonar Enerjisi) | — | Saoirse Kavanagh (yük kapasitesi) |
| Kurtarma ve av | Rafael Quispe (kurtarma ödülü) | Mattias Gröndahl (av ödülü) | Kiri (av ödülü) |
| Kahraman ve teçhizat | Darius Farahani (kahraman eğitim bedeli) | Deniz Karaca (teçhizat süresi), Wira Santoso (malzeme şansı) | — |
| Premium | — | — | Sura (Hadalit bulma şansı) |

### 3.2 Filo komutası (savaş) ⬜

Her denizaltı bir **filodur**: bir Kaptan, en çok iki Subay ve hangarın taşıdığı askerler.
Hangarlar Reaktör <!--v:veri.binalar.binalar.submarine_hangar.adet-->4, 10, 18, 26<!--/v-->'da açıldığı
için oyuncu en çok dört filo, yani dört Kaptan ve sekiz Subay çıkarır. Eski tasarımdaki "3 kahraman
+ 1 denizaltı" seferi ([08 · Denizaltılar, Basınç, Seferler](08_denizaltilar_basinc_seferler.md)) bu
kadronun aynısıdır.

- **Kaptan** filonun her şeyidir: bütün açık yetenekleri çalışır, yetenek ağacı ve teçhizatı
  yalnızca onda etkilidir, filonun sınıf yatkınlığı ve fraksiyon sinerjisi ondan sayılır.
- **Subay** yetenek taşır: aktif, pasif ve sefer yetenekleri çalışır; Komuta ve Uyanış yetenekleri,
  ağaç ve teçhizat çalışmaz. Subay boş bırakılabilir.
- Bir kahraman aynı anda **tek yerde** olur: Komuta Ekibi, bir filo ya da Askeri Kubbe'deki
  **savunma komutası** (Savunma Kaptanı + iki Subay; üs savunmasında garnizonu yönetir,
  [07](07_birlikler_ve_savas.md) ve [08](08_denizaltilar_basinc_seferler.md)). Ekipteki kahramanı
  filoya koymak için önce ekipten çıkarmak gerekir; arayüz bunu tek dokunuşla takas olarak önerir.
- **Ralli:** bütün katılımcıların askerleri liderin filosunda birleşir ve liderin Kaptan'ı ile
  Subayları komuta eder; katılımcıların kahramanları savaşmaz (07). Bu yüzden "ralli lideriyken"
  yazan Komuta yetenekleri ralliye katılan bütün askerlere işler.
- Yürüyüşteki ya da garnizondaki filonun kadrosu kilitlidir; filo üsse dönünce değişir.

**Komuta bonusu.** Kahramanın istatistikleri filonun askerlerine yüzde olarak eklenir:

```
birlik ATK %  += Kaptan ATK / b_ATK  + Σ Subay ATK / b_ATK × s
birlik DEF %  += Kaptan DEF / b_DEF  + Σ Subay DEF / b_DEF × s
birlik HP %   += Kaptan HP  / b_HP   + Σ Subay HP  / b_HP  × s
```

Bölenler ve Subay çarpanı veridedir: b_ATK = <!--v:veri.kahramanlar.komuta.atk_bolen-->100<!--/v-->,
b_DEF = <!--v:veri.kahramanlar.komuta.def_bolen-->100<!--/v-->, b_HP = <!--v:veri.kahramanlar.komuta.hp_bolen-->1.000<!--/v-->,
s = <!--v:veri.kahramanlar.komuta.subay_carpani-->0,4<!--/v-->. Kural olarak: ATK'sı 2.000 olan bir Kaptan
askerlere +%20 ATK verir; aynı kahraman Subay'ken bunun %40'ını verir. Sv 50, 5★ Aelis'in ATK'sı
<!--v:hesap.kahraman.aelis.stat_50.atk-->2.078<!--/v-->, Amiral Kwame Boateng'in HP'si
<!--v:hesap.kahraman.kwame.stat_50.hp-->20.782<!--/v-->'dir. Bu denge savaşın ağırlığını askerde tutarken
kahramanı vazgeçilmez kılar; nihai savaş formülü [07](07_birlikler_ve_savas.md)'dedir.

**Seferde kahramanlar.** Sefer karşılaşmalarında (sonar-yalnız bölgeler, kadim kalıntılar,
canavar yuvaları) üç kahraman savaş alanında birim olarak da görünür; ATK, DEF ve HP orada doğrudan
kullanılır ve "sefer / basınç" yetenekleri devreye girer
([08](08_denizaltilar_basinc_seferler.md)).

#### Örnek filolar

| Filo | Kaptan | Subaylar | Neden |
| --- | --- | --- | --- |
| İlk duvar | Kaptan Amara Okafor | Komutan Elias Rourke, Dr. Lyra Moss | Zırhlı yatkınlık, kalkan + iyileştirme; ilk haftanın PvE'si |
| İlk avcılar | Kael | Noa, Vera Kostina | Zıpkıncı yatkınlık, hız; Uçurum Sürüngenleri'ne karşı üstün |
| Ark yetenek | Aelis | Dr. Orin Hale, Darius Farahani | Alan hasarı + aktif yetenek ve öfke pasifleri |
| Kubbe savunması | Amiral Kwame Boateng | Sable, Dr. Emi Sakamoto | Garnizon ağacı, kalkanlar, kayıp azaltma |
| Leviathan rallisi | Kiri | Mattias Gröndahl, Sura | Avcılık ağacı, Av Lideri, tek hedef hasarı |
| Tarikat saldırısı | Ilian Varga | Odalys Ferreira, Sura | Uçurum Tarikatı sinerjisi, Vaaz + Çağrı |
| Koloni | Sela | Aelis, Kiri | Derindoğan sinerjisi + Koloni; en dayanıklı karma filo |
| Toplama | Saoirse Kavanagh | Vera Kostina, Noa | Yük, hız, Toplama ağacı; savaşa girmez |

---

## 4. İstatistikler ve güç

Her kahramanın üç istatistiği vardır: **ATK**, **DEF**, **HP**.

```
istatistik = nadirlik tabanı × sınıf çarpanı × (1 + a × (Sv − 1)) × (1 + b × (★ − 1))
güç        = (ATK + DEF + HP / 10) × 10                                    (K-14, 02 · bölüm 9)
```

Seviye ve yıldız katsayıları veridedir: a = <!--v:veri.kahramanlar.stat_formulu.seviye-->0,08<!--/v-->,
b = <!--v:veri.kahramanlar.stat_formulu.yildiz-->0,15<!--/v-->. Seviye büyümesi doğrusaldır ve yıldızla
çarpılır; bu yüzden bir kahramanı yıldızlamadan yalnızca eğitmek yarım iştir.

### Nadirlik tabanları

| Nadirlik | ATK | DEF | HP | Eğitim çarpanı | Çağırma (kart) | Kopya parçası | Fazla parça → Anı Kristali | Yetenek |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Sıradan | <!--v:veri.kahramanlar.nadirlikler.common.taban.atk-->100<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.common.taban.def-->80<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.common.taban.hp-->1.000<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.common.egitim_carpani-->1<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.common.cagri_parca-->10<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.common.kopya_parca-->10<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.common.fazla_parca_kristal-->1<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.common.yetenek_sayisi-->3<!--/v--> |
| Nadir | <!--v:veri.kahramanlar.nadirlikler.rare.taban.atk-->130<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.rare.taban.def-->105<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.rare.taban.hp-->1.300<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.rare.egitim_carpani-->1,2<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.rare.cagri_parca-->20<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.rare.kopya_parca-->10<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.rare.fazla_parca_kristal-->2<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.rare.yetenek_sayisi-->3<!--/v--> |
| Destansı | <!--v:veri.kahramanlar.nadirlikler.epic.taban.atk-->170<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.epic.taban.def-->135<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.epic.taban.hp-->1.700<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.epic.egitim_carpani-->1,5<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.epic.cagri_parca-->30<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.epic.kopya_parca-->15<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.epic.fazla_parca_kristal-->3<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.epic.yetenek_sayisi-->4<!--/v--> |
| Efsanevi | <!--v:veri.kahramanlar.nadirlikler.legendary.taban.atk-->220<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.legendary.taban.def-->175<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.legendary.taban.hp-->2.200<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.legendary.egitim_carpani-->2<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.legendary.cagri_parca-->50<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.legendary.kopya_parca-->20<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.legendary.fazla_parca_kristal-->5<!--/v--> | <!--v:veri.kahramanlar.nadirlikler.legendary.yetenek_sayisi-->5<!--/v--> |

Eğitim çarpanı, çağırma ve kopya parçası catalog v8 değerleridir (✅). Bugünkü kodun nadirlik taban
gücü (Sıradan <!--v:veri.kahramanlar.nadirlikler.common.v8_guc-->100<!--/v-->, Efsanevi
<!--v:veri.kahramanlar.nadirlikler.legendary.v8_guc-->320<!--/v-->) stat tabanlı güçle değişir (🟡).

### Sınıflar

| Sınıf | ATK × | DEF × | HP × | Yatkın birlik | Rol | Ağaç eğilimi |
| --- | ---: | ---: | ---: | --- | --- | --- |
| Komutan | <!--v:veri.kahramanlar.siniflar.Commander.carpan.atk-->0,85<!--/v--> | <!--v:veri.kahramanlar.siniflar.Commander.carpan.def-->1,2<!--/v--> | <!--v:veri.kahramanlar.siniflar.Commander.carpan.hp-->1,2<!--/v--> | Zırhlı Dalgıç | <!--v:veri.kahramanlar.siniflar.Commander.rol-->Ön saf ve liderlik: dayanıklı, birlik kapasitesi ve ralli ağaçları.<!--/v--> | <!--v:veri.kahramanlar.siniflar.Commander.agac_egilimi-->Liderlik, Zırhlı, Garnizon, Fetih<!--/v--> |
| Hekim | <!--v:veri.kahramanlar.siniflar.Medic.carpan.atk-->0,8<!--/v--> | <!--v:veri.kahramanlar.siniflar.Medic.carpan.def-->1,1<!--/v--> | <!--v:veri.kahramanlar.siniflar.Medic.carpan.hp-->1,3<!--/v--> | Zırhlı Dalgıç | <!--v:veri.kahramanlar.siniflar.Medic.rol-->İyileştirme, kalkan ve kayıp azaltma; en yüksek HP.<!--/v--> | <!--v:veri.kahramanlar.siniflar.Medic.agac_egilimi-->Destek, Zırhlı, Savunma<!--/v--> |
| Avcı | <!--v:veri.kahramanlar.siniflar.Hunter.carpan.atk-->1,25<!--/v--> | <!--v:veri.kahramanlar.siniflar.Hunter.carpan.def-->0,9<!--/v--> | <!--v:veri.kahramanlar.siniflar.Hunter.carpan.hp-->0,95<!--/v--> | Zıpkıncı | <!--v:veri.kahramanlar.siniflar.Hunter.rol-->Tek hedef hasarı ve canavar avı; en yüksek ATK.<!--/v--> | <!--v:veri.kahramanlar.siniflar.Hunter.agac_egilimi-->Avcılık, Zıpkın, Saldırı<!--/v--> |
| Pilot | <!--v:veri.kahramanlar.siniflar.Pilot.carpan.atk-->1,15<!--/v--> | <!--v:veri.kahramanlar.siniflar.Pilot.carpan.def-->0,95<!--/v--> | <!--v:veri.kahramanlar.siniflar.Pilot.carpan.hp-->1<!--/v--> | Zıpkıncı | <!--v:veri.kahramanlar.siniflar.Pilot.rol-->Hız, yük ve sefer; haritada en çevik kaptan.<!--/v--> | <!--v:veri.kahramanlar.siniflar.Pilot.agac_egilimi-->Akıntı, Zıpkın, Toplama, Derinlik<!--/v--> |
| Mühendis | <!--v:veri.kahramanlar.siniflar.Engineer.carpan.atk-->1<!--/v--> | <!--v:veri.kahramanlar.siniflar.Engineer.carpan.def-->1,15<!--/v--> | <!--v:veri.kahramanlar.siniflar.Engineer.carpan.hp-->1,05<!--/v--> | Ark Askeri | <!--v:veri.kahramanlar.siniflar.Engineer.rol-->Ark birlikleri, garnizon, kuşatma ve üretim.<!--/v--> | <!--v:veri.kahramanlar.siniflar.Engineer.agac_egilimi-->Ark, Garnizon, Fetih, Toplama<!--/v--> |
| Bilim insanı | <!--v:veri.kahramanlar.siniflar.Scientist.carpan.atk-->1,2<!--/v--> | <!--v:veri.kahramanlar.siniflar.Scientist.carpan.def-->0,85<!--/v--> | <!--v:veri.kahramanlar.siniflar.Scientist.carpan.hp-->0,9<!--/v--> | Ark Askeri | <!--v:veri.kahramanlar.siniflar.Scientist.rol-->Aktif yetenek hasarı ve öfke; sefer ve araştırma.<!--/v--> | <!--v:veri.kahramanlar.siniflar.Scientist.agac_egilimi-->Beceri, Ark, Derinlik, Destek<!--/v--> |

Sınıf çarpanları güç bakımından birbirine yakındır; fark istatistiğin **dağılımındadır**. Bilim insanının düşük ham gücü yetenek hasarıyla, Hekim'in düşük
ATK'sı iyileştirmeyle kapanır.

### Örnek güç (Sv 50, 5★)

| Kahraman | Nadirlik · sınıf | Güç |
| --- | --- | ---: |
| Komutan Elias Rourke | Sıradan · Komutan | <!--v:hesap.kahraman.mara.guc_50-->23.696<!--/v--> |
| Kael | Nadir · Avcı | <!--v:hesap.kahraman.kael.guc_50-->29.952<!--/v--> |
| Juno Vega | Destansı · Pilot | <!--v:hesap.kahraman.juno.guc_50-->38.872<!--/v--> |
| Aelis | Efsanevi · Bilim insanı | <!--v:hesap.kahraman.aelis.guc_50-->48.077<!--/v--> |
| Amiral Kwame Boateng | Efsanevi · Komutan | <!--v:hesap.kahraman.kwame.guc_50-->52.032<!--/v--> |

Kahraman gücü toplam güce eklenir (K-14). Dört filo ve üç ekip kahramanı tam güçteyken kahraman
payı, Reaktör 50'deki bina gücünün kabaca onda biri kadardır; kahraman güçlü ama üssün yerini
tutmaz. Teçhizat gücü ayrıca eklenir ([17](17_techizat.md)).

**Araştırmayla büyüyenler.** Araştırmanın [Kahraman dalı](arastirma/heroes.md) bu bölümün
değerlerini yüzde olarak büyütür: kahraman deneyimi, eğitim bedeli, kahraman ATK/DEF/HP, mürettebat
becerisi etkisi, Subayların deneyim payı, öfke birikimi, teçhizat istatistikleri ve fraksiyon
sinerjisi. Araştırma tabanı değiştirmez, üstüne biner; süre ve bedel kuralları (K-13, alt sınır)
burada da geçerlidir.

---

## 5. Yıldız, parça, seviye ve eğitim

### 5.1 Yıldızlar ✅

<!-- URET:tablo:kahramanlar.yildizlar -->
| Yıldız | Bu yıldız için parça | Toplam parça | Seviye tavanı | Ağaç puanı (tavan seviyede) | Açılan yetenek | Not |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| 1★ | 0 | 0 | 10 | 9 | Aktif yetenek (öfke) | Çağırma: Sıradan 10, Nadir 20, Destansı 30, Efsanevi 50 kart |
| 2★ | 10 | 10 | 20 | 21 | Pasif yetenek | +2 ağaç puanı |
| 3★ | 20 | 30 | 30 | 33 | Sefer yeteneği (Destansı, Efsanevi) | +2 ağaç puanı |
| 4★ | 40 | 70 | 40 | 46 | Sefer yeteneği (Sıradan, Nadir) · Komuta yeteneği (Efsanevi) | +3 ağaç puanı |
| 5★ | 80 | 150 | 50 | 60 | Uyanış hakkı (Destansı, Efsanevi) | +4 ağaç puanı; fazla parça Anı Kristali'ne döner |
<!-- /URET:tablo:kahramanlar.yildizlar -->

Parça (yıldız parçası) üç yoldan gelir: sahip olunan kahramanın Kriyo-Kapsül'den yeniden çıkması
(kopya parçası, nadirlik tablosu), kahraman kartları (bölüm 12) ve etkinliklerin adlı kahraman
ödülleri. 5★'tan sonra gelen her parça kendiliğinden Anı Kristali'ne döner; oyuncu hiçbir parçanın
boşa gitmediğini görür.

Bir Efsanevi kahramanı çağırıp 5★'a çıkarmak <!--v:veri.kahramanlar.nadirlikler.legendary.cagri_parca-->50<!--/v-->
kart + <!--v:veri.kahramanlar.yildizlar.4.toplam_parca-->150<!--/v--> parça ister. Kapsül kopyası
<!--v:veri.kahramanlar.nadirlikler.legendary.kopya_parca-->20<!--/v--> parça verdiğinden, yalnızca kapsülle
bu dokuz kopya (bir çağırma + sekiz kopya) demektir; kartlar ve Seçmeli kartlar yolu kısaltır.

### 5.2 Seviye tavanı

```
seviye tavanı = min(★ × 10, Çekirdek Reaktör seviyesi)
```

Yıldız kuralı bugün kodda var (✅). **Reaktör tavanı yenidir** (⬜): hiçbir bina reaktörü geçemediği
gibi hiçbir kahraman da geçemez. Bu kural iki şey sağlar: eğitim bedeli reaktör eğrisine güvenle
bağlanabilir (Sv L eğitimi her zaman o reaktörün ekonomisiyle ödenir) ve ödeme yapan oyuncu
kahramanını üssünden hızlı büyütemez. Reaktör ilk günlerde hızlı yükseldiği için (Reaktör 20,
F2P'de <!--v:hesap.sim.f2p.seviye.20|gun-->4,5. gün<!--/v-->) bu tavan yalnızca ilk haftada hissedilir.

### 5.3 Eğitim ve deneyim

Kahraman seviyesini **deneyim** doldurur. İki yolu vardır:

1. **Eğit** (bugünkü düğme, ✅ işlev / 🟡 bedel): seviyenin kalan deneyimi Gıda ve Biyokütle ile
   anında satın alınır. Bedel reaktör eğrisine bağlıdır:
   `bedel(L → L+1) = oran × nadirlik çarpanı × reaktör_bedel(L)`; oranlar Gıda
   <!--v:veri.kahramanlar.egitim.Food-->0,03<!--/v-->, Biyokütle <!--v:veri.kahramanlar.egitim.Biomass-->0,01<!--/v-->.
2. **Savaş deneyimi** (⬜): harita PvE zaferleri ve seferler kahraman deneyimi verir
   ([09](09_dunya_haritasi_ve_pvp.md) ödül tabloları). Deneyim filoda paylaşılır: Kaptan
   %<!--v:veri.kahramanlar.deneyim.kaptan_payi-->50<!--/v-->, her Subay %<!--v:veri.kahramanlar.deneyim.subay_payi-->25<!--/v-->.
   Bir deneyim puanı bir birim eğitim kaynağına eşittir; savaşan kahraman eğitim bedelinin bir
   kısmını sahada kazanmış olur.

<!-- URET:kahraman-egitim -->
| Seviye | Sıradan | Nadir | Destansı | Efsanevi |
| --- | --- | --- | --- | --- |
| 1 → 2 | 1 Biyokütle · 2 Gıda | 1 Biyokütle · 2 Gıda | 1 Biyokütle · 3 Gıda | 1 Biyokütle · 4 Gıda |
| 5 → 6 | 7 Biyokütle · 20 Gıda | 8 Biyokütle · 25 Gıda | 10 Biyokütle · 30 Gıda | 14 Biyokütle · 40 Gıda |
| 10 → 11 | 50 Biyokütle · 150 Gıda | 60 Biyokütle · 180 Gıda | 75 Biyokütle · 225 Gıda | 100 Biyokütle · 300 Gıda |
| 20 → 21 | 1.300 Biyokütle · 3.900 Gıda | 1.560 Biyokütle · 4.680 Gıda | 1.950 Biyokütle · 5.850 Gıda | 2.600 Biyokütle · 7.800 Gıda |
| 30 → 31 | 11.000 Biyokütle · 33.000 Gıda | 13.200 Biyokütle · 39.600 Gıda | 16.500 Biyokütle · 49.500 Gıda | 22.000 Biyokütle · 66.000 Gıda |
| 40 → 41 | 50.000 Biyokütle · 150.000 Gıda | 60.000 Biyokütle · 180.000 Gıda | 75.000 Biyokütle · 225.000 Gıda | 100.000 Biyokütle · 300.000 Gıda |
| 49 → 50 | 174.000 Biyokütle · 522.000 Gıda | 209.000 Biyokütle · 627.000 Gıda | 261.000 Biyokütle · 783.000 Gıda | 348.000 Biyokütle · 1.040.000 Gıda |
<!-- /URET:kahraman-egitim -->

Bugünkü kod eğitimde Titanyum ve Biyokütle ister ve seviye başına ×1,08 büyür; geç seviyelerde
reaktör ekonomisinin çok altında kalır. Hedef bedel, bir kahramanın bir seviyesini o reaktör
seviyesinin Titanyum bedelinin %4'ü kadar (Gıda + Biyokütle) tutar. Dört filo ve üç ekip
kahramanını bir seviye birden yükseltmek reaktörün bir seviyesinin yarısından biraz fazlasına mal
olur: hissedilir ama boğmaz. Gıda'nın ana tüketicisi yine ordudur
([15 · Ekonomi Akışı](15_ekonomi_akisi.md)).

---

## 6. Yetenekler

### 6.1 Türler ve açılış

<!-- URET:tablo:kahramanlar.yetenek_turleri -->
| Tür | Sıradan / Nadir | Destansı | Efsanevi | Subayken | Ne yapar |
| --- | --- | --- | --- | --- | --- |
| Aktif (öfke) | 1★ | 1★ | 1★ | Çalışır | Öfke dolunca tur başında kendiliğinden tetiklenir: ek hasar, iyileştirme, kalkan ya da zayıflatma. |
| Pasif | 2★ | 2★ | 2★ | Çalışır | Savaş boyunca sürekli etki; aynı türden pasifler toplanır. |
| Sefer / basınç | 4★ | 3★ | 3★ | Çalışır | Sefer dalışları, basınç, kurtarma, toplama ve PvE karşılaşmaları; ordu savaşında etkisizdir. |
| Komuta | — | — | 4★ | Çalışmaz | Yalnızca Kaptan'ken: filoya ya da ralli lideri / Savunma Kaptanı iken komutadaki bütün askerlere yayılan etki. |
| Uyanış | — | 5★ + Uyanış | 5★ + Uyanış | Çalışmaz | Aktif yeteneği dönüştüren ya da güçlendiren son yetenek; Uyanış töreniyle açılır. |
<!-- /URET:tablo:kahramanlar.yetenek_turleri -->

Böylece Sıradan ve Nadir kahramanların 3, Destansıların 4, Efsanevilerin 5 yeteneği vardır. Her
yeteneğin 5 seviyesi ve seviye seviye değerleri kahraman sayfasında yazar.

### 6.2 Öfke

Savaş [07 · Birlikler ve Savaş](07_birlikler_ve_savas.md)'teki gibi turlarla, aynı anda ve
rastgelelik olmadan çözülür. Her kahramanın (Kaptan ve iki Subay) kendi öfke çubuğu vardır. Savaş
<!--v:veri.kahramanlar.ofke.baslangic-->200<!--/v--> öfkeyle başlar; her tur başında
+<!--v:veri.kahramanlar.ofke.tur_basi-->150<!--/v-->, kayıp verilen her tur ayrıca
+<!--v:veri.kahramanlar.ofke.hasar_alinca-->50<!--/v--> öfke gelir. Çubuk
<!--v:veri.kahramanlar.ofke.tavan-->1.000<!--/v-->'de dolar ve aktif yetenek bir sonraki turun başında
kendiliğinden tetiklenir; oyuncu elle zamanlamaz.

Varsayılan ritimde aktif yetenek, kayıp verilen 10 turluk bir savaşta iki kez (5. ve 10. turun
başında), 15 turluk boss savaşında üç kez gelir. Hiç kayıp vermeyen taraf daha yavaş dolar: güçlü
olan yeteneğe daha az muhtaçtır, geride kalan daha sık vurur. Beceri ağacı, pasifler ve teçhizat
bu ritmi bir tur kadar öne çekebilir; üçüncü tetik ancak tam yatırımla gelir.

**Değerler nasıl okunur?** Yetenek hasarı ve iyileştirmesi, filonun o turki **normal hasarının
yüzdesiyle** yazılır (07'deki "katsayı"; <!--v:veri.kahramanlar.ofke.katsayi_birimi-->100<!--/v--> = bir
turluk normal hasar). "Normal hasarın yüzde 125 kadarı" o turun saldırısına bir buçuk tur daha
eklemek demektir. Normal hasar askerlerden gelir ve Komuta bonusunu zaten içerir. Kalkan ve
zayıflatma yetenekleri yüzde olarak yazılır ve o tur bonus toplamına girer; iyileştirmeyle
kurtarılan askerler hafif yaralı olarak döner (07).

**Denge hedefi.** Bir filoda üç kahraman, kayıplı 10 turluk savaşta en çok altı kez yetenek
tetikler. Tam yatırımlı bir Efsanevi üçlünün yeteneklerinin toplam payı, filonun savaş boyunca
verdiği hasarın üçte birini geçmez; Sıradan bir üçlünün payı onda biri civarındadır. Savaşı
askerler kazanır, kahraman farkı yaratır.

### 6.3 Yetenek seviyesi ve Anı Kristali

Yetenekler **Anı Kristali** ile yükselir. Anı Kristali tek bir evrensel eşyadır; bedel kahramanın
nadirliğiyle büyür. Her yetenek seviyesi bir kahraman seviyesi ister (tabloda); kahraman seviyesi
yıldızla sınırlı olduğundan yetenek gücü yıldıza dolaylı olarak bağlıdır: 5. seviye yetenek en az
4★ kahraman ister.

<!-- URET:tablo:kahramanlar.yetenek_bedeli -->
| Yetenek seviyesi | Gereken kahraman Sv | Sıradan | Nadir | Destansı | Efsanevi |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 → 2 | 10 | 5 | 10 | 15 | 20 |
| 2 → 3 | 20 | 10 | 20 | 30 | 45 |
| 3 → 4 | 30 | 20 | 35 | 55 | 80 |
| 4 → 5 | 40 | 35 | 55 | 90 | 135 |
| Yetenek başına toplam | — | 70 | 120 | 190 | 280 |
| Kahraman başına toplam (tüm yetenekler) | — | 210 | 360 | 760 | 1.400 |
<!-- /URET:tablo:kahramanlar.yetenek_bedeli -->

**Nereden gelir?**

<!-- URET:tablo:kahramanlar.ani_kristali.kaynaklar -->
| Kaynak | Miktar | F2P aylık tahmin | Not |
| --- | --- | ---: | --- |
| Kriyo-Kapsül | Her çekimde 1 | 65 | Ücretsiz çekimler dahil; onlu çekim 10 verir |
| Fazla parça (5★ sonrası) | Parça başına 1 / 2 / 3 / 5 | 20 | Sıradan / Nadir / Destansı / Efsanevi; kendiliğinden döner |
| Günlük görev sandığı (100 puan) | 2 | 60 | 15 · Ekonomi ile eklenir |
| Siren Sürüsü ve Fay Çatlağı I | Şansla 1–3 | 30 | 09 · Dünya Haritası ödül tablosu |
| Basınç Arenası haftalık sıralaması | 5–20 | 40 | Orta sıralar |
| Derinlik Kroniği aşamaları | Aşama başına 50–150 | 30 | Tek seferlik; yıl ortalaması |
| Etkinlikler (Lig, Sürü, Mahzen, nesil etkinliği) | Kademe ödülü | 60 | 13 · Etkinlikler |
| İttifak Dükkânı | Haftada en çok 20 | 0 | İttifak katkısıyla; F2P tahminine katılmadı |
| The Syndicate | Kristal kutuları, aylık alım sınırıyla | 0 | 12 · The Syndicate |
<!-- /URET:tablo:kahramanlar.ani_kristali.kaynaklar -->

F2P aylık toplam tahmini <!--v:veri.kahramanlar.ani_kristali.f2p_aylik-->305<!--/v--> Anı Kristali'dir: bir yılda
yaklaşık iki Efsanevi ve birkaç Destansı kahramanın bütün yetenekleri 5. seviyeye çıkar. Oyuncu
kristali nereye harcayacağını seçmek zorundadır; bu seçim kadro kimliğinin bir parçasıdır.

**Kristal iadesi yoktur**, ama kahraman değiştirme cezası da yoktur: bir kahramana harcanan kristal
onda kalır ve kahraman yeniden kullanılınca değerini korur.

---

## 7. Yetenek ağaçları ⬜

Yetenek ağacı yalnızca **Kaptan**'da çalışır. Her kahramanın üç ağacı vardır; ağaçlar
<!--v:veri.kahramanlar.uzmanliklar.0.ad-->Zırhlı<!--/v--> gibi 14 uzmanlıktan seçilir.

### 7.1 Puanlar

```
ağaç puanı = (Sv − 1) × seviye başına puan + ulaşılan yıldızların bonusları
```

Seviye başına <!--v:veri.kahramanlar.agac.puan.seviye_basi-->1<!--/v--> puan; yıldız bonusları 2★
+<!--v:veri.kahramanlar.agac.puan.yildiz_bonus.2-->2<!--/v-->, 3★ +<!--v:veri.kahramanlar.agac.puan.yildiz_bonus.3-->2<!--/v-->,
4★ +<!--v:veri.kahramanlar.agac.puan.yildiz_bonus.4-->3<!--/v-->, 5★ +<!--v:veri.kahramanlar.agac.puan.yildiz_bonus.5-->4<!--/v-->.
Bonus yıldız yükselir yükselmez gelir, böylece yıldız atlamak hemen hissedilir. Sv 50, 5★ kahraman
<!--v:veri.kahramanlar.agac.puan.toplam-->60<!--/v--> puana ulaşır. Bir ağacı tamamen
doldurmak <!--v:veri.kahramanlar.agac.puan.agac_toplam-->35<!--/v--> puan ister; yani hiçbir kahraman iki
ağacı birden bitiremez. Tasarımın kalbi bu eksikliktir: her Kaptan bir **dizilim** seçer.

### 7.2 Ağacın yapısı

Her ağaç 7 kademede 11 düğümdür: 8 sayı düğümü, 2 **anahtar** ve 1 **zirve**. Bir kademe, o
ağaçta yeterince puan harcanınca açılır.

<!-- URET:tablo:kahramanlar.agac.kademeler -->
| Kademe | Düğümler | Açılış (o ağaçta harcanmış puan) | Kademedeki puan |
| ---: | --- | ---: | ---: |
| 1 | 2 düğüm × 3 puan | 0 | 6 |
| 2 | 2 düğüm × 3 puan | 5 | 6 |
| 3 | Anahtar I × 1 | 10 | 1 |
| 4 | 2 düğüm × 5 puan | 11 | 10 |
| 5 | Anahtar II × 1 | 21 | 1 |
| 6 | 2 düğüm × 5 puan | 22 | 10 |
| 7 | Zirve × 1 | 34 | 1 |
<!-- /URET:tablo:kahramanlar.agac.kademeler -->

Anahtar düğümler ve zirve tek puanlık, kural değiştiren düğümlerdir; sayı düğümleri yüzde verir.
Zirve, ağaçtaki her şeyi isteyerek (<!--v:veri.kahramanlar.agac.kademeler.6.gereken-->34<!--/v--> puan)
uzmanlaşmayı ödüllendirir. Anahtarlar zorunlu değildir: kademe eşiği yalnızca puan sayar, oyuncu
bir anahtarı atlayıp sayı düğümlerine yüklenebilir.

### 7.3 Dizilimler

<!-- URET:tablo:kahramanlar.agac.insa_ornekleri -->
| Dizilim | Puan dağılımı | Kazandığı | Kime uygun |
| --- | --- | --- | --- |
| Uzman | 35 + 25 + 0 | Ana ağacın Zirvesi + ikinci ağacın Anahtar II'si | Tek işe odaklı Kaptan: ralli lideri, saf Zırhlı duvar, Leviathan avcısı |
| Çift anahtar | 22 + 22 + 16 | İki Anahtar II + üçüncü ağacın Anahtar I'i | Karma filolar, saha savaşı; Zirve yok ama altı anahtarın beşi |
| Üçlü denge | 20 + 20 + 20 | Üç Anahtar I ve geniş istatistik | Erken oyun ve sık rol değiştiren kahramanlar |
| Ekonomi | Toplama 35 + Akıntı 25 | Toplama Zirvesi (Hadalit İzi) + hızlı rota | Toplama filoları; savaşa girmez |
<!-- /URET:tablo:kahramanlar.agac.insa_ornekleri -->

**Sıfırlama.** İlk sıfırlama ücretsizdir; sonrası bir Kalibrasyon Çipi (görev ve etkinlik ödülü)
ya da <!--v:veri.kahramanlar.agac.sifirlama.hadalit-->300<!--/v--> Hadalit ister. Her kahraman
<!--v:veri.kahramanlar.agac.sifirlama.kayitli_dizilim-->2<!--/v--> dizilim kaydeder ve aralarında bedelsiz
geçer (ör. "saha" ve "garnizon"); geçiş yalnızca filo üsteyken yapılır.

### 7.4 Uzmanlıklar

<!-- URET:tablo:kahramanlar.uzmanliklar -->
| Uzmanlık | Odak | Çoğunlukla | Açıklama |
| --- | --- | --- | --- |
| Zırhlı | Zırhlı Dalgıç savunması ve dayanıklılığı | Komutan, Hekim | Ön saftaki duvarı kalınlaştırır, Ark Askerlerine karşı üstünlüğü büyütür. |
| Zıpkın | Zıpkıncı saldırısı, açılış darbesi | Avcı, Pilot | Uzaktan vuran Zıpkıncıların ağacı: ilk turlarda ağır darbe, Zırhlı Dalgıçlara karşı üstünlük. |
| Ark | Ark Askeri hasarı ve yetenek gücü | Mühendis, Bilim insanı | Suda kararlı elektrik arkı kuran birliklerin ağacı: yetenek hasarı ve Zıpkıncılara karşı üstünlük. |
| Liderlik | Birlik kapasitesi, karma ordu, ralli | Komutan | Büyük ve karışık filoları yöneten kaptanların ağacı: daha çok asker, üç türün birlikte savaşması, ralli liderliği. |
| Saldırı | Türden bağımsız hasar, açılış ve bitiriş | Avcı, Pilot, Komutan | Savaşı erken bitirmek isteyen kaptanların ağacı. |
| Savunma | Hasar azaltma ve dayanma | Komutan, Hekim, Mühendis | Uzun savaşları kazanmak için DEF, HP ve hasar azaltma. |
| Akıntı | Yürüyüş hızı, geri dönüş, Sonar Enerjisi | Pilot, Avcı | Haritada hız ve esneklik: filo daha çabuk varır, daha az Sonar Enerjisiyle avlanır, kayıpsız çekilir. |
| Destek | İyileştirme, yaralı oranı, müttefik desteği | Hekim, Bilim insanı | Kaybı azaltan ve müttefiki güçlendiren ağaç; garnizonda ve rallilerde parlar. |
| Beceri | Öfke ve aktif yetenek | Bilim insanı, Mühendis | Yeteneğini sık ve sert kullanan kaptanların ağacı. |
| Garnizon | Üs ve ittifak yapısı savunması | Mühendis, Komutan, Hekim | Kubbeni ve ittifak yapılarını savunan kaptanların ağacı; yalnızca savunmada çalışır. |
| Toplama | Toplama hızı, yük, malzeme | Pilot, Mühendis | Harita yataklarından kaynak ve teçhizat malzemesi toplayan filolar için; savaş gücü vermez. |
| Avcılık | Canavarlar ve harita PvE | Avcı | Uçurum Sürüngenleri, Siren Sürüsü, Demir Leviathan, Kadim Muhafızlar ve Hadal Titanı'na karşı. Sonarla taranan canlılar hiçbir zaman hedef olmaz; bu ağaç onlara dokunmaz. |
| Fetih | Üs ve yapı saldırısı, Delici, yağma | Komutan, Mühendis | Düşman kubbelerine ve ittifak yapılarına saldıran kaptanların ağacı; Delici kuşatma birliğini güçlendirir. |
| Derinlik | Sefer, basınç ve iç kuşaklar | Bilim insanı, Pilot, Mühendis | Hadal'a özgü ağaç: seferde basınca dayanmak, sonar-yalnız bölgelerde savaşmak ve haritanın derin iç kuşaklarında üstünlük. |
<!-- /URET:tablo:kahramanlar.uzmanliklar -->

Ağaç kataloğu sabit değildir: yeni nesiller yeni uzmanlık getirebilir, ama bir kahramanın üç ağacı
çıkıştan sonra değişmez.

### 7.5 Düğüm listeleri

Sayı düğümlerinde "Etki" puan başınadır (Maks ile çarpılır); anahtar ve zirvede toplam etkidir.

<details><summary><b>Zırhlı</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.zirhli.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Kalın Levha | 3 | Zırhlı Dalgıç DEF +%1 |
| 1 | Basınç Contası | 3 | Zırhlı Dalgıç HP +%1 |
| 2 | Kenet Düzeni | 3 | Zırhlı Dalgıç ATK +%1 |
| 2 | Ağır Adım | 3 | Arka saftan alınan hasar −%1 |
| 3 | Duvar Gibi | 1 | Anahtar: birliklerin yarısından fazlası Zırhlı Dalgıç ise normal saldırıdan alınan hasar −%3 |
| 4 | Titanyum Omuz | 5 | Zırhlı Dalgıç DEF +%1,5 |
| 4 | Dalgıç Ciğeri | 5 | Zırhlı Dalgıç HP +%1,5 |
| 5 | Karşı Basınç | 1 | Anahtar: Zırhlı Dalgıçların aldığı hasarın %5'i saldıran yığına geri yansır |
| 6 | Sabır | 5 | Hasar alınca kazanılan öfke +%2 |
| 6 | Çelik Sıra | 5 | Ark Askerlerine verilen hasar +%1 |
| 7 | Abis Kalesi | 1 | Zirve: filo ilk kez yarı canın altına inince 3 tur alınan tüm hasar −%10 (savaş başına bir kez) |
<!-- /URET:tablo:kahramanlar.uzmanliklar.zirhli.dugumler -->

</details>

<details><summary><b>Zıpkın</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.zipkin.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Keskin Uç | 3 | Zıpkıncı ATK +%1 |
| 1 | Hafif Donanım | 3 | Zıpkıncı HP +%1 |
| 2 | Gergin Halat | 3 | Zıpkıncı DEF +%1 |
| 2 | Hızlı Nişan | 3 | Normal saldırı hasarı +%1 |
| 3 | Kanca ve Çek | 1 | Anahtar: savaşın ilk 3 turunda verilen hasar +%5 |
| 4 | Delici Uç | 5 | Zıpkıncı ATK +%1,5 |
| 4 | Akıntıya Karşı | 5 | Zıpkıncı HP +%1,5 |
| 5 | Zincirleme Atış | 1 | Anahtar: Zıpkıncıların ön safa verdiği hasarın %10'u arka safa da sıçrar |
| 6 | Avcı Gözü | 5 | Zırhlı Dalgıçlara verilen hasar +%1 |
| 6 | Hareketli Hedef | 5 | Yürüyüş hızı +%1 |
| 7 | Derin Zıpkın | 1 | Zirve: her 4. turda Zıpkıncılar hedef DEF'inin %20'sini yok sayar |
<!-- /URET:tablo:kahramanlar.uzmanliklar.zipkin.dugumler -->

</details>

<details><summary><b>Ark</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.ark.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Bakır Bobin | 3 | Ark Askeri ATK +%1 |
| 1 | Yalıtım | 3 | Ark Askeri HP +%1 |
| 2 | Kapasitör | 3 | Ark Askeri DEF +%1 |
| 2 | Statik Yük | 3 | Öfke kazanımı +%1 |
| 3 | Deşarj | 1 | Anahtar: birliklerin yarısından fazlası Ark Askeri ise aktif yetenek hasarı +%5 |
| 4 | Yüksek Gerilim | 5 | Ark Askeri ATK +%1,5 |
| 4 | Faraday Kafesi | 5 | Ark Askeri HP +%1,5 |
| 5 | Zincir Ark | 1 | Anahtar: aktif yetenek hasarının %30'u hedefin yanındaki bir yığına da sıçrar |
| 6 | Toprak Hattı | 5 | Zıpkıncılara verilen hasar +%1 |
| 6 | Aşırı Yük | 5 | Aktif yetenek hasarı +%1 |
| 7 | Fırtına Çekirdeği | 1 | Zirve: aktif yetenek kullanılınca 2 tur Ark Askeri ATK +%10 |
<!-- /URET:tablo:kahramanlar.uzmanliklar.ark.dugumler -->

</details>

<details><summary><b>Liderlik</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.liderlik.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Komuta Zinciri | 3 | Birlik kapasitesi +%1 |
| 1 | Moral | 3 | Tüm birlik ATK +%0,5 |
| 2 | Disiplin | 3 | Tüm birlik DEF +%0,5 |
| 2 | Kriz Yönetimi | 3 | Tüm birlik HP +%0,5 |
| 3 | Karma Düzen | 1 | Anahtar: filoda Zırhlı, Zıpkıncı ve Ark birlikte varsa tüm hasar +%3 |
| 4 | Kalabalık Komuta | 5 | Birlik kapasitesi +%1 |
| 4 | Sağlam Hat | 5 | Alınan tüm hasar −%0,5 |
| 5 | Ralli Sancağı | 1 | Anahtar: ralli lideriyken ralli kapasitesi +%5 |
| 6 | Sıkı Saf | 5 | Tüm birlik ATK +%1 |
| 6 | Yedek Kuvvet | 5 | Ölecek askerlerin +%1'i yaralı olur |
| 7 | Amiral Gemisi | 1 | Zirve: savaşın ilk 2 turunda Subayların öfke kazanımı +%25 |
<!-- /URET:tablo:kahramanlar.uzmanliklar.liderlik.dugumler -->

</details>

<details><summary><b>Saldırı</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.saldiri.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Hücum | 3 | Tüm birlik ATK +%1 |
| 1 | Ön Hat | 3 | Normal saldırı hasarı +%1 |
| 2 | Hedef Seçimi | 3 | Arka safa verilen hasar +%1 |
| 2 | Kesik | 3 | Öfke kazanımı +%1 |
| 3 | İlk Darbe | 1 | Anahtar: savaşın ilk turunda verilen hasar +%10 |
| 4 | Keskin Kenar | 5 | Tüm birlik ATK +%1 |
| 4 | Yarma | 5 | Hedef DEF'inin %1'i yok sayılır |
| 5 | Kırık Zırh | 1 | Anahtar: canı yarının altındaki birliklere verilen hasar +%5 |
| 6 | Ardı Ardına | 5 | Normal saldırı hasarı +%1 |
| 6 | Gözü Kara | 5 | Aktif yetenek hasarı +%1 |
| 7 | Kırılma Noktası | 1 | Zirve: düşman filosunun canı %25'in altına inince tüm hasar +%10 |
<!-- /URET:tablo:kahramanlar.uzmanliklar.saldiri.dugumler -->

</details>

<details><summary><b>Savunma</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.savunma.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Ek Perçin | 3 | Tüm birlik DEF +%1 |
| 1 | Yedek Hava | 3 | Tüm birlik HP +%1 |
| 2 | Kalkan Duvarı | 3 | Normal saldırıdan alınan hasar −%1 |
| 2 | Soğukkanlılık | 3 | Aktif yetenekten alınan hasar −%1 |
| 3 | Sığınak | 1 | Anahtar: saldırıya uğrayan taraf olunca ilk 3 tur alınan hasar −%5 |
| 4 | Kat Kat Zırh | 5 | Tüm birlik DEF +%1 |
| 4 | Dayanıklılık | 5 | Tüm birlik HP +%1 |
| 5 | Basınç Dengesi | 1 | Anahtar: her 5 turda bir, 1 tur boyunca alınan hasar −%15 |
| 6 | İkinci Nefes | 5 | Her tur filoya küçük iyileşme (normal hasarın %2'si kadar HP) |
| 6 | Siper | 5 | Ön saftan alınan hasar −%1 |
| 7 | Kırılmaz | 1 | Zirve: filo ilk kez canının %30'unun altına inince Kaptan HP'sinin 3 katı hasar emen kalkan |
<!-- /URET:tablo:kahramanlar.uzmanliklar.savunma.dugumler -->

</details>

<details><summary><b>Akıntı</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.akinti.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Hidrodinamik | 3 | Yürüyüş hızı +%1 |
| 1 | Hızlı Dönüş | 3 | Üsse dönüş hızı +%2 |
| 2 | Sonar Okuma | 3 | Harita PvE saldırılarında Sonar Enerjisi tüketimi −%1 |
| 2 | Kaçış Rotası | 3 | Geri çekilme hızı +%2 |
| 3 | Akıntıya Bin | 1 | Anahtar: savaştan sonra üsse dönüş hızı +%20 |
| 4 | Türbülans | 5 | Yürüyüş hızı +%1,5 |
| 4 | Hafif Gövde | 5 | Birliklerin yarısından fazlası Zıpkıncı ise tüm birlik ATK +%1 |
| 5 | Pusu Akıntısı | 1 | Anahtar: yürüyüşteyken saldırıya uğrayınca alınan hasar −%5 |
| 6 | Rota Ustası | 5 | İttifak bölgesinde yürüyüş hızı +%2 |
| 6 | Akıntı Haritası | 5 | Tarafsız bölgede yürüyüş hızı +%1 |
| 7 | Girdap | 1 | Zirve: geri çekilme komutuyla savaştan çıkınca kayıplar −%20 |
<!-- /URET:tablo:kahramanlar.uzmanliklar.akinti.dugumler -->

</details>

<details><summary><b>Destek</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.destek.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | İlk Yardım | 3 | İyileştirme etkisi +%1 |
| 1 | Moral Desteği | 3 | Tüm birlik HP +%0,5 |
| 2 | Triyaj | 3 | Ölecek askerlerin +%1'i yaralı olur |
| 2 | Takviye Hattı | 3 | Takviye olarak gönderilen birliklerin DEF'i +%1 |
| 3 | Saha Hekimi | 1 | Anahtar: aktif yetenek kullanılınca filoya iyileşme (normal hasarın %15'i kadar HP) |
| 4 | Kriyo Bandaj | 5 | İyileştirme etkisi +%1,5 |
| 4 | Paylaşılan Kalkan | 5 | Kalkan etkisi +%1,5 |
| 5 | Dayanışma | 1 | Anahtar: ittifak bölgesindeyken tüm birlik DEF +%3 |
| 6 | Hızlı Taburcu | 5 | Bu filonun yaralılarının Revir'de iyileşme süresi −%2 |
| 6 | Ortak Nefes | 5 | Takviye ve rallide tüm birlik ATK +%1 |
| 7 | Hayat Hattı | 1 | Zirve: savaşta ölecek askerlerin %5'i daha yaralı olarak kurtulur |
<!-- /URET:tablo:kahramanlar.uzmanliklar.destek.dugumler -->

</details>

<details><summary><b>Beceri</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.beceri.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Odak | 3 | Öfke kazanımı +%1 |
| 1 | Nöral Bağ | 3 | Aktif yetenek hasarı +%1 |
| 2 | Ritim | 3 | Savaş başında +30 öfke |
| 2 | Rezonans | 3 | Pasif yetenek etkileri +%1 |
| 3 | Kıvılcım | 1 | Anahtar: aktif yetenek kullanılınca 1 tur tüm birlik ATK +%10 |
| 4 | Hadalit Bilinci | 5 | Aktif yetenek hasarı +%1,5 |
| 4 | Sabırsız Zihin | 5 | Öfke kazanımı +%1,5 |
| 5 | Yankı | 1 | Anahtar: aktif yetenek bir sonraki turun başında %30 güçle bir kez daha etki eder |
| 6 | Taşkın Öfke | 5 | Aktif yetenekten sonra harcanan öfkenin %2'si geri gelir |
| 6 | Zayıf Nokta | 5 | Aktif yetenek hedef DEF'inin %1'ini yok sayar |
| 7 | Uyanmış Zihin | 1 | Zirve: aktif yetenek için gereken öfke −%15 |
<!-- /URET:tablo:kahramanlar.uzmanliklar.beceri.dugumler -->

</details>

<details><summary><b>Garnizon</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.garnizon.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Kubbe Muhafızı | 3 | Üs savunmasında tüm birlik DEF +%1 |
| 1 | Basınç Kapısı | 3 | Üs savunmasında tüm birlik HP +%1 |
| 2 | Sur Topçusu | 3 | Üs savunma kulelerinin hasarı +%1 |
| 2 | Tanıdık Sular | 3 | Üs savunmasında tüm birlik ATK +%1 |
| 3 | Son Hat | 1 | Anahtar: Kalkan Jeneratörü çalışırken üs savunmasında alınan hasar −%5 |
| 4 | Kale Garnizonu | 5 | İttifak yapısı savunmasında DEF ve HP +%1 |
| 4 | Takviye Kabulü | 5 | Üsse kabul edilen ittifak takviyesi kapasitesi +%2 |
| 5 | Kuşatma Kırıcı | 1 | Anahtar: Delicilere verilen hasar +%10 |
| 6 | Yaralı Kurtarma | 5 | Üs savunmasında ölecek askerlerin +%1'i yaralı olur |
| 6 | Kat Kat Sur | 5 | Üs savunmasında alınan hasar −%1 |
| 7 | Yıkılmaz Kubbe | 1 | Zirve: üs savunması başında Kaptan DEF'inin 5 katı hasar emen kalkan |
<!-- /URET:tablo:kahramanlar.uzmanliklar.garnizon.dugumler -->

</details>

<details><summary><b>Toplama</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.toplama.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Hasat Rotası | 3 | Toplama hızı +%2 |
| 1 | Geniş Ambar | 3 | Denizaltı yük kapasitesi +%2 |
| 2 | Kısa Yol | 3 | Yataklara yürüyüş hızı +%3 |
| 2 | Titanyum Damarı | 3 | Titanyum toplama hızı +%3 |
| 3 | Eşsiz Tarama | 1 | Anahtar: toplamada teçhizat malzemesi bulma şansı +%10 |
| 4 | Ağır Yük | 5 | Denizaltı yük kapasitesi +%2 |
| 4 | Usta Hasatçı | 5 | Toplama hızı +%2 |
| 5 | Kesintisiz Hasat | 1 | Anahtar: toplama yapan filo saldırıya uğrayınca yükün %30'u korunur |
| 6 | Nodül Gözü | 5 | Süperalaşım toplama hızı +%2 |
| 6 | Derin Yatak | 5 | Sv 5 ve üstü yataklarda toplama hızı +%2 |
| 7 | Hadalit İzi | 1 | Zirve: her tam toplama %5 şansla Hadalit verir (harita günlük sınırına sayılır) |
<!-- /URET:tablo:kahramanlar.uzmanliklar.toplama.dugumler -->

</details>

<details><summary><b>Avcılık</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.avcilik.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | İz Sürme | 3 | Canavarlara verilen hasar +%1 |
| 1 | Tuzak | 3 | Canavarlardan alınan hasar −%1 |
| 2 | Sonar Avı | 3 | Canavar saldırısında Sonar Enerjisi tüketimi −%2 |
| 2 | Ganimet | 3 | Canavar ödülleri +%2 |
| 3 | Sürü Bozucu | 1 | Anahtar: Siren Sürüsü ve Uçurum Sürüngenleri'ne karşı ilk 3 turda hasar +%10 |
| 4 | Leviathan Avcısı | 5 | Demir Leviathan'a verilen hasar +%1,5 |
| 4 | Kadim Bilgisi | 5 | Kadim Muhafızlar'a verilen hasar +%1,5 |
| 5 | Zincirleme Av | 1 | Anahtar: canavar yenilince harcanan Sonar Enerjisinin %25'i geri gelir |
| 6 | Av Disiplini | 5 | Canavar savaşında kayıplar −%2 |
| 6 | Usta Avcı | 5 | Canavar savaşında tüm birlik ATK +%1 |
| 7 | Titan Kokusu | 1 | Zirve: Leviathan rallilerinde ve Hadal Titanı'na karşı tüm hasar +%8 |
<!-- /URET:tablo:kahramanlar.uzmanliklar.avcilik.dugumler -->

</details>

<details><summary><b>Fetih</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.fetih.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Kuşatma Planı | 3 | Delici ATK +%1 |
| 1 | Yarma Hattı | 3 | Üs ve yapı saldırısında tüm birlik ATK +%1 |
| 2 | Basınç Kırıcı | 3 | Yapılara verilen hasar +%1 |
| 2 | Hızlı Yağma | 3 | Yağma kapasitesi +%2 |
| 3 | Kapı Kırıcı | 1 | Anahtar: saldırıda düşman savunma kulelerinin hasarı −%10 |
| 4 | Delici Ustalığı | 5 | Delici HP +%1,5 |
| 4 | Fetih Sancağı | 5 | Saldırıda tüm birlik ATK +%1 |
| 5 | Duvar Yıkıcı | 1 | Anahtar: üs saldırısında savunan garnizonun DEF'i −%5 |
| 6 | Yağmacı | 5 | Yağma kapasitesi +%2 |
| 6 | Tahkimat Yıkımı | 5 | İttifak yapılarına ve geçitlere verilen hasar +%2 |
| 7 | Kuşatma Ustası | 1 | Zirve: yapılara verilen hasar +%15 |
<!-- /URET:tablo:kahramanlar.uzmanliklar.fetih.dugumler -->

</details>

<details><summary><b>Derinlik</b></summary>

<!-- URET:tablo:kahramanlar.uzmanliklar.derinlik.dugumler -->
| Kademe | Düğüm | Maks | Etki (puan başına; anahtar ve zirvede toplam) |
| ---: | --- | ---: | --- |
| 1 | Basınç Toleransı | 3 | Seferde basınç hasarı −%2 |
| 1 | Sonar Haritacı | 3 | Sefer keşif hızı +%2 |
| 2 | Gövde Bilgisi | 3 | Seferde denizaltı gövde dayanımı +%2 |
| 2 | Karanlık Gözü | 3 | Sonar-yalnız bölgelerde tüm birlik ATK +%1 |
| 3 | Basınç Kodu | 1 | Anahtar: sefer ödülleri +%5 |
| 4 | Kriyo Kabin | 5 | Seferde kahraman takımının HP'si +%2 |
| 4 | Akıntı Uzmanı | 5 | Sefer olaylarında başarı şansı +%1 |
| 5 | Hadal Uyumu | 1 | Anahtar: Çukur Yamacı ve daha derin kuşaklarda tüm birlik DEF +%3 |
| 6 | Kalıntı Avcısı | 5 | Seferde kalıntı bulma şansı +%2 |
| 6 | Derin Nefes | 5 | Derin Yamaç ve daha derin kuşaklarda yürüyüş hızı +%1 |
| 7 | Çukurun Çocuğu | 1 | Zirve: Çukur Ekseni ile Dip ve Çekirdek kuşaklarında tüm hasar +%8 |
<!-- /URET:tablo:kahramanlar.uzmanliklar.derinlik.dugumler -->

</details>

**Ağaç ve bağlam.** Bir düğüm yalnızca kendi bağlamında çalışır: Garnizon yalnızca üs ve ittifak
yapısı savunmasında, Avcılık yalnızca Hadal'ın canavarlarına karşı, Derinlik yalnızca seferde ve
adı geçen kuşaklarda, Toplama yalnızca toplama yürüyüşünde. Arayüz etkisiz düğümleri o an soluk
gösterir; puan geri alınmaz ama oyuncu neden çalışmadığını görür.

---

## 8. Uyanış ⬜

Uyanış, bir Destansı ya da Efsanevi kahramanın son adımıdır.

- **Koşul:** <!--v:veri.kahramanlar.uyanis.kosul-->5★, Sv 50 ve diğer tüm yetenekler 5. seviye<!--/v-->.
- **Tören:** Kriyo Salonu'nda yapılır; kahraman kısa bir sahnede kapsülünden yeniden çıkar ve
  portresine kalıcı bir biyolüminesan iz düşer.
- **Kazanç:** Uyanış yeteneği 1. seviyeden başlar ve Anı Kristali ile 5'e çıkar (yetenek bedeli
  tablosundaki "Efsanevi" / "Destansı" sütunu aynen geçerlidir).

<!-- URET:tablo:kahramanlar.uyanis.bedel -->
| Nadirlik | Anı Kristali | Uyanış Özü | Kazanç |
| --- | ---: | ---: | --- |
| Destansı | 100 | 1 | Uyanış yeteneği Sv 1 · portre ve modelde biyolüminesan iz · profilde 'Uyanmış' rozeti |
| Efsanevi | 200 | 2 | Uyanış yeteneği Sv 1 · portre ve modelde biyolüminesan iz · profilde 'Uyanmış' rozeti |
<!-- /URET:tablo:kahramanlar.uyanis.bedel -->

**Uyanış Özü** satılmaz; oynayarak kazanılır:

<!-- URET:tablo:kahramanlar.uyanis.oz_kaynaklari -->
| Kaynak | Miktar | Not |
| --- | --- | --- |
| Derinlik Kroniği | Aşama 6, 8 ve 10'da 1'er | Sunucu ilerledikçe herkes alır |
| Demir Leviathan Yuvası Sv 5–6 | Ralli katılımcısına %5 şans | Günlük Leviathan ödül hakkıyla sınırlı |
| Basınç Arenası sezonu | İlk %10'a 1 | Sezon sonu |
| Akıntı Savaşı dükkânı | Sezonda en çok 1 | Sezon puanıyla |
| Nesil etkinliği | Etkinlik zirvesinde 1 | Her yeni nesilde |
<!-- /URET:tablo:kahramanlar.uyanis.oz_kaynaklari -->

Kronik tek başına F2P'ye yılda üç öz verir: bir Efsanevi ve bir Destansı Uyanış. Ödeme yapan oyuncu
Uyanış'a daha çok kahramanı 5★'a daha erken taşıdığı için daha önce varır, ama özü para ile
almaz. Uyanış bir başarıdır ve profilde öyle görünür.

---

## 9. Sınıf yatkınlığı, karşı üçgen ve anıtlar

Her sınıf bir birlik türüne yatkındır. Filodaki askerlerin en az
%<!--v:veri.kahramanlar.sinerji.yatkinlik_esik-->50<!--/v-->'si Kaptan'ın yatkın olduğu türdense o türün
ATK, DEF ve HP'si +<!--v:veri.kahramanlar.sinerji.yatkinlik_bonus|yuzde-->%5<!--/v--> artar.

<!-- URET:tablo:kahramanlar.birlik_turleri -->
| Birlik | Üstün olduğu | Zayıf olduğu | Yatkın sınıflar | Anıt | Rol |
| --- | --- | --- | --- | --- | --- |
| Zırhlı Dalgıç | Ark Askeri | Zıpkıncı | Komutan, Hekim | Dalgıç Anıtı | Ağır basınç kıyafetli ön saf; yüksek DEF ve HP. |
| Zıpkıncı | Zırhlı Dalgıç | Ark Askeri | Avcı, Pilot | Zıpkın Anıtı | Uzaktan vuran hafif birlik; yüksek ATK ve hız. |
| Ark Askeri | Zıpkıncı | Zırhlı Dalgıç | Mühendis, Bilim insanı | Ark Anıtı | Suda elektrik arkı kuran destek birliği; yetenek ve alan hasarı. |
| Delici | — | — | Hiçbiri (Fetih ağacı) | — | Kuşatma birliği; yapılara ve yataklara karşı. Karşı üçgenin dışındadır. |
<!-- /URET:tablo:kahramanlar.birlik_turleri -->

Karşı üçgen: Zırhlı > Ark > Zıpkıncı > Zırhlı; üstün taraf ×<!--v:veri.birlikler.karsi.avantaj-->1,2<!--/v-->
hasar verir, zayıf taraf ×<!--v:veri.birlikler.karsi.dezavantaj-->0,85<!--/v--> ile vurur. Üç anıt
([Dalgıç Anıtı](binalar/armor_monument.md), [Zıpkın Anıtı](binalar/harpoon_monument.md),
[Ark Anıtı](binalar/arc_monument.md)) kendi türünün üstünlüğünü büyütür; anıtın seviye etkisi ve
asker tarafı [07](07_birlikler_ve_savas.md)'dedir. Kahraman kadrosu üç türe eşit bölünmüştür:
her türe 10 kahraman, her nadirlikte üç türün de temsilcisi vardır.

---

## 10. Fraksiyonlar ve sinerji

Beş fraksiyon hem kahraman grubudur hem de oyuncunun kökenidir (K-19,
[16 · Kronik, Köken, Profil](16_kronik_kokenler_profil.md)). Her fraksiyonda altı kahraman vardır.

<!-- URET:tablo:kahramanlar.fraksiyonlar -->
| Fraksiyon | Değerleri | Ekip rezonansı (3 aynı fraksiyon) | Kahramanlar |
| --- | --- | --- | --- |
| Atlantik Birliği | Düzen, inşa, sorumluluk | İnşa hızı +%5 | Elias Rourke, Tess Ruiz, Amara Okafor, Deniz Karaca, Kwame Boateng, Saoirse Kavanagh |
| Pasifik Tarikatı | Bilgi, şifa, sabır | Araştırma hızı +%5 | Dr. Lyra Moss, Dr. Orin Hale, Dr. Emi Sakamoto, Dr. Priya Raman, Dr. Han Seo-yeon, Dr. Isagani Dela Cruz |
| Derindoğanlar | Uyum, sezgi, topluluk | Gıda ve Biyokütle üretimi +%5 | Kael, Noa, Ilo, Aelis, Kiri, Sela |
| Unutulanlar | Dayanıklılık, beceriklilik, sadakat | Kurtarma ve toplama ödülü +%5 | Vera Kostina, Rafael Quispe, Juno Vega, Wira Santoso, Ufuk, Meridyen |
| Uçurum Tarikatı | Adanmışlık, cesaret, bedel | Hadalit bulma şansı +%5 | Darius Farahani, Sable, Mattias Gröndahl, Odalys Ferreira, Ilian Varga, Sura |
<!-- /URET:tablo:kahramanlar.fraksiyonlar -->

**Atlantik Birliği.** <!--v:veri.kahramanlar.fraksiyonlar.AtlanticUnion.aciklama-->Kuzey ve Güney Atlantik ile Akdeniz kıyı devletlerinin ortak deniz kurumu. Disiplinli, mühendislik ve lojistik odaklı; Porto Riko ve Güney Sandwich çukurlarındaki ilk basınç akademilerini kurdu. Çöküş gecesi tahliyeyi yöneten de, son konvoyu Hadal Eşiği'ne indiren de onlardı.<!--/v-->

**Pasifik Tarikatı.** <!--v:veri.kahramanlar.fraksiyonlar.PacificOrder.aciklama-->Pasifik çemberindeki araştırma istasyonlarının hekim ve bilim insanlarından doğan yemini olan düzen. Mariana, Izu-Ogasawara ve Filipin çukurlarında revir ve laboratuvar ağı işletir. Bilgiyi silahtan üstün tutar, ama bilginin bedelini de bilir.<!--/v-->

**Derindoğanlar.** <!--v:veri.kahramanlar.fraksiyonlar.Deepborn.aciklama-->Hadal habitatlarında doğup büyüyen kuşak. Gün ışığı görmemiş, basınca ve karanlığa uyum sağlamış; sonar tıklamalarıyla yön bulur, kubbe dışını yüzeyden gelenlerden iyi tanır. Çukurun canlılarına dokunmamak onların en eski kuralıdır.<!--/v-->

**Unutulanlar.** <!--v:veri.kahramanlar.fraksiyonlar.ForgottenOnes.aciklama-->Çöküşte boşaltılan istasyonlarda geride bırakılanlar ve onlara sonradan katılanlar. Hurdadan gövde, enkazdan reaktör kurar; 'her şey yeniden kullanılır' sözleri bir yaşam biçimidir. Aralarında insan olmayanlar da vardır.<!--/v--> Adları düşman Kadim Muhafızlar ile karışmaz (K-11).

**Uçurum Tarikatı.** <!--v:veri.kahramanlar.fraksiyonlar.AbyssCult.aciklama-->Hadalit'i ve Demir Leviathan'ları derinin iradesinin işaretleri sayan gizemli topluluk. Kristalle deney yapmaktan, bedeni basınca ve kristale açmaktan çekinmez. Tehlikeli ama sonuç alır: üsleri en az tüketen ve en çok bulanlardır.<!--/v-->

**Kurallar.**

- **Filo sinerjisi:** Kaptan ve iki Subay aynı fraksiyondansa filonun bütün birliklerinin ATK, DEF
  ve HP'si +<!--v:veri.kahramanlar.sinerji.fraksiyon_bonus|yuzde-->%5<!--/v--> artar. Üç yuvanın da dolu
  olması gerekir.
- **Ekip rezonansı:** Komuta Ekibi'nin üç kahramanı aynı fraksiyondansa tablodaki ekonomi bonusu
  açılır (+<!--v:veri.kahramanlar.sinerji.ekip_bonus|yuzde-->%5<!--/v-->).
- **Köken kahramanı (öneri):** Oyuncunun seçtiği köken, o fraksiyonun Nadir kahramanını başlangıçta
  verir: Atlantik Birliği → Kaptan Amara Okafor, Pasifik Tarikatı → Dr. Orin Hale, Derindoğanlar →
  Kael, Unutulanlar → Rafael Quispe, Uçurum Tarikatı → Darius Farahani. 16'nın eşitleme kuralıyla
  hepsi 2★ gelir; böylece beş köken aynı nadirlikte, aynı yıldızda başlar. Komutan Elias Rourke
  herkesin ortak başlangıç kahramanıdır (K-05); Dr. Lyra Moss ve Tess Ruiz hikâyenin ilk iki
  bölümünde herkese gelir. Köken kahramanı zaten varsa kart yerine yıldız parçası gelir (16).
  16'nın bugünkü tablosu farklı kahramanlar veriyor; karar iki bölümün ortak kararıdır (Açık sorular).
- Kökenin kendi pasifleri ve birliği [16](16_kronik_kokenler_profil.md)'dadır; kahraman sinerjisi
  köken seçiminden bağımsızdır.

---

## 11. Kriyo-Kapsül

Eski seferlerden kurtarılan kriyo-kapsüllerde hâlâ uyuyanlar var. Kapsül açılışı bir sonar
darbesiyle başlar, buz çözülür, siluet belirir; nadirlik ışığın rengiyle önce sezdirilir, sonra
gösterilir. Atla düğmesi her zaman vardır.

### 11.1 Oranlar ve garantiler

- Oranlar: Sıradan %<!--v:veri.kahramanlar.nadirlikler.common.agirlik-->65<!--/v-->, Nadir
  %<!--v:veri.kahramanlar.nadirlikler.rare.agirlik-->25<!--/v-->, Destansı
  %<!--v:veri.kahramanlar.nadirlikler.epic.agirlik-->8<!--/v-->, Efsanevi
  %<!--v:veri.kahramanlar.nadirlikler.legendary.agirlik-->2<!--/v--> (✅).
- **Destansı+ garantisi:** <!--v:veri.kahramanlar.kriyo.destansi_garanti-->30<!--/v-->. çekimde Destansı ya
  da üstü (✅). Sayaç Destansı ya da Efsanevi gelince sıfırlanır.
- **Efsanevi garantisi:** <!--v:veri.kahramanlar.kriyo.efsanevi_garanti-->80<!--/v-->. çekimde Efsanevi (⬜,
  K-04). Ayrı bir sayaçtır; yalnızca Efsanevi gelince sıfırlanır. Garantiler birlikte, beklenen
  değeri Efsanevi başına <!--v:veri.kahramanlar.kriyo.beklenen_efsanevi-->40<!--/v--> çekime, Destansı+
  başına <!--v:veri.kahramanlar.kriyo.beklenen_destansi_ustu-->9,6<!--/v--> çekime indirir.
- **Onlu çekim:** <!--v:veri.kahramanlar.kriyo.onlu_garanti-->En az 1 Nadir ya da üstü<!--/v--> (⬜). Onlu
  çekim, tek tek on çekimden ucuzdur ve aynı garanti sayaçlarını onar kez ilerletir; bir garanti
  onlunun ortasında tetiklenirse sayaç o yuvada sıfırlanır.
- **Bedel:** tek çekim <!--v:veri.kahramanlar.kriyo.bedel_tek-->200<!--/v--> Hadalit, onlu
  <!--v:veri.kahramanlar.kriyo.bedel_onlu-->1.800<!--/v--> (K-12; bugün kodda
  <!--v:veri.kahramanlar.kriyo.v8_bedel_tek-->20<!--/v--> / <!--v:veri.kahramanlar.kriyo.v8_bedel_onlu-->180<!--/v-->, 🟡).
- Her çekim ayrıca 1 Anı Kristali verir.
- **Kriyo-Kapsül bileti:** <!--v:veri.kahramanlar.kriyo.bilet-->Kriyo-Kapsül bileti bir çekim yerine geçer; iki kapsülde de kullanılır ve garanti sayaçlarını ilerletir.<!--/v-->
  Biletler Derinlik İzni dükkânından ([11 · VIP](11_vip.md)), The Syndicate'in Kriyo Serisi'nden ve
  sezon biletinden ([12](12_syndicate.md)) gelir. Derinlik İzni'nin haftalık ek ücretsiz çekimleri de
  Standart Kapsül'e yazılır.

### 11.2 Kapsüller

<!-- URET:tablo:kahramanlar.kriyo.bannerlar -->
| Kapsül | Havuz | Garanti | Ücretsiz çekim | Süre | Sayaçlar |
| --- | --- | --- | --- | --- | --- |
| Standart Kapsül | Yayımlanmış tüm kahramanlar | Destansı+ 30 · Efsanevi 80 | Evet (Kriyo Salonu) | Sürekli | Kendi sayaçları |
| Öne Çıkan Kapsül | Tüm kahramanlar; Efsanevi sonucun %50'si öne çıkan kahraman | Destansı+ 30 · Efsanevi 80 · kaybedilen 50/50'den sonraki Efsanevi kesin öne çıkan | Hayır | Nesil çıkışında 21 gün; sonra 14 günlük dönüşüm | Öne çıkanlar arasında taşınır |
<!-- /URET:tablo:kahramanlar.kriyo.bannerlar -->

**Öne çıkan kuralı (50/50).** Öne Çıkan Kapsül'den gelen her Efsanevi'nin
%<!--v:veri.kahramanlar.kriyo.one_cikan_payi-->50<!--/v-->'si öne çıkan kahramandır. Kaybedilen bir 50/50'den
sonraki ilk Efsanevi kesinlikle öne çıkandır. Böylece öne çıkan kahramanın beklenen bedeli
<!--v:veri.kahramanlar.kriyo.beklenen_one_cikan-->60<!--/v--> çekim, en kötü durumu
<!--v:veri.kahramanlar.kriyo.en_kotu_one_cikan-->160<!--/v--> çekimdir ve bu sayılar kapsül ekranında
yazar. Öne çıkanlar her yeni nesille gelir; eski nesillerin kahramanları dönüşümlü olarak yeniden
öne çıkar. Sayaçlar ve kaybedilen 50/50 durumu bir öne çıkandan ötekine taşınır; oyuncunun birikimi
hiçbir zaman silinmez.

**Kopyalar.** Sahip olunan bir kahraman yeniden çıkarsa nadirliğinin kopya parçası kadar yıldız
parçası verir; 5★ kahramanın parçası Anı Kristali'ne döner.

### 11.3 Ücretsiz çekim ve Kriyo Salonu

Standart Kapsül'de ücretsiz çekim vardır. Aralığı [Kriyo Salonu](binalar/cryo_hall.md)'nun seviyesi
belirler: <!--v:veri.binalar.binalar.cryo_hall.kilitler.0.metin-->Günde 1 ücretsiz çekim<!--/v--> (Sv 1),
<!--v:veri.binalar.binalar.cryo_hall.kilitler.1.metin-->Ücretsiz çekim 20 saatte bir<!--/v--> (Sv 20),
<!--v:veri.binalar.binalar.cryo_hall.kilitler.2.metin-->Ücretsiz çekim 16 saatte bir<!--/v--> (Sv 35),
<!--v:veri.binalar.binalar.cryo_hall.kilitler.3.metin-->Ücretsiz çekim 12 saatte bir<!--/v--> (Sv 50).
Ücretsiz çekim birikmez; hazır olduğunda kapsül simgesinde sessiz bir ışık yanar. Ücretsiz
çekimler garanti sayaçlarını ilerletir ve günlük "1 Kriyo-Kapsül çekimi" görevini sayar. Bugün
kodda aralık sabit 24 saattir (✅); Kriyo Salonu kademeleri yenidir (⬜).

### 11.4 Hadalit ekonomisi içinde

F2P oyuncunun VIP hariç günlük Hadalit geliri, hepsi kapsüle giderse günde yaklaşık iki çekime
yeter ([15 · Ekonomi Akışı](15_ekonomi_akisi.md) Hadalit tablosu). Gerçekte Hadalit kalkan,
hızlandırma ve konuşlanmaya da gider; bölüm 15'teki tahminler Hadalit'in %60'ının kapsüle, geri
kalanının bu işlere gittiğini ve ücretsiz çekimlerin aksatılmadığını varsayar.

---

## 12. Kahraman kartları ve çağırma ✅

Kahramanlar yalnızca kapsülden gelmez. **Kahraman kartı**, belirli bir nadirlikten rastgele bir
kahramanın kartlarını verir; kartlar çantada birikir. Sahip olunmayan kahraman yeterli kartla
**çağrılır**: Sıradan <!--v:veri.kahramanlar.nadirlikler.common.cagri_parca-->10<!--/v-->, Nadir
<!--v:veri.kahramanlar.nadirlikler.rare.cagri_parca-->20<!--/v-->, Destansı
<!--v:veri.kahramanlar.nadirlikler.epic.cagri_parca-->30<!--/v-->, Efsanevi
<!--v:veri.kahramanlar.nadirlikler.legendary.cagri_parca-->50<!--/v--> kart. Fazlası yıldız parçasına
geçer; sahip olunan kahramanın kartı doğrudan yıldız parçasıdır.

<!-- URET:tablo:kahramanlar.kartlar -->
| Eşya | İçerik | Nereden | Durum |
| --- | --- | --- | --- |
| Sıradan Kahraman Kartı | Rastgele bir Sıradan kahramanın 10 kartı | Günlük sandık (60 puan), Bronz Sandık, d_pod görevi | ✅ |
| Nadir Kahraman Kartı | Rastgele bir Nadir kahramanın 10 kartı | Günlük sandık (100 puan), Gümüş Sandık, hikâye | ✅ |
| Destansı Kahraman Kartı | Rastgele bir Destansı kahramanın 10 kartı | Hikâye bölümleri, Altın Sandık, Derinlik Sandığı | ✅ |
| Efsanevi Kahraman Kartı | Rastgele bir Efsanevi kahramanın 10 kartı | Hikâye bölümleri, Altın Sandık, etkinlik zirveleri | ✅ |
| Seçmeli Destansı Kart | Seçilen bir Destansı kahramanın 10 kartı | Kronik aşamaları, Derinlik Ligi sezonu | ⬜ |
| Seçmeli Efsanevi Kart | Seçilen bir Efsanevi kahramanın 10 kartı | Kronik aşama 5 ve 10, Akıntı Savaşı sezonu | ⬜ |
| Adlı kahraman kartı | Belirli bir kahramanın kartları | Nesil etkinlikleri, İttifak Dükkânı (Destansı'ya kadar) | ⬜ |
<!-- /URET:tablo:kahramanlar.kartlar -->

**Kuruluş havuzu.** The Syndicate'teki ilk satın almanın Kuruluş Hediyesi, aşağıdaki üç Destansı
kahramandan birinin kartlarını seçtirir ([12](12_syndicate.md)). Havuz her birlik türünden bir
kahraman içerir ve önerilen köken kahramanlarıyla (hepsi Nadir) çakışmaz; ilk alım kimsenin
kökenini "tamamlamaz", yalnızca bir rol ekler.

<!-- URET:tablo:kahramanlar.kriyo.kurulus_havuzu -->
| Kahraman | Yatkın birlik | Fraksiyon | Neden |
| --- | --- | --- | --- |
| Dr. Emi Sakamoto | Zırhlı Dalgıç | Pasifik Tarikatı | Ana hekim Subayı; iyileştirme hızı becerisi her oyuncuya yarar |
| Juno Vega | Zıpkıncı | Unutulanlar | Zıpkıncı Kaptanı; Enerji becerisi orta oyunun açığını kapatır |
| Sable | Ark Askeri | Uçurum Tarikatı | Garnizon Kaptanı; depo becerisi yağmaya karşı korur |
<!-- /URET:tablo:kahramanlar.kriyo.kurulus_havuzu -->

**Rastgele kartın incelikleri.**

- Rastgele kart önce **sahip olunmayan** kahramanlara yönelir: o nadirlikte eksik kahraman varsa
  kart onlardan birine düşer (çağırmaya yaklaştırır). Hepsine sahipse en düşük yıldızlı kahramana
  düşer. Bu kural kartın "boşa gitme" hissini kaldırır.
- Çağırmaya yetecek kart birikince çanta bunu sessizce gösterir; çağırma tek dokunuştur.
- Hikâye ilk iki kahramanı garanti eder: 1. bölüm Dr. Lyra Moss, 2. bölüm Tess Ruiz
  ([15](15_ekonomi_akisi.md) hikâye bölümleri).

---

## 13. Çıkış takvimi

<!-- URET:tablo:kahramanlar.nesiller -->
| Nesil | Sunucu günü | Kahraman | Tema | Kimler |
| ---: | ---: | ---: | --- | --- |
| 1 | 0 | 18 | Çıkış kadrosu: dört Sıradan, altı Nadir, beş Destansı, üç Efsanevi; her köken için en az üç kahraman | Rourke, Moss, Ruiz, Kostina, Kael, Hale, Okafor, Quispe, Farahani, Noa, Vega, Sable, Sakamoto, Ilo, Gröndahl, Aelis, Boateng, Meridyen |
| 2 | 56 | 4 | Kadim kapılar ve Süperalaşım çağı: ilk uyanan Muhafız, rekor dalışçı | Ufuk, Saoirse Kavanagh, Deniz Karaca, Dr. Priya Raman |
| 3 | 112 | 4 | Avcılar: Leviathan sezonu ve kristal ayini | Dr. Han Seo-yeon, Kiri, Sura, Odalys Ferreira |
| 4 | 168 | 4 | Koloni: Derindoğanlar'ın Anası, hastane gemisi, Uçurumun Sözcüsü | Ilian Varga, Dr. Isagani Dela Cruz, Sela, Wira Santoso |
<!-- /URET:tablo:kahramanlar.nesiller -->

**Neden bu takvim?**

- Çıkış kadrosunda her köken en az üç kahramanla temsil edilir; her oyuncu ilk gün kendi
  fraksiyonuyla filo sinerjisi kurabilir.
- Çıkışta yalnızca üç Efsanevi vardır (Aelis, Kwame Boateng, Meridyen; Ark, Zırhlı ve Zıpkıncı
  birer tane): %<!--v:veri.kahramanlar.nadirlikler.legendary.agirlik-->2<!--/v-->'lik Efsanevi şansı üçe
  bölündüğü için F2P ilk aylarda bir Efsanevi'yi gerçekten büyütebilir.
- 2. nesil, F2P'nin Reaktör 30'u geçip Süperalaşım çağına girdiği döneme denk gelir: Süperalaşım
  becerili iki kahraman (Dr. Priya Raman, Ufuk) ve kadim kapıların ilk hikâyesi birlikte gelir.
- Sonraki nesiller her 56 günde bir (sekiz hafta) gelir; Kronik aşamalarıyla hizalanması
  [13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md) ve [16](16_kronik_kokenler_profil.md)
  ile yapılır. Yeni sunucular aynı takvimi kendi gün sayacıyla yeniden oynar.
- Yeni nesil eskiyi geçersiz kılmaz: her yeni Efsanevi bir **halef** ya da **yeni rol** getirir
  (Meridyen → inşa, Ufuk → Süperalaşım, Kiri → av), istatistik tavanı yükseltmez.

---

## 14. Kadro

<!-- URET:kahraman-ozet -->
| Kahraman | Nadirlik | Sınıf | Fraksiyon | Nesil | Kodda | Güç (Sv 50, 5★) |
| --- | --- | --- | --- | ---: | --- | ---: |
| [Aelis](kahramanlar/aelis.md) | Efsanevi | Bilim insanı | Derindoğanlar | 1 | ✅ | 48.077 |
| [Amiral Kwame Boateng](kahramanlar/kwame.md) | Efsanevi | Komutan | Atlantik Birliği | 1 | ⬜ | 52.032 |
| [Dr. Han Seo-yeon](kahramanlar/seoyeon.md) | Efsanevi | Bilim insanı | Pasifik Tarikatı | 3 | ⬜ | 48.077 |
| [Dr. Isagani Dela Cruz](kahramanlar/isagani.md) | Efsanevi | Hekim | Pasifik Tarikatı | 4 | ⬜ | 51.514 |
| [Ilian Varga](kahramanlar/ilian.md) | Efsanevi | Komutan | Uçurum Tarikatı | 4 | ⬜ | 52.032 |
| [Kiri](kahramanlar/kiri.md) | Efsanevi | Avcı | Derindoğanlar | 3 | ⬜ | 50.502 |
| [Meridyen](kahramanlar/meridyen.md) | Efsanevi | Pilot | Unutulanlar | 1 | ⬜ | 50.328 |
| [Saoirse Kavanagh](kahramanlar/saoirse.md) | Efsanevi | Pilot | Atlantik Birliği | 2 | ⬜ | 50.328 |
| [Sela](kahramanlar/sela.md) | Efsanevi | Hekim | Derindoğanlar | 4 | ⬜ | 51.514 |
| [Sura](kahramanlar/sura.md) | Efsanevi | Avcı | Uçurum Tarikatı | 3 | ⬜ | 50.502 |
| [Ufuk](kahramanlar/ufuk.md) | Efsanevi | Mühendis | Unutulanlar | 2 | ⬜ | 51.344 |
| [Deniz Karaca](kahramanlar/deniz.md) | Destansı | Mühendis | Atlantik Birliği | 2 | ⬜ | 39.652 |
| [Dr. Emi Sakamoto](kahramanlar/emi.md) | Destansı | Hekim | Pasifik Tarikatı | 1 | ⬜ | 39.797 |
| [Dr. Priya Raman](kahramanlar/priya.md) | Destansı | Bilim insanı | Pasifik Tarikatı | 2 | ⬜ | 37.134 |
| [Ilo](kahramanlar/ilo.md) | Destansı | Hekim | Derindoğanlar | 1 | ⬜ | 39.797 |
| [Juno Vega](kahramanlar/juno.md) | Destansı | Pilot | Unutulanlar | 1 | ✅ | 38.872 |
| [Mattias Gröndahl](kahramanlar/mattias.md) | Destansı | Avcı | Uçurum Tarikatı | 1 | ⬜ | 39.003 |
| [Odalys Ferreira](kahramanlar/odalys.md) | Destansı | Komutan | Uçurum Tarikatı | 3 | ⬜ | 40.189 |
| [Sable](kahramanlar/sable.md) | Destansı | Mühendis | Uçurum Tarikatı | 1 | ✅ | 39.652 |
| [Wira Santoso](kahramanlar/wira.md) | Destansı | Avcı | Unutulanlar | 4 | ⬜ | 39.003 |
| [Darius Farahani](kahramanlar/darius.md) | Nadir | Bilim insanı | Uçurum Tarikatı | 1 | ⬜ | 28.520 |
| [Dr. Orin Hale](kahramanlar/orin.md) | Nadir | Bilim insanı | Pasifik Tarikatı | 1 | ✅ | 28.520 |
| [Kael](kahramanlar/kael.md) | Nadir | Avcı | Derindoğanlar | 1 | ✅ | 29.952 |
| [Kaptan Amara Okafor](kahramanlar/amara.md) | Nadir | Komutan | Atlantik Birliği | 1 | ⬜ | 30.900 |
| [Noa](kahramanlar/noa.md) | Nadir | Pilot | Derindoğanlar | 1 | ⬜ | 29.854 |
| [Rafael Quispe](kahramanlar/rafa.md) | Nadir | Mühendis | Unutulanlar | 1 | ⬜ | 30.485 |
| [Dr. Lyra Moss](kahramanlar/lyra.md) | Sıradan | Hekim | Pasifik Tarikatı | 1 | ✅ | 23.464 |
| [Komutan Elias Rourke](kahramanlar/mara.md) | Sıradan | Komutan | Atlantik Birliği | 1 | ✅ | 23.696 |
| [Tess Ruiz](kahramanlar/tess.md) | Sıradan | Mühendis | Atlantik Birliği | 1 | ✅ | 23.376 |
| [Vera Kostina](kahramanlar/vera.md) | Sıradan | Pilot | Unutulanlar | 1 | ⬜ | 22.902 |
<!-- /URET:kahraman-ozet -->

Her satır kahramanın sayfasına gider: biyografi, mürettebat becerisinin yıldız yıldız değeri,
yetenekler (seviye 1–5), istatistik tablosu ve tasarım notları.

**Kadronun dengesi.** 30 kahraman altı sınıfa beşer, beş fraksiyona altışar, üç birlik türüne
onar bölünür. İnsan olmayan ya da yarı insan varlıklar azınlıktır ve her biri bir hikâye
kapısıdır: Aelis (Hadalit'le dirilen biyolog), Ufuk (uyanan Kadim Muhafız), Meridyen (terk edilmiş
istasyonun zekâsı) ve Sura (bedeni kristale açılmış avcı). Geri kalan herkes çukur kolonilerinde
yaşayan, dünyanın dört bir yanından gelmiş insanlardır; biyografileri çukurlara ve deniz bilimine
bağlanır ([18 · Bilim Temeli](18_bilim_temeli.md)). Tarihteki kişiler kahraman yapılmaz;
biyografilerde yalnızca mühendislik dersi olarak anılan araçlar vardır (K-26).

---

## 15. Ücretsiz oyuncu ve ödeme yapan oyuncu

<!-- URET:tablo:kahramanlar.f2p_beklenti -->
| Profil | Yılda çekim | Yılda Efsanevi (beklenen) | Anı Kristali / ay | Yıl sonunda 5★ | Tüm yetenekleri 5. seviye |
| --- | ---: | ---: | ---: | --- | --- |
| Ücretsiz oyuncu | 800 | 20 | 305 | 1–2 Efsanevi, 3–4 Destansı | 2 Efsanevi |
| Aylık kart + VIP 8 | 1.400 | 35 | 550 | 3–4 Efsanevi, 6+ Destansı | 4 Efsanevi |
| Balina | 6.000 | 150 | 2.200 | Tüm Efsanevi kadro | Tüm Efsanevi kadro |
<!-- /URET:tablo:kahramanlar.f2p_beklenti -->

- **Ücretsiz oyuncu** bir yılda iki filoyu Efsanevi Kaptanlarla kurar; geri kalan filolar
  Destansı Kaptanlarla güçlüdür. Kökeni ona ilk gün bir Nadir verir, Kronik ve Seçmeli kartlar
  seçtiği Efsanevi'yi tamamlamasına yardım eder. Ödemesizin önündeki asıl duvar yıldız parçasıdır;
  bu yüzden Seçmeli Efsanevi Kart, Uyanış Özü ve fazla parça → kristal dönüşümü oynayarak gelir.
- **Aylık kart** sahibi aynı yolu daha kısa yürür: daha çok çekim, daha çok kristal, Sendika
  Sözleşmesi'nin günlük Hadalit'i. Gücünü para değil zaman kazandırır.
- **Balina** bütün kadroyu toplar, ama **seviye tavanı reaktöre bağlı** olduğu ve Uyanış Özü
  satılmadığı için kahraman gücünü üssünün önüne geçiremez. Savaşın ağırlığı askerdedir; balinanın
  avantajı daha çok seçenek ve daha erken Uyanış'tır.
- **Satışta olanlar** (12 · The Syndicate): Hadalit, içeriği yazılı Anı Kristali kutuları, nesil
  paketlerinde adlı kahraman kartı. **Satılmayanlar:** Uyanış Özü, Efsanevi garanti sayacını
  atlatan herhangi bir şey, rastgele içerikli "gizemli" kahraman kutusu.

---

## 16. Arayüz notları (Sessiz lüks)

- **Kahraman ekranı** beş sekmedir: Genel (istatistik, rol, mürettebat becerisi), Yetenekler,
  Ağaç, Teçhizat, Hikâye. Üstte HeroStage'de 3D model, altta portre ve nadirlik ışığı.
- **Rol rozeti:** Kartın köşesinde kahramanın nerede olduğu yazar: "Ekipte · Yuva 2",
  "Kaptan · 3. Denizaltı", "Subay", "Boşta". Ekipteki kahramanı filoya sürükleyince takas önerisi
  çıkar; açılır pencere değil, satır içi bir onaydır.
- **Ağaç ekranı** sakin bir sonar haritası gibi çizilir: düğümler biyolüminesan noktalar, kademe
  eşikleri ince halkalar. Etkisiz bağlamdaki düğümler soluktur; dizilim kaydı iki sekme.
- **Kriyo-Kapsül ekranı:** oran tablosu, iki garanti sayacı ("Destansı+ için 12 çekim",
  "Efsanevi için 47 çekim") ve öne çıkan kahramanın 50/50 durumu ince bir satırda hep görünür.
  Öne çıkan kapsülün bitişi sakin bir tarih olarak yazılır ("3 Mart'a kadar"), geri sayım yoktur.
  "SON ŞANS", "İNDİRİM" ya da yanıp sönen rozet kullanılmaz.
- **Eksik kaynak:** Anı Kristali ya da Uyanış Özü yetmezse ekran The Syndicate'i değil kaynak
  listesini gösterir ("Bu hafta Arena'dan 20 daha"). The Syndicate bağlantısı listenin en altında,
  küçük bir satırdır.
- **Kapsül açılışı** sonar ve buz sesiyle 3–4 saniye sürer; onlu çekim tek ekranda özetlenir.
  Atla her zaman vardır, tekrar animasyonu isteğe bağlıdır.
- **Erişilebilirlik:** nadirlik yalnızca renkle değil simgeyle de gösterilir (nokta sayısı).

## 17. Uç durumlar

- **Ekipteki kahraman filoya atanırsa:** takas olur; boşalan ekip yuvası boş kalır ve beceri o an
  durur. Filo yürüyüşteyse atama reddedilir (`HeroBusy`).
- **Filo savaştayken ağaç ya da teçhizat değişikliği:** reddedilir; dizilim geçişi de dahil.
- **Kahraman ölmez ve esir düşmez.** Kaybedilen savaşta kahraman üsse döner; askerler K-21'e göre
  yaralı ya da ölüdür.
- **Reaktör tavanı:** Kahraman Sv 30, reaktör 28 olamaz; tavan reaktörden düşük olduğu sürece
  "Eğit" düğmesi kapalıdır ve nedenini yazar. Mevcut oyuncularda (v8) reaktörü geçmiş kahraman
  varsa seviyesi korunur, ama reaktör yetişene kadar daha fazla eğitilemez.
- **Onlu çekimde iki garanti:** Efsanevi garantisi Destansı+ garantisinden önce gelir; aynı yuvada
  ikisi tetiklenirse Efsanevi verilir ve iki sayaç da sıfırlanır.
- **Yeni nesil kahraman standart kapsüle ne zaman girer?** Öne çıkan dönemi bittiğinde
  (<!--v:veri.kahramanlar.kurallar.one_cikan_standarda_gecis_gun-->21<!--/v--> gün).
  O zamana kadar yalnızca Öne Çıkan Kapsül'den ve adlı kartlardan gelir.
- **Fazla parça dönüşümü** yalnızca 5★'tan sonra olur; 4★ kahramanda biriken parça bekler.
- **Kart çağırma ile kapsül aynı anda:** Kahraman kapsülden gelirse çantadaki kartları yıldız
  parçasına döner; hiçbir kart kaybolmaz.
- **Silinen ya da birleşen sunucular:** garanti sayaçları ve öne çıkan durumu oyuncuya bağlıdır,
  sunucuya değil.
- **Bedel ve tüketim indirimleri** üst üste binince alt sınıra takılır
  (%<!--v:veri.kahramanlar.kurallar.bedel_alt_siniri-->25<!--/v-->); arayüz fazla indirimi gri gösterir ve
  "tavanda" yazar, böylece oyuncu boşa kahraman yerleştirmez.

## 18. Bugün kodda

<!-- URET:tablo:kahramanlar.durum -->
| Konu | Durum | Kodda / not |
| --- | --- | --- |
| Komuta Ekibi: 3 yuva, Reaktör 1 / 5 / 10 | ✅ | HeroRules.OpenSlots · catalog heroes.crewSlotReactorLevels |
| Mürettebat becerisi formülü ve 8 kahramanın değerleri | ✅ | HeroRules.EffectValue; değerler v8 ile aynı |
| Yeni mürettebat etki türleri | ⬜ | ApplyEffect yalnızca buildTime, researchTime, capacity, production tanıyor |
| Süre becerileri bölen olarak (K-13) | 🟡 | Kod yüzdeyi süreden çıkarıyor (ResearchRules.ApplyPercent) |
| Seviye tavanı ★ × 10 | ✅ | HeroRules.LevelCap |
| Seviye tavanı ≤ Reaktör seviyesi | ⬜ | Yeni kural |
| Eğitim bedeli: Gıda + Biyokütle, reaktör eğrisi | 🟡 | Kod: 150 Ti + 100 BK × 1,08^(Sv−1) × nadirlik çarpanı |
| Kahraman deneyimi (savaştan) | ⬜ | 09'daki PvE ödülleri bunu verir |
| Yıldız parçası 10 / 20 / 40 / 80 | ✅ | heroes.shardsPerStar |
| Kopya parçası 10 / 10 / 15 / 20 | ✅ | rarities[].duplicateShards |
| Kahraman kartı ve çağırma 10 / 20 / 30 / 50 | ✅ | RecruitHeroCommand · items card_* |
| Kriyo-Kapsül oranları 65 / 25 / 8 / 2 | ✅ | rarities[].weight |
| Destansı+ garantisi 30. çekim | ✅ | pod.pity / pityRarity |
| Efsanevi garantisi 80. çekim (K-04) | ⬜ | İkinci sayaç gerekir |
| Öne Çıkan Kapsül, 50/50 | ⬜ | Banner kimliği HeroRules.PodBanner tek |
| Onlu çekimde Nadir+ garantisi | ⬜ | — |
| Çekim bedeli 200 / 1.800 HD (K-12) | 🟡 | Kod 20 / 180 |
| Ücretsiz çekim 24 saatte bir | ✅ | pod.freeSeconds; Kriyo Salonu kademeleri ⬜ |
| Fazla parça → Anı Kristali | ⬜ | Bugün 5★ sonrası parça birikiyor |
| ATK / DEF / HP ve stat tabanlı güç | 🟡 | Kod: taban güç × (1 + 0,1(Sv−1)) × (1 + 0,2(★−1)) |
| Yetenekler, öfke, Anı Kristali | ⬜ | — |
| Yetenek ağaçları | ⬜ | — |
| Kaptan / Subay, filo kadrosu | ⬜ | 07 ve 08 ile birlikte |
| Uyanış | ⬜ | — |
| Fraksiyon sinerjisi ve ekip rezonansı | ⬜ | — |
| 22 yeni kahraman | ⬜ | Kimlikler, portreler, tr.txt metinleri |
| Kökene bağlı başlangıç kahramanı (K-19) | ⬜ | 16 ile birlikte |
<!-- /URET:tablo:kahramanlar.durum -->

## 19. Uygulama notları

**`shared/catalog/catalog.json` (v9)**
- `heroes.list`: 22 yeni kayıt (`id`, `heroClass`, `faction`, `rarity`, `effect`, `effectBase`,
  `effectPerLevelTenths`, `effectPerStar`, `color`, `portrait`, `model`) + yeni alanlar `generation`,
  `trees[3]`, `skills[]` (tür, yıldız, `values[5]`).
- `heroes.rarities[]`: `stats {atk, def, hp}`, `excessShardCrystals`, `skillCount`, `awakening`.
- `heroes.classes[]` (yeni): stat çarpanı, `troop`.
- `heroes.pod`: `cost` 200 / `tenCost` 1.800 (K-12), `pity` 30 + yeni `legendaryPity` 80,
  `tenGuaranteeRarity: "rare"`, `banners[]` (standart, öne çıkan; `featuredHeroId`, `featuredShare`
  50, başlangıç/bitiş tarihi), `freeSecondsByHallLevel` (Kriyo Salonu kademeleri).
- `heroes.levelCost` → `trainingRatios {Food: 0.03, Biomass: 0.01}`; bedel reaktör eğrisinden
  okunur (catalog v9'un `curves.reactorCost` alanı, [02](02_tempo_ve_denge.md)).
- `heroes.talents`: 14 ağaç × 11 düğüm, kademe eşikleri, puan kuralı.
- `heroes.skillCosts`, `heroes.awakening`, `heroes.synergy`, `heroes.rage`, `heroes.command`.
- `items`: `memory_crystal` (Anı Kristali), `awakening_essence`, `calibration_chip`,
  `card_choice_epic`, `card_choice_legendary`, adlı kart şablonu.

**`shared/HADAL.Shared/Simulation/HeroRules.cs`**
- `LevelCap(rules, stars, reactorLevel)` = min(★ × 10, reaktör).
- `LevelCost` → reaktör eğrisi × oran × nadirlik çarpanı; `ApplyExperience` (savaş deneyimi).
- `Pull`: iki sayaç (`sinceEpicPlus`, `sinceLegendary`), banner kimliği, öne çıkan 50/50 bayrağı
  (`featuredLost`), onlu çekimde Nadir+ kuralı. `PodBanner` sabiti banner listesine dönüşür.
- `EffectValue` aynı kalır. `ApplyEffect` / `IsKnownEffect` yeni anahtarları tanır:
  `trainingTime`, `trainingCost`, `healingSpeed`, `healingCost`, `woundedCapacity`, `gatherSpeed`,
  `carryCapacity`, `sonarRegen`, `salvageReward`, `huntReward`, `materialFind`, `craftTime`,
  `heroTrainCost`, `hadaliteChance`, `upkeep:<Kaynak>`, `consumption:<Kaynak>`, `production:All`,
  `production:Alloy`. Süre anahtarları `PlayerModifiers` içinde hız yüzdesi olarak toplanır (K-13);
  bedel ve tüketim anahtarları çarpan olarak, `kurallar.bedel_alt_siniri` tabanıyla.
- `Power` → stat tabanlı formül; `Stats(hero, level, stars)`.
- Yeni: `TalentRules.cs` (puan, kademe kilidi, sıfırlama, dizilim), `SkillRules.cs` (Anı Kristali
  bedeli, seviye kilidi), `AwakeningRules.cs`, `FleetRules.cs` (Kaptan/Subay, sinerji, yatkınlık,
  komuta bonusu; [07](07_birlikler_ve_savas.md) savaş çözücüsüne girdi verir).

**Komutlar** (`shared/HADAL.Shared/Commands/`): `UpgradeSkillCommand`, `AllocateTalentCommand`,
`ResetTalentsCommand`, `SwitchTalentPresetCommand`, `AwakenHeroCommand`, `AssignFleetHeroCommand`
(denizaltı, yuva: Kaptan/Subay), `ExecuteGachaCommand` alanına `BannerId`.

**Sunucu** (`server/Hadal.Game/`): `PlayerSession.Heroes.cs` yeni komutlar; `Persistence/PlayerExtras.cs`
→ kahraman başına `skillLevels[]`, `talents{}`, `presets[2]`, `awakened`, `fleetSlot`; oyuncu başına
`pity {epicPlus, legendary, featuredLost}` banner bazında; Anı Kristali ve özler çantada.
Güç hesabı (`PlayerSession.Heroes.cs`) K-14'e göre yeniden yazılır.

**İstemci** (`Assets/_Hadal/Scripts/UI/`): `SocialView.Heroes.cs` beş sekmeye ayrılır
(yeni `HeroTalentView`, `HeroSkillView`, `HeroGearView`), `HeroStage.cs` Uyanış izi; Kriyo-Kapsül
ekranında iki sayaç ve banner seçici; filo kadrosu ekranı (08 ile ortak). `tr.txt`: 22 kahraman adı,
biyografi, 121 yetenek adı ve açıklaması, 154 düğüm, 14 uzmanlık.

**Göç (v8 → v9):** mevcut oyuncuların kahraman seviyeleri korunur; eğitim bedeli farkı geri
alınmaz. Garanti sayacı yeni `sinceEpicPlus`'a taşınır, `sinceLegendary` sıfırdan başlar. Eski
"Toplam kahraman gücü" ekranda yeni formülle yeniden hesaplanır.

## 20. Esin kaynakları

| Mekanik | Türdeki örnek (genel) | Hadal'daki biçimi |
| --- | --- | --- |
| Komutan yetenek ağaçları | Rise of Kingdoms'taki komutan yetenek ağaçlarına benzer: üç ağaç, puan bir ağacı bitirip ikinciyi yarılamaya yeter | 14 Hadal uzmanlığı (Derinlik, Akıntı, Avcılık…), 7 kademe, anahtar ve zirve düğümleri |
| Birincil / ikincil komutan | Rise of Kingdoms'ta yalnızca birincil komutanın ağacı ve ekipmanı çalışır | Kaptan + 2 Subay; Subay yetenek taşır, ağaç ve teçhizat Kaptan'da |
| Yıldız başına seviye tavanı | Rise of Kingdoms'taki yıldızla açılan seviye tavanına benzer | ★ × 10, ayrıca Reaktör tavanı |
| Tüm yetenekler dolunca açılan son yetenek | Rise of Kingdoms'taki uzmanlık yeteneğine benzer | Uyanış: 5★ + bütün yetenekler 5 + Uyanış Özü |
| Rarity'ye göre evrensel yetenek malzemesi | Rise of Kingdoms'taki evrensel heykel malzemelerine benzer | Tek Anı Kristali, bedel nadirlikle büyür |
| Yıldız toplamıyla açılan yetenekler, sınıf üçgeni ve anıtlar | Last Asylum'daki kahraman yıldızları, sınıf karşıtlığı ve heykel binalarına benzer | Yıldızla açılan yetenekler, Zırhlı > Ark > Zıpkıncı > Zırhlı, üç anıt |
| Kahraman rol, asker sayı | Last Asylum'da kahramanın rol ve yetenek, askerin sayı vermesi | Komuta bonusu yüzde olarak; savaşın ağırlığı askerde |
| Ekonomi kahramanları | Türün kahramanları savaşla birlikte üssü de güçlendirir | Ayrı Komuta Ekibi; aynı kahraman ya ekipte ya filoda |
| Kahraman kartıyla çağırma | Türde görev ve sandıklardan gelen kahraman parçaları | Kahraman kartları + sahip olunmayana yönelen rastgele kart |

Kopyalanan ad, metin ya da sayı yoktur; yapılar Hadal'ın derin deniz dünyasına göre yeniden
kurulmuştur.

## 21. Sözlük

| Terim | Anlamı |
| --- | --- |
| Komuta Ekibi | Ekonomi becerilerinin çalıştığı 3 kahraman yuvası |
| Mürettebat becerisi | Kahramanın ekipteyken verdiği ekonomi etkisi |
| Filo | Bir denizaltı: Kaptan, en çok iki Subay ve askerler |
| Kaptan | Filonun ana kahramanı; ağacı ve teçhizatı çalışır |
| Subay | Filonun yardımcı kahramanı; aktif, pasif ve sefer yetenekleri çalışır |
| Komuta bonusu | Kahraman istatistiklerinin askerlere yüzde olarak eklenmesi |
| Öfke | Aktif yeteneği dolduran çubuk |
| Katsayı | Yetenek hasarı ve iyileştirmesinin ölçüsü; 100 = bir normal saldırı |
| Anı Kristali | Yetenek seviyesi malzemesi |
| Uyanış / Uyanış Özü | Destansı ve Efsanevi kahramanın son adımı / onun satılmayan malzemesi |
| Dizilim | Bir Kaptan'ın ağaç puanı dağılımı; iki tane kaydedilir |
| Anahtar, Zirve | Ağaçta kural değiştiren tek puanlık düğümler |
| Yatkınlık | Sınıfın bağlı olduğu birlik türü |
| Ekip rezonansı | Ekipte üç aynı fraksiyonun ekonomi bonusu |
| Öne Çıkan Kapsül | Bir kahramanı 50/50 kuralıyla öne çıkaran kapsül |
| Nesil | Kahramanların çıkış dalgası |
| Kalibrasyon Çipi | Ağaç sıfırlama eşyası |

<!-- URET:sozluk-kahramanlar -->
| Türkçe ad | Kimlik |
| --- | --- |
| [Komutan Elias Rourke](kahramanlar/mara.md) | `mara` |
| [Dr. Lyra Moss](kahramanlar/lyra.md) | `lyra` |
| [Tess Ruiz](kahramanlar/tess.md) | `tess` |
| [Vera Kostina](kahramanlar/vera.md) | `vera` |
| [Kael](kahramanlar/kael.md) | `kael` |
| [Dr. Orin Hale](kahramanlar/orin.md) | `orin` |
| [Kaptan Amara Okafor](kahramanlar/amara.md) | `amara` |
| [Rafael Quispe](kahramanlar/rafa.md) | `rafa` |
| [Darius Farahani](kahramanlar/darius.md) | `darius` |
| [Noa](kahramanlar/noa.md) | `noa` |
| [Juno Vega](kahramanlar/juno.md) | `juno` |
| [Sable](kahramanlar/sable.md) | `sable` |
| [Dr. Emi Sakamoto](kahramanlar/emi.md) | `emi` |
| [Ilo](kahramanlar/ilo.md) | `ilo` |
| [Mattias Gröndahl](kahramanlar/mattias.md) | `mattias` |
| [Deniz Karaca](kahramanlar/deniz.md) | `deniz` |
| [Dr. Priya Raman](kahramanlar/priya.md) | `priya` |
| [Odalys Ferreira](kahramanlar/odalys.md) | `odalys` |
| [Wira Santoso](kahramanlar/wira.md) | `wira` |
| [Aelis](kahramanlar/aelis.md) | `aelis` |
| [Amiral Kwame Boateng](kahramanlar/kwame.md) | `kwame` |
| [Meridyen](kahramanlar/meridyen.md) | `meridyen` |
| [Ufuk](kahramanlar/ufuk.md) | `ufuk` |
| [Saoirse Kavanagh](kahramanlar/saoirse.md) | `saoirse` |
| [Dr. Han Seo-yeon](kahramanlar/seoyeon.md) | `seoyeon` |
| [Kiri](kahramanlar/kiri.md) | `kiri` |
| [Sura](kahramanlar/sura.md) | `sura` |
| [Ilian Varga](kahramanlar/ilian.md) | `ilian` |
| [Dr. Isagani Dela Cruz](kahramanlar/isagani.md) | `isagani` |
| [Sela](kahramanlar/sela.md) | `sela` |
<!-- /URET:sozluk-kahramanlar -->

## 22. Açık sorular

1. **07 · Birlikler ve Savaş:** 07 bu bölümün `komuta`, `sinerji` ve `ofke` verisini kullanıyor.
   07'nin "öfke ritmi" sorusuna yanıt olarak öfke güncellendi (başlangıç 200, tur başı 150, kayıpta
   50): kayıplı 10 turluk savaşta iki, boss savaşında üç tetik. 07'deki "9. turun başında" cümlesi
   buna göre düzeltilmeli. Yetenek değerleri 07'nin "katsayı = filonun turluk normal hasarı" tanımına
   göre yeniden ölçeklendi; yetenek ağacındaki şans düğümleri belirlenimci yapıldı. Karşı üçgen
   çarpanları yalnızca 07'nin verisinden okunuyor.
2. **08 · Denizaltılar ve Seferler:** "Sefer / basınç" yeteneklerinin anahtarları (basınç hasarı,
   gövde dayanımı, olay başarı şansı, keşif hızı, kalıntı şansı) sefer sisteminin kavramlarıyla
   eşleşiyor mu? Seferde kahramanların birim olarak dövüşmesi kararı 08'in. 08'in "üste bekleyen
   filo savunma filosu olur" ifadesi ile 07'nin Askeri Kubbe'deki Savunma Kaptanı koltuğu tek modele
   indirilmeli; bu bölüm ikisini de "savunma komutası" diye anar.
3. **09 · Dünya Haritası:** "Kahraman deneyimi" ödülü burada deneyim puanı olarak tanımlandı
   (1 puan = 1 birim eğitim kaynağı). Miktarlar 09'un tablosunda bu birimle verilmeli. Siren Sürüsü
   ve Fay Çatlağı I'in Anı Kristali şansı burada aylık ~30 olarak hesaplandı.
4. **15 · Ekonomi Akışı:** Günlük sandığa (100 puan) 2 Anı Kristali eklenmesi; eğitim oranları
   (Gıda 0,03, Biyokütle 0,01) Gıda bütçesine sığıyor mu? F2P'nin Hadalit'inin %60'ının kapsüle
   gittiği varsayımı.
5. **16 · Köken:** 16'nın bugünkü tablosu köken kahramanı olarak `mara`, `lyra`, `kael`, `juno`,
   `sable` veriyor. Bu bölüm her fraksiyonun **Nadir** kahramanını öneriyor (Okafor, Hale, Kael,
   Quispe, Farahani; 16'nın eşitleme kuralıyla 2★): Rourke herkesin başlangıç kahramanı, Moss ve
   Ruiz hikâyenin ilk iki bölümünde zaten geliyor, Destansı köken kahramanı ise iki kökene erken
   bir Destansı verip dengeyi bozuyor. Okafor, Quispe ve Farahani bu iş için tasarlandı. 16 ile
   birlikte karar verilmeli; karar 16'nınsa bu bölümün `kazanim` metinleri güncellenir.
6. **11 · VIP / 12 · The Syndicate:** Kayıtlı dizilim herkese 2 (11 bunu kabul etti). Derinlik
   İzni 15'in haftalık 7 ek çekimi ve Kriyo Serisi'nin haftalık ~120 çekimi, balina tahminini
   (yılda ~6.000 çekim) doğruluyor; Efsanevi parça duvarı balinayı yaklaşık 5. ayda bütün Efsanevi
   kadroyu 5★'a taşır. Bu hız kabul edilebilir mi? Kuruluş havuzu bu bölümde tanımlandı.
7. **02 · Tempo:** Mürettebat hız becerileri (Rourke + Meridyen birlikte %57 hız; Hale + Han
   Seo-yeon %67 araştırma hızı) simülasyonun `hiz_yuzde` varsayımının içinde mi? Değilse
   Efsanevi eğriler daha da düşürülmeli.
8. **Sahip:** Bir kahramanın aynı anda hem Komuta Ekibi'nde hem filoda olamaması bilinçli bir
   seçimdir (kadro derinliği). Alternatif: ikisinde birden olabilir ama filo üs dışındayken
   mürettebat becerisi durur. Karar sahibin.
9. **Sanat:** 22 yeni portre ve 3D model; dört insan olmayan kahraman (Aelis, Ufuk, Meridyen, Sura)
   için ayrı model dili gerekir. Meridyen'in bedeni bir dron sürüsü ve bir ses: portresi nasıl
   olmalı?
