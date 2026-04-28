using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols;
using AgenticPrison.Communication.Protocols.ContractNet;
using AgenticPrison.Communication.Protocols.Query;

namespace AgenticPrison.Communication {

    // Base abstracta de todo agente FIPA. Gestiona el buffer de mensajes, el enrutado por conversación
    // y el registro global de agentes (transporte unificado, sin MessageBus separado).
    public abstract class FIPAAgent : MonoBehaviour {

        // Buffer circular de capacidad fija para mensajes entrantes
        const int BUFFER_SIZE = 16;
        readonly ACLMessage[] _buffer = new ACLMessage[BUFFER_SIZE];
        int _head;
        int _tail;
        int _count;

        // Protocolos activos indexados por ConversationId
        readonly Dictionary<string, ICommProtocol> _ongoing_conversations = new Dictionary<string, ICommProtocol>();
        const int MAX_CONVERSATIONS = 8;

        // Registro global de agentes por id (transporte unicast y broadcast)
        static readonly Dictionary<string, FIPAAgent> _agents = new Dictionary<string, FIPAAgent>();

        // Registro global de suscripciones a canales: canal → conjunto de agentIds
        static readonly Dictionary<string, HashSet<string>> _channels = new Dictionary<string, HashSet<string>>();

        // Identificador único del agente, usado como clave en el registro global
        public abstract string AgentId { get; }

        // Registrar al agente en el registro global al inicio
        protected virtual void Start() {
            _agents[AgentId] = this;
        }

        // Solo descarta mensajes expirados — el procesamiento lo hace la subclase con ProcessIncoming
        protected virtual void Update() {
            DiscardExpired();
        }

        // Punto de entrada de mensajes
        public void ReceiveMessage(ACLMessage msg) {
            if (_count == BUFFER_SIZE) {
                // Buffer lleno: descartar el mensaje más antiguo
                _head = (_head + 1) % BUFFER_SIZE;
                _count--;
            }
            _buffer[_tail] = msg;
            _tail = (_tail + 1) % BUFFER_SIZE;
            _count++;
        }

        // Lanza un protocolo registrándolo por ConversationId e iniciándolo.
        // Public para que las tareas sociales puedan llamarlo con la referencia al agente.
        public void LaunchProtocol(ICommProtocol protocol, WorldState ws) {
            if (_ongoing_conversations.Count >= MAX_CONVERSATIONS) return;
            _ongoing_conversations[protocol.ConversationId] = protocol;
            protocol.Init(this, ws);
        }

        // Devuelve el protocolo activo con ese ConversationId, o null si no existe.
        public ICommProtocol GetProtocol(string convId) {
            ICommProtocol proto;
            return _ongoing_conversations.TryGetValue(convId, out proto) ? proto : null;
        }

        // Procesa mensajes del buffer enrutándolos a conversaciones activas o a OnMessageReceived.
        // Las conversaciones activas tienen prioridad sobre mensajes desconocidos.
        protected void ProcessIncoming(WorldState ws, int maxPerFrame = 2) {
            int processed = 0;

            // Primero: avanzar protocolos activos por tiempo (transiciones de deadline)
            var convIds = new List<string>(_ongoing_conversations.Keys);
            foreach (string id in convIds) {
                _ongoing_conversations[id].Tick(Time.time, ws);
                if (_ongoing_conversations[id].IsComplete)
                    _ongoing_conversations.Remove(id);
            }

            // ContractNetActive ya no se limpia aquí: lo gestiona GuardBrain cuando el equipo se disuelve

            // Segundo: procesar mensajes del buffer, priorizando conversaciones activas
            int bufferSnapshot = _count;
            int readPos = _head;
            var deferred = new List<ACLMessage>();

            for (int i = 0; i < bufferSnapshot && processed < maxPerFrame; i++) {
                int pos = (readPos + i) % BUFFER_SIZE;
                ACLMessage msg = _buffer[pos];

                bool expired = msg.ReplyBy > 0f && Time.time > msg.ReplyBy;
                if (expired) continue;

                if (_ongoing_conversations.ContainsKey(msg.ConversationId)) {
                    _ongoing_conversations[msg.ConversationId].Tick(msg, ws);
                    if (_ongoing_conversations[msg.ConversationId].IsComplete)
                        _ongoing_conversations.Remove(msg.ConversationId);
                    OnMessageReceived(msg);
                    RemoveFromBuffer(pos);
                    processed++;
                } else {
                    deferred.Add(msg);
                }
            }

            // Segunda pasada: mensajes de conversaciones desconocidas (CFPs nuevos, etc.)
            foreach (ACLMessage msg in deferred) {
                if (processed >= maxPerFrame) break;
                bool expired = msg.ReplyBy > 0f && Time.time > msg.ReplyBy;
                if (!expired) {
                    // Auto-indexar CFPs de apertura + respuesta reactiva (sin HTN)
                    if (msg.Performative == Performative.Cfp &&
                        !_ongoing_conversations.ContainsKey(msg.ConversationId) &&
                        _ongoing_conversations.Count < MAX_CONVERSATIONS)
                    {
                        // Si el CFP pertenece a un sector diferente al equipo actual, disolver primero
                        var cfpContent = msg.Content as CfpContent;
                        bool isDifferentSector = cfpContent != null
                            && !string.IsNullOrEmpty(cfpContent.SectorId)
                            && !string.IsNullOrEmpty(ws.FugitiveSectorId)
                            && cfpContent.SectorId != ws.FugitiveSectorId
                            && !string.IsNullOrEmpty(ws.TeamName);

                        if (isDifferentSector) {
                            var toRemove = new List<string>();
                            foreach (var kv in _ongoing_conversations)
                                if (kv.Value is ContractNetParticipant) toRemove.Add(kv.Key);
                            foreach (var id in toRemove) _ongoing_conversations.Remove(id);

                            ws.AssignedTask      = null;
                            ws.AssignedRole      = AgentRole.None;
                            ws.TeamName          = string.Empty;
                            ws.FugitiveSectorId  = cfpContent.SectorId;
                        }

                        var participant = new ContractNetParticipant(msg, AgentId);
                        _ongoing_conversations[participant.ConversationId] = participant;
                        participant.Init(this, ws);

                        // Respuesta reactiva: evaluar y proponer/rechazar sin pasar por el HTN social
                        float cost;
                        if (EvaluateCfp(msg, ws, out cost))
                            participant.SendPropose(this, ws, cost);
                        else
                            participant.SendRefuse(this, ws);
                    }

                    if (msg.Performative == Performative.Query &&
                        !_ongoing_conversations.ContainsKey(msg.ConversationId) &&
                        _ongoing_conversations.Count < MAX_CONVERSATIONS)
                    {
                        var participant = new QueryParticipant(msg, AgentId);
                        _ongoing_conversations[participant.ConversationId] = participant;
                        participant.Init(this, ws);
                    }
                    OnMessageReceived(msg);
                    processed++;
                }
                RemoveFromBuffer(FindInBuffer(msg));
            }
        }

        // Envío unicast: registra en log y enruta directamente por id
        public void Send(ACLMessage msg) {
            ACLMessage.Log(msg);
            FIPAAgent target;
            if (_agents.TryGetValue(msg.Receiver, out target)) {
                target.ReceiveMessage(msg);
            } else {
                Debug.LogWarning("[FIPA] Receptor no encontrado: " + msg.Receiver);
            }
        }

        // ── Sistema de canales pub/sub ─────────────────────────────────────────────

        public static void SubscribeToChannel(string agentId, string channel) {
            if (!_channels.ContainsKey(channel)) _channels[channel] = new HashSet<string>();
            _channels[channel].Add(agentId);
        }

        public static void UnsubscribeFromChannel(string agentId, string channel) {
            if (_channels.ContainsKey(channel)) _channels[channel].Remove(agentId);
        }

        // Envía un mensaje a todos los suscriptores del canal excepto el emisor
        public void BroadcastToChannel(string channel, ACLMessage msg) {
            msg.Channel = channel;
            ACLMessage.Log(msg);
            HashSet<string> subs;
            if (!_channels.TryGetValue(channel, out subs) || subs.Count == 0) return;
            var snapshot = new string[subs.Count];
            subs.CopyTo(snapshot);
            foreach (string id in snapshot) {
                if (id == msg.Sender) continue;
                FIPAAgent target;
                if (_agents.TryGetValue(id, out target)) target.ReceiveMessage(msg);
            }
        }

        // Verdadero mientras haya algún ContractNetInitiator activo como iniciador
        protected bool HasActiveCnpInitiator() {
            foreach (var p in _ongoing_conversations.Values)
                if (p is ContractNetInitiator) return true;
            return false;
        }

        // Hook para que las subclases evalúen reactivamente si proponer o rechazar un CFP.
        // Devuelve true = proponer con el coste calculado; false = rechazar.
        protected virtual bool EvaluateCfp(ACLMessage cfp, WorldState ws, out float cost) {
            cost = 0f; return false;
        }

        // Envío broadcast a todos los agentes registrados excepto el emisor
        public void Broadcast(ACLMessage msg) {
            ACLMessage.Log(msg);
            FIPAAgent[] snapshot = new FIPAAgent[_agents.Values.Count];
            _agents.Values.CopyTo(snapshot, 0);
            foreach (FIPAAgent agent in snapshot) {
                if (agent.AgentId != msg.Sender)
                    agent.ReceiveMessage(msg);
            }
        }

        // Hook para subclases: mensajes sin conversación activa (CFPs entrantes, Informs, etc.)
        protected virtual void OnMessageReceived(ACLMessage msg) { }

        // Elimina del buffer los mensajes cuyo ReplyBy ya ha expirado
        void DiscardExpired() {
            float now = Time.time;
            int toProcess = _count;
            int newHead = _head;
            int newTail = _head;
            int newCount = 0;

            for (int i = 0; i < toProcess; i++) {
                int pos = (_head + i) % BUFFER_SIZE;
                ACLMessage msg = _buffer[pos];
                bool expired = msg.ReplyBy > 0f && msg.ReplyBy < now;
                if (!expired) {
                    _buffer[newTail] = msg;
                    newTail = (newTail + 1) % BUFFER_SIZE;
                    newCount++;
                }
            }

            _head  = newHead;
            _tail  = newTail;
            _count = newCount;
        }

        // Busca la posición de un mensaje en el buffer por MessageId
        int FindInBuffer(ACLMessage msg) {
            for (int i = 0; i < _count; i++) {
                int pos = (_head + i) % BUFFER_SIZE;
                if (_buffer[pos].MessageId == msg.MessageId) return pos;
            }
            return -1;
        }

        // Elimina una posición del buffer compactando hacia atrás
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
    }
}
