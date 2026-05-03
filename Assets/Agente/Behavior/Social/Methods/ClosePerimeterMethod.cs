using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Activa LaunchSectorCfpsTask cuando se avistó al fugitivo en un sector concreto.
    public class ClosePerimeterMethod : IMethod {
        readonly FIPAAgent _agent;

        public ClosePerimeterMethod(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(GuardWorldState state) {
            if (!string.IsNullOrEmpty(state.TeamName)) return false;
            if (state.AssignedTask != null) return false;
            if (!state.seenByMe || state.LastKnownPosition == UnityEngine.Vector3.zero) return false;
            if (string.IsNullOrEmpty(state.FugitiveSectorId) || state.Map == null || !state.Map.GetAvailableSectors().Contains(state.FugitiveSectorId))
                return false;

            // No relanzar para el mismo sector
            if (!string.IsNullOrEmpty(state.PerimeteredSectorId) && state.FugitiveSectorId == state.PerimeteredSectorId)
                return false;

            return true;
        }

        public Queue<ITask> Decompose(GuardWorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new LaunchSectorCfpsTask(_agent));
            return q;
        }
    }
}
