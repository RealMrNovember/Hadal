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

1. **Seviye:** Her bina 1. seviyede başlar, `maxLevel`'a kadar yükselir (50; kubbeler 5).
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

9. **Reaktör seviyesi binaları açar:** Her binanın bir `requiredReactorLevel` değeri vardır (ör. Hidroponik Çiftlik reaktör 3, Mega Kubbe reaktör 6). İnşa menüsü kilitli binaları ve gereken seviyeyi gösterir.
10. **Adet sınırı:** Her binanın üste kadar kurulabilecek bir `maxCount` değeri vardır (ör. 4 Oksijen Tesisi, 1 Araştırma Merkezi, 1 Mega Kubbe). İnşası süren binalar da sayılır.
11. **Halkalar:** İç halkalar (0–3) sabittir. Halka 1 üretim, halka 2 araştırma ve altyapı, halka 3 askeri binalar içindir; her birinde 3 yuva vardır. Halka 4 ve ötesi **evrensel**dir: her bina kurulabilir ve dışa doğru yuva sayısı artar (6, 8, 10 … en fazla 16). Üretim binaları iç halkaya sığmazsa dış halkalara kurulur.
12. **Halka açma:** Oyun 0–4. halkalar açık başlar. 5–12. halkalar sırayla açılır (`UnlockRing`). Her halkanın bir reaktör seviyesi şartı ve kaynak bedeli vardır (`ringUnlocks`). **7. halka ve ötesi için Mega Kubbe gerekir:** üs yeni bir kubbeye genişlemeden dış halkalar açılamaz. Halka açmak inşaatçıyı meşgul etmez ve anında olur.
13. **Kubbeler:** Mega Kubbe (yaşam alanı: oksijen üretir, dış halkaları açar) ve Tarım Kubbesi (yüksek gıda ve biyokütle) dış halkalara kurulan, en fazla 5. seviyeye çıkan ve üste birer tane olabilen büyük yapılardır. Savaş ve Bilim kubbeleri, savaş (Phase 9) ve araştırma sistemleriyle gelecek.
14. **Kaldırma (`RemoveBuilding`):**
    - Çekirdek Reaktör kaldırılamaz.
    - Boştaki bina kaldırılınca, o seviyeye kadar harcanan toplam bedelin **%50'si** iade edilir (depo sınırı kadar).
    - İlk inşaatı süren bina kaldırılırsa inşaat **iptal** olur ve bedelin **tamamı** iade edilir.
    - Yükseltmesi süren bina kaldırılamaz; önce yükseltmenin bitmesi gerekir.

## Denge (ilk gerçek değerler)

Bir simülasyon tiki 1 saniyedir; değerler 1. seviye içindir ve her seviye üretimi %25 artırır. Binalar 50 seviyelidir (kubbeler 5). Maliyet seviye başına ×1,09, süre ×1,12 artar: 50. seviye, 1. seviyenin yaklaşık 68 katı tutar ve 260 katı sürer (reaktör ≈ 2 saat, Denizaltı İskelesi ≈ 6,5 saat, en pahalısı İttifak Merkezi ≈ 20.500 Titanyum). Değerler `shared/catalog/catalog.json` içindedir ve Overseer panelinin "Denge · Seviyeler" sayfasından canlıda değiştirilebilir.

| Bina | Reaktör | En fazla | Üretim / dk | Tüketim / dk | 1. sv. bedeli | 1. sv. süresi |
|---|---|---|---|---|---|---|
| Çekirdek Reaktör | — | 1 | 480 Enerji | — | (yükseltme) 100 Ti | 30 sn |
| Oksijen Tesisi | 1 | 4 | 180 Oksijen | 60 Enerji | 60 Ti | 10 sn |
| Gıda Laboratuvarı | 1 | 4 | 120 Gıda | 60 Enerji | 50 Ti | 10 sn |
| Biyo Laboratuvar | 2 | 3 | 120 Biyokütle | 60 Enerji | 70 Ti | 15 sn |
| Dron Fabrikası | 2 | 3 | 120 Titanyum | 60 Enerji | 120 Ti, 20 Biyokütle | 30 sn |
| Süs Kulesi | 2 | 6 | — | — | 40 Ti | 10 sn |
| Hidroponik Çiftlik | 3 | 3 | 240 Gıda | 60 Biyokütle, 60 Enerji | 140 Ti, 40 Biyokütle | 40 sn |
| Araştırma Merkezi | 3 | 1 | — | 60 Enerji | 180 Ti, 40 Biyokütle | 60 sn |
| Denizaltı İskelesi | 4 | 1 | — | 60 Enerji | 260 Ti, 60 Biyokütle | 90 sn |
| Radar Merkezi | 4 | 1 | — | 60 Enerji | 160 Ti | 45 sn |
| Askeri Kubbe | 5 | 2 | — | 60 Gıda | 240 Ti | 60 sn |
| Kalkan Jeneratörü | 5 | 1 | — | 120 Enerji | 220 Ti, 120 Enerji | 60 sn |
| Mega Kubbe | 6 | 1 | 240 Oksijen | 120 Enerji | 900 Ti, 300 Biyokütle | 5 dk |
| İttifak Merkezi | 6 | 1 | — | — | 300 Ti | 2 dk |
| Tarım Kubbesi | 7 | 1 | 480 Gıda, 180 Biyokütle | 120 Enerji | 700 Ti, 250 Biyokütle | 4 dk |

Halka bedelleri: 5. halka reaktör 3 + 400 Ti, 100 Biyokütle; 6. halka reaktör 4 + 800/200; 7. halka reaktör 6 + Mega Kubbe + 1.500/400; sonrası her halkada reaktör bir seviye daha ve artan bedel (12. halka: reaktör 10, 9.500 Ti, 2.800 Biyokütle).

Başlangıç: 600 Oksijen, 400 Enerji, 400 Gıda, 200 Biyokütle, 500 Titanyum. Depo sınırları: Oksijen 6.000, Enerji 10.000, Gıda 5.000, Biyokütle 6.000, Titanyum 25.000, Hadalit 1.000; her binanın 50. seviye bedeli bu sınırların içinde kalır (depo yükseltmeleri Phase 4'te).

Enerji açığı bu sürümde yalnızca stoğu eritir. Enerji yetmezse binaların yavaşlaması Phase 4'ün konusudur.

## Otorite (v3)

Tüm kurallar sunucudadır. İstemci yalnızca gösterir ve komut gönderir:

- `PlaceBuilding` (yeni inşa)
- `UpgradeBuilding` (yükselt)
- `FinishConstruction` (ücretsiz bitir)
- `RemoveBuilding` (kaldır / inşaatı iptal et)
- `UnlockRing` (sonraki halkayı aç)

Ret nedenleri `CommandAckDto.Reason` ile gelir ve oyuncuya anlaşılır bir metin olarak gösterilir: inşaatçı meşgul, kaynak yetersiz, reaktör seviyesi düşük, en yüksek seviye, henüz bedava değil, adet sınırı doldu, Mega Kubbe gerekli, reaktör kaldırılamaz.

Açık halka sayısı anlık görüntüde ve `base:layout` değişikliğinde (`BaseLayoutDto`) gelir.

Geri sayım için sunucu `ConstructionRemainingMs` ve `ConstructionTotalMs` gönderir. İstemci kendi saatine güvenmez; her güncellemede kalan süreyi sunucudan alır.

## Sonraki Adımlar

- **Mürettebat talebi (Phase 4):** Binalar mürettebat ister, mürettebat oksijen ve yiyecek tüketir. Talep odaklı ekonominin çekirdeği budur.
- **Görevler:** İlk 10 dakikayı yönlendiren görev zinciri (reaktörü yükselt → oksijen tesisi kur → …).
- **İkinci inşaatçı, hızlandırıcılar:** Quiet Luxury kuralına uygun premium (`15`).
- **Submarine Dock:** Denizaltı inşa kuyruğu, ardından keşifler (`08`).
