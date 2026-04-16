using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Communication {

    // Protocolo Contract Net como FSM con tabla de transiciones.
    // El mismo objeto soporta el rol de iniciador y de participante.
    //
    // Flujo iniciador:  Idle → WaitingForProposals → Evaluating → AcceptSent → Done/Failed
    // Flujo participante: Idle → CfpReceived → Proposed → Executing → Done
    public class ContractNetProtocol : ICommProtocol {

        // ── Estados ────────────────────────────────────────────────────────────────
        enum State {
            Idle,
            // Iniciador
            WaitingForProposals,    // espera propuestas hasta el deadline
            Evaluating,             // elige al ganador
            AcceptSent,             // espera confirmación del ganador
            // Participante
            CfpReceived,            // cfp recibido, pendiente de respuesta
            Proposed,               // propuesta enviada, esperando Accept/Reject
            Executing,              // tarea asignada, ejecutando
            // Terminales
            Done,
            Failed
        }

        public enum CnpRole { Initiator, Participant }

        // ── Tablas de transición ───────────────────────────────────────────────────
        // Mensaje recibido: (estado actual, performativa) → acción
        readonly Dictionary<(State, Performative), Action<ACLMessage, WorldState>> _onMessage
            = new Dictionary<(State, Performative), Action<ACLMessage, WorldState>>();

        // Avance por tiempo: estado actual → acción
        readonly Dictionary<State, Action<float, WorldState>> _onTime
            = new Dictionary<State, Action<float, WorldState>>();

        // ── Estado interno ─────────────────────────────────────────────────────────
        State      _state = State.Idle;
        CnpRole    _role;
        FIPAAgent  _agent;

        // Datos del iniciador
        ContractTask     _task;
        float            _deadline;
        List<ACLMessage> _proposals = new List<ACLMessage>();

        // Datos del participante
        ACLMessage _originalCfp;

        const float REPLY_BY_WINDOW = 0.5f; // segundos de ventana para recoger propuestas

        // ── API pública ────────────────────────────────────────────────────────────
        public string ConversationId { get; private set; }
        public bool   IsComplete     => _state == State.Done || _state == State.Failed;

        // ── Constructores ──────────────────────────────────────────────────────────

        // Iniciador: recibe la tarea a subastar
        public ContractNetProtocol(ContractTask task, string initiatorId) {
            _role          = CnpRole.Initiator;
            _task          = task;
            ConversationId = Guid.NewGuid().ToString();
            BuildTransitions();
        }

        // Participante: recibe el CFP original para responder con el mismo ConversationId
        public ContractNetProtocol(ACLMessage cfp, string participantId) {
            _role          = CnpRole.Participant;
            _originalCfp   = cfp;
            ConversationId = cfp.ConversationId;
            BuildTransitions();
        }

        // ── Ciclo de vida ──────────────────────────────────────────────────────────

        public void Init(FIPAAgent agent, WorldState ws) {
            _agent = agent;

            if (_role == CnpRole.Initiator) {
                _deadline = Time.time + REPLY_BY_WINDOW;

                agent.Broadcast(new ACLMessage {
                    MessageId      = Guid.NewGuid().ToString(),
                    Performative   = Performative.Cfp,
                    Sender         = agent.AgentId,
                    Receiver       = null,
                    ConversationId = ConversationId,
                    Content        = _task,
                    SentAt         = Time.time,
                    ReplyBy        = _deadline,
                    SenderPosition = ws.CurrentPosition
                });

                ConversationTracker.Instance.Register(ConversationId, agent.AgentId);
                FIPALogger.Log(agent.AgentId, ConversationId, Performative.Cfp,
                    $"task={_task.Type} target={_task.Target}");
                Transition(State.WaitingForProposals);

            } else {
                Transition(State.CfpReceived);
            }
        }

        // Tick por mensaje entrante
        public void Tick(ACLMessage msg, WorldState ws) {
            Action<ACLMessage, WorldState> handler;
            if (_onMessage.TryGetValue((_state, msg.Performative), out handler))
                handler(msg, ws);
        }

        // Tick por tiempo (deadlines)
        public void Tick(float currentTime, WorldState ws) {
            Action<float, WorldState> handler;
            if (_onTime.TryGetValue(_state, out handler))
                handler(currentTime, ws);
        }

        // ── Tabla de transiciones ──────────────────────────────────────────────────
        void BuildTransitions() {

            // ── INICIADOR ──────────────────────────────────────────────────────────

            // WaitingForProposals: acumular propuestas y rechazos hasta el deadline
            _onMessage[(State.WaitingForProposals, Performative.Propose)] = (msg, ws) => {
                _proposals.Add(msg);
                ConversationTracker.Instance.AddParticipant(ConversationId, msg.Sender);
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Propose,
                    $"from={msg.Sender} cost={((ProposalContent)msg.Content)?.EstimatedCost:F1}");
            };

            _onMessage[(State.WaitingForProposals, Performative.Refuse)] = (msg, ws) => { };

            // Deadline alcanzado: evaluar si hay propuestas o fallar
            _onTime[State.WaitingForProposals] = (t, ws) => {
                if (t < _deadline) return;

                if (_proposals.Count > 0)
                    EvaluateAndAccept(ws);
                else {
                    FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Failure,
                        "no proposals received");
                    ConversationTracker.Instance.SetOutcome(ConversationId, "Failed");
                    Transition(State.Failed);
                }
            };

            // AcceptSent: esperar confirmación del ganador
            _onMessage[(State.AcceptSent, Performative.InformDone)] = (msg, ws) => {
                if (!ws.TeamMembers.Contains(msg.Sender))
                    ws.TeamMembers.Add(msg.Sender);
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.InformDone,
                    $"from={msg.Sender}");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
                Transition(State.Done);
            };

            _onMessage[(State.AcceptSent, Performative.Failure)] = (msg, ws) => {
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Failure,
                    $"from={msg.Sender}");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Failed");
                Transition(State.Failed);
            };

            // ── PARTICIPANTE ───────────────────────────────────────────────────────

            // Proposed: esperar Accept o Reject del iniciador
            _onMessage[(State.Proposed, Performative.AcceptProposal)] = (msg, ws) => {
                ContractTask won = _task ?? (ContractTask)_originalCfp.Content;
                won.InitiatorId = _originalCfp.Sender;
                ws.AssignedTask = won;
                ws.PendingCfp   = null;
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.AcceptProposal,
                    $"task assigned: {ws.AssignedTask?.Type}");
                ConversationTracker.Instance.UpdateState(ConversationId, "Executing");
                Transition(State.Executing);
            };

            _onMessage[(State.Proposed, Performative.RejectProposal)] = (msg, ws) => {
                ws.PendingCfp = null;
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.RejectProposal,
                    "proposal rejected");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
                Transition(State.Done);
            };

            // Executing: InformDone notifica al protocolo que la tarea terminó
            _onMessage[(State.Executing, Performative.InformDone)] = (msg, ws) => {
                ws.AssignedTask = null;
                ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
                Transition(State.Done);
            };
        }

        // ── API pública para el participante ───────────────────────────────────────

        // Llamado por SendProposeTask: envía Propose al iniciador y pasa a Proposed
        public void SendPropose(FIPAAgent agent, WorldState ws, float cost) {
            if (_role != CnpRole.Participant || _state != State.CfpReceived) return;

            agent.Send(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Propose,
                Sender         = agent.AgentId,
                Receiver       = _originalCfp.Sender,
                ConversationId = ConversationId,
                Content        = new ProposalContent { EstimatedCost = cost, ExecutorId = agent.AgentId },
                SentAt         = Time.time,
                ReplyBy        = _originalCfp.ReplyBy,
                SenderPosition = ws.CurrentPosition
            });
            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Propose,
                $"to={_originalCfp.Sender} cost={cost:F1}");
            Transition(State.Proposed);
        }

        // ── Evaluación ─────────────────────────────────────────────────────────────

        void EvaluateAndAccept(WorldState ws) {
            Transition(State.Evaluating);

            // Propuesta de menor coste gana
            ACLMessage winner = _proposals[0];
            float minCost = GetCost(winner);
            foreach (ACLMessage p in _proposals) {
                float c = GetCost(p);
                if (c < minCost) { minCost = c; winner = p; }
            }

            // Accept al ganador
            _agent.Send(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.AcceptProposal,
                Sender         = _agent.AgentId,
                Receiver       = winner.Sender,
                ConversationId = ConversationId,
                Content        = _task,
                SentAt         = Time.time,
                SenderPosition = ws.CurrentPosition
            });
            FIPALogger.Log(_agent.AgentId, ConversationId, Performative.AcceptProposal,
                $"winner={winner.Sender} cost={minCost:F1}");

            // Reject al resto
            foreach (ACLMessage p in _proposals) {
                if (p.Sender == winner.Sender) continue;
                _agent.Send(new ACLMessage {
                    MessageId      = Guid.NewGuid().ToString(),
                    Performative   = Performative.RejectProposal,
                    Sender         = _agent.AgentId,
                    Receiver       = p.Sender,
                    ConversationId = ConversationId,
                    SentAt         = Time.time,
                    SenderPosition = ws.CurrentPosition
                });
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.RejectProposal,
                    $"to={p.Sender}");
            }

            ConversationTracker.Instance.UpdateState(ConversationId, "AcceptSent");
            Transition(State.AcceptSent);
        }

        float GetCost(ACLMessage proposal) {
            var content = proposal.Content as ProposalContent;
            return content != null ? content.EstimatedCost : float.MaxValue;
        }

        void Transition(State next) {
            _state = next;
            ConversationTracker.Instance.UpdateState(ConversationId, next.ToString());
        }
    }
}
