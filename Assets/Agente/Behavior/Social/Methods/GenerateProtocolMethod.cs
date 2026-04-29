using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Activa GenerateProtocol cuando hay fuga confirmada y no hay subasta ya en curso.
    public class GenerateProtocolMethod : IMethod {
        readonly FIPAAgent _agent;

        public GenerateProtocolMethod(FIPAAgent agent) {
            _agent       = agent;
        }

        public bool CheckPreconditions(WorldState state) =>
            (state.FugitiveInVision && string.IsNullOrEmpty(state.TeamName) && state.AssignedTask == null) ||
            (state.FugitiveSectorId == "[UNK]" && string.IsNullOrEmpty(state.TeamName) &&
             state.AssignedTask == null && !state.PrisonerInCell) ||
            (state.LastNoisePosition != UnityEngine.Vector3.zero && !state.WaitingForNoiseQuery);

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new GenerateProtocol(_agent));
            return q;
        }
    }
}

