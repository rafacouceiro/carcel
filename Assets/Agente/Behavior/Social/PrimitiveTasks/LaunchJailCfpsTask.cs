using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Agents.Tools;

namespace AgenticPrison.Behavior.Social {

    // Barrido completo cuando el sector es "[UNK]": cada guardia recibe un sector entero.
    // El líder coge la primera tarea de sweep para sí y encola el resto como CFPs.
    public class LaunchJailCfpsTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public LaunchJailCfpsTask(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(GuardWorldState state) =>
            state.FugitiveSectorId == "[UNK]" && string.IsNullOrEmpty(state.TeamName);

        public void ApplyEffects(GuardWorldState state) {
            state.TeamName          = "pending"; // placeholder para el planificador; Execute lo sobreescribe
            state.AssignedRole      = AgentRole.Sweeper;
            state.ShouldInitiateCnp = false;
        }

        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) {
            var plan = PerimeterTool.GenerateJailWidePlan(state.Map, state.AgentName);

            state.TeamName             = plan.TeamName;
            state.PendingSweepersCount = plan.TotalSweepers;
            state.ShouldInitiateCnp    = false;

            // El líder se queda con la primera tarea de Sweeping
            ContractTask myTask = plan.AllTasks.Find(t => t.AssignedRole == AgentRole.Sweeper);
            if (myTask != null) {
                state.AssignedTask = myTask;
                plan.AllTasks.Remove(myTask);
            }

            // El resto de tareas van a la cola secuencial
            state.PendingCfps.Clear();
            foreach (var task in plan.AllTasks) {
                state.PendingCfps.Enqueue(task);
            }

            FIPAAgent.SubscribeToChannel(_agent.AgentId, "team_" + plan.TeamName);

            Debug.Log($"<color=orange><b>[{state.AgentName}] Operación CÁRCEL COMPLETA iniciada. " +
                      $"{state.PendingCfps.Count} subastas en cola.</b></color>");
            return TaskExecutionStatus.Success;
        }
    }
}
