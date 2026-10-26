# HADAL Development Roadmap

> **Master plan moved to [ROADMAP.md](../ROADMAP.md)** (2026-10-24). This file keeps the detailed v3 sprint history and server milestone mapping (M1–M9).

**Version:** 3.1 — Production MMO Hardened  
**References:** [17](./17_Technical_Architecture.md) · [18](./18_Unity_Client_Architecture.md) · [21](./21_HADAL_Shared_Protocol_And_Serialization.md) · [15](./15_Monetization.md) · [22](./22_LiveOps_Admin_Terminal.md)

---

## Phase 0-R — Architecture Hardening (BLOCKING)

**Goal:** Production-safe MMO foundation. **No Phase 1 until exit criteria pass.**

| # | Task | Deliverable |
|---|------|-------------|
| 1 | Create `HADAL.Shared` + `proto/` | Commands, DTOs, enums, formulas — single assembly |
| 2 | Protobuf code generation | Client + server share generated types |
| 3 | `NetworkSerializationLayer` | Binary encode/decode + schema handshake |
| 4 | Remove client gameplay save | Delete SaveService authority; SessionCache only |
| 5 | VContainer migration | Remove GameContext, ServiceLocator, static managers |
| 6 | `StateSyncPipeline` | Snapshot + Delta + EntityReconciliation |
| 7 | `VisualStateCache` | Non-authoritative display layer |
| 8 | `ClientPredictionSystem` | PredictionBuffer + optimistic visual |
| 9 | `CommandReconciliationSystem` | Server ack + mismatch handling |
| 10 | `RollbackAnimator` | Smooth correction — no hard snap |
| 11 | Event hardening | EventOwnershipPolicy + EventValidationLayer |
| 12 | Split buses | Network → Translator → Local → UI |
| 13 | PatchService | Pre-MainMenu CDN check |
| 14 | Server Phase A | Static CDN on hadal.cicibyte.com |
| 15 | Gateway stub | Protobuf WebSocket + Hello handshake |
| 16 | **Overseer Admin API stub** | JWT + 2FA scaffold · `AdminActionAuditLog` table |
| 17 | **Overseer UI shell** | Next.js dark SPA · login + telemetry placeholder |

### Exit criteria (ALL required)

- [x] Zero gameplay JSON on WebSocket
- [ ] Zero duplicate types outside HADAL.Shared *(was ticked in error — `ResourceType` was duplicated until Sprint 3 consolidated it; `NetworkSerializationLayer` still exists in both HADAL.Shared and `Hadal.Core.Network`)*
- [x] Zero static Resolve / ServiceLocator in gameplay assemblies
- [ ] Prediction + rollback tested at 100ms and 200ms simulated latency *(Phase 1 test harness)*
- [x] NetworkEventBus has zero Presentation assembly subscribers
- [x] No gameplay data in persistentDataPath

**Phase 0-R status:** **COMPLETE** (2026-06-19) — ready for **Phase 1 (Grid Building System)**.

---

## Phase 0 — Foundation (Post 0-R)

| Task | Output |
|------|--------|
| Folder structure (Infrastructure / Presentation / Shared ref) | Clean assemblies |
| ScriptableObjects | Static config only |
| Addressables + CDN | Remote catalog |
| Game state machine | Patch, Login, SchemaHandshake, Loading, MainMenu |
| Gateway + stub Game Server | Protobuf command echo + validation stub |

**Output:** End-to-end Protobuf command → delta → visual cache loop.

---

## Phase 1 — Circular Grid Building

**Goal:** Server-authoritative polar grid placement with responsive ghost UX under latency.

| # | Sprint | Server | Client | Status |
|---|--------|--------|--------|--------|
| 1 | Polar grid & ghost preview | `GridSlot` + `CircularGridDefinition` in Shared | `CircularGridManager` · `IGridService` snap · `PreviewGhost` · `GridBuildController` (preview only) | [x] |
| 2 | Server-authoritative placement | `PlaceBuildingCommand` stub validation (`LocalPlacementAckSimulator`) | `CommandDispatcher` dispatch · pending ghost · `CommandReconciliationSystem` ack · `BuildingStateView` · grid occupancy sync via `StateSyncService` | [x] |
| 3 | Resource producer & sim tick | Production tick (Shared formulas) · `StateDelta` for resources | Resource HUD from replicated deltas · offline reconnect snapshot | [x] |

| Server | Client |
|--------|--------|
| `PlaceBuildingCommand` validation (Shared rules) | Ghost + prediction visual |
| `RemoveBuildingCommand` | Destroy mode + rollback |
| StateDelta for grid entities | GridStateView from VisualStateCache |
| | RollbackAnimator on reject |

**Phase 1 status (2026-10-24):** **Sprints 1–3 verified.** The project now compiles, loads and boots in Unity 6000.0.32f1 (headless on DL380, `server/unity/unity.sh`); before this it had never compiled. Sprint 3 closed with 56/56 EditMode tests and a PlayMode smoke test that runs the generated Bootstrap scene end to end (seed snapshot → production tick → delta → StateSync → HUD). Results and the bugs this surfaced: [qa/Sprint3-Test-Results.md](./qa/Sprint3-Test-Results.md).

Still open from the QA plan (non-blocking): live-network reconnect, on-device low-battery run, `_pending` flush on reconnect, `LocalPlacementAckSimulator` gating, BaselineTick resync, the 100/200 ms latency harness, and real balance numbers (current values are placeholders).

**Known Sprint 3 deviations (accepted, documented):**
- The stub economy derives offline accrual from `DateTime.UtcNow`, which is Forbidden Pattern #8 territory. It is capped at 12 h, stands down when a real gateway socket connects, and **refuses to start outside Editor/development builds**. It must be deleted when the real server ships.
- `ProductionStateDto` (server-sent display rates) was kept against the CTO's and full-stack lead's advice to defer it — see [cto/ADR-001](./cto/ADR-001-Sprint3-Resource-Tick.md) and the Sprint 3 handoff note.

**Output:** Server-confirmed building with responsive UX under latency.

---

## Phase 2 — Resource System

| Server | Client |
|--------|--------|
| Production/consumption tick (Shared formulas) | HUD from replicated deltas |
| Offline reconciliation on reconnect | StateSnapshot — NOT client offline calc |

> **LiveOps dependency:** Economy grants and manual compensation require **M7 Admin API** before soft-launch support workflows.

---

## Phase 3 — Core Survival

Server tick for oxygen, pressure, emergencies. Client alarms via LocalEventBus.

---

## Phase 4 — Buildings

Full catalog via commands + deltas. Upgrades server-validated.

---

## Phase 5 — Underwater Environment

Client presentation only. Unchanged authority model.

---

## Phase 6 — Submarine System

Server-owned fleet stats. Client renders from VisualStateCache.

---

## Phase 7 — Expedition System

Server-instanced expeditions. Rewards via StateDelta.

---

## Phase 8 — Sonar System

Client rendering; encounter triggers from server NetworkEvents → Translator.

---

## Phase 9 — Heroes

Server combat stats. Client animation from LocalEventBus.

**Status (2026-10-26):** 🟡 Base part done: 8 heroes, Cryo-Pod (pity, daily free pull), training, star promotion with shards, a 3-slot command crew whose skills feed the economy ([25_Heroes_Alliance_Chat.md](./25_Heroes_Alliance_Chat.md)). Combat stats and painted portraits remain.

---

## Phase 10 — Combat

Server adjudicated. Protobuf combat deltas.

---

## Phase 11 — World Map

Server world state. Client map view.

**Product requirement (2026-10-26):** two places — the base interior (today's circular base) and the outer world. The world map must show **other players' bases** (name, alliance tag, level), reached from the HUD's "Territory" button. Reference implementation reviewed: EmpireWars hex map (chunked loading, minimap, fog of war) — see [26_HUD.md](./26_HUD.md).

---

## Phase 12 — Alliance

Server sharded alliance state.

**Status (2026-10-26):** 🟡 First part done on one node: found/join/leave/kick, browse and search, alliance help on constructions, world and alliance chat with Overseer moderation ([25_Heroes_Alliance_Chat.md](./25_Heroes_Alliance_Chat.md)). Shared alliance buildings, sieges and sharding remain.

---

## Phase 13 — PvP

Server validation + anti-cheat audit log.

---

## Phase 14 — Leviathans

Server raid instances.

---

## Phase 15 — THE CORE

Server world event coordination.

---

## Server Milestones

| # | Deliverable | Wire format | Overseer dependency |
|---|-------------|-------------|---------------------|
| M1 | CDN + manifest | JSON (patch only) | — |
| M2 | Auth API | JSON HTTP | Admin login federation |
| M3 | Gateway + schema handshake | Protobuf | CCU telemetry |
| M4 | PlaceBuilding + StateSnapshot | Protobuf | Player CRM snapshot |
| M5 | Resource tick + StateDelta | Protobuf | Economy grants |
| M6 | Alliance + PvP shards | Protobuf | Alliance segment mail |
| **M7** | **Admin API (The Overseer backend)** | JSON REST | **Blocks all GM ops** |
| **M8** | **Overseer UI (Next.js panel)** | HTTPS static | GM daily operations |
| **M9** | **Audit + 2FA + gacha hot-swap** | JSON REST | Production LiveOps |

**Critical path:** M2 → M4 → M7 → M8 → M9 before monetization soft launch or public beta LiveOps.

Parallel track: M7/M8 can begin after M2 (auth) even while M4–M6 gameplay milestones continue.

See [22_LiveOps_Admin_Terminal.md](./22_LiveOps_Admin_Terminal.md) · [HADAL_SERVER_INFRASTRUCTURE.md](./HADAL_SERVER_INFRASTRUCTURE.md) Phase E.

---

## Phase Compatibility (v3)

| Phase | Blocked without 0-R? |
|-------|---------------------|
| 0-R | — (do first) |
| 0–15 | **YES** — all require Shared + Protobuf + StateSyncPipeline |

---

## Risk Register (Post-Hardening)

| Area | Risk | Mitigation |
|------|------|------------|
| HADAL.Shared | LOW | CI duplicate-type check |
| Protobuf | LOW | Schema versioning |
| StateSyncPipeline | MEDIUM | Entity reconciliation tests |
| Prediction/Rollback | MEDIUM | Latency simulation suite |
| Event boundaries | LOW | EventValidationLayer |
| Delta compression | MEDIUM | Baseline tick protocol |
| **Overseer / Admin API** | **HIGH** | JWT + 2FA + audit · no direct DB · IP allowlist |
| **Gacha hot-swap** | **HIGH** | Staging publish · simulation · two-person approval |

---

**Document Version:** 3.1
