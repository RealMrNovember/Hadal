# 30 — PvP and Alliance Territory

Status: design + implementation plan (2026-10-29). Source: Last Asylum on the test phone (2026-10-29, a level 24
account without an alliance) plus the genre survey in docs/29. Everything is server-authoritative and runs on the
march timeline that gathering and monster attacks already use.

## 1. What Last Asylum does (seen on the phone)

| Screen | What we saw | HADAL takes |
|---|---|---|
| Tap an enemy base | Card (name, power, kills, alliance) and a ring of three buttons: **Saldırı**, **Seferberlik** (rally), **Keşif** (scout). Rally asks you to join an alliance first. | Same ring: Saldır, Toplu Saldırı, Keşif (plus Destek / Takviye for alliance mates). |
| Attack panel | My power vs **"Bilinmiyor"** (unknown until scouted), a matchup warning, the five heroes with their troop counts, troops 1577/1577, march time, **5 energy**, team slots 1–4, hospital 0/4,792. | Power vs "?" unless a scout report is fresher than 30 min; team picker as for monsters. No energy cost for PvP (Sonar stays for monsters). |
| Bag | **Kalkan (12 sa.)**, **Kalkan (24 sa.)**: "protects your base from enemy attacks and scouting for N hours. *Does not protect troops outside the base.*" | Shields 8 h / 24 h / 48 h / 7 d; same rule, gathering teams stay attackable. |
| Zoomed-out map | The map is cut into blocky **regions**, each with a town (Lv 1–7, *Kale*, *Şehir*, a central *Kraliyet Şehri* Lv 7). A captured town colours its whole region in the alliance's colour with a bright border and shows `[TAG] Town Lv.N`. A banner names the kingdom (#166) and its king. A minimap with **Bölgem / İttifak Üyeleri / Lonca Lideri** jump buttons. | Regions with **Strongholds** (Kale) Lv 1–7, alliance colours, `[TAG]` labels, minimap jumps (my base, my alliance's hub). |
| Gathering teams | Shown as a badge on the node (done in HADAL, cc17603). Enemies can attack them. | §4. |

## 2. Principles

1. **One timeline, the server's.** Every hostile march has a server arrival instant `ArrivesAt`. The battle happens
   at that instant, with the world as it is at that instant, whether anybody is online or not.
2. **Settle before you act.** Before the server handles *any* command and on every tick, it resolves every hostile
   arrival with `ArrivesAt <= now` (ordered by `ArrivesAt`, then march id). A command received at server time `t`
   therefore sees exactly the battles due at or before `t`, never later ones. A shield or teleport handled at
   `t < ArrivesAt` counts; one handled at `t >= ArrivesAt` is too late. Ties go to the battle (the arrival instant is
   inclusive). No client clock is ever used.
3. **Players who are offline are simulated, not frozen.** A player involved in PvP (a march out, or a hostile march
   on the way to them) is kept *resident* on the server: their session is loaded without a connection and ticks
   like an online one. Before the battle the defender is advanced to exactly `ArrivesAt` (production, training,
   healing, marches coming home). A dropped phone changes nothing for anybody.
4. **Lock order** stays world → hub → session. The settle runs under the world lock and touches one session at a
   time (attacker read → defender resolve+apply → reinforcers apply → attacker apply), so two session locks are never
   held together.
5. **Persistence** follows the write-behind pattern: hostile marches live in `hadal.world_pvp` (march id → JSON),
   strongholds in `hadal.world_regions`; players' marches, shields and war log in `PlayerExtras`.

## 3. Shields (Kalkan)

- Items `shield_8h`, `shield_24h`, `shield_48h`, `shield_7d` (bag, *Özel*). Using one sets
  `ShieldUntil = max(ShieldUntil, now) + duration`. Without the item the shield card buys it for Hadalite
  (300 / 800 / 1,500 / 4,000).
- A shield blocks attacks, rallies and scouts on the **base**. Teams outside (gathering, reinforcing) are not covered.
- A shield cannot be raised while the player has a hostile march of their own (attack, rally, scout) on the way.
- **Attacking breaks your shield**: launching an attack, joining a rally or sending a scout against a player ends
  the attacker's shield at once.
- **Beginner protection**: a base whose Core Reactor is below level 6 cannot be attacked or scouted, and cannot
  attack other players (it can still gather and fight monsters). Reaching level 6 ends it.
- At arrival the shield is checked at `ArrivesAt`; a shield raised one millisecond earlier (server time) blocks the
  battle and the team turns home ("Hedef kalkanlı").

## 4. Attacks

### 4.1 Targets
| Target | Who can be hit | Defender side |
|---|---|---|
| **Base** | any non-ally with reactor ≥ 6, no shield | troops at home + the defender's 5 strongest heroes not out + every reinforcing team stationed there |
| **Gathering team** | any non-ally's team in the *gathering* phase on a node | that team's troops and heroes |
| **Stronghold** (§6) | unowned or another alliance's | NPC garrison by level |

Teams of alliance mates are never targets; the ring shows *Destek* (reinforce) on a mate's base instead.

### 4.2 Resolution (at `ArrivesAt`)
- Base attack: if the base is not on the target tile any more (teleported) → **"Hedef taşındı"**, no battle, team
  turns home. If shielded → **"Hedef kalkanlı"**. Otherwise `CombatRules.Resolve(attacker, defender)` with the
  heroes' command bonus on both sides.
- Team attack: if the team is no longer gathering on that node (recalled, finished, already beaten) →
  **"Hedef ayrıldı"**. Otherwise the battle.
- Losses: the base owner's troops go to the Med Bay as wounded; everyone else (attacker, rally members,
  reinforcers, gathering teams) loses **half dead, half wounded** (the wounded travel home with the team).
- **Plunder** (base, attacker wins): 20 % of each of Titanium, Biomass and Food above the protected amount
  (`reactor × 5,000` each, never Hadalite), capped by the survivors' carry load, shared out by resource. Taken from
  the defender at `ArrivesAt`, carried home.
- **Team plunder** (team attack, attacker wins): the team's gathered cargo so far (up to the survivors' load). The
  beaten team turns home at once with what is left; the node is freed and gets its unworked load back.
- If the defender wins a team attack the team keeps gathering with its survivors.
- Reports: both sides get a battle report (kind `pvp`, opponent name/tag, plunder, outcome) and an entry in the
  **war log** (last 30, kept in the player's save, sent as `base:warlog`).

### 4.3 Scout (Keşif)
A troop-less march at double speed. At arrival (no shield) the scout reports the defender's troops at home, the
number of reinforcing teams and the lootable resources. Breaks the scouter's shield. The target is warned
("Üssün keşfedildi").

## 5. Alarms, reinforcements, rallies, war list

### 5.1 Incoming-attack alarm
- The server keeps, per player, the hostile marches aimed at their base or their teams and pushes `base:incoming`
  (attacker name/tag, kind attack/rally/scout, target base or team, arrival ms). Sent on change and on login.
- Client: while the list has an attack or rally, the screen edge **pulses red**, a banner shows
  "⚠ Saldırı geliyor: [TAG] Name — 00:42" and tapping it centres the map on the attacker's route. A scout shows a
  yellow banner only.
- Android push: the client schedules a local notification for each incoming arrival when it goes to background
  (no push server yet; a Firebase push is a later step once there is a sender).

### 5.2 Reinforcements (Destek)
- `MarchReinforce {PlayerId, HeroIds}` to an alliance mate's base. The team stays (`GatheredAt` far in the future)
  until recalled or the mate leaves the alliance; it fights in every battle at that base. At most 5 reinforcing teams
  per base. Recall brings it home from the base.

### 5.3 Rallies (Toplu Saldırı)
- `StartRally {TargetKind, TargetId, MusterSeconds ∈ {60, 180, 300, 600}, HeroIds}` by any member (R1 too, like LA).
  The leader's team waits at home. Members `JoinRally {RallyId, HeroIds}`; a joiner's team must reach the leader's
  base before the muster ends (the server refuses later joins). Up to 10 teams.
- At muster end the whole army sets out from the leader's base at the slowest team's speed and arrives once.
  Attacker side = all teams' troops; hero bonus = leader's team. Losses and plunder are shared by troop share; each
  team then travels home from the target.
- Before departure: a member can leave (recall), the leader can cancel (all go home). After departure the rally
  cannot be recalled.
- Rally on a shielded base at arrival: everybody turns home, nothing lost.

### 5.4 Alliance war list (Savaş)
A tab on the alliance page and a red-counter shortcut on the right-side alliance button: every hostile march
**into** a member (attacks, rallies, scouts) and **out of** the alliance (members' attacks, open rallies with
muster countdown and a *Katıl* button). Each row: who → whom, arrival countdown, *Git* (centre map), *Katıl*
(rally) or *Destek* (reinforce the target).

### 5.5 Battle reports by mail, result card, away summary (0.9.4)
Found in live play (0.9.3): a player's very first war report never popped up, because the client took the first war
log it received as history; a fought battle left nothing in the mailbox.
- **Mail to every side.** Every report the world writes (base attack and defence, team attack, rally, guard post,
  scout and "scouted", city and alliance-building fights, misses) is also a mail of kind `battle`, sent once per
  player, battle and report kind (`WorldService.BattleMail.cs`). The text is `loc:key|args` lines: result, who and
  from where (the attacker's base), coordinates, powers and rounds, both armies (largest 6 stacks each), own dead and
  wounded, loot gained or lost per resource, the defender's protected storage, plunder share (fatigue / much weaker
  target), rally contribution, and the battle id with seed and rule version. The mail's *Raporu aç* button opens the
  result screen from the war log. Mails live 30 days like every mail.
- **Result card.** A battle fought while the player is online (attacker, defender, guard, scout) opens a card over
  every screen: *Zafer!* / *Yenilgi*, loot (+ or −), losses, *Raporu aç* / *Tamam*. A report counts as news when it
  is unknown and fought after this login's snapshot (`ClientStateView.LiveSinceMs`), so the first one shows too.
- **Away summary.** On login, battles on the player (defence, team, guard, scouted) since they last looked (stored
  per player on the device, at most 3 days back) are summed up in one card: *Üssüne saldırı yapıldı*, how many, total
  dead/wounded and resources lost, the latest four, "Ayrıntılar postanda".
- **Later:** Android push notifications for an incoming attack and a lost defence while the app is closed.

## 6. Alliance territory

### 6.1 Regions and strongholds
- The 200×200 map is cut into regions by `TerritoryRules` (shared, deterministic, so the client draws them without
  data): seeds on a 10×10 grid of 20-tile cells, each moved by a seeded jitter of ±5; a tile belongs to the nearest
  seed by Chebyshev distance (ties: lower index) — blocky shapes like Last Asylum's.
- Every region has a **Stronghold** (Kale) near its seed. Level by belt: abyssal plain 1, Hadal Threshold 2, trench
  slope 3, deep slope 4, trench axis 5, core 6, and the centre region's **Hadal Tahtı** 7.
- Garrison: an NPC army by level (the monster army formula at level `2L+2`), refilled 12 h after a battle.

### 6.2 Claiming
- **Plant the Hub**: an R4/R5 of an alliance with no territory picks an unowned stronghold of level ≤ 2; the
  stronghold becomes the **Alliance Hub** and its region the alliance's. Moving the hub later: R5, 24 h cooldown, to
  another owned stronghold.
- **Capture**: attack or rally a stronghold of a region that touches the alliance's territory. Beat the garrison →
  the region flips to the alliance. Another alliance's region can be taken the same way, except the region holding
  its Hub.
- Limit: `1 + members / 3` regions (max 12). R5 can abandon a region (not the Hub's).
- Hadal Tahtı (Lv 7) is held by the trench itself until a later "Throne" event; it cannot be captured yet.

### 6.3 Territory effects
- Members' gathering on nodes inside their territory: **+25 % speed**.
- Alliance teleport (item `teleport_alliance` or 500 Hadalite) lands in the alliance's territory, on the free tile
  nearest the Hub; with no territory it keeps the old rule (next to the leader).
- Alliance colour: picked from 12 colours by the alliance id hash (no setting yet).

### 6.3.1 Rally point prompt (Last Asylum, seen 2026-10-29)
Right after joining an alliance, Last Asylum shows "İttifak Seferberlik Noktası" with the rally point's coordinates
(a tappable link), "you are very far, use Alliance Teleport", a map preview (alliance flag, glowing circle, the
player's avatar at the edge with the distance in km), the Alliance Teleport item with have/need ("3/1") and a gold
**Taşı** button. Taşı is instant: the camera recentres on a free tile in the alliance cluster within a second and the
base pops in there; no travel animation. HADAL: show the same prompt on joining (or opening the alliance page) when
the base is more than 30 km from the hub; the server already lands the jump on the free tile nearest the hub inside
the territory, falling back to the leader's base while there is no hub (`WorldService.Teleport.cs`, `TerritoryTile`).
Client prompt: not built yet.

### 6.4 Map
- Zoom far out (strategic view, past ~3× the old limit): regions filled with their owner's colour, bright borders,
  `[TAG] Kale Lv.N` labels, the hub as a flag, bases as dots (cyan mine, blue alliance, red/white others).
  `ViewTerritory` returns strongholds and every base as a dot; region shapes come from `TerritoryRules`.
- Minimap jumps: Üssüm, İttifak Merkezi.

## 7. Protocol

| Command | Payload |
|---|---|
| `MarchPvp` (56) | `TargetKind` base/team/stronghold, `TargetId` (player id, march id or stronghold id), `HeroIds` |
| `MarchScout` (57) | `PlayerId` |
| `MarchReinforce` (58) | `PlayerId`, `HeroIds` |
| `StartRally` (59) | `TargetKind`, `TargetId`, `MusterSeconds`, `HeroIds` |
| `JoinRally` (60) | `RallyId`, `HeroIds` |
| `CancelRally` (61) | `RallyId` |
| `ViewWar` (62) | — → `alliance:war` |
| `ViewTerritory` (63) | — → `world:territory` |
| `PlantHub` (64) | `StrongholdId` |
| `AbandonRegion` (65) | `StrongholdId` |

Entities: `base:incoming`, `base:warlog`, `base:shield` (in the VIP/profile delta), `alliance:war`,
`world:territory`. Gathering-team badges carry `MarchId` and `OwnerId` so enemies can target them.

## 8. Build order
| Phase | Content |
|---|---|
| P1 | Resident sessions, PvP settle engine, base and team attacks, shields + beginner protection, plunder, war log, incoming alarm, scout. Server tests for the timing rules. |
| P2 | Reinforcements, rallies, alliance war list. |
| P3 | Regions, strongholds, hub, capture, territory effects, territory teleport, `ViewTerritory`. |
| P4 | Client: attack ring, red alarm + banner + local notification, shield card, war log, war tab, rally/reinforce sheets, strategic zoom with territories, 18 languages. |
