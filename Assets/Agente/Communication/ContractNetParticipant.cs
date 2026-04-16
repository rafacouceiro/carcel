using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Communication {

    // Lado PARTICIPANTE del protocolo Contract Net.
    //
    // Responsabilidad: recibir un CFP, enviar una propuesta con el coste estimado,
    // y ejecutar la tarea si el iniciador acepta nuestra oferta.
    //
    // Flujo de estados:
    //   CfpReceived  ──[SendPropose llamado]──► Proposed
    //   Proposed     ──[AcceptProposal]────────► Executing
    //   Proposed     ──[RejectProposal]───────► Done
    //   Executing    ──[InformDone interno]───► Done
    public class ContractNetParticipant : ICommProtocol {

        // ── Estados ────────────────────────────────────────────────────────────────
        enum State {
            CfpReceived,   // CFP recibido, esperando que BeSocial decida propose/refuse
            Proposed,      // propuesta enviada, esperando Accept o Reject del iniciador
            Executing,     // aceptados, la tarea ya está en WorldState.AssignedTask
            Done
        }

        // ── Tabla de transición ────────────────────────────────────────────────────
        // _onMessage[(estado, performativa)] = método a llamar cuando llega ese mensaje en ese estado
        readonly Dictionary<(State, Performative), Action<ACLMessage, WorldState>> _onMessage
            = new Dictionary<(State, Performative), Action<ACLMessage, WorldState>>();

        // ── Datos internos ─────────────────────────────────────────────────────────
        State      _state = State.CfpReceived;
        ACLMessage _originalCfp;   // guardamos el CFP original para responder al emisor correcto

        // ── ICommProtocol ──────────────────────────────────────────────────────────
        public string ConversationId { get; private set; }
        public bool   IsComplete     => _state == State.Done;

        // ── Constructor ────────────────────────────────────────────────────────────
        // cfp:           el mensaje CFP recibido del iniciador
        // participantId: AgentId de este guardia (para identificación)
        public ContractNetParticipant(ACLMessage cfp, string participantId) {
            _originalCfp   = cfp;
            ConversationId = cfp.ConversationId; // usamos el mismo ID que el iniciador para el enrutado
            BuildTransitions();
        }

        // ── Inicio del protocolo ───────────────────────────────────────────────────
        // Llamado por FIPAAgent.LaunchProtocol. El participante no envía nada aquí:
        // la tarea social SendProposeTask es quien decide si proponer o no.
        public void Init(FIPAAgent agent, WorldState ws) {
            // Sin acción — el participante espera la decisión del HTN social
        }

        // ── Tick por mensaje entrante ──────────────────────────────────────────────
        // FIPAAgent llama a este método cuando llega un mensaje con nuestro ConversationId.
        public void Tick(ACLMessage msg, WorldState ws) {
            Action<ACLMessage, WorldState> handler;
            if (_onMessage.TryGetValue((_state, msg.Performative), out handler))
                handler(msg, ws);
        }

        // ── Tick por tiempo ────────────────────────────────────────────────────────
        // El participante no tiene deadlines propios, así que este tick no hace nada.
        public void Tick(float currentTime, WorldState ws) { }

        // ── Construcción de la tabla de transiciones ───────────────────────────────
        void BuildTransitions() {
            _onMessage[(State.Proposed,  Performative.AcceptProposal)] = OnAccepted;
            _onMessage[(State.Proposed,  Performative.RejectProposal)] = OnRejected;
            _onMessage[(State.Executing, Performative.InformDone)]     = OnExecutionDone;
        }

        // ── API pública ────────────────────────────────────────────────────────────

        // Llamado por SendProposeTask para enviar la propuesta al iniciador.
        // cost: longitud del camino NavMesh hasta el objetivo (bid del guardia)
        public void SendPropose(FIPAAgent agent, WorldState ws, float cost) {
            if (_state != State.CfpReceived) return; // solo se puede proponer una vez

            agent.Send(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Propose,
                Sender         = agent.AgentId,
                Receiver       = _originalCfp.Sender,   // responder al iniciador
                ConversationId = ConversationId,
                Content        = new ProposalContent { EstimatedCost = cost, ExecutorId = agent.AgentId },
                SentAt         = Time.time,
                ReplyBy        = _originalCfp.ReplyBy,  // respetar el deadline del iniciador
                SenderPosition = ws.CurrentPosition
            });

            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Propose,
                $"to={_originalCfp.Sender} cost={cost:F1}");
            _state = State.Proposed;
        }

        // ── Handlers de mensajes ───────────────────────────────────────────────────

        // El iniciador eligió nuestra propuesta.
        // Escribimos la tarea en WorldState para que BeGuard la ejecute.
        void OnAccepted(ACLMessage msg, WorldState ws) {
            ContractTask won = (ContractTask)_originalCfp.Content;
            won.InitiatorId = _originalCfp.Sender;  // InformDoneTask necesita saber a quién informar
            ws.AssignedTask = won;
            ws.PendingCfp   = null;

            FIPALogger.Log(null, ConversationId, Performative.AcceptProposal,
                $"task assigned: {ws.AssignedTask?.Type}");
            ConversationTracker.Instance.UpdateState(ConversationId, "Executing");
            _state = State.Executing;
        }

        // El iniciador eligió a otro guardia. Limpiamos el CFP y cerramos.
        void OnRejected(ACLMessage msg, WorldState ws) {
            ws.PendingCfp = null;
            FIPALogger.Log(null, ConversationId, Performative.RejectProposal,
                "proposal rejected");
            ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
            _state = State.Done;
        }

        // InformDoneTask notifica al protocolo que la tarea física terminó.
        // (El protocolo ya no tiene nada que hacer — la tarea fue completada.)
        void OnExecutionDone(ACLMessage msg, WorldState ws) {
            ws.AssignedTask = null;
            ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
            _state = State.Done;
        }
    }
}
