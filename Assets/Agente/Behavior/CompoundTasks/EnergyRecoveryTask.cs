using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    public class EnergyRecoveryTask : ICompoundTask {
        
        public List<IMethod> Methods { get; }

        public EnergyRecoveryTask() {
            Methods = new List<IMethod> {
                new GuardKeySpotMethod(),
                new TakeBreakMethod()
            };
        }
    }
}
