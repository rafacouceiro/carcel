# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Scope

Your working scope is **exclusively this directory** (`Assets/Agente/`). This folder lives inside a larger Unity project, but you must not modify anything outside it.

---

## Project Overview

**AgenticPrison** is a Unity-based multi-agent simulation of a prison escape scenario. The project is in active development across two phases:

- **Phase 1 (complete):** A single guard agent with perception (vision cone + audio), a `WorldState` belief model, and a fully working HTN planner that drives physical behavior.
- **Phase 2 (in progress):** A FIPA-compliant multiagent communication layer, architected as a BDI `CommPlanner` that operates in parallel with the HTN planner, communicating through an `IActionBridge` interface. The goal is to make this layer **agnostic to any planner architecture** — a guard (HTN), a drone (reactive), or a static camera (no locomotion) must all participate in the same FIPA protocol identically from the outside.

---

## No build commands

This is a Unity project. There is no CLI build or test runner. Development is done through the Unity Editor. Run the scene `REAL.unity` (in `Assets/Scenes/`) to test behavior. There are no unit tests.

---

## Architecture

### Namespace
All code lives under `AgenticPrison`, with sub-namespaces:
- `AgenticPrison.Core` — interfaces, `WorldState`, `HTNPlanner`
- `AgenticPrison.Physical` — sensors, actuators, map structures
- `AgenticPrison.Behavior` — HTN task tree (compound tasks, methods, primitive tasks, root task)

### Two independent reasoning planes per agent

| Plane | Component | State | Output |
|---|---|---|---|
| Physical | HTN planner (`HTNPlanner.cs`) | Reads & writes `WorldState` | `Queue<IPrimitiveTask>` executed by `Actuators.cs` |
| Social | `CommPlanner` BDI *(Phase 2)* | Reads both states; writes `SocialState` | FIPA communicative acts via `MessageBus` |

The two planes **never know about each other**. The only crossing point is `IActionBridge`, called by `CommPlanner` when a task must be delegated to the physical planner.

### Core types

- **`WorldState`** (`Core/WorldState.cs`): Physical belief model — position, energy, fugitive memory, auditory/visual traces, map reference. Phase 2 adds `AssignedTask`, `ActiveContractId`, `dirty`.
- **`SocialState`** *(Phase 2)*: Social belief model — known agents, reliability scores, active contracts, conversation records. Written exclusively by `CommPlanner`.
- **`Brain.cs`**: Central MonoBehaviour. Implements `IVisionEvents`, `INoiseReceiver`, `ICellEventReceiver`. Owns `WorldState`, runs `HTNPlanner` on every `Update()`, and calls `ForzarReplanificacion()` on perception interrupts.

### HTN planner

```
BeGuard (RootTask)
├── SelectEmergency → EmergencyTask (FugitiveInVision)
│   ├── CatchMethod (distance < 1.5f) → GameOverTask
│   └── ChaseMethod → ChangeFlashLight, ChaseTask
├── SelectInvestigation → InvestigationTask
│   ├── SelectInvestigateEscape → InvestigateEscapeTask (fresh LKP, < 25s)
│   │   ├── PredictivePursuitMethod (< 2s)
│   │   └── WideSweepMethod (< 35s, 2nd-degree room expansion + greedy)
│   ├── InvestigateNoiseMethod (LNP < 10s, greedy on nearest key points)
│   └── InvestigateLocationMethod (global key point greedy sweep)
└── SelectRoutine → RoutineTask
    ├── PatrolMethod (PrisonerInCell, DFS on quadrant RoomNode graph)
    └── SelectEnergyRecovery → EnergyRecoveryTask
        ├── GuardKeySpotMethod → move + LookAroundTask (recovers energy)
        └── TakeBreakMethod → TakeAirTask (fallback)
```

The planner works on a **cloned `WorldState`**. `CheckPreconditions` and `ApplyEffects` run on the clone; if a full plan is found, it is returned as a `Queue<IPrimitiveTask>`. `Execute()` on each primitive runs against the real actuators and state each frame.

### Sensor architecture (Emitter–Manager–Receiver)

- **Vision** (`VisionSystem.cs`): Every entity calls `VisionManager.EmitPresence()` each frame. `VisionSystem` runs `CheckPhysicalVisibility` (range + angle + raycast) and fires `IVisionEvents` callbacks on `Brain`.
- **Audition** (`AuditionSystem.cs`): `NoiseManager` broadcasts `NoiseEvent`; `Brain` implements `INoiseReceiver.OnNoiseHeard()` with distance-based error and priority filtering (visual cue > audio cue, self-noise discarded, nearby-guard heuristic).

### Map

`PrisonMap` is a singleton managing `RoomNode` objects (logical rooms with `BoxCollider` + `connectedRooms` graph) and `WayPointData` (waypoints tagged as `isKeyPoint`, `isPatrolCheckpoint`, or `isCell`). Navigation algorithms use `NavMesh.CalculatePath` for real cost estimates.

### IActionBridge (Phase 2 integration point)

```csharp
public interface IActionBridge {
    bool QueryCapability(TaskType taskType);
    bool AssignTask(TaskDescriptor task);
    void CancelTask(string taskId);
    event Action<string> OnTaskCompleted;
    event Action<string, string> OnTaskFailed;
}
```

Each agent type provides its own implementation (`HTNActionBridge` writes `WorldState.AssignedTask` and calls `ForzarReplanificacion`). The `CommPlanner` only ever calls this interface — never the HTN directly.

---

## Code conventions

- **Language**: C# targeting Unity (no `async/await`, no LINQ beyond simple projections).
- **Comments**: Spanish, in line with the existing codebase. All comments explain *why* or *what*, not the obvious.
- **Interfaces over concrete types**: Tasks, actuators, and the bridge are always expressed through interfaces (`IActuators`, `IActionBridge`, `IPrimitiveTask`, etc.).
- **No structural modification to Phase 1 HTN**: Phase 2 work integrates through additions (`AssignedTask` check at top of `BeGuard`, new `IActionBridge` impl), not refactors of existing task tree logic.
- **Rigor sobre pragmatismo**: The project has academic requirements — preserve theoretical correctness of the BDI cycle (Belief Revision → Option Generation → Filter → Execute, in that order, every frame), FIPA performative semantics, and the clean separation between physical and social planes.

---

## Key design constraints for Phase 2

1. `CommPlanner` reads `WorldState` **read-only**. Only `HTNPlanner` writes `WorldState`.
2. `SocialState` is written **exclusively** by `CommPlanner`. No other component touches it.
3. The BDI cycle runs **before** the HTN tick each frame (steps 3–7 precede steps 8–10 in the per-frame order).
4. Maximum **2–3 communicative acts per frame** (`MAX_ACTS_PER_FRAME`).
5. `MessageBus` delivers CFPs only to agents **subscribed to the relevant ontology**; all subsequent messages in the same conversation are **unicast by `conversationId`**.
6. Reliability scores start at **0.5** for unknown agents and are updated on `InformDone` (success) / `Failure`.
