using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Agents.Tools;

namespace AgenticPrison.Agents.Camera {

    // Capa de coordinación de la cámara de vigilancia.
    //
    // FSM de dos estados con responsabilidad exclusiva sobre los contratos CNP:
    //
    //   Watching     → espera avistamiento de CameraBrain
    //   Coordinating → drena PendingCfps, orquesta las subastas Contract Net
    //
    // Separada de CameraBrain (capa física/visión) por diseño: perder el contacto
    // visual no tiene ninguna relación con lanzar o no un CNP.
    public class CameraFSM : FIPAAgent {

        public override string AgentId => gameObject.name;

        // ── Estados ────────────────────────���─────────────────────────────────��────

        enum FsmState { Watching, Coordinating }

        FsmState _state = FsmState.Watching;

        readonly CameraWorldState _ws = new CameraWorldState();

        protected override WorldState GetAgentState() => _ws;

        // ── Ciclo de vida ─────────────────────────────────────────────────────────

        protected override void Start() {
            base.Start();
            _ws.AgentName = AgentId;
        }

        protected override void Update() {
            base.Update();
            if (_state == FsmState.Coordinating) UpdateCoordinating();
        }

        // ── Comportamiento en Coordinating ────────────────────────────────────���───

        void UpdateCoordinating() {
            ProcessIncoming(_ws);
            if (_ws.PendingCfps.Count == 0 && !HasActiveCnpInitiator())
                Transition(FsmState.Watching);
        }

        // ── API pública para CameraBrain ──────────────────────────────────────────

        // Llamado por CameraBrain cuando detecta al fugitivo en un sector nuevo.
        public void NotifyFugitiveSpotted(Vector3 position, string sectorId) {
            if (sectorId == "[UNK]" || sectorId == _ws.FugitiveSectorId) return;

            _ws.FugitiveSectorId = sectorId;
            _ws.PrisonerInCell   = false;
            _ws.PendingCfps.Clear();

            // Broadcast Inform para sincronizar a los guardias
            Broadcast(new ACLMessage {
                Performative = Performative.Inform,
                Sender       = AgentId,
                Content      = new FugitiveSightingContent(position, Time.time, sectorId, AgentId)
            });

            // Generar plan y encolar todos los contratos
            PerimeterTool.TeamPlan plan =
                PerimeterTool.GenerateTeamPlan(sectorId, PrisonMap.Instance, AgentId);

            foreach (ContractTask task in plan.AllTasks)
                _ws.PendingCfps.Enqueue(task);

            FIPALogger.Log(AgentId, "ops", Performative.Cfp,
                $"sector={sectorId} team={plan.TeamName} tasks={plan.AllTasks.Count}");

            Transition(FsmState.Coordinating);
        }

        // ── Reacción a Informs externos ───────────────────────────────────────────

        // Si un guardia avista al fugitivo en un sector distinto mientras coordinamos,
        // abortamos: limpiamos la cola y cancelamos las subastas en curso.
        protected override void HandleInform(ACLMessage msg, WorldState ws) {
            string sectorAntes = ws.FugitiveSectorId;
            base.HandleInform(msg, ws);

            if (_state != FsmState.Coordinating) return;
            if (ws.FugitiveSectorId == sectorAntes) return; // mismo sector, nada que abortar
            if (ws.FugitiveSectorId == "[UNK]")     return; // info menos precisa, mantener operación

            ws.PendingCfps.Clear();
            CancelOngoingCnpProtocols();
            Transition(FsmState.Watching);
        }

        // ── Transiciones ──────────────────────────────────────────────────────────

        void Transition(FsmState next) {
            FIPALogger.Log(AgentId, "fsm", Performative.Inform, $"{_state} → {next}");
            _state = next;
        }
    }
}
