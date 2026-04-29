using System.Collections.Generic;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Physical;
using AgenticPrison.Communication;
using AgenticPrison.Communication.Messages;
using AgenticPrison.Agents.Tools;

namespace AgenticPrison.Behavior.Social {

    // Tarea social: inicia un barrido completo de la cárcel cuando el sector del fugitivo
    // es desconocido. Asigna un sector entero a cada guardia (todos los rooms de ese sector).
    // Los puntos de bloqueo son los del sector 4, que cierran todo el perímetro.
    public class LaunchJailCfpsTask : IPrimitiveTask {

        readonly FIPAAgent _agent;

        public LaunchJailCfpsTask(FIPAAgent agent) {
            _agent = agent;
        }

        public bool CheckPreconditions(WorldState state) =>
            state.FugitiveSectorId == "[UNK]" && !state.ContractNetActive;

        public void ApplyEffects(WorldState state) {
            state.ContractNetActive = true;
            state.AssignedRole      = AgentRole.Sweeper;
        }

        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            var plan = PerimeterTool.GenerateJailWidePlan(state.Map, state.AgentName);

            state.TeamName             = plan.TeamName;
            state.PendingSweepersCount = plan.TotalSweepers;
            state.ContractNetActive    = true;

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
