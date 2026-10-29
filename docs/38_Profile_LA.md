# 38 — Profile: what Last Asylum's "Doktor Bilgisi" screen does

Studied on the user's phone (Last Asylum: Plague, `com.phs.global`, Turkish UI) on 2026-10-30, read only: nothing was
renamed, liked, bought or switched. Screenshots are in the session scratchpad `la_profile\` folder (names below). This is
the reference for HADAL's own profile screen, which is a separate follow-up.

## 1. Main card — "Doktor Bilgisi"

`p01_main`, `p17_card_check`

Opened by tapping the avatar at the top left of the HUD. A centred modal card (dark title bar "Doktor Bilgisi", X on
the right) over the blurred base:

- **Portrait** (large square, polaroid-like white frame) with a **heart and the like count** ("♥ 93") under it.
- Right of it two stat pills: **power** (shield-and-swords icon, 14,554,268) and **kills** (crossed swords, 153,162);
  below them a greyed placeholder emblem with "Çok Yakında" (coming soon — a future title/badge).
- **Name row**: "Sv.24 [ITA] MikeRoss91" (level, alliance tag, name) with an **edit icon** (pen) on the right.
- **Alliance row**: alliance banner icon, "[ITA] MadeInItaly", **search icon** → opens that alliance's page.
- **Server row**: castle icon, "#166", **search icon** → opens the kingdom/throne page.
- **Gift showcase**: six round gift slots and an **edit icon**.
- **Bottom bar** (dark, 4 icon buttons with captions): **Kayıt**, **Hesap**, **Ayarlar**, **Sıralama**.

## 2. Edit icons

- **Name edit** (`p18_name_edit`): a second "Doktor Bilgisi" card for editing:
  - the current avatar with the **player id** and a copy button;
  - **Karakter Adı**: text field "Girmek için dokunun 0/18" + "💎1000 Onayla" (renaming costs gems);
  - **Cinsiyet**: female / male / hidden checkboxes + Onayla;
  - **Avatar**: grid of avatars — the uploaded custom photo (with a red "remove" badge and a green tick when in use)
    and preset portraits — with an (!) info button;
  - buttons **Avatar Çerçevesi** (gold) and **Kullan** (blue, apply the selected avatar).
- **Avatar Çerçevesi** (`p19_frame_picker`) opens a full-screen **"Kostüm"** page: big preview of the avatar in the
  chosen frame on a blue backdrop ("Varsayılan"), tabs *… / Avatar Çerçevesi / İsimlik / Özel Efektler / Sohbet
  (balonu)*, a 4-column grid of frames (locked ones with a padlock, the used one ticked), the duration ("Kalıcı") and a
  **Kullanımda / Kullan** button.
- **Gift showcase edit** (`p22_badge_edit`): a bottom sheet with six "+" slots on top and the gifts the player has
  received below as cards (e.g. "Lale" ×10, worth 1 heart), and a **Vitrin** button to save which gifts are shown.

## 3. Portrait and likes

- **Tapping the portrait** (`p24_portrait_tap`) only shows it enlarged (full-width preview); it is not a picker —
  avatar changes go through the name-edit card.
- **Likes**: on one's own profile the heart is a counter. Other players like you from your profile (the heart there is
  the like button); every like lands in **Kayıt** (`p02_kayit`, `p03_kayit_scrolled`) as a row: liker's portrait with
  level, "[1NF] Shargiyaa", the fixed line "Seni çok takdir ediyorum.", and the date-time. (The heart was not tapped,
  to avoid sending anything.)

## 4. Bottom bar pages

- **Kayıt** (`p02_kayit`, `p03_kayit_scrolled`): the like log described above, newest first, scrolling.
- **Hesap** (`p05_hesap`): "Hesap Ayarları" — avatar, account number, nickname with kingdom, region level;
  "Hesap Durumu: Güvenli" (green); a note that Facebook / Game Center / Google Play accounts hold separate progress;
  buttons **Kullanıcı Merkezi** (publisher account centre: binding, login) and **Karakter Yönetimi** (characters on
  other servers). Neither was opened.
- **Ayarlar** (`p06_ayarlar`): server time and version on top, then an icon grid: Hesap, Ayarlar, Dil, Bildirim
  Ayarları, Yardım, Hediye Kodu, Topyekûn Saldırı, Ağ.
  - inner **Ayarlar** (`p07_ayarlar_game`, `p08_ayarlar_game_scrolled`): grouped cards — *Genel*: Müzik, Ses Efektleri
    (switches); *Diller*: Dili Göster; *Grafik Efekti*: Grafik Kalitesi (Düşük/Orta/Yüksek), Yenileme Hızı
    (Düşük/Yüksek/Ultra), Kare hızını otomatik ayarla, Titreşim, Durum Çubuğunu Göster, Özel Klavyeyi Kapat, Yetkili
    Atama Bildirimi.
  - **Bildirim Ayarları** (`p11_language` — the in-game notification page; the first tap jumped to Android's own app
    notification page `p10_notifications` because notifications are off for LA at the OS level): grouped switches —
    Sıra Zamanlayıcı (building, research, training, gathering, troops home), Kaynak Dolu, Savunma Kalkanı (shield
    expired, kill-event warning), Bölge Savaşı (scouted, attacked, rallied), İttifak Savaşı, İttifak Üyesi Seferberlik…
- **Sıralama** (`p13_siralama`, `p14_siralama_scrolled`, `p15_siralama_scrolled2`, `p16_ranking_detail`): a grid of
  banner cards, each showing the current #1: *İttifak*: İttifak Savaş Gücü, İttifak Öldürme; *Kişisel*: Kişisel Savaş
  Gücü, Kişisel Öldürmeler, Bireysel Sığınak, Macera, En Güçlü Kahraman, Kahraman Savaş Gücü, Savaşçı / Korucu /
  Büyücü Arenası. A card opens a table (rank medals 1–3 in gold/silver/bronze rows, portrait, "[TAG] Name", subline,
  score) with the player's own row pinned at the bottom in green ("Sıralanmamış" when outside the list). Footnote:
  "Not: Sıralamalarda veri güncelleme gecikmesi vardır."

## 5. Other pages reached from the card

- Alliance search icon → the alliance page (`p25_alliance_search`): banner, leader, power, members 98/100, language,
  description, feature buttons and a bottom bar (Savaş Kaydı, Kayıt, Başvurular, Ayarlar).
- Server search icon → the kingdom page (`p26_kingdom_search`): throne hall with the king and ministers and their
  terms, a Sıralama button.

## 6. Notes for HADAL

- One modal card, not a full screen: portrait + likes, power + kills, name/alliance/server rows with actions, gift
  showcase, and four bottom tabs.
- Renaming, gender, avatar and frame are one edit flow; cosmetic frames/nameplates/chat bubbles live in a separate
  "Kostüm" screen.
- Likes are social proof with a visible log; gifts received can be pinned to a six-slot showcase.
- Settings and account binding hang off the profile, as do the leaderboards.
