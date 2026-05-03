using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    // Tarea primitiva: cierre del contrato de sweep.
    // Dispara OnSweepCompleted antes de limpiar AssignedTask para que el suscriptor
    // (GuardBrain.CheckSweepCompletion) pueda leer TeamName y AssignedRole.
    public class ClearAssignedTaskTask : IPrimitiveTask {

        public bool CheckPreconditions(GuardWorldState state) => state.AssignedTask != null;

        public void ApplyEffects(GuardWorldState state) {
            state.AssignedTask = null;
        }

        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) {
            state.OnSweepCompleted?.Invoke();
            state.AssignedTask = null;
            return TaskExecutionStatus.Success;
        }
    }
}
