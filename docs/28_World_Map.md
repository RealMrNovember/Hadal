# 28 — Dış Dünya Haritası

**Document:** Game Design · HADAL
**Language:** TR (canonical design voice)
**Status:** İlk sürüm uygulandı (2026-10-26, istemci 1.6.0). Kurallar `shared/catalog/catalog.json` → `world`.

---

## İki mekân

- **Üs içi:** oyuncunun kendi dairesel üssü (bugüne kadarki ekran).
- **Dış dünya:** bütün oyuncuların üslerinin durduğu deniz tabanı haritası. Alt çubuktaki **Bölge** düğmesi haritayı açar; haritadayken aynı düğme **Üs** olur ve eve döndürür.

## Harita

- 200 × 200 karelik tek bir bölge. Her üsün sabit bir karesi vardır; oyuncu taşınana kadar (ileride: ışınlanma eşyası) orada kalır.
- Yeni üsler merkezden dışa doğru kare bir sarmal üzerinde, 3 kare arayla yerleşir: yeni oyuncular birbirine ve kalabalığa yakın başlar. Harita gelmeden önce var olan oyuncular sunucu açılırken yerleştirildi.
- Her üsün üstünde bir **isim levhası**: seviye (Çekirdek Reaktör) rozeti, `[ETİKET] İsim`, çevrimiçiyse yeşil nokta. Kendi üssün camgöbeği, ittifak arkadaşların mavi, diğerleri beyaz.
- Üs büyüdükçe haritada da büyür: reaktör; Sv. 3'te oksijen tesisi, Sv. 5'te mega kubbe, Sv. 8'de kalkan jeneratörü, Sv. 12'de radar.
- Levhaya dokununca kart: isim, ittifak, seviye, güç, koordinat, çevrimiçi durumu.
- Kamera: tek parmakla kaydır, iki parmakla yakınlaş/uzaklaş. Alttaki kutu bakılan koordinatı gösterir; **Üssüm** kendi üssüne döner. Deniz tabanı, kayalar, yosun ve kristaller her yerde aynı desenle tekrar eder (aynı yer hep aynı görünür).
- Haritadayken üs kamerası ve üs dokunuşları durur; menü üzerinde başlayan hareket haritayı oynatmaz.

## Sonraki adımlar

- Haritada kaynak noktaları, enkazlar ve canavarlar (Phase 7 seferler, Phase 9 savaş).
- Üsse ziyaret, saldırı ve keşif (Phase 10, 13).
- Mini harita ve koordinata git (EmpireWars'taki mini harita bunun için incelendi).

## Teknik Not

- Sunucu: `WorldService` (`Hadal.Game/World`), tablo `hadal.world_bases (player_id, x, y, name, level, power)`. Kilit sırası dünya → ittifak merkezi → oturum.
- Komut `ViewWorld` (29) {X, Y} (ikisi de -1: kendi üssünün çevresi); yanıt `world:view` (`WorldViewDto`), merkezden 14 kare yarıçaptaki üsler. Çevrimiçi oyuncuların adı, seviyesi ve gücü canlı oturumdan gelir.
- İstemci: `WorldMapView` (ayrı kamera, üssün 10.000 m uzağında kurulur), `GameMode.InWorld` bayrağı üs girişini susturur. Modeller sahnedeki `CharacterLibrary` üzerinden (`env_*`, `bld_*`).
