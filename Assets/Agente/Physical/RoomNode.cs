using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

namespace AgenticPrison.Physical {

    // Representa un espacio o habitación lógica dentro de la prisión
    [RequireComponent(typeof(BoxCollider))]
    public class RoomNode : MonoBehaviour
    {
        [Header("Puntos de Interés")]
        // Puntos específicos de patrullaje u observación dentro de esta sala
        public List<WayPointData> waypoints = new List<WayPointData>();

        [Header("Conexiones Lógicas")]
        // Habitaciones adyacentes a las que el agente puede transitar
        public List<RoomNode> connectedRooms = new List<RoomNode>();

        private BoxCollider _collider;

        private void Awake() 
        {
            _collider = GetComponent<BoxCollider>();

            // Poblar automáticamente la lista con los waypoints hijos, si está vacía
            if (waypoints.Count == 0) {
                foreach (Transform child in transform) {
                    WayPointData data = child.GetComponent<WayPointData>();
                    if (data != null) waypoints.Add(data);
                }
            }

            // Establecer conexiones bidireccionales automáticamente con habitaciones vecinas
            foreach (RoomNode neighbor in connectedRooms) {
                if (neighbor != null && !neighbor.connectedRooms.Contains(this)) {
                    neighbor.connectedRooms.Add(this);
                }
            }
        }

        // Devuelve una posición navegable dentro de la sala: primer waypoint o centro del collider
        public Vector3 GetNavigablePosition() {
            if (waypoints.Count > 0 && waypoints[0] != null)
                return waypoints[0].transform.position;
            if (_collider == null) _collider = GetComponent<BoxCollider>();
            return _collider != null ? _collider.bounds.center : transform.position;
        }

        // Dibuja en el editor las conexiones entre salas como líneas verdes
        private void OnDrawGizmos() 
        {
            if (_collider == null) _collider = GetComponent<BoxCollider>();
            if (_collider == null) return;

            Vector3 myCenter = _collider.bounds.center;

            Gizmos.color = Color.green;
            foreach (RoomNode neighbor in connectedRooms) {
                if (neighbor == null) continue;
                BoxCollider nCol = neighbor.GetComponent<BoxCollider>();
                if (nCol != null) Gizmos.DrawLine(myCenter, nCol.bounds.center);
            }
        }
    }
}