using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Tarea raíz del plano social del guardia.
    // Selecciona cada frame entre: iniciar un protocolo, responder mensajes pendientes, o esperar.
    public class BeSocial : ICompoundTask {
        public List<IMethod> Methods { get; }

        public BeSocial(FIPAAgent agent, float contractNetReplyWindow) {
            Methods = new List<IMethod> {
                new GenerateProtocolMethod(agent, contractNetReplyWindow),
                new SendResponseMethod(agent),
                new SocialIdleMethod()
            };
        }
    }

    // ── GenerateProtocol ───────────────────────────────────────────────────────────
    // Tarea compuesta: decide qué protocolo iniciar según el estado del mundo.

    public class GenerateProtocol : ICompoundTask {
        public List<IMethod> Methods { get; }

        public GenerateProtocol(FIPAAgent agent, float replyWindow) {
            Methods = new List<IMethod> {
                new InitiateContractNetMethod(agent, replyWindow)
            };
        }
    }

    // Activa GenerateProtocol cuando hay fuga confirmada y no hay subasta ya en curso.
    public class GenerateProtocolMethod : IMethod {
        readonly FIPAAgent _agent;
        readonly float     _replyWindow;

        public GenerateProtocolMethod(FIPAAgent agent, float replyWindow) {
            _agent       = agent;
            _replyWindow = replyWindow;
        }

        public bool CheckPreconditions(WorldState state) =>
            state.FugitiveInVision && !state.ContractNetActive;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new GenerateProtocol(_agent, _replyWindow));
            return q;
        }
    }

    // Inicia una subasta Contract Net para coordinar la investigación de habitaciones adyacentes.
    public class InitiateContractNetMethod : IMethod {
        readonly FIPAAgent _agent;
        readonly float     _replyWindow;

        public InitiateContractNetMethod(FIPAAgent agent, float replyWindow) {
            _agent       = agent;
            _replyWindow = replyWindow;
        }

        public bool CheckPreconditions(WorldState state) => state.FugitiveInVision;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new LaunchRoomCfpsTask(_agent, _replyWindow));
            return q;
        }
    }

    // ── SendResponse ───────────────────────────────────────────────────────────────
    // Tarea compuesta: responde al primer mensaje pendiente de la cola.

    public class SendResponse : ICompoundTask {
        public List<IMethod> Methods { get; }

        public SendResponse(FIPAAgent agent) {
            Methods = new List<IMethod> {
                new SendProposeMethod(agent),
                new SendRefuseMethod(agent)
            };
        }
    }

    // Activa SendResponse cuando hay mensajes en la cola que necesitan respuesta.
    public class SendResponseMethod : IMethod {
        readonly FIPAAgent _agent;

        public SendResponseMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) => state.PendingActions.Count > 0;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new SendResponse(_agent));
            return q;
        }
    }

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

    // Rechazar (fallback): hay un CFP pendiente pero las condiciones para proponer no se cumplen.
    public class SendRefuseMethod : IMethod {
        readonly FIPAAgent _agent;

        public SendRefuseMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) =>
            state.PendingActions.Count > 0 &&
            state.PendingActions.Peek().Performative == Performative.Cfp;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new SendRefuseTask(_agent));
            return q;
        }
    }

    // ── Idle ───────────────────────────────────────────────────────────────────────

    // Fallback final: no hay nada que comunicar.
    public class SocialIdleMethod : IMethod {
        public bool CheckPreconditions(WorldState state) => true;

        public Queue<ITask> Decompose(WorldState state) {
            var q = new Queue<ITask>();
            q.Enqueue(new SocialWaitTask());
            return q;
        }
    }

    // Tarea primitiva nula: permite que el HTN social produzca un plan vacío sin bloquear.
    public class SocialWaitTask : IPrimitiveTask {
        public bool CheckPreconditions(WorldState state) => true;
        public void ApplyEffects(WorldState state) { }
        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) =>
            TaskExecutionStatus.Success;
    }
}
