using System;
using UnityEngine;
using AgenticPrison.Core;

using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols;

namespace AgenticPrison.Communication.Protocols.Query {

    // Lado PARTICIPANTE del protocolo Query.
    //
    // Responsabilidad: al recibir un Query, comprobar si el agente estaba cerca del punto sospechoso.
    // Si sí, responde con un Inform para que el iniciador descarte el ruido como falsa alarma.
    // El protocolo completa en el mismo frame de Init() — no mantiene estado posterior.
    public class QueryParticipant : ICommProtocol {

        ACLMessage _originalQuery;
        string     _participantId;

        public string ConversationId { get; private set; }
        public bool   IsComplete     { get; private set; } = false;

        public QueryParticipant(ACLMessage queryMsg, string participantId) {
            _originalQuery  = queryMsg;
            _participantId  = participantId;
            ConversationId  = queryMsg.ConversationId;
        } 

        // Comprueba distancia y envía Inform si está en rango. Siempre cierra al instante.
        public void Init(FIPAAgent agent, WorldState ws) {
            var content = _originalQuery.Content as QueryContent;

            if (content != null) {
                float dist = Vector3.Distance(ws.CurrentPosition, content.NoisePosition);
                if (dist <= content.Threshold) {
                    agent.Send(new ACLMessage {
                        MessageId      = Guid.NewGuid().ToString(),
                        Performative   = Performative.Inform,
                        Sender         = agent.AgentId,
                        Receiver       = _originalQuery.Sender,
                        ConversationId = ConversationId,
                        Content        = dist // Enviamos la distancia calculada
                    });

                    FIPALogger.Log(_participantId, ConversationId, Performative.Inform,
                        $"to={_originalQuery.Sender} dist={dist:F1} → soy la fuente del ruido");
                }
            }

            // El participante no espera confirmación — su labor termina aquí
            IsComplete = true;
        }

        // El participante ya completó en Init; estos ticks nunca deben ejecutarse
        public void Tick(ACLMessage msg, WorldState ws) { }
        public void Tick(float currentTime, WorldState ws) { }
    }
}
