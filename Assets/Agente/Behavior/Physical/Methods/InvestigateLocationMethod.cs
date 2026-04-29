using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Establece una ruta óptima para inspeccionar todos los puntos clave disponibles
    public class InvestigateLocationMethod : IMethod {

        private const float SearchSpeed = 4.0f;

        public bool CheckPreconditions(WorldState state) {
            // Ejecutable cuando se confirma empíricamente que el preso escapó y no se conoce su sector
            return !state.PrisonerInCell && state.FugitiveSectorId == "[UNK]";
        }

        public Queue<ITask> Decompose(WorldState state) {
            Queue<ITask> subTasks = new Queue<ITask>();
            
            // Luz amarilla indica alerta general de búsqueda
            subTasks.Enqueue(new ChangeFlashLight(Color.yellow));
            
            List<WayPointData> keyPoints = PrisonMap.Instance.GetAllKeyPoints();
            if (keyPoints == null || keyPoints.Count == 0) return subTasks;

            // Planifica la ruta uniendo todos los nodos de forma eficiente
            List<Vector3> optimizedRoute = CalculateGreedyRoute(state.CurrentPosition, keyPoints);

            // Convierte cada salto de la ruta en un MoveTask
            foreach (Vector3 destination in optimizedRoute) {
                subTasks.Enqueue(new MoveTask(destination, SearchSpeed));
            }

            return subTasks;
        }

        // --- Algoritmo de planificación de ruta Greedy (Vecino más cercano) ---
        // Construye un circuito visitando el punto de interés accesible más cercano en cada salto
        private List<Vector3> CalculateGreedyRoute(Vector3 startPos, List<WayPointData> unvisitedPoints) {
            List<Vector3> route = new List<Vector3>();
            Vector3 currentPos = startPos;
            NavMeshPath path = new NavMeshPath();

            // Lista auxiliar para descartar destinos secuencialmente
            List<WayPointData> remainingPoints = new List<WayPointData>(unvisitedPoints);

            while (remainingPoints.Count > 0) {
                WayPointData closestWp = null;
                float minNavDistance = Mathf.Infinity;
                int closestIndex = -1;

                for (int i = 0; i < remainingPoints.Count; i++) {
                    // Evalúa coste de desplazamiento usando física real del NavMesh
                    if (NavMesh.CalculatePath(currentPos, remainingPoints[i].transform.position, NavMesh.AllAreas, path)) {
                        
                        float pathLength = CalculatePathLength(path);

                        if (pathLength < minNavDistance) {
                            minNavDistance = pathLength;
                            closestWp = remainingPoints[i];
                            closestIndex = i;
                        }
                    }
                }

                if (closestWp != null) {
                    route.Add(closestWp.transform.position);
                    currentPos = closestWp.transform.position; // Se proyecta al nuevo origen
                    remainingPoints.RemoveAt(closestIndex);
                } else {
                    // Descarta el nodo primario si es totalmente inalcanzable
                    remainingPoints.RemoveAt(0);
                }
            }

            return route;
        }

        // Determina la longitud sumando vectores del recorrido NavMesh
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