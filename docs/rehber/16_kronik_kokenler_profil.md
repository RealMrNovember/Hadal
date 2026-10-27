# 16 · Kronik, Köken, Profil

> **Özet.** Her sunucu ortak bir zaman çizelgesi izler: **Derinlik Kroniği** (K-20). On iki aşama,
> kuşakları, geçitleri, asker kademelerini, Akıntı Savaşı'nı, Çekirdek'i (~180. gün) ve Hadal Titanı'nı
> (~240. gün) sırayla açar; sunucu hedeflerini tamamlayan sunucu bir sonraki aşamayı birkaç gün erken
> açar. Oyuncu oyuna beş **köken**den biriyle başlar (K-19): üç küçük pasif, bir köken kahramanı, kendi
> T8–T10 birliği ve kendi mimarisi. **Kaptan profili** gücün dökümünü, istatistikleri, başarımları ve
> unvanları gösterir; harcamayı göstermez. **Göç** çukur sunucuları arasında pasaportla, sıkı ve adil
> kurallarla yapılır.

Bağlı bölümler: [00 · Kanon](00_kanon.md) · [02 · Tempo ve Denge](02_tempo_ve_denge.md) ·
[05 · Kahramanlar](05_kahramanlar.md) · [07 · Birlikler ve Savaş](07_birlikler_ve_savas.md) ·
[09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md) · [10 · İttifak](10_ittifak.md) ·
[13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md) · [14 · Son Oyun](14_son_oyun.md) ·
[18 · Bilim Temeli](18_bilim_temeli.md).
Veri: [`veri/kronik.json`](veri/kronik.json), [`veri/kokenler.json`](veri/kokenler.json).

---

## 1. Amaç

- **Kronik**, bir yıllık tempoya ([02](02_tempo_ve_denge.md)) sunucu düzeyinde bir ritim verir:
  oyuncu tek başına reaktör yükseltirken sunucu da birlikte "daha derine iner". Erken öne fırlayan
  ödeyen oyuncu derinin ödüllerini sunucudan önce toplayamaz; geride kalan ücretsiz oyuncu kapıların
  kendinden önce açıldığını görmez (kapılar F2P temposuna göre ayarlıdır, §2.3).
- **Köken**, ilk dakikadan bir kimlik verir: oyuncu "bir üs" değil, bir topluluğun parçası olarak
  başlar. Etkiler küçüktür; seçim üslup seçimidir, güç seçimi değildir.
- **Profil**, oyuncunun hikâyesini anlatır: ne inşa etti, ne keşfetti, kime yardım etti. Harcamayı
  değil emeği gösterir.
- **Göç**, kapanan sunucularda yalnız kalan oyuncuya ve arkadaşına katılmak isteyene bir yol açar;
  yeni sunucuları güçlü göçmenlerin ezmesine izin vermez.

---

## 2. Derinlik Kroniği

### 2.1 Nasıl işler?

1. Gün, sunucunun açıldığı günden sayılır.
2. Her aşamanın bir **en erken** ve bir **en geç** günü vardır. Önceki aşamanın **sunucu hedefi**
   tamamlanmışsa aşama en erken günde açılır; tamamlanmamışsa en geç günde kendiliğinden açılır.
   Sunucu hiçbir zaman takılıp kalmaz; çalışkan bir sunucu en çok birkaç gün kazanır.
3. Her aşamanın **kişisel** ve **ittifak** hedefleri vardır; ödüller Kronik Feneri'nden alınır
   ([Kronik Feneri](binalar/chronicle_beacon.md), Reaktör 8). Feneri olmayan kaptan hedeflere katkı
   verir, ödülünü fener kurulunca alır.
4. Kronik geriye gitmez. Aşama açıldıktan sonra o aşamanın hedefleri sezon boyunca açık kalır; geç
   katılan oyuncu eski aşamaların kişisel ödüllerini de toplayabilir.

### 2.2 Aşamalar

<!-- URET:tablo:kronik.asamalar -->
| # | Aşama | Gün | Kronikte yazan | Açılanlar | Sunucu hedefi (erken açılış) |
| ---: | --- | --- | --- | --- | --- |
| 1 | İniş | 1 | Sunucu açılır; ilk konvoylar Hadal Eşiği'ne demir atar. | Abisal Düzlük ve Hadal Eşiği; yatak Sv 1–3; Uçurum Sürüngenleri Sv 1–10; T1–T3; ittifak kurma; Yeni Kaptan Koruması | 10 ittifak 25 üyeye ulaşsın · 20.000 Uçurum Sürüngeni yenilsin |
| 2 | İlk Yankı | 4–6 | Sonarlar ilk kez düzenli bir yankı duyar: Rezonans Alanları uyanır. | Yatak Sv 4; Siren Sürüsü; düşman Sv 15'e kadar; Rezonans Alanı I (Eşik ve Abisal); ittifak bölgesi (Karargâh, Sonar Kulesi); Balina Leşi olayları; T4–T5 | 8 farklı Rezonans Alanı ele geçirilsin |
| 3 | Fay Geçitleri | 12–15 | Kanatları ayıran fay sırtlarında geçitler açılır. | Hadal Eşiği'ndeki 4 Fay Geçidi; düşman Sv 20'ye kadar; İttifak Madenleri; T6 | 4 Fay Geçidi'nin bekçisi yenilsin |
| 4 | Birinci Kilit | 25–30 | Çukurun iç duvarındaki ilk basınç kilitleri çözülür. | Basınç Kilidi I ve Yamaç Fay Geçitleri; Çukur Yamacı; yatak Sv 5; Kadim Muhafız Devriyeleri; Kadim Kalıntı I; düşman Sv 25'e kadar; Savaş Bildirisi; T7 | 4 Basınç Kilidi I'in hepsi ele geçirilsin |
| 5 | Yamaç Çekişmesi | 40–45 | Yamaçta ittifaklar yerleşir; ilk büyük alanlar için çekişme başlar. | Rezonans Alanı II (Yamaç ve Abisal); Abisal'de Sv 5 nodül sahaları; düşman Sv 30'a kadar; İttifak Alaşım Madeni (İttifak Sv 10) | Rezonans Alanları toplam 40 kez el değiştirsin |
| 6 | İkinci Kilit | 60–66 | Derin Yamaç'a inen kilitler açılır; sonarda ilk Leviathan yuvaları belirir. | Basınç Kilidi II; Derin Yamaç; yatak Sv 6; Demir Leviathan Yuvası Sv 1–2; Basınç Kodeksi düşüşleri; Rezonans II (Derin Yamaç); düşman Sv 35'e kadar; T8 | 100 Leviathan yuvası yenilsin |
| 7 | Akıntı Çağrısı | 90–95 | Komşu çukurlar sonar menzilinde; çukurlar arası sezon savaşı başlar. | Akıntı Savaşı'nın ilk sezonu (13); Abisal'de Sv 6 nodül sahaları; düşman Sv 40'a kadar | Akıntı Savaşı'nın ilk haftasında sunucu hedefi |
| 8 | Eksen | 120–126 | Çukurun ekseni açılır; büyük Rezonans Alanları uyanır. | Basınç Kilidi III; Çukur Ekseni; yatak Sv 7; Leviathan Sv 3–4; Rezonans Alanı III; düşman Sv 45'e kadar; T9 | 4 Rezonans Alanı III'ün hepsi ele geçirilsin |
| 9 | Çekirdek Kapıları | 150–156 | Dibe inen dört kapının bekçileri uyanır. | 4 Çekirdek Kapısı (Bekçi V); düşman Sv 50'ye kadar | 4 Çekirdek Kapısı'nın bekçisi yenilsin (aşama 10 en erken güne çekilir) |
| 10 | Çekirdek | 180–186 | Dip açılır. Çukurun ortasında, insan eliyle yapılmamış bir kalp atıyor. | Dip ve Çekirdek; Çukur Kalbi ve Egemen; yatak Sv 8; Hadalit Damarı; Leviathan Sv 5–6; T10; Basınç Kilidi I–III aşınır (serbest geçiş) | İlk Egemen taç giysin |
| 11 | Titan'ın Uyanışı | 240–246 | Kalbin altındaki titreşim bir bedene dönüşür. | Hadal Titanı (14); Çekirdek Rezonansı hazırlıkları | Hadal Titanı'na sunucu hasar hedefi |
| 12 | Derin Döngü | 300 | Kronik sezonlara döner: her 60 günde yeni hedefler, Egemenlik ve Akıntı Savaşı döngüsü. | 60 günlük Kronik sezonları; göç pencereleri; çukur birleşmesi adaylığı | Sezona göre |
<!-- /URET:tablo:kronik.asamalar -->

İki büyük kilometre taşı: **Çekirdek** (aşama 10, en erken
<!--v:veri.kronik.asamalar.cekirdek.en_erken-->180<!--/v-->. gün) Çukur Kalbi'ni ve Egemenliği açar
([09](09_dunya_haritasi_ve_pvp.md) §7); **Titan'ın Uyanışı** (aşama 11, en erken
<!--v:veri.kronik.asamalar.titanin_uyanisi.en_erken-->240<!--/v-->. gün) Hadal Titanı'nı uyandırır
([14 · Son Oyun](14_son_oyun.md)). Akıntı Savaşı'nın ilk sezonu aşama 7 ile
(<!--v:veri.kronik.asamalar.akinti_cagrisi.en_erken-->90<!--/v-->. gün) açılır; sezon takvimi
[13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md)'tedir.

### 2.3 Kronik ve tempo

Kronik kapıları ücretsiz oyuncunun temposuna göre ayarlanır: her kademe ve kuşak, F2P oyuncunun ona
gereken binaya ulaşmasından **önce ya da hemen hemen aynı anda** açılır. Kapı yalnızca ödeyen oyuncuyu
bekletir.

| Asker kademesi | Kronik aşaması | En erken gün | Tatbikat Havuzu Sv | F2P bu reaktöre ulaşır |
|---|---|---:|---|---:|
| T1–T3 | 1 · İniş | 1 | 1 · 5 · 10 | R10: <!--v:hesap.sim.f2p.seviye.10|gun-->6 sa 51 dk<!--/v--> |
| T4–T5 | 2 · İlk Yankı | 4 | 15 · 20 | R20: <!--v:hesap.sim.f2p.seviye.20|gun-->4,5. gün<!--/v--> |
| T6 | 3 · Fay Geçitleri | 12 | 25 | R25: <!--v:hesap.sim.f2p.seviye.25|gun-->13,4. gün<!--/v--> |
| T7 | 4 · Birinci Kilit | 25 | 30 | R30: <!--v:hesap.sim.f2p.seviye.30|gun-->28,6. gün<!--/v--> |
| T8 | 6 · İkinci Kilit | 60 | 35 | R35: <!--v:hesap.sim.f2p.seviye.35|gun-->68,7. gün<!--/v--> |
| T9 | 8 · Eksen | 120 | 40 | R40: <!--v:hesap.sim.f2p.seviye.40|gun-->128,4. gün<!--/v--> |
| T10 | 10 · Çekirdek | 180 | 45 | R45: <!--v:hesap.sim.f2p.seviye.45|gun-->224,9. gün<!--/v--> |

Kademe ayrıca Tatbikat Havuzu seviyesini ve araştırmayı ister ([Tatbikat Havuzu](binalar/training_grounds.md),
[07](07_birlikler_ve_savas.md)). Karşılaştırma: aylık kart oyuncusu Reaktör 40'a
<!--v:hesap.sim.aylik_kart.seviye.40|gun-->84,5. gün<!--/v-->, balina
<!--v:hesap.sim.balina.seviye.40|gun-->45,9. gün<!--/v--> ulaşır; T9 için ikisi de 120. günü bekler.
Kuşak kapılarıyla gövde sınıfı karşılaştırması: [09](09_dunya_haritasi_ve_pvp.md) §2.3.

### 2.4 Hedefler ve ödüller

<!-- URET:tablo:kronik.hedefler -->
| Aşama | Tür | Hedef | Ödül |
| --- | --- | --- | --- |
| 1 · İniş | Kişisel | Kronik Feneri'ni kur (Reaktör 8) | 300 Hadalit + 1 sa genel hızlandırma |
| 1 · İniş | Kişisel | 10 Uçurum Sürüngeni yen; 1 toplama seferini tamamla | Sonar Pili ×2 + Kaynak Kapsülü (1 GS) |
| 1 · İniş | İttifak | 25 üyeye ulaş | 500 İttifak Kredisi + Kronik Hediyesi |
| 2 · İlk Yankı | Kişisel | 1 Rezonans Kristali topla; Radar ile 3 tür tara | 200 Hadalit + Anı Kristali |
| 2 · İlk Yankı | İttifak | Karargâh Kubbesi kur ve 1 Rezonans Alanı'nı 24 saat tut | 1.000 İttifak Kredisi + Kronik Hediyesi |
| 3 · Fay Geçitleri | Kişisel | Bir Fay Geçidi'nden geç; 30 PvE zaferi | 300 Hadalit + 2 sa genel hızlandırma |
| 3 · Fay Geçitleri | İttifak | Bir Fay Geçidi'ni ele geçir; 30 bağlı Sonar Kulesi | 1.500 İttifak Kredisi + Kronik Hediyesi |
| 4 · Birinci Kilit | Kişisel | Çukur Yamacı'nda 3 toplama; 10 Muhafız Devriyesi | 400 Hadalit + Sessizlik Kalkanı 8 sa |
| 4 · Birinci Kilit | İttifak | Bir Basınç Kilidi'ni 24 saat tut | 2.000 İttifak Kredisi + Kronik Hediyesi |
| 5 · Yamaç Çekişmesi | Kişisel | 300 Süperalaşım topla (yatak) | 400 Hadalit + 100 Süperalaşım |
| 5 · Yamaç Çekişmesi | İttifak | İki farklı türde Rezonans Alanı'nı aynı anda tut | 2.500 İttifak Kredisi + Kronik Hediyesi |
| 6 · İkinci Kilit | Kişisel | Bir Leviathan rallisine katıl; 30 Basınç Kodeksi topla | 500 Hadalit + 50 Basınç Kodeksi |
| 6 · İkinci Kilit | İttifak | Sv 2 Leviathan yuvası yen | 3.000 İttifak Kredisi + Kronik Hediyesi |
| 7 · Akıntı Çağrısı | Kişisel | Akıntı Savaşı'nda 1 etkinliğe katıl | 500 Hadalit + Çukur Pasaportu ×1 |
| 7 · Akıntı Çağrısı | İttifak | Akıntı Savaşı ittifak hedefi | 3.000 İttifak Kredisi + Kronik Hediyesi |
| 8 · Eksen | Kişisel | Çukur Ekseni'nde Sv 40+ bir Muhafız Devriyesi yen | 600 Hadalit + 100 Basınç Kodeksi |
| 8 · Eksen | İttifak | Bir Rezonans Alanı III'ü tut | 4.000 İttifak Kredisi + Kronik Hediyesi |
| 9 · Çekirdek Kapıları | Kişisel | Bir Çekirdek Kapısı rallisine katıl | 600 Hadalit + 100 Basınç Kodeksi |
| 9 · Çekirdek Kapıları | İttifak | Bir Çekirdek Kapısı'nı tut | 5.000 İttifak Kredisi + Kronik Hediyesi |
| 10 · Çekirdek | Kişisel | Çekirdek'te bir Leviathan rallisine katıl | 1 Kadim Şema + 500 Hadalit |
| 10 · Çekirdek | İttifak | Çukur Kalbi'ni kesintisiz 1 saat tut | 5.000 İttifak Kredisi + Kronik Hediyesi |
| 11 · Titan'ın Uyanışı | Kişisel | Hadal Titanı'na hasar ver | 500 Hadalit + Titan kalıntısı (14) |
<!-- /URET:tablo:kronik.hedefler -->

- Kişisel ödüllerin toplamı bir yılda ~5.000 Hadalit eder; `oduller.json`'daki "Kronik ve Tür
  Kataloğu başarımları" satırıyla birlikte okunur ([15](15_ekonomi_akisi.md)).
- Aşama 10'un kişisel ödülü bir **Kadim Şema**'dır: ücretsiz oyuncunun Reaktör 49 → 50 adımı için
  güvenceli yolu ([Çekirdek Reaktör](binalar/core_reactor.md)).
- İttifak ödülleri ittifaka bir kez verilir (İttifak Kredisi) ve bütün üyelere bir **Kronik Hediyesi**
  düşer ([10](10_ittifak.md) §9).
- Sunucu hedefleri ödül vermez, **zaman** verir: bir sonraki aşamayı en erken güne çeker.

### 2.5 Kronik Feneri ve Kronik Kaydı

Kronik Feneri ekranı üç parçadır:

1. **Zaman çizelgesi:** yatay, sakin bir şerit. Geçmiş aşamalar dolu, şimdiki aşama ince bir ışıkla,
   gelecek aşamalar yalnızca adlarıyla. Sunucu hedefi tamamsa bir sonraki aşamanın açılış günü yazar.
2. **Hedefler:** kişisel, ittifak ve sunucu hedefleri ilerleme çubuklarıyla.
3. **Kronik Kaydı:** sunucunun ilkleri, kalıcı olarak: ilk 25 üyeli ittifak, ilk ele geçirilen Fay
   Geçidi ve her Basınç Kilidi, ilk Leviathan yuvası, ilk Rezonans Alanı III, ilk Egemen ve
   **Egemenler Kaydı**, Titan'a en çok hasar veren ittifak. Kayıt kaptan ve ittifak adı taşır, harcama
   bilgisi taşımaz.

Fener seviyesi Kronik'i hızlandırmaz (sunucu ortaktır); fener yükseldikçe Kaydın ayrıntısı ve fenerin
üs görünümü değişir.

---

## 3. Kökenler

### 3.1 Seçim

- Oyuncu açılış hikâyesinde, ilk iniş sırasında beş kökenden birini seçer (K-19). Seçim ekranı her
  kökenin sözünü, kahramanını, mimarisini ve üç pasifini gösterir; "en güçlü" gibi bir etiket yoktur.
- Herkes hikâyenin başlangıç kahramanı Komutan Elias Rourke (`mara`) ile başlar (K-05). Köken ayrıca
  bir **köken kahramanı** verir; köken kahramanı zaten varsa kart yerine yıldız parçası gelir.
- **Başlangıç değeri eşitlenir:** köken kahramanı nadirliğine göre farklı yıldızla gelir; Sıradan 3★,
  Nadir 2★, Destansı 1★. Bir Sıradan kahramanın 3★ hâli ile bir Destansı kahramanın 1★ hâli erken oyunda
  yakın güçtedir ([05](05_kahramanlar.md)).

### 3.2 Beş köken

<!-- URET:tablo:kokenler.kokenler -->
| Köken | Söz | Köken kahramanı | Başlangıç ★ | Pasif 1 | Pasif 2 | Pasif 3 |
| --- | --- | --- | ---: | --- | --- | --- |
| Atlantik Birliği | Kimse geride kalmaz. | Komutan Elias Rourke | 3 | +%5 Titanyum üretimi | +%3 Zırhlı Dalgıç savunması | +%5 üs dayanıklılığı |
| Pasifik Tarikatı | Ölç, anla, iyileştir. | Dr. Lyra Moss | 3 | +%5 iyileştirme hızı | +%5 Oksijen üretimi | +%2 araştırma hızı |
| Derindoğanlar | Karanlık bizim evimiz. | Kael | 2 | +%5 toplama hızı | +%5 Gıda üretimi | +%3 PvE düşmanlarına hasar |
| Unutulanlar | Enkaz da bir yoldur. | Juno Vega | 1 | +%5 taşıma kapasitesi | +%5 kurtarma görevi ödülü | +%3 yürüyüş hızı |
| Uçurum Tarikatı | Dip bizi çağırıyor. | Sable | 1 | +%4 Kadim Muhafız ve Leviathan'lara hasar | +%5 Sonar Enerjisi yenilenmesi | +%5 Basınç Ambarı koruması |
<!-- /URET:tablo:kokenler.kokenler -->

Her kökenin üç pasifi bir ekonomi, bir savaş ve bir koruma/hareket etkisi karışımıdır; hiçbiri inşa
hızı vermez, böylece tempo simülasyonu kökenden bağımsız kalır.

### 3.3 Hikâyeler

**<!--v:veri.kokenler.kokenler.AtlanticUnion.ad-->Atlantik Birliği<!--/v-->.**
<!--v:veri.kokenler.kokenler.AtlanticUnion.lore-->Kuzey Atlantik'in son tersanelerinden çıkan konvoyların torunlarıdır. Yüzey yaşanmaz hâle geldiğinde ilk inenler onlardı: ağır gövdeler, sıkı vardiyalar ve tek bir yemin, kimse geride kalmaz. Basınç küresini, kaynağı ve perçini iyi bilirler; bir üssü kurmak kadar onu tutmayı da. Komutan Elias Rourke son konvoyun komutanıdır ve hâlâ her gece kayıp gövdelerin listesini okur.<!--/v-->

**<!--v:veri.kokenler.kokenler.PacificOrder.ad-->Pasifik Tarikatı<!--/v-->.**
<!--v:veri.kokenler.kokenler.PacificOrder.lore-->Pasifik'in derin araştırma istasyonlarında doğan, bilimi bir yemin gibi taşıyan topluluktur. Kemosentezi ilk kez gören dalışların defterlerini kutsal metin gibi saklar, her ölçümü bir dua sabrıyla tekrarlarlar. Şifa, gözlem ve sabır onların silahıdır; gerçek canlılara dokunmazlar, yalnızca kaydederler. Dr. Lyra Moss tarikatın hekimidir.<!--/v-->

**<!--v:veri.kokenler.kokenler.Deepborn.ad-->Derindoğanlar<!--/v-->.**
<!--v:veri.kokenler.kokenler.Deepborn.lore-->Üslerde doğan üçüncü kuşaktır; hiçbiri güneşi görmedi. Gözleri biyoışıldamaya, kulakları sonarın tıkırtısına alışıktır. Derinin kurallarını en iyi onlar bilir: Uçurum Sürüngenleri avlanır, gerçek canlılara dokunulmaz. Avcıdırlar, iz sürerler, susmayı bilirler. Kael bu kuşağın en iyi iz sürücüsüdür.<!--/v-->

**<!--v:veri.kokenler.kokenler.ForgottenOnes.ad-->Unutulanlar<!--/v-->.**
<!--v:veri.kokenler.kokenler.ForgottenOnes.lore-->Kubbeler mühürlendiğinde dışarıda kalanlardır. Kimse onları kurtarmaya gelmedi; onlar da enkazdan bir hayat örmeyi öğrendi. Pilot, hurdacı ve kaçakçıdırlar; her batık gövde onlar için bir hazine, her kapalı kapı bir bilmecedir. Juno Vega, akıntıları ezbere bilen bir pilottur ve hiçbir konvoya borcu yoktur.<!--/v-->

**<!--v:veri.kokenler.kokenler.AbyssCult.ad-->Uçurum Tarikatı<!--/v-->.**
<!--v:veri.kokenler.kokenler.AbyssCult.lore-->Çukurun dibinden gelen düzenli titreşimi bir çağrı olarak duyanlardır. Hadalit'in yalnızca karanlıkta yanan ışığını kutsal sayar, kadim makinelerin dilini çözmeye çalışırlar. Başkaları Çekirdek'ten korkar; onlar ona doğru yürür. Sable, kadim kabukları söküp yeniden kurabilen tek mühendistir; ne bulduğunu kimseye tam anlatmaz.<!--/v-->

### 3.4 Özel birlikler

Her köken bir asker türünün **T8–T10** kademelerini kendi birliğiyle değiştirir. Bedel, eğitim süresi
ve güç standart birlikle aynıdır; farklar standarda göre küçük yüzdelerdir (asker değerleri
[07](07_birlikler_ve_savas.md)). Kademe açılışı Kronik'e bağlıdır (T8 aşama 6, T9 aşama 8, T10 aşama 10).

<!-- URET:tablo:kokenler.birlikler -->
| Köken | Özel birlik (T8–T10) | Yerini aldığı tür | Fark (standarda göre) |
| --- | --- | --- | --- |
| Atlantik Birliği | Konvoy Muhafızı | Zırhlı Dalgıç | +%4 can, −%3 hız; üs ve garnizon savunmasında +%3 savunma |
| Pasifik Tarikatı | Biyoark Askeri | Ark Askeri | Savaşın ilk 3 turunda düşman saldırısı −%3; −%2 can |
| Derindoğanlar | Dip Avcısı | Zıpkıncı | +%3 saldırı, PvE'de +%5 hasar; −%2 savunma |
| Unutulanlar | Hurda Delicisi | Delici | +%15 taşıma, geçit ve yapılara +%5 hasar; −%2 saldırı |
| Uçurum Tarikatı | Rezonans Müridi | Ark Askeri | +%5 saldırı, Kadim Muhafız ve Leviathan'a +%5; −%4 can |
<!-- /URET:tablo:kokenler.birlikler -->

Pasifik Tarikatı ve Uçurum Tarikatı aynı türü (Ark Askeri) farklı yönlere çeker: biri savunmacı bir
zayıflatıcı, öteki saldırgan bir avcıdır. Delicinin tek özel birliği Unutulanlar'ındır; Zıpkıncının
Derindoğanlar'ın, Zırhlı Dalgıcın Atlantik Birliği'nin.

### 3.5 Mimari

Mimari tamamen görseldir: üs binalarının kaplaması ve haritadaki üs silueti. Binaların her 10 seviyede
değişen evrim katmanları ([04](04_bina_evrimi.md)) mimarinin malzemesiyle çizilir.

<!-- URET:tablo:kokenler.mimariler -->
| Köken | Mimari | Görünüm |
| --- | --- | --- |
| Atlantik Birliği | Konvoy Çeliği | Perçinli gri çelik plakalar, turuncu ikaz şeritleri, kalın lumbozlar, gövdeye boyanmış konvoy numaraları. |
| Pasifik Tarikatı | Mercan Laboratuvarı | Beyaz kompozit kubbeler, ince camgöbeği biyoışıldayan hatlar, cam seralar ve ölçüm direkleri. |
| Derindoğanlar | Bazalt Oyma | Yastık lav ve bazalt bloklara gömülü yapılar, karanlığa alışmış gözler için kırmızı düşük ışık, sintaktik köpük şamandıralar. |
| Unutulanlar | Hurda Örgü | Eski gövde parçalarından yamalı yapılar, açıkta kablo demetleri, sarı sodyum lambalar, elle boyanmış işaretler. |
| Uçurum Tarikatı | Kadim Kabuk | Hadalit damarlı biyomekanik kabuklar, organik kıvrımlar, karanlıkta yavaşça nabız atan camgöbeği parıltı. |
<!-- /URET:tablo:kokenler.mimariler -->

### 3.6 Köken değişimi

| Kural | Değer |
|---|---|
| Ücretsiz değişim | <!--v:veri.kokenler.degisim.ucretsiz-->İlk 7 gün ve Reaktör ≤ 15 iken 1 kez<!--/v--> |
| Bedel | <!--v:veri.kokenler.degisim.bedel-->3.000<!--/v--> Hadalit |
| Bekleme | <!--v:veri.kokenler.degisim.bekleme_gun-->30<!--/v--> gün |
| Koşul | <!--v:veri.kokenler.degisim.kosul-->Tüm yürüyüşler evde; savaş bildirisi altında değil<!--/v--> |
| Özel birlikler | <!--v:veri.kokenler.degisim.birlik-->Yeni kökenin özel birliği aynı türdense var olan özel askerler ona dönüşür; değilse aynı türün standart T8–T10'una döner<!--/v--> |
| Kahraman | <!--v:veri.kokenler.degisim.kahraman-->Köken kahramanı kalır; yeni kökenin kahramanı verilmez<!--/v--> |
| Mimari | <!--v:veri.kokenler.degisim.mimari-->Yeni mimari açılır; eski mimari kozmetik olarak seçilebilir kalır<!--/v--> |

Köken değişimi bir "doğru seçimi bulma" hakkıdır, bir meta takip aracı değildir: 30 günlük bekleme,
her güncellemede köken değiştirmeyi anlamsız kılar.

---

## 4. Kaptan profili

### 4.1 Profil kartı

Haritada bir üsse ya da ittifak listesinde bir ada dokununca açılan kart:

- Ad, ittifak etiketi ve rütbesi, köken amblemi, **unvan** (güç unvanı ya da seçilen başarım unvanı),
  Egemen unvanı varsa mührü.
- Toplam güç ve reaktör seviyesi; çevrimiçi noktası ve son görülme.
- Sunucu (çukur) ve göç geçmişi (kaç çukur gördüğü).
- **Görünmeyenler:** Derinlik İzni (VIP) seviyesi varsayılan olarak gizlidir; kaptan isterse yalnızca
  "İzin 8 · 8.650 m" biçiminde gösterir ([11 · VIP](11_vip.md)). Harcama ve The Syndicate geçmişi hiçbir
  zaman görünmez. Profil sakin lüksün en görünür yeridir: kimse cüzdanıyla tanınmaz.
- **Ad değişikliği:** ilki ücretsiz, sonra 500 Hadalit; 30 günde bir. Eski adlar profilde "eski adıyla"
  satırında son üç ad olarak durur (dolandırıcılığa karşı).

### 4.2 Güç dökümü

Toplam güç = binalar + araştırma + birlikler + kahramanlar (K-14; formüller [02](02_tempo_ve_denge.md) §9).
Profil bunu dört parçalı yatay bir çubukla gösterir; dokununca her parçanın en büyük beş katkısı
listelenir (ör. "Çekirdek Reaktör 30: 7.000"). Başka kaptanın profilinde yalnızca dört toplam görünür,
ayrıntı görünmez.

### 4.3 İstatistikler

<!-- URET:tablo:kronik.istatistikler -->
| Grup | Profilde görünen |
| --- | --- |
| Güç | Toplam güç; binalar, araştırma, birlikler, kahramanlar dökümü (K-14) |
| Savaş | Saldırı ve savunma zaferleri/yenilgileri; yenilen ve kaybedilen asker (yaralı/ölü ayrı); iyileştirilen asker; Savaş Puanı |
| Harita | PvE zaferleri (düşman türüne göre); Leviathan yuvaları; toplanan yük; açılan kare; kurtarılan buluntular |
| İttifak | Yardım, bağış, garnizon saati, ele geçirilen geçit ve alan |
| Bilim | Tür Kataloğu yüzdesi, kalıntı setleri |
| Kronik | Katıldığı aşamalar, sunucu ilkleri (Kronik Kaydı'nda adı geçenler) |
<!-- /URET:tablo:kronik.istatistikler -->

### 4.4 Başarımlar

Başarımlar üç kademelidir (I · II · III); her kademe küçük bir Hadalit ve profil rozeti verir, III.
kademe çoğunlukla bir unvan açar.

<!-- URET:tablo:kronik.basarimlar -->
| Kategori | Başarım | Ölçü | Kademeler (I · II · III) | Unvan (III) |
| --- | --- | --- | --- | --- |
| Keşif | Sonar Gözü | Sisten açılan kare | 1.000 · 10.000 · 30.000 | Haritacı |
| Keşif | Batık Avcısı | Kurtarılan batık enkaz ve kriyo-kapsül | 10 · 50 · 150 | — |
| Bilim | Katalogcu | Tür Kataloğu kaydı | 10 · 25 · tümü | Katalogcu |
| Ekonomi | Derin Hasat | Haritada toplanan yük | 1 M · 50 M · 1 milyar | Hasatçı |
| Ekonomi | Dökümcü | Elde edilen Süperalaşım | 1.000 · 10.000 · 50.000 | — |
| Savaş | Sürü Kıran | PvE zaferi | 100 · 1.000 · 10.000 | — |
| Savaş | Leviathan Avcısı | Yenilen Leviathan yuvası | 10 · 100 · 500 | Leviathan Avcısı |
| Savaş | Kalkan Duvarı | Kazanılan savunma | 10 · 100 · 1.000 | — |
| İttifak | Uzanan El | Yapılan yardım | 500 · 5.000 · 50.000 | — |
| İttifak | Bağışçı | Teknoloji bağışı | 500 · 5.000 · 20.000 | — |
| Bölge | Kilit Bekçisi | Geçit garnizonunda geçen saat | 24 · 240 · 1.000 | Kilit Bekçisi |
| Kronik | Kronik Tanığı | Aynı sunucuda aşama 1'den 10'a | Tek | Kronik Tanığı |
| Sezon | Akıntı Gazisi | Katılınan Akıntı Savaşı sezonu | 1 · 3 · 10 | Akıntı Gazisi |
| Egemenlik | Taç | Egemen olunan dönem | 1 | Eski Egemen |
<!-- /URET:tablo:kronik.basarimlar -->

### 4.5 Unvanlar

Kaptan bir unvan **seçer**; profil kartında ve haritadaki levhada görünür. Üç kaynak vardır:

1. **Güç unvanları:** toplam güce bağlı deniz rütbeleri. Eşik geçilince açılır, güç düşse de kalır.

<!-- URET:tablo:kronik.guc_unvanlari -->
| Unvan | Toplam güç | Yaklaşık |
| --- | ---: | --- |
| Asteğmen | 0 | Başlangıç |
| Teğmen | 50.000 | ~R15 |
| Yüzbaşı | 200.000 | ~R20 |
| Binbaşı | 600.000 | ~R25 |
| Yarbay | 1.500.000 | ~R30 |
| Albay | 3.000.000 | ~R35 |
| Tuğamiral | 5.000.000 | ~R40 |
| Koramiral | 8.000.000 | ~R45 |
| Oramiral | 12.000.000 | ~R50 |
<!-- /URET:tablo:kronik.guc_unvanlari -->

2. **Başarım unvanları:** §4.4'teki III. kademeler (Haritacı, Katalogcu, Leviathan Avcısı, Kilit
   Bekçisi, Kronik Tanığı, Akıntı Gazisi, Eski Egemen…).
3. **Egemen unvanları:** Egemen'in verdiği 24 saatlik iyi ya da kötü unvanlar
   ([09](09_dunya_haritasi_ve_pvp.md) §7.2). Bunlar seçilmez, kendiliğinden görünür; kötü unvan
   yalnızca küçük bir mühürle gösterilir.

Güç unvanları ittifak rütbelerinden (R5–R1) ayrıdır; adlar bilinçli olarak farklı seçilmiştir.

---

## 5. Göç

Göç, bir kaptanın bir çukur sunucusundan ötekine **Çukur Pasaportu** ile taşınmasıdır.

<!-- URET:tablo:kronik.goc -->
| Kural | Değer |
| --- | --- |
| Reaktör | En az 15 |
| İttifak | Göçten önce ayrılmış olmalı (savaştaki ittifaktan ayrılınca 24 saat bekleme) |
| Durum | Tüm yürüyüşler evde; üs su almıyor; gelen saldırı yok |
| Pasaport | 1 + ⌊güç / 2.000.000⌋, en çok 10 Çukur Pasaportu |
| Hedef sunucu | Açılışından en az 30 gün geçmiş; Akıntı Savaşı sezonunda değil |
| Güç sınırı | Kaptan gücü ≤ hedefin ilk 100 kaptanının ortalaması × 1,2 |
| Kota | Hedef sunucu haftada en çok 20 göçmen alır (Overseer ayarı) |
| Kaynak | Basınç Ambarı korumasına kadar taşınır; fazlasının %25'i çantaya Kaynak Kapsülü olur |
| Bekleme | İki göç arası 30 gün |
| İniş | Hedefin Hadal Eşiği'nde rastgele boş kare; 24 saat Toparlanma Kalkanı |
| Kronik | Kişisel kronik ödülleri korunur; hedef sunucunun aşaması geçerli olur (kilitli kademe eğitilemez, var olan askerler kalır) |
| Düşenler | Egemen unvanı, Rezonans etkileri, İttifak Ambarı bölmesi (24 saat sonra postayla iade) |
<!-- /URET:tablo:kronik.goc -->

- **Neden bu kadar kural?** Göç iki şeyi kolaylaştırmalı: arkadaşa katılmak ve ölü bir sunucudan
  kurtulmak. İki şeyi zorlaştırmalı: yeni sunucuları ezmek ve kaynak taşıyıp yağmalamak. Güç sınırı ve
  kota birinciyi, kaynak sınırı ikinciyi önler.
- Pasaport kaynakları: Kronik aşama 7'nin kişisel ödülü (1 adet), Akıntı Savaşı dükkânı, etkinlikler
  ve The Syndicate ([12](12_syndicate.md)). Ücretsiz oyuncu, sunucusu ölürse göç edebilmelidir:
  sunucu birleşmesi yapılmayan ölü sunuculara Overseer **ücretsiz göç penceresi** açar.
- **Çukur birleşmesi** (iki sunucunun tek sunucuda toplanması) göçün toplu hâlidir; kuralları açık
  sorudur.

---

## 6. Ücretsiz oyuncu ve ödeyen oyuncu

| Konu | Ücretsiz oyuncu | Ödeyen oyuncu |
|---|---|---|
| Kronik | Kapılar F2P temposundan önce açılır; hiçbir kademe onu bekletmez | Hızlı büyür, ama aynı kapıları bekler |
| Kronik ödülleri | Aynı kişisel ödüller; aşama 10'da güvenceli Kadim Şema | Aynı |
| Köken | İlk değişim ücretsiz | Sonraki değişimler Hadalit ve 30 gün bekleme |
| Profil | Emeği gösterilir (yardım, katalog, garnizon) | Harcaması gösterilmez |
| Göç | Ölü sunucuda ücretsiz göç penceresi | Pasaport alabilir; güç sınırı ve kota aynı |

---

## 7. Arayüz notları (sakin lüks)

- Köken seçimi, açılıştaki sonar karanlığında beş ince ışık olarak gelir; her ışığa dokununca kökenin
  sözü, mimarisinin silueti ve kahramanının portresi belirir. Sayılar küçük, en altta.
- Kronik Feneri ekranı bir kayıt defteri gibidir: serif başlık, ince çizgiler, geçmiş aşamalarda
  sunucu ilklerinin adları. Kutlama animasyonu yoktur; aşama açıldığında fenerin ışığı bir kez
  yavaşça yanar ve sessiz bir bildirim düşer.
- Profil kartında güç çubuğu dört renkli tek bir şerittir; başarımlar küçük mühürlerdir.
- Göç ekranı her kuralı bir kontrol listesi gibi gösterir (✔ / ✕); bütün kurallar sağlanmadan
  "Göç et" düğmesi görünmez. Kaybedilecek kaynak miktarı açıkça yazar.

---

## 8. Uç durumlar

- **Kronik ve yeni katılan oyuncu.** 200. günde katılan oyuncu aşama 10'daki bir sunucuya düşer:
  T10 kapısı açıktır ama Tatbikat Havuzu ve araştırma onu yine bekletir. Geçmiş aşamaların kişisel
  hedefleri açıktır; yeni oyuncu bir "yetişme" ödül yolu olarak onları toplar.
- **Göçmen ve Kronik.** Aşama 10'daki sunucudan aşama 6'daki sunucuya göçen kaptanın T9–T10 askerleri
  kalır ama yenileri eğitilemez; hedef sunucunun kuşak ve geçit kuralları geçerlidir.
- **Sunucu hedefi hiç tamamlanmazsa** aşama en geç günde açılır; Kronik Kaydı'na "kendiliğinden açıldı"
  yazılır, kimse cezalandırılmaz.
- **Köken değişimi ve ittifak savaşı.** Savaş bildirisi altındaki ittifakın üyesi köken değiştiremez;
  savaşın ortasında birlik dönüştürmek yasaktır.
- **Köken kahramanı zaten varsa** (ör. Atlantik Birliği'nin `mara`'sı): kart yerine o kahramanın
  yıldız parçası verilir; başlangıç yıldızı kuralı yine uygulanır.
- **Unvan çakışması.** Egemen kötü unvan verdiğinde kaptanın seçtiği unvan 24 saat ikinci sıraya geçer.

---

## 9. Bugün kodda

| Özellik | Kodda | Hedef | Durum |
|---|---|---|---|
| Derinlik Kroniği | Yok | 12 aşama, sunucu takvimi | ⬜ |
| Kronik Feneri | Yok (bina katalogda değil) | Reaktör 8 binası | ⬜ |
| Köken | Fraksiyonlar yalnızca kahraman etiketi (`faction.*` çevirileri) | 5 köken, seçim, pasifler, birlik, mimari | 🟡 |
| Kaptan profili | Haritada üs kartı: ad, ittifak, seviye, güç, koordinat, çevrimiçi | Güç dökümü, istatistik, başarım, unvan | 🟡 |
| Güç | `Σ bina seviyesi × 10 + Σ kahraman gücü` | K-14 dört parçalı güç | 🟡 |
| Göç | Yok | Pasaport ve kurallar | ⬜ |

---

## 10. Uygulama notları

| Değişiklik | Kod |
|---|---|
| Kronik durumu | Sunucu düzeyinde `ChronicleService` (`server/Hadal.Game/World/` yanında): açılış günü, şimdiki aşama, sunucu hedefi sayaçları; tablo `hadal.chronicle` (aşama, açılış zamanı, ilkler). Aşama değişimi `WorldService`'e kuşak/geçit açılışı, kahraman ve asker kurallarına kademe kapısı olarak yayınlanır |
| Kapılar | `shared/HADAL.Shared/Simulation`: `ChronicleRules.IsUnlocked(stage, feature)`; Tatbikat Havuzu kademe kontrolü ve `WorldRules` kuşak kontrolü bunu çağırır |
| Katalog | `catalog.json` → `chronicle.stages[]` (en erken/en geç gün, açılanlar, hedefler), `origins[]` (pasifler, kahraman, birlik varyantı, mimari), `profile.titles[]`, `achievements[]`, `migration` |
| Köken | `PlayerSession`'a `Origin`; pasifler `PlayerModifiers`'a araştırma etkileri gibi eklenir (bugünkü `HeroRules.AddCrew` deseni); seçim yeni hesapta ilk komut `ChooseOrigin` |
| Profil | `WorldBaseDto` → `Title`, `Origin`, güç dökümü yalnızca kendi profilinde (`ProfileDto`); istatistik sayaçları `RecordEvent` altyapısıyla (bugün `help` sayacı var) |
| Göç | Gateway düzeyinde hesap taşıma: kaynak sunucuda kayıt kapatılır, hedefte `hadal.world_bases`'e Eşik yerleşimi; pasaport ve kurallar sunucu tarafında doğrulanır |
| Overseer | Kronik aşamasını izleme, gerekirse aşama açılışını erteleme (bakım), göç kotası ve ücretsiz göç penceresi |

---

## 11. Esin kaynakları

Türün krallık oyunlarından alınan **yapılar**: bölgeleri sırayla açan, krallık/ittifak/kişisel
hedefleri olan ortak bir zaman çizelgesi ve onu gösteren bir anıt binası; başlangıç komutanı, kendi
birliği, küçük pasifleri ve kendi mimarisi olan uygarlık seçimi ve ücretli değişimi; güç dökümü,
başarımlar ve güce bağlı unvanlarla vali profili; krallıklar arası pasaportlu göç. Hadal'ın farkı: kapılar
ücretsiz oyuncunun temposuna göre ayarlanır, kökenler eşit başlangıç değerine bağlanır, profil harcamayı
göstermez, sunucular gerçek çukurlardır.

---

## 12. Açık sorular

- **Akıntı Savaşı'nın ilk sezonu** aşama 7'ye (90. gün) bağlandı; [13](13_etkinlikler_ve_sezonlar.md)
  ilk sezonun gününü farklı belirlerse aşama günü ona uymalı.
- **Hadal Titanı** aşama 11'de (240. gün) uyanıyor; [14](14_son_oyun.md)'teki Titan döngüsü ve
  Reaktör 50 sonrası (Çekirdek Rezonansı) ile eşlenmeli.
- **Köken kahramanı:** `mara` ve `lyra` hikâyede de geliyor (bölüm 1); Atlantik Birliği ve Pasifik
  Tarikatı için köken kahramanı yerine aynı fraksiyondan ikinci bir kahraman (ör. `tess`, `orin`)
  daha anlamlı olabilir. [05](05_kahramanlar.md) ile birlikte karar verilmeli.
- **Özel birlik farkları** (±%2–5) [07](07_birlikler_ve_savas.md)'deki asker istatistikleriyle
  dengelenmeli; Ark Askeri'nin iki kökende özel birliği var.
- **Güç unvanı eşikleri** referans üs gücüne ([02](02_tempo_ve_denge.md) §9) asker ve araştırma gücü
  eklenince yeniden ayarlanmalı.
- **Çukur birleşmesi:** iki sunucunun Kronik aşaması farklıysa hangisi geçerli olur? Öneri: geride
  olanın aşaması, öndekinin Kronik Kaydı korunur.
- **Köken değişimi bedeli:** bu bölüm 3.000 Hadalit (ilk değişim ücretsiz, 30 gün bekleme) önerir;
  [12](12_syndicate.md)'nin Hadalit tablosunda 5.000 (öneri) yazıyor. Karar bu bölümündür; 12 güncellenmeli.
- **Göç güç sınırı** (hedefin ilk 100 ortalaması × 1,2) küçük sunucularda çok dar olabilir; alt sınır
  olarak "Reaktör 30 karşılığı güç" eklenebilir.
