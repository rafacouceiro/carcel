using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Activa LaunchNoiseQueryTask cuando hay un ruido pendiente de verificar y no hay fuga activa.
    // Solo dispara si el ruido es reciente (< QUERY_WINDOW s): pasado ese tiempo el Query ya completó
    // y el flag WaitingForNoiseQuery garantiza que no se relanza durante la ventana activa.
    public class LaunchQueryMethod : IMethod {

        const float QUERY_WINDOW = 0.3f; // debe coincidir con QueryInitiator.QUERY_WINDOW

        readonly FIPAAgent _agent;

        public LaunchQueryMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) =>
            state.LastNoisePosition != Vector3.zero &&
            !state.WaitingForNoiseQuery &&
            (Time.time - state.LastNoisePositionTime) < QUERY_WINDOW &&
            !state.FugitiveInVision;   // durante persecución activa no vale la pena preguntar

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new LaunchNoiseQueryTask(_agent));
            return q;
        }
    }
}
