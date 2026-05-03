using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Agents.Tools;

namespace AgenticPrison.Agents {

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

        // ── Estados ───────────────────────────────────────────────────────────────

        enum CameraState { Watching, Coordinating }

        CameraState state = CameraState.Watching;

        readonly WorldState ws = new WorldState();

        protected override WorldState GetWorldState() => ws;

        // ── Ciclo de vida ─────────────────────────────────────────────────────────

        protected override void Start() {
            base.Start();
            ws.AgentName = AgentId;
        }

        protected override void Update() {
            base.Update();
            if (state == CameraState.Coordinating) UpdateCoordinating();
        }

        // ── Comportamiento en Coordinating ────────────────────────────────────────

        void UpdateCoordinating() {
            ProcessIncoming(ws);
            if (ws.PendingCfps.Count == 0 && !HasActiveCnpInitiator())
                Transition(CameraState.Watching);
        }

        // ── API pública para CameraBrain ──────────────────────────────────────────

        // Llamado por CameraBrain cuando detecta al fugitivo en un sector nuevo.
        public void NotifyFugitiveSpotted(Vector3 position, string sectorId) {
            if (sectorId == "[UNK]" || sectorId == ws.FugitiveSectorId) return;

            ws.FugitiveSectorId = sectorId;
            ws.PrisonerInCell   = false;
            ws.PendingCfps.Clear();

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
                ws.PendingCfps.Enqueue(task);

            FIPALogger.Log(AgentId, "ops", Performative.Cfp,
                $"sector={sectorId} team={plan.TeamName} tasks={plan.AllTasks.Count}");

            Transition(CameraState.Coordinating);
        }

        // ── Reacción a Informs externos ───────────────────────────────────────────

        // Si un guardia avista al fugitivo en un sector distinto mientras coordinamos,
        // abortamos: limpiamos la cola y cancelamos las subastas en curso.
        protected override void HandleInform(ACLMessage msg, WorldState ws) {
            string sectorAntes = ws.FugitiveSectorId;
            base.HandleInform(msg, ws);

            if (state != CameraState.Coordinating) return;
            if (ws.FugitiveSectorId == sectorAntes)  return; // mismo sector, nada que abortar
            if (ws.FugitiveSectorId == "[UNK]")       return; // info menos precisa, mantener operación

            ws.PendingCfps.Clear();
            CancelOngoingCnpProtocols();
            Transition(CameraState.Watching);
        }

        // ── Transiciones ──────────────────────────────────────────────────────────

        void Transition(CameraState next) {
            FIPALogger.Log(AgentId, "fsm", Performative.Inform, $"{state} → {next}");
            state = next;
        }
    }
}
