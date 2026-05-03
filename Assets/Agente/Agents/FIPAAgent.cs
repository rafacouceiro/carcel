using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols;
using AgenticPrison.Communication.Protocols.ContractNet;
using AgenticPrison.Communication.Protocols.Query;

namespace AgenticPrison.Communication {

    // Clase base de todos los agentes FIPA. Se encarga del bus de mensajes,
    // el routing por conversación y el ciclo de vida de los protocolos.
    // Cada subclase solo tiene que sobreescribir los hooks que le interesen.
    public abstract class FIPAAgent : MonoBehaviour {

        const int BUFFER_SIZE = 16;
        readonly ACLMessage[] _buffer = new ACLMessage[BUFFER_SIZE];
        int _head;
        int _tail;
        int _count;

        [SerializeField] protected float _replyByWindow = 3f;

        readonly Dictionary<string, ICommProtocol> _ongoing_conversations = new Dictionary<string, ICommProtocol>();
        const int MAX_CONVERSATIONS = 8;

        // Registro global de agentes y canales — estático para que todos compartan el bus
        static readonly Dictionary<string, FIPAAgent>           _agents   = new Dictionary<string, FIPAAgent>();
        static readonly Dictionary<string, HashSet<string>>     _channels = new Dictionary<string, HashSet<string>>();

        public abstract string AgentId { get; }

        protected virtual void Start() {
            _agents[AgentId] = this;
        }

        protected virtual void Update() {
            DiscardExpired();
            ProcessIncoming(GetAgentState());
        }

        // Cada subclase expone su estado para que la base pueda procesar mensajes
        protected abstract WorldState GetAgentState();

        public void ReceiveMessage(ACLMessage msg) {
            // Si el buffer está lleno, descartamos el más antiguo
            if (_count == BUFFER_SIZE) {
                _head = (_head + 1) % BUFFER_SIZE;
                _count--;
            }
            _buffer[_tail] = msg;
            _tail  = (_tail + 1) % BUFFER_SIZE;
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

        // Procesa el estado de las conversaciones activas, los mensajes del buffer
        // y lanza el siguiente CFP pendiente si no hay ninguna subasta abierta.
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
            int readIdx   = 0;
            var deferred  = new List<ACLMessage>();

            // Primera pasada: mensajes que pertenecen a una conversación ya abierta
            // readIdx no avanza al eliminar porque el siguiente elemento se desplaza a esa posición
            while (readIdx < _count && processed < maxPerFrame) {
                int pos        = (_head + readIdx) % BUFFER_SIZE;
                ACLMessage msg = _buffer[pos];

                if (IsExpired(msg)) { RemoveFromBuffer(pos); continue; }

                if (!string.IsNullOrEmpty(msg.ConversationId) && _ongoing_conversations.ContainsKey(msg.ConversationId)) {
                    _ongoing_conversations[msg.ConversationId].Tick(msg, ws);
                    if (_ongoing_conversations[msg.ConversationId].IsComplete) _ongoing_conversations.Remove(msg.ConversationId);
                    OnMessageReceived(msg, ws);
                    RemoveFromBuffer(pos);
                    processed++;
                } else {
                    deferred.Add(msg);
                    readIdx++;
                }
            }

            // Segunda pasada: mensajes que pueden abrir conversación nueva o son notificaciones sueltas
            foreach (ACLMessage msg in deferred) {
                if (processed >= maxPerFrame) break;
                if (IsExpired(msg)) { RemoveFromBuffer(FindInBuffer(msg)); continue; }

                if (!string.IsNullOrEmpty(msg.ConversationId))
                    HandlePotentialNewProtocol(msg, ws);

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
                // Evaluamos antes de registrar: si rechazamos, el protocolo ya termina
                OnCfpReceived(msg, ws, p);
                if (!p.IsComplete) _ongoing_conversations[p.ConversationId] = p;
                return true;
            }

            if (msg.Performative == Performative.QueryIf) {
                var p = new QueryIfParticipant(msg, AgentId);
                p.Init(this, ws);
                // El participante no espera respuesta, así que no hace falta registrarlo
                OnQueryIfReceived(msg, ws, p);
                return true;
            }

            return false;
        }

        // Las subclases sobreescriben esto si quieren responder a un QueryIf
        protected virtual void OnQueryIfReceived(ACLMessage msg, WorldState ws, QueryIfParticipant participant) { }

        // Por defecto evalúa el CFP con EvaluateCfp y propone o rechaza según el resultado
        protected virtual void OnCfpReceived(ACLMessage msg, WorldState ws, ContractNetParticipant participant) {
            float cost;
            if (EvaluateCfp(msg, ws, out cost))
                participant.SendPropose(this, ws, cost);
            else
                participant.SendRefuse(this, ws);
        }

        protected virtual void OnMessageReceived(ACLMessage msg, WorldState ws) {
            switch (msg.Performative) {
                case Performative.Inform:     HandleInform(msg, ws);     break;
                case Performative.InformDone: HandleInformDone(msg, ws); break;
                case Performative.Cfp:        HandleCfp(msg, ws);        break;
                case Performative.Cancel:     HandleCancel(msg, ws);     break;
                default:                      HandleDefault(msg, ws);    break;
            }
        }

        // Actualiza el estado con la información del Inform recibido.
        // Usamos 'as' en vez de GetContent porque un Inform puede llevar
        // distintos tipos de contenido según el contexto.
        protected virtual void HandleInform(ACLMessage msg, WorldState ws) {
            var sighting = msg.Content as FugitiveSightingContent;
            if (sighting == null) return;

            ws.PrisonerInCell = false;

            if (sighting.SectorId == "[UNK]") {
                if (ws.FugitiveSectorId == "[UNK]") return;
                ws.FugitiveSectorId    = "[UNK]";
                ws.PerimeteredSectorId = string.Empty;
                string logMsg = sighting.Timestamp == 0f ? "escape confirmed — sector [UNK]" : "sweep failed — sector [UNK]";
                FIPALogger.Log(AgentId, "radio", Performative.Inform, logMsg);
            } else if (sighting.Timestamp > ws.LastKnownPositionTime) {
                // Solo actualizamos si la info es más reciente que lo que ya sabemos
                ws.LastKnownPosition     = sighting.Position;
                ws.LastKnownPositionTime = sighting.Timestamp;
                ws.FugitiveSectorId      = sighting.SectorId;
                ws.seenByMe              = false;
                FIPALogger.Log(AgentId, "radio", Performative.Inform, $"recv: Fugitive at {sighting.SectorId} (from {sighting.ReporterId})");
            }
        }

        // Devuelve true si este agente puede y quiere hacer la tarea del CFP.
        // La subclase calcula el coste; la base por defecto siempre rechaza.
        protected virtual bool EvaluateCfp(ACLMessage cfp, WorldState ws, out float cost) {
            cost = 0f; return false;
        }

        protected virtual void HandleCfp(ACLMessage msg, WorldState ws)        { }
        protected virtual void HandleInformDone(ACLMessage msg, WorldState ws)  { }
        protected virtual void HandleCancel(ACLMessage msg, WorldState ws)      { }
        protected virtual void HandleDefault(ACLMessage msg, WorldState ws)     { }

        // Lanza el siguiente CFP de la cola si no hay ninguna subasta abierta.
        // Los CFPs se lanzan de uno en uno para no saturar a los participantes.
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
            foreach (var agent in _agents.Values)
                if (agent.AgentId != msg.Sender) agent.ReceiveMessage(msg);
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

        // Cancela todas las subastas CNP activas, tanto de iniciador como de participante.
        // Se llama cuando cambia el sector del fugitivo y los contratos anteriores ya no sirven.
        protected void CancelOngoingCnpProtocols() {
            var toRemove = new List<string>();
            foreach (var kvp in _ongoing_conversations)
                if (kvp.Value is ContractNetInitiator || kvp.Value is ContractNetParticipant)
                    toRemove.Add(kvp.Key);
            foreach (var id in toRemove) _ongoing_conversations.Remove(id);
        }

        public bool HasActiveQueryIfInitiator() {
            foreach (var p in _ongoing_conversations.Values) if (p is QueryIfInitiator) return true;
            return false;
        }

        protected bool HasActiveCnpParticipant() {
            foreach (var p in _ongoing_conversations.Values) if (p is ContractNetParticipant) return true;
            return false;
        }

        // Limpia el buffer de mensajes caducados antes de procesar nada.
        // Es más barato hacer esto una vez por frame que comprobar en cada acceso.
        void DiscardExpired() {
            float now      = Time.time;
            int toProcess  = _count;
            int newTail    = _head;
            int newCount   = 0;

            for (int i = 0; i < toProcess; i++) {
                int pos        = (_head + i) % BUFFER_SIZE;
                ACLMessage msg = _buffer[pos];
                if (!(msg.ReplyBy > 0f && msg.ReplyBy < now)) {
                    _buffer[newTail] = msg;
                    newTail  = (newTail + 1) % BUFFER_SIZE;
                    newCount++;
                }
            }
            _tail  = newTail;
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
