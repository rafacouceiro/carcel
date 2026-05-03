using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior {
    // Tarea primitiva: Modifica el color de la luz del agente (p.ej: patrullaje vs persecución)
    public class ChangeFlashLight : IPrimitiveTask {
        
        private Color _targetColor;

        public ChangeFlashLight(Color color) {
            _targetColor = color;
        }

        // Siempre ejecutable visualmente
        public bool CheckPreconditions(GuardWorldState state) => true;

        public TaskExecutionStatus Execute(IActuators actuators, GuardWorldState state) {
            // Lógica física directa
            actuators.SetLightColor(_targetColor);
            return TaskExecutionStatus.Success; 
        }

        // Esta acción no posee efectos sobre el estado interno simulado
        public void ApplyEffects(GuardWorldState state) {
        }
    }
}