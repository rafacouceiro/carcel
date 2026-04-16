using System.Collections.Generic;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
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

        // ── Campos sociales Phase 2 ────────────────────────────────────────────────

        // Tarea asignada por contrato ganado — HTN físico la ejecuta con prioridad máxima si !FugitiveInVision
        public ContractTask AssignedTask = null;

        // Prioridad de la tarea física actual — determina si el agente acepta bids entrantes
        public TaskPriority CurrentTaskPriority = TaskPriority.Idle;

        // Guardias que forman el equipo activo para la coordinación en curso
        public List<string> TeamMembers = new List<string>();

        // Salidas ya cubiertas por contratos activos — evita subastas duplicadas
        public List<Vector3> CoveredExits = new List<Vector3>();

        // AgentId del guardia que investiga el ruido actual (null = nadie)
        public string NoiseCoveredBy = null;

        // CFP pendiente de respuesta por el HTN social (null si ninguno)
        public ACLMessage? PendingCfp = null;

        // Genera una copia del estado para simulaciones de planificación
        public WorldState Clone() {
            var clone = new WorldState {
                FugitiveInVision      = this.FugitiveInVision,
                PrisonerInCell        = this.PrisonerInCell,
                Energy                = this.Energy,
                CurrentPosition       = this.CurrentPosition,
                LastKnownPosition     = this.LastKnownPosition,
                LastNoisePosition     = this.LastNoisePosition,
                Map                   = this.Map,
                AssignedQuadrantId    = this.AssignedQuadrantId,
                LastKnownPositionTime = this.LastKnownPositionTime,
                LastNoisePositionTime = this.LastNoisePositionTime,
                AgentName             = this.AgentName,
                LastGuardPosition     = this.LastGuardPosition,
                LastGuardPositionTime = this.LastGuardPositionTime,
                // Campos sociales
                AssignedTask          = this.AssignedTask,
                CurrentTaskPriority   = this.CurrentTaskPriority,
                TeamMembers           = new List<string>(this.TeamMembers),
                CoveredExits          = new List<Vector3>(this.CoveredExits),
                NoiseCoveredBy        = this.NoiseCoveredBy,
                PendingCfp            = this.PendingCfp,
            };

            return clone;
        }
    }
}