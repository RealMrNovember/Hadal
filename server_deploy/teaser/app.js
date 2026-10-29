/* HADAL landing page: language toggle (EN/TR/IT) and the decorative depth gauge.
   English lives in index.html; Turkish and Italian live here. No trackers, no network calls. */
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

  var IT = {
    'skip': "Vai al contenuto",
    'nav.label': "Menu principale",
    'nav.world': "Mondo",
    'nav.gameplay': "Gameplay",
    'nav.status': "Stato",
    'nav.install': "Installazione",
    'nav.faq': "FAQ",
    'lang.label': "Lingua",
    'hero.eyebrow': "Accesso anticipato · Build di prova Android",
    'hero.t1': "Sotto l’abisso,",
    'hero.t2': "l’umanità ricomincia.",
    'hero.lead': "Un MMO strategico di costruzione di basi negli abissi per iOS e Android. Costruisci le ultime cupole dell’umanità a 11.000 metri di profondità, mantieni l’ossigeno in circolo e manda i tuoi sottomarini in un buio molto più antico di noi.",
    'hero.cta': "Scarica la build di prova Android (APK)",
    'hero.howto': "Come installare",
    'hero.note': "Android 8.0+ · Gratis · Richiede una connessione Internet",
    'badge.apple': "App Store — in arrivo",
    'badge.google': "Google Play — in arrivo",
    'badge.small': "Presto su",
    'badge.small2': "Presto su",
    'hero.phoneAlt': "Schermata iniziale di HADAL su un telefono Android: il logo HADAL, il motto e la scritta “Tocca per iniziare”",
    'hero.phoneCap': "Schermata iniziale, build di prova attuale",
    'world.kicker': "Il mondo",
    'world.title': "Anno 2249. La superficie è morta.",
    'world.p1': "L’atmosfera è diventata tossica. La luce del sole è diventata letale. I continenti non possono più ospitare la vita. Ciò che restava dell’umanità si è ritirato nella Zona Adale, lo strato più profondo dell’oceano, sigillandosi dentro cupole pressurizzate undici chilometri sotto le onde.",
    'world.p2': "Non sono stati i primi ad arrivare. Sul fondo dell’oceano giacciono i resti di una civiltà biomeccanica vissuta milioni di anni prima di noi: templi, macchine che sembrano ancora respirare, soldati conosciuti solo come The Forgotten. L’umanità li ha risvegliati.",
    'world.p3': "Da qualche parte al centro della mappa si trova The Core. Nessuno di quelli che sono andati a cercarlo è tornato con una risposta chiara.",
    'world.f1k': "Profondità",
    'world.f1v': "11.000 m",
    'world.f2k': "Luce",
    'world.f2v': "Quasi nulla",
    'world.f3k': "Pressione",
    'world.f3v': "Letale, ogni secondo",
    'world.f4k': "Vicini",
    'world.f4v': "Più antichi di noi",
    'world.quote': "“Sono uno degli ultimi esseri umani rimasti. Cerco di sopravvivere nel buio. Ma in quel buio qualcosa di molto più antico di me mi sta osservando.”",
    'world.creaturesLabel': "Immagini delle creature",
    'world.c1': "Immagine di una creatura rana pescatrice degli abissi",
    'world.c2': "Immagine di una medusa sirena viola",
    'world.c3': "Immagine dell’Iron Leviathan, una bestia marina biomeccanica coperta di spine",
    'world.cap': "Modelli delle creature dal set grafico in sviluppo.",
    'gp.kicker': "Gameplay",
    'gp.title': "Costruisci. Respira. Scendi più a fondo.",
    'gp.lead': "La tua base è la tua casa e la tua identità: cupole disposte attorno a un unico reattore, che crescono verso l’esterno man mano che sopravvivi. Ogni tua mossa è verificata dal server di gioco, così il mondo resta equo per tutti e la tua base continua a funzionare anche quando non ci sei.",
    'gp.lineupAlt': "Edifici della base di HADAL su piattaforme esagonali illuminati di ciano e ambra: reattore, cupole, laboratori e torri",
    'gp.lineupCap': "Edifici della base dalle illustrazioni attuali.",
    'tag.live': "Giocabile ora",
    'tag.partial': "Parzialmente giocabile",
    'tag.dev': "In sviluppo",
    'tag.planned': "Pianificato",
    'gp.p1t': "Costruzione e potenziamento della base",
    'gp.p1': "Al centro c’è il Reattore Centrale; tutto il resto si costruisce negli anelli attorno a esso. Tocca uno slot vuoto per costruire, tocca un edificio per potenziarlo. Nessun edificio può superare il livello del reattore: ogni progresso parte dal cuore della tua base.",
    'gp.p1a': "I lavori avanzano con l’orologio del server, anche quando sei offline",
    'gp.p1b': "Le attese brevi le offriamo noi: gli ultimi cinque minuti finiscono gratis",
    'gp.p1c': "Durante il potenziamento l’edificio continua a produrre al livello precedente",
    'gp.p2t': "Economia di sopravvivenza",
    'gp.p2': "Sei risorse tengono in vita una colonia. L’ossigeno viene sempre per primo: se finisce, la base muore. L’energia arriva dal reattore, la biomassa dagli organismi viventi, il titanio dai tuoi droni; l’hadalite si trova solo nelle zone più profonde.",
    'gp.resLabel': "Risorse",
    'res.o2': "Ossigeno",
    'res.energy': "Energia",
    'res.food': "Cibo",
    'res.bio': "Biomassa",
    'res.ti': "Titanio",
    'res.hadalite': "Hadalite",
    'gp.p3t': "Spedizioni in sottomarino",
    'gp.p3': "Costruisci i sottomarini nel Molo Sottomarini, assegna tre eroi e mandali nel buio: foreste bioluminescenti, città sommerse, vecchi laboratori umani, templi alieni. In alcune zone non puoi accendere le luci. Ti resta solo il sonar.",
    'gp.sub1': "Immagine di un sottomarino da ricognizione arancione",
    'gp.sub2': "Immagine di un sottomarino da carico giallo",
    'gp.p4t': "Alleanze e PvP",
    'gp.p4': "Nessuno sopravvive da solo nell’abisso. Le alleanze costruiranno strutture comuni e città sottomarine insieme; le cupole rivali si assedieranno a vicenda. E tutte le strade portano alla guerra per The Core, al centro del mondo.",
    'gp.bTitle': "Cosa c’è negli anelli",
    'b.reactor': "Reattore Centrale",
    'b.oxygen': "Impianto di Ossigeno",
    'b.food': "Laboratorio Alimentare",
    'b.bio': "Bio-laboratorio",
    'b.drone': "Fabbrica di Droni",
    'b.research': "Centro di Ricerca",
    'b.dock': "Molo Sottomarini",
    'b.shield': "Generatore di Scudi",
    'b.mega': "Mega Cupola",
    'st.kicker': "Stato dello sviluppo",
    'st.title': "Accesso anticipato, in tutta trasparenza.",
    'st.lead': "HADAL è in sviluppo attivo. La build di prova Android è una prima parte del gioco: la base funziona, gli abissi sono ancora in costruzione. Ecco a che punto siamo:",
    'st.nowTag': "Disponibile",
    'st.nowTitle': "Oggi nella build di prova",
    'st.n1': "Account ospite immediato, nessuna registrazione",
    'st.n2': "Base circolare con il Reattore Centrale al suo cuore",
    'st.n3': "Costruzione e potenziamento con tempi reali",
    'st.n4': "Produzione di risorse lato server, anche offline",
    'st.n5': "Protezione dell’account con codice di trasferimento per cambiare telefono",
    'st.n6': "Codici regalo e annunci nel gioco",
    'st.nextTag': "In arrivo",
    'st.nextTitle': "Su cosa stiamo lavorando",
    'st.x1': "Costruzione completa della base: apertura degli anelli, mega cupole, demolizione degli edifici",
    'st.x2': "Ciclo di sopravvivenza: consumo di ossigeno, allarmi ed emergenze",
    'st.x3': "Presentazione: nuova interfaccia, grafica subacquea e audio",
    'st.laterTag': "Più avanti",
    'st.laterTitle': "Nella roadmap",
    'st.l1': "Sottomarini, pressione ed esplorazione col sonar",
    'st.l2': "Eroi, nemici e combattimento",
    'st.l3': "Mappa del mondo condivisa, alleanze, PvP e assedi",
    'st.l4': "Leviatani, The Core e lancio su App Store / Google Play",
    'st.shot1': "Schermata della build di prova: pannello del Reattore Centrale di livello 1, produzione di energia e costo del livello successivo",
    'st.shot1cap': "Pannello del Reattore Centrale",
    'st.shot2': "Schermata della build di prova: un Impianto di Ossigeno in costruzione con il conto alla rovescia e il pulsante Completa gratis",
    'st.shot2cap': "Costruzione con completamento gratuito",
    'st.cap': "Schermate prese dalla build di sviluppo attuale, senza ritocchi. Grafica e interfaccia sono provvisorie e cambieranno.",
    'in.kicker': "Installazione",
    'in.title': "Installare la build di prova Android",
    'in.lead': "La build di prova non è ancora su Google Play, quindi Android la installa come file. Ci vuole circa un minuto.",
    'in.s1t': "Controlla il telefono",
    'in.s1': "Servono Android 8.0 o superiore e una connessione Internet. Per ora l’iPhone non è supportato.",
    'in.s2t': "Scarica l’APK",
    'in.s2': "Apri questa pagina sul telefono e tocca il pulsante di download. Il browser potrebbe avvisarti che questo tipo di file può essere dannoso; scegli “Scarica comunque”.",
    'in.s3t': "Consenti questa origine",
    'in.s3': "Apri il file scaricato. Se Android lo chiede, tocca Impostazioni, attiva “Consenti da questa origine” per il browser e torna indietro.",
    'in.s4t': "Installa e apri",
    'in.s4': "Tocca Installa, poi Apri. Nella schermata iniziale tocca per iniziare. Il tuo account ospite viene creato automaticamente.",
    'in.note': "Scarica HADAL solo da questo sito. Poiché la build non arriva dal Play Store, Google Play Protect potrebbe mostrare un avviso; per le build di prova è normale.",
    'faq.title': "Domande dalla superficie",
    'faq.q1': "Che tipo di gioco è HADAL?",
    'faq.a1': "Un MMO strategico di costruzione di basi ambientato sul fondo dell’oceano. Fai crescere una colonia di cupole attorno a un reattore e gestisci le risorse che la tengono in vita. Con l’avanzare dello sviluppo manderai i sottomarini in esplorazione e formerai alleanze con altri giocatori.",
    'faq.q2': "Quando arriverà su App Store e Google Play?",
    'faq.a2': "Non c’è ancora una data di uscita. HADAL è in test ad accesso anticipato; le versioni per gli store arriveranno quando il gioco sarà pronto. Fino ad allora, per giocare usa la build di prova Android di questa pagina.",
    'faq.q3': "Esiste una versione per iPhone?",
    'faq.a3': "Non ancora. HADAL è sviluppato sia per iOS sia per Android, ma la build di prova attuale è solo per Android.",
    'faq.q4': "La build di prova è a pagamento?",
    'faq.a4': "No. Scaricare e giocare la build di prova Android è gratis.",
    'faq.q5': "I miei progressi passeranno alla versione completa?",
    'faq.a5': "Non possiamo prometterlo. Questa è una build di prova; i progressi potrebbero essere azzerati man mano che il gioco cambia prima dell’uscita.",
    'faq.q6': "Come proteggo il mio account se cambio telefono?",
    'faq.a6': "Il tuo account ospite resta sul dispositivo. Nel gioco apri Profilo e Account e tocca Proteggi account: imposta una password e riceverai un codice di trasferimento. Sul nuovo telefono usa “Accedi con un altro account” con quel codice e la password. L’accesso con Google e Apple arriverà più avanti.",
    'faq.q7': "Posso giocare offline?",
    'faq.a7': "Per giocare serve una connessione, perché il mondo è gestito dal server di gioco. Ma la tua base lavora anche senza di te: mentre sei via i lavori finiscono e la produzione continua.",
    'faq.q8': "Perché Android mostra un avviso per l’APK?",
    'faq.a8': "Android mostra un avviso per ogni app installata fuori da uno store. La build di prova di HADAL è pubblicata da CiciByte Technology, Inc.; scaricala solo da questo sito.",
    'ft.tag': "Sotto l’abisso, l’umanità ricomincia.",
    'ft.rights': "Tutti i diritti riservati."
  };

  var META = {
    en: {
      title: 'HADAL — Deep-Sea Strategy MMO',
      desc: "HADAL is a deep-sea base-building strategy MMO for iOS and Android. Build humanity's last domes 11,000 metres down. Early access: the Android test build is available now."
    },
    it: {
      title: 'HADAL — MMO strategico negli abissi',
      desc: 'HADAL è un MMO strategico di costruzione di basi negli abissi per iOS e Android. Costruisci le ultime cupole dell’umanità a 11.000 metri di profondità. Accesso anticipato: la build di prova Android è disponibile ora.'
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

  var DICT = { tr: TR, it: IT };
  var LOCALE = { en: 'en-US', tr: 'tr-TR', it: 'it-IT' };

  function apply(lang) {
    if (!LOCALE[lang]) lang = 'en';
    var dict = DICT[lang];
    Array.prototype.forEach.call(textNodes, function (el) {
      var k = el.getAttribute('data-i18n');
      var v = dict ? dict[k] : EN.text[k];
      if (v == null) return;
      if (dict) el.textContent = v; else el.innerHTML = v;
    });
    Array.prototype.forEach.call(altNodes, function (el) {
      var k = el.getAttribute('data-i18n-alt');
      var v = dict ? dict[k] : EN.alt[k];
      if (v != null) el.setAttribute('alt', v);
    });
    Array.prototype.forEach.call(ariaNodes, function (el) {
      var k = el.getAttribute('data-i18n-aria');
      var v = dict ? dict[k] : EN.aria[k];
      if (v != null) el.setAttribute('aria-label', v);
    });
    document.documentElement.lang = lang;
    document.title = META[lang].title;
    if (descMeta) descMeta.setAttribute('content', META[lang].desc);
    Array.prototype.forEach.call(document.querySelectorAll('.lang__btn'), function (b) {
      b.setAttribute('aria-pressed', String(b.getAttribute('data-lang') === lang));
    });
    updateDepth();
  }

  function stored() {
    try { return localStorage.getItem(KEY); } catch (e) { return null; }
  }
  function remember(lang) {
    try { localStorage.setItem(KEY, lang); } catch (e) { /* private mode or blocked storage */ }
  }

  var initial = stored();
  if (!LOCALE[initial]) {
    // First visit: follow the browser if it prefers Turkish or Italian, otherwise English.
    var nav = (navigator.languages && navigator.languages[0]) || navigator.language || '';
    initial = /^tr\b/i.test(nav) ? 'tr' : /^it\b/i.test(nav) ? 'it' : 'en';
  }

  Array.prototype.forEach.call(document.querySelectorAll('.lang__btn'), function (btn) {
    btn.addEventListener('click', function () {
      var lang = btn.getAttribute('data-lang');
      apply(lang);
      remember(lang);
    });
  });

  if (initial !== 'en') apply(initial);

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
    if (value) value.textContent = (m ? '-' : '') + m.toLocaleString(LOCALE[document.documentElement.lang] || 'en-US') + ' m';
  }
  window.addEventListener('scroll', function () {
    if (!ticking) { ticking = true; window.requestAnimationFrame(updateDepth); }
  }, { passive: true });
  window.addEventListener('resize', updateDepth, { passive: true });
  updateDepth();
})();
