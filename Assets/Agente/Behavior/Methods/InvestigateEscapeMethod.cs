using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class InvestigateEscapeMethod : IMethod {
        
        // Radio máximo en metros al que el preso podría haber corrido en esos pocos segundos
        private readonly float _maxSearchRadius = 15f; 

        public bool CheckPreconditions(WorldState state) {
            return !state.FugitiveInVision && state.LastKnownPosition != Vector3.zero;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            // 1. Obtener el nodo donde desapareció
            RoomNode lkpRoom = state.Map.GetCurrentNode(state.LastKnownPosition);
            
            // 2. Recopilar candidatos (La sala actual SIEMPRE es candidata)
            List<RoomNode> candidates = new List<RoomNode> { lkpRoom };
            
            // FILTRO DE DISTANCIA: Revisamos las salas conectadas
            foreach (RoomNode neighbor in lkpRoom.connectedRooms) {
                // Calculamos la distancia desde la LKP hasta el centro volumétrico de la sala vecina
                float distance = Vector3.Distance(state.LastKnownPosition, neighbor.GetComponent<BoxCollider>().bounds.center);
                
                if (distance <= _maxSearchRadius) {
                    candidates.Add(neighbor);
                }
            }

            // 3. Elegir una sala al azar de entre los candidatos viables
            RoomNode chosenRoom = candidates[Random.Range(0, candidates.Count)];
            Debug.Log($"[InvestigateEscapeMethod] El guardia va a registrar la sala: {chosenRoom.gameObject.name}");

            // 4. Clonar la lista de waypoints para poder ordenarla
            List<Transform> waypointsToSearch = new List<Transform>(chosenRoom.waypoints);

            // 5. ORDENAR los waypoints del más cercano a la LKP al más lejano
            waypointsToSearch.Sort((a, b) => {
                float distA = Vector3.Distance(state.LastKnownPosition, a.position);
                float distB = Vector3.Distance(state.LastKnownPosition, b.position);
                return distA.CompareTo(distB);
            });

            // 6. GENERAR EL PLAN
            // Primero, ir a toda leche al punto exacto donde lo perdió
            subTasks.Enqueue(new MoveTask(state.LastKnownPosition, 6.5f)); 

            // Luego, ir rápido (5.5f) mirando en los waypoints ordenados de esa sala lógica
            foreach (Transform wp in waypointsToSearch) {
                subTasks.Enqueue(new MoveTask(wp.position, 5.5f));
            }

            return subTasks;
        }
    }
}