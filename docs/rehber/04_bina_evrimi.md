# 04 · Bina Evrimi

> **Özet.** Hadal'da bir bina seviye atladıkça görünür biçimde büyür ve değişir. Her binanın altı
> görünümü vardır: her 10 seviyede bir **katman** (Sv 1–9, 10–19, 20–29, 30–39, 40–49 ve 50).
> Katmanların ortasında, Sv 5, 15, 25, 35 ve 45'te binaya küçük bir **ek modül** takılır. Kubbelerin
> beş seviyesi vardır ve her seviye kendi görünümüdür. Çekirdek Reaktör böyle bir yol izler:
> <!--v:veri.evrim.binalar.core_reactor.adlar.0-->Yama Reaktörü<!--/v--> →
> <!--v:veri.evrim.binalar.core_reactor.adlar.1-->Perçinli Reaktör<!--/v--> →
> <!--v:veri.evrim.binalar.core_reactor.adlar.2-->Basınç Çekirdeği<!--/v--> →
> <!--v:veri.evrim.binalar.core_reactor.adlar.3-->Alaşım Çekirdek Kulesi<!--/v--> →
> <!--v:veri.evrim.binalar.core_reactor.adlar.4-->Kadim Kalp<!--/v--> →
> <!--v:veri.evrim.binalar.core_reactor.adlar.5-->Uyanmış Çekirdek<!--/v-->. Bu bölüm görsel dili, ek
> modülleri, başkalaşım anını, adlandırmayı, kozmetik katmanlarla ilişkiyi, dünya haritasındaki üs
> görünümünü, mobil bütçeyi, Unity ve sanat hattı notlarını ve
> <!--v:hesap.sayilar.bina_turu-->34<!--/v--> binanın bütün biçim adlarını verir. Veri: `veri/evrim.json`.

Durum: ⬜. Bugün her binanın tek bir modeli var; seviye görünümü değiştirmiyor.

---

## 1. Neden evrim?

Bu türün en güçlü duygusu **"üssüm büyüyor"** duygusudur. Hadal'da oyuncu Çekirdek Reaktör'ü 50'ye
yaklaşık bir yılda çıkarır ([02 · Tempo ve Denge](02_tempo_ve_denge.md)). O bir yılın emeği ekranda
görünmüyorsa oyuncu ilerlemeyi yalnızca panellerdeki sayılarda görür.

Oyunun sahibinin Last Asylum için en çok altını çizdiği zayıflık budur: binalar seviye atladıkça
neredeyse aynı kalır, aylardır oynanan bir üs ile yeni kurulmuş bir üs uzaktan birbirine benzer.
Rise of Kingdoms görünümü iki katmana ayırır: ilerlemeyle gelen değişim ve ondan bağımsız, ayrıca
edinilen şehir temaları. Hadal bu ayrımı alır ve sınırını keskinleştirir:

- **Evrim kazanılır.** Silüet yalnızca seviyeyle değişir; satın alınmaz.
- **Kozmetik ayrı bir katmandır.** Köken kaplaması, Derinlik İzni (VIP) ışık imzası ve üs
  kaplamaları palet, ışık ve amblem değiştirir; silüete dokunmaz (§6).

Evrimin üç vaadi:

1. **Görünür emek.** Her 10 seviyede yeni bir silüet, her 5 seviyede yeni bir parça. İki yükseltme
   arasında oyuncu bir sonraki görünümü önceden görür (§4).
2. **Okunur üs.** Başka bir üsse bakan oyuncu, panel açmadan o üssün gücünü silüetten ve ışıktan
   okur: dünya haritasında, ralli öncesinde, sonar taramasında.
3. **Sakin gurur.** Değişim yavaş, ağır ve güzeldir. Arayüzün "Quiet Luxury" diliyle aynı: havai
   fişek, konfeti, ekran sarsıntısı yok.

Katmanlar üssün hikâyesini de anlatır. Konvoy enkazından yamanmış ilk kurulumdan (Yama), saha
montajına (Perçin), derinliğin basıncına göre biçimlenen ağır gövdeye (Basınç), nodüllerden dökülen
süperalaşıma (Alaşım), kadim uygarlığın dokusuyla kaynaşmaya (Biyomekanik) ve sonunda Çekirdek'le
aynı ritimde atan bir üsse (Rezonans). İlk dört katmanın malzemesi derin deniz mühendisliğinden
gelir: titanyum basınç küreleri, sintaktik köpük, konik akrilik pencereler, hidrotermal ısı,
polimetalik nodüller. Son iki katman Hadal'ın kendi evrenine geçer: kadim doku ve Hadalit.

Fikrin gerçek bir karşılığı da var: derin dalış aracı Alvin onlarca yıl boyunca aynı platform
üstünde yeni titanyum küre, yeni safra küreleri ve yeni sintaktik köpük modülleriyle bir üst
derinlik sınıfına taşındı ([18 · Bilim Temeli](18_bilim_temeli.md), Derin Deniz Araçları). Hadal'ın
binaları da böyle evrilir: çekirdek biçim kalır, gövde yenilenir, modüller eklenir.

---

## 2. Altı katman

<!-- URET:evrim-ozet -->
| Seviye | Katman | Tema | Malzeme ve ışık |
| --- | --- | --- | --- |
| 1–9 | Yama | Konvoy enkazından yamanmış ilk kurulum: ne bulunduysa o. Geç açılan binalarda aynı dil "deneme / prototip" okunur. | Uyumsuz gövde levhaları, tehlike turuncusu boya, açıkta kablo ve hortum, iskele; kehribar çalışma lambaları, hafif titrek ışık. |
| 10–19 | Perçin | Saha montajı: standart panel, perçin ve flanşla düzene giren üs. İlk kez "plan" görünür. | Perçinli titanyum paneller, cıvatalı flanşlar, sarı sintaktik köpük blokları; düzenli camgöbeği LED şeritler, sabit ışık. |
| 20–29 | Basınç | Ağır gövde: derinliğin basıncına göre biçimlenen küreler ve silindirler. Yapılar yuvarlaklaşır, ağırlaşır. | Titanyum basınç küreleri, halka takviyeler, konik akrilik basınç pencereleri; soğuk camgöbeği dış ışık, pencerelerden sıcak iç ışık. |
| 30–39 | Alaşım | Süperalaşım çağı: yapılar uzar, çevreyi dinler, bacanın ısısını kullanır. Silüet dikeyleşir. | Nikel-kobalt süperalaşım kaplama (ısıyla mavimsi-bronz), sonar çanakları ve dizileri, ısı eşanjörü kanatları; derin mavi, mor vurgu. |
| 40–49 | Biyomekanik | Kadim uygarlığın dokusuyla kaynaşma: yapı büyür, nefes alır. Mühendislik ile canlı doku arasındaki çizgi silinir. | Kitin kaburgalar, kadim doku, yastık lav bazalt kaideler, Hadalit damarlar; turkuaz biyolüminesans, yavaş nefes ritmi. |
| 50 | Rezonans | Çekirdek'le aynı ritim: bütün üs tek bir kalp gibi atar. Nabız reaktörden halkalara yayılır. | Biyomekanik form, ışık saçan zarlar ve Hadalit taç; beyaz-camgöbeği çekirdek ışığı, mor hale, reaktörden dışa yayılan eşzamanlı nabız. |
<!-- /URET:evrim-ozet -->

### Katman kuralları

<!-- URET:tablo:evrim.katmanlar -->
| Katman | Ad | Seviye | Silüet yüksekliği × | Taban izi (pad) | Üçgen (LOD0, modülsüz) | B sınıfında | Başkalaşım sesi |
| --- | --- | --- | ---: | ---: | ---: | --- | --- |
| I | Yama | 1–9 | 1 | %70 | 2.000 | Mesh (I) | Gevşek metal tıkırtısı, tek kabarcık patlaması |
| II | Perçin | 10–19 | 1,15 | %76 | 2.600 | I + Perçin kiti | Cıvata sıkma tıkırtıları, düzenli uğultuya oturan motor |
| III | Basınç | 20–29 | 1,32 | %82 | 3.200 | Mesh (III) | Gövdenin basınca oturduğu derin gıcırtı, ardından sessizlik |
| IV | Alaşım | 30–39 | 1,52 | %88 | 3.800 | III + Alaşım kiti | Derin sonar vuruşu, uzaklaşan yankı |
| V | Biyomekanik | 40–49 | 1,75 | %94 | 4.400 | Mesh (V) | Yavaş bir nefes, organik gerilme sesi |
| VI | Rezonans | 50 | 2,01 | %100 | 5.000 | V + Rezonans kiti | Çekirdek nabzına akort edilmiş tek, alçak ton |
<!-- /URET:tablo:evrim.katmanlar -->

- **Katman binanın kendi seviyesinden gelir**, reaktörden değil: Sv 50 ise VI, değilse
  1 + ⌊Sv / 10⌋. Hiçbir bina reaktörün seviyesini geçemediği için VI. katmandaki her bina, reaktörün
  de 50 olduğunu söyler.
- **Silüet her katmanda yaklaşık %15 yükselir**; VI. katman I. katmanın
  <!--v:veri.evrim.katmanlar.rezonans.yukseklik-->2,01<!--/v--> katı boydadır. Taban izi binanın
  2,3 m yarıçaplı pedinin içinde kalır (iç halkalar arası 5 m): I. katman pedin
  <!--v:veri.evrim.katmanlar.yama.taban|yuzde-->%70<!--/v-->'ini, VI. katman tamamını kullanır.
  Büyüme yukarı doğrudur, komşu binayı ezmez. 3/4 kamerada boy farkı en iyi okunan farktır.
- **Okuma sırası: uzaktan silüet, ortadan ışık, yakından malzeme.** Her katman üç kanalı birden
  değiştirir; yalnızca boya değiştiren bir katman katman sayılmaz.
- **Işık paleti** kehribardan (Yama) camgöbeğine (Perçin), camgöbeği ve sıcak pencerelere (Basınç),
  mavi-mora (Alaşım), turkuaz nefese (Biyomekanik) ve beyaz-camgöbeği nabza (Rezonans) kayar.
  <!--v:veri.bilim.kurallar.isik_siniri.deger_m-->1.000<!--/v--> m'nin altında güneş ışığı yoktur;
  üssün bütün ışığı ya lambadır ya biyolüminesans. Bu yüzden ışık, katmanı en uzaktan okutan
  kanaldır. Kategori vurgusu (üretim yeşil, askeri kırmızı, ittifak kehribar) her katmanda ikincil
  renk olarak kalır; oyuncu binayı rengiyle tanımaya devam eder.
- **Animasyon temposu katmanla olgunlaşır.** Yama'da tekleme ve sarsıntı, Perçin'de düzen,
  Basınç'ta ağırlık, Alaşım'da tarama, Biyomekanik'te nefes, Rezonans'ta ortak nabız.
- **Malzeme dili gerçek mühendislikle ve gerçek deniz tabanıyla başlar.** Sintaktik köpük (epoksi
  içinde içi boş cam mikroküreler) derin dalış araçlarının kaldırma malzemesidir; tam okyanus
  derinliğine inen araçların mürettebatı kaynaksız titanyum kürelerde oturur; derin deniz
  pencereleri kalın, konik akrilik bloklardır. Biyomekanik katmanın kaidesi okyanus ortası
  sırtlardaki yastık lav bazaltıdır; kaburgaları kabukluların kitin dış iskeletinden esinlenir;
  Alaşım katmanının hammaddesi abisal düzlüklerdeki polimetalik nodüllerdir. Ayrıntı ve kaynaklar:
  [18 · Bilim Temeli](18_bilim_temeli.md) (Mineraller ve Kaynaklar, Derin Deniz Araçları).

### Rezonans nabzı

VI. katmandaki bütün binalar tek bir nabzı paylaşır. Nabız Çekirdek Reaktör'den çıkar ve halka
halka dışa yayılır: her halka bir öncekinden kısa bir gecikmeyle (yaklaşık 0,12 sn) parlar, böylece
üssün üstünden yavaş bir ışık dalgası geçer. Tempo dakikada 40 atış civarındadır, genlik düşüktür:
saniyede 3 parlama eşiğinin çok altında kalır. Ayarlarda **Işık nabzı: Açık / Yumuşak / Kapalı**
seçeneği bulunur. Teknik olarak bina başına iş yoktur: tek bir global gölgelendirici değişkeni (§9).

### Kubbeler

Dört kubbenin beşer seviyesi vardır (K-16) ve her seviye kendi görünümüdür. Kubbe ilk seviyesinde
**Perçin** diliyle kurulur: yamalı bir kubbe basınca dayanmaz. Sonraki seviyeler reaktör kapılarıyla
aynı katmana denk gelir.

| Kubbe Sv | Katman | Reaktör kapısı |
|---:|---|---|
| 1 | II · Perçin | Mega <!--v:veri.binalar.binalar.mega_dome.seviye_esleme.0-->6<!--/v-->, Tarım <!--v:veri.binalar.binalar.agriculture_dome.seviye_esleme.0-->7<!--/v-->, Bilim <!--v:veri.binalar.binalar.science_dome.seviye_esleme.0-->12<!--/v-->, Savaş <!--v:veri.binalar.binalar.war_dome.seviye_esleme.0-->16<!--/v--> |
| 2 | III · Basınç | <!--v:veri.binalar.binalar.mega_dome.seviye_esleme.1-->20<!--/v--> |
| 3 | IV · Alaşım | <!--v:veri.binalar.binalar.mega_dome.seviye_esleme.2-->30<!--/v--> |
| 4 | V · Biyomekanik | <!--v:veri.binalar.binalar.mega_dome.seviye_esleme.3-->40<!--/v--> |
| 5 | VI · Rezonans | <!--v:veri.binalar.binalar.mega_dome.seviye_esleme.4-->45<!--/v--> |

5. seviyedeki kubbe Rezonans biçimini reaktör 50'den önce alır. Reaktör 50 olana kadar kendi
ritmiyle atar; reaktör 50'ye ulaştığında Çekirdek'in nabzına uyar (§4, Reaktör 50 anı). Kubbelere
ek modül takılmaz; seviye sayısı az olduğundan her seviye zaten yeni bir görünümdür.

### Geç açılan binalar

Alaşım Dökümhanesi Reaktör <!--v:veri.binalar.binalar.alloy_foundry.adet.0-->30<!--/v-->'da, Kriyo
Bakım Ünitesi <!--v:veri.binalar.binalar.cryo_ward.adet.0-->16<!--/v-->'da, Kalıntı Arşivi
<!--v:veri.binalar.binalar.relic_archive.adet.0-->15<!--/v-->'te açılır. Bu binalar I. katmanla başlar
ama hızlı yükselir: maliyetleri ve süreleri binanın kendi seviyesine bağlıdır, ilk on seviye kısa
sürede geçer. Yama dili bu binalarda "enkaz" değil **"deneme / prototip"** olarak okunur:
iskele, açık kablo, geçici ışık. Adlar da bunu söyler (ör. Alaşım Dökümhanesi'nin I. katmanı
<!--v:veri.evrim.binalar.alloy_foundry.adlar.0-->Deneme Ocağı<!--/v-->).

### A ve B sınıfı

- **A sınıfı** (kodda olan <!--v:veri.evrim.butce_ozet.a_bina-->15<!--/v--> bina): her katman ayrı,
  elle ya da üreticiyle tasarlanmış bir mesh'tir. Bugünkü Blender tasarımı I. katmanın temelidir;
  sonraki katmanlar onun üstüne kurulur, böylece bina tanınır kalır.
- **B sınıfı** (yeni <!--v:veri.evrim.butce_ozet.b_bina-->19<!--/v--> bina): I, III ve V. katmanlar
  mesh'tir; II, IV ve VI. katmanlar bir önceki mesh'e ortak bir **katman kiti** takılarak elde edilir.
  Kubbelerde Sv 1, 2 ve 4 mesh'tir; Sv 3 Alaşım kitiyle, Sv 5 Rezonans kitiyle gelir.

Üç ortak kit:

| Kit | Ne takar | Ne değiştirir |
|---|---|---|
| Perçin kiti | Perçin şeritleri, flanş halkaları, sintaktik köpük blokları, LED şeritler | Boyayı standart palete çeker |
| Alaşım kiti | Süperalaşım üst plakaları, küçük sonar çanağı, eşanjör kanatları | Gövde malzemesini süperalaşım tonuna çevirir |
| Rezonans kiti | Işık zarları, Hadalit taç, kablo üstü nabız şeritleri | Rezonans nabız maskesini açar (çoğu gölgelendiricide) |

Kitler mesh'teki `SOCKET_kit_*` noktalarına takılır. B sınıfı binalar canlı oyunda öne çıkarsa
(ör. Savaş Kubbesi) sonradan A sınıfına yükseltilebilir.

---

## 3. Ek modüller

Ek modüller iki büyük katman arasındaki ara adımdır: Sv 5, 15, 25, 35 ve 45'te, yani her katmanın
ortasında binaya bir parça takılır. Modül sayısı <!--v:veri.evrim.butce_ozet.modul-->12<!--/v-->,
her biri en çok <!--v:veri.evrim.ek_moduller.isik_seridi.ucgen-->300<!--/v--> üçgendir ve bütün
binalar tarafından paylaşılır. Hangi binanın hangi yuvaya hangi modülü aldığı kategoriden gelir:

<!-- URET:tablo:evrim.modul_matrisi -->
| Kategori | Sv 5 | Sv 15 | Sv 25 | Sv 35 | Sv 45 |
| --- | --- | --- | --- | --- | --- |
| Merkez | Kablo Demeti | Balast Tankları | Basınç Payandası | Isı Eşanjörü | Dron Yuvası |
| Üretim | Kablo Demeti | Balast Tankları | Basınç Payandası | Nodül Haznesi | Biyolüminesan Alg Paneli |
| Depolama | Kablo Demeti | Balast Tankları | Basınç Payandası | Nodül Haznesi | Dron Yuvası |
| Araştırma | Hidrofon Direği | Gözlem Penceresi | Sonar Dizisi | Isı Eşanjörü | Biyolüminesan Alg Paneli |
| Askeri | Hidrofon Direği | Mürettebat İskelesi | Basınç Payandası | Isı Eşanjörü | Dron Yuvası |
| Destek | Işık Şeridi | Mürettebat İskelesi | Sonar Dizisi | Isı Eşanjörü | Biyolüminesan Alg Paneli |
| Anıt | Işık Şeridi | Gözlem Penceresi | Basınç Payandası | Isı Eşanjörü | Biyolüminesan Alg Paneli |
| İttifak | Hidrofon Direği | Mürettebat İskelesi | Sonar Dizisi | Isı Eşanjörü | Dron Yuvası |
| Süs | Işık Şeridi | Balast Tankları | Basınç Payandası | Isı Eşanjörü | Biyolüminesan Alg Paneli |
<!-- /URET:tablo:evrim.modul_matrisi -->

İstisnalar: Radar Merkezi Sv 25'te payanda yerine **Sonar Dizisi**, Hidrotermal Santral Sv 35'te
nodül haznesi yerine **Isı Eşanjörü**, Dron Fabrikası ve Takas İstasyonu Sv 45'te **Dron Yuvası**,
Kronik Feneri Sv 5'te **Hidrofon Direği** alır. Her binanın beş modülü `evrim.json` →
`binalar.<id>.moduller` alanındadır.

<!-- URET:tablo:evrim.ek_moduller -->
| Sv | Modül | Kullanan | Görünüm |
| ---: | --- | --- | --- |
| 5 | Işık Şeridi | Destek, Anıt, Süs; hariç Kronik Feneri | Kenarlar boyunca ince LED şerit; rengi binanın kategori vurgusunu alır. Karanlıkta silüeti okutur. |
| 5 | Kablo Demeti | Merkez, Üretim, Depolama | Zırhlı göbek kabloları reaktör halkasına uzanır; üssün damar ağı ilk kez görünür. Rezonans katmanında nabız bu kablolardan akar. |
| 5 | Hidrofon Direği | Araştırma, Askeri, İttifak; ayrıca Kronik Feneri | Anten yerine hidrofon ve akustik modem direği: suda haberleşme radyo dalgasıyla değil sesle yapılır. |
| 15 | Balast Tankları | Merkez, Üretim, Depolama, Süs | Sarı sintaktik köpük blokları ve küçük balast küreleri; yapının kaldırma dengesini gösterir. |
| 15 | Gözlem Penceresi | Araştırma, Anıt | Kalın, konik akrilik basınç penceresi; içerideki sıcak ışık dışarı taşar. Anıtlarda kaidede küçük bir vitrin. |
| 15 | Mürettebat İskelesi | Askeri, Destek, İttifak | Korkuluklu dış iskele ve kapak; üzerinde yürüyen mürettebat figürleri ve küçük ışıklar. |
| 25 | Basınç Payandası | Merkez, Üretim, Depolama, Askeri, Anıt, Süs; hariç Radar Merkezi | Yapıyı dışarıdan saran titanyum payanda kaburgaları; silüete ağırlık ve güven katar. |
| 25 | Sonar Dizisi | Araştırma, Destek, İttifak; ayrıca Radar Merkezi | Faz dizili sonar panelleri ya da küçük bir çanak; yavaş tarama dönüşü. |
| 35 | Isı Eşanjörü | Merkez, Araştırma, Askeri, Destek, Anıt, İttifak, Süs; ayrıca Hidrotermal Santral | Baca suyunun ısısını alan kanatlı eşanjör; üstünde titreşen sıcak su perdesi (ısı dalgası gölgelendiricisi). |
| 35 | Nodül Haznesi | Üretim, Depolama; hariç Hidrotermal Santral | Polimetalik nodüllerle dolu huni ve kısa konveyör; nodüller yavaşça akar. |
| 45 | Dron Yuvası | Merkez, Depolama, Askeri, İttifak; ayrıca Dron Fabrikası, Takas İstasyonu | Bakım dronu için yanaşma yuvası; dron binanın çevresinde kısa devriyeler atar. |
| 45 | Biyolüminesan Alg Paneli | Üretim, Araştırma, Destek, Anıt, Süs; hariç Dron Fabrikası, Takas İstasyonu | Işıklı kültür panelleri; binaya dokunulduğunda, dalgalanınca parlayan tek hücreli algler gibi bir an parlar. |
<!-- /URET:tablo:evrim.ek_moduller -->

Kurallar:
- **Modüller birikir.** Sv 45'teki bir binada beş modül vardır. Katman değişince modüller kaybolmaz:
  yeni katman mesh'inin `SOCKET_mod_<sv>` noktalarına taşınır ve yeni katmanın paletini alır. Sv 5'te
  gelen hidrofon direği Sv 50'de hâlâ yerindedir, Rezonans ışığıyla.
- **Modül anı küçüktür.** Kamera hareket etmez; modül bir saniyede açılır, tek bir tık sesi ve kısa
  bir ışık. Oyuncu fark eder ama akışı bölünmez.
- **Modül gerçek bir şey söyler.** Hidrofon direği anten yerine durur, çünkü suda haberleşme radyo
  dalgasıyla değil sesle yapılır. Alg paneline dokunulduğunda bir an parlar, dalgalanınca ışıyan
  tek hücreli algler gibi. Nodül haznesi Süperalaşım'ın nereden geldiğini gösterir.

---

## 4. Başkalaşım anı

Bir bina yeni bir katmana geçtiğinde (Sv 10, 20, 30, 40, 50; kubbede her seviye) kısa ve sakin bir
**başkalaşım anı** oynar. İlkeler: en çok 4 saniye, atlanabilir, tekrar izlenebilir (bina kartında
"Başkalaşımı izle"). Havai fişek, konfeti, altın yağmuru, ekran sarsıntısı yok.

| Zaman | Görüntü | Ses | Arayüz |
|---|---|---|---|
| 0,0 sn | Kamera binaya yumuşakça yaklaşır ve hafifçe döner; çevre hafif bulanıklaşır | Müzik alçalır | Üst çubuk solar |
| 0,4 sn | Bir sonar tarama çizgisi binayı aşağıdan yukarı tarar; eski model tarandığı yerden ince bir silt perdesiyle çözülür | Tek sonar vuruşu | — |
| 1,2 sn | Yeni model yukarıdan aşağı belirir; ışık paleti eskisinden yenisine geçer | Katmanın başkalaşım sesi (§2 tablosu) | — |
| 2,4 sn | Deniz tabanında tek bir ışık halkası yayılır ve söner | Alçak bir bas, sonra sessizlik | Alt kart: katman ve biçim adı, bir satır yenilik |
| 4,0 sn | Kamera eski yerine döner | Müzik geri gelir | Kart kendiliğinden kapanır |

Kurallar:
- **Kamera oyuncudan alınmaz.** Oyuncu kaydırıyorsa ya da bir menü açıksa kamera hareket etmez;
  bina yerinde dönüşür ve yalnızca kart çıkar. Bina ekranda değilse kartta "Göster" düğmesi olur.
- **Çevrimdışı biten başkalaşım** oyuncu üsse döndüğünde oynar. Birden fazlası birikmişse tek bir
  sakin sekansta sırayla gösterilir (en çok 3; gerisi yalnızca kart).
- **Dokunmak atlar.** Ayar: **Başkalaşım anı: Tam / Yalnızca dönüşüm / Kapalı.**
- **Haptik:** tek, yumuşak bir titreşim.
- **Ses dili:** [docs/14](../14_Art_and_Sound.md) ile aynı; arcade sesleri yok, sonar vuruşu ana motif.
  Her katmanın kendi başkalaşım sesi vardır (§2 tablosu).
- **Önizleme.** Bir sonraki seviye katman değiştiriyorsa, yükseltme sürerken yeni katmanın silüeti
  bugünkü inşaat hologramı malzemesiyle binanın çevresinde soluk bir hayalet olarak görünür ve
  ilerlemeyle dolar. Bina paneli ([03 · Binalar](03_binalar.md) §6) **evrim önizlemesi** gösterir:
  bir sonraki katmanın adı ve küçük bir silüet görseli. Oyuncu neyin geleceğini bilir; bu da bir
  sonraki yükseltmenin sebebidir.
- **Reaktör 50 anı** tek istisnadır ve biraz daha uzundur (yaklaşık 6 sn): nabız reaktörden başlar,
  halka halka bütün binalara yayılır, 5. seviyedeki kubbeler senkrona girer. Yine havai fişek yok.
  İttifak akışına ve Kronik Feneri'ne tek bir satır düşer: `{üs adı} Rezonans'a ulaştı.`

---

## 5. Adlandırma kuralları

- **İşlev adı değişmez.** "Oksijen Tesisi" menülerde, listelerde ve kodda aynı kalır. Katman adı
  ("Basınçlı Elektroliz Kulesi") binanın **biçim adıdır**: bina kartında alt başlık olarak, başkalaşım
  kartında ve inceleme ekranında görünür.
- **Altı ad bir hikâye anlatır**: hurdadan uyanışa. Okuyan, adın hangi katmana ait olduğunu sözcükten
  anlar.
- **Katman sözcükleri.** I: Yama, Hurda, Geçici, Seyyar, Enkaz, Deneme. II: Perçinli. III: Basınç,
  Basınçlı, Zırhlı. IV: Alaşım. V: Kadim, Kaynaşık, Kitin, Yaşayan. VI daha serbesttir: Uyanmış,
  Rezonans, Nabız, Işıyan, Yankı, Koro gibi.
- **Kısa.** En çok 3 sözcük ve 28 karakter; kart başlığına sığar.
- **Dil bilgisi.** Malzeme sıfatı ek almaz ("çelik kasa" gibi: Alaşım Kasa, Kitin Garnizon); isim
  tamlaması ek alır (Yama Reaktörü, Basınç Ambarı). TDK yazımı: tezgâh, harekât.
- **Gerçek tür adları bina adı olmaz** (K-25'in ruhu). Gerçek mühendislik terimleri olabilir: ay
  havuzu, basınç odası.
- **Başka oyunlardan ad alınmaz**; `veri/yasakli_isimler.json` her çalıştırmada kontrol edilir.
  Oyuncuya dönük metinde "gerçek / kurgu" etiketi kullanılmaz (K-23).
- **Köken adı değiştirmez.** Ad tektir; köken yalnızca kaplamayı değiştirir (§6). Başka dillere
  uyarlama yerelleştirme işidir; çeviri değil, aynı katman sözcüğü mantığıyla uyarlama yapılır.

---

## 6. Kozmetik katmanlar: köken, Derinlik İzni, üs kaplaması

Evrim yalnızca seviyeyle kazanılır; para silüet satın almaz. Kozmetikler palet, ışık, amblem ve
dekor değiştirir ve inceliklidir: yüksek İzin'li bir üs uzaktan "daha gelişmiş" değil, "daha özenli"
görünür.

<!-- URET:tablo:evrim.kancalar -->
| Kanca | Ne değişir | Ne değişmez | Ayrıntı |
| --- | --- | --- | --- |
| Köken kaplaması | Boya paleti, trim rengi, amblem soketi; ışığa hafif köken tonu | Silüet, katman, modüller | Köken (16) |
| Derinlik İzni ışık imzası | Reaktör çekirdeğinde yerinde duran soluk nabız; kubbe tabanında biyolüminesan halka | Silüet, katman, Rezonans dalgası | Derinlik İzni (11) |
| Üs kaplaması | Bütün üste malzeme paleti, trim ve dekor seti (sezon bileti, İzin dükkânı, etkinlik) | Silüet, katman, başkalaşım anı | The Syndicate (12), Derinlik İzni (11) |
| İttifak amblemi | İttifak Merkezi flamaları ve amblem halkası | Silüet, katman | İttifak (10) |
<!-- /URET:tablo:evrim.kancalar -->

- **İzin nabzı ile Rezonans dalgası karışmaz.** Derinlik İzni'nin ışık imzası reaktör çekirdeğinde
  yerinde duran, seyrek ve soluk bir nabızdır (seviyeleri [11 · Derinlik İzni](11_vip.md) bölümünde). Rezonans
  dalgası ise reaktörden çıkıp kablolar ve halkalar boyunca bütün üsse **yayılır** ve yalnızca VI.
  katmanda görülür. Yayılan ışık dalgası kazanılmış evrime ayrılmıştır; hiçbir kozmetik onu taklit
  etmez.
- **Üs kaplaması** (sezon bileti, İzin dükkânı, etkinlik ödülü) bütün üsse bir palet, trim ve dekor
  seti uygular. Kaplama her katmanın kendi malzeme yuvalarına eşlenir; bu yüzden tek bir kaplama altı
  katmanın altısında da çalışmalıdır. Kaplama tasarımı altı katmanlık bir şeritle onaylanır (§10).
- **Köken kaplaması** varsayılan kaplamadır: oyuncu ilk günden kökeninin paletiyle başlar
  ([16 · Kronik, Köken, Profil](16_kronik_kokenler_profil.md)). Üs kaplaması seçilince onun yerini
  alır; amblem soketi kökende kalır.
- **Teknik kanca.** Her katman mesh'inde bir `SOCKET_emblem` noktası; palet değişimi
  `MaterialPropertyBlock` ile (`_OriginTint`, `_AccentTint`); kaplama bir palet seti ve dekor
  listesidir. Dünya haritası maketi de aynı paleti alır.

Bu bölüm yalnızca sınırı koyar: **hiçbir kozmetik silüeti, katmanı ya da başkalaşım anını
değiştirmez**. Ürünler ve fiyatlar [12 · The Syndicate](12_syndicate.md) bölümündedir.

---

## 7. Dünya haritasında üs

Bugün dünya haritasındaki üs maketi reaktör seviyesine göre sabit parçalar ekliyor (Reaktör 3'te
oksijen tesisi, 5'te mega kubbe, 8'de kalkan, 12'de radar) ve 12'den sonra hiç değişmiyor. Öneri:
maket **reaktörün katmanını** gösterir.

<!-- URET:tablo:evrim.dunya_haritasi -->
| Reaktör | Katman | Haritadaki küme | Yükseklik × | Işık |
| --- | --- | --- | ---: | --- |
| 1–2 | I · Yama | Yalnızca Yama Reaktörü; çevrede enkaz yığını | 1 | Kehribar çalışma ışığı |
| 3–9 | I · Yama | + Oksijen Tesisi (3), + Mega Kubbe (6, ilk görünümü); parçalar Yama dilinde | 1 | Kehribar |
| 10–19 | II · Perçin | + Kalkan Jeneratörü, + Radar Merkezi; bütün parçalar Perçin LOD2 | 1,15 | Camgöbeği şeritler |
| 20–29 | III · Basınç | + Askeri Kubbe; Mega Kubbe ikinci görünümüne geçer, parçalar basınç küreli | 1,32 | Camgöbeği, sıcak pencereler |
| 30–39 | IV · Alaşım | + Alaşım Dökümhanesi; reaktörün tepesinde sonar çanağı, kubbe içinde kent | 1,52 | Mavi, mor vurgu |
| 40–49 | V · Biyomekanik | + Bilim ve Savaş Kubbesi; kitin kaburgalar kümeyi sarar | 1,75 | Turkuaz, yavaş nefes |
| 50 | VI · Rezonans | Tam küme; maketin çevresinde yavaş bir nabız halkası | 2,01 | Beyaz-camgöbeği nabız, mor hale |
<!-- /URET:tablo:evrim.dunya_haritasi -->

- **Maket temsilîdir.** Oyuncunun gerçek binalarını değil, reaktör seviyesini ve katmanını gösterir.
  Sunucu zaten reaktör seviyesini gönderiyor; ek veri gerekmez.
- **Kareye sığar.** Harita karesi 8 birimdir; küme yarıçapı kareyi aşmaz. Büyüme yüksekliğe ve ışığa
  gider; taban izi en çok %5 büyür. Komşu üsler birbirine girmez.
- **Uzak zoom'da tek mesh.** Her katman için reaktör ve uydu binaların LOD2'sinden otomatik
  birleştirilmiş bir **üs maketi** (toplam 6 mesh); yakın zoom'da ayrı LOD2 parçaları.
- **Kaplama ve İzin.** Köken ya da üs kaplaması maketin paletine de uygulanır; Derinlik İzni'nin
  harita üstünde görünen nabzı maketin çekirdeğinde yerinde atar. İttifak amblemi ad plakasında kalır.
- **Rezonans üsleri** haritada yavaş bir nabız halkası taşır. Uzaktan "bu üs tamamlanmış" okunur.
  Sis ve sonar altında maketin görünürlüğü [09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md)
  kurallarına uyar.

---

## 8. Mobil performans bütçesi

<!-- URET:tablo:evrim.lod -->
| Düzey | Ne zaman | Üçgen oranı | Not |
| --- | --- | ---: | --- |
| LOD0 | Yakın: bina kartı, başkalaşım anı, yakın zoom | %100 | Katman üçgen bütçesi + modüller |
| LOD1 | Orta: varsayılan üs kamerası | %50 | Modüller tek parçaya birleşir |
| LOD2 | Uzak zoom ve dünya haritası maketi | %15 | Silüet ve ışık kalır, ayrıntı gider |
<!-- /URET:tablo:evrim.lod -->

- **Üçgen.** Katman başına LOD0 bütçesi §2 tablosundadır. En kötü durumda (VI. katman + beş modül)
  bir bina <!--v:veri.evrim.butce_ozet.en_kotu_ucgen-->6.500<!--/v--> üçgendir; bugünkü üretici sınırı
  6.000'dir (`TRIANGLE_BUDGET`). Reaktör 50'de üste
  <!--v:veri.evrim.butce_ozet.ornek_50-->67<!--/v--> bina örneği bulunur; varsayılan kamera LOD1
  kullandığından hepsi aynı anda ekranda olsa bile üst sınır
  <!--v:veri.evrim.butce_ozet.sahne_lod1_50-->217.750<!--/v--> üçgendir. 3/4 kamera bunun bir kısmını
  gösterir.
- **Malzeme.** Doku yerine palet malzemeleri (`HAD_*`), bugünkü gibi. Katman farkı geometri ve
  paletle verilir; isteğe bağlı olarak katman başına 512² bir trim ve emisyon atlası (perçin, damar,
  pencere maskeleri). Yeni palet girdileri: süperalaşım, kitin, bazalt, turkuaz ışık.
- **Çizim çağrısı.** SRP Batcher; aynı türden birden fazla bina (ör. altı oksijen tesisi) GPU
  instancing ile çizilir. Modüller LOD1'de gövdeyle tek parçaya birleşir.
- **Animasyon.** Bugünkü `ArtAnimator` türleri (dönme, nabız, salınım) yeter; yalnızca `breathe`
  (nefes) eklenir. Rezonans nabzı tek global gölgelendirici değişkeniyle çalışır, bina başına CPU
  işi yoktur.
- **Gölge.** Yalnızca LOD0 gölge düşürür; modüller ve kitler gölge düşürmez.
- **Bellek.** Yalnızca üste bulunan katmanlar yüklenir; bir sonraki katman, o katmana geçiren
  yükseltme başladığında önceden yüklenir. Başkalaşım çözülme gölgelendiricisi yalnızca sekans
  süresince kullanılır.
- **İndirme.** İlk kurulumda I–III. katmanlar; IV–VI. katmanlar uzaktan içerik olarak sonradan
  iner. Oyuncu Alaşım katmanına haftalar sonra ulaşır.
- **Hedef.** Orta segment Android'de kilitli 30 FPS, üst segmentte 60 FPS (ROADMAP sunum katmanı
  hedefi). En düşük destekli cihazda VI. katmanlı tam bir üs ölçülmelidir.

---

## 9. Unity uygulama notları

| Değişiklik | Kod |
|---|---|
| Katman hesabı | `shared/HADAL.Shared` içinde `BuildingVisuals.TierFor(id, level)`: Sv 50 → 6, değilse 1 + ⌊Sv / 10⌋; kubbede seviye → `kubbe_katmanlari`. İsteğe bağlı katalog alanı `buildings[].visual.tiers` (varsayılan 1/10/20/30/40/50). Sunucu değişmez: görünüm seviyeden türetilir |
| Prefab seçimi | `BuildingDefinitionSO`: `_prefab` yanına `_tierPrefabs` (6 ya da 5 `AssetReferenceGameObject`); boşsa `_prefab` kullanılır. B sınıfında II/IV/VI = önceki mesh + kit prefab'ı |
| Değişim | `BuildingStateView`: `Instance`'a `Tier` ve `ModuleCount` eklenir; yeniden yaratma koşuluna `instance.Tier != tier` girer; `Destroy` + `Instantiate` yerine `BuildingEvolutionDirector.Play(eski, yeniPrefab)` (çözülme, kart, kamera) |
| Modüller | Prefab'da `SOCKET_mod_5 … SOCKET_mod_45` boş nesneleri; seviye ≥ yuva ise modül prefab'ı sokete takılır ve katman paletini alır |
| Hayalet önizleme | Bugün `MakeHologram` yalnızca ilk inşaatta (seviye 0); katman sınırına yükseltmede bir sonraki katman prefab'ının hologram kopyası eklenir, `Update` içindeki ilerleme mantığı aynen kullanılır |
| Rezonans nabzı | Reaktör `Shader.SetGlobalFloat("_HadalCorePulse", faz)` yazar; malzeme halka gecikmesini binanın `cellRing` değerinden alır. `HadalArtImporter`/`ArtAnimator`: yeni tür `ANIM_breathe_<Hz>__<parça>` |
| Addressables | Adresler `bld/<id>/t<n>`, `bldkit/<kit>`, `bldmod/<modül>`; `AssetCatalogSO` girişleri. Üsteki katmanlar `PreloadAtBoot`, sonraki katman yükseltme başlarken. `MobilePerformanceConfigSO` önbellek sınırı (64) iki katman × 34 bina için yükseltilir ya da bina başına gruplanır |
| Dünya haritası | `WorldMapView.BuildBase`: sabit `Level >= 3/5/8/12` eşikleri yerine `TierFor("core_reactor", b.Level)` ile `bld_maket_t<n>`; yakın zoom'da LOD2 parçaları. `WorldBaseDto.Level` zaten reaktör seviyesidir |
| Bina paneli | Bir sonraki seviye katman değiştiriyorsa evrim önizlemesi (ad + küçük render) |
| Ayarlar | "Başkalaşım anı" ve "Işık nabzı" seçenekleri |

---

## 10. Sanat hattı

### Blender üreticisi

- `art/blender/buildings.py`'deki her bina fonksiyonu bir `tier` parametresi alır. Katmanlar aynı
  çekirdek biçimin üstüne kurulur: oranlar korunur, parçalar eklenir, malzeme değişir. Çıktı
  `Assets/_Hadal/Art/Buildings/<id>_t<n>.fbx`.
- Yükseklik kuralı üreticide ölçülür: `report.json` her katmanın sınır kutusu yüksekliğini yazar;
  ardışık iki katmanın oranı 1,10–1,20 dışındaysa ya da taban 2,3 m pedi aşarsa uyarı verir.
- Soketler boş nesnedir: `SOCKET_mod_<sv>`, `SOCKET_kit_<ad>`, `SOCKET_emblem`. Kitler
  `art/blender/kits.py`, modüller `art/blender/modules.py` dosyasında.
- `generate.py`: `TRIANGLE_BUDGET` katman sözlüğüne döner; `lineup.png` yanında bina başına
  `evrim_<id>.png` (altı katman yan yana) üretilir. Sanat yönetmeni incelemesi bu şerit üzerinden
  yapılır: soldan sağa okunduğunda tek bir binanın büyüdüğü görülmelidir.
- Palet Python ve C# tarafında çifttir (`art/README.md`); yeni malzemeler iki tarafa birden eklenir.

### TRELLIS (Biyomekanik ve Rezonans ayrıntıları)

- Organik parçalar (kitin kaburgalar, kaynaşık doku, bazalt yastıklar, Hadalit taçlar) prosedürel
  kodla zordur. Bunlar `art/gpu/` hattıyla üretilir: SDXL konsept → arka plan ayıklama → TRELLIS →
  Blender temizliği. Çıktı "üst büyüme" parçası olarak V ve VI. katman mesh'lerine eklenir.
- **Süreklilik.** Katman N konsepti, katman N−1 konseptinden img2img ile (güç 0,45–0,6) üretilir ve
  tohum bina başına sabittir. Altı görünüm altı ayrı bina değil, aynı binanın evrimi olarak okunur.
- TRELLIS'in mesh çözücüsü 8 GB'ta yoğun yüzeylerde zorlanır (`art/gpu/assets.json` notu): konseptte
  silüet sade tutulur, ince ayrıntı Blender'da eklenir.
- Her çıktı katmanın üçgen bütçesine indirilir, `HAD_*` paletine yeniden malzemelenir, soketleri
  eklenir. Lisansı küresel dağıtıma uygun olmayan modellerin çıktısı gönderilmez
  (`art/THIRD_PARTY.md`).

### İstem rehberi

Roster stili ve olumsuz istem (`art/gpu/assets.json` → `style`, `negative`) her isteme eklenir.
İstemin başına binanın işlevi ("oxygen electrolysis plant with three tanks"), ortasına katman
anahtarları, sonuna roster stili gelir.

<!-- URET:tablo:evrim.istemler -->
| Katman | SDXL istem anahtarları (İngilizce) |
| --- | --- |
| I · Yama | makeshift outpost built from salvaged convoy parts, mismatched hull plates, hazard orange paint, exposed cables and hoses, scaffolding, amber work lamps |
| II · Perçin | riveted titanium panels, bolted flanges, yellow syntactic foam blocks, tidy cyan LED strips, orderly field assembly |
| III · Basınç | heavy titanium pressure spheres, thick conical acrylic viewports, ring stiffeners, rounded massive hull, warm light inside |
| IV · Alaşım | dark bronze superalloy plating with heat-blued tint, sonar dishes, finned heat exchangers, tall vertical silhouette, violet accent lights |
| V · Biyomekanik | biomechanical, ancient chitin ribs grown over a metal hull, pillow basalt foundation, glowing teal veins, violet crystal inlays |
| VI · Rezonans | radiant bioluminescent pulse, translucent glowing membranes, crystal crown, cyan-white core light with violet halo |
<!-- /URET:tablo:evrim.istemler -->

İstemlere başka oyunların ya da markaların adı yazılmaz; insan figürü, yazı, logo, su yüzeyi
istenmez; tek nesne, 3/4 ön görünüm, açık gri fon.

---

## 11. Sanat bütçesi

<!-- URET:tablo:evrim.butce -->
| Sınıf | Kapsam | Bina | Görünüm / bina | Mesh | Kit | Modül | Not |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| A | Kodda olan binalar (kubbe dışı) | 13 | 6 | 78 | — | — | Her katman ayrı mesh; bugünkü Blender tasarımı I. katmanın temeli |
| A | Kodda olan kubbeler | 2 | 5 | 10 | — | — | Her kubbe seviyesi ayrı mesh |
| B | Yeni binalar (kubbe dışı) | 17 | 6 | 51 | — | — | I, III, V mesh; II, IV, VI ortak kitle |
| B | Yeni kubbeler | 2 | 5 | 6 | — | — | Sv 1, 2, 4 mesh; Sv 3 Alaşım kiti, Sv 5 Rezonans kiti |
| Ortak | Katman kitleri (Perçin, Alaşım, Rezonans) | 19 | — | — | 3 | — | Soketlere takılan parça setleri ve malzeme takası |
| Ortak | Ek modüller (Sv 5, 15, 25, 35, 45) | 30 | — | — | — | 12 | Kubbelerde modül yok |
| Toplam | Bina sanatı | 34 | — | 145 | 3 | 12 | Elle ya da üreticiyle yapılan parçalar |
| Ortak | Dünya haritası üs maketi | — | 6 | 6 | — | — | Reaktör ve uydu binaların LOD2'sinden otomatik birleştirme |
| Genel toplam | Bina sanatı + harita maketi | 34 | — | 151 | 3 | 12 | LOD1 ve LOD2 otomatik; sayıya girmez |
<!-- /URET:tablo:evrim.butce -->

Bina sanatı toplamı <!--v:veri.evrim.butce_ozet.mesh-->145<!--/v--> mesh,
<!--v:veri.evrim.butce_ozet.kit-->3<!--/v--> katman kiti ve
<!--v:veri.evrim.butce_ozet.modul-->12<!--/v--> ek modüldür. Dünya haritası maketleriyle
<!--v:veri.evrim.butce_ozet.genel_mesh-->151<!--/v--> mesh; LOD1 ve LOD2 ile birlikte
<!--v:veri.evrim.butce_ozet.lod_dosyasi-->453<!--/v--> mesh dosyası. LOD'lar otomatik üretilir.

Üretim sırası:
1. Çekirdek Reaktör'ün altı katmanı (vitrin; Google Play sayfasında ve tanıtım videolarında da kullanılır).
2. A sınıfının I–III. katmanları (oyuncunun ilk ayı) ve Perçin kiti.
3. Ek modüller.
4. B sınıfının I ve III. katman mesh'leri.
5. IV–VI. katmanlar, Alaşım ve Rezonans kitleri, dünya haritası maketleri.

---

## 12. Bütün binalar, bütün adlar

Her binanın altı görünümünün ayrıntılı tarifi kendi sayfasındaki **Görünüm evrimi** tablosundadır
(ör. [Çekirdek Reaktör](binalar/core_reactor.md), [Oksijen Tesisi](binalar/oxygen_plant.md)).
Kubbelerin I. sütunu boştur: kubbenin 1. seviyesi Perçin sütunundadır.

<!-- URET:tablo:evrim.ozet -->
| Bina | Sınıf | I · Yama | II · Perçin | III · Basınç | IV · Alaşım | V · Biyomekanik | VI · Rezonans |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [Çekirdek Reaktör](binalar/core_reactor.md) | A | Yama Reaktörü | Perçinli Reaktör | Basınç Çekirdeği | Alaşım Çekirdek Kulesi | Kadim Kalp | Uyanmış Çekirdek |
| [Oksijen Tesisi](binalar/oxygen_plant.md) | A | Yama Elektroliz Tankı | Perçinli Oksijen İstasyonu | Basınçlı Elektroliz Kulesi | Alaşım Ayrıştırma Kulesi | Kadim Solungaç | Soluyan Kule |
| [Gıda Laboratuvarı](binalar/food_lab.md) | A | Geçici Kültür Odası | Perçinli Gıda Laboratuvarı | Basınçlı Kültür Hücresi | Alaşım Biyoreaktör | Yaşayan Mayalık | Bereket Nabzı |
| [Biyo Laboratuvar](binalar/bio_lab.md) | A | Yama Biyo Tankları | Perçinli Biyo Laboratuvar | Basınçlı Biyo Küreler | Alaşım Fermantasyon Hattı | Kaynaşık Doku Fıçıları | Uyanmış Doku Bahçesi |
| [Hidroponik Çiftlik](binalar/hydroponic_farm.md) | A | Hurda Sera Rafları | Perçinli Hidroponik Çiftlik | Basınçlı Sera Sütunları | Alaşım Dikey Tarla | Kadim Kök Ağı | Işıyan Hasat Kulesi |
| [Dron Fabrikası](binalar/drone_factory.md) | A | Hurda Dron Atölyesi | Perçinli Dron Fabrikası | Basınçlı Dron Hangarı | Alaşım Montaj Hattı | Kaynaşık Dron Kovanı | Sürü Korosu |
| [Araştırma Merkezi](binalar/research_center.md) | A | Enkaz Laboratuvarı | Perçinli Araştırma İstasyonu | Basınç Gözlemevi | Alaşım Gözlem Kulesi | Kadim Bilgi Kulesi | Uyanmış Akıl Kulesi |
| [Denizaltı İskelesi](binalar/submarine_dock.md) | A | Yama Kızağı | Perçinli Kuru Havuz | Basınç Tersanesi | Alaşım Gövde Tezgâhı | Kadim Ay Havuzu | Rezonans Limanı |
| [Radar Merkezi](binalar/radar_center.md) | A | Seyyar Dinleme Direği | Perçinli Sonar İstasyonu | Basınçlı Sonar Kulesi | Alaşım Dinleme Çanağı | Kadim Kulak | Yankı Tacı |
| [Askeri Kubbe](binalar/military_dome.md) | A | Yama Kışlası | Perçinli Kışla Kubbesi | Zırhlı Basınç Kışlası | Alaşım Garnizon | Kitin Garnizon | Nabız Kalesi |
| [Kalkan Jeneratörü](binalar/shield_generator.md) | A | Hurda Kalkan Bobini | Perçinli Kalkan Jeneratörü | Basınç Kalkanı Pilonu | Alaşım Alan Kulesi | Yaşayan Zar Pilonu | Rezonans Kalkanı |
| [Mega Kubbe](binalar/mega_dome.md) | A | — | Perçinli Yaşam Kubbesi | Basınç Kubbesi | Alaşım Kubbe Kenti | Kadim Kubbe Kenti | Rezonans Kenti |
| [Tarım Kubbesi](binalar/agriculture_dome.md) | A | — | Perçinli Tarım Kubbesi | Basınçlı Teras Kubbesi | Alaşım Hasat Kubbesi | Kaynaşık Bahçe Kubbesi | Işıyan Bahçe |
| [İttifak Merkezi](binalar/alliance_hub.md) | A | Geçici Bağlantı İstasyonu | Perçinli İttifak Merkezi | Basınç Buluşma Salonu | Alaşım Konsey Kulesi | Kadim Meclis | Yankı Meclisi |
| [Süs Kulesi](binalar/decoration_spire.md) | A | Yama Işık Direği | Perçinli Süs Kulesi | Basınç Işık Kulesi | Alaşım Işık İğnesi | Kadim Işık Dalı | Nabız Sütunu |
| [Kriyo Salonu](binalar/cryo_hall.md) | B | Yama Kapsül Odası | Perçinli Kriyo Salonu | Basınç Uyandırma Salonu | Alaşım Kriyo Galerisi | Kitin Uyku Galerisi | Uyanış Salonu |
| [Basınç Ambarı](binalar/pressure_vault.md) | B | Yama Sandık Yığını | Perçinli Ambar | Basınç Ambarı | Alaşım Kasa | Kadim Mahzen | Rezonans Mahzeni |
| [Mühendislik Bölmesi](binalar/engineering_bay.md) | B | Seyyar Alet Odası | Perçinli Atölye | Basınç Bakım Bölmesi | Alaşım Mühendislik Bölmesi | Kaynaşık Ustahane | Uyanmış Ustahane |
| [Denizaltı Hangarı](binalar/submarine_hangar.md) | B | Yama Bağlama Rıhtımı | Perçinli Hangar | Basınç Hangarı | Alaşım Sefer Hangarı | Kitin Sefer Yuvası | Işıyan Sefer Kapısı |
| [Tatbikat Havuzu](binalar/training_grounds.md) | B | Geçici Tatbikat Çukuru | Perçinli Tatbikat Havuzu | Basınç Odaları | Alaşım Talim Havzası | Kitin Talim Havuzu | Nabız Havuzu |
| [Mürettebat Yatakhanesi](binalar/crew_quarters.md) | B | Yama Ranza Bölmesi | Perçinli Yatakhane | Basınç Yaşam Bloğu | Alaşım Konut Kulesi | Kadim Konaklar | Işıyan Mahalle |
| [Revir](binalar/med_bay.md) | B | Seyyar Revir | Perçinli Revir Bölmesi | Basınçlı Tedavi Bölmesi | Alaşım Şifa Kulesi | Yaşayan Şifahane | Rezonans Şifahanesi |
| [Kriyo Bakım Ünitesi](binalar/cryo_ward.md) | B | Yama Soğuk Odası | Perçinli Kriyo Ünitesi | Basınçlı Kriyo Dizisi | Alaşım Kriyo Kulesi | Kitin Kozalar | Ninni Kozaları |
| [Hidrotermal Santral](binalar/hydrothermal_plant.md) | B | Yama Isı Hattı | Perçinli Baca Santrali | Basınç Türbin Evi | Alaşım Isı Kulesi | Kaynaşık Baca | Rezonans Bacası |
| [Takas İstasyonu](binalar/trade_station.md) | B | Hurda Takas İskelesi | Perçinli Takas İstasyonu | Basınç Aktarma Rıhtımı | Alaşım Kervan Limanı | Kadim Kervansaray | Yankı Kervansarayı |
| [Bilim Kubbesi](binalar/science_dome.md) | B | — | Perçinli Bilim Kubbesi | Basınç Laboratuvar Kubbesi | Alaşım Deney Kubbesi | Kadim Bilgelik Kubbesi | Işıyan Bilgelik Kubbesi |
| [Savaş Kubbesi](binalar/war_dome.md) | B | — | Perçinli Komuta Kubbesi | Basınçlı Harekât Kubbesi | Alaşım Savaş Kubbesi | Kitin Harp Kubbesi | Uyanmış Harp Kubbesi |
| [Teçhizat Atölyesi](binalar/gear_workshop.md) | B | Seyyar Demirci Tezgâhı | Perçinli Teçhizat Atölyesi | Basınç Dövmehanesi | Alaşım Zırh Atölyesi | Kadim Örs | Rezonans Örsü |
| [Dalgıç Anıtı](binalar/armor_monument.md) | B | Yama Kask Kaidesi | Perçinli Dalgıç Anıtı | Basınç Zırhı Abidesi | Alaşım Dalgıç Heykeli | Kitin Dalgıç | Uyanmış Dalgıç |
| [Zıpkın Anıtı](binalar/harpoon_monument.md) | B | Yama Zıpkın Sehpası | Perçinli Zıpkın Anıtı | Basınç Zıpkını Abidesi | Alaşım Avcı Heykeli | Kadim Avcı | Uyanmış Avcı |
| [Ark Anıtı](binalar/arc_monument.md) | B | Yama Ark Bobini | Perçinli Ark Anıtı | Basınç Arkı Abidesi | Alaşım Ark Kulesi | Kaynaşık Ark Ağacı | Uyanmış Ark |
| [Kalıntı Arşivi](binalar/relic_archive.md) | B | Geçici Buluntu Rafları | Perçinli Kalıntı Arşivi | Basınç Sergi Salonu | Alaşım Arşiv Kulesi | Kadim Hafıza | Yankı Hafızası |
| [Alaşım Dökümhanesi](binalar/alloy_foundry.md) | B | Deneme Ocağı | Perçinli Döküm Ocağı | Basınç Potası | Alaşım Dökümhanesi | Kaynaşık Pota | Rezonans Dökümhanesi |
| [Kronik Feneri](binalar/chronicle_beacon.md) | B | Enkaz Yayın Direği | Perçinli Kronik Feneri | Basınç Feneri | Alaşım Yayın Kulesi | Kadim Fener | Kronik Korosu |
<!-- /URET:tablo:evrim.ozet -->

---

## 13. Bugün kodda

| Konu | Durum | Bugün |
|---|---|---|
| Bina başına görünüm | ⬜ | Her binanın tek prefab'ı (`BuildingDefinitionSO._prefab`), her seviyede aynı |
| İlk inşaat hologramı | ✅ | `BuildingStateView.MakeHologram`: seviye 0'da yerden yükselen hologram |
| Katman hesabı ve prefab değişimi | ⬜ | — |
| Ek modüller | ⬜ | — |
| Başkalaşım anı ve evrim önizlemesi | ⬜ | — |
| Dünya haritası üs maketi | 🟡 | Reaktör 3/5/8/12 eşikleriyle sabit parçalar; katman yok |
| Ambiyans animasyonu | ✅ | `ArtAnimator`: dönme, nabız, salınım |
| Prosedürel bina üreticisi | 🟡 | 15 bina × 1 tasarım; 6.000 üçgen sınırı; bütün LOD'lar tek mesh |
| TRELLIS hattı | 🟡 | Dekor, canlı ve denizaltılar için çalışıyor; binalarda kullanılmıyor |
| Addressables | 🟡 | `AssetCatalogSO` var; bina prefab'ları doğrudan referansla yükleniyor |

---

## 14. Açık sorular

- **Kubbe tabloları.** Üretilen bina sayfalarındaki "Görünüm evrimi" tablosu adları katman sırasıyla
  eşliyor; kubbelerde 1. seviye Perçin'dir. Üretici `binalar.<id>.seviyeler` ve
  `binalar.<id>.katmanlar` alanlarını okumalı (kılavuz aracı işi).
- **Geç açılan binalar** için bir taban kuralı gerekli mi? Örneğin görünüm katmanı =
  en büyük(seviye katmanı, reaktör katmanı − 2). Bugünkü öneri: hayır; bu binalar hızlı yükselir ve
  Yama dili "prototip" okunur.
- **Köken kaplamaları silüete dokunsun mu?** Örneğin Derindoğanlar'da biraz daha organik bir
  Biyomekanik. Bugünkü sınır: hayır (köken bölümüyle birlikte karar).
- **Üs kaplamaları güç ya da bonus verir mi?** Öneri: vermez; yalnızca görünüm (The Syndicate
  bölümüyle birlikte karar).
- **Başkalaşım anı başkalarının üssünde** (ziyaret, ittifak üyesi) gösterilsin mi, yoksa yalnızca
  ittifak akışında tek satır mı?
- **B sınıfından A sınıfına** hangi binalar ve ne zaman yükseltilir? Aday: Savaş Kubbesi, Kronik
  Feneri, Alaşım Dökümhanesi.
- **`ozet` listesi** `binalar` alanından türetilmiştir; üretici bu tabloyu doğrudan `binalar`'dan
  basabilirse liste kaldırılır.
- **Erişilebilirlik:** "Işık nabzı" varsayılanı Açık mı, Yumuşak mı?
