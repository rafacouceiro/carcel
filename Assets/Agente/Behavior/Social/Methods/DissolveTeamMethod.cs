using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Behavior.Social {

    // Método social: activa la disolución del equipo cuando todos los sweepers han terminado.
    // Solo el líder (AssignedRole == Sweeper) puede disolver; los blockers esperan su InformDone.
    public class DissolveTeamMethod : IMethod {

        readonly FIPAAgent _agent;

        public DissolveTeamMethod(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.SweepProtocolsActive == 0
                && state.TeamMembers.Count > 0
                && state.AssignedRole == AgentRole.Sweeper;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new DissolveTeamTask(_agent));
            return q;
        }
    }
}
