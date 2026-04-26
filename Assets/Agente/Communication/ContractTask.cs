using UnityEngine;
using AgenticPrison.Physical;

namespace AgenticPrison.Communication {

    // Tarea asignada a un agente como resultado de ganar un contrato FIPA
    public class ContractTask {
        public TaskType  Type;        // qué se espera hacer al llegar al destino
        public RoomNode  Room;        // habitación a investigar (puede ser null)
        public Vector3   Target;      // punto exacto del destino — obligatorio para calcular el coste
        public string    ContractId;  // conversación que originó la tarea
        public string    InitiatorId; // AgentId del iniciador, para enviar InformDone
    }
}
