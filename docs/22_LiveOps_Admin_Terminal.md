# 22 — LiveOps Admin Terminal (The Overseer)

**Document Version:** 3.1 — Production MMO Hardened  
**Codename:** **The Overseer Terminal**  
**Operator:** Cicibyte Corp · Game Master / LiveOps  
**Public URL (planned):** `https://hadal.cicibyte.com/admin`  
**Alternate (recommended prod):** `https://overseer.hadal.cicibyte.com` (isolated vhost, IP allowlist)

**Companion docs:** [15_Monetization.md](./15_Monetization.md) · [16_Live_Ops.md](./16_Live_Ops.md) · [17_Technical_Architecture.md](./17_Technical_Architecture.md) · [HADAL_SERVER_INFRASTRUCTURE.md](./HADAL_SERVER_INFRASTRUCTURE.md)

---

## 1. Purpose

The Overseer Terminal is HADAL's **God Mode command center** — a server-authoritative LiveOps and GM panel for:

- Player support, moderation, and CRM
- Global mail and reward campaigns
- Economy and gacha hot-swap (drop rates, pity, gift codes)
- Real-time server telemetry (CCU, Redis, PostgreSQL, Gateway)

It is **not** a player-facing surface. It must never share authentication, cookies, or WebSocket paths with the game client.

---

## 2. Quiet Luxury Panel Design

Classic cluttered admin dashboards are **forbidden**. The Overseer follows the same **Quiet Luxury** language as [15_Monetization.md](./15_Monetization.md) and [13_UI_UX.md](./13_UI_UX.md).

| Rule | Implementation |
|------|----------------|
| **Dark mode only** | `#0A0E14` base · bioluminescent accent `#2EE6C5` · alert `#E85D4C` |
| **No visual noise** | No gradients, no stock illustrations, no badge spam |
| **Typography** | Sharp sans (e.g. Geist, IBM Plex Sans) · monospace for IDs/logs (JetBrains Mono) |
| **Data tables** | High contrast, zebra rows, sticky headers, server-side pagination |
| **Motion** | Subtle — no celebratory animations on GM actions |
| **Terminology** | "Overseer", "Signal", "Dispatch" — never "Admin Panel v2" or "SALE" |

### Stack (frontend)

| Layer | Choice |
|-------|--------|
| Framework | **Next.js** (App Router) · React 19 · TypeScript |
| Delivery | Static export or SSR container — served behind nginx |
| State | TanStack Query for Admin API · no client-side game state |
| Auth UI | TOTP 2FA step after JWT login — no "remember this device" on prod |

The SPA **never** holds PostgreSQL credentials, Redis passwords, or gacha RNG seeds.

---

## 3. System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│  GM Browser (Cicibyte Corp · IP allowlist · VPN optional)               │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │ HTTPS
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  nginx (hadal.cicibyte.com vhost ONLY)                                  │
│    /admin          → hadal-overseer-ui (static/Next.js)                 │
│    /admin/api/*    → hadal-admin-api (ASP.NET Core)                     │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
        ┌───────────────────────┴───────────────────────┐
        ▼                                               ▼
┌───────────────────┐                         ┌───────────────────┐
│ hadal-overseer-ui │                         │ hadal-admin-api   │
│ (Next.js SPA)     │                         │ JWT + 2FA + RBAC  │
└───────────────────┘                         └─────────┬─────────┘
                                                        │
                        hadal_isolated_net (Docker)     │
        ┌───────────────────────────────────────────────┼──────────────┐
        ▼               ▼               ▼               ▼              ▼
  hadal-game-api  hadal-gateway  hadal-redis   hadal-postgres  hadal-game-server
        │               │               │               │              │
        └───────────────┴───────────────┴───────────────┴──────────────┘
                    Admin API orchestrates — NEVER exposes raw DB port
```

### Absolute rules

| # | Rule |
|---|------|
| 1 | Panel **MUST NOT** connect directly to PostgreSQL or Redis |
| 2 | All mutations go through **Admin API** → Game Server domain services |
| 3 | Every GM action writes **AdminActionAuditLog** (append-only) |
| 4 | Gacha table changes require **two-person approval** (optional flag) in prod |
| 5 | Gameplay WebSocket remains **Protobuf-only** — Admin API uses JSON REST |

---

## 4. Modular Management Structure

### 4.1 Player Management & CRM

| Capability | Behavior |
|------------|----------|
| **Search** | Player ID, display name, device ID, alliance ID, email hash |
| **Profile view** | Read-only snapshot from `player_state` + session metadata |
| **Inventory edit** | Hadalite, resources, heroes, buildings — via validated **AdminCommand** (not SQL) |
| **Moderation** | Ban (account/device), mute, chat restriction, shadow flag |
| **Support notes** | Internal CRM notes — not visible to player |

**Anti-cheat:** All manual grants logged with `admin_user_id`, `reason_code`, `ticket_id`.

### 4.2 LiveOps Mail & Reward Distribution

| Capability | Behavior |
|------------|----------|
| **Global mail** | Title, body, attachment package, expiry — queued server-side |
| **Segmentation** | All players · alliance · depth zone · cohort · CSV upload (validated) |
| **Attachments** | Item packages defined in `reward_packages` table — server resolves IDs |
| **Scheduling** | Send at UTC timestamp · cancel before dispatch |
| **Idempotency** | `campaign_id` prevents duplicate sends on retry |

Player client receives rewards via **StateDelta / mail inbox replication** — not client-side grant.

### 4.3 Economy & Gacha Control Room

Integrates with [15_Monetization.md](./15_Monetization.md) server-authoritative gacha.

| Capability | Behavior |
|------------|----------|
| **Banner management** | Activate/deactivate Cryo-Pod · Deep Sea Sonar banners |
| **Drop rates (hot-swap)** | New `gacha_drop_rates` version row → `table_version` bump → zero client deploy |
| **Pity tuning** | Soft/hard pity thresholds per banner — **server formula only** (`GachaPityFormula`) |
| **Gift codes** | Batch generate · max redemptions · expiry · segment lock |
| **Simulation** | Monte Carlo preview on server (Admin API) — **never** on client |

**Hot-swap flow:**

```
GM edits rates in Overseer → Admin API validates → new table_version in PostgreSQL
→ Redis cache invalidate (banner config key) → next ExecuteGachaCommand uses new table
→ Audit log: GACHA_TABLE_PUBLISH
```

### 4.4 Server Telemetry

| Metric | Source | Refresh |
|--------|--------|---------|
| **CCU** | Gateway session registry (Redis `HADAL:ccu`) | 5s |
| **Gateway WS** | Connection count, msg/s, p99 latency | 10s |
| **Redis** | Memory, connected clients, ops/s, hit ratio | 10s |
| **PostgreSQL** | Active connections, pool wait, slow query count | 30s |
| **Command rate** | `commands_log` aggregate | 60s |
| **Gacha pulls/min** | `gacha_audit_log` aggregate | 60s |

Telemetry is **read-only** via Admin API health aggregators — Redis `INFO` and PG stats pulled by Admin API service account, not browser.

---

## 5. Security Architecture

### 5.1 Admin API Layer (ASP.NET Core)

Isolated service: **`hadal-admin-api`** — separate from public Game API.

| Concern | Implementation |
|---------|----------------|
| **Auth** | JWT (short TTL 15m) + refresh token (HttpOnly, Secure, SameSite=Strict) |
| **2FA** | TOTP (RFC 6238) required for all GM roles in production |
| **RBAC** | Roles: `Viewer`, `Support`, `LiveOps`, `Economy`, `SuperOverseer` |
| **Network** | Bind `127.0.0.1:19006` only · nginx reverse proxy · IP allowlist at nginx |
| **Rate limit** | 60 req/min per admin user · stricter on economy endpoints |
| **CORS** | Allow only Overseer UI origin |

### 5.2 AdminActionAuditLog

Append-only table in `hadal` schema:

```sql
-- Conceptual
admin_action_audit (
  id              BIGSERIAL PRIMARY KEY,
  admin_user_id   UUID NOT NULL,
  action_type     TEXT NOT NULL,      -- e.g. PLAYER_BAN, GACHA_TABLE_PUBLISH
  target_type     TEXT,               -- player, banner, campaign
  target_id       TEXT,
  payload_jsonb   JSONB NOT NULL,
  ip_address      INET,
  user_agent      TEXT,
  created_at      TIMESTAMPTZ DEFAULT NOW()
);
```

Retention: **7 years** (compliance) · no UPDATE/DELETE grants for app role.

### 5.3 Forbidden patterns

| Forbidden | Why |
|-----------|-----|
| Direct DB credentials in browser | Full exposure on XSS |
| Shared JWT with game client | Privilege bleed |
| Gacha RNG in Admin UI | Cheat surface |
| `/admin` on same cookie domain as `/api/v1` player auth | Session fixation risk |
| Global nginx reload without `nginx -t` | Breaks other projects on host |

---

## 6. Admin API Endpoints (Contract Sketch)

Base: `https://hadal.cicibyte.com/admin/api/v1`

| Method | Path | Role | Description |
|--------|------|------|-------------|
| POST | `/auth/login` | public | Username/password → JWT challenge |
| POST | `/auth/totp` | public | TOTP verify → full JWT |
| GET | `/players/search` | Support+ | Search CRM |
| GET | `/players/{id}` | Support+ | Profile snapshot |
| POST | `/players/{id}/moderate` | Support+ | Ban/mute |
| POST | `/players/{id}/grant` | LiveOps+ | Resource/item grant |
| POST | `/campaigns/mail` | LiveOps+ | Schedule mail |
| GET | `/gacha/banners` | Economy+ | List banners |
| PUT | `/gacha/banners/{id}/rates` | Economy+ | Hot-swap drop table |
| POST | `/gacha/gift-codes` | Economy+ | Generate codes |
| GET | `/telemetry/summary` | Viewer+ | CCU + health dashboard |
| GET | `/audit` | SuperOverseer | Audit log query |

All responses JSON. Gameplay mutations trigger internal Game Server commands — same validation as player commands where applicable.

---

## 7. Deployment & URLs

| Environment | UI | API |
|-------------|----|----|
| Production | `https://hadal.cicibyte.com/admin` | `https://hadal.cicibyte.com/admin/api/v1` |
| Production (alt) | `https://overseer.hadal.cicibyte.com` | same host `/api/v1` |
| Staging | `https://hadal.cicibyte.com/admin-staging` | isolated port / schema |

See [HADAL_SERVER_INFRASTRUCTURE.md](./HADAL_SERVER_INFRASTRUCTURE.md) §4–§6 for ports, Docker, nginx.

---

## 8. Phase Dependencies

| Requires | Blocker for |
|----------|-------------|
| M2 Auth API | Admin login federation |
| M4 StateSnapshot | Accurate player CRM view |
| M5 Resource tick | Economy grants |
| Gacha tables (M5+) | Gacha Control Room |
| **M7 Admin API** | **All Overseer modules** |
| **M8 Overseer UI** | GM daily operations |
| **M9 Audit + 2FA** | Production GM access |

Roadmap: [HadalDevelopmentRoadmap.md](./HadalDevelopmentRoadmap.md)

---

## 9. Risk Assessment

| Area | Risk | Mitigation |
|------|------|------------|
| Admin API breach | **HIGH** | JWT + 2FA + IP allowlist + audit |
| Hot-swap gacha misconfig | **HIGH** | Staging publish · simulation · two-person approval |
| Direct DB temptation | **MEDIUM** | Architecture forbid · code review |
| Telemetry overload | **LOW** | Redis pipelining · cached aggregates |
| UI XSS | **MEDIUM** | CSP strict · no inline scripts |

---

## 10. Production Compliance (v3)

- Aligns with server-authoritative model ([17_Technical_Architecture.md](./17_Technical_Architecture.md))
- Gacha changes server-side only ([15_Monetization.md](./15_Monetization.md))
- Isolated deployment — no impact on other projects on `31.40.199.47` ([HADAL_SERVER_INFRASTRUCTURE.md](./HADAL_SERVER_INFRASTRUCTURE.md))
- Quiet Luxury consistent with player-facing UX principles

---

**Document Version:** 3.1  
**Owner:** HADAL Platform · Cicibyte Corp LiveOps
