# 10 · İttifak

> **Özet.** İttifak, Hadal'da yalnız kalmamanın yoludur: yardım inşaatı kısaltır, teknoloji bütün
> üyeleri güçlendirir, bölge toplamayı hızlandırır, ralli tek başına yenilemeyecek yuvaları düşürür.
> Beş rütbe (R5 Lider … R1 Aday) açık bir yetki tablosuyla çalışır. İttifak 20 seviyede 30 üyeden
> 100'e büyür. Altı dallı teknoloji **eşit bağışla** ilerler: herkesin bağışı aynı sayılır, bedeli
> kendi reaktörüne göredir. Hediyeler sessizdir: kimin aldığı görünmez. Bugün kodda kurma, katılma,
> Lider/Üye, yardım ve sohbet var; geri kalanı tasarımdır.

Bağlı bölümler: [00 · Kanon](00_kanon.md) · [02 · Tempo ve Denge](02_tempo_ve_denge.md) ·
[03 · Binalar](03_binalar.md) · [09 · Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md) ·
[12 · The Syndicate](12_syndicate.md) · [13 · Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md) ·
[15 · Ekonomi Akışı](15_ekonomi_akisi.md) · [16 · Kronik, Köken, Profil](16_kronik_kokenler_profil.md).
Veri: [`veri/ittifak.json`](veri/ittifak.json).

---

## 1. Amaç

1. **Yalnız oynayanı cezalandırmadan, birlikte oynayanı ödüllendirmek.** Yardım, teknoloji ve bölge
   tek başına oynayana kapalı değildir ama ittifak onları katlar.
2. **Sunucunun siyasetini taşımak.** Geçitler, Rezonans Alanları ve Çukur Kalbi ittifaklar arasında
   el değiştirir ([09](09_dunya_haritasi_ve_pvp.md)); Kronik'in ittifak hedefleri ortak bir takvim verir.
3. **Harcamayı sessizce paylaşmak.** Ödeyen bir üyenin alımı, bütün ittifaka adsız bir hediye olarak
   yansır; ücretsiz oyuncu bundan pay alır, ödeyen kişi teşhir edilmez.
4. **Katkıyı servetten ayırmak.** Teknolojiye bağış, kaptanın gücüne değil katılımına göre sayılır.

---

## 2. Kuruluş ve katılım

| Kural | Değer |
|---|---|
| Kurma bedeli | <!--v:veri.ittifak.kodda.0.kodda-->3.000 Titanyum<!--/v--> (kodda) |
| Ad ve etiket | <!--v:veri.ittifak.kodda.1.kodda-->3–20 / 3–4 karakter, benzersiz<!--/v--> (kodda) |
| Katılım türü | **Açık** (hemen R1), **Onaylı** (R3+ onaylar), **Kapalı** (yalnızca davet) |
| Katılım filtresi | En düşük reaktör seviyesi ve dil etiketi (isteğe bağlı) |
| Bir kaptan | Aynı anda tek ittifak; ayrılınca 4 saat yeniden katılamaz |

- İttifakı kuran R5 olur. Yeni katılan **R1 Aday**'dır; 48 saat sonra kendiliğinden ya da bir R4'ün
  onayıyla R2 olur. Aday dönemi, ittifak dükkânını ve ambarını boşaltıp ayrılan "kredi turistlerini"
  durdurur.
- İttifak tavsiyesi: yeni kaptana, aynı kanattaki ([09](09_dunya_haritasi_ve_pvp.md) §2.4) ve kendi
  diline yakın, son 24 saatte etkin üyesi olan üç ittifak önerilir.

---

## 3. Rütbeler ve yetkiler

<!-- URET:tablo:ittifak.rutbeler -->
| Rütbe | Ad | Adet | Rolü |
| --- | --- | --- | --- |
| R5 | Lider | 1 | İttifakın sahibi. Egemen olunursa Egemen odur. |
| R4 | Kurmay | 4 → 8 (seviyeyle) | Yönetim: üye, bölge, dükkân, teknoloji önceliği. |
| R3 | Usta | Sınırsız | Güvenilen çekirdek: başvuru onayı, harita imleri, kule kurma. |
| R2 | Dalgıç | Sınırsız | Tam üye: ralli, dükkân, ambar. |
| R1 | Aday | Sınırsız | Yeni katılan; 48 saat ya da R4 onayıyla R2 olur. |
<!-- /URET:tablo:ittifak.rutbeler -->

<!-- URET:tablo:ittifak.yetkiler -->
| Yetki | R5 | R4 | R3 | R2 | R1 |
| --- | --- | --- | --- | --- | --- |
| Başvuru onayı ve davet | ✔ | ✔ | ✔ | — | — |
| R1–R2 üyeyi çıkarma | ✔ | ✔ | — | — | — |
| R3 üyeyi çıkarma | ✔ | ✔ | — | — | — |
| R1–R3 rütbe değiştirme | ✔ | ✔ | — | — | — |
| R4 atama ve görevden alma | ✔ | — | — | — | — |
| Liderliği devretme, ittifakı dağıtma | ✔ | — | — | — | — |
| Ad, etiket, amblem, katılım türü | ✔ | — | — | — | — |
| Duyuru ve ittifak postası | ✔ | ✔ | — | — | — |
| Sohbette mesaj sabitleme | ✔ | ✔ | — | — | — |
| Teknoloji önceliği ve devreye alma | ✔ | ✔ | — | — | — |
| Karargâh, maden, ambar kurma ve kaldırma | ✔ | ✔ | — | — | — |
| Sonar Kulesi kurma | ✔ | ✔ | ✔ | — | — |
| Dükkânı stoklama (İttifak Kredisi) | ✔ | ✔ | — | — | — |
| Savaş bildirisi, ateşkes | ✔ | — | — | — | — |
| Dost ittifak ve geçit izni | ✔ | ✔ | — | — | — |
| Haritaya ittifak imi koyma | ✔ | ✔ | ✔ | — | — |
| Ralli kurma | ✔ | ✔ | ✔ | ✔ | — |
| İttifak Dükkânı'ndan alım | ✔ | ✔ | ✔ | ✔ | — |
| İttifak Ambarı'na yatırma | ✔ | ✔ | ✔ | ✔ | — |
| Bağış, yardım, takviye, ralliye katılma | ✔ | ✔ | ✔ | ✔ | ✔ |
<!-- /URET:tablo:ittifak.yetkiler -->

Kurallar:

- Bir kaptan yalnızca kendinden düşük rütbeyi değiştirebilir ya da çıkarabilir. R4 başka bir R4'ü
  çıkaramaz.
- **Liderlik devri.** Lider ayrılırsa (kodda) en eski üye lider olur. Hedef: lider 7 gün girmezse
  liderlik son 7 günde en çok katkı veren R4'e, R4 yoksa R3'e geçer; eski lider döndüğünde R4 olur.
- **R4 görevleri.** R4'ler isteğe bağlı bir görev etiketi taşır (Savaş, Lojistik, Diplomasi, Keşif).
  Etiket yetki vermez; ittifak listesinde kime ne için yazılacağını gösterir.
- Rütbe değişiklikleri ittifak sohbetine tek satırlık sistem mesajı olarak düşer.

---

## 4. İttifak seviyesi ve üye sayısı

İttifak deneyimi üyelerin ortak etkinliğinden gelir; satın alınamaz.

<!-- URET:tablo:ittifak.xp_kaynaklari -->
| Kaynak | Deneyim | Not |
| --- | --- | --- |
| Bağış (her biri) | 10 | Kaptan başına günde en çok ~48 bağış hakkı |
| Yardım (her biri) | 1 | Kaptan başına günde en çok 50 sayılır |
| PvE rallisi zaferi | 50 | Katılımcı başına |
| Leviathan yuvası zaferi | 500 | Yuva seviyesi × 100 ek |
| Bağlı Sonar Kulesi | 20 | Kule başına günde |
| Kronik ittifak hedefi | 5.000–50.000 | Aşamaya göre |
<!-- /URET:tablo:ittifak.xp_kaynaklari -->

<!-- URET:tablo:ittifak.seviyeler -->
| Sv | Deneyim (adım) | Deneyim (toplam) | Üye | R4 yuvası | Karargâh | Kule sınırı | Açılan |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | — | 0 | 30 | 4 | 1 | 10 | Hız, Kalkınma dalları; temel dükkân |
| 2 | 10.000 | 10.000 | 33 | 4 | 1 | 16 | Harita dalı; İttifak Ambarı |
| 3 | 13.300 | 23.300 | 36 | 4 | 1 | 22 | Bölge dalı; Titanyum, Gıda, Biyokütle madenleri |
| 4 | 17.700 | 41.000 | 39 | 4 | 1 | 28 | Savunma dalı |
| 5 | 23.500 | 64.500 | 43 | 5 | 1 | 34 | Savaş bildirisi |
| 6 | 31.300 | 95.800 | 46 | 5 | 1 | 40 | Muharebe dalı; dükkân 2. kademe |
| 7 | 41.600 | 137.400 | 49 | 5 | 1 | 46 | — |
| 8 | 55.300 | 192.700 | 52 | 5 | 2 | 52 | 2. Karargâh |
| 9 | 73.600 | 266.300 | 55 | 5 | 2 | 58 | — |
| 10 | 97.900 | 364.200 | 58 | 6 | 2 | 64 | Süperalaşım madeni |
| 11 | 130.000 | 494.200 | 62 | 6 | 2 | 70 | — |
| 12 | 173.000 | 667.200 | 65 | 6 | 2 | 76 | Dükkân 3. kademe |
| 13 | 230.000 | 897.200 | 68 | 6 | 2 | 82 | — |
| 14 | 306.000 | 1.203.200 | 71 | 6 | 2 | 88 | — |
| 15 | 407.000 | 1.610.200 | 74 | 7 | 3 | 94 | 3. Karargâh |
| 16 | 542.000 | 2.152.200 | 77 | 7 | 3 | 100 | — |
| 17 | 721.000 | 2.873.200 | 81 | 7 | 3 | 106 | — |
| 18 | 959.000 | 3.832.200 | 84 | 7 | 3 | 112 | Dükkân 4. kademe |
| 19 | 1.270.000 | 5.102.200 | 87 | 7 | 3 | 118 | — |
| 20 | 1.700.000 | 6.802.200 | 90 | 8 | 3 | 124 | En yüksek seviye; kulelerle 100 üye |
<!-- /URET:tablo:ittifak.seviyeler -->

- Üye sınırı seviyeyle 30'dan 90'a çıkar; bağlı her 12 Sonar Kulesi +1 üye ekler (en çok +10). Tam
  bir ittifak böylece **100 üyedir**.
- Referans tempo: 50 etkin üyeli bir ittifak günde ~15.000–20.000 deneyim toplar; Sv 10'a ~45. günde,
  Sv 15'e ~130. günde, Sv 20'ye ~330. günde ulaşır. Sv 20, Reaktör 50 gibi bir yıllık hedeftir.
- İttifak seviyesi düşmez. Üye ayrılsa da kazanılan deneyim kalır.

---

## 5. Yardım

Yardım kodda vardır ve İttifak Merkezi'nin işidir ([İttifak Merkezi](binalar/alliance_hub.md)):

```
yardım sınırı = 10 + ⌊İttifak Merkezi seviyesi / 5⌋   (en çok 20)
her yardım    = sürenin %1'i, en az 60 sn
```

Değerler `ilerleme.json`'dan: taban <!--v:veri.ilerleme.kurallar.yardim.taban-->10<!--/v-->, her
<!--v:veri.ilerleme.kurallar.yardim.her_hub_seviyesi-->5<!--/v--> seviyede +1, en çok
<!--v:veri.ilerleme.kurallar.yardim.maks-->20<!--/v-->; yardım başına
%<!--v:veri.ilerleme.kurallar.yardim.yuzde-->1<!--/v-->, en az
<!--v:veri.ilerleme.kurallar.yardim.min_sn-->60<!--/v--> sn. Sınırı yardımı **isteyenin** İttifak
Merkezi belirler.

- İttifak Merkezi ayrıca alıcının **takviye kapasitesini** belirler: [09](09_dunya_haritasi_ve_pvp.md) §8.3.
- Kodda yardım yalnızca inşaata uygulanır ve sınır sabit 10'dur. Hedefte araştırma ve iyileştirme de
  yardım alır; sınır İttifak Merkezi seviyesiyle büyür.
- İttifak teknolojisi **Dayanışma Ağı** yardımın en az süresini 60 sn'den 135 sn'ye çıkarır; erken
  oyunun kısa inşaatlarını hızlandırır, geç oyunu bozmaz.
- Derinlik İzni'nin (VIP) ek yardımları bu sınırın **üstüne** eklenir ([11 · VIP](11_vip.md)).
- Her yardım yapana 1 Bireysel Kredi (günde en çok 50) ve 1 ittifak deneyimi verir. "Hepsine yardım"
  tek dokunuştur (kodda).
- Tempo simülasyonu ücretsiz oyuncunun yardımların %80'ini aldığını varsayar ([02](02_tempo_ve_denge.md) §5).

---

## 6. İttifak teknolojisi

Teknoloji bütün üyelerine kalıcı etki verir. Altı dal, otuz üç teknoloji; dallar ittifak seviyesiyle
açılır.

<!-- URET:tablo:ittifak.dallar -->
| Dal | İttifak Sv | Teknoloji | Toplam puan | Kapsam |
| --- | ---: | ---: | ---: | --- |
| Kalkınma | 1 | 6 | 472.000 | Üretim ve depo: üssün ekonomisi. |
| Hız | 1 | 5 | 507.500 | İnşaat, araştırma, eğitim, iyileştirme ve yardım. |
| Harita | 2 | 6 | 426.000 | Toplama, taşıma, yürüyüş, Sonar Enerjisi, PvE. |
| Bölge | 3 | 5 | 256.000 | Kuleler, madenler, ambar, dükkân, hediyeler. |
| Savunma | 4 | 5 | 415.000 | Takviye, dayanıklılık, garnizon, Revir. |
| Muharebe | 6 | 6 | 787.500 | Saldırı, savunma, can, ralli, Leviathan. |
<!-- /URET:tablo:ittifak.dallar -->

<!-- URET:tablo:ittifak.teknolojiler -->
| Dal | Teknoloji | Seviye başına | Maks | Sv 1 puanı | Son seviye puanı | Toplam puan | İttifak Sv |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| Kalkınma | Titanyum Verimi | +%1 Titanyum üretimi | 10 | 1.500 | 15.000 | 82.500 | 1 |
| Kalkınma | Gıda Verimi | +%1 Gıda üretimi | 10 | 1.200 | 12.000 | 66.000 | 1 |
| Kalkınma | Biyokütle Verimi | +%1 Biyokütle üretimi | 10 | 1.200 | 12.000 | 66.000 | 2 |
| Kalkınma | Yaşam Desteği | +%1 Oksijen ve Enerji üretimi | 10 | 1.000 | 10.000 | 55.000 | 2 |
| Kalkınma | Ambar Mühürleri | +%2 Basınç Ambarı koruması | 5 | 2.500 | 12.500 | 37.500 | 4 |
| Kalkınma | Döküm Ustalığı | +%1 Alaşım Dökümhanesi verimi | 10 | 3.000 | 30.000 | 165.000 | 10 |
| Hız | Ortak Mühendislik | +%0,5 inşa hızı | 10 | 3.000 | 30.000 | 165.000 | 1 |
| Hız | Ortak Laboratuvar | +%0,5 araştırma hızı | 10 | 3.000 | 30.000 | 165.000 | 2 |
| Hız | Eğitim Protokolü | +%1 asker eğitim hızı | 10 | 2.000 | 20.000 | 110.000 | 3 |
| Hız | Dayanışma Ağı | Yardımın en az süresi +15 sn (60 → 135 sn) | 5 | 2.500 | 12.500 | 37.500 | 3 |
| Hız | Revir Protokolü | +%2 iyileştirme hızı | 5 | 2.000 | 10.000 | 30.000 | 5 |
| Harita | Hızlı Toplama | +%1,5 toplama hızı | 10 | 1.500 | 15.000 | 82.500 | 2 |
| Harita | Geniş Ambar Bölmesi | +%2 taşıma kapasitesi | 10 | 1.200 | 12.000 | 66.000 | 2 |
| Harita | Akıntı Okuma | +%1 yürüyüş hızı | 10 | 1.500 | 15.000 | 82.500 | 4 |
| Harita | Sonar Rezonansı | +%2 Sonar Enerjisi yenilenmesi | 10 | 1.500 | 15.000 | 82.500 | 4 |
| Harita | Derin Haritacılık | +1 kare görüş yarıçapı | 5 | 2.000 | 10.000 | 30.000 | 6 |
| Harita | Av Taktikleri | +%1 PvE düşmanlarına hasar | 10 | 1.500 | 15.000 | 82.500 | 6 |
| Bölge | Kule Mühendisliği | −%8 Sonar Kulesi inşa süresi | 5 | 1.500 | 7.500 | 22.500 | 3 |
| Bölge | Maden Verimi | +%3 İttifak Madeni toplama hızı | 10 | 1.500 | 15.000 | 82.500 | 3 |
| Bölge | Ambar Genişletme | +%10 İttifak Ambarı kişisel bölmesi | 10 | 1.200 | 12.000 | 66.000 | 5 |
| Bölge | Dükkân Ağı | −%2 dükkân stok bedeli | 5 | 2.000 | 10.000 | 30.000 | 6 |
| Bölge | Hediye Ağı | +%2 hediye puanı | 10 | 1.000 | 10.000 | 55.000 | 6 |
| Savunma | Takviye Ağı | +%2 takviye kapasitesi | 10 | 2.000 | 20.000 | 110.000 | 4 |
| Savunma | Kalkan Kaplaması | +%2 üs dayanıklılığı | 10 | 1.500 | 15.000 | 82.500 | 4 |
| Savunma | Su Geçirmez Bölmeler | −%5 su baskını kaybı | 5 | 2.000 | 10.000 | 30.000 | 6 |
| Savunma | Garnizon Disiplini | +%1 garnizondaki askerin savunması | 10 | 2.000 | 20.000 | 110.000 | 8 |
| Savunma | Revir Kanatları | +%2 yaralı kapasitesi | 10 | 1.500 | 15.000 | 82.500 | 8 |
| Muharebe | Saldırı Doktrini | +%0,5 asker saldırısı | 10 | 3.000 | 30.000 | 165.000 | 6 |
| Muharebe | Savunma Doktrini | +%0,5 asker savunması | 10 | 3.000 | 30.000 | 165.000 | 6 |
| Muharebe | Gövde Takviyesi | +%0,5 asker canı | 10 | 3.000 | 30.000 | 165.000 | 7 |
| Muharebe | Ralli Komutası | +%1 ralli kapasitesi | 10 | 2.500 | 25.000 | 137.500 | 8 |
| Muharebe | Leviathan Avcılığı | +%1 Leviathan ve Kadim Muhafızlara hasar | 10 | 2.000 | 20.000 | 110.000 | 10 |
| Muharebe | Kuşatma | +%2 geçit ve yapı dayanıklılığına hasar | 5 | 3.000 | 15.000 | 45.000 | 12 |
<!-- /URET:tablo:ittifak.teknolojiler -->

Bir teknolojinin ℓ. seviyesi `taban × ℓ` puan ister; bütün ağaç
<!--v:veri.ittifak.bagis.toplam_puan-->2.864.000<!--/v--> puan,
<!--v:veri.ittifak.bagis.seviye-->290<!--/v--> seviyedir.

### 6.1 Bağış kuralları

| Kural | Değer |
|---|---|
| Bağış hakkı | En çok <!--v:veri.ittifak.bagis.hak-->20<!--/v-->; her <!--v:veri.ittifak.bagis.hak_dk-->30<!--/v--> dakikada +1 (günde <!--v:veri.ittifak.bagis.gunluk_hak-->48<!--/v-->) |
| Bir bağışın bedeli | <!--v:veri.ittifak.bagis.bedel_gs-->0,1<!--/v--> **Gelir Saati** Titanyum, Gıda ya da Biyokütle (bağışlayanın reaktörüne göre) |
| Bir bağışın kazancı | <!--v:veri.ittifak.bagis.puan-->10<!--/v--> teknoloji puanı · <!--v:veri.ittifak.bagis.bireysel-->10<!--/v--> Bireysel Kredi · <!--v:veri.ittifak.bagis.ittifak-->10<!--/v--> İttifak Kredisi · <!--v:veri.ittifak.bagis.xp-->10<!--/v--> deneyim |
| Öncelikli teknoloji | R4'ün işaretlediği teknolojiye bağışta Bireysel Kredi +%<!--v:veri.ittifak.bagis.oncelik_ek-->50<!--/v--> |
| Hadalit bağışı | <!--v:veri.ittifak.bagis.hadalit_bedel-->20<!--/v--> Hadalit = <!--v:veri.ittifak.bagis.hadalit_puan-->20<!--/v--> puan ve iki kat kredi; günde en çok <!--v:veri.ittifak.bagis.hadalit_gunluk-->10<!--/v--> |
| Devreye alma | Puan dolunca R4 başlatır; seviye başına <!--v:veri.ittifak.bagis.devreye_alma_dk-->30<!--/v--> dk, aynı anda tek teknoloji |

- **Gelir Saati** (GS), o reaktör seviyesindeki ücretsiz oyuncunun bir saatlik Titanyum gelirdir
  ([15](15_ekonomi_akisi.md), `oduller.json`). Reaktör 10'daki kaptan bir bağışa ~900 Titanyum,
  Reaktör 40'taki ~17.000 Titanyum öder; ikisi de aynı **10 puanı** verir. Katkı servetle değil
  süreklilikle ölçülür.
- Günde 48 bağış bir kaptanın gelirinin ~%20'sine denk gelir; tipik bağışçı (günde ~25) ~%10'unu verir.
- 50 etkin üyeli ittifak bütün ağacı ~230 günde, 30 üyeli ittifak ~380 günde tamamlar.
- Hadalit bağışı sınırlıdır; ödeyen bir üye teknolojiyi tek başına bitiremez.

---

## 7. Krediler

İki ayrı kredi vardır: **Bireysel Kredi** kaptanındır ve İttifak Dükkânı'nda harcanır; **İttifak
Kredisi** (İK) ittifakın ortak hesabıdır ve R4'ler tarafından yapılara, dükkân stoğuna ve savaş
bildirisine harcanır.

<!-- URET:tablo:ittifak.krediler -->
| Kaynak | Bireysel Kredi | İttifak Kredisi |
| --- | --- | --- |
| Bağış | 10 (öncelikli teknolojide 15) | 10 |
| Yardım | 1 (günde en çok 50) | 1 |
| PvE rallisi zaferi | 20 | 20 |
| Leviathan yuvası zaferi | 40 | 200 |
| Garnizonda geçen saat (bölge yapısı, geçit, alan) | 5 (günde en çok 60) | — |
| Tutulan Rezonans Alanı | — | 100 / gün |
| Kronik ittifak hedefi | 200 | 1.000–5.000 |
<!-- /URET:tablo:ittifak.krediler -->

- Bireysel Kredi ittifak değiştirince korunur; Aday (R1) iken harcanamaz.
- İttifak Kredisi ittifak dağılırsa kaybolur.
- Bölge yapılarının Titanyum bedelini kuleler ve madenlerin beslediği **ortak hesap** öder (§10);
  üyeler ortak hesaba doğrudan kaynak da bağışlayabilir (teknoloji puanı vermez).

---

## 8. İttifak Dükkânı

R4'ler ürünleri İttifak Kredisi'yle **stoklar**; üyeler stoktan Bireysel Kredi'yle alır. Kademe
ittifak seviyesiyle açılır (1 · 6 · 12 · 18).

<!-- URET:tablo:ittifak.dukkan -->
| Ürün | Stok bedeli (İK) | Üye fiyatı (Bireysel) | Kaptan sınırı | Dükkân kademesi |
| --- | ---: | ---: | --- | ---: |
| Genel Hızlandırma 5 dk | 5 | 30 | 20 / gün | 1 |
| Genel Hızlandırma 1 sa | 50 | 300 | 5 / gün | 1 |
| İnşa Hızlandırma 1 sa | 40 | 240 | 5 / gün | 1 |
| Araştırma Hızlandırma 1 sa | 40 | 240 | 5 / gün | 1 |
| Sonar Pili (küçük, +50) | 10 | 60 | 10 / gün | 1 |
| Kaynak Kapsülü (0,5 GS) | 30 | 200 | 5 / gün | 1 |
| Sonar Pili (orta, +200) | 40 | 220 | 3 / gün | 2 |
| Bereket Akışı (+%25 toplama, 8 sa) | 60 | 400 | 1 / gün | 2 |
| Sessizlik Kalkanı · 8 sa | 150 | 1.200 | 1 / gün | 2 |
| İttifak Rotası | 120 | 900 | 1 / gün | 2 |
| Basınç Kodeksi | 80 | 500 | 2 / gün | 3 |
| Anı Kristali | 60 | 450 | 2 / gün | 3 |
| Hassas Konuşlanma | 400 | 3.000 | 1 / hafta | 4 |
<!-- /URET:tablo:ittifak.dukkan -->

Tipik bir ücretsiz üye günde ~250–300 Bireysel Kredi kazanır: her gün bir saatlik hızlandırma ya da
birkaç Sonar Pili, haftada bir Sessizlik Kalkanı. Dükkân, ödeme yapmadan kazanılan en önemli kalkan ve
konuşlanma kaynağıdır.

---

## 9. Hediyeler

Hediyeler **sessizdir**: kimin aldığı görünmez, tutarı yazmaz, ekrana pop-up düşmez. İttifak sekmesinde
küçük bir rozet belirir; kaptan istediğinde açar.

<!-- URET:tablo:ittifak.hediyeler -->
| Hediye | Ne zaman gelir | İçerik | Hediye puanı | Açma süresi |
| --- | --- | --- | --- | ---: |
| Kıyı Hediyesi | Bir üyenin küçük The Syndicate alımı | Hızlandırma, Kaynak Kapsülü, az Hadalit | 10 | 3 g |
| Derin Hediye | Bir üyenin orta alımı ya da Sendika Sözleşmesi | Daha uzun hızlandırma, Sonar Pili, Hadalit | 40 | 3 g |
| Hadal Hediyesi | Bir üyenin büyük alımı | Kriyo-Kapsül çekimi şansı, Süperalaşım (R30+) | 120 | 3 g |
| Av Hediyesi | Leviathan Avı etkinliğinin 5., 10., 15. dalgası (13) ya da haritada bir Leviathan yuvası zaferi (günde en çok 3) | Basınç Kodeksi şansı, Süperalaşım, hızlandırma | 20 | 3 g |
| Kronik Hediyesi | Bir Kronik ittifak hedefi | Aşamaya göre sandık | 30 | 7 g |
| Derin Hazine | Hediye puanı çubuğu her 2.000 puanda dolar | Büyük sandık; Hediye Seviyesi'ne göre | — | 7 g |
<!-- /URET:tablo:ittifak.hediyeler -->

- **Adsızlık varsayılandır.** Alım yapan kaptan isterse adını gösterebilir; varsayılan "Bir ittifak
  üyesinden" yazar. Kim, ne kadar, hangi paket hiçbir yerde listelenmez; "en çok hediye getiren"
  sıralaması yoktur.
- **Teşekkür et** düğmesi adsız göndericiye adsız bir teşekkür iletir.
- **Hediye puanı** her hediyede ittifaka bir kez yazılır ve **Hediye Seviyesi**'ni büyütür; seviye
  sandıkların içeriğini çarpanla zenginleştirir. Her 2.000 puanda bütün üyelere **Derin Hazine** düşer.
- Av ve Kronik hediyeleri harcamadan doğar: ücretsiz oyuncuların Leviathan avı ve Kronik hedefleri de
  bütün ittifakı ödüllendirir.

<!-- URET:tablo:ittifak.hediye_seviyeleri -->
| Hediye Sv | Toplam puan | İçerik çarpanı |
| ---: | ---: | --- |
| 1 | 0 | ×1,0 |
| 2 | 500 | ×1,05 |
| 3 | 1.120 | ×1,1 |
| 4 | 1.900 | ×1,15 |
| 5 | 2.880 | ×1,2 |
| 6 | 4.100 | ×1,25 |
| 7 | 5.630 | ×1,3 |
| 8 | 7.540 | ×1,35 |
| 9 | 9.920 | ×1,4 |
| 10 | 12.900 | ×1,45 |
| 11 | 16.630 | ×1,5 |
| 12 | 21.290 | ×1,55 |
| 13 | 27.110 | ×1,6 |
| 14 | 34.390 | ×1,65 |
| 15 | 43.480 | ×1,7 |
| 16 | 54.880 | ×1,75 |
| 17 | 69.080 | ×1,8 |
| 18 | 86.880 | ×1,85 |
| 19 | 109.080 | ×1,9 |
| 20 | 136.880 | ×1,95 |
<!-- /URET:tablo:ittifak.hediye_seviyeleri -->

Hediye içeriklerinin kaynak değeri F2P gelir modelinde "Etkinlik + ittifak" payındadır
([15](15_ekonomi_akisi.md)); paket–hediye eşlemesi [12 · The Syndicate](12_syndicate.md)'tedir.

---

## 10. Bölge

Bölge, ittifakın haritada çizdiği alandır ([09](09_dunya_haritasi_ve_pvp.md)). Kronik aşama 2'de
(İlk Yankı) açılır.

<!-- URET:tablo:ittifak.bolge_yapilari -->
| Yapı | Boyut | Bölge yarıçapı | Sınır | Bedel | İnşa | Garnizon (sefer) | İşlev |
| --- | --- | ---: | --- | --- | ---: | ---: | --- |
| Karargâh Kubbesi | 3 × 3 | 7 | 1 · 2 (Sv 8) · 3 (Sv 15) | 2.000.000 Ti (ortak) + 1.000 İK | 8 sa | 20 | Bölgenin çekirdeği; İttifak Rotası iniş noktası; ambar ve madenler buna bağlanır. |
| Sonar Kulesi | 1 × 1 | 3 | 10 + 6 × (Sv − 1) | 100.000 Ti (ortak) + 50 İK; her 10 kulede ×1,25 | 1 sa | 3 | Bölgeyi genişletir; bağlıyken saatte 1.500 Ti ortak hesaba yazar. |
| İttifak Madeni | 2 × 2 | 0 | Her türden 1 (Ti, GD, BK; Sv 10'da SA) | 500.000 Ti (ortak) + 300 İK | 4 sa | 0 | 5.000.000 yük; üyeler toplar, toplananın %10'u kadar ortak hesaba ek yazılır. |
| İttifak Ambarı | 2 × 2 | 0 | 1 (Sv 2) | 1.000.000 Ti (ortak) + 500 İK | 4 sa | 0 | Kişisel bölmeler: yağmalanmaz, yatırılan 24 saat çekilemez. |
<!-- /URET:tablo:ittifak.bolge_yapilari -->

<!-- URET:tablo:ittifak.bolge_etkileri -->
| Etki | Değer |
| --- | --- |
| Toplama hızı (bölgedeki yataklarda ve madenlerde) | +%20 |
| Yürüyüş hızı (kendi bölgende) | +%30 |
| Yürüyüş hızı (başka ittifakın bölgesinde) | −%15 |
| Bölgedeki üye üslerinin savunması | +%5 |
| Görüş | Bölgenin tamamı sissiz |
| Üye kapasitesi | Her 12 bağlı kulede +1 (en çok +10) |
<!-- /URET:tablo:ittifak.bolge_etkileri -->

**Bağlantı kuralı.** Bölge, bir Karargâh Kubbesi'nden başlayıp kesintisiz yayılmalıdır:

1. Yeni Sonar Kulesi, bölge alanı ittifakın bağlı bölgesine **değecek ya da bindirecek** bir kareye
   kurulur. Tutulan geçitler ve Rezonans Alanları da bağlantı noktası sayılır.
2. Bir kule yıkılınca bağlantısı Karargâh'a kopan kuleler **söner**: bölge etkisi vermez, ortak hesaba
   yazmaz, üye sınırına sayılmaz. Bağlantı onarılınca kendiliğinden yanar.
3. Başka ittifakın bölgesine 3 kareden yakın kule kurulmaz; iki bölge üst üste binmez.
4. Dip ve Çekirdek'e, yatak, geçit ve alan karelerine yapı kurulmaz. Abisal Düzlük'e kule ve maden
   kurulur, Karargâh kurulmaz.
5. İttifak yapılarına saldırı yalnızca Gelgit Penceresi'nde yapılır; savaş bildirisi bu sınırı kaldırır
   (§11).

**Ortak hesap (İttifak Kaynakları).** Bağlı her kule saatte 1.500 Titanyum yazar; İttifak Madeni'nde
toplanan her yükün %10'u kadar ek yazılır. 60 kuleli bir ittifakın ortak hesabı günde ~2,2 milyon
Titanyum büyür; yeni Karargâh, kule ve maden buradan ödenir.

**İttifak Madeni.** Bölgede kurulan 5.000.000 yüklük özel yatak. Yalnızca üyeler toplar; bölge
bonusu (+%20) ve Maden Verimi teknolojisi uygulanır. Tükenen maden kaldırılır, yenisi kurulabilir.
Süperalaşım madeni İttifak Sv 10 ve Kronik aşama 5 ister.

**İttifak Ambarı.** Her üyenin kişisel bölmesi vardır. Bölmedeki kaynak yağmalanmaz; yatırılan kaynak
**24 saat** çekilemez. Bölme sınırı: üyenin o reaktördeki Titanyum tavanının %20'si (her kaynak),
Ambar Genişletme teknolojisiyle büyür. Üye ayrılırsa bölmesi 24 saat sonra postayla iade edilir.
Ambar, uzun süre giremeyecek kaptanın sığınağıdır; 24 saat kilidi onu bir savaş anı kaçış kapısı
olmaktan çıkarır.

---

## 11. Diplomasi ve savaş bildirisi

- **Dost ittifak:** R5/R4 en çok 3 dost ittifak belirler. Dostlar birbirinin geçitlerinden geçebilir,
  birbirinin bölgesinde hız cezası almaz; birbirine saldıramaz. Dostluk iki tarafın onayıyla kurulur,
  tek tarafın kararıyla 12 saat sonra biter.
- **Savaş bildirisi:**

| Kural | Değer |
|---|---|
| Kim | R5 |
| Bedel | <!--v:veri.ittifak.savas_bildirisi.bedel_ik-->2.000<!--/v--> İttifak Kredisi |
| Şartlar | İttifak Sv ≥ <!--v:veri.ittifak.savas_bildirisi.min_sv-->5<!--/v-->, Kronik aşama ≥ <!--v:veri.ittifak.savas_bildirisi.min_asama-->4<!--/v-->; hedefin gücü (ilk 30 üye) bildirenin en az %<!--v:veri.ittifak.savas_bildirisi.guc_orani-->50<!--/v-->'si |
| Hazırlık | <!--v:veri.ittifak.savas_bildirisi.hazirlik_saat-->12<!--/v--> saat (iki tarafa sessiz bildirim) |
| Savaş | <!--v:veri.ittifak.savas_bildirisi.savas_saat-->72<!--/v--> saat |
| Aynı çift | <!--v:veri.ittifak.savas_bildirisi.ayni_cift_gun-->7<!--/v--> gün bekleme |
| Ateşkes | İki R5'in onayıyla erken biter |

Savaş süresince: iki ittifak birbirinin yapılarına Gelgit Penceresi dışında da saldırabilir; aralarında
güç farkı eşikleri <!--v:veri.ittifak.savas_bildirisi.esik_carpani-->1,5<!--/v--> katına çıkar
([09](09_dunya_haritasi_ve_pvp.md) §10.5); bir savaş tablosu Savaş Puanı'nı, yıkılan kuleleri ve el
değiştiren geçitleri tutar. Bitişte iki ittifaka da posta raporu düşer; dünya sohbetine yalnızca tek
satır yazılır. Güç şartı, büyük bir ittifakın küçük bir ittifaka savaş açıp güç farkı korumasını
delmesini engeller.

---

## 12. Posta ve sohbet

<!-- URET:tablo:ittifak.posta -->
| Tür | Kim gönderir | Sınır | Not |
| --- | --- | --- | --- |
| Kişisel posta | Herkes | 1 alıcı · 1.000 karakter · saatte 20 | Engelleme ve şikâyet |
| İttifak postası | R5, R4 | Tüm üyeler · 2.000 karakter · günde 3 | Okundu sayacı yalnızca gönderene |
| Rapor | Sistem | Savaş, keşif, toplama, ralli · 30 gün saklanır | Yıldızlanan rapor kalıcı |
| Kronik | Kronik Feneri | Aşama açılışları, sunucu ilkleri | Ayrı sekme |
<!-- /URET:tablo:ittifak.posta -->

**Sohbet** (kodda): iki kanal, **Dünya** ve **İttifak**; mesaj en çok 200 karakter, iki mesaj arası
2 saniye, girişte her kanalın son 50 mesajı. İttifak etiketi adın önünde görünür; katılma, ayrılma ve
çıkarılma sistem mesajı olarak düşer. Hedefe eklenenler:

- R4+ **sabit duyuru** (kanalın üstünde tek satır).
- **Koordinat bağlantısı:** `[X:Y]` yazılan koordinat dokunulabilir olur, haritada o kareyi açar.
- **Ralli kartı:** kurulan ralli ittifak kanalına hedef, toplanma süresi ve katılım düğmesiyle düşer.
- Susturma (`reason.Muted`) ve şikâyet; Overseer moderasyonu bugünkü gibi.
- Akıntı Savaşı kanalı sezon boyunca açılır ([13](13_etkinlikler_ve_sezonlar.md)).

The Syndicate teklifleri postaya ve sohbete **hiçbir zaman** düşmez.

---

## 13. Ücretsiz oyuncu ve ödeyen oyuncu

| Konu | Ücretsiz oyuncu | Ödeyen oyuncu |
|---|---|---|
| Teknoloji katkısı | Bağışı tam sayılır; bedeli kendi gelirine göre | Hadalit bağışı günde 10 ile sınırlı |
| Dükkân | Günlük kredisiyle hızlandırma, haftalık kalkan | Aynı fiyatlar, aynı sınırlar |
| Hediyeler | Ödeyenlerin alımlarından ve av/kronikten adsız pay | Alımı ittifaka yansır; adı gösterilmez |
| Yardım | Her yardım aynı yüzdeyi düşürür | Aynı |
| Rütbe | Rütbe katkıyla ve güvenle gelir | Harcama rütbe vermez |
| Ambar | Uzun yoklukta kaynağını korur | Aynı sınır (reaktörle) |

---

## 14. Arayüz notları (sakin lüks)

- İttifak ekranı beş sekmedir: **Üyeler · Teknoloji · Bölge · Dükkân · Hediyeler**. Sohbet ayrı
  paneldir.
- Üye listesi güce göre sıralıdır (kodda); her satırda rütbe rozeti, çevrimiçi noktası, son görülme.
  Harcama, VIP ve hediye bilgisi listede **yoktur**.
- Teknoloji ağacı sakin bir dal çizimidir; öncelikli teknoloji ince bir altın çerçeveyle işaretlenir.
  Bağış düğmesi tek dokunuştur, bedel düğmenin altında küçük yazar.
- Hediyeler sekmesinde açılmamış hediyeler bir raf gibi durur; "Hepsini aç" tek dokunuştur. Konfeti,
  ses patlaması yoktur; yalnızca içerik listesi.
- Bölge haritada ittifak rengiyle %15 saydam dolgudur; sönmüş kuleler gri ve kesik çizgiyle görünür.

---

## 15. Uç durumlar

- **Son üye ayrılırsa** ittifak dağılır, ad ve etiket boşa çıkar (kodda); bölge yapıları 1 saat içinde
  söner ve kalkar, ortak hesap ve İttifak Kredisi kaybolur, ambar bölmeleri postayla iade edilir.
- **Lider ve tüm R4'ler 7 gün girmezse** liderlik en etkin R3'e, o da yoksa en etkin üyeye geçer.
- **Üye sınırı düşerse** (kuleler söner): kimse çıkarılmaz; yeni üye alınamaz.
- **Savaştaki ittifaktan ayrılan** kaptan 24 saat yeni ittifaka giremez ve göç edemez
  ([16](16_kronik_kokenler_profil.md)).
- **Hediye süresi dolarsa** açılmamış hediye kaybolur; 24 saat kala sessiz bir hatırlatma.
- **Aday dönemi** içinde çıkarılan kaptanın Bireysel Kredi'si korunur.
- **Ambar bölmesi doluyken** reaktör yükselirse sınır büyür; düşerse fazlası çekilebilir ama yeni
  yatırım yapılamaz.

---

## 16. Bugün kodda

<!-- URET:tablo:ittifak.kodda -->
| Özellik | Bugün kodda | Hedef | Durum |
| --- | --- | --- | --- |
| Kurma bedeli | 3.000 Titanyum | 3.000 Titanyum | ✅ |
| Ad / etiket | 3–20 / 3–4 karakter, benzersiz | Aynı | ✅ |
| Katılım | Açık · onaylı · kapalı; davet ve başvuru (bellekte, 48 sa) | Açık · onaylı · kapalı | ✅ |
| Üye sınırı | 30 | 30 → 100 (ittifak seviyesi + kuleler) | 🟡 |
| Rütbeler | R5–R1, yetki tablosu, 4 Kurmay koltuğu ve görev etiketi; Aday 48 sa sonra Dalgıç | R5–R1 ve yetki tablosu | ✅ |
| Liderlik devri | Lider devredebilir; ayrılınca en yüksek rütbe (eşitse en eski üye) | + 7 gün girmeyen liderin yerine en etkin R4 | 🟡 |
| Yardım | %1, en az 60 sn, inşaat başına 10 | 10 + ⌊İttifak Merkezi Sv / 5⌋, en çok 20; araştırma ve iyileştirme de | 🟡 |
| Çevrimdışı yardım | Girişte uygulanır (bellekte) | Kalıcı kayıt | 🟡 |
| Sohbet | Dünya + İttifak + Özel (bire bir) · 200 karakter · 2 sn · son 50 | + sabit duyuru, koordinat bağlantısı, ralli kartı | 🟡 |
| Teknoloji, kredi, dükkân, hediye, bölge, savaş, posta | Yok | Bu bölüm | ⬜ |
<!-- /URET:tablo:ittifak.kodda -->

Kod: [25_Heroes_Alliance_Chat.md](../25_Heroes_Alliance_Chat.md) ve `shared/catalog/catalog.json` →
`alliance` (`createCost`, `maxMembers`, `nameMin/Max`, `tagMin/Max`, `maxHelps`, `helpPercent`,
`helpMinSeconds`) ve `chat`.

---

## 17. Uygulama notları

| Değişiklik | Kod |
|---|---|
| Rütbeler | `server/Hadal.Game/Social/SocialHub.cs`: `MemberRecord`'a `Rank` (1–5); `LeaderId` R5'in kaynağı olarak kalır; yetki kontrolü tek bir `AlliancePermissions.Can(rank, action)` tablosundan (`shared/HADAL.Shared/Simulation`) |
| Yeni komutlar | `SetRank`, `TransferLeadership`, `SetJoinMode`, `ApproveApplicant`, `Donate`, `SetTechPriority`, `StartTech`, `StockShop`, `BuyShop`, `OpenGift`, `PlaceStructure`, `DeclareWar`, `ProposeTruce`, `SetAlly`, `SendAllianceMail` |
| Katalog | `catalog.json` → `alliance`: `levels[]` (deneyim, üye, R4, karargâh, kule), `ranks[]`, `tech[]` (dal, maks, taban, etki), `donation`, `shop[]`, `gifts[]`, `structures[]`; `maxMembers` yerine seviye tablosu; `maxHelps` yerine İttifak Merkezi formülü |
| Yardım | `SocialHub.HelpAll` → sınır `10 + ⌊hub/5⌋` (isteyenin İttifak Merkezi); araştırma ve iyileştirme istekleri; çevrimdışı yardımlar ve açık istekler `ISocialStore`'a kalıcı |
| Bölge | `WorldService` (dünya kilidi) yapı yerleşimi ve bağlantı (BFS); `SocialHub` sahiplik; kilit sırası dünya → ittifak merkezi → oturum |
| Hediyeler | Satın alma doğrulaması (Paket G) → `SocialHub.GrantGift(allianceId, tier)`; alıcı adı saklanmaz, yalnızca isteğe bağlı gösterim bayrağı |
| Tablolar | `hadal.alliances` (+ seviye, deneyim, kredi, ortak hesap), `hadal.alliance_members` (+ rütbe, bireysel kredi, bağış hakkı), yeni `hadal.alliance_tech`, `hadal.alliance_gifts`, `hadal.alliance_structures`, `hadal.alliance_storage` |
| Overseer | Teknoloji ve dükkân tabloları Denge sayfasından; ittifak adı/etiketi moderasyonu |

---

## 18. Esin kaynakları

Türün ittifak sistemlerinden alınan **yapılar**: beş kademeli rütbe ve yetki tablosu; bağışla ilerleyen
ittifak teknolojisi ve önerilen teknoloji; kişisel ve ittifak kredisi ayrımı, stoklanan ittifak dükkânı;
üyelerin alımlarından doğan ittifak hediyeleri; kale ve bayraklarla çizilen, bağlı kalması gereken
bölge; ittifak kaynak noktaları ve ortak ambar; savaş bildirisi. Hadal'ın farkı: bağış eşit sayılır,
hediyeler adsızdır, bölge sonar kuleleriyle çizilir, yapılara saldırı herkesin bildiği bir saatte yapılır.

---

## 19. Açık sorular

- **Bağış bedeli** Gelir Saati'ne bağlı; [15](15_ekonomi_akisi.md)'teki harcama tablosuna "ittifak
  bağışı" satırı eklenmeli (tipik günlük gelirin ~%10'u).
- **Takviye kapasitesi** alıcının sefer kapasitesinin katı olarak tanımlandı ([09](09_dunya_haritasi_ve_pvp.md)
  §8.3); 07'deki sefer kapasitesiyle sayıya çevrilmeli.
- **Hediye içerikleri** paket kademelerine bağlanmalı ([12](12_syndicate.md)); Derin Hazine'nin her
  2.000 puanda dolması büyük ittifaklarda çok sık olabilir, puan ittifak üye sayısıyla ölçeklenebilir.
- **İttifak teknolojisinin hız etkileri** (inşa %5, araştırma %5) tempo simülasyonunun hız bütçesine
  dahil mi? `f2p_profilleri.json` hız tavanı %50; ittifak payı orada ayrı gösterilmeli.
- **Av Hediyesi** iki kaynaktan doğuyor (Leviathan Avı etkinliği ve harita yuvaları); [15](15_ekonomi_akisi.md)
  yalnız etkinlik payını (günde ~0,1 GS) sayıyor. Yuva hediyeleri ittifak başına günde 3 ile sınırlı;
  bütçeye eklenmeli.
- **100 üyelik ittifak** sohbet ve yardım akışını boğar mı? Alt kanallar (kanat sohbeti) gerekebilir.
- **Kule geliri** (saatte 1.500 Ti) ittifak ekonomisinin tek kaynak musluğu; enflasyonu izlemek için
  Overseer'da ortak hesap göstergesi olmalı.
