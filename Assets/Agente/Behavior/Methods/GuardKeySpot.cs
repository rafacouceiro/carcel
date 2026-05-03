using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {
    // Método HTN: Dirige al guardia al punto crucial más cercano para vigilar mientras recarga energía
    public class GuardKeySpotMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            // Condicionado a no estar persiguiendo activamente al fugitivo
            return !state.FugitiveInVision; 
        }

        public Queue<ITask> Decompose(WorldState state) {
            Queue<ITask> subTasks = new Queue<ITask>();
            // Luz morada indica estado de guardia estática
            subTasks.Enqueue(new ChangeFlashLight(Color.purple));
            
            WayPointData closestRestPoint = FindClosestKeyPoint(state.CurrentPosition);
            
            if (closestRestPoint != null) {
                // Navegar asumiendo paso de vigilancia normal
                subTasks.Enqueue(new MoveTask(closestRestPoint.transform.position, 3.0f));
                
                // Inspeccionar el área tantas veces como sea necesario para recuperarse al 100%
                int tasksNeeded = Mathf.CeilToInt((100f - state.Energy) / 20f);

                for (int i = 0; i < tasksNeeded; i++) {
                    subTasks.Enqueue(new LookAroundTask());
                }
            }

            return subTasks;
        }

        // Calcula el puesto de guardia más próximo usando distancias reales del NavMesh
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