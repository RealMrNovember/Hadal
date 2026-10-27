# 19 · Ordu, Sefer ve Geçişler: Uygulama Belgesi

> **Özet.** Bu bölüm kural koymaz; kurallar [07](07_birlikler_ve_savas.md), [08](08_denizaltilar_basinc_seferler.md)
> ve [09](09_dunya_haritasi_ve_pvp.md)'dadır. Burada o kuralların **oyunda nasıl göründüğü ve nasıl
> akacağı** yazılıdır: asker eğitimi ve Revir ekranları, takımlar (denizaltı + kahraman + asker), sefer
> hazırlığı, yolda hızlandırma ve geri çağırma, sol üstteki takım kuyruğu, açılış ve üs ↔ dünya
> geçişleri. Kaynak: 27 Ekim 2026'da Last Asylum: Plague (37Games, `com.phs.global`) içinde yapılan
> gerçek işlemler (10 asker eğitildi, bir toplama seferi gönderilip geri çağrıldı) ve türün
> oyunlarından derlenen araştırma. Sıra ve paketler en sondadır ([7](#7-uygulama-sırası)).

Durum: ⬜. Kodda bugün yalnızca ordusuz toplama yürüyüşü vardır (komutan kahraman + temsili dalgıç
ekibi, catalog `gather`); bu belge onun yerini alacak düzeni tanımlar.

---

## 1. Gözlem: Last Asylum'da akış

Değerler o hesabın değerleridir (Sv 23 şehir, VIP 8); oranlar ve akış önemlidir, sayılar değil.
Asker başına süre, kademe, araştırma, VIP, kahraman ve bina seviyelerinin toplam bonusuyla
belirlenir; her şey birbirine orantılıdır (bizde K-13 böleni).

| Ekran | Ne gördük | Hadal karşılığı |
|---|---|---|
| Açılış | Platform karşılaması ("Hi there, MikeRoss91") → kapak çizimi + logo + yükleme çubuğu, üstte "Welcome back" şeridi → şehir yumuşak açılır | [6.1](#61-açılış-ve-yükleme) |
| Binaya dokunma | Kamera binaya kayar, HUD gizlenir, "Sv.14 Revir" etiketi ve binanın etrafında yuvarlak eylemler (Ayrıntılar, Yükselt, İyileştir, …) | Kodda (TargetMenu) |
| Askeri bölge | 4 **Eğitim Alanı** çadırı: her biri **bir asker türünü** eğitir ve ayrı bir kuyruktur, kademeleri o alanın seviyesi açar (birinde D7 açık, diğerinde D6 kilitli); boştakinin üstünde miğfer balonu (çağrı), eğitimdekinin üstünde geri sayım. 2 **Kışla** (taş kale, asker kapasitesi), 3 **Revir** | [2.1](#21-askeri-kubbe-ve-eğitim-kuyrukları) |
| Eğitim Alanı | Kademe portreleri (D5–D9, eldeki sayılarla, kilitliler kilitli), "Temel Özellikler" (Savaş Gücü, Birlik Yükü, Moral, SAL, SAV, SP), birim bedeli × adet, kaydırıcı + adet kutusu, süre, **Şimdi Eğit** (elmas) / **Eğit**; üstteki ok terfi | [2.2](#22-eğitim-ekranı) |
| Eğitim sonrası | Pencere kapanır, kamera kışlaya kayar, kışla üstünde sayaç | [2.2](#22-eğitim-ekranı) |
| Revir | "Ağır Yaralı Askerler 0/4.792" (tüm Revirlerin toplam kapasitesi), iyileşme kuyruğu ("İyileşiyor 00:05:33 · 25") + Hızlandır; Ayrıntılar'da seviye → kapasite tablosu | [3](#3-revir) |
| Büyüteç | Dünyada **canavar** bulup saldırmak için (tür + seviye, ilk öldürme ödülü); ayrıca "Topla" sekmesiyle yatak arama (tür + seviye, verim) | [4.6](#46-arama) |
| Yatak balonu | "Sv.8 Çiftlik", kalan kaynak çubuğu, "Şu anda kimse toplanmıyor", tek eylem **Topla** | Kodda |
| Sefer hazırlığı | Toplama miktarı (yük / yatak), toplanma süresi, tahmini bitiş saati; takımın 5 kahramanı ve her birinin altında asker sayısı; takım kapasite çubuğu (1573/1573); yol süresi; **Ayrıl** + otomatik doldurma ayarı. Altta **takım seçici** (1–4, boşta olan "Zz"), en altta dayanıklılık, boşta/toplam asker, Revir doluluğu | [4.2](#42-sefer-hazırlığı) |
| Yolda | Sol üstte **"Takım 1/3"** kuyruğu: portre, durum (Taşınıyor / Toplanıyor / Dönülüyor), ilerleme, süre, durumuna göre ⏩ Hızlandır ya da ↩ Geri çağır; altında "+ Yeni Takım". Haritada noktalı rota ve yürüyen birlik + isim levhası | [4.4](#44-yolda-ve-kuyruk) |
| Hızlandır | Hedef koordinatı, kalan süre çubuğu; **%50** ve **%25** "kalan yürüyüş süresini azaltır" eşyaları, yoksa elmasla "Satın al ve kullan" | [4.5](#45-hızlandırma-ve-geri-çağırma) |
| Varış ve toplama | Balonda canlı artan "Toplanan", kalan süre, "Toplayıcı: oyuncu"; eylem **Geri Çağır** | Kodda (kısmen) |
| Geri çağırma | Ücretsiz ve anında; dönüş süresi gidişe eşit, dönüş de hızlandırılabilir | [4.5](#45-hızlandırma-ve-geri-çağırma) |
| Başka oyuncunun üssü | Balonda isim, güç, ittifak, Beğen; eylemler **Saldırı, Seferberlik (ralli), Keşif** | [4.3](#43-sefer-türleri) |

### 1.1 Denenen sayılar (kademe ölçeği)

Aynı hesapta, bonuslar sabitken (asker başına):

| Kademe | Güç | Yük | Bedel (tahıl + odun) | Süre |
|---|---:|---:|---:|---:|
| D4 | 210 | 1.000 | 903 + 903 | ~85 sn |
| D5 | 412 | 1.200 | 1.744 + 1.744 | ~103 sn |
| D7 | 781 | 1.600 | 3.320 + 3.320 | ~137 sn |

Kademe başına güç ve bedel ~**1,9 katı**, süre yalnızca ~**1,2 katı**, yük +%10–20 büyür. Parti üst
sınırı eğitim alanından gelir (bir alanda 512, ötekinde 596). Bizim 07 §3.1 eğrileri aynı biçimdedir
(güç ve bedel hızlı, süre yavaş büyür); sayılar rehberin veri dosyalarından gelir. Toplama seferinde
5 kahramanlı takımın kapasitesi 1.573 askerdi; 1,35M'lik yatağın tamamını (1,4M yük) taşıyordu, toplama
süresi 4 sa 6 dk, yol 50 sn idi.

## 2. Askeri Kubbe, Tatbikat Havuzu ve eğitim

### 2.1 Askeri Kubbe ve eğitim kuyrukları

Kurallar [07 §2.4](07_birlikler_ve_savas.md#24-eğitim-terfi-terhis)'tedir. Görünüm:

- Last Asylum'da tür başına bir Eğitim Alanı (kuyruk) ve ayrı Kışla (kapasite) vardır. Rehberde
  Askeri Kubbe tek binadır, **kuyruk sayısı** seviyeyle artar (Sv 1: 1 · 10: 2 · 25: 3 · 40: 4) ve
  ordu kapasitesini taşır; Tatbikat Havuzu kademe ve parti verir. Görünüm Last Asylum'unki gibi
  olur: kubbenin çevresinde kuyruk başına bir **eğitim bölmesi** modeli, boştakinin üstünde miğfer
  balonu, çalışanın üstünde geri sayım. (Açık soru: tür başına ayrı bina mı, kubbe bölmesi mi —
  [9](#9-açık-sorular).)
- Bölme balonu doğrudan Eğitim ekranını açar (bina menüsüne gerek kalmadan).
- HUD sol üst kuyruğunda eğitim satırları da görünür (inşaat, araştırma ve seferle aynı liste).

### 2.2 Eğitim ekranı

Last Asylum'un "Eğitim Alanı" düzeni, dört türlü olarak:

1. Üstte **tür sekmeleri** (Zırhlı Dalgıç · Zıpkıncı · Ark Askeri · Delici).
2. Altında **kademe şeridi** T1–T10: altıgen portre, eldeki sayı; açılmamış kademe kilitli (hangi
   Tatbikat Havuzu seviyesi / araştırma açar, dokununca söyler); terfi edilebilir kademede yeşil ok.
3. **Temel özellikler** kartı: Güç, Yük, Hız, ATK, DEF, HP ([07 §3.1](07_birlikler_ve_savas.md#31-tür-istatistikleri)).
4. **Bedel satırı**: birim bedeli × adet (sahip/gereken, yetersiz kırmızı), bakım (saatlik Gıda/O₂).
5. **Adet**: − kaydırıcı + ve dokununca yazılan adet kutusu; üst sınır = parti boyutu (Tatbikat
   Havuzu) ve boş ordu kapasitesi.
6. **Süre**: parti süresi = adet × birim süre ÷ (1 + Σhız%/100) (K-13).
7. Düğmeler: **Eğit** (kaynakla) · **Hemen Eğit** (Hadalit, bedel kalan süreye göre, K-12) · kademe
   seçiliyse **Terfi** (fark bedeli ve fark süresi).
8. Eğitim başlayınca pencere kapanır, kamera eğitim bölmesine kayar, bölmenin üstünde sayaç belirir;
   bitince "Topla" balonu (askerler ordu sayısına dokunuşla eklenir, güç sayacı tıklar).

### 2.3 Ordu görünümü

Askeri Kubbe → Ayrıntılar: türe ve kademeye göre asker tablosu, toplam ordu / kapasite, seferdeki,
eğitimdeki, Revir'deki, Kriyo'daki asker; saatlik bakım. Terhis buradan (iki adımlı onay).

## 3. Revir

Kurallar [07 §2.6–2.7](07_birlikler_ve_savas.md#26-yaralı-ölü-kriyo-ve-kurtarma-kapsülü)'dedir.

- Revir ekranı: üstte **"Ağır yaralılar X / kapasite"** (iki Revir toplamı), ardından türe ve kademeye
  göre yaralı listesi (her satırda adet seçici), toplam iyileştirme bedeli ve süresi; **İyileştir** /
  **Hemen İyileştir** / **İttifak Yardımı İste**.
- Tek iyileştirme kuyruğu; satırı "İyileşiyor 00:05:33 · 25" + Hızlandır.
- Revir dolarken HUD'da kırmızı uyarı ve sefer hazırlığında Revir doluluğu (Last Asylum'daki
  "25/4.792") görünür; dolu Revir'le saldırıya çıkarken onay sorulur ("taşan yaralılar ölecek").
- PvE kayıpları: Revir doluysa askerler ölmez, Askeri Kubbe'de dinlenir (8 saatte ücretsiz).

## 4. Seferler

### 4.1 Takım: denizaltı + kahramanlar + askerler

Askerler haritaya **denizaltıyla** gider; araç ayrı bir sistem değil, takımın kendisidir
([08 §2](08_denizaltilar_basinc_seferler.md#2-denizaltılar)):

| Parça | Ne verir | Kaynağı |
|---|---|---|
| Hangar (her biri bir takım) | Takım sayısı (R4/10/18/26) ve **asker kapasitesi** (Sv 1: 100 … Sv 50: 24.000) | 08 §2.1 |
| Gövde | Rol ve **hız çarpanı** (Köpekbalığı 1,0, Mürekkepbalığı 1,3, Mors yavaş/dayanıklı, Fenerbalığı keşif…), basınç sınıfı | 08 §2.2–3 |
| Kahramanlar | Komutan + yardımcılar: yetenekler, tür bonusları, sefer kapasitesine katkı | 05, 08 §2.3 |
| Askerler | Savaş değerleri ve **yük** (tür × kademe), seferin hızı en yavaş türün hızı | 07 §2.1 |

- Takımlar **kalıcı ön ayarlardır** (Last Asylum "Takım 1–4"): oyuncu bir kez kahramanları ve asker
  dağılımını kurar, her seferde takımı seçer; "otomatik doldur" en yüksek kademeden başlayarak
  kapasiteyi doldurur.
- **Takviye ve ittifak desteği** de denizaltıyla gider: takviye takımı müttefikin üssüne demirler ve
  geri çağrılana kadar kalır; nakil (kaynak taşıma) Delici ağırlıklı yük denizaltısıyla, vergili ve
  günlük sınırlı.
- **Saldırı farklı gider**: savaş takımı, yük değil güç ve tür dengesi önemlidir; hedefe vardığında
  savaş çözülür (07 §4), kayıplar 07 §2.6 kurallarıyla Revir'e ya da ölüme; kazanırsa yağma yüke
  girer ve takım eve döner. Saldırı Sonar Enerjisi (PvE) ya da kalkan kuralları (PvP) ister.

### 4.2 Sefer hazırlığı

Yatak, canavar, üs ya da müttefik seçildiğinde açılır (Last Asylum düzeni):

1. Üstte hedefe göre özet: toplamada **toplama miktarı (yük / yatak), toplama süresi, tahmini bitiş
   saati**; saldırıda önerilen güç ve kendi gücün; takviyede hedefin boş garnizon yeri.
2. Ortada takımın kahramanları; her birinin altında asker sayısı; **kapasite çubuğu** (dolu / takım
   kapasitesi). Dokunarak asker dağılımı düzenlenir.
3. **Yol süresi** (saat simgesi) ve mesafe (kare; Hadal'da "km" yerine kare ya da deniz mili).
4. **Ayrıl** + otomatik doldurma ayarı.
5. En altta takım seçici (1–4; boştaki "Zz", dışarıdaki süre ile) ve ordu durumu (Sonar Enerjisi,
   boşta/toplam asker, Revir doluluğu).

### 4.3 Sefer türleri

| Tür | Gittiği yer | Ne olur | Dönüş | Kural |
|---|---|---|---|---|
| Toplama | Yatak | Yük dolana / yatak bitene kadar toplar | Kendiliğinden döner | 09 §4.7 |
| Saldırı (PvE) | Canavar, Leviathan | Savaş; kayıplar yaralı (ölüm yok) | Kendiliğinden döner | 09 §5, 07 §2.6 |
| Saldırı (PvP) | Üs, toplayan takım | Savaş, yağma, kayıplar 70/30 | Kendiliğinden döner | 09 §10 |
| Ralli | İttifak hedefi | Toplanma süresi (5/10/30/60 dk), katılanlar birlikte yürür | Kendiliğinden döner | 09 §8.2 |
| Takviye | Müttefik üssü | Garnizon olarak kalır, savunmaya katılır | Geri çağrılınca | 09 §8.3 |
| Nakil | Müttefik üssü | Kaynak bırakır (vergi, günlük sınır) | Kendiliğinden döner | 10 |
| Keşif | Herhangi bir kare | Asker gitmez; sonar dronu rapor getirir | — | 09 §8.4 |

### 4.4 Yolda ve kuyruk

- Haritada **noktalı rota** üs → hedef; üstünde ilerleyen **denizaltı modeli** ve levha
  ("Kael · 1.573 asker"); dönüşte rota tersine döner.
- **Sol üst "Takım n/m" kuyruğu** (üsteki inşaat kuyruğuyla aynı yer ve mantık): satır başına komutan
  portresi, durum (Yolda / Topluyor / Savaşıyor / Garnizonda / Dönüyor), ilerleme çubuğu, süre ve
  duruma göre ⏩ Hızlandır ya da ↩ Geri çağır; boş kuyruk varsa "+ Yeni sefer".
- Satıra dokunmak kamerayı takıma götürür; takıma dokunmak bilgi balonu + Hızlandır / Geri çağır.
- Yatak balonu toplanırken canlı "Toplanan X / Y", kalan süre ve toplayıcının adı.

### 4.5 Hızlandırma ve geri çağırma

- **Süre**: kare × 60 sn ÷ (en yavaş tür hızı / 100 × gövde çarpanı × (1 + Σhız%)) — [08 §4](08_denizaltilar_basinc_seferler.md#4-seyir-ve-oksijen).
  Bonuslar araştırma, kahraman, ittifak bölgesi (+%30), Rezonans ve fermanlardan gelir.
- **Yürüyüş hızlandırıcıları** (çanta eşyası, yalnızca yoldaki takıma): %25 ve %50 "kalan yürüyüş
  süresini azaltır"; eşya yoksa Hadalit ile "al ve kullan" (fiyat K-12 ölçeğinde).
- **Geri çağırma**: yolda ve toplamada ücretsiz, anında; dönüş süresi o ana kadar gidilen süreye eşit;
  dönüş de hızlandırılabilir. Ralliden ayrılmak ve "anında eve dön" bir eşya ister.
- Toplama süresi hızlandırılmaz (yük sınırı ve 09'daki gelir dengesi için).

### 4.6 Arama

Büyüteç (Last Asylum'daki gibi): sekmeler **Canavar** (tür + seviye, ilk öldürme ödülü, saldırı
gücü sınırı) ve **Yatak** (tür + seviye, verim); "Ara" en yakın uygun hedefe kamerayı götürür ve
balonunu açar.

## 5. Arayüz ilkeleri

- Her ekran aynı dili konuşur: koyu panel, başlık şeridi, sağ üstte kapat; içerikte bölüm başlıkları;
  altta tek büyük ana eylem ve yanında Hadalit'li anında seçenek.
- Durum hep görünür: kuyruklar sol üstte, Revir ve ordu sayıları hazırlıkta, balonlar binaların ve
  takımların üstünde.
- Her işlem kamera hareketiyle biter: eğitim → kışla, sefer → takım, iyileştirme → Revir.

## 6. Açılış ve geçişler

### 6.1 Açılış ve yükleme

1. Platform girişi (Google Play Games karşılaması) — sistemin kendi bildirimi.
2. **Yükleme ekranı**: tam ekran kapak çizimi (Hadal derinliği, kubbe, ışık), logo, altta
   **derinlik göstergesi** gibi dolan yükleme çubuğu (0 → 10.935 m) ve aşama metni ("Kubbe
   basınçlanıyor…", "Sonar ısınıyor…"), dönen ipuçları; bağlanınca üstte "Tekrar hoş geldin, Kaptan".
3. Üsse geçiş: kapak kararır, üs **yukarıdan aşağı dalış** kamerasıyla açılır (0,8 sn), HUD kayarak
   gelir.

### 6.2 Üs ↔ dünya

- **Üs → dünya**: kamera yükselir ("yüzeye çıkış"), ekranı 0,4 sn **kabarcık ve akıntı perdesi**
  örter, perde arkasında sahne değişir, dünya kamerası üssün karesinden uzaklaşarak açılır.
- **Dünya → üs**: kamera üssün karesine yakınlaşır, perde, üs dalış kamerasıyla açılır.
- İki geçişte de HUD aynı kalır (yalnızca alt çubuktaki Bölge/Üs düğmesi döner), böylece iki ekran tek
  bir dünyanın iki yakınlığı gibi hissedilir.

## 7. Uygulama sırası

| Paket | İçerik | Bağlı olduğu |
|---|---|---|
| D1 · Ordu çekirdeği | Asker türleri ve kademeleri (catalog), Askeri Kubbe kuyrukları, Tatbikat Havuzu, Eğitim ekranı, ordu görünümü, bakım | 07 |
| D2 · Revir | Yaralılar, iyileştirme kuyruğu, ittifak yardımı, Kriyo | D1 |
| D3 · Takımlar ve sefer | Hangarlar, gövdeler, takım ön ayarları, sefer hazırlığı, kuyruk, rota ve denizaltı modeli, yürüyüş hızlandırıcıları; bugünkü dalgıçlı toplama bunun yerine geçer | D1, 08 |
| D4 · Savaş | Savaş çözümü (07 §4), harita canavarları ve arama, saldırı, rapor | D2, D3 |
| D5 · İttifak seferleri | Takviye, ralli, nakil, keşif | D4, 10 |
| T · Geçişler | Yükleme ekranı, üs ↔ dünya perdesi ve kamera geçişleri | Bağımsız; her an yapılabilir |

Her paket sunucu (kurallar + testler), istemci (ekranlar) ve telefon denemesiyle biter; denge
değerleri `docs/rehber/veri`'den gelir.

## 8. Esin kaynakları

Last Asylum: Plague (yerinde gözlem, yukarıdaki tablo); Whiteout Survival (kamp başına kuyruk,
parti boyutu ve Revir kapasite eğrileri, Enlistment Office), Rise of Kingdoms (hastane sayısı ve
dolma kuralı, kademeye göre hız), Last War (5 kahramanlı takımlar, ralli toplanma süreleri). Rakamlar
bu oyunlardan alınmaz; yapılar alınır, sayılar rehberin veri dosyalarındandır.

## 9. Açık sorular

- **Eğitim binası düzeni.** Last Asylum gibi tür başına bir eğitim binası (4 bina, her biri kendi
  kuyruğu ve kendi kademe kilidi) mı, rehberdeki tek Askeri Kubbe + kuyruk eşikleri mi? İlki şehri
  canlı gösterir ve kademe ilerlemesini türe böler; ikincisi bina sayısını düşük tutar.
- **Mesafe birimi.** Ekranda kare mi, deniz mili mi (Last Asylum "km" yazıyor)?
