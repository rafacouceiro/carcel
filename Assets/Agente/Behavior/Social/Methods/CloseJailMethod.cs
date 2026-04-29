using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;
namespace AgenticPrison.Behavior.Social {

    // Método de selección: barrido completo de la cárcel cuando el sector del fugitivo
    // es desconocido ("[UNK]"). Es el fallback del ContractNetTask — se activa cuando
    // ClosePerimeterMethod no puede identificar un sector concreto.
    public class CloseJailMethod : IMethod {
        readonly FIPAAgent _agent;

        public CloseJailMethod(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveSectorId == "[UNK]" &&
            string.IsNullOrEmpty(state.TeamName) &&
            state.AssignedTask == null &&
            state.ShouldInitiateCnp &&
            !state.PrisonerInCell;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new LaunchJailCfpsTask(_agent));
            return q;
        }
    }
}
