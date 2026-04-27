using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Physical;
using System.Linq;

namespace AgenticPrison.Core {

    public class PrisonMap : MonoBehaviour {
        
        public static PrisonMap Instance { get; private set; }

        private Dictionary<string, List<RoomNode>> _sections = new Dictionary<string, List<RoomNode>>();
        private List<RoomNode> _allNodes = new List<RoomNode>();

        private Dictionary<string, List<RoomNode>> _searchSectors = new Dictionary<string, List<RoomNode>>();
        private Dictionary<string, Dictionary<string, List<WayPointData>>> _sectorBlockingGroups = new Dictionary<string, Dictionary<string, List<WayPointData>>>();

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;

            InitializeMapData();
        }

        private void InitializeMapData() {
            _allNodes.Clear();
            _searchSectors.Clear();
            _sectorBlockingGroups.Clear();

            foreach (Transform section in transform) {
                string sectionName = section.name; 
                var rooms = new List<RoomNode>(section.GetComponentsInChildren<RoomNode>());
                
                _sections[sectionName] = rooms;
                _allNodes.AddRange(rooms);

                foreach (var room in rooms) {
                    if (room.searchSectorIds == null) continue;

                    foreach (string sectorId in room.searchSectorIds) {
                        if (string.IsNullOrEmpty(sectorId)) continue;

                        if (!_searchSectors.ContainsKey(sectorId)) {
                            _searchSectors[sectorId] = new List<RoomNode>();
                            _sectorBlockingGroups[sectorId] = new Dictionary<string, List<WayPointData>>();
                        }
                        
                        if (!_searchSectors[sectorId].Contains(room)) {
                            _searchSectors[sectorId].Add(room);
                        }

                        if (room.waypoints != null) {
                            foreach (var wp in room.waypoints) {
                                if (wp != null && wp.isBlockingPoint) {
                                    string groupId = string.IsNullOrEmpty(wp.blockingGroupId) ? "DefaultGroup" : wp.blockingGroupId;

                                    if (!_sectorBlockingGroups[sectorId].ContainsKey(groupId)) {
                                        _sectorBlockingGroups[sectorId][groupId] = new List<WayPointData>();
                                    }

                                    if (!_sectorBlockingGroups[sectorId][groupId].Contains(wp)) {
                                        _sectorBlockingGroups[sectorId][groupId].Add(wp);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public List<RoomNode> GetSection(string sectionId) {
            if (_sections.TryGetValue(sectionId, out var rooms)) return rooms;
            return new List<RoomNode>();
        }

        // 2.1) Determina la habitación de una coordenada. Usa NavMesh como fallback.
        public RoomNode GetCurrentNode(Vector3 position) {
            RoomNode closestRoom = null;
            float minDistance = Mathf.Infinity;

            // 1. Comprobar si está perfectamente dentro de un BoxCollider (O(N) rápido)
            foreach (var room in _allNodes) {
                BoxCollider col = room.GetComponent<BoxCollider>();
                if (col != null && col.bounds.Contains(position)) return room;
            }

            // 2. Si no está en ningún collider, buscar la más cercana por NavMesh
            foreach (var room in _allNodes) {
                float dist = GetNavMeshDistance(position, room.GetNavigablePosition());
                if (dist < minDistance) {
                    minDistance = dist;
                    closestRoom = room;
                }
            }

            return closestRoom;
        }

        // 2.1) Obtiene la lista de sectores a los que pertenece la posición
        public List<string> GetFugitiveSectors(Vector3 position) {
            RoomNode node = GetCurrentNode(position);
            if (node != null && node.searchSectorIds != null) {
                return node.searchSectorIds;
            }
            return new List<string>();
        }

        // 2.2.a) Obtiene los puntos de bloqueo para cerrar un sector, agrupados
        public Dictionary<string, List<WayPointData>> GetBlockingGroupsForSector(string sectorId) {
            if (_sectorBlockingGroups.TryGetValue(sectorId, out var groups)) {
                return groups;
            }
            return new Dictionary<string, List<WayPointData>>();
        }

        // 2.2.b) Obtiene los roomnodes de rastreo. Excluye las salas compartidas.
        public List<RoomNode> GetSweepRoomsForSector(string sectorId) {
            if (_searchSectors.TryGetValue(sectorId, out var rooms)) {
                return rooms.Where(r => r.searchSectorIds.Count == 1).ToList();
            }
            return new List<RoomNode>();
        }

        // 2.3) Calcula la distancia por NavMesh desde una posición a una habitación específica
        public float GetDistanceToRoom(Vector3 currentPosition, RoomNode room) {
            if (room == null) return Mathf.Infinity;
            return GetNavMeshDistance(currentPosition, room.GetNavigablePosition());
        }

        // Helper interno para el cálculo de distancias reales en el mapa
        private float GetNavMeshDistance(Vector3 start, Vector3 target) {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(start, target, NavMesh.AllAreas, path)) {
                float distance = 0f;
                for (int i = 0; i < path.corners.Length - 1; i++) {
                    distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
                }
                return distance;
            }
            return Mathf.Infinity; // Ruta no viable
        }

        public List<RoomNode> GetAllNodes() => _allNodes;

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