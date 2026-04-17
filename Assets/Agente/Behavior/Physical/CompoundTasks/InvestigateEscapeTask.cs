using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    // Tarea compuesta: Lógica de búsqueda activa tras confirmar una fuga
    public class InvestigateEscapeTask : ICompoundTask {
        
        // Intenta predecir su destino según pistas o realiza un peinado de la zona
        public List<IMethod> Methods { get; }

        public InvestigateEscapeTask() {
            Methods = new List<IMethod> {
                new PredictivePursuitMethod(),
                new WideSweepMethod()
            };
        }
    }
}