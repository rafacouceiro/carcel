using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

namespace AgenticPrison.Physical {

    [RequireComponent(typeof(BoxCollider))]
    public class RoomNode : MonoBehaviour
    {
        [Header("Puntos Enriquecidos")]
        // Cambiamos Transform por WaypointData
        public List<WayPointData> waypoints = new List<WayPointData>();

        [Header("Conexiones")]
        public List<RoomNode> connectedRooms = new List<RoomNode>();

        private BoxCollider _collider;

        private void Awake() 
        {
            _collider = GetComponent<BoxCollider>();

            // Autocompletar buscando el componente específico en los hijos
            if (waypoints.Count == 0) {
                foreach (Transform child in transform) {
                    WayPointData data = child.GetComponent<WayPointData>();
                    if (data != null) waypoints.Add(data);
                }
            }

            // Automatizar conexión mutua (se mantiene igual)
            foreach (RoomNode neighbor in connectedRooms) {
                if (neighbor != null && !neighbor.connectedRooms.Contains(this)) {
                    neighbor.connectedRooms.Add(this);
                }
            }
        }

        private void OnDrawGizmos() 
        {
            if (_collider == null) _collider = GetComponent<BoxCollider>();
            if (_collider == null) return;

            Vector3 myCenter = _collider.bounds.center;

            // Grafo de conexiones (verde)
            Gizmos.color = Color.green;
            foreach (RoomNode neighbor in connectedRooms) {
                if (neighbor == null) continue;
                BoxCollider nCol = neighbor.GetComponent<BoxCollider>();
                if (nCol != null) Gizmos.DrawLine(myCenter, nCol.bounds.center);
            }
        }
    }
}