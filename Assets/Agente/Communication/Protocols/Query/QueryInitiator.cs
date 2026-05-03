using System;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Communication.Protocols.Query {

    // Lado INICIADOR del protocolo Query.
    // PUREZA: Solo gestiona el flujo de mensajes y expira por tiempo.
    // El agente que lo lanza es el responsable de leer 'SourceWasGuard' y actuar.
    public class QueryInitiator : ICommProtocol {

        const float QUERY_WINDOW  = 0.3f;
        const float GUARD_THRESHOLD = 25f;

        string    _agentId;
        Vector3   _noisePosition;
        float     _deadline;
        bool      _sourceWasGuard = false;
        bool      _isComplete     = false;

        public string ConversationId { get; private set; }
        public bool   IsComplete     => _isComplete;
        public bool   SourceWasGuard => _sourceWasGuard;

        public QueryInitiator(Vector3 noisePosition) {
            _noisePosition = noisePosition;
            ConversationId = Guid.NewGuid().ToString();
        }

        public void Init(FIPAAgent agent, WorldState ws) {
            _agentId  = agent.AgentId;
            _deadline = Time.time + QUERY_WINDOW;

            agent.Broadcast(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Query,
                Sender         = agent.AgentId,
                Receiver       = null,
                ConversationId = ConversationId,
                Content        = new QueryContent {
                NoisePosition = _noisePosition,
                Threshold     = GUARD_THRESHOLD
                },
                ReplyBy        = _deadline
            });

            FIPALogger.Log(_agentId, ConversationId, Performative.Query, $"Ask about noise at {_noisePosition}");
        }

        public void Tick(ACLMessage msg, WorldState ws) {
            if (msg.Performative != Performative.Inform) return;

            // Recogemos la distancia que nos envía el compañero
            if (msg.Content is float dist) {
                if (dist <= GUARD_THRESHOLD) {
                    _sourceWasGuard = true;
                    ws.LastNoisePosition = UnityEngine.Vector3.zero;
                    ws.LastGuardPosition = _noisePosition;
                    ws.LastGuardPositionTime = Time.time;
                    FIPALogger.Log(_agentId, ConversationId, Performative.Inform, $"from={msg.Sender} confirm guard source");
                }
            }
        }

        public void Tick(float currentTime, WorldState ws) {
            if (_isComplete || currentTime < _deadline) return;
            _isComplete = true;
        }
    }
}