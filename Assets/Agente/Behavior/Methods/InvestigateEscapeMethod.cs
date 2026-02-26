using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class InvestigateEscapeMethod : IMethod {
        
        private readonly float _maxSearchRadius = 15f; 
        private const float SearchSpeed = 6.5f;

        public bool CheckPreconditions(WorldState state) {
            return state.LastKnownPosition != Vector3.zero && !state.PrisonerInCell;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            // 1. Linterna azul y correr al lugar del escape
            subTasks.Enqueue(new ChangeFlashLight(Color.blue));
            subTasks.Enqueue(new MoveTask(state.LastKnownPosition, SearchSpeed)); 

            // 2. Componer la ruta lógica
            List<Transform> pointsToSearch = ComposeSearchRoute(state);

            // 3. Encolar los movimientos de búsqueda
            foreach (Transform wp in pointsToSearch) {
                subTasks.Enqueue(new MoveTask(wp.position, SearchSpeed));
            }

            subTasks.Enqueue(new ClearPositionTask());

            return subTasks;
        }

        /// <summary>
        /// Método orquestador: Elige la sala y ordena sus puntos.
        /// </summary>
        private List<Transform> ComposeSearchRoute(WorldState state) {
            
            RoomNode chosenRoom = ChooseSearchRoom(state);
            
            if (chosenRoom == null || chosenRoom.waypoints == null) {
                return new List<Transform>();
            }

            return SortWaypointsByProximity(chosenRoom.waypoints, state.LastKnownPosition);
        }

        /// <summary>
        /// Recopila candidatos basados en radio y escoge uno al azar.
        /// </summary>
        private RoomNode ChooseSearchRoom(WorldState state) {
            
            RoomNode lkpRoom = state.Map.GetCurrentNode(state.LastKnownPosition);
            if (lkpRoom == null) return null;
            
            List<RoomNode> candidates = new List<RoomNode> { lkpRoom };
            
            if (lkpRoom.connectedRooms != null) {
                foreach (RoomNode neighbor in lkpRoom.connectedRooms) {
                    if (neighbor == null) continue;

                    BoxCollider box = neighbor.GetComponent<BoxCollider>();
                    if (box != null) {
                        float distance = Vector3.Distance(state.LastKnownPosition, box.bounds.center);
                        if (distance <= _maxSearchRadius) {
                            candidates.Add(neighbor);
                        }
                    }
                }
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// Transforma los datos y los ordena matemáticamente por cercanía.
        /// </summary>
        private List<Transform> SortWaypointsByProximity(List<WayPointData> waypoints, Vector3 referencePosition) {
            
            List<Transform> transforms = waypoints.ConvertAll(wp => wp.transform);

            transforms.Sort((a, b) => {
                float distA = Vector3.Distance(referencePosition, a.position);
                float distB = Vector3.Distance(referencePosition, b.position);
                return distA.CompareTo(distB);
            });

            return transforms;
        }
    }
}