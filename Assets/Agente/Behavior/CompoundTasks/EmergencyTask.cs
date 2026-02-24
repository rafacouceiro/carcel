using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    public class EmergencyTask : ICompoundTask {
        
        public List<IMethod> Methods { get; }

        public EmergencyTask() {
            Methods = new List<IMethod> {
                new TrapMethod(),
                new ChaseMethod()
            };
        }
    }
}