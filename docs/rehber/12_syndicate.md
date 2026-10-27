# 12 · The Syndicate

The Syndicate, Hadal'ın gerçek parayla alışveriş yaptığı tek yerdir (K-06). Adı her yerde budur;
"Mağaza", "Market", "Store" denmez. Derin denizin karanlığında kimsenin sorgulamadığı bir tedarik ağı:
sessiz, kibar, ısrarsız. Ne alacağını sen seçersin; The Syndicate seni çağırmaz.

Bu bölüm Hadalit fiyatlarını, aylık kartı (**Sendika Sözleşmesi**), sezon biletini (**Derin Akıntı
Bileti**), büyüme fonunu (**Reaktör Fonu**), paket serilerini, haftalık Süperalaşım paketlerini,
ittifak hediyelerini, **hızlandırıcıları** ve Hadalit'in nereye harcandığını tanımlar. Kalıcı VIP
sistemi [11 · Derinlik İzni](11_vip.md)'dir. Bağlı kanon: K-04 (oranlar ve garantiler), K-06 (ad),
K-07 (3. inşaatçı yalnız Sözleşme'yle), K-12 (Hadalit ×10, tavansız), K-13 (hız formülü)
([00 · Kanon](00_kanon.md)). Tempo varsayımları: [02 · Tempo ve Denge](02_tempo_ve_denge.md),
[Tempo simülasyonu](uretilen/simulasyon.md). Sayıların kaynağı `veri/syndicate.json` ve
`veri/hizlandiricilar.json`'dır.

---

## Amaç

1. **Oyunun parasını kazanmak, oyuncunun güvenini kaybetmeden.** Ürünler değerini kendi içeriğiyle
   anlatır; aciliyet, kıtlık ve gösteriş dili kullanılmaz (Quiet Luxury).
2. **Üç profili taşımak.** Ücretsiz oyuncu Reaktör 50'ye ~12 ayda, aylık kart oyuncusu ~9 ayda, balina
   ~5 ayda varır. Bu bölümdeki hızlandırma ve Süperalaşım akışı simülasyonun varsayımlarıyla
   ([Tempo simülasyonu](uretilen/simulasyon.md)) birebir hesaplanmıştır.
3. **En iyi değeri sadakate vermek.** Dolar başına en çok değer sınırlı, düzenli ürünlerdedir
   (Sözleşme, Bilet, Fon); sınırsız Hadalit en pahalı yoldur. "Daha çok al, daha ucuza al" merdiveni yoktur.
4. **Balina harcamasını ittifaka akıtmak.** Her satın alma ittifak üyelerine sessiz bir hediye bırakır.

---

## Kurallar

- **Tek kapı.** Bütün ücretli ürünler The Syndicate'tedir. Oyunun başka hiçbir ekranı (sefer dönüşü,
  yenilgi, Kriyo-Kapsül, giriş) satın alma penceresi açmaz. Tetiklenen teklifler yalnız The Syndicate
  düğmesinde **sessiz bir nokta** olarak görünür.
- **Fiyat.** Fiyatlar mağazanın ABD doları kademeleridir (0,99 $ … 99,99 $). Oyuncu her zaman
  mağazanın yerel fiyatını görür; içerik bütün ülkelerde aynıdır.
- **Hadalit ölçeği.** K-12: Hadalit ×10, cüzdan tavanı yoktur. Kriyo-Kapsül tekli
  <!--v:veri.syndicate.kriyo.tekli-->200<!--/v--> HD, onluk <!--v:veri.syndicate.kriyo.onluk-->1.800<!--/v--> HD.
- **Süre yok, geri sayım yok.** Hiçbir üründe geri sayım sayacı yoktur. Haftalık ürünler pazartesi, günlük
  ürünler sunucu gününde sessizce yenilenir; ekranda yalnız "pazartesi yenilenir" yazar.
- **Abonelik yok.** Sözleşme ve Bilet otomatik yenilenmez; her dönem ayrı satın alınır.
- **İzin puanı.** Her satın alma dolar başına 100 Derinlik İzni puanı verir ([11](11_vip.md)).
- **İttifak.** Her satın alma, alıcının ittifakındaki herkese sessiz bir hediye bırakır ([10 · İttifak](10_ittifak.md)).
- **Süperalaşım** The Syndicate'te Hadalit'le satılmaz; yalnız Nodül Sandıkları, Bilet ve Sözleşme payından
  gelir. Böylece balinanın Süperalaşım geliri ücretsiz oyuncunun ×5'ini aşmaz.
- **Eksik kaynak satılmaz.** İnşaat penceresinde "Hadalit'le tamamla" ya da "satın al" düğmesi yoktur;
  eksik kaynakta "Nereden bulunur?" listesi açılır ([15 · Ekonomi Akışı](15_ekonomi_akisi.md)).
- **Satılmayanlar.** Uyanış Özü, Efsanevi kahraman kartı, Efsanevi garanti sayacını atlatan herhangi bir
  şey ve rastgele içerikli "gizemli" kahraman kutusu hiçbir üründe yoktur ([05 · Kahramanlar](05_kahramanlar.md)).
  Paketlerdeki kahraman kartları seçmeli ya da adlıdır; rastgelelik yalnız oranları açık Kriyo bileti ile gelir.
- **Değer ölçeği iç kullanım içindir.** Oyuncuya "değer", "×8", "%300" gösterilmez; yalnız içerik listesi.

---

## Ürünler

### Değer ölçeği

Ürünleri karşılaştırmak için her kalem Hadalit karşılığına çevrilir. 3. inşaatçı, hız yüzdesi, İzin
puanı ve kozmetik sayılmaz; tablolar bu yüzden temkinlidir. "× taban", dolar başına değerin 0,99 $
Hadalit kademesine (~50 HD / $) oranıdır.

<!-- URET:tablo:syndicate.deger_olcegi -->
| Birim | HD değeri | Gerekçe |
| --- | ---: | --- |
| 1 Hadalit | 1 | Tanım |
| 1 saat genel hızlandırma | 120 | Şimdi bitir fiyatı: 2 HD / dk |
| 1 saat tipli hızlandırma (inşaat, araştırma, eğitim, iyileştirme) | 90 | Tek kuyrukta geçer; genelin %75'i |
| 1 GS kaynak kapsülü (Titanyum + 0,4 × Biyokütle) | 60 | Varsayım: 1 dakikalık gelir = 1 HD, 1 dakikalık inşaatın yarısı |
| 1 Kriyo-Kapsül bileti | 180 | Onluk çekimin çekim başı fiyatı |
| 1 Nadir kahraman kartı | 400 | Varsayım |
| 1 Destansı kahraman kartı (seçmeli) | 1.500 | İzin dükkânı fiyatı |
| 1 Süperalaşım günü | 600 | Varsayım; The Syndicate'te Süperalaşım Hadalit'le satılmaz |
| 1 Anı Kristali | 20 | Varsayım; her Kriyo çekimi 1 kristal verir |
<!-- /URET:tablo:syndicate.deger_olcegi -->

**GS (Gelir Saati)**, [15 · Ekonomi Akışı](15_ekonomi_akisi.md)'nın birimidir: alıcının reaktör seviyesindeki
ücretsiz oyuncu saatlik Titanyum gelirinin bir saati ve 0,4 katı Biyokütle; çantaya damgalı **Kaynak
Kapsülü** olarak düşer. Paketler böylece her seviyede aynı ağırlığı taşır. **Süperalaşım günü**, alıcının
Dökümhane haftasındaki ücretsiz oyuncu günlük Süperalaşım gelirinin bir günüdür.

### Hadalit kademeleri

<!-- URET:tablo:syndicate.hadalit -->
| Fiyat | Hadalit | İlk alımda ek | HD / $ | Çekim başına $ (onluk) | Çekim başına $ (tekli) | İzin puanı |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0,99 $ | 50 | 50 | 50,5 | 3,56 | 3,96 | 100 |
| 4,99 $ | 270 | 270 | 54,1 | 3,33 | 3,7 | 500 |
| 9,99 $ | 560 | 560 | 56,1 | 3,21 | 3,57 | 1.000 |
| 19,99 $ | 1.200 | 1.200 | 60 | 3 | 3,33 | 2.000 |
| 49,99 $ | 3.200 | 3.200 | 64 | 2,81 | 3,12 | 5.000 |
| 99,99 $ | 7.000 | 7.000 | 70 | 2,57 | 2,86 | 10.000 |
<!-- /URET:tablo:syndicate.hadalit -->

- En iyi düzenli oranda (<!--v:veri.syndicate.hadalit.hd_9999.hd_dolar-->70<!--/v--> HD / $) bir Kriyo çekimi
  <!--v:veri.syndicate.hadalit.hd_9999.cekim_onlu|para-->2,57 $<!--/v--> (onluk) ile
  <!--v:veri.syndicate.hadalit.hd_9999.cekim_tekli|para-->2,86 $<!--/v--> (tekli) arasıdır.
- **İlk alım:** her kademenin ilk alımı iki kat Hadalit verir (hesap başına kademe başına bir kez).
  Ekranda kademenin altında küçük harfle "İlk alımda +270 HD" yazar; rozet, şerit, üstü çizili fiyat yoktur.
- Kademeler oyuncuyu gereğinden fazla Hadalit almaya zorlamaz: her kartta "bu miktarla 3 çekim" gibi
  bir yardımcı satır vardır ve eksik tutar için en küçük yeterli kademe önerilir.

### Kuruluş Hediyesi

Hesabın **ilk** satın alması (hangi ürün olursa olsun, 0,99 $ dahil) bir kez şu hediyeyi açar:

<!-- URET:tablo:syndicate.kurulus -->
| Kalem | Miktar |
| --- | --- |
| Destansı kahraman kartı | 1 (Kuruluş havuzundaki 3 kahramandan seçmeli) |
| Hadalit | 500 |
| Genel hızlandırma | 5 × 1 sa |
| Titanyum Kapsülü | 20 GS |
| Derinlik İzni puanı | 1.000 |
| Profil çerçevesi | Kurucu (kozmetik) |
<!-- /URET:tablo:syndicate.kurulus -->

Kuruluş Hediyesi ayrı bir ürün değildir, satın almanın yanında gelir. Seçmeli Destansı kahramanlar
[05 · Kahramanlar](05_kahramanlar.md)'daki Kuruluş havuzundandır.

### Sendika Sözleşmesi

Aylık kart. <!--v:veri.syndicate.sozlesme.fiyat|para-->4,99 $<!--/v-->, <!--v:veri.syndicate.sozlesme.gun-->30<!--/v--> gün.
K-07'nin tanımladığı **3. inşaatçı** yalnız Sözleşme süresince açıktır.

<!-- URET:tablo:syndicate.sozlesme_icerik -->
| Kalem | Miktar | 30 günde |
| --- | --- | --- |
| Hadalit (hemen) | 300 | 300 |
| Hadalit (her gün) | 100 | 3.000 |
| 3. inşaatçı | Sözleşme boyunca | 30 gün |
| İnşa ve araştırma hızı | +%5 | 30 gün |
| İnşaat hızlandırma (her gün) | 1 × 1 sa | 30 sa |
| Genel hızlandırma (her gün) | 2 × 15 dk | 15 sa |
| Titanyum Kapsülü (her gün) | 1 GS | 30 GS |
| Süperalaşım payı (Reaktör 30'dan sonra, her gün) | 0,1 Süperalaşım günü | 3 Süperalaşım günü |
| Derinlik İzni giriş eki | +150 / giriş günü | ≈4.500 |
<!-- /URET:tablo:syndicate.sozlesme_icerik -->

- **Teslim:** günlük pay her gün sunucu tarafından posta kutusuna yatırılır; girmesen de kaybolmaz
  (posta 14 gün saklanır, [13](13_etkinlikler_ve_sezonlar.md)'ün genel kuralı). Günlük "topla" baskısı
  yoktur. Giriş eki ise yalnız girilen günlerde işler.
- **Uzatma:** Sözleşme sürerken yeniden alınabilir; kalan süreye 30 gün eklenir, en çok
  <!--v:veri.syndicate.sozlesme.uzatma_sinir_gun-->60<!--/v--> güne kadar.
- **Bitiş:** sona ermeden 3 gün önce posta kutusuna tek bir not düşer ("Sözleşmen 27 Kasım'da
  tamamlanıyor"). Bildirim, geri sayım ya da ana ekran uyarısı yoktur. Bittiğinde 3. inşaatçı elindeki
  işi bitirir, yenisini almaz.
- **İzin ile ilişkisi:** Sözleşme İzin seviyesini değiştirmez; kendi +%<!--v:veri.syndicate.sozlesme.hiz-->5<!--/v--> hızını ve
  giriş ekini taşır. Aylık kart profilinin "+%15"i İzin 8'in %12'si ile bu %5'in toplamına yakındır
  ([11 · Simülasyon tutarlılığı](11_vip.md#simülasyon-tutarlılığı)).

### Derin Akıntı Bileti

Sezon bileti. Bir sezon <!--v:veri.syndicate.bilet.hafta-->8<!--/v--> hafta, <!--v:veri.syndicate.bilet.seviye-->50<!--/v--> seviye;
sezon takvimi [13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md)'dadır. Üç yol vardır:
**Ücretsiz**, **Akıntı** (<!--v:veri.syndicate.bilet.akinti|para-->9,99 $<!--/v-->) ve **Derin Akıntı**
(<!--v:veri.syndicate.bilet.derin|para-->19,99 $<!--/v-->; Akıntı'yı içerir, Akıntı sahibi
<!--v:veri.syndicate.bilet.yukseltme|para-->9,99 $<!--/v-->'a yükseltir).

- **Bilet puanı:** her seviye <!--v:veri.syndicate.bilet.xp_seviye-->500<!--/v--> puan. Günlük görev puanı (K-09) bilet puanı
  olarak da sayılır; haftalık bilet görevleri ve etkinlikler eklenince düzenli oyuncu haftada
  ~<!--v:veri.syndicate.bilet.xp_hafta-->3.650<!--/v--> puan toplar ve 50. seviyeye 7. haftada varır. Son hafta boşluk payıdır.
- **Geç alım:** ücretli yol sezonun herhangi bir gününde alınabilir; geçilmiş seviyelerin ödülleri hemen açılır.
- **Seviye atlama:** <!--v:veri.syndicate.bilet.atlama_hd-->200<!--/v--> HD / seviye, sezonda en çok
  <!--v:veri.syndicate.bilet.atlama_sinir-->10<!--/v--> seviye. Atlama yalnız geride kalanlar içindir; sezon başında satılmaz.
- **Reaktör 30 öncesi** Süperalaşım günleri aynı değerde GS kaynak kapsülüne çevrilir.

Yolların toplam ödülü (8 hafta):

<!-- URET:tablo:syndicate.bilet_yollari -->
| Yol | Fiyat | Hadalit | Genel + inşaat (sa) | Araştırma (sa) | Eğitim (sa) | Kriyo bileti | Kart | Gelir saati | Süperalaşım günü | Kozmetik | İzin puanı | HD değeri / $ |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: | --- | ---: | ---: |
| Ücretsiz | — | 600 | 9 | 5 | 5 | 2 | 1 Nadir | 40 | 1 | — | 150 | — |
| Akıntı | 9,99 $ | 1.500 | 24 | 10 | 8 | 6 | 1 Destansı (seçmeli) | 50 | 2 | Sezon denizaltı kaplaması | 1.000 | 1.231 |
| Derin Akıntı (Akıntı'ya ek) | 9,99 $ | 1.500 | 30 | 10 | 10 | 12 | 2 Destansı (seçmeli; biri 50. seviyede) | 60 | 2 | Sezon üs kaplaması | 1.000 | 1.628 |
<!-- /URET:tablo:syndicate.bilet_yollari -->

Seviye aralıklarının içeriği:

<!-- URET:tablo:syndicate.bilet_basamaklari -->
| Seviye | Ücretsiz | Akıntı | Derin Akıntı |
| --- | --- | --- | --- |
| 1–10 | Kaynak, kısa hızlandırmalar | Hadalit, inşaat hızlandırmaları, denizaltı kaplaması (1) | Hadalit, genel hızlandırmalar |
| 11–20 | Kriyo bileti (15) | Kriyo biletleri, araştırma hızlandırmaları | Kriyo biletleri, eğitim hızlandırmaları |
| 21–30 | Nadir kart (25), Hadalit | Destansı kart (25), gelir sandıkları | Gelir sandıkları, inşaat hızlandırmaları |
| 31–40 | Süperalaşım günü (35) | Süperalaşım günleri, Hadalit | Süperalaşım günleri, Hadalit |
| 41–50 | Kriyo bileti (50) | Hadalit, Kriyo biletleri | Destansı kart (50), üs kaplaması (50) |
<!-- /URET:tablo:syndicate.bilet_basamaklari -->

### Reaktör Fonu

Büyüme fonu. <!--v:veri.syndicate.fon.fiyat|para-->19,99 $<!--/v-->, hesap başına bir kez, Reaktör
<!--v:veri.syndicate.fon.acilis_reaktor-->6<!--/v-->'da görünür olur. Reaktör kilometre taşlarında toplam
<!--v:veri.syndicate.fon.toplam_hd-->18.000<!--/v--> HD öder. **Geç alınabilir:** fon hangi seviyede alınırsa alınsın
geçilmiş kilometre taşlarının ödemesi hemen yapılır. Ödeme, kilometre taşına ulaşıldığı an posta kutusuna düşer.

<!-- URET:tablo:syndicate.fon_odemeleri -->
| Reaktör | Ödeme (HD) | Toplam (HD) | Balina ulaşır | Aylık kart ulaşır | F2P temposunda |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 10 | 1.000 | 1.000 | 2 sa 24 dk | 4 sa 48 dk | 7 sa 12 dk |
| 15 | 1.000 | 2.000 | 7 sa 12 dk | 19 sa 12 dk | 1,4. gün |
| 20 | 1.500 | 3.500 | 1,1. gün | 2,7. gün | 4,5. gün |
| 25 | 1.500 | 5.000 | 3,7. gün | 8,3. gün | 13,4. gün |
| 30 | 2.000 | 7.000 | 10. gün | 19,9. gün | 28,6. gün |
| 33 | 1.500 | 8.500 | 16,8. gün | 32,5. gün | 50,1. gün |
| 36 | 1.500 | 10.000 | 26,6. gün | 49,9. gün | 79,2. gün |
| 40 | 2.000 | 12.000 | 45,9. gün | 84,5. gün | 128,4. gün |
| 43 | 1.500 | 13.500 | 68,2. gün | 125,2. gün | 184,5. gün |
| 46 | 1.500 | 15.000 | 98,5. gün | 180. gün | 255,8. gün |
| 50 | 3.000 | 18.000 | 147,1. gün | 267,2. gün | 368,1. gün |
<!-- /URET:tablo:syndicate.fon_odemeleri -->

### Seri paketler

Dört tema, her temada beş basamak (4,99 $ → 99,99 $). Basamaklar sırayla açılır, her basamak haftada
bir kez alınır, pazartesi yenilenir.

<!-- URET:tablo:syndicate.seriler -->
| Seri | İçerik | Açılış |
| --- | --- | --- |
| İnşaat Hattı | İnşaat hızlandırma, genel hızlandırma, Titanyum ağırlıklı gelir | Reaktör 6 |
| Laboratuvar Serisi | Araştırma hızlandırma, Biyokütle ağırlıklı gelir | Reaktör 6 |
| Filo Serisi | Eğitim ve iyileştirme hızlandırma, Gıda ağırlıklı gelir | Reaktör 8 |
| Kriyo Serisi | Kriyo-Kapsül biletleri, kahraman kartları | Reaktör 6 |
<!-- /URET:tablo:syndicate.seriler -->

**Üst basamak daha iyi fiyat vermez.** Dolar başına değer basamak yükseldikçe **azalır** (İnşaat Hattı
I'de <!--v:veri.syndicate.seri_basamaklari.0.dolar_basi-->445<!--/v--> → V'te <!--v:veri.syndicate.seri_basamaklari.4.dolar_basi-->249<!--/v-->
HD değeri / $). Bu bilinçli bir kararla tür alışkanlığının tersidir: merdiven, oyuncuyu "bir basamak daha"
diye yukarı itmez; ihtiyacı kadar alan oyuncu en iyi fiyatı alır.

<!-- URET:tablo:syndicate.seri_basamaklari -->
| Seri | Basamak | Fiyat | İçerik | HD değeri | HD değeri / $ | × taban |
| --- | --- | ---: | --- | ---: | ---: | ---: |
| İnşaat Hattı | I | 4,99 $ | 300 HD · 2 sa genel · 12 sa inşaat · 10 GS kaynak kapsülü | 2.220 | 445 | 8,9 |
| İnşaat Hattı | II | 9,99 $ | 500 HD · 3 sa genel · 24 sa inşaat · 16 GS kaynak kapsülü | 3.980 | 398 | 8 |
| İnşaat Hattı | III | 19,99 $ | 900 HD · 6 sa genel · 40 sa inşaat · 30 GS kaynak kapsülü | 7.020 | 351 | 7 |
| İnşaat Hattı | IV | 49,99 $ | 2.000 HD · 12 sa genel · 80 sa inşaat · 72 GS kaynak kapsülü | 14.960 | 299 | 6 |
| İnşaat Hattı | V | 99,99 $ | 4.000 HD · 24 sa genel · 120 sa inşaat · 120 GS kaynak kapsülü | 24.880 | 249 | 5 |
| Laboratuvar Serisi | I | 4,99 $ | 300 HD · 14 sa araştırma · 10 GS kaynak kapsülü | 2.160 | 433 | 8,7 |
| Laboratuvar Serisi | II | 9,99 $ | 500 HD · 28 sa araştırma · 16 GS kaynak kapsülü | 3.980 | 398 | 8 |
| Laboratuvar Serisi | III | 19,99 $ | 900 HD · 50 sa araştırma · 30 GS kaynak kapsülü | 7.200 | 360 | 7,2 |
| Laboratuvar Serisi | IV | 49,99 $ | 2.000 HD · 100 sa araştırma · 70 GS kaynak kapsülü | 15.200 | 304 | 6,1 |
| Laboratuvar Serisi | V | 99,99 $ | 4.000 HD · 160 sa araştırma · 110 GS kaynak kapsülü | 25.000 | 250 | 5 |
| Filo Serisi | I | 4,99 $ | 300 HD · 12 sa eğitim · 2 sa iyileştirme · 10 GS kaynak kapsülü | 2.160 | 433 | 8,7 |
| Filo Serisi | II | 9,99 $ | 500 HD · 24 sa eğitim · 4 sa iyileştirme · 16 GS kaynak kapsülü | 3.980 | 398 | 8 |
| Filo Serisi | III | 19,99 $ | 900 HD · 44 sa eğitim · 6 sa iyileştirme · 30 GS kaynak kapsülü | 7.200 | 360 | 7,2 |
| Filo Serisi | IV | 49,99 $ | 2.000 HD · 90 sa eğitim · 12 sa iyileştirme · 68 GS kaynak kapsülü | 15.260 | 305 | 6,1 |
| Filo Serisi | V | 99,99 $ | 4.000 HD · 140 sa eğitim · 20 sa iyileştirme · 110 GS kaynak kapsülü | 25.000 | 250 | 5 |
| Kriyo Serisi | I | 4,99 $ | 300 HD · 10 Kriyo bileti | 2.100 | 421 | 8,4 |
| Kriyo Serisi | II | 9,99 $ | 500 HD · 12 Kriyo bileti · 1 Destansı kart (seçmeli) | 4.160 | 416 | 8,3 |
| Kriyo Serisi | III | 19,99 $ | 900 HD · 25 Kriyo bileti · 1 Destansı kart (seçmeli) | 6.900 | 345 | 6,9 |
| Kriyo Serisi | IV | 49,99 $ | 2.000 HD · 54 Kriyo bileti · 2 Destansı kart (seçmeli) | 14.720 | 294 | 5,9 |
| Kriyo Serisi | V | 99,99 $ | 4.000 HD · 85 Kriyo bileti · 4 Destansı kart (seçmeli) | 25.300 | 253 | 5,1 |
<!-- /URET:tablo:syndicate.seri_basamaklari -->

### Anı Kristali kutuları ve Nesil Paketi

Anı Kristali kahraman yeteneklerinin ve Uyanış'ın malzemesidir ([05 · Kahramanlar](05_kahramanlar.md)).
Kriyo çekimleri ve etkinlikler verir; The Syndicate yalnız iki kutuyla, **ayda birer kez** satar.
**Nesil Paketi** yeni bir kahraman nesli geldiğinde bir kez açılır ve o neslin adı yazılı bir Destansı
kahramanını verir (Efsanevi değil; garanti sayacı korunur):

<!-- URET:tablo:syndicate.kristal_kutulari -->
| Ürün | Fiyat | İçerik | Sınır | HD değeri / $ | × taban |
| --- | ---: | --- | --- | ---: | ---: |
| Anı Kristali Kutusu I | 4,99 $ | 100 HD · 80 Anı Kristali | ayda 1 | 341 | 6,8 |
| Anı Kristali Kutusu II | 19,99 $ | 400 HD · 300 Anı Kristali | ayda 1 | 320 | 6,4 |
| Nesil Paketi | 19,99 $ | 1.200 HD · 15 Kriyo bileti · yeni neslin adlı bir Destansı kahramanının 20 kartı | nesil başına 1 | 345 | 6,9 |
<!-- /URET:tablo:syndicate.kristal_kutulari -->

### Günlük seçki

Her gün üç küçük teklif (0,99 $, 1,99 $, 4,99 $); her biri günde bir kez. İçerik gün gün döner
(hızlandırma, gelir, bilet ağırlıklı); tablo örnek bir gündür.

<!-- URET:tablo:syndicate.gunluk_secki -->
| Örnek | Fiyat | İçerik | HD değeri / $ | × taban |
| --- | ---: | --- | ---: | ---: |
| Vardiya Payı | 0,99 $ | 60 HD · 3 sa genel | 424 | 8,5 |
| İkmal Sandığı | 1,99 $ | 100 HD · 2 sa araştırma · 6 GS kaynak kapsülü · 1 Kriyo bileti | 412 | 8,2 |
| Derin İkmal | 4,99 $ | 250 HD · 8 sa inşaat · 4 sa araştırma · 10 GS kaynak kapsülü | 387 | 7,7 |
<!-- /URET:tablo:syndicate.gunluk_secki -->

### Nodül Sandıkları (haftalık Süperalaşım)

Süperalaşım geç oyunun kapısıdır ([02 · Tempo ve Denge](02_tempo_ve_denge.md)). Satın alınan Süperalaşım
bu yüzden **haftalık sınırlıdır**: üç sandık, her biri haftada bir, pazartesi yenilenir; Reaktör 30'da
(Alaşım Dökümhanesi) açılır.

<!-- URET:tablo:syndicate.nodul_paketleri -->
| Paket | Fiyat | İçerik | HD değeri / $ | × taban |
| --- | ---: | --- | ---: | ---: |
| Nodül Sandığı I | 4,99 $ | 100 HD · 3 Süperalaşım günü | 381 | 7,6 |
| Nodül Sandığı II | 14,99 $ | 200 HD · 7 Süperalaşım günü | 294 | 5,9 |
| Nodül Sandığı III | 29,99 $ | 400 HD · 9 Süperalaşım günü | 193 | 3,9 |
<!-- /URET:tablo:syndicate.nodul_paketleri -->

**Tavan hesabı.** Simülasyonun balina profili ücretsiz oyuncunun Süperalaşım gelirinin ×5'ini alır.
Bunun bir payı oyuncunun kendisinindir (ücretsiz oyuncu kadar: ×1), bir payı daha hızlı kurulan
Dökümhane, daha çok yürüyüş kuyruğu, İzin toplama bonusu ve etkinlik sıralamasından gelir (≈×1). Satın
alınabilecek pay en fazla ×<!--v:veri.syndicate.nodul.tavan_pay-->3<!--/v-->'tür:

| Satın alma yolu | Haftalık Süperalaşım günü | Ücretsiz oyuncunun haftalık gelirine oranı |
|---|---:|---:|
| Nodül Sandığı I + II + III | <!--v:veri.syndicate.nodul.haftalik_gun-->19<!--/v--> | <!--v:veri.syndicate.nodul.haftalik_pay-->2,71<!--/v--> |
| Derin Akıntı Bileti (iki ücretli yol, haftaya bölünmüş) | 0,5 | <!--v:veri.syndicate.nodul.bilet_pay-->0,07<!--/v--> |
| Sendika Sözleşmesi payı | 0,7 | <!--v:veri.syndicate.nodul.sozlesme_pay-->0,1<!--/v--> |
| **Toplam satın alınabilir** | | **<!--v:veri.syndicate.nodul.satin_alma_pay-->2,89<!--/v-->** ≤ 3 |

Balinanın toplamı ≈ ×<!--v:veri.syndicate.nodul.balina_toplam-->4,89<!--/v-->; profilin ×5'i aşılmaz. İzin dükkânı,
tetiklenen teklifler ve seri paketler Süperalaşım vermez. (Kaçakçı'nın Hadalitli Süperalaşım rafı bu
hesabın dışındadır; bkz. Açık sorular.) Dökümhane haftasına göre içerik:

<!-- URET:tablo:syndicate.nodul_haftalik -->
| Dökümhane haftası | F2P Süperalaşım / gün | Nodül I | Nodül II | Nodül III | Paketler / hafta | F2P / hafta | Balina ×5 / hafta |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 41 | 124 | 289 | 372 | 784 | 289 | 1.445 |
| 4 | 53 | 159 | 372 | 478 | 1.009 | 372 | 1.859 |
| 8 | 68 | 205 | 479 | 615 | 1.299 | 479 | 2.393 |
| 13 | 92 | 276 | 644 | 828 | 1.748 | 644 | 3.220 |
| 17 | 106 | 317 | 739 | 950 | 2.006 | 739 | 3.695 |
| 21 | 121 | 363 | 848 | 1.090 | 2.301 | 848 | 4.239 |
| 26 | 142 | 427 | 996 | 1.280 | 2.702 | 996 | 4.978 |
| 39 | 185 | 555 | 1.294 | 1.664 | 3.512 | 1.294 | 6.469 |
| 48 | 222 | 665 | 1.551 | 1.994 | 4.210 | 1.551 | 7.756 |
<!-- /URET:tablo:syndicate.nodul_haftalik -->

### Sana özel: tetiklenen teklifler

Kilometre taşlarında The Syndicate'in "Sana özel" rafına bir teklif konur ve HUD'daki The Syndicate
düğmesinde küçük camgöbeği bir nokta yanar. Açılır pencere, bildirim, ses yoktur. Teklif **bir sonraki
kilometre taşına kadar** rafta kalır; süre sayacı gösterilmez. Oyuncu "Gösterme" diyerek kalıcı olarak
gizleyebilir. 13–17 yaş hesaplarında tetiklenen teklif yoktur.

<!-- URET:tablo:syndicate.tetiklenen -->
| Tetik | Teklif | Fiyat | İçerik | × taban |
| --- | --- | ---: | --- | ---: |
| Reaktör 10 | İlk Halkalar | 4,99 $ | 400 HD · 10 sa inşaat · 15 GS kaynak kapsülü · 1 Destansı kart (seçmeli) | 14,8 |
| Reaktör 15 | Kubbe Hazırlığı | 9,99 $ | 600 HD · 4 sa genel · 20 sa inşaat · 30 GS kaynak kapsülü · 5 Kriyo bileti | 11,2 |
| Reaktör 20 | Derin Kuşak | 9,99 $ | 600 HD · 18 sa inşaat · 10 sa araştırma · 30 GS kaynak kapsülü · 4 Kriyo bileti | 11,3 |
| Reaktör 25 | Basınç Eşiği | 19,99 $ | 1.200 HD · 36 sa inşaat · 16 sa araştırma · 60 GS kaynak kapsülü · 1 Destansı kart (seçmeli) | 11 |
| Reaktör 30 | Dökümhane Açılışı | 19,99 $ | 1.200 HD · 40 sa inşaat · 60 GS kaynak kapsülü · 10 Kriyo bileti · 1 Destansı kart (seçmeli) | 11,7 |
| Reaktör 35 | Nodül Sahaları | 19,99 $ | 1.200 HD · 40 sa inşaat · 20 sa araştırma · 60 GS kaynak kapsülü · 6 Kriyo bileti | 11,3 |
| Reaktör 40 | Kubbe Ustalığı | 49,99 $ | 3.000 HD · 90 sa inşaat · 40 sa eğitim · 120 GS kaynak kapsülü · 2 Destansı kart (seçmeli) | 10 |
| Reaktör 45 | Son Basamaklar | 49,99 $ | 3.000 HD · 100 sa inşaat · 40 sa araştırma · 140 GS kaynak kapsülü · 10 Kriyo bileti | 10,3 |
| İlk Efsanevi kahraman | Efsanevi Gelişim | 9,99 $ | 500 HD · 20 GS kaynak kapsülü · 120 Anı Kristali | 8,2 |
| İlk ittifaka katılış | İttifak Başlangıcı | 1,99 $ | 120 HD · 3 sa genel · 6 GS kaynak kapsülü | 8,4 |
| Reaktör 50 | Çekirdeğe Varış | — | Ücretsiz armağan: Çekirdek madalyonu (profil) ve sezonluk üs ışığı | — |
<!-- /URET:tablo:syndicate.tetiklenen -->

İlk Efsanevi kahramandan sonra gelen teklif bilerek **çekim değil gelişim** malzemesidir (Anı Kristali,
kaynak); şanslı bir çekimin ardından yeni çekim önerilmez. Teklif **hiçbir zaman** şu anlarda tetiklenmez:

<!-- URET:tablo:syndicate.yasak_tetikler -->
| Durum | Neden teklif yok |
| --- | --- |
| Savaş yenilgisi, üssün yağmalanması | Kayıp anındaki öfke ya da utanç satışa çevrilmez |
| Asker ölümü, Revir taşması | Kayıp telafisi Kurtarma Kapsülü (K-21) ile ücretsiz yapılır |
| Oksijen ya da Gıda kritik seviyede | Hayatta kalma baskısı paraya çevrilmez |
| Kriyo-Kapsül'de uzun şanssız seri | Kumar döngüsü beslenmez; garanti sayacı zaten görünür |
| Uzun aradan dönüş | Dönüşte ücretsiz Dönüş Kapsülü verilir (15), teklif verilmez |
| Oturum 3 saati aştığında | Yorgun karar anında yeni rozet çıkmaz |
| Harcama sınırına yaklaşma | Sınır hiçbir zaman teklif tetikleyicisi değildir |
| 13–17 yaş hesabı | Tetiklenen teklif gösterilmez |
<!-- /URET:tablo:syndicate.yasak_tetikler -->

### İttifak hediyeleri

Bir üye The Syndicate'ten bir şey aldığında ittifaktaki diğer herkesin İttifak → Hediyeler sekmesine
bir hediye düşer. Alıcının adı varsayılan olarak **gizlidir** ("Bir ittifak üyesi"); alıcı isterse
"hediye adımla gitsin" der. Hediye adları, saklama süresi (3 gün), hediye puanı, Derin Hazine ve Hediye
Seviyesi [10 · İttifak](10_ittifak.md)'ındır; bu bölüm yalnız hangi alımın hangi hediyeyi doğurduğunu ve
içeriğini tanımlar.

<!-- URET:tablo:syndicate.ittifak_hediyeleri -->
| Hediye | Üyenin alımı | Her üyeye |
| --- | --- | --- |
| Kıyı Hediyesi | 0,99–9,99 $ | Biri: 5 dk genel hızlandırma · 10 HD · Titanyum Kapsülü 0,05 GS · 5 dk araştırma hızlandırma |
| Derin Hediye | 14,99–49,99 $ ya da Sendika Sözleşmesi | Biri: 15 dk genel hızlandırma · 25 HD · Titanyum Kapsülü 0,1 GS · küçük Sonar Pili |
| Hadal Hediyesi | 99,99 $ | İkisi: 15 dk genel hızlandırma · 50 HD · Titanyum Kapsülü 0,2 GS · %20 olasılıkla 1 Kriyo bileti · R30+ küçük Süperalaşım Kapsülü |
<!-- /URET:tablo:syndicate.ittifak_hediyeleri -->

Ortanca bir ittifakta (30 üye, birkaç düzenli ödeyen) ücretsiz oyuncu günde ~5–10 hediye açar.
Ekonomideki payı (günde ~0,15 GS, 0,1 sa hızlandırma, 15 HD) [15 · Ekonomi Akışı](15_ekonomi_akisi.md)'nın
ittifak hediyeleri satırıdır.

### Dolar başına değer: bütün ürünler

Seri paketlerden İnşaat Hattı'nın beş basamağı ve diğer serilerin ilk basamağı gösterilir.

<!-- URET:tablo:syndicate.deger_tablosu -->
| Ürün | Fiyat | Sınır | HD değeri | HD değeri / $ | × taban | Genel + inşaat (sa) | İzin puanı |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| Hadalit 50 | 0,99 $ | sınırsız | 50 | 51 | 1 | — | 100 |
| Hadalit 270 | 4,99 $ | sınırsız | 270 | 54 | 1,1 | — | 500 |
| Hadalit 560 | 9,99 $ | sınırsız | 560 | 56 | 1,1 | — | 1.000 |
| Hadalit 1.200 | 19,99 $ | sınırsız | 1.200 | 60 | 1,2 | — | 2.000 |
| Hadalit 3.200 | 49,99 $ | sınırsız | 3.200 | 64 | 1,3 | — | 5.000 |
| Hadalit 7.000 | 99,99 $ | sınırsız | 7.000 | 70 | 1,4 | — | 10.000 |
| Kuruluş Hediyesi (ilk alımla) | 0,99 $ | hesap başına 1 | 3.900 | 3.939 | 78,8 | 5 | 1.100 |
| Sendika Sözleşmesi (R30 öncesi) | 4,99 $ | 30 günde 1 | 9.600 | 1.924 | 38,5 | 45 | 500 |
| Sendika Sözleşmesi (R30 sonrası) | 4,99 $ | 30 günde 1 | 11.400 | 2.285 | 45,7 | 45 | 500 |
| Derin Akıntı Bileti · Akıntı | 9,99 $ | sezonda 1 | 12.300 | 1.231 | 24,6 | 24 | 1.000 |
| Derin Akıntı Bileti · Derin Akıntı | 19,99 $ | sezonda 1 | 28.560 | 1.429 | 28,6 | 54 | 2.000 |
| Reaktör Fonu | 19,99 $ | hesap başına 1 | 18.000 | 900 | 18 | — | 2.000 |
| İnşaat Hattı I | 4,99 $ | haftada 1 | 2.220 | 445 | 8,9 | 14 | 500 |
| İnşaat Hattı II | 9,99 $ | haftada 1 | 3.980 | 398 | 8 | 27 | 1.000 |
| İnşaat Hattı III | 19,99 $ | haftada 1 | 7.020 | 351 | 7 | 46 | 2.000 |
| İnşaat Hattı IV | 49,99 $ | haftada 1 | 14.960 | 299 | 6 | 92 | 5.000 |
| İnşaat Hattı V | 99,99 $ | haftada 1 | 24.880 | 249 | 5 | 144 | 10.000 |
| Laboratuvar Serisi I | 4,99 $ | haftada 1 | 2.160 | 433 | 8,7 | — | 500 |
| Filo Serisi I | 4,99 $ | haftada 1 | 2.160 | 433 | 8,7 | — | 500 |
| Kriyo Serisi I | 4,99 $ | haftada 1 | 2.100 | 421 | 8,4 | — | 500 |
| Vardiya Payı | 0,99 $ | günde 1 | 420 | 424 | 8,5 | 3 | 100 |
| İkmal Sandığı | 1,99 $ | günde 1 | 820 | 412 | 8,2 | — | 200 |
| Derin İkmal | 4,99 $ | günde 1 | 1.930 | 387 | 7,7 | 8 | 500 |
| Anı Kristali Kutusu I | 4,99 $ | ayda 1 | 1.700 | 341 | 6,8 | — | 500 |
| Anı Kristali Kutusu II | 19,99 $ | ayda 1 | 6.400 | 320 | 6,4 | — | 2.000 |
| Nesil Paketi | 19,99 $ | nesil başına 1 | 6.900 | 345 | 6,9 | — | 2.000 |
| Nodül Sandığı I | 4,99 $ | haftada 1 | 1.900 | 381 | 7,6 | — | 500 |
| Nodül Sandığı II | 14,99 $ | haftada 1 | 4.400 | 294 | 5,9 | — | 1.500 |
| Nodül Sandığı III | 29,99 $ | haftada 1 | 5.800 | 193 | 3,9 | — | 3.000 |
| İlk Halkalar | 4,99 $ | tek sefer | 3.700 | 741 | 14,8 | 10 | 500 |
| Kubbe Hazırlığı | 9,99 $ | tek sefer | 5.580 | 559 | 11,2 | 24 | 1.000 |
| Derin Kuşak | 9,99 $ | tek sefer | 5.640 | 565 | 11,3 | 18 | 1.000 |
| Basınç Eşiği | 19,99 $ | tek sefer | 10.980 | 549 | 11 | 36 | 2.000 |
| Dökümhane Açılışı | 19,99 $ | tek sefer | 11.700 | 585 | 11,7 | 40 | 2.000 |
| Nodül Sahaları | 19,99 $ | tek sefer | 11.280 | 564 | 11,3 | 40 | 2.000 |
| Kubbe Ustalığı | 49,99 $ | tek sefer | 24.900 | 498 | 10 | 90 | 5.000 |
| Son Basamaklar | 49,99 $ | tek sefer | 25.800 | 516 | 10,3 | 100 | 5.000 |
| Efsanevi Gelişim | 9,99 $ | tek sefer | 4.100 | 410 | 8,2 | — | 1.000 |
| İttifak Başlangıcı | 1,99 $ | tek sefer | 840 | 422 | 8,4 | 3 | 200 |
<!-- /URET:tablo:syndicate.deger_tablosu -->

Sıralama kasıtlıdır: **Kuruluş Hediyesi > Sözleşme > Bilet > Fon > tetiklenen teklifler > seriler,
günlük seçki, kristal kutuları ve Nesil Paketi > Nodül > Hadalit**. Düzenli ve sınırlı alan oyuncu en çok değeri alır; sınırsız harcama
en pahalı yoldan ilerler.

---

## Hızlandırıcılar

### Türler

<!-- URET:tablo:hizlandiricilar.turler -->
| Tür | Geçtiği yer | Simülasyonda sayılır | Liste değeri (HD / dk) |
| --- | --- | --- | ---: |
| Genel | Her kuyruk: inşaat, araştırma, eğitim, iyileştirme, kurtarma görevi | evet | 2 |
| İnşaat | Bina yükseltme ve halka açma | evet | 1,5 |
| Araştırma | Araştırma Merkezi ve Bilim Kubbesi kuyrukları | hayır | 1,5 |
| Eğitim | Askeri Kubbe eğitimi, Tatbikat Havuzu | hayır | 1,5 |
| İyileştirme | Revir ve Kriyo Bakım Ünitesi | hayır | 1,5 |
<!-- /URET:tablo:hizlandiricilar.turler -->

"Simülasyonda sayılır" sütunu: tempo simülasyonunun günlük hızlandırma varsayımı yalnız **genel ve
inşaat** hızlandırmalarını sayar ve hepsini reaktör yoluna harcar. Araştırma, eğitim ve iyileştirme
hızlandırmaları bunun dışındadır.

### Süreler

<!-- URET:tablo:hizlandiricilar.sureler -->
| Süre | Dakika | Tipli sürümü var | Genel liste değeri (HD) | Tipli liste değeri (HD) | Başlıca kaynak |
| --- | ---: | --- | ---: | ---: | --- |
| 1 dk | 1 | hayır | 2 | — | Görev ve sandıklar |
| 5 dk | 5 | evet | 10 | 7,5 | Görev, sandık, ittifak hediyesi |
| 15 dk | 15 | evet | 30 | 22,5 | Görev, sandık, İzin sandığı |
| 30 dk | 30 | evet | 60 | 45 | Etkinlik, bilet |
| 1 sa | 60 | evet | 120 | 90 | Etkinlik, bilet, paket |
| 3 sa | 180 | evet | 360 | 270 | Etkinlik, paket, İzin dükkânı |
| 8 sa | 480 | evet | 960 | 720 | Bilet, paket, İzin dükkânı |
| 24 sa | 1.440 | evet | 2.880 | 2.160 | Paket, sıralama ödülü |
| 3 g | 4.320 | hayır | 8.640 | — | Yalnız sezon sonu ödülü |
<!-- /URET:tablo:hizlandiricilar.sureler -->

### Kurallar

<!-- URET:tablo:hizlandiricilar.kurallar -->
| Kural | Açıklama |
| --- | --- |
| Uygulama sırası | Önce hız bonusları (K-13 bölen formülü), sonra ittifak yardımı, sonra hızlandırma; kalan süre ücretsiz bitirme eşiğine inince iş bedavaya biter. |
| Artan süre | Hızlandırma işi bitirdikten sonra artan süre kaybolur. Arayüz israfsız birleşimi önerir ve fazlayı önceden gösterir. |
| Öncelik | Oyuncu seçmezse önce tipli, sonra genel hızlandırma; önce kısa, sonra uzun süreler kullanılır. |
| Hadalit yedeği | Hızlandırma yetmezse kalan dakika Şimdi bitir fiyatından tamamlanabilir; hiçbir zaman otomatik değil. |
| Kurtarma görevi | Yalnız genel hızlandırma geçer; Hadalit şansı değişmez. |
<!-- /URET:tablo:hizlandiricilar.kurallar -->

### Şimdi bitir

Kalan süre Hadalit'le bitirilebilir: dakika başına <!--v:veri.hizlandiricilar.simdi_bitir.hd_dk-->2<!--/v--> HD, en az
<!--v:veri.hizlandiricilar.simdi_bitir.en_az-->2<!--/v--> HD, kalan süre yukarı, tam dakikaya yuvarlanır. Fiyat süreden
bağımsız olarak düz ve tahmin edilebilirdir; oyuncu her zaman ne ödeyeceğini bilir. 1.000 HD üstündeki
işlemler iki adımlı onay ister. Kaçakçı'nın Hadalitli hızlandırma rafları bu liste değerinin %20
altındadır ([13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md)).

<!-- URET:tablo:hizlandiricilar.simdi_bitir_ornekleri -->
| İş | Kalan dakika | Şimdi bitir (HD) | En iyi Hadalit kademesiyle | İnşaat Hattı I değeriyle |
| --- | ---: | ---: | ---: | ---: |
| 5 dk (ücretsiz bitirme eşiğinde) | 5 | — | — | — |
| 30 dk | 30 | 60 | 0,86 $ | 0,1 $ |
| 1 sa | 60 | 120 | 1,71 $ | 0,2 $ |
| 8 sa | 480 | 960 | 13,71 $ | 1,62 $ |
| 24 sa | 1.440 | 2.880 | 41,14 $ | 4,85 $ |
| Reaktör 40, F2P etkin süresi | 14.089 | 28.178 | 402,54 $ | 47,49 $ |
| Reaktör 50, F2P etkin süresi | 34.178 | 68.356 | 976,51 $ | 115,21 $ |
<!-- /URET:tablo:hizlandiricilar.simdi_bitir_ornekleri -->

### Ücretsiz oyuncunun hızlandırma kaynakları

Simülasyon ücretsiz oyuncunun günde 0,5 saatten (erken oyun) 2 saate (Reaktör 16+) kadar **genel +
inşaat** hızlandırması kazandığını varsayar. Dökümün sahibi [15 · Ekonomi Akışı](15_ekonomi_akisi.md)'dır;
tablo oradan gelir:

<!-- URET:tablo:oduller.hizlandirma -->
| Bant | Günlük sandıklar | Hikâye | İlk Dalış Haftası | Etkinlik + Kronik | Akıntı Savaşı | İttifak | Bilet (ücretsiz yol) | Kurtarma ve sefer | **Toplam sa/gün** | Simülasyon | Sapma |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| R1–R5 | 0,33 | 0,25 | 0 | 0 | 0 | 0 | 0 | 0 | 0,58 | 0,61 | −%4,9 |
| R6–R10 | 0,33 | 0,33 | 0 | 0 | 0 | 0,08 | 0 | 0,04 | 0,78 | 0,78 | %0 |
| R11–R15 | 0,33 | 0,33 | 0,17 | 0 | 0 | 0,1 | 0 | 0,07 | 1 | 1 | %0 |
| R16–R50 | 0,33 | 0 | 0 | 0,75 | 0,25 | 0,25 | 0,17 | 0,25 | 2 | 2 | %0 |
<!-- /URET:tablo:oduller.hizlandirma -->

Derinlik İzni sandığı ücretsiz oyuncunun seviyelerinde (İzin 1–7) genel değil **araştırma**
hızlandırması verir; bu yüzden 2 saatin dışında kalır ([11 · Derinlik İzni sandığı](11_vip.md)).

Tipli hızlandırmalar (öneri; simülasyona girmez, kendi kuyruklarına gider):

<!-- URET:tablo:hizlandiricilar.f2p_tipli -->
| Kaynak | Araştırma (dk / gün) | Eğitim (dk / gün) | İyileştirme (dk / gün) | Not |
| --- | ---: | ---: | ---: | --- |
| Günlük görev sandıkları | 15 | 10 | 0 | 40 ve 80 puan sandıklarına eklenir |
| Derinlik Ligi ve haftalık etkinlikler | 20 | 15 | 10 | Kademe ödülleri |
| Madalya Vitrini ve seferler | 0 | 10 | 10 | 08 · Seferler |
| İttifak (hediyeler, dükkân) | 10 | 5 | 5 | 10 · İttifak |
| Derinlik İzni sandığı (İzin 1–7) | 15 | 0 | 0 | Yıl içinde İzin 4–7 ortalaması |
| Toplam (dk / gün) | 60 | 40 | 25 | Simülasyon dışı; kendi kuyruklarına gider |
<!-- /URET:tablo:hizlandiricilar.f2p_tipli -->

### Zamanın fiyatı: $ / ay → HD / ay → saat / gün

Bir saatlik hızlandırmanın dolar karşılığı yola göre 40 kata kadar değişir:

<!-- URET:tablo:hizlandiricilar.saat_fiyati -->
| Yol | 1 saatin bedeli | Not |
| --- | ---: | --- |
| Sendika Sözleşmesi | 0,06 $ | HD değeri / $ = 1.924 |
| Derin Akıntı Bileti · Akıntı | 0,1 $ | HD değeri / $ = 1.231 |
| İnşaat Hattı I (inşaat saati) | 0,2 $ | HD değeri / $ = 445 |
| İnşaat Hattı V (inşaat saati) | 0,36 $ | HD değeri / $ = 249 |
| Günlük seçki · Vardiya Payı | 0,28 $ | HD değeri / $ = 424 |
| İzin dükkânı (90 HD) · en iyi Hadalit kademesiyle | 1,29 $ | Haftalık sınırlı |
| Şimdi bitir · en iyi Hadalit kademesi (70 HD / $) | 1,71 $ | Sınırsız, en pahalı yol |
| Şimdi bitir · 0,99 $ kademesi | 2,38 $ | En küçük paket |
<!-- /URET:tablo:hizlandiricilar.saat_fiyati -->

Profillerin kararlı ayı (Reaktör 16 sonrası, 30,4 gün):

<!-- URET:tablo:hizlandiricilar.profil_hesabi -->
| Profil | $ / ay | Satın alınan HD / ay | Ücretsiz kaynaklar (sa) | İzin sandığı (sa) | Sözleşme, bilet, paketler (sa) | Hadalit ile (sa) | Toplam sa / gün | Simülasyon sa / gün |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Ücretsiz oyuncu | 0 $ | 0 | 60,8 | — | — | — | 2 | 2 |
| Aylık kart + VIP 8 | 27,56 $ | 7.014 | 60,8 | 8,1 | 101 | 5,8 | 5,8 | 6 |
| Balina | 1.664,41 $ | 79.873 | 60,8 | 46,6 | 608,1 | 25 | 24,4 | 24 |
<!-- /URET:tablo:hizlandiricilar.profil_hesabi -->

**Aylık kart oyuncusu** (≈<!--v:veri.hizlandiricilar.profil_hesabi.1.dolar|para-->27,56 $<!--/v--> / ay): ücretsiz kaynakların
2 saatine Sözleşme'nin günlük 1,5 saati, biletin Akıntı yolu, ayda iki İnşaat Hattı I ve beş Vardiya Payı
eklenir; İzin 8'den sonra sandığın genel hızlandırması da gelir (yolculuk ortalaması günde ~16 dk).
Satın aldığı Hadalit'in ~%10'unu (700 HD) Şimdi bitir'e harcar. Toplam
<!--v:veri.hizlandiricilar.profil_hesabi.1.sa_gun-->5,8<!--/v--> sa / gün; simülasyon 6 sa varsayar.

<!-- URET:tablo:syndicate.harcama_kart -->
| Kalem | Adet / ay | $ / ay | Hadalit / ay | Genel + inşaat (sa / ay) |
| --- | ---: | ---: | ---: | ---: |
| Sendika Sözleşmesi | 1 | 4,99 | 3.300 | 45 |
| Derin Akıntı Bileti · Akıntı (8 haftaya bölünmüş) | 0,54 | 5,42 | 814 | 13 |
| İnşaat Hattı I | 2 | 9,98 | 600 | 28 |
| Günlük seçki · Vardiya Payı | 5 | 4,95 | 300 | 15 |
| Reaktör Fonu (9 aya bölünmüş) | 0,11 | 2,22 | 2.000 | — |
<!-- /URET:tablo:syndicate.harcama_kart -->

**Balina** (≈<!--v:veri.hizlandiricilar.profil_hesabi.2.dolar|para-->1.664,41 $<!--/v--> / ay): hızlandırmanın büyük kısmı her
hafta alınan İnşaat Hattı I–III'ten gelir; ayda bir İnşaat Hattı IV, sekiz Derin İkmal, İzin 10–14
sandığının farkı ve 3.000 HD'lik Şimdi bitir eklenir. Toplam <!--v:veri.hizlandiricilar.profil_hesabi.2.sa_gun-->24,4<!--/v--> sa / gün;
simülasyon 24 sa varsayar. Balinanın asıl harcaması hızlandırma değil, Kriyo ve Laboratuvar serileri,
Nodül Sandıkları ve Hadalit'tir; hızlandırma ona ayda ~300 $'a mal olur.

<!-- URET:tablo:syndicate.harcama_balina -->
| Kalem | Adet / ay | $ / ay | Hadalit / ay | Genel + inşaat (sa / ay) |
| --- | ---: | ---: | ---: | ---: |
| Sendika Sözleşmesi | 1 | 4,99 | 3.300 | 45 |
| Derin Akıntı Bileti · Derin Akıntı (8 haftaya bölünmüş) | 0,54 | 10,85 | 1.629 | 29,3 |
| İnşaat Hattı I–III (her hafta) | 4,34 | 151,87 | 7.383 | 377,8 |
| İnşaat Hattı IV | 1 | 49,99 | 2.000 | 92 |
| Laboratuvar Serisi I–IV (her hafta) | 4,34 | 368,97 | 16.069 | — |
| Filo Serisi I–III (her hafta) | 4,34 | 151,87 | 7.383 | — |
| Kriyo Serisi I–IV (her hafta) | 4,34 | 368,97 | 16.069 | — |
| Nodül Sandığı I–III (her hafta) | 4,34 | 217,01 | 3.040 | — |
| Hadalit 7.000 | 3 | 299,97 | 21.000 | — |
| Günlük seçki · Derin İkmal | 8 | 39,92 | 2.000 | 64 |
<!-- /URET:tablo:syndicate.harcama_balina -->

Çıkarım: hızlandırma hızı satın alınabilir ama **sınırlı ve azalan** ürünlerle. Balinanın 24 saati,
aylık kart oyuncusunun 6 saatinin dört katıdır, ödediği para ise ~60 katı. Fark, sınırlı ürünlerin
tükenip sonraki birimin daha pahalı yoldan (üst basamak, Şimdi bitir) alınmasından gelir.

---

## Hadalit nereye gider

<!-- URET:tablo:hizlandiricilar.batiklar -->
| Harcama yeri | Fiyat | Sınır | Bölüm |
| --- | --- | --- | --- |
| Kriyo-Kapsül | 200 HD tekli · 1.800 HD onluk | Sınırsız | 05 · Kahramanlar |
| Şimdi bitir | 2 HD / dk | Sınırsız | 12 · The Syndicate |
| Derinlik İzni dükkânı | 90–6.000 HD | Haftalık | 11 · Derinlik İzni |
| Sonar Çarkı | 300 HD · 10 çevirme 2.700 HD | Etkinlik başına 100 çevirme | 13 · Etkinlikler ve Sezonlar |
| Kaçakçı rafları ve yenileme | Liste değerinin %20 altı · yenileme 50 / 100 / 200 HD | Raf başına 1, günde 3 yenileme | 13 · Etkinlikler ve Sezonlar |
| Kadim Şema Parçası (Kaçakçı) | 1.500 HD (10 parça = 1 Şema) | Haftada 1 | 14 · Son Oyun |
| Sessizlik Kalkanı 8 sa · 24 sa · 3 g | 400 · 1.000 · 2.500 HD | Saldırı kalkanı kırar | 09 · Dünya Haritası ve PvP |
| Konuşlanma: Akıntı Şamandırası · İttifak Rotası · Hassas | 200 · 600 · 1.500 HD | Ortak konuşlanma koşulları | 09 · Dünya Haritası ve PvP |
| Bilet seviyesi atlama | 200 HD / seviye | Sezonda 10 seviye | 12 · The Syndicate |
| Köken değişimi | 5.000 HD (öneri) | 30 günde 1 | 16 · Kronik, Köken, Profil |
| Kaplamalar ve çerçeveler | 500–8.000 HD | — | 12 · The Syndicate |
| Ad değişikliği | 500 HD (ilki ücretsiz) | 30 günde 1 | 16 · Kronik, Köken, Profil |
<!-- /URET:tablo:hizlandiricilar.batiklar -->

Kalkan ve konuşlanma fiyatları [09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md)'nin önerisidir ve
burada kesinleşir; Sonar Çarkı ve Kaçakçı [13](13_etkinlikler_ve_sezonlar.md)'ündür. Kadim Şema'nın
Hadalit karşılığı Kaçakçı yoluyla 10 × 1.500 = 15.000 HD'dir ve haftada en çok bir parça alınabildiği için
en erken 10 haftada tamamlanır. "(öneri)" yazan fiyatlar ilgili bölümün kararıdır. Kriyo-Kapsül en büyük
batıktır; ücretsiz oyuncunun Hadalit geliri ve çekim temposu [15 · Ekonomi Akışı](15_ekonomi_akisi.md)'ndadır.

---

## Ücretsiz oyuncu ve ödeyen

| | Ücretsiz oyuncu | Aylık kart | Balina |
|---|---|---|---|
| Reaktör 50 | <!--v:veri.syndicate.fon_odemeleri.10.f2p_gun-->368,1<!--/v-->. gün | <!--v:veri.syndicate.fon_odemeleri.10.kart_gun-->267,2<!--/v-->. gün | <!--v:veri.syndicate.fon_odemeleri.10.balina_gun-->147,1<!--/v-->. gün |
| Aylık harcama | 0 $ | ≈<!--v:veri.hizlandiricilar.profil_hesabi.1.dolar-->27,56<!--/v--> $ | ≈<!--v:veri.hizlandiricilar.profil_hesabi.2.dolar-->1.664,41<!--/v--> $ |
| Satın alınan Hadalit / ay | 0 | <!--v:veri.hizlandiricilar.profil_hesabi.1.hd-->7.014<!--/v--> | <!--v:veri.hizlandiricilar.profil_hesabi.2.hd-->79.873<!--/v--> |
| Hızlandırma (genel + inşaat) | <!--v:veri.hizlandiricilar.profil_hesabi.0.sa_gun-->2<!--/v--> sa / gün | <!--v:veri.hizlandiricilar.profil_hesabi.1.sa_gun-->5,8<!--/v--> sa / gün | <!--v:veri.hizlandiricilar.profil_hesabi.2.sa_gun-->24,4<!--/v--> sa / gün |
| İnşaatçı | 2 (araştırma) | 3 (Sözleşme) | 3 (Sözleşme) |
| Süperalaşım | ×1 | ×2 | ×5 (tavan) |
| Bir yılda İzin | 7 | 9 | 15 |

- **Ücretsiz oyuncunun yolu kapalı değildir.** Hiçbir bina, kahraman, birlik türü ya da harita bölgesi
  satın almayla açılmaz. Ödeyen daha hızlı gider; ücretsiz oyuncu aynı yere varır.
- **Ödeyen en çok değeri sınırlı ürünlerde alır.** 4,99 $'lık Sözleşme ve 9,99 $'lık Bilet, ayda
  ~10 $'a ücretsiz oyuncunun hızlandırmasını iki katına çıkarır.
- **Balinanın parası ittifaka akar.** Her alımı 29 üyeye hediye bırakır; ittifak hediye seviyesi
  yükseldikçe herkesin hediyesi büyür.
- **Savaşta fark sınırlıdır.** İzin savaş bonusu en çok %2'dir; asıl fark asker sayısı ve kademedir.
  Bu farkı yumuşatan kurallar (kalkan, Kurtarma Kapsülü, K-21) [09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md)'dedir.

---

## Arayüz ve deneyim

**HUD.** Sağ üstteki düğmenin adı **The Syndicate**'tir ("Mağaza", "Shop" değil). Üzerinde yalnız
Hadalit bakiyesi ve küçük bir kristal simgesi yazar. Renk turuncu değil, koyu zemin üstünde camgöbeği
çizgidir. Nokta yalnız "Sana özel" rafına yeni bir teklif geldiğinde yanar (Fon ödemeleri ve Sözleşme payı
posta kutusuna düşer, noktaya ihtiyaç duymaz). Nokta tek, küçük, sayısız ve kırmızı olmayan bir
işarettir; yanıp sönmez.

**Ekran (dikey telefon).**

```
+----------------------------------------------------+
| The Syndicate                         ◇ 12.480 HD  |
+----------+-----------------------------------------+
| Vitrin   | Sendika Sözleşmesi                      |
| Hadalit  |   18. gün / 30 · bugünkü pay postada    |
| Sözleşme | Derin Akıntı Bileti                     |
| Bilet    |   Seviye 23 / 50 · Akıntı yolu açık     |
| Fon      | Reaktör Fonu                            |
| Seriler  |   7 / 11 ödeme alındı                   |
| Kristal  |                                         |
| Seçki    |                                         |
| Nodül    |                                         |
| Sana   • |                                         |
| İzin     |                                         |
+----------+-----------------------------------------+
| Satın alma geçmişi · Olasılıklar · Harcama sınırı  |
+----------------------------------------------------+
```

- **Sol ray** sekmelerdir; "Sana özel"in yanındaki nokta HUD'dakiyle aynıdır.
- **Kart** biçimi: ürün adı, içerik listesi (miktarlarla), mağazanın yerel fiyatı, tek bir "Satın al"
  düğmesi. Değer yüzdesi, üstü çizili fiyat, "en popüler" şeridi, kalan adet sayacı yoktur.
- **Satın alma anı:** mağazanın kendi ödeme sayfası açılır; dönüşte kartın kenarında tek bir sonar
  halkası yayılır ve içerik "Teslim edildi" satırına döner. Altın yağmuru, sandık patlaması, ses
  patlaması yoktur.
- **Yenilenme** "pazartesi yenilenir" ya da "yarın yenilenir" diye düz metindir; saat:dakika:saniye yazmaz.
- **Kriyo-Kapsül** The Syndicate'in sekmesi değildir; etkinlik sütunundaki kendi düğmesiyle açılır.
  Kapsül ekranında Hadalit yetmezse yalnız "Hadalit yetersiz" yazar, altında küçük bir "The Syndicate"
  bağlantısı durur; pencere açılmaz.
- **Alt şerit:** satın alma geçmişi (makbuz numaralarıyla), olasılık tablosu, harcama sınırı ayarı.

**Metin kuralları.** "İNDİRİM", "SALE", "%300 DEĞER", "SON ŞANS", "KAÇIRMA", "HEMEN" yazılmaz. Ünlem
kullanılmaz. Ürün adları derin deniz dilindedir (Sözleşme, Akıntı, Nodül, İkmal), pazar dili değildir.

---

## Etik ve uyum

<!-- URET:tablo:syndicate.yasal -->
| Konu | Kural | Dayanak |
| --- | --- | --- |
| Olasılık açıklaması | Kriyo-Kapsül oranları (%65 / %25 / %8 / %2) ve garantiler (30. çekimde Destansı, 80. çekimde Efsanevi) satın almadan önce kapsül ekranında; seri paketlerdeki rastgele kalemler de aynı ekranda. | App Store İnceleme Yönergeleri 3.1.1, Google Play ödeme politikası, Güney Kore oyun yasası (2024) |
| Bölge kuralı | Ücretli rastgele içeriği kumar sayan ülkelerde (ör. Belçika) satın alınmış Hadalit ile Kriyo çekimi kapalı; ücretsiz çekim ve bilet açık. | Ülke düzenleyicileri; bölge bayrağı sunucuda |
| Yaş | 13 yaş altı hesap açamaz. 13–17: aylık 50 $ harcama tavanı, tetiklenen teklif yok, Nodül ve 49,99 $ üstü basamaklar kapalı. | COPPA / GDPR-K, mağaza aile politikaları |
| Yaş derecelendirmesi | PEGI 12 ve ESRB T hedefi; 'Oyun içi satın alma (rastgele öğeler içerir)' etiketi. | PEGI, ESRB, IARC |
| Gönüllü harcama sınırı | Hesap → Harcama sınırı: günlük, haftalık ya da aylık. Düşürmek hemen, yükseltmek 72 saat sonra geçerli. | Oyuncu koruması (tasarım ilkesi) |
| Fiyat şeffaflığı | Her ürün mağazanın yerel fiyatını gösterir. Hadalit fiyatlı her şeyin yanında yaklaşık para karşılığı isteğe bağlı görünür (AB'de varsayılan açık). Gereğinden fazla Hadalit almaya zorlayan kademe yoktur. | AB tüketici koruma ağının sanal para ilkeleri (2025) |
| Dil | Emir kipi, aciliyet ve kıtlık dili yok ('hemen al', 'son şans', 'kaçırma'). Düğme yalnız 'Satın al'. | Haksız ticari uygulamalar (çocuklara doğrudan çağrı yasağı) |
| Cayma hakkı | Dijital içerik anında teslim edilir; oyuncu bunu satın alma sayfasında onaylar. İade mağaza üzerinden yürür. | Mesafeli Sözleşmeler Yönetmeliği (cayma hakkı istisnası), AB tüketici hakları direktifi |
| Abonelik yok | Sendika Sözleşmesi ve Bilet otomatik yenilenmez; her dönem ayrı satın alınır. | Tasarım ilkesi; mağaza abonelik kuralları devre dışı |
| Aylık özet | Her ayın ilk günü posta: 'Geçen ay The Syndicate'te harcadığın: …'. Bildirim değil, posta. | Şeffaflık (tasarım ilkesi) |
| Kişisel veri | Makbuz ve satın alma kaydı hesapla saklanır; analitik açık rızayla. Satın alma verisi reklam ağına gitmez. | KVKK, GDPR |
<!-- /URET:tablo:syndicate.yasal -->

Tasarım ilkeleri:

- **Teklif zamanı oyuncunun iyi anıdır.** Teklifler yalnız kilometre taşında çıkar; kayıp, kriz ve
  yorgunluk anında çıkmaz (yasak tetikler tablosu).
- **Kumar döngüsü beslenmez.** Kriyo-Kapsül'ün garantileri (30 / 80) ekranda sayaç olarak görünür; art
  arda kötü sonuçtan sonra "şansını dene" teklifi gelmez; çekim animasyonu "neredeyse kazanıyordun"
  gösterisi yapmaz.
- **Gizli maliyet yoktur.** Her içerik miktarıyla yazılır; rastgele kalemlerin olasılığı kartın altındadır.
- **Satın alma tersine çevrilebilir değildir ama anlaşılırdır.** Onay her zaman mağaza sayfasındadır;
  oyun içinde "tek dokunuşla satın al" yoktur.

---

## Makbuz doğrulama ve iade

Satın alma ödülünü **yalnız sunucu** verir; istemci hiçbir zaman Hadalit eklemez.

1. İstemci mağazadan satın alma makbuzunu (Apple: imzalı işlem, Google: satın alma jetonu) alır ve
   sunucuya gönderir.
2. Sunucu makbuzu mağazanın sunucu API'siyle doğrular (Apple App Store Server API ve imza zinciri,
   Google Play Developer API), ürün kimliğini ve hesabı eşleştirir.
3. İşlem kimliği veritabanında tekildir; aynı makbuz ikinci kez ödül vermez (idempotent).
4. Ödül mevcut ödül hattından verilir (`GameWorld.GrantAsync`), İzin puanı deftere yazılır, ittifak
   hediyesi kuyruğa girer.
5. Google satın alması onaylanır (acknowledge); onaylanmayan satın alma mağaza tarafından iade edilir.
6. İade bildirimleri (Apple sunucu bildirimleri, Google iptal edilen satın almalar) dinlenir:

<!-- URET:tablo:syndicate.iade -->
| Durum | Sunucu ne yapar |
| --- | --- |
| Mağaza iadeyi bildirdi | Ürünün verdiği Hadalit önce ücretli, yetmezse kazanılmış bakiyeden geri alınır (15); bakiye eksiye düşebilir. Eksi bakiyede Hadalit harcanamaz, oyun oynanır. |
| Sendika Sözleşmesi iadesi | Kalan günler biter; 3. inşaatçı süren işini tamamlar, yenisini almaz. Teslim edilmiş günlük Hadalit geri alınır. |
| Bilet ya da Fon iadesi | Ücretli yol ya da fon kapanır; ödenmiş Hadalit geri alınır, kozmetik kalır. |
| Derinlik İzni puanı | İade edilen tutarın puanı düşülür, İzin seviyesi yeniden hesaplanır (kalıcılığın tek istisnası). |
| İttifak hediyeleri | Üyelerden geri alınmaz; masum üçüncü kişiler cezalandırılmaz. |
| Tekrarlayan iade | 90 günde 3+ kullanılmış ürün iadesi → satın alma 30 gün kapanır; hesap kapatılmaz. |
| Bekleyen ödeme | Aile onayı ya da nakit ödeme bekleyen işlem tamamlanınca ödül verilir. |
| Çift bildirim, yeniden deneme | İşlem kimliğiyle tekil kayıt; aynı işlem ikinci kez ödül vermez. |
<!-- /URET:tablo:syndicate.iade -->

Test hesapları (sandbox, lisans testçisi) ayrı işaretlenir; ödülleri istatistiğe girmez.

---

## Bugün kodda

| Konu | Durum | Not |
|---|---|---|
| Hadalit kaynağı | 🟡 | `Hadalite` var; başlangıç 100, tavan 1.000. Hedef ×10, tavansız (K-12) |
| Kriyo-Kapsül | 🟡 | Tekli 20, onluk 180 HD, günde 1 ücretsiz, 30. çekimde Destansı garanti. Hedef 200 / 1.800, 80. çekimde Efsanevi |
| HUD düğmesi | 🟡 | "Shop" adıyla Hadalit bakiyesini gösteriyor ve Kriyo-Kapsül'ü açıyor; turuncu |
| The Syndicate ekranı | ⬜ | Yok |
| Gerçek para satın alma (IAP) | ⬜ | Unity IAP paketi kurulu değil; sunucuda makbuz doğrulama yok |
| Sendika Sözleşmesi, 3. inşaatçı | ⬜ | `progression.builderSlots` = 1; 2. inşaatçı araştırmadan (K-07) |
| Bilet, Fon, seriler, seçki, Nodül | ⬜ | Yok |
| İttifak hediyeleri | ⬜ | Yok |
| Hızlandırma eşyaları, Şimdi bitir | ⬜ | Yok; yalnız ücretsiz bitirme (300 sn) var ✅ |
| Ödül verme hattı, kupon kodu | ✅ | `CouponService.RedeemAsync` → `GameWorld.GrantAsync` |
| Hadalit ödülleri | 🟡 | Görev ve sandık ödülleri eski ölçekte (ör. günlük sandık 20 HD) |

## Uygulama notları

- **Unity IAP kurulu değil.** `Packages/manifest.json`'da `com.unity.purchasing` yok. Eklenecek sürüm,
  Google'ın o yıl zorunlu tuttuğu Play Billing Library sürümünü ve Apple'ın StoreKit 2 işlem biçimini
  desteklemeli. Bütün ürünler **tüketilebilir** (consumable) tanımlanır; hak sahipliği (Sözleşme günleri,
  bilet yolu, fon) sunucudaki hesapta tutulur. Otomatik yenilenen abonelik kullanılmaz.
- **Ürün kimlikleri** `syndicate.json`'daki `id`'lerle aynı tutulur: mağaza kimliği
  `com.cicibyte.hadal.<id>` (ör. `com.cicibyte.hadal.hd_499`, `com.cicibyte.hadal.sozlesme`).
- **Sunucu.** `PurchaseService`: makbuz doğrulama, `purchases` tablosu (işlem kimliği tekil dizin,
  durum: bekliyor / verildi / iade), iade bildirimi uç noktası, eksi Hadalit bakiyesine izin.
  Mağaza sırları (Apple anahtarı, Google hizmet hesabı) yalnız sunucuda, ortam değişkeninde.
- **İki Hadalit defteri.** Ücretli ve kazanılmış Hadalit ayrı tutulur ([15](15_ekonomi_akisi.md)): iade önce
  ücretliden düşer; bölge kuralı (ücretli Hadalit'le rastgele içerik yok) ve mağaza muhasebesi de buna
  dayanır. Harcamada önce kazanılmış bakiye kullanılır.
- **Katalog ×10 geçişi (catalog v9).** `resources.Hadalite.startingAmount` 100 → 1.000;
  `maxCapacity` 1.000 → tavansız (64 bit tamsayı; istemcide `long`); `heroes.pod.cost` 20 → 200,
  `tenCost` 180 → 1.800; bütün Hadalit ödülleri ×10: `quests.dailyChests` 100 puan sandığı 20 → 200,
  `q_salvage` 20 → 200, `q_pod` 30 → 300, `q_alliance` 50 → 500, sandık tabloları 10 / 20 / 30 → 100 /
  200 / 300, `salvageMissions.*.hadaliteMin/Max` ×10. Mevcut oyuncu bakiyeleri bir kerelik göçle ×10.
- **Yeni katalog bölümleri:** `syndicate` (ürünler, içerik, sınır, sıfırlama günü), `speedups`
  (tür, süre, kaynak tabloları), `instantFinish` (`hdPerMinute` 2, `min` 2).
- **Hızlandırma eşyaları** `items[]`'a `kind: "speedup"`, `queue: general|build|research|train|heal`,
  `seconds` alanlarıyla girer; sandık tablolarına eklenir.
- **HUD.** `MainHudView`'daki `Tap(header, "Shop", …)` → The Syndicate ekranını açar; etiket
  `Loc.T("hud.syndicate")` = "The Syndicate"; renk koyu zemin + camgöbeği; Kriyo-Kapsül etkinlik
  sütunundaki kendi düğmesinde kalır. `26_HUD.md`'deki "Mağaza 💎" bu bölümle geçersizdir.
- **Posta kutusu** Sözleşme payı, Fon ödemesi ve aylık özet için gerekir (bugün yok).
- **Sunucu saati.** Günlük ve haftalık sıfırlamalar sunucu gününe (UTC) bağlıdır; istemci saati
  değiştirilerek seçki ya da sözleşme payı yenilenemez.
- **Analitik.** Satın alma hunisi ölçülür; ama A/B testleri fiyat ve içerik içindir, baskı tekniği
  (sayaç, kıtlık metni) için değil.

## Esin kaynakları

- Tür içindeki 4X strateji oyunlarının fiyat merdivenleri (0,99 $ … 99,99 $), 30 günlük premium para
  kaynağı, bir kerelik büyüme fonu, ücretsiz ve iki ücretli yollu sezon bileti, temalı paket
  merdivenleri ve üyelerin alımlarından ittifaka hediye düşen sistemler.
- Hayatta kalma temalı 4X oyunlarının geniş ürün yelpazesi ve kahraman gacha'sı; burada yalnız yapı
  olarak, sayıları ve adları alınmadan.
- Bilinçli olarak alınmayanlar: geri sayımlı teklifler, açılır satış pencereleri, yukarı doğru
  ucuzlayan merdivenler, otomatik yenilenen abonelik, kayıp anında tetiklenen teklifler.

## Açık sorular

1. **Ücretsiz Hadalit geliri.** [15](15_ekonomi_akisi.md) ücretsiz oyuncuya ayda ~14.300 HD veriyor (İzin
   sandığı hariç). Bu, en iyi kademede ~204 $, Sözleşme'nin aylık Hadalit'inin (3.300) ~4 katı ve
   Sözleşme'nin bütün içerik değerinin (9.600 HD) ~1,5 katıdır. 15'in kendi hedefi ("aylık kartın
   aylık değerinin yarısını geçmesin") için ücretsiz Hadalit ayda ~4.800 HD'ye inmeli ya da hedef
   gevşetilmeli. Çekim fiyatı (200 / 1.800) K-12'de sabit olduğundan ayar ücretsiz gelirde yapılmalı.
2. **Kaçakçı'nın Süperalaşım rafı.** [13](13_etkinlikler_ve_sezonlar.md)'ün "Süperalaşım 20 · 300 HD" rafı
   günde 4 ziyaret ve 3 yenilemeyle ödeyene haftada ~14 raf (~280 Süperalaşım) açabilir; Dökümhane'nin ilk
   haftalarında bu ücretsiz oyuncunun haftalık gelirine yakındır ve ×5 tavanını aşar. Öneri: raf kaynakla
   satılsın ya da haftada 1 raf ile sınırlansın.
3. **Süperalaşım çarpanının kaynağı.** [15](15_ekonomi_akisi.md) balinanın ×5'ini yalnız paket kapsülleri ve
   sezon dükkânına bağlıyor ve dökümhane sınırını herkes için aynı sayıyor; bu bölüm ≈×1'i oyuncunun kendi
   avantajına (daha erken ikinci Dökümhane, İzin toplama bonusu, daha çok yürüyüş, sıralama) veriyor.
   15'in yorumu seçilirse satın alınabilir tavan ×4 olur ve Nodül Sandıkları haftada ~26 Süperalaşım
   gününe çıkmalıdır.
4. **Seri merdivenin azalan değeri** tür alışkanlığının tersidir ve geliri düşürebilir. Yumuşak lansmanda
   düz değerli merdivenle karşılaştırılmalı mı (etik çizgiyi koruyarak)?
5. **Kriyo Serisi yoğunluğu.** Haftada I–IV alan balina 101 bilet + 3.700 HD alır (~121 çekim). Efsanevi
   kahraman yıldızlarının parça gereksinimi ([05](05_kahramanlar.md)) bu akışı kaldırıyor mu?
6. **Bilet sezonu ile Akıntı Savaşı.** Bilet 8 haftalık, Akıntı Savaşı döngüsü 50 + 20 gün
   ([13](13_etkinlikler_ve_sezonlar.md)). Bilet sezonları savaş döngüsüne mi oturmalı (10 hafta), yoksa
   bağımsız mı kalmalı?
7. **Batık fiyatları** (köken değişimi, ad değişikliği) 16 ile kesinleşmeli.
8. **Bölge kuralları.** Ücretli rastgele içeriği kısıtlayan ülke listesi yayın öncesi hukukçuyla
   güncellenmeli; Türkiye'de özel bir kural bugün yok.
9. **Kuruluş havuzu.** Kuruluş Hediyesi'nin seçmeli Destansı kahramanları için üç kahramanlık bir havuz
   [05](05_kahramanlar.md)'te tanımlanmalı (her köken grubundan dengeli).
10. **Yerel fiyat.** Türkiye mağaza kademeleri dolar kademelerinin birebir çevirisi değildir; içerik
   sabit kalırken yerel fiyat farkı kabul ediliyor mu?
