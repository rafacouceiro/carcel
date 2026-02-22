using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Core.Math;

namespace AgenticPrison.Physical {

    // Ya NO hereda de MonoBehaviour. Es una clase pura de C#.
    public class MapManager : IMapProvider {
        
        // --- PATRÓN SINGLETON (El servicio global) ---
        private static MapManager _instance;
        public static MapManager Instance {
            get {
                if (_instance == null) {
                    _instance = new MapManager();
                    _instance.InitializeMap(); // Se calcula automáticamente la primera vez
                }
                return _instance;
            }
        }

        private Dictionary<string, ZoneData> _zonesGraph = new Dictionary<string, ZoneData>();
        private Dictionary<(string, string), float> _distanceTable = new Dictionary<(string, string), float>();

        // Constructor privado para obligar a usar "Instance"
        private MapManager() { }

        private void InitializeMap() {
            BuildMapKnowledge();
            CalculateAllNavMeshDistances();
        }

        private void BuildMapKnowledge() 
        {
            LocationNode[] unityNodes = Object.FindObjectsOfType<LocationNode>();
            foreach (var node in unityNodes) {
                
                string uniqueId = node.gameObject.name; 

                if (_zonesGraph.ContainsKey(uniqueId)) {
                    uniqueId = uniqueId + "_" + UnityEngine.Random.Range(1000, 9999).ToString(); 
                }

                // --- EL ARREGLO DE LA DISTANCIA ---
                // Cogemos el centro EXACTO del collider en el mundo 3D, sin importar dónde esté el Transform
                BoxCollider collider = node.GetComponent<BoxCollider>();
                Vector3 centroReal = collider != null ? collider.bounds.center : node.transform.position;

                var zoneData = new ZoneData {
                    Id = uniqueId, 
                    IsExit = node.isExit,
                    Center = new Position3D(centroReal.x, centroReal.y, centroReal.z) // ¡Ahora sí es exacto!
                };

                foreach (Vector3 p in node.GetGeneratedPoints()) {
                    zoneData.PatrolPoints.Add(new Position3D(p.x, p.y, p.z));
                }

                _zonesGraph.Add(zoneData.Id, zoneData);
            }
        }

        private void CalculateAllNavMeshDistances() {
            List<ZoneData> allZones = new List<ZoneData>(_zonesGraph.Values);

            for (int i = 0; i < allZones.Count; i++) {
                for (int j = 0; j < allZones.Count; j++) {
                    if (i == j) {
                        _distanceTable[(allZones[i].Id, allZones[j].Id)] = 0f;
                        continue;
                    }

                    NavMeshPath path = new NavMeshPath();
                    Vector3 startPos = new Vector3(allZones[i].Center.X, allZones[i].Center.Y, allZones[i].Center.Z);
                    Vector3 endPos = new Vector3(allZones[j].Center.X, allZones[j].Center.Y, allZones[j].Center.Z);

                    if (NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, path)) {
                        float distance = GetPathLength(path);
                        _distanceTable[(allZones[i].Id, allZones[j].Id)] = distance;
                    } else {
                        _distanceTable[(allZones[i].Id, allZones[j].Id)] = float.MaxValue; 
                    }
                }
            }
            Debug.Log($"MapManager: Distancias calculadas para {_zonesGraph.Count} zonas sin necesidad de objetos en escena.");
        }

        private float GetPathLength(NavMeshPath path) {
            float length = 0.0f;
            if (path.corners.Length < 2) return length;
            for (int i = 0; i < path.corners.Length - 1; i++) {
                length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            return length;
        }

        public List<ZoneData> GetAllZones() => new List<ZoneData>(_zonesGraph.Values);
        public ZoneData GetZone(string zoneId) => _zonesGraph.TryGetValue(zoneId, out ZoneData data) ? data : null;
        public float GetPathDistance(string fromZoneId, string toZoneId) {
            if (_distanceTable.TryGetValue((fromZoneId, toZoneId), out float distance)) {
                return distance;
            }
            return float.MaxValue; 
        }
    }
}