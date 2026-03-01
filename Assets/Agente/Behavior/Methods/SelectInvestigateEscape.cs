using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    public class SelectInvestigateEscape : IMethod {
        public bool CheckPreconditions(WorldState state) {
            return state.LastKnownPosition != Vector3.zero && !state.PrisonerInCell && (Time.time - state.LastKnownPositionTime) < 25f;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            tasks.Enqueue(new InvestigateEscapeTask()); 
            return tasks;
        }
    }
}