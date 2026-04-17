using System;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;

namespace AgenticPrison.Agents.Guard.Social {

    // Tarea social: rechaza el primer CFP de la cola enviando Refuse al iniciador.
    // El método padre (SendRefuseMethod) ya garantiza que el mensaje es un CFP.
    public class SendRefuseTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public SendRefuseTask(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) =>
            state.PendingActions.Count > 0 &&
            state.PendingActions.Peek().Performative == Performative.Cfp;

        // Consume el mensaje de la cola — el planificador ve el efecto durante la simulación
        public void ApplyEffects(WorldState state) {
            if (state.PendingActions.Count > 0)
                state.PendingActions.Dequeue();
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            if (state.PendingActions.Count == 0) return TaskExecutionStatus.Failure;

            ACLMessage cfp = state.PendingActions.Dequeue();

            _agent.Send(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Refuse,
                Sender         = _agent.AgentId,
                Receiver       = cfp.Sender,
                ConversationId = cfp.ConversationId,
                SentAt         = Time.time,
                SenderPosition = state.CurrentPosition
            });

            Debug.Log($"[{state.AgentName}] Refuse enviado a {cfp.Sender}");
            return TaskExecutionStatus.Success;
        }
    }
}
