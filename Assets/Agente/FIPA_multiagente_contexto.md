# Sistema de Comunicación Multiagente FIPA
> Contexto técnico para Claude Code — Fase 2 del proyecto

---

## Premisa de diseño fundamental

El sistema de comunicación **debe funcionar igual para cualquier tipo de agente, independientemente de su arquitectura interna de planificación**. Un guardia con HTN, un dron con planificación reactiva y una cámara fija sin planner de acción deben participar en el mismo protocolo FIPA de forma idéntica desde el exterior.

> Los agentes distintos al guardia (dron, cámara, híbrido) son **ideas preliminares** que ilustran cómo la arquitectura soporta diferentes planificadores. Lo que sí es un requisito firme del sistema es que **puede haber agentes con arquitecturas muy diferentes**, y que **la capa de comunicación debe funcionar de igual manera para todos ellos**, sin importar qué haya debajo.

---

## 1. Contexto: qué existía en la fase anterior

El guardia de la fase 1 tiene:

- **`WorldState`**: representación mental completa del mundo físico. Incluye memoria visual (`FugitiveInVision`, `LastKnownPosition`, `LastKnownPositionTime`), auditiva (`LastNoisePosition`), sobre otros agentes (`LastGuardPosition`), estado físico (`Energy`, `CurrentPosition`) y del entorno (`PrisonerInCell`, `Map`, `AssignedQuadrantId`).
- **`HTNPlanner`**: toma el `WorldState` y genera un plan. La raíz `BeGuard` elige entre `EmergencyTask`, `InvestigationTask` y `RoutineTask` según precondiciones. Las tareas primitivas modifican el `WorldState` simulado mediante `ApplyEffects`.

> **El HTN existente no se modifica estructuralmente.** Solo se añade una comprobación de prioridad máxima al inicio de `BeGuard` para tareas asignadas externamente. Todo lo demás permanece intacto.

---

## 2. Arquitectura general: los dos planos de razonamiento

Cada agente opera en dos planos independientes que no se conocen entre sí, comunicándose únicamente a través de interfaces bien definidas.

### Plano de acción física
- **Responsable**: planificador interno del agente (HTN, reactivo, híbrido, nulo — lo que sea)
- **Lee y escribe**: `WorldState`
- **Output**: acciones físicas ejecutadas por `Actuators.cs`
- **Conocimiento de comunicación**: ninguno — no sabe que existe el `CommPlanner`

### Plano de comunicación social
- **Responsable**: `CommPlanner` BDI — **idéntico para todos los tipos de agente**
- **Lee**: `WorldState` (solo lectura) + `SocialState` (lectura/escritura)
- **Output**: actos comunicativos FIPA ejecutados por `FIPAAgent.Send/Broadcast`
- **Conocimiento del planner físico**: ninguno — solo habla con `IActionBridge`

Ambos planos ejecutan en el mismo `Update()` cada frame. El guardia puede estar persiguiendo al fugitivo (plano físico) mientras negocia la cobertura de salidas con otros agentes (plano social) sin que ninguno interfiera en la lógica del otro.

---

## 3. Estado del agente: WorldState y SocialState

### WorldState (existente, modificación mínima)
Sin cambios estructurales. Solo se añaden tres campos:

```csharp
TaskDescriptor AssignedTask;   // tarea asignada por comunicación (null si ninguna)
string ActiveContractId;       // id de la conversación que originó la tarea
bool dirty;                    // flag para forzar replanificación
```

### SocialState (nuevo)

```csharp
public class SocialState {
    // Perfiles de agentes conocidos: rol, capacidades declaradas
    public Dictionary<string, AgentProfile> KnownAgents;

    // Capacidades inferidas de propuestas y tareas completadas
    public Dictionary<string, HashSet<TaskType>> ObservedCapabilities;

    // Fiabilidad histórica: ratio completadas/aceptadas [0.0-1.0]
    // Valor inicial 0.5 para agentes desconocidos
    public Dictionary<string, float> ReliabilityScore;

    // Última posición conocida con timestamp (se decae con el tiempo)
    public Dictionary<string, (Vector3, float)> LastKnownPositions;

    // Contratos activos: quién está ejecutando qué ahora mismo
    public List<KnownContract> ActiveContracts;

    // Conversaciones en curso como iniciador o participante
    public List<ConversationRecord> ActiveConversations;
}
```

**Regla de acceso estricta:**
- `HTNPlanner` → lee y escribe `WorldState`
- `CommPlanner BDI` → lee `SocialState` + `WorldState` (solo lectura), escribe `SocialState`
- El único punto de cruce es `IActionBridge`

---

## 4. CommPlanner: arquitectura BDI

BDI se aplica **exclusivamente a la capa de comunicación**. No tiene sentido ponerlo encima del HTN para el comportamiento físico porque el HTN ya es un razonador deliberativo completo: el `WorldState` ya son los Beliefs físicos, las precondiciones HTN son los Desires físicos, y el plan activo son las Intentions físicas.

BDI llena el hueco que el HTN no cubre: conversaciones, compromisos sociales, negociaciones y conocimiento de otros agentes.

### Ciclo BDI — cuatro funciones por frame, en orden determinista

#### 4.1 Belief Revision Function
Actualiza el `SocialState` cuando llega un mensaje. Determinista, sin efectos secundarios más allá del `SocialState`:

```csharp
void ReviseBeliefs(ACLMessage msg) {
    _social.UpdatePosition(msg.Sender, msg.SenderPosition, Time.time);

    switch (msg.Performative) {
        case Propose:
            _social.RecordCapability(msg.Sender, msg.Ontology);
            _social.RecordProposal(msg.ConversationId, msg);
            break;
        case InformDone:
            _social.UpdateReliability(msg.Sender, success: true);
            _social.CloseContract(msg.ConversationId);
            break;
        case Failure:
            _social.UpdateReliability(msg.Sender, success: false);
            _social.CloseContract(msg.ConversationId);
            break;
        case AcceptProposal:
            _social.RegisterActiveContract(msg.ConversationId, msg);
            break;
    }
}
```

#### 4.2 Option Generation Function
Genera `Desires` a partir de los `Beliefs` actuales. Aquí reside la inteligencia comunicativa. Añadir un nuevo tipo de comportamiento comunicativo requiere únicamente añadir una regla aquí:

```csharp
List<Desire> GenerateOptions() {
    var desires = new List<Desire>();

    // Posición crítica sin cubrir
    foreach (var pos in GetUncoveredCriticalPositions()) {
        desires.Add(new CoordinationDesire {
            Position = pos.Location, Urgency = pos.DangerLevel,
            TaskType = TaskType.CoverPosition
        });
    }

    // Contrato completado — debo informar (prioridad máxima)
    foreach (var c in _social.CompletedContracts) {
        desires.Add(new CommitmentDesire { ContractId = c.Id, Urgency = 1.0f });
    }

    // Recibí un cfp y tengo slot libre
    foreach (var cfp in _social.PendingCfps) {
        desires.Add(new ResponseDesire { CfpMessage = cfp, Urgency = cfp.Urgency });
    }

    // Necesito información sobre una zona
    if (NeedsZoneInformation(out var zone)) {
        desires.Add(new InformationDesire { Zone = zone, Urgency = 0.4f });
    }

    return desires;
}
```

#### 4.3 Filter Function
Convierte `Desires` en `Intentions` resolviendo conflictos de recursos (máximo de conversaciones simultáneas, capacidades, redundancias):

```csharp
List<Intention> Filter(List<Desire> desires) {
    var intentions = new List<Intention>();
    int slots = MAX_CONVERSATIONS - _social.ActiveConversations.Count;

    foreach (var d in desires.OrderByDescending(x => x.Urgency)) {
        if (slots <= 0) break;

        // no abrir subasta si ya hay una activa para el mismo goal
        if (d is CoordinationDesire cd &&
            _social.HasActiveConversationFor(cd.Position, cd.TaskType))
            continue;

        // no proponer si no tenemos la capacidad física
        if (d is ResponseDesire rd &&
            !ActionBridge.QueryCapability(rd.CfpMessage.TaskType))
            continue;

        // no informar si la conversación ya está cerrada
        if (d is CommitmentDesire cmd &&
            !_social.HasActiveContract(cmd.ContractId))
            continue;

        intentions.Add(ToIntention(d));
        slots--;
    }

    return intentions;
}
```

#### 4.4 Execute Function
Ejecuta los actos comunicativos. Máximo 2-3 por frame:

```csharp
void Execute(List<Intention> intentions) {
    int executed = 0;
    foreach (var intention in intentions) {
        if (executed >= MAX_ACTS_PER_FRAME) break;

        switch (intention) {
            case OpenCfpIntention i:
                var cfp = BuildCfp(i.Goal);
                _social.RegisterOutstandingCfp(cfp.ConversationId, cfp);
                Broadcast(cfp, filter: i.Goal.RequiredOntology);
                executed++; break;

            case RespondIntention i:
                var proposal = BuildProposal(i.CfpMessage);
                Reply(i.CfpMessage, Performative.Propose, proposal);
                executed++; break;

            case InformIntention i:
                Reply(i.OriginalAccept, Performative.InformDone, null);
                _social.CloseContract(i.ContractId);
                executed++; break;
        }
    }
}
```

---

## 5. IActionBridge — el punto de integración agnóstico

`IActionBridge` es la **única interfaz entre el `CommPlanner` y el planificador físico**. Es lo que hace posible que la comunicación sea agnóstica al planner: el `CommPlanner` nunca llama directamente al HTN, a una tabla reactiva, ni a ningún sistema específico.

```csharp
public interface IActionBridge {
    // ¿Puede este agente ejecutar este tipo de tarea?
    // Se llama en Filter() antes de formar una RespondIntention
    bool QueryCapability(TaskType taskType);

    // Asigna una tarea al agente
    // Se llama cuando la FSM entra en EXECUTING (ganó la subasta)
    // Devuelve false si el agente no puede aceptar en este momento
    bool AssignTask(TaskDescriptor task);

    // Cancela la tarea actualmente en ejecución
    void CancelTask(string taskId);

    // El agente terminó la tarea — el CommPlanner envía InformDone automáticamente
    event Action<string> OnTaskCompleted;

    // El agente falló — el CommPlanner envía Failure automáticamente
    event Action<string, string> OnTaskFailed;
}
```

### TaskDescriptor — vocabulario común

El `CommPlanner` solo habla en términos de intenciones abstractas. Nunca dice "ejecuta `ChaseMethod`" ni "pon la flag `_priorityTask`". Solo expresa qué quiere que ocurra en el mundo:

```csharp
public class TaskDescriptor {
    public TaskType Type;            // qué tipo de tarea
    public Vector3 TargetPosition;   // dónde (si aplica)
    public string TargetAgentId;     // sobre quién (si aplica)
    public float Urgency;            // 0.0-1.0
    public float Deadline;           // Time.time de expiración
    public string ContractId;        // conversación que lo originó
}

public enum TaskType {
    GoToPosition, CoverPosition, InvestigateZone,
    ChaseTarget, MonitorZone, PatrolArea, TakeBreak
}
```

---

## 6. Implementaciones de IActionBridge (ejemplos por arquitectura)

> Estas implementaciones ilustran cómo distintas arquitecturas se integran a través del mismo contrato. Cada agente nuevo que se añada al proyecto necesita únicamente implementar `IActionBridge` — el resto del sistema no cambia.

### HTNActionBridge (guardia)
Traduce el `TaskDescriptor` al lenguaje del HTN: escribe en `WorldState` y fuerza replanificación.

```csharp
public class HTNActionBridge : IActionBridge {
    private WorldState _ws;
    private Brain _brain;

    public bool QueryCapability(TaskType t) {
        if (_ws.Energy < 15f) return false;
        return t != TaskType.MonitorZone; // MonitorZone es solo para agentes fijos
    }

    public bool AssignTask(TaskDescriptor task) {
        _ws.AssignedTask = task;
        _ws.ActiveContractId = task.ContractId;
        _brain.ForzarReplanificacion();
        return true;
    }

    public void CancelTask(string id) {
        if (_ws.ActiveContractId == id) {
            _ws.AssignedTask = null;
            _ws.ActiveContractId = null;
            _brain.ForzarReplanificacion();
        }
    }
}
```

El HTN en su próximo tick detecta `AssignedTask` con prioridad máxima en `BeGuard`:

```csharp
// Única modificación al HTN existente:
if (state.AssignedTask != null) {
    return DecomposeAssignedTask(state.AssignedTask);
    // tiene prioridad sobre Emergency, Investigation y Routine
}
```

### ReactiveActionBridge (ejemplo: agente con planner reactivo)
Inserta la tarea como regla de prioridad máxima en la tabla reactiva:

```csharp
public class ReactiveActionBridge : IActionBridge {
    private DroneReactivePlanner _planner;

    public bool QueryCapability(TaskType t) {
        if (_planner.Battery < 0.15f) return false;
        return t != TaskType.MonitorZone;
    }

    public bool AssignTask(TaskDescriptor task) {
        _planner.SetPriorityTask(new ReactiveRule {
            Priority = 0,
            Condition = () => true,
            Action = () => ExecuteTaskType(task),
            TaskId = task.ContractId
        });
        return true;
    }

    private void ExecuteTaskType(TaskDescriptor task) {
        switch (task.Type) {
            case InvestigateZone:
            case CoverPosition:
            case GoToPosition:
                _planner.SetDestination(task.TargetPosition);
                if (_planner.ReachedDestination()) {
                    _planner.ClearPriorityTask();
                    OnTaskCompleted?.Invoke(task.ContractId);
                }
                break;
        }
    }
}
```

### NullActionBridge (ejemplo: agente sin planner de movimiento)
Valida que la arquitectura funciona incluso con agentes que no se mueven:

```csharp
public class NullActionBridge : IActionBridge {
    private CameraSensor _sensor;

    public bool QueryCapability(TaskType t) {
        if (t == TaskType.MonitorZone)
            return _sensor.HasVisibilityOver(_pendingTaskPosition);
        return false; // nunca puede hacer tareas locomotoras
    }

    public bool AssignTask(TaskDescriptor task) {
        if (task.Type != TaskType.MonitorZone) return false;
        if (!_sensor.HasVisibilityOver(task.TargetPosition)) return false;

        _sensor.ActivateMonitoringMode(task.TargetPosition, task.Deadline);
        _sensor.OnMonitoringComplete += () => OnTaskCompleted?.Invoke(task.ContractId);
        return true;
    }
}
```

### Tabla comparativa de comportamiento por bridge

| TaskType recibido | HTNActionBridge | ReactiveActionBridge | NullActionBridge |
|---|---|---|---|
| `CoverPosition` | `WorldState.AssignedTask = td; ForzarReplan()` | `SetPriorityTask(prioridad 0)` | `return false` |
| `InvestigateZone` | `WorldState.AssignedTask = td; ForzarReplan()` | `SetPriorityTask(prioridad 0)` | `return false` |
| `ChaseTarget` | `WorldState.AssignedTask = td; ForzarReplan()` | `SetPriorityTask(prioridad 0)` | `return false` |
| `MonitorZone` | `return false` (no es su rol) | `return false` (se mueve) | `ActivateMonitoringMode()` si tiene visión |
| `TakeBreak` | `WorldState.AssignedTask = td; ForzarReplan()` | `SetLowPriorityTask(prioridad 5)` | N/A |

---

## 7. Capas del sistema — vista estructural

| Capa | Componente | Responsabilidad | Acceso permitido |
|---|---|---|---|
| 0 — mundo | Unity / NavMesh / Physics | Simulación física | solo escrito por Actuators |
| 1 — sensores | VisionSystem, NoiseManager | Traducir mundo a eventos | escribe WorldState |
| 2 — estado físico | `WorldState.cs` | Representación mental del mundo físico | escrito por sensores y planner físico |
| 3 — estado social | `SocialState` | Representación del mundo social | escrito solo por CommPlanner |
| 4 — planif. física | Planner interno (HTN, reactivo, etc.) | Genera secuencias de acciones físicas | lee y escribe WorldState |
| 5 — planif. social | `CommPlanner BDI` | Genera actos comunicativos | lee ambos estados, escribe SocialState |
| 6 — puente | `IActionBridge` | Único canal capa 5 → capa 4 | llamado por CommPlanner, implementado por cada agente |
| 7 — transporte | `MessageBus` | Enrutado de mensajes FIPA | usado por CommPlanner para enviar |

---

## 8. Ciclo completo por frame — orden de operaciones

| Orden | Capa | Operación | Lee | Escribe |
|---|---|---|---|---|
| 1 | Sensores físicos | `VisionSystem.CheckPhysicalVisibility()` | mundo Unity | WorldState |
| 2 | Sensores físicos | `NoiseManager.OnNoiseHeard()` | mundo Unity | WorldState |
| 3 | CommPlanner BDI | `DiscardExpired()` | `_messageQueue` | `_messageQueue` |
| 4 | CommPlanner BDI | `ReviseBeliefs()` x2-3 msgs | `_messageQueue` | SocialState |
| 5 | CommPlanner BDI | `GenerateOptions()` | WorldState (RO) + SocialState | lista Desires (local) |
| 6 | CommPlanner BDI | `Filter()` | Desires + SocialState | lista Intentions (local) |
| 7 | CommPlanner BDI | `Execute()` — actos comunicativos | Intentions | MessageBus |
| 8 | Planner físico | Replanificación si dirty | WorldState | plan activo |
| 9 | Planner físico | Ejecutar siguiente tarea | plan activo | WorldState |
| 10 | Actuators | `SetDestination / SetSpeed / SetLight` | plan activo | mundo Unity (NavMesh) |

> Los pasos 3-7 (CommPlanner) y 8-10 (planner físico + Actuators) son independientes. El único punto de cruce posible es `IActionBridge`, que ocurre en el paso 7 cuando una Intention de tipo `AcceptTask` llama a `ActionBridge.AssignTask()`.

---

## 9. MessageBus — transporte de mensajes FIPA

### Suscripción por ontología
Cada agente declara al registrarse qué tipos de tarea le son relevantes. El bus entrega un `cfp` únicamente a los agentes suscritos a esa ontología, sin que el emisor conozca quiénes son:

```csharp
// Al inicializarse cada agente:
MessageBus.Register(this, new[]{'cover-position', 'chase-task', 'investigate-zone'});

// Broadcast de un cfp con ontología 'cover-position':
// → entrega solo a suscriptores, nunca a agentes no suscritos
// → O(k) donde k es el número de suscriptores a esa ontología
```

El broadcast es solo el disparo de apertura de la negociación. Todos los mensajes posteriores de la misma conversación (`propose`, `accept`, `reject`, `inform-done`, `failure`) son **unicast directo** por `conversationId`.

### Buffer y política de descarte
Cada `FIPAAgent` tiene un buffer circular de N=16 mensajes. Cuando el buffer está lleno:

| Prioridad del mensaje nuevo | Se descarta |
|---|---|
| Alta (`AcceptProposal`, `Cancel`) | el mensaje de prioridad Baja más antiguo |
| Media (`Propose`, `Refuse`, `InformDone`) | el mensaje de prioridad Baja más antiguo |
| Baja (notificaciones tardías) | el mensaje nuevo |

El límite de 2-3 mensajes procesados por frame es independiente del tamaño del buffer. Los mensajes no procesados esperan en la cola y son válidos mientras no hayan expirado por `replyBy`.

---

## 10. Flujo de negociación Contract Net — ejemplo completo

Ejemplo entre un agente iniciador y un agente participante con arquitectura diferente:

| Frame | Agente | Capa | Operación |
|---|---|---|---|
| N | Iniciador | Sensores | Detecta evento en posición P |
| N | Iniciador | CommPlanner — GenerateOptions | genera `Desire: InvestigatePosition(P, urgency=0.7)` |
| N | Iniciador | CommPlanner — Filter | forma `OpenCfpIntention(InvestigateZone, P)` |
| N | Iniciador | CommPlanner — Execute | construye `ACLMessage{Cfp, ontology:'investigate-zone'}` y llama `Broadcast()` |
| N | MessageBus | Transporte | entrega cfp a todos suscritos a `'investigate-zone'` |
| N+1 | Participante | CommPlanner — ReviseBeliefs | procesa cfp, actualiza `SocialState.PendingCfps` |
| N+1 | Participante | CommPlanner — Filter | `QueryCapability(Investigate)=true` → forma `RespondIntention` |
| N+1 | Participante | CommPlanner — Execute | calcula coste con `BidStrategy` → envía `Propose` |
| N+2 | Iniciador | CommPlanner — ReviseBeliefs | registra propuestas recibidas |
| N+3 | Iniciador | CommPlanner — TickConversations | deadline expirado → evalúa propuestas → selecciona ganador |
| N+3 | Iniciador | CommPlanner — Execute | envía `AcceptProposal` al ganador + `RejectProposal` al resto |
| N+4 | Ganador | CommPlanner — ReviseBeliefs | procesa `AcceptProposal` → FSM → EXECUTING |
| N+4 | Ganador | IActionBridge | `AssignTask(TaskDescriptor{...})` → planner interno |
| N+4 | Rechazado | CommPlanner — ReviseBeliefs | procesa `RejectProposal` → FSM → REJECTED → vuelve al plan anterior |
| N+K | Ganador | ActionBridge | `NotifyComplete()` → dispara `OnTaskCompleted` |
| N+K | Ganador | CommPlanner — Execute | envía `InformDone` al iniciador |
| N+K | Iniciador | CommPlanner — ReviseBeliefs | `UpdateReliability(ganador, success=true)` → FSM → DONE |

> El iniciador nunca supo qué arquitectura de planner tiene el participante. El participante nunca supo de dónde venía la orden. El protocolo FIPA es idéntico desde cualquier perspectiva.
