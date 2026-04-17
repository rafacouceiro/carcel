using UnityEngine;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks; 

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Realiza una ronda de vigilancia por las zonas asignadas
    public class PatrolMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            // Condición: Estado de normalidad (nadie se ha fugado que sepamos)
            return state.PrisonerInCell;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            // Luz verde indica patrullaje pasivo
            subTasks.Enqueue(new ChangeFlashLight(Color.green));
            
            // Computa el recorrido usando búsqueda en profundidad
            List<Transform> route = GenerateDFSRoute(state);

            // Transforma el listado de nodos en comandos de movimiento
            foreach (Transform waypoint in route) {
                subTasks.Enqueue(new MoveTask(waypoint.position, 3.0f));
            }

            return subTasks; 
        }

        // --- Planificación de Patrullaje --- 
        // Genera la ruta recorriendo las salas del cuadrante asignado usando DFS
        private List<Transform> GenerateDFSRoute(WorldState state) {

            List<RoomNode> quadrantRooms = state.Map.GetSection(state.AssignedQuadrantId);
            List<Transform> finalRoute = new List<Transform>();
            HashSet<RoomNode> visitedRooms = new HashSet<RoomNode>();
            Stack<RoomNode> stack = new Stack<RoomNode>();

            RoomNode spawnRoom = state.Map.GetCurrentNode(state.CurrentPosition);
            stack.Push(spawnRoom);

            while (stack.Count > 0) {
                RoomNode currentRoom = stack.Pop();

                // Prevención de bucles infinitos en el grafo de habitaciones
                if (visitedRooms.Contains(currentRoom)) continue;
                visitedRooms.Add(currentRoom);

                // Filtro para visitar solo habitaciones del cuadrante propio
                if (quadrantRooms.Contains(currentRoom)) {
                    if (currentRoom.waypoints != null) {
                        foreach (WayPointData wp in currentRoom.waypoints) {
                            
                            // Selecciona estrictamente los puntos marcados para patrulla
                            if (wp != null && wp.isPatrolCheckpoint) {
                                Transform wp_transform = wp.transform;
                                if (wp_transform != null) {
                                    finalRoute.Add(wp_transform);
                                }
                            }
                        }
                    }
                }

                // Agrega habitaciones conectadas a la pila de expansión DFS
                if (currentRoom.connectedRooms != null) {
                    foreach (RoomNode neighbor in currentRoom.connectedRooms) {
                        if (neighbor != null && !visitedRooms.Contains(neighbor)) {
                            stack.Push(neighbor);
                        }
                    }
                }
            }

            return finalRoute;
        }
    }
}