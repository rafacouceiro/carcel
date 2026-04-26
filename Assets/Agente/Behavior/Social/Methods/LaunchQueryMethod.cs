using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Activa LaunchNoiseQueryTask cuando hay un ruido pendiente de verificar y no hay fuga activa.
    // Se sitúa como primer método de BeSocial para tener prioridad sobre el Contract Net.
    public class LaunchQueryMethod : IMethod {

        readonly FIPAAgent _agent;

        public LaunchQueryMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) =>
            state.LastNoisePosition != UnityEngine.Vector3.zero &&
            !state.WaitingForNoiseQuery &&
            !state.FugitiveInVision;   // durante persecución activa no vale la pena preguntar

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new LaunchNoiseQueryTask(_agent));
            return q;
        }
    }
}
