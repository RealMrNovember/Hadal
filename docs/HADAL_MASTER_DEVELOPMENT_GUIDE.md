# HADAL Master Development Guide

**Version:** 3.0 — Production MMO Hardened  
**Engine:** Unity 6 LTS · URP · C# · ASP.NET Core  
**Genre:** AAA Mobile Strategy MMO

---

## Documentation Index

| Document | Purpose |
|----------|---------|
| [17_Technical_Architecture.md](./17_Technical_Architecture.md) | Distributed MMO, StateSyncPipeline |
| [18_Unity_Client_Architecture.md](./18_Unity_Client_Architecture.md) | VContainer, prediction, client view |
| [19_HADAL_Shared_Protocol_And_Serialization.md](./19_HADAL_Shared_Protocol_And_Serialization.md) | **Protobuf, HADAL.Shared, wire protocol** |
| [HADAL_SERVER_INFRASTRUCTURE.md](./HADAL_SERVER_INFRASTRUCTURE.md) | `hadal.cicibyte.com` deployment |
| [HadalDevelopmentRoadmap.md](./HadalDevelopmentRoadmap.md) | Phased delivery |
| [ProjeDosyası.md](./ProjeDosyası.md) | Vision & gameplay design |

---

## 1. Production MMO Principles

| # | Principle |
|---|-----------|
| 1 | Server is single source of truth |
| 2 | Client is view + input + optimistic visual feedback |
| 3 | **HADAL.Shared** defines all commands, DTOs, enums, formulas |
| 4 | **Protobuf** for all gameplay wire messages — JSON forbidden on wire |
| 5 | **VContainer** only — no static resolve, no singleton managers |
| 6 | **StateSyncPipeline** — Snapshot + Delta → VisualStateCache |
| 7 | **ClientPredictionSystem** with smooth rollback |
| 8 | **Network → Translator → Local → UI** — strict event ownership |
| 9 | Patch before MainMenu |
| 10 | Cheat resistance by design |

---

## 2. Architecture Stack (Canonical)

```
HADAL.Shared (proto + C#)
    ├── Unity Client (VContainer, VisualStateCache, Prediction)
    └── Backend (Validation, Simulation, PostgreSQL)

Wire: Protobuf over WebSocket
Patch: JSON manifest only (CDN)
DI: VContainer (Project / Session / Game / UI scopes)
Events: NetworkEventBus + LocalEventBus + Translator + EventValidationLayer
```

---

## 3. Forbidden Patterns (Complete List)

| # | Forbidden |
|---|-----------|
| 1 | Client gameplay JSON save as truth |
| 2 | JSON StateDelta / Command on WebSocket |
| 3 | Duplicate command or state types outside HADAL.Shared |
| 4 | `ServiceLocator`, `GameContext`, static `Resolve<T>()` |
| 5 | Singleton managers with authoritative state |
| 6 | NetworkEventBus → UI direct subscription |
| 7 | Hard snap rollback on prediction mismatch |
| 8 | Offline reward calculation on client |
| 9 | `FindObjectOfType` in gameplay |
| 10 | Implicit global state |

---

## 4. Required Systems (Phase 0-R)

| System | Doc reference |
|--------|---------------|
| `HADAL.Shared` class library | Doc 19 |
| `NetworkSerializationLayer` (Protobuf) | Doc 19 |
| `StateSyncPipeline` | Doc 17 §3 |
| `ClientPredictionSystem` + `PredictionBuffer` | Doc 18 §6 |
| `CommandReconciliationSystem` + `RollbackAnimator` | Doc 18 §6 |
| `VisualStateCache` | Doc 18 §5 |
| `EventOwnershipPolicy` + `EventValidationLayer` | Doc 17 §7, Doc 18 §7 |
| VContainer lifetime scopes | Doc 18 §1 |
| PatchService | Doc 17 §9 |

---

## 5. Bootstrap Sequence

```
Boot → VContainer → Patch (JSON manifest) → Addressables
  → Login → Schema handshake (Protobuf Hello)
  → StateSnapshot (Protobuf) → MainMenu → Game states
```

---

## 6. Development Workflow

1. Define command + Protobuf schema in `HADAL.Shared`
2. Implement server validation + simulation (Shared formulas)
3. Implement client dispatch + prediction visual
4. Implement StateDelta replication + reconciliation
5. Wire Translator → LocalEventBus → UI
6. Load test under 100–200ms simulated latency

---

## 7. Technology Choices (Frozen)

| Area | Choice |
|------|--------|
| Wire serialization | **Protocol Buffers** |
| Shared library | **HADAL.Shared** (.NET Standard 2.1) |
| Client DI | **VContainer** |
| Server | ASP.NET Core |
| DB | PostgreSQL (`hadal` schema) |
| Cache | Redis (isolated) |
| CDN | `hadal.cicibyte.com` |

---

## 8. Gameplay Systems

All gameplay: **Command (Protobuf) → Server validate → StateDelta → VisualStateCache → UI**

Design intent: [ProjeDosyası.md](./ProjeDosyası.md)

---

## 9. Performance

- 60 FPS mobile
- Protobuf binary (bandwidth + GC vs JSON)
- Batched delta apply
- Object pooling
- Prediction buffer cap (32)
- Smooth rollback ≤300ms

---

## 10. Production Compliance Statement

Documentation v3.0 defines a **production-grade server-authoritative MMO architecture** suitable for Phase 0 implementation:

- Wire protocol specified (Protobuf)
- Shared contracts specified (HADAL.Shared)
- Latency UX specified (prediction + rollback)
- Event boundaries enforced (policy + validation)
- State replication specified (pipeline + entity reconciliation)
- Global access eliminated (VContainer only)

---

**Document Version:** 3.0  
**Supersedes:** v2.0 (conceptual JSON StateSync, incomplete prediction)
