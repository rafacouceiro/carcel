using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.PrimitiveTasks;
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    public class ChaseMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            subTasks.Enqueue(new ChangeFlashLight(Color.red));
            subTasks.Enqueue(new ChaseTask(6.5f));
            return subTasks;
        }
    }
}