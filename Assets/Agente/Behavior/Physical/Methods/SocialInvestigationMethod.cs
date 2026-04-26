using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Behavior.PrimitiveTasks;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: recorre todos los waypoints de la habitación asignada por contrato social.
    // Se activa cuando otro guardia ha ganado una subasta y debe investigar esa sala concreta.
    public class SocialInvestigationMethod : IMethod {

        private const float InvestigateSpeed = 6.5f;

        public bool CheckPreconditions(WorldState state) {
            return state.AssignedTask != null;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();

            // Color magenta: distingue visualmente la investigación coordinada por protocolo social
            subTasks.Enqueue(new ChangeFlashLight(Color.magenta));

            RoomNode room = state.AssignedTask.Room;

            if (room != null && room.waypoints != null && room.waypoints.Count > 0) {
                foreach (WayPointData wp in room.waypoints) {
                    if (wp != null)
                        subTasks.Enqueue(new MoveTask(wp.transform.position, InvestigateSpeed));
                }
            } else {
                // Fallback: ir al punto exacto del contrato si no hay habitación definida
                subTasks.Enqueue(new MoveTask(state.AssignedTask.Target, InvestigateSpeed));
            }

            return subTasks;
        }
    }
}
