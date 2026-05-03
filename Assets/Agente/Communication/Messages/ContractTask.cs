using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Physical;

namespace AgenticPrison.Communication.Messages {

    // Tarea asignada al ganador de una subasta CNP.
    public class ContractTask : IMessageContent {
        public TaskType           Type;          // qué tipo de tarea ejecutar
        public AgentRole          AssignedRole;  // rol del agente durante la operación
        public Vector3            Target;        // destino para calcular el coste de la propuesta
        public List<WayPointData> WayPoints;     // waypoints de bloqueo cíclico (blocker)
        public List<RoomNode>     SweepRooms;    // habitaciones a rastrar (sweeper)
        public string             TeamName;      // nombre del equipo al que pertenece esta tarea
        public int                TotalSweepers; // número total de sweepers en la operación

        // Copia profunda para el planner HTN
        public ContractTask Clone() {
            return new ContractTask {
                Type           = this.Type,
                AssignedRole   = this.AssignedRole,
                Target         = this.Target,
                WayPoints      = this.WayPoints != null ? new List<WayPointData>(this.WayPoints) : null,
                SweepRooms     = this.SweepRooms != null ? new List<RoomNode>(this.SweepRooms) : null,
                TeamName       = this.TeamName,
                TotalSweepers  = this.TotalSweepers,
            };
        }
    }
}
