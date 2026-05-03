using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols;

namespace AgenticPrison.Communication.Protocols.ContractNet {

    // Lado del que lanza la subasta Contract Net.
    // Emite un CFP en broadcast, espera propuestas durante una ventana de tiempo
    // y acepta la más barata. Si nadie propone, la subasta falla.
    //
    // Estados:
    //   WaitingForProposals → Evaluating → Done
    //   WaitingForProposals → Failed (si no hay propuestas al cierre)
    public class ContractNetInitiator : ICommProtocol {

        enum State { WaitingForProposals, Evaluating, Done, Failed }

        // Las transiciones se registran en tablas para que el código de despacho
        // sea genérico y no haya un switch gigante en Tick
        readonly Dictionary<(State, Performative), Action<ACLMessage, WorldState>> _onMessage
            = new Dictionary<(State, Performative), Action<ACLMessage, WorldState>>();

        readonly Dictionary<State, Action<float, WorldState>> _onTime
            = new Dictionary<State, Action<float, WorldState>>();

        State            _state = State.WaitingForProposals;
        FIPAAgent        _agent;
        CfpContent       _content;
        float            _deadline;
        float            _replyByWindow;
        List<ACLMessage> _proposals = new List<ACLMessage>();

        public string ConversationId { get; private set; }
        public bool   IsComplete     => _state == State.Done || _state == State.Failed;

        public ContractNetInitiator(CfpContent content, float replyByWindow) {
            _content       = content;
            _replyByWindow = replyByWindow;
            ConversationId = Guid.NewGuid().ToString();
            BuildTransitions();
        }

        // FIPAAgent llama a esto justo después de registrar el protocolo.
        // Aquí es donde sale el CFP por el bus.
        public void Init(FIPAAgent agent, WorldState ws) {
            _agent    = agent;
            _deadline = Time.time + _replyByWindow;

            agent.Broadcast(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Cfp,
                Sender         = agent.AgentId,
                Receiver       = null,
                ConversationId = ConversationId,
                Content        = _content,
                ReplyBy        = _deadline
            });

            ConversationTracker.Instance.Register(ConversationId, agent.AgentId);
            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Cfp,
                $"task={_content.Task.Type} target={_content.Task.Target}");
        }

        // FIPAAgent enruta aquí los mensajes de esta conversación
        public void Tick(ACLMessage msg, WorldState ws) {
            Action<ACLMessage, WorldState> handler;
            if (_onMessage.TryGetValue((_state, msg.Performative), out handler))
                handler(msg, ws);
            // Combinaciones no registradas simplemente se ignoran
        }

        public void Tick(float currentTime, WorldState ws) {
            Action<float, WorldState> handler;
            if (_onTime.TryGetValue(_state, out handler))
                handler(currentTime, ws);
        }

        void BuildTransitions() {
            _onMessage[(State.WaitingForProposals, Performative.Propose)] = OnProposalReceived;
            _onMessage[(State.WaitingForProposals, Performative.Refuse)]  = OnRefuseReceived;
            _onTime   [State.WaitingForProposals]                         = CheckDeadline;
        }

        void OnProposalReceived(ACLMessage msg, WorldState ws) {
            _proposals.Add(msg);
            ConversationTracker.Instance.AddParticipant(ConversationId, msg.Sender);
            var pc = ACLMessage.GetContent<ProposeContent>(msg);
            FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Propose,
                $"from={msg.Sender} cost={pc?.Cost:F1}");
        }

        // Los rechazos no hacen nada; simplemente no acumulamos esa propuesta
        void OnRefuseReceived(ACLMessage msg, WorldState ws) { }

        void CheckDeadline(float currentTime, WorldState ws) {
            if (currentTime < _deadline) return;

            if (_proposals.Count > 0)
                EvaluateAndAccept(ws);
            else {
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Failure, "no proposals received");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Failed");
                _state = State.Failed;
            }
        }

        void EvaluateAndAccept(WorldState ws) {
            _state = State.Evaluating;

            ACLMessage winner    = default;
            bool       hasWinner = false;
            float      minCost   = float.MaxValue;
            foreach (ACLMessage p in _proposals) {
                float c = GetCost(p);
                if (c < minCost) { minCost = c; winner = p; hasWinner = true; }
            }

            if (!hasWinner) {
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Failure, "all proposers already in team");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Failed");
                _state = State.Failed;
                return;
            }

            _agent.Send(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.AcceptProposal,
                Sender         = _agent.AgentId,
                Receiver       = winner.Sender,
                ConversationId = ConversationId,
                Content        = _content.Task
            });
            FIPALogger.Log(_agent.AgentId, ConversationId, Performative.AcceptProposal,
                $"winner={winner.Sender} cost={minCost:F1}");

            foreach (ACLMessage p in _proposals) {
                if (p.Sender == winner.Sender) continue;
                _agent.Send(new ACLMessage {
                    MessageId      = Guid.NewGuid().ToString(),
                    Performative   = Performative.RejectProposal,
                    Sender         = _agent.AgentId,
                    Receiver       = p.Sender,
                    ConversationId = ConversationId
                });
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.RejectProposal, $"to={p.Sender}");
            }

            ConversationTracker.Instance.UpdateState(ConversationId, "Done");
            // El CNP termina aquí; la ejecución de la tarea se coordina por canales de equipo
            _state = State.Done;
        }

        // Si el contenido no es ProposeContent devolvemos MaxValue para que nunca gane
        float GetCost(ACLMessage proposal) {
            var pc = ACLMessage.GetContent<ProposeContent>(proposal);
            return pc != null ? pc.Cost : float.MaxValue;
        }
    }
}
