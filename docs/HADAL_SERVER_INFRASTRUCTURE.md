# HADAL Server Infrastructure

**Domain:** `hadal.cicibyte.com`  
**Server IP:** `31.40.199.47`  
**Web Root:** `/www/wwwroot/hadal.cicibyte.com`  
**Status:** Planned — isolated deployment (does NOT touch other projects)  
**Operator Access:** Full Cicibyte Corp authority within `/www/wwwroot/hadal.cicibyte.com/` and HADAL Docker stack only  
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
| `https://hadal.cicibyte.com/admin` | **The Overseer Terminal** (GM UI — IP allowlist) |
| `https://hadal.cicibyte.com/admin/api/v1/*` | Admin API (JWT + 2FA) |
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
    # IP allowlist: restrict to Cicibyte Corp egress in production
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
```

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

**Document Version:** 3.1  
**Last Updated:** 2026-06-19  
**Owner:** HADAL Platform Team
