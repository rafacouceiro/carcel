using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Selector de ramificación para casos de emergencia
    public class SelectEmergency : IMethod {
        public bool CheckPreconditions(WorldState state) {
            // Condición estricta: Avistamiento activo
            return state.FugitiveInVision;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            // Delegación de resolución al sub-árbol de emergencia
            tasks.Enqueue(new EmergencyTask()); 
            return tasks;
        }
    }
}