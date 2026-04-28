using AgenticPrison.Communication;

namespace AgenticPrison.Communication.Protocols.ContractNet {

    // Contenido de un mensaje CFP (Call For Proposals) del protocolo Contract Net.
    // Ahora es minimalista: solo contiene la tarea subastada.
    // El sector ya viene dentro de la tarea y la ubicación se gestiona vía broadcast INFORM.
    public class CfpContent {
        public ContractTask Task;      // tarea concreta que se subasta
    }
}
