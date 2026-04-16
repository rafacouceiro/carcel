using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Communication {

    // Protocolo Contract Net como FSM con tabla de transiciones.
    // Sin ifs ni switch-on-strings en las transiciones — todo por delegates indexados por (estado, performativa).
    // Soporta los roles de iniciador y participante en la misma clase.
    public class ContractNetProtocol : ICommProtocol {

        // ── Estados ────────────────────────────────────────────────────────────────
        enum CnpState {
            Idle,
            // Iniciador
            CfpSent, Collecting, Evaluating, AcceptSent,
            // Participante
            CfpReceived, Proposed, Refused, Executing,
            // Terminales
            Done, Failed
        }

        public enum CnpRole { Initiator, Participant }

        // ── Tablas de transición ───────────────────────────────────────────────────
        // (estado, performativa) → acción al recibir mensaje
        readonly Dictionary<(CnpState, Performative), Action<ACLMessage, WorldState>> _onMessage
            = new Dictionary<(CnpState, Performative), Action<ACLMessage, WorldState>>();

        // estado → acción al avanzar por tiempo
        readonly Dictionary<CnpState, Action<float, WorldState>> _onTime
            = new Dictionary<CnpState, Action<float, WorldState>>();

        // ── Estado interno ─────────────────────────────────────────────────────────
        CnpState   _state = CnpState.Idle;
        CnpRole    _role;
        FIPAAgent  _agent;

        // Datos del iniciador
        ContractTask               _task;           // tarea que se subasta
        float                      _deadline;       // Time.time límite de recogida de propuestas
        List<ACLMessage>           _proposals       = new List<ACLMessage>();
        List<string>               _respondents     = new List<string>(); // todos los que recibieron cfp

        // Datos del participante
        ACLMessage  _originalCfp;   // cfp original para responder con el ConversationId correcto

        const float REPLY_BY_WINDOW = 0.5f; // segundos para recoger propuestas

        // ── Identificador de conversación ──────────────────────────────────────────
        public string ConversationId { get; private set; }
        public bool   IsComplete     => _state == CnpState.Done || _state == CnpState.Failed;

        // ── Constructor iniciador ──────────────────────────────────────────────────
        public ContractNetProtocol(ContractTask task, string initiatorId) {
            _role           = CnpRole.Initiator;
            _task           = task;
            ConversationId  = Guid.NewGuid().ToString();
            BuildTransitions();
        }

        // ── Constructor participante ───────────────────────────────────────────────
        public ContractNetProtocol(ACLMessage cfp, string participantId) {
            _role          = CnpRole.Participant;
            _originalCfp   = cfp;
            ConversationId = cfp.ConversationId; // mismo id que el iniciador para enrutado correcto
            BuildTransitions();
        }

        // ── Init ───────────────────────────────────────────────────────────────────
        public void Init(FIPAAgent agent, WorldState ws) {
            _agent = agent;

            if (_role == CnpRole.Initiator) {
                _deadline = Time.time + REPLY_BY_WINDOW;

                ACLMessage cfp = new ACLMessage {
                    MessageId      = Guid.NewGuid().ToString(),
                    Performative   = Performative.Cfp,
                    Sender         = agent.AgentId,
                    Receiver       = null,
                    ConversationId = ConversationId,
                    Content        = _task,
                    SentAt         = Time.time,
                    ReplyBy        = _deadline,
                    SenderPosition = ws.CurrentPosition
                };

                agent.Broadcast(cfp);
                ConversationTracker.Instance.Register(ConversationId, agent.AgentId);
                FIPALogger.Log(agent.AgentId, ConversationId, Performative.Cfp,
                    $"task={_task.Type} target={_task.Target}");
                Transition(CnpState.CfpSent);

            } else {
                // Participante: no envía nada aquí — la tarea social decide propose o refuse
                Transition(CnpState.CfpReceived);
            }
        }

        // ── Tick por mensaje ───────────────────────────────────────────────────────
        public void Tick(ACLMessage msg, WorldState ws) {
            var key = (_state, msg.Performative);
            Action<ACLMessage, WorldState> handler;
            if (_onMessage.TryGetValue(key, out handler))
                handler(msg, ws);
        }

        // ── Tick por tiempo ────────────────────────────────────────────────────────
        public void Tick(float currentTime, WorldState ws) {
            Action<float, WorldState> handler;
            if (_onTime.TryGetValue(_state, out handler))
                handler(currentTime, ws);
        }

        // ── Tabla de transiciones ──────────────────────────────────────────────────
        void BuildTransitions() {

            // ── Iniciador ──────────────────────────────────────────────────────────

            // CfpSent: acumular propuestas
            _onMessage[(CnpState.CfpSent, Performative.Propose)] = (msg, ws) => {
                _proposals.Add(msg);
                ConversationTracker.Instance.AddParticipant(ConversationId, msg.Sender);
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Propose,
                    $"from={msg.Sender} cost={((ProposalContent)msg.Content)?.EstimatedCost:F1}");
                Transition(CnpState.Collecting);
            };

            // Collecting: seguir acumulando propuestas mientras llegan
            _onMessage[(CnpState.Collecting, Performative.Propose)] = (msg, ws) => {
                _proposals.Add(msg);
                ConversationTracker.Instance.AddParticipant(ConversationId, msg.Sender);
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Propose,
                    $"from={msg.Sender} cost={((ProposalContent)msg.Content)?.EstimatedCost:F1}");
            };

            // Refuse se descarta silenciosamente en ambos estados de recogida
            _onMessage[(CnpState.CfpSent, Performative.Refuse)]      = (msg, ws) => { };
            _onMessage[(CnpState.Collecting, Performative.Refuse)]    = (msg, ws) => { };

            // Transición por deadline: CfpSent/Collecting → Evaluating o Failed
            Action<float, WorldState> checkDeadline = (t, ws) => {
                if (t < _deadline) return;
                if (_proposals.Count > 0)
                    EvaluateAndAccept(ws);
                else {
                    FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Failure,
                        "no proposals received");
                    ConversationTracker.Instance.SetOutcome(ConversationId, "Failed");
                    Transition(CnpState.Failed);
                }
            };
            _onTime[CnpState.CfpSent]    = checkDeadline;
            _onTime[CnpState.Collecting] = checkDeadline;

            // AcceptSent: esperar InformDone o Failure del ganador
            _onMessage[(CnpState.AcceptSent, Performative.InformDone)] = (msg, ws) => {
                // El participante completó la tarea — actualizar estado social
                if (!ws.TeamMembers.Contains(msg.Sender))
                    ws.TeamMembers.Add(msg.Sender);
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.InformDone,
                    $"from={msg.Sender}");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
                Transition(CnpState.Done);
            };

            _onMessage[(CnpState.AcceptSent, Performative.Failure)] = (msg, ws) => {
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Failure,
                    $"from={msg.Sender}");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Failed");
                Transition(CnpState.Failed);
            };

            // ── Participante ───────────────────────────────────────────────────────

            // Proposed: esperar Accept o Reject del iniciador
            _onMessage[(CnpState.Proposed, Performative.AcceptProposal)] = (msg, ws) => {
                // Ganar la subasta: asignar la tarea al WorldState para que BeGuard la ejecute
                ContractTask won = _task ?? (ContractTask)_originalCfp.Content;
                won.InitiatorId  = _originalCfp.Sender; // para que InformDoneTask sepa a quién informar
                ws.AssignedTask  = won;
                ws.PendingCfp    = null;
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.AcceptProposal,
                    $"task assigned: {ws.AssignedTask?.Type}");
                ConversationTracker.Instance.UpdateState(ConversationId, "Executing");
                Transition(CnpState.Executing);
            };

            _onMessage[(CnpState.Proposed, Performative.RejectProposal)] = (msg, ws) => {
                ws.PendingCfp = null;
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.RejectProposal,
                    "proposal rejected");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
                Transition(CnpState.Done);
            };

            // Executing: cuando InformDoneTask notifica al protocolo que terminó
            _onMessage[(CnpState.Executing, Performative.InformDone)] = (msg, ws) => {
                ws.AssignedTask = null;
                ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
                Transition(CnpState.Done);
            };
        }

        // ── API pública para el participante ──────────────────────────────────────

        // Llamado por SendProposeTask: envía Propose al iniciador y transiciona a Proposed
        public void SendPropose(FIPAAgent agent, WorldState ws, float cost) {
            if (_role != CnpRole.Participant || _state != CnpState.CfpReceived) return;

            var content = new ProposalContent { EstimatedCost = cost, ExecutorId = agent.AgentId };
            var propose = new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Propose,
                Sender         = agent.AgentId,
                Receiver       = _originalCfp.Sender,
                ConversationId = ConversationId,
                Content        = content,
                SentAt         = Time.time,
                ReplyBy        = _originalCfp.ReplyBy,
                SenderPosition = ws.CurrentPosition
            };
            agent.Send(propose);
            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Propose,
                $"to={_originalCfp.Sender} cost={cost:F1}");
            Transition(CnpState.Proposed);
        }

        // ── Evaluación y envío de Accept/Reject ────────────────────────────────────
        void EvaluateAndAccept(WorldState ws) {
            Transition(CnpState.Evaluating);

            // Elegir la propuesta de menor coste
            ACLMessage winner = _proposals[0];
            float minCost = GetCost(winner);
            foreach (ACLMessage p in _proposals) {
                float c = GetCost(p);
                if (c < minCost) { minCost = c; winner = p; }
            }

            // Enviar Accept al ganador
            ACLMessage accept = new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.AcceptProposal,
                Sender         = _agent.AgentId,
                Receiver       = winner.Sender,
                ConversationId = ConversationId,
                Content        = _task,
                SentAt         = Time.time,
                SenderPosition = ws.CurrentPosition
            };
            _agent.Send(accept);
            FIPALogger.Log(_agent.AgentId, ConversationId, Performative.AcceptProposal,
                $"winner={winner.Sender} cost={minCost:F1}");

            // Enviar Reject al resto
            foreach (ACLMessage p in _proposals) {
                if (p.Sender == winner.Sender) continue;
                ACLMessage reject = new ACLMessage {
                    MessageId      = Guid.NewGuid().ToString(),
                    Performative   = Performative.RejectProposal,
                    Sender         = _agent.AgentId,
                    Receiver       = p.Sender,
                    ConversationId = ConversationId,
                    SentAt         = Time.time,
                    SenderPosition = ws.CurrentPosition
                };
                _agent.Send(reject);
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.RejectProposal,
                    $"to={p.Sender}");
            }

            ConversationTracker.Instance.UpdateState(ConversationId, "AcceptSent");
            Transition(CnpState.AcceptSent);
        }

        float GetCost(ACLMessage proposal) {
            var content = proposal.Content as ProposalContent;
            return content != null ? content.EstimatedCost : float.MaxValue;
        }

        void Transition(CnpState next) {
            _state = next;
            ConversationTracker.Instance.UpdateState(ConversationId, next.ToString());
        }
    }
}
