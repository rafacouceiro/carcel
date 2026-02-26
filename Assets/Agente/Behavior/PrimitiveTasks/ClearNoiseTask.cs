using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    public class ClearNoiseTask : IPrimitiveTask {
        public bool CheckPreconditions(WorldState state) => true;

        public void ApplyEffects(WorldState state) {
            state.LastNoisePosition = UnityEngine.Vector3.zero; 
        }
        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            return TaskExecutionStatus.Success;
        }
    }
}