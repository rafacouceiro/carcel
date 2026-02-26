using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    public class SelectInvestigation : IMethod {
        public bool CheckPreconditions(WorldState state) {
            return state.LastKnownPosition != Vector3.zero;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            tasks.Enqueue(new InvestigationTask()); 
            return tasks;
        }
    }
}