# HADAL

> *Beneath The Abyss. Humanity Begins Again.*

**HADAL** is a server-authoritative mobile strategy MMO set 11,000 meters beneath the ocean. Unity 6 client + ASP.NET Core backend · **protobuf-net** wire · VContainer DI.

| | |
|---|---|
| **Engine** | Unity 6 LTS · URP · Android / iOS |
| **Backend** | ASP.NET Core · PostgreSQL · Redis |
| **Architecture** | v3.1 — Production MMO hardened |
| **Server** | [hadal.cicibyte.com](https://hadal.cicibyte.com) · GM: `/admin` (The Overseer) |
| **Status** | Phase 0-R complete · Phase 1 ready |

---

## Architecture (v3)

```
Input → ClientPredictionSystem → CommandDispatcher (Protobuf)
     → Gateway → Game Server (HADAL.Shared validation)
     → StateDelta → VisualStateCache → UI
```

| Layer | Technology |
|-------|------------|
| Wire protocol | **protobuf-net** (contract attributes) |
| Shared contracts | **HADAL.Shared** (.NET Standard 2.1) |
| Client DI | **VContainer** |
| State replication | **StateSyncService** · ApplyDelta → ClientStateView |
| Monetization | **Server-authoritative Gacha** — [15_Monetization.md](docs/15_Monetization.md) |

Full rules: [HADAL_MASTER_DEVELOPMENT_GUIDE.md](docs/HADAL_MASTER_DEVELOPMENT_GUIDE.md)

---

## Phase 0-R Progress ✓

- [x] MMO documentation audit & design split (01–22)
- [x] VContainer DI · event bus stubs
- [x] HADAL.Shared · protobuf-net · CommandDispatcher
- [x] NativeWebSocket transport · gateway handshake · command buffer
- [x] NetworkStateReceiver · CommandReconciliationSystem · RollbackAnimator
- [x] StateSyncService.ApplyDelta / ApplySnapshot → ClientStateView (RAM only)

**Next:** [Phase 1 — Circular Grid Building](docs/HadalDevelopmentRoadmap.md#phase-1--circular-grid-building)

---

## Documentation Index

### Game Design (01–20)

| # | Document |
|---|----------|
| 01 | [Executive Vision](docs/01_Executive_Vision.md) |
| 02 | [World Lore](docs/02_World_Lore.md) |
| 03 | [Main Story](docs/03_Main_Story.md) |
| 04 | [Gameplay Overview](docs/04_Gameplay_Overview.md) |
| 05 | [Base Building](docs/05_Base_Building.md) |
| 06 | [Resources](docs/06_Resources.md) |
| 07 | [Pressure System](docs/07_Pressure_System.md) |
| 08 | [Expeditions](docs/08_Expeditions.md) |
| 09 | [Enemies](docs/09_Enemies.md) |
| 10 | [Heroes](docs/10_Heroes.md) |
| 11 | [World Map](docs/11_World_Map.md) |
| 12 | [PvP & Alliance](docs/12_PvP_Alliance.md) |
| 13 | [UI / UX](docs/13_UI_UX.md) |
| 14 | [Art & Sound](docs/14_Art_and_Sound.md) |
| 15 | [Monetization (Gacha)](docs/15_Monetization.md) |
| 16 | [Live Ops](docs/16_Live_Ops.md) |
| 19 | [Economy](docs/19_Economy.md) |
| 20 | [End Game](docs/20_End_Game.md) |

### Technical (17–18, 21–22)

| # | Document |
|---|----------|
| 17 | [Technical Architecture](docs/17_Technical_Architecture.md) |
| 18 | [Unity Client Architecture](docs/18_Unity_Client_Architecture.md) |
| 21 | [HADAL.Shared & Protobuf](docs/21_HADAL_Shared_Protocol_And_Serialization.md) |
| 22 | [LiveOps Admin Terminal (The Overseer)](docs/22_LiveOps_Admin_Terminal.md) |

### Operations

| Document | Purpose |
|----------|---------|
| [HADAL_MASTER_DEVELOPMENT_GUIDE.md](docs/HADAL_MASTER_DEVELOPMENT_GUIDE.md) | Canonical dev rules |
| [HadalDevelopmentRoadmap.md](docs/HadalDevelopmentRoadmap.md) | Phase plan · M7–M9 Overseer |
| [HADAL_SERVER_INFRASTRUCTURE.md](docs/HADAL_SERVER_INFRASTRUCTURE.md) | Server deployment · `/admin` proxy |

---

## Project Structure

```
Hadal/
├── docs/                              # 01–22 design + technical docs
├── shared/HADAL.Shared/               # Commands, DTOs, ProtoContract types
├── Packages/com.hadal.protobuf-net/ # protobuf-net 3.2.52
├── Assets/_Hadal/                     # Unity client
└── README.md
```

---

## Getting Started

1. Clone the repository
2. Open in Unity Hub (Unity 6 LTS)
3. Unity resolves `com.hadal.protobuf-net` and `com.hadal.shared` automatically
4. Optional backend build: `shared/build_and_copy.bat` (requires .NET SDK)
5. Editor setup: `Hadal → Setup → Create Bootstrap Scene`

---

## License

Proprietary — © Cicibyte. All rights reserved.
