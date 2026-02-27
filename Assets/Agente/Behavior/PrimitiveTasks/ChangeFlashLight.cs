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

            actuators.SetLightColor(_targetColor);
            return TaskExecutionStatus.Success; 
        }

        public void ApplyEffects(WorldState state) {
        }
    }
}