using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Selector de ramificación para tareas rutinarias
    public class SelectRoutine : IMethod {
        public bool CheckPreconditions(GuardWorldState state) {
            // Se asume habilitado por defecto si no hay estímulos que activar otra acción
            return true; 
        }

        public Queue<ITask> Decompose(GuardWorldState state) {
            var tasks = new Queue<ITask>();
            // Delegación a la tarea compuesta rutinaria
            tasks.Enqueue(new RoutineTask());
            return tasks;
        }
    }
}