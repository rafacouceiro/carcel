using System.Collections.Generic;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Physical;
using UnityEngine;

namespace AgenticPrison.Core {

    // Estado del mundo del guardia. Todo lo que necesita para percibir,
    // moverse y coordinarse con otros agentes está aquí.
    public class GuardWorldState : WorldState {

        public Vector3 CurrentPosition = Vector3.zero;
        public float   Energy          = 100f;

        // Visión
        public bool    FugitiveInVision = false;
        // seenByMe, LastKnownPosition, LastKnownPositionTime vienen de WorldState

        // Última vez que vimos a otro guardia — sirve para filtrar ruidos falsos
        public Vector3 LastGuardPosition     = Vector3.zero;
        public float   LastGuardPositionTime = 0f;

        // Audición
        public Vector3 LastNoisePosition     = Vector3.zero;
        public float   LastNoisePositionTime = 0f;

        // PrisonerInCell viene de WorldState

        public PrisonMap Map;
        public string    AssignedQuadrantId = string.Empty;

        // ── Coordinación ──────────────────────────────────────────────────────────

        // El HTN lo activa cuando quiere iniciar un CNP pero no puede hacerlo él solo
        public bool ShouldInitiateCnp = false;

        // Tarea que ganamos en una subasta. Si hay una, el HTN físico la ejecuta
        // antes que cualquier otra cosa (salvo que veamos al fugitivo en directo).
        public ContractTask AssignedTask = null;

        // Solo el líder del equipo escribe esto; los demás leen AssignedTask.AssignedRole
        public AgentRole AssignedRole = AgentRole.None;

        // FugitiveSectorId y PerimeteredSectorId vienen de WorldState

        public string TeamName           = string.Empty;
        public int    PendingSweepersCount = 0;

        // Mientras esperamos respuestas al QueryIf, bloqueamos InvestigateNoiseMethod
        public bool WaitingForNoiseQuery = false;

        // PendingCfps viene de WorldState

        // No se clona — es el callback del agente, no parte del estado del mundo
        public System.Action OnSweepCompleted;

        // Copia para que el planificador HTN pueda simular sin tocar el estado real.
        // AssignedTask se clona en profundidad porque SweepRooms se va vaciando.
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
