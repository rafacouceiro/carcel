using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class InvestigateEscapeMethod : IMethod {
        
        // Radio máximo en metros al que el preso podría haber corrido en apenas segundos
        private readonly float _maxSearchRadius = 15f; 

        public bool CheckPreconditions(WorldState state) {
            return state.LastKnownPosition != Vector3.zero;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            subTasks.Enqueue(new ChangeFlashLight(Color.blue));
            
            // Obtener el nodo donde desapareció
            RoomNode lkpRoom = state.Map.GetCurrentNode(state.LastKnownPosition);
            
            // Recopilar candidatos (La sala actual siempre es candidata)
            List<RoomNode> candidates = new List<RoomNode> { lkpRoom };
            
            // Revisamos salas conectadas a la LKP
            foreach (RoomNode neighbor in lkpRoom.connectedRooms) {
                // Filtramos por distancia
                float distance = Vector3.Distance(state.LastKnownPosition, neighbor.GetComponent<BoxCollider>().bounds.center);
                
                if (distance <= _maxSearchRadius) {
                    candidates.Add(neighbor);
                }
            }

            // Elegir una sala al azar de entre los candidatos viables
            RoomNode chosenRoom = candidates[Random.Range(0, candidates.Count)];
            List<Transform> waypointsToSearch = chosenRoom.waypoints.ConvertAll(wp => wp.transform);

            // Ordenar los waypoints del más cercano a la LKP al más lejano
            waypointsToSearch.Sort((a, b) => {
                float distA = Vector3.Distance(state.LastKnownPosition, a.position);
                float distB = Vector3.Distance(state.LastKnownPosition, b.position);
                return distA.CompareTo(distB);
            });

            // Generar el plan
            subTasks.Enqueue(new MoveTask(state.LastKnownPosition, 6.5f)); 

            // Buscar en la sala elegida
            foreach (Transform wp in waypointsToSearch) {
                subTasks.Enqueue(new MoveTask(wp.position, 5.5f));
            }

            return subTasks;
        }
    }
}