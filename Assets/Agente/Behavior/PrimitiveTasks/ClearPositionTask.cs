using AgenticPrison.Core;
using UnityEngine;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    public class ClearPositionTask : IPrimitiveTask {
        public bool CheckPreconditions(WorldState state) => true;

        public void ApplyEffects(WorldState state) {
            state.LastKnownPosition = Vector3.zero; 
        }
        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            state.LastKnownPosition = Vector3.zero;
            return TaskExecutionStatus.Success;
        }
    }
}