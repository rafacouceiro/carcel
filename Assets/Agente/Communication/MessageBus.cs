using System.Collections.Generic;
using UnityEngine;

namespace AgenticPrison.Communication {

    // Singleton de transporte FIPA. No es MonoBehaviour: vive mientras haya al menos un agente registrado.
    public class MessageBus {

        static MessageBus _instance;
        public static MessageBus Instance => _instance ?? (_instance = new MessageBus());

        // Registro de agentes por id (unicast) y por ontología (broadcast)
        readonly Dictionary<string, FIPAAgent>        _agents     = new Dictionary<string, FIPAAgent>();
        readonly Dictionary<string, List<FIPAAgent>>  _ontologies = new Dictionary<string, List<FIPAAgent>>();

        MessageBus() { }

        // Registrar un agente y las ontologías a las que se suscribe
        public void Register(FIPAAgent agent, string[] ontologies) {
            _agents[agent.AgentId] = agent;

            foreach (string ont in ontologies) {
                if (!_ontologies.ContainsKey(ont))
                    _ontologies[ont] = new List<FIPAAgent>();

                if (!_ontologies[ont].Contains(agent))
                    _ontologies[ont].Add(agent);
            }
        }

        // Entrega unicast: busca al receptor por nombre y le pone el mensaje en el buffer
        public void Send(ACLMessage msg) {
            FIPAAgent target;
            if (_agents.TryGetValue(msg.Receiver, out target)) { // Acceso 'safe' al diccionario
                target.ReceiveMessage(msg);
            } else {
                Debug.LogWarning("[FIPA] Receptor no encontrado: " + msg.Receiver);
            }
        }

        // Entrega broadcast sin filtro: todos los agentes registrados excepto el emisor
        public void Broadcast(ACLMessage msg) {
            FIPAAgent[] snapshot = new FIPAAgent[_agents.Values.Count];
            _agents.Values.CopyTo(snapshot, 0);
            foreach (FIPAAgent agent in snapshot) {
                if (agent.AgentId != msg.Sender)
                    agent.ReceiveMessage(msg);
            }
        }

        // Entrega broadcast filtrada por ontología (mantiene compatibilidad con PresenceNotifier)
        public void Broadcast(ACLMessage msg, string ontologyFilter) {
            List<FIPAAgent> subscribers;
            if (!_ontologies.TryGetValue(ontologyFilter, out subscribers)) return;

            FIPAAgent[] snapshot = subscribers.ToArray();
            foreach (FIPAAgent agent in snapshot) {
                if (agent.AgentId != msg.Sender)
                    agent.ReceiveMessage(msg);
            }
        }
    }
}
