using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Activa GenerateProtocol cuando hay fuga confirmada y no hay subasta ya en curso.
    public class GenerateProtocolMethod : IMethod {
        readonly FIPAAgent _agent;
        readonly float     _replyWindow;

        public GenerateProtocolMethod(FIPAAgent agent, float replyWindow) {
            _agent       = agent;
            _replyWindow = replyWindow;
        }

        public bool CheckPreconditions(WorldState state) =>
            state.FugitiveInVision && !state.ContractNetActive;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new GenerateProtocol(_agent, _replyWindow));
            return q;
        }
    }
}
