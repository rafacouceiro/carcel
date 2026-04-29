using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Método de selección: cierra el perímetro del sector donde está el fugitivo.
    // Lógica extraída de InitiateContractNetMethod — mismas precondiciones, misma tarea.
    public class ClosePerimeterMethod : IMethod {
        readonly FIPAAgent _agent;

        public ClosePerimeterMethod(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) {
            if (state.ContractNetActive || !string.IsNullOrEmpty(state.TeamName)) return false;
            if (state.AssignedTask != null) return false;
            if (!state.seenByMe || state.LastKnownPosition == UnityEngine.Vector3.zero) return false;

            var sectors = state.Map?.GetFugitiveSectors(state.LastKnownPosition);
            if (sectors == null || sectors.Count != 1) return false;

            // No relanzar para el mismo sector
            if (!string.IsNullOrEmpty(state.PerimeteredSectorId) && sectors[0] == state.PerimeteredSectorId)
                return false;

            return true;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new LaunchSectorCfpsTask(_agent));
            return q;
        }
    }
}
