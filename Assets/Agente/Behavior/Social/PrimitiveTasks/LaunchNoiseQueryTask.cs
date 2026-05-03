using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols.Query;

namespace AgenticPrison.Behavior.Social {

    // Tarea social: lanza un QueryIfInitiator para averiguar si el ruido reciente vino de un compañero.
    // Construye el contenido e inyecta en el protocolo — la respuesta la procesa GuardBrain.HandleInform.
    // Bloquea InvestigateNoiseMethod durante la ventana de espera (WaitingForNoiseQuery = true).
    public class LaunchNoiseQueryTask : IPrimitiveTask {

        const float GUARD_THRESHOLD = 25f;

        readonly FIPAAgent _agent;

        public LaunchNoiseQueryTask(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) =>
            state.LastNoisePosition != Vector3.zero && !state.WaitingForNoiseQuery;

        public void ApplyEffects(WorldState state) {
            state.WaitingForNoiseQuery = true;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            if (state.LastNoisePosition == Vector3.zero) return TaskExecutionStatus.Failure;

            var content = new QueryIfContent {
                NoisePosition = state.LastNoisePosition,
                Threshold     = GUARD_THRESHOLD
            };

            _agent.LaunchProtocol(new QueryIfInitiator(content), state);
            state.WaitingForNoiseQuery = true;

            Debug.Log($"<color=cyan>[{state.AgentName}] QueryIf lanzado para ruido en {state.LastNoisePosition}</color>");
            return TaskExecutionStatus.Success;
        }
    }
}
