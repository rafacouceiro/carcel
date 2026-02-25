using UnityEngine;

namespace AgenticPrison.Physical {
    public class WayPointData : MonoBehaviour {
        [Header("Clasificación del Punto")]
        [Tooltip("Puntos estratégicos para hacer guardia o vigilar (escaleras, salidas, pasillos).")]
        public bool isKeyPoint = false;

        [Tooltip("Si este punto debe incluirse en la ronda de patrulla rutinaria.")]
        public bool isPatrolCheckpoint = true;

        [Tooltip("Marca esto si el punto está en una celda.")]
        public bool isCell = false;
    }
}