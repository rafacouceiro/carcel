using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    // Tarea compuesta: Prioriza tipos de investigación según los indicios actuales
    public class InvestigationTask : ICompoundTask {
        
        // Orden funcional: Fuga confirmada > Ruido sospechoso > Última posición avistada
        public List<IMethod> Methods { get; }

        public InvestigationTask() {
            Methods = new List<IMethod> {
                new SelectInvestigateEscape(),
                new InvestigateNoiseMethod(),
                new InvestigateLocationMethod()
            };
        }
    }
}