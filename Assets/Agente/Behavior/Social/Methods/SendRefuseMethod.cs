using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Behavior.Social {

    // Rechazar (fallback): hay un CFP pendiente pero las condiciones para proponer no se cumplen.
    public class SendRefuseMethod : IMethod {
        readonly FIPAAgent _agent;

        public SendRefuseMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) =>
            state.PendingActions.Count > 0 &&
            state.PendingActions.Peek().Performative == Performative.Cfp;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new SendRefuseTask(_agent));
            return q;
        }
    }
}
