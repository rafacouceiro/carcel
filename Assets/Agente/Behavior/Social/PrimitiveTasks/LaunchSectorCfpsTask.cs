using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;

namespace AgenticPrison.Behavior.Social {

    // Tarea social: lanza subastas Contract Net para tapar el perímetro del sector
    // (blockers) y rastrear sus habitaciones (sweepers). El iniciador se asigna la
    // primera mitad de habitaciones como su propio sweep.
    // FugitiveSectorId se actualiza AL FINAL, tras lanzar los protocolos, para que
    // InitiateContractNetMethod pueda comprobar el sector antiguo antes de decidir.
    public class LaunchSectorCfpsTask : IPrimitiveTask {

        readonly FIPAAgent _agent;
        readonly float     _replyByWindow;

        public LaunchSectorCfpsTask(FIPAAgent agent, float replyByWindow) {
            _agent         = agent;
            _replyByWindow = replyByWindow;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision && state.LastKnownPosition != Vector3.zero;
        }

        public void ApplyEffects(WorldState state) {
            state.ContractNetActive = true;
            state.AssignedRole      = AgentRole.Sweeper;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            // 1. Determinar el sector del fugitivo
            List<string> sectors = state.Map.GetFugitiveSectors(state.LastKnownPosition);
            if (sectors.Count != 1) {
                Debug.Log($"[{state.AgentName}] LaunchSectorCfpsTask: sector ambiguo ({sectors.Count} sectores)");
                return TaskExecutionStatus.Failure;
            }
            string sectorId = sectors[0];

            // 2. No relanzar si el sector ya está perimetrado (FugitiveSectorId aún no actualizado)
            if (sectorId == state.FugitiveSectorId && state.TeamMembers.Count > 0) {
                Debug.Log($"[{state.AgentName}] LaunchSectorCfpsTask: sector {sectorId} ya perimetrado");
                return TaskExecutionStatus.Failure;
            }

            // 3. Lanzar CFPs de bloqueo — uno por grupo de puntos de perímetro
            Dictionary<string, List<WayPointData>> blockingGroups = state.Map.GetBlockingGroupsForSector(sectorId);
            foreach (var pair in blockingGroups) {
                List<WayPointData> waypoints = pair.Value;
                if (waypoints.Count == 0) continue;

                var blockTask = new ContractTask {
                    Type         = TaskType.BlockSector,
                    AssignedRole = AgentRole.Blocker,
                    WayPoints    = new List<WayPointData>(waypoints),
                    SectorId     = sectorId,
                    Target       = waypoints[0].transform.position
                };
                _agent.LaunchProtocol(new ContractNetInitiator(blockTask, _agent.AgentId, _replyByWindow), state);
                Debug.Log($"<color=cyan>[{state.AgentName}] CFP blocker: grupo {pair.Key} ({waypoints.Count} wps)</color>");
            }

            // 4. Obtener y ordenar habitaciones de rastreo por distancia greedy
            List<RoomNode> sweepRooms  = state.Map.GetSweepRoomsForSector(sectorId);
            List<RoomNode> orderedRooms = SortRoomsGreedy(sweepRooms, state.CurrentPosition);

            // Dividir en mitades: primera para el iniciador, segunda para subcontratar
            int half       = orderedRooms.Count / 2;
            int ownCount   = (half > 0) ? half : orderedRooms.Count;
            int otherStart = ownCount;
            int otherCount = orderedRooms.Count - ownCount;

            List<RoomNode> myRooms    = orderedRooms.GetRange(0, ownCount);
            List<RoomNode> otherRooms = (otherCount > 0)
                ? orderedRooms.GetRange(otherStart, otherCount)
                : new List<RoomNode>();

            // 5. Lanzar CFP de sweep para la segunda mitad
            if (otherRooms.Count > 0) {
                var sweepTask = new ContractTask {
                    Type         = TaskType.SweepSector,
                    AssignedRole = AgentRole.Sweeper,
                    SweepRooms   = new List<RoomNode>(otherRooms),
                    SectorId     = sectorId,
                    Target       = otherRooms[0].GetNavigablePosition(),
                    InitiatorId  = _agent.AgentId
                };
                _agent.LaunchProtocol(new ContractNetInitiator(sweepTask, _agent.AgentId, _replyByWindow), state);
                state.SweepProtocolsActive++;
                Debug.Log($"<color=cyan>[{state.AgentName}] CFP sweeper: {otherRooms.Count} habitaciones</color>");
            }

            // 6. Asignarse la primera mitad como sweep propio (líder es también sweeper)
            if (myRooms.Count > 0) {
                state.AssignedTask = new ContractTask {
                    Type         = TaskType.SweepSector,
                    AssignedRole = AgentRole.Sweeper,
                    SweepRooms   = new List<RoomNode>(myRooms),
                    SectorId     = sectorId,
                    InitiatorId  = _agent.AgentId
                };
            }
            state.AssignedRole = AgentRole.Sweeper;

            // 7. Actualizar estado global tras lanzar todos los protocolos
            state.FugitiveSectorId  = sectorId;
            state.ContractNetActive = true;
            if (!state.TeamMembers.Contains(_agent.AgentId))
                state.TeamMembers.Add(_agent.AgentId);

            Debug.Log($"<color=red><b>[{state.AgentName}] Sector {sectorId}: {blockingGroups.Count} blocker(s), {myRooms.Count} salas propias, {otherRooms.Count} subcontratadas</b></color>");
            return TaskExecutionStatus.Success;
        }

        // Ordena habitaciones por distancia NavMesh greedy desde el punto de partida
        private List<RoomNode> SortRoomsGreedy(List<RoomNode> rooms, Vector3 startPos) {
            var remaining = new List<RoomNode>(rooms);
            var sorted    = new List<RoomNode>();
            Vector3 current  = startPos;
            NavMeshPath path = new NavMeshPath();

            while (remaining.Count > 0) {
                RoomNode closest   = null;
                float    minDist   = Mathf.Infinity;
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

        private float CalculatePathLength(NavMeshPath path) {
            float length = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
                length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            return length;
        }
    }
}
