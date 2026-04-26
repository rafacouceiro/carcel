using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Tarea social: lanza un QueryInitiator para averiguar si el ruido reciente vino de un compañero.
    // Bloquea InvestigateNoiseMethod durante la ventana de espera (WaitingForNoiseQuery = true).
    public class LaunchNoiseQueryTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public LaunchNoiseQueryTask(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) =>
            state.LastNoisePosition != Vector3.zero && !state.WaitingForNoiseQuery;

        // Efecto optimista: bloquea la investigación mientras llega la respuesta
        public void ApplyEffects(WorldState state) {
            state.WaitingForNoiseQuery = true;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            if (state.LastNoisePosition == Vector3.zero) return TaskExecutionStatus.Failure;

            var query = new QueryInitiator(state.LastNoisePosition);
            _agent.LaunchProtocol(query, state);

            // ApplyEffects lo hace en el clon del planificador; aquí sobre el estado real
            state.WaitingForNoiseQuery = true;

            Debug.Log($"<color=cyan>[{state.AgentName}] Query lanzado para ruido en {state.LastNoisePosition}</color>");
            return TaskExecutionStatus.Success;
        }
    }
}
