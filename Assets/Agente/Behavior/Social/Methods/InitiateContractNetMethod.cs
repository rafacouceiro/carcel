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

        public bool CheckPreconditions(WorldState state)
        {
            // Bloquear si hay operación activa (ContractNetActive) o el agente ya tiene equipo (TeamName)
            if (state.ContractNetActive || !string.IsNullOrEmpty(state.TeamName)) return false;
            if (state.AssignedTask != null) return false;
            if (!state.seenByMe || state.LastKnownPosition == UnityEngine.Vector3.zero) return false;

            var sectors = state.Map?.GetFugitiveSectors(state.LastKnownPosition);
            if (sectors == null || sectors.Count != 1) return false;

            // No relanzar si el fugitivo sigue en el sector ya perimetrado.
            // La comprobación usa FugitiveSectorId ANTES de que LaunchSectorCfpsTask lo actualice.
            if (!string.IsNullOrEmpty(state.FugitiveSectorId) && sectors[0] == state.FugitiveSectorId)
                return false;

            return true;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new LaunchSectorCfpsTask(_agent, _replyWindow));
            return q;
        }
    }
}
