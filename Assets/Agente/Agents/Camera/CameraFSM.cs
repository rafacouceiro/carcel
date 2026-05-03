using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Agents.Tools;

namespace AgenticPrison.Agents.Camera {

    // Coordina la respuesta de la cámara cuando detecta al fugitivo.
    // Tiene dos estados: esperando (Watching) o lanzando subastas CNP (Coordinating).
    //
    // Está separado de CameraBrain a propósito: que la cámara pierda de vista
    // al fugitivo no tiene nada que ver con si debe seguir coordinando o no.
    public class CameraFSM : FIPAAgent {

        public override string AgentId => gameObject.name;

        enum FsmState { Watching, Coordinating }

        FsmState         _state = FsmState.Watching;
        readonly CameraWorldState _ws = new CameraWorldState();

        protected override WorldState GetAgentState() => _ws;

        protected override void Start() {
            base.Start();
            _ws.AgentName = AgentId;
        }

        protected override void Update() {
            base.Update();
            if (_state == FsmState.Coordinating) UpdateCoordinating();
        }

        void UpdateCoordinating() {
            ProcessIncoming(_ws);
            if (_ws.PendingCfps.Count == 0 && !HasActiveCnpInitiator())
                Transition(FsmState.Watching);
        }

        // CameraBrain llama a esto cuando detecta al fugitivo en un sector concreto.
        // Si ya sabíamos el sector o es desconocido, no hacemos nada.
        public void NotifyFugitiveSpotted(Vector3 position, string sectorId) {
            if (sectorId == "[UNK]" || sectorId == _ws.FugitiveSectorId) return;

            _ws.FugitiveSectorId = sectorId;
            _ws.PrisonerInCell   = false;
            _ws.PendingCfps.Clear();

            Broadcast(new ACLMessage {
                Performative = Performative.Inform,
                Sender       = AgentId,
                Content      = new FugitiveSightingContent(position, Time.time, sectorId, AgentId)
            });

            PerimeterTool.TeamPlan plan = PerimeterTool.GenerateTeamPlan(sectorId, PrisonMap.Instance, AgentId);
            foreach (ContractTask task in plan.AllTasks)
                _ws.PendingCfps.Enqueue(task);

            FIPALogger.Log(AgentId, "ops", Performative.Cfp,
                $"sector={sectorId} team={plan.TeamName} tasks={plan.AllTasks.Count}");

            Transition(FsmState.Coordinating);
        }

        // Si mientras coordinamos llega un Inform con un sector distinto,
        // abortamos todo — las subastas anteriores ya no tienen sentido.
        // Si es [UNK] lo ignoramos porque es info menos precisa que la que tenemos.
        protected override void HandleInform(ACLMessage msg, WorldState ws) {
            string sectorAntes = ws.FugitiveSectorId;
            base.HandleInform(msg, ws);

            if (_state != FsmState.Coordinating)    return;
            if (ws.FugitiveSectorId == sectorAntes) return;
            if (ws.FugitiveSectorId == "[UNK]")     return;

            ws.PendingCfps.Clear();
            CancelOngoingCnpProtocols();
            Transition(FsmState.Watching);
        }

        void Transition(FsmState next) {
            FIPALogger.Log(AgentId, "fsm", Performative.Inform, $"{_state} → {next}");
            _state = next;
        }
    }
}
