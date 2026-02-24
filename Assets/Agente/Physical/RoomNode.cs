using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

namespace AgenticPrison.Physical {

    [RequireComponent(typeof(BoxCollider))]
    public class RoomNode : MonoBehaviour
    {
        [Header("Puntos a vigilar (Waypoints)")]
        public List<Transform> waypoints = new List<Transform>();

        [Header("Conexiones (Puertas)")]
        [Tooltip("Arrastra aquí las salas vecinas.")]
        public List<RoomNode> connectedRooms = new List<RoomNode>();

        private BoxCollider _collider;

        private void Awake() 
        {
            _collider = GetComponent<BoxCollider>();

            // 1. Autocompletar waypoints si la lista está vacía (coge a los hijos)
            if (waypoints.Count == 0) {
                foreach (Transform child in transform) {
                    waypoints.Add(child);
                }
            }

            // 2. Automatizar la conexión mutua
            foreach (RoomNode neighbor in connectedRooms) 
            {
                if (neighbor != null && !neighbor.connectedRooms.Contains(this)) 
                {
                    neighbor.connectedRooms.Add(this);
                }
            }
        }

        // Dibujamos el grafo en la escena
        private void OnDrawGizmos() 
        {
            if (_collider == null) _collider = GetComponent<BoxCollider>();
            if (_collider == null) return;

            Vector3 myCenter = _collider.bounds.center;

            // Dibujar el centro de la sala
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(myCenter, 0.5f);

            // Dibujar los Waypoints (líneas amarillas)
            if (waypoints != null) {
                Gizmos.color = Color.yellow;
                foreach (Transform wp in waypoints) {
                    if (wp != null) {
                        Gizmos.DrawLine(myCenter, wp.position);
                        Gizmos.DrawCube(wp.position, new Vector3(0.2f, 0.2f, 0.2f));
                    }
                }
            }

            // Dibujar conexiones
            if (connectedRooms == null) return;

            Gizmos.color = Color.green;
            foreach (RoomNode neighbor in connectedRooms) 
            {
                if (neighbor != null) 
                {
                    BoxCollider neighborCollider = neighbor.GetComponent<BoxCollider>();
                    if (neighborCollider != null)
                    {
                        Vector3 neighborCenter = neighborCollider.bounds.center;
                        NavMeshPath path = new NavMeshPath();
                        
                        if (NavMesh.CalculatePath(myCenter, neighborCenter, NavMesh.AllAreas, path)) {
                            for (int i = 0; i < path.corners.Length - 1; i++) {
                                Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
                            }
                        } else {
                            Gizmos.color = Color.red;
                            Gizmos.DrawLine(myCenter, neighborCenter);
                            Gizmos.color = Color.green;
                        }
                    }
                }
            }
        }
    }
}