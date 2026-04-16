using System;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;

namespace AgenticPrison.Agents.Guard.Social {

    // Tarea social: rechaza un CFP enviando Refuse directamente.
    // No requiere protocolo — el Refuse no genera conversación continuada.
    public class SendRefuseTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public SendRefuseTask(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.PendingCfp != null;
        }

        public void ApplyEffects(WorldState state) {
            state.PendingCfp = null;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            ACLMessage cfp = state.PendingCfp.Value;

            var refuse = new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Refuse,
                Sender         = _agent.AgentId,
                Receiver       = cfp.Sender,
                ConversationId = cfp.ConversationId,
                SentAt         = Time.time,
                SenderPosition = state.CurrentPosition
            };
            _agent.Send(refuse);

            state.PendingCfp = null;
            Debug.Log($"[{state.AgentName}] Refuse enviado a {cfp.Sender}");
            return TaskExecutionStatus.Success;
        }
    }
}
