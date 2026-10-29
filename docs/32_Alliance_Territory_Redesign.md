# 32 — Alliance Territory, Flags, Alliance Buildings and Cities (redesign)

Status: replaces docs/30 §6 (fixed region grid with "Kale Lv N" labels), 2026-10-29. Reason: the user reviewed 0.9.0;
a fixed grid of labelled plots does not read as alliance territory.

## 1. What we saw (Last Asylum, account in [ITA] MadeInItaly, read-only)

- **Zoom levels** (mouse-wheel zoom on the phone):
  1. Close: castles with `[TAG] Name` plates and level chips; tapping an ally's castle offers *Takviye* (reinforce).
  2. Middle: castles become map pins (blue = my alliance, pink = leader, white = others, my own = avatar disc),
     resource and monster icons, a minimap with *Bölgem / İttifak Üyeleri / Lonca Lideri*, the kingdom banner
     (#166, king).
  3. Far: regions fill with their owner's colour (hatched), bright borders, and only the city labels remain
     (`[FNR] Şahin Kalesi Sv.3`, coloured by relation). The alliance's **rally flag** (a big purple banner,
     "İttifak Seferberlik Noktası") stands over the alliance's cluster of castles, independent of the regions.
- **Alliance menu**: İttifak Savaşı, Yardımı, Hediyesi, Üyeleri, Mağazası, Araştırması, Sıralama, **Seferberlik
  Noktası** (rally flag card with teleport), **Şehir Listesi**.
- **City List**: tabs *İşgal Edildi* / *İşgale Açık*, filters Lv.1–3…, each city with level, tappable coordinates,
  distance in km, owner; "Sahip olunan şehirler: 6/6"; **Şehir Bonusu**: "after a city is captured, alliance members
  enjoy its bonuses" — each city +5 % of one production (crops, grain, wood). "War can only be declared on
  neighbouring cities."
- The official site (lastasylum.com guides/FAQ) has no territory rules; the genre's flag model (Kingshot, Last War,
  RoK) fills the gap: HQ → banners that must connect → territory; facilities give bonuses and must be garrisoned.

## 2. HADAL model

Three layers, all server-authoritative, decided under the world lock (lock order world → hub → session).

### 2.1 Alliance Hub (İttifak Merkezi) — the flag
- R4/R5 plant it on a free tile where bases may stand, at least 3 tiles from any other alliance's land. Needs 3+
  members. One per alliance. R5 can move it (24 h cooldown) to a free tile inside the alliance's land.
- It claims a square of radius `5 + level` (Lv 1–5 → 6…10 tiles).
- **Levels by donations**: members donate Titanium/Biomass/Food to the hub; each resource unit is a point;
  level n → n+1 needs `40,000 × n²` points. Donating also gives alliance contribution (logged).
- It is the alliance's **rally point** (the rally card, the alliance teleport lands next to it).

### 2.2 Beacons (Sinyal Şamandırası) — the territory grows
- R4/R5 place a beacon on a free tile within 2 tiles of the alliance's land and not on another alliance's land.
  It claims radius 3 once **built** (10 minutes after placing).
- Limit: `min(30, 2 × hubLevel + members / 5)` — the territory grows with the hub's level and the member count.
- Cost to the officer who places it: `15,000 × (n + 1)` Titanium for the n-th beacon.
- **Connection**: a beacon counts only while its square touches the hub's square or a counted beacon's square.
  A beacon cut off (its neighbour removed or destroyed) claims nothing until reconnected.
- Tiles already claimed by another alliance stay theirs (first come); squares are applied in creation order.

### 2.3 Alliance buildings (kazanç yapıları)
Placed by R4/R5 inside the alliance's land, unlocked by member count and alliance power, **built by members
sending teams** (each team adds build points per second by its troops; the building is ready at 100 %), **upgraded
by donations** (Lv 1–5), **defended** by members' reinforcing teams, and **attackable** by enemies.

| Building | Unlock | Bonus per level (members, inside the land unless noted) |
|---|---|---|
| Basınç Rölesi (Pressure Relay) | 5 members | +3 % defence for member bases in the land |
| İttifak Sondajı (Alliance Rig) | 10 members, 1 M power | +5 % gathering speed in the land |
| Akıntı Topu (Current Cannon) | 15 members, 3 M power | +3 % attack for teams leaving bases in the land |

- Durability `1,000 × level`. An attack the garrison loses takes durability equal to the attackers' surviving
  power / 100; at 0 the building is **damaged** (no bonus) until repaired (refills 10 % per hour).
- Structures (hub, beacons, buildings) are attackable once enemy land touches them is *not* required (keeps PvP
  simple at our player count); shields do not cover them.

### 2.4 Cities (Şehirler) — the old strongholds, as LA's City List
- The 100 strongholds stay as **cities** (points, not regions): level by belt, an NPC garrison, +5 % gathering of
  one resource for every member while owned (Biomass, Titanium or Food by city index).
- Capture by attack/rally only if the city lies within 4 tiles of the alliance's land ("neighbouring cities").
  Limit `min(6, 1 + members / 3)` owned cities. The Throne (Lv 7) stays closed.
- The map no longer fills regions per city. City labels show only for my alliance's cities, under attack ones, and
  cities near the camera at close zoom.

### 2.5 Map
- Strategic zoom: alliance land filled with the alliance colour, bright borders at the edge of each alliance's land;
  hubs as big flags, beacons as small flags; bases as dots; cities as small icons.
- Close zoom: hub, beacons and buildings as plates with an icon, tap for the menu (info, donate, build, defend,
  attack, remove). An empty tile's menu offers *Merkez Kur* / *Şamandıra Dik* / *Yapı Kur* when allowed.

### 2.6 Seeing each other, and knowing who helps
- Alliance members' bases are visible as such everywhere: blue plates/pins at close and middle zoom, blue dots on the
  strategic map, and a member list on the minimap jump (*İttifak Üyeleri*) — as in LA.
- Reinforcements are visible to the player they protect: their PvP status lists the teams standing at their base
  (who sent it, troops, since when), the base shows a shield-and-person badge with the count, and the sender sees
  their team as "guarding [Name]" in the team queue. Arrival and departure of a reinforcement notify the base's
  owner (toast + war log line). The alliance war list shows which incoming attacks already have reinforcements.

### 2.7 Foundations for several worlds
Later there will be several worlds (kingdoms, servers), wars between worlds, and possibly teleports between worlds
in events. The foundations now:
- Every world-scoped record carries a **world id** (`w1` today): bases, nodes, monsters, PvP records, alliance
  structures, cities. Tables get a `world_id` column; DTOs carry `WorldId`.
- A player has a **home world** (where they were placed) and a **current world** (where their base stands);
  alliances belong to a home world; their land and structures exist in one world.
- `WorldService` instances are keyed by world id; the game routes a player's map commands to the world their base
  stands in. One instance today.
- A cross-world teleport (later) = remove the base from one world and place it in another, carrying marches only when
  all teams are home (the same rule as a teleport today). A cross-world war = a timed event world both sides
  teleport into; battles still settle on that world's timeline.
- Alliance ids, player ids and record ids stay globally unique (GUIDs / prefixed ids) so records can move between
  worlds without collisions.

## 3. Build order
| Phase | Content |
|---|---|
| A | Hub + beacons, land computation, donations and hub levels, rally point/teleport/gather bonus from the land, cities as points with the neighbour rule and fewer labels, strategic map fill. |
| B | Alliance buildings: placing, team construction, donations, garrison, attack/durability, bonuses. |
| C | City List and alliance-page entries (hooks for the alliance page), polish. |
