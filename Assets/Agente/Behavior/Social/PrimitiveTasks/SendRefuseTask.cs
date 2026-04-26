using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Protocols.ContractNet;

namespace AgenticPrison.Behavior.Social {

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

            // Delegar al participante ya indexado para que cierre la conversación correctamente
            var participant = _agent.GetProtocol(cfp.ConversationId) as ContractNetParticipant;
            if (participant != null) {
                participant.SendRefuse(_agent, state);
            }

            Debug.Log($"[{state.AgentName}] Refuse enviado a {cfp.Sender}");
            return TaskExecutionStatus.Success;
        }
    }
}
