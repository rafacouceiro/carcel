using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Behavior.Social {

    // Proponer: el mensaje pendiente es un CFP, el guardia no está persiguiendo,
    // tiene energía y no pertenece ya a un equipo activo.
    public class SendProposeMethod : IMethod {
        readonly FIPAAgent _agent;

        public SendProposeMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) =>
            state.PendingActions.Count > 0                                    &&
            state.PendingActions.Peek().Performative == Performative.Cfp      &&
            !state.FugitiveInVision                                           &&
            state.TeamMembers.Count == 0                                      &&
            state.Energy > 15f;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new SendProposeTask(_agent));
            return q;
        }
    }
}
