using AgenticPrison.Core;
using UnityEngine;

namespace AgenticPrison.Behavior.PrimitiveTasks {
    // Tarea primitiva: Borra de la memoria la última ubicación conocida del fugitivo
    public class ClearPositionTask : IPrimitiveTask {
        public bool CheckPreconditions(GuardWorldState state) => true;

        // Limpieza de memoria visual en simulación teórica
        public void ApplyEffects(GuardWorldState state) {
            state.LastKnownPosition = Vector3.zero; 
        }

        // Limpieza de memoria visual real
        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) {
            state.LastKnownPosition = Vector3.zero;
            return TaskExecutionStatus.Success;
        }
    }
}