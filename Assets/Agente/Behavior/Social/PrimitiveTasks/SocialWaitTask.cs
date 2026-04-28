using AgenticPrison.Core;

namespace AgenticPrison.Behavior.Social {

    // Tarea primitiva nula: permite que el HTN social produzca un plan vacío sin bloquear.
    public class SocialWaitTask : IPrimitiveTask {
        public bool CheckPreconditions(WorldState state) => true;
        public void ApplyEffects(WorldState state) { }
        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) =>
            TaskExecutionStatus.Success;
    }
}