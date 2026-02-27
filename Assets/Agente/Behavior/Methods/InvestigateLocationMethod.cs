using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class InvestigateLocationMethod : IMethod {

        private const float SearchSpeed = 4.0f;

        public bool CheckPreconditions(WorldState state) {
            // Ejecutable si sabemos que el preso no está en la celda
            return !state.PrisonerInCell && state.LastKnownPosition == Vector3.zero; 
        }

        public Queue<ITask> Decompose(WorldState state) {
            Queue<ITask> subTasks = new Queue<ITask>();
            
            subTasks.Enqueue(new ChangeFlashLight(Color.yellow));
            
            // Obtener puntos de interés
            List<WayPointData> keyPoints = PrisonMap.Instance.GetAllKeyPoints();
            if (keyPoints == null || keyPoints.Count == 0) return subTasks;

            List<Vector3> optimizedRoute = CalculateGreedyRoute(state.CurrentPosition, keyPoints);

            // Transformar la ruta calculada en tareas primitivas
            foreach (Vector3 destination in optimizedRoute) {
                subTasks.Enqueue(new MoveTask(destination, SearchSpeed));
            }

            return subTasks;
        }

        /// <summary>
        /// Algoritmo Greedy (Vecino más cercano) para ordenar los puntos a visitar 
        /// usando distancias reales del NavMesh.
        /// </summary>
        private List<Vector3> CalculateGreedyRoute(Vector3 startPos, List<WayPointData> unvisitedPoints) {
            List<Vector3> route = new List<Vector3>();
            Vector3 currentPos = startPos;
            NavMeshPath path = new NavMeshPath();

            // Clonamos la lista para ir eliminando elementos sin afectar al mapa original
            List<WayPointData> remainingPoints = new List<WayPointData>(unvisitedPoints);

            while (remainingPoints.Count > 0) {
                WayPointData closestWp = null;
                float minNavDistance = Mathf.Infinity;
                int closestIndex = -1;

                for (int i = 0; i < remainingPoints.Count; i++) {
                    // Calculamos el camino real ignorando al fugitivo
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
                    currentPos = closestWp.transform.position; // Actualizamos para el siguiente salto
                    remainingPoints.RemoveAt(closestIndex);
                } else {
                    // Si un punto es inalcanzable por NavMesh, lo descartamos
                    remainingPoints.RemoveAt(0);
                }
            }

            return route;
        }

        /// <summary>
        /// Calcula la longitud total sumando los segmentos entre las esquinas del camino.
        /// </summary>
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