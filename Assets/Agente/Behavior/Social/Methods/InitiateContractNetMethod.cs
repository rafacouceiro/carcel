using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Inicia una subasta Contract Net para coordinar la investigación de habitaciones adyacentes.
    public class InitiateContractNetMethod : IMethod {
        readonly FIPAAgent _agent;
        readonly float     _replyWindow;

        public InitiateContractNetMethod(FIPAAgent agent, float replyWindow) {
            _agent       = agent;
            _replyWindow = replyWindow;
        }

        public bool CheckPreconditions(WorldState state) => state.FugitiveInVision;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new LaunchRoomCfpsTask(_agent, _replyWindow));
            return q;
        }
    }
}
