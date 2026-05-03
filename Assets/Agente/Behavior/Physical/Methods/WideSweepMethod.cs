using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Behavior.PrimitiveTasks;

using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: barrido sistemático. Cubre dos casos:
    //   a) El guardia avistó al fugitivo y lo rastrea por su propia cuenta (seenByMe).
    //   b) El guardia tiene asignado un SweepSector por contrato (AssignedRole == Sweeper).
    public class WideSweepMethod : IMethod {

        private const float SweepSpeed = 4.5f;

        public bool CheckPreconditions(GuardWorldState state) {
            bool hasSighting = state.seenByMe
                && state.LastKnownPosition != Vector3.zero
                && !state.PrisonerInCell
                && (Time.time - state.LastKnownPositionTime) < 35f;

            bool isSweeper = state.AssignedTask != null
                && state.AssignedTask.AssignedRole == AgentRole.Sweeper
                && state.AssignedTask.SweepRooms   != null
                && state.AssignedTask.SweepRooms.Count > 0;

            return hasSighting || isSweeper;
        }

        public Queue<ITask> Decompose(GuardWorldState state) {
            var subTasks = new Queue<ITask>();

            bool isSweeper = state.AssignedTask != null
                && state.AssignedTask.AssignedRole == AgentRole.Sweeper
                && state.AssignedTask.SweepRooms   != null
                && state.AssignedTask.SweepRooms.Count > 0;

            if (isSweeper) {
                // Barrido de salas asignadas por contrato
                subTasks.Enqueue(new ChangeFlashLight(new Color(0f, 1f, 1f)));

                List<RoomNode> orderedRooms = SortRoomsGreedy(
                    state.AssignedTask.SweepRooms, state.CurrentPosition);

                foreach (RoomNode room in orderedRooms) {
                    if (room.waypoints != null) {
                        foreach (WayPointData wp in room.waypoints) {
                            if (wp != null && wp.isPatrolCheckpoint)
                                subTasks.Enqueue(new MoveTask(wp.transform.position, SweepSpeed));
                        }
                    }
                    // Persiste el progreso: si el plan se interrumpe, no se repite esta sala
                    subTasks.Enqueue(new RemoveSweepRoomTask(room));
                }

                // Notifica al Participant que el sweep ha terminado → envía InformDone al líder
                subTasks.Enqueue(new ClearAssignedTaskTask());
            } else {
                // Barrido libre por avistamiento propio
                subTasks.Enqueue(new ChangeFlashLight(new Color(0f, 1f, 1f)));

                List<Vector3> sweepPoints = CalculateGreedySweep(state);
                foreach (Vector3 point in sweepPoints)
                    subTasks.Enqueue(new MoveTask(point, SweepSpeed));

                subTasks.Enqueue(new ClearPositionTask());
            }

            return subTasks;
        }

        // Ordena habitaciones por distancia NavMesh greedy desde el punto de partida
        private List<RoomNode> SortRoomsGreedy(List<RoomNode> rooms, Vector3 startPos) {
            var remaining = new List<RoomNode>(rooms);
            var sorted    = new List<RoomNode>();
            Vector3 current  = startPos;
            NavMeshPath path = new NavMeshPath();

            while (remaining.Count > 0) {
                RoomNode closest    = null;
                float    minDist    = Mathf.Infinity;
                int      closestIdx = 0;

                for (int i = 0; i < remaining.Count; i++) {
                    Vector3 target = remaining[i].GetNavigablePosition();
                    float dist = Mathf.Infinity;
                    if (NavMesh.CalculatePath(current, target, NavMesh.AllAreas, path))
                        dist = CalculatePathLength(path);
                    if (dist < minDist) { minDist = dist; closest = remaining[i]; closestIdx = i; }
                }

                if (closest != null) {
                    sorted.Add(closest);
                    current = closest.GetNavigablePosition();
                    remaining.RemoveAt(closestIdx);
                } else {
                    remaining.RemoveAt(0);
                }
            }
            return sorted;
        }

        // --- Algoritmo de Barrido Amplio ---
        // Genera una ruta inspeccionando nodos a partir de la sala ligada a la última posición conocida
        private List<Vector3> CalculateGreedySweep(GuardWorldState state) {
            RoomNode lkpRoom = state.Map.GetCurrentNode(state.LastKnownPosition);
            RoomNode currentRoom = state.Map.GetCurrentNode(state.CurrentPosition);
            
            if (lkpRoom == null) return new List<Vector3>();

            // --- 1. RECOPILACIÓN MATRICIAL DE SALAS (Expansión algorítmica de 1er Grado) ---
            HashSet<RoomNode> roomsToSearch = new HashSet<RoomNode>();

            // Eje central de la búsqueda sistemática
            roomsToSearch.Add(lkpRoom);

            if (lkpRoom.connectedRooms != null) {
                foreach (RoomNode neighbor in lkpRoom.connectedRooms) {
                    if (neighbor == null) continue;
                    
                    // Adyacencias directas desde el eje
                    roomsToSearch.Add(neighbor);

                    // Repercusión ramificada: Conexiones anidadas desde el 1er grado
                    if (neighbor.connectedRooms != null) {
                        foreach (RoomNode subNeighbor in neighbor.connectedRooms) {
                            if (subNeighbor != null) {
                                roomsToSearch.Add(subNeighbor);
                            }
                        }
                    }
                }
            }

            // Descarta la sala ocupada actualmente para asegurar progreso exploratorio
            roomsToSearch.Remove(currentRoom);


            // --- 2. EXTRACCIÓN Y FILTRO DE WAYPOINTS ---
            List<WayPointData> candidatePoints = new List<WayPointData>();

            foreach (RoomNode room in roomsToSearch) {
                if (room.waypoints == null) continue;

                foreach (WayPointData wp in room.waypoints) {
                    // Evalúa solamente puntos predefinidos estables
                    if (wp != null && wp.isPatrolCheckpoint) { 
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
                    // Revisa la distancia lógica calculada real dentro del NavMesh
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
                    simulationPos = closestWp.transform.position; // Evolución del foco calculador
                    candidatePoints.RemoveAt(closestIndex);
                } else {
                    candidatePoints.RemoveAt(0); // Eliminación de elementos no enrutables
                }
            }

            return route;
        }

        // Longitud integral del path basándose en sus intersecciones espaciales
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