using System;
using System.Collections;
using UnityEngine;

namespace AgenticPrison.Communication {

    // Script temporal de verificacion del Dia 1.
    // Confirma que el transporte FIPA funciona entre guardias sin afectar la logica HTN.
    // Eliminar cuando CommPlanner este operativo.
    public class PresenceNotifier : MonoBehaviour {

        Brain _brain;

        void Awake() {
            _brain = GetComponent<Brain>();

            // Suscribir a la ontologia de presencia a traves del agente FIPAAgent de este GameObject
            MessageBus.Instance.AddOntologies(_brain, new[] { "agent-present" });
        }

        void Start() {
            StartCoroutine(BroadcastPresence());
        }

        // Espera 2 segundos y emite un Inform de presencia en broadcast
        IEnumerator BroadcastPresence() {
            yield return new WaitForSeconds(2f);

            ACLMessage msg = new ACLMessage {
                MessageId      = Guid.NewGuid().ToString(),
                Performative   = Performative.Inform,
                Sender         = _brain.AgentId,
                Receiver       = "",
                ConversationId = Guid.NewGuid().ToString(),
                Ontology       = "agent-present",
                Content        = "online",
                SentAt         = Time.time,
                SenderPosition = transform.position
            };

            _brain.Broadcast(msg, "agent-present");
        }

        // Llamado desde Brain.OnMessageReceived cuando llega un mensaje a este agente
        public void HandleMessage(ACLMessage msg) {
            if (msg.Ontology != "agent-present") return;

            if (msg.Content == "online") {
                // Confirmar presencia al emisor
                ACLMessage reply = new ACLMessage {
                    MessageId      = Guid.NewGuid().ToString(),
                    Performative   = Performative.Inform,
                    Sender         = _brain.AgentId,
                    Receiver       = msg.Sender,
                    ConversationId = msg.ConversationId,
                    Ontology       = "agent-present",
                    Content        = "acknowledged",
                    SentAt         = Time.time,
                    SenderPosition = transform.position
                };

                _brain.Send(reply);

                // Senal visual si el emisor esta en radio cercano
                float dist = Vector3.Distance(transform.position, msg.SenderPosition);
                if (dist < 20f && _brain.Flashlight != null)
                    StartCoroutine(DoubleBlink(_brain.Flashlight));
            }
        }

        // Apagar-encender-apagar-encender en 0.1s cada paso
        IEnumerator DoubleBlink(Light light) {
            light.enabled = false;
            yield return new WaitForSeconds(0.1f);
            light.enabled = true;
            yield return new WaitForSeconds(0.1f);
            light.enabled = false;
            yield return new WaitForSeconds(0.1f);
            light.enabled = true;
        }
    }
}
