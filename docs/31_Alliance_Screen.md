# 31 — Alliance screen (sampled from Last Asylum, 2026-10-29)

Sampled on the phone after the test account rejoined its old alliance ([ITA] MadeInItaly, 98/100 members) in Last
Asylum. Hadal today has an alliance panel with three tabs (Members, Help, Admin); this is what the genre's alliance
hub looks like and what we should build towards, in our deep-sea skin.

## 1. Hub page
- **Header card**: banner emblem (left), `[TAG] Name`, Leader, Power (sum of members), Members `98/100`,
  Language. Under it the **announcement** (free text, translate button).
- **Nine tiles** in a 2-column grid, each with an icon and a red dot when something waits:
  Alliance War · Alliance Help · Alliance Gifts · Members · Alliance Shop · Alliance Research · Rankings ·
  Rally Point (Seferberlik Noktası) · City List.
- **Bottom bar**: Battle Log · Alliance Log · Applications · Settings (the last two only for officers).

## 2. Pages
| Page | What it shows |
|---|---|
| **Alliance War** | Tabs Attack / Defence / Events. A card per rally or incoming attack: target (`Sv.53 Vebalı Muhafız`), the rally leader with coordinates, a team countdown, member slots (portraits + "+" to join, `4/5`) and the distance (15 km). |
| **Alliance Help** | Popup: daily "alliance coins" bar from helping (`0/1,000`), "help points reward today", the list of requests (who, what: research speed-up, building level N), a green **Help All** button. |
| **Alliance Gifts** | A gift level bar (points `43,514,120/54,100,000`), tabs Daily / Advanced, chest rows (`Lv.1 Alliance Chest — X defeated a Lv.54 plague guard`, +10 points, **Collect**). Gifts come from members' kills and purchases. |
| **Members** | Leader card on top (R5, name, last online), four officer titles under it (Diplomat, Goddess, Recruiter, War God — each a portrait slot), then collapsible ranks **R4 0/8, R3 7/58, R2 0/21, R1 1/10** with member rows (portrait, name, level, power, online / "7g önce"). |
| **Alliance Shop** | Tabs Gem shop / VIP shop / Alliance shop; "refreshes every Monday" countdown; items priced in alliance coins with stock (`1/1`, `30/30`), including teleports (Targeted 1/1, Alliance 10/10, Random 3/3), shields 8 h/12 h/24 h, speed-ups, hero shards. |
| **Alliance Research** | "Alliance Technology" with Development / War tabs, today's tech donation, a tech tree of nodes with levels (`7/10` research speed, `1/10` grain protection, production, building cost …). Members donate; the leader picks the focus. |
| **Rankings** | Power ranking with tabs Power / Kills / Donations; top three highlighted; the player's own row pinned at the bottom (`94 MikeRoss91`). |
| **Rally Point** | Popup: rally coordinates, distance, a map preview with the alliance flag, the Alliance Teleport item (`2/1`) and **Move**. Shown automatically after joining when far away (see docs/30 hand-off). |
| **City List** | Tabs Occupied / Open to occupy; level filters; city cards (level, coordinates, distance in km, "Occupied"); "Cities owned 6/6"; **City Bonus** button. |
| **Battle Log** | Dated list: "Nagano attacked #166Rose912's territory (X:367 Y:761) and won", newest first, coordinates as links. |
| **Alliance Log** | Tabs Join/Leave · Research · Rank changes · Cities: "X joined", "Y removed Z", with dates. |

## 3. Floating shortcuts on the map/base
- **Handshake** (right, above the Alliance button): appears when members ask for build/research help; one tap helps all.
- **Medical kit** (bottom-left): appears when members ask for healing help; one tap helps all.
(Being built: alliance help for heal and research with both bubbles.)

## 4. What Hadal has / misses
- Have: create/join/browse, members with ranks R1–R5 and duties, help for construction, chat, admin tab (settings,
  transfer, kick), alliance teleport, events with alliance points.
- Missing: hub page with the tile grid and header, announcement, officer titles row, collapsible rank groups,
  alliance gifts (chest pool), alliance coins + shop, alliance research tree with donations, rankings tab, battle log
  and alliance log, applications list, rally/war page (PvP session), rally point flag + territory (PvP session),
  city list (later).

## 5. Suggested order
1. Hub page + header + announcement + members page in the LA layout (titles row, collapsible ranks, last online).
2. Alliance log and battle log (server already knows joins/leaves/kicks; battles come with PvP).
3. Alliance coins (from helping and gifts) + alliance shop (teleports, shields, speed-ups).
4. Alliance gifts (chest pool filled by members' kills, purchases and events).
5. Alliance research tree with donations.
6. Rankings (power / kills / donations).
War page, rally point and territory come from the PvP/territory work (docs/30).
