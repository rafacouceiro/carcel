using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    public class InvestigationTask : ICompoundTask {
        
        public List<IMethod> Methods { get; }

        public InvestigationTask() {
            Methods = new List<IMethod> {
                new InvestigateEscapeMethod(),
                new InvestigateLocationMethod()
            };
        }
    }
}