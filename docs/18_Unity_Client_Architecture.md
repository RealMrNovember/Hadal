# 18 — Unity Client Architecture

Unity 6 LTS · URP · Android / iOS  
**Role:** Rendering + Input + Optimistic Visual Feedback — **NOT** simulation authority.

**Document Version:** 3.0 — Production Hardened  
**Companion:** [21_HADAL_Shared_Protocol_And_Serialization.md](./21_HADAL_Shared_Protocol_And_Serialization.md) · [15_Monetization.md](./15_Monetization.md)

---

## 1. Dependency Injection — VContainer (Mandatory)

**Decision:** [VContainer](https://github.com/hadashiA/VContainer) — only approved DI framework.

### FORBIDDEN (zero tolerance)

| Pattern | Status |
|---------|--------|
| `ServiceLocator` / `GameServiceContainer` | **REMOVED** |
| `GameContext.Current.Resolve<T>()` | **REMOVED** |
| Static manager singletons | **REMOVED** |
| Hidden service locator fallback | **REMOVED** |
| `FindObjectOfType` in gameplay | **REMOVED** |
| Implicit global state | **REMOVED** |

All dependencies: **constructor injection** (preferred) or `[Inject]` on MonoBehaviour in scoped installers only.

### Lifetime scopes

| Scope | Lifetime | Registered services |
|-------|----------|---------------------|
| **Project** | App | `INetworkSerializer`, `ILocalEventBus`, `INetworkEventBus`, `IEventValidationLayer`, `IPatchService`, transport config |
| **Session** | Login → Logout | `IStateSyncService`, `ICommandDispatcher`, `INetworkStateReceiver`, `IClientPredictionSystem`, `ICommandReconciliationSystem`, `IPredictionBuffer` |
| **Game** | Scene | Grid build presenter, camera rig |
| **UI** | Screen | Presenters |

```csharp
// HadalSessionLifetimeScope — conceptual
builder.Register<IClientPredictionSystem, ClientPredictionSystem>(Lifetime.Scoped);
builder.Register<ICommandReconciliationSystem, CommandReconciliationSystem>(Lifetime.Scoped);
builder.Register<IPredictionBuffer, PredictionBuffer>(Lifetime.Scoped);
builder.Register<IRollbackAnimator, RollbackAnimator>(Lifetime.Scoped);
builder.Register<IStateSyncService, StateSyncService>(Lifetime.Scoped);
builder.Register<INetworkToLocalEventTranslator, NetworkToLocalEventTranslator>(Lifetime.Scoped);
builder.Register<IEventValidationLayer, EventValidationLayer>(Lifetime.Singleton);
```

---

## 2. Bootstrap Flow

```
AppLaunch
  └─ HadalProjectLifetimeScope
       └─ BootSceneController.RunAsync()
            1. Transport config (hadal.cicibyte.com)
            2. PatchService (JSON manifest OK — not gameplay)
            3. Addressables catalog
            4. Auth + Protobuf schema handshake
            5. StateSyncPipeline.RequestSnapshotAsync() — Protobuf StateSnapshot
            6. GameStateMachine → MainMenu
```

---

## 3. HADAL.Shared Reference (Client)

Unity client **MUST** reference `HADAL.Shared` assembly:

- Commands sent via `CommandDispatcher` use Shared Protobuf types
- `VisualStateCache` populated from Shared `StateSnapshot` / `StateDelta` DTOs
- Preview validation hints use Shared `Simulation` formulas — **server always wins**

**Forbidden:** duplicate enums, commands, or state DTOs in client assemblies.

---

## 4. NetworkSerializationLayer (Client)

| Responsibility | Implementation |
|----------------|----------------|
| Encode commands | `INetworkSerializer.Serialize<PlaceBuildingCommand>()` |
| Decode deltas | `Deserialize<StateDeltaEnvelope>()` |
| Schema version | Included in every envelope |
| JSON | **Debug builds only** — `#if HADAL_DEBUG_JSON` |

WebSocket frames: **binary Protobuf only** in production.

---

## 5. StateSyncPipeline (Client)

```
NetworkStateReceiver (Protobuf decode)
    → EntityReconciliationSystem (entity_id diff vs VisualStateCache)
    → StateSyncService.ApplySnapshot / ApplyDelta
    → VisualStateCache update
    → NetworkToLocalEventTranslator
    → LocalEventBus
    → Presenters / Views
```

### VisualStateCache vs Authoritative State

| Store | Authority | Purpose |
|-------|-----------|---------|
| ServerState (remote) | **YES** | Never stored locally as editable |
| VisualStateCache | **NO** | Display + prediction overlay |
| PredictionBuffer | **NO** | Tentative optimistic layer |

Client **NEVER** writes VisualStateCache to disk as gameplay save.

Session cache (allowed): `sessionToken`, `catalogHash`, `lastAckServerTick`, `schemaVersion`.

---

## 6. ClientPredictionSystem

Latency compensation for 100–200ms mobile networks.

### Components

| Component | Role |
|-----------|------|
| **ClientPredictionSystem** | Applies optimistic visual on user action |
| **PredictionBuffer** | Ring buffer of pending `{ command_id, sequence, predicted_visual_delta }` |
| **CommandReconciliationSystem** | Compares server `CommandResult` + corrective `StateDelta` |
| **RollbackAnimator** | Smooth correction — lerp/slerp, scale pop, ghost fade — **no hard snap** |

### Flow

```
User confirms build
  → ClientPredictionSystem: show building ghost → solid (optimistic)
  → PredictionBuffer.Push(command)
  → CommandDispatcher.SendAsync (Protobuf)
  → [100-200ms latency]
  → CommandResult + StateDelta received
  → IF match: confirm prediction, remove from buffer
  → IF mismatch: RollbackAnimator smooth-correct to server state
```

### Rules

- Prediction affects **VisualStateCache tentative layer only**
- Never send predicted values as command payload facts (e.g. don't send deducted resources)
- Rollback must complete within 300ms visual transition
- Under reject: show LocalEventBus `BuildRejectedVisual` + restore resources display from server delta

---

## 7. Event System (Final)

### EventOwnershipPolicy

| EventBus | Allowed publishers | Allowed subscribers | Mutates UI? |
|----------|-------------------|---------------------|-------------|
| **LocalEventBus** | Presentation, Audio, Camera | Presentation, VFX | YES |
| **NetworkEventBus** | NetworkStateReceiver only | StateSync, Translator, Reconciliation | **NO** |
| **Translator** | Network handlers | LocalEventBus | Indirect only |

### Mandatory pipeline

```
Server Protobuf message
  → NetworkStateReceiver
  → NetworkEventBus (internal)
  → INetworkToLocalEventTranslator
  → LocalEventBus
  → UI Presenter
```

**Network events NEVER directly mutate UI.**

### EventValidationLayer

- Debug + development builds: asserts subscriber assembly namespace
- Blocks `Presentation` assembly from subscribing to `INetworkEventBus`
- Logs cross-bus leakage attempts

---

## 8. Project Structure (v3)

```
Assets/_Hadal/
├── Scripts/
│   ├── Infrastructure/
│   │   ├── DI/                    # VContainer LifetimeScopes
│   │   ├── Network/               # Serializer, receiver, dispatcher
│   │   ├── StateSync/             # Pipeline, reconciliation, visual cache
│   │   ├── Prediction/            # PredictionBuffer, rollback animator
│   │   ├── Events/                # Buses, translator, validation
│   │   └── Patch/
│   ├── Presentation/              # Views — NO network subscriptions
│   └── Data/                      # ScriptableObjects (static config)
└── References/
    └── HADAL.Shared.dll           # Or asmdef reference to shared project
```

---

## 9. Circular Grid (Prediction-Aware)

1. UI select building → LocalEventBus
2. Ghost preview (local)
3. Confirm → **ClientPredictionSystem** optimistic placement visual
4. `PlaceBuildingCommand` (Protobuf) via CommandDispatcher
5. Server validates → StateDelta
6. Reconciliation confirms or RollbackAnimator corrects

Client `ValidatePlacement` uses Shared formulas as **hint only**.

---

## 10. Performance (Mobile)

- Protobuf binary (smaller than JSON — bandwidth + GC)
- Batch StateDelta apply per server tick
- Object pooling for VFX, grid slots
- PredictionBuffer capped (max 32 pending commands)
- No gameplay mutation in `Update()` — event-driven views
- VContainer scoped services — no static lookups

---

## 11. Testing

| Test | Type |
|------|------|
| Protobuf round-trip (Shared) | Unit |
| Prediction + reconciliation | Unit + integration |
| Rollback animator timing | PlayMode |
| EventValidationLayer blocks UI on NetworkBus | Unit |
| Latency simulation 100/200/500ms | Integration |

---

## 12. Risk Assessment (Client)

| System | Risk |
|--------|------|
| VContainer DI | **LOW** |
| Protobuf serialization | **LOW** |
| VisualStateCache | **LOW** |
| Client prediction + rollback | **MEDIUM** — requires latency test suite |
| Event boundary enforcement | **LOW** |

---

**Document Version:** 3.0
