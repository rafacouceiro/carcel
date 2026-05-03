using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols;

namespace AgenticPrison.Communication.Protocols.ContractNet {

    // Lado del que recibe el CFP en una subasta Contract Net.
    // Cuando llega el CFP, FIPAAgent ya llama a EvaluateCfp y dispara SendPropose o SendRefuse.
    // Si ganamos la subasta, el protocolo cierra y el agente (GuardBrain) aplica el AcceptProposal.
    //
    // Estados:
    //   CfpReceived → Proposed → Done
    //   CfpReceived → Done     (si rechazamos)
    public class ContractNetParticipant : ICommProtocol {

        enum State {
            CfpReceived,  // CFP recibido, pendiente de respuesta reactiva
            Proposed,     // propuesta enviada, esperando la decisión del iniciador
            Done
        }

        readonly Dictionary<(State, Performative), Action<ACLMessage, WorldState>> _onMessage
            = new Dictionary<(State, Performative), Action<ACLMessage, WorldState>>();

        State      _state = State.CfpReceived;
        ACLMessage _originalCfp;
        string     _participantId;

        public string ConversationId { get; private set; }
        public bool   IsComplete     => _state == State.Done;

        public ContractNetParticipant(ACLMessage cfp, string participantId) {
            _originalCfp   = cfp;
            _participantId = participantId;
            ConversationId = cfp.ConversationId;
            BuildTransitions();
        }

        // El agente llama inmediatamente a SendPropose o SendRefuse después de Init
        public void Init(FIPAAgent agent, WorldState ws) { }

        public void Tick(ACLMessage msg, WorldState ws) {
            Action<ACLMessage, WorldState> handler;
            if (_onMessage.TryGetValue((_state, msg.Performative), out handler))
                handler(msg, ws);
        }

        // Si el iniciador nunca responde (falla o se cuelga), cerramos para no quedar bloqueados
        public void Tick(float currentTime, WorldState ws) {
            if (_state == State.Done) return;

            if (_originalCfp.ReplyBy > 0f && currentTime > _originalCfp.ReplyBy + 1.0f) {
                if (_state == State.Proposed)
                    FIPALogger.Log(_participantId, ConversationId, Performative.Failure, "timeout — sin respuesta del iniciador");
                _state = State.Done;
            }
        }

        void BuildTransitions() {
            _onMessage[(State.Proposed, Performative.AcceptProposal)] = OnAccepted;
            _onMessage[(State.Proposed, Performative.RejectProposal)] = OnRejected;
        }

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

        // Ganamos — los efectos (AssignedTask, TeamName…) los aplica GuardBrain.HandleAcceptProposal,
        // no el protocolo. El protocolo solo cierra.
        void OnAccepted(ACLMessage msg, WorldState ws) {
            FIPALogger.Log(_participantId, ConversationId, Performative.AcceptProposal,
                $"propuesta aceptada por {msg.Sender}");
            ConversationTracker.Instance.UpdateState(ConversationId, "Done");
            _state = State.Done;
        }

        void OnRejected(ACLMessage msg, WorldState ws) {
            FIPALogger.Log(_participantId, ConversationId, Performative.RejectProposal,
                "propuesta rechazada");
            ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
            _state = State.Done;
        }
    }
}
