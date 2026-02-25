using UnityEngine;
using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks; // Para poder usar MoveTask
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    public class PatrolMethod : IMethod {
        
        public bool CheckPreconditions(WorldState state) {
            // Solo patrullamos si sabemos dónde estamos y no estamos reventados
            return state.Fatigue < 0.9f && state.PrisonerInCell;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            // 1. El Cerebro ejecuta el DFS
            List<Transform> route = GenerateDFSRoute(state);

            // 2. El Cerebro descompone la ruta en tareas individuales
            foreach (Transform waypoint in route) {
                
                // AQUÍ LE DECIMOS LA VELOCIDAD DE PATRULLA (ej: 2.5f)
                subTasks.Enqueue(new MoveTask(waypoint.position, 2.5f));
            }

            return subTasks; // Devolvemos una cola llena de MoveTasks
        }

        // --- LA LÓGICA DFS AHORA VIVE EN EL MÉTODO ---
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
                        foreach (Transform wp in currentRoom.waypoints) {
                            if (wp != null) finalRoute.Add(wp);
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