# 06 · Araştırma

> **Özet.** Araştırma, Hadal'ın bir yıl boyunca hiç durmayan ikinci saatidir. İki laboratuvar vardır:
> **Araştırma Merkezi** üssün ekonomisini (Gelişim, Ekonomi, Hayatta Kalma, Derinlik), **Bilim
> Kubbesi** savaşı ve ileri bilgiyi (Birlik, Kahraman, iki Taktik dalı, Filo, Konvoy, Derinlik Ligi,
> Leviathan Avı, Kadim Teknoloji) araştırır. Merkezde **İkinci Tezgâh** ile iki tezgâh, kubbede bir
> tezgâh çalışır: aynı anda üç araştırma. Ağaç <!--v:hesap.sayilar.arastirma_dali-->13<!--/v--> dal,
> <!--v:hesap.sayilar.teknoloji-->154<!--/v--> teknoloji ve <!--v:hesap.sayilar.arastirma_seviyesi-->1.138<!--/v-->
> seviyedir. Ücretsiz oyuncu ilk yılın sonunda toplam araştırma süresinin
> <!--v:veri.arastirma.butce.toplam.f2p_yuzde|yuzde-->%68,3<!--/v--> kadarını bitirir: Gelişim ve Ekonomi
> tamamdır, Birlik <!--v:veri.arastirma.butce.troops.f2p_yuzde|yuzde-->%68,9<!--/v-->, Taktikler yarı yolda,
> Kadim Teknoloji başlangıçtadır. Dalların çoğu derin deniz bilimine dayanır: karanlık oksijen,
> sintaktik köpük, kemosentez, metan hidrat, titanyum basınç küreleri.

Durum: 🟡. Bugün kodda Araştırma Merkezi, iki ağaç (Gelişim, Ekonomi), 11 düğüm ve İkinci Tezgâh
çalışıyor. Diğer 11 dal, Bilim Kubbesi tezgâhı, reaktör eğrisine bağlı maliyet ve süre, bölen hız
formülü (K-13) tasarımdır.

Bağlı bölümler: [00 · Kanon](00_kanon.md) · [02 · Tempo ve Denge](02_tempo_ve_denge.md) ·
[03 · Binalar](03_binalar.md) · [05 · Kahramanlar](05_kahramanlar.md) ·
[07 · Birlikler ve Savaş](07_birlikler_ve_savas.md) ·
[08 · Denizaltılar, Basınç, Seferler](08_denizaltilar_basinc_seferler.md) ·
[09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md) · [10 · İttifak](10_ittifak.md) ·
[11 · VIP](11_vip.md) · [13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md) ·
[14 · Son Oyun](14_son_oyun.md) · [18 · Bilim Temeli](18_bilim_temeli.md).
Veri: [`veri/arastirma.json`](veri/arastirma.json). Dal sayfaları (her teknolojinin seviye seviye
maliyeti, süresi, etkisi ve gücü): [`arastirma/`](arastirma/).

---

## 1. Amaç

1. **Tezgâh hiç boş kalmaz.** İlk saatte araştırmalar dakikalar, son seviyeler günler sürer. Her
   reaktör seviyesi Araştırma Merkezi'ni yükseltir, o da yeni seviyeleri açar; ücretsiz oyuncu bir yıl
   boyunca her girişinde başlatacak bir araştırma bulur.
2. **Önce üs, sonra savaş, en son kadim bilgi.** Gelişim ve Ekonomi ilk yıl içinde biter; savaş
   dalları ilk yılın sonunda yarı ile üçte iki arasındadır; Kadim Teknoloji ikinci yılın dalıdır.
3. **Seçim zorunludur.** Bir yılda her şey bitmez. Esnek İkinci Tezgâh'ın ekonomiye mi savaşa mı
   çalışacağı oyuncunun kararıdır; ittifaktaki rolü (toplayıcı, ralli lideri, savunmacı) bunu belirler.
4. **Bilim, isimden fazlası.** Teknolojiler derin denizin yöntem ve keşiflerine bağlanır; her
   bağlantının kaynakçası `veri/bilim.json`'dadır (§5.14).
5. **Sessiz lüks.** Para araştırmayı hızlandırır ama sırayı değiştirmez: Lab kapısı, kubbe seviyesi ve
   önkoşullar herkes için aynıdır. Hadalit ile teknoloji seviyesi satın alınmaz; The Syndicate yalnızca
   hızlandırma ve kaynak satar ([12 · The Syndicate](12_syndicate.md)).

---

## 2. İki laboratuvar, üç tezgâh

<!-- URET:tablo:arastirma.kuyruklar -->
| Tezgâh | Laboratuvar | Açılış | Araştırdığı dallar |
| --- | --- | --- | --- |
| Merkez tezgâhı | Araştırma Merkezi | Araştırma Merkezi 1 (Reaktör 3) | Gelişim, Ekonomi, Hayatta Kalma, Derinlik |
| İkinci Tezgâh (esnek) | Araştırma Merkezi | İkinci Tezgâh araştırması (Lab 8) | Esnek: 13 dalın hepsi; kubbe dallarında Bilim Kubbesi kuralları geçerli |
| Kubbe tezgâhı | Bilim Kubbesi | Bilim Kubbesi 1 (Reaktör 12) | Birlik, Kahraman, Saldırı ve Savunma Taktikleri, Filo, Konvoy, Derinlik Ligi, Leviathan Avı, Kadim Teknoloji |
<!-- /URET:tablo:arastirma.kuyruklar -->

- **Araştırma Merkezi** (Lab 1) Reaktör 3'te kurulur ve reaktör önkoşul zincirindedir
  ([03](03_binalar.md)). Seviyesi **Lab seviyesidir**: bütün dallarda, kubbe dalları dahil, hangi
  teknoloji seviyesinin açılacağını o belirler.
- **İkinci Tezgâh** Lab 8'de araştırılır (kodda var). Hedef tasarımda **esnektir**: Araştırma
  Merkezi'nin dallarına da Bilim Kubbesi'nin dallarına da çalışabilir; kubbe dallarında kubbe kuralları
  geçerlidir.
- **Bilim Kubbesi** (Lab 2) Reaktör 12'de kurulur, Araştırma Merkezi 12 ister ve yalnızca kubbe
  dallarını araştırır. Beş seviyesi dalları açar ve kubbe dallarında kullanılabilecek en yüksek Lab
  seviyesini sınırlar:

<!-- URET:tablo:arastirma.kubbe_kapilari -->
| Bilim Kubbesi Sv | Reaktör | Kubbe dallarında en yüksek Lab | Açılan |
| ---: | ---: | ---: | --- |
| 1 | 12 | 19 | Birlik, Kahraman |
| 2 | 20 | 29 | Saldırı Taktikleri, Savunma Taktikleri, Filo, Konvoy, Derinlik Ligi; T5–T6 |
| 3 | 30 | 39 | Leviathan Avı, Kadim Teknoloji (Lab 35+); T7–T8 |
| 4 | 40 | 44 | Lab 40–44 seviyeleri; T9 |
| 5 | 45 | 50 | Lab 45–50 seviyeleri; T10 |
<!-- /URET:tablo:arastirma.kubbe_kapilari -->

Kurallar:

- Bir teknoloji aynı anda tek bir tezgâhta araştırılır; sonraki seviyesi öncekinin bitmesini bekler.
- Araştırma sunucu saatiyle çevrimdışıyken de biter. Son 5 dakikası ücretsizdir; Mühendislik Bölmesi bu
  süreyi uzatır ([02 · Tempo ve Denge](02_tempo_ve_denge.md)). İttifak yardımı araştırmaya da çalışır
  ([10 · İttifak](10_ittifak.md)).
- Bir sonraki seviyeyi sıraya koymak İzin 9 ayrıcalığıdır ([11 · VIP](11_vip.md)); sıradaki araştırma
  kaynağını başladığı anda öder.
- Laboratuvar yükseltilirken araştırma durmaz.
- Araştırma ekranı üç tezgâh kartını üstte gösterir; altında dal ağacı açılır. Kilitli seviye neyi
  beklediğini söyler: "Araştırma Merkezi 32", "Bilim Kubbesi 4" ya da "Gerekli: Kademe VIII Doktrini".

---

## 3. Lab kapısı, maliyet ve süre

Her teknolojinin her seviyesi bir Lab seviyesine bağlıdır. Maliyet ve süre, reaktörün kendi
eğrilerinden okunur ([02 · Tempo ve Denge](02_tempo_ve_denge.md)); böylece araştırma, üssün geri
kalanıyla aynı tempoda büyür.

```
gereken Lab (r) = min_lab + adim × (ℓ − 1)                     (en çok 50)
maliyet         = olcek × bedel_olcek × reaktor_bedel(r)
ham süre        = 0,35 × sure_olcek × reaktor_sure(r)
güç             = 0,4 × güç(r)
```

- Varsayılan maliyet satırları Titanyum (olcek 0,25) ve Lab 2'den itibaren Biyokütle'dir (0,09).
- **Süperalaşım** isteyen üç yer vardır: Kadim Teknoloji'nin bütün seviyeleri (Lab 35'ten), Kademe IX
  ve X Doktrini (Lab 40 ve 45) ve Süperalaşım Takviye Halkaları (Lab 41).
- Her dalın bir **süre ölçeği** (S) vardır; teknolojinin `sure_olcek` değeri S ile teknolojinin kendi
  ağırlığının çarpımıdır (kademe doktrinleri 3 kat ağırdır). Maliyet ölçeği sürenin %80'idir: geç
  oyunda araştırma kaynakla değil zamanla sınırlanır (§7.2).

<!-- URET:tablo:arastirma.dallar -->
| Dal | Laboratuvar | Açılış | Süre ölçeği (S) | F2P hedefi (365. gün) |
| --- | --- | --- | ---: | ---: |
| Gelişim | Araştırma Merkezi | Araştırma Merkezi 1 (Reaktör 3) | 1 | %100 |
| Ekonomi | Araştırma Merkezi | Araştırma Merkezi 1 (Reaktör 3) | 0,95 | %100 |
| Hayatta Kalma | Araştırma Merkezi | Araştırma Merkezi 4 | 1,2 | %90 |
| Derinlik | Araştırma Merkezi | Araştırma Merkezi 7 | 1,1 | %80 |
| Birlik | Bilim Kubbesi | Bilim Kubbesi 1 (Reaktör 12) | 0,7 | %70 |
| Kahraman | Bilim Kubbesi | Bilim Kubbesi 1 (Reaktör 12) | 0,5 | %70 |
| Saldırı Taktikleri | Bilim Kubbesi | Bilim Kubbesi 2 (Reaktör 20) | 0,45 | %50 |
| Savunma Taktikleri | Bilim Kubbesi | Bilim Kubbesi 2 (Reaktör 20) | 0,45 | %50 |
| Filo | Bilim Kubbesi | Bilim Kubbesi 2 (Reaktör 20) | 0,4 | %55 |
| Konvoy | Bilim Kubbesi | Bilim Kubbesi 2 (Reaktör 20) | 0,35 | %70 |
| Derinlik Ligi | Bilim Kubbesi | Bilim Kubbesi 2 (Reaktör 20) | 0,35 | %75 |
| Leviathan Avı | Bilim Kubbesi | Bilim Kubbesi 3 (Reaktör 30) | 0,4 | %40 |
| Kadim Teknoloji | Bilim Kubbesi | Bilim Kubbesi 3 ve Araştırma Merkezi 35; Süperalaşım ister | 0,55 | %20 |
<!-- /URET:tablo:arastirma.dallar -->

Araştırma Merkezi'nin dalları kubbeninkilerden daha ağırdır: merkezde ayrılmış bir tezgâh ve esnek
tezgâhın büyük kısmı çalışır, ekonomi dalları da oyunun ilk yılında bitmek üzere tasarlanmıştır.
Kubbe dalları daha çok ve daha dar teknolojiye bölünür.

Örnek seviyeler:

| Teknoloji | Sv | Lab | Titanyum | Biyokütle | Süperalaşım | Ham süre |
|---|---:|---:|---:|---:|---:|---:|
| Araştırma Yöntemleri | 10 | <!--v:hesap.arastirma.research_methods.10.lab-->19<!--/v--> | <!--v:hesap.arastirma.research_methods.10.bedel.Titanium-->19.400<!--/v--> | <!--v:hesap.arastirma.research_methods.10.bedel.Biomass-->6.980<!--/v--> | — | <!--v:hesap.arastirma.research_methods.10.sure|sure-->3 sa 50 dk<!--/v--> |
| Kademe VII Doktrini | 1 | <!--v:hesap.arastirma.tier_7_doctrine.1.lab-->30<!--/v--> | <!--v:hesap.arastirma.tier_7_doctrine.1.bedel.Titanium-->462.000<!--/v--> | <!--v:hesap.arastirma.tier_7_doctrine.1.bedel.Biomass-->166.000<!--/v--> | — | <!--v:hesap.arastirma.tier_7_doctrine.1.sure|sure-->3 g 7 sa 30 dk<!--/v--> |
| Karanlık Oksijen | 10 | <!--v:hesap.arastirma.dark_oxygen.10.lab-->48<!--/v--> | <!--v:hesap.arastirma.dark_oxygen.10.bedel.Titanium-->2.880.000<!--/v--> | <!--v:hesap.arastirma.dark_oxygen.10.bedel.Biomass-->1.040.000<!--/v--> | — | <!--v:hesap.arastirma.dark_oxygen.10.sure|sure-->11 g 21 sa<!--/v--> |
| Kademe X Doktrini | 1 | <!--v:hesap.arastirma.tier_10_doctrine.1.lab-->45<!--/v--> | <!--v:hesap.arastirma.tier_10_doctrine.1.bedel.Titanium-->4.200.000<!--/v--> | <!--v:hesap.arastirma.tier_10_doctrine.1.bedel.Biomass-->1.510.000<!--/v--> | <!--v:hesap.arastirma.tier_10_doctrine.1.bedel.Alloy-->300<!--/v--> | <!--v:hesap.arastirma.tier_10_doctrine.1.sure|sure-->20 g 14 sa<!--/v--> |
| Titan Etüdü | 5 | <!--v:hesap.arastirma.titan_studies.5.lab-->48<!--/v--> | <!--v:hesap.arastirma.titan_studies.5.bedel.Titanium-->1.670.000<!--/v--> | <!--v:hesap.arastirma.titan_studies.5.bedel.Biomass-->600.000<!--/v--> | <!--v:hesap.arastirma.titan_studies.5.bedel.Alloy-->370<!--/v--> | <!--v:hesap.arastirma.titan_studies.5.sure|sure-->6 g 21 sa<!--/v--> |

Süreler hamdır; hız, yardım ve ücretsiz bitirme uygulanmadan önceki değerlerdir. Üretici her seviyenin
maliyetini, o seviyenin istediği Lab'daki cüzdan tavanıyla karşılaştırır: hiçbir seviye tavanı aşmaz.

---

## 4. Etkilerin toplanması

Araştırma etkileri kahraman mürettebat becerileri ([05](05_kahramanlar.md)), Derinlik İzni
([11](11_vip.md)), ittifak teknolojisi ([10](10_ittifak.md)), köken pasifleri
([16](16_kronik_kokenler_profil.md)), teçhizat ([17](17_techizat.md)) ve Egemen fermanlarıyla
([09](09_dunya_haritasi_ve_pvp.md)) aynı türdendir ve şu dört kurala göre birleşir:

| Etki türü | Örnek | Kural |
|---|---|---|
| Hız | İnşa, araştırma, eğitim, iyileştirme, yürüyüş, toplama hızı | Yüzdeler toplanır, bölen olarak uygulanır (K-13): süre = ham / (1 + Σhız% / 100) |
| Çarpan | Üretim, ATK, DEF, HP, kapasite, yük, ödül | Yüzdeler toplanır: değer = taban × (1 + Σ% / 100) |
| Azaltma | Tüketim, maliyet, hasar, ceza, yakalanma olasılığı | Yüzdeler toplanır, bölen olarak uygulanır: değer = taban / (1 + Σazaltma% / 100); hiçbir zaman sıfıra inmez |
| Sayı | İnşaatçı, tezgâh, bina sınırı, basınç sınıfı, Subay payı, kaçakçı rafı | Doğrudan eklenir |

Tablolarda azaltmalar eksi işaretiyle yazılır: "Oksijen tüketimi −%30", tüketimin 1,30'a bölündüğü
anlamına gelir. Böylece dört kaynak üst üste bindiğinde de değer sıfırın altına düşmez.

**Örnek: geç oyunda ücretsiz oyuncunun araştırma hızı.** Araştırma Yöntemleri
<!--v:hesap.arastirma.research_methods.10.etki|yuzde-->%50<!--/v-->, Yağ Dengeli Elektronik
<!--v:hesap.arastirma.oil_compensated_electronics.10.etki|yuzde-->%10<!--/v-->, İzin 7
<!--v:veri.vip.ekonomi.7.arastirma|yuzde-->%7<!--/v-->, ittifakın Ortak Laboratuvar teknolojisi (10
seviyede %5) ve mürettebattaki bir araştırma kahramanı (Dr. Orin Hale, orta seviyede yaklaşık %15)
toplanır: yaklaşık %87. Ham 10 günlük bir seviye 10 / 1,87 ≈ 5,3 günde biter. Bir Bilgelik Fermanı
günü (+%5) bunu birkaç saat daha kısaltır.

**İnşa hızı.** İnşaat Ustalığı <!--v:hesap.arastirma.construction_mastery.10.etki|yuzde-->%30<!--/v-->
ve Modüler Montaj <!--v:hesap.arastirma.modular_assembly.10.etki|yuzde-->%10<!--/v--> verir. Tempo
simülasyonu ücretsiz oyuncunun bütün kaynaklardan gelen inşa hızını %50'de keser; bu bölümün
değerleri o varsayımın içinde kalır.

---

## 5. Dallar

<!-- URET:arastirma-ozet -->
| Dal | Kuyruk | Teknoloji | Seviye | Açılış | Odak |
| --- | --- | ---: | ---: | --- | --- |
| [Gelişim](arastirma/development.md) | Araştırma Merkezi | 13 | 97 | Araştırma Merkezi 1 (Reaktör 3) | İnşa ve araştırma hızı, ikinci inşaatçı ve ikinci tezgâh, Takviye, Aşırı Yük ve kurtarma görevleri: üssün iş temposu. |
| [Ekonomi](arastirma/economy.md) | Araştırma Merkezi | 16 | 134 | Araştırma Merkezi 1 (Reaktör 3) | Altı kaynağın üretimi, depo, ek üretim hatları, haritada toplama hızı ve yükü; geç dönemde karanlık oksijen ve metan hidrat. |
| [Hayatta Kalma](arastirma/survival.md) | Araştırma Merkezi | 12 | 95 | Araştırma Merkezi 4 | Oksijen, gıda ve enerji tüketimi, basınç ve deprem olayları, Revir, iyileştirme ve yaralı oranı: üssü ve askeri ayakta tutmak. |
| [Derinlik](arastirma/depth.md) | Araştırma Merkezi | 12 | 73 | Araştırma Merkezi 7 | Gövde basınç sınıfı, sonar, seferler, Sonar Enerjisi, Tür Kataloğu ve derin kuşaklarda toplama: çukurun dibine inmenin bilgisi. |
| [Birlik](arastirma/troops.md) | Bilim Kubbesi | 21 | 147 | Bilim Kubbesi 1 (Reaktör 12) | Dört birlik türünün saldırı, savunma ve dayanıklılığı, T4–T10 kademe açılışları, eğitim hızı ve partisi. |
| [Kahraman](arastirma/heroes.md) | Bilim Kubbesi | 10 | 85 | Bilim Kubbesi 1 (Reaktör 12) | Kahraman deneyimi, istatistikleri, öfke, mürettebat becerileri, Subay deneyimi, fraksiyon sinerjisi ve teçhizat. |
| [Saldırı Taktikleri](arastirma/attack_tactics.md) | Bilim Kubbesi | 11 | 100 | Bilim Kubbesi 2 (Reaktör 20) | Saldırıda ve rallide birlik gücü, ralli kapasitesi, kalkan delme, yağma yükü, sayaç avantajı ve gizli yaklaşma. |
| [Savunma Taktikleri](arastirma/defense_tactics.md) | Bilim Kubbesi | 11 | 95 | Bilim Kubbesi 2 (Reaktör 20) | Savunmada birlik gücü, Kalkan Jeneratörü, ittifak takviyesi, korunan kaynak, keşif engelleme, erken uyarı ve tuzaklar. |
| [Filo](arastirma/fleet.md) | Bilim Kubbesi | 12 | 110 | Bilim Kubbesi 2 (Reaktör 20) | Dört Denizaltı Hangarı'nın her birine özel ekip bonusu, denizaltı kapasitesi, dayanıklılığı, hızı, onarımı ve Subay katkısı. |
| [Konvoy](arastirma/convoy.md) | Bilim Kubbesi | 9 | 56 | Bilim Kubbesi 2 (Reaktör 20) | İttifak içi takas konvoyları: yük, hız, vergi, eskort, sigorta, ek hat, kaçakçı rafları; Leviathan Konvoyu maçı. |
| [Derinlik Ligi](arastirma/depth_league.md) | Bilim Kubbesi | 8 | 40 | Bilim Kubbesi 2 (Reaktör 20) | Haftalık ittifak ligi: her günün temasında (inşa, araştırma, kahraman, toplama, eğitim, av) daha çok puan ve kademe ödülü. |
| [Leviathan Avı](arastirma/leviathan_hunt.md) | Bilim Kubbesi | 9 | 56 | Bilim Kubbesi 3 (Reaktör 30) | Leviathan'lara karşı ittifak rallileri ve haftalık av: hasar, zırh delme, ralli, ganimet, Sonar Enerjisi, Av Feneri ve Av Halkası. |
| [Kadim Teknoloji](arastirma/ancient_tech.md) | Bilim Kubbesi | 10 | 50 | Bilim Kubbesi 3 ve Araştırma Merkezi 35; Süperalaşım ister | Hadalit'i çözen kadim uygarlığın mirası: tüm üretim, Süperalaşım dökümü, tüm birlikler, kalkan, Kadim Muhafızlar ve Hadal Titanı. |

Toplam: **154 teknoloji, 1138 seviye.**
<!-- /URET:arastirma-ozet -->

Her dalın teknoloji listesi, seviye tablosu ve bugünkü koddaki karşılığı kendi sayfasındadır.
Aşağıda her dalın rolü, öne çıkan teknolojileri ve ücretsiz oyuncu için anlamı yazar.

### 5.1 Gelişim

Üssün iş temposu: inşa ve araştırma hızı, iki inşaatçı ve iki tezgâh. **Araştırma Yöntemleri 5** ve
**İkinci Tezgâh** oyunun ilk saatinde, **İkinci İnşaatçı** ilk gününde gelir (K-07). Takviye Ayarı
Takviye'yi uzatır, Aşırı Yük Soğutması reaktör devrelerini soğuk dip suyuyla soğutup Aşırı Yük'ün
soğuma süresini kısaltır, Kurtarma Planlaması Dron Fabrikası'nın kurtarma görevlerini dolu döndürür.
Hiperbarik Kaynak, açık deniz boru hatlarında kullanılan basınç odası kaynağıdır; halkaları ucuzlatır.
İkinci kuşak **Modüler Montaj** ve **Yağ Dengeli Elektronik** Lab 20'den sonra gelir ve dalı 47–48.
Lab seviyelerine taşır. Ücretsiz oyuncunun dalı bitirdiği an:
<!--v:veri.arastirma.f2p_takvim.development_tamam.gun|gun-->343. gün<!--/v-->.
[Dal sayfası](arastirma/development.md)

### 5.2 Ekonomi

Altı kaynağın üretimi ve depo. Kodda olan beş verim düğümü (Oksijen, Gıda, Biyokütle, Titanyum, Depo)
ilk iki günde biter; **Ek Oksijen Hattı** ve **Ek Dron Hattı** Reaktör 12/20 ve 14/22'de birer üretim
binası daha kurdurur ([03](03_binalar.md)). İkinci kuşak, derin denizin besin ve metal kaynaklarını
açar: **Kobalt Kabuğu Kazıma** deniz dağlarının demir-mangan kabuklarından yan ürün titanyum,
**Bakteri Matı Hasadı** soğuk sızıntıların metanla beslenen bakteri matlarını, **Kemosentez Kültürü**
baca bakterilerinin hidrojen sülfürden besin üretmesini kullanır. **Hidrotermal Isı Eşanjörü** kara
bacaların ısısını, geç dönemdeki **Metan Hidrat Hücreleri** basınç ve soğukta su kafesine hapsolmuş
metanı enerjiye çevirir. **Deniz Karı Tuzakları** oşinografların sediment tuzaklarıdır; deniz karı
olaylarında Gıda ve Biyokütle kazancını artırır. Toplama hızı ve yükü de buradadır.

**Karanlık Oksijen** deneysel bir daldır: 2024'te nodül sahalarında ölçülen ama henüz bağımsız olarak
tekrarlanmamış bulgudan doğar ([18 · Bilim Temeli](18_bilim_temeli.md)). Oyunda ana oksijen kaynağı
olmaz; küçük bir üretim payı verir ve ilk seviyesi Nodül Sahası'nda Karanlık Oksijen Hücresi bulma
şansını açar ([09](09_dunya_haritasi_ve_pvp.md)).

Ekonomi, ücretsiz oyuncunun ilk bitirdiği daldır; bittiği an:
<!--v:veri.arastirma.f2p_takvim.economy_tamam.gun|gun-->322,4. gün<!--/v-->.
[Dal sayfası](arastirma/economy.md)

### 5.3 Hayatta Kalma

Üssü ve askeri ayakta tutan dal. **Hava Geri Dönüşümü** denizaltılardaki gibi karbondioksiti lityum
hidroksitle tutar; **Kimyasal Oksijen Mumu** oksijen tükendiğinde mürettebatı birkaç saat daha ayakta
tutar; **Hipoksi Tatbikatı** oksijen açığındaki üretim kaybını azaltır. **Basınç Gövdesi** ve **Sismik
Ağ** doğa olaylarının hasarını düşürür: hadal çukurları dalma-batma kuşaklarıdır, büyük depremler ve
bulanıklık akıntıları burada doğar. Revir tarafında **Triyaj Protokolü**, **Revir Genişletme**,
**Hiperbarik Tıp** ve **Kriyo Taşma Protokolü** vardır. **Tahliye Kapsülleri** savaş kayıplarının
daha büyük kısmını ölü yerine yaralı saydırır; K-21'in yaralı/ölü kuralıyla birlikte çalışır
([07](07_birlikler_ve_savas.md)).

Ücretsiz oyuncu ilk yılın sonunda dalın
<!--v:veri.arastirma.butce.survival.f2p_yuzde|yuzde-->%88,6<!--/v--> kadarındadır; eksik kalan, Aerojel
Yalıtım ve Erzak Disiplini gibi teknolojilerin son, uzun seviyeleridir.
[Dal sayfası](arastirma/survival.md)

### 5.4 Derinlik

Çukurun dibine inmenin bilgisi. **Çok Huzmeli Sonar** sisi geniş açar, **SOFAR Kanalı Rölesi** keşif
dronlarını hızlandırır, **Sonar Enerjisi Kapasitörü** haritadaki PvE saldırıları için Sonar Enerjisi'ni
(K-22) hızlı doldurur. **Sintaktik Köpük**, epoksiye gömülü içi boş cam kürecikler, seferleri hızlandırır.
**Tür Kataloğu Taraması** ve **Balina Leşi Haritalama** canlıları incitmeden kaydeder ve leşlerden
biyokütle toplar (K-25). **Piyezolit Çalışması**, derin balıkların proteinlerini basınca karşı koruyan
TMAO'dan yola çıkar ve derin kuşak cezasını azaltır. **Nodül Sahası Etüdü** Süperalaşım'ın hammaddesi
olan nodül sahalarında toplamayı hızlandırır.

Üç teknoloji **gövde basınç sınıfını** yükseltir. Her biri yalnızca kendi bandında bir sınıf ekler;
böylece araştırma, İskele'nin verdiği sınıfı her an en çok bir adım öne taşır ([08](08_denizaltilar_basinc_seferler.md)):

| Teknoloji | Lab | İskele bu sınıfı verirken | Gövde sayılır | F2P |
|---|---:|---|---|---:|
| Titanyum Basınç Küresi | <!--v:hesap.arastirma.hull_titanium_sphere.1.lab-->15<!--/v--> | P3–P5 | P4–P6 | <!--v:veri.arastirma.f2p_takvim.hull_titanium_sphere.gun|gun-->5,3. gün<!--/v--> |
| Alümina Seramik Muhafaza | <!--v:hesap.arastirma.hull_ceramic_housing.1.lab-->26<!--/v--> | P6–P8 | P7–P9 | <!--v:veri.arastirma.f2p_takvim.hull_ceramic_housing.gun|gun-->21,9. gün<!--/v--> |
| Süperalaşım Takviye Halkaları | <!--v:hesap.arastirma.hull_superalloy_rings.1.lab-->41<!--/v--> | P9 | P10 | <!--v:veri.arastirma.f2p_takvim.hull_superalloy_rings.gun|gun-->165,3. gün<!--/v--> |

Titanyum küre, tam okyanus derinliğine defalarca inen insanlı araçların tek parça gövdesidir. Seramik
muhafaza, hafiflik ile güvenlik payı arasındaki dengenin dersini taşır: insansız Nereus aracının
kaybında olduğu gibi tek bir seramik parçanın çökmesi bütün aracı kaybettirebilir; Hadal'ın muhafazası
bu yüzden yedeklidir. Basınç sınıfı Kronik kapılarını ve kampanya bölümlerinin reaktör şartını
değiştirmez; açık kuşaklardaki daha derin basamaklara erken inmeyi sağlar.

Ücretsiz oyuncu ilk yılın sonunda dalın
<!--v:veri.arastirma.butce.depth.f2p_yuzde|yuzde-->%78,5<!--/v--> kadarındadır.
[Dal sayfası](arastirma/depth.md)

### 5.5 Birlik

Dört türün (Zırhlı Dalgıç, Zıpkıncı, Ark Askeri, Delici) saldırı, savunma ve dayanıklılığı ile kademe
açılışları. **Kademe IV–X Doktrini** dört türün T4–T10 kademesini birlikte açar; her doktrin Tatbikat
Havuzu'nun aynı seviyesini de ister (15, 20 … 45), IX ve X Süperalaşım ister ([07](07_birlikler_ve_savas.md)).
Türlerin istatistik teknolojileri birer mühendislik fikrine dayanır: Zırhlı Dalgıç'ın **Demir Sülfür
Plakaları** baca sahalarındaki pullu ayaklı salyangozun demir sülfür pullarını taklit eden sentetik
plakalardır (salyangozun yaşadığı bacalar kazı alanı değildir), Delici'nin **Elmas Uçlu Matkap**'ı
okyanus sondajının sentetik elmas uçlarıdır, Ark Askeri'nin **Faraday Örgüsü** akımı vücudun dışından
geçirir. **Basınçlı Tatbikat** eğitimi hızlandırır, **Parti Planlaması** partiyi büyütür.

| Kademe | F2P'nin doktrini bitirdiği an |
|---|---:|
| T4 | <!--v:veri.arastirma.f2p_takvim.tier_4_doctrine.gun|gun-->2,9. gün<!--/v--> |
| T5 | <!--v:veri.arastirma.f2p_takvim.tier_5_doctrine.gun|gun-->7,4. gün<!--/v--> |
| T6 | <!--v:veri.arastirma.f2p_takvim.tier_6_doctrine.gun|gun-->14,9. gün<!--/v--> |
| T7 | <!--v:veri.arastirma.f2p_takvim.tier_7_doctrine.gun|gun-->34,7. gün<!--/v--> |
| T8 | <!--v:veri.arastirma.f2p_takvim.tier_8_doctrine.gun|gun-->73,8. gün<!--/v--> |
| T9 | <!--v:veri.arastirma.f2p_takvim.tier_9_doctrine.gun|gun-->146,8. gün<!--/v--> |
| T10 | <!--v:veri.arastirma.f2p_takvim.tier_10_doctrine.gun|gun-->254,7. gün<!--/v--> |

Ücretsiz oyuncu her doktrini kapısı açılır açılmaz araştırır; dalın eksik kalan kısmı, istatistik
teknolojilerinin Lab 44 üstündeki uzun seviyeleridir.
[Dal sayfası](arastirma/troops.md)

### 5.6 Kahraman

**Komuta Simülatörü** deneyimi, **Eğitim Bütçesi** eğitim bedelini, **Nöral Taktik Arayüzü**,
**Kişisel Basınç Zırhı** ve **Biyomedikal İzleme** kahramanın saldırı, savunma ve canını artırır.
**Mürettebat Koçluğu** ekonomi yuvasındaki kahramanların mürettebat becerisini güçlendirir;
**Subay Deneyimi** Subayların savaş deneyiminden aldığı payı, **Fraksiyon Sinerjisi** aynı fraksiyondan
kahramanların sinerji bonusunu büyütür ([05](05_kahramanlar.md)). **Adrenalin Protokolü** öfkeyi,
**Teçhizat Uyumu** teçhizat istatistiklerini ([17](17_techizat.md)) artırır.
[Dal sayfası](arastirma/heroes.md)

### 5.7 Saldırı Taktikleri

Üsse ve yapılara saldıranın dalı. Saldırıda birlik saldırısı, savunması ve dayanıklılığı; ralliye
özel **Toplu Hücum** ve **Ralli Kapasitesi**; Delici ekiplerinin **Kalkan Delme**'si; **Yağma
Bölmeleri**; **Sayaç Doktrini** (Zırhlı > Ark > Zıpkıncı > Zırhlı üçgeninde avantaj). **Sessiz
Pervane** eğik kanatlı pervanelerle kavitasyonu azaltıp saldırı yürüyüşünü hızlandırır; **Karşı
Aydınlatma**, orta su canlılarının karınlarındaki ışıkla gölgelerini silmesinden esinlenir ve yürüyüşü
düşman radarında daha geç gösterir.
[Dal sayfası](arastirma/attack_tactics.md)

### 5.8 Savunma Taktikleri

Kendi kubbesini savunanın dalı. Savunmada birlik istatistikleri, **Kalkan Sertleştirme**, ittifak
takviyesi için **Takviye Limanı**, yağmadan korunan kaynağı artıran **Ambar Kilitleri**, **Basınç
Mayınları** ve ralliye karşı **Akustik Yem**. **Hidrofon Dizisi** deniz tabanına döşenen dinleme
dizileriyle saldırı uyarısını erkene çeker; **Yankısız Kaplama** denizaltı gövdelerindeki kauçuk
kaplamalar gibi sonar dalgasını yutar ve düşman keşif raporunu bulanıklaştırır; **Su Geçirmez
Bölmeler** savunma kayıplarının daha büyük kısmını yaralı saydırır.
[Dal sayfası](arastirma/defense_tactics.md)

### 5.9 Filo

Dört Denizaltı Hangarı'nın her birine kendi ekip bonusu: **1.–4. Hangar Ekibi** o denizaltıdaki
birliklerin saldırı ve savunmasını artırır, böylece oyuncu en güçlü filosunu bir hangara bağlar.
**Asker Bölmeleri** denizaltı kapasitesini, **Kompozit Gövde Levhaları** dayanıklılığı,
**Hidrodinamik Gövde** yürüyüş hızını, **Kuru Havuz Robotları** onarımı, **Seri Gövde Üretimi**
İskele'nin inşa hızını artırır. **Basınca Dayanıklı Batarya** yağ dolu batarya kutularıyla dalışın
oksijen maliyetini düşürür. **Subay Eğitimi** Subay istatistiklerinin askerlere geçen payını büyütür
([08](08_denizaltilar_basinc_seferler.md)); **Filo Düzeni** aynı hedefe giden denizaltıları güçlendirir.
[Dal sayfası](arastirma/fleet.md)

### 5.10 Konvoy

İttifak içi takas konvoyları: yük, hız, eskort, sigorta. **Derin Akıntı Rotaları** yavaş ama kesintisiz
derin akıntıları kullanır; **Saçılma Katmanı Gizlemesi**, her gece yukarı göç eden canlıların
oluşturduğu ve sonarı yanıltan derin saçılma katmanında konvoyu gizler. **Takas Vergisi Muafiyeti**
Takas İstasyonu'nun vergisini puan puan düşürür, **Kaçakçı Ağı** her kaçakçı ziyaretinde bir raf daha
açar ([13](13_etkinlikler_ve_sezonlar.md)). **Tersane Rotasyonu** Leviathan Konvoyu maçında yenilen
yürüyüşün yeniden çıkışını hızlandırır.
[Dal sayfası](arastirma/convoy.md)

### 5.11 Derinlik Ligi

Haftalık ittifak ligi: her gün bir tema (Pazartesi inşa, Salı araştırma, Çarşamba kahraman, Perşembe
toplama, Cuma eğitim, Cumartesi av; [13](13_etkinlikler_ve_sezonlar.md)). Her temanın teknolojisi o
günün puanını artırır; **Kademe Ödülleri** günlük kademe sandığının Titanyum ve hızlandırma içeriğini
büyütür (Hadalit payı değişmez), **Lig Koordinasyonu** her güne küçük bir pay ekler. Dal küçüktür ve
ucuzdur: ücretsiz oyuncu ilk yılın sonunda dalın
<!--v:veri.arastirma.butce.depth_league.f2p_yuzde|yuzde-->%79<!--/v--> kadarındadır.
[Dal sayfası](arastirma/depth_league.md)

### 5.12 Leviathan Avı

Leviathan'lara karşı ittifak rallileri ve haftalık Leviathan Avı etkinliği. **Zıpkın Halatları**
hasarı, **Demir Plaka Delici** Demir Leviathan'ın zırhını delmeyi, **Av Rallisi** ve **Akıntıyla
Toplanma** ralliyi, **Av Ganimeti** ödülü artırır. **Leviathan İz Sürme** avın Sonar Enerjisi
maliyetini düşürür. Etkinliğe özel iki teknoloji vardır: **Biyolüminesans Fener**, fener balığının
ışıyan oltasından esinlenir ve Av Feneri'ni dayanıklı kılar; **Üçüncü Av Hattı** Av Halkası'na üye
başına bir yürüyüş daha yerleştirir. **Av Sağlıkçıları** PvE yaralılarının Askeri Kubbe'deki dinlenme
süresini kısaltır. Hadal Titanı'nın ikinci evresinde çağrılan Demir Leviathan'lar bu dalın
teknolojileriyle ayrı rallilerle indirilir ([14 · Son Oyun](14_son_oyun.md)).
[Dal sayfası](arastirma/leviathan_hunt.md)

### 5.13 Kadim Teknoloji

Hadalit'i ilk çözen uygarlığın mirası. Bilim Kubbesi 3 ve Lab 35 ile açılır, her seviyesi Süperalaşım
ister. **Hadalit Rezonansı** bütün üretimi, **Kadim Döküm** Süperalaşım üretimini, **Rezonans
Silahları** ve **Kadim Kafes Zırh** bütün birliklerin saldırısını ve dayanıklılığını, **Kadim Kalkan
Matrisi** kalkanı, **Kadim Çekirdek** enerjiyi artırır. **Muhafız Şifreleri** Kadim Muhafızlara,
**Titan Etüdü** Hadal Titanı'na verilen hasarı yükseltir; **Kalıntı Haritacılığı** ve **Kodeks
Çözümleme** kalıntı ve Basınç Kodeksi kazancını büyütür. Ücretsiz oyuncunun ilk Kadim seviyesini
bitirdiği an: <!--v:veri.arastirma.f2p_takvim.ancient_first.gun|gun-->178,1. gün<!--/v-->. Yılın
sonunda dalın <!--v:veri.arastirma.butce.ancient_tech.f2p_yuzde|yuzde-->%22<!--/v--> kadarı biter;
dalın tamamı ikinci yılın ve ödeme yapan oyuncunun hedefidir ([14 · Son Oyun](14_son_oyun.md)).
[Dal sayfası](arastirma/ancient_tech.md)

### 5.14 Bilim bağlantıları

Teknolojilerin dayandığı `bilim.json` kayıtları. Derinlik, basınç, sıcaklık ve tarih gibi sayılar
yalnızca bu kayıtlardan gelir ([18 · Bilim Temeli](18_bilim_temeli.md)).

<!-- URET:tablo:arastirma.bilim_baglantilari -->
| Teknoloji | Dal | bilim.json kaydı | Kimlik |
| --- | --- | --- | --- |
| Gıda Verimi | Ekonomi | Kemosentez (kural) | kemosentez |
| Titanyum Geri Kazanımı | Ekonomi | Titanyum Kaynakları (İlmenit ve Rutil Plaserleri) (mineral) | titanyum_kaynaklari |
| Hidrotermal Isı Eşanjörü | Ekonomi | Kara Baca (enerji) · Rekor Baca Sıcaklığı (enerji) | kara_baca, rekor_baca |
| Hafif Yük Kapları | Ekonomi | Sintaktik Köpük (mineral) | sintaktik_kopuk |
| Deniz Karı Tuzakları | Ekonomi | Deniz karı (kural) · Yoğun Deniz Karı (doğa olayı) | deniz_kari, deniz_kari_olayi |
| Kobalt Kabuğu Kazıma | Ekonomi | Kobalt Zengini Ferromangan Kabuk (mineral) | kobalt_kabuk |
| Bakteri Matı Hasadı | Ekonomi | Soğuk Sızıntı ve Metan Çıkışı (doğa olayı) · Kemosentez (kural) | soguk_sizinti, kemosentez |
| Kemosentez Kültürü | Ekonomi | Kemosentez (kural) | kemosentez |
| Karanlık Oksijen | Ekonomi | Karanlık Oksijen (keşif) · Polimetalik Nodül (mineral) | karanlik_oksijen, polimetalik_nodul |
| Metan Hidrat Hücreleri | Ekonomi | Metan Hidrat (Gaz Hidrat) (mineral) · Metan Hidrat (yakıt) (enerji) | metan_hidrat, metan_hidrat_yakit |
| Sismik Ağ | Hayatta Kalma | Çukur (Megathrust) Depremi (doğa olayı) · Bulanıklık Akıntısı (doğa olayı) | megathrust_deprem, bulaniklik_akintisi |
| Balina Leşi Haritalama | Derinlik | Balina düşüşü (kural) · Balina Düşüşü (doğa olayı) | balina_dususu, balina_dususu_olayi |
| Sintaktik Köpük | Derinlik | Sintaktik Köpük (mineral) · Deepsea Challenger (araç) | sintaktik_kopuk, deepsea_challenger |
| Tür Kataloğu Taraması | Derinlik | En Derin Balık (keşif) | en_derin_balik |
| Titanyum Basınç Küresi | Derinlik | DSV Limiting Factor (Triton 36000/2) (araç) | limiting_factor |
| Piyezolit Çalışması | Derinlik | Balık derinlik sınırı (kural) | balik_derinlik_siniri |
| Alümina Seramik Muhafaza | Derinlik | Nereus (araç) | nereus |
| Nodül Sahası Etüdü | Derinlik | Polimetalik Nodül (mineral) | polimetalik_nodul |
| Süperalaşım Takviye Halkaları | Derinlik | Polimetalik Nodül (mineral) | polimetalik_nodul |
| Demir Sülfür Plakalar | Birlik | Pullu Ayaklı Salyangoz (tür) | scaly_foot_snail |
| Biyolüminesans Fener | Leviathan Avı | Kambur Fener Balığı (tür) | humpback_anglerfish |
<!-- /URET:tablo:arastirma.bilim_baglantilari -->

---

## 6. Ücretsiz oyuncu için önerilen sıra

Sıra, §7'deki bütçe simülasyonunun oynadığı sıradır: her tezgâh boşaldığında açık seviyeler arasından
en düşük Lab'lı olanı seçer; dallar arasında Ekonomi ve Gelişim önce, sonra Birlik, Kahraman, Konvoy,
Derinlik Ligi ve Filo, sonra Hayatta Kalma ve Derinlik, en sonda Leviathan Avı, Taktikler ve Kadim
Teknoloji gelir. Kademe doktrinleri, basınç sınıfı teknolojileri, ek üretim hatları ve tezgâh/inşaatçı
düğümleri açıldıkları anda öne alınır.

### 6.1 Erken (ilk 5 gün, Reaktör 1–20)

| Tezgâh | Sıra |
|---|---|
| Araştırma Merkezi | Araştırma Yöntemleri 5 → İkinci Tezgâh → İnşaat Ustalığı 5 → İkinci İnşaatçı → Araştırma Yöntemleri 10 → Titanyum Geri Kazanımı ve Oksijen Verimi → Depo Mühendisliği → Ek Oksijen Hattı |
| İkinci Tezgâh | Gıda ve Biyokütle Verimi, İnşaat Ustalığı 10, Ek Dron Hattı; Bilim Kubbesi kurulunca boşta kaldığı anlarda kubbe dallarına |
| Bilim Kubbesi | Basınçlı Tatbikat 2 → Kademe IV Doktrini → ana türün saldırısı (ör. Hidrolik Pençe) → Komuta Simülatörü → Eğitim Bütçesi → Kademe V Doktrini |

İlk günlerde araştırma kaynakla sınırlıdır: Titanyum inşaata da gerekir ve tezgâhlar zamanın bir
kısmında kaynak bekler (§7.2). Bu bilinçli bir erken oyun seçimidir: "bu saat reaktör mü, verim mi?"

### 6.2 Orta (5.–128. gün, Reaktör 20–40)

| Tezgâh | Sıra |
|---|---|
| Araştırma Merkezi | Ekonomi'nin ikinci kuşağı (Kobalt Kabuğu, Karanlık Oksijen, Kemosentez, Bakteri Matı) ve Hidrotermal Isı Eşanjörü; her Lab açılışında Modüler Montaj ve Yağ Dengeli Elektronik; Titanyum Basınç Küresi ve Alümina Seramik Muhafaza çıkar çıkmaz |
| İkinci Tezgâh | En düşük Lab'lı açık seviye. Akıntı Savaşı'ndan önceki iki haftada Birlik ve Hayatta Kalma (Triyaj Protokolü, Revir Genişletme, Tahliye Kapsülleri) |
| Bilim Kubbesi | Her kademe doktrini kapısı açılır açılmaz; ana türün üç istatistiği; Kahraman (Nöral Taktik, Kişisel Basınç Zırhı); Filo (1. Hangar Ekibi, Asker Bölmeleri, Subay Eğitimi); Konvoy Yük Kapasitesi; lig günlerinin ilk seviyeleri |

### 6.3 Geç (128.–365. gün, Reaktör 40–49)

| Tezgâh | Sıra |
|---|---|
| Araştırma Merkezi | Gelişim ve Ekonomi'nin son seviyeleri; sonra Hayatta Kalma ve Derinlik (Nodül Sahası Etüdü, Süperalaşım Takviye Halkaları) |
| İkinci Tezgâh | Kubbe dallarında biriken seviyeler: Taktikler, Leviathan Avı (Titan'dan önce), Filo |
| Bilim Kubbesi | Kademe IX ve X Doktrini için Süperalaşım ayrılır; ardından Kadim Teknoloji'nin ilk seviyeleri (Hadalit Rezonansı, Muhafız Şifreleri, Kadim Döküm) |

**Rol farkı.** Savaşçı bir oyuncu İkinci Tezgâh'ı Reaktör 30'dan sonra kalıcı olarak kubbe dallarına
verir: Taktikler ve Birlik öne geçer, Gelişim ve Ekonomi'nin son seviyeleri ikinci yıla kayar. Toplayıcı
bir oyuncu Ekonomi ve Derinlik'i önde tutar. Her iki yol da ilk yılda bütün kademe doktrinlerine ulaşır.

### 6.4 F2P takvimi

<!-- URET:tablo:arastirma.f2p_takvim -->
| Araştırma | F2P'nin bitirdiği an | Anlamı |
| --- | ---: | --- |
| Araştırma Yöntemleri 5 | 4 sa 48 dk | İkinci Tezgâh'ın önkoşulu |
| İkinci Tezgâh | 4 sa 48 dk | Araştırma Merkezi'nde esnek ikinci tezgâh açılır |
| İkinci İnşaatçı | 4 sa 48 dk | Aynı anda iki inşaat |
| Bilim Kubbesi 1 (bina) | 14 sa 24 dk | Kubbe kuyruğu açılır |
| Kademe IV Doktrini | 2,9. gün | Dört türün T4 kademesi açılır |
| Araştırma Yöntemleri 10 | 3,8. gün | Araştırma hızı +%50 |
| Ek Oksijen Hattı 2 | 4,7. gün | Oksijen Tesisi sınırı +2 |
| Titanyum Basınç Küresi | 5,3. gün | İskele P3–P5 iken +1 sınıf |
| Karanlık Oksijen 1 | 6,8. gün | Deneysel dal açılır |
| Kademe V Doktrini | 7,4. gün | Dört türün T5 kademesi açılır |
| Ek Dron Hattı 2 | 8,3. gün | Dron Fabrikası sınırı +2 |
| Kademe VI Doktrini | 14,9. gün | Dört türün T6 kademesi açılır |
| Alümina Seramik Muhafaza | 21,9. gün | İskele P6–P8 iken +1 sınıf |
| Kademe VII Doktrini | 34,7. gün | Dört türün T7 kademesi açılır |
| Kademe VIII Doktrini | 73,8. gün | Dört türün T8 kademesi açılır |
| Subay Eğitimi 1 | 78,6. gün | Subay istatistiklerinin filoya katkısı artar |
| Kademe IX Doktrini | 146,8. gün | Dört türün T9 kademesi açılır |
| Süperalaşım Takviye Halkaları | 165,3. gün | İskele P9 iken +1 sınıf (P10) |
| İlk Kadim Teknoloji seviyesi | 178,1. gün | Süperalaşımla araştırma başlar |
| Kademe X Doktrini | 254,7. gün | Dört türün T10 kademesi açılır |
| Karanlık Oksijen 10 | 322,4. gün | Ekonomi dalının son seviyelerinden |
| Ekonomi dalı tamam | 322,4. gün | Dalın bütün seviyeleri bitti |
| Gelişim dalı tamam | 343. gün | Dalın bütün seviyeleri bitti |
<!-- /URET:tablo:arastirma.f2p_takvim -->

---

## 7. Bütçe: bir yılda ne biter?

### 7.1 Dal başına toplam süre ve ilk yılın sonu

<!-- URET:tablo:arastirma.butce -->
| Dal | Teknoloji | Seviye | Toplam ham süre | %60 hızla | Toplam Titanyum | Süperalaşım | Hedef (365. gün) | F2P 365. gün (süreye göre) | F2P 365. gün (seviyeye göre) | Tüm hız kaynaklarıyla (süreye göre) | Dalın bittiği an |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gelişim | 13 | 97 | 198 g 1 sa 49 dk | 123 g 19 sa 8 dk | 36,13 M | — | %100 | %100 | %100 | %100 | 343. gün |
| Ekonomi | 16 | 134 | 282 g 23 sa 4 dk | 176 g 20 sa 25 dk | 52,51 M | — | %100 | %100 | %100 | %100 | 322,4. gün |
| Hayatta Kalma | 12 | 95 | 215 g 3 sa 48 dk | 134 g 11 sa 23 dk | 35,79 M | — | %90 | %88,6 | %97,9 | %100 | — |
| Derinlik | 12 | 73 | 231 g 12 sa 46 dk | 144 g 16 sa 59 dk | 41,41 M | 65 | %80 | %78,5 | %94,5 | %100 | — |
| Birlik | 21 | 147 | 425 g 16 sa 9 dk | 266 g 1 sa 5 dk | 85,82 M | 465 | %70 | %68,9 | %90,5 | %75,1 | — |
| Kahraman | 10 | 85 | 138 g 23 sa 8 dk | 86 g 20 sa 27 dk | 26,2 M | — | %70 | %69,3 | %91,8 | %80,5 | — |
| Saldırı Taktikleri | 11 | 100 | 187 g 21 sa 45 dk | 117 g 10 sa 35 dk | 35,23 M | — | %50 | %47,5 | %78 | %54,5 | — |
| Savunma Taktikleri | 11 | 95 | 171 g 3 sa 35 dk | 106 g 23 sa 14 dk | 31,78 M | — | %50 | %51,9 | %81,1 | %60,1 | — |
| Filo | 12 | 110 | 189 g 9 sa 49 dk | 118 g 9 sa 8 dk | 35,92 M | — | %55 | %55,9 | %82,7 | %69,4 | — |
| Konvoy | 9 | 56 | 80 g 11 sa 53 dk | 50 g 7 sa 25 dk | 14,97 M | — | %70 | %73,9 | %91,1 | %78,2 | — |
| Derinlik Ligi | 8 | 40 | 51 g 15 sa 33 dk | 32 g 6 sa 43 dk | 9 M | — | %75 | %79 | %92,5 | %85,6 | — |
| Leviathan Avı | 9 | 56 | 138 g 9 sa 10 dk | 86 g 11 sa 43 dk | 26,63 M | — | %40 | %39,9 | %62,5 | %53,1 | — |
| Kadim Teknoloji | 10 | 50 | 225 g 6 sa 30 dk | 140 g 19 sa 3 dk | 46,19 M | 11.050 | %20 | %22 | %36 | %30,5 | — |
| Toplam | 154 | 1.138 | 2536 g 15 sa | 1585 g 9 sa 22 dk | 477,59 M | 11.580 | — | %68,3 | %87,1 | %76,7 | — |
<!-- /URET:tablo:arastirma.butce -->

Bütün ağacın ham süresi <!--v:veri.arastirma.butce.toplam.ham_sure|sure-->2536 g 15 sa<!--/v-->, en yüksek
araştırma hızıyla <!--v:veri.arastirma.butce.toplam.f2p_sure|sure-->1585 g 9 sa 22 dk<!--/v--> tezgâh zamanıdır; üç
tezgâhın bir yılda verebileceğinin kabaca dört katı. "Süreye göre" yüzde, bitirilen seviyelerin ham
süresinin dalın toplam ham süresine oranıdır; "seviyeye göre" yüzde her zaman daha yüksektir, çünkü
eksik kalanlar dalın en uzun, en yüksek Lab'lı seviyeleridir. Örneğin Saldırı Taktikleri seviyelerin
çoğunu bitirmiş görünür ama sürenin yarısı son seviyelerdedir.

Hedefler: Gelişim ve Ekonomi %100, Hayatta Kalma ~%90, Derinlik ~%80, Birlik ~%70, Taktikler ~%50,
Kadim Teknoloji ~%20. Seviye adımları büyük olduğundan simülasyon hedefleri birkaç puanlık sapmayla
tutturur.

### 7.2 Kaynak

<!-- URET:tablo:arastirma.kaynak_payi -->
| Dönem | Reaktör | Araştırmaya giden Titanyum | F2P Titanyum geliri | Pay | Araştırmaya giden Süperalaşım | F2P Süperalaşım geliri | Kuyrukların kaynak beklediği zaman |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 0–5. gün | 1–20 | 709,6 B | 2,93 M | %24,2 | — | — | %56,8 |
| 5–29. gün | 20–30 | 9,26 M | 37,39 M | %24,8 | — | 14 | %27,2 |
| 29–70. gün | 30–35 | 25,71 M | 103,48 M | %24,8 | — | 1.990 | %1,4 |
| 70–128. gün | 35–39 | 40,7 M | 199,2 M | %20,4 | — | 4.406 | %0 |
| 128–225. gün | 39–45 | 80,47 M | 449,12 M | %17,9 | 550 | 11.667 | %0 |
| 225–365. gün | 45–49 | 139,86 M | 861,83 M | %16,2 | 2.465 | 25.302 | %0 |
<!-- /URET:tablo:arastirma.kaynak_payi -->

Araştırmaya ücretsiz oyuncunun Titanyum (ve Biyokütle) gelirinin dörtte biri ayrılır. İlk ay bu pay
yetmez; tezgâhlar zamanın bir kısmında kaynak bekler, oyuncu inşaatla araştırma arasında seçim yapar.
Reaktör 30'dan sonra araştırmanın gelirdeki payı düşer
(son dönemde <!--v:veri.arastirma.kaynak_payi.5.ti_pay|yuzde-->%16,2<!--/v-->): geç oyunda araştırmayı
zaman sınırlar. Süperalaşım tarafında araştırma, ücretsiz oyuncunun yıllık Süperalaşım gelirinin küçük
bir kısmını kullanır; ama Kadim Teknoloji'nin tamamı (<!--v:veri.arastirma.butce.ancient_tech.alasim-->11.050<!--/v-->
Süperalaşım) bir yıllık araştırma payını aşar. Dal bu yüzden ücretsiz oyuncu için ikinci yılın işidir.

### 7.3 Model

Bütçe, [02 · Tempo ve Denge](02_tempo_ve_denge.md)'deki F2P tempo simülasyonunun reaktör takvimi
üzerinde oynatılır:

- **Tezgâhlar.** Araştırma Merkezi (yalnız Lab 1 dalları) Reaktör 3'ten, esnek İkinci Tezgâh
  araştırıldığı andan, Bilim Kubbesi kubbe kurulduğu andan itibaren çalışır.
- **Lab seviyesi** her gün o günkü reaktör seviyesidir; **kubbe seviyesi** reaktör kapısına
  ulaşıldıktan sonra kendi inşa süresi kadar gecikir.
- **Hız** yalnızca araştırmadandır: Araştırma Yöntemleri ve Yağ Dengeli Elektronik
  (en çok %60). İttifak yardımı, ücretsiz bitirme ve çevrimdışı bekleme inşaatla aynı modeldir.
  Araştırma hızlandırması kullanılmaz.
- **Kaynak.** Titanyum ve Biyokütle gelirinin
  <!--v:veri.arastirma.kalibrasyon.titanyum_payi-->0,25<!--/v--> payı, Süperalaşım gelirinin
  <!--v:veri.arastirma.kalibrasyon.alasim_payi-->0,15<!--/v--> payı araştırmaya ayrılır.
- **Ufuk** 365. gündür.

**Duyarlılık.** Tablodaki "Tüm hız kaynaklarıyla" sütunu aynı simülasyonu Derinlik İzni (İzin 4 → 7),
mürettebattaki araştırma kahramanı ve ittifak teknolojisiyle birlikte oynatır (ek hız ilk ay %10'dan yıl
sonunda %32'ye çıkar). Ücretsiz oyuncunun bu payı, kaçırdığı tezgâh zamanını ve uykusunu karşılayan
yedektir; düzenli oynayan biri savaş dallarında hedeflerin 5–15 puan üstüne çıkar.

Hesap betiği kılavuz üreticisinin dışındadır; varsayımlar `veri/arastirma.json` → `kalibrasyon`
alanında durur. Ağaç değiştiğinde betik yeniden çalıştırılıp `butce`, `f2p_takvim` ve `kaynak_payi`
güncellenir.

---

## 8. Sezon teknolojisi (Akıntı Savaşı)

Çukurlar arası sezon savaşı Akıntı Savaşı'nın kendi teknoloji ağacı vardır
([13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md), §5.4). Bu bölümün ağacından dört
farkı vardır:

- **Para birimi.** Kaynak değil, sezon haritasındaki Öz Damarları'ndan toplanan **Akıntı Özü** harcar.
- **Tezgâh kullanmaz.** Sezon ağacının tavanı zaman değil Öz'dür ve para onu hızlandırmaz; düğüm Öz
  ödenince açılır. Üç araştırma tezgâhı sezon boyunca normal işine devam eder.
- **Yalnız Havza'da geçerlidir** ve sezon bitince sıfırlanır; kalıcı güç vermez.
- **Kısa ve geniş.** Dört küçük dal (Akıntı Okuma, Öz Rafinerisi, Basınç Zırhı, Saha Revir'i), her
  düğüm beş seviye; ücretsiz oyuncu bir sezonda ağacın büyük kısmını bitirir.

Kalıcı ağaçtaki Hayatta Kalma (Tahliye Kapsülleri, Revir) ve Birlik teknolojileri sezonda da çalışır;
sezon ağacı onların üstüne eklenir. Sezondan iki hafta önce İkinci Tezgâh'ı bu dallara vermek önerilir.

---

## 9. Bugün kodda

| Konu | Kodda (catalog v8) | Hedef | Durum |
|---|---|---|---|
| Laboratuvar | Araştırma Merkezi, tek laboratuvar | Araştırma Merkezi + Bilim Kubbesi | 🟡 |
| Tezgâh | 1 + İkinci Tezgâh (`researchSlots`) | 3: merkez, esnek İkinci Tezgâh, kubbe | 🟡 |
| Dal | Gelişim, Ekonomi (11 düğüm) | <!--v:hesap.sayilar.arastirma_dali-->13<!--/v--> dal, <!--v:hesap.sayilar.teknoloji-->154<!--/v--> teknoloji | 🟡 |
| Maliyet ve süre | Taban × 1,4^(ℓ−1) ve × 1,5^(ℓ−1) | Reaktör eğrisi × dal ölçeği (§3) | 🟡 |
| Merkez kapısı | gerekli + 2 × (ℓ − 1), tek adım | Teknoloji başına `min_lab` ve `adim` | 🟡 |
| Hız | Yüzde süreden çıkarılır, en az %10 kalır (`PlayerModifiers.ApplyPercent`) | Bölen formülü (K-13) | 🟡 |
| Etki türleri | `buildTime`, `researchTime`, `production:*`, `capacity:All`, `limit:*`, `builderSlots`, `researchSlots` | + §10'daki yeni türler | 🟡 |

Kodda olan 11 düğümün kimliği, etkisi, en yüksek seviyesi ve seviye başı değeri korunur; yalnızca
maliyet, süre ve Lab adımı yeni formüle geçer. Hedef toplam ham süreleri:

| Düğüm | Dal | Hedef toplam ham süre |
|---|---|---:|
| İnşaat Ustalığı | Gelişim | <!--v:hesap.arastirma.construction_mastery.10.kumulatif_sure|sure-->8 sa 37 dk<!--/v--> |
| Araştırma Yöntemleri | Gelişim | <!--v:hesap.arastirma.research_methods.10.kumulatif_sure|sure-->8 sa 37 dk<!--/v--> |
| İkinci İnşaatçı | Gelişim | <!--v:hesap.arastirma.second_builder.1.kumulatif_sure|sure-->21 sn<!--/v--> |
| İkinci Tezgâh | Gelişim | <!--v:hesap.arastirma.second_workbench.1.kumulatif_sure|sure-->3 dk<!--/v--> |
| Oksijen Verimi | Ekonomi | <!--v:hesap.arastirma.oxygen_yield.10.kumulatif_sure|sure-->8 sa 5 dk<!--/v--> |
| Gıda Verimi | Ekonomi | <!--v:hesap.arastirma.food_yield.10.kumulatif_sure|sure-->8 sa 5 dk<!--/v--> |
| Biyokütle Verimi | Ekonomi | <!--v:hesap.arastirma.biomass_yield.10.kumulatif_sure|sure-->10 sa 48 dk<!--/v--> |
| Titanyum Geri Kazanımı | Ekonomi | <!--v:hesap.arastirma.titanium_recovery.10.kumulatif_sure|sure-->10 sa 48 dk<!--/v--> |
| Depo Mühendisliği | Ekonomi | <!--v:hesap.arastirma.storage_engineering.10.kumulatif_sure|sure-->14 sa<!--/v--> |
| Ek Oksijen Hattı | Ekonomi | <!--v:hesap.arastirma.extra_oxygen_line.2.kumulatif_sure|sure-->5 sa 7 dk<!--/v--> |
| Ek Dron Hattı | Ekonomi | <!--v:hesap.arastirma.extra_drone_line.2.kumulatif_sure|sure-->8 sa 30 dk<!--/v--> |

Bugünkü değerlerle seviye seviye karşılaştırma her düğümün dal sayfasındadır
([Gelişim](arastirma/development.md), [Ekonomi](arastirma/economy.md)). Kodda `buildTime` ve
`researchTime` eksi yüzde olarak tutulur (−3, −5); hedefte aynı değerler hız yüzdesidir (+3, +5) ve
bölen olarak uygulanır.

---

## 10. Uygulama notları

| Değişiklik | Kod |
|---|---|
| Düğüm alanları | `catalog.json` → `research.nodes[]`: `tree` (13 dal), `lab` (`center` / `dome`), `minLab`, `labStep` (bugünkü global `centerStep` yerine), `costScale`, `timeScale`, ek maliyet satırları (`Alloy`, eğri adıyla); `ResearchNode` ve `GameCatalog` doğrulaması |
| Maliyet ve süre | `ResearchRules.Cost` / `BaseSeconds` → reaktör eğrisi × ölçek (`BuildingProgression` ile aynı eğri tablosu); `CenterLevelFor` → `minLab + labStep × (ℓ − 1)` |
| Hız | `PlayerModifiers.ApplyPercent` → bölen formülü (K-13); `BuildTimePercent` ve `ResearchTimePercent` hız yüzdesi olarak tutulur |
| Tezgâhlar | `ResearchRules.Slots` üçe ayrılır: merkez (1), esnek (`researchSlots`), kubbe (Bilim Kubbesi kuruluysa 1). `Validate` hangi tezgâhın boş olduğunu ve teknolojinin o tezgâha uygun olup olmadığını denetler; yeni red nedenleri `DomeLevelTooLow`, `NoDome`, `WrongLab` |
| Kubbe kapısı | Kubbe dalında gereken kubbe seviyesi = max(dalın kubbesi, Lab'ın düştüğü kubbe bandı) (§2 tablosu) |
| Etki türleri | `ResearchRules.Modifiers` ve `IsKnownEffect` genişler: `consumption:*`, `upkeep:Food`, `healSpeed`, `healCost`, `capacity:wounded`, `woundedShare`, `gatherSpeed`, `gatherLoad`, `marchSpeed`, `trainSpeed`, `trainBatch`, `troopTier`, `atk/def/hp:<tür>`, `pressureClass:<band>`, `squad:<n>`, `officerShare`, `leaguePoints:*`, `dmg:*` ve dal sayfalarındaki diğerleri. Etkiler `PlayerModifiers`'ta toplanır; kahraman, VIP, köken ve ittifak etkileri aynı nesneye eklenir |
| Komutlar | `StartResearch` bir `slot` alır (`center`, `flex`, `dome`); `FinishResearch` değişmez. Araştırma sırası (İzin 9) için `QueueResearch` |
| Durum | `ResearchStateDto.Active` tezgâh kimliğiyle; replikasyon `base:research` |
| İstemci | Araştırma ekranı: üç tezgâh kartı + 13 dal sekmesi; kilit nedeni metinleri (`research.needs.dome`, `research.needs.lab`); `tr.txt` `research.tree.*` ve `research.node.*` adları bu bölümün `ad` alanlarıdır |
| Overseer | "Denge" sayfasında dal ölçekleri (S) ve teknoloji başına `min_lab` / `adim` düzenlenebilir |
| Kılavuz | Dal sayfaları `tools/rehber/uret.py` ile `veri/arastirma.json`'dan üretilir; bütçe tabloları kalibrasyon betiğinden gelir (§7.3) |

---

## 11. Esin kaynakları

- Türün iki laboratuvarlı araştırma yapısı (Last Asylum): ikinci tezgâhın ikinci kuyruk açması,
  ekonomi ve askeri araştırmanın ayrı kuyruklarda yürümesi, etkinlik, kervan, takım ve taktik
  bonuslarının kendi dallarına ayrılması.
- Rise of Kingdoms'un ekonomi ve askeri ağaç ayrımı: üretim, toplama, yük, inşa ve araştırma hızı bir
  tarafta; tür başına saldırı, savunma, can ve yürüyüş hızı öbür tarafta. Kademe açılışlarının bir bina
  seviyesine bağlanması ve geç seviyelerin aylara yayılması. Çapraz sunucu sezonunda ayrı ve geçici bir
  sezon ağacı.
- Hadal'ın farkı: üç tezgâhtan birinin esnek olması, kubbe seviyesinin Lab tavanı olarak çalışması,
  basınç sınıfını bant bant yükselten teknolojiler, dalların derin deniz bilimine bağlanması ve
  Süperalaşım kapılı bir Kadim dalı.

---

## 12. Açık sorular

- **Titanyum verimi.** [15 · Ekonomi Akışı](15_ekonomi_akisi.md)'nın verim çarpanı tablosu araştırmadan
  %10–20 Titanyum verimi bekliyor. Kodda olan Titanyum Geri Kazanımı tek başına
  <!--v:hesap.arastirma.titanium_recovery.10.etki|yuzde-->%40<!--/v--> verir (Lab 20'de tamam), Kobalt
  Kabuğu Kazıma <!--v:hesap.arastirma.ferromanganese_crusts.10.etki|yuzde-->%20<!--/v--> ekler. Ya 15'in
  çarpanı ya da düğümün kodda olan değeri değişmeli.
- **Basınç sınıfı.** [08](08_denizaltilar_basinc_seferler.md)'deki sınıf formülüne araştırmanın bant
  bonusu eklenmeli: sınıf = min(10, 1 + ⌊(İskele − 1) / 5⌋ + bant teknolojisi). F2P P10'a İskele 46
  yerine Reaktör 41 civarında ulaşır; [09](09_dunya_haritasi_ve_pvp.md)'daki basamak ve Kronik
  kapılarının buna dayanıklı olduğu doğrulanmalı.
- **Bütçenin hızı.** Bütçe yalnızca araştırma hızını sayar; tüm hız kaynaklarıyla savaş dalları
  hedeflerin 5–15 puan üstüne çıkıyor. Hedefler bu sütuna göre mi kalibre edilmeli?
- **İnşa hızı tavanı.** Tempo simülasyonu F2P inşa hızını %50'de keser; araştırma
  <!--v:hesap.arastirma.construction_mastery.10.etki|yuzde-->%30<!--/v--> + <!--v:hesap.arastirma.modular_assembly.10.etki|yuzde-->%10<!--/v-->,
  İzin 7 ve mürettebat eklenince bu sınır geç oyunda aşılır. Simülasyon tavanı yükseltilmeli mi?
- **Kahraman sistemine eklenenler.** Subay Deneyimi (Subay deneyim payı) ve Fraksiyon Sinerjisi
  (sinerji bonusu) [05](05_kahramanlar.md)'in değerlerine eklenir; 05'in yetenek ağacı puan bütçesine
  dokunulmadı.
- **Konvoy adı.** Konvoy dalı hem ticaret konvoylarını hem Leviathan Konvoyu etkinliğini (Tersane
  Rotasyonu) taşıyor. İki kavramın adı ayrışmalı mı?
- **Yağma koruması.** Ambar Kilitleri, ittifakın Ambar Mühürleri ve Basınç Ambarı üst üste biniyor;
  [09](09_dunya_haritasi_ve_pvp.md)'daki yağma dengesi için toplam korumaya bir tavan gerekir mi?
- **Bilim kayıtları.** SOFAR kanalı, derin saçılma katmanı, karşı aydınlatma, hidrofon dizileri, yankısız
  kaplama, kavitasyon, aerojel, kimyasal oksijen jeneratörü ve hiperbarik oksijen tedavisi
  `bilim.json`'da yok; bu teknolojilerin açıklamaları sayı vermeden yazıldı. [18](18_bilim_temeli.md)
  bu kayıtları eklemeli mi?
- **Karanlık Oksijen.** Bulgu tartışmalı. Dal deneysel kaldıkça etkisi küçük tutuldu; bulgu çürütülürse
  teknoloji adı ve etkisi korunup açıklaması "sınanan hipotez" olarak mı kalmalı?
