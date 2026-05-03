using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols.Query;

namespace AgenticPrison.Behavior.Social {

    // Pregunta a los compañeros si estaban cerca del ruido antes de ir a investigarlo.
    // Si alguien responde, GuardBrain descarta el ruido como falsa alarma.
    public class LaunchNoiseQueryTask : IPrimitiveTask {

        const float GUARD_THRESHOLD = 25f;

        readonly FIPAAgent _agent;

        public LaunchNoiseQueryTask(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(GuardWorldState state) =>
            state.LastNoisePosition != Vector3.zero && !state.WaitingForNoiseQuery;

        public void ApplyEffects(GuardWorldState state) {
            state.WaitingForNoiseQuery = true;
        }

        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) {
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
