<!-- Extracted from the user's research document Mobil_Strateji_Oyunlari_Tasarim_Teknik_Arastirma.docx (2026-10-29). Turkish source text; design reference for all sessions. -->

MOBİL STRATEJİ OYUNLARI
Last Asylum, Last Fortress, Dark War Survival ve benzeri oyunlarınoynanış, PvP, ekonomi ve motor mimarisi araştırması
Tasarım çözümlemesi • Sistem haritası • Savaş modeli • Teknik mimari • MVP yol haritası
Hazırlanma tarihi: 29 Ekim 2026Amaç: Android ve iOS için uzun ömürlü, ittifak/PvP odaklı 4X hayatta kalma strateji oyunu geliştirmek
# Belgenin kapsamı ve nasıl okunmalı
Bu dosya; kamuya açık oyun mağazası açıklamaları, erişilebilir oyun rehberleri ve Unity'nin teknik belgeleri temel alınarak hazırlanmış bir tasarım/uygulama araştırmasıdır. Ticari oyunların özel sunucu kodu, veri tabloları, gerçek savaş formülleri ve üretim motoru iç mimarisi kamuya açık değilse bunlar doğrulanmış bilgi gibi sunulmaz.
Belgede üç etiket mantığı kullanılır: (1) Kaynakla doğrulanan özellik: mağaza/resmî kaynakta açıkça yer alan bilgi. (2) Tasarım çıkarımı: gözlemlenebilir özelliklerden hareketle yapılan makul analiz. (3) Önerilen uygulama: geliştirilecek yeni oyunda kullanılabilecek somut çözüm.
Kısa sonuç: Bu tür oyunların çekirdeği tek bir savaş mekaniği değildir. Şehir ekonomisi, karakter koleksiyonu, süreli gelişim, harita hedefleri, ittifak koordinasyonu, sezonluk rekabet ve sunucu tarafında doğrulanan sonuçlar birbirini besleyen bir sistem olarak tasarlanır.
# İçindekiler
1. Yönetici özeti ve türün temel formülü
2. Referans oyunların karşılaştırması
3. Oyuncu döngüsü, ilerleme ve kaynak ekonomisi
4. Kahraman, birlik ve savaş kompozisyonu
5. PvP, dünya haritası, ittifak ve sezon tasarımı
6. Para kazanma, adalet ve oyuncu tutundurma
7. Teknik motor ve backend mimarisi
8. Örnek savaş simülasyonu ve denge formülleri
9. Veri modeli, API ve anti-cheat yaklaşımı
10. Ekran iskeleti, UX ve içerik üretim sistemi
11. MVP kapsamı, ekip, riskler ve geliştirme yol haritası
12. Kaynaklar ve doğrulama notları
# 1. Yönetici özeti: Bu tür oyunların temel formülü
Last Asylum: Plague mağaza açıklaması ve resmî tanıtımında hastaları tedavi etme, kaynak toplama, üretim binaları kurma, sığınağı büyütme, kahraman ekipleriyle harabeleri geri alma ve bölgesel kontrol temaları öne çıkıyor. Last Fortress: Underground; yeraltı sığınağı inşası, farklı iş becerilerine sahip kurtulanlar, takım sinerjisi, dış keşif ve ittifak desteğini vurguluyor. Dark War Survival ise sığınak kurma, kahramanları geliştirme, araştırma ve ittifaklarla zorlu içeriklere katılma yapısını öne çıkarıyor. Bu ortaklıklar türün güçlü omurgasını gösterir.
Katman
Oyuncuya verdiği his
Oynanış işlevi
Başarı ölçütü
Üs/şehir
Burası benim alanım
Sürekli yatırım ve görsel ilerleme
Günlük geri dönüş, yükseltme kararları
Kaynak ekonomisi
Bir sonraki hedefe yaklaşıyorum
Kıtlık, öncelik ve zaman yönetimi
Kaynakların anlamlı kullanımı
Kahramanlar
Koleksiyon ve bağ kurma
Farklı takım kurulumları
Birden fazla geçerli kompozisyon
Birlikler
Büyük ölçekli güç
Kayıp, eğitim ve yürüyüş maliyeti
Savaş öncesi planlama
Dünya haritası
Daha büyük bir dünyanın parçasıyım
Keşif, kaynak noktaları, hedefler
Harita üzerinde anlamlı karar
İttifak
Bir grubun parçasıyım
Yardım, koordinasyon, ortak hedef
Sosyal bağ ve ortak kazanım
PvP/sezon
Rakiplerle ölçüşüyorum
Risk, prestij, kontrol ve ödül
Adil rekabet ve geri dönüş motivasyonu
Canlı etkinlikler
Bugün yapacak yeni bir şey var
Rutinleri çeşitlendirme
Etkinlik katılımı ve içerik tüketimi
# 1.1 En kritik tasarım ilkesi
Oyuncu aynı anda üç zaman ölçeğinde ilerlemeli: kısa vadede dakikalar içinde tamamlanan görevler; orta vadede saatler/günler süren bina, araştırma ve kahraman geliştirme; uzun vadede haftalar süren ittifak, sezon ve koleksiyon hedefleri. Bunlardan biri eksikse oyun ya fazla sığ ya da yorucu hâle gelir.
# 1.2 Taklit edilmemesi gereken şey
Başarılı bir oyunun görünüşünü veya özel içeriğini birebir kopyalamak yerine, sistemlerin işlevini anlamak gerekir. Aynı “şehir + kahraman + harita + ittifak” omurgası farklı tema, sanat dili, kaynak dengesi, savaş kararları ve özgün sosyal hedeflerle bağımsız bir oyuna dönüştürülebilir.
# 2. Referans oyunların karşılaştırması
# 2.1 Last Asylum: Plague
Kaynakla doğrulanan çekirdek: salgın temalı sığınak; hastaları tedavi etme; kereste, ot, yiyecek ve benzeri malzeme toplama; üretim/ambar/konut binalarını yükseltme; kahraman takımı oluşturma; harabeleri ve kritik şehirleri ele geçirme; Rat Swarm ve Rat King gibi tehditlere karşı savunma.
Tasarım çıkarımı: Oyunun ayırt edici tarafı yalnızca askerî güç değil, “iyileştirme ve yeniden inşa” fantezisidir. Tedavi ve üretim, oyuncuya savaş dışı bir görev de vererek dünyayı yaşanır kılar. Bu tema, kaynak üretimini anlatı ile birleştirmek için kullanılabilir.
Üs içi işler oyuncuya kısa ve anlaşılır hedefler verir: tedavi et, üret, depola, yükselt.
Harita keşfi iç ekonomiyi besler; dışarı çıkmak yalnızca savaş değil kaynak ve alan kazanımıdır.
Farklı kahraman sınıfları, tek bir en yüksek güç sayısına indirgenmeyen kadro kararları yaratabilir.
Özgün bir projede salgın/tedavi temasını seçmek zorunlu değildir; aynı rolü farklı bir tema taşıyabilir.
# 2.2 Last Fortress: Underground
Mağaza açıklamasında yeraltı sığınağının tasarımını genişletme, farklı iş becerilerine sahip hayatta kalanları kullanma, takım sinerjisi, dış dünyada kaynak toplama, ileri üsler ve ittifaklarla birlikte zorlu düşmanlara karşı mücadele yer alıyor.
Tasarım çıkarımı: Üs içi yerleşim/tesis hissi, üretim zinciri ve personel uzmanlığı sistemleriyle birleşince oyuncuya yalnızca bina seviyesi yükseltmekten daha fazlasını yaptırır. Bir karakterin savaş dışında da değerli olması kadro yönetimini derinleştirir.
# 2.3 Dark War Survival
Mağaza açıklaması; sığınak kurma ve savunma, kahramanları işe alma ve eğitme, silah/teknoloji geliştirme ve ittifaklarla dünyayı geri alma temalarını doğruluyor.
Tasarım çıkarımı: Tema ve sistemler oyuncuya sürekli bir hazırlık döngüsü sunar: kaynak topla → üssü güçlendir → kadroyu hazırla → tehdide veya rakibe karşı mücadele et → ödülle tekrar büyü.
# 2.4 Whiteout Survival ve benzer 4X oyunlar
Bu türdeki oyunlar için analiz edilmesi gereken ortak desen; hayatta kalma temasının kaynak ve üs baskısına dönüşmesi, ittifakların harita hedeflerini birlikte ele alması, süreli etkinliklerin rekabet takvimini oluşturması ve kahraman/birlik gelişiminin uzun vadeli bir hedef sağlamasıdır. Bu rapor Whiteout Survival'ın özel sunucu formüllerini veya doğrulanmamış ayrıntılarını gerçekmiş gibi sunmaz; oyun içi güncel değerler geliştirme sırasında doğrudan gözlemlenerek ayrıca ölçülmelidir.
Oyun
Öne çıkan halka
Oyuncu motivasyonu
Yeni oyuna alınabilecek tasarım dersi
Last Asylum: Plague
Tedavi, sığınak, kaynak, harabe/şehir kontrolü
Dünyayı yeniden kurma
Tema ile üretim ve savunmayı birleştir
Last Fortress: Underground
Yeraltı üssü, uzman kurtulanlar, takım sinerjisi
Kendi sığınağını optimize etme
Personelin savaş ve üretim rolü olsun
Dark War Survival
Sığınak, kahraman, araştırma, ittifak
Giderek güçlenen komutan olma
Gelişim hedefleri anlaşılır ve katmanlı olsun
Whiteout Survival / benzer 4X
Üs gelişimi, harita, ittifak ve dönemsel rekabet
Sosyal güç ve sezon prestiji
Tek oyunculu ilerleme ile ittifak hedefleri birbirini beslesin
# 3. Oyuncu döngüsü, ilerleme ve kaynak ekonomisi
# 3.1 Temel döngü
Önerilen temel döngü: Oyuncu oyuna girer → tamamlanmış üretimleri toplar → yeni bina/araştırma başlatır → kahramanlarını geliştirir → haritada keşif veya PvE görevi yapar → ittifak etkinliğine katılır → rapor ve ödülleri alır → bir sonraki hedefini seçer.
Zaman aralığı
Örnek eylem
Tasarım amacı
Kaçınılacak sorun
1–3 dakika
Ödül topla, sıra başlat, günlük görev
Hızlı tatmin
Çok fazla pop-up
5–15 dakika
PvE görevi, takım ayarı, harita keşfi
Aktif oynanış
Her eylemde uzun bekleme
30–120 dakika
İttifak etkinliği, hedef koordinasyonu
Sosyal bağ
Tüm oyuncuları aynı saatte zorlamak
1–3 gün
Bina/araştırma ve kahraman kilometre taşı
Orta vadeli hedef
Tek kaynağın tüm ilerlemeyi kilitlemesi
1–4 hafta
Sezon, bölgesel kontrol, koleksiyon
Uzun vadeli bağlılık
Kazananın sınırsız kartopu etkisi
# 3.2 Kaynak ekonomisi
Başlangıç için az sayıda, farklı kullanım amacı olan kaynak seçilmelidir. Örneğin: yiyecek (nüfus/birlik bakımı), kereste (erken bina), metal (ileri bina/teçhizat), ilaç/enerji (iyileştirme veya görev), araştırma puanı (teknoloji), premium para (sınırlı hızlandırma ve kozmetik). Her kaynağın en az iki anlamlı harcama alanı olması, oyuncunun kararlarını derinleştirir.
Üretim kaynakları zamanla birikir; depolama kapasitesi oyuncuya düzenli toplama sebebi verir ama sık giriş zorunluluğu yaratacak kadar kısa tutulmamalıdır.
İnşaat ve araştırma süreleri ilerleme hissi verir. Hızlandırma ücretleri sabit ve öngörülebilir olmalı; ödeme yapanın tüm rekabeti otomatik kazanması engellenmelidir.
Kaynak akışı, yeni oyuncunun ilk gününde eksik kalmamalı; orta/ileri aşamada ise her kaynağın farklı darboğazı olmalıdır.
Kaynak paketleri ve ödüller sunucu tarafından doğrulanmalı; istemci kendi bakiyesini doğrudan artırmamalıdır.
# 3.3 Bina ilerleme ağacı
Ana merkez/karargâh seviyesi, diğer binalar için üst sınır işlevi görebilir. Bu, yükseltme sırasını anlamlı kılar. Üretim binaları ekonomi sağlar; depo kayıp riskini sınırlar; eğitim tesisi birlik kapasitesi açar; araştırma merkezi kalıcı bonuslar verir; hastane/onarım tesisi kayıp maliyetini azaltır; keşif binası harita içeriğini açar.
Bina grubu
İşlev
İlerleme koşulu örneği
Denge riski
Karargâh
Genel seviye ve içerik kilidi
Önceki seviyenin ana görevleri
Zorunlu tek yükseltme duvarı
Üretim
Saatlik kaynak üretimi
Kaynak ve bina seviyesi
Üretim farkının çok büyümesi
Depo
Korunan kaynak kapasitesi
Karargâh seviyesi
Yağmanın aşırı cezalandırması
Kışla/eğitim
Birlik açma ve eğitim
Araştırma + kaynak
Erken oyuncuya ağır kayıp
Araştırma
Ekonomi, hız, savunma ve savaş bonusları
Araştırma puanı/süre
Zorunlu tek doğru ağaç
Hastane/onarım
Savaş sonrası toparlanma
İnşaat ve özel kaynak
Kaybedenin oyundan kopması
Keşif/istihbarat
Harita görüşü ve görevler
Karargâh kilometre taşı
Bilginin yalnızca ödeme ile açılması
# 4. Kahramanlar, birlikler ve takım sinerjisi
# 4.1 Kahraman sisteminin rolü
Kahramanlar oyuncuya kişiselleştirme ve koleksiyon hedefi sağlar. İyi bir sistemde kahraman yalnızca daha büyük bir saldırı sayısı değildir: tank/ön hat, tek hedef hasarı, alan hasarı, destek, iyileştirme, kontrol ve keşif gibi farklı görevleri olabilir. Her kahramanın her modda en iyi olmaması gerekir.
# 4.2 Takım kurma ilkeleri
Ön hat hasarı üstlenir; arka hat hasar ve destek sağlar. Pozisyon, hedef seçimi ve beceri sırası önem taşımalıdır.
Fraksiyon/ekip bonusları takım kurulumunu ödüllendirebilir; fakat tek bir fraksiyonun her durumda tartışmasız en iyi olması engellenmelidir.
Roller birbirini tamamlamalı: tank + hasar + destek + karşı hamle gibi.
Yeni kahramanlar eskileri bütünüyle değersizleştirmemeli; niş kullanım, farklı modlarda avantaj ve yatay gelişim korunmalı.
Parça/dupe sistemi varsa uzun süreli koleksiyon hedefi oluşturur; ancak yeni oyuncunun rekabetçi seviyeye çıkmasını imkânsızlaştırmamalıdır.
# 4.3 Birlik ve kahraman farkı
Kahramanlar karar ve kimlik katmanıdır; birlikler ise ölçek, kayıp riski ve harita baskısı katmanıdır. Sadece kahraman gücüyle çözülen bir savaşta eğitim ve lojistik anlamını kaybeder; yalnızca asker sayısıyla çözülen savaşta ise takım kurma önemsizleşir. İki sistem farklı katkılar vermelidir.
Katman
Kontrol edilen değişken
Oyuncunun kararı
Kahraman
Rol, beceri, ekipman, seviye, sinerji
Hangi takım hangi rakibe karşı?
Birlik
Tür, kademe, sayı, eğitim, yaralı/kayıp
Ne kadar güç gönderilmeli ve risk alınmalı?
Teknoloji
Kalıcı ekonomi/savaş bonusu
Bugün güç mü, uzun vadeli üretim mi?
Komutan/ekipman
Sınırlı özel bonuslar
Hangi bonus hangi senaryoya uygun?
İstihbarat
Rakip kompozisyonu ve savunma bilgisi
Saldırı maliyeti ve karşı hamle riski nedir?
# 5. PvP ve dünya haritası tasarımı
# 5.1 PvP türlerini birbirinden ayır
PvP modu
Temel kurallar
Neden kullanılır?
Denge önerisi
Arena/asenkron
Oyuncu savunma takımı kurar; saldıran kayıtlı savunmaya karşı savaşır
Kısa oturum, sıralama
Benzer güç aralığı ve günlük deneme sınırı
Üs saldırısı
Harita hedefi, yürüyüş süresi, savunma ve kaynak riski
Gerçek risk ve hazırlık
Koruma penceresi, depo koruması, eşleştirme
İttifak rallisi
Bir lider hedefi başlatır, üyeler belirli süre içinde katılır
Koordinasyon ve topluluk
Katılım süresi, kapasite ve katkı kuralları açık olsun
Bölge/şehir kontrolü
İttifaklar belirli alanı ele geçirip tutmaya çalışır
Harita stratejisi
Saldıran/savunan için dönüşümlü avantaj
Sezon savaşı
Zaman sınırlı puan, bölge ve hedefler
Uzun vadeli ortak hedef
Sezon sonunda kısmi sıfırlama ve ödül kademeleri
Düello/özel maç
Belirli kurallı küçük ölçekli savaş
Takım deneme ve beceri
Ödüller güç farkını büyütmesin
# 5.2 Dünya haritası
Harita, oyuncuların üslerini ve kaynak noktalarını gösteren dekoratif bir ekran olmamalı; stratejik kararların merkezi olmalıdır. Kaynak bölgeleri, PvE tehditleri, ittifak yapıları, geçitler, korunan başlangıç alanları ve dönemsel hedefler oyuncuyu harita üzerinde hareket etmeye yönlendirir.
Harita hücreleri veya koordinatlar sunucu verisinde tutulur; istemci yalnızca görünümü çizer.
Yürüyüş emri, başlangıç/varış koordinatı, birlik kompozisyonu, hız ve varış zamanı içermelidir.
Yolculuk sırasında iptal, geri çağırma, hızlandırma ve saldırı hedefi değişikliği açık kurallara bağlanmalıdır.
Haritada tüm dünyayı her karede gerçek zamanlı simüle etmek yerine, olay ve zaman damgası temelli bir model kullanılabilir.
Yeni oyuncular için başlangıç koruması ve güçlü ittifakların yeni oyuncu kümelerini ezmesini önleyen coğrafi/kuralsal tamponlar gereklidir.
# 5.3 İttifak sosyal sistemleri
İttifak, sadece sohbet odası değil, ortak ilerleme çarpanı olmalıdır. Yardım butonları inşaat/araştırma süresini azaltabilir; ortak boss hasarı ve bölge kontrolü birlikte çalışma gerektirir; rütbeler sorumluluk ve yetki verir.
Rütbeler: lider, subay, etkinlik yöneticisi, üye ve yeni üye gibi açık yetki seviyeleri.
İttifak deposu/bağışları varsa kötüye kullanım önlemleri, kayıtlar ve günlük limitler.
Ralliye katılım için minimum birlik, süre ve kapasite kuralları.
İttifak ayrılma, lider devri, üyeyi atma ve savaş ilanı işlemlerinde onay ve kayıt.
Yeni oyuncuyu topluluğa katmak için görevli rehberlik, ortak PvE ve yardım ödülleri.
# 5.4 PvP adaleti ve kayıp tasarımı
Kaybeden oyuncunun tamamen sıfırlanması kısa vadede heyecan yaratsa da uzun vadede terk etmeyi teşvik edebilir. Kayıp maliyeti ile toparlanma süresi birlikte tasarlanmalıdır. Birliklerin bir kısmı yaralıya dönüşebilir; hastane kapasitesi aşan kayıplar kalıcı olabilir; korunan kaynak miktarı ve çevrimdışı kalkanlar oyuncuya risk yönetimi alanı sağlar.
Çok güçlü oyuncuların aynı küçük oyuncuyu tekrar tekrar hedeflemesini engelleyen saldırı yorgunluğu veya hedef koruması.
Yeni oyuncular için güç aralığı ve sunucu yaşı dikkate alınarak eşleştirme.
Çevrimdışı oyunculara sınırlı koruma; tam dokunulmazlık değil, makul toparlanma olanağı.
İttifak savaşı ödüllerinin yalnızca en yüksek güçteki birkaç üyeye değil, katkı türlerine göre dağılması.
Sonuç ekranında kayıp, yaralı, hasar, katkı ve ödüllerin anlaşılır şekilde gösterilmesi.
# 6. PvE, etkinlik takvimi ve tutundurma
# 6.1 PvE'nin işlevi
PvE, PvP'ye hazırlık alanı ve daha az rekabetçi oyuncular için kalıcı içerik olmalıdır. Boss, dalga savunması, keşif, kaynak kampı, zindan, hikâye görevi ve günlük av farklı becerileri ödüllendirebilir. PvE ödülleri kahraman geliştirme ve teknoloji ekonomisini besler.
# 6.2 Etkinlik takvimi
Sıklık
Örnek
Ödül yaklaşımı
Tasarım notu
Günlük
Görev listesi, ücretsiz deneme, küçük boss
İlerlemeye katkı
Kısa sürede tamamlanabilmeli
Haftalık
İttifak bossu, puan yarışı, ortak hedef
Kişisel + ittifak ödülü
Her gün saatlerce çevrimiçi olmayı zorunlu kılma
İki haftalık
Temalı PvE/PvP modu
Kozmetik ve geliştirme malzemesi
Meta değişimine alan aç
Sezonluk
Bölge kontrolü, sezon sıralaması
Prestij, kozmetik, sınırlı kaynak
Kazananın sonraki sezonu otomatik kazanmasını engelle
Canlı operasyon
Yeni görev/denge/ödül ayarı
Sunucu taraflı yapılandırma
İstemci güncellemesi gerektirmeyen ayarlar kullan
# 6.3 Günlük geri dönüşün etik tasarımı
Günlük ödül, üretim kuyruğu ve süreli etkinlikler oyuncuya düzenli neden verir. Ancak kaçırılan tek bir günün kalıcı geri kalmaya dönüşmesi veya alarm gibi bildirimlerle baskı kurulması uzun vadeli memnuniyeti düşürebilir. Biriken ödüller, haftalık tamamlama seçenekleri ve kaçırılan görevi sınırlı telafi etme mekanikleri değerlendirilebilir.
# 7. Gelir modeli ve ekonomi dengesi
Free-to-play modelinde gelir; kozmetik, sezon bileti, sınırlı hızlandırma, kaynak paketi, karakter/ekipman çekilişi ve konfor özelliklerinden gelebilir. Her birinin oyuncu güveni üzerindeki etkisi farklıdır. Rekabetçi PvP'de doğrudan güç satışı büyüdükçe kazanma becerisi ve ittifak koordinasyonu yerine harcama belirleyici olur.
Model
Artısı
Riski
Öneri
Kozmetik
Rekabet gücünü doğrudan etkilemez
Kozmetik üretim maliyeti
Görünüm, üs teması, profil çerçevesi
Sezon bileti
Öngörülebilir içerik/gelir
Görev baskısı
Ücretsiz yol anlamlı ödül versin
Hızlandırma
Zamanı değerli oyuncuya kolaylık
Zaman duvarı ve pay-to-win
Sınır, günlük kazanılabilir hızlandırma
Kaynak paketi
Basit ve anlaşılır
Ekonomi enflasyonu
Paket miktarı oyunun kaynak akışına bağlı
Gacha/çekiliş
Koleksiyon ve sürpriz
Şeffaflık ve harcama riski
Olasılıkları açıkla, koruma/pity ve üst sınırları değerlendir
Abonelik/VIP
Sürekli gelir
Kalıcı güç farkı
Konfor ve kozmetik ağırlıklı tut
Ölçülmesi gerekenler: D1/D7/D30 geri dönüş oranı, oturum sayısı ve süresi, ilk satın almaya dönüşüm, oyuncu başına gelir, etkinlik katılımı, PvP güç farkı, terk noktaları, şikâyet/iadeler ve oyuncu destek yükü. Bu ölçümler oyuncuyu daha fazla harcamaya zorlamak için değil, oyunun nerede sıkıcı veya adaletsiz olduğunu anlamak için kullanılmalıdır.
# 8. Teknik motor mimarisi: Unity istemcisi ve sunucu
# 8.1 Unity neyi yapar, sunucu neyi yapar?
Unity istemcisi grafik, animasyon, kamera, UI, ses, yerel efektler, harita görünümü ve savaş sonucunun görsel sunumundan sorumlu olmalıdır. Kaynak bakiyesi, birlik sahipliği, inşaat tamamlanma zamanı, savaş sonucu, ödül, sıralama ve satın alma gibi kritik veriler sunucu tarafından doğrulanmalıdır. İstemcide görünen güç sayısı tek başına güvenilir otorite değildir.
Unity'nin Netcode belgeleri, Netcode for GameObjects'i daha küçük/orta karmaşıklıktaki GameObject tabanlı gerçek zamanlı oyunlar için; Netcode for Entities'i ise DOTS tabanlı yüksek performans ve çok sayıda varlık ihtiyacı olan senaryolar için konumlandırır. Ancak asenkron dünya haritası ve süreli yürüyüşlere dayanan 4X oyununda her hareketi sürekli gerçek zamanlı ağ nesnesi olarak göndermek şart değildir; çoğu eylem API/olay tabanlı backend ile çözülebilir.
# 8.2 Önerilen yüksek seviye yapı
Bileşen
Sorumluluk
Önerilen yaklaşım
Unity Client
UI, sahne, animasyon, yerel sunum
Unity 6 LTS hattı değerlendir; Android/iOS hedeflerini erken test et
API Gateway / Backend API
Kimlik, komut kabulü, sorgular
HTTPS API; versiyonlama ve rate limit
Game Domain Services
Ekonomi, binalar, birlikler, kahramanlar, ittifak
Alan bazlı modüller; başlangıçta modüler monolit olabilir
Authoritative Combat
Savaş hesaplama ve doğrulama
Sunucuda deterministik/tekrarlanabilir hesaplama
PostgreSQL
Kalıcı oyun verisi
İşlemsel tutarlılık, indeksler, yedekleme
Redis / Cache
Geçici veri, sayaçlar, kuyruk kilitleri
Kaynak gerçekliği değil; cache bozulsa da DB'den toparlanabilsin
Job Queue / Scheduler
Bina süreleri, yürüyüş varışı, sezon olayları
Zaman damgası + dayanıklı kuyruk/tekrar çalıştırma
Realtime / Push
Sohbet, bildirim, olay güncellemesi
WebSocket/SignalR benzeri kanal ve push bildirimleri
Admin/LiveOps
Etkinlik ve denge yapılandırması
Yetki kontrolü, iki aşamalı onay, değişiklik günlüğü
Analytics/Observability
Performans, hata, denge telemetrisi
Log, metrik, trace, alarm ve olay korelasyonu
# 8.3 İki tür çok oyunculu yapı
Asenkron strateji: oyuncu bir komut yollar, sunucu doğrular ve kalıcı durumu değiştirir; rakip daha sonra sonucu görür. Örneğin bina yükseltme, kaynak toplama, harita yürüyüşü, kayıtlı savunma takımına arena saldırısı. Bu yaklaşım 4X oyunlarının büyük kısmına uygundur.
Gerçek zamanlı oturum: oyuncular aynı anda etkileşir ve düşük gecikmeli durum senkronizasyonu gerekir. Örneğin doğrudan kontrol edilen küçük arena veya eş zamanlı kooperatif görev. Bu modlarda ağ mimarisi, tick rate, lag telafisi ve oturum sunucusu ihtiyacı artar.
# 8.4 Oyun durumu ve zamanlama
İnşaatın bitmesi için sunucunun her saniye her oyuncuyu güncellemesi gerekmez. Kayıtta started_at, duration_seconds ve finish_at tutulabilir. Oyuncu ödülü talep ettiğinde veya zamanlayıcı işi tetiklendiğinde sunucu bitiş zamanını doğrular. Böylece binlerce zamanlayıcı için sürekli polling ihtiyacı azaltılır.
Tüm saat hesapları sunucu UTC zamanı üzerinden yapılmalı; cihaz saati yalnızca görsel geri sayım için kullanılmalıdır. Oyuncunun saatini ileri alması kaynak, görev veya etkinlik ödülünü değiştirmemelidir.
# 9. Savaş motoru: adım adım önerilen model
# 9.1 Savaşın veri akışı
1. Saldıran, birlikleri ve kahraman dizilimini seçip saldırı emri gönderir.
2. Sunucu oyuncu yetkisini, hedefin geçerliliğini, yürüyüş/enerji/koruma kurallarını ve mevcut birlikleri doğrular.
3. Savaş için kullanılan bütün istatistiklerin sürümlenmiş bir snapshot'ı oluşturulur.
4. Sunucu saldırı ve savunma kompozisyonlarını, pasifleri, karşılaşma kurallarını ve rastgelelik tohumunu sabitler.
5. Combat resolver raund/aksiyon sırasını, hedeflemeyi, hasarı, iyileştirmeyi, kontrol etkilerini ve ölüm durumlarını hesaplar.
6. Sonuç tek bir veritabanı işlemi veya güvenli, idempotent işlem akışıyla uygulanır: birlik kaybı, yaralı, kaynak yağması, puan ve rapor.
7. Savaş raporu ve replay için olay kaydı saklanır; istemci bu kaydı animasyonla oynatır.
8. Aynı saldırı komutunun tekrar gönderilmesi ikinci kez ödül veya hasar oluşturmamalıdır.
# 9.2 Örnek hesaplama çerçevesi
Aşağıdaki formüller herhangi bir referans oyunun gerçek formülü değildir; prototipte başlanabilecek örnek bir tasarımdır. Sayılar testlerle dengelenmelidir.
Değişken
Örnek formül / anlam
Temel saldırı
ATK = kahraman tabanı + seviye katkısı + ekipman + teknoloji
Savunma azaltımı
Mitigation = DEF / (DEF + K) ; K sabit denge parametresi
Ham hasar
RawDamage = SkillMultiplier × ATK × (1 + Bonuslar)
Son hasar
Damage = RawDamage × (1 − Mitigation) × CounterModifier × RandomFactor
Kritik
Crit varsa CriticalMultiplier uygulanır; oran üst sınırı olmalı
Kalkan/iyileştirme
Önce kalkan tüketilir; kalan hasar HP'ye yazılır; iyileştirme üst sınıra takılır
Birlik kaybı
Kayıp, kalan HP/hasar ve birlik türü kurallarına göre hesaplanır
Güç skoru
Birden çok kaynağın ağırlıklı toplamı; savaş kazanma olasılığıyla aynı şey değildir
RandomFactor için örneğin 0.95–1.05 gibi dar bir aralık kullanılabilir; fakat PvP sonucunu rastgeleliğe fazla bağımlı yapmak oyuncuda adaletsizlik hissi yaratır. Aynı savaş raporunun tekrar üretilebilmesi için seed ve kullanılan kural/denge sürümü kaydedilmelidir.
# 9.3 Karşı sistem ve meta
Dengeyi yalnızca +%5 saldırı bonuslarıyla kurmak yerine anlamlı karşı sistemler oluşturun: ağır ön hat hızlı tek hedef saldırılarına dayanabilir; kontrol becerisi yüksek hasar takımını geciktirebilir; destek uzun savaşta avantaj sağlayabilir; alan hasarı kalabalık birliklere karşı güçlü olabilir. Her karşı hamlenin bir bedeli veya zayıf tarafı olmalıdır.
# 9.4 Savaş raporu
Tarafların kahramanları, birlikleri, savaş öncesi güç/istatistik snapshot'ı
Her raundda aksiyon, hedef, hasar, iyileştirme, kalkan ve durum etkileri
Başlangıç ve bitiş birlik sayısı; yaralı ve kalıcı kayıp
Ödül, yağma, puan ve kullanılan savaş kuralı sürümü
Rapor bütünlüğü için savaş kimliği, seed ve sonuç özeti
İstemci yalnızca raporu görselleştirir; ödül ve kaybı kendisi hesaplamaz
# 10. Veri modeli ve API taslağı
İlk sürümde servisleri çok sayıda mikroservise bölmek yerine modüler bir backend ile başlamak genellikle daha hızlı ve operasyonel olarak daha ucuzdur. Trafik ve ekip olgunlaştıkça savaş, sohbet, bildirim veya analitik gibi parçalar ayrılabilir.
Tablo/varlık
Önemli alanlar
Amaç
players
id, account_id, name, created_at, server_id
Oyuncu kimliği
player_state
player_id, level, power_snapshot, last_seen_at
Temel ilerleme
resources
player_id, resource_type, amount, updated_at
Kaynak bakiyesi
buildings
player_id, building_type, level, upgrade_finish_at
Bina seviyesi ve zamanlayıcı
heroes
hero_id, owner_id, definition_id, level, rank, equipment
Oyuncu kahraman koleksiyonu
troops
player_id, troop_type, tier, available, wounded, dead
Birlik envanteri
marches
id, owner_id, target_id, depart_at, arrive_at, status
Harita yürüyüşü
battles
id, attacker_id, defender_id, seed, rules_version, result
Savaşın değişmez kimliği
battle_events
battle_id, seq, event_type, payload
Savaş raporu/replay
alliances
id, name, leader_id, power, region_id
İttifak
alliance_members
alliance_id, player_id, role, joined_at
Üyelik ve yetki
territories
id, owner_alliance_id, x, y, protection_until
Harita kontrolü
event_progress
player_id, event_id, points, claimed_rewards
Etkinlik durumu
transactions
id, player_id, type, amount, idempotency_key
Ekonomi hareket günlüğü
audit_log
actor_id, action, target, timestamp, metadata
Admin ve kritik eylem kaydı
# 10.1 Örnek API uçları
Metot
Uç
İşlev
GET
/v1/player/me
Oyuncu özeti ve sürümlü durum
POST
/v1/buildings/{id}/upgrade
Yükseltme başlat; maliyet/süre sunucuda hesaplanır
POST
/v1/buildings/{id}/claim
Tamamlanmış yükseltmeyi doğrula
POST
/v1/hero/{id}/upgrade
Kahraman geliştirme
POST
/v1/marches
Harita yürüyüşü başlat
POST
/v1/battles/{id}/resolve
Yalnızca yetkili iç servis; doğrudan herkese açık olmamalı
GET
/v1/battle-reports/{id}
Yetkili savaş raporunu getir
POST
/v1/alliances/{id}/join
İttifak katılım kuralları
POST
/v1/events/{id}/claim
Ödülü tek seferlik/idempotent talep et
# 10.2 Ekonomi güvenliği
Her kaynak değişikliği bir işlem günlüğü oluşturmalı; destek ekibi anomaliyi takip edebilmeli.
İstemciden gelen maliyet, ödül miktarı, hasar veya premium para değeri güvenilir kabul edilmemeli.
İdempotency key, tekrar gelen isteklerde iki kez kaynak düşülmesini veya ödül verilmesini engeller.
Yükseltme ve kaynak harcaması aynı mantıksal işlemde doğrulanmalı; yarım kalmış işlem veri tutarsızlığı yaratmamalı.
Rate limit, cihaz/hesap sinyalleri, anormal hız ve imkânsız hareket kontrolleri katmanlı uygulanmalı.
Yönetici panelinde oyuncu bakiyesi değiştirmek için sebep, yetki ve denetim kaydı zorunlu olmalı.
# 11. UI/UX: oyunun hazır iskeleti
Hedeflenen ürün için ana ekranda ilk günden itibaren sabit bir navigasyon omurgası kurulmalıdır. Görsel kabuk, sistemlerin kendisinden bağımsız tasarlanmalı; yeni mod eklenince tüm ekranlar baştan yazılmamalıdır.
Ekran
Üst alan
Ana içerik
Alt menü / aksiyon
Ana üs
Oyuncu seviyesi, kaynaklar, posta
Binalar, karakterler, üretim göstergeleri
Üs / Kahraman / Harita / İttifak / Etkinlik
Kahraman
Geri, filtre, kaynak
Kahraman kartları, seviye, beceri, ekipman
Geliştir / Dizilime ekle
Takım kurma
Güç özeti ve rol filtreleri
Ön/arka sıra, takım bonusu, simüle et
Kaydet / karşılaştır
Dünya haritası
Koordinat, arama, filtre
Bölgeler, hedefler, ittifaklar, yürüyüşler
Keşfet / saldır / toplan
Savaş raporu
Sonuç, rakip, tarih
Raundlar, hasar, kayıp, ödül
Tekrar izle / paylaş
İttifak
Üye sayısı, güç, duyuru
Üyeler, yardım, hedefler, ralli
Sohbet / bağış / katıl
Etkinlik
Süre ve kişisel ilerleme
Görevler, ödül kademeleri, sıralama
Görev yap / ödül al
Mağaza
Para türü ve paket kategorisi
Teklif, kozmetik, sezon bileti
Satın al / olasılık bilgisi
# 11.1 UI teknik yaklaşımı
Tek bir global HUD ve alt navigasyon; her ekran kendi içerik modülüne sahip olsun.
UI durumları Loading, Empty, Error, Offline, Success gibi açık durumlarla tasarlansın.
Kaynak çubuğu animasyonu yalnızca görsel geri bildirimdir; asıl değer sunucu cevabından gelir.
Uzun listelerde sanallaştırma, ikon atlası ve uygun asset yükleme kullanılsın.
Adreslenebilir içerik (Addressables benzeri yaklaşım) karakter, etkinlik ve kozmetik paketlerini gerektiğinde yüklemek için değerlendirilsin.
Dokunma hedefleri, küçük telefonlar, ekran oranları, çentik ve safe area Android/iOS cihazlarında test edilsin.
# 12. İçerik, yapılandırma ve canlı operasyonlar
Denge değerleri kod içine gömülürse her küçük ayar yeni istemci sürümü gerektirebilir. Bina maliyetleri, kahraman istatistikleri, beceri katsayıları, etkinlik tarihleri, ödüller ve mağaza vitrinleri sürümlenmiş veri tabloları/uzak yapılandırma ile yönetilmelidir. Kritik kurallar yine sunucuda doğrulanmalıdır.
İçerik tanımı
Örnek alanlar
Kontrol
BuildingDefinition
id, max_level, cost_curve, duration_curve, prerequisites
Şema doğrulama ve sürüm
HeroDefinition
id, rarity, role, base_stats, skill_ids, faction
Benzersiz ID ve referans kontrolü
SkillDefinition
id, trigger, target_rule, coefficient, cooldown
Simülasyon testleri
EventDefinition
start/end UTC, phases, scoring, rewards
Zaman ve ödül çakışma kontrolü
RewardTable
item_id, quantity, probability, guarantee_rule
Olasılık doğrulama ve şeffaflık
BalanceVersion
version, effective_at, changelog
Eski savaş raporları için sürüm kaydı
Canlı operasyon aracı; etkinlik başlatma/durdurma, ödül tablosu yayımlama, bakım mesajı, denge sürümü, oyuncu desteği, hile incelemesi ve olay günlüğünü kapsamalıdır. Üretim verisi değişiklikleri önce test ortamında doğrulanmalı ve geri alma planı bulunmalıdır.
# 13. Örnek ürün tasarımı: özgün bir MVP
Aşağıdaki kapsam, Last Asylum benzeri türden ilham alan ancak kendine ait tema ve değerlerle geliştirilebilecek bir ilk sürüm önerisidir.
# 13.1 MVP'de mutlaka olsun
Android/iOS Unity istemcisi; giriş, hesap bağlama ve cihazlar arası ilerleme
Ana üs sahnesi: kaynak çubuğu, sabit alt menü, 6–8 bina türü
Üretim ve kaynak toplama; bina yükseltme; araştırma ağacı
12–16 özgün kahraman, 3–4 rol, 3 fraksiyon veya sinerji ailesi
3–5 kahramanlı takım kurma; kayıtlı savunma takımı
PvE görevleri ve en az bir boss/ dalga savunma modu
Asenkron arena PvP; sunucu tarafında savaş sonucu
Harita üzerinde kaynak noktası ve süreli yürüyüşün prototipi
İttifak oluşturma/katılma, üye listesi, yardım ve basit ortak boss
Savaş raporu, günlük görevler, temel bildirimler
Admin/LiveOps yapılandırması, analitik ve hata izleme
# 13.2 İlk sürümden sonraya bırakılabilecekler
Binlerce oyuncunun aynı bölgede sürekli gerçek zamanlı hareket ettiği pahalı harita simülasyonu
Çok sayıda sunucu bölgesi ve karmaşık sezonlar arası göç
Sesli sohbet, oyuncu pazarı ve serbest ticaret
Çok büyük içerik üretim hattı ve onlarca fraksiyon
Tam kapsamlı canlı PvP aksiyonu; önce asenkron ve rapor tabanlı savaş doğrulanmalı
# 13.3 Önerilen geliştirme aşamaları
Aşama
Teslimat
Tamamlandı sayılma koşulu
0. Tasarım doğrulama
Oyun döngüsü, ekonomi, ilk 30 dakikalık akış, kağıt savaş simülasyonu
Oyuncu hedefi ve kaynak darboğazı anlaşılır
1. Görsel iskelet
Ana üs, üst kaynak barı, alt menü, ekran yönlendirme
Her ana ekran çalışır; sahne geçişleri ve safe area doğru
2. Tek oyunculu prototip
Bina, kaynak, kahraman, PvE
Yeni hesap başlangıçtan ilk kilometre taşına ulaşır
3. Backend ve kalıcılık
Kimlik, PostgreSQL, ekonomi, sunucu zamanı
Uygulama yeniden kurulduğunda ilerleme korunur
4. Savaş ve PvP
Savaş motoru, arena, rapor
Aynı giriş aynı sürümde tekrarlanabilir; istemci sonucu değiştiremez
5. Sosyal/ittifak
İttifak, yardım, boss, bildirim
Bir grup ortak hedefi tamamlayabilir
6. Kapalı test
Cihaz/performans, ekonomi, hile, UX
Kritik hatalar ve ilerleme kilitleri çözülmüş
7. Soft launch
Sınırlı oyuncu, ölçüm ve denge
Tutundurma, çökme ve ekonomi metrikleri kabul edilebilir
8. Global canlı operasyon
Sezon, içerik takvimi, destek ve ölçekleme
Geri alma, olay müdahalesi ve yayın süreci denenmiş
# 13.4 Ekip ve maliyet sürücülerini anlamak
Maliyet yalnızca Unity geliştiricisinin süresi değildir. 3D/2D sanat, animasyon, UI, oyun tasarımı, backend, QA, analitik, içerik ve canlı operasyonlar toplam kapsamı belirler. İlk prototipte tek oyunculu simülasyon ve asenkron PvP'ye odaklanmak, eş zamanlı devasa savaşlar ve geniş içerik üretiminden daha kontrollü bir yol sunar.
Önerilen küçük ekip rolleri: oyun tasarımcısı/ürün sahibi; Unity istemci geliştiricisi; backend geliştiricisi; 2D/3D sanatçı; UI/UX; QA/otomasyon. Roller başlangıçta birleştirilebilir ancak savaş ve ekonomi kurallarının tek bir sürümlü tasarım dokümanında tutulması önemlidir.
# 14. Test planı ve kalite kapıları
Alan
Testler
Yayın kapısı
Ekonomi
Negatif bakiye, iki kez ödül, eşzamanlı harcama, kaynak taşması
Her işlem atomik/idempotent
Savaş
Aynı seed, sınır değerler, ölü hedef, sıra etkisi, tekrar oynatma
Sonuç sunucuda ve tekrarlanabilir
PvP
Çoklu saldırı, hedef koruması, çevrimdışı durum, yarış koşulu
İstismar edilebilir açık yok
Zamanlayıcı
Cihaz saati değişimi, saat dilimi, uygulama kapalıyken bitiş
Sunucu zamanı tek otorite
Ağ
Kesilen bağlantı, timeout, tekrar istek, zayıf mobil ağ
Oyuncu iki kez ücret ödemez
Mobil
Düşük bellek, eski cihaz, farklı çözünürlük, arka plan/ön plan
Kritik çökme ve UI taşması yok
Güvenlik
Yetkisiz API, token yenileme, rate limit, admin yetkisi
Kritik veri istemciden yazılamaz
Operasyon
Yedek geri yükleme, denge rollback, bakım mesajı
Felaket kurtarma adımları denenmiş
# 15. Büyük riskler ve karşı önlemler
Risk
Sonuç
Karşı önlem
Pay-to-win aşırılığı
PvP terk oranı ve kötü itibar
Kozmetik/konfor geliri, güç farkı sınırları, sezon reseti
Kartopu etkisi
En güçlü ittifakın sürekli kazanması
Sezon puanı, hedef çeşitliliği, geri dönüş bonusu ve coğrafi denge
Aşırı grind
Günlük iş gibi hissettirme
Kısa oturum, biriken ödül, haftalık telafi
İstemci hilesi
Kaynak ve savaş manipülasyonu
Sunucu otoritesi, işlem günlüğü, anomali tespiti
İçerik açlığı
Oyuncuların ilerleme bitince ayrılması
Modüler etkinlikler ve ölçümlere göre içerik takvimi
Mikroservis erkenliği
Operasyonel karmaşa ve maliyet
Modüler monolit, ölçüme dayalı ayrıştırma
Ağır grafik yükü
Düşük cihazlarda performans sorunu
Asset bütçesi, LOD, Addressables, gerçek cihaz testi
Ödül/etkinlik hatası
Ekonomi enflasyonu ve oyuncu şikâyeti
Test ortamı, dry-run, sürüm ve geri alma
# 16. Sonuç ve net öneri
En uygulanabilir başlangıç; Unity tabanlı, mobil öncelikli, modüler bir istemci; sunucu otoriteli ekonomi ve savaş; PostgreSQL tabanlı kalıcı durum; süreli olaylar için dayanıklı scheduler; ilk etapta asenkron PvP ve sınırlı dünya haritasıdır. Önce oynanabilir bir dikey dilim kurulmalı, ardından ittifak ve sezon sistemleri eklenmelidir.
Oyunun çekirdeği şu sırayla doğrulanmalıdır: (1) Oyuncu ilk 10 dakikada ne yaptığını anlıyor mu? (2) İlk günün sonunda bir sonraki hedefi var mı? (3) Kahraman ve birlik kararları sonucu gerçekten değiştiriyor mu? (4) PvP kaybı toparlanabilir mi? (5) İttifak oyuncuya tek başına elde edemeyeceği bir değer sağlıyor mu? (6) Sunucu her kritik sonucu doğruluyor mu?
En önemli öneri: Önce “bina ekranları olan büyük bir oyun” değil, 15–20 dakikada oynanabilen ve kaynak→gelişim→takım→savaş→ödül döngüsünü baştan sona çalışan küçük bir ürün oluştur. Bu döngü eğlenceli ve adil değilse daha fazla bina, kahraman veya animasyon sorunu çözmez.
# 17. Kaynaklar ve doğrulama notları
Aşağıdaki kaynaklar araştırmada kullanılan erişilebilir referanslardır. Mağaza sayfaları oyunların kendi özel kodunu değil, yayıncı tarafından açıklanan özellikleri doğrular. Teknik belgeler Unity ağ ve backend yaklaşımını açıklar; bunlar belirli bir ticari oyunun aynı mimariyi kullandığını kanıtlamaz.
Last Asylum: Plague — resmî site — https://last-asylum.com/ (tema, sığınak, üretim, kahraman ve harabe kontrolü)
Last Asylum: Plague — Google Play — https://play.google.com/store/apps/details?id=com.phs.global (mağaza açıklaması ve oyun döngüsü)
Last Asylum — bağımsız rehber ve sistem veritabanı — https://wiki-last-asylum.com/en/guides (oyun içi sistemlere dair topluluk rehberi; resmî kaynak değildir)
Last Fortress: Underground — Google Play — https://play.google.com/store/apps/details?id=com.more.lastfortress.gp (sığınak, uzman kurtulanlar, takım sinerjisi ve ittifak)
Dark War Survival — Google Play — https://play.google.com/store/apps/details?id=com.readygo.dark.gp (üs, kahraman, teknoloji ve ittifak açıklaması)
Unity — Netcode packages — https://docs.unity.com/multiplayer/netcode/netcode (Netcode for GameObjects ve Netcode for Entities ayrımı)
Unity — Network topologies — https://mp-docs.dl.it.unity3d.com/netcode/2.2.0/terms-concepts/network-topologies/ (client-server ve distributed authority)
Unity — Authority — https://docs.unity.cn/Packages/com.unity.netcode.gameobjects%402.13/manual/terms-concepts/authority.html (sunucu otoritesi, veri bütünlüğü ve hile direnci)
Unity — Cloud Code server authority — https://docs.unity.com/en-us/cloud-code/server-authority (kritik oyun mantığının sunucuda yürütülmesi)
Unity — Cloud Save + Cloud Code — https://docs.unity.com/en-us/cloud-save/tutorials/cloud-code (sunucu tarafında oyuncu verisi ve güvenli yazma)
Unity — Game state management — https://docs.unity.com/en-us/cloud-code/3.0.0/game-state-management (sunucu kaynaklı oyun durumu ve doğrulama)
Unity — Addressables planning — https://unity.com/blog/engine-platform/addressables-planning-and-best-practices (varlık paketleme ve dinamik içerik yükleme)
Unity — Live game feature blueprints — https://github.com/Unity-Technologies/skills/blob/main/skills/build-live-game/SKILL.md (canlı oyun özelliklerinde config/state/server write desenleri)
# Araştırma sınırları
Bu rapor, oyunların kapalı sunucu kodlarını tersine mühendislikle inceleyen bir rapor değildir. Gerçek oyun içi savaş katsayıları, gizli eşleştirme ağırlıkları, sunucu kapasitesi, gerçek gelir dağılımı ve iç araç zincirleri kamuya açık kaynaklardan güvenilir biçimde belirlenemediğinde burada kesin bilgi olarak verilmemiştir. Whiteout Survival gibi ek referansların ayrıntılı güncel değerleri, ilgili sürümün oyun içinden ayrıca gözlemlenmelidir.