using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Physical;

namespace AgenticPrison.Core {

    // Clase administradora global del mapa, las secciones y las celdas
    public class PrisonMap : MonoBehaviour {
        
        // --- Patrón Singleton para acceso global ---
        public static PrisonMap Instance { get; private set; }

        private Dictionary<string, List<RoomNode>> _sections = new Dictionary<string, List<RoomNode>>();
        private List<RoomNode> _allNodes = new List<RoomNode>();

        private void Awake() {
            // Inicialización del Singleton
            if (Instance != null && Instance != this) {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;

            // Almacenar dinámicamente cada cuadrante y los nodos (habitaciones) que contiene
            foreach (Transform section in transform) {
                string sectionName = section.name; 
                var rooms = new List<RoomNode>(section.GetComponentsInChildren<RoomNode>());
                
                _sections[sectionName] = rooms;
                _allNodes.AddRange(rooms);
            }
            Debug.Log($"[PrisonMap] Mapa cargado. {_sections.Count} secciones y {_allNodes.Count} salas.");
        }

        // Obtiene todas las habitaciones de una sección específica del mapa
        public List<RoomNode> GetSection(string sectionId) {
            if (_sections.TryGetValue(sectionId, out var rooms)) return rooms;
            return new List<RoomNode>();
        }

        // Determina en qué habitación lógica se encuentra una coordenada dada
        public RoomNode GetCurrentNode(Vector3 position) {
            RoomNode closestRoom = null;
            float minDistance = Mathf.Infinity;

            foreach (var room in _allNodes) {
                BoxCollider col = room.GetComponent<BoxCollider>();
                if (col == null) continue;

                // Comprobar si está perfectamente dentro
                if (col.bounds.Contains(position)) return room;

                // Buscar la más cercana en caso de estar en los márgenes
                float dist = Vector3.Distance(position, col.ClosestPoint(position));
                if (dist < minDistance) {
                    minDistance = dist;
                    closestRoom = room;
                }
            }
            return closestRoom;
        }

        public List<RoomNode> GetAllNodes() => _allNodes;

        // Extrae todos los puntos de interés ("KeyPoints") del mapa entero
        public List<WayPointData> GetAllKeyPoints() {
            List<WayPointData> keyPoints = new List<WayPointData>();

            WayPointData[] allWaypointsInMap = GetComponentsInChildren<WayPointData>();

            foreach (WayPointData wp in allWaypointsInMap) {
                if (wp != null && wp.isKeyPoint) {
                    keyPoints.Add(wp);
                }
            }
            
            return keyPoints;
        }

        // Extrae todos los waypoints ubicados en el interior de las celdas de aislamiento
        public List<WayPointData> GetAllCellPoints() {
            List<WayPointData> cellPoints = new List<WayPointData>();

            WayPointData[] allWaypointsInMap = GetComponentsInChildren<WayPointData>();

            foreach (WayPointData wp in allWaypointsInMap) {
                if (wp != null && wp.isCell) {
                    cellPoints.Add(wp);
                }
            }
            
            return cellPoints;
        }
    }
}