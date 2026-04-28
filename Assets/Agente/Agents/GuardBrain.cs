using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior;
using AgenticPrison.Behavior.RootTask;
using AgenticPrison.Communication;
using AgenticPrison.Behavior.Social;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols.ContractNet;
using AgenticPrison.Communication.Protocols.Query;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AgenticPrison.Agents {

    // Cerebro del agente: controla la percepción, el estado y el planificador HTN
    public class GuardBrain : FIPAAgent, INoiseReceiver, IVisionEvents, ICellEventReceiver {

        [Header("Referencias Tangibles")]
        public Transform PlayerTarget;

        [Header("Comunicación FIPA")]
        public Light Flashlight;

        // Identificador único del agente en el bus de mensajes
        public override string AgentId => gameObject.name;

        [Header("Configuración del Guardia")]
        [Tooltip("El ID simbólico del cuadrante. Puedes escribirlo o arrastrar el objeto abajo.")]
        public string QuadrantId = "section1";
        const float EnergyThreshold = 15f;

        #if UNITY_EDITOR
        [Header("Herramientas de Editor (No compila en build)")]
        public Transform ArrastrarCuadrante;
        private void OnValidate() {
            if (ArrastrarCuadrante != null) {
                QuadrantId = ArrastrarCuadrante.name; 
                ArrastrarCuadrante = null;            
            }
        }
        #endif

        [Header("Estado Lógico")]
        public WorldState CurrentState;

        [Header("Audición")]
        public float AuditoryRange = 20f;

        private HTNPlanner _planner;
        private Queue<IPrimitiveTask> _currentPlan;
        private IPrimitiveTask _activeTask;

        // Plano social Phase 2
        private HTNPlanner _socialPlanner;
        private Queue<IPrimitiveTask> _socialPlan;
        private IPrimitiveTask _activeSocialTask;
        private ICompoundTask _socialRootTask;

        private IActuators _actuators;
        private ICompoundTask _rootTask;

        [Tooltip("Nombre del agente, recogido automáticamente.")]
        public string AgentName;
        private static int _guardCounter = 1;

        private void Awake() {
            AgentName = "Patrulla" + _guardCounter;
            gameObject.name = AgentName;
            _guardCounter++; 
        }

        protected override void Start() {
            // Registrar en MessageBus con el nombre ya asignado por Awake()
            base.Start();

            // Inicializar estado del mundo y asignar al agente
            CurrentState = new WorldState();
            CurrentState.AgentName = AgentName;
            CurrentState.Map = PrisonMap.Instance;
            CurrentState.AssignedQuadrantId = QuadrantId;

            _planner      = new HTNPlanner();
            _currentPlan  = new Queue<IPrimitiveTask>();
            _actuators    = GetComponent<Actuators>();
            _rootTask     = new BeGuard();

            // Plano social Phase 2
            _socialPlanner  = new HTNPlanner();
            _socialPlan     = new Queue<IPrimitiveTask>();
            _socialRootTask = new BeSocial(this);
        }

        protected override void Update() {
            // Orden Phase 2: plano social antes que plano físico
            base.Update();                          // DiscardExpired del buffer de mensajes
            ProcessIncoming(CurrentState);          // enruta mensajes a protocolos o OnMessageReceived
            UpdateLocation();
            ProcessSocialHTNExecution();            // plano social (BeSocial)

            // Cuando el QueryInitiator ha terminado (ya no está activo), limpiar el flag
            // y replanificar el HTN físico para que decida si investigar el ruido o ignorarlo
            if (CurrentState.WaitingForNoiseQuery && !HasActiveQueryInitiator()) {
                CurrentState.WaitingForNoiseQuery = false;
                if (CurrentState.LastNoisePosition != UnityEngine.Vector3.zero) ForzarReplanificacion();
            }

            ProcessHTNExecution();                  // plano físico (BeGuard)
            VisionManager.EmitPresence(this.transform);
            CheckSweepCompletion();
        }

        public Vector3 GetPosition() => transform.position;

        private void OnEnable() => NoiseManager.RegisterReceiver(this);
        private void OnDisable() => NoiseManager.UnregisterReceiver(this);

        // ── Evaluación reactiva de CFPs ────────────────────────────────────────────
        protected override bool EvaluateCfp(ACLMessage cfp, WorldState ws, out float cost) {
            cost = 0f;
            var content = cfp.Content as CfpContent;
            if (content == null) return false;

            if (ws.AssignedTask != null || ws.ContractNetActive || ws.Energy < EnergyThreshold
                    || !string.IsNullOrEmpty(ws.TeamName) || HasActiveCnpParticipant())
                return false;

            Vector3 targetPos = content.Task.Target;

            if (content.Task.AssignedRole == AgentRole.Sweeper && content.Task.SweepRooms != null && content.Task.SweepRooms.Count > 0) {
                float minLinearDist = float.MaxValue;
                foreach (var room in content.Task.SweepRooms) {
                    Vector3 roomPos = room.GetNavigablePosition();
                    float d = Vector3.Distance(ws.CurrentPosition, roomPos);
                    if (d < minLinearDist) {
                        minLinearDist = d;
                        targetPos = roomPos;
                    }
                }
            }

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(ws.CurrentPosition, targetPos, NavMesh.AllAreas, path))
                return false;

            cost = CalculatePathLength(path);
            return true;
        }

        // EVENTOS DE AUDICIÓN
        public void OnNoiseHeard(NoiseEvent noise)
        {
            if (noise.emisor == AgentName) return; // Ignorar ruidos propios

            // Si el fugitivo está a la vista, ignorar el ruido
            if (CurrentState.FugitiveInVision) return;

            // Comprobar cercanía de otros guardias
            bool sawGuardRecently = CurrentState.LastGuardPosition != Vector3.zero && (Time.time - CurrentState.LastGuardPositionTime < 8f);
            if (sawGuardRecently) {
                float distToGuard = Vector3.Distance(noise.Position, CurrentState.LastGuardPosition);

                // Si está cerca o es ruido de pasos suaves, descartar
                if (distToGuard < 10f || noise.Volume < 18f) {
                    Debug.Log($"<color=cyan>[{AgentName}] Ignorando ruido cercano a un compañero. Falsa alarma.</color>");
                    return;
                }
            }

            // Margen de error al localizar sonido según la distancia
            float dist = Vector3.Distance(transform.position, noise.Position);
            float errorMagnitude = Mathf.Lerp(0.5f, 10f, dist / noise.Volume);
            Vector2 randomCircle = Random.insideUnitCircle * errorMagnitude;
            Vector3 diffusePosition = noise.Position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Vigencia de las pistas previas
            bool isLKPActive = CurrentState.LastKnownPosition != Vector3.zero && (Time.time - CurrentState.LastKnownPositionTime < 20f);
            bool isLNPActive = CurrentState.LastNoisePosition != Vector3.zero && (Time.time - CurrentState.LastNoisePositionTime < 30f);

            // Pista visual previa es prioridad absoluta
            if (isLKPActive) {
                CurrentState.LastNoisePosition = diffusePosition;
                CurrentState.LastNoisePositionTime = Time.time;
                return;
            }
            else if (isLNPActive) {
                // Replanificar solo ante un sonido más fuerte y alejado del origen anterior
                if (noise.Volume > 18f && Vector3.Distance(CurrentState.LastNoisePosition, diffusePosition) > 15f) {
                    CurrentState.LastNoisePosition = diffusePosition;
                    CurrentState.LastNoisePositionTime = Time.time;
                } else return;
            }
            // Sin rastro reciente, el agente registra el sonido para coordinación social
            else {
                CurrentState.LastNoisePosition = diffusePosition;
                CurrentState.LastNoisePositionTime = Time.time;
            }
        }

        // EVENTOS DE VISIÓN
        public void OnGuardSpotted(Vector3 guardPosition)
        {
            // Registrar ubicación de compañeros detectados
            CurrentState.LastGuardPosition = guardPosition;
            CurrentState.LastGuardPositionTime = Time.time;
        }

        public void OnFugitiveSpotted(Vector3 position) {
            Debug.LogWarning($"<color=magenta>{CurrentState.PrisonerInCell} prisioner in cell</color>");

            // Determinar si lo avistado rompe la condición de cautiverio
            if(CurrentState.PrisonerInCell) {
                List<WayPointData> cellPoints = CurrentState.Map.GetAllCellPoints();
                bool isInsideAnyCell = false;
                foreach(WayPointData cellPoint in cellPoints) {
                    BoxCollider cellBox = cellPoint.GetComponent<BoxCollider>();
                    if(cellBox != null && cellBox.bounds.Contains(position)) {
                        isInsideAnyCell = true;
                        break;
                    }
                }

                // Si efectivamente sigue en la celda según los colliders, ignorar
                if(isInsideAnyCell) {
                    Debug.LogWarning("<color=magenta>El prisionero está dentro de la celda.</color>");
                    return;
                }
            }

            // Fuga confirmada visualmente
            Debug.LogWarning("<color=red>He visto al prisionero fuera de la celda</color>");
            CheckAndBroadcastSector(position);
            CurrentState.PrisonerInCell = false;
            CurrentState.FugitiveInVision = true;
            CurrentState.seenByMe = true;
            CurrentState.LastKnownPosition = position;
            CurrentState.LastKnownPositionTime = Time.time;
            ForzarReplanificacion();
            ForzarReplanificacionSocial(); // Phase 2: activa coordinación de fuga
        }

        public void OnFugitivePositionUpdated(Vector3 position) {
            if (CurrentState.PrisonerInCell) return; // Ignorar actualizaciones si no hay fuga
            CheckAndBroadcastSector(position);
            CurrentState.LastKnownPosition = position;
            CurrentState.LastKnownPositionTime = Time.time;
        }

        public void OnFugitiveLost() {
            Debug.LogWarning("<color=red>He perdido de vista al prisionero</color>");
            CurrentState.FugitiveInVision = false;
        }

        public void OnCellFoundOpen()
        {
            // Constatación de la huida al patrullar las celdas
            if (CurrentState.PrisonerInCell) {
                CurrentState.PrisonerInCell = false;
                Debug.LogWarning("<color=yellow>El prisionero SE HA FUGADO</color>");
                ForzarReplanificacion();
            }
        }

        // ── COMUNICACIÓN FIPA ──────────────────────────────────────────────────────

        protected override void OnMessageReceived(ACLMessage msg, WorldState ws) {
            base.OnMessageReceived(msg, ws);
            if (msg.Performative == Performative.AcceptProposal) HandleAcceptProposal(msg, ws);
            else if (msg.Channel != null && msg.Channel.StartsWith("team_")) HandleTeamSincronization(msg, ws);
        }

        protected override void HandleInform(ACLMessage msg, WorldState ws) {
            base.HandleInform(msg, ws);
            if (msg.Content is FugitiveSightingContent && !ws.FugitiveInVision) ForzarReplanificacion();
        }

        protected override void OnCfpReceived(ACLMessage msg, WorldState ws, ContractNetParticipant participant) {
            if (!string.IsNullOrEmpty(ws.TeamName) && !ws.FugitiveInVision) {
                if (ws.ContractNetActive && ws.AssignedTask != null) {
                    DissolveTeam(ws, msg.ConversationId);
                }
            }
            ws.PrisonerInCell = false;
            float cost;
            if (EvaluateCfp(msg, ws, out cost)) participant.SendPropose(this, ws, cost);
            else participant.SendRefuse(this, ws);
            if (!ws.FugitiveInVision) ForzarReplanificacion();
        }

        private void HandleAcceptProposal(ACLMessage msg, WorldState ws) {
            // El protocolo solo cerró; aquí aplicamos los efectos contractuales en WorldState
            ContractTask won = msg.Content as ContractTask;
            if (won == null) return;

            ws.AssignedTask        = won;
            ws.TeamName            = won.TeamName;
            ws.PendingSweepersCount = won.TotalSweepers;
            ws.ContractNetActive   = true;

            Debug.Log($"<color=cyan>[{AgentName}] Tarea asignada: {won.Type} | equipo: {won.TeamName}</color>");
            SubscribeToChannel(AgentId, "team_" + won.TeamName);

            if (!ws.FugitiveInVision) ForzarReplanificacion();
        }

        private void HandleTeamSincronization(ACLMessage msg, WorldState ws) {
            if (msg.Performative != Performative.InformDone) return;
            if (msg.Sender == AgentId) return;
            if (ws.PendingSweepersCount > 0) {
                ws.PendingSweepersCount--;
                if (ws.PendingSweepersCount <= 0) DissolveTeam(ws, msg.ConversationId);
            }
        }

        private void DissolveTeam(WorldState ws, string conversationId) {
            string teamName = ws.TeamName;
            if (string.IsNullOrEmpty(teamName)) return;
            ws.TeamName           = string.Empty;
            ws.ContractNetActive   = false;
            ws.AssignedRole        = AgentRole.None;
            ws.AssignedTask        = null;
            ws.PendingSweepersCount = 0;
            UnsubscribeFromChannel(AgentId, "team_" + teamName);
            ForzarReplanificacion();
        }

        private void CheckAndBroadcastSector(Vector3 position) {
            string newSectorId = CurrentState.Map.GetCurrentSector(position);
            if (!string.IsNullOrEmpty(newSectorId) && newSectorId != CurrentState.FugitiveSectorId) {
                Broadcast(new ACLMessage {
                    Performative   = Performative.Inform,
                    Sender         = AgentId,
                    Content        = new FugitiveSightingContent(position, Time.time, newSectorId, AgentId),
                    SentAt         = Time.time
                });
                CurrentState.FugitiveSectorId = newSectorId;
            }
        }

        private void CheckSweepCompletion() {
            if (CurrentState.AssignedTask == null || CurrentState.AssignedTask.Type != TaskType.SweepSector) return;
            if (CurrentState.AssignedTask.SweepRooms == null || CurrentState.AssignedTask.SweepRooms.Count > 0) return;

            string teamName = CurrentState.TeamName;
            if (!string.IsNullOrEmpty(teamName)) {
                if (CurrentState.PendingSweepersCount > 0) CurrentState.PendingSweepersCount--;
                BroadcastToChannel("team_" + teamName, new ACLMessage {
                    Performative   = Performative.InformDone,
                    Sender         = AgentId,
                    Content        = CurrentState.AssignedRole.ToString(),
                    SentAt         = Time.time
                });
                if (CurrentState.PendingSweepersCount <= 0) DissolveTeam(CurrentState, "sweep_done");
            }
            CurrentState.AssignedTask = null;
            ForzarReplanificacion();
        }

        // Refrescar coordenadas del agente en su estado interno
        private void UpdateLocation() => CurrentState.CurrentPosition = transform.position;

        // Interrumpir plan físico y detener movimiento actual
        private void ForzarReplanificacion() {
            _currentPlan.Clear();
            _activeTask = null;
            _actuators.StopMoving();
        }

        // Interrumpir plan social para replanificar en el siguiente frame
        private void ForzarReplanificacionSocial() {
            _socialPlan.Clear();
            _activeSocialTask = null;
        }

        // Motor de ejecución del HTN social — mismo patrón que el físico, sin actuadores
        private void ProcessSocialHTNExecution() {
            if (_socialRootTask == null) return;

            if (_socialPlan.Count == 0 && _activeSocialTask == null) {
                _socialPlan = _socialPlanner.GeneratePlan(CurrentState, _socialRootTask);
                if (_socialPlan.Count > 0) _activeSocialTask = _socialPlan.Dequeue();
            }

            if (_activeSocialTask != null) {
                // Las tareas sociales no usan actuadores físicos — se pasa null
                var status = _activeSocialTask.Execute(null, CurrentState);
                if (status == TaskExecutionStatus.Success) _activeSocialTask = (_socialPlan.Count > 0) ? _socialPlan.Dequeue() : null;
                else if (status == TaskExecutionStatus.Failure) { _socialPlan.Clear(); _activeSocialTask = null; }
            }
        }

        // Motor de ejecución continua HTN
        private void ProcessHTNExecution() {
            if (_rootTask == null) return;

            // Solicitar nuevo plan si la cola está vacía
            if (_currentPlan.Count == 0 && _activeTask == null) {
                _currentPlan = _planner.GeneratePlan(CurrentState, _rootTask);
                if (_currentPlan.Count > 0) _activeTask = _currentPlan.Dequeue();
            }

            // Seguimiento de la tarea primitiva actual
            if (_activeTask != null) {
                var status = _activeTask.Execute(_actuators, CurrentState);

                // Extraer próxima si logró éxito
                if (status == TaskExecutionStatus.Success) {
                    _activeTask = (_currentPlan.Count > 0) ? _currentPlan.Dequeue() : null;
                }
                // Vaciado ante fallo para replanificar en el siguiente frame
                else if (status == TaskExecutionStatus.Failure) {
                    _currentPlan.Clear();
                    _activeTask = null;
                }
            }
        }

        private float CalculatePathLength(NavMeshPath path) {
            float length = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++) length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            return length;
        }
    }
}