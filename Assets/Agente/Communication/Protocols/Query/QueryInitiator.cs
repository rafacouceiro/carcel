using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Communication.Protocols.Query {

    // Lado INICIADOR del protocolo QueryIf.
    //
    // Flujo de estados — paralelo a ContractNetInitiator:
    //   WaitingForResponse  ──[Inform]──────► WaitingForResponse  (acumula respuestas)
    //   WaitingForResponse  ──[deadline]────► Done
    //
    // El protocolo no escribe AgentState. Cada Inform llega también a OnMessageReceived
    // del agente (arquitectura base de FIPAAgent), que lo procesa en HandleInform.
    public class QueryIfInitiator : ICommProtocol {

        const float QUERY_WINDOW = 0.3f;

        enum State { WaitingForResponse, Done }

        readonly Dictionary<(State, Performative), Action<ACLMessage, AgentState>> _onMessage
            = new Dictionary<(State, Performative), Action<ACLMessage, AgentState>>();

        readonly Dictionary<State, Action<float, AgentState>> _onTime
            = new Dictionary<State, Action<float, AgentState>>();

        readonly IMessageContent _content;

        State  _state = State.WaitingForResponse;
        string _agentId;
        float  _deadline;

        public string ConversationId { get; private set; }
        public bool   IsComplete     => _state == State.Done;

        public QueryIfInitiator(IMessageContent content) {
            _content       = content;
            ConversationId = Guid.NewGuid().ToString();
            BuildTransitions();
        }

        void BuildTransitions() {
            _onMessage[(State.WaitingForResponse, Performative.Inform)] = OnInformReceived;
            _onTime   [State.WaitingForResponse]                        = CheckDeadline;
        }

        public void Init(FIPAAgent agent, AgentState ws) {
            _agentId  = agent.AgentId;
            _deadline = Time.time + QUERY_WINDOW;

            agent.Broadcast(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.QueryIf,
                Sender         = agent.AgentId,
                ConversationId = ConversationId,
                Content        = _content,
                ReplyBy        = _deadline
            });

            FIPALogger.Log(_agentId, ConversationId, Performative.QueryIf, "broadcast");
        }

        public void Tick(ACLMessage msg, AgentState ws) {
            Action<ACLMessage, AgentState> handler;
            if (_onMessage.TryGetValue((_state, msg.Performative), out handler))
                handler(msg, ws);
        }

        public void Tick(float currentTime, AgentState ws) {
            Action<float, AgentState> handler;
            if (_onTime.TryGetValue(_state, out handler)) handler(currentTime, ws);
        }

        // Registra la respuesta. El AgentState lo escribe el agente en HandleInform.
        void OnInformReceived(ACLMessage msg, AgentState ws) {
            FIPALogger.Log(_agentId, ConversationId, Performative.Inform, $"response from={msg.Sender}");
        }

        void CheckDeadline(float currentTime, AgentState ws) {
            if (currentTime < _deadline) return;
            FIPALogger.Log(_agentId, ConversationId, Performative.QueryIf, "window closed");
            _state = State.Done;
        }
    }
}
