using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Core.Math;

namespace AgenticPrison.Physical {

    public class MapManager : MonoBehaviour, IMapProvider {
        
        private Dictionary<string, ZoneData> _zonesGraph = new Dictionary<string, ZoneData>();
        
        // Tabla para guardar las distancias reales: Key = (ZonaA, ZonaB), Value = Distancia
        private Dictionary<(string, string), float> _distanceTable = new Dictionary<(string, string), float>();

        private void Awake() {
            BuildMapKnowledge();
            CalculateAllNavMeshDistances(); // Calculamos el laberinto
        }

        private void BuildMapKnowledge() {
            LocationNode[] unityNodes = FindObjectsOfType<LocationNode>();
            foreach (var node in unityNodes) {
                var zoneData = new ZoneData {
                    Id = node.zoneName,
                    Center = new Position3D(node.transform.position.x, node.transform.position.y, node.transform.position.z)
                };
                _zonesGraph.Add(zoneData.Id, zoneData);
            }
        }

        // --- LA MAGIA DEL NAVMESH ---
        private void CalculateAllNavMeshDistances() {
            List<ZoneData> allZones = new List<ZoneData>(_zonesGraph.Values);

            for (int i = 0; i < allZones.Count; i++) {
                for (int j = 0; j < allZones.Count; j++) {
                    if (i == j) {
                        _distanceTable[(allZones[i].Id, allZones[j].Id)] = 0f;
                        continue;
                    }

                    // Pedimos a Unity que calcule la ruta en el NavMesh
                    NavMeshPath path = new NavMeshPath();
                    Vector3 startPos = new Vector3(allZones[i].Center.X, allZones[i].Center.Y, allZones[i].Center.Z);
                    Vector3 endPos = new Vector3(allZones[j].Center.X, allZones[j].Center.Y, allZones[j].Center.Z);

                    if (NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, path)) {
                        float distance = GetPathLength(path);
                        _distanceTable[(allZones[i].Id, allZones[j].Id)] = distance;
                    } else {
                        // Si no hay camino posible (ej. puerta bloqueada), ponemos infinito
                        _distanceTable[(allZones[i].Id, allZones[j].Id)] = float.MaxValue; 
                    }
                }
            }
            Debug.Log("MapManager: Distancias de NavMesh pre-calculadas.");
        }

        // Función auxiliar para sumar los segmentos de la ruta del NavMesh
        private float GetPathLength(NavMeshPath path) {
            float length = 0.0f;
            if (path.corners.Length < 2) return length;

            for (int i = 0; i < path.corners.Length - 1; i++) {
                length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            return length;
        }

        // --- Implementación de IMapProvider ---
        
        public List<ZoneData> GetAllZones() => new List<ZoneData>(_zonesGraph.Values);
        public ZoneData GetZone(string zoneId) => _zonesGraph.TryGetValue(zoneId, out ZoneData data) ? data : null;

        // El HTN llama a esto. ¡Es instantáneo porque ya está calculado!
        public float GetPathDistance(string fromZoneId, string toZoneId) {
            if (_distanceTable.TryGetValue((fromZoneId, toZoneId), out float distance)) {
                return distance;
            }
            return float.MaxValue; 
        }
    }
}