using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Communication;
using AgenticPrison.Behavior.PrimitiveTasks;
using AgenticPrison.Physical;

using AgenticPrison.Communication.Messages;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: el agente asignado como blocker patrulla cíclicamente sus waypoints
    // de perímetro mientras el equipo esté activo. Sin ClearAssignedTaskTask al final:
    // el ciclo se repite en cada replanificación hasta que el líder disuelva el equipo.
    public class BlockingPositionMethod : IMethod {

        private const float BlockSpeed   = 4.0f;
        private const float SprintSpeed  = 6.0f;
        private const float FarThreshold = 10f;  // distancia desde la que se considera "lejos"

        public bool CheckPreconditions(WorldState state) {
            return state.AssignedTask != null
                && state.AssignedTask.AssignedRole == AgentRole.Blocker
                && state.AssignedTask.WayPoints    != null
                && state.AssignedTask.WayPoints.Count > 0
                && !string.IsNullOrEmpty(state.TeamName);
        }

        public Queue<ITask> Decompose(WorldState state) {
            var subTasks = new Queue<ITask>();

            subTasks.Enqueue(new ChangeFlashLight(Color.pink));

            foreach (WayPointData wp in state.AssignedTask.WayPoints) {
                // Sprint si el guardia está lejos del waypoint (p. ej. al recibir la asignación)
                float dist  = Vector3.Distance(state.CurrentPosition, wp.transform.position);
                float speed = dist > FarThreshold ? SprintSpeed : BlockSpeed;
                subTasks.Enqueue(new MoveTask(wp.transform.position, speed));
            }

            // Sin ClearAssignedTaskTask: el blocker sigue ciclando hasta que reciba la disolución
            return subTasks;
        }
    }
}
