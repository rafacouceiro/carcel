using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;

namespace AgenticPrison.Agents.Guard.Social {

    // Tarea social: lanza subastas Contract Net para las habitaciones adyacentes al fugitivo.
    // Efecto optimista: añade "pending" a TeamMembers para bloquear subastas duplicadas.
    public class LaunchRoomCfpsTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public LaunchRoomCfpsTask(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision && state.TeamMembers.Count == 0;
        }

        public void ApplyEffects(WorldState state) {
            // Bloquea nuevas subastas hasta que todos los protocolos activos terminen
            state.ContractNetActive = true;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            List<RoomNode> targets = AdjacentRoomGenerator.GetAdjacentRooms(
                state.LastKnownPosition, state.Map, 2);

            if (targets.Count == 0) {
                Debug.Log($"[{state.AgentName}] LaunchRoomCfpsTask: sin habitaciones adyacentes");
                return TaskExecutionStatus.Failure;
            }

            foreach (RoomNode room in targets) {
                var task = new ContractTask {
                    Type       = TaskType.InvestigateRoom,
                    Target     = room.transform.position,
                    Priority   = TaskPriority.Investigate,
                    ContractId = System.Guid.NewGuid().ToString()
                };

                var protocol = new ContractNetProtocol(task, _agent.AgentId);
                _agent.LaunchProtocol(protocol, state);

                Debug.Log($"[{state.AgentName}] CFP lanzado para habitación {room.name}");
            }

            // Activar lock en el estado real — ApplyEffects lo hace en el clon del planificador,
            // pero Execute opera sobre CurrentState, así que hay que hacerlo aquí también.
            state.ContractNetActive = true;
            return TaskExecutionStatus.Success;
        }
    }
}
