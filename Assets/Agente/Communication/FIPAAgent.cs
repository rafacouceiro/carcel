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
        int _count;

        // Protocolos activos indexados por ConversationId
        readonly Dictionary<string, ICommProtocol> _protocols = new Dictionary<string, ICommProtocol>();
        const int MAX_CONVERSATIONS = 3;

        // Identificador único del agente, usado como clave en MessageBus
        public abstract string AgentId { get; }

        // Ontologías a las que se suscribe este agente (broadcast filtrado, para compatibilidad)
        public virtual string[] GetOntologies() { return new string[0]; }

        protected virtual void Start() {
            MessageBus.Instance.Register(this, GetOntologies());
        }

        protected virtual void OnDestroy() {
            MessageBus.Instance.Unregister(this);
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
            if (_protocols.Count >= MAX_CONVERSATIONS) return;
            _protocols[protocol.ConversationId] = protocol;
            protocol.Init(this, ws);
        }

        // Procesa mensajes del buffer enrutándolos a protocolos activos o a OnMessageReceived.
        // Las conversaciones activas tienen prioridad sobre mensajes desconocidos.
        protected void ProcessIncoming(WorldState ws, int maxPerFrame = 2) {
            int processed = 0;

            // Primero: avanzar protocolos activos por tiempo (transiciones de deadline)
            // Se hace antes de procesar mensajes para que los estados sean coherentes
            var convIds = new List<string>(_protocols.Keys);
            foreach (string id in convIds) {
                _protocols[id].Tick(Time.time, ws);
                if (_protocols[id].IsComplete)
                    _protocols.Remove(id);
            }

            // Segundo: procesar mensajes del buffer, priorizando conversaciones activas
            // Primera pasada: mensajes de conversaciones ya activas
            int bufferSnapshot = _count;
            int readPos = _head;
            var deferred = new List<ACLMessage>();

            for (int i = 0; i < bufferSnapshot && processed < maxPerFrame; i++) {
                int pos = (readPos + i) % BUFFER_SIZE;
                ACLMessage msg = _buffer[pos];

                bool expired = msg.ReplyBy > 0f && Time.time > msg.ReplyBy;
                if (expired) continue;

                if (_protocols.ContainsKey(msg.ConversationId)) {
                    _protocols[msg.ConversationId].Tick(msg, ws);
                    if (_protocols[msg.ConversationId].IsComplete)
                        _protocols.Remove(msg.ConversationId);
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
