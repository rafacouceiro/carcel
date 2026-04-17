using System;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;

namespace AgenticPrison.Agents.Guard.Physical {

    // Tarea física: notifica al iniciador del contrato que la tarea ha sido completada.
    // Se ejecuta al final del plan generado por DecomposeAssignedTask en BeGuard.
    public class InformDoneTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public InformDoneTask(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.AssignedTask != null;
        }

        public void ApplyEffects(WorldState state) {
            state.AssignedTask        = null;
            state.CurrentTaskPriority = TaskPriority.Idle;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            if (state.AssignedTask == null) return TaskExecutionStatus.Success;

            var informDone = new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.InformDone,
                Sender         = _agent.AgentId,
                Receiver       = state.AssignedTask.InitiatorId,
                ConversationId = state.AssignedTask.ContractId,
                SentAt         = Time.time,
                SenderPosition = state.CurrentPosition
            };
            _agent.Send(informDone);

            FIPALogger.Log(_agent.AgentId, state.AssignedTask.ContractId,
                Performative.InformDone, $"to={state.AssignedTask.InitiatorId}");

            state.AssignedTask        = null;
            state.CurrentTaskPriority = TaskPriority.Idle;

            Debug.Log($"[{state.AgentName}] InformDone enviado a {informDone.Receiver}");
            return TaskExecutionStatus.Success;
        }
    }
}
