using UnityEngine;

namespace AgenticPrison.Communication {

    // Base abstracta de todo agente FIPA. Gestiona el buffer de mensajes y el ciclo de proceso.
    // El registro en MessageBus ocurre en Start() para que la subclase pueda asignar su nombre en Awake().
    public abstract class FIPAAgent : MonoBehaviour {

        // Buffer circular de capacidad fija para evitar allocations
        const int BUFFER_SIZE = 16;
        readonly ACLMessage[] _buffer = new ACLMessage[BUFFER_SIZE];
        int _head;   // siguiente posición de lectura
        int _tail;   // siguiente posición de escritura
        int _count;

        // Identificador único del agente, usado como clave en MessageBus
        public abstract string AgentId { get; }

        // Ontologías a las que se suscribe este agente (broadcast)
        public virtual string[] GetOntologies() { return new string[0]; }

        protected virtual void Start() {
            // El nombre del GameObject ya fue asignado en Awake() de la subclase
            MessageBus.Instance.Register(this, GetOntologies());
        }

        protected virtual void OnDestroy() {
            MessageBus.Instance.Unregister(this);
        }

        protected virtual void Update() {
            DiscardExpired();
            ProcessMessages();
        }

        // Añadir mensaje al buffer. Si está lleno, descarta el más antiguo (avanza head).
        public void Receive(ACLMessage msg) {
            if (_count == BUFFER_SIZE) {
                // Buffer lleno: descartar el mensaje más antiguo avanzando la cabeza
                _head = (_head + 1) % BUFFER_SIZE;
                _count--;
            }
            _buffer[_tail] = msg;
            _tail = (_tail + 1) % BUFFER_SIZE;
            _count++;
        }

        // Eliminar del buffer mensajes cuyo tiempo de respuesta ya ha expirado
        void DiscardExpired() {
            float now = Time.time;
            int toProcess = _count;
            int readPos = _head;

            // Reconstruir buffer sin mensajes expirados
            int newHead = _head;
            int newTail = _head;
            int newCount = 0;

            for (int i = 0; i < toProcess; i++) {
                int pos = (readPos + i) % BUFFER_SIZE;
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

        // Procesar todos los mensajes pendientes en el buffer y vaciar
        void ProcessMessages() {
            int toProcess = _count;
            for (int i = 0; i < toProcess; i++) {
                ACLMessage msg = _buffer[_head];
                _head  = (_head + 1) % BUFFER_SIZE;
                _count--;
                OnMessageReceived(msg);
            }
        }

        // Envío unicast: registra en log y delega al bus
        public void Send(ACLMessage msg) {
            ACLMessage.Log(msg);
            MessageBus.Instance.Send(msg);
        }

        // Envío broadcast filtrado por ontología
        public void Broadcast(ACLMessage msg, string ontology) {
            ACLMessage.Log(msg);
            MessageBus.Instance.Broadcast(msg, ontology);
        }

        // Hook para subclases: llamado una vez por mensaje al procesarlo
        protected virtual void OnMessageReceived(ACLMessage msg) { }
    }
}
