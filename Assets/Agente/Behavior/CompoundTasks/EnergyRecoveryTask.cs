using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    // Tarea compuesta: Aborda la recuperación de estamina del guardia
    public class EnergyRecoveryTask : ICompoundTask {
        
        // Prioriza hacer guardia en un punto estratégico, o tomar un descanso donde está
        public List<IMethod> Methods { get; }

        public EnergyRecoveryTask() {
            Methods = new List<IMethod> {
                new GuardKeySpotMethod(),
                new TakeBreakMethod()
            };
        }
    }
}
