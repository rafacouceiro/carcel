using System.Collections.Generic;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Physical;
using UnityEngine;

namespace AgenticPrison.Core {

    // Estado del mundo del guardia. Extiende WorldState con percepción física,
    // navegación y coordinación social propias del rol de guardia.
    public class GuardWorldState : WorldState {

        // Estado interno
        public Vector3 CurrentPosition = Vector3.zero;
        public float Energy = 100f; // Energía del agente (0 a 100)

        // Memoria visual
        public bool FugitiveInVision = false; // Indica si el fugitivo está a la vista
        // seenByMe, LastKnownPosition, LastKnownPositionTime → heredados de WorldState

        // Memoria sobre otros agentes
        public Vector3 LastGuardPosition = Vector3.zero;
        public float LastGuardPositionTime = 0f;
        
        // Memoria auditiva
        public Vector3 LastNoisePosition = Vector3.zero; // Origen del último ruido sospechoso
        public float LastNoisePositionTime = 0f; // Instante del último ruido

        // PrisonerInCell → heredado de WorldState

        // Navegación
        public PrisonMap Map; // Referencia al mapa de la prisión
        public string AssignedQuadrantId = string.Empty; // Zona de patrulla asignada

        // ── Campos sociales xs─────────

        // Si es true, el agente quiere iniciar un CNP
        public bool ShouldInitiateCnp = false;

        // Tarea asignada por contrato ganado — HTN físico la ejecuta con prioridad máxima si !FugitiveInVision
        public ContractTask AssignedTask = null;

        // Rol activo durante la operación de sector (solo el líder lo escribe; participantes leen AssignedTask.AssignedRole)
        public AgentRole AssignedRole = AgentRole.None;

        // FugitiveSectorId, PerimeteredSectorId → heredados de WorldState

        // Nombre del equipo activo — identifica el canal de coordinación (team_<teamName>)
        public string TeamName = string.Empty;

        // Sweepers pendientes de completar su tarea — solo el iniciador lo escribe; disolución cuando llega a 0
        public int PendingSweepersCount = 0;

        // true mientras un QueryInitiator espera Informs — bloquea InvestigateNoiseMethod durante la ventana
        public bool WaitingForNoiseQuery = false;

        // PendingCfps → heredado de WorldState

        // Callback que se invoca cuando el Sweeper termina todas sus salas asignadas.
        // No se clona — es comportamiento del agente, no estado del mundo.
        public System.Action OnSweepCompleted;

        // Genera una copia del estado para simulaciones de planificación
        public GuardWorldState Clone() {
            var clone = new GuardWorldState {
                FugitiveInVision      = this.FugitiveInVision,
                seenByMe              = this.seenByMe,
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
                // Clone() profundo para que la simulación HTN no mute SweepRooms real
                AssignedTask          = this.AssignedTask?.Clone(),
                AssignedRole          = this.AssignedRole,
                FugitiveSectorId      = this.FugitiveSectorId,
                PerimeteredSectorId   = this.PerimeteredSectorId,
                TeamName              = this.TeamName,
                PendingSweepersCount  = this.PendingSweepersCount,
                ShouldInitiateCnp     = this.ShouldInitiateCnp,
                WaitingForNoiseQuery  = this.WaitingForNoiseQuery,
                PendingCfps           = new Queue<ContractTask>(this.PendingCfps)
            };

            return clone;
        }
    }
}