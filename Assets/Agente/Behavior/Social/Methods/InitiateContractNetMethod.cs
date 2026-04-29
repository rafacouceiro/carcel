using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Puerta de entrada al ContractNetTask. Se activa cuando hay motivo para iniciar
    // una subasta: fugitivo en sector conocido O sector marcado como "[UNK]".
    // ContractNetTask decide internamente entre closePerimeter y closeJail.
    public class InitiateContractNetMethod : IMethod {
        readonly FIPAAgent _agent;

        public InitiateContractNetMethod(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) {
            if (!string.IsNullOrEmpty(state.TeamName)) return false;
            if (state.AssignedTask != null) return false;

            // Caso [UNK]: barrido global necesario tras un barrido de sector fallido
            if (state.FugitiveSectorId == "[UNK]" && !state.PrisonerInCell) return true;

            // Caso perimeter: fugitivo visto en un sector concreto
            if (!state.seenByMe || state.LastKnownPosition == UnityEngine.Vector3.zero) return false;
            var sectors = state.Map?.GetFugitiveSectors(state.LastKnownPosition);
            return sectors != null && sectors.Count == 1;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new ContractNetTask(_agent));  // compound — se descompone más en closePerimeter/closeJail
            return q;
        }
    }
}
