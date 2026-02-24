using UnityEngine;
using System.Collections.Generic;
using AgenticPrison.Physical;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior.PrimitiveTasks {

    public class PatrolPrimitiveTask : IPrimitiveTask {
        
        // Volvemos a usar la cola de Transforms para los waypoints
        private Queue<Transform> _route;
        private bool _isInitialized = false;

        public bool CheckPreconditions(WorldState state) {
            return state.CurrentRoomNode != null && state.AssignedQuadrant != null;
        }

        public void ApplyEffects(WorldState state) {
            state.Fatigue += 0.1f;
        }

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            
            if (!_isInitialized) {
                _route = GenerateDFSRoute(state);
                _isInitialized = true;
            }

            if (_route == null || _route.Count == 0) {
                return TaskExecutionStatus.Success;
            }

            if (!actuators.IsMoving()) {
                Transform nextWaypoint = _route.Dequeue();
                actuators.SetDestination(nextWaypoint);
            }

            return TaskExecutionStatus.Running;
        }

        private Queue<Transform> GenerateDFSRoute(WorldState state) {

            Queue<Transform> finalRoute = new Queue<Transform>();
            HashSet<RoomNode> visitedRooms = new HashSet<RoomNode>();
            Stack<RoomNode> stack = new Stack<RoomNode>();

            stack.Push(state.CurrentRoomNode);

            while (stack.Count > 0) {
                RoomNode currentRoom = stack.Pop();

                if (visitedRooms.Contains(currentRoom)) continue;
                
                visitedRooms.Add(currentRoom);

                // Si la sala es nuestra, AÑADIMOS SUS WAYPOINTS a la lista de la compra
                if (state.AssignedQuadrant.Contains(currentRoom)) {
                    if (currentRoom.waypoints != null) {
                        foreach (Transform wp in currentRoom.waypoints) {
                            if (wp != null) {
                                finalRoute.Enqueue(wp);
                            }
                        }
                    }
                }

                // Expandir a los vecinos
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