using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Coordina un escaneo táctico alrededor de la fuente reciente de un ruido
    public class InvestigateNoiseMethod : IMethod {
        
        private readonly float _searchRadius = 15f; 
        private readonly int _maxPointsToInspect = 3; 
        private const float SearchSpeed = 4.0f;

        public bool CheckPreconditions(WorldState state) {
            // El ruido debe existir y ser relativamente reciente (menos de 10s)
            float age = Time.time - state.LastNoisePositionTime;
            return state.LastNoisePosition != Vector3.zero && age < 10f;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            // Luz marrón para indicar intriga/sospecha sonora
            subTasks.Enqueue(new ChangeFlashLight(Color.brown));

            // Mapea la zona de influencia del sonido en una ruta útil
            List<Vector3> pointsToSearch = ComposeSearchRoute(state);

            // Convierte en trayectos físicos
            foreach (Vector3 point in pointsToSearch) {
                subTasks.Enqueue(new MoveTask(point, SearchSpeed));
            }

            // Descarta la posición del ruido para no repetir
            subTasks.Enqueue(new ClearNoiseTask());

            return subTasks;
        }

        // --- Orquestador de Ruta ---
        // Adquiere posibles destinos alrededor del ruido, los recorta y ordena eficientemente
        private List<Vector3> ComposeSearchRoute(WorldState state) {
            Vector3 noisePosition = state.LastNoisePosition;
            List<Vector3> rawPoints = new List<Vector3>();

            // 1. Identifica escondites estáticos en las proximidades
            List<Vector3> keyPoints = GetKeyPointsNearNoise(noisePosition);
            
            // 2. Baraja aleatoriamente para evitar rutinas predictibles
            ShuffleList(keyPoints);

            // 3. Añade candidatos de escondites al listado de revisión hasta el límite
            for (int i = 0; i < keyPoints.Count && rawPoints.Count < _maxPointsToInspect; i++) {
                rawPoints.Add(keyPoints[i]);
            }

            // 4. Agrega puntos muertos de patrullaje para alcanzar la cuota
            while (rawPoints.Count < _maxPointsToInspect) {
                Vector3 randomPoint = GetRandomNavMeshPoint(noisePosition, _searchRadius);
                rawPoints.Add(randomPoint);
            }

            // 5. Calcula la cadena de saltos más corta para este conjunto
            return CalculateGreedyRoute(state.CurrentPosition, rawPoints);
        }

        // Trazado de ruta usando Greedy basado estrictamente en topología NavMesh
        private List<Vector3> CalculateGreedyRoute(Vector3 startPos, List<Vector3> unvisitedPoints) {
            List<Vector3> route = new List<Vector3>();
            Vector3 currentPos = startPos;
            List<Vector3> remainingPoints = new List<Vector3>(unvisitedPoints);
            NavMeshPath path = new NavMeshPath();

            while (remainingPoints.Count > 0) {
                Vector3 closestWp = Vector3.zero;
                float minNavDistance = Mathf.Infinity;
                int closestIndex = -1;
                bool foundPath = false;

                for (int i = 0; i < remainingPoints.Count; i++) {
                    // Calcular ruta con el NavMesh esquivando bloqueos
                    if (NavMesh.CalculatePath(currentPos, remainingPoints[i], NavMesh.AllAreas, path)) {
                        
                        float pathLength = CalculatePathLength(path);

                        if (pathLength < minNavDistance) {
                            minNavDistance = pathLength;
                            closestWp = remainingPoints[i];
                            closestIndex = i;
                            foundPath = true;
                        }
                    }
                }

                if (foundPath) {
                    route.Add(closestWp);
                    currentPos = closestWp; // Simula que ya avanzó para calcular a futuro
                    remainingPoints.RemoveAt(closestIndex);
                } else {
                    // Exclusión de puntos topológicamente aislados
                    remainingPoints.RemoveAt(0);
                }
            }

            return route;
        }

        // Rastrea escondites clasificados cercanos al origen sonoro
        private List<Vector3> GetKeyPointsNearNoise(Vector3 noisePosition) {
            List<Vector3> validPoints = new List<Vector3>();

            foreach (RoomNode room in PrisonMap.Instance.GetAllNodes()) {
                if (room.waypoints == null) continue;

                foreach (WayPointData wp in room.waypoints) {
                    if (wp != null && wp.isKeyPoint && !wp.isPatrolCheckpoint) {
                        float distance = Vector3.Distance(noisePosition, wp.transform.position);
                        if (distance <= _searchRadius) {
                            validPoints.Add(wp.transform.position);
                        }
                    }
                }
            }
            return validPoints;
        }

        // Localiza un punto transitable de relleno aleatorio
        private Vector3 GetRandomNavMeshPoint(Vector3 center, float radius) {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += center;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 2f, NavMesh.AllAreas)) {
                return hit.position;
            }
            return center; 
        }

        private void ShuffleList(List<Vector3> list) {
            for (int i = 0; i < list.Count; i++) {
                Vector3 temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        private float CalculatePathLength(NavMeshPath path) {
            if (path.corners.Length < 2) return 0f;

            float length = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++) {
                length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            return length;
        }
    }
}