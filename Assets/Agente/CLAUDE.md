# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Scope

Your working scope is **exclusively this directory** (`Assets/Agente/`). This folder lives inside a larger Unity project, but you must not modify anything outside it.

---

## Project Overview

**AgenticPrison** is a Unity-based multi-agent simulation of a prison escape scenario built in two phases:

- **Phase 1 (complete):** A single guard agent with perception (vision cone + audio), a `WorldState` belief model, and a fully working HTN planner that drives physical behavior.
- **Phase 2 (in progress):** A FIPA-compliant multiagent communication layer built in three layers — transport (`FIPAAgent` + `MessageBus`), reusable protocol FSMs (`ContractNetProtocol`, `InformProtocol`, `RequestProtocol`), and per-agent decision logic. For the guard, the decision layer is a second HTN tree (`BeSocial`) that runs each frame alongside the physical HTN (`BeGuard`). Both trees run on the same `WorldState`; social tasks write communicative acts instead of physical movements.

The communication layer must coordinate four concrete scenarios: fugitive spotted (Contract Net to cover exits), noise heard (Inform to claim investigation), guard tired with an assigned post (Request swap), and bid rejection (guard busy or chasing). Only communicate when the HTN cannot solve the problem alone.

For the full technical spec — architecture layers, state design, protocol FSMs, HTN social tree, file list, and 3-day plan — see [`FIPA_multiagente_contexto.md`](FIPA_multiagente_contexto.md).

---

## No build commands

This is a Unity project. There is no CLI build or test runner. Development is done through the Unity Editor. Run the scene `REAL.unity` (in `Assets/Scenes/`) to test behavior. There are no unit tests.

---

## Principles

**Simplicity is the main goal.** This is a complex multi-agent system — keep the design simple. Don't implement something if it is not needed.

**Simplicity first.** Only add communication when the HTN cannot solve the problem alone. The physical HTN already handles patrol, chase, noise investigation, and energy recovery. Communication adds coordination between agents, not replaces individual behavior.

**Two HTN trees, one `WorldState`.** Each guard runs `BeGuard` (physical) and `BeSocial` (social) each frame. Both read the same `WorldState`; only the physical HTN writes physical fields. Social tasks write communicative acts and update coordination fields (`NoiseCoveredBy`, `CoveredExits`, `AssignedTask`).

**Protocols are reusable, decisions are agent-specific.** `ContractNetProtocol`, `InformProtocol`, and `RequestProtocol` are FSMs that any agent type can use. The guard drives them from `BeSocial`; a camera drives them from a sensor callback. The protocol layer never knows which.

**No structural changes to Phase 1.** Phase 2 integrates through additions only — 5 new fields in `WorldState`, an `AssignedTask` check at the top of `BeGuard`, and `BeSocial` running alongside it. The existing HTN task tree is untouched.

---

## Namespace

All code lives under `AgenticPrison`, with sub-namespaces:
- `AgenticPrison.Core` — interfaces, `WorldState`, `HTNPlanner`
- `AgenticPrison.Physical` — sensors, actuators, map structures
- `AgenticPrison.Behavior` — HTN task tree (compound tasks, methods, primitive tasks, root task)

---

## Code conventions

- **Language**: C# targeting Unity (no `async/await`, no LINQ beyond simple projections).
- **Comments**: Spanish, in line with the existing codebase. All comments explain *why* or *what*, not the obvious.
- **Interfaces over concrete types**: Tasks, actuators, and the bridge are always expressed through interfaces (`IActuators`, `IActionBridge`, `IPrimitiveTask`, etc.).

---

## Workflow

Follow git-workflow skill autonomously. Run `git add` and `git commit` after each verified step without waiting for instruction. Create different branches for really different functionalities, merge them, but **never** commit directly to main. Let me handle that myself or if I directly tell you.
