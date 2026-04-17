using AgenticPrison.Core;
using UnityEngine;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    // Tarea primitiva: Borra de la memoria la última ubicación conocida del fugitivo
    public class ClearPositionTask : IPrimitiveTask {
        public bool CheckPreconditions(WorldState state) => true;

        // Limpieza de memoria visual en simulación teórica
        public void ApplyEffects(WorldState state) {
            state.LastKnownPosition = Vector3.zero; 
        }

        // Limpieza de memoria visual real
        public TaskExecutionStatus Execute(IActuators actuators, WorldState state) {
            state.LastKnownPosition = Vector3.zero;
            return TaskExecutionStatus.Success;
        }
    }
}