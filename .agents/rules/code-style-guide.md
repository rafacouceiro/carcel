---
trigger: always_on
---

# **Agentic Prison Guards System In Unity**

## **1. Project Overview & File Structure**
This project implements a set of autonomous agents (Prison Guards) using a **Hierarchical Task Network (HTN)**. The core goal is to prevent prisoners from escaping.

### **Strict File Organization**
* **Agent Logic & Models**: All agent-related code, scripts, and models must reside in `Assets/Agente/`.
* **Player Reference**: The player controller is located at `Assets/Player/ControlJugador.cs`.

---

## **2. Architectural Standards**

### **A. Simplicity & Atomic Decoupling**
* **Complexity through Simplicity**: Complex behaviors must be built from small, simple, and independent C# classes.
* **Pure C# Logic**: The HTN Planner, WorldState, and Tasks must be **Pure C#**. They must not inherit from `MonoBehaviour` or use Unity namespaces (like `UnityEngine.Vector3`) directly.
* **The "Puzzle" Style**: Each Task or Method is a single file/class. Adding behavior should be as simple as "plugging in" a new class into the Domain.

### **B. Unity Abstraction (Translation Layer)**
* **Sensors (Input)**: Unity scripts that detect physics/triggers and "translate" them into the agent's Pure C# `WorldState`.
* **Actuators (Output)**: A thin wrapper (`Driver`) that receives a command from a Primitive Task and translates it into Unity-specific actions (e.g., `navMeshAgent.destination`).
* **Interface Dependency**: Logic only communicates with Unity via interfaces (e.g., `IMovable`, `IVisualSensor`).


---

## **3. Legibility**

Code needs to be unsderstandable, as it is a complex system, we need correct logging, and decision making.

---

## **5. Manual Unity Requirements (The Bridge)**
If the AI requires manual setup in the Editor, it will be documented here with a justification.

| Manual Setup Task | Justification |
| :--- | :--- |
| **SensitivePosition Tag** | Allows the `WorldStateProvider` to inject guard-spots into the Pure C# logic. |
| **Empty Objects (Nodes)** | Defines the "Graph" that the HTN uses for navigation, decoupling it from NavMesh baking. |

---

## **6. Modular Code Example (Style Preference)**
When creating tasks, follow this "Piece of the Puzzle" pattern:

```csharp
// Example: A single, simple task class
public class MoveToTask : IPrimitiveTask {
    public bool CheckPreconditions(WorldState state) => state.Fatigue < 0.9f;
    public void Execute(IMovable agent, Vector3 target) => agent.SetDestination(target);
    public void ApplyEffects(WorldState state) => state.Fatigue += 0.05f;
}