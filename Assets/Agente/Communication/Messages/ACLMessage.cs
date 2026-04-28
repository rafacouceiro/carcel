using UnityEngine;
using System;
using System.IO;

namespace AgenticPrison.Communication.Messages {

    // Mensaje FIPA-ACL: unidad atómica de comunicación entre agentes.
    // Estructura simplificada sin campos redundantes o innecesarios.
    public struct ACLMessage {
        public string       MessageId;        // Identificador único técnico para gestión de buffer
        public Performative Performative;
        public string       Sender;           // AgentId del emisor
        public string       Receiver;         // AgentId del receptor (vacío si es broadcast)
        public string       ConversationId;   // ID para agrupar mensajes de un mismo protocolo
        public object       Content;          // Payload tipado
        public float        SentAt;           // Timestamp de envío
        public float        ReplyBy;          // Deadline para respuesta (0 = sin límite)
        public string       Channel;          // null = unicast; nombre del canal si es pub/sub

        // Imprime el mensaje en consola y en el log de archivo
        public static void Log(ACLMessage msg) {
            string receiver = string.IsNullOrEmpty(msg.Receiver) ? (string.IsNullOrEmpty(msg.Channel) ? "BROADCAST" : $"CHANNEL:{msg.Channel}") : msg.Receiver;
            string conv     = string.IsNullOrEmpty(msg.ConversationId) ? "none" : (msg.ConversationId.Length >= 8 ? msg.ConversationId.Substring(0, 8) : msg.ConversationId);

            string line = string.Format(
                "[FIPA] {0} | from:{1} -> to:{2} | conv:{3} | content:{4}",
                msg.Performative.ToString(),
                msg.Sender,
                receiver,
                conv,
                msg.Content?.ToString()
            );

            Debug.Log(line);

            try {
                string dir = Path.Combine(Application.dataPath, "Logs");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "comms.log"), line + "\n");
            } catch (Exception) {
                // Silenciamos errores de escritura para evitar spam en consola si el archivo está bloqueado
            }
        }
    }
}
