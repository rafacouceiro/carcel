using System.Collections.Generic;
using AgenticPrison.Core;
using AgenticPrison.Behavior.CompoundTasks;
using UnityEngine;

namespace AgenticPrison.Behavior.Methods {

    // Método HTN: Selector de ramificación para casos de emergencia
    public class SelectEmergency : IMethod {
        public bool CheckPreconditions(WorldState state) {

            bool isFresh = (Time.time - state.LastKnownPositionTime) < 2f;
            return state.FugitiveInVision || isFresh;
        }

        public Queue<ITask> Decompose(WorldState state) {
            var tasks = new Queue<ITask>();
            // Delegación de resolución al sub-árbol de emergencia
            tasks.Enqueue(new EmergencyTask()); 
            return tasks;
        }
    }
}