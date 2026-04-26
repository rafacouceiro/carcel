using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Tarea compuesta: responde al primer mensaje pendiente de la cola.
    public class SendResponse : ICompoundTask {
        public List<IMethod> Methods { get; }

        public SendResponse(FIPAAgent agent) {
            Methods = new List<IMethod> {
                new SendProposeMethod(agent),
                new SendRefuseMethod(agent)
            };
        }
    }
}
