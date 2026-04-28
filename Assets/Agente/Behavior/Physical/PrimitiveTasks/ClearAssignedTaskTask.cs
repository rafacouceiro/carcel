using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    // Tarea primitiva: señaliza que la tarea asignada por contrato ha concluido.
    // En el nuevo diseño GuardBrain detecta la finalización del sweep antes de que esta
    // tarea se ejecute (broadcast al canal perimeter y ForzarReplanificacion), por lo que
    // esta tarea actúa como fallback: limpia AssignedTask si aún no se ha hecho.
    public class ClearAssignedTaskTask : IPrimitiveTask {

        public bool CheckPreconditions(WorldState state) => state.AssignedTask != null;

        public void ApplyEffects(WorldState state) {
            state.AssignedTask = null;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            state.AssignedTask = null;
            return TaskExecutionStatus.Success;
        }
    }
}
