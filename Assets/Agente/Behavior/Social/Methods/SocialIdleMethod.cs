using System.Collections.Generic;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.Social {

    // Fallback final: no hay nada que comunicar.
    public class SocialIdleMethod : IMethod {
        public bool CheckPreconditions(GuardWorldState state) => true;

        public Queue<ITask> Decompose(GuardWorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new SocialWaitTask());
            return q;
        }
    }
}
