using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Physical;

namespace AgenticPrison.Core {

    public class PrisonMap : MonoBehaviour {
        
        // --- EL SINGLETON ---
        public static PrisonMap Instance { get; private set; }

        private Dictionary<string, List<RoomNode>> _sections = new Dictionary<string, List<RoomNode>>();
        private List<RoomNode> _allNodes = new List<RoomNode>();

        private void Awake() {
            // Configuración del Singleton
            if (Instance != null && Instance != this) {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;

            // Recorre los hijos (section1, section2...)
            foreach (Transform section in transform) {
                string sectionName = section.name; // Coge el nombre automáticamente
                var rooms = new List<RoomNode>(section.GetComponentsInChildren<RoomNode>());
                
                _sections[sectionName] = rooms;
                _allNodes.AddRange(rooms);
            }
            Debug.Log($"[PrisonMap] Mapa cargado. {_sections.Count} secciones y {_allNodes.Count} salas.");
        }

        public List<RoomNode> GetSection(string sectionId) {
            if (_sections.TryGetValue(sectionId, out var rooms)) return rooms;
            return new List<RoomNode>();
        }

        public RoomNode GetCurrentNode(Vector3 position) {
            RoomNode closestRoom = null;
            float minDistance = Mathf.Infinity;

            foreach (var room in _allNodes) {
                BoxCollider col = room.GetComponent<BoxCollider>();
                if (col == null) continue;

                if (col.bounds.Contains(position)) return room;

                float dist = Vector3.Distance(position, col.ClosestPoint(position));
                if (dist < minDistance) {
                    minDistance = dist;
                    closestRoom = room;
                }
            }
            return closestRoom;
        }

        public List<RoomNode> GetAllNodes() => _allNodes;

        public List<WayPointData> GetAllKeyPoints() {
            List<WayPointData> keyPoints = new List<WayPointData>();

            // Recorremos todas las habitaciones del mapa
            foreach (RoomNode room in allNodes) {
                if (room == null) continue;

                // Filtramos los waypoints de cada habitación
                foreach (WayPointData wp in room.waypoints) {
                    if (wp != null && wp.isKeyPoint) {
                        keyPoints.Add(wp);
                    }
                }
            }
            return keyPoints;
        }
    }
}