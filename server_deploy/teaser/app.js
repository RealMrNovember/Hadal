/* HADAL landing page: language toggle (EN/TR) and the decorative depth gauge.
   English lives in index.html; Turkish lives here. No trackers, no network calls. */
(function () {
  'use strict';

  var TR = {
    'skip': 'İçeriğe geç',
    'nav.label': 'Ana menü',
    'nav.world': 'Dünya',
    'nav.gameplay': 'Oynanış',
    'nav.status': 'Durum',
    'nav.install': 'Kurulum',
    'nav.faq': 'SSS',
    'lang.label': 'Dil',

    'hero.eyebrow': 'Erken erişim · Android test sürümü',
    'hero.t1': 'Uçurumun dibinde,',
    'hero.t2': 'insanlık yeniden başlıyor.',
    'hero.lead': 'iOS ve Android için derin deniz temalı bir üs kurma ve strateji MMO’su. İnsanlığın son kubbelerini 11.000 metre derinlikte kur, oksijeni tükenmeye bırakma ve denizaltılarını bizden çok daha eski bir karanlığa gönder.',
    'hero.cta': 'Android test sürümünü indir (APK)',
    'hero.howto': 'Nasıl kurulur?',
    'hero.note': 'Android 8.0+ · Ücretsiz · İnternet bağlantısı gerekir',
    'badge.apple': 'App Store — yakında',
    'badge.google': 'Google Play — yakında',
    'badge.small': 'Yakında',
    'badge.small2': 'Yakında',
    'hero.phoneAlt': 'Android telefonda HADAL açılış ekranı: HADAL logosu, slogan ve “Başlamak için dokun” yazısı',
    'hero.phoneCap': 'Açılış ekranı, güncel test sürümü',

    'world.kicker': 'Dünya',
    'world.title': 'Yıl 2249. Yeryüzü öldü.',
    'world.p1': 'Atmosfer zehirlendi. Güneş ışığı öldürücü hâle geldi. Kıtalarda yaşam kalmadı. İnsanlıktan geriye kalanlar okyanusun en derin katmanı olan Hadal Zone’a çekildi ve dalgaların on bir kilometre altında, basınca dayanıklı kubbelere kapandı.',
    'world.p2': 'Oraya ilk varan onlar değildi. Okyanusun dibinde, bizden milyonlarca yıl önce yaşamış biyomekanik bir uygarlığın kalıntıları yatıyor: tapınaklar, hâlâ nefes alıyormuş gibi çalışan makineler ve yalnızca The Forgotten diye anılan askerler. Onları insanlık uyandırdı.',
    'world.p3': 'Haritanın merkezinde bir yerde The Core var. Onu aramaya gidenlerin hiçbiri net bir cevapla dönmedi.',
    'world.f1k': 'Derinlik',
    'world.f1v': '11.000 m',
    'world.f2k': 'Işık',
    'world.f2v': 'Neredeyse hiç',
    'world.f3k': 'Basınç',
    'world.f3v': 'Her an öldürücü',
    'world.f4k': 'Komşular',
    'world.f4v': 'Bizden eski',
    'world.quote': '“Dünyanın son insanlarından biriyim. Karanlıkta hayatta kalmaya çalışıyorum. Ama o karanlıkta, benden çok daha eski bir şey beni izliyor.”',
    'world.creaturesLabel': 'Yaratık görselleri',
    'world.c1': 'Derin deniz fener balığı yaratığının görseli',
    'world.c2': 'Mor renkli siren denizanasının görseli',
    'world.c3': 'Dikenli, biyomekanik bir deniz canavarı olan Iron Leviathan’ın görseli',
    'world.cap': 'Geliştirme aşamasındaki yaratık modelleri.',

    'gp.kicker': 'Oynanış',
    'gp.title': 'Kur. Nefes al. Derine in.',
    'gp.lead': 'Üssün hem yuvan hem kimliğin: tek bir reaktörün çevresine dizilmiş kubbeler, hayatta kaldıkça dışa doğru büyür. Her hamleni oyun sunucusu doğrular; böylece dünya herkes için adil kalır ve sen yokken de üssün çalışmaya devam eder.',
    'gp.lineupAlt': 'Altıgen platformlar üzerinde camgöbeği ve kehribar ışıklarla parlayan HADAL üs binaları: reaktör, kubbeler, laboratuvarlar ve kuleler',
    'gp.lineupCap': 'Güncel sanat çalışmasından üs binaları.',
    'tag.live': 'Şu an oynanabilir',
    'tag.partial': 'Kısmen oynanabilir',
    'tag.dev': 'Geliştiriliyor',
    'tag.planned': 'Planlandı',
    'gp.p1t': 'Üs kurma ve yükseltme',
    'gp.p1': 'Merkezde Çekirdek Reaktör durur, diğer her şey onun etrafındaki halkalara kurulur. Boş bir yuvaya dokun ve inşa et, bir binaya dokun ve yükselt. Hiçbir bina reaktörün seviyesini geçemez; yani her ilerleme üssün kalbinden başlar.',
    'gp.p1a': 'İnşaat sunucu saatiyle ilerler; sen çevrimdışıyken bile',
    'gp.p1b': 'Kısa beklemeler bizden: son beş dakika ücretsiz biter',
    'gp.p1c': 'Yükseltme sürerken bina eski seviyesinde üretmeye devam eder',
    'gp.p2t': 'Hayatta kalma ekonomisi',
    'gp.p2': 'Bir koloniyi altı kaynak ayakta tutar. Oksijen her zaman önce gelir: biterse üs ölür. Enerji reaktörden, biyokütle canlı organizmalardan, titanyum dronlarından gelir; hadalit ise yalnızca en derin bölgelerde bulunur.',
    'gp.resLabel': 'Kaynaklar',
    'res.o2': 'Oksijen',
    'res.energy': 'Enerji',
    'res.food': 'Yiyecek',
    'res.bio': 'Biyokütle',
    'res.ti': 'Titanyum',
    'res.hadalite': 'Hadalit',
    'gp.p3t': 'Denizaltı keşifleri',
    'gp.p3': 'Denizaltılarını Denizaltı İskelesi’nde inşa et, üç kahramanla donat ve karanlığa gönder: biyolüminesan ormanlar, batık şehirler, eski insan laboratuvarları, yabancı tapınaklar. Bazı bölgelerde ışıklarını açamazsın. Elinde yalnızca sonar kalır.',
    'gp.sub1': 'Turuncu keşif denizaltısının görseli',
    'gp.sub2': 'Sarı yük denizaltısının görseli',
    'gp.p4t': 'İttifaklar ve PvP',
    'gp.p4': 'Uçurumda kimse tek başına hayatta kalamaz. İttifaklar ortak yapılar kuracak ve birlikte sualtı şehirleri inşa edecek; rakip kubbeler birbirini kuşatacak. Ve bütün yollar, dünyanın merkezindeki The Core için verilecek savaşa çıkıyor.',
    'gp.bTitle': 'Halkalarda neler var',
    'b.reactor': 'Çekirdek Reaktör',
    'b.oxygen': 'Oksijen Tesisi',
    'b.food': 'Gıda Laboratuvarı',
    'b.bio': 'Biyo Laboratuvar',
    'b.drone': 'Dron Fabrikası',
    'b.research': 'Araştırma Merkezi',
    'b.dock': 'Denizaltı İskelesi',
    'b.shield': 'Kalkan Jeneratörü',
    'b.mega': 'Mega Kubbe',

    'st.kicker': 'Geliştirme durumu',
    'st.title': 'Erken erişim, açık açık.',
    'st.lead': 'HADAL aktif olarak geliştiriliyor. Android test sürümü oyunun erken bir parçası: üs çalışıyor, derinlik hâlâ inşa ediliyor. Durum şu:',
    'st.nowTag': 'Yayında',
    'st.nowTitle': 'Bugün test sürümünde',
    'st.n1': 'Anında misafir hesabı, kayıt gerekmez',
    'st.n2': 'Kalbinde Çekirdek Reaktör olan dairesel üs',
    'st.n3': 'Gerçek inşaat süreleriyle bina kurma ve yükseltme',
    'st.n4': 'Sunucu tarafında kaynak üretimi; çevrimdışıyken de sürer',
    'st.n5': 'Telefon değiştirmek için aktarım kodlu hesap koruma',
    'st.n6': 'Hediye kodları ve oyun içi duyurular',
    'st.nextTag': 'Sırada',
    'st.nextTitle': 'Şu an üzerinde çalışılanlar',
    'st.x1': 'Üs kurmanın tamamı: halka açma, mega kubbeler, bina kaldırma',
    'st.x2': 'Hayatta kalma döngüsü: oksijen tüketimi, alarmlar ve acil durumlar',
    'st.x3': 'Sunum: yeni arayüz, sualtı görselleri ve ses',
    'st.laterTag': 'Sonra',
    'st.laterTitle': 'Yol haritasında',
    'st.l1': 'Denizaltılar, basınç ve sonar keşifleri',
    'st.l2': 'Kahramanlar, düşmanlar ve savaş',
    'st.l3': 'Ortak dünya haritası, ittifaklar, PvP ve kuşatmalar',
    'st.l4': 'Leviathan’lar, The Core ve App Store / Google Play çıkışı',
    'st.shot1': 'Test sürümü ekran görüntüsü: seviye 1 Çekirdek Reaktör paneli, enerji üretimi ve sonraki seviyenin maliyeti',
    'st.shot1cap': 'Çekirdek Reaktör paneli',
    'st.shot2': 'Test sürümü ekran görüntüsü: geri sayımı ve Ücretsiz Bitir düğmesiyle inşa hâlindeki bir Oksijen Tesisi',
    'st.shot2cap': 'Ücretsiz bitirmeli inşaat',
    'st.cap': 'Ekran görüntüleri güncel geliştirme sürümünden, düzenlenmeden alındı. Görseller ve arayüz geçicidir, değişecek.',

    'in.kicker': 'Kurulum',
    'in.title': 'Android test sürümünü kurmak',
    'in.lead': 'Test sürümü henüz Google Play’de değil, bu yüzden Android onu dosya olarak kurar. Yaklaşık bir dakika sürer.',
    'in.s1t': 'Telefonunu kontrol et',
    'in.s1': 'Android 8.0 veya üstü ve internet bağlantısı gerekir. iPhone şimdilik desteklenmiyor.',
    'in.s2t': 'APK’yı indir',
    'in.s2': 'Bu sayfayı telefonunda aç ve indirme düğmesine dokun. Tarayıcın bu tür dosyaların zararlı olabileceği uyarısını gösterebilir; “Yine de indir”i seç.',
    'in.s3t': 'Bu kaynağa izin ver',
    'in.s3': 'İndirilen dosyayı aç. Android sorarsa Ayarlar’a dokun, tarayıcın için “Bu kaynaktan izin ver” seçeneğini aç ve geri dön.',
    'in.s4t': 'Kur ve aç',
    'in.s4': 'Yükle’ye, ardından Aç’a dokun. Açılış ekranında başlamak için dokun. Misafir hesabın otomatik olarak oluşturulur.',
    'in.note': 'HADAL’ı yalnızca bu siteden indir. Sürüm Play Store’dan gelmediği için Google Play Protect bir uyarı gösterebilir; test sürümlerinde bu beklenen bir durumdur.',

    'faq.title': 'Yüzeyden gelen sorular',
    'faq.q1': 'HADAL nasıl bir oyun?',
    'faq.a1': 'Okyanusun dibinde geçen bir üs kurma ve strateji MMO’su. Bir reaktörün çevresinde kubbelerden oluşan bir koloni büyütür, onu hayatta tutan kaynakları yönetirsin. Geliştirme ilerledikçe denizaltılarını keşfe gönderecek ve diğer oyuncularla ittifak kuracaksın.',
    'faq.q2': 'App Store ve Google Play’e ne zaman gelecek?',
    'faq.a2': 'Henüz bir çıkış tarihi yok. HADAL erken erişim testinde; mağaza sürümleri oyun hazır olduğunda gelecek. O zamana kadar oynamanın yolu bu sayfadaki Android test sürümü.',
    'faq.q3': 'iPhone sürümü var mı?',
    'faq.a3': 'Henüz yok. HADAL hem iOS hem Android için geliştiriliyor, ama şu anki test sürümü yalnızca Android’de.',
    'faq.q4': 'Test sürümü ücretli mi?',
    'faq.a4': 'Hayır. Android test sürümünü indirmek ve oynamak ücretsiz.',
    'faq.q5': 'İlerlemem tam sürüme aktarılacak mı?',
    'faq.a5': 'Bunun sözünü veremiyoruz. Bu bir test sürümü; oyun çıkıştan önce değiştikçe ilerleme sıfırlanabilir.',
    'faq.q6': 'Telefon değiştirirsem hesabımı nasıl korurum?',
    'faq.a6': 'Misafir hesabın cihazında durur. Oyunda Profil ve Hesap’ı aç, Hesabı Koru’ya dokun: bir şifre belirle, sana bir aktarım kodu verilsin. Yeni telefonda “Başka Hesapla Giriş” ile bu kodu ve şifreni kullan. Google ve Apple ile giriş daha sonra gelecek.',
    'faq.q7': 'Çevrimdışı oynayabilir miyim?',
    'faq.a7': 'Oynamak için bağlantı gerekir, çünkü dünyayı oyun sunucusu yönetir. Ama üssün sensiz de işler: sen yokken inşaatlar biter, üretim sürer.',
    'faq.q8': 'Android APK için neden uyarı veriyor?',
    'faq.a8': 'Android, uygulama mağazası dışından kurulan her uygulama için uyarı gösterir. HADAL test sürümünü CiciByte Technology, Inc. yayınlar; yalnızca bu siteden indir.',

    'ft.tag': 'Uçurumun dibinde, insanlık yeniden başlıyor.',
    'ft.rights': 'Tüm hakları saklıdır.'
  };

  var META = {
    en: {
      title: 'HADAL — Deep-Sea Strategy MMO',
      desc: "HADAL is a deep-sea base-building strategy MMO for iOS and Android. Build humanity's last domes 11,000 metres down. Early access: the Android test build is available now."
    },
    tr: {
      title: 'HADAL — Derin Deniz Strateji MMO’su',
      desc: 'HADAL, iOS ve Android için derin deniz temalı bir üs kurma ve strateji MMO’su. İnsanlığın son kubbelerini 11.000 metre derinlikte kur. Erken erişim: Android test sürümü şimdi indirilebilir.'
    }
  };

  var KEY = 'hadal_lang';
  var EN = { text: {}, alt: {}, aria: {} };
  var textNodes = document.querySelectorAll('[data-i18n]');
  var altNodes = document.querySelectorAll('[data-i18n-alt]');
  var ariaNodes = document.querySelectorAll('[data-i18n-aria]');
  var descMeta = document.querySelector('meta[name="description"]');

  // Snapshot the English copy from the HTML so it never has to be duplicated here.
  Array.prototype.forEach.call(textNodes, function (el) { EN.text[el.getAttribute('data-i18n')] = el.innerHTML; });
  Array.prototype.forEach.call(altNodes, function (el) { EN.alt[el.getAttribute('data-i18n-alt')] = el.getAttribute('alt'); });
  Array.prototype.forEach.call(ariaNodes, function (el) { EN.aria[el.getAttribute('data-i18n-aria')] = el.getAttribute('aria-label'); });

  function apply(lang) {
    var tr = lang === 'tr';
    Array.prototype.forEach.call(textNodes, function (el) {
      var k = el.getAttribute('data-i18n');
      var v = tr ? TR[k] : EN.text[k];
      if (v == null) return;
      if (tr) el.textContent = v; else el.innerHTML = v;
    });
    Array.prototype.forEach.call(altNodes, function (el) {
      var k = el.getAttribute('data-i18n-alt');
      var v = tr ? TR[k] : EN.alt[k];
      if (v != null) el.setAttribute('alt', v);
    });
    Array.prototype.forEach.call(ariaNodes, function (el) {
      var k = el.getAttribute('data-i18n-aria');
      var v = tr ? TR[k] : EN.aria[k];
      if (v != null) el.setAttribute('aria-label', v);
    });
    document.documentElement.lang = tr ? 'tr' : 'en';
    document.title = META[tr ? 'tr' : 'en'].title;
    if (descMeta) descMeta.setAttribute('content', META[tr ? 'tr' : 'en'].desc);
    Array.prototype.forEach.call(document.querySelectorAll('.lang__btn'), function (b) {
      b.setAttribute('aria-pressed', String(b.getAttribute('data-lang') === (tr ? 'tr' : 'en')));
    });
  }

  function stored() {
    try { return localStorage.getItem(KEY); } catch (e) { return null; }
  }
  function remember(lang) {
    try { localStorage.setItem(KEY, lang); } catch (e) { /* private mode or blocked storage */ }
  }

  var initial = stored();
  if (initial !== 'en' && initial !== 'tr') {
    // First visit: follow the browser if it prefers Turkish, otherwise English.
    var nav = (navigator.languages && navigator.languages[0]) || navigator.language || '';
    initial = /^tr\b/i.test(nav) ? 'tr' : 'en';
  }
  if (initial === 'tr') apply('tr');

  Array.prototype.forEach.call(document.querySelectorAll('.lang__btn'), function (btn) {
    btn.addEventListener('click', function () {
      var lang = btn.getAttribute('data-lang');
      apply(lang);
      remember(lang);
    });
  });

  // Footer year
  var y = document.getElementById('year');
  if (y) y.textContent = String(new Date().getFullYear());

  // Decorative depth gauge: page scroll mapped to 0 – 11,000 m.
  var fill = document.getElementById('depth-fill');
  var value = document.getElementById('depth-value');
  var MAX = 11000;
  var ticking = false;
  function updateDepth() {
    ticking = false;
    var doc = document.documentElement;
    var range = Math.max(1, doc.scrollHeight - window.innerHeight);
    var t = Math.min(1, Math.max(0, window.scrollY / range));
    var m = Math.round((t * MAX) / 10) * 10;
    if (fill) fill.style.width = (t * 100).toFixed(1) + '%';
    if (value) value.textContent = (m ? '-' : '') + m.toLocaleString(document.documentElement.lang === 'tr' ? 'tr-TR' : 'en-US') + ' m';
  }
  window.addEventListener('scroll', function () {
    if (!ticking) { ticking = true; window.requestAnimationFrame(updateDepth); }
  }, { passive: true });
  window.addEventListener('resize', updateDepth, { passive: true });
  updateDepth();
})();
