# 33 — PvP fairness and combat integrity (checked against the genre research)

Source: `docs/research/Mobil_Strateji_Oyunlari_Arastirma.md` §5, §9, §10.2, §13, §15. This document lists what the
PvP/territory engine (docs/30, docs/32) already does, what was missing, and the rules adopted. Numbers are first values
for testing and live in `PvpRules` / `CombatRules` / `AllianceLandRules`.

## 1. PvP types, each with its own rules

| Type | Status | Own rules |
|---|---|---|
| Base attack | live | beginner protection, shield, power band, fatigue, protected resources, recovery shield (§2) |
| Gathering-team attack | live | no shield covers teams outside; the team's cargo is the loot |
| Scout | live | breaks the scout's shield; blocked by shields; reports troops, guards, lootable |
| Alliance rally | live | muster 1–10 min, 10 teams, minimum troops per team, joiners must reach the leader in time, contribution share (§3) |
| City control | live | neighbour rule, garrison refills, 6 cities max (docs/32 §2.4) |
| Alliance structures | live | hub cannot be attacked; buildings damaged, beacons fall (docs/32 §2.3) |
| Season war | later | timed points/regions/targets, partial reset, reward tiers — an event world (docs/32 §2.7) |
| Arena / duel | later | registered defence teams, daily tries, power bands; rewards must not widen power gaps |

## 2. Fairness

- **Beginner protection** (had): Core Reactor < 6 cannot attack or be attacked.
- **Power band** (changed after the user's feedback): nothing refuses an attack. When the attacker's power is more
  than **4×** the target's, the attack goes ahead but takes **half** the plunder (on top of fatigue), and the attack
  setup warns "Hedef senden çok zayıf; yağma azalır".
- **Attack fatigue** (new): per attacker and target, wins on the same base within **2 hours** cut the plunder:
  1st 100 %, 2nd 50 %, 3rd 25 %; a 4th attack is refused ("Fatigue").
- **Limited offline protection** (new): a base that loses **3 defences within 6 hours** gets a **2-hour recovery
  shield**, at most once per **24 hours** (not immunity; attacking breaks it like any shield). The win and defeat
  counts behind fatigue and recovery are stored (`hadal.world_fairness`), so a restart keeps them.
- **Protected resources** (changed): an attack takes 20 % of each lootable resource above its protected amount:
  storage cap × (10 % + 4 % per Shield Generator level), at least `reactor × 5,000`; Hadalite never. The player sees
  the protected amounts in the war window.
- **Wounded vs dead tied to the Med Bay** (changed): every fallen troop becomes wounded while its owner's Med Bay has
  room (capacity − wounded − healing − wounded already riding home); the rest die. Attackers always lose **25 %** of
  their fallen as dead (the price of attacking). The base owner's own fallen follow the Med Bay room only.
- **Clear result screens** (new client): a war-log line opens the battle report — both armies (start and lost per
  type), dead and wounded, loot, the player's contribution share, rounds, the battle id, seed and rule version.

## 3. Rally rules
- Muster window 1/3/5/10 min (had); capacity 10 teams (had); joiners must arrive before the muster ends (had).
- **Minimum troops** (new): every team in a rally brings at least **100** troops ("TooFewTroops").
- **Contribution** (new): losses and plunder are shared by troop share (had); each member's report shows their
  contribution % so rewards can follow contribution, not only the strongest members.

## 4. Combat integrity
- **Server decides, client shows** (had): the client never computes losses or loot.
- **Arrival-time settlement and order** (had, docs/30 §2).
- **Idempotency** (had): a command id runs once (`PlayerSession.Commands`); a battle record is settled once and
  dropped, so a re-sent command or a retried tick never pays twice.
- **Versioned snapshot + seed** (new): each PvP battle uses `CombatRules.Version` and a seed derived from the battle id;
  the resolver applies a narrow **RandomFactor 0.97–1.03** per side per round from that seed, so the same battle with
  the same rule version reproduces exactly. The report stores seed and version.
- **Counters/meta** (had, to grow): the armored → arc → harpoon → armored triangle, drillers as front line, front rows
  shield the back; hero classes lead their troop type. Control/support/AoE hero skills are the next step (heroes).

## 5. Map and marches
- A march carries composition, speed and arrival time (had); everything is timestamp-driven (had).
- **Recall** (had): attacks and scouts until arrival; reinforcements any time; rally members until the muster ends;
  a rally on its way cannot be recalled.
- **Speed-up** (fixed): an attack, scout or rally on its way out cannot be sped up (its battle time is fixed when it
  sets out, and a rally moves as one); a reinforcement on its way and any team on its way home can.
- **Target change**: not allowed; recall and send again.
- **Starter buffer** (new): no alliance hub, beacon or building in the outermost base belt (the Hadal Threshold where
  new bases land), so big alliances cannot fence in new players.

## 6. Win estimate before attacking
- `CombatEstimate.Of` (shared) runs `CombatRules` v2 without a seed on what the player can see: their chosen team with
  its heroes against the enemy army — a base from a scout report of the last 30 minutes, a city or building from its
  known garrison (guards standing at a building are not visible). It answers **Kazanır / Zor / Kaybeder**; with no
  scouted army it says "Bilinmiyor — önce keşif yap". A rally's estimate is for the player's own team.
- The real battle uses the armies at the arrival instant (and a narrow random factor), so it is an estimate.
