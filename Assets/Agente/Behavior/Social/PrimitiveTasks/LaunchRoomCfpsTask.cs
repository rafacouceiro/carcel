using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Agents.Tools;

namespace AgenticPrison.Behavior.Social {

    // Tarea social: lanza subastas Contract Net para las habitaciones adyacentes al fugitivo.
    // Efecto optimista: añade "pending" a TeamMembers para bloquear subastas duplicadas.
    public class LaunchRoomCfpsTask : IPrimitiveTask {

        readonly FIPAAgent _agent;
        readonly float     _replyByWindow;

        public LaunchRoomCfpsTask(FIPAAgent agent, float replyByWindow) {
            _agent         = agent;
            _replyByWindow = replyByWindow;
        }

        public bool CheckPreconditions(WorldState state) {
            return state.FugitiveInVision;
        }

        public void ApplyEffects(WorldState state) {
            state.ContractNetActive = true;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            List<RoomNode> targets = AdjacentRoomGenerator.GetAdjacentRooms(
                state.LastKnownPosition, state.Map, 3);

            if (targets.Count == 0) {
                Debug.Log($"[{state.AgentName}] LaunchRoomCfpsTask: sin habitaciones candidatas");
                return TaskExecutionStatus.Failure;
            }

            Debug.Log($"<color=red><b>[{state.AgentName}] LaunchRoomCfpsTask {targets.Count} habitaciones candidatas (grado 1-2, top por conexiones)</b></color>");

            foreach (RoomNode room in targets) {
                var task = new ContractTask {
                    Type       = TaskType.InvestigateRoom,
                    Room       = room,
                    Target     = room.GetNavigablePosition(),
                    ContractId = System.Guid.NewGuid().ToString()
                };

                var protocol = new ContractNetInitiator(task, _agent.AgentId, _replyByWindow);
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
