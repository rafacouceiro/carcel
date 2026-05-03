# **Documentación Agentes**

## **1. WorldState**

El archivo `WorldState.cs` contiene el estado interno y del entorno, estructurando el conocimiento o la percepción individual que tiene el agente en cada momento.

### **Estado Interno**
- **`AgentName`**: Identificador único del agente que permite descartar ruidos propios u otras lógicas.
- **`CurrentPosition`**: Coordenada física actualizada del guardia (`transform.position`).
- **`Energy`**: Nivel de cansancio del guardia, que va de 0 a 100. Necesario para planificar momentos de descanso.

### **Memoria Visual**
- **`FugitiveInVision`**: Bandera booleana que dicta si el agente se encuentra estableciendo contacto visual directo con el presidiario.
- **`LastKnownPosition`**: Últimas coordenadas exactas donde el agente localizó visualmente al fugitivo. Se actualizan repetidamente durante la visión continua.
- **`LastKnownPositionTime`**: Instante de tiempo (`Time.time`) exacto en el que el guardia perdió o refrendó su visual del fugitivo de forma concreta.

### **Memoria sobre Otros Agentes**
- **`LastGuardPosition`**: Las posiciones recientes donde ha visto físicamente a algún compañero de patrulla. 
- **`LastGuardPositionTime`**: El respectivo momento de dicho avistamiento guardado por tiempo. Permite juzgar ruidos y evitar falsas alarmas.

### **Memoria Auditiva**
- **`LastNoisePosition`**: Información posicional sobre el foco desde el cual sonó el último ruido difuso que superó el umbral de captación.
- **`LastNoisePositionTime`**: Tiempo de la audición.

### **Estado del Entorno**
- **`PrisonerInCell`**: Bandera lógica crucial. Por defecto es cierta, y una vez desmentida (fuga confirmada) cambia de forma absoluta todo el comportamiento del agente.

### **Navegación**
- **`Map`**: Referencia general a las estructuras transitables y a los waypoints.
- **`AssignedQuadrantId`**: La región asignada en la que el agente realizará la patrulla general en rutinas.

### **El Mapa y sus Componentes**
El entorno navegable está representado jerárquicamente por tres componentes principales, accesibles a través de `PrisonMap.cs`:
- **`PrisonMap.cs`**: Clase *Singleton* que administra globalmente todas las secciones de la prisión. Almacena diccionarios con los cuadrantes y agrupa todos los nodos lógicos. Ofrece métodos utilitarios para saber en qué habitación exacta cae una coordenada o extraer listas de puntos clave y celdas.
- **`RoomNode.cs`**: Representa una habitación o espacio lógico delimitado mediante un *BoxCollider*. Contiene una lista de *waypoints* en su interior y establece conexiones lógicas bidireccionales con habitaciones adyacentes (`connectedRooms`), lo que resulta vital para los algoritmos de búsqueda grafales del agente.
- **`WayPointData.cs`**: Representan cada uno de los puntos fijos a los que el agente puede transitar. Se categorizan bajo marcas booleanas (`isKeyPoint` para puestos críticos de guardia, `isPatrolCheckpoint` para circuitos de ronda, `isCell` para definir si están dentro de celdas de aislamiento).

---

## **2. Sensores**

El sistema sensorial de los agentes permite adquirir la información proveniente del mundo y es procesada centralmente en el `Brain.cs`. Cuenta con una arquitectura de `Emisor - Manager - Receptor`, lo que significa que el entorno empuja la señal a un medio universal, un Gestor que empareja y delega las señales a los receptores físicos registrados.

### **Sistema Visual**
- **Emisores**: Todos los elementos observables publican rutinariamente su presencia. Esto abarca al jugador (`ControlJugador.cs`) como objetivo de captura o búsqueda, y al resto de guardias de la escena.
- **Procesamiento**: En cada ciclo y ante emisiones presentes, el `VisionSystem.cs` registrado en `VisionManager` hace cálculos de visibilidad física empleando raycasts y evaluando profundidad y cono de visión angular (`CheckPhysicalVisibility`).
- **Recepción**: Si el `VisionSystem.cs` detecta el estímulo exitosamente, envía los eventos como `OnFugitiveSpotted`, `OnFugitiveLost` o `OnGuardSpotted` según convenga al componente `Brain.cs`, el cual asume el impacto de la información. Eventualmente la huida puede constatarse viendo la alteración de una celda por proximidad (`ProximityButton.cs`).
- **Efectos en el Mundo**: En el WorldState un encuentro visual con la fuga significa establecer `FugitiveInVision = true`, marcar el `LastKnownPosition` y, además, invalidar por completo la bandera `PrisonerInCell = false`. El seguimiento constante provocará replanificaciones obligadas.

### **Sistema Auditivo**
- **Emisores**: El ruido es producido tanto dinámicamente según el movimiento del jugador o los otros agentes (`NoiseEvent`), como originado por elementos de la cárcel de forma estática (el abrir una celda como `CellDoorSlide.cs`).
- **Procesamiento**: La emisión llega al `NoiseManager`, el cual mide perimetralmente las métricas esféricas (alcance y posición) y advierte mediante llamadas a los métodos correspondientes en `Brain.cs` (`OnNoiseHeard`).
- **Recepción**: Al interior del cerebro, el sonido no detiene automáticamente el mundo. Si hay pistas visuales más actuales el ruido se minimiza, y el cerebro descarta también ruidos con origen proveniente de sí mismo o muy tenues cerca de la ubicación de los compañeros (`LastGuardPosition`). De ser superado este filtro, se introduce cierto margen de error circular e inexacto.
- **Efectos en el Mundo**: Graba difusamente un nuevo `LastNoisePosition` en los registros del agente y lanza un aviso en el cerebro para interrumpir la conducta actual (`ForzarReplanificacion`), deteniendo su transitar.

---

## **3. Planificación**

El agente elabora sus planes de comportamiento usando un sistema HTN (Hierarchical Task Network). Cuenta con una tarea raíz, tareas compuestas intermedias, tareas primitivas ejecutables y métodos que describen cómo ramificar las decisiones guiándose por el `WorldState`. 

### **Jerarquía HTN**

```

└── 🧠 **RootTask: BeGuard**
    ├── 📂 *Método: SelectEmergency*
    │   ├── 📝 **Precondiciones:** `state.FugitiveInVision`
    │   └── 📋 **Descomposición:** `EmergencyTask`
    ├── 📂 *Método: SelectInvestigation*
    │   ├── 📝 **Precondiciones:** `true` (Fallback)
    │   └── 📋 **Descomposición:** `InvestigationTask`
    └── 📂 *Método: SelectRoutine* (Default)
        ├── 📝 **Precondiciones:** `true` (Fallback)
        └── 📋 **Descomposición:** `RoutineTask`

└── 🧠 **CompoundTask: EmergencyTask**
    ├── 📂 *Método: CatchMethod*
    │   ├── 📝 **Precondiciones:** `state.FugitiveInVision && distance < 1.5f`
    │   └── 📋 **Descomposición:** `GameOverTask`
    └── 📂 *Método: ChaseMethod*
        ├── 📝 **Precondiciones:** `state.FugitiveInVision`
        └── 📋 **Descomposición:** `ChangeFlashLight -> ChaseTask`

└── 🧠 **CompoundTask: InvestigationTask**
    ├── 📂 *Método: SelectInvestigateEscape*
    │   ├── 📝 **Precondiciones:** `!state.PrisonerInCell && state.LastKnownPosition != Vector3.zero && age < 25f`
    │   └── 📋 **Descomposición:** `InvestigateEscapeTask`
    ├── 📂 *Método: InvestigateNoiseMethod*
    │   ├── 📝 **Precondiciones:** `state.LastNoisePosition != Vector3.zero && age < 10f`
    │   └── 📋 **Descomposición:** `ChangeFlashLight -> MoveTask (xN) -> ClearNoiseTask`
    └── 📂 *Método: InvestigateLocationMethod*
        ├── 📝 **Precondiciones:** `!state.PrisonerInCell`
        └── 📋 **Descomposición:** `ChangeFlashLight -> MoveTask (xN)`

└── 🧠 **CompoundTask: RoutineTask**
    ├── 📂 *Método: PatrolMethod*
    │   ├── 📝 **Precondiciones:** `state.PrisonerInCell`
    │   └── 📋 **Descomposición:** `ChangeFlashLight -> MoveTask (xN)`
    └── 📂 *Método: SelectEnergyRecovery*
        ├── 📝 **Precondiciones:** `true` (Fallback)
        └── 📋 **Descomposición:** `EnergyRecoveryTask`

└── 🧠 **CompoundTask: InvestigateEscapeTask**
    ├── 📂 *Método: PredictivePursuitMethod*
    │   ├── 📝 **Precondiciones:** `!state.PrisonerInCell && isFresh (< 2s)`
    │   └── 📋 **Descomposición:** `ChangeFlashLight -> MoveTask -> MoveTask (xN)`
    └── 📂 *Método: WideSweepMethod*
        ├── 📝 **Precondiciones:** `!state.PrisonerInCell && age < 35f`
        └── 📋 **Descomposición:** `ChangeFlashLight -> MoveTask (xN) -> ClearPositionTask`

└── 🧠 **CompoundTask: EnergyRecoveryTask**
    ├── 📂 *Método: GuardKeySpotMethod*
    │   ├── 📝 **Precondiciones:** `!state.FugitiveInVision`
    │   └── 📋 **Descomposición:** `ChangeFlashLight -> MoveTask -> LookAroundTask (xN)`
    └── 📂 *Método: TakeBreakMethod* (Default)
        ├── 📝 **Precondiciones:** `true` (Fallback)
        └── 📋 **Descomposición:** `ChangeFlashLight -> TakeBreathTask`

```

### **Algoritmia de los Métodos**
Los métodos actúan como resolutores lógicos que dictan cómo una Tarea Compuesta se desglosa en primitivas. A continuación, se detalla la algoritmia específica de los principales métodos implementados:

#### **Emergencia**
- **`CatchMethod`**: Realiza un cálculo de distancia euclídea simple (`Vector3.Distance`). Si el guardia está a menos de `1.5f` del objetivo, desencadena el estado de *GameOver*.
- **`ChaseMethod`**: Ejecuta una persecución predecible y directa que cede el destino de navegación hacia la coordenada más fresca del fugitivo.

#### **Investigación**
- **`InvestigateNoiseMethod`**: Aplica un algoritmo **Greedy** (vecino más cercano). Filtra hasta 3 `WayPointData` que sean `isKeyPoint` en un radio de 15 unidades respecto al origen del ruido. Luego, apoyándose en las físicas reales de ruta de Unity (`NavMesh.CalculatePath`), va encadenando y ordenando iterativamente aquellos puntos que ofrezcan menor coste de viaje calculado, para trazar un barrido óptimo y localizado.
- **`InvestigateLocationMethod`**: También emplea un enfoque **Greedy**. Extrae todos los `KeyPoints` del mapa y planifica un recorrido cíclico visitando el más cercano (calculando su coste con `NavMeshPath`) respecto a la posición actual proyectada tras cada visita, minimizando el tiempo muerto.

#### **Rutina y Patrullaje**
- **`PatrolMethod`**: Ejecuta una **Búsqueda en Profundidad (DFS)** a través del subgrafo compuesto de `RoomNode`. Partiendo de la sala inicial, apila (`Stack`) progresivamente las habitaciones vecinas (`connectedRooms`), limitando su alcance solo a las que pertenezcan en rigor al cuadrante asignado del agente (`AssignedQuadrantId`). Durante la topología recolecta las ubicaciones listadas como `isPatrolCheckpoint`.
- **`GuardKeySpotMethod`**: Evalúa todas las localizaciones `isKeyPoint` de la prisión cotejando su distancia topológica (`NavMesh.CalculatePath`) y elige la de coste mínimo. Se desplaza visualizando allí mientras emite comandos `LookAroundTask` adaptados a la proporción de oxígeno que el agente necesita reparar.

#### **Pérdidas de Fugitivo y Fuga**
- **`PredictivePursuitMethod`**: Escoge predecir la sala de huida tomando la habitación en la que el prisionero desapareció (`LastKnownPosition`) y derivando a uno de sus conectores (`connectedRooms`) vecinos aledaños al azar. Una vez decidido, extrae los `WayPointData` de dicha sub-sala y los ordena por simple distancia euclídea, dirigiéndose frenético a chocar con la huida.
- **`WideSweepMethod`**: Ejecuta una **Expansión Algorítmica de 1er Grado**. Reúne la sala del avistamiento, sus recintos colindantes directos y, a su vez, las colindantes de esas primeras logrando reclutar los nodos para componer un vecindario geográfico grande. Luego destila los `isPatrolCheckpoint` abarcados y teje una ruta conectiva usando heurística **Greedy**.

---

### **Tareas Primitivas (Acciones Finales)**
Estas son las tareas que ejecutan los actuadores y modifican el estado simulado en el planificador.

#### **1. `ChangeFlashLight`**
* **Precondiciones:** `true`.
* **Efectos:** Ninguno sobre el estado lógico (sólo efecto visual en color).

#### **2. `ChaseTask`**
* **Precondiciones:** `state.FugitiveInVision && state.Energy >= 5f`.
* **Efectos:** `state.Energy -= 5f`, `state.CurrentPosition = state.LastKnownPosition`.

#### **3. `ClearNoiseTask`**
* **Precondiciones:** `true`.
* **Efectos:** `state.LastNoisePosition = Vector3.zero`.

#### **4. `ClearPositionTask`**
* **Precondiciones:** `true`.
* **Efectos:** `state.LastKnownPosition = Vector3.zero`.

#### **5. `LookAroundTask`**
* **Precondiciones:** `true`.
* **Efectos:** `state.Energy = Mathf.Min(100f, state.Energy + 20f)`.

#### **6. `MoveTask`**
* **Precondiciones:** `_target != Vector3.zero && state.Energy >= CalculateEnergyCost`.
* **Efectos:** `state.CurrentPosition = _target`, `state.Energy -= CalculateEnergyCost`.

#### **7. `TakeAirTask` (TakeBreathTask)**
* **Precondiciones:** `true`.
* **Efectos:** `state.Energy = Mathf.Min(100f, state.Energy + 30f)`.

#### **8. `GameOverTask`**
* **Precondiciones:** `true`.
* **Efectos:** Funciona como captura final abortando la simulacion, final del juego.

### **Actuadores**
Los operadores o actuadores (`Actuators.cs`) son el eslabón final que procesa las decisiones lógicas y las vuelve acciones tangibles en el motor. Reciben órdenes directas de tareas primitivas (como `SetDestination`, `SetSpeed` o `SetLightColor`) y manejan el `NavMeshAgent` o utilidades como la `linterna` de Unity.

---

## **4. Orquestación**

La integración sistémica global está orquestada conjuntamente entre el `Brain.cs` y el `HTNPlanner.cs`. 

- **El Cerebro (`Brain.cs`)** actúa como el núcleo vital persistente del agente. Por un lado, mantiene los registros actualizados en memoria (`WorldState`) desde lo que dictan sus implementaciones en los terminales sensoriales (`IVisionEvents`, `INoiseReceiver`, `ICellEventReceiver`). Por el otro, actúa de director: pide constantemente un plan a seguir o lo aborta ante un estímulo que interrumpe (`ForzarReplanificacion()`) obligando así al recálculo en el siguiente `Update()`.
  
- **El Planificador (`HTNPlanner.cs`)** entra a escena bajo demanda central. Es un motor de resolución que clona un `WorldState` y explora un gran abanico recursivo ramificando métodos `Decompose()` y checkeando `CheckPreconditions()` simulando los `ApplyEffects()`. Si una sucesión de derivaciones conecta el estado inicial con el éxito completo, el planner expide y solidifica `Queue<IPrimitiveTask>`, para que a posteriori el cerebro lo devore tramo a tramo ejecutando `Execute()` que contactará internamente con los `Actuators.cs`.
