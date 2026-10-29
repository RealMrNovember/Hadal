# 37 — Mailbox: what Last Asylum does, and what HADAL copies

Studied on the user's phone (Last Asylum: Plague, `com.phs.global`, Turkish UI, kingdom #166) on 2026-10-30, read only:
no mail was claimed, deleted or sent, and only mails that were already read were opened (the HUD "Posta" counter stayed
at 20 before and after). Screenshots live outside the repo, in the session scratchpad `la_mail\` folder (names below).

## 1. Entry and category screen

- **HUD:** a "Posta" button bottom right (envelope icon) with a red number badge for all unread mail (20). `la00_start`
- **Posta home** (`la01_mail_home`) is not a tab strip but a **vertical list of category cards**, each a wide light-blue
  card with a round icon on the left, the name, a chevron on the right and a **red dot** (no number) when it holds
  unread mail. A hint line on top: *"Postalar 30 g sonra otomatik olarak silinecek"* with an (!) info button.
  Categories, in order:
  1. **Savaş** (crossed swords): PvP battle reports, scout reports ("Düşman Bölgenizi Keşfetti"), rally hits.
  2. **İttifak** (alliance banner): alliance mail from members/officers; the card carries the player's alliance rank
     badge (R4) because rank R4+ may write alliance-wide mail.
  3. **Etkinlik** (notice board): event results, ranking rewards, alliance caravan / contest notices, kingdom war result.
  4. **Sistem** (gear): game notices ("Yeni Kral Atandı" …), compensation and rewards.
  5. **Rapor** (calendar with tick): PvE reports (monster fights "Zafer… Sv.56 Vebalı Muhafız") and gathering reports.
  6. **Favoriler** (trophy): mails the player starred.
  7. **Yükleme Yap** (gem): purchase receipts.
- Back arrow top left, title "Posta". There is no compose button on the mail screens: personal messages live in chat.

## 2. Category list (rows)

`la02_battle_list`, `la10_alliance_list`, `la11_event_list`, `la12_system_list`, `la13_report_list`, `la17_event_list_scrolled`

- One row per mail, full width card (~110 px at 1080 wide):
  - left: round icon; a **closed sealed envelope** when unread, an **opened envelope** (letter sticking out) when read;
    gathering reports show the resource icon instead.
  - **bold title** ("Yenilgi", "Düşman Bölgenizi Keşfetti", "Yeni Kral Atandı"), PvE wins in **green** ("Zafer…").
  - one **summary line** in a smaller blue-grey: who did what; enemy names in **red** with their tag ("[PVP] ijahe
    bölgenizi keşfetti", "Toplanmış birlik Sv.130 Vebalı Gezgin [ITA]MikeRoss91 saldırdı.").
  - **relative time** in grey under it ("1 sa. önce", "15 sa. önce", "4G ÖNCE").
  - unread: lighter card, **red dot** top right, a thin red-blue airmail stripe along the bottom edge.
  - read: darker blue-grey card with a large faint **checkmark watermark** on the right.
  - alliance mail rows show the sender ("[ITA]Shaina19") as the title and a small blue rank flag on the icon.
  - gathering report rows: resource name, amount ("x1.4M"), coordinates "#:166 X:319 Y:617" top right.
- Bottom bar (dark): **"Okunmuşları Sil"** (trash) and **"Hepsini Al"** (envelope with gift). Both act on the open
  category only.
- Empty category: just "Posta Yok" centred, no bottom bar (`la18_topup_list`, `la19_favorites_list`).

## 3. Battle report (opened)

`la03_battle_report_1`, `la04_battle_report_2`, `la05_battle_details`, `la06_battle_stats`

- Header strip: "Konum:#:166 X:364 Y:678" as a green link (jumps to the map), date-time on the right.
- **VS banner**, split blue (left side) / red (right side): each side has a portrait, a big result word
  (**Victory** gold / **Defeat** grey), coordinates as a link, "Sv.170 Vebalı Gezgin" / "#166 MikeRoss91".
  Above the banner a thin troop bar per side with remaining/lost numbers.
- **Genel Bakış**: per participant a row with name, portrait and a bar "lost … remaining" (0 / 2370) for each side
  (rallies list every member).
- **Savaş Ayrıntıları → 1. Tur**: round card, both portraits, Victory, the heroes used (5 hero portraits with level),
  and an **"Ayrıntıları Görüntüle"** button.
- Details page has two tabs:
  - **Kahraman**: hero formation for both sides, special weapons, formation bonus, faction effects.
  - **Savaş İstatistikleri**: an **Asker** table mirrored for both sides with columns **Düşenler** (dead),
    **Revir** (to the hospital), **Hafif Yaralı** (lightly wounded), **Kalan** (remaining), with the troop type badge in
    the middle; below, hero damage dealt / taken bars.
  - Footer: "Savaş Raporu Kimliği: 31185…" (report id).
- Bottom bar of an opened report: **Favoriler** (star), **Kaldır** (trash), **Paylaş** (share to chat).
- Loot was 0 in the examined mail; LA shows plunder as resource icons with amounts in the overview when there is any.

## 4. Scout report

`la08_scout_report`

- Header "Koordinatlar #:166 X:266 Y:660" + date. Two halves: left blue "Keşif" (the scout, portrait, location link,
  name), a spyglass with green arrows in the middle, right red "Keşif yapıldı" (us).
- Body: one sentence, *"[PVP] ijahe tarafından keşfedildik. Tetikte olun ve çatışmaya hazırlanın."*
- Bottom bar: Favoriler, Kaldır.

## 5. Event / reward mail

`la15_event_reward`, `la16_event_reward2`

- Banner image with the title ("Vebalı Şeytan Kralı Sıralama Ödülü") and the date over it.
- A ranking table (top 5 with gold/silver/bronze medals, portraits, points), then the explanation text ("Maalesef ilk
  500 arasına giremediniz…" / "…12. sırada yer aldınız. Ödül kriterlerini karşılamadığınız için ödül verilmedi").
- With rewards, the attachment grid sits under the text with a **"Al"** button; the bottom bar then has only
  Favoriler + Kaldır (Kaldır is blocked while something is unclaimed). (No unclaimed reward mail was opened to keep the
  account untouched.)

## 6. Alliance mail and personal messages

- Alliance mails are their own category; rows are titled with the sender ("[ITA]Shaina19"). The R4 rank badge on the
  category card marks that the player may write alliance-wide mail.
- LA has **no personal-mail category**: one-to-one messages are private chats. HADAL keeps player-to-player mail
  (MailRules), so it gets its own **Kişisel** tab where LA has Rapor.

## 7. What HADAL takes over

| LA | HADAL |
|---|---|
| Category cards Savaş / İttifak / Etkinlik / Sistem / Rapor | Tabs **Savaş** (battle, scout, shield-blocked) / **İttifak** / **Etkinlik** (events, boss prizes) / **Sistem** (system + Overseer) / **Kişisel** (player mail) |
| Red dot per category, number on the HUD | Unread count per tab (from the server) and the HUD dot for any tab |
| Envelope closed/open, red dot, check watermark | Same row states: envelope icon by state, red dot, faded "read" card, gift marker for unclaimed attachments |
| Red enemy name in the summary line | Opponent "[TAG] Name" highlighted in the battle row's summary |
| VS banner, Victory/Defeat, coordinates | Battle report card from the structured report: two coloured halves, result word, names, coordinates, powers |
| Asker table Düşenler / Revir / Kalan | Troop table per side: type, start, lost, remaining, plus own dead/wounded |
| Plunder icons | Loot / lost rows with resource icons; protected share |
| Okunmuşları Sil / Hepsini Al per category | **Okunanları sil**, **Tümünü al**, **Tümünü okundu say** per tab |
| Favoriler / Paylaş | Not taken now (no favourites store; sharing to chat is a later item) |
