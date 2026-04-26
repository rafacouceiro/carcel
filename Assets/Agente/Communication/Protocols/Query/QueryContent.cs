using UnityEngine;

namespace AgenticPrison.Communication {

    // Payload del mensaje Query: posición sospechosa y radio de relevancia para el receptor
    public class QueryContent {
        public Vector3 NoisePosition; // punto del que proviene el ruido sospechoso
        public float   Threshold;     // distancia máxima para que el receptor se considere fuente
    }
}
