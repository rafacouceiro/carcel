using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Physical;

namespace AgenticPrison.Communication {

    // Tarea asignada a un agente como resultado de ganar un contrato FIPA
    public class ContractTask {
        public TaskType          Type;         // qué tipo de tarea ejecutar
        public AgentRole         AssignedRole; // rol del agente durante la operación
        public string            SectorId;     // sector al que pertenece la operación
        public Vector3           Target;       // destino para calcular el coste de la propuesta
        public List<WayPointData> WayPoints;   // waypoints de bloqueo cíclico (blocker)
        public List<RoomNode>    SweepRooms;   // habitaciones a rastrar (sweeper)
        public string            ContractId;   // conversación que originó la tarea
        public string            InitiatorId;  // AgentId del iniciador, para enviar InformDone
    }
}
