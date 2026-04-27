using UnityEngine;
using System.Collections.Generic;

namespace AgenticPrison.Physical {
    // Almacena información contextual sobre un punto de navegación en el mapa
    public class WayPointData : MonoBehaviour {
        [Header("Clasificación del Punto")]
        
        // Puntos estratégicos de guardia o vigilancia estática
        [Tooltip("Marca si es un punto crítico a vigilar (escaleras, salidas, áreas restringidas).")]
        public bool isKeyPoint = false;

        // Puntos que forzosamente deben ser visitados al patrullar
        [Tooltip("Marca para incluir este punto en las rutas de patrullaje ordinario.")]
        public bool isPatrolCheckpoint = true;

        // Puntos que pertenecen al interior de las celdas
        [Tooltip("Marca para indicar que este waypoint se encuentra en el interior de una celda.")]
        public bool isCell = false;

        [Header("Bloqueo de Cuadrantes (Operaciones de Búsqueda)")]
        [Tooltip("Marca si este punto actúa como tapón para cerrar un sector.")]
        public bool isBlockingPoint = false;

        [Tooltip("Identificador para agrupar varios puntos de bloqueo (ej. 'PuertaNorte'). El agente patrullará entre los puntos con este mismo ID.")]
        public string blockingGroupId = string.Empty;

        [Tooltip("Sector que este punto de bloqueo defiende. Solo si no puede heredarlo del RoomNode padre.")]
        public string blockingSectorId = string.Empty;
    }
}