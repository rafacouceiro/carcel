using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Communication {

    // Lado INICIADOR del protocolo Contract Net.
    //
    // Responsabilidad: emitir un CFP (Call For Proposals), recoger propuestas
    // durante una ventana de tiempo, elegir la de menor coste y notificar Accept/Reject.
    //
    // Flujo de estados:
    //   WaitingForProposals  ──[Propose]──► WaitingForProposals  (acumula)
    //   WaitingForProposals  ──[deadline]─► Evaluating ──► AcceptSent
    //   AcceptSent           ──[InformDone]─► Done
    //   AcceptSent           ──[Failure]───► Failed
    //   (cualquier estado)   ──[sin propuestas al deadline]─► Failed
    public class ContractNetInitiator : ICommProtocol {

        // ── Estados ────────────────────────────────────────────────────────────────
        enum State {
            WaitingForProposals,   // esperando respuestas de los participantes
            Evaluating,            // eligiendo al ganador (estado transitorio, dura un tick)
            AcceptSent,            // Accept enviado, esperando confirmación del ganador
            Done,
            Failed
        }

        // ── Tablas de transición ───────────────────────────────────────────────────
        // Cómo leer estas tablas:
        //   _onMessage[(estado, performativa)] = método a llamar cuando llega ese mensaje en ese estado
        //   _onTime[estado]                   = método a llamar en cada tick de tiempo en ese estado
        readonly Dictionary<(State, Performative), Action<ACLMessage, WorldState>> _onMessage
            = new Dictionary<(State, Performative), Action<ACLMessage, WorldState>>();

        readonly Dictionary<State, Action<float, WorldState>> _onTime
            = new Dictionary<State, Action<float, WorldState>>();

        // ── Datos internos ─────────────────────────────────────────────────────────
        State            _state = State.WaitingForProposals;
        FIPAAgent        _agent;
        ContractTask     _task;
        float            _deadline;
        float            _replyByWindow;
        List<ACLMessage> _proposals = new List<ACLMessage>();

        // ── ICommProtocol ──────────────────────────────────────────────────────────
        public string ConversationId { get; private set; }
        public bool   IsComplete     => _state == State.Done || _state == State.Failed;

        // ── Constructor ────────────────────────────────────────────────────────────
        // task:        la tarea que se va a subastar
        // initiatorId: AgentId del guardia que inicia la subasta (para identificación)
        public ContractNetInitiator(ContractTask task, string initiatorId, float replyByWindow) {
            _task          = task;
            _replyByWindow = replyByWindow;
            ConversationId = Guid.NewGuid().ToString();
            BuildTransitions();
        }

        // ── Inicio del protocolo ───────────────────────────────────────────────────
        // Llamado por FIPAAgent.LaunchProtocol justo después de registrar el protocolo.
        // Envía el CFP en broadcast y arranca el temporizador.
        public void Init(FIPAAgent agent, WorldState ws) {
            _agent    = agent;
            _deadline = Time.time + _replyByWindow;

            agent.Broadcast(new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Cfp,
                Sender         = agent.AgentId,
                Receiver       = null,           // broadcast: sin receptor específico
                ConversationId = ConversationId,
                Content        = _task,
                SentAt         = Time.time,
                ReplyBy        = _deadline,
                SenderPosition = ws.CurrentPosition
            });

            ConversationTracker.Instance.Register(ConversationId, agent.AgentId);
            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Cfp,
                $"task={_task.Type} target={_task.Target}");
        }

        // ── Tick por mensaje entrante ──────────────────────────────────────────────
        // FIPAAgent llama a este método cuando llega un mensaje con nuestro ConversationId.
        // La tabla _onMessage decide qué hacer según el estado actual y la performativa.
        public void Tick(ACLMessage msg, WorldState ws) {
            Action<ACLMessage, WorldState> handler;
            if (_onMessage.TryGetValue((_state, msg.Performative), out handler))
                handler(msg, ws);
            // Si la combinación (estado, performativa) no está en la tabla, se ignora el mensaje
        }

        // ── Tick por tiempo ────────────────────────────────────────────────────────
        // FIPAAgent llama a este método en cada frame.
        // La tabla _onTime decide qué hacer según el estado actual.
        public void Tick(float currentTime, WorldState ws) {
            Action<float, WorldState> handler;
            if (_onTime.TryGetValue(_state, out handler))
                handler(currentTime, ws);
        }

        // ── Construcción de la tabla de transiciones ───────────────────────────────
        // Cada entrada conecta una situación (estado + evento) con el método que la maneja.
        // Los métodos están definidos más abajo con su documentación.
        void BuildTransitions() {
            _onMessage[(State.WaitingForProposals, Performative.Propose)] = OnProposalReceived;
            _onMessage[(State.WaitingForProposals, Performative.Refuse)]  = OnRefuseReceived;
            _onMessage[(State.AcceptSent,          Performative.InformDone)] = OnTaskDone;
            _onMessage[(State.AcceptSent,          Performative.Failure)]    = OnTaskFailed;

            _onTime[State.WaitingForProposals] = CheckDeadline;
        }

        // ── Handlers de mensajes ───────────────────────────────────────────────────

        // Un participante acepta el CFP y envía su oferta.
        // Acumulamos todas las propuestas hasta que expire el deadline.
        void OnProposalReceived(ACLMessage msg, WorldState ws) {
            _proposals.Add(msg);
            ConversationTracker.Instance.AddParticipant(ConversationId, msg.Sender);
            FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Propose,
                $"from={msg.Sender} cost={((ProposalContent)msg.Content)?.EstimatedCost:F1}");
        }

        // Un participante rechaza el CFP (demasiado ocupado, sin energía, etc.).
        // Lo ignoramos — ya contaremos con los que sí propusieron.
        void OnRefuseReceived(ACLMessage msg, WorldState ws) { }

        // El ganador confirma que completó la tarea asignada.
        void OnTaskDone(ACLMessage msg, WorldState ws) {

            // Retirar del equipo
            if (ws.TeamMembers.Contains(msg.Sender))
                ws.TeamMembers.Remove(msg.Sender);

            FIPALogger.Log(_agent.AgentId, ConversationId, Performative.InformDone,
                $"from={msg.Sender}");
            ConversationTracker.Instance.SetOutcome(ConversationId, "Done");
            _state = State.Done;
        }

        // El ganador no pudo completar la tarea.
        void OnTaskFailed(ACLMessage msg, WorldState ws) {

            // Retirar del equipo
            if (ws.TeamMembers.Contains(msg.Sender))
                ws.TeamMembers.Remove(msg.Sender);

            FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Failure,
                $"from={msg.Sender}");
            ConversationTracker.Instance.SetOutcome(ConversationId, "Failed");
            _state = State.Failed;
        }

        // ── Handler de tiempo ──────────────────────────────────────────────────────

        // Comprueba cada frame si ya expiró el plazo de recogida de propuestas.
        // Si expiró y hay propuestas, evalúa y acepta la mejor.
        // Si expiró sin propuestas, la subasta falla.
        void CheckDeadline(float currentTime, WorldState ws) {
            if (currentTime < _deadline) return; // todavía dentro de la ventana

            if (_proposals.Count > 0) {
                EvaluateAndAccept(ws);
            } else {
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Failure,
                    "no proposals received");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Failed");
                _state = State.Failed;
            }
        }

        // ── Evaluación y envío de Accept / Reject ──────────────────────────────────

        // Elige la propuesta de menor coste, envía Accept al ganador y Reject al resto.
        void EvaluateAndAccept(WorldState ws) {
            _state = State.Evaluating;

            // Encontrar la propuesta de menor coste entre candidatos que no sean ya del equipo
            ACLMessage winner  = null;
            float      minCost = float.MaxValue;
            foreach (ACLMessage p in _proposals) {
                if (ws.TeamMembers.Contains(p.Sender)) continue; // descartar candidatos ya en equipo
                float c = GetCost(p);
                if (c < minCost) { minCost = c; winner = p; }
            }

            if (winner == null) {
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Failure,
                    "all proposers already in team");
                ConversationTracker.Instance.SetOutcome(ConversationId, "Failed");
                _state = State.Failed;
                return;
            }

            // Enviar Accept al ganador
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

            // Añadir ganador al equipo
            if (!ws.TeamMembers.Contains(winner.Sender))
                ws.TeamMembers.Add(winner.Sender);

            // Enviar Reject a todos los demás
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

            // Retirar perdedores del equipo
            foreach (ACLMessage p in _proposals) {
                if (p.Sender == winner.Sender) continue;
                ws.TeamMembers.Remove(p.Sender);
            }

            ConversationTracker.Instance.UpdateState(ConversationId, "AcceptSent");
            _state = State.AcceptSent;
        }

        // Extrae el coste estimado del contenido de una propuesta.
        // Devuelve MaxValue si el contenido no es válido, para que nunca gane.
        float GetCost(ACLMessage proposal) {
            var content = proposal.Content as ProposalContent;
            return content != null ? content.EstimatedCost : float.MaxValue;
        }
    }
}
