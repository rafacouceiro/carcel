using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;

namespace AgenticPrison.Behavior.Methods {

    public class SelectEnergyRecovery : IMethod {
        public bool CheckPreconditions(WorldState state) {
            return true;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            tasks.Enqueue(new EnergyRecoveryTask());
            return tasks;
        }
    }
}