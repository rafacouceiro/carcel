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

        // ── Estados ───────────────────────────────────────────────────────────────

        enum CameraState { Watching, Coordinating }

        CameraState _state = CameraState.Watching;

        // Tabla de comportamientos: estado → acción ejecutada en cada Update
        readonly Dictionary<CameraState, Action> _behaviors
            = new Dictionary<CameraState, Action>();

        // Solo necesitamos PendingCfps y FugitiveSectorId del WorldState
        readonly WorldState _ws = new WorldState();

        // ── Ciclo de vida ─────────────────────────────────────────────────────────

        protected override void Start() {
            base.Start();
            _ws.AgentName = AgentId;

            _behaviors[CameraState.Watching]     = UpdateWatching;
            _behaviors[CameraState.Coordinating] = UpdateCoordinating;
        }

        protected override void Update() {
            base.Update();
            VisionManager.EmitPresence(transform);
            _behaviors[_state]();
        }

        // ── Comportamientos por estado ────────────────────────────────────────────

        void UpdateWatching() {
            // El sensor de visión dispara los eventos; aquí no hay acción activa
        }

        void UpdateCoordinating() {
            // Drena la cola de CFPs (uno por frame) y procesa respuestas
            ProcessIncoming(_ws);

            // Volver a Watching cuando todos los contratos estén lanzados y resueltos
            if (_ws.PendingCfps.Count == 0 && !HasActiveCnpInitiator())
                Transition(CameraState.Watching);
        }

        // ── Transiciones ──────────────────────────────────────────────────────────

        void Transition(CameraState next) {
            FIPALogger.Log(AgentId, "fsm", Performative.Inform, $"{_state} → {next}");
            _state = next;
        }

        // ── IVisionEvents ─────────────────────────────────────────────────────────

        public void OnFugitiveSpotted(Vector3 position) {
            List<string> sectors = PrisonMap.Instance.GetFugitiveSectors(position);
            string sectorId = sectors != null && sectors.Count == 1 ? sectors[0] : "[UNK]";

            // Misma política que los guardias: ignorar si el sector ya es conocido o ambiguo
            if (sectorId == "[UNK]" || sectorId == _ws.FugitiveSectorId) return;

            _ws.FugitiveSectorId = sectorId;
            _ws.PrisonerInCell   = false;
            _ws.PendingCfps.Clear(); // descartar operación anterior si la hubiera

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
                _ws.PendingCfps.Enqueue(task);

            FIPALogger.Log(AgentId, "ops", Performative.Cfp,
                $"sector={sectorId} team={plan.TeamName} tasks={plan.AllTasks.Count}");

            Transition(CameraState.Coordinating);
        }

        public void OnFugitiveLost() {
            if (_state != CameraState.Coordinating) return;
            _ws.PendingCfps.Clear();
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
