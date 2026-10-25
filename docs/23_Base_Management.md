# 23 — Base Management (Üs Yönetimi)

**Document:** Game Design · HADAL  
**Language:** TR (canonical design voice)  
**Status:** Uygulanan ilk sürüm (Phase 3 çekirdeği). Sayılar `shared/catalog/catalog.json` içindedir; bu belge kuralları tanımlar.

---

## Senaryo: Üs ve Denizaltılar

- **Üs gerçektir ve sabittir.** Hadal Zone'da, 11.000 metre derinlikteki deniz tabanında kurulu bir kubbe üssüdür. Merkezde **Core Reactor**, çevresinde dairesel halkalar vardır (`05`). Oyuncunun yuvası ve kimliği burasıdır.
- **Denizaltılar üssün araçlarıdır.** **Submarine Dock**'ta inşa edilir ve donatılır. 3 kahramanla karanlık bölgelere **keşfe gönderilir** (`08`), sonra ganimetle üsse döner.
- Üs büyüdükçe yeni kubbeler açılır (Mega, Agriculture, War, Science). Üs yerinde kalır, ama giderek büyür.

## Üs Ekranı (Last Asylum / Last Fortress düzeni)

| Alan | Ne gösterir |
|---|---|
| Üst şerit | Kaynaklar ve üretim hızları (oksijen her zaman ilk sırada) |
| Sağ üst | Profil ve hesap menüsü |
| Sahne | Dairesel üs, binalar, mürettebat, çevredeki yaşam |
| Boş yuva | Halkalar üzerinde parlayan yuva işareti. Dokununca o yuvaya uygun **İnşa menüsü** açılır |
| Bina | Dokununca **Bina paneli** açılır: seviye, üretim, sonraki seviye, yükseltme maliyeti ve süresi |
| İnşaat | Binanın üstünde ilerleme çubuğu ve geri sayım. Son 5 dakikada **Ücretsiz Bitir** düğmesi çıkar |

Dokunma ile kaydırma birbirinden ayrılır: kısa ve hareketsiz bir dokunuş seçim yapar, sürükleme kamerayı kaydırır.

## Kurallar

1. **Seviye:** Her bina 1. seviyede başlar, `maxLevel`'a kadar yükselir (ilk sürümde 10).
2. **Reaktör kapısı:** Core Reactor üssün "karargâhı"dır. Hiçbir bina reaktörün seviyesini geçemez. Oyuncu, ilerlemek için reaktörü yükseltmeye yönlendirilir.
3. **Maliyet:** Hedef seviye L için `taban maliyet × costGrowth^(L-1)`. Yeni inşa, 1. seviyenin maliyetidir.
4. **Süre:** `buildSeconds × timeGrowth^(L-1)`. İlk seviyeler saniyeler sürer (ilk 60 saniyede ödül hissi), sonraki seviyeler uzar.
5. **İnşaatçı:** Aynı anda **1 inşaat** yürür (yeni bina ya da yükseltme). İleride Drone Factory ve premium ile ikinci inşaatçı eklenebilir.
6. **Ücretsiz bitir:** Kalan süre `freeFinishSeconds` (5 dk) veya altındaysa inşaat bedavaya anında biter. Bu, rahat tempo ilkesidir (`research/LastAsylum-Benchmark.md`): oyuncuyu küçük beklemelerle cezalandırma.
7. **Üretim:**
   - Yeni inşa edilen bina bitene kadar üretmez.
   - Yükseltilen bina, yükseltme sürerken **eski seviyesinde üretmeye devam eder**. Böylece yükseltme kayıp gibi hissettirmez.
   - Seviye başına üretim artışı `ProductionFormulas.ScaleByLevel` ile hesaplanır.
8. **Çevrimdışı:** İnşaat, oyuncu yokken de sunucu saatine göre biter. Biten binanın üretimi bitiş anından itibaren sayılır; oyuncu dönünce aradaki süre bölünerek hesaplanır.

## Otorite (v3)

Tüm kurallar sunucudadır. İstemci yalnızca gösterir ve komut gönderir:

- `PlaceBuilding` (yeni inşa)
- `UpgradeBuilding` (yükselt)
- `FinishConstruction` (ücretsiz bitir)

Ret nedenleri `CommandAckDto.Reason` ile gelir ve oyuncuya anlaşılır bir metin olarak gösterilir: inşaatçı meşgul, kaynak yetersiz, reaktör seviyesi düşük, en yüksek seviye, henüz bedava değil.

Geri sayım için sunucu `ConstructionRemainingMs` ve `ConstructionTotalMs` gönderir. İstemci kendi saatine güvenmez; her güncellemede kalan süreyi sunucudan alır.

## Sonraki Adımlar

- **Mürettebat talebi (Phase 4):** Binalar mürettebat ister, mürettebat oksijen ve yiyecek tüketir. Talep odaklı ekonominin çekirdeği budur.
- **Görevler:** İlk 10 dakikayı yönlendiren görev zinciri (reaktörü yükselt → oksijen tesisi kur → …).
- **İkinci inşaatçı, hızlandırıcılar:** Quiet Luxury kuralına uygun premium (`15`).
- **Submarine Dock:** Denizaltı inşa kuyruğu, ardından keşifler (`08`).
