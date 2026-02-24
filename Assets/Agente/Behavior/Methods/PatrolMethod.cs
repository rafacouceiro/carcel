using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class PatrolMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            // Fatiga menor a 0.9 y prisionero en la celda
            return state.Fatigue < 0.9f && state.PrisonerInCell;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            subTasks.Enqueue(new PatrolPrimitiveTask());
            return subTasks;
        }
    }
}