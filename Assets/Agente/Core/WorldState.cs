using System.Collections.Generic;
using AgenticPrison.Communication;
using UnityEngine;

using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Core {

    // Representa el conocimiento que tiene el agente, tanto interno como del entorno
    public class WorldState {

        // Estado interno
        public string AgentName = string.Empty;
        public Vector3 CurrentPosition = Vector3.zero;
        public float Energy = 100f; // Energía del agente (0 a 100)

        // Memoria visual
        public bool FugitiveInVision = false; // Indica si el fugitivo está a la vista
        public bool seenByMe = false;
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

        // Rol activo durante la operación de sector (solo el líder lo escribe; participantes leen AssignedTask.AssignedRole)
        public AgentRole AssignedRole = AgentRole.None;

        // Sector del fugitivo en la operación activa — detecta cambios para disolver y relanzar
        public string FugitiveSectorId = string.Empty;

        // Número de protocolos CNP de tipo SweepSector activos como iniciador — disolución cuando llega a 0
        public int SweepProtocolsActive = 0;

        // Guardias del equipo activo — impide aceptar nuevos bids mientras se coordina una misión
        public List<string> TeamMembers = new List<string>();

        // true mientras haya al menos un Contract Net activo como iniciador — impide relanzar subastas
        public bool ContractNetActive = false;

        // true mientras un QueryInitiator espera Informs — bloquea InvestigateNoiseMethod durante la ventana
        public bool WaitingForNoiseQuery = false;

        // Cola de mensajes entrantes que requieren una decisión del HTN social.
        // Escrita por OnMessageReceived, consumida (Dequeue) por los efectos de las tareas sociales.
        public Queue<ACLMessage> PendingActions = new Queue<ACLMessage>();

        // Genera una copia del estado para simulaciones de planificación
        public WorldState Clone() {
            var clone = new WorldState {
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
                SweepProtocolsActive  = this.SweepProtocolsActive,
                TeamMembers           = new List<string>(this.TeamMembers),
                ContractNetActive     = this.ContractNetActive,
                WaitingForNoiseQuery  = this.WaitingForNoiseQuery,
                PendingActions        = new Queue<ACLMessage>(this.PendingActions),
            };

            return clone;
        }
    }
}