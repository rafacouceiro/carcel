using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;

using AgenticPrison.Communication.Messages;
using AgenticPrison.Communication.Protocols.ContractNet;
using AgenticPrison.Agents.Tools;

namespace AgenticPrison.Behavior.Social {

    // Lanza subastas CNP para cubrir el perímetro del sector (blockers) y rastrear
    // sus habitaciones (sweepers). El iniciador se queda con la primera tarea de sweep.
    public class LaunchSectorCfpsTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public LaunchSectorCfpsTask(FIPAAgent agent) {
            _agent         = agent;
        }

        public bool CheckPreconditions(GuardWorldState state) {
            return state.seenByMe && state.LastKnownPosition != Vector3.zero;
        }

        public void ApplyEffects(GuardWorldState state) {
            state.TeamName     = "pending"; // el planificador solo necesita que no esté vacío; Execute lo asigna de verdad
            state.AssignedRole = AgentRole.Sweeper;
        }

        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) {
            List<string> sectors = state.Map.GetFugitiveSectors(state.LastKnownPosition);
            if (sectors.Count != 1) return TaskExecutionStatus.Failure;
            string sectorId = sectors[0];

            var plan = PerimeterTool.GenerateTeamPlan(sectorId, state.Map, state.AgentName);

            state.TeamName             = plan.TeamName;
            state.PendingSweepersCount = plan.TotalSweepers;
            state.PerimeteredSectorId  = sectorId;

            // El líder se queda con la primera tarea de sweep; el resto va a la cola de CFPs
            ContractTask myTask = plan.AllTasks.Find(t => t.AssignedRole == AgentRole.Sweeper);
            if (myTask != null) {
                state.AssignedTask = myTask;
                plan.AllTasks.Remove(myTask);
            }

            state.PendingCfps.Clear();
            foreach (var task in plan.AllTasks)
                state.PendingCfps.Enqueue(task);

            FIPAAgent.SubscribeToChannel(_agent.AgentId, "team_" + plan.TeamName);

            Debug.Log($"<color=red><b>[{state.AgentName}] Operación en {sectorId} iniciada. {state.PendingCfps.Count} subastas en cola secuencial.</b></color>");
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
