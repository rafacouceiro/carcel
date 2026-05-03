using UnityEngine;
using System;
using System.IO;

namespace AgenticPrison.Communication.Messages {

    // Mensaje FIPA-ACL simplificado. struct para evitar allocaciones en el bus.
    public struct ACLMessage {
        public string          MessageId;       // necesario para RemoveFromBuffer
        public Performative    Performative;
        public string          Sender;
        public string          Receiver;        // vacío en broadcast
        public string          ConversationId;  // agrupa los mensajes de un mismo protocolo
        public IMessageContent Content;
        public float           ReplyBy;         // 0 = sin límite
        public string          Channel;         // null = unicast; canal si es pub/sub

        // Usa 'as' cuando el mensaje puede llevar distintos tipos (ej. HandleInform).
        // Usa GetContent<T> cuando solo esperas un tipo concreto — avisa si el cast falla.
        public static T GetContent<T>(ACLMessage msg) where T : class, IMessageContent {
            if (msg.Content is T typed) return typed;
            if (msg.Content != null)
                Debug.LogWarning($"[FIPA] tipo incorrecto en {msg.Performative}: esperado {typeof(T).Name}, recibido {msg.Content.GetType().Name}");
            return null;
        }

        // Escribe en consola y en comms.log. Lo llama FIPAAgent.Send/Broadcast.
        public static void Log(ACLMessage msg) {
            string receiver = string.IsNullOrEmpty(msg.Receiver)
                ? (string.IsNullOrEmpty(msg.Channel) ? "BROADCAST" : $"CHANNEL:{msg.Channel}")
                : msg.Receiver;
            string conv = string.IsNullOrEmpty(msg.ConversationId) ? "none"
                : (msg.ConversationId.Length >= 8 ? msg.ConversationId.Substring(0, 8) : msg.ConversationId);

            string line = string.Format("[FIPA] {0} | from:{1} -> to:{2} | conv:{3} | content:{4}",
                msg.Performative, msg.Sender, receiver, conv, msg.Content?.ToString());

            Debug.Log(line);

            try {
                string dir = Path.Combine(Application.dataPath, "Logs");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "comms.log"), line + "\n");
            } catch (Exception) {
                // si el archivo está bloqueado, ignoramos — no vale la pena spamear la consola
            }
        }
    }
}
