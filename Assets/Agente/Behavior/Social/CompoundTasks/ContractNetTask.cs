using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Decide entre cerrar el perímetro de un sector conocido o barrer toda la cárcel si el
    // sector es "[UNK]". ClosePerimeterMethod tiene prioridad sobre CloseJailMethod.
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
