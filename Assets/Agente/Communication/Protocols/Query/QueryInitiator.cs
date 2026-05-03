using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Communication.Protocols.Query {

    // Iniciador del protocolo QueryIf: lanza una pregunta en broadcast y espera
    // respuestas Inform durante una ventana corta.
    //
    // Los Inform que llegan se procesan también en FIPAAgent.HandleInform (arquitectura base),
    // así que el protocolo solo necesita controlar el cierre por deadline.
    public class QueryIfInitiator : ICommProtocol {

        const float QUERY_WINDOW = 0.3f;

        enum State { WaitingForResponse, Done }

        readonly Dictionary<(State, Performative), Action<ACLMessage, WorldState>> _onMessage
            = new Dictionary<(State, Performative), Action<ACLMessage, WorldState>>();

        readonly Dictionary<State, Action<float, WorldState>> _onTime
            = new Dictionary<State, Action<float, WorldState>>();

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

        public void Init(FIPAAgent agent, WorldState ws) {
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

        public void Tick(ACLMessage msg, WorldState ws) {
            Action<ACLMessage, WorldState> handler;
            if (_onMessage.TryGetValue((_state, msg.Performative), out handler))
                handler(msg, ws);
        }

        public void Tick(float currentTime, WorldState ws) {
            Action<float, WorldState> handler;
            if (_onTime.TryGetValue(_state, out handler)) handler(currentTime, ws);
        }

        // El WorldState lo actualiza el agente en HandleInform — aquí solo logueamos
        void OnInformReceived(ACLMessage msg, WorldState ws) {
            FIPALogger.Log(_agentId, ConversationId, Performative.Inform, $"respuesta de {msg.Sender}");
        }

        void CheckDeadline(float currentTime, WorldState ws) {
            if (currentTime < _deadline) return;
            FIPALogger.Log(_agentId, ConversationId, Performative.QueryIf, "ventana cerrada");
            _state = State.Done;
        }
    }
}
