# 24 — Building Functions (Bina İşlevleri)

**Document:** Game Design · HADAL
**Language:** TR (canonical design voice)
**Status:** Tasarım. Kurallar `23_Base_Management.md` üzerine kurulur.

---

## Neden

Bugün her binaya dokununca yalnızca **Yükselt** ve **Kaldır** çıkıyor. Last Asylum: Plague'de (referans oyun) binaların çoğunun kendi ekranı vardır. Oyuncu orada bir şey **yapar**: araştırır, eğitir, iyileştirir, üretir, toplar, işe alır. Seviye atlamak yalnızca bu işlevi büyütür. HADAL'da da her bina, yükseltmenin yanında kendine ait bir **işlev düğmesi** taşımalı.

## Referans: Last Asylum: Plague (2026-10 araştırması)

Kaynaklar: fan wiki bina listesi (46 bina), Sanctuary/araştırma rehberleri. Sayılar sürüme göre değişebilir; amaç kalıpları almak.

| Kalıp | Last Asylum'daki karşılığı | Ne öğretiyor |
|---|---|---|
| Karargâh kapısı | **Sanctuary** seviyesi yeni binaları açar, diğer binaların seviyesini sınırlar | HADAL'da zaten var (Çekirdek Reaktör) |
| Üretim + depo | Farm/Lumberyard/Herb Garden (en fazla 4), "saatlik üretim, depolama süresi sınırı"; Granary/Depot baskında stoğun bir kısmını saklar | Üretim binasına dokunup **toplamak** ve depo binalarının ayrı bir rolü olması |
| Araştırma | **Research Lab**: 13+ ağaç (Gelişim, Ekonomi, Askeri, Asker, Taktik, İttifak, Kervan, kahraman fraksiyonları). Gelişim: inşaat hızı (−%5/düğüm), araştırma hızı (+%10/düğüm), iyileştirme, kapasite. Ekonomi: üretim, toplama hızı ve **ekstra Herb Garden hakkı** (yalnızca buradan). **2nd Workbench** ikinci araştırma kuyruğu açar | Araştırma ayrı bir kuyruktur; düğümler seviyelidir, ön koşulları vardır; bazı düğümler bina sınırını artırır |
| Atölyeler | Antitoxin/Smelting/Weaving Workshop: saatlik ikincil malzeme üretir (en fazla 5). Gear/Epigraph Workshop: sipariş üzerine eşya üretir | "Ham kaynak → işlenmiş malzeme" zinciri ve üretim siparişleri |
| Askeri | Barracks (birlik kapasitesi), Training Grounds (eğitim kapasitesi ve seviyesi), Infirmary (yaralıları iyileştirir), Soldier's Rest (ölenlerin bir kısmını geri getirir), Walls | Eğit / iyileştir / savun döngüsü |
| Keşif | Falcon Tower dünya haritasını ve görevleri açar; Squad 1–4 yürüyüş kuyruğu ekler; Scout Squad; Watchtower bilgini gizler | Sefer kuyruğu ve keşif görevleri |
| İnsanlar | Residence (hayatta kalanlar paneli): her hayatta kalanın üretim özelliği vardır, binalara **atanır**. Tavern kahraman/hayatta kalan işe alır | Mürettebat atama: binayı insanlar çalıştırır |
| Hız | Builder's Hut: ücretsiz hızlandırma süresi, ek inşa kuyruğu | İkinci inşaatçı |
| Sosyal | Alliance Hall yardım sayısı/süresi; Stable kervanlar; Nomad Trader takas; Arena PvP | Sonraki fazlar |

## HADAL Karşılıkları

Her bina panelinde: başlık, seviye, durum; altında **işlev kartı** (bina türüne göre), sonra **Yükselt**, en altta **Kaldır**.

| Bina | İşlev (dokununca) | Ne yapar | Faz |
|---|---|---|---|
| **Çekirdek Reaktör** | **Aşırı Yük** | 1 saat enerji üretimi +%50; bekleme süresi 8 saat. Panel bir sonraki reaktör seviyesinde açılacak binaları listeler | 3.6 |
| **Oksijen Tesisi, Gıda Lab., Biyo Lab., Hidroponik Çiftlik, Dron Fabrikası** | **Mürettebat ata** + **Takviye** | Her binada 1–3 mürettebat yuvası; atanan her uzman üretimi artırır. Takviye: 30 dk %30 üretim, bedeli Enerji | 4 (mürettebat), 3.6 (takviye) |
| **Araştırma Merkezi** | **Araştır** | Araştırma ağacı (aşağıda). Ayrı bir kuyruk: inşaatla aynı anda yürür | **3.6** |
| **Dron Fabrikası** | **Kurtarma görevi** | Dronları enkaz alanlarına yollar: 15 dk / 1 saat / 4 saatlik görevler, titanyum ve nadiren hadalit getirir (Falcon Tower görevleri gibi) | 3.6 |
| **Denizaltı İskelesi** | **Denizaltılar** | Denizaltı inşa kuyruğu, filo, keşfe gönderme (`08`) | 6–7 |
| **Askeri Kubbe** | **Eğit** | Savunma birlikleri eğitimi (Barracks + Training Grounds) | 9 |
| **Kalkan Jeneratörü** | **Kalkan** | Üs savunma gücü; basınç olaylarına direnç (Walls karşılığı) | 4 / 9 |
| **Radar Merkezi** | **Tara** | Yaklaşan tehditleri ve çevredeki sinyalleri gösterir (Watchtower/Scout) | 7 |
| **Mega Kubbe** | **Yaşam alanı** | Mürettebat kapasitesi (Residence); 7. halka ve ötesini açar | 4 |
| **Tarım Kubbesi** | **Hasat** | Büyük üretim + günlük hasat ödülü | 4 |
| **İttifak Merkezi** | **İttifak** | İttifak yardımı, kervan (Alliance Hall/Stable) | 11 |
| **Süs Kulesi** | — | Yalnızca görsel; ileride üs gücüne küçük katkı | — |

Henüz fazı gelmemiş işlevler panelde **kilitli kart** olarak görünür ("Denizaltı keşifleri yakında"); boş bırakılmaz.

## Araştırma Merkezi (Phase 3.6, ilk uygulanacak)

**Kurallar**

1. Araştırma bir **düğümün** sonraki seviyesini satın almaktır: bedel (kaynak) + süre. Süre sunucu saatiyle işler, çevrimdışı da biter.
2. **Ayrı kuyruk:** aynı anda 1 araştırma (inşaatçıdan bağımsız). Bir düğüm ikinci araştırma kuyruğunu açar (2nd Workbench).
3. **Ön koşul:** bir düğüm, başka bir düğümün belirli seviyesini ve/veya Araştırma Merkezi seviyesini ister.
4. **Ücretsiz bitirme** inşaattaki gibi son 5 dakikada.
5. Etkiler sunucuda uygulanır ve oyuncunun durumuna kaydedilir; istemci yalnızca gösterir.

**Ağaçlar ve ilk düğümler** (seviye başına etki; en yüksek seviye parantezde)

*Gelişim*
- İnşaat Ustalığı: inşaat süresi −%3 (10)
- Araştırma Yöntemleri: araştırma süresi −%5 (10)
- İkinci İnşaatçı: aynı anda 2 inşaat (1) — ön koşul: İnşaat Ustalığı 5, Merkez 5
- İkinci Tezgâh: aynı anda 2 araştırma (1) — ön koşul: Araştırma Yöntemleri 5, Merkez 8

*Ekonomi*
- Oksijen Verimi / Gıda Verimi / Biyokütle Verimi / Titanyum Geri Kazanımı: ilgili üretim +%4 (10)
- Depo Mühendisliği: tüm depo sınırları +%5 (10)
- Ek Oksijen Hattı: Oksijen Tesisi sınırı +1 (2) — Last Asylum'daki "ekstra Herb Garden" düğümü gibi
- Ek Dron Hattı: Dron Fabrikası sınırı +1 (2)

*Hayatta Kalma* (Phase 4 ile anlam kazanır)
- Hava Geri Dönüşümü: oksijen tüketimi −%3 (10)
- Basınç Gövdesi: basınç olay hasarı −%5 (10)

*Derinlik* (Phase 6–7 ile)
- Gövde Alaşımı, Sonar Menzili, Sefer Hızı — kilitli, "denizaltılarla gelir".

## Teknik Not

- Yüzde etkilerin işe yaraması için üretimin **kesirli** hesaplanması gerekir. Bugün üretim tik başına tam sayıdır ve küçük binalarda seviye artışı yuvarlanıp kayboluyor (bilinen hata). Araştırmadan önce üretim, birikimli kesir (binde birlik hassasiyet) ile hesaplanacak.
- Yeni komutlar: `StartResearch`, `FinishResearch`; replikasyon: `research:` varlıkları (`ResearchStateDto`).
- Katalog: `research` bölümü (ağaç, düğüm, seviye bedeli/süresi, etki türü ve değeri, ön koşul). Overseer "Denge" sayfasında düzenlenebilir.
- Etki türleri (`effect`): `buildTime`, `researchTime`, `production:<Kaynak>`, `capacity:<Kaynak|All>`, `limit:<binaId>`, `builderSlots`, `researchSlots`.
