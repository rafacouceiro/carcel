using UnityEngine;
using AgenticPrison.Core;

namespace AgenticPrison.Behavior {
    public class ChangeFlashLight : IPrimitiveTask {
        
        private Color _targetColor;

        public ChangeFlashLight(Color color) {
            _targetColor = color;
        }

        public bool CheckPreconditions(WorldState state) => true;

        public TaskExecutionStatus Execute(IMovable actuators, WorldState state) {
            // Cambia el color físicamente
            actuators.SetLightColor(_targetColor);
            
            // Termina al instante para pasar a la siguiente tarea (ej: moverse)
            return TaskExecutionStatus.Success; 
        }

        public void ApplyEffects(WorldState state) {
            // No necesitamos cambiar nada en el WorldState
        }
    }
}