using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Communication;

using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Behavior.Social {

    // Tarea social: el líder-sweeper disuelve el equipo cuando todos los sweepers han terminado.
    // Envía InformDone a cada blocker para que limpien su AssignedTask y vuelvan a rutina.
    public class DissolveTeamTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public DissolveTeamTask(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) {
            // SweepProtocolsActive == 0 cubre tanto a sweepers externos como al propio líder
            // (ClearAssignedTaskTask ya decrementó el contador al acabar el sweep del líder)
            return state.SweepProtocolsActive == 0
                && state.TeamMembers.Count > 0
                && state.AssignedRole == AgentRole.Sweeper;
        }

        public void ApplyEffects(WorldState state) {
            state.TeamMembers.Clear();
            state.AssignedRole      = AgentRole.None;
            state.FugitiveSectorId  = string.Empty;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            // Copiar la lista antes de iterar para evitar modificar mientras se recorre
            List<string> members = new List<string>(state.TeamMembers);

            foreach (string member in members) {
                _agent.Send(new ACLMessage {
                    MessageId      = Guid.NewGuid().ToString(),
                    Performative   = Performative.InformDone,
                    Sender         = _agent.AgentId,
                    Receiver       = member,
                    ConversationId = string.Empty,  // sin conversación activa — GuardBrain lo recoge
                    SentAt         = Time.time
                });
                FIPALogger.Log(_agent.AgentId, string.Empty, Performative.InformDone,
                    $"dissolve → {member}");
            }

            state.TeamMembers.Clear();
            state.AssignedRole     = AgentRole.None;
            state.FugitiveSectorId = string.Empty;

            Debug.Log($"<color=orange>[{state.AgentName}] DissolveTeamTask: equipo disuelto ({members.Count} blocker/s notificados)</color>");
            return TaskExecutionStatus.Success;
        }
    }
}
