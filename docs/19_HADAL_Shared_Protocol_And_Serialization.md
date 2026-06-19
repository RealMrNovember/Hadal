# 19 — HADAL.Shared, Protocol Buffers & Network Serialization

**Status:** Mandatory production standard  
**Applies to:** Unity Client · ASP.NET Core Gateway · Game Server  
**Document Version:** 3.0

---

## 1. Serialization Standard (Mandatory)

### Decision: **Protocol Buffers (Protobuf)**

| Wire format | Allowed for |
|-------------|-------------|
| **Protobuf (binary)** | Commands, StateSnapshot, StateDelta, NetworkEvents, session bootstrap |
| **JSON** | Patch manifest (`version.json`), Addressables catalog, **debug/editor logs only** |
| **MessagePack** | **Forbidden** in production path (optional internal tooling with ADR approval only) |

### Rules

- JSON is **FORBIDDEN** for gameplay replication over WebSocket
- All `CommandEnvelope`, `StateSnapshotEnvelope`, `StateDeltaEnvelope` payloads are Protobuf-serialized
- REST auth endpoints may return JSON for HTTP ergonomics; gameplay bootstrap snapshot uses **Protobuf body** or base64 Protobuf in a typed field — never mixed JSON state trees

---

## 2. NetworkSerializationLayer

Shared abstraction used by client and server:

```
┌─────────────────────────────────────────────────────────┐
│              NetworkSerializationLayer                   │
│  Serialize<T>(T message) → ReadOnlyMemory<byte>         │
│  Deserialize<T>(ReadOnlyMemory<byte>) → T               │
│  SchemaVersion negotiation on connect                   │
│  CompatibilityLayer (forward/backward field handling)   │
└─────────────────────────────────────────────────────────┘
```

| Component | Responsibility |
|-----------|----------------|
| `INetworkSerializer` | Protobuf encode/decode entry point |
| `SchemaRegistry` | Maps message type → `MessageParser` / descriptor |
| `CompatibilityLayer` | Handles `schema_version` mismatch; rejects unsafe gaps |
| `SerializationValidator` | Max payload size, depth limits (anti-DoS) |

### Envelope structure (Protobuf)

```protobuf
message CommandEnvelope {
  uint32 schema_version = 1;
  string command_id = 2;
  uint64 client_sequence = 3;
  uint64 server_tick = 4;        // 0 on client send
  CommandType type = 5;
  bytes payload = 6;             // typed inner message
}

message StateDeltaEnvelope {
  uint32 schema_version = 1;
  uint64 server_tick = 2;
  uint64 baseline_tick = 3;      // for delta compression
  bytes payload = 4;             // StateDelta
}
```

---

## 3. HADAL.Shared Assembly (Mandatory)

**Project name:** `HADAL.Shared` (aka `HADAL.Common`)  
**Type:** .NET Standard 2.1 class library  
**Referenced by:** Unity Client (asmdef) · Gateway · Game Server

### MUST contain

| Category | Examples |
|----------|----------|
| **Commands** | `PlaceBuildingCommand`, `RemoveBuildingCommand`, `UpgradeBuildingCommand`, `StartExpeditionCommand` |
| **State DTOs** | `PlayerStateSnapshot`, `StateDelta`, `BuildingStateDto`, `ResourceStateDto` |
| **Enums** | `ResourceType`, `GridSlotType`, `CommandResultCode`, `DepthZone` |
| **Network events** | `ResourceChangedNetworkEvent`, `BuildingPlacedNetworkEvent` |
| **Simulation rules** | `PressureFormula`, `ResourceProductionFormula`, `DamageFormula` (pure static — **single definition**) |
| **Protobuf generated** | `Hadal.Protocol` namespace from `.proto` files |

### MUST NOT contain

- UnityEngine references
- MonoBehaviour / UI code
- Client presentation logic
- Server persistence (EF, Redis drivers)

### Repository layout

```
/shared/
├── HADAL.Shared/
│   ├── Commands/
│   ├── State/
│   ├── Enums/
│   ├── Events/
│   ├── Simulation/          # Formulas — server executes, client preview-hints only
│   └── HADAL.Shared.csproj
└── proto/
    ├── hadal_common.proto
    ├── hadal_commands.proto
    ├── hadal_state.proto
    └── hadal_events.proto
```

### Duplicate definition rule

**NO duplicate model definitions** on client or server.  
If a type appears outside `HADAL.Shared`, CI fails.

---

## 4. Schema Evolution Strategy

| Rule | Implementation |
|------|----------------|
| Field numbers never reused | Protobuf standard |
| New fields optional with defaults | Backward compatible |
| Breaking changes | Increment `schema_version`; deploy CompatibilityLayer |
| Deprecation | `deprecated` annotation + 2-release sunset |
| Client too old | Gateway returns `SCHEMA_INCOMPATIBLE` → force patch/update |
| Client too new | Server rejects with upgrade wait or feature flag |

### Version handshake (WebSocket connect)

1. Client sends `Hello { client_build, schema_version, protocol_version }`
2. Server responds `HelloAck { accepted, server_schema_version, min_client_schema }`
3. If incompatible → disconnect + UI patch prompt

---

## 5. State Message Definitions (Protobuf)

```protobuf
message StateSnapshot {
  uint64 server_tick = 1;
  string player_id = 2;
  ResourceState resources = 3;
  repeated BuildingState buildings = 4;
  GridState grid = 5;
  // ... heroes, map, etc.
}

message StateDelta {
  uint64 server_tick = 1;
  repeated EntityChange changes = 2;   // ID-based diff
}

message EntityChange {
  EntityType entity_type = 1;
  string entity_id = 2;
  ChangeOp op = 3;                     // CREATE, UPDATE, DELETE
  bytes entity_payload = 4;
}
```

Delta compression: server sends deltas relative to `baseline_tick` the client acknowledged.

---

## 6. Command Definitions (Shared)

All commands implement `IGameCommand` marker in Shared; Protobuf is source of truth.

| Command | Server validates |
|---------|------------------|
| `PlaceBuildingCommand` | Resources, polar slot, ring rules (Shared formulas) |
| `RemoveBuildingCommand` | Ownership, combat lock |
| `UpgradeBuildingCommand` | Level cap, costs |

Command results: `CommandResult { command_id, success, error_code, corrective_delta? }`

---

## 7. JSON Policy Summary

| Context | JSON |
|---------|------|
| WebSocket gameplay | **NO** |
| State replication | **NO** |
| `manifest/version.json` | YES (patch metadata only) |
| Addressables catalog | YES (Unity requirement) |
| Structured debug log (dev builds) | YES |
| PostgreSQL storage | Binary JSONB or normalized tables — not wire JSON to clients |

---

## 8. Related Documents

- [17_Technical_Architecture.md](./17_Technical_Architecture.md) — server pipeline
- [18_Unity_Client_Architecture.md](./18_Unity_Client_Architecture.md) — client prediction, visual cache
- [HadalDevelopmentRoadmap.md](./HadalDevelopmentRoadmap.md) — Phase 0-R tasks

---

**Document Version:** 3.0
