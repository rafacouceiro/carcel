using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class TrapMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            distance = GetDistanceToTarget(state.CurrentPosition, state.LastKnownPosition);
            return state.FugitiveInVision && distance < 1.5f;
        }

        private float GetDistanceToTarget(Vector3 agentPosition, Vector3 targetPosition) {
            return Vector3.Distance(agentPosition, targetPosition);
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            subTasks.Enqueue(new GameOverTask());
            return subTasks;
        }
    }
}