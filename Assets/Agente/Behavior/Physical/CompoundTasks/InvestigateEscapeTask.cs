using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;

namespace AgenticPrison.Behavior.CompoundTasks {

    // Tarea compuesta: Lógica de búsqueda activa tras confirmar una fuga
    public class InvestigateEscapeTask : ICompoundTask {
        
        // Realiza un peinado sistemático de la zona cuando no hay datos recientes del fugitivo
        public List<IMethod> Methods { get; }

        public InvestigateEscapeTask() {
            Methods = new List<IMethod> {
                new SocialInvestigationMethod(),
                new WideSweepMethod()
            };
        }
    }
}