using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Tarea compuesta: selecciona la estrategia de contrato según el estado del mundo.
    // Métodos ordenados por prioridad:
    //   1. ClosePerimeterMethod — cierra el sector donde se avistó al fugitivo
    //   2. CloseJailMethod      — barre toda la cárcel cuando el sector es "[UNK]"
    public class ContractNetTask : ICompoundTask {
        public List<IMethod> Methods { get; }

        public ContractNetTask(FIPAAgent agent) {
            Methods = new List<IMethod> {
                new ClosePerimeterMethod(agent),
                new CloseJailMethod(agent)
            };
        }
    }
}
