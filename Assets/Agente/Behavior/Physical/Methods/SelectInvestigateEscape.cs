using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Selector para iniciar rastreo activo por pérdida visual
    public class SelectInvestigateEscape : IMethod {
        public bool CheckPreconditions(WorldState state) {
            // Activable si sabemos que escapó, existe última posición conocida, y no pasó mucho tiempo (25s)
            bool firstCond = state.LastKnownPosition != Vector3.zero && !state.PrisonerInCell && (Time.time - state.LastKnownPositionTime) < 25f;
            return firstCond || state.AssignedTask != null;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            // Delega la resolución a la rama de búsqueda del fugitivo
            tasks.Enqueue(new InvestigateEscapeTask()); 
            return tasks;
        }
    }
}