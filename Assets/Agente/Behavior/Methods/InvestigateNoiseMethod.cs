using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class InvestigateNoiseMethod : IMethod {
        
        private readonly float _searchRadius = 18f; // Radio para cubrir varias salas
        private readonly int _maxPointsToInspect = 3; // Cuántos sitios mirará antes de rendirse

        public bool CheckPreconditions(WorldState state) {
            // Se activa si hay una posición de ruido guardada
            return state.LastNoisePosition != Vector3.zero;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            List<WayPointData> candidates = new List<WayPointData>();

            // 1. Buscamos en TODOS los nodos del mapa que estén cerca del ruido
            foreach (RoomNode room in PrisonMap.Instance.GetAllNodes()) {
                float distToRoom = Vector3.Distance(state.LastNoisePosition, room.GetComponent<BoxCollider>().bounds.center);
                
                if (distToRoom <= _searchRadius) {
                    // Añadimos los puntos clave o de patrulla de estas salas
                    foreach (var wp in room.waypoints) {
                        if (wp.isKeyPoint || wp.isPatrolCheckpoint) {
                            candidates.Add(wp);
                        }
                    }
                }
            }

            // 2. Si no hay puntos cerca, vamos al menos al punto exacto del ruido
            if (candidates.Count == 0) {
                subTasks.Enqueue(new MoveTask(state.LastNoisePosition, 4f));
            } 
            else {
                // 3. Selección Aleatoria: Barajamos los puntos encontrados
                for (int i = 0; i < candidates.Count; i++) {
                    WayPointData temp = candidates[i];
                    int randomIndex = Random.Range(i, candidates.Count);
                    candidates[i] = candidates[randomIndex];
                    candidates[randomIndex] = temp;
                }

                // 4. Creamos la ruta de investigación (limitada a N puntos)
                int pointsAdded = 0;
                foreach (var wp in candidates) {
                    if (pointsAdded >= _maxPointsToInspect) break;
                    
                    // Velocidad de 4f (paso ligero, alerta)
                    subTasks.Enqueue(new MoveTask(wp.transform.position, 4f));
                    pointsAdded++;
                }
            }

            // 5. IMPORTANTE: Limpiar el ruido al terminar para no entrar en bucle
            subTasks.Enqueue(new ClearNoiseTask());

            return subTasks;
        }
    }
}