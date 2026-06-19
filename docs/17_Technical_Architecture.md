# 17 — Technical Architecture (Server-Authoritative MMO)

**Document Version:** 3.0 — Production Hardened  
HADAL is a **server-authoritative mobile MMO**. PostgreSQL + Game Server own all gameplay truth. Unity client is **rendering, input, prediction display** only.

**Companion docs:** [19_HADAL_Shared_Protocol_And_Serialization.md](./19_HADAL_Shared_Protocol_And_Serialization.md) · [18_Unity_Client_Architecture.md](./18_Unity_Client_Architecture.md)

---

## 1. Architectural Tenets

| Principle | Rule |
|-----------|------|
| **Single source of truth** | PostgreSQL + Game Server simulation |
| **Shared contracts** | `HADAL.Shared` — commands, DTOs, enums, formulas |
| **Binary wire protocol** | Protobuf only for gameplay replication |
| **Commands, not mutations** | Client sends intent; server validates and applies |
| **Replication, not save** | StateSnapshot + compressed StateDelta |
| **Prediction is visual only** | Client never commits predicted state as truth |
| **Cheat resistance** | Server validates using Shared rules; audit log |
| **Strict event boundaries** | Network → Translator → Local → UI |

---

## 2. Production System Diagram

```
┌────────────────────────── HADAL.Shared (.NET Standard) ──────────────────────────┐
│  proto/ · Commands · State DTOs · Enums · Simulation formulas · Schema version   │
└───────────────────────────────┬───────────────────────────────┬──────────────────┘
                                │                               │
┌───────────────────────────────▼──────────────┐   ┌────────────▼────────────────────┐
│           UNITY CLIENT (View Layer)         │   │     BACKEND (Authority)         │
│  VContainer DI — NO static globals            │   │  Gateway · Game Server          │
│                                               │   │                                 │
│  Input → ClientPredictionSystem               │   │  CommandValidator (Shared)      │
│       → CommandDispatcher                     │   │  SimulationTick (Shared rules)  │
│       → NetworkSerializationLayer (Protobuf)  │   │  StateSnapshot/Delta builder  │
│                                               │   │  Delta compression            │
│  ← NetworkStateReceiver (Protobuf decode)     │   │  → Redis pub/sub → Gateway    │
│  ← CommandReconciliationSystem                │   │  → PostgreSQL persist         │
│  ← StateSyncPipeline → VisualStateCache       │   │                                 │
│  ← NetworkEventBus → Translator → LocalBus    │   │                                 │
│  PatchService → Addressables                  │   │                                 │
└───────────────────────────────┬──────────────┘   └────────────┬────────────────────┘
                                │         Protobuf / wss         │
                                └──────────────┬───────────────────┘
                                               ▼
                              hadal.cicibyte.com (Gateway + CDN)
```

---

## 3. StateSyncPipeline (Server → Client)

Full replication path — **no JSON on wire**.

```
ServerState (authoritative, in-memory + PostgreSQL)
    │
    ├─► StateSnapshot (full) ── on login / reconnect / desync recovery
    │
    └─► StateDelta (incremental, entity ID diff, compressed)
            │
            ▼
        Gateway (Protobuf frame)
            │
            ▼
        NetworkStateReceiver (client)
            │
            ▼
        EntityReconciliationSystem (ID-based merge vs VisualStateCache)
            │
            ▼
        StateSyncService.ApplySnapshot / ApplyDelta
            │
            ▼
        VisualStateCache (NON-AUTHORITATIVE display copy)
            │
            ▼
        NetworkToLocalEventTranslator → LocalEventBus → UI / Animations
```

### Message types

| Type | When | Content |
|------|------|---------|
| `StateSnapshot` | Login, hard resync | Full player state tree |
| `StateDelta` | Every server tick batch | `EntityChange[]` with CREATE/UPDATE/DELETE |
| `CommandResult` | Per command ack | Success/fail + optional corrective delta |

### Rules

- Client **NEVER** stores authoritative state
- Client **ONLY** stores `VisualStateCache` for rendering
- PostgreSQL is persistence truth; Redis is ephemeral fanout/cache
- Delta compression uses `baseline_tick` acknowledgment from client

---

## 4. Command Flow (With Prediction)

```
1. Player action
2. Client: ClientPredictionSystem applies OPTIMISTIC visual (VisualStateCache tentative layer)
3. Client: CommandDispatcher sends PlaceBuildingCommand (Protobuf CommandEnvelope)
4. Gateway: auth + rate limit + route
5. Game Server: validate using HADAL.Shared rules
6. Success: mutate ServerState → persist → emit StateDelta
7. Failure: CommandResult with error + CommandReconciliationSystem rollback on client
8. Client: smooth RollbackAnimator — NO hard snap
```

Commands MUST be: **idempotent** (`command_id`), **sequenced** (`client_sequence`), **Protobuf-encoded**.

---

## 5. NetworkSerializationLayer (Server)

| Component | Location |
|-----------|----------|
| `INetworkSerializer` | HADAL.Shared or server Infrastructure |
| Protobuf code gen | `proto/` → C# (server + client) |
| `CompatibilityLayer` | Gateway — schema handshake |
| Max frame size | 256 KB default (configurable) |

REST auth may use JSON responses; **`/api/v1/session/bootstrap`** returns Protobuf snapshot bytes or `Content-Type: application/x-protobuf`.

---

## 6. HADAL.Shared on Server

Server MUST reference `HADAL.Shared` for:

- Command deserialization + validation
- State snapshot/delta construction
- `PressureFormula`, `ResourceProductionFormula`, `DamageFormula`
- Enum parity with client

**Forbidden:** duplicate command/state classes in server projects.

---

## 7. Event System (Server Side)

| Layer | Role |
|-------|------|
| **Server domain events** | Internal to simulation (not sent raw to UI) |
| **Network replication events** | Serialized in `StateDelta` or dedicated Protobuf `EventEnvelope` |
| **Client NetworkEventBus** | Receives decoded server events only |

### EventOwnershipPolicy (enforced)

| Bus | Owner | May mutate UI? |
|-----|-------|----------------|
| NetworkEventBus | Infrastructure (client) | **NO** |
| LocalEventBus | Presentation (client) | YES |
| Translator | Infrastructure (client) | Maps network → local only |

### EventValidationLayer

- Runtime check: NetworkEventBus subscribers MUST NOT be in `Hadal.Presentation` assembly
- CI analyzer rule: no `Presentation → NetworkEventBus` references
- Cross-bus publish blocked except via `INetworkToLocalEventTranslator`

---

## 8. Backend Stack

| Layer | Technology |
|-------|------------|
| Shared | `HADAL.Shared` + Protobuf |
| Gateway | ASP.NET Core + WebSocket (binary frames) |
| Game Server | ASP.NET Core worker |
| Cache | Redis (HADAL-dedicated) |
| Database | PostgreSQL 16, schema `hadal` |
| CDN | nginx @ `hadal.cicibyte.com` |
| Deploy | Docker Compose isolated — [HADAL_SERVER_INFRASTRUCTURE.md](./HADAL_SERVER_INFRASTRUCTURE.md) |

---

## 9. Bootstrap Order (Client + Server)

| Step | Client | Server |
|------|--------|--------|
| 1 | Boot | — |
| 2 | VContainer scopes | — |
| 3 | PatchService | CDN manifest (JSON OK) |
| 4 | Addressables | — |
| 5 | Login | Auth API |
| 6 | Schema handshake | Gateway HelloAck |
| 7 | StateSnapshot (Protobuf) | Full sync |
| 8 | MainMenu | — |

---

## 10. Security

- TLS (wss/https)
- Protobuf + size limits (anti-DoS)
- JWT rotation
- Command audit log
- Server-side validation only (Shared formulas)
- No gameplay logic trust in client assemblies

---

## 11. Forbidden Patterns (v3)

| Forbidden | Replacement |
|-----------|-------------|
| JSON gameplay replication | Protobuf via NetworkSerializationLayer |
| Duplicate command/DTO definitions | HADAL.Shared |
| Client authoritative persistence | StateSyncPipeline + VisualStateCache |
| `GameContext` / ServiceLocator / static managers | VContainer injection |
| NetworkEvent → UI direct binding | Translator → LocalEventBus |
| Hard rollback snap | CommandReconciliationSystem + RollbackAnimator |
| Implicit global state | Scoped DI only |

---

## 12. Risk Assessment (Post-Hardening)

| System | Risk | Notes |
|--------|------|-------|
| Protobuf wire protocol | **LOW** | Industry standard; schema versioning documented |
| HADAL.Shared | **LOW** | Single source of truth |
| StateSyncPipeline | **MEDIUM** | Complexity — mitigated by entity ID reconciliation |
| Delta compression | **MEDIUM** | Requires baseline ack protocol |
| Client prediction | **MEDIUM** | UX benefit; reconciliation must be tested under latency |
| Event boundaries | **LOW** | Policy + validation layer |
| Gateway scale | **MEDIUM** | Standard ops — shard when needed |
| Patch CDN | **LOW** | Static files |

---

**Document Version:** 3.0
