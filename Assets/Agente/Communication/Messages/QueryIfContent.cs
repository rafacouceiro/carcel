using UnityEngine;

namespace AgenticPrison.Communication.Messages {

    // Payload del QueryIf: posición del ruido y radio máximo para considerar que un guardia estaba cerca.
    public class QueryIfContent : IMessageContent {
        public Vector3 NoisePosition;
        public float   Threshold;
    }
}
