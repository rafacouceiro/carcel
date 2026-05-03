using AgenticPrison.Core;

namespace AgenticPrison.Behavior.Social {

    // Tarea primitiva nula: permite que el HTN social produzca un plan vacío sin bloquear.
    public class SocialWaitTask : IPrimitiveTask {
        public bool CheckPreconditions(GuardWorldState state) => true;
        public void ApplyEffects(GuardWorldState state) { }
        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) =>
            TaskExecutionStatus.Success;
    }
}