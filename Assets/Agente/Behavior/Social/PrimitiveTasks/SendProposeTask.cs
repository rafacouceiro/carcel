using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Protocols.ContractNet;

namespace AgenticPrison.Behavior.Social {

    // Tarea social: responde a un CFP de la cola con una propuesta.
    // Crea el protocolo participante, envía el Propose y consume el mensaje de PendingActions.
    public class SendProposeTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public SendProposeTask(FIPAAgent agent) { _agent = agent; }

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

            ACLMessage cfp  = state.PendingActions.Dequeue();
            ContractTask task = cfp.Content as ContractTask;

            if (task == null) {
                Debug.LogWarning($"[{state.AgentName}] SendProposeTask: contenido del CFP no es ContractTask");
                return TaskExecutionStatus.Failure;
            }

            // Recuperar el participante ya indexado en FIPAAgent cuando llegó el CFP
            var participant = _agent.GetProtocol(cfp.ConversationId) as ContractNetParticipant;
            if (participant == null) {
                Debug.LogWarning($"[{state.AgentName}] SendProposeTask: participante no encontrado para conversación {cfp.ConversationId}");
                return TaskExecutionStatus.Failure;
            }

            float cost = CalculateNavMeshCost(state.CurrentPosition, task.Target);
            participant.SendPropose(_agent, state, cost);

            Debug.Log($"[{state.AgentName}] Propose enviado a {cfp.Sender} coste={cost:F1}");
            return TaskExecutionStatus.Success;
        }

        float CalculateNavMeshCost(Vector3 from, Vector3 to) {
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path))
                return float.MaxValue;

            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++)
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return length;
        }
    }
}
