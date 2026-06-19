# HADAL Master Development Guide

**Version:** 3.1 — Production MMO Hardened + Design Index  
**Engine:** Unity 6 LTS · URP · C# · ASP.NET Core  
**Genre:** AAA Mobile Strategy MMO

---

## Documentation Index

### Game Design (01–16, 19–20)

| # | Document | Purpose |
|---|----------|---------|
| 01 | [01_Executive_Vision.md](./01_Executive_Vision.md) | Tagline · vision · core loop |
| 02 | [02_World_Lore.md](./02_World_Lore.md) | Setting · ancient civilization |
| 03 | [03_Main_Story.md](./03_Main_Story.md) | Opening · Core narrative surprise |
| 04 | [04_Gameplay_Overview.md](./04_Gameplay_Overview.md) | Systems map |
| 05 | [05_Base_Building.md](./05_Base_Building.md) | Circular dome base |
| 06 | [06_Resources.md](./06_Resources.md) | Oxygen · Hadalite · resources |
| 07 | [07_Pressure_System.md](./07_Pressure_System.md) | Depth pressure mechanic |
| 08 | [08_Expeditions.md](./08_Expeditions.md) | Hero + submarine expeditions |
| 09 | [09_Enemies.md](./09_Enemies.md) | Creatures · bosses |
| 10 | [10_Heroes.md](./10_Heroes.md) | Classes · factions · Aelis |
| 11 | [11_World_Map.md](./11_World_Map.md) | Circular abyss map |
| 12 | [12_PvP_Alliance.md](./12_PvP_Alliance.md) | Underwater cities · sieges |
| 13 | [13_UI_UX.md](./13_UI_UX.md) | Sonar UX · immersion |
| 14 | [14_Art_and_Sound.md](./14_Art_and_Sound.md) | Visual · audio direction |
| 15 | [15_Monetization.md](./15_Monetization.md) | **Gacha · Quiet Luxury · server rolls** |
| 16 | [16_Live_Ops.md](./16_Live_Ops.md) | Seasonal ops principles |
| 19 | [19_Economy.md](./19_Economy.md) | Resource · Hadalite economy |
| 20 | [20_End_Game.md](./20_End_Game.md) | The Core · organism reveal |

### Technical Architecture (17–18, 21)

| # | Document | Purpose |
|---|----------|---------|
| 17 | [17_Technical_Architecture.md](./17_Technical_Architecture.md) | Distributed MMO · StateSyncPipeline |
| 18 | [18_Unity_Client_Architecture.md](./18_Unity_Client_Architecture.md) | VContainer · prediction · client view |
| 21 | [21_HADAL_Shared_Protocol_And_Serialization.md](./21_HADAL_Shared_Protocol_And_Serialization.md) | Protobuf · HADAL.Shared |

### Operations

| Document | Purpose |
|----------|---------|
| [HADAL_SERVER_INFRASTRUCTURE.md](./HADAL_SERVER_INFRASTRUCTURE.md) | `hadal.cicibyte.com` deployment |
| [HadalDevelopmentRoadmap.md](./HadalDevelopmentRoadmap.md) | Phased delivery |

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
| 11 | **Gacha RNG server-only** — see [15_Monetization.md](./15_Monetization.md) |

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
Monetization: ExecuteGachaCommand → GachaResultDelta (Protobuf)
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
| 11 | Client-side gacha RNG or reward selection |
| 12 | Aggressive monetization pop-ups (Quiet Luxury violation) |

---

## 4. Required Systems (Phase 0-R)

| System | Doc reference |
|--------|---------------|
| `HADAL.Shared` class library | Doc 21 |
| `NetworkSerializationLayer` (Protobuf) | Doc 21 |
| `StateSyncPipeline` | Doc 17 §3 |
| `ClientPredictionSystem` + `PredictionBuffer` | Doc 18 §6 |
| `CommandReconciliationSystem` + `RollbackAnimator` | Doc 18 §6 |
| `VisualStateCache` | Doc 18 §5 |
| `EventOwnershipPolicy` + `EventValidationLayer` | Doc 17 §7, Doc 18 §7 |
| VContainer lifetime scopes | Doc 18 §1 |
| PatchService | Doc 17 §9 |
| Gacha command + result pipeline | Doc 15 · Doc 21 |

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
| Premium store (UX) | **The Syndicate** |

---

## 8. Gameplay Systems

All gameplay: **Command (Protobuf) → Server validate → StateDelta → VisualStateCache → UI**

Design index: [01_Executive_Vision.md](./01_Executive_Vision.md) through [20_End_Game.md](./20_End_Game.md)

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

Documentation v3.1 defines a **production-grade server-authoritative MMO architecture** suitable for Phase 0 implementation:

- Wire protocol specified (Protobuf)
- Shared contracts specified (HADAL.Shared)
- Latency UX specified (prediction + rollback)
- Event boundaries enforced (policy + validation)
- State replication specified (pipeline + entity reconciliation)
- Global access eliminated (VContainer only)
- Monetization security specified (server-only gacha)

---

**Document Version:** 3.1  
**Supersedes:** v3.0 · monolithic `ProjeDosyası.md` (removed)
