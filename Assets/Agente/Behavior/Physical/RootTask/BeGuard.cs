using System.Collections.Generic;
using System;
using UnityEngine;
using AgenticPrison.Core;
using AgenticPrison.Behavior.Methods;
using AgenticPrison.Behavior.PrimitiveTasks;
using AgenticPrison.Communication;
using AgenticPrison.Agents.Guard.Physical;

namespace AgenticPrison.Behavior.RootTask {

    // Tarea principal (RootTask) que define la prioridad de comportamiento del agente.
    // Phase 2: AssignedTaskMethod se evalúa primero si hay contrato activo y !FugitiveInVision.
    public class BeGuard : ICompoundTask {

        public List<IMethod> Methods { get; }

        public BeGuard(FIPAAgent agent = null) {
            Methods = new List<IMethod> {
                new AssignedTaskMethod(agent),  // Phase 2: prioridad máxima si !FugitiveInVision
                new SelectEmergency(),
                new SelectInvestigation(),
                new SelectRoutine()
            };
        }
    }

    // Método HTN Phase 2: ejecuta la tarea asignada por contrato con prioridad máxima,
    // pero cede ante EmergencyTask si el fugitivo está a la vista.
    public class AssignedTaskMethod : IMethod {

        readonly FIPAAgent _agent;

        public AssignedTaskMethod(FIPAAgent agent) { _agent = agent; }

        public bool CheckPreconditions(WorldState state) {
            return state.AssignedTask != null && !state.FugitiveInVision;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            ContractTask t = state.AssignedTask;

            tasks.Enqueue(new MoveTask(t.Target, 4.0f));

            // InvestigateRoom: dos rondas de inspección; GuardWaypoint: tres
            int lookRounds = t.Type == TaskType.InvestigateRoom ? 2 : 3;
            for (int i = 0; i < lookRounds; i++)
                tasks.Enqueue(new LookAroundTask());

            // Notifica al iniciador que la tarea está completa y limpia AssignedTask
            tasks.Enqueue(new InformDoneTask(_agent));

            return tasks;
        }
    }
}