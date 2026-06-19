# HADAL

> *Beneath The Abyss. Humanity Begins Again.*

**HADAL** is a next-generation, server-authoritative Mobile Strategy MMO built on a custom high-performance architecture. Set in the crushing depths of the ocean (11,000 meters below), it combines unforgiving survival mechanics, real-time grid building, and massive multiplayer ecosystems.

Designed for uncompromising scalability and a seamless mobile experience.

---

## ⚙️ Core Architecture (v3.0)

This project strictly adheres to a **Server-Authoritative** model. The Unity client acts purely as a rendering, prediction, and input dispatch layer.

* **Engine:** Unity 6 LTS (URP) / Mobile Optimized (60 FPS Target)
* **Backend:** Distributed Game Server Cluster (ASP.NET Core / PostgreSQL / Redis)
* **Networking:** WebSocket + REST Gateway with **Protobuf** binary serialization.
* **Shared Codebase:** `HADAL.Shared` (.NET Standard 2.1) enforcing a single source of truth for all Commands, DTOs, and Simulation Formulas across Client and Server.
* **Dependency Injection:** Strict scoped resolution via **VContainer** (Project / Session / Game). No implicit globals, no service locators.
* **Latency Compensation:** Custom `ClientPredictionSystem` with asynchronous validation and smooth `RollbackAnimator` capabilities to ensure a fluid UX under 100-200ms latency.
* **Asset Management:** Unity Addressables via external CDN `PatchService`.

## 🏗️ Development Roadmap

Currently executing **Phase 0-R (Architecture Hardening)**:
- [x] MMO System Design & Documentation Audit
- [x] Event System Consolidation (Local vs. Network Bus)
- [ ] VContainer DI Implementation
- [ ] Protobuf `NetworkSerializationLayer` & `HADAL.Shared` Integration
- [ ] StateSync Pipeline (Snapshot + Delta Compression) Setup

*(For full milestone tracking, see `/docs/HadalDevelopmentRoadmap.md`)*

## 📂 Project Structure

```text
HADAL/
├── Assets/
│   └── _Hadal/
│       ├── Scripts/
│       │   ├── Core/          # DI, Patching, StateSync, Event Buses
│       │   ├── Shared/        # Protobuf generated schemas & shared models
│       │   ├── Presentation/  # Views, UI, Prediction Logic
│       │   └── Data/          # ScriptableObject configs (Visuals only)
│       └── Art/               # High-contrast, Bioluminescent 3D Assets
├── docs/                      # v3.0 Master Architecture Documentation
└── Packages/                  # Addressables, VContainer, Protobuf tools



**AAA mobile strategy MMO** — build an underwater civilization from the shallows to the abyss.

| | |
|---|---|
| **Engine** | Unity 6 LTS · URP · Android / iOS |
| **Backend** | ASP.NET Core · PostgreSQL · Redis |
| **Architecture** | v3.0 — Server-authoritative MMO (production hardened) |
| **Server** | [hadal.cicibyte.com](https://hadal.cicibyte.com) |
| **Status** | Documentation complete · Phase 0-R implementation next |

---

## Overview

HADAL is a server-authoritative mobile MMO strategy game. The Unity client handles **rendering, input, and optimistic visual feedback** only. All gameplay truth lives on the backend.

```
Input → ClientPredictionSystem → CommandDispatcher (Protobuf)
     → Gateway → Game Server (HADAL.Shared validation)
     → StateDelta → VisualStateCache → UI
```

---

## Architecture (v3)

| Layer | Technology |
|-------|------------|
| Wire protocol | **Protocol Buffers** (JSON forbidden on gameplay WebSocket) |
| Shared contracts | **HADAL.Shared** (.NET Standard 2.1) |
| Client DI | **VContainer** (Project / Session / Game / UI scopes) |
| State replication | **StateSyncPipeline** — Snapshot + Delta → VisualStateCache |
| Latency UX | **ClientPredictionSystem** + RollbackAnimator |
| Events | NetworkEventBus → Translator → LocalEventBus → UI |

---

## Documentation

| Document | Description |
|----------|-------------|
| [HADAL_MASTER_DEVELOPMENT_GUIDE.md](docs/HADAL_MASTER_DEVELOPMENT_GUIDE.md) | Canonical development rules |
| [17_Technical_Architecture.md](docs/17_Technical_Architecture.md) | Backend + StateSyncPipeline |
| [18_Unity_Client_Architecture.md](docs/18_Unity_Client_Architecture.md) | VContainer, prediction, client view |
| [19_HADAL_Shared_Protocol_And_Serialization.md](docs/19_HADAL_Shared_Protocol_And_Serialization.md) | Protobuf + HADAL.Shared |
| [HadalDevelopmentRoadmap.md](docs/HadalDevelopmentRoadmap.md) | Phased delivery (Phase 0-R blocking) |
| [HADAL_SERVER_INFRASTRUCTURE.md](docs/HADAL_SERVER_INFRASTRUCTURE.md) | Server deployment |
| [ProjeDosyası.md](docs/ProjeDosyası.md) | Vision, lore & gameplay design (TR) |

---

## Project Structure

```
Hadal/
├── docs/                    # Architecture & design documentation (v3)
├── shared/                  # HADAL.Shared + proto (Phase 0-R)
├── Assets/_Hadal/Scripts/
│   ├── Core/                # State machine, events, contracts
│   ├── Managers/            # Services (pre–Phase 0-R — refactor pending)
│   ├── Gameplay/            # Grid, buildings, combat, expeditions
│   ├── Data/                # ScriptableObjects, models, enums
│   ├── UI/
│   └── Editor/
└── README.md
```

---

## Getting Started

### Prerequisites

- Unity 6 LTS
- .NET SDK 8+ (for HADAL.Shared / server, Phase 0-R)
- Git

### Unity Client

1. Clone the repository
2. Open the project folder in Unity Hub
3. Open `Assets/_Hadal` scenes after editor setup (`Hadal → Project Setup`)

> **Note:** Current Unity code is pre–Phase 0-R. See [HadalDevelopmentRoadmap.md](docs/HadalDevelopmentRoadmap.md) for the blocking refactor before gameplay phases.

---

## Development Rules (Summary)

- Server is the **single source of truth**
- **Protobuf** for all gameplay wire messages
- **HADAL.Shared** for commands, DTOs, enums, formulas — no duplicates
- **VContainer only** — no ServiceLocator, GameContext, or static singleton managers
- Client stores **VisualStateCache** only — never authoritative state
- Network events **never** bind directly to UI

Full rules: [HADAL_MASTER_DEVELOPMENT_GUIDE.md](docs/HADAL_MASTER_DEVELOPMENT_GUIDE.md)

---

## Roadmap

| Phase | Focus |
|-------|-------|
| **0-R** | Architecture hardening (HADAL.Shared, Protobuf, VContainer, prediction) — **BLOCKING** |
| **0** | Foundation post–0-R |
| **1+** | Grid build, camera, combat, alliance, world events |

Details: [HadalDevelopmentRoadmap.md](docs/HadalDevelopmentRoadmap.md)

---

## License

Proprietary — © Cicibyte. All rights reserved.
