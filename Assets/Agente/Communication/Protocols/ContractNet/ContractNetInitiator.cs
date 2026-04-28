using System;
using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols;

namespace AgenticPrison.Communication.Protocols.ContractNet {

    // Lado INICIADOR del protocolo Contract Net.
    //
    // Responsabilidad: emitir un CFP (Call For Proposals), recoger propuestas
    // durante una ventana de tiempo, elegir la de menor coste y notificar Accept/Reject.
    //
    // Flujo de estados (simplificado — sin fase inform-done):
    //   WaitingForProposals  ──[Propose]──► WaitingForProposals  (acumula)
    //   WaitingForProposals  ──[deadline]─► Evaluating ──► Done
    //   WaitingForProposals  ──[deadline, sin propuestas]─► Failed
    public class ContractNetInitiator : ICommProtocol {

        // ── Estados ────────────────────────────────────────────────────────────────
        enum State {
            WaitingForProposals,   // esperando respuestas de los participantes
            Evaluating,            // eligiendo al ganador (estado transitorio, dura un tick)
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
        CfpContent       _content;
        float            _deadline;
        float            _replyByWindow;
        List<ACLMessage> _proposals = new List<ACLMessage>();

        // ── ICommProtocol ──────────────────────────────────────────────────────────
        public string ConversationId { get; private set; }
        public bool   IsComplete     => _state == State.Done || _state == State.Failed;


        // ── Constructor ────────────────────────────────────────────────────────────
        public ContractNetInitiator(CfpContent content, float replyByWindow) {
            _content       = content;
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
                Content        = _content,
                SentAt         = Time.time,
                ReplyBy        = _deadline
            });

            ConversationTracker.Instance.Register(ConversationId, agent.AgentId);
            FIPALogger.Log(agent.AgentId, ConversationId, Performative.Cfp,
                $"task={_content.Task.Type} target={_content.Task.Target}");
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

            _onTime[State.WaitingForProposals] = CheckDeadline;
        }

        // ── Handlers de mensajes ───────────────────────────────────────────────────

        // Un participante acepta el CFP y envía su oferta.
        // Acumulamos todas las propuestas hasta que expire el deadline.
        void OnProposalReceived(ACLMessage msg, WorldState ws) {
            _proposals.Add(msg);
            ConversationTracker.Instance.AddParticipant(ConversationId, msg.Sender);
            FIPALogger.Log(_agent.AgentId, ConversationId, Performative.Propose,
                $"from={msg.Sender} cost={(float)msg.Content:F1}");
        }

        // Un participante rechaza el CFP (demasiado ocupado, sin energía, etc.).
        // Lo ignoramos — ya contaremos con los que sí propusieron.
        void OnRefuseReceived(ACLMessage msg, WorldState ws) { }

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

            // Encontrar la propuesta de menor coste
            ACLMessage winner    = default;
            bool       hasWinner = false;
            float      minCost   = float.MaxValue;
            foreach (ACLMessage p in _proposals) {
                float c = GetCost(p);
                if (c < minCost) { minCost = c; winner = p; hasWinner = true; }
            }

            if (!hasWinner) {
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
                Content        = _content.Task,
                SentAt         = Time.time
            });
            FIPALogger.Log(_agent.AgentId, ConversationId, Performative.AcceptProposal,
                $"winner={winner.Sender} cost={minCost:F1}");

            // Enviar Reject a todos los demás
            foreach (ACLMessage p in _proposals) {
                if (p.Sender == winner.Sender) continue;
                _agent.Send(new ACLMessage {
                    MessageId      = Guid.NewGuid().ToString(),
                    Performative   = Performative.RejectProposal,
                    Sender         = _agent.AgentId,
                    Receiver       = p.Sender,
                    ConversationId = ConversationId,
                    SentAt         = Time.time
                });
                FIPALogger.Log(_agent.AgentId, ConversationId, Performative.RejectProposal,
                    $"to={p.Sender}");
            }

            ConversationTracker.Instance.UpdateState(ConversationId, "Done");
            _state = State.Done; // CNP termina al asignar la tarea; la ejecución se gestiona por canales
        }

        // Extrae el coste estimado del contenido de una propuesta.
        // Devuelve MaxValue si el contenido no es válido, para que nunca gane.
        float GetCost(ACLMessage proposal) {
            if (proposal.Content is float cost) return cost;
            return float.MaxValue;
        }
    }
}
