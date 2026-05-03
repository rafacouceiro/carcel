# Plan de Implementación Definitivo — Sistema FIPA Multiagente
## Contexto para Claude Code — Fase 2 del proyecto AgenticPrison

---

## 0. Estado actual del proyecto

El proyecto ya tiene implementado en C# (Unity):

- **`GuardBrain.cs`** — hereda de `FIPAAgent`, implementa `INoiseReceiver`, `IVisionEvents`, `ICellEventReceiver`. Ya tiene `OnFugitiveSpotted`, `OnFugitiveLost`, `OnNoiseHeard`, `OnCellFoundOpen`. Tiene `ForzarReplanificacion()`.
- **`HTNPlanner.cs`** — planificador HTN completo con `FindPlan`, `TryDecomposeCompound`, `TryProcessPrimitive`, `CopyState`.
- **`WorldState.cs`** — estado del agente con campos físicos, visuales, auditivos y de navegación.
- **`PrisonMap.cs`** — singleton con `GetSection`, `GetCurrentNode`, `GetAllKeyPoints`, `GetAllCellPoints`.
- **`FIPAAgent.cs`** — clase base abstracta parcialmente implementada. `GuardBrain` ya hereda de ella.

El HTN físico ya maneja correctamente: patrulla, persecución (ChaseTask), investigación de ruidos (InvestigateNoiseMethod), recuperación de energía (EnergyRecoveryTask), investigación de fuga (InvestigateEscapeTask).

**Regla de oro**: si el HTN ya lo resuelve bien sin comunicación, no añadir comunicación. Solo comunicar cuando se necesita coordinación entre agentes.

---

## 1. Qué hace el HTN físico vs qué añade la comunicación

| Situación | HTN físico ya hace | Comunicación añade |
|---|---|---|
| `PrisonerInCell = false` | Todos investigan — `InvestigationTask` en todos | Coordinar quién va a qué salida y qué habitación |
| `FugitiveInVision = true` | `ChaseTask` automático — máxima prioridad | Informar posición al equipo, subastar cobertura de salidas |
| `LastNoisePosition` fresco | `InvestigateNoiseMethod` automático | Inform para evitar que todos vayan al mismo ruido |
| `Energy < umbral` | `EnergyRecoveryTask` automático | Request de swap para no dejar puesto sin cubrir |
| `AssignedTask != null` (nuevo) | `DecomposeAssignedTask` con prioridad máxima | Nada más — HTN ya lo ejecuta |

---

## 2. Arquitectura — tres capas

### Capa 1 — Transporte (común, no cambia nunca)
`FIPAAgent` + `MessageBus` + `ACLMessage`
- `Send(msg)`, `Broadcast(msg)`, cola de entrada por conversación
- Procesa 2-3 mensajes por frame — nunca bloquea el Update
- No sabe nada de protocolos ni decisiones

### Capa 2 — Protocolos (autómatas FSM reutilizables)
`ICommProtocol` y sus implementaciones
- `ContractNetProtocol`: `Idle → CfpSent → Collecting → Evaluating → Done/Failed`
- `InformProtocol`: `Idle → Sent → Done` (fire-and-forget)
- `RequestProtocol`: `Idle → Sent → Agreed/Refused` (para swap)
- **Reutilizables por cualquier agente** — guardia, cámara, dron futuro

### Capa 3 — Decisión (específica de cada agente)
- **Guardia**: HTN social lee WorldState y decide cuándo lanzar protocolos
- **Cámara**: llama `LaunchProtocol()` directamente desde su sensor
- **Dron (futuro)**: su propia lógica, mismos protocolos

### Jerarquía de clases

```
MonoBehaviour
  └── FIPAAgent                    (abstracta — capa transporte)
        ├── GuardBrain             (HTN físico + HTN social)
        ├── CameraAgent            (llama protocolos directamente)
        └── DroneAgent             (futuro)

ICommProtocol
  ├── ContractNetProtocol
  ├── InformProtocol
  └── RequestProtocol
```

---

## 3. WorldState — exactamente 5 campos nuevos

Añadir a `WorldState.cs`:

```csharp
// Tarea asignada externamente por contrato ganado.
// HTN físico la ejecuta con prioridad máxima en BeGuard.
public TaskDescriptor AssignedTask = null;

// Prioridad de la tarea física actual.
// Determina si el agente acepta o rechaza bids entrantes.
public TaskPriority CurrentTaskPriority = TaskPriority.Idle;

// Agentes que forman el equipo activo.
// El HTN social no lanza subastas redundantes si el equipo ya está formado.
public List<string> TeamMembers = new List<string>();

// Salidas ya cubiertas por contratos activos.
// Evita lanzar ContractNet para la misma salida dos veces.
public List<Vector3> CoveredExits = new List<Vector3>();

// AgentId del guardia que investiga el ruido actual. null si nadie.
// Precondición HTN físico: InvestigateNoiseMethod solo si
// NoiseCoveredBy == null || NoiseCoveredBy == AgentName
public string NoiseCoveredBy = null;
```

Añadir a `Clone()`:
```csharp
AssignedTask        = this.AssignedTask,
CurrentTaskPriority = this.CurrentTaskPriority,
TeamMembers         = new List<string>(this.TeamMembers),
CoveredExits        = new List<Vector3>(this.CoveredExits),
NoiseCoveredBy      = this.NoiseCoveredBy,
```

También añadir en `CopyState()` de `HTNPlanner.cs`.

### TaskPriority enum

```csharp
public enum TaskPriority {
    Idle        = 0,
    Patrol      = 1,
    EnergyRest  = 2,
    InvestNoise = 3,
    CoverExit   = 4,
    Investigate = 4,
    Chase       = 5,   // nunca se interrumpe
    GameOver    = 6,   // nunca se interrumpe
}
```

> **Nota**: `FugitiveInVision = true` es condición de rechazo absoluto de cualquier bid, independientemente de la prioridad ofrecida.

---

## 4. TaskDescriptor

```csharp
public class TaskDescriptor {
    public TaskType     Type;
    public Vector3      TargetPosition;
    public TaskPriority Priority;
    public string       ContractId;    // conversación que lo originó
    public float        Deadline;      // Time.time de expiración
}

public enum TaskType {
    CoverPosition,
    Investigate,
    // ampliar según necesidad
}
```

---

## 5. ACLMessage

```csharp
public class ACLMessage {
    public Performative Performative;
    public string       Sender;
    public string       Receiver;       // null = broadcast
    public string       ConversationId;
    public string       InReplyTo;
    public float        ReplyBy;        // Time.time de expiración, 0 = sin límite
    public object       Content;        // TaskDescriptor, ProposalContent, etc.
}

public enum Performative {
    Cfp, Propose, Refuse,
    AcceptProposal, RejectProposal,
    Inform, InformDone, Failure,
    Request, Agree
}
```

---

## 6. FIPAAgent — completar

```csharp
public abstract class FIPAAgent : MonoBehaviour {

    public abstract string AgentId { get; }

    private Dictionary<string, ICommProtocol> _conversations = new();
    private Queue<ACLMessage> _pendingIncoming = new();
    private const int MAX_CONVERSATIONS = 3;

    protected virtual void Start() {
        MessageBus.Instance.Register(this);
    }

    protected virtual void Update() {
        ProcessIncoming(GetWorldState());
    }

    // Punto de entrada único para lanzar cualquier protocolo
    protected void LaunchProtocol(ICommProtocol protocol) {
        if (_conversations.Count >= MAX_CONVERSATIONS) return;
        _conversations[protocol.ConversationId] = protocol;
        protocol.Init(this);
    }

    // Llamado por MessageBus al recibir un mensaje
    public void ReceiveMessage(ACLMessage msg) {
        _pendingIncoming.Enqueue(msg);
    }

    protected void ProcessIncoming(WorldState ws, int maxPerFrame = 2) {
        int processed = 0;
        while (_pendingIncoming.Count > 0 && processed < maxPerFrame) {
            var msg = _pendingIncoming.Dequeue();

            // Descartar si expiró
            if (msg.ReplyBy > 0 && Time.time > msg.ReplyBy) continue;

            // Si pertenece a conversación activa, avanzar FSM
            if (_conversations.TryGetValue(msg.ConversationId, out var fsm)) {
                fsm.Tick(msg, ws);
                if (fsm.IsComplete) _conversations.Remove(msg.ConversationId);
                processed++;
                continue;
            }

            // Mensaje nuevo — delegar a la subclase
            OnIncomingMessage(msg, ws);
            processed++;
        }
    }

    // Cada subclase decide qué hacer con mensajes sin conversación activa
    protected abstract void OnIncomingMessage(ACLMessage msg, WorldState ws);
    protected abstract WorldState GetWorldState();
}
```

---

## 7. ICommProtocol — interfaz base

```csharp
public interface ICommProtocol {
    string ConversationId { get; }
    bool   IsComplete     { get; }

    void Init(FIPAAgent agent);
    void Tick(ACLMessage msg, WorldState ws);       // avanza por mensaje
    void Tick(float currentTime, WorldState ws);    // avanza por deadline
}
```

---

## 8. ContractNetProtocol — estados y transiciones

| Estado | Rol | Transición | Acción |
|---|---|---|---|
| `Idle` | ambos | `Init()` | envía cfp (iniciador) |
| `CfpSent` | iniciador | deadline expirado | → `Evaluating` si hay propuestas, `Failed` si no |
| `CfpSent` | iniciador | `Propose` recibido | acumula, permanece |
| `CfpSent` | iniciador | `Refuse` recibido | descarta, permanece |
| `CfpReceived` | participante | cfp llegó | evalúa prioridad → `Proposed` o `Refused` |
| `Proposed` | participante | `AcceptProposal` | → `Executing`: `WS.AssignedTask = task; ForzarReplan()` |
| `Proposed` | participante | `RejectProposal` | → `Idle`: `WS.PendingCfp = null` |
| `Evaluating` | iniciador | (interno) | elige mejor oferta → envía Accept + Rejects |
| `AcceptSent` | iniciador | `InformDone` | → `Done`: actualiza `CoveredExits` o `TeamMembers` |
| `AcceptSent` | iniciador | `Failure` | → `Failed`: puede re-lanzar |
| `Executing` | participante | tarea completada | envía `InformDone` → `Done` |
| `Done/Failed` | ambos | — | `IsComplete = true` |

> El buffer de mensajes es **por conversación**, no global. Un mensaje con `ConversationId` desconocido se descarta silenciosamente.

---

## 9. HTN social del guardia

Usa exactamente el mismo `HTNPlanner.cs` sin cambios. Las tareas primitivas son actos comunicativos en lugar de movimientos.

### Árbol BeSocial

```
BeSocial (raíz)
  ├── CoordinateFlightMethod
  │     precond: FugitiveInVision && TeamMembers.Count == 0
  │     descomp: LaunchExitCfps → LaunchRoomCfps → SendInform(chasing)
  │
  ├── RespondToBidMethod
  │     precond: PendingCfp != null
  │              && !FugitiveInVision
  │              && cfp.Priority > CurrentTaskPriority
  │              && Energy > 15f
  │     descomp: EvaluateCostTask → SendProposeTask
  │
  ├── RefuseBidMethod
  │     precond: PendingCfp != null  (fallback)
  │     descomp: SendRefuseTask
  │
  ├── RequestSwapMethod
  │     precond: Energy < 20 && AssignedTask != null
  │              && TeamMembers.Count > 0
  │     descomp: SendSwapRequestTask
  │
  ├── InformNoiseMethod
  │     precond: LastNoisePosition != zero
  │              && NoiseCoveredBy == null
  │              && !FugitiveInVision
  │     descomp: SendInform(investigating-noise) → SetNoiseCoveredByMe
  │
  └── SocialIdleMethod (fallback)
        precond: true
        descomp: WaitTask
```

### Tareas primitivas sociales

| Tarea | Precondición | Efecto en WS | Acto comunicativo |
|---|---|---|---|
| `LaunchExitCfpsTask` | `!CoveredExits.Contains(exit)` | `ActiveContracts.Add(exit)` | `Broadcast(Cfp, cover-exit)` |
| `LaunchRoomCfpsTask` | room no asignada | `TeamAssignments.Add(room)` | `Broadcast(Cfp, investigate-room)` |
| `SendProposeTask` | `ProposalCost` calculado | `PendingCfp = null` | `Reply(Propose, cost)` |
| `SendRefuseTask` | `PendingCfp != null` | `PendingCfp = null` | `Reply(Refuse)` |
| `SendInformTask` | `true` | `NoiseCoveredBy = AgentName` | `Broadcast(Inform, tipo)` |
| `SendSwapRequestTask` | `TeamMembers.Count > 0` | — | `Send(Request, teamMember)` |

> El HTN social usa efectos **optimistas**: asume que los contratos lanzados tendrán éxito. Si fallan, la FSM escribe el resultado real en WorldState y el HTN social replanifica.

---

## 10. HTN físico — cambios mínimos

### Añadir `AssignedTaskMethod` como primer método de `BeGuard`

```csharp
// Primera comprobación en BeGuard — tiene prioridad sobre todo, incluido EmergencyTask
if (state.AssignedTask != null) {
    return DecomposeAssignedTask(state.AssignedTask);
}

Queue<IPrimitiveTask> DecomposeAssignedTask(TaskDescriptor task) {
    return task.Type switch {
        TaskType.CoverPosition =>
            [ChangeFlashLight, MoveTask(task.TargetPosition),
             LookAroundTask(x3), InformDoneTask(task.ContractId)],
        TaskType.Investigate =>
            [ChangeFlashLight, MoveTask(task.TargetPosition),
             LookAroundTask(x2), InformDoneTask(task.ContractId)],
        _ => throw new ArgumentException()
    };
}
```

> **Excepción**: si `FugitiveInVision = true` se activa mientras se ejecuta `AssignedTask`, `ForzarReplanificacion()` hace que `EmergencyTask` prevalezca. La persecución siempre tiene prioridad absoluta.

### Precondiciones ajustadas

| Método HTN | Precondición añadida | Efecto |
|---|---|---|
| `InvestigateNoiseMethod` | `NoiseCoveredBy == null \|\| NoiseCoveredBy == AgentName` | Solo un guardia investiga cada ruido |
| `EnergyRecoveryTask` | `AssignedTask == null` | No descansa si tiene puesto sin swap |
| `PatrolMethod` | sin cambios | Ya funciona bien |
| `ChaseMethod` | sin cambios | EmergencyTask ya tiene máxima prioridad |

### InformDoneTask

```csharp
public class InformDoneTask : IPrimitiveTask {
    public bool CheckPreconditions(WorldState state) => true;

    public void ApplyEffects(WorldState state) {
        state.AssignedTask     = null;
        state.ActiveContractId = null;
    }

    public TaskExecutionStatus Execute(IActuators act, WorldState state) {
        state.AssignedTask     = null;
        state.ActiveContractId = null;
        // La ConversationFSM activa envía InformDone al iniciador automáticamente
        return TaskExecutionStatus.Success;
    }
}
```

---

## 11. Los cinco escenarios

### 11.1 Fuga confirmada

**Disparador**: `OnFugitiveSpotted()` → `FugitiveInVision = true`, `PrisonerInCell = false`

```
GuardA ve al fugitivo
├── HTN físico: ChaseTask (CurrentTaskPriority = Chase = 5)
└── HTN social: CoordinateFlightMethod
      → LaunchExitCfpsTask por cada salida en PrisonMap no en CoveredExits
      → LaunchRoomCfpsTask por salas adyacentes a LastKnownPosition
      → SendInform(chasing, myPosition)

GuardB recibe cfp(CoverExit, priority=4):
  Patrol(1) < CoverExit(4) && Energy > 15 && !FugitiveInVision
  → propone con coste NavMesh(myPosition → exit)

GuardA evalúa al deadline → acepta mejor oferta (menor coste)
GuardB recibe AcceptProposal:
  → WS.AssignedTask = {CoverPosition, exitNorte, Priority=4}
  → ForzarReplanificacion()
  → HTN físico: BeGuard → AssignedTask != null → DecomposeAssignedTask
  → [MoveTask(exitNorte), LookAroundTask x3, InformDoneTask]

GuardB llega → InformDoneTask → WS.AssignedTask = null
ConversationFSM → InformDone → GuardA: WS.CoveredExits.Add(exitNorte)
```

**Dissolve**:
- `PrisonerInCell = true` → `Inform(dissolve)` broadcast → limpiar `TeamMembers`, `CoveredExits`, `AssignedTask` → todos vuelven a `RoutineTask`
- `PrisonerInCell = false` && nadie ve al fugitivo → mantener cobertura, HTN social puede lanzar nuevas subastas

### 11.2 Ruido — Inform directo

```
GuardA oye ruido → LastNoisePosition actualizado
HTN social: InformNoiseMethod activo (NoiseCoveredBy == null)
  → SendInform(investigating-noise, noisePos)
  → NoiseCoveredBy = "GuardA"

GuardB recibe Inform → OnIncomingMessage → WS.NoiseCoveredBy = "GuardA"
HTN físico GuardB: InvestigateNoiseMethod
  precond: NoiseCoveredBy == null || NoiseCoveredBy == AgentName
  → FALLA → GuardB no va al ruido

Si GuardA falla o no responde en tiempo → NoiseCoveredBy expira → null
→ cualquier otro guardia puede ir
```

> No se necesita ContractNet para el ruido. Un Inform es suficiente. El primero que informa se adjudica la investigación.

### 11.3 Swap por cansancio

```
GuardA: Energy < 20 && AssignedTask != null && TeamMembers.Count > 0
HTN social: RequestSwapMethod
  → Send(Request{task: myAssignedTask, position: myPosition}, teamMember)

GuardB recibe Request:
  CurrentTaskPriority(Patrol=1) < task.Priority(CoverExit=4)
  → Agree
  → WS.AssignedTask = task recibido
  → ForzarReplanificacion()

GuardA recibe Agree:
  → WS.AssignedTask = null
  → WS.CurrentTaskPriority = EnergyRest
  → HTN físico: EnergyRecoveryTask

GuardA recibe Refuse:
  → HTN social: SocialIdleMethod (sin apoyo, aguanta)
```

### 11.4 Política de rechazo de bids

| Caso | Política | Razón |
|---|---|---|
| `FugitiveInVision = true` | Refuse automático sin evaluar | Nunca abandonar persecución |
| `Energy < 15` | Refuse automático | No comprometerse sin energía |
| `cfp.Priority <= CurrentTaskPriority` | Refuse automático | No interrumpir tarea más importante |
| `MAX_CONVERSATIONS` alcanzado | Refuse automático | No saturar conversaciones |
| `cfp` de posición en `CoveredExits` | Refuse automático | No cubrir lo ya cubierto |
| cfp válido | Evaluar coste NavMesh y proponer | Caso normal |

### 11.5 Queries (opcional, fase posterior)

`QueryRef` / `QueryIf` para preguntar si una zona está cubierta antes de lanzar un ContractNet innecesario. Implementar después de validar los cuatro escenarios anteriores.

---

## 12. Integración trivial — CameraAgent

```csharp
public class CameraAgent : FIPAAgent {
    public override string AgentId => gameObject.name;

    private MotionSensor _sensor;

    protected override void Start() {
        base.Start();
        _sensor = GetComponent<MotionSensor>();
    }

    void Update() {
        base.Update();

        if (_sensor.DetectsSignificantMovement()) {
            // Una línea. Eso es todo lo que la cámara necesita saber.
            LaunchProtocol(new ContractNetProtocol(
                new TaskDescriptor {
                    Type           = TaskType.Investigate,
                    TargetPosition = _sensor.DetectedPosition,
                    Priority       = TaskPriority.Investigate
                }
            ));
        }
    }

    protected override void OnIncomingMessage(ACLMessage msg, WorldState ws) {
        // La cámara no tiene WorldState locomotor
        // Solo loguea o ignora los resultados de contratos lanzados
    }

    protected override WorldState GetWorldState() => null; // sin WorldState físico
}
```

---

## 13. Ficheros a crear y modificar

### Nuevos (en este orden)
```
Communication/
  ACLMessage.cs
  Performative.cs
  MessageBus.cs
  ICommProtocol.cs
  ContractNetProtocol.cs
  InformProtocol.cs
  RequestProtocol.cs

Agents/Core/
  TaskDescriptor.cs
  TaskType.cs
  TaskPriority.cs

Agents/Guard/Social/
  GuardSocialHTN.cs         ← árbol BeSocial + todos sus métodos
  LaunchExitCfpsTask.cs
  LaunchRoomCfpsTask.cs
  SendProposeTask.cs
  SendRefuseTask.cs
  SendInformTask.cs
  SendSwapRequestTask.cs
  EvaluateCostTask.cs

Agents/Guard/Physical/
  InformDoneTask.cs

Agents/
  CameraAgent.cs
```

### Modificar (cambios mínimos)
```
WorldState.cs         ← +5 campos + Clone() + CopyState()
HTNPlanner.cs         ← +AssignedTaskMethod en BeGuard + DecomposeAssignedTask
GuardBrain.cs         ← completar OnIncomingMessage + ForzarReplanificacionSocial
FIPAAgent.cs          ← completar ProcessIncoming + LaunchProtocol
```

### No tocar
```
VisionSystem.cs, VisionManager.cs, NoiseManager.cs,
Actuators.cs, PrisonMap.cs, RoomNode.cs, WayPointData.cs,
ProximityButton.cs, CellDoorSlide.cs, ControlJugador.cs,
todas las tareas HTN físicas existentes (MoveTask, ChaseTask, etc.)
```

---

## 14. Plan de 3 días

### Día 1 — Infraestructura base
- `ACLMessage.cs`, `Performative.cs`, `TaskDescriptor.cs`, `TaskType.cs`, `TaskPriority.cs`
- `MessageBus.cs` — Singleton: `Register`, `Send`, `Broadcast`
- Completar `FIPAAgent.cs` — `LaunchProtocol`, `ProcessIncoming`, `ReceiveMessage`
- `GuardBrain.cs` — verificar que hereda correctamente, stub de `OnIncomingMessage`

**Verificación**: dos guardias intercambian un `Inform` en consola. El juego funciona exactamente igual que en fase 1.

### Día 2 — Protocolos y Contract Net para fuga
- `ICommProtocol.cs`, `ContractNetProtocol.cs`, `InformProtocol.cs`
- `WorldState.cs` — +5 campos, `Clone()`, `CopyState()`
- `HTNPlanner.cs` — `AssignedTaskMethod` + `DecomposeAssignedTask`
- `InformDoneTask.cs`
- `GuardSocialHTN.cs` — `CoordinateFlightMethod`, `RespondToBidMethod`, `RefuseBidMethod`

**Verificación**: fuga confirmada → GuardA persigue → GuardB cubre salida → `InformDone` recibido. GuardC con Chase activo rechaza automáticamente el cfp.

### Día 3 — Ruido, swap y cámara
- `InformNoiseMethod` en HTN social + precondición en `InvestigateNoiseMethod` HTN físico
- `RequestProtocol.cs` + `RequestSwapMethod` en HTN social
- Ajuste precondición en `EnergyRecoveryTask`
- `CameraAgent.cs`

**Verificación**: solo un guardia investiga el ruido. Guardia cansado cede puesto con cobertura garantizada. Cámara lanza contrato en una línea.

---

## 15. Notas de implementación

- **`ChaseTask` nunca se interrumpe**. Si `FugitiveInVision = true`, el agente rechaza todo automáticamente.
- **El HTN social usa los mismos efectos optimistas que el HTN físico**: simula que los contratos tendrán éxito. Si fallan, la FSM escribe el resultado y el HTN replanifica.
- **El buffer es por conversación**. Un mensaje con `ConversationId` desconocido se descarta silenciosamente.
- **`MAX_CONVERSATIONS = 3`** por agente simultáneamente.
- **El dissolve no es un protocolo nuevo**: es un `Inform` broadcast que limpia los campos sociales. El HTN físico detecta `AssignedTask = null` y vuelve a `RoutineTask` solo.
- **`NoiseCoveredBy` puede expirar**: si el agente que se adjudicó el ruido no actualiza en un tiempo razonable (ej. `Time.time - LastNoisePositionTime > 15f`), se puede limpiar para permitir que otro vaya.
- **La cámara no tiene `WorldState` locomotor** pero puede participar como iniciadora en cualquier `ContractNetProtocol`.