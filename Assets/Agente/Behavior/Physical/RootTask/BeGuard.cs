using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.RootTask {

    // Tarea principal (RootTask) que define la prioridad de comportamiento del agente.
    public class BeGuard : ICompoundTask {

        public List<IMethod> Methods { get; }

        public BeGuard() {
            Methods = new List<IMethod> {
                new SelectEmergency(),
                new SelectInvestigation(),
                new SelectRoutine()
            };
        }
    }
}