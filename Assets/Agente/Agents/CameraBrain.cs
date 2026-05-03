using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Agents.Tools;

namespace AgenticPrison.Agents {

    // Agente reactivo de cámara de vigilancia.
    //
    // FSM de dos estados con tabla de comportamientos por estado:
    //
    //   Watching     → solo emite presencia, espera avistamiento
    //   Coordinating → drena cola de CFPs y procesa respuestas Contract Net
    //
    // Política de activación: igual que los guardias — solo actúa si detecta
    // al fugitivo en un sector DISTINTO al que el equipo ya conoce.
    public class CameraBrain : FIPAAgent, IVisionEvents {

        public override string AgentId => gameObject.name;

        static int cameraCounter = 1;

        private void Awake() {
            gameObject.name = "Camara" + cameraCounter;
            cameraCounter++;
        }

        // ── Estados ───────────────────────────────────────────────────────────────

        enum CameraState { Watching, Coordinating }

        CameraState state = CameraState.Watching;

        // Tabla de comportamientos: estado → acción ejecutada en cada Update
        readonly Dictionary<CameraState, Action> behaviors
            = new Dictionary<CameraState, Action>();

        // Solo necesitamos PendingCfps y FugitiveSectorId del WorldState
        readonly WorldState ws = new WorldState();

        protected override WorldState GetWorldState() => ws;

        // ── Ciclo de vida ─────────────────────────────────────────────────────────

        protected override void Start() {
            base.Start();
            ws.AgentName = AgentId;

            behaviors[CameraState.Watching]     = UpdateWatching;
            behaviors[CameraState.Coordinating] = UpdateCoordinating;
        }

        protected override void Update() {
            base.Update();
            behaviors[state]();
        }

        // ── Comportamientos por estado ────────────────────────────────────────────

        void UpdateWatching() {
            // El sensor de visión dispara los eventos; aquí no hay acción activa
        }

        void UpdateCoordinating() {
            // Drena la cola de CFPs (uno por frame) y procesa respuestas
            ProcessIncoming(ws);

            // Volver a Watching cuando todos los contratos estén lanzados y resueltos
            if (ws.PendingCfps.Count == 0 && !HasActiveCnpInitiator())
                Transition(CameraState.Watching);
        }

        // ── Transiciones ──────────────────────────────────────────────────────────

        void Transition(CameraState next) {
            FIPALogger.Log(AgentId, "fsm", Performative.Inform, $"{state} → {next}");
            state = next;
        }

        // ── IVisionEvents ─────────────────────────────────────────────────────────

        public void OnFugitiveSpotted(Vector3 position) {
            Debug.Log($"<color=red>{AgentId.ToUpper()}: FUGITIVO DETECTADO EN POSICIÓN {position}</color>");

            List<string> sectors = PrisonMap.Instance.GetFugitiveSectors(position);
            string sectorId = sectors != null && sectors.Count == 1 ? sectors[0] : "[UNK]";

            // Misma política que los guardias: ignorar si el sector ya es conocido o ambiguo
            if (sectorId == "[UNK]" || sectorId == ws.FugitiveSectorId) return;

            ws.FugitiveSectorId = sectorId;
            ws.PrisonerInCell   = false;
            ws.PendingCfps.Clear(); // descartar operación anterior si la hubiera

            // 1. Broadcast Inform para sincronizar a los guardias
            Broadcast(new ACLMessage {
                Performative = Performative.Inform,
                Sender       = AgentId,
                Content      = new FugitiveSightingContent(position, Time.time, sectorId, AgentId),
                SentAt       = Time.time
            });

            // 2. Generar plan y encolar todos los contratos (la cámara no se queda ninguno)
            PerimeterTool.TeamPlan plan =
                PerimeterTool.GenerateTeamPlan(sectorId, PrisonMap.Instance, AgentId);

            foreach (ContractTask task in plan.AllTasks)
                ws.PendingCfps.Enqueue(task);

            FIPALogger.Log(AgentId, "ops", Performative.Cfp,
                $"sector={sectorId} team={plan.TeamName} tasks={plan.AllTasks.Count}");

            Transition(CameraState.Coordinating);
        }

        public void OnFugitiveLost() {
            if (state != CameraState.Coordinating) return;
            ws.PendingCfps.Clear();
            Transition(CameraState.Watching);
        }

        public void OnFugitivePositionUpdated(Vector3 position) { }
        public void OnGuardSpotted(Vector3 guardPosition) { }

        // Escuchar Informs de guardias para mantener FugitiveSectorId sincronizado
        protected override void OnMessageReceived(ACLMessage msg, WorldState ws) {
            base.OnMessageReceived(msg, ws);
        }
    }
}
