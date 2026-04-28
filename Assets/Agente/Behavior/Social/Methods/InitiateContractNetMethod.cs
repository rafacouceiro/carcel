using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Inicia una subasta Contract Net para coordinar la investigación de habitaciones adyacentes.
    public class InitiateContractNetMethod : IMethod {
        readonly FIPAAgent _agent;

        public InitiateContractNetMethod(FIPAAgent agent) {
            _agent       = agent;
        }

        public bool CheckPreconditions(WorldState state)
        {
            // Bloquear si hay operación activa (ContractNetActive) o el agente ya tiene equipo (TeamName)
            if (state.ContractNetActive || !string.IsNullOrEmpty(state.TeamName)) return false;
            if (state.AssignedTask != null) return false;
            if (!state.seenByMe || state.LastKnownPosition == UnityEngine.Vector3.zero) return false;

            var sectors = state.Map?.GetFugitiveSectors(state.LastKnownPosition);
            if (sectors == null || sectors.Count != 1) return false;

            // Solo lanzar si el sector es nuevo o nunca se ha perimetrado
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
