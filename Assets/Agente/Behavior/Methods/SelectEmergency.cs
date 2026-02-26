using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;

namespace AgenticPrison.Behavior.Methods {

    public class SelectEmergency : IMethod {
        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision && state.Fatigue < 0.8f;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            tasks.Enqueue(new EmergencyTask()); // El Planner recibirá esto y volverá a descomponer
            return tasks;
        }
    }
}