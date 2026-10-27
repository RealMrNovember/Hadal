# 29 — Events, Alliance Operations, Alliance Buildings and Voting

Status: design, not built. Sources: Last Asylum on the test phone (2026-10-28: event centre, Survival War, Canyon
Conquest rules, Cheese Trap rules) and a survey of Whiteout Survival, Last War, State of Survival and Rise of
Kingdoms (fan wikis and guides, numbers are typical ranges). Everything here is retold in HADAL's world: the
abyss, the Pressure, creatures, Hadalite.

## 1. What the genre does (short)

| Pattern | Seen in | HADAL version |
|---|---|---|
| Members charge a shared meter, an officer triggers a 30-min boss fight, damage is scored | WoS Bear Trap, LA Cheese Trap, Last War Marshal's Guard | **Lure Trap** (§4.1) |
| Waves of attackers hit member bases; reinforcing pays | WoS Crazy Joe, Last War Zombie Siege, SoS Infected Horde | **Abyss Siege** (§4.2) |
| Weekly 1-v-1 alliance duel with a theme per day | Last War Alliance Duel | **Alliance Duel** (§4.3) |
| Solo themed points with rotating 4-hour themes, level brackets, stage chests, rank | LA Survival War, RoK MGE, WoS Hall of Chiefs | **Depth Race** (§4.4) |
| Task pool shared by the alliance | WoS Alliance Mobilization | **Mobilization** (§4.5) |
| 3-lane auto-battle league with registered teams and relay fights | LA Canyon Conquest, WoS Alliance Championship | **Trench Conquest** (§4.6) |
| Registered 30-v-30 battlefield over point buildings | WoS Foundry, Last War Desert Storm, RoK Ark | **Vent War** (§4.7, late) |
| Alliance territory from a fortress and border flags, bonuses inside | RoK | **Alliance Hub and Beacons** (§3) |
| Votes only to schedule (time slot); leader loss by inactivity transfer, not vote | WoS Canyon / Winter Siege | **Polls** (§5) |

Recurring rules worth keeping: officer-chosen difficulty that multiplies rewards; cooldowns and two time slots for
time zones; a boss that does not fight back (no troop loss); two reward tracks (personal by damage/points, alliance
by total); alliance rewards only for members of 48 h+; rewards by mail; titled officers may run events.

## 2. Foundations (build first)

### 2.1 Mailbox
The HUD's "Mail" is still "soon". Every event pays out by mail: system mails with attachments (claim → bag),
alliance mails from R4/R5, announcements from the admin panel. Mails expire after 30 days; attachments are server-held
until claimed. Battle reports move here too.

### 2.2 Event engine (server)
- **Definitions live in the database, not the catalog**: `events(id, template, status, starts_at, ends_at, audience,
  config jsonb, rewards jsonb, created_by, version)`. A client update is never needed for a new event, only for a new
  template.
- **Templates** are code: `points`, `ranked`, `themed_days`, `login`, `bonus`, `shop`, `lure_trap`, `abyss_siege`,
  `alliance_duel`, `mobilization`, `trench_conquest`. Each declares its config schema; the admin panel renders a form
  from it.
- **Scoring hooks**: the existing session actions emit one `ScoreEvent(kind, amount, playerId, allianceId)` —
  `gather`, `build_power`, `research_power`, `train(tier)`, `heal`, `monster_kill(level)`, `hero_recruit`,
  `hero_level`, `speedup_minutes`, `hadalite_spent`. Events subscribe by table (`kind → points`). All scoring is
  server side.
- **Scheduler**: one-off or recurring (weekly rotation), server-time windows, brackets by reactor level.
- **Leaderboards**: personal and alliance, in memory with periodic Postgres snapshots; frozen at the end.
- **Payout**: at the end, a job mails rank rewards; thresholds are claimed in the event page. Idempotent and audited.

### 2.3 Alliance roles and permissions
Ranks R1–R5 exist, and so do duties (war, logistics, diplomacy, scout). Duties get permissions:
war → start/schedule alliance events and rallies, set difficulty; logistics → place/upgrade alliance buildings;
diplomacy → polls and announcements; scout → mark targets. R5 can do everything; R4 by duty.

### 2.4 Rallies (D5)
A rally leader opens a rally on a target with a 1/3/5-minute muster; members join with one team each; the leader's
heroes bring all their skills, joiners only their lead hero's. Needed by the Lure Trap and Vent War.

## 3. Alliance buildings and territory

| Building | Who | Cost / rule | What it does |
|---|---|---|---|
| **Alliance Hub** (İttifak Merkezi) | R5 / logistics places, members build it by sending teams | 15+ members, alliance credits | Centre of territory; required for all others; rally point |
| **Beacon** (Sinyal Şamandırası) | Same | Must touch territory; cost rises every 10; cap per hub level | Extends territory; every 10 beacons +1 member slot |
| **Lure Trap** (Yem Kapanı) | R5 / war, next to the hub | One per slot (2 slots); upgrade by donations | Hosts the Lure Trap event |
| **Pressure Relay** (Basınç Rölesi) | Logistics | Inside territory | Defence bonus to member bases in Abyss Siege |
| **Alliance Rig** (İttifak Sondajı) | Logistics | Inside territory, lasts 3 days | Shared gathering node, members' teams cannot be attacked on it |
| **Alliance Depot** | Logistics | Inside territory | Members' gathering in territory fills it; R4/R5 hand it out |

Territory bonuses: +25 % gathering speed, teleport into territory, depot income from alliance resource nodes
enclosed by beacons. Buildings have durability and can be attacked once PvP exists.

## 4. Events

### 4.1 Lure Trap (Yem Kapanı) — alliance started
Our Cheese Trap. A Gulper (Yutucu) is lured to the alliance's trap by bioluminescent bait.
- **Charge**: members donate **Lure Bait** (from dailies, quests, monsters). Donations level the trap (Lv 1–5, +5 %
  team attack each) and give personal damage bonuses up to a cap.
- **Schedule**: R5 / war duty chooses start time and difficulty for Trap I or Trap II; setting one locks the other's
  difficulty. 20 members needed. Unscheduled traps expire at 22:00 server time on the cycle's last day. Cycle 48 h
  (admin-configurable).
- **Fight**: 30 minutes; one trap active at a time; rallies only; the Gulper does not fight back (no losses).
  Rallies led by war-duty officers deal extra damage. Offline members can opt in: their strongest team joins a rally
  with a free slot in the last 20 seconds.
- **Rewards**: personal by damage; alliance tier by combined damage of both traps (unlocks the next difficulty);
  the top dealer of each trap gets 1–10× the alliance reward. Trap levels reset after the event.

### 4.2 Abyss Siege (Derinlik Kuşatması) — alliance started, defence
Creatures rise in 20 waves against every member base. Officer picks difficulty (1–20, reactor minimums). Waves 10 and
20 hit the Alliance Hub (reinforce it); waves 7, 14, 17 are elite and target online players only. Shields do not
help; a base breached twice drops out. Points: kills at your own base 100 %, while reinforcing 50 %. No resources
lost. Charge: clue points from scouting and monster kills, carried over.

### 4.3 Alliance Duel (İttifak Düellosu) — server, weekly
Matched 1-v-1 by alliance power. Day themes: 1 Gathering, 2 Base Growth, 3 Research, 4 Heroes, 5 Total
Mobilisation, 6 Hunt (monster kills). Day wins are worth 1/2/2/2/2/4; first to 7 wins the week. Personal daily
chests at thresholds.

### 4.4 Depth Race (Derinlik Yarışı) — server, solo
LA's Survival War: a 4-hour theme rotation (shown in a calendar), brackets by reactor level, stage chests at point
thresholds, a bracket ranking. The first event to build: it only needs the engine.

### 4.5 Mobilization (Seferberlik) — server, alliance
A shared pool of tasks; each member takes one at a time, drops or refreshes it; colour-coded value. Points go to
the player and the alliance; alliance rank rewards + personal thresholds. 15+ members.

### 4.6 Trench Conquest (Hendek Fethi) — server, alliance league
LA's Canyon Conquest, and it fits our combat rules today:
- **Registration**: members register one team into one of 3 lanes; teams are snapshots, troops never die.
  Reactor 10+, 15 registered members; unregistered members get nothing.
- **Matching**: groups of six alliances of similar rank, league format, 5 rounds.
- **Preparation**: before each round leaders/officers move teams between lanes.
- **Battle**: per lane, teams fight in relay order (lowest power first, fixed); the winner fights on; each team has
  2–3 fights by league tier. Taking a lane's flag = clearing the other side; most flags wins (3 pts), draw 1 pt.
- **Results**: group ranking rewards; per round win/loss rewards; kill rewards per member; an exchange shop.

### 4.7 Vent War (Baca Savaşı) — late
Registered 30 + 10 battlefield around hydrothermal vents with first-capture and per-minute points, copied troops.
Needs rallies and real-time map PvP.

### 4.8 Simple ones
Login (7 days), bonus windows (2× gathering, −20 % build time), event shop (event tokens), first-purchase style
offers stay out until monetisation is designed.

## 5. Voting (Oylama)
- **Event time-slot vote**: for Trench Conquest and Lure Trap, officers offer 2–5 slots; members vote for 24 h; the
  winner is fixed in server time and posted to chat.
- **General polls**: R5 / diplomacy create a poll (question, 2–6 options, 1–72 h, single or multiple choice,
  anonymous or not). Members vote once; results show live; closing posts a summary to alliance chat.
- **Leader inactivity**: no vote — if R5 is offline 14 days, leadership passes automatically to the most powerful
  active R4 (a poll may be offered to members at that point, advisory only).

## 6. Admin panel
- **Events page**: list (draft, scheduled, live, ended); create from template; name/description per language;
  banner; window (server time) or weekly recurrence; audience (all, reactor range, new players, alliance size);
  the template's form (points table, difficulties, waves, cooldowns, member minimums); rewards (thresholds, rank
  bands, alliance tiers) picked from the item catalog; preview; schedule / publish.
- **Live**: participants, leaderboards, suspicious scores, end early, extend, re-send a reward mail. All audited.
- **Alliance-event rules** are template config: cooldowns, windows, min members, difficulty ladder, reward tables.
- **Mail**: send to everyone / an alliance / a player, with attachments.

## 7. Client
The HUD **Events** button opens an event centre: tabs per running event (red dot when something can be claimed),
banner, countdown, my points/rank, stage chests, ranking (personal / alliance), rules, calendar. Alliance events also
appear on the Alliance page with schedule buttons for officers, and as map objects (trap, hub).

## 8. Build order
| Step | Content | Needs |
|---|---|---|
| E1 | Mailbox; event engine core; admin Events page; event centre; Depth Race; login and bonus events | — |
| E2 | Duty permissions; polls and time-slot votes; donations | E1 |
| E3 | Trench Conquest (3 lanes, league, exchange shop) | E1, E2 |
| E4 | Rallies (D5); Alliance Hub on the map; Lure Trap | E2 |
| E5 | Abyss Siege (waves, reinforcement) | E4 |
| E6 | Alliance Duel; Mobilization | E1 |
| E7 | Beacons, territory bonuses, Rig, Depot; Vent War | E4, PvP |

Open: alliance buildings and the voting screen were not visible in Last Asylum because the test account has no
alliance; joining one there would show them.
