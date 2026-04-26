using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Activa SendResponse cuando hay mensajes en la cola que necesitan respuesta.
    public class SendResponseMethod : IMethod {
        readonly FIPAAgent _agent;

        public SendResponseMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) => state.PendingActions.Count > 0;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new SendResponse(_agent));
            return q;
        }
    }
}
