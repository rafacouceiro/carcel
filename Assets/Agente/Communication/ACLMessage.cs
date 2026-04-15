using UnityEngine;
using System;
using System.IO;

namespace AgenticPrison.Communication {

    // Mensaje FIPA-ACL: unidad atómica de comunicación entre agentes
    public struct ACLMessage {
        public string      MessageId;        // GUID único generado al crear el mensaje
        public Performative Performative;
        public string      Sender;           // Lo que no sea sobre la comunicación va en contenido
        public string      Receiver;         // Vacío si es broadcast
        public string      ConversationId;
        public string      Ontology;
        public string      Content;          // Payload serializado como string
        public float       SentAt;           // Time.time al enviar
        public float       ReplyBy;          // Tiempo límite de respuesta (0 = sin límite)
        public Vector3     SenderPosition;   // Posición del emisor al enviar

        // Imprime el mensaje en consola y lo añade a Assets/Logs/comms.log
        public static void Log(ACLMessage msg) {
            string receiver = string.IsNullOrEmpty(msg.Receiver) ? "BROADCAST" : msg.Receiver;
            string conv     = msg.ConversationId.Length >= 8
                              ? msg.ConversationId.Substring(0, 8)
                              : msg.ConversationId;

            string line = string.Format(
                "[FIPA] frame:{0} | {1} | from:{2} -> to:{3} | conv:{4} | ontology:{5} | content:{6} | sentAt:{7:F2}",
                Time.frameCount.ToString("D6"),
                msg.Performative.ToString(),
                msg.Sender,
                receiver,
                conv,
                msg.Ontology,
                msg.Content,
                msg.SentAt
            );

            Debug.Log(line);

            try {
                string dir = Path.Combine(Application.dataPath, "Logs");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "comms.log"), line + "\n");
            } catch (Exception e) {
                Debug.LogWarning("[FIPA] No se pudo escribir en comms.log: " + e.Message);
            }
        }
    }
}
