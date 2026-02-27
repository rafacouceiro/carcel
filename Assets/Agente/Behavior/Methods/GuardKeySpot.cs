using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {
    public class GuardKeySpotMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            // El agente decide descansar si su fatiga es alta
            return !state.FugitiveInVision; 
        }

        public Queue<ITask> Decompose(WorldState state) {
            Queue<ITask> subTasks = new Queue<ITask>();
            subTasks.Enqueue(new ChangeFlashLight(Color.purple));
            
            WayPointData closestRestPoint = FindClosestKeyPoint(state.CurrentPosition);
            
            if (closestRestPoint != null) {
                subTasks.Enqueue(new MoveTask(closestRestPoint.transform.position, 3.0f));
                int tasksNeeded = Mathf.CeilToInt((100f - state.Energy) / 20f);

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