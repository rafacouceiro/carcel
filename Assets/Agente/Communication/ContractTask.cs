using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Physical;

using AgenticPrison.Communication.Messages;

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
        public string            InitiatorId;  // AgentId del iniciador
        public string            TeamName;     // nombre del equipo al que pertenece esta tarea

        // Copia profunda para que el planner HTN no mute la lista real de SweepRooms
        // durante la simulación de efectos (ApplyEffects).
        public ContractTask Clone() {
            return new ContractTask {
                Type         = this.Type,
                AssignedRole = this.AssignedRole,
                SectorId     = this.SectorId,
                Target       = this.Target,
                WayPoints    = this.WayPoints != null ? new List<WayPointData>(this.WayPoints) : null,
                SweepRooms   = this.SweepRooms != null ? new List<RoomNode>(this.SweepRooms) : null,
                ContractId   = this.ContractId,
                InitiatorId  = this.InitiatorId,
                TeamName     = this.TeamName,
            };
        }
    }
}
