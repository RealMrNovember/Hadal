# 15 — Monetization (Gacha / Capsule Systems)

**Document Version:** 3.0 — Production MMO Hardened  
**Genre:** AAA Mobile Strategy MMO  
**Currency:** Hadalite (premium) · soft currencies per [19_Economy.md](./19_Economy.md)

**Companion docs:** [17_Technical_Architecture.md](./17_Technical_Architecture.md) · [18_Unity_Client_Architecture.md](./18_Unity_Client_Architecture.md) · [21_HADAL_Shared_Protocol_And_Serialization.md](./21_HADAL_Shared_Protocol_And_Serialization.md) · [19_Economy.md](./19_Economy.md)

---

## 1. Quiet Luxury Monetization Philosophy

HADAL monetization follows **Quiet Luxury** — premium without spectacle.

| Rule | Enforcement |
|------|-------------|
| **No aggressive pop-ups** | No forced interstitial purchase screens on login, expedition return, or defeat |
| **No cheap gold VFX** | No coin explosions, slot-machine chrome, or arcade jackpot framing |
| **No "SALE" / countdown pressure** | Forbidden in UI copy and art |
| **Minimal dark UI** | High contrast, restrained typography, bioluminescent accent only |
| **Premium store name** | **The Syndicate** (alt internal codename: *The Black Market*) — never "Shop" or "Store" in player-facing premium UI |

Design alignment: [13_UI_UX.md](./13_UI_UX.md) · [14_Art_and_Sound.md](./14_Art_and_Sound.md)

---

## 2. Gacha Systems Overview

HADAL operates **two distinct server-authoritative gacha pipelines**:

| System | Target pool | Fantasy |
|--------|-------------|---------|
| **Cryo-Pod Salvage** | Heroes | Deep-sea retrieval and thawing of frozen life pods |
| **Deep Sea Sonar Ping** | Equipment · hull plates · blueprints | Sonar pulses into the abyss revealing buried tech |

Both consume **Hadalite** (or promotional vouchers issued server-side). Client never selects rewards.

---

## 3. Cryo-Pod Salvage (Hero Gacha)

### Fantasy

Dondurulmuş **yaşam kapsülleri** Hadal trench'den kurtarılır. Oyuncu kapsül açılışını yalnızca **görsel/işitsel** olarak deneyimler; içerik sunucudan gelir.

### Rarity Tiers

| Tier | Code | Notes |
|------|------|-------|
| Common | `COMMON` | Crew specialists |
| Uncommon | `UNCOMMON` | Faction-aligned units |
| Rare | `RARE` | Class-defining kits |
| Epic | `EPIC` | Faction legendaries |
| Legendary | `LEGENDARY` | Named heroes |
| Mythic | `MYTHIC` | Ultra-rare narrative heroes |
| **Mythic (Featured)** | `MYTHIC_FEATURED` | Includes **Aelis** and rotation banner heroes |

**Aelis** is a Mythic-tier featured hero — lore: [10_Heroes.md](./10_Heroes.md).

### Pull Rules

- **Single Salvage:** 1× Cryo-Pod · fixed Hadalite cost (config: `gacha_cryo_single_cost`)
- **Multi Salvage (10×):** Amortized cost · **guaranteed ≥ Rare** on 10th slot (server rule, not client display math)
- Duplicate heroes convert to **Memory Fragments** (server-calculated compensation table)
- Faction pity counters tracked per banner independently

---

## 4. Deep Sea Sonar Ping (Equipment / Blueprint Gacha)

### Fantasy

Karanlığa **sonar dalgası** gönderilir. Dönüş: nadir denizaltı parçaları, gövde zırhları, drone blueprint'leri, pressure-module schematics.

### Pool Categories

| Category | Examples |
|----------|----------|
| **Hull Plates** | Pressure-resistant segments |
| **Submarine Modules** | Engine, sonar array, cargo bay |
| **Weapon Blueprints** | Torpedo racks, depth charges |
| **Tech Schematics** | Research acceleration items |
| **Nano Alloy Shards** | Links to [07_Pressure_System.md](./07_Pressure_System.md) |

### Pull Rules

- Single Ping / Multi Ping (5× or 10×) — costs defined server-side
- Blueprint duplicates → **Fragment conversion** (tier-scaled)
- No hero items in Sonar pool (strict table separation)

---

## 5. Server-Authoritative Security Architecture

### Absolute Rules

| # | Rule |
|---|------|
| 1 | Client **MUST NOT** roll RNG |
| 2 | Client **MUST NOT** choose or preview authoritative reward before server ack |
| 3 | Client **MUST NOT** mutate inventory on gacha UI confirm |
| 4 | All results arrive via **Protobuf** `GachaResultDelta` |
| 5 | All pulls logged in PostgreSQL audit table |

### Command Flow

```
Player taps Salvage / Ping (UI — The Syndicate)
    │
    ▼
Client: optimistic UI lock + local animation stub ONLY
    │
    ▼
Client: ExecuteGachaCommand (Protobuf CommandEnvelope)
    │     fields: command_id, client_sequence, banner_id, pull_count, gacha_type
    ▼
Gateway: auth · rate limit · schema version
    ▼
Game Server:
    1. Validate Hadalite balance (PostgreSQL)
    2. Validate banner active window + player eligibility
    3. Deduct currency (transactional)
    4. Roll rewards via Secure PRNG (see §6)
    5. Apply pity state updates
    6. Persist inventory + audit log
    7. Emit GachaResultDelta
    ▼
Client: NetworkStateReceiver → StateSyncPipeline → VisualStateCache
    ▼
Translator → LocalEventBus → GachaRevealPresenter (VFX / Audio ONLY)
    ▼
RollbackAnimator: if prediction mismatch (should not predict reward) → reconcile UI lock
```

### Wire Messages (HADAL.Shared / proto)

| Message | Direction | Format |
|---------|-----------|--------|
| `ExecuteGachaCommand` | Client → Server | Protobuf |
| `GachaResultDelta` | Server → Client | Protobuf |
| `CommandResult` | Server → Client | Protobuf (failure path) |

JSON is **forbidden** on gameplay WebSocket — [21_HADAL_Shared_Protocol_And_Serialization.md](./21_HADAL_Shared_Protocol_And_Serialization.md).

### Client Presentation Boundary

- `GachaRevealPresenter` subscribes to **LocalEventBus only**
- Sonar **PING** audio synced to reveal timing — not to RNG moment
- Preview "odds screen" reads static config catalog (Addressables) — **not** live drop tables (server secret)

---

## 6. Secure Random & Drop Tables

### PRNG

| Component | Implementation |
|-----------|----------------|
| **Generator** | `System.Security.Cryptography.RandomNumberGenerator` (server) |
| **Seed** | Per-pull cryptographic nonce — never client-provided |
| **Audit** | Store `pull_id`, `server_tick`, `rng_nonce_hash`, `table_version` |

Drop tables live in PostgreSQL + versioned JSONB (`gacha_tables`) — **not** in client bundles.

### Drop Rate Model

```sql
-- Conceptual (hadal schema)
gacha_banners (banner_id, gacha_type, start_at, end_at, table_version)
gacha_drop_rates (table_version, item_id, rarity, weight, pool)
player_gacha_state (player_id, banner_id, pulls_since_rare, pulls_since_featured, soft_pity_counter, hard_pity_counter)
gacha_audit_log (pull_id, player_id, command_id, results_jsonb, created_at)
```

Weights are **relative** — server normalizes at roll time.

---

## 7. Pity & Cumulative Probability

### Definitions

| Term | Meaning |
|------|---------|
| **Soft pity** | Gradual weight increase after pull threshold (e.g. 62+) |
| **Hard pity** | Guaranteed featured / mythic at hard cap (e.g. 90) |
| **Featured rate-up** | Subset of Legendary/Mythic pool for active banner |

### Cryo-Pod Salvage (Hero) — Default Template

| Pull range | Behavior |
|------------|----------|
| 1 – 61 | Base table weights |
| 62 – 89 | Soft pity: +0.8% Legendary/Mythic weight per pull over 61 (cumulative, server-side formula in `HADAL.Shared.Simulation.GachaPityFormula`) |
| 90 | **Hard pity:** guaranteed Mythic or Featured Legendary (banner rules) |
| 10× multi | Slot 10 minimum Rare; pity counters increment per slot |

### Deep Sea Sonar Ping — Default Template

| Pull range | Behavior |
|------------|----------|
| 1 – 39 | Base table |
| 40 – 69 | Soft pity toward Epic blueprint |
| 70 | Hard pity: guaranteed Epic+ blueprint |
| Duplicate handling | Fragments per `duplicate_conversion` table |

### Multi-Pull Amortization

- **10× Cryo:** `cost = single_cost × 10 × 0.90` (example — exact multiplier server config)
- Pity increments **once per slot** (10 increments per 10×)
- Server returns ordered `GachaRollResult[]` in single `GachaResultDelta`

### Banner Independence

Each banner maintains isolated `player_gacha_state` row — rotating Aelis banner does not reset equipment ping pity.

---

## 8. Failure & Edge Cases

| Scenario | Server behavior | Client behavior |
|----------|-----------------|-----------------|
| Insufficient Hadalite | `CommandResult.REJECTED` | Show Syndicate balance hint — no animation |
| Banner expired | Reject | Refresh banner list from snapshot |
| Schema mismatch | `SCHEMA_INCOMPATIBLE` | Force patch flow |
| Network timeout | Idempotent retry via `command_id` | Keep UI locked until ack or fail |
| Duplicate command | Return original `GachaResultDelta` | Replay same reveal |

---

## 9. Compliance & Live Ops Hooks

- Published **odds disclosure** (legal) sourced from server table version hash — static page on CDN, not gameplay wire
- Age-gating / spend limits: server-side account flags
- Live Ops banner rotation: [16_Live_Ops.md](./16_Live_Ops.md)

---

## 10. Implementation Checklist (Phase Reference)

| Item | Owner | Doc |
|------|-------|-----|
| `ExecuteGachaCommand` proto | HADAL.Shared | 21 |
| `GachaResultDelta` proto | HADAL.Shared | 21 |
| `GachaPityFormula` | HADAL.Shared | 21 |
| Server roll service | Game Server | 17 |
| `GachaRevealPresenter` | Unity Presentation | 18 |
| Audit tables | PostgreSQL | 17 / SERVER |

---

## 11. Risk Assessment

| Area | Risk | Mitigation |
|------|------|------------|
| Server-authoritative rolls | **LOW** | Industry standard MMO pattern |
| Pity complexity | **MEDIUM** | Unit tests on `GachaPityFormula` |
| UX without prediction | **LOW** | Animation lock + fast Protobuf ack |
| Regulatory odds disclosure | **MEDIUM** | CDN static + table version hash |

---

**Document Version:** 3.0  
**Supersedes:** N/A (initial monetization spec)
