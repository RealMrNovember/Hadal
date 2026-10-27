# 18 · Bilim Temeli — Hadal'ın Gerçek Okyanusu

Bu bölüm, oyunun üzerine kurulduğu gerçek okyanusun tasarımcılar için başvuru kaynağıdır: derinlik
bölgeleri, basınç, on hadal çukuru, canlılar, mineraller, enerji kaynakları, doğa olayları ve derin
deniz araçları. Buradaki her sayının tek kaynağı [veri/bilim.json](veri/bilim.json)'dır; her kayıt
oradaki `kaynak` alanıyla aşağıdaki kaynakçaya bağlanır. Kaynaklar çeliştiğinde ana değerin yanında
aralık verilir. Oyunun kendi kristal malzemesi bu bölümün konusu değildir. Canlıların oyundaki yeri
için Kanon K-25, hassas konular için K-26 geçerlidir ([00 · Kanon](00_kanon.md)).

---

## 1. Derinlik Bölgeleri

| Bölge | Derinlik | Işık | Sıcaklık | Tipik yaşam |
|---|---|---|---|---|
| Epipelajik (güneşli) | 0–200 m | Görünür ışığın çoğu; fotosentez var | −2 °C (kutup) … 36 °C (Basra Körfezi) | Fitoplankton, algler, balık sürüleri |
| Mezopelajik (alacakaranlık) | 200–1.000 m | Yüzey ışığının ~%1'inden karanlığa | 20 °C üstünden ~4 °C'ye; termoklin burada | Biyoışıldayan balıklar, fıçı göz, vampir kalamar |
| Batipelajik (gece yarısı) | 1.000–4.000 m | Kalıcı karanlık; yalnız biyoışıldama | ~4 °C, derinde 2 °C'ye iner | Fener balıkları, dev kalamar; kaşalot dalışları |
| Abisopelajik (abisal) | 4.000–6.000 m | Zifiri karanlık | 0–4 °C, donma noktasına yakın | Amfipodlar, deniz hıyarları, nodül tarlaları |
| Hadopelajik (hadal) | 6.000–10.935 m | Güneş ışığı yok | 0–4 °C (tipik 1–4 °C) | Salyangoz balıkları (~8.300 m'ye kadar), amfipodlar, deniz hıyarları, ksenofiyoforlar |

Hadal bölge çoğunlukla dalma-batma çukurlarında, kırık zonlarında ve birkaç derin havzada bulunur.

## 2. Basınç ve Sıcaklık

- **Kural:** Deniz suyunda her ~10 m inişte basınç ~1 atmosfer (≈1 bar) artar (NOAA: 10,06 m).
- **Su az da olsa sıkışır.** Derinde yoğunluk yüzeydekinden yüksektir; bu yüzden basınç derinlikle
  doğrusaldan biraz hızlı artar.
- **Challenger Deep:** Sık alıntılanan değer 1.086 bar / 1.071 atm / 15.750 psi'dır; bu, sabit
  yüzey yoğunluğuyla yapılan basit hesaba yakındır. Ölçüm temelli değer daha yüksektir: 1992 CTD
  ölçümü 10.877 m'de 11.197 dbar buldu; 10.935 m'ye uzatıldığında deniz basıncı **≈1.126 bar
  (≈1.111 atm)**, hava basıncıyla ≈1.127 bar eder. Bu, Limiting Factor'ın tam okyanus derinliği
  tasarım değeri olan 1.127 bar ile örtüşür. **Kılavuzda kullanılacak değer: ≈1.126 bar.**
- **Sıcaklık:** Hadal dip suyu tipik olarak 1–4 °C; Güney Sandwich'te 7.000 m'de yerinde sıcaklık
  ~0,03 °C. Sıkışma her 1.000 m'de suyu ~0,16 °C ısıtır, bu yüzden ~4.500 m'den sonra su yeniden
  hafifçe ısınır.
- **Birim tuzağı:** Bazı kaynaklar "atmosfer" yerine teknik atmosfer (kgf/cm²) kullanır; değerler
  %2–4 farklı görünebilir (ör. JAMSTEC 6.500 m için ~681 atmosfer verir).

| Derinlik | Deniz basıncı (≈bar) |
|---|---|
| 1.000 m | 101 |
| 4.000 m | 406 |
| 6.000 m (hadal sınırı) | 612 |
| 8.000 m | 820 |
| 10.000 m | 1.030 |
| 10.935 m (Challenger Deep) | 1.126 |

Değerler UNESCO/Saunders (1981) derinlik–basınç bağıntısıyla hesaplanmıştır (hava basıncı hariç);
bağıntı Challenger Deep CTD ölçümüyle %0,3 içinde uyumludur.

## 3. Hadal Çukurları (sunucu adları)

Her sunucu bir hadal çukurunun adını taşır (K-24). Basınç, en derin noktadaki yaklaşık deniz basıncıdır.

| Sunucu | En derin nokta | Derinlik | ≈Basınç | Okyanus | Öne çıkan |
|---|---|---|---|---|---|
| Mariana Çukuru | Challenger Deep | 10.935 ± 6 m | 1.126 bar | Batı Pasifik | Dünyanın en derin noktası; Trieste 1960, tek kişilik iniş 2012, tekrarlı inişler 2019 sonrası |
| Tonga Çukuru | Horizon Deep | 10.816 ± 16 m | 1.115 bar | Güneybatı Pasifik | İkinci en derin nokta; en derin deniz hıyarı (10.730 m) |
| Filipin Çukuru | Emden (Galathea) Derinliği | 10.540 m | 1.085 bar | Batı Pasifik | Galathea 1951 seferi en derinde yaşamı kanıtladı; en derin kalamar (6.212 m) |
| Kermadec Çukuru | Scholl Deep | 10.047 m | 1.035 bar | Güneybatı Pasifik | Nereus 2014'te 9.990 m'de kayboldu; süperdev amfipod |
| Izu-Ogasawara Çukuru | Adsız derinlik | 9.826 ± 11 m | 1.010 bar | Kuzeybatı Pasifik | En derin balık gözlemi (8.336 m); ilk insanlı iniş 2022 |
| Kuril-Kamçatka Çukuru | Orta kesim | 9.604 m | 990 bar | Kuzeybatı Pasifik | 2025'te Mw 8,8 Kamçatka depremi bu zonda |
| Porto Riko Çukuru | Brownson Deep | 8.378 ± 5 m | 860 bar | Atlantik | Atlantik'in en derin noktası; Alvin 2022'de 6.453 m |
| Güney Sandwich Çukuru | Meteor Deep | 8.265 ± 13 m | 850 bar | Güney Atlantik / Güney Okyanusu | Güney Okyanusu'nun en derini Factorian Deep (7.432 m); koloss kalamar 2025 |
| Atacama Çukuru | Adsız derinlik (Richards Deep'in kuzeyi) | 8.069 ± 8 m | 825 bar | Güneydoğu Pasifik | İlk insanlı iniş 2022; kemosentetik bakteri iplikleri |
| Java Çukuru | Adsız derinlik | 7.187 ± 13 m | 735 bar | Hint Okyanusu | Hint Okyanusu'nun en derini; en derin ahtapot (6.957 m) |

**Belirsiz ya da tartışmalı derinlikler**

- **Mariana:** 10.935 ± 6 m (2020, basınç tabanlı); Beş Derinlik Seferi'nin sonar değeri 10.924 ± 15 m.
- **Tonga:** 10.816 ± 16 m; sefer duyurusu 10.823 ± 10 m, bazı kaynaklar 10.800 m.
- **Filipin:** 10.497 m (1945) ile 10.540 m (1951) eski iskandil ölçümleridir; modern bir belirsizlik değeri yayımlanmamış.
  2021'deki ilk insanlı dalış ~10.045 m'ye indi.
- **Kuril-Kamçatka:** Britannica'nın 10.542 m'si eski bir Vityaz değeridir; 2016 taraması 10.000 m'den
  derin alan bulamadı, modern değer ~9.604 m.
- **Izu-Ogasawara:** 9.826 ± 11 m (2022); bazı listeler 9.780 m.
- **Atacama:** 2022'de en derin nokta Richards Deep'in 77 deniz mili kuzeyinde bulundu; Richards
  Deep'in kendisi 7.727 m ölçüldü (eski kaynaklarda 8.065 m).
- **Java:** 7.187 ± 13 m (2019); Britannica eski değer olarak 7.450 m verir.
- **Güney Sandwich:** Meteor Deep Atlantik sularındadır; Güney Okyanusu sayımında Factorian Deep esas alınır.

## 4. Canlılar

Türler avlanmaz; ortam canlısı ve **Tür Kataloğu** kaydı olarak yer alır (K-25). "Derinlik"
belgelenmiş aralıktır; "—" kaynakta değer olmadığını gösterir.

| Tür | Bilimsel ad | Derinlik | Boy | Özellik | Oyunda |
|---|---|---|---|---|---|
| Mariana Salyangoz Balığı | *Pseudoliparis swirei* | 6.198–8.178 m | 22,6 cm | En bol 7.000–8.000 m; çok iri yumurta (9,4 mm) | Mariana'da yem istasyonu sürüsü; "Hadal Balıkları" sayfası |
| Japon Çukuru Salyangoz Balığı | *Pseudoliparis belyaevi* | 6.579–8.336 m | — | Yakalanan en derin balık (8.022 m); 8.336 m'deki yavru gözlem rekoru | Izu-Ogasawara'da nadir "sınır gözlemi" |
| Kermadec Salyangoz Balığı | *Notoliparis kermadecensis* | 6.472–7.561 m | — | Kas TMAO'su derinlik sınırı tahmininin verisi | "Balık Derinlik Sınırı" bilgi kartı |
| Dev Hadal Amfipodu | *Hirondellea gigas* | 8.000–10.897 m | >3 cm | Odun artığı sindirir; alüminyum kalkan iddiası tartışmalı | Yem düşünce beliren amfipod bulutu |
| Süperdev Amfipod | *Alicella gigantea* | 3.890–8.931 m | 34 cm | En büyük amfipod; okyanusların ~%59'unda olabilir | İri, yavaş, zararsız çöpçü |
| Dev İzopod | *Bathynomus giganteus* | 310–2.300 m | 50 cm | Dipte yaşayan dev eşbacaklı | Sığ başlangıç bölgesinin tanıtım canlısı |
| Kambur Fener Balığı | *Melanocetus johnsonii* | 100–4.500 m | 18 cm (♂ 2,9) | Işıyan yüzgeç ışını "oltası"; cüce erkekler | Yalnız ışığıyla fark edilir; ışık kısarak tarama |
| Vampir Kalamar | *Vampyroteuthis infernalis* | 600–3.300 m | 30 cm | Avcı değil, deniz karı toplar; oksijen minimum bölgesi | Deniz karı yoğunlaşınca görünür |
| Dev Kalamar | *Architeuthis dux* | ~300–1.000 m | ~13 m | ~27 cm göz; ilk görüntü 2004, ~900 m | Menzil dışında "efsane" gözlemi |
| Koloss Kalamar | *Mesonychoteuthis hamiltoni* | 600 m'den derin | ~7 m | En ağır omurgasız (~500 kg); ilk görüntü 2025 | Güney Sandwich'e özgü nadir kayıt |
| Dev Sifonofor | *Praya dubia* | 700–1.000 m | ~45,7 m | Mavi balinadan uzun koloni; 3 m dokunaç | Işıklı zincir gibi dev ortam canlısı |
| Pullu Ayaklı Salyangoz | *Chrysomallon squamiferum* | 2.424–2.785 m | 4,5 cm | Demir sülfür (pirit, greigit) zırh; IUCN "Tehlikede" | Korunan tür; bacaları kazı dışı |
| Yeti Yengeci | *Kiwa hirsuta* | ~2.200 m | 15 cm | Kıllarında bakteri "yetiştirir"; 2005 | "Kemosentez" bilgi kartı örneği |
| Dev Tüp Kurdu | *Riftia pachyptila* | ~2.400–2.500 m | 1,5 m | Ağzı ve bağırsağı yok; ortak bakterilerle beslenir | Baca tarlasının kırmızı "ormanı" |
| Pompeii Kurdu | *Alvinella pompejana* | ~2.500 m | 13 cm | Tüpünde 60–80 °C ölçülür; 50–55 °C'de 2 saat ölümcül | Baca duvarında, ısı tarayıcısıyla |
| Dumbo Ahtapot | *Grimpoteuthis* sp. | 6.957 m'ye kadar | ~20 cm | En derin ahtapot, ilk hadal kafadanbacaklı | Java'da süzülen ortam canlısı |
| Ksenofiyofor | Xenophyophorea | 10.641 m'ye kadar | >10 cm | En büyük tek hücreler; başka canlılara yuva | Her hadal sunucuda "canlı taşlar" |
| Hadal Deniz Hıyarları | Holothuroidea | 6.000–10.730 m | — | Hadal tabanın baskın büyük hayvanları | Tortuda iz bırakan taban "çobanları" |
| Fırfırlı Köpekbalığı | *Chlamydoselachus anguineus* | 120–1.500 m | 2 m | Üç uçlu iğne dişler; "yaşayan fosil" | Yamaçlarda nadir, sessiz ortam canlısı |
| Fıçı Göz | *Macropinna microstoma* | 600–800 m | 15 cm | Şeffaf kafa kalkanı içinde dönen tüp gözler | Taramada kafanın içi görünür |
| Galathea Kusk Yılanbalığı | *Abyssobrotula galatheae* | 3.110–7.965 m | 16,5 cm | Eski "en derin balık" (8.370 m) kaydı tartışmalı | "Tarihî Rekorlar" sayfası |
| Büyük Yüzgeçli Kalamar | *Magnapinna* sp. | 6.212 m'ye kadar | — | En derin kalamar, ilk hadal kalamar (2021) | Filipin'de tabanın üstünde siluet |
| Derin Deniz Karidesi | *Benthesicymus crenatus* | 7.703 m'ye kadar | — | En derin dekapod gözlemi (2008) | Dekapod sınırını gösteren kayıt |

## 5. Derin Yaşamın Kuralları

- **Balık sınırı 8.200–8.400 m.** Balıklar proteinlerini basınca karşı TMAO ile korur; TMAO derinlikle
  artar ve iç ozmotik derişimi yükseltir. Ekstrapolasyon, balığın iç derişiminin ~8.200 m'de deniz
  suyununkine (~1.100 mOsmol/kg) eşitleneceğini gösterir. Bu bir hipotezdir; gözlenen rekor bandın içindedir.
- **Rekorlar:** gözlem 8.336 m (Izu-Ogasawara, 2022), yakalama 8.022 m (Japon Çukuru, 2022), önceki
  gözlem rekoru 8.178 m (Mariana, 2017). Yeni rekorların ancak birkaç metre fark etmesi bekleniyor.
- **Sınırın altında yalnız omurgasızlar:** 8.400–11.000 m arasında balık yoktur; en derin kayıtlar
  amfipod 10.897 m, deniz hıyarı 10.730 m, ksenofiyofor 10.641 m.
- **Grup uçları:** dekapod 7.703 m, ahtapot 6.957 m, kalamar 6.212 m.
- **Işık sınırı:** 200 m altında fotosentez yetersiz, 1.000 m altında güneş ışığı yok.
- **Kemosentez:** Bakteriler hidrojen sülfür ya da metanı oksitleyerek organik karbon üretir. 1977'de
  Galápagos Yarığı'nda bulunan baca toplulukları, yer içinden gelen kimyasal enerjiye dayanan ilk büyük
  hayvan ekosistemiydi; soğuk sızıntılarda da aynı ilke işler.
- **Deniz karı:** Yüzeyden yağan organik madde derin denizin ana besinidir; taneler düşerken birkaç
  santimetreye büyür, bazıları haftalarca düşer. Kalanı tabanın ~3/4'ünü örten, milyon yılda 6 m'ye
  kadar biriken balçığa karışır.
- **Balina düşüşü:** Batan bir leş onlarca yıllık bir vaha kurar; 400'den fazla tür, en az 30'u yalnız burada.

| Balina düşüşü aşaması | Süre | Canlılar |
|---|---|---|
| Hareketli çöpçüler | ~2 yıla kadar | Miksinler (hagfish), uyuyan köpekbalıkları |
| Zenginleşme fırsatçıları | ~2 yıla kadar | Kıllı solucanlar, kabuklular, kemik yiyen Osedax |
| Kükürt sevenler | ~50 yıl, belki bir yüzyıl | Kemik yağını parçalayan bakteriler ve kemosentetik hayvanlar |
| Resif | Belirsiz, her zaman görülmez | İskelet kalıntısında süzerek beslenenler |

## 6. Mineraller ve Kaynaklar

| Kaynak | Nerede / derinlik | Bileşim ve ölçek | Oyundaki karşılığı |
|---|---|---|---|
| Polimetalik nodül | Abisal düzlükler, 4.000–6.000 m (kaynağa göre 3.500–6.500 m); Clarion-Clipperton Bölgesi ~4,5–6 milyon km² | Mn %28,4 · Fe %6,2 · Ni %1,3 · Cu %1,0 · Co %0,2; CCZ'de ≥21,1 milyar kuru ton; milyon yılda birkaç mm büyür | Süperalaşım |
| Kobalt zengini ferromangan kabuk | Deniz dağı yamaçları, 400–7.000 m; en zengini 800–2.500 m | Co çoğu kez >%0,5; Te, Pt, Y, nadir toprak; Orta Pasifik'te ~7,5 milyar ton (karadaki Co rezervinin ~4, Te'nin ~9 katı); Ti ~%1,09 | Süperalaşım, Titanyum yan ürünü |
| Deniz tabanı masif sülfürleri | Okyanus ortası sırtlar, yay ardı havzalar | Cu, Zn, Pb, Au, Ag; ~430 yatak (~300 aktif); toplam ~600 milyon ton | Masif sülfür (teçhizat) |
| Nadir toprak çamuru | Minamitorishima çevresi, ~5.600–6.000 m | >5.000 ppm REY; ~16 milyon ton (en iyi 105 km²'de ~1,2 milyon ton) oksit; 2026'da ilk çıkarım denemesi | Süperalaşım katkısı |
| Metan hidrat | Okyanusun çoğu 500 m'den derin kesimi; kutuplarda daha sığ | Basınç ve soğukta kararlı; ısınınca çözülür | Enerji |
| Titanyum kaynakları | Kıyı ve akarsu ağır mineral kumları | Dünya arzının çoğu ilmenit ve rutilden; derin denizde kabuklarda ~%1 Ti | Titanyum |
| Bazalt camı / yastık lav | Okyanus ortası sırtlar | Yeni kabuğun çoğu yastık bazalt; lav yüzeyi 1 saniyeden kısa sürede cama döner | Bazalt camı (teçhizat) |
| Sintaktik köpük | Mühendislik malzemesi | Epoksi içinde içi boş cam mikroküreler; derinlik arttıkça daha yoğun köpük gerekir | Sintaktik köpük (teçhizat) |
| Kitin | Kabukluların dış iskeleti | Kitin-protein lif ağı + kalsiyum karbonat; her deri değişiminde yenilenir | Kitin (teçhizat) |
| Baca ısısı | Hidrotermal sahalar (okyanus ortası sırtlar) | 350–400 °C akışkan (bkz. bölüm 7) | Enerji |
| Kemosentetik topluluklar | Bacalar, soğuk sızıntılar, tuzlu su havuzu kenarları | Bakteri matları, midye yatakları, tüp kurtları | Gıda, Biyokütle |
| Deniz karı ve balina düşüşü | Tüm derin taban | Yüzeyden gelen organik madde; onlarca yıllık leş vahaları | Biyokütle |
| Karanlık oksijen (tartışmalı) | Nodül tarlaları, ~4.000 m | Nodüllerin elektrolizle oksijen üretebileceği hipotezi; doğrulanmadı | Oksijen (yalnız deneysel araştırma dalı) |

Not: Nodül, kabuk ve nadir toprak değerleri kaynak tahminidir, ekonomik rezerv değildir. Pullu
ayaklı salyangozun yaşadığı Hint Okyanusu bacaları maden arama ruhsatı altındadır; bu alanlar oyunda
kazı alanı olarak sunulmaz.

## 7. Enerji Kaynakları

| Kaynak | Değer | Not |
|---|---|---|
| Kara baca | 350–400 °C | Asidik, metal sülfürlü akışkan; yüksek basınç kaynamayı engeller |
| Rekor baca | 407 °C sürekli · 464 °C ~20 sn | Turtle Pits, ~2.990 m (≈299 bar), kritik noktada; Sisters Peak'te anlık 464 °C. Kaynaklar farklı değerler verir |
| Beyaz baca | ~250–300 °C | Daha serin, alkali; silika, barit, kalsiyum sülfat çökelir |
| Lost City alkali bacaları | 40–90 °C, pH ~10 | Isı magmadan değil serpantinleşmeden; H₂ ve CH₄ zengini; 60 m'lik karbonat bacalar |
| Metan hidrat | 1 m³ ≈ 164 m³ metan | Küresel tahmin çok belirsiz (250.000–700.000 trilyon fit³); ticari üretim yok |
| Kompakt denizaltı reaktörü | Gemi ömrü boyu yakıt (~33 yıl) | Eski sınıflar ~8 yılda bir yakıt alırdı; NR-1 (1969–2008) ~900 m'ye inen nükleer araştırma denizaltısıydı, sınırı yiyecek ve havaydı (~30 gün). Yalnız kamuya açık bilgi |

## 8. Keşifler

| Yıl | Keşif | Özet |
|---|---|---|
| 1951 | En derinde yaşam | Galathea seferi Filipin Çukuru'nun en derin kesimlerinden amfipod, derisidikenli ve bakteri topladı |
| 1977 | Hidrotermal bacalar | Alvin, Galápagos Yarığı'nda kimyasal enerjiyle yaşayan hayvan topluluklarını buldu; 1979'da 350 °C'yi aşan kara bacalar |
| 2011 | En derin dev tek hücreliler | Ksenofiyoforlar Sirena Derinliği'nde 10.641 m'de (önceki rekor ~7.500 m) |
| 2019 | Beş Derinlik Seferi | Beş okyanusun en derin noktalarına ilk insanlı inişler; ~550.000 km² haritalandı, %61'i ilk kez |
| 2019 → 2022 | Amfipodun alüminyum kalkanı | 2019 iddiası; 2022'de sinyalin bromdan kaynaklanabileceği ve kalkanın olmadığı bildirildi |
| 2023 | En derin balık | 8.336 m gözlem, 8.022 m yakalama (2022 seferi) |
| 2023 | Clarion-Clipperton biyoçeşitliliği | 5.578 tür; %88–92'si adlandırılmamış (tahmin, muhtemelen biraz yüksek) |
| 2024 | Karanlık oksijen | Aşağıda |
| 2025 | Süperdev amfipodun yayılımı | Okyanusların ~%59'unda yaşayabileceği modellendi (3.890–8.931 m) |
| 2025 | Koloss kalamarın ilk görüntüsü | Güney Sandwich yakınında, 600 m, ~30 cm saydam yavru |

**Karanlık oksijen (Sweetman vd., Nature Geoscience, Temmuz 2024).** Clarion-Clipperton Bölgesi'nde
(~4.000 m) nodüllerle kapatılan bentik odacıklarda oksijen beklendiği gibi azalmadı, iki günde arka
plan değerinin ~3 katına çıktı. Nodül yüzeyleri arasında **~0,95 V**'a varan gerilim ölçüldü; ekip
nodüllerin "jeo-batarya" gibi davranıp deniz suyunu elektrolizle ayırabileceğini önerdi (suyu ayırmak
için ~1,5 V gerekir; kümelenmiş nodüllerde gerilimlerin toplanabileceği düşünüldü).
**Tartışma:** Bulgu bağımsız olarak tekrarlanmadı. İtirazlar: başka bentik odacık çalışmalarında yalnız
oksijen tüketimi görülmesi, gerilimin elektroliz için yetersizliği, sinyalin ölçüm sisteminden
gelebileceğine dair kontrol verileri ve elektrolizin yan ürünü hidrojenin raporlanmaması. Eleştirilerin
önemli kısmı nodül madenciliği yapmak isteyen bir şirketin bilim insanlarından geldi; bağımsız
araştırmacılar da ekolojik önemin belirsiz olduğunu vurguluyor. Oyunda ana oksijen kaynağı yapılmaz.

## 9. Doğa Olayları

| Olay | Örnek | Ne olur | Oyunda |
|---|---|---|---|
| Bulanıklık akıntısı | 1929 Grand Banks | M 7,2 deprem ~20.000 km²'lik heyelanı tetikledi; 100–150 km³ tortu saatte ~60–100 km hızla ~500 km aktı, 12 kabloyu kopardı (6'sı ~13 saatte sırayla) | Kanyon ağzındaki üslerde görüş düşer, kablo/boru bağlantıları kopar; onarım görevi |
| Çukur (megathrust) depremi | 2011 Tōhoku (Mw 9,1); 2025 Kamçatka (Mw 8,8) | Levha sınırı bir anda kayar; Tōhoku'da ~500 × 200 km kırıldı, çukura yakın kayma ~50 m'yi aştı | "Tabanın kayması": yapılar hasar alır, yeni kaya yüzeyleri ve geçici maden sahaları açılır |
| Denizaltı volkan patlaması | 2022 Hunga Tonga–Hunga Ha'apai | Bulut ~30 dakikada 58 km'ye çıktı (Guinness: 57 km), mezosfere ulaşan ilk volkan bulutu; Pinatubo'nun 1,5 katı | Basınç dalgası ve kül: kısa hasar, ardından bacalarda geçici Enerji artışı |
| Soğuk sızıntı | Meksika Körfezi | Metan ve hidrojen sülfür çevre sıcaklığında sızar; kemosentetik topluluklar, karbonat kayalıklar; tüp kurtları 170–250+ yıl yaşar | Kabarcık sütunlu kaynak noktası: Enerji (metan) ve Gıda |
| Tuzlu su havuzu ("ölüm gölü") | Meksika Körfezi (~1.280 m); Kızıldeniz NEOM (1.770 m) | Yoğun tuzlu su karışmadan göl gibi durur; 4–5 kat tuzlu, oksijensiz; içine düşen hayvan ölür, kenarında midyeler yaşar | Tehlikeli bölge: dron hasar alır; kenardaki midye yatakları Gıda |
| Yoğun deniz karı | Yüzeydeki üretim patlamalarından sonra | Organik madde haftalar içinde tabana yağar | Ortam efekti; Gıda ve Biyokütle geçici artar |
| Balina düşüşü | Santa Catalina Havzası, 1.240 m | Çöpçülerden kükürt bakterilerine onlarca yıllık vaha | Aşamaları hızlandırılmış nadir Biyokütle olayı |

Afetlerin adı, tarihi ve kurbanları oyun olayı olarak canlandırılmaz; olaylar yalnızca doğal süreci örnek alır.

## 10. Derin Deniz Araçları

| Araç | Yıl | Tür | En derin | Mühendislik | Ders |
|---|---|---|---|---|---|
| Trieste | 1960 | Batiskaf | 10.916 m | ~85.000 L benzinli şamandıra (sudan hafif, az sıkışır); ~2 m çaplı çelik küre; çıkışta ~9 ton demir bilye safrası bırakılır; inişte pencere çatladı | Sıkışmaz sıvı + bırakılabilir safra: basit ama hantal |
| Deepsea Challenger | 2012 | Tek kişilik denizaltı | 10.908 m | Dikey "torpido"; hacmin ~%70'i taşıyıcı sintaktik köpük; 109 cm, 6,4 cm çelik küre; 11,8 ton | Köpük hem kaldırır hem taşır; dikey gövde hızlı iner |
| Limiting Factor | 2019 | İnsanlı denizaltı | 10.935 m | 90 mm titanyum küre, 1,5 m iç çap, %99,933 küresellik; 1.127 bar tasarım, 14.000 m test; en az 8 kez Challenger Deep | Sertifikalı tekrarlı tam derinlik: prototipten iş aracına |
| Fendouzhe | 2020 | İnsanlı denizaltı | 10.909 m | 3 kişilik yeni titanyum alaşımı kabin; 13 dalışın 8'i >10.000 m | Bilim ekibini doğrudan dibe taşıyan çok kişilik sınıf |
| Shinkai 6500 | 1991 | İnsanlı denizaltı | 6.500 m | 73,5 mm Ti-6Al-4V küre, 2,0 m iç çap; 3 kişi; 8 saatlik dalış; sintaktik köpük | Kanıtlanmış alaşım ve bakım: on yıllarca güvenilir dalış |
| Alvin | 1964 (2022 yenileme) | İnsanlı denizaltı | 6.500 m | Derece 4.500 → 6.500 m; ~7,6 cm duvarlı titanyum küre; 2021'de köpük bağlantı hasarı; 2022'de 6.453 m | Modüler yükseltme mümkün, ama her parça yeni test ister |
| Nereus | 2009 | Hibrit ROV (insansız) | 10.902 m | ~1.500 adet 9 cm seramik kaldırma küresi; 2014'te Kermadec'te 9.990 m'de büyük olasılıkla küre çökmesiyle kayboldu | Hafiflik ile güvenlik payı dengesi; tek parça tüm aracı kaybettirebilir |

**Basınç sınıfları önerisi (P1–P10).** P1 = 6.500 m, ardından 500 m adımlar, P10 = Challenger Deep.

| Sınıf | Derinlik | ≈Basınç | Okyanustaki karşılığı |
|---|---|---|---|
| P1 | 6.500 m | 665 bar | Shinkai 6500 ve Alvin sınırı; hadal 6.000 m'de başlar |
| P2 | 7.000 m | 715 bar | En derin ahtapot (6.957 m) |
| P3 | 7.500 m | 765 bar | Java (7.187 m), Factorian (7.432 m) |
| P4 | 8.000 m | 820 bar | En derin dekapod (7.703 m); Mariana salyangoz balığının bol olduğu kuşak |
| P5 | 8.500 m | 870 bar | Atacama, Meteor, Brownson; balık sınırı 8.200–8.400 m |
| P6 | 9.000 m | 925 bar | Süperdev amfipodun en derin kaydı (8.931 m) |
| P7 | 9.500 m | 975 bar | Izu-Ogasawara'da dev amfipod örnekleri (9.450 m) |
| P8 | 10.000 m | 1.030 bar | Kuril-Kamçatka, Izu-Ogasawara; Nereus'un kaybolduğu 9.990 m |
| P9 | 10.500 m | 1.080 bar | Kermadec / Scholl Deep (10.047 m) |
| P10 | 10.935 m | 1.126 bar | Filipin, Tonga, Mariana; tam derinlik araçları |

Araçlardan çıkan dersler: titanyum küre kalınlığı ve küreselliği derinlik sınıfını belirler;
kaldırma için gereken sintaktik köpük derinlikle ağırlaşır; bırakılabilir safra güvenli çıkışın
anahtarıdır; tekrarlı dalış için sertifika ve bakım gerekir. Hassas konular için
`veri/bilim.json` → `hassas_konular`.

## 11. Kaynakça

1. [Layers of the Ocean](https://www.noaa.gov/jetstream/ocean/layers-of-ocean) — NOAA (JetStream)
2. [Depth zones of the ocean](https://worldoceanreview.com/en/depth-zones-of-the-ocean/) — World Ocean Review, 2013
3. [Abyssal zone](https://www.britannica.com/print/article/2489) — Encyclopaedia Britannica
4. [Mesopelagic zone](https://en.wikipedia.org/wiki/Mesopelagic_zone) — Wikipedia
5. [How does pressure change with ocean depth?](https://oceanservice.noaa.gov/facts/pressure.html) — NOAA Ocean Service
6. [Ocean Facts: deep-sea animals and pressure](https://oceanexplorer.noaa.gov/facts/animal-pressure.html) — NOAA Ocean Exploration
7. [Revised depth of the Challenger Deep from submersible transects](https://repository.library.noaa.gov/view/noaa/33477) — Greenaway vd., Deep-Sea Research I, 2021
8. [Deep CTD casts in the Challenger Deep, Mariana Trench](https://link.springer.com/doi/10.1007/s10872-005-0053-z) — Taira vd., Journal of Oceanography, 2005
9. [Water properties and bottom water patterns in hadal trench environments](https://os.copernicus.org/articles/20/123/2024/) — Kolbusz vd., Ocean Science, 2024
10. [All About Trenches (HADES)](https://web.whoi.edu/hades/all-about-trenches) — WHOI
11. [Effect of Temperature and Salinity on Compressibility](https://joa.ucsd.edu/dpo/dpo_joa_examples/chapter_3/3c.html) — Descriptive Physical Oceanography, 2011
12. [Dive to the ultimate abyss](https://www.dnv.com/expert-story/maritime-impact/Dive-to-the-ultimate-abyss/) — DNV
13. [High-resolution multibeam sonar bathymetry of the deepest place in each ocean](https://nora.nerc.ac.uk/id/eprint/530930/) — Bongiovanni vd., Geoscience Data Journal, 2022
14. [Deepest points of the Indian Ocean and Southern Ocean revealed](https://www.bgs.ac.uk/?p=72154) — British Geological Survey, 2021
15. [Deepest point in the sea](https://www.guinnessworldrecords.com/world-records/deepest-point-in-the-sea) — Guinness World Records
16. [Five Deeps Expedition — Tonga Trench / Horizon Deep](https://fivedeeps.com/?p=2611) — Five Deeps Expedition, 2019
17. [Kermadec Trench](https://www.britannica.com/place/Kermadec-Trench) — Encyclopaedia Britannica
18. [New Zealand Gazette notice 2023-ln3297](https://gazette.govt.nz/notice/id/2023-ln3297) — New Zealand Gazette, 2023
19. [Chinese and New Zealand scientists dive to one of the ocean's deepest regions](https://astrobiology.com/2022/12/04/chinese-and-new-zealand-scientists-dive-to-one-of-the-oceans-deepest-regions/) — Astrobiology.com, 2022
20. [Philippine Trench](https://www.britannica.com/print/article/456395) — Encyclopaedia Britannica
21. [Emden Deep](https://en.wikipedia.org/wiki/Emden_Deep) — Wikipedia
22. [1st Filipino to reach Emden Deep](https://upd.edu.ph/1st-filipino-to-reach-emden-deep-3rd-deepest-spot-on-earth/) — UP Diliman, 2021
23. [Emden Deep yields dirty secret in Philippine Trench: Trash](https://newsinfo.inquirer.net/1414777/emden-deep-yields-dirty-secret-trash) — Philippine Daily Inquirer, 2021
24. [Kuril Trench](https://www.britannica.com/place/Kuril-Trench) — Encyclopaedia Britannica
25. [Prokaryotic communities of the abyssal-hadal benthic-boundary layer of the Kuril Kamchatka Trench](https://link.springer.com/10.1186/s40793-023-00522-9) — Gorrasi vd., Environmental Microbiome, 2023
26. [RV SONNE SO-250 (KuramBio II) weekly report 6](https://www.ldf.uni-hamburg.de/en/sonne/wochenberichte/wochenberichte-sonne/so250-252/so250-wob6e.pdf) — Universität Hamburg, 2016
27. [Magnitude 8.8 earthquake strikes western Pacific](https://eos.org/research-and-developments/magnitude-8-8-earthquake-strikes-western-pacific) — Eos, 2025
28. [Izu–Ogasawara Trench](https://en.wikipedia.org/wiki/Izu%E2%80%93Ogasawara_Trench) — Wikipedia
29. [First humans dive Japan's ocean deeps](https://divernet.com/scuba-news/first-humans-dive-japans-ocean-deeps/) — Divernet, 2022
30. [Milwaukee Depth](https://www.britannica.com/place/Milwaukee-Depth) — Encyclopaedia Britannica
31. [Atacama Trench Press Release](https://caladanoceanic.com/expeditions/chile/pressrelease/) — Caladan Oceanic, 2022
32. [Java Trench](https://www.britannica.com/place/Java-Trench) — Encyclopaedia Britannica
33. [New maximum depth record for bony fish (8336 m, Izu-Ogasawara Trench)](https://research-repository.uwa.edu.au/en/publications/new-maximum-depth-record-for-bony-fish-teleostei-scorpaeniformes-/) — Jamieson vd., Deep-Sea Research I, 2023
34. [Deepest ever fish filmed at a depth of 8,336 metres](https://nhm.ac.uk/discover/news/2023/april/deepest-ever-fish-filmed-depth-8336-metres.html) — Natural History Museum, 2023
35. [Behold the Deepest Fish Ever Filmed](https://www.smithsonianmag.com/smart-news/behold-the-deepest-fish-ever-filmed-180981937/) — Smithsonian Magazine, 2023
36. [Marine fish may be biochemically constrained from inhabiting the deepest ocean depths](https://pmc.ncbi.nlm.nih.gov/articles/PMC3970477) — Yancey vd., PNAS, 2014
37. [Pseudoliparis swirei, Mariana snailfish](https://fishbase.se/summary/68464) — FishBase
38. [Deepest cusk eel](https://www.guinnessworldrecords.com/world-records/736448-deepest-cusk-eel) — Guinness World Records
39. [Abyssobrotula galatheae, Galathea cuskeel](https://fishbase.se/summary/17075) — FishBase
40. [An aluminum shield enables the amphipod Hirondellea gigas to inhabit deep-sea environments](https://pmc.ncbi.nlm.nih.gov/articles/PMC6449124/) — Kobayashi vd., PLoS ONE, 2019
41. [Unusual bromine enrichment in the gastric mill and setae of the hadal amphipod Hirondellea gigas](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC9352070/) — Okada vd., PLoS ONE, 2022
42. [The supergiant amphipod Alicella gigantea may inhabit over half of the world's oceans](https://pmc.ncbi.nlm.nih.gov/articles/PMC12092127/) — Maroni vd., Royal Society Open Science, 2025
43. [Study finds deep-sea giant may inhabit more than half the world's oceans](https://www.uwa.edu.au/news/article/2025/may/study-finds-deep-sea-giant-may-inhabit-more-than-half-the-worlds-oceans) — University of Western Australia, 2025
44. [The supergiant amphipod Alicella gigantea from hadal depths in the Kermadec Trench](https://sealifebase.ca/references/FBRefSummary.php?id=137514) — Jamieson vd., Deep-Sea Research II, 2013
45. [Bathynomus giganteus, Giant isopod](https://sealifebase.org/summary/23189) — SeaLifeBase
46. [Giant isopod, Windows to the Deep 2021](https://oceanexplorer.noaa.gov/multimedia/okeanos-explorations-ex2107-gallery-media-dive09-isopod/) — NOAA Ocean Exploration, 2021
47. [Melanocetus johnsonii, Humpback anglerfish](https://www.fishbase.org/summary/2292) — FishBase
48. [Melanocetus johnsonii (Deep-Sea Guide)](https://dsg.mbari.org/dsg/view/concept/Melanocetus%20johnsonii) — MBARI
49. [Vampire squid — Animals of the Deep](https://www.mbari.org/animal/vampire-squid/) — MBARI
50. [MBARI researchers discover what vampire squids eat](https://www.mbari.org/news/mbari-researchers-discover-what-vampire-squids-eat-its-not-what-you-think/) — MBARI, 2012
51. [Global deep-sea stable isotope assessment of Vampire Squid](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC6910912/) — Scientific Reports, 2019
52. [Giant Squid](https://ocean.si.edu/giant-squid) — Smithsonian Ocean
53. [I Spy a Giant Squid Eye](https://www.smithsonianmag.com/blogs/national-museum-of-natural-history/2023/09/21/i-spy-a-giant-squid-eye/) — Smithsonian Magazine, 2023
54. [First-ever observations of a live giant squid in the wild](https://pmc.ncbi.nlm.nih.gov/articles/PMC1559985) — Kubodera ve Mori, Proc. R. Soc. B, 2005
55. [Giant squid filmed in the Gulf of Mexico](https://earthsky.org/earth/giant-squid-video-photos/) — EarthSky, 2019
56. [Monsters of the Ocean: The Elusive Giant Squid](https://www.bibalex.org/SCIplanet/en/Article/Details.aspx?id=12382) — Bibliotheca Alexandrina SCIplanet
57. [First confirmed footage of a colossal squid](https://schmidtocean.org/first-colossal-squid-footage/) — Schmidt Ocean Institute, 2025
58. [Colossal squid (collections topic)](https://collections.tepapa.govt.nz/topic/588) — Te Papa Tongarewa
59. [Running the gauntlet — Deep-sea animals face multiple dangers in their daily migration](https://www.mbari.org/news/running-the-gauntlet-deep-sea-animals-face-multiple-dangers-in-their-daily-migration/) — MBARI, 2020
60. [Praya dubia](https://sealifebase.ca/summary/44678) — SeaLifeBase
61. [The making of natural iron sulfide nanoparticles in a hot vent snail](https://pmc.ncbi.nlm.nih.gov/articles/PMC6789796) — Okada vd., PNAS, 2019
62. [First animal officially endangered by deep-sea mining](https://australian.museum/learn/news/blog/first-animal-officially-endangered-by-deep-sea-mining/) — Australian Museum, 2019
63. [Discovery of yeti crab](https://www.mbari.org/discovery-of-yeti-crab/) — MBARI, 2006
64. [Yeti Crab (What's in a Name)](https://whatsinaname.hmnh.harvard.edu/yeti-crab) — Harvard Museum of Natural History
65. [Host-Microbe Interactions in the Chemosynthetic Riftia pachyptila Symbiosis](https://pmc.ncbi.nlm.nih.gov/articles/PMC6918071) — Hinzke vd., mBio, 2019
66. [Thermal Limit for Metazoan Life in Question: In Vivo Heat Tolerance of the Pompeii Worm](https://pmc.ncbi.nlm.nih.gov/articles/PMC3667023) — Ravaux vd., PLoS ONE, 2013
67. [Alvinella pompejana](https://en.wikipedia.org/wiki/Alvinella_pompejana) — Wikipedia
68. [First in situ observation of Cephalopoda at hadal depths](https://link.springer.com/article/10.1007/s00227-020-03701-1) — Jamieson ve Vecchione, Marine Biology, 2020
69. [Deepest octopus](https://www.guinnessworldrecords.com/world-records/83167-deepest-octopus) — Guinness World Records
70. [Deepest squid](https://www.guinnessworldrecords.com/world-records/688048-deepest-squid) — Guinness World Records
71. [Deepest decapod](https://www.guinnessworldrecords.com/world-records/626036-deepest-decapod) — Guinness World Records
72. [Scientists ID Giant Amoebas in the Extreme Deep](https://scripps.ucsd.edu/news/scientists-id-giant-amoebas-extreme-deep) — Scripps Institution of Oceanography, 2011
73. [Deepest echinoderm](https://www.guinnessworldrecords.com/world-records/732437-deepest-echinoderm) — Guinness World Records
74. [Functional effects of the hadal sea cucumber Elpidia atakama](https://eprints.soton.ac.uk/210855) — Jamieson vd., Marine Biology, 2011
75. [Barreleye fish with transparent head](https://www.mbari.org/news/news_releases/2009/barreleye/barreleye.html) — MBARI, 2009
76. [Macropinna microstoma (Deep-Sea Guide)](https://dsg.mbari.org/dsg/view/concept/Macropinna%20microstoma) — MBARI
77. [Frill Shark, Chlamydoselachus anguineus](https://australian.museum/learn/animals/fishes/frill-shark-chlamydoselachus-anguineus-garman-1884/) — Australian Museum
78. [Chlamydoselachus anguineus, Frilled shark](https://www.fishbase.org/summary/635) — FishBase
79. [What is marine snow?](https://oceanservice.noaa.gov/facts/marinesnow.html) — NOAA Ocean Service
80. [Life at the Bottom: The Prolific Afterlife of Whales](https://www.scientificamerican.com/article/the-prolific-afterlife-of-whales/) — Scientific American, 2010
81. [Tell me about: Whale falls](https://www.floridamuseum.ufl.edu/earth-systems/blog/tell-me-about-whale-falls) — Florida Museum of Natural History
82. [1979 — Oases of Exotic Life](https://www.whoi.edu/feature/history-hydrothermal-vents/discovery/1979.html) — WHOI
83. [What are cold seeps? (fact sheet)](https://oceanexplorer.noaa.gov/edu/materials/what-are-cold-seeps-fact-sheet.pdf) — NOAA Ocean Exploration
84. [Tube worms in deep sea discovered to have record long life spans](https://science.psu.edu/news/tube-worms-deep-sea-discovered-have-record-long-life-spans) — Penn State, 2000
85. [Manganese Nodules](https://www.geomar.de/en/research/marine-resources/mmr/mineral-resources-2-1-1) — GEOMAR
86. [Deep-sea potato fields](https://geoexpro.com/deep-sea-potato-fields/) — GEO ExPro, 2021
87. [Deep-ocean polymetallic nodules and cobalt-rich ferromanganese crusts in the global ocean](https://www.usgs.gov/publications/deep-ocean-polymetallic-nodules-and-cobalt-rich-ferromanganese-crusts-global-ocean-new) — Hein ve Mizell, USGS, 2022
88. [Deep-ocean mineral deposits as a source of critical metals](https://www.usgs.gov/publications/deep-ocean-mineral-deposits-a-source-critical-metals-high-and-green-technology) — Hein vd., Ore Geology Reviews, 2013
89. [Polymetallic nodules — ISA workshop presentation](https://www.isa.org.jm/wp-content/uploads/2022/12/Jauhari.pdf) — International Seabed Authority
90. [Over 5,000 species found in a seabed set for deep-sea mining](https://www.courthousenews.com/over-5000-species-found-in-a-seabed-set-for-deep-sea-mining-study-says/) — Courthouse News Service, 2023
91. [Deep-sea mining test approved by regulator](https://dialogue.earth/en/ocean/deep-sea-mining-test-approved-by-regulator/) — Dialogue Earth, 2022
92. [Cobalt-rich ferromanganese crusts](https://www.geomar.de/en/research/marine-resources/mmr/mineral-resources-2-1-2) — GEOMAR
93. [Billions of tonnes](https://geoexpro.com/billions-of-tonnes/) — GEO ExPro, 2021
94. [USGS Open-File Report 85-18 (Marshall Islands crusts)](https://pubs.usgs.gov/of/1985/0018/report.pdf) — USGS, 1985
95. [Cobalt- and platinum-rich ferromanganese crusts from the Marshall Islands](https://pubs.usgs.gov/publication/70014756) — USGS, 1988
96. [Seafloor Massive Sulfides](https://www.geomar.de/en/research/marine-resources/mmr/mineral-resources-2-1) — GEOMAR
97. [Volcanogenic massive sulfide occurrence model](https://www.usgs.gov/publications/volcanogenic-massive-sulfide-occurrence-model) — USGS, 2010
98. [The tremendous potential of deep-sea mud as a source of rare-earth elements](https://www.t.u-tokyo.ac.jp/soee/press/setnws_201804111104201019928526.html) — Tokyo Üniversitesi (Takaya vd.), 2018
99. [Rare-earth-rich deep-sea mud near Minamitorishima](https://www.waseda.jp/top/en/news/58329) — Waseda University, 2018
100. [Japan Retrieves Rare Earth Mud from Deep Seabed](https://www.marinetechnologynews.com/news/japan-retrieves-earth-seabed-658253) — Marine Technology News, 2026
101. [USGS Open-File Report 96-272, chapter 2](https://pubs.usgs.gov/of/1996/of96-272/ch02.html) — USGS, 1996
102. [USGS Open-File Report 92-299](https://pubs.usgs.gov/of/1992/0299/report.pdf) — USGS, 1992
103. [A primer on the geological occurrence of gas hydrate](https://www.usgs.gov/publications/a-primer-geological-occurrence-gas-hydrate) — USGS, 1998
104. [Methane Hydrate R&D Program fact sheet](https://www.energy.gov/sites/default/files/2020/12/f81/Hydrates%20RD%20Program%20Factsheet%20Dec%202020.pdf) — U.S. Department of Energy, 2020
105. [Mineral Resource of the Month: Titanium](https://www.earthmagazine.org/article/mineral-resource-month-titanium/) — EARTH Magazine, 2018
106. [Pillow lava (glossary)](https://volcanoes.usgs.gov/vsc/glossary/pillow_lava.html) — USGS
107. [Using submarine lava pillars to record mid-ocean ridge eruption dynamics](https://www.usgs.gov/publications/using-submarine-lava-pillars-record-mid-ocean-ridge-eruption-dynamics) — USGS
108. [Epoxy/hollow glass microsphere syntactic foams — A review](https://uhra.herts.ac.uk/id/eprint/9681/) — University of Hertfordshire
109. [Low cost deep water efficient buoyancy (US6153294A)](https://patents.google.com/patent/US6153294) — Google Patents, 2000
110. [Biomineralizations: insights and prospects from crustaceans](https://borea.mnhn.fr/sites/default/files/pdfs/Luquet-Zookeys-2012.pdf) — Luquet, ZooKeys, 2012
111. [Hydrothermal vents (video captions)](https://www.oceanexplorer.noaa.gov/edu/multimedia-resources/dsd/media/hydrothermal-vents-captions.vtt) — NOAA Ocean Exploration
112. [Hydrothermal venting above the critical point of seawater, 5°S on the Mid-Atlantic Ridge](https://cris-portal.uni-muenster.de/portal/en/publication/19368641) — Koschinsky vd., Geology, 2008
113. [Record hotspot found underwater](https://www.bioedonline.org/news/nature-news-archive/record-hotspot-found-underwater/) — BioEd Online (Nature News), 2006
114. [InterRidge Vents Database — Sisters Peak](https://vents-data.interridge.org/node/993) — InterRidge
115. [An off-axis hydrothermal vent field near the Mid-Atlantic Ridge at 30° N](https://home.uevora.pt/~cribeiro/CO2Seq/Kelley%20et%20al%202001%20-%20Nature.pdf) — Kelley vd., Nature, 2001
116. [A serpentinite-hosted ecosystem: the Lost City hydrothermal field](https://home.uevora.pt/~cribeiro/CO2Seq/Kelley%20et%20al%202005%20-%20Science%20Lost%20City.pdf) — Kelley vd., Science, 2005
117. [Life-of-the-Ship Reactors and Accelerated Testing](https://fas.org/wp-content/uploads/media/Life-of-the-Ship-Reactors-and-Accelerated-Testing.pdf) — Federation of American Scientists
118. [Nuclear Power Plants on New Submarines May Last 40-Plus Years](https://csbaonline.org/print/1419) — CSBA
119. [NR-1: The Beginning](https://ussnautilus.org/nr-1-the-beginning/) — Submarine Force Library & Museum
120. [Evidence of dark oxygen production at the abyssal seafloor](https://www.nature.com/articles/s41561-024-01480-8) — Sweetman vd., Nature Geoscience, 2024
121. [Metallic Nodules Create Oxygen in the Ocean's Abyss](https://eos.org/articles/metallic-nodules-create-oxygen-in-the-oceans-abyss) — Eos, 2024
122. [Deep-ocean floor produces its own 'dark oxygen'](https://news.northwestern.edu/stories/2024/july/deep-ocean-floor-produces-its-own-dark-oxygen/) — Northwestern University, 2024
123. [Matters arising: Dark oxygen production (pre-print)](https://metals.co/wp-content/uploads/2024/09/Matters_arising_Dark_Oxygen_Production_PRE-PRINT.pdf) — The Metals Company, 2024
124. [The dark oxygen controversy](https://geoforskning.no/?p=60315) — Geoforskning.no, 2026
125. [Benchmarks: November 18, 1929: Turbidity currents snap trans-Atlantic cables](https://www.earthmagazine.org/article/benchmarks-november-18-1929-turbidity-currents-snap-trans-atlantic-cables/) — EARTH Magazine
126. [M 9.1 - 2011 Great Tohoku Earthquake, Japan](https://earthquake.usgs.gov/earthquakes/eventpage/official20110311054624120_30) — USGS
127. [Teachable Moment: M9.0 Japan earthquake of 11 March 2011](https://web-archive-2022.iris.edu/hq/files/programs/education_and_outreach/retm/tm_110311JAPAN/110311JAPAN.pdf) — IRIS, 2011
128. [Tonga Volcano Plume Reached the Mesosphere](https://science.nasa.gov/earth/earth-observatory/tonga-volcano-plume-reached-the-mesosphere-149474/) — NASA Earth Observatory, 2022
129. [Highest volcanic plume on Earth](https://www.guinnessworldrecords.com/world-records/763274-highest-volcanic-plume-on-earth) — Guinness World Records
130. [How this 'Jacuzzi of Despair' formed beneath the Gulf of Mexico](https://csmonitor.com/Science/2016/1102/How-this-Jacuzzi-of-Despair-formed-beneath-the-Gulf-of-Mexico) — The Christian Science Monitor, 2016
131. [Rare deep-sea brine pools discovered in Red Sea](https://news.miami.edu/rosenstiel/stories/2022/07/rare-deep-sea-brine-pools-discovered-in-red-sea.html) — University of Miami, 2022
132. [Bathyscaphe Trieste](https://geology.com/records/bathyscaphe-trieste.shtml) — Geology.com
133. [Lucky Day: Navy Celebrates 60th Anniversary of Deepest Ocean Dive](https://www.onr.navy.mil/media-center/news-releases/lucky-day-navy-celebrates-60th-anniversary-deepest-ocean-dive) — Office of Naval Research, 2020
134. [Trieste (bathyscaphe)](https://en.wikipedia.org/wiki/Trieste_%28bathyscaphe%29) — Wikipedia
135. [HOV DEEPSEA CHALLENGER](https://www.whoi.edu/?p=13538) — WHOI
136. [First dive to the Challenger Deep by a crewed vessel (solo)](https://www.guinnessworldrecords.com/world-records/613583-first-dive-to-the-challenger-deep-by-a-crewed-vessel-solo) — Guinness World Records, 2012
137. [Full Ocean Depth Submersible](https://caladanoceanic.com/?p=425) — Caladan Oceanic
138. [Giant leaps](https://superyachtnews.com/fleet/giant-leaps) — SuperyachtNews
139. [Submersible Fendouzhe Completing 10000-meter Deep-diving Trial](https://english.cas.cn/Special_Reports/Highlights_of_2020_Top_12_Achievements_in_CAS/Submersible_Fendouzhe_Completing_10000_meter_Deep_diving_Trial/202011/t20201111_249059.shtml) — Chinese Academy of Sciences, 2020
140. [Submersible's developers discuss 'incredible' mission](https://subsites.chinadaily.com.cn/en/2020-12/08/c_573338.htm) — China Daily, 2020
141. [Deep-sea Submergence Research Vehicle SHINKAI 6500](https://www.jamstec.go.jp/e/about/equipment/ships/shinkai6500.html) — JAMSTEC
142. [SHINKAI 6500 (IRSO presentation)](https://irso.info/wp-content/uploads/IRSO_yanagitani_.pdf) — International Research Ship Operators
143. [Human-Occupied Submersible Alvin Makes Historic Dive Reaching 6,453 Meters](https://oceannews.com/news/science-technology/human-occupied-submersible-alvin-makes-historic-dive-reaching-6-453-meters/) — Ocean News & Technology, 2022
144. [Navy-Owned Deep-Diving Alvin Being Certified for Operations to 6,500 Meters](https://seapowermagazine.org/navy-owned-deep-diving-alvin-being-certified-for-operations-to-6500-meters/) — Seapower Magazine, 2021
145. [Upgraded Alvin Sub Heads for West Coast](https://www.hydro-international.com/content/news/upgraded-alvin-sub-heads-for-west-coast) — Hydro International
146. [Hybrid Remotely Operated Vehicle 'Nereus' Reaches Deepest Part of the Ocean](https://www.whoi.edu/press-room/news-release/hybrid-remotely-operated-vehicle-nereus-reaches-deepest-part-of-the-ocean) — WHOI, 2009
147. [Nereus Soars to the Ocean's Deepest Trench](https://www.whoi.edu/oceanus/feature/nereus-soars-to-the-oceans-deepest-trench/) — WHOI Oceanus, 2009
148. [Nereus Lost](https://www.whoi.edu/press-room/news-release/nereus-lost) — WHOI, 2014
149. [R.I.P. Nereus](https://www.earthmagazine.org/article/rip-nereus/) — EARTH Magazine, 2014
