using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;

namespace AgenticPrison.Behavior.Methods {

    public class InvestigationSelector : IMethod {
        public bool CheckPreconditions(WorldState state) {
            return state.Alertness && !state.FugitiveInVision; // Estar en alerta pero no ver al fugitivo
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            tasks.Enqueue(new InvestigationTask()); 
            return tasks;
        }
    }
}