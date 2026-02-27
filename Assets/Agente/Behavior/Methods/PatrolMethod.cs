using UnityEngine;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks; // Para poder usar MoveTask
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    public class PatrolMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            return state.PrisonerInCell;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            subTasks.Enqueue(new ChangeFlashLight(Color.green));
            
            // Ejecutar el DFS para obtener la ruta
            List<Transform> route = GenerateDFSRoute(state);

            // Descomponer el método en tareas
            foreach (Transform waypoint in route) {
                subTasks.Enqueue(new MoveTask(waypoint.position, 3.0f));
            }

            return subTasks; // Devolvemos una cola llena de MoveTasks
        }

        // Planificación del patrullaje
        private List<Transform> GenerateDFSRoute(WorldState state) {

            List<RoomNode> quadrantRooms = state.Map.GetSection(state.AssignedQuadrantId);
            List<Transform> finalRoute = new List<Transform>();
            HashSet<RoomNode> visitedRooms = new HashSet<RoomNode>();
            Stack<RoomNode> stack = new Stack<RoomNode>();

            RoomNode spawnRoom = state.Map.GetCurrentNode(state.CurrentPosition);
            stack.Push(spawnRoom);

            while (stack.Count > 0) {
                RoomNode currentRoom = stack.Pop();

                if (visitedRooms.Contains(currentRoom)) continue;
                visitedRooms.Add(currentRoom);

                if (quadrantRooms.Contains(currentRoom)) {
                    if (currentRoom.waypoints != null) {
                        foreach (WayPointData wp in currentRoom.waypoints) {
                            
                            // --- EL FILTRO ESTÁ AQUÍ ---
                            // Solo lo añadimos si el script WayPointData tiene marcado 'Is Patrol Checkpoint'
                            if (wp != null && wp.isPatrolCheckpoint) {
                                Transform wp_transform = wp.transform;
                                if (wp_transform != null) {
                                    finalRoute.Add(wp_transform);
                                }
                            }
                        }
                    }
                }

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