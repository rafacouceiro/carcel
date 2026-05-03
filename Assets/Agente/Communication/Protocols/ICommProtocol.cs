using AgenticPrison.Core;

using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Communication.Protocols {

    // Interfaz común para todos los protocolos FIPA.
    // FIPAAgent los registra en _ongoing_conversations y los tickea cada frame.
    public interface ICommProtocol {

        string ConversationId { get; }

        // Verdadero cuando el protocolo terminó (Done o Failed) — FIPAAgent lo elimina del diccionario
        bool IsComplete { get; }

        void Init(FIPAAgent agent, WorldState ws);
        void Tick(ACLMessage msg, WorldState ws);
        void Tick(float currentTime, WorldState ws);
    }
}
