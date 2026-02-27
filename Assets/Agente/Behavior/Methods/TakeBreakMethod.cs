using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {
    public class TakeBreakMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            // El agente decide descansar si su fatiga es alta
            return true; 
        }

        public Queue<ITask> Decompose(WorldState state) {
            Queue<ITask> subTasks = new Queue<ITask>();
            subTasks.Enqueue(new ChangeFlashLight(Color.orange));
            subTasks.Enqueue(new TakeBreathTask());
            return subTasks;
        }
    }
}