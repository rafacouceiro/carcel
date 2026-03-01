using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    public class WideSweepMethod : IMethod {
        
        private const float SweepSpeed = 4.5f; 

        public bool CheckPreconditions(WorldState state) {
            float age = Time.time - state.LastKnownPositionTime;
            return state.LastKnownPosition != Vector3.zero && !state.PrisonerInCell && age < 35f;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            // Color de la linterna (Cyan para el barrido amplio)
            subTasks.Enqueue(new ChangeFlashLight(new Color(0f, 1f, 1f)));

            List<Vector3> sweepPoints = CalculateGreedySweep(state);

            foreach (Vector3 point in sweepPoints) {
                subTasks.Enqueue(new MoveTask(point, SweepSpeed));
            }

            subTasks.Enqueue(new ClearPositionTask());

            return subTasks;
        }

        private List<Vector3> CalculateGreedySweep(WorldState state) {
            RoomNode lkpRoom = state.Map.GetCurrentNode(state.LastKnownPosition);
            RoomNode currentRoom = state.Map.GetCurrentNode(state.CurrentPosition);
            
            if (lkpRoom == null) return new List<Vector3>();

            // --- 1. RECOPILACIÓN DE SALAS (Expansión de 1er Grado) ---
            HashSet<RoomNode> roomsToSearch = new HashSet<RoomNode>();

            // Añadimos la propia sala donde fue visto por última vez
            roomsToSearch.Add(lkpRoom);

            if (lkpRoom.connectedRooms != null) {
                foreach (RoomNode neighbor in lkpRoom.connectedRooms) {
                    if (neighbor == null) continue;
                    
                    // Añadimos las conexiones directas del LKP
                    roomsToSearch.Add(neighbor);

                    // AÑADIMOS LAS CONEXIONES DE PRIMER GRADO DE LOS VECINOS
                    if (neighbor.connectedRooms != null) {
                        foreach (RoomNode subNeighbor in neighbor.connectedRooms) {
                            if (subNeighbor != null) {
                                roomsToSearch.Add(subNeighbor);
                            }
                        }
                    }
                }
            }

            // Ignoramos la sala en la que está ahora mismo el guardia
            roomsToSearch.Remove(currentRoom);


            // --- 2. EXTRACCIÓN DE WAYPOINTS ---
            List<WayPointData> candidatePoints = new List<WayPointData>();

            foreach (RoomNode room in roomsToSearch) {
                if (room.waypoints == null) continue;

                foreach (WayPointData wp in room.waypoints) {
                    if (wp != null && wp.isPatrolCheckpoint) { // Solo puntos clave
                        candidatePoints.Add(wp);
                    }
                }
            }


            // --- 3. ORDENAMIENTO GREEDY (Vecino más cercano) ---
            List<Vector3> route = new List<Vector3>();
            Vector3 simulationPos = state.CurrentPosition;
            NavMeshPath path = new NavMeshPath();

            while (candidatePoints.Count > 0) {
                WayPointData closestWp = null;
                float minDistance = Mathf.Infinity;
                int closestIndex = -1;

                for (int i = 0; i < candidatePoints.Count; i++) {
                    if (NavMesh.CalculatePath(simulationPos, candidatePoints[i].transform.position, NavMesh.AllAreas, path)) {
                        float dist = CalculatePathLength(path);
                        if (dist < minDistance) {
                            minDistance = dist;
                            closestWp = candidatePoints[i];
                            closestIndex = i;
                        }
                    }
                }

                if (closestWp != null) {
                    route.Add(closestWp.transform.position);
                    simulationPos = closestWp.transform.position;
                    candidatePoints.RemoveAt(closestIndex);
                } else {
                    candidatePoints.RemoveAt(0); // Descartar inalcanzables
                }
            }

            return route;
        }

        private float CalculatePathLength(NavMeshPath path) {
            if (path.corners.Length < 2) return 0f;
            float length = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++) {
                length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            return length;
        }
    }
}