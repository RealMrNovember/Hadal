# 11 · Derinlik İzni (VIP)

Derinlik İzni, Hadal'ın VIP sistemidir: oyuncunun hesabına bağlı, **kalıcı** ve **etkinleştirme süresi
olmayan** bir ayrıcalık kademesi. Her seviye bir derinliğe karşılık gelir: İzin 0 üssün kurulduğu
~6.000 m'de başlar (K-03), İzin 15 çukurun en derin noktasında, Çekirdek'in yattığı derinlikte biter
(Mariana'nın Challenger Deep'inde 10.935 m; kademe etiketleri bu profili kullanır).
Oyuncuya görünen ad hiçbir yerde "VIP" değildir; kod kimliği `vip` kalır.

Bu bölüm [00 · Kanon](00_kanon.md)'a (K-04, K-06, K-07, K-12, K-13) ve tempo simülasyonunun oyuncu
profillerine ([02 · Tempo ve Denge](02_tempo_ve_denge.md), [Tempo simülasyonu](uretilen/simulasyon.md))
bağlıdır. Satın alınan ürünler [12 · The Syndicate](12_syndicate.md)'tedir. Bütün sayılar
`veri/vip.json`'dan gelir.

---

## Amaç

1. **Bağlılığı ödüllendirmek.** Her gün giren ücretsiz oyuncu bir yılda İzin
   <!--v:veri.vip.seviyeler.7.seviye-->7<!--/v-->'ye ulaşır; ayrıcalıkların çoğu üretim, toplama,
   kolaylık ve sandıktır. İzin, "para veren kazanır" rozeti değil, "burada kalan rahat eder" belgesidir.
2. **Harcamayı sessizce tanımak.** Her dolar aynı oranla puan verir; satın alma geçmişi kaybolmaz,
   süresi dolmaz, yeniden etkinleştirilmez.
3. **Tempo profilleriyle tutarlı olmak.** Aylık kart profilinin "+%15 hız" varsayımı İzin 8'in inşa hızı
   (<!--v:veri.vip.ekonomi.8.insa|yuzde-->%12<!--/v-->) ile Sendika Sözleşmesi'nin
   +%<!--v:veri.vip.kurallar.sozlesme_hizi-->5<!--/v-->'ine yakındır; balinanın "+%30"u İzin 15
   (<!--v:veri.vip.ekonomi.15.insa|yuzde-->%25<!--/v-->) ile Sözleşme'nin toplamıdır. Aşağıda simülasyon
   bu eğriyle yeniden oynatılır.

### Neden "Derinlik İzni"?

Derin dalış operasyonlarında kimse "üye" olmaz; ekip belirli bir derinliğe inmeye **yetkilendirilir**.
İzin bu duygunun karşılığıdır: kademe atladıkça oyuncu daha derine inme yetkisi kazanır, arayüzde de
yalnızca seviye ve derinlik yazar ("İzin 8 · 8.650 m"). Taç, altın rozet, renkli isim yoktur.
"VIP" kelimesi kulüp ve gösteriş çağrıştırır; Quiet Luxury bunun tersini ister.

### Neden kalıcı, neden süresiz?

Etkinleştirme süreli VIP sistemleri her hafta "VIP'in sona eriyor" baskısı üretir; oyuncu bir şey
satın almadan ayrıcalığını kaybetmekten korkar. Hadal bu kaygıyı satmaz:

- İzin bir kez kazanıldı mı **sonsuza dek** geçerlidir. Puan azalmaz, seviye düşmez.
- Ayrıcalıklar her zaman açıktır; "etkinleştir" düğmesi, geri sayım, "süre ekle" ürünü yoktur.
- Tek süreli ürün açıkça 30 günlük olan [Sendika Sözleşmesi](12_syndicate.md#sendika-sözleşmesi)'dir ve o
  da İzni kiralamaz; kendi ayrıcalıklarını (3. inşaatçı, +%5 hız, giriş eki) taşır.

---

## Kurallar

**Puan.** İzin seviyesi kümülatif puanla belirlenir. Puanın üç kaynağı vardır:

1. **Günlük giriş serisi.** İlk gün <!--v:veri.vip.kurallar.giris.baslangic-->30<!--/v--> puan, her ardışık gün
   +<!--v:veri.vip.kurallar.giris.artis-->10<!--/v-->, <!--v:veri.vip.kurallar.giris.tavan_gunu-->13<!--/v-->. günden itibaren
   günde <!--v:veri.vip.kurallar.giris.tavan-->150<!--/v--> puan.
2. **Oyun içi kaynaklar.** Günlük görev sandığı, haftalık etkinlikler, bölüm görevleri, Derinlik Kroniği,
   biletin ücretsiz yolu ve ittifak hediyeleri; yılda toplam ≈<!--v:veri.vip.kurallar.olay_puani_yil-->12.000<!--/v--> puan.
3. **Satın alma.** Her dolar <!--v:veri.vip.kurallar.puan_dolar-->100<!--/v--> puan (fiyat tam dolara yuvarlanır:
   0,99 $ → 100, 4,99 $ → 500, 99,99 $ → 10.000). Bütün ürünler aynı oranla puan verir; gizli çarpan yoktur.

**Kaçırılan gün.** Girilmeyen her gün seri sayacı **yarıya iner** (aşağı yuvarlanır), sıfırlanmaz. Yarıya
inen sayaç 12 ya da üstünde kalırsa dönüş günü yine 150 puandır; uzun seriden sonra bir gün kaçırmak
yalnız o günün puanına mal olur. Sayaç hiçbir zaman "serini kaybettin" uyarısı üretmez.

**Puan satılmaz.** Ayrı bir "İzin puanı paketi" yoktur. Etkinlik ve ittifak ödüllerinde seyrek bir
**İzin Puanı eşyası** çıkar (yukarıdaki oyun içi kaynaklara dahil); The Syndicate'te satılmaz. İzin,
satın almanın yan ürünüdür; amaç değildir.

**Kalıcılık.** Puan ve seviye kalıcıdır. Tek istisna iadedir: mağazanın iade ettiği bir satın almanın
puanı geri alınır ve seviye yeniden hesaplanır ([12 · Makbuz doğrulama ve iade](12_syndicate.md#makbuz-doğrulama-ve-iade)).

**Hız.** İzin'in inşa, araştırma ve eğitim yüzdeleri K-13 bölen formülüne girer:
süre = ham süre / (1 + Σhız% / 100). Araştırma, kahraman, ittifak ve Sözleşme hızlarıyla toplanır.

**Ücretsiz bitirme ve yardım.** İzin'in ek ücretsiz bitirme dakikaları Mühendislik Bölmesi'nin uzattığı
eşiğe eklenir. İzin'in ek ittifak yardımları İttifak Merkezi'nin yardım tavanını aşar (tavanın üstüne
eklenir; bkz. [10 · İttifak](10_ittifak.md)).

**Kapsam dışı.** İzin paralel kuyruk, yuva ya da içerik açmaz: inşaatçı sayısı (K-07), yürüyüş kuyruğu,
kayıtlı filo dizilimi ([05 · Kahramanlar](05_kahramanlar.md): herkese 2), kahraman ve birlik türleri herkes
için aynıdır. İzin 8, 9 ve 11'in "sıra" ayrıcalıkları yalnız bir sonraki işi sıraya koyar; aynı anda
çalışan iş sayısı değişmez.

**Sunucu otoritesi.** Seviye ve ayrıcalıklar sunucuda hesaplanır; istemci yalnız gösterir.

---

## Sayılar

### Eşikler ve kim ne zaman ulaşır

"Yalnız harcamayla" sütunu, puanın tamamı satın almadan gelseydi gereken tutardır. Gün sütunları
puan modelinden gelir: ücretsiz oyuncu her gün (ya da haftada 6 gün) girer ve yılda
<!--v:veri.vip.kurallar.olay_puani_yil-->12.000<!--/v--> olay puanı alır; aylık kart oyuncusu ayrıca Sözleşme'nin giriş
ekini ve ayda ≈27,5 $ harcamayı ekler; balina ayda ≈1.665 $ harcar. "—", ilk
<!--v:veri.vip.kurallar.gun_sinir-->480<!--/v--> günde ulaşılmadığını gösterir.

<!-- URET:tablo:vip.seviyeler -->
| İzin | Derinlik (m) | Puan | Yalnız harcamayla | F2P her gün (gün) | F2P 6/7 (gün) | Aylık kart (gün) | Balina (gün) | Açılan |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 0 | 6.000 | 0 | — | — | — | — | — | Giriş serisi başlar |
| 1 | 6.300 | 100 | 1 $ | 2 | 2 | 1 | 1 | Derinlik İzni sandığı (günlük) |
| 2 | 6.650 | 500 | 5 $ | 6 | 6 | 1 | 1 | Tek dokunuşla toplama (tüm üretim binaları) |
| 3 | 7.000 | 1.500 | 15 $ | 13 | 16 | 1 | 1 | Haftalık ek Kriyo-Kapsül çekimi başlar |
| 4 | 7.300 | 4.000 | 40 $ | 27 | 37 | 4 | 1 | Derinlik İzni dükkânı açılır |
| 5 | 7.650 | 10.000 | 100 $ | 59 | 86 | 19 | 2 | Otomatik ittifak yardımı isteği |
| 6 | 8.000 | 22.000 | 220 $ | 125 | 184 | 47 | 4 | Kurtarma görevleri %10 daha kısa sürer |
| 7 | 8.300 | 45.000 | 450 $ | 251 | 370 | 102 | 7 | Reaktör ışığı (yalnız kendi üssünde) |
| 8 | 8.650 | 75.000 | 750 $ | 415 | — | 172 | 12 | İnşaat sırası: bir sonraki yükseltme sıraya alınır |
| 9 | 9.000 | 125.000 | 1.250 $ | — | — | 291 | 19 | Araştırma sırası |
| 10 | 9.300 | 185.000 | 1.850 $ | — | — | 432 | 28 | Reaktör ışığı dünya haritasında da görünür |
| 11 | 9.650 | 270.000 | 2.700 $ | — | — | — | 42 | Eğitim sırası |
| 12 | 10.000 | 400.000 | 4.000 $ | — | — | — | 64 | Küçük savaş ayrıcalıkları başlar |
| 13 | 10.300 | 600.000 | 6.000 $ | — | — | — | 99 | Kubbe tabanında biyolüminesan halka |
| 14 | 10.650 | 850.000 | 8.500 $ | — | — | — | 142 | Profil kartında derinlik çizgisi (isteğe bağlı) |
| 15 | 10.935 | 1.200.000 | 12.000 $ | — | — | — | 202 | Dip ışığı: halka Challenger lacivertine döner |
<!-- /URET:tablo:vip.seviyeler -->

Eşikler İzin 7'ye kadar yumuşak, sonra dik yükselir. İlk yedi kademe zamanla, son sekiz kademe
harcamayla doldurulur. Her gün giren ücretsiz oyuncu İzin 7'ye <!--v:veri.vip.seviyeler.7.f2p_gun-->251<!--/v-->. günde,
haftada bir gün kaçıran oyuncu <!--v:veri.vip.seviyeler.7.f2p_67_gun-->370<!--/v-->. günde ulaşır. İzin 8 ücretsiz oyuncu
için ikinci yılın hedefidir (<!--v:veri.vip.seviyeler.8.f2p_gun-->415<!--/v-->. gün).

### Günlük giriş serisi

<!-- URET:tablo:vip.giris_serisi -->
| Seri günü | Puan |
| --- | ---: |
| 1 | 30 |
| 2 | 40 |
| 3 | 50 |
| 4 | 60 |
| 5 | 70 |
| 6 | 80 |
| 7 | 90 |
| 8 | 100 |
| 9 | 110 |
| 10 | 120 |
| 11 | 130 |
| 12 | 140 |
| 13 ve sonrası | 150 |
<!-- /URET:tablo:vip.giris_serisi -->

Kaçırma örnekleri ("toplam kayıp", hiç kaçırmayan oyuncuya göre 40 günlük puan farkıdır; kaçırılan
günler dahil):

<!-- URET:tablo:vip.kacirma_ornekleri -->
| Durum | Önceki günlük puan | Yeni sayaç | Dönüş günü puanı | Toplam kayıp puan |
| --- | ---: | ---: | ---: | ---: |
| Seri 8. gününde, 1 gün girmedi | 100 | 4 | 70 | 410 |
| Seri 20. gününde, 1 gün girmedi | 150 | 10 | 130 | 180 |
| Seri 40. gününde, 1 gün girmedi | 150 | 20 | 150 | 150 |
| Seri 40. gününde, 3 gün girmedi | 150 | 5 | 80 | 730 |
| Seri 120. gününde, 2 gün girmedi | 150 | 30 | 150 | 300 |
<!-- /URET:tablo:vip.kacirma_ornekleri -->

### Puan kaynakları

Ücretsiz oyuncu (her gün giren) bir yılda ≈<!--v:veri.vip.kurallar.f2p_yil_puani-->66.000<!--/v--> puan toplar:

<!-- URET:tablo:vip.f2p_kaynaklari -->
| Kaynak | Puan / yıl | Açıklama |
| --- | ---: | --- |
| Günlük giriş serisi | 53.970 | 30 puandan başlar, her gün +10, 13. günden itibaren 150 |
| Günlük görev sandığı (100 puan) | 3.650 | Her gün 10 puan |
| Haftalık etkinlikler | 5.200 | Haftada ~100 puan (katılım ödülü) |
| Bölüm görevleri ve Derinlik Kroniği | 1.200 | Çoğu ilk üç ayda |
| Derin Akıntı Bileti ücretsiz yolu | 975 | Sezonda 150 puan, yılda ~6,5 sezon |
| İttifak hediye sandıkları | 975 | Seyrek İzin puanı öğesi (ortanca ittifak) |
<!-- /URET:tablo:vip.f2p_kaynaklari -->

Ücretli kaynaklar:

<!-- URET:tablo:vip.ucretli_kaynaklar -->
| Kaynak | Puan | Not |
| --- | --- | --- |
| Her satın alma | 100 puan / $ | Fiyat yuvarlanır: 0,99 $ → 100, 4,99 $ → 500, 99,99 $ → 10.000 |
| Kuruluş Hediyesi | 1.000 puan | İlk satın almayla bir kez |
| Sendika Sözleşmesi süresince | +150 / giriş günü | Giriş serisi puanına eklenir (tavanda 300) |
| Ayrı İzin puanı paketi | yok | Puan yalnız satın almanın yan ürünüdür; puan satılmaz |
<!-- /URET:tablo:vip.ucretli_kaynaklar -->

### İlerleme: 30, 90, 180, 270 ve 365. gün

<!-- URET:tablo:vip.ilerleme -->
| Profil | 30. gün | 90. gün | 180. gün | 270. gün | 365. gün |
| --- | --- | --- | --- | --- | --- |
| Ücretsiz · her gün | İzin 4 · 4.700 | İzin 5 · 15.700 | İzin 6 · 32.100 | İzin 7 · 48.600 | İzin 7 · 66.000 |
| Ücretsiz · haftada 6 gün | İzin 3 · 3.200 | İzin 5 · 10.700 | İzin 5 · 21.700 | İzin 6 · 32.700 | İzin 6 · 44.400 |
| Ücretsiz · haftada 5 gün | İzin 3 · 2.200 | İzin 4 · 6.800 | İzin 5 · 13.700 | İzin 5 · 20.400 | İzin 6 · 27.600 |
| Aylık kart · ≈27,5 $/ay | İzin 5 · 14.900 | İzin 6 · 40.300 | İzin 8 · 78.400 | İzin 8 · 116.500 | İzin 9 · 156.700 |
| Balina · ≈1.665 $/ay | İzin 10 · 204.100 | İzin 12 · 552.700 | İzin 14 · 1.075.600 | İzin 15 · 1.598.500 | İzin 15 · 2.150.400 |
<!-- /URET:tablo:vip.ilerleme -->

### Ekonomi ayrıcalıkları

Yüzdeler toplam değerdir (bir önceki seviyeye eklenmez). İnşa ve araştırma hızı İzin 8'de
<!--v:veri.vip.ekonomi.8.insa|yuzde-->%12<!--/v-->'ye sıçrar: aylık kart profilinin "İzin 8 ≈ +%10–15 hız"
varsayımı budur. İzin 15'te <!--v:veri.vip.ekonomi.15.insa|yuzde-->%25<!--/v--> ile tavan yapar.

<!-- URET:tablo:vip.ekonomi -->
| İzin | İnşa hızı | Araştırma hızı | Eğitim hızı | Üretim | Toplama | Ücretsiz bitirme (+dk) | İttifak yardımı (+) |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | — | — | — | — | — | — | — |
| 1 | — | — | — | %1 | — | — | — |
| 2 | %1 | %1 | — | %2 | — | 1 | — |
| 3 | %2 | %2 | %2 | %3 | %3 | 1 | — |
| 4 | %3 | %3 | %3 | %4 | %4 | 2 | 1 |
| 5 | %4 | %4 | %5 | %5 | %5 | 2 | 1 |
| 6 | %5 | %5 | %6 | %6 | %6 | 3 | 1 |
| 7 | %7 | %7 | %8 | %8 | %8 | 3 | 1 |
| 8 | %12 | %12 | %10 | %10 | %10 | 5 | 2 |
| 9 | %13 | %13 | %12 | %11 | %12 | 5 | 2 |
| 10 | %15 | %15 | %14 | %12 | %14 | 6 | 2 |
| 11 | %17 | %17 | %16 | %13 | %16 | 7 | 3 |
| 12 | %19 | %19 | %18 | %14 | %18 | 8 | 3 |
| 13 | %21 | %21 | %20 | %16 | %20 | 9 | 4 |
| 14 | %23 | %23 | %22 | %18 | %22 | 10 | 4 |
| 15 | %25 | %25 | %25 | %20 | %25 | 10 | 5 |
<!-- /URET:tablo:vip.ekonomi -->

- **Üretim** Oksijen, Enerji, Gıda, Biyokütle ve Titanyum üretimine uygulanır; **Süperalaşım üretimine
  uygulanmaz** (Süperalaşım tavanı Nodül Sandıkları'yla korunur, bkz. [12 · Nodül](12_syndicate.md#nodül-sandıkları-haftalık-süperalaşım)).
- **Toplama** haritadaki kaynak sahalarında toplama hızıdır.
- **Ücretsiz bitirme** taban 5 dakikaya ve Mühendislik Bölmesi'nin eklediği dakikalara eklenir:
  İzin 15'te bir inşaat kalan <!--v:veri.vip.ekonomi.15.ucretsiz_dk-->10<!--/v--> dakika daha erken bedavaya biter.

### Harita ve savaş ayrıcalıkları

<!-- URET:tablo:vip.harita -->
| İzin | Yürüyüş hızı | Sonar Enerjisi yenilenme | Saldırı | Savunma | Can | Ek ücretsiz Kriyo çekimi / hafta |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | — | — | — | — | — | — |
| 1 | — | — | — | — | — | — |
| 2 | — | — | — | — | — | — |
| 3 | — | — | — | — | — | 1 |
| 4 | — | %5 | — | — | — | 1 |
| 5 | — | %5 | — | — | — | 1 |
| 6 | %3 | %8 | — | — | — | 2 |
| 7 | %3 | %10 | — | — | — | 2 |
| 8 | %5 | %12 | — | — | — | 3 |
| 9 | %5 | %14 | — | — | — | 3 |
| 10 | %6 | %16 | — | — | — | 4 |
| 11 | %7 | %18 | — | — | — | 4 |
| 12 | %8 | %20 | %1 | — | — | 5 |
| 13 | %9 | %22 | %1 | %1 | — | 5 |
| 14 | %10 | %24 | %2 | %1 | %1 | 6 |
| 15 | %12 | %25 | %2 | %2 | %2 | 7 |
<!-- /URET:tablo:vip.harita -->

- **Sonar Enerjisi** (K-22) yenilenme hızıdır; tavanı değiştirmez. Ücretsiz oyuncu İzin 7'de
  <!--v:veri.vip.harita.7.sonar|yuzde-->%10<!--/v--> alır.
- **Savaş istatistikleri** ancak İzin 12'de başlar ve <!--v:veri.vip.harita.15.saldiri|yuzde-->%2<!--/v-->'de durur.
  Bu, asker sayısı ve kademe farkının yanında küçük kalır. Öneri: Basınç Arenası'nda ve sezon savaşının
  eşleştirmeli maçlarında İzin savaş bonusu uygulanmaz ([09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md)).
- **Ek ücretsiz Kriyo çekimi** Kriyo Salonu'nun günlük ücretsiz çekimine eklenir; haftalık hak pazartesi
  yenilenir, birikmez. Ek çekimler garanti sayacını (30 / 80) ilerletir ([05 · Kahramanlar](05_kahramanlar.md)).

### Derinlik İzni sandığı

Her gün bir sandık; İzin panelinden alınır, ertesi güne birikmez. İçerik o günkü seviyeye göredir.
Kaynak kapsülü **Gelir Saati (GS)** ile yazılır: 1 GS, oyuncunun reaktör seviyesindeki ücretsiz oyuncu
saatlik Titanyum gelirinin bir saati (ve 0,4 katı Biyokütle) kadardır ve çantaya kapsül olarak düşer
([15 · Ekonomi Akışı](15_ekonomi_akisi.md)).

<!-- URET:tablo:vip.sandik -->
| İzin | Hadalit | Araştırma hızlandırma (dk) | Genel hızlandırma (dk) | Kaynak kapsülü (GS) |
| ---: | ---: | ---: | ---: | ---: |
| 0 | — | — | — | — |
| 1 | 10 | 5 | — | 0,5 |
| 2 | 15 | 5 | — | 0,5 |
| 3 | 20 | 10 | — | 1 |
| 4 | 25 | 15 | — | 1 |
| 5 | 30 | 15 | — | 1,5 |
| 6 | 40 | 20 | — | 2 |
| 7 | 50 | 25 | — | 2 |
| 8 | 70 | 30 | 45 | 3 |
| 9 | 80 | 30 | 60 | 3 |
| 10 | 100 | 40 | 75 | 4 |
| 11 | 120 | 40 | 90 | 4 |
| 12 | 140 | 50 | 105 | 5 |
| 13 | 160 | 50 | 120 | 5 |
| 14 | 180 | 60 | 135 | 6 |
| 15 | 200 | 60 | 150 | 6 |
<!-- /URET:tablo:vip.sandik -->

**İzin 1–7 sandığı araştırma hızlandırması verir, genel hızlandırma İzin 8'de başlar.** Böylece ücretsiz
oyuncunun inşaata harcanabilen hızlandırması simülasyonun günde 2 saatinde kalır (döküm:
[12 · Ücretsiz oyuncunun hızlandırma kaynakları](12_syndicate.md#ücretsiz-oyuncunun-hızlandırma-kaynakları));
ödeyenin sandığındaki genel hızlandırma ise onun 6 ve 24 saatlik payının içindedir. Sandığın Hadalit'i
ücretsiz oyuncuya ayda ~750–1.500 HD ekler; [15](15_ekonomi_akisi.md)'in Hadalit dökümü onu ayrı tutar.

### Derinlik İzni dükkânı

İzin 4'te açılır. Fiyatlar Hadalit'tir, sınırlar haftalıktır (pazartesi yenilenir). "Liste değeri", aynı
şeyin Şimdi bitir ya da Kriyo-Kapsül fiyatıdır. Dükkânda **Süperalaşım satılmaz**; kahraman kartları
seçmelidir ve Kriyo-Kapsül'de de çıkan kahramanlardır (yalnız İzin'e özel kahraman yoktur).

<!-- URET:tablo:vip.dukkan -->
| Ürün | İzin | Fiyat (HD) | Liste değeri (HD) | Sınır |
| --- | ---: | ---: | ---: | --- |
| 1 sa genel hızlandırma (4 × 15 dk) | 4 | 90 | 120 | haftada 5 · 10 · 15 (İzin 4 · 8 · 12) |
| Titanyum Kapsülü 8 GS | 4 | 400 | 480 | haftada 3 |
| Kriyo-Kapsül bileti | 5 | 180 | 200 | haftada 5 |
| Sonar Pili · orta (+200 Sonar Enerjisi) | 5 | 100 | — | haftada 3 |
| 3 sa inşaat hızlandırma | 6 | 220 | 270 | haftada 3 · 6 (İzin 6 · 10) |
| 3 sa araştırma hızlandırma | 6 | 220 | 270 | haftada 3 · 6 (İzin 6 · 10) |
| Destansı kahraman kartı (seçmeli) | 7 | 1.500 | 1.500 | haftada 1 |
| 8 sa genel hızlandırma | 8 | 700 | 960 | haftada 2 |
| Teçhizat malzemesi kutusu | 9 | 300 | — | haftada 3 |
| Üs kaplaması · Derin İz | 10 | 5.000 | — | tek sefer |
| Kriyo-Kapsül bileti × 10 | 12 | 1.620 | 1.800 | ayda 2 |
<!-- /URET:tablo:vip.dukkan -->

---

## Ücretsiz oyuncu ve ödeyen

**Ücretsiz oyuncu.** Her gün giren oyuncu ilk ay İzin 4'e, üç ayda İzin 5'e, bir yılda İzin 7'ye gelir
(yukarıdaki ilerleme tablosu). Reaktör ilerlemesine göre (üretim yüzdesi
[15 · Ekonomi Akışı](15_ekonomi_akisi.md)'nın verim çarpanına girer):

<!-- URET:tablo:vip.f2p_reaktor -->
| Reaktör | F2P ulaşır | İzin | Üretim | İnşa hızı | Toplama |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 10 | 7 sa 12 dk | 0 | — | — | — |
| 20 | 4,5. gün | 1 | %1 | — | — |
| 30 | 28,6. gün | 4 | %4 | %3 | %4 |
| 40 | 128,4. gün | 6 | %6 | %5 | %6 |
| 50 | 368,1. gün | 7 | %8 | %7 | %8 |
<!-- /URET:tablo:vip.f2p_reaktor -->

Bir yılın sonunda, İzin 7'de ücretsiz oyuncunun elinde şunlar vardır: <!--v:veri.vip.ekonomi.7.uretim|yuzde-->%8<!--/v--> üretim,
<!--v:veri.vip.ekonomi.7.toplama|yuzde-->%8<!--/v--> toplama, <!--v:veri.vip.ekonomi.7.insa|yuzde-->%7<!--/v--> inşa ve araştırma hızı,
haftada <!--v:veri.vip.harita.7.ek_cekim-->2<!--/v--> ek Kriyo çekimi, günde <!--v:veri.vip.sandik.7.hd-->50<!--/v--> HD'lik sandık
ve dükkânın ilk yedi ürünü. Savaş istatistiği almaz; zaten kimse İzin 12'den önce almaz.

**Aylık kart oyuncusu.** Sendika Sözleşmesi giriş puanını ikiye katlar (tavanda günde 300) ve ayda
≈27,5 $ harcama 2.750 puan ekler. İzin 6'ya ~<!--v:veri.vip.seviyeler.6.kart_gun-->47<!--/v-->. günde, İzin 8'e
~<!--v:veri.vip.seviyeler.8.kart_gun-->172<!--/v-->. günde ulaşır. Yani profilin "VIP 8"i yolculuğun ikinci yarısıdır;
ilk yarıda İzin 5–7 ile Sözleşme'nin +%5'i çalışır.

**Balina.** Ayda ≈1.665 $ harcayan oyuncu İzin 7'ye <!--v:veri.vip.seviyeler.7.balina_gun-->7<!--/v-->. günde, İzin 10'a
<!--v:veri.vip.seviyeler.10.balina_gun-->28<!--/v-->. günde, İzin 13'e <!--v:veri.vip.seviyeler.13.balina_gun-->99<!--/v-->. günde ulaşır;
İzin 15'e ~<!--v:veri.vip.seviyeler.15.balina_gun-->202<!--/v-->. günde varır, yani Reaktör 50'den sonra.

### Simülasyon tutarlılığı

Tempo simülasyonu hız bonusunu `min(%50, %1,5 × Sv) + ek` olarak alır; `ek` aylık kartta
<!--v:veri.f2p_profilleri.profiller.aylik_kart.hiz_yuzde.ek|yuzde-->%15<!--/v-->, balinada
<!--v:veri.f2p_profilleri.profiller.balina.hiz_yuzde.ek|yuzde-->%30<!--/v-->'dur ve 1. günden uygulanır. Gerçekte İzin
yavaş yükselir. Profil bu eğriyle yeniden oynatıldı: `ek` yerine her reaktör seviyesinde o günün İzin inşa
hızı + Sözleşme'nin %5'i, ayrıca İzin'in ek ücretsiz bitirme dakikaları ve ek yardımları kondu.

- **(a)** İzin tabanın **üstüne** eklenir (taban eğri VIP içermez sayılır).
- **(b)** Ücretsiz oyuncunun aynı gündeki İzin hızı tabandan **düşülür** (taban eğri F2P'nin kendi
  İzin'ini zaten içerir sayılır). Ücretsiz oyuncu için (b) profilin kendisidir.

<!-- URET:tablo:vip.sim_denetimi -->
| Profil | Profil ek hızı | İzin + Sözleşme (ort.) | F2P payı düşülünce (ort.) | R50 · profil | R50 · (a) | R50 · (b) | Hedef aralık (gün) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Ücretsiz oyuncu | %0 | %4,9 | %0 | 368,1. gün | 357,8. gün | 368,1. gün | 340–390 |
| Aylık kart + VIP 8 | %15 | %12,4 | %8,4 | 267,2. gün | 267,2. gün | 273,8. gün | 235–295 |
| Balina | %30 | %22 | %18,7 | 147,1. gün | 148,1. gün | 151,1. gün | 140–190 |
<!-- /URET:tablo:vip.sim_denetimi -->

Sonuç: aylık kart oyuncusunun ortalama İzin + Sözleşme hızı profilin %15'inin altında kalır, ama İzin'in
ek yardımları ve ücretsiz bitirmesi farkı kapatır; Reaktör 50 her iki yorumda da hedef aralığındadır
(profil <!--v:hesap.sim.aylik_kart.gun_50|gun-->267,2. gün<!--/v-->). Balina ve ücretsiz oyuncu da aralık içinde kalır.
Profil sayıları değişmeden kalabilir; açıklaması "İzin 5–8 + Sözleşme" olarak okunmalıdır (Açık sorular).
Gün sütunları ve bu denetim üreticinin değil, bir tasarım betiğinin çıktısıdır ve `vip.json`'a yazılmıştır;
eğriler ya da profiller değişirse yeniden hesaplanmalıdır.

---

## Arayüz ve deneyim

**Nerede?** İzin paneli avatardan açılan profilin bir sekmesidir ve The Syndicate'in en altında da durur.
Panel üç bölümdür: seviye ve derinlik çizgisi, ayrıcalık listesi (bir sonraki seviyenin farkı soluk yazıyla),
günlük sandık ve dükkân.

**Ne gösterilmez?** Sohbette rozet, renkli isim, dünya haritasında taç ya da çerçeve, "VIP'ini yükselt"
açılır penceresi, "bir sonraki seviyeye X $" çağrısı. İlerleme çubuğunun altında yalnız "İzin 9'a
48.200 puan" yazar; dolar karşılığı yazılmaz.

**Seviye atlama anı.** Ekran ortasında büyük kutlama yok. Panelde derinlik çizgisi yavaşça bir kademe
aşağı iner, alt satırda yeni ayrıcalıklar belirir; HUD'da hiçbir şey patlamaz. İsteyen posta kutusunda
kısa bir not bulur: "Derinlik İznin 8.650 metreye uzandı."

**Görsel kimlik.** İzin üssün içinden belli olur, üstünden değil:

<!-- URET:tablo:vip.gorunum -->
| Seviye | Görünüm | Nerede |
| --- | --- | --- |
| İzin 0–6 | Görsel fark yok | — |
| İzin 7–9 | Reaktör çekirdeğinde soluk camgöbeği nabız (8 saniyede bir) | Yalnız kendi üs görünümün |
| İzin 10–12 | Aynı nabız, yakınlaştırınca dünya haritasındaki üs modelinde de | Üs ve harita |
| İzin 13–14 | Kubbe tabanında yavaş dönen biyolüminesan halka | Üs ve harita |
| İzin 15 | Halka Challenger lacivertine döner; profil kartında 10.935 m çizgisi | Üs, harita, profil |
<!-- /URET:tablo:vip.gorunum -->

- Işık, reaktörün kendi biyolüminesan paletidir (camgöbeği → lacivert); altın, mor ya da gökkuşağı yoktur.
- Profilde "Görünürlük" anahtarı vardır; kapatan oyuncunun üssü herkese İzin 0 gibi görünür.
- Profil kartında İzin seviyesi varsayılan olarak **gizlidir**; oyuncu isterse açar.

---

## Etik ve uyum

- **Eşit oran.** Her ürün dolar başına aynı puanı verir; "bu pakette 2 kat İzin puanı" yoktur.
- **Satılık seviye yok.** İzin puanı tek başına satılmaz; seviye satın alınmaz, yaşanır.
- **Zaman da yol.** İlk yedi kademe yalnız girişle açılır; ödeyenle ödemeyen arasında açılan fark
  kolaylık ve hızdadır, kahraman ya da birlik türünde değildir.
- **Savaş farkı küçük.** Savaş istatistiği İzin 12'den önce yoktur ve en fazla %2'dir; arenada uygulanmaz (öneri).
- **Baskısız seri.** Kaçırılan gün sayacı yarıya indirir, sıfırlamaz; "serini koru" bildirimi gönderilmez.
- **Kalıcılık sözü.** Seviye düşmez (iade hariç); ayrıcalıklar sonradan "yeniden dengeleme" bahanesiyle
  geri alınmaz. Bir ayrıcalık değişirse en az eşit değerde bir karşılığı verilir.
- **Yaş.** 13–17 yaş hesaplarında İzin aynı çalışır; harcama tavanı ve teklif kuralları
  [12 · Etik ve uyum](12_syndicate.md#etik-ve-uyum)'dadır.

---

## Bugün kodda

| Konu | Durum | Not |
|---|---|---|
| Derinlik İzni (seviye, puan, ayrıcalıklar) | ⬜ | Yok |
| Günlük giriş serisi | ⬜ | Yok; günlük görevler var (K-09) |
| Ücretsiz bitirme | ✅ | `progression.freeFinishSeconds` = 300 sn; İzin eki yok |
| İttifak yardımı | ✅ | `alliance.maxHelps` 10, %1, en az 60 sn; İzin eki yok |
| Kriyo-Kapsül günlük ücretsiz çekim | ✅ | `heroes.pod.freeSeconds` 86.400; İzin ek çekimi yok |
| Hız formülü | 🟡 | Kod yüzdeleri çıkarıyor ve %10'da kesiyor; K-13 bölen formülü bekleniyor |
| Posta kutusu | ⬜ | HUD'da "yakında" |
| Kupon ve ödül verme hattı | ✅ | Sunucuda `CouponService` ve `GameWorld.GrantAsync`; İzin sandığı aynı hattı kullanabilir |

## Uygulama notları

- **Veri.** `shared/catalog/catalog.json` için yeni `vip` bölümü: `levels[]` (puan, ayrıcalık alanları),
  `loginStreak` (30 / 10 / 150, yarılama), `shop[]`, `chest[]`. Ayrıcalık alanları mevcut etki
  adlarıyla eşleşmeli (`buildTime`, `researchTime`, `production:<Kaynak>` …).
- **Sunucu.** `vip_points_ledger` tablosu (hesap, değişim, kaynak, işlem kimliği); seviye defterden
  türetilir, ayrı tutulmaz. İade satırı negatif kayıttır. Giriş serisi sunucu gününe (UTC) bağlıdır;
  istemci saati dikkate alınmaz.
- **Hız.** İzin yüzdeleri K-13 bölenine giren toplam hıza eklenir. Mevcut "yüzdeyi çıkar, %10'da kes"
  kodu değişmeden İzin eklenirse İzin'in hızı o tavanda kaybolur; bölen formülüne geçiş önce gelmeli.
- **Yardım tavanı.** İttifak yardım sayısı = min(20, 10 + ⌊Merkez / 5⌋) + İzin eki. Kodda tavan
  `alliance.maxHelps`'tir; İzin eki tavanın dışında toplanmalı.
- **Sıra ayrıcalıkları** (İzin 8, 9, 11) yeni bir "sıradaki iş" alanı ister; sıradaki işin kaynağı sıraya
  alınırken düşülür, iptalde tamamı iade edilir.
- **Görsel.** Reaktör ışığı reaktör modelinin emissive kanalında bir parametredir; dünya haritasındaki
  üs modeline aynı parametre `vipGlow` (0–3) olarak gider. Görünürlük kapalıysa sunucu 0 gönderir.
- **Testler.** Seri yarılama, eşik sınırları, iade sonrası seviye düşüşü, İzin hızının bölen formülüyle
  birleşimi.

## Esin kaynakları

- Tür içindeki kalıcı, puan biriktirmeli VIP sistemleri: süresiz seviye, girişle büyüyen günlük puan,
  orta seviyede güçlü bir kolaylık sıçraması, yüksek seviyede küçük savaş bonusu, günlük sandık ve dükkân.
- Türdeki süreli VIP sistemleri, burada **olumsuz örnek** olarak: etkinleştirme süresi ve "VIP'in
  bitiyor" baskısı bilinçli olarak alınmadı.
- Derin dalış operasyonlarının derinlik yetkisi kavramı: ad ve 6.000 → 10.935 m kademeleri.

## Açık sorular

1. **Profil açıklaması.** `f2p_profilleri.json` aylık kart profilini "VIP 8" diye anlatıyor; bu eğriyle
   oyuncu İzin 8'e ~172. günde varıyor. Sayılar tutarlı (yukarıdaki denetim), ama açıklama "İzin 5–8 +
   Sözleşme" olarak güncellenmeli mi? Dosya ana yazarın.
2. **Taban eğrinin içeriği.** Simülasyonun `min(%50, %1,5 × Sv)` tabanı ücretsiz oyuncunun İzin hızını
   içeriyor mu? İçeriyorsa (b) yorumu geçerlidir ve aylık kart Reaktör 50'ye
   <!--v:veri.vip.sim_denetimi.1.r50_b-->273,8<!--/v-->. günde varır. 02 · Tempo ve Denge bunu açıkça yazmalı.
3. **İzin 8 sıçraması.** İnşa hızı %7 → %12. Ücretsiz oyuncu bu sıçramayı ikinci yılda yaşar. Daha düz bir
   eğri (%8 → %10) ödeyen ile ödemeyen arasındaki farkı küçültür ama aylık kart profilini zayıflatır.
4. **Ek Kriyo çekimi** İzin 15'te haftada 7: bu, Kriyo Salonu 50'nin 12 saatlik ücretsiz çekimiyle
   birleşince balinaya günde ~3 ücretsiz çekim verir. Kahraman ekonomisiyle ([05](05_kahramanlar.md)) birlikte gözden geçirilmeli.
5. **İzin sandığının Hadalit'i** (ücretsiz oyuncu için ayda ~750–1.500 HD) [15 · Ekonomi Akışı](15_ekonomi_akisi.md)'nın
   Hadalit dökümünde "VIP hariç" tutuluyor; toplam ücretsiz Hadalit gelirine eklenmeli mi, yoksa sandık
   Hadalit'i azaltılmalı mı?
6. **Arena istisnası.** İzin savaş bonusunun arenada ve sezon savaşında kapalı olması 09'un kararı.
7. **VIP tavanı.** 15'in kaynak dengesi tablosu "VIP tavanı 11'de" diyor; Derinlik İzni 15 seviyedir
   (İzin 15 = 10.935 m). 15 bu bölüme göre düzeltilmeli.
8. **Verim çarpanı.** 15, F2P'nin VIP üretim payını R10–R20'de %5, R30–R50'de %10 varsayıyor. Bu eğriyle
   pay R10'da %0–1, R30'da %4, R50'de %8'dir (yukarıdaki reaktör tablosu). Fark küçük ama 15'in döküm
   tablosu bu sayılarla güncellenmeli.
9. **Hesap betiği.** Puan modeli ve simülasyon denetimi bugün kılavuz dışı bir betikte; üreticiye
   (`tools/rehber`) bir `vip` hesabı olarak alınırsa gün sütunları her çalıştırmada güncel kalır.
