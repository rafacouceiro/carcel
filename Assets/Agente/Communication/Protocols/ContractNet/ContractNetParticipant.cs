using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols;

namespace AgenticPrison.Communication.Protocols.ContractNet {

    // Lado PARTICIPANTE del protocolo Contract Net.
    //
    // Responsabilidad: recibir un CFP, enviar una propuesta con el coste estimado,
    // y escribir la tarea en WorldState si el iniciador acepta nuestra oferta.
    //
    // Flujo de estados (sin fase de ejecución — la ejecución se gestiona por canales):
    //   CfpReceived  ──[SendPropose llamado]──► Proposed
    //   Proposed     ──[AcceptProposal]────────► Done  (tarea escrita en ws.AssignedTask)
    //   Proposed     ──[RejectProposal]───────► Done
    public class ContractNetParticipant : ICommProtocol {

        // ── Estados ────────────────────────────────────────────────────────────────
        enum State {
            CfpReceived,   // CFP recibido, respuesta reactiva en FIPAAgent.ProcessIncoming
            Proposed,      // propuesta enviada, esperando Accept o Reject del iniciador
            Done
        }

        // ── Tabla de transición ────────────────────────────────────────────────────
        readonly Dictionary<(State, Performative), Action<ACLMessage, WorldState>> _onMessage
            = new Dictionary<(State, Performative), Action<ACLMessage, WorldState>>();

        // ── Datos internos ─────────────────────────────────────────────────────────
        State      _state = State.CfpReceived;
        ACLMessage _originalCfp;
        string     _participantId;

        // ── ICommProtocol ──────────────────────────────────────────────────────────
        public string ConversationId { get; private set; }
        public bool   IsComplete     => _state == State.Done;

        // ── Constructor ────────────────────────────────────────────────────────────
        public ContractNetParticipant(ACLMessage cfp, string participantId) {
            _originalCfp   = cfp;
            _participantId = participantId;
            ConversationId = cfp.ConversationId;
            BuildTransitions();
        }

        // ── Inicio del protocolo ───────────────────────────────────────────────────
        // FIPAAgent llama a EvaluateCfp() justo después y envía respuesta reactiva.
        public void Init(FIPAAgent agent, WorldState ws) { }

        // ── Tick por mensaje entrante ──────────────────────────────────────────────
        public void Tick(ACLMessage msg, WorldState ws) {
            Action<ACLMessage, WorldState> handler;
            if (_onMessage.TryGetValue((_state, msg.Performative), out handler))
                handler(msg, ws);
        }

        // ── Tick por tiempo ────────────────────────────────────────────────────────
        // Si el CFP tenía ReplyBy y ya expiró antes de recibir respuesta definitiva (Accept/Reject), cerrar.
        // Esto evita que el agente quede bloqueado en "participación activa" si el iniciador falla.
        public void Tick(float currentTime, WorldState ws) {
            if (_state == State.Done) return;

            if (_originalCfp.ReplyBy > 0f && currentTime > _originalCfp.ReplyBy + 1.0f) {
                if (_state == State.Proposed) {
                    FIPALogger.Log(_participantId, ConversationId, Performative.Failure, "Protocol timeout - no response from initiator");
                }
                _state = State.Done;
            }
        }

        // ── Construcción de la tabla de transiciones ───────────────────────────────
        void BuildTransitions() {
            _onMessage[(State.Proposed, Performative.AcceptProposal)] = OnAccepted;
            _onMessage[(State.Proposed, Performative.RejectProposal)] = OnRejected;
        }

        // ── API pública ────────────────────────────────────────────────────────────

        // Envía la propuesta al iniciador con el coste calculado
        public void SendPropose(FIPAAgent agent, WorldState ws, float cost) {
            if (_state != State.CfpReceived) return;

            agent.Send(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Propose,
                Sender         = agent.AgentId,
                Receiver       = _originalCfp.Sender,
                ConversationId = ConversationId,
                Content        = new ProposeContent { Cost = cost },
                ReplyBy        = _originalCfp.ReplyBy
            });

            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Propose,
                $"to={_originalCfp.Sender} cost={cost:F1}");
            
            _state = State.Proposed;
        }

        // Rechaza el CFP cuando EvaluateCfp() devuelve false
        public void SendRefuse(FIPAAgent agent, WorldState ws) {
            if (_state != State.CfpReceived) return;

            agent.Send(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Refuse,
                Sender         = agent.AgentId,
                Receiver       = _originalCfp.Sender,
                ConversationId = ConversationId
            });

            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Refuse,
                $"to={_originalCfp.Sender}");
            ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
            _state = State.Done;
        }

        // ── Handlers de mensajes ───────────────────────────────────────────────────

        // El iniciador eligió nuestra propuesta — el agente (GuardBrain.HandleAcceptProposal)
        // es quien escribe los efectos en WorldState; el protocolo solo cierra.
        void OnAccepted(ACLMessage msg, WorldState ws) {
            FIPALogger.Log(_participantId, ConversationId, Performative.AcceptProposal,
                $"proposal accepted by {msg.Sender}");
            ConversationTracker.Instance.UpdateState(ConversationId, "Done");
            _state = State.Done;
        }

        // El iniciador eligió a otro guardia — simplemente cerrar
        void OnRejected(ACLMessage msg, WorldState ws) {
            FIPALogger.Log(_participantId, ConversationId, Performative.RejectProposal,
                "proposal rejected");
            ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
            
            _state = State.Done;
        }
    }
}
