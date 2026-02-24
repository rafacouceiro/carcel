using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class ChaseMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision && state.Fatigue < 0.75f && !state.PrisonerInCell;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            subTasks.Enqueue(new MoveTask(state.LastKnownPosition, 5f));
            return subTasks;
        }
    }
}