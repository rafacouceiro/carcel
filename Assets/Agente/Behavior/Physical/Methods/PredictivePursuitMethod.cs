using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Intenta predecir hacia dónde huyó el fugitivo recién perdido de vista
    public class PredictivePursuitMethod : IMethod {
        
        private readonly float _maxSearchRadius = 15f; 
        private const float SearchSpeed = 6.5f;

        public bool CheckPreconditions(GuardWorldState state) {
            // Requiere: Haberlo visto hace menos de 2 segundos, y estar listos para buscarlo
            bool isFresh = (Time.time - state.LastKnownPositionTime) < 2f;
            return state.LastKnownPosition != Vector3.zero && !state.PrisonerInCell && isFresh;
        }

        public Queue<ITask> Decompose(GuardWorldState state) {
            var subTasks = new Queue<ITask>();
            
            // Luz azul indica barrido táctico a toda velocidad
            subTasks.Enqueue(new ChangeFlashLight(Color.blue));
            
            // Primero, ir corriendo al sitio exacto donde se le vio por última vez
            subTasks.Enqueue(new MoveTask(state.LastKnownPosition, SearchSpeed)); 

            // Luego, investigar la sala probable de huida
            List<Transform> pointsToSearch = ComposeSearchRoute(state);

            foreach (Transform wp in pointsToSearch) {
                subTasks.Enqueue(new MoveTask(wp.position, SearchSpeed));
            }

            return subTasks;
        }

        // Diseña una ruta rápida seleccionando una habitación adyacente lógica
        private List<Transform> ComposeSearchRoute(GuardWorldState state) {
            RoomNode chosenRoom = ChooseSearchRoom(state);
            if (chosenRoom == null || chosenRoom.waypoints == null) return new List<Transform>();
            
            // Ordenamos los puntos de la sala desde el más cercano para ahorrar tiempo
            return SortWaypointsByProximity(chosenRoom.waypoints, state.LastKnownPosition);
        }

        // Escoge una sala conectada al último avistamiento que caiga en un radio lógico de persecución
        private RoomNode ChooseSearchRoom(GuardWorldState state) {
            RoomNode lkpRoom = state.Map.GetCurrentNode(state.LastKnownPosition);
            if (lkpRoom == null) return null;
            
            List<RoomNode> candidates = new List<RoomNode> { lkpRoom };
            if (lkpRoom.connectedRooms != null) {
                foreach (RoomNode neighbor in lkpRoom.connectedRooms) {
                    if (neighbor == null) continue;
                    BoxCollider box = neighbor.GetComponent<BoxCollider>();
                    
                    // Solo consideramos habitaciones que el fugitivo podría haber alcanzado 
                    if (box != null && Vector3.Distance(state.LastKnownPosition, box.bounds.center) <= _maxSearchRadius) {
                        candidates.Add(neighbor);
                    }
                }
            }
            // Elegimos una al azar de las opciones probables
            return candidates[Random.Range(0, candidates.Count)];
        }

        // Ordenamiento por cercanía
        private List<Transform> SortWaypointsByProximity(List<WayPointData> waypoints, Vector3 referencePosition) {
            List<Transform> transforms = waypoints.ConvertAll(wp => wp.transform);
            transforms.Sort((a, b) => Vector3.Distance(referencePosition, a.position).CompareTo(Vector3.Distance(referencePosition, b.position)));
            return transforms;
        }
    }
}