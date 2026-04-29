using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols;
using AgenticPrison.Communication.Protocols.ContractNet;
using AgenticPrison.Communication.Protocols.Query;

namespace AgenticPrison.Communication {

    // Base abstracta de todo agente FIPA. Gestiona el enrutado por conversación
    // y ofrece hooks estandarizados para que las subclases reaccionen a mensajes.
    public abstract class FIPAAgent : MonoBehaviour {

        const int BUFFER_SIZE = 16;
        readonly ACLMessage[] _buffer = new ACLMessage[BUFFER_SIZE];
        int _head;
        int _tail;
        int _count;
        
        [SerializeField] protected float _replyByWindow = 3f;

        // Protocolos activos con estado (GUID)
        readonly Dictionary<string, ICommProtocol> _ongoing_conversations = new Dictionary<string, ICommProtocol>();
        const int MAX_CONVERSATIONS = 8;

        static readonly Dictionary<string, FIPAAgent> _agents = new Dictionary<string, FIPAAgent>();
        static readonly Dictionary<string, HashSet<string>> _channels = new Dictionary<string, HashSet<string>>();

        public abstract string AgentId { get; }

        protected virtual void Start() {
            _agents[AgentId] = this;
        }

        protected virtual void Update() {
            DiscardExpired();
        }

        public void ReceiveMessage(ACLMessage msg) {
            if (_count == BUFFER_SIZE) {
                _head = (_head + 1) % BUFFER_SIZE;
                _count--;
            }
            _buffer[_tail] = msg;
            _tail = (_tail + 1) % BUFFER_SIZE;
            _count++;
        }

        public void LaunchProtocol(ICommProtocol protocol, WorldState ws) {
            if (_ongoing_conversations.Count >= MAX_CONVERSATIONS) return;
            _ongoing_conversations[protocol.ConversationId] = protocol;
            protocol.Init(this, ws);
        }

        public ICommProtocol GetProtocol(string convId) {
            ICommProtocol proto;
            return _ongoing_conversations.TryGetValue(convId, out proto) ? proto : null;
        }

        // ── Motor de Procesamiento Unificado ───────────────────────────────────────

        protected void ProcessIncoming(WorldState ws, int maxPerFrame = 5) {
            TickConversations(ws);
            ProcessBuffer(ws, maxPerFrame);
            ProcessPendingCfps(ws);
        }

        private void TickConversations(WorldState ws) {
            var convIds = new List<string>(_ongoing_conversations.Keys);
            foreach (string id in convIds) {
                if (!_ongoing_conversations.ContainsKey(id)) continue;
                _ongoing_conversations[id].Tick(Time.time, ws);
                if (_ongoing_conversations[id].IsComplete) _ongoing_conversations.Remove(id);
            }
        }

        private void ProcessBuffer(WorldState ws, int maxPerFrame) {
            int processed = 0;
            int bufferSnapshot = _count;
            int readPos = _head;
            var deferred = new List<ACLMessage>();

            // Pasada 1: Conversaciones activas
            for (int i = 0; i < bufferSnapshot && processed < maxPerFrame; i++) {
                int pos = (readPos + i) % BUFFER_SIZE;
                ACLMessage msg = _buffer[pos];

                if (IsExpired(msg)) continue;

                if (!string.IsNullOrEmpty(msg.ConversationId) && _ongoing_conversations.ContainsKey(msg.ConversationId)) {
                    _ongoing_conversations[msg.ConversationId].Tick(msg, ws);
                    if (_ongoing_conversations[msg.ConversationId].IsComplete) _ongoing_conversations.Remove(msg.ConversationId);
                    OnMessageReceived(msg, ws); // permite que el agente reaccione al resultado del protocolo
                    RemoveFromBuffer(pos);
                    processed++;
                } else {
                    deferred.Add(msg);
                }
            }

            // Pasada 2: Nuevas conversaciones y Notificaciones sueltas
            foreach (ACLMessage msg in deferred) {
                if (processed >= maxPerFrame) break;
                if (IsExpired(msg)) { RemoveFromBuffer(FindInBuffer(msg)); continue; }

                // Intentar iniciar protocolo si tiene ID y es una performativa de inicio
                bool startedProtocol = false;
                if (!string.IsNullOrEmpty(msg.ConversationId)) {
                    startedProtocol = HandlePotentialNewProtocol(msg, ws);
                }

                // En cualquier caso, el agente reacciona al mensaje
                OnMessageReceived(msg, ws);

                RemoveFromBuffer(FindInBuffer(msg)); 
                processed++;
            }
        }

        private bool HandlePotentialNewProtocol(ACLMessage msg, WorldState ws) {
            if (_ongoing_conversations.Count >= MAX_CONVERSATIONS) return false;

            if (msg.Performative == Performative.Cfp) {
                var p = new ContractNetParticipant(msg, AgentId);
                p.Init(this, ws);
                // Evaluar primero; registrar solo si propuso (IsComplete=false tras Refuse)
                OnCfpReceived(msg, ws, p);
                if (!p.IsComplete) _ongoing_conversations[p.ConversationId] = p;
                return true;
            } else if (msg.Performative == Performative.Query) {
                var p = new QueryParticipant(msg, AgentId);
                _ongoing_conversations[p.ConversationId] = p;
                p.Init(this, ws);
                return true;
            }
            return false;
        }

        // Hook para que las subclases decidan cómo reaccionar a la subasta
        protected virtual void OnCfpReceived(ACLMessage msg, WorldState ws, ContractNetParticipant participant) {
            float cost;
            if (EvaluateCfp(msg, ws, out cost))
                participant.SendPropose(this, ws, cost);
            else
                participant.SendRefuse(this, ws);
        }

        // ── Handlers Virtuales (Reaction Hooks) ────────────────────────────────────

        protected virtual void OnMessageReceived(ACLMessage msg, WorldState ws) {
            switch (msg.Performative) {
                case Performative.Inform:     HandleInform(msg, ws); break;
                case Performative.InformDone: HandleInformDone(msg, ws); break;
                case Performative.Cfp:        HandleCfp(msg, ws); break;
                case Performative.Cancel:     HandleCancel(msg, ws); break;
                default:                      HandleDefault(msg, ws); break;
            }
        }

        protected virtual void HandleInform(ACLMessage msg, WorldState ws) {
            // Sincronización base: avistamientos fugitivo
            if (msg.Content is FugitiveSightingContent sighting) {
                ws.PrisonerInCell = false;

                if (sighting.SectorId == "[UNK]") {
                    // Señal de barrido fallido: sector desconocido, no sobreescribir LastKnownPosition
                    ws.FugitiveSectorId    = "[UNK]";
                    ws.PerimeteredSectorId = string.Empty; // permite futuras operaciones
                    FIPALogger.Log(AgentId, "radio", Performative.Inform, "sweep failed — sector [UNK]");
                } else if (sighting.Timestamp > ws.LastKnownPositionTime) {
                    ws.LastKnownPosition     = sighting.Position;
                    ws.LastKnownPositionTime = sighting.Timestamp;
                    ws.FugitiveSectorId      = sighting.SectorId;
                    FIPALogger.Log(AgentId, "radio", Performative.Inform, $"Fugitive at {sighting.SectorId}");
                }
            }
        }

        // Hook para que las subclases evalúen reactivamente si proponer o rechazar un CFP.
        // Devuelve true = proponer con el coste calculado; false = rechazar.
        protected virtual bool EvaluateCfp(ACLMessage cfp, WorldState ws, out float cost) {
            cost = 0f; return false;
        }

        // Hooks para subclases
        protected virtual void HandleCfp(ACLMessage msg, WorldState ws) { }
        protected virtual void HandleInformDone(ACLMessage msg, WorldState ws) { }
        protected virtual void HandleCancel(ACLMessage msg, WorldState ws) { }
        protected virtual void HandleDefault(ACLMessage msg, WorldState ws) { }

        // ── Métodos de apoyo (FIPA Base) ───────────────────────────────────────────

        private void ProcessPendingCfps(WorldState ws) {
            if (ws.PendingCfps == null || ws.PendingCfps.Count == 0) return;
            if (HasActiveCnpInitiator()) return; 

            ContractTask nextTask = ws.PendingCfps.Dequeue();
            LaunchProtocol(new ContractNetInitiator(new CfpContent { Task = nextTask }, _replyByWindow), ws);
        }

        private bool IsExpired(ACLMessage msg) => msg.ReplyBy > 0f && Time.time > msg.ReplyBy;

        public void Send(ACLMessage msg) {
            ACLMessage.Log(msg);
            FIPAAgent target;
            if (_agents.TryGetValue(msg.Receiver, out target)) target.ReceiveMessage(msg);
        }

        public void Broadcast(ACLMessage msg) {
            ACLMessage.Log(msg);
            foreach (var agent in _agents.Values) if (agent.AgentId != msg.Sender) agent.ReceiveMessage(msg);
        }

        public void BroadcastToChannel(string channel, ACLMessage msg) {
            msg.Channel = channel;
            ACLMessage.Log(msg);
            if (!_channels.TryGetValue(channel, out var subs)) return;
            foreach (string id in subs) {
                if (id == msg.Sender) continue;
                if (_agents.TryGetValue(id, out var target)) target.ReceiveMessage(msg);
            }
        }

        public static void SubscribeToChannel(string agentId, string channel) {
            if (!_channels.ContainsKey(channel)) _channels[channel] = new HashSet<string>();
            _channels[channel].Add(agentId);
        }

        public static void UnsubscribeFromChannel(string agentId, string channel) {
            if (_channels.ContainsKey(channel)) _channels[channel].Remove(agentId);
        }

        protected bool HasActiveCnpInitiator() {
            foreach (var p in _ongoing_conversations.Values) if (p is ContractNetInitiator) return true;
            return false;
        }

        public bool HasActiveQueryInitiator() {
            foreach (var p in _ongoing_conversations.Values) if (p is QueryInitiator) return true;
            return false;
        }

        protected bool HasActiveCnpParticipant() {
            foreach (var p in _ongoing_conversations.Values) if (p is ContractNetParticipant) return true;
            return false;
        }


        void DiscardExpired() {
            float now = Time.time;
            int toProcess = _count;
            int newTail = _head;
            int newCount = 0;

            for (int i = 0; i < toProcess; i++) {
                int pos = (_head + i) % BUFFER_SIZE;
                ACLMessage msg = _buffer[pos];
                if (!(msg.ReplyBy > 0f && msg.ReplyBy < now)) {
                    _buffer[newTail] = msg;
                    newTail = (newTail + 1) % BUFFER_SIZE;
                    newCount++;
                }
            }
            _tail = newTail;
            _count = newCount;
        }

        void RemoveFromBuffer(int pos) {
            if (pos < 0) return;
            for (int i = 0; i < _count - 1; i++) {
                int cur  = (pos + i)     % BUFFER_SIZE;
                int next = (pos + i + 1) % BUFFER_SIZE;
                _buffer[cur] = _buffer[next];
            }
            _tail = (_tail - 1 + BUFFER_SIZE) % BUFFER_SIZE;
            _count--;
        }

        int FindInBuffer(ACLMessage msg) {
            for (int i = 0; i < _count; i++) {
                int pos = (_head + i) % BUFFER_SIZE;
                if (_buffer[pos].MessageId == msg.MessageId) return pos;
            }
            return -1;
        }
    }
}
