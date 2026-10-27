# 00 · Kanon — Bağlayıcı Kararlar

Bu sayfa HADAL Oyun Kılavuzu'nun anayasasıdır. Kılavuzdaki her bölüm, her veri dosyası ve
her yeni tasarım burada yazanlara uyar. Eski tasarım dökümanları (`docs/01…28`) olduğu gibi
korunur; onlarla çelişen bir yer varsa **bu sayfa geçerlidir**. Her kararın yanında neyi
değiştirdiği ve hangi eski dökümanı geçersiz kıldığı yazar.

Durum etiketleri tüm kılavuzda aynıdır:

| Etiket | Anlamı |
|---|---|
| ✅ | Kodda var ve değer kılavuzla aynı |
| 🟡 | Kodda var ama değer farklı ya da işlevi henüz yok |
| ⬜ | Henüz kodda yok, tasarım |

Kılavuz **yalnızca tasarımdır**. Oyun kodu ve `shared/catalog/catalog.json` (v8) bu kılavuzla
değişmez. Hedef dengenin koda taşınması ayrı bir iştir (bkz. [90 · Durum ve Yol Haritası](90_durum_ve_yol_haritasi.md)).

---

## Dünya ve hikâye

### K-01 · Kaynaklar
Altı temel kaynak vardır: **Oksijen, Enerji, Gıda, Biyokütle, Titanyum, Hadalit**. Geç oyun
malzemesi **Süperalaşım**'dır: polimetalik nodüllerdeki nikel ve kobalttan dökülür, Reaktör 30'dan
sonra kapı kaynağı olur. Kodda boş duran `ResourceType.Alloy = 5` bunun için kullanılır.
- Geçersiz kılar: `06_Resources.md` ve `19_Economy.md` (Gıda yoktu) ve dış taslaklarda uydurulan kristal kaynakları.
- Teçhizat malzemeleri (kitin, bazalt camı, masif sülfür, sintaktik köpük) kaynak değil, eşyadır ([17 · Teçhizat](17_techizat.md)).

### K-02 · Harita biçimi
Dünya haritası 200 × 200 kare ızgaradır (kodda böyle). `11_World_Map.md`'deki "daire" fikri,
merkeze uzaklığa göre derinleşen kuşaklar ve sonar katmanı olarak yaşar. Uzaklık Chebyshev
uzaklığıdır: `max(|x−100|, |y−100|)`.

### K-03 · Üssün derinliği
Üs **~6.000 m'de, Hadal Zonu'nun girişinde** kurulur. Harita kenardan merkeze gerçek bir okyanus
çukurunun kesitidir; merkez, çukurun en derin noktasıdır (Mariana'da Challenger Deep, ~10.935 m).
**Çekirdek** oradadır. "Daha derine in" vaadi böylece gerçek bir yönü olan bir yolculuk olur.
- Geçersiz kılar: `01_Executive_Vision.md` ve `23_Base_Management.md`'deki "üs 11.000 m'de".
- `07_Pressure_System.md`'deki 1.000–7.000 m katmanları hikâye girişine (yüzeyden iniş) taşınır.

### K-11 · Düşman adları
`09_Enemies.md`'deki "The Forgotten" düşmanının Türkçe adı **Kadim Muhafızlar**'dır. Kahraman
fraksiyonu **Unutulanlar** (Forgotten Ones) ile karışmasın diye.

### K-23 · Gerçek dünya verisi
Derinlik, basınç, sıcaklık, canlı türleri, mineraller ve doğa olayları gerçek derin deniz
verilerine dayanır. Bu rakamların tek kaynağı `veri/bilim.json`'dır ve her kaydın bir kaynakçası
vardır ([18 · Bilim Temeli](18_bilim_temeli.md)). Oyuncuya dönük metinlerde "gerçek / kurgu"
etiketi kullanılmaz. Hadalit, kadim uygarlık, Leviathan'lar, Kadim Muhafızlar ve Titan hikâyenin
kurgusal unsurlarıdır; senaryo gerektirirse yenileri eklenebilir.

### K-24 · Sunucular gerçek çukurlardır
Her sunucu gerçek bir hadal çukurunun adını taşır: Mariana, Tonga, Kermadec, Kuril-Kamçatka,
Filipin, Izu-Ogasawara, Porto Riko, Güney Sandwich, Atacama, Java. Haritadaki derinlik
değerleri o çukurun gerçek profiline göre gösterilir; oyun mekaniği kuşak sırasını kullanır, böylece
her sunucu eşit oynanır. Sunucular arası sezon savaşı (Akıntı Savaşı) çukurlar arasında yapılır.

### K-25 · Gerçek canlılar avlanmaz
Gerçek türler savaş hedefi değildir. Haritada ortam canlısı, sonarla taranan **Tür Kataloğu**
kaydı ve sefer karşılaşması olarak yer alır. Saldırılan ve avlanan her hedef kurgusal bir
düşmandır (Uçurum Sürüngenleri, Siren Sürüsü, Demir Leviathan, Kadim Muhafızlar, Hadal Titanı).

### K-26 · Gerçek trajediler içerik değildir
Gerçek kazalar ve can kayıpları (örneğin 2023 Titan denizaltısı) oyun içeriği, olay ya da şaka
konusu yapılmaz. Mühendislik dersleri (örneğin 2014'te 9.990 m'de kaybolan insansız Nereus aracı)
saygılı bir dille, yalnızca bilgi olarak kullanılabilir.

---

## Kahramanlar ve gacha

### K-04 · Nadirlik ve garanti
Dört nadirlik kalır: **Sıradan, Nadir, Destansı, Efsanevi**. Kriyo-Kapsül oranları 65 / 25 / 8 / 2.
Destansı ve üstü 30. çekilişte garantidir (kodda var). Efsanevi garantisi 80. çekilişte eklenir.
- Geçersiz kılar: `15_Monetization.md`'deki 7 kademe, 90 garanti ve "Memory Fragments".

### K-05 · Başlangıç kahramanı
Başlangıç kahramanı **Komutan Elias Rourke**'tur (`mara`). `25_Heroes_Alliance_Chat.md`'deki
"Kaptan Mara Venn" adı geçersizdir; id `mara` olarak kalır.

### K-19 · Oyuncu kökeni
Oyuncu oyuna başlarken beş fraksiyondan birini **köken** olarak seçer: Atlantik Birliği, Pasifik
Tarikatı, Derindoğanlar, Unutulanlar, Uçurum Tarikatı. Köken küçük pasif bonuslar, bir başlangıç
kahramanı, kendine özgü bir birlik ve mimari kaplama verir. Ayrıntı: [16 · Kronik, Köken, Profil](16_kronik_kokenler_profil.md).

---

## Ekonomi ve ilerleme

### K-07 · İnşaatçılar
Oyuncu 1 inşaatçıyla başlar. 2. inşaatçı araştırmadan gelir (İkinci İnşaatçı, kodda var).
3. inşaatçı yalnızca aylık kart (**Sendika Sözleşmesi**) süresince açıktır.
- Geçersiz kılar: `23_Base_Management.md`'deki "2. inşaatçı Dron Fabrikası ya da premium".

### K-09 · Günlük görev puanı
Günlük görevlerin toplamı bugün 130 puandır (kod). Hedef 150: dalış ve arena görevleri eklenir.
`27_Quests_And_Items.md`'deki "150" hedef değer olarak doğrudur.

### K-12 · Hadalit ölçeği
Hadalit ×10 ölçeklenir ve cüzdan tavanı kalkar (premium para birimi biriktirilebilir olmalı).
Kriyo-Kapsül çekimi 20 → 200, onluk 180 → 1.800 olur; bütün ödüller aynı oranla çarpılır.

### K-13 · Hız formülü
Süre kısaltan tüm bonuslar bölen olarak uygulanır: **süre = ham süre / (1 + Σhız% / 100)**.
Bugünkü kod yüzdeleri çıkarıp %10'da durduruyor; araştırma, kahraman, VIP ve ittifak bonusları
üst üste bindiğinde bu yöntem kırılır. Bölen formülü hiçbir zaman sıfıra inmez.

### K-14 · Güç
Güç, her binanın her seviyesi için tabloda yazan değerdir ([02 · Tempo ve Denge](02_tempo_ve_denge.md)).
Bugünkü "bina seviyeleri toplamı × 10" formülü emekliye ayrılır. Toplam güç = binalar + araştırma +
birlikler + kahramanlar.

### K-15 · Askeri yapılar
Askeri Kubbe kışla görevini görür ve en fazla **1** adettir (kodda 2). Asker kademelerini ve parti
boyutunu açan **Tatbikat Havuzu** ayrı bir binadır.

### K-16 · Kubbeler
Dört kubbe vardır: **Mega, Tarım, Bilim, Savaş**. Her biri 5 seviyedir ve her seviyesi bir reaktör
kapısına bağlıdır. `05_Base_Building.md`'deki "War Dome / Science Dome" böylece kanonlaşır.

### K-17 · Üretim birimi
Üretim ve tüketim **saat başına** tanımlanır. Kodda saniye başına tam sayı tutuluyor; saatlik
değere geçişte kalan kesir taşınır (hata %1'in altında).

### K-20 · Derinlik Kroniği
Her sunucu ortak bir zaman çizelgesi izler: **Derinlik Kroniği**. Kuşakların, geçitlerin,
kademelerin ve sezon savaşının açılışı kronik aşamalarına bağlıdır.

### K-21 · Asker kaybı
Askerler ya **yaralı** (Revir'de iyileşir) ya da **ölü** olur. Sezon sonunda ölülerin %50'si
**Kurtarma Kapsülü** olarak geri verilir. Büyük savaşlar ücretsiz oyuncu için de katlanılabilir kalır.

### K-22 · Sonar Enerjisi
Haritadaki PvE saldırıları **Sonar Enerjisi** harcar (hareket puanı). Ayrıntı: [09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md).

---

## Sunum ve süreç

### K-06 · Mağazanın adı
Mağazanın adı her yerde **The Syndicate**'tir. "Mağaza", "Market", "Store" denmez.
- Geçersiz kılar: `26_HUD.md`'deki "Mağaza 💎" düğmesi (koda takip işi).

### K-08 · Yeni üslerin yeri
Yeni üsler **Hadal Eşiği** kuşağına yerleşir. Bugünkü kod merkezden dışa spiral yerleştiriyor; bu,
"merkez en tehlikeli yer" kuralıyla çelişir (koda takip işi).

### K-10 · Faz numaraları
Faz numaralandırmasının tek kaynağı kök dizindeki `ROADMAP.md`'dir. Kılavuz faz yerine **sistem**
adıyla konuşur.

### K-18 · Yazım
Türkçe yazım her yerde **Kılavuz**'dur (TDK). Kod kimlikleri İngilizce `snake_case` kalır; oyuncuya
dönük her metin Türkçedir.

---

## Özet tablo

| Kod | Konu | Karar |
|---|---|---|
| K-01 | Kaynaklar | 6 temel kaynak + geç oyun Süperalaşım |
| K-02 | Harita | 200 × 200 kare, merkeze doğru derinleşen kuşaklar |
| K-03 | Derinlik | Üs ~6.000 m; Çekirdek çukurun en derin noktasında |
| K-04 | Nadirlik | 4 kademe; destansı garanti 30, efsanevi garanti 80 |
| K-05 | Başlangıç kahramanı | Komutan Elias Rourke (`mara`) |
| K-06 | Mağaza | The Syndicate |
| K-07 | İnşaatçı | 1 + araştırma + aylık kart |
| K-08 | Yerleşim | Yeni üsler Hadal Eşiği'nde |
| K-09 | Günlük puan | 130 → 150 |
| K-10 | Faz numarası | ROADMAP.md |
| K-11 | Düşman adı | Kadim Muhafızlar |
| K-12 | Hadalit | ×10, tavansız |
| K-13 | Hız | ham / (1 + Σ%) |
| K-14 | Güç | Bina × seviye tablosu |
| K-15 | Askeri | Askeri Kubbe ×1 + Tatbikat Havuzu |
| K-16 | Kubbeler | Mega, Tarım, Bilim, Savaş; 5'er seviye |
| K-17 | Üretim | Saatlik |
| K-18 | Yazım | Kılavuz; oyuncu metni Türkçe |
| K-19 | Köken | 5 fraksiyondan seçim |
| K-20 | Kronik | Sunucu zaman çizelgesi |
| K-21 | Kayıp | Yaralı / ölü, sezon sonu %50 iade |
| K-22 | Sonar Enerjisi | Harita PvE sınırı |
| K-23 | Gerçek veri | `veri/bilim.json`, etiketsiz |
| K-24 | Sunucular | Gerçek çukur adları |
| K-25 | Fauna | Gerçek türler avlanmaz |
| K-26 | Trajediler | İçerik yapılmaz |
