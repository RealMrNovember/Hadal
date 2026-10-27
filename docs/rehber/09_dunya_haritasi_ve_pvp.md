# 09 · Dünya Haritası ve PvP

> **Özet.** Dünya haritası, gerçek bir okyanus çukurunun kenardan merkeze kesitidir: dışarıda abisal
> düzlük, ortada çukurun en derin noktası. 200 × 200 karelik kare ızgara altı **derinlik kuşağına**
> bölünür; kuşaklar arasındaki **Basınç Kilitleri** ittifakların tuttuğu geçitlerdir. Haritada okyanusun
> bildiğimiz birikimleri (nodül, kobalt kabuğu, sönmüş sülfür bacaları, metan hidrat, balina leşi, enkaz)
> toplanır; derinin düşmanları (Uçurum Sürüngenleri, Kadim Muhafızlar, Leviathan'lar) **Sonar Enerjisi**
> harcanarak avlanır; Rezonans Alanları ve **Çukur Kalbi** ittifaklara güç ve unvan verir. PvP sakin lüks ilkesine bağlıdır: yeni oyuncu koruması, yağma sınırı,
> güç farkı cezası ve otomatik toparlanma kalkanı ücretsiz oyuncuyu ezilmekten korur.

Bağlı bölümler: [00 · Kanon](00_kanon.md) (K-02, K-03, K-08, K-22, K-24, K-25) ·
[02 · Tempo ve Denge](02_tempo_ve_denge.md) · [03 · Binalar](03_binalar.md) ·
[07 · Birlikler ve Savaş](07_birlikler_ve_savas.md) · [08 · Denizaltılar, Basınç, Seferler](08_denizaltilar_basinc_seferler.md) ·
[10 · İttifak](10_ittifak.md) · [13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md) ·
[14 · Son Oyun](14_son_oyun.md) · [15 · Ekonomi Akışı](15_ekonomi_akisi.md) ·
[16 · Kronik, Köken, Profil](16_kronik_kokenler_profil.md) · [18 · Bilim Temeli](18_bilim_temeli.md).
Veri: [`veri/harita.json`](veri/harita.json).

---

## 1. Amaç

Üs ekranı oyuncunun evidir; harita ise **sunucunun ortak hikâyesinin geçtiği yer**. Harita dört işi
birden görür:

1. **"Daha derine in" vaadini bir yöne bağlar.** Kenardan merkeze her adım daha derin, daha basınçlı,
   daha değerli ve daha tehlikelidir (K-03). Merkez, çukurun en derin noktası ve Çekirdek'tir.
2. **Ekonominin üçte birini taşır.** Ücretsiz oyuncunun gelirinin yaklaşık üçte biri toplama,
   kurtarma ve seferden gelir ([15 · Ekonomi Akışı](15_ekonomi_akisi.md)); toplamanın tamamı haritadadır.
3. **İttifakı somutlaştırır.** Bölge, kilitler, Rezonans Alanları ve Çukur Kalbi ittifakın elle tutulur
   kazanımlarıdır ([10 · İttifak](10_ittifak.md)).
4. **Sunucuya bir takvim verir.** Kuşaklar ve geçitler Derinlik Kroniği'nin aşamalarıyla açılır
   (K-20, [16](16_kronik_kokenler_profil.md)); her sunucu aynı yolculuğu kendi temposunda yaşar.

---

## 2. Harita

### 2.1 Izgara ve uzaklık

- Harita <!--v:veri.harita.izgara.boyut-->200<!--/v--> × 200 karelik kare ızgaradır (kodda böyle, K-02).
  Merkez (100, 100) karesidir.
- Bir karenin merkeze uzaklığı **Chebyshev uzaklığıdır**: `u = max(|x − 100|, |y − 100|)`. Aynı
  uzaklıktaki kareler merkezin çevresinde kare bir halka oluşturur; bu yüzden kuşaklar daire değil
  **kare halkalardır**. `11_World_Map.md`'deki daire fikri bu halkalarda yaşar.
- Her üs bir kareyi tutar. Yeni üsler birbirinden en az <!--v:veri.harita.izgara.aralik-->3<!--/v-->
  kare arayla yerleşir (kodda `world.spacing`).
- Oyuncu haritayı kendi üssünün çevresinde <!--v:veri.harita.izgara.gorus-->14<!--/v--> karelik
  yarıçapla görür (kodda `world.viewRadius`); bunun ötesi sistir (bkz. §9).

### 2.2 Altı derinlik kuşağı

Mekanik **kuşak sırasını** kullanır; metredeki değerler Mariana referansıdır ve her sunucuda o
çukurun gerçek profiline göre gösterilir (§2.5).

<!-- URET:tablo:harita.kusaklar -->
| # | Kuşak | Derinlik (Mariana) | Uzaklık (Chebyshev) | Kare | Üs | Gövde (sığ / derin) | Yatak Sv | Düşman Sv | Açılış (Kronik) |
| ---: | --- | --- | --- | ---: | --- | --- | --- | --- | --- |
| 1 | Abisal Düzlük | 4.000–6.000 m | 85–100 | 11.439 | Hayır | P1 / P1 | 1–6 | 1–10 | 1 · İniş |
| 2 | Hadal Eşiği | 6.000–7.000 m | 65–84 | 11.920 | Evet · yeni üsler | P1 / P2 | 1–4 | 1–20 | 1 · İniş |
| 3 | Çukur Yamacı | 7.000–8.000 m | 46–64 | 8.360 | Evet · P3 | P3 / P4 | 3–5 | 15–30 | 4 · Birinci Kilit |
| 4 | Derin Yamaç | 8.000–9.000 m | 28–45 | 5.256 | Evet · P5 | P5 / P6 | 4–6 | 25–40 | 6 · İkinci Kilit |
| 5 | Çukur Ekseni | 9.000–10.000 m | 12–27 | 2.496 | Evet · yalnızca hedefli | P7 / P8 | 5–7 | 35–50 | 8 · Eksen |
| 6 | Dip ve Çekirdek | 10.000–10.935 m | 0–11 | 529 | Hayır | P9 / P10 | 6–8 | 45–50 | 10 · Çekirdek |
<!-- /URET:tablo:harita.kusaklar -->

- **Abisal Düzlük** çukurun dışıdır: düz, sakin, nodül tarlalarıyla kaplı. Üs kurulmaz; keşif ve
  güvenli toplama yeridir. Ücretsiz oyuncunun Süperalaşım'ı en güvenli buradan toplanır (§4).
- **Hadal Eşiği** her yeni üssün kurulduğu kuşaktır (K-08). Spiral yerleşimin yerini kuşak içi
  dağıtım alır (§15).
- **Çukur Yamacı, Derin Yamaç, Çukur Ekseni** sırayla açılır; her biri yeni yatak seviyesi, yeni
  düşman, yeni Rezonans Alanı getirir.
- **Dip ve Çekirdek** üs kurulamayan son halkadır: Çukur Kalbi, en büyük Leviathan yuvaları ve
  Hadalit damarları buradadır.

Hadal Eşiği <!--v:veri.harita.kusaklar.hadal_esigi.kare-->11.920<!--/v--> karedir; 3 kare aralıkla
yaklaşık <!--v:veri.harita.izgara.esik_yuva-->1.324<!--/v--> üs yuvası eder. Yatak ve alanlar
çıkınca sunucu başına **~1.100 kurucu kaptan** kapasitesi kalır (bkz. §13, sunucu kapanışı).

### 2.3 Basamaklar ve gövde sınıfı

Her kuşak iki **basamağa** ayrılır. Bir yürüyüş, gidebileceği en derin basamağı denizaltının gövde
basınç sınıfıyla belirler ([08](08_denizaltilar_basinc_seferler.md)); sınıflar `bilim.json`'daki
gerçek karşılıklarıyla eşleşir: P1 = <!--v:veri.bilim.basinc.siniflar.0.derinlik_m-->6.500<!--/v--> m,
P2 = <!--v:veri.bilim.basinc.siniflar.1.derinlik_m-->7.000<!--/v--> m … P10 =
<!--v:veri.bilim.basinc.siniflar.9.derinlik_m-->10.935<!--/v--> m. Böylece her basamağın en derin
noktası tam bir gövde sınıfının sınırına denk gelir. İlk yürüyüş, ilk Denizaltı Hangarı ve İskele
(Reaktör 4) kurulunca mümkün olur.

<!-- URET:tablo:harita.basamaklar -->
| Kuşak | Basamak | Uzaklık | Kare | Derinlik (Mariana) | Gerekli gövde |
| --- | --- | --- | ---: | --- | --- |
| Abisal Düzlük | Tek | 85–100 | 11.439 | 4.000–6.000 m | P1 |
| Hadal Eşiği | Sığ | 75–84 | 6.360 | 6.000–6.500 m | P1 |
| Hadal Eşiği | Derin | 65–74 | 5.560 | 6.500–7.000 m | P2 |
| Çukur Yamacı | Sığ | 55–64 | 4.760 | 7.000–7.500 m | P3 |
| Çukur Yamacı | Derin | 46–54 | 3.600 | 7.500–8.000 m | P4 |
| Derin Yamaç | Sığ | 37–45 | 2.952 | 8.000–8.500 m | P5 |
| Derin Yamaç | Derin | 28–36 | 2.304 | 8.500–9.000 m | P6 |
| Çukur Ekseni | Sığ | 20–27 | 1.504 | 9.000–9.500 m | P7 |
| Çukur Ekseni | Derin | 12–19 | 992 | 9.500–10.000 m | P8 |
| Dip ve Çekirdek | Dip | 6–11 | 408 | 10.000–10.500 m | P9 |
| Dip ve Çekirdek | Çekirdek | 0–5 | 121 | 10.500–10.935 m | P10 |
<!-- /URET:tablo:harita.basamaklar -->

Kurallar:

- Yürüyüşün gövde sınıfı, yürüyüşü taşıyan denizaltınınkidir (Denizaltı İskelesi seviyesi, 08).
  Tablodaki "gerekli gövde", 08'deki **sınıf açığı** kuralının "gidilen bölgenin istediği sınıf"ıdır:
  açık 0 ise giriş serbest, 1 ise riskli (gövde bütünlüğü erir, askerler DEF kaybeder), 2 ve üstü ise
  basamak kapalıdır ve harita onu soluk gösterir ([08](08_denizaltilar_basinc_seferler.md) §3.3).
- **Ralli lider gövdesiyle iner.** Katılımcının askerleri liderin denizaltısında taşınır; katılımcının
  gövdesi yalnızca ralli noktasına (liderin üssüne) gitmeye yetmelidir. Ücretsiz oyuncu böylece
  Çekirdek rallilerinden dışlanmaz.
- **Çukur Kalbi'ne giden dört Kadim Tünel** Dip basamağından başlar: Kalp'e P9 gövde yeter. P10 yalnızca
  Çekirdek basamağındaki yataklar ve en büyük Leviathan yuvaları için gerekir.
- Üs taşınırken açık kabul edilmez: üssün yeni karesi, İskele'nin sınıfının tam ulaştığı basamakta
  olmalıdır (Yamaç P3, Derin Yamaç P5, Çukur Ekseni P7).

Ücretsiz oyuncu bu sınıflara kuşaklar açılmadan önce ulaşır: P3 İskele 11 ile
(F2P Reaktör 11: <!--v:hesap.sim.f2p.seviye.11|gun-->9 sa 36 dk<!--/v-->), P5 İskele 21 ile
(<!--v:hesap.sim.f2p.seviye.21|gun-->6,1. gün<!--/v-->), P7 İskele 31 ile
(<!--v:hesap.sim.f2p.seviye.31|gun-->35,5. gün<!--/v-->), P9 İskele 41 ile
(<!--v:hesap.sim.f2p.seviye.41|gun-->147,7. gün<!--/v-->). Kronik kapıları (Yamaç 25., Derin Yamaç 60.,
Eksen 120., Dip 180. gün) her seferinde bundan sonradır; derine inmeyi oyuncunun gövdesi değil sunucunun
takvimi belirler. Tek istisna Çekirdek basamağıdır: P10 İskele 46 ister
(<!--v:hesap.sim.f2p.seviye.46|gun-->255,8. gün<!--/v-->); o güne kadar ücretsiz oyuncu Çekirdek'e
ralliyle iner.

### 2.4 Kanatlar ve fay sırtları

Kare halkanın dört kenarı dört **kanattır**: Kuzey, Doğu, Güney, Batı. Kanatlar, harita köşegenleri
boyunca uzanan **fay sırtlarıyla** ayrılır; sırtlar uzaklık 46–84 arasında (Hadal Eşiği ve Çukur
Yamacı) uzanır, Abisal Düzlük'te ve Derin Yamaç'tan içeride yoktur.

- Erken oyunda bir kanattan komşusuna iki yol vardır: dışarıdan, Abisal Düzlük'ün düz tabanını dolaşan
  **uzun yol**; ya da aşama 3'te açılan **Fay Geçidi**. Bu, ilk haftaları kanat içi bir yarışa çevirir:
  ittifaklar önce kendi kanatlarında yerleşir.
- Yeni üsler dört kanada eşit dağıtılır (her kanatta bir sayaç; en boş kanat seçilir). Oyuncu
  isterse ilk 7 günde ücretsiz **Yeni Başlangıç** ile istediği kanada geçer (§10.4).
- Derin Yamaç kesintisiz bir halkadır: kanatlar burada birleşir. Orta oyunun büyük çekişmesi bu
  halkadadır.

### 2.5 Her sunucu kendi çukuru

Her sunucu gerçek bir hadal çukurudur (K-24). Mekanik kuşak ve basamak sırasıyla çalışır; ekranda
görünen derinlik o çukurun gerçek profiline göre ölçeklenir:

```
z ≤ 6.000 m  ise  gösterilen = z                       (Abisal Düzlük aynı kalır)
z > 6.000 m  ise  gösterilen = 6.000 + (z − 6.000) × k
k = (D − 6.000) / (10.935 − 6.000)      D = o çukurun en derin noktası (bilim.json)
```

`z` karenin Mariana referans derinliğidir (kuşak içinde doğrusal). Böylece üs her sunucuda Hadal
Zonu'nun girişinde, ~6.000 m'de kurulur (K-03) ve merkez her sunucuda o çukurun en derin noktasıdır.
Basınç da gösterilen derinlikten okunur (her ~10 m'de ≈
<!--v:veri.bilim.basinc.bar_her_10m-->1<!--/v--> bar).

| Sunucu | Çukur | En derin nokta | Derinlik (m) | Okyanus |
|---|---|---|---:|---|
| Mariana | <!--v:veri.bilim.cukurlar.mariana.ad-->Mariana Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.mariana.en_derin_nokta-->Challenger Deep<!--/v--> | <!--v:veri.bilim.cukurlar.mariana.derinlik_m-->10.935<!--/v--> | <!--v:veri.bilim.cukurlar.mariana.okyanus-->Batı Pasifik<!--/v--> |
| Tonga | <!--v:veri.bilim.cukurlar.tonga.ad-->Tonga Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.tonga.en_derin_nokta-->Horizon Deep<!--/v--> | <!--v:veri.bilim.cukurlar.tonga.derinlik_m-->10.816<!--/v--> | <!--v:veri.bilim.cukurlar.tonga.okyanus-->Güneybatı Pasifik<!--/v--> |
| Filipin | <!--v:veri.bilim.cukurlar.filipin.ad-->Filipin Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.filipin.en_derin_nokta-->Emden (Galathea) Derinliği<!--/v--> | <!--v:veri.bilim.cukurlar.filipin.derinlik_m-->10.540<!--/v--> | <!--v:veri.bilim.cukurlar.filipin.okyanus-->Batı Pasifik<!--/v--> |
| Kermadec | <!--v:veri.bilim.cukurlar.kermadec.ad-->Kermadec Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.kermadec.en_derin_nokta-->Scholl Deep<!--/v--> | <!--v:veri.bilim.cukurlar.kermadec.derinlik_m-->10.047<!--/v--> | <!--v:veri.bilim.cukurlar.kermadec.okyanus-->Güneybatı Pasifik<!--/v--> |
| Izu-Ogasawara | <!--v:veri.bilim.cukurlar.izu_ogasawara.ad-->Izu-Ogasawara Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.izu_ogasawara.en_derin_nokta-->Adsız derinlik (~9.826 m)<!--/v--> | <!--v:veri.bilim.cukurlar.izu_ogasawara.derinlik_m-->9.826<!--/v--> | <!--v:veri.bilim.cukurlar.izu_ogasawara.okyanus-->Kuzeybatı Pasifik<!--/v--> |
| Kuril-Kamçatka | <!--v:veri.bilim.cukurlar.kuril_kamcatka.ad-->Kuril-Kamçatka Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.kuril_kamcatka.en_derin_nokta-->Orta kesim (~9.600 m)<!--/v--> | <!--v:veri.bilim.cukurlar.kuril_kamcatka.derinlik_m-->9.604<!--/v--> | <!--v:veri.bilim.cukurlar.kuril_kamcatka.okyanus-->Kuzeybatı Pasifik<!--/v--> |
| Porto Riko | <!--v:veri.bilim.cukurlar.porto_riko.ad-->Porto Riko Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.porto_riko.en_derin_nokta-->Brownson Deep<!--/v--> | <!--v:veri.bilim.cukurlar.porto_riko.derinlik_m-->8.378<!--/v--> | <!--v:veri.bilim.cukurlar.porto_riko.okyanus-->Atlantik<!--/v--> |
| Güney Sandwich | <!--v:veri.bilim.cukurlar.guney_sandwich.ad-->Güney Sandwich Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.guney_sandwich.en_derin_nokta-->Meteor Deep<!--/v--> | <!--v:veri.bilim.cukurlar.guney_sandwich.derinlik_m-->8.265<!--/v--> | <!--v:veri.bilim.cukurlar.guney_sandwich.okyanus-->Güney Atlantik / Güney Okyanusu<!--/v--> |
| Atacama | <!--v:veri.bilim.cukurlar.atacama.ad-->Atacama Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.atacama.en_derin_nokta-->Adsız derinlik (Richards Deep'in kuzeyi)<!--/v--> | <!--v:veri.bilim.cukurlar.atacama.derinlik_m-->8.069<!--/v--> | <!--v:veri.bilim.cukurlar.atacama.okyanus-->Güneydoğu Pasifik<!--/v--> |
| Java | <!--v:veri.bilim.cukurlar.java.ad-->Java Çukuru<!--/v--> | <!--v:veri.bilim.cukurlar.java.en_derin_nokta-->Adsız derinlik<!--/v--> | <!--v:veri.bilim.cukurlar.java.derinlik_m-->7.187<!--/v--> | <!--v:veri.bilim.cukurlar.java.okyanus-->Hint Okyanusu<!--/v--> |

Kuşakların her sunucuda gösterilen derinliği (`harita.json` → `sunucular`, `bilim.json`'dan hesaplanır):

<!-- URET:tablo:harita.sunucular -->
| Çukur | Ölçek k | Hadal Eşiği (m) | Çukur Yamacı (m) | Derin Yamaç (m) | Çukur Ekseni (m) | Dip ve Çekirdek (m) |
| --- | ---: | --- | --- | --- | --- | --- |
| Mariana Çukuru | 1 | 6.000–7.000 | 7.000–8.000 | 8.000–9.000 | 9.000–10.000 | 10.000–10.935 |
| Tonga Çukuru | 0,98 | 6.000–6.976 | 6.976–7.952 | 7.952–8.928 | 8.928–9.904 | 9.904–10.816 |
| Kuril-Kamçatka Çukuru | 0,73 | 6.000–6.730 | 6.730–7.461 | 7.461–8.191 | 8.191–8.921 | 8.921–9.604 |
| Filipin Çukuru | 0,92 | 6.000–6.920 | 6.920–7.840 | 7.840–8.760 | 8.760–9.680 | 9.680–10.540 |
| Kermadec Çukuru | 0,82 | 6.000–6.820 | 6.820–7.640 | 7.640–8.460 | 8.460–9.280 | 9.280–10.047 |
| Izu-Ogasawara Çukuru | 0,78 | 6.000–6.775 | 6.775–7.551 | 7.551–8.326 | 8.326–9.101 | 9.101–9.826 |
| Porto Riko Çukuru | 0,48 | 6.000–6.482 | 6.482–6.964 | 6.964–7.446 | 7.446–7.927 | 7.927–8.378 |
| Güney Sandwich Çukuru | 0,46 | 6.000–6.459 | 6.459–6.918 | 6.918–7.377 | 7.377–7.836 | 7.836–8.265 |
| Atacama Çukuru | 0,42 | 6.000–6.419 | 6.419–6.839 | 6.839–7.258 | 7.258–7.677 | 7.677–8.069 |
| Java Çukuru | 0,24 | 6.000–6.241 | 6.241–6.481 | 6.481–6.722 | 6.722–6.962 | 6.962–7.187 |
<!-- /URET:tablo:harita.sunucular -->

Bu ölçek oyuna gerçek bir doku katar: **canlılar gösterilen derinliğe göre görünür** (§9). Java'da
çukurun tamamı balık sınırının üstündedir, bu yüzden salyangoz balıkları Dip'e kadar iner; Mariana'da
balıklar Çukur Yamacı'nın altında kaybolur ve yerlerini amfipodlar alır. Her sunucu aynı oynanır ama
aynı görünmez.

---

## 3. Geçitler

Kuşak sınırları dik yamaç duvarlarıdır; yürüyüşler bir kuşaktan ötekine yalnızca **geçitlerden** geçer.
Abisal Düzlük ile Hadal Eşiği arasında duvar yoktur (çukurun ağzı açıktır); diğer her sınır
kilitlidir.

<!-- URET:tablo:harita.gecitler -->
| Geçit | Adet | Konum | Bağladığı | Bekçi | Dayanıklılık | Garnizon (sefer) | Uyanış (Kronik) |
| --- | ---: | --- | --- | --- | ---: | ---: | --- |
| Fay Geçidi (Eşik) | 4 | 4 köşegen × Hadal Eşiği (uzaklık 74–75) | Komşu kanatlar | Bekçi I (Sv 15) | 150.000 | 6 | 3 · Fay Geçitleri |
| Fay Geçidi (Yamaç) | 4 | 4 köşegen × Çukur Yamacı (uzaklık 55) | Komşu kanatlar | Bekçi II (Sv 25) | 300.000 | 8 | 4 · Birinci Kilit |
| Basınç Kilidi I | 4 | Her kanadın ortası, uzaklık 64 \| 65 | Hadal Eşiği ↔ Çukur Yamacı | Bekçi II (Sv 25) | 300.000 | 8 | 4 · Birinci Kilit |
| Basınç Kilidi II | 4 | Her kanadın ortası, uzaklık 45 \| 46 | Çukur Yamacı ↔ Derin Yamaç | Bekçi III (Sv 35) | 600.000 | 10 | 6 · İkinci Kilit |
| Basınç Kilidi III | 4 | Köşegenler, uzaklık 27 \| 28 | Derin Yamaç ↔ Çukur Ekseni | Bekçi IV (Sv 45) | 1.200.000 | 12 | 8 · Eksen |
| Çekirdek Kapısı | 4 | Her kanadın ortası, uzaklık 11 \| 12 | Çukur Ekseni ↔ Dip | Bekçi V (Sv 50) | 2.500.000 | 15 | 9 · Çekirdek Kapıları |
<!-- /URET:tablo:harita.gecitler -->

**Bir geçidin yaşamı:**

1. **Uyku.** Kronik aşaması gelene kadar geçit kapalıdır ve haritada mühürlü görünür.
2. **Uyanış.** Aşama açılınca geçidin önünde bir **Kadim Bekçi** belirir. Bekçi yalnızca ralliyle
   yenilir (Sonar Enerjisi: kuran 100, katılan 60). Bekçi yenilene kadar geçitten kimse geçemez.
3. **Sahipsiz.** Bekçi yenilince geçit açılır ve sahipsiz kalır; sahipsiz geçitten **herkes** geçer.
   Bekçi bir daha doğmaz.
4. **Ele geçirme.** Bir ittifak geçide garnizon yerleştirir ve dayanıklılığını korur. Başka ittifak
   kazandığı her saldırıda dayanıklılıktan *kuşatma gücünün onda biri* kadar düşer (kuşatma gücü:
   hayatta kalan asker gücü, Delici ×3; [07](07_birlikler_ve_savas.md) §4.5); 0'a inince geçit
   saldırana geçer. Sahip ittifak dayanıklılığı saatte %5 onarır.
5. **Sahiplik.** Sahip ittifakın üyeleri ve izin verdiği **dost ittifaklar** (en çok 3) geçer;
   diğerleri geçemez, ancak saldırabilir. Geçit sahibin bölgesine sayılır ve iki yakadaki kuleleri
   birbirine bağlar ([10](10_ittifak.md)).
6. **Aşınma.** Aşama 10'da (Çekirdek) Basınç Kilidi I–III ve tüm Fay Geçitleri **aşınır**: geçiş
   herkese serbest olur, sahiplik bölge ve Kronik puanı için sürer. Yalnızca Çekirdek Kapıları
   sonuna kadar kontrollü kalır. Böylece geç oyunda küçük ittifaklar derin kuşaklardan kilitle
   dışlanmaz.

Geçitlere saldırı yalnızca **Gelgit Penceresi**'nde yapılır (§10.6); ittifaklar arası savaş
bildirisi bu sınırı kaldırır ([10](10_ittifak.md)).

---

## 4. Kaynak yatakları

### 4.1 Yatak türleri

Haritadaki her yatak gerçek bir derin deniz birikimine dayanır ([18 · Bilim Temeli](18_bilim_temeli.md)).
Hadalit Damarı ise kadim uygarlığın mirasıdır.

<!-- URET:tablo:harita.dugum_turleri -->
| Yatak | Okyanustaki karşılığı | Verdiği | Sv | Kuşak | Özel kural |
| --- | --- | --- | --- | --- | --- |
| Enkaz Alanı | Batık konvoy gövdeleri, eski koloni modülleri, kayıp dron ve kızaklar | Titanyum | 1–7 | Abisal → Eksen | En yaygın yatak. Delici askerler +%10 hızla söker. |
| Kobalt Kabuğu | Dalan deniz dağlarının yamaçlarındaki demir-mangan kabukları (kobalt, nikel, titanyum) | Titanyum; Sv 4+ ek Süperalaşım | 2–5 | Abisal → Çukur Yamacı | Süperalaşım payı Alaşım Dökümhanesi (Reaktör 30) ister; öncesinde payın yerine Titanyum gelir. |
| Nodül Sahası | Abisal düzlükte yatan polimetalik nodüller (mangan, nikel, kobalt, bakır) | Sv 1–3 Titanyum (demir-mangan cevheri); Sv 4–6 Süperalaşım | 1–6 | Abisal, Eşik (sığ) | Güvenli dış halkadaki Süperalaşım kaynağı. Sv 4–6'da her tam seferde %5 şansla Karanlık Oksijen Hücresi (Oksijen Kapsülü); yalnızca deneysel araştırma dalı açıksa (18). |
| Masif Sülfür Bacası | Hidrotermal baca ağzında çökelen bakır-çinko-demir sülfürleri | Enerji (termal hücre) + az Titanyum | 3–8 | Yamaç → Çekirdek | Yalnızca sönmüş baca yığınları yatak olur; canlı bacalar kazılmaz. Her tam seferde %15 şansla teçhizat malzemesi masif sülfür; Sv 6–8'de %2 şansla 10–30 Hadalit. |
| Metan Hidrat Sızıntısı | Soğuk sızıntılarda donmuş metan ve onunla beslenen bakteri matları | Gıda (metanotrof kültür) + Enerji | 2–6 | Eşik → Derin Yamaç | Gıda Laboratuvarı'nın kemosentetik kültürüne başlangıç suşu verir. |
| Balina Leşi | Tabana inen bir balina iskeleti ve etrafında kurulan geçici ekosistem | Biyokütle (son evrede + Gıda) | 2–5 | Abisal → Yamaç | Doğar, üç evreden geçer ve 72 saatte söner; kapasite ×1,5. |
| Hadalit Damarı | Çukurun tabanında, yalnızca karanlıkta parıldayan kadim kristal damarı | Süperalaşım + Hadalit | 7–8 | Dip, Çekirdek | Hadalit yük saymaz; kaptan başına günde en çok 60 Hadalit. Sunucuda en fazla 12 damar. |
<!-- /URET:tablo:harita.dugum_turleri -->

Gerçek dayanaklar `bilim.json`'dan: polimetalik nodüller
<!--v:veri.bilim.mineraller.polimetalik_nodul.derinlik_m.0-->4.000<!--/v-->–<!--v:veri.bilim.mineraller.polimetalik_nodul.derinlik_m.1-->6.000<!--/v-->
m arasındaki abisal düzlüklerde yatar; kobalt zengini kabuklar
<!--v:veri.bilim.mineraller.kobalt_kabuk.derinlik_m.0-->400<!--/v-->–<!--v:veri.bilim.mineraller.kobalt_kabuk.derinlik_m.1-->7.000<!--/v-->
m arasındaki deniz dağı yamaçlarını kaplar ve ağırlıkça ~%<!--v:veri.bilim.mineraller.kobalt_kabuk.bilesim_yuzde.Ti-->1,09<!--/v-->
titanyum taşır; metan hidrat okyanusun <!--v:veri.bilim.mineraller.metan_hidrat.derinlik_m.0-->500<!--/v--> m'den
derin kesiminin çoğunda kararlıdır. Çukura dalan deniz dağları kabuklarını Hadal Eşiği'ne ve Çukur
Yamacı'na taşır; bu yüzden kabuk yatakları Yamaç'ta biter.

İki duyarlılık kuralı (bilim.json → hassas konular):

- **Canlı bacalar kazılmaz.** Masif Sülfür Bacası yatakları yalnızca sönmüş baca yığınlarıdır. Etkin
  bacalar Rezonans Alanı (§6) ve Tür Kataloğu alanıdır; pullu ayaklı salyangoz gibi korunan türlerin
  yaşadığı yerler yatak olarak sunulmaz.
- Kaynak mekaniği tarafsız anlatılır; oyun derin deniz madenciliği tartışmasında taraf tutmaz.

### 4.2 Seviyeler

Yatak hızları **yük/saat** cinsindendir: 1 yük = 1 Titanyum değeri (§4.3). *Taban hız* bonussuz tam
bir seferin hızıdır; *etkin hız* o seviyeyi tipik toplayan oyuncunun bonuslarıyla (araştırma, kahraman,
ittifak teknolojisi, bölge, köken) aldığı hızdır. Etkin hız, F2P gelir modelindeki *yürüyüş başına
toplama hızıyla* eşleşir ([15 · Ekonomi Akışı](15_ekonomi_akisi.md), `oduller.json` → `toplama`).

<!-- URET:tablo:harita.dugum_seviyeleri -->
| Sv | Kapasite (yük) | Taban hız (yük/sa) | Tipik reaktör | Tipik bonus | Etkin hız (yük/sa) | Tek seferle boşaltma | Kuşaklar | Açılış (aşama) |
| ---: | ---: | ---: | --- | --- | ---: | ---: | --- | ---: |
| 1 | 25.000 | 2.000 | 1–7 | ×1,1 | 2.200 | 12 sa 30 dk | Abisal, Eşik (sığ) | 1 |
| 2 | 60.000 | 4.500 | 8–13 | ×1,2 | 5.400 | 13 sa 20 dk | Abisal, Eşik | 1 |
| 3 | 140.000 | 9.000 | 14–19 | ×1,25 | 11.200 | 15 sa 33 dk | Abisal, Eşik | 1 |
| 4 | 250.000 | 14.000 | 20–27 | ×1,35 | 18.900 | 17 sa 51 dk | Abisal, Eşik (derin), Yamaç, Derin Yamaç | 2 |
| 5 | 400.000 | 19.000 | 28–35 | ×1,45 | 27.600 | 21 sa 3 dk | Abisal (nodül), Yamaç, Derin Yamaç, Eksen | 4 |
| 6 | 600.000 | 24.000 | 36–41 | ×1,55 | 37.200 | 1 g 1 sa | Abisal (nodül), Derin Yamaç, Eksen, Dip | 6 |
| 7 | 850.000 | 29.000 | 42–47 | ×1,65 | 47.800 | 1 g 5 sa 18 dk | Eksen, Dip | 8 |
| 8 | 1.200.000 | 35.000 | 48–50 | ×1,75 | 61.200 | 1 g 10 sa 17 dk | Dip, Çekirdek | 10 |
<!-- /URET:tablo:harita.dugum_seviyeleri -->

Erken oyunda seferi taşıma sınırı bitirir (Reaktör 10'da 2–3 saat); Reaktör 20'den sonra yük bol
gelir ve seferi oyuncunun dönüş saati bitirir (4–12 saat). Bir yatak böyle 2–3 uzun sefer taşır; tek
seferle boşaltma süresi tablodadır. Kronik aşaması bir kuşakta görünebilecek en yüksek yatak seviyesini
de sınırlar (§5.2).

### 4.3 Yük ağırlıkları

Taşıma kapasitesi askerlerden ve denizaltıdan gelir ([07](07_birlikler_ve_savas.md),
[08](08_denizaltilar_basinc_seferler.md)); haritanın tek kuralı her kaynağın bir yük ağırlığı olmasıdır.

<!-- URET:tablo:harita.yuk_agirliklari -->
| Kaynak | Yük / birim | Not |
| --- | ---: | --- |
| Titanyum | 1 | Ölçü birimi: 1 yük = 1 Titanyum değeri |
| Biyokütle | 1 | Gelir ve harcamada Titanyumun 0,4 katı akar; birim değeri eşit sayılır |
| Gıda | 1 | — |
| Enerji | 0,5 | Üste bol üretilir; 1 yükte 2 birim |
| Oksijen | 0,5 | 1 yükte 2 birim |
| Süperalaşım | 6.000 | Kapı kaynağı; toplama hızı ayrı tanımlanır (aşağıda) |
| Hadalit | 0 | Yük saymaz; günlük harita sınırı 60 |
<!-- /URET:tablo:harita.yuk_agirliklari -->

### 4.4 Tür ve seviyeye göre verim

Taban hızların kaynağa dağılımı (bonussuz, tek sefer, saatte):

<!-- URET:tablo:harita.verim -->
| Sv | Enkaz Alanı | Kobalt Kabuğu | Nodül Sahası | Masif Sülfür Bacası | Metan Hidrat | Balina Leşi | Hadalit Damarı |
| ---: | --- | --- | --- | --- | --- | --- | --- |
| 1 | 2.000 Ti | — | 1.800 Ti | — | — | — | — |
| 2 | 4.500 Ti | 3.600 Ti | 4.050 Ti | — | 3.150 GD + 2.700 EN | 4.500 BK | — |
| 3 | 9.000 Ti | 7.200 Ti | 8.100 Ti | 14.400 EN + 1.800 Ti | 6.300 GD + 5.400 EN | 9.000 BK | — |
| 4 | 14.000 Ti | 11.200 Ti + 0,5 SA | 1,0 SA | 22.400 EN + 2.800 Ti | 9.800 GD + 8.400 EN | 14.000 BK | — |
| 5 | 19.000 Ti | 15.200 Ti + 0,7 SA | 1,4 SA | 30.400 EN + 3.800 Ti | 13.300 GD + 11.400 EN | 19.000 BK | — |
| 6 | 24.000 Ti | — | 1,9 SA | 38.400 EN + 4.800 Ti | 16.800 GD + 14.400 EN | — | — |
| 7 | 29.000 Ti | — | — | 46.400 EN + 5.800 Ti | — | — | 2,6 SA + 1,0 HD |
| 8 | — | — | — | 56.000 EN + 7.000 Ti | — | — | 3,4 SA + 1,4 HD |
<!-- /URET:tablo:harita.verim -->

- **Kobalt Kabuğu** ve **Nodül Sahası**'nın Süperalaşım payı Alaşım Dökümhanesi ister (Reaktör 30).
  Dökümhanesi olmayan oyuncu kabuktan ve Sv 4–6 nodülden payın yerine Titanyum alır; Süperalaşım
  Reaktör 30'dan önce biriktirilemez. Bu, [02](02_tempo_ve_denge.md)'deki Süperalaşım kapısını korur.
- Ücretsiz oyuncunun nodül sahasından günlük Süperalaşım'ı (tek sefer, günde ~10–12 saat: R30'da ~16,
  yıl sonunda ~40–50) `oduller.json` → `alasim_dokumu` tablosundaki *Nodül sahası* sütunudur.
- **Hadalit Damarı**'nın Hadalit'i yük saymaz ama kaptan başına günde en çok
  <!--v:veri.harita.hadalit_damari.gunluk_sinir-->60<!--/v--> Hadalit toplanır; sunucuda aynı anda en
  çok <!--v:veri.harita.hadalit_damari.en_fazla-->12<!--/v--> damar bulunur.

### 4.5 Balina Leşi

Balina leşi gerçek bir **balina düşüşünün** oyun ölçeğine sıkıştırılmış hâlidir. Gerçekte süreç
on yıllarca sürer: hareketli çöpçüler
(<!--v:veri.bilim.kurallar.balina_dususu.asamalar.0.sure-->~2 yıla kadar (balinanın boyuna göre)<!--/v-->),
zenginleşme fırsatçıları
(<!--v:veri.bilim.kurallar.balina_dususu.asamalar.1.sure-->~2 yıla kadar<!--/v-->) ve kükürt sevenler
(<!--v:veri.bilim.kurallar.balina_dususu.asamalar.2.sure-->~50 yıla, belki bir yüzyıla kadar<!--/v-->).
Oyunda aynı üç evre 72 saate iner:

<!-- URET:tablo:harita.balina_evreleri -->
| Evre | Saat | Verim | Taranabilen | Ne olur |
| --- | --- | --- | --- | --- |
| Leşçil evre | 0–24 | Biyokütle ×1,5 | Leşçil balıklar ve amfipodlar (Tür Kataloğu) | Yumuşak doku hızla tüketilir; leşin doğal erimesi en hızlı burada. |
| Zenginleşme evresi | 24–48 | Biyokütle ×1,0 | Kemik yiyen kurtlar, çok kıllı solucanlar | Kemikler ve çevredeki tortu organik maddeyle zenginleşir. |
| Sülfofilik evre | 48–72 | Biyokütle %50 + Gıda %50, ×0,7 | Bakteri matları | Kemiklerdeki yağ parçalanırken sülfür bakterileri yerleşir. Oyunda 72. saatte söner. |
<!-- /URET:tablo:harita.balina_evreleri -->

- **Doğuş.** Her kanatta 6 saatte bir 3 leş düşer (Abisal → Çukur Yamacı, Sv 2–5). Düşüş haritada
  sessiz bir işaretle görünür; sonar menzilindekiler bildirim alır.
- **Kapasite** aynı seviyedeki yatağın 1,5 katıdır; toplanmasa da **saatte %1 erir** (çöpçüler yer).
- Leşi çevreleyen gerçek canlılar (miksinler, amfipodlar, kemik yiyen kurtlar) **ortam canlısıdır**:
  saldırılmaz, yalnızca taranır (K-25). Leşin başında bir Uçurum Sürüngeni sürüsü belirebilir;
  onlar avlanır.

### 4.6 Yoğunluk ve yeniden doğma

<!-- URET:tablo:harita.yogunluk -->
| Kuşak | 100 karede yatak | Yaklaşık yatak | Tür dağılımı |
| --- | ---: | ---: | --- |
| Abisal Düzlük | 10 | 1.144 | Enkaz %35 · Nodül %35 · Kabuk %15 · Leş %15 |
| Hadal Eşiği | 6 | 715 | Enkaz %45 · Kabuk %20 · Hidrat %20 · Nodül (sığ) %10 · Leş %5 |
| Çukur Yamacı | 6 | 502 | Enkaz %35 · Hidrat %25 · Kabuk %20 · Baca %15 · Leş %5 |
| Derin Yamaç | 6 | 315 | Baca %35 · Enkaz %30 · Hidrat %20 · Kabuk %15 |
| Çukur Ekseni | 5 | 125 | Baca %50 · Enkaz %50 |
| Dip ve Çekirdek | 4 | 21 | Hadalit Damarı (en çok 12) · geri kalanı Sv 6–8 Masif Sülfür Bacası |
<!-- /URET:tablo:harita.yogunluk -->

- Tükenen yatak 30 dakika sonra aynı kuşakta rastgele bir boş karede yeniden doğar; seviyesi kuşağın
  ve Kronik aşamasının izin verdiği aralıktan seçilir.
- Bir yatakta aynı anda tek sefer toplar. Toplama sürerken sefer saldırıya açıktır (§8.1).
- İttifak bölgesindeki yataklarda toplama **+%20** hızlıdır; İttifak Madenleri ayrıdır ([10](10_ittifak.md)).

### 4.7 Toplama kuralları

1. Sefer yatağa varır, yük dolana ya da yatak bitene kadar toplar, eve döner. Yük, dönüş yolunda
   taşınır ve eve varınca cüzdana yazılır.
2. Geri çağırma ücretsizdir; o ana kadar toplanan yük korunur.
3. Delici askerler taşıma kapasitesine en çok katkıyı verir (sözlük: "en yüksek taşıma"); Enkaz
   Alanı'nda +%10 hızla söker.
4. Kalkan altındaki oyuncu toplayabilir; toplama kalkanı kırmaz (§10.1).
5. Toplanan kaynak **tavana takılmaz**; tavan üstü kısım Kaynak Kapsülü olarak çantaya düşer
   ([02](02_tempo_ve_denge.md), §7).

---

## 5. PvE

### 5.1 Sonar Enerjisi

Haritadaki her PvE saldırısı **Sonar Enerjisi** harcar (K-22). Sonar Enerjisi sonarın şarjıdır:
denizaltının aktif sonar darbeleri hedefi kilitler.

| Değer | Sayı |
|---|---|
| Kapasite | <!--v:veri.harita.sonar_enerjisi.kapasite-->500<!--/v--> (Radar Merkezi 10, 20, 30, 40'ta +<!--v:veri.harita.sonar_enerjisi.radar_bonus-->100<!--/v--> → en çok <!--v:veri.harita.sonar_enerjisi.dogal_tavan-->900<!--/v-->) |
| Yenilenme | Saatte <!--v:veri.harita.sonar_enerjisi.yenilenme_saat-->30<!--/v--> (günde <!--v:veri.harita.sonar_enerjisi.gunluk-->720<!--/v-->) |
| Eşyayla tavan üstü | En çok <!--v:veri.harita.sonar_enerjisi.esya_tavani-->3.000<!--/v--> |
| Sonar Pili | Küçük +<!--v:veri.harita.sonar_enerjisi.pil_kucuk-->50<!--/v--> · orta +<!--v:veri.harita.sonar_enerjisi.pil_orta-->200<!--/v--> · büyük +<!--v:veri.harita.sonar_enerjisi.pil_buyuk-->500<!--/v--> |
| Zincir indirimi | Eve dönmeden saldırılan her yeni hedef −%<!--v:veri.harita.sonar_enerjisi.zincir_indirim-->25<!--/v--> (seferde en çok <!--v:veri.harita.sonar_enerjisi.zincir_hedef-->4<!--/v--> hedef) |
| Kaybedilen saldırı | Bedelin %<!--v:veri.harita.sonar_enerjisi.kayip_iade-->50<!--/v-->'si iade |

<!-- URET:tablo:harita.sonar_bedelleri -->
| Hedef | Bedel | Zincirde (−%25) | Not |
| --- | --- | --- | --- |
| Uçurum Sürüngenleri | 40 | 30 | Tek sefer |
| Siren Sürüsü | 50 | 38 | Tek sefer |
| Kadim Muhafız Devriyesi | 60 | 45 | Sv 40+ ralli de olabilir |
| Kadim Bekçi (geçit, Rezonans Alanı) | 100 / 60 | — | Ralliyi kuran / katılan |
| Demir Leviathan Yuvası | 150 / 100 | — | Ralliyi kuran / katılan |
<!-- /URET:tablo:harita.sonar_bedelleri -->

Bir günün yenilenmesi ~14 Siren Sürüsü saldırısına, zincirle ~18'e yeter. PvE'de askerler **ölmez,
yalnızca yaralanır** ([07](07_birlikler_ve_savas.md), K-21); ücretsiz oyuncunun ordusu avla erimez.
Zincir indirimi, ardı ardına gelen savaşlarda yaralanan askerlerle zayıflayan seferin riskini ödüllendirir.

### 5.2 Düşmanlar

Saldırılan her hedef derinin düşmanlarındandır (K-25); okyanusun canlıları haritada yalnızca ortam canlısıdır.

<!-- URET:tablo:harita.dusmanlar -->
| Düşman | Tür | Sv | Kuşak | Davranış | Ödül |
| --- | --- | --- | --- | --- | --- |
| Uçurum Sürüngenleri | Sürü | 1–25 | Abisal, Eşik, Yamaç | Yakın dövüşçü sürü; Zıpkıncılara karşı zayıf. | Kahraman deneyimi, kitin, küçük Kaynak Kapsülü |
| Siren Sürüsü | Sürü | 8–35 | Eşik, Yamaç, Derin Yamaç | Yankı saldırısı: yenilen seferin dönüş hızını %20 düşürür. | Kahraman deneyimi, Anı Kristali şansı, bazalt camı |
| Kadim Muhafız Devriyesi | Devriye | 15–50 | Yamaç → Dip | 2–3 birimlik biyomekanik devriye; Ark Askerlerine dirençli. | Basınç Kodeksi şansı (Sv 25+), kadim parça, sintaktik köpük |
| Kadim Bekçi | Bekçi (ralli) | I–V | Geçitler, Rezonans Alanları, Çukur Kalbi | Geçit ve alan uyandığında, sonra her Gelgit Penceresi başında doğar. | Rezonans Kristalleri, Basınç Kodeksi |
| Demir Leviathan Yuvası | Yuva (ralli) | Yuva 1–6 | Derin Yamaç → Çekirdek | Ralli olmadan saldırılamaz; verilen hasar 1 saat kalır. | Kadim Şema, Basınç Kodeksi, Süperalaşım, Hadalit |
| Hadal Titanı | Sunucu olayı | — | Çekirdek | Kronik aşama 11 ile uyanır; dalga dalga ralli. | Ayrıntı 14 · Son Oyun |
<!-- /URET:tablo:harita.dusmanlar -->

Kronik aşaması bir kuşakta doğabilecek en yüksek düşman ve yatak seviyesini sınırlar; böylece erken
oyunda öne fırlayan bir ödeyen oyuncu, sunucudan önce derinin ödüllerini toplayamaz:

<!-- URET:tablo:harita.kronik_dusman -->
| Kronik aşaması | En yüksek düşman Sv | En yüksek yatak Sv |
| --- | ---: | ---: |
| 1 · İniş | 10 | 3 |
| 2 · İlk Yankı | 15 | 4 |
| 3 · Fay Geçitleri | 20 | 4 |
| 4 · Birinci Kilit | 25 | 5 |
| 5 · Yamaç Çekişmesi | 30 | 5 |
| 6 · İkinci Kilit | 35 | 6 |
| 7 · Akıntı Çağrısı | 40 | 6 |
| 8 · Eksen | 45 | 7 |
| 9 · Çekirdek Kapıları | 50 | 7 |
| 10 · Çekirdek | 50 | 8 |
<!-- /URET:tablo:harita.kronik_dusman -->

- Her düşman seviyesinin **ilk zaferi** bir kez Kaynak Kapsülü verir (kaptan başına).
- Kadim Muhafız Devriyesi Sv 25–34'te %20, Sv 35+'ta %30 şansla 1 **Basınç Kodeksi** düşürür.
  Kodeks Savaş Kubbesi 4 (×300) ve 5 (×800) için gerekir ([Savaş Kubbesi](binalar/war_dome.md)).
- PvE ödüllerinin kaynak kısmı küçüktür (ilk zafer kapsülleri ve küçük kapsüller); gelir modelinde
  etkinlik payına sayılır. Asıl değer kahraman deneyimi, teçhizat malzemesi
  ([17 · Teçhizat](17_techizat.md)) ve Kodeks'tir.

### 5.3 Demir Leviathan Yuvaları

Leviathan yuvaları yalnızca **ralliyle** saldırılan yuvalardır; geç oyunun iki malzemesini,
**Kadim Şema**'yı ve **Basınç Kodeksi**'ni haritaya taşırlar.

<!-- URET:tablo:harita.leviathan -->
| Yuva Sv | Kuşak | Adet | Açılış | En az katılımcı | Önerilen Savaş Kubbesi | Basınç Kodeksi | Süperalaşım | Hadalit | Kadim Şema |
| ---: | --- | ---: | --- | ---: | --- | --- | ---: | --- | --- |
| 1 | Derin Yamaç | 8 | 6 · İkinci Kilit | 3 | Sv 2 | 2–3 | 15 | — | — |
| 2 | Derin Yamaç | 4 | 6 · İkinci Kilit | 4 | Sv 2 | 3–4 | 25 | — | — |
| 3 | Çukur Ekseni | 6 | 8 · Eksen | 5 | Sv 3 | 4–6 | 40 | 20 | — |
| 4 | Çukur Ekseni | 4 | 8 · Eksen | 6 | Sv 3 | 5–8 | 60 | 30 | %10 |
| 5 | Dip | 4 | 10 · Çekirdek | 8 | Sv 4 | 6–10 | 90 | 40 | %25 |
| 6 | Çekirdek | 2 | 10 · Çekirdek | 10 | Sv 5 | 8–12 | 130 | 60 | 1 (kesin) |
<!-- /URET:tablo:harita.leviathan -->

Ödüller katılımcı başınadır; ralli lideri +%<!--v:veri.harita.leviathan_kurallar.lider_ek-->20<!--/v--> alır.

- Kaptan başına günde <!--v:veri.harita.leviathan_kurallar.gunluk_odul-->3<!--/v--> yuva ödülü
  (sonrakiler ittifak deneyimi ve Av Hediyesi verir, kişisel ödül vermez).
- Yenilen yuva <!--v:veri.harita.leviathan_kurallar.yeniden_dogma_saat-->4<!--/v--> saat sonra aynı
  kuşakta yeniden doğar. Verilen hasar <!--v:veri.harita.leviathan_kurallar.hasar_kalis_saat-->1<!--/v-->
  saat kalır: küçük ittifaklar yuvayı iki ralliyle düşürebilir.
- Her yuva zaferi tüm ittifaka bir **Av Hediyesi** düşürür (ittifak başına günde en çok 3;
  [10](10_ittifak.md)): ücretsiz oyuncuların ürettiği hediyeler. Leviathan Avı **etkinliği** (dalgalı,
  Reaktör 18'den itibaren) ayrıdır ve [13](13_etkinlikler_ve_sezonlar.md)'ündür; harita yuvaları
  Kronik aşama 6'dan sonra onun kalıcı karşılığıdır.
- Reaktör 49 → 50 bir Kadim Şema ister ([Çekirdek Reaktör](binalar/core_reactor.md)). Ücretsiz oyuncu
  bunu üç yoldan alır: Sv 6 yuvası (kesin), Sv 4–5 yuvası (şans) ve Kronik aşama 10'un kişisel hedefi
  ([16](16_kronik_kokenler_profil.md)). Hadalit karşılığı: [12 · The Syndicate](12_syndicate.md).

Hadal Titanı bir yuva değil, sunucu olayıdır: [14 · Son Oyun](14_son_oyun.md).

---

## 6. Rezonans Alanları

Rezonans Alanları, tutan ittifaka sürekli etki veren özel yerlerdir. Üçü gerçek derin deniz yaşam
alanıdır, biri kadim uygarlığın kalıntısıdır. Adını sonarın bu yerlerde duyduğu düzenli titreşimden
alır; hikâyenin sonunda bu titreşimin Çekirdek'ten geldiği anlaşılır ([14](14_son_oyun.md)).

<!-- URET:tablo:harita.rezonans_turleri -->
| Alan | Okyanustaki karşılığı | İttifak etkisi | Kademe I | Kademe II | Kademe III |
| --- | --- | --- | --- | --- | --- |
| Hidrotermal Baca Sahası | Kara ve beyaz bacalar, sıcak su kolonları | Asker eğitim hızı | %5 | %8 | %12 |
| Soğuk Sızıntı Bahçesi | Metan sızıntısı; midye, deniz tarağı ve tüp kurdu kolonileri | İyileştirme hızı | %8 | %12 | %18 |
| Büyük Nodül Ovası | Kilometrelerce süren nodül döşeği | Toplama hızı | %8 | %12 | %18 |
| Kadim Kalıntı | Hadalit damarlı biyomekanik yapı kalıntısı | Asker saldırısı | %2 | %3 | %5 |
<!-- /URET:tablo:harita.rezonans_turleri -->

<!-- URET:tablo:harita.rezonans_dagilim -->
| Kademe | Adet | Yer | Uyanış (Kronik) | Bekçi | Garnizon (sefer) |
| --- | ---: | --- | --- | --- | ---: |
| I | 16 | Baca ×4 ve Sızıntı ×4 Hadal Eşiği'nde; Nodül ×4 Abisal'de; Kalıntı ×4 Çukur Yamacı'nda (her kanatta birer) | 2 · İlk Yankı (Kalıntı I: 4) | Bekçi I (Sv 15) | 6 |
| II | 8 | Sızıntı ×2 Yamaç'ta; Nodül ×2 Abisal'de; Baca ×2 ve Kalıntı ×2 Derin Yamaç'ta | 5 · Yamaç Çekişmesi (Derin Yamaç'takiler: 6) | Bekçi II (Sv 25) · Derin Yamaç'ta Bekçi III (Sv 35) | 10 |
| III | 4 | Baca ve Kalıntı Çukur Ekseni'nde; Sızıntı Derin Yamaç'ta; Nodül Abisal'in tek büyük ovasında | 8 · Eksen | Bekçi IV (Sv 45) | 15 |
<!-- /URET:tablo:harita.rezonans_dagilim -->

**Kurallar:**

1. **Aynı tür birikmez.** Bir ittifak aynı türden birden çok alan tutarsa yalnızca en yüksek kademenin
   etkisi işler. Farklı türler toplanır. Bu, tek bir dev ittifakın bütün alanları toplamasını anlamsız
   kılar.
2. Bir ittifak aynı anda en çok <!--v:veri.harita.rezonans_kurallar.en_fazla-->8<!--/v--> alan tutar.
3. **Dinginlik.** Ele geçirilen alan <!--v:veri.harita.rezonans_kurallar.dinginlik_saat-->72<!--/v-->
   saat saldırıya kapalıdır. Sonra yalnızca Gelgit Penceresi'nde saldırılabilir (§10.6). Her alan
   böylece en sık üç günde bir el değiştirir.
4. **Garnizon.** Alanı tutan ittifak garnizon yerleştirir (kapasite tabloda). Garnizon yenilip boşalınca
   alan saldırana geçer.
5. **Bekçi ve kristaller.** Dinginlikte olmayan her alanın yanında her Gelgit Penceresi'nin başında bir
   **Kadim Bekçi** doğar. Bekçiyi yenen ralli, çevreye
   <!--v:veri.harita.rezonans_kurallar.kristal_adet-->4–8<!--/v--> **Rezonans Kristali** saçar; her
   kristali ilk dokunan sefer alır (ittifak ayrımı yoktur).

<!-- URET:tablo:harita.kristaller -->
| Kristal | Etki (kişisel) | Süre |
| --- | --- | ---: |
| Akıntı Kristali | +%10 yürüyüş hızı | 4 sa |
| Kabuk Kristali | +%4 asker savunması | 4 sa |
| Diken Kristali | +%3 asker saldırısı | 4 sa |
| Bereket Kristali | +%15 toplama hızı | 4 sa |
| Yankı Kristali | +%20 Sonar Enerjisi yenilenmesi | 4 sa |
<!-- /URET:tablo:harita.kristaller -->

Aynı tür kristal süreyi yeniler, birikmez; aynı anda en çok
<!--v:veri.harita.rezonans_kurallar.kristal_ayni_anda-->2<!--/v--> farklı kristal işler.

---

## 7. Çukur Kalbi ve Egemen

Haritanın tam ortasında, 3 × 3 karelik **Çukur Kalbi** durur. Onu tutan ittifakın lideri sunucunun
**Egemen**'idir.

| Kural | Değer |
|---|---|
| Konum | <!--v:veri.harita.cukur_kalbi.konum-->Merkez 3 × 3 (99–101, 99–101)<!--/v--> |
| Açılış | Kronik aşama <!--v:veri.harita.cukur_kalbi.acilis_asama-->10<!--/v--> (Çekirdek); ilk Taht Savaşı açılıştan <!--v:veri.harita.cukur_kalbi.ilk_taht_saat-->24<!--/v--> saat sonra |
| Bekçi | <!--v:veri.harita.cukur_kalbi.bekci-->Kadim Muhafız Sv 50 garnizonu<!--/v--> (yalnızca ilk Taht Savaşı'nda) |
| Taç şartı | Kalp'i <!--v:veri.harita.cukur_kalbi.tutma_saat-->6<!--/v--> saat **kesintisiz** tutmak |
| Taht Savaşı | <!--v:veri.harita.cukur_kalbi.taht_savasi_saat-->48<!--/v--> saat açık; içinde kimse 6 saati doldurmazsa taç eski sahibinde kalır |
| Egemenlik | <!--v:veri.harita.cukur_kalbi.egemenlik_gun-->5<!--/v--> gün Dinginlik; döngü <!--v:veri.harita.cukur_kalbi.dongu_gun-->7<!--/v--> gün |
| Garnizon | <!--v:veri.harita.cukur_kalbi.garnizon-->30<!--/v--> sefer |

- Taç ittifakındır; Egemen o ittifakın R5'idir. R5 değişirse Egemenlik yeni R5'e geçer.
- Kalp'e ulaşan dört Kadim Tünel P9 gövde ister (§2.3); ralliler lider gövdesiyle iner.
- Egemenin adı Kronik Feneri'ndeki **Egemenler Kaydı**'na yazılır ([16](16_kronik_kokenler_profil.md)).
  Egemen, Akıntı Savaşı'nda çukurunun sancaktarıdır ([13](13_etkinlikler_ve_sezonlar.md)).

### 7.1 Fermanlar

Egemen her gün bir **Ferman** seçer; ferman 24 saat **bütün sunucuya** işler, rakip ittifaklara da.
Egemenlik böylece bir ayrıcalık değil, sunucuya karşı bir sorumluluk olur.

<!-- URET:tablo:harita.fermanlar -->
| Ferman | Etki (24 sa) | Sınır |
| --- | --- | --- |
| Bereket Fermanı | Tüm sunucu +%10 toplama hızı | Günde 1 ferman |
| Şifa Fermanı | Tüm sunucu +%10 iyileştirme hızı | Günde 1 ferman |
| Akıntı Fermanı | Tüm sunucu +%10 yürüyüş hızı | Günde 1 ferman |
| Bilgelik Fermanı | Tüm sunucu +%5 araştırma hızı | Günde 1 ferman |
| Ustalık Fermanı | Tüm sunucu +%5 inşa hızı | Günde 1 ferman |
| Sükûnet Fermanı | Hadal Eşiği'ndeki üslere 24 saat saldırı yapılamaz | 7 günde en çok 1 |
<!-- /URET:tablo:harita.fermanlar -->

### 7.2 Unvanlar

Egemen kaptanlara iyi ve kötü unvanlar verir.

<!-- URET:tablo:harita.unvanlar -->
| Unvan | Tür | Etki (24 sa) | Aynı anda |
| --- | --- | --- | ---: |
| Çukur Mühendisi | İyi | +%5 inşa hızı | 1 |
| Derin Hekim | İyi | +%10 iyileştirme hızı | 2 |
| Akıntı Kaptanı | İyi | +%10 yürüyüş hızı | 2 |
| Bereket Bekçisi | İyi | +%10 toplama hızı | 2 |
| Sonar Ustası | İyi | +%15 Sonar Enerjisi yenilenmesi | 2 |
| Paslı Gövde | Kötü | −%5 asker savunması | 1 |
| Sisli Sonar | Kötü | −%10 yürüyüş hızı | 1 |
| Sızdıran Ambar | Kötü | −%10 toplama hızı | 1 |
| Ağır Pervane | Kötü | −%5 eğitim hızı | 1 |
<!-- /URET:tablo:harita.unvanlar -->

Adil kullanım kuralları:

- Unvan <!--v:veri.harita.cukur_kalbi.unvan_suresi_saat-->24<!--/v--> saat sürer; Egemen bir unvanı
  en sık <!--v:veri.harita.cukur_kalbi.unvan_degistirme_saat-->4<!--/v--> saatte bir değiştirir.
- Kaptanın aynı anda tek unvanı olur.
- Kötü unvan yalnızca Reaktör <!--v:veri.harita.cukur_kalbi.kotu_unvan_min_reaktor-->20<!--/v--> ve
  üstündeki kaptana, aynı kaptana en sık
  <!--v:veri.harita.cukur_kalbi.kotu_unvan_tekrar_saat-->72<!--/v--> saatte bir verilir; Yeni Kaptan
  Koruması'ndaki oyuncuya hiç verilmez.
- Unvanı alan kaptan sessiz bir bildirim alır; dünya sohbetine yayın yapılmaz.

---

## 8. Yürüyüş, ralli, takviye, keşif

### 8.1 Yürüyüş ve durdurma

- Her **Denizaltı Hangarı** bir yürüyüş kuyruğudur, en çok 4 ([Denizaltı Hangarı](binalar/submarine_hangar.md)).
- Yürüyüşler haritada **canlı ve görünür** çizgilerdir. Bir yürüyüşü görüş alanında olan herkes
  görür: kendi üssünün çevresi ve ittifak bölgen sürekli, bir sonar ping'inin taradığı alan ping'den
  sonra 10 dakika ([08](08_denizaltilar_basinc_seferler.md) §6). İttifak üyelerinin yürüyüşleri her
  zaman görünür.
- **Seyir süresi** 08'deki formüldür: mesafe Chebyshev karesidir ve yürüyüş en yavaş asker türünün
  hızıyla gider (taban hızda kare başına 1 dakika; kenardan merkeze ~100 dakika).
- **Durdurma.** Hareket hâlindeki bir yürüyüş hedef alınabilir; savaş iki çizginin buluştuğu karede
  çözülür. Yenilen ve yük taşıyan yürüyüş taşıdığının
  %<!--v:veri.harita.yuruyus.durdurma_yuk-->30<!--/v-->'unu saldırana bırakır; güç farkı kuralı
  burada da geçerlidir (§10.5).
- **Hız çarpanları.** Taban hız askerlerden ve denizaltıdan gelir ([07](07_birlikler_ve_savas.md),
  [08](08_denizaltilar_basinc_seferler.md)). Haritanın eklediği çarpanlar: kendi ittifak bölgende
  +%<!--v:veri.harita.yuruyus.kendi_bolge-->30<!--/v-->, başka ittifakın bölgesinde
  <!--v:veri.harita.yuruyus.yabanci_bolge|yuzde-->−%15<!--/v-->. Geçitler yalnızca kapıdır; hız cezası yoktur.
- **Geri çağırma** ücretsizdir; yürüyüş geldiği yoldan döner.

### 8.2 Ralli

Ralli, birden çok kaptanın askerlerini liderin denizaltısında birleştirdiği toplu saldırıdır. Ralli
boyutu **Savaş Kubbesi**'nden gelir ([Savaş Kubbesi](binalar/war_dome.md); seviyeleri
Reaktör <!--v:veri.binalar.binalar.war_dome.seviye_esleme-->16, 20, 30, 40, 45<!--/v--> kapılarına bağlı).

<!-- URET:tablo:harita.ralli -->
| Savaş Kubbesi | Reaktör | Katılımcı (lider dahil) | Ralli kapasitesi (liderin sefer kapasitesi ×) | Toplanma süreleri |
| --- | --- | --- | --- | --- |
| — | < 16 | — | — | Ralliye katılabilir, kuramaz |
| 1 | 16 | 6 | ×3 | 5 dk · 15 dk · 1 sa |
| 2 | 20 | 8 | ×4 | 5 dk · 15 dk · 1 sa |
| 3 | 30 | 12 | ×6 | 5 dk · 15 dk · 1 sa · 4 sa (PvE) |
| 4 | 40 | 16 | ×8 | 5 dk · 15 dk · 1 sa · 4 sa (PvE) |
| 5 | 45 | 20 | ×10 | 5 dk · 15 dk · 1 sa · 4 sa (PvE) |
<!-- /URET:tablo:harita.ralli -->

- Her katılımcı ralliye bir hangarını ayırır. Lider toplanma süresini seçer; süre dolunca ralli yola
  çıkar, gelmeyenler kalır.
- Ralli hedefleri: oyuncu üssü, Leviathan yuvası, Kadim Bekçi, geçit ve alan garnizonu, ittifak
  yapıları, Çukur Kalbi, Hadal Titanı.
- 4 saatlik toplanma yalnızca PvE hedefleri içindir; dünyanın dört bir yanındaki üyelerin uyanmasına
  zaman tanır.
- Ralli kapasitesine ittifak teknolojisi (Ralli Komutası) eklenir ([10](10_ittifak.md)).

### 8.3 Takviye ve garnizon

İttifak arkadaşının üssüne asker gönderilebilir; üssü savunurken onun askerleriyle birlikte savaşır.
Kapasiteyi **alıcının İttifak Merkezi** belirler ([İttifak Merkezi](binalar/alliance_hub.md)):
`kapasite = (0,5 + 0,05 × Sv) × alıcının sefer kapasitesi`, gönderen sayısı `2 + ⌊Sv / 10⌋`
(en çok 7). Aynı bina ittifak yardım sınırını da belirler (10 + ⌊Sv / 5⌋, en çok 20).

<!-- URET:tablo:harita.takviye -->
| İttifak Merkezi Sv | Takviye kapasitesi (alıcının sefer kapasitesi ×) | En çok gönderen | Yardım sınırı |
| ---: | --- | ---: | ---: |
| 1 | ×0,55 | 2 | 10 |
| 5 | ×0,75 | 2 | 11 |
| 10 | ×1,0 | 3 | 12 |
| 15 | ×1,25 | 3 | 13 |
| 20 | ×1,5 | 4 | 14 |
| 25 | ×1,75 | 4 | 15 |
| 30 | ×2,0 | 5 | 16 |
| 35 | ×2,25 | 5 | 17 |
| 40 | ×2,5 | 6 | 18 |
| 45 | ×2,75 | 6 | 19 |
| 50 | ×3,0 | 7 | 20 |
<!-- /URET:tablo:harita.takviye -->

Geçit, Rezonans Alanı ve Çukur Kalbi **garnizonu** ise o yapının kendi kapasitesiyle sınırlıdır (sefer
sayısı tablolarda). Garnizondaki asker o yapının savunma bonuslarını alır; yapının sahibi ittifaktır,
asker kaptanındır. Takviye ve garnizon her zaman geri çağrılabilir.

### 8.4 Keşif

Keşif, **Radar Merkezi**'nin sonar dronuyla yapılır; dron sayısı, menzil ve ping bedeli
[08](08_denizaltilar_basinc_seferler.md) §6'da, keşif raporunun Radar seviyesine göre açılan katmanları
ve gelen saldırı uyarısı [07](07_birlikler_ve_savas.md) §4.8'dedir. Haritanın eklediği kurallar:

- Keşif kalkanı kırmaz; kalkanlı üs ve Yeni Kaptan Koruması'ndaki üs keşfedilemez.
- Geçit, Rezonans Alanı ve Çukur Kalbi garnizonu da keşfedilebilir; rapor garnizondaki toplam asker
  ve sahip ittifakı gösterir.
- Hedefin Radar'ı 10 seviye yüksekse rapor bir katman eksik gelir (sonar karıştırma, 07).

---

## 9. Sis, buluntular ve Tür Kataloğu

Haritanın görülmemiş kısmı **sonar gölgesidir**: koyu, taneli, yalnızca kuşak sınırları seçilir.
Gölge kişiseldir; açılan kare açık kalır.

- **Görüş kaynakları:** üssün çevresi (14 kare, `world.viewRadius`), ittifak bölgesi, sonar dronu
  ping'leri ([08](08_denizaltilar_basinc_seferler.md) §6) ve ittifak teknolojisi Derin Haritacılık.
- Gölgedeki yataklar ve düşmanlar görünmez; gölgedeki yürüyüşler yalnızca ittifak üyelerininse görünür.
- Ping'in ortaya çıkardığı **buluntular** (batık enkaz, terk edilmiş kriyo-kapsüller, tür yankısı,
  kalıntı izi) oyuncu başına bir kez alınır; türleri ve ödülleri 08'dedir. Haritadaki dağılımları:

<!-- URET:tablo:harita.buluntu_yogunlugu -->
| Kuşak | Batık enkaz | Terk edilmiş kriyo-kapsül | Kapsül sayısı | Kalıntı izi |
| --- | ---: | --- | ---: | --- |
| Abisal Düzlük | 114 | Paslı (I) | 34 | — |
| Hadal Eşiği | 95 | Paslı (I) | 36 | — |
| Çukur Yamacı | 50 | Paslı (I) sığ, Mühürlü (II) derin basamakta | 25 | — |
| Derin Yamaç | 26 | Mühürlü (II) | 16 | 5 |
| Çukur Ekseni | 10 | Mühürlü (II) sığ, Kadim (III) derin basamakta | 7 | 5 |
| Dip ve Çekirdek | 2 | Kadim (III) | 2 | 3 |
<!-- /URET:tablo:harita.buluntu_yogunlugu -->

Buluntular sabittir: her sunucu açılışında tohumdan üretilir, aynı sunucuda herkes için aynı yerdedir.
Kriyo-kapsülün türü basamağa göre değişir (Paslı P1–P3, Mühürlü P4–P7, Kadim P8–P10). Kalıntı izleri
Derin Yamaç'tan içeride başlar ve [14](14_son_oyun.md)'teki hikâye içeriğine bağlanır.

### 9.1 Tür Kataloğu

Haritada gerçek canlılar **gösterilen derinliğe göre** belirir: her türün `bilim.json`'daki gerçek
derinlik aralığı, sunucunun ölçeklenmiş derinliğiyle karşılaştırılır (§2.5). Türün **yankısı** bir ping alanına
düşünce kendiliğinden Kalıntı Arşivi'nin Tür Kataloğu'na yazılır ([Kalıntı Arşivi](binalar/relic_archive.md),
[08](08_denizaltilar_basinc_seferler.md) §6); Fenerbalığı gövdesi taramaya iki kat puan verir.
Canlılar asla hedef değildir (K-25).

| Tür | Gerçek aralık (m) | Mariana'da görüldüğü kuşak | Not |
|---|---|---|---|
| <!--v:veri.bilim.turler.supergiant_amphipod.ad-->Süperdev Amfipod<!--/v--> | <!--v:veri.bilim.turler.supergiant_amphipod.min_m-->3.890<!--/v-->–<!--v:veri.bilim.turler.supergiant_amphipod.max_m-->8.931<!--/v--> | Abisal → Derin Yamaç | Birçok sunucuda nadir, zararsız çöpçü |
| <!--v:veri.bilim.turler.hadal_sea_cucumbers.ad-->Hadal Deniz Hıyarları<!--/v--> | <!--v:veri.bilim.turler.hadal_sea_cucumbers.min_m-->6.000<!--/v-->–<!--v:veri.bilim.turler.hadal_sea_cucumbers.max_m-->10.730<!--/v--> | Eşik → Dip | Tabanda iz bırakan yavaş topluluklar |
| <!--v:veri.bilim.turler.mariana_snailfish.ad-->Mariana Salyangoz Balığı<!--/v--> | <!--v:veri.bilim.turler.mariana_snailfish.min_m-->6.198<!--/v-->–<!--v:veri.bilim.turler.mariana_snailfish.max_m-->8.178<!--/v--> | Eşik → Çukur Yamacı | Mariana'ya özgü kayıt |
| <!--v:veri.bilim.turler.deep_prawn.ad-->Derin Deniz Karidesi<!--/v--> | —–<!--v:veri.bilim.turler.deep_prawn.max_m-->7.703<!--/v--> | Abisal → Çukur Yamacı | Dekapod sınırını gösterir |
| <!--v:veri.bilim.turler.hadal_amphipod.ad-->Dev Hadal Amfipodu<!--/v--> | <!--v:veri.bilim.turler.hadal_amphipod.min_m-->8.000<!--/v-->–<!--v:veri.bilim.turler.hadal_amphipod.max_m-->10.897<!--/v--> | Derin Yamaç → Çekirdek | Yem düşünce beliren amfipod bulutu |
| <!--v:veri.bilim.turler.xenophyophore.ad-->Ksenofiyofor (dev tek hücreli)<!--/v--> | —–<!--v:veri.bilim.turler.xenophyophore.max_m-->10.641<!--/v--> | Her kuşak | Tabanın "canlı taşları" |

- **Balık sınırı.** Balıklar
  <!--v:veri.bilim.kurallar.balik_derinlik_siniri.deger_m.0-->8.200<!--/v-->–<!--v:veri.bilim.kurallar.balik_derinlik_siniri.deger_m.1-->8.400<!--/v-->
  m bandının altında görünmez; bu bandın altında yaşam yalnızca omurgasızlardır. Mariana'da bu Derin
  Yamaç'ın başıdır; Java'da çukurun tamamı sınırın üstündedir.
- **Sunucuya özgü kayıtlar** `bilim.json`'daki oyun önerilerinden gelir: Mariana'da Mariana salyangoz
  balığı, Izu-Ogasawara'da Japon Çukuru salyangoz balığı, Kermadec'te Kermadec salyangoz balığı,
  Java'da Dumbo ahtapot, Porto Riko'da Galathea kusk yılanbalığı, Filipin'de büyük yüzgeçli kalamar.
- 4.000 m'den sığ yaşayan türler (dev kalamar, fıçı göz, vampir kalamar) haritada değil, inişin
  hikâyesinde ve Derinlik Haritası seferlerinde yer alır ([08](08_denizaltilar_basinc_seferler.md)).

---

## 10. PvP kuralları

### 10.1 Kalkanlar

<!-- URET:tablo:harita.kalkanlar -->
| Koruma | Süre | Bedel (öneri) | Kural / kaynak |
| --- | --- | --- | --- |
| Yeni Kaptan Koruması | 72 sa; Reaktör 12'ye kadar uzar (en çok 7 g) | Ücretsiz | Oyuncu üssüne ya da yürüyüşüne saldırınca biter |
| Sessizlik Kalkanı · 8 sa | 8 sa | 400 Hadalit | Görev, etkinlik, İttifak Dükkânı |
| Sessizlik Kalkanı · 24 sa | 24 sa | 1.000 Hadalit | Etkinlik ödülü, The Syndicate |
| Sessizlik Kalkanı · 3 g | 3 g | 2.500 Hadalit | The Syndicate, Akıntı Savaşı dükkânı |
| Toparlanma Kalkanı | 2 sa | Otomatik | 6 saatte 3 yenilgide; 24 saatte 1 kez |
| Sürüklenme sonrası | 4 sa | Otomatik | Üs koptuğunda (dayanıklılık 0) |
<!-- /URET:tablo:harita.kalkanlar -->

- **Sessizlik Kalkanı** üssü sonardan siler: kalkanlı üse saldırılamaz, keşif yapılamaz. Kalkan
  açıkken toplama, PvE, PvE rallisi, takviye ve yardım serbesttir. Başka bir kaptanın üssüne ya da
  yürüyüşüne saldırmak, PvP rallisine katılmak ve bir geçide ya da alana saldırmak kalkanı **kırar**.
- **Yeni Kaptan Koruması** 72 saat sürer; oyuncu 72 saatte Reaktör 12'ye ulaşmamışsa Reaktör 12'ye
  kadar uzar (en çok 7 gün). Simülasyondaki ücretsiz oyuncu Reaktör 12'ye
  <!--v:hesap.sim.f2p.seviye.12|gun-->13 sa 25 dk<!--/v-->'da ulaşır; yani pratikte koruma 72 saattir,
  yavaş başlayan oyuncu için uzar. Koruma altındaki kaptanın üssü taşınabilir, keşfedilemez, kötü
  unvan alamaz.
- **Toparlanma Kalkanı:** 6 saat içinde üç kez yenilen üs 2 saatlik otomatik kalkan alır (24 saatte bir).
- Kalkan fiyatları öneridir; kesin fiyat ve paketler: [12 · The Syndicate](12_syndicate.md).

### 10.2 Yağma

Kazanılan bir üs saldırısında saldıran, savunanın **korumasız** stoğunun her kaynaktan
%<!--v:veri.harita.yagma.oran-->20<!--/v-->'sine kadar alır.

```
korumasız = stok − Basınç Ambarı koruması (her örneğin "Yağmadan korunan" değeri toplamı)
yağma     = min(korumasız × %20 × güç farkı çarpanı, saldıranın kalan taşıma kapasitesi)
```

- **Yağmalanamaz:** <!--v:veri.harita.yagma.yagmalanamaz-->Süperalaşım, Hadalit, çantadaki Kaynak Kapsülleri, İttifak Ambarı'ndaki birikim<!--/v-->.
  Süperalaşım kapı kaynağıdır; ücretsiz oyuncunun haftalarca biriktirdiği alaşım tek gecede gitmez.
- Aynı saldıran aynı üssü 24 saatte en çok <!--v:veri.harita.yagma.gunluk_tekrar-->2<!--/v--> kez
  yağmalayabilir; sonraki zaferler savaş puanı verir, yağma vermez.
- Basınç Ambarı koruma değerleri: [Basınç Ambarı](binalar/pressure_vault.md), tavan ve koruma
  mantığı: [02](02_tempo_ve_denge.md) §7.
- Yağma eve dönen yürüyüşle taşınır ve yolda durdurulabilir (§8.1).

### 10.3 Su baskını ve sürüklenme

Yenilen üs yanmaz, **su alır**. Üs dayanıklılığının üst sınırı Kalkan Jeneratörü'nün kalkan
dayanıklılığıdır ([Kalkan Jeneratörü](binalar/shield_generator.md), değerler
[07](07_birlikler_ve_savas.md) §4.7); kayıplar bu sınırın yüzdesi olarak işler, böylece kural her
seviyede aynı hissettirir.

1. Her yenilgide dayanıklılık anında %<!--v:veri.harita.yagma.su_aninda-->8<!--/v--> × (kuşatma gücü /
   kalan asker gücü) düşer (Delici'siz ordu %8, yalnız Delici %24; [07](07_birlikler_ve_savas.md) §4.5) ve üs
   <!--v:veri.harita.yagma.su_sure_dk-->30<!--/v--> dakika **su alır**: dakikada
   %<!--v:veri.harita.yagma.su_dakika-->0,5<!--/v--> daha.
2. **Pompa** düğmesi su almayı hemen durdurur (Titanyum; bedel reaktör seviyesiyle ölçeklenir).
3. Su almayan üs saatte %<!--v:veri.harita.yagma.onarim_saat-->5<!--/v--> onarılır.
4. Dayanıklılık 0'a inerse üs **demirinden kopar**: aynı kuşakta rastgele bir boş kareye sürüklenir,
   %<!--v:veri.harita.yagma.kopma_sonrasi-->50<!--/v--> dayanıklılıkla ve 4 saatlik kalkanla yeniden
   demir atar. Binalar ve askerler zarar görmez; yalnızca konum değişir.

### 10.4 Konuşlanma (taşınma)

<!-- URET:tablo:harita.konuslanma -->
| Konuşlanma | Nereye | Koşul | Bedel (öneri) |
| --- | --- | --- | --- |
| Yeni Başlangıç | Hadal Eşiği'nde seçilen boş kare | Reaktör ≤ 10, ilk 7 gün, 1 kez | Ücretsiz |
| Akıntı Şamandırası | Aynı kuşakta rastgele boş kare | — | Eşya; 200 Hadalit |
| İttifak Rotası | Kendi ittifak bölgende seçilen kare | Bölge bağlı olmalı | İttifak Dükkânı; 600 Hadalit |
| Hassas Konuşlanma | Açık kuşaklarda seçilen boş kare | Kuşağa giden bir kilit ittifakında ya da kilitler aşınmış | 1.500 Hadalit |
| Çukur Pasaportu | Başka bir çukur sunucusu | Göç kuralları (16) | Pasaport |
<!-- /URET:tablo:harita.konuslanma -->

Ortak koşullar: bütün yürüyüşler evde; üs su almıyor; üsse gelen bir saldırı yok; hedef kare boş, başka
ittifakın bölgesinde değil; Abisal Düzlük'e ve Dip/Çekirdek'e üs kurulmaz; Çukur Ekseni'ne yalnızca
Hassas Konuşlanma ile inilir; gövde sınıfı kuralı (§2.3) geçerlidir.

### 10.5 Güç farkı kuralı

Güç, toplam güçtür (K-14). Oran = saldıranın gücü / savunanın gücü.

<!-- URET:tablo:harita.guc_farki -->
| Güç oranı (saldıran / savunan) | Yağma | Savaş Puanı | Ek |
| --- | --- | --- | --- |
| ≤ 1,5 | %100 | %100 | — |
| 1,5–2,5 | %50 | %50 | — |
| 2,5–4 | %10 | %0 | Zorbalık İzi (24 sa, profilde görünür) |
| > 4 | — | — | Üsse saldırı ve yürüyüş durdurma yapılamaz (Eşik ve Yamaç'taki üsler) |
<!-- /URET:tablo:harita.guc_farki -->

- **Kapsam.** Kural Hadal Eşiği ve Çukur Yamacı'ndaki üslere ve bu kuşaklardaki yürüyüşlere tam
  uygulanır. Derin Yamaç ve daha derinde savaş bölgesidir: çarpanlar uygulanır ama ">4 engeli"
  kalkar.
- **İstisnalar.** Savunan son 24 saatte saldırana saldırmışsa (intikam) kural uygulanmaz. İlan
  edilmiş ittifak savaşında eşikler 1,5 katına çıkar ([10](10_ittifak.md)). Bir geçit, alan ya da
  Kalp garnizonundaki askerler ve bir ralliye katılan askerler kuralın dışındadır: hedefe giren
  savaşı kabul etmiştir.
- **Zorbalık İzi** profilde 24 saat görünür; ittifak listesinde de küçük bir işaretle durur. Bir ceza
  değil, bir aynadır.

### 10.6 Gelgit Penceresi

Her sunucunun günde bir **Gelgit Penceresi** vardır: sunucu saatiyle
<!--v:veri.harita.yuruyus.gelgit_baslangic-->20:00<!--/v-->'den itibaren
<!--v:veri.harita.yuruyus.gelgit_saat-->3<!--/v--> saat (Overseer ayarlar). Geçitlere, Rezonans
Alanları'na ve ittifak yapılarına saldırılar bu pencerede yapılır; Kadim Bekçiler pencere başında doğar.
Oyuncu üslerine saldırı ve PvE her saattedir. Pencere, büyük çekişmeleri herkesin bildiği ve
katılabildiği bir saate toplar; gece 4'te kaybedilen kilit olmaz.

---

## 11. Ücretsiz oyuncu ve ödeyen oyuncu

| Konu | Ücretsiz oyuncu | Ödeyen oyuncu |
|---|---|---|
| Kuşaklara iniş | Gövde sınıfı Kronik kapılarından önce yetişir (§2.3) | Daha erken yetişir ama Kronik beklemesi aynıdır |
| Toplama | 4 hangar, %35'e varan gelir; güvenli Abisal'de Süperalaşım | Hızlandırıcı eşyalar (Bereket Akışı) ve VIP bonusu; aynı yataklar |
| PvE | Günde ~720 Sonar Enerjisi; askerler yalnızca yaralanır | Sonar Pili alabilir; Kronik düşman tavanı aynıdır |
| Leviathan | Ralliye katılım lider gövdesiyle; günde 3 ödül | Aynı günlük ödül sınırı |
| Kadim Şema | Kronik aşama 10 kişisel hedefi + yuvalar | Hadalit ile de alabilir ([12](12_syndicate.md)) |
| Koruma | 72 saat yeni oyuncu koruması, Toparlanma, güç farkı kuralı, Sükûnet Fermanı | Sessizlik Kalkanı'nı daha sık kullanır |
| Yağma | Süperalaşım ve Hadalit asla yağmalanmaz; Ambar korur | Aynı kurallar |
| Konuşlanma | Yeni Başlangıç ücretsiz; İttifak Rotası İttifak Dükkânı'ndan | Hassas Konuşlanma'yı Hadalit'le alır |
| Egemenlik | Fermanlar tüm sunucuya işler | Egemen olmak harcamayla değil Kalp'i tutmakla olur |

İlke: ödeme **zaman ve kolaylık** satın alır, **erişim** satın almaz. Hiçbir kuşak, geçit, yatak ya
da alan yalnızca ödeyen oyuncuya açık değildir.

---

## 12. Arayüz notları (sakin lüks)

- **Harita bir sonar ekranıdır.** Koyu lacivert zemin, ince camgöbeği hatlar; kuşak sınırları ince eş
  derinlik çizgileridir. Yakınlaştıkça taban dokusu, uzaklaştıkça yalnızca kuşaklar ve ittifak
  bölgeleri kalır (ittifak rengiyle %15 saydam dolgu).
- **Derinlik göstergesi** sol altta: bakılan karenin gösterilen derinliği ve basıncı
  (ör. "7.640 m · 784 bar"). Kuşak adı küçük harflerle, Kronik'te kapalıysa kilit simgesiyle.
- **Yürüyüş çizgileri** ince ve sessizdir: kendi yürüyüşün beyaz, ittifakınki mavi, diğerleri gri;
  düşman ittifakınki ince kırmızı kenarla. Sayılar ve kahraman portresi yalnızca dokununca.
- **Bildirimler azdır.** Saldırı uyarısı, ralli çağrısı ve Taht Savaşı dışında kırmızı yoktur. Leş
  düşüşü, yatak doğuşu ve kristal saçılması yalnızca sonar menzilindekilere, küçük bir işaretle.
- **Fiyat haritada görünmez.** Kalkan, konuşlanma ve Sonar Pili için harita yalnızca "The Syndicate'te"
  yazan sakin bir bağlantı gösterir; pop-up teklif, geri sayım, "İNDİRİM" yoktur.
- **Tür Kataloğu taraması** küçük bir ritüeldir: dron hedefe yaklaşır, sonar halkaları genişler, türün
  silueti çizilir, adı ve gerçek derinliği görünür. Atlanabilir, ama atlanması istenmez.
- Egemen fermanı ekranın üstünde tek satırlık sakin bir şerittir; unvanlar profilde küçük bir mühür.

---

## 13. Uç durumlar

- **Sunucu kapanışı.** Hadal Eşiği'nin üs yuvalarının %80'i dolunca ya da 30. günde sunucu yeni
  kayıtlara kapanır, yeni bir çukur sunucusu açılır. Aynı çukura ikinci sunucu açılırsa o çukurun
  bilinen derinliklerinin adı eklenir (açık soru).
- **Uykudaki üsler.** 14 gün girmeyen ve Reaktör < 10 olan üs haritadan kalkar (oyuncu dönünce
  Eşik'te boş bir kareye konur); 30 gün ve Reaktör < 20 için aynı. Reaktör 20+ üsler kalkmaz.
- **Kuşak dolarsa.** Taşınma için boş kare yoksa konuşlanma reddedilir, eşya geri verilir.
- **Kronik geride kalan oyuncu.** Gövde sınıfı yetmeyen oyuncu yeni kuşağa yürüyemez ama ralliye
  katılabilir ve İttifak Rotası'yla bölgesine taşınabilir (sınıf yetmiyorsa yalnızca sınıfının
  izin verdiği basamaktaki bölge karelerine).
- **Geçit el değiştirirken içeride kalan yürüyüş.** Yürüyüş yolunun yarısındaysa geçide sahip olmayan
  ittifak üyesinin yürüyüşü geçitte durur ve geri döner; yük kaybı yoktur.
- **Kalkan açarken gelen saldırı.** Saldırı yoldaysa kalkan açılabilir; varan saldırı boşa döner.
  Kalkan bitimine 5 dakika kala sessiz bir hatırlatma gelir (ayar).
- **Leş yarıda sönerse.** 72. saatte toplamakta olan sefer o ana kadarki yükle eve döner.
- **Egemenin ittifakı dağılırsa.** Kalp sahipsiz kalır ve hemen Taht Savaşı açılır; unvanlar düşer.
- **Göç eden kaptanın geçitteki askerleri** göçten önce geri çağrılmak zorundadır ([16](16_kronik_kokenler_profil.md)).
- **Saat dilimi.** Gelgit Penceresi sunucu saatidir; arayüz oyuncunun yerel saatini de gösterir.

---

## 14. Bugün kodda

| Özellik | Kodda | Hedef | Durum |
|---|---|---|---|
| Izgara | 200 × 200, aralık 3, görüş 14 | Aynı | ✅ |
| Yerleşim | Merkezden dışa kare spiral | Hadal Eşiği'nde kanat dengeli dağıtım (K-08) | 🟡 |
| Görünüm | Salt görüntü; üs levhası, kart, kamera | + kuşaklar, sis, yataklar, yürüyüşler | 🟡 |
| Kuşaklar, basamaklar, kanatlar | Yok | §2 | ⬜ |
| Geçitler, Kronik bağı | Yok | §3, [16](16_kronik_kokenler_profil.md) | ⬜ |
| Yataklar ve toplama | Yok | §4 | ⬜ |
| Sonar Enerjisi, PvE, Leviathan | Yok | §5 | ⬜ |
| Rezonans Alanları, Çukur Kalbi, Egemen | Yok | §6–7 | ⬜ |
| Yürüyüş, ralli, takviye, keşif | Yok | §8 | ⬜ |
| Kalkan, yağma, su baskını, konuşlanma, güç farkı | Yok | §10 | ⬜ |

Bugün harita yalnızca üsleri gösterir: `ViewWorld` komutu bir merkez çevresindeki üsleri ad, ittifak
etiketi, seviye, güç ve çevrimiçi durumla döndürür ([28_World_Map.md](../28_World_Map.md)).

---

## 15. Uygulama notları

| Değişiklik | Kod |
|---|---|
| Kuşak ve basamak hesabı | `shared/HADAL.Shared/Simulation/WorldRules.cs`: `Distance(x, y) = max(|x−c|, |y−c|)`, `BeltOf(d)`, `StepOf(d)`, `RequiredHullClass(d)`; `InView` aynı kalır |
| Yerleşim (K-08) | `WorldRules.Place` → merkezden spiral yerine Hadal Eşiği'nde (uzaklık 65–84) kanat başına sayaçla, en boş kanatta rastgele boş kare; `spacing` korunur. Mevcut üsler yerinde kalır, ilk girişte bir kez ücretsiz Yeni Başlangıç hakkı alır |
| Katalog | `shared/catalog/catalog.json` → `world`: `belts[]` (d aralığı, nominal derinlik, sınıflar), `gates[]`, `nodeLevels[]`, `nodeTypes[]`, `sonar` (kapasite, yenilenme, bedeller), `shields[]`, `pvp` (yağma, güç farkı, gelgit); `GameCatalog.cs` doğrulaması |
| Sunucu profili | Sunucu yapılandırmasına `trenchId`; istemci `bilim` tablosundan D'yi okuyup gösterilen derinliği hesaplar. Mekanik hiçbir yerde metre kullanmaz |
| Dünya durumu | `server/Hadal.Game/World/WorldService.cs`: yataklar, düşmanlar, yuvalar, geçitler, alanlar, yürüyüşler için `WorldEntity` kayıtları ve tik döngüsü (yürüyüş ilerlemesi, toplama, doğuş, leş erimesi). Tablo `hadal.world_entities`; üsler `hadal.world_bases`'te kalır |
| Komutlar | `ViewWorld` (29) genişler (sis, varlıklar); yeni: `MarchGather`, `MarchAttack`, `MarchReinforce`, `StartRally`, `JoinRally`, `RecallMarch`, `Scout`, `Relocate`, `ActivateShield` |
| Savaş çözümü | Sunucu hakemli; [07](07_birlikler_ve_savas.md)'deki `BattleResolver` paylaşılan koddan |
| Sonar Enerjisi | `PlayerSession` yeni sayaç: son güncelleme + kalan; okumada tembel yenilenme (üretimdeki gibi) |
| Kalkan, koruma | `PlayerSession` → `ShieldUntil`, `NewcomerUntil`; `WorldService` saldırı doğrulaması |
| İttifak bağları | Geçit ve alan sahipliği, bölge, garnizon: `server/Hadal.Game/Social/SocialHub.cs` ittifak kimliği; kilit sırası dünya → ittifak merkezi → oturum (bugünkü gibi) |
| Gelgit Penceresi, sunucu kapanışı | Overseer ayarı (`Hadal.Gateway/Admin`), `LiveCatalog` |

---

## 16. Esin kaynakları

Türün krallık haritası oyunlarından alınan **yapılar**, Hadal'ın kendi isim ve sayılarıyla:

- Merkezde zorlaşan bölgeler ve bölgeler arası, ittifakların ele geçirdiği geçitler → derinlik
  kuşakları, fay geçitleri ve Basınç Kilitleri.
- Sunucunun ortak zaman çizelgesiyle açılan bölgeler → Derinlik Kroniği.
- Tutan ittifaka etki veren kutsal yerler, aynı tür etkinin birikmemesi, bekçilerin bıraktığı geçici
  güçlendirmeler → Rezonans Alanları ve kristaller.
- Merkezdeki yapıyı tutan ittifakın liderinin hükümdar olması, iyi ve kötü unvanlar → Çukur Kalbi,
  Egemen, fermanlar.
- Hareket puanı harcayan haritadaki canavarlar ve yalnızca ralliyle düşen kaleler → Sonar Enerjisi,
  Leviathan yuvaları.
- Görünür ve durdurulabilen yürüyüşler, ralli, takviye, keşif, kalkanlar, yanan ve taşınan şehirler →
  yürüyüş, Savaş Kubbesi rallisi, su baskını ve sürüklenme.

Hadal'ın farkı: harita gerçek bir çukurun kesitidir; derinlik, basınç, yataklar ve canlılar gerçek
veriden gelir; sunucular gerçek çukurlardır; gerçek canlılar avlanmaz.

---

## 17. Açık sorular

- **Yeni kaptan koruması:** "72 saat ya da Reaktör 12" ifadesini "hangisi *sonra* gelirse (en çok 7
  gün)" diye yorumladık; simülasyonda F2P Reaktör 12'ye 13 saatte ulaştığından "hangisi önce" yorumu
  korumayı yarım güne indirirdi. Onay gerekiyor.
- **Taşıma kapasitesi:** 07'deki asker yükü × 08'deki hangar kapasitesi R20'de ~76.000 (Zırhlı T5),
  R30'da ~240.000, R50'de ~1,1 milyon yük eder. Erken oyunda (R10, ~12.600 yük) sefer 2–3 saatte dolar;
  R20'den sonra seferi yük değil süre bitirir (oyuncu 4–12 saatte geri çağırır). Gelir modeli
  ([15](15_ekonomi_akisi.md)) günde 12 toplama saati ve "yük ≥ hız × 8 sa" varsayıyor: Zırhlı askerle
  bu ancak ~R25'te sağlanır (R20'de ~5,4 sa); Delici (yük ×2) ve Mors gövdesi (yük ×1,6) ile R20'de
  sağlanır. 07/08 erken yükü artırmalı ya da 15 erken bantlarda gece seferini kısa saymalı.
- **Basınç sınıfı ↔ İskele:** Basamak tablosu `bilim.json`'daki sınıf derinlikleriyle eşleşiyor; 08'in
  İskele seviyesi → sınıf eşlemesi (1 + ⌊(Sv − 1) / 5⌋) değişirse §2.3'teki F2P günleri değişir.
- **Sunucu kapasitesi:** ~1.100 kurucu kaptan bir çukur sunucusu için yeterli mi? Aralık 3 → 2 yapılırsa
  Eşik ~2.900 üs alır ama harita kalabalıklaşır.
- **Aynı çukura ikinci sunucu:** "Mariana · Sirena" gibi o çukurun bilinen derinliklerinin adı mı,
  yoksa "Mariana II" mi? Bilinen derinlik adları `bilim.json`'a eklenmeli.
- **Hadalit Damarı:** Günlük 60 Hadalit sınırı ve sunucuda 12 damar, `oduller.json`'daki Hadalit
  bütçesine henüz eklenmedi ([15](15_ekonomi_akisi.md)).
- **Kalkan fiyatları** (400 / 1.000 / 2.500 Hadalit) öneridir; [12](12_syndicate.md) ile eşitlenmeli.
- **Leviathan yuva ödülleri** (Kodeks, Süperalaşım, Hadalit) `oduller.json`'daki "Leviathan Avı" ve
  Süperalaşım dökümüne eklenmeli; Kodeks bütçesi (Savaş Kubbesi 4 için 128. güne ~300, 5 için 225.
  güne ~800 daha) harita + Kronik + dükkân toplamıyla hesaplandı.
- **Gelgit Penceresi** tek mi olmalı, yoksa sunucunun oyuncu dağılımına göre iki pencere mi (ör. 13:00
  ve 20:00)?
- **Doğa olayları** (bulanıklık akıntısı, tabanın kayması, deniz karı) haritada yatak doğuşunu ve
  görüşü etkileyecek; ayrıntı [13](13_etkinlikler_ve_sezonlar.md)'te.
