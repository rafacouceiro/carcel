using AgenticPrison.Core;
using System.Collections.Generic;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {
    
    public class PatrolMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            return true; // Por ahora, la patrulla siempre es válida
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            subTasks.Enqueue(new ChoosePatrolZoneTask()); // 1. Decide a dónde ir
            subTasks.Enqueue(new PatrolZoneTask());       // 2. Ve allí y recorre los puntos
            
            return subTasks;
        }
    }
}