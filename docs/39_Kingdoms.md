# 39 — Kingdoms (Krallık)

Last Asylum splits its players into numbered kingdoms ("#166" on the profile). Each kingdom is a world of its
own; new kingdoms open as the game grows, and new players land in the newest one. HADAL does the same. The
`world_id` groundwork from docs/32 §2.7 (`"w1"`) becomes Kingdom #1.

## 1. Model

| Field        | Meaning                                                                        |
|--------------|--------------------------------------------------------------------------------|
| `id`         | `"w{number}"`: `w1` is Kingdom #1, which holds every player from before kingdoms |
| `number`     | 1, 2, 3… shown as "Krallık #n"                                                 |
| `name`       | optional display name (empty: "Krallık #n")                                    |
| `opened_at`  | when it opened                                                                 |
| `status`     | `open` (takes new players), `full` (cap or age reached), `closed` (admin)      |
| `player_cap` | new players stop when this many players belong to it (default 1,000)          |
| `open_days`  | new players stop this many days after opening (default 30; 0 = no limit)       |

Every player belongs to exactly one kingdom (`hadal.kingdom_members`: player, kingdom, joined_at).

**Assignment.** A brand-new account joins the newest kingdom whose status is `open`. If that kingdom has reached
its cap or its age limit, it turns `full` and the next kingdom (number + 1, same cap and window) opens at once,
and the player lands there. A player who exists but has no membership row (only possible before the migration
ran) joins the same way, which in practice is #1. An admin can open a new kingdom now, set any kingdom's status,
cap and window; `closed` and `full` kingdoms take no new players, but their players keep playing.

**Counting.** The cap counts members, not "active" players: a kingdom must not reopen because its players went
quiet. The admin page shows members and active (seen in the last 7 days) side by side.

## 2. What is per kingdom

| System              | Per kingdom                                                                                     |
|---------------------|--------------------------------------------------------------------------------------------------|
| World map           | One `WorldService` per kingdom: bases, nodes, monsters, event bosses, territory, cities, PvP, shields. A player is placed only on their kingdom's map and sees only it. |
| World chat          | Channel `world` is Kingdom #1 (the history stays where it is); other kingdoms use `world:w{n}`. |
| Alliances           | An alliance belongs to its leader's kingdom. Only players of that kingdom can find, join, apply to or be invited into it. Alliance chat, help, gifts and research follow from membership. |
| Alliance ranking    | Only the kingdom's alliances.                                                                   |
| Events              | `EventDefinition.Kingdoms`: empty = every kingdom, else the listed ids. Players outside a targeted kingdom do not see the event; event bosses spawn only on the targeted kingdoms' maps; event rankings are per kingdom. |
| PvP                 | Only within the kingdom (marches live on one map).                                              |
| Mail                | Player-to-player mail only within the kingdom (Last Asylum: you cannot write to another kingdom), so resource help cannot cross kingdoms either. The game's own mail (system, admin, events, battle reports) reaches anyone. |
| Profiles            | Viewable across kingdoms (from chat history or a shared location); the card shows the kingdom.  |

## 3. Server

- `KingdomService` (`server/Hadal.Game/Kingdoms/`) holds kingdoms and memberships in memory under its own lock
  and writes through (`IKingdomStore`: Postgres + in-memory). Lock order: world → kingdoms (leaf; it never calls out).
- `GameWorld` keeps one `WorldService` per kingdom (`MapOf(kingdomId)`, created and initialised when a kingdom
  opens); `GameWorld.Map` stays Kingdom #1 for old call sites and tests. Every in-memory world structure is keyed
  by the service instance, so kingdoms share nothing but the process. Ticks, settling, battle mails, boss payouts
  and the resident sweep run over every kingdom.
- `PlayerSession.KingdomId` is set when the session loads (connect or battle summon) and never changes in a
  session.
- The world store is per kingdom (`IWorldStore.ForWorld(id)`): every query filters on `world_id`, every insert
  writes it.

### 3.1 Migration (idempotent and reversible)

Runs at startup (`PostgresKingdomStore.InitializeAsync`, `PostgresWorldStore.InitializeAsync`); every step is a
no-op the second time:

1. `CREATE TABLE IF NOT EXISTS hadal.kingdoms`, `hadal.kingdom_members`.
2. `INSERT … 'w1', 1 … ON CONFLICT DO NOTHING` — Kingdom #1.
3. `INSERT INTO kingdom_members SELECT every player, 'w1' … ON CONFLICT DO NOTHING`.
4. World tables (all rows already have `world_id = 'w1'` from docs/32): the per-map keys become per kingdom —
   `world_bases UNIQUE (x, y)` → `UNIQUE (world_id, x, y)`, `world_nodes` and `world_regions` primary keys gain
   `world_id`. Each change checks `pg_constraint` first.

**Reverse** (only while every row is still in `w1`): see `server/Hadal.Game/Kingdoms/kingdoms_down.sql` — it
restores the old keys and drops the two kingdom tables. Take a DB backup before the first live start.

## 4. Client

- Profile card: "Krallık #n" under the name (own and others').
- World map HUD: "Krallık #n" next to the coordinates; tapping it opens the kingdom list.
- Chat: the world tab reads "Dünya · #n".
- Kingdom list (`ViewKingdoms` → `KingdomListDto`): number, opened date, players, status, "Senin krallığın".

## 5. Admin panel

Kingdoms page: number, name, status, members / active, cap, window, opened; *Yeni krallık aç*, status
(open / full / closed), cap and window. The player page shows the player's kingdom.

## 6. Later (not built)

- **Transfer (göç):** a "Krallık Göçü" item moves a player (base, not alliance) to another kingdom, with limits
  (power ceiling, not in an alliance, no marches out, cooldown). The membership row changes; the base is removed
  from one map and placed on the other.
- **KvK:** cross-kingdom events — a shared event map or a portal region where two kingdoms meet; scores per
  kingdom.
- **Merges:** two old kingdoms become one: bases relocate into the target map's free belt, alliances keep their
  members, world chat merges; done offline with a backup.
