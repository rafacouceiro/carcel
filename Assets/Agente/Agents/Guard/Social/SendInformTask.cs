using AgenticPrison.Core;
using AgenticPrison.Physical;
using UnityEngine;

namespace AgenticPrison.Agents.Guard.Social {

    // Tarea social: envía un Inform genérico (stub — no implementado en esta iteración)
    public class SendInformTask : IPrimitiveTask {

        readonly FIPAAgent _agent;
        readonly string    _informType;

        public SendInformTask(FIPAAgent agent, string informType) {
            _agent      = agent;
            _informType = informType;
        }

        public bool CheckPreconditions(WorldState state) {
            return true;
        }

        public void ApplyEffects(WorldState state) { }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            // TODO: implementar en la siguiente iteración (ruido, disolución de equipo)
            Debug.Log($"[{state.AgentName}] SendInformTask stub: {_informType}");
            return TaskExecutionStatus.Success;
        }
    }
}
