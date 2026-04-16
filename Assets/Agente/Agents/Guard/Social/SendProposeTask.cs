using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;

namespace AgenticPrison.Agents.Guard.Social {

    // Tarea social: responde a un CFP con una propuesta usando coste NavMesh como bid.
    // Crea el protocolo participante y envía el Propose al iniciador.
    public class SendProposeTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public SendProposeTask(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.PendingCfp != null;
        }

        public void ApplyEffects(WorldState state) {
            // Efecto optimista: limpia el CFP pendiente
            state.PendingCfp = null;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            ACLMessage cfp = state.PendingCfp.Value;
            ContractTask task = cfp.Content as ContractTask;

            if (task == null) {
                Debug.LogWarning($"[{state.AgentName}] SendProposeTask: contenido del CFP no es ContractTask");
                state.PendingCfp = null;
                return TaskExecutionStatus.Failure;
            }

            // Calcular coste como longitud del camino NavMesh hasta el objetivo
            float cost = CalculateNavMeshCost(state.CurrentPosition, task.Target);

            // Crear protocolo participante con el mismo ConversationId que el iniciador
            var protocol = new ContractNetProtocol(cfp, _agent.AgentId);
            _agent.LaunchProtocol(protocol, state);  // Init → CfpReceived

            // Enviar Propose a través del protocolo
            protocol.SendPropose(_agent, state, cost);

            state.PendingCfp = null;
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
