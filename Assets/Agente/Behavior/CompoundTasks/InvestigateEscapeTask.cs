using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    public class InvestigateEscapeTask : ICompoundTask {
        
        public List<IMethod> Methods { get; }

        public InvestigateEscapeTask() {
            Methods = new List<IMethod> {
                new PredictivePursuitMethod(),
                new WideSweepMethod()
            };
        }
    }
}