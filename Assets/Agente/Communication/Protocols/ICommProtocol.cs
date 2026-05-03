using AgenticPrison.Core;

using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Communication.Protocols {

    // Interfaz común para todos los protocolos de comunicación FIPA
    public interface ICommProtocol {

        // Identificador único de la conversación que gestiona este protocolo
        string ConversationId { get; }

        // Verdadero cuando el protocolo ha terminado (Done o Failed)
        bool IsComplete { get; }

        // Inicializa el protocolo y envía el primer mensaje si corresponde
        void Init(FIPAAgent agent, WorldState ws);

        // Avanza el protocolo al recibir un mensaje
        void Tick(ACLMessage msg, WorldState ws);

        // Avanza el protocolo por tiempo (para transiciones de deadline)
        void Tick(float currentTime, WorldState ws);
    }
}
