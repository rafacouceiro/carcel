using UnityEngine;

namespace AgenticPrison.Communication {

    // Tarea asignada a un agente como resultado de ganar un contrato FIPA
    public class ContractTask {
        public TaskType     Type;        // qué se espera hacer al llegar al destino
        public Vector3      Target;      // posición del RoomNode o WayPointData destino
        public string       ContractId;  // conversación que originó la tarea
        public TaskPriority Priority;    // para que BeGuard decida si puede interrumpir
        public string       InitiatorId; // AgentId del iniciador, para enviar InformDone
    }
}
