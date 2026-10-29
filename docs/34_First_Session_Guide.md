# 34 — İlk Oturum Rehberi

**Document:** Game Design · HADAL
**Language:** TR (canonical design voice)
**Status:** İlk sürüm uygulandı (2026-10-29, istemci 0.9.3, katalog tasarımı v11). İçerik `shared/catalog/catalog.json` → `quests.onboarding`.

---

## Neden

Araştırma §16'nın ilk sorusu: "Oyuncu ilk 10 dakikada ne yaptığını anlıyor mu?" Yeni hesap, dışarıdan yardım almadan 15–20 dakikada ilk kilometre taşına (ilk toplama seferi) ulaşmalı; ilk günün sonunda da bir ittifaka katılıp ilk yardımını almış olmalı (§1.1 üç zaman ölçeği, §6.3 etik günlük dönüş: baskı yok, kaçırılan adım kaybolmaz).

## Adımlar

Hikâye görevlerinden ayrı, ama aynı görev sistemiyle çalışan kısa bir zincir. Her adımın ödülü doğrudan cüzdana girer (kapsül değil), böylece bir sonraki adımı açar. İlk inşaatlar 5 dakikalık ücretsiz bitirme penceresinde kaldığı için hiçbir adım zamanlayıcıda takılmaz; tek bekleme seferlerin yolculuğudur.

| # | Adım | Bitiren olay | Ödül |
|---|---|---|---|
| 1 | Reaktörü Sv. 2 yap | bina seviyesi | 200 Titanyum |
| 2 | Oksijen Tesisi kur (ilk üretim) | bina sayısı | Lyra Moss ×10 kart, 100 Gıda |
| 3 | 2 kahramana sahip ol (Lyra'yı çağır) | kahraman sayısı | 200 Titanyum |
| 4 | Oksijen Tesisi Sv. 2 | bina seviyesi | 150 Titanyum, 50 Biyokütle |
| 5 | Reaktör Sv. 3 | bina seviyesi | 200 Titanyum |
| 6 | 2. halkayı aç | açık halka sayısı | 150 Titanyum, 50 Biyokütle |
| 7 | Dron Fabrikası kur | bina sayısı | 100 Titanyum |
| 8 | Dron Fabrikası Sv. 3 | bina seviyesi | 300 Titanyum, 150 Biyokütle |
| 9 | Reaktör Sv. 4 (ilk sefer kuyruğu) | bina seviyesi | 300 Zırhlı T1 asker (doğrudan orduya), 500 Gıda |
| 10 | İlk canavarı yen | `monster` sayacı (savaş kazanılınca) | Bronz Sandık, 20 Hadalit |
| 11 | Çantadan bir eşya aç (ilk ödül) | `open` sayacı | 500 Titanyum, 300 Gıda |
| 12 | İlk toplama seferi | `gather` sayacı (sefer başlayınca) | Gümüş Sandık, 30 Hadalit |
| 13 | İttifaka katıl | ittifak üyeliği | 1.000 Titanyum, 50 Hadalit |
| 14 | İttifaktan yardım iste | `helpAsk` sayacı | 3 × 5 dk hızlandırma |
| 15 | İttifaktan yardım al | `helped` sayacı | Gümüş Sandık, Nadir Kahraman Kartı, 50 Hadalit |

- 1–12 ilk oturum, 13–15 ilk gün hedefidir.
- Canavar seferden önce gelir: Reaktör 4'te tek sefer kuyruğu vardır; canavar takımı eve dönerken 11. adım (çantadan ödül açma) aradaki boşluğu doldurur.
- Yardım butonu yalnızca ücretsiz bitirme penceresini (5 dk) aşan işlerde çıkar; 14. adımın "Git"i bu yüzden Araştırma Merkezi'ne götürür.

## Kurallar

1. **Sunucu otoritelidir.** Adım, `ClaimQuest` komutuyla alınır; sunucu adımın sıradaki adım olduğunu ve gerçekten bittiğini (bina, kahraman, sayaç) kendisi doğrular. Aynı `CommandId` ikinci kez ödül vermez (komut defteri); bitmiş bir adımı yeniden almak `NotCurrent` ile reddedilir.
2. **Durum:** `PlayerExtras` içinde `Onboarding { Status, Index, StartedAt }`. Yeni hesap `Active` başlar.
3. **Eski hesaplar:** Rehber belgesi olmayan (rehberden önce kaydedilmiş) bir hesap ilk girişinde karar alır: reaktörü Sv. 4 ve üstü ya da 3+ hikâye görevi almışsa `Skipped` (rehberi hiç görmez), değilse rehbere 1. adımdan katılır (`existing` grubu).
4. **Hiç tuzağa düşürmez:** baloncuktaki × o adımı gizler (sonraki adımda geri gelir), "Rehberi kapat" rehberi kapatır; görev panelindeki "Rehberi aç" geri getirir. Rehber her zaman sunucunun adımından devam eder; ilerleme cihazda değil sunucudadır.

## Ekran

- **Görev şeridi:** rehber sürerken "Adım 3/15" ile rehber adımını gösterir; bitince "Ödülü Al!". Dokunmak adımın "Git"ini çalıştırır.
- **Rehber baloncuğu:** Komutan Elias Rourke'nin portresi (başlangıç kahramanı; yeni çizim yok), kısa talimat, "Git" ve kapat düğmeleri. Bir panel açıkken gizlenir.
- **Nabız halkası:** dokunulacak HUD düğmesinin (Kahramanlar, Bölge, Çanta, İttifak, Yardım ya da Git) etrafında yanıp sönen halka ve ok.
- **Harita oku:** canavar ve toplama adımlarında haritada en yakın Sv. 1 sürüye ya da boş kaynak noktasına turuncu ok; ekrandaysa hedefin üstünde zıplar, değilse kenarda yönü gösterir. Dokununca hedefin menüsü açılır.
- **Kilitler:** Araştırma ve PvP düğmelerinde reaktör kapısı açılana kadar "Sv.X" rozeti. Görev panelinde "Yakında açılacaklar": sonraki reaktör seviyelerinin açtıkları (toplama seferleri, PvP, önemli binalar, halkalar), "Reaktör Sv. X ile açılır".

## Ölçüm (huni)

- Sunucu her adımı `hadal.onboarding_events` tablosuna yazar (`player_id, step, step_id, cohort, at, seconds`; birincil anahtar `(player_id, step)`, tekrar yazım etkisiz). 0 = rehber başladı, n = n. adım alındı, -1 = eski hesap atlandı. `seconds` rehberin başından beri geçen süredir. Üçüncü taraf analitik yoktur.
- Overseer → **İlk Oturum Hunisi**: grup (yeni / sonradan katılan / hepsi) ve dönem seçilir; her adım için ulaşan hesap, başlayanlara oranı, önceki adımdan kayıp, ortanca süre ve ilk 10 dakikada ulaşan sayısı. Üstte "ilk 10 dakikada takılan" oranı: en az bir gün önce başlayıp 10. dakikadan sonra hiç adım almayan ve bitirmeyen hesaplar.

## Teknik Not

- Paylaşılan: `QuestCatalogData.onboarding`, `GameCatalog.TryGetOnboarding`, yeni görev türü `rings`, yeni sayaçlar (`gather`, `attack`, `monster`, `open`, `helpAsk`, `helped`), `OnboardingRules` (atlama eşiği, yakında açılacaklar), `QuestStateDto.Guide/GuideNumber/GuideTotal` (protobuf alan 8–10). "troops" ödülü yalnızca rehber adımlarında geçerlidir.
- Sunucu: `PlayerSession.Onboarding.cs`, `Onboarding/OnboardingStore.cs` (bellek ve Postgres), `Onboarding/OnboardingFunnel.cs`, Overseer `GET /admin/api/onboarding/funnel?cohort=new&days=30`.
- İstemci: `OnboardingGuide` (baloncuk, halka, Git), `MainHudView.Guide`, `BaseManagementView.Guide`, `WorldMapView.Guide`, `SocialView.Onboarding`, `Hud/GuideTargets`.
- Katalog tasarım sürümü 11'e çıktı: yayında dosya, saklı katalogu geçer (`CatalogBootstrap`).
