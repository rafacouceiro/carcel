using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Expansión de búsqueda en un radio amplio cuando se pierde toda pista directa del fugitivo
    public class WideSweepMethod : IMethod {
        
        private const float SweepSpeed = 4.5f; 

        public bool CheckPreconditions(WorldState state) {
            // Solo el guardia que avistó directamente al fugitivo hace el barrido amplio.
            // Los que reciben el CFP no deben perseguir por cuenta propia: solo ejecutan su tarea asignada.
            float age = Time.time - state.LastKnownPositionTime;
            return state.seenByMe && state.LastKnownPosition != Vector3.zero && !state.PrisonerInCell && age < 35f;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();
            
            // Revisa el estado de la linterna y colorea en Cyan indicando barrido sistemático
            subTasks.Enqueue(new ChangeFlashLight(new Color(0f, 1f, 1f)));

            // Coordina la lectura de varias habitaciones y waypoints posibles a través de un Greedy
            List<Vector3> sweepPoints = CalculateGreedySweep(state);

            // Convierte en trayectos físicos
            foreach (Vector3 point in sweepPoints) {
                subTasks.Enqueue(new MoveTask(point, SweepSpeed));
            }

            // Una vez terminado, anula la última posición de la memoria del agente
            subTasks.Enqueue(new ClearPositionTask());

            return subTasks;
        }

        // --- Algoritmo de Barrido Amplio ---
        // Genera una ruta inspeccionando nodos a partir de la sala ligada a la última posición conocida
        private List<Vector3> CalculateGreedySweep(WorldState state) {
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