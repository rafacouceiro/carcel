using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Selector de ramificación genérico para investigaciones
    public class SelectInvestigation : IMethod {
        public bool CheckPreconditions(WorldState state) {
            // Evaluado siempre como la segunda prioridad del agente (tras fallar emergencias)
            return true;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            // Delegación de la lógica a la tarea compuesta de investigación
            tasks.Enqueue(new InvestigationTask()); 
            return tasks;
        }
    }
}