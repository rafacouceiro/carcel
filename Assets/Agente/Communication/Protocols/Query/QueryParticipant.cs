using System;
using AgenticPrison.Core;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Communication.Protocols.Query {

    // Participante del protocolo QueryIf.
    // No evalúa nada por sí solo: expone SendInform() para que el agente
    // (via OnQueryIfReceived) decida si responder o ignorar la pregunta.
    // Completa en el mismo frame que Init() — no hay estado que mantener.
    public class QueryIfParticipant : ICommProtocol {

        readonly ACLMessage _originalQuery;
        readonly string     _participantId;

        public string ConversationId { get; private set; }
        public bool   IsComplete     { get; private set; } = false;

        public QueryIfParticipant(ACLMessage queryMsg, string participantId) {
            _originalQuery = queryMsg;
            _participantId = participantId;
            ConversationId = queryMsg.ConversationId;
        }

        // Termina de inmediato; el agente llama a SendInform justo después si quiere responder
        public void Init(FIPAAgent agent, WorldState ws) {
            IsComplete = true;
        }

        public void SendInform(FIPAAgent agent, IMessageContent content) {
            agent.Send(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Inform,
                Sender         = agent.AgentId,
                Receiver       = _originalQuery.Sender,
                ConversationId = ConversationId,
                Content        = content
            });
            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Inform,
                $"to={_originalQuery.Sender}");
        }

        public void Tick(ACLMessage msg, WorldState ws) { }
        public void Tick(float currentTime, WorldState ws) { }
    }
}
