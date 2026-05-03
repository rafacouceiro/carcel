using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    // Tarea primitiva: Limpia el registro mental del origen de un ruido tras ser investigado
    public class ClearNoiseTask : IPrimitiveTask {
        public bool CheckPreconditions(GuardWorldState state) => true;

        // Limpieza de memoria auditiva en simulación
        public void ApplyEffects(GuardWorldState state) {
            state.LastNoisePosition = UnityEngine.Vector3.zero;
        }

        // Limpieza de memoria auditiva real
        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) {
            state.LastNoisePosition = UnityEngine.Vector3.zero;
            return TaskExecutionStatus.Success;
        }
    }
}