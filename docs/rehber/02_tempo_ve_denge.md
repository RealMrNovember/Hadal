# 02 · Tempo ve Denge

> **Özet.** Ücretsiz oynayan düzenli bir oyuncu Çekirdek Reaktör'ü 50. seviyeye yaklaşık **bir yılda**
> çıkarır (simülasyon: <!--v:hesap.sim.f2p.gun_50|gun-->368,1. gün<!--/v-->). Aylık kart alan oyuncu
> <!--v:hesap.sim.aylik_kart.gun_50|gun-->267,2. gün<!--/v-->, yüksek harcamalı oyuncu
> <!--v:hesap.sim.balina.gun_50|gun-->147,1. gün<!--/v--> ulaşır. Bugünkü oyunda aynı yol yalnızca
> <!--v:hesap.sayilar.v8_reaktor_ham_saat-->20<!--/v--> saat sürüyor. Bu bölüm hedef tempoyu,
> onu üreten eğrileri, maliyet ve süre formüllerini, depolama tavanlarını, gücü ve tempo
> simülasyonunu anlatır. Bütün sayılar `veri/` dosyalarından üretilir.

Durum: 🟡. Kodda seviye, maliyet ve süre sistemi çalışıyor; değerler ve bazı formüller bu
bölümdeki hedeften farklı.

---

## 1. Neden bir yıl?

Bu türün oyuncusu bir oyunda aylarca kalır. Last Asylum ve Rise of Kingdoms'ta ana binanın son
seviyeleri günler, haftalar sürer; Rise of Kingdoms'ta merkez binayı yalnızca kendi süreleriyle son
seviyeye çıkarmak yaklaşık 257 gün tutar, önkoşul binaları bunun üstüne eklenir. Hadal'da bugün her
şey bir günde biter: oyuncu ilk haftasında anlatacak bir şey bulamaz.

Hedef tempo üç ilkeye dayanır:

1. **İlk saat hızlıdır.** İlk 10 reaktör seviyesi dakikalar içinde gelir; oyuncu sistemleri hızla tanır.
2. **İlk ay ritim kurar.** Reaktör 20 ilk haftada, Reaktör 30 ilk ayda gelir. Her gün en az bir "seviye
   atladım" anı vardır.
3. **Geç oyun sabır ve plan ister.** Reaktör 30'dan sonra her seviye günler sürer ve
   **Süperalaşım** kapı kaynağı olur. Oyuncu artık yalnızca beklemez: araştırma, kahraman, ittifak
   ve harita bu dönemin asıl içeriğidir.

### Hedef kilometre taşları (F2P)

<!-- URET:kilometre-taslari -->
| Reaktör | Hedef | Simülasyon | Fark |
| ---: | ---: | ---: | ---: |
| 15 | 1,5. gün | 1,4. gün | −8% |
| 20 | 4,5. gün | 4,5. gün | 0% |
| 25 | 13. gün | 13,4. gün | 3% |
| 30 | 29. gün | 28,6. gün | −1% |
| 35 | 70. gün | 68,7. gün | −2% |
| 40 | 128. gün | 128,4. gün | 0% |
| 45 | 225. gün | 224,9. gün | 0% |
| 50 | 365. gün | 368,1. gün | 1% |
<!-- /URET:kilometre-taslari -->

### Üç profil

<!-- URET:sim-ozet -->
| Profil | Reaktör 50 | Hedef (aralık) | R10 | R20 | R30 | R40 | R45 | Süperalaşım beklemesi |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Ücretsiz oyuncu | **368. gün** | 365 (340–390) | 6 sa 51 dk | 4,5. gün | 28,6. gün | 128,4. gün | 224,9. gün | 21,2. gün |
| Aylık kart + VIP 8 | **267. gün** | 265 (235–295) | 3 sa 59 dk | 2,7. gün | 19,9. gün | 84,5. gün | 156,6. gün | — |
| Balina | **147. gün** | 165 (140–190) | 1 sa 19 dk | 1,1. gün | 10. gün | 45,9. gün | 85,8. gün | — |
<!-- /URET:sim-ozet -->

Ayrıntılı seviye seviye döküm: [Tempo simülasyonu](uretilen/simulasyon.md).

---

## 2. Eğri sistemi

Her maliyet, süre, üretim ve güç değeri **adlandırılmış bir eğriden** gelir. Eğri, birkaç seviyedeki
"çapa" değerleriyle tanımlanır; aradaki seviyeler **log-doğrusal** (geometrik) enterpolasyonla
bulunur. Böylece tasarımcı 50 satırı tek tek elle ayarlamaz, 10–15 çapayı değiştirir; eğri hiçbir
zaman zikzak yapmaz.

- Çapa `[a, va]` ile `[b, vb]` arasındaki seviye `x` için: `değer = va × (vb / va) ^ ((x − a) / (b − a))`.
- Çapaların dışında ilk ya da son aralığın büyüme hızı sürer.
- Bir aralıkta 0 varsa o aralık doğrusal okunur.

<!-- URET:egriler -->
| Eğri | Ad | Birim | Çapalar (Sv: değer) |
| --- | --- | --- | --- |
| `reaktor_sure` | Reaktör inşa süresi | saniye | 1: 3 · 2: 5 · 3: 15 · 4: 30 · 5: 60 · 6: 120 · 7: 240 · 8: 480 · 9: 900 · 10: 1.800 · 12: 4.800 · 15: 14.400 · 20: 50.400 · 25: 172.800 · 30: 388.800 · 35: 820.800 · 40: 1.468.800 · 45: 2.419.200 · 50: 3.628.800 |
| `reaktor_bedel` | Reaktör Titanyum maliyeti | Titanyum | 1: 60 · 2: 100 · 3: 180 · 4: 350 · 5: 700 · 10: 5.000 · 15: 30.000 · 20: 130.000 · 25: 420.000 · 30: 1.100.000 · 35: 2.500.000 · 40: 5.000.000 · 45: 10.000.000 · 50: 20.000.000 |
| `alasim` | Reaktör Süperalaşım maliyeti | Süperalaşım | 31: 100 · 35: 170 · 40: 330 · 45: 600 · 50: 1.050 |
| `uretim` | Üretim çarpanı | × Sv 1 değeri | 1: 1 · 10: 6 · 20: 21 · 30: 48 · 40: 102 · 50: 180 |
| `alasim_uretim` | Süperalaşım üretim çarpanı | × Sv 1 değeri | 1: 1 · 30: 1 · 40: 2,5 · 50: 5 |
| `guc` | Güç artışı | güç | 1: 10 · 10: 120 · 20: 500 · 30: 1.400 · 40: 3.000 · 50: 5.500 |
| `depo` | Basınç Ambarı kapasitesi | × reaktör Titanyum maliyeti | 1: 2.000 · 10: 15.000 · 20: 300.000 · 30: 2.400.000 · 40: 10.000.000 · 50: 40.000.000 |
<!-- /URET:egriler -->

### Yuvarlama

Oyuncunun gördüğü sayılar "temiz" olmalı:

| Değer | Kural |
|---|---|
| Maliyet < 20 | Tam sayı |
| Maliyet < 1.000 | 5'in katı |
| Maliyet ≥ 1.000 | 3 anlamlı basamak (1.040, 164.000, 2.870.000) |
| Süre < 2 dk | Saniye |
| Süre < 2 sa | Dakika |
| Süre < 1 g | 5 dakika |
| Süre ≥ 1 g | 15 dakika |

Kod tarafında (catalog v9) eğrinin kendisi değil, **hesaplanmış tam sayı dizileri** saklanır. Böylece
sunucu (.NET) ile istemci (Unity/Mono) arasında kayan nokta farkı oluşmaz.

---

## 3. Bina maliyeti ve süresi

Her bina, reaktör eğrilerine göre tanımlanır:

- **Maliyet satırı** = `ölçek × eğri(eşdeğer seviye)`; eğri varsayılan olarak `reaktor_bedel`
  (Titanyum) eğrisidir. Satırın bir `başlangıç` seviyesi olabilir: örneğin reaktörün Biyokütle
  maliyeti 4. seviyede, Süperalaşım maliyeti 31. seviyede başlar.
- **Süre** = `süre ölçeği × reaktor_sure(eşdeğer seviye)`.
- **Eşdeğer seviye** normalde binanın kendi seviyesidir. Kubbelerde (5 seviye) her seviye bir
  reaktör kapısına eşlenir: Mega Kubbe 1–5 → Reaktör 6 / 20 / 30 / 40 / 45.
- **Reaktör tavanı:** reaktör dışındaki hiçbir bina reaktörün seviyesini geçemez (kodda var).

Ölçekler bina sayfalarında ve `veri/binalar.json`'da yazar. Örnek: Oksijen Tesisi maliyette
reaktörün %12'si, sürede %15'i; Kalkan Jeneratörü %40 / %45.

### Çekirdek Reaktör çapaları

<!-- URET:reaktor-capalari -->
| Sv | Ham süre | Titanyum | Biyokütle | Süperalaşım | Reaktör gücü (kümülatif) | F2P'nin ulaştığı an |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 2 | 5 sn | 100 | — | — | 116 | — |
| 5 | 60 sn | 700 | 245 | — | 468 | 4 dk 48 sn |
| 10 | 30 dk | 5.000 | 1.750 | — | 2.329 | 6 sa 51 dk |
| 15 | 4 sa | 30.000 | 10.500 | — | 7.027 | 1,4. gün |
| 20 | 14 sa | 130.000 | 45.500 | — | 16.616 | 4,5. gün |
| 25 | 2 g | 420.000 | 147.000 | — | 33.821 | 13,4. gün |
| 30 | 4 g 12 sa | 1.100.000 | 385.000 | — | 62.610 | 28,6. gün |
| 35 | 9 g 12 sa | 2.500.000 | 875.000 | 170 | 106.857 | 68,7. gün |
| 40 | 17 g | 5.000.000 | 1.750.000 | 330 | 171.627 | 128,4. gün |
| 45 | 28 g | 10.000.000 | 3.500.000 | 600 | 261.914 | 224,9. gün |
| 50 | 42 g | 20.000.000 | 7.000.000 | 1.050 | 384.164 | 368,1. gün |
<!-- /URET:reaktor-capalari -->

Reaktörün 1 → 50 toplam ham süresi <!--v:hesap.bina.core_reactor.toplam_sure|sure-->425 g 9 sa 19 dk<!--/v-->,
toplam Titanyum maliyeti <!--v:hesap.bina.core_reactor.toplam_bedel.Titanium-->150.249.010<!--/v-->.
Ham süre, hız bonusları uygulanmadan önceki değerdir (bkz. §5). Tam tablo:
[Çekirdek Reaktör](binalar/core_reactor.md).

### Reaktör önkoşul zinciri

Last Asylum'daki "Sığınak'ı yükseltmek için şu binalar şu seviyede olmalı" ve Rise of Kingdoms'taki
"merkez bina + duvar + bir başka bina" kuralının Hadal karşılığı:

- Reaktör 3–6: sabit önkoşullar (Oksijen Tesisi, Dron Fabrikası, Araştırma Merkezi, Gıda Laboratuvarı).
- Reaktör 7'den itibaren her seviye: **Kalkan Jeneratörü L−1** + **dönüşümlü bir bina L−1**.
  Dönüşüm sırası Araştırma Merkezi → Askeri Kubbe → Denizaltı İskelesi → Tatbikat Havuzu → Revir →
  İttifak Merkezi. Her bina altı seviyede bir sıraya girer; arada yetişmesi gerekir.
- Kubbe kilometre taşları: Reaktör 21, 22, 31, 32, 41, 42 ve 46 bir kubbe seviyesi ister.

Bir önkoşul binası reaktörü geçemediği için, her önkoşulun **son adımı** ancak reaktör L−1
olduktan sonra atılabilir. Bu, "reaktör inşa edilirken ikinci inşaatçı ne yapar?" sorusuna doğal
bir cevap verir: bir sonraki önkoşulu yetiştirir.

<details><summary>Seviye seviye önkoşul tablosu</summary>

<!-- URET:reaktor-on-kosul -->
| Reaktör Sv | Gerekenler |
| ---: | --- |
| 3 | [Oksijen Tesisi](./binalar/oxygen_plant.md) 2 |
| 4 | [Dron Fabrikası](./binalar/drone_factory.md) 3 |
| 5 | [Araştırma Merkezi](./binalar/research_center.md) 4 |
| 6 | [Gıda Laboratuvarı](./binalar/food_lab.md) 5 |
| 7 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 6, [Araştırma Merkezi](./binalar/research_center.md) 6 |
| 8 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 7, [Askeri Kubbe](./binalar/military_dome.md) 7 |
| 9 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 8, [Denizaltı İskelesi](./binalar/submarine_dock.md) 8 |
| 10 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 9, [Tatbikat Havuzu](./binalar/training_grounds.md) 9 |
| 11 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 10, [Revir](./binalar/med_bay.md) 10 |
| 12 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 11, [İttifak Merkezi](./binalar/alliance_hub.md) 11 |
| 13 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 12, [Araştırma Merkezi](./binalar/research_center.md) 12 |
| 14 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 13, [Askeri Kubbe](./binalar/military_dome.md) 13 |
| 15 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 14, [Denizaltı İskelesi](./binalar/submarine_dock.md) 14 |
| 16 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 15, [Tatbikat Havuzu](./binalar/training_grounds.md) 15 |
| 17 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 16, [Revir](./binalar/med_bay.md) 16 |
| 18 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 17, [İttifak Merkezi](./binalar/alliance_hub.md) 17 |
| 19 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 18, [Araştırma Merkezi](./binalar/research_center.md) 18 |
| 20 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 19, [Askeri Kubbe](./binalar/military_dome.md) 19 |
| 21 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 20, [Denizaltı İskelesi](./binalar/submarine_dock.md) 20, [Mega Kubbe](./binalar/mega_dome.md) 2 |
| 22 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 21, [Tatbikat Havuzu](./binalar/training_grounds.md) 21, [Tarım Kubbesi](./binalar/agriculture_dome.md) 2 |
| 23 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 22, [Revir](./binalar/med_bay.md) 22 |
| 24 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 23, [İttifak Merkezi](./binalar/alliance_hub.md) 23 |
| 25 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 24, [Araştırma Merkezi](./binalar/research_center.md) 24 |
| 26 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 25, [Askeri Kubbe](./binalar/military_dome.md) 25 |
| 27 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 26, [Denizaltı İskelesi](./binalar/submarine_dock.md) 26 |
| 28 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 27, [Tatbikat Havuzu](./binalar/training_grounds.md) 27 |
| 29 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 28, [Revir](./binalar/med_bay.md) 28 |
| 30 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 29, [İttifak Merkezi](./binalar/alliance_hub.md) 29 |
| 31 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 30, [Araştırma Merkezi](./binalar/research_center.md) 30, [Mega Kubbe](./binalar/mega_dome.md) 3 |
| 32 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 31, [Askeri Kubbe](./binalar/military_dome.md) 31, [Bilim Kubbesi](./binalar/science_dome.md) 3 |
| 33 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 32, [Denizaltı İskelesi](./binalar/submarine_dock.md) 32 |
| 34 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 33, [Tatbikat Havuzu](./binalar/training_grounds.md) 33 |
| 35 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 34, [Revir](./binalar/med_bay.md) 34 |
| 36 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 35, [İttifak Merkezi](./binalar/alliance_hub.md) 35 |
| 37 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 36, [Araştırma Merkezi](./binalar/research_center.md) 36 |
| 38 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 37, [Askeri Kubbe](./binalar/military_dome.md) 37 |
| 39 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 38, [Denizaltı İskelesi](./binalar/submarine_dock.md) 38 |
| 40 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 39, [Tatbikat Havuzu](./binalar/training_grounds.md) 39 |
| 41 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 40, [Revir](./binalar/med_bay.md) 40, [Mega Kubbe](./binalar/mega_dome.md) 4 |
| 42 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 41, [İttifak Merkezi](./binalar/alliance_hub.md) 41, [Savaş Kubbesi](./binalar/war_dome.md) 4 |
| 43 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 42, [Araştırma Merkezi](./binalar/research_center.md) 42 |
| 44 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 43, [Askeri Kubbe](./binalar/military_dome.md) 43 |
| 45 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 44, [Denizaltı İskelesi](./binalar/submarine_dock.md) 44 |
| 46 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 45, [Tatbikat Havuzu](./binalar/training_grounds.md) 45, [Mega Kubbe](./binalar/mega_dome.md) 5 |
| 47 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 46, [Revir](./binalar/med_bay.md) 46 |
| 48 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 47, [İttifak Merkezi](./binalar/alliance_hub.md) 47 |
| 49 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 48, [Araştırma Merkezi](./binalar/research_center.md) 48 |
| 50 | [Kalkan Jeneratörü](./binalar/shield_generator.md) 49, [Askeri Kubbe](./binalar/military_dome.md) 49 |
<!-- /URET:reaktor-on-kosul -->

</details>

---

## 4. Süperalaşım: geç oyunun kapısı

Reaktör 31'den itibaren reaktör, Kalkan Jeneratörü, dönüşümlü önkoşullar ve kubbeler
**Süperalaşım** ister. Süperalaşım, polimetalik nodüllerin nikel ve kobaltından Alaşım
Dökümhanesi'nde dökülür ([18 · Bilim Temeli](18_bilim_temeli.md)); haritadaki nodül sahaları, üst
düzey seferler ve etkinlikler de verir. Süperalaşım tavana takılmaz, birikir.

Neden ayrı bir kapı kaynağı?

- Titanyum ve Biyokütle geç oyunda bol akar; tek başına bekleme yaratmaz.
- Süperalaşım günlük gelirle sınırlıdır. Oyuncu "bu hafta reaktöre mi, askere mi, araştırmaya mı?"
  diye seçmek zorunda kalır.
- Ödeme yapan oyuncuya hız verir ama sınırsız değildir: Süperalaşım paketleri haftalık limitlidir
  ([12 · The Syndicate](12_syndicate.md)).

Simülasyonda F2P oyuncu toplam <!--v:hesap.sim.f2p.toplam.bekleme_alasim|gun-->21,2. gün<!--/v-->
Süperalaşım bekler; bu beklemenin büyük kısmı Reaktör 31–40 arasındadır.

---

## 5. Hız, yardım, hızlandırma

### Hız formülü (K-13)

```
etkin süre = ham süre / (1 + Σ hız% / 100)
```

Hız bonusları **toplanır, sonra bölen olarak** uygulanır: araştırma (İnşaat Ustalığı ve devamı),
komuta ekibindeki kahraman becerileri, VIP, ittifak teknolojisi, Egemen unvanları. Bölen formülü
hiçbir zaman sıfıra inmez; %100 hız süreyi yarıya, %300 hız dörtte bire indirir.

Bugünkü kod yüzdeleri süreden çıkarıp %10'da durduruyor (`ResearchRules.ApplyPercent`). Bonuslar
üst üste bindiğinde bu yöntem ya çok erken tavana çarpar ya da süreyi neredeyse sıfıra indirir.
Bölen formülüne geçiş catalog v9 ile birlikte yapılır.

### İttifak yardımı

Her yardım toplam sürenin %1'ini, en az 60 saniyeyi düşürür (kodda var). Yardım sınırı İttifak
Merkezi'yle büyür: 10 + ⌊seviye / 5⌋, en çok 20.

### Ücretsiz bitirme

Son 5 dakikası kalan inşaat ve araştırma ücretsiz biter (kodda var). Mühendislik Bölmesi her 5
seviyede +1 dakika ekler; 50. seviyede 15 dakika olur.

### Hızlandırma

Hızlandırma eşyaları genel ve türe özel (inşaat, araştırma, eğitim, iyileştirme) olarak gelir.
Kaynakları, süreleri ve Hadalit karşılığı: [12 · The Syndicate](12_syndicate.md). Simülasyon,
F2P oyuncunun günde 0,5 saatten (erken) 2 saate (Reaktör 16+) kadar hızlandırma kazandığını ve
hepsini reaktöre harcadığını varsayar.

---

## 6. Tempo simülasyonu

Simülasyon (`tools/rehber/rehber/sim.py`) reaktörün **kritik yolunu** inşaatçı inşaatçı oynatır:

1. Reaktör L için önkoşul binaları istenen seviyeye çıkarılır. Alt seviyedeki "yetişme" adımları,
   reaktör inşa edilirken boşta kalan inşaatçıların biriktirdiği kapasiteden karşılanır; son adımlar
   reaktör L−1 olduktan sonra yapılır.
2. Kaynak kontrol edilir: gelirin kritik yola ayrılan payı yetmezse beklenir. İlk seviyelerde hikâye
   görevleri reaktör maliyetinin bir kısmını karşılar.
3. Süperalaşım kontrol edilir (Reaktör 31+).
4. Reaktör inşa edilir: hız formülü, ittifak yardımı, ücretsiz bitirme ve hızlandırma stoğu uygulanır.
5. Uzun inşaatlar oyuncu çevrimdışıyken biterse inşaatçı bir süre boşta bekler.

### Profil varsayımları

<!-- URET:profil-ozet -->
| Profil | İnşaatçı | Hız bonusu | Yardım kullanımı | Hızlandırma (günlük) | Gelir | Süperalaşım | Kritik yola ayrılan | Boşta bekleme |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Ücretsiz oyuncu | R1: 1 → R5: 2 | min(%50, %1,5 × Sv) | %80 | R1: 0,5 sa → R15: 1 sa → R16: 2 sa → R50: 2 sa | ×1 | ×1 | %35 | %30, en çok 5 sa |
| Aylık kart + VIP 8 | R1: 2 → R5: 3 | min(%50, %1,5 × Sv) + %15 | %90 | R1: 2 sa → R15: 4 sa → R16: 6 sa → R50: 6 sa | ×1,5 | ×2 | %40 | %25, en çok 4 sa |
| Balina | R1: 3 | min(%50, %1,5 × Sv) + %30 | %100 | R1: 6 sa → R15: 14 sa → R16: 24 sa → R50: 24 sa | ×4 | ×5 | %45 | %15, en çok 2 sa |
<!-- /URET:profil-ozet -->

### Gelir modeli

F2P oyuncunun saatlik geliri (binalar yaklaşık %45, toplama / kurtarma / sefer %35, görev ve
etkinlik %20). Ayrıntılı kaynak dökümü: [15 · Ekonomi Akışı](15_ekonomi_akisi.md).

<!-- URET:gelir-tablosu -->
| Reaktör | Titanyum/sa | Biyokütle/sa | Titanyum/gün | Hikâye görevlerinin karşıladığı reaktör maliyeti |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 1.200 | 480 | 28.800 | — |
| 5 | 3.500 | 1.400 | 84.000 | %73 |
| 10 | 9.000 | 3.600 | 216.000 | %64 |
| 15 | 20.000 | 8.000 | 480.000 | %43 |
| 20 | 38.000 | 15.200 | 912.000 | %25 |
| 25 | 62.000 | 24.800 | 1.488.000 | — |
| 30 | 90.000 | 36.000 | 2.160.000 | — |
| 35 | 125.000 | 50.000 | 3.000.000 | — |
| 40 | 170.000 | 68.000 | 4.080.000 | — |
| 45 | 230.000 | 92.000 | 5.520.000 | — |
| 50 | 300.000 | 120.000 | 7.200.000 | — |

Süperalaşım geliri (Reaktör 30'a ulaşıldıktan sonraki gün sayısına göre, F2P):

| R30'dan sonra | Süperalaşım/gün |
| ---: | ---: |
| 0. gün | 40 |
| 30. gün | 52 |
| 90. gün | 90 |
| 180. gün | 140 |
| 270. gün | 181 |
| 337. gün | 220 |
<!-- /URET:gelir-tablosu -->

### Kalibrasyon nasıl değiştirilir?

1. `veri/egriler.json`'daki çapaları ya da `veri/f2p_profilleri.json`'daki varsayımları değiştir.
2. `python tools/rehber/uret.py --sim` ile üç profilin sonucuna bak.
3. `python tools/rehber/uret.py` tüm tabloları, sayfaları ve bu bölümü yeniden üretir; kontroller
   hedef aralığın dışına çıkılırsa hata verir.

---

## 7. Depolama, tavan ve yağma koruması

Bugün kaynak tavanları sabittir (Titanyum 25.000) ve tavanı aşan ödül kaybolur. Hedef tasarımda
maliyetler milyonlara çıktığı için tavan reaktörle büyür:

```
tavan(kaynak, R) = max(v8 tavanı, çarpan(kaynak) × reaktör(R+1) Titanyum maliyeti)
```

Çarpanlar: Titanyum 1,5 · Gıda 0,6 · Biyokütle 0,6 · Oksijen 0,5 · Enerji 0,4. Süperalaşım ve
Hadalit tavansızdır. Kontrol: hiçbir bina, araştırma ya da halka açılışı, açıldığı reaktör
seviyesindeki tavandan pahalı değildir.

<!-- URET:tavanlar -->
| Reaktör | Titanyum | Biyokütle | Gıda | Oksijen | Enerji |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 25.000 | 6.000 | 5.000 | 6.000 | 10.000 |
| 5 | 25.000 | 6.000 | 5.000 | 6.000 | 10.000 |
| 10 | 25.000 | 6.000 | 5.000 | 6.000 | 10.000 |
| 15 | 60.300 | 24.100 | 24.100 | 20.100 | 16.100 |
| 20 | 246.000 | 98.400 | 98.400 | 82.000 | 65.600 |
| 25 | 764.000 | 305.000 | 305.000 | 254.000 | 204.000 |
| 30 | 1.950.000 | 780.000 | 780.000 | 650.000 | 520.000 |
| 35 | 4.300.000 | 1.720.000 | 1.720.000 | 1.440.000 | 1.150.000 |
| 40 | 8.610.000 | 3.440.000 | 3.440.000 | 2.870.000 | 2.300.000 |
| 45 | 17.200.000 | 6.900.000 | 6.900.000 | 5.750.000 | 4.600.000 |
| 49 | 30.000.000 | 12.000.000 | 12.000.000 | 10.000.000 | 8.000.000 |
<!-- /URET:tavanlar -->

- **Basınç Ambarı** (3 adet) tavanın üstüne ek kapasite verir ve stoğun bir kısmını yağmadan korur
  ([Basınç Ambarı](binalar/pressure_vault.md)).
- **Kaynak Kapsülü:** görev, sandık, etkinlik ve sefer ödülleri tavana takılmaz; çantaya kapsül
  olarak düşer ve oyuncu istediğinde açar. "Ödül tavanda kayboldu" hissi ortadan kalkar.
- Çevrimdışı birikim 12 saatle sınırlıdır (kodda var).

---

## 8. Enerji dengesi

Her üretim binası Enerji tüketir; Enerji'yi Çekirdek Reaktör ve Hidrotermal Santral üretir.
Kontrol, "açık her bina o reaktör seviyesinde" olan bir referans üste Enerji dengesinin hiçbir
reaktör seviyesinde eksiye düşmediğini doğrular. Askerler, Takviye ve Kalkan Jeneratörü maliyeti
gibi ek harcamalar bu artıdan karşılanır.

<!-- URET:enerji-dengesi -->
| Reaktör | Üretim/sa | Tüketim/sa | Net/sa |
| ---: | ---: | ---: | ---: |
| 1 | 700 | 90 | 610 |
| 5 | 1.552 | 1.153 | 399 |
| 8 | 4.836 | 3.120 | 1.716 |
| 10 | 7.200 | 4.590 | 2.610 |
| 15 | 19.081 | 9.238 | 9.843 |
| 20 | 46.200 | 21.945 | 24.255 |
| 25 | 69.849 | 29.732 | 40.117 |
| 30 | 105.600 | 54.960 | 50.640 |
| 35 | 153.938 | 73.085 | 80.853 |
| 40 | 224.400 | 126.990 | 97.410 |
| 45 | 298.099 | 168.696 | 129.403 |
| 50 | 396.000 | 209.860 | 186.140 |
<!-- /URET:enerji-dengesi -->

---

## 9. Güç (K-14)

Güç, oyuncunun gelişimini tek sayıda gösterir: sıralamalar, eşleştirme, dünya haritası kartı ve
ittifak listesi bunu kullanır. Hedef formül:

```
Toplam güç = Σ bina gücü + Σ araştırma gücü + Σ asker gücü + Σ kahraman gücü
bina güç artışı(Sv) = ağırlık × güç(eşdeğer Sv)
araştırma güç artışı = 0,4 × güç(gerekli lab seviyesi)
kahraman gücü = (ATK + DEF + HP / 10) × 10
```

Asker birim gücü: [07 · Birlikler ve Savaş](07_birlikler_ve_savas.md). Bugün kodda güç
`Σ bina seviyesi × 10 + Σ kahraman gücü`.

<!-- URET:guc-tablosu -->
| Sv | güç(Sv) |
| ---: | ---: |
| 1 | 10 |
| 5 | 30 |
| 10 | 120 |
| 15 | 245 |
| 20 | 500 |
| 25 | 837 |
| 30 | 1.400 |
| 35 | 2.049 |
| 40 | 3.000 |
| 45 | 4.062 |
| 50 | 5.500 |

| Ağırlık | Binalar |
| ---: | --- |
| 6 | Mega Kubbe, Tarım Kubbesi, Bilim Kubbesi, Savaş Kubbesi |
| 5 | Çekirdek Reaktör |
| 1,5 | Araştırma Merkezi, Denizaltı İskelesi, Radar Merkezi, Askeri Kubbe, Kalkan Jeneratörü, Tatbikat Havuzu |
| 1 | İttifak Merkezi, Kriyo Salonu, Basınç Ambarı, Mühendislik Bölmesi, Denizaltı Hangarı, Revir, Kriyo Bakım Ünitesi, Takas İstasyonu, Teçhizat Atölyesi, Dalgıç Anıtı, Zıpkın Anıtı, Ark Anıtı, Kalıntı Arşivi, Alaşım Dökümhanesi, Kronik Feneri |
| 0,6 | Oksijen Tesisi, Gıda Laboratuvarı, Biyo Laboratuvar, Hidroponik Çiftlik, Dron Fabrikası, Mürettebat Yatakhanesi, Hidrotermal Santral |
| 0,2 | Süs Kulesi |

Referans üs (açık her bina o reaktör seviyesinde) bina gücü:

| Reaktör | Bina gücü |
| ---: | ---: |
| 5 | 2.390 |
| 10 | 17.312 |
| 20 | 176.721 |
| 30 | 682.445 |
| 40 | 1.892.081 |
| 50 | 4.182.944 |
<!-- /URET:guc-tablosu -->

---

## 10. Uzun kuyruk: her şeyi maksimuma çıkarmak

Reaktör 50 bir yıl sürer, ama **her binanın her örneğini** 50'ye çıkarmak çok daha uzun sürer:
toplam <!--v:hesap.sayilar.tum_binalar_ham_gun-->4.816<!--/v--> ham inşaatçı-günü. Bu bilinçli bir
tercihtir: türün oyuncusu "maksimum üs"e değil, kendi stratejisine göre önceliklendirdiği bir üsse
sahip olur. Ekonomi odaklı bir oyuncu üretim binalarını, savaşçı oyuncu askeri binaları öne alır.
F2P oyuncunun ikinci inşaatçısı bir yıl boyunca kritik yol dışında yaklaşık
<!--v:hesap.sim.f2p.bos_insaatci_gun|tam-->105<!--/v--> gün çalışabilir; o zaman üretim, depo ve askeri
binalara gider. Her şeyi maksimuma çıkarmanın bunun onlarca katı sürmesi bu yüzdendir ([Tempo simülasyonu](uretilen/simulasyon.md)).

<!-- URET:insaatci-butcesi -->
| Kategori | Ham inşaatçı-günü (her örnek 1 → maks) | Pay |
| --- | ---: | ---: |
| Üretim | 1.884 | %39 |
| Askeri | 957 | %20 |
| Destek | 672 | %14 |
| Merkez | 425 | %9 |
| Depolama | 255 | %5 |
| Kubbe | 201 | %4 |
| Anıt | 191 | %4 |
| Süs | 128 | %3 |
| Araştırma | 51 | %1 |
| İttifak | 51 | %1 |
| **Toplam** | **4.816** | %100 |
<!-- /URET:insaatci-butcesi -->

---

## 11. Bugünkü oyunla karşılaştırma

<!-- URET:v8-karsilastirma -->
| Bina | v8 son seviye Ti / süre | Hedef son seviye Ti / süre | v8 toplam süre | Hedef toplam süre |
| --- | --- | --- | --- | --- |
| Çekirdek Reaktör | 6.822 / 2 sa 9 dk | 20.000.000 / 42 g | 20 sa | 425 g 9 sa 19 dk |
| Oksijen Tesisi | 4.093 / 43 dk | 2.400.000 / 6 g 7 sa 15 dk | 6 sa 40 dk | 63 g 19 sa 24 dk |
| Gıda Laboratuvarı | 3.411 / 43 dk | 2.000.000 / 6 g 7 sa 15 dk | 6 sa 40 dk | 63 g 19 sa 24 dk |
| Biyo Laboratuvar | 4.775 / 1 sa 4 dk | 2.400.000 / 6 g 7 sa 15 dk | 10 sa | 63 g 19 sa 24 dk |
| Hidroponik Çiftlik | 9.551 / 2 sa 52 dk | 3.000.000 / 7 g 13 sa 30 dk | 1 g 2 sa 40 dk | 76 g 13 sa 44 dk |
| Dron Fabrikası | 8.186 / 2 sa 9 dk | 3.000.000 / 7 g 13 sa 30 dk | 20 sa | 76 g 13 sa 44 dk |
| Araştırma Merkezi | 12.279 / 4 sa 18 dk | 5.000.000 / 5 g 1 sa | 1 g 15 sa 59 dk | 51 g 46 dk |
| Denizaltı İskelesi | 17.737 / 6 sa 27 dk | 5.000.000 / 5 g 1 sa | 2 g 12 sa | 51 g 46 dk |
| Radar Merkezi | 10.915 / 3 sa 13 dk | 4.000.000 / 8 g 9 sa 30 dk | 1 g 6 sa | 85 g 1 sa 45 dk |
| Askeri Kubbe | 16.372 / 4 sa 18 dk | 5.000.000 / 5 g 1 sa | 1 g 15 sa 59 dk | 51 g 46 dk |
| Kalkan Jeneratörü | 15.008 / 4 sa 18 dk | 8.000.000 / 18 g 21 sa 30 dk | 1 g 15 sa 59 dk | 191 g 10 sa 36 dk |
| Mega Kubbe | 1.270 / 7 dk 52 sn | 12.000.000 / 28 g | 31 dk 45 sn | 50 g 2 sa 2 dk |
| Tarım Kubbesi | 988 / 6 dk 18 sn | 10.000.000 / 28 g | 25 dk 25 sn | 50 g 2 sa 4 dk |
| İttifak Merkezi | 20.465 / 8 sa 36 dk | 5.000.000 / 5 g 1 sa | 3 g 8 sa | 51 g 46 dk |
| Süs Kulesi | 2.729 / 43 dk | 600.000 / 2 g 2 sa 30 dk | 6 sa 40 dk | 21 g 6 sa 39 dk |
<!-- /URET:v8-karsilastirma -->

---

## 12. Uygulama notları

| Değişiklik | Kod |
|---|---|
| Seviye başına maliyet, süre ve güç dizileri | `shared/catalog/catalog.json` → `buildings[].levels[]` (v9); `shared/HADAL.Shared/Catalog/GameCatalogData.cs`, `GameCatalog.cs` doğrulaması |
| Maliyet ve süre okuma | `shared/HADAL.Shared/Simulation/BuildingProgression.cs` (`Cost`, `Seconds`): formül yerine dizi |
| Hız formülü (K-13) | `ResearchRules.ApplyPercent` → bölen formülü |
| Saatlik üretim (K-17) | `ProductionFormulas` + `ProductionSimulator`: saatlik değerden milli-birim tik, kesir taşıma |
| Reaktör önkoşulları | `BuildingProgression.ValidateUpgrade` → yeni `PrerequisiteMissing` red nedeni |
| Süperalaşım | `ResourceType.Alloy = 5` (enum'da hazır); cüzdan, HUD kaynak çipi |
| Reaktörle büyüyen tavan | `ResearchRules.Capacities` → reaktör seviyesine bağlı tavan + Basınç Ambarı |
| Kaynak Kapsülü | `QuestCatalogData` öğe türü `resourceCrate`; `PlayerSession.Quests.cs` ödül ekleme |
| Güç | `PlayerSession.Heroes.cs` güç hesabı → bina/araştırma/asker/kahraman toplamı |
| Overseer | Denge sayfası seviye dizilerini düzenleyebilmeli; `LiveCatalog` sürümleme aynı kalır |

## 13. Açık sorular

- Reaktör 50'den sonra (Çekirdek Rezonansı) ilerleme hangi sezonda açılacak? Bkz. [14 · Son Oyun](14_son_oyun.md).
- Sunucu birleşmelerinde tempo farkı (yeni ve eski oyuncular) nasıl yumuşatılacak?
- Simülasyon toplama ve sefer gelirini düz bir eğri olarak alıyor; harita çekişmesi (yağma, bölge
  kaybı) gelir dalgalanmasını nasıl etkiler?
