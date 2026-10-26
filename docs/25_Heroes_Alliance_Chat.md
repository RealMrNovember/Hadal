# 25 — Kahramanlar, İttifak ve Sohbet

**Document:** Game Design · HADAL
**Language:** TR (canonical design voice)
**Status:** İlk sürüm uygulandı (2026-10-26, istemci 1.4.0). Sayılar `shared/catalog/catalog.json` içinde (`heroes`, `alliance`, `chat`); Overseer "Denge" sayfasından değiştirilebilir. Hikâye: `10_Heroes.md`, ittifak vizyonu: `12_PvP_Alliance.md`.

---

## Kahramanlar

Last Asylum'da kahramanlar savaşın yanında üssü de güçlendirir. HADAL'da savaş (Phase 10) gelene kadar kahramanlar **üs yeteneği** taşır ve **Komuta Ekibi**'ne atanınca çalışır.

**Kurallar**

1. Her üs başlangıç kahramanı **Kaptan Mara Venn** ile başlar; Mara ekibin 1. yuvasındadır.
2. **Komuta Ekibi:** 3 yuva. 1. yuva hemen, 2. yuva Çekirdek Reaktör 5'te, 3. yuva Reaktör 10'da açılır. Yalnızca ekipteki kahramanların yeteneği işler. Bir kahramanı dolu bir yuvaya koymak oradakini ekipten çıkarır.
3. **Yetenek** araştırma etkileriyle aynı türdendir (inşaat süresi, araştırma süresi, bir kaynağın üretimi, depo sınırları) ve araştırmanın üstüne eklenir. Güç = taban + (seviye − 1) × seviye başına onda bir + (yıldız − 1) × yıldız başına.
4. **Eğitim:** kaynakla anında bir seviye. Bedel = Titanyum 150 + Biyokütle 100, seviye başına ×1,08, nadirlik çarpanı (Sıradan 1, Nadir 1,2, Destansı 1,5, Efsanevi 2).
5. **Seviye sınırı** = yıldız × 10 (5 yıldızda 50).
6. **Yıldız yükseltme** parça ister: 2★ 10, 3★ 20, 4★ 40, 5★ 80 parça. Parça, sahip olunan bir kahraman Kriyo-Kapsül'den tekrar çıkınca gelir (Sıradan/Nadir 10, Destansı 15, Efsanevi 20).
7. **Kriyo-Kapsül** (`15_Monetization.md` Cryo-Pod Salvage): tek çekim 20 Hadalit, 10'lu çekim 180 Hadalit; günde 1 ücretsiz tek çekim. Olasılık Sıradan %65, Nadir %25, Destansı %8, Efsanevi %2. **Garanti:** 30 çekimde Destansı ya da üstü gelmezse 30. çekim Destansı+ olur.
8. **Güç** = nadirlik tabanı (100/150/220/320) × (1 + 0,1 × (seviye − 1)) × (1 + 0,2 × (yıldız − 1)). Oyuncu gücü = bina seviyeleri × 10 + kahraman güçleri (ittifak listesinde görünür).

**İlk kadro**

| Kahraman | Sınıf · Grup | Nadirlik | Yetenek (Sv1 → Sv50 5★) |
|---|---|---|---|
| Kaptan Mara Venn | Komutan · Atlantik Birliği | Sıradan | İnşaat süresi −%3 → −%20 |
| Dr. Lyra Moss | Hekim · Pasifik Tarikatı | Sıradan | Oksijen üretimi +%3 → +%20 |
| Tess Ruiz | Mühendis · Atlantik Birliği | Sıradan | Titanyum üretimi +%3 → +%20 |
| Kael | Avcı · Derindoğanlar | Nadir | Gıda üretimi +%4 → +%30 |
| Dr. Orin Hale | Bilim insanı · Pasifik Tarikatı | Nadir | Araştırma süresi −%4 → −%30 |
| Juno Vega | Pilot · Unutulanlar | Destansı | Enerji üretimi +%5 → +%40 |
| Sable | Mühendis · Uçurum Tarikatı | Destansı | Depo sınırları +%5 → +%40 |
| Aelis | Bilim insanı · Derindoğanlar | Efsanevi | Biyokütle üretimi +%8 → +%52 |

Portreler şimdilik renkli baş harf kutusudur; çizim portreler sanat fazında gelir. Sefer ve savaş istatistikleri (`10_Heroes.md` sınıfları, beceriler) Phase 7 ve 10 ile eklenir.

## İttifak

1. **Kurma:** 3.000 Titanyum. İsim 3–20 karakter (harf, rakam, boşluk, `-_.`), etiket 3–4 harf/rakam (büyük harfe çevrilir). İsim ve etiket benzersizdir (büyük/küçük harf fark etmez).
2. **Katılma:** açık katılım, en fazla 30 üye. Liste güce göre sıralı; isim ya da etiketle aranır.
3. **Lider** üye çıkarabilir. Ayrılan lider yerini en eski üyeye bırakır; son üye ayrılınca ittifak dağılır ve isim/etiket boşa çıkar.
4. **Yardım** (Last Asylum'daki Alliance Help): oyuncu süren bir inşaat için "Yardım iste" der. Her ittifak arkadaşı bir kez yardım edebilir; her yardım toplam sürenin %1'ini (en az 60 sn) düşer, bir inşaata en fazla 10 yardım. "Hepsine Yardım" tüm açık isteklere tek dokunuşla yardım eder. Sahip çevrimdışıysa yardım bir sonraki girişinde uygulanır.
5. İttifak sohbet kanalına katılma, ayrılma ve çıkarılma sistem mesajı olarak düşer.

## Sohbet

1. İki kanal: **Dünya** (herkes) ve **İttifak** (yalnızca üyeler).
2. Mesaj en fazla 200 karakter; kontrol karakterleri ve fazla boşluk temizlenir. İki mesaj arası en az 2 saniye.
3. Girişte her kanalın son 50 mesajı gelir. Ekranın altında son mesaj şeridi ve okunmamış rozeti vardır.
4. Oyuncu adı hesabın görünen adıdır (Hesap ekranından değiştirilir); ittifak etiketi adın önünde görünür. Oyuncular metne biçim etiketi sokamaz.
5. **Moderasyon:** Overseer `GET /admin/api/chat?channel=world` son mesajları gösterir, `DELETE /admin/api/chat/{id}` (Oyun Yöneticisi) bir mesajı her yerden siler. Susturma (`reason.Muted`) sonraki adımdır.

## Teknik Not

- Kahraman durumu `hadal.players.extras` JSON'unda (`PlayerExtras`: kahramanlar, garanti sayacı, ücretsiz çekim zamanı). Komutlar: `ExecuteGacha` (5), `LevelUpHero` (14), `PromoteHero` (15), `AssignHero` (16). Replikasyon: `base:heroes` (`HeroRosterDto`), çekim sonucu `base:gacha:<komut>` (`GachaResultDto`), anlık görüntüde alan 8.
- Ekip yetenekleri `HeroRules.AddCrew` ile araştırma etkilerinin üstüne eklenir (`PlayerModifiers`); sunucu ve istemci aynı kodu kullanır.
- Sohbet ve ittifak `SocialHub` içinde (bellekte, tek kilit) çalışır ve sırayla `ISocialStore`'a yazılır: `hadal.alliances`, `hadal.alliance_members`, `hadal.chat_messages`. Kilit sırası her zaman hub → oturum.
- Komutlar: `SendChat` (17), `CreateAlliance` (18), `JoinAlliance` (19), `LeaveAlliance` (20), `BrowseAlliances` (21), `KickMember` (22), `RequestHelp` (23), `HelpAll` (24). Replikasyon: `chat:history`, `chat:msg:<id>`, `alliance:mine` (yoksa Delete), `alliance:list`, `alliance:help`.
- Açık yardım istekleri ve çevrimdışı oyunculara bekleyen yardımlar bellekte tutulur; sunucu yeniden başlarsa kaybolur (inşaat yine normal süresinde biter).
