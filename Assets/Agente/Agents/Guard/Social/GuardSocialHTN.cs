using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Agents.Guard.Social {

    // Tarea raíz del plano social del guardia.
    // Selecciona el comportamiento comunicativo apropiado cada frame.
    public class BeSocial : ICompoundTask {
        public List<IMethod> Methods { get; }

        public BeSocial(FIPAAgent agent, float contractNetReplyWindow) {
            Methods = new List<IMethod> {
                new CoordinateFlightMethod(agent, contractNetReplyWindow),
                new RespondToBidMethod(agent),
                new RefuseBidMethod(agent),
                new SocialIdleMethod()
            };
        }
    }

    // ── Métodos ────────────────────────────────────────────────────────────────────

    // Cuando el guardia ve al fugitivo y aún no tiene equipo coordinado:
    // lanza subastas para que otros cubran las habitaciones adyacentes.
    public class CoordinateFlightMethod : IMethod {
        readonly FIPAAgent _agent;
        readonly float     _replyByWindow;
        public CoordinateFlightMethod(FIPAAgent agent, float replyByWindow) {
            _agent         = agent;
            _replyByWindow = replyByWindow;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision && state.TeamMembers.Count == 0 && !state.ContractNetActive;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            tasks.Enqueue(new LaunchRoomCfpsTask(_agent, _replyByWindow));
            return tasks;
        }
    }

    // Cuando hay un CFP pendiente y el guardia puede aceptar:
    // no está persiguiendo, tiene energía y la prioridad de la tarea supera la actual.
    public class RespondToBidMethod : IMethod {
        readonly FIPAAgent _agent;
        public RespondToBidMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) {
            if (state.PendingCfp == null) return false;
            if (state.FugitiveInVision)   return false;
            if (state.Energy <= 15f)      return false;

            ContractTask offered = state.PendingCfp.Value.Content as ContractTask;
            if (offered == null) return false;

            return offered.Priority > state.CurrentTaskPriority;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            tasks.Enqueue(new SendProposeTask(_agent));
            return tasks;
        }
    }

    // Fallback: si hay un CFP pendiente pero las condiciones de respuesta no se cumplen,
    // rechazar siempre para liberar el slot.
    public class RefuseBidMethod : IMethod {
        readonly FIPAAgent _agent;
        public RefuseBidMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) {
            return state.PendingCfp != null;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            tasks.Enqueue(new SendRefuseTask(_agent));
            return tasks;
        }
    }

    // Fallback final: no hay nada que comunicar, el agente descansa socialmente.
    public class SocialIdleMethod : IMethod {
        public bool CheckPreconditions(WorldState state) { return true; }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            tasks.Enqueue(new SocialWaitTask());
            return tasks;
        }
    }

    // Tarea primitiva nula: no hace nada, permite que el HTN social produzca un plan vacío.
    public class SocialWaitTask : IPrimitiveTask {
        public bool CheckPreconditions(WorldState state) { return true; }
        public void ApplyEffects(WorldState state) { }
        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            return TaskExecutionStatus.Success;
        }
    }
}
