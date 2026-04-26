using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Communication {

    // Base abstracta de todo agente FIPA. Gestiona el buffer de mensajes y el enrutado por conversación.
    // El registro en MessageBus ocurre en Start() para que la subclase pueda asignar su nombre en Awake().
    public abstract class FIPAAgent : MonoBehaviour {

        // Buffer circular de capacidad fija para mensajes entrantes
        const int BUFFER_SIZE = 16;
        readonly ACLMessage[] _buffer = new ACLMessage[BUFFER_SIZE];
        int _head;
        int _tail;
        int _count; // Numero de mensajes en el buffer

        // Protocolos activos indexados por ConversationId
        readonly Dictionary<string, ICommProtocol> _ongoing_conversations = new Dictionary<string, ICommProtocol>();
        const int MAX_CONVERSATIONS = 8;

        // Identificador único del agente, usado como clave en MessageBus
        public abstract string AgentId { get; }

        // Ontologías a las que se suscribe este agente (broadcast filtrado, para compatibilidad)
        public virtual string[] GetOntologies() { return new string[0]; }

        // Registrar al agente en sus topics
        protected virtual void Start() {
            MessageBus.Instance.Register(this, GetOntologies());
        }

        // Solo descarta mensajes expirados — el procesamiento lo hace la subclase con ProcessIncoming
        protected virtual void Update() {
            DiscardExpired();
        }

        // Punto de entrada de mensajes desde MessageBus
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
        // Permite a las tareas sociales recuperar un protocolo pre-indexado sin crearlo de nuevo.
        public ICommProtocol GetProtocol(string convId) {
            ICommProtocol proto;
            return _ongoing_conversations.TryGetValue(convId, out proto) ? proto : null;
        }

        // Procesa mensajes del buffer enrutándolos a conversaciones activas o a OnMessageReceived.
        // Las conversaciones activas tienen prioridad sobre mensajes desconocidos.
        protected void ProcessIncoming(WorldState ws, int maxPerFrame = 2) {
            int processed = 0;

            // Primero: avanzar protocolos activos por tiempo (transiciones de deadline)
            // Se hace antes de procesar mensajes para que los estados sean coherentes
            var convIds = new List<string>(_ongoing_conversations.Keys);
            foreach (string id in convIds) {
                _ongoing_conversations[id].Tick(Time.time, ws);
                if (_ongoing_conversations[id].IsComplete)
                    _ongoing_conversations.Remove(id); // Eliminar conversación cuando se acaba
            }

            // Si ya no quedan conversaciones activas, liberar el lock de Contract Net
            if (_ongoing_conversations.Count == 0)
                ws.ContractNetActive = false;

            // Segundo: procesar mensajes del buffer, priorizando conversaciones activas
            // Primera pasada: mensajes de conversaciones ya activas
            int bufferSnapshot = _count;
            int readPos = _head;
            var deferred = new List<ACLMessage>(); // Mensajes que no pertenecen a conversaciones conocidas

            for (int i = 0; i < bufferSnapshot && processed < maxPerFrame; i++) { // No vaciar la cola ni procesar demasiados mensajes
                int pos = (readPos + i) % BUFFER_SIZE;
                ACLMessage msg = _buffer[pos];

                bool expired = msg.ReplyBy > 0f && Time.time > msg.ReplyBy;
                if (expired) continue;

                if (_ongoing_conversations.ContainsKey(msg.ConversationId)) {
                    _ongoing_conversations[msg.ConversationId].Tick(msg, ws); // Avanzar el protocolo en un tick de mensaje
                    if (_ongoing_conversations[msg.ConversationId].IsComplete)
                        _ongoing_conversations.Remove(msg.ConversationId); // Eliminar conversación si se ha terminado
                    OnMessageReceived(msg); // Notificar al agente de cualquier mensaje, también mid-protocol
                    RemoveFromBuffer(pos); // Eliminar mensaje del buffer
                    processed++;
                } else {
                    deferred.Add(msg); // Solo se eejcuta si queda espacio para mensajes nuevos
                }
            }

            // Segunda pasada: mensajes de conversaciones desconocidas (CFPs nuevos, etc.)
            foreach (ACLMessage msg in deferred) {
                if (processed >= maxPerFrame) break;
                bool expired = msg.ReplyBy > 0f && Time.time > msg.ReplyBy;
                if (!expired) {
                    // Auto-indexar cualquier mensaje de apertura de conversación.
                    // El protocolo queda registrado antes de que el HTN tome ninguna decisión,
                    // así los mensajes siguientes (Accept, Reject) ya tienen ruta conocida.
                    if (msg.Performative == Performative.Cfp &&
                        !_ongoing_conversations.ContainsKey(msg.ConversationId) &&
                        _ongoing_conversations.Count < MAX_CONVERSATIONS)
                    {
                        var participant = new ContractNetParticipant(msg, AgentId);
                        _ongoing_conversations[participant.ConversationId] = participant;
                        participant.Init(this, ws); // almacena _agent; no envía nada
                    }

                    if (msg.Performative == Performative.Query &&
                        !_ongoing_conversations.ContainsKey(msg.ConversationId) &&
                        _ongoing_conversations.Count < MAX_CONVERSATIONS)
                    {
                        var participant = new QueryParticipant(msg, AgentId);
                        _ongoing_conversations[participant.ConversationId] = participant;
                        participant.Init(this, ws); // comprueba distancia, envía Inform si procede, cierra
                    }
                    OnMessageReceived(msg);
                    processed++;
                }
                RemoveFromBuffer(FindInBuffer(msg));
            }
        }

        // Envío unicast: registra en log y delega al bus
        public void Send(ACLMessage msg) {
            ACLMessage.Log(msg);
            MessageBus.Instance.Send(msg);
        }

        // Envío broadcast a todos los agentes registrados (sin filtro de ontología)
        public void Broadcast(ACLMessage msg) {
            ACLMessage.Log(msg);
            MessageBus.Instance.Broadcast(msg);
        }

        // Envío broadcast filtrado por ontología (compatibilidad con PresenceNotifier)
        public void Broadcast(ACLMessage msg, string ontology) {
            ACLMessage.Log(msg);
            MessageBus.Instance.Broadcast(msg, ontology);
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

        // Busca la posición de un mensaje en el buffer por referencia de ConversationId
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
            // Desplaza los elementos posteriores una posición hacia atrás
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
