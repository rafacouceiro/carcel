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
            // No iniciar si ya estamos en un equipo o ejecutando una tarea asignada por contrato
            if (state.TeamMembers.Count > 0 || state.AssignedTask != null)
                return false;
            return state.FugitiveInVision;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new LaunchRoomCfpsTask(_agent, _replyWindow));
            return q;
        }
    }
}
