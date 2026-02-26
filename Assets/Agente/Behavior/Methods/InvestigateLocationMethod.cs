using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // Imprescindible para el cálculo de caminos
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class InvestigateLocationMethod : IMethod {

        public bool CheckPreconditions(WorldState state) {
            // Ejecutable si sabemos que el preso no está en la celda
            return !state.PrisonerInCell; 
        }

        public Queue<ITask> Decompose(WorldState state) {
            Queue<ITask> subTasks = new Queue<ITask>();
            
            // 1. Obtenemos los puntos de interés del mapa simbólico
            List<WayPointData> unvisitedPoints = PrisonMap.Instance.GetAllKeyPoints();

            if (unvisitedPoints == null || unvisitedPoints.Count == 0) return subTasks;

            Vector3 planningPos = state.CurrentPosition;
            float searchSpeed = 4.0f; 
            
            // Instanciamos el objeto Path una vez para reutilizarlo en el bucle
            NavMeshPath path = new NavMeshPath();

            // 2. Algoritmo Greedy (Vecino más cercano) basado en NavMesh
            while (unvisitedPoints.Count > 0) {
                WayPointData closestWp = null;
                float minNavDistance = Mathf.Infinity;
                int closestIndex = -1;

                for (int i = 0; i < unvisitedPoints.Count; i++) {
                    // Calculamos el camino real ignorando al fugitivo
                    if (NavMesh.CalculatePath(planningPos, unvisitedPoints[i].transform.position, NavMesh.AllAreas, path)) {
                        
                        float pathLength = CalculatePathLength(path);

                        if (pathLength < minNavDistance) {
                            minNavDistance = pathLength;
                            closestWp = unvisitedPoints[i];
                            closestIndex = i;
                        }
                    }
                }

                if (closestWp != null) {
                    // 3. Generamos la intención: moverse al punto óptimo detectado
                    subTasks.Enqueue(new MoveTask(closestWp.transform.position, searchSpeed));

                    // Actualizamos la posición virtual de planificación para el siguiente salto
                    planningPos = closestWp.transform.position;
                    unvisitedPoints.RemoveAt(closestIndex);
                } else {
                    // Si un punto es inalcanzable por NavMesh, lo descartamos
                    unvisitedPoints.RemoveAt(0);
                }
            }

            return subTasks;
        }

        /// <summary>
        /// Calcula la longitud total sumando los segmentos entre las esquinas (corners) del camino.
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