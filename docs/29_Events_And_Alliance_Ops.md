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

### 2.3 Alliance roles and permissions (built, E2)
Ranks R1–R5 exist, and so do duties (war, logistics, diplomacy, scout). Duties get permissions:
war → start/schedule alliance events and rallies, set difficulty; logistics → place/upgrade alliance buildings;
diplomacy → polls and announcements; scout → mark targets. R5 can do everything; R4 by duty.

As built (`AlliancePermissions` in HADAL.Shared, checked on the server everywhere it applies):
- **Leader (R5)** holds every duty. **An officer (R4)** carries one duty (or none) and gets that duty's rights; a duty
  tag below R4 grants nothing. Several officers may share a duty. Only the leader assigns duties: the member card's
  "Duty" button opens a picker with a row per duty and what it allows.
- **War** → opens event time-slot votes (§5). Scheduling the Lure Trap and its difficulty use the same check once they
  exist (E4). Rallies stay open to every member: §4.1 only gives war-led rallies a bonus.
- **Logistics** → plants the hub, places and removes beacons and alliance buildings (`Territory` PlaceHub, PlaceBeacon,
  Remove, PlaceBuilding). Moving the hub and abandoning a city stay leader-only; donating stays open to everyone.
- **Diplomacy** → opens polls (§5) and writes the hub announcement (before: any officer).
- **Scout** → `CanMarkTargets` is defined, but the map has no target marks yet, so nothing uses it.
- Without a duty an officer keeps the plain officer rights: invite and approve, rank R1–R3 members, kick R1–R3, set the
  research focus. Existing officers have no duty until the leader picks one.
- Donations (E2's third item) were already built: alliance research donations (docs/31 step 6) and hub and building
  donations (docs/32).

### 2.4 Rallies (D5)
A rally leader opens a rally on a target with a 1/3/5-minute muster; members join with one team each; the leader's
heroes bring all their skills, joiners only their lead hero's. Needed by the Lure Trap and Vent War.

### 2.5 Weekly calendar and default schedules (built, roadmap §7)
- **Schedule** = an event blueprint (template, texts in every language, actions/phases, stages, rank bands, claim days,
  reactor range, kingdoms) + a weekly rule: weekdays, start time (server time, UTC), duration (1–168 h), lead time
  (0–72 h), optional reactor brackets, enabled flag. Stored in `hadal.event_schedules` (one JSON document each).
- **Scheduler** (`EventService.Calendar`, every 30 s in the game tick): every occurrence that is running or starts within
  its lead time becomes an ordinary published event for **every kingdom** (or the blueprint's kingdoms) and every
  bracket, so the engine scores, ranks and pays it unchanged. The id is fixed — `depth-race:2026-11-02:w1`
  (`:b2` for the second bracket) — and every id created goes to the insert-only ledger `hadal.event_occurrences`:
  a restart, a second pass or an occurrence the Overseer deleted never makes it again. Editing a schedule changes only
  the occurrences created afterwards. Themed phases can rotate one slot a day (`rotatePhases`).
- **Defaults**, seeded once into an empty calendar (marker `seed:defaults` in the ledger), all editable and switchable:

  | Schedule | When (UTC) | What | Rewards (modest) |
  |---|---|---|---|
  | `depth-race` Depth Race (§4.4) | every day 00:00, 24 h | six 4-h themes: gathering, building power, research, training, creature hunting, hero growth; rotated daily; personal ranking per kingdom | stages 600/2,500/7,000 pts: 5k Titanium + 2× 5-min speed-up, 15k Titanium + bronze chest, silver chest + 1-h speed-up; ranks 1: 100 Hadalite + gold chest, 2–3: 50 + silver, 4–10: 20 + 2 bronze, 11–30: bronze (min 600 pts) |
  | `alliance-week` Alliance Week | Monday 00:00, 7 days | alliance contest on every main score kind (+ help) | stages 3k/12k/30k: 15k Titanium, silver chest, gold chest; alliance ranks 1: 200 Hadalite + gold, 2–3: 100 + silver, 4–10: 40 + 2 bronze (min 1,000 member pts) |
  | `login-week` Dive Log | Monday 00:00, 7 days | 7-day sign-in | 5k Titanium, 3× 5-min speed-up, 10k Food, bronze chest, 1-h speed-up, 5k Biomass, 50 Hadalite + silver chest |

  No weekend bonus window: the engine has no bonus template yet (§4.8); it comes with that template.
- **Panel**: the Events page opens with the Calendar — a 7-day grid (UTC days × kingdoms; created, planned, disabled and
  deleted occurrences), the schedule list (on/off, edit days/time/duration/lead/brackets/kingdoms, "create the next
  occurrence now", delete) and "Add to calendar" on any event (a disabled schedule with that event as blueprint). All
  changes are audited (`schedule.*`).
- **Client**: the event centre's first tab is the Calendar: a week strip from today (today gold), the chosen day's events
  with their windows in the phone's time and the template icon; a running event opens its tab, one not open yet shows
  its rules and rewards. Data: `ViewEventCalendar` → `EventCalendarDto` (texts in the asked language only); recurring
  events that have not started get no tab of their own.

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

### 5.1 As built (E2)
Rules in `AlliancePollRules` (HADAL.Shared), server in `SocialHub.Polls`, client page `SocialView.AlliancePolls`.
- **Kinds**: a *poll* (leader, diplomacy officers): question ≤120 characters, 2–6 different answers ≤60 characters,
  1–72 h (24 h default), single or multiple choice, anonymous or not. A *time-slot vote* (leader, war officers): an
  event (`meeting`, `lure_trap`, `trench_conquest`, `alliance_event`), an optional note, 2–5 different starts on the
  quarter hour, each after the vote ends and at most 14 days ahead, 24 h by default (1–72 h), single or multiple
  choice, never anonymous. At most 3 open votes per alliance.
- **Voting**: every member has one ballot and **may change it while the vote is open** (the latest counts; a ballot is
  1 option, or 1–n distinct options on multiple choice). Results are live for everyone: votes, share of voters and,
  unless anonymous, who chose what.
- **Closing**: when its time is up (the game tick checks every 15 s, so it also closes after a restart), or early by its
  creator or anyone allowed to open that kind. The winner is the option with most votes, a tie goes to the earlier
  option (for slots the earlier time); nobody voted → no winner. A summary goes to alliance chat and the alliance log:
  `sys.pollend` (question, answer, votes, voters), `sys.pollnone`, `sys.slotsend` (event, time in server time "2026-11-03
  17:00 UTC", votes, voters), `sys.slotsnone`; opening posts `sys.poll` / `sys.slots`. Texts are localization keys,
  read in each player's language.
- **Result for events**: a closed time-slot vote keeps its winning time; `SocialHub.DecidedSlot(allianceId, purpose)`
  returns the newest one for an event (E3/E4 read their start from it).
- **Storage**: `hadal.alliance_polls` (poll_id, alliance_id, is_open, data jsonb with the ballots, created_at,
  updated_at), created with `CREATE TABLE IF NOT EXISTS`. Each alliance keeps its open votes and its last 5 closed ones;
  a disband deletes its rows, and rows of alliances that no longer exist are dropped at start-up.
- **Client**: the hub's **Polls** tile (red dot while an open vote waits for the player's ballot; the HUD alliance
  button lights too) opens the page: the head card with **New vote**, the open votes as cards with the options as
  selectable rows (radio or tick mark, progress bar with votes and share, first voters' names), **Vote / Change vote**
  and **Close now**, then the recent results with the winner (green) or the fixed time. Times show in the phone's time
  with the server time beside them. **New vote** opens the dialog over the blurred page: kind tabs (when the player may
  open both), question and answers or event, note and times (±24 h, ±30 min), voting time, the switches.
- **Leader inactivity**: checked every 10 minutes. A leader offline 14 days hands over to the most powerful officer seen
  in the last 3 days; without one, the most powerful active member of the next rank down; nobody active → nothing
  happens. The old leader becomes an officer (a veteran when the officer seats are full) without a duty. The alliance
  reads `sys.inactive` in chat and the log and every member gets an alliance mail (`mail.alliance.leader.*`). A leader
  whose last visit was never recorded starts the 14 days at the first check; an online leader's last visit is refreshed
  hourly, so a server restart (no disconnect) cannot make them look long gone.
- **Overseer panel**: "İttifak Oylamaları" lists every alliance's votes read-only (search by tag or name); a game master
  can close one now (the summary still goes to chat) or delete it without a trace; both are audited (`poll.close`,
  `poll.delete`).

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
| E2 | Duty permissions; polls and time-slot votes; donations (built: §2.3, §5.1) | E1 |
| E3 | Trench Conquest (3 lanes, league, exchange shop) | E1, E2 |
| E4 | Rallies (D5); Alliance Hub on the map; Lure Trap | E2 |
| E5 | Abyss Siege (waves, reinforcement) | E4 |
| E6 | Alliance Duel; Mobilization | E1 |
| E7 | Beacons, territory bonuses, Rig, Depot; Vent War | E4, PvP |

Open: alliance buildings and the voting screen were not visible in Last Asylum because the test account has no
alliance; joining one there would show them.
