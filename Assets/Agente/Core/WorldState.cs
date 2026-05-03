using System.Collections.Generic;
using AgenticPrison.Physical;
using UnityEngine;

namespace AgenticPrison.Core {

    // Representa el conocimiento que tiene el agente, tanto interno como del entorno
    public class WorldState {

        // Estado interno
        public string AgentName = string.Empty;
        public Vector3 CurrentPosition = Vector3.zero;
        public float Energy = 100f; // Energía del agente (0 a 100)

        // Memoria visual
        public bool FugitiveInVision = false; // Indica si el fugitivo está a la vista
        public Vector3 LastKnownPosition = Vector3.zero; // Última posición donde se vio al fugitivo
        public float LastKnownPositionTime = 0f; // Instante en el que se vio al fugitivo

        // Memoria sobre otros agentes
        public Vector3 LastGuardPosition = Vector3.zero;
        public float LastGuardPositionTime = 0f;
        
        // Memoria auditiva
        public Vector3 LastNoisePosition = Vector3.zero; // Origen del último ruido sospechoso
        public float LastNoisePositionTime = 0f; // Instante del último ruido

        // Estado del entorno
        public bool PrisonerInCell = true; // Empieza asumiendo que el prisionero está contenido

        // Navegación
        public PrisonMap Map; // Referencia al mapa de la prisión
        public string AssignedQuadrantId = string.Empty; // Zona de patrulla asignada

        // Genera una copia del estado para simulaciones de planificación
        public WorldState Clone() {
            var clone = new WorldState {
                FugitiveInVision = this.FugitiveInVision,
                PrisonerInCell = this.PrisonerInCell,
                Energy = this.Energy,
                CurrentPosition = this.CurrentPosition,
                LastKnownPosition = this.LastKnownPosition,
                LastNoisePosition = this.LastNoisePosition,
                Map = this.Map,
                AssignedQuadrantId = this.AssignedQuadrantId,
                LastKnownPositionTime = this.LastKnownPositionTime,
                LastNoisePositionTime = this.LastNoisePositionTime,
                AgentName = this.AgentName,
                LastGuardPosition = this.LastGuardPosition,
                LastGuardPositionTime = this.LastGuardPositionTime
            };

            return clone;
        }
    }
}