using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Agents.Tools {

    // Utilidad estática para generar la configuración de una operación de sector.
    // Proporciona tareas de bloqueo y rastreo basadas en el diseño del mapa.
    public static class PerimeterTool {

        // Cantidad de sweepers recomendada por sector (ID: Cantidad)
        private static readonly Dictionary<string, int> SweepersPerSector = new Dictionary<string, int> {
            { "1", 4 },
            { "2", 5 },
            { "3", 5 },
            { "4", 4 }
        };

        public struct TeamPlan {
            public string             TeamName;
            public List<ContractTask> AllTasks;
            public int                TotalSweepers;
        }

        // Genera todas las tareas necesarias para cubrir un sector.
        // IDs de todos los sectores del mapa — usados para el barrido completo de la cárcel
        private static readonly string[] AllSectorIds = { "1", "2", "3", "4" };

        // Genera un plan de barrido completo de la cárcel: cada guardia barre un sector entero.
        // Los puntos de bloqueo son los del sector 4, que cierran el perímetro exterior.
        public static TeamPlan GenerateJailWidePlan(PrisonMap map, string leaderName) {
            var plan = new TeamPlan {
                TeamName = leaderName + "_jail_" + Mathf.FloorToInt(Time.time),
                AllTasks = new List<ContractTask>()
            };

            // 1. Blockers: puntos de bloqueo del sector 4 cierran todo el perímetro
            var blockingGroups = map.GetBlockingGroupsForSector("4");
            foreach (var pair in blockingGroups) {
                if (pair.Value.Count == 0) continue;
                plan.AllTasks.Add(new ContractTask {
                    Type          = TaskType.BlockSector,
                    AssignedRole  = AgentRole.Blocker,
                    WayPoints     = new List<WayPointData>(pair.Value),
                    Target        = pair.Value[0].transform.position,
                    TeamName      = plan.TeamName,
                    TotalSweepers = AllSectorIds.Length
                });
            }

            // 2. Sweepers: un guardia por sector, barre todas las salas de ese sector
            int sweepersAdded = 0;
            foreach (string sectorId in AllSectorIds) {
                List<RoomNode> rooms = map.GetSweepRoomsForSector(sectorId);
                if (rooms.Count == 0) continue;
                plan.AllTasks.Add(new ContractTask {
                    Type          = TaskType.SweepSector,
                    AssignedRole  = AgentRole.Sweeper,
                    SweepRooms    = new List<RoomNode>(rooms),
                    Target        = rooms[0].GetNavigablePosition(),
                    TeamName      = plan.TeamName,
                    TotalSweepers = AllSectorIds.Length
                });
                sweepersAdded++;
            }
            plan.TotalSweepers = sweepersAdded;
            return plan;
        }

        public static TeamPlan GenerateTeamPlan(string sectorId, PrisonMap map, string leaderName) {
            var plan = new TeamPlan {
                TeamName = leaderName + "_" + Mathf.FloorToInt(Time.time),
                AllTasks = new List<ContractTask>()
            };

            // 1. Determinar cuántos sweepers se necesitan
            plan.TotalSweepers = SweepersPerSector.TryGetValue(sectorId, out int count) ? count : 2;

            // 2. Tareas de BLOQUEO: una por cada grupo de salida del sector
            var blockingGroups = map.GetBlockingGroupsForSector(sectorId);
            foreach (var pair in blockingGroups) {
                if (pair.Value.Count == 0) continue;
                plan.AllTasks.Add(new ContractTask {
                    Type          = TaskType.BlockSector,
                    AssignedRole  = AgentRole.Blocker,
                    WayPoints     = new List<WayPointData>(pair.Value),
                    Target        = pair.Value[0].transform.position,
                    TeamName      = plan.TeamName,
                    TotalSweepers = plan.TotalSweepers
                });
            }

            // 3. Tareas de RASTREO: dividir las salas del sector entre los sweepers
            List<RoomNode> sweepRooms = map.GetSweepRoomsForSector(sectorId);
            if (sweepRooms.Count > 0) {
                int roomsPerSweeper = Mathf.CeilToInt((float)sweepRooms.Count / plan.TotalSweepers);
                for (int i = 0; i < plan.TotalSweepers; i++) {
                    int start = i * roomsPerSweeper;
                    if (start >= sweepRooms.Count) break;

                    int amount = Mathf.Min(roomsPerSweeper, sweepRooms.Count - start);
                    List<RoomNode> slice = sweepRooms.GetRange(start, amount);

                    plan.AllTasks.Add(new ContractTask {
                        Type          = TaskType.SweepSector,
                        AssignedRole  = AgentRole.Sweeper,
                        SweepRooms    = slice,
                        Target        = slice[0].GetNavigablePosition(),
                        TeamName      = plan.TeamName,
                        TotalSweepers = plan.TotalSweepers
                    });
                }
            }

            return plan;
        }
    }
}
