using System;
using UnityEngine;
using AgenticPrison.Core;

using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols;

namespace AgenticPrison.Communication.Protocols.Query {

    // Lado INICIADOR del protocolo Query.
    //
    // Responsabilidad: ante un ruido sospechoso, preguntar en broadcast si algún compañero
    // estaba cerca de esa posición. Si alguno responde, el ruido se descarta como falsa alarma.
    // Si nadie responde antes del deadline, el HTN físico puede proceder a investigar.
    //
    // Flujo:
    //   Init()            ──► broadcast Query, WaitingForNoiseQuery = true
    //   Tick(Inform)      ──► si SenderPosition ≤ Threshold → _sourceWasGuard = true
    //   Tick(time/deadline)► si _sourceWasGuard: borrar LastNoisePosition
    //                        siempre: WaitingForNoiseQuery = false, IsComplete = true
    public class QueryInitiator : ICommProtocol {

        const float QUERY_WINDOW  = 0.3f; // ventana de espera corta — comunicación eficiente
        const float GUARD_THRESHOLD = 8f; // radio en metros para considerar al guardia fuente del ruido

        FIPAAgent _agent;
        Vector3   _noisePosition;
        float     _deadline;
        bool      _sourceWasGuard = false;
        bool      _isComplete     = false;

        public string ConversationId { get; private set; }
        public bool   IsComplete     => _isComplete;

        public QueryInitiator(Vector3 noisePosition) {
            _noisePosition = noisePosition;
            ConversationId = Guid.NewGuid().ToString();
        }

        // Emite el Query en broadcast y arranca el temporizador
        public void Init(FIPAAgent agent, WorldState ws) {
            _agent    = agent;
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
                SentAt         = Time.time,
                ReplyBy        = _deadline,
                SenderPosition = ws.CurrentPosition
            });

            ws.WaitingForNoiseQuery = true;

            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Query,
                $"noisePos={_noisePosition} threshold={GUARD_THRESHOLD} deadline={_deadline:F2}");
        }

        // Recibe respuestas Inform de compañeros cercanos al ruido
        public void Tick(ACLMessage msg, WorldState ws) {
            if (msg.Performative != Performative.Inform) return;

            float dist = Vector3.Distance(msg.SenderPosition, _noisePosition);
            if (dist <= GUARD_THRESHOLD) {
                _sourceWasGuard = true;
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Inform,
                    $"from={msg.Sender} dist={dist:F1} → ruido de compañero confirmado");
            }
        }

        // Evalúa el resultado cuando expira el deadline
        public void Tick(float currentTime, WorldState ws) {
            if (_isComplete || currentTime < _deadline) return;

            if (_sourceWasGuard) {
                // El ruido vino de un compañero: descartar para que el HTN no lo investigue
                ws.LastNoisePosition     = Vector3.zero;
                ws.LastNoisePositionTime = 0f;
                
                // Memoria posicional del guardia para evitar queries futuras si no lo vemos
                ws.LastGuardPosition     = _noisePosition;
                ws.LastGuardPositionTime = Time.time;

                Debug.Log($"<color=cyan>[{_agent.AgentId}] Query: ruido descartado, fuente era un compañero</color>");
            }

            ws.WaitingForNoiseQuery = false;
            _isComplete = true;
        }
    }
}
