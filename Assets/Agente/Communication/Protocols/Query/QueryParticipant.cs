using System;
using AgenticPrison.Core;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Communication.Protocols.Query {

    // Lado PARTICIPANTE del protocolo QueryIf.
    //
    // Agnóstico al contenido: no evalúa ni decide si responder.
    // Solo expone SendInform() para que el agente (vía OnQueryIfReceived) responda si lo considera oportuno.
    // Completa en el mismo frame de Init() — no mantiene estado posterior.
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

        // No evalúa — el agente decide vía OnQueryIfReceived inmediatamente después
        public void Init(FIPAAgent agent, AgentState ws) {
            IsComplete = true;
        }

        // API para que el agente envíe su respuesta si decide participar
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

        public void Tick(ACLMessage msg, AgentState ws) { }
        public void Tick(float currentTime, AgentState ws) { }
    }
}
