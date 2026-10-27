# Veri şemaları (yazarlar için)

`docs/rehber/veri/*.json` kılavuzun **tek veri kaynağıdır**. Bölüm metinlerindeki her sayı ya bu
dosyalardan bir işaretle gelir ya da üretilen tablolarda görünür; elle yazılmış rakam, veriyle
çelişmeye başladığı an hatadır. Üretici: `python tools/rehber/uret.py` (kontrol: `--kontrol`).

Genel kurallar:
- Kimlikler (`id`) İngilizce `snake_case`; oyuncuya görünen her metin Türkçe.
- Kodda var olan kimlikler (bina, kahraman, araştırma) aynen korunur.
- Başka oyunların isimleri, metinleri, rakamları kopyalanmaz (`yasakli_isimler.json` kontrol edilir).
- "Gerçek / kurgu" etiketi kullanılmaz. Gerçek derin deniz verisi yalnızca `bilim.json`'dan alınır.
- Durum alanı: `"durum": "kodda"` (oyunda var) ya da `"tasarim"` (yok).
- Sayılar Türkçe biçimlenir (üretici yapar): 1.250.000 · 2,5 · %4 · 3 g 4 sa.
- JSON'u kompakt tut: kısa diziler tek satırda.

## İşaretler (Markdown içinde)

| İşaret | Ne yapar |
|---|---|
| `<!-- URET:ad --><!-- /URET:ad -->` | Arası her çalıştırmada yeniden üretilir. Ad listesi aşağıda. |
| `<!-- URET:tablo:dosya.yol --><!-- /URET:tablo:dosya.yol -->` | `veri/dosya.json` içindeki bir nesne listesini tabloya döker. |
| `<!--v:veri.dosya.yol-->…<!--/v-->` | Ham veriden tek değer. |
| `<!--v:hesap.yol\|bicim-->…<!--/v-->` | Hesaplanmış değer. `bicim`: `sayi`, `tam`, `sure`, `yuzde`, `gun`, `kisa`, `para`, `ham`. |

Yol parçaları nokta ile ayrılır; listelerde eleman `id` ile ya da sıra numarasıyla seçilir.
Örnekler: `veri.vip.seviyeler.7.puan`, `hesap.bina.core_reactor.50.bedel.Titanium`,
`hesap.bina.core_reactor.toplam_sure|sure`, `hesap.sim.f2p.gun_50|gun`.

`tablo:` için sütunlar: düğüm `{"sutunlar": [...], "satirlar": [...]}` biçimindeyse oradan; değilse
aynı üst nesnedeki `"<anahtar>_sutunlar"` alanından; o da yoksa ilk satırın anahtarlarından. Sütun:
`["alan", "Başlık"]` ya da `["alan", "Başlık", "bicim"]`.

Hazır bloklar: `bina-ozet`, `reaktor-capalari`, `reaktor-on-kosul`, `halka-acilislari`, `slot-tablosu`,
`tavanlar`, `enerji-dengesi`, `guc-tablosu`, `egriler`, `kaynak-tablosu`, `v8-karsilastirma`, `sim-ozet`,
`kilometre-taslari`, `arastirma-ozet`, `kahraman-ozet`, `kahraman-egitim`, `evrim-ozet`, `sozluk-binalar`,
`sozluk-kahramanlar`, `sozluk-arastirma`.

`hesap` ağacı: `bina.<id>.<seviye>` (bedel, sure, guc, uretim, tuketim, reaktor, kumulatif_*),
`bina.<id>.toplam_bedel|toplam_sure|toplam_guc`, `sim.<profil>.gun_50`, `sim.<profil>.seviye.<L>`,
`tavan.<Kaynak>.<R>`, `slot.<R>`, `enerji.<R>`, `sayilar.*`, `arastirma.<id>.<seviye>`,
`kahraman.<id>.guc_50`.

## Temel dosyalar (ana yazarın; değiştirme, "Açık sorular"a yaz)

`kaynaklar`, `egriler`, `ilerleme`, `f2p_profilleri`, `binalar`. Eğri ve bina alanları dosyaların
`_not` alanlarında anlatılır. Önemli eğriler: `reaktor_bedel` (Titanyum), `reaktor_sure` (saniye),
`alasim` (Süperalaşım), `uretim`, `guc`.

## kahramanlar.json

```json
{
  "surum": "rehber-1",
  "nadirlikler": [{"id": "common", "ad": "Sıradan", "agirlik": 65, "taban": {"atk": 100, "def": 80, "hp": 1000},
                   "egitim_carpani": 1.0, "cagri_parca": 10, "kopya_parca": 10, "v8_guc": 100}],
  "siniflar": [{"id": "Commander", "ad": "Komutan", "carpan": {"atk": 0.85, "def": 1.2, "hp": 1.2}, "birlik": "armored"}],
  "birlik_turleri": [{"id": "armored", "ad": "Zırhlı Dalgıç"}],
  "fraksiyonlar": [{"id": "AtlanticUnion", "ad": "Atlantik Birliği", "aciklama": "..."}],
  "uzmanliklar": [{"id": "liderlik", "ad": "Liderlik", "aciklama": "..."}],
  "stat_formulu": {"seviye": 0.08, "yildiz": 0.15},
  "egitim": {"Food": 0.03, "Biomass": 0.01},
  "kahramanlar": [{
    "id": "mara", "ad": "Komutan Elias Rourke", "unvan": "Son konvoyun komutanı",
    "sinif": "Commander", "fraksiyon": "AtlanticUnion", "nadirlik": "common", "nesil": 1, "durum": "kodda",
    "biyografi": "...", "kazanim": "Başlangıç kahramanı",
    "murettebat": {"etki": "buildTime", "taban": -3, "seviye_onda": -2, "yildiz": -2},
    "yetenekler": [{"ad": "...", "tur": "aktif", "tur_ad": "Aktif (öfke)", "yildiz": 1,
                    "aciklama": "... {0} ...", "degerler": [10, 13, 16, 20, 25]}],
    "agaclar": ["liderlik", "zirhli", "savunma"],
    "notlar": "..."
  }]
}
```
`murettebat` kodda olanlar için catalog v8 değerleriyle aynı kalır. `{0}` yetenek seviyesi 1–5
değerleriyle doldurulur. İstatistik = nadirlik tabanı × sınıf çarpanı × (1 + 0,08(L−1)) × (1 + 0,15(★−1)).

## arastirma.json

```json
{
  "surum": "rehber-1",
  "varsayilan_bedel": [{"kaynak": "Titanium", "olcek": 0.25}, {"kaynak": "Biomass", "olcek": 0.09, "baslangic": 2}],
  "sure_olcek": 0.35, "guc_agirlik": 0.4,
  "dallar": [{"id": "development", "ad": "Gelişim", "kuyruk_ad": "Araştırma Merkezi", "acilis": "Araştırma Merkezi 1",
              "aciklama": "..."}],
  "teknolojiler": [{
    "id": "construction_mastery", "dal": "development", "ad": "İnşaat Ustalığı", "durum": "kodda",
    "maks": 10, "etki": "buildTime", "etki_ad": "İnşa hızı", "seviye_basi": 3, "birim": "%",
    "min_lab": 1, "adim": 2, "bedel_olcek": 1.0, "sure_olcek": 1.0,
    "gerekli": [{"id": "...", "seviye": 5}], "aciklama": "..."
  }]
}
```
Seviye ℓ için gereken Araştırma Merkezi seviyesi r = min_lab + adim × (ℓ − 1) (50'de kırpılır).
Maliyet = olcek × bedel_olcek × reaktor_bedel(r); süre = 0,35 × sure_olcek × reaktor_sure(r).
Teknoloji kendi `bedel` listesini verirse (ör. Süperalaşım: `{"kaynak": "Alloy", "egri": "alasim", "olcek": 0.3, "baslangic": 31}`) varsayılan yerine o kullanılır.

## evrim.json

```json
{
  "surum": "rehber-1",
  "katmanlar": [{"id": "yama", "ad": "Yama", "seviyeler": "1–9", "tema": "...", "malzeme": "..."}],
  "ek_moduller": [{"id": "...", "ad": "...", "seviye": 5, "aciklama": "..."}],
  "binalar": {"core_reactor": {"adlar": ["Yama Reaktörü", "...6 ad"], "gorunum": ["...6 kısa tarif"], "sanat": "A"}}
}
```
Kubbeler (5 seviye) için 5 ad ve görünüm.

## Serbest tablolar (birlikler, denizaltilar, vip, syndicate, hizlandiricilar, oduller, etkinlikler)

Bu dosyaların yapısı bölüm yazarına bırakılmıştır, tek koşul: bölümde gösterilecek her liste
`URET:tablo:` ile basılabilecek nesne listesi olsun ve yanında `<anahtar>_sutunlar` tanımı bulunsun.
`oduller.json` ek olarak F2P gelir özetini taşır (kontrol bunu profille karşılaştırır):

```json
"f2p_gelir_ozeti": {"titanyum_saat": [[10, 9000], [20, 38000], [30, 90000], [40, 170000], [50, 300000]],
                    "alasim_gun": [[0, 40], [90, 90], [180, 140], [337, 220]]}
```
