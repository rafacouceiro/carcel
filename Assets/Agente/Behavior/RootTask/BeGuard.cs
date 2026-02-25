using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.RootTask {

    public class BeGuard : ICompoundTask {
        
        public List<IMethod> Methods { get; }

        public BeGuard() {
            Methods = new List<IMethod> {
                new EmergencySelector(),
                new InvestigationSelector(),
                new RoutineSelector()
            };
        }
    }
}