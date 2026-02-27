using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    public class RoutineTask : ICompoundTask {
        
        public List<IMethod> Methods { get; }

        public RecoverEnergy() {
            Methods = new List<IMethod> {
                new GuardKeySpot(),
                new TakeBreakMethod()
            };
        }
    }
}
