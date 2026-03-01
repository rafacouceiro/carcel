using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class PredictivePursuitMethod : IMethod {
        
        private readonly float _maxSearchRadius = 15f; 
        private const float SearchSpeed = 6.5f;

        public bool CheckPreconditions(WorldState state) {
            // Condición: La pista visual tiene menos de 2 segundos de antigüedad
            bool isFresh = (Time.time - state.LastKnownPositionTime) < 2f;
            return state.LastKnownPosition != Vector3.zero && !state.PrisonerInCell && isFresh;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            subTasks.Enqueue(new ChangeFlashLight(Color.blue));
            subTasks.Enqueue(new MoveTask(state.LastKnownPosition, SearchSpeed)); 

            List<Transform> pointsToSearch = ComposeSearchRoute(state);

            foreach (Transform wp in pointsToSearch) {
                subTasks.Enqueue(new MoveTask(wp.position, SearchSpeed));
            }

            return subTasks;
        }

        private List<Transform> ComposeSearchRoute(WorldState state) {
            RoomNode chosenRoom = ChooseSearchRoom(state);
            if (chosenRoom == null || chosenRoom.waypoints == null) return new List<Transform>();
            return SortWaypointsByProximity(chosenRoom.waypoints, state.LastKnownPosition);
        }

        private RoomNode ChooseSearchRoom(WorldState state) {
            RoomNode lkpRoom = state.Map.GetCurrentNode(state.LastKnownPosition);
            if (lkpRoom == null) return null;
            
            List<RoomNode> candidates = new List<RoomNode> { lkpRoom };
            if (lkpRoom.connectedRooms != null) {
                foreach (RoomNode neighbor in lkpRoom.connectedRooms) {
                    if (neighbor == null) continue;
                    BoxCollider box = neighbor.GetComponent<BoxCollider>();
                    if (box != null && Vector3.Distance(state.LastKnownPosition, box.bounds.center) <= _maxSearchRadius) {
                        candidates.Add(neighbor);
                    }
                }
            }
            return candidates[Random.Range(0, candidates.Count)];
        }

        private List<Transform> SortWaypointsByProximity(List<WayPointData> waypoints, Vector3 referencePosition) {
            List<Transform> transforms = waypoints.ConvertAll(wp => wp.transform);
            transforms.Sort((a, b) => Vector3.Distance(referencePosition, a.position).CompareTo(Vector3.Distance(referencePosition, b.position)));
            return transforms;
        }
    }
}