using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {
    public class TakeBreakMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            // El agente decide descansar si su fatiga es alta
            return true; 
        }

        public Queue<ITask> Decompose(WorldState state) {
            Queue<ITask> subTasks = new Queue<ITask>();
            subTasks.Enqueue(new ChangeFlashLight(Color.green))
            
            // 1. Encontrar el KeyPoint más cercano mediante NavMesh
            WayPointData closestRestPoint = FindClosestKeyPoint(state.CurrentPosition);
            
            if (closestRestPoint != null) {
                // 2. Añadir tarea de movimiento
                subTasks.Enqueue(new MoveTask(closestRestPoint.transform.position, 3.5f));

                // 3. CALCULAR LOOKAROUNDS: Cada una recupera 0.2
                // Si la fatiga es 0.8, necesitamos 4 tareas (0.8 / 0.2 = 4)
                int tasksNeeded = Mathf.CeilToInt(state.Fatigue / 0.2f);

                for (int i = 0; i < tasksNeeded; i++) {
                    subTasks.Enqueue(new LookAroundTask());
                }
            }

            return subTasks;
        }

        private WayPointData FindClosestKeyPoint(Vector3 currentPos) {
            List<WayPointData> keyPoints = PrisonMap.Instance.GetAllKeyPoints();
            WayPointData bestPoint = null;
            float minDistance = Mathf.Infinity;
            NavMeshPath path = new NavMeshPath();

            foreach (var wp in keyPoints) {
                if (NavMesh.CalculatePath(currentPos, wp.transform.position, NavMesh.AllAreas, path)) {
                    float dist = CalculatePathLength(path);
                    if (dist < minDistance) {
                        minDistance = dist;
                        bestPoint = wp;
                    }
                }
            }
            return bestPoint;
        }

        private float CalculatePathLength(NavMeshPath path) {
            float length = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++) {
                length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            return length;
        }
    }
}