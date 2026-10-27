# 01 · Temel Kavramlar

> **Özet.** Hadal, okyanusun en derin çukurlarında geçen, sunucu otoriteli bir mobil strateji MMO'sudur.
> Oyuncu Hadal Zonu'nun girişinde (~6.000 m) bir basınç üssü kurar, kaynak üretir, kahraman ve asker
> toplar, denizaltılarla daha derine iner ve ittifakıyla birlikte çukurun dibindeki **Çekirdek**'e
> ulaşmaya çalışır. Bu bölüm oyunun bütününü bir sayfada anlatır ve kılavuzun nasıl okunacağını
> açıklar.

---

## 1. Dünya

Yıl 2249. Yüzey yaşanmaz hâle geldi; zehirli hava ve öldürücü güneş insanlığı aşağıya itti. Son
konvoylar okyanusun en derin çukurlarına sığındı: ışığın hiç ulaşmadığı, basıncın 600 barı aştığı,
suyun 1–2 °C olduğu Hadal Zonu. Burada yaşamı mümkün kılan şeyler, bugün de orada var olan şeylerdir:
hidrotermal bacaların ısısı, kemosentetik bakteriler, polimetalik nodüller, titanyum basınç
küreleri ([18 · Bilim Temeli](18_bilim_temeli.md)).

Ama çukurun dibinde başka bir şey de var. Milyonlarca yıllık biyomekanik bir uygarlığın kalıntıları,
yalnızca karanlıkta parıldayan **Hadalit** kristalleri ve onları koruyan **Kadim Muhafızlar**.
Çukurun en derin noktasında, insanlığın bir türlü anlamlandıramadığı bir yapı uyuyor: **Çekirdek**.
Hikâyenin son perdesi ([14 · Son Oyun](14_son_oyun.md)), onun gezegen büyüklüğünde uyuyan bir
canlının sinir sistemi olduğunu açığa çıkarır.

Her sunucu gerçek bir hadal çukurudur (Mariana, Tonga, Kermadec, Kuril-Kamçatka, Filipin,
Izu-Ogasawara, Porto Riko, Güney Sandwich, Atacama, Java). Harita kenardan merkeze o çukurun
kesitidir: dışarısı abisal düzlük, merkez çukurun dibi.

---

## 2. Çekirdek döngü

```
Üssü büyüt ─► kaynak üret ─► asker ve kahraman hazırla ─► denizaltıyla daha derine in
     ▲                                                              │
     └──── ödül, Süperalaşım, kalıntı, harita bölgesi ◄──────────────┘
               (ittifakla birlikte: bölge, ralli, sezon savaşı)
```

| Katman | Oyuncu ne yapar | Bölüm |
|---|---|---|
| Üs | Bina kurar ve yükseltir, binalar seviye seviye evrilir | [03](03_binalar.md), [04](04_bina_evrimi.md) |
| Ekonomi | Oksijen, Enerji, Gıda, Biyokütle, Titanyum üretir; geç oyunda Süperalaşım döker | [15](15_ekonomi_akisi.md) |
| Araştırma | İki laboratuvarda 13 dal | [06](06_arastirma.md) |
| Kahramanlar | Komuta ekibi (ekonomi), denizaltı kaptanı ve subayları (savaş) | [05](05_kahramanlar.md), [17](17_techizat.md) |
| Askerler | Dört tür, on kademe; yaralı ve ölü ayrımı | [07](07_birlikler_ve_savas.md) |
| Denizaltılar ve seferler | Basınç sınıfı, Derinlik Haritası kampanyası, sis | [08](08_denizaltilar_basinc_seferler.md) |
| Dünya haritası | Derinlik kuşakları, kaynak yatakları, PvE, PvP, kalkanlar | [09](09_dunya_haritasi_ve_pvp.md) |
| İttifak | Rütbeler, teknoloji, bölge, yardım, hediyeler | [10](10_ittifak.md) |
| Kronik ve köken | Sunucu zaman çizelgesi, oyuncu kökeni, profil | [16](16_kronik_kokenler_profil.md) |
| Etkinlikler ve sezonlar | Haftalık lig, ittifak savaşları, sezonlar arası savaş | [13](13_etkinlikler_ve_sezonlar.md) |
| VIP ve The Syndicate | Kalıcı VIP, aylık kart, sezon bileti, paketler | [11](11_vip.md), [12](12_syndicate.md) |

### Oturum yapısı

- **Kısa (2–5 dk):** üretimi topla, bir yükseltme başlat, kurtarma görevi gönder, yardım et.
- **Orta (10–20 dk):** araştırma sırasını kur, kahraman eğit, bir sefer aşaması oyna, toplama gönder.
- **Uzun (30+ dk):** ittifak rallisi, sezon savaşı, Leviathan avı, etkinlik.

Sunucu otoritelidir: inşaatlar ve araştırmalar çevrimdışıyken sunucu saatiyle biter; üretim 12
saate kadar birikir (kodda var). İstemci ekonomi, rastgelelik ya da çevrimdışı kazanç hesaplamaz.

---

## 3. Kaynaklar

<!-- URET:kaynak-tablosu -->
| Kaynak | Kimlik | Durum | Nereden gelir | Nereye gider | v8 başlangıç / tavan |
| --- | --- | --- | --- | --- | --- |
| Oksijen (O₂) | `Oxygen` | ✅ | Deniz suyu elektrolizi (Oksijen Tesisi), Mega Kubbe; geç dönemde nodül sahalarından karanlık oksijen. | Mürettebat ve asker bakımı, dalış (sefer) maliyeti, iyileştirme. | 600 / 6.000 |
| Enerji (EN) | `Energy` | ✅ | Çekirdek Reaktör (kompakt fisyon), Hidrotermal Santral (baca ısısı). | Bina bakımı, Takviye, Kalkan Jeneratörü. | 400 / 10.000 |
| Gıda (GD) | `Food` | ✅ | Kemosentetik bakteri kültürü (Gıda Laboratuvarı), LED hidroponik (Hidroponik Çiftlik, Tarım Kubbesi), deniz karı olayları. | Asker eğitimi ve bakımı, iyileştirme, kahraman eğitimi. | 400 / 5.000 |
| Biyokütle (BK) | `Biomass` | ✅ | Biyo Laboratuvar, Tarım Kubbesi, haritadaki balina leşleri. | İnşaat (ikincil), araştırma, hidroponik besin. | 200 / 6.000 |
| Titanyum (Ti) | `Titanium` | ✅ | Dron Fabrikası (hurda, ilmenit, demir-mangan kabuğu), kurtarma görevleri, haritadaki enkaz yatakları. | Her bina, araştırma, asker ve denizaltı. | 500 / 25.000 |
| Süperalaşım (SA) | `Alloy` | ⬜ | Alaşım Dökümhanesi, nodül sahası ve kobalt kabuğu toplama, üst düzey seferler, etkinlikler. | Reaktör 31–50 ve önkoşulları, T9–T10 askerler, Kadim Teknoloji araştırmaları, efsanevi teçhizat. | — |
| Hadalit (HD) | `Hadalite` | ✅ | The Syndicate, görevler, sandıklar, kurtarma görevleri, etkinlikler, VIP sandıkları. | Kriyo-Kapsül, hızlandırma, köken değişimi, Kadim Şema, kozmetik. | 100 / 1.000 |
<!-- /URET:kaynak-tablosu -->

İki kaynak **kritiktir**: Oksijen ve Gıda biterse mürettebat verimi düşer, askerler zayıflar. Enerji
açığı stoğu eritir. Süperalaşım ve Hadalit tavansızdır. Tavanlar ve Basınç Ambarı:
[02 · Tempo ve Denge](02_tempo_ve_denge.md#7-depolama-tavan-ve-yağma-koruması).

---

## 4. İlerlemenin omurgası: Çekirdek Reaktör

Her şey reaktöre bağlıdır. Hiçbir bina reaktörün seviyesini geçemez; halkalar, yeni binalar, kahraman
yuvaları, denizaltı hangarları reaktör seviyeleriyle açılır. Reaktörün her seviyesi bir önkoşul
zinciri ister. Ücretsiz oyuncu Reaktör 50'ye yaklaşık bir yılda ulaşır
(<!--v:hesap.sim.f2p.gun_50|gun-->368,1. gün<!--/v-->).

| Reaktör | Ne zaman (F2P) | Ne açılır (örnek) |
|---|---|---|
| 5 | <!--v:hesap.sim.f2p.seviye.5|gun-->4 dk 48 sn<!--/v--> | Askeri Kubbe, Kalkan Jeneratörü, Tatbikat Havuzu, 2. komuta yuvası |
| 10 | <!--v:hesap.sim.f2p.seviye.10|gun-->6 sa 51 dk<!--/v--> | 2. Denizaltı Hangarı, Takas İstasyonu, 3. komuta yuvası |
| 20 | <!--v:hesap.sim.f2p.seviye.20|gun-->4,5. gün<!--/v--> | 3. Basınç Ambarı, 10. halka, kubbe 2. seviyeleri |
| 30 | <!--v:hesap.sim.f2p.seviye.30|gun-->28,6. gün<!--/v--> | Alaşım Dökümhanesi, 12. halka, Süperalaşım dönemi |
| 40 | <!--v:hesap.sim.f2p.seviye.40|gun-->128,4. gün<!--/v--> | 2. Alaşım Dökümhanesi, kubbe 4. seviyeleri |
| 50 | <!--v:hesap.sim.f2p.seviye.50|gun-->368,1. gün<!--/v--> | Rezonans görünümü; Kadim Şema ile son adım |

---

## 5. Güç

Güç, oyuncunun gelişimini tek sayıda özetler: binalar, araştırma, askerler ve kahramanlar toplamı
(K-14). Sıralamalar, ittifak listesi, dünya haritası kartı ve eşleştirme gücü kullanır. Ayrıntı:
[02 · Tempo ve Denge](02_tempo_ve_denge.md#9-güç-k-14).

---

## 6. Tasarım ilkeleri

1. **Gerçek okyanus.** Derinlikler, basınç, sıcaklık, çukurlar, mineraller, doğa olayları ve canlılar
   gerçek derin deniz verisine dayanır; tek kaynak `veri/bilim.json`. Gerçek canlılar avlanmaz:
   taranır, kataloglanır, gözlenir (K-25). Hadalit ve kadim uygarlık hikâyenin kendi parçasıdır.
2. **Görünür büyüme.** Binalar her 10 seviyede biçim değiştirir; oyuncu üssünün büyüdüğünü görür.
3. **Sakin lüks (Quiet Luxury).** Ödeme ekranları bağırmaz: pop-up, geri sayım, "İNDİRİM" etiketi
   yoktur; teklifler yalnızca The Syndicate'te durur.
4. **Uzun soluk, kısa oturum.** Reaktör 50 bir yıl sürer ama her oturumda yapılacak anlamlı bir şey vardır.
5. **Adil rekabet.** Yeni oyuncu koruması, güç farkı cezaları, sezon sonu kayıp iadesi; ücretsiz
   oyuncu büyük savaşlardan dışlanmaz.
6. **Sunucu otoritesi.** Her sayı sunucuda doğrulanır; kılavuzdaki her formül
   `shared/HADAL.Shared` içinde tek bir yerde yaşar.
7. **Özgünlük.** Last Asylum ve Rise of Kingdoms'tan mekanik ve yapı alınır; isim, metin ve sayı
   alınmaz.

---

## 7. Kılavuz nasıl okunur?

- **Durum etiketleri:** ✅ kodda ve aynı · 🟡 kodda ama farklı ya da işlevsiz · ⬜ tasarım.
- **Mevcut (v8) ve Hedef:** kodda olan şeylerde bugünkü değer (`shared/catalog/catalog.json` sürüm 8)
  ile hedef değer yan yana verilir.
- **Ham süre:** tablolardaki süreler hız bonuslarından önceki değerlerdir.
- **Sayılar veriden gelir.** Bölüm metinlerindeki sayıların çoğu `veri/*.json`'dan üretilir;
  sayfayı elle düzeltmek yerine veriyi değiştirip `python tools/rehber/uret.py` çalıştırılır
  (`veri/SEMA.md`).
- **Kanon önce gelir.** Eski dökümanlarla çelişki olursa [00 · Kanon](00_kanon.md) geçerlidir.
- Terimler ve kimlikler: [Sözlük](sozluk.md).
