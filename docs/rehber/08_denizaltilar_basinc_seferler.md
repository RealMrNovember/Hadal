# 08 · Denizaltılar, Basınç, Seferler

> **Özet.** Üs Hadal Zonu'nun girişinde durur; asıl oyun **aşağıdadır**. Oyuncu dört hangarda dört
> denizaltı tutar; her denizaltı bir yürüyüş kuyruğudur ve bir **Kaptan**, iki **Subay** ve hangar
> kapasitesi kadar asker taşır. Denizaltı İskelesi gövdelerin **basınç sınıfını** belirler: P1
> (<!--v:veri.bilim.basinc.siniflar.0.derinlik_m-->6.500<!--/v--> m) ile P10
> (<!--v:veri.bilim.basinc.siniflar.9.derinlik_m-->10.935<!--/v--> m, çukurun dibi) arasında on sınıf.
> Tek oyunculu kampanya **Derinlik Haritası** her basınç bandına bir bölüm ayırır: 10 bölüm × 10 aşama,
> ışıksız sonar aşamaları, yalnızca gerçek derinlik aralığında karşılaşılan canlılar ve hikâyenin
> kendi boss'ları. Haritanın sisini Radar Merkezi'nin sonar dronları açar; Dron Fabrikası'nın bugünkü
> kurtarma görevleri bu sistemin ilk halkasıdır.

Durum: 🟡. Kodda yalnızca Dron Fabrikası kurtarma görevleri (15 dk / 1 sa / 4 sa) çalışır; denizaltı,
basınç sınıfı, kampanya ve sis yoktur.

---

## 1. Amaç

1. **"Daha derine in" vaadini mekaniğe çevirmek.** Derinlik bir sayı değil, bir kapıdır: gövde sınıfı
   yetmeyen denizaltı o suya giremez. İlerleme, oyuncunun haritada gerçekten aşağı inmesiyle hissedilir.
2. **Gerçek okyanusu öğretmek, ders vermeden.** Basınç sınıfları gerçek derinliklere, karşılaşılan
   canlılar gerçek derinlik aralıklarına, gövde kuralları gerçek denizaltı mühendisliğine dayanır
   ([18 · Bilim Temeli](18_bilim_temeli.md)).
3. **Ücretsiz oyuncuya bir yıllık tek oyunculu omurga vermek.** Derinlik Haritası reaktörle birlikte
   açılır; son bölüm F2P için yılın son çeyreğine düşer.
4. **Yürüyüşü anlamlı kılmak.** Rol (hız, taşıma, savaş, keşif), mürettebat ve oksijen; her sefer bir
   seçimdir.

---

## 2. Denizaltılar

### 2.1 Hangarlar ve yürüyüş kuyrukları

Her **Denizaltı Hangarı** bir denizaltı barındırır ve bir **yürüyüş kuyruğudur**: aynı anda en çok
dört denizaltı haritada olabilir. Hangar tamamlanınca İskele ona bir gövde indirir; ayrıca denizaltı
satın alınmaz, inşa edilmez.

<!-- URET:tablo:denizaltilar.hangarlar -->
| Hangar | Açıldığı reaktör | Ne getirir |
| ---: | ---: | --- |
| 1 | 4 | İlk yürüyüş kuyruğu; Derinlik Haritası |
| 2 | 10 | İkinci kuyruk: toplama ve sefer aynı anda |
| 3 | 18 | Üçüncü kuyruk |
| 4 | 26 | Dördüncü kuyruk: tüm ordu sahaya çıkabilir |
<!-- /URET:tablo:denizaltilar.hangarlar -->

Hangar seviyesi, denizaltının taşıyabileceği asker sayısını belirler:

<!-- URET:tablo:denizaltilar.hangar_kapasite -->
| Hangar Sv | Denizaltı asker kapasitesi | Dört hangar toplamı |
| ---: | ---: | ---: |
| 1 | 100 | 400 |
| 5 | 300 | 1.200 |
| 10 | 900 | 3.600 |
| 15 | 2.000 | 8.000 |
| 20 | 3.800 | 15.200 |
| 25 | 6.000 | 24.000 |
| 30 | 8.500 | 34.000 |
| 35 | 11.500 | 46.000 |
| 40 | 15.000 | 60.000 |
| 45 | 19.000 | 76.000 |
| 50 | 24.000 | 96.000 |
<!-- /URET:tablo:denizaltilar.hangar_kapasite -->

Ordu kapasitesiyle karşılaştırma: [07 · Birlikler ve Savaş](07_birlikler_ve_savas.md#32-askeri-binaların-kapasiteleri).
Dört hangar Sv 50'de <!--v:veri.denizaltilar.hangar_kapasite.satirlar.10.dort_hangar-->96.000<!--/v--> asker
taşır, ordu kapasitesi <!--v:veri.birlikler.kapasite.askeri_kubbe.satirlar.10.ordu-->75.000<!--/v-->'dir.

### 2.2 Gövde rolleri

Her denizaltı dört rolden birini taşır. Rol, gövdenin iç düzenidir (kargo ambarı, sevk motorları,
silah bölmeleri, sonar dizisi); basınç sınıfından bağımsızdır.

<!-- URET:tablo:denizaltilar.govdeler -->
| Gövde | Kimlik | Rol | Hız × | Kapasite × | Yük × | ATK +% | DEF +% | O₂ × | Özel |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Mürekkepbalığı | squid | Hız | 1,3 | 0,85 | 0,8 | 0 | 0 | 0,9 | Geri çağırma süresi yarıya iner; takviye ve ralliye katılmada ilk sırada |
| Mors | walrus | Taşıma | 0,85 | 1 | 1,6 | 0 | 0 | 1 | Toplama hızı +%20; enkaz kurtarma süresi −%33 |
| Köpekbalığı | shark | Savaş | 1 | 1,1 | 0,9 | 5 | 5 | 1,1 | +%5 ATK/DEF ralli ve takviyede de işler |
| Fenerbalığı | anglerfish | Keşif | 1,1 | 0,7 | 0,7 | 0 | 0 | 0,8 | Sonar aşamalarında düşman dizilimini önceden görür; dalış O₂ maliyeti −%20; Tür Kataloğu taraması iki kat puan |
<!-- /URET:tablo:denizaltilar.govdeler -->

- **Mürekkepbalığı** takviye, ralliye yetişme ve kaçış içindir.
- **Mors** toplama ve enkaz kurtarma içindir; Delici ile birlikte en yüksek yükü taşır.
- **Köpekbalığı** savaş içindir; kapasitesi en yüksektir.
- **Fenerbalığı** keşif ve kampanya içindir; sonar aşamalarında düşman dizilimini önceden görür.

**Rol değişimi:**

<!-- URET:tablo:denizaltilar.rol_degisimi -->
| Kural | Değer |
| --- | --- |
| Nerede | Denizaltı İskelesi; denizaltı hangarda ve boşta olmalı |
| Süre | 30 dk × basınç sınıfı (hızlandırılabilir) |
| Bedel | Titanyum, sınıfa göre (onarım tablosu) |
| Bekleme | Aynı denizaltı 24 saatte bir kez |
| İlk seçim | Ücretsiz ve anında |
| Askerler | Değişmez; kapasite düşerse fazlası Askeri Kubbe'ye iner |
<!-- /URET:tablo:denizaltilar.rol_degisimi -->

### 2.3 Mürettebat

<!-- URET:tablo:denizaltilar.murettebat -->
| Koltuk | Sayı | Savaşta ne işler |
| --- | --- | --- |
| Kaptan | 1 | Komuta bonusu tam; bütün yetenekleri; yetenek ağacı ve teçhizat; sınıf yatkınlığı ve fraksiyon sinerjisi |
| Subay | 2 | Komuta bonusu Subay çarpanıyla; aktif, pasif ve sefer yetenekleri; Komuta, Uyanış, ağaç ve teçhizat işlemez; boş bırakılabilir |
| Askerler | Hangar kapasitesine kadar | Dört türden herhangi bir karışım; kademe serbest |
<!-- /URET:tablo:denizaltilar.murettebat -->

Kaptan, iki Subay ve askerler birlikte bir **filodur** ([05 · Kahramanlar](05_kahramanlar.md#32-filo-komutası-savaş)).
Yetenek ağacı ve teçhizat yalnızca Kaptan'da işler; Subaylar yetenekleriyle ve istatistiklerinin bir
kısmıyla (Komuta bonusu ×<!--v:veri.kahramanlar.komuta.subay_carpani-->0,4<!--/v-->) katılır. Bir kahraman
aynı anda tek yerdedir: Komuta Ekibi ya da bir filo. Seferdeki filonun kadrosu kilitlidir. Üste bekleyen
bir filo, oyuncu seçerse **savunma filosu** olarak garnizona komuta eder
([07](07_birlikler_ve_savas.md#47-üs-savunması-ve-sürü-baskını)).

Kaptan'ın sınıfı bir asker türüne **yatkındır**: filodaki askerlerin en az
%<!--v:veri.kahramanlar.sinerji.yatkinlik_esik-->50<!--/v-->'si o türdense o türün ATK, DEF ve HP'si
+%<!--v:veri.kahramanlar.sinerji.yatkinlik_bonus-->5<!--/v--> artar.

<!-- URET:tablo:denizaltilar.uyum -->
| Kaptanın sınıfı | Uyumlu asker türü |
| --- | --- |
| Komutan, Hekim | Zırhlı Dalgıç |
| Avcı, Pilot | Zıpkıncı |
| Mühendis, Bilim insanı | Ark Askeri |
| — | Delici (uyumu yok) |
<!-- /URET:tablo:denizaltilar.uyum -->

### 2.4 Denizaltı modülleri

Her denizaltının üç modül yuvası vardır; modüller teçhizat malzemeleriyle
([17 · Teçhizat](17_techizat.md)) yükseltilir ve denizaltıya bağlıdır (kaptana değil).

<!-- URET:tablo:denizaltilar.moduller -->
| Modül | Malzeme | Etki | En yüksek Sv |
| --- | --- | --- | ---: |
| Basınç Küresi | Titanyum + bazalt camı | Açıkta gövde kaybı −%5 / seviye (en çok −%50) | 10 |
| Yüzdürme Bloğu | Sintaktik köpük | Seyir hızı +%2 / seviye; 5. seviyede bölmeli (zincirleme çöküşe karşı) | 10 |
| Sonar Dizisi | Kitin + masif sülfür | Sonar aşamasında 1 tur önceden görüş; Tür taraması +%10 / seviye | 10 |
<!-- /URET:tablo:denizaltilar.moduller -->

---

## 3. Basınç

### 3.1 Basınç sınıfları

Gövdenin basınç sınıfı, **Denizaltı İskelesi** seviyesinden gelir:

```
basınç sınıfı = 1 + ⌊(İskele Sv − 1) / 5⌋        (P1 … P10)
```

Her sınıf, gövdenin güvenle inebildiği en derin noktadır. Derinlik ve basınç değerleri
`veri/bilim.json`'dan gelir (deniz basıncı, hava basıncı hariç; her ~10 m'de ~1 bar).

| Sınıf | İskele Sv | En erken R | F2P | En derin (m) | ≈ bar | Bu derinlikte | Bölüm |
|---|---|---:|---:|---:|---:|---|---:|
| P1 | <!--v:veri.denizaltilar.basinc_siniflari.0.iskele-->1–5<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.0.reaktor-->4<!--/v--> | ilk dakikalar | <!--v:veri.bilim.basinc.siniflar.0.derinlik_m-->6.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.0.basinc_bar-->665<!--/v--> | <!--v:veri.bilim.basinc.siniflar.0.ornek-->Shinkai 6500 ve yenilenmiş Alvin'in sınırı; hadal bölge 6.000 m'de başlar<!--/v--> | 1 |
| P2 | <!--v:veri.denizaltilar.basinc_siniflari.1.iskele-->6–10<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.1.reaktor-->6<!--/v--> | <!--v:hesap.sim.f2p.seviye.6|gun-->26 dk 14 sn<!--/v--> | <!--v:veri.bilim.basinc.siniflar.1.derinlik_m-->7.000<!--/v--> | <!--v:veri.bilim.basinc.siniflar.1.basinc_bar-->715<!--/v--> | <!--v:veri.bilim.basinc.siniflar.1.ornek-->En derin ahtapot (6.957 m, Java Çukuru)<!--/v--> | 2 |
| P3 | <!--v:veri.denizaltilar.basinc_siniflari.2.iskele-->11–15<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.2.reaktor-->11<!--/v--> | <!--v:hesap.sim.f2p.seviye.11|gun-->9 sa 36 dk<!--/v--> | <!--v:veri.bilim.basinc.siniflar.2.derinlik_m-->7.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.2.basinc_bar-->765<!--/v--> | <!--v:veri.bilim.basinc.siniflar.2.ornek-->Java Çukuru (7.187 m), Factorian Derinliği (7.432 m)<!--/v--> | 3 |
| P4 | <!--v:veri.denizaltilar.basinc_siniflari.3.iskele-->16–20<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.3.reaktor-->16<!--/v--> | <!--v:hesap.sim.f2p.seviye.16|gun-->1,8. gün<!--/v--> | <!--v:veri.bilim.basinc.siniflar.3.derinlik_m-->8.000<!--/v--> | <!--v:veri.bilim.basinc.siniflar.3.basinc_bar-->820<!--/v--> | <!--v:veri.bilim.basinc.siniflar.3.ornek-->En derin dekapod (7.703 m); Mariana salyangoz balığının en bol olduğu 7.000–8.000 m<!--/v--> | 4 |
| P5 | <!--v:veri.denizaltilar.basinc_siniflari.4.iskele-->21–25<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.4.reaktor-->21<!--/v--> | <!--v:hesap.sim.f2p.seviye.21|gun-->6,1. gün<!--/v--> | <!--v:veri.bilim.basinc.siniflar.4.derinlik_m-->8.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.4.basinc_bar-->870<!--/v--> | <!--v:veri.bilim.basinc.siniflar.4.ornek-->Atacama (8.069 m), Meteor (8.265 m), Brownson (8.378 m); balık sınırı 8.200–8.400 m<!--/v--> | 5 |
| P6 | <!--v:veri.denizaltilar.basinc_siniflari.5.iskele-->26–30<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.5.reaktor-->26<!--/v--> | <!--v:hesap.sim.f2p.seviye.26|gun-->15,7. gün<!--/v--> | <!--v:veri.bilim.basinc.siniflar.5.derinlik_m-->9.000<!--/v--> | <!--v:veri.bilim.basinc.siniflar.5.basinc_bar-->925<!--/v--> | <!--v:veri.bilim.basinc.siniflar.5.ornek-->Süperdev amfipodun en derin kaydı (8.931 m)<!--/v--> | 6 |
| P7 | <!--v:veri.denizaltilar.basinc_siniflari.6.iskele-->31–35<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.6.reaktor-->31<!--/v--> | <!--v:hesap.sim.f2p.seviye.31|gun-->35,5. gün<!--/v--> | <!--v:veri.bilim.basinc.siniflar.6.derinlik_m-->9.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.6.basinc_bar-->975<!--/v--> | <!--v:veri.bilim.basinc.siniflar.6.ornek-->Izu-Ogasawara'da dev amfipod örnekleri (9.450 m)<!--/v--> | 7 |
| P8 | <!--v:veri.denizaltilar.basinc_siniflari.7.iskele-->36–40<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.7.reaktor-->36<!--/v--> | <!--v:hesap.sim.f2p.seviye.36|gun-->79,2. gün<!--/v--> | <!--v:veri.bilim.basinc.siniflar.7.derinlik_m-->10.000<!--/v--> | <!--v:veri.bilim.basinc.siniflar.7.basinc_bar-->1.030<!--/v--> | <!--v:veri.bilim.basinc.siniflar.7.ornek-->Kuril-Kamçatka (9.604 m), Izu-Ogasawara (9.826 m), Nereus'un kaybolduğu 9.990 m<!--/v--> | 8 |
| P9 | <!--v:veri.denizaltilar.basinc_siniflari.8.iskele-->41–45<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.8.reaktor-->41<!--/v--> | <!--v:hesap.sim.f2p.seviye.41|gun-->147,7. gün<!--/v--> | <!--v:veri.bilim.basinc.siniflar.8.derinlik_m-->10.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.8.basinc_bar-->1.080<!--/v--> | <!--v:veri.bilim.basinc.siniflar.8.ornek-->Kermadec / Scholl Derinliği (10.047 m)<!--/v--> | 9 |
| P10 | <!--v:veri.denizaltilar.basinc_siniflari.9.iskele-->46–50<!--/v--> | <!--v:veri.denizaltilar.basinc_siniflari.9.reaktor-->46<!--/v--> | <!--v:hesap.sim.f2p.seviye.46|gun-->255,8. gün<!--/v--> | <!--v:veri.bilim.basinc.siniflar.9.derinlik_m-->10.935<!--/v--> | <!--v:veri.bilim.basinc.siniflar.9.basinc_bar-->1.126<!--/v--> | <!--v:veri.bilim.basinc.siniflar.9.ornek-->Filipin (10.540 m), Tonga (10.816 m), Mariana (10.935 m); Trieste, Deepsea Challenger, Limiting Factor, Fendouzhe<!--/v--> | 10 |

Üs, Hadal Zonu'nun başladığı yaklaşık <!--v:veri.bilim.derinlik_bolgeleri.hadopelajik.min_m-->6.000<!--/v-->
m'dedir (K-03); P1 üssün çevresini, P10 çukurun en derin noktasını kapsar. Harita kuşaklarının hangi
sınıfı istediği [09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md)'dedir. Her sunucu kendi
çukurunun gerçek derinliklerini gösterir (K-24); mekanik sınıf sırasını kullanır, böylece her sunucu
eşit oynanır.

### 3.2 İskele ve sınıf yükseltme

- **Tek İskele** vardır (Reaktör <!--v:veri.binalar.binalar.submarine_dock.adet.0-->4<!--/v-->) ve bütün denizaltıların sınıfı İskele'nin sınıfıdır. İskele bir
  sınıf eşiğini geçince (Sv 6, 11, … 46) hangardaki her denizaltı ücretsiz ve anında **yeniden
  donatılır**; seferdeki denizaltı dönünce yükselir.
- İskele reaktör önkoşul zincirindedir ([02](02_tempo_ve_denge.md)); sınıf ilerlemesi böylece
  reaktörün ritmini izler.
- İskele, onarım ve rol değişiminin yapıldığı yerdir; İskele yükseltilirken onarım durmaz.

### 3.3 Sınıf açığı

**Açık** = gidilen bölgenin istediği sınıf − denizaltının sınıfı.

<!-- URET:tablo:denizaltilar.acik -->
| Sınıf açığı | Giriş | Etki |
| --- | --- | --- |
| 0 (sınıf yeterli) | Serbest | Yok |
| 1 sınıf | Serbest, uyarıyla | Gövde bütünlüğü dakikada −%2; askerler −%15 DEF |
| 2 sınıf ve üstü | Engelli | Rota bu bölgeden geçemez |
<!-- /URET:tablo:denizaltilar.acik -->

- Bir sınıf açık **bilinçli bir risktir**: kısa bir geçiş, açıktaki bir enkazı kurtarmak, ittifak
  rallisine yetişmek. Gövde dakikada %<!--v:veri.denizaltilar.govde.acik_kaybi-->2<!--/v--> aşınır;
  tam bir gövde açıkta yaklaşık 50 dakika dayanır.
- İki sınıf açık **hiçbir koşulda** girilemez; rota planlayıcı o bölgeyi kapalı gösterir.
- Açık, savaşta askerlere −%<!--v:veri.denizaltilar.govde.acik_def_cezasi-->15<!--/v--> DEF olarak
  yansır (basınç stresi).

### 3.4 Gövde bütünlüğü, tahliye ve onarım

- **Gövde bütünlüğü** %100'den başlar. Açıkta geçen her dakika, bazı boss yetenekleri ve doğa olayları
  ([13](13_etkinlikler_ve_sezonlar.md)) onu düşürür. Savaş kayıpları gövdeye değil askere yazılır.
- **Acil tahliye:** bütünlük %0'a inerse denizaltı balast bırakır ve yükselerek üsse döner. Sefer
  iptal olur, taşınan yük korunur, askerler **sağ salim** iner. Hiçbir denizaltı "kaybolmaz";
  tahliyeden sonra hangarda onarım bekler.
- **Onarım:** hangardaki denizaltı saatte %<!--v:veri.denizaltilar.govde.pasif_onarim-->20<!--/v-->
  ücretsiz onarılır (5 saatte tam). Oyuncu isterse Titanyumla anında onarır:

<!-- URET:tablo:denizaltilar.onarim -->
| Sınıf | Hangarda onarım %/sa | Hızlı onarım Ti / %1 | Rol değişimi Ti | Rol değişimi süresi | Seyir O₂/sa (gövde) |
| --- | ---: | ---: | ---: | ---: | ---: |
| P1 | 20 | 1 | 18 | 30 dk | 50 |
| P2 | 20 | 2 | 115 | 1 sa | 120 |
| P3 | 20 | 15 | 735 | 1 sa 30 dk | 250 |
| P4 | 20 | 70 | 3.620 | 2 sa | 450 |
| P5 | 20 | 265 | 13.100 | 2 sa 30 dk | 800 |
| P6 | 20 | 750 | 37.400 | 3 sa | 1.300 |
| P7 | 20 | 1.800 | 90.000 | 3 sa 30 dk | 2.000 |
| P8 | 20 | 3.790 | 189.000 | 4 sa | 3.000 |
| P9 | 20 | 7.580 | 379.000 | 4 sa 30 dk | 4.300 |
| P10 | 20 | 15.200 | 758.000 | 5 sa | 6.000 |
<!-- /URET:tablo:denizaltilar.onarim -->

---

## 4. Seyir ve oksijen

```
seyir süresi (sn) = mesafe (kare) × 60 / (en yavaş tür hızı / 100 × gövde hız çarpanı × (1 + Σhız%))
seyir O₂ / saat   = gövde O₂ (sınıf) × gövde O₂ çarpanı + asker sayısı × asker bakım O₂ × 2
```

- Bir yürüyüşün hızı **en yavaş asker türünün** hızıdır (Zırhlı 80, Zıpkıncı 120, Ark 100, Delici 70;
  [07](07_birlikler_ve_savas.md#21-dört-tür)). Araştırma, kahraman ve ittifak bonusları K-13 böleniyle
  eklenir.
- Mesafe Chebyshev karesidir (K-02). 100 karelik yol (kenardan merkeze) taban hızda 100 dakikadır.
- Oksijen seyir boyunca saatlik düşülür; seferdeki asker üstekinin iki katı solur
  ([07](07_birlikler_ve_savas.md#25-bakım)). Oksijen biterse yeni sefer başlamaz; yoldaki denizaltılar
  görevini bitirip döner.

<!-- URET:tablo:denizaltilar.seyir_ornekleri -->
| Denizaltı | Hız çarpanı | 50 kare (bonussuz) |
| --- | ---: | ---: |
| Zıpkıncı + Mürekkepbalığı | 1,56 | 32 dk 3 sn |
| Ark Askeri + Köpekbalığı | 1 | 50 dk |
| Zırhlı Dalgıç + Köpekbalığı | 0,8 | 1 sa 2 dk |
| Delici + Mors | 0,59 | 1 sa 24 dk |
<!-- /URET:tablo:denizaltilar.seyir_ornekleri -->

Haritadaki PvE saldırıları ayrıca **Sonar Enerjisi** harcar (K-22); sayıları
[09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md)'dedir. Derinlik Haritası kampanyası Sonar
Enerjisi değil Oksijen harcar (aşağıda).

---

## 5. Derinlik Haritası

### 5.1 Yapı

**Derinlik Haritası**, Radar Merkezi'nin ekranından açılan tek oyunculu kampanyadır: **10 bölüm × 10
aşama**. Her bölüm bir basınç bandıdır ve o sınıfın gövdesini ister; bir önceki bölümün boss'u
yenilmeden sonraki açılmaz.

- **Dalış:** bir aşama, hangardaki bir denizaltının mürettebatı ve askerleriyle anında çözülür
  (denizaltı haritaya çıkmaz, ama dalış sırasında başka yürüyüşe de çıkamaz). Savaş, 07'deki çözümle
  aynıdır; kayıplar PvE kuralıyla yaralıdır.
- **Dalış maliyeti:** aşama başına Oksijen (bölüm tablosu). Kaybedilen dalışta Oksijenin %80'i iade
  edilir; Fenerbalığı gövdesi maliyeti −%20 indirir.
- **Boss aşaması** (10.) en çok iki denizaltıyla dalınır ve <!--v:veri.birlikler.savas.boss_tur_sayisi-->15<!--/v--> tur sürer.
- **Ödüllü dalış:** günde <!--v:veri.denizaltilar.kampanya.odullu_dalis-->3<!--/v--> dalış kaynak ödülü verir
  (ilk geçiş ya da tekrar, kazanılan dalış); ödül ve günlük sayı [15 · Ekonomi Akışı](15_ekonomi_akisi.md#34-sefer)
  bütçesindedir. Hak dolunca dalışlar ilerleme ve madalya için sürer, kaynak vermez; kampanya hiçbir
  zaman kilitlenmez.
- Madalyalar her koşul için bir kez kazanılır; boss'un tek seferlik ödülleri ilk zaferde verilir.

### 5.2 Bölüm 0 · İniş

Kampanya, oyunun ilk dakikalarında yüzeyden üsse inişle başlar (K-03: eski 1.000–7.000 m katmanları
hikâyenin girişidir). İniş savaşsızdır, Oksijen istemez ve beş karşılaşmayla ışıklı sulardan Hadal
Eşiği'ne kadar iner; oyuncu tarama, gözleme, ışık ve sonar kurallarını burada öğrenir. Türler yalnızca
kendi derinlik bölgelerinde görünür (`bilim.json` → `derinlik_bolgeleri`, `turler`).

<!-- URET:tablo:denizaltilar.kampanya.giris -->
| Aşama | Ad | Bölge | Karşılaşılan türler | Ne öğretir |
| --- | --- | --- | --- | --- |
| 0.1 | Alacakaranlık | Mezopelajik Bölge (Alacakaranlık Bölgesi) | Fıçı Göz, Dev Sifonofor, Fırfırlı Köpekbalığı | Son ışık; biyoışıldama ilk kez görünür. Işık kullanımı ve sonar öğretilir. |
| 0.2 | Kalamarların Derinliği | Mezopelajik Bölge (Alacakaranlık Bölgesi) | Dev Kalamar, Koloss Kalamar, Vampir Kalamar | Karşılaşma eylemleri (Tara, Gözle, Kaçın) öğretilir; hiçbir canlı hedef değildir. |
| 0.3 | Gece Yarısı | Batipelajik Bölge (Gece Yarısı Bölgesi) | Kambur Fener Balığı, Dev İzopod | Kalıcı karanlık; basınç göstergesi ve gövde bütünlüğü öğretilir. |
| 0.4 | Baca Sahası | Batipelajik Bölge (Gece Yarısı Bölgesi) | Dev Tüp Kurdu, Pompeii Kurdu, Pullu Ayaklı Salyangoz, Yeti Yengeci | Kemosentez ve üssün enerji fikri; baca canlıları yalnızca gözlenir. |
| 0.5 | Abisal Düzlük | Abisopelajik Bölge (Abisal Bölge) | Süperdev Amfipod, Ksenofiyofor (dev tek hücreli), Galathea Kusk Yılanbalığı | Üssün kurulacağı Hadal Eşiği'ne varış; Derinlik Haritası'nın 1. bölümü görünür olur. |
<!-- /URET:tablo:denizaltilar.kampanya.giris -->

İnişte taranan türler Tür Kataloğu'nun ilk sayfasını doldurur. 4.000 m'den derin olan haritada bu
türler yoktur ([09](09_dunya_haritasi_ve_pvp.md#91-tür-kataloğu)); onlarla yalnızca burada karşılaşılır.
İniş istenildiği kadar yeniden izlenebilir.

### 5.3 Bölümler

<!-- URET:tablo:denizaltilar.kampanya.bolum_tanim -->
| Bölüm | Ad | Sınıf | Yer |
| ---: | --- | --- | --- |
| 1 | Hadal Eşiği | P1 | Abisal düzlüğün bittiği, çukurun ilk basamaklarının başladığı geçiş kuşağı. |
| 2 | Dalan Levha | P2 | Okyanus levhasının çukura doğru büküldüğü dış yamaç; düz tortul tabakalar ve ilk faylar. |
| 3 | Fay Basamakları | P3 | Bükülen levhanın kırılmasıyla oluşan basamaklı sırtlar ve çukurlar (horst ve graben). |
| 4 | Soluk Sürüler | P4 | Salyangoz balıklarının en kalabalık olduğu kuşak; düşen her leşin çevresinde soluk sürüler toplanır. |
| 5 | Balık Sınırı | P5 | Balıkların yaşayabildiği en derin kuşak; bunun altında omurgalı görülmez. |
| 6 | Sessiz Derinlik | P6 | Balıksız derinlik: amfipodlar, deniz hıyarları ve tortul canlıları; sonarda yalnızca küçük yankılar. |
| 7 | Huni Tabanı | P7 | Çukur duvarlarından kayan tortunun ve organik maddenin toplandığı derin havza. |
| 8 | Soğuk Sızıntılar | P8 | Tortudan metan ve sülfür sızan noktalar; güneşsiz, kemosentetik yaşam adaları. |
| 9 | Dip Tortulu | P9 | Çukurun en derin havzalarını kaplayan ince çamur; dev tek hücrelilerin bahçeleri. |
| 10 | Çukurun Dibi | P10 | Çukurun en derin noktası; Mariana sunucusunda Challenger Derinliği. Çekirdek'in kapısı. |
<!-- /URET:tablo:denizaltilar.kampanya.bolum_tanim -->

Her bölümün derinlik bandı ve açıldığı gün:

| Bölüm | Üst (m) | Alt (m) | Sınıf | Açılış (R) | F2P |
|---:|---:|---:|---|---:|---:|
| 1 · Hadal Eşiği | <!--v:veri.bilim.derinlik_bolgeleri.hadopelajik.min_m-->6.000<!--/v--> | <!--v:veri.bilim.basinc.siniflar.0.derinlik_m-->6.500<!--/v--> | P1 | 5 | <!--v:hesap.sim.f2p.seviye.5|gun-->4 dk 48 sn<!--/v--> |
| 2 · Dalan Levha | <!--v:veri.bilim.basinc.siniflar.0.derinlik_m-->6.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.1.derinlik_m-->7.000<!--/v--> | P2 | 6 | <!--v:hesap.sim.f2p.seviye.6|gun-->26 dk 14 sn<!--/v--> |
| 3 · Fay Basamakları | <!--v:veri.bilim.basinc.siniflar.1.derinlik_m-->7.000<!--/v--> | <!--v:veri.bilim.basinc.siniflar.2.derinlik_m-->7.500<!--/v--> | P3 | 11 | <!--v:hesap.sim.f2p.seviye.11|gun-->9 sa 36 dk<!--/v--> |
| 4 · Soluk Sürüler | <!--v:veri.bilim.basinc.siniflar.2.derinlik_m-->7.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.3.derinlik_m-->8.000<!--/v--> | P4 | 16 | <!--v:hesap.sim.f2p.seviye.16|gun-->1,8. gün<!--/v--> |
| 5 · Balık Sınırı | <!--v:veri.bilim.basinc.siniflar.3.derinlik_m-->8.000<!--/v--> | <!--v:veri.bilim.basinc.siniflar.4.derinlik_m-->8.500<!--/v--> | P5 | 21 | <!--v:hesap.sim.f2p.seviye.21|gun-->6,1. gün<!--/v--> |
| 6 · Sessiz Derinlik | <!--v:veri.bilim.basinc.siniflar.4.derinlik_m-->8.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.5.derinlik_m-->9.000<!--/v--> | P6 | 26 | <!--v:hesap.sim.f2p.seviye.26|gun-->15,7. gün<!--/v--> |
| 7 · Huni Tabanı | <!--v:veri.bilim.basinc.siniflar.5.derinlik_m-->9.000<!--/v--> | <!--v:veri.bilim.basinc.siniflar.6.derinlik_m-->9.500<!--/v--> | P7 | 31 | <!--v:hesap.sim.f2p.seviye.31|gun-->35,5. gün<!--/v--> |
| 8 · Soğuk Sızıntılar | <!--v:veri.bilim.basinc.siniflar.6.derinlik_m-->9.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.7.derinlik_m-->10.000<!--/v--> | P8 | 36 | <!--v:hesap.sim.f2p.seviye.36|gun-->79,2. gün<!--/v--> |
| 9 · Dip Tortulu | <!--v:veri.bilim.basinc.siniflar.7.derinlik_m-->10.000<!--/v--> | <!--v:veri.bilim.basinc.siniflar.8.derinlik_m-->10.500<!--/v--> | P9 | 41 | <!--v:hesap.sim.f2p.seviye.41|gun-->147,7. gün<!--/v--> |
| 10 · Çukurun Dibi | <!--v:veri.bilim.basinc.siniflar.8.derinlik_m-->10.500<!--/v--> | <!--v:veri.bilim.basinc.siniflar.9.derinlik_m-->10.935<!--/v--> | P10 | 46 | <!--v:hesap.sim.f2p.seviye.46|gun-->255,8. gün<!--/v--> |

İlk bölüm Reaktör 5'te açılır çünkü askerler orada gelir (Askeri Kubbe ve Tatbikat Havuzu); İskele
P1'i Reaktör 4'te verir. İlk dört bölüm oyunun ilk haftasında öğretici bir ritimle geçer; 7–10. bölümler
aylara yayılır ve geç oyunun tek oyunculu omurgası olur.

Bölümlerin dokusu, o bandın gerçek coğrafyasından gelir:

- **Balık Sınırı (5)** balıkların yaşayabildiği son banttır. Bilimsel açıklama
  [18](18_bilim_temeli.md)'dedir: <!--v:veri.bilim.kurallar.balik_derinlik_siniri.ad-->Balık derinlik sınırı<!--/v-->
  (<!--v:veri.bilim.kurallar.balik_derinlik_siniri.deger_m.0-->8.200<!--/v-->–<!--v:veri.bilim.kurallar.balik_derinlik_siniri.deger_m.1-->8.400<!--/v--> m).
  Bölümün son karşılaşması, menzilinin dibindeki bir salyangoz balığıdır; 6. bölümden itibaren sonarda
  balık yankısı yoktur.
- **Sessiz Derinlik (6)** ve sonrası yalnızca omurgasızların ve tek hücrelilerin dünyasıdır; müzik
  seyrekleşir, yankılar küçülür.
- **Çukurun Dibi (10)** Mariana sunucusunda Challenger Derinliği'dir; diğer sunucularda kendi çukurunun
  en derin noktası (K-24). Çekirdek'in kapısı buradadır ([14 · Son Oyun](14_son_oyun.md)).

### 5.4 Aşama şablonu

Her bölüm aynı ritmi izler: tanış, öğren, karanlıkta sına, boss.

<!-- URET:tablo:denizaltilar.kampanya.asamalar -->
| Aşama | Tür | Işık | Ne olur |
| ---: | --- | --- | --- |
| 1 | Savaş | Açık | Bölümün düşmanlarıyla tanışma |
| 2 | Savaş | Açık | Karşı türü öğreten düşman dizilimi |
| 3 | Karşılaşma | Açık | Bölümün derinliğinde yaşayan bir tür: tara, gözle ya da kaçın |
| 4 | Savaş | Açık | İki dalga |
| 5 | Sonar | Kapalı | Düşman dizilimi gizli; yalnızca sonar yankıları |
| 6 | Enkaz | Açık | Savaşsız kurtarma; bir kerelik malzeme ödülü |
| 7 | Savaş | Açık | Seçkin düşman (boss'un muhafızları) |
| 8 | Sonar | Kapalı | Gizli dizilim + karşılaşma: savaş alanındaki bir canlıyı rahatsız etmemek |
| 9 | Karşılaşma | Kapalı | Işıksız gözlem; Fenerbalığı gövdesiyle kolaylaşır |
| 10 | Boss | Açık | En çok iki denizaltı, 15 tur |
<!-- /URET:tablo:denizaltilar.kampanya.asamalar -->

### 5.5 Sonar aşamaları

Işık suda birkaç on metrede söner; derinde yol sesle bulunur. **Sonar aşamalarında** ışıklar
kapalıdır:

- Düşman dizilimi gizlidir; oyuncu yalnızca yankıları görür: toplam büyüklük (küçük / orta / büyük
  sürü) ve saf (ön / arka). Türler 1. turdan sonra açılır.
- **Fenerbalığı** gövdesi ya da Sonar Dizisi modülü dizilimi önceden gösterir.
- Işığı açmak serbesttir ama "ışık açmadan kazan" madalyası kaybedilir ve **Basınç Hayaletleri**
  ışığa doğru gelir (ilk tur hedef alınamazlar).
- 8. aşama bir karşılaşmayı da içerir: savaş alanında Tür Kataloğu'ndan bir canlı vardır. Ark Askeri kullanılırsa
  canlı ürker ve "rahatsız etmeden" madalyası kaybedilir; canlıya hiçbir koşulda hasar verilmez.
- Bölümlerin sonar aşamalarından birinde ekranın önünden dev bir yankı geçer: uzaktan bir Leviathan.
  Savaşmaz; yalnızca o bölümün boss'unu haber verir.

### 5.6 Karşılaşmalar ve Tür Kataloğu

Karşılaşma aşamalarında gerçek bir tür vardır ve **asla hedef değildir** (K-25). Oyuncu üç eylemden
birini seçer:

<!-- URET:tablo:denizaltilar.karsilasma_eylemleri -->
| Eylem | Koşul | Sonuç | Maliyet |
| --- | --- | --- | --- |
| Tara | Herhangi bir denizaltı | Tür Kataloğu kaydı; 1 madalya | Dalış O₂ |
| Gözle | Işıklar kapalı, 3 tur hareketsiz | Katalog notu + davranış kaydı; 1 madalya | Dalış O₂ ×1,5 |
| Kaçın | Rota değiştir | Aşama geçilir, madalya yok | Dalış O₂ ×0,5 |
<!-- /URET:tablo:denizaltilar.karsilasma_eylemleri -->

Türler yalnızca **gerçek derinlik aralıklarıyla örtüşen** bölümlere konur; üretici araç bunu
`bilim.json` ile denetler.

<!-- URET:tablo:denizaltilar.fauna -->
| Tür | bilim.json | Bölüm | Önerilen karşılaşma | Not |
| --- | --- | --- | --- | --- |
| Büyük Yüzgeçli Kalamar | bigfin_squid | 1 | Işıksız gözlem | Uzun kolları akıntıda asılı durur; ışık açılırsa uzaklaşır. |
| Dumbo Ahtapot | dumbo_octopus | 1, 2 | Gözle | Kulak benzeri yüzgeçleriyle süzülür; 2. bölüm, ahtapotların görüldüğü en derin kuşaktır. |
| Süperdev Amfipod | supergiant_amphipod | 1, 4, 6 | Tara | Leşe toplanan iri amfipodlar; 6. bölümde türün en derin kaydına yakın. |
| Galathea Kusk Yılanbalığı | galathea_cusk_eel | 2 | Tara | Kaydı tartışmalı bir tür; tarama Katalog'a 'doğrulanmamış' notuyla düşer. |
| Kermadec Salyangoz Balığı | kermadec_snailfish | 2, 3 | Gözle | Yemli kameraların etrafında toplanan soluk, jelatinimsi balıklar. |
| Mariana Salyangoz Balığı | mariana_snailfish | 3, 5 | Gözle | Hadal çukurların en bol balığı; 5. bölümde menzilinin dibindedir. |
| Derin Deniz Karidesi | deep_prawn | 3, 4 | Tara | Dekapodların ulaştığı en derin kuşak 4. bölümdedir. |
| Japon Çukuru Salyangoz Balığı | japan_trench_snailfish | 4, 5 | Işıksız gözlem | 5. bölümde balık sınırına en yakın gözlem. |
| Dev Hadal Amfipodu | hadal_amphipod | 5, 7, 8, 9, 10 | Tara | Balık sınırının altındaki çöpçü kabuklu; Çukurun Dibi'ne kadar iner. |
| Hadal Deniz Hıyarları | hadal_sea_cucumbers | 6, 7, 8, 9, 10 | Gözle | Tortuyu yavaşça eleyen deniz hıyarları; sessiz derinliğin baskın hayvanı. |
| Ksenofiyofor (dev tek hücreli) | xenophyophore | 7, 8, 9, 10 | Işıksız gözlem | Tek hücreli ama avuç büyüklüğünde; dokunulmaz, yalnızca gözlenir. |
<!-- /URET:tablo:denizaltilar.fauna -->

Türlerin bilinen derinlik aralıkları (bilim.json; "—" bilinmeyen ya da sığ ucu tanımsız):

| Tür | En sığ (m) | En derin (m) | Bölüm |
|---|---:|---:|---|
| <!--v:veri.bilim.turler.bigfin_squid.ad-->Büyük Yüzgeçli Kalamar<!--/v--> | <!--v:veri.bilim.turler.bigfin_squid.min_m-->—<!--/v--> | <!--v:veri.bilim.turler.bigfin_squid.max_m-->6.212<!--/v--> | <!--v:veri.denizaltilar.fauna.bigfin_squid.bolum-->1<!--/v--> |
| <!--v:veri.bilim.turler.dumbo_octopus.ad-->Dumbo Ahtapot<!--/v--> | <!--v:veri.bilim.turler.dumbo_octopus.min_m-->—<!--/v--> | <!--v:veri.bilim.turler.dumbo_octopus.max_m-->6.957<!--/v--> | <!--v:veri.denizaltilar.fauna.dumbo_octopus.bolum-->1, 2<!--/v--> |
| <!--v:veri.bilim.turler.supergiant_amphipod.ad-->Süperdev Amfipod<!--/v--> | <!--v:veri.bilim.turler.supergiant_amphipod.min_m-->3.890<!--/v--> | <!--v:veri.bilim.turler.supergiant_amphipod.max_m-->8.931<!--/v--> | <!--v:veri.denizaltilar.fauna.supergiant_amphipod.bolum-->1, 4, 6<!--/v--> |
| <!--v:veri.bilim.turler.galathea_cusk_eel.ad-->Galathea Kusk Yılanbalığı<!--/v--> | <!--v:veri.bilim.turler.galathea_cusk_eel.min_m-->3.110<!--/v--> | <!--v:veri.bilim.turler.galathea_cusk_eel.max_m-->7.965<!--/v--> | <!--v:veri.denizaltilar.fauna.galathea_cusk_eel.bolum-->2<!--/v--> |
| <!--v:veri.bilim.turler.kermadec_snailfish.ad-->Kermadec Salyangoz Balığı<!--/v--> | <!--v:veri.bilim.turler.kermadec_snailfish.min_m-->6.472<!--/v--> | <!--v:veri.bilim.turler.kermadec_snailfish.max_m-->7.561<!--/v--> | <!--v:veri.denizaltilar.fauna.kermadec_snailfish.bolum-->2, 3<!--/v--> |
| <!--v:veri.bilim.turler.mariana_snailfish.ad-->Mariana Salyangoz Balığı<!--/v--> | <!--v:veri.bilim.turler.mariana_snailfish.min_m-->6.198<!--/v--> | <!--v:veri.bilim.turler.mariana_snailfish.max_m-->8.178<!--/v--> | <!--v:veri.denizaltilar.fauna.mariana_snailfish.bolum-->3, 5<!--/v--> |
| <!--v:veri.bilim.turler.deep_prawn.ad-->Derin Deniz Karidesi<!--/v--> | <!--v:veri.bilim.turler.deep_prawn.min_m-->—<!--/v--> | <!--v:veri.bilim.turler.deep_prawn.max_m-->7.703<!--/v--> | <!--v:veri.denizaltilar.fauna.deep_prawn.bolum-->3, 4<!--/v--> |
| <!--v:veri.bilim.turler.japan_trench_snailfish.ad-->Japon Çukuru Salyangoz Balığı<!--/v--> | <!--v:veri.bilim.turler.japan_trench_snailfish.min_m-->6.579<!--/v--> | <!--v:veri.bilim.turler.japan_trench_snailfish.max_m-->8.336<!--/v--> | <!--v:veri.denizaltilar.fauna.japan_trench_snailfish.bolum-->4, 5<!--/v--> |
| <!--v:veri.bilim.turler.hadal_amphipod.ad-->Dev Hadal Amfipodu<!--/v--> | <!--v:veri.bilim.turler.hadal_amphipod.min_m-->8.000<!--/v--> | <!--v:veri.bilim.turler.hadal_amphipod.max_m-->10.897<!--/v--> | <!--v:veri.denizaltilar.fauna.hadal_amphipod.bolum-->5, 7, 8, 9, 10<!--/v--> |
| <!--v:veri.bilim.turler.hadal_sea_cucumbers.ad-->Hadal Deniz Hıyarları<!--/v--> | <!--v:veri.bilim.turler.hadal_sea_cucumbers.min_m-->6.000<!--/v--> | <!--v:veri.bilim.turler.hadal_sea_cucumbers.max_m-->10.730<!--/v--> | <!--v:veri.denizaltilar.fauna.hadal_sea_cucumbers.bolum-->6, 7, 8, 9, 10<!--/v--> |
| <!--v:veri.bilim.turler.xenophyophore.ad-->Ksenofiyofor (dev tek hücreli)<!--/v--> | <!--v:veri.bilim.turler.xenophyophore.min_m-->—<!--/v--> | <!--v:veri.bilim.turler.xenophyophore.max_m-->10.641<!--/v--> | <!--v:veri.denizaltilar.fauna.xenophyophore.bolum-->7, 8, 9, 10<!--/v--> |

Tür Kataloğu'nun ödülleri ve koleksiyon ekranı [16 · Kronik, Köken, Profil](16_kronik_kokenler_profil.md)'dedir;
harita üzerindeki ortam canlıları [09](09_dunya_haritasi_ve_pvp.md)'dadır.

### 5.7 Düşmanlar, boss'lar ve önerilen güç

Bütün düşmanlar hikâyenin kendi yaratıklarıdır; savaşta bir asker türü gibi sayılırlar
([07](07_birlikler_ve_savas.md#47-üs-savunması-ve-sürü-baskını)).

<!-- URET:tablo:denizaltilar.kampanya.bolumler -->
| Bölüm | Ad | Sınıf | İskele Sv | Hedef R | Düşmanlar | Boss (10. aşama) | Dalış O₂ | Önerilen güç: 1. aşama | 9. aşama | Boss (2 denizaltı) |
| ---: | --- | --- | ---: | --- | --- | --- | ---: | ---: | ---: | ---: |
| 1 | Hadal Eşiği | P1 | 1 | 5–7 | Uçurum Sürüngenleri | Sürüngen Anası | 150 | 810 | 1.610 | 2.710 |
| 2 | Dalan Levha | P2 | 6 | 7–10 | Uçurum Sürüngenleri, Kum Kazıcıları | Burgu Ağzı | 300 | 1.270 | 4.450 | 7.490 |
| 3 | Fay Basamakları | P3 | 11 | 11–15 | Kum Kazıcıları, Uçurum Sürüngenleri | Demir Leviathan | 1.000 | 4.090 | 14.800 | 25.000 |
| 4 | Soluk Sürüler | P4 | 16 | 16–20 | Basınç Hayaletleri, Uçurum Sürüngenleri | Hayalet Filo | 2.000 | 13.200 | 41.200 | 69.300 |
| 5 | Balık Sınırı | P5 | 21 | 21–25 | Siren Sürüsü, Basınç Hayaletleri | Siren Kraliçesi | 4.000 | 35.500 | 91.200 | 154.000 |
| 6 | Sessiz Derinlik | P6 | 26 | 26–30 | Siren Sürüsü, Uçurum Sürüngenleri | Kabuk Devi | 6.000 | 77.400 | 179.000 | 302.000 |
| 7 | Huni Tabanı | P7 | 31 | 31–35 | Kadim Muhafızlar, Basınç Hayaletleri | Kadim Muhafız | 10.000 | 151.000 | 332.000 | 559.000 |
| 8 | Soğuk Sızıntılar | P8 | 36 | 36–40 | Kadim Muhafızlar, Siren Sürüsü | İkiz Leviathan | 15.000 | 274.000 | 579.000 | 974.000 |
| 9 | Dip Tortulu | P9 | 41 | 41–45 | Kadim Muhafızlar, Uçurum Sürüngenleri, Siren Sürüsü | Muhafız Konseyi | 22.000 | 472.000 | 975.000 | 1.640.000 |
| 10 | Çukurun Dibi | P10 | 46 | 46–50 | Tüm düşmanlar | Titan'ın Yavrusu | 30.000 | 810.000 | 1.230.000 | 2.070.000 |
<!-- /URET:tablo:denizaltilar.kampanya.bolumler -->

- **Önerilen güç** bir denizaltının asker gücüdür (kahramanlar hariç): 1. aşamada bölümün ilk
  reaktöründeki dolu bir denizaltının %75'i, 9. aşamada bölümün son reaktöründeki dolu bir denizaltı
  (en üst iki kademe karışık). Arada geometrik artar. Boss iki denizaltı içindir. Yeni bölümün ilk
  aşaması bir önceki bölümün sonundan biraz kolaydır: bir nefes.
- Boss'ların ortak kuralı: tek gövde ve büyük bir HP havuzu (bozgun eşiği yoktur), her 3–4 turda bir
  özel hareket (yutma, sürü çağırma, kalkan). Ayrıntılar 07'deki düşman tablosundadır.
- **Demir Leviathan (3)**, **Siren Kraliçesi (5)**, **Kadim Muhafız (7)** ve **Titan'ın Yavrusu (10)**
  hikâyenin dört dönüm noktasıdır; diğer boss'lar bu dördünün habercisidir. Hadal Titanı'nın kendisi
  kampanyada değil, son oyundadır ([14](14_son_oyun.md)).

### 5.8 Madalyalar ve Madalya Vitrini

Her aşamada üç **Derinlik Madalyası** vardır; koşullar aşama türüne göre değişir:

<!-- URET:tablo:denizaltilar.kampanya.madalyalar -->
| Aşama türü | 1. madalya | 2. madalya | 3. madalya |
| --- | --- | --- | --- |
| Savaş | Kazan | Kayıp %20'nin altında | 6 turda bitir |
| Sonar | Kazan | Işık açmadan kazan | Kayıp %20'nin altında |
| Karşılaşma | Tamamla | Türü tara | Hiç rahatsız etmeden gözle |
| Enkaz | Kurtar | Mors ile kurtar ya da kurtarmayı süre dolmadan bitir | Kalıntı izini işaretle |
| Boss | Yen | Tek denizaltıyla yen | 10 turda yen |
<!-- /URET:tablo:denizaltilar.kampanya.madalyalar -->

<!-- URET:tablo:denizaltilar.kampanya.madalya_kaynaklari -->
| Kaynak | Miktar |
| --- | --- |
| Aşama madalyaları (ilk kez) | Aşama başına 3, toplam 300 |
| Bölüm tamamlama | Bölüm başına 10 |
| Haftalık Derin Tekrar (geçilmiş boss'ları yeniden yen) | Boss başına haftada 3 |
| Etkinlik görevleri (13) | Değişken |
<!-- /URET:tablo:denizaltilar.kampanya.madalya_kaynaklari -->

Madalyalar **Madalya Vitrini**'nde harcanır. Vitrin The Syndicate değildir: içinde para ile alınan hiçbir
şey yoktur ve madalya satılmaz.

<!-- URET:tablo:denizaltilar.kampanya.dukkan -->
| Ürün | Madalya | Sınır | Açılış |
| --- | ---: | --- | --- |
| Seçmeli nadir kahraman parçası ×5 | 20 | Haftada 4 | Bölüm 1 |
| Seçmeli destansı kahraman parçası ×5 | 60 | Haftada 2 | Bölüm 5 |
| Anı Kristali ×1 | 25 | Haftada 3 | Bölüm 3 |
| Teçhizat malzemesi kutusu (bölümün malzemesi) | 15 | Haftada 5 | Bölüm 2 |
| Eğitim hızlandırma 1 sa | 10 | Haftada 10 | Bölüm 1 |
| İyileştirme hızlandırma 1 sa | 8 | Haftada 10 | Bölüm 1 |
| Gövde kaplaması (bölüme özel, kozmetik) | 150 | Bir kez | Bölümü bitir |
| Madalya profili rozeti | 300 | Bir kez | Bölüm 10 |
<!-- /URET:tablo:denizaltilar.kampanya.dukkan -->

### 5.9 Ödüller

**Ödüllü dalış** (günde <!--v:veri.denizaltilar.kampanya.odullu_dalis-->3<!--/v-->): kazanılan her
ödüllü dalış Titanyum ve Biyokütle verir, Gelir Saati (GS) ile; 7. bölüm ve sonrasında Süperalaşım da
düşer. Günlük toplam, [15 · Ekonomi Akışı](15_ekonomi_akisi.md#34-sefer)'ndaki sefer bütçesidir
(reaktör seviyesine göre GS/gün); bu bölüm miktarı değil, kuralı tanımlar.

**Boss ödülleri** (bir kez, ilk zaferde): kahraman parçası, Hadalit (K-12 ölçeğinde) ve 7. bölümden
itibaren Süperalaşım. Bölüm sonunda ayrıca 10 madalya verilir.

<!-- URET:tablo:denizaltilar.kampanya.oduller -->
| Bölüm | Ad | Ödüllü dalışta SA | Boss Hadalit | Boss Süperalaşım | Boss kahraman parçası (seçmeli) |
| ---: | --- | --- | ---: | ---: | --- |
| 1 | Hadal Eşiği | — | 100 | — | Nadir ×10 |
| 2 | Dalan Levha | — | 100 | — | Nadir ×10 |
| 3 | Fay Basamakları | — | 150 | — | Nadir ×15 |
| 4 | Soluk Sürüler | — | 150 | — | Nadir ×15 |
| 5 | Balık Sınırı | — | 200 | — | Destansı ×10 |
| 6 | Sessiz Derinlik | — | 200 | — | Destansı ×10 |
| 7 | Huni Tabanı | Var | 250 | 60 | Destansı ×15 |
| 8 | Soğuk Sızıntılar | Var | 300 | 100 | Destansı ×15 |
| 9 | Dip Tortulu | Var | 350 | 150 | Efsanevi ×5 |
| 10 | Çukurun Dibi | Var | 500 | 250 | Efsanevi ×10 |
<!-- /URET:tablo:denizaltilar.kampanya.oduller -->

Boss ödüllerinin toplamı: <!--v:veri.denizaltilar.kampanya.boss_hd_toplam-->2.300<!--/v--> Hadalit ve
<!--v:veri.denizaltilar.kampanya.boss_sa_toplam-->560<!--/v--> Süperalaşım; yıla yayıldığında F2P'nin
günlük Hadalit ve Süperalaşım gelirinin küçük bir kesridir. Kampanya ödülleri tavana takılmaz, Kaynak
Kapsülü olarak çantaya düşer ([02](02_tempo_ve_denge.md#7-depolama-tavan-ve-yağma-koruması)).

---

## 6. Sis ve sonar dronları

Dünya haritası başlangıçta **sonar gölgesidir**; oyuncu üssünün çevresini ve ittifak bölgesini görür
([09](09_dunya_haritasi_ve_pvp.md#9-sis-keşif-noktaları-ve-tür-kataloğu)). Gölgeyi **Radar Merkezi**'nin
sonar dronları açar. Dron sayısı Radar seviye kilitlerinden gelir ([Radar Merkezi](binalar/radar_center.md));
menzil, tarama yarıçapı, bekleme ve ping bedeli Radar seviyesiyle büyür.

<!-- URET:tablo:denizaltilar.sonar_dronlari -->
| Radar Sv | Dron | Menzil (kare) | Tarama yarıçapı (kare) | Dron başına bekleme | Ping bedeli (Enerji) |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 10 | 2 | 30 dk | 50 |
| 10 | 1 | 18 | 3 | 27 dk | 500 |
| 15 | 2 | 22 | 3 | 25 dk 30 sn | 1.000 |
| 20 | 2 | 26 | 4 | 24 dk | 2.000 |
| 30 | 3 | 34 | 5 | 21 dk | 5.000 |
| 40 | 3 | 42 | 6 | 18 dk | 9.000 |
| 50 | 3 | 50 | 7 | 15 dk | 15.000 |
<!-- /URET:tablo:denizaltilar.sonar_dronlari -->

- **Ping:** dron menzil içindeki bir kareye gider ve yarıçap içini tarar. Arazi ve sabit buluntular
  kalıcı açılır; yürüyüşler ve canavarlar yalnızca ping'den sonraki
  <!--v:veri.denizaltilar.sis.canli_sure|sure-->10 dk<!--/v--> boyunca görünür.
- Ping Enerji harcar (tablodaki bedel); Enerji, referans üssün artı bütçesinden karşılanır
  ([02](02_tempo_ve_denge.md#8-enerji-dengesi)). Bir oyuncu üssünü keşfetmek de bir ping'dir
  ([07](07_birlikler_ve_savas.md#48-keşif-ve-erken-uyarı)).
- Dronlar savaşmaz ve düşürülmez; Sonar Enerjisi harcamaz (o, haritadaki PvE saldırılarınındır, K-22).

Ping'in ortaya çıkardığı **buluntular** oyuncu başına bir kez alınır; kuşaklara dağılımları 09'dadır:

<!-- URET:tablo:denizaltilar.buluntular -->
| Buluntu | Nerede | Nasıl alınır | Ödül |
| --- | --- | --- | --- |
| Batık enkaz | Her kuşak | Bir denizaltı gidip kurtarır (10–30 dk; Mors −%33) | Bir kerelik 0,3–1 GS Titanyum ve bir teçhizat malzemesi; hedefli dron kurtarmasının hedefi |
| Paslı Kriyo-Kapsül (I) | P1–P3 basamakları | Denizaltıyla getirilir, Kriyo Salonu'nda açılır | Sıradan/nadir kahraman parçası ×5–10 |
| Mühürlü Kriyo-Kapsül (II) | P4–P7 basamakları | Getirilir; açılması 8 saat | Nadir/destansı parça ×10–15, %10 şansla Anı Kristali |
| Kadim Kriyo-Kapsül (III) | P8–P10 basamakları | Getirilir; açılması 24 saat, ittifak yardımı alır | Destansı parça ×20, %5 şansla efsanevi parça ×5 |
| Tür yankısı | Türün bilinen derinlik aralığında | Ping alanına düşünce kendiliğinden kaydolur | Tür Kataloğu kaydı (Fenerbalığı ile iki kat puan) |
| Kalıntı izi | Derin Yamaç ve içerisi | İşaretlenir; hikâye ve sefer içeriği açar | Bkz. 14 · Son Oyun |
<!-- /URET:tablo:denizaltilar.buluntular -->

- **Terk edilmiş kriyo-kapsüller** eski konvoylardan kalmış, akıntıyla dağılmış kapsüllerdir; içlerinde
  donmuş hayatta kalanlar vardır. Kriyo-Kapsül çekilişinin (K-04) haritadaki karşılığıdır ama
  içerikleri sabittir, şans oranı taşımaz. Seviye basamağın basınç sınıfına bağlıdır.
- **Batık enkaz** bir denizaltının gidip kurtarmasını ister; Mors gövdesi süreyi kısaltır, sınıf açığı
  kuralı burada da geçerlidir. Dron Fabrikası'nın hedefli kurtarma görevleri (§7) bu enkazları hedef alabilir.
- **Tür yankısı** Tür Kataloğu'na kendiliğinden yazılır; harita türleri ve sunucuya özgü kayıtlar
  [09](09_dunya_haritasi_ve_pvp.md#91-tür-kataloğu)'dadır.

---

## 7. Dron kurtarma görevleri

Bugün Dron Fabrikası üç kurtarma görevi gönderir (catalog v8, `salvageMissions`). Ödül fabrika
seviyesi başına %10 artar; Hadalit şansı görev başlarken çekilen tohumla belirlenir (kodda var).

<!-- URET:tablo:denizaltilar.kurtarma_v8 -->
| Görev | Kimlik | Süre | Ödül (Sv 1) | Hadalit |
| --- | --- | ---: | --- | --- |
| Hızlı | quick | 15 dk | 1.500 Titanyum | — |
| Standart | standard | 1 sa | 5.000 Titanyum + 400 Biyokütle | %25 şansla 2–4 |
| Derin | deep | 4 sa | 15.000 Titanyum + 1.500 Biyokütle | %60 şansla 4–8 |
<!-- /URET:tablo:denizaltilar.kurtarma_v8 -->

**Evrim.** Hedef tasarımda üç değişiklik olur:

1. **Ödül reaktörle ölçeklenir.** v8'de ödül fabrika seviyesiyle doğrusal büyür ve geç oyunda önemsizleşir;
   hedefte ödül **Gelir Saati** (GS) ile yazılır: 1 GS, o reaktör seviyesindeki F2P saatlik Titanyum
   gelirine eşittir ([15](15_ekonomi_akisi.md#21-gelir-saati-gs)). Hadalit K-12 ile ×10 ölçeklenir.
2. **İki uzun görev eklenir:** gece boyu süren **Gece Kurtarması** (Reaktör 15) ve geç oyunda
   Süperalaşım şansı taşıyan **Çukur Kurtarması** (Reaktör 30). Uzun görev saat başına daha az, ama
   oyuncu uyurken kazandırır.
3. **Hedefli kurtarma:** sis sistemi gelince Derin ve daha uzun görevler sonarla bulunmuş bir batık
   enkaza yönlendirilebilir; Titanyum ödülü aynı kalır, o enkazın teçhizat malzemesi garanti düşer ve
   enkaz kurtarılmış sayılır. Böylece Dron Fabrikası ile Radar Merkezi birbirini besler.

<!-- URET:tablo:denizaltilar.kurtarma_hedef -->
| Görev | Süre | Açılış | Titanyum (GS) | R10 Ti | R20 Ti | R30 Ti | R40 Ti | R50 Ti | Hadalit (×10) |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Hızlı | 15 dk | Dron Fabrikası 1 | 0,1 | 900 | 3.800 | 9.000 | 17.000 | 30.000 | — |
| Standart | 1 sa | Dron Fabrikası 1 | 0,3 | 2.700 | 11.400 | 27.000 | 51.000 | 90.000 | %25 şansla 20–40 |
| Derin | 4 sa | Dron Fabrikası 1 | 0,8 | 7.200 | 30.400 | 72.000 | 136.000 | 240.000 | %60 şansla 40–80 |
| Gece Kurtarması | 8 sa | Reaktör 15 | 1,4 | — | 53.200 | 126.000 | 238.000 | 420.000 | %70 şansla 60–120 |
| Çukur Kurtarması | 12 sa | Reaktör 30 | 2 | — | — | 180.000 | 340.000 | 600.000 | %80 şansla 80–160; %20 şansla 10–30 SA |
<!-- /URET:tablo:denizaltilar.kurtarma_hedef -->

Tipik bir F2P günü (iki Derin + bir Gece Kurtarması) 3 GS, yani günlük Titanyum gelirinin sekizde
birini kurtarmadan alır; kalan pay toplama, sefer ve görevlerindir. F2P'nin reaktöre göre günlük görev
düzeni ve Hadalit sınırı [15](15_ekonomi_akisi.md#33-kurtarma-görevleri)'tedir.

---

## 8. Derin denizaltılardan mühendislik dersleri

Hadal'ın denizaltı kuralları gerçek derin dalış araçlarının mühendisliğinden doğar. Araçların ayrıntılı
kaydı ve kaynakçası [18 · Bilim Temeli](18_bilim_temeli.md)'dedir.

<!-- URET:tablo:denizaltilar.dersler -->
| Mühendislik dersi | Derin denizde | Örnek araç (18) | Hadal'daki karşılığı |
| --- | --- | --- | --- |
| Basınç küresi | Tam okyanus derinliğine inen insanlı araçlarda mürettebat kalın cidarlı bir titanyum kürede oturur; küre, basıncı her yönden eşit taşıyan en verimli biçimdir. | DSV Limiting Factor (Triton 36000/2), Shinkai 6500 | Basınç sınıfı İskele'nin Titanyumla güçlenen küresidir; Basınç Küresi modülü açıkta gövde kaybını azaltır. |
| Sintaktik köpük | Reçineye gömülmüş içi boş cam mikroküreler, ezilmeden yüzdürme sağlayan hafif bir malzeme oluşturur; derin araçların yüzdürme bloklarının çoğu budur. | Deepsea Challenger, Alvin | Teçhizat malzemesi ve Yüzdürme Bloğu modülü: hız ve dalış verimi. |
| Güvenlik payı | Araçlar çalışma derinliklerinin üstünde bir güvenlik payıyla test edilir; pay, gövdenin dayanabileceği sınırla çalıştırıldığı derinlik arasındaki farktır. | DSV Limiting Factor (Triton 36000/2) | Bir sınıf açık hasarla tolere edilir; iki sınıf açık kesin yasaktır. |
| Malzeme yorulması | Her dalış gövdeyi sıkıştırıp bırakır; tekrar eden yükler zamanla iz bırakır, bu yüzden gövdeler düzenli muayene edilir. | Alvin | Gövde bütünlüğü kalıcı değildir; açıkta geçen her dakika iz bırakır ve onarım ister. |
| Tek parçanın bedeli | İnsansız hibrit araç Nereus, ağır köpük yerine yüzlerce içi boş seramik küreyle yüzüyordu; Kermadec Çukuru'ndaki kaybının büyük olasılıkla bu kürelerin çökmesinden kaynaklandığı düşünülüyor. Hafiflik kazancı ile güvenlik payı arasında bir denge vardır. | Nereus | Yüzdürme Bloğu 5. seviyede bölmelere ayrılır; gövde %0'a inince araç parçalanmaz, balast bırakıp yükselir. |
| Balast bırakarak yükselme | Derin dalış araçları inişte taşıdıkları ağırlıkları bırakarak yüzeye döner; bu, güç kesilse bile çalışan pasif bir güvenlik yoludur. | Trieste | Acil tahliye: gövde bütünlüğü %0 olunca denizaltı seferi bırakır ve üsse döner; askerler sağ salim iner. |
| Ses, ışıktan uzağa gider | Işık suda birkaç on metrede söner; derin denizde konum, haberleşme ve haritalama sesle (sonar, akustik modem) yapılır. | — | Sonar aşamaları ve sis: haritayı ışık değil sonar dronları açar. |
<!-- /URET:tablo:denizaltilar.dersler -->

Gerçek kazalar ve can kayıpları oyun içeriği yapılmaz (K-26). Nereus yalnızca insansız bir aracın
kaybından çıkarılan bir mühendislik dersi olarak, saygıyla anılır:
<!--v:veri.bilim.araclar.nereus.ders-->Hafiflik ve maliyet kazancı ile güvenlik payı arasındaki denge: tek bir parçanın çökmesi tüm aracı kaybettirebilir.<!--/v-->

---

## 9. İlerleme

| Aşama | Reaktör | F2P | Ne değişir |
|---|---:|---:|---|
| İniş, ilk denizaltı, P1 | 1–4 | ilk dakikalar | Bölüm 0 · İniş; İskele ve 1. hangar |
| İlk askerler, 1. bölüm | 5 | <!--v:hesap.sim.f2p.seviye.5|gun-->4 dk 48 sn<!--/v--> | Kampanya başlar |
| 2. hangar | 10 | <!--v:hesap.sim.f2p.seviye.10|gun-->6 sa 51 dk<!--/v--> | Toplama ve sefer aynı anda |
| 2. sonar dronu, Gece Kurtarması | 15 | <!--v:hesap.sim.f2p.seviye.15|gun-->1,4. gün<!--/v--> | Sis daha hızlı açılır |
| 3. hangar | 18 | <!--v:hesap.sim.f2p.seviye.18|gun-->2,8. gün<!--/v--> | Ralli + toplama + sefer |
| P5, Balık Sınırı | 21 | <!--v:hesap.sim.f2p.seviye.21|gun-->6,1. gün<!--/v--> | Kampanyanın orta noktası |
| 4. hangar | 26 | <!--v:hesap.sim.f2p.seviye.26|gun-->15,7. gün<!--/v--> | Bütün ordu sahada |
| 3. sonar dronu, Çukur Kurtarması | 30 | <!--v:hesap.sim.f2p.seviye.30|gun-->28,6. gün<!--/v--> | Süperalaşım dönemi |
| P8 | 36 | <!--v:hesap.sim.f2p.seviye.36|gun-->79,2. gün<!--/v--> | Soğuk Sızıntılar |
| P10, Çukurun Dibi | 46 | <!--v:hesap.sim.f2p.seviye.46|gun-->255,8. gün<!--/v--> | Son bölüm, Çekirdek'in kapısı |

---

## 10. F2P ve ödeyen oyuncu

| Konu | Ücretsiz oyuncu | Ödeyen oyuncu | İlke |
|---|---|---|---|
| Hangar ve sınıf | Reaktörle açılır | Aynı; daha erken | Sınıf ve hangar satılmaz |
| Kampanya | Bütün bölümler, bütün madalyalar | Aynı | Madalya satın alınmaz, Vitrin parasızdır |
| Dalış maliyeti | Oksijen | Oksijen | Kaynak paketleri dalışı hızlandırabilir ama atlatamaz |
| Onarım | Ücretsiz, 5 saatte tam | + Titanyumla anında | Para zamanı alır |
| Rol değişimi | 24 saatte bir | Aynı | Esneklik herkes için eşit |
| Kurtarma görevleri | Hepsi | Hepsi | VIP yalnızca süre kısaltır ([11](11_vip.md)) |

---

## 11. Arayüz notları (Quiet Luxury)

- **Hangar ekranı:** dört yuva yan yana; her denizaltının siluet çizimi rolüne göre (ince mürekkepbalığı,
  tıknaz mors, keskin köpekbalığı, ışıklı fenerbalığı). Altında sınıf rozeti (P1–P10), gövde bütünlüğü
  ince bir çubuk, kaptan ve iki subay portresi.
- **Basınç:** sınıf rozeti bir derinlik ölçeği gibi çizilir; oyuncunun sınıfı ve bir sonraki eşik aynı
  çizgide. Açık olan bölgeye rota çizilince çizgi sakin bir kehribar tona döner; iki sınıf açıkta çizgi
  kesik ve gri. Kırmızı alarm yok.
- **Derinlik Haritası:** dikey bir kesit; oyuncu aşağı kaydırdıkça ışık azalır, renkler soğur, müzik
  seyrekleşir. Her bölüm başlığında derinlik ve bar, küçük ve kesin. Balık Sınırı'nın altında sonar
  yankıları küçülür.
- **Sonar aşaması:** ekran koyu; yalnızca halka halka yayılan ping ve yankılar. Işık düğmesi her zaman
  görünür ama sessizdir.
- **Karşılaşma:** canlı tam ekran, sakin; altında üç düğme (Tara / Gözle / Kaçın). Saldırı düğmesi
  hiçbir zaman yoktur. Tür adı ve bilimsel adı küçük bir kartta.
- **Madalya Vitrini:** koyu kadife zemin, az ürün, net sınırlar; geri sayım yok.
- **Kurtarma:** dron fabrikası panelinde görev kartları; ödül tahmini "~" ile, Hadalit şansı açıkça yüzde.

---

## 12. Uç durumlar

| Durum | Kural |
|---|---|
| Seferdeyken İskele sınıf eşiğini geçer | Denizaltı dönünce yükselir; yoldaki açığı değişmez |
| Seferde gövde %0 | Acil tahliye: sefer iptal, yük korunur, askerler sağ, denizaltı onarıma |
| Açık bölgede savaş | Askerler −%15 DEF; gövde aşınmaya devam eder |
| İki sınıf açık bölgeye takviye isteği | Reddedilir; rota çizilemez |
| Rol değişimi kapasiteyi düşürür | Fazla askerler Askeri Kubbe'ye iner; ordu kapasitesi değişmez (seferdeki asker zaten sayılır) |
| Kaptan koltuğu boş denizaltı | Sefere çıkabilir, kahraman katkısı ve öfke yok |
| Kampanya dalışı sırasında üsse saldırı | Dalış anlık çözüldüğü için çakışma yok; dalan denizaltının askerleri garnizonda sayılır |
| Oksijen dalış maliyetine yetmez | Dalış başlamaz; eksik miktar gösterilir |
| Karşılaşmada Ark Askeri | Canlı ürker, "rahatsız etmeden" madalyası kaybedilir; canlı hasar almaz |
| Boss dalışında ikinci denizaltı seferde | Boss tek denizaltıyla denenebilir |
| Buluntu başka oyuncu tarafından alınır | Buluntular oyuncu başınadır; herkes kendi enkazını kurtarır |
| Kurtarma görevi çevrimdışı biter | Sunucu saatinde biter, ödül tavana takılmaz (Kapsül) |

---

## 13. Bugün kodda

| Öğe | Durum | Not |
|---|---|---|
| Dron Fabrikası kurtarma görevleri | ✅ | `salvageMissions` (v8): 15 dk / 1 sa / 4 sa; seviye başına +%10; tohumlu Hadalit |
| Denizaltı İskelesi | 🟡 | Bina var; basınç sınıfı ve onarım işlevi yok |
| Radar Merkezi | 🟡 | Bina var; dron, sis ve uyarı işlevi yok |
| Denizaltı Hangarı, denizaltılar, roller | ⬜ | Yok |
| Basınç sınıfları ve açık | ⬜ | Unity'de `PressureManager` (Assets/_Hadal/Scripts/Managers) eski 1.000–11.000 m katmanlarıyla yerel bir taslaktır; ağa bağlı değildir |
| Derinlik Haritası | ⬜ | Unity'de `ExpeditionManager` yerel bir zamanlayıcı taslağıdır (kahraman sayısı + süre + ödül); sunucuda karşılığı yok |
| Sis ve sonar dronları | ⬜ | Haritada `viewRadius` 14 kare (catalog `world`); sis yok |

---

## 14. Uygulama notları

- **Paylaşılan kurallar** (`shared/HADAL.Shared/Simulation`): `SubmarineRules.cs` (sınıf = 1 + ⌊(Sv−1)/5⌋,
  açık, gövde aşınması, onarım, rol değişimi, seyir süresi ve O₂), `ExpeditionRules.cs` (Derinlik Haritası
  aşamaları, madalya koşulları, önerilen güç), `SonarRules.cs` (ping menzili ve yarıçapı, sis durumu).
  Savaş için 07'deki `CombatRules.cs` kullanılır.
- **Katalog** (v9 ve sonrası): `submarines` (roller, sınıf tablosu, onarım, O₂), `expeditionCampaign`
  (bölümler, aşamalar, düşman dizilimleri, ödüller, Vitrin), `sonar` (dron tablosu, buluntu türleri);
  `salvageMissions` genişler: ödül GS ile (`rewardGs`), `unlockReactor`, Hadalit ×10, `targetWreck`.
  Değerler `veri/denizaltilar.json`'dan üretilir.
- **Sunucu** (`server/Hadal.Game`): `PlayerSession.Submarines.cs` (rol, onarım, mürettebat komutları:
  `AssignCrew`, `SetHullRole`, `RepairHull`), `PlayerSession.Expedition.cs` (`DiveStage`, anlık çözüm,
  rapor), yürüyüşler `World` altında bir `MarchService` ile (seyir süresi, saatlik O₂ düşümü, tahliye).
  Sis durumu oyuncu başına bit alanı olarak (`hadal.players.extras`) saklanır.
- **Tohumlar:** kampanyada rastgelelik yoktur; buluntu yerleşimi sunucu tohumuyla oyuncu başına
  belirlenir (kurtarma görevlerindeki tohum yaklaşımı gibi).
- **Unity:** `ExpeditionManager` ve `PressureManager` taslakları kaldırılır ya da sunucu durumunu
  gösteren görünümlere dönüştürülür; `PressureManager`'daki 1.000–7.000 m katmanları yalnızca hikâye
  girişindeki inişte kullanılır (K-03).
- **Doğrulama:** `bilim.json`'daki tür derinlikleri ile kampanya bölüm bantlarının örtüşmesi üretici
  araçta denetlenmeli (bu bölümün verisi yazılırken denetlendi).

---

## 15. Esin kaynakları

- **4X strateji türü:** yürüyüş kuyruğu olarak takımlar, tek oyunculu kampanya bölümleri ve yıldızlı /
  madalyalı aşamalar, madalya mağazası, gerçek zamanlı yürüyüş ve hız kuralı (en yavaş birim).
- **Hayatta kalma temalı 4X:** keşif kulesi ve görevleri, sisli dünya haritası, harita buluntuları.
- **Derin deniz araştırması:** basınç küreleri, sintaktik köpük, balast bırakma, sonar ve akustik
  haberleşme, insanlı ve insansız tam derinlik araçları, yemli kamera gözlemleri ([18](18_bilim_temeli.md)).

İsimler, metinler ve sayılar özgündür.

---

## 16. Açık sorular

1. **Bölüm ↔ kuşak.** 09'da kuşak basamakları P1–P10'a bağlandı; kampanya bölümleri aynı bantları
   kullanır. Bölüm adları kuşak adlarıyla çakışmasın diye 4. bölüm "Soluk Sürüler" oldu.
2. **Soğuk Sızıntılar (8. bölüm):** bilim.json'daki soğuk sızıntı kaydı genel ve sığ örneklidir. Hadal
   çukurlarda derin sızıntı topluluklarını bildiren yakın tarihli bir kaynak 18'e eklenirse bölüm ona
   bağlanmalı; eklenmezse bölüm coğrafi bir adla ("Kemer Havzası" gibi) değiştirilebilir.
3. **Boss ödülleri 15'in bütçesinde değil:** toplam <!--v:veri.denizaltilar.kampanya.boss_hd_toplam-->2.300<!--/v-->
   Hadalit ve <!--v:veri.denizaltilar.kampanya.boss_sa_toplam-->560<!--/v--> Süperalaşım (tek seferlik).
   15'in Hadalit tablosundaki "Kronik ve Tür Kataloğu başarımları" satırına ya da ayrı bir satıra eklenmeli.
4. **Denizaltı modülleri** teçhizat malzemelerini (kitin, bazalt camı, masif sülfür, sintaktik köpük)
   kullanır; seviye bedelleri 17 yazarıyla eşlenmeli.
5. **Madalya Vitrini ve kriyo-kapsül içerikleri** (seçmeli parça havuzu, haftalık sınırlar) Kriyo-Kapsül
   ekonomisini ezmemeli; 05 ve 12 yazarlarının onayı gerekli.
6. **Sonar dronu menzili ve ping bedeli** 09'daki harita ölçeğine ve kuşak genişliklerine göre yeniden
   ayarlanabilir.
7. **Önerilen güç** kahramanları dışarıda bırakır; 05'teki kahraman gücü eklenince tablo "toplam güç"
   olarak yeniden hesaplanmalı.
8. **İniş (Bölüm 0)** ilk oturumun parçasıdır; 15'teki hikâye bölümleri ve 16'daki Kronik açılışıyla
   sıralaması koordine edilmeli.
