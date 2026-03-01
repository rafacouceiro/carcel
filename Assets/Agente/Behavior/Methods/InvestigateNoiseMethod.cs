using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class InvestigateNoiseMethod : IMethod {
        
        private readonly float _searchRadius = 15f; 
        private readonly int _maxPointsToInspect = 3; 
        private const float SearchSpeed = 4.0f;

        public bool CheckPreconditions(WorldState state) {
            return state.LastNoisePosition != Vector3.zero;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            subTasks.Enqueue(new ChangeFlashLight(Color.brown));

            // 2. Componer la ruta lógica usando el estado completo (para saber dónde estamos)
            List<Vector3> pointsToSearch = ComposeSearchRoute(state);

            // 3. Encolar los movimientos de búsqueda
            foreach (Vector3 point in pointsToSearch) {
                subTasks.Enqueue(new MoveTask(point, SearchSpeed));
            }

            // 4. Limpiar la memoria del ruido
            subTasks.Enqueue(new ClearNoiseTask());

            return subTasks;
        }

        /// <summary>
        /// Método orquestador: Obtiene puntos, los recorta al máximo permitido y los ordena Greedy.
        /// </summary>
        private List<Vector3> ComposeSearchRoute(WorldState state) {
            Vector3 noisePosition = state.LastNoisePosition;
            List<Vector3> rawPoints = new List<Vector3>();

            // 1. Buscar KeyPoints reales cerca del ruido (ignorando patrullas)
            List<Vector3> keyPoints = GetKeyPointsNearNoise(noisePosition);
            
            // 2. Barajar los KeyPoints para no elegir siempre los mismos si hay más del máximo
            ShuffleList(keyPoints);

            // 3. Añadir a la lista de candidatos hasta llegar al máximo deseado
            for (int i = 0; i < keyPoints.Count && rawPoints.Count < _maxPointsToInspect; i++) {
                rawPoints.Add(keyPoints[i]);
            }

            // 4. Si faltan puntos para completar la cuota, generamos aleatorios alrededor del ruido
            while (rawPoints.Count < _maxPointsToInspect) {
                Vector3 randomPoint = GetRandomNavMeshPoint(noisePosition, _searchRadius);
                rawPoints.Add(randomPoint);
            }

            // 5. ORDENAMIENTO GREEDY: Trazamos la ruta más eficiente desde donde está el guardia
            return CalculateGreedyRoute(state.CurrentPosition, rawPoints);
        }

        /// <summary>
        /// Algoritmo Greedy basado en la distancia real del NavMesh.
        /// </summary>
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
                    // Calculamos el camino real evitando obstáculos
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
                    currentPos = closestWp; // El guardia "camina" mentalmente aquí para el siguiente cálculo
                    remainingPoints.RemoveAt(closestIndex);
                } else {
                    // Si un punto es inalcanzable (muy raro con SamplePosition), se descarta
                    remainingPoints.RemoveAt(0);
                }
            }

            return route;
        }

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