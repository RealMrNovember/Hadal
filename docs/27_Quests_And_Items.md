# 27 — Görevler, Sandıklar ve Kahraman Kartları

**Document:** Game Design · HADAL
**Language:** TR (canonical design voice)
**Status:** İlk sürüm uygulandı (2026-10-26, istemci 1.5.0). İçerik `shared/catalog/catalog.json` → `quests`, `items`, `heroes.rarities[].recruitShards`.

---

## Neden

Last Asylum'da kahramanlar yalnızca çekilişten gelmez: görevler, günlük etkinlik sandıkları ve hediye kutuları **kahraman kartı** verir; yeterli kart toplanınca kahraman çağrılır. Bu, oyuncuya her gün yapacak bir şey ve ücretsiz bir kahraman yolu sağlar.

## Hikâye görevleri

- Sırayla gelir, ana ekrandaki görev şeridinde hep **bir tane** görünür ("Oksijen Tesisi binasını Sv. 2 yap (1/2)"). Bitince şerit turuncu "Ödülü Al!" olur; dokununca ödül alınır.
- İlk görevler oyuncuyu temel döngüye sokar ve ilk ücretsiz kahramanları verir: 1. görev **Lyra Moss**'un 10 kartını (çağırmaya yeter), 4. görev **Tess Ruiz**'in 10 kartını verir.
- Türler: bina seviyesi, bina sayısı, olay sayacı (inşaat, araştırma, kurtarma, Takviye, Kriyo-Kapsül, kahraman eğitimi, ittifak yardımı, sohbet), kahraman seviyesi, kahraman sayısı, ittifaka katılma.

## Günlük görevler ve etkinlik sandıkları

- 8 günlük görev, her gün 00:00 UTC'de yenilenir. Her biri 10–20 **etkinlik puanı** verir (toplam 150).
- 20 / 40 / 60 / 80 / 100 puanda sandıklar açılır: Titanyum, Bronz Sandık, Sıradan Kahraman Kartı, Gümüş Sandık, Nadir Kahraman Kartı + Hadalit.

## Çanta: sandıklar ve kahraman kartları

| Eşya | İçerik |
|---|---|
| Bronz Sandık | 2 çekiliş: Titanyum, Biyokütle, Gıda, %15 sıradan kart ×5, %5 Hadalit |
| Gümüş Sandık | 2.000 Titanyum + 2 çekiliş: kaynak, Hadalit, sıradan ×10 / nadir ×5 kart |
| Altın Sandık | 30 Hadalit + 3 çekiliş: kaynak, nadir ×10, destansı ×5, efsanevi ×3 kart |
| Sıradan / Nadir / Destansı / Efsanevi Kahraman Kartı | O nadirlikten rastgele bir kahramanın 10 kartı |

- Sahip olunmayan kahramanın kartları çantada birikir; **çağırma bedeli**: Sıradan 10, Nadir 20, Destansı 30, Efsanevi 50 kart. Fazlası kahramana yıldız parçası olarak geçer.
- Sahip olunan kahramanın kartı doğrudan yıldız parçası olur (yıldız yükseltme `25_Heroes_Alliance_Chat.md`).
- Hepsini aç: tek seferde en fazla 50.

## Kahraman görselleri

- Kartlarda ve detay sayfasında boyalı portreler (`Art/Portraits`, SDXL ile üretildi; lisans `art/THIRD_PARTY.md`). Eşleme katalogda `heroes.list[].portrait`.
- Detay sayfasında kahramanın **hareketli 3D modeli** (`heroes.list[].model`, `Art/Characters`): sürükleyince döner, dokununca el sallar (`HeroStage`, ayrı kamera → RenderTexture).
- Başlangıç kahramanı portresine uygun olarak **Komutan Elias Rourke** oldu (kimlik `mara` aynı kaldı).

## Teknik Not

- Komutlar: `ClaimQuest` (25), `ClaimDailyChest` (26), `OpenItem` (27), `RecruitHero` (28). Replikasyon: `base:quests` (`QuestStateDto`), `base:inventory` (`InventoryDto`), ödül raporu `base:reward:<komut>` (`RewardReportDto`); anlık görüntü alanları 9 ve 10.
- Sayaçlar ömür boyu ve günlük olarak `PlayerExtras` içinde saklanır; ittifak yardımı ve sohbet sayaçlarını `SocialHub` artırır.
- Rastgelelik sunucuda (`Random.Shared`); istemci yalnızca sonucu gösterir.
