# HADAL Server Infrastructure

> **Current deployment (2026-10-25) — supersedes the aaPanel plan below.**
>
> | | |
> |---|---|
> | Host | DL380 (Proxmox) · LXC **CT 160 `hadal`** · Debian 13 · Docker |
> | Stack | `server/docker-compose.hadal.yml`: gateway (127.0.0.1:19001), PostgreSQL 16 (:19005), Redis 7 (:19004) |
> | Public | **`hadal-api.cicibyte.com`** via Cloudflare Tunnel `hadal` (no open ports). `hadal.cicibyte.com` stays the teaser site on the old server |
> | Endpoints | `wss://hadal-api.cicibyte.com/gateway` · `POST /api/v1/auth/guest` · `POST /api/v1/auth/refresh` · `GET /manifest/version.json` · `GET /health` |
> | Deploy | `server/deploy/deploy.sh` (tests → build → rollout → health check → automatic rollback) |
> | Tunnel setup | one-time, by the operator: `ssh cicibyte@dl380 'bash -s' < server/deploy/setup-tunnel.sh` |
> | Secrets | `/opt/hadal/server/.env` on the CT only (`HADAL_DB_PASSWORD`, `HADAL_JWT_SECRET`) |
> | Runbook | [§14](#14-operations-runbook-ct-160): server tests with a throwaway database, backups, restore drill, real restore, balance rollback |
>
> Sections below describe the original single-server aaPanel plan and are kept for the CDN/admin design.

**Domain:** `hadal.cicibyte.com`  
**Server IP:** `31.40.199.47`  
**Web Root:** `/www/wwwroot/hadal.cicibyte.com`  
**Status:** Planned — isolated deployment (does NOT touch other projects)  
**Operator Access:** Full CiciByte Technology, Inc. authority within `/www/wwwroot/hadal.cicibyte.com/` and HADAL Docker stack only  
**Document Version:** 3.1  
**Protocol:** Protobuf (gameplay WebSocket) · JSON (patch manifest / auth HTTP only)

---

## 1. Safety Principles (Mandatory)

This server hosts **multiple active production projects**. All HADAL work MUST follow:

| Rule | Description |
|------|-------------|
| **Isolation** | HADAL uses its own directory, nginx vhost, Docker network, ports, and DB schema only |
| **No global changes** | Do NOT modify shared nginx main config, other vhosts, or system-wide services without explicit approval |
| **No port conflicts** | Verify ports are free before binding; document every assigned port |
| **Rollback ready** | Every change must be reversible within 5 minutes |
| **Staging first** | Use `/patch/` and `/api/v1/health` endpoints before routing live traffic |
| **Read-only audit** | Before any install: `ss -tlnp`, `docker ps`, `nginx -T` (grep hadal only) |

---

## 2. Infrastructure Purpose

HADAL backend serves **server-authoritative** game state. Unity client is a view + prediction display layer only.

**Shared contracts:** Backend MUST deploy `HADAL.Shared` assembly (same Protobuf schemas as client). See [21_HADAL_Shared_Protocol_And_Serialization.md](./21_HADAL_Shared_Protocol_And_Serialization.md).

| Service | Role | Wire format |
|---------|------|-------------|
| **CDN / Static** | Addressables catalog, patch bundles, version manifest | JSON manifest OK |
| **Gateway** | WebSocket entry, schema handshake, session routing | **Protobuf binary** |
| **Game API** | Auth, session bootstrap | JSON HTTP + **Protobuf snapshot body** |
| **Game Server** | Simulation, validation (HADAL.Shared rules) | Internal + Protobuf out |
| **Redis** | Session cache, pub/sub replication | Binary blobs |
| **PostgreSQL** | Persistent state (`hadal` schema) | Normalized / JSONB internal only |
| **Analytics** | Optional — isolated pipeline | — |
| **Admin API** | The Overseer Terminal backend — JWT + 2FA, audit log | JSON REST (admin only) |
| **Overseer UI** | Next.js GM panel — Quiet Luxury dark SPA | Static / SSR via nginx |

See [22_LiveOps_Admin_Terminal.md](./22_LiveOps_Admin_Terminal.md).

---

## 3. Directory Layout (Server)

All paths under web root — **no files outside this tree for HADAL**:

```
/www/wwwroot/hadal.cicibyte.com/
├── cdn/                          # Addressables + patch delivery
│   ├── catalog/                  # Remote catalog JSON + hash
│   │   ├── catalog_1.0.0.json
│   │   └── catalog_1.0.0.hash
│   ├── bundles/                  # Asset bundles (content-addressed)
│   └── manifest/
│       └── version.json          # Client patch check entry point
├── api/                          # Reverse-proxy to Game API (internal)
├── gateway/                      # Reverse-proxy to WebSocket Gateway (internal)
├── static/                       # Landing, status page, legal
├── logs/                         # HADAL-only logs (rotated)
├── admin/                        # Overseer UI static export (optional — or container)
└── deploy/                       # Docker compose, env templates (NOT public)
    ├── docker-compose.hadal.yml
    ├── .env.example
    ├── nginx-hadal.conf.snippet
    └── shared/                       # HADAL.Shared + proto (deployed with server images)
```

**Gameplay wire rule:** WebSocket frames are **Protobuf only**. JSON is NOT used for Command, StateSnapshot, or StateDelta on the gateway.

**Public URLs (target):**

| URL | Purpose |
|-----|---------|
| `https://hadal.cicibyte.com/manifest/version.json` | PatchManager version check |
| `https://hadal.cicibyte.com/cdn/catalog/*` | Addressables remote catalog |
| `https://hadal.cicibyte.com/cdn/bundles/*` | Bundle download |
| `wss://hadal.cicibyte.com/gateway` | WebSocket Gateway |
| `https://hadal.cicibyte.com/api/v1/*` | REST API (auth, commands) |
| `https://hadal.cicibyte.com/health` | Public health (no internal data) |
| `https://hadal.cicibyte.com/admin/` | **The Overseer Terminal** — live. Kale nginx extension `hadal-overseer.conf` proxies to the gateway with `X-Overseer-Key` (docs/22 §6.1) |
| `https://hadal.cicibyte.com/admin/api/*` | Admin API (password + TOTP → Bearer) |
| `https://hadal.cicibyte.com/download/hadal.apk` | Android test build, served from kale (same extension) |
| `https://overseer.hadal.cicibyte.com` | Alternate isolated vhost (recommended prod) |

---

## 4. Port Allocation (Proposed — Verify Before Bind)

Use **internal ports only**; nginx terminates SSL and reverse-proxies.

| Service | Internal Port | Notes |
|---------|---------------|-------|
| HADAL Gateway (WS) | `19001` | Verify free: `ss -tlnp \| grep 19001` |
| HADAL Game API | `19002` | REST |
| HADAL Game Server (gRPC/internal) | `19003` | Not public |
| Redis (HADAL instance) | `19004` | Dedicated container, NOT shared Redis |
| PostgreSQL (HADAL) | `19005` | Dedicated container OR schema-only on isolated instance |
| HADAL Admin API | `19006` | The Overseer backend — localhost only |
| HADAL Overseer UI | `19007` | Next.js SSR/static — localhost only |

> **Important:** If any port is taken, increment in +1 steps and update this document. Never reuse another project's ports.

---

## 5. Docker Isolation

Dedicated compose file: `deploy/docker-compose.hadal.yml`

```yaml
# Conceptual — deploy from /www/wwwroot/hadal.cicibyte.com/deploy/
networks:
  hadal_net:
    name: hadal_isolated_net

services:
  hadal-redis:
    image: redis:7-alpine
    networks: [hadal_net]
    ports: ["127.0.0.1:19004:6379"]
    volumes: [hadal_redis_data:/data]

  hadal-postgres:
    image: postgres:16-alpine
    networks: [hadal_net]
    ports: ["127.0.0.1:19005:5432"]
    environment:
      POSTGRES_DB: hadal
      POSTGRES_USER: hadal_app
      POSTGRES_PASSWORD: ${HADAL_DB_PASSWORD}
    volumes: [hadal_pg_data:/var/lib/postgresql/data]

  hadal-gateway:
    # ASP.NET Core Gateway image
    networks: [hadal_net]
    ports: ["127.0.0.1:19001:8080"]
    depends_on: [hadal-redis, hadal-game-api]

  hadal-game-api:
    networks: [hadal_net]
    ports: ["127.0.0.1:19002:8080"]
    depends_on: [hadal-postgres, hadal-redis]

  hadal-admin-api:
    # ASP.NET Core Admin API — The Overseer backend
    networks: [hadal_net]
    ports: ["127.0.0.1:19006:8080"]
    depends_on: [hadal-postgres, hadal-redis, hadal-game-api]
    environment:
      ADMIN_JWT_SECRET: ${HADAL_ADMIN_JWT_SECRET}
      ADMIN_TOTP_ISSUER: HADAL-Overseer

  hadal-overseer-ui:
    # Next.js GM panel — no DB credentials
    networks: [hadal_net]
    ports: ["127.0.0.1:19007:3000"]
    depends_on: [hadal-admin-api]

volumes:
  hadal_redis_data:
  hadal_pg_data:
```

**Constraints:**

- Network name: `hadal_isolated_net` — never `bridge` shared with other stacks
- Bind all ports to `127.0.0.1` only — external access via nginx only
- Container names prefixed: `hadal-*`

---

## 6. Nginx VHost (Isolated Snippet)

Add **new server block** for `hadal.cicibyte.com` only. Do NOT edit other site configs.

```nginx
server {
    listen 443 ssl http2;
    server_name hadal.cicibyte.com;

    root /www/wwwroot/hadal.cicibyte.com;
    index index.html;

    # Patch manifest + CDN (static)
    location /manifest/ {
        alias /www/wwwroot/hadal.cicibyte.com/cdn/manifest/;
        add_header Cache-Control "no-cache";
    }

    location /cdn/ {
        alias /www/wwwroot/hadal.cicibyte.com/cdn/;
        add_header Cache-Control "public, max-age=31536000, immutable";
    }

    # WebSocket Gateway
    location /gateway {
        proxy_pass http://127.0.0.1:19001;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_read_timeout 86400;
    }

    # REST API
    location /api/ {
        proxy_pass http://127.0.0.1:19002;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location /health {
        return 200 'ok';
        add_header Content-Type text/plain;
    }

    # The Overseer Terminal — GM / LiveOps panel (HADAL vhost ONLY)
    # IP allowlist: restrict to CiciByte Technology, Inc. egress in production
    location /admin/api/ {
        # Optional: allow 203.0.113.0/24; deny all;
        proxy_pass http://127.0.0.1:19006/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        add_header Cache-Control "no-store";
    }

    location /admin {
        proxy_pass http://127.0.0.1:19007;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        add_header Cache-Control "no-store";
        # SPA fallback handled by Next.js container
    }
}
```

**Alternate vhost** (`overseer.hadal.cicibyte.com`): duplicate Admin locations only — no CDN/gateway exposure. Same `hadal_isolated_net` upstream ports.

After edit: `nginx -t` → reload only if test passes. **Never edit other domain vhosts.**

### Deployment topology (HADAL stack on `hadal_isolated_net`)

```
nginx (443, hadal.cicibyte.com)
  ├── /cdn, /manifest     → static files
  ├── /gateway            → hadal-gateway:19001
  ├── /api/               → hadal-game-api:19002
  ├── /admin              → hadal-overseer-ui:19007
  └── /admin/api/         → hadal-admin-api:19006
                                │
                    hadal_isolated_net (Docker)
  hadal-gateway ──┬── hadal-redis ── hadal-postgres
  hadal-game-api ─┤
  hadal-admin-api ─┘  (orchestrates — no public DB port)
  hadal-overseer-ui   (UI only — no DB/Redis credentials)
```

---

## 7. Patch / CDN Flow (Server Side)

Client bootstrap calls `GET https://hadal.cicibyte.com/manifest/version.json`:

```json
{
  "gameVersion": "1.0.0",
  "catalogVersion": "1.0.0",
  "catalogUrl": "https://hadal.cicibyte.com/cdn/catalog/catalog_1.0.0.json",
  "catalogHash": "sha256:abc123...",
  "minClientVersion": "1.0.0",
  "forceUpdate": false
}
```

**Publishing a patch:**

1. Build Addressables in Unity → upload bundles to `cdn/bundles/`
2. Upload catalog + `.hash` to `cdn/catalog/`
3. Update `manifest/version.json` (atomic rename: write temp → mv)
4. Client PatchManager compares hash → downloads delta bundles

---

## 8. Database Schema (PostgreSQL)

Schema name: `hadal` — never use tables in other project schemas.

Core tables (server truth):

- `players` — account link, display name, created_at
- `player_state` — JSONB snapshot or normalized sub-tables (resources, buildings)
- `commands_log` — idempotent command audit (anti-cheat)
- `alliances`, `alliance_members`
- `world_tiles`, `world_events`
- `sessions` — active session tokens (or Redis-only with PG backup)
- `admin_users`, `admin_roles` — Overseer operators (separate from player accounts)
- `admin_action_audit` — append-only GM audit log — [22_LiveOps_Admin_Terminal.md](./22_LiveOps_Admin_Terminal.md)
- `gacha_drop_rates`, `gacha_banners`, `gift_codes` — economy hot-swap tables
- `mail_campaigns`, `reward_packages` — LiveOps dispatch

**Client NEVER writes directly.** All mutations via validated commands.

---

## 9. Deployment Checklist (Safe Order)

### Phase A — Static only (zero risk)

- [ ] Create directory tree under `/www/wwwroot/hadal.cicibyte.com/`
- [ ] Place placeholder `static/index.html` + `cdn/manifest/version.json`
- [ ] Add nginx vhost for `hadal.cicibyte.com` (SSL via panel)
- [ ] Verify `https://hadal.cicibyte.com/health` returns 200
- [ ] Verify other sites still respond normally

### Phase B — CDN test

- [ ] Upload test catalog + one bundle
- [ ] Verify download from mobile network
- [ ] Confirm CORS/HTTPS headers if needed

### Phase C — Backend (isolated Docker)

- [ ] Verify ports 19001–19005 free
- [ ] Deploy `docker-compose.hadal.yml`
- [ ] Run DB migrations (hadal schema only)
- [ ] Gateway + API health on localhost
- [ ] nginx proxy to gateway/api
- [ ] WebSocket test from Unity client

### Phase D — Production hardening

- [ ] Rate limiting on gateway
- [ ] Fail2ban / WAF rules (vhost scoped)
- [ ] Log rotation under `logs/`
- [ ] Backup job for `hadal_pg_data` volume only

### Phase E — The Overseer Terminal (LiveOps GM)

- [ ] Verify ports 19006–19007 free
- [ ] Deploy `hadal-admin-api` + `hadal-overseer-ui` on `hadal_isolated_net`
- [ ] Run admin schema migrations (`admin_action_audit`, `admin_users`)
- [ ] nginx `/admin` + `/admin/api/` reverse proxy (hadal vhost only)
- [ ] TOTP 2FA enrollment for GM accounts
- [ ] IP allowlist on `/admin` paths
- [ ] Telemetry dashboard smoke test (CCU, Redis, PG pool)

See [22_LiveOps_Admin_Terminal.md](./22_LiveOps_Admin_Terminal.md).

---

## 10. SSH Access Notes

- **Server:** `31.40.199.47` — passwordless SSH for Cicibyte deployment operator
- **Scope:** Full authority within `/www/wwwroot/hadal.cicibyte.com/` and HADAL Docker stack (`hadal-*` containers, `hadal_isolated_net`) only
- **Pre-flight on connect:**
  ```bash
  docker ps --format "table {{.Names}}\t{{.Ports}}"
  ss -tlnp | head -50
  ls /www/wwwroot/
  ```
- **Never run:** `docker system prune`, `systemctl restart nginx` (global), edits to `/www/server/panel/vhost/nginx/*.conf` for other domains

---

## 11. Environment Variables Template

File: `deploy/.env.example` (copy to `.env`, never commit)

```env
HADAL_DB_PASSWORD=<generated>
HADAL_REDIS_PASSWORD=<optional>
HADAL_JWT_SECRET=<generated>
HADAL_ADMIN_JWT_SECRET=<generated-separate-from-player-jwt>
HADAL_ADMIN_TOTP_ENCRYPTION_KEY=<generated>
HADAL_GATEWAY_PORT=19001
HADAL_API_PORT=19002
HADAL_ADMIN_API_PORT=19006
HADAL_OVERSEER_UI_PORT=19007
ASPNETCORE_ENVIRONMENT=Production
HADAL_GOOGLE_CLIENT_IDS=<web OAuth client id>
```

### 11.1 Sign in with Google (account link and sign-in on a new phone)

The gateway validates Google ID tokens itself (Google's published signing keys, issuer
`accounts.google.com`, expiry, audience); no client secret is used anywhere.

- **Audience = the WEB OAuth client id.** The Android app asks Credential Manager for an ID token
  with `serverClientId` = the web client id, so Google issues the token *to* that id (`aud`); the
  Android OAuth client (package + SHA-1) only authorises the app to ask. Google Cloud project
  `hadal-513122`:
  - Web client: `1052662691515-2fl1638hjksll51nanstc0opn46b68rf.apps.googleusercontent.com`
  - Android client (`com.cicibyte.hadal`, debug-keystore SHA-1 `AF:F1:68:52:…:1A:EF`):
    `1052662691515-qvn4dlkhleg6afalrb45e2bes0rg0k3a.apps.googleusercontent.com`
- **Server:** in `/opt/hadal/server/.env` set
  `HADAL_GOOGLE_CLIENT_IDS=1052662691515-2fl1638hjksll51nanstc0opn46b68rf.apps.googleusercontent.com`
  (comma-separated if an iOS/other client is added later). `docker-compose.hadal.yml` passes it to the
  gateway as `Auth__GoogleClientIds`; recreate the gateway (`up -d hadal-gateway`). While it is empty
  the endpoints answer `501 ProviderNotConfigured` and the app shows "not configured yet".
- **Client:** `GameConfigSO._googleWebClientId` (Inspector: Game Config → Accounts) holds the same web
  client id; it defaults to the value above, so the existing GameConfig asset needs no edit.
- **Release builds:** an APK signed with another key (release keystore, Play App Signing) needs its
  SHA-1 added to an Android OAuth client in the same project, otherwise the account picker fails
  with "developer error" (shown in game as a sign-in failure).
- While the OAuth consent screen is in *Testing*, only listed test users can sign in; publish it
  (basic scopes `openid email profile` need no verification) before players use it.

---

## 12. Rollback Procedure

1. `docker compose -f docker-compose.hadal.yml down` (HADAL containers only)
2. Remove or disable nginx vhost snippet for hadal.cicibyte.com
3. `nginx -t && nginx -s reload`
4. Other projects unaffected — no shared volumes removed

---

## 13. Related Documentation

- [17_Technical_Architecture.md](./17_Technical_Architecture.md) — StateSyncPipeline, prediction
- [18_Unity_Client_Architecture.md](./18_Unity_Client_Architecture.md) — client view layer
- [21_HADAL_Shared_Protocol_And_Serialization.md](./21_HADAL_Shared_Protocol_And_Serialization.md) — Protobuf + HADAL.Shared
- [15_Monetization.md](./15_Monetization.md) — Gacha · server authority
- [16_Live_Ops.md](./16_Live_Ops.md) — LiveOps principles
- [22_LiveOps_Admin_Terminal.md](./22_LiveOps_Admin_Terminal.md) — The Overseer GM panel
- [HadalDevelopmentRoadmap.md](./HadalDevelopmentRoadmap.md) — Phase 0-R tasks

---

## 14. Operations Runbook (CT 160)

Everything here runs on CT 160 (`ssh cicibyte@dl380`, then `sudo pct exec 160 -- …`). The rule for every step:
**the live database `hadal` is only ever read, and the live gateway is not touched** — except in §14.4, which is the
real restore and is done by the operator only.

### 14.1 Server tests with a throwaway database

```bash
bash server/tests/run-ct-tests.sh                        # committed HEAD
bash server/tests/run-ct-tests.sh --worktree             # HEAD + uncommitted changes under shared/ and server/
bash server/tests/run-ct-tests.sh --worktree <paths>…    # HEAD + only these paths (other sessions' files left out)
TEST_FILTER="FullyQualifiedName~EconomySafety" bash server/tests/run-ct-tests.sh
```

- Sources go to `/opt/hadal-safetytest` (`HADAL_TEST_DIR`), never to `/opt/hadal`.
- `server/tests/ct-tests.sh` creates `hadal_test_<time>_<rand>` inside `hadal-postgres`, runs **all** tests with
  `HADAL_TEST_PG` pointing at it and `HADAL_TEST_PG_REQUIRED=1` (a missing connection string fails the run instead of
  silently skipping the store tests), and drops the database on exit.
- The store tests refuse a connection string whose database is `hadal` (`PostgresTest`). Before 2026-10-29 the deploy
  ran them against the live database: they reset the live-ops settings, briefly published a test catalog and left
  rows behind (see §14.7). `remote-deploy.sh` now uses its own `hadal_test_deploy_<time>` database the same way.
- Leftover check (should print nothing):
  `docker exec hadal-postgres psql -U hadal_app -d postgres -Atc "SELECT datname FROM pg_database WHERE datname ~ '^hadal_(test|restore)_'"`

### 14.2 Taking a backup

Plain `pg_dump` of the `hadal` database, without `--create` (so a restore can never switch to or recreate the live one):

```bash
docker exec hadal-postgres pg_dump -U hadal_app -d hadal > /opt/hadal/backups/before-<what>-$(date +%Y%m%d-%H%M).sql
```

Take one before every deploy that changes the schema or the catalog. There is no scheduled backup yet (§14.7).

### 14.3 Restore drill (throwaway database, nothing live is changed)

Automated, from the dev machine:

```bash
bash server/tests/run-restore-drill.sh                                      # newest dump, code at HEAD
bash server/tests/run-restore-drill.sh HEAD /opt/hadal/backups/<file>.sql   # a chosen dump
KEEP_DB=1 bash server/tests/run-restore-drill.sh                            # keep the restored database to look around
```

What it does (`server/tests/ct-restore-drill.sh`), and the same steps by hand:

1. Pick the dump: `ls -t /opt/hadal/backups/*.sql | head -1`. Refuse it if it contains `\connect`, `CREATE DATABASE`
   or `DROP DATABASE` (`grep -nE '^(\\connect|CREATE DATABASE|DROP DATABASE)' <dump>`).
2. `docker exec hadal-postgres psql -U hadal_app -d postgres -c "CREATE DATABASE hadal_restore_<time>;"`
3. `docker exec -i hadal-postgres psql -v ON_ERROR_STOP=1 -q -U hadal_app -d hadal_restore_<time> < <dump>`
4. Row counts of every `hadal.*` table in the restored database next to the live ones (the live side inside
   `BEGIN READ ONLY … ROLLBACK`). Every table must come back; the live side may be a little ahead of the dump.
5. `RestoreDrillTests` with `HADAL_RESTORE_DRILL_PG=…;Database=hadal_restore_<time>`: the real gateway boots on the
   restored data (schema, alliances, world, events, mail, catalog bootstrap), every player loads through
   `PostgresPlayerRepository` with no negative balance, up to 25 players log in and out, and the balance rollback
   of §14.5 is exercised, including a restart.
6. `docker exec hadal-postgres psql -U hadal_app -d postgres -c "DROP DATABASE hadal_restore_<time> WITH (FORCE);"`

Last drill: **2026-10-29**, `before-protected-20261029-1916.sql` (237 KB) restored in 1 s; all 24 tables came back with
the live row counts (admin_audit 133 vs 134 live, one newer entry); 5 players, 61 buildings, 2 alliances, 2819 world
nodes loaded; the gateway booted and 5 players logged in; the rollback drill passed. Repeat it monthly and after any
schema change.

### 14.4 Real restore (operator only)

Only when the live database is lost or corrupted. The gateway must be stopped so nothing writes during the swap.

1. Run the drill (§14.3) on the dump you intend to use, with `KEEP_DB=1`; note the database name `hadal_restore_<time>`.
2. Stop the gateway only: `docker compose -f /opt/hadal/server/docker-compose.hadal.yml --env-file /opt/hadal/server/.env stop hadal-gateway`
3. Keep what is there: `docker exec hadal-postgres pg_dump -U hadal_app -d hadal > /opt/hadal/backups/broken-$(date +%Y%m%d-%H%M).sql`
4. Swap the databases (connected to `postgres`, not to either of them):
   `ALTER DATABASE hadal RENAME TO hadal_broken_<date>;` then `ALTER DATABASE hadal_restore_<time> RENAME TO hadal;`
5. Start the gateway: `… up -d hadal-gateway`, then check `curl -fs http://127.0.0.1:19001/health` and the Overseer stats.
6. Drop `hadal_broken_<date>` only after a few days of normal play.

### 14.5 Balance (catalog) rollback

- **Now:** Overseer panel → Balance → versions → *Restore* on the wanted version (`POST /admin/api/catalog/{v}/restore`,
  Owner role). It is stored as a new version and swapped in at once: every session first settles its economy under
  the old rules, then runs on the restored ones. `GET /admin/api/catalog` shows the running version.
- **Across restarts and deploys:** on start-up `CatalogBootstrap` publishes the image's catalog file when its design
  `version` is newer than the stored one, which would undo the rollback. Pin it: set
  `HADAL_PUBLISH_CATALOG_UP_TO=<design version you rolled back to>` in `/opt/hadal/server/.env` before the next
  restart or deploy (or ship an image with the older file). The drill showed exactly this: a rollback to design 9 was
  superseded on restart by the release file's design 10.
- The drill also checks that the older version still loads under today's code; a version that no longer validates
  cannot be rolled back to.

### 14.6 Disbanded alliances

Documents of an alliance (`hadal.alliance_docs` keys `a:<allianceId>:log|battle|gifts|research|kills`) are deleted
when the last member leaves, and on every start-up any left without an alliance are deleted. A player's own purse
(`p:<playerId>:coins`) stays. Nothing to do by hand.

### 14.7 Open items

- Traces of the old deploy-time tests in the live database (found read-only on 2026-10-29): 45 `admin_audit` rows
  with action `test` by `pg-…` admins (2026-10-25 → 2026-10-29 19:17), and the `catalog_versions` sequence stands at
  58 while the newest version is 39 (each run published and deleted a test catalog). Each of those deploys also
  briefly set the maintenance flag and then cleared the live-ops settings (maintenance off, announcement empty), so
  re-enter the announcement if one was set. The audit rows are harmless; remove them only if wanted, after a backup:
  `DELETE FROM hadal.admin_audit WHERE action = 'test' AND admin LIKE 'pg-%';`
- No scheduled backup: a daily `pg_dump` cron on CT 160 with a 14-day rotation, plus a copy off the host.

---

**Document Version:** 3.1  
**Last Updated:** 2026-06-19  
**Owner:** HADAL Platform Team
