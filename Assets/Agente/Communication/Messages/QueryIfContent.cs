using UnityEngine;

namespace AgenticPrison.Communication.Messages {

    // Payload del mensaje QueryIf: pregunta si el receptor estaba cerca de una posición sospechosa.
    // Inyectado por quien lanza el protocolo — el iniciador no construye su propio contenido.
    public class QueryIfContent : IMessageContent {
        public Vector3 NoisePosition;
        public float   Threshold;
    }
}
