using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Selector de ramificación para recuperar estamina
    public class SelectEnergyRecovery : IMethod {
        public bool CheckPreconditions(GuardWorldState state) {
            // Apta siempre como último recurso rutinario
            return true;
        }

        public Queue<ITask> Decompose(GuardWorldState state) {
            var tasks = new Queue<ITask>();
            // Delegación a la tarea compuesta de descanso
            tasks.Enqueue(new EnergyRecoveryTask());
            return tasks;
        }
    }
}